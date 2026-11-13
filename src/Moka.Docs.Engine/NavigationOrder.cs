using Moka.Docs.Core.Navigation;

namespace Moka.Docs.Engine;

/// <summary>
///     The sidebar's routes in the order a reader meets them: depth first, as the theme lists them.
/// </summary>
internal static class NavigationOrder
{
	public static List<string> Routes(NavigationTree? navigation)
	{
		var routes = new List<string>();
		Collect(navigation?.Items ?? [], routes);
		return routes;
	}

	private static void Collect(List<NavigationNode> nodes, List<string> routes)
	{
		foreach (NavigationNode node in nodes)
		{
			if (!string.IsNullOrEmpty(node.Route))
			{
				routes.Add(node.Route);
			}

			Collect(node.Children, routes);
		}
	}
}
