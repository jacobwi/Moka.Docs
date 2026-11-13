using System.Globalization;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Moka.Docs.Rendering.Scriban;

/// <summary>
///     Renders pages using Scriban templates from the active theme.
/// </summary>
public sealed class ScribanTemplateEngine(ILogger<ScribanTemplateEngine> logger)
{
	/// <summary>
	///     The English title of the page <see cref="RenderNotFoundPage" /> renders. The page itself uses the
	///     site's <c>notFound.title</c> text.
	/// </summary>
	public const string NotFoundTitle = "Page not found";

	/// <summary>
	///     The route the not-found page renders with. It is the page's own file, so the version
	///     menu's link to "this page" still leads somewhere.
	/// </summary>
	public const string NotFoundRoute = "/404.html";

	private const int _footerColumnLimit = 4;
	private const int _footerLinkLimit = 6;
	private const int _homeSectionLinkLimit = 5;
	private const int _outputLimit = 64 * 1024 * 1024;

	private static readonly string _mokaVersion = ReadMokaVersion();

	private readonly Dictionary<string, Template> _templateCache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     How long one page may take to render. A layout still running after that is stopped and
	///     reported as an error, since a template loop that never ends would otherwise hang the build.
	///     Defaults to 30 seconds.
	/// </summary>
	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	///     Renders a page using the specified layout template.
	/// </summary>
	/// <param name="page">The page to render.</param>
	/// <param name="themeContext">The theme rendering context with templates, config, nav, etc.</param>
	/// <returns>The fully rendered HTML string.</returns>
	public string RenderPage(DocPage page, ThemeRenderContext themeContext) =>
		RenderPage(page, themeContext, CancellationToken.None);

	/// <summary>
	///     Renders a page using the specified layout template, stopping when
	///     <paramref name="cancellationToken" /> is cancelled or <see cref="RenderTimeout" /> runs out.
	/// </summary>
	/// <param name="page">The page to render.</param>
	/// <param name="themeContext">The theme rendering context with templates, config, nav, etc.</param>
	/// <param name="cancellationToken">Cancels the render.</param>
	/// <returns>The fully rendered HTML string.</returns>
	/// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
	/// <exception cref="InvalidOperationException">The layout is missing, has errors, failed or timed out.</exception>
	public string RenderPage(DocPage page, ThemeRenderContext themeContext, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(page);
		ArgumentNullException.ThrowIfNull(themeContext);

		return Render(page, themeContext, false, cancellationToken);
	}

	/// <summary>
	///     Renders the page a host shows for an address that has no page (<c>404.html</c>) with the
	///     theme's default layout, so it looks like the rest of the site: the same style, dark mode,
	///     colors, favicon and layout variants.
	/// </summary>
	/// <remarks>
	///     The page is served at whatever address was missing, so every link in it is root-relative
	///     with the base path. It tells search engines not to index it (<c>page.noindex</c>), names no
	///     canonical URL, and leaves out breadcrumbs, prev/next links and the feedback widget.
	/// </remarks>
	/// <param name="themeContext">The theme rendering context with templates, config, nav, etc.</param>
	/// <param name="cancellationToken">Cancels the render.</param>
	/// <returns>The fully rendered HTML string.</returns>
	/// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
	/// <exception cref="InvalidOperationException">The layout is missing, has errors, failed or timed out.</exception>
	public string RenderNotFoundPage(ThemeRenderContext themeContext, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(themeContext);

		bool search = themeContext.Config.Features.Search.Enabled && themeContext.Config.Theme.Options.ShowSearch;
		ThemeText text = themeContext.Text;
		string home = $"""<a href="/">{WebUtility.HtmlEncode(text["notFound.home"])}</a>""";
		string links = search
			? WebUtility.HtmlEncode(text["notFound.links"]).Replace("{home}", home, StringComparison.Ordinal).Replace("{search}",
				$"""<a href="#search" data-open-search>{WebUtility.HtmlEncode(text["notFound.search"])}</a>""", StringComparison.Ordinal)
			: WebUtility.HtmlEncode(text["notFound.linksNoSearch"]).Replace("{home}", home, StringComparison.Ordinal);
		string title = text["notFound.title"];
		var page = new DocPage
		{
			FrontMatter = new FrontMatter
			{
				Title = title,
				Layout = "default",
				Toc = false,
				Visibility = PageVisibility.Hidden
			},
			Content = new PageContent
			{
				Html = $"""
				        <h1>{WebUtility.HtmlEncode(title)}</h1>
				        <p>{WebUtility.HtmlEncode(text["notFound.text"])}</p>
				        <p>{links}</p>

				        """
			},
			Route = NotFoundRoute
		};

		return Render(page, themeContext, true, cancellationToken);
	}

	private string Render(DocPage page, ThemeRenderContext themeContext, bool notFound,
		CancellationToken cancellationToken)
	{
		string layoutName = page.FrontMatter.Layout;
		string templateContent = themeContext.GetTemplate(layoutName)
		                         ?? themeContext.GetTemplate("default")
		                         ?? throw new InvalidOperationException(
			                         $"Layout template '{layoutName}' not found in theme.");

		Template template = GetOrParseTemplate(layoutName, templateContent);

		// Errors propagate so RenderPhase can report them. This method used to catch them and
		// return the bare page content with an HTML comment, so a layout using {{ include }}
		// produced a site with no layout and a build that reported success.
		if (template.HasErrors)
		{
			throw new InvalidOperationException(
				$"Layout '{layoutName}' has errors: {string.Join("; ", template.Messages.Select(m => m.ToString()))}");
		}

		ScriptObject scriptObject = BuildScriptObject(page, themeContext, notFound);

		// AutoIndent prefixed every line of {{ content }} with the template's indentation, which
		// shifted code inside <pre>. A pass that stripped the common indent of <pre> blocks
		// afterwards also stripped the code's own indent (Python method bodies ended up at
		// column 0) and missed <pre class="..."> blocks such as Mermaid diagrams.
		// Scriban's loop and string limits suit untrusted templates, not a docs layout. LoopLimit (1000)
		// counts every iteration under the outer `for item in nav`, so a sidebar of about 1000 entries
		// failed every page; LimitToString (1 MB) also capped the output and cut big pages off with
		// "...". A runaway loop is stopped by the time budget, and by an output cap far above any real
		// page that throws instead of truncating.
		using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		budget.CancelAfter(RenderTimeout);
		var context = new TemplateContext
		{
			AutoIndent = false,
			LoopLimit = 0,
			LimitToString = 0,
			OutputLimit = _outputLimit,
			OnOutputLimit = ScriptLimitBehavior.Throw,
			CancellationToken = budget.Token
		};
		context.PushGlobal(scriptObject);
		context.MemberRenamer = member => member.Name;

		try
		{
			return template.Render(context);
		}
		catch (ScriptAbortException) when (cancellationToken.IsCancellationRequested)
		{
			throw new OperationCanceledException(cancellationToken);
		}
		catch (ScriptAbortException)
		{
			throw new InvalidOperationException(
				$"Layout '{layoutName}' did not finish within {RenderTimeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} s; a loop in the template may never end");
		}
	}

