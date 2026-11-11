using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Moka.Docs.Engine.Minification;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Integration.Tests.Output;

public sealed class MinifierTests
{
	private static readonly Lazy<bool> _nodeAvailable = new(() =>
	{
		try
		{
			using Process? node = Process.Start(new ProcessStartInfo("node", "--version")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false
			});
			return node is not null && node.WaitForExit(10_000) && node.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			return false;
		}
	});

	[Fact]
	public void Html_CollapsesWhitespaceToOneCharacterAndKeepsLineBreaks()
	{
		const string html = "<div>\r\n    <p>Some   text\tand\r\n\r\n   more</p>  <span>a</span> <span>b</span>\n</div>";

		HtmlMinifier.Minify(html).Should().Be("<div>\n<p>Some text and\nmore</p> <span>a</span> <span>b</span>\n</div>");
	}

	[Fact]
	public void Html_KeepsPreTextareaCodeScriptAndStyleAsWritten()
	{
		string html = """
		                    <PRE class="x">  line 1
		                        <b>bold</b>   kept
		                    </PRE>
		                    <textarea>  a

		                      b</textarea>
		                    <p>Inline <code>a    b</code> code</p>
		                    <script>
		                      if (a  <  b) { x = "  "; }
		                    </script>
		                    <style>
		                      p   { color: red; }
		                    </style>
		                    """.ReplaceLineEndings("\n");

		string minified = HtmlMinifier.Minify(html);

		minified.Should().Contain("<PRE class=\"x\">  line 1\n    <b>bold</b>   kept\n</PRE>")
			.And.Contain("<textarea>  a\n\n  b</textarea>")
			.And.Contain("<code>a    b</code>")
			.And.Contain("<script>\n  if (a  <  b) { x = \"  \"; }\n</script>")
			.And.Contain("<style>\n  p   { color: red; }\n</style>");
	}

	[Fact]
	public void Html_DropsCommentsButKeepsConditionalComments()
	{
		const string html = "<p>a</p><!-- note -->\n<!--[if IE]><p>old</p><![endif]--><!--[if !IE]><!--><p>new</p><!--<![endif]-->";

		HtmlMinifier.Minify(html).Should()
			.Be("<p>a</p>\n<!--[if IE]><p>old</p><![endif]--><!--[if !IE]><!--><p>new</p><!--<![endif]-->");
	}

	[Fact]
	public void Html_CopiesTagsAndAttributesAsWritten()
	{
		const string html = "<a   title=\"a > b   c\"\n   href='/x'>link</a> <img src=x.png alt='1 < 2'/>   <!DOCTYPE  html>";

		HtmlMinifier.Minify(html).Should()
			.Be("<a   title=\"a > b   c\"\n   href='/x'>link</a> <img src=x.png alt='1 < 2'/> <!DOCTYPE  html>");
	}

	[Fact]
	public void Html_KeepsALiteralLessThanSign() =>
		HtmlMinifier.Minify("<p>a <  b</p>").Should().Be("<p>a < b</p>");

	[Fact]
	public void Css_DropsCommentsAndTheSpacesAroundBracesSemicolonsAndCommas()
	{
		const string css = """
		                   /* header */
		                   a ,  b > c {
		                       color : red ;
		                       margin: 0 auto;   /* trailing */
		                   }

		                   @media (max-width: 768px) {
		                       .x { padding: 0; }
		                   }
		                   """;

		CssMinifier.Minify(css).Should()
			.Be("a,b > c{color : red;margin: 0 auto}@media (max-width: 768px){.x{padding: 0}}");
	}

	[Theory]
	[InlineData("a :hover { x: y }", "a :hover{x: y}")]
	[InlineData("p::after { content: '  /* not a comment */  '; }", "p::after{content: '  /* not a comment */  '}")]
	[InlineData("p { background: url(data:image/png;base64,ab/*cd) no-repeat; }", "p{background: url(data:image/png;base64,ab/*cd) no-repeat}")]
	[InlineData("p { width: calc(100% - 2rem); }", "p{width: calc(100% - 2rem)}")]
	[InlineData(":root { --empty: ; --x: 1; }", ":root{--empty: ;--x: 1}")]
	[InlineData("p { color: red !important; }", "p{color: red !important}")]
	[InlineData("a/**/b { }", "a b{}")]
	public void Css_KeepsWhatChangesMeaning(string css, string expected) =>
		CssMinifier.Minify(css).Should().Be(expected);

	[Fact]
	public void Js_DropsCommentsAndIndentationButKeepsLineBreaks()
	{
		const string js = """
		                  // leading comment
		                  function f(a, b) {
		                      /* block */ return a   +   b; // trailing
		                  }

		                  /*
		                   * multi-line
		                   */
		                  const x = f(1, 2);
		                  """;

		JsMinifier.Minify(js).Replace("\r", "").Should().Be("function f(a,b){\nreturn a+b;\n}\nconst x=f(1,2);");
	}

	[Theory]
	[InlineData("const re = /\\/\\/.*$|\\/\\*[\\s\\S]*?\\*\\//gm ;", "const re=/\\/\\/.*$|\\/\\*[\\s\\S]*?\\*\\//gm;")]
	[InlineData("x = { s: /(`(?:[^`\\\\]|\\\\.)*`|\"x\")/g , n: 1 };", "x={s:/(`(?:[^`\\\\]|\\\\.)*`|\"x\")/g,n:1};")]
	[InlineData("const c = /[/]/ ;", "const c=/[/]/;")]
	[InlineData("return /a  b/.test(s);", "return/a  b/.test(s);")]
	[InlineData("ok = /a/ instanceof RegExp || /b/g in x;", "ok=/a/ instanceof RegExp||/b/g in x;")]
	[InlineData("y = a / b / c;", "y=a/b/c;")]
	[InlineData("y = (a + b) / 2 / c;", "y=(a+b)/2/c;")]
	[InlineData("y = i++ / 2;", "y=i++/2;")]
	[InlineData("s = 'a  // b' + \"c /* d */\";", "s='a  // b'+\"c /* d */\";")]
	[InlineData("t = `a  ${ b ? `x  ${ c }` : '}' }  z // c`;", "t=`a  ${b?`x  ${c}`:'}'}  z // c`;")]
	[InlineData("t = `${ { a: 1 }.a } /x/ ${ /re  /.source }`;", "t=`${{a:1}.a} /x/ ${/re  /.source}`;")]
	[InlineData("a = b + +c; d = e - -f; g = h + ++i;", "a=b+ +c;d=e- -f;g=h+ ++i;")]
	[InlineData("n = 1 .toString(); m = x . y;", "n=1 .toString();m=x.y;")]
	[InlineData("if (a < !--b) {}", "if(a< !--b){}")]
	[InlineData("q = a / /re/.source.length;", "q=a/ /re/.source.length;")]
	public void Js_KeepsLiteralsAndTheSpacesTokensNeed(string js, string expected) =>
		JsMinifier.Minify(js).Should().Be(expected);

	[Theory]
	[InlineData("return\nx", "return\nx")]
	[InlineData("a\n++b", "a\n++b")]
	[InlineData("a = b\n(c)", "a=b\n(c)")]
	[InlineData("x = 1 /* one\ntwo */ y = 2", "x=1\ny=2")]
	public void Js_KeepsTheLineBreaksSemicolonInsertionNeeds(string js, string expected) =>
		JsMinifier.Minify(js).Should().Be(expected);

	[Fact]
	public void ThemeScript_StillParsesAndKeepsEveryHighlighterPattern()
	{
		string js = EmbeddedThemeProvider.GetJs();
		string minified = JsMinifier.Minify(js);

		minified.Length.Should().BeLessThan(js.Length * 4 / 5);
		// Each language's patterns hold "//", quotes and backticks; a tokenizer that lost track of one
		// would cut the rest of the line as a comment or merge it with the next.
		HashSet<string> minifiedLines = minified.Split('\n').Select(Squeeze).ToHashSet(StringComparer.Ordinal);
		string[] highlighterLines = js.Split('\n').Where(l => l.Contains("keywords: /", StringComparison.Ordinal)).ToArray();
		highlighterLines.Should().HaveCountGreaterThan(10);
		foreach (string line in highlighterLines)
		{
			minifiedLines.Should().Contain(Squeeze(line));
		}

		minified.Should().Contain("|yield from)", "spaces inside a regex are content");

		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-min-" + Guid.NewGuid().ToString("N") + ".js");
		File.WriteAllText(path, minified);
		try
		{
			using Process node = Process.Start(new ProcessStartInfo("node", ["--check", path])
			{
				RedirectStandardError = true,
				UseShellExecute = false
			})!;
			string errors = node.StandardError.ReadToEnd();
			node.WaitForExit(30_000).Should().BeTrue();
			node.ExitCode.Should().Be(0, errors);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void ThemeStylesheet_LosesOnlyCommentsAndWhitespace()
	{
		string css = EmbeddedThemeProvider.GetCss();
		string minified = CssMinifier.Minify(css);

		minified.Length.Should().BeLessThan(css.Length * 9 / 10);
		minified.Should().NotContain("/*").And.NotContain("\n");
		Squeeze(minified).Should().Be(Squeeze(StripCssComments(css)).Replace(";}", "}"));
	}

	private static string Squeeze(string text) => new(text.Where(c => !char.IsWhiteSpace(c)).ToArray());

	private static string StripCssComments(string css)
	{
		var sb = new StringBuilder();
		int i = 0;
		while (i < css.Length)
		{
			int start = css.IndexOf("/*", i, StringComparison.Ordinal);
			if (start < 0)
			{
				sb.Append(css, i, css.Length - i);
				break;
			}

			sb.Append(css, i, start - i);
			int end = css.IndexOf("*/", start + 2, StringComparison.Ordinal);
			i = end < 0 ? css.Length : end + 2;
		}

		return sb.ToString();
	}
}
