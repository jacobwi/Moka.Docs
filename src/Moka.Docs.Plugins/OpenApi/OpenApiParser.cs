using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi.Reader;
using OApi = Microsoft.OpenApi;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Reads an OpenAPI specification (JSON or YAML, versions 2.0/3.0/3.1)
///     using the official <c>Microsoft.OpenApi</c> package and maps
///     the result to MokaDocs' own <see cref="OpenApiSpec" /> model.
/// </summary>
/// <remarks>
///     Every type from the SDK is referenced through the <c>OApi</c> alias because
///     this namespace declares its own <see cref="OpenApiSchema" />,
///     <see cref="OpenApiParameter" />, <see cref="OpenApiRequestBody" /> and
///     <see cref="OpenApiResponse" /> types that would otherwise collide.
/// </remarks>
public static class OpenApiParser
{
	private static readonly JsonSerializerOptions _indentedJson = new() { WriteIndented = true };

	/// <summary>
	///     Parses the given specification string (JSON or YAML).
	/// </summary>
	/// <param name="content">Raw content of the OpenAPI spec file.</param>
	/// <returns>A populated <see cref="OpenApiSpec" /> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the spec cannot be parsed.</exception>
	public static OpenApiSpec Parse(string content)
	{
		ReadResult result = OApi.OpenApiDocument.Parse(content, DetectFormat(content), CreateSettings());
		return MapDocument(RequireDocument(result));
	}

	/// <summary>
	///     Parses an OpenAPI spec from a stream (JSON or YAML).
	/// </summary>
	/// <param name="stream">Stream positioned at the start of the spec content.</param>
	/// <returns>A populated <see cref="OpenApiSpec" /> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the spec cannot be parsed.</exception>
	public static OpenApiSpec Parse(Stream stream)
	{
		using var reader = new StreamReader(stream);
		return Parse(reader.ReadToEnd());
	}

	#region Reader Plumbing

	/// <summary>
	///     Builds reader settings with the YAML reader registered. The JSON reader is
	///     built in; YAML support moved to a separate package in Microsoft.OpenApi 2.x.
	/// </summary>
	private static OpenApiReaderSettings CreateSettings()
	{
		var settings = new OpenApiReaderSettings();
		settings.AddYamlReader();
		return settings;
	}

	/// <summary>
	///     Picks the reader format from the first meaningful character. A spec starting
	///     with an opening brace is JSON; anything else is treated as YAML (which is a
	///     superset, so this is a safe fallback rather than a guess).
	/// </summary>
	private static string DetectFormat(string content)
	{
		foreach (char c in content)
		{
			if (char.IsWhiteSpace(c))
			{
				continue;
			}

			return c == '{' ? "json" : "yaml";
		}

		return "json";
	}

	private static OApi.OpenApiDocument RequireDocument(ReadResult result)
	{
		if (result.Document is not null)
		{
			return result.Document;
		}

		string errors = result.Diagnostic is not null
			? string.Join("; ", result.Diagnostic.Errors.Select(e => e.Message))
			: "unknown error";

		throw new InvalidOperationException($"Failed to parse OpenAPI spec: {errors}");
	}

	#endregion

	#region Document Mapping

	private static OpenApiSpec MapDocument(OApi.OpenApiDocument doc)
	{
		var spec = new OpenApiSpec
		{
			Title = doc.Info?.Title ?? "",
			Description = doc.Info?.Description ?? "",
			Version = doc.Info?.Version ?? ""
		};

		// Base path from first server
		if (doc.Servers is { Count: > 0 })
		{
			string? serverUrl = doc.Servers[0].Url;
			if (Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri))
			{
				spec.BasePath = uri.AbsolutePath.TrimEnd('/');
			}
		}

		// Schemas
		if (doc.Components?.Schemas is not null)
		{
			foreach ((string name, OApi.IOpenApiSchema schema) in doc.Components.Schemas)
			{
				spec.Schemas[name] = MapSchema(schema, []);
			}
		}

		// Endpoints from paths
		var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (doc.Paths is not null)
		{
			foreach ((string pathStr, OApi.IOpenApiPathItem pathItem) in doc.Paths)
			{
				if (pathItem.Operations is null)
				{
					continue;
				}

				foreach ((HttpMethod method, OApi.OpenApiOperation operation) in pathItem.Operations)
				{
					OpenApiEndpoint endpoint = MapEndpoint(method, pathStr, operation, pathItem);

					foreach (string tag in endpoint.Tags)
					{
						if (tagSet.Add(tag))
						{
							spec.Tags.Add(tag);
						}
					}

					spec.Endpoints.Add(endpoint);
				}
			}
		}

