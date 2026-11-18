using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;
using static Moka.Docs.Engine.Tests.Theming.ThemeColors;

namespace Moka.Docs.Engine.Tests.Theming;

public sealed class EmbeddedThemeAccessibilityTests
{
	private static readonly string _css = ThemeCss.All;
	private static readonly double[] _darkPage = Hex("#0f172a");

	public static TheoryData<string> Presets() =>
		new(global::Moka.Docs.Core.Theming.ColorPresets.Names);

	public static TheoryData<string> Styles() =>
		new(ThemeStyles.Names.Where(s => s != ThemeStyles.Default));

	private static readonly string[] _semanticColors = ["#3b82f6", "#22c55e", "#8b5cf6", "#06b6d4", "#f59e0b", "#ec4899", "#64748b", "#a78bfa", "#f97316", "#ef4444"];

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_KeepsItsTextAccentAndFillsAtAaWithEveryPresetInBothModes(string style)
	{
		// The colors come from the style's own stylesheet with the preset's primary filled in, so a style that mixes its
		// surfaces or accent from the primary is checked against all eleven presets.
		var failures = new List<string>();
		foreach ((string context, Func<string, double[]> color) in StyleContexts(style))
		{
			void Check(string text, string background)
			{
				double ratio = Contrast(color(text), color(background));
				if (ratio < 4.5)
				{
					failures.Add($"{context}: {text} on {background} is {ratio:0.00}:1");
				}
			}

			foreach (string surface in new[] { "var(--color-bg)", "var(--color-bg-secondary)", "var(--color-bg-code)" })
			{
				Check("var(--color-text)", surface);
				Check("var(--color-text-secondary)", surface);
				Check("var(--st-link)", surface);
			}

			Check("var(--color-text-muted)", "var(--color-bg)");
			Check("var(--color-text-muted)", "var(--color-bg-secondary)");
			Check("var(--st-on-fill)", "var(--st-link)");
			foreach (string semantic in _semanticColors)
			{
				Check($"color-mix(in srgb, {semantic} var(--st-hue), var(--st-ink))", "var(--color-bg)");
				Check($"color-mix(in srgb, {semantic} var(--st-hue), var(--st-ink))", $"color-mix(in srgb, {semantic} 12%, var(--color-bg))");
			}
		}

		failures.Should().BeEmpty();
	}

	[Theory]
	[InlineData("neon", "var(--ne-pink)", "var(--color-bg)")]
	[InlineData("neon", "var(--ne-pink)", "var(--color-bg-secondary)")]
	[InlineData("neon", "var(--ne-pink)", "var(--ne-pink-tint)")]
	[InlineData("neon", "var(--ne-cyan)", "var(--color-bg)")]
	[InlineData("neon", "var(--ne-cyan)", "var(--color-bg-secondary)")]
	[InlineData("neon", "var(--ne-cyan)", "var(--color-bg-code)")]
	[InlineData("neon", "var(--ne-cyan)", "color-mix(in srgb, var(--ne-cyan) 8%, var(--color-bg))")]
	[InlineData("neon", "var(--color-text)", "var(--ne-hover)")]
	[InlineData("neon", "var(--color-text-muted)", "var(--ne-hover)")]
	[InlineData("tonal", "var(--tn-on-container)", "var(--tn-container)")]
	[InlineData("tonal", "var(--tn-on-container)", "color-mix(in srgb, var(--tn-on-container) 8%, var(--tn-container))")]
	[InlineData("tonal", "var(--color-text-secondary)", "var(--tn-container)")]
	[InlineData("tonal", "var(--st-link)", "var(--tn-container)")]
	[InlineData("tonal", "var(--color-text)", "var(--tn-raised)")]
	[InlineData("tonal", "var(--color-text-secondary)", "var(--tn-raised)")]
	[InlineData("tonal", "var(--color-text-muted)", "var(--tn-raised)")]
	[InlineData("tonal", "var(--st-link)", "var(--tn-raised)")]
	[InlineData("tonal", "var(--color-text)", "color-mix(in srgb, var(--color-text) 8%, var(--color-bg))")]
	[InlineData("editor", "var(--ed-on-bar)", "var(--ed-bar)")]
	[InlineData("editor", "var(--color-text)", "var(--ed-select)")]
	[InlineData("editor", "var(--color-text-secondary)", "var(--ed-select)")]
	[InlineData("editor", "var(--color-text)", "var(--ed-hover)")]
	[InlineData("editor", "var(--color-text-secondary)", "var(--ed-hover)")]
	[InlineData("editor", "color-mix(in srgb, #f59e0b var(--st-hue), var(--st-ink))", "color-mix(in srgb, #f59e0b 7%, var(--color-bg-secondary))")]
	[InlineData("editor", "color-mix(in srgb, #22c55e var(--st-hue), var(--st-ink))", "color-mix(in srgb, #22c55e 7%, var(--color-bg-secondary))")]
	public void NeonTonalAndEditor_KeepTheirOwnColorsAtAaWithEveryPresetInBothModes(string style, string text, string background)
	{
		// Neon's pink and cyan text, tonal's tinted pills and raised panels, editor's status bar, selected and hovered rows
		// and its admonition title bars.
		var failures = new List<string>();
		foreach ((string context, Func<string, double[]> color) in StyleContexts(style))
		{
			double ratio = Contrast(color(text), color(background));
			if (ratio < 4.5)
			{
				failures.Add($"{context}: {ratio:0.00}:1");
			}
		}

		failures.Should().BeEmpty("{0} on {1} in {2}", text, background, style);
	}

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

