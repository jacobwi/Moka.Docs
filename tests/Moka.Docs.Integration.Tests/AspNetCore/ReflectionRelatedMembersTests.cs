using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Inherited members and extension methods on the type pages the ASP.NET Core host builds by
///     reflection, where every type name is written without its namespace.
/// </summary>
public sealed class ReflectionRelatedMembersTests
{
	private static async Task<InMemorySite> BuildAsync()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options => options.Assemblies = [FixtureAssembly.Instance]);

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	private static string Html(InMemorySite site, string path)
	{
		site.Files.Keys.Should().Contain(path);
		return Encoding.UTF8.GetString(site.Files[path].Content);
	}

	/// <summary>
	///     The HTML of one section, from its heading to the end of its table.
	/// </summary>
	private static string Section(string html, string id)
	{
		int start = html.IndexOf($"<h2 id=\"{id}\">", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the page should have a section with id {id}");
		return html[start..html.IndexOf("</table>", start, StringComparison.Ordinal)];
	}

	[Fact]
	public async Task DerivedTypePages_ListInheritedMembersAndExtensionMethods()
	{
		InMemorySite site = await BuildAsync();
		string square = Html(site, "api/fixture/shapes/square/index.html");
		string circle = Html(site, "api/fixture/shapes/circle/index.html");

		string inherited = Section(square, "inherited-members");
		inherited.Should().Contain("/api/fixture/shapes/shape\">Shape</a></td>")
			.And.Contain("/api/fixture/shapes/shape#name\" class=\"api-member-link\"><code>Name</code></a>")
			.And.Contain("/api/fixture/shapes/shape#changed\" class=\"api-member-link\"><code>Changed</code></a>")
			.And.Contain("/api/fixture/shapes/shape#onchanged\" class=\"api-member-link\"><code>OnChanged()</code></a>");

		// Square overrides Area and Scale, and constructors are never inherited.
		inherited.Should().NotContain("#area\"")
			.And.NotContain("#scale\"")
			.And.NotContain("/api/fixture/shapes/shape#shape");

		// Circle is sealed, so the protected members it inherits are out of reach. The model doesn't have
		// its own protected OnChanged override for the same reason.
		inherited.Should().Contain("/api/fixture/shapes/shape#revision\"");
		Section(circle, "inherited-members").Should().Contain("#name\"")
			.And.NotContain("#onchanged\"")
			.And.NotContain("#revision\"");

		Section(square, "extension-methods").Should()
			.Contain("/api/fixture/shapes/shapeextensions#describe\" class=\"api-member-link\"><code>Describe(Shape, string?, int[])</code></a>")
			.And.Contain("/api/fixture/shapes/shapeextensions\">ShapeExtensions</a></td>");
	}

	[Fact]
	public async Task TypePages_WithNothingToList_HaveNeitherSection()
	{
		InMemorySite site = await BuildAsync();

		// Shape has the extension method but no documented base type; Money has neither.
		string shape = Html(site, "api/fixture/shapes/shape/index.html");
		shape.Should().NotContain("id=\"inherited-members\"").And.Contain("id=\"extension-methods\"");
		Html(site, "api/fixture/shapes/money/index.html").Should()
			.NotContain("id=\"inherited-members\"").And.NotContain("id=\"extension-methods\"");
	}
}
