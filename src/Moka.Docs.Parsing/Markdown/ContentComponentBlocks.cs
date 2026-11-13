using Markdig.Parsers;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     The text on a component's opening line, such as the summary of <c>:::details Summary</c> or the
///     caption of <c>:::figure Caption</c>. It is parsed as inline Markdown, so it can hold code, links,
///     badges and keys.
/// </summary>
public sealed class ComponentLabelBlock : LeafBlock
{
	/// <summary>Creates a label block.</summary>
	/// <param name="parser">The parser that created it.</param>
	public ComponentLabelBlock(BlockParser? parser) : base(parser)
	{
		ProcessInlines = true;
	}
}

/// <summary>
///     A collapsible block, <c>:::details Summary</c>, rendered as <c>&lt;details&gt;</c> with a
///     <c>&lt;summary&gt;</c>.
/// </summary>
public sealed class DetailsBlock(BlockParser parser) : ContainerBlock(parser)
{
	/// <summary>Whether the block starts open (<c>:::details{open}</c>).</summary>
	public bool Open { get; set; }
}

/// <summary>
///     A list of questions, <c>:::faq</c>. Each <c>###</c> heading starts a question whose answer is the
///     content up to the next one; a nested <c>:::details</c> block is a question too.
/// </summary>
public sealed class FaqBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>Side-by-side columns, <c>:::columns</c>, holding one <see cref="ColumnBlock" /> per column.</summary>
public sealed class ColumnsBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>One column of a <see cref="ColumnsBlock" />, <c>:::column</c>.</summary>
public sealed class ColumnBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>
///     A file tree, <c>:::file-tree</c>, written as a nested list. Its list items are read as plain text,
///     so file names keep their underscores and asterisks.
/// </summary>
public sealed class FileTreeBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>
///     A vertical timeline, <c>:::timeline</c>: a list whose items start with a bold date, or <c>###</c>
///     headings holding the dates.
/// </summary>
public sealed class TimelineBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>
///     An embedded video, <c>:::video URL</c>: YouTube, Vimeo or a video file. The block's content, when it
///     has any, is the video's title and caption.
/// </summary>
public sealed class VideoBlock(BlockParser parser) : ContainerBlock(parser)
{
	/// <summary>The URL as written, or rewritten to a site-root path for a file relative to the page.</summary>
	public string Url { get; set; } = "";

	/// <summary>Where the URL is in the Markdown body.</summary>
	public SourceSpan UrlSpan { get; set; } = SourceSpan.Empty;

	/// <summary>The title from <c>{title="..."}</c>, or <c>null</c>.</summary>
	public string? Title { get; set; }

	/// <summary>The image a video file shows before it plays, from <c>{poster="..."}</c>, or <c>null</c>.</summary>
	public string? Poster { get; set; }
}

/// <summary>
///     A figure, <c>:::figure</c>: its first block (usually an image) and a caption from the opening
///     line, the text after the image, or both.
/// </summary>
public sealed class FigureBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>A NuGet install widget, <c>:::package Name [version]</c>.</summary>
public sealed class PackageBlock(BlockParser parser) : ContainerBlock(parser)
{
	/// <summary>The package ID.</summary>
	public string PackageId { get; set; } = "";

	/// <summary>The version to install, or <c>null</c> for the latest.</summary>
	public string? Version { get; set; }
}

/// <summary>
///     The glossary as a definition list, <c>:::glossary</c>. The terms come from the site's glossary,
///     filled in by <see cref="MarkdownParser" />.
/// </summary>
public sealed class GlossaryBlock(BlockParser parser) : ContainerBlock(parser)
{
	/// <summary>The terms and their definitions, sorted by term.</summary>
	public IReadOnlyList<KeyValuePair<string, string>> Terms { get; set; } = [];
}
