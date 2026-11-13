using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore.OpenApi;
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
	///     What the build reads from <see cref="MokaDocsOptions.DocsPath" />: Markdown, plus the asset
	///     types <c>FileDiscoveryService</c> publishes (a test keeps the two lists equal). Everything
	///     used to be read, video and locked files included.
	/// </summary>
	private static readonly HashSet<string> _copiedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".md",
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico",
		".pdf", ".zip", ".mp4", ".webm",
		".css", ".js", ".json", ".xml",
		".woff", ".woff2", ".ttf", ".eot",
		".txt", ".webmanifest", ".avif"
	};

	/// <summary>Extensionless files discovery publishes from the docs root.</summary>
	private static readonly HashSet<string> _rootHostFiles = new(StringComparer.Ordinal)
	{
		"CNAME", "_redirects", "_headers"
	};

	private static readonly EnumerationOptions _oneLevel = new()
	{
		IgnoreInaccessible = true,
		AttributesToSkip = 0
	};

	private readonly StagedDirectoryCache _stagedDirectories = new();

	/// <summary>
	///     The compressed forms the last build made, keyed by <see cref="SiteFile.ContentHash" />, so a
	///     rebuild compresses only the files that changed.
	/// </summary>
	private IReadOnlyDictionary<string, IReadOnlyList<SiteFileEncoding>> _compressed =
		new Dictionary<string, IReadOnlyList<SiteFileEncoding>>();

	/// <summary>
	///     Folders under <see cref="MokaDocsOptions.DocsPath" /> that never reach the site: build
	///     output and packages. With <c>DocsPath = "."</c>, <c>bin</c> published the app's
	///     <c>.deps.json</c> and <c>obj</c> its <c>project.assets.json</c>.
	/// </summary>
	internal static IReadOnlyList<string> ExcludedFolders { get; } = ["bin", "obj", "node_modules"];

	/// <summary>How many times a staged directory was read from disk instead of the cache.</summary>
	internal int StagedDirectoryReads => _stagedDirectories.Reads;

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

		string contentRoot = SiteConfigFactory.ContentRoot(environment);

		// Build API model from runtime assemblies via reflection. It is in the build context before
		// the pipeline runs, so sidebar entries with AutoGenerate can list its namespaces.
		var apiDiagnostics = new DiagnosticBag();
		ApiReference apiModel = apiModelBuilder.Build(options, contentRoot, apiDiagnostics);
		logger.LogInformation("Reflection analysis found {TypeCount} types across {AsmCount} assemblies",
			apiModel.Namespaces.Sum(n => n.Types.Count), apiModel.Assemblies.Count);

		// The virtual file system mirrors the real paths: the docs folder sits at its real location
		// under the content root, which is the build's root directory. Plugins resolve relative
		// option paths against that root on the real disk, the way the CLI resolves them against
		// the mokadocs.yaml folder. The old "/mokadocs-virtual" root sent the OpenAPI spec and the
		// Blazor preview host to a folder that does not exist.
		string docsPath = SiteConfigFactory.DocsDirectory(contentRoot, options.DocsPath);
		SiteConfig config = SiteConfigFactory.Create(options, environment);
		config = config with { Content = config.Content with { Docs = docsPath } };

		var fs = new MockFileSystem();
		fs.Directory.CreateDirectory(contentRoot);
		fs.Directory.CreateDirectory(docsPath);
		fs.Directory.CreateDirectory(_virtualOutput);

		DocsCopy? copy = null;
		if (options.DocsPath is not null)
		{
			logger.LogInformation("Copying docs from {RealPath} to the in-memory file system", docsPath);
			copy = CopyDocsToVirtualFs(fs, docsPath);
			if (copy is null)
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

		if (copy is not null)
		{
			ReportCopyProblems(context, copy, options.DocsPath, docsPath, contentRoot);
		}

		if (!string.IsNullOrWhiteSpace(options.SocialImage) && config.Site.Image is null)
		{
			context.Diagnostics.Warning(
				$"SocialImage \"{options.SocialImage}\" is outside DocsPath ({docsPath}), which is all the build reads, " +
				"so link previews get no image. Move the file under DocsPath or use an absolute URL.",
				"SocialImage");
		}
		else if (config.Site.Image is not null && string.IsNullOrWhiteSpace(config.Site.Url))
		{
			context.Diagnostics.Warning(
				"SocialImage is set but SiteUrl isn't. Link previews need absolute URLs, so pages get no preview tags " +
				"until SiteUrl names the URL readers reach the app at.",
				"SocialImage");
		}

		// Pre-populate the API model so ReflectionApiPagePhase generates pages
		context.ApiModel = apiModel;

		// The model is built before the context exists. The builder reports warnings only.
		foreach (Diagnostic diagnostic in apiDiagnostics.All)
		{
			context.Diagnostics.Warning(diagnostic.Message, diagnostic.Source);
		}

		// The CLI fills these from mokadocs.yaml the same way; the header's version selector reads them.
		var versionManager = new VersionManager(config, NullLogger<VersionManager>.Instance);
		if (versionManager.IsEnabled)
		{
			context.Versions.AddRange(versionManager.Versions);
			context.CurrentVersion = versionManager.DefaultVersion;
		}

		// Initialize plugins
		await pluginHost.DiscoverAndInitializeAsync(ct);

		// The app's own OpenAPI document (MokaDocsOptions.OpenApiDocument), read for every build: the
		// endpoints it describes exist only once the app runs.
		await AppOpenApiDocument.SupplyAsync(pluginHost, context.Diagnostics, logger, ct);

		// Wire up plugin hook
		var staged = new Dictionary<string, SiteFile>(StringComparer.OrdinalIgnoreCase);
		pipeline.PluginHook = async (ctx, c) =>
		{
			if (pluginHost.LoadedPlugins.Count > 0)
			{
				await pluginHost.ExecuteAllAsync(ctx, c);
			}

			StageDeferredDirectories(ctx, staged, c);
		};

		// Run the full build pipeline
		await pipeline.ExecuteAsync(context, ct);

		LogDiagnostics(context);

		// Harvest all generated files from the virtual filesystem
		Dictionary<string, SiteFile> files = HarvestFiles(fs, _virtualOutput);
		CompressFiles(files, ct);

		// Last, as OutputPhase copies staged directories after everything but the theme files.
		foreach ((string path, SiteFile file) in staged)
		{
			files[path] = file;
		}

		var site = new InMemorySite
		{
			Files = files,
			Diagnostics = context.Diagnostics.All
		};

		logger.LogInformation("In-memory build complete: {FileCount} files, {PageCount} pages",
			site.Files.Count, context.Pages.Count);

		return site;
	}

	/// <summary>
	///     Copies the files the site publishes from disk into the MockFileSystem so that
	///     DiscoveryPhase and MarkdownParsePhase can find them.
	/// </summary>
	/// <remarks>
	///     Folders and files whose names start with a dot are skipped, except <c>.well-known</c>.
	/// </remarks>
	/// <returns><c>null</c> when the folder does not exist.</returns>
	private static DocsCopy? CopyDocsToVirtualFs(MockFileSystem fs, string docsPath)
	{
		if (!Directory.Exists(docsPath))
		{
			return null;
		}

		var copy = new DocsCopy();
		CopyDirectory(fs, docsPath, docsPath, copy, new HashSet<string>(StringComparer.Ordinal));
		return copy;
	}

	private static void CopyDirectory(MockFileSystem fs, string docsPath, string directory, DocsCopy copy,
		HashSet<string> visited)
	{
		string[] files;
		string[] subdirectories;
		try
		{
			// A folder symlink that points back up the tree would otherwise be copied forever.
			string realPath = new DirectoryInfo(directory).ResolveLinkTarget(true)?.FullName ?? Path.GetFullPath(directory);
			if (!visited.Add(realPath))
			{
				return;
			}

			files = Directory.GetFiles(directory, "*", _oneLevel);
			subdirectories = Directory.GetDirectories(directory, "*", _oneLevel);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			copy.Unreadable.Add((Path.GetRelativePath(docsPath, directory).Replace('\\', '/') + "/", ex.Message));
			return;
		}

		bool atRoot = string.Equals(directory, docsPath, StringComparison.Ordinal);

		// A site MokaDocs built is not docs. Only its marker is copied, so discovery skips the folder
		// and warns about it the way a CLI build does.
		if (!atRoot && files.Any(f => Path.GetFileName(f) == OutputDirectoryGuard.MarkerFileName))
		{
			string virtualDirectory = fs.Path.Combine(docsPath, Path.GetRelativePath(docsPath, directory));
			fs.Directory.CreateDirectory(virtualDirectory);
			fs.File.WriteAllText(fs.Path.Combine(virtualDirectory, OutputDirectoryGuard.MarkerFileName), "");
			return;
		}

		foreach (string file in files)
		{
			string name = Path.GetFileName(file);
			string relativePath = Path.GetRelativePath(docsPath, file);
			if (IsAppFile(name))
			{
				copy.Excluded.Add(relativePath.Replace('\\', '/'));
				continue;
			}

			// The glossary file isn't published, but the build reads it from the docs root.
			if (name.StartsWith('.')
			    || !(_copiedExtensions.Contains(Path.GetExtension(name))
			         || (atRoot && (_rootHostFiles.Contains(name) || GlossaryFile.FileNames.Contains(name)))))
			{
				continue;
			}

			byte[] content;
			try
			{
				content = File.ReadAllBytes(file);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				copy.Unreadable.Add((relativePath.Replace('\\', '/'), ex.Message));
				continue;
			}

			string virtualPath = fs.Path.Combine(docsPath, relativePath);
			if (fs.Path.GetDirectoryName(virtualPath) is { } virtualDirectory)
			{
				fs.Directory.CreateDirectory(virtualDirectory);
			}

			fs.File.WriteAllBytes(virtualPath, content);
		}

		foreach (string subdirectory in subdirectories)
		{
			string name = Path.GetFileName(subdirectory);
			if (ExcludedFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
			{
				copy.Excluded.Add(Path.GetRelativePath(docsPath, subdirectory).Replace('\\', '/') + "/");
				continue;
			}

			if (name.StartsWith('.') && !name.Equals(".well-known", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			CopyDirectory(fs, docsPath, subdirectory, copy, visited);
		}
	}

	/// <summary>
	///     Whether a file holds the app's settings or is part of its build output. With
	///     <c>DocsPath = "."</c>, <c>GET /docs/appsettings.json</c> returned the connection strings.
	/// </summary>
	internal static bool IsAppFile(string fileName) =>
		(fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
		 && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
		|| fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
		|| fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)
		|| fileName.EndsWith(".runtimeconfig.dev.json", StringComparison.OrdinalIgnoreCase)
		|| fileName.Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase)
		|| fileName.Equals("secrets.json", StringComparison.OrdinalIgnoreCase)
		|| fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
		|| fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	///     Warns about what the copy left out. One warning covers the app's own files, so a
	///     <c>DocsPath</c> of <c>"."</c> doesn't log a line per file on every build.
	/// </summary>
	private static void ReportCopyProblems(BuildContext context, DocsCopy copy, string? configuredPath,
		string docsPath, string contentRoot)
	{
		bool isContentRoot = string.Equals(
			Path.TrimEndingDirectorySeparator(docsPath), Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot)),
			OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

		if (isContentRoot || copy.Excluded.Count > 0)
		{
			const int listed = 5;
			string where = isContentRoot ? "the app's content root" : docsPath;
			string left = copy.Excluded.Count == 0
				? ""
				: " These stay out of the docs site: " + string.Join(", ", copy.Excluded.Take(listed))
				                                       + (copy.Excluded.Count > listed ? $" and {copy.Excluded.Count - listed} more" : "")
				                                       + ".";
			context.Diagnostics.Warning(
				$"DocsPath \"{configuredPath}\" is {where}, which holds the app's settings or build output.{left} " +
				"Point DocsPath at a folder that holds only the docs.",
				"DocsPath");
		}

		foreach ((string path, string reason) in copy.Unreadable)
		{
			context.Diagnostics.Warning($"Skipped {path} in the docs folder because it can't be read: {reason}",
				"DocsPath");
		}
	}

	/// <summary>
	///     Takes the directories plugins staged for the output (the Blazor preview host's published
	///     app) from <see cref="BuildContext.DeferredOutputDirectories" /> and adds their files to
	///     <paramref name="staged" />, through <see cref="StagedDirectoryCache" />.
	/// </summary>
	/// <remarks>
	///     <c>OutputPhase</c> copies a staged directory with the build's file system, which here
	///     is the in-memory one. The directory lives on the real disk, so the copy threw and the
	///     whole build failed as soon as a preview host was found. The files skip the in-memory file
	///     system altogether: going through it read and hashed tens of megabytes on every rebuild.
	/// </remarks>
	private void StageDeferredDirectories(BuildContext context, Dictionary<string, SiteFile> staged,
		CancellationToken ct)
	{
		foreach ((string sourceDir, string destRelPath) in context.DeferredOutputDirectories.ToList())
		{
			if (!Directory.Exists(sourceDir))
			{
				continue; // OutputPhase warns about it and moves on.
			}

			context.DeferredOutputDirectories.Remove((sourceDir, destRelPath));
			if (OutputPrefix(destRelPath) is not { } prefix)
			{
				context.Diagnostics.Error(
					$"'{destRelPath}' was not written: it resolves outside the output directory", "Output");
				continue;
			}

			var unreadable = new List<(string Path, string Reason)>();
			foreach ((string relative, SiteFile file) in _stagedDirectories.Get(sourceDir, unreadable, ct))
			{
				staged[prefix + relative] = file;
			}

			foreach ((string path, string reason) in unreadable)
			{
				context.Diagnostics.Warning($"Skipped {prefix}{path} because it can't be read: {reason}", "Output");
			}
		}
	}

	/// <summary>
	///     A staged directory's output path with a trailing <c>/</c> (empty for the site root), or
	///     <c>null</c> when it would leave the output directory.
	/// </summary>
	internal static string? OutputPrefix(string destRelPath)
	{
		string[] segments = destRelPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (Path.IsPathRooted(destRelPath) || segments.Any(segment => segment == ".." || segment.Contains(':')))
		{
			return null;
		}

		string joined = string.Join('/', segments.Where(segment => segment != "."));
		return joined.Length == 0 ? "" : joined + "/";
	}

	/// <summary>
	///     Adds Brotli and gzip forms to the files worth compressing, reusing those of the last build
	///     for files whose content didn't change. It runs once per build, so no request pays for it.
	/// </summary>
	private void CompressFiles(Dictionary<string, SiteFile> files, CancellationToken ct)
	{
		IReadOnlyDictionary<string, IReadOnlyList<SiteFileEncoding>> previous = Volatile.Read(ref _compressed);
		var current = new ConcurrentDictionary<string, IReadOnlyList<SiteFileEncoding>>(StringComparer.Ordinal);
		KeyValuePair<string, SiteFile>[] candidates =
			files.Where(entry => SiteCompression.ShouldCompress(entry.Value)).ToArray();

		var compressed = new SiteFile[candidates.Length];
		Parallel.For(0, candidates.Length, new ParallelOptions { CancellationToken = ct }, i =>
		{
			SiteFile file = candidates[i].Value;
			IReadOnlyList<SiteFileEncoding> encodings = current.GetOrAdd(file.ContentHash,
				hash => previous.TryGetValue(hash, out IReadOnlyList<SiteFileEncoding>? reused)
					? reused
					: SiteCompression.Compress(file.Content));
			compressed[i] = encodings.Count == 0 ? file : file.WithEncodings(encodings);
		});

		for (int i = 0; i < candidates.Length; i++)
		{
			files[candidates[i].Key] = compressed[i];
		}

		Volatile.Write(ref _compressed, current);
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

	/// <summary>What <see cref="CopyDocsToVirtualFs" /> left out, relative to the docs folder.</summary>
	private sealed class DocsCopy
	{
		public List<string> Excluded { get; } = [];
		public List<(string Path, string Reason)> Unreadable { get; } = [];
	}
}
