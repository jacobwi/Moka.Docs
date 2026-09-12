using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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

	private static async Task<BuildContext> RunAsync(List<NavItem> nav, params DocPage[] pages)
	{
		var context = new BuildContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "Nav" }, Nav = nav },
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
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
		]);

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
}
