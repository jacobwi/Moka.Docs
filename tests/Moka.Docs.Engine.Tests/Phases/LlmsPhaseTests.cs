using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.Engine.Phases;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     Generated pages: C# API pages get a Markdown copy rendered from the API model; other generated
///     pages are listed in llms.txt with their HTML URL.
/// </summary>
public sealed class LlmsPhaseTests
{
	private const string _source = """
	                               namespace Lib.Text
	                               {
	                                   /// <summary>Parses text.</summary>
	                                   public class Parser
	                                   {
	                                       /// <summary>Parses it.</summary>
	                                       public int Parse(string text) => 0;
	                                   }
	                               }
	                               """;

	private static async Task<BuildContext> RunAsync(bool dryRun = false)
	{
		ApiReference model = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance)
			.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(_source)], "Lib");
		List<ApiType> types = model.Namespaces.SelectMany(ns => ns.Types).ToList();
		ApiTypeRoutes routes = ApiTypeRoutes.For(types);

		var context = new BuildContext
		{
			Config = new SiteConfig
			{
				Site = new SiteMetadata { Title = "Lib", Url = "https://lib.example" },
				Build = new BuildConfig { BasePath = "/docs" }
			},
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site",
			DryRun = dryRun,
			ApiModel = model
		};

		context.Pages.Add(Page("/api", "API Reference"));
		context.Pages.AddRange(types.Select(t => Page(routes.Route(t), routes.DisplayName(t))));
		context.Pages.Add(Page("/rest-api/pets", "Pets"));

		await new LlmsPhase(new MarkdownParser(), NullLogger<LlmsPhase>.Instance)
			.ExecuteAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	private static DocPage Page(string route, string title) => new()
	{
		FrontMatter = new FrontMatter { Title = title },
		Content = new PageContent { Html = "<p>x</p>" },
		Route = route,
		Origin = PageOrigin.ApiGenerated
	};

	private static string Staged(BuildContext context, string path) =>
		Encoding.UTF8.GetString(context.DeferredOutputFiles[path]);

	[Fact]
	public async Task ApiTypePage_GetsACopyRenderedFromTheModel()
	{
		BuildContext context = await RunAsync();

		Staged(context, "api/lib/text/parser.md").Should().StartWith("# Parser\n\nClass in `Lib.Text`")
			.And.Contain("### Parse(string text)");
	}

	[Fact]
	public async Task ApiIndex_ListsTheTypesWithTheirCopies()
	{
		BuildContext context = await RunAsync();

		Staged(context, "api.md").Should().Be(
			"# API Reference\n\n## Lib.Text\n\n- [Parser](https://lib.example/docs/api/lib/text/parser.md): Parses text.\n");
	}

	[Fact]
	public async Task OtherGeneratedPages_AreListedWithTheirHtmlUrl()
	{
		BuildContext context = await RunAsync();

		context.DeferredOutputFiles.Should().NotContainKey("rest-api/pets.md");
		Staged(context, "llms.txt").Should().Contain("- [Pets](https://lib.example/docs/rest-api/pets/)");
		Staged(context, "llms-full.txt").Should().Contain("# Parser").And.NotContain("Pets");
		LlmsPhase.StagedMarkdownUrls(context).Should().ContainKey("/api").And.NotContainKey("/rest-api/pets");
	}

	[Fact]
	public async Task DryRun_StagesNothing()
	{
		BuildContext context = await RunAsync(dryRun: true);

		context.DeferredOutputFiles.Should().BeEmpty();
	}

	[Theory]
	[InlineData("/", "index.md")]
	[InlineData("", "index.md")]
	[InlineData("/guide/intro", "guide/intro.md")]
	[InlineData("/guide/", "guide.md")]
	public void MarkdownPath_SitsNextToThePagesFolder(string route, string path)
	{
		LlmsPhase.MarkdownPath(route).Should().Be(path);
	}
}
