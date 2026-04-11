using System.IO.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Reads and parses a mokadocs.yaml configuration file into a <see cref="SiteConfig" />.
/// </summary>
public sealed class SiteConfigReader
{
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
	///     will be null, meaning the build pipeline won't copy them — that's the correct behavior
	///     for virtual / in-memory configs.
	/// </param>
	/// <returns>The parsed <see cref="SiteConfig" />.</returns>
	/// <exception cref="SiteConfigException">Thrown when the YAML is invalid or missing required fields.</exception>
	public static SiteConfig Parse(string yaml, string? yamlDir = null)
	{
		try
		{
			IDeserializer deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.IgnoreUnmatchedProperties()
				.Build();

			SiteConfigDto dto = deserializer.Deserialize<SiteConfigDto>(yaml)
			                    ?? throw new SiteConfigException("Configuration file is empty.");

			return MapFromDto(dto, yamlDir);
		}
		catch (SiteConfigException)
		{
			throw;
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

		// Collision check: if both assets flatten to the SAME publish URL (same filename
		// flattened from different escape paths), the second would silently overwrite the
		// first. Fail loud so the user can rename the source file or move it.
		if (logo is { ShouldCopy: true } && favicon is { ShouldCopy: true }
		                                 && logo.PublishUrl == favicon.PublishUrl
		                                 && !string.Equals(logo.SourcePath, favicon.SourcePath,
			                                 StringComparison.OrdinalIgnoreCase))
		{
			throw new SiteConfigException(
				$"'site.logo' ('{siteDto.Logo}') and 'site.favicon' ('{siteDto.Favicon}') resolve to " +
				$"the same publish URL '{logo.PublishUrl}' from different source files. " +
				"Rename one of them or move them to distinct filenames so they don't collide in the output directory.");
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
				Copyright = siteDto.Copyright,
				EditLink = siteDto.EditLink is { } el
					? new EditLinkConfig
					{
						Repo = el.Repo ?? "",
						Branch = el.Branch ?? "main",
						Path = el.Path ?? "docs/"
					}
					: null
			},
			Content = MapContentConfig(dto.Content),
			Theme = MapThemeConfig(dto.Theme),
			Nav = MapNavItems(dto.Nav),
			Features = MapFeaturesConfig(dto.Features),
			Plugins = MapPlugins(dto.Plugins),
			Cloud = MapCloudConfig(dto.Cloud),
			Build = MapBuildConfig(dto.Build)
		};
	}

	private static ContentConfig MapContentConfig(ContentConfigDto? dto)
	{
		if (dto is null)
		{
			return new ContentConfig();
		}

		return new ContentConfig
		{
			Docs = dto.Docs ?? "./docs",
			Projects = dto.Projects?.Select(p => new ProjectSource
			{
				Path = p.Path ?? "",
				Label = p.Label,
				IncludeInternals = p.IncludeInternals
			}).ToList() ?? []
		};
	}

	private static ThemeConfig MapThemeConfig(ThemeConfigDto? dto)
	{
		if (dto is null)
		{
			return new ThemeConfig();
		}

		return new ThemeConfig
		{
			Name = MokaDefaults.ResolveString("THEME_NAME", dto.Name, MokaDefaults.ThemeName),
			Options = dto.Options is { } o
				? new ThemeOptions
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
					ShowContributors = o.ShowContributors ?? false,
					ColorThemes = MokaDefaults.ResolveBool(
						"SHOW_COLOR_THEME_SELECTOR", o.ColorThemes, MokaDefaults.ShowColorThemeSelector),
					CodeThemeSelector = MokaDefaults.ResolveBool(
						"SHOW_CODE_THEME_SELECTOR", o.CodeThemeSelector, MokaDefaults.ShowCodeThemeSelector),
					CodeStyle = MokaDefaults.ResolveString(
						"CODE_STYLE", o.CodeStyle, MokaDefaults.CodeStyle),
					CodeStyleSelector = MokaDefaults.ResolveBool(
						"SHOW_CODE_STYLE_SELECTOR", o.CodeStyleSelector, MokaDefaults.ShowCodeStyleSelector),
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
					DefaultColorTheme = o.DefaultColorTheme ?? "ocean"
				}
				: new ThemeOptions()
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
		if (dto is null)
		{
			return new FeaturesConfig();
		}

		return new FeaturesConfig
		{
			Search = dto.Search is { } s
				? new SearchFeatureConfig
				{
					Enabled = MokaDefaults.ResolveBool(
						"SEARCH_ENABLED", s.Enabled, MokaDefaults.SearchEnabled),
					Provider = MokaDefaults.ResolveString(
						"SEARCH_PROVIDER", s.Provider, MokaDefaults.SearchProvider)
				}
				: new SearchFeatureConfig(),
			Versioning = dto.Versioning is { } v
				? new VersioningFeatureConfig
				{
					Enabled = v.Enabled ?? false,
					Strategy = v.Strategy ?? "directory",
					Versions = v.Versions?.Select(vd => new VersionDefinition
					{
						Label = vd.Label ?? "",
						Branch = vd.Branch,
						Default = vd.Default,
						Prerelease = vd.Prerelease
					}).ToList() ?? []
				}
				: new VersioningFeatureConfig(),
			Blog = dto.Blog is { } b
				? new BlogFeatureConfig
				{
					Enabled = b.Enabled ?? false,
					PostsPerPage = b.PostsPerPage ?? 10,
					ShowAuthors = b.ShowAuthors ?? true
				}
				: new BlogFeatureConfig()
		};
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
		if (dto is null)
		{
			return new CloudConfig();
		}

		return new CloudConfig
		{
			Enabled = MokaDefaults.ResolveBool(
				"ENABLE_CLOUD_FEATURES", dto.Enabled, MokaDefaults.EnableCloudFeatures),
			ApiKey = dto.ApiKey,
			Features = dto.Features is { } f
				? new CloudFeatures
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
				: new CloudFeatures()
		};
	}

	private static BuildConfig MapBuildConfig(BuildConfigDto? dto)
	{
		if (dto is null)
		{
			return new BuildConfig();
		}

		return new BuildConfig
		{
			Output = dto.Output ?? "./_site",
			Clean = MokaDefaults.ResolveBool(
				"CLEAN_OUTPUT", dto.Clean, MokaDefaults.CleanOutput),
			Minify = dto.Minify ?? true,
			Sitemap = MokaDefaults.ResolveBool(
				"GENERATE_SITEMAP", dto.Sitemap, MokaDefaults.GenerateSitemap),
			Robots = MokaDefaults.ResolveBool(
				"GENERATE_ROBOTS", dto.Robots, MokaDefaults.GenerateRobots),
			Cache = dto.Cache ?? true,
			BasePath = NormalizBasePath(dto.BasePath)
		};
	}

	private static string NormalizBasePath(string? basePath)
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
				Copyright = config.Site.Copyright,
				EditLink = config.Site.EditLink is { } el
					? new EditLinkConfigDto { Repo = el.Repo, Branch = el.Branch, Path = el.Path }
					: null
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
						: null
				}
			},
			Build = new BuildConfigDto
			{
				Output = config.Build.Output,
				Clean = config.Build.Clean,
				Minify = config.Build.Minify,
				Sitemap = config.Build.Sitemap,
				Robots = config.Build.Robots,
				Cache = config.Build.Cache,
				BasePath = config.Build.BasePath == "/" ? null : config.Build.BasePath
			}
		};
	}

	private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

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
	///     and <see cref="SiteAssetReference.SourcePath" /> is left null — the build
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
		string trimmed = value.TrimStart('/');
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
