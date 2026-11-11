using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.Engine.Blog;

/// <summary>
///     <c>features.blog</c> checked and normalized: a folder path without slashes at either end, a
///     route with a leading slash and none at the end, and known feed formats only.
/// </summary>
internal sealed record BlogSettings
{
	public const string RssFormat = "rss";
	public const string AtomFormat = "atom";

	public required string Path { get; init; }
	public required string Route { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required int PostsPerPage { get; init; }
	public required int SidebarPosts { get; init; }
	public required IReadOnlyList<string> Feeds { get; init; }
	public required BlogFeatureConfig Config { get; init; }

	/// <summary>
	///     Reads the settings, warning about each value that can't be used and falling back to its
	///     default.
	/// </summary>
	public static BlogSettings Resolve(BlogFeatureConfig config, DiagnosticBag diagnostics, string source)
	{
		string path = NormalizeFolder(config.Path);
		if (path.Length == 0 || path.Split('/').Any(s => s is "." or ".."))
		{
			diagnostics.Warning(
				$"features.blog.path '{config.Path}' must name a folder inside the docs folder; using '{BlogFeatureConfig.DefaultPath}'",
				source);
			path = BlogFeatureConfig.DefaultPath;
		}

		string defaultRoute = "/" + path;
		string route = config.Route is null ? defaultRoute : "/" + config.Route.Trim().Trim('/');
		if (RouteProblem(route) is { } problem)
		{
			diagnostics.Warning($"features.blog.route '{config.Route}' can't be used ({problem}); using '{defaultRoute}'",
				source);
			route = defaultRoute;
		}

		int postsPerPage = config.PostsPerPage;
		if (postsPerPage < 1)
		{
			diagnostics.Warning(
				$"features.blog.postsPerPage must be 1 or more, not {postsPerPage}; using {BlogFeatureConfig.DefaultPostsPerPage}",
				source);
			postsPerPage = BlogFeatureConfig.DefaultPostsPerPage;
		}

		int sidebarPosts = config.SidebarPosts;
		if (sidebarPosts < 0)
		{
			diagnostics.Warning(
				$"features.blog.sidebarPosts must be 0 or more, not {sidebarPosts}; using {BlogFeatureConfig.DefaultSidebarPosts}",
				source);
			sidebarPosts = BlogFeatureConfig.DefaultSidebarPosts;
		}

		var feeds = new List<string>();
		foreach (string format in config.Feeds)
		{
			if (format is RssFormat or AtomFormat)
			{
				feeds.Add(format);
			}
			else
			{
				diagnostics.Warning(
					$"features.blog.feed '{format}' is not a feed format; use rss, atom or both", source);
			}
		}

		return new BlogSettings
		{
			Path = path,
			Route = route,
			Title = string.IsNullOrWhiteSpace(config.Title) ? "Blog" : config.Title.Trim(),
			Description = config.Description.Trim(),
			PostsPerPage = postsPerPage,
			SidebarPosts = sidebarPosts,
			Feeds = feeds,
			Config = config
		};
	}

	private static string NormalizeFolder(string path)
	{
		string normalized = path.Trim().Replace('\\', '/');
		while (normalized.StartsWith("./", StringComparison.Ordinal))
		{
			normalized = normalized[2..];
		}

		return normalized.Trim('/');
	}

	// The blog's own pages sit below the route, so the site root would put them next to the
	// home page and the docs sections.
	private static string? RouteProblem(string route)
	{
		if (route == "/")
		{
			return "the blog needs a route of its own below the site root";
		}

		return OutputPaths.FindUnsafeRouteProblem(route) ?? OutputPaths.FindNonPortableRouteProblem(route);
	}
}
