using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     Runs a real dev server. The REPL and Blazor preview endpoints execute the code posted
///     to them, and used to accept it from any web page open in the same browser.
/// </summary>
public sealed class DevServerTests : IAsyncLifetime
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-devserver-" + Guid.NewGuid().ToString("N"));
	private readonly int _port = FreePort();
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

	private async Task StartAsync(string basePath = "/")
	{
		_server = new DevServer(NullLogger<DevServer>.Instance, Path.Combine(_root, "site"), _port, basePath: basePath);
		await _server.StartAsync(TestContext.Current.CancellationToken);
	}

	private Task<HttpResponseMessage> PostAsync(string path, string contentType, string? origin)
	{
		var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{_port}{path}")
		{
			Content = new StringContent("""{"code":"1 + 1","page":"/","helpful":true}""", Encoding.UTF8, contentType)
		};
		if (origin is not null)
		{
			request.Headers.Add("Origin", origin);
		}

		return _http.SendAsync(request, TestContext.Current.CancellationToken);
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
}
