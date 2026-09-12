using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Turns links written relative to a Markdown file into links from the site root.
/// </summary>
/// <remarks>
///     A page is written to <c>{route}/index.html</c>, but served at <c>/guide/markdown</c>
///     until a host adds a trailing slash, so a browser resolves <c>./api-docs</c> against a
///     different folder depending on the host. GitHub Pages redirects to the slash form, and
///     the link landed one level too deep. Links to <c>.md</c> files were emitted as written
///     and 404ed everywhere. Resolving against the source file at build time gives the same
///     target on every host, and it is the target an editor or GitHub shows for the file.
/// </remarks>
public static class RelativeLinks
{
	/// <summary>
	///     Rewrites every relative link and image in a parsed document.
	/// </summary>
	/// <param name="document">The parsed Markdown.</param>
	/// <param name="sourcePath">The file's path relative to the docs folder.</param>
	public static void Rewrite(MarkdownDocument document, string sourcePath)
	{
		foreach (LinkInline link in document.Descendants<LinkInline>())
		{
			if (link.Url is { } url && ToRootRelative(url, sourcePath) is { } rewritten)
			{
				link.Url = rewritten;
			}
		}
	}

	/// <summary>
	///     The root-relative form of a link written in <paramref name="sourcePath" />, or
	///     <c>null</c> when the link is not relative: absolute paths, anchors, external URLs
	///     and links that climb above the docs folder are left alone.
	/// </summary>
	/// <param name="url">The link as written, such as <c>../guide/intro.md#setup</c>.</param>
	/// <param name="sourcePath">The linking file's path relative to the docs folder.</param>
	/// <returns>A link such as <c>/guide/intro#setup</c>, or <c>null</c>.</returns>
	public static string? ToRootRelative(string url, string sourcePath)
	{
		if (string.IsNullOrWhiteSpace(url) || url[0] is '/' or '#' or '?' || HasScheme(url))
		{
			return null;
		}

		int suffixStart = url.IndexOfAny(['?', '#']);
		string path = suffixStart >= 0 ? url[..suffixStart] : url;
		string suffix = suffixStart >= 0 ? url[suffixStart..] : "";

		var segments = sourcePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
		if (segments.Count > 0)
		{
			segments.RemoveAt(segments.Count - 1); // the file itself
		}

		foreach (string segment in path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			switch (segment)
			{
				case ".":
					break;
				case "..":
					if (segments.Count == 0)
					{
						return null;
					}

					segments.RemoveAt(segments.Count - 1);
					break;
				default:
					segments.Add(segment);
					break;
			}
		}

		// Page links take the route form: no .md extension, and index names the folder.
		if (segments.Count > 0 && segments[^1].EndsWith(".md", StringComparison.OrdinalIgnoreCase))
		{
			segments[^1] = segments[^1][..^3];
		}

		if (segments.Count > 0 && segments[^1].Equals("index", StringComparison.OrdinalIgnoreCase))
		{
			segments.RemoveAt(segments.Count - 1);
		}

		return "/" + string.Join('/', segments) + suffix;
	}

	// mailto:, https:, data: and the like. A colon after the first slash is part of a path.
	private static bool HasScheme(string url)
	{
		int colon = url.IndexOf(':');
		int slash = url.IndexOf('/');
		return colon > 0 && (slash < 0 || colon < slash);
	}
}
