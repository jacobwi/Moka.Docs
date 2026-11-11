using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class ThemeLayoutsTests
{
	public static TheoryData<string, string[]> Variants => new()
	{
		{ "width", ["default", "wide", "full", "boxed"] },
		{ "navbar", ["default", "compact", "floating", "tabs", "autohide"] },
		{ "footer", ["default", "minimal", "columns", "none"] },
		{ "toc", ["default", "minimal", "inline", "floating"] },
		{ "breadcrumbs", ["default", "chevrons", "pills", "path", "back"] },
		{ "pageNav", ["default", "minimal", "stacked"] }
	};

	[Fact]
	public void Parts_AreListedInTheDocumentedOrder()
	{
		ThemeLayouts.Parts.Select(p => p.Key).Should().Equal("width", "navbar", "footer", "toc", "breadcrumbs", "pageNav");
		ThemeLayouts.Parts.Should().Equal(ThemeLayouts.Width, ThemeLayouts.Navbar, ThemeLayouts.Footer, ThemeLayouts.Toc,
			ThemeLayouts.Breadcrumbs, ThemeLayouts.PageNav);
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
	public void Resolve_AVariantOfAnotherPart_IsUnknown()
	{
		ThemeLayouts.PageNav.IsKnown("floating").Should().BeFalse();
		ThemeLayouts.PageNav.Resolve("floating").Should().Be(ThemeLayouts.Default);
		ThemeLayouts.Toc.IsKnown("floating").Should().BeTrue();
	}
}
