using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     The Blazor preview plugin scaffolds a preview host when it finds none. Each test fakes a
///     publish output newer than the scaffolded files, so the plugin skips <c>dotnet publish</c>,
///     which downloads packages and takes minutes, and still compiles the block.
/// </summary>
public sealed class BlazorPreviewScaffoldTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-scaffold-" + Guid.NewGuid().ToString("N"));

	public BlazorPreviewScaffoldTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\n```blazor-preview\n<h3>Hello</h3>\n```\n");
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
	public async Task BlockWithoutPreviewHostOrLibrary_ScaffoldsAHostWithoutALibraryInTheCacheFolder()
	{
		// Without a preview host or a library option the build failed and asked for a library,
		// although plain Razor blocks need none.
		string host = FakePublishedHost(Path.Combine(".mokadocs", "preview-host"));

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object>());

		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Source == "mokadocs-blazor-preview");
		string csproj = File.ReadAllText(Path.Combine(host, "DocsPreviewHost.csproj"));
		csproj.Should().Contain("<PackageReference Include=\"Moka.Blazor.Repl.Host\" Version=\"")
			.And.NotContain("{LIBRARY").And.NotContain("Your component library");
		Regex.Matches(csproj, "<PackageReference ").Should().HaveCount(3,
			"the WebAssembly packages and Moka.Blazor.Repl.Host, but no library");
		Directory.Exists(Path.Combine(_root, "preview-host")).Should().BeFalse();
		outcome.Context.Pages.Single().Content.Html.Should().Contain("class=\"blazor-preview-iframe\"");
	}

	[Fact]
	public async Task BlockWithALibraryOption_ScaffoldsAHostThatReferencesTheLibrary()
	{
		string host = FakePublishedHost("preview-host");

		DryRunOutcome outcome = await BuildAsync(new Dictionary<string, object> { ["library"] = "Contoso.Widgets@2.1.0" });

		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Source == "mokadocs-blazor-preview");
		string csproj = File.ReadAllText(Path.Combine(host, "DocsPreviewHost.csproj"));
		csproj.Should().Contain("<PackageReference Include=\"Contoso.Widgets\" Version=\"2.1.0\" />")
			.And.NotContain("{LIBRARY");
		Regex.Matches(csproj, "<PackageReference ").Should().HaveCount(4);
		File.ReadAllText(Path.Combine(host, "wwwroot", "index.html")).Should().Contain("_content/Contoso.Widgets/");
	}

	private string FakePublishedHost(string relativeDirectory)
	{
		string host = Path.Combine(_root, relativeDirectory);
		string framework = Path.Combine(host, "publish-output", "net10.0", "wwwroot", "_framework");
		Directory.CreateDirectory(framework);
		string marker = Path.Combine(framework, "blazor.webassembly.js");
		File.WriteAllText(marker, "");
		File.SetLastWriteTimeUtc(marker, DateTime.UtcNow.AddDays(1));

		// Roslyn takes its references from the host's bin folder, which needs at least one dll.
		string bin = Path.Combine(host, "bin", "Release", "net10.0");
		Directory.CreateDirectory(bin);
		File.Copy(typeof(SiteConfig).Assembly.Location, Path.Combine(bin, "Moka.Docs.Core.dll"));
		return host;
	}

	private Task<DryRunOutcome> BuildAsync(Dictionary<string, object> options) =>
		DryRunBuild.RunAsync(new SiteConfig
		{
			Site = new SiteMetadata { Title = "Previews" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false },
			Plugins = [new PluginDeclaration { Name = "mokadocs-blazor-preview", Options = options }]
		}, _root, false, false, TestContext.Current.CancellationToken);
}
