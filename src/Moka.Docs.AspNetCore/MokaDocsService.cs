using Microsoft.Extensions.Logging;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Manages the MokaDocs documentation site lifecycle within an ASP.NET Core application.
///     Builds the site at startup or on first access and caches the result in memory.
///     Thread-safe: concurrent requests wait for the build to complete.
/// </summary>
public sealed class MokaDocsService(
	MokaDocsOptions options,
	InMemoryBuildOrchestrator orchestrator,
	ILogger<MokaDocsService> logger)
	: IDisposable
{
	private readonly SemaphoreSlim _buildLock = new(1, 1);
	private int _rebuildQueued;
	private InMemorySite? _site;

	/// <summary>Set by <c>MapMokaDocs</c>. Nothing is built in the background for an app that never serves the docs.</summary>
	internal bool IsMapped { get; set; }

	/// <summary>The cached site, or <c>null</c> before the first build and after <see cref="InvalidateCache" />.</summary>
	internal InMemorySite? CurrentSite => Volatile.Read(ref _site);

	/// <inheritdoc />
	public void Dispose() => _buildLock.Dispose();

	/// <summary>
	///     Gets the built documentation site, building it on first access.
	///     Thread-safe: if multiple requests arrive before the first build completes,
	///     they all wait for the single build to finish.
	/// </summary>
	/// <remarks>
	///     While <see cref="RebuildAsync" /> runs, this returns the previous site without waiting.
	/// </remarks>
	public async Task<InMemorySite> GetSiteAsync(CancellationToken ct = default)
	{
		// Fast path: return cached site if available
		if (options.CacheOutput && CurrentSite is { } cached)
		{
			return cached;
		}

		await _buildLock.WaitAsync(ct);
		try
		{
			// Double-check after acquiring lock
			if (options.CacheOutput && _site is { } built)
			{
				return built;
			}

			logger.LogInformation("Building MokaDocs site (first request or cache invalidated)");
			InMemorySite site = await orchestrator.BuildAsync(options, ct);
			Volatile.Write(ref _site, site);

			logger.LogInformation("MokaDocs site ready: {FileCount} files in memory", site.Files.Count);
			return site;
		}
		finally
		{
			_buildLock.Release();
		}
	}

	/// <summary>
	///     Builds the site again and replaces the cached one when the build is done. Requests keep
	///     getting the previous site until then, unlike <see cref="InvalidateCache" />, which makes
	///     the next request wait for a build.
	/// </summary>
	/// <remarks>
	///     A call made while another rebuild waits for its turn returns at once: that rebuild hasn't
	///     read the files yet, so it picks up this change too. When the build throws, the previous
	///     site stays and the exception propagates.
	/// </remarks>
	/// <param name="ct">Cancels the build.</param>
	public async Task RebuildAsync(CancellationToken ct = default)
	{
		if (Interlocked.Exchange(ref _rebuildQueued, 1) == 1)
		{
			return;
		}

		bool entered = false;
		try
		{
			await _buildLock.WaitAsync(ct);
			entered = true;

			// From here on the build reads the files, so a change after this point needs another rebuild.
			Volatile.Write(ref _rebuildQueued, 0);

			logger.LogInformation("Rebuilding MokaDocs site");
			InMemorySite site = await orchestrator.BuildAsync(options, ct);
			Volatile.Write(ref _site, site);
			logger.LogInformation("MokaDocs site rebuilt: {FileCount} files in memory", site.Files.Count);
		}
		finally
		{
			if (entered)
			{
				_buildLock.Release();
			}
			else
			{
				Volatile.Write(ref _rebuildQueued, 0);
			}
		}
	}

	/// <summary>
	///     Invalidates the cached site, forcing a rebuild on the next request.
	/// </summary>
	public void InvalidateCache()
	{
		Volatile.Write(ref _site, null);
		logger.LogInformation("MokaDocs cache invalidated - site will be rebuilt on next request");
	}
}
