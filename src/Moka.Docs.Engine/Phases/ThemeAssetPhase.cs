using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Themes;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Writes theme CSS and JS assets to the output directory.
/// </summary>
public sealed class ThemeAssetPhase(
	ThemeResolver themeResolver,
	ThemeLoader themeLoader,
	ILogger<ThemeAssetPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "ThemeAssets";

	/// <inheritdoc />
	public int Order => 1150;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		IFileSystem fs = context.FileSystem;
		string themeDir = fs.Path.Combine(context.OutputDirectory, "_theme");

		// Resolve before the dry-run check so a broken theme still surfaces its warning.
		ResolvedTheme theme = themeResolver.Resolve(context.Config, context.RootDirectory, fs);

		if (context.DryRun)
		{
			return Task.CompletedTask;
		}

		if (!theme.IsEmbedded)
		{
			// Custom theme: ship its own css/, js/ and assets/ directories instead of
			// the compiled-in stylesheet, which would not match its templates.
			themeLoader.CopyAssets(theme.ThemeDirectory!, context.OutputDirectory, fs);
			logger.LogInformation("Copied custom theme assets from {Source} to {Path}",
				theme.ThemeDirectory, themeDir);
			return Task.CompletedTask;
		}

		// Write embedded CSS
		string cssDir = fs.Path.Combine(themeDir, "css");
		fs.Directory.CreateDirectory(cssDir);
		fs.File.WriteAllText(
			fs.Path.Combine(cssDir, "main.css"),
			EmbeddedThemeProvider.GetCss());

		// Write embedded JS
		string jsDir = fs.Path.Combine(themeDir, "js");
		fs.Directory.CreateDirectory(jsDir);
		fs.File.WriteAllText(
			fs.Path.Combine(jsDir, "main.js"),
			EmbeddedThemeProvider.GetJs());

		logger.LogInformation("Wrote theme assets to {Path}", themeDir);
		return Task.CompletedTask;
	}
}
