using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     A <c>theme.options.layout</c> name the default theme doesn't have renders that part as
///     <c>default</c>, so the build says so instead of looking like the setting was ignored.
/// </summary>
public sealed class LayoutVariantWarningTests
{
	private static Dictionary<string, MockFileData> Files() => new()
	{
		{ "/project/docs/index.md", new MockFileData("---\ntitle: Home\n---\n# Home\n") },
		{ "/project/docs/guide/intro.md", new MockFileData("---\ntitle: Intro\n---\n# Intro\n\n## Setup\n\nText.\n") },
		{ "/project/docs/landing.md", new MockFileData("---\ntitle: Landing\nlayout: landing\n---\n# Landing\n") }
	};

	private static SiteConfig Config(ThemeLayoutOptions layout, string theme = "default") =>
		MockSite.Config() with
		{
			Theme = new ThemeConfig { Name = theme, Options = new ThemeOptions { Layout = layout } }
		};

	private static IEnumerable<Diagnostic> RenderDiagnostics(BuildContext context) =>
		context.Diagnostics.All.Where(d => d.Source == "Render");

	[Fact]
	public async Task UnknownVariant_IsOneWarningNamingThePartAndItsVariants()
	{
		(BuildContext context, _) = await MockSite.BuildAsync(
			Config(new ThemeLayoutOptions { Navbar = "tbs" }), Files(), dryRun: true);

		Diagnostic warning = RenderDiagnostics(context).Should().ContainSingle("three pages render, one warning").Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Be(
			"Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide, centered, transparent; using 'default'");
	}

	[Fact]
	public async Task EveryUnknownPart_IsReportedAsAWarningNeverAnError()
	{
		var layout = new ThemeLayoutOptions
		{
			Width = "huge",
			Navbar = "tbs",
			Footer = "fat",
			Toc = "side",
			Breadcrumbs = "slashes",
			PageNav = "cards",
			Sidebar = "tree",
			SidebarScope = "page",
			Home = "landing",
			Hero = "big",
			HeroBackground = "stripes",
			Features = "grid",
			Admonitions = "boxed",
			Tables = "zebra",
			Density = "cozy",
			Font = "comic",
			Headings = "boxed",
			Links = "bold"
		};

		(BuildContext context, _) = await MockSite.BuildAsync(Config(layout), Files(), dryRun: true);

		RenderDiagnostics(context).Select(d => d.Message).Should().Equal(
			"Layout width 'huge' is not one of default, wide, wider, full, boxed, narrow; using 'default'",
			"Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide, centered, transparent; using 'default'",
			"Layout footer 'fat' is not one of default, minimal, columns, none, centered, compact; using 'default'",
			"Layout toc 'side' is not one of default, minimal, inline, floating, numbered, timeline, bordered; using 'default'",
			"Layout breadcrumbs 'slashes' is not one of default, chevrons, pills, path, back, arrows, dots; using 'default'",
			"Layout pageNav 'cards' is not one of default, minimal, stacked, split, compact; using 'default'",
			"Layout sidebar 'tree' is not one of default, lines, pills, compact, accent, cards, minimal; using 'default'",
			"Layout sidebarScope 'page' is not one of default, section; using 'default'",
			"Layout home 'landing' is not one of default, portal, showcase, minimal, docs; using 'default'",
			"Layout hero 'big' is not one of default, split, minimal, banner; using 'default'",
			"Layout heroBackground 'stripes' is not one of default, dots, gradient, none, mesh, lines; using 'default'",
			"Layout features 'grid' is not one of default, list, icons, compact, bento; using 'default'",
			"Layout admonitions 'boxed' is not one of default, filled, minimal, github, outlined, callout; using 'default'",
			"Layout tables 'zebra' is not one of default, striped, bordered, minimal, compact, rounded; using 'default'",
			"Layout density 'cozy' is not one of default, compact, spacious; using 'default'",
			"Layout font 'comic' is not one of default, system, serif, mono, rounded, humanist; using 'default'",
			"Layout headings 'boxed' is not one of default, underlined, accent, numbered; using 'default'",
			"Layout links 'bold' is not one of default, underlined, subtle; using 'default'");
		context.Diagnostics.All.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public async Task KnownVariants_InAnyCase_AreNotReported()
	{
		var layout = new ThemeLayoutOptions
		{
			Width = "Wide",
			Navbar = " TABS ",
			Footer = "none",
			Toc = "Floating",
			Breadcrumbs = "back",
			PageNav = "stacked",
			Sidebar = "Pills",
			SidebarScope = "section",
			Home = " Portal ",
			Hero = " split ",
			HeroBackground = "NONE",
			Features = "BENTO",
			Admonitions = "github",
			Tables = "minimal",
			Density = "spacious",
			Font = "Serif",
			Headings = " Numbered ",
			Links = "SUBTLE"
		};

		(BuildContext context, _) = await MockSite.BuildAsync(Config(layout), Files(), dryRun: true);

		RenderDiagnostics(context).Should().BeEmpty();
	}

	[Fact]
	public async Task UnknownVariant_WithACustomTheme_IsNotReported()
	{
		Dictionary<string, MockFileData> files = Files();
		files["/project/mytheme/layouts/default.html"] = new MockFileData("<html><body>{{ page.content }}</body></html>");
		files["/project/mytheme/layouts/landing.html"] = new MockFileData("<html><body>{{ page.content }}</body></html>");

		(BuildContext context, _) = await MockSite.BuildAsync(
			Config(new ThemeLayoutOptions { Navbar = "tbs", Footer = "brand" }, "mytheme"), files, dryRun: true);

		context.Diagnostics.All.Should().NotContain(d => d.Message.Contains("'tbs'") || d.Message.Contains("'brand'"),
			"a custom theme may have its own variant names");
	}

	[Fact]
	public async Task UnknownVariantFromTheYaml_IsReported()
	{
		SiteConfig config = SiteConfigReader.Parse("""
		                                           site:
		                                             title: Docs
		                                           theme:
		                                             options:
		                                               layout:
		                                                 breadcrumbs: crumbs
		                                           """);

		(BuildContext context, _) = await MockSite.BuildAsync(config, Files(), dryRun: true);

		RenderDiagnostics(context).Should().ContainSingle()
			.Which.Message.Should().StartWith("Layout breadcrumbs 'crumbs' is not one of ");
	}
}
