using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Gives the blog one sidebar entry: the entry whose path is the blog's route lists the latest
///     posts, newest first, instead of every post by title.
/// </summary>
/// <remarks>
///     With a <c>nav:</c> list in mokadocs.yaml, only an item whose <c>path</c> is the blog's route
///     changes, and only when it has no <c>children</c> of its own. Without one, the sidebar built
///     from the folders also loses posts and blog pages listed outside that entry, such as posts
///     whose folder differs from the blog's route.
/// </remarks>
public sealed class BlogNavigationPhase(ILogger<BlogNavigationPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "BlogNavigation";

	/// <inheritdoc />
	public int Order => 650;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (context.Blog is not { } blog || context.Navigation is not { } navigation)
		{
			return Task.CompletedTask;
		}

		BlogFeatureConfig config = context.Config.Features.Blog;
		int count = config.SidebarPosts >= 0 ? config.SidebarPosts : BlogFeatureConfig.DefaultSidebarPosts;
		List<NavigationNode> latest = blog.Posts
			.Take(count)
			.Select(p => new NavigationNode { Label = p.Title, Route = p.Route })
			.ToList();

		bool fromConfig = context.Config.Nav.Count > 0;
		bool keepChildren = fromConfig && HasExplicitChildren(context.Config.Nav, blog.Route);
		var pageRoutes = new HashSet<string>(context.Pages.Select(p => p.Route), StringComparer.OrdinalIgnoreCase);

		List<NavigationNode> Shape(List<NavigationNode> nodes)
		{
			var shaped = new List<NavigationNode>(nodes.Count);
			foreach (NavigationNode node in nodes)
			{
				if (IsBlogRoute(node.Route, blog))
				{
					shaped.Add(keepChildren ? node : node with { Children = latest });
					continue;
				}

				if (!fromConfig && node.Route is { } route && (blog.FindPost(route) is not null || blog.IsListing(route)))
				{
					continue;
				}

				List<NavigationNode> children = Shape(node.Children);
				if (!fromConfig && node.Children.Count > 0 && children.Count == 0
				    && (node.Route is null || !pageRoutes.Contains(node.Route)))
				{
					continue;
				}

				shaped.Add(node with { Children = children });
			}

			return shaped;
		}

		context.Navigation = navigation with { Items = Shape(navigation.Items) };
		logger.LogDebug("Listed {Count} latest posts under the blog's sidebar entry", latest.Count);
		return Task.CompletedTask;
	}

	private static bool IsBlogRoute(string? route, BlogSite blog) =>
		route is not null && string.Equals("/" + route.Trim().Trim('/'), blog.Route, StringComparison.OrdinalIgnoreCase);

	private static bool HasExplicitChildren(List<NavItem> items, string blogRoute)
	{
		foreach (NavItem item in items)
		{
			if (item.Path is { } path && string.Equals("/" + path.Trim().Trim('/'), blogRoute,
				    StringComparison.OrdinalIgnoreCase))
			{
				return item.Children.Count > 0;
			}

			if (HasExplicitChildren(item.Children, blogRoute))
			{
				return true;
			}
		}

		return false;
	}
}
