using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Moka.Docs.Cli.Diagnostics;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Parsing.Markdown;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Diagnoses issues in a MokaDocs documentation project.
/// </summary>
/// <remarks>
///     Checks that need to know what the build produces (routes, plugins, search index, API
///     model) run against a dry-run build rather than re-deriving those answers. Earlier
///     versions kept their own copies of the routing rules and the plugin list, and both
///     drifted from the real ones.
/// </remarks>
internal static class DoctorCommand
{
	private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".bmp", ".ico", ".avif"
	};

	// Files the output phase writes that a page might reasonably link to.
	private static readonly string[] _generatedTargets = ["/sitemap.xml", "/robots.txt", "/search-index.json", "/404"];

	private static readonly Regex _htmlSrcPattern = new(@"src\s*=\s*[""']([^""']+)[""']", RegexOptions.Compiled);

	/// <summary>Creates the doctor command.</summary>
	public static Command Create()
	{
		var fixOption = new Option<bool>("--fix")
			{ Description = "Attempt to auto-fix issues (e.g., add missing front matter titles)" };
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to config file (default: mokadocs.yaml)" };
		var verboseOption = new Option<bool>("--verbose", "-v")
			{ Description = "Show detailed output for each check" };

		var command = new Command("doctor", "Diagnose issues in your documentation project")
		{
			fixOption,
			configOption,
			verboseOption
		};

		command.SetAction(async (parseResult, ct) =>
		{
			bool fix = parseResult.GetValue(fixOption);
			string? configPath = parseResult.GetValue(configOption);
			bool verbose = parseResult.GetValue(verboseOption);

			// Every path in mokadocs.yaml is relative to the yaml file, not to wherever the
			// command was run from. Using the working directory made --config point at one
			// project while the checks inspected another.
			string resolvedConfigPath = ConfigPath.Resolve(configPath);
			string rootDir = Path.GetDirectoryName(resolvedConfigPath)!;

			AnsiConsole.MarkupLine("[bold blue]mokadocs doctor[/] - Diagnosing your documentation project...");
			AnsiConsole.WriteLine();

			var report = new Report();

			SiteConfig? config = CheckConfiguration(resolvedConfigPath, report);
			await CheckDotnetSdkAsync(report);

			if (config is not null)
			{
				string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
				string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));

				await CheckProjectsAsync(config, rootDir, verbose, report);
				string[] markdownFiles = CheckDocsFolder(config, docsDir, outputDir, report);
				CheckBrandAsset("Logo", config.Site.Logo, report);
				CheckBrandAsset("Favicon", config.Site.Favicon, report);

				DryRunOutcome outcome = await DryRunBuild.RunAsync(config, rootDir, false, false, ct);
				CheckBuild(outcome, report);

				CheckBrokenLinks(markdownFiles, rootDir, docsDir, outcome, report);
				CheckFrontMatter(markdownFiles, rootDir, docsDir, fix, verbose, report);
				CheckOrphanImages(markdownFiles, docsDir, outputDir, rootDir, config, report);
				CheckPlugins(config, outcome, report);
				CheckSearch(config, outcome, report);
				CheckApiCoverage(config, outcome, verbose, report);
			}

			AnsiConsole.WriteLine();
			string resultColor = report.Errors > 0 ? "red" : report.Warnings > 0 ? "yellow" : "green";
			AnsiConsole.MarkupLine(
				$"  [{resultColor}]Result: {report.Passed} passed, {report.Warnings} warning(s), {report.Errors} error(s)[/]");

			return report.ExitCode;
		});

		return command;
	}

	#region Environment

	private static SiteConfig? CheckConfiguration(string configPath, Report report)
	{
		try
		{
			SiteConfig config = new SiteConfigReader(new FileSystem()).Read(configPath);
			report.Pass("Configuration", $"{Path.GetFileName(configPath)} found and valid");
			return config;
		}
		catch (FileNotFoundException)
		{
			report.Fail("Configuration", $"{configPath} not found");
		}
		catch (SiteConfigException ex)
		{
			report.Fail("Configuration", $"Invalid config: {ex.Message}");
		}

		return null;
	}

	private static async Task CheckDotnetSdkAsync(Report report)
	{
		(int exitCode, string output) = await RunProcessAsync("dotnet", "--version");
		if (exitCode == 0)
		{
			report.Pass(".NET SDK", $"{output.Trim()} installed");
		}
		else
		{
			report.Fail(".NET SDK", "dotnet SDK not found on PATH");
		}
	}

	private static async Task CheckProjectsAsync(SiteConfig config, string rootDir, bool verbose, Report report)
	{
		List<ProjectSource> projects = config.Content.Projects;
		// A site with no C# projects is valid: a docs-only site, or one whose API reference
		// comes from a plugin such as mokadocs-python-api. Warning here made every such site
		// exit non-zero.
		if (projects.Count == 0)
		{
			report.Pass("Projects", "None configured (no C# API reference)");
			return;
		}

		var missing = projects
			.Where(p => !File.Exists(Path.GetFullPath(Path.Combine(rootDir, p.Path))))
			.Select(p => p.Path)
			.ToList();

		if (missing.Count > 0)
		{
			report.Fail("Projects", $"{missing.Count} project(s) not found");
			foreach (string path in missing)
			{
				Report.Detail(path);
			}

			return;
		}

		report.Pass("Projects", $"{projects.Count} project(s) found");

		if (verbose)
		{
			string first = Path.GetFullPath(Path.Combine(rootDir, projects[0].Path));
			(int exitCode, _) = await RunProcessAsync("dotnet", $"build \"{first}\" --nologo -v q");
			Report.Detail(exitCode == 0 ? "First project builds successfully" : "First project build had issues");
		}
	}

	private static string[] CheckDocsFolder(SiteConfig config, string docsDir, string outputDir, Report report)
	{
		if (!Directory.Exists(docsDir))
		{
			report.Fail("Docs Folder", $"Docs directory not found: {config.Content.Docs}");
			return [];
		}

		// The build output can live inside the docs folder (this repo uses docs/_site), and
		// anything in it is generated rather than authored.
		string[] markdownFiles = Directory.GetFiles(docsDir, "*.md", SearchOption.AllDirectories)
			.Where(f => !DoctorChecks.IsUnder(f, outputDir))
			.ToArray();

		report.Pass("Docs Folder", $"{markdownFiles.Length} markdown file(s) in {config.Content.Docs}");
		return markdownFiles;
	}

	/// <summary>
	///     Checks a single brand asset. Unset assets emit no row: a site without a logo is
	///     valid and shouldn't clutter the output.
	/// </summary>
	private static void CheckBrandAsset(string label, SiteAssetReference? asset, Report report)
	{
		if (asset is null)
		{
			return;
		}

		if (asset.IsAbsoluteUrl)
		{
			report.Pass(label, $"absolute URL (no file copy): {asset.PublishUrl}");
			return;
		}

		if (asset.SourcePath is null)
		{
			report.Warn(label, $"'{asset.RawValue}' could not be resolved to a filesystem path");
			return;
		}

		if (!File.Exists(asset.SourcePath))
		{
			report.Fail(label, $"source file not found: {asset.SourcePath}");
			Report.Detail($"publish URL would have been: {asset.PublishUrl}");
			return;
		}

		// Flag flattening so users see why ../branding/logo.png is served from /_media/.
		bool flattened = asset.PublishUrl.StartsWith("/_media/", StringComparison.Ordinal);
		report.Pass(label, flattened
			? $"{asset.RawValue} -> {asset.PublishUrl} (flattened, source above yaml dir)"
			: $"{asset.RawValue} -> {asset.PublishUrl}");
	}

	#endregion

	#region Build-Backed Checks

	private static void CheckBuild(DryRunOutcome outcome, Report report)
	{
		if (outcome.Failure is not null)
		{
			report.Fail("Build", $"dry run failed: {outcome.Failure.Message}");
			return;
		}

		BuildContext context = outcome.Context;
		int errors = context.Diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Error);
		int warnings = context.Diagnostics.All.Count(d => d.Severity == DiagnosticSeverity.Warning);

		if (errors > 0)
		{
			report.Fail("Build", $"{errors} error(s) - run mokadocs validate for details");
		}
		else if (warnings > 0)
		{
			report.Warn("Build", $"{warnings} warning(s) - run mokadocs validate for details");
		}
		else
		{
			report.Pass("Build",
				$"dry run succeeded in {outcome.Elapsed.TotalSeconds:F2}s ({context.Pages.Count} pages)");
		}
	}

	private static void CheckBrokenLinks(string[] markdownFiles, string rootDir, string docsDir, DryRunOutcome outcome,
		Report report)
	{
		if (markdownFiles.Length == 0)
		{
			return;
		}

		if (outcome.Failure is not null)
		{
			Report.Skip("Broken Links", "skipped because the build failed");
			return;
		}

		BuildContext context = outcome.Context;

		// Routes the build actually produced: markdown pages (honouring route: overrides and
		// feature gating), generated API pages and plugin pages alike.
		var targets = new HashSet<string>(
			context.Pages
				.Where(p => p.FrontMatter.Visibility != PageVisibility.Draft)
				.Select(p => DoctorChecks.NormalizeRoute(p.Route)),
			StringComparer.OrdinalIgnoreCase);
		DoctorChecks.AddSectionAncestors(targets);

		foreach (string asset in context.DiscoveredAssetFiles)
		{
			targets.Add(DoctorChecks.NormalizeRoute("/" + asset.Replace('\\', '/')));
		}

		foreach (string url in context.BrandAssetFiles.Keys.Concat(_generatedTargets))
		{
			targets.Add(DoctorChecks.NormalizeRoute(url));
		}

		var draftRoutes = new HashSet<string>(
			context.Pages
				.Where(p => p.FrontMatter.Visibility == PageVisibility.Draft)
				.Select(p => DoctorChecks.NormalizeRoute(p.Route)),
			StringComparer.OrdinalIgnoreCase);

		var broken = new List<string>();
		foreach (string file in markdownFiles)
		{
			string relFile = Path.GetRelativePath(rootDir, file);
			string pathInDocs = Path.GetRelativePath(docsDir, file);
			foreach (MarkdownLink link in DoctorChecks.FindLinks(File.ReadAllText(file)))
			{
				// Relative links are checked where the build sends them: it resolves them
				// against the file, the same way RelativeLinks does here.
				bool rootRelative = DoctorChecks.IsRootRelative(link.Url);
				string? target = rootRelative ? link.Url : RelativeLinks.ToRootRelative(link.Url, pathInDocs);
				if (target is null)
				{
					continue;
				}

				string route = DoctorChecks.NormalizeRoute(target);
				if (targets.Contains(route))
				{
					continue;
				}

				string note = draftRoutes.Contains(route) ? ", links to a draft page"
					: link.IsImage ? ", image"
					: "";
				string written = rootRelative ? "" : $", written as {link.Url}";
				broken.Add($"{route} ({relFile}:{link.Line}{note}{written})");
			}
		}

		if (broken.Count == 0)
		{
			report.Pass("Broken Links", "No broken internal links found");
			return;
		}

		report.Fail("Broken Links", $"{broken.Count} broken internal link(s) found");
		foreach (string entry in broken)
		{
			Report.Detail(entry);
		}
	}

	private static void CheckFrontMatter(string[] markdownFiles, string rootDir, string docsDir, bool fix, bool verbose,
		Report report)
	{
		if (markdownFiles.Length == 0)
		{
			return;
		}

		var missing = new List<string>();
		foreach (string file in markdownFiles)
		{
			(string content, Encoding encoding) = ReadPreservingEncoding(file);
			if (DoctorChecks.HasTitle(content))
			{
				continue;
			}

			missing.Add(Path.GetRelativePath(rootDir, file));

			if (fix)
			{
				string title = DoctorChecks.TitleFromPath(file, docsDir);
				File.WriteAllText(file, DoctorChecks.AddTitle(content, title), encoding);
			}
		}

		if (missing.Count == 0)
		{
			report.Pass("Front Matter", "All pages have titles");
			return;
		}

		report.Warn("Front Matter", fix
			? $"{missing.Count} page(s) were missing titles (auto-fixed)"
			: $"{missing.Count} page(s) missing title in front matter");

		if (verbose || fix)
		{
			foreach (string file in missing)
			{
				Report.Detail(file);
			}
		}
	}

	private static void CheckOrphanImages(string[] markdownFiles, string docsDir, string outputDir, string rootDir,
		SiteConfig config, Report report)
	{
		if (markdownFiles.Length == 0)
		{
			return;
		}

		var images = Directory.GetFiles(docsDir, "*.*", SearchOption.AllDirectories)
			.Where(f => _imageExtensions.Contains(Path.GetExtension(f)))
			.Where(f => !DoctorChecks.IsUnder(f, outputDir))
			.ToList();

		if (images.Count == 0)
		{
			report.Pass("Orphan Images", "No images found in docs folder");
			return;
		}

		var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (string file in markdownFiles)
		{
			string content = File.ReadAllText(file);
			string fileDir = Path.GetDirectoryName(file)!;

			IEnumerable<string> urls = DoctorChecks.FindLinks(content).Select(l => l.Url)
				.Concat(_htmlSrcPattern.Matches(content).Select(m => m.Groups[1].Value));

			foreach (string url in urls)
			{
				if (ResolveLocalReference(url, docsDir, fileDir) is { } resolved)
				{
					referenced.Add(resolved);
				}
			}
		}

		// The logo and favicon are referenced from mokadocs.yaml, not from any page. An image
		// in docs/ that publishes to the same URL is the file actually served, because the
		// asset copy runs before the brand-asset copy and the brand copy skips existing files.
		foreach (SiteAssetReference? asset in new[] { config.Site.Logo, config.Site.Favicon })
		{
			if (asset is not { IsAbsoluteUrl: false })
			{
				continue;
			}

			if (asset.SourcePath is not null)
			{
				referenced.Add(Path.GetFullPath(asset.SourcePath));
			}

			referenced.Add(Path.GetFullPath(Path.Combine(docsDir, asset.PublishUrl.TrimStart('/'))));
		}

		var orphans = images.Where(img => !referenced.Contains(Path.GetFullPath(img))).ToList();

		if (orphans.Count == 0)
		{
			report.Pass("Orphan Images", $"All {images.Count} image(s) are referenced");
			return;
		}

		report.Warn("Orphan Images", $"{orphans.Count} unreferenced image(s)");
		foreach (string orphan in orphans)
		{
			Report.Detail(Path.GetRelativePath(rootDir, orphan));
		}
	}

	private static void CheckPlugins(SiteConfig config, DryRunOutcome outcome, Report report)
	{
		if (config.Plugins.Count == 0)
		{
			report.Pass("Plugins", "No plugins declared");
			return;
		}

		// Ids come from the plugins the CLI actually registers. The hardcoded list this
		// replaced used names like "repl", which PluginHost would never load, and rejected
		// the real ids such as "mokadocs-repl".
		List<string> ignored = DoctorChecks.FindIgnoredPluginDeclarations(config.Plugins);
		List<string> unknown = DoctorChecks.FindUnknownPlugins(config.Plugins, outcome.RegisteredPluginIds);
		if (ignored.Count > 0 || unknown.Count > 0)
		{
			report.Warn("Plugins", $"{ignored.Count + unknown.Count} plugin declaration(s) will not load");
			foreach (string problem in ignored)
			{
				Report.Detail(problem);
			}

			foreach (string name in unknown)
			{
				Report.Detail($"{name}: no plugin has this id");
			}

			if (unknown.Count > 0)
			{
				Report.Detail($"available: {string.Join(", ", outcome.RegisteredPluginIds.Order())}");
			}

			return;
		}

		var loaded = new HashSet<string>(outcome.LoadedPluginIds, StringComparer.OrdinalIgnoreCase);
		var failed = config.Plugins
			.Select(p => p.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name) && !loaded.Contains(name))
			.ToList();

		if (failed.Count > 0)
		{
			report.Warn("Plugins", $"{failed.Count} plugin(s) failed to initialize - run mokadocs validate -v");
			foreach (string? name in failed)
			{
				Report.Detail(name!);
			}

			return;
		}

		report.Pass("Plugins", $"{config.Plugins.Count} plugin(s) declared and loaded");
	}

	private static void CheckSearch(SiteConfig config, DryRunOutcome outcome, Report report)
	{
		// Disabling search is a deliberate choice, not a problem, so it is not a warning.
		if (!config.Features.Search.Enabled)
		{
			report.Pass("Search", "Disabled in configuration");
			return;
		}

		if (outcome.Failure is not null)
		{
			Report.Skip("Search", "skipped because the build failed");
			return;
		}

		if (outcome.Context.SearchIndex is { Count: > 0 } index)
		{
			report.Pass("Search", $"Index built with {index.Count} entries");
		}
		else
		{
			report.Warn("Search", "Enabled, but the index has no entries");
		}
	}

	private static void CheckApiCoverage(SiteConfig config, DryRunOutcome outcome, bool verbose, Report report)
	{
		if (config.Content.Projects.Count == 0)
		{
			return;
		}

		if (outcome.Failure is not null)
		{
			Report.Skip("API Coverage", "skipped because the build failed");
			return;
		}

		// Measured on the model the build generates pages from. The previous check read
		// compiled .xml doc files, which the CLI never uses (it reads source through
		// Roslyn), so for most projects it silently never ran.
		if (outcome.Context.ApiModel is not { } api)
		{
			report.Warn("API Coverage", "No API model was produced - check the Projects row");
			return;
		}

		ApiCoverage coverage = DoctorChecks.ComputeApiCoverage(api);
		if (coverage.Missing.Count == 0)
		{
			report.Pass("API Coverage", $"100% - all {coverage.Total} types and members have a summary");
			return;
		}

		report.Warn("API Coverage",
			$"{coverage.Percent}% - {coverage.Missing.Count} of {coverage.Total} types and members have no summary");

		if (verbose)
		{
			const int limit = 25;
			foreach (string symbol in coverage.Missing.Take(limit))
			{
				Report.Detail(symbol);
			}

			if (coverage.Missing.Count > limit)
			{
				Report.Detail($"... and {coverage.Missing.Count - limit} more");
			}
		}
		else
		{
			Report.Detail("run with --verbose to list them");
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	///     Resolves a link written in a Markdown file to an absolute path on disk, or
	///     <c>null</c> when it points off-site.
	/// </summary>
	private static string? ResolveLocalReference(string url, string docsDir, string fileDir)
	{
		if (url.Length == 0
		    || url.StartsWith('#')
		    || url.StartsWith("//", StringComparison.Ordinal)
		    || url.Contains("://", StringComparison.Ordinal)
		    || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
		    || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		string path = DoctorChecks.NormalizeRoute(url);

		try
		{
			return url.StartsWith('/')
				? Path.GetFullPath(Path.Combine(docsDir, path.TrimStart('/')))
				: Path.GetFullPath(Path.Combine(fileDir, path));
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return null;
		}
	}

	/// <summary>
	///     Reads a file and remembers whether it had a UTF-8 byte order mark, so an auto-fix
	///     writes it back the way it found it.
	/// </summary>
	private static (string Content, Encoding Encoding) ReadPreservingEncoding(string path)
	{
		byte[] bytes = File.ReadAllBytes(path);
		bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
		var encoding = new UTF8Encoding(hasBom);
		int offset = hasBom ? 3 : 0;
		return (encoding.GetString(bytes, offset, bytes.Length - offset), encoding);
	}

	private static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = Process.Start(psi);
			if (process is null)
			{
				return (-1, "");
			}

			string output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();
			return (process.ExitCode, output);
		}
		catch
		{
			return (-1, "");
		}
	}

	#endregion

	/// <summary>
	///     Prints check rows and keeps the pass, warning and error counts that decide the
	///     exit code.
	/// </summary>
	private sealed class Report
	{
		public int Passed { get; private set; }

		public int Warnings { get; private set; }

		public int Errors { get; private set; }

		/// <summary>0 when clean, 1 with warnings, 2 with errors, as documented.</summary>
		public int ExitCode => Errors > 0 ? 2 : Warnings > 0 ? 1 : 0;

		public void Pass(string label, string detail)
		{
			Passed++;
			AnsiConsole.MarkupLine($"  [green]✓[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
		}

		public void Warn(string label, string detail)
		{
			Warnings++;
			AnsiConsole.MarkupLine($"  [yellow]⚠[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
		}

		public void Fail(string label, string detail)
		{
			Errors++;
			AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
		}

		/// <summary>A check that could not run. Not counted: the cause is already reported.</summary>
		public static void Skip(string label, string reason) =>
			AnsiConsole.MarkupLine($"  [dim]-[/] {Markup.Escape(label),-24} [dim]{Markup.Escape(reason)}[/]");

		public static void Detail(string detail) =>
			AnsiConsole.MarkupLine($"      [dim]->[/] {Markup.Escape(detail)}");
	}
}
