using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

public sealed class PartialTypeTests
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);

	[Fact]
	public void Analyze_PartialTypeInTwoFiles_ProducesOneTypeWithEveryMember()
	{
		// Each partial declaration used to become its own ApiType, so the type got two pages on one route.
		SyntaxTree first = CSharpSyntaxTree.ParseText("""
		                                              namespace TestNs;

		                                              /// <summary>A widget.</summary>
		                                              public partial class Widget
		                                              {
		                                                  /// <summary>Opens it.</summary>
		                                                  public void Open() { }
		                                              }
		                                              """, path: "Widget.cs",
			cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree second = CSharpSyntaxTree.ParseText("""
		                                               namespace TestNs;

		                                               public partial class Widget
		                                               {
		                                                   /// <summary>Closes it.</summary>
		                                                   public void Close() { }
		                                               }
		                                               """, path: "Widget.Close.cs",
			cancellationToken: TestContext.Current.CancellationToken);

		ApiReference result = _analyzer.AnalyzeSyntaxTrees([first, second], "TestAssembly");

		ApiType widget = result.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle().Subject;
		widget.Name.Should().Be("Widget");
		widget.Members.Select(m => m.Name).Should().BeEquivalentTo("Open", "Close");
		widget.Documentation!.Summary.Should().Be("A widget.");
	}

	[Fact]
	public void Analyze_PartialTypeInTwoFiles_ViewSourceShowsBothDeclarations()
	{
		// The source panel showed whichever part the page came from and nothing of the other.
		SyntaxTree first = CSharpSyntaxTree.ParseText(
			"namespace TestNs; public partial class Widget { public void Open() { } }",
			path: "Widget.cs", cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree second = CSharpSyntaxTree.ParseText(
			"namespace TestNs; public partial class Widget { public void Close() { } }",
			path: "Widget.Close.cs", cancellationToken: TestContext.Current.CancellationToken);

		ApiReference result = _analyzer.AnalyzeSyntaxTrees([first, second], "TestAssembly");

		string source = result.Namespaces[0].Types.Should().ContainSingle().Subject.SourceCode!;
		source.Should().Contain("// Widget.cs").And.Contain("Open()");
		source.Should().Contain("// Widget.Close.cs").And.Contain("Close()");
		source.IndexOf("Open()", StringComparison.Ordinal).Should()
			.BeLessThan(source.IndexOf("Close()", StringComparison.Ordinal));
	}
}
