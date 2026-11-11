using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Engine.Tests.Navigation;

public sealed class NavigationBuildPhaseTests
{
	private static DocPage Page(string route, string title, int order = 0, bool expanded = true) => new()
	{
		FrontMatter = new FrontMatter { Title = title, Order = order, Expanded = expanded },
		Content = new PageContent { Html = "", PlainText = "" },
		Route = route
	};

	private static async Task<NavigationTree> BuildAsync(List<NavItem> nav, params DocPage[] pages) =>
		(await RunAsync(nav, pages)).Navigation!;

	private static async Task<BuildContext> RunAsync(List<NavItem> nav, params DocPage[] pages) =>
		await RunAsync(nav, null, pages);

	private static async Task<BuildContext> RunAsync(List<NavItem> nav, ApiReference? apiModel, params DocPage[] pages)
	{
		var context = new BuildContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "Nav" }, Nav = nav },
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site",
			ApiModel = apiModel
		};
		context.Pages.AddRange(pages);

		await new NavigationBuildPhase(NullLogger<NavigationBuildPhase>.Instance)
			.ExecuteAsync(context, TestContext.Current.CancellationToken);

		return context;
	}

	[Fact]
	public async Task ConfiguredNav_PathWithATrailingSlash_StillFindsItsPages()
	{
		// "/guide/" matched no page route, so the item had no children.
		NavigationTree tree = await BuildAsync(
			[new NavItem { Label = "Guide", Path = "/guide/" }],
			Page("/guide/intro", "Intro"));

		NavigationNode guide = tree.Items.Single();
		guide.Route.Should().Be("/guide/intro");
		guide.Children.Select(c => c.Route).Should().Equal("/guide/intro");
	}

	[Fact]
	public async Task UnknownIcons_AreReportedOncePerName()
	{
		BuildContext context = await RunAsync(
		[
			new NavItem { Label = "Themes", Path = "/themes", Icon = "palette" },
			new NavItem { Label = "More", Path = "/more", Icon = "palette" },
			new NavItem { Label = "Guide", Path = "/guide", Icon = "book-open" }
		], Page("/themes", "Themes"), Page("/more", "More"), Page("/guide", "Guide"));

		context.Diagnostics.All.Should().ContainSingle()
			.Which.Message.Should().Contain("'palette'").And.Contain("'Themes'").And.Contain("'More'");
	}

	[Fact]
	public async Task ConfiguredNav_WithoutOrderValues_KeepsTheYamlOrder()
	{
		// Ties used to be sorted by label, so this came out Advanced, Getting Started, Guide.
		List<NavItem> nav =
		[
			new() { Label = "Getting Started", Path = "/getting-started" },
			new() { Label = "Guide", Path = "/guide" },
			new() { Label = "Advanced", Path = "/advanced" }
		];

		NavigationTree tree = await BuildAsync(nav);

		tree.Items.Select(i => i.Label).Should().Equal("Getting Started", "Guide", "Advanced");
	}

	[Fact]
	public async Task ConfiguredNav_OrderValues_StillWin()
	{
		List<NavItem> nav =
		[
			new() { Label = "Getting Started", Path = "/getting-started", Order = 2 },
			new() { Label = "Guide", Path = "/guide", Order = 1 },
			new() { Label = "Advanced", Path = "/advanced", Order = 2 }
		];

		NavigationTree tree = await BuildAsync(nav);

		tree.Items.Select(i => i.Label).Should().Equal("Guide", "Getting Started", "Advanced");
	}

	[Fact]
	public async Task AutoNav_SectionIndexPageIcon_IsUsedForTheSection()
	{
		DocPage guide = Page("/guide", "Guide") with
		{
			FrontMatter = new FrontMatter { Title = "Guide", Icon = "book-open" }
		};

		NavigationTree tree = await BuildAsync([], guide, Page("/guide/intro", "Intro"));

		tree.Items.Single(i => i.Label == "Guide").Icon.Should().Be("book-open");
	}

	[Fact]
	public async Task AutoNav_SectionIndexPageWithExpandedFalse_StartsCollapsed()
	{
		NavigationTree tree = await BuildAsync([],
			Page("/guide", "Guide", expanded: false),
			Page("/guide/intro", "Intro"),
			Page("/reference", "Reference"),
			Page("/reference/api", "API"));

		tree.Items.Single(i => i.Label == "Guide").Expanded.Should().BeFalse();
		tree.Items.Single(i => i.Label == "Reference").Expanded.Should().BeTrue();
	}

	[Fact]
	public async Task ConfiguredNav_PathMatchingNoPage_IsAWarning()
	{
		// The sidebar linked to /guid and nothing said the link was dead.
		BuildContext context = await RunAsync(
			[new NavItem { Label = "Guide", Path = "/guid" }],
			Page("/guide", "Guide"));

		context.Diagnostics.All.Should().ContainSingle()
			.Which.Message.Should().Contain("'Guide'").And.Contain("/guid").And.Contain("matches no page");
	}

	[Theory]
	[InlineData("/guide")]
	[InlineData("/reference")]
	[InlineData("/")]
	public async Task ConfiguredNav_PathWithAPageOrPagesBelowIt_IsNotWarned(string path)
	{
		// /reference has no page of its own, but /reference/api/types gets a section redirect.
		BuildContext context = await RunAsync(
			[new NavItem { Label = "Item", Path = path }],
			Page("/", "Home"), Page("/guide", "Guide"), Page("/reference/api/types", "Types"));

		context.Diagnostics.All.Should().BeEmpty();
	}

	[Fact]
	public async Task ConfiguredNav_RootPathWithoutAHomePage_IsAWarning()
	{
		BuildContext context = await RunAsync(
			[new NavItem { Label = "Home", Path = "/" }],
			Page("/guide/intro", "Intro"));

		context.Diagnostics.All.Should().ContainSingle().Which.Message.Should().Contain("'Home'");
	}

	[Fact]
	public async Task ConfiguredNav_ItemWithChildrenAndNoPath_IsNotWarned()
	{
		BuildContext context = await RunAsync(
			[new NavItem { Label = "Group", Children = [new NavItem { Label = "Intro", Path = "/guide/intro" }] }],
			Page("/guide/intro", "Intro"));

		context.Diagnostics.All.Should().BeEmpty();
	}

	private static string Shape(IEnumerable<NavigationNode> nodes) =>
		string.Join(", ", nodes.Select(n => n.Children.Count == 0 ? n.Label : $"{n.Label} [{Shape(n.Children)}]"));

	[Fact]
	public async Task AutoNav_FoldersBecomeNestedSections()
	{
		// Every page used to sit in one flat list under its first route segment.
		NavigationTree tree = await BuildAsync([],
			Page("/", "Home"),
			Page("/guide", "Guide"),
			Page("/guide/intro", "Intro"),
			Page("/guide/advanced", "Advanced Topics"),
			Page("/guide/advanced/caching", "Caching"),
			Page("/reference/cli", "CLI"));

		Shape(tree.Items).Should().Be("Guide [Advanced Topics [Caching], Intro], Home, Reference [CLI]");
		NavigationNode advanced = tree.Items[0].Children[0];
		advanced.Route.Should().Be("/guide/advanced");
		tree.Items[2].Route.Should().Be("/reference", "a folder without an index page links to its section redirect");
	}

	[Fact]
	public async Task AutoNav_FolderWithoutIndexPage_GetsAReadableName()
	{
		NavigationTree tree = await BuildAsync([], Page("/getting-started/install", "Install"));

		tree.Items.Single().Label.Should().Be("Getting started");
	}

	[Fact]
	public async Task AutoNav_OrdersByFrontMatterOrderThenTitle()
	{
		NavigationTree tree = await BuildAsync([],
			Page("/guide/zeta", "Zeta", order: 1),
			Page("/guide/alpha", "alpha", order: 2),
			Page("/guide/beta", "Beta", order: 1),
			Page("/guide/sub", "Sub", order: 0),
			Page("/guide/sub/page", "Page"),
			Page("/later", "Later", order: 5),
			Page("/earlier", "Earlier", order: -1));

		Shape(tree.Items).Should().Be("Earlier, Guide [Sub [Page], Beta, Zeta, alpha], Later");
	}

	[Fact]
	public async Task AutoNav_HiddenAndDraftPages_AreLeftOut()
	{
		DocPage hidden = Page("/guide/hidden", "Hidden") with
		{
			FrontMatter = new FrontMatter { Title = "Hidden", Visibility = PageVisibility.Hidden }
		};
		DocPage draft = Page("/guide/draft", "Draft") with
		{
			FrontMatter = new FrontMatter { Title = "Draft", Visibility = PageVisibility.Draft }
		};

		NavigationTree tree = await BuildAsync([], Page("/guide/intro", "Intro"), hidden, draft);

		Shape(tree.Items).Should().Be("Guide [Intro]");
	}

	[Fact]
	public async Task AutoNav_PagesBelowTheThirdLevel_AreListedUnderIt()
	{
		// The theme renders three levels, so /a/b/c/d disappeared from the sidebar.
		NavigationTree tree = await BuildAsync([],
			Page("/a/b/c", "C"),
			Page("/a/b/c/d", "D"),
			Page("/a/b/e/f/g", "G"),
			Page("/a/b/z", "Z"));

		Shape(tree.Items).Should().Be("A [B [C, C / D, E / F / G, Z]]");
		NavigationNode b = tree.Items[0].Children[0];
		b.Children.Select(c => c.Route).Should().Equal("/a/b/c", "/a/b/c/d", "/a/b/e/f/g", "/a/b/z");
	}

	private static (ApiReference Model, DocPage[] Pages) Api(params (string Namespace, string Type)[] types)
	{
		var model = new ApiReference
		{
			Namespaces = types
				.GroupBy(t => t.Namespace)
				.Select(g => new ApiNamespace
				{
					Name = g.Key,
					Types = g.Select(t => new ApiType
					{
						Name = t.Type,
						FullName = $"{t.Namespace}.{t.Type}",
						Kind = ApiTypeKind.Class,
						Namespace = t.Namespace
					}).ToList()
				})
				.ToList()
		};

		ApiTypeRoutes routes = ApiTypeRoutes.For(model.Namespaces.SelectMany(n => n.Types).ToList());
		DocPage[] pages =
		[
			Page("/api", "API Reference"),
			.. model.Namespaces.SelectMany(n => n.Types).Select(t => Page(routes.Route(t), routes.DisplayName(t)))
		];
		return (model, pages);
	}

	[Fact]
	public async Task AutoNav_ApiTypes_AreGroupedByNamespaceAndCollapsed()
	{
		// As routes, the namespace Contoso.Shapes.Round became the folders contoso, shapes and round.
		(ApiReference model, DocPage[] pages) = Api(
			("Contoso.Shapes", "Square"), ("Contoso.Shapes", "Circle"), ("Contoso", "Canvas"));

		BuildContext context = await RunAsync([], model, [.. pages, Page("/guide", "Guide")]);

		NavigationNode api = context.Navigation!.Items.Single(i => i.Route == "/api");
		Shape([api]).Should().Be("API Reference [Contoso [Canvas], Contoso.Shapes [Circle, Square]]");
		api.Children.Should().AllSatisfy(ns =>
		{
			ns.Route.Should().BeNull("a namespace has no page");
			ns.Expanded.Should().BeFalse();
		});
		api.Children[1].Children.Select(t => t.Route).Should().Equal("/api/contoso/shapes/circle", "/api/contoso/shapes/square");
	}

	[Fact]
	public async Task ConfiguredNav_AutoGenerate_ListsTheTreeBelowThePath()
	{
		// autoGenerate was parsed and read by nothing.
		(ApiReference model, DocPage[] pages) = Api(("Contoso", "Canvas"));
		List<NavItem> nav =
		[
			new() { Label = "Docs", Path = "/guide", AutoGenerate = true },
			new() { Label = "API", Path = "/api", AutoGenerate = true },
			new() { Label = "Plain API", Path = "/api" }
		];

		BuildContext context = await RunAsync(nav, model,
		[
			.. pages, Page("/guide", "Guide"), Page("/guide/intro", "Intro"), Page("/guide/deep", "Deep"),
			Page("/guide/deep/page", "Page")
		]);

		Shape(context.Navigation!.Items).Should().Be("Docs [Deep [Page], Intro], API [Contoso [Canvas]], Plain API");
		context.Diagnostics.All.Should().BeEmpty();
	}

	[Fact]
	public async Task ConfiguredNav_AutoGenerateAtTheSecondLevel_KeepsThreeLevels()
	{
		List<NavItem> nav =
		[
			new()
			{
				Label = "Docs",
				Children = [new NavItem { Label = "Guide", Path = "/guide", AutoGenerate = true }]
			}
		];

		NavigationTree tree = await BuildAsync(nav,
			Page("/guide", "Guide"), Page("/guide/intro", "Intro"), Page("/guide/deep", "Deep"),
			Page("/guide/deep/page", "Page"));

		Shape(tree.Items).Should().Be("Docs [Guide [Deep, Deep / Page, Intro]]");
	}

	[Fact]
	public async Task ConfiguredNav_AutoGenerateWithChildren_KeepsTheChildren()
	{
		List<NavItem> nav =
		[
			new()
			{
				Label = "Guide",
				Path = "/guide",
				AutoGenerate = true,
				Children = [new NavItem { Label = "Only", Path = "/guide/intro" }]
			}
		];

		NavigationTree tree = await BuildAsync(nav, Page("/guide", "Guide"), Page("/guide/intro", "Intro"),
			Page("/guide/other", "Other"));

		Shape(tree.Items).Should().Be("Guide [Only]");
	}
}
