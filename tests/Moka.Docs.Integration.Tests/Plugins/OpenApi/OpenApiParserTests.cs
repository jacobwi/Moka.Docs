using FluentAssertions;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.Plugins.OpenApi;

/// <summary>
///     Reads documents that real generators wrote for one small minimal-API app (see
///     <c>Fixtures</c>): Microsoft.AspNetCore.OpenApi 9 (OpenAPI 3.0), Microsoft.AspNetCore.OpenApi 10
///     (OpenAPI 3.1) and Swashbuckle 10 (OpenAPI 3.0). The app's model has nullable and non-nullable
///     values, an enum, a dictionary, a self-referencing type and a polymorphic type.
/// </summary>
public sealed class OpenApiParserTests
{
	internal static string Fixture(string name) =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Plugins", "OpenApi", "Fixtures", name));

	private static OpenApiSchema Schema(OpenApiSpec spec, string name) => spec.Schemas[name];

	private static string Property(OpenApiSpec spec, string schema, string property) =>
		spec.Schemas[schema].Properties[property].ToDisplayString();

	private static OpenApiEndpoint Endpoint(OpenApiSpec spec, string method, string path) =>
		spec.Endpoints.Single(e => e.Method == method && e.Path == path);

	[Fact]
	public void AspNetCoreOpenApi10_TypeArrays_ShowTheTypeAndNullability()
	{
		// 3.1 writes nullability into the type list, and .NET 10 adds "string" to every number,
		// which used to show as "string, integer" or, with the null, lose the null.
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json"));

		Property(spec, "Product", "id").Should().Be("integer (int32)");
		Property(spec, "Product", "price").Should().Be("number (double)");
		Property(spec, "Product", "salePrice").Should().Be("number (double) | null");
		Property(spec, "Product", "description").Should().Be("string | null");
		Property(spec, "Product", "name").Should().Be("string");
		Property(spec, "Product", "discontinuedOn").Should().Be("string (date) | null");
		Property(spec, "PagedResultOfProduct", "nextPage").Should().Be("integer (int32) | null");
		Endpoint(spec, "GET", "/api/products").Parameters.Single(p => p.Name == "page").SchemaType
			.Should().Be("integer (int32)");
	}

	[Fact]
	public void AspNetCoreOpenApi10_NullableReference_IsTheReferencedSchemaOrNull()
	{
		// .NET 10 writes a nullable complex property as oneOf: [{"type": "null"}, {"$ref": ...}], with
		// the property's description next to the $ref. It used to show as "object", without a description.
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json"));

		OpenApiSchema supplier = Schema(spec, "Product").Properties["supplier"];
		supplier.ToDisplayString().Should().Be("Supplier | null");
		supplier.RefName.Should().Be("Supplier");
		supplier.Description.Should().Be("Who supplies it, if known.");
		Property(spec, "Category", "parent").Should().Be("Category | null");
		Property(spec, "Category", "children").Should().Be("Category[]");
		Endpoint(spec, "GET", "/api/discounts/{code}").Responses.Single().Schema!.ToDisplayString()
			.Should().Be("Discount | null");
	}

	[Fact]
	public void AspNetCoreOpenApi10_EnumsMapsAndPolymorphism()
	{
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json"));

		OpenApiSchema category = Schema(spec, "Product").Properties["category"];
		category.ToDisplayString().Should().Be("ProductCategory");
		category.EnumValues.Should().Equal("Electronics", "Books", "Clothing");
		category.Type.Should().Be("string", "the enum has no type of its own, so it comes from the values");
		Property(spec, "Product", "attributes").Should().Be("map<string, string>");
		Property(spec, "HttpValidationProblemDetails", "errors").Should().Be("map<string, string[]>");
		Schema(spec, "Discount").Variants.Select(v => v.ToDisplayString())
			.Should().Equal("DiscountPercentDiscount", "DiscountFixedDiscount");
		Schema(spec, "DiscountFixedDiscount").Properties["kind"].EnumValues.Should().Equal("fixed");
	}

	[Fact]
	public void AspNetCoreOpenApi10_XmlDocComments_AreDescriptions()
	{
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json"));

		Schema(spec, "Product").Description.Should().Be("A product in the catalog.");
		Schema(spec, "Product").Properties["id"].Description.Should().Be("Product id.");
		Schema(spec, "Product").RequiredProperties.Should().Equal("name");
	}

