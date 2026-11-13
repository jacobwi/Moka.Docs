using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The theme's text: every key the layouts and main.js use is in the catalog, main.js carries the site's
///     text safely, and the right-to-left stylesheet only touches right-to-left pages.
/// </summary>
public sealed partial class EmbeddedThemeTextTests
{
	private static readonly Dictionary<string, string> _versions = EmbeddedThemeProvider.StyleFileNames.ToDictionary(s => s, _ => "0123456789");

	private static IEnumerable<string> Templates() =>
		[.. EmbeddedThemeProvider.CreateDefault().Templates.Values, EmbeddedThemeProvider.GetSidebarTemplate()];

	[Fact]
	public void Templates_ReadOnlyTextsTheCatalogHas()
	{
		var variables = ThemeStrings.Keys.ToDictionary(ScribanTemplateEngine.TextVariableName, k => k);
		var used = Templates().SelectMany(t => TemplateText().Matches(t).Select(m => m.Groups[1].Value)).ToHashSet();

		used.Should().HaveCountGreaterThan(70, "the layouts read their text from t");
		used.Where(v => !variables.ContainsKey(v)).Should().BeEmpty("every t.* variable is a key of the catalog");
	}

	[Fact]
	public void Templates_WriteNoEnglishTheCatalogOwns()
	{
		string all = string.Join("\n", Templates());

		foreach (string english in (string[])["Skip to content", "On this page", "Search documentation", "Was this page helpful?",
			         "Previous and next pages", "Reset to site defaults", "Toggle navigation", "Built with", "Get Started", "Copy page",
			         "lang=\"en\"", "aria-label=\"Search\"", "Dark mode", "Close search", "Type to search."])
		{
			all.Should().NotContain(english);
		}
	}

	[Fact]
	public void TextVariableNames_AreUniqueSnakeCase()
	{
		List<string> names = [.. ThemeStrings.Keys.Select(ScribanTemplateEngine.TextVariableName)];

		names.Should().OnlyHaveUniqueItems().And.AllSatisfy(n => SnakeCase().IsMatch(n).Should().BeTrue(n));
		ScribanTemplateEngine.TextVariableName("toc.title").Should().Be("toc_title");
		ScribanTemplateEngine.TextVariableName("a11y.skipToContent").Should().Be("a11y_skip_to_content");
		ScribanTemplateEngine.TextVariableName("color.moka-red").Should().Be("color_moka_red");
	}

	[Fact]
	public void Script_UsesOnlyTextsTheCatalogHas()
	{
		string js = EmbeddedThemeProvider.GetJs();
		List<string> called = [.. ScriptCall().Matches(js).Select(m => m.Groups[2].Value)];

		called.Should().NotBeEmpty();
		foreach (string key in called)
		{
			(ThemeStrings.Find(key) is not null || ThemeStrings.Find(key + ".other") is not null).Should().BeTrue($"main.js uses '{key}'");
		}

		EmbeddedThemeProvider.ScriptStringKeys.Should().BeSubsetOf(ThemeStrings.Keys)
			.And.Contain(["search.noResults", "search.resultCount.one", "search.resultCount.other", "toc.position.other", "code.copy"]);
		js.Should().NotContain("/*theme-strings*/").And.NotContain("/*appearance-options*/").And.NotContain("/*style-files*/");
	}

	[Fact]
	public void Script_CarriesTheSiteTextAndTheAppearanceNames()
	{
		string js = EmbeddedThemeProvider.GetJs(_versions, ThemeText.For("de"));

		Dictionary<string, string> strings = Strings(js);
		strings.Keys.Should().BeEquivalentTo(EmbeddedThemeProvider.ScriptStringKeys);
		strings["code.copy"].Should().Be(ThemeText.For("de")["code.copy"]).And.NotBe("Copy");
		strings["search.shortcut"].Should().Be("Strg K");

		JsonElement options = JsonElement("const appearanceOptions = ", js);
		options.GetProperty("style")[3].GetProperty("label").GetString().Should().Be(ThemeText.For("de")["style.classic"]);
		options.GetProperty("layout")[0].GetProperty("label").GetString().Should().Be(ThemeText.For("de")["layout.navbar"]);
		options.GetProperty("code-theme")[0].GetProperty("label").GetString().Should().Be(CodeThemes.All[0].Label, "code themes keep their names");
	}

