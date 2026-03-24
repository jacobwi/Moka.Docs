using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Builds the client-side search index from all pages.
/// </summary>
public sealed class SearchIndexPhase(ILogger<SearchIndexPhase> logger) : IBuildPhase
{
    /// <inheritdoc />
    public string Name => "SearchIndex";

    /// <inheritdoc />
    public int Order => 700;

    /// <inheritdoc />
    public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
    {
        if (!context.Config.Features.Search.Enabled)
        {
            logger.LogInformation("Search is disabled, skipping index build");
            return Task.CompletedTask;
        }

        var entries = new List<SearchEntry>();

        foreach (var page in context.Pages)
        {
            if (page.FrontMatter.Visibility == PageVisibility.Draft) continue;

            var category = page.Origin == PageOrigin.ApiGenerated ? "API Reference" : "Documentation";

            entries.Add(new SearchEntry
            {
                Title = page.FrontMatter.Title,
                Route = page.Route,
                Content = page.Content.PlainText,
                Category = category,
                Tags = page.FrontMatter.Tags
            });

            // Also add ToC entries as separate search entries for deeper matching
            foreach (var tocEntry in page.TableOfContents.Entries) AddTocEntries(entries, page, tocEntry);
        }

        context.SearchIndex = new SearchIndex { Entries = entries };

        logger.LogInformation("Built search index with {Count} entries", entries.Count);
        return Task.CompletedTask;
    }

    private static void AddTocEntries(List<SearchEntry> entries, DocPage page, TocEntry toc)
    {
        entries.Add(new SearchEntry
        {
            Title = page.FrontMatter.Title,
            Section = toc.Text,
            Route = $"{page.Route}#{toc.Id}",
            Content = "", // Individual section content extraction would require splitting HTML
            Category = page.Origin == PageOrigin.ApiGenerated ? "API Reference" : "Documentation"
        });

        foreach (var child in toc.Children) AddTocEntries(entries, page, child);
    }
}