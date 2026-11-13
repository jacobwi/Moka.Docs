using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     The site's text (<see cref="ContentReferences.Text" />) for the renderers of a page: titles of blocks that
///     have none of their own and the names screen readers announce. The parser keeps it on the document, since
///     the renderers are shared by every page and only see the block they write.
/// </summary>
internal static class ContentText
{
	private static readonly object _key = typeof(ContentText);

	internal static void Attach(MarkdownDocument document, ThemeText text) => document.SetData(_key, text);

	internal static ThemeText Of(MarkdownObject node)
	{
		MarkdownObject? current = node;
		while (current is not null)
		{
			if (current is MarkdownDocument document)
			{
				return document.GetData(_key) as ThemeText ?? ThemeText.English;
			}

			current = current switch
			{
				Block block => block.Parent,
				ContainerInline { Parent: null } root => root.ParentBlock,
				Inline inline => inline.Parent,
				_ => null
			};
		}

		return ThemeText.English;
	}
}
