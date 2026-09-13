using System.ComponentModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Executes C# code snippets using Roslyn scripting. Captures console output,
///     handles compilation errors, and enforces a timeout.
/// </summary>
/// <remarks>
///     Snippets run in a separate worker process when the service is created with a worker
///     command, and in the calling process otherwise. Only the worker can be stopped: a timeout
///     kills it, while in-process a snippet that never returns keeps its thread busy.
/// </remarks>
public sealed class ReplExecutionService : IDisposable
{
	private readonly SemaphoreSlim _gate = new(1, 1);
	private readonly ILogger<ReplExecutionService> _logger;
	private readonly List<string> _referencePaths = [];
	private readonly List<string> _temporaryDirectories = [];
	private readonly Func<ProcessStartInfo>? _workerStartInfo;
	private bool _disposed;

	/// <summary>
	///     Script options for in-process runs, with the default imports plus loaded packages and
	///     project assemblies.
	/// </summary>
	private ScriptOptions _scriptOptions = ReplScriptRunner.DefaultOptions;

	private ReplWorkerProcess? _worker;

	/// <summary>
	///     Creates a service that runs snippets in this process. A snippet that never returns can't
	///     be stopped; prefer the constructor that takes a worker command.
	/// </summary>
	/// <param name="logger">Logger instance.</param>
	public ReplExecutionService(ILogger<ReplExecutionService> logger)
	{
		_logger = logger;
	}

	/// <summary>
	///     Creates a service that runs snippets in a worker process, which is killed and replaced when
	///     a snippet runs past the timeout or ends the process.
	/// </summary>
	/// <param name="logger">Logger instance.</param>
	/// <param name="workerStartInfo">
	///     Returns the command for a process that calls <see cref="ReplWorker.RunAsync" /> with its
	///     standard input and output, such as <c>mokadocs repl-worker</c>. Called for every worker
	///     started. Redirection settings are applied by the service.
	/// </param>
	public ReplExecutionService(ILogger<ReplExecutionService> logger, Func<ProcessStartInfo> workerStartInfo)
		: this(logger)
	{
		_workerStartInfo = workerStartInfo;
	}

	/// <summary>How long a run may take, compile included.</summary>
	internal TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

	/// <summary>How long a new worker may take to load .NET, Roslyn and the references.</summary>
	internal TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(60);

	/// <summary>The current worker's process id, or <c>null</c> when none is running.</summary>
	internal int? WorkerProcessId => _worker?.ProcessId;

	/// <summary>
	///     Resolves the given NuGet package specifications and adds their assemblies
	///     and root namespaces to the REPL script options. Each spec is either
	///     "PackageName" or "PackageName@Version".
	/// </summary>
	/// <returns>What was loaded; <see cref="NuGetPackageResolver.ResolvedPackages.Error" /> says why nothing was.</returns>
	public async Task<NuGetPackageResolver.ResolvedPackages> LoadPackagesAsync(IReadOnlyList<string> packageSpecs,
		CancellationToken ct = default)
	{
		if (packageSpecs.Count == 0)
		{
			return new NuGetPackageResolver.ResolvedPackages();
		}

		var resolver = new NuGetPackageResolver(_logger);
		if (_workerStartInfo is null)
		{
			NuGetPackageResolver.ResolvedPackages resolved = await resolver.ResolveAsync(packageSpecs, ct);
			AddResolvedPackages(resolved);
			return resolved;
		}

		// The worker loads the packages from disk, so they stay published until Dispose.
		string directory = Path.Combine(Path.GetTempPath(), "mokadocs-repl-" + Guid.NewGuid().ToString("N")[..8]);
		_temporaryDirectories.Add(directory);
		NuGetPackageResolver.ResolvedPackages published = await resolver.PublishAsync(packageSpecs, directory, ct);
		if (published.Error is null)
		{
			await _gate.WaitAsync(ct);
			try
			{
				AddReferences(published.AssemblyPaths);
			}
			finally
			{
				_gate.Release();
			}
		}

		return published;
	}

