namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Top-level representation of a parsed OpenAPI document (2.0, 3.0 or 3.1).
/// </summary>
public sealed class OpenApiSpec
{
	/// <summary>API title from the info object.</summary>
	public string Title { get; set; } = "";

	/// <summary>API description from the info object.</summary>
	public string Description { get; set; } = "";

	/// <summary>API version string from the info object.</summary>
	public string Version { get; set; } = "";

	/// <summary>Base path extracted from the first server URL, if present.</summary>
	public string BasePath { get; set; } = "";

	/// <summary>All parsed endpoints grouped by their original path and method.</summary>
	public List<OpenApiEndpoint> Endpoints { get; } = [];

	/// <summary>Named schemas from the components/schemas section.</summary>
	public Dictionary<string, OpenApiSchema> Schemas { get; } = new(StringComparer.Ordinal);

	/// <summary>Distinct tag names found across all endpoints, in discovery order.</summary>
	public List<string> Tags { get; } = [];

	/// <summary>
	///     The descriptions (CommonMark) of the tags declared in the document's top-level <c>tags</c> list,
	///     keyed by tag name without regard to case.
	/// </summary>
	public Dictionary<string, string> TagDescriptions { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     The servers the API is available at: the <c>servers</c> list of OpenAPI 3.x, or the URLs built
	///     from <c>host</c>, <c>basePath</c> and <c>schemes</c> in 2.0. Empty when the document names none.
	/// </summary>
	public List<OpenApiServer> Servers { get; } = [];

	/// <summary>
	///     The security schemes from <c>components/securitySchemes</c> (<c>securityDefinitions</c> in 2.0),
	///     keyed by the name requirements refer to them by, in document order.
	/// </summary>
	public Dictionary<string, OpenApiSecurityScheme> SecuritySchemes { get; } = new(StringComparer.Ordinal);

	/// <summary>
	///     The document's default <c>security</c>: alternatives, any one of which grants access. Empty when
	///     the document sets none. An operation's own <see cref="OpenApiEndpoint.Security" /> replaces it.
	/// </summary>
	public List<OpenApiSecurityRequirement> Security { get; } = [];
}

/// <summary>
///     A server the API is available at.
/// </summary>
public sealed class OpenApiServer
{
	/// <summary>The server URL with each variable replaced by its default value.</summary>
	public string Url { get; set; } = "";

	/// <summary>Human-readable description (CommonMark).</summary>
	public string Description { get; set; } = "";

	/// <summary>The variables of the URL template, in document order.</summary>
	public List<OpenApiServerVariable> Variables { get; } = [];
}

/// <summary>
///     A variable in a server URL template, such as <c>{environment}</c>.
/// </summary>
public sealed class OpenApiServerVariable
{
	/// <summary>The variable name, without braces.</summary>
	public string Name { get; set; } = "";

	/// <summary>The value used in <see cref="OpenApiServer.Url" />.</summary>
	public string Default { get; set; } = "";

	/// <summary>Human-readable description (CommonMark).</summary>
	public string Description { get; set; } = "";

	/// <summary>The allowed values, when the document limits them.</summary>
	public List<string> EnumValues { get; } = [];
}

/// <summary>
///     A way of authenticating requests, from <c>components/securitySchemes</c> or 2.0's
///     <c>securityDefinitions</c>.
/// </summary>
public sealed class OpenApiSecurityScheme
{
	/// <summary>The name requirements refer to the scheme by.</summary>
	public string Name { get; set; } = "";

	/// <summary>
	///     The scheme type: <c>apiKey</c>, <c>http</c>, <c>oauth2</c>, <c>openIdConnect</c> or <c>mutualTLS</c>.
	///     A 2.0 <c>basic</c> scheme is read as <c>http</c> with <see cref="Scheme" /> <c>basic</c>.
	/// </summary>
	public string Type { get; set; } = "";

	/// <summary>Human-readable description (CommonMark).</summary>
	public string Description { get; set; } = "";

	/// <summary>For <c>apiKey</c>, the name of the header, query parameter or cookie.</summary>
	public string ParameterName { get; set; } = "";

	/// <summary>For <c>apiKey</c>, where the key goes: <c>header</c>, <c>query</c> or <c>cookie</c>.</summary>
	public string In { get; set; } = "";

	/// <summary>For <c>http</c>, the authorization scheme, such as <c>bearer</c> or <c>basic</c>.</summary>
	public string Scheme { get; set; } = "";

	/// <summary>For <c>http</c> with <c>bearer</c>, a hint at the token's format, such as <c>JWT</c>.</summary>
	public string BearerFormat { get; set; } = "";

	/// <summary>For <c>openIdConnect</c>, the URL of the OpenID Connect discovery document.</summary>
	public string OpenIdConnectUrl { get; set; } = "";

