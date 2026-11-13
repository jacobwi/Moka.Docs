using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     Extension methods for mapping MokaDocs documentation endpoints.
/// </summary>
public static class MokaDocsEndpointExtensions
{
	private const string _previewHostPrefix = "_preview-wasm/";
	private const string _previewFrameworkPrefix = "_preview-wasm/_framework/";
	private const string _previewAssembliesPrefix = "_preview-assemblies/";

	/// <summary>A year, the longest <c>max-age</c> caches honor, as Map Static Assets sends for fingerprinted files.</summary>
	private const string _cacheForever = "max-age=31536000, immutable";

	private static readonly string[] _readMethods = [HttpMethods.Get, HttpMethods.Head];

	// In the site's language, since it shows before the first build: the build reports problems with the options.
	private static string BuildingPage(MokaDocsOptions options)
	{
		ThemeText text = ThemeText.For(options.Language, options.Strings);
		string direction = SiteLanguages.Direction(text.Language, options.Direction);
		return $"<!DOCTYPE html>\n<html lang=\"{WebUtility.HtmlEncode(text.Language)}\" dir=\"{direction}\">\n<head>\n<meta charset=\"utf-8\">\n" +
		       $"<meta http-equiv=\"refresh\" content=\"{MokaDocsService.BuildingRetryAfterSeconds}\">\n" +
		       $"<title>{WebUtility.HtmlEncode(text["building.title"])}</title>\n</head>\n<body>\n" +
		       $"<p>{WebUtility.HtmlEncode(text["building.text"])}</p>\n</body>\n</html>\n";
	}

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
	///     <see cref="MokaDocsOptions.AllowRemoteRepl" /> is set. Conventions for one of those two
	///     alone go in <see cref="MokaDocsOptions.ConfigureFeedbackEndpoint" /> and
	///     <see cref="MokaDocsOptions.ConfigureReplEndpoint" />.
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

		group.MapMethods("/", _readMethods, context => ServeDocsAsync(context, docsService, options));
		group.MapMethods("/{**path}", _readMethods, context => ServeDocsAsync(context, docsService, options));

		if (options.ShowFeedback)
		{
			IEndpointConventionBuilder feedback = group.MapPost("/api/feedback", context =>
			{
				context.Response.Headers.XContentTypeOptions = "nosniff";
				return MokaDocsApiEndpoints.HandleFeedbackAsync(context, options, logger);
			});
			options.ConfigureFeedbackEndpoint?.Invoke(feedback);
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
		IEndpointConventionBuilder endpoint = group.MapPost("/api/repl/execute", context =>
		{
			context.Response.Headers.XContentTypeOptions = "nosniff";
			return MokaDocsApiEndpoints.HandleReplAsync(context, repl, logger);
		});
		options.ConfigureReplEndpoint?.Invoke(endpoint);
	}

	private static async Task ServeDocsAsync(HttpContext context, MokaDocsService docsService, MokaDocsOptions options)
	{
		HttpResponse response = context.Response;
		bool writeBody = !HttpMethods.IsHead(context.Request.Method);
		response.Headers.XContentTypeOptions = "nosniff";
		docsService.CheckPathBase(context.Request.PathBase.Value);

		// Without a cache every request builds the site, so it has to wait for its build.
		MokaDocsService.SiteResult result = options.CacheOutput
			? docsService.GetSiteWithoutWaiting()
			: await docsService.GetSiteOrFailureAsync(context.RequestAborted);
		if (result.Site is not { } site)
		{
			// The service logged a failure once; an exception here would log it again for every request.
			await (result.Failure is null
				? WriteBuildingAsync(context, options, writeBody)
				: WriteFailedAsync(context, docsService, writeBody));
			return;
		}

		string requestPath = (context.GetRouteValue("path") as string)?.Trim('/') ?? "";
		if (ResolveFile(site, requestPath) is not (string key, SiteFile file))
		{
			response.StatusCode = StatusCodes.Status404NotFound;
			response.Headers.CacheControl = "no-cache";
			if (site.Files.GetValueOrDefault("404.html") is { } notFound)
			{
				await WriteFileAsync(context, notFound, NegotiateEncoding(context, notFound), writeBody);
			}

			return;
		}

		SiteFileEncoding? encoding = NegotiateEncoding(context, file);
		string etag = encoding?.ETag ?? file.ETag;
		response.Headers.ETag = etag;
		response.Headers.CacheControl = CacheControlFor(context, key, file);
		if (key.StartsWith(_previewHostPrefix, StringComparison.OrdinalIgnoreCase))
		{
			AddPreviewHostHeaders(response.Headers);
		}

		if (IsNotModified(context.Request, etag))
		{
			response.StatusCode = StatusCodes.Status304NotModified;
			return;
		}

		await WriteFileAsync(context, file, encoding, writeBody);
	}

