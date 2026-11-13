using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension that renders <c>csharp-repl</c>, <c>cs-repl</c>, <c>csharp repl</c> and
///     <c>cs repl</c> fenced code blocks as interactive REPL containers with a <c>data-repl="true"</c> attribute.
///     The REPL plugin later enhances these with Run buttons and output panels.
/// </summary>
/// <remarks>
///     This extension is designed to coexist with <see cref="MermaidExtension" /> by wrapping
///     whatever CodeBlock renderer is currently in the pipeline (including MermaidCodeBlockRenderer).
///     Registration order: Mermaid first, then REPL.
/// </remarks>
public sealed class ReplExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No block parser changes needed - we reuse the built-in FencedCodeBlock parser.
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			// Find whatever renderer currently handles CodeBlock (could be default
			// CodeBlockRenderer or MermaidCodeBlockRenderer). We wrap it so both
			// Mermaid and REPL code blocks are handled correctly.
			IMarkdownObjectRenderer? existingRenderer = null;
			foreach (IMarkdownObjectRenderer r in htmlRenderer.ObjectRenderers)
			{
				if (r is HtmlObjectRenderer<CodeBlock> codeBlockHandler)
				{
					existingRenderer = codeBlockHandler;
					break;
				}
			}

			if (existingRenderer != null)
			{
				htmlRenderer.ObjectRenderers.Remove(existingRenderer);
			}

			htmlRenderer.ObjectRenderers.AddIfNotAlready(
				new ReplCodeBlockRenderer(existingRenderer as HtmlObjectRenderer<CodeBlock>));
		}
	}
}

/// <summary>
///     Custom renderer for <see cref="CodeBlock" /> that intercepts C# REPL fenced code blocks
///     and wraps them in a <c>&lt;div class="repl-container" data-repl="true"&gt;</c>.
///     All other code blocks are delegated to the wrapped inner renderer (which may handle
///     Mermaid blocks or be the default <see cref="CodeBlockRenderer" />).
/// </summary>
internal sealed class ReplCodeBlockRenderer(HtmlObjectRenderer<CodeBlock>? innerRenderer)
	: HtmlObjectRenderer<CodeBlock>
{
	private static readonly HashSet<string> _csharpLanguages = new(StringComparer.OrdinalIgnoreCase)
	{
		"csharp", "cs", "c#"
	};

	private readonly HtmlObjectRenderer<CodeBlock> _innerRenderer = innerRenderer ?? new CodeBlockRenderer();

	protected override void Write(HtmlRenderer renderer, CodeBlock block)
	{
		if (block is FencedCodeBlock fencedBlock && IsRepl(fencedBlock))
		{
			WriteReplBlock(renderer, fencedBlock);
		}
		else
			// Delegate to inner renderer (which handles Mermaid + default code blocks)
		{
			_innerRenderer.Write(renderer, block);
		}
	}

	/// <summary>
	///     Whether a fence is a REPL block: <c>csharp-repl</c> or <c>cs-repl</c>, or a C# fence whose
	///     arguments start with the word <c>repl</c> (<c>csharp repl</c>, <c>cs repl title="x"</c>).
	/// </summary>
	internal static bool IsRepl(FencedCodeBlock block)
	{
		string info = block.Info?.Trim() ?? "";
		if (string.Equals(info, "csharp-repl", StringComparison.OrdinalIgnoreCase)
		    || string.Equals(info, "cs-repl", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Markdig splits the info string at the first space, so "csharp repl" arrives as
		// Info "csharp" and Arguments "repl"; comparing Info with "csharp repl" never matched.
		if (!_csharpLanguages.Contains(info))
		{
			return false;
		}

		ReadOnlySpan<char> arguments = (block.Arguments ?? "").AsSpan().TrimStart();
		return arguments.StartsWith("repl", StringComparison.OrdinalIgnoreCase)
		       && (arguments.Length == 4 || char.IsWhiteSpace(arguments[4]));
	}

	private static void WriteReplBlock(HtmlRenderer renderer, FencedCodeBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"repl-container\" data-repl=\"true\">");

		// The plugin's script inserts its toolbar after the <pre>, or before the output when the
		// <pre> has no next sibling. The <pre> ends the code block wrapper, so it lands here.
		renderer.Write(FencedCodeHtml.Render(block, "csharp"));
		renderer.Write("<div class=\"repl-output\" style=\"display:none;\"></div>");
		renderer.Write("</div>");
		renderer.WriteLine();
	}
}
