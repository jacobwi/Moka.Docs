using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moka.Docs.Plugins;

/// <summary>
///     Extension methods for registering plugin services.
/// </summary>
public static class PluginServiceExtensions
{
	/// <summary>
	///     Adds the <see cref="PluginHost" /> and every built-in plugin. The host runs the plugins
	///     the site declares under <c>plugins:</c>. Loading plugins from <c>plugins[].path</c> stays
	///     off; the other overload turns it on with <see cref="MokaPluginOptions.PluginBaseDirectory" />.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaDocsPlugins(this IServiceCollection services) =>
		services.AddMokaDocsPlugins(_ => { });

	/// <summary>
	///     Adds the <see cref="PluginHost" /> and the built-in plugins that
	///     <see cref="MokaPluginOptions.BuiltInPlugins" /> names, all of them when it is <c>null</c>.
	///     The mokadocs CLI and the ASP.NET Core host both register plugins through this method.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Sets the options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaDocsPlugins(this IServiceCollection services,
		Action<MokaPluginOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);
		var options = new MokaPluginOptions();
		configure(options);

		services.TryAddSingleton(options);
		services.TryAddSingleton<PluginHost>();

		foreach (string id in options.BuiltInPlugins ?? BuiltInPlugins.Ids)
		{
			// An id that is no built-in is left alone: it can be the application's own plugin.
			if (BuiltInPlugins.Find(id) is { } type)
			{
				services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IMokaPlugin), type));
			}
		}

		return services;
	}
}
