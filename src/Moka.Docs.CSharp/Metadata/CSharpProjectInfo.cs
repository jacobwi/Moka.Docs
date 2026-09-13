using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     The settings a C# project compiles with, read from its .csproj, the Directory.Build.props
///     it imports and <c>obj/project.assets.json</c>, without running MSBuild.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 Only unconditional properties and items are read. <c>$(Name)</c> is replaced with a
///                 property read before it; a value that needs more of MSBuild than that is ignored.
///             </description>
///         </item>
///         <item>
///             <description>A project with several target frameworks is read for the highest one.</description>
///         </item>
///         <item>
///             <description>
///                 Package and project references come from <c>obj/project.assets.json</c>, so they
///                 need a restore. A project reference also needs that project built: its assembly
///                 is taken from the referenced project's <c>bin</c> folder.
///             </description>
///         </item>
///     </list>
///     Nothing here is required. A project that was never restored still loads and compiles
///     against the shared framework alone.
/// </remarks>
public sealed class CSharpProjectInfo
{
	private const string _directoryBuildProps = "Directory.Build.props";

	// Microsoft.NET.Sdk.CSharp.props: the namespaces ImplicitUsings adds for every C# project.
	private static readonly string[] _implicitUsings =
	[
		"System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http",
		"System.Threading", "System.Threading.Tasks"
	];

	// The extra namespaces some SDKs add on top (Sdk.Server.props and friends in the .NET SDK).
	private static readonly Dictionary<string, string[]> _sdkImplicitUsings = new(StringComparer.OrdinalIgnoreCase)
	{
		["Microsoft.NET.Sdk.Web"] =
		[
			"System.Net.Http.Json", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Hosting",
			"Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing", "Microsoft.Extensions.Configuration",
			"Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Hosting", "Microsoft.Extensions.Logging"
		],
		["Microsoft.NET.Sdk.Worker"] =
		[
			"Microsoft.Extensions.Configuration", "Microsoft.Extensions.DependencyInjection",
			"Microsoft.Extensions.Hosting", "Microsoft.Extensions.Logging"
		],
		["Microsoft.NET.Sdk.BlazorWebAssembly"] =
		[
			"Microsoft.Extensions.Configuration", "Microsoft.Extensions.DependencyInjection",
			"Microsoft.Extensions.Logging"
		]
	};

	/// <summary>Absolute path to the .csproj.</summary>
	public required string ProjectPath { get; init; }

	/// <summary>The directory holding the .csproj. Source files are read from below it.</summary>
	public string ProjectDirectory => Path.GetDirectoryName(ProjectPath) ?? "";

	/// <summary>The target framework the project is analyzed for, or <c>null</c> when unknown.</summary>
	public string? TargetFramework { get; init; }

	/// <summary>
	///     Preprocessor symbols: the target framework's, <c>TRACE</c>, <c>RELEASE</c> and
	///     <c>DefineConstants</c>.
	/// </summary>
	public IReadOnlyList<string> PreprocessorSymbols { get; init; } = [];

	/// <summary>
	///     The global using directives the SDK would generate, as C# source lines such as
	///     <c>global using global::System;</c>.
	/// </summary>
	public IReadOnlyList<string> GlobalUsings { get; init; } = [];

	/// <summary>The nullable context set by the <c>Nullable</c> property.</summary>
	public NullableContextOptions NullableContext { get; init; } = NullableContextOptions.Disable;

	/// <summary>The shared frameworks the project references, such as <c>Microsoft.AspNetCore.App</c>.</summary>
	public IReadOnlyList<string> SharedFrameworks { get; init; } = [CompilationReferences.CoreFramework];

	/// <summary>Compile-time assemblies of package and project references that were found on disk.</summary>
	public IReadOnlyList<string> ReferencePaths { get; init; } = [];

	/// <summary>
	///     Every file whose change can change the analysis besides the source files: the .csproj,
	///     each place a Directory.Build.props could be, the assets file and the reference
	///     assemblies. Files that do not exist are listed too, so creating one is noticed.
	/// </summary>
	public IReadOnlyList<string> InputFiles { get; init; } = [];

