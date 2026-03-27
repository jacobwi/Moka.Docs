using System.Text;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Analyzes C# projects to build the <see cref="ApiReference" /> model.
/// </summary>
public sealed class CSharpAnalysisPhase(
	AssemblyAnalyzer analyzer,
	InheritDocResolver inheritDocResolver,
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
				logger.LogInformation("Analyzing project: {Name} ({Path})", assemblyName, project.Path);

				ApiReference apiRef = analyzer.AnalyzeDirectory(projectDir, assemblyName, project.IncludeInternals);

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

			// Extract package metadata from the first project's .csproj
			context.PackageInfo = ExtractPackageMetadata(context);

			// Generate API pages
			GenerateApiPages(context);
		}

		return Task.CompletedTask;
	}

	private PackageMetadata? ExtractPackageMetadata(BuildContext context)
	{
		ProjectSource? firstProject = context.Config.Content.Projects.FirstOrDefault();
		if (firstProject is null)
		{
			return null;
		}

		string projectPath = context.FileSystem.Path.GetFullPath(
			context.FileSystem.Path.Combine(context.RootDirectory, firstProject.Path));

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

			// Look for PackageVersion, then Version, then fall back to "1.0.0"
			string version = doc.Descendants("PackageVersion").FirstOrDefault()?.Value
			                 ?? doc.Descendants("Version").FirstOrDefault()?.Value
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

	private static void GenerateApiPages(BuildContext context)
	{
		if (context.ApiModel is null)
		{
			return;
		}

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
				string summary = type.Documentation?.Summary ?? "";
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

		// Collect all types across namespaces for type dependency graph lookups
		var allTypes = context.ApiModel.Namespaces.SelectMany(n => n.Types).ToList();

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
					Description = type.Documentation?.Summary ?? $"API documentation for {type.FullName}",
					Layout = "default"
				},
				Content = new PageContent
				{
					Html = apiHtml,
					PlainText = type.Documentation?.Summary ?? ""
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