	// The styles checked by StyleColors_MeetAaWithEveryPresetInBothModes: the surfaces of their own that carry text, besides
	// --color-bg, --color-bg-secondary and --color-bg-code, and their own text colors with the surfaces each sits on.
	private static readonly Dictionary<string, (string[] Surfaces, (string Text, string[] On)[] Texts)> _styleColors = new()
	{
		["blueprint"] = (["--bp-grid", "--bp-hover"], []),
		["newspaper"] = (["--np-hover"], [("--np-accent", ["--color-bg", "--color-bg-secondary", "--np-hover"])]),
		["retro"] = (["--rt-hover", "--rt-tip"], [("--rt-on-title", ["--rt-title", "--rt-title-end"])])
	};

	public static TheoryData<string, string> StylesAndPresets()
	{
		var data = new TheoryData<string, string>();
		foreach (string style in _styleColors.Keys)
		{
			foreach (string preset in global::Moka.Docs.Core.Theming.ColorPresets.Names)
			{
				data.Add(style, preset);
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(StylesAndPresets))]
	public void StyleColors_MeetAaWithEveryPresetInBothModes(string style, string preset)
	{
		// Text, the accent shade (links, the current page, fills), the semantic colors (admonition titles, badges, REST
		// methods) and the style's own text colors, on every surface of the style that carries text.
		string css = ThemeCss.Style(style);
		string presetPrimary = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary-base: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;
		double[] primary = Hex(presetPrimary);
		string[] semantic = [.. Regex.Matches(css, @"--st-c: (#[0-9a-f]{6});").Select(m => m.Groups[1].Value).Distinct()];
		Match admonition = Regex.Match(css, $@"\nhtml\[data-style=""{style}""\] \.page-content \.admonition \{{[^}}]*background: color-mix\(in srgb, var\(--st-c\) (\d+)%, var\(--color-bg\)\);");
		(string[] ownSurfaces, (string Text, string[] On)[] ownTexts) = _styleColors[style];
		semantic.Should().HaveCountGreaterThan(8);

		foreach (bool dark in new[] { false, true })
		{
			string mode = dark ? "dark" : "light";
			Dictionary<string, string> v = StyleVariables(css, style, dark);
			v["--color-primary"] = presetPrimary;
			double[] Color(string name)
			{
				v.Should().ContainKey(name, "{0} sets {1} in {2} mode", style, name, mode);
				return CssColor($"var({name})", v);
			}

			string graphite = Regex.Match(css, $@"html\[data-style=""{style}""\]\[data-theme=""dark""\]\[data-color-theme=""graphite""\] \{{ --st-hue: (\d+)%; \}}").Groups[1].Value;
			double hue = Percent(dark && preset == "graphite" ? graphite : v["--st-hue"].TrimEnd('%'));
			double[] ink = Color("--st-ink");
			double[] link = Mix(primary, hue, ink);
			Contrast(Color("--st-on-fill"), link).Should().BeGreaterThanOrEqualTo(4.5, "{0} text on the {1} accent fill in {2} mode", style, preset, mode);

			foreach (string surface in new[] { "--color-bg", "--color-bg-secondary", "--color-bg-code" }.Concat(ownSurfaces))
			{
				double[] under = Color(surface);
				foreach (string text in new[] { "--color-text", "--color-text-secondary", "--color-text-muted" })
				{
					Contrast(Color(text), under).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} on {2} in {3} mode", style, text, surface, mode);
				}

				Contrast(link, under).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} accent on {2} in {3} mode", style, preset, surface, mode);
				foreach (string color in semantic)
				{
					Contrast(Mix(Hex(color), Percent(v["--st-hue"].TrimEnd('%')), ink), under).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} text on {2} in {3} mode", style, color, surface, mode);
				}
			}

