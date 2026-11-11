using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.Markdown;
using Moka.Docs.Plugins;

namespace Moka.Docs.Cli.Diagnostics;

/// <summary>
///     The parts of <c>mokadocs doctor</c> that are pure functions of their input, kept
///     apart from the command so they can be tested without a project on disk.
/// </summary>
internal static class DoctorChecks
{
	private static readonly MarkdownPipeline _linkPipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.UseYamlFrontMatter()
		.Build();

	private static readonly Regex _idAttribute = new(
		"""(?<=\s)(?:id|name)\s*=\s*(?:"(?<dq>[^"]*)"|'(?<sq>[^']*)')""",
		RegexOptions.Compiled);

	#region Links

	/// <summary>
	///     Every link and image in a Markdown document, with its 1-based line number.
	/// </summary>
	/// <remarks>
	///     Walks the Markdig AST instead of matching a regex against raw text. Syntax
	///     examples inside fenced code blocks and inline code spans are text, not links, so
	///     the parser never produces a <see cref="LinkInline" /> for them. The regex this
	///     replaced reported every one of those examples as a broken link.
	/// </remarks>
	internal static List<MarkdownLink> FindLinks(string markdown)
	{
		MarkdownDocument document = Markdig.Markdown.Parse(markdown, _linkPipeline);

		return document.Descendants<LinkInline>()
			.Where(link => !string.IsNullOrWhiteSpace(link.Url))
			.Select(link => new MarkdownLink(link.Url!, link.Line + 1, link.IsImage))
			.ToList();
	}

