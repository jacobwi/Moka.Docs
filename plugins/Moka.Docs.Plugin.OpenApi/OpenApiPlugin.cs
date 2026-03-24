// MokaDocs — OpenAPI/Swagger documentation plugin
// Generates API endpoint pages from OpenAPI v2 (Swagger), v3.0, and v3.1 specifications.

using System.Text;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;

namespace Moka.Docs.Plugin.OpenApi;

/// <summary>
/// MokaDocs plugin that reads OpenAPI/Swagger specification files and generates
/// beautiful, navigable API endpoint documentation pages.
/// </summary>
/// <remarks>
/// Configuration in mokadocs.yaml:
/// <code>
/// plugins:
///   - name: Moka.Docs.Plugin.OpenApi
///     options:
///       spec: ./openapi.yaml          # Path to spec file (required)
///       label: "REST API"             # Section label (default: "REST API")
///       basePath: /rest-api           # Route prefix (default: /rest-api)
///       groupBy: tag                  # tag | path (default: tag)
/// </code>
/// Supports OpenAPI v2 (Swagger), v3.0, and v3.1 specs in YAML or JSON format.
/// </remarks>
public sealed class OpenApiPlugin : IMokaPlugin
{
    /// <inheritdoc />
    public string Id => "mokadocs-openapi";

    /// <inheritdoc />
    public string Name => "OpenAPI Documentation";

    /// <inheritdoc />
    public string Version => "1.0.0";

    private string _specPath = "";
    private string _label = "REST API";
    private string _basePath = "/rest-api";
    private string _groupBy = "tag";

