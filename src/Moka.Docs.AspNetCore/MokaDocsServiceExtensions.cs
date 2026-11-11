using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore.Phases;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Extension methods for adding MokaDocs to an ASP.NET Core application's DI container.
/// </summary>
public static class MokaDocsServiceExtensions
{
	private const string _replPluginId = "mokadocs-repl";

	/// <summary>
	///     Adds MokaDocs documentation services to the application.
	///     Call <c>app.MapMokaDocs()</c> to serve the documentation site.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Optional configuration action.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaDocs(
		this IServiceCollection services,
		Action<MokaDocsOptions>? configure = null)
	{
		var options = new MokaDocsOptions();
		configure?.Invoke(options);

		// Auto-discover calling assembly if none specified
		if (options.Assemblies.Count == 0)
		{
			var callingAssembly = Assembly.GetCallingAssembly();
			options.Assemblies.Add(callingAssembly);
		}

		// Create SiteConfig from options
		SiteConfig siteConfig = SiteConfigFactory.Create(options);

		// Register options and config
		services.AddSingleton(options);
		services.AddSingleton(siteConfig);

		// Register filesystem abstraction
		services.AddSingleton<IFileSystem>(new FileSystem());

		// Register all MokaDocs subsystems
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();

		services.AddMokaDocsEngine();

		// Register our reflection-based API page phase
		services.AddSingleton<IBuildPhase, ReflectionApiPagePhase>();

		// The plugin host runs only the plugins it finds in the container, so the built-ins the site
		// declares are registered here, through the same method as the CLI's. An application
		// registers its own plugins itself. The REPL plugin needs EnableRepl, which also maps the
		// endpoint that runs code: a mokadocs-repl entry in options.Plugins alone doesn't register it.
		services.AddMokaDocsPlugins(plugins => plugins.BuiltInPlugins = siteConfig.Plugins
			.Select(p => p.Name ?? "")
			.Where(id => options.EnableRepl || !string.Equals(id, _replPluginId, StringComparison.OrdinalIgnoreCase))
			.ToList());

		if (options.EnableRepl)
		{
			// Owns the worker process; the container disposes it, and the worker with it, on shutdown.
			services.AddSingleton<MokaDocsRepl>();
		}

		// ASP.NET Core integration services
		services.AddSingleton<ReflectionApiModelBuilder>();
		services.AddSingleton<InMemoryBuildOrchestrator>();
		services.AddSingleton<MokaDocsService>();

		// The startup build, the docs watcher and the REPL warm-up. Only a running host starts it.
		services.AddHostedService<MokaDocsStartupService>();

		return services;
	}
}
