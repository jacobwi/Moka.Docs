using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class SidebarModesTests
{
	[Theory]
	[InlineData("auto", "auto")]
	[InlineData("INLINE", "inline")]
	[InlineData(" Shared ", "shared")]
	public void KnownNames_ResolveIgnoringCaseAndSpaces(string configured, string expected)
	{
		SidebarModes.Resolve(configured).Should().Be(expected);
		SidebarModes.IsKnown(configured).Should().BeTrue();
	}

	[Theory]
	[InlineData("split")]
	[InlineData("lazy")]
	public void UnknownNames_AreAuto(string configured)
	{
		SidebarModes.Resolve(configured).Should().Be(SidebarModes.Auto);
		SidebarModes.IsKnown(configured).Should().BeFalse();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void EmptyNames_AreAutoAndKnown(string? configured)
	{
		SidebarModes.Resolve(configured).Should().Be(SidebarModes.Auto);
		SidebarModes.IsKnown(configured).Should().BeTrue();
	}

	[Fact]
	public void Names_ListEveryMode()
	{
		SidebarModes.Names.Should().Equal("auto", "inline", "shared");
	}
}
