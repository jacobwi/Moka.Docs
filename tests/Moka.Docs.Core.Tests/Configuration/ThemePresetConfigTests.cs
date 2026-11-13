using System.Collections.ObjectModel;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     <c>theme.options.preset</c>: the preset fills in the options mokadocs.yaml leaves out, and what
///     the yaml sets wins.
/// </summary>
public sealed class ThemePresetConfigTests
{
	private static ThemeOptions Parse(string options) =>
		SiteConfigReader.Parse("site:\n  title: Docs\ntheme:\n  options:\n" + options).Theme.Options;

	private static ThemePresetInfo Preset(params (string Key, string Value)[] options) => new()
	{
		Name = "test",
		Label = "Test",
		Description = "A preset for the tests.",
		Options = new ReadOnlyDictionary<string, string>(options.ToDictionary(o => o.Key, o => o.Value))
	};

	[Fact]
	public void Preset_FillsTheOptionsTheYamlLeavesOut()
	{
		ThemeOptions options = Parse("    preset: reference\n");

		options.Preset.Should().Be("reference");
		options.Style.Should().Be("classic");
		options.Layout.Navbar.Should().Be("tabs");
		options.Layout.Footer.Should().Be("columns");
		options.Layout.Breadcrumbs.Should().Be("chevrons");
		options.Layout.Width.Should().Be("default", "reference doesn't set the width");
	}

	[Fact]
	public void Preset_AppliesAppearanceDefaultsBesideTheStyle()
	{
		ThemeOptions options = Parse("    preset: terminal\n");

		options.Style.Should().Be("terminal");
		options.DefaultColorTheme.Should().Be("moka-red");
		options.CodeTheme.Should().Be("tokyo-night");
		options.Layout.PageNav.Should().Be("minimal");
	}

	[Fact]
	public void YamlValues_WinOverThePreset()
	{
		ThemeOptions options = Parse("    preset: terminal\n    style: soft\n    codeTheme: nord\n    defaultColorTheme: \"\"\n");

		options.Style.Should().Be("soft");
		options.CodeTheme.Should().Be("nord");
		options.DefaultColorTheme.Should().BeEmpty("an empty value is still set");
		options.Layout.Navbar.Should().Be("compact", "the yaml leaves it out");
	}

	[Fact]
	public void NestedLayoutKeys_MergeOneByOne()
	{
		// The yaml's layout block holds some parts; the preset fills the others instead of replacing it.
		ThemeOptions options = Parse("    preset: reference\n    layout:\n      navbar: floating\n      width: wide\n");

		options.Layout.Navbar.Should().Be("floating");
		options.Layout.Width.Should().Be("wide");
		options.Layout.Footer.Should().Be("columns");
		options.Layout.Breadcrumbs.Should().Be("chevrons");
	}

	[Fact]
	public void Lists_InTheYaml_AreKeptAsWritten()
	{
		ThemeOptions options = Parse(
			"    preset: moka\n    socialLinks:\n      - icon: github\n        url: https://github.com/x\n      - icon: nuget\n        url: https://nuget.org/x\n");

		options.Style.Should().Be("moka");
		options.SocialLinks.Select(s => s.Icon).Should().Equal("github", "nuget");
	}

	[Fact]
	public void PresetName_IgnoresCase_AndIsKeptAsWritten()
	{
		ThemeOptions options = Parse("    preset: \" Book \"\n");

		options.Preset.Should().Be(" Book ");
		options.Style.Should().Be("paper");
		options.Layout.Toc.Should().Be("inline");
	}

	[Fact]
	public void UnknownPreset_AppliesNothing_AndKeepsTheName()
	{
		ThemeOptions options = Parse("    preset: fancy\n");

		options.Preset.Should().Be("fancy");
		options.Style.Should().Be(ThemeStyles.Default);
		options.Layout.Should().Be(new ThemeLayoutOptions());
		options.CodeTheme.Should().Be(new ThemeOptions().CodeTheme);
	}

