using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     A lightweight local static file server with WebSocket-based hot-reload
///     notifications, built on <see cref="HttpListener" />.
/// </summary>
public sealed class DevServer : IDisposable
{
	/// <summary>Largest feedback body read, as in the ASP.NET Core host. A vote is about a hundred bytes.</summary>
	internal const int MaxFeedbackBodyBytes = 4 * 1024;

	/// <summary>Longest page path accepted in a vote.</summary>
	internal const int MaxFeedbackPageLength = 1024;

	/// <summary>
	///     Largest REPL body read. Snippets are capped at 10,000 characters, and JSON can escape a
	///     character as six bytes.
	/// </summary>
	internal const int MaxReplBodyBytes = 64 * 1024;

	/// <summary>The JavaScript snippet injected into HTML pages for hot-reload.</summary>
	private const string _hotReloadScript = """
	                                        <script>
	                                        (function() {
	                                            let ws;
	                                            function connect() {
	                                                ws = new WebSocket('ws://' + location.host + '/__mokadocs-ws');
	                                                ws.onmessage = function(e) {
	                                                    if (e.data === 'reload') location.reload();
	                                                };
	                                                ws.onclose = function() {
	                                                    setTimeout(connect, 1000);
	                                                };
	                                            }
	                                            connect();
	                                        })();
	                                        </script>
	                                        """;

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	private static readonly FileExtensionContentTypeProvider _staticFileTypes = new();

	private readonly ILogger<DevServer> _logger;
	private readonly int _port;
	private readonly ReplExecutionService? _replService;
	private readonly string _basePath;
	private readonly string _rootPath;
	private readonly ConcurrentDictionary<WebSocket, byte> _webSockets = new();
	private CancellationTokenSource? _cts;
	private HttpListener? _listener;

	/// <summary>
	///     Creates a new dev server instance.
	/// </summary>
	/// <param name="logger">Logger instance.</param>
	/// <param name="rootPath">Directory containing the static site files.</param>
	/// <param name="port">Port to listen on.</param>
	/// <param name="replService">Optional REPL execution service.</param>
	/// <param name="basePath">
	///     Base path the site was built for, e.g. <c>/Sub</c>. Pages built with a base path
	///     emit links like <c>/Sub/_theme/css/main.css</c>, so the server has to strip that
	///     prefix before looking the file up on disk. Defaults to <c>/</c> (site root).
	/// </param>
	public DevServer(ILogger<DevServer> logger, string rootPath, int port, ReplExecutionService? replService = null,
		string basePath = "/")
	{
		_logger = logger;
		_rootPath = rootPath;
		_port = port;
		_replService = replService;
		_basePath = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
			? ""
			: "/" + basePath.Trim('/');
	}

	/// <summary>
	///     The base path prefix this server strips from incoming requests, or an empty
	///     string when the site is served from the root.
	/// </summary>
	public string BasePath => _basePath;

	internal int ConnectedBrowserCount => _webSockets.Count;

	/// <inheritdoc />
	public void Dispose()
	{
		_cts?.Cancel();
		try
		{
			// Close alone, not Stop and then Close: the managed listener on Linux and macOS releases
			// its endpoints on both, and the second release binds the port again, which throws
			// "Address already in use" once something else holds it. Stop may have run already.
			_listener?.Close();
		}
		catch (HttpListenerException ex)
		{
			_logger.LogDebug("Closing the listener after it stopped: {Message}", ex.Message);
		}

		_cts?.Dispose();
		_logger.LogInformation("Dev server stopped");
	}

	/// <summary>
	///     Start listening for HTTP requests on <c>localhost</c> and <c>127.0.0.1</c>, and on
	///     <c>[::1]</c> on Windows.
	/// </summary>
	public Task StartAsync(CancellationToken ct = default)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

