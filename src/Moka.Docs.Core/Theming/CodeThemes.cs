namespace Moka.Docs.Core.Theming;

/// <summary>One syntax highlighting theme of the default theme, as the reader's code theme picker lists it.</summary>
public sealed record CodeThemeInfo
{
	/// <summary>The name <c>theme.options.codeTheme</c> and the <c>data-code-theme</c> attribute use.</summary>
	public required string Name { get; init; }

	/// <summary>The name the picker shows, e.g. <c>GitHub Dark</c>.</summary>
	public required string Label { get; init; }

	/// <summary>The theme's code block background as a hex value, which the picker's preview shows.</summary>
	public required string Background { get; init; }

	/// <summary>Whether the theme has a dark background.</summary>
	public required bool IsDark { get; init; }

	/// <summary>
	///     The theme the dark mode toggle swaps this one for (the light variant of a dark theme and the
	///     other way round), or <c>null</c> when it has none and stays as it is.
	/// </summary>
	public string? Counterpart { get; init; }
}

/// <summary>The syntax highlighting themes of the default theme's code blocks.</summary>
public static class CodeThemes
{
	/// <summary>The theme code blocks use when <c>codeTheme</c> isn't set.</summary>
	public const string Default = "catppuccin-mocha";

	/// <summary>Every code theme, in the order the picker lists them.</summary>
	public static IReadOnlyList<CodeThemeInfo> All { get; } =
	[
		Dark("catppuccin-mocha", "Catppuccin Mocha", "#1e1e2e", "catppuccin-latte"),
		Light("catppuccin-latte", "Catppuccin Latte", "#eff1f5", "catppuccin-mocha"),
		Dark("github-dark", "GitHub Dark", "#0d1117", "github-light"),
		Light("github-light", "GitHub Light", "#ffffff", "github-dark"),
		Dark("dracula", "Dracula", "#282a36"),
		Dark("one-dark", "One Dark", "#282c34"),
		Dark("nord", "Nord", "#2e3440"),
		Dark("tokyo-night", "Tokyo Night", "#1a1b26"),
		Dark("gruvbox-dark", "Gruvbox Dark", "#282828", "gruvbox-light"),
		Light("gruvbox-light", "Gruvbox Light", "#fbf1c7", "gruvbox-dark"),
		Dark("solarized-dark", "Solarized Dark", "#002b36", "solarized-light"),
		Light("solarized-light", "Solarized Light", "#fdf6e3", "solarized-dark"),
		Dark("rose-pine", "Rose Pine", "#191724", "rose-pine-dawn"),
		Light("rose-pine-dawn", "Rose Pine Dawn", "#faf4ed", "rose-pine"),
		Dark("night-owl", "Night Owl", "#011627"),
		Dark("monokai", "Monokai", "#272822")
	];

	/// <summary>Every code theme name, in the order the picker lists them.</summary>
	public static IReadOnlyList<string> Names { get; } = [.. All.Select(t => t.Name)];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means the default.
	/// </summary>
	/// <param name="name">A configured code theme name.</param>
	/// <returns><c>true</c> when the name is empty or names a code theme.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The code theme a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Default" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured code theme name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) => Find(name)?.Name ?? Default;

	/// <summary>
	///     The code theme named <paramref name="name" />, ignoring case and surrounding whitespace, or
	///     <c>null</c> when no code theme has that name.
	/// </summary>
	/// <param name="name">A code theme name.</param>
	/// <returns>The code theme, or <c>null</c>.</returns>
	public static CodeThemeInfo? Find(string? name) => AppearanceNames.Find(All, t => t.Name, name);

	private static CodeThemeInfo Dark(string name, string label, string background, string? counterpart = null) =>
		new() { Name = name, Label = label, Background = background, IsDark = true, Counterpart = counterpart };

	private static CodeThemeInfo Light(string name, string label, string background, string counterpart) =>
		new() { Name = name, Label = label, Background = background, IsDark = false, Counterpart = counterpart };
}
