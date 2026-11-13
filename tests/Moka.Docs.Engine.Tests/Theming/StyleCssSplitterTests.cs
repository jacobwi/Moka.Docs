using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The styles' stylesheet is cut into one file per style. Each file has to cascade exactly like the single
///     stylesheet did for its style, and hold nothing that matches another style.
/// </summary>
public sealed class StyleCssSplitterTests
{
	private const string _shared = "html:is([data-style=\"a\"], [data-style=\"b\"])";

	private static Dictionary<string, string> Split(string css) => StyleCssSplitter.Split(Lf(css), ["a", "b"]);

	private static string Lf(string text) => text.Replace("\r\n", "\n");

	[Fact]
	public void SharedRules_GoToEveryStyleUnderItsOwnScope_InTheirPlace()
	{
		Dictionary<string, string> files = Split($$"""
		                                           /* shared */
		                                           {{_shared}} { --x: 1; }
		                                           /* Style: A */
		                                           html[data-style="a"] .p { color: red; }
		                                           /* Style: B */
		                                           html[data-style="b"] .p { color: blue; }
		                                           :where({{_shared}}) .q { color: green; }
		                                           """);

		files["a"].Should().Be(Lf("""
		                       /* shared */
		                       html[data-style="a"] { --x: 1; }
		                       /* Style: A */
		                       html[data-style="a"] .p { color: red; }
		                       :where(html[data-style="a"]) .q { color: green; }

		                       """));
		files["b"].Should().Be(Lf("""
		                       /* shared */
		                       html[data-style="b"] { --x: 1; }
		                       /* Style: B */
		                       html[data-style="b"] .p { color: blue; }
		                       :where(html[data-style="b"]) .q { color: green; }

		                       """));
	}

	[Fact]
	public void MediaBlocks_KeepOnlyTheStylesRules_AndGoWhenNoneIsLeft()
	{
		Dictionary<string, string> files = Split("""
		                                         @media (max-width: 768px) {
		                                             html[data-style="a"] .p { margin: 0; }
		                                         }
		                                         @media print {
		                                             html[data-style="b"] .p { display: none; }
		                                             html:is([data-style="a"], [data-style="b"]) .q { display: none; }
		                                         }
		                                         """);

		files["a"].Should().Be(Lf("""
		                       @media (max-width: 768px) {
		                           html[data-style="a"] .p { margin: 0; }
		                       }
		                       @media print {
		                           html[data-style="a"] .q { display: none; }
		                       }

		                       """));
		files["b"].Should().Be(Lf("""
		                       @media print {
		                           html[data-style="b"] .p { display: none; }
		                           html[data-style="b"] .q { display: none; }
		                       }

		                       """));
	}

	[Fact]
	public void ARuleForSeveralStyles_KeepsOnlyTheSelectorsOfEachFilesStyle()
	{
		// A selector for another style left in a file would apply from the wrong place once a reader who
		// switched styles has both files.
		Dictionary<string, string> files = Split("""
		                                         html[data-style="a"] .p,
		                                         html[data-style="b"] .p { color: red; }
		                                         """);

		files["a"].Should().Be("html[data-style=\"a\"] .p { color: red; }\n");
		files["b"].Should().Be("html[data-style=\"b\"] .p { color: red; }\n");
	}

	[Fact]
	public void BracesInStringsAndComments_DoNotEndARule()
	{
		Dictionary<string, string> files = Split("""
		                                         html[data-style="a"] .p::before { content: '}' / ''; }
		                                         html[data-style="b"] .p { color: red; /* } */ }
		                                         html[data-style="a"] .q { color: blue; }
		                                         """);

		files["a"].Should().Be(Lf("""
		                       html[data-style="a"] .p::before { content: '}' / ''; }
		                       html[data-style="a"] .q { color: blue; }

		                       """));
		files["b"].Should().Be("html[data-style=\"b\"] .p { color: red; /* } */ }\n");
	}

	[Fact]
	public void TheThemesStyleFiles_EachHoldTheirOwnSections()
	{
		string all = string.Concat(EmbeddedThemeProvider.StyleFileNames.Select(ThemeCss.Style));

		foreach (string style in EmbeddedThemeProvider.StyleFileNames)
		{
			all.Should().Contain($"/* Style: {char.ToUpperInvariant(style[0])}{style[1..]}");
		}

		ThemeCss.Style("moka").Should().Contain("html[data-style=\"moka\"] .site-banner").And.Contain("html[data-style=\"moka\"] .blog-card");
	}
}
