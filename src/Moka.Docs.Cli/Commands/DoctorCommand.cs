using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Configuration;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Diagnoses issues in a MokaDocs documentation project.
/// </summary>
internal static class DoctorCommand
{
    /// <summary>Creates the doctor command.</summary>
    public static Command Create()
    {
        var fixOption = new Option<bool>("--fix")
            { Description = "Attempt to auto-fix issues (e.g., add missing front matter titles)" };
        var configOption = new Option<string?>("--config")
            { Description = "Path to config file (default: mokadocs.yaml)" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Show detailed output for each check" };

        var command = new Command("doctor", "Diagnose issues in your documentation project")
        {
            fixOption,
            configOption,
            verboseOption
        };

        command.SetAction(async (parseResult, _) =>
        {
            var fix = parseResult.GetValue(fixOption);
            var configPath = parseResult.GetValue(configOption);
            var verbose = parseResult.GetValue(verboseOption);

            var rootDir = Directory.GetCurrentDirectory();
            var resolvedConfigPath = configPath ?? Path.Combine(rootDir, "mokadocs.yaml");

            AnsiConsole.MarkupLine("[bold blue]mokadocs doctor[/] — Diagnosing your documentation project...");
            AnsiConsole.WriteLine();

            var passed = 0;
            var warnings = 0;
            var errors = 0;

            // 1. Config validation
            SiteConfig? config = null;
            try
            {
                var fs = new FileSystem();
                var reader = new SiteConfigReader(fs);
                config = reader.Read(resolvedConfigPath);
                PrintPass("Configuration", $"{Path.GetFileName(resolvedConfigPath)} found and valid");
                passed++;
            }
            catch (FileNotFoundException)
            {
                PrintError("Configuration", $"{Path.GetFileName(resolvedConfigPath)} not found");
                errors++;
            }
            catch (SiteConfigException ex)
            {
                PrintError("Configuration", $"Invalid config: {Markup.Escape(ex.Message)}");
                errors++;
            }

            // 2. .NET SDK check
            try
            {
                var sdkVersion = await GetDotnetSdkVersionAsync();
                if (sdkVersion is not null)
                {
                    PrintPass(".NET SDK", $"{sdkVersion} installed");
                    passed++;
                }
                else
                {
                    PrintError(".NET SDK", "dotnet SDK not found on PATH");
                    errors++;
                }
            }
            catch
            {
                PrintError(".NET SDK", "Could not detect .NET SDK");
                errors++;
            }

            // 3. Projects check
            if (config is not null)
            {
                var projects = config.Content.Projects;
                if (projects.Count > 0)
                {
                    var allExist = true;
                    var missingProjects = new List<string>();
                    foreach (var proj in projects)
                    {
                        var projPath = Path.GetFullPath(Path.Combine(rootDir, proj.Path));
                        if (!File.Exists(projPath))
                        {
                            allExist = false;
                            missingProjects.Add(proj.Path);
                        }
                    }

                    if (allExist)
                    {
                        PrintPass("Projects", $"{projects.Count} project(s) found");
                        passed++;

                        if (verbose)
                        {
                            // Try building the first project
                            var firstProj = Path.GetFullPath(Path.Combine(rootDir, projects[0].Path));
                            var buildResult = await RunProcessAsync("dotnet", $"build \"{firstProj}\" --nologo -v q");
                            if (buildResult.ExitCode == 0)
                                PrintDetail("First project builds successfully");
                            else
                                PrintDetail("First project build had issues");
                        }
                    }
                    else
                    {
                        PrintError("Projects", $"{missingProjects.Count} project(s) not found");
                        errors++;
                        foreach (var mp in missingProjects)
                            PrintDetail(mp);
                    }
                }
                else
                {
                    PrintWarn("Projects", "No projects configured in content.projects");
                    warnings++;
                }
            }

            // 4. XML Documentation check
            if (config is not null && config.Content.Projects.Count > 0)
            {
                var xmlMissing = new List<string>();
                foreach (var proj in config.Content.Projects)
                {
                    var projPath = Path.GetFullPath(Path.Combine(rootDir, proj.Path));
                    if (!File.Exists(projPath)) continue;

                    var projDir = Path.GetDirectoryName(projPath)!;
                    var projName = Path.GetFileNameWithoutExtension(projPath);
                    // Check common output locations for XML doc files
                    var xmlPaths = new[]
                    {
                        Path.Combine(projDir, "bin", "Debug", "**", $"{projName}.xml"),
                        Path.Combine(projDir, $"{projName}.xml")
                    };

                    var found = false;
                    var binDir = Path.Combine(projDir, "bin");
                    if (Directory.Exists(binDir))
                    {
                        var xmlFiles = Directory.GetFiles(binDir, $"{projName}.xml", SearchOption.AllDirectories);
                        if (xmlFiles.Length > 0) found = true;
                    }

                    if (!found) xmlMissing.Add(proj.Path);
                }

                if (xmlMissing.Count == 0)
                {
                    PrintPass("XML Documentation", "XML doc files found for all projects");
                    passed++;
                }
                else
                {
                    PrintWarn("XML Documentation",
                        $"{xmlMissing.Count} project(s) missing XML doc files (build with GenerateDocumentationFile)");
                    warnings++;
                    if (verbose)
                        foreach (var m in xmlMissing)
                            PrintDetail(m);
                }
            }

            // 5. Docs folder check
            string? docsDir = null;
            var markdownFiles = Array.Empty<string>();
            if (config is not null)
            {
                docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
                if (Directory.Exists(docsDir))
                {
                    markdownFiles = Directory.GetFiles(docsDir, "*.md", SearchOption.AllDirectories);
                    PrintPass("Docs Folder", $"{markdownFiles.Length} markdown file(s) in {config.Content.Docs}");
                    passed++;
                }
                else
                {
                    PrintError("Docs Folder", $"Docs directory not found: {config.Content.Docs}");
                    errors++;
                }
            }

            // 6. Broken links check
            if (docsDir is not null && markdownFiles.Length > 0)
            {
                var brokenLinks = new List<(string file, int line, string link)>();
                var internalLinkPattern = new Regex(@"\[([^\]]*)\]\((/[^)#]+)", RegexOptions.Compiled);

                // Collect all known routes from markdown files
                var knownRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var mdFile in markdownFiles)
                {
                    var rel = Path.GetRelativePath(docsDir, mdFile);
                    var route = "/" + rel.Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(".md", "", StringComparison.OrdinalIgnoreCase);
                    if (route.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
                        route = route[..^6];
                    if (route == "") route = "/";
                    knownRoutes.Add(route);
                }

                foreach (var mdFile in markdownFiles)
                {
                    var lines = File.ReadAllLines(mdFile);
                    var relFile = Path.GetRelativePath(rootDir, mdFile);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var matches = internalLinkPattern.Matches(lines[i]);
                        foreach (Match match in matches)
                        {
                            var linkPath = match.Groups[2].Value.TrimEnd('/');
                            if (linkPath == "") linkPath = "/";
                            // Check if route exists in known routes or as a file
                            if (!knownRoutes.Contains(linkPath))
                                // Also check if it could be an API-generated page path (starts with /api/)
                                if (!linkPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                                    brokenLinks.Add((relFile, i + 1, linkPath));
                        }
                    }
                }

                if (brokenLinks.Count == 0)
                {
                    PrintPass("Broken Links", "No broken internal links found");
                    passed++;
                }
                else
                {
                    PrintError("Broken Links", $"{brokenLinks.Count} broken internal link(s) found");
                    errors++;
                    foreach (var (file, line, link) in brokenLinks)
                        PrintDetail($"{link} (referenced in {file}:{line})");
                }
            }

            // 7. Front matter check
            if (docsDir is not null && markdownFiles.Length > 0)
            {
                var missingTitle = new List<string>();
                var frontMatterRegex =
                    new Regex(@"^---\s*\n(.*?)\n---", RegexOptions.Singleline | RegexOptions.Compiled);
                var titleRegex = new Regex(@"^title\s*:", RegexOptions.Multiline | RegexOptions.Compiled);

                foreach (var mdFile in markdownFiles)
                {
                    var content = File.ReadAllText(mdFile);
                    var fmMatch = frontMatterRegex.Match(content);
                    var relFile = Path.GetRelativePath(rootDir, mdFile);

                    if (!fmMatch.Success || !titleRegex.IsMatch(fmMatch.Groups[1].Value))
                    {
                        missingTitle.Add(relFile);

                        if (fix)
                        {
                            // Auto-fix: add title from filename
                            var fileName = Path.GetFileNameWithoutExtension(mdFile);
                            var title = CultureInfo.CurrentCulture.TextInfo
                                .ToTitleCase(fileName.Replace('-', ' ').Replace('_', ' '));

                            if (fmMatch.Success)
                            {
                                // Front matter exists but no title — inject title
                                var newFm = $"---\ntitle: {title}\n{fmMatch.Groups[1].Value}\n---";
                                content = frontMatterRegex.Replace(content, newFm, 1);
                            }
                            else
                            {
                                // No front matter at all — prepend
                                content = $"---\ntitle: {title}\n---\n\n{content}";
                            }

                            File.WriteAllText(mdFile, content);
                        }
                    }
                }

                if (missingTitle.Count == 0)
                {
                    PrintPass("Front Matter", "All pages have titles");
                    passed++;
                }
                else
                {
                    if (fix)
                    {
                        PrintWarn("Front Matter",
                            $"{missingTitle.Count} page(s) were missing titles (auto-fixed)");
                        warnings++;
                    }
                    else
                    {
                        PrintWarn("Front Matter",
                            $"{missingTitle.Count} page(s) missing title in front matter");
                        warnings++;
                    }

                    if (verbose)
                        foreach (var f in missingTitle)
                            PrintDetail(f);
                }
            }

            // 8. Orphan images check
            if (docsDir is not null && markdownFiles.Length > 0)
            {
                var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".bmp", ".ico" };

                var imageFiles = Directory.Exists(docsDir)
                    ? Directory.GetFiles(docsDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
                        .ToList()
                    : [];

                if (imageFiles.Count > 0)
                {
                    // Collect all image references from markdown files
                    var referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var imgPattern = new Regex(@"!\[[^\]]*\]\(([^)]+)\)|src\s*=\s*""([^""]+)""", RegexOptions.Compiled);

                    foreach (var mdFile in markdownFiles)
                    {
                        var content = File.ReadAllText(mdFile);
                        var mdDir = Path.GetDirectoryName(mdFile)!;
                        foreach (Match match in imgPattern.Matches(content))
                        {
                            var imgRef = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                            imgRef = imgRef.Split('?')[0].Split('#')[0]; // Strip query/fragment

                            // Resolve relative paths
                            string resolved;
                            if (imgRef.StartsWith('/'))
                                resolved = Path.GetFullPath(Path.Combine(docsDir, imgRef.TrimStart('/')));
                            else
                                resolved = Path.GetFullPath(Path.Combine(mdDir, imgRef));

                            referencedImages.Add(resolved);
                        }
                    }

                    var orphans = imageFiles.Where(img => !referencedImages.Contains(img)).ToList();

                    if (orphans.Count == 0)
                    {
                        PrintPass("Orphan Images", $"All {imageFiles.Count} image(s) are referenced");
                        passed++;
                    }
                    else
                    {
                        PrintWarn("Orphan Images", $"{orphans.Count} unreferenced image(s)");
                        warnings++;
                        foreach (var orphan in orphans)
                            PrintDetail(Path.GetRelativePath(rootDir, orphan));
                    }
                }
                else
                {
                    PrintPass("Orphan Images", "No images found in docs folder");
                    passed++;
                }
            }

            // 9. Plugin availability check
            if (config is not null)
            {
                var declaredPlugins = config.Plugins;
                if (declaredPlugins.Count > 0)
                {
                    // We can check that plugin names are among the known built-in plugins
                    var knownPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { "openapi", "repl", "blazor-preview", "changelog" };

                    var unknownPlugins = declaredPlugins
                        .Where(p => p.Name is not null && !knownPlugins.Contains(p.Name))
                        .Select(p => p.Name!)
                        .ToList();

                    if (unknownPlugins.Count == 0)
                    {
                        PrintPass("Plugins", $"{declaredPlugins.Count} plugin(s) declared and available");
                        passed++;
                    }
                    else
                    {
                        PrintWarn("Plugins",
                            $"{unknownPlugins.Count} unknown plugin(s) declared");
                        warnings++;
                        foreach (var p in unknownPlugins)
                            PrintDetail(p);
                    }
                }
                else
                {
                    PrintPass("Plugins", "No plugins declared");
                    passed++;
                }
            }

            // 10. Search index check
            if (config is not null)
            {
                if (config.Features.Search.Enabled)
                {
                    PrintPass("Search", $"Search enabled (provider: {config.Features.Search.Provider})");
                    passed++;
                }
                else
                {
                    PrintWarn("Search", "Search is disabled in configuration");
                    warnings++;
                }
            }

            // 11. API coverage check
            if (config is not null && config.Content.Projects.Count > 0)
            {
                var totalMissingSummary = 0;
                var totalTypes = 0;

                foreach (var proj in config.Content.Projects)
                {
                    var projPath = Path.GetFullPath(Path.Combine(rootDir, proj.Path));
                    if (!File.Exists(projPath)) continue;

                    var projDir = Path.GetDirectoryName(projPath)!;
                    var projName = Path.GetFileNameWithoutExtension(projPath);
                    var binDir = Path.Combine(projDir, "bin");

                    if (!Directory.Exists(binDir)) continue;

                    var xmlFiles = Directory.GetFiles(binDir, $"{projName}.xml", SearchOption.AllDirectories);
                    if (xmlFiles.Length == 0) continue;

                    // Parse the XML doc file to count types and missing summaries
                    try
                    {
                        var xmlContent = File.ReadAllText(xmlFiles[0]);
                        var memberPattern = new Regex(@"<member\s+name=""T:([^""]+)""", RegexOptions.Compiled);
                        var summaryPattern = new Regex(
                            @"<member\s+name=""T:([^""]+)""[^>]*>\s*<summary>",
                            RegexOptions.Compiled | RegexOptions.Singleline);

                        var allTypes = memberPattern.Matches(xmlContent).Count;
                        var typesWithSummary = summaryPattern.Matches(xmlContent).Count;

                        totalTypes += allTypes;
                        totalMissingSummary += allTypes - typesWithSummary;
                    }
                    catch
                    {
                        // Skip if we can't parse the XML
                    }
                }

                if (totalTypes > 0)
                {
                    var coverage = totalTypes > 0
                        ? (totalTypes - totalMissingSummary) * 100 / totalTypes
                        : 100;

                    if (totalMissingSummary == 0)
                    {
                        PrintPass("API Coverage", $"100% — all {totalTypes} public type(s) have XML doc summaries");
                        passed++;
                    }
                    else
                    {
                        PrintWarn("API Coverage",
                            $"{coverage}% — {totalMissingSummary} public type(s) missing <summary>");
                        warnings++;
                    }
                }
            }

            // Summary
            AnsiConsole.WriteLine();
            var resultColor = errors > 0 ? "red" : warnings > 0 ? "yellow" : "green";
            AnsiConsole.MarkupLine(
                $"  [{resultColor}]Result: {passed} passed, {warnings} warning(s), {errors} error(s)[/]");

            return errors > 0 ? 2 : warnings > 0 ? 1 : 0;
        });

        return command;
    }

    private static void PrintPass(string label, string detail)
    {
        AnsiConsole.MarkupLine($"  [green]✓[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
    }

    private static void PrintWarn(string label, string detail)
    {
        AnsiConsole.MarkupLine($"  [yellow]⚠[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
    }

    private static void PrintError(string label, string detail)
    {
        AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(label),-24} {Markup.Escape(detail)}");
    }

    private static void PrintDetail(string detail)
    {
        AnsiConsole.MarkupLine($"      → {Markup.Escape(detail)}");
    }

    private static async Task<string?> GetDotnetSdkVersionAsync()
    {
        var result = await RunProcessAsync("dotnet", "--version");
        return result.ExitCode == 0 ? result.Output.Trim() : null;
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
            if (process is null) return (-1, "");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, "");
        }
    }
}