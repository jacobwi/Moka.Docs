using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Rendering;

public sealed class LandingLayoutTests
{
	[Fact]
	public async Task LandingPage_WithoutFeatures_ShowsNoFeatureSectionAndNoMokaDocsContent()
	{
		// Every site's landing page showed MokaDocs' own feature cards and a sample mokadocs.yaml.
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Widgets\ndescription: Widgets for .NET\nlayout: landing\n---\n## Install\n\nRun it.\n")
		});

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().Contain("Widgets for .NET").And.Contain("Run it.");
		html.Should().NotContain("landing-features")
			.And.NotContain("landing-feature-card")
			.And.NotContain("Everything you need")
			.And.NotContain("C# API Reference")
			.And.NotContain("landing-code-section")
			.And.NotContain("mokadocs.yaml")
			.And.NotContain("landing-divider");
	}

	[Fact]
	public async Task LandingPage_Features_RenderFromFrontMatterWithEscapedTextIconsAndLinks()
	{
		const string index = """
		                     ---
		                     title: Widgets
		                     layout: landing
		                     featuresTitle: Why <Widgets>
		                     featuresSubtitle: Fast & small
		                     features:
		                       - title: Fast
		                         icon: zap
		                         description: Renders in "no" time.
		                         link: /guide/speed
		                       - title: <script>alert(1)</script>
		                         icon: "C#"
		                       - title: Hosted
		                         link: https://example.com/docs?a=1&b=2
		                     ---
		                     Body text.
		                     """;
		SiteConfig config = MockSite.Config() with { Build = new BuildConfig { BasePath = "/Repo" } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new(index)
		});

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().Contain("<h2 class=\"landing-features-title\">Why &lt;Widgets&gt;</h2>")
			.And.Contain("<p class=\"landing-features-subtitle\">Fast &amp; small</p>")
			.And.Contain("<hr class=\"landing-divider\" />");
		Regex.Matches(html, "<(a|div) class=\"landing-feature-card\"").Select(m => m.Groups[1].Value)
			.Should().Equal("a", "div", "a");

		// A root-relative link gets the base path; a link to another site is left as written.
		html.Should().Contain("<a class=\"landing-feature-card\" href=\"/Repo/guide/speed\">")
			.And.Contain("<a class=\"landing-feature-card\" href=\"https://example.com/docs?a=1&amp;b=2\">");

		// zap is in the icon set and renders as SVG; C# is not, so it shows as text.
		html.Should().Contain("<div class=\"landing-feature-icon\"><svg")
			.And.Contain("<div class=\"landing-feature-icon\">C#</div>");

		html.Should().Contain("<p class=\"landing-feature-desc\">Renders in &quot;no&quot; time.</p>")
			.And.Contain("<div class=\"landing-feature-name\">&lt;script&gt;alert(1)&lt;/script&gt;</div>")
			.And.NotContain("<script>alert(1)</script>");
	}
}
