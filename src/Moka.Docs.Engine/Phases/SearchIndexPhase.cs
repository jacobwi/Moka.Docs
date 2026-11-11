using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;
using Moka.Docs.Engine.Search;

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
		Dictionary<string, ApiType> apiTypesByRoute = ApiTypesByRoute(context);

		foreach (DocPage page in context.Pages)
		{
			if (!context.IncludeDrafts && page.FrontMatter.Visibility == PageVisibility.Draft)
			{
				continue;
			}

			// The blog's later index pages and tag pages only repeat the posts' titles and summaries.
			if (context.Blog is { } blog && blog.IsListing(page.Route) && page.Route != blog.Route)
			{
				continue;
			}

			string category = Category(context, page);

			// A type page's headings are its member sections, which made search entries such as "Methods"
			// with no text and left the members themselves unsearchable.
			if (page.Origin == PageOrigin.ApiGenerated && apiTypesByRoute.TryGetValue(page.Route, out ApiType? type))
			{
				entries.Add(PageEntry(page, category, null));
				entries.AddRange(ApiPageRenderer.BuildMemberSearchEntries(type, page.Route, page.FrontMatter.Title));
				continue;
			}

			var headingIds = new HashSet<string>(StringComparer.Ordinal);
			CollectIds(page.TableOfContents.Entries, headingIds);
			SectionSplit split = SearchText.Split(page.Content.Html, headingIds);

			entries.Add(PageEntry(page, category, split.Intro.Code));
			foreach (TocEntry tocEntry in page.TableOfContents.Entries)
			{
				AddTocEntries(entries, page, tocEntry, split, category);
			}
		}

		context.SearchIndex = new SearchIndex { Entries = entries };

		logger.LogInformation("Built search index with {Count} entries", entries.Count);
		return Task.CompletedTask;
	}

	private static Dictionary<string, ApiType> ApiTypesByRoute(BuildContext context)
	{
		var types = new Dictionary<string, ApiType>(StringComparer.Ordinal);
		if (context.ApiModel is null)
		{
			return types;
		}

		var allTypes = context.ApiModel.Namespaces.SelectMany(ns => ns.Types).ToList();
		ApiTypeRoutes routes = ApiTypeRoutes.For(allTypes);
		foreach (ApiType type in allTypes)
		{
			types.TryAdd(routes.Route(type), type);
		}

		return types;
	}

	private static string Category(BuildContext context, DocPage page)
	{
		if (page.Origin == PageOrigin.ApiGenerated)
		{
			return "API Reference";
		}

		return context.Blog is { } blog && (blog.FindPost(page.Route) is not null || page.Route == blog.Route)
			? "Blog"
			: "Documentation";
	}

	private static SearchEntry PageEntry(DocPage page, string category, string? code) => new()
	{
		Title = page.FrontMatter.Title,
		Route = page.Route,
		Content = page.Content.PlainText,
		Code = string.IsNullOrEmpty(code) ? null : code,
		Category = category,
		Tags = page.FrontMatter.Tags
	};

	private static void CollectIds(IEnumerable<TocEntry> toc, HashSet<string> ids)
	{
		foreach (TocEntry entry in toc)
		{
			ids.Add(entry.Id);
			CollectIds(entry.Children, ids);
		}
	}

	private static void AddTocEntries(List<SearchEntry> entries, DocPage page, TocEntry toc, SectionSplit split, string category)
	{
		SectionText? section = split.Sections.GetValueOrDefault(toc.Id);
		entries.Add(new SearchEntry
		{
			Title = page.FrontMatter.Title,
			Section = toc.Text,
			Route = $"{page.Route}#{toc.Id}",
			Content = section?.Text ?? "",
			Code = string.IsNullOrEmpty(section?.Code) ? null : section.Code,
			Category = category
		});

		foreach (TocEntry child in toc.Children)
		{
			AddTocEntries(entries, page, child, split, category);
		}
	}
}
