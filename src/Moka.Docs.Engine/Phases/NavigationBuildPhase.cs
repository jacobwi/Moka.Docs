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
		List<NavigationNode> items;
		AutoNavigation? auto = null;
		AutoNavigation Auto() => auto ??= AutoNavigation.Build(context.Pages, context.ApiModel);

		// If explicit nav config exists, use it as the skeleton
		if (context.Config.Nav.Count > 0)
		{
			var deadLinks = new List<string>();
			items = BuildFromConfig(context.Config.Nav, context.Pages, Auto, 1, deadLinks);
			foreach (string deadLink in deadLinks)
			{
				context.Diagnostics.Warning(deadLink, Name);
			}
		}
		else
		{
			items = Auto().TopLevel();
		}

		context.Navigation = new NavigationTree { Items = items };
		ReportUnknownIcons(context, items);
		logger.LogInformation("Built navigation tree with {Count} top-level items", items.Count);

		return Task.CompletedTask;
	}

	private static List<NavigationNode> BuildFromConfig(List<NavItem> navItems, List<DocPage> pages,
		Func<AutoNavigation> auto, int depth, List<string> deadLinks)
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
				? BuildFromConfig(item.Children, pages, auto, depth + 1, deadLinks)
				: item.AutoGenerate && !string.IsNullOrEmpty(normalizedPath)
					? auto().Below(normalizedPath, depth + 1)
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

			// A section folder with pages deeper down still gets a redirect page, so only a path
			// with nothing at or below it is a dead link.
			if (matchingPage is null && children.Count == 0 && !string.IsNullOrEmpty(normalizedPath)
			    && (normalizedPath == "/"
			        || !pages.Any(p => p.Route.StartsWith(normalizedPath + "/", StringComparison.OrdinalIgnoreCase))))
			{
				deadLinks.Add(
					$"Nav item '{item.Label}' points to '{item.Path}', which matches no page, so its sidebar link is dead");
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
}
