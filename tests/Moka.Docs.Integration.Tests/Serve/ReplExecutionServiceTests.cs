using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     One worker-backed service shared by <see cref="ReplExecutionServiceTests" />, so the tests
///     don't each pay for starting .NET and Roslyn.
/// </summary>
public sealed class ReplWorkerFixture : IDisposable
{
	public ReplExecutionService Service { get; } =
		new(NullLogger<ReplExecutionService>.Instance, StartInfo) { Timeout = TimeSpan.FromSeconds(3) };

	public void Dispose() => Service.Dispose();

	// The CLI is copied into the test output. Start its hidden command the way serve does under
	// `dotnet mokadocs.dll`.
	private static ProcessStartInfo StartInfo() =>
		ServeCommand.CreateReplWorkerStartInfo(
			Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet",
			Path.Combine(AppContext.BaseDirectory, "mokadocs.dll"),
			Environment.ProcessId);
}

/// <summary>
///     Runs snippets in a real <c>mokadocs repl-worker</c> process. They used to run inside serve,
///     where the timeout was only a cancellation token: a snippet that never returned kept a thread
///     spinning, and one that called Environment.Exit stopped the dev server.
/// </summary>
public sealed class ReplExecutionServiceTests(ReplWorkerFixture fixture) : IClassFixture<ReplWorkerFixture>
{
	private ReplExecutionService Service => fixture.Service;

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Expression_ReturnsItsValue()
	{
		ReplResult result = await Service.ExecuteAsync("1 + 2", Ct);

		result.Error.Should().BeNullOrEmpty();
		result.Output.Should().Be("3");
	}

	[Fact]
	public async Task ConsoleOutput_IsCaptured()
	{
		ReplResult result = await Service.ExecuteAsync("Console.WriteLine(\"hello\"); Console.Write(\"world\");", Ct);

		result.Error.Should().BeNullOrEmpty();
		result.Output.Should().Contain("hello").And.EndWith("world");
	}

	[Fact]
	public async Task CompileError_ComesBackAsTheError()
	{
		ReplResult result = await Service.ExecuteAsync("int x = \"text\";", Ct);

		result.Error.Should().NotBeNullOrEmpty();
		result.Output.Should().BeNullOrEmpty();
	}

	[Fact]
	public async Task InfiniteLoop_KillsTheWorkerAndTheNextRunGetsANewOne()
	{
		await Service.ExecuteAsync("0", Ct);
		int? worker = Service.WorkerProcessId;

		ReplResult result = await Service.ExecuteAsync("while (true) { }", Ct);

		result.Error.Should().Contain("timed out");
		worker.Should().NotBeNull();
		HasExited(worker!.Value).Should().BeTrue();
		(await Service.ExecuteAsync("40 + 2", Ct)).Output.Should().Be("42");
		Service.WorkerProcessId.Should().NotBe(worker);
	}

	[Fact]
	public async Task EnvironmentExit_IsReportedAndTheNextRunWorks()
	{
		ReplResult result = await Service.ExecuteAsync("Environment.Exit(7);", Ct);

		result.Error.Should().Contain("exit code 7");
		(await Service.ExecuteAsync("6 * 7", Ct)).Output.Should().Be("42");
	}

	[Fact]
	public async Task ReadLine_GetsNoInputInsteadOfTheNextRequest()
	{
		ReplResult result = await Service.ExecuteAsync("Console.ReadLine() ?? \"no input\"", Ct);

		result.Output.Should().Be("no input");
		(await Service.ExecuteAsync("2 * 21", Ct)).Output.Should().Be("42");
	}

