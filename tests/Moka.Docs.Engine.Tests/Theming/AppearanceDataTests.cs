using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The Appearance panel and the head script take their options from the appearance lists in
///     <c>Moka.Docs.Core.Theming</c>: every style, color preset, code theme and code style, the
///     dark-first styles and the light/dark code theme pairs. Each section shows only while its
///     <c>theme.options</c> flag is on.
/// </summary>
public sealed class AppearanceDataTests
{
	private static readonly string[] _sections = ["mode", "style", "color-theme", "code-theme", "code-style", "text-size", "reader-width"];

	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	private static readonly ThemeOptions _allPickers = OnlySections(_sections);

	public static TheoryData<string> Layouts() => ["default", "landing"];

	public static TheoryData<string> Sections() => [.. _sections];

	[Theory]
	[MemberData(nameof(Layouts))]
	public void Panel_ListsTheFixedOptionsAndLeavesTheLongListsToTheScript(string layout)
	{
		// The style, color, code theme and code style lists were most of the panel on every page. main.js builds
		// them when the panel first opens (EmbeddedThemeRuntimeTests); the page keeps the group and its label.
		string html = Render(_allPickers, layout);

		Options(html, "mode").Should().Equal("light", "dark", "system");
		Options(html, "text-size").Should().Equal("90", "100", "112.5", "125");
		Options(html, "reader-width").Should().Equal("narrow", "default", "wide", "wider", "full");
		foreach (string section in new[] { "style", "color-theme", "code-style" })
		{
			Group(html, section).Trim().Should().BeEmpty();
		}

		Group(html, "code-theme").Trim().Should().Be("<span class=\"appearance-group-label\">Dark</span>\n<span class=\"appearance-group-label\">Light</span>");
		html.Should().NotContain("style-preview").And.NotContain("appearance-swatch-dot").And.NotContain("appearance-code-preview");
	}

	[Fact]
	public void MainScript_CarriesEveryOptionWithItsLabel()
	{
		string js = EmbeddedThemeProvider.GetJs();

		using JsonDocument options = JsonDocument.Parse(Regex.Match(js, @"const appearanceOptions = (\{[^\n]*\});").Groups[1].Value);
		JsonElement root = options.RootElement;
		root.GetProperty("style").EnumerateArray().Select(o => (o.GetProperty("name").GetString(), o.GetProperty("label").GetString(), o.GetProperty("description").GetString()))
			.Should().Equal(ThemeStyles.All.Select(s => ((string?)s.Name, (string?)s.Label, (string?)s.Description)));
		root.GetProperty("color-theme").EnumerateArray().Select(o => (o.GetProperty("name").GetString(), o.GetProperty("label").GetString(), o.GetProperty("swatch").GetString()))
			.Should().Equal(ColorPresets.All.Select(p => ((string?)p.Name, (string?)p.Label, (string?)p.Swatch)));
		root.GetProperty("code-theme").EnumerateArray().Select(o => (o.GetProperty("name").GetString(), o.GetProperty("label").GetString(), o.GetProperty("background").GetString(), o.GetProperty("dark").GetBoolean()))
			.Should().Equal(CodeThemes.All.Select(t => ((string?)t.Name, (string?)t.Label, (string?)t.Background, t.IsDark)));
		root.GetProperty("code-style").EnumerateArray().Select(o => (o.GetProperty("name").GetString(), o.GetProperty("label").GetString()))
			.Should().Equal(CodeStyles.All.Select(s => ((string?)s.Name, (string?)s.Label)));
		js.Should().NotContain("/*appearance-options*/").And.NotContain("/*style-files*/");
	}

