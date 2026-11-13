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

	/// <summary>What the Appearance panel calls the part, e.g. <c>On this page</c> for <c>toc</c>.</summary>
	public string Label => ThemeLayoutLabels.Part(Key);

	/// <summary>
	///     What the Appearance panel calls one of the part's variants, e.g. <c>Auto-hide</c> for
	///     <c>autohide</c> and <c>Standard</c> for <see cref="ThemeLayouts.Default" />.
	/// </summary>
	/// <param name="name">One of <see cref="Names" />.</param>
	/// <returns>A label for readers.</returns>
	public string LabelOf(string name) => ThemeLayoutLabels.Variant(Key, name);

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
	///     Page width: <c>default</c>, <c>wide</c> (a wider reading column), <c>wider</c> (a 1280px
	///     reading column on a page up to 1920px wide), <c>full</c> (the page fills the window),
	///     <c>boxed</c> (a framed page on a tinted background) or <c>narrow</c> (a 640px reading column
	///     between the sidebar and "On this page").
	/// </summary>
	public static ThemeLayoutPart Width { get; } = new("width", Default, "wide", "wider", "full", "boxed", "narrow");

	/// <summary>
	///     The top bar: <c>default</c>, <c>compact</c> (shorter), <c>floating</c> (a rounded bar
	///     detached from the edges), <c>tabs</c> (a second row with the top-level sections),
	///     <c>autohide</c> (slides away while scrolling down and back on scrolling up), <c>centered</c>
	///     (the logo in the middle, search on the left and the buttons on the right) or
	///     <c>transparent</c> (a see-through, blurred bar over the page).
	/// </summary>
	public static ThemeLayoutPart Navbar { get; } =
		new("navbar", Default, "compact", "floating", "tabs", "autohide", "centered", "transparent");

	/// <summary>
	///     The footer: <c>default</c>, <c>minimal</c> (one centered line), <c>columns</c> (link
	///     columns built from the top-level sections), <c>none</c>, <c>centered</c> (everything stacked
	///     and centered under a row of social icons) or <c>compact</c> (one small line, the parts
	///     separated by dots).
	/// </summary>
	public static ThemeLayoutPart Footer { get; } = new("footer", Default, "minimal", "columns", "none", "centered", "compact");

	/// <summary>
	///     The "On this page" panel: <c>default</c>, <c>minimal</c> (the links only), <c>inline</c> (a
	///     collapsible box at the top of the page), <c>floating</c> (a button that opens it over the
	///     page), <c>numbered</c> (entries numbered 1, 1.1, 1.1.1), <c>timeline</c> (a dot per entry on a
	///     vertical line, the current one filled) or <c>bordered</c> (the column as a bordered card).
	/// </summary>
	public static ThemeLayoutPart Toc { get; } =
		new("toc", Default, "minimal", "inline", "floating", "numbered", "timeline", "bordered");

	/// <summary>
	///     Breadcrumbs: <c>default</c>, <c>chevrons</c>, <c>pills</c>, <c>path</c> (a monospace path),
	///     <c>back</c> (a single link to the parent), <c>arrows</c> (arrow separators) or <c>dots</c>
	///     (dot separators and lighter links).
	/// </summary>
	public static ThemeLayoutPart Breadcrumbs { get; } =
		new("breadcrumbs", Default, "chevrons", "pills", "path", "back", "arrows", "dots");

	/// <summary>
	///     The previous and next links under a page: <c>default</c> (cards), <c>minimal</c> (plain
	///     links), <c>stacked</c> (full-width rows), <c>split</c> (one ruled bar, previous on the left and
	///     next on the right) or <c>compact</c> (small links on one line).
	/// </summary>
	public static ThemeLayoutPart PageNav { get; } = new("pageNav", Default, "minimal", "stacked", "split", "compact");

	/// <summary>
	///     The sidebar's look: <c>default</c>, <c>lines</c> (guide lines along nested levels),
	///     <c>pills</c> (roomier rows, the current page as a filled pill), <c>compact</c> (dense rows,
	///     smaller text), <c>accent</c> (an accent bar on the current page's left edge, no fill),
	///     <c>cards</c> (each top-level section in a card) or <c>minimal</c> (no icons or fills, the
	///     current page bold and underlined).
	/// </summary>
	public static ThemeLayoutPart Sidebar { get; } =
		new("sidebar", Default, "lines", "pills", "compact", "accent", "cards", "minimal");

	/// <summary>
	///     What the sidebar lists: <c>default</c> (every section) or <c>section</c> (only the top-level
	///     section the current page is in, and every section on a page outside them).
	/// </summary>
	public static ThemeLayoutPart SidebarScope { get; } = new("sidebarScope", Default, "section");

	/// <summary>
	///     The landing page as a whole: <c>default</c> (the hero, the feature cards, then the content),
	///     <c>portal</c> (a compact hero with a large search box, then a card for each top-level sidebar
	///     section with its first pages), <c>showcase</c> (the hero and the feature cards, then a code sample
	///     band, a row of figures and a call to action from front matter), <c>minimal</c> (the title, the
	///     description, the buttons and the content, without feature cards or a hero backdrop) or
	///     <c>docs</c> (no hero: the sidebar, the title and description as a page header, the feature cards
	///     as link cards, then the content).
	/// </summary>
	public static ThemeLayoutPart Home { get; } = new("home", Default, "portal", "showcase", "minimal", "docs");

	/// <summary>
	///     The landing page's hero: <c>default</c> (centered), <c>split</c> (text on the left, the
	///     <c>heroCode</c> or <c>heroImage</c> from front matter on the right; centered without either),
	///     <c>minimal</c> (left aligned, no icon, a smaller title, no background) or <c>banner</c> (a
	///     full-width band in the accent color, without a backdrop).
	/// </summary>
	public static ThemeLayoutPart Hero { get; } = new("hero", Default, "split", "minimal", "banner");

	/// <summary>
	///     The backdrop behind the landing page's hero: <c>default</c> (a tint with a faint dot grid),
	///     <c>dots</c>, <c>gradient</c>, <c>none</c>, <c>mesh</c> (soft blurred blobs in the color
	///     preset's colors) or <c>lines</c> (faint diagonal lines).
	/// </summary>
	public static ThemeLayoutPart HeroBackground { get; } =
		new("heroBackground", Default, "dots", "gradient", "none", "mesh", "lines");

	/// <summary>
	///     The landing page's feature cards: <c>default</c> (cards, three to a row), <c>list</c> (the icon
	///     beside the text, two to a row and one on phones), <c>icons</c> (large icons without card boxes),
	///     <c>compact</c> (small dense tiles, four to a row) or <c>bento</c> (a grid of mixed sizes in which
	///     the first cards span more cells).
	/// </summary>
	public static ThemeLayoutPart Features { get; } = new("features", Default, "list", "icons", "compact", "bento");

	/// <summary>
	///     Admonitions (<c>::: note</c> and the like): <c>default</c>, <c>filled</c> (a tinted box under a
	///     solid title strip), <c>minimal</c> (a left bar and the title), <c>github</c> (GitHub's alert
	///     look: an icon and the title over a left border), <c>outlined</c> (a border all around, no
	///     fill) or <c>callout</c> (larger, the icon in a tinted circle beside the title).
	/// </summary>
	public static ThemeLayoutPart Admonitions { get; } =
		new("admonitions", Default, "filled", "minimal", "github", "outlined", "callout");

	/// <summary>
	///     Tables in the content: <c>default</c>, <c>striped</c> (alternate rows shaded), <c>bordered</c>
	///     (a frame and rules around every cell), <c>minimal</c> (rules between rows, no vertical lines),
	///     <c>compact</c> (dense rows, smaller text) or <c>rounded</c> (a rounded frame with a tinted
	///     header row).
	/// </summary>
	public static ThemeLayoutPart Tables { get; } =
		new("tables", Default, "striped", "bordered", "minimal", "compact", "rounded");

	/// <summary>
	///     Spacing in the content, the sidebar and "On this page": <c>default</c>, <c>compact</c> or
	///     <c>spacious</c>.
	/// </summary>
	public static ThemeLayoutPart Density { get; } = new("density", Default, "compact", "spacious");

	/// <summary>
	///     The typeface of text and headings: <c>default</c>, <c>system</c> (the operating system's UI
	///     font), <c>serif</c>, <c>mono</c>, <c>rounded</c> (a rounded sans serif) or <c>humanist</c> (a
	///     humanist sans serif such as Seravek, Gill Sans Nova or Calibri). Code stays monospace, and none
	///     of them downloads a font.
	/// </summary>
	public static ThemeLayoutPart Font { get; } = new("font", Default, "system", "serif", "mono", "rounded", "humanist");

	/// <summary>
	///     Section headings in the content: <c>default</c>, <c>underlined</c> (a full-width rule under
	///     each h2), <c>accent</c> (a short bar in the accent color before each h2 and h3) or
	///     <c>numbered</c> (h2 and h3 numbered 1, 1.1; the page title is not).
	/// </summary>
	public static ThemeLayoutPart Headings { get; } = new("headings", Default, "underlined", "accent", "numbered");

	/// <summary>
	///     Links in the content: <c>default</c>, <c>underlined</c> (always underlined) or <c>subtle</c>
	///     (the text color with a dotted underline, a solid underline in the accent color on hover and
	///     focus).
	/// </summary>
	public static ThemeLayoutPart Links { get; } = new("links", Default, "underlined", "subtle");

	/// <summary>Every part, in the order the documentation lists them.</summary>
	public static IReadOnlyList<ThemeLayoutPart> Parts { get; } =
	[
		Width, Navbar, Footer, Toc, Breadcrumbs, PageNav, Sidebar, SidebarScope, Home, Hero, HeroBackground, Features,
		Admonitions, Tables, Density, Font, Headings, Links
	];

	/// <summary>
	///     The parts readers can switch in the Appearance panel's Layout section
	///     (<c>theme.options.layoutSelector</c>): every part of a docs page except <see cref="Width" />, which
	///     the panel's Reading width covers. The landing page's own parts aren't among them.
	/// </summary>
	public static IReadOnlyList<ThemeLayoutPart> ReaderParts { get; } =
	[
		Navbar, Footer, Toc, Breadcrumbs, PageNav, Sidebar, SidebarScope, Admonitions, Tables, Density, Font, Headings,
		Links
	];
}
