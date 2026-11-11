using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     Titles and hrefs from admonitions and components were written into the HTML as typed, so
///     <c>List&lt;T&gt;</c> lost its <c>&lt;T&gt;</c> and a quote in a link-card href ended the attribute.
/// </summary>
public sealed class MarkupEscapingTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<AdmonitionExtension>()
		.Use<ComponentExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, _pipeline);

	[Fact]
	public void AdmonitionTitle_IsEscaped()
	{
		string html = Render("::: tip Use List<T> & friends\nBody\n:::");

		html.Should().Contain("Use List&lt;T&gt; &amp; friends</div>");
		html.Should().NotContain("List<T>");
	}

	[Fact]
	public void CardTitle_IsEscaped()
	{
		string html = Render("::: card{title=\"Generic <T>\"}\nBody\n:::");

		html.Should().Contain("<span class=\"component-card-title\">Generic &lt;T&gt;</span>");
	}

	[Fact]
	public void CardVariant_CannotLeaveTheClassAttribute()
	{
		string html = Render("::: card{title=\"T\" variant='x\" onclick=\"alert(1)'}\nBody\n:::");

		html.Should().NotContain("onclick=\"alert(1)\"");
		html.Should().Contain("component-card-x&quot; onclick=&quot;alert(1)");
	}

	[Fact]
	public void CodeGroupTabTitle_IsEscaped()
	{
		string html = Render("::: code-group\n```cs title=\"List<T>.cs\"\nvar x = 1;\n```\n:::");

		html.Should().Contain(">List&lt;T&gt;.cs</button>");
	}

	[Fact]
	public void LinkCardHref_CannotLeaveTheHrefAttribute()
	{
		string html = Render("::: link-cards\n- [Docs](/x\"onmouseover=\"alert(1)) - Read it\n:::");

		html.Should().NotContain("\"onmouseover=\"");
		html.Should().Contain("href=\"/x%22onmouseover=%22alert(1)\"");
	}

	[Fact]
	public void LinkCardTitle_KeepsDecodedEntities()
	{
		string html = Render("::: link-cards\n- [Q&amp;A](/qa) - Questions\n:::");

		html.Should().Contain("<span class=\"component-link-card-title\">Q&amp;A</span>");
	}
}
