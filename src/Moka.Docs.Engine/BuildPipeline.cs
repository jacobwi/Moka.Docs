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
	///     Optional hook that runs after content phases (order &lt; 50) and before
	///     rendering phases (order &gt;= 50). Used for plugin execution.
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

		foreach (IBuildPhase phase in orderedPhases)
		{
			ct.ThrowIfCancellationRequested();

			// Run plugin hook after content phases (<=400) and before nav/render phases (>=600)
			if (!pluginHookExecuted && phase.Order >= 500 && PluginHook is not null)
			{
				logger.LogInformation("Executing plugin hook");
				await PluginHook(context, ct);
				pluginHookExecuted = true;
			}

			logger.LogInformation("Executing phase: {PhaseName} (order {Order})", phase.Name, phase.Order);
			await phase.ExecuteAsync(context, ct);
		}

		logger.LogInformation("Build pipeline complete");
	}
}
