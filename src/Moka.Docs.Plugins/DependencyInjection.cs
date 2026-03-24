using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}