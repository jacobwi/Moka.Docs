namespace Moka.Docs.Core.Theming;

/// <summary>One appearance style of the default theme, as the reader's style picker lists it.</summary>
public sealed record ThemeStyleInfo
{
	/// <summary>The name <c>theme.options.style</c> and the <c>data-style</c> attribute use.</summary>
	public required string Name { get; init; }

	/// <summary>The name the picker shows, e.g. <c>Moka</c>.</summary>
	public required string Label { get; init; }

	/// <summary>A few words under the label in the picker, e.g. <c>Dense, dark first</c>.</summary>
	public required string Description { get; init; }

	/// <summary>
	///     Whether the style starts in dark mode for a reader who has never used the dark mode toggle,
	///     instead of following the operating system.
	/// </summary>
	public bool DarkFirst { get; init; }
}

/// <summary>
///     The appearance styles of the default theme. A style changes the whole look (surfaces,
///     borders, spacing, radii, shadows and fonts), independent of the color preset, the code
///     theme and dark mode.
/// </summary>
public static class ThemeStyles
{
	/// <summary>The theme's original look, used when no style is configured.</summary>
	public const string Default = "default";

	/// <summary>Dense, accent-tinted, dark-first look with glow rings instead of shadows.</summary>
	public const string Moka = "moka";

	/// <summary>Few borders, more whitespace and a narrow reading column.</summary>
	public const string Minimal = "minimal";

	/// <summary>Traditional reference docs: solid header bar, bordered panels, dense tables.</summary>
	public const string Classic = "classic";

	/// <summary>Rounded cards, soft shadows, tinted sections and pill highlights.</summary>
	public const string Soft = "soft";

	/// <summary>Monospace and dense, prompt-like headings, square corners, dark first.</summary>
	public const string Terminal = "terminal";

	/// <summary>A book page: serif text, a narrow measure, warm paper colors and rule lines.</summary>
	public const string Paper = "paper";

	/// <summary>
	///     Accessibility first: 7:1 body text, underlined links, thick focus rings and no
	///     transparency.
	/// </summary>
	public const string Contrast = "contrast";

	/// <summary>Translucent, blurred header, sidebar and cards over a soft gradient.</summary>
	public const string Glass = "glass";

	/// <summary>Thick solid borders, flat fills, square corners and hard offset shadows.</summary>
	public const string Brutal = "brutal";

	/// <summary>Every style with its picker label, in the order the reader's picker lists them.</summary>
	public static IReadOnlyList<ThemeStyleInfo> All { get; } =
	[
		new() { Name = Default, Label = "Default", Description = "The original look" },
		new() { Name = Moka, Label = "Moka", Description = "Dense, dark first", DarkFirst = true },
		new() { Name = Minimal, Label = "Minimal", Description = "Quiet, few lines" },
		new() { Name = Classic, Label = "Classic", Description = "Reference docs" },
		new() { Name = Soft, Label = "Soft", Description = "Rounded, roomy" },
		new() { Name = Terminal, Label = "Terminal", Description = "Monospace, dark first", DarkFirst = true },
		new() { Name = Paper, Label = "Paper", Description = "Serif, like a book" },
		new() { Name = Contrast, Label = "Contrast", Description = "High contrast, clear focus" },
		new() { Name = Glass, Label = "Glass", Description = "Frosted panels" },
		new() { Name = Brutal, Label = "Brutal", Description = "Thick lines, flat fills" }
	];

	/// <summary>Every style name, in the order the reader's picker lists them.</summary>
	public static IReadOnlyList<string> Names { get; } = [.. All.Select(s => s.Name)];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means the default.
	/// </summary>
	/// <param name="name">A configured style name.</param>
	/// <returns><c>true</c> when the name is empty or names a style.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The style a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Default" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured style name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) => Find(name)?.Name ?? Default;

	/// <summary>
	///     The style named <paramref name="name" />, ignoring case and surrounding whitespace, or
	///     <c>null</c> when no style has that name.
	/// </summary>
	/// <param name="name">A style name.</param>
	/// <returns>The style, or <c>null</c>.</returns>
	public static ThemeStyleInfo? Find(string? name) => AppearanceNames.Find(All, s => s.Name, name);
}
