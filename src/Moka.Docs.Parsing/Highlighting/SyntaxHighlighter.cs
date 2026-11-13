using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.Parsing.Highlighting;

/// <summary>
///     Turns source code into HTML with <c>&lt;span class="token …"&gt;</c> elements around keywords,
///     strings, comments and the like, for the theme's code colors.
/// </summary>
/// <remarks>
///     The token classes are <c>comment</c>, <c>string</c>, <c>keyword</c>, <c>number</c>,
///     <c>type-name</c>, <c>tag-name</c>, <c>attr-name</c> and <c>property</c>. Text between tokens is
///     escaped too, so the result is always safe to put inside a <c>&lt;code&gt;</c> element.
///     Highlighting is a set of regular expressions per language, not a parser: it gives the same
///     result the theme's browser script gave before highlighting moved to the build.
/// </remarks>
public static class SyntaxHighlighter
{
	/// <summary>
	///     The language names the highlighter has rules for, without aliases such as <c>cs</c>.
	/// </summary>
	public static IReadOnlyCollection<string> Languages { get; } = LanguageRules.Languages.ToArray();

	/// <summary>
	///     Returns the name the highlighter uses for a fence language (<c>cs</c> and <c>c#</c> give
	///     <c>csharp</c>), or <c>null</c> when it has no rules for it. Case is ignored.
	/// </summary>
	/// <param name="language">A language name as written after a code fence.</param>
	public static string? ResolveLanguage(string? language) => LanguageRules.Resolve(language);

	/// <summary>
	///     Highlights <paramref name="code" /> as <paramref name="language" />. Code in a language
	///     without rules, or with no language, comes back escaped and otherwise unchanged.
	/// </summary>
	/// <param name="code">The source code, with <c>\n</c> line breaks.</param>
	/// <param name="language">A language name or alias; <c>null</c> for plain text.</param>
	/// <returns>HTML for the inside of a <c>&lt;code&gt;</c> element.</returns>
	public static string Highlight(string code, string? language)
	{
		var html = new StringBuilder(code.Length + code.Length / 2);
		int position = 0;
		foreach (HighlightToken token in Tokenize(code, language))
		{
			AppendEscaped(html, code.AsSpan(position, token.Start - position));
			html.Append("<span class=\"token ").Append(token.TokenClass).Append("\">");
			AppendEscaped(html, code.AsSpan(token.Start, token.End - token.Start));
			html.Append("</span>");
			position = token.End;
		}

		AppendEscaped(html, code.AsSpan(position));
		return html.ToString();
	}

	/// <summary>
	///     Highlights <paramref name="code" /> and returns one HTML string per line. A token that runs
	///     over several lines, such as a block comment, is closed at the end of each line and opened
	///     again on the next, so every line can be wrapped in an element of its own.
	/// </summary>
	/// <param name="code">The source code, with <c>\n</c> line breaks.</param>
	/// <param name="language">A language name or alias; <c>null</c> for plain text.</param>
	/// <returns>As many strings as <paramref name="code" /> has lines (one for empty code).</returns>
	public static IReadOnlyList<string> HighlightLines(string code, string? language)
	{
		var lines = new List<string>();
		var line = new StringBuilder();
		int position = 0;
		foreach (HighlightToken token in Tokenize(code, language))
		{
			AppendLines(lines, line, code, position, token.Start, null);
			AppendLines(lines, line, code, token.Start, token.End, token.TokenClass);
			position = token.End;
		}

		AppendLines(lines, line, code, position, code.Length, null);
		lines.Add(line.ToString());
		return lines;
	}

	/// <summary>
	///     Escapes <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c> and <c>"</c> for HTML element content, as
	///     Markdig does for code.
	/// </summary>
	/// <param name="text">The text to escape.</param>
	public static string Escape(string text)
	{
		if (text.AsSpan().IndexOfAny("&<>\"") < 0)
		{
			return text;
		}

		var html = new StringBuilder(text.Length + 16);
		AppendEscaped(html, text);
		return html.ToString();
	}

	// Every rule runs over the whole text. Then a token that starts inside an earlier one is dropped,
	// and of two starting at the same place the longer wins, the earlier rule on a tie.
	internal static IReadOnlyList<HighlightToken> Tokenize(string code, string? language)
	{
		if (code.Length == 0 || LanguageRules.Resolve(language) is not { } name)
		{
			return [];
		}

		var found = new List<(HighlightToken Token, int Rank)>();
		try
		{
			foreach (TokenRule rule in LanguageRules.For(name))
			{
				for (Match match = rule.Pattern.Match(code); match.Success; match = match.NextMatch())
				{
					if (match.Length > 0)
					{
						found.Add((new HighlightToken(match.Index, match.Index + match.Length, rule.TokenClass), found.Count));
					}
				}
			}
		}
		catch (RegexMatchTimeoutException)
		{
			// A pathological sample isn't worth stalling the build for; it is shown unhighlighted.
			return [];
		}

		found.Sort((a, b) => a.Token.Start != b.Token.Start
			? a.Token.Start.CompareTo(b.Token.Start)
			: a.Token.End != b.Token.End
				? b.Token.End.CompareTo(a.Token.End)
				: a.Rank.CompareTo(b.Rank));

		var tokens = new List<HighlightToken>();
		int lastEnd = -1;
		foreach ((HighlightToken token, _) in found)
		{
			if (token.Start >= lastEnd)
			{
				tokens.Add(token);
				lastEnd = token.End;
			}
		}

		return tokens;
	}

	private static void AppendLines(List<string> lines, StringBuilder line, string code, int start, int end, string? tokenClass)
	{
		while (start < end)
		{
			int newline = code.IndexOf('\n', start, end - start);
			int segmentEnd = newline < 0 ? end : newline;
			if (segmentEnd > start)
			{
				if (tokenClass is not null)
				{
					line.Append("<span class=\"token ").Append(tokenClass).Append("\">");
				}

				AppendEscaped(line, code.AsSpan(start, segmentEnd - start));
				if (tokenClass is not null)
				{
					line.Append("</span>");
				}
			}

			if (newline < 0)
			{
				return;
			}

			lines.Add(line.ToString());
			line.Clear();
			start = newline + 1;
		}
	}

	private static void AppendEscaped(StringBuilder html, ReadOnlySpan<char> text)
	{
		foreach (char c in text)
		{
			switch (c)
			{
				case '&':
					html.Append("&amp;");
					break;
				case '<':
					html.Append("&lt;");
					break;
				case '>':
					html.Append("&gt;");
					break;
				case '"':
					html.Append("&quot;");
					break;
				default:
					html.Append(c);
					break;
			}
		}
	}
}

internal readonly record struct HighlightToken(int Start, int End, string TokenClass);
