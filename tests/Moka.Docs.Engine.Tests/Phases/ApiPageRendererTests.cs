using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     Renders types that the real analyzer extracted, so the documentation IDs are the ones Roslyn writes.
/// </summary>
public sealed class ApiPageRendererTests
{
	private const string _sample = """
	                               using System;
	                               using System.Collections.Generic;

	                               namespace Lib.Money
	                               {
	                                   /// <summary>
	                                   ///     See <see cref="Parse(string, int)" />, <see cref="Amount" />, <see cref="Ledger" />,
	                                   ///     <see cref="Ledger.Post(Money)" />, <see cref="Money(int)" />, <see cref="op_Addition" />,
	                                   ///     <see cref="List{T}" />, <see cref="string.Format(string, object)" /> and <see cref="Missing" />.
	                                   /// </summary>
	                                   public readonly struct Money
	                                   {
	                                       /// <summary>Creates money.</summary>
	                                       public Money(int amount) { Amount = amount; }

	                                       /// <summary>The amount.</summary>
	                                       public int Amount { get; }

	                                       /// <summary>Gets a digit.</summary>
	                                       public int this[int index] => index;

	                                       /// <summary>Parses.</summary>
	                                       public static Money Parse(string s) => new(int.Parse(s));

	                                       /// <summary>Parses in a base.</summary>
	                                       public static Money Parse(string s, int radix) => new(Convert.ToInt32(s, radix));

	                                       /// <summary>Adds.</summary>
	                                       public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);

	                                       /// <summary>Converts.</summary>
	                                       public static implicit operator Money(int amount) => new(amount);

	                                       /// <summary>Groups.</summary>
	                                       public static void Group(Dictionary<string, List<int>> buckets) { }
	                                   }

	                                   /// <summary>A ledger.</summary>
	                                   public class Ledger
	                                   {
	                                       /// <summary>Posts.</summary>
	                                       public void Post(Money money) { }
	                                   }
	                               }
	                               """;

