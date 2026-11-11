using FluentAssertions;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

/// <summary>
///     <c>version</c> takes one range or a list of them. A list used to fail the whole front matter
///     block, because the key was read as a single string.
/// </summary>
public sealed class VersionFrontMatterTests
{
	private readonly FrontMatterExtractor _extractor = new();

	[Theory]
	[InlineData("version: \">=2.0\"", ">=2.0")]
	[InlineData("version: 2.0", "2.0")]
	[InlineData("version: [\"1.x\", \">=3.0\"]", "1.x || >=3.0")]
	[InlineData("version:\n  - \"^1.2\"\n  - \"[3.0,4.0)\"", "^1.2 || [3.0,4.0)")]
	public void Version_IsReadAsOneRange(string yaml, string expected)
	{
		FrontMatterResult result = _extractor.Extract($"---\ntitle: Page\n{yaml}\n---\nBody");

		result.Error.Should().BeNull();
		result.Warnings.Should().BeEmpty();
		result.FrontMatter.Title.Should().Be("Page");
		result.FrontMatter.Version.Should().Be(expected);
	}

	[Theory]
	[InlineData("version:\n  min: 1.0")]
	[InlineData("version:\n  - [1.0, 2.0]")]
	public void Version_ThatIsNotARangeOrAList_IsIgnoredWithAWarning(string yaml)
	{
		FrontMatterResult result = _extractor.Extract($"---\ntitle: Page\n{yaml}\n---\nBody");

		result.Error.Should().BeNull();
		result.FrontMatter.Title.Should().Be("Page");
		result.FrontMatter.Version.Should().BeNull();
		result.Warnings.Should().ContainSingle().Which.Should().Contain("'version' must be a version range");
	}
}
