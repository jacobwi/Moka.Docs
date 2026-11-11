namespace Moka.Docs.Core.Theming;

/// <summary>
///     One part of the default theme's page layout that <c>theme.options.layout</c> can change, with
///     the variants it offers.
/// </summary>
public sealed class ThemeLayoutPart
{
	internal ThemeLayoutPart(string key, params string[] names)
	{
		Key = key;
		Names = Array.AsReadOnly(names);
	}

	/// <summary>The part's key under <c>theme.options.layout</c>, e.g. <c>navbar</c>.</summary>
	public string Key { get; }

	/// <summary>Every variant name, <see cref="ThemeLayouts.Default" /> first.</summary>
	public IReadOnlyList<string> Names { get; }

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means the default.
	/// </summary>
	/// <param name="name">A configured variant name.</param>
	/// <returns><c>true</c> when the name is empty or names a variant.</returns>
	public bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The variant a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="ThemeLayouts.Default" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured variant name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public string Resolve(string? name) =>
		string.IsNullOrWhiteSpace(name) ? ThemeLayouts.Default : Find(name) ?? ThemeLayouts.Default;

	private string? Find(string name)
	{
		string trimmed = name.Trim();
		foreach (string variant in Names)
		{
			if (variant.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
			{
				return variant;
			}
		}

		return null;
	}
}

/// <summary>
///     The layout parts of the default theme and their variants, set under
///     <c>theme.options.layout</c>. Each part is independent of the others and of the style, the
///     color preset and dark mode.
/// </summary>
public static class ThemeLayouts
{
	/// <summary>The variant every part starts with: the theme's original layout.</summary>
	public const string Default = "default";

	/// <summary>
	///     Page width: <c>default</c>, <c>wide</c> (a wider reading column), <c>full</c> (the page
	///     fills the window) or <c>boxed</c> (a framed page on a tinted background).
	/// </summary>
	public static ThemeLayoutPart Width { get; } = new("width", Default, "wide", "full", "boxed");

	/// <summary>
	///     The top bar: <c>default</c>, <c>compact</c> (shorter), <c>floating</c> (a rounded bar
	///     detached from the edges), <c>tabs</c> (a second row with the top-level sections) or
	///     <c>autohide</c> (slides away while scrolling down and back on scrolling up).
	/// </summary>
	public static ThemeLayoutPart Navbar { get; } = new("navbar", Default, "compact", "floating", "tabs", "autohide");

	/// <summary>
	///     The footer: <c>default</c>, <c>minimal</c> (one centered line), <c>columns</c> (link
	///     columns built from the top-level sections) or <c>none</c>.
	/// </summary>
	public static ThemeLayoutPart Footer { get; } = new("footer", Default, "minimal", "columns", "none");

	/// <summary>
	///     The "On this page" panel: <c>default</c>, <c>minimal</c> (the links only), <c>inline</c> (a
	///     collapsible box at the top of the page) or <c>floating</c> (a button that opens it over the
	///     page).
	/// </summary>
	public static ThemeLayoutPart Toc { get; } = new("toc", Default, "minimal", "inline", "floating");

	/// <summary>
	///     Breadcrumbs: <c>default</c>, <c>chevrons</c>, <c>pills</c>, <c>path</c> (a monospace path)
	///     or <c>back</c> (a single link to the parent).
	/// </summary>
	public static ThemeLayoutPart Breadcrumbs { get; } = new("breadcrumbs", Default, "chevrons", "pills", "path", "back");

	/// <summary>
	///     The previous and next links under a page: <c>default</c> (cards), <c>minimal</c> (plain
	///     links) or <c>stacked</c> (full-width rows).
	/// </summary>
	public static ThemeLayoutPart PageNav { get; } = new("pageNav", Default, "minimal", "stacked");

	/// <summary>Every part, in the order the documentation lists them.</summary>
	public static IReadOnlyList<ThemeLayoutPart> Parts { get; } = [Width, Navbar, Footer, Toc, Breadcrumbs, PageNav];
}
