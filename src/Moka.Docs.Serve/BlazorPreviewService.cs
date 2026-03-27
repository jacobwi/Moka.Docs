using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Renders Blazor/Razor component source into a static HTML preview.
///     For v1, this performs lightweight server-side rendering by:
///     <list type="number">
///         <item>Extracting the markup portion (everything before <c>@code { }</c>)</item>
///         <item>Parsing field initializers from the code block for initial values</item>
///         <item>Substituting <c>@variable</c> expressions in the markup with their initial values</item>
///         <item>Stripping Blazor-specific directives (<c>@onclick</c>, <c>@bind</c>, etc.)</item>
///     </list>
///     This gives documentation readers a visual preview of the component's initial state
///     without requiring a full Blazor runtime.
/// </summary>
public sealed class BlazorPreviewService(ILogger<BlazorPreviewService> logger)
{
	/// <summary>Maximum source length to prevent abuse.</summary>
	private const int MaxSourceLength = 50_000;

	/// <summary>
	///     Renders the given Blazor/Razor component source to a static HTML preview.
	/// </summary>
	/// <param name="source">The Razor component source code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The rendered HTML preview result.</returns>
	public Task<BlazorPreviewResult> RenderAsync(string source, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return Task.FromResult(new BlazorPreviewResult { Error = "No source code provided." });
		}

		if (source.Length > MaxSourceLength)
		{
			return Task.FromResult(new BlazorPreviewResult
			{
				Error = $"Source exceeds maximum length of {MaxSourceLength:N0} characters."
			});
		}

