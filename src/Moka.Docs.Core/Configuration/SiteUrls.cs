namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Builds absolute page URLs from <c>site.url</c>, <c>build.basePath</c> and a route.
/// </summary>
public static class SiteUrls
{
	/// <summary>
	///     The absolute URL of a page, or an empty string when <c>site.url</c> is not set. Each
	///     path segment is percent-encoded, and the URL ends with <c>/</c> because every page is
	///     written as <c>{route}/index.html</c>.
	/// </summary>
	/// <remarks>
	///     Accepts <c>site.url</c> with or without the base path. A GitHub Pages project site
	///     can be configured as <c>url: https://user.github.io/Repo</c> or as
	///     <c>url: https://user.github.io</c> plus <c>basePath: /Repo</c>, and both produce
	///     <c>https://user.github.io/Repo/guide/</c>. Canonical links used to append the
	///     base-path-prefixed route to a URL that already ended in the base path, giving
	///     <c>/Repo/Repo/guide</c>, while the sitemap left the base path out entirely.
	///     <para>
	///         Without the trailing slash, hosts such as GitHub Pages answer the canonical URL
	///         with a redirect. Without the encoding, a page such as <c>Q&amp;A.md</c> put a raw
	///         <c>&amp;</c> into the sitemap, which made the whole file invalid XML.
	///     </para>
	/// </remarks>
	/// <param name="siteUrl">The configured <c>site.url</c>.</param>
	/// <param name="basePath">The normalized base path, <c>/</c> for the site root.</param>
	/// <param name="route">The page route without the base path, such as <c>/guide/intro</c>.</param>
	/// <returns>The absolute URL.</returns>
	public static string Absolute(string siteUrl, string basePath, string route)
	{
		string path = EncodePath(route);
		return Combine(siteUrl, basePath, path.Length == 0 ? "/" : "/" + path + "/");
	}

	/// <summary>
	///     The absolute URL of a file the build writes, such as <c>/sitemap.xml</c>, or an empty
	///     string when <c>site.url</c> is not set. Like <see cref="Absolute" />, but without the
	///     trailing slash.
	/// </summary>
	/// <param name="siteUrl">The configured <c>site.url</c>.</param>
	/// <param name="basePath">The normalized base path, <c>/</c> for the site root.</param>
	/// <param name="path">The file's path from the site root, such as <c>/sitemap.xml</c>.</param>
	/// <returns>The absolute URL.</returns>
	public static string AbsoluteFile(string siteUrl, string basePath, string path) =>
		Combine(siteUrl, basePath, "/" + EncodePath(path));

	private static string Combine(string siteUrl, string basePath, string path)
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

		return origin + path;
	}

	private static string EncodePath(string route) =>
		string.Join('/', (route ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
}
