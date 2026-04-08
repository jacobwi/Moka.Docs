using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine.Discovery;

/// <summary>
///     Populates <see cref="BuildContext.BrandAssetFiles" /> with the resolved
///     <see cref="SiteAssetReference" /> entries for <c>site.logo</c> and
///     <c>site.favicon</c>, so the output phase knows to copy them into the built site
///     even when they live outside the <c>content.docs</c> directory tree.
///     <para>
///         Invoked from <c>DiscoveryPhase</c> after the normal markdown/asset glob.
///         Logs warnings for missing source files but does not throw — a missing logo
///         should not break a docs build, just fall back to the default SVG logo in
///         the theme.
///     </para>
/// </summary>
public sealed class BrandAssetResolver(IFileSystem fileSystem, ILogger<BrandAssetResolver> logger)
{
	/// <summary>
	///     Resolves <see cref="SiteConfig.Site" />'s brand assets into
	///     <see cref="BuildContext.BrandAssetFiles" />. Safe to call multiple times:
	///     existing entries with the same publish URL are left alone.
	/// </summary>
	public void Resolve(BuildContext context)
	{
		SiteMetadata site = context.Config.Site;
		AddIfPresent(context, site.Logo, "site.logo");
		AddIfPresent(context, site.Favicon, "site.favicon");
	}

	private void AddIfPresent(BuildContext context, SiteAssetReference? asset, string label)
	{
		if (asset is null || !asset.ShouldCopy)
		{
			return;
		}

		string sourcePath = asset.SourcePath!;
		if (!fileSystem.File.Exists(sourcePath))
		{
			logger.LogWarning("{Label} file not found: {Path}", label, sourcePath);
			return;
		}

		// Use the publish URL (leading "/") as the key and store the absolute source path.
		// If logo and favicon both resolve to the same publish URL, the SiteConfigReader
		// already threw a SiteConfigException during parsing — so we can safely overwrite
		// here (it means the same asset was referenced twice with identical source paths,
		// which is fine).
		context.BrandAssetFiles[asset.PublishUrl] = sourcePath;

		logger.LogInformation("Resolved {Label}: {Source} → {PublishUrl}",
			label, sourcePath, asset.PublishUrl);
	}
}