	/// <summary>
	///     Whether a URL points inside the site from its root: <c>/guide</c>, but not
	///     <c>//cdn.example.com</c>, <c>https://...</c>, <c>#anchor</c> or a relative path.
	/// </summary>
	internal static bool IsRootRelative(string url) =>
		url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal);

	/// <summary>
	///     Reduces a root-relative URL to the shape routes are stored in: query string and
	///     fragment removed, percent-encoding decoded, no trailing slash except on the root.
	/// </summary>
	internal static string NormalizeRoute(string url)
	{
		int cut = url.IndexOfAny(['?', '#']);
		string path = cut >= 0 ? url[..cut] : url;

		try
		{
			path = Uri.UnescapeDataString(path);
		}
		catch (UriFormatException)
		{
			// Leave malformed escapes alone; the path simply won't match a route.
		}

		if (path.Length > 1)
		{
			path = path.TrimEnd('/');
		}

		return path.Length == 0 ? "/" : path;
	}

	/// <summary>
	///     Checks every link in <paramref name="sources" /> against what the build produced.
	/// </summary>
	/// <remarks>
	///     <list type="bullet">
	///         <item>
	///             Paths are compared ordinally. Static hosts on Linux serve <c>/guide</c> and
	///             <c>/Guide</c> as different files, and the case-insensitive match this replaced
	///             passed links that 404 once deployed. A link that only differs in case says so.
	///         </item>
	///         <item>
	///             A <c>#fragment</c> pointing at a page must match an <c>id</c> (or an
	///             <c>&lt;a name&gt;</c>) in that page's rendered HTML, which covers heading ids,
	///             explicit ids and the theme's own anchors. <c>#top</c> always works.
	///         </item>
	///     </list>
	/// </remarks>
	/// <param name="pages">The pages the build produced (drafts included, so links to them can say so).</param>
	/// <param name="otherTargets">Other root-relative URLs that exist: assets, brand files, generated files.</param>
	/// <param name="sources">The Markdown files to check.</param>
	/// <returns>Broken links and links to anchors that don't exist, ready to print.</returns>
	internal static LinkCheckResult CheckLinks(IReadOnlyCollection<DocPage> pages, IEnumerable<string> otherTargets,
		IEnumerable<MarkdownSource> sources)
	{
		var published = pages.Where(p => p.FrontMatter.Visibility != PageVisibility.Draft).ToList();

		var targets = new HashSet<string>(published.Select(p => NormalizeRoute(p.Route)), StringComparer.Ordinal);
		AddSectionAncestors(targets);
		foreach (string target in otherTargets)
		{
			targets.Add(NormalizeRoute(target));
		}

		var spellings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string target in targets.Order(StringComparer.Ordinal))
		{
			spellings.TryAdd(target, target);
		}

		var draftRoutes = new HashSet<string>(
			pages.Where(p => p.FrontMatter.Visibility == PageVisibility.Draft).Select(p => NormalizeRoute(p.Route)),
			StringComparer.OrdinalIgnoreCase);

		var pageByRoute = new Dictionary<string, DocPage>(StringComparer.Ordinal);
		foreach (DocPage page in published)
		{
			pageByRoute[NormalizeRoute(page.Route)] = page;
		}

		var routeBySource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (DocPage page in published.Where(p => !string.IsNullOrEmpty(p.SourcePath)))
		{
			routeBySource[page.SourcePath!.Replace('\\', '/')] = NormalizeRoute(page.Route);
		}

		var idsByRoute = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		var result = new LinkCheckResult();

		foreach (MarkdownSource source in sources)
		{
			foreach (MarkdownLink link in FindLinks(source.Markdown))
			{
				int hash = link.Url.IndexOf('#');
				string fragment = hash >= 0 ? link.Url[(hash + 1)..] : "";

				string route;
				string written;
				if (link.Url.StartsWith('#'))
				{
					if (!routeBySource.TryGetValue(source.PathInDocs.Replace('\\', '/'), out string? own))
					{
						continue;
					}

					route = own;
					written = "";
				}
				else
				{
					bool rootRelative = IsRootRelative(link.Url);
					string? target = rootRelative
						? link.Url
						: RelativeLinks.ToRootRelative(link.Url, source.PathInDocs);
					if (target is null)
					{
						continue;
					}

					route = NormalizeRoute(target);
					written = rootRelative ? "" : $", written as {link.Url}";
				}

				string where = $"{source.DisplayPath}:{link.Line}";
				if (!targets.Contains(route))
				{
					string note = spellings.TryGetValue(route, out string? actual)
						? $", differs only in case from {actual}"
						: draftRoutes.Contains(route) ? ", links to a draft page"
						: link.IsImage ? ", image"
						: "";
					result.Broken.Add($"{route} ({where}{note}{written})");
					continue;
				}

				if (fragment.Length == 0 || fragment == "top" || !pageByRoute.TryGetValue(route, out DocPage? targetPage))
				{
					continue;
				}

				if (!idsByRoute.TryGetValue(route, out HashSet<string>? ids))
				{
					ids = ElementIds(targetPage.Content.Html);
					idsByRoute[route] = ids;
				}

				string decoded = DecodeFragment(fragment);
				if (!ids.Contains(decoded))
				{
					result.BrokenAnchors.Add($"{route}#{decoded} ({where}{written}): no heading or element on that page has this id");
				}
			}
		}

		return result;
	}

	/// <summary>Every <c>id</c> and anchor <c>name</c> in an HTML document, entity-decoded.</summary>
	internal static HashSet<string> ElementIds(string html)
	{
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (Match match in _idAttribute.Matches(html))
		{
			string value = match.Groups["dq"].Success ? match.Groups["dq"].Value : match.Groups["sq"].Value;
			ids.Add(WebUtility.HtmlDecode(value));
		}

		return ids;
	}

	private static string DecodeFragment(string fragment)
	{
		try
		{
			return Uri.UnescapeDataString(fragment);
		}
		catch (UriFormatException)
		{
			return fragment;
		}
	}

	/// <summary>
	///     Adds every ancestor of each route, so <c>/guide/markdown</c> also makes
	///     <c>/guide</c> reachable.
	/// </summary>
	/// <remarks>
	///     The output phase writes a redirect <c>index.html</c> into any section directory
	///     that has pages but no index page of its own, so a link to the section works in
	///     the built site even though no Markdown file produces that route.
	/// </remarks>
	internal static void AddSectionAncestors(ISet<string> routes)
	{
		foreach (string route in routes.ToList())
		{
			int slash = route.LastIndexOf('/');
			while (slash > 0)
			{
				routes.Add(route[..slash]);
				slash = route.LastIndexOf('/', slash - 1);
			}
		}
	}

	#endregion

	#region Front Matter

	/// <summary>
	///     Whether a document has a front matter block with a non-empty <c>title</c>.
	/// </summary>
	internal static bool HasTitle(string content)
	{
		if (!TryGetFrontMatterLines(content, out List<string> lines))
		{
			return false;
		}

		foreach (string line in lines)
		{
			string trimmed = line.TrimStart();
			if (!trimmed.StartsWith("title", StringComparison.Ordinal))
			{
				continue;
			}

			string afterKey = trimmed["title".Length..].TrimStart();
			if (afterKey.StartsWith(':') && afterKey[1..].Trim().Length > 0)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Returns the document with a <c>title</c> added to its front matter, or with a new
	///     front matter block prepended when it has none. Line endings are preserved.
	/// </summary>
	/// <remarks>
	///     Built by concatenation. The previous implementation passed the existing front
	///     matter through <c>Regex.Replace</c> as a replacement string, where <c>$0</c> and
	///     <c>$1</c> are substitution tokens, so front matter such as
	///     <c>description: Save $1 today</c> came back spliced into a garbled copy of itself.
	/// </remarks>
	internal static string AddTitle(string content, string title)
	{
		string nl = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
		string titleLine = "title: " + QuoteYamlScalar(title);

		if (TryGetFrontMatterLines(content, out _))
		{
			int openingEnd = content.IndexOf('\n') + 1;
			return content[..openingEnd] + titleLine + nl + content[openingEnd..];
		}

		return "---" + nl + titleLine + nl + "---" + nl + nl + content;
	}

	/// <summary>
	///     Derives a readable title from a Markdown file path. <c>index.md</c> takes its
	///     directory's name, since "Index" tells a reader nothing.
	/// </summary>
	internal static string TitleFromPath(string filePath, string docsDirectory)
	{
		string name = Path.GetFileNameWithoutExtension(filePath);

		if (name.Equals("index", StringComparison.OrdinalIgnoreCase))
		{
			string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
			string docsRoot = Path.GetFullPath(docsDirectory).TrimEnd(Path.DirectorySeparatorChar);
			name = dir is null || dir.Equals(docsRoot, StringComparison.OrdinalIgnoreCase)
				? "Home"
				: Path.GetFileName(dir);
		}

		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace('-', ' ').Replace('_', ' '));
	}

	private static bool TryGetFrontMatterLines(string content, out List<string> lines)
	{
		lines = [];
		string[] all = content.Split('\n');
		if (all.Length < 2 || all[0].TrimEnd('\r').Trim() != "---")
		{
			return false;
		}

		for (int i = 1; i < all.Length; i++)
		{
			string line = all[i].TrimEnd('\r');
			if (line.Trim() == "---")
			{
				return true;
			}

			lines.Add(line);
		}

		// An opening fence with no closing one is not front matter.
		lines.Clear();
		return false;
	}

	/// <summary>Quotes a YAML scalar when leaving it bare would change or break its meaning.</summary>
	internal static string QuoteYamlScalar(string value)
	{
		bool needsQuotes = value.Length == 0
		                   || value != value.Trim()
		                   || value.StartsWith('-')
		                   || value is "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase)
		                   || value.IndexOfAny([':', '#', '{', '}', '[', ']', ',', '&', '*', '?', '|', '>', '!', '%', '@', '`', '"', '\'']) >= 0;

		return needsQuotes
			? "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
			: value;
	}

	#endregion

	#region Plugins

	/// <summary>
	///     Declared plugin names that match no registered plugin id. Matching is
	///     case-insensitive, the same rule <c>PluginHost</c> uses when loading. Declarations with
	///     a <c>path</c> are left out: they are matched against their own assembly, and the build
	///     reports when that fails.
	/// </summary>
	internal static List<string> FindUnknownPlugins(IEnumerable<PluginDeclaration> declared,
		IEnumerable<string> registeredIds)
	{
		var known = new HashSet<string>(registeredIds, StringComparer.OrdinalIgnoreCase);

		return declared
			.Where(p => string.IsNullOrWhiteSpace(p.Path))
			.Select(p => p.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name) && !known.Contains(name))
			.Select(name => name!)
			.ToList();
	}

	/// <summary>
	///     Declarations the plugin host skips without reporting anything: entries with no name,
	///     path or not.
	/// </summary>
	internal static List<string> FindIgnoredPluginDeclarations(IEnumerable<PluginDeclaration> declared)
	{
		var problems = new List<string>();
		int index = 0;

		foreach (PluginDeclaration declaration in declared)
		{
			if (string.IsNullOrWhiteSpace(declaration.Name))
			{
				problems.Add(string.IsNullOrWhiteSpace(declaration.Path)
					? $"plugins[{index}] has no name"
					: $"plugins[{index}] has no name, so nothing is loaded from '{declaration.Path}'. Set it to the plugin's id");
			}

			index++;
		}

		return problems;
	}

	/// <summary>
	///     Declarations that loaded and matched a plugin, but whose plugin is not among the loaded
	///     ones: its <c>InitializeAsync</c> threw. Path declarations that failed to load are left
	///     out, since the build reports those as errors.
	/// </summary>
	internal static List<PluginDeclaration> FindPluginsThatFailedToInitialize(IEnumerable<PluginDeclaration> declared,
		IEnumerable<string> registeredIds, IEnumerable<string> loadedIds, IEnumerable<PluginLoadFailure> loadFailures)
	{
		var registered = new HashSet<string>(registeredIds, StringComparer.OrdinalIgnoreCase);
		var loaded = new HashSet<string>(loadedIds, StringComparer.OrdinalIgnoreCase);
		var failed = loadFailures.Select(f => (f.Name, f.Path)).ToHashSet();

		return declared
			.Where(p => !string.IsNullOrWhiteSpace(p.Name) && !loaded.Contains(p.Name))
			.Where(p => string.IsNullOrWhiteSpace(p.Path)
				? registered.Contains(p.Name!)
				: !failed.Contains((p.Name!, p.Path)))
			.ToList();
	}

	#endregion

	#region API Coverage

	/// <summary>
	///     Counts documented types and members in an API model.
	/// </summary>
	/// <remarks>
	///     A symbol counts as documented when it has a summary, or when its comment is an
	///     <c>&lt;inheritdoc/&gt;</c> tag. The second case matters because a tag whose base is
	///     a framework type (<c>object.ToString</c>, an <c>Exception</c> constructor) can never
	///     resolve, since the framework is not in the model. Those symbols are documented;
	///     reporting them as missing would ask authors to copy framework docs by hand.
	/// </remarks>
	internal static ApiCoverage ComputeApiCoverage(ApiReference api)
	{
		int total = 0;
		var missing = new List<string>();

		foreach (ApiNamespace ns in api.Namespaces)
		foreach (ApiType type in ns.Types)
		{
			total++;
			if (!IsDocumented(type.Documentation))
			{
				missing.Add(type.FullName);
			}

			foreach (ApiMember member in type.Members)
			{
				total++;
				if (!IsDocumented(member.Documentation))
				{
					missing.Add($"{type.FullName}.{member.Name}");
				}
			}
		}

		return new ApiCoverage(total - missing.Count, total, missing);
	}

	private static bool IsDocumented(XmlDocBlock? doc) =>
		doc is not null && (!string.IsNullOrWhiteSpace(doc.Summary) || doc.HasInheritDocTag);

	#endregion

	#region Paths

	/// <summary>
	///     Whether <paramref name="path" /> is inside <paramref name="directory" />. Uses a
	///     separator-terminated prefix so <c>_sitemap</c> is not mistaken for <c>_site</c>.
	/// </summary>
	internal static bool IsUnder(string path, string directory)
	{
		string full = Path.GetFullPath(path);
		string dir = Path.GetFullPath(directory);

		if (!dir.EndsWith(Path.DirectorySeparatorChar))
		{
			dir += Path.DirectorySeparatorChar;
		}

		return full.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
	}

	#endregion
}

