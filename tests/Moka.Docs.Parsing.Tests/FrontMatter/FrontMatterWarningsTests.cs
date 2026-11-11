using FluentAssertions;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

/// <summary>
///     A misspelled front matter key was ignored without a word, and a YAML error said nothing
///     about where it was.
/// </summary>
public sealed class FrontMatterWarningsTests
{
	private readonly FrontMatterExtractor _extractor = new();

	[Fact]
	public void UnknownKey_IsReportedWithItsLine()
	{
		const string input = "---\ntitle: Ok\ntitel: Typo\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.Warnings.Should().Equal("unknown front matter key 'titel' (line 3)");
	}

	[Fact]
	public void KeyThatDiffersOnlyInCase_SuggestsTheRealKey()
	{
		const string input = "---\nTitle: Wrong case\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Warnings.Should().ContainSingle()
			.Which.Should().Contain("'Title' (line 2)").And.Contain("did you mean 'title'");
	}

	[Fact]
	public void UnknownKeyInAFeatureCard_IsReported()
	{
		const string input = """
		                     ---
		                     title: Home
		                     features:
		                       - title: Fast
		                         descripton: Typo
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.Warnings.Should().Equal("unknown front matter key 'features[].descripton' (line 5)");
	}

	[Fact]
	public void LineNumbers_CountFromTheTopOfTheFile()
	{
		const string input = "\n\n---\ntitle: X\nbogus: 1\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.Warnings.Should().Equal("unknown front matter key 'bogus' (line 5)");
	}

	[Fact]
	public void KnownKeys_GiveNoWarnings()
	{
		const string input = """
		                     ---
		                     title: All
		                     description: d
		                     order: 1
		                     icon: rocket
		                     layout: landing
		                     tags: [a]
		                     visibility: hidden
		                     toc: false
		                     expanded: false
		                     route: /all
		                     version: "1"
		                     requires: Flag
		                     featuresTitle: t
		                     featuresSubtitle: s
		                     features:
		                       - title: a
		                         description: b
		                         icon: zap
		                         link: /x
		                     ---
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.Error.Should().BeNull();
		result.Warnings.Should().BeEmpty();
	}

	[Fact]
	public void SyntaxError_NamesLineAndColumn()
	{
		const string input = "---\ntitle: Ok\ntags: [unclosed\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.Error.Should().StartWith("line ").And.Contain("column ");
		result.Error.Should().NotContain("Idx:");
	}

	[Fact]
	public void WrongValueType_NamesTheLineAndTheInnerMessage()
	{
		const string input = "---\ntitle: Ok\norder: first\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.Error.Should().StartWith("line 3, column 8: ");
		result.Error.Should().Contain("first");
		result.Body.Should().Be("Body");
	}
}
