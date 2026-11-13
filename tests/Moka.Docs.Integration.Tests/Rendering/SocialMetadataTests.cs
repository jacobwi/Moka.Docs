using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     Link preview metadata: <c>site.image</c> as the default <c>og:image</c> and <c>twitter:image</c>,
///     a page's front matter <c>image</c> over it, absolute URLs with the base path once, and the same
///     block on the landing layout.
/// </summary>
public sealed class SocialMetadataTests
{
	private const string _page = MockSite.Root + "/_site/guide/intro/index.html";
	private const string _landing = MockSite.Root + "/_site/index.html";

	private static readonly MockFileSystem _paths = new();

	private static Dictionary<string, MockFileData> Files(string introImage = "") => new()
	{
		[MockSite.Root + "/docs/index.md"] = new("---\ntitle: Home\nlayout: landing\n---\nWelcome.\n"),
		[MockSite.Root + "/docs/guide/intro.md"] = new($"---\ntitle: Intro\n{introImage}---\n# Intro\n"),
		[MockSite.Root + "/docs/guide/cover.png"] = new([1]),
		[MockSite.Root + "/assets/card.png"] = new([2])
	};

	private static SiteConfig Config(string url = "https://user.github.io/Repo", string image = "assets/card.png") =>
		MockSite.Config() with
		{
			Site = new SiteMetadata
			{
				Title = "Test Site",
				Url = url,
				Image = image.Length == 0
					? null
					: new SiteAssetReference
					{
						RawValue = image,
						SourcePath = _paths.Path.GetFullPath(MockSite.Root + "/" + image),
						PublishUrl = "/" + image,
						IsAbsoluteUrl = false
					}
			},
			Build = new BuildConfig { BasePath = "/Repo" }
		};

	[Fact]
	public async Task SiteImage_IsTheDefaultPreviewImageOnBothLayouts()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		foreach (string path in new[] { _page, _landing })
		{
			string html = fs.File.ReadAllText(path);
			html.Should().Contain("<meta property=\"og:image\" content=\"https://user.github.io/Repo/assets/card.png\" />")
				.And.Contain("<meta name=\"twitter:card\" content=\"summary_large_image\" />")
				.And.Contain("<meta name=\"twitter:image\" content=\"https://user.github.io/Repo/assets/card.png\" />");
			html.Should().NotContain("/Repo/Repo/");
		}

		fs.FileExists(MockSite.Root + "/_site/assets/card.png").Should().BeTrue("the image is copied like the logo");
	}

	[Fact]
	public async Task LandingLayout_HasTheOpenGraphBlock()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files());

		fs.File.ReadAllText(_landing).Should().Contain("<meta property=\"og:title\" content=\"Test Site\" />")
			.And.Contain("<meta property=\"og:url\" content=\"https://user.github.io/Repo/\" />")
			.And.Contain("<meta property=\"og:type\" content=\"website\" />");
	}

	[Fact]
	public async Task PageImage_ResolvesFromThePagesFolderAndWins()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files("image: cover.png\n"));

		string html = fs.File.ReadAllText(_page);
		html.Should().Contain("<meta property=\"og:image\" content=\"https://user.github.io/Repo/guide/cover.png\" />")
			.And.NotContain("assets/card.png");
	}

	[Fact]
	public async Task PageImage_AnAbsoluteUrlIsKept()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files("image: https://cdn.example/x.png\n"));

		fs.File.ReadAllText(_page).Should().Contain("<meta property=\"og:image\" content=\"https://cdn.example/x.png\" />");
	}

	[Fact]
	public async Task PageImage_ADataUriFallsBackToTheSiteImage()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(), Files("image: \"data:image/png;base64,AAAA\"\n"));

		fs.File.ReadAllText(_page).Should().Contain("content=\"https://user.github.io/Repo/assets/card.png\"")
			.And.NotContain("data:image");
	}

	[Fact]
	public async Task NoImage_KeepsTheSmallCard()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(image: ""), Files());

		string html = fs.File.ReadAllText(_page);
		html.Should().Contain("<meta name=\"twitter:card\" content=\"summary\" />")
			.And.NotContain("og:image").And.NotContain("twitter:image");
	}

	[Fact]
	public async Task MissingImageFile_IsReportedAndLeftOut()
	{
		Dictionary<string, MockFileData> files = Files();
		files.Remove(MockSite.Root + "/assets/card.png");

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(Config(), files);

		fs.File.ReadAllText(_page).Should().NotContain("og:image");
		context.Diagnostics.All.Should().Contain(d => d.Message.StartsWith("site.image file not found", StringComparison.Ordinal));
	}

	[Fact]
	public async Task WithoutSiteUrl_ThereIsNoPreviewBlock()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(Config(url: ""), Files("image: https://cdn.example/x.png\n"));

		fs.File.ReadAllText(_page).Should().NotContain("og:").And.NotContain("twitter:");
		fs.File.ReadAllText(_landing).Should().NotContain("og:");
	}
}
