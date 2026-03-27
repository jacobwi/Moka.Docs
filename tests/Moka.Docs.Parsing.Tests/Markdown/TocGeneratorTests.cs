using FluentAssertions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class TocGeneratorTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
		.Build();

	private readonly TocGenerator _generator = new();

	private MarkdownDocument ParseDoc(string md) => Markdig.Markdown.Parse(md, Pipeline);

	[Fact]
	public void Generate_FlatHeadings_ReturnsFlatList()
	{
		MarkdownDocument doc = ParseDoc("## One\n## Two\n## Three");

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().HaveCount(3);
		toc.Entries[0].Text.Should().Be("One");
		toc.Entries[1].Text.Should().Be("Two");
		toc.Entries[2].Text.Should().Be("Three");
	}

	[Fact]
	public void Generate_NestedHeadings_BuildsHierarchy()
	{
		MarkdownDocument doc = ParseDoc("# Top\n## Child 1\n### Grandchild\n## Child 2");

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().HaveCount(1);
		toc.Entries[0].Text.Should().Be("Top");
		toc.Entries[0].Level.Should().Be(1);
		toc.Entries[0].Children.Should().HaveCount(2);
		toc.Entries[0].Children[0].Text.Should().Be("Child 1");
		toc.Entries[0].Children[0].Children.Should().HaveCount(1);
		toc.Entries[0].Children[0].Children[0].Text.Should().Be("Grandchild");
	}

	[Fact]
	public void Generate_NoHeadings_ReturnsEmpty()
	{
		MarkdownDocument doc = ParseDoc("Just a paragraph.\n\nAnother paragraph.");

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().BeEmpty();
	}

	[Fact]
	public void Generate_AnchorIds_AreGithubStyle()
	{
		TocGenerator.GenerateAnchorId("Getting Started").Should().Be("getting-started");
		TocGenerator.GenerateAnchorId("API Reference (v2)").Should().Be("api-reference-v2");
		TocGenerator.GenerateAnchorId("What's New?").Should().Be("whats-new");
		TocGenerator.GenerateAnchorId("C# & .NET").Should().Be("c-net");
		TocGenerator.GenerateAnchorId("  Spaces  Around  ").Should().Be("spaces-around");
	}

	[Fact]
	public void Generate_UsesMarkdigAutoIdentifierIds()
	{
		MarkdownDocument doc = ParseDoc("## Getting Started\n## API Reference (v2)\n## What's New?");

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().HaveCount(3);
		toc.Entries[0].Id.Should().Be("getting-started");
		toc.Entries[1].Id.Should().Be("api-reference-v2");
		toc.Entries[2].Id.Should().Be("whats-new");
	}

	[Fact]
	public void Generate_FallsBackToGenerateAnchorId_WhenNoAttributes()
	{
		// Parse without AutoIdentifiers to test fallback
		MarkdownPipeline plainPipeline = new MarkdownPipelineBuilder().Build();
		MarkdownDocument doc = Markdig.Markdown.Parse("## Hello World", plainPipeline);

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().HaveCount(1);
		toc.Entries[0].Id.Should().Be("hello-world");
	}

	[Fact]
	public void Generate_MixedLevels_HandlesSkippedLevels()
	{
		// Jumping from H1 to H3 (skipping H2)
		MarkdownDocument doc = ParseDoc("# Top\n### Deep");

		TableOfContents toc = _generator.Generate(doc);

		toc.Entries.Should().HaveCount(1);
		toc.Entries[0].Children.Should().HaveCount(1);
		toc.Entries[0].Children[0].Text.Should().Be("Deep");
	}
}
