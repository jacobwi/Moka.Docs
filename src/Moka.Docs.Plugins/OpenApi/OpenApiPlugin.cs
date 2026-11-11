using System.Text;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     MokaDocs plugin that reads an OpenAPI document (2.0, 3.0 or 3.1, JSON or YAML) and generates
///     endpoint documentation pages. Endpoints are grouped by tag, and each group receives its own
///     detail page with parameter tables, request/response schemas, HTTP method badges, and example
///     JSON payloads. A models page lists the document's schemas. Schema names link to the API reference
///     page of the C# type with the same name, or else to the schema on the models page. The source
///     document is published as <c>{routePrefix}/openapi.json</c> (or <c>.yaml</c>) and linked from the
///     index page.
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
/// <remarks>
///     The <c>document</c> option takes the document's text instead of a file. The ASP.NET Core host
///     sets it to the document the application generates.
/// </remarks>
public sealed class OpenApiPlugin : IMokaPlugin
{
	/// <summary>The option that holds the document's text, used instead of <c>spec</c>.</summary>
	public const string DocumentOption = "document";

	/// <summary>The option that sets the URL prefix of the generated pages.</summary>
	public const string RoutePrefixOption = "routePrefix";

	/// <summary>The option that sets the title of the index page.</summary>
	public const string LabelOption = "label";

	/// <summary>The route prefix used when <see cref="RoutePrefixOption" /> is not set.</summary>
	public const string DefaultRoutePrefix = "/api";

	/// <summary>The label used when <see cref="LabelOption" /> is not set.</summary>
	public const string DefaultLabel = "REST API";

	private static readonly string[] _discoveredNames = ["openapi.json", "openapi.yaml", "openapi.yml", "swagger.json", "swagger.yaml"];

	/// <inheritdoc />
	public string Id => "openapi";

	/// <inheritdoc />
	public string Name => "OpenAPI Documentation";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		Settings settings = Settings.Read(context.Options);
		context.LogInfo(
			$"OpenAPI plugin initialized (spec={settings.Spec}, label={settings.Label}, routePrefix={settings.RoutePrefix})");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	/// <remarks>
	///     The options are read here, not in <see cref="InitializeAsync" />: every <c>openapi</c> entry
	///     in <c>plugins:</c> shares this instance, so options kept in fields made all entries render
	///     with the last entry's options. The ASP.NET Core host also sets <c>document</c> after the
	///     plugin is initialized, before each build.
	/// </remarks>
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		Settings settings = Settings.Read(context.Options);
		string content;
		string source;

		if (settings.Document is { } document)
		{
			// The ASP.NET Core host leaves it empty after warning that the app's document could not be read.
			if (string.IsNullOrWhiteSpace(document))
			{
				context.LogInfo("OpenAPI plugin: The 'document' option is empty. Skipping.");
				return;
			}

			content = document;
			source = "the 'document' option";
		}
		else
		{
			string? specFullPath = ResolveSpecPath(buildContext, settings.Spec);
			if (specFullPath is null)
			{
				context.LogWarning(settings.Spec is { } configured
					? $"OpenAPI plugin: The spec file '{FullSpecPath(buildContext, configured)}' does not exist. Skipping."
					: $"OpenAPI plugin: No OpenAPI spec file found. Looked for {string.Join(", ", _discoveredNames)} " +
					  $"in '{buildContext.RootDirectory}' and its docs folder. Skipping.");
				return;
			}

			context.LogInfo($"OpenAPI plugin: Processing spec at {specFullPath}");
			source = $"spec file '{specFullPath}'";

			try
			{
				content = await File.ReadAllTextAsync(specFullPath, ct);
			}
			catch (Exception ex)
			{
				context.LogError($"OpenAPI plugin: Failed to read spec file '{specFullPath}': {ex.Message}");
				return;
			}
		}

		OpenApiSpec spec;
		bool isJson;
		try
		{
			spec = OpenApiParser.Parse(content, out isJson);
		}
		catch (Exception ex)
		{
			context.LogError($"OpenAPI plugin: Failed to parse {source}: {ex.Message}");
			return;
		}

