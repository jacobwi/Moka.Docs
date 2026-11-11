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
	public void Parse_RawHtmlBlock_RewritesRelativeSrcHrefAndSrcset()
	{
		// Raw <img src="./x"> was written as is and resolved against the page URL, one folder too deep
		// once a host added the trailing slash.
		const string md = """
		                  ---
		                  title: Raw
		                  ---
		                  <p align="center">
		                    <img src="./img/logo.png" width="200"
		                         srcset="img/logo@2x.png 2x, ../shared/logo.png, img/logo-3x.png 3x">
		                    <a href='other.md#top'>Other</a> <video poster=media/poster.jpg></video>
		                  </p>
		                  """;

		string html = new MarkdownParser().Parse(md, "guide/page.md").Html;

		html.Should().Contain("src=\"/guide/img/logo.png\"")
			.And.Contain("srcset=\"/guide/img/logo@2x.png 2x, /shared/logo.png, /guide/img/logo-3x.png 3x\"")
			.And.Contain("href='/guide/other#top'")
			.And.Contain("poster=/guide/media/poster.jpg")
			.And.Contain("width=\"200\"");
	}

	[Fact]
	public void Parse_RawHtmlInline_RewritesARelativeImage()
	{
		string html = new MarkdownParser().Parse("Logo: <img src=\"pics/a.png\" alt=\"a\"> here.", "docs/intro.md").Html;

		html.Should().Contain("<img src=\"/docs/pics/a.png\" alt=\"a\">");
	}

	[Theory]
	[InlineData("<img src=\"https://example.com/a.png\">")]
	[InlineData("<img src=\"/root/a.png\">")]
	[InlineData("<img src=\"//cdn.example.com/a.png\">")]
	[InlineData("<img src=\"data:image/png;base64,AAAA\">")]
	[InlineData("<a href=\"#local\">x</a>")]
	[InlineData("<a href=\"../../outside.md\">x</a>")]
	public void Parse_RawHtml_LeavesNonRelativeUrlsAlone(string tag)
	{
		string html = new MarkdownParser().Parse(tag + "\n", "guide/page.md").Html;

		html.Should().Contain(tag);
	}

	[Fact]
	public void Parse_RawHtml_LeavesCodeCommentsAndScriptTextAlone()
	{
		const string md = """
		                  ```html
		                  <img src="./in-a-code-block.png">
		                  ```

		                  Inline `<img src="./in-code.png">` code.

		                  <!-- <img src="./commented.png"> -->

		                  <script>
		                  const tag = '<img src="./in-script.png">';
		                  </script>
		                  """;

		string html = new MarkdownParser().Parse(md, "guide/page.md").Html;

		html.Should().Contain("&lt;img src=&quot;./in-a-code-block.png&quot;&gt;")
			.And.Contain("&lt;img src=&quot;./in-code.png&quot;&gt;")
			.And.Contain("<img src=\"./commented.png\">")
			.And.Contain("'<img src=\"./in-script.png\">'")
			.And.NotContain("/guide/");
	}

	[Fact]
	public void Parse_WithoutASourcePath_KeepsLinksAsWritten()
	{
		string html = new MarkdownParser().Parse("---\ntitle: T\n---\n[x](api-docs.md)").Html;

		html.Should().Contain("href=\"api-docs.md\"");
	}
}
