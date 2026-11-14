using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Engine;
using Moka.Docs.Plugins;
using Moka.Docs.Versioning;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     The configuration and the service container a build runs with. <c>build --watch</c> and
///     <c>serve</c> keep one for their whole run and reload it when mokadocs.yaml changes, so an
///     edit to the theme, the navigation or the plugins shows up without a restart.
/// </summary>
/// <remarks>
///     A reload builds a new container, which brings new plugins, a new theme resolver and a new
///     template cache, then disposes the old one. Settings the running command was set up with
///     (the output and docs folders, and under <c>serve</c> the base path) keep their first
///     value until a restart.
/// </remarks>
internal sealed class BuildSession : IAsyncDisposable
{
	private readonly ConfigSource _source;
	private readonly bool _verbose;
	private string _configText;

	private BuildSession(ConfigSource source, bool verbose, string configText, SiteConfig config,
		ServiceProvider services, BuildPipeline pipeline, PluginHost plugins)
	{
		_source = source;
		_verbose = verbose;
		_configText = configText;
		Config = config;
		Services = services;
		Pipeline = pipeline;
		Plugins = plugins;
	}

	/// <summary>The configuration in use, with the command-line overrides applied.</summary>
	public SiteConfig Config { get; private set; }

	/// <summary>The service container built for <see cref="Config" />.</summary>
	public ServiceProvider Services { get; private set; }

	/// <summary>The pipeline, with the plugins of <see cref="Config" /> attached.</summary>
	public BuildPipeline Pipeline { get; private set; }

	/// <summary>The plugin host of <see cref="Services" />.</summary>
	public PluginHost Plugins { get; private set; }

	/// <summary>The version list of <see cref="Config" />.</summary>
	public VersionManager VersionManager => Services.GetRequiredService<VersionManager>();

	/// <summary>The folder holding the configuration file, which every relative path in it starts from.</summary>
	public string RootDirectory => _source.RootDirectory;

	/// <inheritdoc />
	public ValueTask DisposeAsync() => Services.DisposeAsync();

	/// <summary>
	///     Reads the configuration file and applies the command-line overrides.
	/// </summary>
	/// <returns>The configuration and the text it was read from.</returns>
	/// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
	/// <exception cref="SiteConfigException">The file isn't a valid configuration.</exception>
	internal static (SiteConfig Config, string Text) Read(ConfigSource source)
	{
		if (!File.Exists(source.Path))
		{
			throw new FileNotFoundException($"Configuration file not found: {source.Path}", source.Path);
		}

		string text = File.ReadAllText(source.Path);
		return (Parse(text, source), text);
	}

	/// <summary>
	///     Builds the service container for a configuration that <see cref="Read" /> returned and
	///     initializes its plugins.
	/// </summary>
	internal static async Task<BuildSession> StartAsync(ConfigSource source, SiteConfig config, string configText,
		bool verbose, CancellationToken ct = default)
	{
		(ServiceProvider services, BuildPipeline pipeline, PluginHost plugins) =
			await CreateServicesAsync(config, verbose, source.RootDirectory, ct);
		return new BuildSession(source, verbose, configText, config, services, pipeline, plugins);
	}

	/// <summary>
	///     Reads the configuration file again and, when its text changed, switches to a new service
	///     container built from it. A file that doesn't parse leaves everything as it was.
	/// </summary>
	/// <param name="fixedBasePath">
	///     Whether <c>build.basePath</c> keeps its current value, as it must while <c>serve</c>
	///     answers under it.
	/// </param>
	/// <param name="ct">Cancels the plugin initialization.</param>
	internal async Task<ConfigReload> ReloadIfChangedAsync(bool fixedBasePath, CancellationToken ct = default)
	{
		string text;
		try
		{
			text = await File.ReadAllTextAsync(_source.Path, ct);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return ConfigReload.Failed($"Can't read {Path.GetFileName(_source.Path)}: {ex.Message}");
		}

		if (string.Equals(text, _configText, StringComparison.Ordinal))
		{
			return ConfigReload.Unchanged;
		}

		SiteConfig config;
		try
		{
			config = Parse(text, _source);
		}
		catch (SiteConfigException ex)
		{
			return ConfigReload.Failed(ex.Message);
		}

		var notes = new List<string>();
		config = KeepFixedSettings(config, Config, RootDirectory, fixedBasePath, notes);

		ServiceProvider services;
		BuildPipeline pipeline;
		PluginHost plugins;
		try
		{
			(services, pipeline, plugins) = await CreateServicesAsync(config, _verbose, RootDirectory, ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return ConfigReload.Failed($"Can't set up the build for the new configuration: {ex.Message}");
		}

		ServiceProvider previous = Services;
		(Config, Services, Pipeline, Plugins, _configText) = (config, services, pipeline, plugins, text);
		await previous.DisposeAsync();
		return new ConfigReload(ConfigReloadOutcome.Reloaded, null, notes);
	}

	/// <summary>
	///     Prints what a reload did. A failed one names the problem and says that the last good build
	///     stays in place.
	/// </summary>
	/// <param name="reload">The reload.</param>
	/// <param name="keeping">What stays in place while the configuration is broken.</param>
	internal static void Print(ConfigReload reload, string keeping)
	{
		switch (reload.Outcome)
		{
			case ConfigReloadOutcome.Reloaded:
				AnsiConsole.MarkupLine("[blue]Configuration reloaded[/]");
				break;
			case ConfigReloadOutcome.Failed:
				AnsiConsole.MarkupLine($"[red]Configuration error:[/] {Markup.Escape(reload.Error ?? "")}");
				AnsiConsole.MarkupLine($"[dim]{Markup.Escape(keeping)}. Fix the file and save it again to rebuild.[/]");
				break;
		}

		foreach (string note in reload.Notes)
		{
			AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(note)}[/]");
		}
	}

