using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Reads an OpenAPI specification (JSON or YAML, versions 2.0/3.0/3.1)
///     using the official <c>Microsoft.OpenApi.Readers</c> package and maps
///     the result to MokaDocs' own <see cref="OpenApiSpec" /> model.
/// </summary>
public static class OpenApiParser
{
    /// <summary>
    ///     Parses the given specification string (JSON or YAML).
    /// </summary>
    /// <param name="content">Raw content of the OpenAPI spec file.</param>
    /// <returns>A populated <see cref="OpenApiSpec" /> instance.</returns>
    public static OpenApiSpec Parse(string content)
    {
        var reader = new OpenApiStringReader();
        var document = reader.Read(content, out var diagnostic);

        if (document is null)
        {
            var errors = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Failed to parse OpenAPI spec: {errors}");
        }

        return MapDocument(document);
    }

    /// <summary>
    ///     Parses an OpenAPI spec from a stream (JSON or YAML).
    /// </summary>
    public static OpenApiSpec Parse(Stream stream)
    {
        var reader = new OpenApiStreamReader();
        var document = reader.Read(stream, out var diagnostic);

        if (document is null)
        {
            var errors = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Failed to parse OpenAPI spec: {errors}");
        }

        return MapDocument(document);
    }

    private static OpenApiSpec MapDocument(OpenApiDocument doc)
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
            var serverUrl = doc.Servers[0].Url;
            if (Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
                spec.BasePath = uri.AbsolutePath.TrimEnd('/');
        }

        // Schemas
        if (doc.Components?.Schemas is not null)
            foreach (var (name, schema) in doc.Components.Schemas)
                spec.Schemas[name] = MapSchema(schema);

        // Endpoints from paths
        var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (doc.Paths is not null)
            foreach (var (pathStr, pathItem) in doc.Paths)
            foreach (var (operationType, operation) in pathItem.Operations)
            {
                var endpoint = MapEndpoint(operationType, pathStr, operation, pathItem);

                foreach (var tag in endpoint.Tags)
                    if (tagSet.Add(tag))
                        spec.Tags.Add(tag);

                spec.Endpoints.Add(endpoint);
            }

