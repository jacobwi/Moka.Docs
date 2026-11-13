using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Engine.Tests.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;
using static Moka.Docs.Engine.Tests.Theming.ThemeColors;

namespace Moka.Docs.Engine.Tests.Rendering;

public sealed class PrimaryColorContrastTests
{
	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");

	public static TheoryData<string> Presets() =>
		["ocean", "emerald", "violet", "amber", "rose", "moka-red", "slate", "teal", "indigo", "orange", "graphite"];

	[Theory]
	[InlineData("#0ea5e9", "#0076a9", "#0ea5e9")]
	[InlineData("#facc15", "#876c00", "#facc15")]
	[InlineData("#64748b", "#607087", "#8090a8")]
	[InlineData("#e11d48", "#da0d42", "#ff4c64")]
	public void For_KnownColors_GivesTheirTextShades(string primary, string light, string dark)
	{
		PrimaryTextColors colors = PrimaryColorContrast.For(primary)!;

		colors.Light.Should().Be(light);
		colors.Dark.Should().Be(dark);
		colors.LightHover.Should().Be(ToHex(Mix(Hex(light), 0.8, Black)));
		colors.DarkHover.Should().Be(ToHex(Mix(Hex(dark), 0.75, White)));
	}

	[Theory]
	[InlineData("#1e3a8a", false)]
	[InlineData("#52525b", false)]
	[InlineData("#fde68a", true)]
	[InlineData("#0ea5e9", true)]
	public void For_ColorThatAlreadyPasses_IsUsedAsItIs(string primary, bool dark)
	{
		PrimaryTextColors colors = PrimaryColorContrast.For(primary)!;

		(dark ? colors.Dark : colors.Light).Should().Be(primary);
	}

	[Theory]
	[InlineData("#facc15")]
	[InlineData("#22d3ee")]
	[InlineData("#a3e635")]
	[InlineData("#1e3a8a")]
	[InlineData("#000000")]
	[InlineData("#ffffff")]
	[InlineData("#ff00ff")]
	[InlineData("#7c3aed")]
	public void For_AnyColor_MeetsAaOnEveryAccentBackgroundAndKeepsItsHue(string primary)
	{
		PrimaryTextColors colors = PrimaryColorContrast.For(primary)!;
		double[] color = Hex(primary);
		string[] admonitions = ["#3b82f6", "#22c55e", "#f59e0b", "#ef4444", "#8b5cf6"];

		foreach ((string text, double[] page, string code) in new[] { (colors.Light, White, "#f1f5f9"), (colors.Dark, Hex("#0f172a"), "#1e293b") })
		{
			IEnumerable<double[]> backgrounds = new[] { Hex(code), Mix(color, 0.08, page) }.Concat(admonitions.Select(a => Mix(Hex(a), 0.08, page)));
			foreach (double[] background in backgrounds)
			{
				Contrast(Hex(text), background).Should().BeGreaterThanOrEqualTo(4.5, "{0} as text ({1})", primary, text);
			}

			if (Hex(text).Max() - Hex(text).Min() > 0.15)
			{
				double turn = Math.Abs(OklchHue(Hex(text)) - OklchHue(color));
				Math.Min(turn, 360 - turn).Should().BeLessThan(4, "the shade keeps {0}'s hue", primary);
			}
		}
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void Presets_InTheStylesheet_AreThisComputation(string preset)
	{
		// The stylesheet's preset shades and a custom primaryColor's shades come from one method.
		string primary = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;
		(double[] light, double[] dark) = EmbeddedThemeAccessibilityTests.TextShades(preset);
		PrimaryTextColors colors = PrimaryColorContrast.For(primary)!;

		colors.Light.Should().Be(ToHex(light));
		colors.Dark.Should().Be(ToHex(dark));
	}

	[Theory]
	[InlineData("#6366f1", "#6366f1")]
	[InlineData("#63F", "#6633ff")]
	[InlineData("#6366f1ff", "#6366f1")]
	[InlineData("rgb(99, 102, 241)", "#6366f1")]
	[InlineData("rgb(99 102 241)", "#6366f1")]
	[InlineData("rgba(99, 102, 241, 1)", "#6366f1")]
	[InlineData("rgb(100% 0% 0%)", "#ff0000")]
	[InlineData("hsl(120, 100%, 25%)", "#008000")]
	[InlineData("hsl(0.5turn 100% 50%)", "#00ffff")]
	[InlineData("RebeccaPurple", "#663399")]
	[InlineData("slategrey", "#708090")]
	[InlineData("#6366f180", null)]
	[InlineData("rgb(0 0 0 / 50%)", null)]
	[InlineData("oklch(0.6 0.2 250)", null)]
	[InlineData("var(--brand)", null)]
	[InlineData("transparent", null)]
	[InlineData("window", null)]
	[InlineData("notacolor", null)]
	[InlineData("#12345", null)]
	public void Parse_ReadsHexRgbHslAndNamedColors(string css, string? expected)
	{
		double[]? rgb = PrimaryColorContrast.Parse(css);

		(rgb is null ? null : ToHex(rgb)).Should().Be(expected);
	}

	[Fact]
	public void For_ColorItCannotRead_GivesNothing()
	{
		PrimaryColorContrast.For("oklch(0.6 0.2 250)").Should().BeNull();
		PrimaryColorContrast.For("").Should().BeNull();
		PrimaryColorContrast.For(null).Should().BeNull();
	}

	private static string ToHex(double[] rgb) =>
		"#" + string.Concat(rgb.Select(c => ((int)Math.Round(c * 255, MidpointRounding.AwayFromZero)).ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
}
