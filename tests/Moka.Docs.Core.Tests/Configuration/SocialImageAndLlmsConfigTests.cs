using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

public sealed class SocialImageAndLlmsConfigTests
{
	private static readonly string _yamlDir = Path.Combine(Path.GetTempPath(), "mokadocs-test", "docs");

	[Fact]
	public void SiteImage_LocalPath_ResolvesLikeTheLogo()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\n  image: assets/social.png\n", _yamlDir);

		config.Site.Image.Should().NotBeNull();
		config.Site.Image!.PublishUrl.Should().Be("/assets/social.png");
		config.Site.Image.ShouldCopy.Should().BeTrue();
		config.Warnings.Should().BeEmpty();
	}

	[Fact]
	public void SiteImage_Url_IsKeptAsWritten()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\n  image: https://cdn.example/card.png\n", _yamlDir);

		config.Site.Image!.IsAbsoluteUrl.Should().BeTrue();
		config.Site.Image.PublishUrl.Should().Be("https://cdn.example/card.png");
	}

	[Fact]
	public void SiteImage_CollidingWithTheLogo_Throws()
	{
		const string yaml = "site:\n  title: T\n  logo: ../brand/card.png\n  image: ../other/card.png\n";

		Action act = () => SiteConfigReader.Parse(yaml, _yamlDir);

		act.Should().Throw<SiteConfigException>().WithMessage("*'site.logo'*'site.image'*same publish URL*");
	}

	[Fact]
	public void SiteImage_RoundTrips()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\n  image: ../brand/card.png\n", _yamlDir);

		SiteConfigReader.Parse(SiteConfigReader.ToYaml(config), _yamlDir).Site.Image!.RawValue.Should().Be("../brand/card.png");
	}

	[Fact]
	public void BuildLlms_IsOnByDefault()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\n", _yamlDir);

		config.Build.Llms.Should().BeTrue();
		SiteConfigReader.ToYaml(config).Should().NotContain("llms");
	}

	[Fact]
	public void BuildLlms_CanBeTurnedOffAndRoundTrips()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\nbuild:\n  llms: false\n", _yamlDir);

		config.Build.Llms.Should().BeFalse();
		config.Warnings.Should().BeEmpty();
		SiteConfigReader.Parse(SiteConfigReader.ToYaml(config), _yamlDir).Build.Llms.Should().BeFalse();
	}
}
