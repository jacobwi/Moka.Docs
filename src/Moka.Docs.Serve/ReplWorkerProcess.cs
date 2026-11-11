using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     A running <see cref="ReplWorker" /> process, seen from the parent.
/// </summary>
internal sealed class ReplWorkerProcess : IDisposable
{
	/// <summary>
	///     The longest line read from the worker, in characters. A result carries at most 100,000
	///     characters each of output and error, and JSON can escape a character as six.
	/// </summary>
	internal const int MaxMessageLength = 4 * 1024 * 1024;

	private readonly BoundedLineReader _lines;
	private readonly ILogger _logger;
	private readonly Process _process;

	private ReplWorkerProcess(Process process, ILogger logger)
	{
		_process = process;
		_logger = logger;
		_lines = new BoundedLineReader(process.StandardOutput, MaxMessageLength);
	}

	/// <summary>The worker's process id.</summary>
	internal int ProcessId => _process.Id;

	/// <summary>Whether the worker has exited, for example from a timer a snippet left behind.</summary>
	internal bool HasExited
	{
		get
		{
			try
			{
				return _process.HasExited;
			}
			catch (InvalidOperationException)
			{
				return true;
			}
		}
	}

	/// <summary>
	///     Starts a worker, sends it the references and waits until it reports ready.
	/// </summary>
	/// <exception cref="InvalidOperationException">The worker didn't start or didn't become ready.</exception>
	internal static async Task<ReplWorkerProcess> StartAsync(ProcessStartInfo startInfo,
		IReadOnlyList<string> references, TimeSpan startupTimeout, ILogger logger, CancellationToken ct)
	{
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;
		startInfo.RedirectStandardInput = true;
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.StandardInputEncoding = new UTF8Encoding(false);
		startInfo.StandardOutputEncoding = new UTF8Encoding(false);
		startInfo.StandardErrorEncoding = new UTF8Encoding(false);
		ReplWorkerEnvironment.RemoveHotReloadVariables(startInfo.Environment);

		Process process = Process.Start(startInfo)
		                  ?? throw new InvalidOperationException($"'{startInfo.FileName}' did not start.");
		var worker = new ReplWorkerProcess(process, logger);

		// Drained continuously: a full stderr pipe would block the worker.
		process.ErrorDataReceived += (_, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				logger.LogDebug("REPL worker: {Line}", e.Data);
			}
		};
		process.BeginErrorReadLine();

		try
		{
			await worker.SendAsync(
				new ReplWorkerMessage { Type = ReplWorkerMessage.ConfigureType, References = [.. references] }, ct);

			(ReplWorkerMessage? ready, ReplWorkerStatus status) = await worker.ReceiveAsync(startupTimeout, ct);
			if (ready?.Type != ReplWorkerMessage.ReadyType)
			{
				throw new InvalidOperationException(status switch
				{
					ReplWorkerStatus.TimedOut => $"it did not report ready within {startupTimeout.TotalSeconds:0} seconds",
					ReplWorkerStatus.Oversized => "it wrote a line longer than the protocol allows",
					_ => $"it exited with code {worker.WaitForExitCode()?.ToString() ?? "unknown"}"
				});
			}

			return worker;
		}
		catch
		{
			worker.Dispose();
			throw;
		}
	}

	/// <summary>
	///     Runs a snippet and waits up to <paramref name="timeout" /> for its result.
	/// </summary>
	internal async Task<(ReplResult? Result, ReplWorkerStatus Status)> ExecuteAsync(string code, TimeSpan timeout,
		CancellationToken ct)
	{
		try
		{
			await SendAsync(new ReplWorkerMessage { Type = ReplWorkerMessage.ExecuteType, Code = code }, ct);
		}
		catch (IOException)
		{
			// The pipe broke: the worker was gone before it got the snippet.
			return (null, ReplWorkerStatus.NotSent);
		}

		(ReplWorkerMessage? message, ReplWorkerStatus status) = await ReceiveAsync(timeout, ct);
		return message?.Type == ReplWorkerMessage.ResultType
			? (new ReplResult { Output = message.Output, Error = message.Error }, ReplWorkerStatus.Completed)
			: (null, status);
	}

	/// <summary>Kills the worker and everything it started.</summary>
	internal void Kill()
	{
		try
		{
			if (!_process.HasExited)
			{
				_process.Kill(true);
			}
		}
		catch (InvalidOperationException)
		{
			// Already exited.
		}
		catch (Win32Exception ex)
		{
			_logger.LogWarning(ex, "Could not stop the REPL worker process {ProcessId}", _process.Id);
		}

		_process.WaitForExit(2000);
	}

	/// <summary>The exit code once the worker has exited, or <c>null</c> when it is still running.</summary>
	internal int? WaitForExitCode()
	{
		try
		{
			return _process.WaitForExit(2000) ? _process.ExitCode : null;
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		try
		{
			// An idle worker ends its read loop when its input closes.
			_process.StandardInput.Close();
		}
		catch (IOException)
		{
			// Already gone.
		}

		if (!_process.WaitForExit(1000))
		{
			Kill();
		}

		_process.Dispose();
	}

	private async Task SendAsync(ReplWorkerMessage message, CancellationToken ct)
	{
		await _process.StandardInput.WriteLineAsync(message.Serialize().AsMemory(), ct);
		await _process.StandardInput.FlushAsync(ct);
	}

	private async Task<(ReplWorkerMessage? Message, ReplWorkerStatus Status)> ReceiveAsync(TimeSpan timeout,
		CancellationToken ct)
	{
		var elapsed = Stopwatch.StartNew();
		while (true)
		{
			TimeSpan remaining = timeout - elapsed.Elapsed;
			if (remaining <= TimeSpan.Zero)
			{
				return (null, ReplWorkerStatus.TimedOut);
			}

			// WaitAsync instead of cancelling the read: cancelling a pipe read isn't reliable on
			// every platform, and a worker that times out is killed, which ends the read anyway.
			Task<(string? Line, bool TooLong)> read = _lines.ReadLineAsync();
			string? line;
			bool tooLong;
			try
			{
				(line, tooLong) = await read.WaitAsync(remaining, ct);
			}
			catch (TimeoutException)
			{
				_ = read.ContinueWith(static t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
				return (null, ReplWorkerStatus.TimedOut);
			}

			if (tooLong)
			{
				return (null, ReplWorkerStatus.Oversized);
			}

			if (line is null)
			{
				return (null, ReplWorkerStatus.Exited);
			}

			if (ReplWorkerMessage.Parse(line) is { } message)
			{
				return (message, ReplWorkerStatus.Completed);
			}

			// Something other than the protocol reached standard output, such as a snippet writing
			// to Console.OpenStandardOutput(). Skip it and keep waiting for the result.
			_logger.LogDebug("REPL worker wrote a line that isn't a message: {Line}", line);
		}
	}
}

/// <summary>How a request to a <see cref="ReplWorkerProcess" /> ended.</summary>
internal enum ReplWorkerStatus
{
	/// <summary>The worker answered.</summary>
	Completed,

	/// <summary>No answer before the timeout.</summary>
	TimedOut,

	/// <summary>The worker process ended before answering.</summary>
	Exited,

	/// <summary>The worker had exited before the request could be sent.</summary>
	NotSent,

	/// <summary>The worker wrote a line longer than <see cref="ReplWorkerProcess.MaxMessageLength" />.</summary>
	Oversized
}
