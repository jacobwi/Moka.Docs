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

	[Theory]
	[InlineData("notes.md-old/page.md", "/notes.md-old/page")]
	[InlineData("a.md.md", "/a.md")]
	[InlineData("guide/Readme.MD", "/guide/Readme")]
	[InlineData("guide/INDEX.md", "/guide")]
	public void BuildRoute_RemovesOnlyTheTrailingExtension(string file, string expected)
	{
		// ".md" was removed anywhere in the path, so notes.md-old/page.md became /notes-old/page.
		MarkdownParsePhase.BuildRoute(file, new FrontMatter { Title = "T" }).Should().Be(expected);
	}

	[Theory]
	[InlineData("../../outside-escape")]
	[InlineData("/guide/../../x")]
	[InlineData("/faq?lang=en")]
	[InlineData("/faq#answers")]
	[InlineData("/Admin: Users")]
	[InlineData("/a|b")]
	[InlineData("C:\\Users\\x")]
	public void FindRouteOverrideProblem_UnsafeOrNonPortableRoutes_AreRefused(string route) =>
		MarkdownParsePhase.FindRouteOverrideProblem(route).Should().NotBeNull();

	[Theory]
	[InlineData("/help/faq")]
	[InlineData(" help/faq/ ")]
	[InlineData("/")]
	[InlineData("/v1.2/release-notes")]
	public void FindRouteOverrideProblem_OrdinaryRoutes_AreAccepted(string route) =>
		MarkdownParsePhase.FindRouteOverrideProblem(route).Should().BeNull();
}
