using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Dry runs a real project through the build: nested types and types that differ only in their number
///     of type parameters each get a page, doc comment payloads render inert, and a configured project that
///     can't be analyzed fails the build.
/// </summary>
public sealed class ApiPageRouteTests : IDisposable
{
	private const string _project = """
	                                <Project Sdk="Microsoft.NET.Sdk">
	                                  <PropertyGroup>
	                                    <TargetFramework>net10.0</TargetFramework>
	                                  </PropertyGroup>
	                                </Project>
	                                """;

	private const string _source = """
	                               namespace Lib;

	                               /// <summary>Outer.</summary>
	                               public class Outer
	                               {
	                                   /// <summary>Outer's options.</summary>
	                                   public class Options
	                                   {
	                                       /// <summary>Whether it's on.</summary>
	                                       public bool Enabled { get; set; }
	                                   }
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
	                               public class Result<T> : Result
	                               {
	                                   /// <summary>Finds a value by id.</summary>
	                                   public T Find(int id) => default!;
	                               }

	                               /// <summary>Links to <see cref="Outer.Options"/>, <see cref="Other.Options"/> and <see cref="Result{T}.Find(int)"/>.</summary>
	                               public class Linker { }

	                               /// <summary>Uses <paramref name="&lt;img src=x onerror=alert(1)&gt;"/> and <see href="java&#9;script:alert(3)">this</see>.</summary>
	                               public class Hostile
	                               {
	                                   /// <summary>Runs <typeparamref name="&lt;script&gt;alert(2)&lt;/script&gt;"/>.</summary>
	                                   public void Run() { }
	                               }
	                               """;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-routes-" + Guid.NewGuid().ToString("N"));

	public ApiPageRouteTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\nHome\n");
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

