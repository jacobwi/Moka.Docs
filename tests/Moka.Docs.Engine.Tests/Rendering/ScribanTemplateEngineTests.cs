using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Rendering;

/// <summary>
///     Renders pages with the default theme's layouts through <see cref="ScribanTemplateEngine" />
///     directly, for sites too big or values too odd to build through the whole pipeline.
/// </summary>
public sealed class ScribanTemplateEngineTests
{
	private const string _payload = "x\" data-broke=\"1\"><b data-injected>'";

	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	[Fact]
	public void Render_SidebarWith1200Entries_KeepsTheLayout()
	{
		// Scriban's default LoopLimit (1000) counts every iteration under the outer nav loop, so a
		// flat section of about 1000 pages failed every page of the site.
		List<DocPage> pages = Enumerable.Range(0, 1200).Select(i => Page($"/api/type-{i:D4}", $"Type {i}")).ToList();
		var nav = new NavigationTree
		{
			Items =
			[
				new NavigationNode
				{
					Label = "API",
					Route = "/api",
					Children = pages.Select(p => new NavigationNode { Label = p.FrontMatter.Title, Route = p.Route }).ToList()
				}
			]
		};
		ThemeRenderContext context = Context(pages, nav);

		foreach (DocPage page in new[] { pages[0], pages[^1] })
		{
			string html = _engine.RenderPage(page, context, TestContext.Current.CancellationToken);

			html.TrimEnd().Should().EndWith("</html>");
			Regex.Matches(html, "<a class=\"nav-link[^\"]*\" href=\"/api/type-").Should().HaveCount(1200);
			html.Should().Contain("href=\"/api/type-1199\"");
		}
	}

	[Fact]
	public void Render_TwoMegabytePage_IsNotTruncated()
	{
		// Scriban's LimitToString (1 MB) also capped the output: a 1.97 MB page ended at 1,048,581
		// characters with "...", without its footer, scripts or </html>.
		string body = "<p>" + new string('a', 2 * 1024 * 1024) + "</p><p id=\"last-paragraph\">end</p>";
		DocPage page = Page("/big", "Big") with { Content = new PageContent { Html = body } };

		string html = _engine.RenderPage(page, Context([page]), TestContext.Current.CancellationToken);

		html.Length.Should().BeGreaterThan(body.Length);
		html.Should().Contain(body).And.Contain("site-footer");
		html.TrimEnd().Should().EndWith("</html>");
	}

	[Fact]
	public void Render_LayoutThatNeverEnds_StopsAtTheTimeoutWithAnError()
	{
		// Without a loop limit, the time budget is what stops a runaway template.
		var engine = new ScribanTemplateEngine(NullLogger<ScribanTemplateEngine>.Instance)
		{
			RenderTimeout = TimeSpan.FromMilliseconds(200)
		};
		DocPage page = Page("/loop", "Loop");
		ThemeRenderContext context = Context([page], templates: new() { ["default"] = "{{ while true }}{{ end }}" });

		Action render = () => engine.RenderPage(page, context, TestContext.Current.CancellationToken);

		render.Should().Throw<InvalidOperationException>().WithMessage("*did not finish within 0.2 s*");
	}

	[Fact]
	public void Render_CancelledBuild_ThrowsOperationCanceled()
	{
		DocPage page = Page("/loop", "Loop");
		ThemeRenderContext context = Context([page], templates: new() { ["default"] = "{{ while true }}{{ end }}" });
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

		Action render = () => _engine.RenderPage(page, context, cts.Token);

		render.Should().Throw<OperationCanceledException>();
	}

