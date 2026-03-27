using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Plugins;

/// <summary>
///     Default implementation of <see cref="IPluginContext" /> that wraps the host
///     service provider and plugin-specific options from configuration.
/// </summary>
public sealed class PluginContext : IPluginContext
{
	private readonly ILogger _logger;
	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	///     Creates a new plugin context.
	/// </summary>
	/// <param name="siteConfig">The site configuration.</param>
	/// <param name="options">Plugin-specific options from configuration.</param>
	/// <param name="serviceProvider">The host service provider for resolving services.</param>
	/// <param name="logger">Logger scoped to the plugin.</param>
	public PluginContext(
		SiteConfig siteConfig,
		IReadOnlyDictionary<string, object> options,
		IServiceProvider serviceProvider,
		ILogger logger)
	{
		SiteConfig = siteConfig;
		Options = options;
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	/// <inheritdoc />
	public SiteConfig SiteConfig { get; }

	/// <inheritdoc />
	public IReadOnlyDictionary<string, object> Options { get; }

	/// <inheritdoc />
	public T? GetService<T>() where T : class => _serviceProvider.GetService(typeof(T)) as T;

	/// <inheritdoc />
	public void LogInfo(string message) => _logger.LogInformation("[Plugin] {Message}", message);

	/// <inheritdoc />
	public void LogWarning(string message) => _logger.LogWarning("[Plugin] {Message}", message);

	/// <inheritdoc />
	public void LogError(string message) => _logger.LogError("[Plugin] {Message}", message);
}
