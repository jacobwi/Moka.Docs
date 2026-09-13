using FluentAssertions;
using Moka.Docs.Core.Content;
using Moka.Docs.Parsing.FrontMatter;

namespace Moka.Docs.Parsing.Tests.FrontMatter;

/// <summary>
///     The landing layout's feature cards come from front matter. They used to be MokaDocs'
///     own cards, hard-coded into the layout and shown on every site's landing page.
/// </summary>
public sealed class LandingFeaturesFrontMatterTests
{
	private readonly FrontMatterExtractor _extractor = new();

	[Fact]
	public void Extract_FeaturesList_ReadsEveryCardInOrder()
	{
		const string input = """
		                     ---
		                     title: Home
		                     layout: landing
		                     featuresTitle: Why Widgets
		                     featuresSubtitle: " Everything in one package "
		                     features:
		                       - title: Fast
		                         icon: zap
		                         description: Renders in a millisecond.
		                         link: /guide/speed
		                       - title: "C# first"
		                         icon: "C#"
		                       -
		                       - description: A card with only text.
		                     ---
		                     Body
		                     """;

		FrontMatterResult result = _extractor.Extract(input);

		result.Error.Should().BeNull();
		result.FrontMatter.FeaturesTitle.Should().Be("Why Widgets");
		result.FrontMatter.FeaturesSubtitle.Should().Be("Everything in one package");
		result.FrontMatter.Features.Should().Equal(
			new LandingFeature { Title = "Fast", Icon = "zap", Description = "Renders in a millisecond.", Link = "/guide/speed" },
			new LandingFeature { Title = "C# first", Icon = "C#" },
			new LandingFeature { Description = "A card with only text." });
	}

	[Fact]
	public void Extract_FeaturesThatAreNotAList_ReportTheFrontMatterAsUnreadable()
	{
		// The front matter docs promise this, so a page with a typo gets a warning.
		FrontMatterResult result = _extractor.Extract("---\ntitle: Home\nfeatures: Fast\n---\nBody");

		result.Error.Should().NotBeNull();
		result.FrontMatter.Title.Should().Be("Untitled");
	}

	[Fact]
	public void Extract_NoFeatures_LeavesTheSectionEmpty()
	{
		FrontMatterResult result = _extractor.Extract("---\ntitle: Home\nlayout: landing\n---\nBody");

		result.FrontMatter.Features.Should().BeEmpty();
		result.FrontMatter.FeaturesTitle.Should().BeEmpty();
		result.FrontMatter.FeaturesSubtitle.Should().BeEmpty();
	}
}
