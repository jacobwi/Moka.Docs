using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Docs.Serve;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Runs what follows startup: the first build (<see cref="MokaDocsOptions.BuildOnStartup" />),
///     watching the docs folder (<see cref="MokaDocsOptions.WatchDocs" />) and the warning about a
///     REPL that can't start its worker.
/// </summary>
/// <remarks>
///     Everything starts from <see cref="IHostApplicationLifetime.ApplicationStarted" />, not from
///     <see cref="StartAsync" />. By then every endpoint is mapped and the server listens, so a
///     plugin that reads the app's endpoints during the build, such as an OpenAPI document,
///     sees them all.
/// </remarks>
internal sealed class MokaDocsStartupService(
	MokaDocsService docs,
	MokaDocsOptions options,
	IHostApplicationLifetime lifetime,
	IHostEnvironment environment,
	IServiceProvider services,
	ILoggerFactory loggerFactory)
	: IHostedService, IDisposable
{
	private readonly ILogger<MokaDocsStartupService> _logger = loggerFactory.CreateLogger<MokaDocsStartupService>();
	private readonly CancellationTokenSource _stopping = new();
	private readonly List<Task> _work = [];
	private CancellationTokenRegistration _started;
	private FileWatcher? _watcher;

	/// <summary>The build started at startup, or <c>null</c> when none was.</summary>
	internal Task? StartupBuild { get; private set; }

	/// <summary>The folder being watched, or <c>null</c> when nothing is.</summary>
	internal string? WatchedDirectory { get; private set; }

	/// <inheritdoc />
	public void Dispose()
	{
		_started.Dispose();
		_watcher?.Dispose();
		_stopping.Dispose();
	}

	/// <inheritdoc />
	public Task StartAsync(CancellationToken cancellationToken)
	{
		_started = lifetime.ApplicationStarted.Register(OnStarted);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await _stopping.CancelAsync();
		_watcher?.Dispose();
		_watcher = null;

		Task[] work;
		lock (_work)
		{
			work = [.. _work];
		}

		try
		{
			await Task.WhenAll(work).WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			// The host stopped waiting. A build in progress stops at its next cancellation check.
		}
	}

	/// <summary>
	///     Whether the docs folder is watched: <see cref="MokaDocsOptions.WatchDocs" />, or in
	///     Development when it is unset. Without a cache every request rebuilds anyway.
	/// </summary>
	internal static bool ShouldWatch(MokaDocsOptions options, IHostEnvironment environment) =>
		options.CacheOutput
		&& options.DocsPath is not null
		&& (options.WatchDocs ?? environment.IsDevelopment());

	private void OnStarted()
	{
		// An app that registers the docs but maps them only in some environments shouldn't pay
		// for a build or a watcher it never serves.
		if (!docs.IsMapped)
		{
			return;
		}

		if (options.CacheOutput && options.BuildOnStartup)
		{
			StartupBuild = BuildAtStartupAsync();
			Track(StartupBuild);
		}

		if (ShouldWatch(options, environment))
		{
			StartWatching();
		}

		if (services.GetService<MokaDocsRepl>() is { IsMapped: true, CanStartWorker: false } repl)
		{
			repl.WarnNotWiredOnce();
		}
	}

	private async Task BuildAtStartupAsync()
	{
		// Off the thread that raised ApplicationStarted: the host waits for its callbacks.
		await Task.Yield();
		try
		{
			await docs.GetSiteAsync(_stopping.Token);
		}
		catch (Exception ex) when (IsStopping(ex))
		{
			// The app stopped before the build finished.
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "The MokaDocs build at startup failed; the first request builds the site again");
		}
	}

	private void StartWatching()
	{
		string contentRoot = environment.ContentRootPath is { Length: > 0 } root ? root : Directory.GetCurrentDirectory();
		string docsDirectory = Path.GetFullPath(Path.Combine(contentRoot, options.DocsPath!));

		_watcher = new FileWatcher(loggerFactory.CreateLogger<FileWatcher>());
		_watcher.OnChanged += () =>
		{
			Task rebuild = RebuildAsync();
			Track(rebuild);
			return rebuild;
		};
		_watcher.Start(docsDirectory);
		WatchedDirectory = docsDirectory;
	}

	private async Task RebuildAsync()
	{
		try
		{
			await docs.RebuildAsync(_stopping.Token);
		}
		catch (Exception ex) when (IsStopping(ex))
		{
			// The app stopped during the rebuild.
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Rebuilding the MokaDocs site after a change failed; serving the previous site");
		}
	}

	private bool IsStopping(Exception ex) =>
		_stopping.IsCancellationRequested && ex is OperationCanceledException or ObjectDisposedException;

	private void Track(Task task)
	{
		lock (_work)
		{
			_work.RemoveAll(t => t.IsCompleted);
			_work.Add(task);
		}
	}
}
