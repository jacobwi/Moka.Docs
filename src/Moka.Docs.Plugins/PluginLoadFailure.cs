namespace Moka.Docs.Plugins;

/// <summary>
///     A <c>plugins:</c> entry with a <c>path</c> whose plugin could not be loaded.
/// </summary>
/// <param name="Name">The declared <c>name</c>.</param>
/// <param name="Path">The declared <c>path</c>, as written in the configuration.</param>
/// <param name="Reason">What went wrong.</param>
public sealed record PluginLoadFailure(string Name, string Path, string Reason)
{
	/// <summary>The message the build reports as an error, naming the plugin and its path.</summary>
	public string Message => $"Plugin '{Name}' from '{Path}': {Reason}";
}
