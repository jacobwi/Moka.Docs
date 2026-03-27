using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

public sealed class SiteConfigReaderTests
{
	[Fact]
	public void Parse_MinimalConfig_ReturnsConfigWithDefaults()
	{
		const string yaml = """
		                    site:
		                      title: "My Docs"
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);

		config.Site.Title.Should().Be("My Docs");
		config.Site.Description.Should().BeEmpty();
		config.Content.Docs.Should().Be("./docs");
		config.Theme.Name.Should().Be("default");
		config.Theme.Options.PrimaryColor.Should().Be(MokaDefaults.PrimaryColor);
		config.Build.Output.Should().Be("./_site");
		config.Build.Clean.Should().BeTrue();
		config.Cloud.Enabled.Should().BeFalse();
		config.Features.Search.Enabled.Should().BeTrue();
		config.Features.Versioning.Enabled.Should().BeFalse();
		config.Features.Blog.Enabled.Should().BeFalse();
	}

	[Fact]
	public void Parse_FullConfig_MapsAllFields()
	{
		const string yaml = """
		                    site:
		                      title: "MokaDocs"
		                      description: "Best docs ever"
		                      url: "https://docs.moka.dev"
		                      logo: ./assets/logo.svg
		                      favicon: ./assets/favicon.ico
		                      copyright: "© 2026 Moka"
		                      editLink:
		                        repo: "https://github.com/moka/mokadocs"
		                        branch: develop
		                        path: documentation/

		                    content:
		                      docs: ./documentation
		                      projects:
		                        - path: ../src/MyLib/MyLib.csproj
		                          label: "MyLib"
		                          includeInternals: true
		                        - path: ../src/MyOther/MyOther.csproj
		                          label: "MyOther"

		                    theme:
		                      name: custom-theme
		                      options:
		                        primaryColor: "#ff0000"
		                        accentColor: "#00ff00"
		                        codeTheme: "dracula"
		                        showEditLink: false
		                        showLastUpdated: false
		                        showContributors: true
		                        socialLinks:
		                          - icon: github
		                            url: "https://github.com/moka"
		                          - icon: discord
		                            url: "https://discord.gg/moka"

		                    nav:
		                      - label: "Guide"
		                        path: /guide
		                        icon: book
		                        expanded: true
		                      - label: "API"
		                        path: /api
		                        autoGenerate: true

		                    features:
		                      search:
		                        enabled: true
		                        provider: flexsearch
		                      versioning:
		                        enabled: true
		                        strategy: dropdown-only
		                        versions:
		                          - label: "v2.0"
		                            branch: main
		                            default: true
		                          - label: "v1.0"
		                            branch: release/1.0
		                            prerelease: false
		                      blog:
		                        enabled: true
		                        postsPerPage: 20
		                        showAuthors: false

		                    plugins:
		                      - name: Moka.Docs.Plugin.Mermaid
		                      - name: Moka.Docs.Plugin.Analytics
		                        options:
		                          provider: plausible
		                          siteId: mokadocs.dev

		                    cloud:
		                      enabled: true
		                      apiKey: "test-key-123"
		                      features:
		                        aiSummaries: true
		                        pdfExport: true
		                        analytics: false
		                        customDomain: false

		                    build:
		                      output: ./dist
		                      clean: false
		                      minify: false
		                      sitemap: false
		                      robots: false
		                      cache: false
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);

		// Site
		config.Site.Title.Should().Be("MokaDocs");
		config.Site.Description.Should().Be("Best docs ever");
		config.Site.Url.Should().Be("https://docs.moka.dev");
		config.Site.Logo.Should().Be("./assets/logo.svg");
		config.Site.Favicon.Should().Be("./assets/favicon.ico");
		config.Site.Copyright.Should().Be("© 2026 Moka");
		config.Site.EditLink.Should().NotBeNull();
		config.Site.EditLink!.Repo.Should().Be("https://github.com/moka/mokadocs");
		config.Site.EditLink.Branch.Should().Be("develop");
		config.Site.EditLink.Path.Should().Be("documentation/");

		// Content
		config.Content.Docs.Should().Be("./documentation");
		config.Content.Projects.Should().HaveCount(2);
		config.Content.Projects[0].Path.Should().Be("../src/MyLib/MyLib.csproj");
		config.Content.Projects[0].Label.Should().Be("MyLib");
		config.Content.Projects[0].IncludeInternals.Should().BeTrue();
		config.Content.Projects[1].IncludeInternals.Should().BeFalse();