		try
		{
			BlazorPreviewResult result = RenderComponent(source);
			logger.LogDebug("Blazor preview: Rendered {Length} characters of source", source.Length);
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Blazor preview: Rendering error");
			return Task.FromResult(new BlazorPreviewResult
			{
				Error = $"Rendering error: {ex.Message}"
			});
		}
	}

	private static BlazorPreviewResult RenderComponent(string source)
	{
		// Split source into markup and code sections
		(string markup, string codeBlock, List<string> usingDirectives) = ExtractSections(source);

		// Parse field initializers from the code block
		Dictionary<string, string> fields = ParseFieldInitializers(codeBlock);

		// Process the markup: substitute @expressions and clean up directives
		string html = ProcessMarkup(markup, fields);

		return new BlazorPreviewResult { Html = html };
	}

	/// <summary>
	///     Splits the Razor source into its constituent parts: @using directives, markup, and @code block.
	/// </summary>
	private static (string Markup, string CodeBlock, List<string> Usings) ExtractSections(string source)
	{
		var usings = new List<string>();
		string[] lines = source.Split('\n');
		var markupLines = new List<string>();
		string codeBlock = "";

		// Extract @using directives from the top
		int startIndex = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			string trimmed = lines[i].TrimStart();
			if (trimmed.StartsWith("@using ", StringComparison.Ordinal))
			{
				usings.Add(trimmed);
				startIndex = i + 1;
			}
			else if (string.IsNullOrWhiteSpace(trimmed) && startIndex == i)
			{
				startIndex = i + 1; // skip blank lines between @using directives
			}
			else
			{
				break;
			}
		}

		// Find @code { } block
		Match codeMatch = Regex.Match(source, @"@code\s*\{", RegexOptions.Singleline);
		if (codeMatch.Success)
		{
			// Find the matching closing brace
			int codeStart = codeMatch.Index + codeMatch.Length;
			int braceDepth = 1;
			int codeEnd = codeStart;

			for (int i = codeStart; i < source.Length; i++)
			{
				if (source[i] == '{')
				{
					braceDepth++;
				}
				else if (source[i] == '}')
				{
					braceDepth--;
					if (braceDepth == 0)
					{
						codeEnd = i;
						break;
					}
				}
			}

			codeBlock = source[codeStart..codeEnd].Trim();

			// Markup is everything between the using directives and the @code block
			string markupSource = source[..codeMatch.Index];
			// Strip @using lines from markup source
			foreach (string line in markupSource.Split('\n'))
			{
				string trimmed = line.TrimStart();
				if (!trimmed.StartsWith("@using ", StringComparison.Ordinal))
				{
					markupLines.Add(line);
				}
			}
		}
		else
		{
			// No @code block — everything (minus usings) is markup
			for (int i = startIndex; i < lines.Length; i++)
			{
				markupLines.Add(lines[i]);
			}
		}

		string markup = string.Join('\n', markupLines).Trim();
		return (markup, codeBlock, usings);
	}

	/// <summary>
	///     Parses simple field initializers from the C# code block.
	///     Handles patterns like: <c>private int count = 0;</c>, <c>private string title = "Hello";</c>,
	///     and <c>private List&lt;string&gt; items = new() { "A", "B" };</c>
	/// </summary>
	private static Dictionary<string, string> ParseFieldInitializers(string codeBlock)
	{
		var fields = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrWhiteSpace(codeBlock))
		{
			return fields;
		}

		// Match field declarations with initializers
		// Pattern: optional modifiers, type, name = value;
		var fieldPattern = new Regex(
			@"(?:private|protected|public|internal)?\s*(?:static\s+)?(?:readonly\s+)?" +
			@"(?<type>[\w<>,\s\[\]?]+?)\s+(?<name>\w+)\s*=\s*(?<value>.+?)\s*;",
			RegexOptions.Multiline);

		foreach (Match match in fieldPattern.Matches(codeBlock))
		{
			string name = match.Groups["name"].Value;
			string value = match.Groups["value"].Value.Trim();

			// Resolve simple literal values
			string? resolved = ResolveValue(value);
			if (resolved is not null)
			{
				fields[name] = resolved;
			}
		}

		return fields;
	}

	/// <summary>
	///     Resolves a C# expression to a display string for simple cases.
	/// </summary>
	private static string? ResolveValue(string expression)
	{
		// Numeric literals
		if (int.TryParse(expression, out int intVal))
		{
			return intVal.ToString();
		}

		if (double.TryParse(expression, out double dblVal))
		{
			return dblVal.ToString();
		}

		// Boolean literals
		if (expression == "true")
		{
			return "True";
		}

		if (expression == "false")
		{
			return "False";
		}

		// String literals
		if (expression.StartsWith('"') && expression.EndsWith('"'))
		{
			return expression[1..^1];
		}

		// Interpolated string — return with expressions left as placeholders
		if (expression.StartsWith("$\"") && expression.EndsWith('"'))
		{
			return expression[2..^1];
		}

		// new List/Array initializer — return a descriptive placeholder
		if (expression.StartsWith("new", StringComparison.Ordinal))
		{
			// Try to extract inline collection items like new() { "A", "B", "C" }
			Match itemsMatch = Regex.Match(expression, @"\{\s*(.+?)\s*\}");
			if (itemsMatch.Success)
			{
				return itemsMatch.Groups[1].Value;
			}

			return "[collection]";
		}

		// Default — cannot resolve
		return expression;
	}

	/// <summary>
	///     Processes the Razor markup by substituting @expressions with field values
	///     and stripping Blazor event handler directives.
	/// </summary>
	private static string ProcessMarkup(string markup, Dictionary<string, string> fields)
	{
		if (string.IsNullOrWhiteSpace(markup))
		{
			return "";
		}

		string result = markup;

		// Remove @using lines that might have slipped through
		result = Regex.Replace(result, @"^@using\s+.+$", "", RegexOptions.Multiline);

		// Strip Blazor event directives (@onclick, @onchange, @bind, @ref, etc.)
		result = Regex.Replace(result, @"\s+@on\w+=""[^""]*""", "");
		result = Regex.Replace(result, @"\s+@on\w+=\w+", "");
		result = Regex.Replace(result, @"\s+@bind(?:-\w+)?=""[^""]*""", "");
		result = Regex.Replace(result, @"\s+@bind(?:-\w+)?=\w+", "");
		result = Regex.Replace(result, @"\s+@ref=""[^""]*""", "");
		result = Regex.Replace(result, @"\s+@key=""[^""]*""", "");

		// Substitute @variable references with field values
		// Match @identifier that is NOT part of a directive (@code, @if, @foreach, etc.)
		result = Regex.Replace(result,
			@"@(?!code\b|if\b|else\b|foreach\b|for\b|while\b|switch\b|using\b|inject\b|page\b|layout\b|inherits\b|attribute\b|typeparam\b|on\w)(\w+)",
			match =>
			{
				string name = match.Groups[1].Value;
				return fields.TryGetValue(name, out string? value) ? WebEncode(value) : $"@{name}";
			});

		// Process simple @if blocks — evaluate if field is truthy
		result = ProcessIfBlocks(result, fields);

		// Process simple @foreach blocks
		result = ProcessForeachBlocks(result, fields);

		return result.Trim();
	}

	/// <summary>
	///     Processes simple @if (condition) { ... } blocks in the markup.
	///     Evaluates based on field truthiness (non-zero, non-empty, true).
	/// </summary>
	private static string ProcessIfBlocks(string markup, Dictionary<string, string> fields)
	{
		// Match @if (variable) { ... }
		var pattern = new Regex(@"@if\s*\(\s*(\w+)\s*\)\s*\{([^{}]*)\}", RegexOptions.Singleline);
		return pattern.Replace(markup, match =>
		{
			string variable = match.Groups[1].Value;
			string body = match.Groups[2].Value;

			if (fields.TryGetValue(variable, out string? value))
			{
				bool isTruthy = !string.IsNullOrEmpty(value)
				                && value != "0"
				                && !string.Equals(value, "False", StringComparison.OrdinalIgnoreCase);

				return isTruthy ? body.Trim() : "";
			}

			return ""; // Unknown variable — hide the block
		});
	}

	/// <summary>
	///     Processes simple @foreach (var item in collection) { ... } blocks.
	///     For collections parsed from initializers, iterates over the items.
	/// </summary>
	private static string ProcessForeachBlocks(string markup, Dictionary<string, string> fields)
	{
		var pattern = new Regex(
			@"@foreach\s*\(\s*var\s+(\w+)\s+in\s+(\w+)\s*\)\s*\{([^{}]*)\}",
			RegexOptions.Singleline);

		return pattern.Replace(markup, match =>
		{
			string itemVar = match.Groups[1].Value;
			string collectionVar = match.Groups[2].Value;
			string bodyTemplate = match.Groups[3].Value;

			if (!fields.TryGetValue(collectionVar, out string? collectionValue))
			{
				return "";
			}

			// Try to split the collection value into individual items
			List<string> items = ParseCollectionItems(collectionValue);
			if (items.Count == 0)
			{
				return "";
			}

			var sb = new StringBuilder();
			foreach (string item in items)
			{
				string body = bodyTemplate.Replace($"@{itemVar}", WebEncode(item));
				sb.AppendLine(body.Trim());
			}

			return sb.ToString().Trim();
		});
	}

	/// <summary>
	///     Parses a comma-separated collection of items (from field initializer parsing).
	///     Handles quoted strings and bare values.
	/// </summary>
	private static List<string> ParseCollectionItems(string value)
	{
		var items = new List<string>();
		if (string.IsNullOrWhiteSpace(value) || value == "[collection]")
		{
			return items;
		}

		// Split by comma, trimming quotes
		foreach (string part in value.Split(','))
		{
			string trimmed = part.Trim().Trim('"').Trim();
			if (!string.IsNullOrEmpty(trimmed))
			{
				items.Add(trimmed);
			}
		}

		return items;
	}

	/// <summary>
	///     HTML-encodes a string value for safe embedding in the preview HTML.
	/// </summary>
	private static string WebEncode(string value)
	{
		return value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;");
	}
}

/// <summary>
///     The result of rendering a Blazor component preview.
/// </summary>
public sealed class BlazorPreviewResult
{
	/// <summary>The rendered HTML preview. Null if an error occurred.</summary>
	public string? Html { get; init; }

	/// <summary>Error message if rendering failed. Null if successful.</summary>
	public string? Error { get; init; }
}
