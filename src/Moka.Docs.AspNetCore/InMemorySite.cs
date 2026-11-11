using System.Security.Cryptography;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Represents a fully built documentation site held in memory.
///     Files are keyed by their relative URL path (e.g., "index.html", "api/index.html").
/// </summary>
public sealed class InMemorySite
{
	/// <summary>All files in the site, keyed by relative path.</summary>
	public Dictionary<string, SiteFile> Files { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     Warnings and errors the build reported, such as a plugin that failed or two pages
	///     sharing a route. The CLI prints these; the in-memory build also logs them.
	/// </summary>
	public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
///     A single file in the in-memory site.
/// </summary>
/// <param name="Content">The raw file content bytes.</param>
/// <param name="ContentType">The MIME content type (e.g., "text/html").</param>
public sealed record SiteFile(byte[] Content, string ContentType)
{
	private readonly byte[] _content = Content;
	private readonly string _etag = CreateETag(Content);

	/// <summary>The raw file content bytes.</summary>
	public byte[] Content
	{
		get => _content;
		init
		{
			// A `with` expression copies the tag, so a new content needs a new one.
			_content = value;
			_etag = CreateETag(value);
		}
	}

	/// <summary>
	///     A strong HTTP entity tag for the content, quotes included, computed once from a
	///     SHA-256 hash of <see cref="Content" />. An unchanged file keeps its tag across rebuilds.
	/// </summary>
	public string ETag => _etag;

	private static string CreateETag(byte[] content) =>
		"\"" + Convert.ToHexStringLower(SHA256.HashData(content).AsSpan(0, 16)) + "\"";

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
			".yaml" or ".yml" => "application/yaml; charset=utf-8",
			".webp" => "image/webp",
			// The Blazor preview host's runtime. Browsers only compile WebAssembly while it
			// downloads when the response says application/wasm.
			".wasm" => "application/wasm",
			_ => "application/octet-stream"
		};
	}
}
