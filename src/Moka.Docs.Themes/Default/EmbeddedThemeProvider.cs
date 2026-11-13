using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using Moka.Docs.Rendering.Scriban;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Provides the built-in "Moka" default theme with all templates, CSS, and JS.
///     This enables zero-config builds without requiring a theme directory on disk.
/// </summary>
public static partial class EmbeddedThemeProvider
{
	/// <summary>Where the base stylesheet is published: the default style, the components and the Appearance panel.</summary>
	public const string MainCssPath = "/_theme/css/main.css";

	/// <summary>Where the stylesheet of the layout variants (<c>theme.options.layout</c>) is published.</summary>
	public const string LayoutsCssPath = "/_theme/css/layouts.css";

	/// <summary>Where the client script is published.</summary>
	public const string MainJsPath = "/_theme/js/main.js";

	/// <summary>
	///     Where the right-to-left stylesheet is published. Pages link it last when the site is written right to
	///     left (<c>site.direction</c>, or a language such as Arabic or Hebrew).
	/// </summary>
	public const string RtlCssPath = "/_theme/css/rtl.css";

	private const string _stylesSharedMarker = "/* Styles: shared tokens";

	private static readonly UTF8Encoding _utf8 = new(false);

	private static readonly string _defaultTemplate = TemplateWhitespace.Compact(_defaultLayout);
	private static readonly string _landingTemplate = TemplateWhitespace.Compact(_landingLayout);
	private static readonly string _sidebarTemplate = TemplateWhitespace.Compact(_sidebarNav);

	// Components can appear on any page, so their base rules ship in main.css; their style-specific rules
	// go into the style files with the styles' own.
	private static readonly string _mainCss =
		_embeddedCss + _stylesCss[.._stylesCss.IndexOf(_stylesSharedMarker, StringComparison.Ordinal)].TrimEnd() + _componentsCss;

	/// <summary>
	///     The styles that have a stylesheet of their own: every style but <see cref="ThemeStyles.Default" />,
	///     which is the base stylesheet itself.
	/// </summary>
	public static IReadOnlyList<string> StyleFileNames { get; } = [.. ThemeStyles.Names.Where(s => s != ThemeStyles.Default)];

	// Built once: the styles' stylesheet is cut into one file per style the first time any is asked for.
	private static readonly Lazy<Dictionary<string, string>> _styleCss = new(() => StyleCssSplitter.Split(
		_stylesCss[_stylesCss.IndexOf(_stylesSharedMarker, StringComparison.Ordinal)..] + "\r\n" + _componentStylesCss, StyleFileNames));

