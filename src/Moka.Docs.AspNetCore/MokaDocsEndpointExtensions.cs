using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Extension methods for mapping MokaDocs documentation endpoints.
/// </summary>
public static class MokaDocsEndpointExtensions
{
	private static readonly string[] _readMethods = [HttpMethods.Get, HttpMethods.Head];

	/// <summary>
	///     Maps the MokaDocs documentation site at the configured base path.
	///     Serves the fully-generated documentation site including API reference,
	///     guides, search, and all theme assets.
	/// </summary>
	/// <remarks>
	///     Pages and assets answer GET and HEAD. The group also holds
	///     <c>POST {BasePath}/api/feedback</c> when <see cref="MokaDocsOptions.ShowFeedback" /> is
	///     set, and <c>POST {BasePath}/api/repl/execute</c> when <see cref="MokaDocsOptions.EnableRepl" />
	///     is set and the environment is Development or
	///     <see cref="MokaDocsOptions.AllowRemoteRepl" /> is set.
	/// </remarks>
	/// <param name="endpoints">The endpoint route builder.</param>
	/// <param name="basePath">
	///     Optional override for the URL path prefix (default: uses <see cref="MokaDocsOptions.BasePath" />).
	///     <c>"/"</c> serves the docs at the root of the app.
	/// </param>
	/// <returns>
	///     The route group holding every docs endpoint, so conventions such as
	///     <c>RequireAuthorization()</c> chain onto it and cover them all.
	/// </returns>
	public static RouteGroupBuilder MapMokaDocs(
		this IEndpointRouteBuilder endpoints,
		string? basePath = null)
	{
		IServiceProvider services = endpoints.ServiceProvider;
		MokaDocsOptions options = services.GetRequiredService<MokaDocsOptions>();
		MokaDocsService docsService = services.GetRequiredService<MokaDocsService>();
		ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MokaDocsEndpointExtensions));
		string effectiveBase = (basePath ?? options.BasePath).Trim('/');

		// The site is built with its base path in every link, so an override has to reach the
		// build too. Mapping it here alone served pages whose styles and links pointed at the
		// old path.
		if (basePath is not null)
		{
			options.BasePath = "/" + effectiveBase;
		}

		// A root base path used to produce the pattern "//{**path}", which matched nothing.
		RouteGroupBuilder group = endpoints.MapGroup("/" + effectiveBase);

		group.MapMethods("/", _readMethods, context => ServeDocsAsync(context, docsService));
		group.MapMethods("/{**path}", _readMethods, context => ServeDocsAsync(context, docsService));

		if (options.ShowFeedback)
		{
			group.MapPost("/api/feedback", context => MokaDocsApiEndpoints.HandleFeedbackAsync(context, options, logger));
		}

		if (options.EnableRepl)
		{
			MapRepl(group, services, options, logger);
		}

		docsService.IsMapped = true;
		return group;
	}

	private static void MapRepl(RouteGroupBuilder group, IServiceProvider services, MokaDocsOptions options,
		ILogger logger)
	{
		// The endpoint runs any code it is sent, as the app's user. Outside Development that is
		// remote code execution for anyone who can reach it, so it takes an explicit opt-in.
		bool isDevelopment = services.GetService<IHostEnvironment>()?.IsDevelopment() == true;
		if (!isDevelopment && !options.AllowRemoteRepl)
		{
			logger.LogWarning(
				"EnableRepl is on, but the REPL endpoint is not mapped outside the Development environment, " +
				"so Run buttons can't execute code. Set AllowRemoteRepl to map it, behind authorization");
			return;
		}

		MokaDocsRepl repl = services.GetRequiredService<MokaDocsRepl>();
		repl.IsMapped = true;
		group.MapPost("/api/repl/execute", context => MokaDocsApiEndpoints.HandleReplAsync(context, repl, logger));
	}

	private static async Task ServeDocsAsync(HttpContext context, MokaDocsService docsService)
	{
		InMemorySite site = await docsService.GetSiteAsync(context.RequestAborted);

		string requestPath = (context.GetRouteValue("path") as string)?.Trim('/') ?? "";
		SiteFile? file = ResolveFile(site, requestPath);
		HttpResponse response = context.Response;
		bool writeBody = !HttpMethods.IsHead(context.Request.Method);

		if (file is null)
		{
			response.StatusCode = StatusCodes.Status404NotFound;

			// Try serving a 404 page from the built site
			file = site.Files.GetValueOrDefault("404.html");
			if (file is not null)
			{
				response.ContentType = file.ContentType;
				response.ContentLength = file.Content.Length;
				if (writeBody)
				{
					await response.Body.WriteAsync(file.Content, context.RequestAborted);
				}
			}

			return;
		}

		// Static assets are cached for a day. Everything else, pages and the search index
		// included, changes with each rebuild, so browsers revalidate it with the ETag.
		response.Headers.ETag = file.ETag;
		response.Headers.CacheControl = IsStaticAsset(Path.GetExtension(requestPath))
			? "public, max-age=86400"
			: "no-cache";

		if (IsNotModified(context.Request, file.ETag))
		{
			response.StatusCode = StatusCodes.Status304NotModified;
			return;
		}

		response.ContentType = file.ContentType;
		response.ContentLength = file.Content.Length;
		if (writeBody)
		{
			await response.Body.WriteAsync(file.Content, context.RequestAborted);
		}
	}

	/// <summary>
	///     Whether the request's <c>If-None-Match</c> names the file's current tag. The header
	///     uses weak comparison, so <c>W/"tag"</c> matches too.
	/// </summary>
	private static bool IsNotModified(HttpRequest request, string etag)
	{
		if (!EntityTagHeaderValue.TryParseList(request.Headers.IfNoneMatch, out IList<EntityTagHeaderValue>? tags))
		{
			return false;
		}

		var current = new EntityTagHeaderValue(etag);
		return tags.Any(tag => tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(current, false));
	}

	/// <summary>
	///     Resolves a request path to a file in the in-memory site.
	///     Tries: exact match → /index.html → .html suffix.
	/// </summary>
	private static SiteFile? ResolveFile(InMemorySite site, string path)
	{
		// Exact match (for assets like _theme/css/main.css)
		if (site.Files.TryGetValue(path, out SiteFile? exact))
		{
			return exact;
		}

		// Directory index (e.g., "api" → "api/index.html")
		if (site.Files.TryGetValue(path + "/index.html", out SiteFile? index))
		{
			return index;
		}

		// HTML file without extension (e.g., "guide/getting-started" → "guide/getting-started.html")
		if (site.Files.TryGetValue(path + ".html", out SiteFile? html))
		{
			return html;
		}

		// Root request
		if (string.IsNullOrEmpty(path) && site.Files.TryGetValue("index.html", out SiteFile? root))
		{
			return root;
		}

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
