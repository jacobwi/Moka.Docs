namespace Moka.Docs.AspNetCore;

/// <summary>
///     Represents a fully built documentation site held in memory.
///     Files are keyed by their relative URL path (e.g., "index.html", "api/index.html").
/// </summary>
public sealed class InMemorySite
{
	/// <summary>All files in the site, keyed by relative path.</summary>
	public Dictionary<string, SiteFile> Files { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
///     A single file in the in-memory site.
/// </summary>
/// <param name="Content">The raw file content bytes.</param>
/// <param name="ContentType">The MIME content type (e.g., "text/html").</param>
public sealed record SiteFile(byte[] Content, string ContentType)
{
	/// <summary>Content type mappings for common file extensions.</summary>
	internal static string GetContentType(string extension)
	{
		return extension.ToLowerInvariant() switch
		{
			".html" => "text/html; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".js" => "application/javascript; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".svg" => "image/svg+xml",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".ico" => "image/x-icon",
			".woff" => "font/woff",
			".woff2" => "font/woff2",
			".ttf" => "font/ttf",
			".eot" => "application/vnd.ms-fontobject",
			".xml" => "application/xml",
			".txt" => "text/plain; charset=utf-8",
			".webp" => "image/webp",
			_ => "application/octet-stream"
		};
	}
}
