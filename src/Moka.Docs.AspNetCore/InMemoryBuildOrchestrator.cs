using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore.Reflection;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Plugins;
using Moka.Docs.Versioning;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Orchestrates a full MokaDocs build using a <see cref="MockFileSystem" />
///     so all output stays in memory - no disk I/O required.
/// </summary>
/// <param name="pipeline">The build pipeline.</param>
/// <param name="pluginHost">The plugin host.</param>
/// <param name="apiModelBuilder">Builds the API model from the configured assemblies.</param>
/// <param name="environment">
///     The host environment. Relative paths in the options resolve from its content root. When
///     null, they resolve from the current working directory.
/// </param>
/// <param name="logger">Logger instance.</param>
public sealed class InMemoryBuildOrchestrator(
	BuildPipeline pipeline,
	PluginHost pluginHost,
	ReflectionApiModelBuilder apiModelBuilder,
	IHostEnvironment? environment,
	ILogger<InMemoryBuildOrchestrator> logger)
{
	private const string _virtualOutput = "/mokadocs-virtual/_site";

	/// <summary>
	///     Creates an orchestrator that resolves relative paths from the current working directory.
	/// </summary>
	/// <param name="pipeline">The build pipeline.</param>
	/// <param name="pluginHost">The plugin host.</param>
	/// <param name="apiModelBuilder">Builds the API model from the configured assemblies.</param>
	/// <param name="logger">Logger instance.</param>
	public InMemoryBuildOrchestrator(
		BuildPipeline pipeline,
		PluginHost pluginHost,
		ReflectionApiModelBuilder apiModelBuilder,
		ILogger<InMemoryBuildOrchestrator> logger)
		: this(pipeline, pluginHost, apiModelBuilder, null, logger)
	{
	}

	/// <summary>
	///     Builds the entire documentation site in memory from the given options.
	/// </summary>
	public async Task<InMemorySite> BuildAsync(MokaDocsOptions options, CancellationToken ct = default)
	{
		logger.LogInformation("Starting in-memory MokaDocs build for '{Title}'", options.Title);

		// Relative paths resolved from the working directory, so starting the app from another
		// folder, or with --contentRoot, found no docs. The content root is where the app keeps
		// its files, whatever directory the process started in.
		string contentRoot = environment?.ContentRootPath is { Length: > 0 } root
			? root
			: Directory.GetCurrentDirectory();

		// Build API model from runtime assemblies via reflection. It comes first because sidebar
		// entries with AutoGenerate list its namespaces.
		ApiReference apiModel = apiModelBuilder.Build(options.Assemblies, options.IncludeXmlDocs);
		logger.LogInformation("Reflection analysis found {TypeCount} types across {AsmCount} assemblies",
			apiModel.Namespaces.Sum(n => n.Types.Count), apiModel.Assemblies.Count);

		// The virtual file system mirrors the real paths: the docs folder sits at its real location
		// under the content root, which is the build's root directory. Plugins resolve relative
		// option paths against that root on the real disk, the way the CLI resolves them against
		// the mokadocs.yaml folder. The old "/mokadocs-virtual" root sent the OpenAPI spec and the
		// Blazor preview host to a folder that does not exist.
		string docsPath = Path.GetFullPath(Path.Combine(contentRoot, options.DocsPath ?? "docs"));
		SiteConfig config = SiteConfigFactory.Create(options, apiModel);
		config = config with { Content = config.Content with { Docs = docsPath } };

		var fs = new MockFileSystem();
		fs.Directory.CreateDirectory(contentRoot);
		fs.Directory.CreateDirectory(docsPath);
		fs.Directory.CreateDirectory(_virtualOutput);

		// Copy real markdown docs into the virtual filesystem if DocsPath is specified
		if (options.DocsPath is not null)
		{
			logger.LogInformation("Copying docs from {RealPath} to the in-memory file system", docsPath);
			if (!CopyDocsToVirtualFs(fs, docsPath))
			{
				logger.LogWarning("Docs folder {RealPath} does not exist, so the site has no Markdown pages",
					docsPath);
			}
		}

		// Create BuildContext with the virtual filesystem
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = contentRoot,
			OutputDirectory = _virtualOutput
		};

		// Pre-populate the API model so ReflectionApiPagePhase generates pages
		context.ApiModel = apiModel;

		// The CLI fills these from mokadocs.yaml the same way; the header's version selector reads them.
		var versionManager = new VersionManager(config, NullLogger<VersionManager>.Instance);
		if (versionManager.IsEnabled)
		{
			context.Versions.AddRange(versionManager.Versions);
			context.CurrentVersion = versionManager.DefaultVersion;
		}

		// Initialize plugins
		await pluginHost.DiscoverAndInitializeAsync(ct);

		// Wire up plugin hook
		pipeline.PluginHook = async (ctx, c) =>
		{
			if (pluginHost.LoadedPlugins.Count > 0)
			{
				await pluginHost.ExecuteAllAsync(ctx, c);
			}

			StageDeferredDirectories(ctx);
		};

		// Run the full build pipeline
		await pipeline.ExecuteAsync(context, ct);

		LogDiagnostics(context);

		// Harvest all generated files from the virtual filesystem
		var site = new InMemorySite
		{
			Files = HarvestFiles(fs, _virtualOutput),
			Diagnostics = context.Diagnostics.All
		};

		logger.LogInformation("In-memory build complete: {FileCount} files, {PageCount} pages",
			site.Files.Count, context.Pages.Count);

		return site;
	}

	/// <summary>
	///     Copies real markdown files from disk into the MockFileSystem
	///     so that DiscoveryPhase and MarkdownParsePhase can find them.
	/// </summary>
	/// <returns><c>false</c> when the folder does not exist.</returns>
	private static bool CopyDocsToVirtualFs(MockFileSystem fs, string docsPath)
	{
		if (!Directory.Exists(docsPath))
		{
			return false;
		}

		foreach (string file in Directory.EnumerateFiles(docsPath, "*.*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(docsPath, file);
			string virtualPath = fs.Path.Combine(docsPath, relativePath);

			string? dir = fs.Path.GetDirectoryName(virtualPath);
			if (dir is not null)
			{
				fs.Directory.CreateDirectory(dir);
			}

			byte[] content = File.ReadAllBytes(file);
			fs.File.WriteAllBytes(virtualPath, content);
		}

		return true;
	}

	/// <summary>
	///     Reads the directories plugins staged for the output (the Blazor preview host's published
	///     app) into <see cref="BuildContext.DeferredOutputFiles" />.
	/// </summary>
	/// <remarks>
	///     <c>OutputPhase</c> copies a staged directory with the build's file system, which here
	///     is the in-memory one. The directory lives on the real disk, so the copy threw and the
	///     whole build failed as soon as a preview host was found.
	/// </remarks>
	private static void StageDeferredDirectories(BuildContext context)
	{
		foreach ((string sourceDir, string destRelPath) in context.DeferredOutputDirectories.ToList())
		{
			if (!Directory.Exists(sourceDir))
			{
				continue; // OutputPhase warns about it and moves on.
			}

			foreach (string sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
			{
				string relative = Path.GetRelativePath(sourceDir, sourceFile).Replace('\\', '/');
				context.DeferredOutputFiles[$"{destRelPath.TrimEnd('/', '\\')}/{relative}"] = File.ReadAllBytes(sourceFile);
			}

			context.DeferredOutputDirectories.Remove((sourceDir, destRelPath));
		}
	}

	/// <summary>
	///     Logs the build's warnings and errors. The CLI prints them in its summary; this host had
	///     no summary, so a problem only a diagnostic reports, such as two pages sharing a route,
	///     went unnoticed.
	/// </summary>
	private void LogDiagnostics(BuildContext context)
	{
		// Plugins log their own warnings and errors as they report them.
		var pluginSources = new HashSet<string>(
			pluginHost.LoadedPlugins.Select(p => p.Plugin.Id).Append("Plugins"), StringComparer.OrdinalIgnoreCase);

		foreach (Diagnostic diagnostic in context.Diagnostics.All)
		{
			if (diagnostic.Source is not null && pluginSources.Contains(diagnostic.Source))
			{
				continue;
			}

			if (diagnostic.Severity == DiagnosticSeverity.Error)
			{
				logger.LogError("Build error from {Source}: {Message}", diagnostic.Source, diagnostic.Message);
			}
			else if (diagnostic.Severity == DiagnosticSeverity.Warning)
			{
				logger.LogWarning("Build warning from {Source}: {Message}", diagnostic.Source, diagnostic.Message);
			}
		}
	}

	/// <summary>
	///     Reads all files from the virtual output directory.
	///     Links already carry the base path: the site is built with <c>build.basePath</c> set.
	/// </summary>
	private static Dictionary<string, SiteFile> HarvestFiles(MockFileSystem fs, string outputDir)
	{
		var files = new Dictionary<string, SiteFile>(StringComparer.OrdinalIgnoreCase);

		foreach (string filePath in fs.Directory.EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories))
		{
			string relativePath = fs.Path.GetRelativePath(outputDir, filePath);
			// Normalize path separators
			relativePath = relativePath.Replace('\\', '/');

			string extension = fs.Path.GetExtension(filePath);
			string contentType = SiteFile.GetContentType(extension);
			byte[] content = fs.File.ReadAllBytes(filePath);

			files[relativePath] = new SiteFile(content, contentType);
		}

		return files;
	}
}