	[Fact]
	public void PrevNext_FollowsTheSidebar_ThenPagesTheSidebarLeavesOut()
	{
		// Ordering every page by order and route interleaved the sections: with these orders the
		// old prev/next went Install, Classes, Overview, Intro, Also unlisted, Unlisted.
		DocPage intro = Page("/guide/intro", "Intro", order: 2);
		DocPage install = Page("/guide/install", "Install", order: 0);
		DocPage overview = Page("/reference/overview", "Overview", order: 1);
		DocPage classes = Page("/reference/classes", "Classes", order: 0);
		DocPage unlisted = Page("/unlisted", "Unlisted", order: 5);
		DocPage unlistedFirst = Page("/also-unlisted", "Also unlisted", order: 3);
		var nav = new NavigationTree
		{
			Items =
			[
				Section("Guide", "/guide/intro", intro, install),
				Section("Reference", null, overview, classes)
			]
		};
		ThemeRenderContext context = Context([classes, unlisted, install, overview, intro, unlistedFirst], nav);

		string[] expected = ["/guide/intro", "/guide/install", "/reference/overview", "/reference/classes", "/also-unlisted", "/unlisted"];
		for (int i = 0; i < expected.Length; i++)
		{
			DocPage page = context.AllPages.Single(p => p.Route == expected[i]);
			string html = _engine.RenderPage(page, context, TestContext.Current.CancellationToken);

			PrevNext(html).Should().Be((i > 0 ? expected[i - 1] : null, i < expected.Length - 1 ? expected[i + 1] : null),
				"prev/next of {0}", page.Route);
		}
	}

	[Fact]
	public void PageListAndOrder_AreReadOncePerRenderPass()
	{
		// The route set and the prev/next order were rebuilt for every page, which made rendering
		// quadratic in the page count. The page list is now read when the first page renders.
		var pages = new CountingPages(Enumerable.Range(0, 40).Select(i => Page($"/p{i}", $"P {i}")));
		ThemeRenderContext context = Context(pages);

		foreach (DocPage page in pages.ToList())
		{
			_engine.RenderPage(page, context, TestContext.Current.CancellationToken);
		}

		pages.Enumerations.Should().BeLessThan(10);
	}

	[Fact]
	public void Layouts_EscapeEveryInterpolatedAttributeValue()
	{
		// Routes, labels and config values went into href, content and data-* attributes raw, so
		// an OpenAPI tag or nav path with a quote broke out of the attribute on every page.
		DocPage current = Page("/guide/" + _payload, "Current " + _payload) with
		{
			SourcePath = "guide/current.md",
			Contributors = [new PageContributor { Name = "Ann " + _payload, Email = "ann@example.com", Commits = 1 }],
			TableOfContents = new TableOfContents
			{
				Entries =
				[
					new TocEntry
					{
						Level = 2, Text = "H2 " + _payload, Id = "h2" + _payload,
						Children =
						[
							new TocEntry
							{
								Level = 3, Text = "H3 " + _payload, Id = "h3" + _payload,
								Children = [new TocEntry { Level = 4, Text = "H4 " + _payload, Id = "h4" + _payload }]
							}
						]
					}
				]
			}
		};
		DocPage previous = Page("/guide/a" + _payload, "Prev " + _payload, order: -1);
		DocPage next = Page("/guide/z" + _payload, "Next " + _payload, order: 1);
		DocPage crumbParent = Page("/top/" + _payload, "Parent " + _payload, order: 2);
		DocPage crumbChild = Page("/top/" + _payload + "/leaf", "Leaf", order: 3);
		var nav = new NavigationTree
		{
			Items =
			[
				new NavigationNode
				{
					Label = "Guide " + _payload,
					Route = "/guide",
					Children =
					[
						new NavigationNode { Label = "P " + _payload, Route = previous.Route },
						new NavigationNode
						{
							Label = "C " + _payload,
							Route = current.Route,
							Children = [new NavigationNode { Label = "Deep " + _payload, Route = "/deep/" + _payload }]
						},
						new NavigationNode { Label = "N " + _payload, Route = next.Route }
					]
				},
				new NavigationNode { Label = "Top " + _payload, Route = "/top/" + _payload }
			]
		};
		SiteConfig config = new()
		{
			Site = new SiteMetadata
			{
				Title = "Site " + _payload,
				Url = "https://example.com/" + _payload,
				EditLink = new EditLinkConfig { Repo = "https://git.example.com/" + _payload },
				Logo = new SiteAssetReference { RawValue = "x", PublishUrl = "https://cdn.example.com/" + _payload, IsAbsoluteUrl = true },
				Favicon = new SiteAssetReference { RawValue = "x", PublishUrl = "https://cdn.example.com/f" + _payload, IsAbsoluteUrl = true }
			},
			Build = new BuildConfig { BasePath = "/base" + _payload },
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions
				{
					CodeTheme = "c" + _payload,
					CodeStyle = "s" + _payload,
					PrimaryColor = "red}</style><b data-injected>",
					AccentColor = "blue}</style><b data-injected>",
					DefaultColorTheme = "';alert(1);'",
					ShowEditLink = true,
					ShowContributors = true,
					ShowVersionSelector = true,
					TocDepth = 4,
					SocialLinks = [new SocialLink { Icon = "i" + _payload, Url = "https://social.example.com/" + _payload }]
				}
			}
		};
		var context = new ThemeRenderContext
		{
			Config = config,
			Navigation = nav,
			Templates = EmbeddedThemeProvider.CreateDefault().Templates,
			Partials = new Dictionary<string, string>(),
			CssFiles = ["/_theme/css/main.css?v=" + _payload],
			JsFiles = ["/_theme/js/main.js?v=" + _payload],
			AllPages = [previous, current, next, crumbParent, crumbChild, Page("/", "Home", layout: "landing")],
			Versions = [new DocVersion { Label = "v1" + _payload, Slug = "v1" + _payload, IsDefault = false }],
			CurrentVersion = new DocVersion { Label = "v1" + _payload, Slug = "v1" + _payload },
			PackageInfo = new PackageMetadata { Name = "Pkg" + _payload, Version = "1.0" + _payload },
			Blog = new BlogSite
			{
				Route = "/blog",
				Title = "Blog " + _payload,
				Feeds =
				[
					new BlogFeed
					{
						Format = "rss",
						Path = "/blog/rss.xml",
						Url = "https://example.com/" + _payload,
						MediaType = "application/rss+xml" + _payload,
						Title = "Feed " + _payload
					}
				]
			}
		};
		DocPage apiIndex = Page("/api", "API " + _payload);
		DocPage landing = Page("/", "Home", layout: "landing") with
		{
			FrontMatter = new FrontMatter { Title = "Home", Layout = "landing", Description = "Desc " + _payload }
		};

