using System.IO.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Plugins.BlazorPreview;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     The Blazor preview plugin's rendering, staging and publishing. Rendering and staging tests
///     use a published host faked on disk, as <see cref="BlazorPreviewScaffoldTests" /> does.
///     Publishing tests run the real <c>dotnet publish</c> on a project without the SDK whose
///     Publish target only writes files or hangs, so nothing is downloaded.
/// </summary>
public sealed class BlazorPreviewBuildTests : IDisposable
{
	private const string _pluginId = "mokadocs-blazor-preview";
	private const string _fingerprintedScript = "blazor.webassembly.0a1b2c3d4e.js";

	// A page as a host with OverrideHtmlAssetPlaceholders publishes it. The SDK writes the import
	// map with CR LF line endings on Windows.
	private const string _publishedIndex = "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n  <meta charset=\"utf-8\" />\n" +
	                                       "  <base href=\"./\" />\n  <script type=\"importmap\">{\r\n  \"imports\": {}\r\n}</script>\n" +
	                                       "</head>\n<body>\n  <script src=\"_content/Moka.Blazor.Repl.Host/wasmPreview.js\"></script>\n" +
	                                       "  <script src=\"_framework/" + _fingerprintedScript + "\"></script>\n</body>\n</html>\n";

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
		string host = WriteFakeHostProject("""
			<ItemGroup>
			  <ProjectReference Include="..\lib\Lib.csproj" />
			</ItemGroup>
			""" + FakePublishTarget(log));
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

	[Fact]
	public async Task Publish_EmptiesThePublishFolderFirst_SoFilesFromAnEarlierPublishDontShip()
	{
		// dotnet publish only adds and replaces files: an assembly the host stopped using kept
		// shipping, and an unfingerprinted blazor.webassembly.js from before the switch stayed next
		// to the fingerprinted one.
		WriteBlock("<h3>Hi</h3>");
		string log = Path.Combine(_root, "publish-log.txt");
		string host = WriteFakeHostProject(FakePublishTarget(log));
		AddReferenceAssembly(host);
		string framework = Path.Combine(host, "publish-output", "net10.0", "wwwroot", "_framework");
		Directory.CreateDirectory(framework);
		File.WriteAllText(Path.Combine(framework, "Removed.0a1b2c3d4e.wasm"), "old");
		File.WriteAllText(Path.Combine(framework, "blazor.webassembly.js"), "old");
		SetTreeTime(Path.Combine(host, "publish-output"), DateTime.UtcNow.AddDays(-2));

		await BuildAndExpectNoPluginDiagnosticsAsync();

		File.ReadAllLines(log).Should().HaveCount(1);
		Directory.GetFiles(framework).Select(Path.GetFileName).Should().Equal(_fingerprintedScript);
	}

