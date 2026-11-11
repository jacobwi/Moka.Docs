using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Sidebar rules of the default theme's stylesheet and script that a big sidebar depends on.
/// </summary>
public sealed class SidebarThemeRulesTests
{
	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	[Fact]
	public void OpenLists_HaveNoHeightCap()
	{
		// An open list had max-height: 2000px with overflow hidden, so a section of more than about
		// 60 entries was cut off, and a namespace of 50 types inside an open API section too.
		MatchCollection rules = Regex.Matches(_css, @"\.nav-children\.expanded \{(?<body>[^}]*)\}");

		rules.Should().NotBeEmpty();
		rules.Select(r => r.Groups["body"].Value).Should().AllSatisfy(body =>
			Regex.Match(body, @"max-height:\s*([^;]+);").Groups[1].Value.Trim().Should().BeOneOf("none", ""));
	}

	[Fact]
	public void Toggle_AnimatesWithAPixelHeightAndThenDropsIt()
	{
		string toggle = Regex.Match(_js, @"toggle\.addEventListener\('click', \(e\) => \{.*?\}\);", RegexOptions.Singleline).Value;

		toggle.Should().Contain("children.style.maxHeight = children.scrollHeight + 'px';");
		toggle.Should().MatchRegex(@"setTimeout\(\(\) => \{ children\.style\.maxHeight = ''; \}, \d+\)");
	}

	[Fact]
	public void OpeningTheMobileMenu_ScrollsToTheCurrentPage()
	{
		// The sidebar is hidden while a phone loads the page, so nothing scrolled it then.
		_js.Should().MatchRegex(
			@"if \(sidebar\.classList\.toggle\('open'\)\) sidebar\.querySelector\('\.nav-link\.current'\)\?\.scrollIntoView\(");
	}
}