	/// <summary>
	///     Renders the sidebar markup of a theme once for the whole site, with no current page, for a
	///     sidebar that pages share instead of each holding a copy. The template sees the same
	///     <c>nav</c> variable as a layout.
	/// </summary>
	/// <param name="sidebarTemplate">The theme's Scriban markup for the sidebar.</param>
	/// <param name="themeContext">The render context, for the navigation and the base path.</param>
	/// <param name="cancellationToken">Cancels the render.</param>
	/// <returns>The rendered sidebar.</returns>
	/// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
	/// <exception cref="InvalidOperationException">The template has errors, failed or timed out.</exception>
	public string RenderSidebar(string sidebarTemplate, ThemeRenderContext themeContext,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(sidebarTemplate);
		ArgumentNullException.ThrowIfNull(themeContext);

		// A name no layout can have, so a page's layout never gets this template from the cache.
		Template template = GetOrParseTemplate("\0sidebar", sidebarTemplate);
		if (template.HasErrors)
		{
			throw new InvalidOperationException(
				$"The sidebar template has errors: {string.Join("; ", template.Messages.Select(m => m.ToString()))}");
		}

		string bp = themeContext.Config.Build.BasePath;
		var scriptObject = new ScriptObject();
		scriptObject.SetValue("base_path", bp == "/" ? "" : bp, false);
		scriptObject.SetValue("t", themeContext.TextObject, false);
		scriptObject.SetValue("nav",
			BuildNavObject(themeContext.Navigation, null, themeContext.Index, bp, false, IconSpriteUrl(themeContext, bp)), false);

		using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		budget.CancelAfter(RenderTimeout);
		var context = new TemplateContext
		{
			AutoIndent = false,
			LoopLimit = 0,
			LimitToString = 0,
			OutputLimit = _outputLimit,
			OnOutputLimit = ScriptLimitBehavior.Throw,
			CancellationToken = budget.Token
		};
		context.PushGlobal(scriptObject);
		context.MemberRenamer = member => member.Name;

		try
		{
			return template.Render(context);
		}
		catch (ScriptAbortException) when (cancellationToken.IsCancellationRequested)
		{
			throw new OperationCanceledException(cancellationToken);
		}
		catch (ScriptAbortException)
		{
			throw new InvalidOperationException(
				$"The sidebar did not finish within {RenderTimeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} s");
		}
	}

	private Template GetOrParseTemplate(string name, string content)
	{
		if (_templateCache.TryGetValue(name, out Template? cached))
		{
			return cached;
		}

		var template = Template.Parse(content);
		logger.LogDebug("Parsed layout template '{Name}' ({Count} parser messages)", name, template.Messages.Count);
		_templateCache[name] = template;
		return template;
	}

	/// <summary>
	///     The name a template reads a text by in <c>t</c>: the key in snake case, with dots and hyphens as
	///     underscores. <c>toc.title</c> is <c>t.toc_title</c>, <c>a11y.skipToContent</c> is
	///     <c>t.a11y_skip_to_content</c>.
	/// </summary>
	/// <param name="key">A key of <see cref="ThemeStrings.All" />.</param>
	/// <returns>The variable name.</returns>
	public static string TextVariableName(string key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return string.Concat(key.Select(c => c is '.' or '-' ? "_" : char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : c.ToString()));
	}

	// One read-only object per render context, shared by every page.
	internal static ScriptObject BuildTextObject(ThemeText text)
	{
		var obj = new ScriptObject();
		foreach ((string key, string value) in text.Strings)
		{
			obj.SetValue(TextVariableName(key), value, true);
		}

		obj.IsReadOnly = true;
		return obj;
	}

	private static string PrefixRoute(string route, string basePath)
	{
		if (basePath == "/")
		{
			return route;
		}

		if (string.IsNullOrEmpty(route) || route == "/")
		{
			return basePath + "/";
		}

		return basePath + (route.StartsWith('/') ? route : "/" + route);
	}

	// InformationalVersion is the <Version> from Directory.Build.props; AssemblyVersion stays 1.0.0.0.
	private static string ReadMokaVersion()
	{
		string version = typeof(ScribanTemplateEngine).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
		int plus = version.IndexOf('+', StringComparison.Ordinal);
		return plus > 0 ? version[..plus] : version;
	}

	/// <summary>
	///     Builds the final URL for a brand asset (<c>site.logo_url</c> / <c>site.favicon_url</c>).
	///     Absolute URLs (http/https/protocol-relative/data URIs) pass through unchanged so CDN-
	///     hosted brand assets work without a base-path prefix. Root-relative publish URLs get
	///     the build <see cref="BuildConfig.BasePath" /> prepended so GitHub Pages project-page
	///     deploys (<c>/Moka.Red/_site/</c>) resolve correctly. A local file the build didn't
	///     find gets an empty URL, so the theme falls back as if none were configured.
	/// </summary>
	private static string ResolveBrandUrl(SiteAssetReference? asset, string basePath,
		IReadOnlyDictionary<string, string>? brandAssetFiles)
	{
		if (asset is null)
		{
			return "";
		}

		if (asset.IsAbsoluteUrl)
		{
			return asset.PublishUrl;
		}

		// A missing file is never copied into the site, and its URL made a broken image in the
		// header and a favicon link that 404s.
		if (asset.ShouldCopy && brandAssetFiles is not null && !brandAssetFiles.ContainsKey(asset.PublishUrl))
		{
			return "";
		}

		if (basePath == "/" || string.IsNullOrEmpty(basePath))
		{
			return asset.PublishUrl;
		}

		// basePath already has no trailing slash (SiteConfigReader normalizes it);
		// PublishUrl always starts with a single leading slash. Concatenation yields
		// /basepath/assets/logo.png cleanly.
		return basePath + asset.PublishUrl;
	}

	/// <summary>
	///     The absolute URL of <c>site.image</c> for link previews, or an empty string when there is
	///     none, its file wasn't found, or it can't be made absolute (no <c>site.url</c>).
	/// </summary>
	private static string SiteImageUrl(ThemeRenderContext ctx, string basePath)
	{
		if (ctx.Config.Site.Image is not { } image
		    || (image.ShouldCopy && ctx.BrandAssetFiles is not null && !ctx.BrandAssetFiles.ContainsKey(image.PublishUrl)))
		{
			return "";
		}

		return AbsoluteImageUrl(image.PublishUrl, ctx.Config.Site.Url, basePath) ?? "";
	}

	/// <summary>
	///     An image URL that link previews can load: http and https URLs as written, a protocol-relative
	///     one with https, and a path from the site root under <c>site.url</c> and the base path.
	///     <c>null</c> for none, another scheme (a <c>data:</c> URI), or a path without <c>site.url</c>.
	/// </summary>
	/// <remarks>
	///     Front matter images are already resolved from the page's folder by the time a page renders.
	/// </remarks>
	private static string? AbsoluteImageUrl(string? image, string siteUrl, string basePath)
	{
		if (string.IsNullOrWhiteSpace(image))
		{
			return null;
		}

		string value = image.Trim();
		if (value.StartsWith("//", StringComparison.Ordinal))
		{
			return "https:" + value;
		}

		// Checked for a leading slash first: on Linux and macOS "/img.png" parses as an absolute file URI.
		if (!value.StartsWith('/') && Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme.Length > 1)
		{
			return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? value : null;
		}

		string url = SiteUrls.AbsoluteFile(siteUrl, basePath, "/" + Uri.UnescapeDataString(value.TrimStart('/')));
		return url.Length > 0 ? url : null;
	}

