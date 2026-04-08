using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moka.Docs.Core.Features;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Discovery;
using Moka.Docs.Engine.Phases;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

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

		// Template engine and default theme
		services.AddSingleton<ScribanTemplateEngine>();
		services.AddSingleton(_ => EmbeddedThemeProvider.CreateDefault());
		services.AddSingleton<ThemeLoader>();

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
