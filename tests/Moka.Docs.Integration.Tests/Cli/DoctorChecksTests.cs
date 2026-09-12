using FluentAssertions;
using Moka.Docs.Cli.Diagnostics;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class DoctorChecksTests
{
	#region FindLinks

	[Fact]
	public void FindLinks_IgnoresExamplesInFencedCodeAndInlineCode()
	{
		// Every one of these examples was reported as a broken link by the regex scanner
		// this replaced, which made doctor fail on the project's own documentation.
		const string md = """
		                  ---
		                  title: Links
		                  ---

		                  See [real](/guide/markdown).

		                  ```markdown
		                  - [fenced](/guide/not-a-link)
		                  ```

		                  Inline `[code](/also/not)` span.
		                  """;

		List<MarkdownLink> links = DoctorChecks.FindLinks(md);

		links.Should().ContainSingle().Which.Url.Should().Be("/guide/markdown");
	}

	[Fact]
	public void FindLinks_ReportsLineNumbersCountingFrontMatter()
	{
		const string md = "---\ntitle: Lines\n---\n\nFirst [a](/a).\n\nSecond [b](/b).";

		List<MarkdownLink> links = DoctorChecks.FindLinks(md);

		links.Select(l => (l.Url, l.Line)).Should().Equal(("/a", 5), ("/b", 7));
	}

	[Fact]
	public void FindLinks_FindsLinksInsideLinkCardsAndMarksImages()
	{
		const string md = ":::link-cards\n- [Card](/getting-started) - desc\n:::\n\n![logo](/icon.png)";

		List<MarkdownLink> links = DoctorChecks.FindLinks(md);

		links.Should().Contain(l => l.Url == "/getting-started" && !l.IsImage);
		links.Should().Contain(l => l.Url == "/icon.png" && l.IsImage);
	}

	#endregion

	#region Routes

	[Theory]
	[InlineData("/guide/markdown", "/guide/markdown")]
	[InlineData("/guide/markdown/", "/guide/markdown")]
	[InlineData("/guide/markdown#section", "/guide/markdown")]
	[InlineData("/guide/markdown?x=1#y", "/guide/markdown")]
	[InlineData("/my%20page", "/my page")]
	[InlineData("/", "/")]
	[InlineData("/#top", "/")]
	public void NormalizeRoute_StripsDecorationsButKeepsTheRoot(string url, string expected) =>
		DoctorChecks.NormalizeRoute(url).Should().Be(expected);

	[Theory]
	[InlineData("/guide", true)]
	[InlineData("//cdn.example.com/x.js", false)]
	[InlineData("https://example.com", false)]
	[InlineData("#anchor", false)]
	[InlineData("relative/page", false)]
	public void IsRootRelative_OnlyAcceptsSiteRootPaths(string url, bool expected) =>
		DoctorChecks.IsRootRelative(url).Should().Be(expected);

	[Fact]
	public void AddSectionAncestors_MakesSectionDirectoriesReachable()
	{
		// The output phase writes a redirect index into section directories without their
		// own index page, so a link to /guide works when only /guide/markdown exists.
		var routes = new HashSet<string> { "/guide/markdown", "/api/moka/docs/core" };

		DoctorChecks.AddSectionAncestors(routes);

		routes.Should().Contain(["/guide", "/api", "/api/moka", "/api/moka/docs"]);
	}

	#endregion

	#region Front Matter

	[Theory]
	[InlineData("---\ntitle: Home\n---\nBody", true)]
	[InlineData("---\r\ntitle: Home\r\n---\r\nBody", true)]
	[InlineData("---\ndescription: x\n---\nBody", false)]
	[InlineData("---\ntitle:\n---\nBody", false)]
	[InlineData("---\ntitle:   \n---\nBody", false)]
	[InlineData("# No front matter", false)]
	[InlineData("---\ntitle: Unclosed", false)]
	public void HasTitle_RequiresANonEmptyTitleInsideClosedFrontMatter(string content, bool expected) =>
		DoctorChecks.HasTitle(content).Should().Be(expected);

	[Fact]
	public void AddTitle_LeavesDollarSequencesInExistingFrontMatterIntact()
	{
		// Regex.Replace treated $0 and $1 in the existing front matter as substitutions and
		// spliced the block into a garbled copy of itself.
		const string content = "---\ndescription: Save $1 today, pay $0 later\norder: 2\n---\n\nBody.\n";

		string result = DoctorChecks.AddTitle(content, "Deal");

		result.Should().Be("---\ntitle: Deal\ndescription: Save $1 today, pay $0 later\norder: 2\n---\n\nBody.\n");
	}

	[Fact]
	public void AddTitle_PreservesCrlfLineEndings()
	{
		const string content = "---\r\norder: 2\r\n---\r\n\r\nBody.\r\n";

		string result = DoctorChecks.AddTitle(content, "Page");

		result.Should().Be("---\r\ntitle: Page\r\norder: 2\r\n---\r\n\r\nBody.\r\n");
		result.Replace("\r\n", "").Should().NotContain("\n");
	}

	[Fact]
	public void AddTitle_PrependsFrontMatterWhenThereIsNone()
	{
		string result = DoctorChecks.AddTitle("# Heading\n", "Page");

		result.Should().Be("---\ntitle: Page\n---\n\n# Heading\n");
	}

	[Fact]
	public void AddTitle_QuotesTitlesThatWouldBreakYaml()
	{
		string result = DoctorChecks.AddTitle("Body\n", "Api: v2");

		result.Should().StartWith("---\ntitle: \"Api: v2\"\n");
	}

	[Theory]
	[InlineData("Plain Title", "Plain Title")]
	[InlineData("- starts with a dash", "\"- starts with a dash\"")]
	[InlineData("null", "\"null\"")]
	[InlineData("~", "\"~\"")]
	[InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
	public void QuoteYamlScalar_QuotesOnlyWhenBareYamlWouldChangeTheValue(string value, string expected) =>
		DoctorChecks.QuoteYamlScalar(value).Should().Be(expected);

	[Fact]
	public void TitleFromPath_UsesTheFolderNameForIndexPages()
	{
		string docs = Path.Combine(Path.GetTempPath(), "doctor-titles", "docs");

		DoctorChecks.TitleFromPath(Path.Combine(docs, "getting-started", "index.md"), docs)
			.Should().Be("Getting Started");
		DoctorChecks.TitleFromPath(Path.Combine(docs, "index.md"), docs).Should().Be("Home");
		DoctorChecks.TitleFromPath(Path.Combine(docs, "api_overview.md"), docs).Should().Be("Api Overview");
	}

	#endregion

	#region Plugins

	[Fact]
	public void FindUnknownPlugins_AcceptsRealIdsAndRejectsTheOldShortNames()
	{
		// The hardcoded list doctor used approved "repl", which PluginHost never loads, and
		// rejected "mokadocs-repl", which it does.
		string[] registered = ["mokadocs-repl", "mokadocs-blazor-preview", "openapi"];
		PluginDeclaration[] declared =
		[
			new() { Name = "mokadocs-repl" },
			new() { Name = "MOKADOCS-BLAZOR-PREVIEW" },
			new() { Name = "repl" },
			new() { Name = "   " }
		];

		DoctorChecks.FindUnknownPlugins(declared, registered).Should().Equal("repl");
	}

	[Fact]
	public void FindIgnoredPluginDeclarations_ReportsPathsAndNamelessEntries()
	{
		// The plugin host skips both without a trace, so doctor and validate used to pass
		// a config whose plugin never loaded.
		PluginDeclaration[] declared =
		[
			new() { Name = "mokadocs-repl" },
			new() { Path = "./plugins/Footer.dll" },
			new() { Options = new Dictionary<string, object> { ["x"] = 1 } }
		];

		DoctorChecks.FindIgnoredPluginDeclarations(declared).Should().Equal(
			"plugins[1]: path './plugins/Footer.dll' is ignored, the mokadocs CLI cannot load plugin assemblies",
			"plugins[2] has no name");
	}

	#endregion

	#region API Coverage

	[Fact]
	public void ComputeApiCoverage_CountsInheritDocTagsAsDocumented()
	{
		// An <inheritdoc/> whose base is a framework type never resolves, because the
		// framework is not in the model. The author still documented it.
		var api = new ApiReference
		{
			Namespaces =
			[
				new ApiNamespace
				{
					Name = "Ns",
					Types =
					[
						new ApiType
						{
							Name = "Widget",
							FullName = "Ns.Widget",
							Kind = ApiTypeKind.Class,
							Documentation = new XmlDocBlock { Summary = "A widget." },
							Members =
							[
								Member("Documented", new XmlDocBlock { Summary = "Has one." }),
								Member("ToString", new XmlDocBlock { HasInheritDocTag = true }),
								Member("EmptyComment", new XmlDocBlock()),
								Member("NoComment", null)
							]
						}
					]
				}
			]
		};

		ApiCoverage coverage = DoctorChecks.ComputeApiCoverage(api);

		coverage.Total.Should().Be(5);
		coverage.Documented.Should().Be(3);
		coverage.Missing.Should().Equal("Ns.Widget.EmptyComment", "Ns.Widget.NoComment");
		coverage.Percent.Should().Be(60);
	}

	private static ApiMember Member(string name, XmlDocBlock? doc) => new()
	{
		Name = name,
		Kind = ApiMemberKind.Method,
		Signature = $"void {name}()",
		Documentation = doc
	};

	#endregion

	#region Paths

	[Fact]
	public void IsUnder_DoesNotMatchSiblingsThatShareAPrefix()
	{
		string root = Path.Combine(Path.GetTempPath(), "doctor-paths");
		string site = Path.Combine(root, "docs", "_site");

		DoctorChecks.IsUnder(Path.Combine(site, "icon.png"), site).Should().BeTrue();
		DoctorChecks.IsUnder(Path.Combine(root, "docs", "_sitemap", "notes.md"), site).Should().BeFalse();
		DoctorChecks.IsUnder(Path.Combine(root, "docs", "icon.png"), site).Should().BeFalse();
	}

	#endregion
}
