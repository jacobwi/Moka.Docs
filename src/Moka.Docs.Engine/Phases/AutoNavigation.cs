using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     The sidebar tree built from page routes: every route folder is a section, titled by its index
///     page, and API type pages are grouped by namespace under <c>/api</c>. Used when there is no
///     <c>nav:</c> list, and below nav items with <c>autoGenerate</c>.
/// </summary>
/// <remarks>
///     Entries are sorted by front matter <c>order</c>, then label. The tree is cut to
///     <see cref="MaxDepth" /> levels: the pages below a third-level section are listed after it,
///     their labels prefixed with the folders they are in.
/// </remarks>
internal sealed class AutoNavigation
{
	/// <summary>
	///     The levels the default theme renders. It drops anything deeper without a word, so a page
	///     four folders down used to be missing from the sidebar.
	/// </summary>
	internal const int MaxDepth = 3;

	private const string _globalNamespace = "(global)";
	private const string _flattenedSeparator = " / ";

	private readonly HashSet<string> _pageRoutes = new(StringComparer.OrdinalIgnoreCase);
	private readonly Folder _root = new("/", "");

	private AutoNavigation(IEnumerable<DocPage> pages, ApiReference? apiModel)
	{
		Dictionary<string, string> namespaceOf = NamespacesByRoute(apiModel);
		var namespaces = new Dictionary<string, List<DocPage>>(StringComparer.Ordinal);

		foreach (DocPage page in pages)
		{
			if (page.FrontMatter.Visibility != PageVisibility.Public)
			{
				continue;
			}

			_pageRoutes.Add(page.Route);
			if (namespaceOf.TryGetValue(page.Route, out string? ns))
			{
				if (!namespaces.TryGetValue(ns, out List<DocPage>? types))
				{
					types = [];
					namespaces[ns] = types;
				}

				types.Add(page);
				continue;
			}

			Folder folder = _root;
			foreach (string segment in page.Route.Split('/', StringSplitOptions.RemoveEmptyEntries))
			{
				folder = folder.Child(segment);
			}

			folder.Page ??= page;
		}

		if (namespaces.Count == 0)
		{
			return;
		}

		// Type pages sit at /api/{namespace with slashes}/{type}, so as folders the namespace
		// Moka.Docs.Core became three nested sections.
		Folder api = _root.Child("api");
		foreach ((string ns, List<DocPage> types) in namespaces)
		{
			api.Namespaces.Add(new NavigationNode
			{
				Label = ns,
				Expanded = false,
				Children = Sort(types.Select(Leaf))
			});
		}
	}

	/// <summary>Builds the tree from the public pages.</summary>
	/// <param name="pages">Every page of the build.</param>
	/// <param name="apiModel">The API reference, whose type pages are grouped by namespace.</param>
	public static AutoNavigation Build(IEnumerable<DocPage> pages, ApiReference? apiModel) => new(pages, apiModel);

	/// <summary>The whole tree. The home page is a top-level entry like the sections.</summary>
	public List<NavigationNode> TopLevel()
	{
		List<NavigationNode> nodes = Children(_root);
		if (_root.Page is { } home)
		{
			nodes.Add(Leaf(home));
		}

		return LimitDepth(Sort(nodes), 1);
	}

	/// <summary>The entries below a route, for a nav item at <paramref name="depth" /> - 1.</summary>
	/// <param name="path">A root-relative route, such as <c>/guide</c> or <c>/</c>.</param>
	/// <param name="depth">The level the entries are shown at, 1 being the top.</param>
	public List<NavigationNode> Below(string path, int depth)
	{
		Folder? folder = _root;
		foreach (string segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			if (!folder.Subfolders.TryGetValue(segment, out folder))
			{
				return [];
			}
		}

		return LimitDepth(Children(folder), depth);
	}

	private static Dictionary<string, string> NamespacesByRoute(ApiReference? apiModel)
	{
		var namespaceOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (apiModel is null)
		{
			return namespaceOf;
		}

		var allTypes = apiModel.Namespaces.SelectMany(n => n.Types).ToList();
		ApiTypeRoutes routes = ApiTypeRoutes.For(allTypes);
		foreach (ApiNamespace ns in apiModel.Namespaces)
		{
			string label = string.IsNullOrEmpty(ns.Name) ? _globalNamespace : ns.Name;
			foreach (ApiType type in ns.Types)
			{
				namespaceOf.TryAdd(routes.Route(type), label);
			}
		}

		return namespaceOf;
	}

	private static List<NavigationNode> Children(Folder folder) =>
		Sort(folder.Subfolders.Values.Select(Section).Concat(folder.Namespaces));

	private static NavigationNode Section(Folder folder)
	{
		if (folder.Subfolders.Count == 0 && folder.Namespaces.Count == 0)
		{
			// A folder without subfolders exists only because a page has its route.
			return Leaf(folder.Page!);
		}

		DocPage? index = folder.Page;
		return new NavigationNode
		{
			Label = index?.FrontMatter.Title ?? FormatLabel(folder.Segment),
			Route = index?.Route ?? folder.Route,
			Icon = index?.FrontMatter.Icon,
			Order = index?.FrontMatter.Order ?? 0,
			Expanded = index?.FrontMatter.Expanded ?? true,
			Children = Children(folder)
		};
	}

	private static NavigationNode Leaf(DocPage page) => new()
	{
		Label = page.FrontMatter.Title,
		Route = page.Route,
		Icon = page.FrontMatter.Icon,
		Order = page.FrontMatter.Order
	};

	private static List<NavigationNode> Sort(IEnumerable<NavigationNode> nodes) =>
		nodes
			.OrderBy(n => n.Order)
			.ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase)
			.ThenBy(n => n.Route ?? "", StringComparer.Ordinal)
			.ToList();

	private List<NavigationNode> LimitDepth(List<NavigationNode> nodes, int depth)
	{
		if (depth < MaxDepth)
		{
			return nodes
				.Select(n => n.Children.Count == 0 ? n : n with { Children = LimitDepth(n.Children, depth + 1) })
				.ToList();
		}

		var flat = new List<NavigationNode>();
		foreach (NavigationNode node in nodes)
		{
			Flatten(node, "", flat);
		}

		return flat;
	}

	private void Flatten(NavigationNode node, string prefix, List<NavigationNode> into)
	{
		string label = prefix + node.Label;
		if (node.Children.Count == 0)
		{
			into.Add(node with { Label = label });
			return;
		}

		// A folder without an index page only has a redirect to its first page, which follows.
		if (node.Route is { } route && _pageRoutes.Contains(route))
		{
			into.Add(node with { Label = label, Children = [] });
		}

		foreach (NavigationNode child in node.Children)
		{
			Flatten(child, label + _flattenedSeparator, into);
		}
	}

	private static string FormatLabel(string segment) =>
		segment.Length == 0 ? "Home" : char.ToUpperInvariant(segment[0]) + segment[1..].Replace('-', ' ');

	private sealed class Folder(string route, string segment)
	{
		public string Route { get; } = route;

		public string Segment { get; } = segment;

		public DocPage? Page { get; set; }

		public Dictionary<string, Folder> Subfolders { get; } = new(StringComparer.OrdinalIgnoreCase);

		public List<NavigationNode> Namespaces { get; } = [];

		public Folder Child(string name)
		{
			if (!Subfolders.TryGetValue(name, out Folder? child))
			{
				child = new Folder(Route == "/" ? "/" + name : Route + "/" + name, name);
				Subfolders[name] = child;
			}

			return child;
		}
	}
}
