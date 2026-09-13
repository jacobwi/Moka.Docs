using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

// Signatures used to be Roslyn's minimal display: no accessibility or modifiers, no "this" on
// extension methods, "Type.Member" names, bare "{ get; set; }" accessors, no const values, and
// the delegate's "signature" was just its type name.
public sealed class MemberSignatureTests
{
	private const string _source = """
	                               using System;
	                               using System.Collections.Generic;

	                               namespace TestNs
	                               {
	                                   public abstract class Shape
	                                   {
	                                       public const int MaxSides = 12;
	                                       public const string DefaultName = "shape";
	                                       public static readonly string Registry = "shapes";
	                                       protected internal int Sides;
	                                       public volatile bool Dirty;

	                                       protected Shape(string name) { }

	                                       public string Name { get; private set; } = "";
	                                       public required string Id { get; init; }
	                                       public int this[int index] { get => index; protected set { } }

	                                       public abstract double Area();
	                                       public virtual void Draw(int scale = 1, params string[] layers) { }
	                                       public static Shape operator +(Shape left, Shape right) => left;
	                                       public static explicit operator int(Shape shape) => 0;

	                                       public static event EventHandler Changed;
	                                   }

	                                   public sealed class Square : Shape
	                                   {
	                                       public Square() : base("square") { }
	                                       public sealed override double Area() => 1;
	                                       public new void Draw(int scale = 1, params string[] layers) { }
	                                   }

	                                   public static class TextExtensions
	                                   {
	                                       public static string Shout(this string value, bool loud = true) => value;
	                                       public static T FirstOrNew<T>(this IEnumerable<T> items) where T : class, new() => new T();
	                                   }

	                                   public struct Point
	                                   {
	                                       public readonly double Length() => 0;
	                                       public double X { get; set; }
	                                       public readonly int Y;
	                                       public readonly int Doubled => Y * 2;
	                                       public void Move(ref int dx, out int dy, in int dz) { dy = 0; }
	                                   }

	                                   public sealed record Person(string Name);

	                                   public interface IShape
	                                   {
	                                       double Area();
	                                       string Name { get; }
	                                       static abstract IShape Create();
	                                   }

	                                   public delegate TResult Transform<in T, out TResult>(T input);
	                               }
	                               """;

	private readonly ApiReference _api = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance)
		.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(_source)], "TestAssembly");

	private string Signature(string typeName, string memberName) =>
		_api.Namespaces.SelectMany(ns => ns.Types)
			.Single(t => t.Name == typeName)
			.Members.Single(m => m.Name == memberName)
			.Signature;

	[Theory]
	[InlineData("Shape", "Area", "public abstract double Area()")]
	[InlineData("Shape", "Draw", "public virtual void Draw(int scale = 1, params string[] layers)")]
	[InlineData("Shape", "op_Addition", "public static Shape operator +(Shape left, Shape right)")]
	[InlineData("Shape", "op_Explicit", "public static explicit operator int(Shape shape)")]
	[InlineData("Square", "Area", "public sealed override double Area()")]
	[InlineData("Square", "Draw", "public new void Draw(int scale = 1, params string[] layers)")]
	[InlineData("TextExtensions", "Shout", "public static string Shout(this string value, bool loud = true)")]
	[InlineData("TextExtensions", "FirstOrNew",
		"public static T FirstOrNew<T>(this IEnumerable<T> items) where T : class, new()")]
	[InlineData("Point", "Length", "public readonly double Length()")]
	[InlineData("Point", "Move", "public void Move(ref int dx, out int dy, in int dz)")]
	[InlineData("IShape", "Area", "double Area()")]
	[InlineData("IShape", "Create", "static abstract IShape Create()")]
	public void Signature_Method_ReadsLikeTheDeclaration(string typeName, string memberName, string expected)
	{
		Signature(typeName, memberName).Should().Be(expected);
	}

	[Theory]
	[InlineData("Shape", "Name", "public string Name { get; private set; }")]
	[InlineData("Shape", "Id", "public required string Id { get; init; }")]
	[InlineData("Shape", "this[]", "public int this[int index] { get; protected set; }")]
	[InlineData("Point", "X", "public double X { get; set; }")]
	[InlineData("Point", "Doubled", "public readonly int Doubled { get; }")]
	[InlineData("Person", "Name", "public string Name { get; init; }")]
	[InlineData("IShape", "Name", "string Name { get; }")]
	public void Signature_Property_ShowsModifiersAndAccessorAccessibility(string typeName, string memberName,
		string expected)
	{
		Signature(typeName, memberName).Should().Be(expected);
	}

	[Theory]
	[InlineData("Shape", "MaxSides", "public const int MaxSides = 12")]
	[InlineData("Shape", "DefaultName", "public const string DefaultName = \"shape\"")]
	[InlineData("Shape", "Registry", "public static readonly string Registry")]
	[InlineData("Shape", "Sides", "protected internal int Sides")]
	[InlineData("Shape", "Dirty", "public volatile bool Dirty")]
	[InlineData("Point", "Y", "public readonly int Y")]
	[InlineData("Shape", "Changed", "public static event EventHandler Changed")]
	public void Signature_FieldAndEvent_ShowsModifiersAndConstantValue(string typeName, string memberName,
		string expected)
	{
		Signature(typeName, memberName).Should().Be(expected);
	}

	[Fact]
	public void Signature_Constructor_ShowsAccessibilityAndTypeName()
	{
		Signature("Shape", "Shape").Should().Be("protected Shape(string name)");
		Signature("Square", "Square").Should().Be("public Square()");
	}

	[Fact]
	public void Signature_Delegate_ReadsLikeTheDeclaration()
	{
		Signature("Transform", "Invoke").Should().Be("public delegate TResult Transform<in T, out TResult>(T input)");
	}

	[Fact]
	public void Analyze_ParameterTypes_UseTheShortNamesOfTheSignature()
	{
		// Parameter types were fully qualified, and the page heading keeps only the text after the last dot,
		// so "System.Collections.Generic.IReadOnlyList<TestNs.Shape>" printed as "Shape>".
		ApiReference api = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance).AnalyzeSyntaxTrees(
			[CSharpSyntaxTree.ParseText("""
			                            using System.Collections.Generic;
			                            namespace TestNs
			                            {
			                                public class Shape { }
			                                public static class Canvas
			                                {
			                                    public static void Draw(IReadOnlyList<Shape> shapes, Dictionary<string, Shape> named) { }
			                                }
			                            }
			                            """)],
			"TestAssembly");

		ApiMember draw = api.Namespaces[0].Types.Single(t => t.Name == "Canvas").Members.Single(m => m.Name == "Draw");
		draw.Parameters.Select(p => p.Type).Should().Equal("IReadOnlyList<Shape>", "Dictionary<string, Shape>");
	}

	[Fact]
	public void Analyze_Struct_IsNotMarkedSealed()
	{
		// Roslyn reports every struct as sealed, so each struct page carried a Sealed badge its declaration lacks.
		_api.Namespaces[0].Types.Single(t => t.Name == "Point").IsSealed.Should().BeFalse();
		_api.Namespaces[0].Types.Single(t => t.Name == "Square").IsSealed.Should().BeTrue();
	}
}
