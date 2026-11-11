using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Inherited members and extension methods on type pages the CLI builds from two projects, where
///     the second project's base type comes from the first and so is compiled without its namespace.
/// </summary>
public sealed class ApiRelatedMembersTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-related-" + Guid.NewGuid().ToString("N"));

	public ApiRelatedMembersTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\nHome\n");
		Write("src/Core/Core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		Write("src/Core/Shapes.cs", """
		                            namespace Lib.Core;

		                            /// <summary>A shape.</summary>
		                            public abstract class Shape
		                            {
		                                /// <summary>Creates a shape.</summary>
		                                protected Shape() { }

		                                /// <summary>The name.</summary>
		                                public string Name { get; set; } = "";

		                                /// <summary>The area.</summary>
		                                public abstract double Area { get; }

		                                /// <summary>Moves the shape.</summary>
		                                public void Move(int x, int y) { }

		                                /// <summary>Moves the shape by a vector.</summary>
		                                public void Move(double distance) { }
		                            }

		                            /// <summary>Something that draws.</summary>
		                            public interface IDrawable
		                            {
		                                /// <summary>Draws it.</summary>
		                                void Draw();
		                            }

		                            /// <summary>Shape helpers.</summary>
		                            public static class ShapeExtensions
		                            {
		                                /// <summary>Describes a shape.</summary>
		                                public static string Describe(this Shape shape) => shape.Name;

		                                /// <summary>Doubles a number.</summary>
		                                public static int Twice(this int value) => value * 2;
		                            }
		                            """);
		Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		Write("src/App/Circles.cs", """
		                            using Lib.Core;

		                            namespace Lib.App;

		                            /// <summary>A circle.</summary>
		                            public class Circle : Shape
		                            {
		                                /// <inheritdoc />
		                                public override double Area => 3.14;

		                                /// <summary>The radius.</summary>
		                                public double Radius { get; set; }
		                            }

		                            /// <summary>A circle with radius one.</summary>
		                            public sealed class UnitCircle : Circle
		                            {
		                                /// <summary>Moves it without stretching.</summary>
		                                public new void Move(double distance) { }
		                            }
		                            """);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	private async Task<DryRunOutcome> RunAsync()
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Api" },
			Content = new ContentConfig
			{
				Docs = "./docs",
				Projects =
				[
					new ProjectSource { Path = "./src/Core/Core.csproj" },
					new ProjectSource { Path = "./src/App/App.csproj" }
				]
			},
			Build = new BuildConfig { Cache = false }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false,
			TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		return outcome;
	}

	[Fact]
	public async Task TypePage_FromAnotherProject_ListsInheritedMembersAndExtensionMethods()
	{
		DryRunOutcome outcome = await RunAsync();

		DocPage unitCircle = outcome.Context.Pages.Single(p => p.Route == "/api/lib/app/unitcircle");
		string html = unitCircle.Content.Html;

		// Circle and Shape are both documented, Shape only under its unqualified name from App's compilation.
		html.Should().Contain("<h2 id=\"inherited-members\">Inherited Members</h2>")
			.And.Contain("<td><a href=\"/api/lib/app/circle\">Circle</a></td>")
			.And.Contain("<a href=\"/api/lib/app/circle#radius\" class=\"api-member-link\"><code>Radius</code></a>")
			.And.Contain("<a href=\"/api/lib/app/circle#area\" class=\"api-member-link\"><code>Area</code></a>")
			.And.Contain("<td><a href=\"/api/lib/core/shape\">Shape</a></td>")
			.And.Contain("<a href=\"/api/lib/core/shape#name\" class=\"api-member-link\"><code>Name</code></a>")
			.And.Contain("<a href=\"/api/lib/core/shape#move\" class=\"api-member-link\"><code>Move(int, int)</code></a>");

		// Circle overrides Area, UnitCircle hides Move(double), and constructors are never inherited.
		html.Should().NotContain("/api/lib/core/shape#area")
			.And.NotContain("/api/lib/core/shape#move-2")
			.And.NotContain("/api/lib/core/shape#shape");

		html.Should().Contain("<h2 id=\"extension-methods\">Extension Methods</h2>")
			.And.Contain("<a href=\"/api/lib/core/shapeextensions#describe\" class=\"api-member-link\"><code>Describe(Shape)</code></a>")
			.And.Contain("<td><a href=\"/api/lib/core/shapeextensions\">ShapeExtensions</a></td>")
			.And.NotContain("#twice");

		unitCircle.TableOfContents.Entries.Select(e => e.Id).Should()
			.ContainInOrder("methods", "inherited-members", "extension-methods");
	}

	[Fact]
	public async Task TypePage_WithNothingInherited_HasNeitherSection()
	{
		DryRunOutcome outcome = await RunAsync();

		DocPage drawable = outcome.Context.Pages.Single(p => p.Route == "/api/lib/core/idrawable");

		drawable.Content.Html.Should().NotContain("Inherited Members").And.NotContain("Extension Methods");
		drawable.TableOfContents.Entries.Should().NotContain(e => e.Id == "inherited-members" || e.Id == "extension-methods");
	}
}