		foreach (DocPage page in new[] { current, crumbChild, apiIndex, landing })
		{
			string html = _engine.RenderPage(page, context, TestContext.Current.CancellationToken);

			Around(html, "data-broke=\"1\"").Should().BeEmpty("no value may close its attribute ({0})", page.Route);
			Around(html, "<b data-injected").Should().BeEmpty("no value may open a tag ({0})", page.Route);
			Around(html, "';alert(1);'").Should().BeEmpty("no value may close a script string ({0})", page.Route);
			html.Should().Contain("data-broke=&quot;1&quot;");
		}
	}

	[Fact]
	public void Contributors_ShowUnderTheDate_OnlyWithTheOption()
	{
		DocPage page = Page("/guide/intro", "Intro") with
		{
			LastModified = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero),
			Contributors =
			[
				new PageContributor { Name = "Ann Marie Lee", Email = "ann@example.com", Commits = 3 },
				new PageContributor { Name = "bob", Email = "bob@example.com", Commits = 1 }
			]
		};
		SiteConfig on = Config() with
		{
			Theme = new ThemeConfig { Options = new ThemeOptions { ShowContributors = true } }
		};

		string shown = _engine.RenderPage(page, Context([page], config: on), TestContext.Current.CancellationToken);
		string hidden = _engine.RenderPage(page, Context([page]), TestContext.Current.CancellationToken);

		Regex.Replace(shown, @"\s+", " ").Should().Contain(
			"""<li><span class="page-contributor-avatar" aria-hidden="true">AL</span>Ann Marie Lee</li> <li><span class="page-contributor-avatar" aria-hidden="true">B</span>bob</li>""");
		shown.IndexOf("class=\"page-contributors\"", StringComparison.Ordinal)
			.Should().BeGreaterThan(shown.IndexOf("class=\"last-updated\"", StringComparison.Ordinal));
		shown.Should().NotContain("@example.com", "emails stay out of the page");
		hidden.Should().NotContain("page-contributors").And.NotContain("Ann Marie Lee");
	}

	private static List<string> Around(string html, string text)
	{
		var found = new List<string>();
		for (int at = html.IndexOf(text, StringComparison.Ordinal); at >= 0; at = html.IndexOf(text, at + 1, StringComparison.Ordinal))
		{
			int start = Math.Max(0, at - 120);
			found.Add(html[start..Math.Min(html.Length, at + 40)]);
		}

		return found;
	}

	[Fact]
	public void ContentLinks_GetTheBasePathInEveryAttributeForm()
	{
		// Only href="/x" and src="/x" were rewritten: single-quoted, unquoted and srcset URLs
		// missed the base path and 404ed on a GitHub Pages project site.
		const string content = """
		                       <a href="/a">a</a> <a href='/b'>b</a> <a href=/c>c</a> <a HREF="/d">d</a>
		                       <img src='/img/e.png' srcset="/img/e.png 1x, /img/e@2x.png 2x, https://cdn.example.com/e.png 3x">
		                       <source srcset='/img/f.webp'>
		                       <img data-src="/img/g.png">
		                       <a href="//cdn.example.com/x">x</a> <a href="https://example.com/y">y</a> <a href="#z">z</a> <a href="rel/w">w</a>
		                       <pre><code>&lt;a href='/code' src=/code&gt;</code></pre>
		                       """;
		DocPage page = Page("/links", "Links") with { Content = new PageContent { Html = content } };
		SiteConfig config = Config() with { Build = new BuildConfig { BasePath = "/Repo" } };

		string html = _engine.RenderPage(page, Context([page], config: config), TestContext.Current.CancellationToken);

		html.Should().Contain("""<a href="/Repo/a">a</a> <a href='/Repo/b'>b</a> <a href=/Repo/c>c</a> <a HREF="/Repo/d">d</a>""")
			.And.Contain("""<img src='/Repo/img/e.png' srcset="/Repo/img/e.png 1x, /Repo/img/e@2x.png 2x, https://cdn.example.com/e.png 3x">""")
			.And.Contain("<source srcset='/Repo/img/f.webp'>")
			.And.Contain("<img data-src=\"/Repo/img/g.png\">")
			.And.Contain("""<a href="//cdn.example.com/x">x</a> <a href="https://example.com/y">y</a> <a href="#z">z</a> <a href="rel/w">w</a>""")
			.And.Contain("<pre><code>&lt;a href='/code' src=/code&gt;</code></pre>");
	}

	private static (string? Previous, string? Next) PrevNext(string html)
	{
		string? Find(string kind) =>
			Regex.Match(html, $"class=\"page-nav-link page-nav-{kind}\" href=\"([^\"]*)\"") is { Success: true } m
				? m.Groups[1].Value
				: null;

		return (Find("prev"), Find("next"));
	}

	private static NavigationNode Section(string label, string? route, params DocPage[] children) => new()
	{
		Label = label,
		Route = route,
		Children = children.Select(p => new NavigationNode { Label = p.FrontMatter.Title, Route = p.Route }).ToList()
	};

	private static DocPage Page(string route, string title, int order = 0, string layout = "default") => new()
	{
		Route = route,
		FrontMatter = new FrontMatter { Title = title, Order = order, Layout = layout },
		Content = new PageContent { Html = "<p>" + WebUtility.HtmlEncode(title) + "</p>" }
	};

	private static SiteConfig Config() => new() { Site = new SiteMetadata { Title = "Test" } };

	private static ThemeRenderContext Context(IReadOnlyList<DocPage> pages, NavigationTree? nav = null,
		SiteConfig? config = null, Dictionary<string, string>? templates = null)
	{
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		return new ThemeRenderContext
		{
			Config = config ?? Config(),
			Navigation = nav,
			Templates = templates ?? theme.Templates,
			Partials = theme.Partials,
			CssFiles = theme.CssFiles,
			JsFiles = theme.JsFiles,
			AllPages = pages
		};
	}

	private sealed class CountingPages(IEnumerable<DocPage> pages) : IReadOnlyList<DocPage>
	{
		private readonly List<DocPage> _pages = pages.ToList();

		public int Enumerations { get; private set; }

		public int Count => _pages.Count;

		public DocPage this[int index] => _pages[index];

		public IEnumerator<DocPage> GetEnumerator()
		{
			Enumerations++;
			return _pages.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