	/// <summary>
	///     Reads a project's compilation settings.
	/// </summary>
	/// <param name="projectPath">Path to the .csproj.</param>
	/// <returns>The settings. Files that are missing or unreadable leave the defaults in place.</returns>
	public static CSharpProjectInfo Load(string projectPath)
	{
		string fullPath = Path.GetFullPath(projectPath);
		string projectDirectory = Path.GetDirectoryName(fullPath) ?? "";

		var reader = new ProjectFileReader(fullPath);
		var importedItems = new List<ProjectItem>();
		var projectItems = new List<ProjectItem>();

		// MSBuild imports only the nearest Directory.Build.props. A parent one applies when that
		// file imports it, which the reader follows.
		if (FindFileAbove(projectDirectory, _directoryBuildProps) is { } directoryBuildProps)
		{
			reader.Read(directoryBuildProps, importedItems);
		}

		string sdk = reader.Read(fullPath, projectItems)?.Attribute("Sdk")?.Value ?? "";

		string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
		ProjectAssets? assets = ProjectAssets.TryLoad(assetsPath);

		// A TargetFramework set anywhere makes the project single-targeted, even when a
		// Directory.Build.props also sets TargetFrameworks.
		string? targetFramework = HighestTargetFramework(SplitList(reader.Get("TargetFramework")))
		                          ?? HighestTargetFramework(SplitList(reader.Get("TargetFrameworks")))
		                          ?? HighestTargetFramework(assets?.TargetFrameworks ?? []);
		TargetFrameworkMoniker? moniker = TargetFrameworkMoniker.TryParse(targetFramework);

		List<string> referencePaths = assets?.GetCompileAssemblies(targetFramework, projectDirectory) ?? [];

		var inputs = new List<string> { fullPath, assetsPath };
		inputs.AddRange(AncestorDirectories(projectDirectory)
			.Select(directory => Path.Combine(directory, _directoryBuildProps)));
		inputs.AddRange(reader.FilesRead);
		inputs.AddRange(referencePaths);

		return new CSharpProjectInfo
		{
			ProjectPath = fullPath,
			TargetFramework = targetFramework,
			PreprocessorSymbols = GetPreprocessorSymbols(reader, moniker),
			GlobalUsings = GetGlobalUsings(reader, sdk, moniker, importedItems, projectItems),
			NullableContext = ParseNullable(reader.Get("Nullable")),
			SharedFrameworks = GetSharedFrameworks(sdk, targetFramework, assets, importedItems.Concat(projectItems)),
			ReferencePaths = referencePaths,
			InputFiles = inputs.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
		};
	}

	#region Settings

	private static List<string> GetPreprocessorSymbols(ProjectFileReader reader, TargetFrameworkMoniker? moniker)
	{
		var symbols = new List<string>();
		if (moniker is { } framework && !IsTrue(reader.Get("DisableImplicitFrameworkDefines")))
		{
			symbols.AddRange(framework.PreprocessorSymbols());
		}

		// The SDK defines TRACE in every configuration and RELEASE in Release, the build a
		// library ships from.
		symbols.Add("TRACE");
		symbols.Add("RELEASE");
		symbols.AddRange(SplitList(reader.Get("DefineConstants")));

		return symbols.Distinct(StringComparer.Ordinal).ToList();
	}

