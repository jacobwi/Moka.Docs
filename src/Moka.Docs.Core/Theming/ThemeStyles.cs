namespace Moka.Docs.Core.Theming;

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

	/// <summary>Every style name, in the order the reader's picker lists them.</summary>
	public static IReadOnlyList<string> Names { get; } = [Default, Moka, Minimal, Classic, Soft];

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
	public static string Resolve(string? name) =>
		string.IsNullOrWhiteSpace(name) ? Default : Find(name) ?? Default;

	private static string? Find(string name)
	{
		string trimmed = name.Trim();
		foreach (string style in Names)
		{
			if (style.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
			{
				return style;
			}
		}

		return null;
	}
}
