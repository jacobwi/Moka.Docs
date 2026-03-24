using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension that renders fenced code blocks with the "blazor-preview" (or "razor-preview")
///     language as preview containers with a <c>data-blazor-preview="true"</c> attribute.
///     The container includes both the syntax-highlighted source code and a placeholder for the
///     live-rendered preview. The REPL/Blazor plugin later enhances these with tabbed UI and
///     server-side rendering.
/// </summary>
/// <remarks>
///     This extension is designed to coexist with <see cref="MermaidExtension" /> and <see cref="ReplExtension" />
///     by wrapping whatever CodeBlock renderer is currently in the pipeline.
///     Registration order: Mermaid first, then BlazorPreview, then REPL.
/// </remarks>
public sealed class BlazorPreviewExtension : IMarkdownExtension
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
            // Find whatever renderer currently handles CodeBlock (could be default
            // CodeBlockRenderer or MermaidCodeBlockRenderer). We wrap it so all
            // code block types are handled correctly.
            IMarkdownObjectRenderer? existingRenderer = null;
            foreach (var r in htmlRenderer.ObjectRenderers)
                if (r is HtmlObjectRenderer<CodeBlock> codeBlockHandler)
                {
                    existingRenderer = codeBlockHandler;
                    break;
                }

            if (existingRenderer != null) htmlRenderer.ObjectRenderers.Remove(existingRenderer);

            htmlRenderer.ObjectRenderers.AddIfNotAlready(
                new BlazorPreviewCodeBlockRenderer(existingRenderer as HtmlObjectRenderer<CodeBlock>));
        }
    }
}

/// <summary>
///     Custom renderer for <see cref="CodeBlock" /> that intercepts Blazor preview fenced code blocks
///     and wraps them in a <c>&lt;div class="blazor-preview-container" data-blazor-preview="true"&gt;</c>.
///     All other code blocks are delegated to the wrapped inner renderer.
/// </summary>
internal sealed class BlazorPreviewCodeBlockRenderer(HtmlObjectRenderer<CodeBlock>? innerRenderer)
    : HtmlObjectRenderer<CodeBlock>
{
    private readonly HtmlObjectRenderer<CodeBlock> _innerRenderer = innerRenderer ?? new CodeBlockRenderer();

    protected override void Write(HtmlRenderer renderer, CodeBlock block)
    {
        if (block is FencedCodeBlock fencedBlock && IsBlazorPreview(fencedBlock))
            WriteBlazorPreviewBlock(renderer, fencedBlock);
        else
            // Delegate to inner renderer (which handles Mermaid + default code blocks)
            _innerRenderer.Write(renderer, block);
    }

    private static bool IsBlazorPreview(FencedCodeBlock block)
    {
        var info = block.Info?.Trim() ?? "";
        return string.Equals(info, "blazor-preview", StringComparison.OrdinalIgnoreCase)
               || string.Equals(info, "razor-preview", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteBlazorPreviewBlock(HtmlRenderer renderer, FencedCodeBlock block)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"blazor-preview-container\" data-blazor-preview=\"true\">");

        // Source code section
        renderer.Write("<div class=\"blazor-preview-source\">");
        renderer.Write("<pre><code class=\"language-razor\">");

        // Write the content of the code block with HTML encoding
        var lines = block.Lines;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines.Lines[i];
            var slice = line.Slice;
            if (i > 0) renderer.WriteLine();

            renderer.WriteEscape(slice.AsSpan());
        }

        renderer.Write("</code></pre>");
        renderer.Write("</div>"); // .blazor-preview-source

        // Render placeholder section
        renderer.Write("<div class=\"blazor-preview-render\"></div>");

        renderer.Write("</div>"); // .blazor-preview-container
        renderer.WriteLine();
    }
}