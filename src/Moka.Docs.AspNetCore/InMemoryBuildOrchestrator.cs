using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Plugins;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Orchestrates a full MokaDocs build using a <see cref="MockFileSystem" />
///     so all output stays in memory — no disk I/O required.
/// </summary>
public sealed class InMemoryBuildOrchestrator(
    BuildPipeline pipeline,
    PluginHost pluginHost,
    ReflectionApiModelBuilder apiModelBuilder,
    ILogger<InMemoryBuildOrchestrator> logger)
{
    private const string VirtualRoot = "/mokadocs-virtual";
    private const string VirtualOutput = "/mokadocs-virtual/_site";

    /// <summary>
    ///     Builds the entire documentation site in memory from the given options.
    /// </summary>
    public async Task<InMemorySite> BuildAsync(MokaDocsOptions options, CancellationToken ct = default)
    {
        logger.LogInformation("Starting in-memory MokaDocs build for '{Title}'", options.Title);

        var config = SiteConfigFactory.Create(options);

        // Create virtual filesystem
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory(VirtualRoot);
        fs.Directory.CreateDirectory(VirtualOutput);

        // Copy real markdown docs into the virtual filesystem if DocsPath is specified
        if (options.DocsPath is not null)
        {
            var resolvedDocsPath = Path.IsPathRooted(options.DocsPath)
                ? options.DocsPath
                : Path.Combine(Directory.GetCurrentDirectory(), options.DocsPath);
            logger.LogInformation("Copying docs from {RealPath} to virtual FS at {VirtualPath}",
                resolvedDocsPath, $"{VirtualRoot}/{config.Content.Docs}");
            CopyDocsToVirtualFs(fs, resolvedDocsPath, VirtualRoot, config.Content.Docs);
        }
        else
        {
            // Create an empty docs dir so DiscoveryPhase doesn't fail
            var docsDir = fs.Path.Combine(VirtualRoot, config.Content.Docs);
            fs.Directory.CreateDirectory(docsDir);
        }

        // Build API model from runtime assemblies via reflection
        var apiModel = apiModelBuilder.Build(options.Assemblies, options.IncludeXmlDocs);
        logger.LogInformation("Reflection analysis found {TypeCount} types across {AsmCount} assemblies",
            apiModel.Namespaces.Sum(n => n.Types.Count), apiModel.Assemblies.Count);

        // Create BuildContext with the virtual filesystem
        var context = new BuildContext
        {
            Config = config,
            FileSystem = fs,
            RootDirectory = VirtualRoot,
            OutputDirectory = VirtualOutput
        };

        // Pre-populate the API model so ReflectionApiPagePhase generates pages
        context.ApiModel = apiModel;


        // Initialize plugins
        await pluginHost.DiscoverAndInitializeAsync(ct);

        // Wire up plugin hook
        pipeline.PluginHook = async (ctx, c) =>
        {
            if (pluginHost.LoadedPlugins.Count > 0)
                await pluginHost.ExecuteAllAsync(ctx, c);
        };

        // Run the full build pipeline
        await pipeline.ExecuteAsync(context, ct);

        // Harvest all generated files from the virtual filesystem
        var site = HarvestSite(fs, VirtualOutput, options.BasePath);

        logger.LogInformation("In-memory build complete: {FileCount} files, {PageCount} pages",
            site.Files.Count, context.Pages.Count);

        return site;
    }

    /// <summary>
    ///     Copies real markdown files from disk into the MockFileSystem
    ///     so that DiscoveryPhase and MarkdownParsePhase can find them.
    /// </summary>
    private static void CopyDocsToVirtualFs(MockFileSystem fs, string realDocsPath, string virtualRoot,
        string configDocsPath)
    {
        var resolvedPath = Path.GetFullPath(realDocsPath);
        if (!Directory.Exists(resolvedPath)) return;

        var virtualDocsDir = fs.Path.Combine(virtualRoot, configDocsPath);
        fs.Directory.CreateDirectory(virtualDocsDir);

        foreach (var file in Directory.EnumerateFiles(resolvedPath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(resolvedPath, file);
            var virtualPath = fs.Path.Combine(virtualDocsDir, relativePath);

            var dir = fs.Path.GetDirectoryName(virtualPath);
            if (dir is not null) fs.Directory.CreateDirectory(dir);

            var content = File.ReadAllBytes(file);
            fs.File.WriteAllBytes(virtualPath, content);
        }
    }

    /// <summary>
    ///     Reads all files from the virtual output directory and creates an InMemorySite.
    ///     Rewrites paths to include the basePath prefix for correct URL routing.
    /// </summary>
    private static InMemorySite HarvestSite(MockFileSystem fs, string outputDir, string basePath)
    {
        var files = new Dictionary<string, SiteFile>(StringComparer.OrdinalIgnoreCase);
        var normalizedBase = basePath.Trim('/');

        foreach (var filePath in fs.Directory.EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = fs.Path.GetRelativePath(outputDir, filePath);
            // Normalize path separators
            relativePath = relativePath.Replace('\\', '/');

            var extension = fs.Path.GetExtension(filePath);
            var contentType = SiteFile.GetContentType(extension);
            var content = fs.File.ReadAllBytes(filePath);

            // For HTML files, rewrite internal links to include basePath
            if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(normalizedBase))
            {
                var html = Encoding.UTF8.GetString(content);
                html = RewriteLinks(html, normalizedBase);
                content = Encoding.UTF8.GetBytes(html);
            }

            files[relativePath] = new SiteFile(content, contentType);
        }

        return new InMemorySite { Files = files };
    }

    /// <summary>
    ///     Rewrites internal links (href="/..." and src="/...") to include the basePath prefix.
    /// </summary>
    private static string RewriteLinks(string html, string basePath)
    {
        // Rewrite href="/" links (but not external http:// or #anchors)
        html = Regex.Replace(html,
            @"(href|src|action)=""(/(?!/))",
            $"$1=\"/{basePath}/");

        // Rewrite url(/) in inline CSS
        html = Regex.Replace(html,
            @"url\((/(?!/))",
            $"url(/{basePath}/");

        // Clean up any double slashes from joining
        html = html.Replace($"/{basePath}//", $"/{basePath}/");

        return html;
    }
}