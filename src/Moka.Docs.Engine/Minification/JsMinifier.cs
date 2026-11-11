using System.Text;

namespace Moka.Docs.Engine.Minification;

// Drops comments and indentation. Line breaks stay, one per run of whitespace that had one, so
// automatic semicolon insertion sees the same lines (return, ++ and -- depend on them); other
// spaces go unless removing one would join two tokens. Strings, template literals and regular
// expressions are copied as written. Telling a regex from a division needs the previous token, so
// this is a tokenizer, not a pattern replace: a regex such as /\/\/.*$/ holds "//".
internal static class JsMinifier
{
	private const char _lineSeparator = (char)0x2028;
	private const char _paragraphSeparator = (char)0x2029;
	private const char _byteOrderMark = (char)0xFEFF;

	private static readonly HashSet<string> _keywordsBeforeExpression = new(StringComparer.Ordinal)
	{
		"await", "case", "delete", "do", "else", "in", "instanceof", "new", "of", "return", "throw", "typeof",
		"void", "yield"
	};

	public static string Minify(string js)
	{
		var output = new StringBuilder(js.Length);
		// One entry per open brace: true when it is a template literal's "${".
		var braces = new Stack<bool>();
		var previous = new Previous();
		bool pendingSpace = false;
		bool pendingBreak = false;
		bool afterRegex = false;
		int i = 0;

		while (i < js.Length)
		{
			char c = js[i];

			if (IsLineBreak(c))
			{
				pendingBreak = true;
				i++;
				continue;
			}

			if (IsSpace(c))
			{
				pendingSpace = true;
				i++;
				continue;
			}

			if (c == '/' && i + 1 < js.Length && js[i + 1] == '/')
			{
				while (i < js.Length && !IsLineBreak(js[i]))
				{
					i++;
				}

				pendingSpace = true;
				continue;
			}

			if (c == '/' && i + 1 < js.Length && js[i + 1] == '*')
			{
				int close = js.IndexOf("*/", i + 2, StringComparison.Ordinal);
				int end = close < 0 ? js.Length : close + 2;
				if (ContainsLineBreak(js.AsSpan(i, end - i)))
				{
					pendingBreak = true;
				}
				else
				{
					pendingSpace = true;
				}

				i = end;
				continue;
			}

			int tokenEnd;
			TokenKind kind;
			bool isRegex = false;
			if (c is '\'' or '"')
			{
				tokenEnd = StringEnd(js, i);
				kind = TokenKind.Literal;
			}
			else if (c == '`')
			{
				tokenEnd = TemplateChunkEnd(js, i + 1, braces);
				kind = js[tokenEnd - 1] == '{' ? TokenKind.OpensExpression : TokenKind.Literal;
			}
			else if (c == '}' && braces.TryPeek(out bool inTemplate) && inTemplate)
			{
				braces.Pop();
				tokenEnd = TemplateChunkEnd(js, i + 1, braces);
				kind = js[tokenEnd - 1] == '{' ? TokenKind.OpensExpression : TokenKind.Literal;
			}
			else if (c == '/' && previous.AllowsRegex(_keywordsBeforeExpression))
			{
				tokenEnd = RegexEnd(js, i);
				kind = TokenKind.Literal;
				isRegex = true;
			}
			else if (IsWordChar(c))
			{
				tokenEnd = i + 1;
				while (tokenEnd < js.Length && IsWordChar(js[tokenEnd]))
				{
					tokenEnd++;
				}

				kind = TokenKind.Word;
			}
			else
			{
				tokenEnd = i + 1;
				kind = TokenKind.Punctuator;
				if (c == '{')
				{
					braces.Push(false);
				}
				else if (c == '}' && braces.Count > 0)
				{
					braces.Pop();
				}
			}

			if (output.Length > 0)
			{
				if (pendingBreak)
				{
					output.Append('\n');
				}
				else if (pendingSpace && WouldJoin(output[^1], c, afterRegex))
				{
					output.Append(' ');
				}
			}

			bool adjacent = !pendingSpace && !pendingBreak;
			pendingSpace = false;
			pendingBreak = false;
			output.Append(js, i, tokenEnd - i);
			previous.Set(kind, js.AsSpan(i, tokenEnd - i), adjacent);
			afterRegex = isRegex;
			i = tokenEnd;
		}

		return output.ToString();
	}

