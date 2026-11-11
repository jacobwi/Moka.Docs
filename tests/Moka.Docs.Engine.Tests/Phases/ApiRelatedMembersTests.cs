using FluentAssertions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     The Inherited Members and Extension Methods sections of a type page, rendered from hand-built
///     models written the way each host writes type names: the CLI qualifies resolved types and leaves
///     types from other projects unqualified, the reflection host never qualifies.
/// </summary>
public sealed class ApiRelatedMembersTests
{
	#region Model helpers

	private static ApiType Type(string fullName, ApiTypeKind kind = ApiTypeKind.Class, string? baseType = null,
		string[]? interfaces = null, string[]? typeParameters = null, params ApiMember[] members)
	{
		string withoutArguments = fullName.Split('<')[0];
		int dot = withoutArguments.LastIndexOf('.');
		return new ApiType
		{
			Name = withoutArguments[(dot + 1)..],
			FullName = fullName,
			Namespace = dot > 0 ? withoutArguments[..dot] : null,
			Kind = kind,
			BaseType = baseType,
			ImplementedInterfaces = [..interfaces ?? []],
			TypeParameters = [..(typeParameters ?? []).Select(name => new ApiTypeParameter { Name = name })],
			Members = [..members]
		};
	}

	private static ApiMember Method(string name, params string[] parameterTypes) => new()
	{
		Name = name,
		Kind = ApiMemberKind.Method,
		Signature = $"public void {name}()",
		Parameters = [..parameterTypes.Select((type, i) => new ApiParameter { Name = $"p{i}", Type = type })],
		Documentation = new XmlDocBlock { Summary = $"{name} summary." }
	};

	private static ApiMember Extension(string name, string receiver, params string[] typeParameters) =>
		Method(name, receiver) with
		{
			IsStatic = true,
			IsExtensionMethod = true,
			TypeParameters = [..typeParameters.Select(tp => ParseTypeParameter(tp))]
		};

	// "T : Lib.Shape, class" is a type parameter T with two constraints.
	private static ApiTypeParameter ParseTypeParameter(string text)
	{
		string[] parts = text.Split(':', 2);
		return new ApiTypeParameter
		{
			Name = parts[0].Trim(),
			Constraints = parts.Length > 1 ? [..parts[1].Split(',').Select(c => c.Trim())] : []
		};
	}

	private static ApiMember Property(string name) => new()
	{
		Name = name,
		Kind = ApiMemberKind.Property,
		Signature = $"public int {name} {{ get; }}",
		Documentation = new XmlDocBlock { Summary = $"{name} summary." }
	};

	private static ApiMember Constructor(string typeName) => new()
	{
		Name = typeName,
		Kind = ApiMemberKind.Constructor,
		Signature = $"public {typeName}()",
		Documentation = new XmlDocBlock { Summary = "Creates one." }
	};

	private static ApiType StaticClass(string fullName, params ApiMember[] members) =>
		Type(fullName, members: members) with { IsStatic = true, IsAbstract = true, IsSealed = true };

	private static string Render(List<ApiType> types, string fullName) =>
		ApiPageRenderer.RenderType(types.Single(t => t.FullName == fullName), types);

