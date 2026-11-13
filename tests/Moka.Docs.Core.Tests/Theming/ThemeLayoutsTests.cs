using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class ThemeLayoutsTests
{
	public static TheoryData<string, string[]> Variants => new()
	{
		{ "width", ["default", "wide", "wider", "full", "boxed", "narrow"] },
		{ "navbar", ["default", "compact", "floating", "tabs", "autohide", "centered", "transparent"] },
		{ "footer", ["default", "minimal", "columns", "none", "centered", "compact"] },
		{ "toc", ["default", "minimal", "inline", "floating", "numbered", "timeline", "bordered"] },
		{ "breadcrumbs", ["default", "chevrons", "pills", "path", "back", "arrows", "dots"] },
		{ "pageNav", ["default", "minimal", "stacked", "split", "compact"] },
		{ "sidebar", ["default", "lines", "pills", "compact", "accent", "cards", "minimal"] },
		{ "sidebarScope", ["default", "section"] },
		{ "home", ["default", "portal", "showcase", "minimal", "docs"] },
		{ "hero", ["default", "split", "minimal", "banner"] },
		{ "heroBackground", ["default", "dots", "gradient", "none", "mesh", "lines"] },
		{ "features", ["default", "list", "icons", "compact", "bento"] },
		{ "admonitions", ["default", "filled", "minimal", "github", "outlined", "callout"] },
		{ "tables", ["default", "striped", "bordered", "minimal", "compact", "rounded"] },
		{ "density", ["default", "compact", "spacious"] },
		{ "font", ["default", "system", "serif", "mono", "rounded", "humanist"] },
		{ "headings", ["default", "underlined", "accent", "numbered"] },
		{ "links", ["default", "underlined", "subtle"] }
	};

	[Fact]
	public void Parts_AreListedInTheDocumentedOrder()
	{
		ThemeLayouts.Parts.Select(p => p.Key).Should().Equal("width", "navbar", "footer", "toc", "breadcrumbs", "pageNav",
			"sidebar", "sidebarScope", "home", "hero", "heroBackground", "features", "admonitions", "tables", "density", "font",
			"headings", "links");
		ThemeLayouts.Parts.Should().Equal(ThemeLayouts.Width, ThemeLayouts.Navbar, ThemeLayouts.Footer, ThemeLayouts.Toc,
			ThemeLayouts.Breadcrumbs, ThemeLayouts.PageNav, ThemeLayouts.Sidebar, ThemeLayouts.SidebarScope, ThemeLayouts.Home,
			ThemeLayouts.Hero, ThemeLayouts.HeroBackground, ThemeLayouts.Features, ThemeLayouts.Admonitions, ThemeLayouts.Tables,
			ThemeLayouts.Density, ThemeLayouts.Font, ThemeLayouts.Headings, ThemeLayouts.Links);
	}

	[Fact]
	public void ReaderParts_AreTheDocsPagePartsExceptWidth()
	{
		// Width is the Appearance panel's Reading width; hero and heroBackground only shape the landing page.
		ThemeLayouts.ReaderParts.Select(p => p.Key).Should().Equal("navbar", "footer", "toc", "breadcrumbs", "pageNav", "sidebar",
			"sidebarScope", "admonitions", "tables", "density", "font", "headings", "links");
		ThemeLayouts.ReaderParts.Should().BeSubsetOf(ThemeLayouts.Parts);
	}

	[Fact]
	public void Labels_NameEveryPartAndVariantForReaders()
	{
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			part.Label.Should().NotBeNullOrWhiteSpace();
			char.IsUpper(part.Label[0]).Should().BeTrue("{0} starts with a capital", part.Label);
			part.Names.Select(part.LabelOf).Should().OnlyHaveUniqueItems(part.Key)
				.And.OnlyContain(l => l.Length > 0 && char.IsUpper(l[0]), part.Key);
		}

		ThemeLayouts.Toc.Label.Should().Be("On this page");
		ThemeLayouts.PageNav.Label.Should().Be("Previous and next");
		ThemeLayouts.Navbar.LabelOf("default").Should().Be("Standard");
		ThemeLayouts.Navbar.LabelOf("autohide").Should().Be("Auto-hide");
		ThemeLayouts.SidebarScope.LabelOf("default").Should().Be("All sections");
		ThemeLayouts.Admonitions.LabelOf("github").Should().Be("GitHub");
		ThemeLayouts.Tables.LabelOf("striped").Should().Be("Striped");
		ThemeLayouts.HeroBackground.Label.Should().Be("Hero background");
	}

	[Fact]
	public void Variants_AreUniqueWithinEachPart()
	{
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			part.Names.Should().OnlyHaveUniqueItems(part.Key);
			part.Names.Should().OnlyContain(n => n.Length > 0 && n.All(char.IsAsciiLetterLower), "{0} names are written as data attributes and matched by the CSS", part.Key);
		}
	}

	[Theory]
	[MemberData(nameof(Variants))]
	public void Names_StartWithTheDefaultVariant(string key, string[] names)
	{
		ThemeLayoutPart part = ThemeLayouts.Parts.Single(p => p.Key == key);

		part.Names.Should().Equal(names);
		part.Names[0].Should().Be(ThemeLayouts.Default);
	}

	[Theory]
	[MemberData(nameof(Variants))]
	public void Resolve_EveryVariant_ResolvesToItself(string key, string[] names)
	{
		ThemeLayoutPart part = ThemeLayouts.Parts.Single(p => p.Key == key);

		foreach (string name in names)
		{
			part.Resolve(name).Should().Be(name);
			part.IsKnown(name).Should().BeTrue();
		}
	}

	[Theory]
	[InlineData("TABS", "tabs")]
	[InlineData("  Floating ", "floating")]
	[InlineData("autoHide", "autohide")]
	[InlineData("\tcompact\n", "compact")]
	public void Resolve_IgnoresCaseAndSurroundingWhitespace(string configured, string expected)
	{
		ThemeLayouts.Navbar.Resolve(configured).Should().Be(expected);
		ThemeLayouts.Navbar.IsKnown(configured).Should().BeTrue();
	}

	[Theory]
	[InlineData("tbs")]
	[InlineData("auto-hide")]
	[InlineData("tabs floating")]
	[InlineData("wide")]
	public void Resolve_UnknownName_FallsBackToDefault(string configured)
	{
		ThemeLayouts.Navbar.Resolve(configured).Should().Be(ThemeLayouts.Default);
		ThemeLayouts.Navbar.IsKnown(configured).Should().BeFalse();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Resolve_EmptyName_IsTheDefaultAndNotReported(string? configured)
	{
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			part.Resolve(configured).Should().Be(ThemeLayouts.Default);
			part.IsKnown(configured).Should().BeTrue();
		}
	}

	[Fact]
	public void NameOf_ReadsEachPartsOwnProperty()
	{
		var layout = new ThemeLayoutOptions
		{
			Width = "a", Navbar = "b", Footer = "c", Toc = "d", Breadcrumbs = "e", PageNav = "f", Sidebar = "g",
			SidebarScope = "h", Home = "q", Hero = "i", HeroBackground = "j", Features = "r", Admonitions = "k", Tables = "l",
			Density = "m", Font = "n", Headings = "o", Links = "p"
		};

		ThemeLayouts.Parts.Select(layout.NameOf).Should()
			.Equal("a", "b", "c", "d", "e", "f", "g", "h", "q", "i", "j", "r", "k", "l", "m", "n", "o", "p");
	}

	[Fact]
	public void Resolve_AVariantOfAnotherPart_IsUnknown()
	{
		ThemeLayouts.PageNav.IsKnown("floating").Should().BeFalse();
		ThemeLayouts.PageNav.Resolve("floating").Should().Be(ThemeLayouts.Default);
		ThemeLayouts.Toc.IsKnown("floating").Should().BeTrue();
	}
}
