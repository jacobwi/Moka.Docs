using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using MetadataReference = Microsoft.CodeAnalysis.MetadataReference;
using Microsoft.CodeAnalysis.CSharp;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Diagnostics;
using static Moka.Docs.Integration.Tests.AspNetCore.ReflectionSourceTests;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     An assembly whose types and members need a dependency that isn't deployed, as when a package is
///     referenced for compiling only.
/// </summary>
public sealed class ReflectionMissingDependencyTests
{
	private const string _dependencyName = "Moka.Docs.MissingDependencyFixture.Dependency";

	private const string _dependencySource = """
		namespace Dependency
		{
		    public class DepBase { }
		    public interface IDepService { }
		    public class DepType { }
		    public sealed class DepAttribute : System.Attribute { }
		}
		""";

	private const string _source = """
		namespace MainNs
		{
		    /// <summary>Needs nothing.</summary>
		    public class Fine
		    {
		        /// <summary>A number.</summary>
		        public int Number { get; set; }

		        private void Hidden(Dependency.DepType value) { }
		    }

		    public class Derived : Dependency.DepBase { }

		    public class Implements : Dependency.IDepService { }

		    public class UsesParameter
		    {
		        public void Take(Dependency.DepType value) { }
		        public void Keep() { }
		    }

		    public class UsesProperty
		    {
		        public Dependency.DepType? Value { get; set; }
		        public int Kept { get; set; }
		    }

		    [Dependency.Dep]
		    public class AttributedType { }

		    public class AttributedMember
		    {
		        [Dependency.Dep]
		        public void Marked() { }
		        public void Plain() { }
		    }

		    public class Outer<T>
		    {
		        public class Inner : Dependency.DepBase { }
		        public class Other { }
		        protected class Guarded { }
		    }
		}
		""";

	private static readonly Lazy<CSharpCompilation> _compilation = new(() =>
	{
		// Never written to disk, so nothing can load it at run time.
		MetadataReference dependency = FixtureAssembly
			.CreateCompilation(_dependencyName, _dependencySource, LanguageVersion.Latest)
			.ToMetadataReference();
		return FixtureAssembly.CreateCompilation("Moka.Docs.MissingDependencyFixture", _source, LanguageVersion.Latest, [dependency]);
	});

	private static readonly Lazy<Assembly> _fromFile = new(() => FixtureAssembly.Load(_compilation.Value, "missing-dependency-fixture"));

	private static readonly Lazy<Assembly> _fromBytes = new(() =>
	{
		using var image = new MemoryStream();
		_compilation.Value.Emit(image).Success.Should().BeTrue();
		image.Position = 0;
		return new AssemblyLoadContext("missing-dependency-bytes").LoadFromStream(image);
	});

	private static string[] TypeNames(ApiReference model) =>
		model.Namespaces.SelectMany(ns => ns.Types).Select(t => t.FullName).Order(StringComparer.Ordinal).ToArray();

	private static string[] MemberNames(ApiReference model, string typeName) =>
		Type(model, typeName).Members.Select(m => m.Name).Order(StringComparer.Ordinal).ToArray();

	private static string[] Warnings(DiagnosticBag diagnostics) =>
		diagnostics.All
			.Where(d => d.Source == ReflectionApiModelBuilder.DiagnosticSource && d.Severity == DiagnosticSeverity.Warning)
			.Select(d => d.Message)
			.ToArray();

	[Fact]
	public void TypesThatCannotLoad_AreLeftOutAndTheRestIsDocumented()
	{
		var diagnostics = new DiagnosticBag();
		ApiReference model = Build(_fromFile.Value, options => options.IncludeXmlDocs = true, diagnostics);

		TypeNames(model).Should().Equal(
			"MainNs.AttributedMember", "MainNs.Fine", "MainNs.Outer", "MainNs.Outer.Guarded", "MainNs.Outer.Other", "MainNs.UsesParameter",
			"MainNs.UsesProperty");
		Type(model, "Fine").Documentation!.Summary.Should().Be("Needs nothing.");

		string[] warnings = Warnings(diagnostics);
		warnings.Should().HaveCount(7).And.OnlyContain(w => w.Contains(_dependencyName, StringComparison.Ordinal));
		warnings.Should().ContainSingle(w => w.StartsWith("Type 'MainNs.Derived' in assembly 'Moka.Docs.MissingDependencyFixture'"));
		warnings.Should().ContainSingle(w => w.StartsWith("Type 'MainNs.Implements' "));
		warnings.Should().ContainSingle(w => w.StartsWith("Type 'MainNs.Outer<T>.Inner' "));
		warnings.Should().ContainSingle(w => w.StartsWith("Type 'MainNs.AttributedType' "));
	}

	[Fact]
	public void MembersThatCannotLoad_AreLeftOutAndTheirTypeIsKept()
	{
		var diagnostics = new DiagnosticBag();
		ApiReference model = Build(_fromFile.Value, configure: null, diagnostics);

		MemberNames(model, "UsesParameter").Should().Equal("Keep", "UsesParameter");
		MemberNames(model, "UsesProperty").Should().Equal("Kept", "UsesProperty");
		MemberNames(model, "AttributedMember").Should().Equal("AttributedMember", "Plain");

		string[] warnings = Warnings(diagnostics);
		warnings.Should().ContainSingle(w => w.StartsWith("Member 'MainNs.UsesParameter.Take': left out of the API reference."));
		warnings.Should().ContainSingle(w => w.StartsWith("Member 'MainNs.UsesProperty.Value': "));
		warnings.Should().ContainSingle(w => w.StartsWith("Member 'MainNs.AttributedMember.Marked': "));
		warnings.Should().NotContain(w => w.Contains("Hidden", StringComparison.Ordinal));
	}

	[Fact]
	public void AssemblyLoadedFromBytes_KeepsTheTypesThatLoad()
	{
		var diagnostics = new DiagnosticBag();
		ApiReference model = Build(_fromBytes.Value, configure: null, diagnostics);

		TypeNames(model).Should().Contain(["MainNs.Fine", "MainNs.UsesParameter"])
			.And.NotContain(["MainNs.Derived", "MainNs.Implements"]);
		Warnings(diagnostics).Should().Contain(w =>
			w.StartsWith("Some types in assembly 'Moka.Docs.MissingDependencyFixture': left out of the API reference.")
			&& w.Contains(_dependencyName, StringComparison.Ordinal));
	}

	[Fact]
	public async Task Site_BuildsWithTheTypesThatLoadAndListsTheWarnings()
	{
		InMemorySite site = await SiteHost.BuildAsync(_fromFile.Value);

		SiteHost.Html(site, "api/mainns/fine/index.html").Should().Contain("Needs nothing.");
		site.Files.Keys.Should().NotContain(key => key.StartsWith("api/mainns/derived", StringComparison.Ordinal));
		site.Diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning
		                                       && d.Message.StartsWith("Type 'MainNs.Derived' ", StringComparison.Ordinal));
		site.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
	}
}
