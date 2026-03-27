using Moka.Docs.Core.Configuration;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Creates a <see cref="SiteConfig" /> from <see cref="MokaDocsOptions" />.
/// </summary>
internal static class SiteConfigFactory
{
	/// <summary>
	///     Creates a site configuration from the user-provided options.
	/// </summary>
	public static SiteConfig Create(MokaDocsOptions options)
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
				AutoGenerate = true
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
				AutoGenerate = n.AutoGenerate
			}));
		}

		var plugins = new List<PluginDeclaration>();
		if (options.EnableRepl)
		{
			plugins.Add(new PluginDeclaration { Name = "mokadocs-repl" });
		}

		if (options.EnableBlazorPreview)
		{
			plugins.Add(new PluginDeclaration { Name = "mokadocs-blazor-preview" });
		}

		return new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = options.Title,
				Description = options.Description,
				Logo = options.LogoUrl ?? "",
				Favicon = options.FaviconUrl ?? "",
				Url = options.BasePath.TrimEnd('/'),
				Copyright = options.Copyright ?? $"© {DateTime.Now.Year} {options.Title}"
			},
			Content = new ContentConfig
			{
				Docs = "docs",
				Projects = [] // empty — we use reflection, not Roslyn
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
				Search = new SearchFeatureConfig { Enabled = true }
			},
			Nav = nav,
			Plugins = plugins,
			Build = new BuildConfig
			{
				Output = "./_site",
				Clean = true,
				Sitemap = false,
				Robots = false
			}
		};
	}
}
