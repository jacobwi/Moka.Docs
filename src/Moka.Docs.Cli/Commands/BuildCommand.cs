using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Blazor.Repl.Abstractions.Interfaces;
using Moka.Blazor.Repl.Compiler;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.BlazorPreview;
using Moka.Docs.Plugins.Changelog;
using Moka.Docs.Plugins.OpenApi;
using Moka.Docs.Plugins.PythonApi;
using Moka.Docs.Plugins.Repl;
using Moka.Docs.Serve;
using Moka.Docs.Versioning;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Runs the full build pipeline to generate the static site.
/// </summary>
internal static class BuildCommand
{
	/// <summary>Creates the build command.</summary>
	public static Command Create()
	{
		var watchOption = new Option<bool>("--watch") { Description = "Watch for changes and rebuild automatically" };
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };
		var outputOption = new Option<string?>("--output", "-o")
			{ Description = "Output directory (default: build.output from the config)" };
		var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose logging" };
		var draftOption = new Option<bool>("--draft") { Description = "Include draft pages" };
		var noCacheOption = new Option<bool>("--no-cache") { Description = "Force full rebuild without caching" };
		var basePathOption = new Option<string?>("--base-path")
			{ Description = "Base path for subdirectory deployment (e.g., /Moka.Docs for GitHub Pages)" };

		var command = new Command("build", "Build the documentation site")
		{
			watchOption,
			configOption,
			outputOption,
			verboseOption,
			draftOption,
			noCacheOption,
			basePathOption
		};

		command.SetAction(async (parseResult, _) =>
		{
			bool watch = parseResult.GetValue(watchOption);
			string? configPath = parseResult.GetValue(configOption);
			string? output = parseResult.GetValue(outputOption);
			bool verbose = parseResult.GetValue(verboseOption);
			bool draft = parseResult.GetValue(draftOption);
			bool noCache = parseResult.GetValue(noCacheOption);
			string? basePath = parseResult.GetValue(basePathOption);

			var sw = Stopwatch.StartNew();

			AnsiConsole.MarkupLine($"[bold blue]MokaDocs[/] [dim]v{CliVersion.Current}[/] - Building documentation site...");
			AnsiConsole.WriteLine();

			string resolvedConfigPath = ConfigPath.Resolve(configPath);
			string rootDir = Path.GetDirectoryName(resolvedConfigPath)!;

			// Load config
			SiteConfig config;
			try
			{
				var fs = new FileSystem();
				var reader = new SiteConfigReader(fs);
				config = reader.Read(resolvedConfigPath);
				AnsiConsole.MarkupLine(
					$"[green]✓ Config:[/] {Path.GetFileName(resolvedConfigPath)} - \"{Markup.Escape(config.Site.Title)}\"");
			}
			catch (FileNotFoundException)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Config file not found: {resolvedConfigPath}");
				AnsiConsole.MarkupLine("Run [bold]mokadocs init[/] to create a starter project.");
				return 1;
			}
			catch (SiteConfigException ex)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
				return 1;
			}

			// Apply CLI overrides
			if (output is not null)
			{
				config = config with { Build = config.Build with { Output = output } };
			}

			if (basePath is not null)
			{
				config = config with
				{
					Build = config.Build with { BasePath = SiteConfigReader.NormalizeBasePath(basePath) }
				};
			}