		foreach (string warning in spec.Warnings)
		{
			context.LogWarning($"OpenAPI plugin: {source}: {warning}");
		}

		string documentFileName = isJson ? "openapi.json" : "openapi.yaml";
		OpenApiLayout layout = OpenApiLayout.Create(spec, settings.RoutePrefix, documentFileName);

		// OutputPhase writes staged files after it cleans the output, and not at all in a dry run.
		buildContext.DeferredOutputFiles[layout.DocumentOutputPath!] = Encoding.UTF8.GetBytes(content);

		IReadOnlyDictionary<string, string> typeLinks = OpenApiTypeLinks.Build(buildContext);
		List<DocPage> pages = GeneratePages(spec, settings, layout, typeLinks, context);
		buildContext.Pages.AddRange(pages);

		context.LogInfo(
			$"OpenAPI plugin: Generated {pages.Count} pages ({spec.Endpoints.Count} endpoints) from {source}");
	}

	/// <summary>
	///     Resolves the OpenAPI spec file path. If the user specified a path in the config,
	///     resolves it relative to the project root. Otherwise, tries common file names.
	/// </summary>
	private static string? ResolveSpecPath(BuildContext buildContext, string? specPath)
	{
		string root = buildContext.RootDirectory;

		// User-specified path
		if (!string.IsNullOrWhiteSpace(specPath))
		{
			string full = FullSpecPath(buildContext, specPath);
			return File.Exists(full) ? full : null;
		}

		// Auto-discovery (JSON and YAML)
		string[] directories = [root, Path.Combine(root, buildContext.Config.Content.Docs)];

		foreach (string dir in directories)
		{
			if (!Directory.Exists(dir))
			{
				continue;
			}

			foreach (string candidate in _discoveredNames)
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

	private static string FullSpecPath(BuildContext buildContext, string specPath) =>
		Path.IsPathRooted(specPath) ? specPath : Path.GetFullPath(Path.Combine(buildContext.RootDirectory, specPath));

	/// <summary>
	///     Generates all documentation pages from the parsed spec: one index page, one detail page per
	///     tag group and, when the document has schemas, a models page.
	/// </summary>
	private static List<DocPage> GeneratePages(OpenApiSpec spec, Settings settings, OpenApiLayout layout,
		IReadOnlyDictionary<string, string> typeLinks, IPluginContext context)
	{
		var pages = new List<DocPage>();
		string inlineCss = OpenApiPageRenderer.GetInlineCss();

		// A schema links to its C# type's page when there is one, and to its entry on the models page
		// otherwise.
		Func<string, string?> schemaLink = name =>
			typeLinks.GetValueOrDefault(name)
			?? (layout.ModelsRoute is { } modelsRoute && layout.ModelAnchors.TryGetValue(name, out string? anchor)
				? $"{modelsRoute}#{anchor}"
				: null);

		string description = OpenApiMarkdown.PlainText(spec.Description);
		pages.Add(new DocPage
		{
			FrontMatter = new FrontMatter
			{
				Title = settings.Label,
				Description = description,
				Icon = "code"
			},
			Content = new PageContent
			{
				Html = inlineCss + OpenApiPageRenderer.RenderIndexPage(spec, layout, schemaLink),
				PlainText = string.Join(" ", new[] { $"{settings.Label} - {spec.Title} - {description}" }
					.Concat(spec.Webhooks.Select(w => $"{w.Method} {w.Path} {w.Summary}".TrimEnd())))
			},
			TableOfContents = BuildIndexToc(spec, layout),
			Route = settings.RoutePrefix,
			Origin = PageOrigin.ApiGenerated
		});

		int order = 1;
		foreach (OpenApiTagPageLayout page in layout.TagPages)
		{
			var plainTextParts = new List<string> { page.Tag, OpenApiMarkdown.PlainText(spec.TagDescriptions.GetValueOrDefault(page.Tag)) };
			plainTextParts.AddRange(page.Endpoints.Select(e => $"{e.Endpoint.Method} {e.Endpoint.Path} {e.Endpoint.Summary}"));

			pages.Add(new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = page.Tag,
					Description = $"{settings.Label} - {page.Tag}",
					Order = order++
				},
				Content = new PageContent
				{
					Html = inlineCss + OpenApiPageRenderer.RenderTagPage(spec, layout, page, schemaLink),
					PlainText = string.Join(" ", plainTextParts.Where(p => p.Length > 0))
				},
				// One entry per endpoint, which also gives each its own search result.
				TableOfContents = new TableOfContents
				{
					Entries = page.Endpoints.Select(e => new TocEntry { Level = 2, Text = e.Title, Id = e.Anchor }).ToList()
				},
				Route = page.Route,
				Origin = PageOrigin.ApiGenerated
			});

			context.LogInfo($"OpenAPI plugin: Generated page for tag '{page.Tag}' with {page.Endpoints.Count} endpoints");
		}

		if (layout.ModelsRoute is { } route)
		{
			var plainTextParts = new List<string> { "Models" };
			plainTextParts.AddRange(layout.ModelNames.Select(name =>
				$"{name} {OpenApiMarkdown.PlainText(spec.Schemas[name].Description)}".TrimEnd()));

			pages.Add(new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = "Models",
					Description = $"{settings.Label} - Models",
					// After the tag pages, which the sidebar sorts by this.
					Order = order
				},
				Content = new PageContent
				{
					Html = inlineCss + OpenApiPageRenderer.RenderModelsPage(spec, layout, schemaLink, typeLinks.GetValueOrDefault),
					PlainText = string.Join(" ", plainTextParts)
				},
				TableOfContents = new TableOfContents
				{
					Entries = layout.ModelNames.Select(name => new TocEntry { Level = 2, Text = name, Id = layout.ModelAnchors[name] }).ToList()
				},
				Route = route,
				Origin = PageOrigin.ApiGenerated
			});
		}

		return pages;
	}

	private static TableOfContents BuildIndexToc(OpenApiSpec spec, OpenApiLayout layout)
	{
		var entries = new List<TocEntry>();
		if (spec.Servers.Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = "Servers", Id = OpenApiLayout.ServersAnchor });
		}

		if (layout.HasAuthentication)
		{
			entries.Add(new TocEntry
			{
				Level = 2,
				Text = "Authentication",
				Id = OpenApiLayout.AuthenticationAnchor,
				Children = layout.SchemeAnchors.Select(s => new TocEntry { Level = 3, Text = s.Key, Id = s.Value }).ToList()
			});
		}

		entries.AddRange(layout.TagPages.Select(page =>
			new TocEntry { Level = 2, Text = page.Tag, Id = layout.TagSectionAnchors[page.Tag] }));
		if (spec.Webhooks.Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = "Webhooks", Id = OpenApiLayout.WebhooksAnchor });
		}

		return new TableOfContents { Entries = entries };
	}

	/// <summary>
	///     One <c>plugins:</c> entry's options.
	/// </summary>
	private sealed record Settings(string? Spec, string? Document, string Label, string RoutePrefix)
	{
		public static Settings Read(IReadOnlyDictionary<string, object> options)
		{
			string? spec = options.GetValueOrDefault("spec") as string;
			string? label = options.GetValueOrDefault(LabelOption) as string;
			string? prefix = options.GetValueOrDefault(RoutePrefixOption) as string;

			return new Settings(
				string.IsNullOrWhiteSpace(spec) ? null : spec,
				options.GetValueOrDefault(DocumentOption) as string,
				string.IsNullOrWhiteSpace(label) ? DefaultLabel : label,
				prefix is null ? DefaultRoutePrefix : NormalizeRoutePrefix(prefix));
		}
	}

	/// <summary>
	///     The route prefix with one leading slash and no trailing one, such as <c>/rest-api</c>, or an
	///     empty string for the site root.
	/// </summary>
	/// <param name="routePrefix">The <c>routePrefix</c> option as written.</param>
	/// <returns>The prefix the pages' routes start with.</returns>
	public static string NormalizeRoutePrefix(string routePrefix)
	{
		// Without the leading slash, the routes and the index page's links to the tag pages were relative.
		string trimmed = routePrefix.Trim().Trim('/');
		return trimmed.Length == 0 ? "" : "/" + trimmed;
	}
}
