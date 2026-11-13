using System.Security.Cryptography;
using System.Text;
using Moka.Docs.Core.Theming;

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

	/// <summary>
	///     Glossary terms and their definitions (<c>glossary:</c> in mokadocs.yaml), shown by <c>:term[...]</c>
	///     tooltips and listed by <c>:::glossary</c>. A glossary.yml file in the docs folder adds more
	///     (<see cref="GlossaryFile" />). Terms match case-insensitively.
	/// </summary>
	public IReadOnlyDictionary<string, string> Glossary { get; init; } = new Dictionary<string, string>();

	/// <summary>
	///     Problems found while reading mokadocs.yaml that did not stop it from loading, such as
	///     unknown keys, each naming its line. The build reports them as warnings.
	/// </summary>
	public IReadOnlyList<string> Warnings { get; init; } = [];
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

	/// <summary>
	///     Resolved reference to the site logo. See <see cref="SiteAssetReference" /> for
	///     the supported yaml forms (relative to mokadocs.yaml dir, parent-dir escape with
	///     <c>../</c>, absolute URL pass-through).
	/// </summary>
	public SiteAssetReference? Logo { get; init; }

	/// <summary>
	///     Resolved reference to the site favicon. Same resolution rules as <see cref="Logo" />.
	/// </summary>
	public SiteAssetReference? Favicon { get; init; }

	/// <summary>
	///     Resolved reference to the image link previews show for a page that sets none of its own
	///     (<c>og:image</c> and <c>twitter:image</c>). Same resolution rules as <see cref="Logo" />.
	///     The <c>MOKADOCS_SITE_IMAGE</c> environment variable overrides it.
	/// </summary>
	public SiteAssetReference? Image { get; init; }

	/// <summary>Copyright notice for the footer.</summary>
	public string? Copyright { get; init; }

	/// <summary>Edit link configuration for "Edit this page" links.</summary>
	public EditLinkConfig? EditLink { get; init; }

	/// <summary>
	///     The language of the site's pages as a BCP 47 tag, such as <c>en</c>, <c>de</c> or <c>pt-BR</c>:
	///     <c>&lt;html lang&gt;</c>, the theme's built-in translation, plural forms and how dates are written.
	///     Defaults to <see cref="SiteLanguages.Default" />; the <c>MOKADOCS_SITE_LANGUAGE</c> environment
	///     variable overrides it.
	/// </summary>
	public string Language { get; init; } = SiteLanguages.Default;

	/// <summary>
	///     The text direction of the site's pages, <c>ltr</c> or <c>rtl</c>, or empty for the language's own
	///     (<see cref="SiteLanguages.Direction" />): right to left for Arabic, Hebrew, Persian, Urdu and a few more.
	/// </summary>
	public string Direction { get; init; } = "";
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

	/// <summary>
	///     Code syntax highlighting theme, one of <see cref="CodeThemes.Names" />. The default theme
	///     renders an unknown name as <see cref="CodeThemes.Default" /> and the build warns about it.
	/// </summary>
	public string CodeTheme { get; init; } = MokaDefaults.CodeTheme;

	/// <summary>Show "Edit this page" link on each page.</summary>
	public bool ShowEditLink { get; init; } = MokaDefaults.ShowEditLink;

	/// <summary>Show last-updated timestamp on pages.</summary>
	public bool ShowLastUpdated { get; init; } = MokaDefaults.ShowLastUpdated;

	/// <summary>
	///     Show the people who committed to each Markdown page, under the "Last updated" date. The
	///     names come from the same <c>git log</c> as the dates, with the repository's
	///     <c>.mailmap</c> applied and one entry per email. Off by default; turning it on makes the
	///     build read the docs folder's whole history.
	/// </summary>
	public bool ShowContributors { get; init; } = MokaDefaults.ShowContributors;

	/// <summary>Show color theme preset selector in the header.</summary>
	public bool ColorThemes { get; init; } = MokaDefaults.ShowColorThemeSelector;

	/// <summary>Show code syntax theme selector in the header.</summary>
	public bool CodeThemeSelector { get; init; } = MokaDefaults.ShowCodeThemeSelector;

	/// <summary>Code block window style, one of <see cref="CodeStyles.Names" />: "plain", "macos", "terminal", or "vscode".</summary>
	public string CodeStyle { get; init; } = MokaDefaults.CodeStyle;

	/// <summary>Show code block style selector in the header.</summary>
	public bool CodeStyleSelector { get; init; } = MokaDefaults.ShowCodeStyleSelector;

	/// <summary>
	///     The theme preset the options were filled in from, one of <see cref="ThemePresets.Names" />, as
	///     written. Empty for none. The preset is already applied to the other properties: it filled in
	///     each option that mokadocs.yaml leaves out (in the ASP.NET Core host, each option still at its
	///     default value). An unknown name applies nothing, and the build warns about it.
	/// </summary>
	public string Preset { get; init; } = "";

	/// <summary>
	///     Appearance style of the default theme, one of <see cref="ThemeStyles.Names" />. Readers can
	///     pick another one when
	///     <see cref="StyleSelector" /> is on. An unknown name renders as <c>default</c> and the
	///     build warns about it.
	/// </summary>
	public string Style { get; init; } = MokaDefaults.Style;

	/// <summary>Show the appearance style selector in the header. Default is true.</summary>
	public bool StyleSelector { get; init; } = MokaDefaults.ShowStyleSelector;

	/// <summary>
	///     Let readers pick a text size (90%, 100%, 112.5% or 125%) in the Appearance panel. The
	///     pick is saved in the browser and applies to every page. Default is true.
	/// </summary>
	public bool TextSizeSelector { get; init; } = MokaDefaults.ShowTextSizeSelector;

	/// <summary>
	///     Let readers pick a narrow, default, wide, wider or full reading width in the Appearance
	///     panel. The pick is saved in the browser and applies to every page. Default is true.
	/// </summary>
	public bool WidthSelector { get; init; } = MokaDefaults.ShowWidthSelector;

	/// <summary>
	///     Let readers switch the layout variants of a docs page (every part of <see cref="Layout" /> in
	///     <see cref="ThemeLayouts.ReaderParts" />) in a Layout section of the Appearance panel. Picks are
	///     saved in the browser and apply to every page; the configured variants stay the defaults. Pages
	///     then carry the markup of every variant and always link the layout stylesheet. Default is false.
	/// </summary>
	public bool LayoutSelector { get; init; } = MokaDefaults.ShowLayoutSelector;

	/// <summary>
	///     Layout variants of the default theme's page parts: width, navbar, footer, "On this page",
	///     breadcrumbs, the previous/next links, the sidebar, the landing hero, admonitions, tables,
	///     spacing and the typeface. Every part defaults to <c>default</c>.
	/// </summary>
	public ThemeLayoutOptions Layout { get; init; } = new();

	/// <summary>
	///     How pages get the sidebar navigation: <c>auto</c> (the default), <c>inline</c> or
	///     <c>shared</c>. <c>inline</c> writes the whole sidebar into every page. <c>shared</c> writes
	///     it once, to <c>_theme/js/sidebar.js</c>, which every page loads; each page keeps the
	///     top-level entries and the ones along its own path for readers without JavaScript.
	///     <c>auto</c> is <c>inline</c> up to <see cref="SidebarInlineLimit" /> entries and
	///     <c>shared</c> above. An unknown name means <c>auto</c>, and the build warns about it.
	/// </summary>
	/// <remarks>
	///     Only the default theme can share its sidebar. A custom theme always gets it inline.
	/// </remarks>
	public string Sidebar { get; init; } = MokaDefaults.Sidebar;

	/// <summary>
	///     Most sidebar entries (links and section headings) the <c>auto</c> sidebar mode writes into
	///     every page. Default is 300.
	/// </summary>
	public int SidebarInlineLimit { get; init; } = MokaDefaults.SidebarInlineLimit;

	/// <summary>
	///     Show a button that hides and shows the sidebar on wide screens. The reader's choice is kept
	///     in the browser for later pages. Default is false.
	/// </summary>
	public bool SidebarToggle { get; init; } = MokaDefaults.SidebarToggle;

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

	/// <summary>
	///     Open a content image full size in a viewer when the reader clicks it, or presses Enter on
	///     it. Images inside links and images smaller than 96 by 64 pixels are left alone. Default
	///     is true.
	/// </summary>
	public bool ImageZoom { get; init; } = MokaDefaults.ImageZoom;

	/// <summary>Maximum heading level shown in the table of contents (2-6). Default is 3 (h2 + h3).</summary>
	public int TocDepth { get; init; } = MokaDefaults.TocDepth;

	/// <summary>Show the version selector dropdown.</summary>
	public bool ShowVersionSelector { get; init; } = MokaDefaults.ShowVersionSelector;

	/// <summary>Show "Built with MokaDocs" branding and version in the footer. Default is true.</summary>
	public bool ShowBuiltWith { get; init; } = MokaDefaults.ShowBuiltWith;

	/// <summary>Social links displayed in the site header.</summary>
	public List<SocialLink> SocialLinks { get; init; } = [];

	/// <summary>
	///     Default color theme applied on first visit (before localStorage is set).
	///     One of <see cref="ColorPresets.Names" />, or empty for no preset. Defaults to <c>ocean</c>.
	/// </summary>
	public string DefaultColorTheme { get; init; } = "ocean";

	/// <summary>
	///     An announcement shown above the header of every page, or <c>null</c> for none. Only the
	///     default theme renders it.
	/// </summary>
	public ThemeBanner? Banner { get; init; }

	/// <summary>
	///     The site's own wording for the theme's text, by key (<see cref="ThemeStrings.All" />), such as
	///     <c>toc.title</c>: it replaces both the English and the built-in translation. A key the theme doesn't
	///     have is reported and ignored.
	/// </summary>
	public IReadOnlyDictionary<string, string> Strings { get; init; } = new Dictionary<string, string>();
}

