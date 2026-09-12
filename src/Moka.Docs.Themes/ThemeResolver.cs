using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Themes;

/// <summary>
///     Decides which theme a build uses: the embedded default, or a theme directory
///     named by <c>theme.name</c> in mokadocs.yaml.
/// </summary>
/// <remarks>
///     Both <c>RenderPhase</c> and <c>ThemeAssetPhase</c> need the same answer, so
///     results are memoized per (root directory, theme name). Without that they would
///     load a custom theme from disk twice per build.
/// </remarks>
public sealed class ThemeResolver(ThemeLoader loader, ILogger<ThemeResolver> logger)
{
	private readonly Dictionary<string, ResolvedTheme> _cache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     Resolves the theme for a build.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	/// <param name="rootDirectory">Directory containing mokadocs.yaml, used to resolve relative theme paths.</param>
	/// <param name="fileSystem">
	///     The build's file system. Passed explicitly rather than injected so the
	///     ASP.NET Core host's virtual file system is honoured.
	/// </param>
	/// <returns>The resolved theme.</returns>
	public ResolvedTheme Resolve(SiteConfig config, string rootDirectory, IFileSystem fileSystem)
	{
		ArgumentNullException.ThrowIfNull(config);
		ArgumentNullException.ThrowIfNull(fileSystem);

		string themeName = config.Theme.Name;
		string cacheKey = rootDirectory + "|" + themeName;

		if (_cache.TryGetValue(cacheKey, out ResolvedTheme? cached))
		{
			return cached;
		}

		ResolvedTheme resolved = ResolveCore(themeName, rootDirectory, fileSystem);
		_cache[cacheKey] = resolved;
		return resolved;
	}

	private ResolvedTheme ResolveCore(string themeName, string rootDirectory, IFileSystem fileSystem)
	{
		// "default" is reserved for the theme compiled into EmbeddedThemeProvider.
		if (string.IsNullOrWhiteSpace(themeName)
		    || string.Equals(themeName, MokaDefaults.ThemeName, StringComparison.OrdinalIgnoreCase))
		{
			return new ResolvedTheme(EmbeddedThemeProvider.CreateDefault(), null);
		}

		string themePath = fileSystem.Path.IsPathRooted(themeName)
			? themeName
			: fileSystem.Path.GetFullPath(fileSystem.Path.Combine(rootDirectory, themeName));

		if (!fileSystem.Directory.Exists(themePath))
		{
			logger.LogWarning(
				"Theme '{ThemeName}' resolved to '{ThemePath}', which does not exist. Using the default theme.",
				themeName, themePath);
			return new ResolvedTheme(EmbeddedThemeProvider.CreateDefault(), null);
		}

		ThemeRenderContext context = loader.Load(themePath, fileSystem);

		// A theme with no layouts cannot render a page. Fall back rather than fail the
		// build with a template-not-found exception on every page.
		if (context.Templates.Count == 0)
		{
			logger.LogWarning(
				"Theme directory '{ThemePath}' has no layouts/*.html templates. Using the default theme.",
				themePath);
			return new ResolvedTheme(EmbeddedThemeProvider.CreateDefault(), null);
		}

		logger.LogInformation("Using custom theme '{ThemeName}' from {ThemePath}", themeName, themePath);
		return new ResolvedTheme(context, themePath);
	}
}

/// <summary>
///     The outcome of theme resolution.
/// </summary>
/// <param name="Context">Templates, partials and asset paths for the chosen theme.</param>
/// <param name="ThemeDirectory">
///     Absolute path of the custom theme directory, or <c>null</c> when the embedded
///     default theme is in use.
/// </param>
public sealed record ResolvedTheme(ThemeRenderContext Context, string? ThemeDirectory)
{
	/// <summary>Whether the embedded default theme is in use.</summary>
	public bool IsEmbedded => ThemeDirectory is null;
}
