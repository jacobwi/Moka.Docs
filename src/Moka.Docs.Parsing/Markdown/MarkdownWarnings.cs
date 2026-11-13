using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Problems found while parsing one document, kept on the document so the parsers, which are shared
///     by every page, stay free of per-page state. <see cref="MarkdownParser" /> reports them with their
///     line in the file.
/// </summary>
internal static class MarkdownWarnings
{
	private static readonly object _key = new();

	/// <summary>Records a problem on <paramref name="line" /> (0-based, in the Markdown body).</summary>
	internal static void Add(MarkdownDocument? document, int line, string message)
	{
		if (document is null)
		{
			return;
		}

		if (document.GetData(_key) is not List<(int Line, string Message)> warnings)
		{
			warnings = [];
			document.SetData(_key, warnings);
		}

		warnings.Add((line, message));
	}

	/// <summary>The problems recorded on <paramref name="document" />, in the order they were found.</summary>
	internal static IReadOnlyList<(int Line, string Message)> Of(MarkdownDocument document) =>
		document.GetData(_key) as List<(int Line, string Message)> ?? [];
}
