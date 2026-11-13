using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moka.Docs.Serve;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     The POST endpoints in the docs group: the feedback widget's and the REPL's.
/// </summary>
internal static class MokaDocsApiEndpoints
{
	/// <summary>
	///     Headers a proxy adds. The <c>X-Original-*</c> ones are what <c>UseForwardedHeaders()</c>
	///     leaves after it applies the others.
	/// </summary>
	private static readonly string[] _proxyHeaders =
		["Forwarded", "X-Forwarded-For", "X-Forwarded-Host", "X-Original-For", "X-Original-Host"];

	/// <summary>Handles <c>POST {BasePath}/api/feedback</c>.</summary>
	internal static async Task HandleFeedbackAsync(HttpContext context, MokaDocsOptions options, ILogger logger)
	{
		if (!IsSameOriginRequest(context.Request))
		{
			Reject(context, logger);
			return;
		}

		if (await DocsApiRequests.ReadFeedbackAsync(context) is not { } vote)
		{
			return;
		}

		logger.LogInformation("Docs feedback for {Page}: {Vote}", vote.Page, vote.Helpful ? "helpful" : "not helpful");

		if (options.OnFeedback is { } onFeedback)
		{
			try
			{
				await onFeedback(new DocsFeedback(vote.Page, vote.Helpful), context);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				logger.LogError(ex, "MokaDocsOptions.OnFeedback failed for {Page}", vote.Page);
				await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status500InternalServerError, new { ok = false });
				return;
			}
		}

		await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status200OK, new { ok = true });
	}

	/// <summary>Handles <c>POST {BasePath}/api/repl/execute</c>.</summary>
	internal static async Task HandleReplAsync(HttpContext context, MokaDocsRepl repl, ILogger logger)
	{
		if (!IsSameOriginRequest(context.Request))
		{
			Reject(context, logger);
			return;
		}

		// The origin checks stop other web pages, not other machines: an app in Development that
		// listens on every interface, as containers often do, would run code for anyone on the network.
		if (!repl.AllowRemote && LocalOnlyRefusal(context.Request, context.Connection.RemoteIpAddress) is { } refusal)
		{
			logger.LogWarning("Refused a REPL run: {Reason}. Set AllowRemoteRepl to accept it, behind authorization",
				refusal);
			await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status403Forbidden,
				new { error = "The REPL only runs code for requests from the machine the app runs on." });
			return;
		}

		// Starting the executable again without the worker check in Program.cs would start a
		// second copy of the whole web app, so nothing is started.
		if (!repl.CanStartWorker)
		{
			repl.WarnNotWiredOnce();
			await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status503ServiceUnavailable,
				new { error = MokaDocsRepl.NotWiredMessage });
			return;
		}

		if (await DocsApiRequests.ReadReplCodeAsync(context) is not { } code)
		{
			return;
		}

		// A reader who leaves mid-run cancels the run, and the service replaces the worker, so the
		// snippet stops instead of running on until the timeout.
		await DocsApiRequests.RunReplAsync(context, repl.Service, code, context.RequestAborted);
	}

	/// <summary>
	///     Whether a POST may run. Any web page open in a reader's browser can send a request to
	///     this app; only the docs' own pages may use these endpoints.
	/// </summary>
	/// <remarks>
	///     A cross-origin page can POST without a CORS preflight as long as the body is
	///     text/plain or a form. Requiring a JSON content type forces a preflight, which nothing
	///     here answers, and a browser request that does arrive must come from this app's own
	///     origin. Requests without Origin or Sec-Fetch-Site headers don't come from a browser
	///     page, so tools like curl still work. The dev server's <c>IsSameOriginApiRequest</c> differs
	///     on purpose: it only knows its loopback http origins, and lets <c>Sec-Fetch-Site: none</c>
	///     through.
	/// </remarks>
	internal static bool IsSameOriginRequest(HttpRequest request)
	{
		if (request.ContentType is not { } contentType
		    || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string? fetchSite = request.Headers["Sec-Fetch-Site"];
		if (fetchSite is not null && !fetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string? origin = request.Headers.Origin;
		return origin is null || IsRequestOrigin(origin, request);
	}

	/// <summary>
	///     Whether <paramref name="origin" /> is the scheme, host and port the request was sent to.
	///     Behind a proxy that terminates TLS, the forwarded headers middleware has to restore the
	///     original scheme and host, or every browser request is rejected.
	/// </summary>
	private static bool IsRequestOrigin(string origin, HttpRequest request)
	{
		if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri) || !request.Host.HasValue)
		{
			return false;
		}

		int requestPort = request.Host.Port ?? (request.IsHttps ? 443 : 80);
		return originUri.Scheme.Equals(request.Scheme, StringComparison.OrdinalIgnoreCase)
		       && originUri.Host.Equals(request.Host.Host, StringComparison.OrdinalIgnoreCase)
		       && originUri.Port == requestPort;
	}

	/// <summary>
	///     Why a request doesn't count as coming from this machine, or <c>null</c> when it does: a
	///     loopback caller, a loopback <c>Host</c>, and no sign of a proxy in between.
	/// </summary>
	/// <remarks>
	///     A loopback caller alone isn't enough. A page on attacker.example whose DNS name is rebound
	///     to 127.0.0.1 sends <c>Host</c> and <c>Origin</c> as attacker.example, which pass the origin
	///     check, and a forwarder on the box (nginx, ngrok, a dev tunnel) makes every caller loopback.
	/// </remarks>
	internal static string? LocalOnlyRefusal(HttpRequest request, IPAddress? remoteAddress)
	{
		if (!IsLocalCaller(remoteAddress))
		{
			return $"the caller {remoteAddress} is another machine";
		}

		if (!IsLoopbackHost(request.Host))
		{
			return $"the Host header \"{request.Host}\" is not localhost or a loopback address";
		}

		foreach (string header in _proxyHeaders)
		{
			if (request.Headers.ContainsKey(header))
			{
				return $"the request has a {header} header, so it came through a proxy";
			}
		}

		return null;
	}

	/// <summary>
	///     Whether the caller is on this machine. No address means a local transport, such as a Unix
	///     socket or an in-process server.
	/// </summary>
	internal static bool IsLocalCaller(IPAddress? address) =>
		address is null
		|| IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);

	/// <summary>
	///     Whether a <c>Host</c> header names this machine: <c>localhost</c>, a name under
	///     <c>.localhost</c>, or a loopback address, on any port.
	/// </summary>
	internal static bool IsLoopbackHost(HostString host)
	{
		if (!host.HasValue)
		{
			return false;
		}

		string name = host.Host.TrimEnd('.');
		if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)
		    || name.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return IPAddress.TryParse(name.Trim('[', ']'), out IPAddress? address) && IsLocalCaller(address);
	}

	private static void Reject(HttpContext context, ILogger logger)
	{
		logger.LogWarning("Rejected cross-origin POST to {Path} from {Origin}", context.Request.Path,
			context.Request.Headers.Origin.ToString() is { Length: > 0 } origin ? origin : "(no origin)");
		context.Response.StatusCode = StatusCodes.Status403Forbidden;
	}
}
