using System.ComponentModel;
using System.Diagnostics;

namespace Moka.Docs.Plugins;

/// <summary>
///     Runs a command-line tool for a plugin (<c>dotnet publish</c>, the Python analyzer) and
///     collects what it printed, without letting it hang the build.
/// </summary>
internal static class ChildProcessRunner
{
	private static readonly TimeSpan _exitAfterKill = TimeSpan.FromSeconds(10);

	/// <summary>
	///     Starts the process, reads its output and error streams at the same time, and waits for
	///     it to exit. Past <paramref name="timeout" />, or when <paramref name="ct" /> is cancelled,
	///     the process and everything it started are killed.
	/// </summary>
	/// <param name="startInfo">What to run. Stream redirection and window settings are set here.</param>
	/// <param name="timeout">How long the process may run.</param>
	/// <param name="ct">The build's cancellation token.</param>
	/// <returns>The exit code and both streams, or a result with <see cref="ChildProcessResult.TimedOut" /> set.</returns>
	/// <exception cref="Win32Exception">The executable could not be started, for example because it isn't on PATH.</exception>
	/// <exception cref="OperationCanceledException"><paramref name="ct" /> was cancelled.</exception>
	internal static async Task<ChildProcessResult> RunAsync(ProcessStartInfo startInfo, TimeSpan timeout,
		CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;

		using Process process = Process.Start(startInfo)
		                        ?? throw new Win32Exception($"'{startInfo.FileName}' could not be started.");
		using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
		limit.CancelAfter(timeout);

		// Both streams are drained while waiting: a tool that fills the error pipe while the
		// output pipe is still open blocks on the write, and a reader waiting for the end of
		// the output never sees it. The reads take no token: they end when the pipes close,
		// which killing the process tree does.
		Task<string> output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
		Task<string> error = process.StandardError.ReadToEndAsync(CancellationToken.None);
		try
		{
			await process.WaitForExitAsync(limit.Token);

			// A grandchild that inherited the pipes can keep them open after the tool exits.
			await Task.WhenAll(output, error).WaitAsync(limit.Token);
		}
		catch (OperationCanceledException)
		{
			await KillTreeAsync(process);
			ct.ThrowIfCancellationRequested();
			return new ChildProcessResult(-1, "", "", true);
		}

		return new ChildProcessResult(process.ExitCode, await output, await error, false);
	}

	private static async Task KillTreeAsync(Process process)
	{
		try
		{
			process.Kill(true);
		}
		catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
		{
			// It exited between the timeout and the kill, or the tree couldn't be walked.
		}

		try
		{
			await process.WaitForExitAsync(CancellationToken.None).WaitAsync(_exitAfterKill);
		}
		catch (Exception ex) when (ex is TimeoutException or InvalidOperationException)
		{
			// The build moves on either way; waiting only lets the files the tree held close first.
		}
	}
}

/// <summary>What a child process printed and how it ended.</summary>
/// <param name="ExitCode">The exit code, or -1 when it timed out.</param>
/// <param name="Output">Everything written to standard output.</param>
/// <param name="Error">Everything written to standard error.</param>
/// <param name="TimedOut">Whether the process was killed for running past its timeout.</param>
internal readonly record struct ChildProcessResult(int ExitCode, string Output, string Error, bool TimedOut);
