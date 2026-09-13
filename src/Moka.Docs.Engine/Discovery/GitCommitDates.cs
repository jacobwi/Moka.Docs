using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Engine.Discovery;

/// <summary>
///     Reads the date of the last commit that touched each file in a folder, with one
///     <c>git log</c> for the whole folder rather than one process per file.
/// </summary>
/// <remarks>
///     Page dates used to be file modified times. A fresh clone gives every file the time of the
///     checkout, so a site built in CI showed the build date on every page. A shallow clone
///     (the <c>actions/checkout</c> default) only has the newest commit, so every file gets that
///     commit's date; the docs tell CI users to fetch the full history.
/// </remarks>
internal static class GitCommitDates
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	/// <summary>
	///     Returns the last commit date of each committed file under <paramref name="directory" />
	///     that has no uncommitted changes, keyed by its path relative to the directory with
	///     forward slashes.
	/// </summary>
	/// <returns>
	///     An empty map when git is not installed, the directory is not in a work tree, or git
	///     fails. Callers fall back to file times for every file missing from the map.
	/// </returns>
	public static async Task<IReadOnlyDictionary<string, DateTimeOffset>> ReadAsync(
		string directory, ILogger logger, CancellationToken ct)
	{
		// These file systems ignore case, so a file renamed only in case can keep its old
		// spelling in the git index while discovery reports the new one.
		StringComparer comparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
		var dates = new Dictionary<string, DateTimeOffset>(comparer);

		// A file with staged or unstaged edits is newer than its last commit, so it keeps its file
		// time. This command also fails fast when there is no git or no work tree around the folder.
		GitResult changed = await RunAsync(directory, ct,
			"diff", "--name-only", "--relative", "--no-renames", "--no-color", "-z", "HEAD", "--", ".");
		if (changed.ExitCode != 0)
		{
			logger.LogDebug("Using file times for page dates in {Directory}: {Reason}", directory, changed.Error.Trim());
			return dates;
		}

		// log.showSignature would mix signature checks into the output, and log.follow applies
		// --follow to a single path argument such as ".".
		GitResult log = await RunAsync(directory, ct,
			"-c", "log.showSignature=false", "-c", "log.follow=false",
			"log", "--format=%x01%ct", "--name-only", "--relative", "--no-renames", "--no-color", "-z", "--", ".");
		if (log.ExitCode != 0)
		{
			logger.LogDebug("Using file times for page dates in {Directory}: {Reason}", directory, log.Error.Trim());
			return dates;
		}

		var uncommitted = new HashSet<string>(Entries(changed.Output), comparer);

		// With -z, each commit is "\x01<unix time>" followed by its file names, all NUL-separated,
		// with a newline between the header and the first name.
		DateTimeOffset? commitDate = null;
		foreach (string entry in Entries(log.Output))
		{
			if (entry[0] == '\u0001')
			{
				commitDate = long.TryParse(entry.AsSpan(1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
					out long seconds)
					? DateTimeOffset.FromUnixTimeSeconds(seconds)
					: null;
				continue;
			}

			if (commitDate is not { } date || uncommitted.Contains(entry))
			{
				continue;
			}

			if (!dates.TryGetValue(entry, out DateTimeOffset newest) || date > newest)
			{
				dates[entry] = date;
			}
		}

		logger.LogInformation("Read last commit dates for {Count} file(s) in {Directory} from git", dates.Count,
			directory);
		return dates;
	}

	private static IEnumerable<string> Entries(string output) =>
		output.Split('\0').Select(e => e.TrimStart('\n')).Where(e => e.Length > 0);

	private static async Task<GitResult> RunAsync(string directory, CancellationToken ct, params string[] arguments)
	{
		var startInfo = new ProcessStartInfo("git")
		{
			WorkingDirectory = directory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};

		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		// Set by git hooks and some tools; they would point git at that repository instead of the
		// one around the docs folder.
		startInfo.Environment.Remove("GIT_DIR");
		startInfo.Environment.Remove("GIT_WORK_TREE");
		startInfo.Environment.Remove("GIT_INDEX_FILE");

		// git diff refreshes the index and may write it back, taking a lock that a git command
		// the user runs at the same time would fail on.
		startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";

		Process? process;
		try
		{
			process = Process.Start(startInfo);
		}
		catch (Win32Exception ex)
		{
			return new GitResult(-1, "", $"git could not be started ({ex.Message})");
		}

		if (process is null)
		{
			return new GitResult(-1, "", "git could not be started");
		}

		using (process)
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(_timeout);
			try
			{
				// Both streams are read while waiting: a full pipe buffer blocks git forever.
				Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
				Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
				await process.WaitForExitAsync(timeout.Token);
				return new GitResult(process.ExitCode, await output, await error);
			}
			catch (OperationCanceledException)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch (InvalidOperationException)
				{
					// It exited between the cancellation and the kill.
				}

				ct.ThrowIfCancellationRequested();
				return new GitResult(-1, "", $"git did not finish within {_timeout.TotalSeconds:F0}s");
			}
		}
	}

	private readonly record struct GitResult(int ExitCode, string Output, string Error);
}
