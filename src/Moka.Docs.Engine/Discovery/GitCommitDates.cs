using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;

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
	private const int _timedOut = -2;
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	/// <summary>
	///     Returns the last commit date of each committed file under <paramref name="directory" />
	///     that has no uncommitted changes, keyed by its path relative to the directory with
	///     forward slashes.
	/// </summary>
	/// <param name="directory">The folder whose files are dated.</param>
	/// <param name="logger">Receives why git could not be used.</param>
	/// <param name="ct">Cancels the git processes.</param>
	/// <param name="wanted">
	///     The files the caller needs, relative to the directory with forward slashes. The log is
	///     read as git writes it and reading stops once each of them has a date, so a long history
	///     isn't read to the end. With <c>null</c> the whole log is read.
	/// </param>
	/// <returns>
	///     An empty map when git is not installed, the directory is not in a work tree, or git
	///     fails. Callers fall back to file times for every file missing from the map.
	/// </returns>
	public static async Task<IReadOnlyDictionary<string, DateTimeOffset>> ReadAsync(
		string directory, ILogger logger, CancellationToken ct, IReadOnlyCollection<string>? wanted = null) =>
		(await ReadHistoryAsync(directory, logger, ct, wanted, false)).Dates;

	/// <summary>
	///     Reads the last commit dates like <see cref="ReadAsync" /> and, with
	///     <paramref name="includeAuthors" />, every author of each file from the same <c>git log</c>.
	/// </summary>
	/// <param name="directory">The folder whose files are read.</param>
	/// <param name="logger">Receives why git could not be used.</param>
	/// <param name="ct">Cancels the git processes.</param>
	/// <param name="wanted">
	///     The files the caller needs. Without authors, reading stops once each has a date; with
	///     authors the whole log is read, since the first author of a file is in its oldest commit.
	/// </param>
	/// <param name="includeAuthors">Whether to collect the authors.</param>
	/// <returns>
	///     Dates as <see cref="ReadAsync" /> returns them, and the authors of each file, most commits
	///     first. Names and emails have the repository's <c>.mailmap</c> applied, and commits with the
	///     same email (ignoring case) count as one author. A file with uncommitted changes has no date
	///     but keeps its authors.
	/// </returns>
	public static async Task<GitFileHistory> ReadHistoryAsync(string directory, ILogger logger, CancellationToken ct,
		IReadOnlyCollection<string>? wanted, bool includeAuthors)
	{
		// These file systems ignore case, so a file renamed only in case can keep its old
		// spelling in the git index while discovery reports the new one.
		StringComparer comparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
		var dates = new Dictionary<string, DateTimeOffset>(comparer);
		var authors = new Dictionary<string, Dictionary<string, AuthorTally>>(comparer);
		HashSet<string>? authorFiles = includeAuthors && wanted is not null ? new HashSet<string>(wanted, comparer) : null;

		// A file with staged or unstaged edits is newer than its last commit, so it keeps its file
		// time. This command also fails fast when there is no git or no work tree around the folder.
		var uncommitted = new HashSet<string>(comparer);
		GitResult changed = await StreamAsync(directory, entry =>
			{
				uncommitted.Add(entry);
				return true;
			}, ct,
			"diff", "--name-only", "--relative", "--no-renames", "--no-color", "-z", "HEAD", "--", ".");
		if (changed.ExitCode != 0)
		{
			logger.LogDebug("Using file times for page dates in {Directory}: {Reason}", directory, changed.Error.Trim());
			return GitFileHistory.Empty;
		}

		HashSet<string>? undated = wanted is null
			? null
			: new HashSet<string>(wanted.Where(w => !uncommitted.Contains(w)), comparer);
		if (undated is { Count: 0 } && !includeAuthors)
		{
			return new GitFileHistory(dates, new Dictionary<string, IReadOnlyList<PageContributor>>());
		}

		// With -z, each commit is "\x01<unix time>" (plus "\x02<name>\x02<email>" with authors)
		// followed by its file names, all NUL-separated, with a newline between the header and
		// the first name.
		DateTimeOffset? commitDate = null;
		string? authorName = null;
		string authorEmail = "";

		bool OnLogEntry(string entry)
		{
			if (entry[0] == '\u0001')
			{
				string[] header = entry[1..].Split('\u0002');
				commitDate = long.TryParse(header[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
					out long seconds)
					? DateTimeOffset.FromUnixTimeSeconds(seconds)
					: null;
				authorName = header.Length > 1 ? header[1].Trim() : null;
				authorEmail = header.Length > 2 ? header[2].Trim() : "";
				return true;
			}

			if (includeAuthors && !string.IsNullOrEmpty(authorName)
			                   && (authorFiles is null || authorFiles.Contains(entry)))
			{
				AddAuthor(authors, entry, authorName, authorEmail);
			}

			if (commitDate is not { } date || uncommitted.Contains(entry))
			{
				return true;
			}

			if (!dates.TryGetValue(entry, out DateTimeOffset newest) || date > newest)
			{
				dates[entry] = date;
			}

			return includeAuthors || undated is null || !undated.Remove(entry) || undated.Count > 0;
		}

		// log.showSignature would mix signature checks into the output, and log.follow applies
		// --follow to a single path argument such as ".". %aN and %aE are the name and email
		// after .mailmap.
		string format = includeAuthors ? "--format=%x01%ct%x02%aN%x02%aE" : "--format=%x01%ct";
		GitResult log = await StreamAsync(directory, OnLogEntry, ct,
			"-c", "log.showSignature=false", "-c", "log.follow=false",
			"log", format, "--name-only", "--relative", "--no-renames", "--no-color", "-z", "--", ".");
		if (log.ExitCode != 0)
		{
			logger.LogDebug("Using file times for page dates in {Directory}: {Reason}", directory, log.Error.Trim());

			// The dates read before a timeout are right, and the authors are the newest ones; anything
			// else may be half a failed run.
			if (log.ExitCode != _timedOut)
			{
				dates.Clear();
				authors.Clear();
			}
		}

		logger.LogInformation("Read last commit dates for {Count} file(s) in {Directory} from git", dates.Count,
			directory);
		return new GitFileHistory(dates, authors.ToDictionary(
			a => a.Key,
			a => (IReadOnlyList<PageContributor>)a.Value.Values
				.OrderByDescending(t => t.Commits)
				.ThenBy(t => t.FirstSeen)
				.Select(t => new PageContributor { Name = t.Name, Email = t.Email, Commits = t.Commits })
				.ToList(),
			comparer));
	}

	private static void AddAuthor(Dictionary<string, Dictionary<string, AuthorTally>> authors, string file,
		string name, string email)
	{
		if (!authors.TryGetValue(file, out Dictionary<string, AuthorTally>? tallies))
		{
			tallies = new Dictionary<string, AuthorTally>(StringComparer.Ordinal);
			authors[file] = tallies;
		}

		string identity = email.Length > 0 ? email.ToLowerInvariant() : "\u0000" + name;
		if (tallies.TryGetValue(identity, out AuthorTally? tally))
		{
			tally.Commits++;
		}
		else
		{
			// The log is newest first, so the name kept is the one of the newest commit.
			tallies[identity] = new AuthorTally(name, email, tallies.Count);
		}
	}

	/// <summary>
	///     Runs git and hands each NUL-separated entry of its output to <paramref name="onEntry" />
	///     as it arrives. When the callback returns <c>false</c>, git is stopped and the run counts
	///     as a success.
	/// </summary>
	private static async Task<GitResult> StreamAsync(string directory, Func<string, bool> onEntry,
		CancellationToken ct, params string[] arguments)
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
			return new GitResult(-1, $"git could not be started ({ex.Message})");
		}

		if (process is null)
		{
			return new GitResult(-1, "git could not be started");
		}

		using (process)
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(_timeout);

			// Both streams are read while waiting: a full pipe buffer blocks git forever.
			Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
			try
			{
				if (!await ReadEntriesAsync(process.StandardOutput, onEntry, timeout.Token))
				{
					Kill(process);
					await process.WaitForExitAsync(CancellationToken.None);
					return new GitResult(0, "");
				}

				await process.WaitForExitAsync(timeout.Token);
				return new GitResult(process.ExitCode, await error);
			}
			catch (OperationCanceledException)
			{
				Kill(process);
				ct.ThrowIfCancellationRequested();
				return new GitResult(_timedOut, $"git did not finish within {_timeout.TotalSeconds:F0}s");
			}
		}
	}

	/// <returns><c>false</c> when the callback asked to stop before the end of the output.</returns>
	private static async Task<bool> ReadEntriesAsync(StreamReader reader, Func<string, bool> onEntry,
		CancellationToken ct)
	{
		char[] buffer = new char[16 * 1024];
		var entry = new StringBuilder();
		int read;
		while ((read = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
		{
			for (int i = 0; i < read; i++)
			{
				if (buffer[i] != '\0')
				{
					entry.Append(buffer[i]);
				}
				else if (!Deliver(entry, onEntry))
				{
					return false;
				}
			}
		}

		return Deliver(entry, onEntry);
	}

	private static bool Deliver(StringBuilder entry, Func<string, bool> onEntry)
	{
		string text = entry.ToString().TrimStart('\n');
		entry.Clear();
		return text.Length == 0 || onEntry(text);
	}

	private static void Kill(Process process)
	{
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch (InvalidOperationException)
		{
			// It exited before the kill.
		}
	}

	private readonly record struct GitResult(int ExitCode, string Error);

	private sealed class AuthorTally(string name, string email, int firstSeen)
	{
		public string Name { get; } = name;
		public string Email { get; } = email;
		public int FirstSeen { get; } = firstSeen;
		public int Commits { get; set; } = 1;
	}
}

/// <summary>
///     What <see cref="GitCommitDates.ReadHistoryAsync" /> read, keyed by file path relative to the
///     folder with forward slashes.
/// </summary>
/// <param name="Dates">The last commit date of each committed file without uncommitted changes.</param>
/// <param name="Authors">The authors of each file, most commits first; empty unless asked for.</param>
internal sealed record GitFileHistory(
	IReadOnlyDictionary<string, DateTimeOffset> Dates,
	IReadOnlyDictionary<string, IReadOnlyList<PageContributor>> Authors)
{
	public static GitFileHistory Empty { get; } = new(
		new Dictionary<string, DateTimeOffset>(), new Dictionary<string, IReadOnlyList<PageContributor>>());
}