	/// <summary>
	///     The HTML of one section, from its heading to the end of its table.
	/// </summary>
	private static string Section(string html, string id)
	{
		int start = html.IndexOf($"<h2 id=\"{id}\">", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the page should have a section with id {id}");
		int end = html.IndexOf("</table>", start, StringComparison.Ordinal);
		return html[start..end];
	}

	private static string Inherited(List<ApiType> types, string fullName) =>
		Section(Render(types, fullName), "inherited-members");

	private static string Extensions(List<ApiType> types, string fullName) =>
		Section(Render(types, fullName), "extension-methods");

	private static string Link(string href, string text) =>
		$"<a href=\"{href}\" class=\"api-member-link\"><code>{text}</code></a>";

	#endregion

	#region Inherited members

	private static List<ApiType> ShapeChain() =>
	[
		Type("Lib.Shapes.Shape", members:
		[
			Constructor("Shape"), Property("Name"), Method("Area"), Method("Scale", "double"), Method("Describe")
		]),
		Type("Lib.Shapes.Ellipse", baseType: "Lib.Shapes.Shape", members:
		[
			Constructor("Ellipse"), Property("Radius"), Method("Area") with { IsOverride = true }
		]),
		Type("Lib.Shapes.Circle", baseType: "Lib.Shapes.Ellipse", members:
		[
			Constructor("Circle"), Property("Diameter"), Method("Scale", "double") with { IsOverride = true }
		])
	];

	[Fact]
	public void InheritedMembers_ThreeLevels_AreGroupedByDeclaringTypeNearestFirst()
	{
		string section = Inherited(ShapeChain(), "Lib.Shapes.Circle");

		section.Should().Contain("<td><a href=\"/api/lib/shapes/ellipse\">Ellipse</a></td>")
			.And.Contain("<td><a href=\"/api/lib/shapes/shape\">Shape</a></td>")
			.And.Contain(Link("/api/lib/shapes/ellipse#radius", "Radius"))
			.And.Contain(Link("/api/lib/shapes/ellipse#area", "Area()"))
			.And.Contain(Link("/api/lib/shapes/shape#name", "Name"))
			.And.Contain(Link("/api/lib/shapes/shape#describe", "Describe()"));
		section.IndexOf("/api/lib/shapes/ellipse\"", StringComparison.Ordinal)
			.Should().BeLessThan(section.IndexOf("/api/lib/shapes/shape\"", StringComparison.Ordinal));
	}

	[Fact]
	public void InheritedMembers_OverriddenAnywhereBelow_AreLeftOut()
	{
		// Ellipse overrides Shape.Area and Circle overrides Shape.Scale, so neither of Shape's is reachable.
		string section = Inherited(ShapeChain(), "Lib.Shapes.Circle");

		section.Should().NotContain("/api/lib/shapes/shape#area")
			.And.NotContain("#scale");
	}

	[Fact]
	public void InheritedMembers_LeaveOutConstructors()
	{
		string section = Inherited(ShapeChain(), "Lib.Shapes.Circle");

		section.Should().NotContain("#shape\"")
			.And.NotContain("#ellipse\"")
			.And.NotContain("Shape()");
	}

	[Fact]
	public void InheritedMembers_HiddenWithTheSameParameters_AreLeftOutButOtherOverloadsStay()
	{
		List<ApiType> types =
		[
			Type("Lib.Loader", members: [Method("Load", "string"), Method("Load", "string", "int"), Property("Path")]),
			Type("Lib.CachedLoader", baseType: "Lib.Loader", members: [Method("Load", "string?")])
		];

		string section = Inherited(types, "Lib.CachedLoader");

		// The second overload's anchor is load-2 on the Loader page.
		section.Should().Contain(Link("/api/lib/loader#load-2", "Load(string, int)"))
			.And.Contain(Link("/api/lib/loader#path", "Path"))
			.And.NotContain("/api/lib/loader#load\"");
	}

	[Fact]
	public void InheritedMembers_OfASealedType_LeaveOutProtectedMembers()
	{
		// Nothing can derive from a sealed type, so its readers can't reach what it inherits as protected.
		List<ApiType> types =
		[
			Type("Lib.Base", members:
			[
				Property("Name"),
				Method("OnChanged") with { Accessibility = ApiAccessibility.Protected },
				Property("Revision") with { Accessibility = ApiAccessibility.ProtectedInternal }
			]),
			Type("Lib.Open", baseType: "Lib.Base"),
			Type("Lib.Closed", baseType: "Lib.Base") with { IsSealed = true }
		];

		Inherited(types, "Lib.Open").Should().Contain("/api/lib/base#onchanged").And.Contain("/api/lib/base#revision");
		Inherited(types, "Lib.Closed").Should().Contain("/api/lib/base#name")
			.And.NotContain("#onchanged")
			.And.NotContain("#revision");
	}

	[Fact]
	public void InheritedMember_WithoutADetailBlock_LinksToItsSection()
	{
		List<ApiType> types =
		[
			Type("Lib.Base", members: [Method("Run") with { Documentation = null }]),
			Type("Lib.Derived", baseType: "Lib.Base")
		];

		Inherited(types, "Lib.Derived").Should().Contain(Link("/api/lib/base#methods", "Run()"));
	}

	[Theory]
	[InlineData("Lib.Base<T>", "Lib.Base<int>")] // the CLI
	[InlineData("Lib.Base", "Lib.Base<int>")] // the reflection host leaves type parameters out of the full name
	[InlineData("Lib.Base", "Base<int>")] // an unqualified reference
	public void InheritedMembers_FromAGenericBase_UseTheTypeArguments(string baseFullName, string baseReference)
	{
		List<ApiType> types =
		[
			Type(baseFullName, typeParameters: ["T"], members:
			[
				Method("Add", "T"), Method("Find", "T"), Method("Map", "T", "TOut") with
				{
					TypeParameters = [new ApiTypeParameter { Name = "TOut" }]
				},
				Property("Count")
			]),
			Type("Lib.IntList", baseType: baseReference, members:
			[
				Method("Add", "int") with { IsOverride = true },
				Method("Map", "int", "TResult") with { TypeParameters = [new ApiTypeParameter { Name = "TResult" }] }
			])
		];

		string section = Inherited(types, "Lib.IntList");

		section.Should().Contain("<td><a href=\"/api/lib/base\">Base&lt;int&gt;</a></td>")
			.And.Contain(Link("/api/lib/base#find", "Find(int)"))
			.And.Contain(Link("/api/lib/base#count", "Count"))
			.And.NotContain("#add")
			.And.NotContain("#map");
	}

	[Fact]
	public void InheritedMembers_ThroughTwoGenericLevels_CarryTheArgumentsAlong()
	{
		List<ApiType> types =
		[
			Type("Lib.Base<T>", typeParameters: ["T"], members: [Method("Find", "T")]),
			Type("Lib.Middle<U>", baseType: "Lib.Base<U>", typeParameters: ["U"], members: [Method("Peek", "U")]),
			Type("Lib.Leaf", baseType: "Lib.Middle<string>")
		];

		string section = Inherited(types, "Lib.Leaf");

		section.Should().Contain("<td><a href=\"/api/lib/middle\">Middle&lt;string&gt;</a></td>")
			.And.Contain(Link("/api/lib/middle#peek", "Peek(string)"))
			.And.Contain("<td><a href=\"/api/lib/base\">Base&lt;string&gt;</a></td>")
			.And.Contain(Link("/api/lib/base#find", "Find(string)"));
	}

	[Fact]
	public void InheritedMembers_FromABaseNamedWithoutItsNamespace_AreFound()
	{
		// The CLI compiles each project on its own, so a base type from another project has no namespace.
		List<ApiType> types =
		[
			Type("Lib.Core.Shape", members: [Property("Name")]),
			Type("Lib.App.Circle", baseType: "Shape")
		];

		Inherited(types, "Lib.App.Circle").Should().Contain(Link("/api/lib/core/shape#name", "Name"));
	}

	[Fact]
	public void InheritedMembers_AmbiguousUnqualifiedBase_PrefersTheEnclosingNamespace()
	{
		List<ApiType> types =
		[
			Type("Lib.Core.Shape", members: [Property("Name")]),
			Type("Lib.Other.Shape", members: [Property("Title")]),
			Type("Lib.Core.Round.Circle", baseType: "Shape"),
			Type("Lib.App.Circle", baseType: "Shape")
		];

		Inherited(types, "Lib.Core.Round.Circle").Should().Contain("/api/lib/core/shape#name")
			.And.NotContain("/api/lib/other/shape");

		// Neither Shape is in scope from Lib.App, so the page can't tell which one it means.
		Render(types, "Lib.App.Circle").Should().NotContain("Inherited Members");
	}

	[Fact]
	public void InheritedMembers_BaseOutsideTheModel_IsNotMatchedByItsSimpleName()
	{
		// System.Exception isn't documented. A documented type called Exception must not stand in for it.
		List<ApiType> types =
		[
			Type("Lib.Exception", members: [Property("Code")]),
			Type("Lib.App.Failure", baseType: "System.Exception")
		];

		Render(types, "Lib.App.Failure").Should().NotContain("Inherited Members");
	}

	[Fact]
	public void InheritedMembers_OfAnInterface_ComeFromTheInterfacesItExtends()
	{
		List<ApiType> types =
		[
			Type("Lib.IReader", ApiTypeKind.Interface, members: [Method("Read"), Property("Position")]),
			Type("Lib.IStream", ApiTypeKind.Interface, interfaces: ["Lib.IReader"], members: [Method("Seek", "long")]),
			Type("Lib.IBufferedStream", ApiTypeKind.Interface, interfaces: ["Lib.IStream"],
				members: [Method("Flush"), Property("Position") with { Signature = "new long Position { get; }" }])
		];

		string section = Inherited(types, "Lib.IBufferedStream");

		section.Should().Contain(Link("/api/lib/istream#seek", "Seek(long)"))
			.And.Contain(Link("/api/lib/ireader#read", "Read()"))
			.And.NotContain("#position");
	}

	[Fact]
	public void InheritedMembers_OfAClass_DoNotIncludeInterfaceMembers()
	{
		// A class implements an interface's members; it doesn't inherit them. The CLI records an interface
		// from another project as the base type, which mustn't turn it into one either.
		List<ApiType> types =
		[
			Type("Lib.Core.IStream", ApiTypeKind.Interface, members: [Method("Seek", "long")]),
			Type("Lib.Core.FileStream", interfaces: ["Lib.Core.IStream"]),
			Type("Lib.App.NetworkStream", baseType: "IStream")
		];

		Render(types, "Lib.Core.FileStream").Should().NotContain("Inherited Members");
		Render(types, "Lib.App.NetworkStream").Should().NotContain("Inherited Members");
	}

	[Theory]
	[InlineData("Lib.Outer<T>", "Lib.Outer<T>.Inner", new string[0], "Peek(T)")] // the CLI
	[InlineData("Lib.Outer", "Lib.Outer.Inner", new[] { "T" }, "Peek(int)")] // the host lists Outer's T on Inner too
	public void InheritedMembers_FromANestedTypeOfAGenericType_AreFound(string outerFullName, string innerFullName,
		string[] innerTypeParameters, string peek)
	{
		List<ApiType> types =
		[
			Type(outerFullName, typeParameters: ["T"]) with { Name = "Outer" },
			Type(innerFullName, typeParameters: innerTypeParameters, members: [Method("Peek", "T"), Property("Count")])
				with { Name = "Inner", Namespace = "Lib" },
			Type("Lib.Derived", baseType: "Lib.Outer<int>.Inner")
		];

		string section = Inherited(types, "Lib.Derived");

		section.Should().Contain("<td><a href=\"/api/lib/outer.inner\">Outer&lt;int&gt;.Inner</a></td>")
			.And.Contain(Link("/api/lib/outer.inner#count", "Count"))
			.And.Contain(Link("/api/lib/outer.inner#peek", peek));
	}

	[Fact]
	public void InheritedMembers_SameNamedMembersOfSiblingInterfaces_AreBothListed()
	{
		List<ApiType> types =
		[
			Type("Lib.IA", ApiTypeKind.Interface, members: [Method("Reset")]),
			Type("Lib.IB", ApiTypeKind.Interface, members: [Method("Reset")]),
			Type("Lib.IC", ApiTypeKind.Interface, interfaces: ["Lib.IA", "Lib.IB"])
		];

		Inherited(types, "Lib.IC").Should()
			.Contain(Link("/api/lib/ia#reset", "Reset()"))
			.And.Contain(Link("/api/lib/ib#reset", "Reset()"));
	}

	[Fact]
	public void InheritedMembers_HiddenOnOnePathOfADiamond_StayHidden()
	{
		// IA hides IBase.Close with new; C# hides it on every path, so IB's path doesn't bring it back.
		List<ApiType> types =
		[
			Type("Lib.IBase", ApiTypeKind.Interface, members: [Method("Close"), Method("Open")]),
			Type("Lib.IA", ApiTypeKind.Interface, interfaces: ["Lib.IBase"], members: [Method("Close")]),
			Type("Lib.IB", ApiTypeKind.Interface, interfaces: ["Lib.IBase"]),
			Type("Lib.IC", ApiTypeKind.Interface, interfaces: ["Lib.IA", "Lib.IB"])
		];

		string section = Inherited(types, "Lib.IC");

		section.Should().Contain(Link("/api/lib/ia#close", "Close()"))
			.And.Contain(Link("/api/lib/ibase#open", "Open()"))
			.And.NotContain("/api/lib/ibase#close");
	}

	[Fact]
	public void TypeGraph_ASubclassOfAnOutsideTypeWithTheSameName_IsNotADerivedType()
	{
		List<ApiType> types =
		[
			Type("Lib.Exception", members: [Property("Code")]),
			Type("Lib.App.Failure", baseType: "System.Exception"),
			Type("Lib.App.Fault", baseType: "Lib.Exception")
		];

		string page = Render(types, "Lib.Exception");

		page.Should().Contain("class T1[\"Fault\"]").And.NotContain("Failure");
	}

	#endregion

	#region Extension methods

	private static List<ApiType> ExtensionModel() =>
	[
		Type("Lib.IRenderable", ApiTypeKind.Interface),
		Type("Lib.IDrawable", ApiTypeKind.Interface, interfaces: ["Lib.IRenderable"]),
		Type("Lib.Shape", interfaces: ["Lib.IDrawable"]),
		Type("Lib.Circle", baseType: "Lib.Shape"),
		Type("Lib.Box<T>", typeParameters: ["T"]),
		Type("Lib.Box"),
		StaticClass("Lib.ShapeExtensions",
			Extension("Describe", "Shape"),
			Extension("Draw", "IDrawable"),
			Extension("Render", "IRenderable"),
			Extension("Roll", "Circle"),
			Extension("Count", "IEnumerable<Shape>"),
			Extension("Sum", "Shape[]"),
			Extension("Shout", "string"),
			Method("Helper", "Shape") with { IsStatic = true }),
		StaticClass("Lib.BoxExtensions",
			Extension("Open", "Box<T>", "T"),
			Extension("Close", "Box"))
	];

	[Fact]
	public void ExtensionMethods_OnTheType_LinkToTheDeclaringClass()
	{
		string section = Extensions(ExtensionModel(), "Lib.Shape");

		section.Should().Contain(Link("/api/lib/shapeextensions#describe", "Describe(Shape)"))
			.And.Contain("<td><a href=\"/api/lib/shapeextensions\">ShapeExtensions</a></td>")
			.And.Contain("<td>Describe summary.</td>")
			.And.NotContain("#roll");
	}

	[Fact]
	public void ExtensionMethods_OnABaseTypeOrAnImplementedInterface_ApplyToTheDerivedType()
	{
		string section = Extensions(ExtensionModel(), "Lib.Circle");

		// Describe is on the base class, Draw on the interface the base implements and Render on the
		// interface that one extends.
		section.Should().Contain("/api/lib/shapeextensions#describe")
			.And.Contain("/api/lib/shapeextensions#draw")
			.And.Contain("/api/lib/shapeextensions#render")
			.And.Contain("/api/lib/shapeextensions#roll");
	}

	[Fact]
	public void ExtensionMethods_OnOutsideTypesArraysOrNothing_AreNotListed()
	{
		string section = Extensions(ExtensionModel(), "Lib.Circle");

		section.Should().NotContain("#count")
			.And.NotContain("#sum")
			.And.NotContain("#shout")
			.And.NotContain("#helper");
	}

	[Fact]
	public void ExtensionMethods_OnANullableReceiver_ApplyToClassesButNotStructs()
	{
		// Shape? only annotates a class. Point? is Nullable<Point>, which a Point doesn't convert to.
		List<ApiType> types = ExtensionModel();
		types.Add(Type("Lib.Point", ApiTypeKind.Struct));
		types.Add(StaticClass("Lib.NullableExtensions", Extension("Outline", "Shape?"), Extension("Nudge", "Point?")));

		Extensions(types, "Lib.Shape").Should().Contain(Link("/api/lib/nullableextensions#outline", "Outline(Shape?)"));
		Render(types, "Lib.Point").Should().NotContain("Extension Methods");
	}

	[Fact]
	public void ExtensionMethods_OnAnInterface_AreListedOnTheInterface()
	{
		string section = Extensions(ExtensionModel(), "Lib.IDrawable");

		section.Should().Contain("#draw")
			.And.Contain("#render")
			.And.NotContain("#describe");
	}

	[Fact]
	public void ExtensionMethods_MatchGenericTypesByNameAndArity()
	{
		List<ApiType> types = ExtensionModel();

		Extensions(types, "Lib.Box<T>").Should()
			.Contain(Link("/api/lib/boxextensions#open", "Open&lt;T&gt;(Box&lt;T&gt;)"))
			.And.NotContain("#close");
		Extensions(types, "Lib.Box").Should().Contain("/api/lib/boxextensions#close")
			.And.NotContain("#open");
	}

	[Fact]
	public void ExtensionMethods_OnATypeParameterConstrainedToADocumentedType_ApplyToItsDerivedTypes()
	{
		List<ApiType> types = ExtensionModel();
		types.Add(StaticClass("Lib.TagExtensions",
			Extension("Tag", "T", "T : Lib.Shape, class"),
			Extension("Mark", "T", "T : Lib.Shape, IComparable<T>"),
			Extension("Tap", "T", "T")));

		string circle = Extensions(types, "Lib.Circle");
		circle.Should().Contain(Link("/api/lib/tagextensions#tag", "Tag&lt;T&gt;(T)"));
		// IComparable<T> isn't documented, so the page can't tell whether Circle satisfies it.
		circle.Should().NotContain("#mark").And.NotContain("#tap");
		Render(types, "Lib.Box").Should().NotContain("/api/lib/tagextensions#tag");
	}

	[Fact]
	public void ExtensionMethods_ReflectionStyleNames_AreFound()
	{
		List<ApiType> types =
		[
			Type("Fixture.Shapes.Shape"),
			Type("Fixture.Shapes.Square", baseType: "Shape"),
			StaticClass("Fixture.Shapes.ShapeExtensions", Extension("Describe", "Shape"))
		];

		Extensions(types, "Fixture.Shapes.Square").Should()
			.Contain(Link("/api/fixture/shapes/shapeextensions#describe", "Describe(Shape)"));
	}

	#endregion

	#region Placement and the table of contents

	[Fact]
	public void Sections_WithNothingToList_AreNotRendered()
	{
		List<ApiType> types = ExtensionModel();
		ApiType extensions = types.Single(t => t.FullName == "Lib.ShapeExtensions");

		string html = ApiPageRenderer.RenderType(extensions, types);

		html.Should().NotContain("Inherited Members").And.NotContain("Extension Methods");
		ApiPageRenderer.BuildTocForType(extensions, types).Entries.Should()
			.NotContain(e => e.Id == "inherited-members" || e.Id == "extension-methods");
	}

	[Fact]
	public void Sections_AreRenderedAfterTheTypesOwnMembersAndListedInTheToc()
	{
		List<ApiType> types = ShapeChain();
		types.Add(StaticClass("Lib.Shapes.ShapeExtensions", Extension("Describe", "Shape")));
		ApiType circle = types.Single(t => t.Name == "Circle");
		circle = circle with { Documentation = new XmlDocBlock { Examples = ["<pre><code>new Circle()</code></pre>"] } };
		types[types.FindIndex(t => t.Name == "Circle")] = circle;

		string html = ApiPageRenderer.RenderType(circle, types);
		TableOfContents toc = ApiPageRenderer.BuildTocForType(circle, types);

		int methods = html.IndexOf("<h2 id=\"methods\">", StringComparison.Ordinal);
		int inherited = html.IndexOf("<h2 id=\"inherited-members\">", StringComparison.Ordinal);
		int extensions = html.IndexOf("<h2 id=\"extension-methods\">", StringComparison.Ordinal);
		int examples = html.IndexOf("<h2 id=\"examples\">", StringComparison.Ordinal);
		methods.Should().BePositive();
		inherited.Should().BeGreaterThan(methods);
		extensions.Should().BeGreaterThan(inherited);
		examples.Should().BeGreaterThan(extensions);

		toc.Entries.Select(e => e.Id).Should().ContainInOrder("methods", "inherited-members", "extension-methods", "examples");
		ApiPageRenderer.BuildTocForType(circle).Entries.Should().NotContain(e => e.Id == "inherited-members");
	}

	[Fact]
	public void Sections_WithoutTheOtherTypes_AreNotRendered()
	{
		ApiType circle = ShapeChain().Single(t => t.Name == "Circle");

		ApiPageRenderer.RenderType(circle).Should().NotContain("Inherited Members");
	}

	[Fact]
	public void Sections_ForPythonTypes_AreNotRendered()
	{
		// Python pages live under the plugin's route prefix, so /api links would be wrong.
		List<ApiType> types =
		[
			Type("shapes.Shape", members: [Property("name")]) with { SourcePath = "shapes.py" },
			Type("shapes.Circle", baseType: "Shape") with { SourcePath = "shapes.py" }
		];

		Render(types, "shapes.Circle").Should().NotContain("Inherited Members");
	}

	#endregion
}
