using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class RelativeLinksTests
{
	[Theory]
	[InlineData("./api-docs", "guide/markdown.md", "/guide/api-docs")]
	[InlineData("api-docs.md#configuration", "guide/markdown.md", "/guide/api-docs#configuration")]
	[InlineData("../configuration/navigation.md", "guide/markdown.md", "/configuration/navigation")]
	[InlineData("./intro", "guide/index.md", "/guide/intro")]
	[InlineData("./index.md", "guide/markdown.md", "/guide")]
	[InlineData("../index.md", "guide/markdown.md", "/")]
	[InlineData("img/diagram.png", "guide/markdown.md", "/guide/img/diagram.png")]
	[InlineData("setup.md?tab=cli#install", "index.md", "/setup?tab=cli#install")]
	[InlineData(".\\windows\\style.md", "guide\\markdown.md", "/guide/windows/style")]
	public void ToRootRelative_ResolvesAgainstTheLinkingFile(string url, string source, string expected) =>
		RelativeLinks.ToRootRelative(url, source).Should().Be(expected);

	[Theory]
	[InlineData("/guide/intro")]
	[InlineData("//cdn.example.com/lib.js")]
	[InlineData("#section")]
	[InlineData("https://example.com/page.md")]
	[InlineData("mailto:team@example.com")]
	[InlineData("../../outside.md")]
	[InlineData("")]
	public void ToRootRelative_LeavesNonRelativeLinksAlone(string url) =>
		RelativeLinks.ToRootRelative(url, "guide/markdown.md").Should().BeNull();

	[Fact]
	public void Parse_WithASourcePath_RewritesLinksAndImagesButNotCode()
	{
		// Relative links were emitted as written, which broke on hosts that add a trailing
		// slash, and .md links never worked at all.
		const string md = """
		                  ---
		                  title: Links
		                  ---
		                  See [the API guide](api-docs.md#setup) and ![diagram](img/flow.png).

		                  Inline `[not a link](other.md)` stays.
		                  """;

		string html = new MarkdownParser().Parse(md, "guide/markdown.md").Html;

		html.Should().Contain("href=\"/guide/api-docs#setup\"")
			.And.Contain("src=\"/guide/img/flow.png\"")
			.And.Contain("[not a link](other.md)");
	}

	[Fact]
	public void Parse_WithoutASourcePath_KeepsLinksAsWritten()
	{
		string html = new MarkdownParser().Parse("---\ntitle: T\n---\n[x](api-docs.md)").Html;

		html.Should().Contain("href=\"api-docs.md\"");
	}
}
