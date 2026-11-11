using System.Diagnostics;
using System.Security;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Cli;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     <c>plugins[].path</c> naming a project: the CLI runs the real <c>dotnet build</c>, then loads
///     what it built. Each test runs the SDK, so they take seconds; nothing is downloaded.
/// </summary>
[Trait("Category", "Long")]
[Collection(EndToEndCollection.Name)]
public sealed class ExternalPluginProjectTests : IDisposable
{
	private static readonly TimeSpan _guard = TimeSpan.FromMinutes(5);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-extproject-" + Guid.NewGuid().ToString("N"));

	public ExternalPluginProjectTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "site", "docs"));
		File.WriteAllText(Path.Combine(_root, "site", "docs", "index.md"), "---\ntitle: Home\n---\n\nHome.\n");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private string Site => Path.Combine(_root, "site");

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
	public async Task FromProject_BuildsItLoadsItAndRebuildsItWhileTheFirstBuildIsLoaded()
	{
		string project = WriteProject("Probe.Plugin", PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe"));

		DryRunOutcome first = await DryRunAsync("mokadocs-probe", "../Probe.Plugin/Probe.Plugin.csproj", "first");

		NoLoadFailures(first);
		Page(first, "/probe").Should().Contain("<p>first|</p>");

		// The first build's assembly is still loaded. Mapped from disk it stayed locked on Windows,
		// and the second build couldn't overwrite it.
		File.WriteAllText(Path.Combine(Path.GetDirectoryName(project)!, "Plugin.cs"),
			PluginAssemblyBuilder.PagePluginSource("mokadocs-probe", "probe", extraBody: "\"rebuilt\""));
		DryRunOutcome second = await DryRunAsync("mokadocs-probe", "../Probe.Plugin", "second");

		NoLoadFailures(second);
		Page(second, "/probe").Should().Contain("<p>second|rebuilt</p>");
	}

	[Fact]
	public async Task ProjectThatDoesNotCompile_IsAnErrorWithTheCompilerMessage()
	{
		WriteProject("Broken.Plugin", "public sealed class Broken { public void M() { int x = ; } }");

		DryRunOutcome outcome = await DryRunAsync("mokadocs-broken", "../Broken.Plugin/Broken.Plugin.csproj", null);

		outcome.Failure.Should().BeNull();
		string error = outcome.Context.Diagnostics.All.Should()
			.ContainSingle(d => d.Severity == DiagnosticSeverity.Error && d.Source == "Plugins").Which.Message;
		error.Should().StartWith("Plugin 'mokadocs-broken' from '../Broken.Plugin/Broken.Plugin.csproj': dotnet build failed for Broken.Plugin.csproj: ")
			.And.Contain("error CS1525");
	}

	[Fact]
	public async Task NewPlugin_BuildsAndRunsInTheCliOutOfTheBox()
	{
		// The scaffold references the Moka.Docs.Plugins package at the tool's version. A feed packed
		// from this build stands in for nuget.org, and the machine's package cache supplies the rest.
		await WriteLocalFeedAsync();
		int created = await NewCommand.Create().Parse(["plugin", "hello-world", "--path", _root])
			.InvokeAsync(cancellationToken: Ct);
		created.Should().Be(0);
		string yaml = Path.Combine(Site, "mokadocs.yaml");
		File.WriteAllText(yaml, """
		                        site:
		                          title: Scaffold
		                        build:
		                          cache: false
		                        plugins:
		                          - name: mokadocs-hello-world
		                            path: ../Moka.Docs.Plugins.HelloWorld
		                            options:
		                              message: Built by the scaffolded plugin
		                        """);

		DryRunOutcome outcome = await DryRunBuild.RunAsync(BuildSession.Read(new ConfigSource(yaml)).Config, Site,
			false, false, Ct).WaitAsync(_guard, Ct);
		NoLoadFailures(outcome);
		int exitCode = await BuildCommand.Create().Parse(["--config", yaml]).InvokeAsync(cancellationToken: Ct)
			.WaitAsync(_guard, Ct);

		exitCode.Should().Be(0);
		File.ReadAllText(Path.Combine(Site, "_site", "hello-world", "index.html"))
			.Should().Contain("<p>Built by the scaffolded plugin</p>");
	}

	/// <summary>
	///     A project that compiles against this test run's MokaDocs assemblies, for the running
	///     framework, without any package to restore.
	/// </summary>
	private string WriteProject(string name, string source)
	{
		string folder = Path.Combine(_root, name);
		Directory.CreateDirectory(folder);
		// Microsoft.Extensions.Logging.Abstractions comes from the ASP.NET Core shared framework here,
		// not from the test's output folder.
		string references = string.Join(Environment.NewLine,
			new[] { typeof(IMokaPlugin), typeof(SiteConfig), typeof(ILogger<>) }.Select(type => type.Assembly).Select(assembly =>
				$"    <Reference Include=\"{assembly.GetName().Name}\" HintPath=\"{SecurityElement.Escape(assembly.Location)}\" Private=\"false\" />"));
		File.WriteAllText(Path.Combine(folder, name + ".csproj"), $"""
		                                                          <Project Sdk="Microsoft.NET.Sdk">
		                                                            <PropertyGroup>
		                                                              <TargetFramework>net{Environment.Version.Major}.0</TargetFramework>
		                                                              <ImplicitUsings>enable</ImplicitUsings>
		                                                              <Nullable>enable</Nullable>
		                                                              <EnableDynamicLoading>true</EnableDynamicLoading>
		                                                            </PropertyGroup>
		                                                            <ItemGroup>
		                                                          {references}
		                                                            </ItemGroup>
		                                                          </Project>
		                                                          """);
		File.WriteAllText(Path.Combine(folder, "Plugin.cs"), source);
		return Path.Combine(folder, name + ".csproj");
	}

	private Task<DryRunOutcome> DryRunAsync(string name, string path, string? text)
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Projects" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false },
			Plugins =
			[
				new PluginDeclaration
				{
					Name = name,
					Path = path,
					Options = text is null ? [] : new Dictionary<string, object> { ["text"] = text }
				}
			]
		};
		return DryRunBuild.RunAsync(config, Site, false, false, Ct).WaitAsync(_guard, Ct);
	}

	/// <summary>
	///     Packs the MokaDocs libraries the plugin package needs from this build into a feed, and
	///     writes a nuget.config above the scaffolded project that restores from it with no network.
	/// </summary>
	private async Task WriteLocalFeedAsync()
	{
		string repository = FindRepositoryRoot();
		string feed = Path.Combine(_root, "feed");
		List<string> projects = ProjectClosure(Path.Combine(repository, "src", "Moka.Docs.Plugins", "Moka.Docs.Plugins.csproj"));
#if DEBUG
		const string configuration = "Debug";
#else
		const string configuration = "Release";
#endif
		string pack = Path.Combine(_root, "pack.proj");
		File.WriteAllText(pack, $"""
		                         <Project>
		                           <ItemGroup>
		                         {string.Join(Environment.NewLine, projects.Select(p => $"    <PackProject Include=\"{SecurityElement.Escape(p)}\" />"))}
		                           </ItemGroup>
		                           <Target Name="Build">
		                             <MSBuild Projects="@(PackProject)" Targets="Pack" Properties="Configuration={configuration};NoBuild=true;PackageOutputPath={SecurityElement.Escape(feed)}{Path.DirectorySeparatorChar};NuspecOutputPath={SecurityElement.Escape(Path.Combine(_root, "nuspecs"))}{Path.DirectorySeparatorChar}" />
		                           </Target>
		                         </Project>
		                         """);
		ChildProcessResult packed = await RunDotnetAsync(["msbuild", pack, "-nologo", "-nodeReuse:false", "-v:m"]);
		packed.ExitCode.Should().Be(0, packed.Output + packed.Error);

		ChildProcessResult locals = await RunDotnetAsync(["nuget", "locals", "global-packages", "--list"]);
		string cache = locals.Output.Split('\n').Select(line => line.Trim())
			.Single(line => line.StartsWith("global-packages:", StringComparison.Ordinal))["global-packages:".Length..].Trim();

		// A fallback folder wins over the feed, so when the cache already holds a published package of
		// this version, the cache becomes a source instead and source mapping keeps MokaDocs on the feed.
		string version = CliVersion.Current;
		bool shadowed = projects.Any(p =>
			Directory.Exists(Path.Combine(cache, Path.GetFileNameWithoutExtension(p).ToLowerInvariant(), version)));
		string feedXml = SecurityElement.Escape(feed);
		string cacheXml = SecurityElement.Escape(cache);
		string sources = shadowed
			? $"""
			   <packageSources><clear /><add key="feed" value="{feedXml}" /><add key="cache" value="{cacheXml}" /></packageSources>
			   <packageSourceMapping>
			     <packageSource key="feed"><package pattern="Moka.Docs.*" /></packageSource>
			     <packageSource key="cache"><package pattern="*" /></packageSource>
			   </packageSourceMapping>
			   <fallbackPackageFolders><clear /></fallbackPackageFolders>
			   """
			: $"""
			   <packageSources><clear /><add key="feed" value="{feedXml}" /></packageSources>
			   <fallbackPackageFolders><clear /><add key="cache" value="{cacheXml}" /></fallbackPackageFolders>
			   """;
		File.WriteAllText(Path.Combine(_root, "nuget.config"), $"""
		                                                       <?xml version="1.0" encoding="utf-8"?>
		                                                       <configuration>
		                                                         <config><add key="globalPackagesFolder" value="{SecurityElement.Escape(Path.Combine(_root, "packages"))}" /></config>
		                                                       {sources}
		                                                       </configuration>
		                                                       """);
	}

	private async Task<ChildProcessResult> RunDotnetAsync(string[] arguments)
	{
		string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";
		var startInfo = new ProcessStartInfo(dotnet) { WorkingDirectory = _root };
		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		ChildProcessResult result = await ChildProcessRunner.RunAsync(startInfo, _guard, Ct);
		result.TimedOut.Should().BeFalse($"dotnet {string.Join(' ', arguments)} should finish");
		return result;
	}

	private static string FindRepositoryRoot()
	{
		for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "MokaDocs.slnx")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException("The test output is not inside the MokaDocs repository.");
	}

	/// <summary>The project and every project it references, directly or not.</summary>
	private static List<string> ProjectClosure(string project)
	{
		var found = new List<string>();
		var pending = new Queue<string>([Path.GetFullPath(project)]);
		while (pending.TryDequeue(out string? next))
		{
			if (found.Contains(next, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			found.Add(next);
			foreach (XAttribute include in XDocument.Load(next).Descendants("ProjectReference")
				         .Select(reference => reference.Attribute("Include")).OfType<XAttribute>())
			{
				pending.Enqueue(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(next)!,
					include.Value.Replace('\\', Path.DirectorySeparatorChar))));
			}
		}

		return found;
	}

	private static void NoLoadFailures(DryRunOutcome outcome)
	{
		outcome.Failure.Should().BeNull();
		outcome.PluginLoadFailures.Select(f => f.Message).Should().BeEmpty();
		outcome.Context.Diagnostics.All.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message)
			.Should().BeEmpty();
	}

	private static string Page(DryRunOutcome outcome, string route) =>
		outcome.Context.Pages.Should().ContainSingle(p => p.Route == route).Which.Content.Html;
}
