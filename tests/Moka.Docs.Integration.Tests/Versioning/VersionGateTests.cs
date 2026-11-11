using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Versioning;

namespace Moka.Docs.Integration.Tests.Versioning;

/// <summary>
///     A page's <c>version</c> range leaves it out of the versions it doesn't cover: its file, its
///     sidebar entry and its search entries. The key used to be parsed and ignored.
/// </summary>
public sealed class VersionGateTests
{
	private static Dictionary<string, MockFileData> Files() => new()
	{
		["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
		["/project/docs/guide/always.md"] = new("---\ntitle: Always\n---\n# Always\n"),
		["/project/docs/guide/new.md"] = new("---\ntitle: New\nversion: \">=2.0\"\n---\n# New\n"),
		["/project/docs/guide/old.md"] = new("---\ntitle: Old\nversion: 1.x\n---\n# Old\n"),
		["/project/docs/guide/either.md"] = new("---\ntitle: Either\nversion: [\"1.x\", \">=3.0\"]\n---\n# Either\n"),
		["/project/docs/guide/broken.md"] = new("---\ntitle: Broken\nversion: \">=two\"\n---\n# Broken\n")
	};

	private static SiteConfig Config(bool versioning, string label) => MockSite.Config() with
	{
		Features = new FeaturesConfig
		{
			Versioning = new VersioningFeatureConfig
			{
				Enabled = versioning,
				Versions = [new VersionDefinition { Label = label, Default = true }]
			}
		}
	};

	private static async Task<(BuildContext Context, MockFileSystem FileSystem)> BuildAsync(SiteConfig config)
	{
		var fs = new MockFileSystem(Files());
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IFileSystem>(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		await using ServiceProvider provider = services.BuildServiceProvider();
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = MockSite.Root,
			OutputDirectory = MockSite.Root + "/_site"
		};
		new VersionManager(config, NullLogger<VersionManager>.Instance).ApplyTo(context);

		await provider.GetRequiredService<BuildPipeline>().ExecuteAsync(context, TestContext.Current.CancellationToken);
		return (context, fs);
	}

	private static IEnumerable<string> NavRoutes(IEnumerable<NavigationNode> nodes) =>
		nodes.SelectMany(n => new[] { n.Route ?? "" }.Concat(NavRoutes(n.Children)));

	[Fact]
	public async Task PageOutsideTheRange_IsLeftOutOfTheBuildNavAndSearch()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(Config(true, "v1.8"));

		context.Pages.Select(p => p.Route).Should().BeEquivalentTo(
			"/", "/guide/always", "/guide/old", "/guide/either", "/guide/broken");
		fs.File.Exists("/project/_site/guide/new/index.html").Should().BeFalse();
		fs.File.Exists("/project/_site/guide/old/index.html").Should().BeTrue();
		NavRoutes(context.Navigation!.Items).Should().NotContain("/guide/new").And.Contain("/guide/old");
		context.SearchIndex!.Entries.Should().NotContain(e => e.Route.StartsWith("/guide/new", StringComparison.Ordinal));
	}

	[Fact]
	public async Task PageInsideTheRange_IsBuilt()
	{
		(BuildContext context, _) = await BuildAsync(Config(true, "v2.0"));

		context.Pages.Select(p => p.Route).Should().Contain("/guide/new")
			.And.NotContain("/guide/old")
			.And.NotContain("/guide/either");
	}

	[Fact]
	public async Task AListOfRanges_MatchesAnyOfThem()
	{
		(BuildContext v3, _) = await BuildAsync(Config(true, "v3.1"));
		(BuildContext v2, _) = await BuildAsync(Config(true, "v2.5"));

		v3.Pages.Select(p => p.Route).Should().Contain("/guide/either");
		v2.Pages.Select(p => p.Route).Should().NotContain("/guide/either");
	}

	[Fact]
	public async Task InvalidRange_IsAWarningNamingTheFile_AndThePageIsKept()
	{
		(BuildContext context, _) = await BuildAsync(Config(true, "v1.8"));

		context.Pages.Select(p => p.Route).Should().Contain("/guide/broken");
		context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Warning && d.Source == "VersionGate")
			.Which.Message.Should().Contain("broken.md").And.Contain(">=two");
		context.Diagnostics.HasErrors.Should().BeFalse();
	}

	[Fact]
	public async Task VersioningOff_IgnoresTheKey()
	{
		(BuildContext context, _) = await BuildAsync(Config(false, "v1.8"));

		context.CurrentVersion.Should().BeNull();
		context.Pages.Should().HaveCount(6);
		context.Diagnostics.All.Should().NotContain(d => d.Source == "VersionGate");
	}

	[Fact]
	public async Task CurrentVersionWithoutANumber_KeepsEveryPage()
	{
		(BuildContext context, _) = await BuildAsync(Config(true, "next"));

		context.CurrentVersion!.Number.Should().BeNull();
		context.Pages.Should().HaveCount(6);
	}
}
