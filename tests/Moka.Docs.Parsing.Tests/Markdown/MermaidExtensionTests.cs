using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class MermaidExtensionTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<MermaidExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, _pipeline);

	[Fact]
	public void MermaidBlock_ProducesPreWithMermaidClass()
	{
		const string md = """
		                  ```mermaid
		                  graph TD
		                      A --> B
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("<pre class=\"mermaid\">");
		html.Should().Contain("graph TD");
		html.Should().Contain("A --> B");
		html.Should().NotContain("<code");
	}

	[Fact]
	public void MermaidBlock_CaseInsensitive()
	{
		const string md = """
		                  ```Mermaid
		                  graph LR
		                      A --> B
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("<pre class=\"mermaid\">");
	}

	[Fact]
	public void RegularCodeBlock_NotAffected()
	{
		const string md = """
		                  ```csharp
		                  Console.WriteLine("Hello");
		                  ```
		                  """;

		string html = Render(md);

		html.Should().NotContain("<pre class=\"mermaid\">");
		html.Should().Contain("<code");
		html.Should().Contain("language-csharp");
	}

	[Fact]
	public void EmptyMermaidBlock_RendersEmptyPre()
	{
		const string md = """
		                  ```mermaid
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("<pre class=\"mermaid\">");
		html.Should().Contain("</pre>");
	}

	[Fact]
	public void MermaidBlock_PreservesRawContent()
	{
		const string md = """
		                  ```mermaid
		                  sequenceDiagram
		                      Alice->>Bob: Hello Bob
		                      Bob-->>Alice: Hi Alice
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("sequenceDiagram");
		html.Should().Contain("Alice->>Bob");
	}

	[Fact]
	public void MultipleMermaidBlocks_AllRendered()
	{
		const string md = """
		                  ```mermaid
		                  graph TD
		                      A --> B
		                  ```

		                  Some text in between.

		                  ```mermaid
		                  pie
		                      "A" : 50
		                      "B" : 50
		                  ```
		                  """;

		string html = Render(md);

		// Both mermaid blocks should be rendered
		int mermaidCount = html.Split("<pre class=\"mermaid\">").Length - 1;
		mermaidCount.Should().Be(2);
	}

	[Fact]
	public void NonCodeBlock_NotAffected()
	{
		const string md = "This is a paragraph with the word mermaid in it.";

		string html = Render(md);

		html.Should().NotContain("<pre class=\"mermaid\">");
		html.Should().Contain("mermaid");
	}
}
