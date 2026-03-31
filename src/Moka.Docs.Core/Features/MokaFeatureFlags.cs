namespace Moka.Docs.Core.Features;

/// <summary>
///     All MokaDocs feature flag names. Used with IFeatureManager and
///     Microsoft.FeatureManagement for runtime feature toggling.
/// </summary>
public static class MokaFeatureFlags
{
	#region Premium/Cloud

	public const string Cloud = "Cloud";
	public const string Analytics = "Analytics";
	public const string AiSearch = "AiSearch";
	public const string PdfExport = "PdfExport";
	public const string CustomDomain = "CustomDomain";
	public const string WhiteLabel = "WhiteLabel";
	public const string PrivateRepo = "PrivateRepo";
	public const string TeamCollaboration = "TeamCollaboration";
	public const string CustomBranding = "CustomBranding";
	public const string SsoAuth = "SSOAuth";
	public const string AuditLog = "AuditLog";
	public const string ApiAccess = "ApiAccess";

	#endregion

	#region UI Selectors

	public const string ColorThemeSelector = "ColorThemeSelector";
	public const string CodeThemeSelector = "CodeThemeSelector";
	public const string CodeStyleSelector = "CodeStyleSelector";
	public const string DarkModeToggle = "DarkModeToggle";

	#endregion

	#region Page Features

	public const string FeedbackWidget = "FeedbackWidget";
	public const string SearchBar = "SearchBar";
	public const string TableOfContents = "TableOfContents";
	public const string PrevNextNavigation = "PrevNextNavigation";
	public const string Breadcrumbs = "Breadcrumbs";
	public const string BackToTop = "BackToTop";
	public const string VersionSelector = "VersionSelector";
	public const string LastUpdated = "LastUpdated";
	public const string EditLink = "EditLink";
	public const string Contributors = "Contributors";
	public const string PageAnimations = "PageAnimations";

	#endregion

	#region Code Block Features

	public const string CopyButton = "CopyButton";
	public const string LineNumbers = "LineNumbers";
	public const string CodeLanguageBadge = "CodeLanguageBadge";

	#endregion

	#region Plugins

	public const string ReplPlugin = "ReplPlugin";
	public const string BlazorPreview = "BlazorPreview";
	public const string ChangelogPlugin = "ChangelogPlugin";
	public const string OpenApiPlugin = "OpenApiPlugin";

	#endregion

	#region API Docs Features

	public const string TypeDependencyGraph = "TypeDependencyGraph";
	public const string ViewSource = "ViewSource";
	public const string InheritDocResolution = "InheritDocResolution";
	public const string InstallWidget = "InstallWidget";

	#endregion

	#region Build Features

	public const string Sitemap = "Sitemap";
	public const string RobotsTxt = "RobotsTxt";
	public const string SearchIndex = "SearchIndex";
	public const string MinifyOutput = "MinifyOutput";

	#endregion

	#region Documentation Gating

	public const string ShowCloudDocs = "ShowCloudDocs";
	public const string ShowPremiumDocs = "ShowPremiumDocs";
	public const string ShowBetaDocs = "ShowBetaDocs";
	public const string ShowInternalDocs = "ShowInternalDocs";

	#endregion
}
