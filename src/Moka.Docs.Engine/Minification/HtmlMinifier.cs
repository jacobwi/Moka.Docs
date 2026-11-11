using System.Text;

namespace Moka.Docs.Engine.Minification;

// Only changes what a browser ignores: runs of whitespace between tags and in text become one
// character, and comments go. A run that held a line break becomes "\n", so text in an element
// styled white-space: pre-line or pre-wrap keeps its line breaks. Tags and the content of pre,
// textarea, code, script and style are copied as written.
internal static class HtmlMinifier
{
	private static readonly HashSet<string> _verbatimElements = new(StringComparer.OrdinalIgnoreCase)
	{
		"pre", "textarea", "code", "script", "style"
	};

	public static string Minify(string html)
	{
		var sb = new StringBuilder(html.Length);
		int i = 0;

		while (i < html.Length)
		{
			char c = html[i];
			if (IsSpace(c))
			{
				bool lineBreak = false;
				while (i < html.Length && IsSpace(html[i]))
				{
					lineBreak |= html[i] == '\n';
					i++;
				}

				sb.Append(lineBreak ? '\n' : ' ');
				continue;
			}

			if (c != '<')
			{
				int next = NextSpecial(html, i);
				sb.Append(html, i, next - i);
				i = next;
				continue;
			}

			if (html.AsSpan(i).StartsWith("<!--"))
			{
				int end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
				int stop = end < 0 ? html.Length : end + 3;
				if (IsConditionalComment(html.AsSpan(i + 4, Math.Max(0, (end < 0 ? html.Length : end) - i - 4))))
				{
					sb.Append(html, i, stop - i);
				}

				i = stop;
				continue;
			}

			if (i + 1 < html.Length && char.IsAsciiLetter(html[i + 1]))
			{
				int nameEnd = i + 1;
				while (nameEnd < html.Length && (char.IsAsciiLetterOrDigit(html[nameEnd]) || html[nameEnd] == '-'))
				{
					nameEnd++;
				}

				string name = html[(i + 1)..nameEnd];
				int tagEnd = TagEnd(html, nameEnd);
				sb.Append(html, i, tagEnd - i);
				bool selfClosing = tagEnd >= 2 && html[tagEnd - 2] == '/';
				i = tagEnd;

				if (_verbatimElements.Contains(name) && !selfClosing)
				{
					int close = html.IndexOf("</" + name, i, StringComparison.OrdinalIgnoreCase);
					int stop = close < 0 ? html.Length : close;
					sb.Append(html, i, stop - i);
					i = stop;
				}

				continue;
			}

			if (i + 1 < html.Length && html[i + 1] is '/' or '!' or '?')
			{
				int tagEnd = TagEnd(html, i + 2);
				sb.Append(html, i, tagEnd - i);
				i = tagEnd;
				continue;
			}

			sb.Append('<');
			i++;
		}

		return sb.ToString();
	}

	// <!--[if IE]>...<![endif]--> and the <!--[if !IE]><!--> ... <!--<![endif]--> pair.
	private static bool IsConditionalComment(ReadOnlySpan<char> body) =>
		body.StartsWith("[if", StringComparison.OrdinalIgnoreCase)
		|| body.StartsWith("<![endif]", StringComparison.OrdinalIgnoreCase)
		|| body.EndsWith("<![endif]", StringComparison.OrdinalIgnoreCase);

	private static int NextSpecial(string html, int from)
	{
		int i = from;
		while (i < html.Length && html[i] != '<' && !IsSpace(html[i]))
		{
			i++;
		}

		return i;
	}

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

	private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
