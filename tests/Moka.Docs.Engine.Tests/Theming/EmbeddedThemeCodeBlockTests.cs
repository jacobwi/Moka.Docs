using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the default theme's code block chrome. The behavior is CSS and script, so these
///     look for the rules that implement it.
/// </summary>
public sealed class EmbeddedThemeCodeBlockTests
{
	[Fact]
	public void LandingCodeBlocks_ShowTheCopyButtonInPlaceOfTheLanguageLabel()
	{
		// On the landing layout the Copy button was always visible and drawn over the label.
		string css = EmbeddedThemeProvider.GetCss();

		Rule(css, ".landing-content pre .copy-btn {").Should().Contain("opacity: 0;");
		css.Should().Contain(".landing-content pre:hover .copy-btn, .landing-content pre .copy-btn:focus-visible { opacity: 1; }");
		css.Should().Contain(".landing-content pre:hover .code-lang, .landing-content pre:focus-within .code-lang { opacity: 0;");
	}

	[Fact]
	public void PageCodeBlocks_ShowTheCopyButtonOnTouchScreens()
	{
		// Without hover the button stayed at opacity 0: invisible, but still under the finger.
		string css = EmbeddedThemeProvider.GetCss();

		css.Should().Contain("@media (hover: none)");
		css.Should().Contain(".page-content pre .copy-btn:focus-visible { opacity: 1; }");
	}

	[Fact]
	public void LineNumbers_AreOnlyAddedWhereTheThemeStylesThem()
	{
		// Every <pre> got line numbers. Where no CSS positioned them they ran into the code as
		// text, as in the landing page's configuration example ("MyLib.csproj12345678").
		EmbeddedThemeProvider.GetJs().Should().Contain("block.closest('.page-content, .landing-content')");
	}

	private static string Rule(string css, string selector)
	{
		int start = css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the theme should have a rule for {selector}");
		return css[start..(css.IndexOf('}', start) + 1)];
	}
}
