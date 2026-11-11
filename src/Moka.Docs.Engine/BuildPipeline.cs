using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine;

/// <summary>
///     Orchestrates the execution of all build phases in order.
/// </summary>
public sealed class BuildPipeline(
	IEnumerable<IBuildPhase> phases,
	ILogger<BuildPipeline> logger)
{
	/// <summary>
	///     Optional hook that runs once, just before the first phase with order 500 or above
	///     (FeatureGatePhase). Used for plugin execution.
	/// </summary>
	public Func<BuildContext, CancellationToken, Task>? PluginHook { get; set; }

	/// <summary>
	///     Execute all registered build phases in order, with plugin hook in between.
	/// </summary>
	public async Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		var orderedPhases = phases.OrderBy(p => p.Order).ToList();
		logger.LogInformation("Starting build pipeline with {PhaseCount} phases", orderedPhases.Count);

		bool pluginHookExecuted = false;
		var timings = new List<PhaseTiming>(orderedPhases.Count + 1);
		var total = Stopwatch.StartNew();

		foreach (IBuildPhase phase in orderedPhases)
		{
			ct.ThrowIfCancellationRequested();

			// Run the plugin hook once, immediately before the first phase at order 500 or
			// above. That is FeatureGatePhase, so plugin-injected pages are still feature
			// gated, navigable, searchable and rendered.
			if (!pluginHookExecuted && phase.Order >= 500 && PluginHook is not null)
			{
				logger.LogInformation("Executing plugin hook");
				long hookStart = Stopwatch.GetTimestamp();
				await PluginHook(context, ct);
				timings.Add(new PhaseTiming("Plugins", Stopwatch.GetElapsedTime(hookStart)));
				pluginHookExecuted = true;
			}

			logger.LogInformation("Executing phase: {PhaseName} (order {Order})", phase.Name, phase.Order);
			long start = Stopwatch.GetTimestamp();
			await phase.ExecuteAsync(context, ct);
			TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
			timings.Add(new PhaseTiming(phase.Name, elapsed));
			logger.LogInformation("Phase {PhaseName} took {Milliseconds:F0} ms", phase.Name, elapsed.TotalMilliseconds);
		}

		total.Stop();
		logger.LogInformation("Build pipeline complete in {Milliseconds:F0} ms ({Timings})",
			total.Elapsed.TotalMilliseconds,
			string.Join(", ", timings.Select(t => $"{t.Name} {t.Elapsed.TotalMilliseconds:F0} ms")));
		LastTimings = timings;
	}

	/// <summary>
	///     How long each phase (and the plugin hook, as <c>Plugins</c>) took in the most recent
	///     completed run, in execution order. Empty before the first run.
	/// </summary>
	public IReadOnlyList<PhaseTiming> LastTimings { get; private set; } = [];
}

/// <summary>The wall-clock time one build phase took.</summary>
/// <param name="Name">The phase name, or <c>Plugins</c> for the plugin hook.</param>
/// <param name="Elapsed">How long it ran.</param>
public readonly record struct PhaseTiming(string Name, TimeSpan Elapsed);
