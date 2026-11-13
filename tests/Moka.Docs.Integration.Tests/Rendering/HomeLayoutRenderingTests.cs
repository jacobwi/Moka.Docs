using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     <c>theme.options.layout.home</c>: the markup each home page variant gives a <c>landing</c> page, with
///     and without the front matter the variant reads. The look is CSS scoped to <c>data-home</c>.
/// </summary>
public sealed class HomeLayoutRenderingTests
{
	private const string _landing = MockSite.Root + "/_site/index.html";

	private const string _features =
		"featuresTitle: Why Widgets\nfeaturesSubtitle: All in one\nfeatures:\n" +
		"  - title: Fast\n    icon: zap\n    description: Quick.\n    link: /guide/intro\n" +
		"  - title: Typed\n    icon: \"C#\"\n    description: Every option is typed.\n";

	private const string _showcase =
		"showcaseTitle: Two lines\nshowcaseText: Bind and go.\nshowcaseCodeLanguage: csharp\n" +
		"showcaseCode: |\n  var grid = new Grid<Order>();\n" +
		"stats:\n  - value: 2M\n    label: Downloads\n  - value: \"40\"\n" +
		"cta:\n  title: Ready?\n  text: Install it.\n  link: /guide/install\n  linkText: Install now\n";

	private static Dictionary<string, MockFileData> Files(string frontMatter, string content = "## More\n\nText.\n") => new()
	{
		{
			MockSite.Root + "/docs/index.md",
			new MockFileData("---\ntitle: Home\ndescription: Widgets for .NET\nlayout: landing\n" + frontMatter + "---\n" + content)
		},
		{ MockSite.Root + "/docs/guide/intro.md", new MockFileData("---\ntitle: Intro\norder: 1\n---\n# Intro\n") },
		{ MockSite.Root + "/docs/guide/install.md", new MockFileData("---\ntitle: Install\norder: 2\n---\n# Install\n") },
		{ MockSite.Root + "/docs/reference/options.md", new MockFileData("---\ntitle: Options\n---\n# Options\n") }
	};

