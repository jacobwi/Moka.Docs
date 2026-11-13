using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     Whole builds in another language: every page MokaDocs writes carries the language and direction, the
///     parser's and the theme's text is translated, and main.js changes its version with the text.
/// </summary>
public sealed partial class LanguageSiteTests
{
	private const string _site = MockSite.Root + "/_site";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		[MockSite.Root + "/docs/index.md"] = new MockFileData("---\ntitle: Home\nlayout: landing\n---\n\nWelcome.\n"),
		[MockSite.Root + "/docs/guide/intro.md"] = new MockFileData(
			"---\ntitle: Intro\n---\n\n# Intro\n\n## One\n\n:::note\nA note.\n:::\n\n## Two\n\n```csharp\nvar x = 1;\n```\n\n" +
			":::details\nHidden.\n:::\n\n:::file-tree\n- src/\n  - app.cs\n:::\n"),
		[MockSite.Root + "/docs/guide/deeper/page.md"] = new MockFileData("---\ntitle: Deeper\n---\n\n# Deeper\n")
	};

	private static SiteConfig Config(string language, string direction = "", Dictionary<string, string>? strings = null)
	{
		SiteConfig config = MockSite.Config();
		return config with
		{
			Site = config.Site with { Language = language, Direction = direction },
			Theme = new ThemeConfig { Options = new ThemeOptions { Strings = strings ?? [] } }
		};
	}

	private static IEnumerable<string> WrittenPages() =>
		[_site + "/index.html", _site + "/guide/intro/index.html", _site + "/404.html", _site + "/guide/index.html", _site + "/guide/deeper/index.html"];

	[Fact]
	public async Task German_TranslatesTheThemeAndTheParserAndSetsTheLanguageEverywhere()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config("de"), Files());

		context.Diagnostics.All.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
		foreach (string path in WrittenPages())
		{
			string html = fs.File.ReadAllText(path);
			HtmlStart().Match(html).Value.Should().Contain("lang=\"de\" dir=\"ltr\"", path);
		}

		ThemeText text = ThemeText.For("de");
		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		page.Should().Contain(Encoded(text["toc.title"]))
			.And.Contain("<span class=\"admonition-icon\">📝</span>" + Encoded(text["admonition.note"]))
			.And.Contain("<summary>" + Encoded(text["details.summary"]) + "</summary>")
			.And.Contain("aria-label=\"" + Encoded(text.Format("code.label", ("language", "C#"))) + "\"")
			.And.Contain("data-no-search>" + Encoded(text["fileTree.folder"]) + "</span>");
		fs.File.ReadAllText(_site + "/guide/index.html").Should().Contain(Encoded(text["redirect.section"]).Split('{')[0]);
		fs.File.ReadAllText(_site + "/404.html").Should().Contain("<h1>" + Encoded(text["notFound.title"]) + "</h1>");
		fs.File.ReadAllText(_site + "/search-index.json").Should().Contain(text["search.categoryDocs"]).And.NotContain("\"Documentation\"");
	}

	[Fact]
	public async Task Arabic_IsRightToLeftEverywhereAndLoadsTheRtlStylesheetLast()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("ar"), Files());

		foreach (string path in WrittenPages())
		{
			HtmlStart().Match(fs.File.ReadAllText(path)).Value.Should().Contain("lang=\"ar\" dir=\"rtl\"", path);
		}

		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		List<string> stylesheets = [.. Stylesheet().Matches(page).Select(m => m.Groups[1].Value)];
		stylesheets.Last().Should().StartWith("/_theme/css/rtl.css?v=");
		fs.File.ReadAllText(_site + "/_theme/css/rtl.css").Should().Contain("[dir=\"rtl\"]");
	}

	[Fact]
	public async Task English_StaysLeftToRightWithoutTheRtlStylesheet()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("en"), Files());

		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		HtmlStart().Match(page).Value.Should().Contain("lang=\"en\" dir=\"ltr\"");
		page.Should().NotContain("rtl.css").And.NotContain("og:locale");
		page.Should().Contain(">On this page<");
	}

	[Fact]
	public async Task Direction_CanTurnARightToLeftLanguageAround()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("he", "ltr"), Files());

		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		HtmlStart().Match(page).Value.Should().Contain("lang=\"he\" dir=\"ltr\"");
		page.Should().NotContain("rtl.css");
	}

	[Fact]
	public async Task MainScript_VersionChangesWithTheText()
	{
		string english = await ScriptVersion(Config("en"));

		(await ScriptVersion(Config("en"))).Should().Be(english, "the same text gives the same file");
		(await ScriptVersion(Config("de"))).Should().NotBe(english);
		(await ScriptVersion(Config("en", strings: new Dictionary<string, string> { ["search.prompt"] = "Start typing." }))).Should().NotBe(english);
		(await ScriptVersion(Config("en", strings: new Dictionary<string, string> { ["toc.title"] = "Contents" }))).Should().Be(english,
			"a text main.js doesn't show leaves it alone");
	}

	[Fact]
	public async Task Overrides_ReachThePagesAndUnknownKeysWarn()
	{
		var strings = new Dictionary<string, string> { ["toc.title"] = "Contents <&>", ["toc.Title"] = "x", ["no.such"] = "y" };

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config("de", strings: strings), Files());

		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		page.Should().Contain("id=\"tocTitle\">Contents &lt;&amp;&gt;</p>");
		List<string> warnings = [.. context.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.Message)];
		warnings.Should().Contain(w => w.Contains("'toc.Title'") && w.Contains("did you mean 'toc.title'?"));
		warnings.Should().Contain(w => w.Contains("'no.such'"));
	}

	[Theory]
	[InlineData("deutsch!", "", "Site language 'deutsch!'")]
	[InlineData("de", "sideways", "Site direction 'sideways'")]
	public async Task MalformedLanguageOrDirection_Warns(string language, string direction, string warning)
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(language, direction), Files());

		context.Diagnostics.All.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning && d.Message.StartsWith(warning, StringComparison.Ordinal));
		HtmlStart().Match(fs.File.ReadAllText(_site + "/guide/intro/index.html")).Value.Should().Contain("dir=\"ltr\"");
	}

	// The table of text keys in docs/guide/languages.md is the catalog, row for row.
	[Fact]
	public void LanguagesPage_ListsEveryKey()
	{
		string docs = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "guide", "languages.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
		int start = docs.IndexOf("| Key | English | Where it shows |", StringComparison.Ordinal);
		start.Should().BeGreaterThan(0, "the page has the table of text keys");
		string table = string.Join("\n", docs[start..].Split('\n').TakeWhile(line => line.StartsWith('|')));

		var expected = new StringBuilder("| Key | English | Where it shows |\n|---|---|---|");
		foreach (ThemeStringInfo entry in ThemeStrings.All)
		{
			expected.Append($"\n| `{entry.Key}` | `{entry.English}` | {Cell(entry.Description)} |");
		}

		table.Should().Be(expected.ToString(), "docs/guide/languages.md should list the catalog (copy the expected table there)");
	}

	// A brace is escaped so Markdown doesn't read {name} as attributes.
	private static string Cell(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).Replace("{", "\\{", StringComparison.Ordinal);

	private static string Encoded(string text) => WebUtility.HtmlEncode(text);

	private static async Task<string> ScriptVersion(SiteConfig config)
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());
		string page = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		Match script = Regex.Match(page, "<script src=\"/_theme/js/main\\.js\\?v=([0-9a-f]{10})\"");
		script.Success.Should().BeTrue();
		Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(fs.File.ReadAllBytes(_site + "/_theme/js/main.js")))[..10]
			.Should().Be(script.Groups[1].Value, "the version is the hash of the file written");
		return script.Groups[1].Value;
	}

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "MokaDocs.slnx")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
	}

	[GeneratedRegex("<html[^>]*>")]
	private static partial Regex HtmlStart();

	[GeneratedRegex("<link rel=\"stylesheet\" href=\"([^\"]+)\"")]
	private static partial Regex Stylesheet();
}
