using Microsoft.Extensions.Logging;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Manages the MokaDocs documentation site lifecycle within an ASP.NET Core application.
///     Builds the site on first request and caches the result in memory.
///     Thread-safe: concurrent requests wait for the build to complete.
/// </summary>
public sealed class MokaDocsService(
    MokaDocsOptions options,
    InMemoryBuildOrchestrator orchestrator,
    ILogger<MokaDocsService> logger)
    : IDisposable
{
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private bool _isBuilt;

    private InMemorySite? _site;

    /// <inheritdoc />
    public void Dispose()
    {
        _buildLock.Dispose();
    }

    /// <summary>
    ///     Gets the built documentation site, building it on first access.
    ///     Thread-safe: if multiple requests arrive before the first build completes,
    ///     they all wait for the single build to finish.
    /// </summary>
    public async Task<InMemorySite> GetSiteAsync(CancellationToken ct = default)
    {
        // Fast path: return cached site if available
        if (_isBuilt && options.CacheOutput && _site is not null)
            return _site;

        await _buildLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_isBuilt && options.CacheOutput && _site is not null)
                return _site;

            logger.LogInformation("Building MokaDocs site (first request or cache invalidated)");
            _site = await orchestrator.BuildAsync(options, ct);
            _isBuilt = true;

            logger.LogInformation("MokaDocs site ready: {FileCount} files in memory", _site.Files.Count);
            return _site;
        }
        finally
        {
            _buildLock.Release();
        }
    }

    /// <summary>
    ///     Invalidates the cached site, forcing a rebuild on the next request.
    /// </summary>
    public void InvalidateCache()
    {
        _isBuilt = false;
        _site = null;
        logger.LogInformation("MokaDocs cache invalidated — site will be rebuilt on next request");
    }
}