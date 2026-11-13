using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     <c>MokaDocsOptions.Preset</c> and <c>MokaDocsOptions.Banner</c>, the host's counterparts of
///     <c>theme.options.preset</c> and <c>theme.options.banner</c>.
/// </summary>
public sealed class AspNetCorePresetAndBannerTests
{
	private static async Task<InMemorySite> BuildAsync(Action<MokaDocsOptions> configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			configure(options);
		});

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public void NoPreset_AndNoBanner_ByDefault()
	{
		var options = new MokaDocsOptions();
		ThemeOptions theme = SiteConfigFactory.Create(options).Theme.Options;

		options.Preset.Should().BeNull();
		options.Banner.Should().BeNull();
		theme.Preset.Should().BeEmpty();
		theme.Banner.Should().BeNull();
		theme.Style.Should().Be(ThemeStyles.Default);
	}

	[Fact]
	public void Preset_FillsTheOptionsStillAtTheirDefault()
	{
		ThemeOptions theme = SiteConfigFactory.Create(new MokaDocsOptions { Preset = "Terminal" }).Theme.Options;

		theme.Preset.Should().Be("Terminal");
		theme.Style.Should().Be("terminal");
		theme.DefaultColorTheme.Should().Be("moka-red", "options the host has no property for come from the preset too");
		theme.CodeTheme.Should().Be("tokyo-night");
		theme.Layout.Navbar.Should().Be("compact");
		theme.Layout.Breadcrumbs.Should().Be("path");
	}

	[Fact]
	public void Preset_LeavesOptionsTheAppChanged()
	{
		var options = new MokaDocsOptions { Preset = "reference", Style = "glass" };
		options.Layout.Navbar = "floating";

		ThemeOptions theme = SiteConfigFactory.Create(options).Theme.Options;

		theme.Style.Should().Be("glass");
		theme.Layout.Navbar.Should().Be("floating");
		theme.Layout.Footer.Should().Be("columns");
		options.Layout.Footer.Should().Be("default", "the app's own options object is left alone");
	}

	[Fact]
	public async Task UnknownPreset_IsReportedByTheHostBuild()
	{
		InMemorySite site = await BuildAsync(options => options.Preset = "fancy");

		site.Diagnostics.Should().ContainSingle(d => d.Source == "Render")
			.Which.Message.Should().StartWith("Theme preset 'fancy' is not one of reference, terminal");
	}

	[Fact]
	public void Banner_ReachesTheSiteConfig()
	{
		var options = new MokaDocsOptions
		{
			Banner = new MokaDocsBannerOptions
			{
				Text = " Version 2 is out ",
				Link = "/docs/blog",
				LinkText = "Read more",
				Id = "v2",
				Dismissible = false,
				Variant = "accent"
			}
		};

		SiteConfigFactory.Create(options).Theme.Options.Banner.Should().Be(new ThemeBanner
		{
			Text = "Version 2 is out",
			Link = "/docs/blog",
			LinkText = "Read more",
			Id = "v2",
			Dismissible = false,
			Variant = "accent"
		});
	}

	[Fact]
	public void Banner_WithoutText_IsNone()
	{
		SiteConfigFactory.Create(new MokaDocsOptions { Banner = new MokaDocsBannerOptions { Link = "/x" } })
			.Theme.Options.Banner.Should().BeNull();
	}

	[Fact]
	public async Task Banner_IsOnTheHostsPages()
	{
		InMemorySite site = await BuildAsync(options =>
			options.Banner = new MokaDocsBannerOptions { Text = "Version 2 is out", Link = "/api" });

		string html = Encoding.UTF8.GetString(site.Files["api/index.html"].Content);
		html.Should().Contain("<div class=\"site-banner\" id=\"mokadocs-banner\"");
		html.Should().Contain("href=\"/docs/api\"", "a link from the site root gets the base path");
	}
}