	[Fact]
	public void Script_DiffersWithTheText()
	{
		string english = EmbeddedThemeProvider.GetJs(_versions, ThemeText.English);

		EmbeddedThemeProvider.GetJs(_versions, ThemeText.For("en")).Should().Be(english);
		EmbeddedThemeProvider.GetJs(_versions, ThemeText.For("ja")).Should().NotBe(english);
		EmbeddedThemeProvider.GetJs(_versions, ThemeText.For("en", new Dictionary<string, string> { ["code.copy"] = "Grab" }))
			.Should().NotBe(english);
		EmbeddedThemeProvider.GetJs(_versions).Should().Be(english, "the overload without text is English");
	}

	// A site's text is data in main.js: quotes, </script>, template braces, line separators and the build's own
	// placeholders stay inside their string.
	[Fact]
	public void Script_KeepsHostileTextInsideItsString()
	{
		const string hostile = "\"'</script><b>{{ site.title }}\u2028/*style-files*/{}/*theme-strings*/{} {query}";
		ThemeText text = ThemeText.For("en", new Dictionary<string, string> { ["search.noResults"] = hostile, ["style.moka"] = hostile });

		string js = EmbeddedThemeProvider.GetJs(_versions, text);

		Strings(js)["search.noResults"].Should().Be(hostile);
		JsonElement("const appearanceOptions = ", js).GetProperty("style")[1].GetProperty("label").GetString().Should().Be(hostile);
		JsonElement("const styleFiles = ", js).GetProperty("moka").GetString().Should().Be("0123456789");
		js.Should().NotContain("</script>").And.NotContain("\u2028");
		if (NodeScript.Available)
		{
			NodeScript.Run(js[js.IndexOf("const strings = ", StringComparison.Ordinal)..js.IndexOf("function closest(", StringComparison.Ordinal)] +
			               "process.stdout.write(i18n('search.noResults', { query: 'x{query}' }));")
				.Should().Be(hostile.Replace("{query}", "x{query}", StringComparison.Ordinal));
		}
	}

	[Theory]
	[InlineData("fr", "0 résultat|1 résultat|2 résultats|5 résultats")]
	[InlineData("ja", "0件の結果|1件の結果|2件の結果|5件の結果")]
	[InlineData("pl", "0 wyników|1 wynik|2 wyniki|5 wyników")]
	public void CountedText_FollowsThePageLanguage(string language, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");
		// Polish has no built-in translation; a site adds its forms, the few form included.
		ThemeText text = ThemeText.For(language, language != "pl"
			? null
			: new Dictionary<string, string>
			{
				["search.resultCount.one"] = "{count} wynik",
				["search.resultCount.few"] = "{count} wyniki",
				["search.resultCount.other"] = "{count} wyników",
				["search.resultCount.many"] = "{count} wyników"
			});
		string js = EmbeddedThemeProvider.GetJs(_versions, text);
		string helpers = js[js.IndexOf("const strings = ", StringComparison.Ordinal)..js.IndexOf("function closest(", StringComparison.Ordinal)];

		string result = NodeScript.Run($$"""
		                                 const html = { getAttribute: () => '{{language}}' };
		                                 {{helpers}}
		                                 process.stdout.write([0, 1, 2, 5].map(n => i18nCount('search.resultCount', n)).join('|'));
		                                 """);

		result.Should().Be(expected);
	}

