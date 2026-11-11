using System.Text.Json;
using FluentAssertions;

namespace Moka.Docs.Integration.Tests;

/// <summary>
///     What the packages depend on, read from this test project's deps.json, which covers the CLI,
///     the ASP.NET Core host and everything they reference.
/// </summary>
public sealed class PackageGraphTests
{
	private static readonly Lazy<Dictionary<string, string[]>> _libraries = new(() =>
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Moka.Docs.Integration.Tests.deps.json");
		using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(path));
		JsonElement target = deps.RootElement.GetProperty("targets").EnumerateObject().First().Value;
		return target.EnumerateObject().ToDictionary(
			library => library.Name[..library.Name.IndexOf('/')],
			library => library.Value.TryGetProperty("dependencies", out JsonElement dependencies)
				? dependencies.EnumerateObject().Select(d => d.Name).ToArray()
				: []);
	});

	[Fact]
	public void MokaDocsSearch_IsNotPartOfAnyPackage()
	{
		// It only held an AddMokaDocsSearch() that registered nothing; 1.8.0 was its last release.
		_libraries.Value.Keys.Should().Contain("mokadocs").And.Contain("Moka.Docs.AspNetCore");
		_libraries.Value.Keys.Should().NotContain("Moka.Docs.Search");
		_libraries.Value.Should().NotContain(l => l.Value.Contains("Moka.Docs.Search"));
	}

	[Fact]
	public void BlazorReplCompiler_IsReferencedByThePluginsThatUseIt_NotByServe()
	{
		// Serve kept the reference after its Blazor preview endpoint was removed, so every app
		// referencing Moka.Docs.Serve pulled in the Razor compiler and the NuGet client.
		_libraries.Value["Moka.Docs.Serve"].Should().NotContain(d => d.StartsWith("Moka.Blazor.", StringComparison.Ordinal));
		_libraries.Value["Moka.Docs.Plugins"].Should().Contain("Moka.Blazor.Repl.Compiler");
		_libraries.Value["mokadocs"].Should().Contain("Moka.Docs.Plugins", "the CLI tool still ships the compiler");
	}
}
