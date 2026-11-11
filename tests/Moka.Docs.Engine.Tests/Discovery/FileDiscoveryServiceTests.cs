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
		List<ProjectSource>? projects = null,
		string output = "./_site")
	{
		return new SiteConfig
		{
			Site = new SiteMetadata { Title = "Test" },
			Content = new ContentConfig
			{
				Docs = docs,
				Projects = projects ?? []
			},
			Build = new BuildConfig { Output = output }
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

	[Fact]
	public void Discover_OutputInsideDocs_SkipsPreviousBuildOutput()
	{
		// build.output pointing inside content.docs is a reasonable setup. Without an
		// exclusion, each build ingests the last build's assets and nests a copy of the
		// site inside itself.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/logo.png", new MockFileData("img") },
			{ "/project/docs/_site/index.html", new MockFileData("<html></html>") },
			{ "/project/docs/_site/search-index.json", new MockFileData("[]") },
			{ "/project/docs/_site/_theme/css/main.css", new MockFileData("body{}") },
			{ "/project/docs/_site/stale.md", new MockFileData("# Stale") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig(output: "./docs/_site"));

		result.MarkdownFiles.Should().ContainSingle().Which.Should().Be("index.md");
		result.AssetFiles.Should().ContainSingle().Which.Should().Be("logo.png");
	}

	[Fact]
	public void Discover_OutputOutsideDocs_StillFindsEverything()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/logo.png", new MockFileData("img") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig(output: "./_site"));

		result.MarkdownFiles.Should().ContainSingle();
		result.AssetFiles.Should().ContainSingle();
	}

	[Fact]
	public void Discover_DirectoryMerelyStartingWithOutputName_IsNotExcluded()
	{
		// "_sitemap" must not be swallowed by a prefix match on "_site".
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/_sitemap/notes.md", new MockFileData("# Notes") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig(output: "./docs/_site"));

		result.MarkdownFiles.Should().HaveCount(2);
	}

	[Fact]
	public void Discover_HostFilesAtTheDocsRoot_AreCopiedButNotElsewhere()
	{
		// CNAME has no extension, so the GitHub Pages custom domain file was never copied.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/CNAME", new MockFileData("docs.example.com") },
			{ "/project/docs/_redirects", new MockFileData("/old /new 301") },
			{ "/project/docs/guide/CNAME", new MockFileData("not at the root") },
			{ "/project/docs/notes", new MockFileData("extensionless, not a host file") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.AssetFiles.Should().BeEquivalentTo("CNAME", "_redirects");
	}

	[Fact]
	public void Discover_MarkdownExtensionInAnyCase_IsAPage()
	{
		// The "*.md" glob is case sensitive on Linux: Readme.MD was a page on Windows only.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("# Home") },
			{ "/project/docs/Readme.MD", new MockFileData("# Readme") },
			{ "/project/docs/guide/Notes.Md", new MockFileData("# Notes") },
			{ "/project/docs/guide/notes.mdx", new MockFileData("not markdown for us") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig());

		result.MarkdownFiles.Select(f => f.Replace('\\', '/'))
			.Should().BeEquivalentTo("index.md", "Readme.MD", "guide/Notes.Md");
	}

	[Fact]
	public void Discover_MissingDocsFolder_IsReported()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/mokadocs.yaml", new MockFileData("site:\n  title: T\n") }
		});

		var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
		DiscoveryResult result = service.Discover("/project", CreateConfig(docs: "./documentation"));

		result.DocsDirectoryMissing.Should().BeTrue();
		result.DocsDirectory.Should().EndWith("documentation");
		new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance)
			.Discover("/project", CreateConfig(docs: "."))
			.DocsDirectoryMissing.Should().BeFalse();
	}

	[Fact]
	public void Discover_ADirectoryLinkBackToAnAncestor_IsSkippedWithAWarning()
	{
		// A link docs/guide/loop -> docs built 82 pages from 2, walking the loop until the path
		// got too long, and said nothing.
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-discovery-" + Guid.NewGuid().ToString("N"));
		string docs = Path.Combine(root, "docs");
		Directory.CreateDirectory(Path.Combine(docs, "guide"));
		Directory.CreateDirectory(Path.Combine(root, "shared"));
		File.WriteAllText(Path.Combine(docs, "index.md"), "# Home");
		File.WriteAllText(Path.Combine(docs, "guide", "intro.md"), "# Intro");
		File.WriteAllText(Path.Combine(root, "shared", "faq.md"), "# FAQ");
		try
		{
			try
			{
				Directory.CreateSymbolicLink(Path.Combine(docs, "guide", "loop"), docs);
				Directory.CreateSymbolicLink(Path.Combine(docs, "shared"), Path.Combine(root, "shared"));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Assert.Skip($"This machine does not allow creating symbolic links ({ex.Message})");
			}

			var fs = new System.IO.Abstractions.FileSystem();
			var service = new FileDiscoveryService(fs, NullLogger<FileDiscoveryService>.Instance);
			DiscoveryResult result = service.Discover(root, CreateConfig());

			result.MarkdownFiles.Select(f => f.Replace('\\', '/'))
				.Should().BeEquivalentTo("index.md", "guide/intro.md", "shared/faq.md");
			result.Warnings.Should().ContainSingle().Which.Should().Contain("loop");
		}
		finally
		{
			foreach (string link in new[] { Path.Combine(docs, "guide", "loop"), Path.Combine(docs, "shared") })
			{
				if (Directory.Exists(link))
				{
					Directory.Delete(link);
				}
			}

			Directory.Delete(root, true);
		}
	}
}