	// Two characters that must keep a space between them: they would read as one token (a b, + +,
	// 1 .x), start a comment (/ /, / *) or an HTML comment (< !--), or a word after a regex would
	// become its flags (/re/ in).
	private static bool WouldJoin(char left, char right, bool afterRegex) =>
		(IsWordChar(left) && IsWordChar(right))
		|| (left == '+' && right == '+')
		|| (left == '-' && right == '-')
		|| (left == '/' && (right is '/' or '*' || (afterRegex && IsWordChar(right))))
		|| (left == '<' && right == '!')
		|| (char.IsAsciiDigit(left) && right == '.');

	private static int StringEnd(string js, int start)
	{
		char quote = js[start];
		int i = start + 1;
		while (i < js.Length)
		{
			char c = js[i];
			if (c == '\\')
			{
				i += 2;
				continue;
			}

			if (c == quote)
			{
				return i + 1;
			}

			if (c is '\n' or '\r')
			{
				return i;
			}

			i++;
		}

		return js.Length;
	}

	// From just after a backtick or a substitution's "}", to just after the closing backtick or the
	// next "${" (which opens a brace the matching "}" resumes from).
	private static int TemplateChunkEnd(string js, int from, Stack<bool> braces)
	{
		int i = from;
		while (i < js.Length)
		{
			char c = js[i];
			if (c == '\\')
			{
				i += 2;
				continue;
			}

			if (c == '`')
			{
				return i + 1;
			}

			if (c == '$' && i + 1 < js.Length && js[i + 1] == '{')
			{
				braces.Push(true);
				return i + 2;
			}

			i++;
		}

		return js.Length;
	}

	private static int RegexEnd(string js, int start)
	{
		int i = start + 1;
		bool inClass = false;
		while (i < js.Length)
		{
			char c = js[i];
			if (c == '\\')
			{
				i += 2;
				continue;
			}

			if (IsLineBreak(c))
			{
				return i;
			}

			i++;
			if (inClass)
			{
				inClass = c != ']';
			}
			else if (c == '[')
			{
				inClass = true;
			}
			else if (c == '/')
			{
				break;
			}
		}

		while (i < js.Length && IsWordChar(js[i]))
		{
			i++;
		}

		return Math.Min(i, js.Length);
	}

	private static bool ContainsLineBreak(ReadOnlySpan<char> text)
	{
		foreach (char c in text)
		{
			if (IsLineBreak(c))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsWordChar(char c) =>
		char.IsAsciiLetterOrDigit(c) || c is '_' or '$' or '\\'
		|| (c > 127 && !IsSpace(c) && !IsLineBreak(c));

	private static bool IsLineBreak(char c) => c is '\n' or '\r' or _lineSeparator or _paragraphSeparator;

	private static bool IsSpace(char c) =>
		c == _byteOrderMark || (char.IsWhiteSpace(c) && !IsLineBreak(c));

	private enum TokenKind
	{
		Word,
		Punctuator,
		Literal,
		OpensExpression
	}

	private sealed class Previous
	{
		private TokenKind? _kind;
		private string _punctuators = "";
		private string _word = "";

		public void Set(TokenKind kind, ReadOnlySpan<char> text, bool adjacent)
		{
			_punctuators = kind == TokenKind.Punctuator
				? (adjacent && _kind == TokenKind.Punctuator ? _punctuators : "") + text.ToString()
				: "";
			_word = kind == TokenKind.Word ? text.ToString() : "";
			_kind = kind;
		}

		// A "/" starts a regex where an expression can start: at the beginning, after "${", after an
		// operator or opening bracket, or after a keyword such as return. After a name, a literal,
		// ")" or "]" it divides, and after "++" or "--" too (i++ / 2).
		public bool AllowsRegex(HashSet<string> keywordsBeforeExpression) => _kind switch
		{
			null or TokenKind.OpensExpression => true,
			TokenKind.Word => keywordsBeforeExpression.Contains(_word),
			TokenKind.Literal => false,
			_ => !(_punctuators.EndsWith(')') || _punctuators.EndsWith(']')
			       || _punctuators.EndsWith("++", StringComparison.Ordinal)
			       || _punctuators.EndsWith("--", StringComparison.Ordinal))
		};
	}
}
