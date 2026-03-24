using FluentAssertions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

public sealed class InheritDocResolverTests
{
    private readonly InheritDocResolver _resolver = new();

    [Fact]
    public void Resolve_MemberInheritsFromInterface_GetsDoc()
    {
        var reference = new ApiReference
        {
            Namespaces =
            [
                new ApiNamespace
                {
                    Name = "TestNs",
                    Types =
                    [
                        new ApiType
                        {
                            Name = "IFoo",
                            FullName = "TestNs.IFoo",
                            Kind = ApiTypeKind.Interface,
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "DoWork",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void DoWork()",
                                    Documentation = new XmlDocBlock { Summary = "Does the work." }
                                }
                            ]
                        },
                        new ApiType
                        {
                            Name = "Foo",
                            FullName = "TestNs.Foo",
                            Kind = ApiTypeKind.Class,
                            ImplementedInterfaces = ["TestNs.IFoo"],
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "DoWork",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void DoWork()",
                                    Documentation = null // No doc — should inherit
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var resolved = _resolver.Resolve(reference);

        var fooMethod = resolved.Namespaces[0].Types[1].Members[0];
        fooMethod.Documentation.Should().NotBeNull();
        fooMethod.Documentation!.Summary.Should().Be("Does the work.");
        fooMethod.Documentation.IsInherited.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MemberInheritsFromBase_GetsDoc()
    {
        var reference = new ApiReference
        {
            Namespaces =
            [
                new ApiNamespace
                {
                    Name = "TestNs",
                    Types =
                    [
                        new ApiType
                        {
                            Name = "Base",
                            FullName = "TestNs.Base",
                            Kind = ApiTypeKind.Class,
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "Render",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void Render()",
                                    Documentation = new XmlDocBlock { Summary = "Renders output." }
                                }
                            ]
                        },
                        new ApiType
                        {
                            Name = "Child",
                            FullName = "TestNs.Child",
                            Kind = ApiTypeKind.Class,
                            BaseType = "TestNs.Base",
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "Render",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void Render()",
                                    Documentation = null
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var resolved = _resolver.Resolve(reference);

        var childMethod = resolved.Namespaces[0].Types[1].Members[0];
        childMethod.Documentation!.Summary.Should().Be("Renders output.");
        childMethod.Documentation.IsInherited.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MemberWithOwnDoc_NotOverridden()
    {
        var reference = new ApiReference
        {
            Namespaces =
            [
                new ApiNamespace
                {
                    Name = "TestNs",
                    Types =
                    [
                        new ApiType
                        {
                            Name = "IFoo",
                            FullName = "TestNs.IFoo",
                            Kind = ApiTypeKind.Interface,
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "DoWork",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void DoWork()",
                                    Documentation = new XmlDocBlock { Summary = "Interface doc." }
                                }
                            ]
                        },
                        new ApiType
                        {
                            Name = "Foo",
                            FullName = "TestNs.Foo",
                            Kind = ApiTypeKind.Class,
                            ImplementedInterfaces = ["TestNs.IFoo"],
                            Members =
                            [
                                new ApiMember
                                {
                                    Name = "DoWork",
                                    Kind = ApiMemberKind.Method,
                                    Signature = "void DoWork()",
                                    Documentation = new XmlDocBlock { Summary = "Own doc." }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var resolved = _resolver.Resolve(reference);

        var fooMethod = resolved.Namespaces[0].Types[1].Members[0];
        fooMethod.Documentation!.Summary.Should().Be("Own doc.");
        fooMethod.Documentation.IsInherited.Should().BeFalse();
    }
}