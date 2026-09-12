using FluentAssertions;
using Moka.Docs.Cli.Commands;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class ServeCommandTests : IDisposable
{
	private readonly string _project = Path.Combine(Path.GetTempPath(), "mokadocs-serve-" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			Directory.Delete(_project, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private string Build(string configuration, string tfm)
	{
		string path = Path.Combine(_project, "bin", configuration, tfm, "Lib.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllBytes(path, [0]);
		return path;
	}

	[Fact]
	public void FindProjectAssembly_PrefersTheHighestLoadableFramework()
	{
		// The fixed list this replaced only looked in net9.0 and net8.0 folders.
		Build("Release", "net9.0");
		string net10 = Build("Debug", "net10.0");

		ServeCommand.FindProjectAssembly(_project, "Lib", 10).Should().Be(net10);
	}

	[Fact]
	public void FindProjectAssembly_SkipsFrameworksNewerThanTheRuntime()
	{
		string net9 = Build("Release", "net9.0");
		Build("Release", "net10.0");

		ServeCommand.FindProjectAssembly(_project, "Lib", 9).Should().Be(net9);
	}

	[Fact]
	public void FindProjectAssembly_PrefersReleaseForTheSameFramework()
	{
		Build("Debug", "net10.0");
		string release = Build("Release", "net10.0");

		ServeCommand.FindProjectAssembly(_project, "Lib", 10).Should().Be(release);
	}

	[Fact]
	public void FindProjectAssembly_IgnoresNetFrameworkAndFallsBackToNetStandard()
	{
		Build("Release", "net48");
		string standard = Build("Release", "netstandard2.0");

		ServeCommand.FindProjectAssembly(_project, "Lib", 10).Should().Be(standard);
	}

	[Fact]
	public void FindProjectAssembly_NoBuild_ReturnsNull() =>
		ServeCommand.FindProjectAssembly(_project, "Lib", 10).Should().BeNull();
}
