using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Blazor.Repl.Abstractions.Models;
using Moka.Blazor.Repl.Compiler;
using Moka.Docs.Core;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Parsing.Markdown;
using Moka.Docs.Serve;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     MokaDocs plugin that compiles <c>```blazor-preview</c> code blocks with Roslyn at build
///     time and shows each one in an iframe that runs a Blazor WebAssembly preview host.
///     <para>
///         <b>Architecture</b> - one iframe per preview block. Each block is compiled to an
///         assembly at build time, written to <c>_site/_preview-assemblies/</c>, and rendered
///         inside an <c>&lt;iframe loading="lazy"&gt;</c> pointing at the published preview host
///         at <c>/_preview-wasm/</c>. Lazy loading means iframes beyond the viewport don't boot a
///         runtime until scrolled into view. Components portal to the iframe's own
///         <c>document.body</c>, so Dialog/Popover/Toast render inside the preview frame.
///     </para>
///     <para>
///         <b>Options</b> - <c>previewHost</c>, <c>library</c>, <c>publishHost</c>,
///         <c>references</c>, <c>usings</c>, <c>publishTimeout</c> and <c>renderTimeout</c>; see
///         docs/plugins/blazor-preview.md. With <c>publishHost</c> on (the default) the plugin
///         scaffolds a host when it finds none and publishes it with <c>dotnet publish</c>; off,
///         it only uses the published output of <c>previewHost</c>.
///     </para>
///     <para>
///         <b>Outputs</b> - per build:
///         <list type="bullet">
///             <item>
///                 <description>
///                     <c>_site/_preview-wasm/</c> - the host's published wwwroot, its pages carrying a
///                     Content-Security-Policy and a message guard (<see cref="PreviewPageHardening" />).
///                 </description>
///             </item>
///             <item>
///                 <description><c>_site/_preview-assemblies/{sha256}.bin</c> - one compiled assembly per preview block.</description>
///             </item>
///             <item>
///                 <description><c>_site/_preview-assets/</c> - the page stylesheet and script (<see cref="PreviewPageAssets" />).</description>
///             </item>
///             <item>
///                 <description><c>_site/.gitattributes</c> - <c>*.js binary</c>, for branch deploys.</description>
///             </item>
///         </list>
///         Without a published host, each block shows its static render instead of an iframe.
///     </para>
/// </summary>
public sealed class BlazorPreviewPlugin : IMokaPlugin
{
	// ── Preview-host auto-discovery / scaffolding / publishing ────────────────

	/// <summary>
	///     Pinned version of <c>Moka.Blazor.Repl.Host</c> that the scaffold template references.
	///     Keep it equal to the <c>Moka.Blazor.Repl.Compiler</c> version in
	///     Directory.Packages.props: previews compile with that compiler and run in this host.
	/// </summary>
	/// <remarks>
	///     This was 1.3.0, which was never published to nuget.org (the feed starts at 1.3.2), so
	///     a scaffolded host restored 1.3.2 with a warning instead of the version the compiler
	///     here is built against.
	/// </remarks>
	private const string _hostPackageVersion = "1.3.6";

	/// <summary>
	///     Where the plugin scaffolds a preview host when neither <c>previewHost</c> nor
	///     <c>library</c> is set, relative to the mokadocs.yaml directory.
	/// </summary>
	/// <remarks>
	///     A host without a library holds nothing of the user's, so it lives with the build cache
	///     instead of next to their sources. The SDK also leaves dot folders out of a project's
	///     default globs: in <c>./preview-host/</c>, a project that shares its folder with
	///     mokadocs.yaml (the Library sample does) compiles the host's <c>Program.cs</c> as its own
	///     source.
	/// </remarks>
	private const string _generatedHostDirectory = ".mokadocs/preview-host";

	/// <summary>
	///     Written into <c>publish-output/net10.0/</c> when a publish succeeds. Its time is when the
	///     publish started, which the stale check compares the host's inputs against.
	/// </summary>
	/// <remarks>
	///     The published <c>blazor.webassembly.js</c> can't serve as that time: publish copies it
	///     with the time it has in the NuGet package, so it was older than any scaffolded host
	///     and every new process published again.
	/// </remarks>
	private const string _publishStampFile = ".mokadocs-published";

	private const int _maxTimeoutSeconds = 86_400;

	private static readonly TimeSpan _defaultPublishTimeout = TimeSpan.FromMinutes(10);
	private static readonly TimeSpan _defaultRenderTimeout = TimeSpan.FromSeconds(5);

	private static readonly HashSet<string> _referencedSourceExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".cs", ".razor", ".cshtml", ".css", ".js", ".resx", ".csproj", ".props", ".targets"
	};

	private static readonly HashSet<string> _skippedReferenceDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"bin", "obj", "publish-output", "node_modules"
	};

	private static readonly Lazy<string?> _sharedFrameworkRoot =
		new(() => SharedFrameworkRoot(typeof(object).Assembly.Location));

	private static readonly Lazy<Dictionary<string, string>> _trustedPlatformAssemblies =
		new(ReadTrustedPlatformAssemblies);

	// The compiler keeps the client it gets (it fetches NuGet packages with it), and a new one per
	// plugin instance left its sockets behind. The pooled lifetime picks up DNS changes, as
	// fundamentals/http-requests.md ("Alternatives to IHttpClientFactory") describes.
	private static readonly HttpClient _httpClient =
		new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

	// ── Instance state ─────────────────────────────────────────────────────────

	private readonly List<string> _extraUsings = [];
	private readonly List<string> _knownDllPaths = [];
	private string _basePathForAssemblyQuery = "";
	private RoslynCompilationService? _compilationService;

	/// <summary>The published wwwroot to stage, or <c>null</c> when previews show only their static render.</summary>
	private string? _previewHostWwwroot;
	private TimeSpan _renderTimeout = _defaultRenderTimeout;
	private bool _servicesInitialized;

	// ── IMokaPlugin ────────────────────────────────────────────────────────────

	/// <inheritdoc />
	public string Id => "mokadocs-blazor-preview";

	/// <inheritdoc />
	public string Name => "Blazor Component Preview";

	/// <inheritdoc />
	public string Version => "3.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("Blazor preview plugin initialized - interactive in-page hydration mode");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		if (!_servicesInitialized)
		{
			// No preview blocks, no preview host needed. Setting one up regardless made every
			// site that declared the plugin but had no blocks report a missing preview host.
			if (!buildContext.Pages.Any(p =>
				    p.Content.Html?.Contains(BlazorPreviewExtension.ContainerOpenTag, StringComparison.Ordinal) == true))
			{
				return;
			}

			if (!await InitializeFromOptionsAsync(context, buildContext, ct))
			{
				return; // Hard failure already logged.
			}

			_servicesInitialized = true;
		}

		if (_compilationService is null)
		{
			return;
		}

		// Staged on every build, even one without preview blocks, so /_preview-wasm/ is always
		// there. A host republished while serve runs gets hardened again here.
		bool interactive = _previewHostWwwroot is not null && StagePreviewHost(_previewHostWwwroot, buildContext, context);

		// The .nojekyll marker this plugin's underscore-prefixed output depends on
		// (_preview-wasm, _preview-assemblies, _framework, _content) is written by
		// OutputPhase for every build. It used to be emitted here, which meant sites
		// without a published preview host shipped without it and lost _theme/ on
		// branch-based GitHub Pages deployments.

		// Resolve base path for the `?assembly=...` iframe query parameter. The Scriban
		// template engine's RewriteContentLinks regex only matches `href="/..."` and
		// `src="/..."` at the attribute OPENING - it does NOT rewrite `/` characters
		// inside query strings. So while the iframe src itself (`src="/_preview-wasm/..."`)
		// gets basePath-prefixed automatically during render, the assembly path buried
		// in the query string (`?assembly=/_preview-assemblies/...`) does not. We have to
		// prefix it ourselves at emission time so GitHub Pages project-page deployments
		// (e.g. /Moka.Red/) resolve the DLL correctly.
		string rawBase = buildContext.Config.Build.BasePath ?? "/";
		_basePathForAssemblyQuery = rawBase == "/" || string.IsNullOrEmpty(rawBase)
			? ""
			: "/" + rawBase.Trim('/');

		ILoggerFactory loggerFactory = context.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
		await using ServiceProvider renderServices = CreateRenderServices(loggerFactory, _knownDllPaths);
		var snapshotRenderer = new SnapshotRenderer(renderServices, loggerFactory, _renderTimeout);

		int pagesWithPreview = 0;
		int blocksCompiled = 0;
		int blocksFailed = 0;

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html) ||
			    !html.Contains(BlazorPreviewExtension.ContainerOpenTag, StringComparison.Ordinal))
			{
				continue;
			}

			(string rewrittenHtml, int pageCompiled, int pageFailed) = await CompileAndRewritePageAsync(
				page.Route, html, _compilationService, _extraUsings, _knownDllPaths, snapshotRenderer,
				buildContext, context, _basePathForAssemblyQuery, interactive, ct);

			page.Content = page.Content with
			{
				Html = PreviewPageAssets.Link(rewrittenHtml)
			};

			pagesWithPreview++;
			blocksCompiled += pageCompiled;
			blocksFailed += pageFailed;
		}

		if (pagesWithPreview > 0)
		{
			buildContext.DeferredOutputFiles[PreviewPageAssets.StylesheetPath] = PreviewPageAssets.Stylesheet;
			buildContext.DeferredOutputFiles[PreviewPageAssets.ScriptPath] = PreviewPageAssets.Script;
		}

		if (pagesWithPreview > 0 || blocksFailed > 0)
		{
			context.LogInfo(
				$"Blazor preview plugin: {pagesWithPreview} page(s), {blocksCompiled} block(s) compiled, {blocksFailed} failed");
		}
	}

	// ── One-time initialization: parse options, resolve preview-host, build compile refs ──

	private async Task<bool> InitializeFromOptionsAsync(IPluginContext context, BuildContext buildContext,
		CancellationToken ct)
	{
		// ── previewHost option (optional, auto-discovered when omitted) ─────
		string? hostPathRaw = null;
		if (context.Options.TryGetValue("previewHost", out object? hostObj)
		    && hostObj is string hostStr
		    && !string.IsNullOrWhiteSpace(hostStr))
		{
			hostPathRaw = hostStr;
		}

		// ── library option (used by the auto-scaffolded preview-host) ───────
		(string Id, string Version)? library = null;
		if (context.Options.TryGetValue("library", out object? libObj)
		    && libObj is string libStr
		    && !string.IsNullOrWhiteSpace(libStr))
		{
			// The id and version go into the scaffolded csproj, where a quote or a $( would
			// rewrite the project.
			if (NuGetPackageResolver.TryParsePackageSpec(libStr, out string libraryId, out string? libraryVersion))
			{
				library = (libraryId, libraryVersion ?? "*");
			}
			else
			{
				context.LogWarning(
					$"mokadocs-blazor-preview: library '{libStr}' is not a NuGet package id with an optional " +
					"@version (for example MyLibrary@1.2.3). It was ignored, so a scaffolded preview host " +
					"doesn't reference it.");
			}
		}

		TimeSpan publishTimeout = ReadSeconds(context, "publishTimeout", _defaultPublishTimeout);
		_renderTimeout = ReadSeconds(context, "renderTimeout", _defaultRenderTimeout);
		bool publishHost = ReadBoolean(context, "publishHost", true);

		string? previewHostDir;
		string? wwwroot;
		if (publishHost)
		{
			// Resolve the preview-host directory: explicit option wins, otherwise scan
			// conventional locations (./preview-host/, ./docs-preview-host/, or any subdir
			// containing a Microsoft.NET.Sdk.BlazorWebAssembly csproj).
			previewHostDir = ResolvePreviewHostDirectory(hostPathRaw, library is not null, buildContext.RootDirectory);

			// Auto-scaffold a fresh preview-host project from a generic template if the
			// resolved directory doesn't yet contain a Blazor WASM csproj. The user owns the
			// scaffolded files thereafter - mokadocs never overwrites them.
			if (!HasBlazorWasmCsproj(previewHostDir))
			{
				// Both the scaffold and dotnet publish need a real directory, and a build over a
				// virtual file system can have a root that isn't on disk.
				if (!Directory.Exists(buildContext.RootDirectory))
				{
					context.LogError(
						$"mokadocs-blazor-preview: no Blazor WebAssembly preview-host project found at '{previewHostDir}', " +
						$"and the build root '{buildContext.RootDirectory}' is not a directory on disk, so the plugin " +
						"can't scaffold one.");
					return false;
				}

				// Without a library the scaffold references only Moka.Blazor.Repl.Host, which is all
				// plain Razor previews need. This used to fail the build and ask for a library.
				ScaffoldPreviewHost(previewHostDir, library, context);
			}

			// Run `dotnet publish` on the preview-host (incremental - skipped when nothing
			// upstream of the published wwwroot has changed since the last build).
			if (!await EnsurePreviewHostPublishedAsync(previewHostDir, buildContext.OutputDirectory, publishTimeout,
				    context, ct))
			{
				return false;
			}

			// The folder dotnet publish just wrote. Searching publish-output for other layouts
			// could pick an older publish of the same project.
			wwwroot = Path.Combine(PublishOutputDirectory(previewHostDir), "wwwroot");
		}
		else if (hostPathRaw is not null)
		{
			previewHostDir = Path.GetFullPath(Path.Combine(buildContext.RootDirectory, hostPathRaw));
			wwwroot = FindPublishedWwwroot(previewHostDir);
			if (wwwroot is null)
			{
				context.LogWarning(
					$"mokadocs-blazor-preview: publishHost is off and '{previewHostDir}' holds no published Blazor " +
					"WebAssembly app (a wwwroot with _framework/blazor.webassembly.js under publish-output/, " +
					"publish-output/{tfm}/, or the folder itself), so previews show only their static render. " +
					"Publish the preview host there, or turn publishHost on.");
			}
		}
		else
		{
			previewHostDir = null;
			wwwroot = null;
			context.LogWarning(
				"mokadocs-blazor-preview: publishHost is off and no previewHost is set, so previews show only " +
				"their static render. Set previewHost to a published preview host to make them interactive.");
		}

		if (wwwroot is not null && buildContext.DryRun && !IsFingerprinted(wwwroot))
		{
			context.LogWarning(
				$"mokadocs-blazor-preview: the preview host at '{previewHostDir}' doesn't fingerprint or preload its " +
				"framework files, so browsers revalidate them on every preview. Set " +
				"<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders> in its .csproj and, in " +
				"wwwroot/index.html, add <link rel=\"preload\" id=\"webassembly\" /> and <script type=\"importmap\"></script> " +
				"to the head and load _framework/blazor.webassembly#[.{fingerprint}].js. Hosts mokadocs generates " +
				"now do this; existing ones are never rewritten.");
		}

		_previewHostWwwroot = wwwroot;

		string? refBinDir = previewHostDir is null ? null : FindReferenceBinDirectory(previewHostDir);
		if (refBinDir is null && publishHost)
		{
			context.LogError(
				$"mokadocs-blazor-preview: could not find a compiled bin/Release/net*.0 directory under '{previewHostDir}'. " +
				"Run `dotnet build -c Release` on the preview-host project so Roslyn can locate reference assemblies.");
			return false;
		}

		// ── Roslyn compilation service ────────────────────────────────────────
		// Collect all candidate DLLs into a dictionary keyed by assembly simple-name first,
		// so that (a) duplicate-named assemblies from different bin dirs don't produce
		// CS1704, and (b) entries added LATER override earlier ones. Preview-host bin goes
		// in first; additive `references:` entries below overwrite same-named copies.
		// Without a host bin (publishHost off), blocks compile against the framework and the
		// references option only.
		_compilationService = new RoslynCompilationService(_httpClient);
		var dllByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// The preview-host's bin dir holds every transitive assembly it references, including
		// framework assemblies (Microsoft.*, System.*) that Roslyn already supplies via the
		// host runtime. Adding those causes CS0433 type-conflict errors when the TFMs differ
		// (e.g. preview-host built against net9, mokadocs runs on net10). Only include the
		// NON-framework assemblies - i.e. the consumer's own component library DLLs.
		foreach (string dll in refBinDir is null ? [] : Directory.GetFiles(refBinDir, "*.dll"))
		{
			string name = Path.GetFileNameWithoutExtension(dll);
			if (IsFrameworkAssemblyName(name))
			{
				continue;
			}

			dllByName[name] = dll;
		}

		// ── additional references (additive, override by simple-name) ───────
		// The preview-host's bin/Release contains assemblies resolved at that project's build
		// time, which may be NuGet packages rather than the user's local source. The additive
		// `references:` option lets the user pull in extra bin directories (e.g. a sibling
		// Moka.Red/src/Moka.Red/bin/Debug/net10.0) so Roslyn resolves against the latest API.
		// Same-named entries added here OVERRIDE the preview-host copies to avoid CS1704.
		if (context.Options.TryGetValue("references", out object? refsObj)
		    && refsObj is IEnumerable<object> refList)
		{
			foreach (string refEntry in refList
				         .Select(o => o.ToString()!)
				         .Where(s => !string.IsNullOrWhiteSpace(s)))
			{
				string resolved = Path.GetFullPath(Path.Combine(buildContext.RootDirectory, refEntry));
				if (Directory.Exists(resolved))
				{
					foreach (string dll in Directory.GetFiles(resolved, "*.dll"))
					{
						string name = Path.GetFileNameWithoutExtension(dll);
						if (IsFrameworkAssemblyName(name))
						{
							continue;
						}

						dllByName[name] = dll;
					}
				}
				else if (File.Exists(resolved))
				{
					string name = Path.GetFileNameWithoutExtension(resolved);
					if (!IsFrameworkAssemblyName(name))
					{
						dllByName[name] = resolved;
					}
				}
			}
		}

		// Now add the deduplicated set to Roslyn and _knownDllPaths.
		foreach (string dll in dllByName.Values)
		{
			try
			{
				_compilationService.AddReference(MetadataReference.CreateFromFile(dll));
				_knownDllPaths.Add(dll);
			}
			catch
			{
				// Skip unloadable DLLs silently (satellite resources, etc.)
			}
		}

		context.LogInfo(
			$"mokadocs-blazor-preview: loaded {_knownDllPaths.Count} unique reference assemblies.");

		// ── usings option ─────────────────────────────────────────────────────
		if (context.Options.TryGetValue("usings", out object? usingsObj)
		    && usingsObj is IEnumerable<object> usingsList)
		{
			_extraUsings.AddRange(usingsList
				.Select(o => o.ToString()!)
				.Where(s => !string.IsNullOrWhiteSpace(s)));
		}

		return true;
	}

	/// <summary>
	///     Resolves the preview-host project directory from yaml options or convention.
	///     <para>
	///         Resolution order:
	///         <list type="number">
	///             <item>
	///                 <description>Explicit <c>previewHost:</c> yaml option (relative to docs root)</description>
	///             </item>
	///             <item>
	///                 <description>
	///                     <c>./preview-host/</c>
	///                 </description>
	///             </item>
	///             <item>
	///                 <description>
	///                     <c>./docs-preview-host/</c>
	///                 </description>
	///             </item>
	///             <item>
	///                 <description>
	///                     Any subdirectory under the docs root that already contains a
	///                     <c>Microsoft.NET.Sdk.BlazorWebAssembly</c> csproj
	///                 </description>
	///             </item>
	///         </list>
	///     </para>
	///     <para>
	///         If nothing is found, returns the scaffold target: <c>./preview-host/</c> when
	///         <paramref name="hasLibrary" /> is set, otherwise <c>./.mokadocs/preview-host/</c>
	///         (<see cref="_generatedHostDirectory" />). The caller scaffolds a project there
	///         unless one already exists.
	///     </para>
	/// </summary>
	private static string ResolvePreviewHostDirectory(string? explicitPath, bool hasLibrary, string rootDir)
	{
		if (!string.IsNullOrWhiteSpace(explicitPath))
		{
			return Path.GetFullPath(Path.Combine(rootDir, explicitPath));
		}

		string[] conventional = ["preview-host", "docs-preview-host"];
		foreach (string name in conventional)
		{
			string candidate = Path.Combine(rootDir, name);
			if (HasBlazorWasmCsproj(candidate))
			{
				return Path.GetFullPath(candidate);
			}
		}

		// Last-resort scan: any direct subdirectory holding a Blazor WASM csproj.
		if (Directory.Exists(rootDir))
		{
			foreach (string sub in Directory.GetDirectories(rootDir))
			{
				if (HasBlazorWasmCsproj(sub))
				{
					return Path.GetFullPath(sub);
				}
			}
		}

		// Default scaffold target if nothing found.
		return Path.GetFullPath(hasLibrary
			? Path.Combine(rootDir, conventional[0])
			: Path.Combine(rootDir, _generatedHostDirectory));
	}

	/// <summary>
	///     <c>true</c> when <paramref name="dir" /> exists and contains at least one
	///     <c>*.csproj</c> using the <c>Microsoft.NET.Sdk.BlazorWebAssembly</c> SDK.
	/// </summary>
	private static bool HasBlazorWasmCsproj(string dir)
	{
		if (!Directory.Exists(dir))
		{
			return false;
		}

		foreach (string csproj in Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly))
		{
			try
			{
				string content = File.ReadAllText(csproj);
				if (content.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			catch
			{
				// Skip unreadable files
			}
		}

		return false;
	}

	/// <summary>
	///     Writes a fresh, library-agnostic Blazor WebAssembly preview-host project (csproj +
	///     Program.cs + wwwroot/index.html) into <paramref name="dir" />. With a
	///     <paramref name="library" />, the consumer's library package id and version go into
	///     the csproj's <c>PackageReference</c> and into the index.html customization markers;
	///     without one, the csproj references only <c>Moka.Blazor.Repl.Host</c>. Files are written
	///     ONLY when they do not already exist - mokadocs never overwrites user edits.
	/// </summary>
	/// <param name="dir">The preview-host directory.</param>
	/// <param name="library">
	///     The library's package id and version, already checked by
	///     <see cref="NuGetPackageResolver.TryParsePackageSpec" />, or <c>null</c>.
	/// </param>
	/// <param name="context">The plugin context, for logging.</param>
	private static void ScaffoldPreviewHost(string dir, (string Id, string Version)? library, IPluginContext context)
	{
		Directory.CreateDirectory(dir);
		Directory.CreateDirectory(Path.Combine(dir, "wwwroot"));

		string csprojPath = Path.Combine(dir, "DocsPreviewHost.csproj");
		string programPath = Path.Combine(dir, "Program.cs");
		string indexPath = Path.Combine(dir, "wwwroot", "index.html");

		// Empty Directory.Build.props/.targets and Directory.Packages.props in the
		// preview-host directory shadow any parent repo's central files. MSBuild walks
		// UP the directory tree and stops at the first match - so by dropping empty
		// markers here, the preview-host becomes hermetic and inherits nothing from
		// the consumer's parent build configuration (multi-target overrides, central
		// package management, FrameworkReference stripping, etc.).
		string buildPropsPath = Path.Combine(dir, "Directory.Build.props");
		string buildTargetsPath = Path.Combine(dir, "Directory.Build.targets");
		string packagesPropsPath = Path.Combine(dir, "Directory.Packages.props");

		string libraryReference = library is { } lib
			? _scaffoldLibraryReferenceTemplate
				.Replace("{LIBRARY_ID}", lib.Id, StringComparison.Ordinal)
				.Replace("{LIBRARY_VERSION}", lib.Version, StringComparison.Ordinal)
			: "";

		string csproj = _scaffoldCsprojTemplate
			.Replace("{LIBRARY_REFERENCE}", libraryReference, StringComparison.Ordinal)
			.Replace("{HOST_VERSION}", _hostPackageVersion, StringComparison.Ordinal);

		string program = _scaffoldProgramTemplate;
		string indexHtml = _scaffoldIndexHtmlTemplate
			.Replace("{LIBRARY_ID}", library?.Id ?? "YourLibrary", StringComparison.Ordinal);

		bool any = false;
		if (!File.Exists(csprojPath))
		{
			File.WriteAllText(csprojPath, csproj);
			any = true;
		}

		if (!File.Exists(programPath))
		{
			File.WriteAllText(programPath, program);
			any = true;
		}

		if (!File.Exists(indexPath))
		{
			File.WriteAllText(indexPath, indexHtml);
			any = true;
		}

		const string emptyProject = "<Project>\n</Project>\n";
		if (!File.Exists(buildPropsPath))
		{
			File.WriteAllText(buildPropsPath, emptyProject);
			any = true;
		}

		if (!File.Exists(buildTargetsPath))
		{
			File.WriteAllText(buildTargetsPath, emptyProject);
			any = true;
		}

		if (!File.Exists(packagesPropsPath))
		{
			File.WriteAllText(packagesPropsPath, emptyProject);
			any = true;
		}

		if (any)
		{
			string libraryText = library is null
				? "no component library"
				: $"library: {library.Value.Id}@{library.Value.Version}";
			context.LogInfo(
				$"mokadocs-blazor-preview: scaffolded preview-host at '{dir}' " +
				$"({libraryText}, host: Moka.Blazor.Repl.Host@{_hostPackageVersion}). " +
				"Edit Program.cs / wwwroot/index.html to add your services and CSS - mokadocs will not overwrite them.");
		}
	}

	/// <summary>
	///     Reads a timeout option given in seconds, falling back to <paramref name="fallback" />
	///     with a warning when the value isn't a positive number.
	/// </summary>
	private static TimeSpan ReadSeconds(IPluginContext context, string option, TimeSpan fallback)
	{
		if (!context.Options.TryGetValue(option, out object? value) || value is null)
		{
			return fallback;
		}

		string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
		    && seconds > 0 && seconds <= _maxTimeoutSeconds)
		{
			return TimeSpan.FromSeconds(seconds);
		}

		context.LogWarning(
			$"mokadocs-blazor-preview: {option} '{text}' is not a number of seconds between 0 and " +
			$"{_maxTimeoutSeconds}; using {fallback.TotalSeconds:0}.");
		return fallback;
	}

	/// <summary>
	///     Reads a yes/no option: a boolean from code, or <c>true</c>/<c>false</c> text from yaml.
	///     Anything else falls back to <paramref name="fallback" /> with a warning.
	/// </summary>
	private static bool ReadBoolean(IPluginContext context, string option, bool fallback)
	{
		if (!context.Options.TryGetValue(option, out object? value) || value is null)
		{
			return fallback;
		}

		if (value is bool flag)
		{
			return flag;
		}

		string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
		if (bool.TryParse(text.Trim(), out bool parsed))
		{
			return parsed;
		}

		context.LogWarning(
			$"mokadocs-blazor-preview: {option} '{text}' is not true or false; using {(fallback ? "true" : "false")}.");
		return fallback;
	}

	/// <summary>Where the plugin publishes a preview host: <c>publish-output/net10.0/</c>.</summary>
	private static string PublishOutputDirectory(string previewHostDir) =>
		Path.Combine(previewHostDir, "publish-output", "net10.0");

	/// <summary>
	///     The Blazor script of a published wwwroot: <c>_framework/blazor.webassembly.js</c>, or the
	///     fingerprinted <c>blazor.webassembly.{fingerprint}.js</c> a host with
	///     <c>OverrideHtmlAssetPlaceholders</c> publishes instead. <c>null</c> when there is neither.
	/// </summary>
	internal static string? FindBlazorScript(string wwwroot)
	{
		string framework = Path.Combine(wwwroot, "_framework");
		if (!Directory.Exists(framework))
		{
			return null;
		}

		string plain = Path.Combine(framework, "blazor.webassembly.js");
		return File.Exists(plain)
			? plain
			: Directory.EnumerateFiles(framework, "blazor.webassembly.*.js").FirstOrDefault(IsFingerprintedBlazorScript);
	}

	/// <summary>
	///     Whether the published wwwroot loads a fingerprinted Blazor script, which only a host with
	///     <c>OverrideHtmlAssetPlaceholders</c> and the matching index.html placeholders publishes.
	/// </summary>
	private static bool IsFingerprinted(string wwwroot) =>
		FindBlazorScript(wwwroot) is { } script && IsFingerprintedBlazorScript(script);

	private static bool IsFingerprintedBlazorScript(string path)
	{
		string name = Path.GetFileName(path);
		const string prefix = "blazor.webassembly.";
		return name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".js", StringComparison.Ordinal)
		                                                         && name.Length > prefix.Length + ".js".Length
		                                                         && name[prefix.Length..^".js".Length].All(char.IsAsciiLetterOrDigit);
	}

	/// <summary>
	///     Stages the published preview host under <c>_preview-wasm/</c> with its pages hardened
	///     (<see cref="PreviewHostStaging" />), and the site-root <c>.gitattributes</c> branch
	///     deploys need for it. Returns <c>false</c>, with a warning, when the host can't be
	///     staged; the build's previews then show their static render.
	/// </summary>
	private static bool StagePreviewHost(string wwwroot, BuildContext buildContext, IPluginContext context)
	{
		if (!File.Exists(Path.Combine(wwwroot, "index.html")))
		{
			context.LogWarning(
				$"mokadocs-blazor-preview: the published preview host '{wwwroot}' has no index.html, so previews " +
				"show only their static render.");
			return false;
		}

		PreviewHostStage stage;
		try
		{
			stage = PreviewHostStaging.Read(wwwroot);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			context.LogWarning(
				$"mokadocs-blazor-preview: the published preview host '{wwwroot}' could not be read ({ex.Message}), " +
				"so previews show only their static render.");
			return false;
		}

		foreach (string page in stage.PagesWithOwnPolicy)
		{
			context.LogWarning(
				$"mokadocs-blazor-preview: the preview host's {page} has a Content-Security-Policy of its own. The " +
				"browser enforces it next to the one mokadocs adds, so it has to allow the inline scripts mokadocs " +
				"adds (their hashes are in the added policy), or previews stop loading.");
		}

		foreach ((string path, byte[] bytes) in stage.Files)
		{
			buildContext.DeferredOutputFiles[path] = bytes;
		}

		buildContext.DeferredOutputDirectories.AddRange(stage.Directories);
		buildContext.DeferredOutputFiles[PreviewHostStaging.GitAttributesPath] = Encoding.UTF8.GetBytes(PreviewHostStaging.GitAttributes);
		return true;
	}

	/// <summary>
	///     Runs <c>dotnet publish -c Release -f net10.0 -o publish-output/net10.0</c> on the
	///     preview-host project if its publish output is missing or stale relative to its inputs.
	///     The folder is emptied first, so files a previous publish wrote don't ship with the new
	///     one. Returns <c>false</c> on hard failure (logged via <paramref name="context" />).
	/// </summary>
	/// <param name="previewHostDir">The preview-host project directory.</param>
	/// <param name="outputDirectory">The site's output directory, which never counts as an input.</param>
	/// <param name="timeout">How long the publish may take before it is stopped.</param>
	/// <param name="context">The plugin context, for logging.</param>
	/// <param name="ct">The build's cancellation token; cancelling it stops the publish.</param>
	private static async Task<bool> EnsurePreviewHostPublishedAsync(string previewHostDir, string outputDirectory,
		TimeSpan timeout, IPluginContext context, CancellationToken ct)
	{
		string? csproj = Directory.GetFiles(previewHostDir, "*.csproj", SearchOption.TopDirectoryOnly)
			.FirstOrDefault();
		if (csproj is null)
		{
			context.LogError($"mokadocs-blazor-preview: no .csproj found in '{previewHostDir}'.");
			return false;
		}

		string publishOut = PublishOutputDirectory(previewHostDir);
		string publishedWwwroot = Path.Combine(publishOut, "wwwroot");
		string publishStamp = Path.Combine(publishOut, _publishStampFile);

		if (FindBlazorScript(publishedWwwroot) is { } publishedScript)
		{
			DateTime publishedAt = File.GetLastWriteTimeUtc(publishedScript);
			if (File.Exists(publishStamp) && File.GetLastWriteTimeUtc(publishStamp) > publishedAt)
			{
				publishedAt = File.GetLastWriteTimeUtc(publishStamp);
			}

			DateTime newestInput = GetNewestInputMtime(previewHostDir);
			DateTime newestReference = GetNewestReferencedProjectMtime(csproj, previewHostDir, outputDirectory);
			if (newestInput <= publishedAt && newestReference <= publishedAt)
			{
				return true;
			}
		}

		context.LogInfo(
			$"mokadocs-blazor-preview: publishing preview-host '{Path.GetFileName(csproj)}' " +
			$"(this happens once per change to the project; subsequent builds are skipped)...");

		// dotnet publish only adds and replaces files, so without this a renamed or removed
		// assembly from an earlier publish shipped with every later one.
		try
		{
			if (Directory.Exists(publishOut))
			{
				Directory.Delete(publishOut, true);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			context.LogWarning(
				$"mokadocs-blazor-preview: could not empty '{publishOut}' before publishing ({ex.Message}), so files " +
				"from an earlier publish may ship with this one.");
		}

		var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = previewHostDir };
		foreach (string argument in (string[]) ["publish", csproj, "-c", "Release", "-f", "net10.0", "-o", publishOut])
		{
			psi.ArgumentList.Add(argument);
		}

		DateTime startedAt = DateTime.UtcNow;
		try
		{
			ChildProcessResult result = await ChildProcessRunner.RunAsync(psi, timeout, ct);
			if (result.TimedOut)
			{
				context.LogError(
					$"mokadocs-blazor-preview: `dotnet publish` did not finish within {timeout.TotalSeconds:0} seconds " +
					"and was stopped. Run it yourself to see where it hangs, or raise the publishTimeout option " +
					"if the first publish needs longer to restore packages.");
				return false;
			}

			if (result.ExitCode != 0)
			{
				context.LogError(
					$"mokadocs-blazor-preview: `dotnet publish` exited with code {result.ExitCode}.\n" +
					(string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error));
				return false;
			}

			if (FindBlazorScript(publishedWwwroot) is null)
			{
				context.LogError(
					$"mokadocs-blazor-preview: publish completed but '{publishedWwwroot}' has no " +
					"_framework/blazor.webassembly.js. Check your preview-host project for build errors.");
				return false;
			}

			await File.WriteAllTextAsync(publishStamp, "", ct);
			File.SetLastWriteTimeUtc(publishStamp, startedAt);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			context.LogError($"mokadocs-blazor-preview: `dotnet publish` failed: {ex.Message}");
			return false;
		}

		return true;
	}

	/// <summary>
	///     Newest <c>UtcLastWriteTime</c> of any source file under <paramref name="dir" />,
	///     ignoring <c>bin/</c>, <c>obj/</c>, and <c>publish-output/</c>. Used as the input
	///     timestamp against which the publish-output marker is compared for incremental skip.
	/// </summary>
	private static DateTime GetNewestInputMtime(string dir)
	{
		DateTime newest = DateTime.MinValue;
		foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
		{
			string rel = Path.GetRelativePath(dir, file).Replace('\\', '/');
			if (rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
			    || rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
			    || rel.StartsWith("publish-output/", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			DateTime t = File.GetLastWriteTimeUtc(file);
			if (t > newest)
			{
				newest = t;
			}
		}

		return newest;
	}

	/// <summary>
	///     Newest source file of the projects the preview host references with
	///     <c>ProjectReference</c>, followed transitively. A change to a referenced component
	///     library changes what the published host runs, so it makes the publish stale too.
	/// </summary>
	/// <remarks>
	///     Only source-like files count, and build output, dot folders, the preview host itself and
	///     the site output are skipped: a library project often sits at the root of the repository
	///     that also holds the docs, whose build output is rewritten on every build.
	/// </remarks>
	private static DateTime GetNewestReferencedProjectMtime(string hostCsproj, string previewHostDir,
		string outputDirectory)
	{
		var skipped = new HashSet<string>(PathComparer)
		{
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(previewHostDir)),
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(outputDirectory))
		};
		var scannedDirectories = new HashSet<string>(PathComparer);
		var visitedProjects = new HashSet<string>(PathComparer) { Path.GetFullPath(hostCsproj) };
		var pending = new Stack<string>(ReadProjectReferences(hostCsproj));
		DateTime newest = DateTime.MinValue;

		while (pending.TryPop(out string? project))
		{
			if (!visitedProjects.Add(project) || !File.Exists(project))
			{
				continue;
			}

			string projectDirectory = Path.GetDirectoryName(project)!;
			if (scannedDirectories.Add(projectDirectory))
			{
				DateTime projectNewest = GetNewestSourceMtime(projectDirectory, skipped);
				if (projectNewest > newest)
				{
					newest = projectNewest;
				}
			}

			foreach (string next in ReadProjectReferences(project))
			{
				pending.Push(next);
			}
		}

		return newest;
	}

	private static StringComparer PathComparer =>
		OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	private static IEnumerable<string> ReadProjectReferences(string csproj)
	{
		XDocument document;
		try
		{
			document = XDocument.Load(csproj);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			return [];
		}

		string directory = Path.GetDirectoryName(Path.GetFullPath(csproj))!;
		return document.Descendants()
			.Where(e => e.Name.LocalName == "ProjectReference")
			.Select(e => (string?)e.Attribute("Include") ?? "")
			.SelectMany(include => include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Where(include => !include.Contains('$', StringComparison.Ordinal) && !include.Contains('*', StringComparison.Ordinal))
			.Select(include => Path.GetFullPath(Path.Combine(directory,
				include.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))))
			.ToList();
	}

	private static DateTime GetNewestSourceMtime(string root, HashSet<string> skippedDirectories)
	{
		DateTime newest = DateTime.MinValue;
		var pending = new Stack<string>([root]);
		while (pending.TryPop(out string? directory))
		{
			try
			{
				foreach (string file in Directory.EnumerateFiles(directory))
				{
					if (_referencedSourceExtensions.Contains(Path.GetExtension(file)))
					{
						DateTime t = File.GetLastWriteTimeUtc(file);
						if (t > newest)
						{
							newest = t;
						}
					}
				}

				foreach (string child in Directory.EnumerateDirectories(directory))
				{
					string name = Path.GetFileName(child);
					if (name.StartsWith('.') || _skippedReferenceDirectoryNames.Contains(name)
					                         || skippedDirectories.Contains(child))
					{
						continue;
					}

					pending.Push(child);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// An unreadable folder can't hold an edit the publish would pick up either.
			}
		}

		return newest;
	}

	/// <summary>
	///     Returns <c>true</c> if an assembly with the given simple-name is already resolvable
	///     by the host runtime - i.e. it is part of the .NET shared framework (BCL, ASP.NET Core,
	///     Extensions, JSInterop) that the mokadocs tool itself is running on top of.
	///     <para>
	///         The base <see cref="RoslynCompilationService" /> already seeds its compilation with
	///         these host-runtime framework assemblies. Re-adding duplicates from a preview-host's
	///         bin directory (which may have been built against a different TFM) causes CS0433 /
	///         CS1704 type-conflict errors. This check works identically on .NET 9 and .NET 10 -
	///         whichever runtime mokadocs is executing on is the reference version used.
	///     </para>
	///     <para>
	///         The runtime's trusted platform assembly list says where it would load the name from,
	///         and the assembly counts when that file sits in a shared framework folder. Nothing is
	///         loaded to find out.
	///     </para>
	/// </summary>
	internal static bool IsFrameworkAssemblyName(string simpleName)
	{
		if (string.IsNullOrEmpty(simpleName))
		{
			return false;
		}

		if (simpleName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
		    || simpleName.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return _sharedFrameworkRoot.Value is { } root
		       && _trustedPlatformAssemblies.Value.TryGetValue(simpleName, out string? location)
		       && IsInSharedFramework(location, root);
	}

	/// <summary>
	///     The folder that holds the shared frameworks the runtime runs on, found from where
	///     <c>System.Private.CoreLib</c> was loaded: <c>{dotnet root}/shared</c> for a
	///     framework-dependent app (whatever the install root is: Program Files, ~/.dotnet, snap,
	///     Homebrew), or the app's own folder for a self-contained one.
	/// </summary>
	/// <param name="coreLibLocation">The path of <c>System.Private.CoreLib.dll</c>.</param>
	/// <returns>The folder, or <c>null</c> when the runtime has no file location (single-file apps).</returns>
	internal static string? SharedFrameworkRoot(string coreLibLocation)
	{
		if (string.IsNullOrEmpty(coreLibLocation))
		{
			return null;
		}

		DirectoryInfo? versionDirectory = new FileInfo(coreLibLocation).Directory;
		if (versionDirectory?.Parent is { Name: "Microsoft.NETCore.App" } framework && framework.Parent is { } shared)
		{
			return shared.FullName;
		}

		return versionDirectory?.FullName;
	}

	/// <summary>Whether <paramref name="assemblyLocation" /> is a file under <paramref name="sharedFrameworkRoot" />.</summary>
	internal static bool IsInSharedFramework(string assemblyLocation, string sharedFrameworkRoot)
	{
		string relative = Path.GetRelativePath(sharedFrameworkRoot, assemblyLocation);
		return relative != "." && !Path.IsPathRooted(relative)
		                       && !relative.Equals("..", StringComparison.Ordinal)
		                       && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
	}

	private static Dictionary<string, string> ReadTrustedPlatformAssemblies()
	{
		var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
		foreach (string path in paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			byName.TryAdd(Path.GetFileNameWithoutExtension(path), path);
		}

		return byName;
	}

	/// <summary>
	///     Finds the published wwwroot of a preview host the plugin doesn't publish itself
	///     (<c>publishHost</c> off): <c>publish-output/wwwroot/</c>,
	///     <c>publish-output/{tfm}/wwwroot/</c>, or <paramref name="previewHostDir" /> itself or its
	///     <c>wwwroot/</c> when it points at published output. A candidate needs the Blazor script
	///     in <c>_framework/</c>; of several, the most recently published wins.
	/// </summary>
	internal static string? FindPublishedWwwroot(string previewHostDir)
	{
		var candidates = new List<string> { previewHostDir, Path.Combine(previewHostDir, "wwwroot") };
		string publishRoot = Path.Combine(previewHostDir, "publish-output");
		if (Directory.Exists(publishRoot))
		{
			candidates.Add(Path.Combine(publishRoot, "wwwroot"));
			candidates.AddRange(Directory.GetDirectories(publishRoot)
				.Where(d => Path.GetFileName(d).StartsWith("net", StringComparison.OrdinalIgnoreCase))
				.Select(d => Path.Combine(d, "wwwroot")));
		}

		return candidates
			.Select(dir => (Dir: dir, Script: FindBlazorScript(dir)))
			.Where(c => c.Script is not null)
			.OrderByDescending(c => File.GetLastWriteTimeUtc(c.Script!))
			.Select(c => c.Dir)
			.FirstOrDefault();
	}

	/// <summary>
	///     Looks for <c>{previewHostDir}/bin/Release/net*.0/</c> - the Blazor WASM project's
	///     intermediate output directory, which contains normal .dll files suitable as Roslyn
	///     references (unlike the published <c>_framework/*.wasm</c> files which are WebCIL).
	///     Prefers the highest TFM present.
	/// </summary>
	private static string? FindReferenceBinDirectory(string previewHostDir)
	{
		string releaseDir = Path.Combine(previewHostDir, "bin", "Release");
		if (!Directory.Exists(releaseDir))
		{
			return null;
		}

		var candidates = Directory.GetDirectories(releaseDir)
			.Select(d => (Dir: d, Version: TargetFrameworks.LoadableVersion(Path.GetFileName(d), int.MaxValue)))
			.Where(x => x.Version is not null && Directory.GetFiles(x.Dir, "*.dll").Length > 0)
			.OrderByDescending(x => x.Version)
			.ToList();

		// The highest framework this runtime runs, else the highest there is. Sorted by name,
		// net9.0 won over net10.0 even when mokadocs ran on .NET 10.
		return candidates.Where(x => x.Version!.Major <= Environment.Version.Major).Select(x => x.Dir).FirstOrDefault()
		       ?? candidates.Select(x => x.Dir).FirstOrDefault();
	}

	// ── HtmlRenderer (for SSR fallback) ────────────────────────────────────────

	private static ServiceProvider CreateRenderServices(ILoggerFactory loggerFactory, IEnumerable<string> knownDllPaths)
	{
		foreach (string dll in knownDllPaths)
		{
			try
			{
				_ = Assembly.LoadFrom(dll);
			}
			catch
			{
				/* ignore */
			}
		}

		var services = new ServiceCollection();
		services.AddSingleton(loggerFactory);
		services.AddSingleton<IJSRuntime>(NullJsRuntime.Instance);
		services.AddSingleton<NavigationManager>(NullNavigationManager.Instance);

		// Note: no library-specific service stubs are registered here. mokadocs is a generic tool;
		// any injectable services a consumer's components require at SSR time must be provided by
		// the consumer via their preview-host assemblies (the host renders via DynamicComponent,
		// not via a pre-configured DI container). If a component injects a service that cannot
		// be resolved, SSR falls back to the empty render and the iframe still shows the live
		// interactive component.

		return services.BuildServiceProvider();
	}

	// ── Per-page block compilation + rewrite ───────────────────────────────────

	private static async Task<(string html, int compiled, int failed)> CompileAndRewritePageAsync(
		string pageRoute,
		string html,
		RoslynCompilationService compilationService,
		List<string> extraUsings,
		List<string> knownDllPaths,
		SnapshotRenderer snapshotRenderer,
		BuildContext buildContext,
		IPluginContext context,
		string basePathForAssemblyQuery,
		bool interactive,
		CancellationToken ct)
	{
		const string sourceStart = "<div class=\"blazor-preview-source\">";
		const string renderMarker = "<div class=\"blazor-preview-render\"></div>";

		int searchPos = 0;
		int blockIndex = 0;
		int compiled = 0;
		int failed = 0;
		var sb = new StringBuilder(html.Length);
		var accumulatedCodeBlocks = new List<string>();

		while (true)
		{
			int renderIdx = html.IndexOf(renderMarker, searchPos, StringComparison.Ordinal);
			if (renderIdx < 0)
			{
				sb.Append(html, searchPos, html.Length - searchPos);
				break;
			}

			int sourceIdx = html.LastIndexOf(sourceStart, renderIdx, StringComparison.Ordinal);
			string? source = null;
			if (sourceIdx >= 0)
			{
				int codeStart = html.IndexOf("<code", sourceIdx, StringComparison.Ordinal);
				if (codeStart >= 0)
				{
					codeStart = html.IndexOf('>', codeStart) + 1;
					int codeEnd = html.IndexOf("</code>", codeStart, StringComparison.Ordinal);
					if (codeEnd > codeStart)
					{
						source = WebUtility.HtmlDecode(html[codeStart..codeEnd]);
					}
				}
			}

			sb.Append(html, searchPos, renderIdx - searchPos);

			if (source is not null)
			{
				blockIndex++;
				int prevAccumCount = accumulatedCodeBlocks.Count;
				string extracted = ExtractCodeBlocks(source);
				if (!string.IsNullOrWhiteSpace(extracted))
				{
					accumulatedCodeBlocks.Add(extracted);
				}

				try
				{
					var project = new ReplProject { Name = "DocsPreview" };
					project.Files.Add(ProjectFile.CreateRazor("Preview.razor", source));
					foreach (string u in extraUsings)
					{
						project.GlobalUsings.Add(u);
					}

					CompilationResult result = await compilationService.CompileAsync(project, ct);

					// Retry with accumulated context from earlier snippets on the same page
					// (handles cross-snippet state dependencies).
					if (!result.Success && prevAccumCount > 0
					                    && result.Errors.All(e => e.Id is "CS0103" or "CS0246" or "CS0012"))
					{
						string preamble = string.Join("\n", accumulatedCodeBlocks.Take(prevAccumCount));
						var retryProject = new ReplProject { Name = "DocsPreview" };
						retryProject.Files.Add(ProjectFile.CreateRazor("Preview.razor", preamble + "\n" + source));
						foreach (string u in extraUsings)
						{
							retryProject.GlobalUsings.Add(u);
						}

						CompilationResult retryResult = await compilationService.CompileAsync(retryProject, ct);
						if (retryResult.Success || retryResult.Errors.Count() < result.Errors.Count())
						{
							result = retryResult;
						}
					}

					if (result.Success && result.AssemblyBytes is not null)
					{
						string entryPoint = result.EntryPointTypeName ?? "MokaRepl.Preview";

						// Render an SSR snapshot of the component as noscript fallback and for
						// search / link-preview crawlers. Shown in place of the iframe when there
						// is no preview host to run it.
						string ssrHtml;
						try
						{
							ssrHtml = await RenderComponentAsync(
								result.AssemblyBytes, result.EntryPointTypeName, knownDllPaths, snapshotRenderer,
								pageRoute, context, ct);
						}
						catch (Exception) when (!ct.IsCancellationRequested)
						{
							ssrHtml = "";
						}

						if (interactive)
						{
							AppendPreviewFrame(sb, result.AssemblyBytes, entryPoint, ssrHtml, basePathForAssemblyQuery,
								buildContext);
						}
						else
						{
							sb.Append("<div class=\"blazor-preview-render\"><div class=\"blazor-preview-ssr-fallback\">")
								.Append(ssrHtml)
								.Append("</div></div>");
						}

						compiled++;
					}
					else
					{
						string errors = string.Join("\n", result.Errors.Select(e =>
							$"[{e.Id}] {(e.FilePath != null ? $"{e.FilePath}({e.Line},{e.Column}): " : "")}{e.Message}"));
						sb.Append("<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">")
							.Append(WebUtility.HtmlEncode(errors))
							.Append("</div></div>");
						context.LogWarning($"Blazor preview compile errors:\n{errors}");
						failed++;
					}
				}
				catch (Exception ex) when (!ct.IsCancellationRequested)
				{
					context.LogWarning($"Blazor preview compile failed: {ex.Message}");
					sb.Append("<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">")
						.Append(WebUtility.HtmlEncode(ex.Message))
						.Append("</div></div>");
					failed++;
				}
			}
			else
			{
				sb.Append(renderMarker);
			}

			searchPos = renderIdx + renderMarker.Length;
		}

		return (sb.ToString(), compiled, failed);
	}

	/// <summary>
	///     Writes the iframe that runs a compiled preview, with its static render for readers
	///     without JavaScript, and stages the assembly.
	/// </summary>
	private static void AppendPreviewFrame(StringBuilder sb, byte[] assemblyBytes, string entryPoint, string ssrHtml,
		string basePathForAssemblyQuery, BuildContext buildContext)
	{
		// Named by the bytes, so one name always means one assembly and the host can cache it for good.
		string assemblyPath = $"_preview-assemblies/{Convert.ToHexStringLower(SHA256.HashData(assemblyBytes))}.bin";
		buildContext.DeferredOutputFiles[assemblyPath] = assemblyBytes;

		// One lazy-loaded iframe per preview. The preview-host WASM app reads ?assembly=… &entry=…
		// from its URL and mounts the component into its own static root (#app) via a plain
		// RenderFragment inside App.razor's [JSInvokable] LoadAssembly - no dynamic root components
		// API required.
		//
		// iframe `src` is a root-relative absolute path; the mokadocs Scriban template engine's
		// RewriteContentLinks auto-prefixes it with BasePath at render time. The `assembly` query
		// parameter, however, lives INSIDE the src attribute value, so the rewriter doesn't touch it -
		// we must prefix it ourselves here so GitHub Pages project-page deployments resolve it.
		//
		// No sandbox attribute: on a same-origin frame, allow-scripts with allow-same-origin lets the
		// frame lift its own sandbox, so it isolated nothing, and without allow-forms it blocked
		// EditForm submits in previews. The page's CSP and message guard (PreviewPageHardening) are
		// the boundary.
		string iframeSrc =
			$"/_preview-wasm/index.html?assembly={basePathForAssemblyQuery}/{assemblyPath}" +
			$"&amp;entry={WebUtility.HtmlEncode(Uri.EscapeDataString(entryPoint))}";

		sb.Append("<div class=\"blazor-preview-render\">")
			.Append("<iframe class=\"blazor-preview-iframe\" src=\"")
			.Append(iframeSrc)
			.Append("\" loading=\"lazy\" title=\"Blazor preview\"></iframe>")
			.Append("<noscript><div class=\"blazor-preview-ssr-fallback\">")
			.Append(ssrHtml)
			.Append("</div></noscript>")
			.Append("</div>");
	}

	/// <summary>Extracts <c>@code { ... }</c> blocks so they can be prepended to later snippets on the same page.</summary>
	private static string ExtractCodeBlocks(string razorSource)
	{
		var sb = new StringBuilder();
		int i = 0;
		while (i < razorSource.Length)
		{
			int codeIdx = razorSource.IndexOf("@code", i, StringComparison.Ordinal);
			if (codeIdx < 0)
			{
				break;
			}

			int braceStart = codeIdx + 5;
			while (braceStart < razorSource.Length && char.IsWhiteSpace(razorSource[braceStart]))
			{
				braceStart++;
			}

			if (braceStart >= razorSource.Length || razorSource[braceStart] != '{')
			{
				i = codeIdx + 5;
				continue;
			}

			int depth = 1;
			int j = braceStart + 1;
			while (j < razorSource.Length && depth > 0)
			{
				if (razorSource[j] == '{')
				{
					depth++;
				}
				else if (razorSource[j] == '}')
				{
					depth--;
				}

				j++;
			}

			sb.AppendLine(razorSource[codeIdx..j]);
			i = j;
		}

		return sb.ToString();
	}

	private static async Task<string> RenderComponentAsync(
		byte[] assemblyBytes,
		string? entryPointTypeName,
		List<string> knownDllPaths,
		SnapshotRenderer snapshotRenderer,
		string pageRoute,
		IPluginContext context,
		CancellationToken ct)
	{
		var dllLookup = knownDllPaths
			.GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

		var loadCtx = new PreviewAssemblyLoadContext(dllLookup);
		try
		{
			Assembly assembly = loadCtx.LoadFromStream(new MemoryStream(assemblyBytes));

			Type? componentType = null;
			if (entryPointTypeName is not null)
			{
				componentType = assembly.GetType(entryPointTypeName);
			}

			componentType ??= assembly.GetTypes()
				.FirstOrDefault(t =>
				{
					try
					{
						return typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract;
					}
					catch
					{
						return false;
					}
				});

			if (componentType is null)
			{
				context.LogWarning("Blazor preview: could not find a renderable component in compiled assembly");
				return "<div class=\"blazor-preview-error\">No renderable component found in assembly.</div>";
			}

			return await snapshotRenderer.RenderAsync(componentType, pageRoute, context, ct);
		}
		finally
		{
			loadCtx.Unload();
		}
	}

	/// <summary>
	///     Renders the static <c>&lt;noscript&gt;</c> snapshot of a preview with its own
	///     <see cref="HtmlRenderer" />, giving up after a timeout.
	/// </summary>
	/// <remarks>
	///     A component whose async work never ends (a timer that calls <c>StateHasChanged</c>
	///     forever) never reaches quiescence, and waiting for it hung the build, and in the
	///     ASP.NET Core host the build lock, for good. One renderer per preview means a component
	///     that is still running can't hold up the next one's dispatcher.
	/// </remarks>
	private sealed class SnapshotRenderer(IServiceProvider services, ILoggerFactory loggerFactory, TimeSpan timeout)
	{
		private static readonly TimeSpan _cleanupGrace = TimeSpan.FromSeconds(2);

		public async Task<string> RenderAsync(Type componentType, string pageRoute, IPluginContext context,
			CancellationToken ct)
		{
			var renderer = new HtmlRenderer(services, loggerFactory);
			using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
			limit.CancelAfter(timeout);
			HtmlRootComponent? root = null;
			try
			{
				// Started off the build's thread: a lifecycle method that blocks keeps the thread
				// that runs it, and only work the build doesn't run on can be left behind.
				root = await Task.Run(
						() => renderer.Dispatcher.InvokeAsync(() => renderer.BeginRenderingComponent(componentType)),
						CancellationToken.None)
					.WaitAsync(limit.Token);
				await root.Value.QuiescenceTask.WaitAsync(limit.Token);
				return await renderer.Dispatcher.InvokeAsync(root.Value.ToHtmlString);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				if (root is not { } started)
				{
					context.LogWarning(
						$"mokadocs-blazor-preview: a preview on '{pageRoute}' did not render within " +
						$"{timeout.TotalSeconds:0.#} seconds, so it has no <noscript> snapshot. A lifecycle " +
						"method probably blocks; raise renderTimeout if it only needs longer.");
					return "";
				}

				context.LogWarning(
					$"mokadocs-blazor-preview: a preview on '{pageRoute}' was still rendering after " +
					$"{timeout.TotalSeconds:0.#} seconds; its <noscript> snapshot shows it at that point. " +
					"Raise renderTimeout if it needs longer to finish.");
				return await SnapshotAsync(renderer, started);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.LogWarning($"Blazor preview HtmlRenderer error: {ex.Message}");
				return $"<div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(ex.Message)}</div>";
			}
			finally
			{
				await DisposeQuietlyAsync(renderer);
			}
		}

		private static async Task<string> SnapshotAsync(HtmlRenderer renderer, HtmlRootComponent root)
		{
			try
			{
				return await renderer.Dispatcher.InvokeAsync(root.ToHtmlString).WaitAsync(_cleanupGrace);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// The component holds the renderer's dispatcher, so there is nothing to read.
				return "";
			}
		}

		private static async Task DisposeQuietlyAsync(HtmlRenderer renderer)
		{
			try
			{
				await renderer.DisposeAsync().AsTask().WaitAsync(_cleanupGrace);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// A component that never settled can fail or block its own disposal. The snapshot
				// is already taken, and the build must not wait on it.
			}
		}
	}

	// ── Service stub registration ─────────────────────────────────────────────

	private static void TryRegisterStub(IServiceCollection services, string serviceTypeName,
		string implementationTypeName)
	{
		try
		{
			Type? serviceType = Type.GetType(serviceTypeName) ??
			                    AppDomain.CurrentDomain.GetAssemblies()
				                    .Select(a =>
				                    {
					                    try
					                    {
						                    return a.GetType(serviceTypeName);
					                    }
					                    catch
					                    {
						                    return null;
					                    }
				                    })
				                    .FirstOrDefault(t => t is not null);

			Type? implType = serviceType is null
				? null
				: Type.GetType(implementationTypeName) ??
				  AppDomain.CurrentDomain.GetAssemblies()
					  .Select(a =>
					  {
						  try
						  {
							  return a.GetType(implementationTypeName);
						  }
						  catch
						  {
							  return null;
						  }
					  })
					  .FirstOrDefault(t => t is not null);

			if (serviceType is not null && implType is not null)
			{
				services.AddSingleton(serviceType, implType);
			}
		}
		catch
		{
			// DLL not loaded yet or types unavailable - skip silently
		}
	}

	// ── Null IJSRuntime / NavigationManager for SSR rendering ─────────────────

	private sealed class NullJsRuntime : IJSRuntime
	{
		public static readonly NullJsRuntime Instance = new();

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> ValueTask.FromResult(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
			object?[]? args)
			=> ValueTask.FromResult(default(TValue)!);
	}

	private sealed class NullNavigationManager : NavigationManager
	{
		public static readonly NullNavigationManager Instance = new();

		private NullNavigationManager()
		{
			Initialize("https://localhost/", "https://localhost/");
		}

		protected override void NavigateToCore(string uri, NavigationOptions options)
		{
		}
	}

	// ── Collectible assembly load context for preview DLLs ────────────────────

	private sealed class PreviewAssemblyLoadContext(
		Dictionary<string, string> dllLookup)
		: AssemblyLoadContext("BlazorPreviewCtx", true)
	{
		protected override Assembly? Load(AssemblyName assemblyName)
		{
			try
			{
				Assembly? fromDefault = Default.LoadFromAssemblyName(assemblyName);
				if (fromDefault != null)
				{
					return fromDefault;
				}
			}
			catch
			{
				// Not in default context - fall through.
			}

			if (assemblyName.Name is not null
			    && dllLookup.TryGetValue(assemblyName.Name, out string? dllPath)
			    && File.Exists(dllPath))
			{
				return LoadFromAssemblyPath(dllPath);
			}

			return null;
		}
	}

	#region Scaffold templates

	private const string _scaffoldCsprojTemplate = """
	                                               <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

	                                                 <PropertyGroup>
	                                                   <TargetFramework>net10.0</TargetFramework>
	                                                   <Nullable>enable</Nullable>
	                                                   <ImplicitUsings>enable</ImplicitUsings>
	                                                   <IsPackable>false</IsPackable>
	                                                   <NoWarn>$(NoWarn);CA1515</NoWarn>
	                                                   <!-- Fingerprints blazor.webassembly.js and fills the import map and the
	                                                        preload link in wwwroot/index.html when the host is published. -->
	                                                   <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
	                                                   <!-- IL trimming is auto-disabled by Moka.Blazor.Repl.Host's build/.props
	                                                        (the host loads arbitrary preview DLLs at runtime). -->
	                                                 </PropertyGroup>

	                                                 <ItemGroup>
	                                                   <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.15" />
	                                                   <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.15" PrivateAssets="all" />
	                                                 </ItemGroup>

	                                                 <ItemGroup>
	                                                   <!-- The iframe-hosted App.razor + wasmPreview.js bridge. -->
	                                                   <PackageReference Include="Moka.Blazor.Repl.Host" Version="{HOST_VERSION}" />
	                                               {LIBRARY_REFERENCE}  </ItemGroup>

	                                               </Project>

	                                               """;

	// The leading and trailing newlines are deliberate: {LIBRARY_REFERENCE} sits at the start of
	// the </ItemGroup> line, so a host without a library gets no stray blank line.
	private const string _scaffoldLibraryReferenceTemplate = """

	                                                             <!-- Your component library. The mokadocs plugin reads this project's
	                                                                  bin/Release/net10.0/ for Roslyn references when compiling docs
	                                                                  preview snippets, so any types you can `@using` from your library
	                                                                  here are available inside docs preview blocks. -->
	                                                             <PackageReference Include="{LIBRARY_ID}" Version="{LIBRARY_VERSION}" />

	                                                         """;

	private const string _scaffoldProgramTemplate = """
	                                                using Microsoft.AspNetCore.Components.Web;
	                                                using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
	                                                using Moka.Blazor.Repl.Host;

	                                                var builder = WebAssemblyHostBuilder.CreateDefault(args);

	                                                // Static-root mount of the shared App component from Moka.Blazor.Repl.Host.
	                                                // App owns the [JSInvokable] LoadAssembly method that wasmPreview.js calls
	                                                // to dynamically render compiled docs preview snippets.
	                                                builder.RootComponents.Add<App>("#app");
	                                                builder.RootComponents.Add<HeadOutlet>("head::after");

	                                                builder.Services.AddScoped(_ => new HttpClient
	                                                {
	                                                    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
	                                                });

	                                                // ── Customize: register your library's services here ─────────────
	                                                // Anything you add can be @inject-ed by docs preview snippets.
	                                                // Examples:
	                                                //   builder.Services.AddYourThing();
	                                                //   builder.Services.AddSingleton<IMyService, MyService>();
	                                                // ──────────────────────────────────────────────────────────────────

	                                                await builder.Build().RunAsync();

	                                                """;

	private const string _scaffoldIndexHtmlTemplate = """
	                                                  <!DOCTYPE html>
	                                                  <html lang="en">
	                                                  <head>
	                                                    <meta charset="utf-8" />
	                                                    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
	                                                    <base href="./" />
	                                                    <title>Docs preview host</title>
	                                                    <link rel="preload" id="webassembly" />

	                                                    <!-- ── Customize: link your library's CSS here ──────────────────
	                                                         Static web assets from referenced libraries live under
	                                                         _content/<PackageId>/. The Blazor WASM SDK auto-bundles every
	                                                         referenced library's scoped CSS into the .styles.css below.

	                                                         Example:
	                                                           <link rel="stylesheet" href="_content/{LIBRARY_ID}/your.css" />

	                                                         mokadocs adds a Content-Security-Policy to this page when it
	                                                         copies it into the site: scripts and stylesheets load from the
	                                                         docs origin or from origins this page links to, inline scripts
	                                                         run as written, inline event handlers (onclick="...") don't.
	                                                         ────────────────────────────────────────────────────────────── -->

	                                                    <link rel="stylesheet" href="DocsPreviewHost.styles.css" />
	                                                    <script type="importmap"></script>
	                                                  </head>
	                                                  <body>
	                                                    <div id="app"><div style="padding:1rem;color:#888;font-family:sans-serif;font-size:13px">Loading…</div></div>
	                                                    <div id="blazor-error-ui" style="display:none">
	                                                      A rendering error occurred. <a href="">Reload</a>
	                                                    </div>

	                                                    <!-- The wasmPreview bridge ships as a static web asset from
	                                                         Moka.Blazor.Repl.Host. Must load BEFORE blazor.webassembly.js. -->
	                                                    <script src="_content/Moka.Blazor.Repl.Host/wasmPreview.js"></script>
	                                                    <script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
	                                                  </body>
	                                                  </html>

	                                                  """;

	#endregion
}
