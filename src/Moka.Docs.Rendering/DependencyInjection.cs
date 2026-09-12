using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Docs.Rendering.Scriban;

namespace Moka.Docs.Rendering;

/// <summary>
///     Extension methods for registering rendering services.
/// </summary>
public static class RenderingServiceExtensions
{
	/// <summary>
	///     Adds the <see cref="ScribanTemplateEngine" />. Requires logging to be registered.
	/// </summary>
	public static IServiceCollection AddMokaDocsRendering(this IServiceCollection services)
	{
		services.TryAddSingleton<ScribanTemplateEngine>();
		return services;
	}
}
