using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     <c>build.strict</c> and <c>--warnings-as-errors</c> let CI fail on warnings. Without them a
///     build with broken links or skipped pages still exited 0.
/// </summary>
public sealed class StrictModeTests
{
	private static DiagnosticBag Bag(bool warning, bool error)
	{
		var bag = new DiagnosticBag();
		if (warning)
		{
			bag.Warning("a warning");
		}

		if (error)
		{
			bag.Error("an error");
		}

		return bag;
	}

	[Theory]
	[InlineData(false, false, false, false)]
	[InlineData(true, false, false, false)]
	[InlineData(true, false, true, true)]
	[InlineData(false, true, false, true)]
	[InlineData(false, false, true, false)]
	public void Build_FailsOnErrorsAndOnWarningsWhenStrict(bool warning, bool error, bool strict, bool fails) =>
		BuildCommand.Fails(Bag(warning, error), strict).Should().Be(fails);

	[Theory]
	[InlineData(0, 0, false, 0)]
	[InlineData(0, 2, false, 1)]
	[InlineData(0, 2, true, 2)]
	[InlineData(1, 0, false, 2)]
	[InlineData(0, 0, true, 0)]
	public void Validate_ExitCode_CountsWarningsAsErrorsWhenStrict(int errors, int warnings, bool strict, int expected) =>
		ValidateCommand.ExitCode(errors, warnings, strict).Should().Be(expected);

	[Fact]
	public void BuildStrict_IsReadFromTheConfigAndRoundTrips()
	{
		SiteConfig config = SiteConfigReader.Parse("site:\n  title: T\nbuild:\n  strict: true\n");

		config.Build.Strict.Should().BeTrue();
		config.Warnings.Should().BeEmpty();
		SiteConfigReader.Parse(SiteConfigReader.ToYaml(config)).Build.Strict.Should().BeTrue();
		SiteConfigReader.Parse("site:\n  title: T\n").Build.Strict.Should().BeFalse();
	}
}
