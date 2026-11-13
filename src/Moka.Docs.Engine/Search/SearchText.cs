using System.Net;
using System.Text;

namespace Moka.Docs.Engine.Search;

// Heading entries used to have no content, so only a page's first 300 characters were searchable,
// and code blocks weren't indexed at all: an identifier that only appears in a sample could not be
// found. This splits the body HTML rather than the Markdown, so plugin pages are split the same way.
internal static class SearchText
{
	// The theme shows at most 120 characters of an entry's text.
	internal const int TextLimit = 300;
	internal const int CodeLimit = 300;

	// Past this many distinct words the result can't change once CodeLimit is reached, and it keeps
	// the containment check cheap on a page with a huge sample.
	private const int MaxCodeWords = 400;

	private static readonly HashSet<string> _inlineTags = new(StringComparer.OrdinalIgnoreCase)
	{
		"a", "abbr", "b", "bdi", "bdo", "cite", "code", "data", "del", "dfn", "em", "i", "ins", "kbd", "mark",
		"q", "s", "samp", "small", "span", "strong", "sub", "sup", "time", "u", "var", "wbr"
	};

	private static readonly HashSet<string> _skippedTags = new(StringComparer.OrdinalIgnoreCase)
	{
		"script", "style", "svg", "template"
	};

	public static SectionSplit Split(string html, IReadOnlySet<string> headingIds)
	{
		var intro = new SectionBuilder();
		var sections = new Dictionary<string, SectionBuilder>(StringComparer.Ordinal);
		SectionBuilder current = intro;
		int i = 0;

		while (i < html.Length)
		{
			int lt = html.IndexOf('<', i);
			if (lt < 0)
			{
				current.Text.Append(html, i, html.Length - i);
				break;
			}

			current.Text.Append(html, i, lt - i);
			i = lt;

			if (html.AsSpan(i).StartsWith("<!--"))
			{
				int end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
				i = end < 0 ? html.Length : end + 3;
				continue;
			}

			bool closing = i + 1 < html.Length && html[i + 1] == '/';
			int nameStart = closing ? i + 2 : i + 1;
			int nameEnd = nameStart;
			while (nameEnd < html.Length && (char.IsAsciiLetterOrDigit(html[nameEnd]) || html[nameEnd] == '-'))
			{
				nameEnd++;
			}

			if (nameEnd == nameStart || !char.IsAsciiLetter(html[nameStart]))
			{
				current.Text.Append('<');
				i++;
				continue;
			}

			string name = html[nameStart..nameEnd];
			int tagEnd = TagEnd(html, nameEnd);
			string tag = html[i..tagEnd];
			i = tagEnd;

			if (closing)
			{
				AppendBreak(current, name);
				continue;
			}

			if (IsHeading(name) && AttributeValue(tag, "id") is { } id && headingIds.Contains(id))
			{
				// The heading's own text is already the entry's section title.
				i = AfterClosingTag(html, i, name);
				current = new SectionBuilder();
				sections.TryAdd(id, current);
				continue;
			}

			if (name.Equals("pre", StringComparison.OrdinalIgnoreCase)
			    || name.Equals("textarea", StringComparison.OrdinalIgnoreCase))
			{
				int close = IndexOfClosingTag(html, i, name);
				if (!HasClass(tag, "mermaid"))
				{
					current.Code.Add(WebUtility.HtmlDecode(StripTags(html[i..close])));
				}

				i = AfterClosingTag(html, close, name);
				current.Text.Append(' ');
				continue;
			}

			// Glossary tooltips repeat a definition wherever its term is used; the glossary list has it once.
			if (_skippedTags.Contains(name) || HasAttribute(tag, "data-no-search"))
			{
				i = AfterClosingTag(html, i, name);
				current.Text.Append(' ');
				continue;
			}

			AppendBreak(current, name);
		}

		return new SectionSplit(
			intro.Build(),
			sections.ToDictionary(s => s.Key, s => s.Value.Build(), StringComparer.Ordinal));
	}

	// Cut at the last space when there is one in the final fifth, so the last indexed word is whole.
	internal static string Trim(string text, int limit)
	{
		if (text.Length <= limit)
		{
			return text;
		}

		int space = text.LastIndexOf(' ', limit);
		return space > limit * 4 / 5 ? text[..space] : text[..limit];
	}

	// A word that another kept word contains is left out: the theme matches substrings, so
	// "context.FileSystem" already finds "FileSystem".
	internal static string CodeWords(IEnumerable<string> blocks, int limit)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var kept = new List<string>();
		int length = 0;

