using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class ThemeStylesTests
{
	[Theory]
	[InlineData("moka", "moka")]
	[InlineData("Classic", "classic")]
	[InlineData("  soft ", "soft")]
	[InlineData("MINIMAL", "minimal")]
	[InlineData("default", "default")]
	public void Resolve_KnownName_ReturnsTheStyle(string configured, string expected)
	{
		ThemeStyles.Resolve(configured).Should().Be(expected);
		ThemeStyles.IsKnown(configured).Should().BeTrue();
	}

	[Theory]
	[InlineData("fancy")]
	[InlineData("moka-red")]
	[InlineData("moka minimal")]
	public void Resolve_UnknownName_FallsBackToDefault(string configured)
	{
		ThemeStyles.Resolve(configured).Should().Be(ThemeStyles.Default);
		ThemeStyles.IsKnown(configured).Should().BeFalse();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Resolve_EmptyName_IsTheDefaultAndNotReported(string? configured)
	{
		ThemeStyles.Resolve(configured).Should().Be(ThemeStyles.Default);
		ThemeStyles.IsKnown(configured).Should().BeTrue();
	}

	[Fact]
	public void Names_StartWithTheDefaultStyle()
	{
		ThemeStyles.Names.Should().Equal("default", "moka", "minimal", "classic", "soft");
	}
}
