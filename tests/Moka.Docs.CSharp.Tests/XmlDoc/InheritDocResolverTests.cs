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
									Documentation = null // No doc - should inherit
								}
							]
						}
					]
				}
			]
		};

		ApiReference resolved = _resolver.Resolve(reference);

		ApiMember fooMethod = resolved.Namespaces[0].Types[1].Members[0];
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

		ApiReference resolved = _resolver.Resolve(reference);

		ApiMember childMethod = resolved.Namespaces[0].Types[1].Members[0];
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

		ApiReference resolved = _resolver.Resolve(reference);

		ApiMember fooMethod = resolved.Namespaces[0].Types[1].Members[0];
		fooMethod.Documentation!.Summary.Should().Be("Own doc.");
		fooMethod.Documentation.IsInherited.Should().BeFalse();
	}

	#region Cross-project references

	private static ApiType Interface(string ns, string name, string memberDoc) => new()
	{
		Name = name,
		FullName = $"{ns}.{name}",
		Kind = ApiTypeKind.Interface,
		Members =
		[
			new ApiMember
			{
				Name = "Execute",
				Kind = ApiMemberKind.Method,
				Signature = "Task Execute()",
				Documentation = new XmlDocBlock { Summary = memberDoc }
			}
		]
	};

	private static ApiType Implementation(string baseType) => new()
	{
		Name = "Phase",
		FullName = "Engine.Phase",
		Kind = ApiTypeKind.Class,
		BaseType = baseType,
		Members =
		[
			new ApiMember
			{
				Name = "Execute",
				Kind = ApiMemberKind.Method,
				Signature = "Task Execute()",
				Documentation = new XmlDocBlock { HasInheritDocTag = true }
			}
		]
	};

	private static ApiReference Model(params ApiType[] types) => new()
	{
		Namespaces = types.GroupBy(t => t.FullName[..t.FullName.LastIndexOf('.')])
			.Select(g => new ApiNamespace { Name = g.Key, Types = g.ToList() })
			.ToList()
	};

	[Fact]
	public void Resolve_UnqualifiedBaseFromAnotherProject_InheritsMemberDoc()
	{
		// Projects are compiled separately, so an interface defined in another project is
		// unresolved: Roslyn reports it unqualified and as the base type, not an interface.
		// This is exactly how DiscoveryPhase : IBuildPhase arrived, and why every build
		// phase's API page had no description for Name, Order or ExecuteAsync.
		ApiReference resolved = _resolver.Resolve(Model(
			Interface("Core.Pipeline", "IBuildPhase", "Execute this build phase."),
			Implementation("IBuildPhase")));

		ApiMember member = resolved.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Phase").Members[0];
		member.Documentation!.Summary.Should().Be("Execute this build phase.");
		member.Documentation.IsInherited.Should().BeTrue();
		member.Documentation.HasInheritDocTag.Should().BeTrue();
	}

	[Fact]
	public void Resolve_UnqualifiedReferenceMatchingTwoTypes_DoesNotGuess()
	{
		ApiReference resolved = _resolver.Resolve(Model(
			Interface("Alpha", "IBuildPhase", "From Alpha."),
			Interface("Beta", "IBuildPhase", "From Beta."),
			Implementation("IBuildPhase")));

		ApiMember member = resolved.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Phase").Members[0];
		member.Documentation!.Summary.Should().BeEmpty();
		member.Documentation.IsInherited.Should().BeFalse();
	}

	[Fact]
	public void Resolve_QualifiedReferenceNotInModel_DoesNotFallBackToSimpleName()
	{
		// An unfound System.Exception must not attach to a user type named Exception.
		ApiType userException = Interface("MyLib", "Exception", "Not the framework type.");

		ApiReference resolved = _resolver.Resolve(Model(userException, Implementation("System.Exception")));

		ApiMember member = resolved.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Phase").Members[0];
		member.Documentation!.Summary.Should().BeEmpty();
	}

	#endregion

	#region Reflection host names

	private static ApiType Generic(string fullName, string name, string memberDoc, params string[] typeParameters) => new()
	{
		Name = name,
		FullName = fullName,
		Namespace = "Lib",
		Kind = ApiTypeKind.Class,
		TypeParameters = typeParameters.Select(tp => new ApiTypeParameter { Name = tp }).ToList(),
		Members =
		[
			new ApiMember
			{
				Name = "Execute",
				Kind = ApiMemberKind.Method,
				Signature = "Task Execute()",
				Documentation = new XmlDocBlock { Summary = memberDoc }
			}
		]
	};

	private static string ResolvedSummary(ApiReference model) =>
		model.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Phase").Members[0].Documentation!.Summary;

	[Theory]
	[InlineData("Lib.Base<T>", "Lib.Base<int>")] // the CLI
	[InlineData("Lib.Base", "Lib.Base<int>")] // the reflection host leaves type parameters out of the full name
	public void Resolve_QualifiedGenericBase_InheritsMemberDoc(string baseFullName, string reference)
	{
		ApiReference resolved = _resolver.Resolve(Model(
			Generic(baseFullName, "Base", "Runs the base.", "T"),
			Implementation(reference) with { FullName = "Lib.Phase", Namespace = "Lib" }));

		ResolvedSummary(resolved).Should().Be("Runs the base.");
	}

	[Theory]
	[InlineData("Lib.Outer<T>", "Lib.Outer<T>.Inner", new string[0])] // the CLI
	[InlineData("Lib.Outer", "Lib.Outer.Inner", new[] { "T" })] // the host lists the containing type's T on Inner too
	public void Resolve_NestedTypeOfAGenericType_InheritsMemberDoc(string outerFullName, string innerFullName,
		string[] innerTypeParameters)
	{
		ApiReference resolved = _resolver.Resolve(Model(
			Generic(outerFullName, "Outer", "Runs the outer type.", "T"),
			Generic(innerFullName, "Inner", "Runs the inner type.", innerTypeParameters),
			Implementation("Lib.Outer<int>.Inner") with { FullName = "Lib.Phase", Namespace = "Lib" }));

		ResolvedSummary(resolved).Should().Be("Runs the inner type.");
	}

	[Fact]
	public void Resolve_InheritDocCrefToAHostGenericType_FindsIt()
	{
		ApiType phase = Implementation("Lib.Other") with { FullName = "Lib.Phase", Namespace = "Lib" };
		phase = phase with
		{
			Members = [phase.Members[0] with { Documentation = new XmlDocBlock { HasInheritDocTag = true, InheritDocCref = "M:Lib.Base`1.Execute" } }]
		};

		ApiReference resolved = _resolver.Resolve(Model(Generic("Lib.Base", "Base", "Runs the base.", "T"), phase));

		ResolvedSummary(resolved).Should().Be("Runs the base.");
	}

	#endregion
}
