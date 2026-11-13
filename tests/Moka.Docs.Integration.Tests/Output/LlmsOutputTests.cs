using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Output;

/// <summary>
///     Markdown copies of the pages, <c>llms.txt</c> and <c>llms-full.txt</c> (<c>build.llms</c>), and the
///     theme's "Copy page" button that uses them.
/// </summary>
public sealed class LlmsOutputTests
{
	private const string _site = MockSite.Root + "/_site";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\ndescription: Start here\n---\n# Welcome\n\nRead the [intro](guide/intro.md).\n"),
		[MockSite.Root + "/docs/guide/intro.md"] = new(
			"---\ntitle: Intro\ndescription: What it is\norder: 1\n---\n# Intro\n\nSee [setup](./setup.md#run) and ![logo](../img/logo.png).\n\n::: tip\nKeep [this](./setup.md).\n:::\n"),
		[MockSite.Root + "/docs/guide/setup.md"] = new("---\ntitle: Setup\norder: 2\n---\n## Run it\n\nText.\n"),
		[MockSite.Root + "/docs/changelog.md"] = new("---\ntitle: Changelog\n---\n# Changelog\n"),
		[MockSite.Root + "/docs/notes/draft.md"] = new("---\ntitle: Draft\nvisibility: draft\n---\n# Draft\n"),
		[MockSite.Root + "/docs/notes/hidden.md"] = new("---\ntitle: Hidden\nvisibility: hidden\n---\n# Hidden\n"),
		[MockSite.Root + "/docs/notes/gated.md"] = new("---\ntitle: Gated\nrequires: AuditLog\n---\n# Gated\n"),
		[MockSite.Root + "/docs/notes/unlisted.md"] = new("---\ntitle: Unlisted\n---\n# Unlisted\n")
	};

	private static SiteConfig Config(string url = "https://test.example.com", string basePath = "/", bool llms = true) =>
		MockSite.Config() with
		{
			Site = new SiteMetadata { Title = "Test Site", Description = "Docs for tests", Url = url },
			Nav =
			[
				new NavItem { Label = "Home", Path = "/" },
				new NavItem
				{
					Label = "Guide",
					Path = "/guide",
					Children = [new NavItem { Label = "Intro", Path = "/guide/intro" }, new NavItem { Label = "Setup", Path = "/guide/setup" }]
				},
				new NavItem { Label = "Changelog", Path = "/changelog" }
			],
			Build = new BuildConfig { BasePath = basePath, Llms = llms }
		};

	[Fact]
	public async Task EveryPublicPage_GetsAMarkdownCopyNextToItsHtml()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.File.ReadAllText(_site + "/guide/intro.md").Should().Be(
			"# Intro\n\nSee [setup](https://test.example.com/guide/setup#run) and ![logo](https://test.example.com/img/logo.png).\n\n" +
			"::: tip\nKeep [this](https://test.example.com/guide/setup).\n:::\n");
		fs.File.ReadAllText(_site + "/index.md").Should().StartWith("# Welcome\n\nRead the [intro](https://test.example.com/guide/intro).");
		fs.File.ReadAllText(_site + "/guide/setup.md").Should().StartWith("# Setup\n\n## Run it", "a page without an h1 gets its title");
		fs.FileExists(_site + "/notes/unlisted.md").Should().BeTrue();
	}

	[Fact]
	public async Task DraftHiddenAndGatedPages_GetNothing()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.FileExists(_site + "/notes/draft.md").Should().BeFalse();
		fs.FileExists(_site + "/notes/hidden.md").Should().BeFalse();
		fs.FileExists(_site + "/notes/gated.md").Should().BeFalse();
		string everything = fs.File.ReadAllText(_site + "/llms.txt") + fs.File.ReadAllText(_site + "/llms-full.txt");
		everything.Should().NotContain("Draft").And.NotContain("Hidden").And.NotContain("Gated");
	}

	[Fact]
	public async Task LlmsTxt_ListsThePagesBySidebarSection()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.File.ReadAllText(_site + "/llms.txt").Should().Be("""
		                                                       # Test Site

		                                                       > Docs for tests

		                                                       ## Docs

		                                                       - [Home](https://test.example.com/index.md): Start here
		                                                       - [Changelog](https://test.example.com/changelog.md)

		                                                       ## Guide

		                                                       - [Intro](https://test.example.com/guide/intro.md): What it is
		                                                       - [Setup](https://test.example.com/guide/setup.md)

		                                                       ## Optional

		                                                       - [Unlisted](https://test.example.com/notes/unlisted.md)

		                                                       """.ReplaceLineEndings("\n"));
	}

	[Fact]
	public async Task HomePageOutsideTheSidebar_IsListedFirst()
	{
		SiteConfig config = Config() with { Nav = Config().Nav.Skip(1).ToList() };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string index = fs.File.ReadAllText(_site + "/llms.txt");
		index.Should().Contain("> Docs for tests\n\n## Docs\n\n- [Home](https://test.example.com/index.md): Start here\n" +
		                       "- [Changelog](https://test.example.com/changelog.md)\n\n## Guide");
		fs.File.ReadAllText(_site + "/llms-full.txt").Should().StartWith("# Test Site\n\n> Docs for tests\n\n# Welcome\n");
	}

	[Fact]
	public async Task LlmsFullTxt_HoldsEveryCopyInTheSameOrder()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		string full = fs.File.ReadAllText(_site + "/llms-full.txt");
		full.Should().StartWith("# Test Site\n\n> Docs for tests\n\n# Welcome\n");
		string[] headings = ["# Welcome", "# Changelog", "# Intro", "# Setup", "# Unlisted"];
		headings.Select(h => full.IndexOf(h + "\n", StringComparison.Ordinal)).Should().BeInAscendingOrder().And.NotContain(-1);
	}

	[Fact]
	public async Task WithoutSiteUrl_LinksAreRootRelativeWithTheBasePath()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(url: "", basePath: "/Repo"), Files());

		fs.File.ReadAllText(_site + "/guide/intro.md").Should().Contain("[setup](/Repo/guide/setup#run)");
		fs.File.ReadAllText(_site + "/llms.txt").Should().Contain("- [Intro](/Repo/guide/intro.md): What it is");
	}

	[Fact]
	public async Task Pages_HaveACopyButtonAndAMarkdownAlternate()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(url: "", basePath: "/Repo"), Files());

		string html = fs.File.ReadAllText(_site + "/guide/intro/index.html");
		html.Should().Contain("<link rel=\"alternate\" type=\"text/markdown\" title=\"Markdown\" href=\"/Repo/guide/intro.md\" />")
			.And.Contain("data-markdown-url=\"/Repo/guide/intro.md\" data-copied=\"Copied!\" data-failed=\"Copy failed\" hidden>Copy page</button>")
			.And.Contain("<a class=\"page-action page-view-markdown\" href=\"/Repo/guide/intro.md\" type=\"text/markdown\">View as Markdown</a>");
		fs.File.ReadAllText(_site + "/notes/hidden/index.html").Should().NotContain("page-copy-markdown");
	}

	[Fact]
	public async Task LlmsOff_WritesNoCopiesAndNoButton()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(llms: false), Files());

		fs.FileExists(_site + "/llms.txt").Should().BeFalse();
		fs.FileExists(_site + "/llms-full.txt").Should().BeFalse();
		fs.FileExists(_site + "/guide/intro.md").Should().BeFalse();
		fs.File.ReadAllText(_site + "/guide/intro/index.html").Should().NotContain("page-copy-markdown")
			.And.NotContain("text/markdown");
	}

	[Fact]
	public async Task DryRun_StagesAndWritesNothing()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files(), dryRun: true);

		fs.FileExists(_site + "/llms.txt").Should().BeFalse();
		context.DeferredOutputFiles.Keys.Should().NotContain(k => k.EndsWith(".md", StringComparison.Ordinal))
			.And.NotContain("llms.txt");
	}

	[Fact]
	public async Task OwnLlmsTxtInTheDocsFolder_IsPublishedInsteadOfTheGeneratedOne()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/llms.txt"] = new("# Hand-written\n");

		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), files);

		fs.File.ReadAllText(_site + "/llms.txt").Should().Be("# Hand-written\n");
		fs.File.ReadAllText(_site + "/llms-full.txt").Should().StartWith("# Test Site");
	}

	[Fact]
	public async Task LandingPage_CopyListsItsFeatures()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/index.md"] = new("""
		                                              ---
		                                              title: Home
		                                              layout: landing
		                                              featuresTitle: Why
		                                              features:
		                                                - title: Fast
		                                                  description: Builds in seconds
		                                                - title: Small
		                                              ---
		                                              """);

		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), files);

		fs.File.ReadAllText(_site + "/index.md").Should().Be("# Home\n\n## Why\n\n- **Fast**: Builds in seconds\n- **Small**\n");
	}

	[Fact]
	public async Task OldBuildInTheDocsFolder_IsNotPublishedAgain()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/_old/.mokadocs-output"] = new("");
		files[MockSite.Root + "/docs/_old/guide/intro.md"] = new("# Old copy\n");
		files[MockSite.Root + "/docs/_old/llms.txt"] = new("# Old\n");

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(), files);

		fs.Directory.Exists(_site + "/_old").Should().BeFalse();
		context.Diagnostics.All.Should().ContainSingle(d => d.Message.Contains("'_old'") && d.Message.Contains(".mokadocs-output"));
	}
}
