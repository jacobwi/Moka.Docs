using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;

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
            items = BuildFromConfig(context.Config.Nav, context.Pages);
        else
            // Auto-generate from page routes
            items = BuildFromPages(context.Pages);

        context.Navigation = new NavigationTree { Items = items };
        logger.LogInformation("Built navigation tree with {Count} top-level items", items.Count);

        return Task.CompletedTask;
    }

    private static List<NavigationNode> BuildFromConfig(List<NavItem> navItems, List<DocPage> pages)
    {
        return navItems.Select(item =>
        {
            var children = item.Children.Count > 0
                ? BuildFromConfig(item.Children, pages)
                : BuildChildrenFromPath(item.Path ?? "", pages);

            // Check if the nav item's path points to an actual page
            var matchingPage = pages.FirstOrDefault(p =>
                string.Equals(p.Route, item.Path, StringComparison.OrdinalIgnoreCase));

            // If no page exists at this path, resolve to first child's route
            // so clicking doesn't hit a redirect page
            var resolvedRoute = item.Path;
            if (matchingPage is null && !string.IsNullOrEmpty(item.Path) && children.Count > 0)
                resolvedRoute = children[0].Route;

            return new NavigationNode
            {
                Label = item.Label,
                Route = resolvedRoute,
                Icon = item.Icon,
                Expanded = item.Expanded,
                Children = children
            };
        }).ToList();
    }

    private static List<NavigationNode> BuildChildrenFromPath(string parentPath, List<DocPage> pages)
    {
        if (string.IsNullOrEmpty(parentPath)) return [];

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

        foreach (var page in publicPages)
        {
            var segments = page.Route.Trim('/').Split('/', 2);
            var topLevel = segments.Length > 0 ? segments[0] : "";

            if (!groups.TryGetValue(topLevel, out var list))
            {
                list = [];
                groups[topLevel] = list;
            }

            list.Add(page);
        }

        return groups.Select(g =>
            {
                var rootPage = g.Value.FirstOrDefault(p =>
                    p.Route.Trim('/') == g.Key ||
                    p.Route.Trim('/') == g.Key + "/index");

                var label = rootPage?.FrontMatter.Title ?? FormatLabel(g.Key);

                return new NavigationNode
                {
                    Label = label,
                    Route = rootPage?.Route ?? "/" + g.Key,
                    Order = rootPage?.FrontMatter.Order ?? 0,
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

    private static string FormatLabel(string pathSegment)
    {
        if (string.IsNullOrEmpty(pathSegment)) return "Home";
        return char.ToUpper(pathSegment[0]) + pathSegment[1..].Replace('-', ' ');
    }
}