	private static ScriptObject BuildScriptObject(DocPage page, ThemeRenderContext ctx, bool notFound)
	{
		var so = new ScriptObject();
		string bp = ctx.Config.Build.BasePath;
		int tocDepth = ctx.Config.Theme.Options.TocDepth;
		string siteImageUrl = SiteImageUrl(ctx, bp);
		string sprite = IconSpriteUrl(ctx, bp);

		// Base path for templates and JS
		so.SetValue("base_path", bp == "/" ? "" : bp, false);
		so.SetValue("t", ctx.TextObject, false);

		#region Page Data

		so.SetValue("page", new ScriptObject
		{
			{ "title", page.FrontMatter.Title },
			{ "description", page.FrontMatter.Description },
			// The site description stands in for pages that have none, so search engines and
			// link previews never get an empty description.
			{
				"meta_description",
				string.IsNullOrWhiteSpace(page.FrontMatter.Description)
					? ctx.Config.Site.Description
					: page.FrontMatter.Description
			},
			// The not-found page is served at whatever address was missing, so it names no canonical URL.
			{ "canonical_url", notFound ? "" : SiteUrls.Absolute(ctx.Config.Site.Url, bp, page.Route) },
			{ "noindex", notFound },
			{ "image_url", AbsoluteImageUrl(page.FrontMatter.Image, ctx.Config.Site.Url, bp) ?? siteImageUrl },
			{
				"markdown_url",
				ctx.MarkdownUrls.TryGetValue(page.Route, out string? markdownUrl) ? PrefixRoute(markdownUrl, bp) : ""
			},
			// Compared before the base path is added: page.route is prefixed, so the NuGet
			// widget's route check never matched on a site built with --base-path.
			{ "is_api_index", page.Route == "/api" },
			{ "content", ContentLinkRewriter.Rewrite(page.Content.Html, bp) },
			{ "route", PrefixRoute(page.Route, bp) },
			{ "toc", BuildTocObject(page.TableOfContents.Entries, tocDepth) },
			// One entry is no table of contents: 43 API pages showed a sidebar with a single link.
			{
				"show_toc",
				page.FrontMatter.Toc && CountTocEntries(page.TableOfContents.Entries, tocDepth) >= 2 &&
				ctx.Config.Theme.Options.ShowTableOfContents
			},
			{ "tags", page.FrontMatter.Tags },
			{ "layout", page.FrontMatter.Layout },
			// Forward slashes on every OS: the source path comes from the file system and is
			// used in URLs (edit links), where Windows backslashes do not belong.
			{ "source_path", page.SourcePath?.Replace('\\', '/') ?? "" },
			{ "last_modified", page.LastModified?.ToString("yyyy-MM-dd") ?? "" },
			{
				"last_modified_display",
				page.LastModified is { } modified ? ctx.Text.FormatDate(DateOnly.FromDateTime(modified.DateTime)) : ""
			},
			{ "contributors", BuildContributors(page.Contributors) },
			{ "is_api", page.Origin == PageOrigin.ApiGenerated },
			{ "features", BuildFeatures(page.FrontMatter.Features, bp, sprite) },
			{ "features_title", page.FrontMatter.FeaturesTitle },
			{ "features_subtitle", page.FrontMatter.FeaturesSubtitle },
			{ "hero_code", CodeSampleHtml(page.FrontMatter.HeroCode, page.FrontMatter.HeroCodeLanguage, ctx) },
			{ "hero_image", HeroImageUrl(page.FrontMatter.HeroImage, bp) },
			{ "showcase_code", CodeSampleHtml(page.FrontMatter.ShowcaseCode, page.FrontMatter.ShowcaseCodeLanguage, ctx) },
			{ "showcase_title", page.FrontMatter.ShowcaseTitle },
			{ "showcase_text", page.FrontMatter.ShowcaseText },
			{ "stats", BuildStats(page.FrontMatter.Stats) },
			{ "cta", BuildCallToAction(page.FrontMatter.Cta, bp, ctx.Text) },
			{ "is_blog_post", ctx.Blog?.FindPost(page.Route) is not null }
		}, false);

		#endregion

		so.SetValue("blog", BuildBlogObject(ctx.Blog, bp), false);

		#region Site Config

		// Brand assets expose two script variables each:
		//   site.logo / site.favicon           - the user's raw yaml value (for backward
		//                                        compatibility with any custom template that
		//                                        read the old string directly).
		//   site.logo_url / site.favicon_url   - the final resolved URL the theme should emit.
		//                                        Absolute URLs pass through unchanged; relative
		//                                        paths get the BasePath prefix prepended.
		// Templates in EmbeddedThemeProvider use the *_url variants so they "just work" for
		// both GitHub Pages subpath deploys and CDN-hosted brand assets without any
		// conditional logic in the scriban markup.
		so.SetValue("site", new ScriptObject
		{
			{ "title", ctx.Config.Site.Title },
			{ "description", ctx.Config.Site.Description },
			{ "url", ctx.Config.Site.Url },
			{ "copyright", ExpandYear(ctx.Config.Site.Copyright) },
			{ "logo", ctx.Config.Site.Logo?.RawValue ?? "" },
			{ "favicon", ctx.Config.Site.Favicon?.RawValue ?? "" },
			{ "logo_url", ResolveBrandUrl(ctx.Config.Site.Logo, bp, ctx.BrandAssetFiles) },
			{ "favicon_url", ResolveBrandUrl(ctx.Config.Site.Favicon, bp, ctx.BrandAssetFiles) },
			{ "image_url", siteImageUrl },
			// repo_url is used by the landing page "View on GitHub" button.
			// Derived from editLink.repo when available; falls back to empty string
			// which the template uses to hide the button entirely.
			{ "repo_url", ctx.Config.Site.EditLink?.Repo ?? "" },
			{ "language", ctx.Text.Language },
			{ "direction", SiteLanguages.Direction(ctx.Text.Language, ctx.Config.Site.Direction) },
			{ "og_locale", SiteLanguages.OpenGraphLocale(ctx.Text.Language) }
		}, false);

		#endregion

		#region Theme Options

		so.SetValue("theme", new ScriptObject
		{
			{ "primary_color", ctx.Config.Theme.Options.PrimaryColor },
			{ "primary_text", BuildPrimaryTextObject(ctx.Config.Theme.Options) },
			{ "accent_color", ctx.Config.Theme.Options.AccentColor },
			{ "code_theme", ctx.Config.Theme.Options.CodeTheme },
			{ "show_edit_link", ctx.Config.Theme.Options.ShowEditLink },
			{ "show_last_updated", ctx.Config.Theme.Options.ShowLastUpdated },
			{ "show_contributors", ctx.Config.Theme.Options.ShowContributors },
			{ "color_themes", ctx.Config.Theme.Options.ColorThemes },
			{ "code_theme_selector", ctx.Config.Theme.Options.CodeThemeSelector },
			{ "code_style", ctx.Config.Theme.Options.CodeStyle },
			{ "code_style_selector", ctx.Config.Theme.Options.CodeStyleSelector },
			{ "style", ThemeStyles.Resolve(ctx.Config.Theme.Options.Style) },
			{ "style_selector", ctx.Config.Theme.Options.StyleSelector },
			{ "style_files", BuildStyleFiles(ctx.StyleVersions) },
			{ "text_size_selector", ctx.Config.Theme.Options.TextSizeSelector },
			{ "width_selector", ctx.Config.Theme.Options.WidthSelector },
			{ "layout_selector", ctx.Config.Theme.Options.LayoutSelector },
			{ "layout_parts", _layoutParts },
			{ "style_options", _styleOptions },
			{ "color_presets", _colorPresets },
			{ "code_themes", _codeThemes },
			{ "code_styles", _codeStyles },
			{ "layout", BuildLayoutObject(ctx.Config.Theme.Options.Layout) },
			{ "sidebar_toggle", ctx.Config.Theme.Options.SidebarToggle },
			// The not-found page has no feedback, breadcrumbs or prev/next: there is no page to rate or place.
			{ "show_feedback", ctx.Config.Theme.Options.ShowFeedback && !notFound },
			{ "show_dark_mode_toggle", ctx.Config.Theme.Options.ShowDarkModeToggle },
			{ "show_animations", ctx.Config.Theme.Options.ShowAnimations },
			{ "show_search", ctx.Config.Theme.Options.ShowSearch },
			{ "show_table_of_contents", ctx.Config.Theme.Options.ShowTableOfContents },
			{ "show_prev_next", ctx.Config.Theme.Options.ShowPrevNext && !notFound },
			{ "show_breadcrumbs", ctx.Config.Theme.Options.ShowBreadcrumbs && !notFound },
			{ "show_back_to_top", ctx.Config.Theme.Options.ShowBackToTop },
			{ "show_copy_button", ctx.Config.Theme.Options.ShowCopyButton },
			{ "show_line_numbers", ctx.Config.Theme.Options.ShowLineNumbers },
			{ "image_zoom", ctx.Config.Theme.Options.ImageZoom },
			{ "toc_depth", ctx.Config.Theme.Options.TocDepth },
			{ "show_version_selector", ctx.Config.Theme.Options.ShowVersionSelector },
			{ "show_built_with", ctx.Config.Theme.Options.ShowBuiltWith },
			{ "social_links", BuildSocialLinks(ctx.Config.Theme.Options.SocialLinks, sprite) },
			{ "default_color_theme", ctx.Config.Theme.Options.DefaultColorTheme },
			{ "initial_color_theme", InitialColorTheme(ctx.Config.Theme.Options) },
			{ "banner", BuildBannerObject(ctx.Config.Theme.Options.Banner, bp, ctx.Text) }
		}, false);

		// The search button, dialog and shortcut need both an index and the option on.
		so.SetValue("search_enabled",
			ctx.Config.Features.Search.Enabled && ctx.Config.Theme.Options.ShowSearch, false);
		so.SetValue("search_index_url",
			string.IsNullOrEmpty(ctx.SearchIndexUrl) ? "" : PrefixRoute(ctx.SearchIndexUrl, bp), false);

		#endregion

		so.SetValue("mokadocs_version", _mokaVersion, false);
		so.SetValue("icon_sprite", sprite, false);

		// Edit link
		// Generated pages (API, OpenAPI, Python) have no source file. They used to get an edit
		// link to the docs folder itself.
		if (ctx.Config.Site.EditLink is { } el && ctx.Config.Theme.Options.ShowEditLink
		                                       && !string.IsNullOrEmpty(page.SourcePath))
		{
			string editUrl =
				$"{el.Repo.TrimEnd('/')}/edit/{el.Branch}/{el.Path.TrimEnd('/')}/{page.SourcePath.Replace('\\', '/')}";
			so.SetValue("edit_url", editUrl, false);
		}

		#region Navigation and Breadcrumbs

		RenderIndex index = ctx.Index;
		bool sharedSidebar = !string.IsNullOrEmpty(ctx.SidebarScriptUrl);
		so.SetValue("nav", BuildNavObject(ctx.Navigation, page.Route, index, bp, sharedSidebar, sprite), false);
		so.SetValue("sidebar_script", sharedSidebar ? PrefixRoute(ctx.SidebarScriptUrl!, bp) : "", false);

		so.SetValue("breadcrumbs",
			BuildBreadcrumbs(page.Route, page.FrontMatter.Title, ctx.Navigation, index, bp, ctx.Text["breadcrumbs.home"]), false);

		// With the Layout section on, readers can switch to the tabs row and the link columns, so pages carry both.
		ThemeLayoutOptions layout = ctx.Config.Theme.Options.Layout;
		bool layoutSelector = ctx.Config.Theme.Options.LayoutSelector;
		so.SetValue("layout_tabs",
			layoutSelector || ThemeLayouts.Navbar.Resolve(layout.Navbar) == "tabs" ? BuildLayoutTabs(index, page.Route, bp) : [], false);
		so.SetValue("footer_columns",
			layoutSelector || ThemeLayouts.Footer.Resolve(layout.Footer) == "columns" ? BuildFooterColumns(index, bp, ctx.Text) : [], false);
		bool portal = ThemeLayouts.Home.Resolve(layout.Home) == "portal"
		              && string.Equals(page.FrontMatter.Layout, "landing", StringComparison.OrdinalIgnoreCase);
		so.SetValue("home_sections", portal ? BuildHomeSections(index, page.Route, bp, sprite) : [], false);

		string firstNavRoute = index.FirstSidebarRouteOtherThan(page.Route);
		so.SetValue("first_nav_route", firstNavRoute.Length > 0 ? PrefixRoute(firstNavRoute, bp) : "", false);

		#endregion

		// Partials (injected as strings so templates can use {{ partials.head }})
		so.SetValue("partials", BuildPartialsObject(ctx), false);

		// CSS/JS paths (prefixed with base path)
		so.SetValue("css_files", ctx.CssFiles.Select(f => PrefixRoute(f, bp)).ToList(), false);
		so.SetValue("js_files", ctx.JsFiles.Select(f => PrefixRoute(f, bp)).ToList(), false);

		#region Version Data

		if (ctx.Versions.Count > 0)
		{
			var versionsArray = new ScriptArray();
			foreach (DocVersion v in ctx.Versions)
			{
				bool isCurrent = ctx.CurrentVersion is { } current && current.Slug == v.Slug;
				versionsArray.Add(new ScriptObject
				{
					{ "label", v.Label },
					{ "slug", v.Slug },
					{ "is_default", v.IsDefault },
					{ "is_prerelease", v.IsPrerelease },
					{ "is_current", isCurrent },
					{ "url", VersionUrl(v, isCurrent, page.Route, bp) }
				});
			}

			so.SetValue("versions", versionsArray, false);
			so.SetValue("current_version", ctx.CurrentVersion?.Label ?? "", false);
		}
		else
		{
			so.SetValue("versions", new ScriptArray(), false);
			so.SetValue("current_version", "", false);
		}

		#endregion

		// Package metadata for NuGet install widget
		if (ctx.PackageInfo is { } pkg)
		{
			so.SetValue("package", new ScriptObject
			{
				{ "name", pkg.Name },
				{ "version", pkg.Version }
			}, false);
		}

		#region Prev/Next Page Navigation

		(PageLink? previous, PageLink? next) = index.Neighbors(page.Route);
		if (previous is not null)
		{
			so.SetValue("prev_page",
				new ScriptObject { { "title", previous.Title }, { "route", PrefixRoute(previous.Route, bp) } }, false);
		}

		if (next is not null)
		{
			so.SetValue("next_page",
				new ScriptObject { { "title", next.Title }, { "route", PrefixRoute(next.Route, bp) } }, false);
		}

		#endregion

		return so;
	}

