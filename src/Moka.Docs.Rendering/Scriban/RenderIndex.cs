using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;

namespace Moka.Docs.Rendering.Scriban;

// Built once per render pass. Rebuilding the route set and re-sorting every page for prev/next on
// each page made rendering quadratic in the page count.
internal sealed class RenderIndex
{
	private static readonly HashSet<NavigationNode> _noNodes = new(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<string, HashSet<NavigationNode>> _ancestors = new(StringComparer.Ordinal);
	private readonly Dictionary<string, (PageLink? Previous, PageLink? Next)> _neighbors = new(StringComparer.Ordinal);

	public RenderIndex(IReadOnlyList<DocPage> pages, NavigationTree? navigation, BlogSite? blog = null)
	{
		PageRoutes = new HashSet<string>(pages.Select(p => p.Route), StringComparer.OrdinalIgnoreCase);

		var sidebarRoutes = new List<string>();
		if (navigation is not null)
		{
			CollectRoutes(navigation.Items, sidebarRoutes);
			CollectAncestors(navigation.Items, []);
		}

		SidebarRoutes = sidebarRoutes;

		// Blog pages stay out of the docs reading order: a post links to the next newer and older
		// posts instead, and a page that lists posts has its own pager.
		List<DocPage> ordered = ReadingOrder(
			blog is null ? pages : pages.Where(p => blog.FindPost(p.Route) is null && !blog.IsListing(p.Route)).ToList(),
			sidebarRoutes);
		for (int i = 0; i < ordered.Count; i++)
		{
			PageLink? previous = i > 0 ? Link(ordered[i - 1]) : null;
			PageLink? next = i < ordered.Count - 1 ? Link(ordered[i + 1]) : null;
			_neighbors.TryAdd(ordered[i].Route, (previous, next));
		}

		foreach (BlogPost post in blog?.Posts ?? [])
		{
			_neighbors.TryAdd(post.Route, (Link(post.Newer), Link(post.Older)));
		}
	}

	public HashSet<string> PageRoutes { get; }

	public IReadOnlyList<string> SidebarRoutes { get; }

	public (PageLink? Previous, PageLink? Next) Neighbors(string route) =>
		_neighbors.TryGetValue(route, out (PageLink? Previous, PageLink? Next) links) ? links : (null, null);

	// The sidebar nodes that hold an entry for the route somewhere below them. Walking every node's
	// subtree for each node of each page made the sidebar cost grow with its depth too.
	public IReadOnlySet<NavigationNode> AncestorsOf(string? route) =>
		route is not null && _ancestors.TryGetValue(route, out HashSet<NavigationNode>? nodes) ? nodes : _noNodes;

	// A section path the sidebar lists without a page of its own is only a redirect, so a route
	// that is a page wins over one earlier in the sidebar that isn't.
	public string FirstSidebarRouteOtherThan(string currentRoute)
	{
		string? fallback = null;
		foreach (string route in SidebarRoutes)
		{
			if (string.Equals(route, currentRoute, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (PageRoutes.Contains(route))
			{
				return route;
			}

			fallback ??= route;
		}

		return fallback ?? "";
	}

	// Prev/next follows the sidebar: pages in the order a depth-first walk of the navigation meets
	// them, then the pages it doesn't list, by order and route. Sorting every page by order and
	// route interleaved the sections, so Next jumped between unrelated pages.
	private static List<DocPage> ReadingOrder(IReadOnlyList<DocPage> pages, List<string> sidebarRoutes)
	{
		List<DocPage> eligible = pages
			.Where(p => p.FrontMatter.Visibility == PageVisibility.Public && p.FrontMatter.Layout == "default")
			.ToList();

		var byRoute = new Dictionary<string, DocPage>(StringComparer.OrdinalIgnoreCase);
		foreach (DocPage page in eligible)
		{
			byRoute.TryAdd(page.Route, page);
		}

		var ordered = new List<DocPage>(eligible.Count);
		var placed = new HashSet<DocPage>(ReferenceEqualityComparer.Instance);
		foreach (string route in sidebarRoutes)
		{
			if (byRoute.TryGetValue(route, out DocPage? page) && placed.Add(page))
			{
				ordered.Add(page);
			}
		}

		foreach (DocPage page in eligible
			         .OrderBy(p => p.FrontMatter.Order)
			         .ThenBy(p => p.Route, StringComparer.OrdinalIgnoreCase))
		{
			if (placed.Add(page))
			{
				ordered.Add(page);
			}
		}

		return ordered;
	}

	private static void CollectRoutes(List<NavigationNode> nodes, List<string> routes)
	{
		foreach (NavigationNode node in nodes)
		{
			if (!string.IsNullOrEmpty(node.Route))
			{
				routes.Add(node.Route);
			}

			CollectRoutes(node.Children, routes);
		}
	}

	private void CollectAncestors(List<NavigationNode> nodes, List<NavigationNode> path)
	{
		foreach (NavigationNode node in nodes)
		{
			if (!string.IsNullOrEmpty(node.Route) && path.Count > 0)
			{
				if (!_ancestors.TryGetValue(node.Route, out HashSet<NavigationNode>? ancestors))
				{
					ancestors = new HashSet<NavigationNode>(ReferenceEqualityComparer.Instance);
					_ancestors[node.Route] = ancestors;
				}

				ancestors.UnionWith(path);
			}

			if (node.Children.Count > 0)
			{
				path.Add(node);
				CollectAncestors(node.Children, path);
				path.RemoveAt(path.Count - 1);
			}
		}
	}

	private static PageLink Link(DocPage page) => new(page.FrontMatter.Title, page.Route);

	private static PageLink? Link(BlogPostLink? post) => post is null ? null : new PageLink(post.Title, post.Route);
}

internal sealed record PageLink(string Title, string Route);
