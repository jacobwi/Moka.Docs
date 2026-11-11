using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Engine.Phases;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.Plugins.OpenApi;

/// <summary>
///     The pages the OpenAPI plugin generates: Markdown descriptions, endpoint anchors, servers,
///     authentication, the models page and the published source document.
/// </summary>
public sealed class OpenApiPagesTests : IDisposable
{
	private const string _petsDocument = """
	                                     {
	                                       "openapi": "3.0.3",
	                                       "info": {
	                                         "title": "Pets",
	                                         "version": "2.1",
	                                         "description": "The **Pets** API. See [the guide](https://example.com/guide).\n\n<script>alert(1)</script>"
	                                       },
	                                       "servers": [
	                                         { "url": "https://{env}.example.com/v2", "description": "Hosted *API*",
	                                           "variables": { "env": { "default": "api", "enum": ["api", "sandbox"] } } },
	                                         { "url": "/v2" }
	                                       ],
	                                       "tags": [ { "name": "Pets", "description": "Everything about *pets*." } ],
	                                       "security": [ { "bearer": [] } ],
	                                       "paths": {
	                                         "/pets": {
	                                           "get": {
	                                             "tags": ["Pets"], "operationId": "listPets", "summary": "List *all* pets",
	                                             "description": "Returns pets.\n\n- sorted by `name`\n- paged",
	                                             "parameters": [ { "name": "limit", "in": "query", "description": "At most **this** many.", "schema": { "type": "integer" } } ],
	                                             "responses": { "200": { "description": "A page of [pets](/guide/pets).",
	                                               "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Pet" } } } } } }
	                                           },
	                                           "post": {
	                                             "tags": ["Pets"], "summary": "Add a pet",
	                                             "security": [ { "oauth": ["write:pets", "read:pets"], "key": [] }, { "bearer": [] } ],
	                                             "requestBody": { "description": "The pet to *add*.",
	                                               "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } },
	                                             "responses": { "201": { "description": "Created" } }
	                                           }
	                                         },
	                                         "/pets/{id}": {
	                                           "get": { "tags": ["Pets"], "operationId": "listPets", "security": [], "responses": { "200": { "description": "OK" } } },
	                                           "delete": { "tags": ["Pets"], "operationId": "Pets_Delete", "security": [ {}, { "key": [] } ],
	                                             "responses": { "204": { "description": "Gone" } } }
	                                         },
	                                         "/health": { "get": { "responses": { "200": { "description": "OK" } } } }
	                                       },
	                                       "components": {
	                                         "securitySchemes": {
	                                           "bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT", "description": "A token from **the login page**." },
	                                           "key": { "type": "apiKey", "name": "X-Api-Key", "in": "header" },
	                                           "oauth": { "type": "oauth2", "flows": { "authorizationCode": {
	                                             "authorizationUrl": "https://auth.example.com/authorize", "tokenUrl": "javascript:alert(3)",
	                                             "scopes": { "read:pets": "Read pets", "write:pets": "Change pets" } } } },
	                                           "oidc": { "type": "openIdConnect", "openIdConnectUrl": "https://auth.example.com/.well-known/openid-configuration" }
	                                         },
	                                         "schemas": {
	                                           "Pet": { "type": "object", "description": "A pet. <b>bold</b>", "required": ["name"],
	                                             "properties": {
	                                               "name": { "type": "string", "description": "The *name* of the pet." },
	                                               "kind": { "$ref": "#/components/schemas/Kind" },
	                                               "owner": { "$ref": "#/components/schemas/Owner" }
	                                             } },
	                                           "Kind": { "type": "string", "enum": ["dog", "cat"],
	                                             "description": "What it is. [click](javascript:alert(1)) <javascript:alert(2)> [ok](mailto:pets@example.com)" },
	                                           "Owner": { "type": "object", "properties": { "id": { "type": "integer" } } }
	                                         }
	                                       }
	                                     }
	                                     """;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-openapi-pages-" + Guid.NewGuid().ToString("N"));

