namespace Moka.Docs.Core.Theming;

/// <summary>One code block frame of the default theme, as the reader's code style picker lists it.</summary>
public sealed record CodeStyleInfo
{
	/// <summary>The name <c>theme.options.codeStyle</c> and the <c>data-code-style</c> attribute use.</summary>
	public required string Name { get; init; }

	/// <summary>The name the picker shows, e.g. <c>VS Code</c>.</summary>
	public required string Label { get; init; }
}

/// <summary>The frames the default theme can draw around code blocks.</summary>
public static class CodeStyles
{
	/// <summary>The frame code blocks use when <c>codeStyle</c> isn't set.</summary>
	public const string Default = "plain";

	/// <summary>Every code style, in the order the picker lists them.</summary>
	public static IReadOnlyList<CodeStyleInfo> All { get; } =
	[
		new() { Name = Default, Label = "Plain" },
		new() { Name = "macos", Label = "macOS" },
		new() { Name = "terminal", Label = "Terminal" },
		new() { Name = "vscode", Label = "VS Code" }
	];

	/// <summary>Every code style name, in the order the picker lists them.</summary>
	public static IReadOnlyList<string> Names { get; } = [.. All.Select(s => s.Name)];

	/// <summary>
	///     Whether <paramref name="name" /> is one of <see cref="Names" />, ignoring case and
	///     surrounding whitespace. An empty or null name counts as known: it means the default.
	/// </summary>
	/// <param name="name">A configured code style name.</param>
	/// <returns><c>true</c> when the name is empty or names a code style.</returns>
	public static bool IsKnown(string? name) =>
		string.IsNullOrWhiteSpace(name) || Find(name) is not null;

	/// <summary>
	///     The code style a configured name selects: the matching name from <see cref="Names" />, or
	///     <see cref="Default" /> when the name is empty or unknown.
	/// </summary>
	/// <param name="name">A configured code style name.</param>
	/// <returns>A value from <see cref="Names" />.</returns>
	public static string Resolve(string? name) => Find(name)?.Name ?? Default;

	/// <summary>
	///     The code style named <paramref name="name" />, ignoring case and surrounding whitespace, or
	///     <c>null</c> when no code style has that name.
	/// </summary>
	/// <param name="name">A code style name.</param>
	/// <returns>The code style, or <c>null</c>.</returns>
	public static CodeStyleInfo? Find(string? name) => AppearanceNames.Find(All, s => s.Name, name);
}
