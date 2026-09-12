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

		await using ServiceProvider provider = BuildCommand.BuildServices(config, verbose);

		// Read before running anything, so plugin checks still work if the pipeline throws.
		List<string> registeredPluginIds = provider.GetServices<IMokaPlugin>().Select(p => p.Id).ToList();
		var loadedPluginIds = new List<string>();

		try
		{
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			PluginHost pluginHost = await BuildCommand.InitializePluginsAsync(provider, pipeline);
			loadedPluginIds.AddRange(pluginHost.LoadedPlugins.Select(p => p.Plugin.Id));

			VersionManager versionManager = provider.GetRequiredService<VersionManager>();
			if (versionManager.IsEnabled)
			{
				context.Versions.AddRange(versionManager.Versions);
				context.CurrentVersion = versionManager.DefaultVersion;
			}

			await pipeline.ExecuteAsync(context, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return new DryRunOutcome(context, ex, registeredPluginIds, loadedPluginIds, sw.Elapsed);
		}

		return new DryRunOutcome(context, null, registeredPluginIds, loadedPluginIds, sw.Elapsed);
	}
}

/// <summary>The result of a dry-run build.</summary>
/// <param name="Context">The build context, populated as far as the pipeline got.</param>
/// <param name="Failure">The exception that stopped the pipeline, or <c>null</c> if it completed.</param>
/// <param name="RegisteredPluginIds">Ids of every plugin the CLI can load.</param>
/// <param name="LoadedPluginIds">Ids of the declared plugins that initialized successfully.</param>
/// <param name="Elapsed">Wall-clock time for the run.</param>
internal sealed record DryRunOutcome(
	BuildContext Context,
	Exception? Failure,
	IReadOnlyList<string> RegisteredPluginIds,
	IReadOnlyList<string> LoadedPluginIds,
	TimeSpan Elapsed);
