using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Themes;

/// <summary>
///     Extension methods for registering theme services.
/// </summary>
public static class ThemeServiceExtensions
{
    /// <summary>
    ///     Adds MokaDocs theme services to the service collection.
    /// </summary>
    public static IServiceCollection AddMokaDocsThemes(this IServiceCollection services)
    {
        // Will be populated in Phase 6
        return services;
    }
}