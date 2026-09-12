using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Remembers how long the fence that opened a container was (<c>:::</c>, <c>::::</c>,
///     <c>===</c>), so a nested container's closing line does not also close its parent.
/// </summary>
/// <remarks>
///     Markdig offers each line to the outermost open block first. Every container used to close
///     on any bare <c>:::</c>, so the inner block's closing line ended the outer block instead,
///     and whatever followed spilled out of it. Now a line closes a container only when it is at
///     least as long as that container's opening fence, the rule fenced code blocks follow:
///     write the outer block with more colons than the inner one.
/// </remarks>
internal static class ContainerFence
{
	private static readonly object _key = new();

	/// <summary>Records the length of the fence that opened <paramref name="block" />.</summary>
	internal static void Set(Block block, int length) => block.SetData(_key, length);

	/// <summary>
	///     Whether a line of <paramref name="length" /> fence characters can close or continue
	///     <paramref name="block" />, rather than belonging to a container nested inside it.
	/// </summary>
	internal static bool Reaches(Block block, int length) =>
		length >= (block.GetData(_key) is int opened ? opened : 3);
}
