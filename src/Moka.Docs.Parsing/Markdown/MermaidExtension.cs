using Markdig;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension that renders fenced code blocks with the "mermaid" language
///     as <c>&lt;pre class="mermaid"&gt;</c> elements for client-side Mermaid.js rendering.
/// </summary>
public sealed class MermaidExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No block parser changes needed — we reuse the built-in FencedCodeBlock parser.
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			// Replace the default CodeBlockRenderer with our wrapper that
			// handles mermaid blocks specially and delegates everything else.
			CodeBlockRenderer? defaultRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
			if (defaultRenderer != null)
			{
				htmlRenderer.ObjectRenderers.Remove(defaultRenderer);
			}

			htmlRenderer.ObjectRenderers.AddIfNotAlready(new MermaidCodeBlockRenderer(defaultRenderer));
		}
	}
}

/// <summary>
///     Custom renderer for <see cref="CodeBlock" /> that intercepts mermaid fenced code blocks
///     and renders them as <c>&lt;pre class="mermaid"&gt;</c>. All other code blocks are
///     delegated to the default <see cref="CodeBlockRenderer" />.
/// </summary>
internal sealed class MermaidCodeBlockRenderer(CodeBlockRenderer? defaultRenderer) : HtmlObjectRenderer<CodeBlock>
{
	private readonly CodeBlockRenderer? _defaultRenderer = defaultRenderer ?? new CodeBlockRenderer();

	protected override void Write(HtmlRenderer renderer, CodeBlock block)
	{
		if (block is FencedCodeBlock fencedBlock && IsMermaid(fencedBlock))
		{
			WriteMermaidBlock(renderer, fencedBlock);
		}
		else
			// Delegate to the default renderer for all non-mermaid code blocks
		{
			_defaultRenderer!.Write(renderer, block);
		}
	}

	private static bool IsMermaid(FencedCodeBlock block) =>
		string.Equals(block.Info, "mermaid", StringComparison.OrdinalIgnoreCase);

	private static void WriteMermaidBlock(HtmlRenderer renderer, FencedCodeBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<pre class=\"mermaid\">");

		// Write the raw content of the code block without HTML encoding,
		// as Mermaid.js needs the raw diagram syntax.
		StringLineGroup lines = block.Lines;
		for (int i = 0; i < lines.Count; i++)
		{
			StringLine line = lines.Lines[i];
			StringSlice slice = line.Slice;
			if (i > 0)
			{
				renderer.WriteLine();
			}

			renderer.Write(slice.AsSpan());
		}

		renderer.Write("</pre>");
		renderer.WriteLine();
	}
}
