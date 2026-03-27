namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Top-level representation of a parsed OpenAPI 3.0 specification.
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

	/// <summary>Enum values, if this is an enum schema.</summary>
	public List<string> EnumValues { get; } = [];

	/// <summary>
	///     Returns a concise display string for this schema (e.g. <c>string</c>,
	///     <c>User</c>, <c>Pet[]</c>, <c>integer (int64)</c>).
	/// </summary>
	public string ToDisplayString()
	{
		if (RefName is not null)
		{
			return RefName;
		}

		if (Type == "array" && Items is not null)
		{
			return $"{Items.ToDisplayString()}[]";
		}

		if (!string.IsNullOrEmpty(Format))
		{
			return $"{Type} ({Format})";
		}

		return string.IsNullOrEmpty(Type) ? "object" : Type;
	}
}
