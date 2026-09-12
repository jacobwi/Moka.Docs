using FluentAssertions;
using Moka.Docs.Cli;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class ConfigPathTests : IDisposable
{
	private readonly string _dir = Path.Combine(Path.GetTempPath(), "mokadocs-configpath-" + Guid.NewGuid().ToString("N"));

	public ConfigPathTests() => Directory.CreateDirectory(_dir);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_dir, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public void Resolve_OnlyYmlExists_UsesIt()
	{
		File.WriteAllText(Path.Combine(_dir, "mokadocs.yml"), "");

		ConfigPath.Resolve(null, _dir).Should().Be(Path.Combine(_dir, "mokadocs.yml"));
	}

	[Fact]
	public void Resolve_BothExist_PrefersYaml()
	{
		File.WriteAllText(Path.Combine(_dir, "mokadocs.yml"), "");
		File.WriteAllText(Path.Combine(_dir, "mokadocs.yaml"), "");

		ConfigPath.Resolve(null, _dir).Should().Be(Path.Combine(_dir, "mokadocs.yaml"));
	}

	[Fact]
	public void Resolve_NeitherExists_NamesTheYamlFileForTheErrorMessage() =>
		ConfigPath.Resolve(null, _dir).Should().Be(Path.Combine(_dir, "mokadocs.yaml"));

	[Fact]
	public void Resolve_ExplicitOption_WinsEvenWhenADefaultFileExists()
	{
		File.WriteAllText(Path.Combine(_dir, "mokadocs.yaml"), "");
		string explicitPath = Path.Combine(_dir, "sub", "site.yaml");

		ConfigPath.Resolve(explicitPath, _dir).Should().Be(explicitPath);
	}
}
