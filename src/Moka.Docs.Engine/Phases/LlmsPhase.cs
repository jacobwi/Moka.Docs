using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Stages a Markdown copy of each public page next to its HTML, and <c>llms.txt</c> and
///     <c>llms-full.txt</c> at the site root (the format at https://llmstxt.org), when
///     <c>build.llms</c> is on. Nothing is staged in a dry run.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 A Markdown page's copy is its source without front matter, with Markdown links and
///                 images made absolute to the site. MokaDocs blocks and includes stay as written.
///             </description>
///         </item>
///         <item>
///             <description>
///                 A C# API type page's copy is rendered from the API model, as is the API index. Other
///                 generated pages (OpenAPI, Python, blog listings) get no copy; <c>llms.txt</c> links
///                 their HTML.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Only public pages count: drafts, hidden pages and pages a version or feature gate
///                 dropped get nothing. A <c>llms.txt</c> or <c>llms-full.txt</c> in the docs folder is
///                 published instead of the generated one.
///             </description>
///         </item>
///     </list>
/// </remarks>
public sealed class LlmsPhase(MarkdownParser markdownParser, ILogger<LlmsPhase> logger) : IBuildPhase
{
	/// <summary>The index file, relative to the output directory.</summary>
	public const string IndexPath = "llms.txt";

	/// <summary>The file with every page's Markdown, relative to the output directory.</summary>
	public const string FullPath = "llms-full.txt";

	private const string _apiIndexRoute = "/api";

	private static readonly UTF8Encoding _utf8 = new(false);

	/// <inheritdoc />
	public string Name => "Llms";

	/// <inheritdoc />
	public int Order => 800;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (!context.Config.Build.Llms || context.DryRun)
		{
			return Task.CompletedTask;
		}

		var site = new SiteLinks(context.Config) { ResolveXref = ApiCrefResolver.XrefResolver(context.ApiModel) };
		List<DocPage> pages = PublicPages(context);
		Dictionary<string, string> copies = StageCopies(context, pages, site, ct);
		List<(string Title, List<DocPage> Pages)> sections = Sections(context.Navigation, pages);

		StageFile(context, IndexPath, BuildIndex(context.Config, sections, copies, site));
		StageFile(context, FullPath, BuildFull(context.Config, sections, copies));

		logger.LogInformation("Staged {Copies} Markdown page copies, {Index} and {Full}", copies.Count, IndexPath, FullPath);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Where the Markdown copy of the page at <paramref name="route" /> goes, relative to the output
	///     directory: <c>/guide/intro</c> gives <c>guide/intro.md</c>, the home page <c>index.md</c>.
	/// </summary>
	/// <param name="route">The page route.</param>
	/// <returns>The relative path.</returns>
	public static string MarkdownPath(string route)
	{
		string trimmed = route.Trim('/');
		return trimmed.Length == 0 ? "index.md" : trimmed + ".md";
	}

	/// <summary>
	///     The site-relative URLs of the Markdown copies staged for this build, keyed by page route,
	///     for <c>page.markdown_url</c>. Empty when <c>build.llms</c> is off or nothing was staged.
	/// </summary>
	/// <param name="context">The build, after this phase ran.</param>
	/// <returns>The URLs, without the base path.</returns>
	public static IReadOnlyDictionary<string, string> StagedMarkdownUrls(BuildContext context)
	{
		var urls = new Dictionary<string, string>(StringComparer.Ordinal);
		if (!context.Config.Build.Llms)
		{
			return urls;
		}

		foreach (DocPage page in context.Pages)
		{
			string path = MarkdownPath(page.Route);
			if (page.FrontMatter.Visibility == PageVisibility.Public && context.DeferredOutputFiles.ContainsKey(path))
			{
				urls.TryAdd(page.Route, "/" + path);
			}
		}

		return urls;
	}

	/// <summary>
	///     Public pages that the output phase writes, one per route: the last, as the output keeps.
	/// </summary>
	private static List<DocPage> PublicPages(BuildContext context)
	{
		var byRoute = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var pages = new List<DocPage?>();
		foreach (DocPage page in context.Pages)
		{
			if (page.FrontMatter.Visibility != PageVisibility.Public
			    || OutputPaths.FindUnsafeRouteProblem(page.Route) is not null
			    || (OperatingSystem.IsWindows() && OutputPaths.FindNonPortableRouteProblem(page.Route) is not null))
			{
				continue;
			}

			string key = "/" + page.Route.Trim('/');
			if (byRoute.TryGetValue(key, out int earlier))
			{
				pages[earlier] = null;
			}

			byRoute[key] = pages.Count;
			pages.Add(page);
		}

		return pages.OfType<DocPage>().ToList();
	}