	[Fact]
	public async Task CancelledRun_StopsTheWorkerAndTheNextRunGetsItsOwnResult()
	{
		// The cancelled snippet kept running, and its answer reached the next run's read.
		await Service.ExecuteAsync("0", Ct);
		int? worker = Service.WorkerProcessId;

		using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct))
		{
			cancel.CancelAfter(TimeSpan.FromMilliseconds(300));
			Func<Task> run = () => Service.ExecuteAsync("System.Threading.Thread.Sleep(1500); Console.Write(\"first\");", cancel.Token);
			await run.Should().ThrowAsync<OperationCanceledException>();
		}

		HasExited(worker!.Value).Should().BeTrue();
		(await Service.ExecuteAsync("Console.Write(\"second\");", Ct)).Output.Should().Be("second");
	}

	[Fact]
	public async Task LargeOutput_IsTruncated()
	{
		// Output went into an unbounded string, so printing in a loop grew it without limit.
		ReplResult result = await Service.ExecuteAsync("for (int i = 0; i < 300000; i++) Console.Write('x');", Ct);

		result.Output.Should().StartWith("xxxx").And.EndWith("[Output truncated after 100000 characters.]");
		result.Output!.Length.Should().BeLessThan(100_100);
	}

	[Fact]
	public async Task ReturnValue_IsCappedLikeConsoleOutput()
	{
		// The value went back whole: returning a 150 MB string took the app to 2.4 GB.
		ReplResult result = await Service.ExecuteAsync("new string('x', 150_000)", Ct);

		result.Error.Should().BeNullOrEmpty();
		result.Output.Should().StartWith("xxxx").And.EndWith("[Output truncated after 100000 characters.]");
		result.Output!.Length.Should().BeLessThan(100_100);
	}

	[Fact]
	public async Task AnExceptionMessage_IsCappedLikeConsoleOutput()
	{
		ReplResult result = await Service.ExecuteAsync("throw new Exception(new string('x', 150_000));", Ct);

		result.Error.Should().StartWith("Runtime error: xxxx").And.EndWith("[Output truncated after 100000 characters.]");
		result.Error!.Length.Should().BeLessThan(100_100);
	}

	[Fact]
	public async Task AnEndlessLineOnTheProcessOutput_StopsTheWorkerWithAClearError()
	{
		// The parent read protocol lines without a limit, so a snippet writing to the process's
		// standard output directly grew the app's memory until it ran out.
		await Service.ExecuteAsync("0", Ct);
		int? worker = Service.WorkerProcessId;

		ReplResult result = await Service.ExecuteAsync(
			"var stdout = Console.OpenStandardOutput(); var chunk = new byte[1 << 20]; Array.Fill(chunk, (byte)'a'); " +
			"for (int i = 0; i < 6; i++) stdout.Write(chunk); stdout.Flush(); System.Threading.Thread.Sleep(10000);", Ct);

		result.Error.Should().Contain("wrote too much");
		HasExited(worker!.Value).Should().BeTrue();
		(await Service.ExecuteAsync("6 * 7", Ct)).Output.Should().Be("42");
	}

	[Fact]
	public async Task AWorkerThatExitsBetweenRuns_IsReplaced_AndTheNextRunGetsItsOwnResult()
	{
		// A snippet that ended the process after returning made the next run report "exit code 3".
		ReplResult first = await Service.ExecuteAsync(
			"System.Threading.Tasks.Task.Run(async () => { await System.Threading.Tasks.Task.Delay(300); Environment.Exit(3); }); \"x\"",
			Ct);
		int? worker = Service.WorkerProcessId;
		first.Output.Should().Be("x");
		DateTime until = DateTime.UtcNow.AddSeconds(20);
		while (!HasExited(worker!.Value) && DateTime.UtcNow < until)
		{
			await Task.Delay(50, Ct);
		}

		ReplResult next = await Service.ExecuteAsync("2 + 2", Ct);

		next.Error.Should().BeNullOrEmpty();
		next.Output.Should().Be("4");
		Service.WorkerProcessId.Should().NotBe(worker);
	}

	private static bool HasExited(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			return process.HasExited;
		}
		catch (ArgumentException)
		{
			return true;
		}
	}
}