	/// <summary>
	///     Creates a <see cref="ThemeRenderContext" /> with the embedded default theme.
	/// </summary>
	public static ThemeRenderContext CreateDefault()
	{
		return new ThemeRenderContext
		{
			Config = null!, // Set by render phase
			Templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["default"] = _defaultTemplate,
				["landing"] = _landingTemplate,
				["api-type"] = _defaultTemplate
			},
			Partials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["head"] = _headPartial,
				["nav-sidebar"] = _navSidebarPartial,
				["toc"] = _tocPartial,
				["breadcrumbs"] = _breadcrumbsPartial,
				["footer"] = _footerPartial
			},
			CssFiles = [MainCssPath],
			JsFiles = [MainJsPath],
			IconSprite = IconSpritePath
		};
	}

	/// <summary>
	///     The stylesheets a page links, in cascade order: the base stylesheet, the configured style's
	///     stylesheet unless it is the default, and the layout variants' stylesheet when any part of
	///     <c>theme.options.layout</c> isn't the default or readers can switch them
	///     (<c>theme.options.layoutSelector</c>).
	/// </summary>
	/// <remarks>
	///     The layout variants come last so a variant's shape wins over a style's at equal specificity.
	///     A style the reader picks in the Appearance panel is loaded in front of them, after the base.
	/// </remarks>
	/// <param name="options">The site's theme options.</param>
	/// <returns>Site-relative paths without the base path.</returns>
	public static List<string> CssFilesFor(ThemeOptions options) => CssFilesFor(options, SiteLanguages.LeftToRight);

	/// <summary>
	///     The stylesheets a page links, in cascade order, as <see cref="CssFilesFor(ThemeOptions)" /> lists them,
	///     followed by the right-to-left stylesheet (<see cref="RtlCssPath" />) when <paramref name="direction" />
	///     is <c>rtl</c>. It comes last so its rules win over a style's and a layout variant's.
	/// </summary>
	/// <param name="options">The site's theme options.</param>
	/// <param name="direction">The page's text direction, <c>ltr</c> or <c>rtl</c>.</param>
	/// <returns>Site-relative paths without the base path.</returns>
	public static List<string> CssFilesFor(ThemeOptions options, string direction)
	{
		ArgumentNullException.ThrowIfNull(options);

		List<string> files = [MainCssPath];
		if (StyleCssPath(options.Style) is { } style)
		{
			files.Add(style);
		}

		if (options.LayoutSelector || ThemeLayouts.Parts.Any(p => p.Resolve(options.Layout.NameOf(p)) != ThemeLayouts.Default))
		{
			files.Add(LayoutsCssPath);
		}

		if (direction == SiteLanguages.RightToLeft)
		{
			files.Add(RtlCssPath);
		}

		return files;
	}

	/// <summary>
	///     Where the stylesheet of a style is published, such as <c>/_theme/css/style-moka.css</c>, or
	///     <c>null</c> for <see cref="ThemeStyles.Default" /> and unknown names, which use the base stylesheet.
	/// </summary>
	/// <param name="style">A style name, in any case.</param>
	public static string? StyleCssPath(string? style)
	{
		string name = ThemeStyles.Resolve(style);
		return name == ThemeStyles.Default ? null : $"/_theme/css/style-{name}.css";
	}

	/// <summary>
	///     Returns the base stylesheet, published as <see cref="MainCssPath" />: the default style, the
	///     components, the code themes and the Appearance panel with its style previews.
	/// </summary>
	public static string GetCss() => _mainCss;

	/// <summary>
	///     Returns the stylesheet of one style, published at <see cref="StyleCssPath" />. Every rule in it is
	///     scoped to <c>html[data-style="name"]</c>.
	/// </summary>
	/// <param name="style">A style name, in any case.</param>
	/// <returns>The stylesheet, or an empty string for the default style and unknown names.</returns>
	public static string GetStyleCss(string? style) =>
		_styleCss.Value.GetValueOrDefault(ThemeStyles.Resolve(style)) ?? "";

	/// <summary>
	///     Returns the stylesheet of the layout variants, published as <see cref="LayoutsCssPath" />. Every
	///     rule in it is scoped to a variant other than the default.
	/// </summary>
	public static string GetLayoutsCss() => _layoutsCss.TrimStart();

	/// <summary>
	///     Returns the right-to-left stylesheet, published as <see cref="RtlCssPath" />. Every rule in it is
	///     scoped to <c>html[dir="rtl"]</c>, so a left-to-right page that loaded it would look the same.
	/// </summary>
	public static string GetRtlCss() => _rtlStylesheet.Value;

	// The base rules are written out in _rtlCss; the styles', layout variants' and components' rules are mirrored
	// from their own, after them, so a style's mirrored rule wins over the base one.
	private static readonly Lazy<string> _rtlStylesheet = new(() =>
		_rtlCss.TrimStart() + "\r\n/* Mirrored from the styles, the layout variants and the components. */\r\n" +
		RtlMirror.Mirror(_stylesCss + "\r\n" + _componentStylesCss + "\r\n" + _componentsCss + "\r\n" + _layoutsCss) + _rtlCssAfter.TrimStart());

	/// <summary>
	///     Returns the client script with the versions of the style stylesheets as <see cref="GetStyleCss" />
	///     returns them and the English text, which is what an English build that doesn't minify publishes.
	/// </summary>
	public static string GetJs() => GetJs(StyleFileNames.ToDictionary(s => s, s => ContentVersion(GetStyleCss(s))));

	/// <summary>
	///     Returns the client script, which loads a style's stylesheet when the reader picks the style.
	/// </summary>
	/// <param name="styleVersions">
	///     The <c>?v=</c> of each style's stylesheet, keyed by style name: the first ten hex characters of
	///     the SHA-256 hash of the bytes published.
	/// </param>
	public static string GetJs(IReadOnlyDictionary<string, string> styleVersions) => GetJs(styleVersions, ThemeText.English);

	/// <summary>
	///     Returns the client script in the site's language: it loads a style's stylesheet when the reader picks
	///     the style, and shows the texts of <see cref="ScriptStringKeys" /> and the Appearance panel's names from
	///     <paramref name="text" />. The script differs per language and per <c>theme.options.strings</c>, and so
	///     does its <c>?v=</c>.
	/// </summary>
	/// <param name="styleVersions">
	///     The <c>?v=</c> of each style's stylesheet, keyed by style name: the first ten hex characters of
	///     the SHA-256 hash of the bytes published.
	/// </param>
	/// <param name="text">The site's text (<see cref="ThemeText.For(SiteConfig)" />).</param>
	public static string GetJs(IReadOnlyDictionary<string, string> styleVersions, ThemeText text)
	{
		ArgumentNullException.ThrowIfNull(styleVersions);
		ArgumentNullException.ThrowIfNull(text);
		Dictionary<string, string> values = TextPlaceholders(text);
		values["/*style-files*/{}"] = JsonSerializer.Serialize(styleVersions);

		// One pass over the script, so a site's text that happens to hold a placeholder is never filled in.
		return ScriptPlaceholder().Replace(_embeddedJs + _componentsJs, match => values[match.Value]);
	}

	// The same version the build writes into ?v= for a file published as UTF-8 without a byte order mark.
	private static string ContentVersion(string content) =>
		Convert.ToHexStringLower(SHA256.HashData(_utf8.GetBytes(content)))[..10];
}
