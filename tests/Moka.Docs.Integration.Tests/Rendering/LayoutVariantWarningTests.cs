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
		warning.Message.Should().Be("Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide; using 'default'");
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
			Breadcrumbs = "arrows",
			PageNav = "cards"
		};

		(BuildContext context, _) = await MockSite.BuildAsync(Config(layout), Files(), dryRun: true);

		RenderDiagnostics(context).Select(d => d.Message).Should().Equal(
			"Layout width 'huge' is not one of default, wide, full, boxed; using 'default'",
			"Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide; using 'default'",
			"Layout footer 'fat' is not one of default, minimal, columns, none; using 'default'",
			"Layout toc 'side' is not one of default, minimal, inline, floating; using 'default'",
			"Layout breadcrumbs 'arrows' is not one of default, chevrons, pills, path, back; using 'default'",
			"Layout pageNav 'cards' is not one of default, minimal, stacked; using 'default'");
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
			PageNav = "stacked"
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
