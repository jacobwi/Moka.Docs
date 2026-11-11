using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.Plugins.OpenApi;

/// <summary>
///     The routes of tag pages. A tag is free text in the document, and its route becomes a folder, a link
///     and a sitemap entry.
/// </summary>
public sealed partial class OpenApiTagRouteTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-openapi-tags-" + Guid.NewGuid().ToString("N"));

	public OpenApiTagRouteTests() => Directory.CreateDirectory(Path.Combine(_root, "docs"));

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

	/// <summary>A document with one GET operation per entry, in this order, each with the given tags.</summary>
	internal static string Document(params string[][] tagsPerPath) =>
		JsonSerializer.Serialize(new Dictionary<string, object>
		{
			["openapi"] = "3.0.3",
			["info"] = new Dictionary<string, string> { ["title"] = "Tags", ["version"] = "1" },
			["paths"] = tagsPerPath.Select((tags, i) => (tags, i)).ToDictionary(
				entry => $"/p{entry.i}",
				entry => (object)new Dictionary<string, object>
				{
					["get"] = new Dictionary<string, object> { ["tags"] = entry.tags, ["responses"] = new Dictionary<string, object>() }
				})
		});

	private static async Task<BuildContext> RunAsync(string document, string routePrefix = "/rest-api")
	{
		var context = new BuildContext
		{
			Config = new SiteConfig
			{
				Site = new SiteMetadata { Title = "T" },
				Plugins =
				[
					new PluginDeclaration
					{
						Name = "openapi",
						Options = { [OpenApiPlugin.DocumentOption] = document, [OpenApiPlugin.RoutePrefixOption] = routePrefix }
					}
				]
			},
			FileSystem = new MockFileSystem(),
			RootDirectory = Path.GetTempPath(),
			OutputDirectory = Path.Combine(Path.GetTempPath(), "_site")
		};
		ServiceProvider provider = new ServiceCollection()
			.AddSingleton<IMokaPlugin, OpenApiPlugin>()
			.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
			.BuildServiceProvider();
		var host = new PluginHost(context.Config, provider, NullLogger<PluginHost>.Instance);
		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);
		await host.ExecuteAllAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	private static string TagRoute(BuildContext context, string tag) =>
		context.Pages.Single(p => p.FrontMatter.Title == tag && p.Route != "/rest-api").Route;

	[Theory]
	[InlineData("Pets", "/rest-api/pets")]
	[InlineData("Foo Bar", "/rest-api/foo-bar")]
	[InlineData("a.b", "/rest-api/a-b")]
	[InlineData("Admin Users  2", "/rest-api/admin-users--2")]
	[InlineData(" ", "/rest-api/-")]
	[InlineData("ユーザー 管理", "/rest-api/ユーザー-管理")]
	public async Task Tag_WhoseRouteWasAlreadySafe_KeepsIt(string tag, string route)
	{
		BuildContext context = await RunAsync(Document([tag]));

		TagRoute(context, tag).Should().Be(route);
	}

	[Theory]
	[InlineData("x\"onmouseover=\"alert(1)", "/rest-api/x-onmouseover-alert-1")]
	[InlineData("Admin: Users", "/rest-api/admin-users")]
	[InlineData(@"C:\..\..\escaped", "/rest-api/c-escaped")]
	[InlineData("Q&A", "/rest-api/q-a")]
	[InlineData("C#", "/rest-api/c")]
	[InlineData("What?", "/rest-api/what")]
	[InlineData(@"\evil%2ecom", "/rest-api/evil-2ecom")]
	[InlineData("<>|*", "/rest-api/tag")]
	[InlineData("Ünïcødé ✓", "/rest-api/unic-de")]
	[InlineData("Con", "/rest-api/con-tag")]
	public async Task Tag_WithCharactersARouteCantHold_GetsAStrictSlug(string tag, string route)
	{
		BuildContext context = await RunAsync(Document([tag]));

		TagRoute(context, tag).Should().Be(route);
	}

	[Fact]
	public void Tag_WithALoneSurrogate_GetsAStrictSlug()
	{
		// The readers refuse or replace a lone surrogate, but a spec built in code can hold one, and
		// string.Normalize throws on it.
		var spec = new OpenApiSpec();
		var endpoint = new OpenApiEndpoint { Method = "GET", Path = "/a" };
		endpoint.Tags.Add(new string(['\uD800', 'P', 'e', 't', 's']));
		spec.Endpoints.Add(endpoint);

		string index = OpenApiPageRenderer.RenderIndexPage(spec, "/rest-api");

		index.Should().Contain("<a href=\"/rest-api/pets#get-a\">");
	}

	[Fact]
	public async Task Tag_LongerThanAFileNameCanBe_IsCutShort()
	{
		string slugged = "Long: " + new string('x', 300);
		string plain = new('y', 150);

		BuildContext context = await RunAsync(Document([slugged], [plain]));

		TagRoute(context, slugged).Should().Be("/rest-api/long-" + new string('x', 95));
		TagRoute(context, plain).Should().Be("/rest-api/" + new string('y', 100));
	}

	[Fact]
	public async Task TagsThatSlugAlike_GetUniqueRoutes_InDocumentOrder()
	{
		// a/b and a.b both became /rest-api/a-b, and one page silently replaced the other.
		BuildContext context = await RunAsync(Document(["a/b"], ["a.b"], ["Q&A"], ["q-a"], ["Foo Bar"], ["foo-bar"]));

		TagRoute(context, "a/b").Should().Be("/rest-api/a-b");
		TagRoute(context, "a.b").Should().Be("/rest-api/a-b-2");
		TagRoute(context, "Q&A").Should().Be("/rest-api/q-a");
		TagRoute(context, "q-a").Should().Be("/rest-api/q-a-2");
		TagRoute(context, "Foo Bar").Should().Be("/rest-api/foo-bar");
		TagRoute(context, "foo-bar").Should().Be("/rest-api/foo-bar-2");
		context.Pages.Select(p => p.Route).Should().OnlyHaveUniqueItems();
		string index = context.Pages.Single(p => p.Route == "/rest-api").Content.Html;
		index.Should().Contain("<a href=\"/rest-api/a-b-2#get-p1\"><code>/p1</code></a>");
	}

	[Fact]
	public async Task TagsThatSlugAlike_AreNumberedTheSameWayOnEveryBuild()
	{
		string document = Document(["b.x"], ["b/x"], ["a?"], ["a!"]);

		string[] first = (await RunAsync(document)).Pages.Select(p => $"{p.FrontMatter.Title} {p.Route}").ToArray();
		string[] second = (await RunAsync(document)).Pages.Select(p => $"{p.FrontMatter.Title} {p.Route}").ToArray();

		second.Should().Equal(first);
	}

	[Fact]
	public async Task HostileTags_GetIndexAnchorsOfLettersDigitsAndHyphens()
	{
		string[] tags = ["x\"onmouseover=\"alert(1)", "Admin: Users", "<script>", "Q&A", "C#", "#"];

		BuildContext context = await RunAsync(Document(tags.Select(t => new[] { t }).ToArray()));

		context.Pages.Single(p => p.Route == "/rest-api").TableOfContents.Entries.Select(e => e.Id).Should()
			.HaveCount(tags.Length).And.OnlyHaveUniqueItems().And.OnlyContain(id => AnchorId().IsMatch(id));
	}

	[Fact]
	public async Task TagListedTwice_ListsTheEndpointOnce()
	{
		BuildContext context = await RunAsync(Document(["dup", "dup"], ["Users", "users"]));

		context.Pages.Single(p => p.Route == "/rest-api/dup").TableOfContents.Entries.Should().ContainSingle();
		context.Pages.Single(p => p.Route == "/rest-api/users").TableOfContents.Entries.Should().ContainSingle();
		OpenApiParser.Parse(Document(["Users", "users", "Users"])).Endpoints.Single().Tags.Should().Equal("Users");
	}

	[Fact]
	public async Task TagPages_AreOrderedByName_IgnoringCase_WhateverTheCulture()
	{
		// The default comparer put _internal first under en-US and Zebra first under invariant globalization.
		BuildContext context = await RunAsync(Document(["apple"], ["Zebra"], ["banana"], ["_internal"]));

		context.Pages.Where(p => p.Route != "/rest-api").OrderBy(p => p.FrontMatter.Order).Select(p => p.FrontMatter.Title)
			.Should().Equal("apple", "banana", "Zebra", "_internal");
	}

	[Fact]
	public async Task HostTags_BuildIntoTheInMemoryFileSystem()
	{
		// The ASP.NET Core host writes into a MockFileSystem, which threw for "Admin: Users" on Windows.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home\n") }
		});
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Host", Url = "https://docs.example.com" },
			Content = new ContentConfig { Docs = "./docs" },
			Plugins =
			[
				new PluginDeclaration
				{
					Name = "openapi",
					Options = { [OpenApiPlugin.DocumentOption] = Document(["Admin: Users"], ["What?"], ["x\"q"]), [OpenApiPlugin.RoutePrefixOption] = "/rest" }
				}
			]
		};

		BuildContext context = await BuildAsync(config, fs, "/project", "/project/_site");

		context.Diagnostics.All.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
		fs.AllFiles.Select(f => f.Replace('\\', '/')).Should().Contain(f => f.EndsWith("/project/_site/rest/admin-users/index.html", StringComparison.Ordinal))
			.And.Contain(f => f.EndsWith("/project/_site/rest/what/index.html", StringComparison.Ordinal))
			.And.Contain(f => f.EndsWith("/project/_site/rest/x-q/index.html", StringComparison.Ordinal));
	}

	[Fact]
	public async Task HostileTags_AtTheSiteRoot_BuildSafePagesOnDisk()
	{
		// On Windows a tag shaped like an absolute path, with routePrefix "/", wrote index.html outside
		// the output folder: the route was rooted, so Path.Combine dropped the output folder.
		string escapeTarget = Path.Combine(_root, "escaped");
		string[] tags = ["x\"onmouseover=\"alert(1)", "Admin: Users", escapeTarget, "Q&A", "C#", "Pets"];
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\nHome\n");
		File.WriteAllText(Path.Combine(_root, "openapi.json"), Document(tags.Select(t => new[] { t }).ToArray()));
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Tags", Url = "https://docs.example.com" },
			Content = new ContentConfig { Docs = "./docs" },
			Plugins = [new PluginDeclaration { Name = "openapi", Options = { ["spec"] = "openapi.json", ["routePrefix"] = "/" } }],
			Build = new BuildConfig { Cache = false, Output = "./_site" }
		};
		string output = Path.Combine(_root, "_site");

		BuildContext context;
		await using (ServiceProvider provider = BuildCommand.BuildServices(config, false))
		{
			BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
			await BuildCommand.InitializePluginsAsync(provider, pipeline);
			context = new BuildContext
			{
				Config = config,
				FileSystem = new FileSystem(),
				RootDirectory = _root,
				OutputDirectory = output,
				UseCache = false
			};
			await pipeline.ExecuteAsync(context, TestContext.Current.CancellationToken);
		}

		context.Diagnostics.All.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
		Directory.Exists(escapeTarget).Should().BeFalse();
		Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
			.Where(f => !f.StartsWith(Path.Combine(_root, "docs"), StringComparison.Ordinal) && Path.GetFileName(f) != "openapi.json")
			.Should().OnlyContain(f => f.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.Ordinal));

		List<string> tagRoutes = context.Pages.Where(p => p.Origin == PageOrigin.ApiGenerated && tags.Contains(p.FrontMatter.Title))
			.Select(p => p.Route).ToList();
		tagRoutes.Should().HaveCount(tags.Length).And.OnlyContain(r => SafeRoute().IsMatch(r));
		foreach (string route in tagRoutes)
		{
			File.Exists(Path.Combine(output, route.TrimStart('/'), "index.html")).Should().BeTrue(route);
		}

		XDocument.Parse(File.ReadAllText(Path.Combine(output, "sitemap.xml"))).Root!.Elements().Should().NotBeEmpty();
		string page = File.ReadAllText(Path.Combine(output, "pets", "index.html"));
		page.Should().Contain("href=\"/x-onmouseover-alert-1\"");
		HrefBreakout().IsMatch(page).Should().BeFalse("no link's href may end at a quote from the tag");
	}

	private static async Task<BuildContext> BuildAsync(SiteConfig config, IFileSystem fs, string root, string output)
	{
		var services = new ServiceCollection();
		services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
		services.AddSingleton(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();
		services.AddSingleton<IMokaPlugin, OpenApiPlugin>();
		await using ServiceProvider provider = services.BuildServiceProvider();
		BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
		var host = new PluginHost(config, provider, NullLogger<PluginHost>.Instance);
		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);
		pipeline.PluginHook = (ctx, ct) => host.ExecuteAllAsync(ctx, ct);
		var context = new BuildContext { Config = config, FileSystem = fs, RootDirectory = root, OutputDirectory = output };
		await pipeline.ExecuteAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	[GeneratedRegex("^/[a-z0-9-]+$")]
	private static partial Regex SafeRoute();

	[GeneratedRegex("href=\"[^\"]*\"onmouseover")]
	private static partial Regex HrefBreakout();

	[GeneratedRegex(@"^[\p{L}\p{N}-]+$")]
	private static partial Regex AnchorId();
}