/// <summary>
///     The announcement banner of <c>theme.options.banner</c>: one line of plain text above the header,
///     with an optional link, which readers can dismiss.
/// </summary>
public sealed record ThemeBanner
{
	/// <summary>The announcement, as plain text. HTML in it shows as written.</summary>
	public required string Text { get; init; }

	/// <summary>
	///     Where the banner's link goes: an absolute URL, or a path from the site root such as
	///     <c>/blog/1.9</c>, which gets the base path. <c>null</c> for no link.
	/// </summary>
	public string? Link { get; init; }

	/// <summary>The link's text. <c>null</c> shows <c>Learn more</c>.</summary>
	public string? LinkText { get; init; }

	/// <summary>
	///     What a reader's dismissal is remembered by. A reader who dismissed the banner sees it again
	///     once this changes. <c>null</c> or empty uses <see cref="EffectiveId" />'s hash of the text and
	///     the link, so changing either shows the banner again.
	/// </summary>
	public string? Id { get; init; }

	/// <summary>Whether the banner has a button that hides it for the reader. Default is true.</summary>
	public bool Dismissible { get; init; } = true;

	/// <summary>
	///     The look, one of <see cref="BannerVariants.Names" />: <c>info</c> (the default),
	///     <c>warning</c> or <c>accent</c>. An unknown name renders as <c>info</c>, and the build warns
	///     about it.
	/// </summary>
	public string Variant { get; init; } = BannerVariants.Info;

