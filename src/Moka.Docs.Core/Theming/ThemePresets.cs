using System.Collections.ObjectModel;

namespace Moka.Docs.Core.Theming;

/// <summary>
///     A named bundle of theme options that <c>theme.options.preset</c> applies in one line.
/// </summary>
public sealed record ThemePresetInfo
{
	/// <summary>The name <c>theme.options.preset</c> takes, e.g. <c>reference</c>.</summary>
	public required string Name { get; init; }

	/// <summary>A display name, e.g. <c>Reference</c>.</summary>
	public required string Label { get; init; }

	/// <summary>One sentence on the look the preset gives a site.</summary>
	public required string Description { get; init; }

	/// <summary>
	///     The options the preset sets, keyed by their path under <c>theme.options</c> as written in
	///     mokadocs.yaml (<c>style</c>, <c>layout.navbar</c>), with each value as it would be written
	///     there.
	/// </summary>
	/// <remarks>
	///     An option the site sets itself keeps the site's value: the preset only fills in the options
	///     the configuration leaves out.
	/// </remarks>
	public required IReadOnlyDictionary<string, string> Options { get; init; }
}

/// <summary>
///     The theme presets: named bundles of a style, layout variants and appearance defaults that
///     <c>theme.options.preset</c> applies in one line. Anything the site sets itself wins over the
///     preset.
/// </summary>
public static class ThemePresets
{
	/// <summary>Classic reference docs: a solid bar with section tabs, link columns and striped tables.</summary>
	public const string Reference = "reference";

	/// <summary>Monospace and dark first, compact, with a path for breadcrumbs.</summary>
	public const string Terminal = "terminal";

	/// <summary>A book: serif text, the page contents inline and quiet navigation.</summary>
	public const string Book = "book";

	/// <summary>A product site: rounded and roomy, a floating bar, a split hero and filled callouts.</summary>
	public const string Product = "product";

	/// <summary>Quiet and out of the way: a bar that hides while reading and plain navigation.</summary>
	public const string Minimal = "minimal";

	/// <summary>Accessibility first: the contrast style, the system font and spacious layout.</summary>
	public const string Accessible = "accessible";

	/// <summary>The Moka look: dense, dark first, red accent.</summary>
	public const string Moka = "moka";

	/// <summary>Every preset, in the order the documentation lists them.</summary>
	public static IReadOnlyList<ThemePresetInfo> All { get; } =
	[
		Preset(Reference, "Reference", "Classic reference docs: a solid bar with section tabs, link columns and striped tables.",
			("style", ThemeStyles.Classic),
			("layout.navbar", "tabs"),
			("layout.footer", "columns"),
			("layout.breadcrumbs", "chevrons"),
			("layout.tables", "striped")),
		Preset(Terminal, "Terminal", "Monospace and dark first, compact, with a path for breadcrumbs.",
			("style", ThemeStyles.Terminal),
			("defaultColorTheme", "moka-red"),
			("codeTheme", "tokyo-night"),
			("layout.navbar", "compact"),
			("layout.breadcrumbs", "path"),
			("layout.footer", "minimal"),
			("layout.pageNav", "minimal"),
			("layout.density", "compact")),
		Preset(Book, "Book", "A book: serif text, the page contents inline and quiet navigation.",
			("style", ThemeStyles.Paper),
			("layout.toc", "inline"),
			("layout.pageNav", "minimal"),
			("layout.footer", "minimal"),
			("layout.breadcrumbs", "back"),
			("layout.font", "serif")),
		Preset(Product, "Product", "A product site: rounded and roomy, a floating bar, a split hero and filled callouts.",
			("style", ThemeStyles.Soft),
			("layout.navbar", "floating"),
			("layout.footer", "columns"),
			("layout.hero", "split"),
			("layout.heroBackground", "gradient"),
			("layout.admonitions", "filled")),
		Preset(Minimal, "Minimal", "Quiet and out of the way: a bar that hides while reading and plain navigation.",
			("style", ThemeStyles.Minimal),
			("layout.navbar", "autohide"),
			("layout.toc", "minimal"),
			("layout.footer", "minimal"),
			("layout.breadcrumbs", "back"),
			("layout.pageNav", "minimal"),
			("layout.tables", "minimal")),
		Preset(Accessible, "Accessible", "Accessibility first: the contrast style, the system font and spacious layout.",
			("style", ThemeStyles.Contrast),
			("layout.font", "system"),
			("layout.density", "spacious")),
		Preset(Moka, "Moka", "The Moka look: dense, dark first, red accent.",
			("style", ThemeStyles.Moka),
			("defaultColorTheme", "moka-red"),
			("codeTheme", "catppuccin-mocha"),
			("layout.navbar", "compact"),
			("layout.density", "compact"))
	];

	/// <summary>Every preset name, in the order the documentation lists them.</summary>
	public static IReadOnlyList<string> Names { get; } = [.. All.Select(p => p.Name)];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means no preset.
	/// </summary>
	/// <param name="name">A configured preset name.</param>
	/// <returns><c>true</c> when the name is empty or names a preset.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The preset named <paramref name="name" />, ignoring case and surrounding whitespace, or
	///     <c>null</c> when the name is empty or no preset has it.
	/// </summary>
	/// <param name="name">A preset name.</param>
	/// <returns>The preset, or <c>null</c>.</returns>
	public static ThemePresetInfo? Find(string? name) => AppearanceNames.Find(All, p => p.Name, name);

	private static ThemePresetInfo Preset(string name, string label, string description, params (string Key, string Value)[] options) =>
		new()
		{
			Name = name,
			Label = label,
			Description = description,
			Options = new ReadOnlyDictionary<string, string>(options.ToDictionary(o => o.Key, o => o.Value, StringComparer.Ordinal))
		};
}