		// Windows can reserve address prefixes for administrators, and a machine without IPv6 has
		// no [::1], so smaller sets are tried in turn. localhost alone needs neither; when the port
		// is taken, that last attempt throws the port error.
		string[] hosts = ListenerHosts(_port);
		HttpListener? listener = null;
		HttpListenerException? refused = null;
		for (int count = hosts.Length; listener is null; count--)
		{
			try
			{
				listener = StartListener(hosts[..count]);
			}
			catch (HttpListenerException ex) when (count > 1)
			{
				refused ??= ex;
			}
		}

		if (refused is not null)
		{
			_logger.LogInformation("Serving on {Hosts} only; the others were refused: {Message}",
				string.Join(", ", listener.Prefixes), refused.Message);
		}

		_listener = listener;

		_logger.LogInformation("Dev server listening on http://localhost:{Port}/", _port);

		// Start the request loop on a background thread
		_ = Task.Run(() => RequestLoopAsync(_cts.Token), _cts.Token);

		return Task.CompletedTask;
	}

	/// <summary>
	///     Notify all connected browsers to reload.
	/// </summary>
	public async Task NotifyReloadAsync()
	{
		byte[] message = Encoding.UTF8.GetBytes("reload");
		var segment = new ArraySegment<byte>(message);
		int sent = 0;

		foreach (WebSocket ws in _webSockets.Keys)
		{
			try
			{
				if (ws.State == WebSocketState.Open)
				{
					await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
					sent++;
					continue;
				}
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Dropping a browser connection that failed to take the reload: {Message}", ex.Message);
			}

			// Its handler disposes it when its receive loop ends.
			_webSockets.TryRemove(ws, out _);
		}

		_logger.LogDebug("Sent reload to {Count} browser(s)", sent);
	}

	/// <summary>
	///     Stop the server.
	/// </summary>
	public void Stop()
	{
		_cts?.Cancel();
		if (_listener is { IsListening: true })
		{
			_listener.Stop();
		}

		_logger.LogInformation("Dev server stopped");
	}

	/// <summary>
	///     Whether a POST to an <c>/api/</c> endpoint may run. The REPL endpoint executes the code it
	///     receives, so any web page open in the developer's browser must not be able to reach it.
	/// </summary>
	/// <remarks>
	///     A cross-origin page can send a POST without a CORS preflight as long as the body is
	///     text/plain. Requiring a JSON content type forces a preflight, which this server never
	///     answers, and a browser request that does arrive must carry the origin of the address it
	///     was sent to: <c>http://localhost:{port}</c>, <c>http://127.0.0.1:{port}</c> or
	///     <c>http://[::1]:{port}</c>, matching the <c>Host</c> header. A page on one of those
	///     can't post to another. Requests with no Origin header do not come from a cross-origin
	///     page (browsers always send one on POST), so tools like curl still work. The endpoints
	///     used to answer every origin with <c>Access-Control-Allow-Origin: *</c>.
	/// </remarks>
	/// <param name="origin">The request's Origin header.</param>
	/// <param name="secFetchSite">The request's Sec-Fetch-Site header.</param>
	/// <param name="contentType">The request's Content-Type header.</param>
	/// <param name="port">The port this server listens on.</param>
	/// <param name="host">The request's Host header. <c>null</c> stands for <c>localhost:{port}</c>.</param>
	internal static bool IsSameOriginApiRequest(string? origin, string? secFetchSite, string? contentType, int port,
		string? host = null)
	{
		if (contentType is null || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (secFetchSite is not null && secFetchSite is not ("same-origin" or "none"))
		{
			return false;
		}

		if (origin is null)
		{
			return true;
		}

		string requestHost = host ?? $"localhost:{port}";
		return LoopbackHosts(port).Contains(requestHost, StringComparer.OrdinalIgnoreCase)
		       && origin.Equals("http://" + requestHost, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	///     The content type for a file. Text types carry a UTF-8 charset; everything else comes from
	///     ASP.NET Core's static file map. The old list had no <c>.wasm</c>, which browsers need as
	///     <c>application/wasm</c> to compile while it downloads, nor <c>.pdf</c> or video.
	/// </summary>
	internal static string GetContentType(string filePath)
	{
		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		return ext switch
		{
			".html" or ".htm" => "text/html; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".js" or ".mjs" => "application/javascript; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".xml" => "application/xml; charset=utf-8",
			".svg" => "image/svg+xml",
			".txt" => "text/plain; charset=utf-8",
			".yaml" or ".yml" => "application/yaml; charset=utf-8",
			".map" => "application/json",
			".wasm" => "application/wasm",
			".zip" => "application/zip",
			// ASP.NET Core's map has it from .NET 10 on.
			".avif" => "image/avif",
			_ => _staticFileTypes.TryGetContentType(filePath, out string? contentType)
				? contentType
				: "application/octet-stream"
		};
	}

	private static string[] LoopbackHosts(int port) => [$"localhost:{port}", $"127.0.0.1:{port}", $"[::1]:{port}"];

	/// <summary>
	///     The hosts the server listens on, in the order they are given up when the system refuses
	///     them. The managed listener on Linux and macOS can't parse an IPv6 prefix.
	/// </summary>
	private static string[] ListenerHosts(int port) =>
		OperatingSystem.IsWindows() ? LoopbackHosts(port) : LoopbackHosts(port)[..2];

	private static HttpListener StartListener(IEnumerable<string> hosts)
	{
		var listener = new HttpListener();
		try
		{
			foreach (string host in hosts)
			{
				listener.Prefixes.Add($"http://{host}/");
			}

			listener.Start();
			return listener;
		}
		catch
		{
			// On Windows a failed Start leaves the listener disposed. It used to stay in _listener,
			// so Dispose called Stop on it and an ObjectDisposedException replaced the port error.
			try
			{
				listener.Close();
			}
			catch (ObjectDisposedException)
			{
				// Already closed by the failed Start; the port error is what matters.
			}

			throw;
		}
	}

	private async Task RequestLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested && _listener is { IsListening: true })
		{
			try
			{
				HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(ct);
				_ = Task.Run(() => HandleRequestAsync(context, ct), ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (HttpListenerException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error accepting request");
			}
		}
	}

	private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
	{
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;

		try
		{
			string path = request.Url?.AbsolutePath ?? "/";

			// WebSocket upgrade for hot-reload
			if (path == "/__mokadocs-ws" && request.IsWebSocketRequest)
			{
				await HandleWebSocketAsync(context, ct);
				return;
			}

			// Pages built with a base path post to /Sub/api/..., so match the API routes
			// with the prefix removed too. The feedback widget did that and always got a 404.
			string apiPath = StripBasePath(path);
			if (request.HttpMethod == "POST" && apiPath.StartsWith("/api/", StringComparison.Ordinal))
			{
				if (!IsSameOriginApiRequest(request.Headers["Origin"], request.Headers["Sec-Fetch-Site"],
					    request.ContentType, _port, request.UserHostName))
				{
					_logger.LogWarning("Rejected cross-origin POST to {Path} from {Origin}", apiPath,
						request.Headers["Origin"] ?? "(no origin)");
					response.StatusCode = 403;
					return;
				}

				switch (apiPath)
				{
					case "/api/repl/execute":
						await HandleReplExecuteAsync(request, response, ct);
						return;
					case "/api/feedback":
						await HandleFeedbackAsync(request, response, ct);
						return;
				}
			}

			bool head = request.HttpMethod == "HEAD";
			if (!head && request.HttpMethod != "GET")
			{
				response.StatusCode = 405;
				response.Headers.Set("Allow", "GET, HEAD");
				return;
			}

			// Resolve file path
			string? filePath = ResolveFilePath(path);

			if (filePath is null || !File.Exists(filePath))
			{
				response.StatusCode = 404;

				// Try serving a 404 page
				string notFoundPath = Path.Combine(_rootPath, "404.html");
				if (File.Exists(notFoundPath))
				{
					await ServeFileAsync(response, notFoundPath, true, head, ct);
				}
				else
				{
					response.ContentType = "text/plain";
					await WriteBodyAsync(response, "404 Not Found"u8.ToArray(), head, ct);
				}
			}
			else
			{
				response.StatusCode = 200;
				bool isHtml = filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
				await ServeFileAsync(response, filePath, isHtml, head, ct);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Error handling request: {Path}", request.Url?.AbsolutePath);
			try
			{
				response.StatusCode = 500;
				response.ContentType = "text/plain";
				await WriteBodyAsync(response, "500 Internal Server Error"u8.ToArray(),
					request.HttpMethod == "HEAD", ct);
			}
			catch
			{
				// Response may already be sent
			}
		}
		finally
		{
			try
			{
				response.Close();
			}
			catch
			{
				/* ignore */
			}
		}
	}

	private async Task HandleReplExecuteAsync(HttpListenerRequest request, HttpListenerResponse response,
		CancellationToken ct)
	{
		if (_replService is null)
		{
			await WriteJsonAsync(response, 503, new { error = "REPL service is not available." }, ct);
			return;
		}

		byte[]? body = await ReadBodyAsync(request, MaxReplBodyBytes, ct);
		if (body is null)
		{
			await WriteJsonAsync(response, 413, new { error = "The code is too long to run." }, ct);
			return;
		}

		if (Deserialize<ReplRequest>(body) is not { } payload)
		{
			await WriteJsonAsync(response, 400, new { error = "The request body is not valid JSON." }, ct);
			return;
		}

		try
		{
			ReplResult result = await _replService.ExecuteAsync(payload.Code ?? "", ct);
			await WriteJsonAsync(response, 200, new { output = result.Output ?? "", error = result.Error ?? "" }, ct);
		}
		catch (ReplQueueFullException ex)
		{
			await WriteJsonAsync(response, 429, new { error = ex.Message }, ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "REPL endpoint error");
			await WriteJsonAsync(response, 500, new { error = $"Internal error: {ex.Message}" }, ct);
		}
	}

	private async Task HandleFeedbackAsync(HttpListenerRequest request, HttpListenerResponse response,
		CancellationToken ct)
	{
		byte[]? body = await ReadBodyAsync(request, MaxFeedbackBodyBytes, ct);
		if (body is null)
		{
			await WriteJsonAsync(response, 413, new { ok = false }, ct);
			return;
		}

		// The page ends up in the log, so it is checked like the host checks it.
		if (Deserialize<FeedbackRequest>(body) is not { Page: { Length: > 0 and <= MaxFeedbackPageLength } page, Helpful: { } helpful }
		    || page.Any(char.IsControl))
		{
			await WriteJsonAsync(response, 400, new { ok = false }, ct);
			return;
		}

		_logger.LogInformation("Feedback {Emoji} for {Page}", helpful ? "👍" : "👎", page);
		await WriteJsonAsync(response, 200, new { ok = true }, ct);
	}

	/// <summary>
	///     Reads the body, or returns <c>null</c> when it is longer than <paramref name="maxBytes" />.
	///     Bodies used to be read whole: 400 MB posted to /api/feedback took serve to 3.2 GB.
	/// </summary>
	private static async Task<byte[]?> ReadBodyAsync(HttpListenerRequest request, int maxBytes, CancellationToken ct)
	{
		if (request.ContentLength64 > maxBytes)
		{
			return null;
		}

		using var buffer = new MemoryStream();
		byte[] chunk = new byte[4096];
		int read;
		while ((read = await request.InputStream.ReadAsync(chunk, ct)) > 0)
		{
			if (buffer.Length + read > maxBytes)
			{
				return null;
			}

			buffer.Write(chunk, 0, read);
		}

		return buffer.ToArray();
	}

	private static T? Deserialize<T>(byte[] body) where T : class
	{
		try
		{
			return JsonSerializer.Deserialize<T>(body, _jsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static Task WriteJsonAsync<T>(HttpListenerResponse response, int statusCode, T payload,
		CancellationToken ct)
	{
		response.StatusCode = statusCode;
		response.ContentType = "application/json; charset=utf-8";
		return WriteBodyAsync(response, JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOptions), false, ct);
	}

	/// <summary>
	///     Sets the length and writes the body, except for HEAD. On Windows, http.sys refuses a body
	///     on a HEAD response, so every HEAD request used to end in a 500.
	/// </summary>
	private static async Task WriteBodyAsync(HttpListenerResponse response, byte[] body, bool head,
		CancellationToken ct)
	{
		response.ContentLength64 = body.Length;
		if (!head)
		{
			await response.OutputStream.WriteAsync(body, ct);
		}
	}

	private string? ResolveFilePath(string urlPath)
	{
		urlPath = StripBasePath(urlPath);

		// Decode and sanitize path
		string decoded = Uri.UnescapeDataString(urlPath).TrimStart('/');
		if (decoded.Contains(".."))
		{
			return null;
		}

		string filePath = Path.GetFullPath(Path.Combine(_rootPath, decoded.Replace('/', Path.DirectorySeparatorChar)));

		// Path.Combine returns its second argument when that is rooted, so a request for
		// /C:/Windows/win.ini resolved to the real file outside the site.
		string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_rootPath)) + Path.DirectorySeparatorChar;
		if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
		    && !filePath.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		// If path points to a directory, look for index.html
		if (Directory.Exists(filePath))
		{
			filePath = Path.Combine(filePath, "index.html");
		}

		// If file doesn't exist, try adding .html
		if (!File.Exists(filePath) && !Path.HasExtension(filePath))
		{
			filePath += ".html";
		}

		return filePath;
	}

	/// <summary>
	///     Removes the configured base path from a request path so it can be resolved
	///     against the output directory, which has no such prefix on disk.
	/// </summary>
	private string StripBasePath(string urlPath)
	{
		if (_basePath.Length == 0)
		{
			return urlPath;
		}

		if (urlPath.Equals(_basePath, StringComparison.OrdinalIgnoreCase))
		{
			return "/";
		}

		if (urlPath.StartsWith(_basePath + "/", StringComparison.OrdinalIgnoreCase))
		{
			return urlPath[_basePath.Length..];
		}

		return urlPath;
	}

	private static async Task ServeFileAsync(HttpListenerResponse response, string filePath, bool injectScript,
		bool head, CancellationToken ct)
	{
		response.ContentType = GetContentType(filePath);

		// Disable caching for dev server
		response.Headers.Set("Cache-Control", "no-cache, no-store, must-revalidate");

		byte[] bytes;
		if (injectScript)
		{
			// Read, inject hot-reload script before </body>, then serve
			string html = await File.ReadAllTextAsync(filePath, ct);

			int bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
			if (bodyCloseIndex >= 0)
			{
				html = string.Concat(html.AsSpan(0, bodyCloseIndex), _hotReloadScript, html.AsSpan(bodyCloseIndex));
			}
			else
				// No </body> tag - append script at the end
			{
				html += _hotReloadScript;
			}

			bytes = Encoding.UTF8.GetBytes(html);
		}
		else
		{
			bytes = await File.ReadAllBytesAsync(filePath, ct);
		}

		await WriteBodyAsync(response, bytes, head, ct);
	}

	private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken ct)
	{
		WebSocketContext wsContext;
		try
		{
			wsContext = await context.AcceptWebSocketAsync(null);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "WebSocket upgrade failed");
			context.Response.StatusCode = 500;
			context.Response.Close();
			return;
		}

		WebSocket ws = wsContext.WebSocket;
		_webSockets.TryAdd(ws, 0);
		_logger.LogDebug("WebSocket client connected");

		// Keep the socket alive until it closes
		byte[] buffer = new byte[256];
		try
		{
			while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
			{
				WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (WebSocketException)
		{
		}
		finally
		{
			_webSockets.TryRemove(ws, out _);
			ws.Dispose();
		}
	}

	/// <summary>JSON model for the REPL execute request body.</summary>
	private sealed class ReplRequest
	{
		public string? Code { get; set; }
	}

	/// <summary>JSON model for the feedback request body.</summary>
	private sealed class FeedbackRequest
	{
		public string? Page { get; set; }
		public bool? Helpful { get; set; }
	}
}
