using Moka.Docs.AspNetCore.OpenApi;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Creates a <see cref="SiteConfig" /> from <see cref="MokaDocsOptions" />.
/// </summary>
internal static class SiteConfigFactory
{
	/// <summary>Id of the Blazor preview plugin, which <see cref="MokaDocsOptions.EnableBlazorPreview" /> declares.</summary>
	internal const string BlazorPreviewPluginId = "mokadocs-blazor-preview";

	/// <summary>
	///     Creates a site configuration from the user-provided options. The navigation phase fills in
	///     the entries marked <see cref="NavEntry.AutoGenerate" />, the same way as for the CLI.
	/// </summary>
	public static SiteConfig Create(MokaDocsOptions options)
	{
		List<PluginDeclaration> plugins = CreatePluginDeclarations(options);
		var nav = new List<NavItem>();

		// Auto-generate nav if user didn't specify custom entries
		if (options.Nav.Count == 0)
		{
			nav.Add(new NavItem
			{
				Label = "API Reference",
				Path = "/api",
				Icon = "code",
				AutoGenerate = true
			});

			// The REST pages had no way into the default sidebar: they sit at their own prefix, and only
			// a custom Nav could point at it.
			nav.AddRange(CreateOpenApiNavItems(plugins));

			if (options.DocsPath is not null)
			{
				nav.Add(new NavItem
				{
					Label = "Guide",
					Path = "/guide",
					Icon = "book-open",
					Expanded = true,
					AutoGenerate = true
				});
			}

			if (options.DocsPath is not null && options.Blog is { } blog)
			{
				nav.Add(new NavItem
				{
					Label = string.IsNullOrWhiteSpace(blog.Title) ? "Blog" : blog.Title,
					Path = blog.Route ?? "/" + blog.Path.Replace('\\', '/').Trim('/'),
					Icon = "newspaper"
				});
			}
		}
		else
		{
			nav.AddRange(options.Nav.Select(n => new NavItem
			{
				Label = n.Label,
				Path = n.Path,
				Icon = n.Icon,
				Expanded = n.Expanded,
				AutoGenerate = n.AutoGenerate
			}));
		}

		return new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = options.Title,
				Description = options.Description,
				// LogoUrl/FaviconUrl from ASP.NET Core options are treated as pre-resolved URLs
				// that the consumer is hosting themselves (e.g. /images/logo.png on their own
				// wwwroot). Wrap them in SiteAssetReference with IsAbsoluteUrl=true so the
				// template emits them verbatim without filesystem lookup or copy.
				Logo = string.IsNullOrEmpty(options.LogoUrl)
					? null
					: new SiteAssetReference
					{
						RawValue = options.LogoUrl,
						SourcePath = null,
						PublishUrl = options.LogoUrl,
						IsAbsoluteUrl = true
					},
				Favicon = string.IsNullOrEmpty(options.FaviconUrl)
					? null
					: new SiteAssetReference
					{
						RawValue = options.FaviconUrl,
						SourcePath = null,
						PublishUrl = options.FaviconUrl,
						IsAbsoluteUrl = true
					},
				// No public URL is known in embedded mode, so no canonical links or Open Graph URLs.
				// This held the base path, which is not a URL and doubled in every canonical link.
				Url = "",
				Copyright = options.Copyright ?? $"© {DateTime.Now.Year} {options.Title}"
			},
			Content = new ContentConfig
			{
				Docs = "docs",
				Projects = [] // empty - we use reflection, not Roslyn
			},
			Theme = new ThemeConfig
			{
				Name = "default",
				Options = new ThemeOptions
				{
					PrimaryColor = options.PrimaryColor,
					AccentColor = options.AccentColor,
					Style = options.Style,
					StyleSelector = options.StyleSelector,
					Layout = CreateLayoutOptions(options.Layout),
					Sidebar = options.Sidebar,
					ShowEditLink = false, // no source repo in embedded mode
					ShowLastUpdated = false,
					// MapMokaDocs maps the endpoint the widget posts to only when this is on.
					ShowFeedback = options.ShowFeedback
				}
			},
			Features = new FeaturesConfig
			{
				Search = new SearchFeatureConfig { Enabled = true },
				// The header's version selector lists these, as it does for
				// features.versioning.versions in mokadocs.yaml. Version used to be read by nothing.
				Versioning = string.IsNullOrWhiteSpace(options.Version)
					? new VersioningFeatureConfig()
					: new VersioningFeatureConfig
					{
						Enabled = true,
						Versions = [new VersionDefinition { Label = options.Version, Default = true }]
					},
				Blog = CreateBlogConfig(options.Blog)
			},
			Nav = nav,
			Plugins = plugins,
			Build = new BuildConfig
			{
				Output = "./_site",
				Clean = true,
				Sitemap = false,
				Robots = false,
				Minify = options.Minify,
				// The pipeline prefixes every route, asset and search result with this and tells the
				// theme script where the site lives. It used to stay "/" while the finished HTML was
				// patched afterwards, which missed the search index and the script's fetch paths.
				BasePath = SiteConfigReader.NormalizeBasePath(options.BasePath)
			}
		};
	}

	/// <summary>
	///     The blog settings for <see cref="MokaDocsOptions.Blog" />, or a disabled blog. Feeds stay
	///     off: they need <c>site.url</c>, which is empty here, and asking for one would only warn on
	///     every build.
	/// </summary>
	private static BlogFeatureConfig CreateBlogConfig(MokaDocsBlogOptions? blog) =>
		blog is null
			? new BlogFeatureConfig()
			: new BlogFeatureConfig
			{
				Enabled = true,
				Path = blog.Path,
				Route = blog.Route,
				Title = blog.Title,
				Description = blog.Description,
				PostsPerPage = blog.PostsPerPage,
				SidebarPosts = blog.SidebarPosts,
				ShowAuthors = blog.ShowAuthors,
				Feeds = [],
				// The caller may have replaced the dictionary with a case-sensitive one holding both
				// "Jane" and "jane"; the later entry wins instead of the build throwing.
				Authors = blog.Authors.Aggregate(new Dictionary<string, BlogAuthor>(StringComparer.OrdinalIgnoreCase),
					(authors, entry) =>
					{
						authors[entry.Key] = entry.Value;
						return authors;
					})
			};

	/// <summary>
	///     The layout variants of <see cref="MokaDocsOptions.Layout" />, as written. The render phase
	///     warns about an unknown name, and the theme renders that part as <c>default</c>.
	/// </summary>
	private static ThemeLayoutOptions CreateLayoutOptions(MokaDocsLayoutOptions layout) =>
		new ThemeLayoutOptions
		{
			Width = layout.Width,
			Navbar = layout.Navbar,
			Footer = layout.Footer,
			Toc = layout.Toc,
			Breadcrumbs = layout.Breadcrumbs,
			PageNav = layout.PageNav
		};

	/// <summary>
	///     Whether <see cref="MokaDocsOptions.Plugins" /> declares the plugin with this id.
	/// </summary>
	internal static bool IsDeclared(MokaDocsOptions options, string pluginId) =>
		options.Plugins.Any(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

	private static List<PluginDeclaration> CreatePluginDeclarations(MokaDocsOptions options)
	{
		var plugins = new List<PluginDeclaration>();
		if (options.EnableRepl)
		{
			plugins.Add(new PluginDeclaration { Name = "mokadocs-repl" });
		}

		// A Plugins entry for the preview carries its options. A second, bare declaration next to
		// it would load the plugin twice, the second time without previewHost or library.
		if (options.EnableBlazorPreview && !IsDeclared(options, BlazorPreviewPluginId))
		{
			plugins.Add(new PluginDeclaration { Name = BlazorPreviewPluginId });
		}

		plugins.AddRange(options.Plugins.Select(p => new PluginDeclaration
		{
			Name = p.Id,
			Options = WithOpenApiDefaults(p.Id, p.Options is null ? [] : new Dictionary<string, object>(p.Options), options)
		}));

		if (!string.IsNullOrWhiteSpace(options.OpenApiDocument))
		{
			plugins.Add(new PluginDeclaration
			{
				Name = AppOpenApiDocument.PluginId,
				Options = WithOpenApiDefaults(AppOpenApiDocument.PluginId,
					new Dictionary<string, object> { [AppOpenApiDocument.DocumentNameOption] = options.OpenApiDocument },
					options)
			});
		}

		return plugins;
	}

	/// <summary>
	///     Fills in the route prefix and label of an <c>openapi</c> entry that doesn't set them.
	/// </summary>
	/// <remarks>
	///     The plugin's own default prefix is <c>/api</c>, where this host always puts the API reference,
	///     so the two index pages collided and only one was served. The CLI keeps <c>/api</c>: changing it
	///     there would move the pages of existing sites.
	/// </remarks>
	private static Dictionary<string, object> WithOpenApiDefaults(string pluginId, Dictionary<string, object> pluginOptions,
		MokaDocsOptions options)
	{
		if (string.Equals(pluginId, AppOpenApiDocument.PluginId, StringComparison.OrdinalIgnoreCase))
		{
			pluginOptions.TryAdd(OpenApiPlugin.RoutePrefixOption, options.OpenApiRoutePrefix);
			pluginOptions.TryAdd(OpenApiPlugin.LabelOption, options.OpenApiLabel);
		}

		return pluginOptions;
	}

	/// <summary>
	///     A sidebar entry for each route prefix that <c>openapi</c> entries generate pages under. The
	///     navigation phase lists the tag pages below it.
	/// </summary>
	private static IEnumerable<NavItem> CreateOpenApiNavItems(List<PluginDeclaration> plugins) =>
		plugins
			.Where(p => string.Equals(p.Name, AppOpenApiDocument.PluginId, StringComparison.OrdinalIgnoreCase))
			.Select(p => (
				Path: OpenApiPlugin.NormalizeRoutePrefix(p.Options.GetValueOrDefault(OpenApiPlugin.RoutePrefixOption) as string ?? ""),
				Label: p.Options.GetValueOrDefault(OpenApiPlugin.LabelOption) as string ?? OpenApiPlugin.DefaultLabel))
			.Where(item => item.Path.Length > 0)
			.DistinctBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
			.Select(item => new NavItem { Label = item.Label, Path = item.Path, Icon = "globe" });
}
