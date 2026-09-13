using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Rendering;

public sealed class BrandAssetRenderingTests
{
	private static readonly MockFileSystem _paths = new();

	private static Dictionary<string, MockFileData> Pages() => new()
	{
		["/project/docs/index.md"] = new("---\ntitle: Home\nlayout: landing\n---\nWelcome.\n"),
		["/project/docs/guide/intro.md"] = new("---\ntitle: Intro\n---\n# Intro\n")
	};

	private static SiteAssetReference Local(string relativePath) => new()
	{
		RawValue = relativePath,
		SourcePath = _paths.Path.GetFullPath("/project/" + relativePath),
		PublishUrl = "/" + relativePath,
		IsAbsoluteUrl = false
	};

	private static SiteConfig WithBrand(SiteAssetReference logo, SiteAssetReference favicon) => MockSite.Config() with
	{
		Site = new SiteMetadata { Title = "Brand Test", Logo = logo, Favicon = favicon },
		Build = new BuildConfig { BasePath = "/Repo" }
	};

	[Fact]
	public async Task MissingLogoAndFavicon_FallBackToTheDefaultMarkAndAreReported()
	{
		// Both files were missing, yet every page emitted an <img> and a favicon link that 404'd,
		// and the only trace was a log line a normal build doesn't print.
		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(
			WithBrand(Local("assets/logo.png"), Local("assets/favicon.ico")), Pages());

		foreach (string page in new[] { "/project/_site/guide/intro/index.html", "/project/_site/index.html" })
		{
			string html = fs.File.ReadAllText(page);
			html.Should().NotContain("<img").And.NotContain("rel=\"icon\"").And.NotContain("assets/logo.png");
		}

		fs.File.ReadAllText("/project/_site/guide/intro/index.html").Should().Contain("class=\"site-logo-icon\"");
		context.Diagnostics.All.Where(d => d.Source == "Discovery")
			.Should().SatisfyRespectively(
				logo => logo.Message.Should().StartWith("site.logo file not found").And.Contain("logo.png"),
				favicon => favicon.Message.Should().StartWith("site.favicon file not found").And.Contain("favicon.ico"));
		context.Diagnostics.All.Where(d => d.Source == "Discovery")
			.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
	}

	[Fact]
	public async Task ExistingLogoAndFavicon_AreEmittedWithTheBasePath()
	{
		Dictionary<string, MockFileData> files = Pages();
		files["/project/assets/logo.png"] = new(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
		files["/project/assets/favicon.ico"] = new(new byte[] { 0, 0, 1, 0 });

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(
			WithBrand(Local("assets/logo.png"), Local("assets/favicon.ico")), files);

		string html = fs.File.ReadAllText("/project/_site/guide/intro/index.html");
		html.Should().Contain("<img src=\"/Repo/assets/logo.png\"")
			.And.Contain("<link rel=\"icon\" href=\"/Repo/assets/favicon.ico\" />")
			.And.NotContain("class=\"site-logo-icon\"");
		fs.File.Exists("/project/_site/assets/logo.png").Should().BeTrue();
		context.Diagnostics.All.Should().NotContain(d => d.Source == "Discovery");
	}

	[Fact]
	public async Task AbsoluteUrlLogo_IsEmittedWithoutAFileCheck()
	{
		var cdn = new SiteAssetReference
		{
			RawValue = "https://cdn.example.com/logo.svg",
			SourcePath = null,
			PublishUrl = "https://cdn.example.com/logo.svg",
			IsAbsoluteUrl = true
		};

		(BuildContext context, MockFileSystem fs) = await MockSite.BuildAsync(WithBrand(cdn, cdn), Pages());

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().Contain("<img src=\"https://cdn.example.com/logo.svg\"")
			.And.Contain("<link rel=\"icon\" href=\"https://cdn.example.com/logo.svg\" />");
		context.Diagnostics.All.Should().NotContain(d => d.Source == "Discovery");
	}
}
