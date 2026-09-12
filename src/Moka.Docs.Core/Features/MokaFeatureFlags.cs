namespace Moka.Docs.Core.Features;

/// <summary>
///     Feature flag names registered with Microsoft.FeatureManagement.
/// </summary>
/// <remarks>
///     The build reads these flags in one place: a page whose front matter sets
///     <c>requires: &lt;flag&gt;</c> is left out unless that flag is on. None of them switches a
///     UI element or build step by itself; those are controlled by <c>mokadocs.yaml</c>.
///     Defaults come from <see cref="MokaFeatureConfiguration.GetDefaults" />, and an
///     environment variable overrides one, for example
///     <c>MOKADOCS_FeatureManagement__Cloud=true</c>. A name with no value from either source
///     counts as off.
/// </remarks>
public static class MokaFeatureFlags
{
	#region Premium/Cloud

	/// <summary>Cloud-hosted MokaDocs features.</summary>
	public const string Cloud = "Cloud";

	/// <summary>Site analytics.</summary>
	public const string Analytics = "Analytics";

	/// <summary>AI-assisted search.</summary>
	public const string AiSearch = "AiSearch";

	/// <summary>PDF export.</summary>
	public const string PdfExport = "PdfExport";

	/// <summary>Custom domains for hosted sites.</summary>
	public const string CustomDomain = "CustomDomain";

	/// <summary>Removing MokaDocs branding.</summary>
	public const string WhiteLabel = "WhiteLabel";

	/// <summary>Documentation built from private repositories.</summary>
	public const string PrivateRepo = "PrivateRepo";

	/// <summary>Team collaboration.</summary>
	public const string TeamCollaboration = "TeamCollaboration";

	/// <summary>Custom branding.</summary>
	public const string CustomBranding = "CustomBranding";

	/// <summary>Single sign-on. The flag value is <c>SSOAuth</c>.</summary>
	public const string SsoAuth = "SSOAuth";

	/// <summary>Audit logging.</summary>
	public const string AuditLog = "AuditLog";

	/// <summary>Programmatic API access.</summary>
	public const string ApiAccess = "ApiAccess";

	#endregion

	#region UI Selectors

	/// <summary>The color theme picker.</summary>
	public const string ColorThemeSelector = "ColorThemeSelector";

	/// <summary>The code highlighting theme picker.</summary>
	public const string CodeThemeSelector = "CodeThemeSelector";

	/// <summary>The code block style picker.</summary>
	public const string CodeStyleSelector = "CodeStyleSelector";

	/// <summary>The dark mode toggle.</summary>
	public const string DarkModeToggle = "DarkModeToggle";

	#endregion

	#region Page Features

	/// <summary>The "Was this page helpful?" widget.</summary>
	public const string FeedbackWidget = "FeedbackWidget";

	/// <summary>The search bar.</summary>
	public const string SearchBar = "SearchBar";

	/// <summary>The table of contents sidebar.</summary>
	public const string TableOfContents = "TableOfContents";

	/// <summary>Previous and next page links.</summary>
	public const string PrevNextNavigation = "PrevNextNavigation";

	/// <summary>Breadcrumb navigation.</summary>
	public const string Breadcrumbs = "Breadcrumbs";

	/// <summary>The back-to-top button.</summary>
	public const string BackToTop = "BackToTop";

	/// <summary>The version dropdown.</summary>
	public const string VersionSelector = "VersionSelector";

	/// <summary>The last updated date.</summary>
	public const string LastUpdated = "LastUpdated";

	/// <summary>The "Edit this page" link.</summary>
	public const string EditLink = "EditLink";

	/// <summary>Page contributor lists.</summary>
	public const string Contributors = "Contributors";

	/// <summary>Page transition animations.</summary>
	public const string PageAnimations = "PageAnimations";

	#endregion

	#region Code Block Features

	/// <summary>The copy button on code blocks.</summary>
	public const string CopyButton = "CopyButton";

	/// <summary>Line numbers on code blocks.</summary>
	public const string LineNumbers = "LineNumbers";

	/// <summary>The language badge on code blocks.</summary>
	public const string CodeLanguageBadge = "CodeLanguageBadge";

	#endregion

	#region Plugins

	/// <summary>The interactive C# REPL plugin.</summary>
	public const string ReplPlugin = "ReplPlugin";

	/// <summary>The Blazor component preview plugin.</summary>
	public const string BlazorPreview = "BlazorPreview";

	/// <summary>The changelog plugin.</summary>
	public const string ChangelogPlugin = "ChangelogPlugin";

	/// <summary>The OpenAPI plugin.</summary>
	public const string OpenApiPlugin = "OpenApiPlugin";

	#endregion

	#region API Docs Features

	/// <summary>The type dependency graph on API pages.</summary>
	public const string TypeDependencyGraph = "TypeDependencyGraph";

	/// <summary>The "View Source" section on API pages.</summary>
	public const string ViewSource = "ViewSource";

	/// <summary><c>&lt;inheritdoc/&gt;</c> resolution.</summary>
	public const string InheritDocResolution = "InheritDocResolution";

	/// <summary>The NuGet install widget.</summary>
	public const string InstallWidget = "InstallWidget";

	#endregion

	#region Build Features

	/// <summary>sitemap.xml generation.</summary>
	public const string Sitemap = "Sitemap";

	/// <summary>robots.txt generation.</summary>
	public const string RobotsTxt = "RobotsTxt";

	/// <summary>Search index generation.</summary>
	public const string SearchIndex = "SearchIndex";

	/// <summary>Output minification.</summary>
	public const string MinifyOutput = "MinifyOutput";

	#endregion

	#region Documentation Gating

	/// <summary>Pages about cloud features.</summary>
	public const string ShowCloudDocs = "ShowCloudDocs";

	/// <summary>Pages about premium features.</summary>
	public const string ShowPremiumDocs = "ShowPremiumDocs";

	/// <summary>Pages about beta features.</summary>
	public const string ShowBetaDocs = "ShowBetaDocs";

	/// <summary>Internal-only pages.</summary>
	public const string ShowInternalDocs = "ShowInternalDocs";

	#endregion
}
