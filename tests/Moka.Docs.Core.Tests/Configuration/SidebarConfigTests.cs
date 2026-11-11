using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class SidebarConfigTests
{
	private const string _yaml = """
	                             site:
	                               title: Docs
	                             theme:
	                               options:
	                                 sidebar: shared
	                                 sidebarInlineLimit: 40
	                             """;

	[Fact]
	public void Yaml_SetsTheModeAndTheLimit()
	{
		SiteConfig config = SiteConfigReader.Parse(_yaml);

		config.Theme.Options.Sidebar.Should().Be("shared");
		config.Theme.Options.SidebarInlineLimit.Should().Be(40);
		config.Warnings.Should().BeEmpty("both keys are known");
	}

	[Fact]
	public void Defaults_AreAutoWithALimitOf300()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: Docs\n");

		config.Theme.Options.Sidebar.Should().Be("auto");
		config.Theme.Options.SidebarInlineLimit.Should().Be(300);
	}

	[Fact]
	public void NegativeLimit_IsZero()
	{
		SiteConfig config = SiteConfigReader.Parse(
			"site:\n  title: Docs\ntheme:\n  options:\n    sidebarInlineLimit: -5\n");

		config.Theme.Options.SidebarInlineLimit.Should().Be(0);
	}

	[Fact]
	public void ToYaml_RoundTripsTheModeAndTheLimit()
	{
		SiteConfig config = SiteConfigReader.Parse(SiteConfigReader.ToYaml(SiteConfigReader.Parse(_yaml)));

		config.Theme.Options.Sidebar.Should().Be("shared");
		config.Theme.Options.SidebarInlineLimit.Should().Be(40);
		SiteConfigReader.ToYaml(SiteConfigReader.Parse("site:\n  title: Docs\n")).Should().NotContain("sidebar");
	}

	[Fact]
	public void EnvironmentVariables_WinOverTheYaml()
	{
		ParseWith("MOKADOCS_SIDEBAR", "inline").Sidebar.Should().Be("inline");
		ParseWith("MOKADOCS_SIDEBAR_INLINE_LIMIT", "7").SidebarInlineLimit.Should().Be(7);
	}

	private static ThemeOptions ParseWith(string variable, string value)
	{
		string? previous = Environment.GetEnvironmentVariable(variable);
		Environment.SetEnvironmentVariable(variable, value);
		try
		{
			return SiteConfigReader.Parse(_yaml).Theme.Options;
		}
		finally
		{
			Environment.SetEnvironmentVariable(variable, previous);
		}
	}
}
