using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     Dry runs a real project on disk, since the C# analyzer reads source files directly.
/// </summary>
public sealed class ApiGenerationTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-api-" + Guid.NewGuid().ToString("N"));

	public ApiGenerationTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\nHome\n");
		Write("Directory.Build.props", "<Project><PropertyGroup><Version>2.3.4</Version></PropertyGroup></Project>");
		Write("src/Lib/Lib.csproj",
			"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><PackageId>Acme.Lib</PackageId></PropertyGroup></Project>");
		Write("src/Lib/Widget.cs", """
		                           namespace Lib;

		                           /// <summary>Holds a <c>List&lt;T&gt;</c> of parts.</summary>
		                           public class Widget { }
		                           """);
		Write("src/Lib/Helper.cs", """
		                           /// <summary>Lives in the global namespace.</summary>
		                           public class Helper { }
		                           """);
	}

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

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	private async Task<DryRunOutcome> RunAsync(Func<SiteConfig, SiteConfig>? configure = null)
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Api" },
			Content = new ContentConfig
			{
				Docs = "./docs",
				Projects = [new ProjectSource { Path = "./src/Lib/Lib.csproj" }]
			},
			Build = new BuildConfig { Cache = false }
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(configure?.Invoke(config) ?? config, _root, false, false,
			TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		return outcome;
	}

	[Fact]
	public async Task PackageVersion_ComesFromDirectoryBuildPropsWhenTheProjectHasNone()
	{
		// The install widget showed 1.0.0 for every project that set its version there.
		DryRunOutcome outcome = await RunAsync();

		outcome.Context.PackageInfo!.Name.Should().Be("Acme.Lib");
		outcome.Context.PackageInfo.Version.Should().Be("2.3.4");
	}

	[Fact]
	public async Task PackageVersion_InTheProjectFile_WinsOverDirectoryBuildProps()
	{
		Write("src/Lib/Lib.csproj",
			"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><VersionPrefix>3.0.0</VersionPrefix><VersionSuffix>beta</VersionSuffix></PropertyGroup></Project>");

		DryRunOutcome outcome = await RunAsync();

		outcome.Context.PackageInfo!.Version.Should().Be("3.0.0-beta");
	}

	[Fact]
	public async Task GlobalNamespaceType_GetsARouteThatIsAValidPath()
	{
		DryRunOutcome outcome = await RunAsync();

		outcome.Context.Pages.Should().Contain(p => p.Route == "/api/(global)/helper");
		outcome.Context.Pages.Should().NotContain(p => p.Route.Contains('<') || p.Route.Contains(' '));
	}

	[Fact]
	public async Task TypeSummary_IsPlainTextInMetadataAndEscapedInTheBody()
	{
		DryRunOutcome outcome = await RunAsync();

		DocPage page = outcome.Context.Pages.Single(p => p.Route == "/api/lib/widget");
		page.FrontMatter.Description.Should().Be("Holds a List<T> of parts.");
		page.Content.PlainText.Should().Be("Holds a List<T> of parts.");
		page.Content.Html.Should().Contain("<code>List&lt;T&gt;</code>")
			.And.Contain("<meta name=\"description\" content=\"Holds a List&lt;T&gt; of parts.\" />");
	}

	[Fact]
	public async Task EditLink_OnlyOnPagesWithASourceFile()
	{
		// Generated API pages linked to the docs folder itself.
		DryRunOutcome outcome = await RunAsync(config => config with
		{
			Site = config.Site with
			{
				EditLink = new EditLinkConfig { Repo = "https://github.com/o/r", Branch = "main", Path = "docs" }
			},
			Theme = new ThemeConfig { Options = new ThemeOptions { ShowEditLink = true } }
		});

		outcome.Context.Pages.Single(p => p.Route == "/").Content.Html
			.Should().Contain("https://github.com/o/r/edit/main/docs/index.md");
		outcome.Context.Pages.Single(p => p.Route == "/api/lib/widget").Content.Html
			.Should().NotContain("github.com/o/r/edit");
	}
}
