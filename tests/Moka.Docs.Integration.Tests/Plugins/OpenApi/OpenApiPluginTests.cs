using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.Plugins.OpenApi;

/// <summary>
///     Runs the OpenAPI plugin the way the plugin host does, against the document Microsoft.AspNetCore.OpenApi
///     10 wrote for the fixture app.
/// </summary>
public sealed class OpenApiPluginTests : IDisposable
{
	private const string _productModelLink = "<a class=\"openapi-type-link\" href=\"/rest-api/models#product\">Product</a>";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-openapi-" + Guid.NewGuid().ToString("N"));

	public OpenApiPluginTests() => Directory.CreateDirectory(Path.Combine(_root, "docs"));

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

	private static ApiType Type(string name, string ns) =>
		new() { Name = name, FullName = $"{ns}.{name}", Namespace = ns, Kind = ApiTypeKind.Class };

	/// <summary>An API model of the given namespaces, with a page for each type as the API phases make them.</summary>
	private BuildContext CreateBuild(params ApiNamespace[] namespaces)
	{
		var config = new SiteConfig { Site = new SiteMetadata { Title = "T" } };
		var context = new BuildContext
		{
			Config = config,
			FileSystem = new MockFileSystem(),
			RootDirectory = _root,
			OutputDirectory = Path.Combine(_root, "_site")
		};

		if (namespaces.Length > 0)
		{
			context.ApiModel = new ApiReference { Namespaces = [.. namespaces] };
			foreach (ApiNamespace ns in namespaces)
			foreach (ApiType type in ns.Types)
			{
				context.Pages.Add(new DocPage
				{
					FrontMatter = new FrontMatter { Title = type.Name },
					Content = PageContent.Empty,
					Route = $"/api/{ns.Name.Replace('.', '/')}/{type.Name}".ToLowerInvariant(),
					Origin = PageOrigin.ApiGenerated
				});
			}
		}

		return context;
	}

