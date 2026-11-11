using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Serve;
using Moka.Docs.Versioning;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

internal static class VersionedBuild
{
	internal static bool Applies(SiteConfig config, string? onlyVersion) =>
		onlyVersion is not null || VersionedBuildOrchestrator.IsVersionedBuild(config);

	internal static string? ServeNotice(SiteConfig config) => VersionedBuildOrchestrator.IsVersionedBuild(config)
		? "Versioning: serve builds the working tree only; mokadocs build builds every version."
		: null;

	internal static async Task<int> RunAsync(VersionedBuildRequest request, bool verbose, bool warningsAsErrors,
		bool watch, CancellationToken ct = default)
	{
		using ILoggerFactory loggerFactory = LoggerFactory.Create(b =>
		{
			b.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning);
			if (verbose)
			{
				b.AddSimpleConsole(o =>
				{
					o.SingleLine = true;
					o.TimestampFormat = null;
				});
			}
		});

		var orchestrator = new VersionedBuildOrchestrator(loggerFactory.CreateLogger<VersionedBuildOrchestrator>());
		int exitCode = await BuildOnceAsync(orchestrator, request, verbose, warningsAsErrors, ct);
		if (!watch)
		{
			return exitCode;
		}

		string rootDir = Path.GetDirectoryName(Path.GetFullPath(request.ConfigPath))!;
		string docsDir = Path.GetFullPath(Path.Combine(rootDir, request.Config.Content.Docs));
		VersionedBuildRequest rebuild = request.Only.Count > 0 ? request : request with { WorkingTreeOnly = true };

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		Console.CancelKeyPress += (_, e) =>
		{
			e.Cancel = true;
			cts.Cancel();
		};

		await BuildCommand.WatchAsync(docsDir, request.ConfigPath,
			BuildCommand.WatchIgnoredDirectories(rootDir, request.OutputDirectory),
			loggerFactory.CreateLogger<FileWatcher>(),
			() => BuildOnceAsync(orchestrator, rebuild, verbose, warningsAsErrors, cts.Token), cts.Token);

		AnsiConsole.MarkupLine("[dim]Stopped watching[/]");
		return exitCode;
	}

	internal static VersionPipelineRunner PipelineRunner(bool verbose) => async (context, routesPhase, ct) =>
	{
		await using ServiceProvider provider = BuildCommand.BuildServices(context.Config, verbose, context.RootDirectory);
		var pipeline = new BuildPipeline(provider.GetServices<IBuildPhase>().Append(routesPhase),
			provider.GetRequiredService<ILogger<BuildPipeline>>());
		await BuildCommand.InitializePluginsAsync(provider, pipeline, ct);
		await pipeline.ExecuteAsync(context, ct);
	};

	private static async Task<int> BuildOnceAsync(VersionedBuildOrchestrator orchestrator, VersionedBuildRequest request,
		bool verbose, bool warningsAsErrors, CancellationToken ct)
	{
		var sw = Stopwatch.StartNew();
		string rootDir = Path.GetDirectoryName(Path.GetFullPath(request.ConfigPath))!;

		// Each version's own build only guards its own checkout, so the project's folders are checked
		// here, like OutputPhase does for a plain build.
		if (OutputDirectoryGuard.FindConflict(request.Config, rootDir, request.OutputDirectory) is { } conflict)
		{
			AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(conflict)}");
			return 1;
		}

		VersionedBuildResult result;
		try
		{
			result = await orchestrator.RunAsync(request, PipelineRunner(verbose), ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(ex.Message)}");
			if (verbose)
			{
				AnsiConsole.WriteException(ex);
			}

			return 1;
		}

		sw.Stop();
		bool failed = BuildCommand.Fails(result.Diagnostics, warningsAsErrors);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(failed
			? $"[red bold]❌ MokaDocs build finished with errors in {sw.Elapsed.TotalSeconds:F2}s[/]"
			: $"[green bold]✅ MokaDocs build complete in {sw.Elapsed.TotalSeconds:F2}s[/]");

		foreach (VersionBuildOutcome build in result.Builds)
		{
			int mdPages = build.Context.Pages.Count(p => p.Origin == PageOrigin.Markdown);
			int apiPages = build.Context.Pages.Count(p => p.Origin == PageOrigin.ApiGenerated);
			string where = build.IsSiteRoot ? "site root" : build.Version.Slug + "/";
			string mark = build.Succeeded ? "📄" : "❌";
			AnsiConsole.MarkupLine(
				$"{mark} {Markup.Escape(build.Version.Label)} → {Markup.Escape(where)}: {mdPages + apiPages} pages ({mdPages} markdown, {apiPages} generated) from {Markup.Escape(build.Source)}");
		}

		AnsiConsole.MarkupLine($"📦 Output:       {Markup.Escape(request.Config.Build.Output)}");
		BuildCommand.PrintDiagnostics(result.Diagnostics, verbose, warningsAsErrors);
		return failed ? 1 : 0;
	}
}