    /// <inheritdoc />
    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context.Options.TryGetValue("spec", out var spec))
            _specPath = spec?.ToString() ?? "";
        if (context.Options.TryGetValue("label", out var label))
            _label = label?.ToString() ?? "REST API";
        if (context.Options.TryGetValue("basePath", out var basePath))
            _basePath = basePath?.ToString() ?? "/rest-api";
        if (context.Options.TryGetValue("groupBy", out var groupBy))
            _groupBy = groupBy?.ToString() ?? "tag";

        if (string.IsNullOrEmpty(_specPath))
        {
            context.LogWarning("OpenAPI plugin: 'spec' option is required. Skipping.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_specPath)) return Task.CompletedTask;

        var fullPath = Path.GetFullPath(Path.Combine(buildContext.RootDirectory, _specPath));
        if (!File.Exists(fullPath))
        {
            context.LogWarning($"OpenAPI spec not found: {_specPath}");
            return Task.CompletedTask;
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out var diagnostic);

            if (diagnostic.Errors.Count > 0)
            {
                foreach (var error in diagnostic.Errors)
                    context.LogWarning($"OpenAPI parse warning: {error.Message}");
            }

            context.LogInfo($"Parsed OpenAPI spec: {document.Info?.Title} v{document.Info?.Version}");

            // Generate index page
            var indexPage = GenerateIndexPage(document);
            buildContext.Pages.Add(indexPage);

            // Generate endpoint pages
            var pages = _groupBy == "path"
                ? GeneratePagesByPath(document)
                : GeneratePagesByTag(document);

            buildContext.Pages.AddRange(pages);

            context.LogInfo($"Generated {pages.Count + 1} REST API pages from OpenAPI spec");
        }
        catch (Exception ex)
        {
            context.LogError($"Failed to process OpenAPI spec: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    #region Index Page

    private DocPage GenerateIndexPage(OpenApiDocument doc)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(doc.Info?.Description))
            sb.AppendLine($"<p>{Esc(doc.Info.Description)}</p>");

        // Server URLs
        if (doc.Servers?.Count > 0)
        {
            sb.AppendLine("<h2>Base URLs</h2><ul>");
            foreach (var server in doc.Servers)
            {
                sb.AppendLine($"<li><code>{Esc(server.Url)}</code>");
                if (!string.IsNullOrEmpty(server.Description))
                    sb.Append($" — {Esc(server.Description)}");
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");
        }

        // Endpoint summary table
        sb.AppendLine("<h2>Endpoints</h2>");
        sb.AppendLine("<table><thead><tr><th>Method</th><th>Path</th><th>Description</th></tr></thead><tbody>");

        foreach (var (path, pathItem) in doc.Paths)
        {
            foreach (var (method, operation) in pathItem.Operations)
            {
                var httpMethod = method.ToString().ToUpperInvariant();
                var summary = operation.Summary ?? operation.Description ?? "";
                var opRoute = GetOperationRoute(path, httpMethod);

                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td><span class=\"api-badge api-badge-{httpMethod.ToLowerInvariant()}\">{httpMethod}</span></td>");
                sb.AppendLine($"<td><a href=\"{_basePath}/{opRoute}\"><code>{Esc(path)}</code></a></td>");
                sb.AppendLine($"<td>{Esc(Truncate(summary, 100))}</td>");
                sb.AppendLine("</tr>");
            }
        }
        sb.AppendLine("</tbody></table>");

        // Auth schemes
        if (doc.Components?.SecuritySchemes?.Count > 0)
        {
            sb.AppendLine("<h2>Authentication</h2><table><thead><tr><th>Scheme</th><th>Type</th><th>Description</th></tr></thead><tbody>");
            foreach (var (name, scheme) in doc.Components.SecuritySchemes)
            {
                sb.AppendLine($"<tr><td><code>{Esc(name)}</code></td><td>{Esc(scheme.Type.ToString())}</td><td>{Esc(scheme.Description ?? "")}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        return new DocPage
        {
            FrontMatter = new FrontMatter
            {
                Title = _label,
                Description = doc.Info?.Description ?? $"REST API documentation",
                Layout = "default"
            },
            Content = new PageContent { Html = sb.ToString(), PlainText = doc.Info?.Description ?? "" },
            Route = _basePath,
            Origin = PageOrigin.ApiGenerated
        };
    }

    #endregion

    #region Endpoint Pages

    private List<DocPage> GeneratePagesByTag(OpenApiDocument doc)
    {
        var pages = new List<DocPage>();

        foreach (var (path, pathItem) in doc.Paths)
        {
            foreach (var (method, operation) in pathItem.Operations)
            {
                pages.Add(GenerateEndpointPage(path, method, operation));
            }
        }

        return pages;
    }

    private List<DocPage> GeneratePagesByPath(OpenApiDocument doc)
    {
        // Same generation, different ordering could be applied
        return GeneratePagesByTag(doc);
    }

    private DocPage GenerateEndpointPage(string path, OperationType method, OpenApiOperation operation)
    {
        var httpMethod = method.ToString().ToUpperInvariant();
        var sb = new StringBuilder();

        // Method + path header
        sb.AppendLine($"<div class=\"api-type-header\">");
        sb.AppendLine($"<span class=\"api-badge api-badge-{httpMethod.ToLowerInvariant()}\">{httpMethod}</span>");
        if (operation.Deprecated)
            sb.AppendLine($"<span class=\"api-badge api-badge-obsolete\">Deprecated</span>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<pre class=\"api-signature\"><code>{httpMethod} {Esc(path)}</code></pre>");

        // Summary & description
        if (!string.IsNullOrEmpty(operation.Summary))
            sb.AppendLine($"<p class=\"api-summary\">{Esc(operation.Summary)}</p>");
        if (!string.IsNullOrEmpty(operation.Description))
            sb.AppendLine($"<div class=\"api-remarks\">{Esc(operation.Description)}</div>");

        // Tags
        if (operation.Tags?.Count > 0)
        {
            var tags = string.Join(", ", operation.Tags.Select(t => $"<code>{Esc(t.Name)}</code>"));
            sb.AppendLine($"<p>Tags: {tags}</p>");
        }

        // Parameters
        if (operation.Parameters?.Count > 0)
        {
            sb.AppendLine("<h2>Parameters</h2>");
            sb.AppendLine("<table><thead><tr><th>Name</th><th>In</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");

            foreach (var param in operation.Parameters)
            {
                var type = param.Schema?.Type ?? "string";
                var required = param.Required ? "✓" : "";
                sb.AppendLine($"<tr><td><code>{Esc(param.Name)}</code></td><td>{Esc(param.In?.ToString() ?? "")}</td><td><code>{Esc(type)}</code></td><td>{required}</td><td>{Esc(param.Description ?? "")}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        // Request body
        if (operation.RequestBody is { } body)
        {
            sb.AppendLine("<h2>Request Body</h2>");
            if (!string.IsNullOrEmpty(body.Description))
                sb.AppendLine($"<p>{Esc(body.Description)}</p>");

            foreach (var (mediaType, content) in body.Content)
            {
                sb.AppendLine($"<p>Content-Type: <code>{Esc(mediaType)}</code></p>");
                if (content.Schema is { } schema)
                    RenderSchema(sb, schema, "Request");
            }
        }

        // Responses
        if (operation.Responses?.Count > 0)
        {
            sb.AppendLine("<h2>Responses</h2>");
            sb.AppendLine("<table><thead><tr><th>Status</th><th>Description</th></tr></thead><tbody>");

            foreach (var (statusCode, response) in operation.Responses)
            {
                sb.AppendLine($"<tr><td><code>{Esc(statusCode)}</code></td><td>{Esc(response.Description ?? "")}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            // Render response schemas
            foreach (var (statusCode, response) in operation.Responses)
            {
                if (response.Content is null) continue;
                foreach (var (mediaType, content) in response.Content)
                {
                    if (content.Schema is { } schema)
                    {
                        sb.AppendLine($"<h3>Response {Esc(statusCode)} ({Esc(mediaType)})</h3>");
                        RenderSchema(sb, schema, $"Response {statusCode}");
                    }
                }
            }
        }

        var opRoute = GetOperationRoute(path, httpMethod);
        var title = operation.Summary ?? $"{httpMethod} {path}";

        return new DocPage
        {
            FrontMatter = new FrontMatter
            {
                Title = Truncate(title, 60),
                Description = operation.Description ?? operation.Summary ?? "",
                Layout = "default"
            },
            Content = new PageContent { Html = sb.ToString(), PlainText = operation.Summary ?? "" },
            Route = $"{_basePath}/{opRoute}",
            Origin = PageOrigin.ApiGenerated
        };
    }

    #endregion

    #region Schema Rendering

    private static void RenderSchema(StringBuilder sb, OpenApiSchema schema, string label)
    {
        if (schema.Properties?.Count > 0)
        {
            sb.AppendLine("<table><thead><tr><th>Property</th><th>Type</th><th>Description</th></tr></thead><tbody>");
            foreach (var (name, prop) in schema.Properties)
            {
                var type = FormatSchemaType(prop);
                var required = schema.Required?.Contains(name) == true ? " <strong>required</strong>" : "";
                sb.AppendLine($"<tr><td><code>{Esc(name)}</code>{required}</td><td><code>{Esc(type)}</code></td><td>{Esc(prop.Description ?? "")}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }
        else if (!string.IsNullOrEmpty(schema.Type))
        {
            sb.AppendLine($"<p>Type: <code>{Esc(FormatSchemaType(schema))}</code></p>");
        }
    }

    private static string FormatSchemaType(OpenApiSchema schema)
    {
        if (schema.Reference is { } r) return r.Id;
        if (schema.Type == "array" && schema.Items is { } items)
            return $"{FormatSchemaType(items)}[]";
        var type = schema.Type ?? "object";
        if (!string.IsNullOrEmpty(schema.Format))
            type += $" ({schema.Format})";
        if (schema.Nullable) type += "?";
        return type;
    }

    #endregion

    #region Helpers

    private static string GetOperationRoute(string path, string method)
    {
        var route = path.Trim('/').Replace("/", "-").Replace("{", "").Replace("}", "");
        return $"{method.ToLowerInvariant()}-{route}".ToLowerInvariant();
    }

    private static string Esc(string text) => HttpUtility.HtmlEncode(text);

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    #endregion
}
