namespace Moka.Docs.Core.Navigation;

/// <summary>
///     The complete navigation tree for the site sidebar.
/// </summary>
public sealed record NavigationTree
{
	/// <summary>Top-level navigation items.</summary>
	public List<NavigationNode> Items { get; init; } = [];

	/// <summary>Empty navigation tree.</summary>
	public static NavigationTree Empty => new();
}

/// <summary>
///     A single node in the navigation tree.
/// </summary>
public sealed record NavigationNode
{
	/// <summary>Display label.</summary>
	public required string Label { get; init; }

	/// <summary>URL route for this node (null for section headers).</summary>
	public string? Route { get; init; }

	/// <summary>Icon name for display.</summary>
	public string? Icon { get; init; }

	/// <summary>Sort order.</summary>
	public int Order { get; init; }

	/// <summary>Whether this node is expanded by default.</summary>
	public bool Expanded { get; init; } = true;

	/// <summary>Whether this node is the currently active page.</summary>
	public bool IsActive { get; init; }

	/// <summary>Child nodes.</summary>
	public List<NavigationNode> Children { get; init; } = [];

	/// <inheritdoc />
	public override string ToString() => $"NavNode({Label}, {Children.Count} children)";
}
