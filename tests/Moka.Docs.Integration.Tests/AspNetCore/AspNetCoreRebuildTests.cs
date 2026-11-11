using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

	[Fact]
	public async Task AFailingBuild_IsNotRunAgainForEveryRequest_AndAnswers503()
	{
		// Failed builds weren't remembered, so every request, CSS and scripts included, ran the whole
		// build again under the lock and got a 500.
		var failing = new FailingPhase { Failing = true };
		await using DocsTestHost host = await StartAsync(options => options.BuildOnStartup = false,
			register: services => services.AddSingleton<IBuildPhase>(failing));
		host.Docs.FailureRetryDelay = TimeSpan.FromMinutes(5);

		HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 6)
			.Select(_ => host.GetAsync("/docs")));

		responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable));
		responses[0].Headers.RetryAfter.Should().NotBeNull();
		failing.Runs.Should().Be(1);
		host.Logs.Messages(LogLevel.Error).Where(m => m.Contains("The MokaDocs build failed")).Should().ContainSingle();
		await FluentActions.Awaiting(() => host.Docs.GetSiteAsync(Ct))
			.Should().ThrowAsync<InvalidOperationException>().WithMessage("*failed to build*phase failed*");

		failing.Failing = false;
		host.Docs.FailureRetryDelay = TimeSpan.Zero;
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");
		failing.Runs.Should().Be(2);
	}

	[Fact]
	public async Task AFailingRebuild_KeepsServingTheLastSiteThatBuilt()
	{
		var failing = new FailingPhase();
		await using DocsTestHost host = await StartAsync(options => options.BuildOnStartup = false,
			register: services => services.AddSingleton<IBuildPhase>(failing));
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");

		failing.Failing = true;
		WriteHome("Version two");
		host.Docs.InvalidateCache();

		(await host.GetTextAsync("/docs")).Should().Contain("Version one");
		(await host.GetTextAsync("/docs")).Should().Contain("Version one");
		failing.Runs.Should().Be(2, "the second request reuses the failure instead of building again");
	}

	[Fact]
	public async Task GetSiteAsync_ACallerWhoStopsWaiting_DoesNotCancelTheBuild()
	{
		// The build ran on the first request's token, so a reader who left cancelled it for everyone.
		var gate = new GatePlugin(1);
		await using DocsTestHost host = await StartAsync(
			options =>
			{
				options.BuildOnStartup = false;
				options.Plugins = [new PluginEntry { Id = GatePlugin.PluginId }];
			},
			register: services => services.AddSingleton<IMokaPlugin>(gate));

		using (var leave = CancellationTokenSource.CreateLinkedTokenSource(Ct))
		{
			Task<InMemorySite> first = host.Docs.GetSiteAsync(leave.Token);
			await gate.RebuildStarted.Task.WaitAsync(_wait, Ct);
			await leave.CancelAsync();
			await FluentActions.Awaiting(() => first).Should().ThrowAsync<OperationCanceledException>();
		}

		gate.Release.SetResult();
		InMemorySite site = await host.Docs.GetSiteAsync(Ct).WaitAsync(_wait, Ct);

		gate.Builds.Should().Be(1);
		gate.SawCancellation.Should().BeFalse();
		System.Text.Encoding.UTF8.GetString(site.Files["index.html"].Content).Should().Contain("Version one");
	}

	[Fact]
	public async Task ALockedDocsFile_IsSkippedWithAWarning_AndTheRestBuilds()
	{
		// Every file in DocsPath was read, video and editor files included, and one held open by
		// another process threw, failing the whole build.
		File.WriteAllText(Path.Combine(_root, "docs", "notes.md"), "---\ntitle: Notes\n---\n\nNotes.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "recording.mov"), "not published");
		await using DocsTestHost host = await StartAsync(options => options.BuildOnStartup = false);

		InMemorySite site;
		using (Lock("notes.md"))
		using (Lock("recording.mov"))
		{
			site = await host.Docs.GetSiteAsync(Ct);
		}

		System.Text.Encoding.UTF8.GetString(site.Files["index.html"].Content).Should().Contain("Version one");
		site.Files.Keys.Should().NotContain(key => key.StartsWith("notes", StringComparison.Ordinal));
		site.Diagnostics.Should().Contain(d => d.Message.Contains("notes.md") && d.Message.Contains("can't be read"));
		site.Diagnostics.Should().NotContain(d => d.Message.Contains("recording.mov"),
			"a file the site doesn't publish is never read");

		FileStream Lock(string name) =>
			new(Path.Combine(_root, "docs", name), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
	}

	[Fact]
	public async Task DocsPathAtTheContentRoot_LeavesTheAppsSettingsAndBuildOutputOut()
	{
		// With DocsPath ".", /docs/appsettings.json returned the connection strings, and bin/ and obj/
		// published the app's dependency graph.
		Directory.Move(Path.Combine(_root, "docs"), Path.Combine(_root, "guide"));
		File.WriteAllText(Path.Combine(_root, "index.md"), "---\ntitle: Home\n---\n\nRoot docs.\n");
		File.WriteAllText(Path.Combine(_root, "appsettings.json"), """{"ConnectionStrings":{"Db":"Password=hunter2"}}""");
		File.WriteAllText(Path.Combine(_root, "appsettings.Development.json"), "{}");
		Directory.CreateDirectory(Path.Combine(_root, "bin", "Debug", "net10.0"));
		File.WriteAllText(Path.Combine(_root, "bin", "Debug", "net10.0", "Shop.deps.json"), "{}");
		File.WriteAllText(Path.Combine(_root, "bin", "Debug", "net10.0", "Shop.runtimeconfig.json"), "{}");
		Directory.CreateDirectory(Path.Combine(_root, "obj"));
		File.WriteAllText(Path.Combine(_root, "obj", "project.assets.json"), "{}");
		Directory.CreateDirectory(Path.Combine(_root, "Properties"));
		File.WriteAllText(Path.Combine(_root, "Properties", "launchSettings.json"), "{}");
		Directory.CreateDirectory(Path.Combine(_root, ".vs"));
		File.WriteAllText(Path.Combine(_root, ".vs", "settings.json"), "{}");
		File.WriteAllText(Path.Combine(_root, "data.json"), """{"public":true}""");

		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = ".";
			options.BuildOnStartup = false;
		});

		(await host.GetTextAsync("/docs")).Should().Contain("Root docs.");
		(await host.GetTextAsync("/docs/data.json")).Should().Contain("public");
		string[] hidden =
		[
			"appsettings.json", "appsettings.Development.json", "bin/Debug/net10.0/Shop.deps.json",
			"bin/Debug/net10.0/Shop.runtimeconfig.json", "obj/project.assets.json",
			"Properties/launchSettings.json", ".vs/settings.json"
		];
		foreach (string path in hidden)
		{
			(await host.GetAsync("/docs/" + path)).StatusCode.Should().Be(HttpStatusCode.NotFound, path);
		}

		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		site.Diagnostics.Where(d => d.Source == "DocsPath").Should().ContainSingle()
			.Which.Message.Should().Contain("content root").And.Contain("appsettings.json").And.Contain("bin/");
		host.Logs.Messages(LogLevel.Warning).Should().Contain(m => m.Contains("Point DocsPath at a folder"));
	}

	[Fact]
	public void CopiedExtensions_MatchWhatDiscoveryPublishes()
	{
		// The host copies only what discovery turns into pages or assets; the lists must not drift.
		static HashSet<string> Read(Type type, string field) =>
			(HashSet<string>)type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

		HashSet<string> discovered = Read(typeof(Moka.Docs.Engine.Discovery.FileDiscoveryService), "_assetExtensions");
		HashSet<string> copied = Read(typeof(InMemoryBuildOrchestrator), "_copiedExtensions");

		copied.Should().BeEquivalentTo(discovered.Append(".md"));
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

	/// <summary>Holds one build, the second (the first rebuild) by default, until the test releases it.</summary>
	private sealed class GatePlugin(int heldBuild = 2) : IMokaPlugin
	{
		public const string PluginId = "gate";
		private int _builds;

		public TaskCompletionSource RebuildStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Builds => Volatile.Read(ref _builds);
		public bool SawCancellation { get; private set; }
		public string Id => PluginId;
		public string Name => "Gate";
		public string Version => "1.0.0";

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;

		public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			if (Interlocked.Increment(ref _builds) == heldBuild)
			{
				RebuildStarted.SetResult();
				try
				{
					await Release.Task.WaitAsync(ct);
				}
				catch (OperationCanceledException)
				{
					SawCancellation = true;
					throw;
				}
			}
		}
	}

	/// <summary>A build phase that throws while <see cref="Failing" /> is set, as a broken build does.</summary>
	private sealed class FailingPhase : IBuildPhase
	{
		private int _runs;

		public bool Failing { get; set; }
		public int Runs => Volatile.Read(ref _runs);
		public string Name => "Failing";
		public int Order => 50;

		public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
		{
			Interlocked.Increment(ref _runs);
			return Failing ? throw new InvalidOperationException("The phase failed.") : Task.CompletedTask;
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
