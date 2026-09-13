using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Blazor.Repl.Compiler;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Plugins;
using Moka.Docs.Serve;
using Moka.Docs.Versioning;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Builds and serves the documentation site with hot reload.
/// </summary>
internal static class ServeCommand
{
	/// <summary>Creates the serve command.</summary>
	public static Command Create()
	{
		var portOption = new Option<int>("--port", "-p")
			{ Description = "Port to serve on", DefaultValueFactory = _ => 5080 };
		var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose logging" };
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };
		var outputOption = new Option<string?>("--output", "-o")
			{ Description = "Output directory (default: build.output from the config)" };
		var openOption = new Option<bool>("--open")
			{ Description = "Open browser automatically", DefaultValueFactory = _ => true };
		var noOpenOption = new Option<bool>("--no-open") { Description = "Don't open browser automatically" };
		var draftOption = new Option<bool>("--draft") { Description = "Include draft pages" };
		var basePathOption = new Option<string?>("--base-path")
			{ Description = "Base path the site is served under (e.g. /Sub)" };

		var command = new Command("serve", "Build and serve the site locally with hot reload")
		{
			portOption,
			verboseOption,
			configOption,
			outputOption,
			openOption,
			noOpenOption,
			draftOption,
			basePathOption
		};

		command.SetAction(async (parseResult, ct) =>
		{
			int port = parseResult.GetValue(portOption);
			bool verbose = parseResult.GetValue(verboseOption);
			string? configPath = parseResult.GetValue(configOption);
			string? output = parseResult.GetValue(outputOption);
			bool open = parseResult.GetValue(openOption) && !parseResult.GetValue(noOpenOption);
			bool draft = parseResult.GetValue(draftOption);
			string? basePath = parseResult.GetValue(basePathOption);

			AnsiConsole.MarkupLine($"[bold blue]MokaDocs[/] [dim]v{CliVersion.Current}[/] - Dev server starting...");
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
					$"[green]Config:[/] {Path.GetFileName(resolvedConfigPath)} - \"{Markup.Escape(config.Site.Title)}\"");
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
			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));

			// Set up DI (reuse BuildCommand's service setup for plugins)
			await using ServiceProvider provider = BuildCommand.BuildServices(config, verbose);
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();

			// Initialize plugins
			await BuildCommand.InitializePluginsAsync(provider, pipeline);

			// Resolve version data
			VersionManager versionManager = provider.GetRequiredService<VersionManager>();

			// Run initial build
			if (!await RunBuildAsync(pipeline, config, rootDir, outputDir, versionManager, draft, verbose))
			{
				return 1;
			}

			ILogger<DevServer> serverLogger = loggerFactory.CreateLogger<DevServer>();

			// The REPL and Blazor preview endpoints run code sent to them, so they only exist
			// when the site declares the plugin that uses them. They used to be started for
			// every site.
			PluginDeclaration? replPlugin = config.Plugins.FirstOrDefault(p =>
				string.Equals(p.Name, "mokadocs-repl", StringComparison.OrdinalIgnoreCase));
			// Snippets run in a `mokadocs repl-worker` process that is killed when a run times out.
			// In-process, a snippet that never returned kept running until serve exited.
			using ReplExecutionService? replService = replPlugin is null
				? null
				: new ReplExecutionService(loggerFactory.CreateLogger<ReplExecutionService>(),
					() => CreateReplWorkerStartInfo(Environment.ProcessPath ?? "dotnet",
						typeof(ServeCommand).Assembly.Location, Environment.ProcessId));

			if (replService is not null)
			{
				await LoadReplReferencesAsync(replService, replPlugin!, config, rootDir);
				_ = replService.WarmUpAsync(ct);
			}

			ILogger<BlazorPreviewService> blazorLogger = loggerFactory.CreateLogger<BlazorPreviewService>();
			var compilationService = new RoslynCompilationService(new HttpClient());

			// Load assemblies and extra usings configured in the blazor-preview plugin options
			PluginDeclaration? blazorPlugin = config.Plugins.FirstOrDefault(p =>
				string.Equals(p.Name, "mokadocs-blazor-preview", StringComparison.OrdinalIgnoreCase));

			var blazorExtraUsings = new List<string>();
			var blazorRuntimeDlls = new List<string>();
			if (blazorPlugin is not null)
			{
				// references: list of directories or DLL paths to add as Roslyn references
				if (blazorPlugin.Options.TryGetValue("references", out object? refsObj)
				    && refsObj is IEnumerable<object> refList)
				{
					foreach (string refEntry in refList.Select(o => o.ToString()!)
						         .Where(s => !string.IsNullOrWhiteSpace(s)))
					{
						string resolvedRef = Path.GetFullPath(Path.Combine(rootDir, refEntry));

						// If it's a directory, load all *.dll files from it
						if (Directory.Exists(resolvedRef))
						{
							foreach (string dll in Directory.GetFiles(resolvedRef, "*.dll"))
							{
								try
								{
									compilationService.AddReference(MetadataReference.CreateFromFile(dll));
									blazorRuntimeDlls.Add(dll);
								}
								catch (Exception ex)
								{
									AnsiConsole.MarkupLine(
										$"[yellow]Blazor preview:[/] Could not load reference {Markup.Escape(dll)}: {Markup.Escape(ex.Message)}");
								}
							}

							AnsiConsole.MarkupLine(
								$"[blue]Blazor preview:[/] Loaded references from [bold]{Markup.Escape(resolvedRef)}[/]");
						}
						else if (File.Exists(resolvedRef))
						{
							try
							{
								compilationService.AddReference(MetadataReference.CreateFromFile(resolvedRef));
								blazorRuntimeDlls.Add(resolvedRef);
								AnsiConsole.MarkupLine(
									$"[blue]Blazor preview:[/] Loaded reference [bold]{Markup.Escape(Path.GetFileName(resolvedRef))}[/]");
							}
							catch (Exception ex)
							{
								AnsiConsole.MarkupLine(
									$"[yellow]Blazor preview:[/] Could not load reference {Markup.Escape(resolvedRef)}: {Markup.Escape(ex.Message)}");
							}
						}
					}
				}

				// usings: list of extra @using directives to inject into each preview
				if (blazorPlugin.Options.TryGetValue("usings", out object? usingsObj)
				    && usingsObj is IEnumerable<object> usingsList)
				{
					blazorExtraUsings.AddRange(
						usingsList.Select(o => o.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)));
				}
			}

			BlazorPreviewService? blazorPreviewService = blazorPlugin is null
				? null
				: new BlazorPreviewService(
					compilationService, loggerFactory, blazorLogger,
					blazorExtraUsings.Count > 0 ? blazorExtraUsings : null,
					blazorRuntimeDlls.Count > 0 ? blazorRuntimeDlls : null);

			using var server = new DevServer(serverLogger, outputDir, port, replService, blazorPreviewService,
				config.Build.BasePath);
			try
			{
				await server.StartAsync();
			}
			catch (HttpListenerException ex)
			{
				AnsiConsole.MarkupLine(
					$"[red]Error:[/] Can't listen on port {port} ({Markup.Escape(ex.Message.TrimEnd('.'))}). Use [bold]--port[/] to pick another.");
				return 1;
			}

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[bold green]Dev server running:[/] [link]http://localhost:{port}/[/]");
			AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");
			AnsiConsole.WriteLine();

			// Auto-open browser
			if (open)
			{
				string url = $"http://localhost:{port}";
				try
				{
					if (OperatingSystem.IsMacOS())
					{
						Process.Start(new ProcessStartInfo("open", url) { UseShellExecute = true });
					}
					else if (OperatingSystem.IsWindows())
					{
						Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { UseShellExecute = true });
					}
					else if (OperatingSystem.IsLinux())
					{
						Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = true });
					}
				}
				catch
				{
					/* silently ignore if browser can't be opened */
				}
			}

			// Start file watcher
			ILogger<FileWatcher> watcherLogger = loggerFactory.CreateLogger<FileWatcher>();
			using var watcher = new FileWatcher(watcherLogger);

			watcher.OnChanged += async () =>
			{
				AnsiConsole.MarkupLine("[yellow]Changes detected, rebuilding...[/]");
				if (await RunBuildAsync(pipeline, config, rootDir, outputDir, versionManager, draft, verbose))
				{
					await server.NotifyReloadAsync();
					AnsiConsole.MarkupLine("[green]Rebuild complete - browser reloaded[/]");
				}
			};

			watcher.Start(docsDir, resolvedConfigPath, [outputDir]);

			// Wait for Ctrl+C
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
				// Normal shutdown
			}

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[dim]Shutting down...[/]");
			return 0;
		});

		return command;
	}

	private static async Task<bool> RunBuildAsync(
		BuildPipeline pipeline, SiteConfig config, string rootDir, string outputDir, VersionManager versionManager,
		bool includeDrafts, bool verbose)
	{
		var sw = Stopwatch.StartNew();

		var context = new BuildContext
		{
			Config = config,
			FileSystem = new FileSystem(),
			RootDirectory = rootDir,
			OutputDirectory = outputDir,
			IncludeDrafts = includeDrafts,
			UseCache = config.Build.Cache
		};

		// Wire version data
		if (versionManager.IsEnabled)
		{
			context.Versions.AddRange(versionManager.Versions);
			context.CurrentVersion = versionManager.DefaultVersion;
		}

		try
		{
			await pipeline.ExecuteAsync(context);
			sw.Stop();
			AnsiConsole.MarkupLine(context.Diagnostics.HasErrors
				? $"[red]Build finished with errors in {sw.Elapsed.TotalSeconds:F2}s[/]"
				: $"[green]Build complete in {sw.Elapsed.TotalSeconds:F2}s[/]");
			BuildCommand.PrintDiagnostics(context.Diagnostics, verbose);

			// Still serve the site: the errors are listed, and the next save may fix them.
			return true;
		}
		catch (Exception ex)
		{
			sw.Stop();
			AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(ex.Message)}");
			return false;
		}
	}

	/// <summary>
	///     The command for a REPL worker: this executable again, running the hidden
	///     <c>repl-worker</c> command. Under <c>dotnet mokadocs.dll</c> the process is the dotnet
	///     host, which needs the assembly path first.
	/// </summary>
	/// <param name="processPath">The running process's executable.</param>
	/// <param name="entryAssemblyPath">The path of <c>mokadocs.dll</c>.</param>
	/// <param name="parentProcessId">This process's id, so the worker exits along with it.</param>
	internal static ProcessStartInfo CreateReplWorkerStartInfo(string processPath, string entryAssemblyPath,
		int parentProcessId)
	{
		var startInfo = new ProcessStartInfo(processPath);
		if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
		{
			startInfo.ArgumentList.Add(entryAssemblyPath);
		}

		startInfo.ArgumentList.Add(ReplWorkerCommand.Name);
		startInfo.ArgumentList.Add("--parent-pid");
		startInfo.ArgumentList.Add(parentProcessId.ToString(CultureInfo.InvariantCulture));
		return startInfo;
	}

	/// <summary>
	///     Loads the NuGet packages listed in the REPL plugin's options and the compiled
	///     assemblies of the documented projects, so REPL blocks can use their types.
	/// </summary>
	private static async Task LoadReplReferencesAsync(ReplExecutionService replService,
		PluginDeclaration replPlugin, SiteConfig config, string rootDir)
	{
		if (replPlugin.Options.TryGetValue("packages", out object? packagesObj)
		    && packagesObj is IEnumerable<object> packageList)
		{
			var specs = packageList
				.Select(o => o.ToString()!)
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.ToList();
			if (specs.Count > 0)
			{
				AnsiConsole.MarkupLine($"[blue]REPL:[/] Loading {specs.Count} NuGet package(s)...");
				NuGetPackageResolver.ResolvedPackages resolved = await replService.LoadPackagesAsync(specs);

				// "Packages loaded" used to print even when the restore failed.
				AnsiConsole.MarkupLine(resolved.Error is null
					? $"[green]REPL:[/] Loaded {resolved.AssemblyPaths.Count} package assemblies"
					: $"[red]REPL:[/] Packages not loaded: {Markup.Escape(resolved.Error)}");
			}
		}

		foreach (ProjectSource project in config.Content.Projects)
		{
			string projectPath = Path.GetFullPath(Path.Combine(rootDir, project.Path));
			if (!File.Exists(projectPath))
			{
				continue;
			}

			string projectName = Path.GetFileNameWithoutExtension(projectPath);
			string? dllPath = FindProjectAssembly(Path.GetDirectoryName(projectPath)!, projectName,
				Environment.Version.Major);
			if (dllPath is not null)
			{
				AnsiConsole.MarkupLine($"[blue]REPL:[/] Loading project assembly [bold]{Markup.Escape(projectName)}[/]");
				replService.LoadProjectAssembly(dllPath);
			}
		}
	}

	/// <summary>
	///     Finds the documented project's compiled assembly so REPL blocks can use its types.
	///     Picks the highest target framework the running runtime can load, Release before
	///     Debug.
	/// </summary>
	/// <remarks>
	///     The list this replaced named net9.0 and net8.0 folders only, so a project
	///     targeting net10.0 never had its assembly loaded.
	/// </remarks>
	/// <param name="projectDir">The directory containing the .csproj.</param>
	/// <param name="projectName">The .csproj file name without extension.</param>
	/// <param name="runtimeMajor">Major version of the running .NET runtime.</param>
	/// <returns>The assembly path, or <c>null</c> when no loadable build exists.</returns>
	internal static string? FindProjectAssembly(string projectDir, string projectName, int runtimeMajor)
	{
		string[] configurations = ["Release", "Debug"];

		return configurations
			.Select((configuration, rank) => (Dir: Path.Combine(projectDir, "bin", configuration), Rank: rank))
			.Where(c => Directory.Exists(c.Dir))
			.SelectMany(c => Directory.GetDirectories(c.Dir).Select(tfmDir => (
				Path: Path.Combine(tfmDir, projectName + ".dll"),
				Version: TargetFrameworks.LoadableVersion(Path.GetFileName(tfmDir), runtimeMajor),
				c.Rank)))
			.Where(candidate => candidate.Version is not null && File.Exists(candidate.Path))
			.OrderByDescending(candidate => candidate.Version)
			.ThenBy(candidate => candidate.Rank)
			.Select(candidate => candidate.Path)
			.FirstOrDefault();
	}
}
