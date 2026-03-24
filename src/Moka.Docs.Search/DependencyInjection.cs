using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.Search;

/// <summary>
///     Extension methods for registering search services.
/// </summary>
public static class SearchServiceExtensions
{
    /// <summary>
    ///     Adds MokaDocs search services to the service collection.
    /// </summary>
    public static IServiceCollection AddMokaDocsSearch(this IServiceCollection services)
    {
        // Will be populated in Phase 7
        return services;
    }
}