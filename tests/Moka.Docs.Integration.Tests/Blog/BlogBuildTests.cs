using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;

namespace Moka.Docs.Integration.Tests.Blog;

/// <summary>
///     The blog: posts under <c>features.blog.path</c>, the pages that list them, the sidebar entry,
///     search and the feeds, built by the real pipeline over a mock file system.
/// </summary>
public sealed class BlogBuildTests
{
	private const string _site = "/project/_site";
	private static readonly DateTimeOffset _fileTime = new(2025, 12, 24, 8, 0, 0, TimeSpan.Zero);
	private static readonly XNamespace _atom = "http://www.w3.org/2005/Atom";
	private static readonly XNamespace _dc = "http://purl.org/dc/elements/1.1/";

	private static SiteConfig Config(BlogFeatureConfig? blog = null, string url = "https://test.example.com",
		string basePath = "/") => new()
	{
		Site = new SiteMetadata { Title = "Test Site", Url = url, Description = "Site description" },
		Content = new ContentConfig { Docs = "./docs" },
		Features = new FeaturesConfig { Blog = blog ?? new BlogFeatureConfig { Enabled = true } },
		Build = new BuildConfig { BasePath = basePath }
	};

	private static string Post(string title, string? date, string extra = "", string body = "The first paragraph.") =>
		$"---\ntitle: \"{title}\"\n{(date is null ? "" : $"date: {date}\n")}{extra}\n---\n# {title}\n\n{body}\n";

	private static async Task<(BuildContext Context, MockFileSystem Fs)> BuildAsync(Dictionary<string, string> docs,
		SiteConfig? config = null, bool dryRun = false, bool includeDrafts = false)
	{
		var files = new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n") { LastWriteTime = _fileTime }
		};
		foreach ((string path, string content) in docs)
		{
			files["/project/docs/" + path] = new MockFileData(content) { LastWriteTime = _fileTime };
		}

		var fs = new MockFileSystem(files);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IFileSystem>(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		await using ServiceProvider provider = services.BuildServiceProvider();
		var context = new BuildContext
		{
			Config = config ?? Config(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = _site,
			DryRun = dryRun,
			IncludeDrafts = includeDrafts
		};

		await provider.GetRequiredService<BuildPipeline>().ExecuteAsync(context, TestContext.Current.CancellationToken);
		return (context, fs);
	}

	private static string Html(MockFileSystem fs, string route) =>
		fs.File.ReadAllText($"{_site}/{route.Trim('/')}/index.html".Replace("//", "/"));

	private static List<string> CardTitles(string html) =>
		Regex.Matches(html, "<h2 class=\"blog-card-title\"><a href=\"[^\"]*\">([^<]*)</a>").Select(m => m.Groups[1].Value).ToList();

	private static IEnumerable<string> BlogWarnings(BuildContext context) =>
		context.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.Message);

