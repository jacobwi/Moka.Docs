using Markdig.Helpers;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Extension methods and helpers for Markdig types.
/// </summary>
internal static class MarkdigHelpers
{
    /// <summary>
    ///     Counts consecutive occurrences of a character at the current position and advances past them.
    /// </summary>
    internal static int CountAndSkipChar(ref StringSlice slice, char c)
    {
        var count = 0;
        while (slice.CurrentChar == c)
        {
            count++;
            slice.SkipChar();
        }

        return count;
    }

    /// <summary>
    ///     Returns all descendant blocks from a Markdown document.
    /// </summary>
    internal static IEnumerable<Block> AllDescendants(this MarkdownDocument document)
    {
        foreach (var block in document)
        {
            yield return block;

            if (block is ContainerBlock container)
                foreach (var child in DescendantBlocksOfType<Block>(container))
                    yield return child;
        }
    }

    /// <summary>
    ///     Returns all descendant blocks of a specific type from a Markdown document.
    /// </summary>
    internal static IEnumerable<T> DescendantsOfType<T>(this MarkdownDocument document) where T : Block
    {
        foreach (var block in document)
        {
            if (block is T typed)
                yield return typed;

            if (block is ContainerBlock container)
                foreach (var child in DescendantBlocksOfType<T>(container))
                    yield return child;
        }
    }

    private static IEnumerable<T> DescendantBlocksOfType<T>(ContainerBlock container) where T : Block
    {
        foreach (var block in container)
        {
            if (block is T typed)
                yield return typed;

            if (block is ContainerBlock childContainer)
                foreach (var child in DescendantBlocksOfType<T>(childContainer))
                    yield return child;
        }
    }
}