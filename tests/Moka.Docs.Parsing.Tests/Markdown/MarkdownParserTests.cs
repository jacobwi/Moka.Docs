using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class MarkdownParserTests
{
	private readonly MarkdownParser _parser = new();

	[Fact]
	public void Parse_BasicMarkdown_ReturnsHtml()
	{
		const string md = """
		                  ---
		                  title: Test Page
		                  ---
		                  # Heading One

		                  This is a paragraph with **bold** and *italic* text.
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.FrontMatter.Title.Should().Be("Test Page");
		result.Html.Should().Contain("<h1");
		result.Html.Should().Contain("Heading One");
		result.Html.Should().Contain("<strong>bold</strong>");
		result.Html.Should().Contain("<em>italic</em>");
	}

	[Fact]
	public void Parse_GeneratesTableOfContents()
	{
		const string md = """
		                  ---
		                  title: ToC Test
		                  ---
		                  # Introduction
		                  ## Getting Started
		                  ## Installation
		                  ### Prerequisites
		                  ## Usage
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		// The h1 is the page's title, so the h2 headings are the roots.
		result.TableOfContents.Entries.Select(e => e.Text).Should().Equal("Getting Started", "Installation", "Usage");
		result.TableOfContents.Entries.Should().OnlyContain(e => e.Level == 2);
		result.TableOfContents.Entries[1].Children.Should().ContainSingle().Which.Text.Should().Be("Prerequisites");
	}

	[Fact]
	public void Parse_ExtractsPlainText()
	{
		const string md = """
		                  ---
		                  title: Plain Text
		                  ---
		                  # Hello

		                  This is **some** text.
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.PlainText.Should().Contain("Hello");
		result.PlainText.Should().Contain("This is some text.");
	}

	[Fact]
	public void Parse_Tables_RenderedCorrectly()
	{
		const string md = """
		                  ---
		                  title: Table Test
		                  ---
		                  | Name | Value |
		                  |------|-------|
		                  | Foo  | 42    |
		                  | Bar  | 99    |
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("<table>");
		result.Html.Should().Contain("<th>Name</th>");
		result.Html.Should().Contain("<td>Foo</td>");
	}

	[Fact]
	public void Parse_FencedCodeBlock_RenderedWithLanguageClass()
	{
		const string md = """
		                  ---
		                  title: Code
		                  ---
		                  ```csharp
		                  Console.WriteLine("Hello");
		                  ```
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("<code class=\"language-csharp\" data-highlighted=\"true\"");
		result.Html.Should().Contain("<span class=\"token type-name\">Console</span>.<span class=\"token type-name\">WriteLine</span>");
	}

	[Fact]
	public void Parse_TaskLists_Rendered()
	{
		const string md = """
		                  ---
		                  title: Tasks
		                  ---
		                  - [x] Done
		                  - [ ] Not done
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("checked");
		result.Html.Should().Contain("type=\"checkbox\"");
	}

	[Fact]
	public void Parse_Links_RenderedAsAnchors()
	{
		const string md = """
		                  ---
		                  title: Links
		                  ---
		                  Visit [MokaDocs](https://mokadocs.dev) for more info.
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("<a href=\"https://mokadocs.dev\"");
		result.Html.Should().Contain("MokaDocs</a>");
	}

	[Fact]
	public void Parse_WithoutFrontMatter_StillWorks()
	{
		const string md = "# No Front Matter\n\nJust content.";

		MarkdownParseResult result = _parser.Parse(md);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Html.Should().Contain("No Front Matter");
		result.Html.Should().Contain("Just content.");
	}

	[Fact]
	public void Parse_HeadingsGetAnchorIds()
	{
		const string md = """
		                  ---
		                  title: Anchors
		                  ---
		                  ## Getting Started
		                  ## API Reference
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("id=\"getting-started\"");
		result.Html.Should().Contain("id=\"api-reference\"");
	}

	[Fact]
	public void Parse_Footnotes_Rendered()
	{
		const string md = """
		                  ---
		                  title: Footnotes
		                  ---
		                  This has a footnote[^1].

		                  [^1]: This is the footnote content.
		                  """;

		MarkdownParseResult result = _parser.Parse(md);

		result.Html.Should().Contain("footnote");
	}

	[Fact]
	public void Parse_EmptyInput_ReturnsEmptyResult()
	{
		MarkdownParseResult result = _parser.Parse("");

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Html.Should().BeEmpty();
	}
}