	[Fact]
	public void NoPreset_ChangesNothing()
	{
		ThemeOptions options = Parse("    style: moka\n");

		options.Preset.Should().BeEmpty();
		options.Should().BeEquivalentTo(new ThemeOptions { Style = "moka" });
	}

	[Fact]
	public void PresetAndBanner_AreKnownKeys()
	{
		SiteConfig config = SiteConfigReader.Parse(
			"site:\n  title: Docs\ntheme:\n  options:\n    preset: moka\n    banner:\n      text: Hi\n      link: /x\n      linkText: Go\n      id: a\n      dismissible: false\n      variant: accent\n");

		config.Warnings.Should().BeEmpty();
	}

	[Fact]
	public void Preset_RoundTripsThroughToYaml()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: Docs\ntheme:\n  options:\n    preset: product\n    style: brutal\n");
		string yaml = SiteConfigReader.ToYaml(config);

		yaml.Should().Contain("preset: product");
		SiteConfigReader.Parse(yaml).Theme.Options.Should().BeEquivalentTo(config.Theme.Options);
		SiteConfigReader.ToYaml(SiteConfigReader.Parse("site:\n  title: Docs\n")).Should().NotContain("preset");
	}

	[Fact]
	public void FillMissing_ConvertsBoolAndIntValues_AndSkipsKeysWithNoOption()
	{
		var dto = new ThemeOptionsDto { ShowSearch = true };
		ThemePresetMerge.FillMissing(dto, Preset(
			("showSearch", "false"), ("showBreadcrumbs", "false"), ("tocDepth", "4"), ("layout.width", "full"), ("layout.nope", "x"), ("nope", "x")));

		dto.ShowSearch.Should().BeTrue("the yaml set it");
		dto.ShowBreadcrumbs.Should().BeFalse();
		dto.TocDepth.Should().Be(4);
		dto.Layout!.Width.Should().Be("full");
	}

	[Theory]
	[InlineData("style", true)]
	[InlineData("layout.navbar", true)]
	[InlineData("layout.pageNav", true)]
	[InlineData("showSearch", true)]
	[InlineData("tocDepth", true)]
	[InlineData("banner.text", true)]
	[InlineData("layout", false)]
	[InlineData("socialLinks", false)]
	[InlineData("Style", false)]
	[InlineData("layout.nope", false)]
	[InlineData("style.name", false)]
	[InlineData("nope", false)]
	public void IsOption_AcceptsPathsToScalarOptionsOnly(string key, bool expected)
	{
		ThemePresetMerge.IsOption(key).Should().Be(expected);
	}

	[Fact]
	public void FillDefaults_SetsOnlyOptionsStillAtTheirDefault_AndLeavesTheInputAlone()
	{
		// The host's options have no "unset": a value equal to the default counts as not set.
		var options = new ThemeOptions
		{
			Style = "soft",
			Layout = new ThemeLayoutOptions { Navbar = "compact" },
			Sidebar = "shared"
		};

		ThemeOptions filled = ThemePresetMerge.FillDefaults(options, ThemePresets.Find("reference")!);

		filled.Style.Should().Be("soft");
		filled.Layout.Navbar.Should().Be("compact");
		filled.Layout.Footer.Should().Be("columns");
		filled.Layout.Breadcrumbs.Should().Be("chevrons");
		filled.Sidebar.Should().Be("shared");
		filled.Should().NotBeSameAs(options);
		options.Layout.Should().Be(new ThemeLayoutOptions { Navbar = "compact" });
	}

	[Fact]
	public void FillDefaults_ConvertsTypedValues_AndKeepsTheOnesThatWereSet()
	{
		ThemeOptions filled = ThemePresetMerge.FillDefaults(new ThemeOptions { ShowSearch = false },
			Preset(("showSearch", "true"), ("showBreadcrumbs", "false"), ("tocDepth", "5"), ("nope", "x")));

		filled.ShowSearch.Should().BeFalse("it differs from its default, so it was set");
		filled.ShowBreadcrumbs.Should().BeFalse();
		filled.TocDepth.Should().Be(5);
	}
}
