using Markdig.Helpers;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     The opening line of a component after its colons, <c>name{attributes} text</c>: the component's
///     name, the attributes in braces and the text after them, with the text's place in the source.
/// </summary>
internal sealed class ComponentHeader
{
	public required string Name { get; init; }

	public required Dictionary<string, string> Attributes { get; init; }

	public required string Text { get; init; }

	/// <summary>Where <see cref="Text" /> starts in the source, or -1 when there is none.</summary>
	public int TextStart { get; init; } = -1;

	/// <summary>Where <see cref="Text" /> ends in the source (inclusive, as <see cref="StringSlice.End" />).</summary>
	public int TextEnd { get; init; } = -1;

	/// <summary>
	///     Reads the header from <paramref name="line" />, positioned just after the colons, or returns
	///     <c>null</c> when the line doesn't start with a name.
	/// </summary>
	public static ComponentHeader? Parse(StringSlice line)
	{
		string source = line.Text;
		int end = line.End;
		int i = SkipSpaces(source, line.Start, end);

		int nameStart = i;
		while (i <= end && (char.IsAsciiLetterOrDigit(source[i]) || source[i] is '-' or '_'))
		{
			i++;
		}

		if (i == nameStart)
		{
			return null;
		}

		string name = source[nameStart..i];
		var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		int afterName = SkipSpaces(source, i, end);
		if (afterName <= end && source[afterName] == '{')
		{
			int close = FindClosingBrace(source, afterName, end);
			if (close > afterName)
			{
				attributes = ComponentAttributes.Parse(source.AsSpan(afterName + 1, close - afterName - 1));
				i = close + 1;
			}
		}

		int textStart = SkipSpaces(source, i, end);
		int textEnd = end;
		while (textEnd >= textStart && char.IsWhiteSpace(source[textEnd]))
		{
			textEnd--;
		}

		bool hasText = textEnd >= textStart;
		return new ComponentHeader
		{
			Name = name,
			Attributes = attributes,
			Text = hasText ? source[textStart..(textEnd + 1)] : "",
			TextStart = hasText ? textStart : -1,
			TextEnd = hasText ? textEnd : -1
		};
	}

	/// <summary>The brace that closes the one at <paramref name="open" />, skipping quoted values, or -1.</summary>
	internal static int FindClosingBrace(string source, int open, int end)
	{
		char quote = '\0';
		for (int i = open + 1; i <= end; i++)
		{
			char c = source[i];
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
			else if (c == '}')
			{
				return i;
			}
		}

		return -1;
	}

	private static int SkipSpaces(string source, int i, int end)
	{
		while (i <= end && source[i] is ' ' or '\t')
		{
			i++;
		}

		return i;
	}
}

/// <summary>
///     The attributes of a component in braces: <c>key="value"</c>, <c>key='value'</c>, <c>key=value</c>,
///     or a word on its own (a flag such as <c>open</c>, whose value is empty).
/// </summary>
internal static class ComponentAttributes
{
	public static Dictionary<string, string> Parse(ReadOnlySpan<char> span)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int i = 0;
		while (i < span.Length)
		{
			while (i < span.Length && char.IsWhiteSpace(span[i]))
			{
				i++;
			}

			int keyStart = i;
			while (i < span.Length && !char.IsWhiteSpace(span[i]) && span[i] != '=')
			{
				i++;
			}

			if (i == keyStart)
			{
				// A stray '=' with no name before it: skip it and go on.
				i++;
				continue;
			}

			string key = span[keyStart..i].ToString();
			int j = i;
			while (j < span.Length && char.IsWhiteSpace(span[j]))
			{
				j++;
			}

			if (j >= span.Length || span[j] != '=')
			{
				result[key] = "";
				continue;
			}

			j++;
			while (j < span.Length && char.IsWhiteSpace(span[j]))
			{
				j++;
			}

			if (j < span.Length && span[j] is '"' or '\'')
			{
				char quote = span[j];
				int close = span[(j + 1)..].IndexOf(quote);
				if (close < 0)
				{
					result[key] = span[(j + 1)..].ToString();
					break;
				}

				result[key] = span.Slice(j + 1, close).ToString();
				i = j + close + 2;
			}
			else
			{
				int valueStart = j;
				while (j < span.Length && !char.IsWhiteSpace(span[j]))
				{
					j++;
				}

				result[key] = span[valueStart..j].ToString();
				i = j;
			}
		}

		return result;
	}
}
