using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     The markup <c>theme.options.layout</c> changes in the default theme's page and landing layouts.
///     The look of each variant is CSS scoped to the attribute the layouts write on <c>&lt;html&gt;</c>.
/// </summary>
public sealed class LayoutVariantRenderingTests
{
	private const string _page = MockSite.Root + "/_site/guide/intro/index.html";
	private const string _landing = MockSite.Root + "/_site/index.html";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		{
			MockSite.Root + "/docs/index.md",
			new MockFileData("---\ntitle: Home\nlayout: landing\n---\n# Home\n")
		},
		{
			MockSite.Root + "/docs/guide/intro.md",
			new MockFileData("---\ntitle: Intro\norder: 1\n---\n# Intro\n\n## Setup\n\nText.\n\n## Usage\n\nMore.\n")
		},
		{
			MockSite.Root + "/docs/guide/install.md",
			new MockFileData("---\ntitle: Install\norder: 2\n---\n# Install\n\n## Steps\n\nText.\n")
		},
		{
			MockSite.Root + "/docs/reference/options.md",
			new MockFileData("---\ntitle: Options\n---\n# Options\n\n## Flags\n\nText.\n")
		}
	};

	private static async Task<MockFileSystem> BuildAsync(ThemeLayoutOptions layout, string basePath = "/")
	{
		SiteConfig config = MockSite.Config() with
		{
			Site = new SiteMetadata
			{
				Title = "Test Site",
				Description = "Docs for <tests>",
				Url = "https://test.example.com",
				Copyright = "Copyright Test"
			},
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = layout } },
			Build = new BuildConfig { BasePath = basePath }
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());
		return fs;
	}

	public static TheoryData<string, string> Variants()
	{
		var data = new TheoryData<string, string>();
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			foreach (string variant in part.Names.Where(n => n != ThemeLayouts.Default))
			{
				data.Add(part.Key, variant);
			}
		}

		return data;
	}

	private static ThemeLayoutOptions With(string part, string variant) => part switch
	{
		"width" => new ThemeLayoutOptions { Width = variant },
		"navbar" => new ThemeLayoutOptions { Navbar = variant },
		"footer" => new ThemeLayoutOptions { Footer = variant },
		"toc" => new ThemeLayoutOptions { Toc = variant },
		"breadcrumbs" => new ThemeLayoutOptions { Breadcrumbs = variant },
		"pageNav" => new ThemeLayoutOptions { PageNav = variant },
		_ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
	};

	private static string Attribute(string part) => part == "pageNav" ? "data-page-nav" : "data-" + part;

	private static string HtmlTag(string page)
	{
		int start = page.IndexOf("<html", StringComparison.Ordinal);
		return page[start..page.IndexOf('>', start)];
	}

	[Fact]
	public async Task DefaultLayout_WritesNoLayoutAttributesOrVariantMarkup()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions());

		foreach (string path in new[] { _page, _landing })
		{
			string page = fs.File.ReadAllText(path);
			foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
			{
				HtmlTag(page).Should().NotContain(Attribute(part.Key) + "=");
			}

			page.Should().NotContain("class=\"header-tabs\"").And.NotContain("class=\"footer-columns\"");
		}

		string intro = fs.File.ReadAllText(_page);
		intro.Should().Contain("<aside class=\"toc-sidebar\"").And.Contain("id=\"tocProgress\"")
			.And.Contain("id=\"tocCounter\"").And.Contain("id=\"tocToggle\"").And.NotContain("toc-inline");
		intro.Should().Contain("<footer class=\"site-footer\">").And.Contain("<div class=\"footer-inner\">");
	}

	[Theory]
	[MemberData(nameof(Variants))]
	public async Task Variant_IsWrittenOnTheHtmlElementOfBothLayouts(string part, string variant)
	{
		MockFileSystem fs = await BuildAsync(With(part, variant));

		string expected = $"{Attribute(part)}=\"{variant}\"";
		HtmlTag(fs.File.ReadAllText(_page)).Should().Contain(expected);
		HtmlTag(fs.File.ReadAllText(_landing)).Should().Contain(expected);
	}

	[Fact]
	public async Task NavbarTabs_LinkEachTopLevelSectionAndMarkTheCurrentOne()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Navbar = "tabs" }, "/Repo");

		string page = fs.File.ReadAllText(_page);
		page.Should().Contain("<nav class=\"header-tabs\" aria-label=\"Sections\">");
		page.Should().Contain("<a class=\"header-tab current\" href=\"/Repo/guide/intro\" aria-current=\"true\">Guide</a>");
		page.Should().Contain("<a class=\"header-tab\" href=\"/Repo/reference/options\">Reference</a>");
		page.IndexOf("class=\"header-tabs\"", StringComparison.Ordinal)
			.Should().BeLessThan(page.IndexOf("</header>", StringComparison.Ordinal));

		string landing = fs.File.ReadAllText(_landing);
		landing.Should().Contain("<a class=\"header-tab current\" href=\"/Repo/\" aria-current=\"true\">Home</a>");
		landing.Should().Contain("<a class=\"header-tab\" href=\"/Repo/guide/intro\">Guide</a>");
	}

	[Fact]
	public async Task FooterColumns_ListTheSectionsOnBothLayouts()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Footer = "columns" });

		foreach (string path in new[] { _page, _landing })
		{
			string page = fs.File.ReadAllText(path);
			page.Should().Contain("<div class=\"footer-columns\">");
			page.Should().Contain("<p class=\"footer-brand-desc\">Docs for &lt;tests&gt;</p>");
			page.Should().Contain("<p class=\"footer-col-title\">Guide</p>");
			page.Should().Contain("<li><a href=\"/guide/intro\">Intro</a></li><li><a href=\"/guide/install\">Install</a></li>");
			page.Should().Contain("<p class=\"footer-col-title\">Reference</p>");
			page.Should().Contain("<div class=\"footer-bottom\">").And.Contain("Copyright Test").And.Contain("Built with");
			page.Should().NotContain("<div class=\"footer-inner\">").And.NotContain("class=\"landing-footer\"");
		}
	}

	[Fact]
	public async Task FooterNone_LeavesOutTheFooterOnBothLayouts()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Footer = "none" });

		fs.File.ReadAllText(_page).Should().NotContain("<footer");
		fs.File.ReadAllText(_landing).Should().NotContain("<footer");
	}

	[Fact]
	public async Task TocInline_PutsTheListInThePageInsteadOfTheSideColumn()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Toc = "inline" });

		string page = fs.File.ReadAllText(_page);
		page.Should().Contain("<details class=\"toc-inline\" id=\"tocInline\" open>");
		page.Should().Contain("<a href=\"#setup\">Setup</a>").And.Contain("<a href=\"#usage\">Usage</a>");
		page.Should().NotContain("class=\"toc-sidebar\"").And.NotContain("id=\"tocToggle\"");
		int details = page.IndexOf("toc-inline", StringComparison.Ordinal);
		details.Should().BeGreaterThan(page.IndexOf("<article class=\"page-content\">", StringComparison.Ordinal));
		details.Should().BeLessThan(page.IndexOf("</article>", StringComparison.Ordinal));
	}

	[Fact]
	public async Task TocMinimal_KeepsTheLinksAndDropsProgressCounterAndTopLink()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Toc = "minimal" });

		string page = fs.File.ReadAllText(_page);
		page.Should().Contain("<aside class=\"toc-sidebar\"").And.Contain("<a href=\"#setup\">Setup</a>");
		page.Should().NotContain("id=\"tocProgress\"").And.NotContain("id=\"tocCounter\"")
			.And.NotContain("id=\"tocBackToTop\"");
	}

	[Fact]
	public async Task TocFloating_KeepsThePanelAndItsButton()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions { Toc = "floating" });

		string page = fs.File.ReadAllText(_page);
		page.Should().Contain("<aside class=\"toc-sidebar\"").And.Contain("id=\"tocToggle\"");
	}

	[Fact]
	public async Task ShowTableOfContentsOff_StillWinsOverTheInlineVariant()
	{
		SiteConfig config = MockSite.Config() with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions { ShowTableOfContents = false, Layout = new ThemeLayoutOptions { Toc = "inline" } }
			}
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		fs.File.ReadAllText(_page).Should().NotContain("toc-inline").And.NotContain("toc-sidebar");
	}
}
