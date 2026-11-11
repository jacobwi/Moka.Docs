using System.Diagnostics;
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
	private readonly CancellationTokenSource _disposing = new();
	private BuildFailure? _failure;
	private InMemorySite? _lastGoodSite;
	private int _rebuildQueued;
	private InMemorySite? _site;

	/// <summary>Set by <c>MapMokaDocs</c>. Nothing is built in the background for an app that never serves the docs.</summary>
	internal bool IsMapped { get; set; }

	/// <summary>The cached site, or <c>null</c> before the first build and after <see cref="InvalidateCache" />.</summary>
	internal InMemorySite? CurrentSite => Volatile.Read(ref _site);

	/// <summary>
	///     How long requests reuse a failed build's outcome before one tries again. A failed build
	///     used to run again for every request, CSS and scripts included, one at a time under the lock.
	/// </summary>
	internal TimeSpan FailureRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc />
	public void Dispose()
	{
		// Cancelled, not disposed: a build that ignores the token still reads it and releases the lock.
		_disposing.Cancel();
	}

	/// <summary>
	///     Gets the built documentation site, building it on first access.
	///     Thread-safe: if multiple requests arrive before the first build completes,
	///     they all wait for the single build to finish.
	/// </summary>
	/// <remarks>
	///     <para>
	///         While <see cref="RebuildAsync" /> runs, this returns the previous site without waiting.
	///     </para>
	///     <para>
	///         When a build fails, this returns the last site that built, if there is one. For
	///         <see cref="FailureRetryDelay" /> after a failure, calls don't build again; they get that
	///         site or the same failure.
	///     </para>
	/// </remarks>
	/// <param name="ct">
	///     Stops this call waiting. The build itself carries on for the other callers and is
	///     stopped only when the service is disposed.
	/// </param>
	/// <exception cref="InvalidOperationException">The build failed and no earlier build succeeded.</exception>
	public async Task<InMemorySite> GetSiteAsync(CancellationToken ct = default)
	{
		SiteResult result = await GetSiteOrFailureAsync(ct);
		if (result.Site is { } site)
		{
			return site;
		}

		Exception error = result.Failure?.Error ?? new UnreachableException();
		throw new InvalidOperationException($"The MokaDocs site failed to build: {error.Message}", error);
	}

	/// <summary>
	///     The site, or the failure that left no site to serve. The endpoint answers a failure with
	///     <c>503</c> instead of an exception per request.
	/// </summary>
	internal async Task<SiteResult> GetSiteOrFailureAsync(CancellationToken ct)
	{
		if (options.CacheOutput && CurrentSite is { } cached)
		{
			return new SiteResult(cached, null);
		}

		// The build runs on the service's token. With the request's, the first reader to leave
		// cancelled the build everyone else was waiting for.
		return await BuildForRequestsAsync().WaitAsync(ct);
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

		using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposing.Token);
		bool entered = false;
		try
		{
			await _buildLock.WaitAsync(cancel.Token);
			entered = true;

			// From here on the build reads the files, so a change after this point needs another rebuild.
			Volatile.Write(ref _rebuildQueued, 0);

			logger.LogInformation("Rebuilding MokaDocs site");
			InMemorySite site;
			try
			{
				site = await orchestrator.BuildAsync(options, cancel.Token);
			}
			catch (Exception ex) when (!cancel.IsCancellationRequested)
			{
				// Requests that find no site reuse this failure instead of rebuilding the same files.
				Volatile.Write(ref _failure, new BuildFailure(ex, Environment.TickCount64));
				throw;
			}

			Succeeded(site);
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
	/// <remarks>
	///     When that rebuild fails, requests get the site that was cached before.
	/// </remarks>
	public void InvalidateCache()
	{
		Volatile.Write(ref _site, null);
		Volatile.Write(ref _failure, null);
		logger.LogInformation("MokaDocs cache invalidated - site will be rebuilt on next request");
	}

	private async Task<SiteResult> BuildForRequestsAsync()
	{
		CancellationToken stopping = _disposing.Token;
		await _buildLock.WaitAsync(stopping);
		try
		{
			// Double-check after acquiring lock
			if (options.CacheOutput && _site is { } built)
			{
				return new SiteResult(built, null);
			}

			if (Volatile.Read(ref _failure) is { } failure
			    && TimeSpan.FromMilliseconds(Environment.TickCount64 - failure.At) < FailureRetryDelay)
			{
				return new SiteResult(_lastGoodSite, _lastGoodSite is null ? failure : null);
			}

			logger.LogInformation("Building MokaDocs site (first request or cache invalidated)");
			try
			{
				InMemorySite site = await orchestrator.BuildAsync(options, stopping);
				Succeeded(site);
				logger.LogInformation("MokaDocs site ready: {FileCount} files in memory", site.Files.Count);
				return new SiteResult(site, null);
			}
			catch (Exception ex) when (!stopping.IsCancellationRequested)
			{
				var failed = new BuildFailure(ex, Environment.TickCount64);
				Volatile.Write(ref _failure, failed);
				logger.LogError(ex,
					"The MokaDocs build failed; {Outcome} for the next {Seconds}s, then a request builds again",
					_lastGoodSite is null ? "requests get 503" : "requests get the last site that built",
					FailureRetryDelay.TotalSeconds);
				return new SiteResult(_lastGoodSite, _lastGoodSite is null ? failed : null);
			}
		}
		finally
		{
			_buildLock.Release();
		}
	}

	/// <summary>Stores a site that built. The caller holds the build lock.</summary>
	private void Succeeded(InMemorySite site)
	{
		_lastGoodSite = site;
		Volatile.Write(ref _site, site);
		Volatile.Write(ref _failure, null);
	}

	/// <summary>A build that threw, and when, in <see cref="Environment.TickCount64" /> milliseconds.</summary>
	internal sealed record BuildFailure(Exception Error, long At);

	/// <summary>A site to serve, or the failure that left none.</summary>
	internal readonly record struct SiteResult(InMemorySite? Site, BuildFailure? Failure);
}