		// Theme
		config.Theme.Name.Should().Be("custom-theme");
		config.Theme.Options.PrimaryColor.Should().Be("#ff0000");
		config.Theme.Options.AccentColor.Should().Be("#00ff00");
		config.Theme.Options.CodeTheme.Should().Be("dracula");
		config.Theme.Options.ShowEditLink.Should().BeFalse();
		config.Theme.Options.ShowLastUpdated.Should().BeFalse();
		config.Theme.Options.ShowContributors.Should().BeTrue();
		config.Theme.Options.SocialLinks.Should().HaveCount(2);
		config.Theme.Options.SocialLinks[0].Icon.Should().Be("github");

		// Nav
		config.Nav.Should().HaveCount(2);
		config.Nav[0].Label.Should().Be("Guide");
		config.Nav[0].Path.Should().Be("/guide");
		config.Nav[0].Icon.Should().Be("book");
		config.Nav[0].Expanded.Should().BeTrue();
		config.Nav[1].AutoGenerate.Should().BeTrue();

		// Features
		config.Features.Search.Enabled.Should().BeTrue();
		config.Features.Search.Provider.Should().Be("flexsearch");
		config.Features.Versioning.Enabled.Should().BeTrue();
		config.Features.Versioning.Strategy.Should().Be("dropdown-only");
		config.Features.Versioning.Versions.Should().HaveCount(2);
		config.Features.Versioning.Versions[0].Label.Should().Be("v2.0");
		config.Features.Versioning.Versions[0].Default.Should().BeTrue();
		config.Features.Blog.Enabled.Should().BeTrue();
		config.Features.Blog.PostsPerPage.Should().Be(20);
		config.Features.Blog.ShowAuthors.Should().BeFalse();

		// Plugins
		config.Plugins.Should().HaveCount(2);
		config.Plugins[0].Name.Should().Be("Moka.Docs.Plugin.Mermaid");
		config.Plugins[1].Name.Should().Be("Moka.Docs.Plugin.Analytics");

		// Cloud
		config.Cloud.Enabled.Should().BeTrue();
		config.Cloud.ApiKey.Should().Be("test-key-123");
		config.Cloud.Features.AiSummaries.Should().BeTrue();
		config.Cloud.Features.PdfExport.Should().BeTrue();
		config.Cloud.Features.Analytics.Should().BeFalse();

		// Build
		config.Build.Output.Should().Be("./dist");
		config.Build.Clean.Should().BeFalse();
		config.Build.Minify.Should().BeFalse();
		config.Build.Sitemap.Should().BeFalse();
		config.Build.Robots.Should().BeFalse();
		config.Build.Cache.Should().BeFalse();
	}

	[Fact]
	public void Parse_MissingSiteSection_Throws()
	{
		const string yaml = """
		                    theme:
		                      name: default
		                    """;

		Func<SiteConfig> act = () => SiteConfigReader.Parse(yaml);
		act.Should().Throw<SiteConfigException>().WithMessage("*site*required*");
	}

	[Fact]
	public void Parse_MissingSiteTitle_Throws()
	{
		const string yaml = """
		                    site:
		                      description: "No title here"
		                    """;

		Func<SiteConfig> act = () => SiteConfigReader.Parse(yaml);
		act.Should().Throw<SiteConfigException>().WithMessage("*site.title*required*");
	}

	[Fact]
	public void Parse_EmptyYaml_Throws()
	{
		Func<SiteConfig> act = () => SiteConfigReader.Parse("");
		act.Should().Throw<SiteConfigException>();
	}

	[Fact]
	public void Parse_InvalidYaml_Throws()
	{
		const string yaml = """
		                    site:
		                      title: [invalid
		                    """;

		Func<SiteConfig> act = () => SiteConfigReader.Parse(yaml);
		act.Should().Throw<SiteConfigException>();
	}

	[Fact]
	public void Parse_UnknownProperties_AreIgnored()
	{
		const string yaml = """
		                    site:
		                      title: "Test"
		                      unknownField: "should be ignored"
		                    extraSection:
		                      foo: bar
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);
		config.Site.Title.Should().Be("Test");
	}

	[Fact]
	public void ToYaml_RoundTrip_PreservesValues()
	{
		const string yaml = """
		                    site:
		                      title: "Round Trip Test"
		                      description: "Testing serialization"
		                    content:
		                      docs: ./my-docs
		                    theme:
		                      name: default
		                      options:
		                        primaryColor: "#123456"
		                    build:
		                      output: ./output
		                      clean: true
		                    """;

		SiteConfig config = SiteConfigReader.Parse(yaml);
		string serialized = SiteConfigReader.ToYaml(config);
		SiteConfig reparsed = SiteConfigReader.Parse(serialized);

		reparsed.Site.Title.Should().Be("Round Trip Test");
		reparsed.Site.Description.Should().Be("Testing serialization");
		reparsed.Content.Docs.Should().Be("./my-docs");
		reparsed.Theme.Options.PrimaryColor.Should().Be("#123456");
		reparsed.Build.Output.Should().Be("./output");
	}
}
