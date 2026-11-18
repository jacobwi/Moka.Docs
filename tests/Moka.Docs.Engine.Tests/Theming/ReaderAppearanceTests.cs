using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using static Moka.Docs.Engine.Tests.Theming.ThemeColors;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The Appearance panel's reader options that change colors and shapes: the dark tone, the accent strength,
///     corners, line spacing and focus mode. Contrast is computed from the stylesheets' own values for every style,
///     color preset, mode, strength and tone.
/// </summary>
public sealed class ReaderAppearanceTests
{
	private static readonly string _main = ThemeCss.Main;

	public static TheoryData<string> Styles() => new(ThemeStyles.Names);

	[Theory]
	[MemberData(nameof(Styles))]
	public void AccentText_MeetsAaWithEveryStrengthPresetModeAndTone(string style)
	{
		// Links, the current sidebar entry, step numbers and buttons use the accent as text or as a fill under
		// --st-on-fill / --color-on-primary. Subtle and vivid move it, the tones move what it sits on. A style names the
		// tints of the accent that hold accent text --<prefix>-tint (soft's current sidebar entry and step numbers).
		var failures = new List<string>();
		foreach (bool dark in new[] { false, true })
		{
			Palette palette = Palette.For(style, dark);
			foreach (string preset in ColorPresets.Names)
			{
				double[] primary = Hex(PresetPrimary(preset));
				foreach (string strength in new[] { "default", "subtle", "vivid" })
				{
					double[] accentPrimary = strength == "subtle" ? MixOklab(primary, Subtle(), Hex(palette.Text)) : primary;
					double[] accent = palette.Accent(preset, accentPrimary, strength);
					foreach ((string tone, Func<double[], bool, double[]> toned) in Tones(dark))
					{
						double[] page = toned(palette.Surface(palette.Page, accentPrimary), true);
						List<(string Name, double[] Color)> backgrounds =
						[
							("page", page),
							("panel", toned(palette.Surface(palette.Panel, accentPrimary), false)),
							("code", toned(palette.Surface(palette.Code, accentPrimary), false)),
							("current link", Mix(accentPrimary, 0.08, page))
						];
						backgrounds.AddRange(palette.Tints.Select(t => (t.Key, palette.Surface(t.Value, accentPrimary, page))));
						if (style == ThemeStyles.Default)
						{
							backgrounds.AddRange(Admonitions().Select(a => ("admonition " + a.Box, Mix(Hex(a.Box), 0.08, page))));
						}

						foreach ((string name, double[] background) in backgrounds)
						{
							Check(failures, Contrast(accent, background), $"{(dark ? "dark" : "light")} {preset} {strength} {tone}: accent on {name}");
						}

						Check(failures, Contrast(palette.Surface(palette.OnFill, accentPrimary), accent), $"{(dark ? "dark" : "light")} {preset} {strength} {tone}: text on the accent fill");
					}
				}
			}
		}

		failures.Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void DarkTones_KeepEveryTextColorAtAa(string style)
	{
		// Dim lifts the dark surfaces and black lowers them; the style's text, its semantic colors (badges, admonitions,
		// REST methods) and the default style's badge and admonition shades must still read on them.
		Palette palette = Palette.For(style, true);
		var failures = new List<string>();
		foreach ((string tone, Func<double[], bool, double[]> toned) in Tones(true))
		{
			double[] page = toned(palette.Surface(palette.Page, Hex(PresetPrimary("ocean"))), true);
			double[] panel = toned(palette.Surface(palette.Panel, Hex(PresetPrimary("ocean"))), false);
			double[] code = toned(palette.Surface(palette.Code, Hex(PresetPrimary("ocean"))), false);
			foreach ((string role, string text) in new[] { ("text", palette.Text), ("secondary text", palette.Secondary), ("muted text", palette.Muted) })
			{
				foreach ((string name, double[] background) in new[] { ("page", page), ("panel", panel), ("code", code) })
				{
					Check(failures, Contrast(Hex(text), background), $"{tone}: {role} on {name}");
				}
			}

			if (style == ThemeStyles.Default)
			{
				foreach ((string box, string text) in Admonitions())
				{
					Check(failures, Contrast(Hex(text), Mix(Hex(box), 0.08, page)), $"{tone}: admonition title {text}");
				}

				foreach ((string tint, double amount, string text) in Badges())
				{
					Check(failures, Contrast(Hex(text), Mix(Hex(tint), amount, page)), $"{tone}: badge {text}");
				}
			}
			else
			{
				foreach (string color in SemanticColors(style))
				{
					Check(failures, Contrast(Mix(Hex(color), palette.Hue("ocean"), Hex(palette.Ink)), Mix(Hex(color), 0.12, page)), $"{tone}: semantic {color}");
				}
			}
		}

		failures.Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void DarkTone_FollowsTheStylesOwnSurfaces(string style)
	{
		// The tone can only mix a style's dark colors when they are named --dark-bg, --dark-bg-secondary and
		// --dark-bg-code. A style that set --color-bg in its dark block would keep its colors and ignore the tone.
		string dark = style == ThemeStyles.Default ? Block(_main, "[data-theme=\"dark\"] {") : Blocks(ThemeCss.Style(style), $"html[data-style=\"{style}\"][data-theme=\"dark\"] {{");

		dark.Should().Contain("--dark-bg:").And.Contain("--dark-bg-secondary:").And.Contain("--dark-bg-code:");
		Regex.IsMatch(dark, @"--color-bg(-secondary|-code)?:").Should().BeFalse();
		_main.Should().Contain("html:root[data-theme=\"dark\"] {\n    --color-bg: var(--dark-bg);\n    --color-bg-secondary: var(--dark-bg-secondary);\n    --color-bg-code: var(--dark-bg-code);\n}");
	}

	[Fact]
	public void DarkTone_DimIsSofterAndBlackIsBlack()
	{
		// Dim lifts the page; black puts it at #000 with the panels a little above it.
		Palette palette = Palette.For(ThemeStyles.Default, true);
		double[] ocean = Hex(PresetPrimary("ocean"));
		Dictionary<string, Func<double[], bool, double[]>> tones = Tones(true).ToDictionary(t => t.Name, t => t.Apply);
		double[] page = palette.Surface(palette.Page, ocean);
		double[] panel = palette.Surface(palette.Panel, ocean);

		Luminance(tones["dim"](page, true)).Should().BeGreaterThan(Luminance(page));
		Luminance(tones["dim"](panel, false)).Should().BeGreaterThan(Luminance(tones["dim"](page, true)), "panels stay a step lighter");
		tones["black"](page, true).Should().Equal(0, 0, 0);
		Luminance(tones["black"](panel, false)).Should().BeInRange(0.0005, Luminance(panel));
	}

	[Fact]
	public void Corners_WinOverEveryStylesOwnRadii()
	{
		// :root lifts the reader's corners above html[data-style], the only place a style sets its radii, so square
		// styles get round corners too and soft and glass go square.
		_main.Should().Contain("html:root[data-corners=\"sharp\"] { --radius: 0px; --radius-sm: 0px; --radius-md: 0px; --radius-scale: 0; }")
			.And.Contain("html:root[data-corners=\"round\"] { --radius: 14px; --radius-sm: 10px; --radius-md: 16px; }")
			.And.Contain("    --radius-scale: 1;");
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			string css = ThemeCss.Style(style);
			foreach (Match declaration in Regex.Matches(css, @"\n(?<selector>[^{};\n]*)\{[^}]*--radius(-sm|-md)?:"))
			{
				declaration.Groups["selector"].Value.Trim().Should().Be($"html[data-style=\"{style}\"]", "a style sets its radii only in its own block");
			}

			// A style's own radius variable counts when it is worked out from the radii (tonal's pill, --radius * 999).
			string[] derived = [.. Regex.Matches(css, @"(--[\w-]+): [^;]*var\(--radius").Select(m => "var(" + m.Groups[1].Value + ")")];
			foreach (Match radius in Regex.Matches(css, @"border-radius:\s*(?<value>[^;!}]+)"))
			{
				string value = radius.Groups["value"].Value.Trim();
				bool follows = value.Contains("var(--radius", StringComparison.Ordinal) || value is "0" or "50%" || derived.Any(value.Contains);
				follows.Should().BeTrue("{0} writes border-radius: {1}, which the corners choice can't change", style, value);
			}
		}

		// A square style takes its corners from the variables, which are 0 there, so round wins. Only edges that meet
		// another box stay 0: a tab row or code chrome inside a frame, a progress line, a box drawn as a single rule.
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default && Blocks(ThemeCss.Style(s), $"html[data-style=\"{s}\"] {{").Contains("--radius: 0px;", StringComparison.Ordinal)))
		{
			foreach (Match zero in Regex.Matches(ThemeCss.Style(style), @"\n(?<selector>[^{}\n]*)\{(?<body>[^}]*border-radius: 0[; ][^}]*)\}"))
			{
				bool edge = new[] { "blockquote", ".tab-headers", ".install-header", ".code-lang", ".toc-progress" }.Any(zero.Groups["selector"].Value.Contains)
				            || Regex.IsMatch(zero.Groups["body"].Value, @"border: none|border-(top|right|bottom|left): ");
				edge.Should().BeTrue("{0} squares {1} itself, so round corners can't reach it", style, zero.Groups["selector"].Value.Trim());
			}
		}
	}

	[Fact]
	public void LineSpacing_ScalesTheArticlesParagraphsAndListsInEveryStyle()
	{
		// Paragraph and list item line heights multiply by --line-spacing and their margins by --paragraph-spacing,
		// so tight and relaxed move every style's own values. Headings, tables and code keep theirs.
		_main.Should().Contain("html:root[data-line-spacing=\"tight\"] { --line-spacing: 0.88; --paragraph-spacing: 0.7; }")
			.And.Contain("html:root[data-line-spacing=\"relaxed\"] { --line-spacing: 1.1; --paragraph-spacing: 1.35; }")
			.And.Contain("    --line-spacing: 1;\n    --paragraph-spacing: 1;");
		string css = ThemeCss.All;
		MatchCollection rules = Regex.Matches(css, @"(?<selector>[^{}]*\.page-content (p|li|ul, \.page-content ol|:is\(p, ul, ol\)))\s*\{(?<body>[^}]*)\}");
		rules.Count.Should().BeGreaterThan(10);
		foreach (Match rule in rules)
		{
			string body = rule.Groups["body"].Value;
			foreach (Match lineHeight in Regex.Matches(body, @"line-height:\s*([^;]+);"))
			{
				lineHeight.Groups[1].Value.Should().Contain("* var(--line-spacing)", rule.Groups["selector"].Value.Trim());
			}

			foreach (Match margin in Regex.Matches(body, @"margin-bottom:\s*([^;]+);"))
			{
				margin.Groups[1].Value.Should().Contain("* var(--paragraph-spacing)", rule.Groups["selector"].Value.Trim());
			}
		}
	}

	[Fact]
	public void FocusMode_HidesTheNavigationOnScreensOnlyAndOffersAWayOut()
	{
		// Print looks the same with focus mode on: the rules sit in a screen media query.
		string screen = Regex.Match(_main, @"\.focus-exit \{ display: none; \}\n@media screen \{\n(?<rules>.*?)\n\}", RegexOptions.Singleline).Groups["rules"].Value;

		screen.Should().Contain("html[data-focus-mode=\"on\"] :is(.sidebar, .sidebar-toggle, .toc-sidebar, .toc-inline, .toc-dropdown, .breadcrumbs, .header-tabs, .back-to-top, .toc-toggle-btn, .mobile-nav-toggle, .drawer-backdrop) { display: none !important; }");
		screen.Should().Contain("html:root[data-focus-mode=\"on\"] { --tabs-height: 0px; --toc-dropdown-offset: 0px; }", "the tabs row and the \"On this page\" bar give back the room they reserved under the header");
		screen.Should().Contain("html[data-focus-mode=\"on\"] .content { margin-inline: auto; }");
		screen.Should().Contain("inset-inline-end: var(--fab-right);", "the exit button sits at the end of the line in both directions");
		Regex.Match(_main, @"@media \(forced-colors: active\) \{.*?\n\}", RegexOptions.Singleline).Value.Should().Contain(".focus-exit");
		Regex.IsMatch(_main, @"@media print \{[^@]*\.focus-exit").Should().BeFalse("the button is hidden outside screens already");
	}

	[Fact]
	public void DarkToneSection_ShowsOnlyInDarkMode()
	{
		_main.Should().Contain("html:not([data-theme=\"dark\"]) .appearance-dark-only { display: none; }");
	}

	private static void Check(List<string> failures, double contrast, string what)
	{
		if (contrast < 4.5)
		{
			failures.Add($"{what}: {contrast.ToString("0.00", CultureInfo.InvariantCulture)}:1");
		}
	}

	private static double Luminance(double[] rgb) => (Contrast(rgb, Black) * 0.05) - 0.05;

	private static double Subtle() =>
		Percent(Regex.Match(_main, @"html:root\[data-accent-strength=""subtle""\] \{\n    --color-primary: color-mix\(in oklab, var\(--color-primary-base\) (\d+)%, var\(--color-text\)\);").Groups[1].Value);

	// The default tone, then dim and black as html[data-dark-tone] mixes the page (true) and the panels (false).
	private static IEnumerable<(string Name, Func<double[], bool, double[]> Apply)> Tones(bool dark)
	{
		yield return ("dark", (color, _) => color);
		if (!dark)
		{
			yield break;
		}

		Match dim = Regex.Match(_main, @"html\[data-theme=""dark""\]\[data-dark-tone=""dim""\] \{\n    --color-bg: color-mix\(in oklab, var\(--dark-bg\) (\d+)%, (#[0-9a-f]{6})\);\n    --color-bg-secondary: color-mix\(in oklab, var\(--dark-bg-secondary\) (\d+)%, (#[0-9a-f]{6})\);");
		Match black = Regex.Match(_main, @"html\[data-theme=""dark""\]\[data-dark-tone=""black""\] \{\n    --color-bg: (#[0-9a-f]{6});\n    --color-bg-secondary: color-mix\(in oklab, var\(--dark-bg-secondary\) (\d+)%, (#[0-9a-f]{6})\);");
		dim.Success.Should().BeTrue();
		black.Success.Should().BeTrue();
		yield return ("dim", (color, page) => page
			? MixOklab(color, Percent(dim.Groups[1].Value), Hex(dim.Groups[2].Value))
			: MixOklab(color, Percent(dim.Groups[3].Value), Hex(dim.Groups[4].Value)));
		yield return ("black", (color, page) => page ? Hex(black.Groups[1].Value) : MixOklab(color, Percent(black.Groups[2].Value), Hex(black.Groups[3].Value)));
	}

	private static string PresetPrimary(string preset) =>
		Regex.Match(_main, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary-base: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;

	private static IEnumerable<(string Box, string Text)> Admonitions()
	{
		string dark = Block(_main, "[data-theme=\"dark\"] {\n    --admonition-note-text");
		foreach (string kind in new[] { "note", "tip", "warning", "danger", "info" })
		{
			string box = Regex.Match(_main, $@"\.page-content \.admonition-{kind} \{{ border-color: (#[0-9a-f]{{6}});").Groups[1].Value;
			yield return (box, Regex.Match(dark, $@"--admonition-{kind}-text: (#[0-9a-f]{{6}});").Groups[1].Value);
		}
	}

	private static IEnumerable<(string Tint, double Amount, string Text)> Badges()
	{
		string dark = Block(_main, "[data-theme=\"dark\"] {\n    --badge-blue");
		foreach (Match rule in Regex.Matches(_main, @"\.api-badge[\w.-]* \{ background: color-mix\(in srgb, (#[0-9a-f]{6}) (\d+)%, var\(--color-bg\)\); color: var\((--badge-[a-z]+)\); \}"))
		{
			yield return (rule.Groups[1].Value, Percent(rule.Groups[2].Value), Regex.Match(dark, Regex.Escape(rule.Groups[3].Value) + @": (#[0-9a-f]{6});").Groups[1].Value);
		}
	}

	private static IEnumerable<string> SemanticColors(string style) =>
		Regex.Matches(ThemeCss.Style(style), @"--st-c: (#[0-9a-f]{6});").Select(m => m.Groups[1].Value).Distinct();

	private static double Percent(string value) => double.Parse(value, CultureInfo.InvariantCulture) / 100;

	private static string Block(string css, string start)
	{
		int at = css.IndexOf("\n" + start, StringComparison.Ordinal);
		at.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have {0}", start);
		return css[(at + 1)..(css.IndexOf('}', at) + 1)];
	}

	// Every rule with exactly this selector, one after the other: a style's file has the shared tokens first.
	private static string Blocks(string css, string selector)
	{
		var rules = new List<string>();
		for (int at = css.IndexOf("\n" + selector, StringComparison.Ordinal); at >= 0; at = css.IndexOf("\n" + selector, at + 1, StringComparison.Ordinal))
		{
			rules.Add(css[(at + 1)..(css.IndexOf('}', at) + 1)]);
		}

		rules.Should().NotBeEmpty("the stylesheet should have {0}", selector);
		return string.Join("\n", rules);
	}

	private static Dictionary<string, string> Variables(string block) =>
		Regex.Matches(block, @"(--[\w-]+): ([^;]+);").GroupBy(m => m.Groups[1].Value).ToDictionary(g => g.Key, g => g.Last().Groups[2].Value.Trim());

	// A style's colors in one mode, as its stylesheet writes them.
	private sealed record Palette(string Style, bool Dark, string Page, string Panel, string Code, string Text, string Secondary, string Muted, string OnFill, string Ink, string HueValue, string? GraphiteHue, double VividChroma, Dictionary<string, string> Tints)
	{
		public static Palette For(string style, bool dark)
		{
			if (style == ThemeStyles.Default)
			{
				Dictionary<string, string> root = Variables(Block(_main, ":root {\n    --color-primary-base"));
				Dictionary<string, string> night = Variables(Block(_main, "[data-theme=\"dark\"] {\n    --dark-bg"));
				Dictionary<string, string> v = dark ? night : root;
				return new Palette(style, dark, v[dark ? "--dark-bg" : "--color-bg"], v[dark ? "--dark-bg-secondary" : "--color-bg-secondary"], v[dark ? "--dark-bg-code" : "--color-bg-code"],
					v["--color-text"], v["--color-text-secondary"], v["--color-text-muted"], dark ? "#0f172a" : "#ffffff", "", "", null, 1, []);
			}

			string css = ThemeCss.Style(style);
			Dictionary<string, string> light = Variables(Blocks(css, $"html[data-style=\"{style}\"] {{"));
			Dictionary<string, string> both = dark
				? light.Concat(Variables(Blocks(css, $"html[data-style=\"{style}\"][data-theme=\"dark\"] {{"))).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Last().Value)
				: light;
			string? graphite = dark ? Regex.Match(css, $@"html\[data-style=""{style}""\]\[data-theme=""dark""\]\[data-color-theme=""graphite""\] \{{ --st-hue: (\d+%); \}}") is { Success: true } g ? g.Groups[1].Value : null : null;
			double vivid = double.Parse(Regex.Match(css, @"\[data-accent-strength=""vivid""\] \{ --st-link: oklch\(from color-mix\(in srgb, var\(--color-primary\) var\(--st-hue\), var\(--st-ink\)\) l calc\(c \* ([\d.]+)\) h\); \}").Groups[1].Value, CultureInfo.InvariantCulture);
			return new Palette(style, dark, both[dark ? "--dark-bg" : "--color-bg"], both[dark ? "--dark-bg-secondary" : "--color-bg-secondary"], both[dark ? "--dark-bg-code" : "--color-bg-code"],
				both["--color-text"], both["--color-text-secondary"], both["--color-text-muted"], both["--st-on-fill"], both["--st-ink"], both["--st-hue"], graphite, vivid,
				both.Where(p => Regex.IsMatch(p.Key, @"^--[a-z]+-tint")).ToDictionary(p => p.Key, p => p.Value));
		}

		public double Hue(string preset) => Percent((preset == "graphite" && GraphiteHue is not null ? GraphiteHue : HueValue).TrimEnd('%'));

		// A surface is a color, or the style's color-mix of the primary into one or into the page.
		public double[] Surface(string value, double[] primary, double[]? page = null)
		{
			if (value.StartsWith('#'))
			{
				return Hex(value);
			}

			Match mix = Regex.Match(value, @"^color-mix\(in srgb, var\(--color-primary\) (\d+)%, (#[0-9a-f]{6}|var\(--color-bg\))\)$");
			mix.Success.Should().BeTrue("{0} is a color or a mix of the primary", value);
			double[] under = mix.Groups[2].Value.StartsWith('#') ? Hex(mix.Groups[2].Value) : page ?? throw new ArgumentNullException(nameof(page));
			return ThemeColors.Mix(primary, Percent(mix.Groups[1].Value), under);
		}

		// The accent as text: the default style's shade for the preset, a style's --st-link.
		public double[] Accent(string preset, double[] primary, string strength)
		{
			if (Style == ThemeStyles.Default)
			{
				(double[] light, double[] dark) = strength == "vivid" ? EmbeddedThemeAccessibilityTests.VividTextShades(preset) : EmbeddedThemeAccessibilityTests.TextShades(preset);
				double[] shade = Dark ? dark : light;
				return strength == "subtle" ? MixOklab(shade, Subtle(), Hex(Text)) : shade;
			}

			double[] link = ThemeColors.Mix(primary, Hue(preset), Hex(Ink));
			return strength == "vivid" ? ScaleChroma(link, VividChroma) : link;
		}
	}
}