	/// <summary>
	///     Loads a project output assembly into the REPL so users can reference
	///     the documented project's types directly.
	/// </summary>
	public void LoadProjectAssembly(string dllPath)
	{
		if (_workerStartInfo is null)
		{
			var resolver = new NuGetPackageResolver(_logger);
			AddResolvedPackages(resolver.LoadAssemblyFromPath(dllPath));
			return;
		}

		if (!File.Exists(dllPath))
		{
			_logger.LogWarning("Assembly not found: {Path}", dllPath);
			return;
		}

		_gate.Wait();
		try
		{
			AddReferences([dllPath]);
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <summary>
	///     Starts the worker process ahead of the first run, so that run doesn't wait for it. Does
	///     nothing when snippets run in-process.
	/// </summary>
	public async Task WarmUpAsync(CancellationToken ct = default)
	{
		if (_workerStartInfo is null)
		{
			return;
		}

		await _gate.WaitAsync(ct);
		try
		{
			if (!_disposed)
			{
				_worker ??= await StartWorkerAsync(ct);
			}
		}
		catch (Exception ex) when (IsStartFailure(ex))
		{
			// The first run tries again and reports the failure to the page.
			_logger.LogWarning("REPL: the worker process failed to start: {Message}", ex.Message);
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <summary>
	///     Executes the given C# code and returns the result.
	/// </summary>
	/// <param name="code">The C# source code to execute.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The execution result containing output and/or error messages.</returns>
	public async Task<ReplResult> ExecuteAsync(string code, CancellationToken ct = default)
	{
		if (ReplScriptRunner.Validate(code) is { } invalid)
		{
			return invalid;
		}

		_logger.LogDebug("REPL: Executing {Length} characters of code", code.Length);

		return _workerStartInfo is null
			? await ExecuteInProcessAsync(code, ct)
			: await ExecuteInWorkerAsync(code, ct);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		// Waits for a run in progress, which ends within the timeout.
		bool entered = _gate.Wait(Timeout + TimeSpan.FromSeconds(1));
		try
		{
			_disposed = true;
			_worker?.Dispose();
			_worker = null;
		}
		finally
		{
			if (entered)
			{
				_gate.Release();
			}
		}

		foreach (string directory in _temporaryDirectories)
		{
			try
			{
				Directory.Delete(directory, true);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_logger.LogDebug("Could not delete {Directory}: {Message}", directory, ex.Message);
			}
		}
	}

	private async Task<ReplResult> ExecuteInProcessAsync(string code, CancellationToken ct)
	{
		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(Timeout);

		try
		{
			return await ReplScriptRunner.RunAsync(code, _scriptOptions, timeoutCts.Token);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			_logger.LogWarning("REPL: Execution timed out after {Timeout}s", Timeout.TotalSeconds);
			return TimedOut();
		}
	}

	private async Task<ReplResult> ExecuteInWorkerAsync(string code, CancellationToken ct)
	{
		await _gate.WaitAsync(ct);
		try
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			try
			{
				_worker ??= await StartWorkerAsync(ct);
			}
			catch (Exception ex) when (IsStartFailure(ex))
			{
				_logger.LogWarning("REPL: the worker process failed to start: {Message}", ex.Message);
				return new ReplResult { Error = $"The REPL worker process failed to start: {ex.Message}" };
			}

			(ReplResult? result, ReplWorkerStatus status) = await _worker.ExecuteAsync(code, Timeout, ct);
			switch (status)
			{
				case ReplWorkerStatus.Completed:
					return result!;

				case ReplWorkerStatus.TimedOut:
					// Killing the process is the only thing that stops code which never yields. A
					// cancellation token only stopped the wait, and the loop kept running.
					_logger.LogWarning("REPL: Execution timed out after {Timeout}s; restarting the worker",
						Timeout.TotalSeconds);
					_worker.Kill();
					DiscardWorker();
					return TimedOut();

				default:
					int? exitCode = _worker.WaitForExitCode();
					DiscardWorker();
					return new ReplResult
					{
						Error = exitCode is null
							? "The code ended the REPL process."
							: $"The code ended the REPL process (exit code {exitCode})."
					};
			}
		}
		finally
		{
			_gate.Release();
		}
	}

	private Task<ReplWorkerProcess> StartWorkerAsync(CancellationToken ct) =>
		ReplWorkerProcess.StartAsync(_workerStartInfo!(), _referencePaths, StartupTimeout, _logger, ct);

	/// <summary>Adds worker references. The caller holds the gate.</summary>
	private void AddReferences(IEnumerable<string> paths)
	{
		_referencePaths.AddRange(paths);

		// A running worker was configured with the old list; the next run starts a new one.
		DiscardWorker();
	}

	private void DiscardWorker()
	{
		_worker?.Dispose();
		_worker = null;
	}

	private void AddResolvedPackages(NuGetPackageResolver.ResolvedPackages resolved)
	{
		if (resolved.Assemblies.Count > 0)
		{
			_scriptOptions = _scriptOptions.AddReferences(resolved.Assemblies);
		}

		if (resolved.Namespaces.Count > 0)
		{
			_scriptOptions = _scriptOptions.AddImports(resolved.Namespaces);
		}
	}

	private ReplResult TimedOut() =>
		new() { Error = $"Execution timed out after {Timeout.TotalSeconds} seconds." };

	private static bool IsStartFailure(Exception ex) =>
		ex is InvalidOperationException or Win32Exception or IOException;
}

/// <summary>
///     The result of a REPL code execution.
/// </summary>
public sealed class ReplResult
{
	/// <summary>The captured console output (stdout). Null or empty if no output.</summary>
	public string? Output { get; init; }

	/// <summary>Error message if compilation or execution failed. Null if successful.</summary>
	public string? Error { get; init; }
}