	/// <summary>
	///     Where a version dropdown entry links from this page: the page itself for the current version,
	///     the same page in another version when that version has it, else that version's home page.
	/// </summary>
	private static string VersionUrl(DocVersion version, bool isCurrent, string route, string basePath)
	{
		if (isCurrent)
		{
			return PrefixRoute(route, basePath);
		}

		string sitePrefix = basePath == "/" ? "" : basePath;
		string root = version.UrlPath ?? (version.IsDefault ? sitePrefix : sitePrefix + "/" + version.Slug);
		string path = route.StartsWith('/') ? route : "/" + route;
		return path != "/" && version.Routes?.Contains(route) == true ? root + path : root + "/";
	}

	/// <summary>Replaces <c>{year}</c> in the copyright notice with the current year.</summary>
	private static string ExpandYear(string? copyright) =>
		string.IsNullOrEmpty(copyright)
			? ""
			: copyright.Replace("{year}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture),
				StringComparison.Ordinal);

	/// <summary>
	///     The color preset applied before the reader picks one, or an empty string for none.
	/// </summary>
	/// <remarks>
	///     Presets set <c>--color-primary</c> with <c>!important</c>, so while one is applied
	///     <c>primaryColor</c> has no visible effect. Every page used to start on a preset, which
	///     made <c>primaryColor</c> do nothing at all. A site that changes <c>primaryColor</c>
	///     and leaves <c>defaultColorTheme</c> at its default now starts with no preset.
	/// </remarks>
	private static string InitialColorTheme(ThemeOptions options)
	{
		bool customPrimary = !string.IsNullOrWhiteSpace(options.PrimaryColor)
		                     && !options.PrimaryColor.Equals(MokaDefaults.PrimaryColor, StringComparison.OrdinalIgnoreCase);
		bool defaultPreset = options.DefaultColorTheme.Equals("ocean", StringComparison.OrdinalIgnoreCase);

		return customPrimary && defaultPreset ? "" : options.DefaultColorTheme;
	}

