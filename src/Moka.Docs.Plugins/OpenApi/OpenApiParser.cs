using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Reads an OpenAPI document (JSON or YAML, versions 2.0, 3.0 and 3.1) and maps it to MokaDocs' own
///     <see cref="OpenApiSpec" /> model.
/// </summary>
/// <remarks>
///     The document is read with System.Text.Json (YAML through YamlDotNet), not with an OpenAPI library.
///     Microsoft.OpenApi 1.x and 2.x share an assembly name, so a process loads one of them: referencing
///     2.x here moved every app that uses MokaDocs to 2.x, and Microsoft.AspNetCore.OpenApi 9 and
///     Swashbuckle 9, which are built for 1.x, then failed to generate their own documents.
/// </remarks>
public static class OpenApiParser
{
	private static readonly JsonSerializerOptions _indentedJson = new() { WriteIndented = true };

	private static readonly JsonDocumentOptions _lenientJson = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip,
		MaxDepth = OpenApiYaml.MaxDepth
	};

	private static readonly string[] _methods = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

	// The order types are listed in when a schema allows several.
	private static readonly string[] _typeOrder = ["string", "integer", "number", "boolean", "object", "array"];

	/// <summary>
	///     Parses the given specification string (JSON or YAML).
	/// </summary>
	/// <param name="content">Raw content of the OpenAPI spec file.</param>
	/// <returns>A populated <see cref="OpenApiSpec" /> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the spec cannot be parsed.</exception>
	public static OpenApiSpec Parse(string content) => Parse(content, out _);

	/// <summary>
	///     Parses the given specification string (JSON or YAML) and says which of the two it was read as.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the spec cannot be parsed.</exception>
	internal static OpenApiSpec Parse(string content, out bool isJson)
	{
		(JsonObject root, int nodeCount) = ReadDocument(content, out isJson);
		return new DocumentMapper(root, nodeCount).Map();
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
	///     Reads the document as JSON, or as YAML when it isn't JSON, and counts its nodes.
	/// </summary>
	/// <remarks>
	///     JSON is tried first, so a JSON document that starts with a comment is still JSON, and a YAML
	///     document written as one flow mapping (<c>{openapi: 3.0.0, ...}</c>) still reads as YAML.
	/// </remarks>
	private static (JsonObject Root, int NodeCount) ReadDocument(string content, out bool isJson)
	{
		string text = content.StartsWith('\uFEFF') ? content[1..] : content;
		JsonNode? root;
		try
		{
			root = JsonNode.Parse(text, documentOptions: _lenientJson);
			isJson = true;
		}
		catch (JsonException jsonError)
		{
			try
			{
				root = OpenApiYaml.Parse(text);
				isJson = false;
			}
			catch (Exception yamlError) when (yamlError is YamlException or InvalidOperationException)
			{
				Exception shown = LooksLikeJson(text) ? jsonError : yamlError;
				throw new InvalidOperationException($"Failed to parse OpenAPI spec: {shown.Message}", shown);
			}
		}

		if (root is not JsonObject document)
		{
			throw new InvalidOperationException("Failed to parse OpenAPI spec: the document is not an object.");
		}

		int nodeCount = CountNodes(document);
		if (Text(document["openapi"]).Length == 0 && Text(document["swagger"]).Length == 0)
		{
			throw new InvalidOperationException(
				"Failed to parse OpenAPI spec: the document has no 'openapi' or 'swagger' version field.");
		}

		return (document, nodeCount);
	}

	private static bool LooksLikeJson(string text)
	{
		string start = text.TrimStart();
		return start.StartsWith('{') || start.StartsWith('[') || start.StartsWith("//", StringComparison.Ordinal)
		       || start.StartsWith("/*", StringComparison.Ordinal);
	}

	/// <summary>
	///     Counts the document's nodes without recursing. Reading each object here also turns a key written
	///     twice into a parse error: <see cref="JsonObject" /> reads its properties only when first asked,
	///     and threw an <see cref="ArgumentException" /> from somewhere in the middle of mapping.
	/// </summary>
	private static int CountNodes(JsonNode root)
	{
		int count = 0;
		var pending = new Stack<JsonNode>();
		pending.Push(root);
		while (pending.TryPop(out JsonNode? node))
		{
			count++;
			switch (node)
			{
				case JsonObject obj:
					List<JsonNode?> values;
					try
					{
						values = obj.Select(property => property.Value).ToList();
					}
					catch (ArgumentException ex)
					{
						throw new InvalidOperationException(
							$"Failed to parse OpenAPI spec: an object at '{obj.GetPath()}' has the same key more than once. {ex.Message}", ex);
					}

					foreach (JsonNode? value in values)
					{
						if (value is not null)
						{
							pending.Push(value);
						}
					}

					break;
				case JsonArray array:
					foreach (JsonNode? item in array)
					{
						if (item is not null)
						{
							pending.Push(item);
						}
					}

					break;
			}
		}

		return count;
	}

	#endregion

	#region JSON Helpers

	/// <summary>A string value as is, a number or boolean as its JSON text, anything else as empty.</summary>
	private static string Text(JsonNode? node) =>
		node is JsonValue value
			? value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString()
			: "";

	private static bool IsTrue(JsonNode? node) => node is JsonValue value && value.GetValueKind() == JsonValueKind.True;

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

	/// <summary>
	///     The keywords of a schema: those written next to a <c>$ref</c> first, then the referenced
	///     schema's. OpenAPI 3.1 lets a <c>$ref</c> carry a <c>description</c> or <c>examples</c> of its own.
	/// </summary>
	private readonly record struct SchemaView(JsonObject Target, JsonObject? Siblings)
	{
		public JsonNode? this[string keyword] =>
			Siblings is not null && Siblings.TryGetPropertyValue(keyword, out JsonNode? own) ? own : Target[keyword];

		public bool Has(string keyword) =>
			(Siblings is not null && Siblings.ContainsKey(keyword)) || Target.ContainsKey(keyword);
	}

	/// <summary>
	///     A resolved <c>$ref</c>: the object it points at, the object that held it, the name shown for
	///     it and the pointer, which identifies it while its schema is being expanded.
	/// </summary>
	private readonly record struct Resolved(JsonObject Target, JsonObject? Siblings, string? RefName, string? Pointer)
	{
		public SchemaView View => new(Target, Siblings);
	}

	/// <summary>
	///     Where one schema walk is: the references being expanded on the way down and how deep it is.
	/// </summary>
	private sealed class SchemaWalk
	{
		/// <summary>The pointers of the references being expanded, to stop at a cycle.</summary>
		public HashSet<string> Expanding { get; } = new(StringComparer.Ordinal);

		/// <summary>How many references are expanded on the way down, not counting <c>allOf</c> parts.</summary>
		public int References { get; set; }

		/// <summary>How many schemas deep the walk is.</summary>
		public int Depth { get; set; }
	}

	/// <summary>
	///     Maps one document. Holds the root so <c>$ref</c> pointers resolve against it.
	/// </summary>
	/// <remarks>
	///     A reference is expanded only where nothing above it is a reference already; deeper ones keep their
	///     name, type, description and values, which is all a page shows of them. Expanding every reference
	///     made a 2.6 KB document whose schemas each referenced the next one twice take 28 seconds, and nine
	///     schemas that referenced each other 140 GB. <c>allOf</c> parts are merged at any depth, within a
	///     budget that grows with the document's size, and a walk stops after <see cref="_maxSchemaDepth" />
	///     schemas, so a long <c>allOf</c> chain can't overflow the stack.
	/// </remarks>
	private sealed class DocumentMapper(JsonObject root, int nodeCount)
	{
		private const int _maxSchemaDepth = 64;
		private const int _maxListedReferences = 10;

		private readonly JsonObject _root = root;
		private readonly bool _swagger2 = Text(root["swagger"]).StartsWith('2');
		private readonly List<string> _unresolved = [];
		private readonly HashSet<string> _unresolvedSeen = new(StringComparer.Ordinal);
		private readonly List<string> _warnings = [];
		private int _schemaBudget = 100_000 + (20 * nodeCount);
		private bool _budgetWarned;
		private bool _depthWarned;

		public OpenApiSpec Map()
		{
			var info = _root["info"] as JsonObject;
			var spec = new OpenApiSpec
			{
				Title = Text(info?["title"]),
				Description = Text(info?["description"]),
				Version = Text(info?["version"]),
				ExternalDocs = MapExternalDocs(_root["externalDocs"])
			};

			if (_swagger2)
			{
				spec.BasePath = Text(_root["basePath"]).TrimEnd('/');
			}
			else if (_root["servers"] is JsonArray { Count: > 0 } servers
			         && Uri.TryCreate(Text((servers[0] as JsonObject)?["url"]), UriKind.Absolute, out Uri? uri))
			{
				spec.BasePath = uri.AbsolutePath.TrimEnd('/');
			}

			MapServers(spec);
			MapSecuritySchemes(spec);
			spec.Security.AddRange(MapSecurity(_root["security"]) ?? []);

			if (_root["tags"] is JsonArray declaredTags)
			{
				foreach (JsonObject tag in declaredTags.OfType<JsonObject>())
				{
					if (Text(tag["name"]) is not { Length: > 0 } name)
					{
						continue;
					}

					if (Text(tag["description"]) is { Length: > 0 } description)
					{
						spec.TagDescriptions.TryAdd(name, description);
					}

					if (MapExternalDocs(tag["externalDocs"]) is { } tagDocs)
					{
						spec.TagExternalDocs.TryAdd(name, tagDocs);
					}
				}
			}

			JsonObject? schemas = _swagger2
				? _root["definitions"] as JsonObject
				: (_root["components"] as JsonObject)?["schemas"] as JsonObject;
			if (schemas is not null)
			{
				foreach ((string name, JsonNode? schema) in schemas)
				{
					spec.Schemas[name] = MapSchema(schema);
				}
			}

			var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (OpenApiEndpoint endpoint in MapPathItems(_root["paths"] as JsonObject))
			{
				foreach (string tag in endpoint.Tags.Where(tagSet.Add))
				{
					spec.Tags.Add(tag);
				}

				spec.Endpoints.Add(endpoint);
			}

			// OpenAPI 3.1: requests the API sends, keyed by a name instead of a path.
			spec.Webhooks.AddRange(MapPathItems(_root["webhooks"] as JsonObject));

			spec.Warnings.AddRange(_warnings);
			if (_unresolved.Count > 0)
			{
				string listed = string.Join(", ", _unresolved.Take(_maxListedReferences).Select(r => $"'{r}'"));
				string more = _unresolved.Count > _maxListedReferences ? $" and {_unresolved.Count - _maxListedReferences} more" : "";
				spec.Warnings.Add(
					$"{_unresolved.Count} $ref value(s) point outside the document or at nothing in it, so they show only their name: {listed}{more}.");
			}

			return spec;
		}

		/// <summary>The operations of a <c>paths</c> or <c>webhooks</c> map, in document order.</summary>
		private IEnumerable<OpenApiEndpoint> MapPathItems(JsonObject? items)
		{
			if (items is null)
			{
				yield break;
			}

			foreach ((string path, JsonNode? pathNode) in items)
			{
				if (pathNode is not JsonObject pathObject)
				{
					continue;
				}

				JsonObject pathItem = Resolve(pathObject).Target;
				foreach ((string method, JsonNode? operation) in pathItem)
				{
					if (operation is JsonObject operationObject && _methods.Contains(method, StringComparer.OrdinalIgnoreCase))
					{
						yield return MapEndpoint(method, path, operationObject, pathItem);
					}
				}
			}
		}

		#region Endpoints

		private OpenApiEndpoint MapEndpoint(string method, string path, JsonObject operation, JsonObject pathItem)
		{
			var endpoint = new OpenApiEndpoint
			{
				Method = method.ToUpperInvariant(),
				Path = path,
				Summary = Text(operation["summary"]),
				Description = Text(operation["description"]),
				OperationId = Text(operation["operationId"]),
				Deprecated = IsTrue(operation["deprecated"]),
				Security = MapSecurity(operation["security"]),
				ExternalDocs = MapExternalDocs(operation["externalDocs"])
			};

			if (!_swagger2)
			{
				endpoint.Servers = MapServerList(operation["servers"]) ?? MapServerList(pathItem["servers"]);
			}

			if (operation["tags"] is JsonArray tags)
			{
				// Tags group without regard to case, so ["Users", "users"] listed the endpoint twice on one page.
				endpoint.Tags.AddRange(tags.Select(Text).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
			}

			// Path-level parameters first; an operation parameter with the same name and location replaces one.
			List<JsonObject> parameters = ResolveParameters(pathItem["parameters"]);
			List<JsonObject> own = ResolveParameters(operation["parameters"]);
			var ownKeys = new HashSet<string>(own.Select(ParameterKey), StringComparer.OrdinalIgnoreCase);
			parameters.RemoveAll(p => ownKeys.Contains(ParameterKey(p)));
			parameters.AddRange(own);

			if (_swagger2)
			{
				MapSwagger2Body(endpoint, parameters, operation);
				parameters.RemoveAll(p => Text(p["in"]) is "body" or "formData");
			}
			else if (operation["requestBody"] is JsonObject requestBody)
			{
				JsonObject body = Resolve(requestBody).Target;
				endpoint.RequestBody = MapRequestBody(body);
				endpoint.ExampleRequestJson = ExtractExample(body["content"] as JsonObject);
			}

			endpoint.Parameters.AddRange(parameters.Select(MapParameter));

			if (operation["responses"] is JsonObject responses)
			{
				foreach ((string statusCode, JsonNode? responseNode) in responses)
				{
					if (responseNode is not JsonObject responseObject)
					{
						continue;
					}

					JsonObject response = Resolve(responseObject).Target;
					endpoint.Responses.Add(MapResponse(statusCode, response));

					// Capture first 2xx example
					if (endpoint.ExampleResponseJson is null && statusCode.StartsWith('2'))
					{
						endpoint.ExampleResponseJson = _swagger2
							? Swagger2ResponseExample(response)
							: ExtractExample(response["content"] as JsonObject);
					}
				}
			}

			return endpoint;
		}

		private List<JsonObject> ResolveParameters(JsonNode? node) =>
			node is JsonArray parameters
				? parameters.OfType<JsonObject>().Select(p => Resolve(p).Target).ToList()
				: [];

		private static string ParameterKey(JsonObject parameter) => $"{Text(parameter["name"])}:{Text(parameter["in"])}";

		private OpenApiParameter MapParameter(JsonObject parameter)
		{
			// A 2.0 parameter describes its value itself (type, format, items, enum); 3.x has a schema, or a
			// content map with one media type for a value sent as, say, JSON in a query string.
			JsonNode? schemaNode = _swagger2
				? parameter
				: parameter["schema"] ?? ((parameter["content"] as JsonObject)?.FirstOrDefault().Value as JsonObject)?["schema"];
			OpenApiSchema? schema = schemaNode is not null ? MapSchema(schemaNode) : null;
			if (_swagger2 && schema is not null)
			{
				// The parameter's description is shown in its own column.
				schema.Description = "";
			}

			return new OpenApiParameter
			{
				Name = Text(parameter["name"]),
				In = Text(parameter["in"]).ToLowerInvariant(),
				Description = Text(parameter["description"]),
				Required = IsTrue(parameter["required"]),
				SchemaType = schema?.ToDisplayString() ?? "",
				Schema = schema
			};
		}

		private OpenApiRequestBody MapRequestBody(JsonObject body)
		{
			var requestBody = new OpenApiRequestBody
			{
				Description = Text(body["description"]),
				Required = IsTrue(body["required"])
			};

			if (body["content"] is JsonObject content && content.FirstOrDefault() is { Key: { } contentType } first)
			{
				requestBody.ContentType = contentType;
				if ((first.Value as JsonObject)?["schema"] is { } schema)
				{
					requestBody.Schema = MapSchema(schema);
				}
			}

			return requestBody;
		}

		/// <summary>
		///     OpenAPI 2.0 puts the request body in a <c>body</c> parameter, or spreads a form over
		///     <c>formData</c> parameters, which become one object with a property each.
		/// </summary>
		private void MapSwagger2Body(OpenApiEndpoint endpoint, List<JsonObject> parameters, JsonObject operation)
		{
			string[] consumes = (operation["consumes"] ?? _root["consumes"]) is JsonArray list
				? list.Select(Text).ToArray()
				: [];

			if (parameters.FirstOrDefault(p => Text(p["in"]) == "body") is { } body)
			{
				endpoint.RequestBody = new OpenApiRequestBody
				{
					Description = Text(body["description"]),
					Required = IsTrue(body["required"]),
					ContentType = consumes.FirstOrDefault() ?? "application/json",
					Schema = body["schema"] is { } schema ? MapSchema(schema) : null
				};
				endpoint.ExampleRequestJson = SchemaExample(body["schema"] as JsonObject);
				return;
			}

			List<JsonObject> form = parameters.Where(p => Text(p["in"]) == "formData").ToList();
			if (form.Count == 0)
			{
				return;
			}

			var formSchema = new OpenApiSchema { Type = "object" };
			foreach (JsonObject field in form)
			{
				formSchema.Properties[Text(field["name"])] = MapSchema(field);
				if (IsTrue(field["required"]))
				{
					formSchema.RequiredProperties.Add(Text(field["name"]));
				}
			}

			bool hasFile = form.Any(f => Text(f["type"]) == "file");
			endpoint.RequestBody = new OpenApiRequestBody
			{
				Required = form.Any(f => IsTrue(f["required"])),
				ContentType = consumes.FirstOrDefault(c => c.Contains("form", StringComparison.OrdinalIgnoreCase))
				              ?? (hasFile ? "multipart/form-data" : "application/x-www-form-urlencoded"),
				Schema = formSchema
			};
		}

		private OpenApiResponse MapResponse(string statusCode, JsonObject response)
		{
			var result = new OpenApiResponse
			{
				StatusCode = statusCode,
				Description = Text(response["description"])
			};

			JsonNode? schema = _swagger2
				? response["schema"]
				: response["content"] is JsonObject content && content.FirstOrDefault().Value is JsonObject media
					? media["schema"]
					: null;
			if (schema is not null)
			{
				result.Schema = MapSchema(schema);
			}

			return result;
		}

		#endregion

		#region Servers

		private void MapServers(OpenApiSpec spec)
		{
			if (_swagger2)
			{
				MapSwagger2Servers(spec);
				return;
			}

			spec.Servers.AddRange(MapServerList(_root["servers"]) ?? []);
		}

		/// <summary>
		///     A <c>servers</c> list, or <c>null</c> when <paramref name="node" /> isn't one or names no server
		///     with a URL. A path or operation with such a list uses the servers above it, as with none.
		/// </summary>
		private static List<OpenApiServer>? MapServerList(JsonNode? node)
		{
			if (node is not JsonArray servers)
			{
				return null;
			}

			var result = new List<OpenApiServer>();
			foreach (JsonObject server in servers.OfType<JsonObject>())
			{
				string template = Text(server["url"]);
				if (template.Length > 0)
				{
					result.Add(MapServer(server, template));
				}
			}

			return result.Count > 0 ? result : null;
		}

		private static OpenApiServer MapServer(JsonObject server, string template)
		{
			var result = new OpenApiServer { Description = Text(server["description"]) };
			if (server["variables"] is JsonObject variables)
			{
				foreach ((string name, JsonNode? node) in variables)
				{
					if (node is not JsonObject variable)
					{
						continue;
					}

					var mapped = new OpenApiServerVariable
					{
						Name = name,
						Default = Text(variable["default"]),
						Description = Text(variable["description"])
					};
					if (variable["enum"] is JsonArray values)
					{
						mapped.EnumValues.AddRange(values.Select(JsonNodeToScalar));
					}

					result.Variables.Add(mapped);
				}
			}

			result.Url = result.Variables
				.Where(v => v.Default.Length > 0)
				.Aggregate(template, (url, v) => url.Replace("{" + v.Name + "}", v.Default, StringComparison.Ordinal));
			return result;
		}

		/// <summary>
		///     OpenAPI 2.0 names one host and base path, with the schemes it is served over. Without
		///     <c>schemes</c>, it is the scheme the document itself is served with, which a
		///     protocol-relative URL says; without a <c>host</c>, it is the document's host.
		/// </summary>
		private void MapSwagger2Servers(OpenApiSpec spec)
		{
			string host = Text(_root["host"]).Trim().TrimEnd('/');
			string basePath = Text(_root["basePath"]).Trim().Trim('/');
			string path = basePath.Length == 0 ? "" : "/" + basePath;
			if (host.Length == 0)
			{
				if (path.Length > 0)
				{
					spec.Servers.Add(new OpenApiServer { Url = path });
				}

				return;
			}

			string[] schemes = _root["schemes"] is JsonArray list
				? list.Select(Text).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
				: [];
			if (schemes.Length == 0)
			{
				spec.Servers.Add(new OpenApiServer { Url = $"//{host}{path}" });
				return;
			}

			spec.Servers.AddRange(schemes.Select(scheme => new OpenApiServer { Url = $"{scheme.ToLowerInvariant()}://{host}{path}" }));
		}

		/// <summary>An <c>externalDocs</c> object, or <c>null</c> when it isn't one or has no URL.</summary>
		private static OpenApiExternalDocs? MapExternalDocs(JsonNode? node) =>
			node is JsonObject docs && Text(docs["url"]).Trim() is { Length: > 0 } url
				? new OpenApiExternalDocs { Url = url, Description = Text(docs["description"]) }
				: null;

		#endregion

		#region Security

		private void MapSecuritySchemes(OpenApiSpec spec)
		{
			JsonObject? schemes = _swagger2
				? _root["securityDefinitions"] as JsonObject
				: (_root["components"] as JsonObject)?["securitySchemes"] as JsonObject;
			if (schemes is null)
			{
				return;
			}

			foreach ((string name, JsonNode? node) in schemes)
			{
				if (node is JsonObject scheme)
				{
					spec.SecuritySchemes[name] = MapSecurityScheme(name, Resolve(scheme).Target);
				}
			}
		}

		private OpenApiSecurityScheme MapSecurityScheme(string name, JsonObject scheme)
		{
			var result = new OpenApiSecurityScheme
			{
				Name = name,
				Type = Text(scheme["type"]),
				Description = Text(scheme["description"]),
				ParameterName = Text(scheme["name"]),
				In = Text(scheme["in"]),
				Scheme = Text(scheme["scheme"]),
				BearerFormat = Text(scheme["bearerFormat"]),
				OpenIdConnectUrl = Text(scheme["openIdConnectUrl"])
			};

			if (_swagger2)
			{
				if (result.Type == "basic")
				{
					result.Type = "http";
					result.Scheme = "basic";
				}
				else if (result.Type == "oauth2" && Text(scheme["flow"]) is { Length: > 0 } flow)
				{
					// A 2.0 scheme has one flow, with its URLs and scopes on the scheme itself.
					string kind = flow switch
					{
						"application" => "clientCredentials",
						"accessCode" => "authorizationCode",
						_ => flow
					};
					result.Flows.Add(MapFlow(kind, scheme));
				}
			}
			else if (scheme["flows"] is JsonObject flows)
			{
				foreach ((string kind, JsonNode? flow) in flows)
				{
					if (flow is JsonObject flowObject)
					{
						result.Flows.Add(MapFlow(kind, flowObject));
					}
				}
			}

			return result;
		}

		private static OpenApiOAuthFlow MapFlow(string kind, JsonObject flow)
		{
			var result = new OpenApiOAuthFlow
			{
				Kind = kind,
				AuthorizationUrl = Text(flow["authorizationUrl"]),
				TokenUrl = Text(flow["tokenUrl"]),
				RefreshUrl = Text(flow["refreshUrl"])
			};

			if (flow["scopes"] is JsonObject scopes)
			{
				foreach ((string scope, JsonNode? description) in scopes)
				{
					result.Scopes[scope] = Text(description);
				}
			}

			return result;
		}

		/// <summary>
		///     A <c>security</c> list, or <c>null</c> when <paramref name="node" /> isn't one, which for an
		///     operation means the document's list applies.
		/// </summary>
		private static List<OpenApiSecurityRequirement>? MapSecurity(JsonNode? node)
		{
			if (node is not JsonArray alternatives)
			{
				return null;
			}

			var result = new List<OpenApiSecurityRequirement>();
			foreach (JsonObject alternative in alternatives.OfType<JsonObject>())
			{
				var requirement = new OpenApiSecurityRequirement();
				foreach ((string scheme, JsonNode? scopes) in alternative)
				{
					requirement.Schemes[scheme] = scopes is JsonArray list
						? list.Select(Text).Where(s => s.Length > 0).ToList()
						: [];
				}

				result.Add(requirement);
			}

			return result;
		}

		#endregion

		#region Examples

		/// <summary>
		///     Extracts an example JSON string from the first media type of a <c>content</c> map.
		/// </summary>
		private string? ExtractExample(JsonObject? content)
		{
			if (content?.FirstOrDefault().Value is not JsonObject mediaType)
			{
				return null;
			}

			// Media type level example
			if (mediaType["example"] is { } example)
			{
				return FormatJsonNode(example);
			}

			// Examples map: the first one's value, which may sit behind a $ref to components/examples
			if (mediaType["examples"] is JsonObject examples
			    && examples.FirstOrDefault().Value is JsonObject first
			    && Resolve(first).Target["value"] is { } value)
			{
				return FormatJsonNode(value);
			}

			return SchemaExample(mediaType["schema"] as JsonObject);
		}

		/// <summary>
		///     A schema's example: the first of the <c>examples</c> array of OpenAPI 3.1, which a $ref may
		///     carry next to it, then the singular <c>example</c> of 3.0.
		/// </summary>
		private string? SchemaExample(JsonObject? schema)
		{
			if (schema is null)
			{
				return null;
			}

			SchemaView view = Resolve(schema).View;
			if (view["examples"] is JsonArray { Count: > 0 } examples && examples[0] is { } first)
			{
				return FormatJsonNode(first);
			}

			return view["example"] is { } example ? FormatJsonNode(example) : null;
		}

		/// <summary>OpenAPI 2.0 keeps response examples in a map keyed by media type.</summary>
		private string? Swagger2ResponseExample(JsonObject response)
		{
			if (response["examples"] is JsonObject examples && examples.FirstOrDefault().Value is { } example)
			{
				return FormatJsonNode(example);
			}

			return SchemaExample(response["schema"] as JsonObject);
		}

		#endregion

		#region References

		/// <summary>
		///     Follows a <c>$ref</c>, and a chain of them, to the object it points at. An object without one
		///     resolves to itself. A reference that leads outside the document, or nowhere, resolves to an
		///     empty object, keeps its name and is listed in a warning.
		/// </summary>
		private Resolved Resolve(JsonObject node)
		{
			if (Text(node["$ref"]) is not { Length: > 0 } reference)
			{
				return new Resolved(node, null, null, null);
			}

			var target = new JsonObject();
			var seen = new HashSet<string>(StringComparer.Ordinal);
			string? next = reference;
			while (next is { Length: > 0 } && seen.Add(next))
			{
				if (ResolvePointer(next) is not JsonObject found)
				{
					if (_unresolvedSeen.Add(next))
					{
						_unresolved.Add(next);
					}

					break;
				}

				target = found;
				next = Text(found["$ref"]);
			}

			return new Resolved(target, node.Count > 1 ? node : null, RefName(reference), reference);
		}

		/// <summary>Resolves a pointer within this document, such as <c>#/components/schemas/Pet</c>.</summary>
		private JsonNode? ResolvePointer(string reference)
		{
			if (!reference.StartsWith('#'))
			{
				return null;
			}

			JsonNode? current = _root;
			foreach (string segment in reference[1..].Split('/', StringSplitOptions.RemoveEmptyEntries))
			{
				string key = Uri.UnescapeDataString(segment).Replace("~1", "/").Replace("~0", "~");
				current = current switch
				{
					JsonObject obj => obj[key],
					JsonArray array when int.TryParse(key, out int index) && index >= 0 && index < array.Count => array[index],
					_ => null
				};

				if (current is null)
				{
					return null;
				}
			}

			return current;
		}

		/// <summary>The name a reference is shown under: the last segment, <c>Pet</c> for <c>#/components/schemas/Pet</c>.</summary>
		private static string RefName(string reference)
		{
			string last = reference[(reference.LastIndexOfAny(['/', '#']) + 1)..];
			string name = Uri.UnescapeDataString(last).Replace("~1", "/").Replace("~0", "~");
			return name.Length > 0 ? name : reference;
		}

		#endregion

		#region Schema Mapping

		/// <summary>
		///     Maps a schema that starts a walk: a component, or the schema of a parameter, a request body or
		///     a response.
		/// </summary>
		private OpenApiSchema MapSchema(JsonNode? node) => MapSchema(node, new SchemaWalk(), false);

		/// <summary>
		///     Maps a schema to the local model.
		/// </summary>
		/// <param name="node">The schema, possibly a <c>$ref</c>.</param>
		/// <param name="walk">
		///     Where the walk is. A reference already being expanded higher up (a tree node whose children
		///     are the same type, say) is not expanded again.
		/// </param>
		/// <param name="allOfPart">Whether the schema is an <c>allOf</c> part whose properties merge into its parent's.</param>
		private OpenApiSchema MapSchema(JsonNode? node, SchemaWalk walk, bool allOfPart)
		{
			// A JSON Schema can also be true or false (3.1); true allows anything.
			if (node is not JsonObject schemaObject)
			{
				return new OpenApiSchema();
			}

			Resolved resolved = Resolve(schemaObject);
			if (walk.Depth >= _maxSchemaDepth)
			{
				Warn(ref _depthWarned, $"A schema nests more than {_maxSchemaDepth} schemas deep; the schemas below that show only their type.");
				return Stub(resolved);
			}

			if (walk.Expanding.Count > 0)
			{
				_schemaBudget--;
			}

			if (resolved.Pointer is { } pointer)
			{
				if (walk.Expanding.Contains(pointer) || (walk.References > 0 && !allOfPart))
				{
					return Stub(resolved);
				}

				if (_schemaBudget < 0)
				{
					Warn(ref _budgetWarned, "The schemas reference each other too often to expand every reference; the rest show only their name.");
					return Stub(resolved);
				}

				walk.Expanding.Add(pointer);
				walk.References += allOfPart ? 0 : 1;
			}

			walk.Depth++;
			try
			{
				return MapExpanded(resolved, walk);
			}
			finally
			{
				walk.Depth--;
				if (resolved.Pointer is { } expanded)
				{
					walk.Expanding.Remove(expanded);
					walk.References -= allOfPart ? 0 : 1;
				}
			}
		}

		private OpenApiSchema MapExpanded(Resolved resolved, SchemaWalk walk)
		{
			SchemaView schema = resolved.View;
			string? refName = resolved.RefName;
			var result = new OpenApiSchema
			{
				// Must stay null (not "") for non-references: OpenApiSchema.ToDisplayString
				// returns RefName whenever it is non-null.
				RefName = refName,
				Type = TypeName(schema),
				Format = Text(schema["format"]),
				Description = Text(schema["description"]),
				Nullable = IsNullable(schema),
				ExternalDocs = MapExternalDocs(schema["externalDocs"])
			};

			if (_swagger2 && Text(schema["type"]) == "file")
			{
				// 2.0's upload type, which 3.x writes as a binary string.
				result.Type = "string";
				result.Format = "binary";
			}

			List<JsonNode?> alternatives = [.. Alternatives(schema, "oneOf"), .. Alternatives(schema, "anyOf")];

			// A single allOf part with nothing else is how OpenAPI 3.0 gives a $ref a description or
			// nullable: true. It stands for the schema it wraps, so it is expanded like a reference.
			JsonArray? parts = schema["allOf"] as JsonArray;
			bool wrapper = parts is { Count: 1 } && refName is null && IsBare(schema) && alternatives.Count == 0;

			// allOf parts come first: with inheritance, the base type's properties lead.
			List<OpenApiSchema> allOf = parts?.Select(part => MapSchema(part, walk, !wrapper)).ToList() ?? [];
			if (wrapper)
			{
				return Unwrap(allOf[0], result);
			}

			foreach (OpenApiSchema part in allOf)
			{
				foreach ((string name, OpenApiSchema property) in part.Properties)
				{
					result.Properties.TryAdd(name, property);
				}

				foreach (string required in part.RequiredProperties.Where(r => !result.RequiredProperties.Contains(r)).ToList())
				{
					result.RequiredProperties.Add(required);
				}
			}

			if (allOf.Count > 0 && result.Type.Length == 0)
			{
				result.Type = "object";
			}

			// OpenAPI 3.1 (and .NET 10's generator) writes a nullable reference as
			// oneOf: [{"type": "null"}, {"$ref": ...}], since a $ref can't carry a type of its own.
			foreach (JsonNode? alternative in alternatives)
			{
				if (IsNullAlternative(alternative))
				{
					result.Nullable = true;
					continue;
				}

				result.Variants.Add(MapSchema(alternative, walk, false));
			}

			if (result.Variants.Count == 1 && refName is null && allOf.Count == 0 && IsBare(schema))
			{
				return Unwrap(result.Variants[0], result);
			}

			// Array items
			if (result.Type == "array" && schema["items"] is JsonObject items)
			{
				result.Items = MapSchema(items, walk, false);
			}

			// Map values, as for a Dictionary<string, T>
			if (schema["additionalProperties"] is JsonObject additional)
			{
				result.AdditionalProperties = MapSchema(additional, walk, false);
			}

			// Object properties
			if (schema["properties"] is JsonObject properties)
			{
				foreach ((string name, JsonNode? property) in properties)
				{
					result.Properties[name] = MapSchema(property, walk, false);
				}
			}

			// Required properties
			if (schema["required"] is JsonArray requiredNames)
			{
				foreach (string required in requiredNames.Select(Text)
					         .Where(r => r.Length > 0 && !result.RequiredProperties.Contains(r)))
				{
					result.RequiredProperties.Add(required);
				}
			}

			AddValues(result, schema);
			return result;
		}

		/// <summary>
		///     A schema that is not expanded: a reference's name, with the type, description, nullability and
		///     allowed values of the schema it points at. That is all a page shows of a schema whose
		///     properties it doesn't list.
		/// </summary>
		private static OpenApiSchema Stub(Resolved resolved)
		{
			SchemaView schema = resolved.View;
			var stub = new OpenApiSchema
			{
				RefName = resolved.RefName,
				Type = TypeName(schema),
				Format = Text(schema["format"]),
				Description = Text(schema["description"]),
				Nullable = IsNullable(schema)
				           || Alternatives(schema, "oneOf").Concat(Alternatives(schema, "anyOf")).Any(IsNullAlternative)
			};
			AddValues(stub, schema);
			return stub;
		}

		/// <summary>Enum values, and a 3.1 <c>const</c> as the one allowed value.</summary>
		private static void AddValues(OpenApiSchema result, SchemaView schema)
		{
			if (schema["enum"] is JsonArray values)
			{
				result.EnumValues.AddRange(values.Select(JsonNodeToScalar));
			}
			else if (schema.Has("const"))
			{
				result.EnumValues.Add(JsonNodeToScalar(schema["const"]));
			}

			if (result.Type.Length == 0 && result.EnumValues.Count > 0)
			{
				// Generators leave the type off an enum ("enum": ["Books", "Music"]), which would
				// otherwise show as "any".
				result.Type = InferValueType(schema);
			}
		}

		private static bool IsNullAlternative(JsonNode? alternative) =>
			alternative is JsonObject alternativeObject && !alternativeObject.ContainsKey("$ref")
			                                            && TypeName(new SchemaView(alternativeObject, null)) == "null";

		/// <summary>Adds a warning the first time a limit is reached.</summary>
		private void Warn(ref bool warned, string message)
		{
			if (!warned)
			{
				warned = true;
				_warnings.Add(message);
			}
		}

		private static IEnumerable<JsonNode?> Alternatives(SchemaView schema, string keyword) =>
			schema[keyword] as JsonArray ?? [];

		/// <summary>
		///     Whether a schema has nothing of its own besides composition keywords, a description and
		///     nullability, so a single <c>allOf</c> part or <c>oneOf</c> alternative can stand for it.
		/// </summary>
		private static bool IsBare(SchemaView schema) =>
			TypeName(schema) is "" or "null"
			&& schema["properties"] is not JsonObject { Count: > 0 }
			&& schema["items"] is null
			&& schema["enum"] is not JsonArray { Count: > 0 }
			&& !schema.Has("const");

		/// <summary>
		///     Returns the schema a wrapper stands for, keeping the wrapper's description, nullability and
		///     external documentation.
		/// </summary>
		private static OpenApiSchema Unwrap(OpenApiSchema inner, OpenApiSchema wrapper)
		{
			inner.Nullable |= wrapper.Nullable;
			if (wrapper.Description.Length > 0)
			{
				inner.Description = wrapper.Description;
			}

			inner.ExternalDocs = wrapper.ExternalDocs ?? inner.ExternalDocs;
			return inner;
		}

		/// <summary>
		///     A <c>null</c> in the type list (3.1), <c>nullable: true</c> (3.0, kept even on a schema with no
		///     <c>type</c>), or <c>x-nullable: true</c> (a common 2.0 extension).
		/// </summary>
		private static bool IsNullable(SchemaView schema) =>
			TypeNames(schema).Contains("null") || IsTrue(schema["nullable"]) || IsTrue(schema["x-nullable"]);

		private static List<string> TypeNames(SchemaView schema) => schema["type"] switch
		{
			JsonArray list => list.Select(Text).Where(t => t.Length > 0).ToList(),
			JsonValue single when Text(single) is { Length: > 0 } name => [name],
			_ => []
		};

		/// <summary>
		///     Renders a schema's type as its lowercase spec name. OpenAPI 3.1 allows a list of types
		///     (<c>["string", "null"]</c>): the <c>null</c> type is left out (it sets
		///     <see cref="OpenApiSchema.Nullable" />), and several types are joined with <c>|</c>.
		/// </summary>
		private static string TypeName(SchemaView schema)
		{
			List<string> names = TypeNames(schema);
			if (names.Count == 0)
			{
				return "";
			}

			var types = names.Where(n => n != "null").Distinct().ToList();
			if (types.Count == 0)
			{
				return "null";
			}

			if (types.Contains("string") && types.Count > 1
			                             && types.All(t => t is "string" or "integer" or "number")
			                             && (schema["pattern"] is not null || IsNumericFormat(Text(schema["format"]))))
			{
				// .NET 10 writes a number that may also arrive as a string (JsonNumberHandling.AllowReadingFromString,
				// on by default in web apps) as ["integer", "string"] with a digits-only pattern. It is a number.
				types.Remove("string");
			}

			return string.Join(" | ", types.OrderBy(t => Array.IndexOf(_typeOrder, t) is var i and >= 0 ? i : int.MaxValue));
		}

		private static bool IsNumericFormat(string format) =>
			format is "int32" or "int64" or "float" or "double" or "decimal" or "int16" or "uint8" or "int8";

		/// <summary>
		///     The type of a schema with an <c>enum</c> or <c>const</c> but no <c>type</c>, from its values.
		/// </summary>
		private static string InferValueType(SchemaView schema)
		{
			List<JsonNode> values = schema["enum"] is JsonArray list
				? list.OfType<JsonNode>().ToList()
				: schema["const"] is { } constant
					? [constant]
					: [];

			JsonValueKind[] kinds = values.Select(v => v.GetValueKind()).Distinct().ToArray();
			return kinds switch
			{
				[JsonValueKind.String] => "string",
				[JsonValueKind.Number] => values.All(v => v.ToJsonString().All(c => char.IsDigit(c) || c == '-'))
					? "integer"
					: "number",
				[JsonValueKind.True] or [JsonValueKind.False] or [JsonValueKind.True, JsonValueKind.False]
					or [JsonValueKind.False, JsonValueKind.True] => "boolean",
				_ => ""
			};
		}

		#endregion
	}
}
