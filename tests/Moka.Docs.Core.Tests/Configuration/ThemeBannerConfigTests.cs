using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary><c>theme.options.banner</c>, the announcement above the header.</summary>
public sealed class ThemeBannerConfigTests
{
	private static ThemeBanner? Parse(string banner) =>
		SiteConfigReader.Parse("site:\n  title: Docs\ntheme:\n  options:\n    banner:\n" + banner).Theme.Options.Banner;

	[Fact]
	public void NoBanner_IsNull()
	{
		SiteConfigReader.Parse("site:\n  title: Docs\n").Theme.Options.Banner.Should().BeNull();
	}

	[Fact]
	public void Banner_ReadsEveryKey()
	{
		ThemeBanner banner = Parse(
			"      text: \" Version 2 is out \"\n      link: /blog/v2\n      linkText: Read the post\n      id: v2\n      dismissible: false\n      variant: accent\n")!;

		banner.Should().BeEquivalentTo(new ThemeBanner
		{
			Text = "Version 2 is out",
			Link = "/blog/v2",
			LinkText = "Read the post",
			Id = "v2",
			Dismissible = false,
			Variant = "accent"
		});
		banner.EffectiveId.Should().Be("v2");
	}

	[Fact]
	public void Banner_Defaults_AreDismissibleInfoWithADerivedId()
	{
		ThemeBanner banner = Parse("      text: Version 2 is out\n")!;

		banner.Link.Should().BeNull();
		banner.LinkText.Should().BeNull();
		banner.Id.Should().BeNull();
		banner.Dismissible.Should().BeTrue();
		banner.Variant.Should().Be(BannerVariants.Info);
		banner.EffectiveId.Should().MatchRegex("^[0-9a-f]{12}$");
	}

	[Theory]
	[InlineData("      link: /blog\n")]
	[InlineData("      text: \"   \"\n")]
	[InlineData("      text: \"\"\n      variant: warning\n")]
	public void Banner_WithoutText_IsNull(string yaml)
	{
		Parse(yaml).Should().BeNull();
	}

	[Fact]
	public void UnknownVariant_IsKeptAsWritten_ForTheRenderPhaseWarning()
	{
		Parse("      text: Hi\n      variant: Loud\n")!.Variant.Should().Be("Loud");
	}

	[Fact]
	public void EffectiveId_ChangesWithTheTextOrTheLink_SoTheBannerShowsAgain()
	{
		var first = new ThemeBanner { Text = "Version 2 is out", Link = "/blog/v2" };

		first.EffectiveId.Should().Be(new ThemeBanner { Text = "Version 2 is out", Link = "/blog/v2" }.EffectiveId);
		first.EffectiveId.Should().NotBe((first with { Text = "Version 3 is out" }).EffectiveId);
		first.EffectiveId.Should().NotBe((first with { Link = "/blog/v3" }).EffectiveId);
		(first with { Id = "  launch " }).EffectiveId.Should().Be("launch");
		(first with { Id = " " }).EffectiveId.Should().Be(first.EffectiveId);
	}

	[Fact]
	public void Banner_RoundTripsThroughToYaml_WritingOnlyWhatDiffersFromTheDefaults()
	{
		SiteConfig withDefaults = SiteConfigReader.Parse("site:\n  title: Docs\ntheme:\n  options:\n    banner:\n      text: Hi\n");
		SiteConfig full = SiteConfigReader.Parse(
			"site:\n  title: Docs\ntheme:\n  options:\n    banner:\n      text: Hi\n      link: https://x.example\n      linkText: Go\n      id: a\n      dismissible: false\n      variant: warning\n");

		string yaml = SiteConfigReader.ToYaml(withDefaults);
		yaml.Should().Contain("banner:").And.Contain("text: Hi").And.NotContain("dismissible").And.NotContain("variant");
		SiteConfigReader.Parse(yaml).Theme.Options.Banner.Should().Be(withDefaults.Theme.Options.Banner);
		SiteConfigReader.Parse(SiteConfigReader.ToYaml(full)).Theme.Options.Banner.Should().Be(full.Theme.Options.Banner);
		SiteConfigReader.ToYaml(SiteConfigReader.Parse("site:\n  title: Docs\n")).Should().NotContain("banner");
	}
}
