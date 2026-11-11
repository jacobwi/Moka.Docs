using FluentAssertions;
using Moka.Docs.Engine.Blog;

namespace Moka.Docs.Integration.Tests.Blog;

/// <summary>Summaries, reading times and tag slugs, from the posts' HTML and front matter.</summary>
public sealed class BlogTextTests
{
	[Fact]
	public void FirstParagraph_IsPlainText_WithInlineMarkupRemoved()
	{
		const string html = "<h1 id=\"t\">Title</h1>\n<p>Use <code>List&lt;T&gt;</code>, <strong>not</strong> arrays.<br />\nReally.</p>\n<p>Second.</p>";

		BlogText.FirstParagraph(html).Should().Be("Use List<T>, not arrays. Really.");
	}

	[Fact]
	public void FirstParagraph_PrefersOneOutsideAdmonitionsAndLists()
	{
		const string html = """
		                    <div class="admonition admonition-tip"><div class="admonition-title">Tip</div><div class="admonition-content"><p>Inside the tip.</p></div></div>
		                    <ul><li><p>In a list.</p></li></ul>
		                    <p><img src="x.png" alt="" /></p>
		                    <p>The post's own words.</p>
		                    """;

		BlogText.FirstParagraph(html).Should().Be("The post's own words.");
	}

	[Fact]
	public void FirstParagraph_FallsBackToANestedOne_ThenToNothing()
	{
		BlogText.FirstParagraph("<blockquote><p>Quoted.</p></blockquote>").Should().Be("Quoted.");
		BlogText.FirstParagraph("<h1>Only a title</h1><pre><code>code</code></pre>").Should().BeEmpty();
	}

	[Fact]
	public void FirstParagraph_CutsALongOneAtAWord()
	{
		string words = string.Join(' ', Enumerable.Range(1, 120).Select(i => "word" + i));

		string summary = BlogText.FirstParagraph($"<p>{words}</p>");

		summary.Length.Should().BeLessThanOrEqualTo(BlogText.MaxSummaryLength + 1);
		summary.Should().EndWith("…");
		words.Should().StartWith(summary.TrimEnd('…'));
		words[summary.Length - 1].Should().Be(' ', "the cut falls between two words");
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(1, 1)]
	[InlineData(200, 1)]
	[InlineData(201, 2)]
	[InlineData(1000, 5)]
	public void ReadingMinutes_RoundsUp_AtTwoHundredWordsAMinute(int words, int minutes)
	{
		BlogText.ReadingMinutes(string.Join("\n", Enumerable.Repeat("word", words))).Should().Be(minutes);
	}

	[Theory]
	[InlineData("Release Notes", "release-notes")]
	[InlineData("  C#  ", "csharp")]
	[InlineData("C++", "cplusplus")]
	[InlineData(".NET 10", "net-10")]
	[InlineData("Ünïcödé Tag", "unicode-tag")]
	[InlineData("日本語", "日本語")]
	[InlineData("---", "tag")]
	[InlineData("../../etc/passwd", "etc-passwd")]
	[InlineData("a\\b:c*d?e\"f<g>h|i", "a-b-c-d-e-f-g-h-i")]
	[InlineData("NUL", "nul-tag")]
	[InlineData("com1", "com1-tag")]
	public void Slug_IsSafeInAUrlAndAsAFolderName(string tag, string slug)
	{
		BlogText.Slug(tag).Should().Be(slug);
	}

	[Fact]
	public void Slug_IsAtMostSixtyCharacters_AndNeverEndsWithAHyphen()
	{
		string slug = BlogText.Slug(string.Join(' ', Enumerable.Repeat("abcd", 30)));

		slug.Length.Should().BeLessThanOrEqualTo(60);
		slug.Should().NotEndWith("-");
	}
}
