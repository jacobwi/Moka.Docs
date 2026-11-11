using FluentAssertions;
using Moka.Docs.Cli.Diagnostics;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Plugins;

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
	public void FindIgnoredPluginDeclarations_ReportsNamelessEntries()
	{
		// The plugin host skips them without a trace, so doctor and validate used to pass a config
		// whose plugin never loaded. A path with a name is loaded now, so only the nameless one counts.
		PluginDeclaration[] declared =
		[
			new() { Name = "mokadocs-repl" },
			new() { Path = "./plugins/Footer.dll" },
			new() { Options = new Dictionary<string, object> { ["x"] = 1 } },
			new() { Name = "mokadocs-footer", Path = "./plugins/Footer.dll" }
		];

		DoctorChecks.FindIgnoredPluginDeclarations(declared).Should().Equal(
			"plugins[1] has no name, so nothing is loaded from './plugins/Footer.dll'. Set it to the plugin's id",
			"plugins[2] has no name");
	}

	[Fact]
	public void FindUnknownPlugins_LeavesPathDeclarationsToTheirAssembly()
	{
		// A plugin from a path has an id the CLI doesn't register; the build reports when it doesn't load.
		PluginDeclaration[] declared =
		[
			new() { Name = "mokadocs-footer", Path = "./plugins/Footer.dll" },
			new() { Name = "mokadocs-footer" }
		];

		DoctorChecks.FindUnknownPlugins(declared, ["openapi"]).Should().Equal("mokadocs-footer");
	}

	[Fact]
	public void FindPluginsThatFailedToInitialize_SkipsPathsThatFailedToLoadAndUnknownNames()
	{
		PluginDeclaration loaded = new() { Name = "openapi" };
		PluginDeclaration initThrew = new() { Name = "mokadocs-changelog" };
		PluginDeclaration unknown = new() { Name = "nope" };
		PluginDeclaration pathInitThrew = new() { Name = "mokadocs-a", Path = "./a.dll" };
		PluginDeclaration pathFailed = new() { Name = "mokadocs-b", Path = "./b.dll" };

		DoctorChecks.FindPluginsThatFailedToInitialize(
				[loaded, initThrew, unknown, pathInitThrew, pathFailed],
				["openapi", "mokadocs-changelog"],
				["openapi"],
				[new PluginLoadFailure("mokadocs-b", "./b.dll", "missing")])
			.Should().Equal(initThrew, pathInitThrew);
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

	#region CheckLinks

	private static DocPage LinkPage(string route, string? source, string html) => new()
	{
		FrontMatter = new FrontMatter { Title = route },
		Content = new PageContent { Html = html, PlainText = "" },
		Route = route,
		SourcePath = source
	};

	private static readonly DocPage[] _linkPages =
	[
		LinkPage("/guide/intro", "guide/intro.md",
			"""<h2 id="setup">Setup</h2><h2 id='usage'>Usage</h2><a name="legacy"></a><h3 id="a&amp;b">A</h3>"""),
		LinkPage("/Reference", "Reference.md", """<h2 id="types">Types</h2>""")
	];

	private static LinkCheckResult Check(string markdown, string pathInDocs = "guide/intro.md") =>
		DoctorChecks.CheckLinks(_linkPages, ["/assets/logo.png"],
			[new MarkdownSource("docs/" + pathInDocs, pathInDocs, markdown)]);

	[Theory]
	[InlineData("[x](/guide/Intro)", "/guide/intro")]
	[InlineData("[x](/reference)", "/Reference")]
	[InlineData("![x](/assets/Logo.png)", "/assets/logo.png")]
	[InlineData("[x](../reference.md)", "/Reference")]
	public void CheckLinks_ALinkThatOnlyMatchesIgnoringCase_IsBrokenAndSaysSo(string markdown, string actual)
	{
		// Linux hosts serve /guide/Intro and /guide/intro as different paths. The case-insensitive
		// comparison passed these links, which 404 once deployed.
		Check(markdown).Broken.Should().ContainSingle()
			.Which.Should().Contain($"differs only in case from {actual}");
	}

	[Theory]
	[InlineData("[x](/guide/intro)")]
	[InlineData("[x](/Reference/)")]
	[InlineData("[x](/guide)")]
	[InlineData("![x](/assets/logo.png)")]
	[InlineData("[x](https://example.com/Guide)")]
	public void CheckLinks_ExactTargets_AreFine(string markdown) =>
		Check(markdown).Broken.Should().BeEmpty();

	[Theory]
	[InlineData("[x](/guide/intro#setup)")]
	[InlineData("[x](/guide/intro#usage)")]
	[InlineData("[x](/guide/intro#legacy)")]
	[InlineData("[x](/guide/intro#a%26b)")]
	[InlineData("[x](#setup)")]
	[InlineData("[x](#top)")]
	[InlineData("[x](intro.md#setup)")]
	[InlineData("[x](/Reference#types)")]
	public void CheckLinks_AnchorsThatExist_AreFine(string markdown)
	{
		LinkCheckResult result = Check(markdown);

		result.Broken.Should().BeEmpty();
		result.BrokenAnchors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("[x](/guide/intro#install)", "/guide/intro#install")]
	[InlineData("[x](#Setup)", "/guide/intro#Setup")]
	[InlineData("[x](/Reference#methods)", "/Reference#methods")]
	public void CheckLinks_AnchorsThatDoNotExist_AreReported(string markdown, string reported)
	{
		LinkCheckResult result = Check(markdown);

		result.Broken.Should().BeEmpty();
		result.BrokenAnchors.Should().ContainSingle()
			.Which.Should().StartWith(reported).And.Contain("docs/guide/intro.md:1");
	}

	[Fact]
	public void ElementIds_ReadsDoubleAndSingleQuotedIdsAndAnchorNames()
	{
		DoctorChecks.ElementIds("""<div class="x" id="one"><a name='two'></a><p data-id="no">id="no"</p></div>""")
			.Should().BeEquivalentTo("one", "two");
	}

	#endregion
}