/// <summary>A Markdown file whose links are checked.</summary>
/// <param name="DisplayPath">The path shown in reports, relative to the project.</param>
/// <param name="PathInDocs">The path relative to the docs folder, used to resolve relative links.</param>
/// <param name="Markdown">The file's content.</param>
internal readonly record struct MarkdownSource(string DisplayPath, string PathInDocs, string Markdown);

/// <summary>What <see cref="DoctorChecks.CheckLinks" /> found.</summary>
internal sealed class LinkCheckResult
{
	/// <summary>Links whose target page or file doesn't exist.</summary>
	public List<string> Broken { get; } = [];

	/// <summary>Links whose page exists but whose <c>#fragment</c> matches no id on it.</summary>
	public List<string> BrokenAnchors { get; } = [];
}

/// <summary>A link or image found in a Markdown document.</summary>
/// <param name="Url">The link target exactly as written.</param>
/// <param name="Line">1-based line number in the source file.</param>
/// <param name="IsImage">Whether this is an image rather than a hyperlink.</param>
internal readonly record struct MarkdownLink(string Url, int Line, bool IsImage);

/// <summary>Documentation coverage of an API model.</summary>
/// <param name="Documented">Types and members with a summary.</param>
/// <param name="Total">All types and members.</param>
/// <param name="Missing">Fully qualified names of symbols without a summary.</param>
internal readonly record struct ApiCoverage(int Documented, int Total, IReadOnlyList<string> Missing)
{
	/// <summary>Whole-number percentage documented; 100 when there is nothing to document.</summary>
	public int Percent => Total == 0 ? 100 : Documented * 100 / Total;
}