	/// <summary>
	///     <see cref="Id" /> when it is set, else the first 12 hexadecimal digits of the SHA-256 hash of
	///     <see cref="Text" /> and <see cref="Link" />.
	/// </summary>
	public string EffectiveId => string.IsNullOrWhiteSpace(Id) ? DerivedId(Text, Link) : Id.Trim();

	private static string DerivedId(string text, string? link)
	{
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text + "\n" + link));
		return Convert.ToHexStringLower(hash)[..12];
	}
}

/// <summary>
///     The layout variant of each page part, set under <c>theme.options.layout</c>. Names come from
///     <see cref="ThemeLayouts" />; an unknown name renders as <c>default</c> and the build warns
///     about it. Only the default theme reads these.
/// </summary>
public sealed record ThemeLayoutOptions
{
	/// <summary>Page width. See <see cref="ThemeLayouts.Width" />.</summary>
	public string Width { get; init; } = ThemeLayouts.Default;

	/// <summary>The top bar. See <see cref="ThemeLayouts.Navbar" />.</summary>
	public string Navbar { get; init; } = ThemeLayouts.Default;

	/// <summary>The footer. See <see cref="ThemeLayouts.Footer" />.</summary>
	public string Footer { get; init; } = ThemeLayouts.Default;

	/// <summary>The "On this page" panel. See <see cref="ThemeLayouts.Toc" />.</summary>
	public string Toc { get; init; } = ThemeLayouts.Default;

