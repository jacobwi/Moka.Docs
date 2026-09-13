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
///         A missing source file gets a warning diagnostic but does not throw - a missing
///         logo should not break a docs build. The asset stays out of
///         <see cref="BuildContext.BrandAssetFiles" />, which is how the template engine
///         knows to fall back to the theme's default logo and to write no favicon link.
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
		AddIfPresent(context, site.Logo, "site.logo", "the header shows the theme's default logo instead");
		AddIfPresent(context, site.Favicon, "site.favicon", "pages get no favicon link");
	}

	private void AddIfPresent(BuildContext context, SiteAssetReference? asset, string label, string consequence)
	{
		if (asset is null || !asset.ShouldCopy)
		{
			return;
		}

		string sourcePath = asset.SourcePath!;
		if (!fileSystem.File.Exists(sourcePath))
		{
			// This only reached the logger, which a normal build doesn't print, while the theme
			// kept emitting an <img> for a file that was never copied.
			context.Diagnostics.Warning($"{label} file not found: {sourcePath}; {consequence}", "Discovery");
			return;
		}

		// Use the publish URL (leading "/") as the key and store the absolute source path.
		// If logo and favicon both resolve to the same publish URL, the SiteConfigReader
		// already threw a SiteConfigException during parsing - so we can safely overwrite
		// here (it means the same asset was referenced twice with identical source paths,
		// which is fine).
		context.BrandAssetFiles[asset.PublishUrl] = sourcePath;

		logger.LogInformation("Resolved {Label}: {Source} → {PublishUrl}",
			label, sourcePath, asset.PublishUrl);
	}
}