	/// <summary>For <c>oauth2</c>, the flows the scheme supports, in document order.</summary>
	public List<OpenApiOAuthFlow> Flows { get; } = [];
}

/// <summary>
///     One OAuth 2.0 flow of an <c>oauth2</c> security scheme.
/// </summary>
public sealed class OpenApiOAuthFlow
{
	/// <summary>
	///     The flow: <c>implicit</c>, <c>password</c>, <c>clientCredentials</c> or <c>authorizationCode</c>.
	///     2.0's <c>application</c> and <c>accessCode</c> are read as the last two.
	/// </summary>
	public string Kind { get; set; } = "";

	/// <summary>The authorization URL, for the implicit and authorization code flows.</summary>
	public string AuthorizationUrl { get; set; } = "";

	/// <summary>The token URL, for the password, client credentials and authorization code flows.</summary>
	public string TokenUrl { get; set; } = "";

	/// <summary>The URL for refreshing tokens, if the document gives one.</summary>
	public string RefreshUrl { get; set; } = "";

	/// <summary>The available scopes and their descriptions, in document order.</summary>
	public Dictionary<string, string> Scopes { get; } = new(StringComparer.Ordinal);
}

/// <summary>
///     One alternative of a <c>security</c> list: the schemes it needs, all of them together.
/// </summary>
/// <remarks>
///     An alternative with no schemes (<c>{}</c> in the document) means a request without credentials
///     is accepted too.
/// </remarks>
public sealed class OpenApiSecurityRequirement
{
	/// <summary>The scheme names, each with the scopes it needs (often none outside OAuth 2.0), in document order.</summary>
	public Dictionary<string, List<string>> Schemes { get; } = new(StringComparer.Ordinal);
}

/// <summary>
///     Represents a single HTTP endpoint (one path + one HTTP method).
/// </summary>
public sealed class OpenApiEndpoint
{
	/// <summary>HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</summary>
	public string Method { get; set; } = "";

	/// <summary>URL path template (e.g. <c>/users/{id}</c>).</summary>
	public string Path { get; set; } = "";

	/// <summary>Short summary of the endpoint.</summary>
	public string Summary { get; set; } = "";

	/// <summary>Longer description of the endpoint.</summary>
	public string Description { get; set; } = "";

	/// <summary>Unique operation identifier.</summary>
	public string OperationId { get; set; } = "";

	/// <summary>Whether this endpoint is marked as deprecated.</summary>
	public bool Deprecated { get; set; }

	/// <summary>Tags used to group this endpoint in the documentation.</summary>
	public List<string> Tags { get; } = [];

	/// <summary>Path, query, header, and cookie parameters.</summary>
	public List<OpenApiParameter> Parameters { get; } = [];

	/// <summary>Request body definition, if any.</summary>
	public OpenApiRequestBody? RequestBody { get; set; }

	/// <summary>Response definitions keyed by status code.</summary>
	public List<OpenApiResponse> Responses { get; } = [];

	/// <summary>Example JSON request body, if provided in the spec.</summary>
	public string? ExampleRequestJson { get; set; }

	/// <summary>Example JSON response body (typically from the first 2xx response), if provided.</summary>
	public string? ExampleResponseJson { get; set; }

	/// <summary>
	///     The operation's own <c>security</c>, which replaces the document's <see cref="OpenApiSpec.Security" />,
	///     or <c>null</c> when it has none. An empty list means the operation needs no authentication.
	/// </summary>
	public List<OpenApiSecurityRequirement>? Security { get; set; }
}

/// <summary>
///     Represents a parameter (path, query, header, or cookie) on an endpoint.
/// </summary>
public sealed class OpenApiParameter
{
	/// <summary>Parameter name.</summary>
	public string Name { get; set; } = "";

	/// <summary>Parameter location: <c>path</c>, <c>query</c>, <c>header</c>, or <c>cookie</c>.</summary>
	public string In { get; set; } = "";

	/// <summary>Human-readable description.</summary>
	public string Description { get; set; } = "";

	/// <summary>Whether this parameter is required.</summary>
	public bool Required { get; set; }

	/// <summary>Schema type description (e.g. <c>string</c>, <c>integer (int64)</c>).</summary>
	public string SchemaType { get; set; } = "";

	/// <summary>The parameter's schema, or <c>null</c> when it has none.</summary>
	public OpenApiSchema? Schema { get; set; }
}

/// <summary>
///     Represents the request body of an endpoint.
/// </summary>
public sealed class OpenApiRequestBody
{
	/// <summary>Human-readable description of the request body.</summary>
	public string Description { get; set; } = "";

	/// <summary>Whether the request body is required.</summary>
	public bool Required { get; set; }

	/// <summary>Content type (e.g. <c>application/json</c>).</summary>
	public string ContentType { get; set; } = "";

