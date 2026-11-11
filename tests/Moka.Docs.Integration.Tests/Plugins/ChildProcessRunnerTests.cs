using System.Diagnostics;
using FluentAssertions;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.Plugins;

public sealed class ChildProcessRunnerTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "mokadocs-runner-" + Guid.NewGuid().ToString("N"));

	public ChildProcessRunnerTests()
	{
		Directory.CreateDirectory(_directory);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_directory, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task ToolThatFillsTheErrorPipeBeforeItWritesOutput_Finishes()
	{
		// dotnet publish was read as all of stdout, then all of stderr. A tool that filled the
		// stderr pipe before closing stdout blocked on the write while mokadocs waited for the
		// end of stdout, and neither side ever moved.
		string big = Path.Combine(_directory, "big.txt");
		await File.WriteAllTextAsync(big, new string('e', 1 << 20), TestContext.Current.CancellationToken);
		ProcessStartInfo startInfo;
		if (OperatingSystem.IsWindows())
		{
			startInfo = new ProcessStartInfo("cmd.exe", $"/d /c type \"{big}\" 1>&2 & echo done");
		}
		else
		{
			startInfo = new ProcessStartInfo("/bin/sh");
			startInfo.ArgumentList.Add("-c");
			startInfo.ArgumentList.Add("cat \"$0\" >&2; echo done");
			startInfo.ArgumentList.Add(big);
		}

		Task<ChildProcessResult> run = ChildProcessRunner.RunAsync(startInfo, TimeSpan.FromMinutes(5),
			TestContext.Current.CancellationToken);

		(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken)))
			.Should().BeSameAs(run, "reading both streams together can't deadlock");
		ChildProcessResult result = await run;
		result.TimedOut.Should().BeFalse();
		result.ExitCode.Should().Be(0);
		result.Output.Trim().Should().Be("done");
		result.Error.Length.Should().BeGreaterThanOrEqualTo(1 << 20);
	}

	[Fact]
	public async Task ToolThatOutlivesItsTimeout_IsKilledWithEverythingItStarted()
	{
		using var tree = new HangingProcessTree();

		ChildProcessResult result = await ChildProcessRunner.RunAsync(tree.StartInfo(), TimeSpan.FromSeconds(3),
			TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

		result.TimedOut.Should().BeTrue();
		(await tree.GrandchildExitedAsync(TimeSpan.FromSeconds(15))).Should().BeTrue(
			"the timeout kills the whole process tree, not only the direct child");
	}

	[Fact]
	public async Task CancellingTheBuild_KillsTheToolAndThrows()
	{
		using var tree = new HangingProcessTree();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		Task<ChildProcessResult> run = ChildProcessRunner.RunAsync(tree.StartInfo(), TimeSpan.FromMinutes(5), cts.Token);
		await tree.WaitUntilStartedAsync(TimeSpan.FromSeconds(30));

		await cts.CancelAsync();
		Func<Task> act = () => run.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<OperationCanceledException>();
		(await tree.GrandchildExitedAsync(TimeSpan.FromSeconds(15))).Should().BeTrue();
	}
}
