using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>
///     Everything an author writes in a component, text and attributes, reaches the page escaped: no tag,
///     attribute or script can come out of it.
/// </summary>
public sealed class ComponentEscapingTests
{
	private const string _payload = "\"><img src=x onerror=alert(1)>";

	private readonly MarkdownParser _parser = new();

	private string Html(string markdown, ContentReferences? references = null) =>
		_parser.Parse(markdown, "page.md", references ?? ContentReferences.None).Html;

	// Summaries and captions are inline Markdown, escaped as any paragraph is (raw HTML in them is raw
	// HTML, as it is anywhere in Markdown).
	[Fact]
	public void DetailsSummary_IsEscapedLikeParagraphText() =>
		Html(":::details 1 < 2 & \"3\" `List<T>`\nBody\n:::").Should()
			.Contain("<summary>1 &lt; 2 &amp; &quot;3&quot; <code>List&lt;T&gt;</code></summary>");

	[Fact]
	public void FigureCaption_IsEscapedLikeParagraphText() =>
		Html(":::figure 1 < 2 & `List<T>`\n![a](b.png)\n:::").Should()
			.Contain("<figcaption>1 &lt; 2 &amp; <code>List&lt;T&gt;</code></figcaption>");

	[Fact]
	public void Badge_TextAndVariant_AreEscaped()
	{
		string html = Html($":badge[{_payload.Replace("]", "")}] and :badge[x]{{variant='\" onclick=\"alert(1)'}}");

		html.Should().NotContain("<img src=x").And.NotContain("onclick=\"alert");
	}

	[Fact]
	public void Kbd_KeysAreEscaped() =>
		Html(":kbd[<script>+&]").Should().Contain("<kbd>&lt;script&gt;</kbd>+<kbd>&amp;</kbd>");

	[Fact]
	public void Term_TextAndDefinition_AreEscaped()
	{
		var references = new ContentReferences
		{
			Glossary = new Dictionary<string, string> { ["<b>"] = "A <script>alert(1)</script> `<code>` & \"quote\"" }
		};

		string html = Html(":term[<b>]", references);

		html.Should().Contain("aria-describedby=\"mokadocs-term-1\">&lt;b&gt;</button>")
			.And.Contain(">A &lt;script&gt;alert(1)&lt;/script&gt; <code>&lt;code&gt;</code> &amp; &quot;quote&quot;</span>")
			.And.NotContain("<script>");
	}

	[Fact]
	public void GlossaryList_IsEscaped()
	{
		var references = new ContentReferences { Glossary = new Dictionary<string, string> { [_payload] = _payload } };

		Html(":::glossary\n:::", references).Should().NotContain("<img src=x").And.Contain("&lt;img src=x onerror=alert(1)&gt;</dt>");
	}

	[Fact]
	public void FileTree_NamesAndCommentsAreEscaped() =>
		Html($":::file-tree\n- {_payload} # {_payload}\n:::").Should().NotContain("<img src=x");

	[Fact]
	public void Video_TitleAndUrl_CannotLeaveTheirAttributes()
	{
		string html = Html($":::video{{title='{_payload}'}} https://youtu.be/aqz-KE-bpKQ\n:::");
		html.Should().Contain("title=\"&quot;&gt;&lt;img src=x onerror=alert(1)&gt;\"").And.NotContain("<img src=x");

		Html(":::video ./a\".mp4\n:::").Should().NotContain("a\".mp4");
	}

	[Fact]
	public void Package_RefusesAnIdThatIsNotAPackageId() =>
		Html($":::package {_payload}\n:::").Should().NotContain("<img").And.NotContain("package-install-widget");

	[Fact]
	public void Xref_UnresolvedReference_IsEscapedCode() =>
		Html("<xref:A&B>").Should().Contain("<code>A&amp;B</code>");

	[Fact]
	public void Xref_ResolvedTarget_IsEscaped()
	{
		var references = new ContentReferences { ResolveXref = _ => new XrefTarget("/api/x\"y", "Name<T>") };

		Html("<xref:X>", references).Should().Contain("<a href=\"/api/x%22y\" class=\"xref\">Name&lt;T&gt;</a>");
	}
}
