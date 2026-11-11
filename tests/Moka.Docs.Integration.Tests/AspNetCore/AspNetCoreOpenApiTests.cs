using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.OpenApi;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     REST API pages in the ASP.NET Core host: from a spec file in a <c>Plugins</c> entry, and from the
///     application's own document, read from real Microsoft.AspNetCore.OpenApi and Swashbuckle providers
///     in a running <see cref="WebApplication" />.
/// </summary>
public sealed class AspNetCoreOpenApiTests : IAsyncDisposable
{
	private const string _spec = """
	                             {
	                               "openapi": "3.0.3",
	                               "info": { "title": "Shop", "version": "1.0.0" },
	                               "paths": {
	                                 "/products": {
	                                   "get": { "tags": ["Products"], "summary": "List products", "responses": { "200": { "description": "OK" } } }
	                                 }
	                               }
	                             }
	                             """;

	private readonly List<WebApplication> _apps = [];
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-aspnetcore-openapi-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreOpenApiTests() => Directory.CreateDirectory(_root);

	public async ValueTask DisposeAsync()
	{
		foreach (WebApplication app in _apps)
		{
			await app.StopAsync();
			await app.DisposeAsync();
		}

		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	#region Route prefix and navigation

	[Fact]
	public async Task OpenApiEntry_WithoutARoutePrefix_GoesToRestApi_BesideTheApiReference()
	{
		// The plugin's default prefix, /api, is the API reference's route, which this host always generates.
		// The two index pages collided and only one of them was served.
		File.WriteAllText(Path.Combine(_root, "openapi.json"), _spec);

		InMemorySite site = await BuildAsync(options =>
			options.Plugins = [new PluginEntry { Id = "openapi", Options = { ["spec"] = "openapi.json" } }]);

		site.Diagnostics.Should().NotContain(d => d.Message.Contains("share the route"));
		site.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
		Html(site, "rest-api/products/index.html").Should().Contain("List products");
		Html(site, "rest-api/index.html").Should().Contain("href=\"/docs/rest-api/products\"");
		Html(site, "api/index.html").Should().Contain("Fixture.Shapes");
	}

	[Fact]
	public void OpenApiEntry_KeepsARoutePrefixAndLabelOfItsOwn()
	{
		var options = new MokaDocsOptions
		{
			OpenApiRoutePrefix = "/http",
			OpenApiLabel = "HTTP API",
			Plugins =
			[
				new PluginEntry { Id = "openapi", Options = { ["routePrefix"] = "/admin-api", ["label"] = "Admin" } },
				new PluginEntry { Id = "OpenAPI" }
			]
		};

		SiteConfig config = SiteConfigFactory.Create(options);

		config.Plugins.Select(p => $"{p.Options["routePrefix"]} {p.Options["label"]}")
			.Should().Equal("/admin-api Admin", "/http HTTP API");
		config.Nav.Where(n => n.Icon == "globe").Select(n => $"{n.Path} {n.Label}")
			.Should().Equal("/admin-api Admin", "/http HTTP API");
	}

	[Fact]
	public void OpenApiDocument_DeclaresAnOpenApiEntryForIt()
	{
		SiteConfig config = SiteConfigFactory.Create(new MokaDocsOptions { OpenApiDocument = "v1" });

		PluginDeclaration declaration = config.Plugins.Should().ContainSingle(p => p.Name == "openapi").Which;
		declaration.Options.Should().Contain(AppOpenApiDocument.DocumentNameOption, "v1")
			.And.Contain(OpenApiPlugin.RoutePrefixOption, "/rest-api")
			.And.Contain(OpenApiPlugin.LabelOption, "REST API");
		SiteConfigFactory.Create(new MokaDocsOptions()).Plugins.Should().BeEmpty();
	}

	[Fact]
	public async Task DefaultSidebar_HasAnEntryForTheRestApiPages()
	{
		File.WriteAllText(Path.Combine(_root, "openapi.json"), _spec);
		PluginEntry entry = new() { Id = "openapi", Options = { ["spec"] = "openapi.json" } };

		InMemorySite withPages = await BuildAsync(options => options.Plugins = [entry]);
		InMemorySite customNav = await BuildAsync(options =>
		{
			options.Plugins = [new PluginEntry { Id = "openapi", Options = { ["spec"] = "openapi.json" } }];
			options.Nav = [new NavEntry { Label = "API Reference", Path = "/api" }];
		});
		InMemorySite withoutPages = await BuildAsync(_ => { });

		string sidebar = Sidebar(Html(withPages, "api/index.html"));
		sidebar.Should().Contain("href=\"/docs/rest-api\"").And.Contain("REST API");
		sidebar.Should().Contain("href=\"/docs/rest-api/products\"", "the tag pages are listed below the entry");
		Sidebar(Html(customNav, "api/index.html")).Should().NotContain("/docs/rest-api");
		Sidebar(Html(withoutPages, "api/index.html")).Should().NotContain("/docs/rest-api");
	}

	#endregion

	#region Reading the application's document

	[Fact]
	public async Task Swashbuckle_DocumentIsRead()
	{
		WebApplication app = await StartAppAsync(AddSwashbuckle);

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "v1", TestContext.Current.CancellationToken);

		result.Warning.Should().BeNull();
		result.Document.Should().Contain("\"openapi\": \"3.0");
		OpenApiParser.Parse(result.Document!).Endpoints.Select(e => $"{e.Method} {e.Path}")
			.Should().BeEquivalentTo("GET /shop/points", "POST /shop/orders");
	}

