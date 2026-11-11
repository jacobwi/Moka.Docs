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

		ScriptObject scriptObject = BuildScriptObject(page, themeContext);

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
		scriptObject.SetValue("nav", BuildNavObject(themeContext.Navigation, null, themeContext.Index, bp, false), false);

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

	private static ScriptObject BuildScriptObject(DocPage page, ThemeRenderContext ctx)
	{
		var so = new ScriptObject();
		string bp = ctx.Config.Build.BasePath;

		// Base path for templates and JS
		so.SetValue("base_path", bp == "/" ? "" : bp, false);

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
			{ "canonical_url", SiteUrls.Absolute(ctx.Config.Site.Url, bp, page.Route) },
			// Compared before the base path is added: page.route is prefixed, so the NuGet
			// widget's route check never matched on a site built with --base-path.
			{ "is_api_index", page.Route == "/api" },
			{ "content", ContentLinkRewriter.Rewrite(page.Content.Html, bp) },
			{ "route", PrefixRoute(page.Route, bp) },
			{ "toc", BuildTocObject(page.TableOfContents) },
			{
				"show_toc",
				page.FrontMatter.Toc && page.TableOfContents.Entries.Count > 0 &&
				ctx.Config.Theme.Options.ShowTableOfContents
			},
			{ "tags", page.FrontMatter.Tags },
			{ "layout", page.FrontMatter.Layout },
			// Forward slashes on every OS: the source path comes from the file system and is
			// used in URLs (edit links), where Windows backslashes do not belong.
			{ "source_path", page.SourcePath?.Replace('\\', '/') ?? "" },
			{ "last_modified", page.LastModified?.ToString("yyyy-MM-dd") ?? "" },
			{ "contributors", BuildContributors(page.Contributors) },
			{ "is_api", page.Origin == PageOrigin.ApiGenerated },
			{ "features", BuildFeatures(page.FrontMatter.Features, bp) },
			{ "features_title", page.FrontMatter.FeaturesTitle },
			{ "features_subtitle", page.FrontMatter.FeaturesSubtitle },
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
			// repo_url is used by the landing page "View on GitHub" button.
			// Derived from editLink.repo when available; falls back to empty string
			// which the template uses to hide the button entirely.
			{ "repo_url", ctx.Config.Site.EditLink?.Repo ?? "" }
		}, false);

		#endregion

		#region Theme Options

		so.SetValue("theme", new ScriptObject
		{
			{ "primary_color", ctx.Config.Theme.Options.PrimaryColor },
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
			{ "show_feedback", ctx.Config.Theme.Options.ShowFeedback },
			{ "show_dark_mode_toggle", ctx.Config.Theme.Options.ShowDarkModeToggle },
			{ "show_animations", ctx.Config.Theme.Options.ShowAnimations },
			{ "show_search", ctx.Config.Theme.Options.ShowSearch },
			{ "show_table_of_contents", ctx.Config.Theme.Options.ShowTableOfContents },
			{ "show_prev_next", ctx.Config.Theme.Options.ShowPrevNext },
			{ "show_breadcrumbs", ctx.Config.Theme.Options.ShowBreadcrumbs },
			{ "show_back_to_top", ctx.Config.Theme.Options.ShowBackToTop },
			{ "show_copy_button", ctx.Config.Theme.Options.ShowCopyButton },
			{ "show_line_numbers", ctx.Config.Theme.Options.ShowLineNumbers },
			{ "toc_depth", ctx.Config.Theme.Options.TocDepth },
			{ "show_version_selector", ctx.Config.Theme.Options.ShowVersionSelector },
			{ "show_built_with", ctx.Config.Theme.Options.ShowBuiltWith },
			{ "social_links", BuildSocialLinks(ctx.Config.Theme.Options.SocialLinks) },
			{ "default_color_theme", ctx.Config.Theme.Options.DefaultColorTheme },
			{ "initial_color_theme", InitialColorTheme(ctx.Config.Theme.Options) }
		}, false);

		// The search button, dialog and shortcut need both an index and the option on.
		so.SetValue("search_enabled",
			ctx.Config.Features.Search.Enabled && ctx.Config.Theme.Options.ShowSearch, false);

		#endregion

		so.SetValue("mokadocs_version", _mokaVersion, false);

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
		so.SetValue("nav", BuildNavObject(ctx.Navigation, page.Route, index, bp, sharedSidebar), false);
		so.SetValue("sidebar_script", sharedSidebar ? PrefixRoute(ctx.SidebarScriptUrl!, bp) : "", false);

		so.SetValue("breadcrumbs",
			BuildBreadcrumbs(page.Route, page.FrontMatter.Title, ctx.Navigation, index.PageRoutes, bp), false);

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

	private static ScriptArray BuildTocObject(TableOfContents toc)
	{
		var arr = new ScriptArray();
		foreach (TocEntry entry in toc.Entries)
		{
			arr.Add(BuildTocEntryObject(entry));
		}

		return arr;
	}

	private static ScriptObject BuildTocEntryObject(TocEntry entry)
	{
		var obj = new ScriptObject
		{
			{ "level", entry.Level },
			{ "text", entry.Text },
			{ "id", entry.Id }
		};

		var children = new ScriptArray();
		foreach (TocEntry child in entry.Children)
		{
			children.Add(BuildTocEntryObject(child));
		}

		obj.SetValue("children", children, false);

		return obj;
	}

	/// <summary>
	///     The sidebar entries for a page. With <paramref name="pathOnly" />, the shared sidebar's short
	///     stand-in: the top-level entries and the entries along the path to the page, each other
	///     section shown as a link to its route.
	/// </summary>
	private static ScriptArray BuildNavObject(NavigationTree? nav, string? activeRoute, RenderIndex index,
		string basePath, bool pathOnly)
	{
		if (nav is null)
		{
			return [];
		}

		IReadOnlySet<NavigationNode> ancestors = index.AncestorsOf(activeRoute);
		var arr = new ScriptArray();
		foreach (NavigationNode node in nav.Items)
		{
			if (BuildNavNodeObject(node, activeRoute, ancestors, index.PageRoutes, basePath, pathOnly) is { } obj)
			{
				arr.Add(obj);
			}
		}

		return arr;
	}

	private static ScriptObject? BuildNavNodeObject(NavigationNode node, string? activeRoute,
		IReadOnlySet<NavigationNode> ancestors, HashSet<string> pageRoutes, string basePath, bool pathOnly)
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
			{ "icon", ResolveIcon(node.Icon) },
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
				if (BuildNavNodeObject(child, activeRoute, ancestors, pageRoutes, basePath, pathOnly) is { } childObj)
				{
					children.Add(childObj);
				}
			}
		}

		obj.SetValue("children", children, false);

		return obj;
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
	private static ScriptArray BuildFeatures(List<LandingFeature> features, string basePath)
	{
		var arr = new ScriptArray();
		foreach (LandingFeature feature in features)
		{
			string icon = feature.Icon ?? "";
			string link = feature.Link ?? "";

			// A root-relative link needs the base path like every other site URL, and nothing
			// rewrites front matter values later. "//host/x" is protocol-relative, not a route.
			if (link.StartsWith('/') && !link.StartsWith("//", StringComparison.Ordinal))
			{
				link = PrefixRoute(link, basePath);
			}

			arr.Add(new ScriptObject
			{
				{ "title", feature.Title },
				{ "description", feature.Description },
				{ "icon", icon },
				{ "icon_svg", icon.Length > 0 ? LucideIcons.Get(icon) ?? "" : "" },
				{ "link", link }
			});
		}

		return arr;
	}

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

	private static ScriptArray BuildSocialLinks(List<SocialLink> links)
	{
		var arr = new ScriptArray();
		foreach (SocialLink link in links)
		{
			string iconSvg = LucideIcons.Get(link.Icon) ?? WebUtility.HtmlEncode(link.Icon);
			arr.Add(new ScriptObject { { "icon", link.Icon }, { "url", link.Url }, { "icon_svg", iconSvg } });
		}

		return arr;
	}

	private static ScriptArray BuildBreadcrumbs(string route, string pageTitle, NavigationTree? nav,
		HashSet<string> pageRoutes, string basePath)
	{
		var crumbs = new ScriptArray();

		// Always start with Home
		crumbs.Add(new ScriptObject
			{ { "label", "Home" }, { "url", PrefixRoute("/", basePath) }, { "is_current", false } });

		string[] segments = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
		{
			return crumbs;
		}

		// Build intermediate crumbs from route segments
		string pathSoFar = "";
		for (int i = 0; i < segments.Length - 1; i++)
		{
			pathSoFar += "/" + segments[i];

			// Try to find a label from the nav tree
			string label = FindNavLabel(nav, pathSoFar) ?? FormatSegment(segments[i]);

			// Only make it a link if a real page exists at this route (avoid linking to redirects)
			bool hasPage = pageRoutes.Contains(pathSoFar);
			crumbs.Add(new ScriptObject
			{
				{ "label", label },
				{ "url", hasPage ? PrefixRoute(pathSoFar + "/", basePath) : "" },
				{ "is_current", false },
				{ "has_page", hasPage }
			});
		}

		// Current page
		crumbs.Add(new ScriptObject
		{
			{ "label", pageTitle }, { "url", PrefixRoute(route, basePath) }, { "is_current", true },
			{ "has_page", true }
		});

		return crumbs;
	}

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

	private static string ResolveIcon(string? iconName)
	{
		if (string.IsNullOrEmpty(iconName))
		{
			return "";
		}

		return LucideIcons.Get(iconName) ?? "";
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

	internal RenderIndex Index => _index ??= new RenderIndex(AllPages, Navigation, Blog);

	/// <summary>Gets a template by layout name.</summary>
	public string? GetTemplate(string layoutName) => Templates.GetValueOrDefault(layoutName);
}
