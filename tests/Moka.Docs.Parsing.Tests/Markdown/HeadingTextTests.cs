using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     The table of contents and the search text read only literals and one level of emphasis,
///     so link text, bold italics and entities went missing.
/// </summary>
public sealed class HeadingTextTests
{
	private readonly MarkdownParser _parser = new();

	[Theory]
	[InlineData("## See [API](x) docs", "See API docs")]
	[InlineData("## ***Bold*** start", "Bold start")]
	[InlineData("## Use **`List<T>`** here", "Use List<T> here")]
	[InlineData("## List&lt;T&gt;", "List<T>")]
	[InlineData("## Q&amp;A", "Q&A")]
	[InlineData("## Visit <https://example.com/x>", "Visit https://example.com/x")]
	[InlineData("## [**Nested** *link*](x)", "Nested link")]
	public void TocEntry_KeepsTheReadableText(string heading, string expected)
	{
		MarkdownParseResult result = _parser.Parse(heading);

		TocEntry entry = result.TableOfContents.Entries.Should().ContainSingle().Subject;
		entry.Text.Should().Be(expected);
	}

	[Fact]
	public void SearchText_KeepsLinkTextEmphasisAndEntities()
	{
		MarkdownParseResult result = _parser.Parse(
			"Read the [setup guide](setup.md) and ***always*** use List&lt;T&gt;. Mail <a@b.dev>.");

		result.PlainText.Should().Be("Read the setup guide and always use List<T>. Mail a@b.dev.");
	}
}
