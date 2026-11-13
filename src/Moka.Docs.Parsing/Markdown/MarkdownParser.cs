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
/// <remarks>
///     <see cref="Parse(string, string)" /> is safe to call from several threads at once, and the
///     same input always renders the same HTML: tab and code group ids come from the page path
///     and their position, not from a counter.
/// </remarks>
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
	public MarkdownParseResult Parse(string markdown) => Parse(markdown, null);

	/// <summary>
	///     Parses a Markdown file into structured content, resolving its relative links.
	/// </summary>
	/// <param name="markdown">The raw Markdown content (may include YAML front matter).</param>
	/// <param name="sourcePath">
	///     The file's path relative to the docs folder. When given, relative links and images
	///     are rewritten to site-root links; see <see cref="RelativeLinks" />.
	/// </param>
	/// <returns>The fully parsed result.</returns>
	public MarkdownParseResult Parse(string markdown, string? sourcePath) => Parse(markdown, sourcePath, ContentReferences.None);

	/// <summary>
	///     Parses a Markdown file into structured content, resolving its relative links and what it refers to
	///     outside itself: <c>xref:</c> links, glossary terms and package versions.
	/// </summary>
	/// <param name="markdown">The raw Markdown content (may include YAML front matter).</param>
	/// <param name="sourcePath">
	///     The file's path relative to the docs folder, or <c>null</c>. When given, relative links and images
	///     are rewritten to site-root links; see <see cref="RelativeLinks" />.
	/// </param>
	/// <param name="references">
	///     The API types, glossary and package versions the page can refer to. An <c>xref:</c> or a term that
	///     isn't in them is a warning in <see cref="MarkdownParseResult.Warnings" />, with its line.
	/// </param>
	/// <returns>The fully parsed result.</returns>
	public MarkdownParseResult Parse(string markdown, string? sourcePath, ContentReferences references)
	{
		ArgumentNullException.ThrowIfNull(references);

		// Step 1: Extract front matter
		FrontMatterResult fmResult = _frontMatterExtractor.Extract(markdown);

		// Step 2: Parse Markdown body
		MarkdownDocument document = Markdig.Markdown.Parse(fmResult.Body, _pipeline);
		if (sourcePath is not null)
		{
			RelativeLinks.Rewrite(document, sourcePath);
			GroupIds.ScopeToPage(document, sourcePath);
		}

		ContentResolution.Apply(document, references);
		ContentText.Attach(document, references.Text);

		// Step 3: Generate ToC
		TableOfContents toc = _tocGenerator.Generate(document, fmResult.FrontMatter.Title);

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
			FrontMatterError = fmResult.Error,
			Warnings = [.. fmResult.Warnings, .. BodyWarnings(markdown, fmResult.Body, document)],
			Html = html,
			PlainText = plainText,
			TableOfContents = toc
		};
	}

	/// <summary>
	///     A page's Markdown for a reader outside the site: the body without its front matter, under
	///     an <c>h1</c> with <paramref name="title" /> when it doesn't start with one, and with each
	///     Markdown link and image that is relative to the file or to the site root pointing at
	///     <paramref name="siteRoot" /> instead. Everything else stays as written, MokaDocs blocks,
	///     includes and raw HTML too.
	/// </summary>
	/// <param name="markdown">The file's content, front matter included.</param>
	/// <param name="sourcePath">The file's path relative to the docs folder.</param>
	/// <param name="title">The page title.</param>
	/// <param name="siteRoot">
	///     What a link from the site root gets in front of it: the site's URL with its base path, such
	///     as <c>https://example.com/docs</c>, else the base path alone, or an empty string.
	/// </param>
	/// <returns>The Markdown, with <c>\n</c> line endings.</returns>
	public string ToStandaloneMarkdown(string markdown, string sourcePath, string title, string siteRoot) =>
		ToStandaloneMarkdown(markdown, sourcePath, title, siteRoot, null);

	/// <summary>
	///     A page's Markdown for a reader outside the site, as
	///     <see cref="ToStandaloneMarkdown(string, string, string, string)" /> makes it, with each <c>xref:</c>
	///     link that <paramref name="resolveXref" /> resolves pointing at the API page instead:
	///     <c>[text](xref:Ns.Type)</c> keeps its text and <c>&lt;xref:Ns.Type&gt;</c> becomes a link named after
	///     the type. References that don't resolve stay as written.
	/// </summary>
	/// <param name="markdown">The file's content, front matter included.</param>
	/// <param name="sourcePath">The file's path relative to the docs folder.</param>
	/// <param name="title">The page title.</param>
	/// <param name="siteRoot">What a link from the site root gets in front of it.</param>
	/// <param name="resolveXref">Resolves an <c>xref:</c> reference, or <c>null</c> to leave them all as written.</param>
	/// <returns>The Markdown, with <c>\n</c> line endings.</returns>
	public string ToStandaloneMarkdown(string markdown, string sourcePath, string title, string siteRoot,
		Func<string, XrefTarget?>? resolveXref)
	{
		string body = _frontMatterExtractor.Extract(markdown).Body;
		MarkdownDocument document = Markdig.Markdown.Parse(body, _pipeline);

		var edits = new List<(int Start, int Length, string Url)>();
		foreach (LinkInline link in document.Descendants<LinkInline>())
		{
			if (!link.IsAutoLink && link.Reference is null && link.UrlSpan is { IsEmpty: false } span)
			{
				if (ContentResolution.IsXref(link.Url))
				{
					AddXrefEdit(edits, body, span, link.Url!, siteRoot, resolveXref, false);
					continue;
				}

				AddLinkEdit(edits, body, span, link.Url, sourcePath, siteRoot);
			}
		}

		foreach (LinkReferenceDefinition definition in document.Descendants<LinkReferenceDefinition>())
		{
			if (definition.UrlSpan is { IsEmpty: false } span)
			{
				if (ContentResolution.IsXref(definition.Url))
				{
					AddXrefEdit(edits, body, span, definition.Url!, siteRoot, resolveXref, false);
					continue;
				}

				AddLinkEdit(edits, body, span, definition.Url, sourcePath, siteRoot);
			}
		}

		foreach (AutolinkInline autolink in document.Descendants<AutolinkInline>())
		{
			if (!autolink.IsEmail && ContentResolution.IsXref(autolink.Url) && !autolink.Span.IsEmpty)
			{
				AddXrefEdit(edits, body, autolink.Span, autolink.Url, siteRoot, resolveXref, true);
			}
		}

		// A video file next to the page plays from the site, like an image.
		foreach (VideoBlock video in document.Descendants<VideoBlock>())
		{
			if (!video.UrlSpan.IsEmpty)
			{
				AddLinkEdit(edits, body, video.UrlSpan, video.Url, sourcePath, siteRoot);
			}
		}

		var sb = new StringBuilder(body);
		foreach ((int start, int length, string url) in edits.OrderByDescending(e => e.Start))
		{
			sb.Remove(start, length).Insert(start, url);
		}

		string standalone = sb.ToString().Trim();
		if (document.FirstOrDefault(b => b is not LinkReferenceDefinitionGroup) is not HeadingBlock { Level: 1 })
		{
			standalone = $"# {title}\n\n{standalone}";
		}

		return standalone.ReplaceLineEndings("\n").TrimEnd() + "\n";
	}

	#region Private Helpers

	// A resolved xref: the link's URL made absolute, or an <xref:...> autolink turned into [Name](url).
	private static void AddXrefEdit(List<(int Start, int Length, string Url)> edits, string body, SourceSpan span,
		string url, string siteRoot, Func<string, XrefTarget?>? resolveXref, bool autolink)
	{
		if (resolveXref?.Invoke(ContentResolution.Reference(url)) is not { } target || span.End >= body.Length)
		{
			return;
		}

		string absolute = target.Url.StartsWith('/') ? siteRoot + target.Url : target.Url;
		string text = target.Text.Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");
		edits.Add((span.Start, span.Length, autolink ? $"[{text}]({absolute})" : absolute));
	}

	// Body warnings carry the line within the body; the file's line adds the front matter's lines.
	private static IEnumerable<string> BodyWarnings(string markdown, string body, MarkdownDocument document)
	{
		IReadOnlyList<(int Line, string Message)> warnings = MarkdownWarnings.Of(document);
		if (warnings.Count == 0)
		{
			return [];
		}

		int bodyStart = Math.Max(0, markdown.Length - body.Length);
		int offset = markdown.AsSpan(0, bodyStart).Count('\n');
		return warnings.OrderBy(w => w.Line).Select(w => $"{w.Message} (line {offset + w.Line + 1})");
	}

	private static void AddLinkEdit(List<(int Start, int Length, string Url)> edits, string body, SourceSpan span,
		string? url, string sourcePath, string siteRoot)
	{
		if (string.IsNullOrEmpty(url) || span.End >= body.Length)
		{
			return;
		}

		string? fromRoot = url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)
			? url
			: RelativeLinks.ToRootRelative(url, sourcePath);
		if (fromRoot is null)
		{
			return;
		}

		bool bracketed = body[span.Start] == '<';
		string absolute = siteRoot + fromRoot;
		edits.Add((span.Start, span.Length, bracketed ? $"<{absolute}>" : absolute));
	}

	private static MarkdownPipeline BuildPipeline(MarkdownParserOptions options)
	{
		MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions() // Tables, footnotes, task lists, auto-links, etc.
			.UseEmojiAndSmiley();

		// No UseYamlFrontMatter: FrontMatterExtractor has already taken the front matter off, and
		// Markdig's version would read a body that opens with a --- rule as a second YAML block
		// and drop everything up to the next ---.

		// Admonitions (::: note, ::: tip Custom Title, ...). This must be registered
		// rather than left to Markdig's built-in CustomContainers, which renders
		// ::: tip Hot Tip as a bare <div class="tip"> and silently drops the title.
		if (options.EnableAdmonitions)
		{
			builder.Extensions.Add(new AdmonitionExtension());
		}

		// Tabbed content (=== "Tab title"). Without this, the === lines fall through
		// to CommonMark and render as literal paragraph text.
		if (options.EnableTabs)
		{
			builder.Extensions.Add(new TabbedContentExtension());
		}

		// UI Components (card, steps, link-cards, code-group)
		builder.Extensions.Add(new ComponentExtension());

		// Heading ids come from the AutoIdentifiers extension that UseAdvancedExtensions adds
		// with Markdig's default options. A UseAutoIdentifiers(GitHub) call used to sit here, but
		// Markdig only adds an extension once, so it never applied. Switching to GitHub-style ids
		// now would change the anchors of existing pages.

		// Highlighted code blocks with fence options. Mermaid, Blazor preview and REPL wrap its
		// renderer, so it comes first.
		builder.Extensions.AddIfNotAlready<CodeBlockExtension>();

		// Mermaid diagram support - renders ```mermaid blocks as <pre class="mermaid">
		builder.Extensions.AddIfNotAlready<MermaidExtension>();

		// Blazor preview support - renders ```blazor-preview blocks as preview containers
		builder.Extensions.AddIfNotAlready<BlazorPreviewExtension>();

		// REPL support - renders ```csharp-repl blocks as interactive containers
		builder.Extensions.AddIfNotAlready<ReplExtension>();

		// Changelog support - renders :::changelog blocks as rich timeline UI
		builder.Extensions.Add(new ChangelogExtension());

		return builder.Build();
	}

	private static string ExtractPlainText(MarkdownDocument document)
	{
		var sb = new StringBuilder();

		foreach (Block block in document.AllDescendants())
		{
			ContainerInline? inline = block switch
			{
				ParagraphBlock paragraph => paragraph.Inline,
				HeadingBlock heading => heading.Inline,
				ComponentLabelBlock label => label.Inline,
				_ => null
			};

			if (inline is not null)
			{
				InlineText.Append(sb, inline, true);
				sb.AppendLine();
				continue;
			}

			string? text = block switch
			{
				ParagraphBlock { Inline: null, ProcessInlines: false } entry => FileTreeText(entry),
				PackageBlock { PackageId.Length: > 0 } package => package.PackageId,
				GlossaryBlock glossary => string.Join(Environment.NewLine, glossary.Terms.Select(t => $"{t.Key} {t.Value}")),
				VideoBlock { Title: { } title } => title,
				_ => null
			};

			if (!string.IsNullOrEmpty(text))
			{
				sb.AppendLine(text);
			}
		}

		return sb.ToString().Trim();
	}

	private static string FileTreeText(ParagraphBlock entry)
	{
		FileTreeEntry parsed = FileTreeEntry.Parse(FileTreeEntry.RawText(entry));
		return $"{parsed.Name} {parsed.Comment}".Trim();
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

	/// <summary>Why the front matter block could not be read, or <c>null</c> when it was fine or absent.</summary>
	public string? FrontMatterError { get; init; }

	/// <summary>
	///     Problems that did not stop the page from building, such as an unknown front matter key
	///     or a front matter block without its closing <c>---</c>. Messages do not name the file.
	/// </summary>
	public IReadOnlyList<string> Warnings { get; init; } = [];
}
