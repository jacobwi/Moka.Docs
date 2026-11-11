using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class InitCommandTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-init-" + Guid.NewGuid().ToString("N"));

	public InitCommandTests() => Directory.CreateDirectory(_root);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public void Scaffold_EmptyFolder_CreatesAConfigThatLoadsAndAHomePage()
	{
		List<(string File, bool Created)> results = InitCommand.Scaffold(_root);

		results.Should().Equal(("mokadocs.yaml", true), ("docs/index.md", true));
		SiteConfigReader.Parse(File.ReadAllText(Path.Combine(_root, "mokadocs.yaml"))).Warnings.Should().BeEmpty();
		File.ReadAllText(Path.Combine(_root, "docs", "index.md")).Should().Contain("title: Welcome");
	}

	[Fact]
	public void Scaffold_ExistingHomePage_IsLeftAlone()
	{
		// init wrote docs/index.md whenever mokadocs.yaml was missing, replacing the user's page.
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "my own home page");

		List<(string File, bool Created)> results = InitCommand.Scaffold(_root);

		results.Should().Equal(("mokadocs.yaml", true), ("docs/index.md", false));
		File.ReadAllText(Path.Combine(_root, "docs", "index.md")).Should().Be("my own home page");
	}

	[Theory]
	[InlineData("mokadocs.yaml")]
	[InlineData("mokadocs.yml")]
	public void Scaffold_ExistingConfig_IsLeftAlone(string configName)
	{
		File.WriteAllText(Path.Combine(_root, configName), "site:\n  title: Mine\n");

		List<(string File, bool Created)> results = InitCommand.Scaffold(_root);

		results.Should().Contain((configName, false));
		File.ReadAllText(Path.Combine(_root, configName)).Should().Be("site:\n  title: Mine\n");
		File.Exists(Path.Combine(_root, configName == "mokadocs.yml" ? "mokadocs.yaml" : "mokadocs.yml"))
			.Should().BeFalse();
	}

	[Fact]
	public void Scaffold_RunTwice_SkipsEverythingTheSecondTime()
	{
		InitCommand.Scaffold(_root);

		InitCommand.Scaffold(_root).Should().OnlyContain(r => !r.Created);
	}
}