	private async Task<DryRunOutcome> RunAsync(params string[] projects)
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Api" },
			Content = new ContentConfig
			{
				Docs = "./docs",
				Projects = projects.Select(path => new ProjectSource { Path = path }).ToList()
			},
			Build = new BuildConfig { Cache = false }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false, TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		return outcome;
	}

	private async Task<DryRunOutcome> RunLibraryAsync()
	{
		Write("src/Lib/Lib.csproj", _project);
		Write("src/Lib/Types.cs", _source);
		return await RunAsync("src/Lib/Lib.csproj");
	}

	private static DocPage Page(DryRunOutcome outcome, string route) =>
		outcome.Context.Pages.Single(p => p.Route == route);

	[Fact]
	public async Task NestedTypesAndGenericTwins_EachGetAPage()
	{
		DryRunOutcome outcome = await RunLibraryAsync();

		List<DocPage> apiPages = outcome.Context.Pages.Where(p => p.Origin == PageOrigin.ApiGenerated).ToList();
		apiPages.Select(p => p.Route).Should().OnlyHaveUniqueItems();
		apiPages.Select(p => (p.Route, p.FrontMatter.Title)).Should().Contain([
			("/api/lib/outer", "Outer"),
			("/api/lib/outer.options", "Outer.Options"),
			("/api/lib/other.options", "Other.Options"),
			("/api/lib/result", "Result"),
			("/api/lib/result-1", "Result<T>")
		]);
		Page(outcome, "/api/lib/outer.options").Content.Html.Should().Contain("Whether it's on.");
		Page(outcome, "/api/lib/result-1").Content.Html.Should().Contain("Finds a value by id.");
		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Message.Contains("same route"));
	}

	[Fact]
	public async Task IndexRowsAndCrefs_PointAtTheRightPage()
	{
		DryRunOutcome outcome = await RunLibraryAsync();

		Page(outcome, "/api").Content.Html.Should()
			.Contain("<a href=\"/api/lib/outer.options\">Outer.Options</a>")
			.And.Contain("<a href=\"/api/lib/other.options\">Other.Options</a>")
			.And.Contain("<a href=\"/api/lib/result\">Result</a>")
			.And.Contain("<a href=\"/api/lib/result-1\">Result&lt;T&gt;</a>");

		Page(outcome, "/api/lib/linker").Content.Html.Should()
			.Contain("<a href=\"/api/lib/outer.options\">Outer.Options</a>")
			.And.Contain("<a href=\"/api/lib/other.options\">Other.Options</a>")
			.And.Contain("<a href=\"/api/lib/result-1#find\">Result.Find</a>");
	}

	[Fact]
	public async Task DocCommentPayloads_RenderInert_OnTheTypePageMemberTableAndIndex()
	{
		DryRunOutcome outcome = await RunLibraryAsync();

		foreach (string html in new[] { Page(outcome, "/api/lib/hostile").Content.Html, Page(outcome, "/api").Content.Html })
		{
			html.Should().NotContain("<img").And.NotContain("<script>alert").And.NotContain("href=\"java");
			html.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
		}

		// Roslyn repeats an escaped name attribute's text; what matters is that it stays text.
		Page(outcome, "/api/lib/hostile").Content.Html.Should()
			.Contain("<td>Runs <code>&lt;script&gt;alert(2)&lt;/script&gt;");
	}

	[Fact]
	public async Task SearchIndex_HasAnEntryPerMember_InsteadOfEmptySectionEntries()
	{
		DryRunOutcome outcome = await RunLibraryAsync();
		List<SearchEntry> entries = outcome.Context.SearchIndex!.Entries;

		entries.Should().ContainSingle(e => e.Route == "/api/lib/result-1#find")
			.Which.Should().Match<SearchEntry>(e =>
				e.Title == "Result<T>" && e.Section == "Find(int id)" && e.Content == "Finds a value by id.");
		entries.Should().ContainSingle(e => e.Route == "/api/lib/outer.options#enabled");
		entries.Should().NotContain(e => e.Route.StartsWith("/api/lib/", StringComparison.Ordinal) && e.Section == "Methods");
	}

	[Fact]
	public async Task MissingProject_IsAnErrorThatFailsTheBuild()
	{
		DryRunOutcome outcome = await RunAsync("src/Gone/Gone.csproj");

		outcome.Context.Diagnostics.HasErrors.Should().BeTrue();
		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
			.Which.Message.Should().StartWith("Project file not found: src/Gone/Gone.csproj")
			.And.Contain("Its API reference was not generated.");
	}

	[Fact]
	public async Task BadlyFormedDocComment_IsAWarningNamingTheMember()
	{
		Write("src/Lib/Lib.csproj", _project);
		Write("src/Lib/Broken.cs", """
		                           namespace Lib;

		                           /// <summary>Unclosed <b>tag</summary>
		                           public class Broken { }
		                           """);

		DryRunOutcome outcome = await RunAsync("src/Lib/Lib.csproj");

		outcome.Context.Diagnostics.HasErrors.Should().BeFalse();
		outcome.Context.Diagnostics.All.Should().ContainSingle(d =>
			d.Severity == DiagnosticSeverity.Warning && d.Message.StartsWith("Lib: Lib.Broken (Broken.cs:4): ", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("<PackageId>$(MSBuildProjectName)</PackageId>", "Acme.Lib")]
	[InlineData("<AssemblyName>Acme.$(MSBuildProjectName).Core</AssemblyName>", "Acme.Acme.Lib.Core")]
	[InlineData("<Prefix>Acme</Prefix><PackageId>$(Prefix).Tools</PackageId>", "Acme.Tools")]
	[InlineData("<PackageId>$(SetSomewhereElse)</PackageId>", "Acme.Lib")]
	public async Task PackageId_BuiltFromProperties_IsFilledIn(string properties, string expected)
	{
		Write("src/Lib/Acme.Lib.csproj", $"""
		                                  <Project Sdk="Microsoft.NET.Sdk">
		                                    <PropertyGroup>
		                                      <TargetFramework>net10.0</TargetFramework>
		                                      {properties}
		                                    </PropertyGroup>
		                                  </Project>
		                                  """);
		Write("src/Lib/Widget.cs", "namespace Lib;\n\n/// <summary>A widget.</summary>\npublic class Widget { }\n");

		DryRunOutcome outcome = await RunAsync("src/Lib/Acme.Lib.csproj");

		outcome.Context.PackageInfo!.Name.Should().Be(expected);
	}
}
