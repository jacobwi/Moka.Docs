using System.Buffers;
using System.IO.Compression;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Brotli and gzip forms of the site's files: made once per build, picked per request from
///     <c>Accept-Encoding</c>.
/// </summary>
/// <remarks>
///     Compressing HTTPS responses can expose secrets through BREACH when a response mixes a secret
///     with text an attacker controls. These files are fixed when the build ends and hold no
///     per-request data, so that attack has nothing to recover, the same reasoning that lets Map
///     Static Assets serve precompressed files over HTTPS.
/// </remarks>
internal static class SiteCompression
{
	/// <summary>Brotli's content coding.</summary>
	internal const string Brotli = "br";

	/// <summary>Gzip's content coding.</summary>
	internal const string Gzip = "gzip";

	/// <summary>
	///     Smallest file compressed. Below about 1 KB the headers cost more than compression saves,
	///     and the result can be larger than the file.
	/// </summary>
	internal const int MinimumLength = 1024;

	/// <summary>Content types compressed besides <c>text/*</c>. Images other than SVG, fonts and archives are compressed already.</summary>
	private static readonly HashSet<string> _compressibleTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"application/javascript", "application/json", "application/xml", "application/yaml",
		"application/manifest+json", "application/wasm", "image/svg+xml"
	};

	/// <summary>
	///     Whether a file is worth compressing: a text type, SVG or WebAssembly, of at least
	///     <see cref="MinimumLength" /> bytes, with no compressed forms yet.
	/// </summary>
	internal static bool ShouldCompress(SiteFile file)
	{
		if (file.Encodings.Count > 0 || file.Content.Length < MinimumLength)
		{
			return false;
		}

		string mediaType = file.ContentType.Split(';', 2)[0].Trim();
		return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || _compressibleTypes.Contains(mediaType);
	}

	/// <summary>
	///     Brotli and gzip forms of <paramref name="content" /> at the optimal level, leaving out
	///     any that isn't smaller than the content.
	/// </summary>
	internal static IReadOnlyList<SiteFileEncoding> Compress(byte[] content)
	{
		var encodings = new List<SiteFileEncoding>(2);
		foreach (string coding in (string[]) [Brotli, Gzip])
		{
			byte[] compressed = CompressWith(coding, content);
			if (compressed.Length < content.Length)
			{
				encodings.Add(new SiteFileEncoding(coding, compressed));
			}
		}

		return encodings;
	}

	/// <summary>
	///     Whether <paramref name="compressed" /> decompresses to exactly <paramref name="expected" />.
	///     A publish folder that isn't cleared keeps <c>.br</c> and <c>.gz</c> files from an older
	///     publish next to newer files of the same name, and serving those would break the app.
	/// </summary>
	internal static bool Decompresses(string coding, byte[] compressed, byte[] expected)
	{
		using var input = new MemoryStream(compressed, false);
		using Stream decoder = coding == Brotli
			? new BrotliStream(input, CompressionMode.Decompress)
			: new GZipStream(input, CompressionMode.Decompress);

		// Pooled, so checking a whole Blazor runtime doesn't allocate a large object per file.
		byte[] buffer = ArrayPool<byte>.Shared.Rent(81_920);
		try
		{
			int offset = 0;
			int read;
			while ((read = decoder.Read(buffer, 0, buffer.Length)) > 0)
			{
				if (read > expected.Length - offset
				    || !buffer.AsSpan(0, read).SequenceEqual(expected.AsSpan(offset, read)))
				{
					return false;
				}

				offset += read;
			}

			return offset == expected.Length;
		}
		catch (InvalidDataException)
		{
			return false;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	/// <summary>
	///     The compressed form to send for a request's <c>Accept-Encoding</c>, or <c>null</c> for
	///     the file as it is. The highest quality wins; on a tie Brotli goes before gzip. A coding the
	///     header gives <c>q=0</c>, directly or through <c>*;q=0</c>, is never sent.
	/// </summary>
	internal static SiteFileEncoding? Select(StringValues acceptEncoding, SiteFile file)
	{
		if (file.Encodings.Count == 0 || StringValues.IsNullOrEmpty(acceptEncoding)
		                              || !StringWithQualityHeaderValue.TryParseList(acceptEncoding,
			                              out IList<StringWithQualityHeaderValue>? accepted))
		{
			return null;
		}

		SiteFileEncoding? best = null;
		double bestQuality = 0;
		foreach (SiteFileEncoding encoding in file.Encodings)
		{
			double quality = QualityOf(accepted, encoding.Name) ?? QualityOf(accepted, "*") ?? 0;
			if (quality > bestQuality)
			{
				best = encoding;
				bestQuality = quality;
			}
		}

		// Only an identity entry that the client ranks above every coding keeps the file as it is.
		return QualityOf(accepted, "identity") is { } identity && identity > bestQuality ? null : best;
	}

	private static double? QualityOf(IList<StringWithQualityHeaderValue> accepted, string coding)
	{
		foreach (StringWithQualityHeaderValue value in accepted)
		{
			StringSegment name = value.Value;
			if (name.Equals(coding, StringComparison.OrdinalIgnoreCase)
			    || (coding == Gzip && name.Equals("x-gzip", StringComparison.OrdinalIgnoreCase)))
			{
				return value.Quality ?? 1;
			}
		}

		return null;
	}

	private static byte[] CompressWith(string coding, byte[] content)
	{
		using var output = new MemoryStream(content.Length / 2);
		using (Stream encoder = coding == Brotli
			       ? new BrotliStream(output, CompressionLevel.Optimal, true)
			       : new GZipStream(output, CompressionLevel.Optimal, true))
		{
			encoder.Write(content);
		}

		return output.ToArray();
	}
}
