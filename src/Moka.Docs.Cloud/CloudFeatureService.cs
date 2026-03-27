using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Cloud;

/// <summary>
///     Central gate for cloud features. All cloud functionality checks this service
///     before performing any work. When cloud is disabled (the default), every
///     method is a fast no-op that never calls home.
/// </summary>
public sealed class CloudFeatureService
{
	private readonly CloudConfig _config;
	private readonly ILogger<CloudFeatureService> _logger;

	/// <summary>
	///     Creates a new cloud feature service.
	/// </summary>
	/// <param name="siteConfig">The site configuration containing cloud settings.</param>
	/// <param name="logger">Logger instance.</param>
	public CloudFeatureService(SiteConfig siteConfig, ILogger<CloudFeatureService> logger)
	{
		_config = siteConfig.Cloud;
		_logger = logger;

		if (!IsEnabled)
		{
			_logger.LogDebug("Cloud features are disabled");
		}
	}

	/// <summary>
	///     Whether the cloud master switch is on. All other cloud features
	///     require this to be <c>true</c>.
	/// </summary>
	public bool IsEnabled => _config.Enabled;

	/// <summary>
	///     Whether AI-generated summaries are enabled.
	/// </summary>
	public bool AiSummariesEnabled => IsEnabled && _config.Features.AiSummaries;

	/// <summary>
	///     Whether server-side PDF export is enabled.
	/// </summary>
	public bool PdfExportEnabled => IsEnabled && _config.Features.PdfExport;

	/// <summary>
	///     Whether usage analytics are enabled.
	/// </summary>
	public bool AnalyticsEnabled => IsEnabled && _config.Features.Analytics;

	/// <summary>
	///     Whether custom domain with SSL is enabled.
	/// </summary>
	public bool CustomDomainEnabled => IsEnabled && _config.Features.CustomDomain;

	/// <summary>
	///     Checks whether a specific cloud feature is enabled by name.
	///     Returns <c>false</c> for unknown feature names or when cloud is globally disabled.
	/// </summary>
	/// <param name="featureName">The feature name (e.g., "aiSummaries", "pdfExport").</param>
	/// <returns><c>true</c> if the feature is enabled; otherwise <c>false</c>.</returns>
	public bool IsFeatureEnabled(string featureName)
	{
		if (!IsEnabled)
		{
			return false;
		}

		return featureName.ToLowerInvariant() switch
		{
			"aisummaries" => _config.Features.AiSummaries,
			"pdfexport" => _config.Features.PdfExport,
			"analytics" => _config.Features.Analytics,
			"customdomain" => _config.Features.CustomDomain,
			_ => false
		};
	}
}
