using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     A REPL worker inherited the environment dotnet watch gives the web app, so it loaded the
///     browser refresh startup hook and ran in hot reload mode.
/// </summary>
public sealed class ReplWorkerEnvironmentTests
{
	private static readonly string[] _hotReloadNames =
	[
		"DOTNET_STARTUP_HOOKS",
		"DOTNET_MODIFIABLE_ASSEMBLIES",
		"DOTNET_WATCH",
		"DOTNET_WATCH_ITERATION",
		"DOTNET_WATCH_HOTRELOAD_NAMEDPIPE_NAME",
		"DOTNET_HOTRELOAD_NAMEDPIPE_NAME",
		"ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT",
		"ASPNETCORE_AUTO_RELOAD_WS_KEY",
		"ASPNETCORE_AUTO_RELOAD_VDIR",
		"__ASPNETCORE_BROWSER_TOOLS",
		"ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"
	];

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public void RemoveHotReloadVariables_DropsWhatDotnetWatchSets_AndKeepsTheRest()
	{
		// The variables dotnet watch (SDK 10.0.404) passed to a web app, plus the app's own.
		var environment = new Dictionary<string, string?>
		{
			["DOTNET_STARTUP_HOOKS"] = string.Join(Path.PathSeparator,
				"/sdk/dotnet-watch/hotreload/net6.0/Microsoft.AspNetCore.Watch.BrowserRefresh.dll",
				"/sdk/dotnet-watch/hotreload/net10.0/Microsoft.Extensions.DotNetDeltaApplier.dll"),
			["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug",
			["DOTNET_WATCH"] = "1",
			["DOTNET_WATCH_ITERATION"] = "3",
			["DOTNET_WATCH_HOTRELOAD_NAMEDPIPE_NAME"] = "2f8a0424b3834825936ad1fd256a8e93",
			["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1",
			["dotnet_watch_agent_websocket_port"] = "5099",
			["DOTNET_HOTRELOAD_NAMEDPIPE_NAME"] = "vs-pipe",
			["ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT"] = "ws://localhost:51636,wss://localhost:51637",
			["ASPNETCORE_AUTO_RELOAD_WS_KEY"] = "key",
			["ASPNETCORE_AUTO_RELOAD_VDIR"] = "/",
			["__ASPNETCORE_BROWSER_TOOLS"] = "true",
			["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Contoso.Telemetry; Microsoft.AspNetCore.Watch.BrowserRefresh",
			["ASPNETCORE_ENVIRONMENT"] = "Development",
			["DOTNET_ROOT"] = "/usr/share/dotnet",
			["DOTNET_HOST_PATH"] = "/usr/share/dotnet/dotnet",
			["DOTNET_LAUNCH_PROFILE"] = "https",
			["MOKADOCS_REPL_WORKER"] = "1",
			["PATH"] = "/usr/bin"
		};

		ReplWorkerEnvironment.RemoveHotReloadVariables(environment);

		environment.Should().BeEquivalentTo(new Dictionary<string, string?>
		{
			["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Contoso.Telemetry",
			["ASPNETCORE_ENVIRONMENT"] = "Development",
			["DOTNET_ROOT"] = "/usr/share/dotnet",
			["DOTNET_HOST_PATH"] = "/usr/share/dotnet/dotnet",
			["DOTNET_LAUNCH_PROFILE"] = "https",
			["MOKADOCS_REPL_WORKER"] = "1",
			["PATH"] = "/usr/bin"
		});
	}

	[Fact]
	public void RemoveHotReloadVariables_BrowserRefreshAloneAsHostingStartup_RemovesTheVariable()
	{
		var environment = new Dictionary<string, string?>
		{
			["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Microsoft.AspNetCore.Watch.BrowserRefresh"
		};

		ReplWorkerEnvironment.RemoveHotReloadVariables(environment);

		environment.Should().BeEmpty();
	}

	[Fact]
	public async Task Worker_StartedFromADotnetWatchEnvironment_RunsWithoutIt()
	{
		// A startup hook that doesn't exist stops the runtime before Main, so a worker that kept
		// DOTNET_STARTUP_HOOKS would never report ready.
		string missingHook = Path.Combine(Path.GetTempPath(), "mokadocs-" + Guid.NewGuid().ToString("N"),
			"Microsoft.Extensions.DotNetDeltaApplier.dll");
		using var service = new ReplExecutionService(NullLogger<ReplExecutionService>.Instance, () =>
		{
			ProcessStartInfo startInfo = CliWorker();
			startInfo.Environment["DOTNET_STARTUP_HOOKS"] = missingHook;
			startInfo.Environment["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
			startInfo.Environment["DOTNET_WATCH"] = "1";
			startInfo.Environment["DOTNET_WATCH_ITERATION"] = "1";
			startInfo.Environment["DOTNET_WATCH_HOTRELOAD_NAMEDPIPE_NAME"] = Guid.NewGuid().ToString("N");
			startInfo.Environment["ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT"] = "ws://localhost:1";
			startInfo.Environment["ASPNETCORE_AUTO_RELOAD_WS_KEY"] = "key";
			startInfo.Environment["ASPNETCORE_AUTO_RELOAD_VDIR"] = "/";
			startInfo.Environment["__ASPNETCORE_BROWSER_TOOLS"] = "true";
			startInfo.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Microsoft.AspNetCore.Watch.BrowserRefresh";
			return startInfo;
		});

		ReplResult result = await service.ExecuteAsync(
			$$"""
			  var names = new[] { {{string.Join(", ", _hotReloadNames.Select(n => $"\"{n}\""))}} };
			  Console.Write(string.Join(",", names.Where(n => Environment.GetEnvironmentVariable(n) is not null)));
			  Console.Write("|" + System.Reflection.Metadata.MetadataUpdater.IsSupported);
			  """, Ct);

		result.Error.Should().BeNullOrEmpty();
		result.Output.Should().Be("|False", "no hot reload variable reaches the worker, and it runs without hot reload");
	}

	private static ProcessStartInfo CliWorker() =>
		ServeCommand.CreateReplWorkerStartInfo(
			Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet",
			Path.Combine(AppContext.BaseDirectory, "mokadocs.dll"),
			Environment.ProcessId);
}
