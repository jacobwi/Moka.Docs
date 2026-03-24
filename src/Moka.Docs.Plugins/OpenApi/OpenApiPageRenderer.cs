using System.Text;
using System.Web;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Generates HTML content for OpenAPI endpoint documentation pages.
///     Produces an index page listing all endpoints and per-tag detail pages
///     with parameter tables, request/response schemas, and example JSON.
/// </summary>
public static class OpenApiPageRenderer
{
    /// <summary>
    ///     Renders an index page listing all endpoints grouped by tag.
    /// </summary>
    /// <param name="spec">The parsed OpenAPI specification.</param>
    /// <param name="routePrefix">The route prefix for linking to tag pages.</param>
    /// <returns>HTML string for the index page.</returns>
    public static string RenderIndexPage(OpenApiSpec spec, string routePrefix)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"openapi-index\">");
        sb.AppendLine($"<h1>{Esc(spec.Title)}</h1>");

        if (!string.IsNullOrEmpty(spec.Description))
            sb.AppendLine($"<p class=\"api-summary\">{Esc(spec.Description)}</p>");

        if (!string.IsNullOrEmpty(spec.Version))
            sb.AppendLine($"<p class=\"api-namespace\">Version: <code>{Esc(spec.Version)}</code></p>");

        // Group endpoints by tag
        var grouped = GroupByTag(spec.Endpoints);

        foreach (var (tag, endpoints) in grouped.OrderBy(kv => kv.Key))
        {
            var slug = Slugify(tag);
            sb.AppendLine($"<h2>{Esc(tag)}</h2>");
            sb.AppendLine(
                "<div class=\"table-responsive\"><table><thead><tr><th>Method</th><th>Path</th><th>Description</th></tr></thead><tbody>");

            foreach (var ep in endpoints)
            {
                var methodCss = GetMethodCssClass(ep.Method);
                var deprecatedClass = ep.Deprecated ? " class=\"openapi-deprecated\"" : "";
                sb.AppendLine($"<tr{deprecatedClass}>");
                sb.AppendLine($"<td><span class=\"openapi-method {methodCss}\">{ep.Method}</span></td>");
                sb.AppendLine($"<td><a href=\"{routePrefix}/{slug}\"><code>{Esc(ep.Path)}</code></a></td>");
                sb.AppendLine($"<td>{Esc(ep.Summary)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    /// <summary>
    ///     Renders a detail page for all endpoints under a specific tag.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <param name="endpoints">Endpoints belonging to this tag.</param>
    /// <returns>HTML string for the tag detail page.</returns>
    public static string RenderTagPage(string tag, IReadOnlyList<OpenApiEndpoint> endpoints)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"openapi-tag-page\">");
        sb.AppendLine($"<h1>{Esc(tag)}</h1>");

        foreach (var ep in endpoints) RenderEndpointCard(sb, ep);

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    /// <summary>
    ///     Renders a single endpoint as a card with all details.
    /// </summary>
    private static void RenderEndpointCard(StringBuilder sb, OpenApiEndpoint ep)
    {
        var methodCss = GetMethodCssClass(ep.Method);

        sb.AppendLine("<div class=\"openapi-endpoint-card\">");

        // Header: method badge + path
        sb.AppendLine("<div class=\"openapi-endpoint-header\">");
        sb.AppendLine($"<span class=\"openapi-method {methodCss}\">{ep.Method}</span>");
        sb.AppendLine($"<code class=\"openapi-path\">{Esc(ep.Path)}</code>");
        if (ep.Deprecated)
            sb.AppendLine("<span class=\"api-badge api-badge-obsolete\">Deprecated</span>");
        sb.AppendLine("</div>");

        // Summary and description
        if (!string.IsNullOrEmpty(ep.Summary))
            sb.AppendLine($"<p class=\"openapi-summary\">{Esc(ep.Summary)}</p>");
        if (!string.IsNullOrEmpty(ep.Description) && ep.Description != ep.Summary)
            sb.AppendLine($"<p class=\"openapi-description\">{Esc(ep.Description)}</p>");

        // Parameters table
        if (ep.Parameters.Count > 0)
        {
            sb.AppendLine("<h3>Parameters</h3>");
            sb.AppendLine(
                "<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Location</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");

            foreach (var p in ep.Parameters)
            {
                var reqBadge = p.Required
                    ? "<span class=\"openapi-required\">required</span>"
                    : "<span class=\"openapi-optional\">optional</span>";
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><code>{Esc(p.Name)}</code></td>");
                sb.AppendLine($"<td>{Esc(p.In)}</td>");
                sb.AppendLine($"<td><code>{Esc(p.SchemaType)}</code></td>");
                sb.AppendLine($"<td>{reqBadge}</td>");
                sb.AppendLine($"<td>{Esc(p.Description)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        // Request body
        if (ep.RequestBody is { } body)
        {
            sb.AppendLine("<h3>Request Body</h3>");

            if (!string.IsNullOrEmpty(body.ContentType))
                sb.AppendLine($"<p>Content-Type: <code>{Esc(body.ContentType)}</code></p>");
            if (!string.IsNullOrEmpty(body.Description))
                sb.AppendLine($"<p>{Esc(body.Description)}</p>");

            if (body.Schema is not null) RenderSchemaTable(sb, body.Schema);
        }

        // Example request JSON
        if (!string.IsNullOrEmpty(ep.ExampleRequestJson))
        {
            sb.AppendLine("<h4>Example Request</h4>");
            sb.AppendLine($"<pre><code class=\"language-json\">{Esc(ep.ExampleRequestJson)}</code></pre>");
        }

        // Responses table
        if (ep.Responses.Count > 0)
        {
            sb.AppendLine("<h3>Responses</h3>");
            sb.AppendLine(
                "<div class=\"table-responsive\"><table><thead><tr><th>Status</th><th>Description</th><th>Schema</th></tr></thead><tbody>");

            foreach (var r in ep.Responses)
            {
                var statusCss = GetStatusCssClass(r.StatusCode);
                var schemaDisplay = r.Schema is not null ? $"<code>{Esc(r.Schema.ToDisplayString())}</code>" : "\u2014";
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><span class=\"openapi-status {statusCss}\">{Esc(r.StatusCode)}</span></td>");
                sb.AppendLine($"<td>{Esc(r.Description)}</td>");
                sb.AppendLine($"<td>{schemaDisplay}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        // Example response JSON
        if (!string.IsNullOrEmpty(ep.ExampleResponseJson))
        {
            sb.AppendLine("<h4>Example Response</h4>");
            sb.AppendLine($"<pre><code class=\"language-json\">{Esc(ep.ExampleResponseJson)}</code></pre>");
        }

        sb.AppendLine("</div>"); // end endpoint-card
    }

    /// <summary>
    ///     Renders a schema as a properties table (for request/response body schemas).
    /// </summary>
    private static void RenderSchemaTable(StringBuilder sb, OpenApiSchema schema)
    {
        // If it's a reference or has properties, show them
        if (schema.Properties.Count > 0)
        {
            sb.AppendLine($"<p>Schema: <code>{Esc(schema.ToDisplayString())}</code></p>");
            sb.AppendLine(
                "<div class=\"table-responsive\"><table><thead><tr><th>Property</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");

            foreach (var (name, propSchema) in schema.Properties)
            {
                var isReq = schema.RequiredProperties.Contains(name);
                var reqBadge = isReq
                    ? "<span class=\"openapi-required\">required</span>"
                    : "<span class=\"openapi-optional\">optional</span>";

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><code>{Esc(name)}</code></td>");
                sb.AppendLine($"<td><code>{Esc(propSchema.ToDisplayString())}</code></td>");
                sb.AppendLine($"<td>{reqBadge}</td>");
                sb.AppendLine($"<td>{Esc(propSchema.Description)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }
        else
        {
            sb.AppendLine($"<p>Schema: <code>{Esc(schema.ToDisplayString())}</code></p>");
        }
    }

    /// <summary>
    ///     Returns inline CSS for the OpenAPI-specific styles.
    ///     These supplement the existing theme styles.
    /// </summary>
    public static string GetInlineCss()
    {
        return """
               <style>
               .openapi-method {
                   display: inline-block; padding: 0.2rem 0.5rem; border-radius: 4px;
                   font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.03em;
                   color: #fff; font-family: var(--font-mono, monospace);
                   min-width: 4rem; text-align: center;
               }
               .openapi-method-get { background: #22c55e; }
               .openapi-method-post { background: #3b82f6; }
               .openapi-method-put { background: #f59e0b; }
               .openapi-method-delete { background: #ef4444; }
               .openapi-method-patch { background: #8b5cf6; }
               .openapi-method-head { background: #64748b; }
               .openapi-method-options { background: #64748b; }

               .openapi-endpoint-card {
                   border: 1px solid var(--color-border, #e5e7eb);
                   border-radius: var(--radius-md, 8px);
                   padding: 1.5rem;
                   margin: 1.5rem 0;
                   background: var(--color-bg, #fff);
               }
               .openapi-endpoint-header {
                   display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;
                   margin-bottom: 0.75rem;
               }
               .openapi-path {
                   font-size: 1.05rem; font-weight: 600;
                   color: var(--color-text, #1a1a1a);
               }
               .openapi-summary { color: var(--color-text-secondary, #6b7280); margin-bottom: 0.5rem; }
               .openapi-description { color: var(--color-text-secondary, #6b7280); font-size: 0.9rem; }
               .openapi-deprecated { opacity: 0.6; text-decoration: line-through; }

               .openapi-required {
                   display: inline-block; padding: 0.125rem 0.375rem; border-radius: 3px;
                   font-size: 0.6875rem; font-weight: 600; min-width: 3.75rem; text-align: center;
                   background: color-mix(in srgb, #ef4444 12%, var(--color-bg, #fff)); color: #ef4444;
               }
               .openapi-optional {
                   display: inline-block; padding: 0.125rem 0.375rem; border-radius: 3px;
                   font-size: 0.6875rem; font-weight: 600; min-width: 3.75rem; text-align: center;
                   background: color-mix(in srgb, #64748b 12%, var(--color-bg, #fff)); color: #64748b;
               }
               .openapi-status {
                   display: inline-block; padding: 0.2rem 0.5rem; border-radius: 4px;
                   font-size: 0.8125rem; font-weight: 600; font-family: var(--font-mono, monospace);
               }
               .openapi-status-success { background: color-mix(in srgb, #22c55e 15%, var(--color-bg, #fff)); color: #22c55e; }
               .openapi-status-redirect { background: color-mix(in srgb, #f59e0b 15%, var(--color-bg, #fff)); color: #f59e0b; }
               .openapi-status-client-error { background: color-mix(in srgb, #ef4444 15%, var(--color-bg, #fff)); color: #ef4444; }
               .openapi-status-server-error { background: color-mix(in srgb, #dc2626 15%, var(--color-bg, #fff)); color: #dc2626; }
               .openapi-status-info { background: color-mix(in srgb, #3b82f6 15%, var(--color-bg, #fff)); color: #3b82f6; }

               .openapi-endpoint-card h3 {
                   font-size: 1rem; margin-top: 1.25rem; margin-bottom: 0.5rem;
                   padding-bottom: 0.25rem; border-bottom: 1px solid var(--color-border, #e5e7eb);
               }
               .openapi-endpoint-card h4 {
                   font-size: 0.875rem; margin-top: 1rem; margin-bottom: 0.375rem;
                   color: var(--color-text-secondary, #6b7280);
               }
               .openapi-index table { margin-top: 0.5rem; }
               </style>
               """;
    }

    #region Helpers

    private static Dictionary<string, List<OpenApiEndpoint>> GroupByTag(IEnumerable<OpenApiEndpoint> endpoints)
    {
        var grouped = new Dictionary<string, List<OpenApiEndpoint>>(StringComparer.OrdinalIgnoreCase);

        foreach (var ep in endpoints)
            if (ep.Tags.Count > 0)
            {
                foreach (var tag in ep.Tags)
                {
                    if (!grouped.TryGetValue(tag, out var list))
                    {
                        list = [];
                        grouped[tag] = list;
                    }

                    list.Add(ep);
                }
            }
            else
            {
                if (!grouped.TryGetValue("Other", out var list))
                {
                    list = [];
                    grouped["Other"] = list;
                }

                list.Add(ep);
            }

        return grouped;
    }

    private static string GetMethodCssClass(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => "openapi-method-get",
            "POST" => "openapi-method-post",
            "PUT" => "openapi-method-put",
            "DELETE" => "openapi-method-delete",
            "PATCH" => "openapi-method-patch",
            "HEAD" => "openapi-method-head",
            "OPTIONS" => "openapi-method-options",
            _ => "openapi-method-get"
        };
    }

    private static string GetStatusCssClass(string statusCode)
    {
        if (statusCode.StartsWith('2')) return "openapi-status-success";
        if (statusCode.StartsWith('3')) return "openapi-status-redirect";
        if (statusCode.StartsWith('4')) return "openapi-status-client-error";
        if (statusCode.StartsWith('5')) return "openapi-status-server-error";
        if (statusCode.StartsWith('1')) return "openapi-status-info";
        return "openapi-status-info"; // "default" and other
    }

    private static string Slugify(string value)
    {
        return value.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('/', '-')
            .Replace('.', '-');
    }

    private static string Esc(string value)
    {
        return HttpUtility.HtmlEncode(value);
    }

    #endregion
}