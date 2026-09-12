using System.Net;
using System.Text.RegularExpressions;

namespace Moka.Docs.Core.Api;

/// <summary>
///     Converts the HTML that the XML doc parsers produce into plain text.
/// </summary>
public static partial class ApiDocText
{
	/// <summary>
	///     Strips tags and decodes entities. Meta descriptions and search snippets show text as-is,
	///     so a summary with <c>&lt;c&gt;List&lt;T&gt;&lt;/c&gt;</c> showed the markup instead of <c>List&lt;T&gt;</c>.
	/// </summary>
	/// <param name="html">An HTML fragment such as <see cref="XmlDocBlock.Summary" />.</param>
	/// <returns>The text, with whitespace collapsed.</returns>
	public static string ToPlainText(string? html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}

		string text = BlockTagRegex().Replace(html, " ");
		text = TagRegex().Replace(text, "");
		return WhitespaceRegex().Replace(WebUtility.HtmlDecode(text), " ").Trim();
	}

	[GeneratedRegex(@"</?(?:p|br|li|ul|ol|pre)\b[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex BlockTagRegex();

	[GeneratedRegex("<[^>]+>")]
	private static partial Regex TagRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
