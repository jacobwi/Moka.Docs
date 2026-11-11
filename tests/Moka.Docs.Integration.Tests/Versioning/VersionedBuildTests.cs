using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Versioning;

namespace Moka.Docs.Integration.Tests.Versioning;

/// <summary>
///     Builds the library repository from <see cref="VersionedSites" /> once: "next" from the working
///     tree on main, and v2.0 and v1.0 from their tags.
/// </summary>
public sealed class VersionedSiteFixture : IAsyncLifetime
{
	internal TempGitRepo? Repo { get; private set; }

	public VersionedBuildResult? Result { get; private set; }

	public string StatusBefore { get; private set; } = "";

	public string StatusAfter { get; private set; } = "";

	public string IndexBefore { get; private set; } = "";

	public string IndexAfter { get; private set; } = "";

	public List<string> WorkLeftovers { get; private set; } = [];

	public string Worktrees { get; private set; } = "";

	public async ValueTask InitializeAsync()
	{
		if (!TempGitRepo.GitAvailable)
		{
			return;
		}

		Repo = VersionedSites.CreateLibraryRepo();
		StatusBefore = Repo.Git("-c", "core.quotePath=false", "status", "--porcelain", "--untracked-files=all");
		IndexBefore = Repo.IndexHash();

		Result = await VersionedSites.BuildAsync(VersionedSites.Request(Repo));

		IndexAfter = Repo.IndexHash();
		StatusAfter = Repo.Git("-c", "core.quotePath=false", "status", "--porcelain", "--untracked-files=all");
		WorkLeftovers = Directory.EnumerateFileSystemEntries(Repo.WorkDirectory).ToList();
		Worktrees = Repo.Git("worktree", "list", "--porcelain");
	}

	public ValueTask DisposeAsync()
	{
		Repo?.Dispose();
		return ValueTask.CompletedTask;
	}
}

/// <summary>
///     <c>features.versioning.strategy</c> and <c>versions[].branch</c> were parsed and read by
///     nothing: one build made one site. A build now checks out each version's ref and builds it into
///     its own folder, with a dropdown that keeps the reader on the same page.
/// </summary>
[Collection(VersionedBuildCollection.Name)]
public sealed class VersionedBuildTests(VersionedSiteFixture site) : IClassFixture<VersionedSiteFixture>
{
	private TempGitRepo Repo
	{
		get
		{
			Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
			return site.Repo!;
		}
	}

	private string Read(string sitePath) => VersionedSites.Read(Repo, sitePath);

	private bool Exists(string sitePath) => VersionedSites.Exists(Repo, sitePath);

	private static Dictionary<string, string> DropdownLinks(string html) =>
		Regex.Matches(html, "<a class=\"version-option[^\"]*\" href=\"([^\"]*)\" data-version-slug=\"([^\"]*)\"")
			.ToDictionary(m => WebUtility.HtmlDecode(m.Groups[2].Value), m => WebUtility.HtmlDecode(m.Groups[1].Value));

	[Fact]
	public void EveryVersion_IsBuiltIntoItsFolder_AndTheDefaultAtTheRootToo()
	{
		_ = Repo;
		site.Result!.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
		site.Result.Builds.Select(b => (b.Version.Label, b.IsSiteRoot, b.Succeeded)).Should().Equal(
			("v2.0", true, true), ("next", false, true), ("v2.0", false, true), ("v1.0", false, true));

		Read("index.html").Should().Contain("Home of version two");
		Read("v2.0/index.html").Should().Contain("Home of version two");
		Read("v1.0/index.html").Should().Contain("Home of version one");
		Read("next/index.html").Should().Contain("Home of version two");
		Exists("v1.0/guide/legacy/index.html").Should().BeTrue();
		Exists("v2.0/guide/legacy/index.html").Should().BeFalse();
		Exists("v1.0/guide/new/index.html").Should().BeFalse();
		Exists("guide/new/index.html").Should().BeTrue();
		Exists("v1.0/api/lib/widget/index.html").Should().BeTrue();

		foreach (string folder in new[] { "", "v1.0/", "v2.0/", "next/" })
		{
			Exists(folder + ".mokadocs-output").Should().BeTrue(folder);
			Exists(folder + "_theme/css/main.css").Should().BeTrue(folder);
		}
	}

