using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     <c>plugins[].path</c> loads a plugin assembly into the CLI. The path used to be read and
///     ignored, so a plugin of your own could only run in a host you wrote yourself.
/// </summary>
public sealed class ExternalPluginTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-extplugin-" + Guid.NewGuid().ToString("N"));

	public ExternalPluginTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nHome.\n");
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
	public async Task FromDll_RunsThePluginWithItsOptionsAndConstructorServices()
	{
		string dll = PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));
		File.Exists(Path.ChangeExtension(dll, ".deps.json")).Should().BeFalse("a dll without a deps.json loads too");

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-probe", "./plugin/Probe.Plugin.dll", "hello"));

		outcome.Failure.Should().BeNull();
		outcome.PluginLoadFailures.Should().BeEmpty();
		outcome.LoadedPluginIds.Should().Equal("mokadocs-probe");
		Page(outcome, "/probe").Should().Contain("<p>hello|</p>");
		outcome.Context.Diagnostics.HasErrors.Should().BeFalse();
	}

	[Fact]
	public async Task FromDll_RealBuild_WritesThePluginsPage()
	{
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));
		string yaml = WriteYaml("""
		                        plugins:
		                          - name: MokaDocs-Probe
		                            path: plugin/Probe.Plugin.dll
		                            options:
		                              text: from the yaml
		                        """);

		int exitCode = await BuildCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct);

		exitCode.Should().Be(0);
		File.ReadAllText(Path.Combine(_root, "_site", "probe", "index.html")).Should().Contain("<p>from the yaml|</p>");
	}

	[Fact]
	public async Task FromFolder_PicksTheAssemblyWithADepsFile()
	{
		string folder = Path.Combine(_root, "plugin");
		string helper = PluginAssemblyBuilder.Compile(folder, "Probe.Helper",
			"namespace Helper; public static class Text { public static string Value => \"from the helper\"; }");
		PluginAssemblyBuilder.Compile(folder, "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe", extraBody: "Helper.Text.Value"),
			references: [helper]);
		File.WriteAllText(Path.Combine(folder, "Probe.Plugin.deps.json"),
			DepsJson("Probe.Plugin", ("Probe.Helper", "1.0.0")));

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-probe", "./plugin"));

		outcome.PluginLoadFailures.Should().BeEmpty();
		Page(outcome, "/probe").Should().Contain("<p>default|from the helper</p>");
	}

	[Fact]
	public async Task DependencyOfAnotherVersionThanTheHosts_IsLoadedFromThePluginsFolder()
	{
		// The host runs on YamlDotNet; this plugin ships an assembly of that name at version 99 with
		// a type the host's copy doesn't have. Sharing the host's copy would fail to find it.
		string folder = Path.Combine(_root, "plugin");
		string fake = PluginAssemblyBuilder.Compile(folder, "YamlDotNet",
			"namespace Isolation; public static class Marker { public static string Value => \"plugin copy\"; }",
			version: "99.0.0.0");
		PluginAssemblyBuilder.Compile(folder, "Isolated.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-isolated", "isolated",
				extraBody: "Isolation.Marker.Value + \" \" + typeof(Isolation.Marker).Assembly.GetName().Version"),
			references: [fake], leaveOut: ["YamlDotNet"]);
		File.WriteAllText(Path.Combine(folder, "Isolated.Plugin.deps.json"),
			DepsJson("Isolated.Plugin", ("YamlDotNet", "99.0.0")));

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-isolated", "./plugin/Isolated.Plugin.dll"));

		outcome.PluginLoadFailures.Should().BeEmpty();
		Page(outcome, "/isolated").Should().Contain("plugin copy 99.0.0.0");
		typeof(YamlDotNet.Serialization.IDeserializer).Assembly.GetName().Version!.Major.Should().BeLessThan(99,
			"the host keeps its own YamlDotNet");
	}

	[Fact]
	public async Task PluginFolderWithItsOwnMokaDocsAssemblies_UsesTheHostsTypes()
	{
		// Without ExcludeAssets="runtime" a plugin's output carries Moka.Docs.*.dll. Loading those
		// copies would give the plugin an IMokaPlugin the host can't call.
		string folder = Path.Combine(_root, "plugin");
		PluginAssemblyBuilder.Compile(folder, "Probe.Plugin", PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));
		foreach (string name in (string[])["Moka.Docs.Plugins.dll", "Moka.Docs.Core.dll"])
		{
			File.Copy(Path.Combine(AppContext.BaseDirectory, name), Path.Combine(folder, name));
		}

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-probe", "./plugin/Probe.Plugin.dll"));

		outcome.PluginLoadFailures.Should().BeEmpty();
		Page(outcome, "/probe").Should().Contain("<p>default|</p>");
	}

	[Fact]
	public async Task DependencyBuiltAgainstANewerMokaDocs_NeverLoadsTheCopyInThePluginsFolder()
	{
		// The plugin references this mokadocs, but a library it uses was built against a newer
		// Moka.Docs.Core whose copy sits in the folder. That copy would give the plugin a second set
		// of MokaDocs types, so the host's is used and the call fails instead.
		Version host = typeof(IMokaPlugin).Assembly.GetName().Version!;
		string folder = Path.Combine(_root, "plugin");
		Directory.CreateDirectory(folder);
		string newerCore = PluginAssemblyBuilder.Compile(Path.Combine(_root, "newer"), "Moka.Docs.Core",
			"namespace Moka.Docs.Core; public static class OnlyInTheCopy { public static string Value => \"copy\"; }",
			version: $"{host.Major}.999.0.0");
		File.Copy(newerCore, Path.Combine(folder, "Moka.Docs.Core.dll"));
		string helper = PluginAssemblyBuilder.Compile(folder, "Probe.Helper",
			"namespace Helper; public static class Text { public static string Value => Moka.Docs.Core.OnlyInTheCopy.Value; }",
			references: [newerCore], leaveOut: ["Moka.Docs.Core"]);
		PluginAssemblyBuilder.Compile(folder, "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe", extraBody: "Helper.Text.Value"),
			references: [helper]);

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-probe", "./plugin/Probe.Plugin.dll"));

		outcome.PluginLoadFailures.Should().BeEmpty();
		outcome.Context.Pages.Should().NotContain(p => p.Route == "/probe");
		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
			.Which.Message.Should().StartWith("Plugin 'mokadocs-probe' failed:");
		// Compared with the host's own Moka.Docs assemblies: plugins loaded by tests running at the
		// same time, such as the `new plugin` project Moka.Docs.Plugins.HelloWorld, share the prefix.
		HashSet<string?> hostAssemblies = AssemblyLoadContext.Default.Assemblies
			.Select(assembly => assembly.GetName().Name)
			.Where(name => name!.StartsWith("Moka.Docs.", StringComparison.Ordinal))
			.ToHashSet();
		AssemblyLoadContext.All
			.Where(context => context.Name?.StartsWith("mokadocs plugin", StringComparison.Ordinal) == true)
			.SelectMany(context => context.Assemblies)
			.Select(assembly => assembly.GetName().Name)
			.Should().NotContain(name => hostAssemblies.Contains(name));
	}

	[Fact]
	public async Task IdThatTheAssemblyDoesNotHave_IsAnErrorListingTheIdsItHas()
	{
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-other", "./plugin/Probe.Plugin.dll"));

		ErrorFor(outcome).Should().Be(
			"Plugin 'mokadocs-other' from './plugin/Probe.Plugin.dll': it has no plugin with the id 'mokadocs-other', only 'mokadocs-probe'");
		outcome.LoadedPluginIds.Should().BeEmpty();
	}

	[Fact]
	public async Task MissingPath_IsAnErrorNamingThePluginAndPath()
	{
		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-gone", "./plugins/Gone.dll"));

		ErrorFor(outcome).Should().StartWith("Plugin 'mokadocs-gone' from './plugins/Gone.dll': '")
			.And.EndWith("Gone.dll' doesn't exist");
	}

	[Fact]
	public async Task MissingPath_FailsTheBuild()
	{
		string yaml = WriteYaml("plugins:\n  - name: mokadocs-gone\n    path: ./plugins/Gone.dll\n");

		int exitCode = await BuildCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct);

		exitCode.Should().Be(1, "the plugin's pages are missing from the site");
	}

	[Theory]
	[InlineData("2.0.0.0", "it is built against Moka.Docs.Plugins 2.0.0, and this mokadocs is {host}. Rebuild it against Moka.Docs.Plugins {major}.x")]
	[InlineData("0.9.0.0", "it is built against Moka.Docs.Plugins 0.9.0, and this mokadocs is {host}. Rebuild it against Moka.Docs.Plugins {major}.x")]
	[InlineData("{major}.999.0.0", "it needs Moka.Docs.Plugins {major}.999.0 or later, and this mokadocs is {host}. Update mokadocs")]
	public async Task PluginBuiltAgainstAnIncompatibleMokaDocs_IsAnError(string builtAgainst, string reason)
	{
		Version host = typeof(IMokaPlugin).Assembly.GetName().Version!;
		string Fill(string text) => text.Replace("{major}", host.Major.ToString()).Replace("{host}", host.ToString(3));

		string contracts = PluginAssemblyBuilder.Compile(Path.Combine(_root, "contracts"), "Moka.Docs.Plugins",
			"namespace Moka.Docs.Plugins; public interface IMokaPlugin { string Id { get; } }", version: Fill(builtAgainst));
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Old.Plugin",
			"public sealed class OldPlugin : Moka.Docs.Plugins.IMokaPlugin { public string Id => \"mokadocs-old\"; }",
			references: [contracts], leaveOut: ["Moka.Docs.Plugins"]);

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-old", "./plugin/Old.Plugin.dll"));

		ErrorFor(outcome).Should().Be($"Plugin 'mokadocs-old' from './plugin/Old.Plugin.dll': {Fill(reason)}");
	}

	[Fact]
	public async Task PluginOfAnotherIMokaPlugin_IsATypeMismatchError()
	{
		// An interface with the same name from another assembly is a different type to the runtime.
		string folder = Path.Combine(_root, "plugin");
		string contracts = PluginAssemblyBuilder.Compile(folder, "Lookalike.Contracts",
			"namespace Moka.Docs.Plugins; public interface IMokaPlugin { string Id { get; } }");
		PluginAssemblyBuilder.Compile(folder, "Lookalike.Plugin",
			"public sealed class LookalikePlugin : Moka.Docs.Plugins.IMokaPlugin { public string Id => \"mokadocs-lookalike\"; }",
			references: [contracts], leaveOut: ["Moka.Docs.Plugins"]);

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-lookalike", "./plugin/Lookalike.Plugin.dll"));

		ErrorFor(outcome).Should().Be(
			"Plugin 'mokadocs-lookalike' from './plugin/Lookalike.Plugin.dll': LookalikePlugin implements the IMokaPlugin of " +
			"'Lookalike.Contracts', not the one in Moka.Docs.Plugins that mokadocs runs, so it can't be used. " +
			"Reference the Moka.Docs.Plugins package");
	}

	[Fact]
	public async Task AssemblyWithoutAPlugin_IsAnError()
	{
		string dll = PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Empty", "public static class Nothing { }");

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-empty", "./plugin/Empty.dll"));

		ErrorFor(outcome).Should().Be($"Plugin 'mokadocs-empty' from './plugin/Empty.dll': '{dll}' has no class that implements IMokaPlugin");
	}

	[Fact]
	public async Task PluginWhoseConstructorThrows_IsAnErrorWithTheReason()
	{
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Broken.Plugin",
			"""
			using System;
			using System.Threading;
			using System.Threading.Tasks;
			using Moka.Docs.Core.Pipeline;
			using Moka.Docs.Plugins;

			public sealed class BrokenPlugin : IMokaPlugin
			{
			    public BrokenPlugin() => throw new InvalidOperationException("no config file");
			    public string Id => "mokadocs-broken";
			    public string Name => "Broken";
			    public string Version => "1.0.0";
			    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;
			    public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default) => Task.CompletedTask;
			}
			""");

		DryRunOutcome outcome = await DryRunAsync(Declare("mokadocs-broken", "./plugin/Broken.Plugin.dll"));

		ErrorFor(outcome).Should().Be(
			"Plugin 'mokadocs-broken' from './plugin/Broken.Plugin.dll': couldn't create BrokenPlugin: no config file");
	}

	[Fact]
	public async Task HostThatDoesNotLoadPaths_ReportsTheDeclarationInsteadOfIgnoringIt()
	{
		// The ASP.NET Core host registers plugins in DI; a custom host has to opt in with PluginBaseDirectory.
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "T" },
			Plugins = [new PluginDeclaration { Name = "mokadocs-probe", Path = "./Probe.dll" }]
		};
		ServiceProvider provider = new ServiceCollection()
			.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
			.BuildServiceProvider();
		var host = new PluginHost(config, provider, NullLogger<PluginHost>.Instance);

		await host.DiscoverAndInitializeAsync(Ct);

		host.LoadFailures.Should().ContainSingle().Which.Message.Should().Be(
			"Plugin 'mokadocs-probe' from './Probe.dll': this host doesn't load plugins from a path. Register the plugin as an IMokaPlugin service");
	}

	[Fact]
	public async Task SameAssemblyForTwoDeclarations_IsLoadedOnceAndSharesThePlugin()
	{
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));
		SiteConfig config = Config(
			new PluginDeclaration { Name = "mokadocs-probe", Path = "./plugin/Probe.Plugin.dll" },
			new PluginDeclaration { Name = "mokadocs-probe", Path = "plugin/../plugin/Probe.Plugin.dll" });
		await using ServiceProvider provider = BuildCommand.BuildServices(config, false, _root);
		PluginHost host = provider.GetRequiredService<PluginHost>();

		await host.DiscoverAndInitializeAsync(Ct);

		host.LoadedPlugins.Should().HaveCount(2);
		host.LoadedPlugins[0].Plugin.Should().BeSameAs(host.LoadedPlugins[1].Plugin);
	}

	[Fact]
	public async Task Validate_ReportsTheLoadFailureOnceAsAnErrorAndExitsWithTwo()
	{
		string yaml = WriteYaml("plugins:\n  - name: mokadocs-gone\n    path: ./plugins/Gone.dll\n  - name: mokadocs-unknown\n");
		SiteConfig config = BuildSession.Read(new ConfigSource(yaml)).Config;
		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false, Ct);

		List<Diagnostic> problems = ValidateCommand.CollectProblems(config, outcome);
		int exitCode = await ValidateCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct);

		problems.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message).Should().ContainSingle()
			.Which.Should().StartWith("Plugin 'mokadocs-gone' from './plugins/Gone.dll': ");
		problems.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.Message)
			.Should().Equal("Plugin 'mokadocs-unknown' is declared but no plugin has that id");
		exitCode.Should().Be(2);
	}

	[Fact]
	public async Task ValidateAndDoctor_PassAPluginThatLoadsFromItsPath()
	{
		PluginAssemblyBuilder.Compile(Path.Combine(_root, "plugin"), "Probe.Plugin",
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));
		string yaml = WriteYaml("plugins:\n  - name: mokadocs-probe\n    path: ./plugin/Probe.Plugin.dll\n");
		SiteConfig config = BuildSession.Read(new ConfigSource(yaml)).Config;
		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false, Ct);

		ValidateCommand.CollectProblems(config, outcome).Should().BeEmpty();
		(await ValidateCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct)).Should().Be(0);
	}

	[Fact]
	public async Task Doctor_FailsOnAPluginThatDoesNotLoad()
	{
		string yaml = WriteYaml("plugins:\n  - name: mokadocs-gone\n    path: ./plugins/Gone.dll\n");

		int exitCode = await DoctorCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct);

		exitCode.Should().Be(2);
	}

	internal static string DepsJson(string plugin, params (string Name, string Version)[] dependencies)
	{
		string tfm = $".NETCoreApp,Version=v{Environment.Version.Major}.0";
		string dependencyList = string.Join(", ", dependencies.Select(d => $"\"{d.Name}\": \"{d.Version}\""));
		string targets = string.Join(",\n", dependencies.Select(d =>
			$"      \"{d.Name}/{d.Version}\": {{ \"runtime\": {{ \"{d.Name}.dll\": {{}} }} }}"));
		string libraries = string.Join(",\n", dependencies.Select(d =>
			$"    \"{d.Name}/{d.Version}\": {{ \"type\": \"project\", \"serviceable\": false, \"sha512\": \"\" }}"));
		return $$"""
		         {
		           "runtimeTarget": { "name": "{{tfm}}", "signature": "" },
		           "compilationOptions": {},
		           "targets": {
		             "{{tfm}}": {
		               "{{plugin}}/1.0.0": { "dependencies": { {{dependencyList}} }, "runtime": { "{{plugin}}.dll": {} } },
		         {{targets}}
		             }
		           },
		           "libraries": {
		             "{{plugin}}/1.0.0": { "type": "project", "serviceable": false, "sha512": "" },
		         {{libraries}}
		           }
		         }
		         """;
	}

	private static PluginDeclaration Declare(string name, string path, string? text = null) => new()
	{
		Name = name,
		Path = path,
		Options = text is null ? [] : new Dictionary<string, object> { ["text"] = text }
	};

	private SiteConfig Config(params PluginDeclaration[] plugins) => new()
	{
		Site = new SiteMetadata { Title = "External" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = "./_site", Cache = false },
		Plugins = [.. plugins]
	};

	private Task<DryRunOutcome> DryRunAsync(params PluginDeclaration[] plugins) =>
		DryRunBuild.RunAsync(Config(plugins), _root, false, false, Ct);

	private string WriteYaml(string plugins)
	{
		string yaml = Path.Combine(_root, "mokadocs.yaml");
		File.WriteAllText(yaml, "site:\n  title: External\ncontent:\n  docs: ./docs\nbuild:\n  output: ./_site\n  cache: false\n" + plugins);
		return yaml;
	}

	private static string Page(DryRunOutcome outcome, string route) =>
		outcome.Context.Pages.Should().ContainSingle(p => p.Route == route).Which.Content.Html;

	private static string ErrorFor(DryRunOutcome outcome)
	{
		outcome.Failure.Should().BeNull();
		Diagnostic error = outcome.Context.Diagnostics.All.Should()
			.ContainSingle(d => d.Severity == DiagnosticSeverity.Error && d.Source == "Plugins").Which;
		outcome.PluginLoadFailures.Should().ContainSingle().Which.Message.Should().Be(error.Message);
		return error.Message;
	}
}
