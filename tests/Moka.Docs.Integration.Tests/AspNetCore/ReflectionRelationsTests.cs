using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Api;
using static Moka.Docs.Integration.Tests.AspNetCore.ReflectionSourceTests;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Base types, interfaces, inherited members and <c>&lt;inheritdoc/&gt;</c> across the types of a
///     reflected assembly: names qualified like the CLI's, nested types of generic types and interface
///     diamonds.
/// </summary>
public sealed class ReflectionRelationsTests
{
	private const string _source = """
	                               using System.Collections.Generic;

	                               namespace Fixture.Relations
	                               {
	                                   /// <summary>Not System.Exception.</summary>
	                                   public class Exception
	                                   {
	                                       /// <summary>The error code.</summary>
	                                       public int Code { get; set; }

	                                       /// <summary>Why it failed.</summary>
	                                       public string Reason { get; set; } = "";
	                                   }

	                                   /// <summary>A failure.</summary>
	                                   public class Failure : System.Exception
	                                   {
	                                       public int Code => 0;
	                                   }

	                                   /// <summary>A fault.</summary>
	                                   public class Fault : Exception
	                                   {
	                                   }

	                                   /// <summary>An outer type.</summary>
	                                   public class Outer<T>
	                                   {
	                                       /// <summary>An inner type.</summary>
	                                       public class Inner
	                                       {
	                                           /// <summary>Runs the value.</summary>
	                                           /// <param name="value">The value.</param>
	                                           public virtual void Run(T value) { }

	                                           /// <summary>Looks at the value.</summary>
	                                           /// <param name="value">The value.</param>
	                                           public void Peek(T value) { }
	                                       }
	                                   }

	                                   /// <summary>Derives from a nested type of a generic type.</summary>
	                                   public class Derived : Outer<int>.Inner, IEnumerable<KeyValuePair<string, Exception>>
	                                   {
	                                       /// <inheritdoc />
	                                       public override void Run(int value) { }

	                                       /// <inheritdoc />
	                                       public IEnumerator<KeyValuePair<string, Exception>> GetEnumerator() => null!;

	                                       System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!;
	                                   }

	                                   /// <summary>The root of a diamond.</summary>
	                                   public interface IBase
	                                   {
	                                       /// <summary>Opens it.</summary>
	                                       void Open();

	                                       /// <summary>Closes it.</summary>
	                                       void Close();
	                                   }

	                                   /// <summary>One side.</summary>
	                                   public interface IA : IBase
	                                   {
	                                       /// <summary>Resets A.</summary>
	                                       void Reset();

	                                       /// <summary>Closes A.</summary>
	                                       new void Close();
	                                   }

	                                   /// <summary>The other side.</summary>
	                                   public interface IB : IBase
	                                   {
	                                       /// <summary>Resets B.</summary>
	                                       void Reset();
	                                   }

	                                   /// <summary>Both sides.</summary>
	                                   public interface IC : IA, IB
	                                   {
	                                   }
	                               }
	                               """;

	private static readonly Lazy<Assembly> _assembly = new(() =>
		FixtureAssembly.Compile("Moka.Docs.RelationsFixture", _source, LanguageVersion.Latest, "relations-fixture"));

	private static readonly Lazy<Task<InMemorySite>> _site = new(() => SiteHost.BuildAsync(_assembly.Value));

	private static ApiReference Model() => Build(_assembly.Value);

	private static async Task<string> PageAsync(string type) =>
		SiteHost.Html(await _site.Value, $"api/fixture/relations/{type}/index.html");

	private static string InheritedSection(string page)
	{
		int start = page.IndexOf("<h2 id=\"inherited-members\">", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the page should list inherited members");
		return page[start..page.IndexOf("</table>", start, StringComparison.Ordinal)];
	}

	[Fact]
	public void BaseTypesAndInterfaces_AreQualifiedLikeTheClisModel()
	{
		ApiReference model = Model();

		Type(model, "Failure").BaseType.Should().Be("System.Exception");
		Type(model, "Fault").BaseType.Should().Be("Fixture.Relations.Exception");
		Type(model, "Derived").BaseType.Should().Be("Fixture.Relations.Outer<int>.Inner");
		Type(model, "Derived").ImplementedInterfaces.Should().Equal(
			"System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Fixture.Relations.Exception>>");
		Type(model, "IC").ImplementedInterfaces.Should().Equal("Fixture.Relations.IA", "Fixture.Relations.IB");
	}

	[Fact]
	public async Task ADocumentedTypeNamedException_IsNotTakenForSystemException()
	{
		Type(Model(), "Failure").Members.Single(m => m.Name == "Code").Documentation?.Summary.Should().BeNullOrEmpty();

		string failure = await PageAsync("failure");
		failure.Should().NotContain("inherited-members").And.NotContain("/docs/api/fixture/relations/exception#");
		InheritedSection(await PageAsync("fault")).Should().Contain("/docs/api/fixture/relations/exception#reason");

		string exception = await PageAsync("exception");
		int diagram = exception.IndexOf("<pre class=\"mermaid\">", StringComparison.Ordinal);
		diagram.Should().BeGreaterThanOrEqualTo(0);
		string graph = exception[diagram..exception.IndexOf("</pre>", diagram, StringComparison.Ordinal)];
		graph.Should().Contain("class T1[\"Fault\"]").And.NotContain("Failure");
	}

	[Fact]
	public async Task NestedTypeOfAGenericBase_PassesItsMembersAndDocumentationOn()
	{
		ApiMember run = Type(Model(), "Derived").Members.Single(m => m.Name == "Run");
		run.Documentation!.Summary.Should().Be("Runs the value.");

		string section = InheritedSection(await PageAsync("derived"));
		section.Should().Contain("<td><a href=\"/docs/api/fixture/relations/outer.inner\">Outer&lt;int&gt;.Inner</a></td>")
			.And.Contain("<a href=\"/docs/api/fixture/relations/outer.inner#peek\" class=\"api-member-link\"><code>Peek(int)</code></a>")
			.And.NotContain("#run");
	}

	[Fact]
	public async Task InterfaceDiamond_ListsTheSameNamedMemberOfEachSide()
	{
		string section = InheritedSection(await PageAsync("ic"));

		section.Should().Contain("/docs/api/fixture/relations/ia#reset")
			.And.Contain("/docs/api/fixture/relations/ib#reset")
			.And.Contain("/docs/api/fixture/relations/ia#close")
			.And.Contain("/docs/api/fixture/relations/ibase#open")
			.And.NotContain("/docs/api/fixture/relations/ibase#close");
	}
}
