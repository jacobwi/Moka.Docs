using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Minification;
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
	private const string _themePrefix = "/_theme/";

	private static readonly IReadOnlyDictionary<string, byte[]> _embeddedAssets = CreateEmbeddedAssets(false);

	private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> _minifiedEmbeddedAssets =
		new(() => CreateEmbeddedAssets(true));

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

		foreach ((string url, byte[] bytes) in EmbeddedAssets(context.Config.Build.Minify))
		{
			string path = fs.Path.Combine([context.OutputDirectory, .. url.TrimStart('/').Split('/')]);
			fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(path)!);
			fs.File.WriteAllBytes(path, bytes);
		}

		logger.LogInformation("Wrote theme assets to {Path}", themeDir);
		return Task.CompletedTask;
	}

	// RenderPhase and the 404 page hash these same bytes into the ?v= of the URLs they write, so the
	// minified build hashes the minified files.
	internal static IReadOnlyDictionary<string, byte[]> EmbeddedAssets(bool minify) =>
		minify ? _minifiedEmbeddedAssets.Value : _embeddedAssets;

	internal static string ContentHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes))[..10];

	// The ?v= hash makes a browser or CDN holding an old main.css fetch the new one after an upgrade;
	// without it new HTML ran with old CSS and JS for up to a day. The embedded theme hashes the
	// bytes this phase writes, a custom theme the files it copies. Other URLs stay as they are.
	internal static string VersionedUrl(string url, ResolvedTheme theme, BuildContext context)
	{
		byte[]? bytes = AssetBytes(url, theme, context);
		return bytes is null ? url : url + "?v=" + ContentHash(bytes);
	}

	private static byte[]? AssetBytes(string url, ResolvedTheme theme, BuildContext context)
	{
		if (theme.IsEmbedded)
		{
			return EmbeddedAssets(context.Config.Build.Minify).GetValueOrDefault(url);
		}

		if (!url.StartsWith(_themePrefix, StringComparison.Ordinal))
		{
			return null;
		}

		IFileSystem fs = context.FileSystem;
		string path = fs.Path.Combine(theme.ThemeDirectory!, url[_themePrefix.Length..]);
		return fs.File.Exists(path) ? fs.File.ReadAllBytes(path) : null;
	}

	private static Dictionary<string, byte[]> CreateEmbeddedAssets(bool minify)
	{
		string css = EmbeddedThemeProvider.GetCss();
		string js = EmbeddedThemeProvider.GetJs();
		var utf8 = new UTF8Encoding(false);
		return new Dictionary<string, byte[]>(StringComparer.Ordinal)
		{
			["/_theme/css/main.css"] = utf8.GetBytes(minify ? CssMinifier.Minify(css) : css),
			["/_theme/js/main.js"] = utf8.GetBytes(minify ? JsMinifier.Minify(js) : js)
		};
	}
}
