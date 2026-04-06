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
///     MokaDocs plugin that compiles <c>```blazor-preview</c> code blocks using Roslyn at build time,
///     renders them to HTML via Blazor's <see cref="HtmlRenderer" /> (static SSR), and injects the
///     result inline — no iframe, no WASM runtime required.
/// </summary>
public sealed class BlazorPreviewPlugin : IMokaPlugin
{
	#region Inline CSS

	/// <summary>
	///     Structural CSS for the preview container, tabs, and error display.
	///     Moka.Red design tokens are provided by moka.css via the stylesheets bundle.
	/// </summary>
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
	                                  .blazor-preview-source pre {
	                                      margin: 0;
	                                      border: none;
	                                      border-radius: 0;
	                                  }
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
	                                      font-family: monospace;
	                                      padding: 1em;
	                                      background: #fef2f2;
	                                      white-space: pre-wrap;
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

	#region Inline JS

	private const string _inlineJs = """
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

	private readonly List<string> _extraUsings = [];
	private readonly List<string> _knownDllPaths = [];
	private RoslynCompilationService? _compilationService;

	// ── Instance state ────────────────────────────────────────────────────────

	private BlazorPreviewMode _mode = BlazorPreviewMode.Wasm;

	/// <summary>Concatenated CSS bundle (scoped CSS from all referenced Moka.Red packages).</summary>
	private byte[]? _previewCssBundle;

	private bool _servicesInitialized;

	// ── IMokaPlugin ───────────────────────────────────────────────────────────

	/// <inheritdoc />
	public string Id => "mokadocs-blazor-preview";

	/// <inheritdoc />
	public string Name => "Blazor Component Preview";

	/// <inheritdoc />
	public string Version => "2.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("Blazor preview plugin initialized — static SSR component preview enabled");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		// Lazy-init: compilation service + CSS bundle (expensive — do once per process lifetime)
		if (!_servicesInitialized)
		{
			// Parse mode option (default: wasm)
			if (context.Options.TryGetValue("mode", out object? modeObj)
			    && modeObj is string modeStr
			    && Enum.TryParse<BlazorPreviewMode>(modeStr, true, out BlazorPreviewMode parsed))
			{
				_mode = parsed;
			}

			_compilationService = CreateCompilationService(context, buildContext.RootDirectory);
			_previewCssBundle = BuildCssBundle(context, buildContext.RootDirectory);
			_servicesInitialized = true;
		}

		// Register CSS bundle for deferred write (runs after OutputPhase clean step)
		if (_previewCssBundle is { Length: > 0 })
		{
			buildContext.DeferredOutputFiles["_preview-css/moka-preview.css"] = _previewCssBundle;
		}

		int pagesWithPreview = 0;

		if (_compilationService is null)
		{
			return;
		}

		ILoggerFactory loggerFactory = context.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

		// Create a single HtmlRenderer for all pages in this build pass (used for SSR and WASM fallback)
		await using HtmlRenderer htmlRenderer = CreateHtmlRenderer(loggerFactory, _knownDllPaths);

		// Resolve WASM app assets if in WASM mode
		WasmAppAssetResolver? wasmResolver = null;
		if (_mode == BlazorPreviewMode.Wasm)
		{
			string? wasmAppPath = null;
			if (context.Options.TryGetValue("wasmAppPath", out object? pathObj) && pathObj is string pathStr)
			{
				wasmAppPath = pathStr;
			}

			wasmResolver = new WasmAppAssetResolver(wasmAppPath, buildContext.RootDirectory);
			if (!wasmResolver.IsAvailable)
			{
				context.LogWarning("WASM preview app not found — falling back to SSR mode. " +
				                   "Set the 'wasmAppPath' plugin option or install the Moka.Blazor.Repl.Wasm NuGet package.");
				_mode = BlazorPreviewMode.Ssr;
			}
		}

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html) ||
			    !html.Contains("data-blazor-preview=\"true\"", StringComparison.Ordinal))
			{
				continue;
			}

			if (_mode == BlazorPreviewMode.Wasm)
			{
				html = await CompileForWasmAsync(html, _compilationService, _extraUsings, _knownDllPaths,
					htmlRenderer, buildContext, context, ct);
			}
			else
			{
				html = await CompileAndRenderBlocksAsync(html, _compilationService, _extraUsings, _knownDllPaths,
					htmlRenderer, context, ct);
			}

			page.Content = page.Content with
			{
				Html = InjectPreviewAssets(html, _previewCssBundle is { Length: > 0 }, _mode)
			};
			pagesWithPreview++;
		}

		// Copy WASM app to output if we have preview pages
		if (_mode == BlazorPreviewMode.Wasm && pagesWithPreview > 0 && wasmResolver?.IsAvailable == true)
		{
			buildContext.DeferredOutputDirectories.Add((wasmResolver.WasmAppDirectory!, "_preview-wasm"));
		}

		if (pagesWithPreview > 0)
		{
			context.LogInfo(
				$"Blazor preview plugin: Rendered {pagesWithPreview} page(s) with {_mode} component previews");
		}
	}

	// ── Compilation service setup ──────────────────────────────────────────────

	private RoslynCompilationService CreateCompilationService(IPluginContext context, string rootDir)
	{
		var svc = new RoslynCompilationService(new HttpClient());

		if (context.Options.TryGetValue("references", out object? refsObj)
		    && refsObj is IEnumerable<object> refList)
		{
			foreach (string refEntry in refList.Select(o => o.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)))
			{
				string resolved = Path.GetFullPath(Path.Combine(rootDir, refEntry));

				if (Directory.Exists(resolved))
				{
					foreach (string dll in Directory.GetFiles(resolved, "*.dll"))
					{
						try
						{
							svc.AddReference(MetadataReference.CreateFromFile(dll));
							_knownDllPaths.Add(dll);
						}
						catch
						{
							// Skip unloadable DLLs silently
						}
					}
				}
				else if (File.Exists(resolved))
				{
					try
					{
						svc.AddReference(MetadataReference.CreateFromFile(resolved));
						_knownDllPaths.Add(resolved);
					}
					catch
					{
						// Skip unloadable DLLs silently
					}
				}
			}
		}

		if (context.Options.TryGetValue("usings", out object? usingsObj)
		    && usingsObj is IEnumerable<object> usingsList)
		{
			_extraUsings.AddRange(usingsList.Select(o => o.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)));
		}

		return svc;
	}

	// ── CSS bundle ────────────────────────────────────────────────────────────

	/// <summary>
	///     Reads all CSS files listed under the <c>stylesheets</c> plugin option and
	///     concatenates them into a single bundle that will be served at
	///     <c>/_preview-css/moka-preview.css</c>.
	/// </summary>
	private static byte[] BuildCssBundle(IPluginContext context, string rootDir)
	{
		if (!context.Options.TryGetValue("stylesheets", out object? sheetsObj)
		    || sheetsObj is not IEnumerable<object> sheetList)
		{
			return [];
		}

		var bundle = new StringBuilder();

		foreach (string sheet in sheetList.Select(o => o.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)))
		{
			string sourcePath = Path.GetFullPath(Path.Combine(rootDir, sheet));
			if (!File.Exists(sourcePath))
			{
				continue;
			}

			bundle.AppendLine($"/* {Path.GetFileName(sourcePath)} */");
			bundle.AppendLine(File.ReadAllText(sourcePath));
		}

		return bundle.Length == 0 ? [] : Encoding.UTF8.GetBytes(bundle.ToString());
	}

	// ── HtmlRenderer ──────────────────────────────────────────────────────────

	private static HtmlRenderer CreateHtmlRenderer(ILoggerFactory loggerFactory, IEnumerable<string> knownDllPaths)
	{
		// Pre-load all project DLLs into the default load context so that:
		//   (a) TryRegisterStub can find injectable service types by name, and
		//   (b) PreviewAssemblyLoadContext.Load will reuse the same Type instances
		//       (it tries Default.LoadFromAssemblyName first), keeping ServiceProvider
		//       type identity consistent with the compiled component's injected types.
		foreach (string dll in knownDllPaths)
		{
			try
			{
				_ = Assembly.LoadFrom(dll);
			}
			catch
			{
				/* ignore if already loaded or unavailable */
			}
		}

		var services = new ServiceCollection();
		services.AddSingleton(loggerFactory);
		services.AddSingleton(loggerFactory);
		services.AddSingleton<IJSRuntime>(NullJsRuntime.Instance);
		services.AddSingleton<NavigationManager>(NullNavigationManager.Instance);

		// Register stub implementations for common injectable Moka.Red services so that
		// components using @inject won't fail at render time in static SSR context.
		TryRegisterStub(services, "Moka.Red.Feedback.Toast.IMokaToastService",
			"Moka.Red.Feedback.Toast.MokaToastService");

		ServiceProvider sp = services.BuildServiceProvider();
		return new HtmlRenderer(sp, loggerFactory);
	}

	/// <summary>
	///     For each blazor-preview render placeholder in the HTML, compiles the snippet
	///     with Roslyn, loads the resulting assembly, renders the component to HTML using
	///     <see cref="HtmlRenderer" />, and replaces the placeholder with the rendered output.
	/// </summary>
	private static async Task<string> CompileAndRenderBlocksAsync(
		string html,
		RoslynCompilationService compilationService,
		List<string> extraUsings,
		List<string> knownDllPaths,
		HtmlRenderer htmlRenderer,
		IPluginContext context,
		CancellationToken ct)
	{
		const string sourceStart = "<div class=\"blazor-preview-source\">";
		const string renderMarker = "<div class=\"blazor-preview-render\"></div>";

		int searchPos = 0;
		var sb = new StringBuilder(html.Length);

		// Accumulates @code blocks from previously-rendered snippets on this page so that
		// later snippets can reference variables/types defined in earlier ones.
		var accumulatedCodeBlocks = new List<string>();

		while (true)
		{
			int renderIdx = html.IndexOf(renderMarker, searchPos, StringComparison.Ordinal);
			if (renderIdx < 0)
			{
				sb.Append(html, searchPos, html.Length - searchPos);
				break;
			}

			// Find the source code block preceding this render marker
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
				// Track how many @code blocks were accumulated from PREVIOUS snippets
				// before we potentially add the current snippet's own blocks.
				int prevAccumCount = accumulatedCodeBlocks.Count;

				// Record this snippet's own @code blocks so they're available for
				// later snippets on the same page.
				string extracted = ExtractCodeBlocks(source);
				if (!string.IsNullOrWhiteSpace(extracted))
				{
					accumulatedCodeBlocks.Add(extracted);
				}

				try
				{
					// ── Pass 1: compile the snippet as-is (self-contained) ──────────────
					var project = new ReplProject { Name = "DocsPreview" };
					project.Files.Add(ProjectFile.CreateRazor("Preview.razor", source));
					foreach (string u in extraUsings)
					{
						project.GlobalUsings.Add(u);
					}

					CompilationResult result = await compilationService.CompileAsync(project, ct);

					// ── Pass 2: if pass 1 has only "name not found" errors and there is
					//    accumulated context from prior snippets, retry with that context
					//    prepended.  This resolves cross-snippet state dependencies
					//    (e.g. _people defined in snippet 1, used in snippet 2) without
					//    introducing CS0102 duplicate-definition errors for self-contained
					//    snippets that redeclare common names like _open or _selected. ──
					if (!result.Success
					    && prevAccumCount > 0 // there are @code blocks from previous snippets
					    && result.Errors.All(e => e.Id is "CS0103" or "CS0246" or "CS0012"))
					{
						// Prepend @code from all PREVIOUS snippets only (not the current
						// snippet's own @code, which is already part of 'source').
						string preamble = string.Join("\n",
							accumulatedCodeBlocks.Take(prevAccumCount));
						string fullSource = preamble + "\n" + source;

						var retryProject = new ReplProject { Name = "DocsPreview" };
						retryProject.Files.Add(ProjectFile.CreateRazor("Preview.razor", fullSource));
						foreach (string u in extraUsings)
						{
							retryProject.GlobalUsings.Add(u);
						}

						CompilationResult retryResult = await compilationService.CompileAsync(retryProject, ct);
						if (retryResult.Success ||
						    retryResult.Errors.Count() < result.Errors.Count())
						{
							result = retryResult;
						}
					}

					if (result.Success && result.AssemblyBytes is not null)
					{
						string renderedHtml = await RenderComponentAsync(
							result.AssemblyBytes, result.EntryPointTypeName, knownDllPaths, htmlRenderer, context);
						sb.Append($"<div class=\"blazor-preview-render\">{renderedHtml}</div>");
					}
					else
					{
						string errors = string.Join("\n", result.Errors.Select(e =>
							$"[{e.Id}] {(e.FilePath != null ? $"{e.FilePath}({e.Line},{e.Column}): " : "")}{e.Message}"));
						sb.Append(
							$"<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(errors)}</div></div>");
						context.LogWarning($"Blazor preview compile errors:\n{errors}");
					}
				}
				catch (Exception ex)
				{
					context.LogWarning($"Blazor preview compile failed: {ex.Message}");
					sb.Append(
						$"<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(ex.Message)}</div></div>");
				}
			}
			else
			{
				sb.Append(renderMarker);
			}

			searchPos = renderIdx + renderMarker.Length;
		}

		return sb.ToString();
	}

	/// <summary>
	///     Extracts all <c>@code { ... }</c> blocks from a Razor snippet so they can be
	///     prepended to subsequent snippets on the same page as shared state.
	/// </summary>
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

			// Skip past "@code" and optional whitespace to find the opening brace
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

			// Count braces to find the matching closing brace
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

			// j is now one past the closing '}'
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
		// Build a lookup of assembly-name → physical DLL path so the load context
		// can resolve Moka.Red.* and other project DLLs that aren't in the process yet.
		var dllLookup = knownDllPaths
			.GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

		// Collectible context so it can be unloaded after each render (avoids assembly leak)
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
					// Compare against IComponent resolved in the default context to avoid cross-context issues
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

	// ── WASM compilation ──────────────────────────────────────────────────────

	/// <summary>
	///     For each blazor-preview block, compiles the snippet to a DLL, saves it as a deferred
	///     output file, renders SSR fallback HTML, and replaces the placeholder with an iframe
	///     that loads the WASM preview app with the compiled assembly.
	/// </summary>
	private static async Task<string> CompileForWasmAsync(
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

					// Retry with accumulated context if needed
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
						// Hash the source for a stable DLL filename
						string hash = Convert.ToHexString(
							SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..12].ToLowerInvariant();
						string dllPath = $"_preview-assemblies/{hash}.dll";
						string entryPoint = result.EntryPointTypeName ?? "MokaRepl.Preview";

						// Register the compiled DLL as a deferred output file
						buildContext.DeferredOutputFiles[dllPath] = result.AssemblyBytes;

						// Also render SSR fallback
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

						// Emit iframe + SSR fallback
						sb.Append($"""
						           <div class="blazor-preview-render">
						           <iframe class="blazor-preview-iframe" src="/_preview-wasm/index.html?assembly=/{dllPath}&amp;entry={WebUtility.HtmlEncode(entryPoint)}" loading="lazy" sandbox="allow-scripts allow-same-origin"></iframe>
						           <noscript><div class="blazor-preview-ssr-fallback">{ssrHtml}</div></noscript>
						           </div>
						           """);
					}
					else
					{
						string errors = string.Join("\n", result.Errors.Select(e =>
							$"[{e.Id}] {(e.FilePath != null ? $"{e.FilePath}({e.Line},{e.Column}): " : "")}{e.Message}"));
						sb.Append(
							$"<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(errors)}</div></div>");
						context.LogWarning($"Blazor preview compile errors:\n{errors}");
					}
				}
				catch (Exception ex)
				{
					context.LogWarning($"Blazor preview compile failed: {ex.Message}");
					sb.Append(
						$"<div class=\"blazor-preview-render\"><div class=\"blazor-preview-error\">{WebUtility.HtmlEncode(ex.Message)}</div></div>");
				}
			}
			else
			{
				sb.Append(renderMarker);
			}

			searchPos = renderIdx + renderMarker.Length;
		}

		return sb.ToString();
	}

	// ── Asset injection ───────────────────────────────────────────────────────

	private static string InjectPreviewAssets(string html, bool hasCssBundle, BlazorPreviewMode mode)
	{
		string js = mode == BlazorPreviewMode.Wasm ? _wasmJs : _inlineJs;
		string css = mode == BlazorPreviewMode.Wasm ? _inlineCss + _wasmCss : _inlineCss;

		var sb = new StringBuilder(html.Length + css.Length + js.Length + 80);

		if (hasCssBundle)
		{
			sb.Append("<link rel=\"stylesheet\" href=\"/_preview-css/moka-preview.css\">");
		}

		sb.Append(css);
		sb.Append(html);
		sb.Append(js);
		return sb.ToString();
	}

	// ── Service stub registration ─────────────────────────────────────────────

	/// <summary>
	///     Attempts to register a service interface with its implementation by type name.
	///     Uses reflection so the plugin doesn't need a compile-time reference to the DLL.
	/// </summary>
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

	// ── Null JS runtime ───────────────────────────────────────────────────────

	/// <summary>
	///     No-op <see cref="IJSRuntime" /> used during static SSR rendering so that components
	///     that inject <c>IJSRuntime</c> can be instantiated without a browser context.
	///     JS calls during <c>OnAfterRenderAsync</c> are never reached by <see cref="HtmlRenderer" />.
	/// </summary>
	private sealed class NullJsRuntime : IJSRuntime
	{
		public static readonly NullJsRuntime Instance = new();

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> ValueTask.FromResult(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
			object?[]? args)
			=> ValueTask.FromResult(default(TValue)!);
	}

	// ── Null navigation manager ───────────────────────────────────────────────

	/// <summary>
	///     No-op <see cref="NavigationManager" /> for static SSR so components that inject
	///     it (e.g., for NavigateTo calls wired to buttons) can be instantiated.
	/// </summary>
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

	// ── Assembly load context ─────────────────────────────────────────────────

	/// <summary>
	///     Collectible <see cref="AssemblyLoadContext" /> that resolves Moka.Red (and other project)
	///     DLLs by name so that a compiled preview assembly can load its dependencies without
	///     polluting the host process's default load context.
	/// </summary>
	private sealed class PreviewAssemblyLoadContext(
		Dictionary<string, string> dllLookup)
		: AssemblyLoadContext("BlazorPreviewCtx", true)
	{
		protected override Assembly? Load(AssemblyName assemblyName)
		{
			// Try to resolve from the host process first (framework + already-loaded assemblies).
			// This keeps IComponent, ILoggerFactory, etc. from the same context as the host,
			// which avoids type-identity mismatches when HtmlRenderer casts the component.
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
				// Not in default context — fall through to our DLL lookup.
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

	#region WASM CSS & JS

	private const string _wasmCss = """
	                                <style>
	                                .blazor-preview-iframe {
	                                    width: 100%;
	                                    border: none;
	                                    min-height: 60px;
	                                    display: block;
	                                    background: var(--color-bg, #ffffff);
	                                }
	                                .blazor-preview-ssr-fallback {
	                                    padding: 1.25rem 1.5rem;
	                                }
	                                </style>
	                                """;

	private const string _wasmJs = """
	                               <script>
	                               (function() {
	                                   // Auto-resize iframes based on content height
	                                   window.addEventListener('message', function(e) {
	                                       if (e.data && e.data.type === 'resize') {
	                                           document.querySelectorAll('.blazor-preview-iframe').forEach(function(iframe) {
	                                               if (iframe.contentWindow === e.source) {
	                                                   iframe.style.height = (e.data.height + 16) + 'px';
	                                               }
	                                           });
	                                       }
	                                   });

	                                   // Set up tabs for each preview container
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
	                                       badge.textContent = 'Blazor WASM';

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
}
