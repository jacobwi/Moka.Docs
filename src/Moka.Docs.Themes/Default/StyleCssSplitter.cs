using System.Text;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Cuts the styles' stylesheet into one stylesheet per style. Each keeps the rules that apply to its
///     style in their original order, so it cascades exactly like the single stylesheet did.
/// </summary>
/// <remarks>
///     Only selectors for the style itself stay: a reader who switches styles keeps the first style's
///     file loaded, and a selector for another style left in it would apply from the wrong place in the
///     cascade. The shared scope, <c>html:is([data-style="moka"], ...)</c> with every style, becomes
///     <c>html[data-style="moka"]</c>, which has the same specificity.
/// </remarks>
internal static class StyleCssSplitter
{
	private static readonly string[] _groupingRules = ["@media", "@supports", "@container", "@layer"];

	/// <summary>Splits <paramref name="css" /> into a stylesheet for each of <paramref name="styles" />.</summary>
	/// <param name="css">Rules scoped to the shared scope of <paramref name="styles" /> or to one style.</param>
	/// <param name="styles">The style names, in the order the shared scope lists them.</param>
	/// <returns>The stylesheet of each style.</returns>
	public static Dictionary<string, string> Split(string css, IReadOnlyList<string> styles)
	{
		string shared = "html:is(" + string.Join(", ", styles.Select(s => $"[data-style=\"{s}\"]")) + ")";
		List<CssBlock> blocks = Parse(css, 0, css.Length, out _);
		string newline = css.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
		var files = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string style in styles)
		{
			var sb = new StringBuilder();
			foreach (CssBlock block in blocks)
			{
				Write(sb, block, style, shared);
			}

			files[style] = sb.ToString().TrimStart() + newline;
		}

		return files;
	}

	private static void Write(StringBuilder sb, CssBlock block, string style, string shared)
	{
		if (block.Children is { } children)
		{
			var inner = new StringBuilder();
			foreach (CssBlock child in children)
			{
				Write(inner, child, style, shared);
			}

			if (inner.Length > 0)
			{
				sb.Append(block.Leading).Append(block.Prelude).Append('{').Append(inner).Append(block.Trailing).Append('}');
			}

			return;
		}

		if (block.Body is null || block.Prelude.StartsWith('@'))
		{
			sb.Append(block.Leading).Append(block.Prelude).Append(block.Body is null ? ";" : "{" + block.Body + "}");
			return;
		}

		string? prelude = SelectorsFor(block.Prelude, style, shared);
		if (prelude is not null)
		{
			sb.Append(block.Leading).Append(prelude).Append('{').Append(block.Body).Append('}');
		}
	}

	// The selectors of a rule that apply to the style, or null when none does. A selector with no
	// style scope is kept in every file; the scoping tests reject one.
	private static string? SelectorsFor(string prelude, string style, string shared)
	{
		string own = $"html[data-style=\"{style}\"]";
		List<string> selectors = SplitSelectors(prelude);
		List<string> kept = selectors.Where(s => AppliesTo(s.Trim(), style, shared)).ToList();
		if (kept.Count == 0)
		{
			return null;
		}

		string result = kept.Count == selectors.Count
			? prelude
			: string.Join(prelude.Contains('\n') ? ",\n" : ", ", kept.Select(s => s.Trim())) + " ";
		return result.Replace(shared, own, StringComparison.Ordinal);
	}

	private static bool AppliesTo(string selector, string style, string shared)
	{
		string scoped = selector.StartsWith(":where(", StringComparison.Ordinal) ? selector[":where(".Length..] : selector;
		if (scoped.StartsWith(shared, StringComparison.Ordinal))
		{
			return true;
		}

		const string prefix = "html[data-style=\"";
		if (!scoped.StartsWith(prefix, StringComparison.Ordinal))
		{
			return true;
		}

		int end = scoped.IndexOf('"', prefix.Length);
		return end > 0 && scoped[prefix.Length..end] == style;
	}

	private static List<string> SplitSelectors(string prelude)
	{
		var parts = new List<string>();
		int depth = 0;
		int start = 0;
		for (int i = 0; i < prelude.Length; i++)
		{
			char c = prelude[i];
			if (c is '"' or '\'')
			{
				i = StringEnd(prelude, i) - 1;
			}
			else if (c is '(' or '[')
			{
				depth++;
			}
			else if (c is ')' or ']')
			{
				depth--;
			}
			else if (c == ',' && depth == 0)
			{
				parts.Add(prelude[start..i]);
				start = i + 1;
			}
		}

		parts.Add(prelude[start..]);
		return parts;
	}

	// Blocks from start to end, each with the whitespace and comments in front of it. trailing is what
	// follows the last block.
	private static List<CssBlock> Parse(string css, int start, int end, out string trailing)
	{
		var blocks = new List<CssBlock>();
		int i = start;
		while (true)
		{
			int leadingStart = i;
			i = SkipTrivia(css, i, end);
			if (i >= end)
			{
				trailing = css[leadingStart..end];
				return blocks;
			}

			string leading = css[leadingStart..i];
			int preludeStart = i;
			while (i < end && css[i] is not ('{' or ';'))
			{
				i = css[i] is '"' or '\'' ? StringEnd(css, i) : css[i] == '/' && i + 1 < end && css[i + 1] == '*' ? CommentEnd(css, i) : i + 1;
			}

			string prelude = css[preludeStart..Math.Min(i, end)];
			if (i >= end || css[i] == ';')
			{
				blocks.Add(new CssBlock(leading, prelude, null, null, ""));
				i = Math.Min(i + 1, end);
				continue;
			}

			int bodyStart = i + 1;
			int bodyEnd = MatchingBrace(css, bodyStart, end);
			if (_groupingRules.Any(r => prelude.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
			{
				List<CssBlock> children = Parse(css, bodyStart, bodyEnd, out string inner);
				blocks.Add(new CssBlock(leading, prelude, null, children, inner));
			}
			else
			{
				blocks.Add(new CssBlock(leading, prelude, css[bodyStart..bodyEnd], null, ""));
			}

			i = Math.Min(bodyEnd + 1, end);
		}
	}

	private static int SkipTrivia(string css, int i, int end)
	{
		while (i < end)
		{
			if (char.IsWhiteSpace(css[i]))
			{
				i++;
			}
			else if (css[i] == '/' && i + 1 < end && css[i + 1] == '*')
			{
				i = CommentEnd(css, i);
			}
			else
			{
				break;
			}
		}

		return i;
	}

	private static int MatchingBrace(string css, int i, int end)
	{
		int depth = 1;
		while (i < end)
		{
			char c = css[i];
			if (c is '"' or '\'')
			{
				i = StringEnd(css, i);
				continue;
			}

			if (c == '/' && i + 1 < end && css[i + 1] == '*')
			{
				i = CommentEnd(css, i);
				continue;
			}

			if (c == '{')
			{
				depth++;
			}
			else if (c == '}' && --depth == 0)
			{
				return i;
			}

			i++;
		}

		return end;
	}

	private static int CommentEnd(string css, int i)
	{
		int close = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
		return close < 0 ? css.Length : close + 2;
	}

	private static int StringEnd(string css, int i)
	{
		char quote = css[i];
		for (int j = i + 1; j < css.Length; j++)
		{
			if (css[j] == '\\')
			{
				j++;
			}
			else if (css[j] == quote || css[j] == '\n')
			{
				return j + 1;
			}
		}

		return css.Length;
	}

	private sealed record CssBlock(string Leading, string Prelude, string? Body, List<CssBlock>? Children, string Trailing);
}
