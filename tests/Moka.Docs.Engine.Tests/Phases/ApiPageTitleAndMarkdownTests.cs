using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Phases;

/// <summary>
///     Type pages start with an h1 of the type's name, and render as Markdown for their Markdown copy.
/// </summary>
public sealed class ApiPageTitleAndMarkdownTests
{
	private const string _sample = """
	                               namespace Lib
	                               {
	                                   /// <summary>A result of <c>T</c>.</summary>
	                                   /// <typeparam name="T">The value type.</typeparam>
	                                   public sealed class Result<T>
	                                   {
	                                       /// <summary>Creates a result.</summary>
	                                       public Result(T value) { Value = value; }

	                                       /// <summary>The value.</summary>
	                                       public T Value { get; }

	                                       /// <summary>Maps the value.</summary>
	                                       /// <param name="map">How to map it.</param>
	                                       /// <param name="times">How often, in `ticks`.</param>
	                                       /// <returns>The mapped value.</returns>
	                                       public string Map(System.Func<T, string> map, int times) => map(Value);
	                                   }

	                                   /// <summary>An outer type.</summary>
	                                   public static class Outer
	                                   {
	                                       /// <summary>Options.</summary>
	                                       public sealed class Options { }
	                                   }
	                               }
	                               """;

	private static List<ApiType> Analyze()
	{
		var analyzer = new AssemblyAnalyzer(NullLogger<AssemblyAnalyzer>.Instance);
		return analyzer.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText(_sample)], "Tests").Namespaces
			.SelectMany(ns => ns.Types)
			.ToList();
	}

	[Theory]
	[InlineData("Result", "Result&lt;T&gt;")]
	[InlineData("Options", "Outer.Options")]
	public void RenderType_StartsWithAnH1OfTheDisplayName(string name, string heading)
	{
		List<ApiType> types = Analyze();

		string html = ApiPageRenderer.RenderType(types.Single(t => t.Name == name), types);

		html.Should().StartWith($"<h1 class=\"api-type-title\">{heading}</h1>");
	}

	[Fact]
	public void RenderType_KeepsTheMemberAnchors()
	{
		List<ApiType> types = Analyze();

		string html = ApiPageRenderer.RenderType(types.Single(t => t.Name == "Result"), types);

		html.Should().Contain("<h2 id=\"methods\">").And.Contain("<h3 id=\"map\">").And.NotContain("<h1 id=");
	}

	[Fact]
	public void RenderMarkdown_HasTheDeclarationDocsAndMembers()
	{
		List<ApiType> types = Analyze();

		string markdown = ApiPageRenderer.RenderMarkdown(types.Single(t => t.Name == "Result"), types);

		markdown.Should().StartWith("# Result<T>\n\nClass in `Lib`\n\n```csharp\npublic sealed class Result<T>");
		markdown.Should().Contain("A result of T.")
			.And.Contain("## Type Parameters\n\n- `T`: The value type.")
			.And.Contain("## Constructors\n\n### Result(T value)")
			.And.Contain("## Properties\n\n### Value")
			.And.Contain("### Map(Func<T, string> map, int times)")
			.And.Contain("- `map` (`Func<T, string>`): How to map it.")
			.And.Contain("- `times` (`int`): How often, in `ticks`.")
			.And.Contain("Returns: The mapped value.");
		markdown.Should().NotContain("<c>").And.NotContain("<p>").And.NotContain("\r");
		markdown.Should().EndWith("\n").And.NotEndWith("\n\n");
	}

	[Fact]
	public void RenderMarkdown_FenceOutlastsBackticksInTheCode()
	{
		var type = new ApiType
		{
			Name = "Tick",
			FullName = "Lib.Tick",
			Namespace = "Lib",
			Kind = ApiTypeKind.Class,
			Signature = "public class Tick // ```"
		};

		string markdown = ApiPageRenderer.RenderMarkdown(type);

		markdown.Should().Contain("````csharp\npublic class Tick // ```\n````");
	}
}