	[Fact]
	public void AspNetCoreOpenApi9_NullableTrue_ShowsAsNull()
	{
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-9.json"));

		Property(spec, "Product", "description").Should().Be("string | null");
		Property(spec, "Product", "salePrice").Should().Be("number (double) | null");
		Property(spec, "Product", "id").Should().Be("integer (int32)");
		// .NET 9 marks the Supplier component itself nullable, so every reference to it is.
		Property(spec, "Product", "supplier").Should().Be("Supplier | null");
		Property(spec, "Product", "dimensions").Should().Be("Dimensions");
		// .NET 9 writes the items of a self-referencing list as an empty schema.
		Property(spec, "Category", "children").Should().Be("any[]");
		Endpoint(spec, "GET", "/api/discounts/{code}").Responses.Single().Schema!.ToDisplayString()
			.Should().Be("Discount");
	}

	[Fact]
	public void Swashbuckle10_Document_IsRead()
	{
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("swashbuckle-10.json"));

		spec.Title.Should().Be("Shop");
		spec.Tags.Should().BeEquivalentTo("Products", "Categories", "Discounts", "Shop");
		Property(spec, "Product", "tags").Should().Be("string[] | null");
		Property(spec, "Product", "category").Should().Be("ProductCategory");
		Schema(spec, "ProductCategory").EnumValues.Should().Equal("Electronics", "Books", "Clothing");
		Endpoint(spec, "GET", "/api/products").Responses.Single().Schema!.ToDisplayString()
			.Should().Be("ProductPagedResult");
		Endpoint(spec, "PUT", "/api/products/{id}/tags").Parameters.Select(p => $"{p.Name}:{p.In}:{p.SchemaType}")
			.Should().Equal("id:path:integer (int32)", "X-Request-Id:header:string");
	}

	[Fact]
	public void OpenApi31_Keywords_AreRead()
	{
		const string document = """
		                        {
		                          "openapi": "3.1.0",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": {
		                            "/pets": {
		                              "post": {
		                                "tags": ["Pets"],
		                                "requestBody": { "content": { "application/json": {
		                                  "schema": { "$ref": "#/components/schemas/Pet", "description": "The pet to add", "examples": [ { "name": "Fido" } ] } } } },
		                                "responses": { "200": { "description": "OK", "content": { "application/json": {
		                                  "schema": { "type": "object", "properties": { "n": { "type": "integer" } }, "examples": [ { "n": 7 } ] } } } } }
		                              }
		                            }
		                          },
		                          "components": {
		                            "schemas": {
		                              "Pet": {
		                                "type": "object",
		                                "properties": {
		                                  "kind": { "const": "dog" },
		                                  "tag": { "type": ["string", "null"] },
		                                  "code": { "type": ["integer", "string"] },
		                                  "owner": { "anyOf": [ { "$ref": "#/components/schemas/Owner" }, { "type": "null" } ] },
		                                  "keeper": { "$ref": "#/components/schemas/Owner", "description": "Who keeps it" },
		                                  "nothing": { "type": "null" },
		                                  "either": { "oneOf": [ { "type": "string" }, { "$ref": "#/components/schemas/Owner" } ] },
		                                  "list": { "type": "array", "items": { "type": ["string", "null"] } }
		                                }
		                              },
		                              "Owner": { "type": "object", "description": "An owner", "properties": { "id": { "type": "integer" } } }
		                            }
		                          }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		OpenApiSchema pet = Schema(spec, "Pet");
		pet.Properties["kind"].EnumValues.Should().Equal("dog");
		pet.Properties["kind"].ToDisplayString().Should().Be("string");
		pet.Properties["tag"].ToDisplayString().Should().Be("string | null");
		pet.Properties["code"].ToDisplayString().Should().Be("string | integer", "without a numeric pattern or format it is a real union");
		pet.Properties["owner"].ToDisplayString().Should().Be("Owner | null");
		pet.Properties["keeper"].ToDisplayString().Should().Be("Owner");
		pet.Properties["keeper"].Description.Should().Be("Who keeps it");
		pet.Properties["nothing"].ToDisplayString().Should().Be("null");
		pet.Properties["either"].ToDisplayString().Should().Be("string | Owner");
		pet.Properties["list"].ToDisplayString().Should().Be("(string | null)[]");

		OpenApiEndpoint post = Endpoint(spec, "POST", "/pets");
		post.RequestBody!.Description.Should().BeEmpty();
		post.RequestBody.Schema!.Description.Should().Be("The pet to add");
		post.ExampleRequestJson.Should().Contain("\"name\": \"Fido\"");
		post.ExampleResponseJson.Should().Contain("\"n\": 7");
	}

	[Fact]
	public void OpenApi30_SchemaExampleAndAllOf_AreRead()
	{
		// A 3.0 schema's "example" went missing: Microsoft.OpenApi 2.x read it into an obsolete property only.
		const string document = """
		                        openapi: 3.0.3
		                        info: { title: Pets, version: "1" }
		                        paths:
		                          /pets:
		                            get:
		                              responses:
		                                "200":
		                                  description: OK
		                                  content:
		                                    application/json:
		                                      schema:
		                                        $ref: "#/components/schemas/Dog"
		                        components:
		                          schemas:
		                            Animal:
		                              type: object
		                              required: [name]
		                              properties:
		                                name: { type: string }
		                            Dog:
		                              example: { name: Rex, barks: true }
		                              allOf:
		                                - $ref: "#/components/schemas/Animal"
		                                - type: object
		                                  properties:
		                                    barks: { type: boolean }
		                                    owner:
		                                      description: The owner
		                                      allOf: [ { $ref: "#/components/schemas/Animal" } ]
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		OpenApiEndpoint get = Endpoint(spec, "GET", "/pets");
		get.ExampleResponseJson.Should().Contain("\"name\": \"Rex\"");
		OpenApiSchema dog = get.Responses.Single().Schema!;
		dog.ToDisplayString().Should().Be("Dog");
		dog.Properties.Keys.Should().Equal("name", "barks", "owner");
		dog.RequiredProperties.Should().Equal("name");
		dog.Properties["owner"].ToDisplayString().Should().Be("Animal");
		dog.Properties["owner"].Description.Should().Be("The owner");
	}

	[Fact]
	public void OpenApi30_NullableWithoutAType_IsKept()
	{
		// Microsoft.OpenApi 2.x dropped nullable: true from a schema with no type, the 3.0 way to make a
		// reference nullable.
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": {},
		                          "components": { "schemas": {
		                            "Pet": { "type": "object", "properties": {
		                              "owner": { "nullable": true, "allOf": [ { "$ref": "#/components/schemas/Owner" } ] },
		                              "keeper": { "$ref": "#/components/schemas/Owner" }
		                            } },
		                            "Owner": { "type": "object", "properties": { "id": { "type": "integer" } } }
		                          } }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		Property(spec, "Pet", "owner").Should().Be("Owner | null");
		Property(spec, "Pet", "keeper").Should().Be("Owner");
	}

	[Fact]
	public void Yaml_ScalarsAreTypedLikeJson()
	{
		const string document = """
		                        openapi: 3.1.0
		                        info:
		                          title: Pets
		                          version: 1.0
		                          description: 'quoted: true'
		                        x-defaults: &page
		                          type: integer
		                          format: int32
		                        paths:
		                          /pets:
		                            get:
		                              tags: [Pets]
		                              deprecated: true
		                              parameters:
		                                - name: page
		                                  in: query
		                                  schema:
		                                    <<: *page
		                                - name: flag
		                                  in: query
		                                  required: true
		                                  schema: { type: string, enum: [yes, "no", 'true', null] }
		                              responses:
		                                200:
		                                  description: OK
		                                  content:
		                                    application/json:
		                                      example: { count: 3, ratio: 0.5, ok: true, none: ~, text: "007" }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.Version.Should().Be("1.0", "a number keeps the text it was written with");
		spec.Description.Should().Be("quoted: true");
		OpenApiEndpoint get = Endpoint(spec, "GET", "/pets");
		get.Deprecated.Should().BeTrue();
		get.Tags.Should().Equal("Pets");
		get.Parameters[0].SchemaType.Should().Be("integer (int32)", "the merge key brings in the anchored keywords");
		get.Parameters[1].Required.Should().BeTrue();
		get.Parameters[1].Schema!.EnumValues.Should().Equal("yes", "no", "true", "null");
		get.Responses.Single().StatusCode.Should().Be("200");
		get.ExampleResponseJson.Should().Contain("\"count\": 3").And.Contain("\"ratio\": 0.5").And.Contain("\"ok\": true")
			.And.Contain("\"none\": null").And.Contain("\"text\": \"007\"");
	}

	[Fact]
	public void ComponentReferences_ResolveForParametersBodiesResponsesAndExamples()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": {
		                            "/pets/{id}": {
		                              "parameters": [ { "$ref": "#/components/parameters/Id" } ],
		                              "put": {
		                                "requestBody": { "$ref": "#/components/requestBodies/PetBody" },
		                                "responses": {
		                                  "200": { "$ref": "#/components/responses/PetResponse" },
		                                  "404": { "description": "Not found" }
		                                }
		                              }
		                            }
		                          },
		                          "components": {
		                            "parameters": { "Id": { "name": "id", "in": "path", "required": true, "schema": { "type": "integer", "format": "int64" } } },
		                            "requestBodies": { "PetBody": { "description": "The pet", "required": true,
		                              "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } } },
		                            "responses": { "PetResponse": { "description": "The saved pet",
		                              "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" },
		                                "examples": { "rex": { "$ref": "#/components/examples/Rex" } } } } } },
		                            "examples": { "Rex": { "value": { "name": "Rex" } } },
		                            "schemas": {
		                              "Pet": { "$ref": "#/components/schemas/Animal" },
		                              "Animal": { "type": "object", "properties": { "name": { "type": "string" }, "friend": { "$ref": "other.json#/Friend" } } }
		                            }
		                          }
		                        }
		                        """;

		OpenApiEndpoint put = Endpoint(OpenApiParser.Parse(document), "PUT", "/pets/{id}");

		put.Parameters.Single().Should().Match<OpenApiParameter>(p =>
			p.Name == "id" && p.In == "path" && p.Required && p.SchemaType == "integer (int64)");
		put.RequestBody!.Description.Should().Be("The pet");
		put.RequestBody.Required.Should().BeTrue();
		// A reference to a reference keeps the first name and the last one's content.
		put.RequestBody.Schema!.ToDisplayString().Should().Be("Pet");
		put.RequestBody.Schema.Properties.Keys.Should().Equal("name", "friend");
		// A reference outside the document keeps its name.
		put.RequestBody.Schema.Properties["friend"].ToDisplayString().Should().Be("Friend");
		put.Responses.Select(r => $"{r.StatusCode} {r.Description}").Should().Equal("200 The saved pet", "404 Not found");
		put.ExampleResponseJson.Should().Contain("\"name\": \"Rex\"");
	}

	[Fact]
	public void Swagger20_Document_IsRead()
	{
		const string document = """
		                        {
		                          "swagger": "2.0",
		                          "info": { "title": "Pets", "version": "1" },
		                          "basePath": "/v1/",
		                          "consumes": ["application/json"],
		                          "paths": {
		                            "/pets": {
		                              "post": {
		                                "tags": ["Pets"],
		                                "parameters": [
		                                  { "name": "pet", "in": "body", "required": true, "description": "The pet", "schema": { "$ref": "#/definitions/Pet" } },
		                                  { "name": "dryRun", "in": "query", "type": "boolean", "description": "Validate only" }
		                                ],
		                                "responses": { "200": { "description": "OK", "schema": { "type": "array", "items": { "$ref": "#/definitions/Pet" } },
		                                  "examples": { "application/json": [ { "name": "Rex" } ] } } }
		                              }
		                            },
		                            "/pets/{id}/photo": {
		                              "put": {
		                                "parameters": [
		                                  { "name": "id", "in": "path", "required": true, "type": "integer", "format": "int64" },
		                                  { "name": "file", "in": "formData", "required": true, "type": "file" },
		                                  { "name": "caption", "in": "formData", "type": "string" }
		                                ],
		                                "responses": { "204": { "description": "Saved" } }
		                              }
		                            }
		                          },
		                          "definitions": { "Pet": { "type": "object", "required": ["name"], "properties": { "name": { "type": "string" }, "tag": { "type": "string", "x-nullable": true } } } }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.BasePath.Should().Be("/v1");
		Property(spec, "Pet", "tag").Should().Be("string | null");
		OpenApiEndpoint post = Endpoint(spec, "POST", "/pets");
		post.RequestBody!.Should().Match<OpenApiRequestBody>(b =>
			b.Description == "The pet" && b.Required && b.ContentType == "application/json");
		post.RequestBody.Schema!.ToDisplayString().Should().Be("Pet");
		post.Parameters.Single().Should().Match<OpenApiParameter>(p =>
			p.Name == "dryRun" && p.SchemaType == "boolean" && p.Description == "Validate only" && p.Schema!.Description == "");
		post.Responses.Single().Schema!.ToDisplayString().Should().Be("Pet[]");
		post.ExampleResponseJson.Should().Contain("\"name\": \"Rex\"");

		OpenApiEndpoint put = Endpoint(spec, "PUT", "/pets/{id}/photo");
		put.Parameters.Select(p => p.Name).Should().Equal("id");
		put.RequestBody!.ContentType.Should().Be("multipart/form-data");
		put.RequestBody.Schema!.Properties["file"].ToDisplayString().Should().Be("string (binary)");
		put.RequestBody.Schema.RequiredProperties.Should().Equal("file");
	}

	[Theory]
	[InlineData("")]
	[InlineData("[1, 2]")]
	[InlineData("{ \"info\": { \"title\": \"No version\" } }")]
	[InlineData("{ \"openapi\": ")]
	[InlineData("openapi: [unclosed")]
	public void NotAnOpenApiDocument_Throws(string content)
	{
		Action parse = () => OpenApiParser.Parse(content);

		parse.Should().Throw<InvalidOperationException>().WithMessage("Failed to parse OpenAPI spec*");
	}

	[Fact]
	public void Servers_OpenApi3_HaveTheirVariableDefaultsFilledIn()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "servers": [
		                            { "url": "https://{env}.example.com:{port}/v2", "description": "The *hosted* API",
		                              "variables": { "env": { "default": "api", "enum": ["api", "sandbox"] }, "port": { "default": "443" } } },
		                            { "url": "/v2" },
		                            { "description": "No URL" }
		                          ],
		                          "paths": {}
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.Servers.Select(s => s.Url).Should().Equal("https://api.example.com:443/v2", "/v2");
		spec.Servers[0].Description.Should().Be("The *hosted* API");
		spec.Servers[0].Variables.Select(v => $"{v.Name}={v.Default}").Should().Equal("env=api", "port=443");
		spec.Servers[0].Variables[0].EnumValues.Should().Equal("api", "sandbox");
	}

	[Theory]
	[InlineData("\"host\": \"api.example.com\", \"basePath\": \"/v1/\", \"schemes\": [\"https\", \"http\"]", "https://api.example.com/v1|http://api.example.com/v1")]
	[InlineData("\"host\": \"api.example.com\"", "//api.example.com")]
	[InlineData("\"basePath\": \"/v1\"", "/v1")]
	[InlineData("\"basePath\": \"/\"", "")]
	[InlineData("\"schemes\": [\"https\"]", "")]
	public void Servers_Swagger20_ComeFromHostBasePathAndSchemes(string fields, string expected)
	{
		string document = $$"""{ "swagger": "2.0", "info": { "title": "Pets", "version": "1" }, {{fields}}, "paths": {} }""";

		OpenApiSpec spec = OpenApiParser.Parse(document);

		string.Join('|', spec.Servers.Select(s => s.Url)).Should().Be(expected);
	}

	[Fact]
	public void Servers_OnPathsAndOperations_TheOperationsReplaceThePaths()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Files", "version": "1" },
		                          "servers": [ { "url": "https://api.example.com" } ],
		                          "paths": {
		                            "/files": {
		                              "servers": [ { "url": "https://files.example.com", "description": "Files" } ],
		                              "get": { "responses": {} },
		                              "post": { "servers": [ { "url": "https://{region}.upload.example.com",
		                                "variables": { "region": { "default": "eu", "enum": ["eu", "us"] } } } ], "responses": {} },
		                              "put": { "servers": [], "responses": {} },
		                              "delete": { "servers": [ { "description": "No URL" } ], "responses": {} }
		                            },
		                            "/plain": { "get": { "responses": {} } }
		                          }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		Endpoint(spec, "GET", "/files").Servers!.Select(s => $"{s.Url} {s.Description}").Should().Equal("https://files.example.com Files");
		OpenApiServer upload = Endpoint(spec, "POST", "/files").Servers.Should().ContainSingle().Subject;
		upload.Url.Should().Be("https://eu.upload.example.com");
		upload.Variables.Single().EnumValues.Should().Equal("eu", "us");
		Endpoint(spec, "PUT", "/files").Servers!.Select(s => s.Url).Should().Equal(["https://files.example.com"], "an empty list is no list");
		Endpoint(spec, "DELETE", "/files").Servers!.Select(s => s.Url).Should().Equal(["https://files.example.com"], "a server needs a URL");
		Endpoint(spec, "GET", "/plain").Servers.Should().BeNull();
		spec.Servers.Select(s => s.Url).Should().Equal("https://api.example.com");
	}

	[Fact]
	public void Servers_Swagger20_OperationsHaveNone()
	{
		const string document = """
		                        { "swagger": "2.0", "info": { "title": "Old", "version": "1" }, "host": "old.example.com",
		                          "paths": { "/a": { "servers": [ { "url": "https://x.example.com" } ],
		                            "get": { "servers": [ { "url": "https://y.example.com" } ], "responses": {} } } } }
		                        """;

		OpenApiParser.Parse(document).Endpoints.Single().Servers.Should().BeNull();
	}

	[Fact]
	public void ExternalDocs_AreReadFromTheDocumentTagsOperationsAndSchemas()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "externalDocs": { "url": " https://example.com/guide ", "description": "The *guide*" },
		                          "tags": [
		                            { "name": "Pets", "externalDocs": { "url": "https://example.com/pets" } },
		                            { "name": "Owners", "description": "People", "externalDocs": { "description": "No URL" } }
		                          ],
		                          "paths": { "/pets": { "get": { "tags": ["Pets"], "externalDocs": { "url": "https://example.com/list", "description": "Listing" },
		                            "responses": {} } } },
		                          "components": { "schemas": {
		                            "Pet": { "type": "object", "externalDocs": { "url": "https://example.com/pet" } },
		                            "Named": { "allOf": [ { "$ref": "#/components/schemas/Pet" } ], "externalDocs": { "url": "https://example.com/named" } },
		                            "Plain": { "type": "string" }
		                          } }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.ExternalDocs!.Url.Should().Be("https://example.com/guide");
		spec.ExternalDocs.Description.Should().Be("The *guide*");
		spec.TagExternalDocs.Should().ContainKey("pets").WhoseValue.Url.Should().Be("https://example.com/pets");
		spec.TagExternalDocs.Should().NotContainKey("Owners", "an externalDocs without a URL is no link");
		spec.TagDescriptions.Should().ContainKey("Owners");
		spec.Endpoints.Single().ExternalDocs!.Description.Should().Be("Listing");
		spec.Schemas["Pet"].ExternalDocs!.Url.Should().Be("https://example.com/pet");
		spec.Schemas["Named"].ExternalDocs!.Url.Should().Be("https://example.com/named", "a wrapper keeps its own link");
		spec.Schemas["Plain"].ExternalDocs.Should().BeNull();
	}

	[Theory]
	[InlineData("aspnetcore-openapi-9.json")]
	[InlineData("aspnetcore-openapi-10.json")]
	[InlineData("swashbuckle-10.json")]
	public void GeneratedFixtures_ListNoServersAndNoSecurity(string fixture)
	{
		// The generators write servers only while serving a request; the build-time documents have none.
		OpenApiSpec spec = OpenApiParser.Parse(Fixture(fixture));

		spec.Servers.Should().BeEmpty();
		spec.SecuritySchemes.Should().BeEmpty();
		spec.Security.Should().BeEmpty();
		spec.Endpoints.Should().OnlyContain(e => e.Security == null);
	}

	[Fact]
	public void SecuritySchemes_OpenApi3_EachKindIsRead()
	{
		const string document = """
		                        {
		                          "openapi": "3.1.0",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": {},
		                          "components": {
		                            "securitySchemes": {
		                              "key": { "type": "apiKey", "name": "X-Api-Key", "in": "header", "description": "Ask **us**." },
		                              "bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" },
		                              "oauth": { "type": "oauth2", "flows": {
		                                "authorizationCode": { "authorizationUrl": "https://auth.example.com/authorize", "tokenUrl": "https://auth.example.com/token",
		                                  "refreshUrl": "https://auth.example.com/refresh", "scopes": { "read:pets": "Read pets", "write:pets": "Change pets" } },
		                                "clientCredentials": { "tokenUrl": "https://auth.example.com/token", "scopes": {} } } },
		                              "oidc": { "type": "openIdConnect", "openIdConnectUrl": "https://auth.example.com/.well-known/openid-configuration" },
		                              "shared": { "$ref": "#/components/securitySchemes/bearer" }
		                            }
		                          }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.SecuritySchemes.Keys.Should().Equal("key", "bearer", "oauth", "oidc", "shared");
		spec.SecuritySchemes["key"].Should().Match<OpenApiSecurityScheme>(s =>
			s.Type == "apiKey" && s.ParameterName == "X-Api-Key" && s.In == "header" && s.Description == "Ask **us**.");
		spec.SecuritySchemes["bearer"].Should().Match<OpenApiSecurityScheme>(s =>
			s.Type == "http" && s.Scheme == "bearer" && s.BearerFormat == "JWT");
		spec.SecuritySchemes["shared"].Should().Match<OpenApiSecurityScheme>(s => s.Name == "shared" && s.Scheme == "bearer");
		spec.SecuritySchemes["oidc"].OpenIdConnectUrl.Should().Be("https://auth.example.com/.well-known/openid-configuration");

		List<OpenApiOAuthFlow> flows = spec.SecuritySchemes["oauth"].Flows;
		flows.Select(f => f.Kind).Should().Equal("authorizationCode", "clientCredentials");
		flows[0].Should().Match<OpenApiOAuthFlow>(f => f.AuthorizationUrl == "https://auth.example.com/authorize"
		                                               && f.TokenUrl == "https://auth.example.com/token"
		                                               && f.RefreshUrl == "https://auth.example.com/refresh");
		flows[0].Scopes.Should().Equal(new Dictionary<string, string> { ["read:pets"] = "Read pets", ["write:pets"] = "Change pets" });
		flows[1].Scopes.Should().BeEmpty();
	}

	[Fact]
	public void SecurityDefinitions_Swagger20_AreRead()
	{
		const string document = """
		                        {
		                          "swagger": "2.0",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": {},
		                          "securityDefinitions": {
		                            "basic": { "type": "basic" },
		                            "key": { "type": "apiKey", "name": "api_key", "in": "query" },
		                            "app": { "type": "oauth2", "flow": "application", "tokenUrl": "https://auth.example.com/token", "scopes": { "admin": "Everything" } },
		                            "code": { "type": "oauth2", "flow": "accessCode", "authorizationUrl": "https://auth.example.com/authorize", "tokenUrl": "https://auth.example.com/token" }
		                          },
		                          "security": [ { "basic": [] } ]
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.SecuritySchemes["basic"].Should().Match<OpenApiSecurityScheme>(s => s.Type == "http" && s.Scheme == "basic");
		spec.SecuritySchemes["key"].Should().Match<OpenApiSecurityScheme>(s => s.ParameterName == "api_key" && s.In == "query");
		OpenApiOAuthFlow app = spec.SecuritySchemes["app"].Flows.Should().ContainSingle().Which;
		app.Kind.Should().Be("clientCredentials");
		app.TokenUrl.Should().Be("https://auth.example.com/token");
		app.Scopes.Should().ContainKey("admin");
		spec.SecuritySchemes["code"].Flows.Single().Should().Match<OpenApiOAuthFlow>(f =>
			f.Kind == "authorizationCode" && f.AuthorizationUrl == "https://auth.example.com/authorize");
		spec.Security.Single().Schemes.Keys.Should().Equal("basic");
	}

	[Fact]
	public void Security_OperationListReplacesTheDocumentList()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "security": [ { "bearer": [] } ],
		                          "paths": {
		                            "/inherits": { "get": { "responses": {} } },
		                            "/public": { "get": { "security": [], "responses": {} } },
		                            "/either": { "get": { "security": [ { "oauth": ["read", "write"], "key": [] }, { "bearer": [] } ], "responses": {} } },
		                            "/optional": { "get": { "security": [ {}, { "key": [] } ], "responses": {} } }
		                          }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.Security.Single().Schemes.Should().ContainKey("bearer").WhoseValue.Should().BeEmpty();
		Endpoint(spec, "GET", "/inherits").Security.Should().BeNull();
		Endpoint(spec, "GET", "/public").Security.Should().NotBeNull().And.BeEmpty();

		List<OpenApiSecurityRequirement> either = Endpoint(spec, "GET", "/either").Security!;
		either.Should().HaveCount(2);
		either[0].Schemes.Keys.Should().Equal("oauth", "key");
		either[0].Schemes["oauth"].Should().Equal("read", "write");
		either[1].Schemes.Keys.Should().Equal("bearer");

		Endpoint(spec, "GET", "/optional").Security!.Select(r => r.Schemes.Count).Should().Equal(0, 1);
	}

	[Fact]
	public void TagDescriptions_ComeFromTheTopLevelTagList()
	{
		const string document = """
		                        openapi: 3.0.3
		                        info: { title: Pets, version: "1" }
		                        tags:
		                          - name: Pets
		                            description: Everything about *pets*.
		                          - name: Stores
		                        paths: {}
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.TagDescriptions.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("Pets", "Everything about *pets*."));
		spec.TagDescriptions["pets"].Should().Be("Everything about *pets*.", "tags are matched without regard to case");
	}

	[Fact]
	public void SelfReferencingSchema_StopsAtTheCycle()
	{
		OpenApiSpec spec = OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json"));

		OpenApiSchema parent = Schema(spec, "Category").Properties["parent"];
		parent.Properties["children"].Items!.RefName.Should().Be("Category");
		parent.Properties["children"].Items!.Properties.Should().BeEmpty();
	}

	[Theory]
	[InlineData("\uFEFF{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"bom\",\"version\":\"1\"},\"paths\":{}}", true)]
	[InlineData("\uFEFFopenapi: 3.0.0\r\ninfo:\r\n  title: bom\r\n  version: '1'\r\npaths: {}\r\n", false)]
	[InlineData("// Generated\n{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"bom\",\"version\":\"1\"},\"paths\":{}}", true)]
	[InlineData("/* Generated */ {\"openapi\":\"3.0.0\",\"info\":{\"title\":\"bom\",\"version\":\"1\"},\"paths\":{}}", true)]
	[InlineData("{openapi: 3.0.0, info: {title: bom, version: \"1\"}, paths: {}}", false)]
	public void Document_WithABomACommentOrYamlFlowStyle_IsRead(string content, bool json)
	{
		OpenApiSpec spec = OpenApiParser.Parse(content, out bool isJson);

		spec.Title.Should().Be("bom");
		isJson.Should().Be(json);
	}

	[Fact]
	public void JsonKeyWrittenTwice_IsAParseError()
	{
		// It threw an ArgumentException from somewhere in the middle of mapping, which callers didn't expect.
		Action parse = () => OpenApiParser.Parse("{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"a\",\"title\":\"b\"},\"paths\":{}}");

		parse.Should().Throw<InvalidOperationException>()
			.WithMessage("Failed to parse OpenAPI spec: an object at '$.info' has the same key more than once*");
	}

	[Fact]
	public void YamlKeyWrittenTwice_IsAParseError()
	{
		Action parse = () => OpenApiParser.Parse("openapi: 3.0.0\nopenapi: 3.1.0\npaths: {}\n");

		parse.Should().Throw<InvalidOperationException>().WithMessage("Failed to parse OpenAPI spec*Duplicate key*");
	}

	[Fact]
	public void BrokenJson_ReportsTheJsonError_NotTheYamlOne()
	{
		Action parse = () => OpenApiParser.Parse("{ \"openapi\": \"3.0.0\", \"paths\": { ] }");

		parse.Should().Throw<InvalidOperationException>().Which.InnerException.Should().BeAssignableTo<System.Text.Json.JsonException>();
	}

	[Fact]
	public void ParameterDescribedWithContent_UsesTheMediaTypesSchema()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": { "/pets": { "get": {
		                            "parameters": [ { "name": "filter", "in": "query",
		                              "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Filter" } } } } ],
		                            "responses": {} } } },
		                          "components": { "schemas": { "Filter": { "type": "object", "properties": { "kind": { "type": "string" } } } } }
		                        }
		                        """;

		OpenApiParameter filter = Endpoint(OpenApiParser.Parse(document), "GET", "/pets").Parameters.Single();

		filter.SchemaType.Should().Be("Filter");
		filter.Schema!.Properties.Keys.Should().Equal("kind");
	}

	[Fact]
	public void UnresolvedReferences_KeepTheirNameAndAreListedInAWarning()
	{
		const string document = """
		                        {
		                          "openapi": "3.0.3",
		                          "info": { "title": "Pets", "version": "1" },
		                          "paths": { "/pets": { "get": { "parameters": [ { "$ref": "#/components/parameters/Gone" } ], "responses": {} } } },
		                          "components": { "schemas": { "Pet": { "type": "object", "properties": {
		                            "owner": { "$ref": "people.yaml#/Owner" },
		                            "tag": { "$ref": "#/components/schemas/Missing" },
		                            "again": { "$ref": "#/components/schemas/Missing" },
		                            "self": { "$ref": "#/components/schemas/Pet" } } } } }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		Property(spec, "Pet", "owner").Should().Be("Owner");
		Property(spec, "Pet", "tag").Should().Be("Missing");
		spec.Warnings.Should().ContainSingle().Which.Should().Be(
			"3 $ref value(s) point outside the document or at nothing in it, so they show only their name: " +
			"'people.yaml#/Owner', '#/components/schemas/Missing', '#/components/parameters/Gone'.");
	}

	[Fact]
	public void DocumentWithEverythingResolved_HasNoWarnings()
	{
		OpenApiParser.Parse(Fixture("aspnetcore-openapi-10.json")).Warnings.Should().BeEmpty();
		OpenApiParser.Parse(Fixture("swashbuckle-10.json")).Warnings.Should().BeEmpty();
	}

	[Fact]
	public void Webhooks_OpenApi31_AreRead()
	{
		const string document = """
		                        {
		                          "openapi": "3.1.0",
		                          "info": { "title": "Pets", "version": "1" },
		                          "webhooks": {
		                            "newPet": { "post": { "summary": "A pet was added", "tags": ["Pets"],
		                              "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } },
		                              "responses": { "200": { "description": "Received" } } } },
		                            "petGone": { "$ref": "#/components/pathItems/Gone" }
		                          },
		                          "components": {
		                            "pathItems": { "Gone": { "delete": { "summary": "A pet was removed", "responses": {} } } },
		                            "schemas": { "Pet": { "type": "object", "properties": { "name": { "type": "string" } } } }
		                          }
		                        }
		                        """;

		OpenApiSpec spec = OpenApiParser.Parse(document);

		spec.Endpoints.Should().BeEmpty("webhooks are not endpoints of the API");
		spec.Tags.Should().BeEmpty();
		spec.Webhooks.Select(w => $"{w.Method} {w.Path} {w.Summary}").Should().Equal("POST newPet A pet was added", "DELETE petGone A pet was removed");
		spec.Webhooks[0].RequestBody!.Schema!.ToDisplayString().Should().Be("Pet");
	}
}
