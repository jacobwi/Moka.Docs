using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Engine;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class CleanCommandTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-clean-" + Guid.NewGuid().ToString("N"));

	public CleanCommandTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n");
	}

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

	private static SiteConfig Config(string output) => new()
	{
		Site = new SiteMetadata { Title = "Clean" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = output }
	};

	[Fact]
	public void Clean_DeletesTheOutputDirectoryAndTheCache()
	{
		string page = Path.Combine(_root, "docs", "_site", "guide", "index.html");
		string cacheEntry = Path.Combine(_root, ".mokadocs", "cache", "api-0.json");
		Directory.CreateDirectory(Path.GetDirectoryName(page)!);
		Directory.CreateDirectory(Path.GetDirectoryName(cacheEntry)!);
		File.WriteAllText(page, "<html/>");
		File.WriteAllText(Path.Combine(_root, "docs", "_site", OutputDirectoryGuard.MarkerFileName), "");
		File.WriteAllText(cacheEntry, "{}");

		int exitCode = CleanCommand.Clean(_root, Config("./docs/_site"));

		exitCode.Should().Be(0);
		Directory.Exists(Path.Combine(_root, "docs", "_site")).Should().BeFalse();
		Directory.Exists(Path.Combine(_root, ".mokadocs")).Should().BeFalse();
		File.Exists(Path.Combine(_root, "docs", "index.md")).Should().BeTrue();
	}

	[Theory]
	[InlineData("./docs")]
	[InlineData(".")]
	public void Clean_OutputContainingTheSources_RefusesAndDeletesNothing(string output)
	{
		int exitCode = CleanCommand.Clean(_root, Config(output));

		exitCode.Should().Be(1);
		File.Exists(Path.Combine(_root, "docs", "index.md")).Should().BeTrue();
	}

	[Fact]
	public void Clean_NothingToDelete_Succeeds()
	{
		CleanCommand.Clean(_root, Config("./_site")).Should().Be(0);
	}

	[Fact]
	public void Clean_AFolderMokaDocsDidNotWrite_IsRefused()
	{
		string appFile = Path.Combine(_root, "public", "app.js");
		Directory.CreateDirectory(Path.GetDirectoryName(appFile)!);
		File.WriteAllText(appFile, "hand written");

		CleanCommand.Clean(_root, Config("./public")).Should().Be(1);

		File.Exists(appFile).Should().BeTrue();
	}

	[Fact]
	public void Clean_OutputHoldingAConfiguredProject_IsRefused()
	{
		// output: ./src passed validate, and clean deleted src/ with the project in it.
		string csproj = Path.Combine(_root, "src", "MyLib", "MyLib.csproj");
		Directory.CreateDirectory(Path.GetDirectoryName(csproj)!);
		File.WriteAllText(csproj, "<Project />");
		File.WriteAllText(Path.Combine(_root, "src", OutputDirectoryGuard.MarkerFileName), "");
		SiteConfig config = Config("./src") with
		{
			Content = new ContentConfig { Docs = "./docs", Projects = [new ProjectSource { Path = "src/MyLib/MyLib.csproj" }] }
		};

		CleanCommand.Clean(_root, config).Should().Be(1);

		File.Exists(csproj).Should().BeTrue();
	}
}
