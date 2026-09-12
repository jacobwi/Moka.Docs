using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     A lightweight local static file server with WebSocket-based hot-reload
///     notifications. Uses <see cref="HttpListener" /> to avoid requiring the
///     ASP.NET Core shared framework.
/// </summary>
public sealed class DevServer : IDisposable
{
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

	private readonly BlazorPreviewService? _blazorPreviewService;
	private readonly ILogger<DevServer> _logger;
	private readonly int _port;
	private readonly ReplExecutionService? _replService;
	private readonly string _basePath;
	private readonly string _rootPath;
	private readonly ConcurrentBag<WebSocket> _webSockets = [];
	private CancellationTokenSource? _cts;
	private HttpListener? _listener;

	/// <summary>
	///     Creates a new dev server instance.
	/// </summary>
	/// <param name="logger">Logger instance.</param>
	/// <param name="rootPath">Directory containing the static site files.</param>
	/// <param name="port">Port to listen on.</param>
	/// <param name="replService">Optional REPL execution service.</param>
	/// <param name="blazorPreviewService">Optional Blazor preview renderer.</param>
	/// <param name="basePath">
	///     Base path the site was built for, e.g. <c>/Sub</c>. Pages built with a base path
	///     emit links like <c>/Sub/_theme/css/main.css</c>, so the server has to strip that
	///     prefix before looking the file up on disk. Defaults to <c>/</c> (site root).
	/// </param>
	public DevServer(ILogger<DevServer> logger, string rootPath, int port, ReplExecutionService? replService = null,
		BlazorPreviewService? blazorPreviewService = null, string basePath = "/")
	{
		_logger = logger;
		_rootPath = rootPath;
		_port = port;
		_replService = replService;
		_blazorPreviewService = blazorPreviewService;
		_basePath = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
			? ""
			: "/" + basePath.Trim('/');
	}

	/// <summary>
	///     The base path prefix this server strips from incoming requests, or an empty
	///     string when the site is served from the root.
	/// </summary>
	public string BasePath => _basePath;

	/// <inheritdoc />
	public void Dispose()
	{
		Stop();
		_cts?.Dispose();
		(_listener as IDisposable)?.Dispose();
	}

	/// <summary>
	///     Start listening for HTTP requests.
	/// </summary>
	public Task StartAsync(CancellationToken ct = default)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

		var listener = new HttpListener();
		listener.Prefixes.Add($"http://localhost:{_port}/");
		try
		{
			listener.Start();
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
				// Already closed by the failed Start; the port error below is what matters.
			}

			throw;
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
		var deadSockets = new List<WebSocket>();

		foreach (WebSocket ws in _webSockets)
		{
			try
			{
				if (ws.State == WebSocketState.Open)
				{
					await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
				}
				else
				{
					deadSockets.Add(ws);
				}
			}
			catch
			{
				deadSockets.Add(ws);
			}
		}

