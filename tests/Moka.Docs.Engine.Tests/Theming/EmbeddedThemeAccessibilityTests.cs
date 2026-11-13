using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;
using static Moka.Docs.Engine.Tests.Theming.ThemeColors;

namespace Moka.Docs.Engine.Tests.Theming;

public sealed class EmbeddedThemeAccessibilityTests
{
	private static readonly string _css = ThemeCss.All;
	private static readonly double[] _darkPage = Hex("#0f172a");

	public static TheoryData<string> Presets() =>
		new(global::Moka.Docs.Core.Theming.ColorPresets.Names);

	[Theory]
	[MemberData(nameof(Presets))]
	public void GradientTitles_PassAsLargeTextWithEveryPreset(string preset)
	{
		// The page title went from the raw primary to a lighter shade, 1.7:1 for amber on white; the
		// landing hero's cyan and teal stops were 1.9:1.
		Rule(".page-content h1 {\n    font-size: 2.25rem !important;").Should()
			.Contain("background: linear-gradient(135deg, var(--color-primary-text) 0%, var(--color-primary-text-hover) 100%);");
		Rule(".landing-hero-title {\n    font-size").Should()
			.Contain("linear-gradient(135deg, var(--color-primary-text) 0%, var(--gradient-text-secondary) 50%, var(--gradient-text-tertiary) 100%)");
		double lightHue = Percent(Regex.Match(_css, @"--title-hue: (\d+)%;").Groups[1].Value);
		double darkHue = Percent(Regex.Match(_css, @"\[data-theme=""dark""\] \{ --title-ink: #ffffff; --title-hue: (\d+)%; \}").Groups[1].Value);
		(double[] primary, double[] secondary, double[] tertiary) = Gradient(preset);
		(double[] light, double[] dark) = TextShades(preset);

		// The hero sits on up to about 14% of the primary (its tint and glow) over the page.
		foreach ((double[] page, double[] ink, double hue, double[] text, double[] hover) in new[]
		         {
			         (White, Black, lightHue, light, Mix(light, 0.8, Black)),
			         (_darkPage, White, darkHue, dark, Mix(dark, 0.75, White))
		         })
		{
			double[][] backgrounds = [page, Mix(primary, 0.14, page)];
			double[][] stops = [text, hover, MixOklab(secondary, hue, ink), MixOklab(tertiary, hue, ink)];
			foreach (double[] background in backgrounds)
			{
				foreach (double[] stop in stops)
				{
					Contrast(stop, background).Should().BeGreaterThanOrEqualTo(3, "every gradient stop for {0}", preset);
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void PrimaryFills_CarryTextThatMeetsAa(string preset)
	{
		// White on the raw primary was 2.2:1 for amber (back to top, Get Started, step numbers).
		(double[] light, double[] dark) = TextShades(preset);

		Contrast(White, light).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(White, Mix(light, 0.8, Black)).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(_darkPage, dark).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(_darkPage, Mix(dark, 0.75, White)).Should().BeGreaterThanOrEqualTo(4.5);
	}

	[Theory]
	[InlineData(".back-to-top { position: fixed;")]
	[InlineData(".landing-btn-primary {")]
	[InlineData(".landing .landing-content > h1:first-of-type + p + ul a:first-of-type {")]
	[InlineData("    .toc-toggle-btn {\n        display: flex;")]
	public void PrimaryFills_UseTheFillAndOnPrimaryColors(string selector)
	{
		Rule(selector).Should().Contain("var(--color-primary-fill)").And.Contain("var(--color-on-primary)");
		Regex.Match(_css, @"\.component-step-number \{[^}]*\}").Value.Should().Contain("background: var(--color-primary-fill);").And.Contain("color: var(--color-on-primary);");
		Variables(":root {\n    --color-primary-fill")["--color-on-primary"].Should().Be("#ffffff");
		_css.Should().Contain("[data-theme=\"dark\"] { --color-on-primary: #0f172a; }");
	}

	[Theory]
	[InlineData("note", "note")]
	[InlineData("tip", "tip")]
	[InlineData("warning", "warning")]
	[InlineData("caution", "warning")]
	[InlineData("danger", "danger")]
	[InlineData("info", "info")]
	[InlineData("important", "info")]
	public void AdmonitionTitles_MeetAaOnTheirTint(string kind, string color)
	{
		// The raw colors were 2.0 (warning) to 3.8:1 (info) on their light tints.
		Rule($".page-content .admonition-{kind} .admonition-title {{").Should().Contain($"color: var(--admonition-{color}-text);");
		double[] box = Hex(Regex.Match(_css, $@"\.page-content \.admonition-{kind} \{{ border-color: (#[0-9a-f]{{6}});").Groups[1].Value);
		string light = Variables(":root {\n    --admonition-note-text")[$"--admonition-{color}-text"];
		string dark = Variables("[data-theme=\"dark\"] {\n    --admonition-note-text")[$"--admonition-{color}-text"];

		Contrast(Hex(light), Mix(box, 0.08, White)).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(Hex(dark), Mix(box, 0.08, _darkPage)).Should().BeGreaterThanOrEqualTo(4.5);
	}

	[Fact]
	public void TabsAndSearchMarks_UseTextColorsThatMeetAa()
	{
		// The active tab was the raw primary (2.1:1 for amber); marked search words kept the
		// snippet's muted color on the highlight (3.2:1 in dark mode).
		_css.Should().Contain(".tab-header.active { color: var(--color-primary-text); border-bottom-color: var(--color-primary); }");
		Rule(".search-result-snippet mark {").Should().Contain("color: var(--color-text);");
	}

	[Theory]
	[InlineData(":root { --feedback-yes", "#ffffff")]
	[InlineData("[data-theme=\"dark\"] { --feedback-yes", "#0f172a")]
	public void FeedbackChoice_MeetsAa(string scope, string page)
	{
		// The chosen answer was white on #22c55e, 2.3:1.
		string block = Regex.Match(_css, Regex.Escape(scope) + @"[^}]*\}").Value;
		string Value(string name) => Regex.Match(block, Regex.Escape(name) + @": (#[0-9a-f]{6});").Groups[1].Value;

		foreach (string fill in new[] { Value("--feedback-yes"), Value("--feedback-no") })
		{
			Contrast(Hex(Value("--feedback-on")), Hex(fill)).Should().BeGreaterThanOrEqualTo(4.5);
			Contrast(Hex(fill), Hex(page)).Should().BeGreaterThanOrEqualTo(3, "the hovered answer's text and the fill against the page");
		}

		_css.Should().Contain(".feedback-btn-yes.selected { border-color: var(--feedback-yes); color: var(--feedback-on); background: var(--feedback-yes); }");
	}

	[Fact]
	public void ApiBadges_MeetAaOnTheirTint()
	{
		Dictionary<string, string> light = Variables(":root {\n    --badge-blue");
		Dictionary<string, string> dark = Variables("[data-theme=\"dark\"] {\n    --badge-blue");
		MatchCollection rules = Regex.Matches(_css, @"\.api-badge[\w.-]* \{ background: color-mix\(in srgb, (#[0-9a-f]{6}) (\d+)%, var\(--color-bg\)\); color: var\((--badge-[a-z]+)\); \}");

		rules.Should().HaveCount(14);
		foreach (Match rule in rules)
		{
			double[] tint = Hex(rule.Groups[1].Value);
			double amount = Percent(rule.Groups[2].Value);
			Contrast(Hex(light[rule.Groups[3].Value]), Mix(tint, amount, White)).Should().BeGreaterThanOrEqualTo(4.5, rule.Value);
			Contrast(Hex(dark[rule.Groups[3].Value]), Mix(tint, amount, _darkPage)).Should().BeGreaterThanOrEqualTo(4.5, rule.Value);
		}
	}

	[Fact]
	public void TocLevels_IndentEvenlyDownToH6()
	{
		// Round 1 nested the TOC to h6, but levels 5 and 6 sat at level 4's indent.
		double[] indents = Enumerable.Range(2, 5)
			.Select(level => level == 2 ? 0.75 : double.Parse(Regex.Match(_css, $@"\n\.toc-level-{level} a \{{ padding-left: ([\d.]+)rem; \}}").Groups[1].Value, CultureInfo.InvariantCulture))
			.ToArray();

		indents.Zip(indents.Skip(1), (a, b) => b - a).Should().AllSatisfy(step => step.Should().BeApproximately(0.75, 0.001));
	}

	[Fact]
	public void PageActions_AreStyledByTheStylesheetAndCopyPageIsScriptOnly()
	{
		string layout = EmbeddedThemeProvider.CreateDefault().Templates["default"];

		layout.Should().NotContain("<style>.page-actions");
		layout.Should().Contain("class=\"page-action page-copy-markdown js-only\"");
		Rule(".page-actions {").Should().Contain("justify-content: flex-end;");
		_css.Should().Contain(".page-actions .page-action[hidden] { display: none; }");
		_css.Should().Contain(".page-actions .page-action:focus-visible { outline: 2px solid currentColor; outline-offset: 2px; }");
	}

	[Fact]
	public void Print_ShowsTheArticleInBlackOnWhiteWithLinkTargets()
	{
		string print = Block("@media print {");

		string hidden = Regex.Match(print, @"\.skip-link, [^{]*\{\s*display: none !important;").Value;
		foreach (string part in new[] { ".site-header", ".sidebar", ".toc-sidebar", ".toc-toggle-btn", ".back-to-top", ".feedback-widget", ".page-nav", ".page-actions", ".search-modal", ".copy-btn" })
		{
			hidden.Should().Contain(part);
		}

		print.Should().Contain("color: #000000 !important;").And.Contain("-webkit-text-fill-color: currentColor !important;");
		print.Should().Contain(".page-content a[href]:not([href^=\"#\"])").And.Contain("content: \" (\" attr(href) \")\";");
		print.Should().Contain("white-space: pre-wrap !important;").And.Contain("--sh-comment: #6e7781 !important;");
		print.Should().Contain(":is(pre, .code-block, table, .admonition, figure, img, .mermaid, blockquote, .component-card, .install-panel) { break-inside: avoid; }");
		print.Should().Contain(".content { max-width: none; padding: 0; }");
	}

	[Fact]
	public void ForcedColors_GiveGradientTitlesAndStatesSystemColors()
	{
		string forced = Block("@media (forced-colors: active) {");

		forced.Should().Contain("-webkit-text-fill-color: CanvasText !important;");
		forced.Should().Contain(":focus-visible { outline: 2px solid Highlight !important;");
		forced.Should().Contain(".nav-link { border-left-color: Canvas !important; }");
		forced.Should().Contain(".nav-link.current { border-left-color: Highlight !important; text-decoration: underline;");
		forced.Should().Contain(".toc-item a.active { border-left: 3px solid Highlight !important; text-decoration: underline;");
		forced.Should().Contain(":is(.tab-header, .install-tab).active, .header-tab.current { border-bottom: 3px solid Highlight !important; }");
		forced.Should().Contain("[role=\"radio\"][aria-checked=\"true\"]");
	}

	private static (double[] Primary, double[] Secondary, double[] Tertiary) Gradient(string preset)
	{
		Match block = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary: (#[0-9a-f]{{6}}) !important;[^}}]*--gradient-secondary: (#[0-9a-f]{{6}});\s*--gradient-tertiary: (#[0-9a-f]{{6}});");
		block.Success.Should().BeTrue("{0} has a preset block", preset);
		return (Hex(block.Groups[1].Value), Hex(block.Groups[2].Value), Hex(block.Groups[3].Value));
	}

	// The preset's --color-primary-text in light and dark mode: its own rule, else the primary.
	internal static (double[] Light, double[] Dark) TextShades(string preset)
	{
		string primary = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;
		string light = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{ --color-primary-text: (#[0-9a-f]{{6}}); \}}") is { Success: true } l ? l.Groups[1].Value : primary;
		string dark = Regex.Match(_css, $@"html\[data-theme=""dark""\]\[data-color-theme=""{preset}""\] \{{ --color-primary-text: (#[0-9a-f]{{6}}); \}}") is { Success: true } d
			? d.Groups[1].Value
			: Regex.IsMatch(_css, $@"html\[data-theme=""dark""\]:is\([^)]*\[data-color-theme=""{preset}""\][^)]*\) \{{ --color-primary-text: var\(--color-primary\); \}}")
				? primary
				: light;
		return (Hex(light), Hex(dark));
	}

	private static double Percent(string value) => double.Parse(value, CultureInfo.InvariantCulture) / 100;

	private static Dictionary<string, string> Variables(string blockStart)
	{
		int start = _css.IndexOf(blockStart, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have {0}", blockStart);
		string body = _css[start..(_css.IndexOf('}', start) + 1)];
		return Regex.Matches(body, @"(--[\w-]+): ([^;]+);").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim());
	}

	private static string Rule(string selector)
	{
		int start = _css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have a rule for {0}", selector);
		return _css[start..(_css.IndexOf('}', start) + 1)];
	}

	private static string Block(string start)
	{
		int at = _css.IndexOf(start, StringComparison.Ordinal);
		at.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have {0}", start);
		int depth = 0;
		for (int i = at; i < _css.Length; i++)
		{
			depth += _css[i] == '{' ? 1 : _css[i] == '}' ? -1 : 0;
			if (depth == 0 && _css[i] == '}')
			{
				return _css[at..(i + 1)];
			}
		}

		throw new InvalidOperationException(start + " has no closing brace");
	}
}
