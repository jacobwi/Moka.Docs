using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Diagnostics;
using static Moka.Docs.Integration.Tests.AspNetCore.ReflectionSourceTests;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Namespace filters, <c>[EditorBrowsable(Never)]</c> and XML documentation files in the
///     ASP.NET Core host.
/// </summary>
public sealed class ReflectionFilterTests
{
	private static string[] Namespaces(ApiReference model) => model.Namespaces.Select(ns => ns.Name).ToArray();

	private static string[] TypeNames(ApiReference model) =>
		model.Namespaces.SelectMany(ns => ns.Types).Select(t => t.FullName).ToArray();

	[Fact]
	public void IncludeNamespaces_MatchesTheNamespaceAndTheOnesBelowIt()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb, options => options.IncludeNamespaces = ["Contoso.Api"]);

		Namespaces(model).Should().Equal("Contoso.Api", "Contoso.Api.Models");
	}

	[Fact]
	public void IncludeNamespaces_Empty_DocumentsEverything()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb);

		Namespaces(model).Should().Equal(
			"(global)", "Contoso.Api", "Contoso.Api.Models", "Contoso.Apis", "Contoso.Internal", "Contoso.Widgets");
	}

	[Fact]
	public void ExcludeNamespaces_WinsOverAnInclude()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb, options =>
		{
			options.IncludeNamespaces = ["Contoso.Api", "Contoso.Apis"];
			options.ExcludeNamespaces = ["Contoso.Api.Models"];
		});

		Namespaces(model).Should().Equal("Contoso.Api", "Contoso.Apis");
	}

	[Fact]
	public void ExcludeNamespaces_StopsAtASegmentBoundary()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb, options => options.ExcludeNamespaces = ["Contoso.Api"]);

		Namespaces(model).Should().Equal("(global)", "Contoso.Apis", "Contoso.Internal", "Contoso.Widgets");
	}

	[Theory]
	[InlineData("(global)")]
	[InlineData("")]
	public void GlobalNamespace_IsAddressedAsGlobalOrEmpty(string entry)
	{
		Namespaces(Build(SourceFixtureAssembly.NoPdb, options => options.IncludeNamespaces = [entry]))
			.Should().Equal("(global)");
		Namespaces(Build(SourceFixtureAssembly.NoPdb, options => options.ExcludeNamespaces = [entry]))
			.Should().NotContain("(global)").And.HaveCount(5);
	}

	[Fact]
	public void NamespaceEntry_WithAStar_IsAWarning()
	{
		var diagnostics = new DiagnosticBag();
		Build(SourceFixtureAssembly.NoPdb, options => options.IncludeNamespaces = ["Contoso.*"], diagnostics);

		Diagnostic warning = diagnostics.All.Should().ContainSingle().Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Contain("IncludeNamespaces entry 'Contoso.*'").And.Contain("not a wildcard");
	}

	[Fact]
	public void NamespaceEntry_ThatMatchesNoNamespace_IsAWarning()
	{
		var diagnostics = new DiagnosticBag();
		Build(SourceFixtureAssembly.NoPdb, options =>
		{
			options.IncludeNamespaces = ["Contoso.Api", "contoso.widgets"];
			options.ExcludeNamespaces = ["Contoso.Api.Nothing", "(global)"];
		}, diagnostics);

		diagnostics.All.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
		diagnostics.All.Select(d => d.Message).Should().SatisfyRespectively(
			first => first.Should().StartWith("IncludeNamespaces entry 'contoso.widgets' matches no namespace"),
			second => second.Should().StartWith("ExcludeNamespaces entry 'Contoso.Api.Nothing' matches no namespace"));
	}

	[Fact]
	public void ExcludedBaseType_StillPassesItsDocumentationOn()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb, options => options.ExcludeNamespaces = ["Contoso.Internal"]);

		Type(model, "Order").Members.Single(m => m.Name == "Describe").Documentation!.Summary
			.Should().Be("Describes the entity.");
	}

	[Fact]
	public void EditorBrowsableNever_HidesTypesTheirNestedTypesAndMembers()
	{
		ApiReference model = Build(SourceFixtureAssembly.NoPdb);

		TypeNames(model).Should().NotContain(name => name.Contains("HiddenEndpoint"));
		Type(model, "Widget").Members.Should().NotContain(m => m.Name == "Legacy");
		Type(model, "Widget").Members.Should().Contain(m => m.Name == "Spin");
		Type(model, "WidgetKind").Members.Select(m => m.Name).Should().Equal("Round");
	}

	[Fact]
	public async Task FilteredTypes_GetNoPagesNavigationOrLinks()
	{
		InMemorySite site = await SiteHost.BuildAsync(SourceFixtureAssembly.NoPdb, options =>
		{
			options.ExcludeNamespaces = ["Contoso.Internal"];
			options.Nav = [new NavEntry { Label = "API Reference", Path = "/api", AutoGenerate = true }];
		});

		site.Files.Keys.Should().NotContain(key => key.StartsWith("api/contoso/internal", StringComparison.Ordinal));
		site.Files.Keys.Should().NotContain(key => key.Contains("hiddenendpoint", StringComparison.Ordinal));
		string order = SiteHost.Html(site, "api/contoso/api/models/order/index.html");
		order.Should().Contain("EntityBase");
		order.Should().NotContain("/api/contoso/internal");
		SiteHost.Html(site, "api/index.html").Should().NotContain("/api/contoso/internal").And.NotContain(">EntityBase</a>");
		SiteHost.Html(site, "search-index.json").Should().NotContain("/api/contoso/internal");

		// A cref to an included type does become a link, so the absence above means something.
		InMemorySite unfiltered = await SiteHost.BuildAsync(SourceFixtureAssembly.NoPdb);
		SiteHost.Html(unfiltered, "api/contoso/api/models/order/index.html").Should().Contain("/docs/api/contoso/internal/entitybase");
	}

	[Fact]
	public async Task MissingXmlFile_IsAWarningThatNamesThePath()
	{
		InMemorySite site = await SiteHost.BuildAsync(SourceFixtureAssembly.Embedded);
		string expectedPath = Path.ChangeExtension(SourceFixtureAssembly.Embedded.Location, ".xml");

		Diagnostic warning = site.Diagnostics.Should()
			.ContainSingle(d => d.Source == ReflectionApiModelBuilder.DiagnosticSource).Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Contain("'Moka.Docs.SourceFixture.Embedded'")
			.And.Contain(expectedPath)
			.And.Contain("<GenerateDocumentationFile>true</GenerateDocumentationFile>")
			.And.Contain("XmlDocFiles");
	}

	[Fact]
	public async Task MissingXmlFile_IsNotReportedWhenXmlDocsAreOff()
	{
		InMemorySite site = await SiteHost.BuildAsync(SourceFixtureAssembly.Embedded, options => options.IncludeXmlDocs = false);

		site.Diagnostics.Should().NotContain(d => d.Source == ReflectionApiModelBuilder.DiagnosticSource);
	}

	[Fact]
	public async Task XmlDocFiles_AreMatchedByAssemblyNameAndResolveFromTheContentRoot()
	{
		// Named after nothing in particular, so only the <assembly><name> element can match it.
		InMemorySite site = await SiteHost.BuildAsync(
			SourceFixtureAssembly.Embedded,
			options => options.XmlDocFiles = ["packages/docs.xml"],
			root =>
			{
				Directory.CreateDirectory(Path.Combine(root, "packages"));
				File.Copy(SourceFixtureAssembly.EmbeddedXmlPath, Path.Combine(root, "packages", "docs.xml"));
			});

		site.Diagnostics.Should().NotContain(d => d.Source == ReflectionApiModelBuilder.DiagnosticSource);
		SiteHost.Html(site, "api/contoso/widgets/widget/index.html").Should().Contain("Spins the widget.");
	}

	[Fact]
	public void XmlDocFiles_ForAnotherAssembly_IsAWarning()
	{
		var diagnostics = new DiagnosticBag();
		ApiReference model = Build(SourceFixtureAssembly.NoPdb,
			options => options.XmlDocFiles = [SourceFixtureAssembly.EmbeddedXmlPath], diagnostics);

		diagnostics.All.Should().ContainSingle().Which.Message.Should()
			.Contain("'Moka.Docs.SourceFixture.Embedded'").And.Contain("not in Assemblies");
		// The XML file next to the DLL is still read.
		Type(model, "Widget").Documentation!.Summary.Should().Be("A widget.");
	}

	[Fact]
	public void UnreadableXmlFile_NextToTheAssembly_IsAWarningWithTheReason()
	{
		Assembly assembly = FixtureAssembly.Load(
			FixtureAssembly.CreateCompilation("Moka.Docs.BrokenXmlFixture", "/// <summary>A.</summary>\npublic class A { }",
				LanguageVersion.Latest),
			"broken-xml-fixture");
		string xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
		File.WriteAllText(xmlPath, "<doc><assembly><name>Moka.Docs.BrokenXmlFixture</name></assembly><members>");

		var diagnostics = new DiagnosticBag();
		Build(assembly, configure: null, diagnostics);

		Diagnostic warning = diagnostics.All.Should().ContainSingle().Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Contain(xmlPath).And.Contain("'Moka.Docs.BrokenXmlFixture'").And.Contain("could not be read");
	}

	[Fact]
	public void UnreadableXmlDocFilesEntry_IsAWarningWithTheReason()
	{
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-broken-" + Guid.NewGuid().ToString("N") + ".xml");
		File.WriteAllText(path, "<doc><assembly><name>Moka.Docs.SourceFixture.NoPdb</name></assembly><members><member>");
		try
		{
			var diagnostics = new DiagnosticBag();
			Build(SourceFixtureAssembly.NoPdb, options => options.XmlDocFiles = [path], diagnostics);

			diagnostics.All.Should().ContainSingle().Which.Message.Should()
				.Contain(path).And.Contain("could not be read, so it matches no assembly: ");
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void XmlDocFiles_MissingFile_IsAWarning()
	{
		var diagnostics = new DiagnosticBag();
		Build(SourceFixtureAssembly.NoPdb, options => options.XmlDocFiles = ["no/such/file.xml"], diagnostics);

		diagnostics.All.Should().ContainSingle().Which.Message.Should().Contain("no/such/file.xml").And.Contain("does not exist");
	}
}
