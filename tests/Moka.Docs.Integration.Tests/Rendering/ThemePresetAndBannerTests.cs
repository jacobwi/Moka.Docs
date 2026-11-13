using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     <c>theme.options.preset</c> and <c>theme.options.banner</c> from mokadocs.yaml through the whole
///     pipeline.
/// </summary>
public sealed class ThemePresetAndBannerTests
{
	private const string _page = MockSite.Root + "/_site/guide/intro/index.html";
	private const string _landing = MockSite.Root + "/_site/index.html";
	private const string _notFound = MockSite.Root + "/_site/404.html";

	private static Dictionary<string, MockFileData> Files() => new()
	{
		{ MockSite.Root + "/docs/index.md", new MockFileData("---\ntitle: Home\nlayout: landing\n---\n# Home\n") },
		{ MockSite.Root + "/docs/guide/intro.md", new MockFileData("---\ntitle: Intro\n---\n# Intro\n\n## Setup\n\nText.\n") }
	};

	private static async Task<(BuildContext Context, MockFileSystem FileSystem)> BuildAsync(string themeOptions, string basePath = "/", bool dryRun = false)
	{
		SiteConfig parsed = SiteConfigReader.Parse("site:\n  title: Test Site\ntheme:\n  options:\n" + themeOptions);
		SiteConfig config = MockSite.Config() with { Theme = parsed.Theme, Build = new BuildConfig { BasePath = basePath } };
		return await MockSite.BuildAsync(config, Files(), dryRun);
	}

	private static IEnumerable<Diagnostic> RenderDiagnostics(BuildContext context) =>
		context.Diagnostics.All.Where(d => d.Source == "Render");

	[Fact]
	public async Task Preset_ShapesEveryPage()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync("    preset: reference\n    layout:\n      footer: minimal\n");

		foreach (string file in new[] { _page, _landing })
		{
			string html = fs.File.ReadAllText(file);
			html.Should().Contain("data-style=\"classic\"").And.Contain("data-navbar=\"tabs\"").And.Contain("data-breadcrumbs=\"chevrons\"");
			html.Should().Contain("data-footer=\"minimal\"", "the yaml's footer wins over the preset's columns");
		}

		RenderDiagnostics(context).Should().BeEmpty();
	}

	[Fact]
	public async Task UnknownPreset_IsOneWarningAndAppliesNothing()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync("    preset: fancy\n");

		Diagnostic warning = RenderDiagnostics(context).Should().ContainSingle().Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Be(
			"Theme preset 'fancy' is not one of reference, terminal, book, product, minimal, accessible, moka; no preset applied");
		fs.File.ReadAllText(_page).Should().Contain("data-style=\"default\"").And.NotContain("data-navbar=");
	}

	[Fact]
	public async Task UnknownPreset_IsReportedForACustomThemeToo()
	{
		SiteConfig parsed = SiteConfigReader.Parse("site:\n  title: Test Site\ntheme:\n  name: ./mytheme\n  options:\n    preset: fancy\n");
		Dictionary<string, MockFileData> files = Files();
		files[MockSite.Root + "/mytheme/layouts/default.html"] = new MockFileData("<html><body>{{ page.content }}</body></html>");
		files[MockSite.Root + "/mytheme/layouts/landing.html"] = new MockFileData("<html><body>{{ page.content }}</body></html>");

		(BuildContext context, _) = await MockSite.BuildAsync(MockSite.Config() with { Theme = parsed.Theme }, files, dryRun: true);

		RenderDiagnostics(context).Select(d => d.Message).Should().ContainSingle(m => m.StartsWith("Theme preset 'fancy'", StringComparison.Ordinal))
			.And.NotContain(m => m.Contains("was not found", StringComparison.Ordinal), "the custom theme is the one rendering");
	}

	[Fact]
	public async Task Banner_IsOnEveryLayout_WithTheBasePathOnARootLink()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync(
			"    banner:\n      text: Version 2 is out\n      link: /guide/intro\n      variant: warning\n", "/docs");

		foreach (string file in new[] { _page, _landing, _notFound })
		{
			string html = fs.File.ReadAllText(file);
			html.Should().Contain("<div class=\"site-banner\" id=\"mokadocs-banner\" role=\"region\" aria-label=\"Announcement\" data-variant=\"warning\"", file);
			html.Should().Contain("<a class=\"site-banner-link\" href=\"/docs/guide/intro\">Learn more", file);
		}

		RenderDiagnostics(context).Should().BeEmpty();
	}

	[Fact]
	public async Task UnknownBannerVariant_IsAWarningAndRendersAsInfo()
	{
		(BuildContext context, MockFileSystem fs) = await BuildAsync("    banner:\n      text: Hi\n      variant: loud\n");

		Diagnostic warning = RenderDiagnostics(context).Should().ContainSingle().Subject;
		warning.Severity.Should().Be(DiagnosticSeverity.Warning);
		warning.Message.Should().Be("Banner variant 'loud' is not one of info, warning, accent; using 'info'");
		fs.File.ReadAllText(_page).Should().Contain("data-variant=\"info\"");
	}

	[Fact]
	public async Task NoBanner_LeavesThePagesWithoutOne()
	{
		(_, MockFileSystem fs) = await BuildAsync("    style: moka\n");

		fs.File.ReadAllText(_page).Should().NotContain("site-banner");
		fs.File.ReadAllText(_landing).Should().NotContain("site-banner");
	}
}
