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
	public ThemeRenderContext Load(string themePath) => Load(themePath, fileSystem);

	/// <summary>
	///     Loads a theme using a specific file system rather than the injected one.
	///     The ASP.NET Core host builds over a virtual file system, so the caller has to
	///     be able to say which one to read from.
	/// </summary>
	/// <param name="themePath">Absolute path to the theme directory.</param>
	/// <param name="fs">File system to read the theme from.</param>
	/// <returns>The loaded theme context with templates, partials, and asset lists.</returns>
	public ThemeRenderContext Load(string themePath, IFileSystem fs)
	{
		ArgumentNullException.ThrowIfNull(fs);
		var templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var partials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var cssFiles = new List<string>();
		var jsFiles = new List<string>();

		// Load layout templates
		string layoutsDir = fs.Path.Combine(themePath, "layouts");
		if (fs.Directory.Exists(layoutsDir))
		{
			foreach (string file in fs.Directory.GetFiles(layoutsDir, "*.html"))
			{
				string name = fs.Path.GetFileNameWithoutExtension(file);
				templates[name] = fs.File.ReadAllText(file);
				logger.LogDebug("Loaded layout: {Name}", name);
			}
		}

		// Load partials
		string partialsDir = fs.Path.Combine(themePath, "partials");
		if (fs.Directory.Exists(partialsDir))
		{
			foreach (string file in fs.Directory.GetFiles(partialsDir, "*.html"))
			{
				string name = fs.Path.GetFileNameWithoutExtension(file);
				partials[name] = fs.File.ReadAllText(file);
				logger.LogDebug("Loaded partial: {Name}", name);
			}
		}

		// Discover CSS files
		string cssDir = fs.Path.Combine(themePath, "css");
		if (fs.Directory.Exists(cssDir))
		{
			cssFiles = fs.Directory
				.GetFiles(cssDir, "*.css", SearchOption.AllDirectories)
				.Select(f => "/_theme/" + fs.Path.GetRelativePath(themePath, f).Replace('\\', '/'))
				.OrderBy(f => f) // main.css first via alphabetical
				.ToList();
		}

		// Discover JS files
		string jsDir = fs.Path.Combine(themePath, "js");
		if (fs.Directory.Exists(jsDir))
		{
			jsFiles = fs.Directory
				.GetFiles(jsDir, "*.js")
				.Select(f => "/_theme/" + fs.Path.GetRelativePath(themePath, f).Replace('\\', '/'))
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
	public void CopyAssets(string themePath, string outputDir) =>
		CopyAssets(themePath, outputDir, fileSystem);

	/// <summary>
	///     Copies theme static assets using a specific file system.
	/// </summary>
	/// <param name="themePath">Source theme directory.</param>
	/// <param name="outputDir">Target output directory.</param>
	/// <param name="fs">File system to read and write through.</param>
	public void CopyAssets(string themePath, string outputDir, IFileSystem fs)
	{
		ArgumentNullException.ThrowIfNull(fs);
		string themeOutputDir = fs.Path.Combine(outputDir, "_theme");
		string[] assetDirs = new[] { "css", "js", "assets" };

		foreach (string dir in assetDirs)
		{
			string sourceDir = fs.Path.Combine(themePath, dir);
			if (!fs.Directory.Exists(sourceDir))
			{
				continue;
			}

			string[] files = fs.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string relativePath = fs.Path.GetRelativePath(themePath, file);
				string destPath = fs.Path.Combine(themeOutputDir, relativePath);
				string destDir = fs.Path.GetDirectoryName(destPath)!;

				fs.Directory.CreateDirectory(destDir);
				fs.File.Copy(file, destPath, true);
			}
		}

		logger.LogInformation("Copied theme assets to {Path}", themeOutputDir);
	}
}
