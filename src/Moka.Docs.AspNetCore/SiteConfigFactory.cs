using Moka.Docs.AspNetCore.Phases;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Creates a <see cref="SiteConfig" /> from <see cref="MokaDocsOptions" />.
/// </summary>
internal static class SiteConfigFactory
{
	/// <summary>Id of the Blazor preview plugin, which <see cref="MokaDocsOptions.EnableBlazorPreview" /> declares.</summary>
	internal const string BlazorPreviewPluginId = "mokadocs-blazor-preview";

	/// <summary>
	///     Creates a site configuration from the user-provided options.
	/// </summary>
	public static SiteConfig Create(MokaDocsOptions options) => Create(options, null);

	/// <summary>
	///     Creates a site configuration from the user-provided options. With an API model,
	///     navigation entries marked <see cref="NavEntry.AutoGenerate" /> list its namespaces and types.
	/// </summary>
	public static SiteConfig Create(MokaDocsOptions options, ApiReference? apiModel)
	{
		var nav = new List<NavItem>();

		// Auto-generate nav if user didn't specify custom entries
		if (options.Nav.Count == 0)
		{
			nav.Add(new NavItem
			{
				Label = "API Reference",
				Path = "/api",
				Icon = "code",
				AutoGenerate = true,
				Children = CreateApiChildren("/api", apiModel)
			});

			if (options.DocsPath is not null)
			{
				nav.Add(new NavItem
				{
					Label = "Guide",
					Path = "/guide",
					Icon = "book-open",
					Expanded = true
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
				AutoGenerate = n.AutoGenerate,
				Children = n.AutoGenerate ? CreateApiChildren(n.Path, apiModel) : []
			}));
		}

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
			Options = p.Options is null ? [] : new Dictionary<string, object>(p.Options)
		}));

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
					ShowEditLink = false, // no source repo in embedded mode
					ShowLastUpdated = false
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
					}
			},
			Nav = nav,
			Plugins = plugins,
			Build = new BuildConfig
			{
				Output = "./_site",
				Clean = true,
				Sitemap = false,
				Robots = false,
				// The pipeline prefixes every route, asset and search result with this and tells the
				// theme script where the site lives. It used to stay "/" while the finished HTML was
				// patched afterwards, which missed the search index and the script's fetch paths.
				BasePath = SiteConfigReader.NormalizeBasePath(options.BasePath)
			}
		};
	}

	/// <summary>
	///     Whether <see cref="MokaDocsOptions.Plugins" /> declares the plugin with this id.
	/// </summary>
	internal static bool IsDeclared(MokaDocsOptions options, string pluginId) =>
		options.Plugins.Any(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	///     One child per namespace, holding one child per type, for an entry that points at the
	///     API reference. Any other path gets none, and the navigation phase fills in the pages
	///     one route segment below it.
	/// </summary>
	/// <remarks>
	///     That fill-in is why <see cref="NavEntry.AutoGenerate" /> needs this: type pages sit at
	///     <c>/api/{namespace path}/{type}</c>, never one segment below <c>/api</c>, so the API
	///     entry had no children whether the flag was set or not.
	/// </remarks>
	private static List<NavItem> CreateApiChildren(string? path, ApiReference? apiModel)
	{
		if (apiModel is null
		    || !string.Equals(path?.Trim().Trim('/'), "api", StringComparison.OrdinalIgnoreCase))
		{
			return [];
		}

		return apiModel.Namespaces
			.Where(ns => ns.Types.Count > 0)
			.Select(ns => new NavItem
			{
				// No path: no page exists for a namespace, so the theme shows it as a heading
				// instead of linking it to its first type.
				Label = ns.Name,
				Children = ns.Types
					.Select(type => new NavItem
					{
						Label = type.Name,
						Path = ReflectionApiPagePhase.GetTypeRoute(ns, type)
					})
					.ToList()
			})
			.ToList();
	}
}
