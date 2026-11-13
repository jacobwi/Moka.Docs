namespace Moka.Docs.Core.Theming;

/// <summary>One color preset of the default theme, as the reader's color picker lists it.</summary>
public sealed record ColorPresetInfo
{
	/// <summary>
	///     The name <c>theme.options.defaultColorTheme</c> and the <c>data-color-theme</c> attribute
	///     use.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>The name the picker shows, e.g. <c>Moka Red</c>.</summary>
	public required string Label { get; init; }

	/// <summary>The preset's primary color as a hex value, which the picker's swatch shows.</summary>
	public required string Swatch { get; init; }
}

/// <summary>
///     The color presets of the default theme. A preset sets the primary color (links, the current
///     page, buttons) and the landing page gradient, in every style and in light and dark mode.
/// </summary>
public static class ColorPresets
{
	/// <summary>The preset a page starts with when <c>defaultColorTheme</c> isn't set.</summary>
	public const string Default = "ocean";

	/// <summary>Every preset with its picker label and swatch, in the order the picker lists them.</summary>
	public static IReadOnlyList<ColorPresetInfo> All { get; } =
	[
		new() { Name = "ocean", Label = "Ocean", Swatch = "#0ea5e9" },
		new() { Name = "emerald", Label = "Emerald", Swatch = "#10b981" },
		new() { Name = "violet", Label = "Violet", Swatch = "#8b5cf6" },
		new() { Name = "amber", Label = "Amber", Swatch = "#f59e0b" },
		new() { Name = "rose", Label = "Rose", Swatch = "#f43f5e" },
		new() { Name = "moka-red", Label = "Moka Red", Swatch = "#d32f2f" },
		new() { Name = "slate", Label = "Slate", Swatch = "#64748b" },
		new() { Name = "teal", Label = "Teal", Swatch = "#14b8a6" },
		new() { Name = "indigo", Label = "Indigo", Swatch = "#6366f1" },
		new() { Name = "orange", Label = "Orange", Swatch = "#f97316" },
		new() { Name = "graphite", Label = "Graphite", Swatch = "#52525b" }
	];

	/// <summary>Every preset name, in the order the picker lists them.</summary>
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
	///     The preset a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Default" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured preset name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) => Find(name)?.Name ?? Default;

	/// <summary>
	///     The preset named <paramref name="name" />, ignoring case and surrounding whitespace, or
	///     <c>null</c> when no preset has that name.
	/// </summary>
	/// <param name="name">A preset name.</param>
	/// <returns>The preset, or <c>null</c>.</returns>
	public static ColorPresetInfo? Find(string? name) => AppearanceNames.Find(All, p => p.Name, name);
}
