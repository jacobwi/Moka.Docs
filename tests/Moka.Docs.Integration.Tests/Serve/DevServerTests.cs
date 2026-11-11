using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
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

	private async Task StartAsync(string basePath = "/", ReplExecutionService? repl = null)
	{
		// FreePort releases the port before the server binds it, and a client socket from a test
		// running in parallel can take it in between ("Address already in use" on Linux CI).
		for (int attempt = 1; ; attempt++)
		{
			_server = new DevServer(NullLogger<DevServer>.Instance, Path.Combine(_root, "site"), _port, repl,
				basePath);
			try
			{
				await _server.StartAsync(TestContext.Current.CancellationToken);
				return;
			}
			catch (HttpListenerException) when (attempt < 5)
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
		Assert.SkipUnless(host != "[::1]" || (OperatingSystem.IsWindows() && Socket.OSSupportsIPv6),
			"Only http.sys takes an IPv6 prefix.");
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
			$$"""{"page":"/{{new string('a', DevServer.MaxFeedbackPageLength)}}","helpful":true}""");

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
	[InlineData("data.bin-unknown", "application/octet-stream")]
	public void GetContentType_CoversTheTypesASiteServes(string file, string contentType)
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

		await start.Should().ThrowAsync<HttpListenerException>();
		second.Invoking(s => s.Dispose()).Should().NotThrow();
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
