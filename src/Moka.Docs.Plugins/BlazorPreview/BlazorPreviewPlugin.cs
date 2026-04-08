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
	                                      background: var(--color-bg-code, #181825);
	                                      padding: 0;
	                                      margin: 0;
	                                  }
	                                  .blazor-preview-tab {
	                                      padding: 0.5em 1.2em;
	                                      font-size: 0.8rem;
	                                      font-weight: 600;
	                                      color: #94a3b8;
	                                      background: transparent;
	                                      border: none;
	                                      border-bottom: 2px solid transparent;
	                                      cursor: pointer;
	                                      transition: color 0.15s, border-color 0.15s;
	                                      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
	                                  }
	                                  .blazor-preview-tab:hover { color: #cbd5e1; }
	                                  .blazor-preview-tab.active {
	                                      color: #60a5fa;
	                                      border-bottom-color: #60a5fa;
	                                  }
	                                  .blazor-preview-source { display: none; }
	                                  .blazor-preview-source.active { display: block; }
	                                  .blazor-preview-source pre { margin: 0; border: none; border-radius: 0; }
	                                  .blazor-preview-source code {
	                                      font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                  }
	                                  .blazor-preview-render {
	                                      display: none;
	                                      padding: 1.25rem 1.5rem;
	                                      background: var(--color-bg, #ffffff);
	                                      min-height: 48px;
	                                  }
	                                  [data-theme="dark"] .blazor-preview-render {
	                                      background: var(--color-bg, #0f172a);
	                                  }
	                                  .blazor-preview-render.active { display: block; }
	                                  .blazor-preview-error {
	                                      color: #ef4444;
	                                      font-size: 0.85rem;
	                                      font-family: 'JetBrains Mono', 'Cascadia Code', monospace;
	                                      padding: 1em;
	                                      background: #fef2f2;
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
	                                      display: inline-block;
	                                      font-size: 0.65rem;
	                                      font-weight: 700;
	                                      text-transform: uppercase;
	                                      letter-spacing: 0.05em;
	                                      color: #7c3aed;
	                                      background: #ede9fe;
	                                      padding: 0.15em 0.5em;
	                                      border-radius: 3px;
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
				buildContext, context, ct);

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
		// ── previewHost option ────────────────────────────────────────────────
		if (!context.Options.TryGetValue("previewHost", out object? hostObj)
		    || hostObj is not string hostPathRaw
		    || string.IsNullOrWhiteSpace(hostPathRaw))
		{
			context.LogError(
				"mokadocs-blazor-preview: the 'previewHost' option is required. " +
				"Set it to the path of your Blazor WebAssembly preview-host project directory.");
			return false;
		}

		string previewHostDir = Path.GetFullPath(Path.Combine(buildContext.RootDirectory, hostPathRaw));
		if (!Directory.Exists(previewHostDir))
		{
			context.LogError($"mokadocs-blazor-preview: previewHost directory does not exist: {previewHostDir}");
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

						// Emit one sandboxed, lazy-loaded iframe per preview. The `Moka.Blazor.Repl.Wasm`
						// preview-host reads ?assembly=… &entry=… from its URL and mounts the component
						// into its own static root (#app) via a plain RenderFragment — no dynamic
						// root components API, no RegisterForJavaScript, just the static pattern that
						// already works in your REPL app today.
						string iframeSrc =
							$"/_preview-wasm/index.html?assembly=/{dllRelPath}&amp;entry={WebUtility.HtmlEncode(entryPoint)}";

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
		const string iframeCss = """
		                         <style>
		                         .blazor-preview-iframe {
		                             width: 100%;
		                             min-height: 80px;
		                             border: none;
		                             display: block;
		                             background: var(--color-bg, #ffffff);
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
