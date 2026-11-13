using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     The "On this page" list: the page's own h1 isn't in it, it nests to <c>tocDepth</c> at any depth,
///     and a page with fewer than two entries has none.
/// </summary>
public sealed partial class TableOfContentsRenderingTests
{
	private const string _deepPage = """
	                                 ---
	                                 title: Deep
	                                 ---
	                                 # Deep

	                                 ## Two

	                                 ### Three

	                                 #### Four

	                                 ##### Five

	                                 ###### Six

	                                 ## Two again
	                                 """;

	private static async Task<string> RenderAsync(string markdown, int tocDepth = 3, string toc = "default")
	{
		SiteConfig config = MockSite.Config() with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions { TocDepth = tocDepth, Layout = new ThemeLayoutOptions { Toc = toc } }
			}
		};

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			[MockSite.Root + "/docs/page.md"] = new(markdown)
		});

		return fs.File.ReadAllText(MockSite.Root + "/_site/page/index.html");
	}

	private static string TocList(string html)
	{
		int start = html.IndexOf("<ul class=\"toc-list\">", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the page has a table of contents");
		int depth = 0;
		foreach (Match tag in ListTagRegex().Matches(html, start))
		{
			depth += tag.Value.StartsWith("</", StringComparison.Ordinal) ? -1 : 1;
			if (depth == 0)
			{
				return html[start..(tag.Index + tag.Length)];
			}
		}

		throw new InvalidOperationException("The list never closes");
	}

	private static List<string> Levels(string list) =>
		LevelRegex().Matches(list).Select(m => m.Groups[1].Value).ToList();

	[Fact]
	public async Task PageTitle_IsNotAnEntryAndTheH2HeadingsAreTheRoots()
	{
		string list = TocList(await RenderAsync(_deepPage));

		list.Should().NotContain("href=\"#deep\"");
		Levels(list).Should().Equal("2", "3", "2");
		Regex.Count(list, "<ul").Should().Be(2, "the list and the one nested list under Two");
	}

	[Theory]
	[InlineData(2, new[] { "2", "2" })]
	[InlineData(4, new[] { "2", "3", "4", "2" })]
	[InlineData(6, new[] { "2", "3", "4", "5", "6", "2" })]
	public async Task TocDepth_ShowsHeadingsDownToThatLevel(int tocDepth, string[] levels)
	{
		string list = TocList(await RenderAsync(_deepPage, tocDepth));

		Levels(list).Should().Equal(levels);
		list.Should().NotMatchRegex(@"<ul>\s*</ul>", "an entry whose children are all too deep gets no nested list");
		Regex.Count(list, "<ul").Should().Be(tocDepth - 1, "each level below h2 is one nested list");
	}

	[Fact]
	public async Task NestedEntries_KeepTheLinkFirstAndTheListAfterIt()
	{
		string list = TocList(await RenderAsync(_deepPage, 6));

		list.Should().MatchRegex(
			"""<li class="toc-item toc-level-2">\s*<a href="#two">Two</a>\s*<ul>\s*<li class="toc-item toc-level-3">""");
	}

	[Fact]
	public async Task OneEntry_ShowsNoTableOfContents()
	{
		string html = await RenderAsync("---\ntitle: Short\n---\n# Short\n\n## Only section\n\nText.\n");

		html.Should().NotContain("toc-sidebar").And.NotContain("toc-list").And.NotContain("id=\"tocToggle\"");
	}

	[Fact]
	public async Task OneEntryWithAChild_CountsAsTwoAndShows()
	{
		string html = await RenderAsync("---\ntitle: Short\n---\n# Short\n\n## Section\n\n### Detail\n");

		Levels(TocList(html)).Should().Equal("2", "3");
	}

	[Fact]
	public async Task InlineVariant_UsesTheSameList()
	{
		string html = await RenderAsync(_deepPage, 4, "inline");

		html.Should().Contain("<details class=\"toc-inline\"");
		Levels(TocList(html)).Should().Equal("2", "3", "4", "2");
	}

	[GeneratedRegex("</?ul\\b[^>]*>")]
	private static partial Regex ListTagRegex();

	[GeneratedRegex("toc-level-(\\d)")]
	private static partial Regex LevelRegex();
}
