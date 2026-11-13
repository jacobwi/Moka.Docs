using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     Breadcrumbs follow the sidebar: its labels, and a link for every section, to the section's own
///     page or else its first page.
/// </summary>
public sealed partial class BreadcrumbTests
{
	private static Dictionary<string, MockFileData> Files() => new()
	{
		[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
		[MockSite.Root + "/docs/guide/intro.md"] = new("---\ntitle: Intro\n---\n# Intro\n"),
		[MockSite.Root + "/docs/guide/install.md"] = new("---\ntitle: Install\n---\n# Install\n"),
		[MockSite.Root + "/docs/guide/secret.md"] = new("---\ntitle: Secret\nvisibility: hidden\n---\n# Secret\n"),
		[MockSite.Root + "/docs/reference/options.md"] = new("---\ntitle: Options\n---\n# Options\n")
	};

	private static SiteConfig Config(string breadcrumbs = "default") => MockSite.Config() with
	{
		Nav =
		[
			new NavItem
			{
				Label = "User Guide",
				Path = "/guide",
				Children = [new NavItem { Label = "Intro", Path = "/guide/intro" }, new NavItem { Label = "Install", Path = "/guide/install" }]
			},
			new NavItem { Label = "Reference", Children = [new NavItem { Label = "All options", Path = "/reference/options" }] }
		],
		Theme = new ThemeConfig { Options = new ThemeOptions { Layout = new ThemeLayoutOptions { Breadcrumbs = breadcrumbs } } },
		Build = new BuildConfig { BasePath = "/Repo" }
	};

	private static async Task<List<string>> CrumbsAsync(string page, string breadcrumbs = "default")
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(breadcrumbs), Files());
		string html = fs.File.ReadAllText($"{MockSite.Root}/_site/{page}/index.html");
		int start = html.IndexOf("<nav class=\"breadcrumbs\"", StringComparison.Ordinal);
		string nav = html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];
		return CrumbRegex().Matches(nav).Select(m => Regex.Replace(m.Groups[1].Value.Trim(), @"\s+", " ")).ToList();
	}

	[Fact]
	public async Task SectionWithoutAPage_UsesTheSidebarLabelAndLinksToItsFirstPage()
	{
		// The section's route was resolved to its first page, so the crumb fell back to the folder name
		// ("Guide") and, with no page at /guide, was plain text.
		List<string> crumbs = await CrumbsAsync("guide/install");

		crumbs.Should().Equal(
			"<a href=\"/Repo/\">Home</a>",
			"<a href=\"/Repo/guide/intro/\">User Guide</a>",
			"<span aria-current=\"page\">Install</span>");
	}

	[Fact]
	public async Task NavGroupWithoutAPath_LinksToItsFirstPage()
	{
		List<string> crumbs = await CrumbsAsync("reference/options");

		crumbs.Should().Equal(
			"<a href=\"/Repo/\">Home</a>",
			"<a href=\"/Repo/reference/options/\">Reference</a>",
			"<span aria-current=\"page\">Options</span>");
	}

	[Fact]
	public async Task PageOutsideTheSidebar_UsesTheSectionHoldingItsFolder()
	{
		List<string> crumbs = await CrumbsAsync("guide/secret");

		crumbs.Should().Equal(
			"<a href=\"/Repo/\">Home</a>",
			"<a href=\"/Repo/guide/intro/\">User Guide</a>",
			"<span aria-current=\"page\">Secret</span>");
	}

	[Fact]
	public async Task FolderNoSidebarSectionHolds_GetsItsNameAndItsFirstPage()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/extra-notes/two.md"] = new("---\ntitle: Two\nvisibility: hidden\norder: 2\n---\n# Two\n");
		files[MockSite.Root + "/docs/extra-notes/one.md"] = new("---\ntitle: One\nvisibility: hidden\norder: 1\n---\n# One\n");

		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), files);
		string html = fs.File.ReadAllText(MockSite.Root + "/_site/extra-notes/two/index.html");

		html.Should().Contain("<a href=\"/Repo/extra-notes/one/\">Extra notes</a>", "the folder's redirect leads there too");
		fs.File.ReadAllText(MockSite.Root + "/_site/extra-notes/index.html").Should().Contain("url=/Repo/extra-notes/one/\"");
	}

	[Fact]
	public async Task SectionOfASubfolder_DoesNotNameTheFolder()
	{
		SiteConfig config = MockSite.Config() with
		{
			Nav = [new NavItem { Label = "Lib.Core", Children = [new NavItem { Label = "X", Path = "/lib/core/x" }] }]
		};
		var files = new Dictionary<string, MockFileData>
		{
			[MockSite.Root + "/docs/lib/core/x.md"] = new("---\ntitle: X\n---\n# X\n"),
			[MockSite.Root + "/docs/lib/notes.md"] = new("---\ntitle: Notes\nvisibility: hidden\n---\n# Notes\n")
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, files);
		string html = fs.File.ReadAllText(MockSite.Root + "/_site/lib/notes/index.html");
		int start = html.IndexOf("<nav class=\"breadcrumbs\"", StringComparison.Ordinal);
		string trail = html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];

		CrumbRegex().Matches(trail).Select(m => m.Groups[1].Value).Should().Equal(
			"<a href=\"/\">Home</a>", "<a href=\"/lib/core/x/\">Lib</a>", "<span aria-current=\"page\">Notes</span>");
	}

	[Fact]
	public async Task BackVariant_AlwaysHasALinkToShow()
	{
		// The back variant shows only the crumb before the page. For a section without a page that was
		// a <span>, so the page showed an arrow that went nowhere.
		List<string> crumbs = await CrumbsAsync("guide/intro", "back");

		crumbs[^2].Should().Be("<a href=\"/Repo/guide/intro/\">User Guide</a>");
	}

	[Fact]
	public async Task AutoNavigation_SectionWithAnIndexPage_LinksToIt()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/guide/index.md"] = new("---\ntitle: The Guide\n---\n# The Guide\n");

		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), files);
		string html = fs.File.ReadAllText(MockSite.Root + "/_site/guide/install/index.html");

		html.Should().Contain("<a href=\"/guide/\">The Guide</a>");
	}

	[GeneratedRegex(@"<li>\s*(<(?:a|span)[^>]*>[^<]*</(?:a|span)>)\s*</li>")]
	private static partial Regex CrumbRegex();
}
