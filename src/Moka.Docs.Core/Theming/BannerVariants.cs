namespace Moka.Docs.Core.Theming;

/// <summary>
///     The looks of the announcement banner (<c>theme.options.banner.variant</c>) in the default theme.
/// </summary>
public static class BannerVariants
{
	/// <summary>A light tint of the primary color. The default.</summary>
	public const string Info = "info";

	/// <summary>An amber tint, for a deprecation or an outage.</summary>
	public const string Warning = "warning";

	/// <summary>A solid bar in the primary color, for a release or an event.</summary>
	public const string Accent = "accent";

	/// <summary>Every variant name, <see cref="Info" /> first.</summary>
	public static IReadOnlyList<string> Names { get; } = [Info, Warning, Accent];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means <see cref="Info" />.
	/// </summary>
	/// <param name="name">A configured variant name.</param>
	/// <returns><c>true</c> when the name is empty or names a variant.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The variant a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Info" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured variant name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) => Find(name) ?? Info;

	private static string? Find(string? name) => AppearanceNames.Find(Names, n => n, name);
}
