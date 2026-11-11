using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     View Source and SourceLink in the ASP.NET Core host, read from the portable PDB of a
///     library compiled at test time.
/// </summary>
public sealed class ReflectionSourceTests
{
	private static readonly string _githubBlob = $"https://github.com/contoso/widgets/blob/{SourceFixtureAssembly.Commit}/";
	private static readonly string _gitLabBlob = $"https://gitlab.com/contoso/widgets/-/blob/{SourceFixtureAssembly.Commit}/";

	// The first code in Widget.cs is the Name property: its accessors and its initializer.
	private static readonly int _widgetLine = SourceFixtureAssembly.LineOf(SourceFixtureAssembly.WidgetSource, "public string Name");

	internal static ApiReference Build(Assembly assembly, Action<MokaDocsOptions>? configure = null, DiagnosticBag? diagnostics = null)
	{
		var options = new MokaDocsOptions { Assemblies = [assembly] };
		configure?.Invoke(options);
		return new ReflectionApiModelBuilder(
				new XmlDocParser(NullLogger<XmlDocParser>.Instance),
				new InheritDocResolver(),
				NullLogger<ReflectionApiModelBuilder>.Instance)
			.Build(options, Directory.GetCurrentDirectory(), diagnostics ?? new DiagnosticBag());
	}

