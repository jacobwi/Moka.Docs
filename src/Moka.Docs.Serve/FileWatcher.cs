using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Watches a documentation directory and config file for changes,
///     debouncing notifications to avoid rapid-fire rebuilds.
/// </summary>
public sealed class FileWatcher : IDisposable
{
	private readonly TimeSpan _debounceInterval;
	private readonly object _lock = new();
	private readonly ILogger<FileWatcher> _logger;
	private readonly List<FileSystemWatcher> _watchers = [];
	private CancellationTokenSource? _debounceCts;
	private bool _disposed;

	/// <summary>
	///     Creates a new file watcher.
	/// </summary>
	/// <param name="logger">Logger instance.</param>
	/// <param name="debounceMs">Debounce interval in milliseconds (default: 300).</param>
	public FileWatcher(ILogger<FileWatcher> logger, int debounceMs = 300)
	{
		_logger = logger;
		_debounceInterval = TimeSpan.FromMilliseconds(debounceMs);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		_debounceCts?.Cancel();
		_debounceCts?.Dispose();

		foreach (FileSystemWatcher watcher in _watchers)
		{
			watcher.EnableRaisingEvents = false;
			watcher.Dispose();
		}

		_watchers.Clear();
	}

	/// <summary>
	///     Raised when one or more files have changed (after debounce).
	/// </summary>
	public event Func<Task>? OnChanged;

	/// <summary>
	///     Start watching the specified directory and optional config file path.
	/// </summary>
	/// <param name="docsDirectory">The docs directory to watch recursively.</param>
	/// <param name="configFilePath">Optional path to mokadocs.yaml to also watch.</param>
	public void Start(string docsDirectory, string? configFilePath = null)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(FileWatcher));
		}

		// Watch docs directory recursively
		if (Directory.Exists(docsDirectory))
		{
			var docsWatcher = new FileSystemWatcher(docsDirectory)
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName
				               | NotifyFilters.LastWrite
				               | NotifyFilters.CreationTime
				               | NotifyFilters.DirectoryName,
				EnableRaisingEvents = true
			};

			docsWatcher.Changed += OnFileEvent;
			docsWatcher.Created += OnFileEvent;
			docsWatcher.Deleted += OnFileEvent;
			docsWatcher.Renamed += OnRenameEvent;

			_watchers.Add(docsWatcher);
			_logger.LogInformation("Watching directory: {Directory}", docsDirectory);
		}
		else
		{
			_logger.LogWarning("Docs directory does not exist: {Directory}", docsDirectory);
		}

		// Watch config file specifically
		if (configFilePath is not null && File.Exists(configFilePath))
		{
			string configDir = Path.GetDirectoryName(configFilePath)!;
			string configName = Path.GetFileName(configFilePath);

			var configWatcher = new FileSystemWatcher(configDir, configName)
			{
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				EnableRaisingEvents = true
			};

			configWatcher.Changed += OnFileEvent;
			_watchers.Add(configWatcher);
			_logger.LogInformation("Watching config: {ConfigFile}", configFilePath);
		}
	}

	/// <summary>
	///     Stop watching for changes.
	/// </summary>
	public void Stop()
	{
		foreach (FileSystemWatcher watcher in _watchers)
		{
			watcher.EnableRaisingEvents = false;
		}

		_logger.LogInformation("File watching stopped");
	}

	private void OnFileEvent(object sender, FileSystemEventArgs e)
	{
		// Ignore changes in _site output directory or hidden directories
		if (e.FullPath.Contains("_site") || e.FullPath.Contains("/.") || e.FullPath.Contains("\\."))
		{
			return;
		}

		_logger.LogDebug("File changed: {Path} ({ChangeType})", e.FullPath, e.ChangeType);
		ScheduleDebounce();
	}

	private void OnRenameEvent(object sender, RenamedEventArgs e)
	{
		if (e.FullPath.Contains("_site") || e.FullPath.Contains("/.") || e.FullPath.Contains("\\."))
		{
			return;
		}

		_logger.LogDebug("File renamed: {OldPath} -> {Path}", e.OldFullPath, e.FullPath);
		ScheduleDebounce();
	}

	private void ScheduleDebounce()
	{
		lock (_lock)
		{
			// Cancel any existing debounce timer
			_debounceCts?.Cancel();
			_debounceCts?.Dispose();
			_debounceCts = new CancellationTokenSource();
			CancellationToken token = _debounceCts.Token;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(_debounceInterval, token);
					if (!token.IsCancellationRequested)
					{
						_logger.LogInformation("Changes detected, triggering rebuild...");
						if (OnChanged is not null)
						{
							await OnChanged.Invoke();
						}
					}
				}
				catch (TaskCanceledException)
				{
					// Debounce was reset — expected behavior
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in file change handler");
				}
			});
		}
	}
}
