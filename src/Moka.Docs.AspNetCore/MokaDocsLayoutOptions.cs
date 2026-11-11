using Moka.Docs.Core.Theming;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     The layout variant of each part of the pages, the counterpart of <c>theme.options.layout</c> in
///     mokadocs.yaml. Every part defaults to <c>"default"</c>, the theme's original layout. Names are
///     matched without regard to case; an unknown name renders as <c>"default"</c>, and the build logs
///     a warning.
/// </summary>
/// <remarks>
///     Unlike <see cref="MokaDocsOptions.Style" />, readers can't switch layouts: every page uses
///     these.
/// </remarks>
public sealed class MokaDocsLayoutOptions
{
	/// <summary>
	///     Page width: <c>"default"</c> (a 780px reading column, up to 1440px wide), <c>"wide"</c> (a
	///     wider reading column, up to 1680px), <c>"full"</c> (the page fills the window) or
	///     <c>"boxed"</c> (a framed column on a tinted background).
	/// </summary>
	public string Width { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The top bar: <c>"default"</c> (sticky), <c>"compact"</c> (shorter), <c>"floating"</c> (a
	///     rounded bar inset from the edges), <c>"tabs"</c> (a second row with a tab for each top-level
	///     sidebar section) or <c>"autohide"</c> (slides away while scrolling down).
	/// </summary>
	public string Navbar { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The footer: <c>"default"</c> (one row), <c>"minimal"</c> (one centered line),
	///     <c>"columns"</c> (a column of links for each top-level sidebar section) or <c>"none"</c>.
	/// </summary>
	public string Footer { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The "On this page" panel: <c>"default"</c> (a right-hand column with reading progress),
	///     <c>"minimal"</c> (the column with the links only), <c>"inline"</c> (a collapsible box at the
	///     top of the page) or <c>"floating"</c> (a corner button that opens the list over the page).
	/// </summary>
	public string Toc { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     Breadcrumbs: <c>"default"</c> (slash-separated), <c>"chevrons"</c>, <c>"pills"</c>,
	///     <c>"path"</c> (a monospace path) or <c>"back"</c> (only a link to the parent).
	/// </summary>
	public string Breadcrumbs { get; set; } = ThemeLayouts.Default;

	/// <summary>
	///     The previous and next links: <c>"default"</c> (two cards), <c>"minimal"</c> (plain links) or
	///     <c>"stacked"</c> (full-width rows).
	/// </summary>
	public string PageNav { get; set; } = ThemeLayouts.Default;
}
