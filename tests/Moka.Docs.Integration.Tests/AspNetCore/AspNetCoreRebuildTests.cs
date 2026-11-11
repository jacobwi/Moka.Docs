using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     When the in-memory site is built: at startup, on a change to the docs folder, or on every
///     request without a cache. The first reader used to wait for the whole build.
/// </summary>
public sealed class AspNetCoreRebuildTests : IDisposable
{
	private static readonly TimeSpan _wait = TimeSpan.FromSeconds(30);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-rebuild-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreRebuildTests()
	{
		Directory.CreateDirectory(_root);
		WriteHome("Version one");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

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
	public async Task BuildOnStartup_BuildsBeforeAnyRequest_WithEveryEndpointMapped()
	{
		// The OpenAPI document a plugin reads during the build lists the app's endpoints, so the
		// build has to wait until all of them are mapped, including those mapped after the docs.
		var probe = new EndpointProbePlugin();
		await using DocsTestHost host = await StartAsync(
			options => options.Plugins = [new PluginEntry { Id = EndpointProbePlugin.PluginId }],
			app =>
			{
				app.MapMokaDocs();
				app.MapGet("/health", () => "ok");
			},
			services => services.AddSingleton<IMokaPlugin>(sp => probe.WithServices(sp)));

		host.Startup.StartupBuild.Should().NotBeNull();
		await host.Startup.StartupBuild!.WaitAsync(_wait, Ct);

		host.Docs.CurrentSite.Should().NotBeNull();
		probe.Routes.Should().Contain(["/docs/{**path}", "/health"]);
	}

	[Fact]
	public async Task BuildOnStartup_Off_LeavesTheBuildToTheFirstRequest()
	{
		await using DocsTestHost host = await StartAsync(options => options.BuildOnStartup = false);

		host.Startup.StartupBuild.Should().BeNull();
		host.Docs.CurrentSite.Should().BeNull();
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");
		host.Docs.CurrentSite.Should().NotBeNull();
	}

	[Fact]
	public async Task BuildOnStartup_WithoutMapMokaDocs_BuildsAndWatchesNothing()
	{
		// An app that maps the docs in Development only still calls AddMokaDocs everywhere.
		await using DocsTestHost host = await StartAsync(options => options.WatchDocs = true, app => { });

		host.Startup.StartupBuild.Should().BeNull();
		host.Startup.WatchedDirectory.Should().BeNull();
	}

	[Fact]
	public async Task WatchDocs_AChangedFile_RebuildsInTheBackground_AndServesThePreviousSiteUntilTheSwap()
	{
		var gate = new GatePlugin();
		await using DocsTestHost host = await StartAsync(
			options =>
			{
				options.WatchDocs = true;
				options.Plugins = [new PluginEntry { Id = GatePlugin.PluginId }];
			},
			register: services => services.AddSingleton<IMokaPlugin>(gate));
		await host.Startup.StartupBuild!.WaitAsync(_wait, Ct);
		host.Startup.WatchedDirectory.Should().Be(Path.Combine(_root, "docs"));
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");

		WriteHome("Version two");
		await gate.RebuildStarted.Task.WaitAsync(_wait, Ct);

		// The rebuild holds the build lock; requests must not wait for it.
		string during = await host.GetTextAsync("/docs").WaitAsync(TimeSpan.FromSeconds(10), Ct);
		during.Should().Contain("Version one");

		gate.Release.SetResult();
		string after = await PollAsync(host, "/docs", html => html.Contains("Version two"));
		after.Should().Contain("Version two");
	}

	[Theory]
	[InlineData(null, "Development", true)]
	[InlineData(null, "Production", false)]
	[InlineData(true, "Production", true)]
	[InlineData(false, "Development", false)]
	public void WatchDocs_DefaultsToDevelopmentOnly(bool? watchDocs, string environment, bool watched)
	{
		var options = new MokaDocsOptions { DocsPath = "docs", WatchDocs = watchDocs };

		MokaDocsStartupService.ShouldWatch(options, new TestEnvironment(environment)).Should().Be(watched);
		MokaDocsStartupService.ShouldWatch(new MokaDocsOptions { WatchDocs = true }, new TestEnvironment(environment))
			.Should().BeFalse("there is no docs folder to watch");
		MokaDocsStartupService.ShouldWatch(
				new MokaDocsOptions { DocsPath = "docs", WatchDocs = true, CacheOutput = false }, new TestEnvironment(environment))
			.Should().BeFalse("every request rebuilds without a cache");
	}

	[Fact]
	public async Task CacheOutput_Off_StillRebuildsOnEveryRequest()
	{
		await using DocsTestHost host = await StartAsync(options =>
		{
			options.CacheOutput = false;
			options.WatchDocs = true;
		});
		host.Startup.StartupBuild.Should().BeNull();
		host.Startup.WatchedDirectory.Should().BeNull();
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");

		WriteHome("Version two");

		(await host.GetTextAsync("/docs")).Should().Contain("Version two");
	}

	[Fact]
	public async Task RebuildAsync_SwapsTheSite_WhileGetSiteAsyncKeepsAnsweringWithTheOldOne()
	{
		var gate = new GatePlugin();
		await using DocsTestHost host = await StartAsync(
			options =>
			{
				options.BuildOnStartup = false;
				options.Plugins = [new PluginEntry { Id = GatePlugin.PluginId }];
			},
			register: services => services.AddSingleton<IMokaPlugin>(gate));
		InMemorySite first = await host.Docs.GetSiteAsync(Ct);

		WriteHome("Version two");
		Task rebuild = host.Docs.RebuildAsync(Ct);
		await gate.RebuildStarted.Task.WaitAsync(_wait, Ct);
		InMemorySite during = await host.Docs.GetSiteAsync(Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);
		gate.Release.SetResult();
		await rebuild.WaitAsync(_wait, Ct);

		during.Should().BeSameAs(first);
		InMemorySite after = await host.Docs.GetSiteAsync(Ct);
		after.Should().NotBeSameAs(first);
		System.Text.Encoding.UTF8.GetString(after.Files["index.html"].Content).Should().Contain("Version two");
	}

	private Task<DocsTestHost> StartAsync(Action<MokaDocsOptions>? configure = null, Action<WebApplication>? map = null,
		Action<IServiceCollection>? register = null) =>
		DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			configure?.Invoke(options);
		}, map, register: register);

	private static async Task<string> PollAsync(DocsTestHost host, string path, Func<string, bool> done)
	{
		DateTime until = DateTime.UtcNow + _wait;
		string html;
		do
		{
			html = await host.GetTextAsync(path);
			if (done(html))
			{
				break;
			}

			await Task.Delay(100, Ct);
		} while (DateTime.UtcNow < until);

		return html;
	}

	private void WriteHome(string text)
	{
		string path = Path.Combine(_root, "docs", "index.md");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, $"---\ntitle: Home\n---\n\n{text}\n");
	}

	/// <summary>Records the app's routes as the build sees them.</summary>
	private sealed class EndpointProbePlugin : IMokaPlugin
	{
		public const string PluginId = "endpoint-probe";
		private IServiceProvider? _services;

		public List<string> Routes { get; private set; } = [];
		public string Id => PluginId;
		public string Name => "Endpoint probe";
		public string Version => "1.0.0";

		public EndpointProbePlugin WithServices(IServiceProvider services)
		{
			_services = services;
			return this;
		}

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;

		public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			Routes = _services!.GetRequiredService<EndpointDataSource>().Endpoints
				.OfType<RouteEndpoint>()
				.Select(e => e.RoutePattern.RawText ?? "")
				.ToList();
			return Task.CompletedTask;
		}
	}

	/// <summary>Holds the second build, the first rebuild, until the test releases it.</summary>
	private sealed class GatePlugin : IMokaPlugin
	{
		public const string PluginId = "gate";
		private int _builds;

		public TaskCompletionSource RebuildStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public string Id => PluginId;
		public string Name => "Gate";
		public string Version => "1.0.0";

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;

		public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			if (Interlocked.Increment(ref _builds) == 2)
			{
				RebuildStarted.SetResult();
				await Release.Task.WaitAsync(ct);
			}
		}
	}

	private sealed class TestEnvironment(string name) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = name;
		public string ApplicationName { get; set; } = "Moka.Docs.Integration.Tests";
		public string ContentRootPath { get; set; } = "";
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
