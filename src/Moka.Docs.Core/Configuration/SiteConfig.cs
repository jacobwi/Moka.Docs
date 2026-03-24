namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Top-level configuration for a MokaDocs site, parsed from mokadocs.yaml.
/// </summary>
public sealed record SiteConfig
{
    /// <summary>Site metadata (title, description, URL, etc.).</summary>
    public required SiteMetadata Site { get; init; }

    /// <summary>Content source configuration.</summary>
    public ContentConfig Content { get; init; } = new();

    /// <summary>Theme configuration.</summary>
    public ThemeConfig Theme { get; init; } = new();

    /// <summary>Navigation overrides.</summary>
    public List<NavItem> Nav { get; init; } = [];

    /// <summary>Feature toggles.</summary>
    public FeaturesConfig Features { get; init; } = new();

    /// <summary>Plugin declarations.</summary>
    public List<PluginDeclaration> Plugins { get; init; } = [];

    /// <summary>Cloud feature configuration.</summary>
    public CloudConfig Cloud { get; init; } = new();

    /// <summary>Build options.</summary>
    public BuildConfig Build { get; init; } = new();
}

/// <summary>
///     Site metadata: title, description, base URL, branding.
/// </summary>
public sealed record SiteMetadata
{
    /// <summary>The site title displayed in the header and browser tab.</summary>
    public required string Title { get; init; }

    /// <summary>A short description of the site for meta tags.</summary>
    public string Description { get; init; } = "";

    /// <summary>The base URL where the site will be hosted.</summary>
    public string Url { get; init; } = "";

    /// <summary>Path to the site logo.</summary>
    public string? Logo { get; init; }

    /// <summary>Path to the favicon.</summary>
    public string? Favicon { get; init; }

    /// <summary>Copyright notice for the footer.</summary>
    public string? Copyright { get; init; }

    /// <summary>Edit link configuration for "Edit this page" links.</summary>
    public EditLinkConfig? EditLink { get; init; }
}

/// <summary>
///     Configuration for "Edit this page" links pointing to a source repository.
/// </summary>
public sealed record EditLinkConfig
{
    /// <summary>Repository URL (e.g., "https://github.com/org/repo").</summary>
    public required string Repo { get; init; }

    /// <summary>Branch name (defaults to "main").</summary>
    public string Branch { get; init; } = "main";

    /// <summary>Path prefix within the repo where docs live.</summary>
    public string Path { get; init; } = "docs/";
}

/// <summary>
///     Content source configuration: where to find Markdown and C# projects.
/// </summary>
public sealed record ContentConfig
{
    /// <summary>Path to the Markdown documentation directory.</summary>
    public string Docs { get; init; } = "./docs";

    /// <summary>C# projects to extract API documentation from.</summary>
    public List<ProjectSource> Projects { get; init; } = [];
}

/// <summary>
///     A C# project source for API documentation extraction.
/// </summary>
public sealed record ProjectSource
{
    /// <summary>Path to the .csproj file.</summary>
    public required string Path { get; init; }

    /// <summary>Display label for this project in the docs.</summary>
    public string? Label { get; init; }

    /// <summary>Whether to include internal members in the documentation.</summary>
    public bool IncludeInternals { get; init; }
}

/// <summary>
///     Theme configuration.
/// </summary>
public sealed record ThemeConfig
{
    /// <summary>Theme name (built-in) or path to a custom theme directory.</summary>
    public string Name { get; init; } = MokaDefaults.ThemeName;

    /// <summary>Theme-specific options.</summary>
    public ThemeOptions Options { get; init; } = new();
}

/// <summary>
///     Theme-specific options for customizing appearance.
/// </summary>
public sealed record ThemeOptions
{
    /// <summary>Primary brand color.</summary>
    public string PrimaryColor { get; init; } = MokaDefaults.PrimaryColor;

    /// <summary>Accent color for highlights.</summary>
    public string AccentColor { get; init; } = "#f59e0b";

    /// <summary>Code syntax highlighting theme.</summary>
    public string CodeTheme { get; init; } = MokaDefaults.CodeTheme;

    /// <summary>Show "Edit this page" link on each page.</summary>
    public bool ShowEditLink { get; init; } = MokaDefaults.ShowEditLink;

