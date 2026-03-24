using FluentAssertions;
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
        var tree = CSharpSyntaxTree.ParseText(source);
        return _analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly");
    }

    [Fact]
    public void Analyze_SimpleClass_ExtractsType()
    {
        var result = AnalyzeSource("""
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

        var type = result.Namespaces[0].Types[0];
        type.Name.Should().Be("MyClass");
        type.Kind.Should().Be(ApiTypeKind.Class);
        type.Documentation?.Summary.Should().Be("A test class.");
    }

    [Fact]
    public void Analyze_Methods_ExtractsParameters()
    {
        var result = AnalyzeSource("""
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

        var method = result.Namespaces[0].Types[0].Members
            .First(m => m.Name == "Add");

        method.Kind.Should().Be(ApiMemberKind.Method);
        method.Parameters.Should().HaveCount(2);
        method.Parameters[0].Name.Should().Be("a");
        method.Parameters[0].Type.Should().Be("int");
        method.ReturnType.Should().Be("int");
        method.Documentation?.Summary.Should().Be("Adds values.");
        method.Documentation?.Parameters.Should().ContainKey("a");
    }

    [Fact]
    public void Analyze_Interface_ExtractsCorrectly()
    {
        var result = AnalyzeSource("""
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

        var type = result.Namespaces[0].Types[0];
        type.Kind.Should().Be(ApiTypeKind.Interface);
        type.Members.Should().Contain(m => m.Name == "Area" && m.Kind == ApiMemberKind.Property);
        type.Members.Should().Contain(m => m.Name == "Draw" && m.Kind == ApiMemberKind.Method);
    }

    [Fact]
    public void Analyze_Enum_ExtractsMembers()
    {
        var result = AnalyzeSource("""
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

        var type = result.Namespaces[0].Types[0];
        type.Kind.Should().Be(ApiTypeKind.Enum);
        type.Members.Should().HaveCount(3);
        type.Members[0].Name.Should().Be("Red");
    }

    [Fact]
    public void Analyze_GenericClass_ExtractsTypeParams()
    {
        var result = AnalyzeSource("""
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

        var type = result.Namespaces[0].Types[0];
        type.TypeParameters.Should().HaveCount(1);
        type.TypeParameters[0].Name.Should().Be("T");
        type.TypeParameters[0].Constraints.Should().Contain("class");
        type.TypeParameters[0].Constraints.Should().Contain("new()");
    }

    [Fact]
    public void Analyze_Properties_ExtractsGetSet()
    {
        var result = AnalyzeSource("""
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

        var members = result.Namespaces[0].Types[0].Members;
        members.Should().Contain(m => m.Name == "Name" && m.Kind == ApiMemberKind.Property);
        members.Should().Contain(m => m.Name == "Age" && m.Kind == ApiMemberKind.Property);
    }

    [Fact]
    public void Analyze_StaticClass_MarksStatic()
    {
        var result = AnalyzeSource("""
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

        var type = result.Namespaces[0].Types[0];
        type.IsStatic.Should().BeTrue();

        var method = type.Members[0];
        method.IsStatic.Should().BeTrue();
        method.IsExtensionMethod.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ObsoleteType_MarksFlagAndMessage()
    {
        var result = AnalyzeSource("""
                                   using System;
                                   namespace TestNs
                                   {
                                       /// <summary>Old class.</summary>
                                       [Obsolete("Use NewClass instead.")]
                                       public class OldClass { }
                                   }
                                   """);

        var type = result.Namespaces[0].Types[0];
        type.IsObsolete.Should().BeTrue();
        type.ObsoleteMessage.Should().Be("Use NewClass instead.");
    }

    [Fact]
    public void Analyze_PrivateMembers_Excluded()
    {
        var result = AnalyzeSource("""
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

        var members = result.Namespaces[0].Types[0].Members;
        members.Should().NotContain(m => m.Name == "_secret");
        members.Should().NotContain(m => m.Name == "Hidden");
        members.Should().Contain(m => m.Name == "Visible");
    }

    [Fact]
    public void Analyze_InternalTypes_ExcludedByDefault()
    {
        var result = AnalyzeSource("""
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
        var tree = CSharpSyntaxTree.ParseText("""
                                              namespace TestNs
                                              {
                                                  internal class InternalClass { }
                                                  public class PublicClass { }
                                              }
                                              """, cancellationToken: TestContext.Current.CancellationToken);

        var result = _analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly", true);

        result.Namespaces[0].Types.Should().HaveCount(2);
    }

    [Fact]
    public void Analyze_Record_IdentifiedAsRecord()
    {
        var result = AnalyzeSource("""
                                   namespace TestNs
                                   {
                                       /// <summary>A point.</summary>
                                       public sealed record Point(double X, double Y);
                                   }
                                   """);

        var type = result.Namespaces[0].Types[0];
        type.Kind.Should().Be(ApiTypeKind.Record);
        type.IsRecord.Should().BeTrue();
        type.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Analyze_Events_Extracted()
    {
        var result = AnalyzeSource("""
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

        var members = result.Namespaces[0].Types[0].Members;
        members.Should().Contain(m => m.Name == "Clicked" && m.Kind == ApiMemberKind.Event);
    }

    [Fact]
    public void Analyze_Delegate_Extracted()
    {
        var result = AnalyzeSource("""
                                   namespace TestNs
                                   {
                                       /// <summary>A callback.</summary>
                                       public delegate void MyCallback(string message);
                                   }
                                   """);

        var type = result.Namespaces[0].Types[0];
        type.Kind.Should().Be(ApiTypeKind.Delegate);
        type.Name.Should().Be("MyCallback");
    }

    [Fact]
    public void Analyze_Inheritance_ExtractsBaseAndInterfaces()
    {
        var result = AnalyzeSource("""
                                   using System;
                                   namespace TestNs
                                   {
                                       public interface IFoo { }
                                       public class Base { }
                                       public class Child : Base, IFoo { }
                                   }
                                   """);

        var child = result.Namespaces[0].Types.First(t => t.Name == "Child");
        child.BaseType.Should().Be("TestNs.Base");
        child.ImplementedInterfaces.Should().Contain("TestNs.IFoo");
    }
}