			string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));

			// Set up DI
			await using ServiceProvider provider = BuildServices(config, verbose);
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			VersionManager versionManager = provider.GetRequiredService<VersionManager>();

			// Initialize plugins once; they inject pages on every pipeline run.
			await InitializePluginsAsync(provider, pipeline);

			var options = new BuildRunOptions(
				config, rootDir, outputDir, draft, !noCache && config.Build.Cache, verbose);

			int exitCode = await RunBuildAsync(pipeline, versionManager, options, sw);

			if (!watch)
			{
				return exitCode;
			}

			#region Watch Loop

			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
			ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
			using var watcher = new FileWatcher(loggerFactory.CreateLogger<FileWatcher>());

			watcher.OnChanged += async () =>
			{
				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine("[yellow]Changes detected, rebuilding...[/]");
				await RunBuildAsync(pipeline, versionManager, options, Stopwatch.StartNew());
			};

			watcher.Start(docsDir, resolvedConfigPath);

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[bold green]Watching[/] [dim]{Markup.Escape(docsDir)}[/]");
			AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");

			using var cts = new CancellationTokenSource();
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				cts.Cancel();
			};

			try
			{
				await Task.Delay(Timeout.Infinite, cts.Token);
			}
			catch (OperationCanceledException)
			{
				// Ctrl+C is the normal way out of watch mode.
			}

			AnsiConsole.MarkupLine("[dim]Stopped watching[/]");
			return exitCode;

			#endregion
		});

		return command;
	}

	/// <summary>
	///     Discovers and initializes the plugins declared in mokadocs.yaml and attaches them
	///     to the pipeline's plugin hook. Shared by build, serve, validate and doctor so the
	///     four commands cannot drift in how plugins are loaded.
	/// </summary>
	/// <returns>The host, whose <see cref="PluginHost.LoadedPlugins" /> reports what loaded.</returns>
	internal static async Task<PluginHost> InitializePluginsAsync(IServiceProvider provider, BuildPipeline pipeline)
	{
		PluginHost pluginHost = provider.GetRequiredService<PluginHost>();
		await pluginHost.DiscoverAndInitializeAsync();

		pipeline.PluginHook = async (ctx, ct) =>
		{
			if (pluginHost.LoadedPlugins.Count > 0)
			{
				await pluginHost.ExecuteAllAsync(ctx, ct);
			}
		};

		return pluginHost;
	}

	/// <summary>
	///     Inputs for a single build run, so watch mode can repeat it without rebuilding
	///     the DI container or re-reading the config.
	/// </summary>
	private sealed record BuildRunOptions(
		SiteConfig Config,
		string RootDirectory,
		string OutputDirectory,
		bool IncludeDrafts,
		bool UseCache,
		bool Verbose);

	/// <summary>
	///     Runs the pipeline once and prints the build summary.
	/// </summary>
	/// <returns>0 on success, 1 when the pipeline threw or reported an error.</returns>
	private static async Task<int> RunBuildAsync(
		BuildPipeline pipeline, VersionManager versionManager, BuildRunOptions options, Stopwatch sw)
	{
		var context = new BuildContext
		{
			Config = options.Config,
			FileSystem = new FileSystem(),
			RootDirectory = options.RootDirectory,
			OutputDirectory = options.OutputDirectory,
			IncludeDrafts = options.IncludeDrafts,
			UseCache = options.UseCache
		};

		// Wire version data from VersionManager into the build context
		if (versionManager.IsEnabled)
		{
			context.Versions.AddRange(versionManager.Versions);
			context.CurrentVersion = versionManager.DefaultVersion;
		}

		try
		{
			await pipeline.ExecuteAsync(context);
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(ex.Message)}");
			if (options.Verbose)
			{
				AnsiConsole.WriteException(ex);
			}

			return 1;
		}

		sw.Stop();

		#region Build Summary

		int mdPages = context.Pages.Count(p => p.Origin == PageOrigin.Markdown);
		int apiPages = context.Pages.Count(p => p.Origin == PageOrigin.ApiGenerated);
		int apiTypes = context.ApiModel?.Namespaces.Sum(n => n.Types.Count) ?? 0;
		int searchEntries = context.SearchIndex?.Entries.Count ?? 0;

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(context.Diagnostics.HasErrors
			? $"[red bold]❌ MokaDocs build finished with errors in {sw.Elapsed.TotalSeconds:F2}s[/]"
			: $"[green bold]✅ MokaDocs build complete in {sw.Elapsed.TotalSeconds:F2}s[/]");
		AnsiConsole.MarkupLine($"📄 Pages:        {mdPages + apiPages} ({mdPages} markdown, {apiPages} generated)");

		if (apiTypes > 0)
		{
			AnsiConsole.MarkupLine(
				$"🔧 API Types:    {apiTypes} across {context.ApiModel!.Assemblies.Count} assemblies");
		}

		if (searchEntries > 0)
		{
			AnsiConsole.MarkupLine($"🔍 Search Index: {searchEntries} entries");
		}

		AnsiConsole.MarkupLine($"📦 Output:       {options.Config.Build.Output}");

		PrintDiagnostics(context.Diagnostics, options.Verbose);

		#endregion

		// A build that reported errors has missing or broken output. Exiting 0 let CI publish it.
		return context.Diagnostics.HasErrors ? 1 : 0;
	}

	/// <summary>
	///     Prints the warning and error counts, every error, and the warnings when verbose.
	///     <c>serve</c> uses it too; it used to print nothing about a build's diagnostics.
	/// </summary>
	internal static void PrintDiagnostics(DiagnosticBag diagnostics, bool verbose)
	{
		if (!diagnostics.HasWarnings && !diagnostics.HasErrors)
		{
			return;
		}

		int warnings = diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Warning);
		int errors = diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Error);
		AnsiConsole.MarkupLine($"⚠️  Diagnostics:  {warnings} warnings, {errors} errors");

		// Errors are always listed, since they fail the build. Warnings only with --verbose.
		foreach (Diagnostic diag in diagnostics.All.Where(d =>
			         d.Severity == DiagnosticSeverity.Error
			         || (verbose && d.Severity == DiagnosticSeverity.Warning)))
		{
			string color = diag.Severity == DiagnosticSeverity.Error ? "red" : "yellow";
			AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(diag.ToString())}[/]");
		}

		if (!verbose && warnings > 0)
		{
			AnsiConsole.MarkupLine("    [dim]Run with --verbose to see warnings[/]");
		}
	}

	/// <summary>
	///     Builds the service provider with all MokaDocs services and plugins.
	/// </summary>
	internal static ServiceProvider BuildServices(SiteConfig config, bool verbose)
	{
		var services = new ServiceCollection();
		services.AddLogging(b =>
		{
			if (verbose)
			{
				// Without a provider every ILogger call in the pipeline is discarded, which
				// made --verbose affect only the diagnostics summary printed below. Quiet
				// builds stay provider-free so the Spectre output is the only thing on
				// stdout; DiagnosticBag still reports warnings in the build summary.
				b.SetMinimumLevel(LogLevel.Debug);
				b.AddSimpleConsole(o =>
				{
					o.SingleLine = true;
					o.TimestampFormat = null;
				});
			}
			else
			{
				b.SetMinimumLevel(LogLevel.Warning);
			}
		});

		// AddMokaDocsEngine registers feature management, including MOKADOCS_ environment
		// variable overrides. This used to be registered here a second time.
		services.AddSingleton<IFileSystem>(new FileSystem());
		services.AddSingleton(config);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();
		services.AddMokaDocsVersioning();

		// Plugin system
		services.AddSingleton<PluginHost>();
		services.AddSingleton<IMokaPlugin, OpenApiPlugin>();
		services.AddSingleton<IMokaPlugin, ReplPlugin>();
		services.AddSingleton<IMokaPlugin, BlazorPreviewPlugin>();
		services.AddSingleton<IMokaPlugin, ChangelogPlugin>();
		services.AddSingleton<IMokaPlugin, PythonApiPlugin>();

		// Blazor preview: register compiler + preview service for build-time pre-rendering
		services.AddSingleton<ICompilationService>(
			new RoslynCompilationService(new HttpClient()));
		services.AddSingleton<BlazorPreviewService>();

		return services.BuildServiceProvider();
	}
}
