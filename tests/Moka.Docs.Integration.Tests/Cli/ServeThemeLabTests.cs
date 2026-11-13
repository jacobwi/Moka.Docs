using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     <c>mokadocs serve</c> adds the theme lab to its pages unless <c>--no-lab</c> is given, and a save
///     from the lab rebuilds the site like any other edit to mokadocs.yaml.
/// </summary>
[Collection(EndToEndCollection.Name)]
public sealed class ServeThemeLabTests : IDisposable
{
	private static readonly TimeSpan _patience = TimeSpan.FromSeconds(60);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-serve-lab-" + Guid.NewGuid().ToString("N"));

	public ServeThemeLabTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nThe home page.\n");
		File.WriteAllText(Yaml, "# lab test\nsite:\n  title: Lab\n");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private string Yaml => Path.Combine(_root, "mokadocs.yaml");

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public void CreateThemeLab_IsNullWithNoLab()
	{
		ServeCommand.CreateThemeLab(false, Yaml, NullLoggerFactory.Instance).Should().BeNull();
		ServeCommand.CreateThemeLab(true, Yaml, NullLoggerFactory.Instance)!.PageHtml.Should().Contain("/api/theme-lab/lab.js");
	}

	[Fact]
	public async Task Serve_AddsTheLab_AndASaveRebuildsTheSite()
	{
		using var http = new HttpClient();
		using var stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		(Task<int> serving, int port) = await StartServeAsync([], stop.Token);
		string origin = $"http://localhost:{port}";
		try
		{
			(await Get(http, origin + "/")).Should().Contain("<script src=\"/api/theme-lab/lab.js\" defer></script>");

			using var save = new HttpRequestMessage(HttpMethod.Post, origin + "/api/theme-lab/save")
			{
				Content = new StringContent("""{ "values": { "layout.width": "wide" } }""", Encoding.UTF8, "application/json")
			};
			save.Headers.Add("Origin", origin);
			(await http.SendAsync(save, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);

			File.ReadAllText(Yaml).Should().Be("# lab test\nsite:\n  title: Lab\ntheme:\n  options:\n    layout:\n      width: wide\n");
			await EventuallyAsync(async () => (await Get(http, origin + "/")).Contains("data-width=\"wide\""));
		}
		finally
		{
			await stop.CancelAsync();
		}

		(await serving.WaitAsync(_patience, Ct)).Should().Be(0);
	}

	[Fact]
	public async Task Serve_WithNoLab_LeavesItOut()
	{
		using var http = new HttpClient();
		using var stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		(Task<int> serving, int port) = await StartServeAsync(["--no-lab"], stop.Token);
		try
		{
			(await Get(http, $"http://localhost:{port}/")).Should().Contain("The home page.").And.NotContain("theme-lab");
			(await http.GetAsync($"http://localhost:{port}/api/theme-lab/catalog", Ct)).StatusCode.Should().Be(HttpStatusCode.NotFound);
		}
		finally
		{
			await stop.CancelAsync();
		}

		(await serving.WaitAsync(_patience, Ct)).Should().Be(0);
	}

	private async Task<(Task<int> Serving, int Port)> StartServeAsync(string[] options, CancellationToken stop)
	{
		// The port is free when picked, but a parallel test can take it before serve binds it.
		for (int attempt = 1; ; attempt++)
		{
			int port = FreePort();
			Task<int> serving = ServeCommand.Create()
				.Parse(["--config", Yaml, "--port", port.ToString(), "--no-open", .. options])
				.InvokeAsync(cancellationToken: stop);

			using var http = new HttpClient();
			DateTime until = DateTime.UtcNow + _patience;
			while (!serving.IsCompleted && DateTime.UtcNow < until)
			{
				if ((await Get(http, $"http://localhost:{port}/")).Length > 0)
				{
					return (serving, port);
				}

				await Task.Delay(100, Ct);
			}

			if (!serving.IsCompleted || attempt == 5)
			{
				throw new TimeoutException($"serve didn't answer on port {port}");
			}
		}
	}

	private static int FreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private static async Task<string> Get(HttpClient http, string url)
	{
		try
		{
			using HttpResponseMessage response = await http.GetAsync(url, Ct);
			return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(Ct) : "";
		}
		catch (HttpRequestException)
		{
			return "";
		}
	}

	// An edit made before the watcher started is never seen, so the file is written again, unchanged,
	// every few seconds: that still rebuilds.
	private async Task EventuallyAsync(Func<Task<bool>> condition)
	{
		DateTime until = DateTime.UtcNow + _patience;
		DateTime nudge = DateTime.UtcNow + TimeSpan.FromSeconds(3);
		while (!await condition())
		{
			if (DateTime.UtcNow > until)
			{
				throw new TimeoutException("The site wasn't rebuilt with the saved value.");
			}

			if (DateTime.UtcNow > nudge)
			{
				File.WriteAllText(Yaml, File.ReadAllText(Yaml));
				nudge = DateTime.UtcNow + TimeSpan.FromSeconds(3);
			}

			await Task.Delay(200, Ct);
		}
	}
}
