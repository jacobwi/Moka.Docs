using FluentAssertions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     C# 14 extension blocks on API pages: grouped under their receiver on the static class's page, and
///     listed with the extension methods on the pages of the types they apply to.
/// </summary>
public sealed class ApiExtensionBlockRenderingTests
{
	#region Model helpers

	private static ApiExtensionBlock Block(string signature, string receiverName, string receiverType,
		string? summary = null, bool isRef = false, string? grouping = null, params ApiTypeParameter[] typeParameters) => new()
	{
		Signature = signature,
		DocumentationId = grouping is null ? null : $"T:Lib.ShapeExtensions.{grouping}.<M>$1F2E3D",
		Receiver = new ApiParameter { Name = receiverName, Type = receiverType, IsRef = isRef },
		TypeParameters = [..typeParameters],
		Documentation = summary is null
			? null
			: new XmlDocBlock { Summary = summary, Parameters = new Dictionary<string, string> { [receiverName] = $"The {receiverName}." } }
	};

	private static readonly ApiExtensionBlock _shapeBlock = Block("extension(Shape shape)", "shape", "Shape", "Members for any shape.", grouping: "<G>$0A8A2B8D");
	private static readonly ApiExtensionBlock _maybeShapeBlock = Block("extension(Shape? shape)", "shape", "Shape?", grouping: "<G>$0A8A2B8D");
	private static readonly ApiExtensionBlock _drawableBlock = Block("extension(IDrawable)", "", "IDrawable");

	private static readonly ApiExtensionBlock _genericBlock = Block(
		"extension<TShape>(TShape item) where TShape : Lib.Shape", "item", "TShape", "Members for shape types.",
		grouping: "<G>$C4989336`1", typeParameters: new ApiTypeParameter { Name = "TShape", Constraints = ["Lib.Shape"] });

	private static readonly ApiExtensionBlock _pointBlock = Block("extension(ref Point point)", "point", "Point", isRef: true, grouping: "<G>$D0997B04");
	private static readonly ApiExtensionBlock _maybePointBlock = Block("extension(Point? point)", "point", "Point?");

	private static ApiMember Member(string name, ApiMemberKind kind, ApiExtensionBlock? block, bool isStatic = false,
		string? summary = "", params string[] parameterTypes) => new()
	{
		Name = name,
		Kind = kind,
		Signature = $"public {(isStatic ? "static " : "")}void {name}()",
		IsStatic = isStatic,
		Extension = block,
		Parameters = [..parameterTypes.Select((type, i) => new ApiParameter { Name = $"p{i}", Type = type })],
		Documentation = summary is null ? null : new XmlDocBlock { Summary = summary.Length > 0 ? summary : $"{name} summary." }
	};

	private static ApiType Type(string fullName, ApiTypeKind kind = ApiTypeKind.Class, string? baseType = null,
		string[]? interfaces = null, string? summary = null, params ApiMember[] members)
	{
		int dot = fullName.LastIndexOf('.');
		return new ApiType
		{
			Name = fullName[(dot + 1)..],
			FullName = fullName,
			Namespace = fullName[..dot],
			Kind = kind,
			BaseType = baseType,
			ImplementedInterfaces = [..interfaces ?? []],
			Members = [..members],
			Documentation = summary is null ? null : new XmlDocBlock { Summary = summary }
		};
	}

