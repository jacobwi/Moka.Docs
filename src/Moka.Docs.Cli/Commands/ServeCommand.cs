using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
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
			var source = new ConfigSource(resolvedConfigPath, output, basePath);
			string rootDir = source.RootDirectory;

			// Load config, with the CLI overrides applied
			SiteConfig config;
			string configText;
			try
			{
				(config, configText) = BuildSession.Read(source);
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

			string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));
			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));

			// The services and plugins come from mokadocs.yaml and are rebuilt when it changes; the
			// server, the watcher and the REPL live as long as the command and log through their own factory.
			await using BuildSession session = await BuildSession.StartAsync(source, config, configText, verbose, ct);
			using ILoggerFactory loggerFactory = BuildCommand.CreateLoggerFactory(verbose);

			if (VersionedBuild.ServeNotice(config) is { } versionNotice)
			{
				AnsiConsole.MarkupLine($"[dim]{Markup.Escape(versionNotice)}[/]");
			}

			// Run initial build
			if (!await RunBuildAsync(session, outputDir, draft, verbose))
			{
				return 1;
			}

			ILogger<DevServer> serverLogger = loggerFactory.CreateLogger<DevServer>();

			// The REPL endpoint runs code sent to it, so it only exists when the site declares the
			// plugin that uses it. It used to be started for every site.
			PluginDeclaration? replPlugin = FindReplDeclaration(config);
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

			using var server = new DevServer(serverLogger, outputDir, port, replService, config.Build.BasePath);
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
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			ILogger<FileWatcher> watcherLogger = loggerFactory.CreateLogger<FileWatcher>();
			using var watcher = new FileWatcher(watcherLogger);

			watcher.OnChanged += async () =>
			{
				AnsiConsole.MarkupLine("[yellow]Changes detected, rebuilding...[/]");
				ConfigReload reload = await session.ReloadIfChangedAsync(true, cts.Token);
				BuildSession.Print(reload, "Still serving the last good site");
				if (reload.Outcome == ConfigReloadOutcome.Failed)
				{
					return;
				}

				if (reload.Outcome == ConfigReloadOutcome.Reloaded
				    && !SameReplSettings(replPlugin, FindReplDeclaration(session.Config)))
				{
					AnsiConsole.MarkupLine(
						"[yellow]The mokadocs-repl entry changed, which takes a restart: the REPL endpoint keeps the settings serve started with.[/]");
				}

				if (await RunBuildAsync(session, outputDir, draft, verbose))
				{
					await server.NotifyReloadAsync();
					AnsiConsole.MarkupLine("[green]Rebuild complete - browser reloaded[/]");
				}
			};

			watcher.Start(docsDir, resolvedConfigPath, BuildCommand.WatchIgnoredDirectories(rootDir, outputDir));

			// Wait for Ctrl+C
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

	private static async Task<bool> RunBuildAsync(BuildSession session, string outputDir, bool includeDrafts,
		bool verbose)
	{
		var sw = Stopwatch.StartNew();
		SiteConfig config = session.Config;

		var context = new BuildContext
		{
			Config = config,
			FileSystem = new FileSystem(),
			RootDirectory = session.RootDirectory,
			OutputDirectory = outputDir,
			IncludeDrafts = includeDrafts,
			UseCache = config.Build.Cache
		};

		session.VersionManager.ApplyTo(context);

		try
		{
			await session.Pipeline.ExecuteAsync(context);
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

	private static PluginDeclaration? FindReplDeclaration(SiteConfig config) =>
		config.Plugins.FirstOrDefault(p => string.Equals(p.Name, "mokadocs-repl", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	///     Whether two <c>mokadocs-repl</c> entries set the REPL up the same way: both missing, or
	///     both present with the same options. The endpoint and its packages are set up once, when
	///     serve starts.
	/// </summary>
	internal static bool SameReplSettings(PluginDeclaration? started, PluginDeclaration? current) =>
		(started, current) switch
		{
			(null, null) => true,
			({ } a, { } b) => Describe(a.Options) == Describe(b.Options),
			_ => false
		};

	private static string Describe(object? value) => value switch
	{
		null => "~",
		string text => "\"" + text + "\"",
		IDictionary<string, object> map => "{" + string.Join(",",
			map.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => e.Key + ":" + Describe(e.Value))) + "}",
		IDictionary<object, object> map => "{" + string.Join(",",
			map.OrderBy(e => e.Key.ToString(), StringComparer.Ordinal).Select(e => e.Key + ":" + Describe(e.Value))) + "}",
		IEnumerable<object> items => "[" + string.Join(",", items.Select(Describe)) + "]",
		_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
	};

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
