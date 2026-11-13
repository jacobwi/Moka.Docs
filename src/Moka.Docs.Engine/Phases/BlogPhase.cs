using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Moka.Docs.Engine.Blog;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Turns the Markdown files under <c>features.blog.path</c> into a blog: adds a header to each
///     post, generates the paginated index, the tags page and one page per tag, and stages the
///     RSS and Atom feeds for the output phase.
/// </summary>
/// <remarks>
///     Runs after <see cref="FeatureGatePhase" />, so a post that requires a disabled feature is
///     neither listed nor in the feeds, and before navigation, search and rendering, which see the
///     generated pages like any other. The feeds go to <see cref="BuildContext.DeferredOutputFiles" />,
///     which the output phase writes after its clean and skips on a dry run.
/// </remarks>
public sealed class BlogPhase(ILogger<BlogPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "Blog";

	/// <inheritdoc />
	public int Order => 550;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (!context.Config.Features.Blog.Enabled)
		{
			return Task.CompletedTask;
		}

		BlogSettings settings = BlogSettings.Resolve(context.Config.Features.Blog, context.Diagnostics, Name);
		(DocPage? intro, List<DocPage> postPages) = FindPosts(context, settings);

		List<(DocPage Page, BlogPost Post)> posts = postPages
			.Select(page => (page, CreatePost(context, settings, page)))
			.ToList();

		var tagSlugs = new TagSlugs(settings.Route, posts.SelectMany(p => p.Post.Tags).Select(t => t.Name));
		posts = posts.Select(p => (p.Page, p.Post with { Tags = tagSlugs.Resolve(p.Post.Tags) })).ToList();

		List<BlogPost> listed = LinkNeighbors(posts
			.Where(p => IsListed(context, p.Page))
			.Select(p => p.Post)
			.OrderByDescending(p => p.Date)
			.ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.Route, StringComparer.Ordinal)
			.ToList());
		List<BlogTag> tags = TagSlugs.Count(listed);

		AddPostHeaders(context, settings, posts, listed, tags);
		List<string> listingRoutes = AddListingPages(context, settings, intro, listed, tags);
		IReadOnlyList<BlogFeed> feeds = StageFeeds(context, settings, listed);

		context.Blog = new BlogSite
		{
			Route = settings.Route,
			Title = settings.Title,
			Description = settings.Description,
			Posts = listed,
			Tags = tags,
			ListingRoutes = listingRoutes,
			Feeds = feeds
		};

		logger.LogInformation("Built the blog at {Route}: {Posts} posts, {Tags} tags, {Pages} listing pages",
			settings.Route, listed.Count, tags.Count, listingRoutes.Count);
		return Task.CompletedTask;
	}

	private static (DocPage? Intro, List<DocPage> Posts) FindPosts(BuildContext context, BlogSettings settings)
	{
		string folder = settings.Path + "/";
		DocPage? intro = null;
		var posts = new List<DocPage>();
		foreach (DocPage page in context.Pages)
		{
			string? source = page.SourcePath?.Replace('\\', '/');
			if (page.Origin != PageOrigin.Markdown || source is null
			                                       || !source.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (source.Equals(folder + "index.md", StringComparison.OrdinalIgnoreCase))
			{
				intro = page;
			}
			else
			{
				posts.Add(page);
			}
		}

		return (intro, posts);
	}

	// Hidden posts are built but not listed, like hidden pages are left out of the sidebar. Drafts
	// are listed only in a build that includes them.
	private static bool IsListed(BuildContext context, DocPage page) =>
		page.FrontMatter.Visibility == PageVisibility.Public
		|| (page.FrontMatter.Visibility == PageVisibility.Draft && context.IncludeDrafts);

	private BlogPost CreatePost(BuildContext context, BlogSettings settings, DocPage page)
	{
		FrontMatter fm = page.FrontMatter;
		DateTimeOffset date;
		if (fm.Date is { } written)
		{
			date = written;
		}
		else
		{
			date = page.LastModified ?? DateTimeOffset.UtcNow;
			context.Diagnostics.Warning(
				$"{page.SourcePath?.Replace('\\', '/')}: blog post has no date, so it is dated " +
				$"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} from its last commit or file time; " +
				"add 'date: yyyy-MM-dd' to its front matter", Name);
		}

		return new BlogPost
		{
			Route = page.Route,
			Title = fm.Title,
			SourcePath = page.SourcePath,
			Date = date,
			Authors = ResolveAuthors(fm.Authors, settings.Config),
			Summary = fm.Summary ?? BlogText.FirstParagraph(page.Content.Html),
			Tags = fm.Tags
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.Select(t => new BlogTag { Name = t.Trim(), Slug = "", Route = "" })
				.DistinctBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
				.ToList(),
			Image = ResolveImage(fm.Image, page.SourcePath),
			ReadingMinutes = BlogText.ReadingMinutes(page.Content.PlainText),
			IsDraft = fm.Visibility == PageVisibility.Draft
		};
	}

	private static List<BlogAuthor> ResolveAuthors(List<string> names, BlogFeatureConfig config)
	{
		var authors = new List<BlogAuthor>();
		foreach (string name in names)
		{
			BlogAuthor author = config.Authors.TryGetValue(name, out BlogAuthor? defined)
				? defined with { Avatar = SiteRelative(defined.Avatar) }
				: new BlogAuthor { Name = name };
			if (!authors.Contains(author))
			{
				authors.Add(author);
			}
		}

		return authors;
	}

	// mokadocs.yaml applies to every page, so "img/jane.png" there means the file at the site root.
	private static string? SiteRelative(string? url)
	{
		if (url is null || url.StartsWith('/') || IsAbsoluteUrl(url))
		{
			return url;
		}

		return "/" + url.TrimStart('.', '/');
	}

	private static string? ResolveImage(string? image, string? sourcePath)
	{
		if (image is null || IsAbsoluteUrl(image) || image.StartsWith('/') || sourcePath is null)
		{
			return image;
		}

		return RelativeLinks.ToRootRelative(image, sourcePath) ?? image;
	}

	private static bool IsAbsoluteUrl(string url) =>
		url.StartsWith("//", StringComparison.Ordinal)
		|| (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme.Length > 1);

	private static List<BlogPost> LinkNeighbors(List<BlogPost> posts)
	{
		var linked = new List<BlogPost>(posts.Count);
		for (int i = 0; i < posts.Count; i++)
		{
			linked.Add(posts[i] with
			{
				Newer = i > 0 ? new BlogPostLink(posts[i - 1].Title, posts[i - 1].Route) : null,
				Older = i < posts.Count - 1 ? new BlogPostLink(posts[i + 1].Title, posts[i + 1].Route) : null
			});
		}

		return linked;
	}

	private static void AddPostHeaders(BuildContext context, BlogSettings settings,
		List<(DocPage Page, BlogPost Post)> posts, List<BlogPost> listed, List<BlogTag> tags)
	{
		var pageIndex = new Dictionary<DocPage, int>(ReferenceEqualityComparer.Instance);
		for (int i = 0; i < context.Pages.Count; i++)
		{
			pageIndex[context.Pages[i]] = i;
		}

		var tagsWithPages = new HashSet<string>(tags.Select(t => t.Slug), StringComparer.Ordinal);
		foreach ((DocPage page, BlogPost post) in posts)
		{
			// A tag only a hidden post uses has no page to link to.
			BlogPost shown = post with { Tags = post.Tags.Where(t => tagsWithPages.Contains(t.Slug)).ToList() };
			string header = BlogHtml.PostHeader(shown, settings.Config.ShowAuthors, ThemeText.For(context.Config));
			context.Pages[pageIndex[page]] = page with
			{
				Content = page.Content with
				{
					Html = BlogHtml.WithPostHeader(page.Content.Html, page.FrontMatter.Title, header)
				}
			};
		}
	}

	private List<string> AddListingPages(BuildContext context, BlogSettings settings, DocPage? intro,
		List<BlogPost> listed, List<BlogTag> tags)
	{
		if (intro is not null)
		{
			context.Pages.Remove(intro);
		}

		var taken = new HashSet<string>(context.Pages.Select(p => "/" + p.Route.Trim('/')), StringComparer.OrdinalIgnoreCase);
		var routes = new List<string>();
		bool showAuthors = settings.Config.ShowAuthors;
		DateTimeOffset? newest = listed.Count > 0 ? listed[0].Date : null;
		ThemeText text = ThemeText.For(context.Config);

		void Add(DocPage page)
		{
			if (!taken.Add(page.Route))
			{
				context.Diagnostics.Warning(
					$"The blog's page '{page.Route}' was not generated: another page already has that route", Name);
				return;
			}

			context.Pages.Add(page);
			routes.Add(page.Route);
		}

		string introHtml = intro?.Content.Html ?? "";
		string description = settings.Description.Length > 0 ? settings.Description : intro?.FrontMatter.Description ?? "";
		string lead = introHtml.TrimStart().StartsWith("<h1", StringComparison.OrdinalIgnoreCase)
			? introHtml
			: BlogHtml.Paragraph("blog-description", description) + introHtml;

		List<List<BlogPost>> pages = Paginate(listed, settings.PostsPerPage);
		StagePageFolderRedirect(context, settings.Route, pages.Count);
		for (int n = 1; n <= pages.Count; n++)
		{
			Add(new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = n == 1 ? settings.Title : PageTitle(text, settings.Title, n),
					Description = description,
					Icon = intro?.FrontMatter.Icon ?? "newspaper",
					Order = intro?.FrontMatter.Order ?? 0,
					Expanded = intro?.FrontMatter.Expanded ?? true,
					Toc = false,
					Visibility = n == 1 ? PageVisibility.Public : PageVisibility.Hidden
				},
				Content = new PageContent
				{
					Html = BlogHtml.ListPage(settings.Title, n == 1 ? lead : "", pages[n - 1], showAuthors,
						Pager(settings.Route, n, pages.Count), text),
					PlainText = n == 1 ? $"{description}\n{intro?.Content.PlainText}".Trim() : ""
				},
				// The edit link of the first page opens the introduction it shows.
				SourcePath = n == 1 ? intro?.SourcePath : null,
				Route = PageRoute(settings.Route, n),
				LastModified = newest
			});
		}

		string tagsRoute = settings.Route + "/tags";
		Add(ListingPage(tagsRoute, text["blog.tags"], text.Format("blog.tagsDescription", ("blog", settings.Title)),
			BlogHtml.TagsPage(text["blog.tags"], tags, text), newest));

		foreach (BlogTag tag in tags)
		{
			List<BlogPost> tagged = listed.Where(p => p.Tags.Any(t => t.Slug == tag.Slug)).ToList();
			List<List<BlogPost>> tagPages = Paginate(tagged, settings.PostsPerPage);
			StagePageFolderRedirect(context, tag.Route, tagPages.Count);
			string title = text.Format("blog.taggedTitle", ("tag", tag.Name));
			for (int n = 1; n <= tagPages.Count; n++)
			{
				string html = BlogHtml.ListPage(title, BlogHtml.TagLead(tag.Count, tagsRoute, text), tagPages[n - 1],
					showAuthors, Pager(tag.Route, n, tagPages.Count), text);
				Add(ListingPage(PageRoute(tag.Route, n), n == 1 ? title : PageTitle(text, title, n),
					text.Format("blog.taggedDescription", ("tag", tag.Name), ("blog", settings.Title)), html, tagged[0].Date));
			}
		}

		return routes;
	}

	private static DocPage ListingPage(string route, string title, string description, string html,
		DateTimeOffset? lastModified) => new()
	{
		FrontMatter = new FrontMatter
		{
			Title = title,
			Description = description,
			Toc = false,
			Visibility = PageVisibility.Hidden
		},
		Content = new PageContent { Html = html },
		Route = route,
		LastModified = lastModified
	};

	// Without this, "/blog/page/" got the output phase's section redirect to the first page folder
	// in name order, which is page 10 once there are ten pages. Deferred files are written after
	// that redirect, so this one replaces it.
	private static void StagePageFolderRedirect(BuildContext context, string firstPage, int pageCount)
	{
		if (pageCount < 2)
		{
			return;
		}

		string basePath = context.Config.Build.BasePath == "/" ? "" : context.Config.Build.BasePath;
		string target = WebUtility.HtmlEncode(basePath + firstPage + "/");
		ThemeText text = ThemeText.For(context.Config);
		string link = $"<a href=\"{target}\">{target}</a>";
		string html = $"""
		               <!DOCTYPE html>
		               {OutputPhase.HtmlStart(context.Config, text)}
		               <head><meta http-equiv="refresh" content="0; url={target}"><link rel="canonical" href="{target}"></head>
		               <body><p>{WebUtility.HtmlEncode(text["redirect.page"]).Replace("{link}", link, StringComparison.Ordinal)}</p></body>
		               </html>
		               """;
		context.DeferredOutputFiles[$"{firstPage.TrimStart('/')}/page/index.html"] = Encoding.UTF8.GetBytes(html);
	}

	private static string PageTitle(ThemeText text, string title, int page) =>
		text.Format("blog.pageTitle", ("title", title), ("page", page.ToString(CultureInfo.InvariantCulture)));

	private static BlogHtml.Pager? Pager(string firstPage, int page, int pageCount) =>
		pageCount > 1
			? new BlogHtml.Pager(page, pageCount,
				page > 1 ? PageRoute(firstPage, page - 1) : null,
				page < pageCount ? PageRoute(firstPage, page + 1) : null)
			: null;

	private static string PageRoute(string firstPage, int page) =>
		page == 1 ? firstPage : $"{firstPage}/page/{page.ToString(CultureInfo.InvariantCulture)}";

	private static List<List<BlogPost>> Paginate(List<BlogPost> posts, int perPage)
	{
		List<List<BlogPost>> pages = posts.Chunk(perPage).Select(chunk => chunk.ToList()).ToList();
		return pages.Count > 0 ? pages : [[]];
	}

	private IReadOnlyList<BlogFeed> StageFeeds(BuildContext context, BlogSettings settings, List<BlogPost> listed)
	{
		if (settings.Feeds.Count == 0)
		{
			return [];
		}

		SiteConfig config = context.Config;
		if (string.IsNullOrWhiteSpace(config.Site.Url))
		{
			context.Diagnostics.Warning(
				"The blog has no feed: feed readers need absolute links, so set site.url in mokadocs.yaml", Name);
			return [];
		}

		List<BlogPost> published = listed.Where(p => !p.IsDraft).ToList();
		if (published.Count == 0)
		{
			logger.LogInformation("The blog has no published posts, so no feed was written");
			return [];
		}

		var info = new BlogFeedWriter.FeedInfo
		{
			Title = $"{settings.Title} - {config.Site.Title}",
			SiteTitle = config.Site.Title,
			Description = FirstNonEmpty(settings.Description, config.Site.Description, settings.Title),
			BlogUrl = SiteUrls.Absolute(config.Site.Url, config.Build.BasePath, settings.Route),
			SelfUrl = "",
			Config = config
		};

		var feeds = new List<BlogFeed>();
		foreach (string format in settings.Feeds)
		{
			string path = $"{settings.Route}/{format}.xml";
			string url = SiteUrls.AbsoluteFile(config.Site.Url, config.Build.BasePath, path);
			try
			{
				context.DeferredOutputFiles[path.TrimStart('/')] =
					BlogFeedWriter.Write(format, info with { SelfUrl = url }, published);
			}
			catch (ArgumentException ex)
			{
				// XmlWriter refuses characters XML can't hold, such as control characters in a title.
				context.Diagnostics.Warning($"The blog's {format} feed was not written: {ex.Message}", Name);
				continue;
			}

			feeds.Add(new BlogFeed
			{
				Format = format,
				Path = path,
				Url = url,
				MediaType = format == BlogSettings.AtomFormat ? "application/atom+xml" : "application/rss+xml",
				Title = settings.Feeds.Count > 1
					? $"{info.Title} ({(format == BlogSettings.AtomFormat ? "Atom" : "RSS")})"
					: info.Title
			});
		}

		return feeds;
	}

	private static string FirstNonEmpty(params string[] values) =>
		values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

	/// <summary>
	///     Gives each tag a unique slug and route. Slugs are handed out in tag name order, so they
	///     don't depend on which post was written first.
	/// </summary>
	private sealed class TagSlugs
	{
		private readonly Dictionary<string, BlogTag> _byName = new(StringComparer.OrdinalIgnoreCase);

		public TagSlugs(string blogRoute, IEnumerable<string> names)
		{
			var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string name in names
				         .Order(StringComparer.Ordinal)
				         .DistinctBy(n => n, StringComparer.OrdinalIgnoreCase)
				         .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
				         .ThenBy(n => n, StringComparer.Ordinal))
			{
				string baseSlug = BlogText.Slug(name);
				string slug = baseSlug;
				for (int n = 2; !slugs.Add(slug); n++)
				{
					slug = $"{baseSlug}-{n.ToString(CultureInfo.InvariantCulture)}";
				}

				_byName[name] = new BlogTag { Name = name, Slug = slug, Route = $"{blogRoute}/tags/{slug}" };
			}
		}

		public List<BlogTag> Resolve(IReadOnlyList<BlogTag> tags) => tags.Select(t => _byName[t.Name]).ToList();

		/// <summary>The tags of the listed posts with their post counts, in name order.</summary>
		public static List<BlogTag> Count(List<BlogPost> listed) =>
			listed
				.SelectMany(p => p.Tags)
				.GroupBy(t => t.Slug, StringComparer.Ordinal)
				.Select(g => g.First() with { Count = g.Count() })
				.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(t => t.Slug, StringComparer.Ordinal)
				.ToList();
	}
}
