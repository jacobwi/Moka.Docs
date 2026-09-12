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
	private List<string> _ignoredDirectories = [];
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
	public void Start(string docsDirectory, string? configFilePath = null) =>
		Start(docsDirectory, configFilePath, []);

	/// <summary>
	///     Start watching the specified directory and optional config file path, ignoring
	///     changes under <paramref name="ignoredDirectories" />.
	/// </summary>
	/// <param name="docsDirectory">The docs directory to watch recursively.</param>
	/// <param name="configFilePath">Optional path to mokadocs.yaml to also watch.</param>
	/// <param name="ignoredDirectories">
	///     Folders whose changes never trigger a rebuild. Pass the build output when it can sit
	///     inside the docs folder, or writing the site triggers the next rebuild.
	/// </param>
	public void Start(string docsDirectory, string? configFilePath, IEnumerable<string> ignoredDirectories)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(FileWatcher));
		}

		_ignoredDirectories = ignoredDirectories.Select(Path.GetFullPath).ToList();

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
		if (IsIgnored(e.FullPath, ((FileSystemWatcher)sender).Path, _ignoredDirectories))
		{
			return;
		}

		_logger.LogDebug("File changed: {Path} ({ChangeType})", e.FullPath, e.ChangeType);
		ScheduleDebounce();
	}

	private void OnRenameEvent(object sender, RenamedEventArgs e)
	{
		if (IsIgnored(e.FullPath, ((FileSystemWatcher)sender).Path, _ignoredDirectories))
		{
			return;
		}

		_logger.LogDebug("File renamed: {OldPath} -> {Path}", e.OldFullPath, e.FullPath);
		ScheduleDebounce();
	}

	/// <summary>
	///     Whether a change is ignored: it's under an ignored folder, or a file or folder below the
	///     watched folder starts with a dot.
	/// </summary>
	/// <remarks>
	///     The old check looked for "_site" and "/." anywhere in the full path. A project stored
	///     under a folder such as <c>~/.projects</c> never rebuilt, a file named <c>my_site.md</c>
	///     was ignored, and output inside the docs folder under any other name rebuilt endlessly.
	/// </remarks>
	internal static bool IsIgnored(string fullPath, string watchedDirectory, IReadOnlyList<string> ignoredDirectories)
	{
		string path = Path.GetFullPath(fullPath);
		foreach (string ignored in ignoredDirectories)
		{
			string dir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ignored));
			if (path.Equals(dir, StringComparison.OrdinalIgnoreCase)
			    || path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		string relative = Path.GetRelativePath(watchedDirectory, path);
		return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			.Any(segment => segment.Length > 1 && segment[0] == '.' && segment != "..");
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
					// Debounce was reset - expected behavior
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in file change handler");
				}
			});
		}
	}
}
