using FluentAssertions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

public sealed class FileWatcherTests
{
	private static readonly string _root = Path.Combine(Path.GetTempPath(), ".projects", "site");
	private static readonly string _docs = Path.Combine(_root, "docs");

	[Fact]
	public void IsIgnored_ProjectUnderADotFolder_StillRebuilds()
	{
		// The old check ignored any path containing "\.", so a project stored under a folder
		// like ~/.projects never rebuilt.
		FileWatcher.IsIgnored(Path.Combine(_docs, "guide", "intro.md"), _docs, []).Should().BeFalse();
	}

	[Fact]
	public void IsIgnored_FileNameContainingSite_StillRebuilds()
	{
		FileWatcher.IsIgnored(Path.Combine(_docs, "my_site.md"), _docs, []).Should().BeFalse();
	}

	[Theory]
	[InlineData(".git", "HEAD")]
	[InlineData("guide", ".draft.md")]
	public void IsIgnored_DotFilesAndFoldersBelowTheWatchedFolder_AreIgnored(string folder, string file)
	{
		FileWatcher.IsIgnored(Path.Combine(_docs, folder, file), _docs, []).Should().BeTrue();
	}

	[Fact]
	public void IsIgnored_OutputInsideTheDocsFolder_IsIgnoredWhateverItsName()
	{
		// Output at docs/public used to trigger a rebuild on every write, rebuilding endlessly.
		string output = Path.Combine(_docs, "public");

		FileWatcher.IsIgnored(Path.Combine(output, "guide", "index.html"), _docs, [output]).Should().BeTrue();
		FileWatcher.IsIgnored(output, _docs, [output]).Should().BeTrue();
		FileWatcher.IsIgnored(Path.Combine(_docs, "public-notes.md"), _docs, [output]).Should().BeFalse();
	}

	[Fact]
	public void IsIgnored_ConfigFileNextToTheDocsFolder_Rebuilds()
	{
		FileWatcher.IsIgnored(Path.Combine(_root, "mokadocs.yaml"), _root, [Path.Combine(_docs, "_site")])
			.Should().BeFalse();
	}
}
