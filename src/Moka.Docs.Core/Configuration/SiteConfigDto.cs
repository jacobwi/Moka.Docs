namespace Moka.Docs.Core.Configuration;

/// <summary>DTO for top-level mokadocs.yaml.</summary>
internal sealed class SiteConfigDto
{
    public SiteMetadataDto? Site { get; set; }
    public ContentConfigDto? Content { get; set; }
    public ThemeConfigDto? Theme { get; set; }
    public List<NavItemDto>? Nav { get; set; }
    public FeaturesConfigDto? Features { get; set; }
    public List<PluginDeclarationDto>? Plugins { get; set; }
    public CloudConfigDto? Cloud { get; set; }
    public BuildConfigDto? Build { get; set; }
}

/// <summary>DTO for site metadata.</summary>
internal sealed class SiteMetadataDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? Logo { get; set; }
    public string? Favicon { get; set; }
    public string? Copyright { get; set; }
    public EditLinkConfigDto? EditLink { get; set; }
}

/// <summary>DTO for edit link config.</summary>
internal sealed class EditLinkConfigDto
{
    public string? Repo { get; set; }
    public string? Branch { get; set; }
    public string? Path { get; set; }
}

/// <summary>DTO for content config.</summary>
internal sealed class ContentConfigDto
{
    public string? Docs { get; set; }
    public List<ProjectSourceDto>? Projects { get; set; }
}

/// <summary>DTO for a project source.</summary>
internal sealed class ProjectSourceDto
{
    public string? Path { get; set; }
    public string? Label { get; set; }
    public bool IncludeInternals { get; set; }
}

/// <summary>DTO for theme config.</summary>
internal sealed class ThemeConfigDto
{
    public string? Name { get; set; }
    public ThemeOptionsDto? Options { get; set; }
}

/// <summary>DTO for theme options.</summary>
internal sealed class ThemeOptionsDto
{
    public string? PrimaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? CodeTheme { get; set; }
    public bool? ShowEditLink { get; set; }
    public bool? ShowLastUpdated { get; set; }
    public bool? ShowContributors { get; set; }
    public bool? ColorThemes { get; set; }
    public bool? CodeThemeSelector { get; set; }
    public string? CodeStyle { get; set; }
    public bool? CodeStyleSelector { get; set; }
    public bool? ShowFeedback { get; set; }
    public bool? ShowDarkModeToggle { get; set; }
    public bool? ShowAnimations { get; set; }
    public bool? ShowSearch { get; set; }
    public bool? ShowTableOfContents { get; set; }
    public bool? ShowPrevNext { get; set; }
    public bool? ShowBreadcrumbs { get; set; }
    public bool? ShowBackToTop { get; set; }
    public bool? ShowCopyButton { get; set; }
    public bool? ShowLineNumbers { get; set; }
    public int? TocDepth { get; set; }
    public bool? ShowVersionSelector { get; set; }
    public List<SocialLinkDto>? SocialLinks { get; set; }
}

/// <summary>DTO for a social link.</summary>
internal sealed class SocialLinkDto
{
    public string? Icon { get; set; }
    public string? Url { get; set; }
}

/// <summary>DTO for a nav item.</summary>
internal sealed class NavItemDto
{
    public string? Label { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public bool Expanded { get; set; }
    public bool AutoGenerate { get; set; }
    public List<NavItemDto>? Children { get; set; }
}

/// <summary>DTO for features config.</summary>
internal sealed class FeaturesConfigDto
{
    public SearchFeatureConfigDto? Search { get; set; }
    public VersioningFeatureConfigDto? Versioning { get; set; }
    public BlogFeatureConfigDto? Blog { get; set; }
}

/// <summary>DTO for search feature config.</summary>
internal sealed class SearchFeatureConfigDto
{
    public bool? Enabled { get; set; }
    public string? Provider { get; set; }
}

/// <summary>DTO for versioning feature config.</summary>
internal sealed class VersioningFeatureConfigDto
{
    public bool? Enabled { get; set; }
    public string? Strategy { get; set; }
    public List<VersionDefinitionDto>? Versions { get; set; }
}

/// <summary>DTO for a version definition.</summary>
internal sealed class VersionDefinitionDto
{
    public string? Label { get; set; }
    public string? Branch { get; set; }
    public bool Default { get; set; }
    public bool Prerelease { get; set; }
}

/// <summary>DTO for blog feature config.</summary>
internal sealed class BlogFeatureConfigDto
{
    public bool? Enabled { get; set; }
    public int? PostsPerPage { get; set; }
    public bool? ShowAuthors { get; set; }
}

/// <summary>DTO for a plugin declaration.</summary>
internal sealed class PluginDeclarationDto
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>DTO for cloud config.</summary>
internal sealed class CloudConfigDto
{
    public bool? Enabled { get; set; }
    public string? ApiKey { get; set; }
    public CloudFeaturesDto? Features { get; set; }
}

/// <summary>DTO for cloud features.</summary>
internal sealed class CloudFeaturesDto
{
    public bool? AiSummaries { get; set; }
    public bool? PdfExport { get; set; }
    public bool? Analytics { get; set; }
    public bool? CustomDomain { get; set; }
}

/// <summary>DTO for build config.</summary>
internal sealed class BuildConfigDto
{
    public string? Output { get; set; }
    public bool? Clean { get; set; }
    public bool? Minify { get; set; }
    public bool? Sitemap { get; set; }
    public bool? Robots { get; set; }
    public bool? Cache { get; set; }
}