using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class ChangelogExtensionTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<ChangelogExtension>()
		.Build();

	private static string Render(string releases) =>
		Markdig.Markdown.ToHtml(":::changelog\n" + releases + "\n:::", _pipeline);

	[Fact]
	public void TypeInTheHeading_SetsTheBadgeAndStaysOutOfTheDate()
	{
		// The documented "{type: major}" heading suffix was read as part of the date.
		string html = Render("## v2.0.0 - 2025-05-01 {type: major}\n\n### Added\n- Thing");

		html.Should().Contain("changelog-badge-major");
		html.Should().NotContain("{type");
	}

	[Theory]
	[InlineData("## v3.0.0 - 2025-07-01", "major")]
	[InlineData("## v2.1.0 - 2025-06-15", "minor")]
	[InlineData("## v2.1.1 - 2025-06-20", "patch")]
	[InlineData("## 1.0.0-beta.2", "major")]
	[InlineData("## vNext", "patch")]
	public void NoType_IsInferredFromTheVersion(string heading, string expected)
	{
		// Every release without a type used to render as a patch.
		string html = Render(heading + "\n\n### Fixed\n- Thing");

		html.Should().Contain($"changelog-badge-{expected}\"");
	}

	[Fact]
	public void PrereleaseVersion_IsNotSplitIntoAVersionAndADate()
	{
		string html = Render("## v1.0.0-beta.2 - 2025-01-10\n\n### Added\n- Thing");

		html.Should().Contain("1.0.0-beta.2").And.NotContain(">beta.2<");
	}

	[Fact]
	public void TypeLine_OverridesTheInferredType()
	{
		string html = Render("## v2.0.0 - 2025-05-01\ntype: initial\n\n### Added\n- Thing");

		html.Should().Contain("changelog-badge-initial");
	}
}
