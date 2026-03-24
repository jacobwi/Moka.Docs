using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Parses all discovered Markdown files into <see cref="DocPage" /> objects.
/// </summary>
public sealed class MarkdownParsePhase(
    MarkdownParser markdownParser,
    ILogger<MarkdownParsePhase> logger) : IBuildPhase
{
    /// <inheritdoc />
    public string Name => "MarkdownParse";

    /// <inheritdoc />
    public int Order => 400;

    /// <inheritdoc />
    public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
    {
        var docsPath = context.FileSystem.Path.GetFullPath(
            context.FileSystem.Path.Combine(context.RootDirectory, context.Config.Content.Docs));

        foreach (var relativePath in context.DiscoveredMarkdownFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fullPath = context.FileSystem.Path.Combine(docsPath, relativePath);
            try
            {
                var markdown = context.FileSystem.File.ReadAllText(fullPath);
                var result = markdownParser.Parse(markdown);

                var route = BuildRoute(relativePath, result.FrontMatter);

                var page = new DocPage
                {
                    FrontMatter = result.FrontMatter,
                    Content = new PageContent
                    {
                        Html = result.Html,
                        PlainText = result.PlainText
                    },
                    TableOfContents = result.TableOfContents,
                    SourcePath = relativePath,
                    Route = route,
                    Origin = PageOrigin.Markdown,
                    LastModified = GetLastModified(context, fullPath)
                };

                context.Pages.Add(page);
            }
            catch (Exception ex)
            {
                context.Diagnostics.Warning($"Failed to parse {relativePath}: {ex.Message}", Name);
                logger.LogWarning(ex, "Failed to parse Markdown file: {Path}", relativePath);
            }
        }

        logger.LogInformation("Parsed {Count} Markdown pages", context.Pages.Count);
        return Task.CompletedTask;
    }

    private static string BuildRoute(string relativePath, FrontMatter frontMatter)
    {
        // Use custom route from front matter if specified
        if (!string.IsNullOrEmpty(frontMatter.Route))
            return frontMatter.Route;

        // Convert file path to URL route
        var route = "/" + relativePath
            .Replace('\\', '/')
            .Replace(".md", "", StringComparison.OrdinalIgnoreCase);

        // index.md → parent directory route
        if (route.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
            route = route[..^6];

        if (string.IsNullOrEmpty(route))
            route = "/";

        return route;
    }

    private static DateTimeOffset? GetLastModified(BuildContext context, string fullPath)
    {
        try
        {
            var info = context.FileSystem.FileInfo.New(fullPath);
            return info.Exists ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero) : null;
        }
        catch
        {
            return null;
        }
    }
}