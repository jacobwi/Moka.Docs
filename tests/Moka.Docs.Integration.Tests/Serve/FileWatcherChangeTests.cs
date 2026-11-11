using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Integration.Tests.AspNetCore;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     A <see cref="FileWatcher" /> on a real folder, changed on disk while it watches.
/// </summary>
public sealed class FileWatcherChangeTests : IDisposable
{
	private static readonly TimeSpan _wait = TimeSpan.FromSeconds(20);
	private readonly string _docs;
	private readonly TestLoggerProvider _logs = new();
	private readonly ILoggerFactory _loggerFactory;
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-watch-" + Guid.NewGuid().ToString("N"));

	public FileWatcherChangeTests()
	{
		_docs = Path.Combine(_root, "docs");
		Directory.CreateDirectory(_docs);
		File.WriteAllText(Path.Combine(_docs, "index.md"), "# Home");
		_loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_logs));
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public void Dispose()
	{
		_loggerFactory.Dispose();
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task ChangesDuringARebuild_QueueOneMoreRebuild_AndNeverOverlap()
	{
		// A change during a build started a second build into the same output folder, and its
		// clean step deleted the files the first was writing.
		using FileWatcher watcher = Create();
		var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		int running = 0;
		int overlapped = 0;
		int runs = 0;
		watcher.OnChanged += async () =>
		{
			if (Interlocked.Increment(ref running) > 1)
			{
				Interlocked.Increment(ref overlapped);
			}

			int run = Interlocked.Increment(ref runs);
			if (run == 1)
			{
				firstStarted.TrySetResult();
				await release.Task;
			}
			else
			{
				secondDone.TrySetResult();
			}

			Interlocked.Decrement(ref running);
		};
		watcher.Start(_docs);

		Touch("index.md", "# One");
		await firstStarted.Task.WaitAsync(_wait, Ct);
		for (int i = 0; i < 3; i++)
		{
			Touch($"page{i}.md", $"# Page {i}");
			await Task.Delay(500, Ct);
		}

		release.SetResult();
		await secondDone.Task.WaitAsync(_wait, Ct);
		await Task.Delay(1000, Ct);

		runs.Should().Be(2, "the changes made during the first rebuild are covered by one more");
		overlapped.Should().Be(0);
	}

	[Fact]
	public async Task Polling_PicksUpChanges_AndSkipsIgnoredFolders()
	{
		// DOTNET_USE_POLLING_FILE_WATCHER was ignored, so folders the OS can't watch, such as some
		// network and container mounts, never rebuilt.
		string output = Path.Combine(_docs, "_site");
		Directory.CreateDirectory(output);
		using FileWatcher watcher = Create(usePolling: true);
		int runs = 0;
		watcher.OnChanged += () =>
		{
			Interlocked.Increment(ref runs);
			return Task.CompletedTask;
		};
		watcher.Start(_docs, null, [output]);
		watcher.IsPolling.Should().BeTrue();

		File.WriteAllText(Path.Combine(output, "index.html"), "built");
		await Task.Delay(1000, Ct);
		runs.Should().Be(0, "the output folder is ignored");

		Touch("guide.md", "# Guide");
		await WaitUntilAsync(() => Volatile.Read(ref runs) == 1);
	}

	[Fact]
	public async Task AWatcherTheSystemRefuses_FallsBackToPolling_WithAWarning()
	{
		// On Linux the inotify limit throws an IOException when watching starts. It escaped from
		// serve and from the host's ApplicationStarted callback.
		using FileWatcher watcher = Create(startRaisingEvents: _ =>
			throw new IOException("The configured user limit (128) on the number of inotify instances has been reached."));
		int runs = 0;
		watcher.OnChanged += () =>
		{
			Interlocked.Increment(ref runs);
			return Task.CompletedTask;
		};

		watcher.Start(_docs);

		watcher.IsPolling.Should().BeTrue();
		_logs.Messages(LogLevel.Warning).Should().Contain(m => m.Contains("inotify") && m.Contains(_docs));
		Touch("guide.md", "# Guide");
		await WaitUntilAsync(() => Volatile.Read(ref runs) == 1);
	}

	[Fact]
	public async Task ABufferOverflow_RebuildsEverything()
	{
		// Without an Error handler, an overflow dropped the changes it lost without a rebuild.
		using FileWatcher watcher = Create();
		int runs = 0;
		watcher.OnChanged += () =>
		{
			Interlocked.Increment(ref runs);
			return Task.CompletedTask;
		};
		watcher.Start(_docs);

		watcher.OnWatcherError(this, new ErrorEventArgs(new InternalBufferOverflowException()));

		await WaitUntilAsync(() => Volatile.Read(ref runs) == 1);
		watcher.IsPolling.Should().BeFalse();
	}

	[Fact]
	public async Task AWatcherThatFails_IsReplacedByPolling()
	{
		using FileWatcher watcher = Create();
		int runs = 0;
		watcher.OnChanged += () =>
		{
			Interlocked.Increment(ref runs);
			return Task.CompletedTask;
		};
		watcher.Start(_docs);
		using var failed = new FileSystemWatcher(_docs) { IncludeSubdirectories = true };

		watcher.OnWatcherError(failed, new ErrorEventArgs(new IOException("The watch limit was reached.")));

		watcher.IsPolling.Should().BeTrue();
		await WaitUntilAsync(() => Volatile.Read(ref runs) == 1);

		// The watcher Start created still runs beside the polling, so the change may rebuild twice.
		Touch("guide.md", "# Guide");
		await WaitUntilAsync(() => Volatile.Read(ref runs) >= 2);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("0", false)]
	[InlineData("false", false)]
	[InlineData("1", true)]
	[InlineData("true", true)]
	[InlineData(" TRUE ", true)]
	public void IsPollingRequested_FollowsTheDotnetVariable(string? value, bool polling)
	{
		FileWatcher.IsPollingRequested(value).Should().Be(polling);
	}

	private FileWatcher Create(bool usePolling = false, Action<FileSystemWatcher>? startRaisingEvents = null) =>
		new(_loggerFactory.CreateLogger<FileWatcher>(), 100)
		{
			UsePolling = usePolling,
			PollingInterval = TimeSpan.FromMilliseconds(100),
			StartRaisingEvents = startRaisingEvents ?? (w => w.EnableRaisingEvents = true)
		};

	private void Touch(string name, string content) => File.WriteAllText(Path.Combine(_docs, name), content);

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		DateTime until = DateTime.UtcNow + _wait;
		while (!condition())
		{
			DateTime.UtcNow.Should().BeBefore(until, "the watcher should react within {0}", _wait);
			await Task.Delay(50, Ct);
		}
	}
}
