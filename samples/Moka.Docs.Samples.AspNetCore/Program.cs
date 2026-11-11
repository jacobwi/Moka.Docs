using Moka.Docs.AspNetCore;
using Moka.Docs.Samples.AspNetCore.Models;
using Moka.Docs.Samples.AspNetCore.Services;

// REPL snippets run in a copy of this executable. In that copy, this runs them and returns.
if (await MokaDocsReplWorker.TryRunAsync()) return;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.AddSingleton<IProductService, ProductService>();

// The app's OpenAPI document, served at /openapi/v1.json and rendered as the docs' REST API pages
builder.Services.AddOpenApi();

// Add MokaDocs embedded documentation
builder.Services.AddMokaDocs(options =>
{
	options.Title = "Product API";
	options.Description = "REST API for managing products, categories, and inventory";
	options.Version = "v1.0";
	options.PrimaryColor = "#10b981";
	options.Copyright = "© 2026 MokaDocs Samples";
	options.DocsPath = "./docs";
	options.BasePath = "/docs";
	// Run buttons on csharp-repl blocks. The endpoint exists in Development only.
	options.EnableRepl = true;

	options.Assemblies =
	[
		typeof(Product).Assembly
	];

	// REST API pages from the document AddOpenApi() generates, at /docs/rest-api
	options.OpenApiDocument = "v1";

	options.Nav =
	[
		new NavEntry { Label = "Guide", Path = "/guide", Icon = "book-open", Expanded = true },
		new NavEntry { Label = "REST API", Path = "/rest-api", Icon = "globe" },
		new NavEntry { Label = "API Reference", Path = "/api", Icon = "code", AutoGenerate = true }
	];
});

WebApplication app = builder.Build();

// Minimal API endpoints
RouteGroupBuilder products = app.MapGroup("/api/products").WithTags("Products");

products.MapGet("/", (IProductService svc) => svc.GetAll())
	.WithSummary("Get all products")
	.WithDescription("Returns a list of all products in the catalog.");

products.MapGet("/{id:int}", (int id, IProductService svc) =>
		svc.GetById(id) is { } product
			? Results.Ok(product)
			: Results.NotFound())
	.WithSummary("Get product by ID");

products.MapPost("/", (CreateProductRequest request, IProductService svc) =>
{
	Product product = svc.Create(request);
	return Results.Created($"/api/products/{product.Id}", product);
}).WithSummary("Create a new product");

products.MapPut("/{id:int}", (int id, UpdateProductRequest request, IProductService svc) =>
		svc.Update(id, request) ? Results.NoContent() : Results.NotFound())
	.WithSummary("Update an existing product");

products.MapDelete("/{id:int}", (int id, IProductService svc) =>
		svc.Delete(id) ? Results.NoContent() : Results.NotFound())
	.WithSummary("Delete a product");

app.MapOpenApi();

// Serve MokaDocs at /docs
app.MapMokaDocs();

app.Run();
