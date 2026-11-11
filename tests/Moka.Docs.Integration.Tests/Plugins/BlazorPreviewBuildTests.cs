using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Plugins.BlazorPreview;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     The Blazor preview plugin's rendering and publishing must end on their own. Rendering tests
///     use a published host faked on disk, as <see cref="BlazorPreviewScaffoldTests" /> does.
///     Publishing tests run the real <c>dotnet publish</c> on a project without the SDK whose
///     Publish target only writes files or hangs, so nothing is downloaded.
/// </summary>
public sealed class BlazorPreviewBuildTests : IDisposable
{
	private const string _pluginId = "mokadocs-blazor-preview";
	private static readonly TimeSpan _hangGuard = TimeSpan.FromSeconds(90);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-preview-" + Guid.NewGuid().ToString("N"));

	public BlazorPreviewBuildTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
	}

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
	public async Task ComponentThatNeverSettles_IsRenderedWithWhatItHasAndTheBuildFinishes()
	{
		// A timer that calls StateHasChanged forever never reaches quiescence, and the build,
		// and the ASP.NET Core host's build lock with it, waited for it forever.
		WriteBlock("""
			<p>Tick @_n</p>
			@code {
			    int _n;
			    protected override async Task OnInitializedAsync()
			    {
			        while (true) { await Task.Delay(100); _n++; StateHasChanged(); }
			    }
			}
			""");
		FakePublishedHost();

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["renderTimeout"] = "1" })
			.WaitAsync(_hangGuard, TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		string html = outcome.Context.Pages.Single().Content.Html;
		html.Should().Contain("class=\"blazor-preview-iframe\"");
		html.Should().MatchRegex("<noscript><div class=\"blazor-preview-ssr-fallback\"><p>Tick [0-9]+</p>");
		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Source == _pluginId)
			.Which.Message.Should().Contain("still rendering after 1 seconds").And.Contain("renderTimeout");
	}

	[Fact]
	public async Task ComponentThatBlocksWhileInitializing_DoesNotHangTheBuild()
	{
		WriteBlock("""
			<p>Never</p>
			@code {
			    protected override void OnInitialized() => System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
			}
			""");
		FakePublishedHost();

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["renderTimeout"] = "1" })
			.WaitAsync(_hangGuard, TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.Pages.Single().Content.Html
			.Should().Contain("class=\"blazor-preview-iframe\"")
			.And.Contain("<noscript><div class=\"blazor-preview-ssr-fallback\"></div></noscript>");
		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Source == _pluginId)
			.Which.Message.Should().Contain("did not render within 1 seconds");
	}

	[Fact]
	public async Task CancellingTheBuild_StopsWaitingForAPreviewThatNeverSettles()
	{
		string started = Path.Combine(_root, "started.txt");
		WriteBlock($$"""
			<p>Tick @_n</p>
			@code {
			    int _n;
			    protected override async Task OnInitializedAsync()
			    {
			        System.IO.File.WriteAllText(@"{{started}}", "");
			        while (true) { await Task.Delay(100); _n++; StateHasChanged(); }
			    }
			}
			""");
		FakePublishedHost();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		Task<DryRunOutcome> build = BuildAsync(new Dictionary<string, object> { ["renderTimeout"] = "600" }, cts.Token);
		await WaitForFileAsync(started);
		await cts.CancelAsync();
		Func<Task> act = () => build.WaitAsync(_hangGuard, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task PublishThatOutlivesPublishTimeout_IsStoppedWithAnError()
	{
		// dotnet publish ran with no timeout and no cancellation, so a publish that hung hung the build.
		WriteBlock("<h3>Hi</h3>");
		string host = WriteFakeHostProject("""
			<Target Name="Publish">
			  <Exec Condition="'$(OS)' == 'Windows_NT'" Command="ping -n 300 127.0.0.1 &gt; nul" />
			  <Exec Condition="'$(OS)' != 'Windows_NT'" Command="sleep 300" />
			</Target>
			""");
		AddReferenceAssembly(host);

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["publishTimeout"] = "5" })
			.WaitAsync(_hangGuard, TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Should().Contain(d =>
			d.Source == _pluginId && d.Severity == DiagnosticSeverity.Error
			                      && d.Message.Contains("did not finish within 5 seconds", StringComparison.Ordinal)
			                      && d.Message.Contains("publishTimeout", StringComparison.Ordinal));
	}

	[Fact]
	public async Task PublishedHost_IsPublishedAgainOnlyWhenItOrALibraryItReferencesChanges()
	{
		// Publish copies blazor.webassembly.js with its time from the NuGet package, so comparing
		// against it republished every host in every new process. A library the host references
		// with ProjectReference wasn't looked at, so changing it never republished.
		WriteBlock("<h3>Hi</h3>");
		string log = Path.Combine(_root, "publish-log.txt");
		string host = WriteFakeHostProject($$"""
			<ItemGroup>
			  <ProjectReference Include="..\lib\Lib.csproj" />
			</ItemGroup>
			<Target Name="Publish">
			  <PropertyGroup>
			    <Out>$([MSBuild]::EnsureTrailingSlash('$(PublishDir)'))</Out>
			  </PropertyGroup>
			  <MakeDir Directories="$(Out)wwwroot/_framework" />
			  <Touch Files="$(Out)wwwroot/_framework/blazor.webassembly.js" AlwaysCreate="true" Time="2020-01-01" />
			  <WriteLinesToFile File="{{log}}" Lines="published" />
			</Target>
			""");
		AddReferenceAssembly(host);
		string library = Path.Combine(_root, "lib");
		Directory.CreateDirectory(library);
		File.WriteAllText(Path.Combine(library, "Lib.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Razor\" />");
		string widget = Path.Combine(library, "Widget.razor");
		File.WriteAllText(widget, "<p>Widget</p>");
		SetTreeTime(_root, DateTime.UtcNow.AddDays(-2));

		await BuildAndExpectNoPluginDiagnosticsAsync();
		await BuildAndExpectNoPluginDiagnosticsAsync();
		File.ReadAllLines(log).Should().HaveCount(1, "nothing changed after the first publish");

		File.SetLastWriteTimeUtc(widget, DateTime.UtcNow.AddMinutes(1));
		await BuildAndExpectNoPluginDiagnosticsAsync();
		File.ReadAllLines(log).Should().HaveCount(2, "the referenced library changed");
	}

	[Theory]
	[InlineData("dotnet", "8.0.11")]
	[InlineData("snap-dotnet-sdk-current", "9.0.1")]
	[InlineData("homebrew-Cellar-dotnet-9.0.1-libexec", "9.0.1")]
	public void SharedFramework_IsFoundFromWhereTheRuntimeRunsWhateverTheInstallFolderIsCalled(string installFolder,
		string version)
	{
		// Only paths containing /dotnet/shared/ or /.dotnet/shared/ counted, so on snap, Homebrew
		// or a custom root the framework assemblies in the preview host's bin were added again
		// and the compile failed with CS0433.
		string dotnetRoot = Path.Combine(Path.GetTempPath(), "fake-install", installFolder.Replace('-', Path.DirectorySeparatorChar));
		string shared = Path.Combine(dotnetRoot, "shared");
		string coreLib = Path.Combine(shared, "Microsoft.NETCore.App", version, "System.Private.CoreLib.dll");
		string components = Path.Combine(shared, "Microsoft.AspNetCore.App", version, "Microsoft.AspNetCore.Components.dll");
		string appLibrary = Path.Combine(dotnetRoot, "tools", "MyApp", "MyLibrary.dll");

		string? root = BlazorPreviewPlugin.SharedFrameworkRoot(coreLib);

		root.Should().Be(shared);
		BlazorPreviewPlugin.IsInSharedFramework(coreLib, root!).Should().BeTrue();
		BlazorPreviewPlugin.IsInSharedFramework(components, root!).Should().BeTrue();
		BlazorPreviewPlugin.IsInSharedFramework(appLibrary, root!).Should().BeFalse();
		BlazorPreviewPlugin.IsInSharedFramework(shared + "-other" + Path.DirectorySeparatorChar + "x.dll", root!).Should().BeFalse();
	}

	[Theory]
	[InlineData("System.Runtime", true)]
	[InlineData("System.Collections", true)]
	[InlineData("Microsoft.AspNetCore.Components", true)]
	[InlineData("Microsoft.JSInterop", true)]
	[InlineData("Moka.Docs.Core", false)]
	[InlineData("FluentAssertions", false)]
	[InlineData("Not.A.Loaded.Assembly", false)]
	public void FrameworkAssemblyNames_AreTheOnesTheRuntimeLoadsFromASharedFramework(string name, bool expected)
	{
		BlazorPreviewPlugin.IsFrameworkAssemblyName(name).Should().Be(expected);
	}

	private async Task BuildAndExpectNoPluginDiagnosticsAsync()
	{
		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object>())
			.WaitAsync(_hangGuard, TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Where(d => d.Source == _pluginId).Should().BeEmpty();
		outcome.Context.Pages.Single().Content.Html.Should().Contain("class=\"blazor-preview-iframe\"");
	}

	private void WriteBlock(string block) =>
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\n```blazor-preview\n" + block + "\n```\n");

	private void FakePublishedHost()
	{
		string host = Path.Combine(_root, ".mokadocs", "preview-host");
		string framework = Path.Combine(host, "publish-output", "net10.0", "wwwroot", "_framework");
		Directory.CreateDirectory(framework);
		string marker = Path.Combine(framework, "blazor.webassembly.js");
		File.WriteAllText(marker, "");
		File.SetLastWriteTimeUtc(marker, DateTime.UtcNow.AddDays(1));
		AddReferenceAssembly(host);
	}

	private string WriteFakeHostProject(string body)
	{
		string host = Path.Combine(_root, "preview-host");
		Directory.CreateDirectory(host);
		File.WriteAllText(Path.Combine(host, "DocsPreviewHost.csproj"),
			"<Project>\n<!-- Stands in for a Microsoft.NET.Sdk.BlazorWebAssembly project. -->\n" +
			"<Target Name=\"Restore\" />\n" + body + "\n</Project>\n");
		return host;
	}

	private static void AddReferenceAssembly(string host)
	{
		// Roslyn takes its references from the host's bin folder, which needs at least one dll.
		string bin = Path.Combine(host, "bin", "Release", "net10.0");
		Directory.CreateDirectory(bin);
		File.Copy(typeof(SiteConfig).Assembly.Location, Path.Combine(bin, "Moka.Docs.Core.dll"), true);
	}

	private static void SetTreeTime(string directory, DateTime time)
	{
		foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
		{
			File.SetLastWriteTimeUtc(file, time);
		}
	}

	private static async Task WaitForFileAsync(string path)
	{
		using var timeout = new CancellationTokenSource(_hangGuard);
		while (!File.Exists(path))
		{
			await Task.Delay(50, timeout.Token);
		}
	}

	// On the thread pool, so a build that blocks a thread can't block the test's guard as well.
	private Task<DryRunOutcome> BuildAsync(Dictionary<string, object> options, CancellationToken ct = default) =>
		Task.Run(() => DryRunBuild.RunAsync(new SiteConfig
		{
			Site = new SiteMetadata { Title = "Previews" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false },
			Plugins = [new PluginDeclaration { Name = _pluginId, Options = options }]
		}, _root, false, false, ct == default ? TestContext.Current.CancellationToken : ct), CancellationToken.None);
}