		return spec;
	}

	private static OpenApiEndpoint MapEndpoint(
		HttpMethod method,
		string path,
		OApi.OpenApiOperation operation,
		OApi.IOpenApiPathItem pathItem)
	{
		var endpoint = new OpenApiEndpoint
		{
			Method = method.Method.ToUpperInvariant(),
			Path = path,
			Summary = operation.Summary ?? "",
			Description = operation.Description ?? "",
			OperationId = operation.OperationId ?? "",
			Deprecated = operation.Deprecated
		};

		// Tags
		if (operation.Tags is not null)
		{
			foreach (OApi.OpenApiTagReference tag in operation.Tags)
			{
				if (!string.IsNullOrEmpty(tag.Name))
				{
					endpoint.Tags.Add(tag.Name);
				}
			}
		}

		// Merge path-level + operation-level parameters
		var allParams = new List<OApi.IOpenApiParameter>();

		// Path-level parameters first
		if (pathItem.Parameters is not null)
		{
			allParams.AddRange(pathItem.Parameters);
		}

		// Operation-level parameters override path-level ones
		if (operation.Parameters is not null)
		{
			var opParamKeys = new HashSet<string>(
				operation.Parameters.Select(p => $"{p.Name}:{p.In}"),
				StringComparer.OrdinalIgnoreCase);

			// Remove path-level params that are overridden
			allParams.RemoveAll(p => opParamKeys.Contains($"{p.Name}:{p.In}"));
			allParams.AddRange(operation.Parameters);
		}

		foreach (OApi.IOpenApiParameter p in allParams)
		{
			endpoint.Parameters.Add(MapParameter(p));
		}

		// Request body
		if (operation.RequestBody is not null)
		{
			endpoint.RequestBody = MapRequestBody(operation.RequestBody);
			endpoint.ExampleRequestJson = ExtractExample(operation.RequestBody.Content);
		}

		// Responses
		if (operation.Responses is not null)
		{
			foreach ((string statusCode, OApi.IOpenApiResponse resp) in operation.Responses)
			{
				OpenApiResponse response = MapResponse(statusCode, resp);
				endpoint.Responses.Add(response);

				// Capture first 2xx example
				if (endpoint.ExampleResponseJson is null && statusCode.StartsWith('2'))
				{
					string? example = ExtractExample(resp.Content);
					if (example is not null)
					{
						endpoint.ExampleResponseJson = example;
					}
				}
			}
		}

		return endpoint;
	}

	private static OpenApiParameter MapParameter(OApi.IOpenApiParameter param)
	{
		return new OpenApiParameter
		{
			Name = param.Name ?? "",
			In = param.In?.ToString().ToLowerInvariant() ?? "",
			Description = param.Description ?? "",
			Required = param.Required,
			SchemaType = param.Schema is not null ? SchemaToTypeString(param.Schema) : ""
		};
	}

	private static OpenApiRequestBody MapRequestBody(OApi.IOpenApiRequestBody body)
	{
		var rb = new OpenApiRequestBody
		{
			Description = body.Description ?? "",
			Required = body.Required
		};

		if (body.Content is { Count: > 0 })
		{
			KeyValuePair<string, OApi.OpenApiMediaType> first = body.Content.First();
			rb.ContentType = first.Key;
			if (first.Value.Schema is not null)
			{
				rb.Schema = MapSchema(first.Value.Schema, []);
			}
		}

		return rb;
	}

	private static OpenApiResponse MapResponse(string statusCode, OApi.IOpenApiResponse resp)
	{
		var response = new OpenApiResponse
		{
			StatusCode = statusCode,
			Description = resp.Description ?? ""
		};

		if (resp.Content is { Count: > 0 })
		{
			KeyValuePair<string, OApi.OpenApiMediaType> first = resp.Content.First();
			if (first.Value.Schema is not null)
			{
				response.Schema = MapSchema(first.Value.Schema, []);
			}
		}

		return response;
	}

	#endregion

	#region Schema Mapping

	/// <summary>
	///     Maps an SDK schema to the local model.
	/// </summary>
	/// <param name="schema">The schema, possibly an <c>OpenApiSchemaReference</c>.</param>
	/// <param name="expanding">
	///     Component names already being expanded further up the call stack. Microsoft.OpenApi 2.x
	///     resolves references transparently - reading <c>Properties</c> on a reference returns the
	///     target's properties - so a self-referencing schema (a tree node whose children are the
	///     same type, say) would recurse forever. Re-entering a name already in this set emits the
	///     reference name only and stops.
	/// </param>
	private static OpenApiSchema MapSchema(OApi.IOpenApiSchema schema, HashSet<string> expanding)
	{
		string? refName = (schema as OApi.OpenApiSchemaReference)?.Reference.Id;

		if (refName is not null && !expanding.Add(refName))
		{
			// Cycle: this component is already being expanded higher up the stack.
			return new OpenApiSchema { RefName = refName, Type = TypeToString(schema.Type) };
		}

		try
		{
			var result = new OpenApiSchema
			{
				// Must stay null (not "") for non-references: OpenApiSchema.ToDisplayString
				// returns RefName whenever it is non-null.
				RefName = refName,
				Type = TypeToString(schema.Type),
				Format = schema.Format ?? "",
				Description = schema.Description ?? ""
			};

			// Array items
			if (result.Type == "array" && schema.Items is not null)
			{
				result.Items = MapSchema(schema.Items, expanding);
			}

			// Object properties
			if (schema.Properties is not null)
			{
				foreach ((string name, OApi.IOpenApiSchema propSchema) in schema.Properties)
				{
					result.Properties[name] = MapSchema(propSchema, expanding);
				}
			}

			// Required properties
			if (schema.Required is not null)
			{
				foreach (string req in schema.Required)
				{
					result.RequiredProperties.Add(req);
				}
			}

			// Enum values
			if (schema.Enum is not null)
			{
				foreach (JsonNode? val in schema.Enum)
				{
					result.EnumValues.Add(JsonNodeToScalar(val));
				}
			}

			return result;
		}
		finally
		{
			if (refName is not null)
			{
				expanding.Remove(refName);
			}
		}
	}

	/// <summary>
	///     Produces a concise type string from a schema (for parameter display).
	/// </summary>
	private static string SchemaToTypeString(OApi.IOpenApiSchema schema)
	{
		if (schema is OApi.OpenApiSchemaReference schemaRef)
		{
			return schemaRef.Reference.Id ?? "object";
		}

		string type = TypeToString(schema.Type);

		if (type == "array" && schema.Items is not null)
		{
			return $"{SchemaToTypeString(schema.Items)}[]";
		}

		if (!string.IsNullOrEmpty(schema.Format))
		{
			return $"{type} ({schema.Format})";
		}

		return type;
	}

	/// <summary>
	///     Renders a JSON Schema type as its lowercase spec name. The SDK models this as a
	///     <c>[Flags]</c> enum so nullable types arrive as e.g. <c>String | Null</c>; the
	///     <c>Null</c> bit is dropped so the display name matches what the spec author wrote.
	/// </summary>
	private static string TypeToString(OApi.JsonSchemaType? type)
	{
		if (type is null)
		{
			return "";
		}

		OApi.JsonSchemaType flags = type.Value & ~OApi.JsonSchemaType.Null;

		return flags switch
		{
			0 => "null",
			OApi.JsonSchemaType.Array => "array",
			OApi.JsonSchemaType.Object => "object",
			OApi.JsonSchemaType.String => "string",
			OApi.JsonSchemaType.Integer => "integer",
			OApi.JsonSchemaType.Number => "number",
			OApi.JsonSchemaType.Boolean => "boolean",
			_ => flags.ToString().ToLowerInvariant()
		};
	}

	#endregion

	#region Examples

	/// <summary>
	///     Extracts an example JSON string from content media types.
	/// </summary>
	private static string? ExtractExample(IDictionary<string, OApi.OpenApiMediaType>? content)
	{
		if (content is null or { Count: 0 })
		{
			return null;
		}

		foreach ((string _, OApi.OpenApiMediaType mediaType) in content)
		{
			// Check media type level example
			if (mediaType.Example is not null)
			{
				return FormatJsonNode(mediaType.Example);
			}

			// Check examples map
			if (mediaType.Examples is { Count: > 0 })
			{
				OApi.IOpenApiExample first = mediaType.Examples.Values.First();
				if (first.Value is not null)
				{
					return FormatJsonNode(first.Value);
				}
			}

			// Check schema-level examples. The singular `Schema.Example` is obsolete in
			// Microsoft.OpenApi 2.x in favour of the JSON Schema `examples` array.
			if (mediaType.Schema?.Examples is { Count: > 0 } schemaExamples
			    && schemaExamples[0] is { } schemaExample)
			{
				return FormatJsonNode(schemaExample);
			}

			break;
		}

		return null;
	}

	/// <summary>
	///     Pretty-prints a <see cref="JsonNode" /> as indented JSON.
	/// </summary>
	private static string FormatJsonNode(JsonNode node) => node.ToJsonString(_indentedJson);

	/// <summary>
	///     Renders a <see cref="JsonNode" /> for a list of literal values: plain strings are
	///     unquoted, everything else falls back to its JSON form.
	/// </summary>
	private static string JsonNodeToScalar(JsonNode? node)
	{
		if (node is null)
		{
			return "null";
		}

		return node.GetValueKind() == JsonValueKind.String
			? node.GetValue<string>()
			: node.ToJsonString();
	}

	#endregion
}
