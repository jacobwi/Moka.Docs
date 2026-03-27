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
}
