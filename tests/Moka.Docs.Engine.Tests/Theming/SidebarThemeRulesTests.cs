using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Sidebar rules of the default theme's stylesheet and script that a big sidebar depends on.
/// </summary>
public sealed class SidebarThemeRulesTests
{
	private static readonly string _css = ThemeCss.All;
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
		string toggle = Regex.Match(_js, @"sidebar\.addEventListener\('click', function\(e\) \{\s*const toggle = closest\(e\.target, '\.nav-toggle'\);.*?\n        \}\);",
			RegexOptions.Singleline).Value;

		toggle.Should().Contain("children.style.maxHeight = children.scrollHeight + 'px';");
		toggle.Should().MatchRegex(@"setTimeout\(\(\) => \{ children\.style\.maxHeight = ''; \}, \d+\)");
		toggle.Should().Contain("toggle.setAttribute('aria-expanded', isExpanded ? 'true' : 'false');");
	}

	[Fact]
	public void OpeningTheMobileMenu_ScrollsToTheCurrentPage()
	{
		// The sidebar is hidden while a phone loads the page, so nothing scrolled it then.
		_js.Should().MatchRegex(
			@"if \(open\) \{\s*// [^\n]*\n\s*const current = sidebar\.querySelector\('\.nav-link\.current'\);\s*if \(current\) current\.scrollIntoView\(");
	}

	[Fact]
	public void InlineSidebar_ScrollsToTheCurrentPageOnLoadLikeTheSharedOne()
	{
		// Only the shared sidebar script did; a page deep in a long inline sidebar opened with its
		// entry out of sight.
		string init = Regex.Match(_js, @"function initSidebar\(\) \{.*?const settle", RegexOptions.Singleline).Value;

		init.Should().Contain("if (!sidebar.hasAttribute('data-nav-route')) {");
		init.Should().Contain("sidebar.scrollTop += offset - sidebar.clientHeight / 3;");
	}
}
