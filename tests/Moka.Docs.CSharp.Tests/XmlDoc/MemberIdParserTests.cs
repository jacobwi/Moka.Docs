using FluentAssertions;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

public sealed class MemberIdParserTests
{
	[Fact]
	public void Parse_TypeId_ReturnsTypeKind()
	{
		MemberIdInfo? result = MemberIdParser.Parse("T:System.String");

		result.Should().NotBeNull();
		result!.Kind.Should().Be(MemberIdKind.Type);
		result.FullName.Should().Be("System.String");
		result.Name.Should().Be("System.String");
	}

	[Fact]
	public void Parse_MethodWithParams_ExtractsParameters()
	{
		MemberIdInfo? result = MemberIdParser.Parse("M:MyApp.Calculator.Add(System.Int32,System.Int32)");

		result.Should().NotBeNull();
		result!.Kind.Should().Be(MemberIdKind.Method);
		result.ContainingType.Should().Be("MyApp.Calculator");
		result.Name.Should().Be("Add");
		result.ParameterList.Should().Be("System.Int32,System.Int32");
	}

	[Fact]
	public void Parse_Property_ReturnsPropertyKind()
	{
		MemberIdInfo? result = MemberIdParser.Parse("P:MyApp.Person.Name");

		result.Should().NotBeNull();
		result!.Kind.Should().Be(MemberIdKind.Property);
		result.Name.Should().Be("Name");
		result.ContainingType.Should().Be("MyApp.Person");
	}

	[Fact]
	public void Parse_Constructor_ExtractsCorrectly()
	{
		MemberIdInfo? result = MemberIdParser.Parse("M:MyApp.Foo.#ctor(System.String)");

		result.Should().NotBeNull();
		result!.Kind.Should().Be(MemberIdKind.Method);
		result.Name.Should().Be("#ctor");
	}

	[Fact]
	public void Parse_InvalidInput_ReturnsNull()
	{
		MemberIdParser.Parse("").Should().BeNull();
		MemberIdParser.Parse("X:Invalid").Should().BeNull();
		MemberIdParser.Parse("nocolon").Should().BeNull();
	}

	[Fact]
	public void GetDisplayName_Type_ReturnsSimpleName()
	{
		MemberIdParser.GetDisplayName("T:System.String").Should().Be("String");
		MemberIdParser.GetDisplayName("T:MyApp.Models.Person").Should().Be("Person");
	}

	[Fact]
	public void GetDisplayName_Method_ReturnsTypeAndMember()
	{
		MemberIdParser.GetDisplayName("M:MyApp.Calculator.Add(System.Int32)")
			.Should().Be("Calculator.Add");
	}

	[Theory]
	[InlineData("P:Lib.ShapeExtensions.<G>$0A8A2B8D6C6D4A6ADDD5B3E19BF9CD6D.Perimeter",
		"<G>$0A8A2B8D6C6D4A6ADDD5B3E19BF9CD6D", "P:Lib.ShapeExtensions.Perimeter")]
	[InlineData("M:Lib.ShapeExtensions.<G>$2580CBA50C52ABB91465AA64415CDF40`1.Add(`0,System.Int32)",
		"<G>$2580CBA50C52ABB91465AA64415CDF40`1", "M:Lib.ShapeExtensions.Add(`0,System.Int32)")]
	[InlineData("T:Lib.ShapeExtensions.<G>$0A8A2B8D6C6D4A6ADDD5B3E19BF9CD6D.<M>$D2B4D929343BC7E41A47F8EA77C785DD",
		"<G>$0A8A2B8D6C6D4A6ADDD5B3E19BF9CD6D", "T:Lib.ShapeExtensions")]
	public void ExtensionMemberIds_NameTheGroupingTypeAndTheStaticClass(string memberId, string grouping, string withoutIt)
	{
		// The compiler documents extension block members on a nested grouping type with an unspeakable name.
		MemberIdParser.ExtensionGroupingName(memberId).Should().Be(grouping);
		MemberIdParser.WithoutExtensionTypes(memberId).Should().Be(withoutIt);
	}

	[Theory]
	[InlineData("M:Lib.ShapeExtensions.Grow(Lib.Shape,System.Double)")]
	[InlineData("T:Lib.Outer`1.Inner")]
	[InlineData("!:extension(Shape).Grow")]
	public void WithoutExtensionTypes_LeavesOtherIdsAlone(string memberId)
	{
		MemberIdParser.ExtensionGroupingName(memberId).Should().BeNull();
		MemberIdParser.WithoutExtensionTypes(memberId).Should().Be(memberId);
	}

	[Fact]
	public void GetDisplayName_ExtensionMember_ReadsAsAMemberOfTheStaticClass()
	{
		MemberIdParser.GetDisplayName("P:Lib.ShapeExtensions.<G>$0A8A2B8D6C6D4A6ADDD5B3E19BF9CD6D.Perimeter")
			.Should().Be("ShapeExtensions.Perimeter");
	}

	[Fact]
	public void ForType_GeneratesCorrectId() => MemberIdParser.ForType("System.String").Should().Be("T:System.String");

	[Fact]
	public void ForMethod_WithParams_GeneratesCorrectId()
	{
		MemberIdParser.ForMethod("MyApp.Calc", "Add", ["System.Int32", "System.Int32"])
			.Should().Be("M:MyApp.Calc.Add(System.Int32,System.Int32)");
	}

	[Fact]
	public void ForConstructor_GeneratesCorrectId()
	{
		MemberIdParser.ForConstructor("MyApp.Foo", ["System.String"])
			.Should().Be("M:MyApp.Foo.#ctor(System.String)");
	}
}
