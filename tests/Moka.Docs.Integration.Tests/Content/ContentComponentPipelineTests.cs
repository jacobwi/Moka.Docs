using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;

namespace Moka.Docs.Integration.Tests.Content;

/// <summary>
///     The Markdown components through the whole build: <c>xref:</c> links against the API model, the glossary
///     from mokadocs.yaml and glossary.yml, the package version, search text and the Markdown copies.
/// </summary>
public sealed class ContentComponentPipelineTests
{
	private const string _root = "/project";

	private static readonly ApiReference _api = new()
	{
		Namespaces =
		[
			new ApiNamespace
			{
				Name = "MyLib.Text",
				Types =
				[
					new ApiType
					{
						Name = "Parser",
						FullName = "MyLib.Text.Parser",
						Namespace = "MyLib.Text",
						Kind = ApiTypeKind.Class,
						Members =
						[
							new ApiMember
							{
								Name = "Parse",
								Kind = ApiMemberKind.Method,
								Signature = "public string Parse(string text)",
								Parameters = [new ApiParameter { Name = "text", Type = "string" }],
								Documentation = new XmlDocBlock { Summary = "Parses." }
							}
						]
					},
					new ApiType
					{
						Name = "Result",
						FullName = "MyLib.Text.Result<T>",
						Namespace = "MyLib.Text",
						Kind = ApiTypeKind.Class,
						TypeParameters = [new ApiTypeParameter { Name = "T" }]
					}
				]
			}
		]
	};