	private static async Task<string> BuildLandingAsync(string frontMatter, ThemeLayoutOptions layout,
		string basePath = "/", Func<SiteConfig, SiteConfig>? configure = null, string content = "## More\n\nText.\n")
	{
		SiteConfig config = MockSite.Config() with
		{
			Site = new SiteMetadata
			{
				Title = "Widgets",
				Url = "https://test.example.com",
				EditLink = new EditLinkConfig { Repo = "https://github.com/example/widgets" }
			},
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = layout } },
			Build = new BuildConfig { BasePath = basePath }
		};
		(_, MockFileSystem fs) = await MockSite.BuildAsync(configure?.Invoke(config) ?? config, Files(frontMatter, content));
		return fs.File.ReadAllText(_landing);
	}

	private static string MainOf(string page)
	{
		int start = page.IndexOf("<main", StringComparison.Ordinal);
		return page[start..(page.IndexOf("</main>", start, StringComparison.Ordinal) + 7)];
	}

	private static void ShouldBeInOrder(string page, params string[] parts)
	{
		int previous = -1;
		foreach (string part in parts)
		{
			int at = page.IndexOf(part, StringComparison.Ordinal);
			at.Should().BeGreaterThan(previous, "'{0}' should follow what comes before it", part);
			previous = at;
		}
	}

	[Fact]
	public async Task DefaultLayout_KeepsTheLandingHeaderWithoutASidebarToggle()
	{
		// Only the docs home page has a sidebar, so only it needs the phone menu button.
		string landing = await BuildLandingAsync("", new ThemeLayoutOptions());

		landing.Should().Contain("<body class=\"landing\">").And.NotContain("mobile-nav-toggle").And.NotContain("data-home=");
	}

	[Fact]
	public async Task Default_IgnoresTheShowcaseFrontMatter()
	{
		string landing = await BuildLandingAsync(_features + _showcase, new ThemeLayoutOptions());

		landing.Should().Contain("<section class=\"landing-features\">").And.Contain("<hr class=\"landing-divider\" />");
		landing.Should().NotContain("home-showcase").And.NotContain("home-stats").And.NotContain("home-cta")
			.And.NotContain("home-search").And.NotContain("home-sections");
	}

	[Fact]
	public async Task Portal_PutsASearchBoxInTheHeroAndACardPerSection()
	{
		string landing = await BuildLandingAsync(_features, new ThemeLayoutOptions { Home = "portal" }, "/Repo");
		string main = MainOf(landing);

		main.Should().Contain("<button class=\"home-search js-only\" type=\"button\" aria-keyshortcuts=\"Control+K\">")
			.And.Contain("<span class=\"home-search-label\">Search the documentation</span>");
		main.Should().NotContain("landing-hero-actions").And.NotContain("landing-hero-icon").And.NotContain("landing-features",
			"the section cards stand in for the feature cards");
		main.Should().Contain("<nav class=\"home-sections\" aria-label=\"Documentation sections\">");
		main.Should().Contain("<h2 class=\"home-section-title\"><a href=\"/Repo/guide/intro\"><span>Guide</span></a></h2>");
		main.Should().Contain("<li><a href=\"/Repo/guide/intro\">Intro</a></li><li><a href=\"/Repo/guide/install\">Install</a></li>");
		main.Should().Contain("<a href=\"/Repo/reference/options\"><span>Reference</span></a>");
		main.Should().NotContain("<span>Home</span>", "a card for the page it sits on leads nowhere");
		ShouldBeInOrder(main, "class=\"landing-hero\"", "home-search", "home-sections", "landing-md-content");
		landing.Should().Contain("id=\"searchModal\"");
	}

	[Fact]
	public async Task Portal_SectionCards_ShowTheNavIconAndTheFirstFivePages()
	{
		var children = Enumerable.Range(1, 7)
			.Select(i => new NavItem { Label = "Page " + i, Path = "/guide/p" + i })
			.ToList();
		Dictionary<string, MockFileData> files = Files("");
		for (int i = 1; i <= 7; i++)
		{
			files[$"{MockSite.Root}/docs/guide/p{i}.md"] = new MockFileData($"---\ntitle: Page {i}\n---\n# Page\n");
		}

		SiteConfig config = MockSite.Config() with
		{
			Nav = [new NavItem { Label = "Guide", Path = "/guide", Icon = "rocket", Children = children }],
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = new ThemeLayoutOptions { Home = "portal" } } }
		};
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, files);
		string main = MainOf(fs.File.ReadAllText(_landing));

		main.Should().Contain("<span class=\"home-section-icon\"><svg").And.Contain("#rocket\"/></svg></span><span>Guide</span>");
		main.Should().Contain(">Page 5</a></li>").And.NotContain(">Page 6</a>");
	}

	[Fact]
	public async Task Portal_WithoutSearch_HasNoSearchBox()
	{
		string landing = await BuildLandingAsync("", new ThemeLayoutOptions { Home = "portal" },
			configure: c => c with { Features = new FeaturesConfig { Search = new SearchFeatureConfig { Enabled = false } } });

		landing.Should().NotContain("home-search").And.NotContain("searchModal");
		MainOf(landing).Should().Contain("<nav class=\"home-sections\"");
	}

	[Fact]
	public async Task Showcase_PutsTheCodeBandFiguresAndCallToActionAfterTheFeatures()
	{
		string main = MainOf(await BuildLandingAsync(_features + _showcase, new ThemeLayoutOptions { Home = "showcase" }, "/Repo"));

		main.Should().Contain("<section class=\"home-showcase\">");
		main.Should().Contain("<h2 class=\"home-showcase-title\">Two lines</h2>")
			.And.Contain("<p class=\"home-showcase-desc\">Bind and go.</p>");
		main.Should().Contain("<div class=\"home-showcase-code landing-content\"><div class=\"code-block\" data-line-numbers=\"false\" data-lang=\"csharp\">")
			.And.Contain("data-highlighted=\"true\"").And.Contain("&lt;").And.NotContain("<Order>");
		main.Should().Contain("<section class=\"home-stats\" aria-label=\"Key figures\">");
		main.Should().Contain("<li class=\"home-stat\"><span class=\"home-stat-value\">2M</span> <span class=\"home-stat-label\">Downloads</span></li>")
			.And.Contain("<li class=\"home-stat\"><span class=\"home-stat-value\">40</span></li>");
		main.Should().Contain("<h2 class=\"home-cta-title\">Ready?</h2>").And.Contain("<p class=\"home-cta-text\">Install it.</p>");
		main.Should().Contain("<a class=\"landing-btn landing-btn-primary home-cta-button\" href=\"/Repo/guide/install\">")
			.And.Contain("Install now");
		main.Should().NotContain("landing-divider", "the bands already separate the features from the content");
		ShouldBeInOrder(main, "class=\"landing-hero\"", "class=\"landing-features\"", "class=\"home-showcase\"",
			"class=\"home-stats\"", "class=\"home-cta\"", "landing-md-content");
	}

	[Fact]
	public async Task Showcase_WithoutItsFrontMatter_IsTheDefaultPage()
	{
		string showcase = await BuildLandingAsync(_features, new ThemeLayoutOptions { Home = "showcase" });
		string plain = await BuildLandingAsync(_features, new ThemeLayoutOptions());

		MainOf(showcase).Should().Be(MainOf(plain));
		showcase.Should().Contain("data-home=\"showcase\"");
	}

	[Fact]
	public async Task Showcase_TextWithoutCode_IsAPlainBand()
	{
		string main = MainOf(await BuildLandingAsync("showcaseTitle: Why\n", new ThemeLayoutOptions { Home = "showcase" }));

		main.Should().Contain("<section class=\"home-showcase home-showcase-plain\">")
			.And.Contain("<h2 class=\"home-showcase-title\">Why</h2>")
			.And.NotContain("home-showcase-code").And.NotContain("home-showcase-desc");
	}

	[Fact]
	public async Task Showcase_CodeWithoutText_HasNoTextColumn()
	{
		string main = MainOf(await BuildLandingAsync("showcaseCode: dotnet add package Widgets\n", new ThemeLayoutOptions { Home = "showcase" }));

		main.Should().Contain("<section class=\"home-showcase\">").And.Contain("home-showcase-code")
			.And.NotContain("home-showcase-text");
	}

	[Fact]
	public async Task Showcase_CallToActionWithoutALink_HasNoButton_AndALinkWithoutTextSaysGetStarted()
	{
		string noLink = MainOf(await BuildLandingAsync("cta:\n  title: Questions?\n", new ThemeLayoutOptions { Home = "showcase" }));
		string noText = MainOf(await BuildLandingAsync("cta:\n  link: https://example.com/start\n", new ThemeLayoutOptions { Home = "showcase" }));

		noLink.Should().Contain("<h2 class=\"home-cta-title\">Questions?</h2>").And.NotContain("home-cta-button")
			.And.NotContain("home-cta-text");
		noText.Should().Contain("href=\"https://example.com/start\">").And.Contain("Get Started").And.NotContain("home-cta-title");
	}

	[Fact]
	public async Task Showcase_EscapesTheFrontMatter()
	{
		string main = MainOf(await BuildLandingAsync(
			"showcaseTitle: \"<b>Bold</b>\"\nstats:\n  - value: \"<i>1</i>\"\n    label: \"a & b\"\ncta:\n  title: \"<script>x</script>\"\n" +
			"  link: \"/go\\\" onclick=\\\"x\"\n  linkText: \"<em>Go</em>\"\n",
			new ThemeLayoutOptions { Home = "showcase" }));

		main.Should().Contain("&lt;b&gt;Bold&lt;/b&gt;").And.Contain("&lt;i&gt;1&lt;/i&gt;").And.Contain("a &amp; b")
			.And.Contain("&lt;script&gt;x&lt;/script&gt;").And.Contain("&lt;em&gt;Go&lt;/em&gt;")
			.And.Contain("href=\"/go&quot; onclick=&quot;x\"");
		main.Should().NotContain("<b>Bold").And.NotContain("<script>x").And.NotContain("<em>Go");
	}

	[Fact]
	public async Task Minimal_KeepsTheTitleDescriptionButtonsAndContent()
	{
		string main = MainOf(await BuildLandingAsync(_features + "heroCode: var x = 1;\n",
			new ThemeLayoutOptions { Home = "minimal", Hero = "split" }));

		main.Should().Contain("<h1 class=\"landing-hero-title\">Widgets</h1>")
			.And.Contain("<p class=\"landing-hero-subtitle\">Widgets for .NET</p>")
			.And.Contain("<div class=\"landing-hero-actions\">").And.Contain("Get Started").And.Contain("View on GitHub")
			.And.Contain("<div class=\"landing-md-content landing-content\">");
		main.Should().NotContain("landing-features").And.NotContain("hero-grid-pattern").And.NotContain("landing-hero-icon")
			.And.NotContain("landing-hero-media").And.NotContain("landing-divider");
	}

	[Fact]
	public async Task Docs_RendersTheSidebarAndAPageHeaderInsteadOfAHero()
	{
		string landing = await BuildLandingAsync(_features, new ThemeLayoutOptions { Home = "docs" }, "/Repo");

		landing.Should().Contain("<body>").And.NotContain("class=\"landing\"").And.NotContain("landing-hero");
		landing.Should().Contain("<a class=\"skip-link\" href=\"#mokadocs-main\">Skip to content</a>");
		landing.Should().Contain("<button class=\"mobile-nav-toggle\" type=\"button\" aria-label=\"Toggle navigation\" aria-expanded=\"false\" aria-controls=\"mokadocs-sidebar\">");
		landing.Should().Contain("<aside class=\"sidebar\" id=\"mokadocs-sidebar\">").And.Contain("<nav class=\"sidebar-nav\" aria-label=\"Documentation\">");
		landing.Should().Contain("<main class=\"content\" id=\"mokadocs-main\" tabindex=\"-1\">")
			.And.Contain("<article class=\"page-content home-docs\">");
		landing.Should().Contain("<h1>Widgets</h1>").And.Contain("<p class=\"home-docs-lead\">Widgets for .NET</p>");
		landing.Should().Contain("<section class=\"home-docs-features\">")
			.And.Contain("<h2 class=\"home-docs-features-title\">Why Widgets</h2>", "no id, so the heading gets no copy-link anchor")
			.And.Contain("<p class=\"home-docs-features-subtitle\">All in one</p>");
		landing.Should().Contain("<a class=\"component-link-card\" href=\"/Repo/guide/intro\">")
			.And.Contain("<span class=\"component-link-card-title\">Fast</span>")
			.And.Contain("<span class=\"component-link-card-desc\">Quick.</span>")
			.And.Contain("<div class=\"component-link-card\">")
			.And.Contain("<span class=\"home-docs-card-icon\">C#</span>");
		ShouldBeInOrder(landing, "<header class=\"site-header\">", "mobile-nav-toggle", "</header>", "<aside class=\"sidebar\"",
			"<main class=\"content\"", "home-docs-header", "home-docs-features", "<h2 id=\"more\">More</h2>", "</article>", "</main>");
		landing.Should().Contain("<footer class=\"landing-footer\">").And.Contain("id=\"searchModal\"");
	}

	[Fact]
	public async Task Docs_WithoutFeaturesOrDescription_IsTheHeaderAndTheContent()
	{
		Dictionary<string, MockFileData> files = Files("");
		files[MockSite.Root + "/docs/index.md"] = new MockFileData("---\ntitle: Home\nlayout: landing\n---\nWelcome.\n");
		SiteConfig config = MockSite.Config() with
		{
			Theme = new ThemeConfig { Options = new ThemeOptions { Layout = new ThemeLayoutOptions { Home = "docs" } } }
		};
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, files);
		string main = MainOf(fs.File.ReadAllText(_landing));

		main.Should().Contain("<h1>Test Site</h1>").And.Contain("<p>Welcome.</p>")
			.And.NotContain("home-docs-lead").And.NotContain("home-docs-features");
	}

	[Fact]
	public async Task Docs_WithTheSharedSidebarAndToggle_UsesTheDefaultLayoutsSidebar()
	{
		SiteConfig config = MockSite.Config() with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions
				{
					Sidebar = "shared",
					SidebarToggle = true,
					Layout = new ThemeLayoutOptions { Home = "docs" }
				}
			}
		};
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files(""));
		string landing = fs.File.ReadAllText(_landing);
		string page = fs.File.ReadAllText(MockSite.Root + "/_site/guide/intro/index.html");

		landing.Should().Contain("<aside class=\"sidebar\" id=\"mokadocs-sidebar\" data-nav-route=\"/\">")
			.And.Contain("/_theme/js/sidebar.js?v=").And.Contain("id=\"sidebarToggle\"");
		static string Aside(string html) => html[html.IndexOf("</aside>", StringComparison.Ordinal)..html.IndexOf("<main", StringComparison.Ordinal)];
		Aside(landing).Should().Be(Aside(page), "the toggle and its script are the default layout's");
	}
}
