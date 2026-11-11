using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

/// <summary>
///     What the parser writes into HTML: the comment author's text always escaped, links only to safe
///     URLs, and code blocks and lists that keep their shape.
/// </summary>
public sealed class XmlDocParserMarkupTests
{
	private readonly XmlDocParser _parser = new(NullLogger<XmlDocParser>.Instance);

	private string Summary(string inner) =>
		_parser.ParseXml($"""
		                  <doc><members><member name="T:Lib.Widget">
		                  <summary>{inner}</summary>
		                  </member></members></doc>
		                  """).Members["T:Lib.Widget"].Summary;

	[Fact]
	public void ParamrefAndTypeparamrefNames_AreEscaped()
	{
		// The auditor's payload: the name went into the page as markup.
		string html = Summary("""Uses <paramref name="&lt;img src=x onerror=alert(1)&gt;"/> and <typeparamref name="&lt;script&gt;alert(2)&lt;/script&gt;"/>.""");

		html.Should().Be("Uses <code>&lt;img src=x onerror=alert(1)&gt;</code> and <code>&lt;script&gt;alert(2)&lt;/script&gt;</code>.");
		html.Should().NotContain("<img").And.NotContain("<script");
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("java&#9;script:alert(3)")]
	[InlineData("java&#10;script:alert(3)")]
	[InlineData("&#13;javascript:alert(3)")]
	[InlineData("data:text/html,x")]
	public void SeeHref_UnsafeScheme_IsTextNotALink(string href)
	{
		string html = Summary($"""Read <see href="{href}">the guide</see>.""");

		html.Should().Be("Read the guide.");
	}

	[Theory]
	[InlineData("https://example.com/guide", "https://example.com/guide")]
	[InlineData("mailto:team@example.com", "mailto:team@example.com")]
	[InlineData("/guide/install", "/guide/install")]
	[InlineData("https://exa&#9;mple.com", "https://example.com")]
	public void SeeHref_SafeUrl_IsALink(string href, string expected)
	{
		Summary($"""Read <see href="{href}">the guide</see>.""")
			.Should().Be($"Read <a href=\"{expected}\">the guide</a>.");
	}

	[Fact]
	public void SeeHref_WithoutText_ShowsTheEscapedUrl()
	{
		Summary("""<see href="javascript:alert('&lt;b&gt;')"/>""").Should().Be("javascript:alert(&#39;&lt;b&gt;&#39;)");
	}

	[Fact]
	public void SeeAlsoHref_KeepsTheUrlForThePageToCheck()
	{
		XmlDocFile file = _parser.ParseXml("""
		                                   <doc><members><member name="T:Lib.Widget">
		                                   <seealso href="java&#9;script:alert(3)">Run</seealso>
		                                   </member></members></doc>
		                                   """);

		file.Members["T:Lib.Widget"].SeeAlso.Should().Equal("[Run](java\tscript:alert(3))");
	}

	[Fact]
	public void CodeBlock_LosesTheCommentIndentationAndSurroundingBlankLines()
	{
		string html = Summary("""
		                      <code>

		                          var widget = new Widget();
		                          if (widget.IsReady)
		                          {
		                              widget.Run();
		                          }

		                      </code>
		                      """);

		html.Should().Be("<pre><code>var widget = new Widget();\nif (widget.IsReady)\n{\n    widget.Run();\n}</code></pre>");
	}

	[Fact]
	public void BulletList_KeepsTheTermBeforeTheDescription()
	{
		string html = Summary("""
		                      <list type="bullet">
		                        <item><term>Fast</term><description>Runs in parallel.</description></item>
		                        <item><term>Only a term</term></item>
		                        <item><description>Only a description.</description></item>
		                        <item>Plain text.</item>
		                      </list>
		                      """);

		html.Should().Be("<ul><li><strong>Fast</strong>: Runs in parallel.</li><li><strong>Only a term</strong></li>"
		                 + "<li>Only a description.</li><li>Plain text.</li></ul>");
	}

	[Fact]
	public void TableList_RendersATableWithTheHeaderRow()
	{
		string html = Summary("""
		                      <list type="table">
		                        <listheader><term>Option</term><description>Effect</description></listheader>
		                        <item><term>&lt;fast&gt;</term><description>Skips <c>checks</c>.</description></item>
		                      </list>
		                      """);

		html.Should().Be("<div class=\"table-responsive\"><table><thead><tr><th>Option</th><th>Effect</th></tr></thead>"
		                 + "<tbody><tr><td>&lt;fast&gt;</td><td>Skips <code>checks</code>.</td></tr></tbody></table></div>");
	}
}
