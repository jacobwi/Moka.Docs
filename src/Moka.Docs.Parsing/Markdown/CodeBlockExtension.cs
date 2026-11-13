using System.Text;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Moka.Docs.Parsing.Highlighting;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension that renders code blocks highlighted at build time, in the markup
///     <see cref="CodeBlockHtml" /> describes, and reads the fence options: <c>title="…"</c>, line
///     ranges such as <c>{1,3-5}</c>, <c>showLineNumbers</c>, <c>noLineNumbers</c>, <c>wrap</c>,
///     <c>[!code …]</c> markers in comments and <c>diff</c> / <c>diff-&lt;language&gt;</c> blocks.
/// </summary>
/// <remarks>
///     It replaces Markdig's <see cref="CodeBlockRenderer" />, so register it before the
///     extensions that wrap the code block renderer (<see cref="MermaidExtension" />,
///     <see cref="BlazorPreviewExtension" />, <see cref="ReplExtension" />).
/// </remarks>
public sealed class CodeBlockExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is not HtmlRenderer htmlRenderer)
		{
			return;
		}

		IMarkdownObjectRenderer? existing = htmlRenderer.ObjectRenderers.FirstOrDefault(r => r is HtmlObjectRenderer<CodeBlock>);
		if (existing is not null)
		{
			htmlRenderer.ObjectRenderers.Remove(existing);
		}

		htmlRenderer.ObjectRenderers.AddIfNotAlready(new HighlightedCodeBlockRenderer());
	}
}

internal sealed class HighlightedCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
	protected override void Write(HtmlRenderer renderer, CodeBlock block)
	{
		renderer.EnsureLine();
		if (!renderer.EnableHtmlForBlock)
		{
			renderer.WriteLeafRawLines(block, true, true);
			return;
		}

		renderer.Write(FencedCodeHtml.Render(block));
		renderer.WriteLine();
	}
}

internal static class FencedCodeHtml
{
	private const string _languagePrefix = "language-";

	// language overrides the fence's own: the REPL writes csharp-repl blocks as csharp.
	public static string Render(CodeBlock block, string? language = null)
	{
		CodeFenceOptions options = block is FencedCodeBlock fenced
			? CodeFenceOptions.Parse(fenced.Info, fenced.Arguments)
			: CodeFenceOptions.None;
		if (language is not null)
		{
			options = options with { Language = language };
		}

		bool diff = options.IsDiff;
		string? codeLanguage = diff ? options.DiffLanguage : options.Language;

		int count = block.Lines.Count;
		string[] texts = new string[count];
		var marks = new LineMarks[count];
		for (int i = 0; i < count; i++)
		{
			(texts[i], marks[i]) = CodeFenceLines.StripMarkers(block.Lines.Lines[i].Slice.ToString());
		}

		if (diff)
		{
			CodeFenceLines.SplitDiff(texts, marks);
		}

		var lines = new List<CodeLine>(count);
		if (count > 0)
		{
			string code = string.Join('\n', texts.Select((text, i) => marks[i].HasFlag(LineMarks.Hunk) ? "" : text));
			IReadOnlyList<string> html = SyntaxHighlighter.HighlightLines(code, codeLanguage);
			for (int i = 0; i < count; i++)
			{
				LineMarks lineMarks = marks[i] | (options.IsHighlighted(i + 1) ? LineMarks.Highlighted : LineMarks.None);
				string lineHtml = lineMarks.HasFlag(LineMarks.Hunk)
					? $"<span class=\"token comment\">{SyntaxHighlighter.Escape(texts[i])}</span>"
					: html[i];
				lines.Add(new CodeLine(lineHtml, lineMarks));
			}
		}

		var view = new CodeBlockView
		{
			Language = diff ? codeLanguage ?? "diff" : options.Language,
			Text = ContentText.Of(block),
			Title = block.Parent is CodeGroupBlock ? null : options.Title,
			LineNumbers = options.LineNumbers,
			Wrap = options.Wrap,
			Diff = diff,
			CodeAttributes = CodeAttributes(block, options.Language),
			Lines = lines
		};

		var output = new StringBuilder();
		CodeBlockHtml.Write(output, view);
		return output.ToString();
	}

	// The fence parser has already put language-{info} in the classes, with any {1,3} still
	// attached; it is replaced by the cleaned language. Generic attributes ({#id .class}) stay.
	private static string CodeAttributes(CodeBlock block, string? language)
	{
		var html = new StringBuilder();
		HtmlAttributes? attributes = block.TryGetAttributes();
		if (attributes?.Id is { Length: > 0 } id)
		{
			html.Append(" id=\"").Append(CodeBlockHtml.Attribute(id)).Append('"');
		}

		var classes = new List<string>();
		if (language is not null)
		{
			classes.Add(_languagePrefix + language);
		}

		if (attributes?.Classes is { } existing)
		{
			classes.AddRange(existing.Where(c => !c.StartsWith(_languagePrefix, StringComparison.Ordinal)));
		}

		if (classes.Count > 0)
		{
			html.Append(" class=\"").Append(CodeBlockHtml.Attribute(string.Join(' ', classes))).Append('"');
		}

		if (attributes?.Properties is { } properties)
		{
			foreach (KeyValuePair<string, string?> property in properties)
			{
				html.Append(' ').Append(property.Key);
				if (property.Value is not null)
				{
					html.Append("=\"").Append(CodeBlockHtml.Attribute(property.Value)).Append('"');
				}
			}
		}

		return html.ToString();
	}
}
