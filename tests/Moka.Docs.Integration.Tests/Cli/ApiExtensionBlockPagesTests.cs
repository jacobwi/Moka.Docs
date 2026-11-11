using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     C# 14 extension blocks on the pages the CLI builds from a project's source.
/// </summary>
public sealed class ApiExtensionBlockPagesTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-extensions-" + Guid.NewGuid().ToString("N"));

	public ApiExtensionBlockPagesTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\nHome\n");
		Write("src/Shapes/Shapes.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		Write("src/Shapes/Shapes.cs", """
		                              namespace Lib.Shapes;

		                              /// <summary>A shape. Measure it with <see cref="ShapeExtensions.extension(Shape).Perimeter" />.</summary>
		                              public class Shape
		                              {
		                                  /// <summary>The width.</summary>
		                                  public double Width { get; set; }
		                              }

		                              /// <summary>A circle.</summary>
		                              public class Circle : Shape
		                              {
		                              }

		                              /// <summary>Shape helpers.</summary>
		                              public static class ShapeExtensions
		                              {
		                                  /// <summary>Describes a shape.</summary>
		                                  public static string Describe(this Shape shape) => "";

		                                  /// <summary>Members for any shape.</summary>
		                                  /// <param name="shape">The shape to extend.</param>
		                                  extension(Shape shape)
		                                  {
		                                      /// <summary>The perimeter.</summary>
		                                      public double Perimeter => shape.Width * 4;

		                                      /// <summary>A unit shape.</summary>
		                                      public static Shape Unit() => new();
		                                  }
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

	[Fact]
	public async Task ProjectWithExtensionBlocks_GroupsThemAndListsThemOnReceiverPages()
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Api" },
			Content = new ContentConfig
			{
				Docs = "./docs",
				Projects = [new ProjectSource { Path = "./src/Shapes/Shapes.csproj" }]
			},
			Build = new BuildConfig { Cache = false }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false, TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();

		outcome.Context.Pages.Select(p => p.Route).Should().OnlyContain(route => !route.Contains('$') && !route.Contains('<'));

		DocPage extensions = outcome.Context.Pages.Single(p => p.Route == "/api/lib/shapes/shapeextensions");
		extensions.Content.Html.Should().Contain("<h2 id=\"extension-shape\">extension(Shape shape)</h2>")
			.And.Contain("<tr><td><code>shape</code></td><td><code>Shape</code></td><td>The shape to extend.</td></tr>")
			.And.Contain("<h3 id=\"perimeter\">Perimeter</h3>")
			.And.Contain("<code class=\"language-csharp\">public double Perimeter { get; }</code>")
			.And.Contain("<code class=\"language-csharp\">public static Shape Unit()</code>")
			.And.Contain("extension(Shape shape)\n    {");
		extensions.TableOfContents.Entries.Select(e => e.Id).Should().Equal("methods", "extension-shape");

		DocPage circle = outcome.Context.Pages.Single(p => p.Route == "/api/lib/shapes/circle");
		circle.Content.Html.Should().Contain("<h2 id=\"extension-methods\">Extension Members</h2>")
			.And.Contain("<a href=\"/api/lib/shapes/shapeextensions#perimeter\" class=\"api-member-link\"><code>Perimeter</code></a>")
			.And.Contain("<a href=\"/api/lib/shapes/shapeextensions#unit\" class=\"api-member-link\"><code>Unit()</code></a>")
			.And.Contain("<a href=\"/api/lib/shapes/shapeextensions#describe\" class=\"api-member-link\"><code>Describe(Shape)</code></a>");

		outcome.Context.Pages.Single(p => p.Route == "/api/lib/shapes/shape").Content.Html.Should()
			.Contain("Measure it with <a href=\"/api/lib/shapes/shapeextensions#perimeter\">ShapeExtensions.Perimeter</a>.");
	}
}
