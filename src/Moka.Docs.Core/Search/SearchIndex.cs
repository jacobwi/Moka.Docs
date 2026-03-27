namespace Moka.Docs.Core.Search;

/// <summary>
///     A pre-built search index for client-side search functionality.
/// </summary>
public sealed record SearchIndex
{
	/// <summary>All searchable entries.</summary>
	public List<SearchEntry> Entries { get; init; } = [];

	/// <summary>Total number of entries.</summary>
	public int Count => Entries.Count;

	/// <summary>Empty search index.</summary>
	public static SearchIndex Empty => new();
}

/// <summary>
///     A single searchable entry in the index.
/// </summary>
public sealed record SearchEntry
{
	/// <summary>The page title.</summary>
	public required string Title { get; init; }

	/// <summary>The section heading (if this entry is for a specific section).</summary>
	public string? Section { get; init; }

	/// <summary>URL route to navigate to.</summary>
	public required string Route { get; init; }

	/// <summary>Text content snippet for matching and display.</summary>
	public required string Content { get; init; }

	/// <summary>Category for grouping results (e.g., "Documentation", "API Reference").</summary>
	public string Category { get; init; } = "Documentation";

	/// <summary>Tags for additional matching.</summary>
	public List<string> Tags { get; init; } = [];
}
