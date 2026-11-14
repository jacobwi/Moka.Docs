using System.IO.Abstractions;
using Moka.Docs.Core.Theming;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Reads and parses a mokadocs.yaml configuration file into a <see cref="SiteConfig" />.
/// </summary>
public sealed class SiteConfigReader
{
	private static readonly IDeserializer _deserializer = new DeserializerBuilder()
		.WithNamingConvention(CamelCaseNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.Build();

	private readonly IFileSystem _fileSystem;

	/// <summary>
	///     Creates a new config reader.
	/// </summary>
	public SiteConfigReader(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}

	/// <summary>
	///     Reads and deserializes a mokadocs.yaml file.
	/// </summary>
	/// <param name="configPath">Absolute path to the YAML configuration file.</param>
	/// <returns>The parsed <see cref="SiteConfig" />.</returns>
	/// <exception cref="FileNotFoundException">Thrown when the config file does not exist.</exception>
	/// <exception cref="SiteConfigException">Thrown when the config file is invalid.</exception>
	public SiteConfig Read(string configPath)
	{
		if (!_fileSystem.File.Exists(configPath))
		{
			throw new FileNotFoundException($"Configuration file not found: {configPath}", configPath);
		}

		string yaml = _fileSystem.File.ReadAllText(configPath);
		string? yamlDir = _fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetFullPath(configPath));
		return Parse(yaml, yamlDir);
	}

	/// <summary>
	///     Parses a YAML string into a <see cref="SiteConfig" />.
	/// </summary>
	/// <param name="yaml">The YAML content.</param>
	/// <param name="yamlDir">
	///     Optional absolute directory containing the source mokadocs.yaml file. Used to resolve
	///     relative brand asset paths (<c>site.logo</c>, <c>site.favicon</c>) against the yaml
	///     file's location. When null (e.g. when parsing a yaml string that isn't backed by a
	///     file), brand assets keep their raw value but <see cref="SiteAssetReference.SourcePath" />
	///     will be null, meaning the build pipeline won't copy them - that's the correct behavior
	///     for virtual / in-memory configs.
	/// </param>
	/// <returns>The parsed <see cref="SiteConfig" />.</returns>
	/// <exception cref="SiteConfigException">Thrown when the YAML is invalid or missing required fields.</exception>
	public static SiteConfig Parse(string yaml, string? yamlDir = null)
	{
		try
		{
			SiteConfigDto dto = _deserializer.Deserialize<SiteConfigDto>(yaml)
			                    ?? throw new SiteConfigException("Configuration file is empty.");

			return MapFromDto(dto, yamlDir) with
			{
				Warnings = [.. YamlConfigChecks.FindUnknownKeys(yaml, typeof(SiteConfigDto)), .. GlossaryWarnings(dto.Glossary)]
			};
		}
		catch (SiteConfigException)
		{
			throw;
		}
		catch (YamlException ex)
		{
			throw new SiteConfigException($"Failed to parse configuration: {YamlConfigChecks.Describe(ex)}", ex);
		}
		catch (Exception ex)
		{
			throw new SiteConfigException($"Failed to parse configuration: {ex.Message}", ex);
		}
	}

	/// <summary>
	///     Serializes a <see cref="SiteConfig" /> back to YAML.
	/// </summary>
	public static string ToYaml(SiteConfig config)
	{
		SiteConfigDto dto = MapToDto(config);
		ISerializer serializer = new SerializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
			.Build();

		return serializer.Serialize(dto);
	}

	#region DTO Mapping

	private static SiteConfig MapFromDto(SiteConfigDto dto, string? yamlDir = null)
	{
		SiteMetadataDto siteDto = dto.Site ??
		                          throw new SiteConfigException(
			                          "'site' section is required and must include a 'title'.");
		if (string.IsNullOrWhiteSpace(siteDto.Title))
		{
			throw new SiteConfigException("'site.title' is required.");
		}

		SiteAssetReference? logo = ParseAssetReference(siteDto.Logo, yamlDir);
		SiteAssetReference? favicon = ParseAssetReference(siteDto.Favicon, yamlDir);
		SiteAssetReference? image = ParseAssetReference(
			MokaDefaults.ResolveString("SITE_IMAGE", siteDto.Image, ""), yamlDir);

		// Collision check: if two assets flatten to the SAME publish URL (same filename
		// flattened from different escape paths), the second would silently overwrite the
		// first. Fail loud so the user can rename the source file or move it.
		(string Key, SiteAssetReference? Asset)[] brandAssets =
			[("site.logo", logo), ("site.favicon", favicon), ("site.image", image)];
		for (int i = 0; i < brandAssets.Length; i++)
		for (int j = i + 1; j < brandAssets.Length; j++)
		{
			if (brandAssets[i].Asset is { ShouldCopy: true } first && brandAssets[j].Asset is { ShouldCopy: true } second
			                                                        && first.PublishUrl == second.PublishUrl
			                                                        && !string.Equals(first.SourcePath, second.SourcePath,
				                                                        StringComparison.OrdinalIgnoreCase))
			{
				throw new SiteConfigException(
					$"'{brandAssets[i].Key}' ('{first.RawValue}') and '{brandAssets[j].Key}' ('{second.RawValue}') resolve to " +
					$"the same publish URL '{first.PublishUrl}' from different source files. " +
					"Rename one of them or move them to distinct filenames so they don't collide in the output directory.");
			}
		}

		return new SiteConfig
		{
			Site = new SiteMetadata
			{
				Title = siteDto.Title,
				Description = siteDto.Description ?? "",
				Url = siteDto.Url ?? "",
				Logo = logo,
				Favicon = favicon,
				Image = image,
				Copyright = siteDto.Copyright,
				EditLink = siteDto.EditLink is { } el
					? new EditLinkConfig
					{
						Repo = el.Repo ?? "",
						Branch = el.Branch ?? "main",
						Path = el.Path ?? "docs/"
					}
					: null,
				Language = MokaDefaults.ResolveString("SITE_LANGUAGE", NullIfBlank(siteDto.Language), SiteLanguages.Default).Trim(),
				Direction = MokaDefaults.ResolveString("SITE_DIRECTION", siteDto.Direction, "").Trim()
			},
			Content = MapContentConfig(dto.Content),
			Theme = MapThemeConfig(dto.Theme),
			Nav = MapNavItems(dto.Nav),
			Features = MapFeaturesConfig(dto.Features),
			Plugins = MapPlugins(dto.Plugins),
			Cloud = MapCloudConfig(dto.Cloud),
			Build = MapBuildConfig(dto.Build),
			Glossary = MapGlossary(dto.Glossary)
		};
	}