	private static List<string> GetGlobalUsings(
		ProjectFileReader reader,
		string sdk,
		TargetFrameworkMoniker? moniker,
		List<ProjectItem> importedItems,
		List<ProjectItem> projectItems)
	{
		var usings = new List<GlobalUsing>();

		// Evaluation order decides what a Remove can see: Directory.Build.props items come before
		// the SDK's implicit usings and the project's own items after them.
		ApplyUsingItems(importedItems, usings);

		string? implicitUsings = reader.Get("ImplicitUsings");
		if (IsTrue(implicitUsings) || string.Equals(implicitUsings, "enable", StringComparison.OrdinalIgnoreCase))
		{
			IEnumerable<string> namespaces = _implicitUsings
				// The SDK leaves System.Net.Http out when targeting .NET Framework.
				.Where(ns => ns != "System.Net.Http" || moniker?.IsNetFramework != true)
				.Concat(SdkNames(sdk).SelectMany(name => _sdkImplicitUsings.GetValueOrDefault(name) ?? []));

			usings.AddRange(namespaces.Select(ns => new GlobalUsing(ns, false, null)));
		}

		ApplyUsingItems(projectItems, usings);

		return usings
			.Select(item => item switch
			{
				{ Alias.Length: > 0 } => $"global using {item.Alias} = global::{item.Namespace};",
				{ IsStatic: true } => $"global using static global::{item.Namespace};",
				_ => $"global using global::{item.Namespace};"
			})
			.Distinct(StringComparer.Ordinal)
			.ToList();
	}

	private static void ApplyUsingItems(IEnumerable<ProjectItem> items, List<GlobalUsing> usings)
	{
		foreach (ProjectItem item in items.Where(i => i.ItemType == "Using"))
		{
			if (item.Remove is not null)
			{
				string[] removed = SplitList(item.Remove);
				usings.RemoveAll(u => removed.Contains(u.Namespace, StringComparer.OrdinalIgnoreCase));
			}

			if (item.Include is not null)
			{
				usings.AddRange(SplitList(item.Include).Select(ns => new GlobalUsing(ns, item.IsStatic, item.Alias)));
			}
		}
	}

