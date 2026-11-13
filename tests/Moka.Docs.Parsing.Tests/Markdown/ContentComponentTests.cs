using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     The block components: details, FAQ, columns, file trees, timelines, videos, figures, package widgets and
///     the glossary list.
/// </summary>
public sealed class ContentComponentTests
{
	private readonly MarkdownParser _parser = new();

	private string Html(string markdown, ContentReferences? references = null) =>
		_parser.Parse(markdown, "guide/page.md", references ?? ContentReferences.None).Html;

	[Fact]
	public void Details_IsANativeDetailsElementWithAnInlineSummary()
	{
		string html = Html(":::details Why does `build` *fail*?\nBecause.\n:::");

		html.Should().Contain("<details class=\"component-details\">")
			.And.Contain("<summary>Why does <code>build</code> <em>fail</em>?</summary>")
			.And.Contain("<div class=\"component-details-body\">\n<p>Because.</p>\n</div>\n</details>");
	}

	[Fact]
	public void Details_OpenAttribute_StartsOpen_AndAMissingSummaryReadsDetails()
	{
		Html(":::details{open} Shown\nBody\n:::").Should().Contain("<details class=\"component-details\" open>");
		Html(":::details\nBody\n:::").Should().Contain("<summary>Details</summary>");
	}

	[Fact]
	public void Faq_TurnsQuestionsIntoDetails_KeepsTheirIds_AndWritesOneFaqPage()
	{
		string html = Html("""
		                   :::faq
		                   ### Is it free?
		                   Yes, it is.

		                   ### Does it need Node?
		                   No.
		                   :::

		                   ::::faq
		                   :::details Another one?
		                   An answer with `code`.
		                   :::
		                   ::::
		                   """);

		html.Should().Contain("<summary><h3 id=\"is-it-free\" class=\"component-faq-question\">Is it free?</h3></summary>")
			.And.Contain("<p>Yes, it is.</p>")
			.And.Contain("<summary>Another one?</summary>");
		html.Split("application/ld+json").Should().HaveCount(2, "one FAQPage block for the whole page");
		html.Should().Contain("\"@type\":\"FAQPage\"")
			.And.Contain("{\"@type\":\"Question\",\"name\":\"Is it free?\",\"acceptedAnswer\":{\"@type\":\"Answer\",\"text\":\"Yes, it is.\"}}")
			.And.Contain("\"name\":\"Another one?\",\"acceptedAnswer\":{\"@type\":\"Answer\",\"text\":\"An answer with code.\"}");
	}

	[Fact]
	public void Faq_StructuredDataCannotCloseItsScript()
	{
		string html = Html(":::faq\n### Is 1 < 2 & 3?\nUse `</script><script>alert(1)</script>`.\n:::");

		string json = html[html.IndexOf("application/ld+json", StringComparison.Ordinal)..];
		json.Should().Contain("\"name\":\"Is 1 \\u003C 2 \\u0026 3?\"")
			.And.Contain("\\u003C/script\\u003E\\u003Cscript\\u003E")
			.And.NotContain("</script><script>");
	}

	[Fact]
	public void Columns_RenderAGridWithItsColumnCount()
	{
		string html = Html("::::columns\n:::column\nLeft\n:::\n:::column\nMiddle\n:::\n:::column\nRight\n:::\n::::");

		html.Should().Contain("<div class=\"component-columns\" data-columns=\"3\">")
			.And.Contain("<div class=\"component-column\">\n<p>Left</p>\n</div>");
	}

	[Fact]
	public void Columns_MoreThanFour_IsAWarning()
	{
		MarkdownParseResult result = _parser.Parse(
			"# T\n\n:::::columns\n" + string.Concat(Enumerable.Repeat(":::column\nx\n:::\n", 5)) + ":::::", "p.md");

		result.Html.Should().Contain("data-columns=\"4\"");
		result.Warnings.Should().ContainSingle(w => w.Contains("':::columns' has 5 columns") && w.EndsWith("(line 3)"));
	}

