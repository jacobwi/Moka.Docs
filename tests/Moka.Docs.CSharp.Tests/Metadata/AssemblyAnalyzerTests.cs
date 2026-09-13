using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

public sealed class AssemblyAnalyzerTests
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);

	private ApiReference AnalyzeSource(string source)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		return _analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly");
	}

	[Fact]
	public void Analyze_SimpleClass_ExtractsType()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A test class.</summary>
		                                        public class MyClass
		                                        {
		                                            /// <summary>A test method.</summary>
		                                            public int Add(int a, int b) => a + b;
		                                        }
		                                    }
		                                    """);

		result.Namespaces.Should().HaveCount(1);
		result.Namespaces[0].Name.Should().Be("TestNs");
		result.Namespaces[0].Types.Should().HaveCount(1);

		ApiType type = result.Namespaces[0].Types[0];
		type.Name.Should().Be("MyClass");
		type.Kind.Should().Be(ApiTypeKind.Class);
		type.Documentation.Should().NotBeNull();
		type.Documentation!.Summary.Should().Be("A test class.");
	}

	[Fact]
	public void Analyze_Methods_ExtractsParameters()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        public class Calc
		                                        {
		                                            /// <summary>Adds values.</summary>
		                                            /// <param name="a">First.</param>
		                                            /// <param name="b">Second.</param>
		                                            /// <returns>The sum.</returns>
		                                            public int Add(int a, int b) => a + b;
		                                        }
		                                    }
		                                    """);

		ApiMember method = result.Namespaces[0].Types[0].Members
			.First(m => m.Name == "Add");

		method.Kind.Should().Be(ApiMemberKind.Method);
		method.Parameters.Should().HaveCount(2);
		method.Parameters[0].Name.Should().Be("a");
		method.Parameters[0].Type.Should().Be("int");
		method.ReturnType.Should().Be("int");
		method.Documentation.Should().NotBeNull();
		method.Documentation!.Summary.Should().Be("Adds values.");
		method.Documentation.Parameters.Should().ContainKey("a");
	}

	[Fact]
	public void Analyze_Interface_ExtractsCorrectly()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A shape.</summary>
		                                        public interface IShape
		                                        {
		                                            /// <summary>Gets the area.</summary>
		                                            double Area { get; }

		                                            /// <summary>Draws the shape.</summary>
		                                            void Draw();
		                                        }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.Kind.Should().Be(ApiTypeKind.Interface);
		type.Members.Should().Contain(m => m.Name == "Area" && m.Kind == ApiMemberKind.Property);
		type.Members.Should().Contain(m => m.Name == "Draw" && m.Kind == ApiMemberKind.Method);
	}

	[Fact]
	public void Analyze_Enum_ExtractsMembers()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>Colors.</summary>
		                                        public enum Color
		                                        {
		                                            /// <summary>Red.</summary>
		                                            Red,
		                                            /// <summary>Green.</summary>
		                                            Green = 5,
		                                            /// <summary>Blue.</summary>
		                                            Blue
		                                        }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.Kind.Should().Be(ApiTypeKind.Enum);
		type.Members.Should().HaveCount(3);
		type.Members[0].Name.Should().Be("Red");
	}

	[Fact]
	public void Analyze_GenericClass_ExtractsTypeParams()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A collection.</summary>
		                                        /// <typeparam name="T">Element type.</typeparam>
		                                        public class MyList<T> where T : class, new()
		                                        {
		                                            /// <summary>Adds item.</summary>
		                                            public void Add(T item) { }
		                                        }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.TypeParameters.Should().HaveCount(1);
		type.TypeParameters[0].Name.Should().Be("T");
		type.TypeParameters[0].Constraints.Should().Contain("class");
		type.TypeParameters[0].Constraints.Should().Contain("new()");
	}

	[Fact]
	public void Analyze_Properties_ExtractsGetSet()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        public class Person
		                                        {
		                                            /// <summary>The name.</summary>
		                                            public string Name { get; set; } = "";

		                                            /// <summary>Read-only age.</summary>
		                                            public int Age { get; }
		                                        }
		                                    }
		                                    """);

		List<ApiMember> members = result.Namespaces[0].Types[0].Members;
		members.Should().Contain(m => m.Name == "Name" && m.Kind == ApiMemberKind.Property);
		members.Should().Contain(m => m.Name == "Age" && m.Kind == ApiMemberKind.Property);
	}

	[Fact]
	public void Analyze_StaticClass_MarksStatic()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>Extensions.</summary>
		                                        public static class Extensions
		                                        {
		                                            /// <summary>Trims text.</summary>
		                                            public static string TrimAll(this string s) => s.Trim();
		                                        }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.IsStatic.Should().BeTrue();

		ApiMember method = type.Members[0];
		method.IsStatic.Should().BeTrue();
		method.IsExtensionMethod.Should().BeTrue();
	}

	[Fact]
	public void Analyze_ObsoleteType_MarksFlagAndMessage()
	{
		ApiReference result = AnalyzeSource("""
		                                    using System;
		                                    namespace TestNs
		                                    {
		                                        /// <summary>Old class.</summary>
		                                        [Obsolete("Use NewClass instead.")]
		                                        public class OldClass { }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.IsObsolete.Should().BeTrue();
		type.ObsoleteMessage.Should().Be("Use NewClass instead.");
	}

	[Fact]
	public void Analyze_PrivateMembers_Excluded()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        public class Foo
		                                        {
		                                            private int _secret;
		                                            private void Hidden() { }
		                                            public int Visible { get; set; }
		                                        }
		                                    }
		                                    """);

		List<ApiMember> members = result.Namespaces[0].Types[0].Members;
		members.Should().NotContain(m => m.Name == "_secret");
		members.Should().NotContain(m => m.Name == "Hidden");
		members.Should().Contain(m => m.Name == "Visible");
	}

	private const string _accessibilitySource = """
	                                           namespace TestNs
	                                           {
	                                               public class Widget
	                                               {
	                                                   public void PublicMethod() { }
	                                                   protected void ProtectedMethod() { }
	                                                   protected internal void ProtectedInternalMethod() { }
	                                                   internal void InternalMethod() { }
	                                                   private protected void PrivateProtectedMethod() { }
	                                               }

	                                               internal class Hidden
	                                               {
	                                                   public class NestedInHidden { }
	                                               }
	                                           }
	                                           """;

	[Fact]
	public void Analyze_InternalMembersOfPublicTypes_ExcludedByDefault()
	{
		// Only private members used to be filtered, so internal members of public types
		// appeared in the public API reference.
		ApiReference result = AnalyzeSource(_accessibilitySource);

		ApiType widget = result.Namespaces[0].Types.Single(t => t.Name == "Widget");
		widget.Members.Select(m => m.Name).Should().BeEquivalentTo(
			"PublicMethod", "ProtectedMethod", "ProtectedInternalMethod");
		result.Namespaces[0].Types.Should().NotContain(t => t.Name == "NestedInHidden");
	}

	[Fact]
	public void Analyze_IncludeInternals_AddsInternalMembersAndTypes()
	{
		ApiReference result = _analyzer.AnalyzeSyntaxTrees(
			[CSharpSyntaxTree.ParseText(_accessibilitySource)], "TestAssembly", includeInternals: true);

		ApiType widget = result.Namespaces[0].Types.Single(t => t.Name == "Widget");
		widget.Members.Should().Contain(m => m.Name == "InternalMethod")
			.And.Contain(m => m.Name == "PrivateProtectedMethod");
		result.Namespaces[0].Types.Should().Contain(t => t.Name == "NestedInHidden");
	}

	[Fact]
	public void Analyze_ExceptionCrefs_KeepTheWholeTypeName()
	{
		// TrimStart('T', ':') turned "T:TimeoutError" into "imeoutError".
		ApiReference result = AnalyzeSource("""
		                                    public class TimeoutError : System.Exception { }

		                                    namespace TestNs
		                                    {
		                                        public class Client
		                                        {
		                                            /// <summary>Connects.</summary>
		                                            /// <exception cref="TimeoutError">Too slow.</exception>
		                                            /// <exception cref="TypoedMissingType">Never resolves.</exception>
		                                            public void Connect() { }
		                                        }
		                                    }
		                                    """);

		ApiMember connect = result.Namespaces.SelectMany(n => n.Types)
			.Single(t => t.Name == "Client").Members.Single(m => m.Name == "Connect");
		connect.Documentation!.Exceptions.Select(e => e.Type)
			.Should().Equal("TimeoutError", "TypoedMissingType");
	}

	[Fact]
	public void Analyze_InternalTypes_ExcludedByDefault()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        internal class InternalClass { }
		                                        public class PublicClass { }
		                                    }
		                                    """);

		result.Namespaces[0].Types.Should().HaveCount(1);
		result.Namespaces[0].Types[0].Name.Should().Be("PublicClass");
	}

	[Fact]
	public void Analyze_InternalTypes_IncludedWhenFlagged()
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText("""
		                                             namespace TestNs
		                                             {
		                                                 internal class InternalClass { }
		                                                 public class PublicClass { }
		                                             }
		                                             """, cancellationToken: TestContext.Current.CancellationToken);

		ApiReference result = _analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly", true);

		result.Namespaces[0].Types.Should().HaveCount(2);
	}

	[Fact]
	public void Analyze_Record_IdentifiedAsRecord()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A point.</summary>
		                                        public sealed record Point(double X, double Y);
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.Kind.Should().Be(ApiTypeKind.Record);
		type.IsRecord.Should().BeTrue();
		type.IsSealed.Should().BeTrue();
	}

	[Fact]
	public void Analyze_Events_Extracted()
	{
		ApiReference result = AnalyzeSource("""
		                                    using System;
		                                    namespace TestNs
		                                    {
		                                        public class Button
		                                        {
		                                            /// <summary>Fires on click.</summary>
		                                            public event EventHandler? Clicked;
		                                        }
		                                    }
		                                    """);

		List<ApiMember> members = result.Namespaces[0].Types[0].Members;
		members.Should().Contain(m => m.Name == "Clicked" && m.Kind == ApiMemberKind.Event);
	}

	[Fact]
	public void Analyze_Delegate_Extracted()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A callback.</summary>
		                                        public delegate void MyCallback(string message);
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.Kind.Should().Be(ApiTypeKind.Delegate);
		type.Name.Should().Be("MyCallback");
	}

	[Fact]
	public void Analyze_Inheritance_ExtractsBaseAndInterfaces()
	{
		ApiReference result = AnalyzeSource("""
		                                    using System;
		                                    namespace TestNs
		                                    {
		                                        public interface IFoo { }
		                                        public class Base { }
		                                        public class Child : Base, IFoo { }
		                                    }
		                                    """);

		ApiType child = result.Namespaces[0].Types.First(t => t.Name == "Child");
		child.BaseType.Should().Be("TestNs.Base");
		child.ImplementedInterfaces.Should().Contain("TestNs.IFoo");
	}

	[Fact]
	public void Analyze_InheritDocComment_RecordsTheTagEvenWhenUnresolvable()
	{
		// object.ToString is outside the model, so nothing can fill the summary in. The tag
		// is the only evidence the author documented it.
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A record.</summary>
		                                        public class Thing
		                                        {
		                                            /// <inheritdoc />
		                                            public override string ToString() => "thing";

		                                            /// <summary>Plain.</summary>
		                                            public void Plain() { }
		                                        }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types[0];
		type.Members.Single(m => m.Name == "ToString").Documentation!.HasInheritDocTag.Should().BeTrue();
		type.Members.Single(m => m.Name == "Plain").Documentation!.HasInheritDocTag.Should().BeFalse();
		type.Documentation!.HasInheritDocTag.Should().BeFalse();
	}

	[Fact]
	public void Analyze_Delegate_InvokeSharesTheDeclarationDocs()
	{
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>Raised when a value changes.</summary>
		                                        /// <param name="oldValue">The previous value.</param>
		                                        public delegate void Changed(int oldValue, int newValue);
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types.Single();
		ApiMember invoke = type.Members.Single(m => m.Name == "Invoke");

		invoke.Documentation!.Summary.Should().Contain("Raised when a value changes.");
		invoke.Documentation.Parameters.Should().ContainKey("oldValue");
	}

	[Fact]
	public void Analyze_GlobalNamespaceTypes_GroupUnderGlobalWithoutANamespace()
	{
		// Roslyn displays the global namespace as "<global namespace>". That name went into the
		// page route, and writing the page failed on Windows.
		ApiReference result = AnalyzeSource("""
		                                    /// <summary>No namespace.</summary>
		                                    public class Helper { }

		                                    public enum Mode { On, Off }

		                                    public delegate void Callback();
		                                    """);

		result.Namespaces.Should().ContainSingle().Which.Name.Should().Be("(global)");
		result.Namespaces[0].Types.Should().HaveCount(3).And.OnlyContain(t => t.Namespace == null);
	}

	[Fact]
	public void Analyze_Attributes_KeepTheirNamesAndArgumentsAsWritten()
	{
		// Replace("Attribute", "") turned AttributeUsage into "Usage", strings lost their quotes and enums became numbers.
		ApiReference result = AnalyzeSource("""
		                                    using System;
		                                    namespace TestNs
		                                    {
		                                        [AttributeUsage(AttributeTargets.Class)]
		                                        public sealed class LabelAttribute : Attribute
		                                        {
		                                            public LabelAttribute(string text, int order) { }
		                                        }

		                                        [Label("Widget", 3)]
		                                        public class Widget { }
		                                    }
		                                    """);

		List<ApiType> types = result.Namespaces[0].Types;
		ApiAttribute usage = types.Single(t => t.Name == "LabelAttribute").Attributes.Should().ContainSingle().Subject;
		usage.Name.Should().Be("AttributeUsage");
		usage.Arguments.Should().ContainSingle().Which.Should().EndWith("AttributeTargets.Class");

		ApiAttribute label = types.Single(t => t.Name == "Widget").Attributes.Should().ContainSingle().Subject;
		label.Name.Should().Be("Label");
		label.Arguments.Should().Equal("\"Widget\"", "3");
	}

	[Fact]
	public void Analyze_SeeAlsoHref_KeepsTheUrlAndText()
	{
		// The analyzer built its own list from the cref or the text, so an href entry lost its URL.
		ApiReference result = AnalyzeSource("""
		                                    namespace TestNs
		                                    {
		                                        /// <summary>A widget.</summary>
		                                        /// <seealso href="https://docs.example.com/widgets">Widget guide</seealso>
		                                        public class Widget { }
		                                    }
		                                    """);

		ApiType type = result.Namespaces[0].Types.Single();
		type.Documentation.Should().NotBeNull();
		type.Documentation!.SeeAlso.Should().Equal("[Widget guide](https://docs.example.com/widgets)");
	}
}