	/// <summary>Breadcrumbs. See <see cref="ThemeLayouts.Breadcrumbs" />.</summary>
	public string Breadcrumbs { get; init; } = ThemeLayouts.Default;

	/// <summary>The previous and next links. See <see cref="ThemeLayouts.PageNav" />.</summary>
	public string PageNav { get; init; } = ThemeLayouts.Default;

	/// <summary>The sidebar's look. See <see cref="ThemeLayouts.Sidebar" />.</summary>
	public string Sidebar { get; init; } = ThemeLayouts.Default;

	/// <summary>What the sidebar lists. See <see cref="ThemeLayouts.SidebarScope" />.</summary>
	public string SidebarScope { get; init; } = ThemeLayouts.Default;

	/// <summary>The landing page as a whole. See <see cref="ThemeLayouts.Home" />.</summary>
	public string Home { get; init; } = ThemeLayouts.Default;

	/// <summary>The landing page's hero. See <see cref="ThemeLayouts.Hero" />.</summary>
	public string Hero { get; init; } = ThemeLayouts.Default;

	/// <summary>The backdrop behind the landing page's hero. See <see cref="ThemeLayouts.HeroBackground" />.</summary>
	public string HeroBackground { get; init; } = ThemeLayouts.Default;

	/// <summary>The landing page's feature cards. See <see cref="ThemeLayouts.Features" />.</summary>
	public string Features { get; init; } = ThemeLayouts.Default;

	/// <summary>Admonitions. See <see cref="ThemeLayouts.Admonitions" />.</summary>
	public string Admonitions { get; init; } = ThemeLayouts.Default;

	/// <summary>Tables in the content. See <see cref="ThemeLayouts.Tables" />.</summary>
	public string Tables { get; init; } = ThemeLayouts.Default;

	/// <summary>Spacing. See <see cref="ThemeLayouts.Density" />.</summary>
	public string Density { get; init; } = ThemeLayouts.Default;

	/// <summary>The typeface. See <see cref="ThemeLayouts.Font" />.</summary>
	public string Font { get; init; } = ThemeLayouts.Default;

	/// <summary>Section headings in the content. See <see cref="ThemeLayouts.Headings" />.</summary>
	public string Headings { get; init; } = ThemeLayouts.Default;

	/// <summary>Links in the content. See <see cref="ThemeLayouts.Links" />.</summary>
	public string Links { get; init; } = ThemeLayouts.Default;

