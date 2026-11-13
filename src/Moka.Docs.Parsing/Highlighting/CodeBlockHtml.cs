using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Parsing.Highlighting;

/// <summary>
///     Writes the markup of a highlighted code block, and gives the same markup to code blocks that
///     came from somewhere other than a Markdown fence: API pages, plugins and raw HTML.
/// </summary>
/// <remarks>
///     <para>The shape, which the default theme styles and its script finishes:</para>
///     <code>
///     &lt;div class="code-block" data-lang="csharp" [data-title] [data-line-numbers="true|false"] [data-wrap] [data-diff] [data-focus]&gt;
///       [&lt;div class="code-title"&gt;Program.cs&lt;/div&gt;]
///       &lt;pre&gt;&lt;code class="language-csharp" data-highlighted="true" tabindex="0" role="group" aria-label="C# code"&gt;
///         &lt;span class="line"&gt;…&lt;/span&gt;  (one per line, joined by \n; also highlighted, diff-add, diff-remove, diff-hunk, focused)
///       &lt;/code&gt;&lt;/pre&gt;
///     &lt;/div&gt;
///     </code>
///     <para>
///         The <c>&lt;pre&gt;</c> is the last child of the wrapper, so a script that inserts after it
///         (the REPL toolbar) lands outside the wrapper. The theme script adds the language label,
///         line numbers and copy button in front of it.
///     </para>
/// </remarks>
public static class CodeBlockHtml
{
	/// <summary>The attribute that marks a <c>&lt;code&gt;</c> element whose content is already final.</summary>
	public const string HighlightedAttribute = "data-highlighted";

	private static readonly Regex _foreignBlock = new(
		"""<pre(?<pre>\s[^>]*)?>(?<lead>\s*)<code(?<code>\s[^>]*)?>(?<content>[^<]*)</code>(?<trail>\s*)</pre>""",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
		TimeSpan.FromSeconds(5));

	private static readonly Regex _languageClass = new(
		"""(?:^|\s)language-(?<lang>[^\s"']+)""",
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(1));

	private static readonly Regex _opaqueElement = new(
		"""<!--.*?-->|<(?<tag>script|style|textarea|template)\b[^>]*>.*?</\k<tag>\s*>""",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline,
		TimeSpan.FromSeconds(5));

	private static readonly Regex _classAttribute = new(
		"""\sclass\s*=\s*(?:"(?<v>[^"]*)"|'(?<v>[^']*)'|(?<v>[^\s>]+))""",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
		TimeSpan.FromSeconds(1));

	/// <summary>
	///     Highlights and wraps every <c>&lt;pre&gt;&lt;code&gt;</c> block in <paramref name="html" /> that
	///     doesn't have the markup yet. The language comes from a <c>language-*</c> class on the
	///     <c>&lt;code&gt;</c> element; the attributes of both elements are kept.
	/// </summary>
	/// <param name="html">A page body.</param>
	/// <returns>The page with every plain code block replaced, or <paramref name="html" /> itself when none was found.</returns>
	/// <remarks>
	///     Blocks whose content holds markup, and blocks already carrying
	///     <see cref="HighlightedAttribute" />, are left as they are.
	/// </remarks>
	public static string EnhanceBlocks(string html) => EnhanceBlocks(html, ThemeText.English);

	/// <summary>
	///     Highlights and wraps every plain <c>&lt;pre&gt;&lt;code&gt;</c> block in <paramref name="html" />, as
	///     <see cref="EnhanceBlocks(string)" /> does, naming each block for screen readers in the site's language
	///     (<c>code.label</c>, <c>code.labelPlain</c>).
	/// </summary>
	/// <param name="html">A page body.</param>
	/// <param name="text">The site's text.</param>
	/// <returns>The page with every plain code block replaced, or <paramref name="html" /> itself when none was found.</returns>
	public static string EnhanceBlocks(string html, ThemeText text)
	{
		ArgumentNullException.ThrowIfNull(text);
		if (!html.Contains("<code", StringComparison.OrdinalIgnoreCase)
		    || !html.Contains("<pre", StringComparison.OrdinalIgnoreCase))
		{
			return html;
		}

		try
		{
			// A <pre><code> written inside a script string or a comment is text, not a block; the
			// markup put in its place would add quotes to the script.
			List<(int Start, int End)> opaque = _opaqueElement.Matches(html).Select(m => (m.Index, m.Index + m.Length)).ToList();
			return _foreignBlock.Replace(html, match =>
			{
				string codeAttributes = match.Groups["code"].Value;
				if (codeAttributes.Contains(HighlightedAttribute, StringComparison.OrdinalIgnoreCase)
				    || opaque.Any(range => match.Index >= range.Start && match.Index < range.End))
				{
					return match.Value;
				}

				string? language = _languageClass.Match(ClassValue(codeAttributes)) is { Success: true } found
					? found.Groups["lang"].Value
					: null;
				string code = WebUtility.HtmlDecode(match.Groups["content"].Value);
				if (code.EndsWith('\n'))
				{
					code = code[..^1];
				}

				var view = new CodeBlockView
				{
					Language = language,
					Text = text,
					PreAttributes = match.Groups["pre"].Value,
					CodeAttributes = codeAttributes,
					Lines = SyntaxHighlighter.HighlightLines(code, language)
						.Select(line => new CodeLine(line, LineMarks.None))
						.ToList()
				};

				var block = new StringBuilder(match.Length * 2);
				Write(block, view);
				return block.ToString();
			});
		}
		catch (RegexMatchTimeoutException)
		{
			return html;
		}
	}

