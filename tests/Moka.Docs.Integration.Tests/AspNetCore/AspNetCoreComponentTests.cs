using FluentAssertions;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The host's pages get <c>xref:</c> links to its reflection-built API pages, and a glossary from
///     <see cref="MokaDocsOptions.Glossary" /> and a glossary.yml file in the docs folder.
/// </summary>
public sealed class AspNetCoreComponentTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-host-components-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreComponentTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n# Home\n\nA [square](xref:Fixture.Shapes.Square), <xref:Fixture.Shapes.Shape.Scale> " +
			"and <xref:Fixture.Shapes.Missing>.\n\nThe :term[Shape] and the :term[Point].\n");
		File.WriteAllText(Path.Combine(_root, "docs", "glossary.yml"), "Point: A place on the plane.\n");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task Host_ResolvesXrefsAgainstTheReflectionModel_AndReadsTheGlossary()
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.Assemblies = [FixtureAssembly.Instance];
			options.Glossary = new Dictionary<string, string> { ["Shape"] = "Anything with an area." };
		});

		string home = await host.GetTextAsync("/docs/");
		home.Should().Contain("<a href=\"/docs/api/fixture/shapes/square\" class=\"xref\">square</a>")
			.And.Contain("href=\"/docs/api/fixture/shapes/shape#scale\" class=\"xref\">Shape.Scale</a>")
			.And.Contain("<code>Fixture.Shapes.Missing</code>")
			.And.Contain("data-no-search>Anything with an area.</span>")
			.And.Contain("data-no-search>A place on the plane.</span>");
		(await host.GetTextAsync("/docs/api/fixture/shapes/shape/")).Should().Contain("id=\"scale\"");

		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		site.Diagnostics.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Warning && d.Message.Contains("xref:"))
			.Which.Message.Should().Contain("xref:Fixture.Shapes.Missing doesn't match");
		site.Files.Keys.Should().NotContain(path => path.EndsWith("glossary.yml"));
	}
}