	private static SiteConfig Parse(string text, ConfigSource source)
	{
		SiteConfig config = SiteConfigReader.Parse(text, source.RootDirectory);
		if (source.Output is not null)
		{
			config = config with { Build = config.Build with { Output = source.Output } };
		}

		if (source.BasePath is not null)
		{
			config = config with
			{
				Build = config.Build with { BasePath = SiteConfigReader.NormalizeBasePath(source.BasePath) }
			};
		}

		if (source.ReadableAssets && config.Build.MinifyAssets is null)
		{
			config = config with { Build = config.Build with { MinifyAssets = false } };
		}

		return config;
	}

	/// <summary>
	///     Gives back the settings the running command was set up with their current values, and
	///     adds a note for each one the new configuration changes.
	/// </summary>
	internal static SiteConfig KeepFixedSettings(SiteConfig next, SiteConfig current, string rootDirectory,
		bool fixedBasePath, List<string> notes)
	{
		if (!SameFolder(rootDirectory, next.Content.Docs, current.Content.Docs))
		{
			notes.Add(RestartNote("content.docs", current.Content.Docs, next.Content.Docs));
			next = next with { Content = next.Content with { Docs = current.Content.Docs } };
		}

		if (!SameFolder(rootDirectory, next.Build.Output, current.Build.Output))
		{
			notes.Add(RestartNote("build.output", current.Build.Output, next.Build.Output));
			next = next with { Build = next.Build with { Output = current.Build.Output } };
		}

		if (fixedBasePath && !string.Equals(next.Build.BasePath, current.Build.BasePath, StringComparison.Ordinal))
		{
			notes.Add(RestartNote("build.basePath", current.Build.BasePath, next.Build.BasePath));
			next = next with { Build = next.Build with { BasePath = current.Build.BasePath } };
		}

		return next;
	}

	private static bool SameFolder(string rootDirectory, string a, string b) =>
		string.Equals(
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(rootDirectory, a))),
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(rootDirectory, b))),
			OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

	private static string RestartNote(string setting, string current, string next) =>
		$"{setting} changed from '{current}' to '{next}', which takes a restart. Still using '{current}'.";

	private static async Task<(ServiceProvider, BuildPipeline, PluginHost)> CreateServicesAsync(SiteConfig config,
		bool verbose, string rootDirectory, CancellationToken ct)
	{
		ServiceProvider services = BuildCommand.BuildServices(config, verbose, rootDirectory);
		try
		{
			BuildPipeline pipeline = services.GetRequiredService<BuildPipeline>();
			PluginHost plugins = await BuildCommand.InitializePluginsAsync(services, pipeline, ct);
			return (services, pipeline, plugins);
		}
		catch
		{
			await services.DisposeAsync();
			throw;
		}
	}
}

/// <summary>
///     Where a command reads its configuration from, and the command-line options that override
///     what the file says.
/// </summary>
/// <param name="Path">The full path of mokadocs.yaml.</param>
/// <param name="Output">The <c>--output</c> value, or <c>null</c>.</param>
/// <param name="BasePath">The <c>--base-path</c> value, or <c>null</c>.</param>
/// <param name="ReadableAssets">
///     Whether the theme's CSS and JS stay unminified unless the configuration or the environment sets
///     <c>build.minifyAssets</c>, as <c>serve</c> wants them for debugging. <c>build.minify</c> still
///     minifies them.
/// </param>
internal sealed record ConfigSource(string Path, string? Output = null, string? BasePath = null, bool ReadableAssets = false)
{
	/// <summary>The folder holding the configuration file.</summary>
	public string RootDirectory => System.IO.Path.GetDirectoryName(Path)!;
}

/// <summary>What <see cref="BuildSession.ReloadIfChangedAsync" /> did.</summary>
internal enum ConfigReloadOutcome
{
	/// <summary>The file has the same text as before.</summary>
	Unchanged,

	/// <summary>The new configuration is in use.</summary>
	Reloaded,

	/// <summary>The file couldn't be read or parsed; the previous configuration stays.</summary>
	Failed
}

/// <summary>The result of a configuration reload.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Error">Why the reload failed, or <c>null</c>.</param>
/// <param name="Notes">Changed settings that need a restart.</param>
internal sealed record ConfigReload(ConfigReloadOutcome Outcome, string? Error, IReadOnlyList<string> Notes)
{
	/// <summary>The file didn't change.</summary>
	public static ConfigReload Unchanged { get; } = new(ConfigReloadOutcome.Unchanged, null, []);

	/// <summary>A reload that failed for <paramref name="error" />.</summary>
	public static ConfigReload Failed(string error) => new(ConfigReloadOutcome.Failed, error, []);
}
