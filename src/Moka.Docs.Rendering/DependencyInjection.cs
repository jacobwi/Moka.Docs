using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Rendering;

/// <summary>
///     Extension methods for registering rendering services.
/// </summary>
public static class RenderingServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs rendering services to the service collection.
	/// </summary>
	public static IServiceCollection AddMokaDocsRendering(this IServiceCollection services)
	{
		// Will be populated in Phase 6
		return services;
	}
}