	public OpenApiPagesTests() => Directory.CreateDirectory(Path.Combine(_root, "docs"));

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private BuildContext CreateBuild(params ApiType[] types)
	{
		var context = new BuildContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "T" } },
			FileSystem = new MockFileSystem(),
			RootDirectory = _root,
			OutputDirectory = Path.Combine(_root, "_site")
		};

		if (types.Length > 0)
		{
			context.ApiModel = new ApiReference { Namespaces = [new ApiNamespace { Name = "Pets.Models", Types = [.. types] }] };
			foreach (ApiType type in types)
			{
				context.Pages.Add(new DocPage
				{
					FrontMatter = new FrontMatter { Title = type.Name },
					Content = PageContent.Empty,
					Route = $"/api/pets/models/{type.Name}".ToLowerInvariant(),
					Origin = PageOrigin.ApiGenerated
				});
			}
		}

		return context;
	}

	private static ApiType Type(string name) =>
		new() { Name = name, FullName = $"Pets.Models.{name}", Namespace = "Pets.Models", Kind = ApiTypeKind.Class };

	private static async Task<BuildContext> RunAsync(BuildContext context, params Dictionary<string, object>[] entries)
	{
		SiteConfig config = context.Config with
		{
			Plugins = [.. entries.Select(options => new PluginDeclaration { Name = "openapi", Options = options })]
		};
		ServiceProvider provider = new ServiceCollection()
			.AddSingleton<IMokaPlugin, OpenApiPlugin>()
			.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
			.BuildServiceProvider();
		var host = new PluginHost(config, provider, NullLogger<PluginHost>.Instance);

		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);
		await host.ExecuteAllAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	private static Dictionary<string, object> Entry(string document, string routePrefix = "/rest-api") => new()
	{
		[OpenApiPlugin.DocumentOption] = document,
		[OpenApiPlugin.RoutePrefixOption] = routePrefix
	};

	private async Task<BuildContext> RunPetsAsync(params ApiType[] types) => await RunAsync(CreateBuild(types), Entry(_petsDocument));

	private static DocPage Page(BuildContext context, string route) => context.Pages.Single(p => p.Route == route);

	private static string Html(BuildContext context, string route) => Page(context, route).Content.Html;

	#region Markdown

	[Fact]
	public async Task Descriptions_AreRenderedAsMarkdown()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api").Should().Contain("<strong>Pets</strong> API. See <a href=\"https://example.com/guide\">the guide</a>.");
		string pets = Html(context, "/rest-api/pets");
		pets.Should().Contain("<div class=\"openapi-markdown openapi-tag-description\"><p>Everything about <em>pets</em>.</p></div>");
		pets.Should().Contain("<ul>").And.Contain("<li>sorted by <code>name</code></li>");
		pets.Should().Contain("<div class=\"openapi-markdown\"><p>The pet to <em>add</em>.</p></div>");
		Html(context, "/rest-api/models").Should().Contain("<td>The <em>name</em> of the pet.</td>");
	}

	[Fact]
	public async Task TableCells_DropTheParagraphOfASingleParagraph()
	{
		BuildContext context = await RunPetsAsync();

		string pets = Html(context, "/rest-api/pets");
		pets.Should().Contain("<td>At most <strong>this</strong> many.</td>");
		pets.Should().Contain("<td>A page of <a href=\"/guide/pets\">pets</a>.</td>");
		pets.Should().Contain("<td>Created</td>");
	}

	[Fact]
	public async Task RawHtml_IsShownAsText()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api").Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;").And.NotContain("<script>");
		Html(context, "/rest-api/models").Should().Contain("A pet. &lt;b&gt;bold&lt;/b&gt;").And.NotContain("<b>bold</b>");
	}

	[Fact]
	public async Task Links_WithAnUnsafeScheme_KeepOnlyTheirText()
	{
		BuildContext context = await RunPetsAsync();

		string models = Html(context, "/rest-api/models");
		models.Should().NotContain("href=\"javascript");
		models.Should().Contain("What it is. click javascript:alert(2) <a href=\"mailto:pets@example.com\">ok</a>");
		Html(context, "/rest-api").Should().NotContain("href=\"javascript").And.Contain("<code>javascript:alert(3)</code>");
	}

	[Fact]
	public async Task Summaries_StayPlainText()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api/pets").Should().Contain("<p class=\"openapi-summary\">List *all* pets</p>");
		Html(context, "/rest-api").Should().Contain("<td>List *all* pets</td>");
	}

	[Fact]
	public async Task PageDescriptionAndSearchText_AreTheDescriptionWithoutMarkup()
	{
		BuildContext context = await RunPetsAsync();

		DocPage index = Page(context, "/rest-api");
		index.FrontMatter.Description.Should().StartWith("The Pets API. See the guide.");
		index.Content.PlainText.Should().NotContain("**").And.NotContain("](");
	}

	#endregion

	#region Anchors

	[Fact]
	public async Task Endpoints_GetAnAnchorFromTheirOperationIdOrMethodAndPath_UniqueOnThePage()
	{
		BuildContext context = await RunPetsAsync();

		string pets = Html(context, "/rest-api/pets");
		pets.Should().Contain("<h2 class=\"openapi-endpoint-header\" id=\"list-pets\">");
		pets.Should().Contain("<h2 class=\"openapi-endpoint-header\" id=\"post-pets\">");
		pets.Should().Contain("<h2 class=\"openapi-endpoint-header\" id=\"list-pets-2\">", "a repeated operationId still gets an id of its own");
		pets.Should().Contain("<h2 class=\"openapi-endpoint-header\" id=\"pets-delete\">");
		Html(context, "/rest-api/other").Should().Contain("<h2 class=\"openapi-endpoint-header\" id=\"get-health\">");
	}

	[Fact]
	public async Task Anchors_AreTheSameOnEveryBuild()
	{
		string first = Html(await RunPetsAsync(), "/rest-api/pets");
		string second = Html(await RunPetsAsync(), "/rest-api/pets");

		second.Should().Be(first);
	}

	[Fact]
	public async Task TagPage_ListsItsEndpointsInTheTableOfContents()
	{
		BuildContext context = await RunPetsAsync();

		Page(context, "/rest-api/pets").TableOfContents.Entries.Select(e => $"{e.Level} {e.Text} #{e.Id}").Should().Equal(
			"2 GET /pets #list-pets",
			"2 POST /pets #post-pets",
			"2 GET /pets/{id} #list-pets-2",
			"2 DELETE /pets/{id} #pets-delete");
	}

	[Fact]
	public async Task IndexRows_LinkToTheEndpointOnItsTagPage()
	{
		BuildContext context = await RunPetsAsync();

		string index = Html(context, "/rest-api");
		index.Should().Contain("<a href=\"/rest-api/pets#list-pets\"><code>/pets</code></a>");
		index.Should().Contain("<a href=\"/rest-api/pets#list-pets-2\"><code>/pets/{id}</code></a>");
		index.Should().Contain("<a href=\"/rest-api/other#get-health\"><code>/health</code></a>");
	}

	[Fact]
	public async Task SearchIndex_HasAnEntryPerEndpoint()
	{
		BuildContext context = await RunPetsAsync();

		await new SearchIndexPhase(NullLogger<SearchIndexPhase>.Instance).ExecuteAsync(context, TestContext.Current.CancellationToken);

		context.SearchIndex!.Entries.Where(e => e.Route.StartsWith("/rest-api/pets#", StringComparison.Ordinal))
			.Select(e => $"{e.Section} {e.Route}")
			.Should().Equal(
				"GET /pets /rest-api/pets#list-pets",
				"POST /pets /rest-api/pets#post-pets",
				"GET /pets/{id} /rest-api/pets#list-pets-2",
				"DELETE /pets/{id} /rest-api/pets#pets-delete");
	}

	#endregion

	#region Servers

	[Fact]
	public async Task Servers_AreListedOnTheIndexPage()
	{
		BuildContext context = await RunPetsAsync();

		string index = Html(context, "/rest-api");
		index.Should().Contain("<h2 id=\"servers\">Servers</h2>");
		index.Should().Contain("<td><code>https://api.example.com/v2</code></td>");
		index.Should().Contain("Hosted <em>API</em><span class=\"openapi-values\"><code>{env}</code>: <code>api</code>, <code>sandbox</code></span>");
		index.Should().Contain("<td><code>/v2</code></td>");
		Page(context, "/rest-api").TableOfContents.Entries.Should().Contain(e => e.Id == "servers" && e.Text == "Servers");
	}

	[Fact]
	public async Task Servers_WhenTheDocumentListsNone_AreNotShown()
	{
		BuildContext context = await RunAsync(CreateBuild(), Entry(OpenApiParserTests.Fixture("aspnetcore-openapi-10.json")));

		Html(context, "/rest-api").Should().NotContain("id=\"servers\"");
		Page(context, "/rest-api").TableOfContents.Entries.Should().NotContain(e => e.Id == "servers");
	}

	[Fact]
	public async Task Servers_Swagger20_AreListed()
	{
		const string document = """
		                        { "swagger": "2.0", "info": { "title": "Old", "version": "1" }, "host": "old.example.com", "basePath": "/v1",
		                          "schemes": ["https"], "paths": { "/a": { "get": { "responses": { "200": { "description": "OK" } } } } } }
		                        """;

		BuildContext context = await RunAsync(CreateBuild(), Entry(document));

		Html(context, "/rest-api").Should().Contain("<td><code>https://old.example.com/v1</code></td>");
	}

	#endregion

	#region Authentication

	[Fact]
	public async Task AuthenticationSection_DescribesEachScheme()
	{
		BuildContext context = await RunPetsAsync();

		string index = Html(context, "/rest-api");
		index.Should().Contain("<h2 id=\"authentication\">Authentication</h2>");
		index.Should().Contain("<p>Unless an endpoint says otherwise, it needs <a href=\"/rest-api#auth-bearer\"><code>bearer</code></a>.</p>");

		index.Should().Contain("<h3 id=\"auth-bearer\">bearer</h3>");
		index.Should().Contain("<p>A token from <strong>the login page</strong>.</p>");
		index.Should().Contain("<tr><th>Type</th><td>HTTP</td></tr>").And.Contain("<tr><th>Scheme</th><td><code>bearer</code></td></tr>")
			.And.Contain("<tr><th>Bearer format</th><td>JWT</td></tr>");

		index.Should().Contain("<h3 id=\"auth-key\">key</h3>");
		index.Should().Contain("<tr><th>Type</th><td>API key</td></tr>").And.Contain("<tr><th>Name</th><td><code>X-Api-Key</code></td></tr>")
			.And.Contain("<tr><th>In</th><td>header</td></tr>");

		index.Should().Contain("<h3 id=\"auth-oauth\">oauth</h3>");
		index.Should().Contain("<tr><th>Type</th><td>OAuth 2.0</td></tr>").And.Contain("<tr><th>Flow</th><td>Authorization code</td></tr>")
			.And.Contain("<tr><th>Authorization URL</th><td><a href=\"https://auth.example.com/authorize\"><code>https://auth.example.com/authorize</code></a></td></tr>")
			.And.Contain("<tr><th>Scopes</th><td><code>read:pets</code>: Read pets<br><code>write:pets</code>: Change pets</td></tr>");

		index.Should().Contain("<h3 id=\"auth-oidc\">oidc</h3>");
		index.Should().Contain("<tr><th>Type</th><td>OpenID Connect</td></tr>").And.Contain(
			"<tr><th>Discovery URL</th><td><a href=\"https://auth.example.com/.well-known/openid-configuration\">");

		TocEntry authentication = Page(context, "/rest-api").TableOfContents.Entries.Single(e => e.Id == "authentication");
		authentication.Children.Select(c => $"{c.Text} #{c.Id}").Should().Equal("bearer #auth-bearer", "key #auth-key", "oauth #auth-oauth", "oidc #auth-oidc");
	}

	[Fact]
	public async Task EndpointBadge_ShowsTheDocumentDefault_WhenTheOperationHasNoList()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api/other").Should().Contain(
			"<p class=\"openapi-auth\"><a class=\"openapi-auth-label\" href=\"/rest-api#authentication\">Auth</a> " +
			"<a href=\"/rest-api#auth-bearer\"><code>bearer</code></a></p>");
	}

	[Fact]
	public async Task EndpointBadge_ListsAlternativesAndTheSchemesEachCombines()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api/pets").Should().Contain(
			"<a href=\"/rest-api#auth-oauth\"><code>oauth</code></a> <span class=\"openapi-scopes\">(write:pets, read:pets)</span>" +
			" + <a href=\"/rest-api#auth-key\"><code>key</code></a>" +
			" <span class=\"openapi-auth-or\">or</span> <a href=\"/rest-api#auth-bearer\"><code>bearer</code></a></p>");
	}

	[Fact]
	public async Task EndpointBadge_WithAnEmptyList_SaysNoAuthentication()
	{
		BuildContext context = await RunPetsAsync();

		string pets = Html(context, "/rest-api/pets");
		string getById = pets[pets.IndexOf("id=\"list-pets-2\"", StringComparison.Ordinal)..pets.IndexOf("id=\"pets-delete\"", StringComparison.Ordinal)];
		getById.Should().Contain("<a class=\"openapi-auth-label\" href=\"/rest-api#authentication\">No authentication</a>");
		getById.Should().NotContain("auth-bearer");
	}

	[Fact]
	public async Task EndpointBadge_WithAnEmptyAlternative_IsOptional()
	{
		BuildContext context = await RunPetsAsync();

		Html(context, "/rest-api/pets").Should().Contain(
			"<a class=\"openapi-auth-label\" href=\"/rest-api#authentication\">Auth (optional)</a> <a href=\"/rest-api#auth-key\"><code>key</code></a></p>");
	}

	[Fact]
	public async Task DocumentWithoutSecurity_HasNoAuthenticationSectionOrBadges()
	{
		BuildContext context = await RunAsync(CreateBuild(), Entry(OpenApiParserTests.Fixture("aspnetcore-openapi-10.json")));

		Html(context, "/rest-api").Should().NotContain("id=\"authentication\"");
		Html(context, "/rest-api/products").Should().NotContain("class=\"openapi-auth\"");
	}

	#endregion

	#region Models

	[Fact]
	public async Task ModelsPage_ListsEachSchemaWithAnAnchor()
	{
		BuildContext context = await RunPetsAsync();

		string models = Html(context, "/rest-api/models");
		models.Should().Contain("<h2 id=\"kind\">Kind</h2>").And.Contain("<h2 id=\"owner\">Owner</h2>").And.Contain("<h2 id=\"pet\">Pet</h2>");
		models.Should().Contain("<p>Type: <code>string</code></p>").And.Contain("<p>Values: <code>dog</code>, <code>cat</code></p>");
		// The property table, with a schema that has no C# type linked to its anchor on this page.
		models.Should().Contain("<td><code>name</code></td>").And.Contain("<span class=\"openapi-required\">required</span>");
		models.Should().Contain("<td><code><a class=\"openapi-type-link\" href=\"/rest-api/models#owner\">Owner</a></code></td>");
		Page(context, "/rest-api/models").TableOfContents.Entries.Select(e => $"{e.Text} #{e.Id}")
			.Should().Equal("Kind #kind", "Owner #owner", "Pet #pet");
	}

	[Fact]
	public async Task ModelsPage_LinksToTheMatchingCSharpType()
	{
		BuildContext context = await RunPetsAsync(Type("Pet"));

		string models = Html(context, "/rest-api/models");
		models.Should().Contain("<p class=\"openapi-model-type\">C# type: <a class=\"openapi-type-link\" href=\"/api/pets/models/pet\">Pet</a></p>");
		models.Should().NotContain("href=\"/api/pets/models/owner\"");
		// Elsewhere the name keeps linking to the C# page, and the rest to the models page.
		string pets = Html(context, "/rest-api/pets");
		pets.Should().Contain("<a class=\"openapi-type-link\" href=\"/api/pets/models/pet\">Pet</a>[]");
		models.Should().Contain("<a class=\"openapi-type-link\" href=\"/rest-api/models#kind\">Kind</a>");
	}

	[Fact]
	public async Task ModelsPage_ComesAfterTheTagPages_AndTheIndexLinksToIt()
	{
		BuildContext context = await RunPetsAsync();

		Page(context, "/rest-api/models").FrontMatter.Order.Should().BeGreaterThan(
			context.Pages.Where(p => p.Route is "/rest-api/pets" or "/rest-api/other").Max(p => p.FrontMatter.Order));
		Html(context, "/rest-api").Should().Contain("<a href=\"/rest-api/models\">Models</a>: the document's 3 schemas.");
	}

	[Fact]
	public async Task ModelsPage_MovesAsideForATagCalledModels()
	{
		const string document = """
		                        { "openapi": "3.0.3", "info": { "title": "M", "version": "1" },
		                          "paths": { "/m": { "get": { "tags": ["Models"], "responses": { "200": { "description": "OK" } } } } },
		                          "components": { "schemas": { "Thing": { "type": "object" } } } }
		                        """;

		BuildContext context = await RunAsync(CreateBuild(), Entry(document));

		context.Pages.Select(p => p.Route).Should().Contain(["/rest-api/models", "/rest-api/models-2"]);
		Html(context, "/rest-api/models-2").Should().Contain("<h2 id=\"thing\">Thing</h2>");
	}

	[Fact]
	public async Task DocumentWithoutSchemas_HasNoModelsPage()
	{
		const string document = """
		                        { "openapi": "3.0.3", "info": { "title": "M", "version": "1" },
		                          "paths": { "/m": { "get": { "responses": { "200": { "description": "OK" } } } } } }
		                        """;

		BuildContext context = await RunAsync(CreateBuild(), Entry(document));

		context.Pages.Select(p => p.Route).Should().NotContain("/rest-api/models");
		Html(context, "/rest-api").Should().NotContain("Models</a>");
	}

	#endregion

	#region Source document

	[Fact]
	public async Task SourceDocument_IsStagedWithItsOriginalText_AndLinked()
	{
		BuildContext context = await RunPetsAsync();

		context.DeferredOutputFiles.Should().ContainKey("rest-api/openapi.json")
			.WhoseValue.Should().Equal(Encoding.UTF8.GetBytes(_petsDocument));
		Html(context, "/rest-api").Should().Contain(
			"<a href=\"/rest-api/openapi.json\" download>Download the OpenAPI document</a> (<code>openapi.json</code>)");
	}

	[Fact]
	public async Task SourceDocument_InYaml_IsPublishedAsYaml()
	{
		const string yaml = "# A comment, kept as written\nopenapi: 3.0.3\ninfo: { title: Y, version: '1' }\npaths: {}\n";

		BuildContext context = await RunAsync(CreateBuild(), Entry(yaml));

		context.DeferredOutputFiles.Keys.Should().Equal("rest-api/openapi.yaml");
		Encoding.UTF8.GetString(context.DeferredOutputFiles["rest-api/openapi.yaml"]).Should().Be(yaml);
		Html(context, "/rest-api").Should().Contain("href=\"/rest-api/openapi.yaml\" download");
	}

	[Fact]
	public async Task SourceDocument_OfEachEntry_GoesUnderItsOwnPrefix()
	{
		string admin = _petsDocument.Replace("\"title\": \"Pets\"", "\"title\": \"Admin\"", StringComparison.Ordinal);

		BuildContext context = await RunAsync(CreateBuild(), Entry(_petsDocument, "/public-api"), Entry(admin, "admin-api"));

		Encoding.UTF8.GetString(context.DeferredOutputFiles["public-api/openapi.json"]).Should().Be(_petsDocument);
		Encoding.UTF8.GetString(context.DeferredOutputFiles["admin-api/openapi.json"]).Should().Be(admin);
		Html(context, "/admin-api").Should().Contain("href=\"/admin-api/openapi.json\"");
	}

	[Fact]
	public async Task SourceDocument_AtTheSiteRoot_IsPublishedAtTheRoot()
	{
		BuildContext context = await RunAsync(CreateBuild(), Entry(_petsDocument, "/"));

		context.DeferredOutputFiles.Should().ContainKey("openapi.json");
		Html(context, "").Should().Contain("href=\"/openapi.json\" download").And.Contain("href=\"/pets#list-pets\"");
		Html(context, "/pets").Should().Contain("href=\"/#authentication\"");
	}

	[Fact]
	public async Task Cli_Build_WritesTheSourceDocument_ButADryRunDoesNot()
	{
		const string yaml = "openapi: 3.0.3   # spacing and comments as written\ninfo:\n  title: Disk\n  version: '1'\npaths: {}\n";
		File.WriteAllText(Path.Combine(_root, "openapi.yaml"), yaml);
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\nHome\n");
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Disk" },
			Content = new ContentConfig { Docs = "./docs" },
			Plugins = [new PluginDeclaration { Name = "openapi", Options = { ["spec"] = "openapi.yaml", ["routePrefix"] = "/rest-api" } }],
			Build = new BuildConfig { Cache = false, Output = "./_site" }
		};
		string written = Path.Combine(_root, "_site", "rest-api", "openapi.yaml");

		DryRunOutcome dryRun = await DryRunBuild.RunAsync(config, _root, false, false, TestContext.Current.CancellationToken);

		dryRun.Failure.Should().BeNull();
		dryRun.Context.DeferredOutputFiles.Should().ContainKey("rest-api/openapi.yaml");
		File.Exists(written).Should().BeFalse();

		await using (ServiceProvider provider = BuildCommand.BuildServices(config, false))
		{
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			await BuildCommand.InitializePluginsAsync(provider, pipeline);
			await pipeline.ExecuteAsync(new BuildContext
			{
				Config = config,
				FileSystem = new System.IO.Abstractions.FileSystem(),
				RootDirectory = _root,
				OutputDirectory = Path.Combine(_root, "_site"),
				UseCache = false
			}, TestContext.Current.CancellationToken);
		}

		File.ReadAllText(written).Should().Be(yaml);
		File.ReadAllText(Path.Combine(_root, "_site", "rest-api", "index.html")).Should().Contain("href=\"/rest-api/openapi.yaml\" download");
	}

	#endregion
}
