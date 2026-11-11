using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Moka.Docs.Integration.Tests.Versioning;

/// <summary>
///     A git repository in a temp folder, isolated from the machine's git configuration. Deleted on
///     dispose.
/// </summary>
internal sealed class TempGitRepo : IDisposable
{
	private static readonly Lazy<bool> _gitAvailable = new(() =>
	{
		try
		{
			using Process? process = Process.Start(new ProcessStartInfo("git", "--version")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			return process is not null && process.WaitForExit(10_000) && process.ExitCode == 0;
		}
		catch (Exception)
		{
			return false;
		}
	});

	public TempGitRepo()
	{
		Root = Path.Combine(Path.GetTempPath(), "mokadocs-vtest-" + Guid.NewGuid().ToString("N")[..12]);
		Directory.CreateDirectory(Root);
		Directory.CreateDirectory(WorkDirectory);
		File.WriteAllText(Root + ".gitconfig", "");
		Git("init", "-q", "-b", "main");
	}

	public static bool GitAvailable => _gitAvailable.Value;

	public string Root { get; }

	// Next to the repository, for the versioned build's checkouts, so a test can see what is left.
	public string WorkDirectory => Root + "-work";

	public string FullPath(string relativePath) => Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

	public void Write(string relativePath, string content)
	{
		string path = FullPath(relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	public void Delete(string relativePath) => File.Delete(FullPath(relativePath));

	public void Commit(string message)
	{
		Git("add", "-A");
		Git("commit", "-q", "-m", message);
	}

	public string IndexHash()
	{
		using var stream = File.OpenRead(Path.Combine(Root, ".git", "index"));
		return Convert.ToHexString(SHA256.HashData(stream));
	}

	public string Git(params string[] arguments)
	{
		var startInfo = new ProcessStartInfo("git")
		{
			WorkingDirectory = Root,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8
		};

		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		// The fixture must not pick up the machine's git configuration, such as commit signing or
		// hooks, or an outer repository from a hook environment.
		startInfo.Environment["GIT_CONFIG_GLOBAL"] = Root + ".gitconfig";
		startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
		startInfo.Environment.Remove("GIT_DIR");
		startInfo.Environment.Remove("GIT_WORK_TREE");
		startInfo.Environment.Remove("GIT_INDEX_FILE");
		startInfo.Environment["GIT_AUTHOR_NAME"] = "MokaDocs Tests";
		startInfo.Environment["GIT_AUTHOR_EMAIL"] = "tests@mokadocs.invalid";
		startInfo.Environment["GIT_COMMITTER_NAME"] = "MokaDocs Tests";
		startInfo.Environment["GIT_COMMITTER_EMAIL"] = "tests@mokadocs.invalid";

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("git did not start");
		Task<string> output = process.StandardOutput.ReadToEndAsync();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {error}");
		}

		return output.GetAwaiter().GetResult();
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(Root))
			{
				// git marks its object files read-only, which Directory.Delete refuses on Windows.
				foreach (string file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
				{
					File.SetAttributes(file, FileAttributes.Normal);
				}

				Directory.Delete(Root, true);
			}

			if (Directory.Exists(WorkDirectory))
			{
				Directory.Delete(WorkDirectory, true);
			}

			File.Delete(Root + ".gitconfig");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}
}
