using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     The theme lab on a running dev server: its script on every page but not in Blazor preview
///     frames, and its save behind the server's same-origin check, since it writes to the developer's
///     files.
/// </summary>
public sealed class ThemeLabServerTests : IAsyncLifetime
{
	private const string _yaml = "site:\n  title: Docs\n";
	private const string _script = "<script src=\"/api/theme-lab/lab.js\" defer></script>";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-lab-server-" + Guid.NewGuid().ToString("N"));
	private readonly HttpClient _http = new();
	private int _port;
	private DevServer? _server;

	private string Origin => $"http://localhost:{_port}";

	private string ConfigPath => Path.Combine(_root, "mokadocs.yaml");

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public ValueTask InitializeAsync()
	{
		Directory.CreateDirectory(Path.Combine(_root, "site", "_preview-wasm"));
		File.WriteAllText(Path.Combine(_root, "site", "index.html"), "<html><body>home</body></html>");
		File.WriteAllText(Path.Combine(_root, "site", "404.html"), "<html><body>missing</body></html>");
		File.WriteAllText(Path.Combine(_root, "site", "_preview-wasm", "index.html"), "<html><body>preview</body></html>");
		File.WriteAllText(ConfigPath, _yaml);
		return ValueTask.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		if (_server is not null)
		{
			await _server.DisposeAsync();
		}

		_http.Dispose();
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
		// FreePort releases the port before the server binds it, and a parallel test can take it in between.
		for (int attempt = 1; ; attempt++)
		{
			_port = FreePort();
			_server = new DevServer(NullLogger<DevServer>.Instance, Path.Combine(_root, "site"), _port, null, basePath);
			_server.Extensions.Add(new ThemeLabExtension(ConfigPath));
			try
			{
				await _server.StartAsync(Ct);
				return;
			}
			catch (IOException) when (attempt < 5)
			{
				await _server.DisposeAsync();
			}
		}
	}

	private Task<HttpResponseMessage> PostSaveAsync(string body, string? origin, string contentType = "application/json",
		string path = "/api/theme-lab/save")
	{
		var request = new HttpRequestMessage(HttpMethod.Post, Origin + path)
		{
			Content = new StringContent(body, Encoding.UTF8, contentType)
		};
		if (origin is not null)
		{
			request.Headers.Add("Origin", origin);
		}

		return _http.SendAsync(request, Ct);
	}

	[Fact]
	public async Task Pages_GetTheScript_AfterTheReloadScript_ButPreviewFramesDont()
	{
		await StartAsync();

		string home = await _http.GetStringAsync(Origin + "/", Ct);
		HttpResponseMessage missing = await _http.GetAsync(Origin + "/nowhere", Ct);
		string preview = await _http.GetStringAsync(Origin + "/_preview-wasm/index.html", Ct);

		home.IndexOf(_script, StringComparison.Ordinal).Should()
			.BeGreaterThan(home.IndexOf("/__mokadocs-ws", StringComparison.Ordinal))
			.And.BeLessThan(home.IndexOf("</body>", StringComparison.Ordinal));
		missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
		(await missing.Content.ReadAsStringAsync(Ct)).Should().Contain(_script);
		preview.Should().NotContain("theme-lab");
	}

	[Fact]
	public async Task Script_IsServedWithItsStylesInlined()
	{
		await StartAsync();

		HttpResponseMessage response = await _http.GetAsync(Origin + "/api/theme-lab/lab.js", Ct);
		string script = await response.Content.ReadAsStringAsync(Ct);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType!.ToString().Should().Be("application/javascript; charset=utf-8");
		response.Headers.CacheControl!.NoStore.Should().BeTrue();
		script.Should().Contain("attachShadow").And.Contain(":host { all: initial; }").And.NotContain("__LAB_CSS__");
	}

	[Fact]
	public async Task Catalog_AndSave_WorkUnderTheBasePath()
	{
		await StartAsync("/Sub");

		string page = await _http.GetStringAsync(Origin + "/Sub/", Ct);
		using JsonDocument catalog = JsonDocument.Parse(await _http.GetStringAsync(Origin + "/Sub/api/theme-lab/catalog", Ct));
		string version = catalog.RootElement.GetProperty("version").GetString()!;
		HttpResponseMessage save = await PostSaveAsync($$"""{ "version": "{{version}}", "values": { "style": "paper" } }""", Origin,
			path: "/Sub/api/theme-lab/save");

		page.Should().Contain(_script);
		save.StatusCode.Should().Be(HttpStatusCode.OK);
		File.ReadAllText(ConfigPath).Should().Be(_yaml + "theme:\n  options:\n    style: paper\n");
	}

	[Theory]
	[InlineData("https://evil.example", "application/json")]
	[InlineData("http://127.0.0.1:1", "application/json")]
	[InlineData(null, "text/plain")]
	public async Task Save_FromAnotherOrigin_OrNotJson_IsForbidden(string? origin, string contentType)
	{
		await StartAsync();

		HttpResponseMessage response = await PostSaveAsync("""{ "values": { "style": "paper" } }""", origin, contentType);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
		File.ReadAllText(ConfigPath).Should().Be(_yaml);
	}

	[Fact]
	public async Task Save_BodyOverTheCap_IsRefusedUnread()
	{
		await StartAsync();

		HttpResponseMessage response = await PostSaveAsync(
			"{ \"values\": { \"banner.text\": \"" + new string('a', ThemeLabExtension.MaxSaveBodyBytes) + "\" } }", Origin);

		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
		File.ReadAllText(ConfigPath).Should().Be(_yaml);
	}

	[Fact]
	public async Task Save_InvalidJson_IsABadRequest()
	{
		await StartAsync();

		HttpResponseMessage response = await PostSaveAsync("{ values", Origin);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Save_SameOrigin_WritesTheFile()
	{
		await StartAsync();

		HttpResponseMessage response = await PostSaveAsync("""{ "values": { "layout.navbar": "tabs", "sidebarToggle": true } }""", Origin);
		using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Headers.CacheControl!.NoStore.Should().BeTrue();
		body.RootElement.GetProperty("changed").EnumerateArray().Select(e => e.GetString()).Should().Equal("layout.navbar", "sidebarToggle");
		File.ReadAllText(ConfigPath).Should().Be(_yaml + "theme:\n  options:\n    layout:\n      navbar: tabs\n    sidebarToggle: true\n");
	}
}
