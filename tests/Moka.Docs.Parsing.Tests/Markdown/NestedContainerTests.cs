using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     Containers nest when the outer fence is longer than the inner one. Every container used
///     to close on any bare <c>:::</c>, so the inner block's closing line ended the outer block.
/// </summary>
public sealed class NestedContainerTests
{
	private readonly MarkdownParser _parser = new();

	private string Html(string body) => _parser.Parse("---\ntitle: Nesting\n---\n\n" + body).Html;

	[Fact]
	public void StepsContainingACardAndACodeGroup_KeepEveryStepInside()
	{
		const string md = """
		                  ::::steps
		                  ### Install

		                  :::card{title="Note"}
		                  Card body.
		                  :::

		                  ### Configure

		                  :::code-group
		                  ```bash title="Bash"
		                  mokadocs init
		                  ```
		                  :::

		                  ### Build
		                  Done.
		                  ::::

		                  After the steps.
		                  """;

		string html = Html(md);

		html.Should().Contain("component-step-number\">3</span>");
		int stepsEnd = html.LastIndexOf("After the steps.", StringComparison.Ordinal);
		html.IndexOf("Configure", StringComparison.Ordinal).Should().BeLessThan(stepsEnd);
		html.Should().Contain("component-card").And.Contain("component-code-group");
		html.Should().NotContain("<p>:::</p>").And.NotContain("<p>::::</p>");
	}

	[Fact]
	public void AdmonitionInsideAnAdmonition_ClosesOnlyTheInnerOne()
	{
		const string md = """
		                  ::::note Outer
		                  Before.

		                  :::warning Inner
		                  Inner text.
		                  :::

		                  Still in the outer note.
		                  ::::
		                  """;

		string html = Html(md);

		// The inner warning closes, then the paragraph, then the outer note's two divs.
		html.Should().MatchRegex(@"Inner text\.</p>\s*</div>\s*</div>\s*<p>Still in the outer note\.</p>\s*</div>\s*</div>");
		html.Should().NotContain("<p>::::</p>");
	}

	[Fact]
	public void TabsInsideTabs_KeepTheirOwnTitles()
	{
		const string md = """
		                  ==== "Outer A"
		                  === "Inner 1"
		                  One.
		                  === "Inner 2"
		                  Two.
		                  ===
		                  ==== "Outer B"
		                  Bee.
		                  ====
		                  """;

		string html = Html(md);

		// The outer group's own header row holds exactly its two tabs.
		html.Should().MatchRegex(
			@"<div class=""tab-headers"" role=""tablist"">\s*<button[^>]*>Outer A</button>\s*<button[^>]*>Outer B</button>\s*</div>");
		html.Should().MatchRegex(
			@"<div class=""tab-headers"" role=""tablist"">\s*<button[^>]*>Inner 1</button>\s*<button[^>]*>Inner 2</button>\s*</div>");
	}
}