	[Fact]
	public async Task PublishedHost_IsStagedWithItsPagesHardened_AndItsFoldersCopiedAsTheyAre()
	{
		// The published page loaded any ?assembly= URL and ran a 'run' message from any window. A
		// copy of the whole folder would also put the published page back over the hardened one.
		WriteBlock("<h3>Hi</h3>");
		string wwwroot = FakePublishedHost();
		File.WriteAllBytes(Path.Combine(wwwroot, "index.html.br"), [1, 2, 3]);
		File.WriteAllBytes(Path.Combine(wwwroot, "index.html.gz"), [1, 2, 3]);
		File.WriteAllText(Path.Combine(wwwroot, "DocsPreviewHost.styles.css"), "p{}");

		DryRunOutcome outcome = await BuildAsync([]);

		BuildContext context = outcome.Context;
		context.Diagnostics.All.Should().NotContain(d => d.Source == _pluginId);
		byte[] page = context.DeferredOutputFiles["_preview-wasm/index.html"];
		string html = Encoding.UTF8.GetString(page);
		html.Should().Contain("<script data-mokadocs-preview>" + PreviewPageHardening.GuardScript + "</script>");
		Regex.Replace(html,
				"\n<meta http-equiv=\"Content-Security-Policy\" data-mokadocs-preview content=\"[^\"]+\">\n<script data-mokadocs-preview>.*?</script>",
				"", RegexOptions.Singleline)
			.Should().Be(_publishedIndex, "nothing else in the page changes");
		Decompress(new BrotliStream(new MemoryStream(context.DeferredOutputFiles["_preview-wasm/index.html.br"]),
			CompressionMode.Decompress)).Should().Equal(page, "a server that negotiates encodings serves this copy");
		Decompress(new GZipStream(new MemoryStream(context.DeferredOutputFiles["_preview-wasm/index.html.gz"]),
			CompressionMode.Decompress)).Should().Equal(page);
		context.DeferredOutputFiles["_preview-wasm/DocsPreviewHost.styles.css"].Should().Equal("p{}"u8.ToArray());
		context.DeferredOutputDirectories.Should().ContainSingle()
			.Which.Should().Be((Path.Combine(wwwroot, "_framework"), "_preview-wasm/_framework"));
		context.DeferredOutputFiles[".gitattributes"].Should().Equal("*.js binary\n"u8.ToArray());
		File.ReadAllText(Path.Combine(wwwroot, "index.html")).Should().Be(_publishedIndex, "the host's folder is never written to");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ScopedCssLink_IsStagedOnlyWhenThePublishWroteTheBundle(bool bundled)
	{
		// The scaffolded page links DocsPreviewHost.styles.css, which the SDK only writes when there
		// are scoped styles, so every preview of a host without a library requested a missing file.
		WriteBlock("<h3>Hi</h3>");
		string wwwroot = FakePublishedHost();
		const string link = "  <link rel=\"stylesheet\" href=\"DocsPreviewHost.styles.css\" />\n";
		File.WriteAllText(Path.Combine(wwwroot, "index.html"), _publishedIndex.Replace("</head>", link + "</head>", StringComparison.Ordinal));
		if (bundled)
		{
			File.WriteAllText(Path.Combine(wwwroot, "DocsPreviewHost.styles.css"), "p{}");
		}

		DryRunOutcome outcome = await BuildAsync([]);

		string html = Encoding.UTF8.GetString(outcome.Context.DeferredOutputFiles["_preview-wasm/index.html"]);
		html.Contains(link, StringComparison.Ordinal).Should().Be(bundled);
		outcome.Context.DeferredOutputFiles.ContainsKey("_preview-wasm/DocsPreviewHost.styles.css").Should().Be(bundled);
		if (!bundled)
		{
			Regex.Replace(html,
					"\n<meta http-equiv=\"Content-Security-Policy\" data-mokadocs-preview content=\"[^\"]+\">\n<script data-mokadocs-preview>.*?</script>",
					"", RegexOptions.Singleline)
				.Should().Be(_publishedIndex, "only the link line goes");
		}
	}

	[Fact]
	public async Task Build_WritesTheHardenedHostPage_AndTheCopiedFoldersDontReplaceIt()
	{
		WriteBlock("<h3>Hi</h3>");
		FakePublishedHost();

		BuildContext context = await BuildToDiskAsync([], "/docs");

		context.Diagnostics.All.Should().NotContain(d => d.Source == _pluginId);
		string site = Path.Combine(_root, "_site");
		File.ReadAllText(Path.Combine(site, "_preview-wasm", "index.html")).Should().Contain("data-mokadocs-preview");
		File.Exists(Path.Combine(site, "_preview-wasm", "_framework", _fingerprintedScript)).Should().BeTrue();
		File.ReadAllText(Path.Combine(site, ".gitattributes")).Should().Be("*.js binary\n");
		File.Exists(Path.Combine(site, "_preview-assets", "blazor-preview.css")).Should().BeTrue();
		File.Exists(Path.Combine(site, "_preview-assets", "blazor-preview.js")).Should().BeTrue();
		File.ReadAllText(Path.Combine(site, "index.html"))
			.Should().Contain("<link rel=\"stylesheet\" href=\"/docs/_preview-assets/blazor-preview.css?v=")
			.And.Contain("<script src=\"/docs/_preview-assets/blazor-preview.js?v=")
			.And.Contain("src=\"/docs/_preview-wasm/index.html?assembly=/docs/_preview-assemblies/");
	}

	[Fact]
	public async Task PreviewAssembly_IsNamedByTheSha256OfItsBytes_InAnIframeWithoutSandbox()
	{
		// Named by 12 hex characters of the source, the same name could stand for different bytes
		// (another compiler, other references), so the file couldn't be cached for good.
		WriteBlock("<h3>Hi</h3>");
		FakePublishedHost();

		DryRunOutcome outcome = await BuildAsync([]);

		string html = outcome.Context.Pages.Single().Content.Html;
		Match frame = Regex.Match(html,
			"<iframe class=\"blazor-preview-iframe\" src=\"/_preview-wasm/index.html\\?assembly=/_preview-assemblies/([0-9a-f]{64})\\.bin" +
			"&amp;entry=DocsPreview.Preview\" loading=\"lazy\" title=\"Blazor preview\"></iframe>");
		frame.Success.Should().BeTrue(html);
		byte[] assembly = outcome.Context.DeferredOutputFiles[$"_preview-assemblies/{frame.Groups[1].Value}.bin"];
		Convert.ToHexStringLower(SHA256.HashData(assembly)).Should().Be(frame.Groups[1].Value);
		html.Should().NotContain("sandbox");
		outcome.Context.DeferredOutputFiles.Keys.Should().NotContain(key => key.EndsWith(".dll", StringComparison.Ordinal));
	}

	[Fact]
	public async Task PageStylesAndScript_AreVersionedFiles_NotBlocksInEveryPage()
	{
		WriteBlock("<h3>Hi</h3>");
		FakePublishedHost();

		DryRunOutcome outcome = await BuildAsync([]);

		string html = outcome.Context.Pages.Single().Content.Html;
		byte[] css = outcome.Context.DeferredOutputFiles["_preview-assets/blazor-preview.css"];
		byte[] js = outcome.Context.DeferredOutputFiles["_preview-assets/blazor-preview.js"];
		html.Should().Contain($"<link rel=\"stylesheet\" href=\"/_preview-assets/blazor-preview.css?v={ShortHash(css)}\">")
			.And.Contain($"<script src=\"/_preview-assets/blazor-preview.js?v={ShortHash(js)}\" defer></script>")
			.And.NotContain(".blazor-preview-container {").And.NotContain("tabBar");
		Encoding.UTF8.GetString(css).Should().Contain(".blazor-preview-iframe").And.Contain(".blazor-preview-tab.active");
		Encoding.UTF8.GetString(js).Should().Contain("blazor-preview-tabs").And.Contain("e.origin !== location.origin");
	}

	[Theory]
	[InlineData("false")]
	[InlineData(false)]
	public async Task PublishHostOff_WithoutAPreviewHost_ShowsTheStaticRender_AndNeverScaffoldsOrPublishes(object value)
	{
		// An app on the ASP.NET Core host must not scaffold and run dotnet publish in production.
		WriteBlock("<h3>Hi</h3>");

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["publishHost"] = value });

		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Source == _pluginId)
			.Which.Message.Should().Contain("publishHost is off and no previewHost is set");
		outcome.Context.Pages.Single().Content.Html
			.Should().Contain("<div class=\"blazor-preview-render\"><div class=\"blazor-preview-ssr-fallback\"><h3>Hi</h3></div></div>")
			.And.NotContain("<iframe").And.NotContain("<noscript>");
		outcome.Context.DeferredOutputFiles.Keys.Should().NotContain(key =>
			key.StartsWith("_preview-wasm/", StringComparison.Ordinal) ||
			key.StartsWith("_preview-assemblies/", StringComparison.Ordinal) || key == ".gitattributes");
		outcome.Context.DeferredOutputDirectories.Should().BeEmpty();
		Directory.Exists(Path.Combine(_root, ".mokadocs", "preview-host")).Should().BeFalse();
		Directory.Exists(Path.Combine(_root, "preview-host")).Should().BeFalse();
	}

	[Fact]
	public async Task PublishHostOff_WithAPreviewHost_StagesItsNewestPublish_WithoutPublishing()
	{
		WriteBlock("<h3>Hi</h3>");
		string log = Path.Combine(_root, "publish-log.txt");
		string host = WriteFakeHostProject(FakePublishTarget(log));
		AddReferenceAssembly(host);
		// The project is newer than both, so with publishHost on it would be published again.
		WritePublishedHost(Path.Combine(host, "publish-output", "wwwroot"), "blazor.webassembly.js", "<p>older</p>",
			DateTime.UtcNow.AddDays(-3));
		WritePublishedHost(Path.Combine(host, "publish-output", "net10.0", "wwwroot"), _fingerprintedScript, "<p>newer</p>",
			DateTime.UtcNow.AddDays(-2));

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object>
		{
			["publishHost"] = "false",
			["previewHost"] = "preview-host"
		});

		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Source == _pluginId);
		File.Exists(log).Should().BeFalse("publishHost off never runs dotnet publish");
		Encoding.UTF8.GetString(outcome.Context.DeferredOutputFiles["_preview-wasm/index.html"]).Should().EndWith("<p>newer</p>");
		outcome.Context.Pages.Single().Content.Html.Should().Contain("class=\"blazor-preview-iframe\"");
	}

	[Fact]
	public async Task PublishHostThatIsNotTrueOrFalse_IsReported_AndPublishingStaysOn()
	{
		WriteBlock("<h3>Hi</h3>");
		FakePublishedHost();

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["publishHost"] = "sometimes" });

		outcome.Context.Diagnostics.All.Should().ContainSingle(d => d.Source == _pluginId)
			.Which.Message.Should().Contain("publishHost 'sometimes' is not true or false");
		outcome.Context.Pages.Single().Content.Html.Should().Contain("class=\"blazor-preview-iframe\"");
	}

	[Fact]
	public async Task HostWithoutFingerprinting_IsReportedByValidateAndDoctor_ButNotByBuild()
	{
		// Existing generated hosts are never rewritten, so the dry runs behind validate and doctor
		// say what to add. A warning in every build would fail strict builds.
		WriteBlock("<h3>Hi</h3>");
		FakePublishedHost("blazor.webassembly.js");

		DryRunOutcome dryRun = await BuildAsync([]);
		BuildContext build = await BuildToDiskAsync([]);

		dryRun.Context.Diagnostics.All.Should().ContainSingle(d => d.Source == _pluginId)
			.Which.Message.Should().Contain("<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>")
			.And.Contain("blazor.webassembly#[.{fingerprint}].js");
		build.Diagnostics.All.Should().NotContain(d => d.Source == _pluginId);
		File.ReadAllText(Path.Combine(_root, "_site", "_preview-wasm", "index.html")).Should().Contain("data-mokadocs-preview");
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

	/// <summary>
	///     A generated host published after its files were written, so the plugin doesn't publish
	///     it again. Returns its published wwwroot.
	/// </summary>
	private string FakePublishedHost(string blazorScript = _fingerprintedScript)
	{
		string host = Path.Combine(_root, ".mokadocs", "preview-host");
		string wwwroot = Path.Combine(host, "publish-output", "net10.0", "wwwroot");
		string framework = Path.Combine(wwwroot, "_framework");
		Directory.CreateDirectory(framework);
		File.WriteAllText(Path.Combine(wwwroot, "index.html"), _publishedIndex);
		File.WriteAllText(Path.Combine(framework, "dotnet.native.wasm"), "wasm");
		string marker = Path.Combine(framework, blazorScript);
		File.WriteAllText(marker, "");
		File.SetLastWriteTimeUtc(marker, DateTime.UtcNow.AddDays(1));
		AddReferenceAssembly(host);
		return wwwroot;
	}

	private static void WritePublishedHost(string wwwroot, string blazorScript, string index, DateTime publishedAt)
	{
		Directory.CreateDirectory(Path.Combine(wwwroot, "_framework"));
		File.WriteAllText(Path.Combine(wwwroot, "index.html"), index);
		string script = Path.Combine(wwwroot, "_framework", blazorScript);
		File.WriteAllText(script, "");
		File.SetLastWriteTimeUtc(script, publishedAt);
	}

	private static byte[] Decompress(Stream decompressor)
	{
		using var output = new MemoryStream();
		using (decompressor)
		{
			decompressor.CopyTo(output);
		}

		return output.ToArray();
	}

	private static string ShortHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes))[..10];

	/// <summary>A Publish target that writes the smallest published host, older than any input.</summary>
	private static string FakePublishTarget(string log) => $$"""
		<Target Name="Publish">
		  <PropertyGroup>
		    <Out>$([MSBuild]::EnsureTrailingSlash('$(PublishDir)'))</Out>
		  </PropertyGroup>
		  <MakeDir Directories="$(Out)wwwroot/_framework" />
		  <Touch Files="$(Out)wwwroot/_framework/{{_fingerprintedScript}}" AlwaysCreate="true" Time="2020-01-01" />
		  <WriteLinesToFile File="$(Out)wwwroot/index.html" Lines="&lt;html&gt;&lt;head&gt;&lt;/head&gt;&lt;/html&gt;" Overwrite="true" />
		  <WriteLinesToFile File="{{log}}" Lines="published" />
		</Target>
		""";

	private string WriteFakeHostProject(string body)
	{
		string host = Path.Combine(_root, "preview-host");
		Directory.CreateDirectory(host);
		File.WriteAllText(Path.Combine(host, "DocsPreviewHost.csproj"),
			"<Project>\n<!-- Stands in for a Microsoft.NET.Sdk.BlazorWebAssembly project. -->\n" +
			"<Target Name=\"Restore\" />\n" + body + "\n</Project>\n");
		return host;
	}

	/// <summary>Runs the real pipeline over the disk, writing the site to <c>_site</c>.</summary>
	private async Task<BuildContext> BuildToDiskAsync(Dictionary<string, object> options, string basePath = "/")
	{
		SiteConfig config = Config(options) with { Build = new BuildConfig { Output = "./_site", Cache = false, BasePath = basePath } };
		await using ServiceProvider provider = BuildCommand.BuildServices(config, false, _root);
		BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
		await BuildCommand.InitializePluginsAsync(provider, pipeline);
		var context = new BuildContext
		{
			Config = config,
			FileSystem = new FileSystem(),
			RootDirectory = _root,
			OutputDirectory = Path.Combine(_root, "_site"),
			UseCache = false
		};
		await pipeline.ExecuteAsync(context, TestContext.Current.CancellationToken);
		return context;
	}

	private SiteConfig Config(Dictionary<string, object> options) => new()
	{
		Site = new SiteMetadata { Title = "Previews" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = "./_site", Cache = false },
		Plugins = [new PluginDeclaration { Name = _pluginId, Options = options }]
	};

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
		Task.Run(() => DryRunBuild.RunAsync(Config(options), _root, false, false,
			ct == default ? TestContext.Current.CancellationToken : ct), CancellationToken.None);
}
