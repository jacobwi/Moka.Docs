using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moka.Docs.Core.Features;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Caching;
using Moka.Docs.Engine.Discovery;
using Moka.Docs.Engine.Phases;
using Moka.Docs.Rendering;
using Moka.Docs.Themes;

namespace Moka.Docs.Engine;

/// <summary>
///     Extension methods for registering build engine services.
/// </summary>
public static class EngineServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs build engine services and all pipeline phases.
	/// </summary>
	public static IServiceCollection AddMokaDocsEngine(this IServiceCollection services)
	{
		// Register feature management with MokaDefaults + env var overrides
		IConfigurationRoot featureConfig = new ConfigurationBuilder()
			.AddInMemoryCollection(
				MokaFeatureConfiguration.GetDefaults()
					.Select(kvp =>
						new KeyValuePair<string, string?>($"FeatureManagement:{kvp.Key}", kvp.Value.ToString())))
			.AddEnvironmentVariables("MOKADOCS_")
			.Build();
		services.AddSingleton<IConfiguration>(featureConfig);
		services.AddFeatureManagement(featureConfig.GetSection("FeatureManagement"));

		services.AddSingleton<BuildPipeline>();
		services.AddSingleton<FileDiscoveryService>();
		services.AddSingleton<BrandAssetResolver>();
		services.AddSingleton<BuildCache>();

		// Template engine and theme resolution. ThemeResolver picks between the embedded
		// default and a theme directory named by theme.name in mokadocs.yaml; it needs the
		// build's root directory and file system, so the choice is made per build rather
		// than baked into a DI registration.
		services.AddMokaDocsRendering();
		services.AddMokaDocsThemes();

		// Register build phases
		services.AddSingleton<IBuildPhase, DiscoveryPhase>();
		services.AddSingleton<IBuildPhase, CSharpAnalysisPhase>();
		services.AddSingleton<IBuildPhase, MarkdownParsePhase>();
		services.AddSingleton<IBuildPhase, FeatureGatePhase>();
		services.AddSingleton<IBuildPhase, NavigationBuildPhase>();
		services.AddSingleton<IBuildPhase, SearchIndexPhase>();
		services.AddSingleton<IBuildPhase, RenderPhase>();
		services.AddSingleton<IBuildPhase, ThemeAssetPhase>();
		services.AddSingleton<IBuildPhase, OutputPhase>();
		services.AddSingleton<IBuildPhase, PostProcessPhase>();

		return services;
	}
}
