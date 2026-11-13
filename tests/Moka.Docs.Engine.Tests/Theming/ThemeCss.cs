using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The default theme's stylesheets with line feeds only, for tests that look for rules.
/// </summary>
internal static class ThemeCss
{
	/// <summary>main.css: the default style, the components and the Appearance panel.</summary>
	public static string Main { get; } = Normalize(EmbeddedThemeProvider.GetCss());

	/// <summary>layouts.css, which starts with its <c>/* Layouts:</c> comment.</summary>
	public static string Layouts { get; } = Normalize(EmbeddedThemeProvider.GetLayoutsCss());

	/// <summary>
	///     Every stylesheet in cascade order, one after the other: main.css, each style's, layouts.css. A
	///     page links main.css, at most one style's and layouts.css, in that order.
	/// </summary>
	public static string All { get; } = Main + "\n" + string.Concat(EmbeddedThemeProvider.StyleFileNames.Select(s => Style(s) + "\n")) + Layouts;

	/// <summary>The stylesheet of one style.</summary>
	public static string Style(string style) => Normalize(EmbeddedThemeProvider.GetStyleCss(style));

	private static string Normalize(string css) => css.Replace("\r\n", "\n");
}
