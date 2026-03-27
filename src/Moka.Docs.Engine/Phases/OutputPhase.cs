using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Writes all rendered pages and copies assets to the output directory.
///     Handles output directory creation and cleaning.
/// </summary>
public sealed class OutputPhase(ILogger<OutputPhase> logger) : IBuildPhase
{
	private static readonly JsonSerializerOptions SearchJsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	/// <inheritdoc />
	public string Name => "Output";

	/// <inheritdoc />
	public int Order => 1100;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		IFileSystem fs = context.FileSystem;
		string outputDir = context.OutputDirectory;

		// Clean output directory if configured
		if (context.Config.Build.Clean && fs.Directory.Exists(outputDir))
		{
			logger.LogInformation("Cleaning output directory: {Path}", outputDir);
			fs.Directory.Delete(outputDir, true);
		}

		fs.Directory.CreateDirectory(outputDir);

		// Write each page as an HTML file
		int writtenCount = 0;
		foreach (DocPage page in context.Pages)
		{
			ct.ThrowIfCancellationRequested();

			if (page.FrontMatter.Visibility == PageVisibility.Draft)
			{
				continue;
			}

			string pagePath = RouteToFilePath(page.Route);
			string fullPath = fs.Path.Combine(outputDir, pagePath);
			string dir = fs.Path.GetDirectoryName(fullPath)!;

			fs.Directory.CreateDirectory(dir);

			// Write the fully rendered HTML (template applied by RenderPhase)
			fs.File.WriteAllText(fullPath, page.Content.Html);
			writtenCount++;
		}

		// Generate section index pages (redirects) for directories without index.html
		int redirectCount = GenerateSectionIndexPages(context);

		// Write 404 page
		Write404Page(context);

		// Write search index JSON
		WriteSearchIndex(context);

		// Copy static assets
		int assetsCopied = CopyAssets(context);

		logger.LogInformation("Wrote {Pages} pages, {Redirects} section redirects, and {Assets} assets to {Output}",
			writtenCount, redirectCount, assetsCopied, outputDir);