	/// <summary>
	///     The compressed form to send, or <c>null</c> for the file as it is. A file with compressed
	///     forms gets <c>Vary: Accept-Encoding</c> whichever is sent, so a shared cache doesn't hand a
	///     Brotli response to a client that can't read it (the rule ASP.NET Core 11's response
	///     compression applies to every response).
	/// </summary>
	private static SiteFileEncoding? NegotiateEncoding(HttpContext context, SiteFile file)
	{
		if (file.Encodings.Count == 0)
		{
			return null;
		}

		context.Response.Headers.Vary = HeaderNames.AcceptEncoding;
		return SiteCompression.Select(context.Request.Headers.AcceptEncoding, file);
	}

	private static async Task WriteFileAsync(HttpContext context, SiteFile file, SiteFileEncoding? encoding,
		bool writeBody)
	{
		HttpResponse response = context.Response;
		byte[] body = encoding?.Content ?? file.Content;
		response.ContentType = file.ContentType;
		if (encoding is not null)
		{
			response.Headers.ContentEncoding = encoding.Name;
		}

		response.ContentLength = body.Length;
		if (writeBody)
		{
			await response.Body.WriteAsync(body, context.RequestAborted);
		}
	}

	/// <summary>
	///     <c>Cache-Control</c> for a file. Most URLs stay the same when the file changes, such as
	///     every page, <c>search-index.json</c> and the preview host's <c>index.html</c> and
	///     <c>dotnet.js</c>: they get <c>no-cache</c>, so a browser revalidates with the
	///     <c>ETag</c> for the cost of a <c>304</c> and sees a rebuild at once. A URL that names the
	///     content can't serve anything else, so it's cached for a year without revalidation.
	/// </summary>
	private static string CacheControlFor(HttpContext context, string key, SiteFile file)
	{
		if (!IsImmutable(context.Request.Query, key, file))
		{
			return "no-cache";
		}

		// A shared cache keeps what it stores for everyone. Docs that only signed-in readers see
		// stay in each reader's own browser.
		return (context.User.Identity?.IsAuthenticated == true ? "private, " : "public, ") + _cacheForever;
	}

	/// <summary>
	///     Whether a URL names the content it serves: a <c>?v=</c> that is a prefix of the file's
	///     content hash (theme CSS and JS, the shared sidebar script), a fingerprinted file in the
	///     Blazor preview host's <c>_framework</c> folder, or a preview assembly named after the
	///     SHA-256 hash of its bytes.
	/// </summary>
	internal static bool IsImmutable(IQueryCollection query, string key, SiteFile file)
	{
		if (query.TryGetValue("v", out StringValues version) && version.Count == 1
		                                                   && file.MatchesContentHash(version[0]))
		{
			return true;
		}

		string name = key[(key.LastIndexOf('/') + 1)..];
		if (key.StartsWith(_previewFrameworkPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return HasFingerprint(name);
		}

		return key.StartsWith(_previewAssembliesPrefix, StringComparison.OrdinalIgnoreCase)
		       && key.Length == _previewAssembliesPrefix.Length + name.Length
		       && name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)
		       && file.MatchesContentHash(name.AsSpan(0, name.Length - ".bin".Length));
	}