	/// <summary>The variant name configured for <paramref name="part" />, as written.</summary>
	/// <param name="part">One of <see cref="ThemeLayouts.Parts" />.</param>
	/// <returns>The configured name, which may be empty or unknown.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="part" /> is not one of <see cref="ThemeLayouts.Parts" />.</exception>
	public string NameOf(ThemeLayoutPart part) => part.Key switch
	{
		"width" => Width,
		"navbar" => Navbar,
		"footer" => Footer,
		"toc" => Toc,
		"breadcrumbs" => Breadcrumbs,
		"pageNav" => PageNav,
		"sidebar" => Sidebar,
		"sidebarScope" => SidebarScope,
		"home" => Home,
		"hero" => Hero,
		"heroBackground" => HeroBackground,
		"features" => Features,
		"admonitions" => Admonitions,
		"tables" => Tables,
		"density" => Density,
		"font" => Font,
		"headings" => Headings,
		"links" => Links,
		_ => throw new ArgumentOutOfRangeException(nameof(part), part.Key, "Not a layout part")
	};
}

/// <summary>
///     A social link displayed in the site footer.
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

	/// <summary>Sort order within its parent section. Lower values appear first. Default is 0.</summary>
	public int Order { get; init; }

	/// <summary>
	///     Lists the pages below <see cref="Path" /> as a tree, the way the sidebar is built when
	///     there is no <c>nav:</c> list: folders become sections and API types are grouped by
	///     namespace. Ignored when <see cref="Children" /> is not empty. Without it, an item lists
	///     the pages one route segment below its path.
	/// </summary>
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

	/// <summary>
	///     Not used. Search always uses the built-in <c>search-index.json</c> and the theme's script,
	///     and <c>features.search.provider</c> in mokadocs.yaml is ignored with a warning.
	/// </summary>
	[Obsolete("Search has no providers; this value is ignored. It will be removed in 2.0.")]
	public string Provider { get; init; } = "flexsearch";
}

/// <summary>Versioning feature configuration.</summary>
public sealed record VersioningFeatureConfig
{
	/// <summary>Whether versioning is enabled.</summary>
	public bool Enabled { get; init; }

	/// <summary>
	///     Versioning strategy. <c>directory</c>, the default, has <c>mokadocs build</c> build every
	///     version that names a git branch or tag into its own folder. <c>dropdown-only</c> builds the
	///     working tree alone and only lists the other versions.
	/// </summary>
	public string Strategy { get; init; } = "directory";

	/// <summary>Version definitions.</summary>
	public List<VersionDefinition> Versions { get; init; } = [];
}

/// <summary>A version definition for multi-version documentation.</summary>
public sealed record VersionDefinition
{
	/// <summary>
	///     Display label (e.g., "v2.0"). When empty, the label is <c>v{major}.{minor}</c> of the
	///     documented package's version.
	/// </summary>
	public required string Label { get; init; }

	/// <summary>Git branch this version is built from.</summary>
	public string? Branch { get; init; }

	/// <summary>Git tag this version is built from. Wins over <see cref="Branch" /> when both are set.</summary>
	public string? Tag { get; init; }

	/// <summary>Whether this is the default version.</summary>
	public bool Default { get; init; }

	/// <summary>Whether this is a prerelease version.</summary>
	public bool Prerelease { get; init; }
}

/// <summary>Blog feature configuration.</summary>
public sealed record BlogFeatureConfig
{
	/// <summary>The folder under the docs folder that holds the posts when <see cref="Path" /> is not set.</summary>
	public const string DefaultPath = "blog";

	/// <summary>Posts per index page when <see cref="PostsPerPage" /> is not set.</summary>
	public const int DefaultPostsPerPage = 10;

	/// <summary>Latest posts listed under the blog's sidebar entry when <see cref="SidebarPosts" /> is not set.</summary>
	public const int DefaultSidebarPosts = 5;

	/// <summary>Whether the blog is enabled.</summary>
	public bool Enabled { get; init; }

	/// <summary>
	///     Folder under the docs folder that holds the posts, such as <c>blog</c>. Every Markdown
	///     file below it is a post except its <c>index.md</c>, which becomes the introduction of
	///     the blog's index page. Posts keep the route of their file.
	/// </summary>
	public string Path { get; init; } = DefaultPath;