	private static List<string> GetSharedFrameworks(
		string sdk,
		string? targetFramework,
		ProjectAssets? assets,
		IEnumerable<ProjectItem> items)
	{
		var frameworks = new List<string> { CompilationReferences.CoreFramework };
		if (assets is not null)
		{
			frameworks.AddRange(assets.GetFrameworkReferences(targetFramework));
		}
		else if (SdkNames(sdk).Contains("Microsoft.NET.Sdk.Web", StringComparer.OrdinalIgnoreCase))
		{
			// Restore records the Web SDK's implicit framework reference in the assets file.
			// Without one, go by the SDK name.
			frameworks.Add("Microsoft.AspNetCore.App");
		}

		frameworks.AddRange(items
			.Where(item => item.ItemType == "FrameworkReference" && item.Include is not null)
			.SelectMany(item => SplitList(item.Include)));

		return frameworks.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static NullableContextOptions ParseNullable(string? value)
	{
		return value?.ToLowerInvariant() switch
		{
			"enable" => NullableContextOptions.Enable,
			"warnings" => NullableContextOptions.Warnings,
			"annotations" => NullableContextOptions.Annotations,
			_ => NullableContextOptions.Disable
		};
	}

	private static string? HighestTargetFramework(IEnumerable<string> frameworks)
	{
		string? best = null;
		TargetFrameworkMoniker? bestMoniker = null;
		foreach (string framework in frameworks)
		{
			TargetFrameworkMoniker? moniker = TargetFrameworkMoniker.TryParse(framework);
			bool higher = moniker is { } parsed && (bestMoniker is null || parsed.CompareTo(bestMoniker.Value) > 0);
			if (best is null || higher)
			{
				best = framework;
				bestMoniker = moniker;
			}
		}

		return best;
	}

	#endregion

	#region Helpers

	// "Microsoft.NET.Sdk.Web", "Microsoft.NET.Sdk/1.0.0" or several separated by semicolons.
	private static IEnumerable<string> SdkNames(string sdk) =>
		SplitList(sdk).Select(name => name.Split('/')[0].Trim());

	private static string[] SplitList(string? value) =>
		value?.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

	private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	private static IEnumerable<string> AncestorDirectories(string directory)
	{
		for (string? current = Path.TrimEndingDirectorySeparator(directory);
		     !string.IsNullOrEmpty(current);
		     current = Path.GetDirectoryName(current))
		{
			yield return current;
		}
	}

	private static string? FindFileAbove(string directory, string fileName) =>
		AncestorDirectories(directory)
			.Select(current => Path.Combine(current, fileName))
			.FirstOrDefault(File.Exists);

	#endregion

	#region Project files

	private sealed record ProjectItem(string ItemType, string? Include, string? Remove, bool IsStatic, string? Alias);

	private sealed record GlobalUsing(string Namespace, bool IsStatic, string? Alias);

	/// <summary>
	///     Collects properties and items from project files in the order MSBuild evaluates them.
	/// </summary>
	private sealed class ProjectFileReader
	{
		private const string _thisFileDirectory = "MSBuildThisFileDirectory";
		private const string _pathOfFileAbove = "$([MSBuild]::GetPathOfFileAbove(";

		private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

		public ProjectFileReader(string projectPath)
		{
			_properties["MSBuildProjectDirectory"] = Path.GetDirectoryName(projectPath) ?? "";
			_properties["MSBuildProjectName"] = Path.GetFileNameWithoutExtension(projectPath);
		}

		/// <summary>Every project file that was read, imports included.</summary>
		public List<string> FilesRead { get; } = [];

		public string? Get(string name) =>
			_properties.TryGetValue(name, out string? value) && value.Length > 0 ? value : null;

		/// <summary>
		///     Reads a project file and the files it imports, returning its root element.
		/// </summary>
		public XElement? Read(string path, List<ProjectItem> items)
		{
			string fullPath = Path.GetFullPath(path);
			if (!_visited.Add(fullPath) || LoadRoot(fullPath) is not { } root)
			{
				return null;
			}

			FilesRead.Add(fullPath);
			string directory = Path.GetDirectoryName(fullPath) ?? "";
			string? outerFileDirectory = _properties.GetValueOrDefault(_thisFileDirectory);
			_properties[_thisFileDirectory] = directory + Path.DirectorySeparatorChar;

			foreach (XElement element in root.Elements())
			{
				// Evaluating conditions needs MSBuild, so a conditional group is skipped whole.
				if (element.Attribute("Condition") is not null)
				{
					continue;
				}

				switch (element.Name.LocalName)
				{
					case "PropertyGroup":
						foreach (XElement property in Unconditional(element))
						{
							if (Expand(property.Value) is { } value)
							{
								_properties[property.Name.LocalName] = value;
							}
						}

						break;

					case "ItemGroup":
						items.AddRange(Unconditional(element).Select(item => new ProjectItem(
							item.Name.LocalName,
							Metadata(item, "Include"),
							Metadata(item, "Remove"),
							IsTrue(Metadata(item, "Static")),
							Metadata(item, "Alias"))));
						break;

					case "Import" when ResolveImport(element, directory) is { } imported:
						Read(imported, items);
						break;
				}
			}

			if (outerFileDirectory is null)
			{
				_properties.Remove(_thisFileDirectory);
			}
			else
			{
				_properties[_thisFileDirectory] = outerFileDirectory;
			}

			return root;
		}

		private static IEnumerable<XElement> Unconditional(XElement group) =>
			group.Elements().Where(child => child.Attribute("Condition") is null);

		private static XElement? LoadRoot(string path)
		{
			try
			{
				return File.Exists(path) ? XDocument.Load(path).Root : null;
			}
			catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}

		private string? ResolveImport(XElement import, string directory)
		{
			// <Import Project="Sdk.props" Sdk="..."/> points into the SDK, not at a project file.
			if (import.Attribute("Sdk") is not null)
			{
				return null;
			}

			string project = import.Attribute("Project")?.Value.Trim() ?? "";

			// Nested Directory.Build.props files chain to the next one up with
			// $([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../')).
			if (project.StartsWith(_pathOfFileAbove, StringComparison.OrdinalIgnoreCase)
			    && project.EndsWith("))", StringComparison.Ordinal))
			{
				string[] arguments = project[_pathOfFileAbove.Length..^2]
					.Split(',')
					.Select(argument => argument.Trim().Trim('\'').Trim())
					.ToArray();
				string? start = arguments.Length > 1 ? Expand(arguments[1]) : directory;
				return start is null || arguments[0].Length == 0
					? null
					: FindFileAbove(Path.GetFullPath(Path.Combine(directory, start)), arguments[0]);
			}

			return Expand(project) is { Length: > 0 } expanded
				? Path.GetFullPath(Path.Combine(directory, expanded))
				: null;
		}

		private string? Metadata(XElement item, string name)
		{
			// Item metadata can be an attribute or a child element: Static="true" or <Static>true</Static>.
			string? raw = item.Attribute(name)?.Value
			              ?? item.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
			return raw is null ? null : Expand(raw);
		}

		/// <summary>
		///     Replaces <c>$(Name)</c> with the properties read so far, which covers the
		///     <c>$(DefineConstants);EXTRA</c> pattern. Returns <c>null</c> for anything that needs
		///     MSBuild to evaluate: property functions, item lists and metadata.
		/// </summary>
		private string? Expand(string value)
		{
			var result = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				if (c is ('$' or '@' or '%') && i + 1 < value.Length && value[i + 1] == '(')
				{
					int end = value.IndexOf(')', i + 2);
					string name = end < 0 ? "" : value[(i + 2)..end];
					if (c != '$' || !IsPropertyName(name))
					{
						return null;
					}

					result.Append(_properties.GetValueOrDefault(name));
					i = end;
				}
				else
				{
					result.Append(c);
				}
			}

			return result.ToString().Trim();
		}

		private static bool IsPropertyName(string name) =>
			name.Length > 0
			&& (char.IsLetter(name[0]) || name[0] == '_')
			&& name.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
	}

