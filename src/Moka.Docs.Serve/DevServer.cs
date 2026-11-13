using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     A local static file server with WebSocket-based hot-reload notifications. It runs Kestrel
///     on the loopback addresses only.
/// </summary>
public sealed class DevServer : IDisposable, IAsyncDisposable
{
	/// <summary>The path browsers open the reload WebSocket on, at the root even under a base path.</summary>
	internal const string ReloadSocketPath = "/__mokadocs-ws";

	private const string _noStore = "no-cache, no-store, must-revalidate";

	private const string _htmlContentType = "text/html; charset=utf-8";

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

	private static readonly FileExtensionContentTypeProvider _contentTypes = CreateContentTypes();

	private readonly ILogger<DevServer> _logger;
	private readonly int _port;
	private readonly ReplExecutionService? _replService;
	private readonly string _basePath;
	private readonly string _rootPath;
	private readonly ConcurrentDictionary<WebSocket, byte> _webSockets = new();
	private readonly CancellationTokenSource _stopping = new();
	private WebApplication? _app;
	private PhysicalFileProvider? _files;
	private string _pageHtml = _hotReloadScript;
	private int _stopped;
	private int _disposed;

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

	/// <summary>
	///     Dev-only endpoints and page HTML to add. Changes after <see cref="StartAsync" /> have no effect.
	/// </summary>
	public IList<IDevServerExtension> Extensions { get; } = [];

	internal int ConnectedBrowserCount => _webSockets.Count;