	internal static ApiType Type(ApiReference model, string name) =>
		model.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == name);

	[Fact]
	public void SourceUrl_GitHub_OpensTheFileAtTheTypesFirstLine()
	{
		ApiReference model = Build(SourceFixtureAssembly.Sidecar);

		Type(model, "Widget").SourceUrl.Should().Be($"{_githubBlob}{SourceFixtureAssembly.WidgetPath}#L{_widgetLine}");
	}

	[Fact]
	public void SourceUrl_GitLab_OpensTheFileAtTheTypesFirstLine()
	{
		ApiReference model = Build(SourceFixtureAssembly.Embedded);

		Type(model, "Widget").SourceUrl.Should().Be($"{_gitLabBlob}{SourceFixtureAssembly.WidgetPath}#L{_widgetLine}");
	}

	[Fact]
	public void SourceUrl_PartialType_SkipsTheGeneratedPartTheRepositoryDoesNotHave()
	{
		// The generated part comes first in the PDB, but it is embedded, as untracked files are, so it isn't in the repository.
		ApiReference model = Build(SourceFixtureAssembly.Sidecar);

		Type(model, "Widget").SourceUrl.Should().StartWith(_githubBlob + SourceFixtureAssembly.WidgetPath + "#L");
	}

	[Fact]
	public void SourceUrl_CodeInAnIterator_CountsForTheTypeThatDeclaresIt()
	{
		// The iterator's body compiles into a nested state machine type, which has the sequence points.
		ApiReference model = Build(SourceFixtureAssembly.Sidecar);
		int iteratorLine = SourceFixtureAssembly.LineOf(SourceFixtureAssembly.KindsSource, "public static IEnumerable<int> Count()") + 1;

		Type(model, "Sequences").SourceUrl.Should().Be($"{_githubBlob}{SourceFixtureAssembly.KindsPath}#L{iteratorLine}");
	}

	[Fact]
	public void SourceUrl_TypeWithoutMethodBodies_LinksToTheFileWithoutALine()
	{
		ApiReference model = Build(SourceFixtureAssembly.Sidecar);

		Type(model, "WidgetKind").SourceUrl.Should().Be(_githubBlob + SourceFixtureAssembly.KindsPath);
		Type(model, "ISpinner").SourceUrl.Should().Be(_githubBlob + SourceFixtureAssembly.KindsPath);
	}

	[Fact]
	public void SourceUrl_SourceLinksOff_IsNotSet()
	{
		ApiReference model = Build(SourceFixtureAssembly.Sidecar, options => options.SourceLinks = false);

		model.Namespaces.SelectMany(ns => ns.Types).Should().OnlyContain(t => t.SourceUrl == null);
	}

	[Fact]
	public void IncludeSourceCode_IsOffByDefault()
	{
		// Source can expose method bodies of a web application, so reading it is opt-in.
		new MokaDocsOptions().IncludeSourceCode.Should().BeFalse();
		ApiReference model = Build(SourceFixtureAssembly.Embedded);

		model.Namespaces.SelectMany(ns => ns.Types).Should().OnlyContain(t => t.SourceCode == null);
	}

	[Fact]
	public void IncludeSourceCode_EmbeddedSource_ShowsTheDocumentedMembersOfEveryPart()
	{
		ApiReference model = Build(SourceFixtureAssembly.Embedded, options => options.IncludeSourceCode = true);

		string source = Type(model, "Widget").SourceCode!;
		source.Should().Contain("// Widget.cs").And.Contain("// Widget.Part.cs");
		source.Should().Contain("public int Spin()").And.Contain("return Helper() + 1;");
		source.Should().Contain("protected virtual void OnSpin()");
		source.Should().Contain("public void Reset()");
		source.Should().NotContain("InternalHelper").And.NotContain("private int Helper").And.NotContain("Legacy");
	}

	[Fact]
	public void IncludeSourceCode_SourceFilesOnDisk_AreRead()
	{
		// Only the generated part is embedded in this PDB; the rest is read from the paths it names.
		ApiReference model = Build(SourceFixtureAssembly.Sidecar, options => options.IncludeSourceCode = true);

		string source = Type(model, "Widget").SourceCode!;
		source.Should().Contain("// Widget.g.cs").And.Contain("// Widget.cs").And.Contain("// Widget.Part.cs");
		source.Should().Contain("return Helper() + 1;");
		Type(model, "WidgetKind").SourceCode.Should().Contain("Round");
		Type(model, "Order").SourceCode.Should().Contain("public override string Describe() => \"order\";");
	}

	[Fact]
	public void NoPdb_MeansNoSourceAndNoLink()
	{
		var diagnostics = new DiagnosticBag();
		ApiReference model = Build(SourceFixtureAssembly.NoPdb, options => options.IncludeSourceCode = true, diagnostics);

		model.Namespaces.SelectMany(ns => ns.Types).Should().OnlyContain(t => t.SourceUrl == null && t.SourceCode == null);
		Type(model, "Widget").Documentation!.Summary.Should().Be("A widget.");
		diagnostics.All.Should().BeEmpty();
	}

	[Theory]
	[InlineData("https://raw.githubusercontent.com/o/r/abc/src/A.cs", 7, "https://github.com/o/r/blob/abc/src/A.cs#L7")]
	[InlineData("https://raw.githubusercontent.com/o/r/abc/src/A.cs", null, "https://github.com/o/r/blob/abc/src/A.cs")]
	[InlineData("https://gitlab.example.com/group/sub/r/-/raw/abc/src/A.cs", 3, "https://gitlab.example.com/group/sub/r/-/blob/abc/src/A.cs#L3")]
	[InlineData("https://bitbucket.org/o/r/raw/abc/src/A.cs", 3, "https://bitbucket.org/o/r/raw/abc/src/A.cs")]
	[InlineData("https://dev.azure.com/o/p/_apis/git/repositories/r/items?api-version=1.0&version=abc&path=/src/A.cs", 3,
		"https://dev.azure.com/o/p/_apis/git/repositories/r/items?api-version=1.0&version=abc&path=/src/A.cs")]
	public void ToBrowseUrl_OpensGitHubAndGitLabFilesAndKeepsOtherUrls(string raw, int? line, string expected)
	{
		SourceLinkMap.ToBrowseUrl(raw, line).Should().Be(expected);
	}

	[Fact]
	public void SourceLinkMap_LongestPrefixWins_AndEscapesEachSegment()
	{
		SourceLinkMap map = SourceLinkMap.Parse("""
		                                        {
		                                          "documents": {
		                                            "C:\\src\\*": "https://raw.githubusercontent.com/o/outer/1/*",
		                                            "C:\\src\\lib\\*": "https://raw.githubusercontent.com/o/lib/2/*",
		                                            "C:\\one.cs": "https://example.com/one.cs"
		                                          }
		                                        }
		                                        """)!;

		map.GetUrl(@"C:\src\lib\My File.cs").Should().Be("https://raw.githubusercontent.com/o/lib/2/My%20File.cs");
		map.GetUrl(@"c:\SRC\app\A.cs").Should().Be("https://raw.githubusercontent.com/o/outer/1/app/A.cs");
		map.GetUrl(@"C:\one.cs").Should().Be("https://example.com/one.cs");
		map.GetUrl(@"D:\other\A.cs").Should().BeNull();
		SourceLinkMap.Parse("not json").Should().BeNull();
	}

	[Fact]
	public async Task TypePage_ShowsTheSourcePanelAndTheLink()
	{
		string withSource = await SiteHost.PageAsync(SourceFixtureAssembly.Sidecar, "api/contoso/widgets/widget/index.html",
			options => options.IncludeSourceCode = true);
		string linkOnly = await SiteHost.PageAsync(SourceFixtureAssembly.Sidecar, "api/contoso/widgets/widget/index.html");

		withSource.Should().Contain("<details class=\"source-viewer\">");
		withSource.Should().Contain("return Helper() + 1;");
		string link = $"<a href=\"{_githubBlob}{SourceFixtureAssembly.WidgetPath}#L{_widgetLine}\" target=\"_blank\" rel=\"noopener\">";
		withSource.Should().Contain(link).And.Contain("View source on github.com");
		linkOnly.Should().Contain(link);
		linkOnly.Should().NotContain("source-viewer");
	}

	/// <summary>
	///     Builds sites the way <c>AddMokaDocs</c> and <c>MapMokaDocs</c> do, with a content root in a
	///     temp folder.
	/// </summary>
	internal static class SiteHost
	{
		public static async Task<InMemorySite> BuildAsync(Assembly assembly, Action<MokaDocsOptions>? configure = null,
			Action<string>? prepareContentRoot = null)
		{
			string root = Path.Combine(Path.GetTempPath(), "mokadocs-source-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				prepareContentRoot?.Invoke(root);
				var services = new ServiceCollection();
				services.AddLogging();
				services.AddSingleton<IHostEnvironment>(new ContentRootEnvironment(root));
				services.AddMokaDocs(options =>
				{
					options.Assemblies = [assembly];
					configure?.Invoke(options);
				});

				await using ServiceProvider provider = services.BuildServiceProvider();
				return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

		public static async Task<string> PageAsync(Assembly assembly, string path, Action<MokaDocsOptions>? configure = null)
		{
			InMemorySite site = await BuildAsync(assembly, configure);
			return Html(site, path);
		}

		public static string Html(InMemorySite site, string path)
		{
			site.Files.Keys.Should().Contain(path);
			return Encoding.UTF8.GetString(site.Files[path].Content);
		}

		private sealed class ContentRootEnvironment(string contentRoot) : IHostEnvironment
		{
			public string EnvironmentName { get; set; } = Environments.Production;
			public string ApplicationName { get; set; } = "Moka.Docs.Integration.Tests";
			public string ContentRootPath { get; set; } = contentRoot;
			public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		}
	}
}
