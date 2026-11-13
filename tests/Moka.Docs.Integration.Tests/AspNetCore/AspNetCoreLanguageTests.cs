using FluentAssertions;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary><c>MokaDocsOptions.Language</c>, <c>Direction</c> and <c>Strings</c>, the host's counterparts of the site's.</summary>
public sealed class AspNetCoreLanguageTests
{
	[Fact]
	public void Defaults_AreEnglishWithTheLanguageDirection()
	{
		SiteConfig config = SiteConfigFactory.Create(new MokaDocsOptions());

		config.Site.Language.Should().Be("en");
		config.Site.Direction.Should().BeEmpty();
		config.Theme.Options.Strings.Should().BeEmpty();
		config.Nav[0].Label.Should().Be("API Reference");
	}

	[Fact]
	public void Options_ReachTheSiteConfig()
	{
		var options = new MokaDocsOptions
		{
			Language = "de",
			Direction = "rtl",
			Strings = new Dictionary<string, string> { ["toc.title"] = "Inhalt", ["api.title"] = "Schnittstellen" }
		};

		SiteConfig config = SiteConfigFactory.Create(options);

		config.Site.Language.Should().Be("de");
		config.Site.Direction.Should().Be("rtl");
		config.Theme.Options.Strings.Should().Equal(options.Strings);
		config.Nav[0].Label.Should().Be("Schnittstellen", "the API section is named like the API index page");
		ThemeText.For(config)["search.button"].Should().Be(ThemeText.For("de")["search.button"]);
	}

	[Fact]
	public void BlankLanguage_IsEnglish()
	{
		SiteConfigFactory.Create(new MokaDocsOptions { Language = " " }).Site.Language.Should().Be("en");
	}
}
