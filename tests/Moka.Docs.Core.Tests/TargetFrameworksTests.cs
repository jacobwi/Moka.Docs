using FluentAssertions;

namespace Moka.Docs.Core.Tests;

public sealed class TargetFrameworksTests
{
	[Theory]
	[InlineData("net10.0", 10, "10.0")]
	[InlineData("net9.0", 10, "9.0")]
	[InlineData("net8.0-windows", 10, "8.0")]
	[InlineData("netcoreapp3.1", 10, "3.1")]
	[InlineData("netstandard2.0", 10, "0.0")]
	[InlineData("net10.0", 9, null)]
	[InlineData("net48", 10, null)]
	[InlineData("publish", 10, null)]
	public void LoadableVersion_ParsesFolderNames(string folder, int runtimeMajor, string? expected)
	{
		string? version = TargetFrameworks.LoadableVersion(folder, runtimeMajor)?.ToString();

		version.Should().Be(expected);
	}

	[Fact]
	public void LoadableVersion_OrdersNet10AboveNet9()
	{
		// Sorted by name, net9.0 came first, so a project building both used its older build.
		string[] folders = ["net9.0", "net10.0", "netstandard2.0"];

		folders.OrderByDescending(f => TargetFrameworks.LoadableVersion(f, 10))
			.Should().Equal("net10.0", "net9.0", "netstandard2.0");
	}
}
