using System.Text;

namespace Moka.Docs.Rendering.Scriban;

// Puts the base path in front of root-relative URLs in page HTML: attributes named *href and *src,
// and each candidate of a *srcset list, whether double-quoted, single-quoted or unquoted. It reads
// real tags only: text and code blocks hold "<" as "&lt;", so an href='/x' shown in a code sample
// stays as written, which a regex over the whole page can't promise once it matches single quotes.
internal static class ContentLinkRewriter
{
	public static string Rewrite(string html, string basePath)
	{
		if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(basePath) || basePath == "/")
		{
			return html;
		}

		var inserts = new List<int>();
		int i = 0;
		while ((i = html.IndexOf('<', i)) >= 0)
		{
			i++;
			if (i < html.Length && char.IsAsciiLetter(html[i]))
			{
				i = ReadTag(html, i, inserts);
			}
		}

		if (inserts.Count == 0)
		{
			return html;
		}

		var sb = new StringBuilder(html.Length + inserts.Count * basePath.Length);
		int copied = 0;
		foreach (int at in inserts)
		{
			sb.Append(html, copied, at - copied).Append(basePath);
			copied = at;
		}

		return sb.Append(html, copied, html.Length - copied).ToString();
	}

	private static int ReadTag(string html, int i, List<int> inserts)
	{
		while (i < html.Length && !IsSpace(html[i]) && html[i] != '>' && html[i] != '/')
		{
			i++;
		}

		while (i < html.Length)
		{
			while (i < html.Length && (IsSpace(html[i]) || html[i] == '/'))
			{
				i++;
			}

			if (i >= html.Length || html[i] == '>')
			{
				return i;
			}

			int nameStart = i;
			while (i < html.Length && !IsSpace(html[i]) && html[i] != '=' && html[i] != '>' && html[i] != '/')
			{
				i++;
			}

			ReadOnlySpan<char> name = html.AsSpan(nameStart, i - nameStart);
			while (i < html.Length && IsSpace(html[i]))
			{
				i++;
			}

			if (i >= html.Length || html[i] != '=')
			{
				continue;
			}

			i++;
			while (i < html.Length && IsSpace(html[i]))
			{
				i++;
			}

			int valueStart;
			int valueEnd;
			if (i < html.Length && html[i] is '"' or '\'')
			{
				char quote = html[i];
				valueStart = i + 1;
				valueEnd = html.IndexOf(quote, valueStart);
				if (valueEnd < 0)
				{
					valueEnd = html.Length;
				}

				i = Math.Min(valueEnd + 1, html.Length);
			}
			else
			{
				valueStart = i;
				while (i < html.Length && !IsSpace(html[i]) && html[i] != '>')
				{
					i++;
				}

				valueEnd = i;
			}

			ReadOnlySpan<char> value = html.AsSpan(valueStart, valueEnd - valueStart);
			if (name.EndsWith("srcset", StringComparison.OrdinalIgnoreCase))
			{
				AddSrcsetInserts(value, valueStart, inserts);
			}
			else if ((name.EndsWith("href", StringComparison.OrdinalIgnoreCase)
			          || name.EndsWith("src", StringComparison.OrdinalIgnoreCase))
			         && IsRootRelative(value))
			{
				inserts.Add(valueStart);
			}
		}

		return i;
	}

	// The HTML spec ends a srcset URL at whitespace, not at a comma, so a data: URL with commas
	// stays one candidate. Descriptors run to the next comma outside parentheses.
	private static void AddSrcsetInserts(ReadOnlySpan<char> value, int offset, List<int> inserts)
	{
		int p = 0;
		while (p < value.Length)
		{
			while (p < value.Length && (IsSpace(value[p]) || value[p] == ','))
			{
				p++;
			}

			int urlStart = p;
			while (p < value.Length && !IsSpace(value[p]))
			{
				p++;
			}

			if (p == urlStart)
			{
				break;
			}

			if (IsRootRelative(value[urlStart..p]))
			{
				inserts.Add(offset + urlStart);
			}

			if (value[p - 1] == ',')
			{
				continue;
			}

			int depth = 0;
			while (p < value.Length && (value[p] != ',' || depth > 0))
			{
				if (value[p] == '(')
				{
					depth++;
				}
				else if (value[p] == ')' && depth > 0)
				{
					depth--;
				}

				p++;
			}
		}
	}

	private static bool IsRootRelative(ReadOnlySpan<char> url)
	{
		if (url.Length == 0 || url[0] != '/')
		{
			return false;
		}

		if (url.Length > 1 && url[1] == '/')
		{
			return false;
		}

		int letters = 1;
		while (letters < url.Length && char.IsAsciiLetter(url[letters]))
		{
			letters++;
		}

		bool schemeAfterSlash = letters > 1 && letters < url.Length && url[letters] == ':';
		return !schemeAfterSlash;
	}

	private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
