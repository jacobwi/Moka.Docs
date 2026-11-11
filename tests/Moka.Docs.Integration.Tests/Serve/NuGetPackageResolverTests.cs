using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

public sealed class NuGetPackageResolverTests
{
	[Theory]
	[InlineData("Newtonsoft.Json", "Newtonsoft.Json", null)]
	[InlineData(" Humanizer@2.14.1 ", "Humanizer", "2.14.1")]
	[InlineData("My_Lib-Core.Abstractions@1.0.0-beta.2+sha.5", "My_Lib-Core.Abstractions", "1.0.0-beta.2+sha.5")]
	[InlineData("Foo@*", "Foo", "*")]
	[InlineData("Foo@1.*", "Foo", "1.*")]
	[InlineData("Foo@1.2.*-*", "Foo", "1.2.*-*")]
	[InlineData("Foo@2.0.0-rc*", "Foo", "2.0.0-rc*")]
	[InlineData("Foo@[1.0,2.0)", "Foo", "[1.0,2.0)")]
	[InlineData("Foo@(, 3.0]", "Foo", "(, 3.0]")]
	public void TryParsePackageSpec_AcceptsNuGetIdsAndVersions(string spec, string id, string? version)
	{
		NuGetPackageResolver.TryParsePackageSpec(spec, out string parsedId, out string? parsedVersion).Should().BeTrue();
		parsedId.Should().Be(id);
		parsedVersion.Should().Be(version);
	}

	[Theory]
	[InlineData("")]
	[InlineData("Evil\" /><Target Name=\"X\" BeforeTargets=\"Build\"><Exec Command=\"echo pwned\" /></Target><PackageReference Include=\"A")]
	[InlineData("A;B")]
	[InlineData("$(Secret)")]
	[InlineData("Foo@1.0\" Condition=\"true")]
	[InlineData("Foo@$(Version)")]
	[InlineData("Foo@1.0;Bar")]
	[InlineData("Foo@")]
	[InlineData("@1.0")]
	[InlineData("Foo@1.0@2.0")]
	[InlineData("Foo..Bar")]
	[InlineData(".Foo")]
	[InlineData("Foo Bar")]
	[InlineData("Foo@latest")]
	public void TryParsePackageSpec_RejectsAnythingElse(string spec)
	{
		NuGetPackageResolver.TryParsePackageSpec(spec, out string id, out string? version).Should().BeFalse();
		id.Should().BeEmpty();
		version.Should().BeNull();
	}

	[Fact]
	public void TryParsePackageSpec_RejectsAnIdOverOneHundredCharacters()
	{
		NuGetPackageResolver.TryParsePackageSpec(new string('a', 100), out _, out _).Should().BeTrue();
		NuGetPackageResolver.TryParsePackageSpec(new string('a', 101), out _, out _).Should().BeFalse();
	}

	[Fact]
	public async Task PublishAsync_RefusesASpecThatWouldRewriteTheProjectFile()
	{
		// Specs went into the generated csproj as they were, so a quote in one wrote MSBuild XML.
		string directory = Path.Combine(Path.GetTempPath(), "mokadocs-resolver-" + Guid.NewGuid().ToString("N"));
		var resolver = new NuGetPackageResolver(NullLogger.Instance);

		NuGetPackageResolver.ResolvedPackages result = await resolver.PublishAsync(
			["Humanizer@2.14.1", "Evil\" /><Target Name=\"X\" />"], directory, TestContext.Current.CancellationToken);

		result.Error.Should().Contain("Evil").And.Contain("not a NuGet package id");
		Directory.Exists(directory).Should().BeFalse("nothing is written for a bad spec");
	}

	[Theory]
	[InlineData("System.Runtime", true)]
	[InlineData("System.Text.Json", true)]
	[InlineData("Microsoft.CSharp", true)]
	[InlineData("netstandard", true)]
	[InlineData("System.Reactive", false)]
	[InlineData("System.IO.Abstractions", false)]
	[InlineData("Newtonsoft.Json", false)]
	public void IsSharedFrameworkAssembly_SkipsOnlyWhatTheRuntimeShips(string assemblyName, bool expected)
	{
		// Every "System." file used to be skipped, which dropped packages such as System.Reactive.
		NuGetPackageResolver.IsSharedFrameworkAssembly(assemblyName).Should().Be(expected);
	}
}
