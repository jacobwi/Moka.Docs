namespace Moka.Docs.Plugins;

/// <summary>
///     Settings for <see cref="PluginServiceExtensions.AddMokaDocsPlugins(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{MokaPluginOptions})" />.
/// </summary>
public sealed class MokaPluginOptions
{
	/// <summary>
	///     The ids of the built-in plugins to register. <c>null</c>, the default, registers all of
	///     them. Either way the plugin host runs only the plugins the site declares.
	/// </summary>
	public IEnumerable<string>? BuiltInPlugins { get; set; }

	/// <summary>
	///     The folder that relative <c>plugins[].path</c> values resolve from, normally the folder
	///     holding mokadocs.yaml. <c>null</c>, the default, turns loading plugins from a path off:
	///     a declaration with a path is then reported as an error.
	/// </summary>
	public string? PluginBaseDirectory { get; set; }

	/// <summary>
	///     How long building a plugin project named by <c>plugins[].path</c> may take, restore
	///     included, before it is stopped. Ten minutes by default.
	/// </summary>
	public TimeSpan ProjectBuildTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
