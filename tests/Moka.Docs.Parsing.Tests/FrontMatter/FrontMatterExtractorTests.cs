// MokaDocs - Tests for FrontMatterExtractor

using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

public sealed class FrontMatterExtractorTests
{
	private readonly FrontMatterExtractor _extractor = new();

	[Fact]
	public void Extract_WithValidFrontMatter_ParsesAllFields()
	{
		const string input = """
		                     ---
		                     title: Getting Started
		                     description: How to install MokaDocs
		                     order: 10
		                     icon: rocket
		                     layout: landing
		                     tags: [setup, quickstart]
		                     visibility: hidden
		                     toc: false
		                     expanded: false
		                     route: /getting-started
		                     version: ">=2.0"
		                     ---
		                     # Hello World
		                     This is the body.
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Getting Started");
		result.FrontMatter.Description.Should().Be("How to install MokaDocs");
		result.FrontMatter.Order.Should().Be(10);
		result.FrontMatter.Icon.Should().Be("rocket");
		result.FrontMatter.Layout.Should().Be("landing");
		result.FrontMatter.Tags.Should().BeEquivalentTo("setup", "quickstart");
		result.FrontMatter.Visibility.Should().Be(PageVisibility.Hidden);
		result.FrontMatter.Toc.Should().BeFalse();
		result.FrontMatter.Expanded.Should().BeFalse();
		result.FrontMatter.Route.Should().Be("/getting-started");
		result.FrontMatter.Version.Should().Be(">=2.0");
		result.Body.Should().Contain("# Hello World");
	}

	[Fact]
	public void Extract_WithMinimalFrontMatter_ReturnsDefaults()
	{
		const string input = """
		                     ---
		                     title: Minimal
		                     ---
		                     Body content
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Minimal");
		result.FrontMatter.Description.Should().BeEmpty();
		result.FrontMatter.Order.Should().Be(0);
		result.FrontMatter.Layout.Should().Be("default");
		result.FrontMatter.Toc.Should().BeTrue();
		result.FrontMatter.Expanded.Should().BeTrue();
		result.FrontMatter.Visibility.Should().Be(PageVisibility.Public);
		result.Body.Should().Be("Body content");
	}

	[Fact]
	public void Extract_WithNoFrontMatter_ReturnsUntitled()
	{
		const string input = "# Just Markdown\nNo front matter here.";

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be(input);
	}

	[Fact]
	public void Extract_WithEmptyInput_ReturnsUntitled()
	{
		FrontMatterResult result = _extractor.Extract("");

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().BeEmpty();
	}

	[Fact]
	public void Extract_WithUnclosedFrontMatter_TreatsAsNoFrontMatter()
	{
		const string input = """
		                     ---
		                     title: Unclosed
		                     Still no closing delimiter
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be(input);
	}

	[Fact]
	public void Extract_WithDraftVisibility_ParsesCorrectly()
	{
		const string input = """
		                     ---
		                     title: Draft Page
		                     visibility: draft
		                     ---
		                     Content
		                     """;

		FrontMatterResult result = _extractor.Extract(input);
		result.FrontMatter.Visibility.Should().Be(PageVisibility.Draft);
	}

	[Fact]
	public void Extract_WithInvalidYaml_TreatsAsNoFrontMatter()
	{
		const string input = """
		                     ---
		                     title: [invalid yaml
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);
		// Should not crash - falls back gracefully
		result.FrontMatter.Title.Should().Be("Untitled");
	}

	[Fact]
	public void Extract_WithEmptyFrontMatter_ReturnsUntitled()
	{
		const string input = """
		                     ---
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);
		result.FrontMatter.Title.Should().Be("Untitled");
		result.Body.Should().Be("Body");
	}

	[Fact]
	public void Extract_WithNoTitle_ReturnsUntitled()
	{
		const string input = """
		                     ---
		                     description: Has description but no title
		                     order: 5
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);
		result.FrontMatter.Title.Should().Be("Untitled");
		result.FrontMatter.Description.Should().Be("Has description but no title");
	}

	[Fact]
	public void Extract_HeroKeys_KeepTheCodeIndentedAndTrimTheRest()
	{
		const string input = """
		                     ---
		                     title: Home
		                     layout: landing
		                     heroCodeLanguage: " csharp "
		                     heroImage: " images/hero.png "
		                     heroCode: |
		                       if (ready)
		                       {
		                           Run();
		                       }

		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.Warnings.Should().BeEmpty();
		result.FrontMatter.HeroCode.Should().Be("if (ready)\n{\n    Run();\n}");
		result.FrontMatter.HeroCodeLanguage.Should().Be("csharp");
		result.FrontMatter.HeroImage.Should().Be("images/hero.png");
	}

	[Fact]
	public void Extract_ShowcaseKeys_ReadTheCodeBandStatsAndCallToAction()
	{
		const string input = """
		                     ---
		                     title: Home
		                     layout: landing
		                     showcaseTitle: " Two lines "
		                     showcaseText: Bind and go.
		                     showcaseCodeLanguage: " csharp "
		                     showcaseCode: |
		                       var grid = new Grid
		                       {
		                           Size = 2
		                       };

		                     stats:
		                       - value: 2M
		                         label: " Downloads "
		                       - value: 40
		                       - label: Only a label
		                       -
		                       - value: " "
		                         label: ""
		                     cta:
		                       title: Ready?
		                       text: " Install it. "
		                       link: " /guide/install "
		                       linkText: Install
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.Warnings.Should().BeEmpty();
		result.FrontMatter.ShowcaseTitle.Should().Be("Two lines");
		result.FrontMatter.ShowcaseText.Should().Be("Bind and go.");
		result.FrontMatter.ShowcaseCode.Should().Be("var grid = new Grid\n{\n    Size = 2\n};");
		result.FrontMatter.ShowcaseCodeLanguage.Should().Be("csharp");
		result.FrontMatter.Stats.Should().Equal(
			new LandingStat { Value = "2M", Label = "Downloads" },
			new LandingStat { Value = "40" },
			new LandingStat { Label = "Only a label" });
		result.FrontMatter.Cta.Should().Be(new LandingCallToAction
		{
			Title = "Ready?", Text = "Install it.", Link = "/guide/install", LinkText = "Install"
		});
	}

	[Fact]
	public void Extract_NoShowcaseKeys_LeavesThemEmpty()
	{
		FrontMatterResult result = _extractor.Extract("---\ntitle: Home\nshowcaseCode: '  '\ncta:\n  linkText: Go\n---\nBody");

		result.FrontMatter.ShowcaseCode.Should().BeNull();
		result.FrontMatter.ShowcaseCodeLanguage.Should().BeNull();
		result.FrontMatter.ShowcaseTitle.Should().BeEmpty();
		result.FrontMatter.ShowcaseText.Should().BeEmpty();
		result.FrontMatter.Stats.Should().BeEmpty();
		result.FrontMatter.Cta.Should().BeNull("a call to action with only a button text has nothing to show");
	}

	[Fact]
	public void Extract_NoHeroKeys_LeavesThemNull()
	{
		FrontMatterResult result = _extractor.Extract("---\ntitle: Home\nheroCode: '  '\n---\nBody");

		result.FrontMatter.HeroCode.Should().BeNull();
		result.FrontMatter.HeroCodeLanguage.Should().BeNull();
		result.FrontMatter.HeroImage.Should().BeNull();
	}
}
