using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Plugins;
using Moka.Docs.Versioning;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Runs the real build pipeline with <see cref="BuildContext.DryRun" /> set, so every
///     analysis, plugin and render phase executes but the output directory is left alone.
///     Shared by <c>mokadocs validate</c> and <c>mokadocs doctor</c>.
/// </summary>
internal static class DryRunBuild
{
	/// <summary>
	///     Runs the pipeline once.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	/// <param name="rootDirectory">Directory containing mokadocs.yaml.</param>
	/// <param name="includeDrafts">Whether draft pages take part.</param>
	/// <param name="verbose">Whether pipeline logging is printed.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	///     The populated context, or the exception that stopped the pipeline. Never throws
	///     for pipeline failures, because callers want to report them, not crash on them.
	/// </returns>
	internal static async Task<DryRunOutcome> RunAsync(
		SiteConfig config, string rootDirectory, bool includeDrafts, bool verbose, CancellationToken ct = default)
	{
		var sw = Stopwatch.StartNew();

		var context = new BuildContext
		{
			Config = config,
			FileSystem = new FileSystem(),
			RootDirectory = rootDirectory,
			OutputDirectory = Path.GetFullPath(Path.Combine(rootDirectory, config.Build.Output)),
			IncludeDrafts = includeDrafts,
			UseCache = config.Build.Cache,
			DryRun = true
		};

		await using ServiceProvider provider = BuildCommand.BuildServices(config, verbose, rootDirectory);

		// Read before running anything, so plugin checks still work if the pipeline throws.
		List<string> registeredPluginIds = provider.GetServices<IMokaPlugin>().Select(p => p.Id).ToList();
		var loadedPluginIds = new List<string>();
		var pluginLoadFailures = new List<PluginLoadFailure>();

		DryRunOutcome Outcome(Exception? failure) =>
			new(context, failure, registeredPluginIds, loadedPluginIds, sw.Elapsed) { PluginLoadFailures = pluginLoadFailures };

		try
		{
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			PluginHost pluginHost = await BuildCommand.InitializePluginsAsync(provider, pipeline, ct);
			loadedPluginIds.AddRange(pluginHost.LoadedPlugins.Select(p => p.Plugin.Id));
			pluginLoadFailures.AddRange(pluginHost.LoadFailures);

			VersionManager versionManager = provider.GetRequiredService<VersionManager>();
			versionManager.ApplyTo(context);

			// The other versions are not built; their refs only have to exist.
			VersionedBuildOrchestrator.CheckRefs(config, rootDirectory, context.Diagnostics);

			await pipeline.ExecuteAsync(context, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Outcome(ex);
		}

		return Outcome(null);
	}
}

/// <summary>The result of a dry-run build.</summary>
/// <param name="Context">The build context, populated as far as the pipeline got.</param>
/// <param name="Failure">The exception that stopped the pipeline, or <c>null</c> if it completed.</param>
/// <param name="RegisteredPluginIds">Ids of the built-in plugins the CLI registers.</param>
/// <param name="LoadedPluginIds">Ids of the declared plugins that initialized successfully, from a path too.</param>
/// <param name="Elapsed">Wall-clock time for the run.</param>
internal sealed record DryRunOutcome(
	BuildContext Context,
	Exception? Failure,
	IReadOnlyList<string> RegisteredPluginIds,
	IReadOnlyList<string> LoadedPluginIds,
	TimeSpan Elapsed)
{
	/// <summary>The declarations with a <c>path</c> whose plugin didn't load. The build reports each as an error.</summary>
	public IReadOnlyList<PluginLoadFailure> PluginLoadFailures { get; init; } = [];
}
