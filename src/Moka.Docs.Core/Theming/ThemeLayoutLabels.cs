namespace Moka.Docs.Core.Theming;

// The words the Appearance panel's Layout section shows readers. A part or variant without an entry gets its key
// or name split into words, first letter capitalized (pageNav: "Page nav", striped: "Striped").
internal static class ThemeLayoutLabels
{
	private static readonly Dictionary<string, string> _parts = new(StringComparer.Ordinal)
	{
		["navbar"] = "Top bar",
		["toc"] = "On this page",
		["pageNav"] = "Previous and next",
		["sidebarScope"] = "Sidebar lists",
		["density"] = "Spacing"
	};

	// Keyed "part.variant"; "*.variant" applies to every part without its own entry.
	private static readonly Dictionary<string, string> _variants = new(StringComparer.Ordinal)
	{
		["*.default"] = "Standard",
		["navbar.autohide"] = "Auto-hide",
		["sidebarScope.default"] = "All sections",
		["sidebarScope.section"] = "Current section",
		["sidebar.accent"] = "Accent bar",
		["headings.accent"] = "Accent bar",
		["admonitions.github"] = "GitHub",
		["font.mono"] = "Monospace",
		["font.system"] = "System UI"
	};

	public static string Part(string key) => _parts.GetValueOrDefault(key) ?? Words(key);

	public static string Variant(string key, string name) =>
		_variants.GetValueOrDefault(key + "." + name) ?? _variants.GetValueOrDefault("*." + name) ?? Words(name);

	public static bool HasOwnVariantLabel(string key, string name) => _variants.ContainsKey(key + "." + name);

	private static string Words(string camelCase)
	{
		if (camelCase.Length == 0)
		{
			return camelCase;
		}

		string spaced = string.Concat(camelCase.Select((c, i) => char.IsUpper(c) && i > 0 ? " " + char.ToLowerInvariant(c) : c.ToString()));
		return char.ToUpperInvariant(spaced[0]) + spaced[1..];
	}
}
