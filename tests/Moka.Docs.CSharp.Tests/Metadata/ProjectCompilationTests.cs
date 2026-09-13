using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

// The analyzer used to compile every project against five runtime assemblies with no global
// usings and no preprocessor symbols, so anything from another assembly, an implicit using or an
// #if branch came out unresolved or missing.
public sealed class ProjectCompilationTests : IDisposable
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-tests", Guid.NewGuid().ToString("N"));

	public ProjectCompilationTests()
	{
		Directory.CreateDirectory(_root);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// A locked temp folder is not worth failing a test run over.
		}
	}

	private string WriteFile(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	private static ApiMember Member(ApiReference api, string typeName, string memberName) =>
		api.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == typeName).Members.Single(m => m.Name == memberName);

	[Fact]
	public void AnalyzeProject_ImplicitUsings_ResolveFrameworkTypes()
	{
		// Without the SDK's global usings, List<string> and Task stayed unresolved and printed unqualified.
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                                 <ImplicitUsings>enable</ImplicitUsings>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteFile("Lib/Store.cs", """
		                          namespace Lib;

		                          public class Store
		                          {
		                              public List<string> Items { get; } = [];
		                              public Task SaveAsync() => Task.CompletedTask;
		                          }
		                          """);

		ApiReference api = _analyzer.AnalyzeProject(CSharpProjectInfo.Load(project), "Lib");

		Member(api, "Store", "Items").ReturnType.Should().Be("System.Collections.Generic.List<string>");
		Member(api, "Store", "SaveAsync").ReturnType.Should().Be("System.Threading.Tasks.Task");
	}

	[Fact]
	public void AnalyzeSyntaxTrees_FrameworkAssemblyOutsideTheOldFive_ResolvesTypes()
	{
		// System.Net.Http was not among the referenced assemblies, so HttpClient was an error type.
		SyntaxTree tree = CSharpSyntaxTree.ParseText("""
		                                             using System.Net.Http;
		                                             namespace Lib;
		                                             public class Client { public HttpClient Http { get; } = new(); }
		                                             """, cancellationToken: TestContext.Current.CancellationToken);

		ApiReference api = _analyzer.AnalyzeSyntaxTrees([tree], "Lib");

		Member(api, "Client", "Http").ReturnType.Should().Be("System.Net.Http.HttpClient");
	}

	[Fact]
	public void Load_UsingItems_BecomeGlobalUsings()
	{
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                                 <ImplicitUsings>enable</ImplicitUsings>
		                                               </PropertyGroup>
		                                               <ItemGroup>
		                                                 <Using Include="System.Text" />
		                                                 <Using Include="System.Math" Static="true" />
		                                                 <Using Include="System.Text.Json" Alias="Json" />
		                                                 <Using Remove="System.Net.Http" />
		                                               </ItemGroup>
		                                             </Project>
		                                             """);

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);

		info.GlobalUsings.Should().Contain([
			"global using global::System;",
			"global using global::System.Threading.Tasks;",
			"global using global::System.Text;",
			"global using static global::System.Math;",
			"global using Json = global::System.Text.Json;"
		]);
		info.GlobalUsings.Should().NotContain("global using global::System.Net.Http;");
	}

	[Fact]
	public void AnalyzeProject_StaticAndAliasUsings_ResolveInTheCompilation()
	{
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                               </PropertyGroup>
		                                               <ItemGroup>
		                                                 <Using Include="System.Math" Static="true" />
		                                                 <Using Include="System.Text.Json" Alias="Json" />
		                                               </ItemGroup>
		                                             </Project>
		                                             """);
		WriteFile("Lib/Settings.cs", """
		                             namespace Lib;

		                             public class Settings
		                             {
		                                 public const double Tau = PI * 2;
		                                 public Json.JsonSerializerOptions Options { get; } = new();
		                             }
		                             """);

		ApiReference api = _analyzer.AnalyzeProject(CSharpProjectInfo.Load(project), "Lib");

		Member(api, "Settings", "Tau").Signature.Should().StartWith("public const double Tau = 6.28");
		Member(api, "Settings", "Options").ReturnType.Should().Be("System.Text.Json.JsonSerializerOptions");
	}

	[Fact]
	public void Load_ImplicitUsingsInChainedDirectoryBuildProps_AreApplied()
	{
		WriteFile("Directory.Build.props", """
		                                   <Project>
		                                     <PropertyGroup>
		                                       <ImplicitUsings>enable</ImplicitUsings>
		                                       <TargetFramework>net10.0</TargetFramework>
		                                     </PropertyGroup>
		                                   </Project>
		                                   """);
		WriteFile("src/Directory.Build.props", """
		                                       <Project>
		                                         <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
		                                         <ItemGroup>
		                                           <Using Include="System.Text" />
		                                         </ItemGroup>
		                                       </Project>
		                                       """);
		string project = WriteFile("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);

		info.TargetFramework.Should().Be("net10.0");
		info.GlobalUsings.Should().Contain("global using global::System.Linq;")
			.And.Contain("global using global::System.Text;");
	}

	[Theory]
	[InlineData("net9.0",
		new[] { "NET", "NET9_0", "NETCOREAPP", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET5_0_OR_GREATER", "NETCOREAPP3_1_OR_GREATER", "NETCOREAPP1_0_OR_GREATER" },
		new[] { "NET10_0_OR_GREATER", "NETSTANDARD" })]
	[InlineData("netstandard2.0",
		new[] { "NETSTANDARD", "NETSTANDARD2_0", "NETSTANDARD2_0_OR_GREATER", "NETSTANDARD1_6_OR_GREATER" },
		new[] { "NETSTANDARD2_1_OR_GREATER", "NET", "NETCOREAPP" })]
	[InlineData("net48",
		new[] { "NETFRAMEWORK", "NET48", "NET48_OR_GREATER", "NET472_OR_GREATER", "NET20_OR_GREATER" },
		new[] { "NET481_OR_GREATER", "NETCOREAPP" })]
	[InlineData("netcoreapp3.1",
		new[] { "NETCOREAPP", "NETCOREAPP3_1", "NETCOREAPP3_1_OR_GREATER", "NETCOREAPP2_0_OR_GREATER" },
		new[] { "NET", "NET5_0_OR_GREATER" })]
	public void Load_TargetFramework_DefinesTheSdkSymbols(string framework, string[] defined, string[] notDefined)
	{
		string project = WriteFile("Lib/Lib.csproj", $"""
		                                              <Project Sdk="Microsoft.NET.Sdk">
		                                                <PropertyGroup>
		                                                  <TargetFramework>{framework}</TargetFramework>
		                                                </PropertyGroup>
		                                              </Project>
		                                              """);

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);

		info.PreprocessorSymbols.Should().Contain(defined).And.Contain(["TRACE", "RELEASE"]);
		info.PreprocessorSymbols.Should().NotContain(notDefined);
	}

	[Fact]
	public void AnalyzeProject_MultiTargetWithDefineConstants_UsesHighestFrameworkBranches()
	{
		// #if blocks were all inactive, so members behind framework or custom symbols vanished from the docs.
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>
		                                                 <DefineConstants>$(DefineConstants);FEATURE_EXPORT</DefineConstants>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteFile("Lib/Exporter.cs", """
		                             namespace Lib;

		                             public class Exporter
		                             {
		                             #if NET9_0_OR_GREATER
		                                 public void ExportModern() { }
		                             #else
		                                 public void ExportLegacy() { }
		                             #endif

		                             #if FEATURE_EXPORT
		                                 public void ExportExtra() { }
		                             #endif
		                             }
		                             """);

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);
		ApiReference api = _analyzer.AnalyzeProject(info, "Lib");

		info.TargetFramework.Should().Be("net10.0");
		List<ApiMember> members = api.Namespaces[0].Types.Single(t => t.Name == "Exporter").Members;
		members.Select(m => m.Name).Should().BeEquivalentTo("ExportModern", "ExportExtra");
	}

	[Fact]
	public void AnalyzeProject_PackageReferenceInAssetsFile_ResolvesPackageTypes()
	{
		// Types from NuGet packages were never referenced, so they printed as unqualified error types.
		string packageDll = Path.Combine(_root, "packages", "contoso.models", "1.0.0", "lib", "net10.0", "Moka.Docs.Core.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(packageDll)!);
		File.Copy(typeof(ApiType).Assembly.Location, packageDll);

		string project = WriteFile("App/App.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteFile("App/obj/project.assets.json", AssetsJson(
			"""
			"Contoso.Models/1.0.0": { "type": "package", "compile": { "lib/net10.0/Moka.Docs.Core.dll": {} } },
			"Empty.Package/2.0.0": { "type": "package", "compile": { "lib/net10.0/_._": {} } }
			""",
			"""
			"Contoso.Models/1.0.0": { "type": "package", "path": "contoso.models/1.0.0" },
			"Empty.Package/2.0.0": { "type": "package", "path": "empty.package/2.0.0" }
			"""));
		WriteFile("App/Holder.cs", """
		                           using Moka.Docs.Core.Api;
		                           namespace App;
		                           public class Holder { public ApiType Value { get; set; } }
		                           """);

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);
		ApiReference api = _analyzer.AnalyzeProject(info, "App");

		info.ReferencePaths.Should().ContainSingle().Which.Should().Be(packageDll);
		Member(api, "Holder", "Value").ReturnType.Should().Be("Moka.Docs.Core.Api.ApiType");
	}

	[Fact]
	public void AnalyzeProject_BuiltProjectReference_ResolvesItsTypes()
	{
		// NuGet records only a placeholder path for project references; the built assembly has to be found in bin.
		string builtDll = Path.Combine(_root, "Models", "bin", "Release", "net10.0", "Moka.Docs.Core.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(builtDll)!);
		File.Copy(typeof(ApiType).Assembly.Location, builtDll);
		WriteFile("Models/Models.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

		string project = WriteFile("App/App.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteFile("App/obj/project.assets.json", AssetsJson(
			"""
			"Models/1.0.0": { "type": "project", "framework": ".NETCoreApp,Version=v10.0", "compile": { "bin/placeholder/Moka.Docs.Core.dll": {} } }
			""",
			"""
			"Models/1.0.0": { "type": "project", "path": "../Models/Models.csproj", "msbuildProject": "../Models/Models.csproj" }
			"""));
		WriteFile("App/Holder.cs", """
		                           using Moka.Docs.Core.Api;
		                           namespace App;
		                           public class Holder { public ApiType Value { get; set; } }
		                           """);

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);
		ApiReference api = _analyzer.AnalyzeProject(info, "App");

		info.ReferencePaths.Should().ContainSingle().Which.Should().Be(builtDll);
		Member(api, "Holder", "Value").ReturnType.Should().Be("Moka.Docs.Core.Api.ApiType");
	}

	[Fact]
	public void AnalyzeProject_WebSdk_ResolvesAspNetCoreTypes()
	{
		// Only the core runtime was referenced, so ASP.NET Core types in a web project never resolved.
		string project = WriteFile("Api/Api.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk.Web">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                                 <ImplicitUsings>enable</ImplicitUsings>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteFile("Api/Endpoints.cs", """
		                              namespace Api;
		                              public static class Endpoints
		                              {
		                                  public static IResult Hello(HttpContext context) => Results.Ok();
		                              }
		                              """);

		ApiReference api = _analyzer.AnalyzeProject(CSharpProjectInfo.Load(project), "Api");

		Member(api, "Endpoints", "Hello").ReturnType.Should().Be("Microsoft.AspNetCore.Http.IResult");
	}

	[Fact]
	public void Load_ProjectNeverRestored_FallsBackToTheSharedFramework()
	{
		string project = WriteFile("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");

		CSharpProjectInfo info = CSharpProjectInfo.Load(project);

		info.ReferencePaths.Should().BeEmpty();
		info.SharedFrameworks.Should().BeEquivalentTo("Microsoft.NETCore.App", "Microsoft.AspNetCore.App");
		info.GlobalUsings.Should().Contain("global using global::Microsoft.AspNetCore.Builder;");
		info.InputFiles.Should().Contain(Path.Combine(_root, "Lib", "obj", "project.assets.json"))
			.And.Contain(Path.Combine(_root, "Lib", "Directory.Build.props"))
			.And.Contain(project);
	}

	private string AssetsJson(string targetLibraries, string libraries)
	{
		string packageFolder = System.Text.Json.JsonSerializer.Serialize(Path.Combine(_root, "packages") + Path.DirectorySeparatorChar);
		return $$"""
		         {
		           "version": 3,
		           "targets": {
		             "net10.0": {
		               {{targetLibraries}}
		             }
		           },
		           "libraries": {
		             {{libraries}}
		           },
		           "packageFolders": {
		             {{packageFolder}}: {}
		           },
		           "project": {
		             "frameworks": {
		               "net10.0": { "frameworkReferences": { "Microsoft.NETCore.App": { "privateAssets": "all" } } }
		             }
		           }
		         }
		         """;
	}
}
