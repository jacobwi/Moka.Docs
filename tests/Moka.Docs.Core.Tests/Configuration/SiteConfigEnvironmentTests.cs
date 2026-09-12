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
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoBuildSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_GENERATE_ROBOTS", "false", "site:\n  title: Docs\n");

		config.Build.Robots.Should().BeFalse();
	}

	[Fact]
	public void EnvironmentOverride_AppliesWhenTheYamlHasNoSearchSection()
	{
		SiteConfig config = ParseWith("MOKADOCS_SEARCH_ENABLED", "false", "site:\n  title: Docs\nfeatures: {}\n");

		config.Features.Search.Enabled.Should().BeFalse();
	}
}
