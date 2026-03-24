using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class ReplExtensionTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .Use<ReplExtension>()
        .Build();

    private static string Render(string md)
    {
        return Markdig.Markdown.ToHtml(md, Pipeline);
    }

    [Theory]
    [InlineData("csharp-repl")]
    [InlineData("cs-repl")]
    public void ReplBlock_ProducesReplContainer(string lang)
    {
        var md = $"```{lang}\nConsole.WriteLine(\"Hello\");\n```";

        var html = Render(md);

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

        var html = Render(md);

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

        var html = Render(md);

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

        var html = Render(md);

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

        var html = Render(md);

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

        var html = Render(md);

        html.Should().NotContain("repl-container");
    }
}