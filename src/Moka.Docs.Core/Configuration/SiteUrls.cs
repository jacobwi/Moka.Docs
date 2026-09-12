namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Builds absolute page URLs from <c>site.url</c>, <c>build.basePath</c> and a route.
/// </summary>
public static class SiteUrls
{
	/// <summary>
	///     The absolute URL of a page, or an empty string when <c>site.url</c> is not set.
	/// </summary>
	/// <remarks>
	///     Accepts <c>site.url</c> with or without the base path. A GitHub Pages project site
	///     can be configured as <c>url: https://user.github.io/Repo</c> or as
	///     <c>url: https://user.github.io</c> plus <c>basePath: /Repo</c>, and both produce
	///     <c>https://user.github.io/Repo/guide</c>. Canonical links used to append the
	///     base-path-prefixed route to a URL that already ended in the base path, giving
	///     <c>/Repo/Repo/guide</c>, while the sitemap left the base path out entirely.
	/// </remarks>
	/// <param name="siteUrl">The configured <c>site.url</c>.</param>
	/// <param name="basePath">The normalized base path, <c>/</c> for the site root.</param>
	/// <param name="route">The page route without the base path, such as <c>/guide/intro</c>.</param>
	/// <returns>The absolute URL.</returns>
	public static string Absolute(string siteUrl, string basePath, string route)
	{
		if (string.IsNullOrWhiteSpace(siteUrl))
		{
			return "";
		}

		string origin = siteUrl.Trim().TrimEnd('/');
		string prefix = string.IsNullOrEmpty(basePath) || basePath == "/" ? "" : "/" + basePath.Trim('/');

		if (prefix.Length > 0 && !origin.EndsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			origin += prefix;
		}

		string path = string.IsNullOrEmpty(route) ? "/" : route.StartsWith('/') ? route : "/" + route;
		return origin + path;
	}
}