			// API badges sit on 14% of their color over the page, a tinted admonition on its own share.
			foreach (double tint in admonition.Success ? new[] { 0.14, Percent(admonition.Groups[1].Value) } : [0.14])
			{
				foreach (string color in semantic)
				{
					double[] under = Mix(Hex(color), tint, Color("--color-bg"));
					Contrast(Mix(Hex(color), Percent(v["--st-hue"].TrimEnd('%')), ink), under).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} on its {2} tint in {3} mode", style, color, tint, mode);
					Contrast(Color("--color-text"), under).Should().BeGreaterThanOrEqualTo(4.5, "{0} text on the {1} {2} tint in {3} mode", style, color, tint, mode);
				}
			}

			foreach ((string text, string[] on) in ownTexts)
			{
				foreach (string surface in on)
				{
					Contrast(Color(text), Color(surface)).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} on {2} in {3} mode", style, text, surface, mode);
				}
			}
		}
	}

	// A style's variables in one mode: its light rules in order, then its dark ones over them, and in dark mode the surfaces
	// main.css makes of the style's --dark-* sources with the default dark tone.
	private static Dictionary<string, string> StyleVariables(string css, string style, bool dark)
	{
		var variables = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string scope in dark ? new[] { "", "[data-theme=\"dark\"]" } : [""])
		{
			foreach (Match rule in Regex.Matches(css, $@"\nhtml\[data-style=""{style}""\]{Regex.Escape(scope)} \{{(?<body>[^}}]*)\}}"))
			{
				foreach (Match variable in Regex.Matches(rule.Groups["body"].Value, @"(--[\w-]+): ([^;]+);"))
				{
					variables[variable.Groups[1].Value] = variable.Groups[2].Value.Trim();
				}
			}
		}

		if (dark)
		{
			foreach (string surface in new[] { "--color-bg", "--color-bg-secondary", "--color-bg-code" })
			{
				variables[surface] = $"var({surface.Replace("--color-", "--dark-", StringComparison.Ordinal)})";
			}
		}

		return variables;
	}

	// Each mode and preset of a style, with a function that turns a color written in the style's own terms (a hex color,
	// var() of a custom property, or a color-mix in srgb or oklab of those) into the color it computes to there. The custom
	// properties are the ones the style's file sets on <html> for that mode and preset, in file order, plus the preset's
	// primary. In dark mode the surfaces are the style's --dark-* sources, as main.css maps them with the default dark tone.
	private static IEnumerable<(string Context, Func<string, double[]> Color)> StyleContexts(string style)
	{
		string css = ThemeCss.Style(style);
		string scope = $"html[data-style=\"{style}\"]";
		// More attribute selectors win over fewer, whatever the order; the shared graphite rule comes before a style's own
		// dark block and still wins.
		List<Match> rules = [.. Regex.Matches(css, $@"\n{Regex.Escape(scope)}((?:\[[\w-]+=""[\w-]+""\])*) \{{([^}}]*)\}}").OrderBy(r => r.Groups[1].Value.Count(c => c == '['))];
		rules.Should().NotBeEmpty("the {0} stylesheet sets its variables on <html>", style);
		foreach (string mode in new[] { "light", "dark" })
		{
			foreach (string preset in ColorPresets.Names)
			{
				var variables = new Dictionary<string, string>(StringComparer.Ordinal)
				{
					["--color-primary"] = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary-base: (#[0-9a-f]{{6}}) !important;").Groups[1].Value
				};
				foreach (Match rule in rules)
				{
					bool applies = Regex.Matches(rule.Groups[1].Value, @"\[([\w-]+)=""([\w-]+)""\]").All(a =>
						(a.Groups[1].Value, a.Groups[2].Value) is ("data-theme", var m) ? m == mode
						: (a.Groups[1].Value, a.Groups[2].Value) is ("data-color-theme", var p) && p == preset);
					if (!applies)
					{
						continue;
					}

					foreach (Match declaration in Regex.Matches(rule.Groups[2].Value, @"(--[\w-]+):\s*([^;]+);"))
					{
						if (declaration.Groups[1].Value != "--color-primary")
						{
							variables[declaration.Groups[1].Value] = declaration.Groups[2].Value.Trim();
						}
					}
				}

				if (mode == "dark")
				{
					foreach (string surface in new[] { "--color-bg", "--color-bg-secondary", "--color-bg-code" })
					{
						variables[surface] = $"var({surface.Replace("--color-", "--dark-", StringComparison.Ordinal)})";
					}
				}

				yield return ($"{style}/{mode}/{preset}", value => CssColor(value, variables));
			}
		}
	}

	private static double[] CssColor(string value, IReadOnlyDictionary<string, string> variables)
	{
		value = value.Trim();
		if (value.StartsWith("var(", StringComparison.Ordinal))
		{
			string name = value[4..^1].Split(',')[0].Trim();
			variables.Should().ContainKey(name);
			return CssColor(variables[name], variables);
		}

		if (value.StartsWith('#'))
		{
			return Hex(value);
		}

		bool oklab = value.StartsWith("color-mix(in oklab,", StringComparison.Ordinal);
		(oklab || value.StartsWith("color-mix(in srgb,", StringComparison.Ordinal)).Should().BeTrue("{0} is a color-mix in srgb or oklab", value);
		string[] parts = SplitTopLevel(value[(value.IndexOf(',', StringComparison.Ordinal) + 1)..^1]);
		parts.Should().HaveCount(2, "{0} mixes two colors", value);
		(string first, double? amount) = ColorAndAmount(parts[0], variables);
		(string second, double? secondAmount) = ColorAndAmount(parts[1], variables);
		double share = amount ?? 1 - (secondAmount ?? 0.5);
		return oklab
			? MixOklab(CssColor(first, variables), share, CssColor(second, variables))
			: Mix(CssColor(first, variables), share, CssColor(second, variables));
	}

	private static (string Color, double? Amount) ColorAndAmount(string part, IReadOnlyDictionary<string, string> variables)
	{
		string[] tokens = SplitTopLevel(part.Trim(), ' ');
		string amount = tokens[^1];
		if (amount.StartsWith("var(", StringComparison.Ordinal) && variables.TryGetValue(amount[4..^1], out string? value))
		{
			amount = value;
		}

		return tokens.Length > 1 && amount.EndsWith('%')
			? (string.Join(' ', tokens[..^1]), Percent(amount[..^1]))
			: (part, null);
	}

	private static string[] SplitTopLevel(string text, char separator = ',')
	{
		var parts = new List<string>();
		int depth = 0, start = 0;
		for (int i = 0; i < text.Length; i++)
		{
			depth += text[i] == '(' ? 1 : text[i] == ')' ? -1 : 0;
			if (text[i] == separator && depth == 0)
			{
				parts.Add(text[start..i]);
				start = i + 1;
			}
		}

		parts.Add(text[start..]);
		return [.. parts.Where(p => p.Length > 0)];
	}

	private static (double[] Primary, double[] Secondary, double[] Tertiary) Gradient(string preset)
	{
		Match block = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary-base: (#[0-9a-f]{{6}}) !important;[^}}]*--gradient-secondary: (#[0-9a-f]{{6}});\s*--gradient-tertiary: (#[0-9a-f]{{6}});");
		block.Success.Should().BeTrue("{0} has a preset block", preset);
		return (Hex(block.Groups[1].Value), Hex(block.Groups[2].Value), Hex(block.Groups[3].Value));
	}

	// The preset's --color-primary-text in light and dark mode: its own rule, else the primary.
	internal static (double[] Light, double[] Dark) TextShades(string preset)
	{
		string primary = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary-base: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;
		string light = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{ --color-primary-text-base: (#[0-9a-f]{{6}}); \}}") is { Success: true } l ? l.Groups[1].Value : primary;
		string dark = Regex.Match(_css, $@"html\[data-theme=""dark""\]\[data-color-theme=""{preset}""\] \{{ --color-primary-text-base: (#[0-9a-f]{{6}}); \}}") is { Success: true } d
			? d.Groups[1].Value
			: Regex.IsMatch(_css, $@"html\[data-theme=""dark""\]:is\([^)]*\[data-color-theme=""{preset}""\][^)]*\) \{{ --color-primary-text-base: var\(--color-primary-base\); \}}")
				? primary
				: light;
		return (Hex(light), Hex(dark));
	}

	// The preset's text shades with the vivid accent strength: its vivid rule where the shade changes, else the usual one.
	internal static (double[] Light, double[] Dark) VividTextShades(string preset)
	{
		(double[] light, double[] dark) = TextShades(preset);
		Match vividLight = Regex.Match(_css, $@"html\[data-accent-strength=""vivid""\]:not\(\[data-theme=""dark""\]\)\[data-color-theme=""{preset}""\] \{{ --color-primary-text-base: (#[0-9a-f]{{6}}); \}}");
		Match vividDark = Regex.Match(_css, $@"html\[data-accent-strength=""vivid""\]\[data-theme=""dark""\]\[data-color-theme=""{preset}""\] \{{ --color-primary-text-base: (#[0-9a-f]{{6}}); \}}");
		return (vividLight.Success ? Hex(vividLight.Groups[1].Value) : light, vividDark.Success ? Hex(vividDark.Groups[1].Value) : dark);
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
