using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

/// <summary>What the parser writes itself is in the site's language (<see cref="ContentReferences.Text" />).</summary>
public sealed class ContentTextTests
{
	private static readonly MarkdownParser _parser = new();

	private static string Html(string markdown, ThemeText text) =>
		_parser.Parse(markdown, "guide/page.md", new ContentReferences { Text = text }).Html;

	[Fact]
	public void English_IsTheDefault()
	{
		string html = _parser.Parse(":::note\nA.\n:::\n\n:::details\nB.\n:::\n").Html;

		html.Should().Contain("</span>Note</div>").And.Contain("<summary>Details</summary>");
	}

	[Fact]
	public void Admonitions_DetailsAndFileTrees_UseTheSiteText()
	{
		ThemeText text = ThemeText.For("ja");

		string html = Html(":::warning\nA.\n:::\n\n:::warning Own title\nB.\n:::\n\n:::details\nC.\n:::\n\n:::file-tree\n- src/\n  - a.cs\n:::\n", text);

		html.Should().Contain("</span>" + text["admonition.warning"] + "</div>");
		html.Should().Contain("</span>Own title</div>", "a title of its own stays");
		html.Should().Contain("<summary>" + text["details.summary"] + "</summary>");
		html.Should().Contain("data-no-search>" + text["fileTree.folder"] + "</span>");
	}

	[Fact]
	public void CodeBlocks_AreNamedInTheSiteLanguage()
	{
		ThemeText text = ThemeText.For("de");

		string html = Html("```csharp\nvar x = 1;\n```\n\n```\nplain\n```\n", text);

		html.Should().Contain("aria-label=\"" + text.Format("code.label", ("language", "C#")) + "\"");
		html.Should().Contain("aria-label=\"" + text["code.labelPlain"] + "\"");
	}

	[Fact]
	public void PackageWidgetAndVideo_UseTheSiteText()
	{
		ThemeText text = ThemeText.For("fr", new Dictionary<string, string> { ["install.packageManager"] = "Console \"PM\"" });

		string html = Html(":::package Moka.Docs\n:::\n\n:::video https://youtu.be/aqz-KE-bpKQ\n:::\n\n:::video ./clip.mp4\n:::\n", text);

		html.Should().Contain("aria-label=\"" + Escape(text.Format("install.nugetLink", ("package", "Moka.Docs"))) + "\"");
		html.Should().Contain("aria-label=\"" + Escape(text.Format("install.with", ("package", "Moka.Docs"))) + "\"");
		html.Should().Contain(">Console &quot;PM&quot;</button>");
		html.Should().Contain("title=\"" + Escape(text.Format("video.youtube", ("id", "aqz-KE-bpKQ"))) + "\"");
		html.Should().Contain(Escape(text.Format("video.download", ("title", "clip.mp4"))));
	}

	// Markdig escapes markup characters and leaves other text as it is.
	private static string Escape(string text) =>
		text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);
}