	private static async Task<BuildContext> RunAsync(BuildContext context, params Dictionary<string, object>[] entries)
	{
		var config = context.Config with
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

	private static Dictionary<string, object> Document(string routePrefix = "/rest-api") => new()
	{
		[OpenApiPlugin.DocumentOption] = OpenApiParserTests.Fixture("aspnetcore-openapi-10.json"),
		[OpenApiPlugin.RoutePrefixOption] = routePrefix
	};

	private static string Html(BuildContext context, string route) =>
		context.Pages.Single(p => p.Route == route).Content.Html;

	[Fact]
	public async Task SchemaName_WithOneDocumentedType_LinksToItsPage()
	{
		BuildContext context = await RunAsync(
			CreateBuild(new ApiNamespace
			{
				Name = "Shop.Models",
				Types = [Type("Product", "Shop.Models"), Type("ProductCategory", "Shop.Models"), Type("Discount", "Shop.Models")]
			}),
			Document());

		string products = Html(context, "/rest-api/products");
		// Response types and the property types of a request body link.
		products.Should().Contain("<code><a class=\"openapi-type-link\" href=\"/api/shop/models/product\">Product</a></code>");
		products.Should().Contain("<code><a class=\"openapi-type-link\" href=\"/api/shop/models/productcategory\">ProductCategory</a></code>");
		// No documented type is called PagedResultOfProduct or CreateProductRequest, so they link to the models page.
		products.Should()
			.Contain("<code><a class=\"openapi-type-link\" href=\"/rest-api/models#paged-result-of-product\">PagedResultOfProduct</a></code>")
			.And.Contain("Schema: <code><a class=\"openapi-type-link\" href=\"/rest-api/models#create-product-request\">CreateProductRequest</a></code>");
		Html(context, "/rest-api/discounts").Should()
			.Contain("<code><a class=\"openapi-type-link\" href=\"/api/shop/models/discount\">Discount</a> | null</code>");
		context.Diagnostics.All.Should().BeEmpty();
	}

	[Fact]
	public async Task SchemaName_SharedByTwoDocumentedTypes_LinksToTheModel()
	{
		BuildContext context = await RunAsync(
			CreateBuild(
				new ApiNamespace { Name = "Shop.Models", Types = [Type("Product", "Shop.Models")] },
				new ApiNamespace { Name = "Shop.Legacy", Types = [Type("Product", "Shop.Legacy")] }),
			Document());

		string products = Html(context, "/rest-api/products");
		products.Should().NotContain("href=\"/api/shop");
		products.Should().Contain(_productModelLink);
	}

	[Fact]
	public async Task SchemaName_WithoutAnApiModel_LinksToTheModel()
	{
		BuildContext context = await RunAsync(CreateBuild(), Document());

		Html(context, "/rest-api/products").Should().NotContain("href=\"/api/").And.Contain(_productModelLink);
	}

	[Fact]
	public async Task SchemaName_WhoseTypeHasNoPage_LinksToTheModel()
	{
		BuildContext context = CreateBuild(new ApiNamespace { Name = "Shop.Models", Types = [Type("Product", "Shop.Models")] });
		context.Pages.Clear();

		await RunAsync(context, Document());

		Html(context, "/rest-api/products").Should().NotContain("href=\"/api/").And.Contain(_productModelLink);
	}

	[Fact]
	public async Task EnumValues_AreListedWithTheProperty()
	{
		BuildContext context = await RunAsync(CreateBuild(), Document());

		Html(context, "/rest-api/products").Should().Contain(
			"<span class=\"openapi-values\">Values: <code>Electronics</code>, <code>Books</code>, <code>Clothing</code></span>");
	}

	[Fact]
	public async Task SeveralEntries_EachUseTheirOwnOptions()
	{
		// The options lived in fields set by InitializeAsync, and the entries share one plugin instance,
		// so every entry rendered with the last one's options.
		BuildContext context = await RunAsync(CreateBuild(), Document("/public-api"), Document("/admin-api"));

		context.Pages.Select(p => p.Route).Should().Contain(["/public-api", "/public-api/products", "/admin-api", "/admin-api/products"]);
	}

	[Fact]
	public async Task RoutePrefix_WithoutALeadingSlash_GetsOne()
	{
		BuildContext context = await RunAsync(CreateBuild(), Document("rest-api/"));

		context.Pages.Select(p => p.Route).Should().Contain(["/rest-api", "/rest-api/products"]);
		Html(context, "/rest-api").Should().Contain("href=\"/rest-api/products#get-api-products\"");
	}

	[Fact]
	public async Task EmptyDocument_GeneratesNothingAndDoesNotLookForAFile()
	{
		// The ASP.NET Core host leaves it empty after warning that the app's document could not be read.
		File.WriteAllText(Path.Combine(_root, "openapi.json"), OpenApiParserTests.Fixture("aspnetcore-openapi-10.json"));

		BuildContext context = await RunAsync(CreateBuild(), new Dictionary<string, object> { [OpenApiPlugin.DocumentOption] = "" });

		context.Pages.Should().BeEmpty();
		context.Diagnostics.All.Should().BeEmpty();
	}

	[Fact]
	public async Task Cli_SchemaNames_LinkToTheCSharpTypePages()
	{
		// The CLI documents the projects and runs the plugin in one build, so the same links apply.
		Write("src/Shop/Shop.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		Write("src/Shop/Product.cs", """
		                             namespace Shop.Models;

		                             /// <summary>A product.</summary>
		                             public class Product { }
		                             """);
		Write("openapi.json", OpenApiParserTests.Fixture("aspnetcore-openapi-10.json"));
		Write("docs/index.md", "---\ntitle: Home\n---\nHome\n");
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Shop" },
			Content = new ContentConfig { Docs = "./docs", Projects = [new ProjectSource { Path = "./src/Shop/Shop.csproj" }] },
			Plugins = [new PluginDeclaration { Name = "openapi", Options = { ["routePrefix"] = "/rest-api" } }],
			Build = new BuildConfig { Cache = false, BasePath = "/Shop" }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false, TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.Pages.Should().Contain(p => p.Route == "/api/shop/models/product");
		// Rendered: the base path is in the link, as in every other content link.
		outcome.Context.Pages.Single(p => p.Route == "/rest-api/products").Content.Html
			.Should().Contain("<a class=\"openapi-type-link\" href=\"/Shop/api/shop/models/product\">Product</a>");
	}

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}
}
