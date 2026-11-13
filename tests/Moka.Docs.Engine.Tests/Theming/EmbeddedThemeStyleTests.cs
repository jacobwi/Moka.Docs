using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the appearance styles of the default theme. A style is CSS scoped to
///     <c>html[data-style]</c> plus the picker script, so these look for the rules that implement it.
/// </summary>
public sealed class EmbeddedThemeStyleTests
{
	private static readonly string _css = ThemeCss.All;

	public static TheoryData<string> Styles()
	{
		var data = new TheoryData<string>();
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			data.Add(style);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_HasALightAndADarkBlock(string style)
	{
		string light = Rules(style, $"html[data-style=\"{style}\"] {{");
		string dark = Rules(style, $"html[data-style=\"{style}\"][data-theme=\"dark\"] {{");

		light.Should().Contain("--color-bg:").And.Contain("--color-text:").And.Contain("--color-text-muted:");
		dark.Should().Contain("--color-bg:").And.Contain("--color-text:").And.Contain("--color-text-muted:");
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void StyleRules_AreScopedToTheirStyle(string style)
	{
		// An unscoped selector would change the default look, which must stay as it was, and a selector for another
		// style would apply from the wrong place once a reader who switched styles has both files.
		string[] allowed = [$"html[data-style=\"{style}\"]", $":where(html[data-style=\"{style}\"])"];

		var unscoped = new List<string>();
		bool inComment = false;
		foreach (string line in ThemeCss.Style(style).Split('\n'))
		{
			if (inComment || line.TrimStart().StartsWith("/*", StringComparison.Ordinal))
			{
				inComment = !line.Contains("*/", StringComparison.Ordinal);
				continue;
			}

			int brace = line.IndexOf('{', StringComparison.Ordinal);
			string? selector = brace >= 0 ? line[..brace].Trim()
				: line.Length > 0 && !char.IsWhiteSpace(line[0]) && line.EndsWith(',') ? line.Trim()
				: null;
			if (string.IsNullOrEmpty(selector) || selector.StartsWith('@'))
			{
				continue;
			}

			if (!allowed.Any(scope => selector.StartsWith(scope, StringComparison.Ordinal)))
			{
				unscoped.Add(selector);
			}
		}

		unscoped.Should().BeEmpty();
		Regex.Matches(ThemeCss.Style(style), @"data-style=""(\w+)""").Select(m => m.Groups[1].Value).Distinct().Should().Equal(style);
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_GetsTheSharedTokensAndRulesInItsOwnScope(string style)
	{
		// The shared rules used to list every style in one html:is(...). Each style's file has them under its own
		// scope, which has the same specificity, in the same place relative to the style's own rules.
		string css = ThemeCss.Style(style);

		css.Should().StartWith("/* Styles: shared tokens");
		Rules(style, $"html[data-style=\"{style}\"] {{").Should()
			.Contain("--st-link: color-mix(in srgb, var(--color-primary) var(--st-hue), var(--st-ink));");
		css.Should().Contain($":where(html[data-style=\"{style}\"]) a,\n:where(html[data-style=\"{style}\"]) a:hover {{ color: var(--st-link); }}");
		css.IndexOf("/* Styles: shared tokens", StringComparison.Ordinal).Should().BeLessThan(
			css.IndexOf("/* Style: ", StringComparison.Ordinal));
		css.Should().NotContain("html:is(");
	}

	[Fact]
	public void BaseStylesheet_HoldsEveryPreviewButNoStyleRules()
	{
		// main.css is the default style. The others are files of their own that a page links only when it needs them.
		Regex.Matches(ThemeCss.Main, @"html(:is\(|\[)\[?data-style=").Should().BeEmpty();
		EmbeddedThemeProvider.GetStyleCss(ThemeStyles.Default).Should().BeEmpty();
		EmbeddedThemeProvider.GetStyleCss("no-such-style").Should().BeEmpty();
		EmbeddedThemeProvider.GetStyleCss("Moka").Should().Be(EmbeddedThemeProvider.GetStyleCss("moka"));
		EmbeddedThemeProvider.StyleFileNames.Should().Equal(ThemeStyles.Names.Where(s => s != ThemeStyles.Default));
	}

	[Fact]
	public void StyleRules_KeepPseudoElementsOutOfIs()
	{
		// A pseudo-element inside :is() is invalid, and the browser drops the whole rule.
		Regex.Matches(_css, @":is\([^)]*::").Should().BeEmpty();
	}

	[Fact]
	public void MokaStyle_HasNoDropShadows()
	{
		string moka = Rules("moka", "html[data-style=\"moka\"] {");

		moka.Should().Contain("--shadow-sm: none;").And.Contain("--shadow-md: none;").And.Contain("--shadow-lg: none;");
	}

	[Fact]
	public void MokaStyle_TintsBordersWithTheActiveAccent()
	{
		string dark = Rules("moka", "html[data-style=\"moka\"][data-theme=\"dark\"] {");

		dark.Should().Contain("--mk-border: color-mix(in srgb, var(--color-primary) 6%, transparent);");
		dark.Should().Contain("--mk-border-visible: color-mix(in srgb, var(--color-primary) 12%, transparent);");
		dark.Should().Contain("--mk-border-strong: color-mix(in srgb, var(--color-primary) 24%, transparent);");
	}

	[Fact]
	public void Panel_IsKeyboardAccessibleAndRemembersTheChoice()
	{
		string js = EmbeddedThemeProvider.GetJs();

		js.Should().Contain("store.set('mokadocs-style', style);");
		js.Should().Contain("radio.setAttribute('aria-checked', radio === checked ? 'true' : 'false');");
		js.Should().Contain("radio.setAttribute('tabindex', radio === tabStop ? '0' : '-1');");
		js.Should().Contain("e.key === 'ArrowDown'").And.Contain("if (e.key !== 'Escape') return;");
	}

	[Fact]
	public void Panel_SwitchesToDarkForDarkFirstStylesOnlyWhenTheReaderNeverChose()
	{
		// Light and Dark store mokadocs-theme. Without it, picking a dark-first style switches to dark.
		string js = EmbeddedThemeProvider.GetJs();

		js.Should().Contain("if (!store.get('mokadocs-theme')) html.setAttribute('data-theme', defaultMode());");
		js.Should().Contain("return darkFirstStyles.indexOf(html.getAttribute('data-style')) !== -1 || mediaMatches('(prefers-color-scheme: dark)') ? 'dark' : 'light';");
		js.Should().Contain("const darkFirstStyles = [\"moka\",\"terminal\"];");
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_StylesTheAppearancePanel(string style)
	{
		Regex.IsMatch(_css, $@"\nhtml\[data-style=""{style}""\][^{{\n]*\.appearance-(panel|option|section|label)").Should().BeTrue();
		Regex.IsMatch(_css, $@"\nhtml\[data-style=""{style}""\][^{{\n]*\.appearance-trigger").Should().BeTrue();
	}

	[Fact]
	public void FontSizes_AreInRemSoTheTextSizeScalesEveryStyle()
	{
		// The text size choice scales the root font size; a size in pixels would stay as it was.
		Regex.Matches(_css, @"font-size: \d+(\.\d+)?px").Select(m => m.Value).Should().BeEmpty();
		_css.Should().Contain("html[data-text-size=\"90\"] { font-size: 90%; }")
			.And.Contain("html[data-text-size=\"112.5\"] { font-size: 112.5%; }")
			.And.Contain("html[data-text-size=\"125\"] { font-size: 125%; }");
	}

	[Fact]
	public void ReadingWidth_OnlyMovesTheColumnAndPageWidthsAndWinsOverStylesAndLayouts()
	{
		// :root lifts the rules above html[data-style] and html[data-width], which set the same variables.
		// The page grows where the column wouldn't fit between the sidebar and "On this page", and a pick
		// never comes out narrower than the site's own layout.width, except narrow.
		MatchCollection rules = Regex.Matches(_css, @"\n(html:root\[data-reader-width=""\w+""\][^{]*)\{ ([^}]*)\}");

		rules.Select(m => m.Groups[1].Value.Trim() + " " + m.Groups[2].Value.Trim()).Should().Equal(
			"html:root[data-reader-width=\"narrow\"] --content-max-width: 60ch;",
			"html:root[data-reader-width=\"wide\"] --content-max-width: 1040px;",
			"html:root[data-reader-width=\"wide\"]:is(:not([data-width]), [data-width=\"narrow\"]) --layout-max-width: 1680px;",
			"html:root[data-reader-width=\"wide\"][data-width=\"wider\"] --content-max-width: 1280px;",
			"html:root[data-reader-width=\"wide\"][data-width=\"full\"] --content-max-width: none;",
			"html:root[data-reader-width=\"wider\"] --layout-max-width: 1920px; --content-max-width: 1280px;",
			"html:root[data-reader-width=\"wider\"][data-width=\"full\"] --layout-max-width: none; --content-max-width: none;",
			"html:root[data-reader-width=\"full\"] --layout-max-width: none; --content-max-width: none;");
	}

	[Fact]
	public void EveryStyle_HasAPickerPreview()
	{
		foreach (string style in ThemeStyles.Names)
		{
			_css.Should().Contain($".style-preview-{style} {{", "the picker shows a preview of {0}", style);
		}
	}

	[Fact]
	public void MokaStyle_KeepsLightCodeThemesLight()
	{
		// Moka darkens every dark code theme's background in dark mode; a light theme it missed got
		// its dark tokens on a near-black background.
		string themes = Regex.Match(_css, @"html\[data-style=""moka""\]\[data-theme=""dark""\]:not\((?<themes>[^)]*)\)").Groups["themes"].Value;

		foreach (CodeThemeInfo theme in CodeThemes.All.Where(t => !t.IsDark))
		{
			themes.Should().Contain($"[data-code-theme=\"{theme.Name}\"]");
		}
	}

	[Fact]
	public void PseudoContent_IsHiddenFromScreenReaders()
	{
		// Prompt marks, brackets and path prefixes are decoration; the alternative text after the
		// slash keeps them out of what a screen reader reads.
		string styles = string.Concat(EmbeddedThemeProvider.StyleFileNames.Select(ThemeCss.Style));

		foreach (Match content in Regex.Matches(styles, @"content: '([^']+)';(?! content)"))
		{
			styles.Should().Contain($"content: '{content.Groups[1].Value}' / '';");
		}
	}

	[Fact]
	public void AppearancePanel_StaysOnPhonesAndTakesOverFromTheDarkModeButton()
	{
		// Phones had no pickers at all: every one of them was display: none at 768px and below.
		_css.Should().NotContain(".style-selector").And.NotContain(".color-theme-selector").And.NotContain(".appearance-group {");
		Regex.IsMatch(_css, @"\.appearance(-trigger|-panel)? \{[^}]*display: none").Should().BeFalse();
		string phone = Regex.Match(_css, @"@media \(max-width: 768px\) \{\n    \.appearance \{ position: static; \}.*?\n\}", RegexOptions.Singleline).Value;
		phone.Should().Contain("left: 0;").And.Contain("right: 0;").And.Contain("max-height: calc(100dvh - var(--header-offset));");
		phone.Should().Contain(".appearance ~ .theme-toggle { display: none; }");
		Rule(".appearance-panel {").Should().Contain("z-index: var(--z-popover);").And.Contain("overflow-y: auto;");
	}

	private static string Rule(string selector)
	{
		int start = _css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the theme should have a rule for {selector.Trim()}");
		return _css[start..(_css.IndexOf('}', start) + 1)];
	}

	// Every rule in the style's file with exactly this selector, one after the other. The shared tokens come first
	// under the same selector as the style's own variables.
	private static string Rules(string style, string selector)
	{
		string css = ThemeCss.Style(style);
		var rules = new List<string>();
		for (int at = css.IndexOf("\n" + selector, StringComparison.Ordinal); at >= 0; at = css.IndexOf("\n" + selector, at + 1, StringComparison.Ordinal))
		{
			rules.Add(css[(at + 1)..(css.IndexOf('}', at) + 1)]);
		}

		rules.Should().NotBeEmpty($"the {style} style should have a rule for {selector}");
		return string.Join("\n", rules);
	}
}
