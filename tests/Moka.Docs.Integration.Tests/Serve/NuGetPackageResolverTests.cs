using FluentAssertions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

public sealed class NuGetPackageResolverTests
{
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
