using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.Metadata;

/// <summary>
///     C# 14 extension blocks in the Roslyn source analysis: their members belong to the static class that
///     declares the blocks, the way the source reads.
/// </summary>
public sealed class ExtensionBlockTests
{
	private const string _source = """
	                               using System;
	                               using System.Collections.Generic;

	                               namespace Lib
	                               {
	                                   /// <summary>A shape.</summary>
	                                   public class Shape
	                                   {
	                                       /// <summary>The width.</summary>
	                                       public double Width { get; set; }

	                                       /// <inheritdoc cref="ShapeExtensions.extension(Shape).Grow(double)" />
	                                       public Shape Scale(double factor) => this;
	                                   }

	                                   /// <summary>A point.</summary>
	                                   public struct Point
	                                   {
	                                       /// <summary>The x coordinate.</summary>
	                                       public int X;
	                                   }

	                                   /// <summary>Shape helpers.</summary>
	                                   public static class ShapeExtensions
	                                   {
	                                       /// <summary>Describes a shape.</summary>
	                                       public static string Describe(this Shape shape) => "";

	                                       /// <summary>Members for any shape.</summary>
	                                       /// <param name="shape">The shape to extend.</param>
	                                       extension(Shape shape)
	                                       {
	                                           /// <summary>The perimeter.</summary>
	                                           public double Perimeter => shape.Width * 4;

	                                           /// <summary>A label.</summary>
	                                           public string Label { get => ""; set { } }

	                                           /// <summary>Grows the shape.</summary>
	                                           /// <param name="factor">The factor.</param>
	                                           /// <returns>The grown shape.</returns>
	                                           public Shape Grow(double factor) => shape;

	                                           /// <summary>A unit shape.</summary>
	                                           public static Shape Unit() => new();

	                                           /// <summary>The default shape.</summary>
	                                           public static Shape Default => new();

	                                           /// <summary>Combines two shapes.</summary>
	                                           public static Shape operator +(Shape left, Shape right) => left;

	                                           internal double Hidden => 0;
	                                       }

	                                       /// <summary>Members for lists of comparable items.</summary>
	                                       /// <typeparam name="T">The item type.</typeparam>
	                                       /// <param name="list">The list.</param>
	                                       extension<T>(List<T> list) where T : IComparable<T>
	                                       {
	                                           /// <summary>The largest item.</summary>
	                                           public T Largest() => list[0];

	                                           /// <summary>Converts the items.</summary>
	                                           /// <typeparam name="TOut">The result type.</typeparam>
	                                           public List<TOut> Convert<TOut>(Func<T, TOut> convert) => new();
	                                       }

	                                       /// <param name="point">The point to move.</param>
	                                       extension(ref Point point)
	                                       {
	                                           /// <summary>Moves the point.</summary>
	                                           public void Move(int dx) => point.X += dx;
	                                       }

	                                       extension(in Point point)
	                                       {
	                                           /// <summary>The distance from the origin.</summary>
	                                           public int Distance => point.X;
	                                       }

	                                       extension(Shape)
	                                       {
	                                           /// <summary>Creates a shape.</summary>
	                                           public static Shape Create(int sides) => new();
	                                       }
	                                   }
	                               }
	                               """;

	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);

	private ApiReference Analyze(bool includeInternals = false) =>
		_analyzer.AnalyzeSyntaxTrees(
			[CSharpSyntaxTree.ParseText(_source, cancellationToken: TestContext.Current.CancellationToken)],
			"TestAssembly", includeInternals);

	private ApiType Extensions(bool includeInternals = false) =>
		Analyze(includeInternals).Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == "ShapeExtensions");

	private ApiMember Member(string name) => Extensions().Members.Single(m => m.Name == name);

	[Fact]
	public void ExtensionBlocks_AreDocumentedOnTheStaticClass_WithoutMarkerTypesOrImplementationMethods()
	{
		ApiReference result = Analyze();

		result.Namespaces.SelectMany(ns => ns.Types).Select(t => t.Name)
			.Should().BeEquivalentTo("Shape", "Point", "ShapeExtensions");
		Extensions().Members.Select(m => m.Name).Should().BeEquivalentTo(
			"Describe", "Perimeter", "Label", "Grow", "Unit", "Default", "op_Addition",
			"Largest", "Convert", "Move", "Distance", "Create");
	}

	[Fact]
	public void ExtensionBlockMembers_KeepTheirKindAndWhetherTheyAreStatic()
	{
		ApiType extensions = Extensions();

		extensions.Members.Where(m => m.Extension is not null)
			.Select(m => (m.Name, m.Kind, m.IsStatic, m.IsExtensionMethod))
			.Should().BeEquivalentTo([
				("Perimeter", ApiMemberKind.Property, false, false),
				("Label", ApiMemberKind.Property, false, false),
				("Grow", ApiMemberKind.Method, false, false),
				("Unit", ApiMemberKind.Method, true, false),
				("Default", ApiMemberKind.Property, true, false),
				("op_Addition", ApiMemberKind.Operator, true, false),
				("Largest", ApiMemberKind.Method, false, false),
				("Convert", ApiMemberKind.Method, false, false),
				("Move", ApiMemberKind.Method, false, false),
				("Distance", ApiMemberKind.Property, false, false),
				("Create", ApiMemberKind.Method, true, false)
			]);
	}

	[Fact]
	public void ClassicExtensionMethod_IsUnchanged()
	{
		ApiMember describe = Member("Describe");

		describe.Extension.Should().BeNull();
		describe.IsExtensionMethod.Should().BeTrue();
		describe.Signature.Should().Be("public static string Describe(this Shape shape)");
		describe.Parameters.Select(p => p.Name).Should().Equal("shape");
	}

	[Fact]
	public void ExtensionBlockMembers_HaveSignaturesThatReadLikeTheSource()
	{
		Member("Perimeter").Extension!.Signature.Should().Be("extension(Shape shape)");
		Member("Perimeter").Signature.Should().Be("public double Perimeter { get; }");
		Member("Label").Signature.Should().Be("public string Label { get; set; }");
		Member("Grow").Signature.Should().Be("public Shape Grow(double factor)");
		Member("Unit").Signature.Should().Be("public static Shape Unit()");
		Member("Default").Signature.Should().Be("public static Shape Default { get; }");
		Member("op_Addition").Signature.Should().Be("public static Shape operator +(Shape left, Shape right)");
		Member("Create").Extension!.Signature.Should().Be("extension(Shape)");
	}

	[Fact]
	public void InstanceMembers_HaveTheirOwnParametersAndShareTheReceiver()
	{
		ApiMember grow = Member("Grow");

		grow.Parameters.Select(p => (p.Name, p.Type)).Should().Equal(("factor", "double"));
		grow.Extension!.Receiver.Name.Should().Be("shape");
		grow.Extension.Receiver.Type.Should().Be("Shape");
		Member("Perimeter").Extension!.Receiver.Name.Should().Be("shape");

		// A block with only static members can leave the receiver's name out.
		Member("Create").Extension!.Receiver.Name.Should().BeEmpty();
		Member("Create").Extension!.Receiver.Type.Should().Be("Shape");
	}

	[Fact]
	public void RefAndInReceivers_OnAStruct_KeepTheirModifiers()
	{
		ApiExtensionBlock byRef = Member("Move").Extension!;
		byRef.Signature.Should().Be("extension(ref Point point)");
		byRef.Receiver.Should().BeEquivalentTo(new { Name = "point", Type = "Point", IsRef = true, IsIn = false });

		ApiExtensionBlock byIn = Member("Distance").Extension!;
		byIn.Signature.Should().Be("extension(in Point point)");
		byIn.Receiver.Should().BeEquivalentTo(new { Name = "point", Type = "Point", IsRef = false, IsIn = true });
	}

	[Fact]
	public void GenericReceiver_KeepsTheBlocksTypeParametersAndConstraints()
	{
		ApiMember largest = Member("Largest");
		ApiMember convert = Member("Convert");

		largest.Extension!.Signature.Should().Be("extension<T>(List<T> list) where T : IComparable<T>");
		largest.Extension.TypeParameters.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new { Name = "T", Constraints = new[] { "System.IComparable<T>" } });
		largest.Extension.Receiver.Type.Should().Be("List<T>");
		largest.TypeParameters.Should().BeEmpty();
		largest.ReturnType.Should().Be("T");

		// The member's own type parameters stay on the member.
		convert.TypeParameters.Select(tp => tp.Name).Should().Equal("TOut");
		convert.Signature.Should().Be("public List<TOut> Convert<TOut>(Func<T, TOut> convert)");
	}

	[Fact]
	public void XmlDocs_AttachToTheMembersAndTheBlock()
	{
		ApiMember grow = Member("Grow");

		grow.Documentation!.Summary.Should().Be("Grows the shape.");
		grow.Documentation.Parameters.Should().ContainKey("factor");
		grow.Documentation.Returns.Should().Be("The grown shape.");
		Member("Perimeter").Documentation!.Summary.Should().Be("The perimeter.");

		ApiExtensionBlock block = grow.Extension!;
		block.Documentation!.Summary.Should().Be("Members for any shape.");
		block.Documentation.Parameters.Should().Equal(new Dictionary<string, string> { ["shape"] = "The shape to extend." });

		XmlDocBlock generic = Member("Largest").Extension!.Documentation!;
		generic.TypeParameters.Should().Equal(new Dictionary<string, string> { ["T"] = "The item type." });
		generic.Parameters.Should().Equal(new Dictionary<string, string> { ["list"] = "The list." });

		Member("Move").Extension!.Documentation!.Parameters.Should().ContainKey("point");
		Member("Distance").Extension!.Documentation.Should().BeNull();
	}

	[Fact]
	public void Block_KeepsTheDocumentationIdTheCompilerWrites()
	{
		string shape = Member("Grow").Extension!.DocumentationId!;
		shape.Should().MatchRegex(@"^T:Lib\.ShapeExtensions\.<G>\$[0-9A-F]+\.<M>\$[0-9A-F]+$");

		// Blocks for one receiver type share the grouping type their members are documented on.
		string Grouping(string member) => Member(member).Extension!.DocumentationId!.Split('.')[2];
		Grouping("Create").Should().Be(Grouping("Grow"));
		Grouping("Move").Should().NotBe(Grouping("Grow"));
		Grouping("Largest").Should().EndWith("`1");
	}

	[Fact]
	public void InheritDocCref_PicksTheBlockTheGroupingTypeNames()
	{
		// Three methods called Resize with one parameter: the cref's grouping type is what tells them apart.
		SyntaxTree tree = CSharpSyntaxTree.ParseText("""
		                                             namespace Lib
		                                             {
		                                                 public class Shape { }
		                                                 public struct Point { }

		                                                 public static class ShapeExtensions
		                                                 {
		                                                     /// <summary>Not an extension.</summary>
		                                                     public static int Resize(int value) => value;

		                                                     extension(Point point)
		                                                     {
		                                                         /// <summary>Resizes a point.</summary>
		                                                         public void Resize(double factor) { }
		                                                     }

		                                                     extension(Shape shape)
		                                                     {
		                                                         /// <summary>Resizes a shape.</summary>
		                                                         public void Resize(double factor) { }
		                                                     }
		                                                 }

		                                                 public class Box
		                                                 {
		                                                     /// <inheritdoc cref="ShapeExtensions.extension(Shape).Resize(double)" />
		                                                     public void Resize(double factor) { }
		                                                 }
		                                             }
		                                             """, cancellationToken: TestContext.Current.CancellationToken);

		ApiReference resolved = new InheritDocResolver().Resolve(_analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly"));

		resolved.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == "Box").Members.Single(m => m.Name == "Resize")
			.Documentation!.Summary.Should().Be("Resizes a shape.");
	}

	[Fact]
	public void InternalBlockMembers_FollowIncludeInternals()
	{
		Extensions().Members.Should().NotContain(m => m.Name == "Hidden");
		Extensions(includeInternals: true).Members.Should()
			.Contain(m => m.Name == "Hidden" && m.Accessibility == ApiAccessibility.Internal);
	}

	[Fact]
	public void ViewSource_ShowsTheBlocksWithoutTheirHiddenMembers()
	{
		string source = Extensions().SourceCode!;

		source.Should().Contain("extension(Shape shape)")
			.And.Contain("public double Perimeter => shape.Width * 4;")
			.And.Contain("extension<T>(List<T> list)")
			.And.NotContain("Hidden");
	}

	[Fact]
	public void InheritDocCref_ToAnExtensionMember_TakesItsDocumentation()
	{
		ApiReference resolved = new InheritDocResolver().Resolve(Analyze());

		ApiMember scale = resolved.Namespaces.SelectMany(ns => ns.Types)
			.Single(t => t.Name == "Shape").Members.Single(m => m.Name == "Scale");
		scale.Documentation!.Summary.Should().Be("Grows the shape.");
		scale.Documentation.Returns.Should().Be("The grown shape.");
	}
}
