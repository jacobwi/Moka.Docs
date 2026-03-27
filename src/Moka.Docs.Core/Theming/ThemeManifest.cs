namespace Moka.Docs.Core.Theming;

/// <summary>
///     Metadata and configuration for an active theme.
/// </summary>
public sealed record ThemeManifest
{
	/// <summary>Theme name.</summary>
	public required string Name { get; init; }

	/// <summary>Theme version.</summary>
	public string Version { get; init; } = "1.0.0";

	/// <summary>Theme description.</summary>
	public string Description { get; init; } = "";

	/// <summary>Theme author.</summary>
	public string? Author { get; init; }

	/// <summary>Root directory path of the theme.</summary>
	public required string RootPath { get; init; }

	/// <summary>Available layout template names.</summary>
	public List<string> Layouts { get; init; } = [];

	/// <summary>CSS files to include.</summary>
	public List<string> CssFiles { get; init; } = [];

	/// <summary>JavaScript files to include.</summary>
	public List<string> JsFiles { get; init; } = [];

	/// <summary>Theme-configurable variables.</summary>
	public Dictionary<string, string> Variables { get; init; } = [];

	/// <inheritdoc />
	public override string ToString() => $"Theme({Name} v{Version})";
}
