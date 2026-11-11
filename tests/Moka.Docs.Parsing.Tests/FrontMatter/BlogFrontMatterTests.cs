using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

/// <summary>
///     Blog posts' <c>date</c>, <c>author</c>, <c>summary</c>, <c>image</c> and <c>draft</c> were
///     dropped, and the unknown-key check warned about them.
/// </summary>
public sealed class BlogFrontMatterTests
{
	private readonly FrontMatterExtractor _extractor = new();

	private FrontMatterResult Extract(string yaml) => _extractor.Extract($"---\n{yaml}\n---\nBody");

	[Fact]
	public void BlogKeys_AreNotUnknown()
	{
		FrontMatterResult result = Extract("""
		                                   title: Post
		                                   date: 2026-03-15
		                                   author: jane
		                                   authors: [john]
		                                   summary: Short
		                                   image: cover.png
		                                   draft: false
		                                   tags: [a]
		                                   """);

		result.Warnings.Should().BeEmpty();
		result.Error.Should().BeNull();
	}

	[Fact]
	public void Date_WithoutATime_IsMidnightUtc()
	{
		Extract("title: Post\ndate: 2026-03-15").FrontMatter.Date
			.Should().Be(new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero));
	}

	[Theory]
	[InlineData("2026-03-15T09:30:00Z", 9, 30, 0)]
	[InlineData("2026-03-15T09:30", 9, 30, 0)]
	[InlineData("2026-03-15 09:30:00", 9, 30, 0)]
	[InlineData("2026-03-15T09:30:00+02:00", 9, 30, 2)]
	[InlineData("2026-03-15T09:30:00.250-05:00", 9, 30, -5)]
	public void Date_WithATime_KeepsTheTimeAndOffset(string value, int hour, int minute, int offsetHours)
	{
		DateTimeOffset? date = Extract($"title: Post\ndate: {value}").FrontMatter.Date;

		date.Should().NotBeNull();
		date!.Value.Date.Should().Be(new DateTime(2026, 3, 15));
		date.Value.Hour.Should().Be(hour);
		date.Value.Minute.Should().Be(minute);
		date.Value.Offset.Should().Be(TimeSpan.FromHours(offsetHours));
	}

	[Theory]
	[InlineData("03/15/2026")]
	[InlineData("March 15, 2026")]
	[InlineData("2026-13-01")]
	[InlineData("2026-3-5")]
	[InlineData("yesterday")]
	public void Date_ThatIsNotIso_IsIgnoredWithAWarningNamingTheLine(string value)
	{
		FrontMatterResult result = _extractor.Extract($"---\ntitle: Post\ndate: {value}\n---\nBody");

		result.FrontMatter.Date.Should().BeNull();
		result.Warnings.Should().ContainSingle()
			.Which.Should().Be($"date '{value}' is not an ISO 8601 date such as 2026-03-15 or 2026-03-15T09:30:00Z, so it was ignored (line 3)");
	}

	[Fact]
	public void Author_TakesOneName()
	{
		Extract("title: Post\nauthor: Jane Doe").FrontMatter.Authors.Should().Equal("Jane Doe");
	}

	[Fact]
	public void Author_TakesAList_AndAuthorsAddsToIt()
	{
		Extract("title: Post\nauthor: [jane, \" john \"]\nauthors: [Guest]").FrontMatter.Authors
			.Should().Equal("jane", "john", "Guest");
	}

	[Fact]
	public void Author_AsAMapping_IsAWarning()
	{
		FrontMatterResult result = Extract("title: Post\nauthor:\n  name: Jane\n  url: https://example.com");

		result.FrontMatter.Authors.Should().BeEmpty();
		result.Warnings.Should().ContainSingle().Which.Should().StartWith("author must be a name or a list of names")
			.And.EndWith("(line 3)");
	}

	[Fact]
	public void Summary_AndImage_AreTrimmed_AndEmptyMeansNone()
	{
		Core.Content.FrontMatter fm = Extract("title: Post\nsummary: \"  A short summary.  \"\nimage: ./cover.png").FrontMatter;
		fm.Summary.Should().Be("A short summary.");
		fm.Image.Should().Be("./cover.png");

		Core.Content.FrontMatter empty = Extract("title: Post\nsummary: \"\"\nimage: \" \"").FrontMatter;
		empty.Summary.Should().BeNull();
		empty.Image.Should().BeNull();
	}

	[Theory]
	[InlineData("draft: true", PageVisibility.Draft)]
	[InlineData("draft: false", PageVisibility.Public)]
	[InlineData("draft: false\nvisibility: draft", PageVisibility.Draft)]
	[InlineData("draft: true\nvisibility: hidden", PageVisibility.Draft)]
	public void Draft_UsesTheDraftVisibility(string yaml, PageVisibility expected)
	{
		Extract($"title: Post\n{yaml}").FrontMatter.Visibility.Should().Be(expected);
	}

	[Theory]
	[InlineData("2026-03-15", true)]
	[InlineData(" 2026-03-15T10:00:00Z ", true)]
	[InlineData("2026-03-15T10:00:00+0200", false)]
	[InlineData("15.03.2026", false)]
	public void TryParseDate_AcceptsIsoOnly(string value, bool expected)
	{
		FrontMatterExtractor.TryParseDate(value, out _).Should().Be(expected);
	}
}
