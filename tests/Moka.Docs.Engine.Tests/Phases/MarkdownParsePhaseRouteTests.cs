using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

public sealed class MarkdownParsePhaseRouteTests
{
	[Theory]
	[InlineData("guide/intro.md", null, "/guide/intro")]
	[InlineData("guide/index.md", null, "/guide")]
	[InlineData("index.md", null, "/")]
	[InlineData("guide\\windows.md", null, "/guide/windows")]
	[InlineData("faq.md", "/help/faq", "/help/faq")]
	[InlineData("faq.md", "help/faq", "/help/faq")]
	[InlineData("faq.md", "/help/faq/", "/help/faq")]
	[InlineData("faq.md", " /help/faq ", "/help/faq")]
	[InlineData("home.md", "/", "/")]
	public void BuildRoute_NormalizesRouteOverrides(string file, string? route, string expected)
	{
		// A route without a leading slash produced relative sidebar links, and a trailing
		// slash matched no nav path.
		var frontMatter = new FrontMatter { Title = "T", Route = route };

		MarkdownParsePhase.BuildRoute(file, frontMatter).Should().Be(expected);
	}
}