        return spec;
    }

    private static OpenApiEndpoint MapEndpoint(
        OperationType method,
        string path,
        OpenApiOperation operation,
        OpenApiPathItem pathItem)
    {
        var endpoint = new OpenApiEndpoint
        {
            Method = method.ToString().ToUpperInvariant(),
            Path = path,
            Summary = operation.Summary ?? "",
            Description = operation.Description ?? "",
            OperationId = operation.OperationId ?? "",
            Deprecated = operation.Deprecated
        };

        // Tags
        if (operation.Tags is not null)
            foreach (var tag in operation.Tags)
                if (!string.IsNullOrEmpty(tag.Name))
                    endpoint.Tags.Add(tag.Name);

        // Merge path-level + operation-level parameters
        var allParams = new List<Microsoft.OpenApi.Models.OpenApiParameter>();

        // Path-level parameters first
        if (pathItem.Parameters is not null)
            foreach (var p in pathItem.Parameters)
                allParams.Add(p);

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

        foreach (var p in allParams) endpoint.Parameters.Add(MapParameter(p));

        // Request body
        if (operation.RequestBody is not null)
        {
            endpoint.RequestBody = MapRequestBody(operation.RequestBody);
            endpoint.ExampleRequestJson = ExtractExample(operation.RequestBody.Content);
        }

        // Responses
        if (operation.Responses is not null)
            foreach (var (statusCode, resp) in operation.Responses)
            {
                var response = MapResponse(statusCode, resp);
                endpoint.Responses.Add(response);

                // Capture first 2xx example
                if (endpoint.ExampleResponseJson is null && statusCode.StartsWith('2'))
                {
                    var example = ExtractExample(resp.Content);
                    if (example is not null)
                        endpoint.ExampleResponseJson = example;
                }
            }

        return endpoint;
    }

    private static OpenApiParameter MapParameter(Microsoft.OpenApi.Models.OpenApiParameter param)
    {
        return new OpenApiParameter
        {
            Name = param.Name ?? "",
            In = param.In?.ToString()?.ToLowerInvariant() ?? "",
            Description = param.Description ?? "",
            Required = param.Required,
            SchemaType = param.Schema is not null ? SchemaToTypeString(param.Schema) : ""
        };
    }

    private static OpenApiRequestBody MapRequestBody(Microsoft.OpenApi.Models.OpenApiRequestBody body)
    {
        var rb = new OpenApiRequestBody
        {
            Description = body.Description ?? "",
            Required = body.Required
        };

        if (body.Content is { Count: > 0 })
        {
            var first = body.Content.First();
            rb.ContentType = first.Key;
            if (first.Value.Schema is not null)
                rb.Schema = MapSchema(first.Value.Schema);
        }

        return rb;
    }

    private static OpenApiResponse MapResponse(string statusCode, Microsoft.OpenApi.Models.OpenApiResponse resp)
    {
        var response = new OpenApiResponse
        {
            StatusCode = statusCode,
            Description = resp.Description ?? ""
        };

        if (resp.Content is { Count: > 0 })
        {
            var first = resp.Content.First();
            if (first.Value.Schema is not null)
                response.Schema = MapSchema(first.Value.Schema);
        }

        return response;
    }

    private static OpenApiSchema MapSchema(Microsoft.OpenApi.Models.OpenApiSchema schema)
    {
        // Handle reference
        if (schema.Reference is not null)
        {
            var refName = schema.Reference.Id ?? "";

            // Microsoft.OpenApi auto-resolves references, so we can still get properties
            var resolved = new OpenApiSchema
            {
                RefName = refName,
                Type = schema.Type ?? "",
                Format = schema.Format ?? "",
                Description = schema.Description ?? ""
            };

            // Map properties from the resolved reference
            if (schema.Properties is not null)
                foreach (var (name, propSchema) in schema.Properties)
                    resolved.Properties[name] = MapSchema(propSchema);

            if (schema.Required is not null)
                foreach (var req in schema.Required)
                    resolved.RequiredProperties.Add(req);

            return resolved;
        }

        var result = new OpenApiSchema
        {
            Type = schema.Type ?? "",
            Format = schema.Format ?? "",
            Description = schema.Description ?? ""
        };

        // Array items
        if (result.Type == "array" && schema.Items is not null)
            result.Items = MapSchema(schema.Items);

        // Object properties
        if (schema.Properties is not null)
            foreach (var (name, propSchema) in schema.Properties)
                result.Properties[name] = MapSchema(propSchema);

        // Required properties
        if (schema.Required is not null)
            foreach (var req in schema.Required)
                result.RequiredProperties.Add(req);

        // Enum values
        if (schema.Enum is not null)
            foreach (var val in schema.Enum)
                if (val is OpenApiString strVal)
                    result.EnumValues.Add(strVal.Value);
                else
                    result.EnumValues.Add(val.ToString() ?? "");

        return result;
    }

    /// <summary>
    ///     Extracts an example JSON string from content media types.
    /// </summary>
    private static string? ExtractExample(IDictionary<string, OpenApiMediaType>? content)
    {
        if (content is null or { Count: 0 })
            return null;

        foreach (var (_, mediaType) in content)
        {
            // Check media type level example
            if (mediaType.Example is not null)
                return FormatOpenApiAny(mediaType.Example);

            // Check examples map
            if (mediaType.Examples is { Count: > 0 })
            {
                var first = mediaType.Examples.Values.First();
                if (first.Value is not null)
                    return FormatOpenApiAny(first.Value);
            }

            // Check schema-level example
            if (mediaType.Schema?.Example is not null)
                return FormatOpenApiAny(mediaType.Schema.Example);

            break;
        }

        return null;
    }

    /// <summary>
    ///     Serializes an <see cref="Microsoft.OpenApi.Any.IOpenApiAny" /> to a pretty-printed JSON string.
    /// </summary>
    private static string FormatOpenApiAny(IOpenApiAny any)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            WriteOpenApiAny(writer, any);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteOpenApiAny(Utf8JsonWriter writer, IOpenApiAny any)
    {
        switch (any)
        {
            case OpenApiObject obj:
                writer.WriteStartObject();
                foreach (var (key, value) in obj)
                {
                    writer.WritePropertyName(key);
                    WriteOpenApiAny(writer, value);
                }

                writer.WriteEndObject();
                break;

            case OpenApiArray arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                    WriteOpenApiAny(writer, item);
                writer.WriteEndArray();
                break;

            case OpenApiString str:
                writer.WriteStringValue(str.Value);
                break;

            case OpenApiInteger intVal:
                writer.WriteNumberValue(intVal.Value);
                break;

            case OpenApiLong longVal:
                writer.WriteNumberValue(longVal.Value);
                break;

            case OpenApiFloat floatVal:
                writer.WriteNumberValue(floatVal.Value);
                break;

            case OpenApiDouble doubleVal:
                writer.WriteNumberValue(doubleVal.Value);
                break;

            case OpenApiBoolean boolVal:
                writer.WriteBooleanValue(boolVal.Value);
                break;

            case OpenApiDate dateVal:
                writer.WriteStringValue(dateVal.Value.ToString("yyyy-MM-dd"));
                break;

            case OpenApiDateTime dateTimeVal:
                writer.WriteStringValue(dateTimeVal.Value.ToString("O"));
                break;

            case OpenApiNull:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteStringValue(any.ToString() ?? "");
                break;
        }
    }

    /// <summary>
    ///     Produces a concise type string from a schema (for parameter display).
    /// </summary>
    private static string SchemaToTypeString(Microsoft.OpenApi.Models.OpenApiSchema schema)
    {
        if (schema.Reference is not null)
            return schema.Reference.Id ?? "object";

        var type = schema.Type ?? "";

        if (type == "array" && schema.Items is not null)
            return $"{SchemaToTypeString(schema.Items)}[]";

        if (!string.IsNullOrEmpty(schema.Format))
            return $"{type} ({schema.Format})";

        return type;
    }
}