using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
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
		var warningsAsErrorsOption = new Option<bool>("--warnings-as-errors")
			{ Description = "Fail the build (exit 1) when it reports any warning, like build.strict: true" };
		var versionOption = new Option<string?>("--version")
			{ Description = "Build only this version (slug or label) into its folder of the output" };

		var command = new Command("build", "Build the documentation site")
		{
			watchOption,
			configOption,
			outputOption,
			verboseOption,
			draftOption,
			noCacheOption,
			basePathOption,
			warningsAsErrorsOption,
			versionOption
		};

		command.SetAction(async (parseResult, ct) =>
		{
			bool watch = parseResult.GetValue(watchOption);
			string? configPath = parseResult.GetValue(configOption);
			string? output = parseResult.GetValue(outputOption);
			bool verbose = parseResult.GetValue(verboseOption);
			bool draft = parseResult.GetValue(draftOption);
			bool noCache = parseResult.GetValue(noCacheOption);
			string? basePath = parseResult.GetValue(basePathOption);
			bool warningsAsErrors = parseResult.GetValue(warningsAsErrorsOption);

			var sw = Stopwatch.StartNew();

			AnsiConsole.MarkupLine($"[bold blue]MokaDocs[/] [dim]v{CliVersion.Current}[/] - Building documentation site...");
			AnsiConsole.WriteLine();

			string resolvedConfigPath = ConfigPath.Resolve(configPath);
			var source = new ConfigSource(resolvedConfigPath, output, basePath);
			string rootDir = source.RootDirectory;

			// Load config, with the CLI overrides applied
			SiteConfig config;
			string configText;
			try
			{
				(config, configText) = BuildSession.Read(source);
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

			string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));

			string? onlyVersion = parseResult.GetValue(versionOption);
			if (VersionedBuild.Applies(config, onlyVersion))
			{
				return await VersionedBuild.RunAsync(new VersionedBuildRequest
				{
					Config = config,
					ConfigPath = resolvedConfigPath,
					OutputDirectory = outputDir,
					IncludeDrafts = draft,
					UseCache = !noCache && config.Build.Cache,
					Only = onlyVersion is null ? [] : [onlyVersion]
				}, verbose, warningsAsErrors || config.Build.Strict, watch);
			}

			// Set up DI and initialize plugins once; they inject pages on every pipeline run.
			await using BuildSession session = await BuildSession.StartAsync(source, config, configText, verbose, ct);

			BuildRunOptions RunOptions(SiteConfig current) => new(
				current, rootDir, outputDir, draft, !noCache && current.Build.Cache, verbose,
				warningsAsErrors || current.Build.Strict);

			int exitCode = await RunBuildAsync(session.Pipeline, session.VersionManager, RunOptions(session.Config), sw);

			if (!watch)
			{
				return exitCode;
			}

			#region Watch Loop

			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
			using ILoggerFactory loggerFactory = CreateLoggerFactory(verbose);

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				cts.Cancel();
			};

			await WatchAsync(docsDir, resolvedConfigPath, WatchIgnoredDirectories(rootDir, outputDir),
				loggerFactory.CreateLogger<FileWatcher>(),
				async () =>
				{
					ConfigReload reload = await session.ReloadIfChangedAsync(false, cts.Token);
					BuildSession.Print(reload, $"The last good build stays in {config.Build.Output}");
					if (reload.Outcome != ConfigReloadOutcome.Failed)
					{
						await RunBuildAsync(session.Pipeline, session.VersionManager, RunOptions(session.Config),
							Stopwatch.StartNew());
					}
				},
				cts.Token);

			AnsiConsole.MarkupLine("[dim]Stopped watching[/]");
			return exitCode;

			#endregion
		});

		return command;
	}

	/// <summary>
	///     Folders whose changes never trigger a watch rebuild: the output, which a rebuild writes,
	///     and the <c>.mokadocs</c> cache. Shared with <c>serve</c>. Without them, <c>build --watch</c>
	///     with the output inside the docs folder rebuilt in a loop, 39 times in 14 seconds.
	/// </summary>
	internal static IReadOnlyList<string> WatchIgnoredDirectories(string rootDirectory, string outputDirectory) =>
		[outputDirectory, Path.Combine(rootDirectory, ".mokadocs")];

	/// <summary>
	///     Runs <paramref name="rebuild" /> after each change under <paramref name="docsDirectory" />
	///     or to the config file, until <paramref name="ct" /> is cancelled. The watcher runs one
	///     rebuild at a time and queues one more for changes made during it.
	/// </summary>
	internal static async Task WatchAsync(string docsDirectory, string configPath,
		IEnumerable<string> ignoredDirectories, ILogger<FileWatcher> logger, Func<Task> rebuild, CancellationToken ct)
	{
		using var watcher = new FileWatcher(logger);
		watcher.OnChanged += async () =>
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[yellow]Changes detected, rebuilding...[/]");
			await rebuild();
		};

		watcher.Start(docsDirectory, configPath, ignoredDirectories);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold green]Watching[/] [dim]{Markup.Escape(docsDirectory)}[/]");
		AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");

		try
		{
			await Task.Delay(Timeout.Infinite, ct);
		}
		catch (OperationCanceledException)
		{
			// Ctrl+C is the normal way out of watch mode.
		}
	}

	/// <summary>
	///     Discovers and initializes the plugins declared in mokadocs.yaml and attaches them
	///     to the pipeline's plugin hook. Shared by build, serve, validate and doctor so the
	///     four commands cannot drift in how plugins are loaded.
	/// </summary>
	/// <returns>
	///     The host, whose <see cref="PluginHost.LoadedPlugins" /> reports what loaded and
	///     <see cref="PluginHost.LoadFailures" /> what a <c>path</c> failed to load.
	/// </returns>
	internal static async Task<PluginHost> InitializePluginsAsync(IServiceProvider provider, BuildPipeline pipeline,
		CancellationToken ct = default)
	{
		PluginHost pluginHost = provider.GetRequiredService<PluginHost>();
		await pluginHost.DiscoverAndInitializeAsync(ct);

		// Also with no plugin loaded: the hook reports the declarations that failed to load.
		pipeline.PluginHook = pluginHost.ExecuteAllAsync;

		return pluginHost;
	}

	/// <summary>
	///     Inputs for a single build run. Watch mode makes new ones from the configuration
	///     <see cref="BuildSession" /> reloads.
	/// </summary>
	private sealed record BuildRunOptions(
		SiteConfig Config,
		string RootDirectory,
		string OutputDirectory,
		bool IncludeDrafts,
		bool UseCache,
		bool Verbose,
		bool WarningsAsErrors);

	/// <summary>
	///     Runs the pipeline once and prints the build summary.
	/// </summary>
	/// <returns>
	///     0 on success, 1 when the pipeline threw or reported an error, or a warning while
	///     warnings count as errors.
	/// </returns>
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

		versionManager.ApplyTo(context);

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

		bool failed = Fails(context.Diagnostics, options.WarningsAsErrors);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(failed
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

		PrintDiagnostics(context.Diagnostics, options.Verbose, options.WarningsAsErrors);

		#endregion

		// A build that reported errors has missing or broken output. Exiting 0 let CI publish it.
		return failed ? 1 : 0;
	}

	/// <summary>
	///     Whether a build with these diagnostics fails: on any error, and on any warning when
	///     warnings count as errors (<c>build.strict</c> or <c>--warnings-as-errors</c>).
	/// </summary>
	internal static bool Fails(DiagnosticBag diagnostics, bool warningsAsErrors) =>
		diagnostics.HasErrors || (warningsAsErrors && diagnostics.HasWarnings);

	/// <summary>
	///     Prints the warning and error counts, every error, and the warnings when verbose.
	///     <c>serve</c> uses it too; it used to print nothing about a build's diagnostics.
	/// </summary>
	internal static void PrintDiagnostics(DiagnosticBag diagnostics, bool verbose, bool warningsAsErrors = false)
	{
		if (!diagnostics.HasWarnings && !diagnostics.HasErrors)
		{
			return;
		}

		int warnings = diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Warning);
		int errors = diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Error);
		AnsiConsole.MarkupLine($"⚠️  Diagnostics:  {warnings} warnings, {errors} errors");

		// Errors are always listed, since they fail the build, and so are warnings when they count
		// as errors. Other warnings only with --verbose.
		bool showWarnings = verbose || warningsAsErrors;
		foreach (Diagnostic diag in diagnostics.All.Where(d =>
			         d.Severity == DiagnosticSeverity.Error
			         || (showWarnings && d.Severity == DiagnosticSeverity.Warning)))
		{
			string color = diag.Severity == DiagnosticSeverity.Error || warningsAsErrors ? "red" : "yellow";
			AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(diag.ToString())}[/]");
		}

		if (warningsAsErrors && warnings > 0)
		{
			AnsiConsole.MarkupLine("    [dim]Warnings fail this build (build.strict or --warnings-as-errors)[/]");
		}
		else if (!showWarnings && warnings > 0)
		{
			AnsiConsole.MarkupLine("    [dim]Run with --verbose to see warnings[/]");
		}
	}

	/// <summary>
	///     Builds the service provider with all MokaDocs services and plugins.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	/// <param name="verbose">Whether log output is printed.</param>
	/// <param name="rootDirectory">
	///     The folder holding mokadocs.yaml, which relative <c>plugins[].path</c> values resolve from.
	///     The working directory when <c>null</c>.
	/// </param>
	internal static ServiceProvider BuildServices(SiteConfig config, bool verbose, string? rootDirectory = null)
	{
		var services = new ServiceCollection();
		services.AddLogging(b => ConfigureLogging(b, verbose));

		// AddMokaDocsEngine registers feature management, including MOKADOCS_ environment
		// variable overrides. This used to be registered here a second time.
		services.AddSingleton<IFileSystem>(new FileSystem());
		services.AddSingleton(config);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();
		services.AddMokaDocsVersioning();

		// Every built-in, since the CLI runs whatever the site declares, and plugins from a path.
		services.AddMokaDocsPlugins(options =>
			options.PluginBaseDirectory = rootDirectory ?? Directory.GetCurrentDirectory());

		return services.BuildServiceProvider();
	}

	/// <summary>
	///     A logger factory with the same output as <see cref="BuildServices" />, for what outlives one
	///     service container: the file watcher, the dev server and the REPL under <c>serve</c>.
	/// </summary>
	internal static ILoggerFactory CreateLoggerFactory(bool verbose) =>
		LoggerFactory.Create(b => ConfigureLogging(b, verbose));

	private static void ConfigureLogging(ILoggingBuilder builder, bool verbose)
	{
		if (verbose)
		{
			// Without a provider every ILogger call in the pipeline is discarded, which
			// made --verbose affect only the diagnostics summary printed below. Quiet
			// builds stay provider-free so the Spectre output is the only thing on
			// stdout; DiagnosticBag still reports warnings in the build summary.
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddSimpleConsole(o =>
			{
				o.SingleLine = true;
				o.TimestampFormat = null;
			});
		}
		else
		{
			builder.SetMinimumLevel(LogLevel.Warning);
		}
	}
}