    /// <summary>Show last-updated timestamp on pages.</summary>
    public bool ShowLastUpdated { get; init; } = MokaDefaults.ShowLastUpdated;

    /// <summary>Show contributor avatars on pages.</summary>
    public bool ShowContributors { get; init; }

    /// <summary>Show color theme preset selector in the header.</summary>
    public bool ColorThemes { get; init; } = MokaDefaults.ShowColorThemeSelector;

    /// <summary>Show code syntax theme selector in the header.</summary>
    public bool CodeThemeSelector { get; init; } = MokaDefaults.ShowCodeThemeSelector;

    /// <summary>Code block window style: "plain", "macos", "terminal", or "vscode".</summary>
    public string CodeStyle { get; init; } = MokaDefaults.CodeStyle;

    /// <summary>Show code block style selector in the header.</summary>
    public bool CodeStyleSelector { get; init; } = MokaDefaults.ShowCodeStyleSelector;

    /// <summary>Show the "Was this page helpful?" feedback widget.</summary>
    public bool ShowFeedback { get; init; } = MokaDefaults.ShowFeedbackWidget;

    /// <summary>Show the dark/light mode toggle.</summary>
    public bool ShowDarkModeToggle { get; init; } = MokaDefaults.ShowDarkModeToggle;

    /// <summary>Show page animations (fade-in, slide-up, etc).</summary>
    public bool ShowAnimations { get; init; } = MokaDefaults.ShowAnimations;

    /// <summary>Show the search bar and Ctrl+K shortcut.</summary>
    public bool ShowSearch { get; init; } = MokaDefaults.ShowSearch;

    /// <summary>Show the table of contents sidebar.</summary>
    public bool ShowTableOfContents { get; init; } = MokaDefaults.ShowTableOfContents;

    /// <summary>Show Previous/Next navigation at bottom of pages.</summary>
    public bool ShowPrevNext { get; init; } = MokaDefaults.ShowPrevNext;

    /// <summary>Show breadcrumb navigation.</summary>
    public bool ShowBreadcrumbs { get; init; } = MokaDefaults.ShowBreadcrumbs;

    /// <summary>Show the back-to-top button.</summary>
    public bool ShowBackToTop { get; init; } = MokaDefaults.ShowBackToTop;

    /// <summary>Show copy button on code blocks.</summary>
    public bool ShowCopyButton { get; init; } = MokaDefaults.ShowCopyButton;

    /// <summary>Show line numbers on code blocks.</summary>
    public bool ShowLineNumbers { get; init; } = MokaDefaults.ShowLineNumbers;

    /// <summary>Maximum heading level shown in the table of contents (2–6). Default is 3 (h2 + h3).</summary>
    public int TocDepth { get; init; } = MokaDefaults.TocDepth;

    /// <summary>Show the version selector dropdown.</summary>
    public bool ShowVersionSelector { get; init; } = MokaDefaults.ShowVersionSelector;

    /// <summary>Social links displayed in the site header.</summary>
    public List<SocialLink> SocialLinks { get; init; } = [];
}

/// <summary>
///     A social link displayed in the site header.
/// </summary>
public sealed record SocialLink
{
    /// <summary>Icon name (e.g., "github", "discord", "nuget").</summary>
    public required string Icon { get; init; }

    /// <summary>URL for the link.</summary>
    public required string Url { get; init; }
}

/// <summary>
///     Navigation sidebar item (can be auto-generated or manual override).
/// </summary>
public sealed record NavItem
{
    /// <summary>Display label.</summary>
    public required string Label { get; init; }

    /// <summary>URL path for this nav item.</summary>
    public string? Path { get; init; }

    /// <summary>Icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Whether this section is expanded by default.</summary>
    public bool Expanded { get; init; }

    /// <summary>Auto-generate children from C# projects.</summary>
    public bool AutoGenerate { get; init; }

    /// <summary>Child navigation items.</summary>
    public List<NavItem> Children { get; init; } = [];
}

/// <summary>
///     Feature toggles for optional capabilities.
/// </summary>
public sealed record FeaturesConfig
{
    /// <summary>Search configuration.</summary>
    public SearchFeatureConfig Search { get; init; } = new();