	/// <inheritdoc />
	public void Dispose() => Task.Run(() => DisposeAsync().AsTask()).GetAwaiter().GetResult();

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 1)
		{
			return;
		}

		await StopCoreAsync();
		if (_app is not null)
		{
			await _app.DisposeAsync();
		}

		_files?.Dispose();
		_stopping.Dispose();
	}

	/// <summary>
	///     Start listening for HTTP requests on <c>localhost</c>: <c>127.0.0.1</c>, and <c>[::1]</c>
	///     where the system has IPv6.
	/// </summary>
	/// <param name="ct">Cancels starting. Stop the running server with <see cref="Stop" /> or by disposing it.</param>
	/// <exception cref="IOException">The port is in use, or neither loopback address can be bound.</exception>
	public async Task StartAsync(CancellationToken ct = default)
	{
		// The provider needs its root to exist. A rebuild may delete and recreate the folder
		// later, which it copes with: it holds the path, not a handle.
		Directory.CreateDirectory(_rootPath);
		_files = new PhysicalFileProvider(Path.GetFullPath(_rootPath), ExclusionFilters.None);
		_pageHtml = _hotReloadScript + string.Concat(Extensions.Select(e => e.PageHtml));

		WebApplication app = CreateApp(_files);
		try
		{
			await app.StartAsync(ct);
		}
		catch
		{
			await app.DisposeAsync();
			throw;
		}

		_app = app;
		_logger.LogInformation("Dev server listening on http://localhost:{Port}/", _port);
	}

	/// <summary>
	///     Notify all connected browsers to reload.
	/// </summary>
	public async Task NotifyReloadAsync()
	{
		byte[] message = Encoding.UTF8.GetBytes("reload");
		int sent = 0;

		foreach (WebSocket ws in _webSockets.Keys)
		{
			try
			{
				if (ws.State == WebSocketState.Open)
				{
					await ws.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true,
						CancellationToken.None);
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
	public void Stop() => Task.Run(StopCoreAsync).GetAwaiter().GetResult();

	/// <summary>
	///     Whether a request to an <c>/api/</c> endpoint, other than <c>GET</c> or <c>HEAD</c>, may
	///     run. The REPL endpoint executes the code it receives, so any web page open in the
	///     developer's browser must not be able to reach it.
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
	///     The content type a file is served with, or <c>null</c> for an extension the server doesn't
	///     serve. Text types carry a UTF-8 charset; everything else comes from ASP.NET Core's static
	///     file map, plus <c>.wasm</c> (browsers need <c>application/wasm</c> to compile while it
	///     downloads) and the Blazor preview runtime's own files.
	/// </summary>
	internal static string? GetContentType(string filePath) =>
		_contentTypes.TryGetContentType(filePath, out string? contentType) ? contentType : null;

	private static FileExtensionContentTypeProvider CreateContentTypes()
	{
		var provider = new FileExtensionContentTypeProvider();
		IDictionary<string, string> map = provider.Mappings;
		map[".html"] = _htmlContentType;
		map[".htm"] = _htmlContentType;
		map[".css"] = "text/css; charset=utf-8";
		map[".js"] = "application/javascript; charset=utf-8";
		map[".mjs"] = "application/javascript; charset=utf-8";
		map[".json"] = "application/json; charset=utf-8";
		map[".xml"] = "application/xml; charset=utf-8";
		map[".txt"] = "text/plain; charset=utf-8";
		// The pages' Markdown copies (build.llms). Without a charset, browsers read them as Latin-1.
		map[".md"] = "text/markdown; charset=utf-8";
		map[".yaml"] = "application/yaml; charset=utf-8";
		map[".yml"] = "application/yaml; charset=utf-8";
		map[".svg"] = "image/svg+xml";
		map[".map"] = "application/json";
		map[".wasm"] = "application/wasm";
		map[".zip"] = "application/zip";
		// ASP.NET Core's map has it from .NET 10 on.
		map[".avif"] = "image/avif";

		// The preview runtime under _preview-wasm and the compiled previews under
		// _preview-assemblies, mapped the way Blazor's own dev server maps them.
		foreach (string extension in (string[])[".dll", ".pdb", ".dat", ".blat", ".webcil", ".br"])
		{
			map[extension] = "application/octet-stream";
		}

		return provider;
	}

	private static string[] LoopbackHosts(int port) => [$"localhost:{port}", $"127.0.0.1:{port}", $"[::1]:{port}"];

	/// <summary>
	///     Preview frames under <c>/_preview-wasm/</c> are pages too, but each would open its own
	///     reload socket next to the page that holds it.
	/// </summary>
	private static bool AddsPageHtml(PathString path) =>
		!path.StartsWithSegments("/_preview-wasm", StringComparison.OrdinalIgnoreCase);

	private static bool IsGetOrHead(HttpRequest request) =>
		HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

	private WebApplication CreateApp(IFileProvider files)
	{
		// The empty builder reads no appsettings.json or ASPNETCORE_ variables from the folder or
		// shell serve runs in, which belong to the user's project, and starts faster.
		WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
		{
			ContentRootPath = AppContext.BaseDirectory
		});
		builder.WebHost.UseKestrelCore().ConfigureKestrel(options =>
		{
			options.AddServerHeader = false;
			options.ListenLocalhost(_port, listen => listen.Protocols = HttpProtocols.Http1);
		});
		builder.Services.AddRoutingCore();
		builder.Services.AddHostFiltering(options => options.AllowedHosts = ["localhost", "127.0.0.1", "[::1]"]);
		// The console lifetime would stop the server on Ctrl+C by itself; serve stops it when it's done.
		builder.Services.AddSingleton<IHostLifetime, OwnerControlledLifetime>();
		builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(5));

		WebApplication app = builder.Build();
		app.MapGet(ReloadSocketPath, AcceptBrowserAsync);
		app.MapPost("/api/feedback", HandleFeedbackAsync);
		app.MapPost("/api/repl/execute", HandleReplExecuteAsync);
		foreach (IDevServerExtension extension in Extensions)
		{
			extension.MapEndpoints(app);
		}

		var webSockets = new WebSocketOptions();
		foreach (string host in LoopbackHosts(_port))
		{
			webSockets.AllowedOrigins.Add("http://" + host);
		}

		app.Use(LogErrorsAsync);
		// Kestrel answers any Host, so a page whose DNS name is rebound to 127.0.0.1 could read the
		// site. HttpListener only took these three.
		app.UseHostFiltering();
		app.UseWebSockets(webSockets);
		if (_basePath.Length > 0)
		{
			// Pages built with a base path link and post to /Sub/..., and paths without the
			// prefix work too, as they did before.
			app.UsePathBase(_basePath);
		}

		app.Use(RejectCrossOriginApiRequestsAsync);
		app.Use(ServePageAsync);
		app.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = files,
			ContentTypeProvider = _contentTypes,
			ServeUnknownFileTypes = false,
			OnPrepareResponse = file => file.Context.Response.Headers.CacheControl = _noStore
		});
		app.UseRouting();
		app.UseEndpoints(_ => { });
		app.Run(AnswerUnmatchedAsync);
		return app;
	}

	private async Task StopCoreAsync()
	{
		if (Interlocked.Exchange(ref _stopped, 1) == 1)
		{
			return;
		}

		// Ends the reload sockets and REPL runs first; Kestrel waits for open requests to finish.
		await _stopping.CancelAsync();
		if (_app is null)
		{
			return;
		}

		try
		{
			await _app.StopAsync();
		}
		catch (OperationCanceledException)
		{
			_logger.LogDebug("Requests still open after the shutdown timeout were aborted");
		}

		_logger.LogInformation("Dev server stopped");
	}

	private async Task LogErrorsAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Error handling request: {Path}", context.Request.Path);
			if (context.Response.HasStarted)
			{
				return;
			}

			context.Response.Clear();
			await WriteTextAsync(context, StatusCodes.Status500InternalServerError, "500 Internal Server Error");
		}
	}

	private async Task RejectCrossOriginApiRequestsAsync(HttpContext context, RequestDelegate next)
	{
		HttpRequest request = context.Request;
		if (!IsGetOrHead(request)
		    && request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
		    && !IsSameOriginApiRequest(request.Headers.Origin, request.Headers["Sec-Fetch-Site"], request.ContentType,
			    _port, request.Host.Value))
		{
			_logger.LogWarning("Rejected cross-origin {Method} to {Path} from {Origin}", request.Method, request.Path,
				request.Headers.Origin.ToString() is { Length: > 0 } origin ? origin : "(no origin)");
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			return;
		}

		await next(context);
	}

	/// <summary>
	///     Serves pages, which get the reload script, so the static file middleware only sees other
	///     files. A folder is served from its <c>index.html</c> without a redirect, and a path
	///     without an extension that matches nothing is tried with <c>.html</c>.
	/// </summary>
	private async Task ServePageAsync(HttpContext context, RequestDelegate next)
	{
		if (IsGetOrHead(context.Request) && FindPage(context.Request.Path) is { } page)
		{
			await WritePageAsync(context, page, StatusCodes.Status200OK);
			return;
		}

		await next(context);
	}

	private IFileInfo? FindPage(PathString path)
	{
		string subpath = path.Value ?? "";
		IFileInfo file = _files!.GetFileInfo(subpath);
		if (!file.Exists)
		{
			if (_files.GetDirectoryContents(subpath).Exists)
			{
				file = _files.GetFileInfo(subpath.TrimEnd('/') + "/index.html");
			}
			else if (!Path.HasExtension(subpath))
			{
				file = _files.GetFileInfo(subpath + ".html");
			}
		}

		return file.Exists && file.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? file : null;
	}

	private async Task WritePageAsync(HttpContext context, IFileInfo page, int statusCode)
	{
		byte[] body = await ReadPageAsync(page, AddsPageHtml(context.Request.Path), context.RequestAborted);
		HttpResponse response = context.Response;
		response.StatusCode = statusCode;
		response.ContentType = _htmlContentType;
		response.Headers.CacheControl = _noStore;
		response.ContentLength = body.Length;
		if (!HttpMethods.IsHead(context.Request.Method))
		{
			await response.Body.WriteAsync(body, context.RequestAborted);
		}
	}

	private async Task<byte[]> ReadPageAsync(IFileInfo page, bool addPageHtml, CancellationToken ct)
	{
		await using Stream stream = page.CreateReadStream();
		if (!addPageHtml)
		{
			using var copy = new MemoryStream();
			await stream.CopyToAsync(copy, ct);
			return copy.ToArray();
		}

		using var reader = new StreamReader(stream, Encoding.UTF8);
		string html = await reader.ReadToEndAsync(ct);
		int bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
		html = bodyClose >= 0
			? string.Concat(html.AsSpan(0, bodyClose), _pageHtml, html.AsSpan(bodyClose))
			: html + _pageHtml;
		return Encoding.UTF8.GetBytes(html);
	}

	private async Task AnswerUnmatchedAsync(HttpContext context)
	{
		if (!IsGetOrHead(context.Request))
		{
			context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
			context.Response.Headers.Allow = "GET, HEAD";
			return;
		}

		IFileInfo notFoundPage = _files!.GetFileInfo("404.html");
		if (notFoundPage.Exists)
		{
			await WritePageAsync(context, notFoundPage, StatusCodes.Status404NotFound);
			return;
		}

		await WriteTextAsync(context, StatusCodes.Status404NotFound, "404 Not Found");
	}

	private static async Task WriteTextAsync(HttpContext context, int statusCode, string text)
	{
		byte[] body = Encoding.UTF8.GetBytes(text);
		HttpResponse response = context.Response;
		response.StatusCode = statusCode;
		response.ContentType = "text/plain";
		response.ContentLength = body.Length;
		if (!HttpMethods.IsHead(context.Request.Method))
		{
			await response.Body.WriteAsync(body, context.RequestAborted);
		}
	}

	/// <summary>
	///     Holds a browser's reload socket open until it closes or the server stops. The WebSocket
	///     middleware has already refused a handshake from another origin with 403: browsers send
	///     no CORS preflight for WebSockets, so any page could otherwise connect.
	/// </summary>
	private async Task AcceptBrowserAsync(HttpContext context)
	{
		if (!context.WebSockets.IsWebSocketRequest)
		{
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
			return;
		}

		using WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
		using var closed = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, _stopping.Token);
		_webSockets.TryAdd(ws, 0);
		_logger.LogDebug("WebSocket client connected");

		byte[] buffer = new byte[256];
		try
		{
			while (ws.State == WebSocketState.Open)
			{
				WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), closed.Token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
				}
			}
		}
		catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
		{
		}
		finally
		{
			_webSockets.TryRemove(ws, out _);
		}
	}

	private async Task HandleReplExecuteAsync(HttpContext context)
	{
		if (_replService is null)
		{
			await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status503ServiceUnavailable,
				new { error = "REPL service is not available." });
			return;
		}

		if (await DocsApiRequests.ReadReplCodeAsync(context) is not { } code)
		{
			return;
		}

		// A reader who leaves mid-run cancels the run, and the service replaces the worker, so the
		// snippet stops instead of running on until the timeout.
		using var run = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, _stopping.Token);
		try
		{
			await DocsApiRequests.RunReplAsync(context, _replService, code, run.Token);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "REPL endpoint error");
			await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status500InternalServerError,
				new { error = $"Internal error: {ex.Message}" });
		}
	}

	private async Task HandleFeedbackAsync(HttpContext context)
	{
		if (await DocsApiRequests.ReadFeedbackAsync(context) is not { } vote)
		{
			return;
		}

		_logger.LogInformation("Feedback {Emoji} for {Page}", vote.Helpful ? "👍" : "👎", vote.Page);
		await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status200OK, new { ok = true });
	}

	/// <summary>Leaves stopping to the server's owner instead of the console's Ctrl+C and SIGTERM.</summary>
	private sealed class OwnerControlledLifetime : IHostLifetime
	{
		public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
