using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Cloud;

/// <summary>
///     Extension methods for registering cloud services.
///     All cloud features are OFF by default and require explicit opt-in.
/// </summary>
public static class CloudServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs cloud services to the service collection.
	///     Services are always registered but behave as no-ops when
	///     cloud is disabled in configuration.
	/// </summary>
	public static IServiceCollection AddMokaDocsCloud(this IServiceCollection services)
	{
		services.AddSingleton<CloudFeatureService>();
		services.AddSingleton<CredentialStore>();
		return services;
	}
}
