using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

/// <summary>
///     How <c>&lt;see&gt;</c> and <c>&lt;seealso&gt;</c> references are stored for the page renderer.
/// </summary>
public sealed class XmlDocParserReferenceTests
{
	private readonly XmlDocParser _parser = new(NullLogger<XmlDocParser>.Instance);

	private XmlDocBlock Parse(string memberXml) =>
		_parser.ParseXml($"<doc><assembly><name>TestLib</name></assembly><members>{memberXml}</members></doc>")
			.Members.Values.Single();

	[Fact]
	public void SeeAlso_KeepsTheUrlAndTextOfEachEntry()
	{
		// Only the cref or the text was kept, so an href entry lost its URL and a bare href vanished.
		XmlDocBlock doc = Parse("""
		                        <member name="T:MyApp.Widget">
		                            <seealso cref="T:MyApp.Gadget" />
		                            <seealso cref="T:MyApp.Gadget">the gadget</seealso>
		                            <seealso href="https://example.com/guide">Widget
		                                guide</seealso>
		                            <seealso href="https://example.com/bare" />
		                        </member>
		                        """);

		doc.SeeAlso.Should().Equal(
			"T:MyApp.Gadget",
			"[the gadget](T:MyApp.Gadget)",
			"[Widget guide](https://example.com/guide)",
			"[https://example.com/bare](https://example.com/bare)");
	}

	[Theory]
	[InlineData("T:MyApp.Widget`1", "Widget")]
	[InlineData("M:MyApp.Widget`1.Map``1(System.Func{`0,``0})", "Widget.Map")]
	[InlineData("M:MyApp.Widget.#ctor(System.Int32)", "Widget")]
	[InlineData("P:MyApp.Widget.Size", "Widget.Size")]
	[InlineData("!:List<T>", "List<T>")]
	public void SeeCrefWithoutText_ShowsAReadableName(string cref, string expected)
	{
		// Metadata syntax leaked into the text: "Widget`1", "Widget`1.Map``1" and "Widget.#ctor".
		XmlDocBlock doc = Parse($"""<member name="T:MyApp.Widget"><summary><see cref="{cref.Replace("<", "&lt;").Replace(">", "&gt;")}" /></summary></member>""");

		doc.Summary.Should().EndWith($">{expected.Replace("<", "&lt;").Replace(">", "&gt;")}</a>");
		XmlDocParser.GetCrefDisplayName(cref).Should().Be(expected);
	}
}