	// Words in Chinese and Japanese text aren't separated by spaces, so a query is often one long term. It matches
	// as written, and else where at least two thirds of its character pairs appear, below every exact match.
	[Theory]
	[InlineData("検索設定", "/search-settings:1|/setting-search:1")]
	[InlineData("検索の設定", "/search-settings:1:0")]
	[InlineData("插件配置", "/plugins:1")]
	[InlineData("天気予報", "")]
	[InlineData("設定", "/search-settings:1:0|/setting-search:1:0")]
	public void Search_MatchesChineseAndJapaneseByCharacterPairs(string query, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");
		string js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n", StringComparison.Ordinal);
		string search = js[js.IndexOf("function prepareSearchIndex(", StringComparison.Ordinal)..js.IndexOf("function initSearch(", StringComparison.Ordinal)];

		string result = NodeScript.Run($$"""
		                                 {{search}}
		                                 const index = prepareSearchIndex([
		                                     { t: 'Search', s: '', r: '/search-settings', c: '検索の設定について説明します。', g: 'Docs' },
		                                     { t: 'Settings', s: '', r: '/setting-search', c: '設定を検索する方法。', g: 'Docs' },
		                                     { t: 'Plugins', s: '', r: '/plugins', c: '如何配置插件。', g: 'Docs' }
		                                 ]);
		                                 process.stdout.write(searchIndex(index, {{JsonSerializer.Serialize(query)}}).map(r => r.entry.r + ':' + r.score + (r.typos ? '' : ':0')).join('|'));
		                                 """);

		result.Should().Be(expected);
	}

	[Fact]
	public void RtlStylesheet_TouchesOnlyRightToLeftPages()
	{
		string rtl = EmbeddedThemeProvider.GetRtlCss();
		List<string> selectors = Selectors(rtl);

		selectors.Should().HaveCountGreaterThan(100);
		selectors.Should().AllSatisfy(s => s.Should().Contain("[dir=\"rtl\"]"));
		foreach (string css in (string[])[EmbeddedThemeProvider.GetCss(), EmbeddedThemeProvider.GetLayoutsCss(), .. EmbeddedThemeProvider.StyleFileNames.Select(EmbeddedThemeProvider.GetStyleCss)])
		{
			css.Should().NotContain("[dir=", "left-to-right pages keep exactly the rules they had");
		}
	}

	[Fact]
	public void RtlStylesheet_IsLinkedLastAndOnlyForRightToLeft()
	{
		var options = new ThemeOptions { Style = "classic", Layout = new ThemeLayoutOptions { Navbar = "tabs" } };

		EmbeddedThemeProvider.CssFilesFor(options).Should().NotContain(EmbeddedThemeProvider.RtlCssPath);
		EmbeddedThemeProvider.CssFilesFor(options, "ltr").Should().Equal(EmbeddedThemeProvider.CssFilesFor(options));
		EmbeddedThemeProvider.CssFilesFor(options, "rtl").Should().Equal([.. EmbeddedThemeProvider.CssFilesFor(options), EmbeddedThemeProvider.RtlCssPath]);
	}

	[Fact]
	public void RtlStylesheet_MirrorsTheStyles()
	{
		string rtl = EmbeddedThemeProvider.GetRtlCss();

		rtl.Should().Contain("html:where([dir=\"rtl\"])[data-style=\"classic\"] .nav-link { padding: 0.3125rem 1.25rem !important; " +
		                     "border-right: 3px solid transparent !important; border-left: none !important; }");
		rtl.Should().Contain("html:where([dir=\"rtl\"])[data-style=\"brutal\"] .sidebar { border-left: 3px solid var(--bt-ink); border-right: none; }");
		rtl.Should().NotContain("pre .line-numbers", "code keeps its left-to-right layout");
	}

