using System.Text.RegularExpressions;
using Moka.Docs.Parsing.Highlighting;

namespace Moka.Docs.Parsing.Markdown;

// What a code fence's info string asks for: ```csharp title="Program.cs" {1,3-5} showLineNumbers wrap.
// Words it doesn't know (repl, for one) are left to the extensions that read them.
internal sealed record CodeFenceOptions
{
	public static CodeFenceOptions None { get; } = new();

	public string? Language { get; init; }

	public string? Title { get; init; }

	public IReadOnlyList<(int From, int To)> HighlightRanges { get; init; } = [];

	public bool? LineNumbers { get; init; }

	public bool Wrap { get; init; }

	public bool IsDiff =>
		Language is { } language
		&& (language.Equals("diff", StringComparison.OrdinalIgnoreCase)
		    || language.StartsWith("diff-", StringComparison.OrdinalIgnoreCase) && language.Length > 5);

	// diff-csharp is a diff of C# code; plain diff has no language for the lines themselves.
	public string? DiffLanguage => IsDiff && Language!.Length > 5 ? Language[5..] : null;

	public bool IsHighlighted(int lineNumber) =>
		HighlightRanges.Any(range => lineNumber >= range.From && lineNumber <= range.To);

	public static CodeFenceOptions Parse(string? info, string? arguments)
	{
		var ranges = new List<(int From, int To)>();
		string language = info?.Trim() ?? "";

		// ```csharp{1,3} with no space puts the ranges in the info word itself.
		int brace = language.IndexOf('{');
		if (brace >= 0)
		{
			if (language.EndsWith('}'))
			{
				AddRanges(ranges, language[(brace + 1)..^1]);
			}

			language = language[..brace];
		}

		string? title = null;
		bool? lineNumbers = null;
		bool wrap = false;
		string args = arguments ?? "";
		int i = 0;
		while (i < args.Length)
		{
			if (char.IsWhiteSpace(args[i]))
			{
				i++;
				continue;
			}

			if (args[i] == '{')
			{
				int close = args.IndexOf('}', i + 1);
				int end = close < 0 ? args.Length : close;
				AddRanges(ranges, args[(i + 1)..end]);
				i = end + 1;
				continue;
			}

			int nameStart = i;
			while (i < args.Length && !char.IsWhiteSpace(args[i]) && args[i] != '=')
			{
				i++;
			}

			string name = args[nameStart..i];
			if (i < args.Length && args[i] == '=')
			{
				string value = ReadValue(args, ref i);
				if (name.Equals("title", StringComparison.OrdinalIgnoreCase) && value.Trim().Length > 0)
				{
					title = value.Trim();
				}

				continue;
			}

			if (name.Equals("showLineNumbers", StringComparison.OrdinalIgnoreCase))
			{
				lineNumbers = true;
			}
			else if (name.Equals("noLineNumbers", StringComparison.OrdinalIgnoreCase))
			{
				lineNumbers = false;
			}
			else if (name.Equals("wrap", StringComparison.OrdinalIgnoreCase))
			{
				wrap = true;
			}
		}

		return new CodeFenceOptions
		{
			Language = language.Length == 0 ? null : language,
			Title = title,
			HighlightRanges = ranges,
			LineNumbers = lineNumbers,
			Wrap = wrap
		};
	}

	private static string ReadValue(string args, ref int i)
	{
		i++;
		if (i < args.Length && args[i] is '"' or '\'')
		{
			char quote = args[i];
			int close = args.IndexOf(quote, i + 1);
			int end = close < 0 ? args.Length : close;
			string quoted = args[(i + 1)..end];
			i = Math.Min(end + 1, args.Length);
			return quoted;
		}

		int start = i;
		while (i < args.Length && !char.IsWhiteSpace(args[i]))
		{
			i++;
		}

		return args[start..i];
	}

