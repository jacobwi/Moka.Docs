using System.Security.Cryptography;
using Microsoft.AspNetCore.StaticFiles;
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
	private static readonly FileExtensionContentTypeProvider _staticFileTypes = new();
	private readonly byte[] _content = Content;
	private readonly IReadOnlyList<SiteFileEncoding> _encodings = [];
	private readonly ContentTags _tags = ContentTags.Of(Content);

	/// <summary>The raw file content bytes.</summary>
	public byte[] Content
	{
		get => _content;
		init
		{
			// A `with` expression copies the tag and the compressed forms, so a new content needs
			// a new tag and none of the old forms.
			_content = value;
			_tags = ContentTags.Of(value);
			_encodings = [];
		}
	}

	/// <summary>
	///     A strong HTTP entity tag for the content, quotes included, computed once from a
	///     SHA-256 hash of <see cref="Content" />. An unchanged file keeps its tag across rebuilds.
	/// </summary>
	public string ETag => _tags.ETag;

	/// <summary>The lowercase hex SHA-256 hash of <see cref="Content" />.</summary>
	internal string ContentHash => _tags.Hash;

	/// <summary>
	///     Compressed forms of <see cref="Content" />, in the order the server prefers them
	///     (Brotli, then gzip). Empty for a file that isn't worth compressing.
	/// </summary>
	internal IReadOnlyList<SiteFileEncoding> Encodings
	{
		get => _encodings;
		init => _encodings = value;
	}

	/// <summary>
	///     Whether <paramref name="hex" /> is a prefix of <see cref="ContentHash" /> at least eight
	///     characters long, as in the <c>?v=</c> of theme URLs and content-addressed file names.
	/// </summary>
	internal bool MatchesContentHash(ReadOnlySpan<char> hex) =>
		hex.Length is >= 8 and <= 64
		&& hex.Equals(_tags.Hash.AsSpan(0, hex.Length), StringComparison.OrdinalIgnoreCase);

	/// <summary>A copy of this file with the given compressed forms, without hashing the content again.</summary>
	internal SiteFile WithEncodings(IReadOnlyList<SiteFileEncoding> encodings) => this with { Encodings = encodings };

	/// <summary>A strong entity tag for some bytes, quotes included.</summary>
	internal static string CreateETag(byte[] content) => ContentTags.Of(content).ETag;

	/// <summary>
	///     The content type for a file extension. Text types the build writes carry a UTF-8
	///     charset; everything else comes from ASP.NET Core's static file map, which knows
	///     <c>.pdf</c>, <c>.mp4</c>, <c>.avif</c> and the rest that went out as
	///     <c>application/octet-stream</c>.
	/// </summary>
	internal static string GetContentType(string extension)
	{
		return extension.ToLowerInvariant() switch
		{
			".html" => "text/html; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".js" => "application/javascript; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".svg" => "image/svg+xml",
			".xml" => "application/xml",
			".txt" => "text/plain; charset=utf-8",
			// The pages' Markdown copies (build.llms). Without a charset, browsers read them as Latin-1.
			".md" => "text/markdown; charset=utf-8",
			".yaml" or ".yml" => "application/yaml; charset=utf-8",
			".zip" => "application/zip",
			// ASP.NET Core's map has it from .NET 10 on.
			".avif" => "image/avif",
			// The Blazor preview host's runtime. Browsers only compile WebAssembly while it
			// downloads when the response says application/wasm.
			".wasm" => "application/wasm",
			_ => _staticFileTypes.TryGetContentType("x" + extension, out string? contentType)
				? contentType
				: "application/octet-stream"
		};
	}

	private readonly record struct ContentTags(string Hash, string ETag)
	{
		public static ContentTags Of(byte[] content)
		{
			string hash = Convert.ToHexStringLower(SHA256.HashData(content));
			return new ContentTags(hash, "\"" + hash[..32] + "\"");
		}
	}
}

/// <summary>A compressed form of a <see cref="SiteFile" />, served with <c>Content-Encoding</c>.</summary>
/// <param name="Name">The content coding: <c>br</c> or <c>gzip</c>.</param>
/// <param name="Content">The compressed bytes.</param>
internal sealed record SiteFileEncoding(string Name, byte[] Content)
{
	/// <summary>
	///     The entity tag of the compressed bytes. Each encoding is its own representation, so it
	///     needs its own tag, as Map Static Assets gives each one.
	/// </summary>
	public string ETag { get; } = SiteFile.CreateETag(Content);
}