	[Theory]
	[InlineData(".a { border-left: 3px solid red; }", ":where(html[dir=\"rtl\"]) .a { border-right: 3px solid red; border-left: none; }")]
	[InlineData(".a { padding-left: 1rem; padding-right: 2rem; }", ":where(html[dir=\"rtl\"]) .a { padding-right: 1rem; padding-left: 2rem; }")]
	[InlineData(".a { left: 0; text-align: left; }", ":where(html[dir=\"rtl\"]) .a { right: 0; left: auto; text-align: right; }")]
	[InlineData(".a { border-radius: 1px 2px 3px 4px; }", ":where(html[dir=\"rtl\"]) .a { border-radius: 2px 1px 4px 3px; }")]
	[InlineData(".a { padding: 0 1rem 0 2rem; }", ":where(html[dir=\"rtl\"]) .a { padding: 0 2rem 0 1rem; }")]
	[InlineData(".a::before { transform: translateY(-1px) rotate(45deg); }", ":where(html[dir=\"rtl\"]) .a::before { transform: translateY(-1px) rotate(-45deg); }")]
	[InlineData(".a { box-shadow: inset 3px 0 0 red; }", ":where(html[dir=\"rtl\"]) .a { box-shadow: inset -3px 0 0 red; }")]
	[InlineData("html[data-x=\"y\"] .a { margin-left: 2px !important; }", "html:where([dir=\"rtl\"])[data-x=\"y\"] .a { margin-right: 2px !important; margin-left: 0 !important; }")]
	[InlineData(".page-content .admonition { border: 1px solid blue; border-left: 4px solid red; }",
		":where(html[dir=\"rtl\"]) .page-content .admonition { border: 1px solid blue; border-right: 4px solid red; border-left: 1px solid blue; }")]
	[InlineData(".component-card-info { border-left-width: 3px; }", ":where(html[dir=\"rtl\"]) .component-card-info { border-right-width: 3px; border-left-width: 1px; }")]
	[InlineData("@media (max-width: 9px) { .a { right: 1px; } }", "@media (max-width: 9px) { :where(html[dir=\"rtl\"]) .a { left: 1px; right: auto; } }")]
	[InlineData(".a { color: red; }", "")]
	[InlineData(".page-content pre .copy-btn { right: 0; }", "")]
	public void RtlMirror_TurnsRulesAround(string css, string expected)
	{
		RtlMirror.Mirror(css).Trim().Should().Be(expected);
	}

	private static Dictionary<string, string> Strings(string js) =>
		JsonElement("const strings = ", js).Deserialize<Dictionary<string, string>>()!;

	private static JsonElement JsonElement(string declaration, string js)
	{
		js = js.Replace("\r\n", "\n", StringComparison.Ordinal);
		int start = js.IndexOf(declaration, StringComparison.Ordinal) + declaration.Length;
		int end = js.IndexOf(";\n", start, StringComparison.Ordinal);
		return JsonDocument.Parse(js[start..end]).RootElement.Clone();
	}

	private static List<string> Selectors(string css)
	{
		string text = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
		var selectors = new List<string>();
		foreach (Match rule in RuleHead().Matches(text))
		{
			string head = rule.Groups[1].Value.Trim();
			if (head.StartsWith('@') || head.Length == 0)
			{
				continue;
			}

			int depth = 0;
			int start = 0;
			for (int i = 0; i <= head.Length; i++)
			{
				if (i == head.Length || (head[i] == ',' && depth == 0))
				{
					selectors.Add(head[start..i].Trim());
					start = i + 1;
				}
				else if (head[i] is '(' or '[')
				{
					depth++;
				}
				else if (head[i] is ')' or ']')
				{
					depth--;
				}
			}
		}

		return selectors;
	}

	// t.toc_title, not the inline scripts' t.parentElement.
	[GeneratedRegex(@"\bt\.([a-z0-9_]+)(?![A-Za-z0-9_])")]
	private static partial Regex TemplateText();

	[GeneratedRegex(@"^[a-z0-9]+(_[a-z0-9]+)*$")]
	private static partial Regex SnakeCase();

	[GeneratedRegex(@"\bi18n(Count)?\('([\w.]+)'")]
	private static partial Regex ScriptCall();

	[GeneratedRegex(@"(?<=^|[{};])\s*([^{};]+)\{")]
	private static partial Regex RuleHead();
}