	[Fact]
	public async Task Index_ListsPostsNewestFirst_WithDateAuthorsSummaryTagsAndReadMore()
	{
		var config = Config(new BlogFeatureConfig
		{
			Enabled = true,
			Authors = new Dictionary<string, BlogAuthor>(StringComparer.OrdinalIgnoreCase)
			{
				["jane"] = new() { Name = "Jane Doe", Url = "https://example.com/jane", Avatar = "img/jane.png" }
			}
		});

		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/older.md"] = Post("Older", "2026-01-05", "author: jane\ntags: [release]"),
			["blog/newest.md"] = Post("Newest", "2026-03-01T23:30:00-05:00", "author: [Jane, Guest Writer]\nsummary: Front matter summary"),
			["blog/middle.md"] = Post("Middle", "2026-02-10")
		}, config);

		context.Blog!.Posts.Select(p => p.Title).Should().Equal("Newest", "Middle", "Older");
		string index = Html(fs, "/blog");
		CardTitles(index).Should().Equal("Newest", "Middle", "Older");
		index.Should().Contain("<time datetime=\"2026-03-01\">March 1, 2026</time>",
			"a post is dated by the day it was written in its own offset, not by UTC");
		index.Should().Contain("<a href=\"https://example.com/jane\" rel=\"author\">Jane Doe</a>")
			.And.Contain("<img class=\"blog-author-avatar\" src=\"/img/jane.png\"")
			.And.Contain("<span class=\"blog-author-name\">Guest Writer</span>")
			.And.Contain("<p class=\"blog-card-summary\">Front matter summary</p>")
			.And.Contain("<p class=\"blog-card-summary\">The first paragraph.</p>")
			.And.Contain("<a class=\"blog-tag\" href=\"/blog/tags/release\">release</a>")
			.And.Contain("<a class=\"blog-read-more\" href=\"/blog/newest\">");
		BlogWarnings(context).Should().BeEmpty();
	}

	[Fact]
	public async Task Post_WithoutADate_IsAWarning_AndIsDatedByItsFile()
	{
		(BuildContext context, _) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/undated.md"] = Post("Undated", null)
		});

		context.Blog!.Posts.Should().ContainSingle().Which.Date.Should().Be(_fileTime);
		BlogWarnings(context).Should().ContainSingle().Which.Should()
			.StartWith("blog/undated.md: blog post has no date, so it is dated 2025-12-24");
	}

	[Fact]
	public async Task Post_WithADateThatIsNotIso_WarnsTwice_AndIsDatedByItsFile()
	{
		(BuildContext context, _) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/bad.md"] = Post("Bad", "03/15/2026")
		});

		context.Blog!.Posts[0].Date.Should().Be(_fileTime);
		BlogWarnings(context).Should().HaveCount(2)
			.And.Contain(w => w.Contains("is not an ISO 8601 date"))
			.And.Contain(w => w.Contains("blog post has no date"));
	}

	[Fact]
	public async Task Post_Page_HasTheHeaderUnderItsTitle_WithReadingTimeAndTags()
	{
		string words = string.Join(' ', Enumerable.Repeat("word", 450));
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/long.md"] = Post("Long Read", "2026-02-01", "tags: [deep dive]\nauthor: Ann", words)
		});

		string html = Html(fs, "/blog/long");
		Match header = Regex.Match(html, "<h1 id=\"long-read\">Long Read</h1>\\s*<div class=\"blog-post-header\">");
		header.Success.Should().BeTrue("the header follows the post's own title");
		html.Should().Contain("<span class=\"blog-reading-time\">3 min read</span>")
			.And.Contain("<a class=\"blog-tag\" href=\"/blog/tags/deep-dive\">deep dive</a>")
			.And.Contain("<span class=\"blog-author-name\">Ann</span>");
	}

	[Fact]
	public async Task Post_WithoutAHeading_GetsItsTitleAsOne()
	{
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/plain.md"] = "---\ntitle: \"Plain <b>&</b>\"\ndate: 2026-01-01\n---\nJust text.\n"
		});

		Html(fs, "/blog/plain").Should().Contain("<h1>Plain &lt;b&gt;&amp;&lt;/b&gt;</h1>\n<div class=\"blog-post-header\">");
	}

	[Fact]
	public async Task Post_LinksToTheNextNewerAndOlderPosts_InsteadOfTheDocsOrder()
	{
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("First", "2026-01-01"),
			["blog/b.md"] = Post("Second", "2026-01-02"),
			["blog/c.md"] = Post("Third", "2026-01-03"),
			["guide/intro.md"] = "---\ntitle: Intro\n---\n# Intro\n"
		});

		string middle = Html(fs, "/blog/b");
		middle.Should().Contain("<span class=\"page-nav-label\">Newer post</span>")
			.And.Contain("<span class=\"page-nav-label\">Older post</span>");
		Regex.Match(middle, "page-nav-prev\" href=\"([^\"]+)\"").Groups[1].Value.Should().Be("/blog/c");
		Regex.Match(middle, "page-nav-next\" href=\"([^\"]+)\"").Groups[1].Value.Should().Be("/blog/a");

		Html(fs, "/blog/c").Should().NotContain("page-nav-prev", "the newest post has no newer one");
		Html(fs, "/blog/a").Should().NotContain("page-nav-next", "the oldest post has no older one");

		string guide = Html(fs, "/guide/intro");
		List<string> docsNeighbors = Regex.Matches(guide, "class=\"page-nav-link page-nav-(?:prev|next)\" href=\"([^\"]+)\"")
			.Select(m => m.Groups[1].Value)
			.ToList();
		docsNeighbors.Should().Equal(["/"], "a docs page's previous and next stay among the docs");
		guide.Should().Contain("<span class=\"page-nav-label\">Next</span>");
	}

	[Fact]
	public async Task Pagination_WritesNumberedPages_WithPagerLinks()
	{
		var docs = new Dictionary<string, string>();
		for (int day = 1; day <= 5; day++)
		{
			docs[$"blog/post-{day}.md"] = Post($"Post {day}", $"2026-01-0{day}");
		}

		(BuildContext context, MockFileSystem fs) = await BuildAsync(docs,
			Config(new BlogFeatureConfig { Enabled = true, PostsPerPage = 2 }));

		CardTitles(Html(fs, "/blog")).Should().Equal("Post 5", "Post 4");
		CardTitles(Html(fs, "/blog/page/2")).Should().Equal("Post 3", "Post 2");
		CardTitles(Html(fs, "/blog/page/3")).Should().Equal("Post 1");
		fs.File.Exists($"{_site}/blog/page/4/index.html").Should().BeFalse();

		string second = Html(fs, "/blog/page/2");
		second.Should().Contain("<a class=\"blog-pager-link blog-pager-newer\" href=\"/blog\" rel=\"prev\">")
			.And.Contain("<a class=\"blog-pager-link blog-pager-older\" href=\"/blog/page/3\" rel=\"next\">")
			.And.Contain("<span class=\"blog-pager-status\">Page 2 of 3</span>");
		Html(fs, "/blog").Should().NotContain("blog-pager-newer");
		Html(fs, "/blog/page/3").Should().NotContain("blog-pager-older");

		fs.File.ReadAllText($"{_site}/blog/page/index.html").Should().Contain("url=/blog/\"",
			"a reader who drops the page number lands on the first page, not on page 10");
		context.Blog!.ListingRoutes.Should().StartWith(["/blog", "/blog/page/2", "/blog/page/3"]);
	}

	[Fact]
	public async Task Pagination_UsesTheBasePath()
	{
		var docs = new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01"),
			["blog/b.md"] = Post("B", "2026-01-02")
		};

		(_, MockFileSystem fs) = await BuildAsync(docs,
			Config(new BlogFeatureConfig { Enabled = true, PostsPerPage = 1 }, basePath: "/Repo"));

		Html(fs, "/blog").Should().Contain("href=\"/Repo/blog/page/2\" rel=\"next\"")
			.And.Contain("<a class=\"blog-read-more\" href=\"/Repo/blog/b\">");
		fs.File.ReadAllText($"{_site}/blog/page/index.html").Should().Contain("url=/Repo/blog/\"");
	}

	[Fact]
	public async Task TagPages_HaveSafeUniqueSlugs_AndListTheirPosts()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01", "tags: [\"C#\", csharp, con, Café, \"../etc\", \"a/b?c#d\"]"),
			["blog/b.md"] = Post("B", "2026-01-02", "tags: [c#, Release]"),
			["blog/c.md"] = Post("C", "2026-01-03", "tags: [release]")
		});

		Dictionary<string, string> slugs = context.Blog!.Tags.ToDictionary(t => t.Name, t => t.Slug);
		slugs.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["C#"] = "csharp",
			["csharp"] = "csharp-2",
			["con"] = "con-tag",
			["Café"] = "cafe",
			["../etc"] = "etc",
			["a/b?c#d"] = "a-b-csharpd",
			["Release"] = "release"
		});
		context.Blog.Tags.Single(t => t.Name == "C#").Count.Should().Be(2, "tags match without regard to case");

		foreach (BlogTag tag in context.Blog.Tags)
		{
			tag.Route.Should().Be("/blog/tags/" + tag.Slug);
			fs.File.Exists($"{_site}/blog/tags/{tag.Slug}/index.html").Should().BeTrue();
		}

		CardTitles(Html(fs, "/blog/tags/release")).Should().Equal("C", "B");
		Html(fs, "/blog/tags/release").Should().Contain("<h1>Posts tagged &quot;Release&quot;</h1>");

		string all = Html(fs, "/blog/tags");
		all.Should().Contain("<a class=\"blog-tag\" href=\"/blog/tags/csharp\">C# <span class=\"blog-tag-count\">2");
		context.Pages.Where(p => p.Route.StartsWith("/blog/tags", StringComparison.Ordinal))
			.Should().OnlyContain(p => p.FrontMatter.Visibility == PageVisibility.Hidden);
	}

	[Fact]
	public async Task Drafts_AreLeftOut_UnlessTheBuildIncludesThem_AndNeverReachTheFeed()
	{
		var docs = new Dictionary<string, string>
		{
			["blog/live.md"] = Post("Live", "2026-01-01", "tags: [news]"),
			["blog/wip.md"] = Post("Work in progress", "2026-02-01", "draft: true\ntags: [secret]")
		};

		(BuildContext context, MockFileSystem fs) = await BuildAsync(docs);
		context.Blog!.Posts.Select(p => p.Title).Should().Equal("Live");
		Html(fs, "/blog").Should().NotContain("Work in progress");
		fs.File.Exists($"{_site}/blog/wip/index.html").Should().BeFalse();
		fs.File.Exists($"{_site}/blog/tags/secret/index.html").Should().BeFalse();

		(BuildContext preview, MockFileSystem previewFs) = await BuildAsync(docs, includeDrafts: true);
		preview.Blog!.Posts.Select(p => p.Title).Should().Equal("Work in progress", "Live");
		Html(previewFs, "/blog").Should().Contain("<span class=\"blog-draft\">Draft</span>");
		previewFs.File.ReadAllText($"{_site}/blog/rss.xml").Should().NotContain("Work in progress");
	}

	[Fact]
	public async Task HiddenPost_IsBuiltWithItsHeader_ButNotListed()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/public.md"] = Post("Public", "2026-01-01"),
			["blog/unlisted.md"] = Post("Unlisted", "2026-02-01", "visibility: hidden\ntags: [only-here]")
		});

		context.Blog!.Posts.Select(p => p.Title).Should().Equal("Public");
		string hidden = Html(fs, "/blog/unlisted");
		hidden.Should().Contain("<div class=\"blog-post-header\">")
			.And.NotContain("/blog/tags/only-here", "a tag only a hidden post uses has no page")
			.And.NotContain("page-nav-prev");
		fs.File.ReadAllText($"{_site}/blog/rss.xml").Should().NotContain("Unlisted");
	}

	[Fact]
	public async Task Feeds_AreValidXml_WithAbsoluteUrls_AndEscapedText()
	{
		var config = Config(new BlogFeatureConfig
		{
			Enabled = true,
			Title = "News & <Notes>",
			Feeds = ["rss", "atom"],
			Authors = new Dictionary<string, BlogAuthor>(StringComparer.OrdinalIgnoreCase)
			{
				["jane"] = new() { Name = "Jane <J> & Co", Url = "https://example.com/jane" }
			}
		}, url: "https://user.github.io", basePath: "/Repo");

		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/q-and-a.md"] = Post("Q & A: <generics>", "2026-03-15T09:30:00Z",
				"author: jane\nsummary: \"Use List<T> & friends ]]> safely\"\ntags: [\"c&d\"]"),
			["blog/older.md"] = Post("Older", "2026-01-01")
		}, config);

		XDocument rss = XDocument.Parse(fs.File.ReadAllText($"{_site}/blog/rss.xml"));
		XElement channel = rss.Root!.Element("channel")!;
		channel.Element("title")!.Value.Should().Be("News & <Notes> - Test Site");
		channel.Element("link")!.Value.Should().Be("https://user.github.io/Repo/blog/");
		channel.Element(_atom + "link")!.Attribute("href")!.Value.Should().Be("https://user.github.io/Repo/blog/rss.xml");
		channel.Element("lastBuildDate")!.Value.Should().Be("Sun, 15 Mar 2026 09:30:00 GMT");

		List<XElement> items = channel.Elements("item").ToList();
		items.Select(i => i.Element("title")!.Value).Should().Equal("Q & A: <generics>", "Older");
		XElement first = items[0];
		first.Element("link")!.Value.Should().Be("https://user.github.io/Repo/blog/q-and-a/");
		first.Element("guid")!.Value.Should().Be("https://user.github.io/Repo/blog/q-and-a/");
		first.Element("pubDate")!.Value.Should().Be("Sun, 15 Mar 2026 09:30:00 GMT");
		first.Element("description")!.Value.Should().Be("Use List<T> & friends ]]> safely");
		first.Element(_dc + "creator")!.Value.Should().Be("Jane <J> & Co");
		first.Element("category")!.Value.Should().Be("c&d");
		items[1].Element("description")!.Value.Should().Be("The first paragraph.");

		XDocument atom = XDocument.Parse(fs.File.ReadAllText($"{_site}/blog/atom.xml"));
		XElement feed = atom.Root!;
		feed.Name.Should().Be(_atom + "feed");
		feed.Element(_atom + "id")!.Value.Should().Be("https://user.github.io/Repo/blog/");
		feed.Element(_atom + "updated")!.Value.Should().Be("2026-03-15T09:30:00+00:00");
		feed.Elements(_atom + "link").Select(l => l.Attribute("href")!.Value)
			.Should().Equal("https://user.github.io/Repo/blog/atom.xml", "https://user.github.io/Repo/blog/");
		XElement entry = feed.Elements(_atom + "entry").First();
		entry.Element(_atom + "title")!.Value.Should().Be("Q & A: <generics>");
		entry.Element(_atom + "link")!.Attribute("href")!.Value.Should().Be("https://user.github.io/Repo/blog/q-and-a/");
		entry.Element(_atom + "summary")!.Value.Should().Be("Use List<T> & friends ]]> safely");
		entry.Element(_atom + "author")!.Element(_atom + "uri")!.Value.Should().Be("https://example.com/jane");
		entry.Element(_atom + "category")!.Attribute("term")!.Value.Should().Be("c&d");
	}

	[Fact]
	public async Task Feed_ListsTheTwentyLatestPosts()
	{
		var docs = new Dictionary<string, string>();
		for (int day = 1; day <= 25; day++)
		{
			docs[$"blog/day-{day:00}.md"] = Post($"Day {day}", $"2026-01-{day:00}");
		}

		(_, MockFileSystem fs) = await BuildAsync(docs);

		List<string> titles = XDocument.Parse(fs.File.ReadAllText($"{_site}/blog/rss.xml"))
			.Descendants("item").Select(i => i.Element("title")!.Value).ToList();
		titles.Should().HaveCount(20);
		titles[0].Should().Be("Day 25");
		titles[^1].Should().Be("Day 6");
	}

	[Fact]
	public async Task Feed_WithoutSiteUrl_IsAWarning_AndNothingLinksToIt()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01")
		}, Config(url: ""));

		BlogWarnings(context).Should().ContainSingle().Which.Should().Contain("set site.url");
		fs.File.Exists($"{_site}/blog/rss.xml").Should().BeFalse();
		context.Blog!.Feeds.Should().BeEmpty();
		Html(fs, "/blog").Should().NotContain("rel=\"alternate\"");
	}

	[Fact]
	public async Task EveryPage_LinksTheFeedsFromItsHead()
	{
		(_, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01"),
			["guide/intro.md"] = "---\ntitle: Intro\n---\n# Intro\n",
			["landing.md"] = "---\ntitle: Landing\nlayout: landing\n---\n# Landing\n"
		}, Config(new BlogFeatureConfig { Enabled = true, Title = "News", Feeds = ["rss", "atom"] }));

		foreach (string route in new[] { "/", "/guide/intro", "/blog", "/blog/a", "/landing" })
		{
			string head = Html(fs, route).Split("</head>")[0];
			head.Should().Contain(
				"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"News - Test Site (RSS)\" href=\"https://test.example.com/blog/rss.xml\" />",
				route);
			head.Should().Contain(
				"<link rel=\"alternate\" type=\"application/atom+xml\" title=\"News - Test Site (Atom)\" href=\"https://test.example.com/blog/atom.xml\" />",
				route);
		}
	}

	[Fact]
	public async Task Search_FilesPostsUnderBlog_AndSkipsTheListingsAfterTheFirstPage()
	{
		(BuildContext context, _) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("Alpha", "2026-01-01", "tags: [x]", "Alpha text.\n\n## Details\n\nMore."),
			["blog/b.md"] = Post("Beta", "2026-01-02"),
			["guide/intro.md"] = "---\ntitle: Intro\n---\n# Intro\n"
		}, Config(new BlogFeatureConfig { Enabled = true, PostsPerPage = 1 }));

		List<SearchEntry> entries = context.SearchIndex!.Entries.ToList();
		entries.Where(e => e.Title == "Alpha").Should().HaveCountGreaterThan(1, "headings get entries of their own")
			.And.OnlyContain(e => e.Category == "Blog");
		entries.Should().Contain(e => e.Route == "/blog" && e.Category == "Blog");
		entries.Should().Contain(e => e.Route == "/guide/intro" && e.Category == "Documentation");
		entries.Should().NotContain(e => e.Route.StartsWith("/blog/tags", StringComparison.Ordinal)
		                                 || e.Route.StartsWith("/blog/page", StringComparison.Ordinal));
	}

	[Fact]
	public async Task DryRun_BuildsTheBlog_ButWritesNothing()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01", "tags: [x]")
		}, dryRun: true);

		context.Blog!.Posts.Should().ContainSingle();
		context.Blog.Feeds.Should().ContainSingle();
		fs.Directory.Exists(_site).Should().BeFalse();
	}

	[Fact]
	public async Task IntroPage_IsShownAboveThePosts_AndIsNoPost()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/index.md"] = "---\ntitle: Our Blog\ndescription: From the intro\norder: 7\nicon: rocket\n---\nWelcome to **the** blog.\n",
			["blog/a.md"] = Post("A", "2026-01-01")
		});

		context.Blog!.Posts.Should().ContainSingle();
		context.Pages.Count(p => p.Route == "/blog").Should().Be(1, "the generated index replaces the intro page");
		DocPage index = context.Pages.Single(p => p.Route == "/blog");
		index.SourcePath!.Replace('\\', '/').Should().Be("blog/index.md", "its edit link opens the intro");
		index.FrontMatter.Order.Should().Be(7);
		index.FrontMatter.Icon.Should().Be("rocket");

		string html = Html(fs, "/blog");
		html.Should().Contain("<h1>Blog</h1>\n<p class=\"blog-description\">From the intro</p>\n<p>Welcome to <strong>the</strong> blog.</p>");
		html.IndexOf("Welcome", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("blog-card", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Sidebar_HasOneBlogEntry_ListingTheLatestPosts()
	{
		var docs = new Dictionary<string, string>
		{
			["guide/intro.md"] = "---\ntitle: Intro\n---\n# Intro\n"
		};
		for (int day = 1; day <= 4; day++)
		{
			docs[$"blog/post-{day}.md"] = Post($"Post {day}", $"2026-01-0{day}");
		}

		(BuildContext context, _) = await BuildAsync(docs,
			Config(new BlogFeatureConfig { Enabled = true, Title = "News", SidebarPosts = 2, PostsPerPage = 1 }));

		NavigationNode blog = context.Navigation!.Items.Should().ContainSingle(n => n.Route == "/blog").Subject;
		blog.Label.Should().Be("News");
		blog.Children.Select(c => c.Label).Should().Equal("Post 4", "Post 3");
		AllRoutes(context.Navigation.Items).Should().NotContain(r => r.StartsWith("/blog/page", StringComparison.Ordinal)
		                                                             || r.StartsWith("/blog/tags", StringComparison.Ordinal));
	}

	[Fact]
	public async Task SidebarFromConfig_FillsTheBlogItem_UnlessItHasChildrenOfItsOwn()
	{
		var docs = new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01"),
			["blog/b.md"] = Post("B", "2026-01-02")
		};

		SiteConfig config = Config() with
		{
			Nav = [new NavItem { Label = "Home", Path = "/" }, new NavItem { Label = "Posts", Path = "/blog/" }]
		};
		(BuildContext context, _) = await BuildAsync(docs, config);
		context.Navigation!.Items[1].Children.Select(c => c.Route).Should().Equal("/blog/b", "/blog/a");

		SiteConfig explicitChildren = config with
		{
			Nav = [new NavItem { Label = "Posts", Path = "/blog", Children = [new NavItem { Label = "Only A", Path = "/blog/a" }] }]
		};
		(BuildContext kept, _) = await BuildAsync(docs, explicitChildren);
		kept.Navigation!.Items[0].Children.Select(c => c.Label).Should().Equal("Only A");
	}

	[Fact]
	public async Task Disabled_LeavesTheFolderAsOrdinaryPages()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01", "tags: [x]")
		}, Config(new BlogFeatureConfig()));

		context.Blog.Should().BeNull();
		Html(fs, "/blog/a").Should().NotContain("blog-post-header");
		fs.File.Exists($"{_site}/blog/tags/x/index.html").Should().BeFalse();
		fs.File.Exists($"{_site}/blog/rss.xml").Should().BeFalse();
		BlogWarnings(context).Should().BeEmpty("blog front matter is not unknown even when the blog is off");
	}

	[Fact]
	public async Task SettingsThatCantBeUsed_AreWarnings_AndFallBack()
	{
		var config = Config(new BlogFeatureConfig
		{
			Enabled = true,
			Path = "../outside",
			Route = "/",
			PostsPerPage = 0,
			SidebarPosts = -1,
			Feeds = ["rss", "json"]
		});

		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01")
		}, config);

		BlogWarnings(context).Should().BeEquivalentTo(
			"features.blog.path '../outside' must name a folder inside the docs folder; using 'blog'",
			"features.blog.route '/' can't be used (the blog needs a route of its own below the site root); using '/blog'",
			"features.blog.postsPerPage must be 1 or more, not 0; using 10",
			"features.blog.sidebarPosts must be 0 or more, not -1; using 5",
			"features.blog.feed 'json' is not a feed format; use rss, atom or both");
		context.Blog!.Route.Should().Be("/blog");
		fs.File.Exists($"{_site}/blog/rss.xml").Should().BeTrue();
	}

	[Fact]
	public async Task RouteAndPath_CanDiffer_PostsKeepTheirFileRoutes()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["posts/2026/a.md"] = Post("A", "2026-01-01", "tags: [x]")
		}, Config(new BlogFeatureConfig { Enabled = true, Path = "./posts/", Route = "news" }));

		context.Blog!.Route.Should().Be("/news");
		context.Blog.Posts.Single().Route.Should().Be("/posts/2026/a");
		CardTitles(Html(fs, "/news")).Should().Equal("A");
		fs.File.Exists($"{_site}/news/tags/x/index.html").Should().BeTrue();
		fs.File.Exists($"{_site}/news/rss.xml").Should().BeTrue();
	}

	[Fact]
	public async Task ListingRouteTakenByAPage_IsNotGenerated_AndThePageWins()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/a.md"] = Post("A", "2026-01-01"),
			["blog/tags.md"] = Post("Tags Post", "2026-01-02")
		});

		BlogWarnings(context).Should().ContainSingle()
			.Which.Should().Be("The blog's page '/blog/tags' was not generated: another page already has that route");
		Html(fs, "/blog/tags").Should().Contain("Tags Post");
	}

	[Fact]
	public async Task CoverImage_RelativeToThePost_ResolvesFromTheSiteRoot()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(new Dictionary<string, string>
		{
			["blog/2026/launch.md"] = Post("Launch", "2026-01-01", "image: ./cover.png"),
			["blog/remote.md"] = Post("Remote", "2026-01-02", "image: https://cdn.example.com/c.png")
		}, Config(basePath: "/Repo"));

		context.Blog!.Posts.Select(p => p.Image).Should().Equal("https://cdn.example.com/c.png", "/blog/2026/cover.png");
		Html(fs, "/blog/2026/launch").Should().Contain("<img class=\"blog-cover\" src=\"/Repo/blog/2026/cover.png\" alt=\"\" />");
		Html(fs, "/blog").Should().Contain("<img src=\"https://cdn.example.com/c.png\" alt=\"\" loading=\"lazy\" />");
	}

	private static IEnumerable<string> AllRoutes(IEnumerable<NavigationNode> nodes) =>
		nodes.SelectMany(n => (n.Route is null ? [] : new[] { n.Route }).Concat(AllRoutes(n.Children)));
}
