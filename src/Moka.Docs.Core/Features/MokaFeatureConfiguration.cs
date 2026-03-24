namespace Moka.Docs.Core.Features;

/// <summary>
///     Provides default feature flag values. The library author changes these
///     in <see cref="MokaDefaults" /> before shipping to control what's enabled
///     out of the box. Users can override via appsettings.json or environment variables.
/// </summary>
public static class MokaFeatureConfiguration
{
    /// <summary>
    ///     Returns the default FeatureManagement configuration dictionary.
    ///     Values are sourced from <see cref="MokaDefaults" /> so that the static
    ///     properties remain the single source of truth.
    /// </summary>
    public static Dictionary<string, bool> GetDefaults()
    {
        return new Dictionary<string, bool>
        {
            // Premium/Cloud — all OFF by default
            [MokaFeatureFlags.Cloud] = MokaDefaults.EnableCloudFeatures,
            [MokaFeatureFlags.Analytics] = MokaDefaults.EnableAnalytics,
            [MokaFeatureFlags.AiSearch] = MokaDefaults.EnableAISearch,
            [MokaFeatureFlags.PdfExport] = MokaDefaults.EnablePdfExport,
            [MokaFeatureFlags.CustomDomain] = MokaDefaults.EnableCustomDomain,
            [MokaFeatureFlags.WhiteLabel] = MokaDefaults.EnableWhiteLabel,
            [MokaFeatureFlags.PrivateRepo] = false,
            [MokaFeatureFlags.TeamCollaboration] = false,
            [MokaFeatureFlags.CustomBranding] = false,
            [MokaFeatureFlags.SSOAuth] = false,
            [MokaFeatureFlags.AuditLog] = false,
            [MokaFeatureFlags.ApiAccess] = false,

            // UI Selectors — all ON by default
            [MokaFeatureFlags.ColorThemeSelector] = MokaDefaults.ShowColorThemeSelector,
            [MokaFeatureFlags.CodeThemeSelector] = MokaDefaults.ShowCodeThemeSelector,
            [MokaFeatureFlags.CodeStyleSelector] = MokaDefaults.ShowCodeStyleSelector,
            [MokaFeatureFlags.DarkModeToggle] = MokaDefaults.ShowDarkModeToggle,

            // Page Features
            [MokaFeatureFlags.FeedbackWidget] = MokaDefaults.ShowFeedbackWidget,
            [MokaFeatureFlags.SearchBar] = MokaDefaults.ShowSearch,
            [MokaFeatureFlags.TableOfContents] = MokaDefaults.ShowTableOfContents,
            [MokaFeatureFlags.PrevNextNavigation] = MokaDefaults.ShowPrevNext,
            [MokaFeatureFlags.Breadcrumbs] = MokaDefaults.ShowBreadcrumbs,
            [MokaFeatureFlags.BackToTop] = MokaDefaults.ShowBackToTop,
            [MokaFeatureFlags.VersionSelector] = MokaDefaults.ShowVersionSelector,
            [MokaFeatureFlags.LastUpdated] = MokaDefaults.ShowLastUpdated,
            [MokaFeatureFlags.EditLink] = MokaDefaults.ShowEditLink,
            [MokaFeatureFlags.Contributors] = false,
            [MokaFeatureFlags.PageAnimations] = MokaDefaults.ShowAnimations,

            // Code Block Features
            [MokaFeatureFlags.CopyButton] = MokaDefaults.ShowCopyButton,
            [MokaFeatureFlags.LineNumbers] = MokaDefaults.ShowLineNumbers,
            [MokaFeatureFlags.CodeLanguageBadge] = true,

            // Plugins — all OFF by default (opt-in)
            [MokaFeatureFlags.ReplPlugin] = false,
            [MokaFeatureFlags.BlazorPreview] = false,
            [MokaFeatureFlags.ChangelogPlugin] = false,
            [MokaFeatureFlags.OpenApiPlugin] = false,

            // API Docs Features — all ON by default
            [MokaFeatureFlags.TypeDependencyGraph] = true,
            [MokaFeatureFlags.ViewSource] = true,
            [MokaFeatureFlags.InheritDocResolution] = true,
            [MokaFeatureFlags.InstallWidget] = true,

            // Build Features
            [MokaFeatureFlags.Sitemap] = MokaDefaults.GenerateSitemap,
            [MokaFeatureFlags.RobotsTxt] = MokaDefaults.GenerateRobots,
            [MokaFeatureFlags.SearchIndex] = MokaDefaults.SearchEnabled,
            [MokaFeatureFlags.MinifyOutput] = true,

            // Documentation Gating — OFF (hide premium docs from public builds)
            [MokaFeatureFlags.ShowCloudDocs] = false,
            [MokaFeatureFlags.ShowPremiumDocs] = false,
            [MokaFeatureFlags.ShowBetaDocs] = false,
            [MokaFeatureFlags.ShowInternalDocs] = false
        };
    }
}