	[Fact]
	public void LabelsComeFromEachRefsPackageVersion_AndAVersionFromARefUsesItsOwnConfig()
	{
		Read("v1.0/guide/intro/index.html").Should().Contain("<span class=\"version-label\">v1.0</span>")
			.And.Contain("Lib Docs (old)");
		Read("v2.0/guide/intro/index.html").Should().Contain("<span class=\"version-label\">v2.0</span>")
			.And.NotContain("Lib Docs (old)");
		Read("v1.0/guide/intro/index.html").Should().Contain("data-base-path=\"/v1.0\"")
			.And.Contain("href=\"/v1.0/_theme/css/main.css?v=");
	}

	[Fact]
	public void TheCheckedOutBranch_IsBuiltFromTheWorkingTree()
	{
		Read("next/guide/intro/index.html").Should().Contain("Intro work in progress");
		Exists("next/guide/scratch/index.html").Should().BeTrue();
		Read("v2.0/guide/intro/index.html").Should().Contain("Intro two").And.NotContain("work in progress");
	}

	[Fact]
	public void DropdownLinks_KeepThePage_OrFallBackToTheVersionHome()
	{
		DropdownLinks(Read("v1.0/guide/intro/index.html")).Should().Equal(new Dictionary<string, string>
		{
			["next"] = "/next/guide/intro",
			["v2.0"] = "/guide/intro",
			["v1.0"] = "/v1.0/guide/intro"
		});

		Dictionary<string, string> fromLegacy = DropdownLinks(Read("v1.0/guide/legacy/index.html"));
		fromLegacy["v2.0"].Should().Be("/");
		fromLegacy["next"].Should().Be("/next/");

		Dictionary<string, string> fromRoot = DropdownLinks(Read("guide/new/index.html"));
		fromRoot["v1.0"].Should().Be("/v1.0/");
		fromRoot["next"].Should().Be("/next/guide/new");
		fromRoot["v2.0"].Should().Be("/guide/new");
	}

	[Fact]
	public void FrontMatterRanges_AreMatchedAgainstEachVersion()
	{
		// future.md is for 2.1 and later: main is 2.1.0 (its label "next" has no number of its own).
		Exists("next/guide/future/index.html").Should().BeTrue();
		Exists("v2.0/guide/future/index.html").Should().BeFalse();
		Exists("guide/future/index.html").Should().BeFalse();
	}

	[Fact]
	public void Search_IsPerVersion()
	{
		string v1 = Read("v1.0/search-index.json");
		v1.Should().Contain("\"/v1.0/guide/legacy\"").And.NotContain("guide/new");

		string root = Read("search-index.json");
		root.Should().Contain("\"/guide/new\"").And.NotContain("/v1.0/").And.NotContain("legacy");
	}

	[Fact]
	public void TheWorkingTreeAndIndex_AreUntouched_AndTheWorktreesAreRemoved()
	{
		_ = Repo;
		site.StatusAfter.Should().Be(site.StatusBefore);
		site.StatusBefore.Should().Contain("docs/guide/intro.md").And.Contain("docs/guide/scratch.md");
		site.IndexAfter.Should().Be(site.IndexBefore);
		Regex.Matches(site.Worktrees, "^worktree ", RegexOptions.Multiline).Should().ContainSingle();
		site.WorkLeftovers.Should().BeEmpty();
	}
}