	[Theory]
	[MemberData(nameof(Sections))]
	public void Panel_ShowsASectionOnlyWhileItsOptionIsOn(string section)
	{
		string alone = Render(OnlySections([section]));
		string without = Render(OnlySections([.. _sections.Where(s => s != section)]));

		RenderedSections(alone).Should().Equal(section);
		RenderedSections(without).Should().Equal(_sections.Where(s => s != section));
		alone.Should().Contain("<button class=\"appearance-trigger\"");
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public void Panel_WithEverySectionOff_RendersNoButton(string layout)
	{
		string html = Render(OnlySections([]), layout);

		html.Should().NotContain("appearance-trigger").And.NotContain("appearancePanel").And.NotContain("role=\"radio\"");
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public void Panel_IsAScriptOnlyNonModalDialogThatCarriesTheSiteDefaults(string layout)
	{
		string html = Render(_allPickers with { Style = "Paper", CodeTheme = "nord", CodeStyle = "macos", DefaultColorTheme = "teal" }, layout);

		html.Should().Contain("<div class=\"appearance js-only\">");
		html.Should().Contain("<button class=\"appearance-trigger\" type=\"button\" aria-label=\"Appearance\" aria-haspopup=\"dialog\" aria-expanded=\"false\" aria-controls=\"appearancePanel\">");
		html.Should().Contain("<div class=\"appearance-panel\" id=\"appearancePanel\" role=\"dialog\" aria-labelledby=\"appearanceTitle\" data-default-style=\"paper\" data-default-color-theme=\"teal\" data-default-code-theme=\"nord\" data-default-code-style=\"macos\" hidden>");
		html.Should().Contain("<h2 class=\"appearance-title\" id=\"appearanceTitle\">Appearance</h2>");
		html.Should().NotContain("aria-modal=\"true\" aria-labelledby=\"appearanceTitle\"");
		html.Should().Contain("<button class=\"appearance-reset\" type=\"button\">Reset to site defaults</button>");
		foreach (Match group in Regex.Matches(html, "role=\"radiogroup\" aria-labelledby=\"(\\w+)\""))
		{
			html.Should().Contain($"id=\"{group.Groups[1].Value}\"", "every radiogroup is named by its label");
		}

		Regex.Matches(html, "role=\"radio\" aria-checked=\"false\" tabindex=\"-1\"").Count.Should().Be(3 + 4 + 5, "main.js builds the other lists");
		html.IndexOf("class=\"appearance js-only\"", StringComparison.Ordinal).Should().BeLessThan(
			html.IndexOf("class=\"theme-toggle\"", StringComparison.Ordinal), "phones hide the dark mode button that follows the panel");
	}

	[Fact]
	public void Panel_EscapesTheSiteDefaultsAndLeavesNoPresetForACustomPrimaryColor()
	{
		string html = Render(_allPickers with { CodeTheme = "\"><script>", PrimaryColor = "#d946ef", DefaultColorTheme = "ocean" });

		Regex.Match(html, "<div class=\"appearance-panel\"[^>]*>").Value.Should().NotContain("<script>").And.Contain("data-default-color-theme=\"\"");
	}

	[Fact]
	public void CodeThemePairs_GoBothWaysAndJoinADarkAndALightTheme()
	{
		foreach (CodeThemeInfo theme in CodeThemes.All.Where(t => t.Counterpart is not null))
		{
			CodeThemeInfo? other = CodeThemes.Find(theme.Counterpart);
			other.Should().NotBeNull("{0} names a code theme as its counterpart", theme.Name);
			other!.Counterpart.Should().Be(theme.Name);
			other.IsDark.Should().Be(!theme.IsDark);
		}

		CodeThemes.All.Where(t => !t.IsDark).Should().OnlyContain(t => t.Counterpart != null, "a light theme swaps back to dark when the reader turns dark mode on");
	}

	[Fact]
	public void MainScript_CarriesTheSameDarkFirstStylesAndPairs()
	{
		string js = EmbeddedThemeProvider.GetJs();

		string pairs = Regex.Match(js, @"const codeThemePairs = (\{[^;]*\});").Groups[1].Value;
		JsonSerializer.Deserialize<Dictionary<string, string>>(pairs).Should().BeEquivalentTo(
			CodeThemes.All.Where(t => t.IsDark && t.Counterpart is not null).ToDictionary(t => t.Name, t => t.Counterpart!));
		string dark = Regex.Match(js, @"const darkFirstStyles = (\[[^;]*\]);").Groups[1].Value;
		JsonSerializer.Deserialize<string[]>(dark).Should().Equal(ThemeStyles.All.Where(s => s.DarkFirst).Select(s => s.Name));
		js.Should().NotContain("/*code-theme-pairs*/").And.NotContain("/*dark-first-styles*/");
	}

	[Theory]
	[InlineData("terminal", "nord", "{}", false, "terminal", "dark", "nord")]
	[InlineData("moka", "nord", "{}", false, "moka", "dark", "nord")]
	[InlineData("paper", "nord", "{}", false, "paper", "light", "nord")]
	[InlineData("paper", "nord", "{}", true, "paper", "dark", "nord")]
	[InlineData("terminal", "nord", """{"mokadocs-theme":"light"}""", false, "terminal", "light", "nord")]
	[InlineData("default", "gruvbox-dark", """{"mokadocs-theme":"light"}""", false, "default", "light", "gruvbox-light")]
	[InlineData("default", "solarized-light", """{"mokadocs-theme":"dark"}""", false, "default", "dark", "solarized-dark")]
	[InlineData("default", "rose-pine", """{"mokadocs-theme":"light"}""", false, "default", "light", "rose-pine-dawn")]
	[InlineData("default", "github-dark", "{}", false, "default", "light", "github-dark")]
	[InlineData("default", "tokyo-night", """{"mokadocs-theme":"light"}""", false, "default", "light", "tokyo-night")]
	[InlineData("default", "catppuccin-mocha", """{"mokadocs-style":"brutal","mokadocs-code-theme":"monokai","mokadocs-theme":"light"}""", false, "brutal", "light", "monokai")]
	public void HeadScript_SetsTheStyleModeAndCodeThemeBeforeTheFirstPaint(string style, string codeTheme, string stored, bool osDark,
		string expectedStyle, string expectedMode, string expectedCodeTheme)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Dictionary<string, string> attrs = RunHeadScript(_allPickers with { Style = style, CodeTheme = codeTheme }, stored, osDark);

		$"{attrs["data-style"]} {attrs["data-theme"]} {attrs["data-code-theme"]}".Should().Be($"{expectedStyle} {expectedMode} {expectedCodeTheme}");
	}

	[Theory]
	[InlineData("""{"mokadocs-text-size":"125","mokadocs-reader-width":"wide"}""", true, true, "125 wide")]
	[InlineData("""{"mokadocs-text-size":"112.5","mokadocs-reader-width":"narrow"}""", true, true, "112.5 narrow")]
	[InlineData("""{"mokadocs-text-size":"90"}""", true, true, "90 -")]
	[InlineData("""{"mokadocs-text-size":"100","mokadocs-reader-width":"wider"}""", true, true, "- wider")]
	[InlineData("""{"mokadocs-reader-width":"full"}""", true, true, "- full")]
	[InlineData("""{"mokadocs-text-size":"100","mokadocs-reader-width":"default"}""", true, true, "- -")]
	[InlineData("""{"mokadocs-text-size":"300","mokadocs-reader-width":"huge"}""", true, true, "- -")]
	[InlineData("""{"mokadocs-text-size":"125","mokadocs-reader-width":"wide"}""", false, false, "- -")]
	[InlineData("""{"mokadocs-text-size":"125","mokadocs-reader-width":"wide"}""", true, false, "125 -")]
	[InlineData("""{"mokadocs-text-size":"125","mokadocs-reader-width":"wide"}""", false, true, "- wide")]
	public void HeadScript_AppliesAStoredTextSizeAndReadingWidthOnlyWhileTheirSectionIsOn(string stored, bool textSize, bool width, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Dictionary<string, string> attrs = RunHeadScript(_allPickers with { TextSizeSelector = textSize, WidthSelector = width }, stored, false);

		string Value(string name) => attrs.TryGetValue(name, out string? value) ? value : "-";
		$"{Value("data-text-size")} {Value("data-reader-width")}".Should().Be(expected);
		attrs["data-style"].Should().Be("default", "the rest of the script still ran");
	}

	[Fact]
	public void HeadScript_LeavesOutTheReaderKeysWhenTheirSectionsAreOff()
	{
		string bootstrap = HeadScript(Render(_allPickers with { TextSizeSelector = false, WidthSelector = false }));

		bootstrap.Should().NotContain("mokadocs-text-size").And.NotContain("mokadocs-reader-width");
		bootstrap.Should().EndWith("d.setAttribute('data-code-style',cs);}catch(e){}");
	}

	[Fact]
	public void MainScript_CarriesTheLayoutPartsReadersSwitchWithTheirLabels()
	{
		string js = EmbeddedThemeProvider.GetJs();

		using JsonDocument options = JsonDocument.Parse(Regex.Match(js, @"const appearanceOptions = (\{[^\n]*\});").Groups[1].Value);
		JsonElement layout = options.RootElement.GetProperty("layout");
		layout.EnumerateArray().Select(p => p.GetProperty("key").GetString()).Should().Equal(
			"navbar", "footer", "toc", "breadcrumbs", "page-nav", "sidebar", "sidebar-scope", "admonitions", "tables", "density",
			"font", "headings", "links");
		foreach ((JsonElement entry, ThemeLayoutPart part) in layout.EnumerateArray().Zip(ThemeLayouts.ReaderParts))
		{
			entry.GetProperty("label").GetString().Should().Be(part.Label);
			entry.GetProperty("variants").EnumerateArray().Select(v => (v.GetProperty("name").GetString(), v.GetProperty("label").GetString()))
				.Should().Equal(part.Names.Select(n => ((string?)n, (string?)part.LabelOf(n))));
		}
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public void LayoutSection_IsOnlyOnPagesWhoseSiteTurnsItOn(string layout)
	{
		ThemeOptions on = OnlySections([]) with
		{
			LayoutSelector = true,
			Layout = new ThemeLayoutOptions { Navbar = "Tabs", PageNav = "split", Width = "wide", Hero = "banner" }
		};

		string html = Render(on, layout);
		string without = Render(_allPickers, layout);

		html.Should().Contain("<button class=\"appearance-trigger\"", "the Layout section alone is reason enough for the button");
		html.Should().Contain("<p class=\"appearance-label\" id=\"appearanceLayoutLabel\">Layout</p>");
		html.Should().Contain("<div class=\"appearance-layout\" role=\"group\" aria-labelledby=\"appearanceLayoutLabel\" data-default-layout=\"navbar=tabs page-nav=split \">",
			"the panel knows the site's own variants of the parts readers switch, and only those");
		html.IndexOf("appearanceLayoutLabel", StringComparison.Ordinal).Should().BeLessThan(
			html.IndexOf("class=\"appearance-reset\"", StringComparison.Ordinal));
		without.Should().NotContain("appearance-layout").And.NotContain("mokadocs-layout-");
	}

	[Theory]
	[InlineData("""{"mokadocs-layout-navbar":"tabs","mokadocs-layout-page-nav":"split"}""", true, "tabs split - columns")]
	[InlineData("""{"mokadocs-layout-footer":"default"}""", true, "- - - -")]
	[InlineData("""{"mokadocs-layout-footer":"Columns","mokadocs-layout-toc":"sideways","mokadocs-layout-width":"full"}""", true, "- - - columns")]
	[InlineData("""{"mokadocs-layout-sidebar-scope":"section","mokadocs-layout-links":"subtle"}""", true, "- - section columns")]
	[InlineData("""{"mokadocs-layout-navbar":"tabs","mokadocs-layout-footer":"default"}""", false, "- - - columns")]
	public void HeadScript_AppliesStoredLayoutPicksItKnowsOnlyWhileTheSectionIsOn(string stored, bool selector, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");
		ThemeOptions options = _allPickers with { LayoutSelector = selector, Layout = new ThemeLayoutOptions { Footer = "columns" } };

		Dictionary<string, string> attrs = RunHeadScript(options, stored, false);

		string Value(string name) => attrs.TryGetValue(name, out string? value) ? value : "-";
		$"{Value("data-navbar")} {Value("data-page-nav")} {Value("data-sidebar-scope")} {Value("data-footer")}".Should().Be(expected);
		attrs.Should().NotContainKey("data-toc", "an unknown name is ignored").And.NotContainKey("data-width", "width isn't a part readers switch here");
		attrs["data-style"].Should().Be("default", "the rest of the script still ran");
	}

	[Fact]
	public void HeadScript_LeavesOutTheLayoutPartsWhenTheSectionIsOff()
	{
		HeadScript(Render(_allPickers)).Should().NotContain("mokadocs-layout-");
		HeadScript(Render(_allPickers with { LayoutSelector = true })).Should().Contain(
			"const l={ 'navbar':'default compact floating tabs autohide centered transparent','footer':'default minimal columns none centered compact',");
	}

	private Dictionary<string, string> RunHeadScript(ThemeOptions options, string stored, bool osDark)
	{
		string html = Render(options);
		Dictionary<string, string> configured = Regex.Matches(Regex.Match(html, "<html [^>]*>").Value, "(data-[a-z-]+)=\"([^\"]*)\"")
			.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

		string result = NodeScript.Run($$"""
		                                 const attrs = {{JsonSerializer.Serialize(configured)}};
		                                 const document = { documentElement: { classList: { remove() {} }, getAttribute: n => n in attrs ? attrs[n] : null, setAttribute: (n, v) => { attrs[n] = String(v); }, removeAttribute: n => { delete attrs[n]; } } };
		                                 const stored = {{stored}};
		                                 const localStorage = { getItem: k => k in stored ? stored[k] : null };
		                                 const matchMedia = q => ({ matches: {{(osDark ? "true" : "false")}} && q.includes('dark') });
		                                 {{HeadScript(html)}}
		                                 process.stdout.write(JSON.stringify(attrs));
		                                 """);
		return JsonSerializer.Deserialize<Dictionary<string, string>>(result)!;
	}

	private static string HeadScript(string html) =>
		Regex.Match(html, @"<script>(document\.documentElement\.classList\.remove\('no-js'\);.*?)</script>").Groups[1].Value;

	private static ThemeOptions OnlySections(IReadOnlyCollection<string> on) => new()
	{
		ShowDarkModeToggle = on.Contains("mode"),
		StyleSelector = on.Contains("style"),
		ColorThemes = on.Contains("color-theme"),
		CodeThemeSelector = on.Contains("code-theme"),
		CodeStyleSelector = on.Contains("code-style"),
		TextSizeSelector = on.Contains("text-size"),
		WidthSelector = on.Contains("reader-width")
	};

	private static List<string> RenderedSections(string html) =>
		Regex.Matches(html, "role=\"radiogroup\" aria-labelledby=\"\\w+\" data-appearance=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

	private static string Group(string html, string section) =>
		Regex.Match(html, $"data-appearance=\"{section}\">(.*?)</div>", RegexOptions.Singleline).Groups[1].Value;

	private static List<string> Options(string html, string section) =>
		Regex.Matches(Group(html, section), "role=\"radio\" aria-checked=\"false\" tabindex=\"-1\" data-value=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();

	private string Render(ThemeOptions options, string layout = "default")
	{
		DocPage page = new()
		{
			Route = layout == "landing" ? "/" : "/guide",
			FrontMatter = new FrontMatter { Title = "Guide", Layout = layout },
			Content = new PageContent { Html = "<p>Guide</p>" }
		};
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		var context = new ThemeRenderContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "Test" }, Theme = new ThemeConfig { Options = options } },
			Templates = theme.Templates,
			Partials = theme.Partials,
			CssFiles = theme.CssFiles,
			JsFiles = theme.JsFiles,
			AllPages = [page]
		};
		return _engine.RenderPage(page, context, TestContext.Current.CancellationToken);
	}
}
