using System.Text;

namespace Moka.Docs.Parsing.Markdown;

// Rewrites relative URLs in the attributes of raw HTML tags written in Markdown: names ending in
// src, href or srcset (data-src too, as ContentLinkRewriter treats them) and poster. Only real tags
// are read; the text of script, style and textarea elements and comments is left as written.
internal static class RawHtmlLinks
{
	private static readonly string[] _rawTextElements = ["script", "style", "textarea"];

	// Returns the same instance when nothing changed.
	public static string Rewrite(string html, string sourcePath)
	{
		if (html.IndexOf('<') < 0)
		{
			return html;
		}

		var replacements = new List<(int Start, int End, string Value)>();
		int i = 0;
		while ((i = html.IndexOf('<', i)) >= 0)
		{
			if (html.AsSpan(i).StartsWith("<!--"))
			{
				int end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
				i = end < 0 ? html.Length : end + 3;
				continue;
			}

			i++;
			if (i >= html.Length || !char.IsAsciiLetter(html[i]))
			{
				continue;
			}

			int nameStart = i;
			while (i < html.Length && (char.IsAsciiLetterOrDigit(html[i]) || html[i] == '-'))
			{
				i++;
			}

			string name = html[nameStart..i];
			i = ReadAttributes(html, i, sourcePath, replacements);

			string? rawText = _rawTextElements.FirstOrDefault(e => e.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (rawText is not null)
			{
				int close = html.IndexOf("</" + rawText, i, StringComparison.OrdinalIgnoreCase);
				i = close < 0 ? html.Length : close;
			}
		}

		if (replacements.Count == 0)
		{
			return html;
		}

		var sb = new StringBuilder(html.Length + replacements.Count * 16);
		int copied = 0;
		foreach ((int start, int end, string value) in replacements)
		{
			sb.Append(html, copied, start - copied).Append(value);
			copied = end;
		}

		return sb.Append(html, copied, html.Length - copied).ToString();
	}

	private static int ReadAttributes(string html, int i, string sourcePath, List<(int, int, string)> replacements)
	{
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
			while (i < html.Length && !IsSpace(html[i]) && html[i] is not ('=' or '>' or '/'))
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
					return html.Length;
				}

				i = valueEnd + 1;
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

			string value = html[valueStart..valueEnd];
			string? rewritten = name.EndsWith("srcset", StringComparison.OrdinalIgnoreCase)
				? RewriteSrcset(value, sourcePath)
				: IsUrlAttribute(name)
					? RelativeLinks.ToRootRelative(value, sourcePath)
					: null;
			if (rewritten is not null && rewritten != value)
			{
				replacements.Add((valueStart, valueEnd, rewritten));
			}
		}

		return i;
	}

	private static bool IsUrlAttribute(ReadOnlySpan<char> name) =>
		name.EndsWith("src", StringComparison.OrdinalIgnoreCase)
		|| name.EndsWith("href", StringComparison.OrdinalIgnoreCase)
		|| name.Equals("poster", StringComparison.OrdinalIgnoreCase);

	// Each candidate is a URL up to whitespace, then descriptors up to the next comma outside
	// parentheses; a URL with no descriptor can end with the comma itself.
	private static string? RewriteSrcset(string value, string sourcePath)
	{
		var sb = new StringBuilder(value.Length);
		bool changed = false;
		int p = 0;
		while (p < value.Length)
		{
			int separatorStart = p;
			while (p < value.Length && (IsSpace(value[p]) || value[p] == ','))
			{
				p++;
			}

			sb.Append(value, separatorStart, p - separatorStart);
			int urlStart = p;
			while (p < value.Length && !IsSpace(value[p]))
			{
				p++;
			}

			if (p == urlStart)
			{
				break;
			}

			string url = value[urlStart..p];
			string trailingComma = url.EndsWith(',') ? "," : "";
			url = url[..^trailingComma.Length];
			if (RelativeLinks.ToRootRelative(url, sourcePath) is { } rewritten)
			{
				sb.Append(rewritten);
				changed = true;
			}
			else
			{
				sb.Append(url);
			}

			sb.Append(trailingComma);
			if (trailingComma.Length > 0)
			{
				continue;
			}

			int descriptorStart = p;
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

			sb.Append(value, descriptorStart, p - descriptorStart);
		}

		return changed ? sb.ToString() : null;
	}

	private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
