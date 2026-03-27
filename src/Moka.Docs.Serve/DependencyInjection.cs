using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Serve;

/// <summary>
///     Extension methods for registering dev server services.
/// </summary>
public static class ServeServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs dev server services to the service collection.
	/// </summary>
	public static IServiceCollection AddMokaDocsServe(this IServiceCollection services)
	{
		services.AddSingleton<FileWatcher>();
		return services;
	}
}
