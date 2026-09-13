using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     The worker-process side of out-of-process REPL execution. A host exposes it as a command
///     (the CLI's hidden <c>mokadocs repl-worker</c>) and passes that command to
///     <see cref="ReplExecutionService(ILogger{ReplExecutionService}, Func{ProcessStartInfo})" />,
///     which kills the worker when a snippet runs past its timeout.
/// </summary>
public static class ReplWorker
{
	/// <summary>
	///     Answers requests read from <paramref name="input" /> on <paramref name="output" /> until the
	///     input closes.
	/// </summary>
	/// <param name="input">The parent's requests, one JSON object per line.</param>
	/// <param name="output">Where responses go, one JSON object per line.</param>
	/// <param name="parentProcessId">
	///     The parent's process id. The worker exits when that process is gone: a snippet stuck in a
	///     loop never reads its input again, so it wouldn't notice the parent closing the pipe.
	/// </param>
	/// <param name="ct">Stops the loop.</param>
	/// <returns>The exit code for the worker process.</returns>
	public static async Task<int> RunAsync(Stream input, Stream output, int? parentProcessId = null,
		CancellationToken ct = default)
	{
		using var reader = new StreamReader(input, new UTF8Encoding(false));
		await using var writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true };
		await using var log = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true };

		// Standard input and output carry the protocol. Console.ReadLine in a snippet would swallow
		// the next request, and a write from a snippet's leftover thread would corrupt a response.
		Console.SetIn(TextReader.Null);
		Console.SetOut(TextWriter.Null);
		Console.SetError(TextWriter.Null);

		using Timer? watchdog = parentProcessId is { } pid ? StartParentWatchdog(pid) : null;

		ReplWorkerMessage? configure = ReplWorkerMessage.Parse(await reader.ReadLineAsync(ct));
		ScriptOptions options = ReplScriptRunner.DefaultOptions;
		if (configure?.Type == ReplWorkerMessage.ConfigureType)
		{
			options = ReplScriptRunner.WithAssemblies(options, LoadAssemblies(configure.References ?? [], log));
		}

		// Roslyn's first compile takes a second or more. Paying for it before reporting ready keeps
		// it out of the first snippet's timeout.
		await ReplScriptRunner.RunAsync("0", options, ct);
		await writer.WriteLineAsync(new ReplWorkerMessage { Type = ReplWorkerMessage.ReadyType }.Serialize());

		while (await reader.ReadLineAsync(ct) is { } line)
		{
			ReplWorkerMessage? request = ReplWorkerMessage.Parse(line);
			if (request?.Type != ReplWorkerMessage.ExecuteType)
			{
				continue;
			}

			string code = request.Code ?? "";
			ReplResult result = ReplScriptRunner.Validate(code) ?? await ReplScriptRunner.RunAsync(code, options, ct);
			await writer.WriteLineAsync(new ReplWorkerMessage
			{
				Type = ReplWorkerMessage.ResultType,
				Output = result.Output,
				Error = result.Error
			}.Serialize());
		}

		return 0;
	}

	private static List<Assembly> LoadAssemblies(IEnumerable<string> paths, TextWriter log)
	{
		var assemblies = new List<Assembly>();
		foreach (string path in paths)
		{
			try
			{
				assemblies.Add(Assembly.LoadFrom(path));
			}
			catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
			{
				log.WriteLine($"Skipped reference {path}: {ex.Message}");
			}
		}

		return assemblies;
	}

	private static Timer? StartParentWatchdog(int parentProcessId)
	{
		DateTime parentStarted;
		try
		{
			using Process parent = Process.GetProcessById(parentProcessId);
			parentStarted = parent.StartTime;
		}
		catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
		{
			// The parent is already gone.
			Environment.Exit(0);
			return null;
		}
		catch (Win32Exception)
		{
			// Not allowed to inspect the parent; run without the watchdog.
			return null;
		}

		return new Timer(_ =>
		{
			try
			{
				using Process parent = Process.GetProcessById(parentProcessId);

				// The start time guards against a new process that reused the id.
				if (!parent.HasExited && parent.StartTime == parentStarted)
				{
					return;
				}
			}
			catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
			{
				// No process with that id any more.
			}
			catch (Win32Exception)
			{
				return;
			}

			Environment.Exit(0);
		}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
	}
}