		return Task.CompletedTask;
	}

	private int CopyAssets(BuildContext context)
	{
		IFileSystem fs = context.FileSystem;
		string docsPath = fs.Path.GetFullPath(
			fs.Path.Combine(context.RootDirectory, context.Config.Content.Docs));
		string outputDir = context.OutputDirectory;
		int count = 0;

		foreach (string assetPath in context.DiscoveredAssetFiles)
		{
			string sourcePath = fs.Path.Combine(docsPath, assetPath);
			string destPath = fs.Path.Combine(outputDir, assetPath);
			string destDir = fs.Path.GetDirectoryName(destPath)!;

			if (!fs.File.Exists(sourcePath))
			{
				continue;
			}

			fs.Directory.CreateDirectory(destDir);
			fs.File.Copy(sourcePath, destPath, true);
			count++;
		}

		return count;
	}

	private void WriteSearchIndex(BuildContext context)
	{
		SearchIndex? searchIndex = context.SearchIndex;
		if (searchIndex is null || searchIndex.Count == 0)
		{
			logger.LogDebug("No search index to write");
			return;
		}

		IFileSystem fs = context.FileSystem;
		string outputPath = fs.Path.Combine(context.OutputDirectory, "search-index.json");

		// Serialize entries with compact field names for smaller payload
		string bp = context.Config.Build.BasePath;
		var entries = searchIndex.Entries.Select(e => new
		{
			t = e.Title,
			s = e.Section ?? "",
			r = bp == "/" ? e.Route : bp + e.Route,
			c = e.Content.Length > 300 ? e.Content[..300] : e.Content,
			g = e.Category
		});

		string json = JsonSerializer.Serialize(entries, SearchJsonOptions);
		fs.File.WriteAllText(outputPath, json);

		logger.LogInformation("Wrote search index ({Count} entries, {Size} bytes)",
			searchIndex.Count, json.Length);
	}

	private void Write404Page(BuildContext context)
	{
		IFileSystem fs = context.FileSystem;
		SiteConfig config = context.Config;
		string path = fs.Path.Combine(context.OutputDirectory, "404.html");

		string bp = config.Build.BasePath == "/" ? "" : config.Build.BasePath;
		string html = $"""
		               <!DOCTYPE html>
		               <html lang="en" data-theme="light" data-code-theme="{config.Theme.Options.CodeTheme}">
		               <head>
		                   <meta charset="utf-8" />
		                   <meta name="viewport" content="width=device-width, initial-scale=1" />
		                   <title>Page Not Found — {HttpUtility.HtmlEncode(config.Site.Title)}</title>
		                   <link rel="stylesheet" href="{bp}/_theme/css/main.css" />
		               </head>
		               <body>
		                   <header class="site-header">
		                       <div class="header-inner">
		                           <a class="site-logo" href="{bp}/">
		                               <span class="site-name">{HttpUtility.HtmlEncode(config.Site.Title)}</span>
		                           </a>
		                           <div class="header-actions">
		                               <button class="theme-toggle" aria-label="Toggle dark mode">
		                                   <svg class="icon-sun" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
		                                   <svg class="icon-moon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
		                               </button>
		                           </div>
		                       </div>
		                   </header>
		                   <main style="display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:calc(100vh - 200px);text-align:center;padding:2rem;">
		                       <h1 style="font-size:6rem;font-weight:800;color:var(--color-primary);margin:0;line-height:1;">404</h1>
		                       <p style="font-size:1.25rem;color:var(--color-text-secondary);margin:1rem 0 2rem;">This page could not be found.</p>
		                       <a href="{bp}/" style="display:inline-flex;align-items:center;gap:0.5rem;padding:0.625rem 1.5rem;background:var(--color-primary);color:white;border-radius:var(--radius);font-weight:600;text-decoration:none;transition:opacity 150ms;">
		                           ← Back to Home
		                       </a>
		                   </main>
		                   <footer class="site-footer">
		                       <div class="footer-inner">
		                           <span class="built-with">Built with <a href="https://mokadocs.dev">MokaDocs</a></span>
		                       </div>
		                   </footer>
		                   <script src="{bp}/_theme/js/main.js"></script>
		               </body>
		               </html>
		               """;

		fs.File.WriteAllText(path, html);
		logger.LogInformation("Generated 404.html");
	}

	private int GenerateSectionIndexPages(BuildContext context)
	{
		IFileSystem fs = context.FileSystem;
		string outputDir = context.OutputDirectory;
		int count = 0;

		// Collect all directories under the output that contain at least one
		// subdirectory with an index.html, but don't have their own index.html.
		var dirsWithIndex = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Walk all written page routes to find directories that have an index.html
		foreach (DocPage page in context.Pages)
		{
			if (page.FrontMatter.Visibility == PageVisibility.Draft)
			{
				continue;
			}

			string filePath = RouteToFilePath(page.Route);
			string fullPath = fs.Path.Combine(outputDir, filePath);
			string dir = fs.Path.GetDirectoryName(fullPath)!;
			dirsWithIndex.Add(dir);
		}

		// For each directory that has an index.html, check whether its parent
		// directory is missing an index.html. If so, generate a redirect.
		var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (string dir in dirsWithIndex)
		{
			// Walk up from each page directory, generating redirects for any
			// ancestor (up to but not including the output root) that lacks index.html.
			string? parent = fs.Path.GetDirectoryName(dir);
			while (parent != null
			       && parent.Length > outputDir.Length
			       && parent.StartsWith(outputDir, StringComparison.OrdinalIgnoreCase))
			{
				if (!processed.Add(parent))
				{
					break; // already handled this parent
				}

				string parentIndex = fs.Path.Combine(parent, "index.html");
				if (fs.File.Exists(parentIndex))
					// This directory already has an index page; stop walking up.
				{
					break;
				}

				// Find the first child subdirectory (alphabetically) that has an index.html
				var childDirs = fs.Directory.GetDirectories(parent)
					.OrderBy(d => fs.Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
					.ToList();

				string? targetSubDir = null;
				foreach (string child in childDirs)
				{
					string childIndex = fs.Path.Combine(child, "index.html");
					if (fs.File.Exists(childIndex))
					{
						targetSubDir = fs.Path.GetFileName(child);
						break;
					}
				}

				if (targetSubDir is null)
				{
					// No child with an index page; skip.
					parent = fs.Path.GetDirectoryName(parent);
					continue;
				}

				string redirectUrl = $"./{targetSubDir}/";
				string redirectHtml = $"""
				                       <!DOCTYPE html>
				                       <html>
				                       <head><meta http-equiv="refresh" content="0; url={redirectUrl}"><link rel="canonical" href="{redirectUrl}"></head>
				                       <body><p>Redirecting to <a href="{redirectUrl}">{redirectUrl}</a>...</p></body>
				                       </html>
				                       """;

				fs.File.WriteAllText(parentIndex, redirectHtml);
				count++;

				string relativePath = parentIndex[outputDir.Length..].TrimStart(
					fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar);
				logger.LogDebug("Generated section redirect: {Path} -> {Target}", relativePath, redirectUrl);

				parent = fs.Path.GetDirectoryName(parent);
			}
		}

		if (count > 0)
		{
			logger.LogInformation("Generated {Count} section index redirect(s)", count);
		}

		return count;
	}

	private static string RouteToFilePath(string route)
	{
		string path = route.Trim('/');
		if (string.IsNullOrEmpty(path))
		{
			return "index.html";
		}

		return path + "/index.html";
	}
}
