using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     Environment variables are process-wide, so these tests must not run alongside tests
///     that read the same defaults.
/// </summary>
[CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
public sealed class EnvironmentVariableCollection;

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class SiteConfigEnvironmentTests
{
	private static SiteConfig ParseWith(string variable, string value, string yaml)
	{
		string? previous = Environment.GetEnvironmentVariable(variable);
		Environment.SetEnvironmentVariable(variable, value);
		try
		{
			return SiteConfigReader.Parse(yaml);
		}
		finally
		{
			Environment.SetEnvironmentVariable(variable, previous);
		}
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoThemeSection()
	{
		// Overrides used to be read only while mapping a section the yaml already contained.
		SiteConfig config = ParseWith("MOKADOCS_CODE_STYLE", "terminal", "site:\n  title: Docs\n");

		config.Theme.Options.CodeStyle.Should().Be("terminal");
	}

	[Fact]
	public void EnvironmentOverride_Style_WinsOverTheYaml()
	{
		SiteConfig config = ParseWith("MOKADOCS_STYLE", "classic",
			"site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n");

		config.Theme.Options.Style.Should().Be("classic");
	}

	[Fact]
	public void EnvironmentOverride_StyleSelector_CanHideIt()
	{
		SiteConfig config = ParseWith("MOKADOCS_SHOW_STYLE_SELECTOR", "false", "site:\n  title: Docs\n");

		config.Theme.Options.StyleSelector.Should().BeFalse();
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoBuildSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_GENERATE_ROBOTS", "false", "site:\n  title: Docs\n");

		config.Build.Robots.Should().BeFalse();
	}

	[Theory]
	[InlineData("true", "build:\n  minify: false\n", true)]
	[InlineData("false", "build:\n  minify: true\n", false)]
	[InlineData("1", "", true)]
	public void EnvironmentOverride_MinifyOutput_WinsOverTheYaml(string value, string build, bool expected)
	{
		SiteConfig config = ParseWith("MOKADOCS_MINIFY_OUTPUT", value, "site:\n  title: Docs\n" + build);

		config.Build.Minify.Should().Be(expected);
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoSearchSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_SEARCH_ENABLED", "false", "site:\n  title: Docs\nfeatures: {}\n");

		config.Features.Search.Enabled.Should().BeFalse();
	}
}
