using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

/// <summary>
///     Declarations, default values, attributes and doc comments read the way the source writes them.
/// </summary>
public sealed class AnalyzerFidelityTests : IDisposable
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-fidelity-" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private ApiReference Analyze(string source) =>
		_analyzer.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(source, path: "Source.cs")], "Lib");

	private static ApiType Type(ApiReference api, string name) =>
		api.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == name);

	[Fact]
	public void TypeDeclarations_ReadLikeTheSource()
	{
		ApiReference api = Analyze("""
		                           using System;

		                           namespace Lib;

		                           public readonly record struct Money(decimal Amount, string Currency);

		                           public record struct Point(int X, int Y);

		                           public sealed record Person(string Name, int Age = 30) : IComparable<Person>
		                           {
		                               public int CompareTo(Person other) => 0;
		                           }

		                           public struct Plain { }

		                           public ref struct Window { }

		                           public enum Small : byte { A }

		                           public delegate TResult Transform<in T, out TResult>(T input, int count = 2);

		                           public interface IProducer<out T> { }

		                           public abstract class Shape : IDisposable
		                           {
		                               public void Dispose() { }
		                           }
		                           """);

		Type(api, "Money").Signature.Should().Be("public readonly record struct Money(decimal Amount, string Currency)");
		Type(api, "Point").Signature.Should().Be("public record struct Point(int X, int Y)");
		Type(api, "Person").Signature.Should().Be("public sealed record Person(string Name, int Age = 30) : System.IComparable<Lib.Person>");
		Type(api, "Plain").Signature.Should().Be("public struct Plain");
		Type(api, "Window").Signature.Should().Be("public ref struct Window");
		Type(api, "Small").Signature.Should().Be("public enum Small : byte");
		Type(api, "Transform").Signature.Should().Be("public delegate TResult Transform<in T, out TResult>(T input, int count = 2)");
		Type(api, "IProducer").Signature.Should().Be("public interface IProducer<out T>");
		Type(api, "Shape").Signature.Should().Be("public abstract class Shape : System.IDisposable");
	}

	[Fact]
	public void DefaultValues_AreWrittenAsTheSourceWritesThem()
	{
		ApiReference api = Analyze("""
		                           using System;
		                           using System.Threading;

		                           namespace Lib;

		                           public enum Mode { Slow, Fast }

		                           public class Scheduler
		                           {
		                               public void Run(DayOfWeek day = DayOfWeek.Friday, double limit = double.NaN, string name = null,
		                                   CancellationToken token = default, Mode mode = Mode.Fast, char letter = 'x', int count = -1) { }
		                           }
		                           """);

		ApiMember run = Type(api, "Scheduler").Members.Single(m => m.Name == "Run");

		run.Signature.Should().Be("public void Run(DayOfWeek day = DayOfWeek.Friday, double limit = double.NaN, string name = null, "
		                          + "CancellationToken token = default, Mode mode = Mode.Fast, char letter = 'x', int count = -1)");
		run.Parameters.Select(p => p.DefaultValue).Should()
			.Equal("DayOfWeek.Friday", "double.NaN", "null", "default", "Mode.Fast", "'x'", "-1");
	}

	[Fact]
	public void AttributeNamedArguments_AreKept()
	{
		ApiReference api = Analyze("""
		                           using System;

		                           namespace Lib;

		                           [AttributeUsage(AttributeTargets.All)]
		                           public sealed class TagAttribute(string name) : Attribute
		                           {
		                               public int Order { get; set; }
		                           }

		                           [Tag("x", Order = 2)]
		                           public class Tagged { }
		                           """);

		Type(api, "Tagged").Attributes.Single(a => a.Name == "Tag").Arguments.Should().Equal("\"x\"", "Order = 2");
	}

	[Fact]
	public void EditorBrowsableNever_IsLeftOutLikeTheHostDoes()
	{
		ApiReference api = Analyze("""
		                           using System.ComponentModel;

		                           namespace Lib;

		                           public class Visible
		                           {
		                               [EditorBrowsable(EditorBrowsableState.Never)]
		                               public void Hidden() { }

		                               [EditorBrowsable(EditorBrowsableState.Advanced)]
		                               public void Advanced() { }

		                               public void Shown() { }
		                           }

		                           [EditorBrowsable(EditorBrowsableState.Never)]
		                           public class HiddenType
		                           {
		                               public class NestedInHidden { }
		                           }

		                           public enum Flags
		                           {
		                               A,
		                               [EditorBrowsable(EditorBrowsableState.Never)]
		                               Legacy
		                           }
		                           """);

		api.Namespaces.SelectMany(ns => ns.Types).Select(t => t.Name).Should().BeEquivalentTo("Visible", "Flags");
		Type(api, "Visible").Members.Select(m => m.Name).Should().BeEquivalentTo("Advanced", "Shown");
		Type(api, "Visible").SourceCode.Should().NotContain("Hidden()").And.Contain("Shown()");
		Type(api, "Flags").Members.Select(m => m.Name).Should().Equal("A");
	}

	[Fact]
	public void EditorBrowsableNever_EnumMembers_AreLeftOutOfTheSourcePanel()
	{
		ApiReference api = Analyze("""
		                           using System.ComponentModel;

		                           namespace Lib;

		                           public enum Flags
		                           {
		                               A = 1,
		                               [EditorBrowsable(EditorBrowsableState.Never)]
		                               Old = 2,
		                               B = 4,
		                               [EditorBrowsable(EditorBrowsableState.Never)]
		                               Legacy = 8
		                           }

		                           public class Holder
		                           {
		                               public enum Mode
		                               {
		                                   [EditorBrowsable(EditorBrowsableState.Never)]
		                                   Unset,
		                                   On
		                               }
		                           }
		                           """);

		Type(api, "Flags").SourceCode.Should().Be("""
		                                          public enum Flags
		                                          {
		                                              A = 1,
		                                              B = 4
		                                          }
		                                          """.ReplaceLineEndings("\n"));
		Type(api, "Holder").SourceCode.Should().NotContain("Unset").And.Contain("On");
		Type(api, "Mode").SourceCode.Should().NotContain("Unset").And.Contain("On");
	}

	[Fact]
	public void IncludedDocs_AreExpanded_AndAnUnreadableIncludeIsAWarning()
	{
		Directory.CreateDirectory(_root);
		File.WriteAllText(Path.Combine(_root, "docs.xml"), """
		                                                  <docs>
		                                                    <members name="widget">
		                                                      <Widget>
		                                                        <summary>From the include file.</summary>
		                                                      </Widget>
		                                                    </members>
		                                                  </docs>
		                                                  """);
		File.WriteAllText(Path.Combine(_root, "Widget.cs"), """
		                                                   namespace Lib;

		                                                   /// <include file="docs.xml" path="docs/members[@name='widget']/Widget/*"/>
		                                                   public class Widget { }

		                                                   /// <include file="missing.xml" path="docs/members/Gadget/*"/>
		                                                   public class Gadget { }
		                                                   """);

		ApiReference api = _analyzer.AnalyzeDirectory(_root, "Lib");

		Type(api, "Widget").Documentation!.Summary.Should().Be("From the include file.");
		api.Warnings.Should().ContainSingle()
			.Which.Should().StartWith("Lib.Gadget (Widget.cs:7): ").And.EndWith("missing.xml");
	}

	[Fact]
	public void BadlyFormedDocComment_IsAWarningThatNamesTheMember()
	{
		ApiReference api = Analyze("""
		                           namespace Lib;

		                           /// <summary>Broken <b>bold</summary>
		                           public class Broken
		                           {
		                               /// <summary>Fine.</summary>
		                               public void Run() { }
		                           }
		                           """);

		Type(api, "Broken").Documentation.Should().BeNull();
		Type(api, "Broken").Members.Single(m => m.Name == "Run").Documentation!.Summary.Should().Be("Fine.");
		api.Warnings.Should().ContainSingle()
			.Which.Should().StartWith("Lib.Broken (Source.cs:4): ").And.Contain("Its doc comment was left out.");
	}

	[Fact]
	public void ViewSource_IsTheAuthorsTextWithoutTheHiddenMembers()
	{
		ApiReference api = Analyze("""
		                           namespace Lib
		                           {
		                               /// <summary>A widget.</summary>
		                               public class Widget
		                               {
		                                   private int _count;

		                                   /// <summary>Runs.</summary>
		                                   public int Run(int times)
		                                   {
		                                       // keeps   its spacing
		                                       return  times*2;
		                                   }
		                               }
		                           }
		                           """);

		Type(api, "Widget").SourceCode.Should().Be("""
		                                           /// <summary>A widget.</summary>
		                                           public class Widget
		                                           {
		                                               /// <summary>Runs.</summary>
		                                               public int Run(int times)
		                                               {
		                                                   // keeps   its spacing
		                                                   return  times*2;
		                                               }
		                                           }
		                                           """.ReplaceLineEndings("\n"));
	}

	[Fact]
	public void ManyTypes_ComeOutInTheSameOrderEveryTime()
	{
		// Types are analyzed in parallel; the model's order must not depend on which thread finished first.
		var trees = Enumerable.Range(0, 40)
			.Select(i => CSharpSyntaxTree.ParseText(
				$"namespace Lib.N{i % 2};\n/// <summary>Type {i}.</summary>\npublic partial class T{i % 10} {{ public void M{i}() {{ }} }}",
				path: $"F{i:D2}.cs"))
			.ToList();

		string Snapshot() => string.Join("|", _analyzer.AnalyzeSyntaxTrees(trees, "Lib").Namespaces
			.SelectMany(ns => ns.Types)
			.Select(t => $"{t.FullName}:{string.Join(",", t.Members.Select(m => m.Name))}:{t.SourceCode?.Length}"));

		string first = Snapshot();
		Enumerable.Range(0, 5).Select(_ => Snapshot()).Should().AllBe(first);
	}
}
