using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Themes;

/// <summary>
///     Extension methods for registering theme services.
/// </summary>
public static class ThemeServiceExtensions
{
	/// <summary>
	///     Adds <see cref="ThemeLoader" /> and <see cref="ThemeResolver" />. Requires an
	///     <c>IFileSystem</c> and logging to be registered.
	/// </summary>
	public static IServiceCollection AddMokaDocsThemes(this IServiceCollection services)
	{
		services.TryAddSingleton<ThemeLoader>();
		services.TryAddSingleton<ThemeResolver>();
		return services;
	}
}
