using System.Text;
using System.Web;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Generates HTML content for OpenAPI endpoint documentation pages.
///     Produces an index page listing all endpoints, per-tag detail pages
///     with parameter tables, request/response schemas, and example JSON,
///     and a page listing the document's schemas.
/// </summary>
/// <remarks>
///     Descriptions are CommonMark, as the OpenAPI specification defines them, rendered without raw
///     HTML. Summaries are plain text.
/// </remarks>
public static class OpenApiPageRenderer
{
	private const string _requiredBadge = "<span class=\"openapi-required\">required</span>";
	private const string _optionalBadge = "<span class=\"openapi-optional\">optional</span>";

	/// <summary>
	///     Renders an index page listing all endpoints grouped by tag.
	/// </summary>
	/// <param name="spec">The parsed OpenAPI specification.</param>
	/// <param name="routePrefix">The route prefix for linking to tag pages.</param>
	/// <returns>HTML string for the index page.</returns>
	public static string RenderIndexPage(OpenApiSpec spec, string routePrefix) =>
		RenderIndexPage(spec, OpenApiLayout.Create(spec, routePrefix, null));

	/// <summary>
	///     Renders a detail page for all endpoints under a specific tag.
	/// </summary>
	/// <param name="tag">The tag name.</param>
	/// <param name="endpoints">Endpoints belonging to this tag.</param>
	/// <returns>HTML string for the tag detail page.</returns>
	public static string RenderTagPage(string tag, IReadOnlyList<OpenApiEndpoint> endpoints) =>
		RenderTagPage(tag, endpoints, null);

	/// <summary>
	///     Renders a detail page for all endpoints under a specific tag, linking schema names to pages.
	/// </summary>
	/// <param name="tag">The tag name.</param>
	/// <param name="endpoints">Endpoints belonging to this tag.</param>
	/// <param name="schemaLink">
	///     Returns the URL to link a schema name to, such as the API reference page of the C# type with
	///     that name, or <c>null</c> to show the name as text. A root-relative URL gets the site's base
	///     path when the page is rendered.
	/// </param>
	/// <returns>HTML string for the tag detail page.</returns>
	public static string RenderTagPage(string tag, IReadOnlyList<OpenApiEndpoint> endpoints,
		Func<string, string?>? schemaLink)
	{
		var spec = new OpenApiSpec();
		OpenApiTagPageLayout page = OpenApiLayout.CreateTagPage(tag, endpoints, "");
		return RenderTagPage(spec, OpenApiLayout.Create(spec, "", null), page, schemaLink);
	}

	#region Index Page

