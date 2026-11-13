using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class CodeBlockExtensionTests
{
	private static readonly MarkdownParser _parser = new();

	private static string Render(string md) => _parser.Parse(md).Html;

	[Fact]
	public void Fence_IsHighlightedAndWrappedAtBuildTime()
	{
		string html = Render("```csharp\nvar x = 1;\n```");

		html.Trim().Should().Be(
			"<div class=\"code-block\" data-lang=\"csharp\"><pre><code class=\"language-csharp\" data-highlighted=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"C# code\">"
			+ "<span class=\"line\"><span class=\"token keyword\">var</span> x = <span class=\"token number\">1</span>;</span>"
			+ "</code></pre></div>");
	}

	[Fact]
	public void EveryLine_IsWrappedAndTheTextIsUnchanged()
	{
		const string code = "if (a < b && c > \"d\")\n{\n\n    Run(); // done\n}";

		string html = Render($"```csharp\n{code}\n```");

		Regex.Matches(html, "<span class=\"line\">").Should().HaveCount(5);
		Text(html).Should().Be(code);
	}

	[Theory]
	[InlineData("title=\"Program.cs\"", "Program.cs")]
	[InlineData("title='My File.cs'", "My File.cs")]
	[InlineData("title=Program.cs", "Program.cs")]
	[InlineData("{2} TITLE=\"Program.cs\" wrap", "Program.cs")]
	public void Title_ShowsAFileNameBar(string arguments, string title)
	{
		string html = Render($"```csharp {arguments}\nvar x = 1;\nvar y = 2;\n```");

		html.Should().Contain("<div class=\"code-block\" data-lang=\"csharp\" data-title")
			.And.Contain($"<div class=\"code-title\">{title}</div><pre>")
			.And.Contain($"aria-label=\"{title}\"");
	}

	[Fact]
	public void Title_IsEscaped()
	{
		string html = Render("```js title=\"<b>&x</b>\"\nx\n```");

		html.Should().Contain("<div class=\"code-title\">&lt;b&gt;&amp;x&lt;/b&gt;</div>")
			.And.Contain("aria-label=\"&lt;b&gt;&amp;x&lt;/b&gt;\"")
			.And.NotContain("<b>");
	}

	[Fact]
	public void Title_NamesTheTabInACodeGroupInsteadOfABar()
	{
		const string md = """
		                  ::: code-group
		                  ```bash title="CLI"
		                  dotnet add package X
		                  ```
		                  ```xml title="PackageReference"
		                  <PackageReference Include="X" />
		                  ```
		                  :::
		                  """;

		string html = Render(md);

		html.Should().Contain(">CLI</button>").And.Contain(">PackageReference</button>")
			.And.NotContain("code-title").And.NotContain("data-title");
		Regex.Matches(html, "<div class=\"code-block\"").Should().HaveCount(2);
	}

	[Theory]
	[InlineData("csharp {1,3-4}", new[] { 1, 3, 4 })]
	[InlineData("csharp{2}", new[] { 2 })]
	[InlineData("csharp {4-2}", new[] { 2, 3, 4 })]
	[InlineData("csharp {0, 9, x, 5}", new[] { 5 })]
	[InlineData("csharp {1} {5}", new[] { 1, 5 })]
	public void LineRanges_HighlightThoseLines(string info, int[] expected)
	{
		string html = Render($"```{info}\na\nb\nc\nd\ne\n```");

		Lines(html).Select((line, i) => (line, number: i + 1))
			.Where(l => l.line.Classes.Contains("highlighted"))
			.Select(l => l.number)
			.Should().Equal(expected);
		html.Should().Contain("class=\"language-csharp\"").And.NotContain("language-csharp{");
	}

	[Theory]
	[InlineData("csharp", "var a = 1; // [!code highlight]", "highlighted", "var a = 1;")]
	[InlineData("csharp", "Run();// [!code hl]", "highlighted", "Run();")]
	[InlineData("csharp", "i--; // [!code --]", "diff-remove", "i--;")]
	[InlineData("python", "x = 1  # [!code ++]", "diff-add", "x = 1")]
	[InlineData("yaml", "# [!code focus]", "focused", "")]
	[InlineData("sql", "SELECT 1; -- [!code --]", "diff-remove", "SELECT 1;")]
	[InlineData("css", "a { color: red; } /* [!code focus] */", "focused", "a { color: red; }")]
	[InlineData("html", "<p>x</p> <!-- [!code ++] -->", "diff-add", "<p>x</p>")]
	[InlineData("razor", "<p>@Name</p> @* [!code highlight] *@", "highlighted", "<p>@Name</p>")]
	[InlineData("csharp", "x(); // [!CODE ++]", "diff-add", "x();")]
	[InlineData("csharp", "x(); // [!code focus] // [!code ++]", "diff-add focused", "x();")]
	public void Markers_MarkTheLineAndLeaveTheCode(string language, string line, string classes, string text)
	{
		string html = Render($"```{language}\n{line}\nnext\n```");

		(string Classes, string Text) first = Lines(html)[0];
		first.Classes.Should().Be(classes);
		first.Text.Should().Be(text);
		Text(html).Should().NotContain("[!code");
	}

	[Theory]
	[InlineData("x(); // [!code warning]")]
	[InlineData("x(); // [!code highlight] and more")]
	[InlineData("x(); [!code highlight]")]
	[InlineData("var s = \"[!code ++]\";")]
	public void TextThatIsNotAMarker_StaysAsWritten(string line)
	{
		string html = Render($"```csharp\n{line}\n```");

		Lines(html).Single().Should().Be(("", line));
	}

	[Fact]
	public void EscapedMarker_IsShownAsText()
	{
		string html = Render("```csharp\nx(); // [!!code highlight]\n```");

		Lines(html).Single().Should().Be(("", "x(); // [!code highlight]"));
	}

	[Fact]
	public void Markers_SetTheBlocksDiffAndFocusAttributes()
	{
		string html = Render("```csharp\na(); // [!code ++]\nb(); // [!code focus]\n```");

		html.Should().Contain("<div class=\"code-block\" data-lang=\"csharp\" data-diff data-focus>");
		Render("```csharp\na(); // [!code highlight]\n```").Should().NotContain("data-diff").And.NotContain("data-focus");
	}

	[Fact]
	public void DiffBlock_MarksAddedAndRemovedLinesAndDropsTheirSigns()
	{
		const string md = """
		                  ```diff
		                  diff --git a/x.txt b/x.txt
		                  --- a/x.txt
		                  +++ b/x.txt
		                  @@ -1,3 +1,3 @@
		                   keep
		                  -old
		                  +new
		                    indented
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("<div class=\"code-block\" data-lang=\"diff\" data-diff>").And.Contain("class=\"language-diff\"");
		Lines(html).Should().Equal(
			("diff-hunk", "diff --git a/x.txt b/x.txt"),
			("diff-hunk", "--- a/x.txt"),
			("diff-hunk", "+++ b/x.txt"),
			("diff-hunk", "@@ -1,3 +1,3 @@"),
			("", "keep"),
			("diff-remove", "old"),
			("diff-add", "new"),
			("", " indented"));
		CopyText(html).Should().Be("keep\nnew\n indented");
	}

	[Fact]
	public void DiffBlock_KeepsContextIndentationWhenTheContextLinesHaveNoPrefix()
	{
		string html = Render("```diff\n{\n-    Old();\n+    New();\n}\n```");

		Lines(html).Should().Equal(("", "{"), ("diff-remove", "    Old();"), ("diff-add", "    New();"), ("", "}"));
		CopyText(html).Should().Be("{\n    New();\n}");
	}

	[Fact]
	public void DiffWithALanguage_HighlightsTheCode()
	{
		string html = Render("```diff-csharp\n public void M()\n {\n-    var a = 1;\n+    var b = 2;\n }\n```");

		html.Should().Contain("<div class=\"code-block\" data-lang=\"csharp\" data-diff>")
			.And.Contain("aria-label=\"C# code\"")
			.And.Contain("<span class=\"line diff-add\">    <span class=\"token keyword\">var</span> b = <span class=\"token number\">2</span>;</span>")
			.And.Contain("<span class=\"line\"><span class=\"token keyword\">public</span> <span class=\"token keyword\">void</span>");
		CopyText(html).Should().Be("public void M()\n{\n    var b = 2;\n}");
	}

	[Fact]
	public void PlusAndMinus_OutsideADiffBlock_AreCode()
	{
		string html = Render("```bash\n-v\n+x\n```");

		Lines(html).Should().Equal(("", "-v"), ("", "+x"));
		html.Should().NotContain("data-diff");
	}

	[Theory]
	[InlineData("csharp showLineNumbers", " data-line-numbers=\"true\"")]
	[InlineData("csharp ShowLineNumbers", " data-line-numbers=\"true\"")]
	[InlineData("csharp noLineNumbers", " data-line-numbers=\"false\"")]
	[InlineData("csharp wrap", " data-wrap")]
	[InlineData("csharp noLineNumbers wrap", " data-line-numbers=\"false\" data-wrap")]
	[InlineData("csharp", "")]
	public void LineNumberAndWrapOptions_AreAttributesOnTheBlock(string info, string attributes)
	{
		string html = Render($"```{info}\nx\n```");

		html.Should().StartWith($"<div class=\"code-block\" data-lang=\"csharp\"{attributes}>");
	}

	[Fact]
	public void IndentedCode_GetsTheSameMarkupWithoutALanguage()
	{
		string html = Render("Text\n\n    line one\n      line two\n");

		html.Should().Contain("<div class=\"code-block\"><pre><code data-highlighted=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"Code\">"
		                      + "<span class=\"line\">line one</span>\n<span class=\"line\">  line two</span></code></pre></div>");
	}

	[Fact]
	public void FenceWithoutALanguage_IsPlainText()
	{
		string html = Render("```\nvar x = \"<y>\";\n```");

		html.Trim().Should().Be("<div class=\"code-block\"><pre><code data-highlighted=\"true\" tabindex=\"0\" role=\"group\" aria-label=\"Code\">"
		                        + "<span class=\"line\">var x = &quot;&lt;y&gt;&quot;;</span></code></pre></div>");
	}

	[Fact]
	public void EmptyFence_HasNoLines()
	{
		string html = Render("```csharp\n```");

		html.Should().Contain("aria-label=\"C# code\"></code></pre></div>");
	}

	[Fact]
	public void GenericAttributes_StayOnTheCodeElement()
	{
		string html = Render("```csharp {#sample .wide}\nx\n```");

		html.Should().Contain("<code id=\"sample\" class=\"language-csharp wide\" data-highlighted=\"true\"");
	}

	[Fact]
	public void LanguageInAnyCase_IsHighlightedAndLabelledAsWritten()
	{
		string html = Render("```CSharp\nvar x = 1;\n```");

		html.Should().Contain("data-lang=\"CSharp\"").And.Contain("<span class=\"token keyword\">var</span>");
	}

	[Fact]
	public void Mermaid_IsNotWrapped()
	{
		string html = Render("```mermaid\ngraph TD; A-->B\n```");

		html.Trim().Should().Be("<pre class=\"mermaid\">graph TD; A--&gt;B</pre>");
	}

	[Fact]
	public void BlazorPreviewSource_StaysPlainForThePluginThatCompilesIt()
	{
		// BlazorPreviewPlugin reads the text between <code ...> and </code> and decodes it; spans
		// would end up in the Razor it compiles. CodeHighlightPhase highlights it after the plugins.
		const string source = "<button @onclick=\"Inc\">Count: @count</button>\n@code { int count; void Inc() => count++; }";

		string html = Render($"```blazor-preview\n{source}\n```");

		int start = html.IndexOf('>', html.IndexOf("<code", StringComparison.Ordinal)) + 1;
		string content = html[start..html.IndexOf("</code>", start, StringComparison.Ordinal)];
		content.Should().NotContain("<span");
		WebUtility.HtmlDecode(content).Should().Be(source);
	}

	[Fact]
	public void Repl_IsHighlightedWithThePreLastInItsWrapper()
	{
		string html = Render("```csharp-repl\nConsole.WriteLine(\"hi\"); // [!code highlight]\n```");

		html.Should().StartWith("<div class=\"repl-container\" data-repl=\"true\"><div class=\"code-block\" data-lang=\"csharp\"><pre><code class=\"language-csharp\"")
			.And.Contain("<span class=\"line highlighted\"><span class=\"token type-name\">Console</span>")
			.And.Contain("</code></pre></div><div class=\"repl-output\" style=\"display:none;\"></div></div>");
		Text(html).Should().Be("Console.WriteLine(\"hi\");");
	}

	[Fact]
	public void ReplTitle_ShowsLikeAnyOtherBlock()
	{
		string html = Render("```csharp repl title=\"Demo.cs\"\nvar x = 1;\n```");

		html.Should().Contain("data-repl=\"true\"").And.Contain("<div class=\"code-title\">Demo.cs</div>");
	}

	private static string Text(string html)
	{
		int start = html.IndexOf('>', html.IndexOf("<code", StringComparison.Ordinal)) + 1;
		string code = html[start..html.IndexOf("</code>", start, StringComparison.Ordinal)];
		return WebUtility.HtmlDecode(Regex.Replace(code, "<[^>]+>", ""));
	}

	private static List<(string Classes, string Text)> Lines(string html) =>
		Regex.Matches(html, "<span class=\"line(?<classes>[^\"]*)\">(?<html>.*?)</span>(?=\n|</code>)")
			.Select(m => (m.Groups["classes"].Value.Trim(), WebUtility.HtmlDecode(Regex.Replace(m.Groups["html"].Value, "<[^>]+>", ""))))
			.ToList();

	// What the theme's Copy button copies: every line but removed lines and hunk headers.
	private static string CopyText(string html) =>
		string.Join('\n', Lines(html).Where(l => !l.Classes.Contains("diff-remove") && !l.Classes.Contains("diff-hunk")).Select(l => l.Text));
}
