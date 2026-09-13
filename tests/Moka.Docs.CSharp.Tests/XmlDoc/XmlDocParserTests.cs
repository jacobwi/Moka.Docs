using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

public sealed class XmlDocParserTests
{
	private readonly XmlDocParser _parser = new(NullLogger<XmlDocParser>.Instance);

	[Fact]
	public void ParseXml_ValidDoc_ExtractsSummary()
	{
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="T:MyNamespace.MyClass">
		                               <summary>A test class that does things.</summary>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocFile result = _parser.ParseXml(xml);

		result.AssemblyName.Should().Be("TestLib");
		result.Members.Should().ContainKey("T:MyNamespace.MyClass");
		result.Members["T:MyNamespace.MyClass"].Summary.Should().Be("A test class that does things.");
	}

	[Fact]
	public void ParseXml_MethodWithParams_ExtractsAll()
	{
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Calculator.Add(System.Int32,System.Int32)">
		                               <summary>Adds two numbers.</summary>
		                               <param name="a">First number.</param>
		                               <param name="b">Second number.</param>
		                               <returns>The sum.</returns>
		                               <exception cref="T:System.OverflowException">When result overflows.</exception>
		                               <example>var result = calc.Add(1, 2);</example>
		                               <seealso cref="M:MyApp.Calculator.Subtract"/>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocFile result = _parser.ParseXml(xml);
		XmlDocBlock doc = result.Members["M:MyApp.Calculator.Add(System.Int32,System.Int32)"];

		doc.Summary.Should().Be("Adds two numbers.");
		doc.Parameters.Should().ContainKey("a").WhoseValue.Should().Be("First number.");
		doc.Parameters.Should().ContainKey("b").WhoseValue.Should().Be("Second number.");
		doc.Returns.Should().Be("The sum.");
		doc.Exceptions.Should().HaveCount(1);
		doc.Exceptions[0].Type.Should().Be("System.OverflowException");
		doc.Exceptions[0].Description.Should().Be("When result overflows.");
		doc.Examples.Should().HaveCount(1);
		doc.SeeAlso.Should().Contain("M:MyApp.Calculator.Subtract");
	}

	[Fact]
	public void ParseXml_InlineElements_RenderedCorrectly()
	{
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Foo.Bar">
		                               <summary>Uses <c>inline code</c> and <see cref="T:System.String"/>.</summary>
		                               <remarks><para>First paragraph.</para><para>Second paragraph.</para></remarks>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocFile result = _parser.ParseXml(xml);
		XmlDocBlock doc = result.Members["M:MyApp.Foo.Bar"];

		doc.Summary.Should().Contain("<code>inline code</code>");
		doc.Summary.Should().Contain("<a data-cref=\"T:System.String\">");
		doc.Remarks.Should().Contain("<p>First paragraph.</p>");
		doc.Remarks.Should().Contain("<p>Second paragraph.</p>");
	}

	[Fact]
	public void ParseXml_EscapedAngleBrackets_StayEscapedInTheHtml()
	{
		// The reader decodes &lt;T&gt; to <T>, which went into the page as a tag, so
		// <c>List&lt;T&gt;</c> rendered as "List".
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Foo.Bar">
		                               <summary>Returns a <c>List&lt;T&gt;</c> when a &lt; b &amp;&amp; b &gt; c.</summary>
		                               <example><code>var x = new List&lt;int&gt;();</code></example>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocBlock doc = _parser.ParseXml(xml).Members["M:MyApp.Foo.Bar"];

		doc.Summary.Should().Be("Returns a <code>List&lt;T&gt;</code> when a &lt; b &amp;&amp; b &gt; c.");
		doc.Examples.Should().ContainSingle().Which.Should().Be("<pre><code>var x = new List&lt;int&gt;();</code></pre>");
	}

	[Fact]
	public void ParseXml_SeeLangword_RendersTheKeyword()
	{
		// <see langword="null" /> has no cref or href and used to render nothing at all.
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Foo.Find">
		                               <returns>The item, or <see langword="null" /> when missing.</returns>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		_parser.ParseXml(xml).Members["M:MyApp.Foo.Find"].Returns
			.Should().Be("The item, or <code>null</code> when missing.");
	}

	[Fact]
	public void ParseXml_ParamrefAndTypeparamref_Rendered()
	{
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Foo.Process``1(``0)">
		                               <summary>Processes <paramref name="value"/> of type <typeparamref name="T"/>.</summary>
		                               <typeparam name="T">The value type.</typeparam>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocFile result = _parser.ParseXml(xml);
		XmlDocBlock doc = result.Members["M:MyApp.Foo.Process``1(``0)"];

		doc.Summary.Should().Contain("<code>value</code>");
		doc.Summary.Should().Contain("<code>T</code>");
		doc.TypeParameters.Should().ContainKey("T");
	}

	[Fact]
	public void ParseXml_EmptyDoc_ReturnsEmpty()
	{
		XmlDocFile result = _parser.ParseXml("<doc><assembly><name>Empty</name></assembly><members/></doc>");

		result.AssemblyName.Should().Be("Empty");
		result.Members.Should().BeEmpty();
	}

	[Fact]
	public void ParseXml_InvalidXml_ReturnsEmpty()
	{
		XmlDocFile result = _parser.ParseXml("not xml at all");

		result.Members.Should().BeEmpty();
		result.AssemblyName.Should().BeEmpty();
	}

	[Fact]
	public void ParseXml_ListElement_RenderedAsList()
	{
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="T:MyApp.Foo">
		                               <summary>
		                                   Supports:
		                                   <list type="bullet">
		                                       <item><description>Item one</description></item>
		                                       <item><description>Item two</description></item>
		                                   </list>
		                               </summary>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocFile result = _parser.ParseXml(xml);
		XmlDocBlock doc = result.Members["T:MyApp.Foo"];

		doc.Summary.Should().Contain("<ul>");
		doc.Summary.Should().Contain("<li>Item one</li>");
		doc.Summary.Should().Contain("<li>Item two</li>");
	}

	[Fact]
	public void ParseXml_InheritDocCref_IsKept()
	{
		// Only the source analyzer stored the cref, so the ASP.NET Core host ignored it.
		const string xml = """
		                   <?xml version="1.0"?>
		                   <doc>
		                       <assembly><name>TestLib</name></assembly>
		                       <members>
		                           <member name="M:MyApp.Foo.ProcessMarkdown(System.String)">
		                               <inheritdoc cref="M:MyApp.IProcessor.Process(System.String)"/>
		                           </member>
		                       </members>
		                   </doc>
		                   """;

		XmlDocBlock doc = _parser.ParseXml(xml).Members["M:MyApp.Foo.ProcessMarkdown(System.String)"];

		doc.HasInheritDocTag.Should().BeTrue();
		doc.InheritDocCref.Should().Be("M:MyApp.IProcessor.Process(System.String)");
	}
}
