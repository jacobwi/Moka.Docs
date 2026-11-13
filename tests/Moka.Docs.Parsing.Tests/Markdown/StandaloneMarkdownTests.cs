using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class StandaloneMarkdownTests
{
	private const string _site = "https://example.com/docs";

	private readonly MarkdownParser _parser = new();

	[Fact]
	public void ToStandaloneMarkdown_DropsFrontMatterAndKeepsTheBody()
	{
		string markdown = _parser.ToStandaloneMarkdown(
			"---\ntitle: Intro\ndescription: Hi\n---\n# Intro\n\nText with **bold**.\n", "guide/intro.md", "Intro", _site);

		markdown.Should().Be("# Intro\n\nText with **bold**.\n");
	}

	[Fact]
	public void ToStandaloneMarkdown_MakesRelativeAndRootLinksAbsolute()
	{
		const string source = """
		                      # Links

		                      See [setup](./setup.md#install), [the API](../api/index.md) and [home](/).
		                      ![Diagram](images/flow.png "Flow")
		                      Also [config](/configuration/site-config).
		                      """;

		string markdown = _parser.ToStandaloneMarkdown(source, "guide/intro.md", "Links", _site);

		markdown.Should().Contain("[setup](https://example.com/docs/guide/setup#install)")
			.And.Contain("[the API](https://example.com/docs/api)")
			.And.Contain("[home](https://example.com/docs/)")
			.And.Contain("![Diagram](https://example.com/docs/guide/images/flow.png \"Flow\")")
			.And.Contain("[config](https://example.com/docs/configuration/site-config)");
	}

	[Fact]
	public void ToStandaloneMarkdown_LeavesExternalLinksAnchorsAndCodeAlone()
	{
		const string source = """
		                      # Page

		                      [Site](https://other.example/x), [mail](mailto:a@b.c), [here](#usage), <https://auto.example>.

		                      ```markdown
		                      [not a link](./inside-code.md)
		                      ```

		                      `[inline](./code.md)`
		                      """;

		string markdown = _parser.ToStandaloneMarkdown(source, "guide/page.md", "Page", _site);

		markdown.Should().Contain("[Site](https://other.example/x)")
			.And.Contain("[mail](mailto:a@b.c)")
			.And.Contain("[here](#usage)")
			.And.Contain("<https://auto.example>")
			.And.Contain("[not a link](./inside-code.md)")
			.And.Contain("`[inline](./code.md)`");
	}

	[Fact]
	public void ToStandaloneMarkdown_RewritesReferenceDefinitionsAndBracketedUrls()
	{
		const string source = """
		                      # Refs

		                      Read [the guide][g] and [spaced](<./my page.md>).

		                      [g]: ./guide.md
		                      """;

		string markdown = _parser.ToStandaloneMarkdown(source, "start/refs.md", "Refs", "/Repo");

		markdown.Should().Contain("[g]: /Repo/start/guide")
			.And.Contain("[spaced](</Repo/start/my page>)")
			.And.Contain("[the guide][g]");
	}

	[Fact]
	public void ToStandaloneMarkdown_LeavesMokaDocsBlocksAsWritten()
	{
		const string source = """
		                      # Blocks

		                      ::: tip Remember
		                      Link to [setup](./setup.md).
		                      :::

		                      ```mermaid
		                      graph TD; A-->B
		                      ```
		                      """;

		string markdown = _parser.ToStandaloneMarkdown(source, "guide/blocks.md", "Blocks", "");

		markdown.Should().Contain("::: tip Remember\nLink to [setup](/guide/setup).\n:::")
			.And.Contain("```mermaid\ngraph TD; A-->B\n```");
	}

	[Fact]
	public void ToStandaloneMarkdown_PageWithoutH1_GetsTheTitle()
	{
		string markdown = _parser.ToStandaloneMarkdown("---\ntitle: Setup\n---\n## Step one\n\nDo it.\n",
			"setup.md", "Setup", "");

		markdown.Should().StartWith("# Setup\n\n## Step one");
	}

	[Fact]
	public void ToStandaloneMarkdown_UsesUnixLineEndings()
	{
		string markdown = _parser.ToStandaloneMarkdown("# A\r\n\r\nB\r\n", "a.md", "A", "");

		markdown.Should().Be("# A\n\nB\n");
	}
}
