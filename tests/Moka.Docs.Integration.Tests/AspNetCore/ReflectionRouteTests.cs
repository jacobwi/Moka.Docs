using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.AspNetCore;
using Moka.Docs.AspNetCore.Phases;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The host builds the same routes as the CLI for nested types and for types that differ only in their
///     number of type parameters, although its model writes full names without type parameters.
/// </summary>
public sealed class ReflectionRouteTests
{
	private const string _source = """
	                               namespace Fixture.Routes
	                               {
	                                   /// <summary>Outer.</summary>
	                                   public class Outer
	                                   {
	                                       /// <summary>Outer's options.</summary>
	                                       public class Options { }
	                                   }

	                                   /// <summary>Other.</summary>
	                                   public class Other
	                                   {
	                                       /// <summary>Other's options.</summary>
	                                       public class Options { }
	                                   }

	                                   /// <summary>A result.</summary>
	                                   public class Result { }

	                                   /// <summary>A result with a value.</summary>
	                                   public class Result<T> : Result
	                                   {
	                                       /// <summary>A value inside a generic result.</summary>
	                                       public class Inner { }
	                                   }

	                                   /// <summary>Links to <see cref="Outer.Options"/> and <see cref="Result{T}.Inner"/>.</summary>
	                                   public class Linker { }
	                               }
	                               """;

	private static readonly Lazy<Assembly> _assembly = new(() =>
		FixtureAssembly.Compile("Moka.Docs.RouteFixture", _source, LanguageVersion.Latest, "route-fixture"));

	private static async Task<InMemorySite> BuildAsync()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options => options.Assemblies = [_assembly.Value]);

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	private static string Html(InMemorySite site, string path)
	{
		site.Files.Keys.Should().Contain(path);
		return Encoding.UTF8.GetString(site.Files[path].Content);
	}

	[Fact]
	public async Task NestedTypesAndGenericTwins_EachGetAPage()
	{
		InMemorySite site = await BuildAsync();

		Html(site, "api/fixture/routes/outer.options/index.html").Should().Contain("Outer's options.");
		Html(site, "api/fixture/routes/other.options/index.html").Should().Contain("Other's options.");
		Html(site, "api/fixture/routes/result/index.html").Should().Contain("A result.");
		Html(site, "api/fixture/routes/result-1/index.html").Should().Contain("A result with a value.");
		Html(site, "api/fixture/routes/result-1.inner/index.html").Should()
			.Contain("A value inside a generic result.")
			.And.Contain("<title>Result&lt;T&gt;.Inner");
	}

	[Fact]
	public async Task IndexRowsAndCrefs_PointAtTheRightPage()
	{
		InMemorySite site = await BuildAsync();

		// The host serves the docs under /docs.
		Html(site, "api/index.html").Should()
			.Contain("<a href=\"/docs/api/fixture/routes/outer.options\">Outer.Options</a>")
			.And.Contain("<a href=\"/docs/api/fixture/routes/other.options\">Other.Options</a>")
			.And.Contain("<a href=\"/docs/api/fixture/routes/result-1\">Result&lt;T&gt;</a>")
			.And.Contain("<a href=\"/docs/api/fixture/routes/result-1.inner\">Result&lt;T&gt;.Inner</a>");
		Html(site, "api/fixture/routes/linker/index.html").Should()
			.Contain("<a href=\"/docs/api/fixture/routes/outer.options\">Outer.Options</a>")
			.And.Contain("<a href=\"/docs/api/fixture/routes/result-1.inner\">Result&lt;T&gt;.Inner</a>");
	}

	[Fact]
	public void NavRoute_MatchesThePageRoute()
	{
		// The sidebar entries NavEntry.AutoGenerate adds link to GetTypeRoute.
		var type = new ApiType { Name = "Options", FullName = "Fixture.Routes.Outer.Options", Kind = ApiTypeKind.Class, Namespace = "Fixture.Routes" };
		var ns = new ApiNamespace { Name = "Fixture.Routes", Types = [type] };

		ReflectionApiPagePhase.GetTypeRoute(ns, type).Should().Be("/api/fixture/routes/outer.options");
	}

	[Fact]
	public async Task NavLabels_UseTheDisplayName()
	{
		// The sidebar listed both nested Options types, and both Result types, under the same label.
		ApiType Type(string name, string fullName, params string[] typeParameters) => new()
		{
			Name = name,
			FullName = fullName,
			Kind = ApiTypeKind.Class,
			Namespace = "Fixture.Routes",
			TypeParameters = typeParameters.Select(p => new ApiTypeParameter { Name = p }).ToList()
		};

		var ns = new ApiNamespace
		{
			Name = "Fixture.Routes",
			Types =
			[
				Type("Options", "Fixture.Routes.Outer.Options"),
				Type("Options", "Fixture.Routes.Other.Options"),
				Type("Result", "Fixture.Routes.Result"),
				Type("Result", "Fixture.Routes.Result`1", "T")
			]
		};
		var options = new MokaDocsOptions
		{
			Nav = [new NavEntry { Label = "API Reference", Path = "/api", AutoGenerate = true }]
		};
		var context = new BuildContext
		{
			Config = SiteConfigFactory.Create(options),
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site",
			ApiModel = new ApiReference { Namespaces = [ns] }
		};

		// The host's sidebar comes from the same navigation phase as the CLI's.
		await new ReflectionApiPagePhase(NullLogger<ReflectionApiPagePhase>.Instance)
			.ExecuteAsync(context, TestContext.Current.CancellationToken);
		await new NavigationBuildPhase(NullLogger<NavigationBuildPhase>.Instance)
			.ExecuteAsync(context, TestContext.Current.CancellationToken);

		NavigationNode group = context.Navigation!.Items.Single().Children.Single();
		group.Label.Should().Be("Fixture.Routes");
		group.Children.Select(c => (c.Label, c.Route)).Should().Equal(
			("Other.Options", "/api/fixture/routes/other.options"),
			("Outer.Options", "/api/fixture/routes/outer.options"),
			("Result", "/api/fixture/routes/result"),
			("Result<T>", "/api/fixture/routes/result-1"));
	}
}
