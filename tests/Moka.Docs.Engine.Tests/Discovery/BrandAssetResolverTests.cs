using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Discovery;

namespace Moka.Docs.Engine.Tests.Discovery;

public sealed class BrandAssetResolverTests
{
	// Tests use a normalized forward-slash rootDir so assertions work identically on
	// Windows and Linux — System.IO.Abstractions' MockFileSystem converts to the
	// platform separator internally but keeps our forward-slash inputs consistent.
	private const string RootDir = "/mokadocs-test/docs";
	private const string ParentDir = "/mokadocs-test";

	private static BuildContext CreateContext(MockFileSystem fs, SiteConfig config)
	{
		return new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = RootDir,
			OutputDirectory = fs.Path.Combine(RootDir, "_site")
		};
	}

	private static BrandAssetResolver CreateResolver(MockFileSystem fs) =>
		new(fs, NullLogger<BrandAssetResolver>.Instance);

	[Fact]
	public void Resolve_NoLogoNoFavicon_LeavesBrandAssetFilesEmpty()
	{
		var fs = new MockFileSystem();
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Test" },
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		CreateResolver(fs).Resolve(ctx);

		ctx.BrandAssetFiles.Should().BeEmpty();
	}

	[Fact]
	public void Resolve_LogoInsideYamlDir_AddsToBrandAssetFiles()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[$"{RootDir}/assets/logo.png"] = new("PNGDATA")
		});
		var config = new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = "Test",
				Logo = new SiteAssetReference
				{
					RawValue = "assets/logo.png",
					SourcePath = fs.Path.GetFullPath($"{RootDir}/assets/logo.png"),
					PublishUrl = "/assets/logo.png",
					IsAbsoluteUrl = false
				}
			},
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		CreateResolver(fs).Resolve(ctx);

		ctx.BrandAssetFiles.Should().ContainKey("/assets/logo.png");
		ctx.BrandAssetFiles["/assets/logo.png"].Should()
			.Be(fs.Path.GetFullPath($"{RootDir}/assets/logo.png"));
	}

	[Fact]
	public void Resolve_LogoEscapingYamlDir_FlattensToMediaAndResolves()
	{
		string brandingPath = $"{ParentDir}/branding/logo.png";
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[brandingPath] = new("PNGDATA")
		});
		var config = new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = "Test",
				Logo = new SiteAssetReference
				{
					RawValue = "../branding/logo.png",
					SourcePath = fs.Path.GetFullPath(brandingPath),
					PublishUrl = "/_media/logo.png",
					IsAbsoluteUrl = false
				}
			},
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		CreateResolver(fs).Resolve(ctx);

		ctx.BrandAssetFiles.Should().ContainKey("/_media/logo.png");
	}

	[Fact]
	public void Resolve_MissingSourceFile_SkipsWithoutThrowing()
	{
		var fs = new MockFileSystem();
		var config = new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = "Test",
				Logo = new SiteAssetReference
				{
					RawValue = "assets/missing.png",
					SourcePath = fs.Path.GetFullPath($"{RootDir}/assets/missing.png"),
					PublishUrl = "/assets/missing.png",
					IsAbsoluteUrl = false
				}
			},
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		Action act = () => CreateResolver(fs).Resolve(ctx);

		act.Should().NotThrow();
		ctx.BrandAssetFiles.Should().BeEmpty();
	}

	[Fact]
	public void Resolve_AbsoluteUrl_DoesNotAddToBrandAssetFiles()
	{
		var fs = new MockFileSystem();
		var config = new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = "Test",
				Logo = new SiteAssetReference
				{
					RawValue = "https://cdn.example.com/logo.png",
					SourcePath = null,
					PublishUrl = "https://cdn.example.com/logo.png",
					IsAbsoluteUrl = true
				}
			},
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		CreateResolver(fs).Resolve(ctx);

		// Absolute-URL assets pass through to the template via ResolveBrandUrl;
		// BrandAssetFiles is filesystem-copy only, so it stays empty.
		ctx.BrandAssetFiles.Should().BeEmpty();
	}

	[Fact]
	public void Resolve_LogoAndFavicon_BothResolved()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[$"{RootDir}/assets/logo.png"] = new("LOGO"),
			[$"{RootDir}/assets/favicon.ico"] = new("ICO")
		});
		var config = new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = "Test",
				Logo = new SiteAssetReference
				{
					RawValue = "assets/logo.png",
					SourcePath = fs.Path.GetFullPath($"{RootDir}/assets/logo.png"),
					PublishUrl = "/assets/logo.png",
					IsAbsoluteUrl = false
				},
				Favicon = new SiteAssetReference
				{
					RawValue = "assets/favicon.ico",
					SourcePath = fs.Path.GetFullPath($"{RootDir}/assets/favicon.ico"),
					PublishUrl = "/assets/favicon.ico",
					IsAbsoluteUrl = false
				}
			},
			Content = new ContentConfig { Docs = "." }
		};
		BuildContext ctx = CreateContext(fs, config);

		CreateResolver(fs).Resolve(ctx);

		ctx.BrandAssetFiles.Should().HaveCount(2);
		ctx.BrandAssetFiles.Should().ContainKeys("/assets/logo.png", "/assets/favicon.ico");
	}
}
