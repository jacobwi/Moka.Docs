using Moka.Docs.Core.Theming;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     The layout variant of each part of the pages, the counterpart of <c>theme.options.layout</c> in
///     mokadocs.yaml. Every part defaults to <c>"default"</c>, the theme's original layout. Names are
///     matched without regard to case; an unknown name renders as <c>"default"</c>, and the build logs
///     a warning.
/// </summary>
/// <remarks>
///     Readers can't switch layouts unless <see cref="MokaDocsOptions.LayoutSelector" /> is on; these
///     are then the defaults of its Layout section.
/// </remarks>
public sealed class MokaDocsLayoutOptions
{
	/// <summary>
	///     Page width: <c>"default"</c> (a 780px reading column, up to 1440px wide), <c>"wide"</c> (a
	///     wider reading column, up to 1680px), <c>"wider"</c> (a 1280px reading column, up to 1920px),
	///     <c>"full"</c> (the page fills the window),
	///     <c>"boxed"</c> (a framed column on a tinted background) or <c>"narrow"</c> (a 640px reading
	///     column centered between the sidebar and the "On this page" panel).
	/// </summary>
	public string Width { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The top bar: <c>"default"</c> (sticky), <c>"compact"</c> (shorter), <c>"floating"</c> (a
	///     rounded bar inset from the edges), <c>"tabs"</c> (a second row with a tab for each top-level
	///     sidebar section), <c>"autohide"</c> (slides away while scrolling down), <c>"centered"</c> (the
	///     logo in the middle, search on the left, the buttons on the right) or <c>"transparent"</c> (a
	///     see-through, blurred bar over the page; solid when the reader asks for less transparency).
	/// </summary>
	public string Navbar { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The footer: <c>"default"</c> (one row), <c>"minimal"</c> (one centered line),
	///     <c>"columns"</c> (a column of links for each top-level sidebar section), <c>"none"</c>,
	///     <c>"centered"</c> (stacked and centered under a row of social icons) or <c>"compact"</c> (one
	///     small line, the parts separated by dots).
	/// </summary>
	public string Footer { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The "On this page" panel: <c>"default"</c> (a right-hand column with reading progress),
	///     <c>"minimal"</c> (the column with the links only), <c>"inline"</c> (a collapsible box at the
	///     top of the page), <c>"floating"</c> (a corner button that opens the list over the page),
	///     <c>"numbered"</c> (entries numbered 1, 1.1, 1.1.1), <c>"timeline"</c> (a dot per entry on a
	///     vertical line) or <c>"bordered"</c> (the column as a bordered card).
	/// </summary>
	public string Toc { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Breadcrumbs: <c>"default"</c> (slash-separated), <c>"chevrons"</c>, <c>"pills"</c>,
	///     <c>"path"</c> (a monospace path), <c>"back"</c> (only a link to the parent), <c>"arrows"</c>
	///     or <c>"dots"</c> (dot separators, lighter links).
	/// </summary>
	public string Breadcrumbs { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The previous and next links: <c>"default"</c> (two cards), <c>"minimal"</c> (plain links),
	///     <c>"stacked"</c> (full-width rows), <c>"split"</c> (one ruled bar, previous left and next
	///     right) or <c>"compact"</c> (small links on one line).
	/// </summary>
	public string PageNav { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The sidebar's look: <c>"default"</c>, <c>"lines"</c> (guide lines along nested levels),
	///     <c>"pills"</c> (roomier rows, the current page as a filled pill), <c>"compact"</c> (dense
	///     rows, smaller text), <c>"accent"</c> (an accent bar on the current page's edge, no fill),
	///     <c>"cards"</c> (each top-level section in a card) or <c>"minimal"</c> (no icons or fills, the
	///     current page bold and underlined).
	/// </summary>
	public string Sidebar { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     What the sidebar lists: <c>"default"</c> (every section) or <c>"section"</c> (only the
	///     top-level section the current page is in, and every section on a page outside them). Pairs
	///     with <see cref="Navbar" /> <c>"tabs"</c>, which links the sections from the top bar.
	/// </summary>
	public string SidebarScope { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The landing page as a whole: <c>"default"</c> (the hero, the feature cards, then the content),
	///     <c>"portal"</c> (a compact hero with a large search box, then a card for each top-level section of
	///     the sidebar), <c>"showcase"</c> (the hero and feature cards, then the page's <c>showcaseCode</c>,
	///     <c>stats</c> and <c>cta</c> front matter as bands), <c>"minimal"</c> (the title, the description,
	///     the buttons and the content) or <c>"docs"</c> (no hero: the sidebar, a page header, the feature
	///     cards as link cards and the content).
	/// </summary>
	public string Home { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The landing page's hero: <c>"default"</c> (centered), <c>"split"</c> (text on the left, the
	///     page's <c>heroCode</c> or <c>heroImage</c> front matter on the right; centered without
	///     either), <c>"minimal"</c> (left aligned, no icon, a smaller title, no background) or
	///     <c>"banner"</c> (a full-width band in the accent color, without a backdrop).
	/// </summary>
	public string Hero { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The backdrop behind the landing page's hero: <c>"default"</c> (a tint with a faint dot grid),
	///     <c>"dots"</c>, <c>"gradient"</c>, <c>"none"</c>, <c>"mesh"</c> (soft blurred blobs in the
	///     color preset's colors) or <c>"lines"</c> (faint diagonal lines).
	/// </summary>
	public string HeroBackground { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The landing page's feature cards: <c>"default"</c> (cards, three to a row), <c>"list"</c> (the
	///     icon beside the text, two to a row), <c>"icons"</c> (large icons without card boxes),
	///     <c>"compact"</c> (small dense tiles, four to a row) or <c>"bento"</c> (a grid of mixed sizes in
	///     which the first cards span more cells).
	/// </summary>
	public string Features { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Admonitions: <c>"default"</c>, <c>"filled"</c> (a tinted box under a solid title strip),
	///     <c>"minimal"</c> (a left bar and the title), <c>"github"</c> (an icon and the title over a
	///     left border, like GitHub's alerts), <c>"outlined"</c> (a border all around, no fill) or
	///     <c>"callout"</c> (larger, the icon in a tinted circle beside the title).
	/// </summary>
	public string Admonitions { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Tables: <c>"default"</c>, <c>"striped"</c> (alternate rows shaded), <c>"bordered"</c> (a
	///     rule around every cell), <c>"minimal"</c> (rules between rows only), <c>"compact"</c> (dense
	///     rows, smaller text) or <c>"rounded"</c> (a rounded frame with a tinted header row).
	/// </summary>
	public string Tables { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Spacing in the content, the sidebar and the "On this page" panel: <c>"default"</c>,
	///     <c>"compact"</c> or <c>"spacious"</c>.
	/// </summary>
	public string Density { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The typeface of text and headings: <c>"default"</c>, <c>"system"</c> (the operating
	///     system's UI font), <c>"serif"</c>, <c>"mono"</c>, <c>"rounded"</c> (a rounded sans serif) or
	///     <c>"humanist"</c> (a humanist sans serif such as Seravek or Calibri). Code stays monospace, and
	///     none of them downloads a font.
	/// </summary>
	public string Font { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Section headings in the content: <c>"default"</c>, <c>"underlined"</c> (a full-width rule
	///     under each h2), <c>"accent"</c> (a short accent bar before each h2 and h3) or
	///     <c>"numbered"</c> (h2 and h3 numbered 1, 1.1; the page title is not).
	/// </summary>
	public string Headings { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Links in the content: <c>"default"</c>, <c>"underlined"</c> (always underlined) or
	///     <c>"subtle"</c> (the text color with a dotted underline, a solid accent underline on hover and
	///     focus).
	/// </summary>
	public string Links { get; set; } = ThemeLayouts.Default;
}
