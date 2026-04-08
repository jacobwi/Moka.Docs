using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
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
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     MokaDocs plugin that compiles <c>```blazor-preview</c> code blocks with Roslyn at build
///     time and wires them up for interactive in-page hydration in the browser.
///     <para>
///         <b>Architecture</b> — one iframe per preview block. Each block is compiled to a
///         standalone .dll at build time, written to <c>_site/_preview-assemblies/</c>, and
///         rendered inside an <c>&lt;iframe loading="lazy"&gt;</c> pointing at the consumer's
///         published Blazor WebAssembly preview-host app at <c>/_preview-wasm/</c>. Lazy loading
///         means iframes beyond the viewport don't boot a runtime until scrolled into view.
///         Components portal to the iframe's own <c>document.body</c>, so Dialog/Popover/Toast
///         render inside the preview frame — an intentional, documented constraint.
///     </para>
///     <para>
///         <b>Inputs</b> — one yaml option:
///         <list type="bullet">
///             <item>
///                 <term>previewHost</term>
///                 <description>
///                     Path (absolute or relative to the docs root) to the consumer's Blazor
///                     WebAssembly preview-host project directory. The plugin expects conventional
///                     subdirectories:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 <c>bin/Release/{tfm}/</c> — build-time .dll references for Roslyn.
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 <c>publish-output/wwwroot/</c> — the published static WASM
///                                 runtime that is copied to <c>_site/_preview-wasm/</c>.
///                             </description>
///                         </item>
///                     </list>
///                     The consumer is responsible for running <c>dotnet publish</c> on the
///                     preview-host project before invoking <c>mokadocs build</c>.
///                 </description>
///             </item>
///             <item>
///                 <term>usings</term>
///                 <description>Optional list of global using directives prepended to every snippet compilation.</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Outputs</b> — per build:
///         <list type="bullet">
///             <item>
///                 <description><c>_site/_preview-wasm/</c> — copy of the preview-host's published wwwroot.</description>
///             </item>
///             <item>
///                 <description><c>_site/_preview-assemblies/{sha}.dll</c> — one compiled assembly per preview block.</description>
///             </item>
///             <item>
///                 <description>
///                     Inline placeholder + SSR fallback HTML inside each doc page, plus two boot scripts
///                     appended to body.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </summary>
public sealed class BlazorPreviewPlugin : IMokaPlugin
{
	#region Inline container CSS (tab wrapper + error + placeholder states)

	// All colors inherit from mokadocs theme tokens (--color-*) so the preview chrome
	// matches the surrounding docs theme in both light and dark mode, and across all
	// color themes (ocean, emerald, violet, amber, rose, moka-red, ...). Fallbacks are
	// only used when the page lacks a mokadocs theme (e.g. standalone render).
	private const string _inlineCss = """
	                                  <style>
	                                  /* ── Preview container & tabs ────────────────────────────────── */
	                                  .blazor-preview-container {
	                                      position: relative;
	                                      border: 1px solid var(--color-border, #e2e8f0);
	                                      border-radius: 8px;
	                                      margin: 1.5em 0;
	                                      overflow: hidden;
	                                      background: var(--color-bg, #ffffff);
	                                  }
	                                  .blazor-preview-tabs {
	                                      display: flex;
	                                      align-items: center;
	                                      border-bottom: 1px solid var(--color-border, #e2e8f0);
	                                      background: var(--color-bg-secondary, var(--color-bg, #f7f7f9));
	                                      padding: 0;
	                                      margin: 0;
	                                  }
	                                  .blazor-preview-tab {
	                                      padding: 0.55em 1.1em;
	                                      font-size: 0.78rem;
	                                      font-weight: 600;
	                                      color: var(--color-text-muted, var(--color-text-secondary, #64748b));
	                                      background: transparent;
	                                      border: none;
	                                      border-bottom: 2px solid transparent;
	                                      cursor: pointer;
	                                      transition: color 0.15s ease-out, border-color 0.15s ease-out, background 0.15s ease-out;
	                                      font-family: inherit;
	                                      letter-spacing: 0.01em;
	                                  }
	                                  .blazor-preview-tab:hover {
	                                      color: var(--color-text, #1a1a1a);
	                                      background: var(--color-bg, #ffffff);
	                                  }
	                                  .blazor-preview-tab.active {
	                                      color: var(--color-primary, #0ea5e9);
	                                      border-bottom-color: var(--color-primary, #0ea5e9);
	                                      background: var(--color-bg, #ffffff);
	                                  }
	                                  .blazor-preview-source { display: none; }
	                                  .blazor-preview-source.active { display: block; }
	                                  .blazor-preview-source pre { margin: 0; border: none; border-radius: 0; }
	                                  .blazor-preview-source code {
	                                      font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', ui-monospace, monospace;
	                                  }
	                                  .blazor-preview-render {
	                                      display: none;
	                                      padding: 0;
	                                      background: var(--color-bg, #ffffff);
	                                      min-height: 160px;
	                                      background-image: linear-gradient(var(--color-border-light, var(--color-border, #eef0f2)) 1px, transparent 1px),
	                                                        linear-gradient(90deg, var(--color-border-light, var(--color-border, #eef0f2)) 1px, transparent 1px);
	                                      background-size: 24px 24px;
	                                      background-position: -1px -1px;
	                                  }
	                                  .blazor-preview-render.active { display: block; }
	                                  .blazor-preview-error {
	                                      color: var(--color-primary, #ef4444);
	                                      font-size: 0.85rem;
	                                      font-family: 'JetBrains Mono', 'Cascadia Code', ui-monospace, monospace;
	                                      padding: 1em;
	                                      background: var(--color-bg-secondary, #fef2f2);
	                                      border: 1px solid var(--color-border, #fecaca);
	                                      white-space: pre-wrap;
	                                      border-radius: 4px;
	                                  }
	                                  .blazor-preview-loading {
	                                      min-height: 24px;
	                                      opacity: 0.5;
	                                      background: repeating-linear-gradient(
	                                          -45deg,
	                                          var(--color-border, #e2e8f0),
	                                          var(--color-border, #e2e8f0) 10px,
	                                          transparent 10px,
	                                          transparent 20px
	                                      );
	                                      border-radius: 4px;
	                                  }
	                                  .blazor-preview-badge {
	                                      display: inline-flex;
	                                      align-items: center;
	                                      font-size: 0.62rem;
	                                      font-weight: 700;
	                                      text-transform: uppercase;
	                                      letter-spacing: 0.06em;
	                                      color: var(--color-primary, #0ea5e9);
	                                      background: var(--color-bg, #ffffff);
	                                      border: 1px solid var(--color-border, #e2e8f0);
	                                      padding: 0.2em 0.55em;
	                                      border-radius: 999px;
	                                      margin-left: auto;
	                                      margin-right: 0.75em;
	                                  }
	                                  </style>
	                                  """;

	#endregion

	#region Tab switching JS (Preview / Source tabs per container)

	private const string _tabsJs = """
	                               <script>
	                               (function() {
	                                   document.querySelectorAll('.blazor-preview-container[data-blazor-preview="true"]').forEach(function(container) {
	                                       var sourceDiv = container.querySelector('.blazor-preview-source');
	                                       var renderDiv = container.querySelector('.blazor-preview-render');
	                                       if (!sourceDiv || !renderDiv) return;

	                                       var tabBar = document.createElement('div');
	                                       tabBar.className = 'blazor-preview-tabs';

	                                       var previewTab = document.createElement('button');
	                                       previewTab.className = 'blazor-preview-tab active';
	                                       previewTab.textContent = 'Preview';
	                                       previewTab.type = 'button';

	                                       var sourceTab = document.createElement('button');
	                                       sourceTab.className = 'blazor-preview-tab';
	                                       sourceTab.textContent = 'Source';
	                                       sourceTab.type = 'button';

	                                       var badge = document.createElement('span');
	                                       badge.className = 'blazor-preview-badge';
	                                       badge.textContent = 'Blazor';

	                                       tabBar.appendChild(previewTab);
	                                       tabBar.appendChild(sourceTab);
	                                       tabBar.appendChild(badge);
	                                       container.insertBefore(tabBar, container.firstChild);

	                                       renderDiv.classList.add('active');

	                                       previewTab.addEventListener('click', function() {
	                                           previewTab.classList.add('active');
	                                           sourceTab.classList.remove('active');
	                                           renderDiv.classList.add('active');
	                                           sourceDiv.classList.remove('active');
	                                       });
	                                       sourceTab.addEventListener('click', function() {
	                                           sourceTab.classList.add('active');
	                                           previewTab.classList.remove('active');
	                                           sourceDiv.classList.add('active');
	                                           renderDiv.classList.remove('active');
	                                       });
	                                   });
	                               })();
	                               </script>
	                               """;

	#endregion

	// ── Instance state ─────────────────────────────────────────────────────────

	private readonly List<string> _extraUsings = [];
	private readonly List<string> _knownDllPaths = [];
	private RoslynCompilationService? _compilationService;
	private string? _previewHostWwwroot;
	private bool _servicesInitialized;
	private string _basePathForAssemblyQuery = "";

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
		context.LogInfo("Blazor preview plugin initialized — interactive in-page hydration mode");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		if (!_servicesInitialized)
		{
			if (!InitializeFromOptions(context, buildContext))
			{
				return; // Hard failure already logged.
			}

			_servicesInitialized = true;
		}

		if (_compilationService is null || _previewHostWwwroot is null)
		{
			return;
		}

		// Always register the preview-host copy so the static files are present at
		// /_preview-wasm/ even if no pages on this build currently contain preview blocks.
		buildContext.DeferredOutputDirectories.Add((_previewHostWwwroot, "_preview-wasm"));

		// GitHub Pages runs Jekyll by default, which STRIPS directories starting with an
		// underscore — which is exactly every directory this plugin writes (_preview-wasm,
		// _preview-assemblies, _framework, _content). Without a .nojekyll marker at the site
		// root, the entire preview runtime is missing from the deployed site. Emit an empty
		// .nojekyll file so Jekyll is bypassed and every directory ships verbatim.
		if (!buildContext.DeferredOutputFiles.ContainsKey(".nojekyll"))
		{
			buildContext.DeferredOutputFiles[".nojekyll"] = [];
		}

		// Resolve base path for the `?assembly=...` iframe query parameter. The Scriban
		// template engine's RewriteContentLinks regex only matches `href="/..."` and
		// `src="/..."` at the attribute OPENING — it does NOT rewrite `/` characters
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
		await using HtmlRenderer htmlRenderer = CreateHtmlRenderer(loggerFactory, _knownDllPaths);

		int pagesWithPreview = 0;
		int blocksCompiled = 0;
		int blocksFailed = 0;

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html) ||
			    !html.Contains("data-blazor-preview=\"true\"", StringComparison.Ordinal))
			{
				continue;
			}

			(string rewrittenHtml, int pageCompiled, int pageFailed) = await CompileAndRewritePageAsync(
				html, _compilationService, _extraUsings, _knownDllPaths, htmlRenderer,
				buildContext, context, _basePathForAssemblyQuery, ct);

			page.Content = page.Content with
			{
				Html = InjectPreviewAssets(rewrittenHtml)
			};

			pagesWithPreview++;
			blocksCompiled += pageCompiled;
			blocksFailed += pageFailed;
		}

		if (pagesWithPreview > 0 || blocksFailed > 0)
		{
			context.LogInfo(
				$"Blazor preview plugin: {pagesWithPreview} page(s), {blocksCompiled} block(s) compiled, {blocksFailed} failed");
		}
	}

	// ── One-time initialization: parse options, resolve preview-host, build compile refs ──

	private bool InitializeFromOptions(IPluginContext context, BuildContext buildContext)
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
		string? library = null;
		if (context.Options.TryGetValue("library", out object? libObj)
		    && libObj is string libStr
		    && !string.IsNullOrWhiteSpace(libStr))
		{
			library = libStr;
		}

		// Resolve the preview-host directory: explicit option wins, otherwise scan
		// conventional locations (./preview-host/, ./docs-preview-host/, or any subdir
		// containing a Microsoft.NET.Sdk.BlazorWebAssembly csproj).
		string previewHostDir = ResolvePreviewHostDirectory(hostPathRaw, buildContext.RootDirectory);

		// Auto-scaffold a fresh preview-host project from a generic template if the
		// resolved directory doesn't yet contain a Blazor WASM csproj. The user owns the
		// scaffolded files thereafter — mokadocs never overwrites them.
		if (!HasBlazorWasmCsproj(previewHostDir))
		{
			if (string.IsNullOrWhiteSpace(library))
			{
				context.LogError(
					$"mokadocs-blazor-preview: no Blazor WebAssembly preview-host project found at " +
					$"'{previewHostDir}', and the 'library' option is not set. Add " +
					$"`library: <PackageId>@<Version>` (or just `library: <PackageId>`) to your " +
					"mokadocs.yaml so the plugin can scaffold a preview-host project for you. " +
					"Alternatively, set `previewHost: <path>` to point at an existing project.");
				return false;
			}

			ScaffoldPreviewHost(previewHostDir, library, context);
		}

		// Run `dotnet publish` on the preview-host (incremental — skipped when nothing
		// upstream of the published wwwroot has changed since the last build).
		if (!EnsurePreviewHostPublished(previewHostDir, context))
		{
			return false;
		}

		// Conventional layout for the consumer's Blazor WebAssembly preview-host project
		// (e.g. Moka.Blazor.Repl.Wasm):
		//   {previewHostDir}/publish-output/{tfm}/wwwroot/  OR  {previewHostDir}/publish-output/wwwroot/
		//     — runtime static files (the WASM app copied to _site/_preview-wasm/)
		//   {previewHostDir}/bin/Release/{tfm}/
		//     — build-time Roslyn .dll references
		string? wwwroot = FindPublishedWwwroot(previewHostDir);
		if (wwwroot is null)
		{
			context.LogError(
				$"mokadocs-blazor-preview: published wwwroot not found under '{previewHostDir}/publish-output/'. " +
				"Run `dotnet publish -c Release -o publish-output[/{tfm}]` in the preview-host project before building the docs.");
			return false;
		}

		if (!File.Exists(Path.Combine(wwwroot, "_framework", "blazor.webassembly.js")))
		{
			context.LogError(
				$"mokadocs-blazor-preview: '{wwwroot}' does not look like a published Blazor WASM app " +
				"(missing _framework/blazor.webassembly.js).");
			return false;
		}

		_previewHostWwwroot = wwwroot;

		string? refBinDir = FindReferenceBinDirectory(previewHostDir);
		if (refBinDir is null)
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
		_compilationService = new RoslynCompilationService(new HttpClient());
		var dllByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// The preview-host's bin dir holds every transitive assembly it references, including
		// framework assemblies (Microsoft.*, System.*) that Roslyn already supplies via the
		// host runtime. Adding those causes CS0433 type-conflict errors when the TFMs differ
		// (e.g. preview-host built against net9, mokadocs runs on net10). Only include the
		// NON-framework assemblies — i.e. the consumer's own component library DLLs.
		foreach (string dll in Directory.GetFiles(refBinDir, "*.dll"))
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

	// ── Preview-host auto-discovery / scaffolding / publishing ────────────────

	/// <summary>
	///     Pinned version of <c>Moka.Blazor.Repl.Host</c> that the scaffold template references.
	///     Update this whenever a new compatible host RCL is published to NuGet.
	/// </summary>
	private const string _hostPackageVersion = "1.3.0";

	/// <summary>
	///     Resolves the preview-host project directory from yaml options or convention.
	///     <para>
	///         Resolution order:
	///         <list type="number">
	///             <item><description>Explicit <c>previewHost:</c> yaml option (relative to docs root)</description></item>
	///             <item><description><c>./preview-host/</c></description></item>
	///             <item><description><c>./docs-preview-host/</c></description></item>
	///             <item><description>Any subdirectory under the docs root that already contains a
	///                 <c>Microsoft.NET.Sdk.BlazorWebAssembly</c> csproj</description></item>
	///         </list>
	///     </para>
	///     <para>
	///         If nothing is found, returns <c>./preview-host/</c> as the default scaffold target —
	///         the caller is expected to scaffold a fresh project there.
	///     </para>
	/// </summary>
	private static string ResolvePreviewHostDirectory(string? explicitPath, string rootDir)
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
		return Path.GetFullPath(Path.Combine(rootDir, conventional[0]));
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
	///     Program.cs + wwwroot/index.html) into <paramref name="dir" />, substituting the
	///     consumer's library package id and version into the csproj's <c>PackageReference</c>
	///     and into the index.html theme/customization markers. Files are written ONLY when they
	///     do not already exist — mokadocs never overwrites user edits.
	/// </summary>
	private static void ScaffoldPreviewHost(string dir, string librarySpec, IPluginContext context)
	{
		(string libId, string libVersion) = ParseLibrarySpec(librarySpec);

		Directory.CreateDirectory(dir);
		Directory.CreateDirectory(Path.Combine(dir, "wwwroot"));

		string csprojPath = Path.Combine(dir, "DocsPreviewHost.csproj");
		string programPath = Path.Combine(dir, "Program.cs");
		string indexPath = Path.Combine(dir, "wwwroot", "index.html");

		// Empty Directory.Build.props/.targets and Directory.Packages.props in the
		// preview-host directory shadow any parent repo's central files. MSBuild walks
		// UP the directory tree and stops at the first match — so by dropping empty
		// markers here, the preview-host becomes hermetic and inherits nothing from
		// the consumer's parent build configuration (multi-target overrides, central
		// package management, FrameworkReference stripping, etc.).
		string buildPropsPath = Path.Combine(dir, "Directory.Build.props");
		string buildTargetsPath = Path.Combine(dir, "Directory.Build.targets");
		string packagesPropsPath = Path.Combine(dir, "Directory.Packages.props");

		string csproj = _scaffoldCsprojTemplate
			.Replace("{LIBRARY_ID}", libId, StringComparison.Ordinal)
			.Replace("{LIBRARY_VERSION}", libVersion, StringComparison.Ordinal)
			.Replace("{HOST_VERSION}", _hostPackageVersion, StringComparison.Ordinal);

		string program = _scaffoldProgramTemplate;
		string indexHtml = _scaffoldIndexHtmlTemplate
			.Replace("{LIBRARY_ID}", libId, StringComparison.Ordinal);

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
			context.LogInfo(
				$"mokadocs-blazor-preview: scaffolded preview-host at '{dir}' " +
				$"(library: {libId}@{libVersion}, host: Moka.Blazor.Repl.Host@{_hostPackageVersion}). " +
				"Edit Program.cs / wwwroot/index.html to add your services and CSS — mokadocs will not overwrite them.");
		}
	}

	/// <summary>
	///     Parses a <c>library:</c> yaml value into <c>(packageId, version)</c>.
	///     <list type="bullet">
	///         <item><description><c>Foo.Bar@1.2.3</c> → <c>("Foo.Bar", "1.2.3")</c></description></item>
	///         <item><description><c>Foo.Bar</c>      → <c>("Foo.Bar", "*")</c> (latest)</description></item>
	///     </list>
	/// </summary>
	private static (string id, string version) ParseLibrarySpec(string spec)
	{
		int at = spec.IndexOf('@', StringComparison.Ordinal);
		if (at < 0)
		{
			return (spec.Trim(), "*");
		}

		return (spec[..at].Trim(), spec[(at + 1)..].Trim());
	}

	/// <summary>
	///     Runs <c>dotnet publish -c Release -f net10.0 -o publish-output/net10.0</c> on the
	///     preview-host project if its publish output is missing or stale relative to its inputs.
	///     Returns <c>false</c> on hard failure (logged via <paramref name="context" />).
	/// </summary>
	private static bool EnsurePreviewHostPublished(string previewHostDir, IPluginContext context)
	{
		string? csproj = Directory.GetFiles(previewHostDir, "*.csproj", SearchOption.TopDirectoryOnly)
			.FirstOrDefault();
		if (csproj is null)
		{
			context.LogError($"mokadocs-blazor-preview: no .csproj found in '{previewHostDir}'.");
			return false;
		}

		string publishOut = Path.Combine(previewHostDir, "publish-output", "net10.0");
		string publishMarker = Path.Combine(publishOut, "wwwroot", "_framework", "blazor.webassembly.js");

		bool needsPublish = !File.Exists(publishMarker);
		if (!needsPublish)
		{
			DateTime publishedAt = File.GetLastWriteTimeUtc(publishMarker);
			DateTime newestInput = GetNewestInputMtime(previewHostDir);
			if (newestInput > publishedAt)
			{
				needsPublish = true;
			}
		}

		if (!needsPublish)
		{
			return true;
		}

		context.LogInfo(
			$"mokadocs-blazor-preview: publishing preview-host '{Path.GetFileName(csproj)}' " +
			$"(this happens once per change to the project; subsequent builds are skipped)...");

		var psi = new ProcessStartInfo("dotnet",
			$"publish \"{csproj}\" -c Release -f net10.0 -o \"{publishOut}\"")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = previewHostDir
		};

		try
		{
			using var proc = Process.Start(psi);
			if (proc is null)
			{
				context.LogError("mokadocs-blazor-preview: failed to start `dotnet publish`.");
				return false;
			}

			string stdout = proc.StandardOutput.ReadToEnd();
			string stderr = proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			if (proc.ExitCode != 0)
			{
				context.LogError(
					$"mokadocs-blazor-preview: `dotnet publish` exited with code {proc.ExitCode}.\n" +
					(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr));
				return false;
			}

			if (!File.Exists(publishMarker))
			{
				context.LogError(
					$"mokadocs-blazor-preview: publish completed but '{publishMarker}' is missing. " +
					"Check your preview-host project for build errors.");
				return false;
			}
		}
		catch (Exception ex)
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

	#region Scaffold templates

	private const string _scaffoldCsprojTemplate = """
	                                                <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

	                                                  <PropertyGroup>
	                                                    <TargetFramework>net10.0</TargetFramework>
	                                                    <Nullable>enable</Nullable>
	                                                    <ImplicitUsings>enable</ImplicitUsings>
	                                                    <IsPackable>false</IsPackable>
	                                                    <NoWarn>$(NoWarn);CA1515</NoWarn>
	                                                    <!-- IL trimming is auto-disabled by Moka.Blazor.Repl.Host's build/.props
	                                                         (the host loads arbitrary preview DLLs at runtime). -->
	                                                  </PropertyGroup>

	                                                  <ItemGroup>
	                                                    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.5" />
	                                                    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.5" PrivateAssets="all" />
	                                                  </ItemGroup>

	                                                  <ItemGroup>
	                                                    <!-- The iframe-hosted App.razor + wasmPreview.js bridge. -->
	                                                    <PackageReference Include="Moka.Blazor.Repl.Host" Version="{HOST_VERSION}" />

	                                                    <!-- Your component library. The mokadocs plugin reads this project's
	                                                         bin/Release/net10.0/ for Roslyn references when compiling docs
	                                                         preview snippets, so any types you can `@using` from your library
	                                                         here are available inside docs preview blocks. -->
	                                                    <PackageReference Include="{LIBRARY_ID}" Version="{LIBRARY_VERSION}" />
	                                                  </ItemGroup>

	                                                </Project>

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

	                                                     <!-- ── Customize: link your library's CSS here ──────────────────
	                                                          Static web assets from referenced libraries live under
	                                                          _content/<PackageId>/. The Blazor WASM SDK auto-bundles every
	                                                          referenced library's scoped CSS into the .styles.css below.

	                                                          Example:
	                                                            <link rel="stylesheet" href="_content/{LIBRARY_ID}/your.css" />
	                                                          ────────────────────────────────────────────────────────────── -->

	                                                     <link rel="stylesheet" href="DocsPreviewHost.styles.css" />
	                                                   </head>
	                                                   <body>
	                                                     <div id="app"><div style="padding:1rem;color:#888;font-family:sans-serif;font-size:13px">Loading…</div></div>
	                                                     <div id="blazor-error-ui" style="display:none">
	                                                       A rendering error occurred. <a href="">Reload</a>
	                                                     </div>

	                                                     <!-- The wasmPreview bridge ships as a static web asset from
	                                                          Moka.Blazor.Repl.Host. Must load BEFORE blazor.webassembly.js. -->
	                                                     <script src="_content/Moka.Blazor.Repl.Host/wasmPreview.js"></script>
	                                                     <script src="_framework/blazor.webassembly.js"></script>
	                                                   </body>
	                                                   </html>

	                                                   """;

	#endregion

	/// <summary>
	///     Returns <c>true</c> if an assembly with the given simple-name is already resolvable
	///     by the host runtime — i.e. it is part of the .NET shared framework (BCL, ASP.NET Core,
	///     Extensions, JSInterop) that the mokadocs tool itself is running on top of.
	///     <para>
	///         The base <see cref="RoslynCompilationService" /> already seeds its compilation with
	///         these host-runtime framework assemblies. Re-adding duplicates from a preview-host's
	///         bin directory (which may have been built against a different TFM) causes CS0433 /
	///         CS1704 type-conflict errors. This check works identically on .NET 9 and .NET 10 —
	///         whichever runtime mokadocs is executing on is the reference version used.
	///     </para>
	///     <para>
	///         We deliberately use <see cref="Assembly.Load(AssemblyName)" /> instead of a
	///         hardcoded prefix list: any future framework or extension package added to the
	///         shared framework is automatically treated as such, with no plugin code change.
	///     </para>
	/// </summary>
	private static bool IsFrameworkAssemblyName(string simpleName)
	{
		if (string.IsNullOrEmpty(simpleName))
		{
			return false;
		}

		// Cheap fast path: mscorlib / System / netstandard are always framework.
		if (simpleName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
		    || simpleName.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		try
		{
			var loaded = Assembly.Load(new AssemblyName(simpleName));
			// Only TRUSTED host runtime locations count — an assembly that the host loaded from
			// a user-supplied path is not "framework". The shared framework lives under
			// dotnet/shared or dotnet/packs — both contain "dotnet" in a normalized path.
			string location = loaded.Location;
			if (string.IsNullOrEmpty(location))
			{
				return false;
			}

			string normalized = location.Replace('\\', '/').ToLowerInvariant();
			return normalized.Contains("/dotnet/shared/")
			       || normalized.Contains("/dotnet/packs/")
			       || normalized.Contains("/.dotnet/shared/")
			       || normalized.Contains("/.dotnet/packs/");
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	///     Looks for the published <c>wwwroot/</c> under <c>{previewHostDir}/publish-output/</c>.
	///     Accepts either a TFM-subdirectory layout
	///     (<c>publish-output/net10.0/wwwroot/</c>) or a flat layout (<c>publish-output/wwwroot/</c>).
	///     The directory must contain <c>_framework/blazor.webassembly.js</c>.
	/// </summary>
	private static string? FindPublishedWwwroot(string previewHostDir)
	{
		string publishRoot = Path.Combine(previewHostDir, "publish-output");
		if (!Directory.Exists(publishRoot))
		{
			return null;
		}

		static bool IsWasmApp(string dir)
		{
			return Directory.Exists(Path.Combine(dir, "_framework"))
			       && File.Exists(Path.Combine(dir, "_framework", "blazor.webassembly.js"));
		}

		// Flat layout: publish-output/wwwroot/
		string flat = Path.Combine(publishRoot, "wwwroot");
		if (Directory.Exists(flat) && IsWasmApp(flat))
		{
			return flat;
		}

		// TFM-subdir layout: publish-output/net10.0/wwwroot/ (prefer newest TFM)
		foreach (string tfmDir in Directory.GetDirectories(publishRoot)
			         .Where(d => Path.GetFileName(d).StartsWith("net", StringComparison.OrdinalIgnoreCase))
			         .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
		{
			string candidate = Path.Combine(tfmDir, "wwwroot");
			if (Directory.Exists(candidate) && IsWasmApp(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	/// <summary>
	///     Looks for <c>{previewHostDir}/bin/Release/net*.0/</c> — the Blazor WASM project's
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

		string? best = Directory.GetDirectories(releaseDir)
			.Where(d => Path.GetFileName(d).StartsWith("net", StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault(d => Directory.GetFiles(d, "*.dll").Length > 0);

		return best;
	}

	// ── HtmlRenderer (for SSR fallback) ────────────────────────────────────────

	private static HtmlRenderer CreateHtmlRenderer(ILoggerFactory loggerFactory, IEnumerable<string> knownDllPaths)
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

		ServiceProvider sp = services.BuildServiceProvider();
		return new HtmlRenderer(sp, loggerFactory);
	}

	// ── Per-page block compilation + rewrite ───────────────────────────────────

	private static async Task<(string html, int compiled, int failed)> CompileAndRewritePageAsync(
		string html,
		RoslynCompilationService compilationService,
		List<string> extraUsings,
		List<string> knownDllPaths,
		HtmlRenderer htmlRenderer,
		BuildContext buildContext,
		IPluginContext context,
		string basePathForAssemblyQuery,
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
						string hash = Convert.ToHexString(
							SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..12].ToLowerInvariant();
						string dllRelPath = $"_preview-assemblies/{hash}.dll";
						string entryPoint = result.EntryPointTypeName ?? "MokaRepl.Preview";

						buildContext.DeferredOutputFiles[dllRelPath] = result.AssemblyBytes;

						// Render an SSR snapshot of the component as noscript fallback and for
						// search / link-preview crawlers. Never shown when JS is enabled.
						string ssrHtml;
						try
						{
							ssrHtml = await RenderComponentAsync(
								result.AssemblyBytes, result.EntryPointTypeName, knownDllPaths, htmlRenderer, context);
						}
						catch
						{
							ssrHtml = "";
						}

						// Emit one sandboxed, lazy-loaded iframe per preview. The preview-host WASM app
						// reads ?assembly=… &entry=… from its URL and mounts the component into its
						// own static root (#app) via a plain RenderFragment inside App.razor's
						// [JSInvokable] LoadAssembly — no dynamic root components API required.
						//
						// iframe `src` is a root-relative absolute path; the mokadocs Scriban template
						// engine's RewriteContentLinks auto-prefixes it with BasePath at render time.
						// The `assembly` query parameter, however, lives INSIDE the src attribute
						// value, so the regex-based rewriter doesn't touch it — we must prefix it
						// ourselves here so GitHub Pages project-page deployments resolve the DLL.
						string iframeSrc =
							$"/_preview-wasm/index.html?assembly={basePathForAssemblyQuery}/{dllRelPath}&amp;entry={WebUtility.HtmlEncode(entryPoint)}";

						sb.Append("<div class=\"blazor-preview-render\">")
							.Append("<iframe class=\"blazor-preview-iframe\" src=\"")
							.Append(iframeSrc)
							.Append("\" loading=\"lazy\" sandbox=\"allow-scripts allow-same-origin\"")
							.Append(" title=\"Blazor preview\"></iframe>")
							.Append("<noscript><div class=\"blazor-preview-ssr-fallback\">")
							.Append(ssrHtml)
							.Append("</div></noscript>")
							.Append("</div>");

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
				catch (Exception ex)
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
		HtmlRenderer htmlRenderer,
		IPluginContext context)
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

			try
			{
				return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
				{
					HtmlRootComponent output = await htmlRenderer.RenderComponentAsync(componentType);
					return output.ToHtmlString();
				});
			}
			catch (Exception ex)
			{
				context.LogWarning($"Blazor preview HtmlRenderer error: {ex.Message}");
				return $"<div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(ex.Message)}</div>";
			}
		}
		finally
		{
			loadCtx.Unload();
		}
	}

	// ── Asset injection ───────────────────────────────────────────────────────

	/// <summary>
	///     Injects the preview container CSS at the top of the page and the tab-switching JS
	///     plus an iframe auto-resize listener at the bottom. Each preview iframe is self-hosted
	///     (owns its own Blazor runtime), so there is no shared boot script injected per page.
	/// </summary>
	private static string InjectPreviewAssets(string html)
	{
		// iframe min-height is 160px (matches preview-host body min-height) so small
		// previews aren't jammed into a cramped strip while the runtime boots and the
		// ResizeObserver hasn't fired yet. The height transition smooths the jump when
		// the postMessage resize event resizes the iframe to fit its actual content.
		const string iframeCss = """
		                         <style>
		                         .blazor-preview-iframe {
		                             width: 100%;
		                             min-height: 160px;
		                             border: none;
		                             display: block;
		                             background: transparent;
		                             transition: height 200ms ease-out;
		                         }
		                         .blazor-preview-ssr-fallback { padding: 1.25rem 1.5rem; }
		                         </style>
		                         """;

		const string resizeJs = """
		                        <script>
		                        (function() {
		                            // The preview-host posts { type: 'resize', height } up to the parent frame
		                            // when its content height changes, so each iframe grows to fit its component.
		                            window.addEventListener('message', function(e) {
		                                if (!e.data || e.data.type !== 'resize') return;
		                                document.querySelectorAll('.blazor-preview-iframe').forEach(function(iframe) {
		                                    if (iframe.contentWindow === e.source) {
		                                        iframe.style.height = (e.data.height + 16) + 'px';
		                                    }
		                                });
		                            });
		                        })();
		                        </script>
		                        """;

		var sb = new StringBuilder(html.Length + _inlineCss.Length + iframeCss.Length + _tabsJs.Length +
		                           resizeJs.Length + 64);
		sb.Append(_inlineCss);
		sb.Append(iframeCss);
		sb.Append(html);
		sb.Append(_tabsJs);
		sb.Append(resizeJs);
		return sb.ToString();
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
			// DLL not loaded yet or types unavailable — skip silently
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
				// Not in default context — fall through.
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
}
