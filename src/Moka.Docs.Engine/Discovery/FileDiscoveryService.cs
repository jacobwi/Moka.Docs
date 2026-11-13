using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Engine.Discovery;

/// <summary>
///     Scans the project directory for Markdown files, C# projects, and static assets.
/// </summary>
public sealed class FileDiscoveryService(IFileSystem fileSystem, ILogger<FileDiscoveryService> logger)
{
	private static readonly HashSet<string> _assetExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico",
		".pdf", ".zip", ".mp4", ".webm",
		".css", ".js", ".json", ".xml",
		".woff", ".woff2", ".ttf", ".eot",
		".txt", ".webmanifest", ".avif"
	};

	// Files static hosts read from the site root, which have no extension: CNAME sets a GitHub
	// Pages custom domain, _redirects and _headers configure Netlify and Cloudflare Pages.
	// They were never copied, so a documented custom-domain setup silently did nothing.
	private static readonly HashSet<string> _rootHostFiles = new(StringComparer.Ordinal)
	{
		"CNAME", "_redirects", "_headers"
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

		string docsPath = ResolvePath(fs, rootDirectory, config.Content.Docs);
		result.DocsDirectory = docsPath;

		// Nothing under the build output may be discovered. Users reasonably point
		// build.output at a path inside content.docs (./docs/_site), and without this
		// every build would ingest the previous build's assets and nest a copy of the
		// site inside itself, growing on each run.
		string outputPath = ResolvePath(fs, rootDirectory, config.Build.Output);
		if (fs.Directory.Exists(docsPath))
		{
			List<string> files = CollectFiles(fs, docsPath, outputPath, result.Warnings)
				.Where(f => !IsUnder(fs, f, outputPath))
				.ToList();

			// ".md" in any case on every OS. The glob this replaced matched Readme.MD on Windows
			// and macOS but not on Linux, so the same checkout built different sites.
			var mdFiles = files
				.Where(f => string.Equals(fs.Path.GetExtension(f), ".md", StringComparison.OrdinalIgnoreCase))
				.Select(f => fs.Path.GetRelativePath(docsPath, f))
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList();

			result.MarkdownFiles.AddRange(mdFiles);
			logger.LogInformation("Discovered {Count} Markdown files in {Path}", mdFiles.Count, docsPath);

			var assetFiles = files
				.Where(f => _assetExtensions.Contains(fs.Path.GetExtension(f))
				            || (_rootHostFiles.Contains(fs.Path.GetFileName(f))
				                && string.Equals(fs.Path.GetDirectoryName(f), docsPath, StringComparison.OrdinalIgnoreCase)))
				.Select(f => fs.Path.GetRelativePath(docsPath, f))
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList();

			result.AssetFiles.AddRange(assetFiles);
			logger.LogInformation("Discovered {Count} asset files", assetFiles.Count);
		}
		else
		{
			result.DocsDirectoryMissing = true;
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

		return result;
	}

	/// <summary>
	///     Every file under the docs folder, following directory links unless one points back at
	///     a folder it sits in.
	/// </summary>
	/// <remarks>
	///     A link such as <c>docs/loop</c> pointing at <c>docs</c> made the recursive glob walk the
	///     same folder again and again until the path got too long, building the same pages dozens
	///     of times. Each folder carries the real paths of the folders above it, and a link whose
	///     target is one of them, or holds one of them, is reported and skipped.
	/// </remarks>
	private static List<string> CollectFiles(IFileSystem fs, string docsPath, string outputPath, List<string> warnings)
	{
		StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		string output = fs.Path.TrimEndingDirectorySeparator(fs.Path.GetFullPath(outputPath));
		var files = new List<string>();
		string root = fs.Path.TrimEndingDirectorySeparator(fs.Path.GetFullPath(docsPath));
		Walk(root, [root]);
		return files;

		void Walk(string directory, List<string> realChain)
		{
			if (directory.Equals(output, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			string[] entries;
			string[] subdirectories;
			try
			{
				entries = fs.Directory.GetFiles(directory);
				subdirectories = fs.Directory.GetDirectories(directory);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				warnings.Add($"Skipped '{fs.Path.GetRelativePath(docsPath, directory)}': {ex.Message}");
				return;
			}

			// An older build left in the docs folder (./docs/_site after build.output moved elsewhere) was
			// read as content: its search index, theme files and images were published again inside the
			// new site, and its Markdown copies of the pages would become pages.
			if (!directory.Equals(root, StringComparison.Ordinal)
			    && entries.Any(e => fs.Path.GetFileName(e) == OutputDirectoryGuard.MarkerFileName))
			{
				warnings.Add(
					$"Skipped '{fs.Path.GetRelativePath(docsPath, directory)}': it holds a site MokaDocs built " +
					$"({OutputDirectoryGuard.MarkerFileName}), not docs. Delete it or move it out of the docs folder.");
				return;
			}

			files.AddRange(entries);

			foreach (string subdirectory in subdirectories.Order(StringComparer.Ordinal))
			{
				string real = fs.Path.Combine(realChain[^1], fs.Path.GetFileName(subdirectory));
				IDirectoryInfo info = fs.DirectoryInfo.New(subdirectory);
				if (info.LinkTarget is not null)
				{
					string shown = fs.Path.GetRelativePath(docsPath, subdirectory);
					if (ResolveLink(fs, info) is not { } target)
					{
						warnings.Add($"Skipped '{shown}': it is a link whose target does not exist");
						continue;
					}

					if (realChain.Any(ancestor => IsSameOrBelow(ancestor, target, comparison)))
					{
						warnings.Add(
							$"Skipped '{shown}': it links to '{target}', which contains it, so following it would loop forever");
						continue;
					}

					real = target;
				}

				Walk(subdirectory, [.. realChain, real]);
			}
		}
	}

	private static string? ResolveLink(IFileSystem fs, IDirectoryInfo link)
	{
		try
		{
			IFileSystemInfo? target = link.ResolveLinkTarget(true);
			return target is { Exists: true }
				? fs.Path.TrimEndingDirectorySeparator(fs.Path.GetFullPath(target.FullName))
				: null;
		}
		catch (IOException)
		{
			return null;
		}
	}

	private static bool IsSameOrBelow(string path, string directory, StringComparison comparison) =>
		path.Equals(directory, comparison)
		|| (path.Length > directory.Length
		    && path.StartsWith(directory, comparison)
		    && (path[directory.Length] == '/' || path[directory.Length] == '\\'));

	/// <summary>
	///     Whether a discovered file sits inside the given directory.
	/// </summary>
	private static bool IsUnder(IFileSystem fs, string filePath, string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			return false;
		}

		string full = fs.Path.GetFullPath(filePath);
		string dir = fs.Path.GetFullPath(directory);

		if (!dir.EndsWith(fs.Path.DirectorySeparatorChar))
		{
			dir += fs.Path.DirectorySeparatorChar;
		}

		return full.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
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

	/// <summary>The resolved <c>content.docs</c> directory.</summary>
	public string DocsDirectory { get; set; } = "";

	/// <summary>Whether the <c>content.docs</c> directory does not exist.</summary>
	public bool DocsDirectoryMissing { get; set; }

	/// <summary>Problems found while scanning, such as directory links that loop.</summary>
	public List<string> Warnings { get; } = [];

	/// <summary>Total number of discovered files.</summary>
	public int TotalCount => MarkdownFiles.Count + ProjectFiles.Count + AssetFiles.Count;
}
