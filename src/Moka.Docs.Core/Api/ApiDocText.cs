using System.Net;
using System.Text.RegularExpressions;

namespace Moka.Docs.Core.Api;

/// <summary>
///     Works with the HTML that the XML doc parsers produce: converts it to plain text and checks the
///     URLs it links to.
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

	/// <summary>
	///     Checks a link target from a doc comment before it becomes an <c>href</c>. Only http, https and
	///     mailto URLs, and relative ones, are links; anything else, such as a <c>javascript:</c> URL, isn't.
	/// </summary>
	/// <remarks>
	///     Browsers drop tabs and line breaks anywhere in a URL and control characters around it, so
	///     <c>java&amp;#9;script:</c> runs as <c>javascript:</c>. The control characters are removed before the
	///     scheme is checked, and the cleaned URL is the one to link to.
	/// </remarks>
	/// <param name="url">The URL as the comment wrote it.</param>
	/// <param name="safeUrl">The URL without control characters, or an empty string when it isn't safe.</param>
	/// <returns>Whether the URL is safe to link to.</returns>
	public static bool TryGetSafeUrl(string? url, out string safeUrl)
	{
		safeUrl = "";
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		string cleaned = new string(url.Where(c => !char.IsControl(c)).ToArray()).Trim();
		if (cleaned.Length == 0)
		{
			return false;
		}

		Match scheme = UrlSchemeRegex().Match(cleaned);
		if (scheme.Success && scheme.Groups[1].Value.ToLowerInvariant() is not ("http" or "https" or "mailto"))
		{
			return false;
		}

		safeUrl = cleaned;
		return true;
	}

	[GeneratedRegex("^([A-Za-z][A-Za-z0-9+.-]*):")]
	private static partial Regex UrlSchemeRegex();

	[GeneratedRegex(@"</?(?:p|br|li|ul|ol|pre|table|tr|th|td)\b[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex BlockTagRegex();

	[GeneratedRegex("<[^>]+>")]
	private static partial Regex TagRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
