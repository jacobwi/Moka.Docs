using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Extension methods for mapping MokaDocs documentation endpoints.
/// </summary>
public static class MokaDocsEndpointExtensions
{
    /// <summary>
    ///     Maps the MokaDocs documentation site at the configured base path.
    ///     Serves the fully-generated documentation site including API reference,
    ///     guides, search, and all theme assets.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="basePath">
    ///     Optional override for the URL path prefix (default: uses <see cref="MokaDocsOptions.BasePath" />).
    /// </param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapMokaDocs(
        this IEndpointRouteBuilder endpoints,
        string? basePath = null)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<MokaDocsOptions>();
        var effectiveBase = (basePath ?? options.BasePath).Trim('/');

        // Shared handler for serving docs pages
        async Task ServeDocs(HttpContext context, string? path)
        {
            var docsService = context.RequestServices.GetRequiredService<MokaDocsService>();
            var site = await docsService.GetSiteAsync(context.RequestAborted);

            var requestPath = path?.Trim('/') ?? "";
            var file = ResolveFile(site, requestPath);

            if (file is null)
            {
                context.Response.StatusCode = 404;

                // Try serving a 404 page from the built site
                file = site.Files.GetValueOrDefault("404.html");
                if (file is not null)
                {
                    context.Response.ContentType = file.ContentType;
                    await context.Response.Body.WriteAsync(file.Content, context.RequestAborted);
                }

                return;
            }

            // Set cache headers for static assets (CSS, JS, fonts, images)
            var extension = Path.GetExtension(requestPath);
            if (IsStaticAsset(extension)) context.Response.Headers.CacheControl = "public, max-age=86400"; // 24 hours

            context.Response.ContentType = file.ContentType;
            context.Response.ContentLength = file.Content.Length;
            await context.Response.Body.WriteAsync(file.Content, context.RequestAborted);
        }

        // Catch-all route for the documentation site (handles /docs/anything)
        endpoints.Map($"/{effectiveBase}/{{**path}}", (HttpContext ctx, string? path) => ServeDocs(ctx, path));

        // Root route: /docs and /docs/ (serves index.html)
        endpoints.Map($"/{effectiveBase}", ctx => ServeDocs(ctx, null));

        return endpoints;
    }

    /// <summary>
    ///     Resolves a request path to a file in the in-memory site.
    ///     Tries: exact match → /index.html → .html suffix.
    /// </summary>
    private static SiteFile? ResolveFile(InMemorySite site, string path)
    {
        // Exact match (for assets like _theme/css/main.css)
        if (site.Files.TryGetValue(path, out var exact))
            return exact;

        // Directory index (e.g., "api" → "api/index.html")
        if (site.Files.TryGetValue(path + "/index.html", out var index))
            return index;

        // HTML file without extension (e.g., "guide/getting-started" → "guide/getting-started.html")
        if (site.Files.TryGetValue(path + ".html", out var html))
            return html;

        // Root request
        if (string.IsNullOrEmpty(path) && site.Files.TryGetValue("index.html", out var root))
            return root;

        return null;
    }

    /// <summary>
    ///     Returns true for file extensions that should receive cache headers.
    /// </summary>
    private static bool IsStaticAsset(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".css" or ".js" or ".woff" or ".woff2" or ".ttf" or ".eot" => true,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".ico" or ".webp" => true,
            _ => false
        };
    }
}