	private static List<ApiType> Analyze(params string[] projects)
	{
		// One compilation per source, the way CSharpAnalysisPhase compiles each configured project.
		var analyzer = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance);
		return projects
			.SelectMany(source => analyzer.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(source)], "Tests").Namespaces)
			.SelectMany(ns => ns.Types)
			.ToList();
	}

	private static string Render(List<ApiType> types, string name) =>
		ApiPageRenderer.RenderType(types.Single(t => t.Name == name), types);

	#region cref links

	[Fact]
	public void SeeCref_LinksToTheTypePageOrTheMemberAnchor()
	{
		// <see cref> became an anchor with a data-cref attribute and no href, so nothing linked.
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("<a href=\"#amount\">Money.Amount</a>")
			.And.Contain("<a href=\"/api/lib/money/ledger\">Ledger</a>")
			.And.Contain("<a href=\"/api/lib/money/ledger#post\">Ledger.Post</a>")
			.And.Contain("<a href=\"#money\">Money</a>")
			.And.Contain("<a href=\"#op_addition\">Money.operator +</a>")
			.And.NotContain("data-cref");
	}

	[Fact]
	public void SeeCref_ToAnOverload_LinksToThatOverload()
	{
		// Overloads shared one anchor id, so the second Parse could not be linked to at all.
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("<a href=\"#parse-2\">Money.Parse</a>")
			.And.Contain("<h3 id=\"parse\">Parse(string s)</h3>")
			.And.Contain("<h3 id=\"parse-2\">Parse(string s, int radix)</h3>");
	}

	[Fact]
	public void SeeCref_ToFrameworkApis_LinksToLearn()
	{
		// System types have no page in the site, so their references were dead text.
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("<a href=\"https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1\">List</a>")
			.And.Contain("<a href=\"https://learn.microsoft.com/dotnet/api/system.string.format\">String.Format</a>");
	}

	[Fact]
	public void SeeCref_ThatDoesNotResolve_StaysText()
	{
		// An unresolved reference looked like a link but went nowhere.
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("and <code>Missing</code>.");
	}

	[Fact]
	public void SeeCref_IntoAnotherProject_LinksWhenTheNameIsUnique()
	{
		// Each project compiles on its own, so a cref to another project's type never bound and never linked.
		List<ApiType> types = Analyze(
			"""
			namespace Lib.Core
			{
			    /// <summary>Settings.</summary>
			    public class Settings
			    {
			        /// <summary>The path.</summary>
			        public string Path { get; set; } = "";
			    }
			}
			""",
			"""
			namespace Lib.App
			{
			    /// <summary>Uses <see cref="Settings" /> and <see cref="Settings.Path" />.</summary>
			    public class Runner { }
			}
			""");

		string html = Render(types, "Runner");

		html.Should().Contain("<a href=\"/api/lib/core/settings\">Settings</a>")
			.And.Contain("<a href=\"/api/lib/core/settings#path\">Settings.Path</a>");
	}

	[Fact]
	public void ExceptionType_LinksLikeACref()
	{
		// The Exceptions table showed the type as code even when it had a page to link to.
		List<ApiType> types = Analyze("""
		                              namespace Lib
		                              {
		                                  public class Store
		                                  {
		                                      /// <exception cref="System.InvalidOperationException">When closed.</exception>
		                                      public void Save() { }
		                                  }
		                              }
		                              """);

		Render(types, "Store").Should().Contain(
			"<td><a href=\"https://learn.microsoft.com/dotnet/api/system.invalidoperationexception\">System.InvalidOperationException</a></td>");
	}

	#endregion

	#region See Also

	[Fact]
	public void SeeAlso_FromAnXmlDocFile_LinksCrefsAndKeepsHrefs()
	{
		// See Also showed "T:Lib.Ledger" as code, and an href entry lost its URL.
		XmlDocBlock doc = new XmlDocParser(NullLogger<XmlDocParser>.Instance).ParseXml("""
			<doc><assembly><name>Lib</name></assembly><members>
			    <member name="T:Lib.Account">
			        <summary>An account.</summary>
			        <seealso cref="T:Lib.Ledger" />
			        <seealso href="https://example.com/guide">Accounting guide</seealso>
			        <seealso href="https://example.com/bare" />
			    </member>
			</members></doc>
			""").Members["T:Lib.Account"];
		var account = new ApiType { Name = "Account", FullName = "Lib.Account", Namespace = "Lib", Kind = ApiTypeKind.Class, Documentation = doc };
		var ledger = new ApiType { Name = "Ledger", FullName = "Lib.Ledger", Namespace = "Lib", Kind = ApiTypeKind.Class };

		string html = ApiPageRenderer.RenderType(account, [account, ledger]);

		html.Should().Contain("<li><a href=\"/api/lib/ledger\">Ledger</a></li>")
			.And.Contain("<li><a href=\"https://example.com/guide\">Accounting guide</a></li>")
			.And.Contain("<li><a href=\"https://example.com/bare\">https://example.com/bare</a></li>")
			.And.NotContain("T:Lib.Ledger");
	}

	[Fact]
	public void SeeAlso_PlainTextEntries_StayEscapedText()
	{
		// The Python plugin stores See Also lines as plain text, which must not turn into markup or links.
		var type = new ApiType
		{
			Name = "Calc",
			FullName = "calc.Calc",
			Kind = ApiTypeKind.Class,
			SourcePath = "calc.py",
			Documentation = new XmlDocBlock
			{
				SeeAlso = ["other_function: <b>does</b> things", "[click](javascript:alert(1))"]
			}
		};

		string html = ApiPageRenderer.RenderType(type, [type]);

		html.Should().Contain("<li><code>other_function: &lt;b&gt;does&lt;/b&gt; things</code></li>")
			.And.Contain("<li>click</li>")
			.And.NotContain("javascript:");
	}

	#endregion

	#region Member names

	[Fact]
	public void OperatorsIndexersAndConstructors_ShowTheirCSharpNames()
	{
		// Tables and headings showed metadata names: op_Addition, op_Implicit and this[](int index).
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("<code>operator +(Money a, Money b)</code>")
			.And.Contain("<h3 id=\"op_implicit\">implicit operator Money(int amount)</h3>")
			.And.Contain("<h3 id=\"this[]\">this[int index]</h3>")
			.And.Contain("<h3 id=\"money\">Money(int amount)</h3>")
			.And.NotContain(">op_Addition")
			.And.NotContain("this[](");
	}

	[Fact]
	public void ParameterTypes_KeepTheirGenericArguments()
	{
		// Types were cut at the last dot, so Dictionary<string, List<int>> showed as "List<int>>",
		// and the heading wasn't escaped.
		string html = Render(Analyze(_sample), "Money");

		html.Should().Contain("<h3 id=\"group\">Group(Dictionary&lt;string, List&lt;int&gt;&gt; buckets)</h3>");
	}

	#endregion

	#region Member details

	[Fact]
	public void MemberWithOnlyRemarksOrExceptions_GetsADetailBlock()
	{
		// A member needed a summary, param or returns for its detail block, so these showed only a table row.
		List<ApiType> types = Analyze("""
		                              namespace Lib
		                              {
		                                  public class Store
		                                  {
		                                      /// <remarks>Flushes first.</remarks>
		                                      public void Close() { }

		                                      /// <exception cref="System.InvalidOperationException">When closed.</exception>
		                                      public void Save() { }

		                                      public void Undocumented() { }
		                                  }
		                              }
		                              """);

		string html = Render(types, "Store");

		html.Should().Contain("<h3 id=\"close\">Close()</h3>")
			.And.Contain("<div class=\"api-remarks\">Flushes first.</div>")
			.And.Contain("<h3 id=\"save\">Save()</h3>")
			.And.Contain("<td><code>Undocumented()</code></td>")
			.And.NotContain("id=\"undocumented\"");
	}

	[Fact]
	public void MemberDetails_RenderRemarksExamplesSeeAlsoTypeParametersAndValue()
	{
		// These tags only rendered on type pages. On members they were dropped.
		List<ApiType> types = Analyze("""
		                              namespace Lib
		                              {
		                                  public class Box
		                                  {
		                                      /// <summary>Maps the box.</summary>
		                                      /// <typeparam name="TOut">The result type.</typeparam>
		                                      /// <remarks>Runs once.</remarks>
		                                      /// <example><code>box.Map(x =&gt; x);</code></example>
		                                      /// <seealso cref="Size" />
		                                      public TOut Map<TOut>() => default!;

		                                      /// <summary>The size.</summary>
		                                      /// <value>Always positive.</value>
		                                      public int Size { get; }
		                                  }
		                              }
		                              """);

		string html = Render(types, "Box");

		html.Should().Contain("<h3 id=\"map\">Map&lt;TOut&gt;()</h3>")
			.And.Contain("<tr><td><code>TOut</code></td><td>-</td><td>The result type.</td></tr>")
			.And.Contain("<div class=\"api-remarks\">Runs once.</div>")
			.And.Contain("<div class=\"api-example\"><pre><code>box.Map(x =&gt; x);</code></pre></div>")
			.And.Contain("<h4>See Also</h4>")
			.And.Contain("<li><a href=\"#size\">Box.Size</a></li>")
			.And.Contain("<p><strong>Value:</strong> Always positive.</p>");
	}

	#endregion

	#region Attributes

	[Fact]
	public void Attributes_AppearAboveTypeAndMemberSignatures()
	{
		// [Obsolete] was the only attribute shown anywhere.
		List<ApiType> types = Analyze("""
		                              using System;
		                              using System.Diagnostics;

		                              namespace Lib
		                              {
		                                  /// <summary>Options.</summary>
		                                  [Flags]
		                                  [DebuggerDisplay("{Value}")]
		                                  public enum Options { None = 0, Fast = 1 }

		                                  /// <summary>Retries a call.</summary>
		                                  public sealed class RetryAttribute(int times) : Attribute { }

		                                  public class Store
		                                  {
		                                      /// <summary>Saves.</summary>
		                                      [Retry(3)]
		                                      [DebuggerStepThrough]
		                                      [Obsolete("Use SaveAsync.")]
		                                      public void Save() { }
		                                  }
		                              }
		                              """);

		// The whole code element is matched: View Source still shows the attributes that are left out here.
		Render(types, "Options").Should().Contain("<code class=\"language-csharp\">[Flags]\npublic enum Options</code>");

		Render(types, "Store").Should().Contain("<code class=\"language-csharp\">[Retry(3)]\npublic void Save()</code>")
			.And.Contain("<span class=\"api-badge-sm api-badge-obsolete\">obsolete</span>")
			.And.Contain("<div class=\"warning\"><p><strong>Obsolete:</strong> Use SaveAsync.</p></div>");
	}

	[Fact]
	public void PythonDecorators_AppearAsDecorators()
	{
		// The Python plugin's decorators come through as attributes, and "module" marks its module pages.
		var type = new ApiType
		{
			Name = "Point",
			FullName = "shapes.Point",
			Kind = ApiTypeKind.Record,
			SourcePath = "shapes.py",
			Attributes = [new ApiAttribute { Name = "dataclass" }, new ApiAttribute { Name = "module" }]
		};

		string html = ApiPageRenderer.RenderType(type, [type]);

		html.Should().Contain("<code class=\"language-csharp\">@dataclass\npublic record Point</code>")
			.And.NotContain("module")
			.And.NotContain("[dataclass]");
	}

	#endregion
}
