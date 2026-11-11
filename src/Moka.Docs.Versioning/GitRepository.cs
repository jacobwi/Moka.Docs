using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Moka.Docs.Versioning;

internal sealed class GitRepository
{
	private const int _timedOut = -2;
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan _checkoutTimeout = TimeSpan.FromMinutes(10);

	private readonly string _workingDirectory;

	private GitRepository(string workingDirectory, string prefix)
	{
		_workingDirectory = workingDirectory;
		Prefix = prefix;
	}

	// The opened folder relative to the top of the work tree, with "/" separators and no trailing
	// slash; "" at the top. Asked from git rather than computed from paths, which differ when the
	// folder is reached through a symbolic link (/var and /private/var on macOS).
	public string Prefix { get; }

	public static GitRepository? Open(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return null;
		}

		GitResult result = Run(directory, _timeout, "rev-parse", "--show-prefix");
		return result.ExitCode == 0
			? new GitRepository(directory, result.Output.Trim().TrimEnd('/'))
			: null;
	}

	public string? CurrentBranch()
	{
		GitResult result = Run(_workingDirectory, _timeout, "symbolic-ref", "--quiet", "--short", "HEAD");
		return result.ExitCode == 0 && result.Output.Trim() is { Length: > 0 } branch ? branch : null;
	}

	// A branch may exist only as origin/<name>, which is all a CI checkout has of other branches.
	public string? ResolveCommit(VersionRef reference)
	{
		if (reference.Name.Length == 0 || reference.Name.StartsWith('-'))
		{
			return null;
		}

		string[] candidates = reference.IsTag
			? [$"refs/tags/{reference.Name}", reference.Name]
			: [$"refs/heads/{reference.Name}", $"refs/remotes/origin/{reference.Name}", reference.Name];
		foreach (string candidate in candidates)
		{
			GitResult result = Run(_workingDirectory, _timeout, "rev-parse", "--verify", "--quiet", candidate + "^{commit}");
			if (result.ExitCode == 0 && result.Output.Trim() is { Length: > 0 } commit)
			{
				return commit;
			}
		}

		return null;
	}

	public string? ReadFile(string commit, string path)
	{
		GitResult result = Run(_workingDirectory, _timeout, "cat-file", "blob", $"{commit}:{path}");
		return result.ExitCode == 0 ? result.Output : null;
	}

	// Detached, so a branch that is checked out elsewhere can be checked out here too. Hooks are
	// pointed at a folder that doesn't exist: a post-checkout hook in the user's repository has no
	// business running during a docs build.
	public bool TryAddWorktree(string path, string commit, string noHooksDirectory, out string error)
	{
		GitResult result = Run(_workingDirectory, _checkoutTimeout,
			"-c", $"core.hooksPath={noHooksDirectory}", "-c", "advice.detachedHead=false",
			"worktree", "add", "--detach", path, commit);
		error = result.Error.Trim();
		return result.ExitCode == 0;
	}

	public void RemoveWorktree(string path)
	{
		GitResult removed = Run(_workingDirectory, _timeout, "worktree", "remove", "--force", path);
		if (removed.ExitCode == 0 && !Directory.Exists(path))
		{
			return;
		}

		DeleteDirectory(path);
		Run(_workingDirectory, _timeout, "worktree", "prune");
	}

	// git marks object files read-only, which Directory.Delete refuses on Windows.
	public static void DeleteDirectory(string path)
	{
		try
		{
			if (!Directory.Exists(path))
			{
				return;
			}

			foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(file, FileAttributes.Normal);
			}

			Directory.Delete(path, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Left for the OS temp cleaner; the worktree entry is pruned either way.
		}
	}

	private static GitResult Run(string directory, TimeSpan timeout, params string[] arguments)
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

		// Set by git hooks and some tools; they would point git at another repository.
		startInfo.Environment.Remove("GIT_DIR");
		startInfo.Environment.Remove("GIT_WORK_TREE");
		startInfo.Environment.Remove("GIT_INDEX_FILE");

		// Commands such as status refresh the index and take its lock, which a git command the
		// user runs at the same time would fail on.
		startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";
		startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

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
			// Both streams are read while waiting: a full pipe buffer blocks git forever.
			Task<string> output = process.StandardOutput.ReadToEndAsync();
			Task<string> error = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(timeout))
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch (InvalidOperationException)
				{
					// It exited before the kill.
				}

				return new GitResult(_timedOut, "", $"git {arguments[0]} did not finish within {timeout.TotalSeconds:F0}s");
			}

			process.WaitForExit();
			return new GitResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
		}
	}

	private readonly record struct GitResult(int ExitCode, string Output, string Error);
}

internal sealed record VersionRef(string Name, bool IsTag)
{
	public static VersionRef? From(Core.Configuration.VersionDefinition definition)
	{
		if (definition.Tag?.Trim() is { Length: > 0 } tag)
		{
			return new VersionRef(tag, true);
		}

		return definition.Branch?.Trim() is { Length: > 0 } branch ? new VersionRef(branch, false) : null;
	}

	public override string ToString() => IsTag ? $"tag '{Name}'" : $"branch '{Name}'";
}
