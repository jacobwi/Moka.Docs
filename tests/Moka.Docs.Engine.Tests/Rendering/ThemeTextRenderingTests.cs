using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Rendering;

/// <summary>The default theme's layouts with the site's language, direction and own wording.</summary>
public sealed class ThemeTextRenderingTests
{
	private const string _payload = "x\" data-broke=\"1\"><b data-injected>'</script>{{ site.title }}";

	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	// Every text replaced by one that tries to leave its attribute, open a tag, end a script or run template code.
	[Fact]
	public void Layouts_EscapeEveryText()
	{
		var overrides = ThemeStrings.All.ToDictionary(s => s.Key, s => s.English + " " + _payload);
		SiteConfig config = Config("en") with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions
				{
					Strings = overrides,
					ShowEditLink = true,
					ShowLastUpdated = true,
					ShowContributors = true,
					ShowVersionSelector = true,
					LayoutSelector = true,
					SidebarToggle = true,
					Banner = new ThemeBanner { Text = "News", Link = "/news" }
				}
			}
		};
		DocPage page = Page("/guide/intro", "Intro") with
		{
			SourcePath = "guide/intro.md",
			LastModified = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero),
			Contributors = [new PageContributor { Name = "Ann", Email = "a@example.com", Commits = 1 }],
			TableOfContents = Toc()
		};
		ThemeRenderContext withText = Context(config, [Page("/guide/a", "A", -1), page, Page("/guide/z", "Z", 1)], ThemeText.For(config), true);
		DocPage apiIndex = Page("/api", "API");
		DocPage landing = Page("/", "Home") with { FrontMatter = new FrontMatter { Title = "Home", Layout = "landing" } };

		string[] pages =
		[
			_engine.RenderPage(page, withText, TestContext.Current.CancellationToken),
			_engine.RenderPage(apiIndex, withText, TestContext.Current.CancellationToken),
			_engine.RenderPage(landing, withText, TestContext.Current.CancellationToken),
			_engine.RenderNotFoundPage(withText, TestContext.Current.CancellationToken),
			_engine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(), withText, TestContext.Current.CancellationToken)
		];

		foreach (string html in pages)
		{
			html.Should().NotContain("data-broke=\"1\"").And.NotContain("<b data-injected").And.NotContain("'</script>");
			html.Should().Contain("data-broke=&quot;1&quot;").And.Contain("{{ site.title }}", "a text is never run as a template");
		}

		pages[0].Should().Contain("id=\"tocTitle\">On this page x&quot; data-broke=&quot;1&quot;&gt;&lt;b data-injected&gt;");
	}

	[Fact]
	public void Page_TakesTheLanguageDirectionAndTranslation()
	{
		SiteConfig config = Config("de") with { Site = Config("de").Site with { Url = "https://example.com" } };
		DocPage page = Page("/guide/intro", "Intro") with
		{
			LastModified = new DateTimeOffset(2026, 3, 5, 10, 0, 0, TimeSpan.Zero),
			TableOfContents = Toc()
		};
		ThemeRenderContext context = Context(config, [page], ThemeText.For(config));

		string html = _engine.RenderPage(page, context, TestContext.Current.CancellationToken);

		html.Should().Contain("<html class=\"no-js\" lang=\"de\" dir=\"ltr\" ");
		html.Should().Contain("<p class=\"toc-title\" id=\"tocTitle\">Auf dieser Seite</p>");
		html.Should().Contain("Zuletzt aktualisiert: <time datetime=\"2026-03-05\">" + WebUtility.HtmlEncode("5. März 2026") + "</time>");
		html.Should().Contain(">Startseite</a>", "the first breadcrumb");
		html.Should().Contain("aria-label=\"Dokumentation durchsuchen\"");
		html.Should().NotContain("og:locale", "de has no region to name");
	}

	[Theory]
	[InlineData("pt-BR", "pt-BR", "ltr", "pt_BR")]
	[InlineData("ar", "ar", "rtl", "")]
	[InlineData("ja", "ja", "ltr", "ja_JP")]
	[InlineData("en", "en", "ltr", "")]
	public void EveryLayout_WritesLangDirAndLocale(string language, string lang, string dir, string locale)
	{
		SiteConfig config = Config(language) with { Site = Config(language).Site with { Url = "https://example.com" } };
		DocPage page = Page("/guide/intro", "Intro");
		DocPage landing = Page("/", "Home") with { FrontMatter = new FrontMatter { Title = "Home", Layout = "landing" } };
		ThemeRenderContext context = Context(config, [page, landing], ThemeText.For(config));

		foreach (string html in new[]
		         {
			         _engine.RenderPage(page, context, TestContext.Current.CancellationToken),
			         _engine.RenderPage(landing, context, TestContext.Current.CancellationToken),
			         _engine.RenderNotFoundPage(context, TestContext.Current.CancellationToken)
		         })
		{
			html.Should().Contain($"lang=\"{lang}\" dir=\"{dir}\"");
			if (locale.Length > 0 && html.Contains("og:title", StringComparison.Ordinal))
			{
				html.Should().Contain($"<meta property=\"og:locale\" content=\"{locale}\" />");
			}
			else
			{
				html.Should().NotContain("og:locale");
			}
		}
	}

	[Fact]
	public void NotFoundPage_IsInTheSiteLanguage()
	{
		SiteConfig config = Config("fr") with { Features = new FeaturesConfig { Search = new SearchFeatureConfig { Enabled = true } } };
		ThemeText text = ThemeText.For(config);

		string html = _engine.RenderNotFoundPage(Context(config, [], text), TestContext.Current.CancellationToken);

		html.Should().Contain("<h1>" + WebUtility.HtmlEncode(text["notFound.title"]) + "</h1>");
		html.Should().Contain("data-open-search>" + WebUtility.HtmlEncode(text["notFound.search"]) + "</a>");
		html.Should().NotContain("Page not found");
	}

	private static TableOfContents Toc() => new()
	{
		Entries =
		[
			new TocEntry { Level = 2, Text = "One", Id = "one" },
			new TocEntry { Level = 2, Text = "Two", Id = "two" }
		]
	};

	private static SiteConfig Config(string language) => new() { Site = new SiteMetadata { Title = "Test", Language = language } };

	private static DocPage Page(string route, string title, int order = 0) => new()
	{
		Route = route,
		FrontMatter = new FrontMatter { Title = title, Order = order },
		Content = new PageContent { Html = "<p>" + WebUtility.HtmlEncode(title) + "</p>" }
	};

	private static ThemeRenderContext Context(SiteConfig config, IReadOnlyList<DocPage> pages, ThemeText? text = null, bool extras = false)
	{
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		return new ThemeRenderContext
		{
			Versions = extras ? [new DocVersion { Label = "v1", Slug = "v1", IsDefault = true, IsPrerelease = true }] : [],
			CurrentVersion = extras ? new DocVersion { Label = "v1", Slug = "v1" } : null,
			PackageInfo = extras ? new PackageMetadata { Name = "Pkg", Version = "1.0" } : null,
			Config = config,
			Navigation = new NavigationTree
			{
				Items =
				[
					new NavigationNode
					{
						Label = "Guide",
						Route = "/guide",
						Children = [.. pages.Where(p => p.Route.StartsWith("/guide/", StringComparison.Ordinal))
							.Select(p => new NavigationNode { Label = p.FrontMatter.Title, Route = p.Route })]
					}
				]
			},
			Templates = theme.Templates,
			Partials = theme.Partials,
			CssFiles = theme.CssFiles,
			JsFiles = theme.JsFiles,
			AllPages = pages,
			Text = text ?? ThemeText.English
		};
	}
}
