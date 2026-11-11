using System.Text;

namespace Moka.Docs.Engine.Minification;

// Drops comments, turns each run of whitespace into one space and removes the spaces next to
// { } ; and , plus the last ; of a block. Strings and url(...) are copied as written. Spaces around
// ':' stay: "a :hover" and "a:hover" are different selectors.
internal static class CssMinifier
{
	public static string Minify(string css)
	{
		var sb = new StringBuilder(css.Length);
		bool pendingSpace = false;
		int i = 0;

		while (i < css.Length)
		{
			char c = css[i];

			if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
			{
				int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
				i = end < 0 ? css.Length : end + 2;
				pendingSpace = true;
				continue;
			}

			if (char.IsWhiteSpace(c))
			{
				pendingSpace = true;
				i++;
				continue;
			}

			// "--x: ;" is an empty custom property, which early versions of the spec didn't allow
			// without the space, so that space and its ';' stay.
			if (c == '}' && sb.Length >= 2 && sb[^1] == ';' && sb[^2] is not (' ' or ':'))
			{
				sb.Length--;
			}

			if (pendingSpace)
			{
				pendingSpace = false;
				char previous = sb.Length > 0 ? sb[^1] : '{';
				bool tight = (IsTight(previous) || IsTight(c)) && !(previous == ':' && c == ';');
				if (!tight)
				{
					sb.Append(' ');
				}
			}

			if (c is '"' or '\'')
			{
				int end = StringEnd(css, i);
				sb.Append(css, i, end - i);
				i = end;
				continue;
			}

			if (c is 'u' or 'U' && IsUrlStart(css, i) && UrlEnd(css, i + 4) is { } urlEnd)
			{
				sb.Append(css, i, urlEnd - i);
				i = urlEnd;
				continue;
			}

			sb.Append(c);
			i++;
		}

		return sb.ToString();
	}

	private static bool IsTight(char c) => c is '{' or '}' or ';' or ',';

	private static int StringEnd(string css, int start)
	{
		char quote = css[start];
		int i = start + 1;
		while (i < css.Length)
		{
			if (css[i] == '\\')
			{
				i += 2;
				continue;
			}

			if (css[i] == quote || css[i] == '\n')
			{
				return i + 1;
			}

			i++;
		}

		return css.Length;
	}

	private static bool IsUrlStart(string css, int i) =>
		css.AsSpan(i).StartsWith("url(", StringComparison.OrdinalIgnoreCase)
		&& (i == 0 || !(char.IsLetterOrDigit(css[i - 1]) || css[i - 1] is '-' or '_'));

	// An unquoted url(...) can hold "/*" (in base64, for one) that isn't a comment. A quoted one is
	// a string and handled as such.
	private static int? UrlEnd(string css, int from)
	{
		int i = from;
		while (i < css.Length && char.IsWhiteSpace(css[i]))
		{
			i++;
		}

		if (i < css.Length && css[i] is '"' or '\'')
		{
			return null;
		}

		int close = css.IndexOf(')', i);
		return close < 0 ? null : close + 1;
	}
}