	private Dictionary<string, string> StageCopies(BuildContext context, List<DocPage> pages, SiteLinks site,
		CancellationToken ct)
	{
		IFileSystem fs = context.FileSystem;
		string docsPath = fs.Path.GetFullPath(fs.Path.Combine(context.RootDirectory, context.Config.Content.Docs));
		List<ApiType> allTypes = context.ApiModel?.Namespaces.SelectMany(ns => ns.Types).ToList() ?? [];
		ApiTypeRoutes routes = ApiTypeRoutes.For(allTypes);
		var typesByRoute = new Dictionary<string, ApiType>(StringComparer.Ordinal);
		foreach (ApiType type in allTypes)
		{
			typesByRoute.TryAdd(routes.Route(type), type);
		}

		var copies = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (DocPage page in pages)
		{
			ct.ThrowIfCancellationRequested();
			string path = MarkdownPath(page.Route);
			if (context.DeferredOutputFiles.ContainsKey(path))
			{
				continue;
			}

			string? markdown = page.Origin switch
			{
				PageOrigin.ApiGenerated when typesByRoute.TryGetValue(page.Route, out ApiType? type) =>
					ApiPageRenderer.RenderMarkdown(type, allTypes),
				PageOrigin.ApiGenerated when page.Route == _apiIndexRoute && context.ApiModel is { } model =>
					ApiIndexMarkdown(model, routes, site),
				_ when !string.IsNullOrEmpty(page.SourcePath) => SourceMarkdown(fs, docsPath, page, site),
				_ => null
			};

			if (markdown is null)
			{
				continue;
			}

			context.DeferredOutputFiles[path] = _utf8.GetBytes(markdown);
			copies[page.Route] = markdown;
		}

		return copies;
	}

	private string? SourceMarkdown(IFileSystem fs, string docsPath, DocPage page, SiteLinks site)
	{
		string file = fs.Path.Combine(docsPath, page.SourcePath!);
		string source;
		try
		{
			source = fs.File.ReadAllText(file);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(ex, "No Markdown copy of {Route}: {File} could not be read", page.Route, file);
			return null;
		}

		string markdown = markdownParser.ToStandaloneMarkdown(source, page.SourcePath!, page.FrontMatter.Title, site.Root,
			site.ResolveXref);
		if (page.FrontMatter.Features.Count == 0)
		{
			return markdown;
		}

		// A landing page keeps most of its text in front matter, which the copy leaves out.
		var sb = new StringBuilder(markdown);
		string heading = page.FrontMatter.FeaturesTitle.Length > 0 ? page.FrontMatter.FeaturesTitle : "Features";
		sb.Append('\n').Append("## ").Append(OneLine(heading)).Append("\n\n");
		foreach (LandingFeature feature in page.FrontMatter.Features)
		{
			sb.Append("- **").Append(OneLine(feature.Title)).Append("**");
			if (feature.Description.Length > 0)
			{
				sb.Append(": ").Append(OneLine(feature.Description));
			}

			sb.Append('\n');
		}

		return sb.ToString();
	}

	private static string ApiIndexMarkdown(ApiReference model, ApiTypeRoutes routes, SiteLinks site)
	{
		var sb = new StringBuilder("# API Reference\n");
		foreach (ApiNamespace ns in model.Namespaces)
		{
			sb.Append("\n## ").Append(ns.Name).Append("\n\n");
			foreach (ApiType type in ns.Types)
			{
				string route = routes.Route(type);
				sb.Append("- [").Append(routes.DisplayName(type)).Append("](").Append(site.MarkdownUrl(route)).Append(')');
				if (ApiDocText.ToPlainText(type.Documentation?.Summary) is { Length: > 0 } summary)
				{
					sb.Append(": ").Append(summary);
				}

				sb.Append('\n');
			}
		}

		return sb.ToString();
	}

	/// <summary>
	///     The pages grouped as <c>llms.txt</c> lists them: one section per top-level sidebar entry with
	///     entries below it, holding that entry's page and every page below it in sidebar order; the
	///     top-level pages without entries below share a "Docs" section where the first of them is, or
	///     first with the home page when the sidebar doesn't list it; the public pages the sidebar
	///     doesn't list go last, under "Optional".
	/// </summary>
	private static List<(string Title, List<DocPage> Pages)> Sections(NavigationTree? navigation, List<DocPage> pages)
	{
		var byRoute = new Dictionary<string, DocPage>(StringComparer.OrdinalIgnoreCase);
		foreach (DocPage page in pages)
		{
			byRoute["/" + page.Route.Trim('/')] = page;
		}

		var listed = new HashSet<DocPage>(ReferenceEqualityComparer.Instance);
		var sections = new List<(string Title, List<DocPage> Pages)>();
		List<DocPage>? loose = null;

		foreach (NavigationNode node in navigation?.Items ?? [])
		{
			var found = new List<DocPage>();
			Collect(node);
			if (found.Count == 0)
			{
				continue;
			}

			if (node.Children.Count > 0)
			{
				sections.Add((node.Label, found));
			}
			else if (loose is null)
			{
				loose = found;
				sections.Add(("Docs", loose));
			}
			else
			{
				loose.AddRange(found);
			}

			void Collect(NavigationNode current)
			{
				if (!string.IsNullOrEmpty(current.Route)
				    && byRoute.TryGetValue("/" + current.Route.Trim('/'), out DocPage? page) && listed.Add(page))
				{
					found.Add(page);
				}

				foreach (NavigationNode child in current.Children)
				{
					Collect(child);
				}
			}
		}

		// The home page usually isn't in the sidebar, and it is where a reader starts.
		if (byRoute.TryGetValue("/", out DocPage? home) && listed.Add(home))
		{
			if (loose is null)
			{
				sections.Insert(0, ("Docs", [home]));
			}
			else
			{
				loose.Insert(0, home);
				int index = sections.FindIndex(s => ReferenceEquals(s.Pages, loose));
				(string Title, List<DocPage> Pages) docs = sections[index];
				sections.RemoveAt(index);
				sections.Insert(0, docs);
			}
		}

		List<DocPage> unlisted = pages.Where(p => !listed.Contains(p)).ToList();
		if (unlisted.Count > 0)
		{
			sections.Add(("Optional", unlisted));
		}

		return sections;
	}