	// A custom primaryColor shows only while no preset is applied, and it had no checked text shade, so
	// links and inline code in a light color failed AA. Null (nothing written) when a preset always
	// applies, or for a color the check can't read.
	private static ScriptObject? BuildPrimaryTextObject(ThemeOptions options) =>
		InitialColorTheme(options) == "" && PrimaryColorContrast.For(options.PrimaryColor) is { } colors
			? new ScriptObject
			{
				{ "light", colors.Light },
				{ "light_hover", colors.LightHover },
				{ "dark", colors.Dark },
				{ "dark_hover", colors.DarkHover }
			}
			: null;

	// Entries deeper than tocDepth are left out here rather than only skipped by the layout: a heading
	// whose children were all too deep used to get an empty nested list. A child is always deeper
	// than its parent, so dropping an entry never drops one that should show.
	private static ScriptArray BuildTocObject(IReadOnlyList<TocEntry> entries, int maxLevel)
	{
		var arr = new ScriptArray();
		foreach (TocEntry entry in entries)
		{
			if (entry.Level > maxLevel)
			{
				continue;
			}

			arr.Add(new ScriptObject
			{
				{ "level", entry.Level },
				{ "text", entry.Text },
				{ "id", entry.Id },
				{ "children", BuildTocObject(entry.Children, maxLevel) }
			});
		}

		return arr;
	}

	private static int CountTocEntries(IReadOnlyList<TocEntry> entries, int maxLevel) =>
		entries.Where(e => e.Level <= maxLevel).Sum(e => 1 + CountTocEntries(e.Children, maxLevel));

	/// <summary>
	///     The sidebar entries for a page. With <paramref name="pathOnly" />, the shared sidebar's short
	///     stand-in: the top-level entries and the entries along the path to the page, each other
	///     section shown as a link to its route.
	/// </summary>
	private static ScriptArray BuildNavObject(NavigationTree? nav, string? activeRoute, RenderIndex index,
		string basePath, bool pathOnly, string sprite)
	{
		if (nav is null)
		{
			return [];
		}

		IReadOnlySet<NavigationNode> ancestors = index.AncestorsOf(activeRoute);
		var arr = new ScriptArray();
		foreach (NavigationNode node in nav.Items)
		{
			if (BuildNavNodeObject(node, activeRoute, ancestors, index.PageRoutes, basePath, pathOnly, sprite) is { } obj)
			{
				arr.Add(obj);
			}
		}

		return arr;
	}

	private static ScriptObject? BuildNavNodeObject(NavigationNode node, string? activeRoute,
		IReadOnlySet<NavigationNode> ancestors, HashSet<string> pageRoutes, string basePath, bool pathOnly, string sprite)
	{
		bool hasActiveChild = ancestors.Contains(node);
		bool isOnRoute = activeRoute is not null && node.Route == activeRoute;
		bool keepChildren = !pathOnly || hasActiveChild || isOnRoute;

		// A section without a route of its own (an API namespace, a nav group) links to its first
		// entry instead, so the short sidebar still leads into it without JavaScript.
		string? route = keepChildren || !string.IsNullOrEmpty(node.Route) ? node.Route : FirstRoute(node.Children);
		if (!keepChildren && node.Children.Count > 0 && string.IsNullOrEmpty(route))
		{
			return null;
		}

		bool hasChildren = keepChildren && node.Children.Count > 0;
		bool hasPage = !string.IsNullOrEmpty(route) && pageRoutes.Contains(route);

		// A node is "active" only if its route matches AND it doesn't have an active child.
		// This prevents parent sections from showing as "current" when their route was
		// resolved to a child's route (e.g. /guide → /guide/getting-started).
		bool isActive = isOnRoute && !hasActiveChild;

		var obj = new ScriptObject
		{
			{ "label", node.Label },
			{ "route", !string.IsNullOrEmpty(route) ? PrefixRoute(route, basePath) : "" },
			{ "icon", string.IsNullOrEmpty(node.Icon) ? "" : IconMarkup(node.Icon, sprite) ?? "" },
			{ "expanded", node.Expanded || hasActiveChild },
			{ "is_active", isActive },
			{ "has_active_child", hasActiveChild },
			{ "has_children", hasChildren },
			{ "has_page", hasPage }
		};

		var children = new ScriptArray();
		if (keepChildren)
		{
			foreach (NavigationNode child in node.Children)
			{
				if (BuildNavNodeObject(child, activeRoute, ancestors, pageRoutes, basePath, pathOnly, sprite) is { } childObj)
				{
					children.Add(childObj);
				}
			}
		}

		obj.SetValue("children", children, false);

		return obj;
	}

