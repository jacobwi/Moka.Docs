using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Blazor.Repl.Abstractions.Interfaces;
using Moka.Blazor.Repl.Abstractions.Models;

namespace Moka.Docs.Serve;

/// <summary>
///     Renders Blazor/Razor component source into a static HTML preview using real
///     Roslyn compilation and Blazor's HtmlRenderer for server-side rendering.
/// </summary>
public sealed class BlazorPreviewService
{
	private const int _maxSourceLength = 50_000;

	private readonly ICompilationService _compilationService;
	private readonly IReadOnlyList<string> _extraUsings;
	private readonly ILogger<BlazorPreviewService> _logger;
	private readonly ILoggerFactory _loggerFactory;
	private readonly IServiceProvider _serviceProvider;

	public BlazorPreviewService(
		ICompilationService compilationService,
		ILoggerFactory loggerFactory,
		ILogger<BlazorPreviewService> logger,
		IEnumerable<string>? extraUsings = null,
		IEnumerable<string>? runtimeAssemblyPaths = null)
	{
		_compilationService = compilationService;
		_loggerFactory = loggerFactory;
		_logger = logger;
		_extraUsings = extraUsings?.ToList() ?? [];

		// Pre-load runtime assemblies so HtmlRenderer can resolve child component types.
		// MetadataReference.CreateFromFile only covers compilation; for rendering we need
		// the assemblies actually present in the AppDomain.
		if (runtimeAssemblyPaths is not null)
		{
			foreach (string path in runtimeAssemblyPaths)
			{
				try
				{
					Assembly.LoadFrom(path);
				}
				catch (Exception ex)
				{
					logger.LogWarning("Could not load runtime assembly {Path}: {Message}", path, ex.Message);
				}
			}
		}

		// Minimal service provider for HtmlRenderer — components rendered in preview
		// don't have access to app-level services (intentional isolation)
		var services = new ServiceCollection();
		services.AddLogging(b => b.AddProvider(new ForwardingLoggerProvider(loggerFactory)));
		_serviceProvider = services.BuildServiceProvider();
	}

	/// <summary>
	///     Renders the given Blazor/Razor component source to an HTML preview
	///     using real Roslyn compilation and Blazor HtmlRenderer.
	/// </summary>
	public async Task<BlazorPreviewResult> RenderAsync(string source, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return new BlazorPreviewResult { Error = "No source code provided." };
		}

		if (source.Length > _maxSourceLength)
		{
			return new BlazorPreviewResult
				{ Error = $"Source exceeds maximum length of {_maxSourceLength:N0} characters." };
		}

		try
		{
			// 1. Build a project from the source
			var project = new ReplProject { Name = "BlazorPreview" };
			project.Files.Add(ProjectFile.CreateRazor("Preview.razor", source));

			// Add common usings
			project.GlobalUsings.Add("System");
			project.GlobalUsings.Add("System.Collections.Generic");
			project.GlobalUsings.Add("System.Linq");
			project.GlobalUsings.Add("Microsoft.AspNetCore.Components");
			project.GlobalUsings.Add("Microsoft.AspNetCore.Components.Web");

			// Add any extra usings provided at construction time (e.g., Moka.Red namespaces)
			foreach (string u in _extraUsings)
			{
				project.GlobalUsings.Add(u);
			}

			// 2. Compile with Roslyn
			CompilationResult result = await _compilationService.CompileAsync(project, ct);
			if (!result.Success)
			{
				IEnumerable<string> errors = result.Diagnostics
					.Where(d => d.Severity == DiagnosticSeverity.Error)
					.Select(d => d.Message);
				return new BlazorPreviewResult { Error = string.Join("\n", errors) };
			}

			if (result.AssemblyBytes is null)
			{
				return new BlazorPreviewResult { Error = "Compilation produced no assembly." };
			}

			// 3. Load assembly and find the component type
			var assembly = Assembly.Load(result.AssemblyBytes);
			Type? componentType = !string.IsNullOrEmpty(result.EntryPointTypeName)
				? assembly.GetType(result.EntryPointTypeName)
				: null;

			componentType ??= assembly.GetTypes()
				.FirstOrDefault(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract);

			if (componentType is null)
			{
				return new BlazorPreviewResult { Error = "No renderable component found in compiled assembly." };
			}

			// 4. Render to HTML using Blazor's HtmlRenderer
			await using var htmlRenderer = new HtmlRenderer(_serviceProvider, _loggerFactory);
			string html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
			{
				HtmlRootComponent output = await htmlRenderer.RenderComponentAsync(componentType);
				return output.ToHtmlString();
			});

			_logger.LogDebug(
				"Blazor preview: Compiled and rendered {Length} chars of source to {HtmlLength} chars of HTML",
				source.Length, html.Length);

			return new BlazorPreviewResult { Html = html };
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Blazor preview: Rendering error");
			return new BlazorPreviewResult { Error = $"Rendering error: {ex.Message}" };
		}
	}

	/// <summary>
	///     Forwards log messages from the HtmlRenderer's internal service provider
	///     to the host application's logging infrastructure.
	/// </summary>
	private sealed class ForwardingLoggerProvider(ILoggerFactory factory) : ILoggerProvider
	{
		public ILogger CreateLogger(string categoryName) => factory.CreateLogger(categoryName);

		public void Dispose()
		{
		}
	}
}

/// <summary>
///     The result of rendering a Blazor component preview.
/// </summary>
public sealed class BlazorPreviewResult
{
	/// <summary>The rendered HTML preview. Null if an error occurred.</summary>
	public string? Html { get; init; }

	/// <summary>Error message if rendering failed. Null if successful.</summary>
	public string? Error { get; init; }
}
