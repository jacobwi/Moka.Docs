using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The API model the ASP.NET Core host builds by reflection, checked against a library
///     compiled with its XML documentation file.
/// </summary>
public sealed class ReflectionApiModelBuilderTests
{
	private static readonly Lazy<ApiReference> _model = new(() =>
		new ReflectionApiModelBuilder(
				new XmlDocParser(NullLogger<XmlDocParser>.Instance),
				new InheritDocResolver(),
				NullLogger<ReflectionApiModelBuilder>.Instance)
			.Build([FixtureAssembly.Instance], true));

	private static ApiType Type(string name) =>
		_model.Value.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == name);

	private static ApiMember Member(string typeName, string memberName) =>
		Type(typeName).Members.Single(m => m.Name == memberName);

	// Written as a method so the assertion runs when there is no documentation:
	// "Documentation?.Summary.Should()" skips the whole assertion instead.
	private static string? Summary(ApiType type) => type.Documentation?.Summary;

	private static string? Summary(ApiMember member) => member.Documentation?.Summary;

	[Fact]
	public void ProtectedMembers_OfATypeThatCanBeDerivedFrom_AreDocumented()
	{
		// Only public members were read, so a base class lost its protected constructors and virtual methods.
		ApiType shape = Type("Shape");

		shape.Members.Where(m => m.Kind == ApiMemberKind.Constructor)
			.Select(m => (m.Accessibility, m.Signature, m.Documentation?.Summary))
			.Should().BeEquivalentTo([
				(ApiAccessibility.Protected, "protected Shape()", "Creates a shape."),
				(ApiAccessibility.ProtectedInternal, "protected internal Shape(string name)", "Creates a named shape.")
			]);
		Member("Shape", "OnChanged").Signature.Should().Be("protected virtual void OnChanged()");
		Member("Shape", "Revision").Accessibility.Should().Be(ApiAccessibility.ProtectedInternal);
		shape.Members.Should().NotContain(m => m.Name == "InternalHelper" || m.Name == "PrivateProtectedHelper");
	}

	[Fact]
	public void ProtectedMembers_OfASealedType_AreLeftOut()
	{
		Type("Circle").Members.Should().NotContain(m => m.Name == "OnChanged");
	}

	[Fact]
	public void ProtectedNestedType_IsDocumentedWithItsAccessibility()
	{
		ApiType cache = Type("Cache");

		cache.Accessibility.Should().Be(ApiAccessibility.Protected);
		Summary(cache).Should().Be("Cached measurements.");
	}

	[Fact]
	public void Operators_AreDocumentedWithOperatorSignatures()
	{
		// Operators have special names, and every special-name method was skipped with the accessors.
		ApiType money = Type("Money");

		money.Members.Where(m => m.Kind == ApiMemberKind.Operator)
			.Select(m => (m.Name, m.Signature, m.Documentation?.Summary))
			.Should().BeEquivalentTo([
				("op_Addition", "public static Money operator +(Money left, Money right)", "Adds two amounts."),
				("op_UnaryNegation", "public static Money operator -(Money value)", "Negates an amount."),
				("op_Implicit", "public static implicit operator decimal(Money value)", "The amount as a number."),
				("op_Explicit", "public static explicit operator Money(decimal value)", "An amount from a number.")
			]);
	}

	[Fact]
	public void Delegate_HasAnInvokeMemberWithItsParameters()
	{
		// Delegates got no members at all, so their parameters appeared nowhere.
		ApiMember invoke = Type("ChangeHandler").Members.Should().ContainSingle().Subject;

		invoke.Name.Should().Be("Invoke");
		invoke.Signature.Should().Be("public delegate bool ChangeHandler<in T>(T sender, ref int count, out string? note)");
		invoke.ReturnType.Should().Be("bool");
		invoke.Parameters.Select(p => (p.Name, p.Type, p.IsRef, p.IsOut)).Should().Equal(
			("sender", "T", false, false),
			("count", "int", true, false),
			("note", "string?", false, true));
		invoke.Documentation!.Parameters.Should().ContainKeys("sender", "count", "note");
		invoke.Documentation.Returns.Should().Be("Whether the change was handled.");
	}

	[Fact]
	public void NestedTypes_GetTheirXmlDocumentation()
	{
		// Reflection names nested types Outer+Inner and the XML file Outer.Inner, so none of them matched.
		Summary(Type("Metadata")).Should().Be("Describes a shape.");
		Summary(Member("Metadata", "Tag")).Should().Be("A free-form tag.");
		Summary(Type("Inner")).Should().Be("Lives inside the container.");
		Type("Inner").FullName.Should().Be("Fixture.Shapes.Outer.Inner");
		Summary(Member("Inner", "Run")).Should().Be("Runs the inner part.");
		Member("Inner", "Run").Signature.Should()
			.Be("public void Run(T value, Outer<int>.Inner other, CancellationToken cancellationToken = default)");
	}

	[Fact]
	public void Signatures_ReadLikeDeclarations()
	{
		// Signatures had no accessibility or modifiers, showed "{ get; set; }" for a private setter and no "this".
		Member("Shape", "Name").Signature.Should().Be("public string Name { get; private set; }");
		Member("Shape", "Id").Signature.Should().Be("public string Id { get; init; }");
		Member("Shape", "Area").Signature.Should().Be("public abstract double Area { get; }");
		Member("Shape", "MaxSides").Signature.Should().Be("public const int MaxSides = 12");
		Member("Shape", "Unit").Signature.Should().Be("public static readonly string Unit");
		Member("Shape", "Changed").Signature.Should().Be("public event EventHandler? Changed");
		Member("Shape", "Scale").Signature.Should().Be("public abstract Shape Scale(double factor)");
		Member("Square", "Scale").Signature.Should().Be("public sealed override Shape Scale(double factor)");
		Member("Circle", "Area").Signature.Should().Be("public override double Area { get; }");
		Member("ShapeExtensions", "Describe").Signature.Should()
			.Be("public static string Describe(this Shape shape, string? prefix = null, params int[] values)");

		// Interface members are public and abstract or virtual without saying so, unless they are static.
		Member("IResizable", "Width").Signature.Should().Be("double Width { get; }");
		Member("IResizable", "Create").Signature.Should().Be("static abstract IResizable Create(double width)");
		Member("IResizable", "Reset").Signature.Should().Be("void Reset()");
	}

	[Fact]
	public void CompilerGeneratedRecordMembers_AreLeftOut()
	{
		ApiType point = Type("Point");

		point.Members.Select(m => m.Name).Should().BeEquivalentTo("Point", "X", "Y");
		point.Members.Single(m => m.Kind == ApiMemberKind.Constructor).Signature.Should().Be("public Point(int X, int Y)");
	}

	[Fact]
	public void Struct_IsNotMarkedSealed()
	{
		// Every struct is sealed in metadata, so struct pages got a Sealed badge their declaration never had.
		Type("Money").IsSealed.Should().BeFalse();
	}
}
