using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Writes theme CSS and JS assets to the output directory.
/// </summary>
public sealed class ThemeAssetPhase(ILogger<ThemeAssetPhase> logger) : IBuildPhase
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
