using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class ReplExtensionTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<ReplExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, _pipeline);

	[Theory]
	[InlineData("csharp-repl")]
	[InlineData("cs-repl")]
	public void ReplBlock_ProducesReplContainer(string lang)
	{
		string md = $"```{lang}\nConsole.WriteLine(\"Hello\");\n```";

		string html = Render(md);

		html.Should().Contain("class=\"repl-container\"");
		html.Should().Contain("data-repl=\"true\"");
		html.Should().Contain("class=\"language-csharp\"");
		html.Should().Contain("Console.WriteLine");
		html.Should().Contain("repl-output");
	}

	[Fact]
	public void ReplBlock_OutputDivIsHidden()
	{
		const string md = """
		                  ```csharp-repl
		                  var x = 42;
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("class=\"repl-output\" style=\"display:none;\"");
	}

	[Fact]
	public void RegularCsharpBlock_NotAffected()
	{
		const string md = """
		                  ```csharp
		                  Console.WriteLine("Hello");
		                  ```
		                  """;

		string html = Render(md);

		html.Should().NotContain("repl-container");
		html.Should().NotContain("data-repl");
		html.Should().Contain("<code class=\"language-csharp\">");
	}

	[Fact]
	public void EmptyReplBlock_StillRendersContainer()
	{
		const string md = """
		                  ```csharp-repl
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("repl-container");
		html.Should().Contain("data-repl=\"true\"");
	}

	[Fact]
	public void ReplBlock_HtmlEncodesContent()
	{
		const string md = """
		                  ```csharp-repl
		                  var x = new List<string>();
		                  ```
		                  """;

		string html = Render(md);

		// Angle brackets in code should be HTML-encoded
		html.Should().Contain("List&lt;string&gt;");
	}

	[Fact]
	public void OtherLanguageBlock_NotAffected()
	{
		const string md = """
		                  ```python
		                  print("Hello")
		                  ```
		                  """;

		string html = Render(md);

		html.Should().NotContain("repl-container");
	}

	[Theory]
	[InlineData("csharp repl")]
	[InlineData("cs repl")]
	[InlineData("CSharp REPL")]
	[InlineData("c# repl")]
	[InlineData("csharp repl title=\"Demo\"")]
	public void LanguageThenRepl_ProducesReplContainer(string info)
	{
		// Markdig puts "repl" in Arguments, so comparing Info with "csharp repl" never matched,
		// though docs/plugins/repl.md documents these spellings.
		string html = Render($"```{info}\nvar x = 1;\n```");

		html.Should().Contain("data-repl=\"true\"");
	}

	[Theory]
	[InlineData("csharp replay")]
	[InlineData("python repl")]
	[InlineData("csharp title=\"repl\"")]
	public void OtherArguments_AreNotARepl(string info)
	{
		string html = Render($"```{info}\nvar x = 1;\n```");

		html.Should().NotContain("repl-container");
	}
}