	private static void AddRanges(List<(int From, int To)> ranges, string spec)
	{
		foreach (string part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int dash = part.IndexOf('-');
			if (dash < 0)
			{
				if (int.TryParse(part, out int line) && line > 0)
				{
					ranges.Add((line, line));
				}

				continue;
			}

			if (int.TryParse(part.AsSpan(0, dash).Trim(), out int from)
			    && int.TryParse(part.AsSpan(dash + 1).Trim(), out int to)
			    && from > 0 && to > 0)
			{
				ranges.Add((Math.Min(from, to), Math.Max(from, to)));
			}
		}
	}
}

internal static partial class CodeFenceLines
{
	// A [!code ...] marker in a comment at the end of the line. Escaped as [!!code, it's shown as
	// [!code instead, so a page can document the syntax.
	[GeneratedRegex("""\s*(?:(?://|#|--)\s*\[!code\s+(?<kind>highlight|hl|\+\+|--|focus)\]|/\*\s*\[!code\s+(?<kind>highlight|hl|\+\+|--|focus)\]\s*\*/|<!--\s*\[!code\s+(?<kind>highlight|hl|\+\+|--|focus)\]\s*-->|@\*\s*\[!code\s+(?<kind>highlight|hl|\+\+|--|focus)\]\s*\*@)\s*$""",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
	private static partial Regex Marker();

	public static (string Text, LineMarks Marks) StripMarkers(string line)
	{
		LineMarks marks = LineMarks.None;
		if (line.Contains("[!code", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				while (Marker().Match(line) is { Success: true } match)
				{
					marks |= match.Groups["kind"].Value.ToLowerInvariant() switch
					{
						"++" => LineMarks.Added,
						"--" => LineMarks.Removed,
						"focus" => LineMarks.Focused,
						_ => LineMarks.Highlighted
					};
					line = line[..match.Index];
				}
			}
			catch (RegexMatchTimeoutException)
			{
				return (line, marks);
			}
		}

		if (line.Contains("[!!code", StringComparison.OrdinalIgnoreCase))
		{
			line = line.Replace("[!!code", "[!code", StringComparison.OrdinalIgnoreCase);
		}

		return (line, marks);
	}

	// A unified diff starts its context lines with a space; a hand-written one often doesn't. The
	// space only counts as a prefix when every context line has one. File headers (---, +++) are
	// only read as such before the first @@ hunk header.
	public static void SplitDiff(string[] texts, LineMarks[] marks)
	{
		int firstHunk = Array.FindIndex(texts, text => text.StartsWith("@@", StringComparison.Ordinal));
		for (int i = 0; i < firstHunk; i++)
		{
			if (IsDiffHeader(texts[i]))
			{
				marks[i] |= LineMarks.Hunk;
			}
		}

		bool unified = true;
		for (int i = 0; i < texts.Length; i++)
		{
			string text = texts[i];
			if (!marks[i].HasFlag(LineMarks.Hunk) && text.Length > 0 && text[0] is not ('+' or '-' or ' ')
			    && !text.StartsWith("@@", StringComparison.Ordinal))
			{
				unified = false;
				break;
			}
		}

		for (int i = 0; i < texts.Length; i++)
		{
			string text = texts[i];
			if (marks[i].HasFlag(LineMarks.Hunk) || text.StartsWith("@@", StringComparison.Ordinal))
			{
				marks[i] |= LineMarks.Hunk;
			}
			else if (text.StartsWith('+'))
			{
				marks[i] |= LineMarks.Added;
				texts[i] = text[1..];
			}
			else if (text.StartsWith('-'))
			{
				marks[i] |= LineMarks.Removed;
				texts[i] = text[1..];
			}
			else if (unified && text.StartsWith(' '))
			{
				texts[i] = text[1..];
			}
		}
	}

	private static bool IsDiffHeader(string text) =>
		text.StartsWith("diff ", StringComparison.Ordinal)
		|| text.StartsWith("index ", StringComparison.Ordinal)
		|| text.StartsWith("--- ", StringComparison.Ordinal)
		|| text.StartsWith("+++ ", StringComparison.Ordinal)
		|| text is "---" or "+++";
}
