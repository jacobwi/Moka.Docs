using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Features;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.BlazorPreview;
using Moka.Docs.Plugins.Changelog;
using Moka.Docs.Plugins.OpenApi;
using Moka.Docs.Plugins.Repl;
using Moka.Docs.Versioning;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Runs the full build pipeline to generate the static site.
/// </summary>
internal static class BuildCommand
{
    /// <summary>Creates the build command.</summary>
    public static Command Create()
    {
        var watchOption = new Option<bool>("--watch") { Description = "Watch for changes and rebuild automatically" };
        var configOption = new Option<string?>("--config")
            { Description = "Path to configuration file (default: mokadocs.yaml)" };
        var outputOption = new Option<string?>("--output") { Description = "Output directory (default: _site/)" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose logging" };
        var draftOption = new Option<bool>("--draft") { Description = "Include draft pages" };
        var noCacheOption = new Option<bool>("--no-cache") { Description = "Force full rebuild without caching" };

        var command = new Command("build", "Build the documentation site")
        {
            watchOption,
            configOption,
            outputOption,
            verboseOption,
            draftOption,
            noCacheOption
        };

        command.SetAction(async (parseResult, _) =>
        {
            var watch = parseResult.GetValue(watchOption);
            var configPath = parseResult.GetValue(configOption);
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var draft = parseResult.GetValue(draftOption);
            var noCache = parseResult.GetValue(noCacheOption);

            var sw = Stopwatch.StartNew();

            AnsiConsole.MarkupLine("[bold blue]MokaDocs[/] — Building documentation site...");
            AnsiConsole.WriteLine();

            var rootDir = Directory.GetCurrentDirectory();
            var resolvedConfigPath = configPath ?? Path.Combine(rootDir, "mokadocs.yaml");

            // Load config
            SiteConfig config;
            try
            {
                var fs = new FileSystem();
                var reader = new SiteConfigReader(fs);
                config = reader.Read(resolvedConfigPath);
                AnsiConsole.MarkupLine(
                    $"[green]✓ Config:[/] {Path.GetFileName(resolvedConfigPath)} — \"{Markup.Escape(config.Site.Title)}\"");
            }
            catch (FileNotFoundException)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Config file not found: {resolvedConfigPath}");
                AnsiConsole.MarkupLine("Run [bold]mokadocs init[/] to create a starter project.");
                return 1;
            }
            catch (SiteConfigException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                return 1;
            }

            // Apply CLI overrides
            if (output is not null)
                config = config with { Build = config.Build with { Output = output } };

            var outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));

            // Set up DI
            await using var provider = BuildServices(config, verbose);
            var pipeline = provider.GetRequiredService<BuildPipeline>();

            // Build context
            var context = new BuildContext
            {
                Config = config,
                FileSystem = new FileSystem(),
                RootDirectory = rootDir,
                OutputDirectory = outputDir
            };

            // Wire version data from VersionManager into the build context
            var versionManager = provider.GetRequiredService<VersionManager>();
            if (versionManager.IsEnabled)
            {
                context.Versions.AddRange(versionManager.Versions);
                context.CurrentVersion = versionManager.DefaultVersion;
            }

            // Execute pipeline
            try
            {
                // Initialize plugins before pipeline so they can inject pages
                var pluginHost = provider.GetRequiredService<PluginHost>();
                await pluginHost.DiscoverAndInitializeAsync();

                // Run main pipeline — plugins execute as a hook after content phases
                pipeline.PluginHook = async (ctx, ct) =>
                {
                    if (pluginHost.LoadedPlugins.Count > 0)
                        await pluginHost.ExecuteAllAsync(ctx, ct);
                };

                await pipeline.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Build failed:[/] {Markup.Escape(ex.Message)}");
                if (verbose) AnsiConsole.WriteException(ex);
                return 1;
            }

            sw.Stop();

            // Build summary
            var mdPages = context.Pages.Count(p => p.Origin == PageOrigin.Markdown);
            var apiPages = context.Pages.Count(p => p.Origin == PageOrigin.ApiGenerated);
            var apiTypes = context.ApiModel?.Namespaces.Sum(n => n.Types.Count) ?? 0;
            var searchEntries = context.SearchIndex?.Entries.Count ?? 0;

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green bold]✅ MokaDocs build complete in {sw.Elapsed.TotalSeconds:F2}s[/]");
            AnsiConsole.MarkupLine($"📄 Pages:        {mdPages + apiPages} ({mdPages} markdown, {apiPages} generated)");

            if (apiTypes > 0)
                AnsiConsole.MarkupLine(
                    $"🔧 API Types:    {apiTypes} across {context.ApiModel!.Assemblies.Count} assemblies");

            if (searchEntries > 0)
                AnsiConsole.MarkupLine($"🔍 Search Index: {searchEntries} entries");

            AnsiConsole.MarkupLine($"📦 Output:       {config.Build.Output}");

            if (context.Diagnostics.HasWarnings || context.Diagnostics.HasErrors)
            {
                var warnings = context.Diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Warning);
                var errors = context.Diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Error);
                AnsiConsole.MarkupLine($"⚠️  Diagnostics:  {warnings} warnings, {errors} errors");

                if (verbose)
                    foreach (var diag in context.Diagnostics.All)
                    {
                        var color = diag.Severity == DiagnosticSeverity.Error ? "red" : "yellow";
                        AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(diag.ToString())}[/]");
                    }
                else
                    AnsiConsole.MarkupLine("    [dim]Run with --verbose to see details[/]");
            }

            return 0;
        });

        return command;
    }

    /// <summary>
    ///     Builds the service provider with all MokaDocs services and plugins.
    /// </summary>
    internal static ServiceProvider BuildServices(SiteConfig config, bool verbose)
    {
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            if (verbose) b.SetMinimumLevel(LogLevel.Debug);
            else b.SetMinimumLevel(LogLevel.Warning);
        });

        // Feature management: MokaDefaults -> in-memory config, overridable via MOKADOCS_ env vars
        var featureConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                MokaFeatureConfiguration.GetDefaults()
                    .ToDictionary(
                        kv => $"FeatureManagement:{kv.Key}",
                        kv => (string?)kv.Value.ToString()))
            .AddEnvironmentVariables("MOKADOCS_")
            .Build();

        services.AddSingleton<IConfiguration>(featureConfig);
        services.AddFeatureManagement(featureConfig.GetSection("FeatureManagement"));

        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddSingleton(config);
        services.AddMokaDocsParsing();
        services.AddMokaDocsCSharp();
        services.AddMokaDocsEngine();
        services.AddMokaDocsVersioning();

        // Plugin system
        services.AddSingleton<PluginHost>();
        services.AddSingleton<IMokaPlugin, OpenApiPlugin>();
        services.AddSingleton<IMokaPlugin, ReplPlugin>();
        services.AddSingleton<IMokaPlugin, BlazorPreviewPlugin>();
        services.AddSingleton<IMokaPlugin, ChangelogPlugin>();

        return services.BuildServiceProvider();
    }
}