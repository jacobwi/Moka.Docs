using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore.Phases;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.BlazorPreview;
using Moka.Docs.Plugins.OpenApi;
using Moka.Docs.Plugins.Repl;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Extension methods for adding MokaDocs to an ASP.NET Core application's DI container.
/// </summary>
public static class MokaDocsServiceExtensions
{
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
        var siteConfig = SiteConfigFactory.Create(options);

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

        // Plugin system
        services.AddSingleton<PluginHost>();
        services.AddSingleton<IMokaPlugin, OpenApiPlugin>();

        if (options.EnableRepl)
            services.AddSingleton<IMokaPlugin, ReplPlugin>();
        if (options.EnableBlazorPreview)
            services.AddSingleton<IMokaPlugin, BlazorPreviewPlugin>();

        // ASP.NET Core integration services
        services.AddSingleton<ReflectionApiModelBuilder>();
        services.AddSingleton<InMemoryBuildOrchestrator>();
        services.AddSingleton<MokaDocsService>();

        return services;
    }
}