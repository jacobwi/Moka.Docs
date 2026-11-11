namespace Moka.Docs.Core.Theming;

/// <summary>
///     How the default theme puts the sidebar navigation into pages, set by
///     <c>theme.options.sidebar</c>.
/// </summary>
public static class SidebarModes
{
	/// <summary>
	///     <see cref="Inline" /> while the sidebar has at most <c>theme.options.sidebarInlineLimit</c>
	///     entries, <see cref="Shared" /> above that. The default.
	/// </summary>
	public const string Auto = "auto";

	/// <summary>The whole sidebar is written into every page. Works without JavaScript.</summary>
	public const string Inline = "inline";

	/// <summary>
	///     The sidebar is written once, to <c>_theme/js/sidebar.js</c>, and every page loads it. A page
	///     holds only the top-level entries and the ones along the path to itself, which is what a
	///     reader without JavaScript sees.
	/// </summary>
	public const string Shared = "shared";

	/// <summary>Every mode name.</summary>
	public static IReadOnlyList<string> Names { get; } = [Auto, Inline, Shared];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means <see cref="Auto" />.
	/// </summary>
	/// <param name="name">A configured mode name.</param>
	/// <returns><c>true</c> when the name is empty or names a mode.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The mode a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Auto" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured mode name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) =>
		string.IsNullOrWhiteSpace(name) ? Auto : Find(name) ?? Auto;

	private static string? Find(string name)
	{
		string trimmed = name.Trim();
		foreach (string mode in Names)
		{
			if (mode.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
			{
				return mode;
			}
		}

		return null;
	}
}
