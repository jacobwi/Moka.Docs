using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class TabbedContentExtensionTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<TabbedContentExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, _pipeline);

	/// <summary>
	///     Every case here runs through <see cref="RenderWithTimeout" /> rather than
	///     <see cref="Render" /> where a malformed block could loop. The original parser
	///     pushed two blocks from TryOpen and hung the build on any tab block at all, and
	///     the extension had no tests to catch it.
	/// </summary>
	private static string RenderWithTimeout(string md)
	{
		Task<string> task = Task.Run(() => Render(md));
		task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue("parsing must not hang");
		return task.Result;
	}

	[Fact]
	public void Tabs_TwoTabs_RendersHeadersAndPanels()
	{
		const string md = "=== \"npm\"\nInstall with npm\n=== \"yarn\"\nInstall with yarn\n===";

		string html = RenderWithTimeout(md);

		html.Should().Contain("class=\"tabs\"");
		html.Should().Contain("class=\"tab-headers\"");
		html.Should().Contain(">npm</button>");
		html.Should().Contain(">yarn</button>");
		html.Should().Contain("Install with npm");
		html.Should().Contain("Install with yarn");
	}

	[Fact]
	public void Tabs_FirstTabIsActive_RemainingAreHidden()
	{
		const string md = "=== \"one\"\nFirst\n=== \"two\"\nSecond\n===";

		string html = RenderWithTimeout(md);

		html.Should().Contain("class=\"tab-header active\" role=\"tab\" aria-selected=\"true\"");
		html.Should().Contain("class=\"tab-header\" role=\"tab\" aria-selected=\"false\"");
		html.Should().Contain("class=\"tab-content active\" role=\"tabpanel\"");
		html.Should().Contain("role=\"tabpanel\" hidden");
	}

	[Fact]
	public void Tabs_ContentIsSplitBetweenPanels()
	{
		const string md = "=== \"a\"\nAlpha\n=== \"b\"\nBeta\n===";

		string html = RenderWithTimeout(md);

		int firstPanel = html.IndexOf("tab-content active", StringComparison.Ordinal);
		int secondPanel = html.IndexOf("role=\"tabpanel\" hidden", StringComparison.Ordinal);
		int alpha = html.IndexOf("Alpha", StringComparison.Ordinal);
		int beta = html.IndexOf("Beta", StringComparison.Ordinal);

		alpha.Should().BeGreaterThan(firstPanel).And.BeLessThan(secondPanel);
		beta.Should().BeGreaterThan(secondPanel);
	}

	[Fact]
	public void Tabs_WithFencedCode_KeepsCodeInsideItsPanel()
	{
		const string md = "=== \"npm\"\n```bash\nnpm i x\n```\n=== \"yarn\"\n```bash\nyarn add x\n```\n===";

		string html = RenderWithTimeout(md);

		html.Should().Contain("language-bash");
		html.Should().Contain("npm i x");
		html.Should().Contain("yarn add x");
		html.Split("tab-content").Length.Should().Be(3, "two panels produce two splits plus the head");
	}

	[Fact]
	public void Tabs_SingleQuotedTitle_IsAccepted()
	{
		const string md = "=== 'single'\nBody\n===";

		string html = RenderWithTimeout(md);

		html.Should().Contain(">single</button>");
	}

	[Fact]
	public void Tabs_WithoutClosingMarker_StillRenders()
	{
		const string md = "=== \"only\"\nBody with no closing marker";

		string html = RenderWithTimeout(md);

		html.Should().Contain("class=\"tabs\"");
		html.Should().Contain(">only</button>");
		html.Should().Contain("Body with no closing marker");
	}

	[Fact]
	public void Tabs_TitleIsHtmlEscaped()
	{
		const string md = "=== \"<script>\"\nBody\n===";

		string html = RenderWithTimeout(md);

		html.Should().NotContain("<script>");
		html.Should().Contain("&lt;script&gt;");
	}

	[Fact]
	public void SetextHeading_IsNotTreatedAsATab()
	{
		// A bare === under a paragraph is a CommonMark setext h1. The tab parser sits at
		// position 0 in the block parser list, so it has to decline this line.
		const string md = "Just a heading\n===";

		string html = RenderWithTimeout(md);

		html.Should().Contain("<h1");
		html.Should().Contain("Just a heading");
		html.Should().NotContain("class=\"tabs\"");
	}

	[Fact]
	public void ThreeTabs_ProduceThreeHeadersAndThreePanels()
	{
		const string md = "=== \"a\"\nA\n=== \"b\"\nB\n=== \"c\"\nC\n===";

		string html = RenderWithTimeout(md);

		html.Split("tab-header\"").Length.Should().Be(3, "two inactive headers");
		html.Should().Contain("tab-header active");
		html.Split("role=\"tabpanel\"").Length.Should().Be(4, "three panels");
	}
}
