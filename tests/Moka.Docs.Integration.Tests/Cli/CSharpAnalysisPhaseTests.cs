using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Dry runs real projects on disk through the C# analysis phase: package selection for the
///     install widget, partial types, and cache invalidation by project files.
/// </summary>
public sealed class CSharpAnalysisPhaseTests : IDisposable
{
	private const string _libraryProject = """
	                                       <Project Sdk="Microsoft.NET.Sdk">
	                                         <PropertyGroup>
	                                           <TargetFramework>net10.0</TargetFramework>
	                                           <PackageId>Acme.Lib</PackageId>
	                                         </PropertyGroup>
	                                       </Project>
	                                       """;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-analysis-" + Guid.NewGuid().ToString("N"));

	public CSharpAnalysisPhaseTests()
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

	private string Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	private async Task<DryRunOutcome> RunAsync(bool useCache, params string[] projects)
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Api" },
			Content = new ContentConfig
			{
				Docs = "./docs",
				Projects = projects.Select(path => new ProjectSource { Path = path }).ToList()
			},
			Build = new BuildConfig { Cache = useCache }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false,
			TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		return outcome;
	}

	private static ApiMember Member(DryRunOutcome outcome, string typeName, string memberName) =>
		outcome.Context.ApiModel!.Namespaces.SelectMany(ns => ns.Types)
			.Single(t => t.Name == typeName)
			.Members.Single(m => m.Name == memberName);

	#region Install widget

	[Fact]
	public async Task PackageInfo_TestAndSampleProjectsListedFirst_DescribesThePackableProject()
	{
		// The widget described the first configured project, here a test project nobody can install.
		Write("tests/Lib.Tests/Lib.Tests.csproj", """
		                                          <Project Sdk="Microsoft.NET.Sdk">
		                                            <PropertyGroup>
		                                              <TargetFramework>net10.0</TargetFramework>
		                                              <IsTestProject>true</IsTestProject>
		                                            </PropertyGroup>
		                                          </Project>
		                                          """);
		Write("tests/Lib.Tests/WidgetTests.cs", "namespace Lib.Tests; public class WidgetTests { }");
		Write("samples/Directory.Build.props", "<Project><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>");
		Write("samples/Demo/Demo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		Write("samples/Demo/Program.cs", "namespace Demo; public class Program { }");
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Widget.cs", "namespace Lib; public class Widget { }");

		DryRunOutcome outcome = await RunAsync(false,
			"./tests/Lib.Tests/Lib.Tests.csproj", "./samples/Demo/Demo.csproj", "./src/Lib/Lib.csproj");

		outcome.Context.PackageInfo!.Name.Should().Be("Acme.Lib");
	}

	[Fact]
	public async Task PackageInfo_WebApplicationListedFirst_DescribesTheLibrary()
	{
		Write("src/Api/Api.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><PackageId>Acme.Api</PackageId></PropertyGroup></Project>");
		Write("src/Api/Program.cs", "namespace Api; public class Program { }");
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Widget.cs", "namespace Lib; public class Widget { }");

		DryRunOutcome outcome = await RunAsync(false, "./src/Api/Api.csproj", "./src/Lib/Lib.csproj");

		outcome.Context.PackageInfo!.Name.Should().Be("Acme.Lib");
	}

	[Fact]
	public async Task PackageInfo_NoPackableProject_FallsBackToTheFirstProject()
	{
		Write("tests/Lib.Tests/Lib.Tests.csproj",
			"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>");
		Write("tests/Lib.Tests/WidgetTests.cs", "namespace Lib.Tests; public class WidgetTests { }");

		DryRunOutcome outcome = await RunAsync(false, "./tests/Lib.Tests/Lib.Tests.csproj");

		outcome.Context.PackageInfo!.Name.Should().Be("Lib.Tests");
	}

	#endregion

	#region Partial types

	[Fact]
	public async Task PartialType_DeclaredInTwoFiles_GetsOnePage()
	{
		// Each declaration became its own page on the same route, and the build warned that two pages collided.
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Widget.cs", """
		                           namespace Lib;

		                           /// <summary>A widget.</summary>
		                           public partial class Widget
		                           {
		                               /// <summary>Opens the widget.</summary>
		                               public void Open() { }
		                           }
		                           """);
		Write("src/Lib/Widget.Close.cs", """
		                                 namespace Lib;

		                                 public partial class Widget
		                                 {
		                                     /// <summary>Closes the widget.</summary>
		                                     public void Close() { }
		                                 }
		                                 """);

		DryRunOutcome outcome = await RunAsync(false, "./src/Lib/Lib.csproj");

		DocPage page = outcome.Context.Pages.Should().ContainSingle(p => p.Route == "/api/lib/widget").Subject;
		page.Content.Html.Should().Contain("Opens the widget.").And.Contain("Closes the widget.");
		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Message.Contains("share the route"));
	}

	#endregion

	#region Cache

	[Fact]
	public async Task Cache_NothingChanged_ReusesTheEntry()
	{
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Store.cs", "namespace Lib; public class Store { }");

		await RunAsync(true, "./src/Lib/Lib.csproj");
		string entry = Directory.GetFiles(Path.Combine(_root, ".mokadocs", "cache")).Single();
		File.SetLastWriteTimeUtc(entry, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		await RunAsync(true, "./src/Lib/Lib.csproj");

		File.GetLastWriteTimeUtc(entry).Should().Be(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
	}

	[Fact]
	public async Task Cache_ImplicitUsingsEnabledInProjectFile_ReanalyzesTheProject()
	{
		// The fingerprint covered source files only, so a project file change served the stale model.
		string project = Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Store.cs", "namespace Lib; public class Store { public List<string> Items { get; } = new(); }");

		DryRunOutcome before = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(before, "Store", "Items").ReturnType.Should().Be("List<string>");

		File.WriteAllText(project, _libraryProject.Replace(
			"<PackageId>", "<ImplicitUsings>enable</ImplicitUsings><PackageId>", StringComparison.Ordinal));
		File.SetLastWriteTimeUtc(project, DateTime.UtcNow.AddMinutes(1));

		DryRunOutcome after = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(after, "Store", "Items").ReturnType.Should().Be("System.Collections.Generic.List<string>");
	}

	[Fact]
	public async Task Cache_DirectoryBuildPropsAdded_ReanalyzesTheProject()
	{
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Store.cs", "namespace Lib; public class Store { public List<string> Items { get; } = new(); }");

		DryRunOutcome before = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(before, "Store", "Items").ReturnType.Should().Be("List<string>");

		Write("Directory.Build.props", "<Project><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");

		DryRunOutcome after = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(after, "Store", "Items").ReturnType.Should().Be("System.Collections.Generic.List<string>");
	}

	[Fact]
	public async Task Cache_AssetsFileWritten_ReanalyzesTheProject()
	{
		Write("src/Lib/Lib.csproj", _libraryProject);
		Write("src/Lib/Holder.cs", "using Moka.Docs.Core.Api; namespace Lib; public class Holder { public ApiType Value { get; set; } }");

		DryRunOutcome before = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(before, "Holder", "Value").ReturnType.Should().Be("ApiType");

		// A restore that adds a package: its assembly becomes resolvable.
		string packageFolder = Path.Combine(_root, "packages") + Path.DirectorySeparatorChar;
		string dll = Path.Combine(packageFolder, "acme.models", "1.0.0", "lib", "net10.0", "Moka.Docs.Core.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(dll)!);
		File.Copy(typeof(ApiType).Assembly.Location, dll);
		Write("src/Lib/obj/project.assets.json", $$"""
		                                          {
		                                            "version": 3,
		                                            "targets": {
		                                              "net10.0": {
		                                                "Acme.Models/1.0.0": { "type": "package", "compile": { "lib/net10.0/Moka.Docs.Core.dll": {} } }
		                                              }
		                                            },
		                                            "libraries": {
		                                              "Acme.Models/1.0.0": { "type": "package", "path": "acme.models/1.0.0" }
		                                            },
		                                            "packageFolders": { {{System.Text.Json.JsonSerializer.Serialize(packageFolder)}}: {} },
		                                            "project": { "frameworks": { "net10.0": {} } }
		                                          }
		                                          """);

		DryRunOutcome after = await RunAsync(true, "./src/Lib/Lib.csproj");
		Member(after, "Holder", "Value").ReturnType.Should().Be("Moka.Docs.Core.Api.ApiType");
	}

	#endregion
}
