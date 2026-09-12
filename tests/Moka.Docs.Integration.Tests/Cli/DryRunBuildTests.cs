using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Runs the real dry-run path against a project on disk. <c>validate</c> and
///     <c>doctor</c> both promise to leave the output directory alone, and the output phase
///     deletes that directory on a normal build, so this is the property worth guarding.
/// </summary>
public sealed class DryRunBuildTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-dryrun-" + Guid.NewGuid().ToString("N"));

	public DryRunBuildTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs", "guide"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nSee [guide](/guide/intro).\n");
		File.WriteAllText(Path.Combine(_root, "docs", "guide", "intro.md"), "---\ntitle: Intro\n---\n\n# Intro\n");
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

	private SiteConfig Config(string output = "./_site") => new()
	{
		Site = new SiteMetadata { Title = "Dry Run" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = output, Cache = false }
	};

	[Fact]
	public async Task RunAsync_ProducesPagesWithoutWritingOutput()
	{
		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.Pages.Select(p => p.Route).Should().BeEquivalentTo(["/", "/guide/intro"]);
		Directory.Exists(Path.Combine(_root, "_site")).Should().BeFalse();
	}

	[Fact]
	public async Task RunAsync_LeavesAnExistingSiteUntouched()
	{
		// A normal build cleans the output directory first. A dry run must not.
		string site = Path.Combine(_root, "_site");
		Directory.CreateDirectory(site);
		string sentinel = Path.Combine(site, "keep-me.html");
		File.WriteAllText(sentinel, "previous build");

		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		File.Exists(sentinel).Should().BeTrue();
		File.ReadAllText(sentinel).Should().Be("previous build");
		Directory.GetFiles(site).Should().ContainSingle();
	}

	[Fact]
	public async Task RunAsync_ReportsTheRegisteredPluginIds()
	{
		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);

		outcome.RegisteredPluginIds.Should().Contain(["mokadocs-repl", "mokadocs-blazor-preview", "openapi"]);
	}
}
