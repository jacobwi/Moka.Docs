using System.IO.Compression;
using System.Text;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     Reads a published preview host's <c>wwwroot</c> for staging under <c>_preview-wasm/</c>:
///     its top-level pages hardened by <see cref="PreviewPageHardening" /> and without links to a
///     scoped CSS bundle the publish didn't write, its other top-level files as they are, and its
///     folders as folders to copy.
/// </summary>
/// <remarks>
///     The pages go in as bytes and never as part of a copied folder, so no output path holds the
///     page as it was published: <c>OutputPhase</c> copies staged folders after it writes staged
///     files, and the ASP.NET Core host reads staged folders into the same file map. The host's
///     own folder is never written to, so a prebuilt host on a read-only disk works too.
/// </remarks>
internal static class PreviewHostStaging
{
	/// <summary>Where the preview host goes in the site.</summary>
	internal const string SiteFolder = "_preview-wasm";

	/// <summary>
	///     Site-root <c>.gitattributes</c> for branch deploys: Git must not rewrite the line
	///     endings of the framework scripts, or their integrity checks fail.
	/// </summary>
	internal const string GitAttributesPath = ".gitattributes";

	/// <summary>The <see cref="GitAttributesPath" /> file, from the Blazor GitHub Pages deployment notes.</summary>
	internal const string GitAttributes = "*.js binary\n";

	/// <summary>Reads <paramref name="wwwroot" />.</summary>
	/// <exception cref="IOException">A file could not be read.</exception>
	/// <exception cref="UnauthorizedAccessException">A file could not be read.</exception>
	internal static PreviewHostStage Read(string wwwroot)
	{
		var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		var pagesWithOwnPolicy = new List<string>();
		string[] topLevelFiles = Directory.GetFiles(wwwroot);
		var pages = topLevelFiles
			.Where(f => Path.GetExtension(f).Equals(".html", StringComparison.OrdinalIgnoreCase))
			.ToHashSet(StringComparer.Ordinal);
		var encoding = new UTF8Encoding(false);

		foreach (string file in topLevelFiles)
		{
			string sitePath = $"{SiteFolder}/{Path.GetFileName(file)}";
			if (pages.Contains(file))
			{
				string html = PreviewPageHardening.RemoveMissingScopedCss(File.ReadAllText(file),
					bundle => File.Exists(Path.Combine(wwwroot, bundle)));
				HardenedPage page = PreviewPageHardening.Harden(html);
				byte[] bytes = encoding.GetBytes(page.Html);
				files[sitePath] = bytes;
				if (page.HasOwnPolicy)
				{
					pagesWithOwnPolicy.Add(Path.GetFileName(file));
				}

				// Precompressed copies of the published page would serve it unhardened to any
				// server that negotiates encodings, so they are made again from the new bytes.
				if (File.Exists(file + ".br"))
				{
					files[sitePath + ".br"] = Compress(bytes, output => new BrotliStream(output, CompressionLevel.SmallestSize));
				}

				if (File.Exists(file + ".gz"))
				{
					files[sitePath + ".gz"] = Compress(bytes, output => new GZipStream(output, CompressionLevel.SmallestSize));
				}
			}
			else if (!IsCompressedPage(file, pages))
			{
				files[sitePath] = File.ReadAllBytes(file);
			}
		}

		List<(string SourceDir, string DestRelPath)> directories = Directory.GetDirectories(wwwroot)
			.Select(d => (d, $"{SiteFolder}/{Path.GetFileName(d)}"))
			.ToList();
		return new PreviewHostStage(files, directories, pagesWithOwnPolicy);
	}

	private static bool IsCompressedPage(string file, HashSet<string> pages) =>
		(file.EndsWith(".br", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
		&& pages.Contains(file[..^3]);

	private static byte[] Compress(byte[] bytes, Func<Stream, Stream> compressor)
	{
		using var output = new MemoryStream();
		using (Stream compressed = compressor(output))
		{
			compressed.Write(bytes);
		}

		return output.ToArray();
	}
}

/// <summary>What <see cref="PreviewHostStaging.Read" /> found.</summary>
/// <param name="Files">Site-relative path to bytes: the hardened pages and the other top-level files.</param>
/// <param name="Directories">The host's folders and where they go in the site.</param>
/// <param name="PagesWithOwnPolicy">Pages that carry a Content-Security-Policy of their own.</param>
internal sealed record PreviewHostStage(
	Dictionary<string, byte[]> Files,
	List<(string SourceDir, string DestRelPath)> Directories,
	List<string> PagesWithOwnPolicy);
