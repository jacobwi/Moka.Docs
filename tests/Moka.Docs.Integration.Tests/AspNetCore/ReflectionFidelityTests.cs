using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.CSharp.Metadata;
using static Moka.Docs.Integration.Tests.AspNetCore.ReflectionSourceTests;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Declarations the reflection model used to get wrong, checked against what the CLI's Roslyn
///     analysis prints for the same source.
/// </summary>
public sealed class ReflectionFidelityTests
{
	private const string _source = """
		using System;
		using System.ComponentModel;

		namespace Fidelity
		{
		    /// <summary>A ref struct.</summary>
		    public ref struct Cursor
		    {
		        /// <summary>A span.</summary>
		        public Span<int> Items;
		        /// <summary>A ref field.</summary>
		        public ref int Current;
		        /// <summary>A readonly ref field.</summary>
		        public readonly ref readonly int Pinned;
		    }

		    /// <summary>Has required members.</summary>
		    public class Settings
		    {
		        /// <summary>Creates settings.</summary>
		        public Settings() { }
		        /// <summary>The name.</summary>
		        public required string Name { get; init; }
		    }

		    /// <summary>Has an obsolete constructor.</summary>
		    public class Legacy
		    {
		        /// <summary>The old constructor.</summary>
		        [Obsolete("Use Legacy(int).")]
		        public Legacy() { }
		        /// <summary>The new constructor.</summary>
		        public Legacy(int size) { }
		    }

		    /// <summary>Takes content types.</summary>
		    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
		    public sealed class ProducesAttribute : Attribute
		    {
		        /// <summary>Creates the attribute.</summary>
		        public ProducesAttribute(string contentType, params string[] additional) { }
		        /// <summary>Types.</summary>
		        public Type[]? Types { get; set; }
		        /// <summary>Anything.</summary>
		        public object? Value { get; set; }
		    }

		    /// <summary>Attributed.</summary>
		    [Produces("application/json", "text/plain", Types = new[] { typeof(int), typeof(System.Collections.Generic.List<string>) },
		        Value = AttributeTargets.Class)]
		    [EditorBrowsable(EditorBrowsableState.Advanced)]
		    public class Attributed { }

		    /// <summary>Indexers.</summary>
		    public class Lookup : IDisposable
		    {
		        void IDisposable.Dispose() { }
		        /// <summary>An indexer.</summary>
		        public int this[int index, string key] => 0;
		        /// <summary>An event.</summary>
		        public event EventHandler? Changed;
		    }

		    /// <summary>Constants.</summary>
		    public class Constants
		    {
		        /// <summary>A decimal.</summary>
		        public const decimal Rate = 1.5m;
		        /// <summary>The largest decimal.</summary>
		        public const decimal Largest = 79228162514264337593543950335m;
		    }

		    /// <summary>A fixed buffer.</summary>
		    public unsafe struct Buffer
		    {
		        /// <summary>The bytes.</summary>
		        public fixed byte Bytes[16];
		    }

		    /// <summary>Function pointers.</summary>
		    public unsafe class Pointers
		    {
		        /// <summary>Calls back.</summary>
		        public static void Call(delegate*<int, void> callback) { }
		        /// <summary>Native.</summary>
		        public static delegate* unmanaged[Cdecl]<int> Native() => null;
		        /// <summary>Annotated.</summary>
		        public static delegate*<ref int, out string, string?> Annotated() => null;
		        /// <summary>Mixed.</summary>
		        public static void Mixed(delegate*<string?, string> first, delegate*<int, string?>[] rest) { }
		        /// <summary>A field.</summary>
		        public delegate*<int> Field;
		    }

		    /// <summary>A record struct.</summary>
		    public record struct Pair(int Left, int Right);

		    /// <summary>A readonly record struct.</summary>
		    public readonly record struct Point(int X, int Y);

		    /// <summary>A generic record struct.</summary>
		    public record struct Box<T>(T Value);

		    /// <summary>A struct with its own equality operators.</summary>
		    public struct Plain
		    {
		        /// <summary>Equal.</summary>
		        public static bool operator ==(Plain left, Plain right) => true;
		        /// <summary>Not equal.</summary>
		        public static bool operator !=(Plain left, Plain right) => false;
		        /// <inheritdoc />
		        public override bool Equals(object? obj) => true;
		        /// <inheritdoc />
		        public override int GetHashCode() => 0;
		    }
		}
		""";

