using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Generates the sidebar navigation tree from the folder structure, front matter, and config overrides.
/// </summary>
public sealed class NavigationBuildPhase(ILogger<NavigationBuildPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "NavigationBuild";

	/// <inheritdoc />
	public int Order => 600;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		var items = new List<NavigationNode>();

		// If explicit nav config exists, use it as the skeleton
		if (context.Config.Nav.Count > 0)
		{
			items = BuildFromConfig(context.Config.Nav, context.Pages);
		}
		else
			// Auto-generate from page routes
		{
			items = BuildFromPages(context.Pages);
		}

		context.Navigation = new NavigationTree { Items = items };
		ReportUnknownIcons(context, items);
		logger.LogInformation("Built navigation tree with {Count} top-level items", items.Count);

		return Task.CompletedTask;
	}

	private static List<NavigationNode> BuildFromConfig(List<NavItem> navItems, List<DocPage> pages)
	{
		return navItems.Select(item =>
		{
			// Normalize path to root-relative (leading "/") so sidebar links always
			// resolve from the site root regardless of what page the user is currently
			// viewing. Without this, a yaml `path: mpc-fopdt` rendered as a relative
			// href while viewing /algo-api/base/someclass would navigate to
			// /algo-api/base/mpc-fopdt instead of /mpc-fopdt.
			string? normalizedPath = NormalizePath(item.Path);

			List<NavigationNode> children = item.Children.Count > 0
				? BuildFromConfig(item.Children, pages)
				: BuildChildrenFromPath(normalizedPath ?? "", pages);

			// Check if the nav item's path points to an actual page.
			// Match against both the normalized path and the raw path so that users
			// who write `path: /mpc-fopdt` and `path: mpc-fopdt` both resolve correctly.
			DocPage? matchingPage = pages.FirstOrDefault(p =>
				string.Equals(p.Route, normalizedPath, StringComparison.OrdinalIgnoreCase));

			// If no page exists at this path, resolve to first child's route
			// so clicking doesn't hit a redirect page
			string? resolvedRoute = normalizedPath;
			if (matchingPage is null && !string.IsNullOrEmpty(normalizedPath) && children.Count > 0)
			{
				resolvedRoute = children[0].Route;
			}

			return new NavigationNode
			{
				Label = item.Label,
				Route = resolvedRoute,
				Icon = item.Icon,
				Order = item.Order,
				Expanded = item.Expanded,
				Children = children
			};
		})
		// OrderBy is stable, so items with the same order keep their mokadocs.yaml order.
		// Sorting ties by label threw that away: every nav config without explicit order
		// values came out alphabetical, whatever order the author wrote it in.
		.OrderBy(n => n.Order)
		.ToList();
	}

	/// <summary>
	///     Ensures a nav path starts with "/" so sidebar links are always root-relative.
	///     Yaml authors commonly write both <c>path: mpc-fopdt</c> (no slash) and
	///     <c>path: /mpc-fopdt</c> (with slash) - both should produce the same href.
	/// </summary>
	private static string? NormalizePath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		// "/guide/" matched no page route, so the item got no children and no page.
		string trimmed = path.Trim().Trim('/');
		return "/" + trimmed;
	}

	private static List<NavigationNode> BuildChildrenFromPath(string parentPath, List<DocPage> pages)
	{
		if (string.IsNullOrEmpty(parentPath))
		{
			return [];
		}

		return pages
			.Where(p => p.Route.StartsWith(parentPath + "/", StringComparison.OrdinalIgnoreCase)
			            && p.Route[parentPath.Length..].Count(c => c == '/') == 1
			            && p.FrontMatter.Visibility == PageVisibility.Public)
			.OrderBy(p => p.FrontMatter.Order)
			.ThenBy(p => p.FrontMatter.Title, StringComparer.OrdinalIgnoreCase)
			.Select(p => new NavigationNode
			{
				Label = p.FrontMatter.Title,
				Route = p.Route,
				Icon = p.FrontMatter.Icon,
				Order = p.FrontMatter.Order,
				Expanded = p.FrontMatter.Expanded,
				Children = BuildChildrenFromPath(p.Route, pages)
			})
			.ToList();
	}

	private static List<NavigationNode> BuildFromPages(List<DocPage> pages)
	{
		var publicPages = pages
			.Where(p => p.FrontMatter.Visibility == PageVisibility.Public)
			.OrderBy(p => p.FrontMatter.Order)
			.ThenBy(p => p.FrontMatter.Title, StringComparer.OrdinalIgnoreCase)
			.ToList();

		// Group by top-level path segment
		var groups = new Dictionary<string, List<DocPage>>(StringComparer.OrdinalIgnoreCase);

		foreach (DocPage page in publicPages)
		{
			string[] segments = page.Route.Trim('/').Split('/', 2);
			string topLevel = segments.Length > 0 ? segments[0] : "";

			if (!groups.TryGetValue(topLevel, out List<DocPage>? list))
			{
				list = [];
				groups[topLevel] = list;
			}

			list.Add(page);
		}

		return groups.Select(g =>
			{
				DocPage? rootPage = g.Value.FirstOrDefault(p =>
					p.Route.Trim('/') == g.Key ||
					p.Route.Trim('/') == g.Key + "/index");

				string label = rootPage?.FrontMatter.Title ?? FormatLabel(g.Key);

				return new NavigationNode
				{
					Label = label,
					Route = rootPage?.Route ?? "/" + g.Key,
					Order = rootPage?.FrontMatter.Order ?? 0,
					// A section's index page supplies its icon and whether it starts collapsed.
					// The icon was never copied, so top-level entries had no icons.
					Icon = rootPage?.FrontMatter.Icon,
					Expanded = rootPage?.FrontMatter.Expanded ?? true,
					Children = g.Value
						.Where(p => p != rootPage)
						.Select(p => new NavigationNode
						{
							Label = p.FrontMatter.Title,
							Route = p.Route,
							Icon = p.FrontMatter.Icon,
							Order = p.FrontMatter.Order
						})
						.OrderBy(n => n.Order)
						.ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase)
						.ToList()
				};
			})
			.OrderBy(n => n.Order)
			.ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	///     Warns about sidebar icons that are not in the icon set. The theme renders nothing
	///     for them, so a typo (or a Lucide name MokaDocs does not ship) silently lost the icon.
	/// </summary>
	private void ReportUnknownIcons(BuildContext context, IEnumerable<NavigationNode> items)
	{
		var unknown = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Collect(items);

		foreach ((string icon, List<string> labels) in unknown)
		{
			context.Diagnostics.Warning(
				$"Icon '{icon}' is not in the icon set and renders nothing (used by {string.Join(", ", labels.Select(l => $"'{l}'"))})",
				Name);
		}

		void Collect(IEnumerable<NavigationNode> nodes)
		{
			foreach (NavigationNode node in nodes)
			{
				if (!string.IsNullOrWhiteSpace(node.Icon) && LucideIcons.Get(node.Icon) is null)
				{
					if (!unknown.TryGetValue(node.Icon, out List<string>? labels))
					{
						labels = [];
						unknown[node.Icon] = labels;
					}

					labels.Add(node.Label);
				}

				Collect(node.Children);
			}
		}
	}

	private static string FormatLabel(string pathSegment)
	{
		if (string.IsNullOrEmpty(pathSegment))
		{
			return "Home";
		}

		return char.ToUpper(pathSegment[0]) + pathSegment[1..].Replace('-', ' ');
	}
}
