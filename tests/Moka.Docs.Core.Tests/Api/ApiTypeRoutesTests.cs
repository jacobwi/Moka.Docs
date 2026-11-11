using FluentAssertions;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Core.Tests.Api;

/// <summary>
///     Routes and names of type pages for nested types and types that share a name with a different
///     number of type parameters. The CLI's model writes type parameters in full names
///     (<c>Lib.Box&lt;T&gt;.Inner</c>); the ASP.NET Core host's doesn't (<c>Lib.Box.Inner</c>), and its
///     nested types repeat the type parameters of their containing types.
/// </summary>
public sealed class ApiTypeRoutesTests
{
	private static ApiType Type(string name, string fullName, params string[] typeParameters) => new()
	{
		Name = name,
		FullName = fullName,
		Kind = ApiTypeKind.Class,
		Namespace = "Lib",
		TypeParameters = typeParameters.Select(tp => new ApiTypeParameter { Name = tp }).ToList()
	};

	private static List<ApiType> CliModel() =>
	[
		Type("Outer", "Lib.Outer"),
		Type("Options", "Lib.Outer.Options"),
		Type("Other", "Lib.Other"),
		Type("Options", "Lib.Other.Options"),
		Type("Result", "Lib.Result"),
		Type("Result", "Lib.Result<T>", "T"),
		Type("Result", "Lib.Result<T, TError>", "T", "TError"),
		Type("Repository", "Lib.Repository<T>", "T"),
		Type("Widget", "Lib.Widget"),
		Type("Box", "Lib.Box"),
		Type("Box", "Lib.Box<T>", "T"),
		Type("Inner", "Lib.Box.Inner"),
		Type("Inner", "Lib.Box<T>.Inner")
	];

	private static List<ApiType> HostModel() =>
	[
		Type("Outer", "Lib.Outer"),
		Type("Options", "Lib.Outer.Options"),
		Type("Other", "Lib.Other"),
		Type("Options", "Lib.Other.Options"),
		Type("Result", "Lib.Result"),
		Type("Result", "Lib.Result", "T"),
		Type("Result", "Lib.Result", "T", "TError"),
		Type("Repository", "Lib.Repository", "T"),
		Type("Widget", "Lib.Widget"),
		Type("Box", "Lib.Box"),
		Type("Box", "Lib.Box", "T"),
		Type("Inner", "Lib.Box.Inner"),
		Type("Inner", "Lib.Box.Inner", "T")
	];

	private static readonly string[] _routes =
	[
		"/api/lib/outer", "/api/lib/outer.options", "/api/lib/other", "/api/lib/other.options",
		"/api/lib/result", "/api/lib/result-1", "/api/lib/result-2", "/api/lib/repository", "/api/lib/widget",
		"/api/lib/box", "/api/lib/box-1", "/api/lib/box.inner", "/api/lib/box-1.inner"
	];

	private static readonly string[] _names =
	[
		"Outer", "Outer.Options", "Other", "Other.Options", "Result", "Result<T>", "Result<T, TError>",
		"Repository<T>", "Widget", "Box", "Box<T>", "Box.Inner", "Box<T>.Inner"
	];

	private static readonly string[] _ids =
	[
		"Lib.Outer", "Lib.Outer.Options", "Lib.Other", "Lib.Other.Options", "Lib.Result", "Lib.Result`1",
		"Lib.Result`2", "Lib.Repository`1", "Lib.Widget", "Lib.Box", "Lib.Box`1", "Lib.Box.Inner", "Lib.Box`1.Inner"
	];

	[Fact]
	public void CliModel_NestedTypesAndGenericTwins_GetDistinctRoutes()
	{
		List<ApiType> types = CliModel();
		ApiTypeRoutes routes = ApiTypeRoutes.For(types);

		types.Select(routes.Route).Should().Equal(_routes);
		types.Select(routes.DisplayName).Should().Equal(_names);
		types.Select(routes.DocumentationId).Should().Equal(_ids);
	}

	[Fact]
	public void HostModel_GivesTheSameRoutesAsTheCli()
	{
		List<ApiType> types = HostModel();
		ApiTypeRoutes routes = ApiTypeRoutes.For(types);

		types.Select(routes.Route).Should().Equal(_routes);
		types.Select(routes.DisplayName).Should().Equal(_names);
		types.Select(routes.DocumentationId).Should().Equal(_ids);
	}

	[Fact]
	public void Route_ByNamespace_MatchesTheRouteFromAllTypes()
	{
		List<ApiType> types = CliModel();
		var ns = new ApiNamespace { Name = "Lib", Types = types };

		types.Select(type => ApiTypeRoutes.Route(ns, type)).Should().Equal(_routes);
	}

	[Fact]
	public void LoneTypes_KeepTheRouteTheyAlwaysHad()
	{
		// Only nested types and generic types with a same-named sibling move.
		var types = new List<ApiType>
		{
			Type("Parser", "Lib.Parser"),
			Type("Cache", "Lib.Cache<TKey, TValue>", "TKey", "TValue")
		};
		ApiTypeRoutes routes = ApiTypeRoutes.For(types);

		types.Select(routes.Route).Should().Equal("/api/lib/parser", "/api/lib/cache");
	}

	[Fact]
	public void GlobalNamespace_IsListedUnderGlobal()
	{
		ApiType helper = Type("Helper", "Helper") with { Namespace = null };

		ApiTypeRoutes.For([helper]).Route(helper).Should().Be("/api/(global)/helper");
	}

	[Fact]
	public void TypeOutsideTheList_GetsTheRouteItWouldHaveOnItsOwn()
	{
		ApiTypeRoutes routes = ApiTypeRoutes.For(CliModel());

		routes.Route(Type("Result", "Lib.Result<T>", "T")).Should().Be("/api/lib/result");
		routes.Route(Type("Options", "Lib.Settings.Options")).Should().Be("/api/lib/settings.options");
	}
}
