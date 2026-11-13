using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class SiteLanguageConfigTests
{
	[Fact]
	public void Defaults_AreEnglishLeftToRightWithNoStrings()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: Docs\n");

		config.Site.Language.Should().Be("en");
		config.Site.Direction.Should().BeEmpty();
		config.Theme.Options.Strings.Should().BeEmpty();
	}

	[Fact]
	public void Reads_LanguageDirectionAndStrings()
	{
		SiteConfig config = SiteConfigReader.Parse(
			"site:\n  title: Docs\n  language: pt-BR\n  direction: rtl\n" +
			"theme:\n  options:\n    strings:\n      toc.title: Conteúdo\n      search.resultCount.few: \"{count} wyniki\"\n      footer.builtWith:\n");

		config.Site.Language.Should().Be("pt-BR");
		config.Site.Direction.Should().Be("rtl");
		config.Theme.Options.Strings.Should().Equal(new Dictionary<string, string>
		{
			["toc.title"] = "Conteúdo",
			["search.resultCount.few"] = "{count} wyniki",
			["footer.builtWith"] = ""
		});
		config.Warnings.Should().BeEmpty("the keys under strings are the theme's, not mokadocs.yaml's");
	}

	[Fact]
	public void ToYaml_RoundTripsWhatDiffersFromTheDefaults()
	{
		SiteConfig config = SiteConfigReader.Parse(
			"site:\n  title: Docs\n  language: de\ntheme:\n  options:\n    strings:\n      toc.title: Inhalt\n");

		string yaml = SiteConfigReader.ToYaml(config);
		SiteConfig again = SiteConfigReader.Parse(yaml);

		again.Site.Language.Should().Be("de");
		again.Theme.Options.Strings.Should().ContainKey("toc.title").WhoseValue.Should().Be("Inhalt");
		SiteConfigReader.ToYaml(SiteConfigReader.Parse("site:\n  title: Docs\n")).Should().NotContain("language").And.NotContain("strings");
	}

	[Fact]
	public void EnvironmentVariable_SetsTheLanguage()
	{
		string? previous = Environment.GetEnvironmentVariable("MOKADOCS_SITE_LANGUAGE");
		Environment.SetEnvironmentVariable("MOKADOCS_SITE_LANGUAGE", "ja");
		try
		{
			SiteConfigReader.Parse("site:\n  title: Docs\n  language: de\n").Site.Language.Should().Be("ja");
		}
		finally
		{
			Environment.SetEnvironmentVariable("MOKADOCS_SITE_LANGUAGE", previous);
		}
	}
}
