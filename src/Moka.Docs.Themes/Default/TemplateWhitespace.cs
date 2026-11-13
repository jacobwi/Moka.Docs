using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Takes the indentation out of the embedded layouts' own text before Scriban parses them. The
///     layouts are indented C# raw literals, and that indentation used to be over a third of every page.
/// </summary>
/// <remarks>
///     Only the template text changes, never what an expression writes (<c>page.content</c> and the
///     like), so code samples keep every space. Outside <c>pre</c>, <c>textarea</c>, <c>script</c> and
///     <c>style</c>, a run of whitespace holding a line break renders like a single line break, so each
///     line loses its indentation, blank lines go, and a line holding only statements (<c>if</c>,
///     <c>for</c>, <c>end</c>...) joins the next one: the line before it still ends with a line break.
///     Template lines inside one of those four elements, when it spans lines, stay as written.
/// </remarks>
internal static partial class TemplateWhitespace
{
	public static string Compact(string template)
	{
		var sb = new StringBuilder(template.Length);
		string? verbatim = null;
		bool joinNext = true;
		foreach (string rawLine in template.Split('\n'))
		{
			string line = rawLine.TrimEnd('\r');
			if (verbatim is not null)
			{
				sb.Append('\n').Append(line);
				verbatim = OpenAfterClosing(line, verbatim);
				joinNext = false;
				continue;
			}

			string trimmed = line.Trim();
			if (trimmed.Length == 0)
			{
				continue;
			}

			if (!joinNext)
			{
				sb.Append('\n');
			}

			verbatim = LeftOpen(trimmed);
			sb.Append(verbatim is null ? trimmed : line.TrimStart());
			joinNext = verbatim is null && StatementsOnly().IsMatch(trimmed);
		}

		return sb.ToString();
	}

	// The element a line leaves open whose text has to stay as written, or null.
	private static string? LeftOpen(string line)
	{
		string? open = null;
		foreach (Match tag in VerbatimTag().Matches(line))
		{
			string name = tag.Groups["name"].Value.ToLowerInvariant();
			if (!tag.Groups["close"].Success)
			{
				open ??= name;
			}
			else if (open == name)
			{
				open = null;
			}
		}

		return open;
	}

	// A line inside an open element: still open unless it closes it, and then whatever the rest opens.
	private static string? OpenAfterClosing(string line, string element)
	{
		int close = line.IndexOf("</" + element, StringComparison.OrdinalIgnoreCase);
		if (close < 0)
		{
			return element;
		}

		int end = line.IndexOf('>', close);
		return end < 0 ? null : LeftOpen(line[(end + 1)..]);
	}

	[GeneratedRegex(@"<(?<close>/)?(?<name>pre|textarea|script|style)\b", RegexOptions.IgnoreCase)]
	private static partial Regex VerbatimTag();

	// Tags that write nothing: if, else, end, for, func and the like, and assignments.
	[GeneratedRegex(@"^(?:\{\{-?~?\s*(?:(?:if|else|end|for|func|while|case|when|with|capture|ret|break|continue)\b|[a-z_][a-z0-9_.]*\s*=[^=])[^}]*\}\})+$")]
	private static partial Regex StatementsOnly();
}