	#endregion

	#region Assets file

	private sealed record AssetsLibrary(string Type, string RelativePath);

	private sealed record TargetLibrary(string Name, string Type, string? Framework, List<string> CompileAssets);

	/// <summary>
	///     The parts of NuGet's project.assets.json the analyzer uses.
	/// </summary>
	private sealed class ProjectAssets
	{
		private readonly Dictionary<string, List<string>> _frameworkReferences = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, AssetsLibrary> _libraries = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<string> _packageFolders = [];
		private readonly Dictionary<string, List<TargetLibrary>> _targets = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>The target framework monikers the project was restored for.</summary>
		public List<string> TargetFrameworks { get; } = [];

		public static ProjectAssets? TryLoad(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}

			try
			{
				using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
				JsonElement root = document.RootElement;
				var assets = new ProjectAssets();

				foreach (JsonProperty folder in Properties(root, "packageFolders"))
				{
					assets._packageFolders.Add(folder.Name);
				}

				foreach (JsonProperty library in Properties(root, "libraries"))
				{
					assets._libraries[library.Name] = new AssetsLibrary(
						ReadString(library.Value, "type") ?? "",
						ReadString(library.Value, "path") ?? "");
				}

				foreach (JsonProperty target in Properties(root, "targets"))
				{
					assets._targets[target.Name] = Properties(target.Value)
						.Select(library => new TargetLibrary(
							library.Name,
							ReadString(library.Value, "type") ?? "",
							ReadString(library.Value, "framework"),
							Properties(library.Value, "compile").Select(asset => asset.Name).ToList()))
						.ToList();
				}

				JsonElement project = root.ValueKind == JsonValueKind.Object
				                      && root.TryGetProperty("project", out JsonElement projectElement)
					? projectElement
					: default;
				foreach (JsonProperty framework in Properties(project, "frameworks"))
				{
					assets.TargetFrameworks.Add(framework.Name);
					assets._frameworkReferences[framework.Name] = Properties(framework.Value, "frameworkReferences")
						.Select(reference => reference.Name)
						.ToList();
				}

				return assets;
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				// A corrupt or half-written assets file costs type resolution, not the build.
				return null;
			}
		}