		foreach (string word in blocks.SelectMany(Words))
		{
			if (seen.Count >= MaxCodeWords)
			{
				break;
			}

			if (!seen.Add(word) || kept.Any(k => k.Contains(word, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}

			if (kept.RemoveAll(k => word.Contains(k, StringComparison.OrdinalIgnoreCase)) > 0)
			{
				length = kept.Sum(k => k.Length + 1);
			}

			if (length + word.Length > limit)
			{
				continue;
			}

			kept.Add(word);
			length += word.Length + 1;
		}

		return string.Join(' ', kept);
	}

	// Words keep their dots and dashes (context.FileSystem, --base-path) so a dotted or hyphenated
	// query still matches. Anything without a letter (numbers, ---) is left out.
	private static IEnumerable<string> Words(string code)
	{
		int i = 0;
		while (i < code.Length)
		{
			while (i < code.Length && !IsWordChar(code[i]))
			{
				i++;
			}

			int start = i;
			while (i < code.Length && IsWordChar(code[i]))
			{
				i++;
			}

			string word = code[start..i].TrimStart('.').TrimEnd('.', '-');
			if (word.Length >= 2 && word.Any(char.IsLetter))
			{
				yield return word;
			}
		}
	}

	private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.' or '-';

	private static void AppendBreak(SectionBuilder section, string tagName)
	{
		if (!_inlineTags.Contains(tagName))
		{
			section.Text.Append(' ');
		}
	}

	private static bool IsHeading(string name) =>
		name.Length == 2 && name[0] is 'h' or 'H' && name[1] is >= '1' and <= '6';

	// Just past the '>' that ends the tag, skipping quoted attribute values.
	private static int TagEnd(string html, int from)
	{
		char quote = '\0';
		for (int i = from; i < html.Length; i++)
		{
			char c = html[i];
			if (quote != '\0')
			{
				if (c == quote)
				{
					quote = '\0';
				}
			}
			else if (c is '"' or '\'')
			{
				quote = c;
			}
			else if (c == '>')
			{
				return i + 1;
			}
		}

		return html.Length;
	}

	private static int IndexOfClosingTag(string html, int from, string name)
	{
		int at = html.IndexOf("</" + name, from, StringComparison.OrdinalIgnoreCase);
		return at < 0 ? html.Length : at;
	}

	private static int AfterClosingTag(string html, int from, string name)
	{
		int at = IndexOfClosingTag(html, from, name);
		if (at >= html.Length)
		{
			return html.Length;
		}

		int end = html.IndexOf('>', at);
		return end < 0 ? html.Length : end + 1;
	}

	private static string StripTags(string html)
	{
		var sb = new StringBuilder(html.Length);
		int i = 0;
		while (i < html.Length)
		{
			int lt = html.IndexOf('<', i);
			if (lt < 0)
			{
				sb.Append(html, i, html.Length - i);
				break;
			}

			sb.Append(html, i, lt - i);
			if (lt + 1 < html.Length && (char.IsAsciiLetter(html[lt + 1]) || html[lt + 1] is '/' or '!'))
			{
				i = TagEnd(html, lt + 1);
			}
			else
			{
				sb.Append('<');
				i = lt + 1;
			}
		}

		return sb.ToString();
	}

	private static bool HasAttribute(string tag, string attribute)
	{
		int at = tag.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);
		return at > 0 && char.IsWhiteSpace(tag[at - 1])
		              && (at + attribute.Length >= tag.Length || tag[at + attribute.Length] is '=' or '>' or '/' || char.IsWhiteSpace(tag[at + attribute.Length]));
	}

	private static bool HasClass(string tag, string className) =>
		AttributeValue(tag, "class") is { } classes
		&& classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(className, StringComparer.Ordinal);

	private static string? AttributeValue(string tag, string attribute)
	{
		int i = tag.IndexOfAny([' ', '\t', '\n', '\r', '\f']);
		if (i < 0)
		{
			return null;
		}

		while (i < tag.Length)
		{
			while (i < tag.Length && (char.IsWhiteSpace(tag[i]) || tag[i] == '/'))
			{
				i++;
			}

			int nameStart = i;
			while (i < tag.Length && !char.IsWhiteSpace(tag[i]) && tag[i] is not ('=' or '>' or '/'))
			{
				i++;
			}

			if (i == nameStart)
			{
				return null;
			}

			bool matches = tag.AsSpan(nameStart, i - nameStart).Equals(attribute, StringComparison.OrdinalIgnoreCase);
			while (i < tag.Length && char.IsWhiteSpace(tag[i]))
			{
				i++;
			}

			if (i >= tag.Length || tag[i] != '=')
			{
				if (matches)
				{
					return "";
				}

				continue;
			}

			i++;
			while (i < tag.Length && char.IsWhiteSpace(tag[i]))
			{
				i++;
			}

			int valueStart;
			int valueEnd;
			if (i < tag.Length && tag[i] is '"' or '\'')
			{
				char quote = tag[i];
				valueStart = i + 1;
				valueEnd = tag.IndexOf(quote, valueStart);
				if (valueEnd < 0)
				{
					valueEnd = tag.Length;
				}

				i = Math.Min(valueEnd + 1, tag.Length);
			}
			else
			{
				valueStart = i;
				while (i < tag.Length && !char.IsWhiteSpace(tag[i]) && tag[i] != '>')
				{
					i++;
				}

				valueEnd = i;
			}

			if (matches)
			{
				return WebUtility.HtmlDecode(tag[valueStart..valueEnd]);
			}
		}

		return null;
	}

	private static string CollapseWhitespace(string text)
	{
		var sb = new StringBuilder(text.Length);
		bool space = false;
		foreach (char c in text)
		{
			if (char.IsWhiteSpace(c))
			{
				space = sb.Length > 0;
				continue;
			}

			if (space)
			{
				sb.Append(' ');
				space = false;
			}

			sb.Append(c);
		}

		return sb.ToString();
	}

	private sealed class SectionBuilder
	{
		public StringBuilder Text { get; } = new();

		public List<string> Code { get; } = [];

		public SectionText Build() =>
			new(Trim(CollapseWhitespace(WebUtility.HtmlDecode(Text.ToString())), TextLimit),
				CodeWords(Code, CodeLimit));
	}
}

internal sealed record SectionText(string Text, string Code);

internal sealed record SectionSplit(SectionText Intro, IReadOnlyDictionary<string, SectionText> Sections);
