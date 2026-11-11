using System.Diagnostics;
using System.Globalization;
using Moka.Docs.Serve;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Lets the application's own executable run REPL snippets for
///     <see cref="MokaDocsOptions.EnableRepl" />. Snippets run in a child process that can be killed
///     when one runs too long, and that child is this application started again.
/// </summary>
/// <example>
///     The first line of Program.cs:
///     <code>
///     if (await MokaDocsReplWorker.TryRunAsync()) return;
///     </code>
/// </example>
public static class MokaDocsReplWorker
{
	/// <summary>Set to <c>1</c> on the worker process only.</summary>
	internal const string WorkerVariable = "MOKADOCS_REPL_WORKER";

	/// <summary>The web application's process id, so the worker exits when it does.</summary>
	internal const string ParentProcessIdVariable = "MOKADOCS_REPL_PARENT_PID";

	private static volatile bool _entryPointWired;

	/// <summary>
	///     Whether this process called <see cref="TryRunAsync" />, so starting the executable again
	///     reaches it before the web application starts.
	/// </summary>
	internal static bool IsEntryPointWired
	{
		get => _entryPointWired;
		set => _entryPointWired = value;
	}

	/// <summary>
	///     In a process started as a REPL worker, answers snippet requests on standard input and
	///     output until the web application closes them, then returns <c>true</c>. In any other
	///     process, returns <c>false</c> at once.
	/// </summary>
	/// <remarks>
	///     Call it before anything else in Program.cs, and return when it returns <c>true</c>:
	///     the worker must not build or start the web application, and anything written to
	///     standard output before it runs reaches the protocol stream.
	/// </remarks>
	/// <param name="ct">Stops the worker.</param>
	/// <returns><c>true</c> when this process was a worker and has finished.</returns>
	public static async Task<bool> TryRunAsync(CancellationToken ct = default)
	{
		if (Environment.GetEnvironmentVariable(WorkerVariable) != "1")
		{
			_entryPointWired = true;
			return false;
		}

		int? parentProcessId = int.TryParse(Environment.GetEnvironmentVariable(ParentProcessIdVariable),
			NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
			? pid
			: null;

		Environment.ExitCode = await ReplWorker.RunAsync(
			Console.OpenStandardInput(), Console.OpenStandardOutput(), parentProcessId, ct);
		return true;
	}

	/// <summary>
	///     The command that starts this application again as a worker.
	/// </summary>
	/// <param name="processPath">The running executable, <see cref="Environment.ProcessPath" />.</param>
	/// <param name="entryAssemblyPath">The application's assembly, the entry assembly's location.</param>
	/// <param name="dotnetHostPath">The <c>DOTNET_HOST_PATH</c> variable, or <c>null</c>.</param>
	/// <param name="entryAssemblyName">
	///     The entry assembly's name. A single-file app has one but no assembly location.
	/// </param>
	/// <exception cref="InvalidOperationException">The application's assembly is unknown.</exception>
	internal static ProcessStartInfo CreateStartInfo(string? processPath, string? entryAssemblyPath,
		string? dotnetHostPath, string? entryAssemblyName = null)
	{
		string executable = processPath is null ? "" : WithoutExeSuffix(Path.GetFileName(processPath));
		string? appName = !string.IsNullOrEmpty(entryAssemblyName) ? entryAssemblyName
			: !string.IsNullOrEmpty(entryAssemblyPath) ? Path.GetFileNameWithoutExtension(entryAssemblyPath)
			: null;

		// The app's own apphost (MyApp.exe, or MyApp on Linux and macOS), single-file apps included,
		// which is why this comes before the check for an assembly location.
		if (processPath is not null && appName is not null
		                            && executable.Equals(appName, StringComparison.OrdinalIgnoreCase))
		{
			return new ProcessStartInfo(processPath);
		}

		if (string.IsNullOrEmpty(entryAssemblyPath))
		{
			throw new InvalidOperationException(
				"the application's assembly has no file location. Set MokaDocsOptions.ReplWorkerStartInfo.");
		}

		// `dotnet MyApp.dll`: the host needs the assembly path again.
		if (processPath is not null && executable.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
		{
			return new ProcessStartInfo(processPath) { ArgumentList = { entryAssemblyPath } };
		}

		// Someone else's process hosts the runtime: w3wp for IIS in-process hosting, or a test
		// host. Starting that executable again would not run this app.
		string host = string.IsNullOrEmpty(dotnetHostPath) ? "dotnet" : dotnetHostPath;
		return new ProcessStartInfo(host) { ArgumentList = { entryAssemblyPath } };
	}

	// Path.GetFileNameWithoutExtension would turn the Linux apphost "My.App" into "My".
	private static string WithoutExeSuffix(string fileName) =>
		fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? fileName[..^4] : fileName;
}
