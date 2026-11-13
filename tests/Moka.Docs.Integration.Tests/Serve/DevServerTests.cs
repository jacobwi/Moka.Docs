using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     Runs a real dev server. The REPL endpoint executes the code posted to it, and used to
///     accept it from any web page open in the same browser.
/// </summary>
public sealed class DevServerTests : IAsyncLifetime
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-devserver-" + Guid.NewGuid().ToString("N"));
	private int _port = FreePort();
	private readonly HttpClient _http = new();
	private DevServer? _server;

	private string Origin => $"http://localhost:{_port}";

	public ValueTask InitializeAsync()
	{
		Directory.CreateDirectory(Path.Combine(_root, "site"));
		File.WriteAllText(Path.Combine(_root, "site", "index.html"), "<html><body>home</body></html>");
		File.WriteAllText(Path.Combine(_root, "secret.txt"), "outside the site");
		return ValueTask.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		_server?.Dispose();
		_http.Dispose();
		await Task.Yield();
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private static int FreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private async Task StartAsync(string basePath = "/", ReplExecutionService? repl = null,
		IDevServerExtension? extension = null)
	{
		// FreePort releases the port before the server binds it, and a client socket from a test
		// running in parallel can take it in between ("Address already in use" on Linux CI).
		for (int attempt = 1; ; attempt++)
		{
			_server = new DevServer(NullLogger<DevServer>.Instance, Path.Combine(_root, "site"), _port, repl,
				basePath);
			if (extension is not null)
			{
				_server.Extensions.Add(extension);
			}

			try
			{
				await _server.StartAsync(TestContext.Current.CancellationToken);
				return;
			}
			catch (IOException) when (attempt < 5)
			{
				_server.Dispose();
				_port = FreePort();
			}
		}
	}

	private Task<HttpResponseMessage> PostAsync(string path, string contentType, string? origin,
		string body = """{"code":"1 + 1","page":"/","helpful":true}""", string host = "localhost")
	{
		var request = new HttpRequestMessage(HttpMethod.Post, $"http://{host}:{_port}{path}")
		{
			Content = new StringContent(body, Encoding.UTF8, contentType)
		};
		if (origin is not null)
		{
			request.Headers.Add("Origin", origin);
		}

		return _http.SendAsync(request, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Head_ReturnsTheHeadersWithoutABody()
	{
		// http.sys refuses a body on a HEAD response, so every HEAD got a 500 on Windows.
		await StartAsync();
		File.WriteAllBytes(Path.Combine(_root, "site", "logo.png"), new byte[300]);

		HttpResponseMessage page = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{Origin}/"),
			TestContext.Current.CancellationToken);
		HttpResponseMessage image = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{Origin}/logo.png"),
			TestContext.Current.CancellationToken);
		HttpResponseMessage missing = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{Origin}/nope"),
			TestContext.Current.CancellationToken);

		page.StatusCode.Should().Be(HttpStatusCode.OK);
		page.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
		page.Content.Headers.ContentLength.Should().BeGreaterThan(0);
		(await page.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
		image.StatusCode.Should().Be(HttpStatusCode.OK);
		image.Content.Headers.ContentLength.Should().Be(300);
		missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task OtherMethods_AreNotAllowed()
	{
		await StartAsync();

		HttpResponseMessage response = await _http.PutAsync($"{Origin}/", new StringContent("x"),
			TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
	}

	[Fact]
	public async Task BlazorPreviewEndpoint_IsGone()
	{
		// No page called it, and every request loaded an assembly that could never be unloaded.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/blazor/preview", "application/json", Origin,
			"""{"source":"<h1>Hi</h1>"}""");

		response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
	}

	[Theory]
	[InlineData("127.0.0.1")]
	[InlineData("[::1]")]
	public async Task LoopbackAddresses_AreServed_AndAcceptTheirOwnOrigin(string host)
	{
		// Only http://localhost:<port> answered; http://127.0.0.1:<port> got 400 Invalid Hostname.
		Assert.SkipUnless(host != "[::1]" || CanListenOnIPv6Loopback(), "This machine has no IPv6 loopback.");
		await StartAsync();

		string html = await _http.GetStringAsync($"http://{host}:{_port}/", TestContext.Current.CancellationToken);
		HttpResponseMessage feedback = await PostAsync("/api/feedback", "application/json", $"http://{host}:{_port}",
			host: host);
		HttpResponseMessage crossHost = await PostAsync("/api/feedback", "application/json", Origin, host: host);

		html.Should().Contain("home");
		feedback.StatusCode.Should().Be(HttpStatusCode.OK);
		crossHost.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a page on localhost is another origin");
	}

	[Fact]
	public async Task Feedback_OversizedBody_IsRefusedWithoutReadingIt()
	{
		// 400 MB posted here took serve to 3.2 GB.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/feedback", "application/json", Origin,
			$$"""{"page":"/","helpful":true,"pad":"{{new string('x', 5000)}}"}""");

		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
	}

	[Fact]
	public async Task Feedback_OversizedBodyWithoutALength_StopsReadingAtTheLimit()
	{
		// A chunked body has no Content-Length to check up front, so the read itself has to stop.
		await StartAsync();
		var request = new HttpRequestMessage(HttpMethod.Post, $"{Origin}/api/feedback")
		{
			Content = new UnknownLengthContent(
				Encoding.UTF8.GetBytes($$"""{"page":"/","helpful":true,"pad":"{{new string('x', 6000)}}"}"""))
		};
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

		HttpResponseMessage response = await _http.SendAsync(request, TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
	}

	[Theory]
	[InlineData("""{"page":"/guide\nFAKE LOG LINE","helpful":true}""")]
	[InlineData("""{"page":"","helpful":true}""")]
	[InlineData("""{"page":"/guide"}""")]
	[InlineData("not json")]
	public async Task Feedback_InvalidVote_IsRefused(string body)
	{
		// The page went into the log unchecked.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/feedback", "application/json", Origin, body);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Feedback_PageLongerThanTheLimit_IsRefused()
	{
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/feedback", "application/json", Origin,
			$$"""{"page":"/{{new string('a', DocsApiRequests.MaxFeedbackPageLength)}}","helpful":true}""");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Repl_OversizedBody_IsRefused()
	{
		using var repl = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance);
		await StartAsync(repl: repl);

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "application/json", Origin,
			JsonSerializer.Serialize(new { code = new string('x', 70_000) }));

		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
	}

	[Fact]
	public async Task Repl_InvalidJson_IsABadRequest()
	{
		using var repl = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance);
		await StartAsync(repl: repl);

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "application/json", Origin, "not json");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Repl_PastTheQueueLimit_Answers429()
	{
		using var repl = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance)
		{
			MaxWaitingRuns = 0,
			Timeout = TimeSpan.FromSeconds(60)
		};
		await StartAsync(repl: repl);
		string release = "mokadocs-devserver-queue-" + Guid.NewGuid().ToString("N");

		// The run is in-process, so it can wait for the test through the app domain.
		Task<HttpResponseMessage> running = PostAsync("/api/repl/execute", "application/json", Origin,
			JsonSerializer.Serialize(new
			{
				code = $"while (AppDomain.CurrentDomain.GetData(\"{release}\") is null) System.Threading.Thread.Sleep(10); 1"
			}));
		try
		{
			DateTime until = DateTime.UtcNow.AddSeconds(30);
			while (repl.PendingRuns == 0 && DateTime.UtcNow < until)
			{
				await Task.Delay(20, TestContext.Current.CancellationToken);
			}

			HttpResponseMessage refused = await PostAsync("/api/repl/execute", "application/json", Origin,
				"""{"code":"2"}""");

			refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
			(await refused.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("Too many snippets");
		}
		finally
		{
			AppDomain.CurrentDomain.SetData(release, true);
		}

		(await running).StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task HotReloadSockets_ClosedOrDropped_AreForgotten()
	{
		// Closed sockets stayed in a ConcurrentBag for the life of the process.
		await StartAsync();
		const int cycles = 100;

		for (int i = 0; i < cycles; i++)
		{
			using ClientWebSocket browser = await ConnectBrowserAsync();
			if (i % 2 == 0)
			{
				await browser.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Ct);
			}
			else
			{
				browser.Abort();
			}
		}

		await WaitUntilAsync(() => _server!.ConnectedBrowserCount == 0);
		_server!.ConnectedBrowserCount.Should().Be(0);
	}

	[Fact]
	public async Task NotifyReload_AfterManyBrowsersCameAndWent_ReachesTheOnesStillOpen()
	{
		await StartAsync();
		for (int i = 0; i < 50; i++)
		{
			using ClientWebSocket gone = await ConnectBrowserAsync();
			await gone.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Ct);
		}

		using ClientWebSocket first = await ConnectBrowserAsync();
		using ClientWebSocket second = await ConnectBrowserAsync();
		await WaitUntilAsync(() => _server!.ConnectedBrowserCount == 2);

		await _server!.NotifyReloadAsync();

		(await ReceiveTextAsync(first)).Should().Be("reload");
		(await ReceiveTextAsync(second)).Should().Be("reload");
		_server.ConnectedBrowserCount.Should().Be(2);
	}

	[Theory]
	[InlineData("site/app.wasm", "application/wasm")]
	[InlineData("guide.pdf", "application/pdf")]
	[InlineData("intro.mp4", "video/mp4")]
	[InlineData("intro.webm", "video/webm")]
	[InlineData("hero.avif", "image/avif")]
	[InlineData("site.webmanifest", "application/manifest+json")]
	[InlineData("samples.zip", "application/zip")]
	[InlineData("index.html", "text/html; charset=utf-8")]
	[InlineData("app.js", "application/javascript; charset=utf-8")]
	[InlineData("_preview-assemblies/3f2a9c1b7d4e.dll", "application/octet-stream")]
	[InlineData("_preview-wasm/_framework/icudt.dat", "application/octet-stream")]
	[InlineData("data.bin-unknown", null)]
	public void GetContentType_CoversTheTypesASiteServes(string file, string? contentType)
	{
		// .wasm went out as application/octet-stream, so the Blazor preview runtime couldn't stream-compile.
		DevServer.GetContentType(file).Should().Be(contentType);
	}

	[Fact]
	public async Task ReplEndpoint_PlainTextPostFromAnotherSite_IsRejected()
	{
		// The request a malicious page can send without triggering a CORS preflight.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "text/plain", "https://evil.example");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task ReplEndpoint_JsonPostFromAnotherSite_IsRejected()
	{
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "application/json", "https://evil.example");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
	}

	[Fact]
	public async Task ReplEndpoint_JsonPostFromTheServedSite_ReachesTheEndpoint()
	{
		// No REPL service is registered, so reaching the endpoint means 503.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "application/json", Origin);

		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Fact]
	public async Task ReplEndpoint_JsonPostWithoutOrigin_ReachesTheEndpoint()
	{
		// Browsers always send Origin on POST, so its absence means a tool such as curl.
		await StartAsync();

		HttpResponseMessage response = await PostAsync("/api/repl/execute", "application/json", null);

		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Fact]
	public async Task FeedbackEndpoint_UnderABasePath_IsRouted()
	{
		// The feedback widget posts to {base path}/api/feedback, which used to 404.
		await StartAsync("/Sub");

		HttpResponseMessage response = await PostAsync("/Sub/api/feedback", "application/json", Origin);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task StaticFiles_AnAbsolutePathOutsideTheSite_IsNotServed()
	{
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Drive-letter paths only exist on Windows.");
		await StartAsync();
		string secret = Path.Combine(_root, "secret.txt").Replace('\\', '/');

		HttpResponseMessage response = await _http.GetAsync($"http://localhost:{_port}/{secret}",
			TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
			.Should().NotContain("outside the site");
	}

	[Fact]
	public async Task StaticFiles_ThePageItself_IsServed()
	{
		await StartAsync();

		string html = await _http.GetStringAsync($"http://localhost:{_port}/", TestContext.Current.CancellationToken);

		html.Should().Contain("home");
	}

	[Fact]
	public async Task StartAsync_PortInUse_ThrowsThePortErrorAndDisposesCleanly()
	{
		// Dispose used to call Stop on the listener that failed to start, and the resulting
		// ObjectDisposedException replaced the port error.
		await StartAsync();
		var second = new DevServer(NullLogger<DevServer>.Instance, Path.Combine(_root, "site"), _port);

		Func<Task> start = () => second.StartAsync(TestContext.Current.CancellationToken);

		await start.Should().ThrowAsync<IOException>();
		second.Invoking(s => s.Dispose()).Should().NotThrow();
	}

	[Theory]
	[InlineData("https://evil.example")]
	[InlineData("http://localhost:1")]
	[InlineData("null")]
	public async Task HotReloadSocket_FromAnotherOrigin_IsRefused(string origin)
	{
		// Browsers send no CORS preflight for a WebSocket, so any page could open one and learn of every save.
		await StartAsync();
		using var browser = new ClientWebSocket();
		browser.Options.SetRequestHeader("Origin", origin);
		browser.Options.CollectHttpResponseDetails = true;

		Func<Task> connect = () => browser.ConnectAsync(new Uri($"ws://localhost:{_port}/__mokadocs-ws"), Ct);

		await connect.Should().ThrowAsync<WebSocketException>();
		browser.HttpStatusCode.Should().Be(HttpStatusCode.Forbidden);
		_server!.ConnectedBrowserCount.Should().Be(0);
	}

	[Theory]
	[InlineData("localhost")]
	[InlineData("127.0.0.1")]
	public async Task HotReloadSocket_FromTheServedOrigin_GetsTheReload(string host)
	{
		await StartAsync();
		using var browser = new ClientWebSocket();
		browser.Options.SetRequestHeader("Origin", $"http://{host}:{_port}");

		await browser.ConnectAsync(new Uri($"ws://{host}:{_port}/__mokadocs-ws"), Ct);
		await WaitUntilAsync(() => _server!.ConnectedBrowserCount == 1);
		await _server!.NotifyReloadAsync();

		(await ReceiveTextAsync(browser)).Should().Be("reload");
	}

	[Theory]
	[InlineData("/_preview-wasm/index.html?assembly=/_preview-assemblies/a.dll")]
	[InlineData("/_preview-wasm/")]
	[InlineData("/Sub/_preview-wasm/index.html")]
	public async Task PreviewFrames_DontGetTheReloadScript(string path)
	{
		// Every Blazor preview iframe opened a reload socket of its own next to the page holding it.
		const string preview = "<html><body>preview</body></html>";
		Directory.CreateDirectory(Path.Combine(_root, "site", "_preview-wasm"));
		File.WriteAllText(Path.Combine(_root, "site", "_preview-wasm", "index.html"), preview);
		await StartAsync("/Sub");

		string page = await _http.GetStringAsync($"{Origin}/Sub/", Ct);
		HttpResponseMessage frame = await _http.GetAsync($"{Origin}{path}", Ct);

		page.Should().Contain("/__mokadocs-ws");
		page.IndexOf("/__mokadocs-ws", StringComparison.Ordinal).Should()
			.BeLessThan(page.IndexOf("</body>", StringComparison.Ordinal));
		frame.StatusCode.Should().Be(HttpStatusCode.OK);
		frame.Content.Headers.ContentType!.ToString().Should().Be("text/html; charset=utf-8");
		(await frame.Content.ReadAsStringAsync(Ct)).Should().Be(preview);
	}

	[Theory]
	[InlineData("/guide", "guide page")]
	[InlineData("/guide/", "guide page")]
	[InlineData("/about", "about page")]
	[InlineData("/Sub", "home")]
	[InlineData("/Sub/guide", "guide page")]
	[InlineData("/sub/about", "about page")]
	public async Task Pages_FoldersAndPathsWithoutAnExtension_AreServedWithoutARedirect(string path, string text)
	{
		Directory.CreateDirectory(Path.Combine(_root, "site", "guide"));
		File.WriteAllText(Path.Combine(_root, "site", "guide", "index.html"), "<html><body>guide page</body></html>");
		File.WriteAllText(Path.Combine(_root, "site", "about.html"), "<html><body>about page</body></html>");
		await StartAsync("/Sub");

		HttpResponseMessage response = await _http.GetAsync($"{Origin}{path}", Ct);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be(path, "a redirect would be followed");
		(await response.Content.ReadAsStringAsync(Ct)).Should().Contain(text).And.Contain("/__mokadocs-ws");
	}

	[Fact]
	public async Task Responses_AreNeverCached()
	{
		await StartAsync();
		File.WriteAllBytes(Path.Combine(_root, "site", "logo.png"), new byte[10]);

		HttpResponseMessage page = await _http.GetAsync($"{Origin}/", Ct);
		HttpResponseMessage image = await _http.GetAsync($"{Origin}/logo.png", Ct);

		foreach (HttpResponseMessage response in (HttpResponseMessage[])[page, image])
		{
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			response.Headers.CacheControl!.NoStore.Should().BeTrue();
			response.Headers.CacheControl.NoCache.Should().BeTrue();
			response.Headers.CacheControl.MustRevalidate.Should().BeTrue();
		}

		image.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
	}

	[Fact]
	public async Task NotFound_ServesThe404PageWithTheReloadScript()
	{
		File.WriteAllText(Path.Combine(_root, "site", "404.html"), "<html><body>lost</body></html>");
		await StartAsync();

		HttpResponseMessage response = await _http.GetAsync($"{Origin}/nope", Ct);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
		(await response.Content.ReadAsStringAsync(Ct)).Should().Contain("lost").And.Contain("/__mokadocs-ws");
	}

	[Fact]
	public async Task NotFound_WithoutA404Page_AnswersPlainText()
	{
		await StartAsync();

		HttpResponseMessage response = await _http.GetAsync($"{Origin}/nope.css", Ct);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		(await response.Content.ReadAsStringAsync(Ct)).Should().Be("404 Not Found");
	}

	[Fact]
	public async Task StaticFiles_AnExtensionWithNoContentType_IsNotServed()
	{
		File.WriteAllText(Path.Combine(_root, "site", "notes.unknownext"), "not a site file");
		await StartAsync();

		HttpResponseMessage response = await _http.GetAsync($"{Origin}/notes.unknownext", Ct);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Theory]
	[InlineData("/../secret.txt")]
	[InlineData("/%2e%2e/secret.txt")]
	[InlineData("/..%2fsecret.txt")]
	[InlineData("/..%5csecret.txt")]
	[InlineData("/%2E%2E%5Csecret.txt")]
	public async Task StaticFiles_TraversalOutOfTheSite_IsNotServed(string target)
	{
		await StartAsync();

		string response = await SendRawGetAsync(target);

		response.Should().StartWith("HTTP/1.1 4");
		response.Should().NotContain("outside the site");
	}

	[Fact]
	public async Task OtherHostNames_AreRefused()
	{
		// A page whose DNS name is rebound to 127.0.0.1 would read the site under its own origin.
		// HttpListener refused hosts it had no prefix for; Kestrel answers any.
		await StartAsync();
		var request = new HttpRequestMessage(HttpMethod.Get, $"{Origin}/");
		request.Headers.Host = $"attacker.example:{_port}";

		HttpResponseMessage response = await _http.SendAsync(request, Ct);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		(await response.Content.ReadAsStringAsync(Ct)).Should().NotContain("home");
	}

	[Theory]
	[InlineData("PUT")]
	[InlineData("DELETE")]
	[InlineData("PATCH")]
	[InlineData("OPTIONS")]
	public async Task ApiRequests_OtherMethodsFromAnotherSite_AreRejected(string method)
	{
		await StartAsync();
		var request = new HttpRequestMessage(new HttpMethod(method), $"{Origin}/api/repl/execute");
		request.Headers.Add("Origin", "https://evil.example");

		HttpResponseMessage response = await _http.SendAsync(request, Ct);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
	}

	[Fact]
	public async Task Extensions_AddPageHtmlAndEndpointsBehindTheSameOriginCheck()
	{
		Directory.CreateDirectory(Path.Combine(_root, "site", "_preview-wasm"));
		File.WriteAllText(Path.Combine(_root, "site", "_preview-wasm", "index.html"), "<html><body>preview</body></html>");
		await StartAsync("/Sub", extension: new LabExtension());

		string page = await _http.GetStringAsync($"{Origin}/", Ct);
		string preview = await _http.GetStringAsync($"{Origin}/_preview-wasm/index.html", Ct);
		HttpResponseMessage save = await PostAsync("/Sub/api/lab/save", "application/json", Origin, "{}");
		HttpResponseMessage crossSite = await PostAsync("/api/lab/save", "application/json", "https://evil.example", "{}");

		page.IndexOf(LabExtension.Html, StringComparison.Ordinal).Should()
			.BeGreaterThan(page.IndexOf("/__mokadocs-ws", StringComparison.Ordinal))
			.And.BeLessThan(page.IndexOf("</body>", StringComparison.Ordinal));
		preview.Should().NotContain(LabExtension.Html);
		save.StatusCode.Should().Be(HttpStatusCode.Accepted);
		crossSite.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task Dispose_WithABrowserConnected_ReturnsPromptly()
	{
		// Kestrel waits for open requests when it stops, and a reload socket lasts as long as its tab.
		await StartAsync();
		using ClientWebSocket browser = await ConnectBrowserAsync();
		await WaitUntilAsync(() => _server!.ConnectedBrowserCount == 1);
		var watch = Stopwatch.StartNew();

		await _server!.DisposeAsync();

		watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
	}

	[Theory]
	[InlineData("http://localhost:5080", "same-origin", "application/json", true)]
	[InlineData("http://localhost:5080", null, "application/json; charset=utf-8", true)]
	[InlineData(null, null, "application/json", true)]
	[InlineData("http://localhost:5080", "same-origin", "text/plain", false)]
	[InlineData("http://localhost:5080", "same-origin", null, false)]
	[InlineData("https://evil.example", "cross-site", "application/json", false)]
	[InlineData("http://localhost:5081", "same-site", "application/json", false)]
	[InlineData("null", null, "application/json", false)]
	public void IsSameOriginApiRequest_OnlyAcceptsJsonFromThisServer(string? origin, string? secFetchSite,
		string? contentType, bool expected) =>
		DevServer.IsSameOriginApiRequest(origin, secFetchSite, contentType, 5080).Should().Be(expected);

	[Theory]
	[InlineData("http://127.0.0.1:5080", "127.0.0.1:5080", true)]
	[InlineData("http://[::1]:5080", "[::1]:5080", true)]
	[InlineData("http://localhost:5080", "localhost:5080", true)]
	[InlineData("http://localhost:5080", "127.0.0.1:5080", false)]
	[InlineData("http://127.0.0.1:5080", "localhost:5080", false)]
	[InlineData("http://attacker.example:5080", "attacker.example:5080", false)]
	[InlineData("http://127.0.0.1:5081", "127.0.0.1:5081", false)]
	public void IsSameOriginApiRequest_MatchesTheOriginToTheHostItWasSentTo(string origin, string host, bool expected) =>
		DevServer.IsSameOriginApiRequest(origin, "same-origin", "application/json", 5080, host).Should().Be(expected);

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static bool CanListenOnIPv6Loopback()
	{
		if (!Socket.OSSupportsIPv6)
		{
			return false;
		}

		try
		{
			using var listener = new TcpListener(IPAddress.IPv6Loopback, 0);
			listener.Start();
			return true;
		}
		catch (SocketException)
		{
			return false;
		}
	}

	/// <summary>Sends a request target exactly as given; HttpClient would remove the dot segments.</summary>
	private async Task<string> SendRawGetAsync(string target)
	{
		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, _port, Ct);
		await using NetworkStream stream = client.GetStream();
		await stream.WriteAsync(
			Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\nHost: localhost:{_port}\r\nConnection: close\r\n\r\n"), Ct);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return await reader.ReadToEndAsync(Ct);
	}

	private async Task<ClientWebSocket> ConnectBrowserAsync()
	{
		var browser = new ClientWebSocket();
		await browser.ConnectAsync(new Uri($"ws://localhost:{_port}/__mokadocs-ws"), Ct);
		return browser;
	}

	private static async Task<string> ReceiveTextAsync(ClientWebSocket browser)
	{
		byte[] buffer = new byte[64];
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		timeout.CancelAfter(TimeSpan.FromSeconds(30));
		WebSocketReceiveResult result = await browser.ReceiveAsync(buffer, timeout.Token);
		return Encoding.UTF8.GetString(buffer, 0, result.Count);
	}

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		DateTime until = DateTime.UtcNow + TimeSpan.FromSeconds(30);
		while (!condition() && DateTime.UtcNow < until)
		{
			await Task.Delay(20, Ct);
		}
	}

	/// <summary>What a dev-only tool such as a theme editor adds: a script on pages and an endpoint.</summary>
	private sealed class LabExtension : IDevServerExtension
	{
		public const string Html = "<script>/* lab */</script>";

		public string PageHtml => Html;

		public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
			endpoints.MapPost("/api/lab/save", context =>
			{
				context.Response.StatusCode = (int)HttpStatusCode.Accepted;
				return Task.CompletedTask;
			});
	}

	/// <summary>A body sent chunked, with no Content-Length header.</summary>
	private sealed class UnknownLengthContent(byte[] body) : HttpContent
	{
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
			stream.WriteAsync(body).AsTask();

		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}
	}
}
