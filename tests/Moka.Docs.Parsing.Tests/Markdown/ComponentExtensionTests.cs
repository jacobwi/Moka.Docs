using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class ComponentExtensionTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .Use<ComponentExtension>()
        .Build();

    private static string Render(string md)
    {
        return Markdig.Markdown.ToHtml(md, Pipeline);
    }

    #region Card Component

    [Fact]
    public void Card_WithTitleAndIcon_RendersHeaderAndBody()
    {
        const string md = """
                          ::: card{title="Getting Started" icon="rocket"}
                          Some card content here.
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-card");
        html.Should().Contain("component-card-header");
        html.Should().Contain("component-card-title");
        html.Should().Contain("Getting Started");
        html.Should().Contain("component-card-icon");
        html.Should().Contain("<svg"); // rocket icon SVG
        html.Should().Contain("component-card-body");
        html.Should().Contain("Some card content here.");
    }

    [Fact]
    public void Card_WithVariant_AddsVariantClass()
    {
        const string md = """
                          ::: card{title="Warning Card" variant="warning"}
                          Be careful!
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-card component-card-warning");
        html.Should().Contain("Warning Card");
    }

    [Fact]
    public void Card_WithDefaultVariant_NoExtraClass()
    {
        const string md = """
                          ::: card{title="Default Card"}
                          Content
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("class=\"component-card\"");
        html.Should().NotContain("component-card-default");
    }

    [Fact]
    public void Card_WithoutTitle_NoHeader()
    {
        const string md = """
                          ::: card
                          Just body content.
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-card");
        html.Should().NotContain("component-card-header");
        html.Should().Contain("component-card-body");
        html.Should().Contain("Just body content.");
    }

    [Fact]
    public void Card_WithUnknownIcon_NoIconRendered()
    {
        const string md = """
                          ::: card{title="Test" icon="nonexistent-icon"}
                          Content
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-card-header");
        html.Should().NotContain("component-card-icon");
        html.Should().Contain("Test");
    }

    [Fact]
    public void Card_WithMarkdownContent_ParsesInnerMarkdown()
    {
        const string md = """
                          ::: card{title="Rich Content"}
                          **Bold** and *italic* text.
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("<strong>Bold</strong>");
        html.Should().Contain("<em>italic</em>");
    }

    [Theory]
    [InlineData("book")]
    [InlineData("code")]
    [InlineData("settings")]
    [InlineData("zap")]
    [InlineData("shield")]
    [InlineData("package")]
    [InlineData("star")]
    [InlineData("check")]
    [InlineData("globe")]
    public void Card_WithKnownIcon_RendersIconSvg(string icon)
    {
        var md = $"::: card{{title=\"Test\" icon=\"{icon}\"}}\nContent\n:::";

        var html = Render(md);

        html.Should().Contain("component-card-icon");
        html.Should().Contain("<svg");
    }

    #endregion

    #region Steps Component

    [Fact]
    public void Steps_WithHeadings_RendersNumberedSteps()
    {
        const string md = """
                          ::: steps
                          ### Install the package
                          Run `dotnet add package MokaDocs`.

                          ### Configure your site
                          Create a `mokadocs.yaml` file.

                          ### Build and deploy
                          Run `moka build`.
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-steps");
        html.Should().Contain("component-step");
        html.Should().Contain("component-step-indicator");
        html.Should().Contain("component-step-number\">1</span>");
        html.Should().Contain("component-step-number\">2</span>");
        html.Should().Contain("component-step-number\">3</span>");
        html.Should().Contain("component-step-title");
        html.Should().Contain("Install the package");
        html.Should().Contain("Configure your site");
        html.Should().Contain("Build and deploy");
    }

    [Fact]
    public void Steps_Empty_RendersEmptyContainer()
    {
        const string md = """
                          ::: steps
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-steps");
        // No step items
        html.Should().NotContain("component-step-number");
    }

    [Fact]
    public void Steps_WithContentBeforeFirstHeading_CreatesImplicitStep()
    {
        const string md = """
                          ::: steps
                          Some introductory content.

                          ### Actual Step
                          Step content.
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-step-number\">1</span>");
        html.Should().Contain("Some introductory content.");
    }

    #endregion

    #region Link Cards Component

    [Fact]
    public void LinkCards_WithLinks_RendersCardGrid()
    {
        const string md = """
                          ::: link-cards
                          - [Getting Started](/docs/getting-started) — Learn the basics
                          - [API Reference](/docs/api) — Full API documentation
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-link-cards");
        html.Should().Contain("component-link-card");
        html.Should().Contain("href=\"/docs/getting-started\"");
        html.Should().Contain("href=\"/docs/api\"");
        html.Should().Contain("component-link-card-title");
        html.Should().Contain("Getting Started");
        html.Should().Contain("API Reference");
        html.Should().Contain("component-link-card-arrow");
    }

    [Fact]
    public void LinkCards_WithDescription_RendersDescription()
    {
        const string md = """
                          ::: link-cards
                          - [Docs](/docs) — Complete documentation
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-link-card-desc");
        html.Should().Contain("Complete documentation");
    }

    [Fact]
    public void LinkCards_Empty_RendersEmptyContainer()
    {
        const string md = """
                          ::: link-cards
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-link-cards");
        html.Should().NotContain("component-link-card\""); // no actual cards
    }

    #endregion

    #region Code Group Component

    [Fact]
    public void CodeGroup_WithMultipleBlocks_RendersTabbedInterface()
    {
        const string md = """
                          ::: code-group
                          ```csharp
                          Console.WriteLine("Hello");
                          ```
                          ```javascript
                          console.log("Hello");
                          ```
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("component-code-group");
        html.Should().Contain("tab-headers");
        html.Should().Contain("role=\"tablist\"");
        html.Should().Contain("role=\"tab\"");
        html.Should().Contain("role=\"tabpanel\"");
        html.Should().Contain("C#"); // language display name for csharp
        html.Should().Contain("JavaScript"); // language display name for javascript
    }

    [Fact]
    public void CodeGroup_FirstTabIsActive()
    {
        const string md = """
                          ::: code-group
                          ```csharp
                          var x = 1;
                          ```
                          ```python
                          x = 1
                          ```
                          :::
                          """;

        var html = Render(md);

        // First tab should be active
        html.Should().Contain("tab-header active");
        html.Should().Contain("aria-selected=\"true\"");
        // Second tab should not be active
        html.Should().Contain("aria-selected=\"false\"");
    }

    [Fact]
    public void CodeGroup_WithCustomTitle_UsesTitle()
    {
        const string md = """
                          ::: code-group
                          ```csharp title="Program.cs"
                          Console.WriteLine("Hello");
                          ```
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("Program.cs");
    }

    [Fact]
    public void CodeGroup_Empty_FallbackRendering()
    {
        const string md = """
                          ::: code-group
                          :::
                          """;

        var html = Render(md);

        // Empty code group should not produce tab UI
        html.Should().NotContain("tab-headers");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UnknownComponentType_NotParsed()
    {
        const string md = """
                          ::: unknown-component
                          Content
                          :::
                          """;

        var html = Render(md);

        html.Should().NotContain("component-card");
        html.Should().NotContain("component-steps");
        html.Should().NotContain("component-link-cards");
        html.Should().NotContain("component-code-group");
    }

    [Fact]
    public void Card_WithSingleQuotedAttributes_ParsesCorrectly()
    {
        const string md = """
                          ::: card{title='Single Quoted' icon='book'}
                          Content
                          :::
                          """;

        var html = Render(md);

        html.Should().Contain("Single Quoted");
        html.Should().Contain("component-card-icon");
    }

    [Fact]
    public void Card_WithMultipleVariants_RendersCorrectly()
    {
        var variants = new[] { "info", "success", "warning" };
        foreach (var variant in variants)
        {
            var md = $"::: card{{title=\"Test\" variant=\"{variant}\"}}\nContent\n:::";
            var html = Render(md);
            html.Should().Contain($"component-card-{variant}");
        }
    }

    [Fact]
    public void TwoColons_NotParsedAsComponent()
    {
        const string md = ":: card{title=\"Test\"}\nContent\n::";

        var html = Render(md);

        html.Should().NotContain("component-card");
    }

    #endregion
}