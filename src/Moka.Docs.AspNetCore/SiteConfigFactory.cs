using Microsoft.Extensions.Hosting;
using Moka.Docs.AspNetCore.OpenApi;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
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
	///     The Blazor preview option that lets the plugin scaffold a preview host and run
	///     <c>dotnet publish</c> during the build.
	/// </summary>
	internal const string PublishHostOption = "publishHost";

	/// <summary>
	///     Creates a site configuration from the user-provided options. The navigation phase fills in
	///     the entries marked <see cref="NavEntry.AutoGenerate" />, the same way as for the CLI.
	/// </summary>
	/// <param name="options">The options.</param>
	/// <param name="environment">
	///     The app's environment. Outside Development the Blazor preview plugin gets
	///     <c>publishHost: false</c>, unless its entry sets the option. <c>null</c> leaves it alone.
	/// </param>
	public static SiteConfig Create(MokaDocsOptions options, IHostEnvironment? environment = null)
	{
		List<PluginDeclaration> plugins = CreatePluginDeclarations(options, environment);
		string docsDirectory = DocsDirectory(ContentRoot(environment), options.DocsPath);
		var nav = new List<NavItem>();

		// Auto-generate nav if user didn't specify custom entries
		if (options.Nav.Count == 0)
		{
			nav.Add(new NavItem
			{
				Label = ThemeText.For(options.Language, options.Strings)["api.title"],
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
				Image = CreateSocialImage(options.SocialImage, docsDirectory),
				// The app can't tell its public URL from a request, so without SiteUrl there are no
				// canonical links or link preview tags. This once held the base path, which is not a
				// URL and doubled in every canonical link.
				Url = options.SiteUrl?.Trim() ?? "",
				Copyright = options.Copyright ?? $"© {DateTime.Now.Year} {options.Title}",
				Language = string.IsNullOrWhiteSpace(options.Language) ? SiteLanguages.Default : options.Language.Trim(),
				Direction = options.Direction?.Trim() ?? ""
			},
			Content = new ContentConfig
			{
				Docs = "docs",
				Projects = [] // empty - we use reflection, not Roslyn
			},
			Theme = new ThemeConfig
			{
				Name = "default",
				Options = WithPreset(new ThemeOptions
				{
					PrimaryColor = options.PrimaryColor,
					AccentColor = options.AccentColor,
					Style = options.Style,
					StyleSelector = options.StyleSelector,
					TextSizeSelector = options.TextSizeSelector,
					WidthSelector = options.WidthSelector,
					LayoutSelector = options.LayoutSelector,
					ImageZoom = options.ImageZoom,
					Layout = CreateLayoutOptions(options.Layout),
					Sidebar = options.Sidebar,
					SidebarToggle = options.SidebarToggle,
					ShowEditLink = false, // no source repo in embedded mode
					ShowLastUpdated = false,
					// MapMokaDocs maps the endpoint the widget posts to only when this is on.
					ShowFeedback = options.ShowFeedback,
					Banner = CreateBanner(options.Banner),
					Strings = new Dictionary<string, string>(options.Strings, StringComparer.Ordinal)
				}, options.Preset)
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
				Llms = options.Llms,
				// The pipeline prefixes every route, asset and search result with this and tells the
				// theme script where the site lives. It used to stay "/" while the finished HTML was
				// patched afterwards, which missed the search index and the script's fetch paths.
				// Browsers resolve the links against the public URL, which starts with the app's
				// path base when the app isn't at the root of its host.
				BasePath = PublicBasePath(options)
			},
			Glossary = options.Glossary
				.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
				.ToDictionary(entry => entry.Key.Trim(), entry => entry.Value.Trim(), StringComparer.Ordinal)
		};
	}

	/// <summary>
	///     The blog settings for <see cref="MokaDocsOptions.Blog" />, or a disabled blog. Feeds stay
	///     off: they need <c>site.url</c>, which is empty unless <see cref="MokaDocsOptions.SiteUrl" />
	///     is set, and asking for one without it would only warn on every build.
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
			PageNav = layout.PageNav,
			Sidebar = layout.Sidebar,
			SidebarScope = layout.SidebarScope,
			Home = layout.Home,
			Hero = layout.Hero,
			HeroBackground = layout.HeroBackground,
			Features = layout.Features,
			Admonitions = layout.Admonitions,
			Tables = layout.Tables,
			Density = layout.Density,
			Font = layout.Font,
			Headings = layout.Headings,
			Links = layout.Links
		};

	/// <summary>
	///     <paramref name="theme" /> with <see cref="MokaDocsOptions.Preset" /> applied: the preset fills in
	///     each option still at its default value, since a property that was never set looks the same as
	///     one set to its default. An unknown name applies nothing; the render phase warns about it.
	/// </summary>
	private static ThemeOptions WithPreset(ThemeOptions theme, string? preset)
	{
		theme = theme with { Preset = preset?.Trim() ?? "" };
		return ThemePresets.Find(preset) is { } found ? ThemePresetMerge.FillDefaults(theme, found) : theme;
	}

	/// <summary>
	///     The banner of <see cref="MokaDocsOptions.Banner" />, or <c>null</c> when there is none or its
	///     text is empty.
	/// </summary>
	private static ThemeBanner? CreateBanner(MokaDocsBannerOptions? banner) =>
		banner is null || string.IsNullOrWhiteSpace(banner.Text)
			? null
			: new ThemeBanner
			{
				Text = banner.Text.Trim(),
				Link = string.IsNullOrWhiteSpace(banner.Link) ? null : banner.Link.Trim(),
				LinkText = string.IsNullOrWhiteSpace(banner.LinkText) ? null : banner.LinkText.Trim(),
				Id = string.IsNullOrWhiteSpace(banner.Id) ? null : banner.Id.Trim(),
				Dismissible = banner.Dismissible,
				Variant = banner.Variant
			};

	/// <summary>
	///     Where the build reads the docs from: <see cref="MokaDocsOptions.DocsPath" />, or <c>docs</c>
	///     when it isn't set, from the content root.
	/// </summary>
	internal static string DocsDirectory(string contentRoot, string? docsPath) =>
		Path.GetFullPath(Path.Combine(contentRoot, docsPath ?? "docs"));

	/// <summary>
	///     The app's content root, where relative paths in the options resolve from, or the working
	///     directory without a host environment.
	/// </summary>
	/// <remarks>
	///     Relative paths used to resolve from the working directory, so starting the app from another
	///     folder, or with --contentRoot, found no docs.
	/// </remarks>
	internal static string ContentRoot(IHostEnvironment? environment) =>
		environment?.ContentRootPath is { Length: > 0 } root ? root : Directory.GetCurrentDirectory();

	/// <summary>
	///     <see cref="MokaDocsOptions.SocialImage" /> as a site asset: a URL as written, or a file under
	///     <paramref name="docsDirectory" /> at the URL the site publishes it under. <c>null</c> when it
	///     isn't set or names a file outside <paramref name="docsDirectory" />, which the build never reads.
	/// </summary>
	internal static SiteAssetReference? CreateSocialImage(string? image, string docsDirectory)
	{
		if (string.IsNullOrWhiteSpace(image))
		{
			return null;
		}

		string value = image.Trim();
		if (IsUrl(value))
		{
			return new SiteAssetReference { RawValue = image, SourcePath = null, PublishUrl = value, IsAbsoluteUrl = true };
		}

		string sourcePath = Path.GetFullPath(Path.Combine(docsDirectory, value.Replace('\\', '/').TrimStart('/')));
		string relative = Path.GetRelativePath(docsDirectory, sourcePath);
		if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
		{
			return null;
		}

		return new SiteAssetReference
		{
			RawValue = image,
			SourcePath = sourcePath,
			PublishUrl = "/" + relative.Replace(Path.DirectorySeparatorChar, '/'),
			IsAbsoluteUrl = false
		};
	}

	/// <summary>
	///     Whether <paramref name="value" /> has a scheme (<c>https:</c>, <c>data:</c>) or is
	///     protocol-relative. A one-letter scheme is a Windows drive.
	/// </summary>
	private static bool IsUrl(string value) =>
		value.StartsWith("//", StringComparison.Ordinal)
		|| (value.IndexOf(':', StringComparison.Ordinal) is > 1 and var colon
		    && char.IsAsciiLetter(value[0])
		    && value[..colon].All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.'));

	/// <summary>
	///     The prefix of every link in the site: <see cref="MokaDocsOptions.PathBase" /> followed by
	///     <see cref="MokaDocsOptions.BasePath" />, as <see cref="SiteConfigReader.NormalizeBasePath" /> shapes it.
	/// </summary>
	internal static string PublicBasePath(MokaDocsOptions options) =>
		SiteConfigReader.NormalizeBasePath(NormalizePathBase(options.PathBase) + "/" + options.BasePath?.Trim('/'));

	/// <summary>
	///     A path base as <c>HttpRequest.PathBase</c> holds it: empty, or a leading <c>/</c> and no
	///     trailing one.
	/// </summary>
	internal static string NormalizePathBase(string? pathBase) =>
		pathBase?.Trim().Trim('/') is { Length: > 0 } trimmed ? "/" + trimmed : "";

	/// <summary>
	///     Whether <see cref="MokaDocsOptions.Plugins" /> declares the plugin with this id.
	/// </summary>
	internal static bool IsDeclared(MokaDocsOptions options, string pluginId) =>
		options.Plugins.Any(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

	private static List<PluginDeclaration> CreatePluginDeclarations(MokaDocsOptions options, IHostEnvironment? environment)
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

		// Outside Development the plugin uses a published host or static snapshots: a scaffold and a
		// `dotnet publish` inside a running production app need the SDK on the server, write to the
		// app's folder and hold the first build for minutes.
		if (environment is not null && !environment.IsDevelopment())
		{
			foreach (PluginDeclaration preview in plugins.Where(p =>
				         string.Equals(p.Name, BlazorPreviewPluginId, StringComparison.OrdinalIgnoreCase)))
			{
				preview.Options.TryAdd(PublishHostOption, false);
			}
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
