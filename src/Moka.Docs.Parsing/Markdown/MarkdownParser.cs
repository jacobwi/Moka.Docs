using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Parses a Markdown file into a <see cref="MarkdownParseResult" /> containing
///     front matter, rendered HTML, plain text, and a table of contents.
/// </summary>
public sealed class MarkdownParser
{
	private readonly FrontMatterExtractor _frontMatterExtractor;
	private readonly MarkdownPipeline _pipeline;
	private readonly TocGenerator _tocGenerator;

	/// <summary>
	///     Creates a new Markdown parser with all MokaDocs extensions configured.
	/// </summary>
	/// <param name="options">Optional parser configuration.</param>
	public MarkdownParser(MarkdownParserOptions? options = null)
	{
		options ??= new MarkdownParserOptions();
		_frontMatterExtractor = new FrontMatterExtractor();
		_tocGenerator = new TocGenerator();
		_pipeline = BuildPipeline(options);
	}

	/// <summary>
	///     Parses a Markdown string into structured content.
	/// </summary>
	/// <param name="markdown">The raw Markdown content (may include YAML front matter).</param>
	/// <returns>The fully parsed result.</returns>
	public MarkdownParseResult Parse(string markdown)
	{
		// Step 1: Extract front matter
		FrontMatterResult fmResult = _frontMatterExtractor.Extract(markdown);

		// Step 2: Parse Markdown body
		MarkdownDocument document = Markdig.Markdown.Parse(fmResult.Body, _pipeline);

		// Step 3: Generate ToC
		TableOfContents toc = _tocGenerator.Generate(document);

		// Step 4: Render HTML
		using var writer = new StringWriter();
		var renderer = new HtmlRenderer(writer);
		_pipeline.Setup(renderer);
		renderer.Render(document);
		writer.Flush();
		string html = writer.ToString();

		// Step 5: Extract plain text (for search indexing)
		string plainText = ExtractPlainText(document);

		return new MarkdownParseResult
		{
			FrontMatter = fmResult.FrontMatter,
			Html = html,
			PlainText = plainText,
			TableOfContents = toc
		};
	}

	#region Private Helpers

	private static MarkdownPipeline BuildPipeline(MarkdownParserOptions options)
	{
		MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions() // Tables, footnotes, task lists, auto-links, etc.
			.UseYamlFrontMatter()
			.UseEmojiAndSmiley();

		// Admonitions use Markdig's built-in CustomContainers extension
		// (included via UseAdvancedExtensions) which renders ::: note → <div class="note">
		// Our CSS styles .note, .tip, .warning, .danger, .info, .caution, .important

		// UI Components (card, steps, link-cards, code-group)
		builder.Extensions.Add(new ComponentExtension());

		// Auto-generate IDs on headings
		builder.UseAutoIdentifiers(AutoIdentifierOptions.GitHub);

		// Mermaid diagram support — renders ```mermaid blocks as <pre class="mermaid">
		builder.Extensions.AddIfNotAlready<MermaidExtension>();

		// Blazor preview support — renders ```blazor-preview blocks as preview containers
		builder.Extensions.AddIfNotAlready<BlazorPreviewExtension>();

		// REPL support — renders ```csharp-repl blocks as interactive containers
		builder.Extensions.AddIfNotAlready<ReplExtension>();

		// Changelog support — renders :::changelog blocks as rich timeline UI
		builder.Extensions.Add(new ChangelogExtension());

		return builder.Build();
	}

	private static string ExtractPlainText(MarkdownDocument document)
	{
		var sb = new StringBuilder();

		foreach (Block block in document.AllDescendants())
		{
			if (block is ParagraphBlock paragraph && paragraph.Inline is not null)
			{
				foreach (Inline inline in paragraph.Inline)
				{
					AppendInlineText(sb, inline);
				}

				sb.AppendLine();
			}
			else if (block is HeadingBlock heading && heading.Inline is not null)
			{
				foreach (Inline inline in heading.Inline)
				{
					AppendInlineText(sb, inline);
				}

				sb.AppendLine();
			}
		}

		return sb.ToString().Trim();
	}

	private static void AppendInlineText(StringBuilder sb, Inline inline)
	{
		switch (inline)
		{
			case LiteralInline literal:
				sb.Append(literal.Content);
				break;
			case CodeInline code:
				sb.Append(code.Content);
				break;
			case ContainerInline container:
				foreach (Inline child in container)
				{
					AppendInlineText(sb, child);
				}

				break;
			case LineBreakInline:
				sb.Append(' ');
				break;
		}
	}

	#endregion
}

/// <summary>
///     Configuration options for the Markdown parser.
/// </summary>
public sealed class MarkdownParserOptions
{
	/// <summary>Whether to enable admonition/callout blocks. Default: true.</summary>
	public bool EnableAdmonitions { get; init; } = true;

	/// <summary>Whether to enable tabbed content blocks. Default: true.</summary>
	public bool EnableTabs { get; init; } = true;
}

/// <summary>
///     The result of parsing a Markdown file.
/// </summary>
public sealed record MarkdownParseResult
{
	/// <summary>The parsed front matter metadata.</summary>
	public required Core.Content.FrontMatter FrontMatter { get; init; }

	/// <summary>The rendered HTML content.</summary>
	public required string Html { get; init; }

	/// <summary>The plain text content (for search indexing).</summary>
	public required string PlainText { get; init; }

	/// <summary>The auto-generated table of contents.</summary>
	public required TableOfContents TableOfContents { get; init; }
}
