using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Filters pages from the build based on feature flags.
///     Pages with a <c>requires</c> front matter field are excluded when
///     the corresponding feature flag is disabled in the feature manager.
/// </summary>
public sealed class FeatureGatePhase(
    IFeatureManager featureManager,
    ILogger<FeatureGatePhase> logger) : IBuildPhase
{
    /// <inheritdoc />
    public string Name => "FeatureGate";

    /// <inheritdoc />
    public int Order => 500;

    /// <inheritdoc />
    public async Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
    {
        var removed = 0;

        for (var i = context.Pages.Count - 1; i >= 0; i--)
        {
            ct.ThrowIfCancellationRequested();

            var requiredFeature = context.Pages[i].FrontMatter.Requires;
            if (string.IsNullOrWhiteSpace(requiredFeature))
                continue;

            var isEnabled = await featureManager.IsEnabledAsync(requiredFeature);
            if (!isEnabled)
            {
                logger.LogDebug("Excluding page {Route} — feature flag '{Feature}' is disabled",
                    context.Pages[i].Route, requiredFeature);
                context.Pages.RemoveAt(i);
                removed++;
            }
        }

        if (removed > 0)
            logger.LogInformation("Feature gating excluded {Count} page(s)", removed);
    }
}