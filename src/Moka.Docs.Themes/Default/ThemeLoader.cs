using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Rendering.Scriban;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Loads theme templates, CSS, JS, and assets from the theme directory.
///     Supports both embedded default theme and custom theme directories.
/// </summary>
public sealed class ThemeLoader(IFileSystem fileSystem, ILogger<ThemeLoader> logger)
{
	/// <summary>
	///     Loads a theme render context from the specified theme directory.
	/// </summary>
	/// <param name="themePath">Absolute path to the theme directory.</param>
	/// <returns>The loaded theme context with templates, partials, and asset lists.</returns>
	public ThemeRenderContext Load(string themePath)
	{
		var templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var partials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var cssFiles = new List<string>();
		var jsFiles = new List<string>();

		// Load layout templates
		string layoutsDir = fileSystem.Path.Combine(themePath, "layouts");
		if (fileSystem.Directory.Exists(layoutsDir))
		{
			foreach (string file in fileSystem.Directory.GetFiles(layoutsDir, "*.html"))
			{
				string name = fileSystem.Path.GetFileNameWithoutExtension(file);
				templates[name] = fileSystem.File.ReadAllText(file);
				logger.LogDebug("Loaded layout: {Name}", name);
			}
		}

		// Load partials
		string partialsDir = fileSystem.Path.Combine(themePath, "partials");
		if (fileSystem.Directory.Exists(partialsDir))
		{
			foreach (string file in fileSystem.Directory.GetFiles(partialsDir, "*.html"))
			{
				string name = fileSystem.Path.GetFileNameWithoutExtension(file);
				partials[name] = fileSystem.File.ReadAllText(file);
				logger.LogDebug("Loaded partial: {Name}", name);
			}
		}

		// Discover CSS files
		string cssDir = fileSystem.Path.Combine(themePath, "css");
		if (fileSystem.Directory.Exists(cssDir))
		{
			cssFiles = fileSystem.Directory
				.GetFiles(cssDir, "*.css", SearchOption.AllDirectories)
				.Select(f => "/_theme/" + fileSystem.Path.GetRelativePath(themePath, f).Replace('\\', '/'))
				.OrderBy(f => f) // main.css first via alphabetical
				.ToList();
		}

		// Discover JS files
		string jsDir = fileSystem.Path.Combine(themePath, "js");
		if (fileSystem.Directory.Exists(jsDir))
		{
			jsFiles = fileSystem.Directory
				.GetFiles(jsDir, "*.js")
				.Select(f => "/_theme/" + fileSystem.Path.GetRelativePath(themePath, f).Replace('\\', '/'))
				.OrderBy(f => f)
				.ToList();
		}

		logger.LogInformation("Loaded theme: {Layouts} layouts, {Partials} partials, {Css} CSS, {Js} JS",
			templates.Count, partials.Count, cssFiles.Count, jsFiles.Count);

		return new ThemeRenderContext
		{
			Config = null!, // Will be set by the render phase
			Templates = templates,
			Partials = partials,
			CssFiles = cssFiles,
			JsFiles = jsFiles
		};
	}

	/// <summary>
	///     Copies all theme static assets (CSS, JS, fonts, icons) to the output directory.
	/// </summary>
	/// <param name="themePath">Source theme directory.</param>
	/// <param name="outputDir">Target output directory.</param>
	public void CopyAssets(string themePath, string outputDir)
	{
		string themeOutputDir = fileSystem.Path.Combine(outputDir, "_theme");
		string[] assetDirs = new[] { "css", "js", "assets" };

		foreach (string dir in assetDirs)
		{
			string sourceDir = fileSystem.Path.Combine(themePath, dir);
			if (!fileSystem.Directory.Exists(sourceDir))
			{
				continue;
			}

			string[] files = fileSystem.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string relativePath = fileSystem.Path.GetRelativePath(themePath, file);
				string destPath = fileSystem.Path.Combine(themeOutputDir, relativePath);
				string destDir = fileSystem.Path.GetDirectoryName(destPath)!;

				fileSystem.Directory.CreateDirectory(destDir);
				fileSystem.File.Copy(file, destPath, true);
			}
		}

		logger.LogInformation("Copied theme assets to {Path}", themeOutputDir);
	}
}
