using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     The markup the default theme's client runtime and assistive technology rely on: landmarks, the
///     skip link, the search dialog, the Appearance panel, the head bootstrap and the no-JS class, on the page
///     and landing layouts.
/// </summary>
public sealed class ThemeAccessibilityRenderingTests
{
	private const string _page = MockSite.Root + "/_site/guide/intro/index.html";
	private const string _landing = MockSite.Root + "/_site/index.html";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		{ MockSite.Root + "/docs/index.md", new MockFileData("---\ntitle: Home\nlayout: landing\n---\n# Home\n\nWelcome.\n") },
		{
			MockSite.Root + "/docs/guide/intro.md",
			new MockFileData("---\ntitle: Intro\norder: 1\n---\n# Intro\n\n## Setup\n\nText.\n\n### Detail\n\nMore.\n\n## Usage\n\nMore.\n")
		},
		{ MockSite.Root + "/docs/guide/next.md", new MockFileData("---\ntitle: Next\norder: 2\n---\n# Next\n\nText.\n") }
	};

	private static async Task<MockFileSystem> BuildAsync(ThemeOptions? options = null)
	{
		SiteConfig config = MockSite.Config() with { Theme = new ThemeConfig { Options = options ?? new ThemeOptions() } };
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());
		return fs;
	}

	private static string HtmlTag(string page)
	{
		int start = page.IndexOf("<html", StringComparison.Ordinal);
		return page[start..(page.IndexOf('>', start) + 1)];
	}

	public static TheoryData<string> Layouts() => [_page, _landing];

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task EveryLayout_StartsWithASkipLinkToTheMainLandmark(string path)
	{
		string page = (await BuildAsync()).File.ReadAllText(path);

		string body = page[(page.IndexOf("<body", StringComparison.Ordinal))..];
		Regex.Match(body, @"<body[^>]*>\s*(<[^>]+>)").Groups[1].Value.Should().Be("<a class=\"skip-link\" href=\"#mokadocs-main\">");
		Regex.Matches(page, "<main[^>]* id=\"mokadocs-main\" tabindex=\"-1\"").Should().ContainSingle();
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task EveryLayout_HasTheNoJsClassThatTheBootstrapRemovesFirst(string path)
	{
		string page = (await BuildAsync()).File.ReadAllText(path);

		HtmlTag(page).Should().StartWith("<html class=\"no-js\"");
		page.Should().Contain("<script>document.documentElement.classList.remove('no-js');try{");
		page.IndexOf("classList.remove('no-js')", StringComparison.Ordinal).Should()
			.BeLessThan(page.IndexOf("<link rel=\"stylesheet\"", StringComparison.Ordinal));
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task EveryLayout_HasAModalSearchDialogWithALabelledComboboxAndAListbox(string path)
	{
		string page = (await BuildAsync()).File.ReadAllText(path);

		page.Should().Contain("<div class=\"search-dialog\" role=\"dialog\" aria-modal=\"true\" aria-label=\"Search documentation\">");
		page.Should().Contain("<label class=\"visually-hidden\" for=\"searchInput\">Search documentation</label>");
		page.Should().Contain("id=\"searchInput\"").And.Contain("role=\"combobox\" aria-expanded=\"false\" aria-controls=\"searchResults\"");
		page.Should().Contain("<div class=\"search-results\" id=\"searchResults\" role=\"listbox\" aria-label=\"Search results\"></div>");
		page.Should().Contain("id=\"searchStatus\" role=\"status\" aria-live=\"polite\"");
		page.Should().Contain("<button type=\"button\" class=\"search-close\" aria-label=\"Close search\">");
		page.Should().Contain("aria-keyshortcuts=\"Control+K\"").And.NotContain("\u2318");
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task EveryLayout_GivesTheAppearancePanelDialogAndRadioSemantics(string path)
	{
		ThemeOptions options = new() { CodeThemeSelector = true, CodeStyleSelector = true };
		string page = (await BuildAsync(options)).File.ReadAllText(path);

		Regex.Match(page, "<button class=\"appearance-trigger\"[^>]*>").Value.Should()
			.Contain("aria-label=\"Appearance\"").And.Contain("aria-haspopup=\"dialog\"").And.Contain("aria-expanded=\"false\"")
			.And.Contain("aria-controls=\"appearancePanel\"");
		page.Should().Contain("id=\"appearancePanel\" role=\"dialog\" aria-labelledby=\"appearanceTitle\"");
		Regex.Matches(page, "role=\"radiogroup\" aria-labelledby=\"\\w+\" data-appearance=").Count.Should().Be(7);
		Regex.Matches(page, "role=\"radio\" aria-checked=\"false\" tabindex=\"-1\"").Count.Should()
			.Be(3 + 4 + 5, "main.js builds the style, color, code theme and code style options when the panel opens");
		page.Should().NotContain("menuitemradio").And.NotContain("role=\"menu\"");
		int panel = page.IndexOf("<div class=\"appearance js-only\">", StringComparison.Ordinal);
		string markup = page[panel..page.IndexOf("class=\"appearance-reset\"", panel, StringComparison.Ordinal)];
		Regex.Matches(markup, "<(?:svg|span class=\"(?:style-preview|appearance-swatch-dot|appearance-code-preview))[^>]*>").Select(m => m.Value)
			.Should().NotBeEmpty().And.OnlyContain(tag => tag.Contains("aria-hidden=\"true\""), "icons and previews are hidden from screen readers");
		page.Should().Contain("<button class=\"theme-toggle\" type=\"button\" aria-label=\"Dark mode\" aria-pressed=\"false\">");
	}

	[Fact]
	public async Task PageLayout_LabelsItsLandmarksAndToggles()
	{
		string page = (await BuildAsync()).File.ReadAllText(_page);

		page.Should().Contain("<nav class=\"toc\" aria-labelledby=\"tocTitle\">").And.Contain("<p class=\"toc-title\" id=\"tocTitle\">On this page</p>");
		page.Should().NotContain("<h4 class=\"toc-title\"");
		page.Should().Contain("<nav class=\"page-nav\" aria-label=\"Previous and next pages\">");
		Regex.Match(page, "<button class=\"mobile-nav-toggle\"[^>]*>").Value.Should()
			.Contain("aria-expanded=\"false\"").And.Contain("aria-controls=\"mokadocs-sidebar\"");
		Regex.Match(page, "<button class=\"toc-toggle-btn\"[^>]*>").Value.Should()
			.Contain("aria-expanded=\"false\"").And.Contain("aria-controls=\"tocSidebar\"");
		page.Should().Contain("<aside class=\"sidebar\" id=\"mokadocs-sidebar\"").And.Contain("<aside class=\"toc-sidebar\" id=\"tocSidebar\">");
		string sidebar = Regex.Match(page, "<nav class=\"sidebar-nav\".*?</nav>", RegexOptions.Singleline).Value;
		Regex.Match(sidebar, "<a class=\"nav-link current\"[^>]*>").Value.Should().EndWith(" aria-current=\"page\">");
		Regex.Matches(sidebar, "aria-current=\"page\"").Should().ContainSingle();
	}

	[Fact]
	public async Task LandingLayout_PutsItsContentInTheMainLandmark()
	{
		string page = (await BuildAsync()).File.ReadAllText(_landing);

		int main = page.IndexOf("<main id=\"mokadocs-main\"", StringComparison.Ordinal);
		int end = page.IndexOf("</main>", StringComparison.Ordinal);
		main.Should().BeGreaterThan(page.IndexOf("</header>", StringComparison.Ordinal));
		page.IndexOf("class=\"landing-hero\"", StringComparison.Ordinal).Should().BeInRange(main, end);
		page.IndexOf("Welcome.", StringComparison.Ordinal).Should().BeInRange(main, end);
		end.Should().BeLessThan(page.IndexOf("<footer", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(false, false, false, false, false)]
	[InlineData(true, true, true, true, true)]
	[InlineData(false, true, false, true, false)]
	public async Task Bootstrap_ReadsAStoredPickOnlyWhenItsPickerIsOn(bool colorThemes, bool codeThemes, bool codeStyles, bool textSize, bool width)
	{
		ThemeOptions options = new()
		{
			ColorThemes = colorThemes, CodeThemeSelector = codeThemes, CodeStyleSelector = codeStyles, TextSizeSelector = textSize,
			WidthSelector = width
		};
		string page = (await BuildAsync(options)).File.ReadAllText(_page);
		string bootstrap = Regex.Match(page, @"<script>document\.documentElement\.classList\.remove\('no-js'\);.*?</script>").Value;

		bootstrap.Contains("g('mokadocs-color-theme')", StringComparison.Ordinal).Should().Be(colorThemes);
		bootstrap.Contains("g('mokadocs-code-theme')", StringComparison.Ordinal).Should().Be(codeThemes);
		bootstrap.Contains("g('mokadocs-code-style')", StringComparison.Ordinal).Should().Be(codeStyles);
		bootstrap.Contains("g('mokadocs-text-size')", StringComparison.Ordinal).Should().Be(textSize);
		bootstrap.Contains("g('mokadocs-reader-width')", StringComparison.Ordinal).Should().Be(width);
		bootstrap.Should().Contain("g=k=>{try{return localStorage.getItem(k)}catch(e){}return null}", "blocked storage reads as empty");
		bootstrap.Should().Contain("f=m&&p.find(x=>x.includes(b))").And.Contain("(f?f[m==='light'?1:0]:b)",
			"the code theme follows a mode the reader chose without being stored");
	}

	[Fact]
	public async Task ImageZoom_TurnedOff_MarksTheRootForTheScript()
	{
		string on = (await BuildAsync()).File.ReadAllText(_page);
		string off = (await BuildAsync(new ThemeOptions { ImageZoom = false })).File.ReadAllText(_page);
		string offLanding = (await BuildAsync(new ThemeOptions { ImageZoom = false })).File.ReadAllText(_landing);

		HtmlTag(on).Should().NotContain("data-no-image-zoom");
		HtmlTag(off).Should().Contain(" data-no-image-zoom>");
		HtmlTag(offLanding).Should().Contain(" data-no-image-zoom>");
	}

	[Fact]
	public async Task ThemeIds_DoNotCollideWithHeadingIds()
	{
		// The sidebar was <aside id="sidebar">, so a "## Sidebar" heading's anchor and its search
		// result jumped to the sidebar: the first element with an id wins.
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/guide/next.md"] = new MockFileData("---\ntitle: Next\norder: 2\n---\n# Next\n\n## Sidebar\n\nText.\n");
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), files);

		string page = fs.File.ReadAllText(MockSite.Root + "/_site/guide/next/index.html");
		Regex.Matches(page, "id=\"sidebar\"").Should().ContainSingle();
		page.Should().MatchRegex("<h2[^>]* id=\"sidebar\"");
	}
}
