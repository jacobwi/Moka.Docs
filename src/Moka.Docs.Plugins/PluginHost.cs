using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins;

/// <summary>
///     Discovers, loads, and manages the lifecycle of <see cref="IMokaPlugin" /> instances.
///     Plugins are resolved from the DI container and matched against declarations
///     in the site configuration.
/// </summary>
public sealed class PluginHost
{
	private readonly List<LoadedPlugin> _loaded = [];
	private readonly ILogger<PluginHost> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly SiteConfig _siteConfig;

	/// <summary>
	///     Creates a new plugin host.
	/// </summary>
	/// <param name="siteConfig">The site configuration containing plugin declarations.</param>
	/// <param name="serviceProvider">The host service provider.</param>
	/// <param name="logger">Logger instance.</param>
	public PluginHost(SiteConfig siteConfig, IServiceProvider serviceProvider, ILogger<PluginHost> logger)
	{
		_siteConfig = siteConfig;
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	/// <summary>
	///     The currently loaded plugins.
	/// </summary>
	public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loaded;

	/// <summary>
	///     Discovers and initializes all plugins declared in the site configuration.
	///     Plugins registered in the DI container via <see cref="IMokaPlugin" /> are matched
	///     by their <see cref="IMokaPlugin.Id" /> against <see cref="PluginDeclaration.Name" />.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	public async Task DiscoverAndInitializeAsync(CancellationToken ct = default)
	{
		List<PluginDeclaration> declarations = _siteConfig.Plugins;
		if (declarations.Count == 0)
		{
			_logger.LogDebug("No plugins declared in configuration");
			return;
		}

		// Resolve all registered IMokaPlugin instances from DI
		IEnumerable<IMokaPlugin> available = _serviceProvider.GetService(typeof(IEnumerable<IMokaPlugin>))
			as IEnumerable<IMokaPlugin> ?? [];

		var pluginMap = new Dictionary<string, IMokaPlugin>(StringComparer.OrdinalIgnoreCase);
		foreach (IMokaPlugin plugin in available)
		{
			pluginMap[plugin.Id] = plugin;
		}

		foreach (PluginDeclaration declaration in declarations)
		{
			if (string.IsNullOrWhiteSpace(declaration.Name))
			{
				_logger.LogWarning("Skipping plugin declaration with no name");
				continue;
			}

			if (!pluginMap.TryGetValue(declaration.Name, out IMokaPlugin? plugin))
			{
				_logger.LogWarning("Plugin '{PluginName}' declared in config but not found in DI container",
					declaration.Name);
				continue;
			}

			ILogger pluginLogger = _serviceProvider.GetService(typeof(ILogger<PluginHost>)) as ILogger
			                       ?? _logger;

			var context = new PluginContext(
				_siteConfig,
				declaration.Options.AsReadOnly(),
				_serviceProvider,
				pluginLogger);

			try
			{
				await plugin.InitializeAsync(context, ct);
				_loaded.Add(new LoadedPlugin(plugin, context, declaration));
				_logger.LogInformation("Loaded plugin '{PluginName}' v{Version}", plugin.Name, plugin.Version);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to initialize plugin '{PluginName}'", plugin.Name);
			}
		}
	}

	/// <summary>
	///     Executes all loaded plugins against the given build context.
	/// </summary>
	/// <param name="buildContext">The current build context.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task ExecuteAllAsync(BuildContext buildContext, CancellationToken ct = default)
	{
		foreach (LoadedPlugin loaded in _loaded)
		{
			try
			{
				_logger.LogDebug("Executing plugin '{PluginName}'", loaded.Plugin.Name);
				await loaded.Plugin.ExecuteAsync(loaded.Context, buildContext, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Plugin '{PluginName}' failed during execution", loaded.Plugin.Name);
			}
		}
	}
}

/// <summary>
///     Represents a successfully loaded and initialized plugin.
/// </summary>
/// <param name="Plugin">The plugin instance.</param>
/// <param name="Context">The plugin's context.</param>
/// <param name="Declaration">The original configuration declaration.</param>
public sealed record LoadedPlugin(IMokaPlugin Plugin, PluginContext Context, PluginDeclaration Declaration);