    /// <summary>Versioning configuration.</summary>
    public VersioningFeatureConfig Versioning { get; init; } = new();

    /// <summary>Blog feature configuration.</summary>
    public BlogFeatureConfig Blog { get; init; } = new();
}

/// <summary>Search feature configuration.</summary>
public sealed record SearchFeatureConfig
{
    /// <summary>Whether search is enabled.</summary>
    public bool Enabled { get; init; } = MokaDefaults.SearchEnabled;

    /// <summary>Search provider: "pagefind", "flexsearch", or "custom".</summary>
    public string Provider { get; init; } = MokaDefaults.SearchProvider;
}

/// <summary>Versioning feature configuration.</summary>
public sealed record VersioningFeatureConfig
{
    /// <summary>Whether versioning is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Versioning strategy: "directory" or "dropdown-only".</summary>
    public string Strategy { get; init; } = "directory";

    /// <summary>Version definitions.</summary>
    public List<VersionDefinition> Versions { get; init; } = [];
}

/// <summary>A version definition for multi-version documentation.</summary>
public sealed record VersionDefinition
{
    /// <summary>Display label (e.g., "v2.0").</summary>
    public required string Label { get; init; }

    /// <summary>Git branch for this version.</summary>
    public string? Branch { get; init; }

    /// <summary>Whether this is the default version.</summary>
    public bool Default { get; init; }

    /// <summary>Whether this is a prerelease version.</summary>
    public bool Prerelease { get; init; }
}

/// <summary>Blog feature configuration.</summary>
public sealed record BlogFeatureConfig
{
    /// <summary>Whether the blog is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Number of posts per page.</summary>
    public int PostsPerPage { get; init; } = 10;

    /// <summary>Show author information on posts.</summary>
    public bool ShowAuthors { get; init; } = true;
}

/// <summary>Plugin declaration in configuration.</summary>
public sealed record PluginDeclaration
{
    /// <summary>NuGet package name or plugin identifier.</summary>
    public string? Name { get; init; }

    /// <summary>Path to a local plugin DLL.</summary>
    public string? Path { get; init; }

    /// <summary>Plugin-specific options.</summary>
    public Dictionary<string, object> Options { get; init; } = [];
}

/// <summary>
///     Cloud feature configuration (all OFF by default).
/// </summary>
public sealed record CloudConfig
{
    /// <summary>Master switch — nothing calls home unless true.</summary>
    public bool Enabled { get; init; } = MokaDefaults.EnableCloudFeatures;

    /// <summary>API key for cloud features.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Individual cloud feature toggles.</summary>
    public CloudFeatures Features { get; init; } = new();
}

/// <summary>Individual cloud feature toggles.</summary>
public sealed record CloudFeatures
{
    /// <summary>AI-generated summaries for API types.</summary>
    public bool AiSummaries { get; init; } = MokaDefaults.EnableAISearch;

    /// <summary>Server-side PDF generation.</summary>
    public bool PdfExport { get; init; } = MokaDefaults.EnablePdfExport;

    /// <summary>Usage analytics dashboard.</summary>
    public bool Analytics { get; init; } = MokaDefaults.EnableAnalytics;

    /// <summary>Custom domain with SSL.</summary>
    public bool CustomDomain { get; init; } = MokaDefaults.EnableCustomDomain;
}

/// <summary>Build options.</summary>
public sealed record BuildConfig
{
    /// <summary>Output directory path.</summary>
    public string Output { get; init; } = "./_site";

    /// <summary>Clean output directory before build.</summary>
    public bool Clean { get; init; } = MokaDefaults.CleanOutput;

    /// <summary>Minify HTML/CSS/JS in output.</summary>
    public bool Minify { get; init; } = true;

    /// <summary>Generate sitemap.xml.</summary>
    public bool Sitemap { get; init; } = MokaDefaults.GenerateSitemap;

    /// <summary>Generate robots.txt.</summary>
    public bool Robots { get; init; } = MokaDefaults.GenerateRobots;

    /// <summary>Cache intermediate results for faster rebuilds.</summary>
    public bool Cache { get; init; } = true;
}