	// A link from the site root gets the base path, like the sidebar's routes; "//host/x" is a URL.
	private static ScriptObject? BuildBannerObject(ThemeBanner? banner, string basePath, ThemeText text)
	{
		if (banner is null || string.IsNullOrWhiteSpace(banner.Text))
		{
			return null;
		}

		string link = banner.Link?.Trim() ?? "";
		if (link.StartsWith('/') && !link.StartsWith("//", StringComparison.Ordinal))
		{
			link = PrefixRoute(link, basePath);
		}

		return new ScriptObject
		{
			{ "text", banner.Text },
			{ "link", link },
			{ "link_text", string.IsNullOrWhiteSpace(banner.LinkText) ? text["banner.learnMore"] : banner.LinkText },
			{ "id", banner.EffectiveId },
			{ "dismissible", banner.Dismissible },
			{ "variant", BannerVariants.Resolve(banner.Variant) }
		};
	}

	// Keys are the parts' snake_case names: pageNav is theme.layout.page_nav.
	private static ScriptObject BuildLayoutObject(ThemeLayoutOptions layout)
	{
		var obj = new ScriptObject();
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			obj.Add(SnakeCase(part.Key), part.Resolve(layout.NameOf(part)));
		}

		return obj;
	}

	private static string SnakeCase(string name) =>
		string.Concat(name.Select(c => char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : c.ToString()));

	// A landing page's code sample (heroCode, showcaseCode), highlighted like a fenced block when the
	// build supplies a highlighter. Only pages that set one pay for it.
	private static string CodeSampleHtml(string? code, string? language, ThemeRenderContext ctx)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return "";
		}

		return ctx.CodeHighlighter is { } highlight
			? highlight(code, language)
			: "<pre><code>" + WebUtility.HtmlEncode(code) + "</code></pre>";
	}

	private static ScriptArray BuildStats(List<LandingStat> stats)
	{
		var arr = new ScriptArray();
		foreach (LandingStat stat in stats)
		{
			arr.Add(new ScriptObject { { "value", stat.Value }, { "label", stat.Label } });
		}

		return arr;
	}

	// Text stays raw for the layout to escape; the link gets the base path like the feature cards'.
	private static ScriptObject? BuildCallToAction(LandingCallToAction? cta, string basePath, ThemeText text) =>
		cta is null
			? null
			: new ScriptObject
			{
				{ "title", cta.Title },
				{ "text", cta.Text },
				{ "link", cta.Link is null ? "" : SiteLink(cta.Link, basePath) },
				{ "link_text", cta.LinkText ?? text["landing.getStarted"] }
			};

	// A path from the site root gets the base path; URLs and anything else stay as written.
	private static string HeroImageUrl(string? image, string basePath)
	{
		if (string.IsNullOrWhiteSpace(image))
		{
			return "";
		}

		string value = image.Trim();
		return value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal)
			? PrefixRoute(value, basePath)
			: value;
	}

	// The pickers' option lists, which the head script also reads its dark-first styles and code
	// theme pairs from. They never change, so every page shares one read-only copy.
	private static readonly ScriptArray _styleOptions = ReadOnlyList(ThemeStyles.All, s => new ScriptObject
	{
		{ "name", s.Name },
		{ "label", s.Label },
		{ "description", s.Description },
		{ "dark_first", s.DarkFirst }
	});

	private static readonly ScriptArray _colorPresets = ReadOnlyList(ColorPresets.All, p => new ScriptObject
	{
		{ "name", p.Name },
		{ "label", p.Label },
		{ "swatch", p.Swatch }
	});

	private static readonly ScriptArray _codeThemes = ReadOnlyList(CodeThemes.All, t => new ScriptObject
	{
		{ "name", t.Name },
		{ "label", t.Label },
		{ "background", t.Background },
		{ "is_dark", t.IsDark },
		{ "counterpart", t.Counterpart ?? "" }
	});

	private static readonly ScriptArray _codeStyles = ReadOnlyList(CodeStyles.All, s => new ScriptObject
	{
		{ "name", s.Name },
		{ "label", s.Label }
	});

	// The parts of the Appearance panel's Layout section, for the head script that applies a reader's picks:
	// key is the part's name in theme.layout, name its attribute without data- (page_nav, page-nav), names
	// every variant, space separated.
	private static readonly ScriptArray _layoutParts = ReadOnlyList(ThemeLayouts.ReaderParts, p => new ScriptObject
	{
		{ "key", SnakeCase(p.Key) },
		{ "name", SnakeCase(p.Key).Replace('_', '-') },
		{ "names", string.Join(' ', p.Names) }
	});

	private static ScriptArray BuildStyleFiles(IReadOnlyDictionary<string, string> versions) =>
		ReadOnlyList(versions, v => new ScriptObject { { "name", v.Key }, { "version", v.Value } });

	private static ScriptArray ReadOnlyList<T>(IEnumerable<T> items, Func<T, ScriptObject> convert)
	{
		var list = new ScriptArray();
		foreach (T item in items)
		{
			ScriptObject entry = convert(item);
			entry.IsReadOnly = true;
			list.Add(entry);
		}

		list.IsReadOnly = true;
		return list;
	}

	private static ScriptArray BuildLayoutTabs(RenderIndex index, string route, string basePath)
	{
		IReadOnlySet<NavigationNode> ancestors = index.AncestorsOf(route);
		var tabs = new ScriptArray();
		foreach (NavSection section in index.TopSections)
		{
			bool isCurrent = ancestors.Contains(section.Node)
			                 || string.Equals(section.Node.Route, route, StringComparison.OrdinalIgnoreCase)
			                 || string.Equals(section.Route, route, StringComparison.OrdinalIgnoreCase);
			tabs.Add(new ScriptObject
			{
				{ "label", section.Label },
				{ "url", PrefixRoute(section.Route, basePath) },
				{ "is_current", isCurrent }
			});
		}

		return tabs;
	}

	// Sections become columns of their first entries, up to four. Top-level pages without entries
	// under them share one more column after the sections. That column used to come first and take
	// one of the four places, so the last section of a sidebar with four went missing.
	private static ScriptArray BuildFooterColumns(RenderIndex index, string basePath, ThemeText text)
	{
		var columns = new ScriptArray();
		foreach (NavSection section in index.TopSections.Where(s => s.Links.Count > 0).Take(_footerColumnLimit))
		{
			columns.Add(FooterColumn(section.Label, section.Links, basePath));
		}

		List<PageLink> loose = index.TopSections
			.Where(s => s.Links.Count == 0)
			.Select(s => new PageLink(s.Label, s.Route))
			.ToList();
		if (loose.Count > 0)
		{
			columns.Add(FooterColumn(columns.Count == 0 ? text["footer.docs"] : text["footer.more"], loose, basePath));
		}

		return columns;
	}

	// The portal home page's cards, one per top-level section with its first pages. A top-level entry
	// for the home page itself is left out: a card leading to the page it sits on goes nowhere.
	private static ScriptArray BuildHomeSections(RenderIndex index, string route, string basePath, string sprite)
	{
		var sections = new ScriptArray();
		foreach (NavSection section in index.TopSections)
		{
			if (section.Links.Count == 0 && string.Equals(section.Route, route, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var links = new ScriptArray();
			foreach (PageLink link in section.Links.Take(_homeSectionLinkLimit))
			{
				links.Add(new ScriptObject { { "label", link.Title }, { "url", PrefixRoute(link.Route, basePath) } });
			}

			sections.Add(new ScriptObject
			{
				{ "title", section.Label },
				{ "url", PrefixRoute(section.Route, basePath) },
				{ "icon", string.IsNullOrEmpty(section.Node.Icon) ? "" : IconMarkup(section.Node.Icon, sprite) ?? "" },
				{ "links", links }
			});
		}

		return sections;
	}

	private static ScriptObject FooterColumn(string title, IEnumerable<PageLink> links, string basePath)
	{
		var items = new ScriptArray();
		foreach (PageLink link in links.Take(_footerLinkLimit))
		{
			items.Add(new ScriptObject { { "label", link.Title }, { "url", PrefixRoute(link.Route, basePath) } });
		}

		return new ScriptObject { { "title", title }, { "links", items } };
	}

	private static string? FirstRoute(List<NavigationNode> nodes)
	{
		foreach (NavigationNode node in nodes)
		{
			if (!string.IsNullOrEmpty(node.Route))
			{
				return node.Route;
			}

			if (FirstRoute(node.Children) is { } route)
			{
				return route;
			}
		}

		return null;
	}

	/// <summary>
	///     The landing page feature cards. Text stays raw so the layout escapes it where it is
	///     written. Icons resolve to SVG markup here because templates can't reach the icon set.
	/// </summary>
	private static ScriptArray BuildFeatures(List<LandingFeature> features, string basePath, string sprite)
	{
		var arr = new ScriptArray();
		foreach (LandingFeature feature in features)
		{
			string icon = feature.Icon ?? "";
			string link = SiteLink(feature.Link ?? "", basePath);

			arr.Add(new ScriptObject
			{
				{ "title", feature.Title },
				{ "description", feature.Description },
				{ "icon", icon },
				{ "icon_svg", icon.Length > 0 ? IconMarkup(icon, sprite) ?? "" : "" },
				{ "link", link }
			});
		}

		return arr;
	}

	// A root-relative link in front matter needs the base path like every other site URL, and nothing
	// rewrites front matter values later. "//host/x" is protocol-relative, not a route.
	private static string SiteLink(string link, string basePath) =>
		link.StartsWith('/') && !link.StartsWith("//", StringComparison.Ordinal) ? PrefixRoute(link, basePath) : link;

	/// <summary>
	///     The blog for every page: whether there is one, its title and index route, and the feeds
	///     the head links with <c>rel="alternate"</c>. Feed URLs are already absolute.
	/// </summary>
	private static ScriptObject BuildBlogObject(BlogSite? blog, string basePath)
	{
		var feeds = new ScriptArray();
		foreach (BlogFeed feed in blog?.Feeds ?? [])
		{
			feeds.Add(new ScriptObject
			{
				{ "format", feed.Format },
				{ "type", feed.MediaType },
				{ "title", feed.Title },
				{ "url", feed.Url }
			});
		}

		return new ScriptObject
		{
			{ "enabled", blog is not null },
			{ "title", blog?.Title ?? "" },
			{ "route", blog is null ? "" : PrefixRoute(blog.Route, basePath) },
			{ "feeds", feeds }
		};
	}

	// The email stays out of the template data: the theme has no use for it, and a page shouldn't
	// publish an address because a template printed the whole object.
	private static ScriptArray BuildContributors(IReadOnlyList<PageContributor> contributors)
	{
		var arr = new ScriptArray();
		foreach (PageContributor contributor in contributors)
		{
			arr.Add(new ScriptObject
			{
				{ "name", contributor.Name },
				{ "initials", contributor.Initials },
				{ "commits", contributor.Commits }
			});
		}

		return arr;
	}

	private static ScriptArray BuildSocialLinks(List<SocialLink> links, string sprite)
	{
		var arr = new ScriptArray();
		foreach (SocialLink link in links)
		{
			string iconSvg = IconMarkup(link.Icon, sprite) ?? WebUtility.HtmlEncode(link.Icon);
			arr.Add(new ScriptObject { { "icon", link.Icon }, { "url", link.Url }, { "icon_svg", iconSvg } });
		}

		return arr;
	}

	/// <summary>
	///     Home, then the sidebar sections above the page with their sidebar labels, then the page.
	///     A page the sidebar doesn't list gets one crumb per folder of its route instead.
	/// </summary>
	/// <remarks>
	///     A section without a page of its own links to its first page. It used to be plain text,
	///     which left <c>layout.breadcrumbs: back</c> with no link at all, and a section whose route
	///     the sidebar had resolved to its first page lost its label to the folder name.
	/// </remarks>
	private static ScriptArray BuildBreadcrumbs(string route, string pageTitle, NavigationTree? nav,
		RenderIndex index, string basePath, string homeLabel)
	{
		var crumbs = new ScriptArray();

		// Always start with Home
		crumbs.Add(new ScriptObject
			{ { "label", homeLabel }, { "url", PrefixRoute("/", basePath) }, { "is_current", false } });

		string[] segments = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
		{
			return crumbs;
		}

		IReadOnlyList<NavigationNode> sidebarPath = index.SidebarPath(route);
		if (sidebarPath.Count > 0)
		{
			foreach (NavigationNode section in sidebarPath.Take(sidebarPath.Count - 1))
			{
				if (section.Route is "/")
				{
					continue;
				}

				bool ownPage = index.HasOwnPage(section);
				string? target = index.FirstPageRoute(section);
				crumbs.Add(Crumb(section.Label, target, ownPage, basePath));
			}
		}
		else
		{
			string pathSoFar = "";
			for (int i = 0; i < segments.Length - 1; i++)
			{
				pathSoFar += "/" + segments[i];
				bool hasPage = index.PageRoutes.Contains(pathSoFar);
				string? navLabel = FindNavLabel(nav, pathSoFar);
				NavigationNode? section = navLabel is null ? index.SectionFor(pathSoFar) : null;
				string label = navLabel ?? section?.Label ?? FormatSegment(segments[i]);
				string? target = hasPage
					? pathSoFar
					: (section is null ? null : index.FirstPageRoute(section)) ?? index.FirstPageUnder(pathSoFar);
				crumbs.Add(Crumb(label, target, hasPage, basePath));
			}
		}

		// Current page
		crumbs.Add(new ScriptObject
		{
			{ "label", pageTitle }, { "url", PrefixRoute(route, basePath) }, { "is_current", true },
			{ "has_page", true }
		});

		return crumbs;
	}

	private static ScriptObject Crumb(string label, string? route, bool hasPage, string basePath) => new()
	{
		{ "label", label },
		{ "url", string.IsNullOrEmpty(route) ? "" : PrefixRoute(route.TrimEnd('/') + "/", basePath) },
		{ "is_current", false },
		{ "has_page", hasPage }
	};

	private static string? FindNavLabel(NavigationTree? nav, string route)
	{
		if (nav is null)
		{
			return null;
		}

		return FindNavLabelInNodes(nav.Items, route);
	}

	private static string? FindNavLabelInNodes(List<NavigationNode> nodes, string route)
	{
		foreach (NavigationNode node in nodes)
		{
			if (string.Equals(node.Route, route, StringComparison.OrdinalIgnoreCase))
			{
				return node.Label;
			}

			string? childResult = FindNavLabelInNodes(node.Children, route);
			if (childResult is not null)
			{
				return childResult;
			}
		}

		return null;
	}

	private static string IconSpriteUrl(ThemeRenderContext ctx, string basePath) =>
		string.IsNullOrEmpty(ctx.IconSprite) ? "" : PrefixRoute(ctx.IconSprite, basePath);

	/// <summary>
	///     The markup of an icon in the icon set, or <c>null</c> for a name it doesn't have. With a sprite, it is
	///     the icon's <c>&lt;svg&gt;</c> around a <c>&lt;use&gt;</c> of the sprite's symbol, so a page doesn't
	///     carry the shapes of every icon in its sidebar; without one, the whole icon.
	/// </summary>
	private static string? IconMarkup(string name, string sprite)
	{
		string? svg = LucideIcons.Get(name);
		if (svg is null || sprite.Length == 0)
		{
			return svg;
		}

		int open = svg.IndexOf('>', StringComparison.Ordinal) + 1;
		return svg[..open] + "<use href=\"" + WebUtility.HtmlEncode(sprite) + "#" + name.ToLowerInvariant() + "\"/></svg>";
	}

	private static string FormatSegment(string segment)
	{
		if (string.IsNullOrEmpty(segment))
		{
			return "";
		}

		string formatted = segment.Replace('-', ' ');
		return char.ToUpper(formatted[0]) + formatted[1..];
	}

	private static ScriptObject BuildPartialsObject(ThemeRenderContext ctx)
	{
		var obj = new ScriptObject();
		foreach ((string name, string content) in ctx.Partials)
			// Parse and render partials as-is (they'll be included raw)
		{
			obj.SetValue(name, content, false);
		}

		return obj;
	}
}

/// <summary>
///     Context provided to the template engine for rendering a page.
/// </summary>
public sealed class ThemeRenderContext
{
	private RenderIndex? _index;
	private ScriptObject? _textObject;

	/// <summary>Site configuration.</summary>
	public required SiteConfig Config { get; init; }

	/// <summary>Navigation tree.</summary>
	public NavigationTree? Navigation { get; init; }

	/// <summary>Layout templates keyed by name.</summary>
	public required Dictionary<string, string> Templates { get; init; }

	/// <summary>Partial templates keyed by name.</summary>
	public required Dictionary<string, string> Partials { get; init; }

	/// <summary>CSS file paths relative to output root.</summary>
	public List<string> CssFiles { get; init; } = [];

	/// <summary>JS file paths relative to output root.</summary>
	public List<string> JsFiles { get; init; } = [];

	/// <summary>
	///     The <c>?v=</c> of each style's stylesheet that the default theme publishes, keyed by style name, such
	///     as <c>moka</c> → <c>0123456789</c>. Pages get them as <c>theme.style_files</c> (<c>name</c>,
	///     <c>version</c>), so the head can load a style the reader picked. Empty for a custom theme.
	/// </summary>
	public IReadOnlyDictionary<string, string> StyleVersions { get; init; } = new Dictionary<string, string>();

	/// <summary>
	///     The site-relative URL of the search index with its content hash, such as
	///     <c>/search-index.json?v=0123456789</c>, or <c>null</c> when the build writes no index. Pages get it,
	///     with the base path, as <c>search_index_url</c>, so a browser fetches a new index after a rebuild and
	///     can keep the old one cached until then.
	/// </summary>
	public string? SearchIndexUrl { get; init; }

	/// <summary>
	///     The site-relative URL of the theme's SVG sprite, such as <c>/_theme/icons.svg?v=0123456789</c>, or
	///     <c>null</c> when the theme has none. Pages get it, with the base path, as <c>icon_sprite</c>, and
	///     the icons of nav entries, social links and landing features become a <c>&lt;use&gt;</c> of the
	///     sprite's symbol instead of the whole icon. A custom theme publishes no sprite, so its icons stay inline.
	/// </summary>
	public string? IconSprite { get; init; }

	/// <summary>
	///     All pages in the site, used for prev/next navigation. The page routes, sidebar order and
	///     prev/next links derived from this list and <see cref="Navigation" /> are computed when
	///     the first page renders and reused for every page rendered with this context, so a page
	///     added to the list or a route changed afterwards needs a new context.
	/// </summary>
	public IReadOnlyList<DocPage> AllPages { get; init; } = [];

	/// <summary>All configured documentation versions.</summary>
	public IReadOnlyList<DocVersion> Versions { get; init; } = [];

	/// <summary>The current version being built, if versioning is enabled.</summary>
	public DocVersion? CurrentVersion { get; init; }

	/// <summary>Package metadata for NuGet install widget.</summary>
	public PackageMetadata? PackageInfo { get; init; }

	/// <summary>
	///     The local brand assets the build found and copies into the site, keyed by publish URL
	///     (<see cref="BuildContext.BrandAssetFiles" />). A <c>site.logo</c> or <c>site.favicon</c>
	///     file that is not listed gets an empty <c>site.logo_url</c> or <c>site.favicon_url</c>.
	///     <c>null</c> leaves every configured URL in place.
	/// </summary>
	public IReadOnlyDictionary<string, string>? BrandAssetFiles { get; init; }

	/// <summary>
	///     The blog (<see cref="BuildContext.Blog" />), or <c>null</c> without one. Its posts link to
	///     the next newer and older posts as their previous and next page, and its feeds are linked
	///     from every page's head.
	/// </summary>
	public BlogSite? Blog { get; init; }

	/// <summary>
	///     The site-relative URL of the shared sidebar script, such as
	///     <c>/_theme/js/sidebar.js?v=0123456789</c>, or <c>null</c> when every page holds the whole
	///     sidebar. When set, pages get the URL as <c>sidebar_script</c> and a <c>nav</c> with only
	///     the top-level entries and the entries along the path to the page.
	/// </summary>
	public string? SidebarScriptUrl { get; init; }

	/// <summary>
	///     The site-relative URLs of the pages' Markdown copies, such as <c>/guide/intro.md</c>, keyed by
	///     page route and without the base path. A page listed here gets the URL, with the base path, as
	///     <c>page.markdown_url</c>; any other page gets an empty string.
	/// </summary>
	public IReadOnlyDictionary<string, string> MarkdownUrls { get; init; } = new Dictionary<string, string>();

	/// <summary>
	///     Writes a page's <c>heroCode</c> front matter as a highlighted code block, given the code and
	///     its <c>heroCodeLanguage</c> (or <c>null</c>). The result is <c>page.hero_code</c>. <c>null</c>
	///     writes the code escaped in a plain <c>&lt;pre&gt;&lt;code&gt;</c>.
	/// </summary>
	public Func<string, string?, string>? CodeHighlighter { get; init; }

	/// <summary>
	///     The theme's text in the site's language with its overrides (<see cref="ThemeText.For(SiteConfig)" />).
	///     Templates read it as <c>t</c>, one variable per key in snake case: <c>toc.title</c> is
	///     <c>t.toc_title</c> (<see cref="ScribanTemplateEngine.TextVariableName" />). Defaults to English.
	/// </summary>
	public ThemeText Text { get; init; } = ThemeText.English;

	internal RenderIndex Index => _index ??= new RenderIndex(AllPages, Navigation, Blog);

	internal ScriptObject TextObject => _textObject ??= ScribanTemplateEngine.BuildTextObject(Text);

	/// <summary>Gets a template by layout name.</summary>
	public string? GetTemplate(string layoutName) => Templates.GetValueOrDefault(layoutName);
}
