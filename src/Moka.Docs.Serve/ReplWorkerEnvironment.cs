namespace Moka.Docs.Serve;

// A worker inherits the web app's environment. Under dotnet watch (checked with SDK 10.0.404) that
// holds DOTNET_STARTUP_HOOKS with the browser refresh and delta applier agents,
// DOTNET_MODIFIABLE_ASSEMBLIES=debug, DOTNET_WATCH*, ASPNETCORE_AUTO_RELOAD_*,
// __ASPNETCORE_BROWSER_TOOLS and the browser refresh hosting startup. The delta applier deletes its
// own entries from the app's environment when it loads, but the browser refresh hook stayed, so
// every worker loaded it and ran in hot reload mode. An agent that doesn't clean up would also
// connect the worker to the pipe meant for the app. Every startup hook goes: a worker needs none.
internal static class ReplWorkerEnvironment
{
	internal const string StartupHooksVariable = "DOTNET_STARTUP_HOOKS";
	internal const string HostingStartupAssembliesVariable = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";
	internal const string BrowserRefreshAssembly = "Microsoft.AspNetCore.Watch.BrowserRefresh";

	private static readonly string[] _removedNames =
	[
		StartupHooksVariable,
		"DOTNET_MODIFIABLE_ASSEMBLIES",
		// The delta applier's pipe name in older SDKs; current ones use DOTNET_WATCH_HOTRELOAD_NAMEDPIPE_NAME.
		"DOTNET_HOTRELOAD_NAMEDPIPE_NAME",
		"__ASPNETCORE_BROWSER_TOOLS"
	];

	private static readonly string[] _removedPrefixes = ["DOTNET_WATCH", "ASPNETCORE_AUTO_RELOAD_"];

	internal static void RemoveHotReloadVariables(IDictionary<string, string?> environment)
	{
		foreach (string name in environment.Keys.Where(IsHotReloadVariable).ToList())
		{
			environment.Remove(name);
		}

		if (environment.TryGetValue(HostingStartupAssembliesVariable, out string? startups) && startups is not null)
		{
			string[] kept = startups
				.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(name => !name.Equals(BrowserRefreshAssembly, StringComparison.OrdinalIgnoreCase))
				.ToArray();
			if (kept.Length == 0)
			{
				environment.Remove(HostingStartupAssembliesVariable);
			}
			else
			{
				environment[HostingStartupAssembliesVariable] = string.Join(';', kept);
			}
		}
	}

	private static bool IsHotReloadVariable(string name) =>
		_removedNames.Contains(name, StringComparer.OrdinalIgnoreCase)
		|| _removedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
