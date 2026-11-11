using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

/// <summary>
///     The analyzer reads the files the project compiles: <c>Compile Remove</c> and linked
///     <c>Compile Include</c> items count, and generated or tool folders don't.
/// </summary>
public sealed class CompileItemsTests : IDisposable
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-tests", Guid.NewGuid().ToString("N"));

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

	private void WriteType(string relativePath, string typeName) =>
		WriteFile(relativePath, $"namespace Lib;\n\npublic class {typeName} {{ }}\n");

	private IEnumerable<string> TypeNames(string project) =>
		_analyzer.AnalyzeProject(CSharpProjectInfo.Load(project), "Lib").Namespaces.SelectMany(ns => ns.Types).Select(t => t.Name);

	[Fact]
	public void CompileRemoveAndLinkedIncludes_AreHonored()
	{
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <TargetFramework>net10.0</TargetFramework>
		                                               </PropertyGroup>
		                                               <ItemGroup>
		                                                 <Compile Remove="Legacy/**" />
		                                                 <Compile Remove="Excluded.cs" />
		                                                 <Compile Include="../Shared/Linked.cs" Link="Linked.cs" />
		                                               </ItemGroup>
		                                               <ItemGroup Condition="'$(Configuration)' == 'Debug'">
		                                                 <Compile Remove="Widget.cs" />
		                                               </ItemGroup>
		                                             </Project>
		                                             """);
		WriteType("Lib/Widget.cs", "Widget");
		WriteType("Lib/Excluded.cs", "Excluded");
		WriteType("Lib/Legacy/Old.cs", "Old");
		WriteType("Shared/Linked.cs", "Linked");
		WriteType("Shared/NotLinked.cs", "NotLinked");

		TypeNames(project).Should().BeEquivalentTo("Widget", "Linked");
	}

	[Fact]
	public void DotFoldersAndBuildOutput_AreSkipped()
	{
		string project = WriteFile("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		WriteType("Lib/Widget.cs", "Widget");
		WriteType("Lib/.mokadocs/preview-host/Host.cs", "PreviewHost");
		WriteType("Lib/.vs/Cache.cs", "VsCache");
		WriteType("Lib/bin/Debug/Generated.cs", "InBin");
		WriteType("Lib/obj/Debug/Generated.cs", "InObj");

		TypeNames(project).Should().Equal("Widget");
	}

	[Fact]
	public void ProjectUnderAFolderNamedBin_StillHasItsSources()
	{
		// bin and obj used to be matched anywhere in the full path, so this project had no types at all.
		string project = WriteFile("bin/obj/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		WriteType("bin/obj/Lib/Widget.cs", "Widget");

		TypeNames(project).Should().Equal("Widget");
	}

	[Fact]
	public void DefaultItemsOff_OnlyTheIncludedFilesAreRead()
	{
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
		                                               </PropertyGroup>
		                                               <ItemGroup>
		                                                 <Compile Include="Src/**/*.cs" Exclude="Src/Skip/*.cs" />
		                                               </ItemGroup>
		                                             </Project>
		                                             """);
		WriteType("Lib/Root.cs", "Root");
		WriteType("Lib/Src/A.cs", "A");
		WriteType("Lib/Src/Deep/B.cs", "B");
		WriteType("Lib/Src/Skip/C.cs", "C");

		TypeNames(project).Should().BeEquivalentTo("A", "B");
	}

	[Fact]
	public void DefaultItemExcludes_LeavesThoseFilesOut()
	{
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <PropertyGroup>
		                                                 <DefaultItemExcludes>$(DefaultItemExcludes);samples/**</DefaultItemExcludes>
		                                               </PropertyGroup>
		                                             </Project>
		                                             """);
		WriteType("Lib/Widget.cs", "Widget");
		WriteType("Lib/samples/Demo.cs", "Demo");

		TypeNames(project).Should().Equal("Widget");
	}

	[Fact]
	public void DirectoryBuildPropsItems_ApplyBeforeTheProjectsOwn()
	{
		WriteFile("Directory.Build.props", """
		                                   <Project>
		                                     <ItemGroup>
		                                       <Compile Include="$(MSBuildThisFileDirectory)Shared/*.cs" />
		                                     </ItemGroup>
		                                   </Project>
		                                   """);
		string project = WriteFile("Lib/Lib.csproj", """
		                                             <Project Sdk="Microsoft.NET.Sdk">
		                                               <ItemGroup>
		                                                 <Compile Remove="../Shared/Dropped.cs" />
		                                               </ItemGroup>
		                                             </Project>
		                                             """);
		WriteType("Lib/Widget.cs", "Widget");
		WriteType("Shared/Common.cs", "Common");
		WriteType("Shared/Dropped.cs", "Dropped");

		TypeNames(project).Should().BeEquivalentTo("Widget", "Common");
		CSharpProjectInfo.Load(project).SourceFiles.Should().Contain(Path.Combine(_root, "Shared", "Common.cs"));
	}
}