	[Fact]
	public void FileTree_NestsFolders_WithIconsCommentsAndHighlights()
	{
		string html = Html("""
		                   :::file-tree
		                   - src/
		                     - **Program.cs** # entry point, see `Main`
		                     - __init__.py
		                     - logo.png
		                   - empty/
		                   - ...
		                   :::
		                   """);

		html.Should().Contain("<li class=\"file-tree-folder\"><details open><summary class=\"file-tree-entry\">")
			.And.Contain("<li class=\"file-tree-file file-tree-highlight\"><span class=\"file-tree-entry\"><span class=\"file-tree-icon file-tree-icon-code\" aria-hidden=\"true\">")
			.And.Contain("<span class=\"file-tree-name\">Program.cs</span><span class=\"file-tree-comment\">entry point, see <code>Main</code></span>")
			.And.Contain("<span class=\"file-tree-name\">__init__.py</span>", "entries are plain text, not Markdown")
			.And.Contain("file-tree-icon-image")
			.And.Contain("<span class=\"file-tree-name\">empty</span><span class=\"visually-hidden\" data-no-search>, folder</span>")
			.And.Contain("<span class=\"file-tree-entry\"><span class=\"file-tree-name\">...</span></span>");
		html.Should().Contain("</summary>\n<ul>\n<li class=\"file-tree-file file-tree-highlight\">", "Program.cs is inside src");
	}

	[Theory]
	[InlineData("app.cs", "code")]
	[InlineData("mokadocs.yaml", "data")]
	[InlineData("Directory.Build.props", "data")]
	[InlineData("README.md", "text")]
	[InlineData("LICENSE", "text")]
	[InlineData("Dockerfile", "code")]
	[InlineData("intro.mp4", "media")]
	[InlineData("MyLib.1.0.0.nupkg", "archive")]
	[InlineData("data.bin", "file")]
	public void FileTree_PicksTheIconByFileType(string name, string family) =>
		Html($":::file-tree\n- {name}\n:::").Should().Contain($"file-tree-icon-{family}\"");

	[Fact]
	public void Timeline_ListItemsStartWithABoldDate()
	{
		string html = Html(":::timeline\n- **2026-11** Shipped: layouts.\n- **Q3 2026** - Planning.\n- No date.\n:::");

		html.Should().Contain("<ol class=\"component-timeline\">")
			.And.Contain("<div class=\"component-timeline-date\"><time datetime=\"2026-11\">2026-11</time></div>")
			.And.Contain("<p>Shipped: layouts.</p>")
			.And.Contain("<div class=\"component-timeline-date\">Q3 2026</div>")
			.And.Contain("<p>Planning.</p>", "a hyphen after the date is a separator")
			.And.Contain("<div class=\"component-timeline-body\">\n<p>No date.</p>");
	}

	[Fact]
	public void Timeline_HeadingsAreDates_AndKeepTheirIds()
	{
		string html = Html(":::timeline\n### 2026-03-16\nFirst release.\n\n- with a list\n:::");

		html.Should().Contain("<h3 id=\"section\" class=\"component-timeline-date\"><time datetime=\"2026-03-16\">2026-03-16</time></h3>")
			.And.Contain("<p>First release.</p>\n<ul>");
	}

