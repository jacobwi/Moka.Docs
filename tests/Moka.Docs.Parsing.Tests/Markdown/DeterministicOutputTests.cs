using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     Tab and code group ids came from process-wide counters, so every watch rebuild changed the
///     HTML of unchanged pages and parallel parses raced on the counters.
/// </summary>
public sealed partial class DeterministicOutputTests
{
	private const string _page = """
	                             ---
	                             title: Groups
	                             ---
	                             === "One"
	                             First
	                             === "Two"
	                             Second
	                             ===

	                             ::: code-group
	                             ```cs
	                             var a = 1;
	                             ```
	                             ```js
	                             let a = 1;
	                             ```
	                             :::

	                             === "Three"
	                             Third
	                             ===
	                             """;

	[GeneratedRegex("data-tab-group=\"([^\"]+)\"")]
	private static partial Regex GroupIdPattern();

	private static List<string> GroupIds(string html) =>
		GroupIdPattern().Matches(html).Select(m => m.Groups[1].Value).ToList();

	[Fact]
	public void SamePage_RendersTheSameHtmlEveryTime()
	{
		string first = new MarkdownParser().Parse(_page, "guide/groups.md").Html;
		string second = new MarkdownParser().Parse(_page, "guide/groups.md").Html;

		second.Should().Be(first);
	}

	[Fact]
	public void GroupIds_AreUniqueWithinThePage()
	{
		List<string> ids = GroupIds(new MarkdownParser().Parse(_page, "guide/groups.md").Html);

		ids.Should().HaveCount(3).And.OnlyHaveUniqueItems();
		ids.Should().AllSatisfy(id => id.Should().MatchRegex("^[a-z]+-[0-9a-f]{8}-[0-9]+$"));
	}

	[Fact]
	public void GroupIds_DifferBetweenPages()
	{
		List<string> a = GroupIds(new MarkdownParser().Parse(_page, "a.md").Html);
		List<string> b = GroupIds(new MarkdownParser().Parse(_page, "b.md").Html);

		a.Should().NotIntersectWith(b);
	}

	[Fact]
	public void PathSeparator_DoesNotChangeTheIds()
	{
		string forward = new MarkdownParser().Parse(_page, "guide/groups.md").Html;
		string backward = new MarkdownParser().Parse(_page, "guide\\groups.md").Html;

		backward.Should().Be(forward);
	}

	[Fact]
	public void WithoutAPath_IdsComeFromTheOpeningLine()
	{
		List<string> ids = GroupIds(new MarkdownParser().Parse(_page).Html);

		ids.Should().Equal("tabs-1", "codegroup-7", "tabs-16");
	}

	[Fact]
	public void ParallelParses_MatchSequentialParses()
	{
		var parser = new MarkdownParser();
		string[] paths = Enumerable.Range(0, 200).Select(i => $"section{i % 7}/page{i}.md").ToArray();
		string Source(int i) => _page.Replace("Groups", $"Page {i}") + $"\n\n## Heading {i}\n\nText [link](../x{i}.md)";

		string[] sequential = paths.Select((path, i) => parser.Parse(Source(i), path).Html).ToArray();
		string[] parallel = new string[paths.Length];
		Parallel.For(0, paths.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 },
			i => parallel[i] = parser.Parse(Source(i), paths[i]).Html);

		parallel.Should().Equal(sequential);
	}
}
