using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Watches a documentation directory and config file for changes,
///     debouncing notifications to avoid rapid-fire rebuilds.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="OnChanged" /> handlers run one at a time. Changes that arrive while they run
///         queue a single run after them, which sees every change up to its start. Rebuilds used to
///         overlap, and one build's clean step deleted the folder another was writing.
///     </para>
///     <para>
///         When the <c>DOTNET_USE_POLLING_FILE_WATCHER</c> environment variable is <c>1</c> or
///         <c>true</c>, or the operating system can't watch a folder (the inotify limits on Linux),
///         the folder is checked for changes every second instead.
///     </para>
/// </remarks>
public sealed class FileWatcher : IDisposable
{
	/// <summary>The variable ASP.NET Core and <c>dotnet watch</c> read to poll instead of watching.</summary>
	internal const string PollingVariable = "DOTNET_USE_POLLING_FILE_WATCHER";

	private const NotifyFilters _docsFilters = NotifyFilters.FileName
	                                           | NotifyFilters.LastWrite
	                                           | NotifyFilters.CreationTime
	                                           | NotifyFilters.DirectoryName;

	private static readonly EnumerationOptions _oneLevel = new() { IgnoreInaccessible = true, AttributesToSkip = 0 };

	private readonly TimeSpan _debounceInterval;
	private readonly object _lock = new();
	private readonly ILogger<FileWatcher> _logger;
	private readonly List<PolledFolder> _polled = [];
	private readonly List<FileSystemWatcher> _watchers = [];
	private CancellationTokenSource? _debounceCts;
	private bool _disposed;
	private List<string> _ignoredDirectories = [];
	private Timer? _pollTimer;
	private bool _rerunQueued;
	private bool _running;

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

	/// <summary>Whether folders are polled from the start. Defaults to the environment variable.</summary>
	internal bool UsePolling { get; init; } = IsPollingRequested(Environment.GetEnvironmentVariable(PollingVariable));

	/// <summary>How often polled folders are checked.</summary>
	internal TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(1);

	/// <summary>Turns a watcher on. Tests replace it to fail the way the inotify limit does.</summary>
	internal Action<FileSystemWatcher> StartRaisingEvents { get; init; } = static w => w.EnableRaisingEvents = true;

	/// <summary>Whether any folder is being polled rather than watched.</summary>
	internal bool IsPolling
	{
		get
		{
			lock (_lock)
			{
				return _polled.Count > 0;
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		List<FileSystemWatcher> watchers;
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_debounceCts?.Cancel();
			_debounceCts?.Dispose();
			_debounceCts = null;
			_pollTimer?.Dispose();
			_pollTimer = null;
			_polled.Clear();
			watchers = [.. _watchers];
			_watchers.Clear();
		}

		foreach (FileSystemWatcher watcher in watchers)
		{
			watcher.EnableRaisingEvents = false;
			watcher.Dispose();
		}
	}

	/// <summary>
	///     Raised when one or more files have changed (after debounce). Handlers never run
	///     concurrently.
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
		ObjectDisposedException.ThrowIf(_disposed, this);

		_ignoredDirectories = ignoredDirectories.Select(Path.GetFullPath).ToList();

		// Watch docs directory recursively
		if (Directory.Exists(docsDirectory))
		{
			Watch(docsDirectory, null, true, _docsFilters);
			_logger.LogInformation("Watching directory: {Directory}", docsDirectory);
		}
		else
		{
			_logger.LogWarning("Docs directory does not exist: {Directory}", docsDirectory);
		}

		// Watch config file specifically
		if (configFilePath is not null && File.Exists(configFilePath))
		{
			string configDir = Path.GetDirectoryName(Path.GetFullPath(configFilePath))!;
			Watch(configDir, Path.GetFileName(configFilePath), false,
				NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime);
			_logger.LogInformation("Watching config: {ConfigFile}", configFilePath);
		}
	}

	/// <summary>
	///     Stop watching for changes.
	/// </summary>
	public void Stop()
	{
		lock (_lock)
		{
			foreach (FileSystemWatcher watcher in _watchers)
			{
				watcher.EnableRaisingEvents = false;
			}

			_pollTimer?.Dispose();
			_pollTimer = null;
			_polled.Clear();
		}

		_logger.LogInformation("File watching stopped");
	}

