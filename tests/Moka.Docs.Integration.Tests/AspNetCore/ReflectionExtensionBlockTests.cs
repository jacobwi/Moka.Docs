using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     C# 14 extension blocks in the API model the ASP.NET Core host builds by reflection, from a library
///     compiled with its XML documentation file.
/// </summary>
public sealed class ReflectionExtensionBlockTests
{
	private static readonly Lazy<ApiReference> _model = new(() =>
		new ReflectionApiModelBuilder(
				new XmlDocParser(NullLogger<XmlDocParser>.Instance),
				new InheritDocResolver(),
				NullLogger<ReflectionApiModelBuilder>.Instance)
			.Build([ExtensionFixtureAssembly.Instance], true));

	private static ApiType Extensions =>
		_model.Value.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == "ShapeExtensions");

	private static ApiMember Member(string name) => Extensions.Members.Single(m => m.Name == name);

	[Fact]
	public void GroupingAndMarkerTypes_AreNotDocumented()
	{
		_model.Value.Namespaces.SelectMany(ns => ns.Types).Select(t => t.FullName).Should().BeEquivalentTo(
			"Fixture.Extensions.Shape", "Fixture.Extensions.Square", "Fixture.Extensions.Point",
			"Fixture.Extensions.ShapeExtensions");
	}

	[Fact]
	public void ImplementationMethods_AreNotListedNextToTheMembersTheyImplement()
	{
		// The compiler emits a public static method on the class for every block member and accessor.
		Extensions.Members.Select(m => m.Name).Should().BeEquivalentTo(
			"Describe", "Twice", "Perimeter", "Label", "Grow", "Unit", "Default", "op_Addition",
			"Largest", "AddMany", "Convert", "IsEmpty", "Move", "Create", "IsMissing");
	}

	[Fact]
	public void BlockMembers_KeepTheirKindAndWhetherTheyAreStatic()
	{
		Extensions.Members.Where(m => m.Extension is not null)
			.Select(m => (m.Name, m.Kind, m.IsStatic, m.IsExtensionMethod))
			.Should().BeEquivalentTo([
				("Perimeter", ApiMemberKind.Property, false, false),
				("Label", ApiMemberKind.Property, false, false),
				("Grow", ApiMemberKind.Method, false, false),
				("Unit", ApiMemberKind.Method, true, false),
				("Default", ApiMemberKind.Property, true, false),
				("op_Addition", ApiMemberKind.Operator, true, false),
				("Largest", ApiMemberKind.Method, false, false),
				("AddMany", ApiMemberKind.Method, false, false),
				("Convert", ApiMemberKind.Method, false, false),
				("IsEmpty", ApiMemberKind.Property, false, false),
				("Move", ApiMemberKind.Method, false, false),
				("Create", ApiMemberKind.Method, true, false),
				("IsMissing", ApiMemberKind.Property, false, false)
			]);

		Member("Describe").Should().BeEquivalentTo(new
		{
			Extension = (ApiExtensionBlock?)null,
			IsExtensionMethod = true,
			Signature = "public static string Describe(this Shape shape)"
		});
		Member("Twice").Extension.Should().BeNull();
	}

	[Fact]
	public void Signatures_ReadLikeTheSource()
	{
		Member("Perimeter").Signature.Should().Be("public double Perimeter { get; }");
		Member("Label").Signature.Should().Be("public string Label { get; set; }");
		Member("Grow").Signature.Should().Be("public Shape Grow(double factor)");
		Member("Unit").Signature.Should().Be("public static Shape Unit()");
		Member("Default").Signature.Should().Be("public static Shape Default { get; }");
		Member("op_Addition").Signature.Should().Be("public static Shape operator +(Shape left, Shape right)");
		Member("Move").Signature.Should().Be("public void Move(int dx)");

		Member("Grow").Extension!.Signature.Should().Be("extension(Shape shape)");
		Member("Move").Extension!.Signature.Should().Be("extension(ref Point point)");
		Member("Create").Extension!.Signature.Should().Be("extension(Shape)");
		Member("IsMissing").Extension!.Signature.Should().Be("extension(Shape? shape)");
	}

	[Fact]
	public void Receivers_KeepTheirNameTypeAndModifiers()
	{
		Member("Grow").Extension!.Receiver.Should().BeEquivalentTo(new { Name = "shape", Type = "Shape", IsRef = false });
		Member("Move").Extension!.Receiver.Should().BeEquivalentTo(new { Name = "point", Type = "Point", IsRef = true });
		Member("Create").Extension!.Receiver.Should().BeEquivalentTo(new { Name = "", Type = "Shape" });
		Member("IsMissing").Extension!.Receiver.Should().BeEquivalentTo(new { Type = "Shape?", IsNullable = true });
	}

	[Fact]
	public void GenericBlock_UsesItsOwnTypeParameterNames()
	{
		// The grouping type calls the block's type parameter $T0.
		ApiExtensionBlock block = Member("Largest").Extension!;

		block.Signature.Should().Be("extension<T>(List<T> list) where T : IComparable<T>");
		block.TypeParameters.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new { Name = "T", Constraints = new[] { "IComparable<T>" } });
		Member("Largest").Signature.Should().Be("public T Largest()");
		Member("Largest").ReturnType.Should().Be("T");
		Member("AddMany").Parameters.Select(p => (p.Name, p.Type)).Should().Equal(("item", "T"), ("count", "int"));
		Member("Convert").Signature.Should().Be("public List<TOut> Convert<TOut>(Func<T, TOut> convert)");
		Member("Convert").TypeParameters.Select(tp => tp.Name).Should().Equal("TOut");
		Member("IsEmpty").Signature.Should().Be("public bool IsEmpty { get; }");

		Extensions.Members.SelectMany(m => new[] { m.Signature, m.ReturnType ?? "" }.Concat(m.Parameters.Select(p => p.Type)))
			.Should().NotContain(text => text.Contains('$'));
	}

	[Fact]
	public void XmlDocs_AreMatchedByTheIdsTheCompilerWrites()
	{
		Member("Grow").Documentation.Should().BeEquivalentTo(new
		{
			Summary = "Grows the shape.",
			Returns = "The grown shape.",
			Parameters = new Dictionary<string, string> { ["factor"] = "The factor." }
		});
		Member("Perimeter").Documentation!.Summary.Should().Be("The perimeter.");
		Member("Default").Documentation!.Summary.Should().Be("The default shape.");
		Member("op_Addition").Documentation!.Parameters.Should().ContainKeys("left", "right");
		Member("AddMany").Documentation!.Summary.Should().Be("Adds an item several times.");
		Member("Convert").Documentation!.TypeParameters.Should().ContainKey("TOut");
		Member("IsEmpty").Documentation!.Summary.Should().Be("Whether the list is empty.");

		// The block's own comment carries the receiver's <param> and its <typeparam>.
		Member("Grow").Extension!.Documentation.Should().BeEquivalentTo(new
		{
			Summary = "Members for any shape.",
			Parameters = new Dictionary<string, string> { ["shape"] = "The shape to extend." }
		});
		Member("Largest").Extension!.Documentation.Should().BeEquivalentTo(new
		{
			TypeParameters = new Dictionary<string, string> { ["T"] = "The item type." },
			Parameters = new Dictionary<string, string> { ["list"] = "The list." }
		});
		Member("Create").Extension!.Documentation.Should().BeNull();
	}

	[Fact]
	public void MemberAttributes_ComeFromTheBlockMember()
	{
		ApiMember perimeter = Member("Perimeter");

		perimeter.IsObsolete.Should().BeTrue();
		perimeter.ObsoleteMessage.Should().Be("Use Width.");
		Extensions.Members.SelectMany(m => m.Attributes).Should().NotContain(a => a.Name.StartsWith("Extension"));
	}

	[Fact]
	public void Model_MatchesTheSourceAnalysis()
	{
		// The CLI reads the same library from source, with the project's nullable setting: the block's ID
		// depends on the receiver's nullable annotations. Return types and constraints differ in
		// qualification between the two hosts already, so the comparison leaves them out.
		CSharpCompilation compilation = CSharpCompilation.Create("Fixture",
			[CSharpSyntaxTree.ParseText(ExtensionFixtureAssembly.Source, new CSharpParseOptions(LanguageVersion.CSharp14),
				cancellationToken: TestContext.Current.CancellationToken)],
			((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? "")
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(path => MetadataReference.CreateFromFile(path)),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
		ApiType fromSource = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance)
			.AnalyzeCompilation(compilation, "Fixture")
			.Namespaces.SelectMany(ns => ns.Types)
			.Single(t => t.Name == "ShapeExtensions");

		Shape(Extensions).Should().Equal(Shape(fromSource));

		Member("Grow").Extension!.DocumentationId.Should().StartWith("T:Fixture.Extensions.ShapeExtensions.<G>$");

		static List<string> Shape(ApiType type) =>
		[
			..type.Members.Where(m => m.Extension is not null).Select(m =>
				$"{m.Extension!.Signature} {m.Extension.DocumentationId} | " +
				$"{m.Extension.Receiver.Name}:{m.Extension.Receiver.Type}:{m.Extension.Receiver.IsRef} | " +
				$"{m.Kind} {m.Signature} | {string.Join(",", m.Parameters.Select(p => $"{p.Type} {p.Name}"))} | " +
				$"{m.Documentation?.Summary} | {m.Extension.Documentation?.Summary}")
		];
	}

	[Fact]
	public async Task HostSite_GroupsTheBlocksAndListsTheirMembersOnReceiverPages()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options => options.Assemblies = [ExtensionFixtureAssembly.Instance]);

		await using ServiceProvider provider = services.BuildServiceProvider();
		InMemorySite site = await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);

		site.Files.Keys.Should().NotContain(path => path.Contains('$') || path.Contains("&lt;"));

		string extensions = Html(site, "api/fixture/extensions/shapeextensions/index.html");
		extensions.Should().Contain("<h2 id=\"extension-shape\">extension(Shape shape)</h2>")
			.And.Contain("<p>Members for any shape.</p>")
			.And.Contain("<tr><td><code>shape</code></td><td><code>Shape</code></td><td>The shape to extend.</td></tr>")
			.And.Contain("<h3 id=\"grow\">Grow(double factor)</h3>")
			.And.Contain("<code class=\"language-csharp\">public Shape Grow(double factor)</code>")
			.And.Contain("<h2 id=\"extension-list-t\">extension&lt;T&gt;(List&lt;T&gt; list)</h2>")
			.And.Contain("<h2 id=\"extension-point\">extension(ref Point point)</h2>")
			.And.NotContain("get_Perimeter");

		// Declaration order, which the compiler's grouping types lose.
		extensions.IndexOf("id=\"extension-list-t\"", StringComparison.Ordinal).Should()
			.BeLessThan(extensions.IndexOf("id=\"extension-shape-2\"", StringComparison.Ordinal));

		string square = Html(site, "api/fixture/extensions/square/index.html");
		square.Should().Contain("<h2 id=\"extension-methods\">Extension Members</h2>")
			.And.Contain("/api/fixture/extensions/shapeextensions#perimeter\" class=\"api-member-link\"><code>Perimeter</code></a>")
			.And.Contain("/api/fixture/extensions/shapeextensions#unit\" class=\"api-member-link\"><code>Unit()</code></a>")
			.And.Contain("/api/fixture/extensions/shapeextensions#create\" class=\"api-member-link\"><code>Create(int)</code></a>")
			.And.Contain("<code>operator +(Shape, Shape)</code>")
			.And.Contain("<code>Describe(Shape)</code>")
			.And.NotContain("#largest\"");

		Html(site, "api/fixture/extensions/point/index.html").Should()
			.Contain("/api/fixture/extensions/shapeextensions#move\" class=\"api-member-link\"><code>Move(int)</code></a>");
	}

	private static string Html(InMemorySite site, string path)
	{
		site.Files.Keys.Should().Contain(path);
		return Encoding.UTF8.GetString(site.Files[path].Content);
	}
}