	internal static void Write(StringBuilder html, CodeBlockView view)
	{
		html.Append("<div class=\"code-block\"");
		if (!string.IsNullOrEmpty(view.Language))
		{
			html.Append(" data-lang=\"").Append(Attribute(view.Language)).Append('"');
		}

		if (!string.IsNullOrEmpty(view.Title))
		{
			html.Append(" data-title");
		}

		if (view.LineNumbers is { } lineNumbers)
		{
			html.Append(" data-line-numbers=\"").Append(lineNumbers ? "true" : "false").Append('"');
		}

		if (view.Wrap)
		{
			html.Append(" data-wrap");
		}

		if (view.Diff || view.Lines.Any(line => (line.Marks & (LineMarks.Added | LineMarks.Removed)) != 0))
		{
			html.Append(" data-diff");
		}

		if (view.Lines.Any(line => line.Marks.HasFlag(LineMarks.Focused)))
		{
			html.Append(" data-focus");
		}

		html.Append('>');
		if (!string.IsNullOrEmpty(view.Title))
		{
			html.Append("<div class=\"code-title\">").Append(SyntaxHighlighter.Escape(view.Title)).Append("</div>");
		}

		html.Append("<pre").Append(view.PreAttributes).Append("><code").Append(view.CodeAttributes)
			.Append(' ').Append(HighlightedAttribute).Append("=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"")
			.Append(Attribute(view.Title is { Length: > 0 } title ? title : CodeLanguages.Describe(view.Language, view.Text)))
			.Append("\">");

		for (int i = 0; i < view.Lines.Count; i++)
		{
			if (i > 0)
			{
				html.Append('\n');
			}

			CodeLine line = view.Lines[i];
			html.Append("<span class=\"line");
			AppendLineClasses(html, line.Marks);
			html.Append("\">").Append(line.Html).Append("</span>");
		}

		html.Append("</code></pre></div>");
	}

	internal static string Attribute(string value) => WebUtility.HtmlEncode(value);

	private static void AppendLineClasses(StringBuilder html, LineMarks marks)
	{
		if (marks.HasFlag(LineMarks.Highlighted))
		{
			html.Append(" highlighted");
		}

		if (marks.HasFlag(LineMarks.Added))
		{
			html.Append(" diff-add");
		}

		if (marks.HasFlag(LineMarks.Removed))
		{
			html.Append(" diff-remove");
		}

		if (marks.HasFlag(LineMarks.Hunk))
		{
			html.Append(" diff-hunk");
		}

		if (marks.HasFlag(LineMarks.Focused))
		{
			html.Append(" focused");
		}
	}

	private static string ClassValue(string attributes) =>
		_classAttribute.Match(attributes) is { Success: true } match ? match.Groups["v"].Value : "";
}

internal sealed class CodeBlockView
{
	public string? Language { get; init; }

	public string? Title { get; init; }

	public ThemeText Text { get; init; } = ThemeText.English;

	public bool? LineNumbers { get; init; }

	public bool Wrap { get; init; }

	public bool Diff { get; init; }

	public string PreAttributes { get; init; } = "";

	public string CodeAttributes { get; init; } = "";

	public required IReadOnlyList<CodeLine> Lines { get; init; }
}

internal readonly record struct CodeLine(string Html, LineMarks Marks);

[Flags]
internal enum LineMarks
{
	None = 0,
	Highlighted = 1,
	Added = 2,
	Removed = 4,
	Focused = 8,
	Hunk = 16
}
