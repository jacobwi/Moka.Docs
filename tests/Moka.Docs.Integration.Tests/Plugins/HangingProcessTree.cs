using System.Diagnostics;
using System.Text;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     A command that starts a grandchild which runs for five minutes, and a way to tell when that
///     grandchild is gone. Killing only the direct child leaves the grandchild running, which is
///     what the tests catch.
/// </summary>
/// <remarks>
///     On Windows the grandchild is <c>ping</c> with its output redirected to a file, and the file
///     can be deleted once nothing holds it open. On Linux it is <c>sleep</c>, whose pid the shell
///     writes to a file.
/// </remarks>
internal sealed class HangingProcessTree : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "mokadocs-hang-" + Guid.NewGuid().ToString("N"));

	public HangingProcessTree()
	{
		Directory.CreateDirectory(_directory);
	}

	private string HeldFile => Path.Combine(_directory, "held.txt");

	private string PidFile => Path.Combine(_directory, "grandchild.pid");

	public void Dispose()
	{
		try
		{
			Directory.Delete(_directory, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	/// <summary>A start info that runs the tree directly.</summary>
	public ProcessStartInfo StartInfo()
	{
		if (OperatingSystem.IsWindows())
		{
			return new ProcessStartInfo("cmd.exe", $"/d /c ping -n 300 127.0.0.1 > \"{HeldFile}\"");
		}

		var startInfo = new ProcessStartInfo("/bin/sh");
		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add(ShellScript);
		return startInfo;
	}

	/// <summary>Writes a script that runs the tree and ignores its arguments, and returns its path.</summary>
	public string WriteScript()
	{
		if (OperatingSystem.IsWindows())
		{
			string cmd = Path.Combine(_directory, "hang.cmd");
			File.WriteAllText(cmd, $"@ping -n 300 127.0.0.1 > \"{HeldFile}\"\r\n");
			return cmd;
		}

		string script = Path.Combine(_directory, "hang.sh");
		File.WriteAllText(script, "#!/bin/sh\n" + ShellScript + "\n", new UTF8Encoding(false));
		File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		return script;
	}

	private string ShellScript => $"sleep 300 & echo $! > '{PidFile}'; wait";

	/// <summary>Waits until the grandchild has started, so a kill can't come before it.</summary>
	public async Task WaitUntilStartedAsync(TimeSpan timeout)
	{
		var sw = Stopwatch.StartNew();
		string marker = OperatingSystem.IsWindows() ? HeldFile : PidFile;
		while (!(File.Exists(marker) && (OperatingSystem.IsWindows() || new FileInfo(marker).Length > 0)))
		{
			if (sw.Elapsed > timeout)
			{
				throw new TimeoutException("The hanging process tree didn't start.");
			}

			await Task.Delay(50);
		}
	}

	/// <summary>Whether the grandchild has exited, polling for up to <paramref name="timeout" />.</summary>
	public async Task<bool> GrandchildExitedAsync(TimeSpan timeout)
	{
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			if (GrandchildExited())
			{
				return true;
			}

			await Task.Delay(100);
		}

		return GrandchildExited();
	}

	private bool GrandchildExited()
	{
		if (OperatingSystem.IsWindows())
		{
			try
			{
				File.Delete(HeldFile);
				return true;
			}
			catch (IOException)
			{
				return false;
			}
		}

		if (!File.Exists(PidFile) || !int.TryParse(File.ReadAllText(PidFile).Trim(), out int pid))
		{
			return false;
		}

		string stat = $"/proc/{pid}/stat";
		try
		{
			return !File.Exists(stat) || File.ReadAllText(stat).Contains(") Z ", StringComparison.Ordinal);
		}
		catch (IOException)
		{
			return true;
		}
	}
}
