using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;
using System.IO.Abstractions.TestingHelpers;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     Plugins report problems through <see cref="IPluginContext.LogWarning" /> and
///     <see cref="IPluginContext.LogError" />. Those used to reach only the logger, which build
///     and serve silence without --verbose, so a failing plugin produced a clean build.
/// </summary>
public sealed class PluginDiagnosticsTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-plugindiag-" + Guid.NewGuid().ToString("N"));

	public PluginDiagnosticsTests() => Directory.CreateDirectory(Path.Combine(_root, "docs"));

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private sealed class NoisyPlugin : IMokaPlugin
	{
		public IPluginContext? Context { get; private set; }
		public string Id => "noisy";
		public string Name => "Noisy";
		public string Version => "1.0.0";

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
		{
			Context = context;
			context.LogWarning("during init");
			return Task.CompletedTask;
		}

		public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			context.LogInfo("just info");
			context.LogWarning("spec file not found");
			context.LogError("analyzer crashed");
			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task LogWarningAndLogError_DuringExecution_BecomeBuildDiagnosticsFromThePlugin()
	{
		var plugin = new NoisyPlugin();
		var config = new SiteConfig { Site = new SiteMetadata { Title = "T" }, Plugins = [new PluginDeclaration { Name = "noisy" }] };
		ServiceProvider provider = new ServiceCollection()
			.AddSingleton<IMokaPlugin>(plugin)
			.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
			.BuildServiceProvider();
		var host = new PluginHost(config, provider, NullLogger<PluginHost>.Instance);
		var context = new BuildContext
		{
			Config = config,
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		};

		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);
		await host.ExecuteAllAsync(context, TestContext.Current.CancellationToken);

		context.Diagnostics.All.Select(d => (d.Severity, d.Message, d.Source)).Should().Equal(
			(DiagnosticSeverity.Warning, "spec file not found", "noisy"),
			(DiagnosticSeverity.Error, "analyzer crashed", "noisy"));

		// Outside a build there is nothing to attach to; this must not throw or leak into it.
		plugin.Context!.LogError("after the build");
		context.Diagnostics.All.Should().HaveCount(2);
	}

	[Fact]
	public async Task BlazorPreview_WithoutPreviewBlocks_DoesNotAskForAPreviewHost()
	{
		// The plugin set up its preview host on every build, so a site that declared it but had
		// no blocks yet failed with "no preview-host project found". Documentation that merely
		// mentions the data-blazor-preview attribute counted as a block, too.
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\nBlocks carry `data-blazor-preview=\"true\"` in the HTML.\n");

		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Source == "mokadocs-blazor-preview");
	}

	[Fact]
	public async Task BlazorPreview_WithABlockButNoPreviewHost_ReportsAnError()
	{
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\n```blazor-preview\n<h3>Hello</h3>\n```\n");

		DryRunOutcome outcome = await DryRunBuild.RunAsync(Config(), _root, false, false,
			TestContext.Current.CancellationToken);

		outcome.Context.Diagnostics.All.Should().ContainSingle(d =>
			d.Severity == DiagnosticSeverity.Error
			&& d.Source == "mokadocs-blazor-preview"
			&& d.Message.Contains("preview-host"));
	}

	[Fact]
	public async Task Build_WhenAPluginReportsAnError_ExitsNonZero()
	{
		// Error diagnostics used to leave the exit code at 0, so CI published a broken site.
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\n```blazor-preview\n<h3>Hello</h3>\n```\n");
		string yaml = Path.Combine(_root, "mokadocs.yaml");
		File.WriteAllText(yaml,
			"site:\n  title: T\ncontent:\n  docs: ./docs\nbuild:\n  output: ./_site\n  cache: false\nplugins:\n  - name: mokadocs-blazor-preview\n");

		int failing = await BuildCommand.Create().Parse(["--config", yaml]).InvokeAsync();

		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nNo previews.\n");
		int passing = await BuildCommand.Create().Parse(["--config", yaml]).InvokeAsync();

		failing.Should().Be(1);
		passing.Should().Be(0);
	}

	[Fact]
	public async Task DiscoverAndInitializeAsync_CalledForEveryBuild_LoadsEachPluginOnce()
	{
		// The ASP.NET Core host calls it before each in-memory build, and every call added the
		// plugins again, so they ran once more per rebuild.
		var plugin = new NoisyPlugin();
		var config = new SiteConfig { Site = new SiteMetadata { Title = "T" }, Plugins = [new PluginDeclaration { Name = "noisy" }] };
		ServiceProvider provider = new ServiceCollection()
			.AddSingleton<IMokaPlugin>(plugin)
			.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
			.BuildServiceProvider();
		var host = new PluginHost(config, provider, NullLogger<PluginHost>.Instance);

		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);
		await host.DiscoverAndInitializeAsync(TestContext.Current.CancellationToken);

		host.LoadedPlugins.Should().ContainSingle();
	}

	private static SiteConfig Config() => new()
	{
		Site = new SiteMetadata { Title = "Plugins" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = "./_site", Cache = false },
		Plugins = [new PluginDeclaration { Name = "mokadocs-blazor-preview" }]
	};
}
