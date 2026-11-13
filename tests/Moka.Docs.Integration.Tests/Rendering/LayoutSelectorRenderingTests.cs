using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     <c>theme.options.layoutSelector</c> lets readers switch layout variants on an open page, so the
///     pages carry the markup of every variant and the CSS shows only the one that is on. Without it the
///     pages carry the configured variant's markup alone, as before.
/// </summary>
public sealed partial class LayoutSelectorRenderingTests
{
	private const string _page = MockSite.Root + "/_site/guide/intro/index.html";
	private const string _landing = MockSite.Root + "/_site/index.html";

	private static readonly string _mainCss = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");
	private static readonly string _layoutsCss = EmbeddedThemeProvider.GetLayoutsCss().Replace("\r\n", "\n");

	private static Dictionary<string, MockFileData> Files() => new()
	{
		{ MockSite.Root + "/docs/index.md", new MockFileData("---\ntitle: Home\nlayout: landing\n---\n# Home\n") },
		{
			MockSite.Root + "/docs/guide/intro.md",
			new MockFileData("---\ntitle: Intro\norder: 1\n---\n# Intro\n\n## Setup\n\nText.\n\n### Details\n\nMore.\n\n## Usage\n\nMore.\n")
		},
		{ MockSite.Root + "/docs/guide/install.md", new MockFileData("---\ntitle: Install\norder: 2\n---\n# Install\n\n## Steps\n\nText.\n") },
		{ MockSite.Root + "/docs/reference/options.md", new MockFileData("---\ntitle: Options\n---\n# Options\n\n## Flags\n\nText.\n") }
	};

	private static async Task<MockFileSystem> BuildAsync(bool layoutSelector, ThemeLayoutOptions? layout = null)
	{
		SiteConfig config = MockSite.Config() with
		{
			Site = new SiteMetadata { Title = "Test Site", Url = "https://test.example.com", Copyright = "Copyright Test" },
			Theme = new ThemeConfig { Options = new ThemeOptions { LayoutSelector = layoutSelector, Layout = layout ?? new ThemeLayoutOptions() } }
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());
		return fs;
	}

	[Fact]
	public async Task Off_PagesCarryOnlyTheConfiguredVariantsMarkup()
	{
		MockFileSystem plain = await BuildAsync(false);
		MockFileSystem configured = await BuildAsync(false,
			new ThemeLayoutOptions { Navbar = "tabs", Footer = "columns", Toc = "inline" });
		MockFileSystem bare = await BuildAsync(false, new ThemeLayoutOptions { Footer = "none", Toc = "minimal" });

		string page = plain.File.ReadAllText(_page);
		page.Should().NotContain("header-tabs").And.NotContain("toc-inline").And.NotContain("footer-columns")
			.And.NotContain("site-footer-row").And.NotContain("appearance-layout").And.NotContain("mokadocs-layout-")
			.And.NotContain("layouts.css");
		page.Should().Contain("<footer class=\"site-footer\">").And.Contain("id=\"tocSidebar\"").And.Contain("id=\"tocProgress\"");

		string variants = configured.File.ReadAllText(_page);
		variants.Should().Contain("<nav class=\"header-tabs\"").And.Contain("<details class=\"toc-inline\" id=\"tocInline\" open>")
			.And.Contain("<footer class=\"site-footer\">\n<div class=\"footer-columns\">");
		variants.Should().NotContain("id=\"tocSidebar\"").And.NotContain("id=\"tocToggle\"").And.NotContain("footer-inner")
			.And.NotContain("site-footer-columns");

		string none = bare.File.ReadAllText(_page);
		none.Should().NotContain("<footer").And.NotContain("tocProgress").And.NotContain("tocBackToTop").And.NotContain("tocCounter");
	}

