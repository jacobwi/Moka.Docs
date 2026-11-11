using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Blog;

/// <summary>
///     The blog a build produced from <c>features.blog</c>: its posts, the pages that list them
///     and its feeds. Set on <c>BuildContext.Blog</c> by the blog phase; <c>null</c> when the blog
///     is off.
/// </summary>
public sealed class BlogSite
{
	private Dictionary<string, BlogPost>? _postsByRoute;
	private HashSet<string>? _listingRoutes;

	/// <summary>Route of the first index page, such as <c>/blog</c>.</summary>
	public required string Route { get; init; }

	/// <summary>The blog's title.</summary>
	public required string Title { get; init; }

	/// <summary>The blog's description, or empty.</summary>
	public string Description { get; init; } = "";

	/// <summary>
	///     The posts the index pages list, newest first: public posts, plus drafts when the build
	///     includes them. Hidden posts are built but not listed.
	/// </summary>
	public IReadOnlyList<BlogPost> Posts { get; init; } = [];

	/// <summary>Every tag used by a listed post, in name order.</summary>
	public IReadOnlyList<BlogTag> Tags { get; init; } = [];

	/// <summary>
	///     Routes of the generated pages that list posts: the index pages, the tags page and the
	///     tag pages. The first index page is <see cref="Route" />.
	/// </summary>
	public IReadOnlyList<string> ListingRoutes { get; init; } = [];

	/// <summary>The feeds the build writes. Empty without <c>site.url</c> or with no posts.</summary>
	public IReadOnlyList<BlogFeed> Feeds { get; init; } = [];

	/// <summary>The listed post with this route, or <c>null</c>.</summary>
	/// <param name="route">A page route without the base path.</param>
	/// <returns>The post, or <c>null</c>.</returns>
	public BlogPost? FindPost(string route)
	{
		_postsByRoute ??= Posts
			.GroupBy(p => p.Route, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
		return _postsByRoute.GetValueOrDefault(route);
	}

	/// <summary>Whether the route is one of the generated pages that list posts.</summary>
	/// <param name="route">A page route without the base path.</param>
	/// <returns><c>true</c> for an index page, the tags page or a tag page.</returns>
	public bool IsListing(string route)
	{
		_listingRoutes ??= new HashSet<string>(ListingRoutes, StringComparer.OrdinalIgnoreCase);
		return _listingRoutes.Contains(route);
	}
}

/// <summary>A blog post as the index pages, the post page and the feeds show it.</summary>
public sealed record BlogPost
{
	/// <summary>The post's route.</summary>
	public required string Route { get; init; }

	/// <summary>The post's title.</summary>
	public required string Title { get; init; }

	/// <summary>The post's file, relative to the docs folder.</summary>
	public string? SourcePath { get; init; }

	/// <summary>The publication date: front matter <c>date</c>, else the file's last commit or modified time.</summary>
	public required DateTimeOffset Date { get; init; }

	/// <summary>The authors, resolved against <c>features.blog.authors</c>.</summary>
	public IReadOnlyList<BlogAuthor> Authors { get; init; } = [];

	/// <summary>Front matter <c>summary</c>, else the first paragraph as plain text.</summary>
	public string Summary { get; init; } = "";

	/// <summary>The post's tags.</summary>
	public IReadOnlyList<BlogTag> Tags { get; init; } = [];

	/// <summary>The cover image URL, without the base path, or <c>null</c>.</summary>
	public string? Image { get; init; }

	/// <summary>Estimated reading time in whole minutes, at least 1.</summary>
	public int ReadingMinutes { get; init; } = 1;

	/// <summary>Whether the post is a draft, listed only because the build includes drafts.</summary>
	public bool IsDraft { get; init; }

	/// <summary>The next newer listed post, or <c>null</c> for the newest.</summary>
	public BlogPostLink? Newer { get; init; }

	/// <summary>The next older listed post, or <c>null</c> for the oldest.</summary>
	public BlogPostLink? Older { get; init; }
}

/// <summary>A link to another post.</summary>
/// <param name="Title">The post's title.</param>
/// <param name="Route">The post's route.</param>
public sealed record BlogPostLink(string Title, string Route);

/// <summary>A tag and the page that lists its posts.</summary>
public sealed record BlogTag
{
	/// <summary>The tag as first written.</summary>
	public required string Name { get; init; }

	/// <summary>The URL segment of the tag page, unique among the blog's tags.</summary>
	public required string Slug { get; init; }

	/// <summary>The tag page's route, such as <c>/blog/tags/release</c>.</summary>
	public required string Route { get; init; }

	/// <summary>The number of listed posts with this tag.</summary>
	public int Count { get; init; }
}

/// <summary>A feed the build writes.</summary>
public sealed record BlogFeed
{
	/// <summary><c>rss</c> or <c>atom</c>.</summary>
	public required string Format { get; init; }

	/// <summary>The file's path from the site root, such as <c>/blog/rss.xml</c>.</summary>
	public required string Path { get; init; }

	/// <summary>The feed's absolute URL.</summary>
	public required string Url { get; init; }

	/// <summary>The media type for <c>&lt;link rel="alternate"&gt;</c>.</summary>
	public required string MediaType { get; init; }

	/// <summary>The title readers see for the feed.</summary>
	public required string Title { get; init; }
}
