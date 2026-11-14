using System.IO.Abstractions;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Themes;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     <c>build --watch</c> and <c>serve</c> reload mokadocs.yaml. A change used to rebuild with the
///     configuration and the service container from startup, so it took a restart to show up.
/// </summary>
[Collection(EndToEndCollection.Name)]
public sealed class ConfigReloadTests : IDisposable
{
	private static readonly TimeSpan _patience = TimeSpan.FromSeconds(60);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-reload-" + Guid.NewGuid().ToString("N"));

	public ConfigReloadTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nVersion one.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "guide.md"), "---\ntitle: Guide\n---\n\nThe guide.\n");
		WriteYaml("Alphanav", "#111111");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private string Yaml => Path.Combine(_root, "mokadocs.yaml");

	private string Index => Path.Combine(_root, "_site", "index.html");

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
	public async Task Reload_ThemeOptionAndNavEntry_ShowUpInTheNextBuildWithNewServices()
	{
		await using BuildSession session = await StartAsync();
		await BuildAsync(session);
		File.ReadAllText(Index).Should().Contain("Alphanav").And.Contain("--color-primary:#111111");
		ThemeResolver firstResolver = session.Services.GetRequiredService<ThemeResolver>();

		WriteYaml("Betanav", "#ab12cd");
		ConfigReload reload = await session.ReloadIfChangedAsync(true, Ct);
		await BuildAsync(session);

		reload.Outcome.Should().Be(ConfigReloadOutcome.Reloaded);
		reload.Notes.Should().BeEmpty();
		File.ReadAllText(Index).Should().Contain("Betanav").And.Contain("--color-primary:#ab12cd")
			.And.NotContain("Alphanav");
		session.Services.GetRequiredService<ThemeResolver>().Should().NotBeSameAs(firstResolver,
			"the theme and template caches belong to the container and start over with it");
	}

	[Fact]
	public async Task Reload_BrokenYaml_KeepsTheLastGoodConfigurationThenRecovers()
	{
		await using BuildSession session = await StartAsync();
		ServiceProvider services = session.Services;

		File.WriteAllText(Yaml, "site:\n  title: [unclosed\n");
		ConfigReload broken = await session.ReloadIfChangedAsync(true, Ct);

		broken.Outcome.Should().Be(ConfigReloadOutcome.Failed);
		broken.Error.Should().StartWith("Failed to parse configuration");
		session.Services.Should().BeSameAs(services);
		session.Config.Nav.Should().ContainSingle().Which.Label.Should().Be("Alphanav");

		WriteYaml("Gammanav", "#111111");
		ConfigReload fixedYaml = await session.ReloadIfChangedAsync(true, Ct);

		fixedYaml.Outcome.Should().Be(ConfigReloadOutcome.Reloaded);
		session.Config.Nav.Should().ContainSingle().Which.Label.Should().Be("Gammanav");
	}

	[Fact]
	public async Task Reload_SameText_DoesNothing()
	{
		await using BuildSession session = await StartAsync();
		ServiceProvider services = session.Services;
		File.SetLastWriteTimeUtc(Yaml, DateTime.UtcNow.AddMinutes(1));

		(await session.ReloadIfChangedAsync(true, Ct)).Should().BeSameAs(ConfigReload.Unchanged);
		session.Services.Should().BeSameAs(services);
	}

	[Fact]
	public async Task Reload_PluginDeclarations_RebuildThePluginHost()
	{
		await using BuildSession session = await StartAsync();
		session.Plugins.LoadedPlugins.Should().BeEmpty();

		WriteYaml("Alphanav", "#111111", "plugins:\n  - name: mokadocs-changelog\n");
		await session.ReloadIfChangedAsync(true, Ct);

		session.Plugins.LoadedPlugins.Select(p => p.Plugin.Id).Should().Equal("mokadocs-changelog");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Reload_BasePath_IsKeptUnderServeAndAppliedInWatch(bool serve)
	{
		await using BuildSession session = await StartAsync();

		WriteYaml("Alphanav", "#111111", basePath: "/sub");
		ConfigReload reload = await session.ReloadIfChangedAsync(serve, Ct);

		reload.Outcome.Should().Be(ConfigReloadOutcome.Reloaded);
		if (serve)
		{
			session.Config.Build.BasePath.Should().Be("/");
			reload.Notes.Should().Equal("build.basePath changed from '/' to '/sub', which takes a restart. Still using '/'.");
		}
		else
		{
			session.Config.Build.BasePath.Should().Be("/sub");
			reload.Notes.Should().BeEmpty();
		}
	}

	[Fact]
	public async Task Reload_OutputAndDocsFolders_KeepTheirValueAndSaySo()
	{
		await using BuildSession session = await StartAsync();

		WriteYaml("Alphanav", "#111111", folders: "content:\n  docs: ./other\nbuild:\n  output: ./public\n  cache: false\n");
		ConfigReload reload = await session.ReloadIfChangedAsync(false, Ct);

		session.Config.Content.Docs.Should().Be("./docs");
		session.Config.Build.Output.Should().Be("./_site");
		reload.Notes.Should().Equal(
			"content.docs changed from './docs' to './other', which takes a restart. Still using './docs'.",
			"build.output changed from './_site' to './public', which takes a restart. Still using './_site'.");
	}

	[Fact]
	public async Task Reload_CommandLineOverrides_StillWin()
	{
		BuildSession session = await StartAsync(new ConfigSource(Yaml, BasePath: "/cli"));
		await using (session)
		{
			WriteYaml("Alphanav", "#111111", basePath: "/yaml");
			ConfigReload reload = await session.ReloadIfChangedAsync(true, Ct);

			session.Config.Build.BasePath.Should().Be("/cli");
			reload.Notes.Should().BeEmpty();
		}
	}

	[Theory]
	[InlineData("", false, true)]
	[InlineData("", true, false)]
	[InlineData("  minifyAssets: true\n", true, true)]
	[InlineData("  minifyAssets: false\n", false, false)]
	[InlineData("  minify: true\n", true, true)]
	public async Task ThemeAssets_AreReadableUnderServeUnlessTheConfigurationSetsThem(string build, bool serve, bool minified)
	{
		WriteYaml("Alphanav", "#111111", folders: "content:\n  docs: ./docs\nbuild:\n  output: ./_site\n  cache: false\n" + build);
		BuildSession session = await StartAsync(new ConfigSource(Yaml, ReadableAssets: serve));
		await using (session)
		{
			session.Config.Build.ShouldMinifyAssets.Should().Be(minified);

			WriteYaml("Betanav", "#111111", folders: "content:\n  docs: ./docs\nbuild:\n  output: ./_site\n  cache: false\n" + build);
			(await session.ReloadIfChangedAsync(serve, Ct)).Outcome.Should().Be(ConfigReloadOutcome.Reloaded);
			session.Config.Build.ShouldMinifyAssets.Should().Be(minified, "a reload keeps the serve default");
		}
	}

	[Fact]
	public async Task BuildWatch_ReloadsTheConfigAndSkipsBuildsWhileItIsBroken()
	{
		using var stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		Task<int> watching = BuildCommand.Create().Parse(["--watch", "--config", Yaml]).InvokeAsync(cancellationToken: stop.Token);
		try
		{
			await EventuallyAsync(() => Read(Index).Contains("Alphanav"), null);

			await EventuallyAsync(() => Read(Index).Contains("--color-primary:#ab12cd") && Read(Index).Contains("Betanav"),
				() => WriteYaml("Betanav", "#ab12cd"));

			await BreakTheYamlAndEditAPageAsync(() => Read(Index));

			await EventuallyAsync(() => Read(Index).Contains("Gammanav") && Read(Index).Contains("Version two."),
				() => WriteYaml("Gammanav", "#ab12cd"));
		}
		finally
		{
			await stop.CancelAsync();
		}

		(await watching.WaitAsync(_patience, Ct)).Should().Be(0);
	}

	[Fact]
	public async Task Serve_ReloadsTheConfigKeepsItsBasePathAndServesTheLastGoodSiteWhileItIsBroken()
	{
		using var http = new HttpClient();
		using var stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		(Task<int> serving, int port) = await StartServeAsync(stop.Token);
		string Home() => Get(http, $"http://127.0.0.1:{port}/");
		try
		{
			await EventuallyAsync(() => Home().Contains("Alphanav"), null);
			Get(http, $"http://127.0.0.1:{port}/_theme/css/main.css").Should()
				.Contain("/*").And.Contain("\n    ", "serve keeps the theme's CSS readable");
			Get(http, $"http://127.0.0.1:{port}/_theme/js/main.js").Should()
				.Contain("//").And.Contain("\n    ", "and its JS");

			await EventuallyAsync(() => Home().Contains("Betanav") && Home().Contains("--color-primary:#ab12cd"),
				() => WriteYaml("Betanav", "#ab12cd", basePath: "/sub"));
			Home().Should().NotContain("/sub/", "serve keeps the base path it started with");

			await BreakTheYamlAndEditAPageAsync(Home);

			await EventuallyAsync(() => Home().Contains("Gammanav") && Home().Contains("Version two."),
				() => WriteYaml("Gammanav", "#ab12cd"));
		}
		finally
		{
			await stop.CancelAsync();
		}

		(await serving.WaitAsync(_patience, Ct)).Should().Be(0);
	}

	/// <summary>
	///     While mokadocs.yaml doesn't parse, an edited page must not be built: the last good site stays.
	/// </summary>
	private async Task BreakTheYamlAndEditAPageAsync(Func<string> site)
	{
		string before = site();
		File.WriteAllText(Yaml, "site:\n  title: [unclosed\n");
		await Task.Delay(1500, Ct);
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nVersion two.\n");
		await Task.Delay(3000, Ct);

		site().Should().NotContain("Version two.").And.Contain("Betanav");
		site().Should().Be(before);
	}

	private async Task<(Task<int> Serving, int Port)> StartServeAsync(CancellationToken stop)
	{
		// The port is free when picked, but a parallel test can take it before serve binds it.
		for (int attempt = 1; ; attempt++)
		{
			int port = FreePort();
			Task<int> serving = ServeCommand.Create()
				.Parse(["--config", Yaml, "--port", port.ToString(), "--no-open"])
				.InvokeAsync(cancellationToken: stop);

			using var http = new HttpClient();
			DateTime until = DateTime.UtcNow + _patience;
			while (!serving.IsCompleted && DateTime.UtcNow < until)
			{
				if (Get(http, $"http://127.0.0.1:{port}/").Length > 0)
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

	private static string Get(HttpClient http, string url)
	{
		try
		{
			using HttpResponseMessage response = http.GetAsync(url, Ct).GetAwaiter().GetResult();
			return response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync(Ct).GetAwaiter().GetResult() : "";
		}
		catch (HttpRequestException)
		{
			return "";
		}
	}

	private static string Read(string path)
	{
		try
		{
			return File.ReadAllText(path);
		}
		catch (IOException)
		{
			return "";
		}
	}

	/// <summary>
	///     Waits for <paramref name="condition" />, doing <paramref name="change" /> first and again
	///     every few seconds: an edit made before the watcher started is never seen.
	/// </summary>
	private static async Task EventuallyAsync(Func<bool> condition, Action? change)
	{
		DateTime until = DateTime.UtcNow + _patience;
		DateTime nextChange = DateTime.MinValue;
		while (!condition())
		{
			DateTime.UtcNow.Should().BeBefore(until, "the change should show up");
			if (change is not null && DateTime.UtcNow >= nextChange)
			{
				change();
				nextChange = DateTime.UtcNow.AddSeconds(4);
			}

			await Task.Delay(100, Ct);
		}
	}

	private Task<BuildSession> StartAsync(ConfigSource? source = null)
	{
		source ??= new ConfigSource(Yaml);
		(SiteConfig config, string text) = BuildSession.Read(source);
		return BuildSession.StartAsync(source, config, text, false, Ct);
	}

	private async Task BuildAsync(BuildSession session) =>
		await session.Pipeline.ExecuteAsync(new BuildContext
		{
			Config = session.Config,
			FileSystem = new FileSystem(),
			RootDirectory = _root,
			OutputDirectory = Path.Combine(_root, "_site")
		}, Ct);

	private void WriteYaml(string navLabel, string primaryColor, string plugins = "", string basePath = "/",
		string folders = "content:\n  docs: ./docs\nbuild:\n  output: ./_site\n  cache: false\n")
	{
		string yaml = $"""
		               site:
		                 title: Reload
		               theme:
		                 options:
		                   primaryColor: "{primaryColor}"
		               nav:
		                 - label: {navLabel}
		                   path: /guide
		               {folders}  basePath: {basePath}
		               {plugins}
		               """;
		File.WriteAllText(Yaml, yaml);
	}
}