	private static async Task<(BuildContext Context, MockFileSystem FileSystem)> BuildAsync(
		Dictionary<string, MockFileData> files, Func<SiteConfig, SiteConfig>? configure = null, bool withApi = true)
	{
		var fs = new MockFileSystem(files);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IFileSystem>(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		await using ServiceProvider provider = services.BuildServiceProvider();
		SiteConfig config = MockSite.Config();
		var context = new BuildContext
		{
			Config = configure?.Invoke(config) ?? config,
			FileSystem = fs,
			RootDirectory = _root,
			OutputDirectory = _root + "/_site",
			// Analysis doesn't run without projects, so a model set here is the one the pages see.
			ApiModel = withApi ? _api : null,
			PackageInfo = new PackageMetadata { Name = "MyLib", Version = "2.3.4" }
		};

		await provider.GetRequiredService<BuildPipeline>().ExecuteAsync(context, TestContext.Current.CancellationToken);
		return (context, fs);
	}

	private static string Page(MockFileSystem fs, string route) =>
		fs.File.ReadAllText($"{_root}/_site/{route.Trim('/')}/index.html".Replace("//", "/"));

	[Fact]
	public async Task Xref_ResolvesToTheApiRoutesAndAnchors_WithTheBasePath()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/guide.md"] = new("---\ntitle: Guide\n---\n# Guide\n\n" +
			                                 "See [the parser](xref:MyLib.Text.Parser), <xref:MyLib.Text.Parser.Parse>, " +
			                                 "<xref:MyLib.Text.Result`1>, <xref:T:MyLib.Text.Result`1>, [short](xref:Parser) " +
			                                 "and <xref:System.String>.\n")
		}, config => config with { Build = config.Build with { BasePath = "/Repo" } });

		string html = Page(fs, "/guide");
		html.Should().Contain("<a href=\"/Repo/api/mylib/text/parser\" class=\"xref\">the parser</a>")
			.And.Contain("<a href=\"/Repo/api/mylib/text/parser#parse\" class=\"xref\">Parser.Parse</a>")
			.And.Contain("<a href=\"/Repo/api/mylib/text/result\" class=\"xref\">Result&lt;T&gt;</a>")
			.And.Contain("<a href=\"/Repo/api/mylib/text/parser\" class=\"xref\">short</a>")
			.And.Contain("<a href=\"https://learn.microsoft.com/dotnet/api/system.string\" class=\"xref\">String</a>");
		context.Diagnostics.All.Should().NotContain(d => d.Message.Contains("xref"));
	}

	[Fact]
	public async Task Xref_Unresolved_IsAWarningWithThePageAndLine()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/guide/intro.md"] = new("---\ntitle: Intro\n---\n# Intro\n\nSee <xref:MyLib.Text.Missing>.\n")
		});

		Page(fs, "/guide/intro").Should().Contain("See <code>MyLib.Text.Missing</code>.");
		Diagnostic warning = context.Diagnostics.All.Should()
			.ContainSingle(d => d.Severity == DiagnosticSeverity.Warning && d.Message.Contains("xref")).Which;
		warning.Message.Should().MatchRegex(@"^guide[\\/]intro\.md: ")
			.And.EndWith("xref:MyLib.Text.Missing doesn't match a documented type or member, so it isn't a link (line 6)");
		warning.Source.Should().Be("MarkdownParse");
	}

	[Fact]
	public async Task Glossary_ComesFromTheConfigAndTheDocsFolderFile_WhichIsNotPublished()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n\nThe :term[API] and :term[route].\n\n:::glossary\n:::\n"),
			["/project/docs/glossary.yml"] = new("Route: A page's path.\napi: Ignored, the config has it.\nEmpty:\n")
		}, config => config with { Glossary = new Dictionary<string, string> { ["API"] = "The public surface." } });

		string html = Page(fs, "/");
		html.Should().Contain("id=\"mokadocs-term-1\" data-no-search>The public surface.</span>")
			.And.Contain("id=\"mokadocs-term-2\" data-no-search>A page's path.</span>")
			.And.Contain("<dt id=\"glossary-api\">API</dt>\n<dd>The public surface.</dd>\n<dt id=\"glossary-route\">Route</dt>");
		fs.File.Exists("/project/_site/glossary.yml").Should().BeFalse();
		context.Diagnostics.All.Select(d => d.Message).Should()
			.Contain(m => m.StartsWith("glossary.yml: 'api' is already in the glossary"))
			.And.Contain(m => m.StartsWith("glossary.yml: the term 'Empty' has no definition") && m.EndsWith("(line 3)"));
	}

	[Fact]
	public async Task Package_WithoutAVersion_GetsTheDocumentedPackagesVersion()
	{
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n\n:::package MyLib\n:::\n\n:::package Other\n:::\n")
		});

		string html = Page(fs, "/");
		html.Should().Contain("dotnet add package MyLib --version 2.3.4").And.Contain("dotnet add package Other</code>");
	}

	[Fact]
	public async Task Search_LeavesTooltipDefinitionsOut_AndIndexesTheGlossaryList()
	{
		(BuildContext context, _) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n\n## Usage\n\nCall the :term[API] here.\n\n## Terms\n\n:::glossary\n:::\n")
		}, config => config with { Glossary = new Dictionary<string, string> { ["API"] = "Xylophone surface." } });

		List<SearchEntry> entries = context.SearchIndex!.Entries;
		entries.Single(e => e.Section == "Usage").Content.Should().Contain("Call the API here.").And.NotContain("Xylophone");
		entries.Single(e => e.Section == "Terms").Content.Should().Contain("API").And.Contain("Xylophone surface.");
	}

	[Fact]
	public async Task MarkdownCopy_KeepsComponentsAndResolvesXrefs()
	{
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/guide.md"] = new("---\ntitle: Guide\n---\n# Guide\n\n:::details Why?\n:badge[New] <xref:MyLib.Text.Parser>\n:::\n")
		});

		string copy = Encoding.UTF8.GetString(fs.File.ReadAllBytes("/project/_site/guide.md"));
		copy.Should().Contain(":::details Why?\n:badge[New] [Parser](https://test.example.com/api/mylib/text/parser)\n:::");
	}

	[Fact]
	public async Task Xref_WithoutAnApiModel_IsAWarning()
	{
		(BuildContext context, _) = await BuildAsync(new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n\n[x](xref:MyLib.Text.Parser)\n")
		}, withApi: false);

		context.Diagnostics.All.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning
		                                              && d.Message.Contains("xref:MyLib.Text.Parser doesn't match"));
		context.Pages.Single(p => p.Route == "/").Content.Html.Should().Contain("<p>x</p>");
	}
}
