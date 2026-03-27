using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     MokaDocs plugin that reads an OpenAPI 3.0 JSON specification file and generates
///     beautiful endpoint documentation pages. Endpoints are grouped by tag, and each
///     group receives its own detail page with parameter tables, request/response schemas,
///     HTTP method badges, and example JSON payloads.
///     <para>
///         Configuration in <c>mokadocs.yaml</c>:
///         <code>
/// plugins:
///   - name: openapi
///     options:
///       spec: ./openapi.json
///       label: "REST API"
///       routePrefix: /rest-api
/// </code>
///     </para>
/// </summary>
public sealed class OpenApiPlugin : IMokaPlugin
{
	private string _label = "REST API";
	private string _routePrefix = "/api";
	private string _specPath = "";

	/// <inheritdoc />
	public string Id => "openapi";

	/// <inheritdoc />
	public string Name => "OpenAPI Documentation";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		IReadOnlyDictionary<string, object> options = context.Options;

		if (options.TryGetValue("spec", out object? specObj) && specObj is string specStr)
		{
			_specPath = specStr;
		}

		if (options.TryGetValue("label", out object? labelObj) && labelObj is string labelStr)
		{
			_label = labelStr;
		}

		if (options.TryGetValue("routePrefix", out object? prefixObj) && prefixObj is string prefixStr)
		{
			_routePrefix = prefixStr.TrimEnd('/');
		}

		if (string.IsNullOrWhiteSpace(_specPath))
		{
			context.LogWarning("OpenAPI plugin: 'spec' option is not set. " +
			                   "Will attempt auto-discovery of openapi.json or swagger.json.");
		}

		context.LogInfo($"OpenAPI plugin initialized (spec={_specPath}, label={_label}, routePrefix={_routePrefix})");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		string? specFullPath = ResolveSpecPath(buildContext);
		if (specFullPath is null)
		{
			context.LogWarning("OpenAPI plugin: No OpenAPI spec file found. Skipping.");
			return;
		}

		context.LogInfo($"OpenAPI plugin: Processing spec at {specFullPath}");

		string json;
		try
		{
			json = await File.ReadAllTextAsync(specFullPath, ct);
		}
		catch (Exception ex)
		{
			context.LogError($"OpenAPI plugin: Failed to read spec file '{specFullPath}': {ex.Message}");
			return;
		}

		OpenApiSpec spec;
		try
		{
			spec = OpenApiParser.Parse(json);
		}
		catch (Exception ex)
		{
			context.LogError($"OpenAPI plugin: Failed to parse spec file '{specFullPath}': {ex.Message}");
			return;
		}

		List<DocPage> pages = GeneratePages(spec, context);
		buildContext.Pages.AddRange(pages);

		context.LogInfo(
			$"OpenAPI plugin: Generated {pages.Count} pages ({spec.Endpoints.Count} endpoints) from '{specFullPath}'");
	}

	/// <summary>
	///     Resolves the OpenAPI spec file path. If the user specified a path in the config,
	///     resolves it relative to the project root. Otherwise, tries common file names.
	/// </summary>
	private string? ResolveSpecPath(BuildContext buildContext)
	{
		string root = buildContext.RootDirectory;

		// User-specified path
		if (!string.IsNullOrWhiteSpace(_specPath))
		{
			string full = Path.IsPathRooted(_specPath)
				? _specPath
				: Path.GetFullPath(Path.Combine(root, _specPath));

			return File.Exists(full) ? full : null;
		}

		// Auto-discovery (JSON and YAML)
		string[] candidates = ["openapi.json", "openapi.yaml", "openapi.yml", "swagger.json", "swagger.yaml"];
		string[] directories = [root, Path.Combine(root, buildContext.Config.Content.Docs)];

		foreach (string dir in directories)
		{
			if (!Directory.Exists(dir))
			{
				continue;
			}

			foreach (string candidate in candidates)
			{
				string path = Path.Combine(dir, candidate);
				if (File.Exists(path))
				{
					return path;
				}
			}
		}

		return null;
	}

	/// <summary>
	///     Generates all documentation pages from the parsed spec: one index page
	///     and one detail page per tag group.
	/// </summary>
	private List<DocPage> GeneratePages(OpenApiSpec spec, IPluginContext context)
	{
		var pages = new List<DocPage>();
		string inlineCss = OpenApiPageRenderer.GetInlineCss();

		// Group endpoints by tag
		var grouped = new Dictionary<string, List<OpenApiEndpoint>>(StringComparer.OrdinalIgnoreCase);
		foreach (OpenApiEndpoint ep in spec.Endpoints)
		{
			List<string> tags = ep.Tags.Count > 0 ? ep.Tags : ["Other"];
			foreach (string tag in tags)
			{
				if (!grouped.TryGetValue(tag, out List<OpenApiEndpoint>? list))
				{
					list = [];
					grouped[tag] = list;
				}

				list.Add(ep);
			}
		}

		// Index page
		string indexHtml = inlineCss + OpenApiPageRenderer.RenderIndexPage(spec, _routePrefix);
		pages.Add(new DocPage
		{
			FrontMatter = new FrontMatter
			{
				Title = _label,
				Description = spec.Description,
				Icon = "code"
			},
			Content = new PageContent
			{
				Html = indexHtml,
				PlainText = $"{_label} - {spec.Title} - {spec.Description}"
			},
			Route = _routePrefix,
			Origin = PageOrigin.ApiGenerated
		});

		// Per-tag detail pages
		int order = 1;
		foreach ((string tag, List<OpenApiEndpoint> endpoints) in grouped.OrderBy(kv => kv.Key))
		{
			string slug = Slugify(tag);
			string tagHtml = inlineCss + OpenApiPageRenderer.RenderTagPage(tag, endpoints);

			var plainTextParts = new List<string> { tag };
			foreach (OpenApiEndpoint ep in endpoints)
			{
				plainTextParts.Add($"{ep.Method} {ep.Path} {ep.Summary}");
			}

			pages.Add(new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = tag,
					Description = $"{_label} - {tag}",
					Order = order++
				},
				Content = new PageContent
				{
					Html = tagHtml,
					PlainText = string.Join(" ", plainTextParts)
				},
				Route = $"{_routePrefix}/{slug}",
				Origin = PageOrigin.ApiGenerated
			});

			context.LogInfo($"OpenAPI plugin: Generated page for tag '{tag}' with {endpoints.Count} endpoints");
		}

		return pages;
	}

	private static string Slugify(string value)
	{
		return value.ToLowerInvariant()
			.Replace(' ', '-')
			.Replace('/', '-')
			.Replace('.', '-');
	}
}
