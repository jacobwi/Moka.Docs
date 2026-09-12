using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Runs <c>mokadocs stats</c> counting against a real dry run. The previous version read
///     compiled XML doc files from bin/, which the build never uses, so a project without one
///     reported no API and zero generated pages.
/// </summary>
public sealed class ProjectStatsTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-stats-" + Guid.NewGuid().ToString("N"));

	public ProjectStatsTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\none two three\n");
		Write("docs/guide/intro.md", "---\r\ntitle: Intro\r\n---\r\nfour five\r\n");
		Write("docs/logo.png", "0123456789");

		// A previous build inside the docs folder is output, not source.
		Write("docs/_site/stale.md", "---\ntitle: Stale\n---\nsix seven eight nine\n");
		Write("docs/_site/bundle.js", new string('x', 5000));

		Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		Write("src/Lib/Widget.cs", """
		                           namespace Lib;

		                           /// <summary>A widget.</summary>
		                           public class Widget
		                           {
		                               /// <summary>Spins it.</summary>
		                               public void Spin() { }

		                               public int Size { get; set; }
		                           }
		                           """);
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

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	private SiteConfig Config() => new()
	{
		Site = new SiteMetadata { Title = "Stats" },
		Content = new ContentConfig
		{
			Docs = "./docs",
			Projects = [new ProjectSource { Path = "./src/Lib/Lib.csproj" }]
		},
		Build = new BuildConfig { Output = "./docs/_site", Cache = false }
	};

	private async Task<ProjectStats> RunAsync()
	{
		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		return ProjectStats.From(outcome, Path.Combine(_root, "docs"));
	}

	[Fact]
	public async Task From_CountsSourcePagesAndFilesButNotTheBuiltSite()
	{
		ProjectStats stats = await RunAsync();

		stats.MarkdownPages.Should().Be(2);
		stats.WordCount.Should().Be(5);
		stats.DocsFiles.Should().Be(3);

		long expectedBytes = new[] { "docs/index.md", "docs/guide/intro.md", "docs/logo.png" }
			.Sum(p => new FileInfo(Path.Combine(_root, p)).Length);
		stats.DocsBytes.Should().Be(expectedBytes);
	}

	[Fact]
	public async Task From_ReadsTheApiModelTheBuildGenerates()
	{
		ProjectStats stats = await RunAsync();

		stats.ApiTypes.Should().Be(1);
		stats.Namespaces.Should().Be(1);
		stats.GeneratedPages.Should().BeGreaterThan(0);
		stats.Coverage.Should().NotBeNull();
		stats.Coverage!.Value.Total.Should().Be(stats.ApiTypes + stats.ApiMembers);
		stats.Coverage.Value.Missing.Should().Contain("Lib.Widget.Size").And.NotContain("Lib.Widget.Spin");
	}

	[Fact]
	public async Task ToJsonShape_KeepsTheKeysEarlierVersionsPrinted()
	{
		// CI scripts parse these names, so renaming one is a breaking change.
		ProjectStats stats = await RunAsync();

		stats.ToJsonShape().Keys.Should().Equal(
			"Markdown Pages", "Word Count", "Generated Pages", "Total Pages",
			"API Types", "API Members", "Namespaces", "XML Doc Coverage",
			"Plugins", "Search Enabled", "Search Entries", "Docs File Count", "Docs Size");
	}

	[Theory]
	[InlineData("one two", 2)]
	[InlineData("---\ntitle: A B C\n---\none two", 2)]
	[InlineData("---\r\ntitle: A B C\r\n---\r\none", 1)]
	[InlineData("---\ntitle: never closed\none", 5)]
	[InlineData("", 0)]
	public void CountWords_SkipsOnlyALeadingFrontMatterBlock(string markdown, long expected) =>
		ProjectStats.CountWords(markdown).Should().Be(expected);
}
