namespace Moka.Docs.Core;

/// <summary>
///     Central defaults for MokaDocs. The library author sets these to control
///     what ships as the default experience. Users override via mokadocs.yaml,
///     and environment variables override everything.
///     Priority: Environment Variable > User YAML Config > MokaDefaults
/// </summary>
public static class MokaDefaults
{
    #region UI Feature Toggles

    /// <summary>Show the color theme palette selector in the header.</summary>
    public static bool ShowColorThemeSelector { get; set; } = true;

    /// <summary>Show the code syntax theme selector in the header.</summary>
    public static bool ShowCodeThemeSelector { get; set; } = false;

    /// <summary>Show the code block window style selector in the header.</summary>
    public static bool ShowCodeStyleSelector { get; set; } = false;

    /// <summary>Show the "Was this page helpful?" feedback widget.</summary>
    public static bool ShowFeedbackWidget { get; set; } = true;

    /// <summary>Show the version selector dropdown.</summary>
    public static bool ShowVersionSelector { get; set; } = true;

    /// <summary>Show the dark/light mode toggle.</summary>
    public static bool ShowDarkModeToggle { get; set; } = true;

    /// <summary>Show page animations (fade-in, slide-up, etc).</summary>
    public static bool ShowAnimations { get; set; } = false;

    /// <summary>Show the search bar and Ctrl+K shortcut.</summary>
    public static bool ShowSearch { get; set; } = true;

    /// <summary>Show the table of contents sidebar.</summary>
    public static bool ShowTableOfContents { get; set; } = true;

    /// <summary>Show "Last updated" date on pages.</summary>
    public static bool ShowLastUpdated { get; set; } = true;

    /// <summary>Show "Edit on GitHub" link.</summary>
    public static bool ShowEditLink { get; set; } = false;

    /// <summary>Show Previous/Next navigation at bottom of pages.</summary>
    public static bool ShowPrevNext { get; set; } = true;

    /// <summary>Show breadcrumb navigation.</summary>
    public static bool ShowBreadcrumbs { get; set; } = true;

    /// <summary>Show the back-to-top button.</summary>
    public static bool ShowBackToTop { get; set; } = true;

    /// <summary>Show copy button on code blocks.</summary>
    public static bool ShowCopyButton { get; set; } = true;

    /// <summary>Show line numbers on code blocks.</summary>
    public static bool ShowLineNumbers { get; set; } = true;

    /// <summary>Maximum heading level shown in the table of contents (2–6).</summary>
    public static int TocDepth { get; set; } = 3;

    #endregion

    #region Theme Defaults

    /// <summary>Default primary color (hex).</summary>
    public static string PrimaryColor { get; set; } = "#0ea5e9";

    /// <summary>Default code syntax theme.</summary>
    public static string CodeTheme { get; set; } = "catppuccin-mocha";

    /// <summary>Default code block window style (plain, macos, terminal, vscode).</summary>
    public static string CodeStyle { get; set; } = "plain";

    /// <summary>Default theme name.</summary>
    public static string ThemeName { get; set; } = "default";

    #endregion

    #region Build Defaults

    /// <summary>Generate sitemap.xml by default.</summary>
    public static bool GenerateSitemap { get; set; } = true;

    /// <summary>Generate robots.txt by default.</summary>
    public static bool GenerateRobots { get; set; } = true;

    /// <summary>Clean output directory before build.</summary>
    public static bool CleanOutput { get; set; } = true;

    #endregion

    #region Feature Defaults

    /// <summary>Enable search by default.</summary>
    public static bool SearchEnabled { get; set; } = true;

    /// <summary>Default search provider.</summary>
    public static string SearchProvider { get; set; } = "flexsearch";

    #endregion

    #region Cloud/Premium Features

    /// <summary>Enable cloud features (requires license).</summary>
    public static bool EnableCloudFeatures { get; set; } = false;

    /// <summary>Enable analytics dashboard.</summary>
    public static bool EnableAnalytics { get; set; } = false;

    /// <summary>Enable AI-powered semantic search.</summary>
    public static bool EnableAISearch { get; set; } = false;

    /// <summary>Enable PDF export.</summary>
    public static bool EnablePdfExport { get; set; } = false;

    /// <summary>Enable custom domain support.</summary>
    public static bool EnableCustomDomain { get; set; } = false;

    /// <summary>Enable white-labeling (remove MokaDocs branding).</summary>
    public static bool EnableWhiteLabel { get; set; } = false;

    #endregion

    #region Environment Variable Resolution

    private const string EnvPrefix = "MOKADOCS_";

    /// <summary>
    ///     Resolves a default value with environment variable override.
    ///     Environment variables use the pattern: MOKADOCS_PROPERTY_NAME
    ///     e.g., MOKADOCS_SHOW_COLOR_THEME_SELECTOR, MOKADOCS_PRIMARY_COLOR
    /// </summary>
    /// <typeparam name="T">The type of value to resolve.</typeparam>
    /// <param name="envVarName">The environment variable suffix (without MOKADOCS_ prefix).</param>
    /// <param name="defaultValue">The default value if no environment variable is set.</param>
    /// <returns>The resolved value.</returns>
    public static T Resolve<T>(string envVarName, T defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(EnvPrefix + envVarName);
        if (string.IsNullOrEmpty(envValue))
            return defaultValue;

        var targetType = typeof(T);

        if (targetType == typeof(bool))
            return (T)(object)ParseBool(envValue, (bool)(object)defaultValue!);

        if (targetType == typeof(string))
            return (T)(object)envValue;

        if (targetType == typeof(int) && int.TryParse(envValue, out var intVal))
            return (T)(object)intVal;

        return defaultValue;
    }

    /// <summary>
    ///     Resolves a bool value: env var > yaml config > MokaDefaults.
    /// </summary>
    /// <param name="envVarName">The environment variable suffix (without MOKADOCS_ prefix).</param>
    /// <param name="yamlValue">The value from the user's YAML config, or null if not specified.</param>
    /// <param name="defaultValue">The default value from MokaDefaults.</param>
    /// <returns>The resolved boolean value.</returns>
    public static bool ResolveBool(string envVarName, bool? yamlValue, bool defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(EnvPrefix + envVarName);
        if (!string.IsNullOrEmpty(envValue))
            return ParseBool(envValue, defaultValue);

        return yamlValue ?? defaultValue;
    }

    /// <summary>
    ///     Resolves a string value: env var > yaml config > MokaDefaults.
    /// </summary>
    /// <param name="envVarName">The environment variable suffix (without MOKADOCS_ prefix).</param>
    /// <param name="yamlValue">The value from the user's YAML config, or null if not specified.</param>
    /// <param name="defaultValue">The default value from MokaDefaults.</param>
    /// <returns>The resolved string value.</returns>
    public static string ResolveString(string envVarName, string? yamlValue, string defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(EnvPrefix + envVarName);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        return yamlValue ?? defaultValue;
    }

    /// <summary>
    ///     Resolves an int value: env var > yaml config > MokaDefaults.
    /// </summary>
    /// <param name="envVarName">The environment variable suffix (without MOKADOCS_ prefix).</param>
    /// <param name="yamlValue">The value from the user's YAML config, or null if not specified.</param>
    /// <param name="defaultValue">The default value from MokaDefaults.</param>
    /// <returns>The resolved int value.</returns>
    public static int ResolveInt(string envVarName, int? yamlValue, int defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(EnvPrefix + envVarName);
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var parsed))
            return parsed;

        return yamlValue ?? defaultValue;
    }

    private static bool ParseBool(string value, bool fallback)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => fallback
        };
    }

    #endregion
}