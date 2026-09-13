using System.IO.Abstractions;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;
using Moka.Docs.Engine.Caching;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Analyzes C# projects to build the <see cref="ApiReference" /> model.
/// </summary>
public sealed class CSharpAnalysisPhase(
	AssemblyAnalyzer analyzer,
	InheritDocResolver inheritDocResolver,
	BuildCache cache,
	ILogger<CSharpAnalysisPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "CSharpAnalysis";

	/// <inheritdoc />
	public int Order => 300;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (context.Config.Content.Projects.Count == 0)
		{
			logger.LogInformation("No C# projects configured, skipping analysis");
			return Task.CompletedTask;
		}

		var allNamespaces = new List<ApiNamespace>();
		var allAssemblies = new List<string>();

		foreach (ProjectSource project in context.Config.Content.Projects)
		{
			ct.ThrowIfCancellationRequested();

			string projectPath = context.FileSystem.Path.GetFullPath(
				context.FileSystem.Path.Combine(context.RootDirectory, project.Path));

			if (!context.FileSystem.File.Exists(projectPath))
			{
				context.Diagnostics.Warning($"Project file not found: {project.Path}", Name);
				continue;
			}

			string projectDir = context.FileSystem.Path.GetDirectoryName(projectPath) ?? "";
			string assemblyName = project.Label ?? context.FileSystem.Path.GetFileNameWithoutExtension(projectPath);

			try
			{
				// Like the analyzer, this reads the real disk: the references, usings and symbols
				// come from files next to the sources.
				CSharpProjectInfo projectInfo = CSharpProjectInfo.Load(projectPath);

				// Roslyn analysis dominates build time, so a cache hit here is most of the
				// difference between a warm and a cold build.
				string fingerprint = context.UseCache
					? BuildCache.ComputeFingerprint(projectDir, projectInfo.InputFiles, project.IncludeInternals)
					: "";

				ApiReference? apiRef = context.UseCache && fingerprint.Length > 0
					? cache.TryGet(context.RootDirectory, projectPath, fingerprint)
					: null;

				if (apiRef is null)
				{
					logger.LogInformation("Analyzing project: {Name} ({Path})", assemblyName, project.Path);
					apiRef = analyzer.AnalyzeProject(projectInfo, assemblyName, project.IncludeInternals);

					if (context.UseCache && fingerprint.Length > 0)
					{
						cache.Set(context.RootDirectory, projectPath, fingerprint, apiRef);
					}
				}

				allNamespaces.AddRange(apiRef.Namespaces);
				allAssemblies.AddRange(apiRef.Assemblies);
			}
			catch (Exception ex)
			{
				context.Diagnostics.Warning($"Failed to analyze {assemblyName}: {ex.Message}", Name);
				logger.LogWarning(ex, "Failed to analyze project: {Path}", project.Path);
			}
		}

		if (allNamespaces.Count > 0)
		{
			var combined = new ApiReference
			{
				Assemblies = allAssemblies,
				Namespaces = allNamespaces
					.GroupBy(ns => ns.Name)
					.Select(g => new ApiNamespace
					{
						Name = g.Key,
						Types = g.SelectMany(ns => ns.Types).OrderBy(t => t.Name).ToList()
					})
					.OrderBy(ns => ns.Name)
					.ToList()
			};

			// Resolve inheritdoc
			context.ApiModel = inheritDocResolver.Resolve(combined);

			logger.LogInformation("Extracted {TypeCount} types in {NsCount} namespaces",
				context.ApiModel.Namespaces.Sum(n => n.Types.Count),
				context.ApiModel.Namespaces.Count);

			// Extract package metadata for the NuGet install widget
			context.PackageInfo = ExtractPackageMetadata(context);

			// Generate API pages
			GenerateApiPages(context);
		}

		return Task.CompletedTask;
	}

	private PackageMetadata? ExtractPackageMetadata(BuildContext context)
	{
		ProjectSource? packageProject = SelectPackageProject(context);
		if (packageProject is null)
		{
			return null;
		}

		string projectPath = context.FileSystem.Path.GetFullPath(
			context.FileSystem.Path.Combine(context.RootDirectory, packageProject.Path));

		if (!context.FileSystem.File.Exists(projectPath))
		{
			return null;
		}

		try
		{
			string csprojContent = context.FileSystem.File.ReadAllText(projectPath);
			var doc = XDocument.Parse(csprojContent);

			// Look for PackageId, then AssemblyName, then fall back to file name
			string packageId = doc.Descendants("PackageId").FirstOrDefault()?.Value
			                   ?? doc.Descendants("AssemblyName").FirstOrDefault()?.Value
			                   ?? context.FileSystem.Path.GetFileNameWithoutExtension(projectPath);

			// The project file first, then Directory.Build.props files above it. Projects that set
			// the version once in Directory.Build.props, as this repo does, used to show 1.0.0.
			// That stays the fallback, since it's what MSBuild packs when nothing sets a version.
			string version = ReadVersion(doc) ?? ReadDirectoryBuildPropsVersion(context.FileSystem, projectPath)
			                 ?? "1.0.0";

			logger.LogInformation("Extracted package metadata: {Name} v{Version}", packageId, version);

			return new PackageMetadata { Name = packageId, Version = version };
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to extract package metadata from {Path}", projectPath);
			return null;
		}
	}

	/// <summary>
	///     Picks the project the install widget describes: the first configured project that
	///     produces a package, or the first project when none does.
	/// </summary>
	/// <remarks>
	///     The widget used to describe the first project whatever it was, so a site that listed a
	///     test or sample project first told readers to install a package that does not exist.
	/// </remarks>
	private static ProjectSource? SelectPackageProject(BuildContext context)
	{
		IFileSystem fs = context.FileSystem;
		foreach (ProjectSource project in context.Config.Content.Projects)
		{
			string projectPath = fs.Path.GetFullPath(fs.Path.Combine(context.RootDirectory, project.Path));
			if (fs.File.Exists(projectPath) && IsPackable(fs, projectPath))
			{
				return project;
			}
		}

		return context.Config.Content.Projects.FirstOrDefault();
	}

	private static bool IsPackable(IFileSystem fs, string projectPath)
	{
		if (string.Equals(ReadProjectProperty(fs, projectPath, "IsTestProject"), "true", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (ReadProjectProperty(fs, projectPath, "IsPackable") is { } isPackable)
		{
			return !string.Equals(isPackable, "false", StringComparison.OrdinalIgnoreCase);
		}

		// Application SDKs default IsPackable to false; the others default it to true.
		string sdk = TryParseXml(fs, projectPath)?.Root?.Attribute("Sdk")?.Value ?? "";
		return !sdk.Split(';')
			.Select(name => name.Split('/')[0].Trim())
			.Any(name => name.Equals("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)
			             || name.Equals("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase)
			             || name.Equals("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	///     Reads an unconditional property from the project file, then from the
	///     <c>Directory.Build.props</c> files above it, nearest first.
	/// </summary>
	private static string? ReadProjectProperty(IFileSystem fs, string projectPath, string name)
	{
		if (TryParseXml(fs, projectPath) is { } project && ReadProperty(project, name) is { } value)
		{
			return value;
		}

		for (string? dir = fs.Path.GetDirectoryName(projectPath); dir is not null; dir = fs.Path.GetDirectoryName(dir))
		{
			string props = fs.Path.Combine(dir, "Directory.Build.props");
			if (fs.File.Exists(props) && TryParseXml(fs, props) is { } doc && ReadProperty(doc, name) is { } inherited)
			{
				return inherited;
			}
		}

		return null;
	}

	private static XDocument? TryParseXml(IFileSystem fs, string path)
	{
		try
		{
			return XDocument.Parse(fs.File.ReadAllText(path));
		}
		catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	/// <summary>
	///     Walks up from the project's folder and returns the version from the nearest
	///     <c>Directory.Build.props</c> that sets one.
	/// </summary>
	private static string? ReadDirectoryBuildPropsVersion(IFileSystem fs, string projectPath)
	{
		for (string? dir = fs.Path.GetDirectoryName(projectPath); dir is not null; dir = fs.Path.GetDirectoryName(dir))
		{
			string props = fs.Path.Combine(dir, "Directory.Build.props");
			if (fs.File.Exists(props) && ReadVersion(XDocument.Parse(fs.File.ReadAllText(props))) is { } version)
			{
				return version;
			}
		}

		return null;
	}

	/// <summary>
	///     Reads PackageVersion, Version or VersionPrefix (plus VersionSuffix) from unconditional
	///     property groups. Values built from other properties, such as <c>$(Major).1</c>, are skipped
	///     because nothing here evaluates MSBuild.
	/// </summary>
	private static string? ReadVersion(XDocument doc)
	{
		string? version = ReadProperty(doc, "PackageVersion") ?? ReadProperty(doc, "Version");
		if (version is not null)
		{
			return version;
		}

		string? prefix = ReadProperty(doc, "VersionPrefix");
		string? suffix = ReadProperty(doc, "VersionSuffix");
		return prefix is null ? null : suffix is null ? prefix : $"{prefix}-{suffix}";
	}

	/// <summary>
	///     Reads the last unconditional definition of a property, skipping values built from other
	///     properties such as <c>$(Major).1</c>.
	/// </summary>
	private static string? ReadProperty(XDocument doc, string name)
	{
		string? value = doc.Root?.Elements()
			.Where(e => e.Name.LocalName == "PropertyGroup" && e.Attribute("Condition") is null)
			.Elements()
			.LastOrDefault(e => e.Name.LocalName == name && e.Attribute("Condition") is null)
			?.Value.Trim();

		return string.IsNullOrEmpty(value) || value.Contains("$(") ? null : value;
	}

	private static void GenerateApiPages(BuildContext context)
	{
		if (context.ApiModel is null)
		{
			return;
		}

		// Collect all types across namespaces for cref links and type dependency graph lookups
		var allTypes = context.ApiModel.Namespaces.SelectMany(n => n.Types).ToList();

		// Generate API index page listing all namespaces and types
		var indexHtml = new StringBuilder();
		foreach (ApiNamespace ns in context.ApiModel.Namespaces)
		{
			indexHtml.AppendLine($"<h2>{HttpUtility.HtmlEncode(ns.Name)}</h2>");
			indexHtml.AppendLine("<div class=\"table-responsive\"><table class=\"api-member-table\">");
			indexHtml.AppendLine("<thead><tr><th>Name</th><th>Kind</th><th>Description</th></tr></thead>");
			indexHtml.AppendLine("<tbody>");
			foreach (ApiType type in ns.Types)
			{
				string route = $"/api/{ns.Name.Replace('.', '/')}/{type.Name}".ToLowerInvariant();
				string kindBadge = type.Kind.ToString().ToLowerInvariant();
				// Resolved like the type page: raw <see cref> anchors had no href in this table.
				string summary = ApiPageRenderer.RenderSummary(type, allTypes);
				indexHtml.AppendLine("<tr>");
				indexHtml.AppendLine($"<td><a href=\"{route}\">{HttpUtility.HtmlEncode(type.Name)}</a></td>");
				indexHtml.AppendLine($"<td><span class=\"api-badge api-badge-{kindBadge}\">{type.Kind}</span></td>");
				indexHtml.AppendLine($"<td>{summary}</td>");
				indexHtml.AppendLine("</tr>");
			}

			indexHtml.AppendLine("</tbody>");
			indexHtml.AppendLine("</table></div>");
		}

		var indexPage = new DocPage
		{
			FrontMatter = new FrontMatter
			{
				Title = "API Reference",
				Layout = "default"
			},
			Content = new PageContent
			{
				Html = indexHtml.ToString(),
				PlainText = ""
			},
			Route = "/api",
			Origin = PageOrigin.ApiGenerated
		};
		context.Pages.Add(indexPage);

		foreach (ApiNamespace ns in context.ApiModel.Namespaces)
		foreach (ApiType type in ns.Types)
		{
			string safeName = SanitizeRoutePart(type.Name);
			string route = $"/api/{ns.Name.Replace('.', '/')}/{safeName}".ToLowerInvariant();
			string apiHtml = ApiPageRenderer.RenderType(type, allTypes);
			TableOfContents toc = ApiPageRenderer.BuildTocForType(type);
			var page = new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = type.Name,
					Description = ApiDocText.ToPlainText(type.Documentation?.Summary) is { Length: > 0 } description
						? description
						: $"API documentation for {type.FullName}",
					Layout = "default"
				},
				Content = new PageContent
				{
					Html = apiHtml,
					PlainText = ApiDocText.ToPlainText(type.Documentation?.Summary)
				},
				TableOfContents = toc,
				Route = route,
				Origin = PageOrigin.ApiGenerated
			};

			context.Pages.Add(page);
		}
	}

	private static string SanitizeRoutePart(string name) =>
		name.Replace('<', '-').Replace('>', '-').Replace('`', '-').TrimEnd('-');
}
