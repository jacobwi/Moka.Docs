using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

public sealed class SiteConfigTests
{
    [Fact]
    public void SiteConfig_WithRequiredFields_CreatesSuccessfully()
    {
        var config = new SiteConfig
        {
            Site = new SiteMetadata { Title = "Test Docs" }
        };

        config.Site.Title.Should().Be("Test Docs");
        config.Build.Output.Should().Be("./_site");
        config.Cloud.Enabled.Should().BeFalse();
        config.Features.Search.Enabled.Should().BeTrue();
    }

    [Fact]
    public void SiteConfig_DefaultValues_AreCorrect()
    {
        var config = new SiteConfig
        {
            Site = new SiteMetadata { Title = "Test" }
        };

        config.Theme.Name.Should().Be("default");
        config.Theme.Options.PrimaryColor.Should().Be(MokaDefaults.PrimaryColor);
        config.Build.Clean.Should().BeTrue();
        config.Build.Minify.Should().BeTrue();
        config.Build.Sitemap.Should().BeTrue();
        config.Plugins.Should().BeEmpty();
        config.Nav.Should().BeEmpty();
    }
}