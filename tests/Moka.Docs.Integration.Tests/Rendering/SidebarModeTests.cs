using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     <c>theme.options.sidebar</c>: the whole sidebar in every page, or written once and loaded by
///     every page. 5012 pages with an inline sidebar wrote 8.4 GB.
/// </summary>
public sealed class SidebarModeTests
{
	private const string _sidebarFile = "/project/_site/_theme/js/sidebar.js";

	// Seven sidebar entries: Guide [Advanced, Intro], Home, Reference [CLI, Config].
	private static Dictionary<string, MockFileData> Files() => new()
	{
		{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home\n") },
		{ "/project/docs/guide/index.md", new MockFileData("---\ntitle: Guide\n---\n# Guide\n") },
		{ "/project/docs/guide/intro.md", new MockFileData("---\ntitle: Intro\n---\n# Intro\n") },
		{ "/project/docs/guide/advanced.md", new MockFileData("---\ntitle: Advanced\n---\n# Advanced\n") },
		{ "/project/docs/reference/cli.md", new MockFileData("---\ntitle: CLI\n---\n# CLI\n") },
		{ "/project/docs/reference/config.md", new MockFileData("---\ntitle: Config\n---\n# Config\n") }
	};

	private static SiteConfig Config(string sidebar = "auto", int limit = 300, string basePath = "/") =>
		MockSite.Config() with
		{
			Theme = new ThemeConfig { Options = new ThemeOptions { Sidebar = sidebar, SidebarInlineLimit = limit } },
			Build = new BuildConfig { BasePath = basePath }
		};

	private static string Sidebar(string html) =>
		Regex.Match(html, "<aside class=\"sidebar\".*?</aside>", RegexOptions.Singleline).Value;

	// Links only: the icons' <use href> point into the sprite.
	private static List<string> Links(string html) =>
		Regex.Matches(html, "<a [^>]*?href=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();

	[Fact]
	public async Task DefaultMode_SmallSite_PutsTheWholeSidebarInEveryPage()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.FileExists(_sidebarFile).Should().BeFalse();
		context.DeferredOutputFiles.Keys.Should().NotContain(k => k.EndsWith("sidebar.js", StringComparison.Ordinal));
		string sidebar = Sidebar(fs.File.ReadAllText("/project/_site/guide/intro/index.html"));
		Links(sidebar).Should().Equal("/guide", "/guide/advanced", "/guide/intro", "/", "/reference/cli", "/reference/config");
		sidebar.Should().Contain("<span class=\"nav-link nav-group-label\">", "reference/ has no index page");
		sidebar.Should().NotContain("<script").And.NotContain("data-nav-route");
	}

	[Fact]
	public async Task SharedMode_WritesTheSidebarOnceAndEveryPageLoadsItByItsHash()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("shared"), Files());

		fs.FileExists(_sidebarFile).Should().BeTrue();
		fs.AllFiles.Where(f => f.EndsWith("sidebar.js", StringComparison.Ordinal)).Should().ContainSingle();
		byte[] script = fs.File.ReadAllBytes(_sidebarFile);
		string hash = Convert.ToHexStringLower(SHA256.HashData(script))[..10];
		string text = fs.File.ReadAllText(_sidebarFile);
		foreach (string route in new[] { "/guide", "/guide/advanced", "/guide/intro", "/", "/reference/cli", "/reference/config" })
		{
			text.Should().Contain($"href=\\\"{route}\\\"");
		}

		foreach (string page in new[] { "", "guide/", "guide/intro/", "guide/advanced/", "reference/cli/", "reference/config/" })
		{
			fs.File.ReadAllText($"/project/_site/{page}index.html")
				.Should().Contain($"<script src=\"/_theme/js/sidebar.js?v={hash}\"></script></aside>",
					"/{0} should load the shared sidebar", page);
		}
	}

	[Fact]
	public async Task SharedMode_PageKeepsTheTopLevelAndItsOwnSection()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("shared"), Files());

		string sidebar = Sidebar(fs.File.ReadAllText("/project/_site/guide/intro/index.html"));

		Links(sidebar).Should().Equal("/guide", "/guide/advanced", "/guide/intro", "/", "/reference");
		sidebar.Should().Contain("data-nav-route=\"/guide/intro\"");
	}

	[Theory]
	[InlineData(6, true)]
	[InlineData(7, false)]
	public async Task AutoMode_SharesTheSidebarAboveTheLimit(int limit, bool shared)
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(limit: limit), Files());

		fs.FileExists(_sidebarFile).Should().Be(shared);
		fs.File.ReadAllText("/project/_site/guide/intro/index.html").Contains("/_theme/js/sidebar.js?v=").Should().Be(shared);
	}

	[Fact]
	public async Task InlineMode_IgnoresTheLimit()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("inline", limit: 0), Files());

		fs.FileExists(_sidebarFile).Should().BeFalse();
	}

	[Fact]
	public async Task SharedMode_WithABasePath_PrefixesTheScriptAndItsLinks()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config("shared", basePath: "/Repo"), Files());

		string page = fs.File.ReadAllText("/project/_site/guide/intro/index.html");
		page.Should().MatchRegex("<script src=\"/Repo/_theme/js/sidebar\\.js\\?v=[0-9a-f]{10}\"></script>");
		page.Should().Contain("data-nav-route=\"/Repo/guide/intro\"");
		fs.File.ReadAllText(_sidebarFile).Should().Contain("href=\\\"/Repo/reference/cli\\\"").And.NotContain("href=\\\"/reference");
	}

	[Fact]
	public async Task UnknownMode_IsReportedAndWorksLikeAuto()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config("lazy"), Files());

		context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Warning)
			.Which.Message.Should().Contain("'lazy'").And.Contain("auto");
		fs.FileExists(_sidebarFile).Should().BeFalse();
	}

	[Fact]
	public async Task SharedMode_WithACustomTheme_IsReportedAndInline()
	{
		Dictionary<string, MockFileData> files = Files();
		files["/project/mytheme/layouts/default.html"] =
			new MockFileData("<html><body>{{ for item in nav }}<a href=\"{{ item.route }}\">{{ item.label }}</a>" +
			                 "{{ for child in item.children }}<a href=\"{{ child.route }}\">{{ child.label }}</a>{{ end }}{{ end }}" +
			                 "{{ page.content }}</body></html>");
		SiteConfig config = Config("shared") with
		{
			Theme = new ThemeConfig { Name = "mytheme", Options = new ThemeOptions { Sidebar = "shared" } }
		};

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(config, files);

		context.Diagnostics.All.Should().Contain(d => d.Message.Contains("'mytheme'") && d.Message.Contains("shared sidebar"));
		fs.FileExists(_sidebarFile).Should().BeFalse();
		Links(fs.File.ReadAllText("/project/_site/guide/intro/index.html")).Should().Contain("/reference/cli");
	}

	[Fact]
	public async Task SharedMode_DryRun_WritesNothing()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config("shared"), Files(), dryRun: true);

		context.DeferredOutputFiles.Keys.Should().Contain("_theme/js/sidebar.js", "the sidebar is still rendered and checked");
		fs.Directory.Exists("/project/_site").Should().BeFalse();
	}
}
