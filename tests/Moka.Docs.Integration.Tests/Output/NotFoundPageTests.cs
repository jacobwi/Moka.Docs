using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Output;

/// <summary>
///     <c>404.html</c> is rendered with the theme's default layout, so it has the site's style, dark mode,
///     colors, favicon and layout variants, and stays out of search engines.
/// </summary>
public sealed class NotFoundPageTests
{
	private const string _notFound = MockSite.Root + "/_site/404.html";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
		[MockSite.Root + "/docs/guide/intro.md"] = new("---\ntitle: Intro\n---\n# Intro\n\nText.\n"),
		[MockSite.Root + "/favicon.png"] = new([1, 2, 3])
	};

	private static SiteConfig Config(bool search = true) => MockSite.Config() with
	{
		Site = new SiteMetadata
		{
			Title = "Test Site",
			Url = "https://test.example.com",
			Favicon = new SiteAssetReference
			{
				RawValue = "favicon.png", SourcePath = new MockFileSystem().Path.GetFullPath(MockSite.Root + "/favicon.png"),
				PublishUrl = "/favicon.png", IsAbsoluteUrl = false
			}
		},
		Theme = new ThemeConfig
		{
			Options = new ThemeOptions
			{
				Style = "moka",
				PrimaryColor = "#123456",
				ShowFeedback = true,
				Layout = new ThemeLayoutOptions { Width = "wide" }
			}
		},
		Features = new FeaturesConfig { Search = new SearchFeatureConfig { Enabled = search } },
		Build = new BuildConfig { BasePath = "/Repo" }
	};

	[Fact]
	public async Task NotFoundPage_IsRenderedWithTheSitesThemeSettings()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		string html = fs.File.ReadAllText(_notFound);
		string cssHash = Convert.ToHexStringLower(SHA256.HashData(fs.File.ReadAllBytes(MockSite.Root + "/_site/_theme/css/main.css")))[..10];

		html.Should().Contain("data-style=\"moka\"").And.Contain("data-width=\"wide\"");
		html.Should().Contain("g('mokadocs-theme')", "the dark mode bootstrap runs before first paint");
		html.Should().Contain("--color-primary:#123456");
		html.Should().Contain("<link rel=\"icon\" href=\"/Repo/favicon.png\" />");
		html.Should().Contain($"href=\"/Repo/_theme/css/main.css?v={cssHash}\"");
		html.Should().Contain("<title>Page not found - Test Site</title>");
		html.Should().Contain("<aside class=\"sidebar\"").And.Contain("href=\"/Repo/guide/intro\"");
	}

	[Fact]
	public async Task NotFoundPage_TellsSearchEnginesToSkipItAndLinksHome()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		string html = fs.File.ReadAllText(_notFound);
		html.Should().Contain("<meta name=\"robots\" content=\"noindex\" />");
		html.Should().NotContain("rel=\"canonical\"").And.NotContain("og:url");
		html.Should().Contain("<h1>Page not found</h1>").And.Contain("<a href=\"/Repo/\">Go to the home page</a>");
		html.Should().Contain("data-open-search>search the docs</a>");
		html.Should().NotContain("class=\"breadcrumbs\"").And.NotContain("id=\"feedbackWidget\"")
			.And.NotContain("class=\"page-nav\"").And.NotContain("page-copy-markdown");
	}

	[Fact]
	public async Task NotFoundPage_WithoutSearch_HasNoSearchLink()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(search: false), Files());

		fs.File.ReadAllText(_notFound).Should().NotContain("data-open-search");
	}

	[Fact]
	public async Task OtherPages_HaveNoRobotsMeta()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.File.ReadAllText(MockSite.Root + "/_site/guide/intro/index.html").Should().NotContain("name=\"robots\"");
	}

	[Fact]
	public async Task MinifiedBuild_MinifiesTheNotFoundPage()
	{
		SiteConfig config = Config() with { Build = new BuildConfig { BasePath = "/Repo", Minify = true } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string html = fs.File.ReadAllText(_notFound);
		html.Should().Contain("<h1>Page not found</h1>").And.NotContain("\n                ");
	}

	[Fact]
	public async Task DryRun_WritesNoNotFoundPage()
	{
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files(), dryRun: true);

		fs.FileExists(_notFound).Should().BeFalse();
		context.Diagnostics.HasErrors.Should().BeFalse();
	}
}
