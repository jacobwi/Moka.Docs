using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Search;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Content;

/// <summary>
///     Top-level model representing the entire documentation site being generated.
/// </summary>
/// <remarks>
///     Nothing in MokaDocs creates or reads this type. A build's state is in
///     <see cref="Moka.Docs.Core.Pipeline.BuildContext" />.
/// </remarks>
[Obsolete("DocSite is not used by MokaDocs; a build's state is in BuildContext. It will be removed in 2.0.")]
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

	/// <summary>
	///     The version number the label stands for, such as <c>1.8.0</c>, which the <c>version</c>
	///     ranges in page front matter are matched against. <c>null</c> when it isn't known; the
	///     number in <see cref="Label" /> is used then, and a label without one, such as
	///     <c>next</c>, keeps every page.
	/// </summary>
	public string? Number { get; init; }

	/// <summary>
	///     Where this version's pages are published: a URL path that includes the base path and has
	///     no trailing slash, such as <c>/docs/v1.0</c>, or an empty string for the site root.
	///     <c>null</c> when the build doesn't place versions; the version dropdown then links the
	///     default version to <c>{base path}/</c> and the others to <c>{base path}/{slug}/</c>.
	/// </summary>
	public string? UrlPath { get; init; }

	/// <summary>
	///     The routes of this version's pages, when the build knows them. The version dropdown keeps
	///     the reader on the same page when its route is listed here, and links to the version's home
	///     page otherwise.
	/// </summary>
	public IReadOnlySet<string>? Routes { get; init; }

	/// <inheritdoc />
	public override string ToString() => $"DocVersion({Label})";
}
