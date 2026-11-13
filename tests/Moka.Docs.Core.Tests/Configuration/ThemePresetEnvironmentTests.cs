using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     <c>MOKADOCS_THEME_PRESET</c>, <c>MOKADOCS_BANNER_TEXT</c> and <c>MOKADOCS_BANNER_LINK</c>. They
///     share the non-parallel collection of the other environment variable tests.
/// </summary>
[Collection(nameof(EnvironmentVariableCollection))]
public sealed class ThemePresetEnvironmentTests
{
	private static SiteConfig ParseWith(string yaml, params (string Name, string Value)[] variables)
	{
		string?[] previous = variables.Select(v => Environment.GetEnvironmentVariable(v.Name)).ToArray();
		foreach ((string name, string value) in variables)
		{
			Environment.SetEnvironmentVariable(name, value);
		}

		try
		{
			return SiteConfigReader.Parse(yaml);
		}
		finally
		{
			for (int i = 0; i < variables.Length; i++)
			{
				Environment.SetEnvironmentVariable(variables[i].Name, previous[i]);
			}
		}
	}

	[Fact]
	public void Preset_FromTheEnvironment_AppliesWithoutAThemeSection()
	{
		ThemeOptions options = ParseWith("site:\n  title: Docs\n", ("MOKADOCS_THEME_PRESET", "book")).Theme.Options;

		options.Preset.Should().Be("book");
		options.Style.Should().Be("paper");
		options.Layout.Toc.Should().Be("inline");
	}

	[Fact]
	public void Preset_FromTheEnvironment_WinsOverTheYamlPreset_ButNotOverTheYamlOptions()
	{
		ThemeOptions options = ParseWith(
			"site:\n  title: Docs\ntheme:\n  options:\n    preset: book\n    layout:\n      footer: none\n",
			("MOKADOCS_THEME_PRESET", "reference")).Theme.Options;

		options.Preset.Should().Be("reference");
		options.Style.Should().Be("classic");
		options.Layout.Toc.Should().Be("default", "book's inline contents are not applied");
		options.Layout.Footer.Should().Be("none", "the yaml sets it");
	}

	[Fact]
	public void OptionVariables_WinOverThePreset()
	{
		ThemeOptions options = ParseWith("site:\n  title: Docs\ntheme:\n  options:\n    preset: terminal\n",
			("MOKADOCS_STYLE", "glass"), ("MOKADOCS_LAYOUT_NAVBAR", "tabs")).Theme.Options;

		options.Style.Should().Be("glass");
		options.Layout.Navbar.Should().Be("tabs");
		options.CodeTheme.Should().Be("tokyo-night");
	}

	[Fact]
	public void BannerVariables_ShowABannerWithoutABannerBlock()
	{
		ThemeBanner banner = ParseWith("site:\n  title: Docs\n",
			("MOKADOCS_BANNER_TEXT", "Release 2.0 today"), ("MOKADOCS_BANNER_LINK", "/blog/2-0")).Theme.Options.Banner!;

		banner.Text.Should().Be("Release 2.0 today");
		banner.Link.Should().Be("/blog/2-0");
		banner.Dismissible.Should().BeTrue();
	}

	[Fact]
	public void BannerVariables_WinOverTheYaml_AndKeepItsOtherKeys()
	{
		ThemeBanner banner = ParseWith(
			"site:\n  title: Docs\ntheme:\n  options:\n    banner:\n      text: Old\n      link: /old\n      variant: warning\n",
			("MOKADOCS_BANNER_TEXT", "New"), ("MOKADOCS_BANNER_LINK", "https://example.com/new")).Theme.Options.Banner!;

		banner.Text.Should().Be("New");
		banner.Link.Should().Be("https://example.com/new");
		banner.Variant.Should().Be("warning");
	}
}