	[Theory]
	[InlineData("https://www.youtube.com/watch?v=aqz-KE-bpKQ", "https://www.youtube-nocookie.com/embed/aqz-KE-bpKQ")]
	[InlineData("https://youtu.be/aqz-KE-bpKQ?t=1m30s", "https://www.youtube-nocookie.com/embed/aqz-KE-bpKQ?start=90")]
	[InlineData("https://www.youtube.com/shorts/aqz-KE-bpKQ", "https://www.youtube-nocookie.com/embed/aqz-KE-bpKQ")]
	[InlineData("https://vimeo.com/123456789", "https://player.vimeo.com/video/123456789?dnt=1")]
	[InlineData("https://vimeo.com/123456789/abcdef1234", "https://player.vimeo.com/video/123456789?dnt=1&amp;h=abcdef1234")]
	public void Video_EmbedsYouTubeAndVimeoWithoutTracking(string url, string embed)
	{
		string html = Html($":::video {url}\nThe title\n:::");

		html.Should().Contain($"<iframe src=\"{embed}\" title=\"The title\" loading=\"lazy\"")
			.And.Contain("referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>")
			.And.Contain("<figcaption>The title</figcaption>")
			.And.NotContain("<script");
	}

	[Fact]
	public void Video_FileNextToThePage_PlaysInAVideoElement()
	{
		string html = Html(":::video{poster=\"./still.png\"} ./media/intro.mp4\n:::");

		html.Should().Contain("<video controls preload=\"none\" aria-label=\"intro.mp4\" poster=\"/guide/still.png\">")
			.And.Contain("<source src=\"/guide/media/intro.mp4\" type=\"video/mp4\">")
			.And.NotContain("<figcaption>");
	}

	[Fact]
	public void Video_TitleComesFromTheAttribute_ElseTheUrl()
	{
		Html(":::video{title=\"Install walkthrough\"} https://youtu.be/aqz-KE-bpKQ\n:::")
			.Should().Contain("title=\"Install walkthrough\"");
		Html(":::video https://youtu.be/aqz-KE-bpKQ\n:::").Should().Contain("title=\"YouTube video aqz-KE-bpKQ\"");
	}

	[Fact]
	public void Video_OtherUrls_AreAWarning_AndScriptUrlsShowNothing()
	{
		MarkdownParseResult web = _parser.Parse(":::video https://example.com/page\n:::", "p.md");
		web.Html.Should().Contain("<p><a href=\"https://example.com/page\">https://example.com/page</a></p>").And.NotContain("<iframe");
		web.Warnings.Should().ContainSingle(w => w.Contains("not a YouTube or Vimeo address"));

		MarkdownParseResult script = _parser.Parse(":::video javascript:alert(1)\n:::", "p.md");
		script.Html.Should().NotContain("javascript:");
		script.Warnings.Should().ContainSingle();
	}

	[Fact]
	public void Figure_CaptionFromTheOpeningLine_OrTheTextAfterTheImage()
	{
		Html(":::figure The pipeline\n![Pipeline](./flow.png)\n:::").Should()
			.Contain("<figure class=\"component-figure\">\n<img src=\"/guide/flow.png\" alt=\"Pipeline\" />\n<figcaption>The pipeline</figcaption>\n</figure>");
		Html(":::figure\n![Pipeline](./flow.png)\nWith **bold** text.\n:::").Should()
			.Contain("<figcaption>With <strong>bold</strong> text.</figcaption>")
			.And.NotContain("<p><img");
	}

	[Fact]
	public void Package_RendersTheInstallWidgetMarkup_WithTheVersion()
	{
		string html = Html(":::package Moka.Docs.Core 1.9.0\n:::");

		html.Should().Contain("<div class=\"package-install-widget\">")
			.And.Contain("<span class=\"install-package-name\">Moka.Docs.Core</span><a href=\"https://www.nuget.org/packages/Moka.Docs.Core/1.9.0\"")
			.And.Contain("<button class=\"install-tab active\" type=\"button\" role=\"tab\" aria-selected=\"true\" data-tab=\"pm\">Package Manager</button>")
			.And.Contain("<div class=\"install-panel active\" data-panel=\"pm\"><pre><code data-highlighted=\"true\">Install-Package Moka.Docs.Core -Version 1.9.0</code></pre>")
			.And.Contain("dotnet add package Moka.Docs.Core --version 1.9.0")
			.And.Contain("&lt;PackageReference Include=&quot;Moka.Docs.Core&quot; Version=&quot;1.9.0&quot; /&gt;");
	}

