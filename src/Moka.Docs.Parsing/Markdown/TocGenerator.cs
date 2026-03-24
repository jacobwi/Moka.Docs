using System.Text;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Generates a <see cref="TableOfContents" /> from the heading blocks in a parsed Markdown document.
/// </summary>
public sealed class TocGenerator
{
    /// <summary>
    ///     Generates a table of contents from a Markdig document.
    /// </summary>
    /// <param name="document">The parsed Markdig document.</param>
    /// <returns>A <see cref="TableOfContents" /> with nested entries.</returns>
    public TableOfContents Generate(MarkdownDocument document)
    {
        var flatEntries = new List<TocEntry>();

        foreach (var block in document.DescendantsOfType<HeadingBlock>())
        {
            var text = ExtractHeadingText(block);
            var attributes = block.TryGetAttributes();
            var id = attributes?.Id ?? GenerateAnchorId(text);

            flatEntries.Add(new TocEntry
            {
                Level = block.Level,
                Text = text,
                Id = id
            });
        }

        if (flatEntries.Count == 0) return TableOfContents.Empty;

        var nested = BuildNestedEntries(flatEntries);
        return new TableOfContents { Entries = nested };
    }

    #region Private Helpers

    private static string ExtractHeadingText(HeadingBlock heading)
    {
        if (heading.Inline is null) return "";

        var text = new StringBuilder();
        foreach (var inline in heading.Inline)
            if (inline is LiteralInline literal)
                text.Append(literal.Content);
            else if (inline is CodeInline code)
                text.Append(code.Content);
            else if (inline is EmphasisInline emphasis)
                foreach (var child in emphasis)
                    if (child is LiteralInline lit)
                        text.Append(lit.Content);

        return text.ToString();
    }

    /// <summary>
    ///     Generates a URL-safe anchor ID from heading text.
    ///     Follows GitHub-style anchor generation.
    /// </summary>
    /// <summary>
    ///     Generates a URL-safe anchor ID from heading text.
    ///     Follows GitHub-style anchor generation.
    /// </summary>
    public static string GenerateAnchorId(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (var ch in text)
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
            else if (ch is ' ' or '-') sb.Append('-');

        // Other characters are stripped
        // Collapse multiple dashes
        var result = sb.ToString();
        while (result.Contains("--")) result = result.Replace("--", "-");

        return result.Trim('-');
    }

    private static List<TocEntry> BuildNestedEntries(List<TocEntry> flat)
    {
        var root = new List<TocEntry>();
        var stack = new Stack<TocEntry>();

        foreach (var entry in flat)
        {
            // Pop entries from stack that are at the same level or deeper
            while (stack.Count > 0 && stack.Peek().Level >= entry.Level) stack.Pop();

            if (stack.Count == 0)
                root.Add(entry);
            else
                stack.Peek().Children.Add(entry);

            stack.Push(entry);
        }

        return root;
    }

    #endregion
}