	/// <summary>
	///     Whether a published Blazor file name carries the fingerprint the SDK puts in front of the
	///     extension: ten lowercase letters and digits, as in <c>dotnet.native.3bptkeu1pi.wasm</c>.
	///     <c>dotnet.js</c> and an unfingerprinted <c>blazor.webassembly.js</c> keep their names
	///     from one publish to the next.
	/// </summary>
	internal static bool HasFingerprint(string fileName)
	{
		string[] parts = fileName.Split('.');
		return parts.Length >= 3
		       && parts[^2] is { Length: 10 } fingerprint
		       && fingerprint.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterLower(c));
	}

	/// <summary>
	///     Headers for the Blazor preview host, which pages load in an iframe. Only the docs' own
	///     pages may frame it.
	/// </summary>
	/// <remarks>
	///     The host's <c>index.html</c> carries its full content security policy in a meta tag, with
	///     hashes of its inline scripts. A <c>script-src</c> here would apply on top of that one and
	///     would have to allow the same hashes, so the header holds what a meta tag can't express.
	/// </remarks>
	private static void AddPreviewHostHeaders(IHeaderDictionary headers)
	{
		headers.ContentSecurityPolicy = "frame-ancestors 'self'";
		headers["Cross-Origin-Opener-Policy"] = "same-origin";
	}

	/// <summary>
	///     Answers a request that arrives before the first build is done. Browsers get a page that
	///     reloads itself; other clients get the <c>Retry-After</c> header.
	/// </summary>
	private static Task WriteBuildingAsync(HttpContext context, MokaDocsOptions options, bool writeBody)
	{
		HttpResponse response = context.Response;
		response.StatusCode = StatusCodes.Status503ServiceUnavailable;
		response.Headers.RetryAfter = MokaDocsService.BuildingRetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
		response.Headers.CacheControl = "no-store";

		bool html = context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
		response.ContentType = html ? "text/html; charset=utf-8" : "text/plain; charset=utf-8";
		return writeBody
			? response.WriteAsync(html ? BuildingPage(options) : "The documentation is being built. Try again in a few seconds.",
				context.RequestAborted)
			: Task.CompletedTask;
	}

	private static Task WriteFailedAsync(HttpContext context, MokaDocsService docsService, bool writeBody)
	{
		HttpResponse response = context.Response;
		response.StatusCode = StatusCodes.Status503ServiceUnavailable;
		response.Headers.RetryAfter = Math.Ceiling(docsService.FailureRetryDelay.TotalSeconds)
			.ToString(CultureInfo.InvariantCulture);
		response.Headers.CacheControl = "no-store";
		response.ContentType = "text/plain; charset=utf-8";
		return writeBody
			? response.WriteAsync("The documentation failed to build. The application's log has the error.",
				context.RequestAborted)
			: Task.CompletedTask;
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
	///     Resolves a request path to a file in the in-memory site, and the file's key.
	///     Tries: exact match → /index.html → .html suffix.
	/// </summary>
	private static (string Key, SiteFile File)? ResolveFile(InMemorySite site, string path)
	{
		// Exact match (for assets like _theme/css/main.css), directory index ("api" → "api/index.html"),
		// then an HTML file without its extension ("guide/getting-started" → "guide/getting-started.html").
		foreach (string key in (string[]) [path, path + "/index.html", path + ".html"])
		{
			if (site.Files.TryGetValue(key, out SiteFile? file))
			{
				return (key, file);
			}
		}

		// Root request
		return path.Length == 0 && site.Files.TryGetValue("index.html", out SiteFile? root) ? ("index.html", root) : null;
	}
}
