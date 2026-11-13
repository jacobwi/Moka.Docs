using System.Text;
using System.Text.RegularExpressions;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Generates a <see cref="TableOfContents" /> from the heading blocks in a parsed Markdown document.
/// </summary>
public sealed partial class TocGenerator
{
	/// <summary>
	///     Generates a table of contents from a Markdig document. A leading <c>h1</c> is the page's
	///     title, not a section of it, so it is left out.
	/// </summary>
	/// <param name="document">The parsed Markdig document.</param>
	/// <returns>A <see cref="TableOfContents" /> with nested entries.</returns>
	public TableOfContents Generate(MarkdownDocument document) => Generate(document, null);

	/// <summary>
	///     Generates a table of contents from a Markdig document, leaving out the page's own title: the
	///     first heading when it is an <c>h1</c> or its text is <paramref name="pageTitle" />.
	/// </summary>
	/// <remarks>
	///     The title used to be the one root entry, so every heading nested under it, the theme's
	///     collapse toggle on it hid the whole list, and it used up one of the nesting levels the theme
	///     shows. The headings under it become the roots.
	/// </remarks>
	/// <param name="document">The parsed Markdig document.</param>
	/// <param name="pageTitle">The page title from front matter, or <c>null</c>.</param>
	/// <returns>A <see cref="TableOfContents" /> with nested entries.</returns>
	public TableOfContents Generate(MarkdownDocument document, string? pageTitle)
	{
		var flatEntries = new List<TocEntry>();

		foreach (HeadingBlock block in document.DescendantsOfType<HeadingBlock>())
		{
			// A badge left out of the text ("Setup :badge[New] guide") leaves two spaces behind.
			string text = SpaceRuns().Replace(InlineText.Of(block.Inline), " ").Trim();
			HtmlAttributes? attributes = block.TryGetAttributes();
			string id = attributes?.Id ?? GenerateAnchorId(text);

			flatEntries.Add(new TocEntry
			{
				Level = block.Level,
				Text = text,
				Id = id
			});
		}

		if (flatEntries.Count > 0 && IsPageTitle(flatEntries[0], pageTitle))
		{
			flatEntries.RemoveAt(0);
		}

		if (flatEntries.Count == 0)
		{
			return TableOfContents.Empty;
		}

		List<TocEntry> nested = BuildNestedEntries(flatEntries);
		return new TableOfContents { Entries = nested };
	}

	#region Private Helpers

	private static bool IsPageTitle(TocEntry entry, string? pageTitle) =>
		entry.Level == 1
		|| (!string.IsNullOrWhiteSpace(pageTitle)
		    && string.Equals(entry.Text.Trim(), pageTitle.Trim(), StringComparison.OrdinalIgnoreCase));

	/// <summary>
	///     Generates a URL-safe anchor ID from heading text.
	///     Follows GitHub-style anchor generation.
	/// </summary>
	public static string GenerateAnchorId(string text)
	{
		var sb = new StringBuilder(text.Length);

		foreach (char ch in text)
		{
			if (char.IsLetterOrDigit(ch))
			{
				sb.Append(char.ToLowerInvariant(ch));
			}
			else if (ch is ' ' or '-')
			{
				sb.Append('-');
			}
		}

		// Other characters are stripped
		// Collapse multiple dashes
		string result = sb.ToString();
		while (result.Contains("--"))
		{
			result = result.Replace("--", "-");
		}

		return result.Trim('-');
	}

	private static List<TocEntry> BuildNestedEntries(List<TocEntry> flat)
	{
		var root = new List<TocEntry>();
		var stack = new Stack<TocEntry>();

		foreach (TocEntry entry in flat)
		{
			// Pop entries from stack that are at the same level or deeper
			while (stack.Count > 0 && stack.Peek().Level >= entry.Level)
			{
				stack.Pop();
			}

			if (stack.Count == 0)
			{
				root.Add(entry);
			}
			else
			{
				stack.Peek().Children.Add(entry);
			}

			stack.Push(entry);
		}

		return root;
	}

	[GeneratedRegex(@"\s{2,}")]
	private static partial Regex SpaceRuns();

	#endregion
}