	internal static string RenderIndexPage(OpenApiSpec spec, OpenApiLayout layout, Func<string, string?>? schemaLink = null)
	{
		var sb = new StringBuilder();

		sb.AppendLine("<div class=\"openapi-index\">");
		sb.AppendLine($"<h1>{Esc(spec.Title)}</h1>");
		AppendMarkdownBlock(sb, spec.Description, "api-summary");

		if (!string.IsNullOrEmpty(spec.Version))
		{
			sb.AppendLine($"<p class=\"api-namespace\">Version: <code>{Esc(spec.Version)}</code></p>");
		}

		AppendExternalDocs(sb, spec.ExternalDocs);

		if (layout.DocumentUrl is { } documentUrl)
		{
			sb.AppendLine($"<p class=\"openapi-download\"><a href=\"{Attr(documentUrl)}\" download>Download the OpenAPI document</a> " +
			              $"(<code>{Esc(layout.DocumentFileName!)}</code>)</p>");
		}

		if (layout.ModelsRoute is { } modelsRoute)
		{
			string count = layout.ModelNames.Count == 1 ? "1 schema" : $"{layout.ModelNames.Count} schemas";
			sb.AppendLine($"<p class=\"openapi-models-link\"><a href=\"{Attr(modelsRoute)}\">Models</a>: the document's {count}.</p>");
		}

		RenderServers(sb, spec);
		RenderAuthentication(sb, spec, layout);

		foreach (OpenApiTagPageLayout page in layout.TagPages)
		{
			sb.AppendLine($"<h2 id=\"{Attr(layout.TagSectionAnchors[page.Tag])}\">{Esc(page.Tag)}</h2>");
			sb.AppendLine(
				"<div class=\"table-responsive\"><table><thead><tr><th>Method</th><th>Path</th><th>Description</th></tr></thead><tbody>");

			foreach (OpenApiEndpointLayout entry in page.Endpoints)
			{
				OpenApiEndpoint ep = entry.Endpoint;
				string deprecatedClass = ep.Deprecated ? " class=\"openapi-deprecated\"" : "";
				sb.AppendLine($"<tr{deprecatedClass}>");
				sb.AppendLine($"<td><span class=\"openapi-method {GetMethodCssClass(ep.Method)}\">{Esc(ep.Method)}</span></td>");
				sb.AppendLine($"<td><a href=\"{Attr($"{page.Route}#{entry.Anchor}")}\"><code>{Esc(ep.Path)}</code></a></td>");
				sb.AppendLine($"<td>{Esc(ep.Summary)}</td>");
				sb.AppendLine("</tr>");
			}

			sb.AppendLine("</tbody></table></div>");
		}

		RenderWebhooks(sb, spec, layout, schemaLink);
		sb.AppendLine("</div>");
		return sb.ToString();
	}

	/// <summary>
	///     The webhooks of an OpenAPI 3.1 document: one row each with the method, the name, the summary and
	///     the request body the API sends. Their parameters and responses are left to the document itself.
	/// </summary>
	private static void RenderWebhooks(StringBuilder sb, OpenApiSpec spec, OpenApiLayout layout, Func<string, string?>? schemaLink)
	{
		if (spec.Webhooks.Count == 0)
		{
			return;
		}

		sb.AppendLine($"<h2 id=\"{OpenApiLayout.WebhooksAnchor}\">Webhooks</h2>");
		string document = layout.DocumentUrl is { } documentUrl ? $"<a href=\"{Attr(documentUrl)}\">the OpenAPI document</a>" : "the OpenAPI document";
		sb.AppendLine($"<p>Requests the API sends to a URL you give it. Their parameters and responses are in {document}.</p>");
		sb.AppendLine(
			"<div class=\"table-responsive\"><table><thead><tr><th>Method</th><th>Name</th><th>Description</th><th>Request body</th></tr></thead><tbody>");
		foreach (OpenApiEndpoint webhook in spec.Webhooks)
		{
			string body = webhook.RequestBody is { } requestBody
				? (requestBody.Schema is { } schema ? $"<code>{RenderType(schema, schemaLink)}</code>" : "")
				  + (requestBody.ContentType.Length > 0 ? $" <span class=\"openapi-values\">{Esc(requestBody.ContentType)}</span>" : "")
				: "-";
			string deprecatedClass = webhook.Deprecated ? " class=\"openapi-deprecated\"" : "";
			sb.AppendLine($"<tr{deprecatedClass}>");
			sb.AppendLine($"<td><span class=\"openapi-method {GetMethodCssClass(webhook.Method)}\">{Esc(webhook.Method)}</span></td>");
			sb.AppendLine($"<td><code>{Esc(webhook.Path)}</code></td>");
			sb.AppendLine($"<td>{Esc(webhook.Summary)}</td>");
			sb.AppendLine($"<td>{body}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	private static void RenderServers(StringBuilder sb, OpenApiSpec spec)
	{
		if (spec.Servers.Count == 0)
		{
			return;
		}

		sb.AppendLine($"<h2 id=\"{OpenApiLayout.ServersAnchor}\">Servers</h2>");
		sb.AppendLine("<div class=\"table-responsive\"><table><thead><tr><th>URL</th><th>Description</th></tr></thead><tbody>");
		foreach (OpenApiServer server in spec.Servers)
		{
			string variables = string.Concat(server.Variables
				.Where(v => v.EnumValues.Count > 1)
				.Select(v => $"<span class=\"openapi-values\"><code>{{{Esc(v.Name)}}}</code>: " +
				             $"{string.Join(", ", v.EnumValues.Select(value => $"<code>{Esc(value)}</code>"))}</span>"));
			sb.AppendLine("<tr>");
			sb.AppendLine($"<td><code>{Esc(server.Url)}</code></td>");
			sb.AppendLine($"<td>{OpenApiMarkdown.Inline(server.Description)}{variables}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	private static void RenderAuthentication(StringBuilder sb, OpenApiSpec spec, OpenApiLayout layout)
	{
		if (!layout.HasAuthentication)
		{
			return;
		}

		sb.AppendLine($"<h2 id=\"{OpenApiLayout.AuthenticationAnchor}\">Authentication</h2>");

		List<OpenApiSecurityRequirement> defaults = spec.Security.Where(r => r.Schemes.Count > 0).ToList();
		if (defaults.Count > 0)
		{
			string requirements = RenderRequirements(defaults, layout);
			sb.AppendLine(defaults.Count < spec.Security.Count
				? $"<p>Unless an endpoint says otherwise, it accepts {requirements}, or no credentials.</p>"
				: $"<p>Unless an endpoint says otherwise, it needs {requirements}.</p>");
		}

		foreach ((string name, OpenApiSecurityScheme scheme) in spec.SecuritySchemes)
		{
			sb.AppendLine($"<h3 id=\"{Attr(layout.SchemeAnchors[name])}\">{Esc(name)}</h3>");
			AppendMarkdownBlock(sb, scheme.Description, null);

			var rows = new List<(string Label, string Html)> { ("Type", Esc(SchemeTypeName(scheme.Type))) };
			switch (scheme.Type)
			{
				case "apiKey":
					AddRow(rows, "Name", scheme.ParameterName, Code);
					AddRow(rows, "In", scheme.In, Esc);
					break;
				case "http":
					AddRow(rows, "Scheme", scheme.Scheme, Code);
					AddRow(rows, "Bearer format", scheme.BearerFormat, Esc);
					break;
				case "openIdConnect":
					AddRow(rows, "Discovery URL", scheme.OpenIdConnectUrl, Url);
					break;
			}

			foreach (OpenApiOAuthFlow flow in scheme.Flows)
			{
				rows.Add(("Flow", Esc(FlowName(flow.Kind))));
				AddRow(rows, "Authorization URL", flow.AuthorizationUrl, Url);
				AddRow(rows, "Token URL", flow.TokenUrl, Url);
				AddRow(rows, "Refresh URL", flow.RefreshUrl, Url);
				if (flow.Scopes.Count > 0)
				{
					rows.Add(("Scopes", string.Join("<br>", flow.Scopes.Select(s =>
						s.Value.Length > 0 ? $"<code>{Esc(s.Key)}</code>: {Esc(s.Value)}" : $"<code>{Esc(s.Key)}</code>"))));
				}
			}

			sb.AppendLine("<div class=\"table-responsive\"><table class=\"openapi-scheme\"><tbody>");
			foreach ((string label, string html) in rows)
			{
				sb.AppendLine($"<tr><th>{label}</th><td>{html}</td></tr>");
			}

			sb.AppendLine("</tbody></table></div>");
		}
	}

	private static void AddRow(List<(string, string)> rows, string label, string value, Func<string, string> format)
	{
		if (value.Length > 0)
		{
			rows.Add((label, format(value)));
		}
	}

	private static string SchemeTypeName(string type) => type switch
	{
		"apiKey" => "API key",
		"http" => "HTTP",
		"oauth2" => "OAuth 2.0",
		"openIdConnect" => "OpenID Connect",
		"mutualTLS" => "Mutual TLS",
		_ => type
	};

	private static string FlowName(string kind) => kind switch
	{
		"implicit" => "Implicit",
		"password" => "Password",
		"clientCredentials" => "Client credentials",
		"authorizationCode" => "Authorization code",
		_ => kind
	};

	#endregion

	#region Tag Page

	internal static string RenderTagPage(OpenApiSpec spec, OpenApiLayout layout, OpenApiTagPageLayout page,
		Func<string, string?>? schemaLink)
	{
		var sb = new StringBuilder();

		sb.AppendLine("<div class=\"openapi-tag-page\">");
		sb.AppendLine($"<h1>{Esc(page.Tag)}</h1>");
		AppendMarkdownBlock(sb, spec.TagDescriptions.GetValueOrDefault(page.Tag), "openapi-tag-description");
		AppendExternalDocs(sb, spec.TagExternalDocs.GetValueOrDefault(page.Tag));

		foreach (OpenApiEndpointLayout entry in page.Endpoints)
		{
			RenderEndpointCard(sb, entry, spec, layout, schemaLink);
		}

		sb.AppendLine("</div>");
		return sb.ToString();
	}

	/// <summary>
	///     Renders a single endpoint as a card with all details.
	/// </summary>
	private static void RenderEndpointCard(StringBuilder sb, OpenApiEndpointLayout entry, OpenApiSpec spec,
		OpenApiLayout layout, Func<string, string?>? schemaLink)
	{
		OpenApiEndpoint ep = entry.Endpoint;

		sb.AppendLine("<div class=\"openapi-endpoint-card\">");

		sb.AppendLine($"<h2 class=\"openapi-endpoint-header\" id=\"{Attr(entry.Anchor)}\">");
		sb.AppendLine($"<span class=\"openapi-method {GetMethodCssClass(ep.Method)}\">{Esc(ep.Method)}</span>");
		sb.AppendLine($"<code class=\"openapi-path\">{Esc(ep.Path)}</code>");
		if (ep.Deprecated)
		{
			sb.AppendLine("<span class=\"api-badge api-badge-obsolete\">Deprecated</span>");
		}

		sb.AppendLine("</h2>");

		RenderEndpointAuth(sb, ep, spec, layout);
		RenderEndpointServers(sb, ep, spec);

		if (!string.IsNullOrEmpty(ep.Summary))
		{
			sb.AppendLine($"<p class=\"openapi-summary\">{Esc(ep.Summary)}</p>");
		}

		if (ep.Description != ep.Summary)
		{
			AppendMarkdownBlock(sb, ep.Description, "openapi-description");
		}

		AppendExternalDocs(sb, ep.ExternalDocs);

		if (ep.Parameters.Count > 0)
		{
			sb.AppendLine("<h3>Parameters</h3>");
			sb.AppendLine(
				"<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Location</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");

			foreach (OpenApiParameter p in ep.Parameters)
			{
				string type = p.Schema is not null ? RenderType(p.Schema, schemaLink) : Esc(p.SchemaType);
				sb.AppendLine("<tr>");
				sb.AppendLine($"<td><code>{Esc(p.Name)}</code></td>");
				sb.AppendLine($"<td>{Esc(p.In)}</td>");
				sb.AppendLine($"<td><code>{type}</code></td>");
				sb.AppendLine($"<td>{(p.Required ? _requiredBadge : _optionalBadge)}</td>");
				sb.AppendLine($"<td>{OpenApiMarkdown.Inline(p.Description)}{RenderValues(p.Schema)}</td>");
				sb.AppendLine("</tr>");
			}

			sb.AppendLine("</tbody></table></div>");
		}

		if (ep.RequestBody is { } body)
		{
			sb.AppendLine("<h3>Request Body</h3>");

			if (!string.IsNullOrEmpty(body.ContentType))
			{
				sb.AppendLine($"<p>Content-Type: <code>{Esc(body.ContentType)}</code></p>");
			}

			AppendMarkdownBlock(sb, body.Description, null);

			if (body.Schema is not null)
			{
				RenderSchemaTable(sb, body.Schema, schemaLink);
			}
		}

		if (!string.IsNullOrEmpty(ep.ExampleRequestJson))
		{
			sb.AppendLine("<h4>Example Request</h4>");
			sb.AppendLine($"<pre><code class=\"language-json\">{Esc(ep.ExampleRequestJson)}</code></pre>");
		}

		if (ep.Responses.Count > 0)
		{
			sb.AppendLine("<h3>Responses</h3>");
			sb.AppendLine(
				"<div class=\"table-responsive\"><table><thead><tr><th>Status</th><th>Description</th><th>Schema</th></tr></thead><tbody>");

			foreach (OpenApiResponse r in ep.Responses)
			{
				string schemaDisplay =
					r.Schema is not null ? $"<code>{RenderType(r.Schema, schemaLink)}</code>" : "-";
				sb.AppendLine("<tr>");
				sb.AppendLine($"<td><span class=\"openapi-status {GetStatusCssClass(r.StatusCode)}\">{Esc(r.StatusCode)}</span></td>");
				sb.AppendLine($"<td>{OpenApiMarkdown.Inline(r.Description)}</td>");
				sb.AppendLine($"<td>{schemaDisplay}</td>");
				sb.AppendLine("</tr>");
			}

			sb.AppendLine("</tbody></table></div>");
		}

		if (!string.IsNullOrEmpty(ep.ExampleResponseJson))
		{
			sb.AppendLine("<h4>Example Response</h4>");
			sb.AppendLine($"<pre><code class=\"language-json\">{Esc(ep.ExampleResponseJson)}</code></pre>");
		}

		sb.AppendLine("</div>");
	}

	/// <summary>
	///     A line under the endpoint's heading naming the schemes it needs, each linked to its entry in
	///     the index page's Authentication section.
	/// </summary>
	private static void RenderEndpointAuth(StringBuilder sb, OpenApiEndpoint ep, OpenApiSpec spec, OpenApiLayout layout)
	{
		List<OpenApiSecurityRequirement> effective = ep.Security ?? spec.Security;
		List<OpenApiSecurityRequirement> alternatives = effective.Where(r => r.Schemes.Count > 0).ToList();

		if (alternatives.Count == 0)
		{
			// Worth saying only when the operation opts out of the document's default.
			if (ep.Security is not null && spec.Security.Any(r => r.Schemes.Count > 0))
			{
				sb.AppendLine($"<p class=\"openapi-auth\">{AuthLabel("No authentication", layout)}</p>");
			}

			return;
		}

		string label = alternatives.Count < effective.Count ? "Auth (optional)" : "Auth";
		sb.AppendLine($"<p class=\"openapi-auth\">{AuthLabel(label, layout)} {RenderRequirements(alternatives, layout)}</p>");
	}

	private static string AuthLabel(string text, OpenApiLayout layout) =>
		layout.HasAuthentication
			? $"<a class=\"openapi-auth-label\" href=\"{Attr($"{layout.IndexUrl}#{OpenApiLayout.AuthenticationAnchor}")}\">{text}</a>"
			: $"<span class=\"openapi-auth-label\">{text}</span>";

	/// <summary>
	///     Alternatives joined with "or", the schemes of one alternative with "+", each with its scopes.
	/// </summary>
	private static string RenderRequirements(IEnumerable<OpenApiSecurityRequirement> alternatives, OpenApiLayout layout) =>
		string.Join(" <span class=\"openapi-auth-or\">or</span> ", alternatives.Select(requirement =>
			string.Join(" + ", requirement.Schemes.Select(s => RenderSchemeUse(s.Key, s.Value, layout)))));

	private static string RenderSchemeUse(string name, List<string> scopes, OpenApiLayout layout)
	{
		string html = $"<code>{Esc(name)}</code>";
		if (layout.SchemeAnchors.TryGetValue(name, out string? anchor))
		{
			html = $"<a href=\"{Attr($"{layout.IndexUrl}#{anchor}")}\">{html}</a>";
		}

		return scopes.Count > 0
			? $"{html} <span class=\"openapi-scopes\">({string.Join(", ", scopes.Select(Esc))})</span>"
			: html;
	}

	/// <summary>
	///     A line under the endpoint's heading naming the servers its path or operation lists, when they
	///     aren't the document's. The index page lists the document's own.
	/// </summary>
	private static void RenderEndpointServers(StringBuilder sb, OpenApiEndpoint ep, OpenApiSpec spec)
	{
		if (ep.Servers is not { Count: > 0 } servers || SameServers(servers, spec.Servers))
		{
			return;
		}

		string label = servers.Count == 1 ? "Server" : "Servers";
		string list = string.Join(" or ", servers.Select(RenderServerUse));
		sb.AppendLine($"<p class=\"openapi-endpoint-servers\"><span class=\"openapi-servers-label\">{label}</span> {list}</p>");
	}

	private static string RenderServerUse(OpenApiServer server)
	{
		var html = new StringBuilder($"<code>{Esc(server.Url)}</code>");
		if (OpenApiMarkdown.PlainText(server.Description) is { Length: > 0 } description)
		{
			html.Append($" ({Esc(description)})");
		}

		foreach (OpenApiServerVariable variable in server.Variables.Where(v => v.EnumValues.Count > 1))
		{
			html.Append($" <span class=\"openapi-server-values\"><code>{{{Esc(variable.Name)}}}</code>: ")
				.Append(string.Join(", ", variable.EnumValues.Select(value => $"<code>{Esc(value)}</code>")))
				.Append("</span>");
		}

		return html.ToString();
	}

	/// <summary>The same server URLs in any order, ignoring a trailing slash.</summary>
	private static bool SameServers(List<OpenApiServer> servers, List<OpenApiServer> documentServers)
	{
		static HashSet<string> Urls(IEnumerable<OpenApiServer> list) =>
			new(list.Select(s => s.Url.TrimEnd('/')), StringComparer.Ordinal);

		return Urls(servers).SetEquals(Urls(documentServers));
	}

	#endregion

	#region Models Page

	/// <summary>
	///     Renders the page that lists the document's schemas.
	/// </summary>
	/// <param name="spec">The parsed document.</param>
	/// <param name="layout">The layout, which names the schemas and their anchors.</param>
	/// <param name="schemaLink">Returns the URL to link a schema name to, as for the tag pages.</param>
	/// <param name="typePage">Returns the route of the C# type page for a schema name, or <c>null</c>.</param>
	internal static string RenderModelsPage(OpenApiSpec spec, OpenApiLayout layout, Func<string, string?>? schemaLink,
		Func<string, string?> typePage)
	{
		var sb = new StringBuilder();
		sb.AppendLine("<div class=\"openapi-models-page\">");
		sb.AppendLine("<h1>Models</h1>");

		foreach (string name in layout.ModelNames)
		{
			OpenApiSchema schema = spec.Schemas[name];
			sb.AppendLine("<section class=\"openapi-model\">");
			sb.AppendLine($"<h2 id=\"{Attr(layout.ModelAnchors[name])}\">{Esc(name)}</h2>");

			if (typePage(name) is { Length: > 0 } route)
			{
				sb.AppendLine($"<p class=\"openapi-model-type\">C# type: <a class=\"openapi-type-link\" href=\"{Attr(route)}\">{Esc(name)}</a></p>");
			}

			AppendMarkdownBlock(sb, schema.Description, null);
			AppendExternalDocs(sb, schema.ExternalDocs);

			bool plainObject = schema.Properties.Count > 0 && schema.Variants.Count == 0 && !schema.Nullable
			                   && schema.RefName is null && schema.Type is "object" or "";
			if (!plainObject)
			{
				sb.AppendLine($"<p>Type: <code>{RenderType(schema, schemaLink)}</code></p>");
			}

			if (schema.EnumValues.Count > 0)
			{
				sb.AppendLine($"<p>Values: {string.Join(", ", schema.EnumValues.Select(v => $"<code>{Esc(v)}</code>"))}</p>");
			}

			RenderPropertyTable(sb, schema, schemaLink);
			sb.AppendLine("</section>");
		}

		sb.AppendLine("</div>");
		return sb.ToString();
	}

	#endregion

	#region Schemas

	/// <summary>
	///     Renders a schema as a properties table (for request/response body schemas).
	/// </summary>
	private static void RenderSchemaTable(StringBuilder sb, OpenApiSchema schema, Func<string, string?>? schemaLink)
	{
		sb.AppendLine($"<p>Schema: <code>{RenderType(schema, schemaLink)}</code></p>");

		// A polymorphic body (oneOf/anyOf behind a $ref) shows its alternatives, which the name hides.
		if (schema.RefName is not null && schema.Variants.Count > 1)
		{
			string variants = string.Join(", ", schema.Variants.Select(v => $"<code>{RenderType(v, schemaLink)}</code>"));
			sb.AppendLine($"<p>One of: {variants}</p>");
		}

		RenderPropertyTable(sb, schema, schemaLink);
	}

	private static void RenderPropertyTable(StringBuilder sb, OpenApiSchema schema, Func<string, string?>? schemaLink)
	{
		if (schema.Properties.Count == 0)
		{
			return;
		}

		sb.AppendLine(
			"<div class=\"table-responsive\"><table><thead><tr><th>Property</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");

		foreach ((string name, OpenApiSchema propSchema) in schema.Properties)
		{
			sb.AppendLine("<tr>");
			sb.AppendLine($"<td><code>{Esc(name)}</code></td>");
			sb.AppendLine($"<td><code>{RenderType(propSchema, schemaLink)}</code></td>");
			sb.AppendLine($"<td>{(schema.RequiredProperties.Contains(name) ? _requiredBadge : _optionalBadge)}</td>");
			sb.AppendLine($"<td>{OpenApiMarkdown.Inline(propSchema.Description)}{RenderValues(propSchema)}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	/// <summary>
	///     A schema's display type as HTML, with each schema name linked where
	///     <paramref name="schemaLink" /> has a URL for it.
	/// </summary>
	private static string RenderType(OpenApiSchema schema, Func<string, string?>? schemaLink) =>
		schema.Render(
			name => schemaLink?.Invoke(name) is { Length: > 0 } href
				? $"<a class=\"openapi-type-link\" href=\"{Attr(href)}\">{Esc(name)}</a>"
				: Esc(name),
			Esc);

	/// <summary>
	///     The allowed values of an enum or <c>const</c> schema, for the description column.
	/// </summary>
	private static string RenderValues(OpenApiSchema? schema)
	{
		if (schema is null || schema.EnumValues.Count == 0)
		{
			return "";
		}

		string values = string.Join(", ", schema.EnumValues.Select(v => $"<code>{Esc(v)}</code>"));
		return $"<span class=\"openapi-values\">Values: {values}</span>";
	}

	#endregion

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
		       .openapi-endpoint-card h2.openapi-endpoint-header {
		           display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;
		           margin: 0 0 0.75rem; padding: 0; border: none;
		           font-size: 1rem !important; letter-spacing: normal; line-height: 1.4;
		       }
		       .openapi-path {
		           font-size: 1.05rem; font-weight: 600;
		           color: var(--color-text, #1a1a1a);
		       }
		       .openapi-summary { color: var(--color-text-secondary, #6b7280); margin-bottom: 0.5rem; }
		       .openapi-description { color: var(--color-text-secondary, #6b7280); font-size: 0.9rem; }
		       .openapi-deprecated { opacity: 0.6; text-decoration: line-through; }
		       .openapi-markdown > :first-child { margin-top: 0; }
		       .openapi-markdown > :last-child { margin-bottom: 0; }
		       td > p:first-child { margin-top: 0; }
		       td > p:last-child { margin-bottom: 0; }

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

		       .openapi-auth { font-size: 0.8125rem; margin: 0 0 0.5rem; color: var(--color-text-secondary, #6b7280); }
		       .openapi-auth-label {
		           display: inline-block; padding: 0.1rem 0.45rem; border-radius: 3px; margin-right: 0.25rem;
		           font-size: 0.6875rem; font-weight: 600; text-decoration: none;
		           background: color-mix(in srgb, #f59e0b 14%, var(--color-bg, #fff)); color: #b45309;
		       }
		       .openapi-auth a { text-decoration: none; }
		       .openapi-auth-or { margin: 0 0.25rem; }
		       .openapi-scheme th { text-align: left; white-space: nowrap; width: 1%; }
		       .openapi-endpoint-servers { font-size: 0.8125rem; margin: 0 0 0.5rem; color: var(--color-text-secondary, #6b7280); }
		       .openapi-servers-label {
		           display: inline-block; padding: 0.05rem 0.4rem; border-radius: var(--radius-sm, 3px); margin-right: 0.25rem;
		           font-size: 0.6875rem; font-weight: 600; border: 1px solid var(--color-border, #e5e7eb);
		       }
		       .openapi-server-values { margin-left: 0.25rem; }
		       .openapi-external-docs { font-size: 0.875rem; color: var(--color-text-secondary, #6b7280); }

		       .openapi-endpoint-card h3 {
		           font-size: 1rem; margin-top: 1.25rem; margin-bottom: 0.5rem;
		           padding-bottom: 0.25rem; border-bottom: 1px solid var(--color-border, #e5e7eb);
		       }
		       .openapi-endpoint-card h4 {
		           font-size: 0.875rem; margin-top: 1rem; margin-bottom: 0.375rem;
		           color: var(--color-text-secondary, #6b7280);
		       }
		       .openapi-index table { margin-top: 0.5rem; }
		       .openapi-type-link {
		           color: inherit; text-decoration: underline;
		           text-decoration-thickness: 1px; text-underline-offset: 0.15em;
		       }
		       .openapi-type-link:hover { text-decoration-thickness: 2px; }
		       .openapi-values {
		           display: block; margin-top: 0.25rem;
		           font-size: 0.8125rem; color: var(--color-text-secondary, #6b7280);
		       }
		       .openapi-model-type { font-size: 0.875rem; color: var(--color-text-secondary, #6b7280); }
		       </style>
		       """;
	}

	#region Helpers

	/// <summary>
	///     Appends a description rendered as Markdown, in a <c>div</c> with the given class, or nothing
	///     when it is blank.
	/// </summary>
	private static void AppendMarkdownBlock(StringBuilder sb, string? markdown, string? cssClass)
	{
		string html = OpenApiMarkdown.Block(markdown);
		if (html.Length > 0)
		{
			string classes = cssClass is null ? "openapi-markdown" : $"openapi-markdown {cssClass}";
			sb.AppendLine($"<div class=\"{classes}\">{html}</div>");
		}
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
		if (statusCode.StartsWith('2'))
		{
			return "openapi-status-success";
		}

		if (statusCode.StartsWith('3'))
		{
			return "openapi-status-redirect";
		}

		if (statusCode.StartsWith('4'))
		{
			return "openapi-status-client-error";
		}

		if (statusCode.StartsWith('5'))
		{
			return "openapi-status-server-error";
		}

		if (statusCode.StartsWith('1'))
		{
			return "openapi-status-info";
		}

		return "openapi-status-info"; // "default" and other
	}

	/// <summary>An http or https URL as a link, anything else as text.</summary>
	private static string Url(string url) =>
		IsHttpUrl(url) ? $"<a href=\"{Attr(url)}\"><code>{Esc(url)}</code></a>" : Code(url);

	/// <summary>
	///     Whether a URL from the document can be a link: an absolute http or https URL. Relative and
	///     protocol-relative ones would resolve against the docs site, not the API.
	/// </summary>
	private static bool IsHttpUrl(string url) =>
		OpenApiMarkdown.IsSafeUrl(url) && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
		                                   || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	///     An <c>externalDocs</c> link, its text the description without markup or else the URL. A URL that
	///     isn't http or https is shown as text.
	/// </summary>
	private static void AppendExternalDocs(StringBuilder sb, OpenApiExternalDocs? docs)
	{
		if (docs is null)
		{
			return;
		}

		string description = OpenApiMarkdown.PlainText(docs.Description);
		string html = IsHttpUrl(docs.Url)
			? $"<a href=\"{Attr(docs.Url)}\">{Esc(description.Length > 0 ? description : docs.Url)}</a>"
			: description.Length > 0
				? $"{Esc(description)} {Code(docs.Url)}"
				: Code(docs.Url);
		sb.AppendLine($"<p class=\"openapi-external-docs\">External documentation: {html}</p>");
	}

	private static string Code(string value) => $"<code>{Esc(value)}</code>";

	private static string Esc(string value) => HttpUtility.HtmlEncode(value);

	private static string Attr(string value) => HttpUtility.HtmlAttributeEncode(value);

	#endregion
}
