using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins;

/// <summary>
///     Discovers, loads, and manages the lifecycle of <see cref="IMokaPlugin" /> instances.
///     Plugins are resolved from the DI container, or loaded from the assembly a declaration's
///     <c>path</c> names, and matched against the declarations in the site configuration.
/// </summary>
public sealed class PluginHost : IDisposable, IAsyncDisposable
{
	private readonly List<IMokaPlugin> _created = [];
	private readonly List<PluginLoadFailure> _failures = [];
	private readonly List<LoadedPlugin> _loaded = [];
	private readonly ILogger<PluginHost> _logger;
	private readonly MokaPluginOptions _options;
	private readonly IServiceProvider _serviceProvider;
	private readonly SiteConfig _siteConfig;
	private bool _initialized;

	/// <summary>
	///     Creates a new plugin host that doesn't load plugins from a path.
	/// </summary>
	/// <param name="siteConfig">The site configuration containing plugin declarations.</param>
	/// <param name="serviceProvider">The host service provider.</param>
	/// <param name="logger">Logger instance.</param>
	public PluginHost(SiteConfig siteConfig, IServiceProvider serviceProvider, ILogger<PluginHost> logger)
		: this(siteConfig, serviceProvider, logger, new MokaPluginOptions())
	{
	}

	/// <summary>
	///     Creates a new plugin host.
	/// </summary>
	/// <param name="siteConfig">The site configuration containing plugin declarations.</param>
	/// <param name="serviceProvider">The host service provider.</param>
	/// <param name="logger">Logger instance.</param>
	/// <param name="options">
	///     The plugin options. <see cref="MokaPluginOptions.PluginBaseDirectory" /> turns on loading
	///     plugins from <c>plugins[].path</c>.
	/// </param>
	public PluginHost(SiteConfig siteConfig, IServiceProvider serviceProvider, ILogger<PluginHost> logger,
		MokaPluginOptions options)
	{
		_siteConfig = siteConfig;
		_serviceProvider = serviceProvider;
		_logger = logger;
		_options = options;
	}

	/// <summary>
	///     The currently loaded plugins.
	/// </summary>
	public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loaded;

	/// <summary>
	///     The declarations with a <c>path</c> whose plugin could not be loaded. Each build reports
	///     them as errors.
	/// </summary>
	public IReadOnlyList<PluginLoadFailure> LoadFailures => _failures;

	/// <summary>Disposes the plugins this host created from plugin assemblies.</summary>
	public void Dispose()
	{
		foreach (IMokaPlugin plugin in _created)
		{
			(plugin as IDisposable)?.Dispose();
		}

		_created.Clear();
	}

	/// <summary>Disposes the plugins this host created from plugin assemblies.</summary>
	public async ValueTask DisposeAsync()
	{
		foreach (IMokaPlugin plugin in _created)
		{
			if (plugin is IAsyncDisposable asyncDisposable)
			{
				await asyncDisposable.DisposeAsync();
			}
			else
			{
				(plugin as IDisposable)?.Dispose();
			}
		}

		_created.Clear();
	}

