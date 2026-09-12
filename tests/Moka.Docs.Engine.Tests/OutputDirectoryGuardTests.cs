using FluentAssertions;
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
}
