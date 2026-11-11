using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     mokadocs.yaml typos used to be ignored without a word, and a value of the wrong type
///     failed with "Exception during deserialization" and no location.
/// </summary>
public sealed class SiteConfigStrictnessTests
{
	[Fact]
	public void Parse_UnknownKeys_AreWarningsWithTheirPathAndLine()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                      titel: Typo
		                    theme:
		                      options:
		                        primaryColour: "#fff"
		                    nav:
		                      - label: Guide
		                        children:
		                          - label: Intro
		                            pth: /guide/intro
		                    build:
		                      basepath: /Repo
		                    analytics: true
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);

		config.Warnings.Should().Equal(
			"unknown key 'site.titel' (line 3)",
			"unknown key 'theme.options.primaryColour' (line 6)",
			"unknown key 'nav[0].children[0].pth' (line 11)",
			"unknown key 'build.basepath' (line 13); keys are case sensitive, did you mean 'basePath'?",
			"unknown key 'analytics' (line 14)");
		config.Build.BasePath.Should().Be("/", "the misspelled key was not applied");
	}

	[Fact]
	public void Parse_SearchProvider_IsAWarningThatItIsIgnored()
	{
		// It was parsed and read by nothing, so "provider: pagefind" looked like it did something.
		const string yaml = """
		                    site:
		                      title: Docs
		                    features:
		                      search:
		                        enabled: false
		                        provider: pagefind
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);

		config.Warnings.Should().Equal(
			"'features.search.provider' (line 6) is no longer read and is ignored: " +
			"search always uses the built-in search-index.json");
		config.Features.Search.Enabled.Should().BeFalse();
	}

	[Fact]
	public void Parse_ProviderUnderAnotherSection_IsAnUnknownKey()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    features:
		                      versioning:
		                        provider: x
		                    """;

		SiteConfigReader.Parse(yaml).Warnings.Should().Equal("unknown key 'features.versioning.provider' (line 5)");
	}

	[Fact]
	public void Parse_PluginOptions_TakeAnyKey()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    plugins:
		                      - name: mokadocs-openapi
		                        options:
		                          spec: ./api.yaml
		                          anythingAtAll: 1
		                    """;

		SiteConfigReader.Parse(yaml).Warnings.Should().BeEmpty();
	}

	[Fact]
	public void Parse_AValueOfTheWrongType_NamesTheLineColumnAndCause()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    theme:
		                      options:
		                        tocDepth: three
		                    """;

		Action parse = () => SiteConfigReader.Parse(yaml);

		parse.Should().Throw<SiteConfigException>()
			.Which.Message.Should().StartWith("Failed to parse configuration: line 5, column 15: ")
			.And.Contain("three");
	}

	[Fact]
	public void Parse_BrokenYaml_NamesTheLineAndColumn()
	{
		const string yaml = "site:\n  title: Docs\n  description: \"unterminated\n";

		Action parse = () => SiteConfigReader.Parse(yaml);

		parse.Should().Throw<SiteConfigException>().Which.Message.Should().MatchRegex(@"line \d+, column \d+: ");
	}

	[Fact]
	public void Parse_BackslashesInPaths_BecomeForwardSlashes()
	{
		// On Linux ".\_site" named a folder literally called ".\_site" next to the docs.
		const string yaml = """
		                    site:
		                      title: Docs
		                    content:
		                      docs: .\documentation
		                      projects:
		                        - path: ..\src\Lib\Lib.csproj
		                    theme:
		                      name: .\themes\brand
		                    build:
		                      output: .\_site
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);

		config.Content.Docs.Should().Be("./documentation");
		config.Content.Projects.Single().Path.Should().Be("../src/Lib/Lib.csproj");
		config.Theme.Name.Should().Be("./themes/brand");
		config.Build.Output.Should().Be("./_site");
	}

	[Fact]
	public void Parse_BackslashesInTheLogoPath_ResolveOnEveryOs()
	{
		string yamlDir = Path.Combine(Path.GetTempPath(), "mokadocs-logo");

		SiteConfig config = SiteConfigReader.Parse("site:\n  title: Docs\n  logo: assets\\brand\\logo.svg\n", yamlDir);

		config.Site.Logo!.PublishUrl.Should().Be("/assets/brand/logo.svg");
		config.Site.Logo.SourcePath.Should().Be(Path.Combine(yamlDir, "assets", "brand", "logo.svg"));
		config.Site.Logo.RawValue.Should().Be("assets\\brand\\logo.svg");
	}
}