		public IEnumerable<string> GetFrameworkReferences(string? targetFramework) =>
			targetFramework is not null && _frameworkReferences.TryGetValue(targetFramework, out List<string>? names)
				? names
				: [];

		public List<string> GetCompileAssemblies(string? targetFramework, string projectDirectory)
		{
			var assemblies = new List<string>();
			if (FindTarget(targetFramework) is not { } target)
			{
				return assemblies;
			}

			foreach (TargetLibrary library in target)
			{
				if (!_libraries.TryGetValue(library.Name, out AssetsLibrary? info))
				{
					continue;
				}

				foreach (string asset in library.CompileAssets)
				{
					// NuGet writes "_._" when a package has nothing to compile against for the framework.
					if (Path.GetFileName(asset) == "_._")
					{
						continue;
					}

					string? assembly = library.Type switch
					{
						"package" => FindPackageAsset(info.RelativePath, asset),
						"project" => FindProjectOutput(projectDirectory, info.RelativePath, Path.GetFileName(asset),
							library.Framework),
						_ => null
					};

					if (assembly is not null)
					{
						assemblies.Add(assembly);
					}
				}
			}

			return assemblies;
		}

		private List<TargetLibrary>? FindTarget(string? targetFramework)
		{
			if (targetFramework is not null)
			{
				// Assets files from recent NuGet versions key targets by moniker, older ones by framework name.
				if (_targets.TryGetValue(targetFramework, out List<TargetLibrary>? byMoniker))
				{
					return byMoniker;
				}

				if (TargetFrameworkMoniker.TryParse(targetFramework) is { } moniker
				    && _targets.TryGetValue(moniker.FrameworkName, out List<TargetLibrary>? byName))
				{
					return byName;
				}
			}

			// Runtime-specific targets ("net10.0/win-x64") repeat the plain one.
			List<string> plain = _targets.Keys.Where(key => !key.Contains('/')).ToList();
			return plain.Count == 1 ? _targets[plain[0]] : null;
		}

		private string? FindPackageAsset(string libraryPath, string asset)
		{
			string relative = Path.Combine(libraryPath, asset).Replace('/', Path.DirectorySeparatorChar);
			return _packageFolders
				.Select(folder => Path.Combine(folder, relative))
				.FirstOrDefault(File.Exists);
		}

		private static string? FindProjectOutput(
			string projectDirectory,
			string libraryPath,
			string fileName,
			string? framework)
		{
			// NuGet records a placeholder (bin/placeholder/Name.dll) for a project reference. The
			// real assembly only exists once that project is built, somewhere under its bin folder.
			string referencedProject = Path.GetFullPath(
				Path.Combine(projectDirectory, libraryPath.Replace('/', Path.DirectorySeparatorChar)));
			string bin = Path.Combine(Path.GetDirectoryName(referencedProject) ?? "", "bin");
			if (!Directory.Exists(bin))
			{
				return null;
			}

			string? moniker = TargetFrameworkMoniker.AliasFor(framework);
			return new DirectoryInfo(bin)
				.EnumerateFiles(fileName, SearchOption.AllDirectories)
				.OrderByDescending(file => string.Equals(file.Directory?.Name, moniker, StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(file => file.LastWriteTimeUtc)
				.Select(file => file.FullName)
				.FirstOrDefault();
		}

		private static IEnumerable<JsonProperty> Properties(JsonElement element, string? name = null)
		{
			if (element.ValueKind != JsonValueKind.Object)
			{
				return [];
			}

			if (name is null)
			{
				return element.EnumerateObject();
			}

			return element.TryGetProperty(name, out JsonElement child) && child.ValueKind == JsonValueKind.Object
				? child.EnumerateObject()
				: [];
		}

		private static string? ReadString(JsonElement element, string name) =>
			element.ValueKind == JsonValueKind.Object
			&& element.TryGetProperty(name, out JsonElement value)
			&& value.ValueKind == JsonValueKind.String
				? value.GetString()
				: null;
	}

	#endregion
}
