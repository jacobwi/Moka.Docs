using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Features;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;

namespace Moka.Docs.Integration.Tests;

public sealed class BuildPipelineIntegrationTests
{
	private static (BuildPipeline pipeline, IServiceProvider provider) CreatePipeline(MockFileSystem fs)
	{
		var services = new ServiceCollection();
		services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug).AddDebug());
		services.AddSingleton<IFileSystem>(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		IConfigurationRoot featureConfig = new ConfigurationBuilder()
			.AddInMemoryCollection(
				MokaFeatureConfiguration.GetDefaults()
					.ToDictionary(
						kv => $"FeatureManagement:{kv.Key}",
						kv => (string?)kv.Value.ToString()))
			.Build();
		services.AddFeatureManagement(featureConfig.GetSection("FeatureManagement"));

		ServiceProvider provider = services.BuildServiceProvider();
		return (provider.GetRequiredService<BuildPipeline>(), provider);
	}

	private static SiteConfig MinimalConfig(string title = "Test Site")
	{
		return new SiteConfig
		{
			Site = new SiteMetadata { Title = title, Url = "https://test.example.com" },
			Content = new ContentConfig { Docs = "./docs" }
		};
	}

	[Fact]
	public async Task FullPipeline_WithMarkdownFiles_ProducesOutput()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/index.md", new MockFileData("""
				                                           ---
				                                           title: Home
				                                           description: Welcome to the docs
				                                           ---
				                                           # Welcome

				                                           This is the home page.
				                                           """)
			},
			{
				"/project/docs/guide/getting-started.md", new MockFileData("""
				                                                           ---
				                                                           title: Getting Started
				                                                           order: 1
				                                                           tags: [setup, quickstart]
				                                                           ---
				                                                           # Getting Started

				                                                           ## Installation

				                                                           Run `dotnet tool install mokadocs`.

				                                                           ## Configuration

				                                                           Create a `mokadocs.yaml` file.
				                                                           """)
			}
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		// Pages should be parsed
		context.Pages.Should().HaveCount(2);
		context.Pages.Should().Contain(p => p.FrontMatter.Title == "Home");
		context.Pages.Should().Contain(p => p.FrontMatter.Title == "Getting Started");

		// HTML files should be written
		fs.File.Exists("/project/_site/index.html").Should().BeTrue();
		fs.File.Exists("/project/_site/guide/getting-started/index.html").Should().BeTrue();

		// Content should contain rendered HTML
		string homeHtml = fs.File.ReadAllText("/project/_site/index.html");
		homeHtml.Should().Contain("Welcome");
		homeHtml.Should().Contain("<h1");

		string guideHtml = fs.File.ReadAllText("/project/_site/guide/getting-started/index.html");
		guideHtml.Should().Contain("Installation");

		// Navigation should be built
		context.Navigation.Should().NotBeNull();
		context.Navigation!.Items.Should().NotBeEmpty();

		// Search index should be built
		context.SearchIndex.Should().NotBeNull();
		context.SearchIndex!.Entries.Should().NotBeEmpty();

		// Sitemap and robots.txt should be generated
		fs.File.Exists("/project/_site/sitemap.xml").Should().BeTrue();
		fs.File.Exists("/project/_site/robots.txt").Should().BeTrue();

		string sitemap = fs.File.ReadAllText("/project/_site/sitemap.xml");
		sitemap.Should().Contain("https://test.example.com/");
	}

	[Fact]
	public async Task FullPipeline_WithAssets_CopiesAssets()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home") },
			{ "/project/docs/images/logo.png", new MockFileData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }) }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		fs.File.Exists("/project/_site/images/logo.png").Should().BeTrue();
	}

	[Fact]
	public async Task FullPipeline_WithFrontMatterRoute_UsesCustomRoute()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/my-page.md", new MockFileData("""
				                                             ---
				                                             title: Custom Route
				                                             route: /custom/path
				                                             ---
				                                             # Custom
				                                             """)
			}
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		context.Pages.Should().Contain(p => p.Route == "/custom/path");
		fs.File.Exists("/project/_site/custom/path/index.html").Should().BeTrue();
	}

	[Fact]
	public async Task FullPipeline_WithDraftPages_ExcludedFromOutput()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home") },
			{ "/project/docs/draft.md", new MockFileData("---\ntitle: Draft\nvisibility: draft\n---\n# Draft") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		// Draft page is parsed but not output
		context.Pages.Should().HaveCount(2);
		fs.File.Exists("/project/_site/index.html").Should().BeTrue();
		fs.File.Exists("/project/_site/draft/index.html").Should().BeFalse();
	}

	[Fact]
	public async Task FullPipeline_WithTableOfContents_ExtractedCorrectly()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/index.md", new MockFileData("""
				                                           ---
				                                           title: ToC Page
				                                           ---
				                                           # Main Title
				                                           ## Section One
				                                           ## Section Two
				                                           ### Subsection
				                                           """)
			}
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		DocPage page = context.Pages.First();
		page.TableOfContents.Entries.Should().NotBeEmpty();
		page.TableOfContents.Entries[0].Text.Should().Be("Main Title");
	}

	[Fact]
	public async Task FullPipeline_WithSearchDisabled_SkipsIndex()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home") }
		});

		SiteConfig config = MinimalConfig() with
		{
			Features = new FeaturesConfig
			{
				Search = new SearchFeatureConfig { Enabled = false }
			}
		};

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		context.SearchIndex.Should().BeNull();
	}

	[Fact]
	public async Task FullPipeline_NoWarnings_OnValidInput()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		context.Diagnostics.HasErrors.Should().BeFalse();
	}

	[Fact]
	public async Task FullPipeline_AlwaysWritesNoJekyllMarker()
	{
		// GitHub Pages runs Jekyll on branch-based deployments and strips directories
		// starting with an underscore, which would take _theme/ and all styling with it.
		// The marker used to come from the Blazor preview plugin, so any site without a
		// published preview host shipped without it.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/index.md", new MockFileData("""
				                                           ---
				                                           title: Home
				                                           ---
				                                           Body
				                                           """)
			}
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await pipeline.ExecuteAsync(context);

		fs.File.Exists("/project/_site/.nojekyll").Should().BeTrue();
		fs.Directory.Exists("/project/_site/_theme").Should().BeTrue();
	}

	[Theory]
	[InlineData("/project/docs")]
	[InlineData("/project")]
	public async Task FullPipeline_OutputContainingTheSources_StopsBeforeDeletingThem(string outputDirectory)
	{
		// The output phase deletes the output directory before writing. With output set to
		// the docs folder or the project root, that deleted the Markdown it had just read.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\nBody") },
			{ "/project/mokadocs.yaml", new MockFileData("site:\n  title: Test\n") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = outputDirectory
		};

		Func<Task> build = () => pipeline.ExecuteAsync(context);

		await build.Should().ThrowAsync<InvalidOperationException>().WithMessage("*output directory*");
		fs.File.Exists("/project/docs/index.md").Should().BeTrue();
		fs.File.Exists("/project/mokadocs.yaml").Should().BeTrue();
	}

	private static async Task<MockFileSystem> BuildSiteAsync(SiteConfig config)
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home\n") },
			{
				"/project/docs/guide/intro.md",
				new MockFileData("---\ntitle: Intro\n---\n# Intro\n\n## Setup\n\nText.\n\n## Usage\n\nMore text.\n")
			}
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		await pipeline.ExecuteAsync(new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		});

		return fs;
	}

	[Theory]
	[InlineData("https://user.github.io/Repo")]
	[InlineData("https://user.github.io")]
	public async Task FullPipeline_SubpathSite_AbsoluteUrlsCarryTheBasePathOnce(string siteUrl)
	{
		// Canonical links appended the prefixed route to a site.url that already ended in the
		// base path (/Repo/Repo/...), and the sitemap never added the base path at all.
		SiteConfig config = MinimalConfig() with
		{
			Site = new SiteMetadata { Title = "Test Site", Url = siteUrl },
			Build = new BuildConfig { BasePath = "/Repo" }
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		string page = fs.File.ReadAllText("/project/_site/guide/intro/index.html");
		page.Should().Contain("""<link rel="canonical" href="https://user.github.io/Repo/guide/intro" />""");
		page.Should().NotContain("/Repo/Repo/");
		fs.File.ReadAllText("/project/_site/sitemap.xml")
			.Should().Contain("<loc>https://user.github.io/Repo/guide/intro</loc>");
		fs.File.ReadAllText("/project/_site/robots.txt")
			.Should().Contain("Sitemap: https://user.github.io/Repo/sitemap.xml");
	}

	[Fact]
	public async Task FullPipeline_NoSiteUrl_WritesNoOpenGraphTags()
	{
		// Scriban treats "" as true, so {{ if site.url }} emitted the tags with an empty og:url.
		MockFileSystem fs = await BuildSiteAsync(MinimalConfig() with
		{
			Site = new SiteMetadata { Title = "Test Site", Url = "" }
		});

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().NotContain("og:title").And.NotContain("rel=\"canonical\"");
	}

	[Fact]
	public async Task FullPipeline_EditLink_UsesForwardSlashesOnEveryOs()
	{
		MockFileSystem fs = await BuildSiteAsync(MinimalConfig() with
		{
			Site = new SiteMetadata
			{
				Title = "Test Site",
				EditLink = new EditLinkConfig { Repo = "https://github.com/o/r", Branch = "main", Path = "docs/" }
			},
			Theme = new ThemeConfig { Options = new ThemeOptions { ShowEditLink = true } }
		});

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().Contain("href=\"https://github.com/o/r/edit/main/docs/guide/intro.md\"");
	}

	[Fact]
	public async Task FullPipeline_SitemapOff_RobotsDoesNotPointAtOne()
	{
		SiteConfig config = MinimalConfig() with { Build = new BuildConfig { Sitemap = false } };

		MockFileSystem fs = await BuildSiteAsync(config);

		fs.File.Exists("/project/_site/sitemap.xml").Should().BeFalse();
		fs.File.ReadAllText("/project/_site/robots.txt").Should().NotContain("Sitemap:");
	}

	[Fact]
	public async Task FullPipeline_ThemeOptionsTurnedOff_RemoveTheirElements()
	{
		// These options reached the templates but nothing read them, so every element always
		// rendered.
		SiteConfig config = MinimalConfig() with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions
				{
					ShowSearch = false,
					ShowDarkModeToggle = false,
					ShowPrevNext = false,
					ShowBreadcrumbs = false,
					ShowBackToTop = false,
					ShowCopyButton = false,
					ShowLineNumbers = false
				}
			}
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		string page = fs.File.ReadAllText("/project/_site/guide/intro/index.html");
		page.Should().NotContain("class=\"search-trigger\"")
			.And.NotContain("id=\"searchModal\"")
			.And.NotContain("class=\"theme-toggle\"")
			.And.NotContain("<nav class=\"page-nav\">")
			.And.NotContain("class=\"breadcrumbs\"")
			.And.NotContain("id=\"backToTop\"");
		page.Should().Contain("data-no-copy-button").And.Contain("data-no-line-numbers");
	}

	[Fact]
	public async Task FullPipeline_SearchFeatureDisabled_RemovesTheSearchButton()
	{
		SiteConfig config = MinimalConfig() with
		{
			Features = new FeaturesConfig { Search = new SearchFeatureConfig { Enabled = false } }
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().NotContain("class=\"search-trigger\"").And.NotContain("id=\"searchModal\"");
	}

	[Fact]
	public async Task FullPipeline_DefaultOptions_KeepEveryElement()
	{
		MockFileSystem fs = await BuildSiteAsync(MinimalConfig());

		string page = fs.File.ReadAllText("/project/_site/guide/intro/index.html");
		page.Should().Contain("class=\"search-trigger\"")
			.And.Contain("class=\"theme-toggle\"")
			.And.Contain("id=\"backToTop\"")
			.And.NotContain("data-no-copy-button");
	}

	[Theory]
	[InlineData("#d946ef", "ocean", "''")]
	[InlineData("#0ea5e9", "ocean", "'ocean'")]
	[InlineData("#d946ef", "violet", "'violet'")]
	public async Task FullPipeline_ColorPreset_IsSkippedOnlyForACustomPrimaryColor(string primary, string preset,
		string expectedInitial)
	{
		// Presets override --color-primary with !important, so starting every page on one made
		// primaryColor do nothing.
		SiteConfig config = MinimalConfig() with
		{
			Theme = new ThemeConfig { Options = new ThemeOptions { PrimaryColor = primary, DefaultColorTheme = preset } }
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().Contain($"localStorage.getItem('mokadocs-color-theme')||{expectedInitial};");
	}

	[Fact]
	public async Task FullPipeline_Copyright_ReplacesTheYearPlaceholder()
	{
		SiteConfig config = MinimalConfig() with
		{
			Site = new SiteMetadata { Title = "Test Site", Copyright = "(c) {year} Example" }
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().Contain($"(c) {DateTime.UtcNow.Year} Example").And.NotContain("{year}");
	}

	[Fact]
	public async Task FullPipeline_PageWithoutDescription_UsesTheSiteDescription()
	{
		SiteConfig config = MinimalConfig() with
		{
			Site = new SiteMetadata { Title = "Test Site", Description = "Docs for the widget library" }
		};

		MockFileSystem fs = await BuildSiteAsync(config);

		fs.File.ReadAllText("/project/_site/guide/intro/index.html")
			.Should().Contain("""<meta name="description" content="Docs for the widget library" />""");
	}

	[Fact]
	public async Task FullPipeline_SearchIndex_CarriesFrontMatterTags()
	{
		// Tags were collected into the index model but never written to search-index.json.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/setup.md", new MockFileData("---\ntitle: Setup\ntags: [install, quickstart]\n---\nText") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		await pipeline.ExecuteAsync(new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		});

		fs.File.ReadAllText("/project/_site/search-index.json").Should().Contain("\"k\":\"install quickstart\"");
	}

	private static async Task<BuildContext> DryRunAsync(SiteConfig config, Dictionary<string, MockFileData> files)
	{
		var fs = new MockFileSystem(files);
		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site",
			DryRun = true
		};

		await pipeline.ExecuteAsync(context);
		return context;
	}

	[Fact]
	public async Task FullPipeline_MalformedFrontMatter_IsReported()
	{
		// The page silently became "Untitled" with its front matter printed as content.
		BuildContext context = await DryRunAsync(MinimalConfig(), new Dictionary<string, MockFileData>
		{
			{ "/project/docs/broken.md", new MockFileData("---\ntitle: Broken\norder: first\n---\nBody") }
		});

		context.Diagnostics.All.Should().ContainSingle(d =>
			d.Source == "MarkdownParse" && d.Message.Contains("broken.md") && d.Message.Contains("front matter"));
	}

	[Fact]
	public async Task FullPipeline_UnknownLayout_IsReportedOnce()
	{
		BuildContext context = await DryRunAsync(MinimalConfig(), new Dictionary<string, MockFileData>
		{
			{ "/project/docs/a.md", new MockFileData("---\ntitle: A\nlayout: wide\n---\nA") },
			{ "/project/docs/b.md", new MockFileData("---\ntitle: B\nlayout: wide\n---\nB") },
			{ "/project/docs/c.md", new MockFileData("---\ntitle: C\nlayout: landing\n---\nC") }
		});

		context.Diagnostics.All.Should().ContainSingle(d => d.Source == "Render")
			.Which.Message.Should().Contain("'wide'").And.Contain("2 page(s)");
	}

	[Fact]
	public async Task FullPipeline_MissingCustomTheme_IsReported()
	{
		// The resolver fell back to the default theme and only logged it.
		SiteConfig config = MinimalConfig() with { Theme = new ThemeConfig { Name = "./themes/missing" } };

		BuildContext context = await DryRunAsync(config, new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\nHome") }
		});

		context.Diagnostics.All.Should().ContainSingle(d => d.Source == "Render")
			.Which.Message.Should().Contain("./themes/missing");
	}

	[Fact]
	public async Task FullPipeline_PagesSharingARoute_AreReported()
	{
		// Both were written to /same/index.html and the first silently disappeared.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/a.md", new MockFileData("---\ntitle: A\nroute: /same\n---\nA") },
			{ "/project/docs/b.md", new MockFileData("---\ntitle: B\nroute: /Same/\n---\nB") },
			{ "/project/docs/c.md", new MockFileData("---\ntitle: C\n---\nC") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/_site",
			DryRun = true
		};

		await pipeline.ExecuteAsync(context);

		context.Diagnostics.All.Should().ContainSingle(d =>
			d.Severity == Moka.Docs.Core.Diagnostics.DiagnosticSeverity.Warning
			&& d.Message.Contains("2 pages share the route '/same'")
			&& d.Message.Contains("a.md") && d.Message.Contains("b.md"));
	}

	[Fact]
	public async Task FullPipeline_DryRunWithOutputContainingTheSources_ReportsAnError()
	{
		// validate and doctor should surface the misconfiguration instead of passing.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\nBody") }
		});

		(BuildPipeline pipeline, _) = CreatePipeline(fs);
		var context = new BuildContext
		{
			Config = MinimalConfig(),
			FileSystem = fs,
			RootDirectory = "/project",
			OutputDirectory = "/project/docs",
			DryRun = true
		};

		await pipeline.ExecuteAsync(context);

		context.Diagnostics.All.Should().ContainSingle(d =>
			d.Severity == Moka.Docs.Core.Diagnostics.DiagnosticSeverity.Error && d.Source == "Output");
		fs.File.Exists("/project/docs/index.md").Should().BeTrue();
	}

	[Fact]
	public async Task FullPipeline_SectionRedirect_IsRootRelativeWithTheBasePath()
	{
		// "./intro/" resolved against /Repo/ when a host served /Repo/guide without a trailing
		// slash, sending the browser to /Repo/intro/.
		MockFileSystem fs = await BuildSiteAsync(MinimalConfig() with { Build = new BuildConfig { BasePath = "/Repo" } });

		fs.File.ReadAllText("/project/_site/guide/index.html")
			.Should().Contain("url=/Repo/guide/intro/\"").And.NotContain("./intro/");
	}

	[Fact]
	public async Task FullPipeline_UnknownSocialLinkIcon_IsReported()
	{
		SiteConfig config = MinimalConfig() with
		{
			Theme = new ThemeConfig
			{
				Options = new ThemeOptions
				{
					SocialLinks =
					[
						new SocialLink { Icon = "github", Url = "https://github.com/o/r" },
						new SocialLink { Icon = "mastodon", Url = "https://mastodon.social/@o" }
					]
				}
			}
		};

		BuildContext context = await DryRunAsync(config, new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\nHome") }
		});

		context.Diagnostics.All.Should().ContainSingle(d => d.Source == "Render")
			.Which.Message.Should().Contain("'mastodon'");
	}

	[Fact]
	public async Task FullPipeline_LayoutThatFailsToRender_IsAnErrorReportedOnce()
	{
		// The engine caught the error and wrote the bare content, so a layout using
		// {{ include }} produced a site without its layout and a build that passed.
		SiteConfig config = MinimalConfig() with { Theme = new ThemeConfig { Name = "./themes/broken" } };

		BuildContext context = await DryRunAsync(config, new Dictionary<string, MockFileData>
		{
			{ "/project/themes/broken/layouts/default.html", new MockFileData("<html>{{ include 'header' }}{{ content }}</html>") },
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\nHome") },
			{ "/project/docs/about.md", new MockFileData("---\ntitle: About\n---\nAbout") }
		});

		context.Diagnostics.All.Should().ContainSingle(d =>
				d.Severity == Moka.Docs.Core.Diagnostics.DiagnosticSeverity.Error && d.Source == "Render")
			.Which.Message.Should().Contain("2 page(s)");
	}

	[Fact]
	public async Task FullPipeline_QuotesInTitleAndDescription_StayInsideTheMetaTags()
	{
		BuildContext context = await DryRunAsync(MinimalConfig(), new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/index.md",
				new MockFileData("---\ntitle: 'The \"best\" <guide>'\ndescription: 'Say \"hi\" & wave'\n---\nHome")
			}
		});

		string html = context.Pages.Single().Content.Html;
		html.Should().Contain("<meta name=\"description\" content=\"Say &quot;hi&quot; &amp; wave\" />")
			.And.Contain("<meta property=\"og:title\" content=\"The &quot;best&quot; &lt;guide&gt;\" />");
	}

	[Fact]
	public async Task FullPipeline_CodeStartingOnThePreLine_KeepsItsIndentation()
	{
		// Every line after the first lost its common indent, so Python source rendered with the
		// method bodies pulled out of the class.
		BuildContext context = await DryRunAsync(MinimalConfig(), new Dictionary<string, MockFileData>
		{
			{
				"/project/docs/index.md",
				new MockFileData("---\ntitle: Home\n---\n<pre><code>class A:\n    def f(self):\n        return 1</code></pre>\n")
			}
		});

		string html = context.Pages.Single().Content.Html;
		string pre = html[html.IndexOf("<pre>", StringComparison.Ordinal)..(html.IndexOf("</pre>", StringComparison.Ordinal) + 6)];
		pre.Should().Be("<pre><code>class A:\n    def f(self):\n        return 1</code></pre>");
	}
}
