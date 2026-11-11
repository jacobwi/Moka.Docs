using Moka.Docs.Plugins.BlazorPreview;
using Moka.Docs.Plugins.Changelog;
using Moka.Docs.Plugins.OpenApi;
using Moka.Docs.Plugins.PythonApi;
using Moka.Docs.Plugins.Repl;

namespace Moka.Docs.Plugins;

/// <summary>
///     The plugins that ship with MokaDocs, by the id a site declares them with.
/// </summary>
public static class BuiltInPlugins
{
	private static readonly (string Id, Type Type)[] _plugins =
	[
		("openapi", typeof(OpenApiPlugin)),
		("mokadocs-repl", typeof(ReplPlugin)),
		("mokadocs-blazor-preview", typeof(BlazorPreviewPlugin)),
		("mokadocs-changelog", typeof(ChangelogPlugin)),
		("mokadocs-python-api", typeof(PythonApiPlugin))
	];

	/// <summary>
	///     The ids of every built-in plugin, the names to declare under <c>plugins:</c> in mokadocs.yaml.
	/// </summary>
	public static IReadOnlyList<string> Ids { get; } = [.. _plugins.Select(p => p.Id)];

	/// <summary>
	///     The class of the built-in plugin with this id, matched without regard to case, or
	///     <c>null</c> when no built-in has it.
	/// </summary>
	internal static Type? Find(string id) =>
		_plugins.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)).Type;
}
