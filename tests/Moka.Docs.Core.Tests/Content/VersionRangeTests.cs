using FluentAssertions;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Core.Tests.Content;

/// <summary>
///     The <c>version</c> front matter key was parsed and never read. Pages now declare the versions
///     they apply to with npm-style ranges or NuGet intervals.
/// </summary>
public sealed class VersionRangeTests
{
	[Theory]
	[InlineData(">=2.0", "2.0.0", true)]
	[InlineData(">=2.0", "1.9.9", false)]
	[InlineData(">=2.0", "3.1.0", true)]
	[InlineData(">2.0", "2.0.5", false)]
	[InlineData(">2.0", "2.1.0", true)]
	[InlineData(">2.0.1", "2.0.2", true)]
	[InlineData(">2", "2.9.0", false)]
	[InlineData("<3", "2.99.0", true)]
	[InlineData("<3", "3.0.0", false)]
	[InlineData("<=2.4", "2.4.7", true)]
	[InlineData("<=2.4", "2.5.0", false)]
	[InlineData("<=2.4.1", "2.4.1", true)]
	[InlineData("=2.0.1", "2.0.1", true)]
	[InlineData("=2.0.1", "2.0.2", false)]
	[InlineData("2.0", "2.0.9", true)]
	[InlineData("2.0", "2.1.0", false)]
	[InlineData("1.x", "1.8.0", true)]
	[InlineData("1.x", "2.0.0", false)]
	[InlineData("1.*", "1.0.0", true)]
	[InlineData("2", "2.4.0", true)]
	[InlineData("*", "9.9.9", true)]
	[InlineData("x", "0.0.1", true)]
	[InlineData("v2.0", "2.0.0", true)]
	[InlineData(">= 1.0 < 2.0", "1.5.0", true)]
	[InlineData(">=1.0 <2.0", "2.0.0", false)]
	[InlineData("1.x || >=3.0", "3.2.0", true)]
	[InlineData("1.x || >=3.0", "2.5.0", false)]
	[InlineData("^1.2", "1.9.0", true)]
	[InlineData("^1.2", "2.0.0", false)]
	[InlineData("^1.2", "1.1.0", false)]
	[InlineData("^0.2.3", "0.2.9", true)]
	[InlineData("^0.2.3", "0.3.0", false)]
	[InlineData("^0.0.3", "0.0.4", false)]
	[InlineData("~1.2.3", "1.2.9", true)]
	[InlineData("~1.2.3", "1.3.0", false)]
	[InlineData("~1", "1.9.0", true)]
	[InlineData("1.0 - 2.0", "2.0.9", true)]
	[InlineData("1.0 - 2.0", "2.1.0", false)]
	[InlineData("1.0 - 2.0", "0.9.0", false)]
	[InlineData("[1.0,2.0)", "1.0.0", true)]
	[InlineData("[1.0,2.0)", "2.0.0", false)]
	[InlineData("(1.0,2.0]", "1.0.0", false)]
	[InlineData("(1.0,2.0]", "2.0.0", true)]
	[InlineData("[2.0,)", "7.0.0", true)]
	[InlineData("(,3.0]", "3.0.0", true)]
	[InlineData("(,3.0)", "3.0.0", false)]
	[InlineData("[2.0]", "2.0.0", true)]
	[InlineData("[2.0]", "2.0.1", false)]
	[InlineData(">=3.0.0-beta", "3.0.0", true)]
	public void Contains_FollowsNpmAndNuGetRangeRules(string range, string version, bool expected)
	{
		VersionRange.TryParse(range, out VersionRange? parsed, out string? error).Should().BeTrue(error);
		VersionNumber.TryParse(version, out VersionNumber number).Should().BeTrue();

		parsed!.Contains(number).Should().Be(expected, $"'{range}' against {version}");
	}

	[Fact]
	public void Contains_IgnoresAPrereleaseSuffixOnTheVersion()
	{
		// Docs for a 3.0 beta should show the pages written for 3.0.
		VersionRange.TryParse(">=3.0", out VersionRange? range, out _).Should().BeTrue();
		VersionNumber.TryParse("3.0.0-beta.2", out VersionNumber beta).Should().BeTrue();

		range!.Contains(beta).Should().BeTrue();
	}

	[Theory]
	[InlineData("", "empty")]
	[InlineData("   ", "empty")]
	[InlineData(">=two", "'two' is not a version")]
	[InlineData("1.x ||", "'||' needs a range on each side")]
	[InlineData(">=", "needs a version after it")]
	[InlineData("1.2.3.4", "is not a version")]
	[InlineData("[1.0,2.0", "needs a closing")]
	[InlineData("[,]", "needs at least one bound")]
	[InlineData("(2.0)", "an exact version is written like [2.0]")]
	[InlineData("[1.x,2.0)", "'1.x' is not a version")]
	[InlineData("next", "'next' is not a version")]
	public void TryParse_RejectsWhatIsNotARange(string range, string reason)
	{
		VersionRange.TryParse(range, out VersionRange? parsed, out string? error).Should().BeFalse();

		parsed.Should().BeNull();
		error.Should().Contain(reason);
	}

	[Theory]
	[InlineData("1.8.0", 1, 8, 0)]
	[InlineData("v2.0 Beta", 2, 0, 0)]
	[InlineData("V3", 3, 0, 0)]
	[InlineData("3.1.0-preview.2+abc", 3, 1, 0)]
	[InlineData("1.2.3.4", 1, 2, 3)]
	public void VersionNumber_ReadsTheNumbersAtTheStart(string text, int major, int minor, int patch)
	{
		VersionNumber.TryParse(text, out VersionNumber version).Should().BeTrue();

		version.Should().Be(new VersionNumber(major, minor, patch));
	}

	[Theory]
	[InlineData("latest")]
	[InlineData("next")]
	[InlineData("")]
	[InlineData(null)]
	public void VersionNumber_RejectsLabelsWithoutANumber(string? text) =>
		VersionNumber.TryParse(text, out _).Should().BeFalse();

	[Fact]
	public void VersionNumber_ComparesByMajorMinorPatch()
	{
		new VersionNumber(1, 10, 0).Should().BeGreaterThan(new VersionNumber(1, 9, 9));
		(new VersionNumber(2, 0, 0) > new VersionNumber(1, 99, 99)).Should().BeTrue();
		(new VersionNumber(1, 2, 3) <= new VersionNumber(1, 2, 3)).Should().BeTrue();
		new VersionNumber(1, 2, 3).ToString().Should().Be("1.2.3");
	}
}
