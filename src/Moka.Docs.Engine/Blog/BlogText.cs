using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.Engine.Blog;

/// <summary>Plain text pieces of a post: its summary, reading time and tag slugs.</summary>
internal static partial class BlogText
{
	/// <summary>Words per minute behind <see cref="ReadingMinutes" />.</summary>
	public const int WordsPerMinute = 200;

	/// <summary>Longest summary taken from a paragraph, in characters, before it is cut at a word.</summary>
	public const int MaxSummaryLength = 300;

	private const int _maxSlugLength = 60;

	// The device names Windows reserves in every folder. A tag page named after one can't be written there.
	private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"con", "prn", "aux", "nul",
		"com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
		"lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
	};

	/// <summary>
	///     The first paragraph of the post's HTML as plain text, cut at a word near
	///     <see cref="MaxSummaryLength" /> characters. Paragraphs inside admonitions, lists, tables
	///     and other blocks are used only when the post has no paragraph of its own.
	/// </summary>
	public static string FirstParagraph(string html)
	{
		string? nested = null;
		int depth = 0;
		foreach (Match tag in BlockTag().Matches(html))
		{
			string name = tag.Groups["name"].Value.ToLowerInvariant();
			bool closing = tag.Groups["close"].Success;
			if (name != "p")
			{
				depth = Math.Max(0, depth + (closing ? -1 : 1));
				continue;
			}

			if (closing)
			{
				continue;
			}

			int start = tag.Index + tag.Length;
			int end = html.IndexOf("</p>", start, StringComparison.OrdinalIgnoreCase);
			string text = PlainText(end < 0 ? html[start..] : html[start..end]);
			if (text.Length == 0)
			{
				continue;
			}

			if (depth == 0)
			{
				return Shorten(text);
			}

			nested ??= text;
		}

		return nested is null ? "" : Shorten(nested);
	}

	/// <summary>Minutes to read the text at <see cref="WordsPerMinute" />, rounded up, at least 1.</summary>
	public static int ReadingMinutes(string plainText)
	{
		int words = plainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
		return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
	}

	/// <summary>
	///     A URL segment for a tag: lower case letters and digits joined by hyphens, accents removed,
	///     <c>#</c> spelled <c>sharp</c> and <c>+</c> spelled <c>plus</c> (<c>C#</c> is
	///     <c>csharp</c>). Never empty, at most 60 characters and never a name Windows reserves.
	/// </summary>
	public static string Slug(string tag)
	{
		string spelled = tag.Trim().Replace("#", "sharp", StringComparison.Ordinal)
			.Replace("+", "plus", StringComparison.Ordinal);

		var sb = new StringBuilder();
		bool pendingHyphen = false;
		foreach (char c in spelled.Normalize(NormalizationForm.FormD))
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}

			if (char.IsLetterOrDigit(c))
			{
				if (pendingHyphen && sb.Length > 0)
				{
					sb.Append('-');
				}

				sb.Append(char.ToLowerInvariant(c));
				pendingHyphen = false;
			}
			else
			{
				pendingHyphen = true;
			}
		}

		string slug = sb.ToString().Normalize(NormalizationForm.FormC);
		if (slug.Length > _maxSlugLength)
		{
			slug = slug[.._maxSlugLength].TrimEnd('-');
		}

		if (slug.Length == 0)
		{
			return "tag";
		}

		return _reservedNames.Contains(slug) ? slug + "-tag" : slug;
	}

	// Inline tags such as <code> and <strong> sit inside words and sentences, so they go without a
	// trace; a line break becomes a space.
	private static string PlainText(string html)
	{
		string text = WebUtility.HtmlDecode(AnyTag().Replace(LineBreak().Replace(html, " "), ""));
		return Whitespace().Replace(text, " ").Trim();
	}

	private static string Shorten(string text)
	{
		if (text.Length <= MaxSummaryLength)
		{
			return text;
		}

		int cut = text.LastIndexOf(' ', MaxSummaryLength);
		return (cut > MaxSummaryLength / 2 ? text[..cut] : text[..MaxSummaryLength]).TrimEnd(',', ';', ':', '.') + "…";
	}

	[GeneratedRegex(@"<(?<close>/)?(?<name>p|div|blockquote|ul|ol|table|details|aside|section|figure|pre)\b[^>]*>",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex BlockTag();

	[GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant)]
	private static partial Regex AnyTag();

	[GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex LineBreak();

	[GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
	private static partial Regex Whitespace();
}
