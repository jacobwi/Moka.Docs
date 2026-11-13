using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Output;

/// <summary>
///     A folder with pages but no index page gets a redirect stub to the page the sidebar opens the
///     section with.
/// </summary>
public sealed class SectionRedirectTests
{
	private const string _stub = MockSite.Root + "/_site/guide/index.html";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
		[MockSite.Root + "/docs/guide/alpha.md"] = new("---\ntitle: Alpha\norder: 2\n---\n# Alpha\n"),
		[MockSite.Root + "/docs/guide/beta.md"] = new("---\ntitle: Beta & co\norder: 3\n---\n# Beta\n"),
		[MockSite.Root + "/docs/guide/zulu.md"] = new("---\ntitle: Zulu\norder: 1\n---\n# Zulu\n")
	};

	[Fact]
	public async Task Stub_RedirectsToTheSectionsFirstPageInSidebarOrder()
	{
		// The alphabetically first subfolder (alpha) won, though the sidebar opens the section with Zulu.
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), Files());

		fs.File.ReadAllText(_stub).Should().Contain("url=/guide/zulu/\"").And.Contain("<a href=\"/guide/zulu/\">Zulu</a>");
	}

	[Fact]
	public async Task Stub_FollowsAnExplicitNavOrder()
	{
		SiteConfig config = MockSite.Config() with
		{
			Nav =
			[
				new NavItem
				{
					Label = "Guide",
					Path = "/guide",
					Children = [new NavItem { Label = "Beta", Path = "/guide/beta" }, new NavItem { Label = "Alpha", Path = "/guide/alpha" }]
				}
			]
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		fs.File.ReadAllText(_stub).Should().Contain("url=/guide/beta/\"").And.Contain(">Beta &amp; co</a>");
	}

	[Fact]
	public async Task Stub_IsACompleteNoindexPageWithAnAbsoluteCanonical()
	{
		SiteConfig config = MockSite.Config() with
		{
			Site = new SiteMetadata { Title = "Test", Url = "https://user.github.io/Repo" },
			Build = new BuildConfig { BasePath = "/Repo" }
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string stub = fs.File.ReadAllText(_stub);
		stub.Should().Contain("<html lang=\"en\" dir=\"ltr\">")
			.And.Contain("<meta charset=\"utf-8\">")
			.And.Contain("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
			.And.Contain("<meta name=\"robots\" content=\"noindex\">")
			.And.Contain("<meta http-equiv=\"refresh\" content=\"0; url=/Repo/guide/zulu/\">")
			.And.Contain("<link rel=\"canonical\" href=\"https://user.github.io/Repo/guide/zulu/\">")
			.And.Contain("<a href=\"/Repo/guide/zulu/\">Zulu</a>");
		stub.Should().NotContain("/Repo/Repo/");
	}

	[Fact]
	public async Task Stub_WithoutSiteUrl_HasNoCanonical()
	{
		SiteConfig config = MockSite.Config() with { Site = new SiteMetadata { Title = "Test" } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		fs.File.ReadAllText(_stub).Should().NotContain("canonical").And.Contain("url=/guide/zulu/\"");
	}

	[Fact]
	public async Task NestedFolders_EachRedirectStraightToAPage()
	{
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/docs/ref/api/types.md"] = new("---\ntitle: Types\n---\n# Types\n");

		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), files);

		fs.File.ReadAllText(MockSite.Root + "/_site/ref/index.html").Should().Contain("url=/ref/api/types/\"");
		fs.File.ReadAllText(MockSite.Root + "/_site/ref/api/index.html").Should().Contain("url=/ref/api/types/\"");
	}

	[Fact]
	public async Task FolderOutsideTheSidebar_RedirectsToItsFirstPageByOrder()
	{
		SiteConfig config = MockSite.Config() with { Nav = [new NavItem { Label = "Home", Path = "/" }] };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		fs.File.ReadAllText(_stub).Should().Contain("url=/guide/zulu/\"");
	}
}