/// <summary>Versioned builds that change the repository or the output, each in its own repository.</summary>
[Collection(VersionedBuildCollection.Name)]
public sealed class VersionedBuildEdgeTests
{
	[Fact]
	public async Task OnlyVersion_RebuildsThatFolderAndLeavesTheRestOfTheSite()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);
		(await VersionedSites.BuildAsync(VersionedSites.Request(repo))).Diagnostics.HasErrors.Should().BeFalse();

		string rootSentinel = Path.Combine(repo.Root, "_site", "keep-root.txt");
		string v2Sentinel = Path.Combine(repo.Root, "_site", "v2.0.0", "keep-v2.txt");
		string v1Sentinel = Path.Combine(repo.Root, "_site", "v1.0.0", "stale-v1.txt");
		foreach (string sentinel in new[] { rootSentinel, v2Sentinel, v1Sentinel })
		{
			File.WriteAllText(sentinel, "x");
		}

		VersionedBuildResult v1 = await VersionedSites.BuildAsync(VersionedSites.Request(repo, only: ["v1.0.0"]));

		v1.Diagnostics.HasErrors.Should().BeFalse();
		v1.Builds.Should().ContainSingle().Which.Version.Slug.Should().Be("v1.0.0");
		File.Exists(v1Sentinel).Should().BeFalse("the rebuilt version's folder is cleaned");
		File.Exists(rootSentinel).Should().BeTrue();
		File.Exists(v2Sentinel).Should().BeTrue();

		// v2.0.0 wasn't built in this run; its pages come from the folder an earlier build wrote.
		string intro = VersionedSites.Read(repo, "v1.0.0/guide/intro/index.html");
		intro.Should().Contain("href=\"/guide/intro\" data-version-slug=\"v2.0.0\"");

		VersionedBuildResult v2 = await VersionedSites.BuildAsync(VersionedSites.Request(repo, only: ["V2.0.0"]));

		v2.Builds.Select(b => b.IsSiteRoot).Should().Equal(true, false);
		File.Exists(rootSentinel).Should().BeTrue("the site root holds the other versions and is not cleaned");
		File.Exists(v2Sentinel).Should().BeFalse();
		VersionedSites.Exists(repo, "v1.0.0/index.html").Should().BeTrue();
	}

	[Fact]
	public async Task UnknownVersionName_IsAnError()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);

		VersionedBuildResult result = await VersionedSites.BuildAsync(VersionedSites.Request(repo, only: ["v3"]));

		result.Builds.Should().BeEmpty();
		result.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
			.Which.Message.Should().Contain("'v3'").And.Contain("next, v2.0.0, v1.0.0");
		Directory.Exists(Path.Combine(repo.Root, "_site")).Should().BeFalse();
	}

	[Fact]
	public async Task MissingRef_IsAnErrorNamingTheVersion_AndTheOthersAreStillBuilt()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);
		SiteConfig config = WithVersions(VersionedSites.ReadConfig(repo),
			new VersionDefinition { Label = "", Branch = "main", Default = true },
			new VersionDefinition { Label = "v9", Tag = "v9.9.9" },
			new VersionDefinition { Label = "", Tag = "v1.0.0" });

		VersionedBuildResult result = await VersionedSites.BuildAsync(VersionedSites.Request(repo, config));

		result.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Should().ContainSingle()
			.Which.Message.Should().Contain("Version 'v9'").And.Contain("tag 'v9.9.9'");
		BuildCommand.Fails(result.Diagnostics, false).Should().BeTrue();
		VersionedSites.Exists(repo, "v1.0.0/index.html").Should().BeTrue();
		VersionedSites.Exists(repo, "index.html").Should().BeTrue();
		VersionedSites.Read(repo, "index.html").Should().NotContain("data-version-slug=\"v9\"");
		Directory.Exists(Path.Combine(repo.Root, "_site", "v9")).Should().BeFalse();
	}

	[Fact]
	public async Task MissingRef_FailsTheBuildCommand()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);
		SiteConfig config = WithVersions(VersionedSites.ReadConfig(repo),
			new VersionDefinition { Label = "", Branch = "main", Default = true },
			new VersionDefinition { Label = "", Branch = "release/9.x" });

		int exitCode = await VersionedBuild.RunAsync(VersionedSites.Request(repo, config), false, false, false,
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task DryRun_ChecksRefsWithoutCheckingThemOut()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);
		SiteConfig config = WithVersions(VersionedSites.ReadConfig(repo),
			new VersionDefinition { Label = "next", Branch = "main" },
			new VersionDefinition { Label = "", Tag = "v2.0.0", Default = true },
			new VersionDefinition { Label = "old", Tag = "v0.1.0" });

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, repo.Root, false, false,
			TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.CurrentVersion!.Label.Should().Be("next");
		outcome.Context.Versions.Select(v => v.Label).Should().Equal("next", "v2.0.0", "old");
		outcome.Context.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Should().ContainSingle()
			.Which.Message.Should().Contain("Version 'old'").And.Contain("tag 'v0.1.0'");
		Directory.Exists(Path.Combine(repo.Root, "_site")).Should().BeFalse();
		Regex.Matches(repo.Git("worktree", "list", "--porcelain"), "^worktree ", RegexOptions.Multiline)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task BasePathAndSiteUrl_ApplyToEveryVersion()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo(withProject: false);
		SiteConfig config = VersionedSites.ReadConfig(repo);
		config = config with
		{
			Site = config.Site with { Url = "https://u.example.com/Lib" },
			Build = config.Build with { BasePath = "/Lib" }
		};

		VersionedBuildResult result = await VersionedSites.BuildAsync(VersionedSites.Request(repo, config));

		result.Diagnostics.HasErrors.Should().BeFalse();
		string intro = VersionedSites.Read(repo, "v1.0.0/guide/intro/index.html");
		intro.Should().Contain("<link rel=\"canonical\" href=\"https://u.example.com/Lib/v1.0.0/guide/intro/\"")
			.And.Contain("href=\"/Lib/guide/intro\" data-version-slug=\"v2.0.0\"")
			.And.Contain("href=\"/Lib/v1.0.0/_theme/css/main.css?v=");
		VersionedSites.Read(repo, "v1.0.0/search-index.json").Should().Contain("\"/Lib/v1.0.0/guide/intro\"");
		VersionedSites.Read(repo, "guide/intro/index.html")
			.Should().Contain("<link rel=\"canonical\" href=\"https://u.example.com/Lib/guide/intro/\"");
	}

	[Fact]
	public async Task NotAGitRepository_IsAnErrorForTheVersionsFromRefs()
	{
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-vtest-nogit-" + Guid.NewGuid().ToString("N")[..12]);
		Directory.CreateDirectory(Path.Combine(root, "docs"));
		try
		{
			File.WriteAllText(Path.Combine(root, "docs", "index.md"), "---\ntitle: Home\n---\n# Home\n");
			File.WriteAllText(Path.Combine(root, "mokadocs.yaml"),
				"site:\n  title: T\nfeatures:\n  versioning:\n    enabled: true\n    versions:\n      - label: current\n        default: true\n      - tag: v1.0.0\nbuild:\n  cache: false\n");
			SiteConfig config = new SiteConfigReader(new System.IO.Abstractions.FileSystem())
				.Read(Path.Combine(root, "mokadocs.yaml"));

			VersionedBuildResult result = await VersionedSites.BuildAsync(new VersionedBuildRequest
			{
				Config = config,
				ConfigPath = Path.Combine(root, "mokadocs.yaml"),
				OutputDirectory = Path.Combine(root, "_site"),
				UseCache = false
			});

			result.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Should().ContainSingle()
				.Which.Message.Should().Contain("tag 'v1.0.0'").And.Contain("not in a git work tree");
			File.Exists(Path.Combine(root, "_site", "current", "index.html")).Should().BeTrue();
			File.Exists(Path.Combine(root, "_site", "index.html")).Should().BeTrue();
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void ApplyTo_PicksTheVersionTheWorkingTreeIs_AndLabelsARefFromItsCommit()
	{
		Assert.SkipUnless(TempGitRepo.GitAvailable, "git is not on PATH");
		using TempGitRepo repo = VersionedSites.CreateLibraryRepo();

		var context = new Core.Pipeline.BuildContext
		{
			Config = VersionedSites.ReadConfig(repo),
			FileSystem = new System.IO.Abstractions.FileSystem(),
			RootDirectory = repo.Root,
			OutputDirectory = Path.Combine(repo.Root, "_site")
		};
		new VersionManager(context.Config, Microsoft.Extensions.Logging.Abstractions.NullLogger<VersionManager>.Instance)
			.ApplyTo(context);

		context.Versions.Select(v => (v.Label, v.Number, v.IsDefault)).Should().Equal(
			("next", "2.1.0", false), ("v2.0", "2.0.0", true), ("v1.0", "1.0.0", false));
		context.CurrentVersion!.Label.Should().Be("next");
		context.Diagnostics.All.Should().BeEmpty();
	}

	private static SiteConfig WithVersions(SiteConfig config, params VersionDefinition[] versions) => config with
	{
		Features = config.Features with
		{
			Versioning = config.Features.Versioning with { Versions = versions.ToList() }
		}
	};
}
