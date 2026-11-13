using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     Environment variables are process-wide, so these tests must not run alongside tests
///     that read the same defaults.
/// </summary>
[CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
public sealed class EnvironmentVariableCollection;

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class SiteConfigEnvironmentTests
{
	private static SiteConfig ParseWith(string variable, string value, string yaml)
	{
		string? previous = Environment.GetEnvironmentVariable(variable);
		Environment.SetEnvironmentVariable(variable, value);
		try
		{
			return SiteConfigReader.Parse(yaml);
		}
		finally
		{
			Environment.SetEnvironmentVariable(variable, previous);
		}
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoThemeSection()
	{
		// Overrides used to be read only while mapping a section the yaml already contained.
		SiteConfig config = ParseWith("MOKADOCS_CODE_STYLE", "terminal", "site:\n  title: Docs\n");

		config.Theme.Options.CodeStyle.Should().Be("terminal");
	}

	[Fact]
	public void EnvironmentOverride_Style_WinsOverTheYaml()
	{
		SiteConfig config = ParseWith("MOKADOCS_STYLE", "classic",
			"site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n");

		config.Theme.Options.Style.Should().Be("classic");
	}

	[Fact]
	public void EnvironmentOverride_SiteImage_WinsOverTheYaml()
	{
		SiteConfig config = ParseWith("MOKADOCS_SITE_IMAGE", "https://cdn.example/env.png",
			"site:\n  title: Docs\n  image: assets/yaml.png\n");

		config.Site.Image!.PublishUrl.Should().Be("https://cdn.example/env.png");
	}

	[Fact]
	public void EnvironmentOverride_Llms_CanTurnItOff()
	{
		SiteConfig config = ParseWith("MOKADOCS_LLMS", "false", "site:\n  title: Docs\n");

		config.Build.Llms.Should().BeFalse();
	}

	[Fact]
	public void EnvironmentOverride_StyleSelector_CanHideIt()
	{
		SiteConfig config = ParseWith("MOKADOCS_SHOW_STYLE_SELECTOR", "false", "site:\n  title: Docs\n");

		config.Theme.Options.StyleSelector.Should().BeFalse();
	}

	[Theory]
	[InlineData("MOKADOCS_SHOW_TEXT_SIZE_SELECTOR")]
	[InlineData("MOKADOCS_SHOW_WIDTH_SELECTOR")]
	public void EnvironmentOverride_ReaderSelectors_WinOverTheYaml(string variable)
	{
		SiteConfig config = ParseWith(variable, "false",
			"site:\n  title: Docs\ntheme:\n  options:\n    textSizeSelector: true\n    widthSelector: true\n");

		bool selector = variable.Contains("TEXT_SIZE", StringComparison.Ordinal)
			? config.Theme.Options.TextSizeSelector
			: config.Theme.Options.WidthSelector;
		selector.Should().BeFalse();
	}

	[Theory]
	[InlineData("true", "layoutSelector: false", true)]
	[InlineData("false", "layoutSelector: true", false)]
	[InlineData("yes", "showSearch: true", true)]
	public void EnvironmentOverride_LayoutSelector_WinsOverTheYaml(string value, string option, bool expected)
	{
		SiteConfig config = ParseWith("MOKADOCS_SHOW_LAYOUT_SELECTOR", value,
			$"site:\n  title: Docs\ntheme:\n  options:\n    {option}\n");

		config.Theme.Options.LayoutSelector.Should().Be(expected);
	}

	[Fact]
	public void EnvironmentOverride_ImageZoom_WinsOverTheYaml()
	{
		SiteConfig config = ParseWith("MOKADOCS_IMAGE_ZOOM", "false",
			"site:\n  title: Docs\ntheme:\n  options:\n    imageZoom: true\n");

		config.Theme.Options.ImageZoom.Should().BeFalse();
	}

	private static Dictionary<string, string> LayoutParts(ThemeLayoutOptions layout) => new()
	{
		["WIDTH"] = layout.Width,
		["NAVBAR"] = layout.Navbar,
		["FOOTER"] = layout.Footer,
		["TOC"] = layout.Toc,
		["BREADCRUMBS"] = layout.Breadcrumbs,
		["PAGE_NAV"] = layout.PageNav,
		["SIDEBAR"] = layout.Sidebar,
		["SIDEBAR_SCOPE"] = layout.SidebarScope,
		["HOME"] = layout.Home,
		["HERO"] = layout.Hero,
		["HERO_BACKGROUND"] = layout.HeroBackground,
		["FEATURES"] = layout.Features,
		["ADMONITIONS"] = layout.Admonitions,
		["TABLES"] = layout.Tables,
		["DENSITY"] = layout.Density,
		["FONT"] = layout.Font,
		["HEADINGS"] = layout.Headings,
		["LINKS"] = layout.Links
	};

	[Theory]
	[InlineData("WIDTH", "full")]
	[InlineData("NAVBAR", "tabs")]
	[InlineData("FOOTER", "columns")]
	[InlineData("TOC", "floating")]
	[InlineData("BREADCRUMBS", "pills")]
	[InlineData("PAGE_NAV", "stacked")]
	[InlineData("SIDEBAR", "pills")]
	[InlineData("SIDEBAR_SCOPE", "section")]
	[InlineData("HOME", "portal")]
	[InlineData("HERO", "split")]
	[InlineData("HERO_BACKGROUND", "dots")]
	[InlineData("FEATURES", "bento")]
	[InlineData("ADMONITIONS", "github")]
	[InlineData("TABLES", "striped")]
	[InlineData("DENSITY", "compact")]
	[InlineData("FONT", "serif")]
	[InlineData("HEADINGS", "numbered")]
	[InlineData("LINKS", "subtle")]
	public void EnvironmentOverride_LayoutPart_AppliesWhenTheYamlHasNoThemeSection(string part, string value)
	{
		SiteConfig config = ParseWith("MOKADOCS_LAYOUT_" + part, value, "site:\n  title: Docs\n");

		Dictionary<string, string> parts = LayoutParts(config.Theme.Options.Layout);
		parts[part].Should().Be(value);
		parts.Where(p => p.Key != part).Select(p => p.Value).Should().AllBe("default");
	}

	[Fact]
	public void EnvironmentOverride_LayoutPart_AppliesWhenTheOptionsHaveNoLayoutSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_LAYOUT_NAVBAR", "autohide",
			"site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n");

		config.Theme.Options.Layout.Navbar.Should().Be("autohide");
		config.Theme.Options.Style.Should().Be("moka");
	}

	[Fact]
	public void EnvironmentOverride_LayoutPart_WinsOverTheYamlAndLeavesTheOtherParts()
	{
		SiteConfig config = ParseWith("MOKADOCS_LAYOUT_NAVBAR", "compact",
			"site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      navbar: tabs\n      footer: minimal\n");

		config.Theme.Options.Layout.Navbar.Should().Be("compact");
		config.Theme.Options.Layout.Footer.Should().Be("minimal");
	}

	[Fact]
	public void EnvironmentOverride_SidebarToggle_WinsOverTheYaml()
	{
		SiteConfig config = ParseWith("MOKADOCS_SIDEBAR_TOGGLE", "true",
			"site:\n  title: Docs\ntheme:\n  options:\n    sidebarToggle: false\n");

		config.Theme.Options.SidebarToggle.Should().BeTrue();
	}

	[Fact]
	public void EnvironmentOverride_LayoutPart_KeepsAnUnknownNameForTheWarning()
	{
		SiteConfig config = ParseWith("MOKADOCS_LAYOUT_TOC", "Sidebar", "site:\n  title: Docs\n");

		config.Theme.Options.Layout.Toc.Should().Be("Sidebar");
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoBuildSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_GENERATE_ROBOTS", "false", "site:\n  title: Docs\n");

		config.Build.Robots.Should().BeFalse();
	}

	[Theory]
	[InlineData("true", "build:\n  minify: false\n", true)]
	[InlineData("false", "build:\n  minify: true\n", false)]
	[InlineData("1", "", true)]
	public void EnvironmentOverride_MinifyOutput_WinsOverTheYaml(string value, string build, bool expected)
	{
		SiteConfig config = ParseWith("MOKADOCS_MINIFY_OUTPUT", value, "site:\n  title: Docs\n" + build);

		config.Build.Minify.Should().Be(expected);
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoSearchSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_SEARCH_ENABLED", "false", "site:\n  title: Docs\nfeatures: {}\n");

		config.Features.Search.Enabled.Should().BeFalse();
	}
}
