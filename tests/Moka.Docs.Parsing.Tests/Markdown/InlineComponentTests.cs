using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>Badges, keys, glossary terms and <c>xref:</c> links.</summary>
public sealed class InlineComponentTests
{
	private static readonly ContentReferences _references = new()
	{
		Glossary = new Dictionary<string, string> { ["API"] = "The public `types` & members.", ["Process"] = "A running program." },
		ResolveXref = reference => reference switch
		{
			"MyLib.Parser" or "T:MyLib.Parser" => new XrefTarget("/api/mylib/parser", "Parser"),
			"MyLib.Parser.Parse" => new XrefTarget("/api/mylib/parser#parse", "Parser.Parse"),
			"MyLib.Result`1" => new XrefTarget("/api/mylib/result-1", "Result<T>"),
			"System.String" => new XrefTarget("https://learn.microsoft.com/dotnet/api/system.string", "String"),
			_ => null
		}
	};

	private readonly MarkdownParser _parser = new();

	private MarkdownParseResult Parse(string markdown) => _parser.Parse(markdown, "guide/page.md", _references);

	[Theory]
	[InlineData(":badge[New]", "info", "New")]
	[InlineData(":badge[New]{variant=success}", "success", "New")]
	[InlineData(":badge[Beta]{variant=\"warning\"}", "warning", "Beta")]
	[InlineData(":badge[Deprecated]{danger}", "danger", "Deprecated")]
	[InlineData(":badge[Since 1.9]{NEUTRAL}", "neutral", "Since 1.9")]
	public void Badge_RendersItsVariant(string markdown, string variant, string text) =>
		Parse($"A {markdown} label.").Html.Should()
			.Contain($"A <span class=\"component-badge component-badge-{variant}\">{text}</span> label.");

	[Fact]
	public void Badge_UnknownVariant_IsAWarningWithItsLine()
	{
		MarkdownParseResult result = Parse("---\ntitle: T\n---\n# T\n\nA :badge[x]{variant=pink} label.");

		result.Html.Should().Contain("component-badge-info");
		result.Warnings.Should().ContainSingle().Which.Should()
			.Be(":badge[x] has the variant 'pink', which isn't one of info, success, warning, danger, neutral; it is shown as info (line 6)");
	}

	[Fact]
	public void Badge_InAHeading_KeepsItsVariant_AndStaysOutOfTheIdAndTableOfContents()
	{
		MarkdownParseResult result = Parse("## Setup :badge[New]{variant=success} for :kbd[Ctrl+K] {#setup-id}\n\nText.");

		result.Html.Should().Contain(
			"<h2 id=\"setup-id\">Setup <span class=\"component-badge component-badge-success\">New</span> for <kbd class=\"component-kbd\"><kbd>Ctrl</kbd>+<kbd>K</kbd></kbd></h2>");
		result.TableOfContents.Entries.Should().ContainSingle().Which.Text.Should().Be("Setup for Ctrl+K");
	}

	[Fact]
	public void Badge_AndKeys_DontChangeTheHeadingsGeneratedId()
	{
		Parse("## Configure :badge[New]{success}").Html.Should().Contain("<h2 id=\"configure\">");
	}

	[Theory]
	[InlineData(":kbd[K]", "<kbd>K</kbd>")]
	[InlineData(":kbd[Ctrl+Shift+P]", "<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd>")]
	[InlineData(":kbd[Ctrl + Alt + Del]", "<kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Del</kbd>")]
	[InlineData(":kbd[Ctrl++]", "<kbd>Ctrl</kbd>+<kbd>+</kbd>")]
	[InlineData(":kbd[+]", "<kbd>+</kbd>")]
	[InlineData(":kbd[Ctrl+\\]]", "<kbd>Ctrl</kbd>+<kbd>]</kbd>")]
	public void Kbd_RendersOneKeyCapPerKey(string markdown, string keys) =>
		Parse(markdown).Html.Should().Contain($"<kbd class=\"component-kbd\">{keys}</kbd>");

