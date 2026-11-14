using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Builds sites the way <c>AddMokaDocs</c> and <c>MapMokaDocs</c> do, with a content root in a
///     temp folder that is not the working directory.
/// </summary>
public sealed class AspNetCoreHostTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-aspnetcore-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreHostTests() => Directory.CreateDirectory(_root);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it. Windows can keep a loaded preview DLL locked.
		}
	}

	[Fact]
	public async Task DocsPath_Relative_ResolvesFromTheContentRoot()
	{
		// It resolved from the working directory, so an app started from another folder found no docs.
		Write("guides/index.md", "---\ntitle: Home\n---\n\nWritten in the content root.\n");

		InMemorySite site = await BuildAsync(options => options.DocsPath = "guides");

		Html(site, "index.html").Should().Contain("Written in the content root.");
	}

	[Fact]
	public async Task DocsPath_FolderHoldingASiteMokaDocsBuilt_IsSkippedWithAWarning()
	{
		// A CLI build left in the docs folder was published again inside the app's docs.
		Write("guides/index.md", "---\ntitle: Home\n---\n\nHome.\n");
		Write("guides/_site/.mokadocs-output", "");
		Write("guides/_site/old/page.md", "---\ntitle: Old copy\n---\n\nOld.\n");
		Write("guides/_site/search-index.json", "[]");

		InMemorySite site = await BuildAsync(options => options.DocsPath = "guides");

		site.Files.Keys.Should().NotContain(key => key.StartsWith("_site/", StringComparison.Ordinal));
		site.Diagnostics.Should().ContainSingle(d => d.Message.Contains("'_site'") && d.Message.Contains(".mokadocs-output"));
	}

	[Fact]
	public async Task DocsPath_Absolute_IsUsedAsIs()
	{
		string elsewhere = Path.Combine(_root, "elsewhere");
		Write("elsewhere/index.md", "---\ntitle: Home\n---\n\nWritten somewhere else.\n");

		InMemorySite site = await BuildAsync(options => options.DocsPath = elsewhere);

		Html(site, "index.html").Should().Contain("Written somewhere else.");
	}

	[Fact]
	public async Task Plugins_APluginRegisteredInTheContainer_RunsWithItsOptions()
	{
		// Only the REPL and Blazor preview could be declared, so a plugin the app registered never ran.
		var plugin = new RecordingPlugin();

		InMemorySite site = await BuildAsync(
			options => options.Plugins = [new PluginEntry { Id = "Recording", Options = { ["greeting"] = "hello" } }],
			services => services.AddSingleton<IMokaPlugin>(plugin));

		plugin.Options.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
		Html(site, "from-plugin/index.html").Should().Contain("Added by a plugin.");
	}

	[Fact]
	public void Plugins_DeclaredBuiltIns_AreRegistered()
	{
		// Declaring a built-in is enough. Only EnableRepl and EnableBlazorPreview used to register one besides openapi.
		static string[] RegisteredIds(Action<MokaDocsOptions> configure)
		{
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddMokaDocs(options =>
			{
				options.Assemblies = [FixtureAssembly.Instance];
				configure(options);
			});

			using ServiceProvider provider = services.BuildServiceProvider();
			return provider.GetServices<IMokaPlugin>().Select(p => p.Id).ToArray();
		}

		RegisteredIds(options => options.Plugins =
			[
				new PluginEntry { Id = "mokadocs-changelog" },
				new PluginEntry { Id = "mokadocs-python-api" },
				new PluginEntry { Id = "mokadocs-blazor-preview" }
			])
			.Should().BeEquivalentTo("mokadocs-changelog", "mokadocs-python-api", "mokadocs-blazor-preview");

		// openapi was registered for every app, declared or not. It is registered like the others now.
		RegisteredIds(_ => { }).Should().BeEmpty();
		RegisteredIds(options => options.OpenApiDocument = "v1").Should().BeEquivalentTo("openapi");

		// The REPL runs code, so only EnableRepl registers it; an entry in Plugins doesn't.
		RegisteredIds(options => options.Plugins = [new PluginEntry { Id = "mokadocs-repl" }]).Should().BeEmpty();
		RegisteredIds(options => options.EnableRepl = true).Should().BeEquivalentTo("mokadocs-repl");
	}

	[Fact]
	public async Task Plugins_OpenApiSpec_ResolvesFromTheContentRoot()
	{
		// The build's root was a virtual folder, so the plugin looked for the spec in a directory that does not exist.
		Write("specs/shop.json", """
		                         {
		                           "openapi": "3.0.3",
		                           "info": { "title": "Shop", "version": "1.0.0" },
		                           "paths": {
		                             "/products": {
		                               "get": { "tags": ["Products"], "summary": "List products", "responses": { "200": { "description": "OK" } } }
		                             }
		                           }
		                         }
		                         """);

		InMemorySite site = await BuildAsync(options => options.Plugins =
		[
			new PluginEntry { Id = "openapi", Options = { ["spec"] = "specs/shop.json", ["routePrefix"] = "/rest-api" } }
		]);

		site.Diagnostics.Should().NotContain(d => d.Source == "openapi");
		Html(site, "rest-api/products/index.html").Should().Contain("List products");
		site.Files.Keys.Should().Contain("api/index.html");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void BlazorPreview_OptionsInAPluginsEntry_AreDeclaredOnce(bool alsoEnabledByTheFlag)
	{
		// MokaDocsOptions had no way to pass previewHost or library, so previews never built in this
		// host. The flag must not add a second declaration without the options.
		var options = new MokaDocsOptions
		{
			EnableBlazorPreview = alsoEnabledByTheFlag,
			Plugins = [new PluginEntry { Id = "mokadocs-blazor-preview", Options = { ["previewHost"] = "preview-app" } }]
		};

		SiteConfig config = SiteConfigFactory.Create(options);

		config.Plugins.Should().ContainSingle(p => p.Name == "mokadocs-blazor-preview")
			.Which.Options.Should().Contain("previewHost", "preview-app");
	}

	[Theory]
	[InlineData("Production", false)]
	[InlineData("Staging", false)]
	[InlineData("Development", null)]
	[InlineData(null, null)]
	public void BlazorPreview_OutsideDevelopment_NeverPublishesAHost(string? environment, bool? publishHost)
	{
		// The plugin scaffolded a preview host and ran dotnet publish inside the running app, which
		// needs the SDK on the server and held the first build for minutes.
		var flagged = new MokaDocsOptions { EnableBlazorPreview = true };
		var declared = new MokaDocsOptions
		{
			Plugins = [new PluginEntry { Id = "mokadocs-blazor-preview", Options = { ["previewHost"] = "preview-app" } }]
		};
		IHostEnvironment? host = environment is null ? null : new ContentRootEnvironment(_root) { EnvironmentName = environment };

		foreach (MokaDocsOptions options in (MokaDocsOptions[]) [flagged, declared])
		{
			PluginDeclaration preview = SiteConfigFactory.Create(options, host).Plugins
				.Should().ContainSingle(p => p.Name == "mokadocs-blazor-preview").Which;
			preview.Options.GetValueOrDefault("publishHost").Should().Be(publishHost);
		}
	}

	[Theory]
	[InlineData("Production", null, false, true)]
	[InlineData("Staging", null, false, true)]
	[InlineData(null, null, false, true)]
	[InlineData("Development", null, false, false)]
	[InlineData("Development", true, false, true)]
	[InlineData("Production", false, false, false)]
	[InlineData("Development", null, true, true)]
	public void MinifyAssets_DefaultsToReadableInDevelopmentOnly(string? environment, bool? minifyAssets, bool minify,
		bool minified)
	{
		IHostEnvironment? host = environment is null ? null : new ContentRootEnvironment(_root) { EnvironmentName = environment };

		BuildConfig build = SiteConfigFactory.Create(new MokaDocsOptions { MinifyAssets = minifyAssets, Minify = minify }, host).Build;

		build.ShouldMinifyAssets.Should().Be(minified);
		build.Minify.Should().Be(minify, "Minify keeps controlling the pages");
	}

	[Fact]
	public void BlazorPreview_PublishHostSetInTheEntry_IsKept()
	{
		var options = new MokaDocsOptions
		{
			Plugins = [new PluginEntry { Id = "mokadocs-blazor-preview", Options = { ["publishHost"] = true } }]
		};

		SiteConfigFactory.Create(options, new ContentRootEnvironment(_root)).Plugins
			.Single(p => p.Name == "mokadocs-blazor-preview").Options["publishHost"].Should().Be(true);
	}

	[Fact]
	public void BlazorPreview_ThePluginHostReadsTheEnvironmentsOptions()
	{
		// The plugin host takes plugin options from the registered SiteConfig, not from the build's.
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IHostEnvironment>(new ContentRootEnvironment(_root));
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.EnableBlazorPreview = true;
		});

		using ServiceProvider provider = services.BuildServiceProvider();

		provider.GetRequiredService<SiteConfig>().Plugins.Single(p => p.Name == "mokadocs-blazor-preview")
			.Options["publishHost"].Should().Be(false);
	}

	[Fact]
	public async Task BlazorPreview_WithAPublishedPreviewHost_IsServedFromMemory()
	{
		// Previews could not build in this host: no options reached the plugin, it looked for the host in a
		// virtual folder, and copying the published host through the in-memory file system threw.
		Write("docs/index.md", "---\ntitle: Home\n---\n\n```blazor-preview\n<h3>Hello from a preview</h3>\n```\n");
		Write("preview-app/PreviewApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"></Project>");
		Write("preview-app/publish-output/net10.0/wwwroot/index.html", "<p>preview host</p>");
		Write("preview-app/publish-output/net10.0/wwwroot/_framework/blazor.webassembly.js", "// runtime");
		Write("preview-app/publish-output/net10.0/wwwroot/_framework/dotnet.native.wasm", "wasm");
		// Newer than the project file, so the plugin takes the host as published and does not run dotnet publish.
		File.SetLastWriteTimeUtc(Path.Combine(_root, "preview-app/publish-output/net10.0/wwwroot/_framework/blazor.webassembly.js"),
			DateTime.UtcNow.AddHours(1));
		Directory.CreateDirectory(Path.Combine(_root, "preview-app/bin/Release/net10.0"));
		File.Copy(FixtureAssembly.Instance.Location, Path.Combine(_root, "preview-app/bin/Release/net10.0/Components.dll"));

		InMemorySite site = await BuildAsync(options =>
		{
			options.DocsPath = "docs";
			options.Plugins =
			[
				new PluginEntry { Id = "mokadocs-blazor-preview", Options = { ["previewHost"] = "preview-app" } }
			];
		});

		site.Diagnostics.Should().NotContain(d => d.Source == "mokadocs-blazor-preview");
		Html(site, "index.html").Should().Contain("src=\"/docs/_preview-wasm/index.html?assembly=/docs/_preview-assemblies/");
		site.Files.Keys.Should().Contain(key => key.StartsWith("_preview-assemblies/", StringComparison.Ordinal));
		// The host reads staged folders into the same file map the plugin's hardened page is in;
		// the published page must not replace it.
		Html(site, "_preview-wasm/index.html")
			.Should().StartWith("\n<meta http-equiv=\"Content-Security-Policy\" data-mokadocs-preview")
			.And.EndWith("<p>preview host</p>");
		site.Files["_preview-wasm/_framework/dotnet.native.wasm"].ContentType.Should().Be("application/wasm");
	}

	[Fact]
	public async Task DeferredOutputDirectory_FromAPlugin_IsServedFromMemory()
	{
		// OutputPhase copied it through the in-memory file system, where the folder does not exist, and the build threw.
		Write("host/index.html", "<p>preview host</p>");
		Write("host/_framework/dotnet.native.wasm", "wasm");
		var plugin = new RecordingPlugin { StageDirectory = (Path.Combine(_root, "host"), "_preview-wasm") };

		InMemorySite site = await BuildAsync(
			options => options.Plugins = [new PluginEntry { Id = "recording" }],
			services => services.AddSingleton<IMokaPlugin>(plugin));

		Html(site, "_preview-wasm/index.html").Should().Be("<p>preview host</p>");
		site.Files["_preview-wasm/_framework/dotnet.native.wasm"].ContentType.Should().Be("application/wasm");
	}

	[Fact]
	public async Task Version_ShowsInTheVersionSelector()
	{
		// MokaDocsOptions.Version was read by nothing.
		InMemorySite withVersion = await BuildAsync(options => options.Version = "v2.1");
		InMemorySite withoutVersion = await BuildAsync(_ => { });

		string selector = Html(withVersion, "api/index.html");
		selector.Should().Contain("<div class=\"version-selector\">");
		selector.Should().Contain("<span class=\"version-label\">v2.1</span>");
		Html(withoutVersion, "api/index.html").Should().NotContain("<div class=\"version-selector\">");
	}

	[Fact]
	public void Style_DefaultsToTheDefaultStyleWithThePicker()
	{
		SiteConfig config = SiteConfigFactory.Create(new MokaDocsOptions());

		config.Theme.Options.Style.Should().Be("default");
		config.Theme.Options.StyleSelector.Should().BeTrue();
		config.Theme.Options.TextSizeSelector.Should().BeTrue();
		config.Theme.Options.WidthSelector.Should().BeTrue();
		SiteConfig off = SiteConfigFactory.Create(new MokaDocsOptions { TextSizeSelector = false, WidthSelector = false });
		off.Theme.Options.TextSizeSelector.Should().BeFalse();
		off.Theme.Options.WidthSelector.Should().BeFalse();
		config.Theme.Options.LayoutSelector.Should().BeFalse();
		SiteConfigFactory.Create(new MokaDocsOptions { LayoutSelector = true }).Theme.Options.LayoutSelector.Should().BeTrue();
	}

	[Fact]
	public async Task LayoutSelector_GivesThePagesTheLayoutSection()
	{
		InMemorySite site = await BuildAsync(options =>
		{
			options.LayoutSelector = true;
			options.Layout.Navbar = "compact";
		});
		InMemorySite plain = await BuildAsync(_ => { });

		string page = Html(site, "api/index.html");
		page.Should().Contain("class=\"appearance-layout\"").And.Contain("data-default-layout=\"navbar=compact \"")
			.And.Contain("g('mokadocs-layout-'+k)").And.Contain("/_theme/css/layouts.css?v=");
		Html(plain, "api/index.html").Should().NotContain("appearance-layout").And.NotContain("mokadocs-layout-");
	}

	[Fact]
	public async Task Style_AndStyleSelector_ReachThePages()
	{
		InMemorySite styled = await BuildAsync(options =>
		{
			options.Style = "classic";
			options.StyleSelector = false;
			options.WidthSelector = false;
		});
		InMemorySite unknown = await BuildAsync(options => options.Style = "fancy");

		string page = Html(styled, "api/index.html");
		page.Should().Contain("data-style=\"classic\"").And.Contain("const s='classic';");
		page.Should().NotContain("data-appearance=\"style\"").And.NotContain("data-appearance=\"reader-width\"").And.Contain("data-appearance=\"text-size\"");
		Html(unknown, "api/index.html").Should().Contain("data-style=\"default\"").And.Contain("data-appearance=\"style\"");
		unknown.Diagnostics.Should().ContainSingle(d => d.Source == "Render" && d.Message.Contains("'fancy'"));
	}

	[Fact]
	public async Task AutoGenerate_ListsTheApiNamespacesAndTypesInTheSidebar()
	{
		// The flag was read by nothing, and type pages sit too deep below /api to be listed as its children.
		InMemorySite generated = await BuildAsync(options =>
			options.Nav = [new NavEntry { Label = "API Reference", Path = "/api", AutoGenerate = true }]);
		InMemorySite plain = await BuildAsync(options =>
			options.Nav = [new NavEntry { Label = "API Reference", Path = "/api" }]);
		InMemorySite defaults = await BuildAsync(_ => { });

		string sidebar = Sidebar(Html(generated, "api/index.html"));
		sidebar.Should().Contain("<span class=\"nav-link nav-group-label\">Fixture.Shapes</span>");
		sidebar.Should().Contain("href=\"/docs/api/fixture/shapes/money\"");
		Sidebar(Html(defaults, "api/index.html")).Should().Contain("href=\"/docs/api/fixture/shapes/money\"");
		Sidebar(Html(plain, "api/index.html")).Should().NotContain("/docs/api/fixture/shapes/money");
	}

	[Fact]
	public async Task ApiPages_HaveAnH1AndAMarkdownCopy()
	{
		// Type pages and the API index had no h1 at all.
		InMemorySite site = await BuildAsync(_ => { });

		Html(site, "api/index.html").Should().Contain("<h1>API Reference</h1>");
		Html(site, "api/fixture/shapes/money/index.html").Should().Contain("<h1 class=\"api-type-title\">Money</h1>")
			.And.Contain("data-markdown-url=\"/docs/api/fixture/shapes/money.md\"");
		Encoding.UTF8.GetString(site.Files["api/fixture/shapes/money.md"].Content).Should().StartWith("# Money\n");
		site.Files["llms.txt"].ContentType.Should().StartWith("text/plain");
		site.Files["api/fixture/shapes/money.md"].ContentType.Should().Be("text/markdown; charset=utf-8");
	}

	[Fact]
	public async Task LayoutHome_Showcase_RendersTheLandingFrontMatterUnderThePathBase()
	{
		Write("docs/index.md",
			"---\ntitle: Home\nlayout: landing\nshowcaseCodeLanguage: csharp\nshowcaseCode: |\n  var shop = new Shop();\n" +
			"stats:\n  - value: 12\n    label: Endpoints\ncta:\n  title: Try it\n  link: /guide/start\n---\n\nHome.\n");
		Write("docs/guide/start.md", "---\ntitle: Start\n---\n\n# Start\n");

		InMemorySite site = await BuildAsync(options =>
		{
			options.DocsPath = "docs";
			options.Layout.Home = "showcase";
			options.Layout.Features = "bento";
		});

		string landing = Html(site, "index.html");
		landing.Should().Contain("data-home=\"showcase\"").And.Contain("data-features=\"bento\"");
		landing.Should().Contain("<div class=\"home-showcase-code landing-content\"><div class=\"code-block\"")
			.And.Contain("data-highlighted=\"true\"");
		landing.Should().Contain("<span class=\"home-stat-value\">12</span> <span class=\"home-stat-label\">Endpoints</span>");
		landing.Should().Contain("<a class=\"landing-btn landing-btn-primary home-cta-button\" href=\"/docs/guide/start\">");
		site.Diagnostics.Should().NotContain(d => d.Source == "Render");
	}

	[Fact]
	public async Task AddMokaDocs_KeepsTheApplicationsConfiguration()
	{
		// The engine registered its feature flags as IConfiguration after the host did, so the app injected those instead.
		IConfiguration appConfiguration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["Shop:Name"] = "Moka Shop" })
			.Build();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(appConfiguration);

		services.AddMokaDocs(options => options.Assemblies = [FixtureAssembly.Instance]);

		await using ServiceProvider provider = services.BuildServiceProvider();
		provider.GetRequiredService<IConfiguration>()["Shop:Name"].Should().Be("Moka Shop");

		// Feature management reads its own settings, so the site still builds.
		InMemorySite site = await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
		site.Files.Keys.Should().Contain("api/index.html");
	}

	private async Task<InMemorySite> BuildAsync(Action<MokaDocsOptions> configure, Action<IServiceCollection>? register = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IHostEnvironment>(new ContentRootEnvironment(_root));
		register?.Invoke(services);
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			configure(options);
		});

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
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

	private sealed class RecordingPlugin : IMokaPlugin
	{
		public IReadOnlyDictionary<string, object> Options { get; private set; } = new Dictionary<string, object>();
		public (string SourceDir, string DestRelPath)? StageDirectory { get; init; }
		public string Id => "recording";
		public string Name => "Recording";
		public string Version => "1.0.0";

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;

		public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			Options = context.Options;
			buildContext.Pages.Add(new DocPage
			{
				FrontMatter = new FrontMatter { Title = "From a plugin" },
				Content = new PageContent { Html = "<p>Added by a plugin.</p>", PlainText = "Added by a plugin." },
				Route = "/from-plugin",
				Origin = PageOrigin.ApiGenerated
			});

			if (StageDirectory is { } directory)
			{
				buildContext.DeferredOutputDirectories.Add(directory);
			}

			return Task.CompletedTask;
		}
	}
}
