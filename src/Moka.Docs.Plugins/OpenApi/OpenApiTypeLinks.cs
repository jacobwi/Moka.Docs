using Moka.Docs.Core.Api;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Finds the API reference page of the C# type behind an OpenAPI schema name.
/// </summary>
/// <remarks>
///     ASP.NET Core's generators name a schema after its C# type, so <c>Product</c> in the document is
///     usually the <c>Product</c> class in the API reference. A name links only when exactly one
///     documented type has it, since two <c>Product</c> types in different namespaces leave no way
///     to tell which one the schema describes.
/// </remarks>
internal static class OpenApiTypeLinks
{
	/// <summary>
	///     Maps schema names to the routes of their type pages. Empty when the build has no API model.
	/// </summary>
	/// <param name="context">
	///     The build, after the API pages were generated (C# analysis and the ASP.NET Core host's
	///     reflection phase both run before plugins).
	/// </param>
	internal static IReadOnlyDictionary<string, string> Build(BuildContext context)
	{
		if (context.ApiModel is null)
		{
			return new Dictionary<string, string>();
		}

		// A null route marks a name that more than one type has.
		var routes = new Dictionary<string, string?>(StringComparer.Ordinal);
		foreach (ApiNamespace ns in context.ApiModel.Namespaces)
		foreach (ApiType type in ns.Types)
		{
			routes[type.Name] = routes.ContainsKey(type.Name) ? null : TypeRoute(ns, type);
		}

		// Only routes a page was generated for: a link that 404s is worse than plain text.
		var pageRoutes = new HashSet<string>(context.Pages.Select(p => p.Route), StringComparer.Ordinal);
		var links = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach ((string name, string? route) in routes)
		{
			if (route is not null && pageRoutes.Contains(route))
			{
				links[name] = route;
			}
		}

		return links;
	}

	/// <summary>
	///     The route of a type page, as <c>CSharpAnalysisPhase</c> and the ASP.NET Core host's
	///     <c>ReflectionApiPagePhase</c> build it. The render phase adds the base path to it.
	/// </summary>
	private static string TypeRoute(ApiNamespace ns, ApiType type)
	{
		string name = type.Name.Replace('<', '-').Replace('>', '-').Replace('`', '-').TrimEnd('-');
		return $"/api/{ns.Name.Replace('.', '/')}/{name}".ToLowerInvariant();
	}
}