	/// <summary>Schema describing the request body structure.</summary>
	public OpenApiSchema? Schema { get; set; }
}

/// <summary>
///     Represents a single response definition on an endpoint.
/// </summary>
public sealed class OpenApiResponse
{
	/// <summary>HTTP status code as a string (e.g. <c>200</c>, <c>404</c>, <c>default</c>).</summary>
	public string StatusCode { get; set; } = "";

	/// <summary>Human-readable description of the response.</summary>
	public string Description { get; set; } = "";

	/// <summary>Schema describing the response body, if any.</summary>
	public OpenApiSchema? Schema { get; set; }
}

/// <summary>
///     Simplified representation of a JSON Schema used in OpenAPI specs.
/// </summary>
public sealed class OpenApiSchema
{
	/// <summary>Schema type (e.g. <c>object</c>, <c>array</c>, <c>string</c>, <c>integer</c>).</summary>
	public string Type { get; set; } = "";

	/// <summary>Format qualifier (e.g. <c>int64</c>, <c>date-time</c>, <c>uuid</c>).</summary>
	public string Format { get; set; } = "";

	/// <summary>Referenced type name (from <c>$ref</c>), without the path prefix.</summary>
	public string? RefName { get; set; }

	/// <summary>For <c>array</c> types, the schema of the array items.</summary>
	public OpenApiSchema? Items { get; set; }

	/// <summary>For <c>object</c> types, the named properties.</summary>
	public Dictionary<string, OpenApiSchema> Properties { get; } = new(StringComparer.Ordinal);

	/// <summary>List of required property names for object schemas.</summary>
	public List<string> RequiredProperties { get; } = [];

	/// <summary>Human-readable description of this schema.</summary>
	public string Description { get; set; } = "";

	/// <summary>Enum values, if this is an enum schema. A <c>const</c> is listed as its only value.</summary>
	public List<string> EnumValues { get; } = [];

	/// <summary>
	///     Whether the value can be <c>null</c>: a <c>null</c> type (<c>nullable: true</c> in OpenAPI 3.0),
	///     or a <c>{"type": "null"}</c> alternative in <c>oneOf</c> or <c>anyOf</c>.
	/// </summary>
	public bool Nullable { get; set; }

	/// <summary>
	///     The alternatives of a <c>oneOf</c> or <c>anyOf</c>, without the <c>null</c> alternative, which
	///     sets <see cref="Nullable" /> instead.
	/// </summary>
	public List<OpenApiSchema> Variants { get; } = [];

	/// <summary>
	///     The schema of the values of a map: an object with <c>additionalProperties</c>, such as a
	///     <c>Dictionary&lt;string, int&gt;</c>.
	/// </summary>
	public OpenApiSchema? AdditionalProperties { get; set; }

	/// <summary>
	///     Returns a concise display string for this schema (e.g. <c>string</c>,
	///     <c>User</c>, <c>Pet[]</c>, <c>integer (int64)</c>, <c>string | null</c>,
	///     <c>map&lt;string, integer (int32)&gt;</c>).
	/// </summary>
	public string ToDisplayString() => Render(name => name, text => text);

	/// <summary>
	///     Builds the display string, passing each schema name through <paramref name="name" /> and
	///     everything else through <paramref name="text" />, so the page renderer can escape both and
	///     link the names.
	/// </summary>
	internal string Render(Func<string, string> name, Func<string, string> text)
	{
		string core = RenderCore(name, text);
		return Nullable && Type != "null" ? core + text(" | null") : core;
	}

	private string RenderCore(Func<string, string> name, Func<string, string> text)
	{
		if (RefName is not null)
		{
			return name(RefName);
		}

		if (Variants.Count > 0)
		{
			return string.Join(text(" | "), Variants.Select(v => Group(v, name, text)));
		}

		if (Type == "array" && Items is not null)
		{
			return Group(Items, name, text) + text("[]");
		}

		if (AdditionalProperties is not null && Properties.Count == 0)
		{
			return text("map<string, ") + AdditionalProperties.Render(name, text) + text(">");
		}

		if (string.IsNullOrEmpty(Type))
		{
			// No type, no reference and no alternatives: an empty schema, which allows any value.
			// One with properties is an object whose author left out "type": "object".
			return text(Properties.Count > 0 ? "object" : "any");
		}

		return text(string.IsNullOrEmpty(Format) ? Type : $"{Type} ({Format})");
	}

	/// <summary>Wraps a union in parentheses where it is followed by <c>[]</c> or joined to another.</summary>
	private static string Group(OpenApiSchema schema, Func<string, string> name, Func<string, string> text)
	{
		string rendered = schema.Render(name, text);
		bool union = (schema.RefName is null && schema.Variants.Count > 1) || (schema.Nullable && schema.Type != "null");
		return union ? text("(") + rendered + text(")") : rendered;
	}
}
