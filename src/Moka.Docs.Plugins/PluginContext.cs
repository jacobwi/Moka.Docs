using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;

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

	/// <summary>The id of the plugin this context belongs to, used as the diagnostic source.</summary>
	internal string? PluginId { get; init; }

	/// <summary>
	///     The running build's diagnostics while the plugin executes, otherwise <c>null</c>.
	///     Set by <see cref="PluginHost" />.
	/// </summary>
	internal DiagnosticBag? BuildDiagnostics { get; set; }

	/// <inheritdoc />
	public void LogInfo(string message) => _logger.LogInformation("[Plugin] {Message}", message);

	/// <inheritdoc />
	/// <remarks>
	///     During a build the warning is also recorded as a build diagnostic. Logging alone hid
	///     it: build and serve print no log output without --verbose, so a missing OpenAPI spec
	///     or a failed preview-host publish ended in a clean build summary.
	/// </remarks>
	public void LogWarning(string message)
	{
		_logger.LogWarning("[Plugin] {Message}", message);
		BuildDiagnostics?.Warning(message, PluginId);
	}

	/// <inheritdoc />
	/// <remarks>During a build the error is also recorded as a build diagnostic.</remarks>
	public void LogError(string message)
	{
		_logger.LogError("[Plugin] {Message}", message);
		BuildDiagnostics?.Error(message, PluginId);
	}
}
