using System.Globalization;
using System.Text;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     The routes, file names and anchors of the pages generated for one OpenAPI document, worked out once
///     so the index page, the tag pages, the models page and their tables of contents agree on them.
/// </summary>
internal sealed class OpenApiLayout
{
	/// <summary>The id of the Servers section on the index page.</summary>
	internal const string ServersAnchor = "servers";

	/// <summary>The id of the Authentication section on the index page.</summary>
	internal const string AuthenticationAnchor = "authentication";

	/// <summary>The id of the Webhooks section on the index page.</summary>
	internal const string WebhooksAnchor = "webhooks";

	/// <summary>The tag that endpoints without tags are listed under.</summary>
	internal const string UntaggedTag = "Other";

	// Well under the 255 characters a file name can have on Windows and Linux, with room for a "-2" suffix.
	private const int _maxSegmentLength = 100;

	private static readonly HashSet<string> _windowsDeviceNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"con", "prn", "aux", "nul",
		"com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
		"lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
	};

	private OpenApiLayout(string routePrefix) => RoutePrefix = routePrefix;

	/// <summary>The normalized route prefix: <c>/rest-api</c>, or empty for the site root.</summary>
	public string RoutePrefix { get; }

	/// <summary>The route of the index page as a link target.</summary>
	public string IndexUrl => RoutePrefix.Length == 0 ? "/" : RoutePrefix;

	/// <summary>One entry per tag page, sorted by tag name.</summary>
	public IReadOnlyList<OpenApiTagPageLayout> TagPages { get; private init; } = [];

	/// <summary>The id of each tag's section on the index page.</summary>
	public IReadOnlyDictionary<string, string> TagSectionAnchors { get; private init; } = new Dictionary<string, string>();

	/// <summary>Whether the index page has an Authentication section.</summary>
	public bool HasAuthentication => SchemeAnchors.Count > 0;

	/// <summary>The id of each security scheme's entry in the Authentication section, keyed by scheme name.</summary>
	public IReadOnlyDictionary<string, string> SchemeAnchors { get; private init; } = new Dictionary<string, string>();

	/// <summary>The route of the models page, or <c>null</c> when the document has no schemas.</summary>
	public string? ModelsRoute { get; private init; }

	/// <summary>The schema names in the order the models page lists them.</summary>
	public IReadOnlyList<string> ModelNames { get; private init; } = [];

	/// <summary>The id of each schema's entry on the models page, keyed by schema name.</summary>
	public IReadOnlyDictionary<string, string> ModelAnchors { get; private init; } = new Dictionary<string, string>();

	/// <summary>The file name the source document is published under, or <c>null</c> when it isn't.</summary>
	public string? DocumentFileName { get; private init; }

	/// <summary>The link to the published source document.</summary>
	public string? DocumentUrl => DocumentFileName is null ? null : $"{RoutePrefix}/{DocumentFileName}";

	/// <summary>The published document's path relative to the output directory.</summary>
	public string? DocumentOutputPath =>
		DocumentFileName is null ? null : RoutePrefix.Length == 0 ? DocumentFileName : $"{RoutePrefix.TrimStart('/')}/{DocumentFileName}";

	/// <summary>
	///     Works out the layout of the pages for <paramref name="spec" />.
	/// </summary>
	/// <param name="spec">The parsed document.</param>
	/// <param name="routePrefix">The normalized route prefix.</param>
	/// <param name="documentFileName">The name the source document is published under, or <c>null</c>.</param>
	internal static OpenApiLayout Create(OpenApiSpec spec, string routePrefix, string? documentFileName)
	{
		// Segments are handed out in document order, so of two tags that slug alike the first keeps the plain one.
		var segments = new AnchorSet();
		List<OpenApiTagPageLayout> tagPages = GroupByTag(spec.Endpoints)
			.Select(group => CreateTagPage(group.Tag, group.Endpoints, $"{routePrefix}/{segments.Add(TagSlug(group.Tag), "tag")}"))
			.ToList()
			.OrderBy(page => page.Tag, StringComparer.OrdinalIgnoreCase)
			.ToList();

		// The fixed section ids come first so a tag or scheme with the same name can't take them.
		var indexAnchors = new AnchorSet(spec.Webhooks.Count > 0
			? [ServersAnchor, AuthenticationAnchor, WebhooksAnchor]
			: [ServersAnchor, AuthenticationAnchor]);
		var schemeAnchors = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string scheme in spec.SecuritySchemes.Keys)
		{
			schemeAnchors[scheme] = indexAnchors.Add("auth-" + Slug(scheme), "auth");
		}

		var tagAnchors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (OpenApiTagPageLayout page in tagPages)
		{
			tagAnchors[page.Tag] = indexAnchors.Add(Slug(page.Tag), "tag");
		}

		var modelNames = spec.Schemas.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
		var modelAnchors = new Dictionary<string, string>(StringComparer.Ordinal);
		var modelAnchorSet = new AnchorSet();
		foreach (string name in modelNames)
		{
			modelAnchors[name] = modelAnchorSet.Add(Slug(name), "model");
		}

		return new OpenApiLayout(routePrefix)
		{
			TagPages = tagPages,
			TagSectionAnchors = tagAnchors,
			SchemeAnchors = schemeAnchors,
			ModelsRoute = modelNames.Count > 0 ? $"{routePrefix}/{ModelsSlug(tagPages)}" : null,
			ModelNames = modelNames,
			ModelAnchors = modelAnchors,
			DocumentFileName = documentFileName
		};
	}

	/// <summary>
	///     A tag page's layout, with an anchor for each endpoint that is unique on the page.
	/// </summary>
	internal static OpenApiTagPageLayout CreateTagPage(string tag, IEnumerable<OpenApiEndpoint> endpoints, string route)
	{
		var anchors = new AnchorSet();
		List<OpenApiEndpointLayout> entries = endpoints
			.Select(ep => new OpenApiEndpointLayout(ep, anchors.Add(Slug(ep.OperationId), Slug($"{ep.Method} {ep.Path}"))))
			.ToList();
		return new OpenApiTagPageLayout(tag, route, entries);
	}

	/// <summary>
	///     The endpoints of each tag, with untagged ones under <see cref="UntaggedTag" />, in the order the
	///     document first uses each tag.
	/// </summary>
	private static List<(string Tag, List<OpenApiEndpoint> Endpoints)> GroupByTag(IEnumerable<OpenApiEndpoint> endpoints)
	{
		var groups = new List<(string Tag, List<OpenApiEndpoint> Endpoints)>();
		var byTag = new Dictionary<string, List<OpenApiEndpoint>>(StringComparer.OrdinalIgnoreCase);
		foreach (OpenApiEndpoint ep in endpoints)
		{
			foreach (string tag in ep.Tags.Count > 0 ? ep.Tags : [UntaggedTag])
			{
				if (!byTag.TryGetValue(tag, out List<OpenApiEndpoint>? list))
				{
					list = [];
					byTag[tag] = list;
					groups.Add((tag, list));
				}

				list.Add(ep);
			}
		}

		return groups;
	}

	/// <summary>
	///     <c>models</c>, unless a tag page already has that route.
	/// </summary>
	private static string ModelsSlug(List<OpenApiTagPageLayout> tagPages)
	{
		var taken = new HashSet<string>(tagPages.Select(p => p.Route[(p.Route.LastIndexOf('/') + 1)..]),
			StringComparer.OrdinalIgnoreCase);
		return new AnchorSet(taken).Add("models", "models");
	}

	/// <summary>
	///     A tag's route segment, before it is made unique. Tags whose old segment (lowercase, with spaces,
	///     slashes and dots replaced by hyphens) holds only letters, digits and hyphens keep it, since
	///     existing sites link to those routes. Any other tag gets a strict slug of <c>a-z</c>, <c>0-9</c>
	///     and single hyphens, or <c>tag</c> when nothing is left.
	/// </summary>
	/// <remarks>
	///     A tag is free text and the segment becomes a file path, a link and a sitemap entry: a quote broke
	///     out of the sidebar's <c>href</c>, a colon or question mark failed the build on Windows, a tag
	///     shaped like <c>C:\dir</c> wrote outside the output folder, an ampersand broke the sitemap and
	///     <c>C#</c> linked to <c>c</c>.
	/// </remarks>
	internal static string TagSlug(string tag)
	{
		string legacy = tag.ToLowerInvariant().Replace(' ', '-').Replace('/', '-').Replace('.', '-');
		if (legacy.Length <= _maxSegmentLength && legacy.All(c => c == '-' || char.IsLetterOrDigit(c))
		                                       && !_windowsDeviceNames.Contains(legacy))
		{
			return legacy;
		}

		string decomposed;
		try
		{
			decomposed = tag.Normalize(NormalizationForm.FormD);
		}
		catch (ArgumentException)
		{
			// Normalize refuses a lone surrogate, which a JSON "\ud800" escape gives.
			decomposed = tag;
		}

		var sb = new StringBuilder(Math.Min(tag.Length, _maxSegmentLength));
		foreach (char c in decomposed)
		{
			if (sb.Length >= _maxSegmentLength)
			{
				break;
			}

			if (char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}

			char lower = char.ToLowerInvariant(c);
			if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
			{
				sb.Append(lower);
			}
			else if (sb.Length > 0 && sb[^1] != '-')
			{
				sb.Append('-');
			}
		}

		string slug = sb.ToString().TrimEnd('-');
		if (_windowsDeviceNames.Contains(slug))
		{
			return slug + "-tag";
		}

		return slug.Length > 0 ? slug : "tag";
	}

	/// <summary>
	///     An anchor id: words in lowercase joined by hyphens, with camelCase split into words, so
	///     <c>getTodoById</c> becomes <c>get-todo-by-id</c> and <c>GET /todos/{id}</c> becomes
	///     <c>get-todos-id</c>. Empty when the value has no letters or digits.
	/// </summary>
	internal static string Slug(string value)
	{
		var sb = new StringBuilder(value.Length + 8);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (!char.IsLetterOrDigit(c))
			{
				if (sb.Length > 0 && sb[^1] != '-')
				{
					sb.Append('-');
				}

				continue;
			}

			bool wordStart = char.IsUpper(c) && i > 0
			                                 && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])
			                                     || (char.IsUpper(value[i - 1]) && i + 1 < value.Length && char.IsLower(value[i + 1])));
			if (wordStart && sb.Length > 0 && sb[^1] != '-')
			{
				sb.Append('-');
			}

			sb.Append(char.ToLowerInvariant(c));
		}

		return sb.ToString().TrimEnd('-');
	}

	/// <summary>
	///     Hands out ids that are unique on one page, adding <c>-2</c>, <c>-3</c> and so on to a taken one.
	/// </summary>
	private sealed class AnchorSet(IEnumerable<string>? reserved = null)
	{
		private readonly HashSet<string> _taken = new(reserved ?? [], StringComparer.OrdinalIgnoreCase);

		public string Add(string preferred, string fallback)
		{
			string anchor = preferred.Length > 0 ? preferred : fallback.Length > 0 ? fallback : "section";
			string candidate = anchor;
			for (int n = 2; !_taken.Add(candidate); n++)
			{
				candidate = $"{anchor}-{n}";
			}

			return candidate;
		}
	}
}

/// <summary>A tag page: its tag, its route and its endpoints with their anchors.</summary>
/// <param name="Tag">The tag name.</param>
/// <param name="Route">The page's route.</param>
/// <param name="Endpoints">The endpoints, in document order.</param>
internal sealed record OpenApiTagPageLayout(string Tag, string Route, IReadOnlyList<OpenApiEndpointLayout> Endpoints);

/// <summary>An endpoint on a tag page, with the id of its heading.</summary>
/// <param name="Endpoint">The endpoint.</param>
/// <param name="Anchor">The id of the endpoint's heading, unique on the page.</param>
internal sealed record OpenApiEndpointLayout(OpenApiEndpoint Endpoint, string Anchor)
{
	/// <summary>The heading's text as the table of contents shows it, such as <c>GET /todos/{id}</c>.</summary>
	public string Title => $"{Endpoint.Method} {Endpoint.Path}";
}
