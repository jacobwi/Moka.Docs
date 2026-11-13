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
		// No block parser changes needed - we reuse the built-in FencedCodeBlock parser.
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			// Wrap whichever renderer handles code blocks now (Markdig's, or CodeBlockExtension's)
			// and delegate everything that isn't a mermaid block to it.
			var existingRenderer =
				htmlRenderer.ObjectRenderers.FirstOrDefault(r => r is HtmlObjectRenderer<CodeBlock>) as HtmlObjectRenderer<CodeBlock>;
			if (existingRenderer != null)
			{
				htmlRenderer.ObjectRenderers.Remove(existingRenderer);
			}

			htmlRenderer.ObjectRenderers.AddIfNotAlready(new MermaidCodeBlockRenderer(existingRenderer));
		}
	}
}

/// <summary>
///     Custom renderer for <see cref="CodeBlock" /> that intercepts mermaid fenced code blocks
///     and renders them as <c>&lt;pre class="mermaid"&gt;</c>. All other code blocks are
///     delegated to the renderer it wraps.
/// </summary>
internal sealed class MermaidCodeBlockRenderer(HtmlObjectRenderer<CodeBlock>? defaultRenderer) : HtmlObjectRenderer<CodeBlock>
{
	private readonly HtmlObjectRenderer<CodeBlock> _defaultRenderer = defaultRenderer ?? new CodeBlockRenderer();

	protected override void Write(HtmlRenderer renderer, CodeBlock block)
	{
		if (block is FencedCodeBlock fencedBlock && IsMermaid(fencedBlock))
		{
			WriteMermaidBlock(renderer, fencedBlock);
		}
		else
			// Delegate to the default renderer for all non-mermaid code blocks
		{
			_defaultRenderer.Write(renderer, block);
		}
	}

	private static bool IsMermaid(FencedCodeBlock block) =>
		string.Equals(block.Info, "mermaid", StringComparison.OrdinalIgnoreCase);

	private static void WriteMermaidBlock(HtmlRenderer renderer, FencedCodeBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<pre class=\"mermaid\">");

		// HTML-encoded. Mermaid decodes entities before parsing, so the diagram still sees
		// "-->" and "List<string>". Written raw, the browser parsed "<string>" as a tag
		// before Mermaid ever saw it, which broke any diagram with generics or <<interface>>.
		StringLineGroup lines = block.Lines;
		for (int i = 0; i < lines.Count; i++)
		{
			StringLine line = lines.Lines[i];
			StringSlice slice = line.Slice;
			if (i > 0)
			{
				renderer.WriteLine();
			}

			renderer.WriteEscape(slice.AsSpan());
		}

		renderer.Write("</pre>");
		renderer.WriteLine();
	}
}
