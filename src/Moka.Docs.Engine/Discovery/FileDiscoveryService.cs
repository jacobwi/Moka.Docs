using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Engine.Discovery;

/// <summary>
///     Scans the project directory for Markdown files, C# projects, and static assets.
/// </summary>
public sealed class FileDiscoveryService(IFileSystem fileSystem, ILogger<FileDiscoveryService> logger)
{
	private static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico",
		".pdf", ".zip", ".mp4", ".webm",
		".css", ".js", ".json", ".xml",
		".woff", ".woff2", ".ttf", ".eot"
	};

	/// <summary>
	///     Discovers all relevant files based on the site configuration.
	/// </summary>
	public DiscoveryResult Discover(string rootDirectory, SiteConfig config) =>
		Discover(rootDirectory, config, fileSystem);

	/// <summary>
	///     Discovers all relevant files using a specific filesystem abstraction.
	///     Used by ASP.NET Core integration to pass the virtual MockFileSystem.
	/// </summary>
	public DiscoveryResult Discover(string rootDirectory, SiteConfig config, IFileSystem fs)
	{
		var result = new DiscoveryResult();

		// Discover Markdown files
		string docsPath = ResolvePath(fs, rootDirectory, config.Content.Docs);
		if (fs.Directory.Exists(docsPath))
		{
			var mdFiles = fs.Directory
				.GetFiles(docsPath, "*.md", SearchOption.AllDirectories)
				.Select(f => fs.Path.GetRelativePath(docsPath, f))
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList();

			result.MarkdownFiles.AddRange(mdFiles);
			logger.LogInformation("Discovered {Count} Markdown files in {Path}", mdFiles.Count, docsPath);
		}
		else
		{
			logger.LogWarning("Documentation directory not found: {Path}", docsPath);
		}

		// Discover C# project files
		foreach (ProjectSource project in config.Content.Projects)
		{
			string projectPath = ResolvePath(fs, rootDirectory, project.Path);
			if (fs.File.Exists(projectPath))
			{
				result.ProjectFiles.Add(projectPath);
				logger.LogInformation("Discovered project: {Path}", projectPath);
			}
			else
			{
				logger.LogWarning("Project file not found: {Path}", projectPath);
			}
		}

		// Discover static assets in docs directory
		if (fs.Directory.Exists(docsPath))
		{
			var assetFiles = fs.Directory
				.GetFiles(docsPath, "*.*", SearchOption.AllDirectories)
				.Where(f => AssetExtensions.Contains(fs.Path.GetExtension(f)))
				.Select(f => fs.Path.GetRelativePath(docsPath, f))
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList();

			result.AssetFiles.AddRange(assetFiles);
			logger.LogInformation("Discovered {Count} asset files", assetFiles.Count);
		}

		return result;
	}

	private static string ResolvePath(IFileSystem fs, string rootDirectory, string relativePath)
	{
		if (fs.Path.IsPathRooted(relativePath))
		{
			return relativePath;
		}

		return fs.Path.GetFullPath(
			fs.Path.Combine(rootDirectory, relativePath));
	}
}

/// <summary>
///     The result of a file discovery scan.
/// </summary>
public sealed class DiscoveryResult
{
	/// <summary>Markdown file paths relative to the docs directory.</summary>
	public List<string> MarkdownFiles { get; } = [];

	/// <summary>Absolute paths to C# project files.</summary>
	public List<string> ProjectFiles { get; } = [];

	/// <summary>Asset file paths relative to the docs directory.</summary>
	public List<string> AssetFiles { get; } = [];

	/// <summary>Total number of discovered files.</summary>
	public int TotalCount => MarkdownFiles.Count + ProjectFiles.Count + AssetFiles.Count;
}
