using FluentAssertions;
using Moka.Docs.Parsing.FrontMatter;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

/// <summary>
///     Front matter delimiters are <c>---</c> lines at column 0. An indented <c>---</c> inside a
///     block scalar used to close the block, a missing closer was silent, and a body opening with
///     a <c>---</c> rule was read as a second YAML block and lost up to the next <c>---</c>.
/// </summary>
public sealed class FrontMatterDelimiterTests
{
	private readonly FrontMatterExtractor _extractor = new();

	[Fact]
	public void IndentedDashesInsideABlockScalar_DoNotCloseTheBlock()
	{
		const string input = "---\ntitle: X\ndescription: |\n  first\n  ---\n  second\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.Error.Should().BeNull();
		result.FrontMatter.Description.Should().Be("first\n---\nsecond\n");
		result.Body.Should().Be("Body");
	}

	[Fact]
	public void MissingClosingDelimiter_WarnsAndKeepsTheTextAsBody()
	{
		const string input = "---\ntitle: Unclosed\nStill no closing delimiter";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be(input);
		result.Warnings.Should().ContainSingle().Which.Should().Contain("no closing '---'");
	}

	[Fact]
	public void IndentedOpeningDashes_AreNotFrontMatter()
	{
		const string input = "  ---\ntitle: X\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be(input);
		result.Warnings.Should().BeEmpty();
	}

	[Fact]
	public void LongerDashRun_IsNotADelimiter()
	{
		const string input = "-----\ntitle: X\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be(input);
	}

	[Fact]
	public void ByteOrderMarkAndLeadingBlankLines_AreAllowed()
	{
		const string input = "\uFEFF\n\n---\ntitle: With BOM\n---\nBody";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("With BOM");
		result.Body.Should().Be("Body");
	}

	[Fact]
	public void CrLfLineEndings_AreRead()
	{
		const string input = "---\r\ntitle: Windows\r\n---\r\nBody\r\n";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Windows");
		result.Body.Should().Be("Body\r\n");
	}

	[Fact]
	public void BodyOpeningWithARule_KeepsItsContent()
	{
		var parser = new MarkdownParser();

		MarkdownParseResult result = parser.Parse("---\ntitle: X\n---\n---\nKept text\n\n---\nAfter");

		result.FrontMatter.Title.Should().Be("X");
		result.Html.Should().Contain("<hr />").And.Contain("Kept text").And.Contain("After");
	}

	[Fact]
	public void InvalidFrontMatter_IsNotRenderedAsContent()
	{
		var parser = new MarkdownParser();

		MarkdownParseResult result = parser.Parse("---\ntitle: [unclosed\n---\nBody text");

		result.FrontMatterError.Should().NotBeNull();
		result.Html.Should().Contain("Body text").And.NotContain("unclosed");
	}

	[Fact]
	public void MissingClosingDelimiter_ReachesTheParseResult()
	{
		var parser = new MarkdownParser();

		MarkdownParseResult result = parser.Parse("---\ntitle: X\n\nText");

		result.Warnings.Should().ContainSingle().Which.Should().Contain("no closing '---'");
		result.Html.Should().Contain("Text");
	}
}
