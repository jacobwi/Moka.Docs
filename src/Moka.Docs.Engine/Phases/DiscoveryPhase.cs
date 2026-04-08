using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Discovery;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Scans for Markdown files, C# projects, and static assets.
/// </summary>
public sealed class DiscoveryPhase(
	FileDiscoveryService discoveryService,
	BrandAssetResolver brandAssetResolver,
	ILogger<DiscoveryPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "Discovery";

	/// <inheritdoc />
	public int Order => 200;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		DiscoveryResult result = discoveryService.Discover(context.RootDirectory, context.Config, context.FileSystem);

		context.DiscoveredMarkdownFiles.AddRange(result.MarkdownFiles);
		context.DiscoveredProjectFiles.AddRange(result.ProjectFiles);
		context.DiscoveredAssetFiles.AddRange(result.AssetFiles);

		// Resolve brand assets (site.logo, site.favicon). May discover files outside
		// content.docs via filesystem lookup, so this runs AFTER the normal glob.
		brandAssetResolver.Resolve(context);

		logger.LogInformation(
			"Discovered {Md} markdown, {Proj} projects, {Assets} assets, {Brand} brand assets",
			result.MarkdownFiles.Count, result.ProjectFiles.Count, result.AssetFiles.Count,
			context.BrandAssetFiles.Count);

		return Task.CompletedTask;
	}
}
