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
	private readonly Dictionary<string, string?> _firstPagesUnder = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, NavigationNode?> _sectionsByFolder = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, (PageLink? Previous, PageLink? Next)> _neighbors = new(StringComparer.Ordinal);
	private readonly NavigationTree? _navigation;
	private readonly List<string> _pagesInOrder;
	private Dictionary<string, NavigationNode[]>? _sidebarPaths;
	private IReadOnlyList<NavSection>? _topSections;

	public RenderIndex(IReadOnlyList<DocPage> pages, NavigationTree? navigation, BlogSite? blog = null)
	{
		_navigation = navigation;
		PageRoutes = new HashSet<string>(pages.Select(p => p.Route), StringComparer.OrdinalIgnoreCase);
		_pagesInOrder = pages
			.Where(p => p.FrontMatter.Visibility != PageVisibility.Draft)
			.OrderBy(p => p.FrontMatter.Visibility == PageVisibility.Public ? 0 : 1)
			.ThenBy(p => p.FrontMatter.Order)
			.ThenBy(p => p.Route, StringComparer.OrdinalIgnoreCase)
			.Select(p => p.Route)
			.ToList();

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

	// The top-level sidebar entries that lead somewhere, each with the entries directly under it, for
	// the header tabs and the footer columns.
	public IReadOnlyList<NavSection> TopSections => _topSections ??= BuildTopSections();

	// The sidebar entries from the top level down to the route's own entry, or none when the sidebar
	// doesn't list the route. A section whose route was resolved to its first page shares that route,
	// so the deepest entry wins within a branch; across branches, the first one in sidebar order.
	public IReadOnlyList<NavigationNode> SidebarPath(string route)
	{
		_sidebarPaths ??= BuildSidebarPaths();
		return _sidebarPaths.TryGetValue(route, out NavigationNode[]? path) ? path : [];
	}

	// Whether the entry's route is a page of its own, not a page below it that the sidebar resolved
	// the section's route to.
	public bool HasOwnPage(NavigationNode node) =>
		!string.IsNullOrEmpty(node.Route) && PageRoutes.Contains(node.Route)
		                                 && (node.Children.Count == 0
		                                     || !string.Equals(node.Children[0].Route, node.Route, StringComparison.OrdinalIgnoreCase));

	public string? FirstPageRoute(NavigationNode node) => PageRouteOf(node);

	// The sidebar section holding a route folder's pages, for a page the sidebar doesn't list: the
	// first entry with entries below it whose routes are all inside the folder, one of them directly.
	// A section of a subfolder (an API namespace below /api/lib) doesn't stand for the folder.
	public NavigationNode? SectionFor(string folderRoute)
	{
		string prefix = folderRoute.TrimEnd('/') + "/";
		if (!_sectionsByFolder.TryGetValue(prefix, out NavigationNode? section))
		{
			section = Find(_navigation?.Items ?? []);
			_sectionsByFolder[prefix] = section;
		}

		return section;

		NavigationNode? Find(List<NavigationNode> nodes)
		{
			foreach (NavigationNode node in nodes)
			{
				if (node.Children.Count > 0)
				{
					var routes = new List<string>();
					CollectRoutes([node], routes);
					if (routes.Count > 0
					    && routes.TrueForAll(r => r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					    && routes.Exists(r => r.Length > prefix.Length && r.IndexOf('/', prefix.Length) < 0))
					{
						return node;
					}
				}

				if (Find(node.Children) is { } below)
				{
					return below;
				}
			}

			return null;
		}
	}

	// The page a route folder without a page of its own leads to, as its redirect stub does: the first
	// below it in sidebar order, else the first by visibility, order and route.
	public string? FirstPageUnder(string route)
	{
		string prefix = route.TrimEnd('/') + "/";
		if (!_firstPagesUnder.TryGetValue(prefix, out string? first))
		{
			first = SidebarRoutes.FirstOrDefault(r =>
				        r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && PageRoutes.Contains(r))
			        ?? _pagesInOrder.FirstOrDefault(r => r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
			_firstPagesUnder[prefix] = first;
		}

		return first;
	}

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

	private Dictionary<string, NavigationNode[]> BuildSidebarPaths()
	{
		var paths = new Dictionary<string, NavigationNode[]>(StringComparer.OrdinalIgnoreCase);
		var path = new List<NavigationNode>();
		Walk(_navigation?.Items ?? []);
		return paths;

		void Walk(List<NavigationNode> nodes)
		{
			foreach (NavigationNode node in nodes)
			{
				path.Add(node);
				if (!string.IsNullOrEmpty(node.Route)
				    && (!paths.TryGetValue(node.Route, out NavigationNode[]? earlier)
				        || path.Exists(n => ReferenceEquals(n, earlier[^1]))))
				{
					paths[node.Route] = [.. path];
				}

				Walk(node.Children);
				path.RemoveAt(path.Count - 1);
			}
		}
	}

	private List<NavSection> BuildTopSections()
	{
		var sections = new List<NavSection>();
		foreach (NavigationNode node in _navigation?.Items ?? [])
		{
			if (PageRouteOf(node) is not { } route)
			{
				continue;
			}

			var links = new List<PageLink>();
			foreach (NavigationNode child in node.Children)
			{
				if (PageRouteOf(child) is { } childRoute)
				{
					links.Add(new PageLink(child.Label, childRoute));
				}
			}

			sections.Add(new NavSection(node, node.Label, route, links));
		}

		return sections;
	}

	// The node's own page, else the first page under it. A section route that isn't a page would
	// only redirect, and a group without any page has nowhere to go.
	private string? PageRouteOf(NavigationNode node)
	{
		if (!string.IsNullOrEmpty(node.Route) && PageRoutes.Contains(node.Route))
		{
			return node.Route;
		}

		foreach (NavigationNode child in node.Children)
		{
			if (PageRouteOf(child) is { } route)
			{
				return route;
			}
		}

		return null;
	}

	private static PageLink Link(DocPage page) => new(page.FrontMatter.Title, page.Route);

	private static PageLink? Link(BlogPostLink? post) => post is null ? null : new PageLink(post.Title, post.Route);
}

internal sealed record PageLink(string Title, string Route);

internal sealed record NavSection(NavigationNode Node, string Label, string Route, IReadOnlyList<PageLink> Links);
