using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Docs.AspNetCore;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     A real web app on a free loopback port with <c>AddMokaDocs</c> and, unless the test maps
///     something else, <c>MapMokaDocs()</c>. The project has no TestServer, so requests go
///     through Kestrel.
/// </summary>
internal sealed class DocsTestHost : IAsyncDisposable
{
	private DocsTestHost(WebApplication app, TestLoggerProvider logs)
	{
		App = app;
		Logs = logs;
		Client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
	}

	public WebApplication App { get; }

	public HttpClient Client { get; }

	public TestLoggerProvider Logs { get; }

	/// <summary>The app's own origin, as a browser on one of its pages sends it.</summary>
	public string Origin => Client.BaseAddress!.GetLeftPart(UriPartial.Authority);

	public MokaDocsService Docs => App.Services.GetRequiredService<MokaDocsService>();

	internal MokaDocsStartupService Startup =>
		App.Services.GetServices<IHostedService>().OfType<MokaDocsStartupService>().Single();

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await App.StopAsync(CancellationToken.None);
		await App.DisposeAsync();
	}

	/// <summary>
	///     Starts the app and, unless <paramref name="waitForStartupBuild" /> is false, waits for the
	///     build it starts: until that is done, docs requests get 503.
	/// </summary>
	public static async Task<DocsTestHost> StartAsync(
		string contentRoot,
		Action<MokaDocsOptions>? configure = null,
		Action<WebApplication>? map = null,
		string environment = "Production",
		Action<IServiceCollection>? register = null,
		bool waitForStartupBuild = true)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ContentRootPath = contentRoot,
			EnvironmentName = environment
		});
		builder.WebHost.UseUrls("http://127.0.0.1:0");

		var logs = new TestLoggerProvider();
		builder.Logging.ClearProviders();
		builder.Logging.AddProvider(logs);

		register?.Invoke(builder.Services);
		builder.Services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			configure?.Invoke(options);
		});

		WebApplication app = builder.Build();
		if (map is null)
		{
			app.MapMokaDocs();
		}
		else
		{
			map(app);
		}

		await app.StartAsync(Ct);
		var host = new DocsTestHost(app, logs);
		if (waitForStartupBuild && host.Startup.StartupBuild is { } build)
		{
			await build.WaitAsync(TimeSpan.FromSeconds(60), Ct);
		}

		return host;
	}

	public Task<HttpResponseMessage> GetAsync(string path, string? ifNoneMatch = null, string? acceptEncoding = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (ifNoneMatch is not null)
		{
			request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
		}

		if (acceptEncoding is not null)
		{
			request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
		}

		return Client.SendAsync(request, Ct);
	}

	/// <summary>Sends a request built by the test.</summary>
	public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Action<HttpRequestMessage>? configure = null)
	{
		var request = new HttpRequestMessage(method, path);
		configure?.Invoke(request);
		return Client.SendAsync(request, Ct);
	}

	public async Task<string> GetTextAsync(string path)
	{
		HttpResponseMessage response = await GetAsync(path);
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, "GET {0} should be served", path);
		return await response.Content.ReadAsStringAsync(Ct);
	}

	/// <summary>Posts like a browser on one of the app's own pages, unless told otherwise.</summary>
	public Task<HttpResponseMessage> PostAsync(string path, string body, string contentType = "application/json",
		string? origin = "", string? fetchSite = "same-origin", Action<HttpRequestMessage>? configure = null,
		CancellationToken cancellationToken = default)
	{
		var request = new HttpRequestMessage(HttpMethod.Post, path)
		{
			Content = new StringContent(body, Encoding.UTF8)
		};
		request.Content.Headers.Remove("Content-Type");
		request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

		// An empty origin stands for the app's own.
		if (origin is not null)
		{
			request.Headers.TryAddWithoutValidation("Origin", origin.Length == 0 ? Origin : origin);
		}

		if (fetchSite is not null)
		{
			request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", fetchSite);
		}

		configure?.Invoke(request);
		return Client.SendAsync(request, cancellationToken.CanBeCanceled ? cancellationToken : Ct);
	}
}

/// <summary>Keeps every log entry, so a test can check what the app reported.</summary>
internal sealed class TestLoggerProvider : ILoggerProvider
{
	private readonly ConcurrentQueue<LogEntry> _entries = new();

	public IReadOnlyList<LogEntry> Entries => [.. _entries];

	public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

	public void Dispose()
	{
	}

	public IEnumerable<string> Messages(LogLevel level) =>
		Entries.Where(e => e.Level == level).Select(e => e.Message);

	internal sealed record LogEntry(LogLevel Level, string Category, string Message);

	private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter) =>
			entries.Enqueue(new LogEntry(logLevel, category, formatter(state, exception)));
	}
}