	private static string BuildIndex(SiteConfig config, List<(string Title, List<DocPage> Pages)> sections,
		Dictionary<string, string> copies, SiteLinks site)
	{
		var sb = new StringBuilder();
		Header(sb, config);
		foreach ((string title, List<DocPage> pages) in sections)
		{
			sb.Append("## ").Append(OneLine(title)).Append("\n\n");
			foreach (DocPage page in pages)
			{
				string url = copies.ContainsKey(page.Route) ? site.MarkdownUrl(page.Route) : site.PageUrl(page.Route);
				sb.Append("- [").Append(LinkText(page.FrontMatter.Title)).Append("](").Append(url).Append(')');
				if (OneLine(page.FrontMatter.Description) is { Length: > 0 } description)
				{
					sb.Append(": ").Append(description);
				}

				sb.Append('\n');
			}

			sb.Append('\n');
		}

		return sb.ToString().TrimEnd() + "\n";
	}

	private static string BuildFull(SiteConfig config, List<(string Title, List<DocPage> Pages)> sections,
		Dictionary<string, string> copies)
	{
		var sb = new StringBuilder();
		Header(sb, config);
		foreach (DocPage page in sections.SelectMany(s => s.Pages))
		{
			if (copies.TryGetValue(page.Route, out string? markdown))
			{
				sb.Append(markdown.TrimEnd()).Append("\n\n");
			}
		}

		return sb.ToString().TrimEnd() + "\n";
	}

	private static void Header(StringBuilder sb, SiteConfig config)
	{
		sb.Append("# ").Append(OneLine(config.Site.Title)).Append("\n\n");
		if (OneLine(config.Site.Description) is { Length: > 0 } description)
		{
			sb.Append("> ").Append(description).Append("\n\n");
		}
	}

	/// <summary>
	///     Stages a root file unless the docs folder has its own, which the output copies as an asset,
	///     or a plugin staged one.
	/// </summary>
	private void StageFile(BuildContext context, string path, string content)
	{
		if (context.DiscoveredAssetFiles.Any(f => string.Equals(f.Replace('\\', '/'), path, StringComparison.OrdinalIgnoreCase)))
		{
			context.Diagnostics.Info($"The docs folder has its own {path}, which is published instead of a generated one", Name);
			return;
		}

		context.DeferredOutputFiles.TryAdd(path, _utf8.GetBytes(content));
	}

	private static string OneLine(string? text) =>
		string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	private static string LinkText(string title) => OneLine(title).Replace("[", "\\[").Replace("]", "\\]");

	/// <summary>The site's absolute URLs, or root-relative ones with the base path without <c>site.url</c>.</summary>
	private sealed class SiteLinks(SiteConfig config)
	{
		private readonly string _basePath = config.Build.BasePath;
		private readonly string _siteUrl = config.Site.Url;

		/// <summary>Resolves the xref: links of the Markdown copies to API pages.</summary>
		public Func<string, XrefTarget?>? ResolveXref { get; init; }

		/// <summary>What a link from the site root gets in front of it.</summary>
		public string Root { get; } = SiteUrls.AbsoluteFile(config.Site.Url, config.Build.BasePath, "") is { Length: > 0 } url
			? url.TrimEnd('/')
			: config.Build.BasePath == "/" ? "" : config.Build.BasePath;

		public string PageUrl(string route) =>
			!string.IsNullOrWhiteSpace(_siteUrl)
				? SiteUrls.Absolute(_siteUrl, _basePath, route)
				: route.Trim('/').Length == 0
					? Root + "/"
					: Root + "/" + Encode(route.Trim('/')) + "/";

		public string MarkdownUrl(string route) =>
			!string.IsNullOrWhiteSpace(_siteUrl)
				? SiteUrls.AbsoluteFile(_siteUrl, _basePath, "/" + MarkdownPath(route))
				: Root + "/" + Encode(MarkdownPath(route));

		private static string Encode(string path) => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
	}
}
