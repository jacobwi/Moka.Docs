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

	/// <summary>
	///     The people who committed changes to the page's source file, most commits first. Read
	///     from git only when <c>theme.options.showContributors</c> is on; empty otherwise and for
	///     generated pages.
	/// </summary>
	public IReadOnlyList<PageContributor> Contributors { get; init; } = [];

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

	/// <summary>
	///     Feature cards the <c>landing</c> layout shows under its hero section, in order. The
	///     layout leaves the whole section out when this is empty.
	/// </summary>
	public List<LandingFeature> Features { get; init; } = [];

	/// <summary>Heading above the landing layout's feature cards, or empty for none.</summary>
	public string FeaturesTitle { get; init; } = "";

	/// <summary>Text under <see cref="FeaturesTitle" />, or empty for none.</summary>
	public string FeaturesSubtitle { get; init; } = "";

	/// <summary>
	///     A code sample the <c>landing</c> layout shows beside its hero text when
	///     <c>theme.options.layout.hero</c> is <c>split</c>. <c>null</c> for none.
	/// </summary>
	public string? HeroCode { get; init; }

	/// <summary>The language <see cref="HeroCode" /> is highlighted as, such as <c>csharp</c>; <c>null</c> for plain text.</summary>
	public string? HeroCodeLanguage { get; init; }

	/// <summary>
	///     An image the <c>landing</c> layout shows beside its hero text when
	///     <c>theme.options.layout.hero</c> is <c>split</c> and there is no <see cref="HeroCode" />: an
	///     absolute URL, a path from the site root, or a path relative to the page's file. <c>null</c>
	///     for none.
	/// </summary>
	public string? HeroImage { get; init; }

	/// <summary>
	///     A code sample the <c>landing</c> layout shows in a band of its own, beside
	///     <see cref="ShowcaseTitle" /> and <see cref="ShowcaseText" />, when
	///     <c>theme.options.layout.home</c> is <c>showcase</c>. <c>null</c> for none.
	/// </summary>
	public string? ShowcaseCode { get; init; }

	/// <summary>The language <see cref="ShowcaseCode" /> is highlighted as, such as <c>csharp</c>; <c>null</c> for plain text.</summary>
	public string? ShowcaseCodeLanguage { get; init; }

	/// <summary>The heading of the showcase code band, or empty for none.</summary>
	public string ShowcaseTitle { get; init; } = "";

	/// <summary>The text under <see cref="ShowcaseTitle" /> in the showcase code band, or empty for none.</summary>
	public string ShowcaseText { get; init; } = "";

	/// <summary>
	///     Figures the <c>landing</c> layout shows in a row when <c>theme.options.layout.home</c> is
	///     <c>showcase</c>, in order. The row is left out when this is empty.
	/// </summary>
	public List<LandingStat> Stats { get; init; } = [];

	/// <summary>
	///     The call to action the <c>landing</c> layout shows in a band near the end of the page when
	///     <c>theme.options.layout.home</c> is <c>showcase</c>, or <c>null</c> for none.
	/// </summary>
	public LandingCallToAction? Cta { get; init; }

	/// <summary>
	///     The publication date of a blog post, from an ISO 8601 date or date and time. A date
	///     without a time is midnight UTC. <c>null</c> when the page has none or it could not be read.
	/// </summary>
	public DateTimeOffset? Date { get; init; }

	/// <summary>
	///     The authors of a blog post as written in <c>author</c> or <c>authors</c>: keys of
	///     <c>features.blog.authors</c>, or names shown as written.
	/// </summary>
	public List<string> Authors { get; init; } = [];

	/// <summary>A blog post's summary for the index page and feeds, or <c>null</c> to use its first paragraph.</summary>
	public string? Summary { get; init; }

	/// <summary>
	///     A blog post's cover image: an absolute URL, a path from the site root, or a path
	///     relative to the post's file. <c>null</c> for none.
	/// </summary>
	public string? Image { get; init; }
}

/// <summary>
///     One card from the <c>features</c> front matter list of a <c>landing</c> layout page.
///     Every field is optional; the layout leaves out the parts that are empty.
/// </summary>
public sealed record LandingFeature
{
	/// <summary>The card heading.</summary>
	public string Title { get; init; } = "";

	/// <summary>The text under the heading.</summary>
	public string Description { get; init; } = "";

	/// <summary>
	///     A Lucide icon name, or short text such as <c>C#</c> that is shown when the name is not
	///     in the icon set. <c>null</c> for no icon.
	/// </summary>
	public string? Icon { get; init; }

	/// <summary>The URL the card links to, or <c>null</c> when the card is not a link.</summary>
	public string? Link { get; init; }
}

/// <summary>
///     One figure from the <c>stats</c> front matter list of a <c>landing</c> layout page, such as
///     <c>2M</c> downloads. Either field may be empty, not both.
/// </summary>
public sealed record LandingStat
{
	/// <summary>The figure, shown large, such as <c>2M</c> or <c>99.9%</c>.</summary>
	public string Value { get; init; } = "";

	/// <summary>What the figure counts, shown under it.</summary>
	public string Label { get; init; } = "";
}

/// <summary>
///     The <c>cta</c> front matter of a <c>landing</c> layout page: a closing band with a heading, a
///     line of text and a button. Every field is optional; the layout leaves out the parts that are empty.
/// </summary>
public sealed record LandingCallToAction
{
	/// <summary>The band's heading.</summary>
	public string Title { get; init; } = "";

	/// <summary>The text under the heading.</summary>
	public string Text { get; init; } = "";

	/// <summary>The URL the button links to, or <c>null</c> for no button.</summary>
	public string? Link { get; init; }

	/// <summary>The button's text, or <c>null</c> for the layout's default.</summary>
	public string? LinkText { get; init; }
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
