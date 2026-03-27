using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class AdmonitionExtensionTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.Use<AdmonitionExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, Pipeline);

	[Theory]
	[InlineData("note")]
	[InlineData("tip")]
	[InlineData("warning")]
	[InlineData("danger")]
	[InlineData("info")]
	[InlineData("caution")]
	[InlineData("important")]
	public void Render_AllTypes_ProducesCorrectClass(string type)
	{
		string md = $"::: {type}\nContent here\n:::";

		string html = Render(md);

		html.Should().Contain($"admonition-{type}");
		html.Should().Contain("admonition-content");
	}

	[Fact]
	public void Render_WithCustomTitle_UsesTitle()
	{
		const string md = "::: tip Custom Tip Title\nSome content\n:::";

		string html = Render(md);

		html.Should().Contain("Custom Tip Title");
		html.Should().Contain("admonition-tip");
	}

	[Fact]
	public void Render_WithDefaultTitle_CapitalizesType()
	{
		const string md = "::: warning\nBe careful!\n:::";

		string html = Render(md);

		html.Should().Contain("Warning");
	}

	[Fact]
	public void Render_UnknownType_NotParsed()
	{
		const string md = "::: unknown\nContent\n:::";

		string html = Render(md);

		// Should not produce admonition HTML
		html.Should().NotContain("admonition");
	}

	[Fact]
	public void Render_WithMarkdownContent_ParsesInnerContent()
	{
		const string md = "::: note\n**Bold** text inside\n:::";

		string html = Render(md);

		html.Should().Contain("<strong>Bold</strong>");
	}
}
