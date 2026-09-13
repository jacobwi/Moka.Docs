using System.Reflection;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Configuration options for embedding MokaDocs in an ASP.NET Core application.
/// </summary>
public sealed class MokaDocsOptions
{
	/// <summary>Documentation site title.</summary>
	public string Title { get; set; } = "API Documentation";

	/// <summary>Site description shown on the landing page.</summary>
	public string Description { get; set; } = "";

	/// <summary>URL to a logo image. If null, uses the default book icon.</summary>
	public string? LogoUrl { get; set; }

	/// <summary>URL to the favicon. If null, uses the default.</summary>
	public string? FaviconUrl { get; set; }

	/// <summary>
	///     Path to a folder containing Markdown documentation files.
	///     Relative paths are resolved from the application's content root.
	///     If null, only API reference pages are generated.
	/// </summary>
	public string? DocsPath { get; set; }

	/// <summary>
	///     Assemblies to scan for public API types.
	///     If empty, the calling assembly is auto-detected.
	/// </summary>
	public List<Assembly> Assemblies { get; set; } = [];

	/// <summary>
	///     Whether to auto-discover and parse XML documentation files (.xml)
	///     located next to each assembly's DLL. Default: true.
	/// </summary>
	public bool IncludeXmlDocs { get; set; } = true;

	/// <summary>Primary theme color (CSS hex). Default: sky blue.</summary>
	public string PrimaryColor { get; set; } = "#0ea5e9";

	/// <summary>Accent theme color (CSS hex).</summary>
	public string AccentColor { get; set; } = "#f59e0b";

	/// <summary>
	///     Version label shown in the version selector in the header (e.g., "v2.0").
	///     If null, the header has no version selector.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>Whether to enable the interactive C# REPL plugin.</summary>
	public bool EnableRepl { get; set; }

	/// <summary>
	///     Whether to enable Blazor component preview. The plugin's options, such as
	///     <c>previewHost</c> or <c>library</c>, go in a <see cref="Plugins" /> entry with the id
	///     <c>mokadocs-blazor-preview</c>. That entry turns the plugin on by itself, too.
	/// </summary>
	public bool EnableBlazorPreview { get; set; }

	/// <summary>
	///     Plugins to run during the build, the counterpart of the <c>plugins:</c> list in
	///     mokadocs.yaml. Each entry is matched by <see cref="PluginEntry.Id" /> against the
	///     <c>IMokaPlugin</c> services in the container.
	/// </summary>
	/// <remarks>
	///     <c>AddMokaDocs</c> registers the built-in plugins <c>openapi</c>,
	///     <c>mokadocs-blazor-preview</c>, <c>mokadocs-changelog</c> and <c>mokadocs-python-api</c>
	///     when they are declared here. Register a plugin of your own as an <c>IMokaPlugin</c>
	///     singleton. The REPL plugin is registered by <see cref="EnableRepl" /> only.
	/// </remarks>
	public List<PluginEntry> Plugins { get; set; } = [];

	/// <summary>
	///     URL path prefix where documentation is served.
	///     Default: "/docs". The site is accessible at this path and all sub-paths.
	/// </summary>
	public string BasePath { get; set; } = "/docs";

	/// <summary>
	///     Whether to cache the built documentation site in memory.
	///     When true (default), the site is built once on first request.
	///     When false, the site is rebuilt on every request (useful for development).
	/// </summary>
	public bool CacheOutput { get; set; } = true;

	/// <summary>Copyright text shown in the footer.</summary>
	public string? Copyright { get; set; }

	/// <summary>
	///     Navigation items to show in the sidebar.
	///     If empty, navigation is auto-generated from the docs folder structure and API namespaces.
	/// </summary>
	public List<NavEntry> Nav { get; set; } = [];
}

/// <summary>
///     A navigation entry for the sidebar.
/// </summary>
public sealed class NavEntry
{
	/// <summary>Display label.</summary>
	public required string Label { get; set; }

	/// <summary>URL path (e.g., "/guide").</summary>
	public required string Path { get; set; }

	/// <summary>Lucide icon name (e.g., "book-open", "code").</summary>
	public string? Icon { get; set; }

	/// <summary>Whether this section is expanded by default.</summary>
	public bool Expanded { get; set; }

	/// <summary>
	///     Whether to list the API reference under this entry: each namespace, with its types.
	///     Applies when <see cref="Path" /> is <c>/api</c>, where the API pages are generated.
	/// </summary>
	/// <remarks>
	///     An entry for any other path lists the pages one route segment below it, whether or
	///     not this is set. Type pages sit several segments below <c>/api</c>, so without this an
	///     API entry has no children.
	/// </remarks>
	public bool AutoGenerate { get; set; }
}

/// <summary>
///     Declares a plugin for the build, like an entry in the <c>plugins:</c> list of mokadocs.yaml.
/// </summary>
public sealed class PluginEntry
{
	/// <summary>
	///     The plugin's <c>IMokaPlugin.Id</c>, for example <c>"openapi"</c>. Matched without regard
	///     to case.
	/// </summary>
	public required string Id { get; set; }

	/// <summary>
	///     Plugin-specific options, the <c>options:</c> map of a mokadocs.yaml entry. The plugin
	///     reads them from <c>IPluginContext.Options</c>. Lists can be any
	///     <see cref="IEnumerable{T}" /> of strings, such as a <c>string[]</c>.
	/// </summary>
	/// <remarks>
	///     The build's root directory is the application's content root, so the built-in plugins
	///     resolve relative paths in their options, such as the OpenAPI <c>spec</c> or the Blazor
	///     preview's <c>previewHost</c>, from there.
	/// </remarks>
	public Dictionary<string, object> Options { get; set; } = [];
}