	/// <summary>Whether <paramref name="value" />, the polling variable, asks for polling.</summary>
	internal static bool IsPollingRequested(string? value) =>
		value is not null
		&& (value.Trim() == "1" || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase));

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

	/// <summary>
	///     Handles a watcher error. A buffer overflow drops events, so everything is rebuilt. Any
	///     other error stops the watcher, so its folder is polled from then on. Without a handler,
	///     both lost changes silently.
	/// </summary>
	internal void OnWatcherError(object sender, ErrorEventArgs e)
	{
		Exception error = e.GetException();
		if (error is InternalBufferOverflowException)
		{
			_logger.LogWarning("Too many changes at once for the file watcher to list; rebuilding everything");
			ScheduleDebounce();
			return;
		}

		if (sender is not FileSystemWatcher watcher)
		{
			ScheduleDebounce();
			return;
		}

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_watchers.Remove(watcher);
		}

		string? fileName = watcher.Filter is "" or "*" or "*.*" ? null : watcher.Filter;
		_logger.LogWarning(error, "Watching {Directory} failed; checking it for changes every {Seconds}s instead",
			watcher.Path, PollingInterval.TotalSeconds);
		watcher.EnableRaisingEvents = false;
		watcher.Dispose();
		AddPolled(watcher.Path, fileName, watcher.IncludeSubdirectories);
		ScheduleDebounce();
	}

	private void Watch(string directory, string? fileName, bool recursive, NotifyFilters filters)
	{
		if (UsePolling)
		{
			AddPolled(directory, fileName, recursive);
			return;
		}

		var watcher = fileName is null ? new FileSystemWatcher(directory) : new FileSystemWatcher(directory, fileName);
		watcher.IncludeSubdirectories = recursive;
		watcher.NotifyFilter = filters;
		watcher.Changed += OnFileEvent;
		watcher.Created += OnFileEvent;
		watcher.Deleted += OnFileEvent;
		watcher.Renamed += OnRenameEvent;
		watcher.Error += OnWatcherError;

		try
		{
			StartRaisingEvents(watcher);
		}
		catch (IOException ex)
		{
			// On Linux this is the inotify instance or watch limit. It used to escape from serve and
			// from the host's ApplicationStarted callback.
			watcher.Dispose();
			_logger.LogWarning(
				"Can't watch {Directory} for changes ({Message}); checking it every {Seconds}s instead. " +
				"On Linux, raising fs.inotify.max_user_instances and fs.inotify.max_user_watches lets it watch",
				directory, ex.Message, PollingInterval.TotalSeconds);
			AddPolled(directory, fileName, recursive);
			return;
		}

		lock (_lock)
		{
			if (_disposed)
			{
				watcher.Dispose();
				return;
			}

			_watchers.Add(watcher);
		}
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

	private void ScheduleDebounce()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

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
					await RaiseChangedAsync();
				}
				catch (OperationCanceledException)
				{
					// Debounce was reset - expected behavior
				}
			});
		}
	}

	/// <summary>
	///     Runs the handlers, or queues one more run when they are already running.
	/// </summary>
	private async Task RaiseChangedAsync()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			if (_running)
			{
				_rerunQueued = true;
				return;
			}

			_running = true;
		}

		while (true)
		{
			_logger.LogInformation("Changes detected, triggering rebuild...");
			foreach (Func<Task> handler in OnChanged?.GetInvocationList().Cast<Func<Task>>() ?? [])
			{
				try
				{
					await handler();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in file change handler");
				}
			}

			lock (_lock)
			{
				if (!_rerunQueued || _disposed)
				{
					_running = false;
					return;
				}

				_rerunQueued = false;
			}
		}
	}

	private void AddPolled(string directory, string? fileName, bool recursive)
	{
		var folder = new PolledFolder(directory, fileName, recursive);
		folder.Snapshot = TakeSnapshot(folder);

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_polled.Add(folder);
			_pollTimer ??= new Timer(_ => Poll(), null, PollingInterval, Timeout.InfiniteTimeSpan);
		}
	}

	private void Poll()
	{
		List<PolledFolder> folders;
		lock (_lock)
		{
			if (_disposed || _pollTimer is null)
			{
				return;
			}

			folders = [.. _polled];
		}

		bool changed = false;
		foreach (PolledFolder folder in folders)
		{
			try
			{
				Dictionary<string, (long Length, DateTime Written)> current = TakeSnapshot(folder);
				if (!SameSnapshot(folder.Snapshot, current))
				{
					changed = true;
				}

				folder.Snapshot = current;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_logger.LogDebug("Checking {Directory} for changes failed: {Message}", folder.Directory, ex.Message);
			}
		}

		if (changed)
		{
			_logger.LogDebug("Polling found changes");
			ScheduleDebounce();
		}

		lock (_lock)
		{
			// Timers re-armed one tick at a time never overlap, however long a check takes.
			_pollTimer?.Change(PollingInterval, Timeout.InfiniteTimeSpan);
		}
	}

	private Dictionary<string, (long Length, DateTime Written)> TakeSnapshot(PolledFolder folder)
	{
		var snapshot = new Dictionary<string, (long, DateTime)>(StringComparer.Ordinal);
		if (folder.FileName is { } fileName)
		{
			var file = new FileInfo(Path.Combine(folder.Directory, fileName));
			if (file.Exists)
			{
				snapshot[file.FullName] = (file.Length, file.LastWriteTimeUtc);
			}

			return snapshot;
		}

		AddToSnapshot(snapshot, folder.Directory, folder.Directory, folder.Recursive);
		return snapshot;
	}

	private void AddToSnapshot(Dictionary<string, (long, DateTime)> snapshot, string root, string directory,
		bool recursive)
	{
		var info = new DirectoryInfo(directory);
		if (!info.Exists)
		{
			return;
		}

		foreach (FileSystemInfo entry in info.EnumerateFileSystemInfos("*", _oneLevel))
		{
			if (IsIgnored(entry.FullName, root, _ignoredDirectories))
			{
				continue;
			}

			if (entry is FileInfo file)
			{
				snapshot[file.FullName] = (file.Length, file.LastWriteTimeUtc);
			}
			else
			{
				snapshot[entry.FullName] = (-1, default);

				// A folder link can point back up the tree.
				if (recursive && entry.LinkTarget is null)
				{
					AddToSnapshot(snapshot, root, entry.FullName, true);
				}
			}
		}
	}

	private static bool SameSnapshot(Dictionary<string, (long Length, DateTime Written)> before,
		Dictionary<string, (long Length, DateTime Written)> after) =>
		before.Count == after.Count
		&& before.All(entry => after.TryGetValue(entry.Key, out (long, DateTime) now) && now == entry.Value);

	/// <summary>A folder, or one file in it, checked on a timer.</summary>
	private sealed class PolledFolder(string directory, string? fileName, bool recursive)
	{
		public string Directory { get; } = directory;
		public string? FileName { get; } = fileName;
		public bool Recursive { get; } = recursive;
		public Dictionary<string, (long Length, DateTime Written)> Snapshot { get; set; } = [];
	}
}
