using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Versioning;

/// <summary>
///     Manages version definitions parsed from <see cref="SiteConfig" /> and provides
///     helpers for resolving the default version and computing versioned output paths.
/// </summary>
public sealed class VersionManager
{
    private readonly VersioningFeatureConfig _config;
    private readonly ILogger<VersionManager> _logger;
    private readonly List<DocVersion> _versions;

    /// <summary>
    ///     Creates a new <see cref="VersionManager" /> from the site configuration.
    /// </summary>
    /// <param name="siteConfig">The site configuration containing versioning settings.</param>
    /// <param name="logger">Logger instance.</param>
    public VersionManager(SiteConfig siteConfig, ILogger<VersionManager> logger)
    {
        _config = siteConfig.Features.Versioning;
        _logger = logger;
        _versions = BuildVersionList(_config);

        _logger.LogDebug("VersionManager initialized — enabled={Enabled}, versions={Count}",
            IsEnabled, _versions.Count);
    }

    /// <summary>
    ///     Whether versioning is enabled in the site configuration.
    /// </summary>
    public bool IsEnabled => _config.Enabled;

    /// <summary>
    ///     The versioning strategy configured for the site (e.g., "directory" or "dropdown-only").
    /// </summary>
    public string Strategy => _config.Strategy;

    /// <summary>
    ///     All configured versions, mapped to <see cref="DocVersion" /> records.
    ///     Returns an empty list when versioning is disabled.
    /// </summary>
    public IReadOnlyList<DocVersion> Versions => _versions;

    /// <summary>
    ///     The default version, or <c>null</c> when versioning is disabled or no versions are defined.
    ///     If no version is explicitly marked as default, the first non-prerelease version is used.
    /// </summary>
    public DocVersion? DefaultVersion => _versions.FirstOrDefault(v => v.IsDefault)
                                         ?? _versions.FirstOrDefault(v => !v.IsPrerelease)
                                         ?? _versions.FirstOrDefault();

    /// <summary>
    ///     Gets the output subdirectory path for a given version relative to the base output directory.
    ///     The default version outputs to the root; other versions output to a subdirectory named after their slug.
    /// </summary>
    /// <param name="version">The version to compute the output path for.</param>
    /// <param name="baseOutputPath">The base output directory (e.g., "./_site").</param>
    /// <returns>The resolved output directory path for this version.</returns>
    public string GetOutputPath(DocVersion version, string baseOutputPath)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseOutputPath);

        // Default version lives at the root output; others get a subdirectory.
        if (version.IsDefault) return baseOutputPath;

        return Path.Combine(baseOutputPath, version.Slug);
    }

    /// <summary>
    ///     Resolves a version by its label. Returns <c>null</c> if not found.
    /// </summary>
    /// <param name="label">The version label to look up (e.g., "v2.0").</param>
    /// <returns>The matching <see cref="DocVersion" />, or <c>null</c>.</returns>
    public DocVersion? FindByLabel(string label)
    {
        return _versions.FirstOrDefault(v =>
            string.Equals(v.Label, label, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Looks up the git branch associated with a version label, if configured.
    /// </summary>
    /// <param name="version">The version to look up.</param>
    /// <returns>The branch name, or <c>null</c> if not configured.</returns>
    public string? GetBranch(DocVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var definition = _config.Versions.FirstOrDefault(v =>
            string.Equals(v.Label, version.Label, StringComparison.OrdinalIgnoreCase));

        return definition?.Branch;
    }

    private static List<DocVersion> BuildVersionList(VersioningFeatureConfig config)
    {
        if (!config.Enabled || config.Versions.Count == 0) return [];

        return config.Versions.Select(v => new DocVersion
        {
            Label = v.Label,
            Slug = GenerateSlug(v.Label),
            IsDefault = v.Default,
            IsPrerelease = v.Prerelease
        }).ToList();
    }

    /// <summary>
    ///     Generates a URL-safe slug from a version label.
    /// </summary>
    private static string GenerateSlug(string label)
    {
        // Simple slug: lowercase, replace spaces with hyphens, strip non-alphanumeric except dots and hyphens
        var slug = label.Trim().ToLowerInvariant().Replace(' ', '-');
        return new string(slug.Where(c => char.IsLetterOrDigit(c) || c is '-' or '.').ToArray());
    }
}