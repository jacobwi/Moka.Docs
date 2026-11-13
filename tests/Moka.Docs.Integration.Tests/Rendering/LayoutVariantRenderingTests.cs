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
		"sidebar" => new ThemeLayoutOptions { Sidebar = variant },
		"sidebarScope" => new ThemeLayoutOptions { SidebarScope = variant },
		"home" => new ThemeLayoutOptions { Home = variant },
		"hero" => new ThemeLayoutOptions { Hero = variant },
		"heroBackground" => new ThemeLayoutOptions { HeroBackground = variant },
		"features" => new ThemeLayoutOptions { Features = variant },
		"admonitions" => new ThemeLayoutOptions { Admonitions = variant },
		"tables" => new ThemeLayoutOptions { Tables = variant },
		"density" => new ThemeLayoutOptions { Density = variant },
		"font" => new ThemeLayoutOptions { Font = variant },
		"headings" => new ThemeLayoutOptions { Headings = variant },
		"links" => new ThemeLayoutOptions { Links = variant },
		_ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
	};

	private static string Attribute(string part) =>
		"data-" + string.Concat(part.Select(c => char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : c.ToString()));

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
	public async Task EveryPartAtOnce_IsWrittenOnTheHtmlStartTagOfBothLayouts()
	{
		// The headings and links attributes sit on a second template line joined with {{- so the start
		// tag stays on one line.
		var layout = new ThemeLayoutOptions
		{
			Width = "narrow", Navbar = "transparent", Footer = "compact", Toc = "timeline", Breadcrumbs = "dots",
			PageNav = "split", Sidebar = "cards", SidebarScope = "section", Home = "showcase", Hero = "banner",
			HeroBackground = "mesh", Features = "bento", Admonitions = "callout", Tables = "rounded", Density = "compact",
			Font = "humanist", Headings = "numbered", Links = "subtle"
		};

		MockFileSystem fs = await BuildAsync(layout);
		MockFileSystem plain = await BuildAsync(new ThemeLayoutOptions());

		foreach (string path in new[] { _page, _landing })
		{
			string tag = HtmlTag(fs.File.ReadAllText(path));
			foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
			{
				tag.Should().Contain($"{Attribute(part.Key)}=\"{layout.NameOf(part)}\"");
			}

			tag.Should().NotContain("\n").And.Contain("data-font=\"humanist\" data-headings=\"numbered\" data-links=\"subtle\"");
			HtmlTag(plain.File.ReadAllText(path)).Should().NotContain("\n");
		}
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
	public async Task FooterColumns_TopLevelPagesFollowFourSectionColumns()
	{
		// Top-level pages without entries below shared a column that came first and took one of the
		// four places, so the fourth section went missing.
		var files = new Dictionary<string, MockFileData>
		{
			[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
			[MockSite.Root + "/docs/changelog.md"] = new("---\ntitle: Changelog\n---\n# Changelog\n")
		};
		var nav = new List<NavItem> { new() { Label = "Home", Path = "/" } };
		foreach (string section in new[] { "one", "two", "three", "four", "five" })
		{
			files[$"{MockSite.Root}/docs/{section}/page.md"] = new($"---\ntitle: Page {section}\n---\n# Page\n");
			nav.Add(new NavItem
			{
				Label = "Section " + section,
				Path = "/" + section,
				Children = [new NavItem { Label = "Page " + section, Path = $"/{section}/page" }]
			});
		}

		nav.Add(new NavItem { Label = "Changelog", Path = "/changelog" });
		SiteConfig config = MockSite.Config() with
		{
			Nav = nav,
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = new ThemeLayoutOptions { Footer = "columns" } } }
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, files);

		string page = fs.File.ReadAllText(MockSite.Root + "/_site/one/page/index.html");
		List<string> titles = System.Text.RegularExpressions.Regex.Matches(page, "<p class=\"footer-col-title\">([^<]*)</p>")
			.Select(m => m.Groups[1].Value).ToList();
		titles.Should().Equal("Section one", "Section two", "Section three", "Section four", "More");
		int more = page.IndexOf("<p class=\"footer-col-title\">More</p>", StringComparison.Ordinal);
		page[more..].Should().Contain("href=\"/\">Home</a>").And.Contain("href=\"/changelog\">Changelog</a>");
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

	private static async Task<string> BuildLandingAsync(string frontMatter, ThemeLayoutOptions layout, string basePath = "/")
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/index.md"] =
			new MockFileData("---\ntitle: Home\ndescription: The home page\nlayout: landing\n" + frontMatter + "---\n# Home\n");
		SiteConfig config = MockSite.Config() with
		{
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = layout } },
			Build = new BuildConfig { BasePath = basePath }
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, files);
		return fs.File.ReadAllText(_landing);
	}

	private const string _heroCode = "heroCodeLanguage: csharp\nheroCode: |\n  var list = new List<int>();\n  list.Add(1);\n";

	[Fact]
	public async Task HeroSplit_PutsTheHighlightedCodeBesideTheText()
	{
		string landing = await BuildLandingAsync(_heroCode, new ThemeLayoutOptions { Hero = "split" });

		landing.Should().Contain("<section class=\"landing-hero landing-hero-split\">");
		landing.Should().Contain("<div class=\"landing-hero-media landing-content\">");
		landing.Should().Contain("<div class=\"code-block\" data-line-numbers=\"false\" data-lang=\"csharp\">");
		landing.Should().Contain("data-highlighted=\"true\"").And.Contain("<span class=\"token keyword\">new</span>");
		landing.Should().Contain("&lt;").And.NotContain("List<int>");
		int text = landing.IndexOf("<div class=\"landing-hero-text\">", StringComparison.Ordinal);
		text.Should().BeGreaterThan(0);
		text.Should().BeLessThan(landing.IndexOf("<h1 class=\"landing-hero-title\">", StringComparison.Ordinal));
		landing.IndexOf("landing-hero-media", StringComparison.Ordinal)
			.Should().BeGreaterThan(landing.IndexOf("<div class=\"landing-hero-actions\">", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("images/hero.png", "/", "/images/hero.png")]
	[InlineData("./images/hero.png", "/Repo", "/Repo/images/hero.png")]
	[InlineData("/media/hero.png", "/Repo", "/Repo/media/hero.png")]
	[InlineData("https://cdn.example.com/hero.png", "/Repo", "https://cdn.example.com/hero.png")]
	public async Task HeroSplit_ShowsTheImageResolvedLikeOtherLinks(string image, string basePath, string expected)
	{
		string landing = await BuildLandingAsync($"heroImage: {image}\n", new ThemeLayoutOptions { Hero = "split" }, basePath);

		landing.Should().Contain("<section class=\"landing-hero landing-hero-split\">");
		landing.Should().Contain($"<img src=\"{expected}\" alt=\"\" />");
	}

	[Fact]
	public async Task HeroSplit_PrefersTheCodeOverTheImage()
	{
		string landing = await BuildLandingAsync(_heroCode + "heroImage: images/hero.png\n", new ThemeLayoutOptions { Hero = "split" });

		landing.Should().Contain("class=\"code-block\"").And.NotContain("images/hero.png");
	}

	[Fact]
	public async Task HeroSplit_WithoutCodeOrImage_KeepsTheCenteredHero()
	{
		string landing = await BuildLandingAsync("", new ThemeLayoutOptions { Hero = "split" });

		landing.Should().Contain("<section class=\"landing-hero\">");
		landing.Should().NotContain("landing-hero-split").And.NotContain("landing-hero-text").And.NotContain("landing-hero-media");
		landing.Should().Contain("data-hero=\"split\"");
	}

	[Theory]
	[InlineData("default")]
	[InlineData("minimal")]
	public async Task HeroWithoutSplit_LeavesOutTheCode(string hero)
	{
		string landing = await BuildLandingAsync(_heroCode, new ThemeLayoutOptions { Hero = hero });

		landing.Should().Contain("<section class=\"landing-hero\">");
		landing.Should().NotContain("landing-hero-media").And.NotContain("class=\"code-block\"");
	}

	[Fact]
	public async Task HeroMinimal_LeavesOutTheIcon()
	{
		string minimal = await BuildLandingAsync("", new ThemeLayoutOptions { Hero = "minimal" });
		string standard = await BuildLandingAsync("", new ThemeLayoutOptions());

		minimal.Should().NotContain("landing-hero-icon").And.Contain("<h1 class=\"landing-hero-title\">");
		standard.Should().Contain("<div class=\"landing-hero-icon\">");
	}

	[Fact]
	public async Task PrevNextAndFeedback_SitInTheContentColumnAfterTheArticle()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions());

		string page = fs.File.ReadAllText(_page);
		int article = page.IndexOf("</article>", StringComparison.Ordinal);
		int feedback = page.IndexOf("<div class=\"feedback-widget\"", StringComparison.Ordinal);
		int nav = page.IndexOf("<nav class=\"page-nav\"", StringComparison.Ordinal);
		int main = page.IndexOf("</main>", StringComparison.Ordinal);
		article.Should().BePositive();
		feedback.Should().BeGreaterThan(article);
		nav.Should().BeGreaterThan(feedback);
		main.Should().BeGreaterThan(nav);
	}

	[Fact]
	public async Task PrevNext_IsLeftOutOnAPageWithNeither()
	{
		var files = new Dictionary<string, MockFileData>
		{
			[MockSite.Root + "/docs/only.md"] = new("---\ntitle: Only\n---\n# Only\n")
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), files);

		fs.File.ReadAllText(MockSite.Root + "/_site/only/index.html").Should().NotContain("class=\"page-nav\"");
	}

	[Fact]
	public async Task SidebarToggle_IsOffByDefault()
	{
		MockFileSystem fs = await BuildAsync(new ThemeLayoutOptions());

		fs.File.ReadAllText(_page).Should().NotContain("sidebarToggle").And.NotContain("mokadocs-sidebar-hidden");
	}

	[Fact]
	public async Task SidebarToggle_AddsAButtonAndAppliesTheStoredChoiceRightAfterTheSidebar()
	{
		SiteConfig config = MockSite.Config() with { Theme = new ThemeConfig { Options = new ThemeOptions { SidebarToggle = true } } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string page = fs.File.ReadAllText(_page);
		const string button =
			"<button class=\"sidebar-toggle js-only\" id=\"sidebarToggle\" type=\"button\" aria-label=\"Toggle sidebar\" aria-controls=\"mokadocs-sidebar\" aria-expanded=\"true\">";
		page.Should().Contain(button);
		int aside = page.IndexOf("</aside>", StringComparison.Ordinal);
		int toggle = page.IndexOf(button, StringComparison.Ordinal);
		int script = page.IndexOf("<script>try{if(localStorage.getItem('mokadocs-sidebar-hidden')==='true')", StringComparison.Ordinal);
		toggle.Should().BeGreaterThan(aside);
		script.Should().BeGreaterThan(toggle);
		script.Should().BeLessThan(page.IndexOf("<main", StringComparison.Ordinal));
		page[..page.IndexOf("</head>", StringComparison.Ordinal)].Should().NotContain("mokadocs-sidebar-hidden");
		fs.File.ReadAllText(_landing).Should().NotContain("sidebarToggle");
	}
}