	[Theory]
	[InlineData("Emoji :smile: stays.", "😄")]
	[InlineData("Note: a colon.", "Note: a colon.")]
	[InlineData("A word:badge[x] stays.", "word:badge[x]")]
	[InlineData("An :unknown[x] name.", ":unknown[x]")]
	[InlineData("Empty :kbd[] stays.", ":kbd[]")]
	[InlineData("In `:badge[x]` code.", "<code>:badge[x]</code>")]
	public void Text_ThatIsNotAComponent_IsLeftAlone(string markdown, string expected) =>
		Parse(markdown).Html.Should().Contain(expected);

	[Fact]
	public void Term_ShowsItsDefinitionInATooltipThatDescribesTheButton()
	{
		string html = Parse("An :term[API] here.").Html;

		html.Should().Contain(
			"An <span class=\"glossary-term\"><button type=\"button\" class=\"glossary-term-button\" aria-describedby=\"mokadocs-term-1\">API</button>" +
			"<span class=\"glossary-tip\" role=\"tooltip\" id=\"mokadocs-term-1\" data-no-search>The public <code>types</code> &amp; members.</span></span> here.");
	}

	[Fact]
	public void Term_MatchesCaseAndPlurals_AndCanNameItsTerm()
	{
		string html = Parse(":term[apis], :term[processes], :term[public surface]{term=\"API\"}").Html;

		html.Should().Contain("aria-describedby=\"mokadocs-term-1\">apis</button>")
			.And.Contain("id=\"mokadocs-term-2\" data-no-search>A running program.</span>")
			.And.Contain("aria-describedby=\"mokadocs-term-3\">public surface</button>");
	}

	[Fact]
	public void Term_NotInTheGlossary_IsPlainText_AndAWarning()
	{
		MarkdownParseResult result = Parse("Text\n\nA :term[Widget] here.");

		result.Html.Should().Contain("<p>A Widget here.</p>");
		result.Warnings.Should().ContainSingle().Which.Should()
			.Be(":term[Widget] isn't in the glossary, so it is shown as plain text (line 3)");
	}

	[Fact]
	public void Xref_LinksResolveToTheApiRoute_KeepingTheirText()
	{
		string html = Parse("See [the parser](xref:MyLib.Parser), [parse](xref:MyLib.Parser.Parse) and [`T`](xref:MyLib.Result%601).").Html;

		html.Should().Contain("<a href=\"/api/mylib/parser\" class=\"xref\">the parser</a>")
			.And.Contain("<a href=\"/api/mylib/parser#parse\" class=\"xref\">parse</a>")
			.And.Contain("<a href=\"/api/mylib/result-1\" class=\"xref\"><code>T</code></a>");
	}

	[Fact]
	public void Xref_Autolinks_ShowTheResolvedName()
	{
		string html = Parse("Use <xref:MyLib.Parser.Parse>, <xref:T:MyLib.Parser> or <xref:System.String>.").Html;

		html.Should().Contain("<a href=\"/api/mylib/parser#parse\" class=\"xref\">Parser.Parse</a>")
			.And.Contain("<a href=\"/api/mylib/parser\" class=\"xref\">Parser</a>")
			.And.Contain("<a href=\"https://learn.microsoft.com/dotnet/api/system.string\" class=\"xref\">String</a>");
	}

	[Fact]
	public void Xref_Unresolved_IsAWarningWithItsLine_AndPlainText()
	{
		MarkdownParseResult result = Parse("---\ntitle: T\n---\n\nSee [the thing](xref:Missing.Thing)\nand <xref:Missing.Other> too.");

		result.Html.Should().Contain("<p>See the thing\nand <code>Missing.Other</code> too.</p>").And.NotContain("xref:");
		result.Warnings.Should().Equal(
			"xref:Missing.Thing doesn't match a documented type or member, so it isn't a link (line 5)",
			"xref:Missing.Other doesn't match a documented type or member, so it isn't a link (line 6)");
	}

