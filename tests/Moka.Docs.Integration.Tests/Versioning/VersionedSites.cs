using System.IO.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Versioning;

namespace Moka.Docs.Integration.Tests.Versioning;

/// <summary>The git-backed versioned builds run one at a time; each checks out several worktrees.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class VersionedBuildCollection
{
	public const string Name = "Versioned builds";
}

/// <summary>Builds a small library repository with two tagged releases and work on main.</summary>
internal static class VersionedSites
{
	public static string MainConfig(bool withProject = true) => string.Join("\n", new[]
	{
		"site:",
		"  title: Lib Docs",
		"  url: https://docs.example.com",
		"content:",
		"  docs: ./docs",
		withProject ? "  projects:\n    - path: ./src/Lib/Lib.csproj" : "  projects: []",
		"features:",
		"  versioning:",
		"    enabled: true",
		"    versions:",
		"      - label: next",
		"        branch: main",
		"        prerelease: true",
		"      - tag: v2.0.0",
		"        default: true",
		"      - tag: v1.0.0",
		"build:",
		"  output: ./_site",
		"  cache: false",
		""
	});

	private static string Props(string version) =>
		$"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>";

	private static string Page(string title, string body, string? extra = null) =>
		$"---\ntitle: {title}\n{extra}---\n\n# {title}\n\n{body}\n";

	/// <summary>
	///     v1.0.0 has a legacy page and its own site title; v2.0.0 drops it, adds a new page and a page
	///     for 2.1 and later; main is 2.1.0 with the version list, an uncommitted edit and an untracked page.
	/// </summary>
	public static TempGitRepo CreateLibraryRepo(bool withProject = true)
	{
		var repo = new TempGitRepo();
		string projects = withProject ? "  projects:\n    - path: ./src/Lib/Lib.csproj\n" : "";
		repo.Write(".gitignore", "_site/\n.mokadocs/\n");
		repo.Write("Directory.Build.props", Props("1.0.0"));
		repo.Write("src/Lib/Lib.csproj",
			"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>");
		repo.Write("src/Lib/Widget.cs", "namespace Lib;\n\n/// <summary>A widget.</summary>\npublic class Widget\n{\n}\n");
		repo.Write("mokadocs.yaml",
			$"site:\n  title: Lib Docs (old)\ncontent:\n  docs: ./docs\n{projects}features:\n  versioning:\n    enabled: true\n");
		repo.Write("docs/index.md", Page("Home", "Home of version one"));
		repo.Write("docs/guide/intro.md", Page("Intro", "Intro one"));
		repo.Write("docs/guide/legacy.md", Page("Legacy", "Only in one"));
		repo.Commit("Release 1.0");
		repo.Git("tag", "v1.0.0");

		repo.Write("Directory.Build.props", Props("2.0.0"));
		repo.Write("mokadocs.yaml",
			$"site:\n  title: Lib Docs\ncontent:\n  docs: ./docs\n{projects}features:\n  versioning:\n    enabled: true\n");
		repo.Write("docs/index.md", Page("Home", "Home of version two"));
		repo.Write("docs/guide/intro.md", Page("Intro", "Intro two"));
		repo.Delete("docs/guide/legacy.md");
		repo.Write("docs/guide/new.md", Page("New", "New in two"));
		repo.Write("docs/guide/future.md", Page("Future", "From 2.1 on", "version: \">=2.1\"\n"));
		repo.Commit("Release 2.0");
		repo.Git("tag", "v2.0.0");

		repo.Write("Directory.Build.props", Props("2.1.0"));
		repo.Write("mokadocs.yaml", MainConfig(withProject));
		repo.Commit("Work on 2.1");

		repo.Write("docs/guide/intro.md", Page("Intro", "Intro work in progress"));
		repo.Write("docs/guide/scratch.md", Page("Scratch", "Untracked notes"));
		return repo;
	}

	public static SiteConfig ReadConfig(TempGitRepo repo) =>
		new SiteConfigReader(new FileSystem()).Read(Path.Combine(repo.Root, "mokadocs.yaml"));

	public static VersionedBuildRequest Request(TempGitRepo repo, SiteConfig? config = null,
		IReadOnlyCollection<string>? only = null) => new()
	{
		Config = config ?? ReadConfig(repo),
		ConfigPath = Path.Combine(repo.Root, "mokadocs.yaml"),
		OutputDirectory = Path.Combine(repo.Root, "_site"),
		UseCache = false,
		Only = only ?? [],
		WorkDirectory = repo.WorkDirectory
	};

	public static Task<VersionedBuildResult> BuildAsync(VersionedBuildRequest request) =>
		new VersionedBuildOrchestrator(NullLogger<VersionedBuildOrchestrator>.Instance)
			.RunAsync(request, VersionedBuild.PipelineRunner(false), TestContext.Current.CancellationToken);


	public static string Read(TempGitRepo repo, string sitePath) =>
		File.ReadAllText(Path.Combine(repo.Root, "_site", sitePath.Replace('/', Path.DirectorySeparatorChar)));

	public static bool Exists(TempGitRepo repo, string sitePath) =>
		File.Exists(Path.Combine(repo.Root, "_site", sitePath.Replace('/', Path.DirectorySeparatorChar)));
}
