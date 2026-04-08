using System.IO.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Core.Pipeline;

/// <summary>
///     A single phase in the MokaDocs build pipeline.
/// </summary>
public interface IBuildPhase
{
	/// <summary>Display name of this phase.</summary>
	string Name { get; }

	/// <summary>Execution order (lower runs first).</summary>
	int Order { get; }

	/// <summary>Execute this build phase.</summary>
	Task ExecuteAsync(BuildContext context, CancellationToken ct = default);
}

/// <summary>
///     Mutable state bag passed through all build pipeline phases.
/// </summary>
public sealed class BuildContext
{
	/// <summary>The site configuration.</summary>
	public required SiteConfig Config { get; init; }

	/// <summary>All discovered and parsed pages.</summary>
	public List<DocPage> Pages { get; } = [];

	/// <summary>The extracted API reference model.</summary>
	public ApiReference? ApiModel { get; set; }

	/// <summary>The generated navigation tree.</summary>
	public NavigationTree? Navigation { get; set; }

	/// <summary>The generated search index.</summary>
	public SearchIndex? SearchIndex { get; set; }

	/// <summary>The current version being built.</summary>
	public DocVersion? CurrentVersion { get; set; }

	/// <summary>All configured documentation versions.</summary>
	public List<DocVersion> Versions { get; } = [];

	/// <summary>Build diagnostics (warnings, errors, info).</summary>
	public DiagnosticBag Diagnostics { get; } = new();

	/// <summary>Abstracted file system for testability.</summary>
	public required IFileSystem FileSystem { get; init; }

	/// <summary>The root directory of the documentation project.</summary>
	public required string RootDirectory { get; init; }

	/// <summary>The output directory for generated files.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Discovered Markdown file paths (relative to docs root).</summary>
	public List<string> DiscoveredMarkdownFiles { get; } = [];

	/// <summary>Discovered C# project file paths.</summary>
	public List<string> DiscoveredProjectFiles { get; } = [];

	/// <summary>Discovered static asset file paths.</summary>
	public List<string> DiscoveredAssetFiles { get; } = [];

	/// <summary>
	///     Resolved brand assets (logo + favicon) that need to be copied to the output site
	///     from locations OUTSIDE the normal content.docs directory tree. Keyed by
	///     site-root-relative publish URL (e.g. <c>/assets/logo.png</c> or <c>/_media/logo.png</c>);
	///     value is the absolute filesystem source path.
	///     <para>
	///         Populated during the Discovery phase by <c>BrandAssetResolver</c> from the
	///         <see cref="SiteAssetReference" /> data already parsed by <see cref="SiteConfigReader" />.
	///         Consumed during the Output phase by <c>OutputPhase.CopyAssets</c>, which writes
	///         each entry to <c>_site/{publishUrl}</c>.
	///     </para>
	///     <para>
	///         This is a separate collection from <see cref="DiscoveredAssetFiles" /> because
	///         those are relative-to-<c>content.docs</c> paths discovered by globbing, whereas
	///         brand assets may live anywhere on disk (including above the yaml dir via <c>../</c>)
	///         and need explicit filesystem lookup.
	///     </para>
	/// </summary>
	public Dictionary<string, string> BrandAssetFiles { get; } = new(StringComparer.Ordinal);

	/// <summary>Package metadata extracted from .csproj files (name and version for NuGet install widgets).</summary>
	public PackageMetadata? PackageInfo { get; set; }

	/// <summary>
	///     Extra files to write to the output directory after the clean step.
	///     Keyed by output-relative path (e.g. "preview-wasm/assemblies/abc.dll").
	///     Populated by plugins during ExecuteAsync; written by OutputPhase.
	/// </summary>
	public Dictionary<string, byte[]> DeferredOutputFiles { get; } = new();

	/// <summary>
	///     Extra directories to copy to the output directory after the clean step.
	///     Each entry is (absoluteSourceDir, outputRelativeDestPath).
	///     Populated by plugins during ExecuteAsync; copied by OutputPhase.
	/// </summary>
	public List<(string SourceDir, string DestRelPath)> DeferredOutputDirectories { get; } = [];
}

/// <summary>
///     NuGet package metadata extracted from a .csproj file.
/// </summary>
public sealed record PackageMetadata
{
	/// <summary>The package name (from PackageId, AssemblyName, or project file name).</summary>
	public required string Name { get; init; }

	/// <summary>The package version (from PackageVersion, Version, or fallback "1.0.0").</summary>
	public required string Version { get; init; }
}
