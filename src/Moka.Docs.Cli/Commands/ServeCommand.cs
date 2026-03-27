using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
		var portOption = new Option<int>("--port")
			{ Description = "Port to serve on", DefaultValueFactory = _ => 5080 };
		var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose logging" };
		var configOption = new Option<string?>("--config")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };
		var outputOption = new Option<string?>("--output") { Description = "Output directory (default: _site/)" };
		var openOption = new Option<bool>("--open")
			{ Description = "Open browser automatically", DefaultValueFactory = _ => true };
		var noOpenOption = new Option<bool>("--no-open") { Description = "Don't open browser automatically" };

		var command = new Command("serve", "Build and serve the site locally with hot reload")
		{
			portOption,
			verboseOption,
			configOption,
			outputOption,
			openOption,
			noOpenOption
		};

		command.SetAction(async (parseResult, _) =>
		{
			int port = parseResult.GetValue(portOption);
			bool verbose = parseResult.GetValue(verboseOption);
			string? configPath = parseResult.GetValue(configOption);
			string? output = parseResult.GetValue(outputOption);
			bool open = parseResult.GetValue(openOption) && !parseResult.GetValue(noOpenOption);

			string version = Assembly.GetExecutingAssembly()
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
				?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
			// Strip build metadata (e.g. "+sha.abc123") if present
			int plusIdx = version.IndexOf('+');
			if (plusIdx >= 0) version = version[..plusIdx];
			AnsiConsole.MarkupLine($"[bold blue]MokaDocs[/] [dim]v{version}[/] — Dev server starting...");
			AnsiConsole.WriteLine();

			string rootDir = Directory.GetCurrentDirectory();
			string resolvedConfigPath = configPath ?? Path.Combine(rootDir, "mokadocs.yaml");

			// Load config
			SiteConfig config;
			try
			{
				var fs = new FileSystem();
				var reader = new SiteConfigReader(fs);
				config = reader.Read(resolvedConfigPath);
				AnsiConsole.MarkupLine(
					$"[green]Config:[/] {Path.GetFileName(resolvedConfigPath)} — \"{Markup.Escape(config.Site.Title)}\"");
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

			string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));
			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));

			// Set up DI (reuse BuildCommand's service setup for plugins)
			await using ServiceProvider provider = BuildCommand.BuildServices(config, verbose);
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			PluginHost pluginHost = provider.GetRequiredService<PluginHost>();
			ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();

			// Initialize plugins
			await pluginHost.DiscoverAndInitializeAsync();
			pipeline.PluginHook = async (ctx, ct) =>
			{
				if (pluginHost.LoadedPlugins.Count > 0)
				{
					await pluginHost.ExecuteAllAsync(ctx, ct);
				}
			};

			// Resolve version data
			VersionManager versionManager = provider.GetRequiredService<VersionManager>();

			// Run initial build
			if (!await RunBuildAsync(pipeline, config, rootDir, outputDir, versionManager))
			{
				return 1;
			}

			// Start dev server with REPL support
			ILogger<DevServer> serverLogger = loggerFactory.CreateLogger<DevServer>();
			ILogger<ReplExecutionService> replLogger = loggerFactory.CreateLogger<ReplExecutionService>();
			var replService = new ReplExecutionService(replLogger);

			// Load NuGet packages configured in the REPL plugin options
			PluginDeclaration? replPlugin = config.Plugins.FirstOrDefault(p =>
				string.Equals(p.Name, "mokadocs-repl", StringComparison.OrdinalIgnoreCase));

			if (replPlugin?.Options.TryGetValue("packages", out object? packagesObj) == true
			    && packagesObj is IEnumerable<object> packageList)
			{
				var specs = packageList
					.Select(o => o.ToString()!)
					.Where(s => !string.IsNullOrWhiteSpace(s))
					.ToList();
				if (specs.Count > 0)
				{
					AnsiConsole.MarkupLine($"[blue]REPL:[/] Loading {specs.Count} NuGet package(s)...");
					await replService.LoadPackagesAsync(specs);
					AnsiConsole.MarkupLine("[green]REPL:[/] Packages loaded");
				}
			}

			// Auto-load documented project assemblies so REPL blocks can use them
			foreach (ProjectSource project in config.Content.Projects)
			{
				string projectPath = Path.GetFullPath(Path.Combine(rootDir, project.Path));
				if (File.Exists(projectPath))
				{
					string projectDir = Path.GetDirectoryName(projectPath)!;
					string projectName = Path.GetFileNameWithoutExtension(projectPath);
					// Look for the built DLL in standard output locations
					string[] candidatePaths = new[]
					{
						Path.Combine(projectDir, "bin", "Release", "net9.0", $"{projectName}.dll"),
						Path.Combine(projectDir, "bin", "Debug", "net9.0", $"{projectName}.dll"),
						Path.Combine(projectDir, "bin", "Release", "net8.0", $"{projectName}.dll"),
						Path.Combine(projectDir, "bin", "Debug", "net8.0", $"{projectName}.dll")
					};

					string? dllPath = candidatePaths.FirstOrDefault(File.Exists);
					if (dllPath is not null)
					{
						AnsiConsole.MarkupLine(
							$"[blue]REPL:[/] Loading project assembly [bold]{Markup.Escape(projectName)}[/]");
						replService.LoadProjectAssembly(dllPath);
					}
				}
			}

			ILogger<BlazorPreviewService> blazorLogger = loggerFactory.CreateLogger<BlazorPreviewService>();
			var blazorPreviewService = new BlazorPreviewService(blazorLogger);

			using var server = new DevServer(serverLogger, outputDir, port, replService, blazorPreviewService);
			await server.StartAsync();

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
				if (await RunBuildAsync(pipeline, config, rootDir, outputDir, versionManager))
				{
					await server.NotifyReloadAsync();
					AnsiConsole.MarkupLine("[green]Rebuild complete — browser reloaded[/]");
				}
			};

			watcher.Start(docsDir, resolvedConfigPath);

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
		BuildPipeline pipeline, SiteConfig config, string rootDir, string outputDir, VersionManager versionManager)
	{
		var sw = Stopwatch.StartNew();

		var context = new BuildContext
		{
			Config = config,
			FileSystem = new FileSystem(),
			RootDirectory = rootDir,
			OutputDirectory = outputDir
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
			AnsiConsole.MarkupLine($"[green]Build complete in {sw.Elapsed.TotalSeconds:F2}s[/]");
			return true;
		}
		catch (Exception ex)
		{
			sw.Stop();
			AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(ex.Message)}");
			return false;
		}
	}
}
