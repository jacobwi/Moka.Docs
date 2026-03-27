// MokaDocs — Plugin contract and context interfaces

using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins;

/// <summary>
///     Contract that all MokaDocs plugins must implement.
///     Plugins are discovered and loaded by the <see cref="PluginHost" />.
/// </summary>
public interface IMokaPlugin
{
	/// <summary>
	///     Unique identifier for this plugin (e.g., "mokadocs-mermaid").
	/// </summary>
	string Id { get; }

	/// <summary>
	///     Human-readable display name.
	/// </summary>
	string Name { get; }

	/// <summary>
	///     Semantic version of this plugin.
	/// </summary>
	string Version { get; }

	/// <summary>
	///     Called once when the plugin is loaded. Use this to register services,
	///     subscribe to events, or validate configuration.
	/// </summary>
	/// <param name="context">The plugin context providing access to site services.</param>
	/// <param name="ct">Cancellation token.</param>
	Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

	/// <summary>
	///     Called during the build pipeline, giving the plugin a chance to
	///     modify the build context (add pages, transform content, etc.).
	/// </summary>
	/// <param name="context">The plugin context.</param>
	/// <param name="buildContext">The current build context.</param>
	/// <param name="ct">Cancellation token.</param>
	Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default);
}

/// <summary>
///     Provides plugins with access to host services, configuration, and logging.
/// </summary>
public interface IPluginContext
{
	/// <summary>
	///     The current site configuration.
	/// </summary>
	SiteConfig SiteConfig { get; }

	/// <summary>
	///     Plugin-specific options from the <c>plugins[].options</c> section of mokadocs.yaml.
	/// </summary>
	IReadOnlyDictionary<string, object> Options { get; }

	/// <summary>
	///     Resolves a service from the host DI container.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>The resolved service, or <c>null</c> if not registered.</returns>
	T? GetService<T>() where T : class;

	/// <summary>
	///     Logs an informational message from the plugin.
	/// </summary>
	/// <param name="message">The log message.</param>
	void LogInfo(string message);

	/// <summary>
	///     Logs a warning message from the plugin.
	/// </summary>
	/// <param name="message">The warning message.</param>
	void LogWarning(string message);

	/// <summary>
	///     Logs an error message from the plugin.
	/// </summary>
	/// <param name="message">The error message.</param>
	void LogError(string message);
}
