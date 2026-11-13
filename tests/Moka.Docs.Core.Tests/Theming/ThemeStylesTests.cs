using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class ThemeStylesTests
{
	[Theory]
	[InlineData("moka", "moka")]
	[InlineData("Classic", "classic")]
	[InlineData("  soft ", "soft")]
	[InlineData("MINIMAL", "minimal")]
	[InlineData("default", "default")]
	public void Resolve_KnownName_ReturnsTheStyle(string configured, string expected)
	{
		ThemeStyles.Resolve(configured).Should().Be(expected);
		ThemeStyles.IsKnown(configured).Should().BeTrue();
	}

	[Theory]
	[InlineData("fancy")]
	[InlineData("moka-red")]
	[InlineData("moka minimal")]
	public void Resolve_UnknownName_FallsBackToDefault(string configured)
	{
		ThemeStyles.Resolve(configured).Should().Be(ThemeStyles.Default);
		ThemeStyles.IsKnown(configured).Should().BeFalse();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Resolve_EmptyName_IsTheDefaultAndNotReported(string? configured)
	{
		ThemeStyles.Resolve(configured).Should().Be(ThemeStyles.Default);
		ThemeStyles.IsKnown(configured).Should().BeTrue();
	}

	[Fact]
	public void Names_StartWithTheDefaultStyle()
	{
		ThemeStyles.Names.Should().Equal("default", "moka", "minimal", "classic", "soft", "terminal", "paper", "contrast", "glass", "brutal");
	}

	[Fact]
	public void DarkFirst_IsMokaAndTerminal()
	{
		ThemeStyles.All.Where(s => s.DarkFirst).Select(s => s.Name).Should().Equal(ThemeStyles.Moka, ThemeStyles.Terminal);
		ThemeStyles.All.Should().OnlyContain(s => s.Label.Length > 0 && s.Description.Length > 0);
		ThemeStyles.Find(" Glass ")!.Label.Should().Be("Glass");
	}

	[Theory]
	[InlineData("Teal", true, "teal")]
	[InlineData(" moka-red ", true, "moka-red")]
	[InlineData("neon", false, ColorPresets.Default)]
	[InlineData("", true, ColorPresets.Default)]
	public void ColorPresets_ResolveLikeTheStyles(string configured, bool known, string expected)
	{
		ColorPresets.IsKnown(configured).Should().Be(known);
		ColorPresets.Resolve(configured).Should().Be(expected);
	}

	[Fact]
	public void ColorPresets_HaveAHexSwatchEach()
	{
		ColorPresets.Names.Should().Equal("ocean", "emerald", "violet", "amber", "rose", "moka-red", "slate", "teal", "indigo", "orange", "graphite");
		ColorPresets.All.Should().OnlyContain(p => System.Text.RegularExpressions.Regex.IsMatch(p.Swatch, "^#[0-9a-f]{6}$"));
	}

	[Theory]
	[InlineData("GitHub-Dark", true, "github-dark")]
	[InlineData("night-owl", true, "night-owl")]
	[InlineData("solarised", false, CodeThemes.Default)]
	[InlineData(null, true, CodeThemes.Default)]
	public void CodeThemes_ResolveLikeTheStyles(string? configured, bool known, string expected)
	{
		CodeThemes.IsKnown(configured).Should().Be(known);
		CodeThemes.Resolve(configured).Should().Be(expected);
	}

	[Fact]
	public void CodeThemes_PairEveryLightThemeWithADarkOne()
	{
		CodeThemes.Names.Should().HaveCount(16).And.OnlyHaveUniqueItems().And.StartWith(CodeThemes.Default);
		foreach (CodeThemeInfo light in CodeThemes.All.Where(t => !t.IsDark))
		{
			CodeThemeInfo dark = CodeThemes.Find(light.Counterpart)!;
			dark.IsDark.Should().BeTrue();
			dark.Counterpart.Should().Be(light.Name);
		}

		CodeThemes.Find("tokyo-night")!.Counterpart.Should().BeNull();
	}

	[Theory]
	[InlineData("VSCode", true, "vscode")]
	[InlineData("retro", false, CodeStyles.Default)]
	public void CodeStyles_ResolveLikeTheStyles(string configured, bool known, string expected)
	{
		CodeStyles.IsKnown(configured).Should().Be(known);
		CodeStyles.Resolve(configured).Should().Be(expected);
		CodeStyles.Names.Should().Equal("plain", "macos", "terminal", "vscode");
	}
}
