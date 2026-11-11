using System.Text;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     The readable text of Markdown inlines, as the table of contents, the search index and
///     link cards show it: link and emphasis text kept, entities decoded, markup dropped.
/// </summary>
internal static class InlineText
{
	/// <summary>The text of every inline in <paramref name="container" />.</summary>
	internal static string Of(ContainerInline? container)
	{
		if (container is null)
		{
			return "";
		}

		var text = new StringBuilder();
		Append(text, container);
		return text.ToString();
	}

	/// <summary>The text of one inline and its children.</summary>
	internal static string Of(Inline inline)
	{
		var text = new StringBuilder();
		Append(text, inline);
		return text.ToString();
	}

	/// <summary>Appends the text of <paramref name="inline" /> and its children.</summary>
	internal static void Append(StringBuilder text, Inline inline)
	{
		switch (inline)
		{
			case HtmlEntityInline entity:
				text.Append(entity.Transcoded.AsSpan());
				break;
			case LiteralInline literal:
				text.Append(literal.Content.AsSpan());
				break;
			case CodeInline code:
				text.Append(code.Content);
				break;
			case AutolinkInline autolink:
				text.Append(autolink.Url);
				break;
			case LineBreakInline:
				text.Append(' ');
				break;
			case ContainerInline container:
				foreach (Inline child in container)
				{
					Append(text, child);
				}

				break;
		}
	}
}
