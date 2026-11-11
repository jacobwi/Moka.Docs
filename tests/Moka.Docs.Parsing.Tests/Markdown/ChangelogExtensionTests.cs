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

	[Theory]
	[InlineData("## v3.1.20251111093000", "patch")]
	[InlineData("## v3.99999999999999999999.0", "minor")]
	[InlineData("## v4.00000000000000000000.0000000000000000000000", "major")]
	public void LongNumericVersionParts_DoNotThrow(string heading, string expected)
	{
		// int.Parse overflowed on a timestamp patch number, and the page was dropped.
		string html = Render(heading + "\n\n### Fixed\n- Thing");

		html.Should().Contain($"changelog-badge-{expected}\"");
	}

	[Theory]
	[InlineData("2025-06-15", "June 15, 2025")]
	[InlineData("2025-06", "June 2025")]
	[InlineData("2025", "2025")]
	[InlineData("2025-06-15T23:30:00+05:00", "June 15, 2025")]
	[InlineData("2025-06-15T10:00:00Z", "June 15, 2025")]
	[InlineData("11/12/2025", "11/12/2025")]
	[InlineData("Q3 <soon>", "Q3 &lt;soon&gt;")]
	public void ReleaseDate_IsReadAsIsoOnly(string date, string shown)
	{
		// A general parse read 2025-06 as June 1, 2025 and 11/12/2025 as a US date.
		string html = Render($"## v1.2.3 - {date}\n\n### Fixed\n- Thing");

		html.Should().Contain($"<span class=\"changelog-date\">{shown}</span>");
	}
}