	private static readonly Lazy<CSharpCompilation> _compilation = new(() =>
		FixtureAssembly.CreateCompilation("Moka.Docs.FidelityFixture", _source, LanguageVersion.Latest, allowUnsafe: true));

	private static readonly Lazy<Assembly> _assembly = new(() => FixtureAssembly.Load(_compilation.Value, "fidelity-fixture"));

	private static readonly Lazy<ApiReference> _reflection = new(() => Build(_assembly.Value));

	private static readonly Lazy<ApiReference> _roslyn = new(() =>
		new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance).AnalyzeCompilation(_compilation.Value, "Moka.Docs.FidelityFixture"));

	private static ApiMember Member(ApiReference model, string typeName, string memberName) =>
		Type(model, typeName).Members.Single(m => m.Name == memberName);

	private static string Signature(ApiReference model, string typeName, string memberName) =>
		Member(model, typeName, memberName).Signature;

	[Theory]
	[InlineData("Cursor", "Current")]
	[InlineData("Cursor", "Pinned")]
	[InlineData("Lookup", "this[]")]
	[InlineData("Constants", "Rate")]
	[InlineData("Constants", "Largest")]
	[InlineData("Pointers", "Call")]
	[InlineData("Pointers", "Native")]
	[InlineData("Pointers", "Annotated")]
	[InlineData("Pointers", "Mixed")]
	[InlineData("Pointers", "Field")]
	public void Signature_MatchesTheCli(string typeName, string memberName)
	{
		Signature(_reflection.Value, typeName, memberName).Should().Be(Signature(_roslyn.Value, typeName, memberName));
	}

	[Fact]
	public void Signatures_ReadLikeTheDeclarations()
	{
		Signature(_reflection.Value, "Cursor", "Pinned").Should().Be("public readonly ref readonly int Pinned");
		Signature(_reflection.Value, "Lookup", "this[]").Should().Be("public int this[int index, string key] { get; }");
		Member(_reflection.Value, "Lookup", "this[]").Parameters.Select(p => p.IsNullable).Should().Equal(false, false);
		Signature(_reflection.Value, "Constants", "Rate").Should().Be("public const decimal Rate = 1.5");
		Signature(_reflection.Value, "Pointers", "Native").Should().Be("public static delegate* unmanaged[Cdecl]<int> Native()");
		Signature(_reflection.Value, "Pointers", "Annotated").Should().Be("public static delegate*<ref int, out string, string?> Annotated()");
		Member(_reflection.Value, "Pointers", "Call").Documentation!.Summary.Should().Be("Calls back.");
	}

	[Fact]
	public void FixedBuffer_IsDeclaredWithItsLength()
	{
		ApiMember bytes = Member(_reflection.Value, "Buffer", "Bytes");

		bytes.Signature.Should().Be("public fixed byte Bytes[16]");
		bytes.ReturnType.Should().Be(Member(_roslyn.Value, "Buffer", "Bytes").ReturnType).And.Be("byte*");
		bytes.Attributes.Should().BeEmpty();
	}

	[Theory]
	[InlineData("Pair")]
	[InlineData("Point")]
	[InlineData("Box")]
	public void RecordStruct_IsARecord(string typeName)
	{
		ApiType type = Type(_reflection.Value, typeName);

		type.IsRecord.Should().BeTrue();
		type.Kind.Should().Be(ApiTypeKind.Record).And.Be(Type(_roslyn.Value, typeName).Kind);
		Type(_reflection.Value, "Plain").IsRecord.Should().BeFalse();
	}

	[Fact]
	public void RefStruct_AndRequiredMembersConstructor_AreNotObsolete()
	{
		ApiType cursor = Type(_reflection.Value, "Cursor");
		ApiMember settingsConstructor = Member(_reflection.Value, "Settings", "Settings");

		cursor.IsObsolete.Should().BeFalse();
		cursor.Attributes.Should().BeEmpty();
		settingsConstructor.IsObsolete.Should().BeFalse();
		settingsConstructor.Attributes.Should().BeEmpty();
		Type(_reflection.Value, "Settings").Attributes.Should().BeEmpty();
	}

	[Fact]
	public void ObsoleteConstructor_IsMarked()
	{
		ApiMember[] constructors = Type(_reflection.Value, "Legacy").Members
			.Where(m => m.Kind == ApiMemberKind.Constructor)
			.OrderBy(m => m.Parameters.Count)
			.ToArray();

		constructors[0].IsObsolete.Should().BeTrue();
		constructors[0].ObsoleteMessage.Should().Be("Use Legacy(int).");
		constructors[1].IsObsolete.Should().BeFalse();
	}

	[Fact]
	public void AttributeArguments_ReadLikeSource()
	{
		ApiAttribute[] attributes = Type(_reflection.Value, "Attributed").Attributes.ToArray();

		attributes.Select(a => $"{a.Name}({string.Join(", ", a.Arguments)})").Should().Equal(
			"Produces(\"application/json\", \"text/plain\", Types = new Type[] { typeof(int), typeof(List<string>) }, Value = AttributeTargets.Class)",
			"EditorBrowsable(EditorBrowsableState.Advanced)");
		Type(_reflection.Value, "ProducesAttribute").Attributes.Single().Arguments
			.Should().Equal("AttributeTargets.Class | AttributeTargets.Method");
	}

	[Fact]
	public void SameAssemblyTwice_IsDocumentedOnce()
	{
		ApiReference model = Build(_assembly.Value, options => options.Assemblies = [_assembly.Value, _assembly.Value]);

		model.Assemblies.Should().Equal("Moka.Docs.FidelityFixture");
		model.Namespaces.SelectMany(ns => ns.Types).Count(t => t.Name == "Legacy").Should().Be(1);
	}

	[Fact]
	public async Task Pages_ShowTheRealObsoleteAndNotTheCompilersMarker()
	{
		InMemorySite site = await SiteHost.BuildAsync(_assembly.Value);

		string cursor = SiteHost.Html(site, "api/fidelity/cursor/index.html");
		cursor.Should().NotContain("api-badge-obsolete").And.NotContain("not supported in this version of your compiler");
		SiteHost.Html(site, "api/fidelity/legacy/index.html").Should().Contain("Use Legacy(int).");
	}

	[Fact]
	public void SourceLinkMap_SkipsOnlyTheEntryThatIsNotAString()
	{
		SourceLinkMap map = SourceLinkMap.Parse("""
		                                        {
		                                          "documents": {
		                                            "/other/*": 1,
		                                            "/nested/*": { "url": "https://example.com/*" },
		                                            "/_/*": "https://raw.githubusercontent.com/o/r/c/*"
		                                          }
		                                        }
		                                        """)!;

		map.Should().NotBeNull();
		map.GetUrl("/_/src/A.cs").Should().Be("https://raw.githubusercontent.com/o/r/c/src/A.cs");
		map.GetUrl("/other/A.cs").Should().BeNull();
	}

	[Fact]
	public void DiagnosticsAreClean_ForAWellFormedAssembly()
	{
		var diagnostics = new DiagnosticBag();
		Build(_assembly.Value, configure: null, diagnostics);

		diagnostics.All.Should().BeEmpty();
	}
}
