using System.Globalization;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;

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
	///     case-insensitive, the same rule <c>PluginHost</c> uses when loading.
	/// </summary>
	internal static List<string> FindUnknownPlugins(IEnumerable<PluginDeclaration> declared,
		IEnumerable<string> registeredIds)
	{
		var known = new HashSet<string>(registeredIds, StringComparer.OrdinalIgnoreCase);

		return declared
			.Select(p => p.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name) && !known.Contains(name))
			.Select(name => name!)
			.ToList();
	}

	/// <summary>
	///     Declarations the plugin host skips without reporting anything: entries that set
	///     <c>path</c>, which no host reads, and entries with no name.
	/// </summary>
	internal static List<string> FindIgnoredPluginDeclarations(IEnumerable<PluginDeclaration> declared)
	{
		var problems = new List<string>();
		int index = 0;

		foreach (PluginDeclaration declaration in declared)
		{
			if (!string.IsNullOrWhiteSpace(declaration.Path))
			{
				problems.Add(
					$"plugins[{index}]: path '{declaration.Path}' is ignored, the mokadocs CLI cannot load plugin assemblies");
			}
			else if (string.IsNullOrWhiteSpace(declaration.Name))
			{
				problems.Add($"plugins[{index}] has no name");
			}

			index++;
		}

		return problems;
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
