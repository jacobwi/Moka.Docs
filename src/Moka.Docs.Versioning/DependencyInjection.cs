using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Versioning;

/// <summary>
///     Extension methods for registering versioning services.
/// </summary>
public static class VersioningServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs versioning services to the service collection.
	///     The <see cref="VersionManager" /> reads version definitions from the site configuration
	///     and provides helpers for resolving output paths and default versions.
	/// </summary>
	public static IServiceCollection AddMokaDocsVersioning(this IServiceCollection services)
	{
		services.AddSingleton<VersionManager>();
		return services;
	}
}
