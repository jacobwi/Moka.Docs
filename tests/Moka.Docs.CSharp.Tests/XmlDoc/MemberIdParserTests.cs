using FluentAssertions;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

public sealed class MemberIdParserTests
{
    [Fact]
    public void Parse_TypeId_ReturnsTypeKind()
    {
        var result = MemberIdParser.Parse("T:System.String");

        result.Should().NotBeNull();
        result!.Kind.Should().Be(MemberIdKind.Type);
        result.FullName.Should().Be("System.String");
        result.Name.Should().Be("System.String");
    }

    [Fact]
    public void Parse_MethodWithParams_ExtractsParameters()
    {
        var result = MemberIdParser.Parse("M:MyApp.Calculator.Add(System.Int32,System.Int32)");

        result.Should().NotBeNull();
        result!.Kind.Should().Be(MemberIdKind.Method);
        result.ContainingType.Should().Be("MyApp.Calculator");
        result.Name.Should().Be("Add");
        result.ParameterList.Should().Be("System.Int32,System.Int32");
    }

    [Fact]
    public void Parse_Property_ReturnsPropertyKind()
    {
        var result = MemberIdParser.Parse("P:MyApp.Person.Name");

        result.Should().NotBeNull();
        result!.Kind.Should().Be(MemberIdKind.Property);
        result.Name.Should().Be("Name");
        result.ContainingType.Should().Be("MyApp.Person");
    }

    [Fact]
    public void Parse_Constructor_ExtractsCorrectly()
    {
        var result = MemberIdParser.Parse("M:MyApp.Foo.#ctor(System.String)");

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

    [Fact]
    public void ForType_GeneratesCorrectId()
    {
        MemberIdParser.ForType("System.String").Should().Be("T:System.String");
    }

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