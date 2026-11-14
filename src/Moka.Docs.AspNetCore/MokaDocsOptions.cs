using System.Reflection;
using Moka.Docs.Core.Theming;

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
	///     The public URL readers reach the app at, such as <c>https://contoso.com</c>, like
	///     <c>site.url</c> in mokadocs.yaml. When set, pages get a canonical link and link preview
	///     tags (Open Graph and Twitter), and <c>llms.txt</c> links are absolute. Default:
	///     <c>null</c>, which leaves them out: the app can't tell its public URL from a request.
	/// </summary>
	/// <remarks>
	///     <see cref="PathBase" /> and <see cref="BasePath" /> are added to it, unless it already
	///     ends with them. The blog still gets no feeds.
	/// </remarks>
	public string? SiteUrl { get; set; }

	/// <summary>
	///     The picture link previews show for pages that don't set an <c>image</c> in their front
	///     matter, like <c>site.image</c> in mokadocs.yaml: a path to a file under
	///     <see cref="DocsPath" />, such as <c>"images/social-card.png"</c>, or an absolute http or
	///     https URL. Default: <c>null</c>.
	/// </summary>
	/// <remarks>
	///     Link previews need absolute URLs, so pages only carry the image when
	///     <see cref="SiteUrl" /> is set. A file outside <see cref="DocsPath" /> isn't published, and
	///     the build logs a warning.
	/// </remarks>
	public string? SocialImage { get; set; }

	/// <summary>
	///     Path to a folder containing Markdown documentation files.
	///     Relative paths are resolved from the application's content root.
	///     If null, only API reference pages are generated.
	/// </summary>
	/// <remarks>
	///     The build reads Markdown and the asset types the site publishes, skipping dot folders
	///     other than <c>.well-known</c>. App settings and build output (<c>appsettings*.json</c>,
	///     <c>*.deps.json</c>, <c>*.runtimeconfig.json</c>, <c>*.dll</c>, <c>*.pdb</c>, and the
	///     <c>bin</c>, <c>obj</c> and <c>node_modules</c> folders) are left out with a warning.
	/// </remarks>
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
	///     A theme preset, one of <see cref="ThemePresets.Names" />, like <c>theme.options.preset</c> in
	///     mokadocs.yaml: a named bundle of a style, layout variants and appearance defaults. Default:
	///     <c>null</c>, for none. An unknown name applies nothing, and the build logs a warning.
	/// </summary>
	/// <remarks>
	///     The preset fills in each option that is still at its default value. These options are plain
	///     properties, so one you set to its default value, such as <c>Style = "default"</c>, can't be
	///     told from one you never set and takes the preset's value too. Options the host has no
	///     property for, such as the code theme, come from the preset as well.
	/// </remarks>
	public string? Preset { get; set; }

	/// <summary>
	///     The language of the pages as a BCP 47 tag, such as <c>"de"</c>, <c>"ja"</c> or <c>"pt-BR"</c>, like
	///     <c>site.language</c> in mokadocs.yaml: <c>&lt;html lang&gt;</c>, the theme's built-in translation
	///     (Spanish, French, German, Brazilian Portuguese, Japanese and simplified Chinese), plural forms and
	///     how dates are written. Default: <c>"en"</c>. A malformed tag gives English, and the build logs a warning.
	/// </summary>
	public string Language { get; set; } = SiteLanguages.Default;

	/// <summary>
	///     The text direction of the pages, <c>"ltr"</c> or <c>"rtl"</c>, like <c>site.direction</c>. Default:
	///     <c>null</c>, which follows <see cref="Language" />: right to left for Arabic, Hebrew, Persian, Urdu and a few more.
	/// </summary>
	public string? Direction { get; set; }

	/// <summary>
	///     The site's own wording for the theme's text, by key (<see cref="ThemeStrings.All" />), like
	///     <c>theme.options.strings</c>: for example <c>["toc.title"] = "Contents"</c>. It wins over English and
	///     the built-in translation. An unknown key is ignored, and the build logs a warning.
	/// </summary>
	public Dictionary<string, string> Strings { get; set; } = [];

	/// <summary>
	///     Appearance style the pages start with, one of <see cref="ThemeStyles.Names" />. Default:
	///     <c>"default"</c>. An unknown name renders as <c>"default"</c>, and the build logs a warning.
	/// </summary>
	/// <remarks>
	///     <c>"moka"</c> and <c>"terminal"</c> start in dark mode for a reader who has never used the
	///     dark mode toggle.
	/// </remarks>
	public string Style { get; set; } = ThemeStyles.Default;

	/// <summary>
	///     Whether the header has a button for picking another style. The reader's pick is saved in
	///     the browser and wins over <see cref="Style" />. Default: true.
	/// </summary>
	public bool StyleSelector { get; set; } = true;

	/// <summary>
	///     Whether the Appearance panel lets readers pick a text size (90% to 125%). The pick is
	///     saved in the browser. Default: true.
	/// </summary>
	public bool TextSizeSelector { get; set; } = true;

	/// <summary>
	///     Whether the Appearance panel lets readers pick a narrow, default, wide, wider or full reading
	///     width. The pick is saved in the browser. Default: true.
	/// </summary>
	public bool WidthSelector { get; set; } = true;

	/// <summary>
	///     Whether the Appearance panel has a Layout section where readers switch the layout variants of a
	///     docs page (the top bar, footer, "On this page", breadcrumbs, previous/next links, sidebar,
	///     admonitions, tables, spacing, typeface, headings and links). The picks are saved in the browser;
	///     <see cref="Layout" /> stays the default. Pages then carry the markup every variant needs.
	///     Default: false.
	/// </summary>
	public bool LayoutSelector { get; set; }

	/// <summary>
	///     Whether clicking a content image, or pressing Enter on it, opens it full size in a viewer.
	///     Images inside links and very small images are left alone. Default: true.
	/// </summary>
	public bool ImageZoom { get; set; } = true;

	/// <summary>
	///     The layout variant of each part of the pages: width, top bar, footer, "On this page" panel,
	///     breadcrumbs, the previous/next links, the sidebar, the landing hero, admonitions, tables,
	///     spacing and the typeface. Every part defaults to <c>"default"</c>. Readers can switch the parts of
	///     a docs page only when <see cref="LayoutSelector" /> is on.
	/// </summary>
	public MokaDocsLayoutOptions Layout { get; set; } = new();

	/// <summary>
	///     Whether pages have a button that hides and shows the sidebar on wide screens. The reader's
	///     choice is kept in the browser for later pages. Default: false.
	/// </summary>
	public bool SidebarToggle { get; set; }

	/// <summary>
	///     How pages get the sidebar: <c>"auto"</c> (the default), <c>"inline"</c> or <c>"shared"</c>.
	///     <c>"inline"</c> writes the whole sidebar into every page. <c>"shared"</c> writes it once, to
	///     <c>{BasePath}/_theme/js/sidebar.js</c>, which every page loads; a page holds only the
	///     top-level entries and the ones along its own path, for readers without JavaScript.
	///     <c>"auto"</c> is inline up to 300 sidebar entries and shared above. An unknown name means
	///     <c>"auto"</c>, and the build logs a warning.
	/// </summary>
	/// <remarks>
	///     The shared file is served like the rest of the site, with an <c>ETag</c>. Its URL carries a
	///     hash of its content, so a rebuild that changes the sidebar changes the URL too.
	/// </remarks>
	public string Sidebar { get; set; } = SidebarModes.Auto;

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
	/// <remarks>
	///     Outside the Development environment the plugin gets <c>publishHost: false</c>, unless the
	///     entry sets <c>publishHost</c> itself: it never scaffolds a preview host or runs
	///     <c>dotnet publish</c> in the running app, and uses a host published beforehand.
	/// </remarks>
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
	///     The application's path base: the part of the public URL in front of the app's own routes,
	///     such as <c>"/shop"</c> for an app that runs as an IIS sub-application at <c>/shop</c>, behind
	///     <c>app.UsePathBase("/shop")</c>, or behind a proxy whose <c>X-Forwarded-Prefix</c> the
	///     forwarded headers middleware applies. Default: <c>null</c>, for an app at the root of its host.
	/// </summary>
	/// <remarks>
	///     The site is built once and held in memory, so its links can't follow each request's
	///     <c>HttpRequest.PathBase</c>. They start with <see cref="PathBase" /> followed by
	///     <see cref="BasePath" />. A request that arrives with a different path base is still
	///     served, and the first one for each path base logs a warning.
	/// </remarks>
	public string? PathBase { get; set; }

	/// <summary>
	///     Whether to cache the built documentation site in memory.
	///     When true (default), the site is built once, at startup or on the first request, and
	///     kept until it is rebuilt. When false, the site is rebuilt on every request, and each
	///     request waits for its build.
	/// </summary>
	public bool CacheOutput { get; set; } = true;

	/// <summary>
	///     Whether to minify the pages, like <c>build.minify</c> in <c>mokadocs.yaml</c>: whitespace
	///     collapsed and comments removed. When true, the theme's CSS and JS are minified too, whatever
	///     <see cref="MinifyAssets" /> says. Default: false.
	/// </summary>
	public bool Minify { get; set; }

	/// <summary>
	///     Whether to minify the theme's CSS and JS, like <c>build.minifyAssets</c> in
	///     <c>mokadocs.yaml</c>. The default, <c>null</c>, minifies them outside the Development
	///     environment and keeps them readable in Development, for debugging.
	/// </summary>
	public bool? MinifyAssets { get; set; }

	/// <summary>
	///     Whether to publish a Markdown copy of each page next to its HTML (<c>/docs/guide/intro</c>
	///     gets <c>/docs/guide/intro.md</c>), plus <c>llms.txt</c> and <c>llms-full.txt</c>, like
	///     <c>build.llms</c> in mokadocs.yaml. Pages with a copy show a "Copy page" button. Default: true.
	/// </summary>
	public bool Llms { get; set; } = true;

	/// <summary>
	///     Whether to build the site in the background as soon as the application has started,
	///     so the first reader doesn't wait for the build. Default: true. Ignored when
	///     <see cref="CacheOutput" /> is false, and when <c>MapMokaDocs</c> is never called.
	/// </summary>
	/// <remarks>
	///     The build starts from <c>IHostApplicationLifetime.ApplicationStarted</c>, after every
	///     endpoint is mapped. Requests don't wait for it: until the first build is done they get
	///     <c>503</c> with a <c>Retry-After</c> header. When this is false, the first request starts
	///     the build and gets the same answer. A failed build is logged once, requests get
	///     <c>503</c>, and the first request after 5 seconds tries again.
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
	///     Called with the feedback endpoint (<c>POST {BasePath}/api/feedback</c>) when
	///     <c>MapMokaDocs</c> maps it, to add conventions to that endpoint alone, such as
	///     <c>RequireRateLimiting("votes")</c>. Conventions on the group <c>MapMokaDocs</c> returns
	///     cover every docs route.
	/// </summary>
	public Action<Microsoft.AspNetCore.Builder.IEndpointConventionBuilder>? ConfigureFeedbackEndpoint { get; set; }

	/// <summary>
	///     Called with the REPL endpoint (<c>POST {BasePath}/api/repl/execute</c>) when
	///     <c>MapMokaDocs</c> maps it, to add conventions to that endpoint alone, such as
	///     <c>RequireAuthorization("Admins")</c> or <c>RequireRateLimiting("repl")</c>. Not called when
	///     the endpoint isn't mapped (see <see cref="EnableRepl" />).
	/// </summary>
	public Action<Microsoft.AspNetCore.Builder.IEndpointConventionBuilder>? ConfigureReplEndpoint { get; set; }

	/// <summary>
	///     Whether the code execution endpoint of <see cref="EnableRepl" /> is mapped outside the
	///     Development environment and runs code for requests from other machines or through a
	///     proxy. Default: false, which maps it in Development only and runs code only for
	///     requests from a loopback address, with a <c>Host</c> of <c>localhost</c>, a name under
	///     <c>.localhost</c> or a loopback address, and no <c>Forwarded</c>,
	///     <c>X-Forwarded-For</c> or <c>X-Forwarded-Host</c> header, as <c>mokadocs serve</c>
	///     listens on this machine only.
	/// </summary>
	/// <remarks>
	///     The endpoint compiles and runs any C# it receives, as the user the application runs
	///     as. Anyone who can reach it can run code on the server. Turn this on only behind
	///     authorization, for example <c>app.MapMokaDocs().RequireAuthorization("Admins")</c>. A
	///     reverse proxy on the same machine needs it too, because the proxy makes every caller
	///     look local.
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
	///     and the redirection settings, and removes <c>DOTNET_STARTUP_HOOKS</c> and the variables
	///     <c>dotnet watch</c> sets for hot reload. When this is set, the REPL doesn't check that
	///     Program.cs calls <see cref="MokaDocsReplWorker.TryRunAsync" />.
	/// </remarks>
	public Func<System.Diagnostics.ProcessStartInfo>? ReplWorkerStartInfo { get; set; }

	/// <summary>Copyright text shown in the footer.</summary>
	public string? Copyright { get; set; }

	/// <summary>
	///     An announcement above the header of every page, like <c>theme.options.banner</c> in
	///     mokadocs.yaml. <c>null</c> (the default) shows none.
	/// </summary>
	public MokaDocsBannerOptions? Banner { get; set; }

	/// <summary>
	///     The blog: posts in a folder under <see cref="DocsPath" />, a paginated index, tag pages
	///     and a sidebar entry. <c>null</c> (the default) builds no blog, and the folder's files are
	///     ordinary pages.
	/// </summary>
	public MokaDocsBlogOptions? Blog { get; set; }

	/// <summary>
	///     Glossary terms and their definitions, like <c>glossary:</c> in mokadocs.yaml: <c>:term[API]</c> shows the
	///     definition in a tooltip and <c>:::glossary</c> lists them all. A glossary.yml file at the root of
	///     <see cref="DocsPath" /> adds more.
	/// </summary>
	public Dictionary<string, string> Glossary { get; set; } = [];

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
	///     Whether to list everything below <see cref="Path" /> as a tree, as <c>mokadocs</c> does for
	///     a site without a <c>nav:</c> list: each folder is a section, and under <c>/api</c> each
	///     namespace is a section holding its types.
	/// </summary>
	/// <remarks>
	///     Without it, an entry lists the pages one route segment below its path. Type pages sit
	///     several segments below <c>/api</c>, so an API entry without it has no children.
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