	/// <summary>
	///     Route of the blog's index page, under which the build also writes the page list
	///     (<c>{route}/page/2</c>), the tag pages (<c>{route}/tags/{tag}</c>) and the feeds.
	///     <c>null</c> uses <c>/</c> followed by <see cref="Path" />.
	/// </summary>
	public string? Route { get; init; }

	/// <summary>Title of the blog's index page, sidebar entry and feeds. Default: <c>Blog</c>.</summary>
	public string Title { get; init; } = "Blog";

	/// <summary>Text under the index page title and the description of the feeds.</summary>
	public string Description { get; init; } = "";

	/// <summary>Number of posts per page.</summary>
	public int PostsPerPage { get; init; } = DefaultPostsPerPage;

	/// <summary>
	///     How many of the latest posts the blog's sidebar entry lists, newest first. 0 lists none,
	///     so the entry only links to the index page.
	/// </summary>
	public int SidebarPosts { get; init; } = DefaultSidebarPosts;

	/// <summary>
	///     Feed formats to write: <c>rss</c> (<c>{route}/rss.xml</c>) and <c>atom</c>
	///     (<c>{route}/atom.xml</c>), lower case as written. Empty writes no feed. Feeds need
	///     <c>site.url</c>.
	/// </summary>
	public List<string> Feeds { get; init; } = ["rss"];

	/// <summary>
	///     Post authors defined once, keyed by the name a post's <c>author</c> front matter uses.
	///     A name that is no key here is shown as written.
	/// </summary>
	public Dictionary<string, BlogAuthor> Authors { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Show author information on posts.</summary>
	public bool ShowAuthors { get; init; } = true;
}

/// <summary>An author of blog posts, defined once under <c>features.blog.authors</c>.</summary>
public sealed record BlogAuthor
{
	/// <summary>The name shown on posts and in feeds.</summary>
	public required string Name { get; init; }

	/// <summary>A page about the author, such as a profile. <c>null</c> for none.</summary>
	public string? Url { get; init; }

	/// <summary>An image of the author: an absolute URL, or a path from the site root. <c>null</c> for none.</summary>
	public string? Avatar { get; init; }
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
	/// <summary>Master switch - nothing calls home unless true.</summary>
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
	public bool AiSummaries { get; init; } = MokaDefaults.EnableAiSearch;

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

	/// <summary>
	///     Minify the HTML pages and the default theme's CSS and JS: whitespace collapsed and comments
	///     removed. Off by default.
	/// </summary>
	public bool Minify { get; init; } = MokaDefaults.MinifyOutput;

	/// <summary>Generate sitemap.xml.</summary>
	public bool Sitemap { get; init; } = MokaDefaults.GenerateSitemap;

	/// <summary>Generate robots.txt.</summary>
	public bool Robots { get; init; } = MokaDefaults.GenerateRobots;

	/// <summary>
	///     Write a Markdown copy of each public page next to its HTML (<c>/guide/intro</c> gets
	///     <c>/guide/intro.md</c>), plus <c>llms.txt</c> and <c>llms-full.txt</c> at the site root.
	///     On by default; the <c>MOKADOCS_LLMS</c> environment variable overrides it.
	/// </summary>
	public bool Llms { get; init; } = MokaDefaults.GenerateLlmsFiles;

	/// <summary>Cache intermediate results for faster rebuilds.</summary>
	public bool Cache { get; init; } = true;

	/// <summary>
	///     Treat warnings as errors, so <c>mokadocs build</c> exits 1 when the build reports any
	///     warning (the <c>--warnings-as-errors</c> option does the same for one run).
	/// </summary>
	public bool Strict { get; init; }

	/// <summary>
	///     Base path for the generated site when deployed to a subdirectory.
	///     For example, "/Moka.Docs" for GitHub Pages at https://user.github.io/Moka.Docs/.
	///     Defaults to "/" (site root).
	/// </summary>
	public string BasePath { get; init; } = "/";
}
