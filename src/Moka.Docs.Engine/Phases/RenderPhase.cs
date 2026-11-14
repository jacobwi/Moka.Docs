using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Moka.Docs.Engine.Minification;
using Moka.Docs.Parsing.Highlighting;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Applies theme templates to every page, producing final HTML output.
/// </summary>
public sealed class RenderPhase(
	ScribanTemplateEngine templateEngine,
	ThemeResolver themeResolver,
	ILogger<RenderPhase> logger) : IBuildPhase
{
	/// <summary>Where a shared sidebar is written, relative to the output directory.</summary>
	public const string SidebarScriptPath = "_theme/js/sidebar.js";

	/// <summary>Where the page for missing addresses is written, relative to the output directory.</summary>
	public const string NotFoundPagePath = "404.html";

	/// <inheritdoc />
	public string Name => "Render";

	/// <inheritdoc />
	public int Order => 900;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		ResolvedTheme theme = themeResolver.Resolve(context.Config, context.RootDirectory, context.FileSystem);
		ThemeRenderContext themeContext = theme.Context;

		// The resolver falls back to the embedded theme and only logs why, which a quiet build
		// never shows: a typo in theme.name looked like the custom theme had been ignored.
		string themeName = context.Config.Theme.Name;
		if (theme.IsEmbedded && !string.IsNullOrWhiteSpace(themeName)
		                     && !themeName.Equals(MokaDefaults.ThemeName, StringComparison.OrdinalIgnoreCase))
		{
			context.Diagnostics.Warning(
				$"Theme '{themeName}' was not found, or its folder has no layouts/*.html; using the default theme",
				Name);
		}

		ReportUnknownLayouts(context, themeContext);
		ReportUnknownSocialIcons(context);
		ThemeText text = ThemeText.For(context.Config);
		ReportLanguageProblems(context, text);
		ReportUnknownPreset(context);
		if (theme.IsEmbedded)
		{
			ReportUnknownStyle(context);
			ReportUnknownAppearanceNames(context);
			ReportUnknownLayoutVariants(context);
			ReportUnknownBannerVariant(context);
		}

		// Update the theme context with the current config, navigation, and version data
		var renderContext = new ThemeRenderContext
		{
			Config = theme.IsEmbedded ? WithKnownAppearanceNames(context.Config) : context.Config,
			Navigation = context.Navigation,
			Templates = themeContext.Templates,
			Partials = themeContext.Partials,
			CssFiles = VersionedAssetUrls(
				theme.IsEmbedded
					? EmbeddedThemeProvider.CssFilesFor(context.Config.Theme.Options,
						SiteLanguages.Direction(context.Config.Site.Language, context.Config.Site.Direction))
					: themeContext.CssFiles,
				theme, context),
			JsFiles = VersionedAssetUrls(themeContext.JsFiles, theme, context),
			StyleVersions = theme.IsEmbedded ? EmbeddedStyleVersions(context) : new Dictionary<string, string>(),
			IconSprite = theme.IsEmbedded ? ThemeAssetPhase.VersionedUrl(EmbeddedThemeProvider.IconSpritePath, theme, context) : null,
			SearchIndexUrl = SearchIndexFile.Bytes(context) is { } searchIndex
				? "/" + SearchIndexFile.FileName + "?v=" + ThemeAssetPhase.ContentHash(searchIndex)
				: null,
			AllPages = context.Pages,
			Versions = context.Versions,
			CurrentVersion = context.CurrentVersion,
			PackageInfo = context.PackageInfo,
			BrandAssetFiles = context.BrandAssetFiles,
			Blog = context.Blog,
			SidebarScriptUrl = ShareSidebar(context, theme, ct),
			MarkdownUrls = LlmsPhase.StagedMarkdownUrls(context),
			CodeHighlighter = (code, language) => HighlightCode(code, language, text),
			Text = text
		};

		int rendered = 0;
		var failures = new Dictionary<string, (int Count, string Example)>();
		for (int i = 0; i < context.Pages.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			DocPage page = context.Pages[i];
			if (!context.IncludeDrafts && page.FrontMatter.Visibility == PageVisibility.Draft)
			{
				continue;
			}

			try
			{
				string html = templateEngine.RenderPage(page, renderContext, ct);

				// Replace the page content with the fully rendered HTML
				context.Pages[i] = page with
				{
					Content = new PageContent
					{
						Html = html,
						PlainText = page.Content.PlainText
					}
				};

				rendered++;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				// A broken layout fails every page that uses it, so each distinct error is
				// reported once with a count.
				failures[ex.Message] = failures.TryGetValue(ex.Message, out (int Count, string Example) seen)
					? (seen.Count + 1, seen.Example)
					: (1, page.Route);
				logger.LogDebug(ex, "Failed to render page: {Route}", page.Route);
			}
		}

		// An error, not a warning: the page is written without its layout, and a build that
		// exits 0 gets published.
		foreach ((string message, (int count, string example)) in failures)
		{
			context.Diagnostics.Error(
				$"Failed to render {count} page(s) (for example '{example}'): {message}", Name);
		}

		StageNotFoundPage(context, renderContext, ct);

		logger.LogInformation("Rendered {Count} pages with theme templates", rendered);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Renders <see cref="NotFoundPagePath" /> with the theme and stages it for <c>OutputPhase</c>,
	///     which writes it after the clean and not at all in a dry run. A plugin's own page wins.
	/// </summary>
	/// <remarks>
	///     It used to be hand-written HTML with the default style's markup, so it ignored the site's
	///     style, dark mode, primary color, favicon and layout variants.
	/// </remarks>
	private void StageNotFoundPage(BuildContext context, ThemeRenderContext renderContext, CancellationToken ct)
	{
		if (context.DeferredOutputFiles.ContainsKey(NotFoundPagePath))
		{
			return;
		}

		try
		{
			string html = templateEngine.RenderNotFoundPage(renderContext, ct);
			if (context.Config.Build.Minify)
			{
				html = HtmlMinifier.Minify(html);
			}

			context.DeferredOutputFiles[NotFoundPagePath] = new UTF8Encoding(false).GetBytes(html);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			context.Diagnostics.Warning(
				$"The 404 page could not be rendered with the theme, so it is a plain page instead: {ex.Message}", Name);
		}
	}

	/// <summary>
	///     Decides between an inline and a shared sidebar (<c>theme.options.sidebar</c>). A shared
	///     sidebar is rendered once and staged as <see cref="SidebarScriptPath" />; the result is its
	///     URL with a content hash, or <c>null</c> for an inline sidebar.
	/// </summary>
	/// <remarks>
	///     Every page used to hold the whole sidebar: 5012 pages with an entry each wrote 8.4 GB.
	///     The file is written by <c>OutputPhase</c> with the other staged files, after the clean and
	///     not at all in a dry run.
	/// </remarks>
	private string? ShareSidebar(BuildContext context, ResolvedTheme theme, CancellationToken ct)
	{
		ThemeOptions options = context.Config.Theme.Options;
		if (!SidebarModes.IsKnown(options.Sidebar))
		{
			context.Diagnostics.Warning(
				$"Sidebar mode '{options.Sidebar}' is not one of {string.Join(", ", SidebarModes.Names)}; using '{SidebarModes.Auto}'",
				Name);
		}

		string mode = SidebarModes.Resolve(options.Sidebar);
		int entries = CountEntries(context.Navigation?.Items ?? []);
		if (mode == SidebarModes.Inline || (mode == SidebarModes.Auto && entries <= options.SidebarInlineLimit))
		{
			return null;
		}

		if (!theme.IsEmbedded)
		{
			if (mode == SidebarModes.Shared)
			{
				context.Diagnostics.Warning(
					$"Theme '{context.Config.Theme.Name}' has no shared sidebar, so every page holds the whole sidebar",
					Name);
			}

			return null;
		}

		var sidebarContext = new ThemeRenderContext
		{
			Config = context.Config,
			Navigation = context.Navigation,
			Templates = [],
			Partials = [],
			AllPages = context.Pages,
			IconSprite = ThemeAssetPhase.VersionedUrl(EmbeddedThemeProvider.IconSpritePath, theme, context),
			Text = ThemeText.For(context.Config)
		};

		string html;
		try
		{
			html = templateEngine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(), sidebarContext, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			context.Diagnostics.Warning(
				$"The shared sidebar could not be rendered, so every page holds the whole sidebar: {ex.Message}", Name);
			return null;
		}

		string source = EmbeddedThemeProvider.CreateSidebarScript(html);
		byte[] script = new UTF8Encoding(false).GetBytes(
			context.Config.Build.ShouldMinifyAssets ? JsMinifier.Minify(source) : source);
		context.DeferredOutputFiles[SidebarScriptPath] = script;
		logger.LogInformation("The sidebar has {Entries} entries: written once to {Path} ({Bytes} bytes) for every page",
			entries, SidebarScriptPath, script.Length);

		return "/" + SidebarScriptPath + "?v=" + ThemeAssetPhase.ContentHash(script);
	}

	private static int CountEntries(List<NavigationNode> nodes) => nodes.Sum(n => 1 + CountEntries(n.Children));

	private static List<string> VersionedAssetUrls(List<string> urls, ResolvedTheme theme, BuildContext context) =>
		urls.Select(url => ThemeAssetPhase.VersionedUrl(url, theme, context)).ToList();

	private static Dictionary<string, string> EmbeddedStyleVersions(BuildContext context)
	{
		IReadOnlyDictionary<string, byte[]> assets = ThemeAssetPhase.EmbeddedAssets(context);
		return EmbeddedThemeProvider.StyleFileNames.ToDictionary(
			style => style,
			style => ThemeAssetPhase.ContentHash(assets[EmbeddedThemeProvider.StyleCssPath(style)!]),
			StringComparer.Ordinal);
	}

	/// <summary>
	///     Warns about a <c>site.language</c> that isn't a language tag (pages are English then), a
	///     <c>site.direction</c> other than <c>ltr</c> or <c>rtl</c> (the language's own is used), and each
	///     <c>theme.options.strings</c> key the theme doesn't have. Custom themes read the same text, so this
	///     applies to them too.
	/// </summary>
	private void ReportLanguageProblems(BuildContext context, ThemeText text)
	{
		string language = context.Config.Site.Language;
		if (!string.IsNullOrWhiteSpace(language) && !SiteLanguages.IsWellFormed(language))
		{
			context.Diagnostics.Warning(
				$"Site language '{language}' is not a language tag such as 'en', 'de' or 'pt-BR'; using '{SiteLanguages.Default}'",
				Name);
		}

		string direction = context.Config.Site.Direction;
		if (!SiteLanguages.IsKnownDirection(direction))
		{
			context.Diagnostics.Warning(
				$"Site direction '{direction}' is not one of {string.Join(", ", SiteLanguages.Directions)}; using " +
				$"'{SiteLanguages.Direction(text.Language, null)}' for '{text.Language}'",
				Name);
		}

		foreach (string key in text.UnknownKeys)
		{
			ThemeStringInfo? near = ThemeStrings.All.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
			context.Diagnostics.Warning(
				$"theme.options.strings: the theme has no text '{key}', so it is ignored" +
				(near is null ? "" : $"; keys are case sensitive, did you mean '{near.Key}'?"),
				Name);
		}
	}

	/// <summary>
	///     Warns when <c>theme.options.style</c> names no style of the default theme. Pages
	///     render with the default style then. A custom theme may define its own style names, so
	///     it is only checked for the embedded theme.
	/// </summary>
	private void ReportUnknownStyle(BuildContext context)
	{
		string style = context.Config.Theme.Options.Style;
		if (!ThemeStyles.IsKnown(style))
		{
			context.Diagnostics.Warning(
				$"Style '{style}' is not one of {string.Join(", ", ThemeStyles.Names)}; using '{ThemeStyles.Default}'",
				Name);
		}
	}

	/// <summary>
	///     Warns when <c>theme.options.preset</c> names no preset. The configuration reader applies
	///     nothing then, so every option keeps the value the site gave it or its default. Presets are
	///     options, not theme files, so this applies to a custom theme too.
	/// </summary>
	private void ReportUnknownPreset(BuildContext context)
	{
		string preset = context.Config.Theme.Options.Preset;
		if (!ThemePresets.IsKnown(preset))
		{
			context.Diagnostics.Warning(
				$"Theme preset '{preset}' is not one of {string.Join(", ", ThemePresets.Names)}; no preset applied",
				Name);
		}
	}

	/// <summary>
	///     Warns when <c>theme.options.banner.variant</c> names no look of the default theme's banner,
	///     which then renders as <c>info</c>. Like the style, a custom theme may have its own names.
	/// </summary>
	private void ReportUnknownBannerVariant(BuildContext context)
	{
		if (context.Config.Theme.Options.Banner is { } banner && !BannerVariants.IsKnown(banner.Variant))
		{
			context.Diagnostics.Warning(
				$"Banner variant '{banner.Variant}' is not one of {string.Join(", ", BannerVariants.Names)}; using '{BannerVariants.Info}'",
				Name);
		}
	}

	/// <summary>
	///     Warns when <c>defaultColorTheme</c>, <c>codeTheme</c> or <c>codeStyle</c> names nothing the
	///     default theme has. Like the style, a custom theme may have its own names, so they are only
	///     checked for the embedded theme.
	/// </summary>
	private void ReportUnknownAppearanceNames(BuildContext context)
	{
		ThemeOptions options = context.Config.Theme.Options;
		if (!ColorPresets.IsKnown(options.DefaultColorTheme))
		{
			context.Diagnostics.Warning(
				$"Default color theme '{options.DefaultColorTheme}' is not one of {string.Join(", ", ColorPresets.Names)}; using '{ColorPresets.Default}'",
				Name);
		}

		if (!CodeThemes.IsKnown(options.CodeTheme))
		{
			context.Diagnostics.Warning(
				$"Code theme '{options.CodeTheme}' is not one of {string.Join(", ", CodeThemes.Names)}; using '{CodeThemes.Default}'",
				Name);
		}

		if (!CodeStyles.IsKnown(options.CodeStyle))
		{
			context.Diagnostics.Warning(
				$"Code style '{options.CodeStyle}' is not one of {string.Join(", ", CodeStyles.Names)}; using '{CodeStyles.Default}'",
				Name);
		}
	}

	/// <summary>
	///     The configuration the embedded theme renders with: the color preset, code theme and code
	///     style by their canonical names, and the defaults in place of unknown ones.
	/// </summary>
	/// <remarks>
	///     An unknown code theme gave code blocks the page's light code background with Catppuccin
	///     Mocha's pastel token colors, unreadable in light mode, and a name in another case
	///     (<c>GitHub-Dark</c>) matched no stylesheet rule. An empty color preset stays empty: it means
	///     no preset.
	/// </remarks>
	private static SiteConfig WithKnownAppearanceNames(SiteConfig config)
	{
		ThemeOptions options = config.Theme.Options;
		string colorTheme = string.IsNullOrWhiteSpace(options.DefaultColorTheme)
			? options.DefaultColorTheme
			: ColorPresets.Resolve(options.DefaultColorTheme);
		string codeTheme = CodeThemes.Resolve(options.CodeTheme);
		string codeStyle = CodeStyles.Resolve(options.CodeStyle);
		if (colorTheme == options.DefaultColorTheme && codeTheme == options.CodeTheme && codeStyle == options.CodeStyle)
		{
			return config;
		}

		return config with
		{
			Theme = config.Theme with
			{
				Options = options with { DefaultColorTheme = colorTheme, CodeTheme = codeTheme, CodeStyle = codeStyle }
			}
		};
	}

	// The same markup as a fenced block: EnhanceBlocks wraps and highlights a plain <pre><code>. A
	// showcase sample gets no line numbers.
	private static string HighlightCode(string code, string? language, ThemeText text)
	{
		const string wrapper = "<div class=\"code-block\"";
		string languageClass = string.IsNullOrWhiteSpace(language)
			? ""
			: $" class=\"language-{WebUtility.HtmlEncode(language.Trim())}\"";
		string block = CodeBlockHtml.EnhanceBlocks($"<pre><code{languageClass}>{WebUtility.HtmlEncode(code)}</code></pre>", text);
		return block.StartsWith(wrapper, StringComparison.Ordinal)
			? wrapper + " data-line-numbers=\"false\"" + block[wrapper.Length..]
			: block;
	}

	/// <summary>
	///     Warns about each <c>theme.options.layout</c> part that names no variant of the default
	///     theme. That part renders with its default variant. A custom theme may define its own
	///     variant names, so, like the style, it is only checked for the embedded theme.
	/// </summary>
	private void ReportUnknownLayoutVariants(BuildContext context)
	{
		ThemeLayoutOptions layout = context.Config.Theme.Options.Layout;
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			string configured = layout.NameOf(part);
			if (!part.IsKnown(configured))
			{
				context.Diagnostics.Warning(
					$"Layout {part.Key} '{configured}' is not one of {string.Join(", ", part.Names)}; using '{ThemeLayouts.Default}'",
					Name);
			}
		}
	}

	/// <summary>
	///     Warns about social links whose icon is not in the icon set. The footer prints the
	///     name as text instead, which looked like a theme bug.
	/// </summary>
	private void ReportUnknownSocialIcons(BuildContext context)
	{
		foreach (SocialLink link in context.Config.Theme.Options.SocialLinks)
		{
			if (!string.IsNullOrWhiteSpace(link.Icon) && LucideIcons.Get(link.Icon) is null)
			{
				context.Diagnostics.Warning(
					$"Social link icon '{link.Icon}' is not in the icon set; the footer shows the name as text ({link.Url})",
					Name);
			}
		}
	}

	/// <summary>
	///     Warns once per layout name that the theme does not have. Such pages render with the
	///     default layout, which used to happen without a word (layout: wide, for example).
	/// </summary>
	private void ReportUnknownLayouts(BuildContext context, ThemeRenderContext theme)
	{
		IEnumerable<IGrouping<string, DocPage>> missing = context.Pages
			.Where(p => context.IncludeDrafts || p.FrontMatter.Visibility != PageVisibility.Draft)
			.Where(p => theme.GetTemplate(p.FrontMatter.Layout) is null)
			.GroupBy(p => p.FrontMatter.Layout, StringComparer.OrdinalIgnoreCase);

		foreach (IGrouping<string, DocPage> group in missing)
		{
			string example = group.First().SourcePath ?? group.First().Route;
			context.Diagnostics.Warning(
				$"Layout '{group.Key}' does not exist in the theme; {group.Count()} page(s) use the default layout instead (for example {example})",
				Name);
		}
	}
}
