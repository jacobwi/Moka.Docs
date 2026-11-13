using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Manages the MokaDocs documentation site lifecycle within an ASP.NET Core application.
///     Builds the site at startup or on first access and caches the result in memory.
///     Thread-safe: one build runs at a time, and callers of <see cref="GetSiteAsync" /> that
///     arrive during it share it. Docs requests never wait for a build.
/// </summary>
public sealed class MokaDocsService(
	MokaDocsOptions options,
	InMemoryBuildOrchestrator orchestrator,
	ILogger<MokaDocsService> logger)
	: IDisposable
{
	/// <summary>The <c>Retry-After</c>, in seconds, of a request that arrives before the first build is done.</summary>
	internal const int BuildingRetryAfterSeconds = 2;

	/// <summary>Path bases logged at most. Past this, a mismatch isn't logged again, whatever the path base.</summary>
	private const int _maxReportedPathBases = 16;

	private readonly SemaphoreSlim _buildLock = new(1, 1);
	private readonly CancellationTokenSource _disposing = new();
	private readonly Lock _pendingLock = new();
	private readonly ConcurrentDictionary<string, byte> _reportedPathBases = new(StringComparer.OrdinalIgnoreCase);
	private BuildFailure? _failure;
	private InMemorySite? _lastGoodSite;
	private Task<SiteResult>? _pendingBuild;
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
	///     Thread-safe: callers that arrive before the build completes all wait for that one build.
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
	///     The site, or the failure that left no site to serve, after waiting for a build when there
	///     is no site yet. With <see cref="MokaDocsOptions.CacheOutput" /> off, every call builds.
	/// </summary>
	internal async Task<SiteResult> GetSiteOrFailureAsync(CancellationToken ct)
	{
		if (options.CacheOutput && CurrentSite is { } cached)
		{
			return new SiteResult(cached, null);
		}

		// The build runs on the service's token. With the request's, the first reader to leave
		// cancelled the build everyone else was waiting for.
		Task<SiteResult> build = options.CacheOutput ? StartBuild() : BuildForRequestsAsync();
		return await build.WaitAsync(ct);
	}

	/// <summary>
	///     The site for a docs request, without waiting: the current site; the last site that built
	///     while a build runs or after a failure; the failure when nothing has built; or neither while
	///     the first build runs. Starts a build when there is no site and no recent failure.
	/// </summary>
	/// <remarks>
	///     A request that waited for a build held its connection for as long as the build took,
	///     minutes with a Blazor preview host to publish, and every other request queued behind it.
	/// </remarks>
	internal SiteResult GetSiteWithoutWaiting()
	{
		if (CurrentSite is { } site)
		{
			return new SiteResult(site, null);
		}

		InMemorySite? lastGood = Volatile.Read(ref _lastGoodSite);
		if (Volatile.Read(ref _failure) is { } failure && IsRecent(failure))
		{
			return new SiteResult(lastGood, lastGood is null ? failure : null);
		}

		StartBuild();
		return new SiteResult(lastGood, null);
	}

	/// <summary>
	///     Logs a warning the first time a docs request arrives with a path base that the site's
	///     links don't start with (<see cref="MokaDocsOptions.PathBase" />).
	/// </summary>
	internal void CheckPathBase(string? requestPathBase)
	{
		if (string.IsNullOrEmpty(requestPathBase) && string.IsNullOrEmpty(options.PathBase))
		{
			return;
		}

		string actual = SiteConfigFactory.NormalizePathBase(requestPathBase);
		string expected = SiteConfigFactory.NormalizePathBase(options.PathBase);
		if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
		    || _reportedPathBases.Count >= _maxReportedPathBases
		    || !_reportedPathBases.TryAdd(actual, 0))
		{
			return;
		}

		logger.LogWarning(
			"A docs request arrived with the path base \"{RequestPathBase}\", but the site was built for the path base " +
			"\"{PathBase}\" and its links start with \"{Prefix}\", so pages reached this way load without styles, " +
			"scripts or working links. Set MokaDocsOptions.PathBase to the path base readers reach the app through",
			actual, expected, SiteConfigFactory.PublicBasePath(options));
	}

	/// <summary>
	///     Builds the site again and replaces the cached one when the build is done. Requests keep
	///     getting the previous site until then, unlike <see cref="InvalidateCache" />, which makes
	///     the next <see cref="GetSiteAsync" /> call wait for a build.
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
	///     The next docs request starts the rebuild, and requests get the site that was cached before
	///     until it is done, or when it fails. <see cref="GetSiteAsync" /> waits for the rebuild.
	/// </remarks>
	public void InvalidateCache()
	{
		Volatile.Write(ref _site, null);
		Volatile.Write(ref _failure, null);
		logger.LogInformation("MokaDocs cache invalidated - site will be rebuilt on next request");
	}

	/// <summary>The build requests share, started now unless one is already running.</summary>
	private Task<SiteResult> StartBuild()
	{
		lock (_pendingLock)
		{
			if (_pendingBuild is { IsCompleted: false } running)
			{
				return running;
			}

			// On the thread pool: the build's synchronous start, reflection over every assembly, would
			// otherwise run on the thread of the request that started it.
			Task<SiteResult> build = Task.Run(BuildForRequestsAsync);
			_pendingBuild = build;
			return build;
		}
	}

	private bool IsRecent(BuildFailure failure) =>
		TimeSpan.FromMilliseconds(Environment.TickCount64 - failure.At) < FailureRetryDelay;

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

			if (Volatile.Read(ref _failure) is { } failure && IsRecent(failure))
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
		Volatile.Write(ref _lastGoodSite, site);
		Volatile.Write(ref _site, site);
		Volatile.Write(ref _failure, null);
	}

	/// <summary>A build that threw, and when, in <see cref="Environment.TickCount64" /> milliseconds.</summary>
	internal sealed record BuildFailure(Exception Error, long At);

	/// <summary>
	///     A site to serve, or the failure that left none. Neither means the first build is still
	///     running.
	/// </summary>
	internal readonly record struct SiteResult(InMemorySite? Site, BuildFailure? Failure);
}