	[Fact]
	public async Task MicrosoftAspNetCoreOpenApi_DocumentIsRead()
	{
		// On net9.0 this is Microsoft.AspNetCore.OpenApi 9, built for Microsoft.OpenApi 1.x. MokaDocs used to
		// move the app to 2.x, and generating the document threw TypeLoadException. In this Development host,
		// its provider also failed to resolve a scoped service until MokaDocs built it from a scope.
		WebApplication app = await StartAppAsync(builder => builder.Services.AddOpenApi());

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "v1", TestContext.Current.CancellationToken);

		result.Warning.Should().BeNull();
#if NET10_0_OR_GREATER
		result.Document.Should().Contain("\"openapi\": \"3.1");
#else
		result.Document.Should().Contain("\"openapi\": \"3.0");
#endif
		OpenApiParser.Parse(result.Document!).Endpoints.Select(e => $"{e.Method} {e.Path}")
			.Should().BeEquivalentTo("GET /shop/points", "POST /shop/orders");
	}

	[Fact]
	public async Task Provider_ThatThrows_IsAWarningWithItsMessage()
	{
		WebApplication app = await StartAppAsync(builder =>
			builder.Services.AddSingleton<Microsoft.Extensions.ApiDescriptions.IDocumentProvider,
				Microsoft.Extensions.ApiDescriptions.ThrowingDocumentProvider>());

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "v1", TestContext.Current.CancellationToken);

		result.Document.Should().BeNull();
		result.Warning.Should().Be("OpenAPI: Generating the application's OpenAPI document 'v1' failed: A document transformer failed.");
	}

	[Fact]
	public async Task DocumentName_IsMatchedWithoutRegardToCase()
	{
		WebApplication app = await StartAppAsync(AddSwashbuckle);

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "V1", TestContext.Current.CancellationToken);

		result.Document.Should().NotBeNull();
	}

	[Fact]
	public async Task NoProvider_WarnsToRegisterOne()
	{
		WebApplication app = await StartAppAsync(_ => { });

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "v1", TestContext.Current.CancellationToken);

		result.Document.Should().BeNull();
		result.Warning.Should().Contain("no OpenAPI document provider")
			.And.Contain("AddOpenApi()")
			.And.Contain("AddSwaggerGen()");
	}

	[Fact]
	public async Task UnknownDocumentName_ListsTheAvailableNames()
	{
		WebApplication app = await StartAppAsync(AddSwashbuckle);

		AppOpenApiDocument.ReadResult result =
			await AppOpenApiDocument.ReadAsync(app.Services, "v2", TestContext.Current.CancellationToken);

		result.Document.Should().BeNull();
		result.Warning.Should().Be("OpenAPI: The application has no OpenAPI document named 'v2'. Available: 'v1', 'internal'.");
	}

	#endregion

	#region The whole host

	[Theory]
	[InlineData("swashbuckle", "/swagger/v1/swagger.json")]
	[InlineData("aspnetcore-openapi", "/openapi/v1.json")]
	public async Task OpenApiDocument_BecomesRestApiPages_WithTypeLinksAndASidebarEntry(string provider, string documentUrl)
	{
		WebApplication app = await StartAppAsync(builder =>
		{
			if (provider == "swashbuckle")
			{
				AddSwashbuckle(builder);
			}
			else
			{
				builder.Services.AddOpenApi();
			}

			builder.Services.AddMokaDocs(options =>
			{
				options.Assemblies = [FixtureAssembly.Instance];
				options.OpenApiDocument = "v1";
			});
		}, app =>
		{
			if (provider == "swashbuckle")
			{
				app.UseSwagger();
			}
			else
			{
				app.MapOpenApi();
			}

			app.MapMokaDocs();
		});

		using var client = new HttpClient { BaseAddress = new Uri(Address(app)) };
		string points = await client.GetStringAsync("/docs/rest-api/points", TestContext.Current.CancellationToken);
		string index = await client.GetStringAsync("/docs/rest-api", TestContext.Current.CancellationToken);

		// The app still serves its own document: MokaDocs adds no Microsoft.OpenApi version of its own.
		using HttpResponseMessage document = await client.GetAsync(documentUrl, TestContext.Current.CancellationToken);
		document.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
		(await document.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("/shop/points");

		points.Should().Contain("/shop/points");
		// Point is a documented type, ShopOrder is not, so it links to the models page.
		points.Should().Contain("<a class=\"openapi-type-link\" href=\"/docs/api/fixture/shapes/point\">Point</a>");
		points.Should().Contain("Schema: <code><a class=\"openapi-type-link\" href=\"/docs/rest-api/models#shop-order\">ShopOrder</a></code>");
		Sidebar(index).Should().Contain("href=\"/docs/rest-api\"").And.Contain("href=\"/docs/rest-api/points\"");
		InMemorySite site = await app.Services.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
		site.Diagnostics.Should().NotContain(d => d.Source == "openapi" || d.Message.Contains("share the route"));

		// The document the pages were built from, served next to them, and the models page.
		index.Should().Contain("<a href=\"/docs/rest-api/openapi.json\" download>");
		using HttpResponseMessage download = await client.GetAsync("/docs/rest-api/openapi.json", TestContext.Current.CancellationToken);
		download.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
		download.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
		string downloaded = await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		OpenApiParser.Parse(downloaded).Endpoints.Select(e => e.Path).Should().Contain("/shop/points");
		string models = await client.GetStringAsync("/docs/rest-api/models", TestContext.Current.CancellationToken);
		models.Should().Contain("<h2 id=\"shop-order\">ShopOrder</h2>");
		Sidebar(models).Should().Contain("href=\"/docs/rest-api/models\"");
	}

	[Fact]
	public async Task YamlSpec_IsServedAsWritten_WithAModelsPageAfterTheTagPages()
	{
		const string yaml = """
		                    # Kept as written, comments too
		                    openapi: 3.0.3
		                    info: { title: Shop, version: "1" }
		                    paths:
		                      /products:
		                        get:
		                          tags: [Products]
		                          responses:
		                            "200":
		                              description: OK
		                              content:
		                                application/json:
		                                  schema: { $ref: "#/components/schemas/Widget" }
		                    components:
		                      schemas:
		                        Widget: { type: object, properties: { id: { type: integer } } }
		                    """;
		File.WriteAllText(Path.Combine(_root, "openapi.yaml"), yaml);

		InMemorySite site = await BuildAsync(options =>
			options.Plugins = [new PluginEntry { Id = "openapi", Options = { ["spec"] = "openapi.yaml" } }]);

		site.Files.Should().ContainKey("rest-api/openapi.yaml");
		Encoding.UTF8.GetString(site.Files["rest-api/openapi.yaml"].Content).Should().Be(File.ReadAllText(Path.Combine(_root, "openapi.yaml")));
		site.Files["rest-api/openapi.yaml"].ContentType.Should().Be("application/yaml; charset=utf-8");
		Html(site, "rest-api/index.html").Should().Contain("href=\"/docs/rest-api/openapi.yaml\" download");
		Html(site, "rest-api/products/index.html").Should()
			.Contain("<a class=\"openapi-type-link\" href=\"/docs/rest-api/models#widget\">Widget</a>");

		string models = Html(site, "rest-api/models/index.html");
		models.Should().Contain("<h2 id=\"widget\">Widget</h2>");
		string sidebar = Sidebar(models);
		sidebar.IndexOf("href=\"/docs/rest-api/models\"", StringComparison.Ordinal).Should()
			.BeGreaterThan(sidebar.IndexOf("href=\"/docs/rest-api/products\"", StringComparison.Ordinal), "the models page follows the tag pages");
	}

	[Fact]
	public async Task PluginEntry_WithAppDocument_AddsASecondDocument()
	{
		WebApplication app = await StartAppAsync(builder =>
		{
			AddSwashbuckle(builder);
			builder.Services.AddMokaDocs(options =>
			{
				options.Assemblies = [FixtureAssembly.Instance];
				options.OpenApiDocument = "v1";
				options.Plugins =
				[
					new PluginEntry { Id = "openapi", Options = { ["appDocument"] = "internal", ["routePrefix"] = "/internal-api" } }
				];
			});
		});

		InMemorySite site = await app.Services.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);

		Html(site, "rest-api/index.html").Should().Contain("<h1>Shop</h1>");
		Html(site, "internal-api/index.html").Should().Contain("<h1>Shop internal</h1>");
		site.Diagnostics.Should().NotContain(d => d.Source == "openapi");
	}

	[Fact]
	public async Task OpenApiDocument_ThatCantBeRead_IsAWarning()
	{
		// Not used: the entry is for the app's document, so the plugin doesn't fall back to looking for a file.
		File.WriteAllText(Path.Combine(_root, "openapi.json"), _spec);
		WebApplication app = await StartAppAsync(builder => builder.Services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.OpenApiDocument = "v1";
		}));

		InMemorySite site = await app.Services.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);

		Diagnostic warning = site.Diagnostics.Should().ContainSingle(d => d.Source == "openapi").Which;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Contain("no OpenAPI document provider");
		site.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
		site.Files.Keys.Should().NotContain(key => key.StartsWith("rest-api", StringComparison.Ordinal));
		site.Files.Keys.Should().Contain("api/index.html");
	}

	#endregion

	#region Helpers

	private static void AddSwashbuckle(WebApplicationBuilder builder)
	{
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(o =>
		{
			// Target-typed: OpenApiInfo is in Microsoft.OpenApi.Models in 1.x (Swashbuckle 9) and in
			// Microsoft.OpenApi in 2.x (Swashbuckle 10).
			o.SwaggerDoc("v1", new() { Title = "Shop", Version = "1.0" });
			o.SwaggerDoc("internal", new() { Title = "Shop internal", Version = "1.0" });
		});
	}

	/// <summary>
	///     Starts an app with two endpoints: one returns the fixture assembly's <c>Point</c>, which the API
	///     reference documents, the other takes a <see cref="ShopOrder" />, which it doesn't.
	/// </summary>
	private async Task<WebApplication> StartAppAsync(Action<WebApplicationBuilder> configure, Action<WebApplication>? map = null)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			// The environment the docs are usually read in, where the host also validates scopes.
			EnvironmentName = Environments.Development,
			ContentRootPath = _root
		});
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		configure(builder);

		WebApplication app = builder.Build();
		Type point = FixtureAssembly.Instance.GetType("Fixture.Shapes.Point", true)!;
		app.MapGet("/shop/points", () => Results.Ok()).WithTags("Points").Produces(StatusCodes.Status200OK, point);
		app.MapPost("/shop/orders", (ShopOrder order) => TypedResults.Created("/shop/orders/1", order)).WithTags("Points");
		map?.Invoke(app);

		await app.StartAsync(TestContext.Current.CancellationToken);
		_apps.Add(app);
		return app;
	}

	private static string Address(WebApplication app) =>
		app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

	private async Task<InMemorySite> BuildAsync(Action<MokaDocsOptions> configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IHostEnvironment>(new ContentRootEnvironment(_root));
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			configure(options);
		});

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	private static string Html(InMemorySite site, string path)
	{
		site.Files.Keys.Should().Contain(path);
		return Encoding.UTF8.GetString(site.Files[path].Content);
	}

	private static string Sidebar(string html)
	{
		int start = html.IndexOf("<nav class=\"sidebar-nav\"", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		return html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];
	}

	private sealed class ContentRootEnvironment(string contentRoot) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Production;
		public string ApplicationName { get; set; } = "Moka.Docs.Integration.Tests";
		public string ContentRootPath { get; set; } = contentRoot;
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}

	#endregion
}

/// <summary>A request body type that the API reference doesn't document.</summary>
/// <param name="Id">Order id.</param>
/// <param name="Note">Optional note.</param>
public sealed record ShopOrder(int Id, string? Note);
