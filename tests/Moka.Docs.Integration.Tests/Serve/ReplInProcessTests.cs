using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

[CollectionDefinition(nameof(ConsoleCollection), DisableParallelization = true)]
public sealed class ConsoleCollection;

/// <summary>
///     The REPL service without a worker process, and the line reader the worker protocol uses.
///     In-process runs swap <see cref="Console.Out" />, so nothing else runs beside these tests.
/// </summary>
[Collection(nameof(ConsoleCollection))]
public sealed class ReplInProcessTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task RunsAtTheSameTime_KeepTheirOwnOutput_AndLeaveConsoleOutAlone()
	{
		// Each run swapped Console.Out and restored what it had found, which could be another run's
		// writer, so output crossed between runs and Console.Out ended on a dead writer.
		using var service = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance)
		{
			MaxWaitingRuns = 16,
			Timeout = TimeSpan.FromSeconds(60)
		};
		TextWriter before = Console.Out;

		ReplResult[] results = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Task.Run(() =>
			service.ExecuteAsync($"Console.Write(\"{i}-\"); System.Threading.Thread.Sleep({(6 - i) * 40}); Console.Write(\"{i}\");",
				Ct), Ct)));

		Console.Out.Should().BeSameAs(before);
		for (int i = 0; i < results.Length; i++)
		{
			results[i].Error.Should().BeNullOrEmpty();
			results[i].Output.Should().Be($"{i}-{i}");
		}
	}

	[Fact]
	public async Task ExecuteAsync_PastTheQueueLimit_Throws()
	{
		using var service = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance)
		{
			MaxWaitingRuns = 1,
			Timeout = TimeSpan.FromSeconds(60)
		};
		string started = "mokadocs-queue-started-" + Guid.NewGuid().ToString("N");
		string release = "mokadocs-queue-release-" + Guid.NewGuid().ToString("N");

		// The run is in-process, so it can talk to the test through the app domain. Until it says
		// it started, it may still be compiling, and the next run could take its turn.
		Task<ReplResult> running = Task.Run(() => service.ExecuteAsync(
			$"AppDomain.CurrentDomain.SetData(\"{started}\", true); " +
			$"while (AppDomain.CurrentDomain.GetData(\"{release}\") is null) System.Threading.Thread.Sleep(10); 1", Ct), Ct);
		Task<ReplResult> waiting;
		Exception? refused;
		bool runningWhenRefused;
		try
		{
			await WaitUntilAsync(() => AppDomain.CurrentDomain.GetData(started) is not null || running.IsCompleted);
			waiting = Task.Run(() => service.ExecuteAsync("2", Ct), Ct);
			await WaitUntilAsync(() => service.PendingRuns == 2 || running.IsCompleted);

			refused = await Record.ExceptionAsync(() => service.ExecuteAsync("3", Ct));
			runningWhenRefused = !running.IsCompleted;
		}
		finally
		{
			AppDomain.CurrentDomain.SetData(release, true);
		}

		ReplResult first = await running;
		first.Error.Should().BeNullOrEmpty();
		first.Output.Should().Be("1");
		runningWhenRefused.Should().BeTrue("the first run waits for the test");
		refused.Should().BeOfType<ReplQueueFullException>();
		(await waiting).Output.Should().Be("2");
		service.PendingRuns.Should().Be(0);
	}

	[Fact]
	public async Task BoundedLineReader_ReturnsLinesAndStopsPastTheLimit()
	{
		var reader = new BoundedLineReader(new StringReader("one\r\ntwo\n" + new string('x', 50) + "\nlast"), 10);

		(await reader.ReadLineAsync()).Should().Be(("one", false));
		(await reader.ReadLineAsync()).Should().Be(("two", false));
		(await reader.ReadLineAsync()).Should().Be(((string?)null, true));
	}

	[Fact]
	public async Task BoundedLineReader_ReturnsTheLastLineAndThenTheEnd()
	{
		var reader = new BoundedLineReader(new StringReader("0123456789\r\nend"), 10);

		(await reader.ReadLineAsync()).Should().Be(("0123456789", false));
		(await reader.ReadLineAsync()).Should().Be(("end", false));
		(await reader.ReadLineAsync()).Should().Be(((string?)null, false));
	}

	[Fact]
	public async Task BoundedLineReader_ALongLineAcrossManyReads_IsCaught()
	{
		var reader = new BoundedLineReader(new StringReader(new string('x', 100_000)), 20_000);

		(await reader.ReadLineAsync()).TooLong.Should().BeTrue();
	}

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		DateTime until = DateTime.UtcNow.AddSeconds(30);
		while (!condition())
		{
			DateTime.UtcNow.Should().BeBefore(until, "the condition should hold within 30 seconds");
			await Task.Delay(10, Ct);
		}
	}

	[Fact]
	public void Truncate_DoesNotSplitASurrogatePair()
	{
		string text = new string('x', ReplScriptRunner.MaxOutputLength - 1) + "\U0001F600" + "tail";

		string cut = ReplScriptRunner.Truncate(text);

		cut.Should().StartWith(new string('x', ReplScriptRunner.MaxOutputLength - 1) + "\n[Output truncated");
	}
}
