using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary><c>glossary:</c> in mokadocs.yaml and the glossary.yml file in the docs folder.</summary>
public sealed class GlossaryConfigTests
{
	[Fact]
	public void Glossary_IsReadFromTheConfig_AndATermWithoutADefinitionIsAWarning()
	{
		SiteConfig config = SiteConfigReader.Parse(
			"site:\n  title: T\nglossary:\n  API: The public surface.\n  \"Front matter\": The YAML block.\n  Empty:\n");

		config.Glossary.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["API"] = "The public surface.",
			["Front matter"] = "The YAML block."
		});
		config.Warnings.Should().ContainSingle().Which.Should().Be("glossary term 'Empty' has no definition and is ignored");
	}

	[Fact]
	public void Glossary_TermsAreNotUnknownKeys() =>
		SiteConfigReader.Parse("site:\n  title: T\nglossary:\n  Anything: Goes.\n").Warnings.Should().BeEmpty();

	[Fact]
	public void Glossary_RoundTripsThroughYaml()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\nglossary:\n  API: The public surface.\n");

		string yaml = SiteConfigReader.ToYaml(config);

		yaml.Should().Contain("glossary:");
		SiteConfigReader.Parse(yaml).Glossary.Should().ContainKey("API").WhoseValue.Should().Be("The public surface.");
		SiteConfigReader.ToYaml(SiteConfigReader.Parse("site:\n  title: T\n")).Should().NotContain("glossary");
	}

	[Fact]
	public void GlossaryFile_ReadsTermsAndReportsBadEntriesWithTheirLine()
	{
		GlossaryReadResult result = GlossaryFile.Parse("API: The public surface.\nRoute: A page's path.\nEmpty:\nNested:\n  a: b\n");

		result.Terms.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["API"] = "The public surface.",
			["Route"] = "A page's path."
		});
		result.Warnings.Should().Equal(
			"the term 'Empty' has no definition (a text value) and was ignored (line 3)",
			"the term 'Nested' has no definition (a text value) and was ignored (line 4)");
	}

	[Fact]
	public void GlossaryFile_AcceptsAGlossaryKeyAroundTheTerms() =>
		GlossaryFile.Parse("glossary:\n  API: The public surface.\n").Terms.Should().ContainKey("API");

	[Theory]
	[InlineData("API: [unclosed\n", "isn't valid YAML")]
	[InlineData("- a\n- b\n", "should be a list of 'term: definition' lines")]
	public void GlossaryFile_InvalidContent_IsAWarning(string yaml, string message)
	{
		GlossaryReadResult result = GlossaryFile.Parse(yaml);

		result.Terms.Should().BeEmpty();
		result.Warnings.Should().ContainSingle(w => w.Contains(message));
	}

	[Fact]
	public void GlossaryFile_EmptyFile_HasNothing() => GlossaryFile.Parse("").Terms.Should().BeEmpty();
}
