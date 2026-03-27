namespace Moka.Docs.Core.Content;

/// <summary>
///     Represents a single documentation page, whether from Markdown or auto-generated from API docs.
/// </summary>
public sealed record DocPage
{
	/// <summary>Front matter metadata for the page.</summary>
	public required FrontMatter FrontMatter { get; init; }

	/// <summary>The parsed content body (HTML).</summary>
	public required PageContent Content { get; set; }

	/// <summary>Auto-generated table of contents from headings.</summary>
	public TableOfContents TableOfContents { get; init; } = TableOfContents.Empty;

	/// <summary>The source file path (relative to docs root), or null for generated pages.</summary>
	public string? SourcePath { get; init; }

	/// <summary>The output URL route for this page.</summary>
	public required string Route { get; init; }

	/// <summary>The origin of this page.</summary>
	public PageOrigin Origin { get; init; } = PageOrigin.Markdown;

	/// <summary>Last modified timestamp, if available.</summary>
	public DateTimeOffset? LastModified { get; init; }

	/// <inheritdoc />
	public override string ToString() => $"DocPage({Route}, {FrontMatter.Title})";
}

/// <summary>
///     Indicates where a page was sourced from.
/// </summary>
public enum PageOrigin
{
	/// <summary>Page was authored as a Markdown file.</summary>
	Markdown,

	/// <summary>Page was auto-generated from C# API metadata.</summary>
	ApiGenerated
}

/// <summary>
///     Front matter metadata extracted from YAML at the top of a Markdown file.
/// </summary>
public sealed record FrontMatter
{
	/// <summary>Page title.</summary>
	public required string Title { get; init; }

	/// <summary>Page description for meta tags.</summary>
	public string Description { get; init; } = "";

	/// <summary>Sort order within its parent section.</summary>
	public int Order { get; init; }

	/// <summary>Icon name for sidebar display.</summary>
	public string? Icon { get; init; }

	/// <summary>Layout template to use.</summary>
	public string Layout { get; init; } = "default";

	/// <summary>Tags for categorization and search.</summary>
	public List<string> Tags { get; init; } = [];

	/// <summary>Page visibility.</summary>
	public PageVisibility Visibility { get; init; } = PageVisibility.Public;

	/// <summary>Whether to show a table of contents.</summary>
	public bool Toc { get; init; } = true;

	/// <summary>Whether this section is expanded in the sidebar by default.</summary>
	public bool Expanded { get; init; } = true;

	/// <summary>Custom URL slug override.</summary>
	public string? Route { get; init; }

	/// <summary>Version range this page applies to (e.g., ">=2.0").</summary>
	public string? Version { get; init; }

	/// <summary>Feature flag required for this page to be included in the build.</summary>
	public string? Requires { get; init; }
}

/// <summary>
///     Page visibility control.
/// </summary>
public enum PageVisibility
{
	/// <summary>Page is visible in navigation and search.</summary>
	Public,

	/// <summary>Page is accessible by URL but hidden from navigation.</summary>
	Hidden,

	/// <summary>Page is only included when building with --draft flag.</summary>
	Draft
}

/// <summary>
///     The parsed body content of a page.
/// </summary>
public sealed record PageContent
{
	/// <summary>The rendered HTML body.</summary>
	public required string Html { get; init; }

	/// <summary>Plain text content (for search indexing).</summary>
	public string PlainText { get; init; } = "";

	/// <summary>Creates empty content.</summary>
	public static PageContent Empty => new() { Html = "" };
}

/// <summary>
///     Auto-generated table of contents from page headings.
/// </summary>
public sealed record TableOfContents
{
	/// <summary>The heading entries in order.</summary>
	public IReadOnlyList<TocEntry> Entries { get; init; } = [];

	/// <summary>Empty table of contents.</summary>
	public static TableOfContents Empty => new();
}

/// <summary>
///     A single entry in the table of contents.
/// </summary>
public sealed record TocEntry
{
	/// <summary>The heading level (1-6).</summary>
	public required int Level { get; init; }

	/// <summary>The heading text.</summary>
	public required string Text { get; init; }

	/// <summary>The anchor ID for linking.</summary>
	public required string Id { get; init; }

	/// <summary>Child entries (sub-headings).</summary>
	public List<TocEntry> Children { get; init; } = [];
}
