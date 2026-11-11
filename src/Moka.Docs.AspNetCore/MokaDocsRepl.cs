using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Serve;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     The REPL behind <c>POST {BasePath}/api/repl/execute</c>: one <see cref="ReplExecutionService" />
///     that runs snippets in a worker process, created on first use and disposed with the app.
/// </summary>
/// <remarks>
///     The worker starts on the first run, not at startup like <c>mokadocs serve</c>'s: the docs
///     are one part of a web app, and most runs of it never execute a snippet.
/// </remarks>
internal sealed class MokaDocsRepl : IDisposable
{
	/// <summary>The message the endpoint returns when the worker can't be started.</summary>
	internal const string NotWiredMessage =
		"The REPL can't run code yet. Add `if (await MokaDocsReplWorker.TryRunAsync()) return;` " +
		"as the first line of Program.cs and restart the app.";

	private readonly ILogger<MokaDocsRepl> _logger;
	private readonly MokaDocsOptions _options;
	private readonly Lazy<ReplExecutionService> _service;
	private int _warnedNotWired;

	public MokaDocsRepl(MokaDocsOptions options, ILoggerFactory loggerFactory)
	{
		_options = options;
		_logger = loggerFactory.CreateLogger<MokaDocsRepl>();
		_service = new Lazy<ReplExecutionService>(
			() => CreateService(loggerFactory.CreateLogger<ReplExecutionService>()),
			LazyThreadSafetyMode.ExecutionAndPublication);
	}

	/// <summary>Set by <c>MapMokaDocs</c> when it maps the endpoint.</summary>
	internal bool IsMapped { get; set; }

	/// <summary>Whether the service exists, which means a worker may have been started.</summary>
	internal bool IsServiceCreated => _service.IsValueCreated;

	/// <summary>
	///     Whether a worker can be started. Without <see cref="MokaDocsReplWorker.TryRunAsync" /> in
	///     Program.cs, starting the executable again would start a second copy of the web app.
	/// </summary>
	/// <summary>Whether requests from other machines may run code (<see cref="MokaDocsOptions.AllowRemoteRepl" />).</summary>
	internal bool AllowRemote => _options.AllowRemoteRepl;

	internal bool CanStartWorker => _options.ReplWorkerStartInfo is not null || MokaDocsReplWorker.IsEntryPointWired;

	/// <summary>The service. Creating it starts nothing; the first run does.</summary>
	internal ReplExecutionService Service => _service.Value;

	/// <inheritdoc />
	public void Dispose()
	{
		if (_service.IsValueCreated)
		{
			_service.Value.Dispose();
		}
	}

	/// <summary>Logs, once per app, that Program.cs doesn't call <see cref="MokaDocsReplWorker.TryRunAsync" />.</summary>
	internal void WarnNotWiredOnce()
	{
		if (Interlocked.Exchange(ref _warnedNotWired, 1) == 0)
		{
			_logger.LogWarning(
				"EnableRepl is on, but snippets can't run: Program.cs doesn't call MokaDocsReplWorker.TryRunAsync(). " +
				"Add `if (await MokaDocsReplWorker.TryRunAsync()) return;` as its first line, " +
				"or set MokaDocsOptions.ReplWorkerStartInfo");
		}
	}

	/// <summary>
	///     The worker's command: <see cref="MokaDocsOptions.ReplWorkerStartInfo" />, or this
	///     application started again, with the variables that make it a worker.
	/// </summary>
	internal ProcessStartInfo CreateWorkerStartInfo()
	{
		ProcessStartInfo startInfo = _options.ReplWorkerStartInfo?.Invoke()
		                             ?? MokaDocsReplWorker.CreateStartInfo(
			                             Environment.ProcessPath,
			                             Assembly.GetEntryAssembly()?.Location,
			                             Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"));

		// Set on the child's start info only, so the web app itself never turns into a worker.
		startInfo.Environment[MokaDocsReplWorker.WorkerVariable] = "1";
		startInfo.Environment[MokaDocsReplWorker.ParentProcessIdVariable] =
			Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
		return startInfo;
	}

	private ReplExecutionService CreateService(ILogger<ReplExecutionService> logger)
	{
		var service = new ReplExecutionService(logger, CreateWorkerStartInfo);

		// Snippets can use the documented types.
		foreach (Assembly assembly in _options.Assemblies)
		{
			if (!string.IsNullOrEmpty(assembly.Location))
			{
				service.LoadProjectAssembly(assembly.Location);
			}
		}

		return service;
	}
}