	private static List<ApiType> Model() =>
	[
		Type("Lib.IDrawable", ApiTypeKind.Interface),
		Type("Lib.Shape", interfaces: ["Lib.IDrawable"]),
		Type("Lib.Circle", baseType: "Lib.Shape"),
		Type("Lib.Point", ApiTypeKind.Struct),
		Type("Lib.Unrelated", summary:
			"See <a data-cref=\"P:Lib.ShapeExtensions.&lt;G&gt;$0A8A2B8D.Perimeter\">ShapeExtensions.Perimeter</a>, " +
			"<a data-cref=\"M:Lib.ShapeExtensions.Grow(Lib.Shape,System.Double)\">ShapeExtensions.Grow</a>, " +
			"<a data-cref=\"M:Lib.ShapeExtensions.get_Perimeter(Lib.Shape)\">ShapeExtensions.get_Perimeter</a> and " +
			"<a data-cref=\"M:Lib.ShapeExtensions.&lt;G&gt;$0A8A2B8D.Undocumented\">ShapeExtensions.Undocumented</a>. " +
			"Fill with <a data-cref=\"M:Lib.ShapeExtensions.&lt;G&gt;$C4989336`1.Fill(`0)\">ShapeExtensions.Fill</a>. " +
			"Resize with <a data-cref=\"M:Lib.ShapeExtensions.&lt;G&gt;$D0997B04.Resize(System.Double)\">ShapeExtensions.Resize</a> " +
			"or <a data-cref=\"M:Lib.ShapeExtensions.Resize(Lib.Point@,System.Double)\">ShapeExtensions.Resize</a>."),
		Type("Lib.ShapeExtensions", members:
		[
			Member("Describe", ApiMemberKind.Method, null, true, parameterTypes: "Shape") with { IsExtensionMethod = true },
			Member("Perimeter", ApiMemberKind.Property, _shapeBlock),
			Member("Grow", ApiMemberKind.Method, _shapeBlock, parameterTypes: "double"),
			Member("Unit", ApiMemberKind.Method, _shapeBlock, true),
			Member("op_Addition", ApiMemberKind.Operator, _shapeBlock, true, parameterTypes: ["Shape", "Shape"]),
			Member("Undocumented", ApiMemberKind.Method, _shapeBlock, summary: null),
			Member("Resize", ApiMemberKind.Method, _shapeBlock, parameterTypes: "double"),
			Member("IsMissing", ApiMemberKind.Property, _maybeShapeBlock),
			Member("Count", ApiMemberKind.Method, _drawableBlock, true),
			Member("Copy", ApiMemberKind.Method, _genericBlock),
			Member("Fill", ApiMemberKind.Method, _genericBlock, parameterTypes: "int"),
			Member("Fill", ApiMemberKind.Method, _genericBlock, parameterTypes: "TShape"),
			Member("Move", ApiMemberKind.Method, _pointBlock, parameterTypes: "int"),
			Member("Resize", ApiMemberKind.Method, _pointBlock, parameterTypes: "double"),
			Member("Nudge", ApiMemberKind.Method, _maybePointBlock)
		]) with { IsStatic = true, IsAbstract = true, IsSealed = true }
	];

	private static string Render(List<ApiType> types, string fullName) =>
		ApiPageRenderer.RenderType(types.Single(t => t.FullName == fullName), types);

	private static TableOfContents Toc(List<ApiType> types, string fullName) =>
		ApiPageRenderer.BuildTocForType(types.Single(t => t.FullName == fullName), types);