	// A key with no text (strings: { toc.title: }) gives an empty text, which hides a label the site doesn't want.
	private static Dictionary<string, string> MapStrings(Dictionary<string, string?>? dto)
	{
		var strings = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach ((string key, string? text) in dto ?? [])
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				strings[key.Trim()] = text ?? "";
			}
		}

		return strings;
	}

	private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

	private static Dictionary<string, string> MapGlossary(Dictionary<string, string?>? dto)
	{
		var glossary = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach ((string term, string? definition) in dto ?? [])
		{
			if (!string.IsNullOrWhiteSpace(term) && !string.IsNullOrWhiteSpace(definition))
			{
				glossary[term.Trim()] = definition.Trim();
			}
		}

		return glossary;
	}

	private static IEnumerable<string> GlossaryWarnings(Dictionary<string, string?>? dto) =>
		(dto ?? [])
		.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && string.IsNullOrWhiteSpace(entry.Value))
		.Select(entry => $"glossary term '{entry.Key.Trim()}' has no definition and is ignored");

	private static ContentConfig MapContentConfig(ContentConfigDto? dto)
	{
		if (dto is null)
		{
			return new ContentConfig();
		}

		return new ContentConfig
		{
			Docs = NormalizeSeparators(dto.Docs) ?? "./docs",
			Projects = dto.Projects?.Select(p => new ProjectSource
			{
				Path = NormalizeSeparators(p.Path) ?? "",
				Label = p.Label,
				IncludeInternals = p.IncludeInternals
			}).ToList() ?? []
		};
	}

	// Sections missing from the yaml are mapped from an empty DTO rather than skipped, so the
	// MOKADOCS_* environment variables still apply. They used to be ignored unless the yaml
	// already had the section, e.g. MOKADOCS_PRIMARY_COLOR without a theme.options block.
	private static ThemeConfig MapThemeConfig(ThemeConfigDto? dto)
	{
		dto ??= new ThemeConfigDto();
		ThemeOptionsDto o = dto.Options ?? new ThemeOptionsDto();
		string preset = ApplyPreset(o);

		return new ThemeConfig
		{
			Name = NormalizeSeparators(MokaDefaults.ResolveString("THEME_NAME", dto.Name, MokaDefaults.ThemeName))!,
			Options = new ThemeOptions
				{
					PrimaryColor = MokaDefaults.ResolveString(
						"PRIMARY_COLOR", o.PrimaryColor, MokaDefaults.PrimaryColor),
					AccentColor = o.AccentColor ?? "#f59e0b",
					CodeTheme = MokaDefaults.ResolveString(
						"CODE_THEME", o.CodeTheme, MokaDefaults.CodeTheme),
					ShowEditLink = MokaDefaults.ResolveBool(
						"SHOW_EDIT_LINK", o.ShowEditLink, MokaDefaults.ShowEditLink),
					ShowLastUpdated = MokaDefaults.ResolveBool(
						"SHOW_LAST_UPDATED", o.ShowLastUpdated, MokaDefaults.ShowLastUpdated),
					ShowContributors = MokaDefaults.ResolveBool(
						"SHOW_CONTRIBUTORS", o.ShowContributors, MokaDefaults.ShowContributors),
					ColorThemes = MokaDefaults.ResolveBool(
						"SHOW_COLOR_THEME_SELECTOR", o.ColorThemes, MokaDefaults.ShowColorThemeSelector),
					CodeThemeSelector = MokaDefaults.ResolveBool(
						"SHOW_CODE_THEME_SELECTOR", o.CodeThemeSelector, MokaDefaults.ShowCodeThemeSelector),
					CodeStyle = MokaDefaults.ResolveString(
						"CODE_STYLE", o.CodeStyle, MokaDefaults.CodeStyle),
					CodeStyleSelector = MokaDefaults.ResolveBool(
						"SHOW_CODE_STYLE_SELECTOR", o.CodeStyleSelector, MokaDefaults.ShowCodeStyleSelector),
					Preset = preset,
					Style = MokaDefaults.ResolveString(
						"STYLE", o.Style, MokaDefaults.Style),
					StyleSelector = MokaDefaults.ResolveBool(
						"SHOW_STYLE_SELECTOR", o.StyleSelector, MokaDefaults.ShowStyleSelector),
					TextSizeSelector = MokaDefaults.ResolveBool(
						"SHOW_TEXT_SIZE_SELECTOR", o.TextSizeSelector, MokaDefaults.ShowTextSizeSelector),
					WidthSelector = MokaDefaults.ResolveBool(
						"SHOW_WIDTH_SELECTOR", o.WidthSelector, MokaDefaults.ShowWidthSelector),
					LayoutSelector = MokaDefaults.ResolveBool(
						"SHOW_LAYOUT_SELECTOR", o.LayoutSelector, MokaDefaults.ShowLayoutSelector),
					Layout = MapLayoutOptions(o.Layout),
					Sidebar = MokaDefaults.ResolveString(
						"SIDEBAR", o.Sidebar, MokaDefaults.Sidebar),
					SidebarInlineLimit = Math.Max(0, MokaDefaults.ResolveInt(
						"SIDEBAR_INLINE_LIMIT", o.SidebarInlineLimit, MokaDefaults.SidebarInlineLimit)),
					SidebarToggle = MokaDefaults.ResolveBool(
						"SIDEBAR_TOGGLE", o.SidebarToggle, MokaDefaults.SidebarToggle),
					ShowFeedback = MokaDefaults.ResolveBool(
						"SHOW_FEEDBACK", o.ShowFeedback, MokaDefaults.ShowFeedbackWidget),
					ShowDarkModeToggle = MokaDefaults.ResolveBool(
						"SHOW_DARK_MODE_TOGGLE", o.ShowDarkModeToggle, MokaDefaults.ShowDarkModeToggle),
					ShowAnimations = MokaDefaults.ResolveBool(
						"SHOW_ANIMATIONS", o.ShowAnimations, MokaDefaults.ShowAnimations),
					ShowSearch = MokaDefaults.ResolveBool(
						"SHOW_SEARCH", o.ShowSearch, MokaDefaults.ShowSearch),
					ShowTableOfContents = MokaDefaults.ResolveBool(
						"SHOW_TABLE_OF_CONTENTS", o.ShowTableOfContents, MokaDefaults.ShowTableOfContents),
					ShowPrevNext = MokaDefaults.ResolveBool(
						"SHOW_PREV_NEXT", o.ShowPrevNext, MokaDefaults.ShowPrevNext),
					ShowBreadcrumbs = MokaDefaults.ResolveBool(
						"SHOW_BREADCRUMBS", o.ShowBreadcrumbs, MokaDefaults.ShowBreadcrumbs),
					ShowBackToTop = MokaDefaults.ResolveBool(
						"SHOW_BACK_TO_TOP", o.ShowBackToTop, MokaDefaults.ShowBackToTop),
					ShowCopyButton = MokaDefaults.ResolveBool(
						"SHOW_COPY_BUTTON", o.ShowCopyButton, MokaDefaults.ShowCopyButton),
					ShowLineNumbers = MokaDefaults.ResolveBool(
						"SHOW_LINE_NUMBERS", o.ShowLineNumbers, MokaDefaults.ShowLineNumbers),
					ImageZoom = MokaDefaults.ResolveBool(
						"IMAGE_ZOOM", o.ImageZoom, MokaDefaults.ImageZoom),
					TocDepth = Math.Clamp(o.TocDepth ?? MokaDefaults.TocDepth, 2, 6),
					ShowVersionSelector = MokaDefaults.ResolveBool(
						"SHOW_VERSION_SELECTOR", o.ShowVersionSelector, MokaDefaults.ShowVersionSelector),
					ShowBuiltWith = MokaDefaults.ResolveBool(
						"SHOW_BUILT_WITH", o.ShowBuiltWith, MokaDefaults.ShowBuiltWith),
					SocialLinks = o.SocialLinks?.Select(s => new SocialLink
					{
						Icon = s.Icon ?? "",
						Url = s.Url ?? ""
					}).ToList() ?? [],
					DefaultColorTheme = o.DefaultColorTheme ?? "ocean",
					Banner = MapBanner(o.Banner),
					Strings = MapStrings(o.Strings)
				}
		};
	}

	// The preset goes in under the yaml: it fills the options the yaml leaves out, and the
	// environment variables still win over both. The name is kept as written so the render phase can
	// quote an unknown one in its warning.
	private static string ApplyPreset(ThemeOptionsDto options)
	{
		string preset = MokaDefaults.ResolveString("THEME_PRESET", options.Preset, "");
		if (ThemePresets.Find(preset) is { } found)
		{
			ThemePresetMerge.FillMissing(options, found);
		}

		return preset;
	}

	// MOKADOCS_BANNER_TEXT and MOKADOCS_BANNER_LINK work without a banner block, so a CI job can
	// announce a release without editing mokadocs.yaml.
	private static ThemeBanner? MapBanner(ThemeBannerDto? dto)
	{
		dto ??= new ThemeBannerDto();
		string text = MokaDefaults.ResolveString("BANNER_TEXT", dto.Text, "").Trim();
		if (text.Length == 0)
		{
			return null;
		}

		string link = MokaDefaults.ResolveString("BANNER_LINK", dto.Link, "").Trim();
		return new ThemeBanner
		{
			Text = text,
			Link = link.Length == 0 ? null : link,
			LinkText = string.IsNullOrWhiteSpace(dto.LinkText) ? null : dto.LinkText.Trim(),
			Id = string.IsNullOrWhiteSpace(dto.Id) ? null : dto.Id.Trim(),
			Dismissible = dto.Dismissible ?? true,
			Variant = dto.Variant ?? BannerVariants.Info
		};
	}

	// Names are kept as written, like the style, so the render phase can quote an unknown one in its
	// warning. ThemeLayouts resolves them when the pages are rendered.
	private static ThemeLayoutOptions MapLayoutOptions(ThemeLayoutDto? dto)
	{
		dto ??= new ThemeLayoutDto();

		return new ThemeLayoutOptions
		{
			Width = MokaDefaults.ResolveString("LAYOUT_WIDTH", dto.Width, ThemeLayouts.Default),
			Navbar = MokaDefaults.ResolveString("LAYOUT_NAVBAR", dto.Navbar, ThemeLayouts.Default),
			Footer = MokaDefaults.ResolveString("LAYOUT_FOOTER", dto.Footer, ThemeLayouts.Default),
			Toc = MokaDefaults.ResolveString("LAYOUT_TOC", dto.Toc, ThemeLayouts.Default),
			Breadcrumbs = MokaDefaults.ResolveString("LAYOUT_BREADCRUMBS", dto.Breadcrumbs, ThemeLayouts.Default),
			PageNav = MokaDefaults.ResolveString("LAYOUT_PAGE_NAV", dto.PageNav, ThemeLayouts.Default),
			Sidebar = MokaDefaults.ResolveString("LAYOUT_SIDEBAR", dto.Sidebar, ThemeLayouts.Default),
			SidebarScope = MokaDefaults.ResolveString("LAYOUT_SIDEBAR_SCOPE", dto.SidebarScope, ThemeLayouts.Default),
			Home = MokaDefaults.ResolveString("LAYOUT_HOME", dto.Home, ThemeLayouts.Default),
			Hero = MokaDefaults.ResolveString("LAYOUT_HERO", dto.Hero, ThemeLayouts.Default),
			HeroBackground = MokaDefaults.ResolveString("LAYOUT_HERO_BACKGROUND", dto.HeroBackground, ThemeLayouts.Default),
			Features = MokaDefaults.ResolveString("LAYOUT_FEATURES", dto.Features, ThemeLayouts.Default),
			Admonitions = MokaDefaults.ResolveString("LAYOUT_ADMONITIONS", dto.Admonitions, ThemeLayouts.Default),
			Tables = MokaDefaults.ResolveString("LAYOUT_TABLES", dto.Tables, ThemeLayouts.Default),
			Density = MokaDefaults.ResolveString("LAYOUT_DENSITY", dto.Density, ThemeLayouts.Default),
			Font = MokaDefaults.ResolveString("LAYOUT_FONT", dto.Font, ThemeLayouts.Default),
			Headings = MokaDefaults.ResolveString("LAYOUT_HEADINGS", dto.Headings, ThemeLayouts.Default),
			Links = MokaDefaults.ResolveString("LAYOUT_LINKS", dto.Links, ThemeLayouts.Default)
		};
	}

	private static List<NavItem> MapNavItems(List<NavItemDto>? dtos)
	{
		if (dtos is null)
		{
			return [];
		}

		return dtos.Select(d => new NavItem
		{
			Label = d.Label ?? "",
			Path = d.Path,
			Icon = d.Icon,
			Order = d.Order,
			Expanded = d.Expanded,
			AutoGenerate = d.AutoGenerate,
			Children = MapNavItems(d.Children)
		}).ToList();
	}

	private static FeaturesConfig MapFeaturesConfig(FeaturesConfigDto? dto)
	{
		dto ??= new FeaturesConfigDto();
		SearchFeatureConfigDto s = dto.Search ?? new SearchFeatureConfigDto();

		return new FeaturesConfig
		{
			Search = new SearchFeatureConfig
			{
				Enabled = MokaDefaults.ResolveBool(
					"SEARCH_ENABLED", s.Enabled, MokaDefaults.SearchEnabled)
			},
			Versioning = dto.Versioning is { } v
				? new VersioningFeatureConfig
				{
					Enabled = v.Enabled ?? false,
					Strategy = v.Strategy ?? "directory",
					Versions = v.Versions?.Select(vd => new VersionDefinition
					{
						Label = vd.Label ?? "",
						Branch = vd.Branch,
						Tag = vd.Tag,
						Default = vd.Default,
						Prerelease = vd.Prerelease
					}).ToList() ?? []
				}
				: new VersioningFeatureConfig(),
			Blog = dto.Blog is { } b
				? new BlogFeatureConfig
				{
					Enabled = b.Enabled ?? false,
					PostsPerPage = b.PostsPerPage ?? BlogFeatureConfig.DefaultPostsPerPage,
					ShowAuthors = b.ShowAuthors ?? true,
					Path = NormalizeSeparators(b.Path) ?? BlogFeatureConfig.DefaultPath,
					Route = b.Route,
					Title = string.IsNullOrWhiteSpace(b.Title) ? "Blog" : b.Title.Trim(),
					Description = b.Description?.Trim() ?? "",
					SidebarPosts = b.SidebarPosts ?? BlogFeatureConfig.DefaultSidebarPosts,
					Feeds = b.Feed is null ? ["rss"] : MapBlogFeeds(b.Feed),
					Authors = MapBlogAuthors(b.Authors)
				}
				: new BlogFeatureConfig()
		};
	}

	// "rss", "rss, atom" or a YAML list. "none" or false turns the feeds off. Unknown names are kept
	// so the blog phase can name them in a warning.
	private static List<string> MapBlogFeeds(object feed)
	{
		IEnumerable<string> names = feed is IEnumerable<object> list
			? list.Select(item => item?.ToString() ?? "")
			: (feed.ToString() ?? "").Split(',');

		return names
			.Select(name => name.Trim().ToLowerInvariant())
			.Where(name => name.Length > 0 && name is not ("none" or "false" or "off" or "no"))
			.Distinct()
			.ToList();
	}

	private static Dictionary<string, BlogAuthor> MapBlogAuthors(Dictionary<string, BlogAuthorDto>? authors)
	{
		var mapped = new Dictionary<string, BlogAuthor>(StringComparer.OrdinalIgnoreCase);
		foreach ((string key, BlogAuthorDto? author) in authors ?? [])
		{
			mapped[key] = new BlogAuthor
			{
				Name = string.IsNullOrWhiteSpace(author?.Name) ? key : author.Name.Trim(),
				Url = string.IsNullOrWhiteSpace(author?.Url) ? null : author.Url.Trim(),
				Avatar = string.IsNullOrWhiteSpace(author?.Avatar) ? null : author.Avatar.Trim()
			};
		}

		return mapped;
	}

	private static List<PluginDeclaration> MapPlugins(List<PluginDeclarationDto>? dtos)
	{
		if (dtos is null)
		{
			return [];
		}

		return dtos.Select(d => new PluginDeclaration
		{
			Name = d.Name,
			Path = d.Path,
			Options = d.Options ?? []
		}).ToList();
	}

	private static CloudConfig MapCloudConfig(CloudConfigDto? dto)
	{
		dto ??= new CloudConfigDto();
		CloudFeaturesDto f = dto.Features ?? new CloudFeaturesDto();

		return new CloudConfig
		{
			Enabled = MokaDefaults.ResolveBool(
				"ENABLE_CLOUD_FEATURES", dto.Enabled, MokaDefaults.EnableCloudFeatures),
			ApiKey = dto.ApiKey,
			Features = new CloudFeatures
				{
					AiSummaries = MokaDefaults.ResolveBool(
						"ENABLE_AI_SEARCH", f.AiSummaries, MokaDefaults.EnableAiSearch),
					PdfExport = MokaDefaults.ResolveBool(
						"ENABLE_PDF_EXPORT", f.PdfExport, MokaDefaults.EnablePdfExport),
					Analytics = MokaDefaults.ResolveBool(
						"ENABLE_ANALYTICS", f.Analytics, MokaDefaults.EnableAnalytics),
					CustomDomain = MokaDefaults.ResolveBool(
						"ENABLE_CUSTOM_DOMAIN", f.CustomDomain, MokaDefaults.EnableCustomDomain)
				}
		};
	}

	private static BuildConfig MapBuildConfig(BuildConfigDto? dto)
	{
		dto ??= new BuildConfigDto();

		return new BuildConfig
		{
			Output = NormalizeSeparators(dto.Output) ?? "./_site",
			Clean = MokaDefaults.ResolveBool(
				"CLEAN_OUTPUT", dto.Clean, MokaDefaults.CleanOutput),
			Minify = MokaDefaults.ResolveBool(
				"MINIFY_OUTPUT", dto.Minify, MokaDefaults.MinifyOutput),
			MinifyAssets = MokaDefaults.ResolveOptionalBool("MINIFY_ASSETS", dto.MinifyAssets),
			Sitemap = MokaDefaults.ResolveBool(
				"GENERATE_SITEMAP", dto.Sitemap, MokaDefaults.GenerateSitemap),
			Robots = MokaDefaults.ResolveBool(
				"GENERATE_ROBOTS", dto.Robots, MokaDefaults.GenerateRobots),
			Llms = MokaDefaults.ResolveBool("LLMS", dto.Llms, MokaDefaults.GenerateLlmsFiles),
			Cache = dto.Cache ?? true,
			Strict = dto.Strict ?? false,
			BasePath = NormalizeBasePath(dto.BasePath)
		};
	}

	/// <summary>
	///     Normalizes a build base path to a leading slash with no trailing slash
	///     ("/" stays "/"). Public because the CLI applies --base-path after the config
	///     is read and has to produce exactly the same shape; a trailing slash here
	///     yields "//" in rendered links.
	/// </summary>
	/// <param name="basePath">Raw base path, or null.</param>
	/// <returns>The normalized base path.</returns>
	public static string NormalizeBasePath(string? basePath)
	{
		if (string.IsNullOrWhiteSpace(basePath))
		{
			return "/";
		}

		string p = basePath.Trim();
		if (!p.StartsWith('/'))
		{
			p = "/" + p;
		}

		if (p.EndsWith('/') && p.Length > 1)
		{
			p = p.TrimEnd('/');
		}

		return p;
	}

	// Windows accepts "/" too, so the same mokadocs.yaml works on both. On Linux ".\_site" used to
	// name a folder literally called ".\_site".
	private static string? NormalizeSeparators(string? path) => path?.Replace('\\', '/');

	private static SiteConfigDto MapToDto(SiteConfig config)
	{
		return new SiteConfigDto
		{
			Site = new SiteMetadataDto
			{
				Title = config.Site.Title,
				Description = NullIfEmpty(config.Site.Description),
				Url = NullIfEmpty(config.Site.Url),
				// Round-trip: emit the raw yaml value the user originally wrote, so a
				// Read → ToYaml → Read cycle preserves the exact on-disk form.
				Logo = config.Site.Logo?.RawValue,
				Favicon = config.Site.Favicon?.RawValue,
				Image = config.Site.Image?.RawValue,
				Copyright = config.Site.Copyright,
				EditLink = config.Site.EditLink is { } el
					? new EditLinkConfigDto { Repo = el.Repo, Branch = el.Branch, Path = el.Path }
					: null,
				Language = config.Site.Language == SiteLanguages.Default ? null : NullIfEmpty(config.Site.Language),
				Direction = NullIfEmpty(config.Site.Direction)
			},
			Content = new ContentConfigDto
			{
				Docs = config.Content.Docs,
				Projects = config.Content.Projects.Count > 0
					? config.Content.Projects.Select(p => new ProjectSourceDto
					{
						Path = p.Path,
						Label = p.Label,
						IncludeInternals = p.IncludeInternals
					}).ToList()
					: null
			},
			Theme = new ThemeConfigDto
			{
				Name = config.Theme.Name,
				Options = new ThemeOptionsDto
				{
					PrimaryColor = config.Theme.Options.PrimaryColor,
					AccentColor = config.Theme.Options.AccentColor,
					CodeTheme = config.Theme.Options.CodeTheme,
					ShowEditLink = config.Theme.Options.ShowEditLink,
					ShowLastUpdated = config.Theme.Options.ShowLastUpdated,
					ShowContributors = config.Theme.Options.ShowContributors ? true : null,
					ColorThemes = config.Theme.Options.ColorThemes ? null : false,
					CodeThemeSelector = config.Theme.Options.CodeThemeSelector ? null : false,
					CodeStyle = config.Theme.Options.CodeStyle == "plain" ? null : config.Theme.Options.CodeStyle,
					CodeStyleSelector = config.Theme.Options.CodeStyleSelector ? null : false,
					Preset = NullIfEmpty(config.Theme.Options.Preset),
					Style = config.Theme.Options.Style == ThemeStyles.Default ? null : config.Theme.Options.Style,
					StyleSelector = config.Theme.Options.StyleSelector ? null : false,
					TextSizeSelector = config.Theme.Options.TextSizeSelector ? null : false,
					WidthSelector = config.Theme.Options.WidthSelector ? null : false,
					LayoutSelector = config.Theme.Options.LayoutSelector ? true : null,
					Layout = MapLayoutToDto(config.Theme.Options.Layout),
					Sidebar = config.Theme.Options.Sidebar == SidebarModes.Auto ? null : config.Theme.Options.Sidebar,
					SidebarInlineLimit = config.Theme.Options.SidebarInlineLimit == MokaDefaults.SidebarInlineLimit
						? null
						: config.Theme.Options.SidebarInlineLimit,
					SidebarToggle = config.Theme.Options.SidebarToggle ? true : null,
					ShowFeedback = config.Theme.Options.ShowFeedback ? null : false,
					ShowDarkModeToggle = config.Theme.Options.ShowDarkModeToggle ? null : false,
					ShowAnimations = config.Theme.Options.ShowAnimations ? null : false,
					ShowSearch = config.Theme.Options.ShowSearch ? null : false,
					ShowTableOfContents = config.Theme.Options.ShowTableOfContents ? null : false,
					ShowPrevNext = config.Theme.Options.ShowPrevNext ? null : false,
					ShowBreadcrumbs = config.Theme.Options.ShowBreadcrumbs ? null : false,
					ShowBackToTop = config.Theme.Options.ShowBackToTop ? null : false,
					ShowCopyButton = config.Theme.Options.ShowCopyButton ? null : false,
					ShowLineNumbers = config.Theme.Options.ShowLineNumbers ? null : false,
					ImageZoom = config.Theme.Options.ImageZoom ? null : false,
					TocDepth = config.Theme.Options.TocDepth != 3 ? config.Theme.Options.TocDepth : null,
					ShowVersionSelector = config.Theme.Options.ShowVersionSelector ? null : false,
					ShowBuiltWith = config.Theme.Options.ShowBuiltWith ? null : false,
					SocialLinks = config.Theme.Options.SocialLinks.Count > 0
						? config.Theme.Options.SocialLinks.Select(s => new SocialLinkDto
						{
							Icon = s.Icon,
							Url = s.Url
						}).ToList()
						: null,
					DefaultColorTheme = config.Theme.Options.DefaultColorTheme != "ocean"
						? config.Theme.Options.DefaultColorTheme
						: null,
					Banner = MapBannerToDto(config.Theme.Options.Banner),
					Strings = config.Theme.Options.Strings.Count > 0
						? config.Theme.Options.Strings.ToDictionary(entry => entry.Key, string? (entry) => entry.Value, StringComparer.Ordinal)
						: null
				}
			},
			Build = new BuildConfigDto
			{
				Output = config.Build.Output,
				Clean = config.Build.Clean,
				Minify = config.Build.Minify ? true : null,
				// An explicit true is written too: unset and true differ under mokadocs serve.
				MinifyAssets = config.Build.MinifyAssets,
				Sitemap = config.Build.Sitemap,
				Robots = config.Build.Robots,
				Llms = config.Build.Llms ? null : false,
				Cache = config.Build.Cache,
				Strict = config.Build.Strict ? true : null,
				BasePath = config.Build.BasePath == "/" ? null : config.Build.BasePath
			},
			Glossary = config.Glossary.Count > 0
				? config.Glossary.ToDictionary(entry => entry.Key, string? (entry) => entry.Value, StringComparer.Ordinal)
				: null
		};
	}

	private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

	// Null rather than an empty DTO when every part is the default: the serializer writes an empty
	// object as "layout: {}".
	private static ThemeLayoutDto? MapLayoutToDto(ThemeLayoutOptions layout)
	{
		var dto = new ThemeLayoutDto
		{
			Width = NullIfDefaultLayout(layout.Width),
			Navbar = NullIfDefaultLayout(layout.Navbar),
			Footer = NullIfDefaultLayout(layout.Footer),
			Toc = NullIfDefaultLayout(layout.Toc),
			Breadcrumbs = NullIfDefaultLayout(layout.Breadcrumbs),
			PageNav = NullIfDefaultLayout(layout.PageNav),
			Sidebar = NullIfDefaultLayout(layout.Sidebar),
			SidebarScope = NullIfDefaultLayout(layout.SidebarScope),
			Home = NullIfDefaultLayout(layout.Home),
			Hero = NullIfDefaultLayout(layout.Hero),
			HeroBackground = NullIfDefaultLayout(layout.HeroBackground),
			Features = NullIfDefaultLayout(layout.Features),
			Admonitions = NullIfDefaultLayout(layout.Admonitions),
			Tables = NullIfDefaultLayout(layout.Tables),
			Density = NullIfDefaultLayout(layout.Density),
			Font = NullIfDefaultLayout(layout.Font),
			Headings = NullIfDefaultLayout(layout.Headings),
			Links = NullIfDefaultLayout(layout.Links)
		};

		return dto is
		{
			Width: null, Navbar: null, Footer: null, Toc: null, Breadcrumbs: null, PageNav: null, Sidebar: null,
			SidebarScope: null, Home: null, Hero: null, HeroBackground: null, Features: null, Admonitions: null,
			Tables: null, Density: null, Font: null, Headings: null, Links: null
		}
			? null
			: dto;
	}

	private static ThemeBannerDto? MapBannerToDto(ThemeBanner? banner) =>
		banner is null
			? null
			: new ThemeBannerDto
			{
				Text = banner.Text,
				Link = banner.Link,
				LinkText = banner.LinkText,
				Id = banner.Id,
				Dismissible = banner.Dismissible ? null : false,
				Variant = banner.Variant == BannerVariants.Info ? null : banner.Variant
			};

	private static string? NullIfDefaultLayout(string? name) =>
		string.IsNullOrWhiteSpace(name) || name.Trim().Equals(ThemeLayouts.Default, StringComparison.OrdinalIgnoreCase)
			? null
			: name;

	/// <summary>
	///     Resolves a raw yaml brand-asset string (logo or favicon) into a
	///     <see cref="SiteAssetReference" />. See that type's documentation for full
	///     resolution rules. Returns <c>null</c> when the input is null or empty so the
	///     calling code can cleanly skip rendering.
	/// </summary>
	/// <param name="raw">Raw yaml value from the user's mokadocs.yaml.</param>
	/// <param name="yamlDir">
	///     Absolute directory of the source mokadocs.yaml file. When null (e.g. parsing a
	///     standalone yaml string not backed by a file), filesystem resolution is skipped
	///     and <see cref="SiteAssetReference.SourcePath" /> is left null - the build
	///     pipeline will then log a warning and omit the asset from copy.
	/// </param>
	private static SiteAssetReference? ParseAssetReference(string? raw, string? yamlDir)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}

		string value = raw.Trim();

		// Absolute URL pass-through. Covers http(s), protocol-relative, data URIs, and
		// anything else that looks like a URL rather than a filesystem path. The template
		// engine emits these verbatim with no base-path prefix and no asset copy.
		if (IsAbsoluteUrl(value))
		{
			return new SiteAssetReference
			{
				RawValue = raw,
				SourcePath = null,
				PublishUrl = value,
				IsAbsoluteUrl = true
			};
		}

		// Without a yaml directory we can't resolve the filesystem path. Produce a
		// "best effort" reference that carries the raw value so the template still emits
		// something, but with no SourcePath so CopyAssets skips it. The pipeline logs a
		// diagnostic in that path so misconfigurations are visible.
		if (string.IsNullOrEmpty(yamlDir))
		{
			return new SiteAssetReference
			{
				RawValue = raw,
				SourcePath = null,
				PublishUrl = "/" + NormalizeRelativeSegments(value.TrimStart('/')),
				IsAbsoluteUrl = false
			};
		}

		// Normalize to forward slashes so publish URLs are consistent across platforms.
		// Leading slash is interpreted as "relative to yaml dir" not filesystem root,
		// matching user intent for forms like "/assets/logo.png".
		string trimmed = value.Replace('\\', '/').TrimStart('/');
		string normalizedYamlDir = Path.GetFullPath(yamlDir);
		string sourcePath = Path.GetFullPath(Path.Combine(normalizedYamlDir, trimmed));

		// Determine whether the resolved source path escapes the yaml directory via `..`.
		string sourceRelativeToYamlDir = Path.GetRelativePath(normalizedYamlDir, sourcePath);
		bool escapesYamlDir = sourceRelativeToYamlDir.StartsWith("..", StringComparison.Ordinal)
		                      || Path.IsPathRooted(sourceRelativeToYamlDir);

		string publishUrl;
		if (escapesYamlDir)
		{
			// Flatten out-of-tree assets to /_media/{filename}. The `_media` prefix starts
			// with an underscore to match the rest of mokadocs' framework-prefix convention;
			// a .nojekyll marker in _site/ (emitted by the blazor-preview plugin, or by any
			// future general emitter) is required to keep these from being stripped by
			// GitHub Pages' Jekyll processor.
			publishUrl = "/_media/" + Path.GetFileName(sourcePath);
		}
		else
		{
			publishUrl = "/" + sourceRelativeToYamlDir.Replace(Path.DirectorySeparatorChar, '/');
		}

		return new SiteAssetReference
		{
			RawValue = raw,
			SourcePath = sourcePath,
			PublishUrl = publishUrl,
			IsAbsoluteUrl = false
		};
	}

	/// <summary>
	///     Strips leading <c>./</c> segments and collapses <c>/./</c> mid-path so the
	///     no-yamlDir fallback produces clean publish URLs like <c>/assets/logo.svg</c>
	///     instead of <c>/./assets/logo.svg</c>. Used only by the null-yamlDir path;
	///     the normal resolve-against-filesystem path uses <c>Path.GetFullPath</c> which
	///     handles this natively.
	/// </summary>
	private static string NormalizeRelativeSegments(string value)
	{
		string result = value;
		while (result.StartsWith("./", StringComparison.Ordinal))
		{
			result = result[2..];
		}

		return result.Replace("/./", "/", StringComparison.Ordinal);
	}

	private static bool IsAbsoluteUrl(string value)
	{
		// Protocol-prefixed: http://, https://, ws://, data:, file:, mailto: etc.
		if (value.Contains("://", StringComparison.Ordinal))
		{
			return true;
		}

		// Protocol-relative: //cdn.example.com/logo.png
		if (value.StartsWith("//", StringComparison.Ordinal))
		{
			return true;
		}

		// data: and other schemeless URIs that use a colon
		int colonIdx = value.IndexOf(':', StringComparison.Ordinal);
		if (colonIdx > 0)
		{
			string scheme = value[..colonIdx];
			// Must look like a scheme (letters / digits / +-.), not a Windows drive letter
			if (scheme.Length >= 2
			    && scheme.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.')
			    // Windows drive letters are always 1 char ("C:"), so scheme >= 2 rules them out
			    && !scheme.StartsWith('/'))
			{
				return true;
			}
		}

		return false;
	}

	#endregion
}

/// <summary>
///     Exception thrown when the site configuration is invalid.
/// </summary>
public sealed class SiteConfigException : Exception
{
	/// <inheritdoc />
	public SiteConfigException(string message) : base(message)
	{
	}

	/// <inheritdoc />
	public SiteConfigException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
