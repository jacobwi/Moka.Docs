using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Search;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Content;

/// <summary>
///     Top-level model representing the entire documentation site being generated.
/// </summary>
public sealed record DocSite
{
	/// <summary>The site configuration.</summary>
	public required SiteConfig Config { get; init; }

	/// <summary>All pages in the site.</summary>
	public List<DocPage> Pages { get; init; } = [];

	/// <summary>The API reference model, if C# projects were analyzed.</summary>
	public ApiReference? ApiReference { get; init; }

	/// <summary>The navigation tree for the sidebar.</summary>
	public NavigationTree? Navigation { get; init; }

	/// <summary>The pre-built search index.</summary>
	public SearchIndex? SearchIndex { get; init; }

	/// <summary>The current version being built.</summary>
	public DocVersion? CurrentVersion { get; init; }

	/// <summary>All configured versions.</summary>
	public List<DocVersion> Versions { get; init; } = [];

	/// <summary>The active theme manifest.</summary>
	public ThemeManifest? Theme { get; init; }
}

/// <summary>
///     Represents a specific version snapshot of the documentation.
/// </summary>
public sealed record DocVersion
{
	/// <summary>Display label (e.g., "v2.0").</summary>
	public required string Label { get; init; }

	/// <summary>URL path segment for this version.</summary>
	public required string Slug { get; init; }

	/// <summary>Whether this is the default/latest version.</summary>
	public bool IsDefault { get; init; }

	/// <summary>Whether this is a prerelease version.</summary>
	public bool IsPrerelease { get; init; }

	/// <inheritdoc />
	public override string ToString() => $"DocVersion({Label})";
}
