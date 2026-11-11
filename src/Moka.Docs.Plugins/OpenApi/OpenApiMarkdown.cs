using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Renders the CommonMark of OpenAPI <c>description</c> fields.
/// </summary>
/// <remarks>
///     A document can come from anywhere, so raw HTML is shown as text and a link whose scheme isn't
///     http, https or mailto keeps its text and loses the link. Heading ids and footnotes are left
///     out: several descriptions share one page, and their ids would collide with each other and with
///     the endpoint anchors.
/// </remarks>
internal static class OpenApiMarkdown
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.UsePipeTables()
		.UseEmphasisExtras()
		.UseAutoLinks()
		.DisableHtml()
		.Build();

	private static readonly string[] _safeSchemes = ["http", "https", "mailto"];

	/// <summary>The description as HTML blocks, or an empty string when it is blank.</summary>
	internal static string Block(string? markdown) => Render(markdown, false);

	/// <summary>
	///     The description as HTML for a table cell or a line of text: a single paragraph without its
	///     <c>&lt;p&gt;</c>, anything longer as blocks.
	/// </summary>
	internal static string Inline(string? markdown) => Render(markdown, true);

	/// <summary>The description's text without markup, on one line, for search and meta descriptions.</summary>
	internal static string PlainText(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return "";
		}

		string text = Markdown.ToPlainText(markdown, _pipeline);
		return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static string Render(string? markdown, bool unwrapParagraph)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return "";
		}

		MarkdownDocument document = Markdown.Parse(markdown, _pipeline);
		RemoveUnsafeLinks(document);

		using var writer = new StringWriter();
		var renderer = new HtmlRenderer(writer)
		{
			ImplicitParagraph = unwrapParagraph && document.Count == 1 && document[0] is ParagraphBlock
		};
		_pipeline.Setup(renderer);
		renderer.Render(document);
		writer.Flush();
		return writer.ToString().Trim();
	}

	private static void RemoveUnsafeLinks(MarkdownDocument document)
	{
		foreach (LinkInline link in document.Descendants<LinkInline>().Where(l => !IsSafeUrl(l.Url)).ToList())
		{
			Inline? child = link.FirstChild;
			while (child is not null)
			{
				Inline? next = child.NextSibling;
				child.Remove();
				link.InsertBefore(child);
				child = next;
			}

			link.Remove();
		}

		foreach (AutolinkInline autolink in document.Descendants<AutolinkInline>().Where(l => !l.IsEmail && !IsSafeUrl(l.Url)).ToList())
		{
			autolink.ReplaceBy(new LiteralInline(autolink.Url));
		}
	}

	/// <summary>
	///     Whether a URL is relative or uses http, https or mailto.
	/// </summary>
	internal static bool IsSafeUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return true;
		}

		// Browsers drop tabs, newlines and leading spaces from a URL, so "java\tscript:" still runs.
		string compact = string.Concat(url.Where(c => c > ' '));
		int colon = compact.IndexOf(':');
		if (colon < 0)
		{
			return true;
		}

		int pathStart = compact.IndexOfAny(['/', '?', '#']);
		if (pathStart >= 0 && pathStart < colon)
		{
			return true;
		}

		string scheme = compact[..colon];
		return _safeSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
	}
}
