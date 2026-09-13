using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Scriban;
using Scriban.Runtime;

namespace Moka.Docs.Rendering.Scriban;

/// <summary>
///     Renders pages using Scriban templates from the active theme.
/// </summary>
public sealed class ScribanTemplateEngine(ILogger<ScribanTemplateEngine> logger)
{
	private readonly Dictionary<string, Template> _templateCache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     Renders a page using the specified layout template.
	/// </summary>
	/// <param name="page">The page to render.</param>
	/// <param name="themeContext">The theme rendering context with templates, config, nav, etc.</param>
	/// <returns>The fully rendered HTML string.</returns>
	public string RenderPage(DocPage page, ThemeRenderContext themeContext)
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

		ScriptObject scriptObject = BuildScriptObject(page, themeContext);

		// AutoIndent prefixed every line of {{ content }} with the template's indentation, which
		// shifted code inside <pre>. A pass that stripped the common indent of <pre> blocks
		// afterwards also stripped the code's own indent (Python method bodies ended up at
		// column 0) and missed <pre class="..."> blocks such as Mermaid diagrams.
		var context = new TemplateContext { AutoIndent = false };
		context.PushGlobal(scriptObject);
		context.MemberRenamer = member => member.Name;

		return template.Render(context);
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

	private static string RewriteContentLinks(string html, string basePath)
	{
		if (basePath == "/" || string.IsNullOrEmpty(html))
		{
			return html;
		}

		// Rewrite href="/..." and src="/..." in rendered markdown content
		return Regex.Replace(
			html,
			"""(href|src)="(/(?!/)(?![a-zA-Z]+:))""",
			$"""$1="{basePath}$2""");
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
			{ "content", RewriteContentLinks(page.Content.Html, bp) },
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
			{ "is_api", page.Origin == PageOrigin.ApiGenerated },
			{ "features", BuildFeatures(page.FrontMatter.Features, bp) },
			{ "features_title", page.FrontMatter.FeaturesTitle },
			{ "features_subtitle", page.FrontMatter.FeaturesSubtitle }
		}, false);

		#endregion

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
			{ "color_themes", ctx.Config.Theme.Options.ColorThemes },
			{ "code_theme_selector", ctx.Config.Theme.Options.CodeThemeSelector },
			{ "code_style", ctx.Config.Theme.Options.CodeStyle },
			{ "code_style_selector", ctx.Config.Theme.Options.CodeStyleSelector },
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

		// MokaDocs version for footer/header branding. Use InformationalVersion (set from
		// <Version> in Directory.Build.props, e.g. "1.4.1+commithash") rather than
		// AssemblyVersion (which stays at 1.0.0.0 unless explicitly overridden).
		// Strip the "+commithash" suffix so the display is clean.
		string mokaVersion = System.Reflection.CustomAttributeExtensions
			.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(ScribanTemplateEngine).Assembly)
			?.InformationalVersion ?? "0.0.0";
		int plusIdx = mokaVersion.IndexOf('+');
		if (plusIdx > 0)
		{
			mokaVersion = mokaVersion[..plusIdx];
		}

		so.SetValue("mokadocs_version", mokaVersion, false);

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

		var pageRoutes = new HashSet<string>(ctx.AllPages.Select(p => p.Route), StringComparer.OrdinalIgnoreCase);
		so.SetValue("nav", BuildNavObject(ctx.Navigation, page.Route, pageRoutes, bp), false);

		so.SetValue("breadcrumbs",
			BuildBreadcrumbs(page.Route, page.FrontMatter.Title, ctx.Navigation, pageRoutes, bp), false);

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
				versionsArray.Add(new ScriptObject
				{
					{ "label", v.Label },
					{ "slug", v.Slug },
					{ "is_default", v.IsDefault },
					{ "is_prerelease", v.IsPrerelease }
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

		var orderedPages = ctx.AllPages
			.Where(p => p.FrontMatter.Visibility == PageVisibility.Public && p.FrontMatter.Layout == "default")
			.OrderBy(p => p.FrontMatter.Order)
			.ThenBy(p => p.Route, StringComparer.OrdinalIgnoreCase)
			.ToList();

		int currentIndex = orderedPages.FindIndex(p => p.Route == page.Route);
		if (currentIndex >= 0)
		{
			if (currentIndex > 0)
			{
				DocPage prev = orderedPages[currentIndex - 1];
				so.SetValue("prev_page", new ScriptObject
				{
					{ "title", prev.FrontMatter.Title },
					{ "route", PrefixRoute(prev.Route, bp) }
				}, false);
			}

			if (currentIndex < orderedPages.Count - 1)
			{
				DocPage next = orderedPages[currentIndex + 1];
				so.SetValue("next_page", new ScriptObject
				{
					{ "title", next.FrontMatter.Title },
					{ "route", PrefixRoute(next.Route, bp) }
				}, false);
			}
		}

		#endregion

		return so;
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

	private static ScriptArray BuildNavObject(NavigationTree? nav, string activeRoute, HashSet<string> pageRoutes,
		string basePath)
	{
		if (nav is null)
		{
			return [];
		}

		var arr = new ScriptArray();
		foreach (NavigationNode node in nav.Items)
		{
			arr.Add(BuildNavNodeObject(node, activeRoute, pageRoutes, basePath));
		}

		return arr;
	}

	private static ScriptObject BuildNavNodeObject(NavigationNode node, string activeRoute, HashSet<string> pageRoutes,
		string basePath)
	{
		bool hasActiveChild = HasActiveDescendant(node, activeRoute);
		bool hasChildren = node.Children.Count > 0;
		bool hasPage = !string.IsNullOrEmpty(node.Route) && pageRoutes.Contains(node.Route);

		// A node is "active" only if its route matches AND it doesn't have an active child.
		// This prevents parent sections from showing as "current" when their route was
		// resolved to a child's route (e.g. /guide → /guide/getting-started).
		bool isActive = node.Route == activeRoute && !hasActiveChild;

		var obj = new ScriptObject
		{
			{ "label", node.Label },
			{ "route", !string.IsNullOrEmpty(node.Route) ? PrefixRoute(node.Route, basePath) : "" },
			{ "icon", ResolveIcon(node.Icon) },
			{ "expanded", node.Expanded || hasActiveChild },
			{ "is_active", isActive },
			{ "has_active_child", hasActiveChild },
			{ "has_children", hasChildren },
			{ "has_page", hasPage }
		};

		var children = new ScriptArray();
		foreach (NavigationNode child in node.Children)
		{
			children.Add(BuildNavNodeObject(child, activeRoute, pageRoutes, basePath));
		}

		obj.SetValue("children", children, false);

		return obj;
	}

	private static bool HasActiveDescendant(NavigationNode node, string activeRoute)
	{
		foreach (NavigationNode child in node.Children)
		{
			if (child.Route == activeRoute)
			{
				return true;
			}

			if (HasActiveDescendant(child, activeRoute))
			{
				return true;
			}
		}

		return false;
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

	/// <summary>All pages in the site, used for prev/next navigation.</summary>
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

	/// <summary>Gets a template by layout name.</summary>
	public string? GetTemplate(string layoutName) => Templates.GetValueOrDefault(layoutName);
}