	[Fact]
	public void Package_WithoutAVersion_InstallsTheLatest_OrTheKnownVersion()
	{
		string latest = Html(":::package Other.Lib\n:::");
		latest.Should().Contain("Install-Package Other.Lib</code>").And.Contain("Version=&quot;*&quot;")
			.And.Contain("href=\"https://www.nuget.org/packages/Other.Lib\"");

		var references = new ContentReferences { PackageVersions = new Dictionary<string, string> { ["other.lib"] = "2.0.0" } };
		Html(":::package Other.Lib\n:::", references).Should().Contain("dotnet add package Other.Lib --version 2.0.0");
		Html(":::package{version=\"3.1.0\"} Other.Lib\n:::", references).Should().Contain("--version 3.1.0");
	}

	[Fact]
	public void Package_TwoOnAPage_AreTwoWidgets()
	{
		string html = Html(":::package A.One\n:::\n\n:::package B.Two\n:::");

		html.Split("class=\"package-install-widget\"").Should().HaveCount(3);
	}

	[Theory]
	[InlineData(":::package\n:::", "needs a package ID")]
	[InlineData(":::package bad\"id\n:::", "not a NuGet package ID")]
	[InlineData(":::package Good.Id not-a-version\n:::", "not a NuGet version")]
	public void Package_BadInput_IsAWarning(string markdown, string message)
	{
		MarkdownParseResult result = _parser.Parse(markdown, "p.md");

		result.Warnings.Should().ContainSingle(w => w.Contains(message));
		result.Html.Should().NotContain("bad\"id");
	}

	[Fact]
	public void Glossary_ListsTheSitesTermsSorted_WithAnchors()
	{
		var references = new ContentReferences
		{
			Glossary = new Dictionary<string, string> { ["Route"] = "A page's path.", ["API"] = "Public `types`.", ["c#"] = "A language.", ["C"] = "Another." }
		};

		string html = Html(":::glossary\nIntro text.\n:::", references);

		html.Should().Contain("<p>Intro text.</p>\n<dl class=\"component-glossary\">\n<dt id=\"glossary-api\">API</dt>\n<dd>Public <code>types</code>.</dd>")
			.And.Contain("<dt id=\"glossary-c\">C</dt>")
			.And.Contain("<dt id=\"glossary-c-2\">c#</dt>");
		html.IndexOf("Route", StringComparison.Ordinal).Should().BeGreaterThan(html.IndexOf("c#", StringComparison.Ordinal));
	}

	[Fact]
	public void Glossary_WithNoTerms_IsAWarning()
	{
		MarkdownParseResult result = _parser.Parse(":::glossary\n:::", "p.md");

		result.Html.Should().NotContain("<dl");
		result.Warnings.Should().ContainSingle(w => w.Contains("':::glossary' has nothing to list"));
	}

	[Theory]
	[InlineData(":::card\n- a\n  - b\n:::")]
	[InlineData("::: note\n- a\n  - b\n:::")]
	[InlineData(":::details\n- a\n  - b\n:::")]
	[InlineData("=== \"Tab\"\n- a\n  - b\n===")]
	public void Containers_KeepNestedListsNested(string markdown) =>
		Html(markdown).Should().Contain("<li>a\n<ul>\n<li>b</li>\n</ul>\n</li>");

	[Fact]
	public void Containers_KeepTheIndentationOfCodeInside()
	{
		string html = Html("::: tip\n```xml\n<A>\n  <B />\n</A>\n```\n:::");

		html.Should().Contain("<span class=\"line\">  ");
	}

	[Fact]
	public void UnknownComponentNames_StillFallBackToPlainContainers()
	{
		Html("::: something\nText\n:::").Should().Contain("<div class=\"something\">");
	}
}