	[Fact]
	public void Xref_InAReferenceDefinition_Resolves()
	{
		Parse("See [the parser][p].\n\n[p]: xref:MyLib.Parser").Html.Should()
			.Contain("<a href=\"/api/mylib/parser\" class=\"xref\">the parser</a>");
	}

	[Fact]
	public void Xref_InAHeading_GoesIntoTheTableOfContentsAsText()
	{
		MarkdownParseResult result = Parse("## Using <xref:MyLib.Parser>\n\nText.");

		result.TableOfContents.Entries.Should().ContainSingle().Which.Text.Should().Be("Using Parser");
	}

	[Fact]
	public void PlainText_HasTheComponentsReadableText()
	{
		var references = new ContentReferences { Glossary = new Dictionary<string, string> { ["API"] = "Public surface." } };
		string text = _parser.Parse("""
		                            # Title

		                            A :badge[Beta]{warning} feature, :kbd[Ctrl+K] and :term[API].

		                            :::details Summary text
		                            Body text.
		                            :::

		                            :::figure Figure caption
		                            ![alt](x.png)
		                            :::

		                            :::file-tree
		                            - **Program.cs** # entry point
		                            :::

		                            :::timeline
		                            - **2026-11** Shipped.
		                            :::

		                            :::package Moka.Docs.Core
		                            :::

		                            :::video{title="Walkthrough"} https://youtu.be/aqz-KE-bpKQ
		                            :::

		                            :::glossary
		                            :::
		                            """, "p.md", references).PlainText;

		text.Should().Contain("Beta").And.Contain("Ctrl+K").And.Contain("API")
			.And.Contain("Summary text").And.Contain("Body text.")
			.And.Contain("Figure caption")
			.And.Contain("Program.cs entry point")
			.And.Contain("2026-11 Shipped.")
			.And.Contain("Moka.Docs.Core")
			.And.Contain("Walkthrough")
			.And.Contain("API Public surface.")
			.And.NotContain("Public surface.\nPublic surface.", "the tooltip's definition isn't repeated where the term is used");
	}

	[Fact]
	public void MarkdownCopy_KeepsComponentsAsWritten_AndResolvesXrefs()
	{
		const string source = "# Page\n\n:::details{open} Why?\nBecause :term[API] and :badge[New]{success}.\n:::\n\n" +
		                      "See [the parser](xref:MyLib.Parser), <xref:MyLib.Parser.Parse>, <xref:Missing.Type> and [m](xref:Missing.Other).\n\n" +
		                      ":::video ./intro.mp4\nIntro\n:::\n";

		string markdown = _parser.ToStandaloneMarkdown(source, "guide/page.md", "Page", "https://example.com/docs", _references.ResolveXref);

		markdown.Should().Contain(":::details{open} Why?\nBecause :term[API] and :badge[New]{success}.\n:::")
			.And.Contain("[the parser](https://example.com/docs/api/mylib/parser)")
			.And.Contain("[Parser.Parse](https://example.com/docs/api/mylib/parser#parse)")
			.And.Contain("<xref:Missing.Type>").And.Contain("[m](xref:Missing.Other)")
			.And.Contain(":::video https://example.com/docs/guide/intro.mp4");
	}

	[Fact]
	public void MarkdownCopy_WithoutAResolver_LeavesXrefsAsWritten() =>
		_parser.ToStandaloneMarkdown("See [p](xref:MyLib.Parser).", "p.md", "P", "").Should().Contain("[p](xref:MyLib.Parser)");

	[Fact]
	public void MinimalPipeline_RendersComponentsWithoutTheParser()
	{
		MarkdownPipeline pipeline = new MarkdownPipelineBuilder().Use<ComponentExtension>().Build();

		Markdig.Markdown.ToHtml(":badge[New]{success} :kbd[Ctrl+K] :term[API]", pipeline).Should()
			.Contain("component-badge-success").And.Contain("<kbd>Ctrl</kbd>").And.Contain("API").And.NotContain("glossary-tip");
	}
}
