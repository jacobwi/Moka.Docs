using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
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

	private static readonly StaticAssets _embeddedAssets = CreateStaticAssets(false);

	private static readonly Lazy<StaticAssets> _minifiedEmbeddedAssets = new(() => CreateStaticAssets(true));

	// main.js holds the site's text, so it is made per site. A build asks for it several times (each ?v= and the
	// write); the last one made is kept per minify setting, which is all a build or a serve session needs.
	private static readonly Lock _scriptLock = new();
	private static readonly Dictionary<bool, (string Source, byte[] Bytes)> _lastScript = [];

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

		foreach ((string url, byte[] bytes) in EmbeddedAssets(context))
		{
			string path = fs.Path.Combine([context.OutputDirectory, .. url.TrimStart('/').Split('/')]);
			fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(path)!);
			fs.File.WriteAllBytes(path, bytes);
		}

		logger.LogInformation("Wrote theme assets to {Path}", themeDir);
		return Task.CompletedTask;
	}

	// RenderPhase and the 404 page hash these same bytes into the ?v= of the URLs they write, so the
	// minified build hashes the minified files, and a change to the site's text changes main.js's ?v=.
	internal static IReadOnlyDictionary<string, byte[]> EmbeddedAssets(BuildContext context) =>
		EmbeddedAssets(context.Config.Build.ShouldMinifyAssets, ThemeText.For(context.Config));

	internal static IReadOnlyDictionary<string, byte[]> EmbeddedAssets(bool minify, ThemeText text)
	{
		StaticAssets assets = minify ? _minifiedEmbeddedAssets.Value : _embeddedAssets;
		string js = EmbeddedThemeProvider.GetJs(assets.StyleVersions, text);
		byte[] script;
		lock (_scriptLock)
		{
			if (!_lastScript.TryGetValue(minify, out (string Source, byte[] Bytes) last) || last.Source != js)
			{
				last = (js, new UTF8Encoding(false).GetBytes(minify ? JsMinifier.Minify(js) : js));
				_lastScript[minify] = last;
			}

			script = last.Bytes;
		}

		return new Dictionary<string, byte[]>(assets.Files, StringComparer.Ordinal) { [EmbeddedThemeProvider.MainJsPath] = script };
	}

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
			return EmbeddedAssets(context).GetValueOrDefault(url);
		}

		if (!url.StartsWith(_themePrefix, StringComparison.Ordinal))
		{
			return null;
		}

		IFileSystem fs = context.FileSystem;
		string path = fs.Path.Combine(theme.ThemeDirectory!, url[_themePrefix.Length..]);
		return fs.File.Exists(path) ? fs.File.ReadAllBytes(path) : null;
	}

	// main.js names each style's stylesheet with the hash of the bytes written here, so the styles come first.
	private static StaticAssets CreateStaticAssets(bool minify)
	{
		var utf8 = new UTF8Encoding(false);
		byte[] Css(string css) => utf8.GetBytes(minify ? CssMinifier.Minify(css) : css);

		var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal)
		{
			[EmbeddedThemeProvider.MainCssPath] = Css(EmbeddedThemeProvider.GetCss()),
			[EmbeddedThemeProvider.LayoutsCssPath] = Css(EmbeddedThemeProvider.GetLayoutsCss()),
			[EmbeddedThemeProvider.RtlCssPath] = Css(EmbeddedThemeProvider.GetRtlCss()),
			[EmbeddedThemeProvider.IconSpritePath] = utf8.GetBytes(EmbeddedThemeProvider.GetIconSprite())
		};

		var styleVersions = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string style in EmbeddedThemeProvider.StyleFileNames)
		{
			byte[] bytes = Css(EmbeddedThemeProvider.GetStyleCss(style));
			assets[EmbeddedThemeProvider.StyleCssPath(style)!] = bytes;
			styleVersions[style] = ContentHash(bytes);
		}

		return new StaticAssets(assets, styleVersions);
	}

	private sealed record StaticAssets(Dictionary<string, byte[]> Files, Dictionary<string, string> StyleVersions);
}
