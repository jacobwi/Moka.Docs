using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Engine;

namespace Moka.Docs.Engine.Tests;

public sealed class OutputDirectoryGuardTests
{
	private static readonly string _root = Path.Combine(Path.GetTempPath(), "guard-project");
	private static readonly string _docs = Path.Combine(_root, "docs");

	private static string? Check(string output, string? docs = null) =>
		OutputDirectoryGuard.FindConflict(_root, docs ?? _docs, Path.Combine(_root, output));

	[Theory]
	[InlineData("_site")]
	[InlineData("docs/_site")]
	[InlineData("../guard-project-site")]
	[InlineData("docs-out")]
	public void FindConflict_DedicatedFolders_AreSafe(string output) =>
		Check(output).Should().BeNull();

	[Theory]
	[InlineData(".")]
	[InlineData("./")]
	[InlineData("..")]
	public void FindConflict_TheProjectOrAnAncestor_IsRefused(string output) =>
		Check(output).Should().Contain("contains the project");

	[Theory]
	[InlineData("docs")]
	[InlineData("./docs/")]
	[InlineData("DOCS")]
	public void FindConflict_TheDocsFolder_IsRefused(string output) =>
		Check(output).Should().Contain("contains the docs folder");

	[Fact]
	public void FindConflict_AFolderThatHoldsTheDocs_IsRefused()
	{
		string docsInsideSite = Path.Combine(_root, "site", "docs");

		Check("site", docsInsideSite).Should().Contain("contains the docs folder");
	}

	[Fact]
	public void FindConflict_ASiblingSharingANamePrefix_IsSafe()
	{
		// "docs-out" starts with "docs" but is not inside it.
		Check("docs-out").Should().BeNull();
		OutputDirectoryGuard.FindConflict(_root, _docs, _root + "-site").Should().BeNull();
	}

	[Fact]
	public void FindConflict_TheFileSystemRoot_IsRefused()
	{
		string driveRoot = Path.GetPathRoot(_root)!;

		OutputDirectoryGuard.FindConflict(_root, _docs, driveRoot).Should().Contain("contains the project");
	}

	private static SiteConfig ConfigWith(Func<SiteConfig, SiteConfig> change) => change(new SiteConfig
	{
		Site = new SiteMetadata { Title = "Guard" },
		Content = new ContentConfig { Docs = "./docs" }
	});

	[Theory]
	[InlineData("src")]
	[InlineData("src/MyLib")]
	public void FindConflict_AFolderHoldingAConfiguredProject_IsRefused(string output)
	{
		// output: ./src passed validate, and clean deleted the project with its csproj.
		SiteConfig config = ConfigWith(c => c with
		{
			Content = c.Content with { Projects = [new ProjectSource { Path = "src/MyLib/MyLib.csproj" }] }
		});

		OutputDirectoryGuard.FindConflict(config, _root, Path.Combine(_root, output))
			.Should().Contain("content.projects[0]");
	}

	[Fact]
	public void FindConflict_AFolderInsideAProject_IsSafe()
	{
		SiteConfig config = ConfigWith(c => c with
		{
			Content = c.Content with { Projects = [new ProjectSource { Path = "src/MyLib/MyLib.csproj" }] }
		});

		OutputDirectoryGuard.FindConflict(config, _root, Path.Combine(_root, "src", "MyLib", "_site"))
			.Should().BeNull();
	}

	[Fact]
	public void FindConflict_TheCustomThemeFolder_IsRefused()
	{
		SiteConfig config = ConfigWith(c => c with { Theme = new ThemeConfig { Name = "./themes/brand" } });

		OutputDirectoryGuard.FindConflict(config, _root, Path.Combine(_root, "themes"))
			.Should().Contain("theme folder");
		OutputDirectoryGuard.FindConflict(config with { Theme = new ThemeConfig { Name = "default" } }, _root,
			Path.Combine(_root, "themes")).Should().BeNull();
	}

	[Fact]
	public void FindConflict_AFolderHoldingTheLogoSource_IsRefused()
	{
		SiteConfig parsed = SiteConfigReader.Parse("site:\n  title: Guard\n  logo: ../branding/logo.svg\n", _root);

		OutputDirectoryGuard.FindConflict(parsed, _root, Path.Combine(Path.GetDirectoryName(_root)!, "branding"))
			.Should().Contain("site.logo");
	}

	[Fact]
	public void FindForeignContent_MissingOrEmptyOrMokaDocsFolders_MayBeDeleted()
	{
		var fs = new MockFileSystem();
		fs.Directory.CreateDirectory("/project/empty");
		fs.AddFile("/project/marked/" + OutputDirectoryGuard.MarkerFileName, new MockFileData(""));
		fs.AddFile("/project/marked/index.html", new MockFileData("<html/>"));
		fs.AddFile("/project/older/_theme/css/main.css", new MockFileData(""));
		fs.AddFile("/project/older/index.html", new MockFileData("<html/>"));

		OutputDirectoryGuard.FindForeignContent(fs, "/project/missing").Should().BeNull();
		OutputDirectoryGuard.FindForeignContent(fs, "/project/empty").Should().BeNull();
		OutputDirectoryGuard.FindForeignContent(fs, "/project/marked").Should().BeNull();
		OutputDirectoryGuard.FindForeignContent(fs, "/project/older").Should().BeNull();
	}

	[Fact]
	public void FindForeignContent_AFolderMokaDocsDidNotWrite_IsRefused()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/project/wwwroot/index.html", new MockFileData("<html/>"));

		OutputDirectoryGuard.FindForeignContent(fs, "/project/wwwroot")
			.Should().Contain("Refusing to delete").And.Contain(OutputDirectoryGuard.MarkerFileName);
	}
}