	[Theory]
	[InlineData(_page)]
	[InlineData(_landing)]
	public async Task On_PagesCarryEveryVariantsMarkupWithEachIdOnce(string path)
	{
		MockFileSystem fs = await BuildAsync(true, new ThemeLayoutOptions { Toc = "inline", Footer = "none" });

		string page = fs.File.ReadAllText(path);
		page.Should().Contain("<nav class=\"header-tabs\" aria-label=\"Sections\">", "a reader can switch the navbar to tabs");
		page.Should().Contain("<footer class=\"site-footer site-footer-columns\">", "and the footer to columns");
		page.Should().Contain(path == _landing ? "<footer class=\"landing-footer\">" : "<footer class=\"site-footer site-footer-row\">",
			"and back to the one-row footer, though the site has none");
		page.Should().MatchRegex("<link rel=\"stylesheet\" href=\"/_theme/css/layouts\\.css\\?v=[0-9a-f]{10}\" />");
		if (path == _page)
		{
			page.Should().Contain("<details class=\"toc-inline\" id=\"tocInline\" open>").And.Contain("id=\"tocSidebar\"")
				.And.Contain("id=\"tocToggle\"").And.Contain("id=\"tocProgress\"").And.Contain("id=\"tocBackToTop\"")
				.And.Contain("id=\"tocCounter\"").And.Contain("h.after(t)");
			Regex.Matches(page, "<ul class=\"toc-list\">").Should().HaveCount(2, "the list sits beside the page and inline");
		}

		string[] ids = [.. IdPattern().Matches(page).Select(m => m.Groups[1].Value)];
		ids.Should().OnlyHaveUniqueItems();
		page.Should().Contain("data-toc=\"inline\"").And.Contain("data-footer=\"none\"");
	}

	[Fact]
	public void LayoutsCss_IsLinkedWheneverReadersCanSwitchLayouts()
	{
		EmbeddedThemeProvider.CssFilesFor(new ThemeOptions()).Should().Equal(EmbeddedThemeProvider.MainCssPath);
		EmbeddedThemeProvider.CssFilesFor(new ThemeOptions { LayoutSelector = true })
			.Should().Equal(EmbeddedThemeProvider.MainCssPath, EmbeddedThemeProvider.LayoutsCssPath);
	}

	[Fact]
	public void Css_ShowsTheMarkupOfTheVariantThatIsOnAndHidesWhatItReplaces()
	{
		// main.css hides the markup only some variants show; layouts.css shows it under its own variant.
		_mainCss.Should().Contain(".header-tabs, .toc-inline, .site-footer-columns { display: none; }");
		_layoutsCss.Should().Contain("html[data-navbar=\"tabs\"] .header-tabs { display: block; }");
		_layoutsCss.Should().Contain("html[data-footer=\"columns\"] .site-footer-columns { display: block; }");
		_layoutsCss.Should().Contain("html[data-footer=\"columns\"] :is(.site-footer-row, .landing-footer) { display: none; }");
		_layoutsCss.Should().Contain("html[data-footer=\"none\"] :is(.site-footer, .landing-footer) { display: none; }");
		_layoutsCss.Should().Contain("html[data-toc=\"inline\"] .toc-inline { display: block; }");
		_layoutsCss.Should().Contain("html[data-toc=\"inline\"] :is(.toc-sidebar, .toc-toggle-btn) { display: none; }");
		_layoutsCss.Should().Contain("html[data-toc=\"minimal\"] :is(.toc-progress, .toc-back-to-top, .toc-counter) { display: none; }");
		_layoutsCss.Should().Contain("html[data-toc=\"inline\"]:not(.no-js):has(.toc-toggle-btn) { --fab-stack: 0px; }",
			"the hidden button leaves no gap above back to top");
	}

	[Fact]
	public void Css_WiderWidthAndTheBoxedFrameUnderAReadersWiderWidth()
	{
		_layoutsCss.Should().Contain("html[data-width=\"wider\"] { --layout-max-width: 1920px; --content-max-width: 1280px; }");
		_layoutsCss.Should().Contain(
			"html[data-width=\"boxed\"]:is([data-reader-width=\"wider\"], [data-reader-width=\"full\"]) body { background: var(--color-bg); }");
	}

	[GeneratedRegex("\\sid=\"([^\"]+)\"")]
	private static partial Regex IdPattern();
}
