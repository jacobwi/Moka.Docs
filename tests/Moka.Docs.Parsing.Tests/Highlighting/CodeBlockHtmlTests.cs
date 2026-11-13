using FluentAssertions;
using Moka.Docs.Parsing.Highlighting;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Highlighting;

public sealed class CodeBlockHtmlTests
{
	[Fact]
	public void EnhanceBlocks_HighlightsAndWrapsAPlainBlockKeepingItsAttributes()
	{
		const string html = "<p>Signature:</p>\n<pre class=\"api-signature\"><code class=\"language-csharp\">public int Count { get; }</code></pre>\n";

		CodeBlockHtml.EnhanceBlocks(html).Should().Be(
			"<p>Signature:</p>\n<div class=\"code-block\" data-lang=\"csharp\"><pre class=\"api-signature\"><code class=\"language-csharp\" data-highlighted=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"C# code\">"
			+ "<span class=\"line\"><span class=\"token keyword\">public</span> <span class=\"token keyword\">int</span> <span class=\"token type-name\">Count</span> { <span class=\"token keyword\">get</span>; }</span>"
			+ "</code></pre></div>\n");
	}

	[Fact]
	public void EnhanceBlocks_DecodesTheTextOnceAndSplitsItIntoLines()
	{
		const string html = "<pre><code class=\"language-json\">{\n  &quot;a&quot;: &quot;x &amp;lt; y&quot;\n}\n</code></pre>";

		string result = CodeBlockHtml.EnhanceBlocks(html);

		result.Should().Contain("<span class=\"line\">{</span>\n<span class=\"line\">  <span class=\"token string\">&quot;a&quot;</span>: <span class=\"token string\">&quot;x &amp;lt; y&quot;</span></span>\n<span class=\"line\">}</span></code>",
			"the trailing line break isn't an extra line, and an escaped entity stays escaped");
	}

	[Fact]
	public void EnhanceBlocks_WrapsABlockWithoutALanguage()
	{
		string result = CodeBlockHtml.EnhanceBlocks("<pre><code>a &lt; b\nc</code></pre>");

		result.Should().Be("<div class=\"code-block\"><pre><code data-highlighted=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"Code\">"
		                   + "<span class=\"line\">a &lt; b</span>\n<span class=\"line\">c</span></code></pre></div>");
	}

	[Theory]
	[InlineData("<pre class=\"mermaid\">graph TD; A--&gt;B</pre>")]
	[InlineData("<pre><code>see <a href=\"/x\">this</a></code></pre>")]
	[InlineData("<pre>no code element</pre>")]
	[InlineData("<p>No blocks, <code>inline</code> only.</p>")]
	public void EnhanceBlocks_LeavesOtherMarkupAlone(string html)
	{
		CodeBlockHtml.EnhanceBlocks(html).Should().Be(html);
	}

	[Fact]
	public void EnhanceBlocks_LeavesBlocksInScriptsAndCommentsAlone()
	{
		const string html = "<script>el.innerHTML = \"<pre><code>x</code></pre>\";</script>\n"
		                    + "<!-- <pre><code>y</code></pre> -->\n"
		                    + "<template><pre><code>z</code></pre></template>\n"
		                    + "<pre><code>real</code></pre>";

		string result = CodeBlockHtml.EnhanceBlocks(html);

		result.Should().StartWith("<script>el.innerHTML = \"<pre><code>x</code></pre>\";</script>\n<!-- <pre><code>y</code></pre> -->\n<template><pre><code>z</code></pre></template>\n")
			.And.EndWith("<span class=\"line\">real</span></code></pre></div>");
	}

	[Fact]
	public void EnhanceBlocks_SkipsWhatTheMarkdownRendererAlreadyWrote()
	{
		string rendered = new MarkdownParser().Parse("```csharp\nvar x = 1;\n```\n\n```\n\n```").Html;

		CodeBlockHtml.EnhanceBlocks(rendered).Should().Be(rendered);
	}

	[Fact]
	public void EnhanceBlocks_IsIdempotent()
	{
		const string html = "<pre><code class=\"language-python\">def f():\n    return 1</code></pre><pre><code>x</code></pre>";

		string once = CodeBlockHtml.EnhanceBlocks(html);

		CodeBlockHtml.EnhanceBlocks(once).Should().Be(once);
	}

	[Fact]
	public void EnhanceBlocks_FindsTheLanguageAmongOtherClasses()
	{
		string result = CodeBlockHtml.EnhanceBlocks("<pre><code class='wide language-yaml'>a: true</code></pre>");

		result.Should().StartWith("<div class=\"code-block\" data-lang=\"yaml\"><pre><code class='wide language-yaml' data-highlighted=\"true\"")
			.And.Contain("<span class=\"token keyword\">true</span>");
	}
}
