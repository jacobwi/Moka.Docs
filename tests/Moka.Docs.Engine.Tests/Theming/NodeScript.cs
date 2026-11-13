using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Runs a script under Node for the theme tests that execute parts of main.js. Tests skip when
///     Node is not on PATH.
/// </summary>
internal static class NodeScript
{
	private static readonly Lazy<bool> _available = new(() =>
	{
		try
		{
			using Process? node = Process.Start(new ProcessStartInfo("node", "--version")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false
			});
			return node is not null && node.WaitForExit(10_000) && node.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			return false;
		}
	});

	public static bool Available => _available.Value;

	public static string Run(string script)
	{
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-runtime-" + Guid.NewGuid().ToString("N") + ".js");
		File.WriteAllText(path, script);
		try
		{
			var info = new ProcessStartInfo("node")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};
			info.ArgumentList.Add(path);
			using Process node = Process.Start(info)!;
			Task<string> stdout = node.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
			Task<string> stderr = node.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
			node.WaitForExit(30_000).Should().BeTrue("node should finish");
			node.ExitCode.Should().Be(0, stderr.Result);
			return stdout.Result;
		}
		finally
		{
			File.Delete(path);
		}
	}
}
