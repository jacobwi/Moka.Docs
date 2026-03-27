using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Engine.Discovery;

namespace Moka.Docs.Engine.Tests.Discovery;

public sealed class FileDiscoveryServiceTests
{
	private static SiteConfig CreateConfig(
		string docs = "./docs",
		List<ProjectSource>? projects = null)
	{
		return new SiteConfig
		{
			Site = new SiteMetadata { Title = "Test" },
			Content = new ContentConfig
			{
				Docs = docs,
				Projects = projects ?? []
			}
		};
	}

	[Fact]
	public void Discover_WithMarkdownFiles_ReturnsAllMdFiles()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/guide/getting-started.md", new MockFileData("# Getting Started") },
			{ "/project/docs/guide/advanced.md", new MockFileData("# Advanced") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.MarkdownFiles.Should().HaveCount(3);
		result.MarkdownFiles.Should().Contain("index.md");
		result.MarkdownFiles.Should().Contain(fs.Path.Combine("guide", "getting-started.md"));
		result.MarkdownFiles.Should().Contain(fs.Path.Combine("guide", "advanced.md"));
	}

	[Fact]
	public void Discover_WithNoDocsDirectory_ReturnsEmpty()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>());

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.MarkdownFiles.Should().BeEmpty();
	}

	[Fact]
	public void Discover_WithAssetFiles_ReturnsAssets()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/images/logo.png", new MockFileData(new byte[] { 0x89, 0x50 }) },
			{ "/project/docs/images/hero.jpg", new MockFileData(new byte[] { 0xFF, 0xD8 }) },
			{ "/project/docs/assets/style.css", new MockFileData("body {}") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.MarkdownFiles.Should().HaveCount(1);
		result.AssetFiles.Should().HaveCount(3);
		result.AssetFiles.Should().Contain(f => f.EndsWith("logo.png"));
		result.AssetFiles.Should().Contain(f => f.EndsWith("hero.jpg"));
		result.AssetFiles.Should().Contain(f => f.EndsWith("style.css"));
	}

	[Fact]
	public void Discover_WithProjectFiles_ReturnsExistingProjects()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/src/MyLib/MyLib.csproj", new MockFileData("<Project/>") }
		});

		SiteConfig config = CreateConfig(projects:
		[
			new ProjectSource { Path = "/src/MyLib/MyLib.csproj" },
			new ProjectSource { Path = "/src/Missing/Missing.csproj" }
		]);

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", config);

		result.ProjectFiles.Should().HaveCount(1);
		result.ProjectFiles[0].Should().Be("/src/MyLib/MyLib.csproj");
	}

	[Fact]
	public void Discover_WithCustomDocsPath_UsesConfiguredPath()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/documentation/intro.md", new MockFileData("# Intro") }
		});

		SiteConfig config = CreateConfig("./documentation");

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", config);

		result.MarkdownFiles.Should().HaveCount(1);
		result.MarkdownFiles.Should().Contain("intro.md");
	}

	[Fact]
	public void Discover_TotalCount_SumsAllFileTypes()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/guide.md", new MockFileData("# Guide") },
			{ "/project/docs/logo.png", new MockFileData(new byte[] { 0x89 }) }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.TotalCount.Should().Be(3);
	}

	[Fact]
	public void Discover_MarkdownFiles_AreSortedAlphabetically()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/zebra.md", new MockFileData("# Z") },
			{ "/project/docs/alpha.md", new MockFileData("# A") },
			{ "/project/docs/middle.md", new MockFileData("# M") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.MarkdownFiles.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
	}
}