	/// <summary>
	///     Discovers and initializes all plugins declared in the site configuration.
	///     Plugins registered in the DI container via <see cref="IMokaPlugin" /> are matched
	///     by their <see cref="IMokaPlugin.Id" /> against <see cref="PluginDeclaration.Name" />.
	///     A declaration with a <see cref="PluginDeclaration.Path" /> takes the plugin with that id
	///     from the assembly the path names instead. Only the first call does anything.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	public async Task DiscoverAndInitializeAsync(CancellationToken ct = default)
	{
		// The ASP.NET Core host calls this before every in-memory build. Each call used to add
		// the plugins again, so they ran once more per rebuild and injected their assets twice.
		if (_initialized)
		{
			return;
		}

		_initialized = true;

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

		var assemblies = new Dictionary<string, AssemblyPlugins>(
			OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		foreach (PluginDeclaration declaration in declarations)
		{
			if (string.IsNullOrWhiteSpace(declaration.Name))
			{
				_logger.LogWarning("Skipping plugin declaration with no name");
				continue;
			}

			IMokaPlugin? plugin;
			if (!string.IsNullOrWhiteSpace(declaration.Path))
			{
				plugin = await FindInAssemblyAsync(declaration, assemblies, ct);
				if (plugin is null)
				{
					continue;
				}
			}
			else if (!pluginMap.TryGetValue(declaration.Name, out plugin))
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
				pluginLogger) { PluginId = plugin.Id };

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
	///     Executes all loaded plugins against the given build context. Declarations whose plugin
	///     could not be loaded from its path are reported first, as errors.
	/// </summary>
	/// <param name="buildContext">The current build context.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task ExecuteAllAsync(BuildContext buildContext, CancellationToken ct = default)
	{
		// An error, not a warning: the pages or content the plugin adds are missing from the site.
		foreach (PluginLoadFailure failure in _failures)
		{
			buildContext.Diagnostics.Error(failure.Message, "Plugins");
		}

		foreach (LoadedPlugin loaded in _loaded)
		{
			try
			{
				_logger.LogDebug("Executing plugin '{PluginName}'", loaded.Plugin.Name);
				loaded.Context.BuildDiagnostics = buildContext.Diagnostics;
				await loaded.Plugin.ExecuteAsync(loaded.Context, buildContext, ct);
			}
			catch (Exception ex)
			{
				// Also record it on the build: a crashed plugin means missing pages, and
				// without this the only trace was a log line nobody sees in a quiet build.
				buildContext.Diagnostics.Error(
					$"Plugin '{loaded.Plugin.Id}' failed: {ex.Message}", "Plugins");
				_logger.LogError(ex, "Plugin '{PluginName}' failed during execution", loaded.Plugin.Name);
			}
			finally
			{
				loaded.Context.BuildDiagnostics = null;
			}
		}
	}

	/// <summary>
	///     The plugin with the declared id from the assembly the declaration's path names, or
	///     <c>null</c> after recording why there is none. Each path is loaded once, so several
	///     declarations naming one assembly share its plugin instances.
	/// </summary>
	private async Task<IMokaPlugin?> FindInAssemblyAsync(PluginDeclaration declaration,
		Dictionary<string, AssemblyPlugins> assemblies, CancellationToken ct)
	{
		string name = declaration.Name!;
		string path = declaration.Path!;
		if (_options.PluginBaseDirectory is not { } baseDirectory)
		{
			Fail(declaration, "this host doesn't load plugins from a path. Register the plugin as an IMokaPlugin service");
			return null;
		}

		string fullPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
		if (!assemblies.TryGetValue(fullPath, out AssemblyPlugins? loaded))
		{
			_logger.LogInformation("Loading plugins from {PluginPath}", fullPath);
			PluginAssemblyResult result = await PluginAssemblyLoader.LoadAsync(fullPath, _options.ProjectBuildTimeout, ct);
			loaded = result.Error is { } error ? new AssemblyPlugins([], [error]) : Create(result.PluginTypes);
			assemblies[fullPath] = loaded;
		}

		IMokaPlugin? match = loaded.Plugins
			.FirstOrDefault(p => string.Equals(p.Id, name, StringComparison.OrdinalIgnoreCase)).Plugin;
		if (match is not null)
		{
			return match;
		}

		if (loaded.Plugins.Count == 0)
		{
			Fail(declaration, string.Join("; ", loaded.Errors));
			return null;
		}

		string found = string.Join(", ", loaded.Plugins.Select(p => $"'{p.Id}'"));
		string notCreated = loaded.Errors.Count == 0 ? "" : $" ({string.Join("; ", loaded.Errors)})";
		Fail(declaration, $"it has no plugin with the id '{name}', only {found}{notCreated}");
		return null;
	}

	/// <summary>Creates an instance of each plugin class, with constructor arguments from the host's services.</summary>
	private AssemblyPlugins Create(IReadOnlyList<Type> pluginTypes)
	{
		var plugins = new List<(string Id, IMokaPlugin Plugin)>();
		var errors = new List<string>();
		foreach (Type type in pluginTypes)
		{
			try
			{
				var plugin = (IMokaPlugin)ActivatorUtilities.CreateInstance(_serviceProvider, type);
				_created.Add(plugin);
				plugins.Add((plugin.Id, plugin));
			}
			catch (Exception ex)
			{
				Exception cause = ex is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : ex;
				errors.Add($"couldn't create {type.FullName}: {cause.Message}");
			}
		}

		return new AssemblyPlugins(plugins, errors);
	}

	private void Fail(PluginDeclaration declaration, string reason)
	{
		var failure = new PluginLoadFailure(declaration.Name!, declaration.Path!, reason);
		_failures.Add(failure);
		_logger.LogError("{Message}", failure.Message);
	}

	private sealed record AssemblyPlugins(IReadOnlyList<(string Id, IMokaPlugin Plugin)> Plugins,
		IReadOnlyList<string> Errors);
}

/// <summary>
///     Represents a successfully loaded and initialized plugin.
/// </summary>
/// <param name="Plugin">The plugin instance.</param>
/// <param name="Context">The plugin's context.</param>
/// <param name="Declaration">The original configuration declaration.</param>
public sealed record LoadedPlugin(IMokaPlugin Plugin, PluginContext Context, PluginDeclaration Declaration);
