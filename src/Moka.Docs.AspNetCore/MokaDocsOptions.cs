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

	/// <summary>
	///     XML documentation files to read besides the ones next to each DLL. A package reference's
	///     XML file stays in the NuGet cache unless the project copies it to the output, so this is
	///     how a package assembly in <see cref="Assemblies" /> gets its documentation.
	/// </summary>
	/// <remarks>
	///     Each file is matched to an assembly by its <c>&lt;doc&gt;&lt;assembly&gt;&lt;name&gt;</c>
	///     element, not by its file name, and is read instead of the file next to that assembly's DLL.
	///     Relative paths resolve from the application's content root. Ignored when
	///     <see cref="IncludeXmlDocs" /> is false.
	/// </remarks>
	public List<string> XmlDocFiles { get; set; } = [];

	/// <summary>
	///     Namespaces to document. When empty, every namespace is documented. An entry matches the
	///     namespace and the namespaces below it: <c>Contoso.Api</c> matches <c>Contoso.Api</c> and
	///     <c>Contoso.Api.Models</c>, not <c>Contoso.Apis</c>. <c>(global)</c> or an empty string
	///     matches the types declared outside any namespace.
	/// </summary>
	public List<string> IncludeNamespaces { get; set; } = [];

	/// <summary>
	///     Namespaces to leave out, matched like <see cref="IncludeNamespaces" />. An excluded
	///     namespace stays out even when an include entry matches it.
	/// </summary>
	/// <remarks>
	///     The types of an excluded namespace still pass their documentation on through
	///     <c>&lt;inheritdoc/&gt;</c>, but get no page, no sidebar entry and no links.
	/// </remarks>
	public List<string> ExcludeNamespaces { get; set; } = [];

	/// <summary>
	///     Whether type pages link to the type's source in its repository, read from the SourceLink
	///     data in the assembly's portable PDB. GitHub and GitLab links open the file at the type;
	///     other hosts get the raw file URL. Default: true.
	/// </summary>
	public bool SourceLinks { get; set; } = true;

	/// <summary>
	///     Whether type pages show the type's declaration in a View Source section, like pages the
	///     CLI builds. The source comes from the files the assembly's portable PDB names, or from
	///     the PDB itself when it embeds them. Default: false.
	/// </summary>
	/// <remarks>
	///     The section shows the bodies of the members the page documents, which can reveal how a
	///     web application works. Turn it on only when every reader of the docs may see that code.
	/// </remarks>
	public bool IncludeSourceCode { get; set; }

	/// <summary>Primary theme color (CSS hex). Default: sky blue.</summary>
	public string PrimaryColor { get; set; } = "#0ea5e9";

	/// <summary>Accent theme color (CSS hex).</summary>
	public string AccentColor { get; set; } = "#f59e0b";

	/// <summary>
	///     Version label shown in the version selector in the header (e.g., "v2.0").
	///     If null, the header has no version selector.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	///     Whether to enable the interactive C# REPL plugin, which adds a Run button to
	///     <c>csharp-repl</c> code blocks, and the <c>POST {BasePath}/api/repl/execute</c> endpoint
	///     the button posts to. The endpoint is mapped in the Development environment only and runs
	///     code for requests from this machine only, unless <see cref="AllowRemoteRepl" /> is set.
	/// </summary>
	/// <remarks>
	///     Snippets run in a separate process: this application's own executable, started again
	///     with <see cref="MokaDocsReplWorker.TryRunAsync" /> as the first line of Program.cs.
	/// </remarks>
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
	///     Name of one of the application's own OpenAPI documents, such as <c>"v1"</c>, to document
	///     as REST API pages. <c>null</c> (the default) turns this off.
	/// </summary>
	/// <remarks>
	///     The document comes from the provider that <c>AddOpenApi()</c> (Microsoft.AspNetCore.OpenApi)
	///     or <c>AddSwaggerGen()</c> (Swashbuckle.AspNetCore) registers, read for each build after the
	///     application has started. When it can't be read, the build warns and has no REST API pages.
	/// </remarks>
	public string? OpenApiDocument { get; set; }

	/// <summary>
	///     Route prefix of the REST API pages, below <see cref="BasePath" />. Default: <c>"/rest-api"</c>.
	///     Also the default <c>routePrefix</c> of <c>openapi</c> entries in <see cref="Plugins" />, since
	///     the plugin's own default, <c>/api</c>, is where the API reference is.
	/// </summary>
	public string OpenApiRoutePrefix { get; set; } = "/rest-api";

	/// <summary>
	///     Title of the REST API index page and label of its sidebar entry. Default: <c>"REST API"</c>.
	///     Also the default <c>label</c> of <c>openapi</c> entries in <see cref="Plugins" />.
	/// </summary>
	public string OpenApiLabel { get; set; } = "REST API";

	/// <summary>
	///     URL path prefix where documentation is served.
	///     Default: "/docs". The site is accessible at this path and all sub-paths.
	/// </summary>
	public string BasePath { get; set; } = "/docs";

	/// <summary>
	///     Whether to cache the built documentation site in memory.
	///     When true (default), the site is built once, at startup or on the first request, and
	///     kept until it is rebuilt. When false, the site is rebuilt on every request.
	/// </summary>
	public bool CacheOutput { get; set; } = true;

	/// <summary>
	///     Whether to build the site in the background as soon as the application has started,
	///     so the first reader doesn't wait for the build. Default: true. Ignored when
	///     <see cref="CacheOutput" /> is false, and when <c>MapMokaDocs</c> is never called.
	/// </summary>
	/// <remarks>
	///     The build starts from <c>IHostApplicationLifetime.ApplicationStarted</c>, after every
	///     endpoint is mapped. A failed build is logged, and the first request tries again.
	/// </remarks>
	public bool BuildOnStartup { get; set; } = true;

	/// <summary>
	///     Whether to rebuild the site when a file under <see cref="DocsPath" /> changes. The
	///     default, <c>null</c>, watches in the Development environment only. Ignored when
	///     <see cref="CacheOutput" /> is false, since every request rebuilds then.
	/// </summary>
	/// <remarks>
	///     A rebuild runs in the background. Requests get the previous site until the new one is
	///     ready, then the new one replaces it in a single step. A failed rebuild is logged and
	///     the previous site stays.
	/// </remarks>
	public bool? WatchDocs { get; set; }

	/// <summary>
	///     Whether pages show the "Was this page helpful?" widget, and whether <c>MapMokaDocs</c>
	///     maps the <c>POST {BasePath}/api/feedback</c> endpoint it posts to. Default: true.
	/// </summary>
	public bool ShowFeedback { get; set; } = true;

	/// <summary>
	///     Called for each vote the feedback widget sends, after it is logged. Use it to store
	///     the votes. When null, votes are only logged.
	/// </summary>
	/// <remarks>
	///     The page is the path the reader was on, base path included. Anyone who can load the
	///     docs can vote as often as they like, so treat the counts as a hint.
	/// </remarks>
	public Func<DocsFeedback, Microsoft.AspNetCore.Http.HttpContext, Task>? OnFeedback { get; set; }

	/// <summary>
	///     Whether the code execution endpoint of <see cref="EnableRepl" /> is mapped outside the
	///     Development environment and runs code for requests from other machines. Default: false,
	///     which maps it in Development only and answers callers on a loopback address only, as
	///     <c>mokadocs serve</c> listens on localhost only.
	/// </summary>
	/// <remarks>
	///     The endpoint compiles and runs any C# it receives, as the user the application runs
	///     as. Anyone who can reach it can run code on the server. Turn this on only behind
	///     authorization, for example <c>app.MapMokaDocs().RequireAuthorization("Admins")</c>.
	/// </remarks>
	public bool AllowRemoteRepl { get; set; }

	/// <summary>
	///     Returns the command that starts a REPL worker process, for hosts where the default
	///     doesn't work. When null, the worker is this application's own executable, which
	///     needs <see cref="MokaDocsReplWorker.TryRunAsync" /> as the first line of Program.cs.
	/// </summary>
	/// <remarks>
	///     The process must call <c>ReplWorker.RunAsync</c> with its standard input and output,
	///     as <see cref="MokaDocsReplWorker.TryRunAsync" /> does. The service adds the
	///     <c>MOKADOCS_REPL_WORKER</c> and <c>MOKADOCS_REPL_PARENT_PID</c> environment variables
	///     and the redirection settings. When this is set, the REPL doesn't check that
	///     Program.cs calls <see cref="MokaDocsReplWorker.TryRunAsync" />.
	/// </remarks>
	public Func<System.Diagnostics.ProcessStartInfo>? ReplWorkerStartInfo { get; set; }

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