		// Clean up closed sockets (ConcurrentBag doesn't support removal,
		// but stale entries are harmless and few in a dev scenario)
		_logger.LogDebug("Sent reload to {Count} browser(s)", _webSockets.Count - deadSockets.Count);
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
					    request.ContentType, _port))
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
					case "/api/blazor/preview":
						await HandleBlazorPreviewAsync(request, response, ct);
						return;
					case "/api/feedback":
						await HandleFeedbackAsync(request, response, ct);
						return;
				}
			}

			// Resolve file path
			string? filePath = ResolveFilePath(path);

			if (filePath is null || !File.Exists(filePath))
			{
				// Try serving a 404 page
				string notFoundPath = Path.Combine(_rootPath, "404.html");
				if (File.Exists(notFoundPath))
				{
					response.StatusCode = 404;
					await ServeFileAsync(response, notFoundPath, true);
				}
				else
				{
					response.StatusCode = 404;
					response.ContentType = "text/plain";
					byte[] msg = Encoding.UTF8.GetBytes("404 Not Found");
					await response.OutputStream.WriteAsync(msg, ct);
				}
			}
			else
			{
				response.StatusCode = 200;
				bool isHtml = filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
				await ServeFileAsync(response, filePath, isHtml);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Error handling request: {Path}", request.Url?.AbsolutePath);
			try
			{
				response.StatusCode = 500;
				response.ContentType = "text/plain";
				byte[] msg = Encoding.UTF8.GetBytes("500 Internal Server Error");
				await response.OutputStream.WriteAsync(msg, ct);
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
		response.ContentType = "application/json; charset=utf-8";

		if (_replService is null)
		{
			response.StatusCode = 503;
			string errorJson = JsonSerializer.Serialize(new { error = "REPL service is not available." });
			byte[] errorBytes = Encoding.UTF8.GetBytes(errorJson);
			response.ContentLength64 = errorBytes.Length;
			await response.OutputStream.WriteAsync(errorBytes, ct);
			return;
		}

		try
		{
			using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
			string body = await reader.ReadToEndAsync(ct);
			ReplRequest? payload = JsonSerializer.Deserialize<ReplRequest>(body, _jsonOptions);
			string code = payload?.Code ?? "";

			ReplResult result = await _replService.ExecuteAsync(code, ct);

			response.StatusCode = 200;
			string json = JsonSerializer.Serialize(new { output = result.Output ?? "", error = result.Error ?? "" },
				_jsonOptions);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "REPL endpoint error");
			response.StatusCode = 500;
			string json = JsonSerializer.Serialize(new { error = $"Internal error: {ex.Message}" }, _jsonOptions);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes, ct);
		}
	}

	private async Task HandleBlazorPreviewAsync(HttpListenerRequest request, HttpListenerResponse response,
		CancellationToken ct)
	{
		response.ContentType = "application/json; charset=utf-8";

		if (_blazorPreviewService is null)
		{
			response.StatusCode = 503;
			string errorJson = JsonSerializer.Serialize(new { error = "Blazor preview service is not available." });
			byte[] errorBytes = Encoding.UTF8.GetBytes(errorJson);
			response.ContentLength64 = errorBytes.Length;
			await response.OutputStream.WriteAsync(errorBytes, ct);
			return;
		}

		try
		{
			using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
			string body = await reader.ReadToEndAsync(ct);
			BlazorPreviewRequest? payload = JsonSerializer.Deserialize<BlazorPreviewRequest>(body, _jsonOptions);
			string source = payload?.Source ?? "";

			BlazorPreviewResult result = await _blazorPreviewService.RenderAsync(source, ct);

			response.StatusCode = 200;
			string json = JsonSerializer.Serialize(new { html = result.Html ?? "", error = result.Error ?? "" },
				_jsonOptions);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Blazor preview endpoint error");
			response.StatusCode = 500;
			string json = JsonSerializer.Serialize(new { error = $"Internal error: {ex.Message}" }, _jsonOptions);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes, ct);
		}
	}

	private async Task HandleFeedbackAsync(HttpListenerRequest request, HttpListenerResponse response,
		CancellationToken ct)
	{
		response.ContentType = "application/json; charset=utf-8";

		try
		{
			using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
			string body = await reader.ReadToEndAsync(ct);
			FeedbackRequest? payload = JsonSerializer.Deserialize<FeedbackRequest>(body, _jsonOptions);

			string page = payload?.Page ?? "(unknown)";
			bool? helpful = payload?.Helpful;
			string emoji = helpful == true ? "👍" : "👎";
			_logger.LogInformation("Feedback {Emoji} for {Page}", emoji, page);

			response.StatusCode = 200;
			byte[] json = """{"ok":true}"""u8.ToArray();
			response.ContentLength64 = json.Length;
			await response.OutputStream.WriteAsync(json, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Feedback endpoint error");
			response.StatusCode = 500;
			byte[] json = """{"ok":false}"""u8.ToArray();
			response.ContentLength64 = json.Length;
			await response.OutputStream.WriteAsync(json, ct);
		}
	}

	/// <summary>
	///     Whether a POST to an <c>/api/</c> endpoint may run. The REPL and Blazor preview
	///     endpoints execute the code they receive, so any web page open in the developer's
	///     browser must not be able to reach them.
	/// </summary>
	/// <remarks>
	///     A cross-origin page can send a POST without a CORS preflight as long as the body is
	///     text/plain. Requiring a JSON content type forces a preflight, which this server never
	///     answers, and a browser request that does arrive must carry this server's own origin.
	///     Requests with no Origin header do not come from a cross-origin page (browsers always
	///     send one on POST), so tools like curl still work. The endpoints used to answer
	///     every origin with <c>Access-Control-Allow-Origin: *</c>.
	/// </remarks>
	/// <param name="origin">The request's Origin header.</param>
	/// <param name="secFetchSite">The request's Sec-Fetch-Site header.</param>
	/// <param name="contentType">The request's Content-Type header.</param>
	/// <param name="port">The port this server listens on.</param>
	internal static bool IsSameOriginApiRequest(string? origin, string? secFetchSite, string? contentType, int port)
	{
		if (contentType is null || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (secFetchSite is not null && secFetchSite is not ("same-origin" or "none"))
		{
			return false;
		}

		return origin is null || origin.Equals($"http://localhost:{port}", StringComparison.OrdinalIgnoreCase);
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

	private async Task ServeFileAsync(HttpListenerResponse response, string filePath, bool injectScript)
	{
		response.ContentType = GetContentType(filePath);

		// Disable caching for dev server
		response.Headers.Set("Cache-Control", "no-cache, no-store, must-revalidate");

		if (injectScript)
		{
			// Read, inject hot-reload script before </body>, then serve
			string html = await File.ReadAllTextAsync(filePath);

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

			byte[] bytes = Encoding.UTF8.GetBytes(html);
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes);
		}
		else
		{
			byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
			response.ContentLength64 = fileBytes.Length;
			await response.OutputStream.WriteAsync(fileBytes);
		}
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
		_webSockets.Add(ws);
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
	}

	private static string GetContentType(string filePath)
	{
		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		return ext switch
		{
			".html" or ".htm" => "text/html; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".js" => "application/javascript; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".xml" => "application/xml; charset=utf-8",
			".svg" => "image/svg+xml",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".webp" => "image/webp",
			".ico" => "image/x-icon",
			".woff" => "font/woff",
			".woff2" => "font/woff2",
			".ttf" => "font/ttf",
			".eot" => "application/vnd.ms-fontobject",
			".txt" => "text/plain; charset=utf-8",
			".map" => "application/json",
			_ => "application/octet-stream"
		};
	}

	/// <summary>JSON model for the REPL execute request body.</summary>
	private sealed class ReplRequest
	{
		public string Code { get; set; } = "";
	}

	/// <summary>JSON model for the Blazor preview request body.</summary>
	private sealed class BlazorPreviewRequest
	{
		public string Source { get; set; } = "";
	}

	/// <summary>JSON model for the feedback request body.</summary>
	private sealed class FeedbackRequest
	{
		public string Page { get; set; } = "";
		public bool? Helpful { get; set; }
	}
}
