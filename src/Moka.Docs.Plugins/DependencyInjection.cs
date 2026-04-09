using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Plugins.PythonApi;

namespace Moka.Docs.Plugins;

/// <summary>
///     Extension methods for registering plugin services.
/// </summary>
public static class PluginServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs plugin infrastructure to the service collection.
	///     Registers the <see cref="PluginHost" /> which discovers and manages
	///     <see cref="IMokaPlugin" /> instances at build time.
	/// </summary>
	public static IServiceCollection AddMokaDocsPlugins(this IServiceCollection services)
	{
		services.AddSingleton<PluginHost>();

		// Register built-in plugins. Each is discoverable by the PluginHost via
		// its Id property when the user adds the corresponding plugin declaration
		// to their mokadocs.yaml.
		services.AddSingleton<IMokaPlugin, PythonApiPlugin>();

		return services;
	}
}
