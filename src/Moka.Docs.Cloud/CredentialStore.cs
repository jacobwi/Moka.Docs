using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Cloud;

/// <summary>
///     Stub credential store that reads the cloud API key from configuration.
///     In a future implementation this could integrate with OS keychains or
///     environment variables. When cloud is disabled, all methods return <c>null</c>.
/// </summary>
public sealed class CredentialStore
{
    private readonly CloudConfig _config;
    private readonly ILogger<CredentialStore> _logger;

    /// <summary>
    ///     Creates a new credential store.
    /// </summary>
    /// <param name="siteConfig">The site configuration containing the cloud API key.</param>
    /// <param name="logger">Logger instance.</param>
    public CredentialStore(SiteConfig siteConfig, ILogger<CredentialStore> logger)
    {
        _config = siteConfig.Cloud;
        _logger = logger;
    }

    /// <summary>
    ///     Returns whether a valid API key is available for cloud features.
    /// </summary>
    public bool HasApiKey => GetApiKey() is not null;

    /// <summary>
    ///     Retrieves the configured API key for cloud features.
    ///     Returns <c>null</c> when cloud is disabled or no key is configured.
    ///     Also checks the <c>MOKADOCS_API_KEY</c> environment variable as a fallback.
    /// </summary>
    /// <returns>The API key, or <c>null</c> if not available.</returns>
    public string? GetApiKey()
    {
        if (!_config.Enabled) return null;

        // Config value takes precedence
        if (!string.IsNullOrWhiteSpace(_config.ApiKey)) return _config.ApiKey;

        // Fall back to environment variable
        var envKey = Environment.GetEnvironmentVariable("MOKADOCS_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _logger.LogDebug("Using API key from MOKADOCS_API_KEY environment variable");
            return envKey;
        }

        _logger.LogDebug("No cloud API key configured");
        return null;
    }
}