	// From the section's heading to the next one.
	private static string Section(string html, string id)
	{
		int start = html.IndexOf($"<h2 id=\"{id}\">", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the page should have a section with id {id}");
		int end = html.IndexOf("<h2 ", start + 1, StringComparison.Ordinal);
		return end < 0 ? html[start..] : html[start..end];
	}

	private static string Link(string href, string text) =>
		$"<a href=\"{href}\" class=\"api-member-link\"><code>{text}</code></a>";

	private const string _staticBadge = "<span class=\"api-badge-sm api-badge-static\">static</span>";

	#endregion

	#region The static class page

	[Fact]
	public void StaticClassPage_GroupsBlockMembersUnderTheirReceiver()
	{
		string section = Section(Render(Model(), "Lib.ShapeExtensions"), "extension-shape");

		section.Should().StartWith("<h2 id=\"extension-shape\">extension(Shape shape)</h2>")
			.And.Contain("<pre class=\"api-signature\"><code class=\"language-csharp\">extension(Shape shape)</code></pre>")
			.And.Contain("<p>Members for any shape.</p>")
			.And.Contain("<tr><td><code>shape</code></td><td><code>Shape</code></td><td>The shape.</td></tr>")
			.And.Contain("<a href=\"#perimeter\" class=\"api-member-link\"><code>Perimeter</code></a>")
			.And.Contain("<a href=\"#grow\" class=\"api-member-link\"><code>Grow(double p0)</code></a>")
			.And.Contain($"<a href=\"#unit\" class=\"api-member-link\"><code>Unit()</code></a> {_staticBadge}")
			.And.Contain("<a href=\"#op_addition\" class=\"api-member-link\"><code>operator +(Shape p0, Shape p1)</code></a>")
			.And.Contain("<h3 id=\"perimeter\">Perimeter</h3>")
			.And.Contain("<h3 id=\"grow\">Grow(double p0)</h3>");

		// Without documentation a member has no detail block, so its name isn't a link.
		section.Should().Contain("<td><code>Undocumented()</code></td>");
		section.Should().NotContain("IsMissing").And.NotContain("Count()");
	}

	[Fact]
	public void StaticClassPage_KeepsBlockMembersOutOfTheKindSections()
	{
		List<ApiType> types = Model();
		string html = Render(types, "Lib.ShapeExtensions");

		Section(html, "methods").Should().Contain("#describe").And.NotContain("#grow").And.NotContain("#unit");
		html.Should().NotContain("<h2 id=\"properties\">").And.NotContain("<h2 id=\"operators\">");

		Toc(types, "Lib.ShapeExtensions").Entries.Select(e => (e.Id, e.Text)).Should().Equal(
			("methods", "Methods"),
			("extension-shape", "extension(Shape shape)"),
			("extension-shape-2", "extension(Shape? shape)"),
			("extension-idrawable", "extension(IDrawable)"),
			("extension-tshape", "extension<TShape>(TShape item)"),
			("extension-point", "extension(ref Point point)"),
			("extension-point-2", "extension(Point? point)"));
	}

	[Fact]
	public void StaticClassPage_GenericBlock_ShowsItsConstraintsAndTypeParameters()
	{
		string section = Section(Render(Model(), "Lib.ShapeExtensions"), "extension-tshape");

		section.Should().StartWith("<h2 id=\"extension-tshape\">extension&lt;TShape&gt;(TShape item)</h2>")
			.And.Contain("<code class=\"language-csharp\">extension&lt;TShape&gt;(TShape item) where TShape : Lib.Shape</code>")
			.And.Contain("<h4>Type Parameters</h4>")
			.And.Contain("<tr><td><code>TShape</code></td><td>Lib.Shape</td>");
	}

	[Fact]
	public void StaticClassPage_BlockWithTheSameDeclarationTwice_IsOneSection()
	{
		// C# allows several blocks with the same receiver; the page lists their members together.
		ApiExtensionBlock again = _shapeBlock with { Documentation = null };
		List<ApiType> types = Model();
		ApiType extensions = types.Single(t => t.Name == "ShapeExtensions");
		extensions.Members.Add(Member("Area", ApiMemberKind.Property, again));

		string html = Render(types, "Lib.ShapeExtensions");

		Section(html, "extension-shape").Should().Contain("#area\"");
		html.Should().Contain("<h2 id=\"extension-shape-2\">extension(Shape? shape)</h2>");
	}

	#endregion

	#region Receiver pages

	[Fact]
	public void ReceiverPage_ListsBlockMembersWithTheExtensionMethods()
	{
		List<ApiType> types = Model();
		string section = Section(Render(types, "Lib.Shape"), "extension-methods");

		section.Should().StartWith("<h2 id=\"extension-methods\">Extension Members</h2>")
			.And.Contain(Link("/api/lib/shapeextensions#describe", "Describe(Shape)"))
			.And.Contain(Link("/api/lib/shapeextensions#perimeter", "Perimeter"))
			.And.Contain(Link("/api/lib/shapeextensions#grow", "Grow(double)"))
			.And.Contain(Link("/api/lib/shapeextensions#unit", "Unit()") + " " + _staticBadge)
			.And.Contain(Link("/api/lib/shapeextensions#op_addition", "operator +(Shape, Shape)"))
			.And.Contain(Link("/api/lib/shapeextensions#ismissing", "IsMissing"))
			.And.Contain(Link("/api/lib/shapeextensions#count", "Count()"))
			.And.Contain(Link("/api/lib/shapeextensions#copy", "Copy()"))
			.And.Contain("<td><a href=\"/api/lib/shapeextensions\">ShapeExtensions</a></td>")
			.And.Contain("<td>Perimeter summary.</td>");

		// The classic extension method is static too, but it is called on an instance.
		section.Should().NotContain(Link("/api/lib/shapeextensions#describe", "Describe(Shape)") + " " + _staticBadge);

		Toc(types, "Lib.Shape").Entries.Should()
			.ContainSingle(e => e.Id == "extension-methods").Which.Text.Should().Be("Extension Members");
	}

	[Fact]
	public void ReceiverPage_MemberWithoutADetailBlock_LinksToItsBlocksSection()
	{
		Section(Render(Model(), "Lib.Shape"), "extension-methods").Should()
			.Contain(Link("/api/lib/shapeextensions#extension-shape", "Undocumented()"));
	}

	[Fact]
	public void DerivedTypePage_ListsInstanceAndStaticBlockMembers()
	{
		// C# finds static extension members through a derived type as well: Circle.Unit() compiles.
		string section = Section(Render(Model(), "Lib.Circle"), "extension-methods");

		section.Should().Contain("#perimeter\"")
			.And.Contain("#unit\"")
			.And.Contain("#count\"")
			.And.Contain("#copy\"")
			.And.Contain("#describe\"");
	}

	[Fact]
	public void InterfacePage_ListsOnlyTheBlocksForTheInterface()
	{
		string section = Section(Render(Model(), "Lib.IDrawable"), "extension-methods");

		section.Should().Contain("#count\"").And.NotContain("#perimeter\"").And.NotContain("#describe\"");
	}

	[Fact]
	public void StructPage_ListsARefReceiverButNotANullableOne()
	{
		// Point? is Nullable<Point>, which a Point doesn't convert to.
		string section = Section(Render(Model(), "Lib.Point"), "extension-methods");

		section.Should().Contain(Link("/api/lib/shapeextensions#move", "Move(int)")).And.NotContain("#nudge\"");
	}

	[Fact]
	public void ReceiverPage_WithOnlyClassicExtensionMethods_KeepsItsHeading()
	{
		List<ApiType> types =
		[
			Type("Lib.Shape"),
			Type("Lib.ShapeExtensions", members:
				Member("Describe", ApiMemberKind.Method, null, true, parameterTypes: "Shape") with { IsExtensionMethod = true })
		];

		Render(types, "Lib.Shape").Should().Contain("<h2 id=\"extension-methods\">Extension Methods</h2>");
		Toc(types, "Lib.Shape").Entries.Should().ContainSingle(e => e.Id == "extension-methods")
			.Which.Text.Should().Be("Extension Methods");
	}

	[Fact]
	public void UnrelatedTypePage_HasNoExtensionSection()
	{
		Render(Model(), "Lib.Unrelated").Should().NotContain("id=\"extension-methods\"");
	}

	#endregion

	#region Cref links

	[Fact]
	public void CrefsToExtensionMembers_LinkToTheirEntries()
	{
		string summary = Render(Model(), "Lib.Unrelated");

		summary.Should()
			.Contain("<a href=\"/api/lib/shapeextensions#perimeter\">ShapeExtensions.Perimeter</a>")
			.And.Contain("<a href=\"/api/lib/shapeextensions#grow\">ShapeExtensions.Grow</a>")
			.And.Contain("<a href=\"/api/lib/shapeextensions#extension-shape\">ShapeExtensions.Undocumented</a>");

		// The compiler's get_Perimeter(Shape) implements the property.
		summary.Should().NotContain("get_Perimeter")
			.And.Contain(", <a href=\"/api/lib/shapeextensions#perimeter\">ShapeExtensions.Perimeter</a> and");

		// `0 is the block's type parameter, so the overload taking TShape is the one meant, not Fill(int).
		summary.Should().Contain("Fill with <a href=\"/api/lib/shapeextensions#fill-2\">ShapeExtensions.Fill</a>.");

		// Both blocks have Resize(double). The grouping type in the ID, or the receiver in the implementation
		// method's, says which one: the Point block's, whose anchor is resize-2.
		summary.Should().Contain(
			"Resize with <a href=\"/api/lib/shapeextensions#resize-2\">ShapeExtensions.Resize</a> " +
			"or <a href=\"/api/lib/shapeextensions#resize-2\">ShapeExtensions.Resize</a>.");
	}

	#endregion
}
