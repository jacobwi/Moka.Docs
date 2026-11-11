using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;

namespace Moka.Docs.Integration.Tests;

/// <summary>
///     Where the build writes, what it refuses to write, and what it reports instead of failing
///     silently.
/// </summary>
public sealed class OutputSafetyTests
{
	private static async Task<BuildContext> BuildAsync(IFileSystem fs, SiteConfig config, string root = "/project",
		bool dryRun = false, Action<BuildContext>? plugin = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		await using ServiceProvider provider = services.BuildServiceProvider();
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = root,
			OutputDirectory = fs.Path.Combine(root, "_site"),
			DryRun = dryRun
		};

		BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
		if (plugin is not null)
		{
			pipeline.PluginHook = (ctx, _) =>
			{
				plugin(ctx);
				return Task.CompletedTask;
			};
		}

		await pipeline.ExecuteAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	private static MockFileSystem Site(params (string Path, string Content)[] pages)
	{
		var files = new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n")
		};
		foreach ((string path, string content) in pages)
		{
			files["/project/docs/" + path] = new MockFileData(content);
		}

		return new MockFileSystem(files);
	}

	private static List<string> FilesOutside(MockFileSystem fs, params string[] allowed)
	{
		string[] roots = allowed.Select(a => fs.Path.GetFullPath(a) + fs.Path.DirectorySeparatorChar).ToArray();
		return fs.AllFiles.Where(f => !roots.Any(r => f.StartsWith(r, StringComparison.OrdinalIgnoreCase))).ToList();
	}

	private static IEnumerable<string> Errors(BuildContext context) =>
		context.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message);

	[Fact]
	public async Task RouteOverride_ClimbingOutOfTheSite_IsAnErrorAndNothingIsWrittenOutside()
	{
		// route: ../../outside-escape wrote index.html two folders above _site, and the section
		// redirect walk dropped more index.html files into the project root and its parent.
		MockFileSystem fs = Site(
			("escape.md", "---\ntitle: Escape\nroute: ../../outside-escape\n---\n# Escape\n"),
			("guide/deep/page.md", "---\ntitle: Deep\nroute: /guide/../../../deep\n---\n# Deep\n"));

		BuildContext context = await BuildAsync(fs, MockSite.Config());

		Errors(context).Should().Contain(m => m.StartsWith("escape.md: front matter route '../../outside-escape'"));
		Errors(context).Should().Contain(m => m.Contains("page.md: front matter route"));
		FilesOutside(fs, "/project/_site", "/project/docs").Should().BeEmpty();
		fs.File.Exists("/project/_site/escape/index.html").Should().BeTrue("the page falls back to its file route");
	}

	[Fact]
	public async Task RouteOverride_WithAQueryString_IsAnErrorAndTheRestOfTheSiteIsBuilt()
	{
		// route: /faq?lang=en crashed the Windows build after the old site had been deleted.
		MockFileSystem fs = Site(("faq.md", "---\ntitle: FAQ\nroute: /faq?lang=en\n---\n# FAQ\n"));

		BuildContext context = await BuildAsync(fs, MockSite.Config());

		Errors(context).Should().ContainSingle().Which.Should().Contain("faq.md").And.Contain("query string");
		fs.File.Exists("/project/_site/index.html").Should().BeTrue();
		fs.File.Exists("/project/_site/faq/index.html").Should().BeTrue();
	}

	[Fact]
	public async Task PluginPages_WithEscapingRoutes_AreNotWritten()
	{
		// Plugin pages skip front matter checks. An OpenAPI tag such as "C:\...\escaped" under
		// routePrefix "/" became a rooted route, and Path.Combine wrote it outside the site.
		MockFileSystem fs = Site();
		string rooted = "/" + fs.Path.GetFullPath("/elsewhere/escaped");

		BuildContext context = await BuildAsync(fs, MockSite.Config(), plugin: ctx =>
		{
			foreach (string route in new[] { rooted, "/../../escaped", "/api/../../../escaped" })
			{
				ctx.Pages.Add(new DocPage
				{
					FrontMatter = new FrontMatter { Title = "Tag" },
					Content = new PageContent { Html = "<p>tag</p>", PlainText = "tag" },
					Route = route,
					Origin = PageOrigin.ApiGenerated
				});
			}
		});

		FilesOutside(fs, "/project/_site", "/project/docs").Should().BeEmpty();
		Errors(context).Count(m => m.StartsWith("generated page 'Tag'")).Should().BeGreaterThanOrEqualTo(2);
		string sitemap = fs.File.ReadAllText("/project/_site/sitemap.xml");
		sitemap.Should().NotContain("..", "pages that were not written are not listed");
		if (OperatingSystem.IsWindows())
		{
			sitemap.Should().NotContain("escaped");
		}
		else
		{
			// Without drive letters the rooted route is just a folder name inside the site.
			fs.File.Exists("/project/_site/elsewhere/escaped/index.html").Should().BeTrue();
		}
	}

	[Fact]
	public async Task PluginDeferredFiles_LeavingTheSite_AreNotWritten()
	{
		MockFileSystem fs = Site();

		BuildContext context = await BuildAsync(fs, MockSite.Config(), plugin: ctx =>
		{
			ctx.DeferredOutputFiles["../../escaped.txt"] = "x"u8.ToArray();
			ctx.DeferredOutputFiles[fs.Path.GetFullPath("/elsewhere/rooted.txt")] = "x"u8.ToArray();
			ctx.DeferredOutputFiles["preview/ok.txt"] = "x"u8.ToArray();
		});

		FilesOutside(fs, "/project/_site", "/project/docs").Should().BeEmpty();
		fs.File.Exists("/project/_site/preview/ok.txt").Should().BeTrue();
		Errors(context).Should().HaveCount(2).And.OnlyContain(m => m.Contains("resolves outside the output directory"));
	}

	[Fact]
	public async Task PluginPage_RoutedAtAnAbsolutePathOnDisk_StaysInsideTheSite()
	{
		// The audit's case on a real disk: routePrefix "/" plus a tag holding an absolute path.
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-escape-" + Guid.NewGuid().ToString("N"));
		string outside = Path.Combine(Path.GetTempPath(), "mokadocs-escaped-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, "docs"));
		File.WriteAllText(Path.Combine(root, "docs", "index.md"), "---\ntitle: Home\n---\n# Home\n");
		try
		{
			BuildContext context = await BuildAsync(new FileSystem(), MockSite.Config(), root, plugin: ctx =>
				ctx.Pages.Add(new DocPage
				{
					FrontMatter = new FrontMatter { Title = "Tag" },
					Content = new PageContent { Html = "<p>tag</p>", PlainText = "tag" },
					Route = "/" + outside,
					Origin = PageOrigin.ApiGenerated
				}));

			Directory.Exists(outside).Should().BeFalse();
			File.Exists(Path.Combine(root, "_site", "index.html")).Should().BeTrue();
			if (OperatingSystem.IsWindows())
			{
				Errors(context).Should().ContainSingle().Which.Should().Contain("generated page 'Tag'");
			}
		}
		finally
		{
			Directory.Delete(root, true);
			if (Directory.Exists(outside))
			{
				Directory.Delete(outside, true);
			}
		}
	}

	[Fact]
	public async Task MissingDocsFolder_IsAnError()
	{
		// A wrong content.docs built an empty site with exit code 0, and validate passed it.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/project/mokadocs.yaml"] = new("site:\n  title: T\n")
		});

		BuildContext context = await BuildAsync(fs, MockSite.Config() with
		{
			Content = new ContentConfig { Docs = "./documentation" }
		}, dryRun: true);

		Errors(context).Should().ContainSingle().Which.Should().StartWith("Docs folder not found")
			.And.Contain("./documentation");
	}

	[Fact]
	public async Task PageThatCannotBeRead_IsAnErrorNamingTheFileAndTheCause()
	{
		MockFileSystem fs = Site(("guide/locked.md", "---\ntitle: Locked\n---\n"));
		fs.GetFile("/project/docs/guide/locked.md").AllowedFileShare = FileShare.None;

		BuildContext context = await BuildAsync(fs, MockSite.Config(), dryRun: true);

		Diagnostic error = context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
			.Subject;
		error.Message.Should().Contain("locked.md").And.Contain("could not be parsed");
		context.Pages.Select(p => p.Route).Should().Equal("/");
	}

	[Fact]
	public async Task ParserWarnings_AreReportedWithTheFileName()
	{
		MockFileSystem fs = Site(("guide/typo.md", "---\ntitel: Typo\n---\n# Typo\n"));

		BuildContext context = await BuildAsync(fs, MockSite.Config(), dryRun: true);

		context.Diagnostics.All.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning
		                                              && d.Message.Contains("typo.md")
		                                              && d.Message.Contains("'titel'"));
	}

	[Fact]
	public async Task ConfigWarnings_AreReportedByTheBuild()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\nbuild:\n  basepath: /x\n") with
		{
			Content = new ContentConfig { Docs = "./docs" }
		};

		BuildContext context = await BuildAsync(Site(), config, dryRun: true);

		context.Diagnostics.All.Should().ContainSingle(d => d.Source == "Config")
			.Which.Message.Should().Contain("build.basepath").And.Contain("line 4").And.Contain("basePath");
	}

	[Fact]
	public async Task Sitemap_IsValidXmlWithEncodedDirectoryUrls()
	{
		// docs/Q&A.md put a raw & into <loc>, so search engines rejected the whole sitemap.
		MockFileSystem fs = Site(
			("Q&A.md", "---\ntitle: Q and A\n---\n# Q and A\n"),
			("guide/my page.md", "---\ntitle: My Page\n---\n# My Page\n"));

		await BuildAsync(fs, MockSite.Config());

		XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
		List<string> locs = XDocument.Parse(fs.File.ReadAllText("/project/_site/sitemap.xml"))
			.Descendants(ns + "loc").Select(e => e.Value).ToList();
		locs.Should().BeEquivalentTo(
			"https://test.example.com/",
			"https://test.example.com/Q%26A/",
			"https://test.example.com/guide/my%20page/");
		fs.File.ReadAllText("/project/_site/Q&A/index.html")
			.Should().Contain("href=\"https://test.example.com/Q%26A/\"");
	}

	[Fact]
	public async Task Build_WritesTheMarkerAndRefusesToCleanAFolderItDidNotCreate()
	{
		MockFileSystem fs = Site();
		await BuildAsync(fs, MockSite.Config());
		fs.File.Exists("/project/_site/" + OutputDirectoryGuard.MarkerFileName).Should().BeTrue();

		var foreign = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\n# Home\n"),
			["/project/_site/app.js"] = new("hand written")
		});

		Func<Task> build = () => BuildAsync(foreign, MockSite.Config());
		await build.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Refusing to delete*");
		foreign.File.ReadAllText("/project/_site/app.js").Should().Be("hand written");

		BuildContext dryRun = await BuildAsync(foreign, MockSite.Config(), dryRun: true);
		Errors(dryRun).Should().ContainSingle().Which.Should().Contain(OutputDirectoryGuard.MarkerFileName);

		BuildContext noClean = await BuildAsync(foreign, MockSite.Config() with
		{
			Build = new BuildConfig { Clean = false }
		});
		Errors(noClean).Should().BeEmpty("without build.clean nothing is deleted");
	}

	[Fact]
	public async Task BrandAssetOutsideTheDocs_WithCleanOff_IsRefreshedWhenItChanges()
	{
		// The copy skipped every logo already in the output, so with build.clean off an edited
		// logo outside docs/ never reached the site.
		MockFileSystem fs = Site();
		fs.AddFile("/project/branding/logo.svg", new MockFileData("<svg>v1</svg>"));
		SiteConfig config = MockSite.Config() with
		{
			Site = new SiteMetadata
			{
				Title = "Brand",
				Logo = new SiteAssetReference
				{
					RawValue = "./branding/logo.svg",
					SourcePath = fs.Path.GetFullPath("/project/branding/logo.svg"),
					PublishUrl = "/branding/logo.svg",
					IsAbsoluteUrl = false
				}
			},
			Build = new BuildConfig { Clean = false }
		};

		await BuildAsync(fs, config);
		fs.File.ReadAllText("/project/_site/branding/logo.svg").Should().Be("<svg>v1</svg>");

		fs.File.WriteAllText("/project/branding/logo.svg", "<svg>v2</svg>");
		await BuildAsync(fs, config);

		fs.File.ReadAllText("/project/_site/branding/logo.svg").Should().Be("<svg>v2</svg>");
	}

	[Fact]
	public async Task RealDisk_ParallelBuild_IsRepeatableAndTheLastPageOnARouteWins()
	{
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-parallel-" + Guid.NewGuid().ToString("N"));
		string docs = Path.Combine(root, "docs");
		for (int section = 0; section < 8; section++)
		{
			Directory.CreateDirectory(Path.Combine(docs, $"s{section}"));
			for (int page = 0; page < 25; page++)
			{
				File.WriteAllText(Path.Combine(docs, $"s{section}", $"p{page}.md"),
					$"---\ntitle: Page {section}.{page}\n---\n# Page {section}.{page}\n\n=== \"One\"\n\nA\n\n=== \"Two\"\n\nB\n");
			}
		}

		File.WriteAllText(Path.Combine(docs, "index.md"), "---\ntitle: Home\n---\n# Home\n");
		File.WriteAllText(Path.Combine(docs, "a-first.md"), "---\ntitle: First\nroute: /same\n---\nfirst\n");
		File.WriteAllText(Path.Combine(docs, "b-second.md"), "---\ntitle: Second\nroute: /same\n---\nsecond\n");
		try
		{
			SiteConfig config = MockSite.Config() with { Build = new BuildConfig { Cache = false } };
			await BuildAsync(new FileSystem(), config, root);
			Dictionary<string, string> first = ReadTree(Path.Combine(root, "_site"));

			BuildContext context = await BuildAsync(new FileSystem(), config, root);
			Dictionary<string, string> second = ReadTree(Path.Combine(root, "_site"));

			context.Pages.Should().HaveCount(203);
			second.Should().BeEquivalentTo(first);
			second[Path.Combine("same", "index.html")].Should().Contain("second").And.NotContain(">first<");
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	private static Dictionary<string, string> ReadTree(string directory) =>
		Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
			.ToDictionary(f => Path.GetRelativePath(directory, f), File.ReadAllText);
}
