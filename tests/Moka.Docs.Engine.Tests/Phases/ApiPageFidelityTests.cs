using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Search;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     Type pages built from analyzed source: comment text that can't inject markup, links to nested and
///     generic types, the type graph and the declaration line.
/// </summary>
public sealed class ApiPageFidelityTests
{
	private const string _source = """
	                               using System;

	                               namespace Lib
	                               {
	                                   /// <summary>
	                                   ///     Money. Uses <paramref name="&lt;img src=x onerror=alert(1)&gt;"/>, see
	                                   ///     <see href="java&#9;script:alert(3)">this</see>.
	                                   /// </summary>
	                                   /// <seealso href="java&#9;script:alert(4)">Run</seealso>
	                                   /// <seealso href="https://example.com/money">Guide</seealso>
	                                   public readonly record struct Money(decimal Amount) : IComparable<Money>
	                                   {
	                                       /// <summary>Compares with <typeparamref name="&lt;b&gt;bold&lt;/b&gt;"/>.</summary>
	                                       public int CompareTo(Money other) => 0;
	                                   }

	                                   /// <summary>Links to <see cref="Outer.Options"/>, <see cref="Other.Options"/> and <see cref="Result{T}"/>.</summary>
	                                   public class Linker { }

	                                   /// <summary>Outer.</summary>
	                                   public class Outer
	                                   {
	                                       /// <summary>Outer's options.</summary>
	                                       public class Options { }
	                                   }

	                                   /// <summary>Other.</summary>
	                                   public class Other
	                                   {
	                                       /// <summary>Other's options.</summary>
	                                       public class Options { }
	                                   }

	                                   /// <summary>A result.</summary>
	                                   public class Result { }

	                                   /// <summary>A result with a value.</summary>
	                                   public class Result<T> : Result { }

	                                   /// <summary>A repository.</summary>
	                                   public class Repository<T> { }

	                                   /// <summary>Users.</summary>
	                                   public class UserRepository : Repository<string>
	                                   {
	                                       /// <summary>Finds a user.</summary>
	                                       public string Find(int id) => "";

	                                       /// <summary>Finds a user by name.</summary>
	                                       public string Find(string name) => "";

	                                       public int Count { get; }
	                                   }
	                               }
	                               """;

	private static readonly List<ApiType> _types = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance)
		.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(_source)], "Lib")
		.Namespaces.SelectMany(ns => ns.Types)
		.ToList();

	private static ApiType Type(string fullName) => _types.Single(t => t.FullName == fullName);

	private static string Render(string fullName) => ApiPageRenderer.RenderType(Type(fullName), _types);

	[Fact]
	public void DocCommentPayloads_RenderInert_OnThePageAndInTheIndex()
	{
		string page = Render("Lib.Money");
		string index = ApiPageRenderer.RenderSummary(Type("Lib.Money"), _types);

		foreach (string html in new[] { page, index })
		{
			html.Should().NotContain("<img").And.NotContain("<b>bold").And.NotContain("href=\"java");
			html.Should().Contain("<code>&lt;img src=x onerror=alert(1)&gt;");
		}

		// Roslyn repeats an escaped name attribute's text; what matters is that it stays text.
		page.Should().Contain("<code>&lt;b&gt;bold&lt;/b&gt;")
			.And.Contain("<li>Run</li>")
			.And.Contain("<li><a href=\"https://example.com/money\">Guide</a></li>");
	}

	[Fact]
	public void DeclarationLine_ComesFromTheSourceDeclaration()
	{
		Render("Lib.Money").Should()
			.Contain("<code class=\"language-csharp\">public readonly record struct Money(decimal Amount) : System.IComparable&lt;Lib.Money&gt;</code>");
	}

	[Fact]
	public void Crefs_ToNestedAndGenericTypes_LinkToTheirOwnPages()
	{
		Render("Lib.Linker").Should()
			.Contain("<a href=\"/api/lib/outer.options\">Outer.Options</a>")
			.And.Contain("<a href=\"/api/lib/other.options\">Other.Options</a>")
			.And.Contain("<a href=\"/api/lib/result-1\">Result&lt;T&gt;</a>");
	}

	[Fact]
	public void TypeGraph_GenericTypes_GetLabelledNodes()
	{
		// IComparable<Lib.Money> became a node called "Money~", so Money seemed to implement itself.
		string page = Render("Lib.Money");

		page.Should().Contain("class T0[\"Money\"]")
			.And.Contain("class T2[\"IComparable#lt;Money#gt;\"]")
			.And.Contain("T0 ..|> T2 : implements")
			.And.Contain("style T0 fill:")
			.And.NotContain("~");
	}

	[Fact]
	public void TypeGraph_DerivedTypesOfAGenericBase_AreFound()
	{
		Render("Lib.Repository<T>").Should()
			.Contain("class T0[\"Repository#lt;T#gt;\"]")
			.And.Contain("class T1[\"UserRepository\"]")
			.And.Contain("T1 --|> T0 : inherits");
	}

	[Fact]
	public void MemberSearchEntries_OnePerMember_WithItsSummaryAndAnchor()
	{
		IReadOnlyList<SearchEntry> entries =
			ApiPageRenderer.BuildMemberSearchEntries(Type("Lib.UserRepository"), "/api/lib/userrepository", "UserRepository");

		entries.Select(e => (e.Section, e.Route, e.Content)).Should().Equal(
			("Count", "/api/lib/userrepository#properties", ""),
			("Find(int id)", "/api/lib/userrepository#find", "Finds a user."),
			("Find(string name)", "/api/lib/userrepository#find-2", "Finds a user by name."));
		entries.Should().OnlyContain(e => e.Title == "UserRepository" && e.Category == "API Reference");
	}
}
