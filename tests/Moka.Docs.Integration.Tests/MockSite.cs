using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;

namespace Moka.Docs.Integration.Tests;

/// <summary>
///     Runs the real pipeline over a <see cref="MockFileSystem" /> with the project at
///     <c>/project</c>, for test classes that don't live in <see cref="BuildPipelineIntegrationTests" />.
/// </summary>
internal static class MockSite
{
	public const string Root = "/project";

	public static SiteConfig Config(string title = "Test Site") => new()
	{
		Site = new SiteMetadata { Title = title, Url = "https://test.example.com" },
		Content = new ContentConfig { Docs = "./docs" }
	};

	public static async Task<(BuildContext Context, MockFileSystem FileSystem)> BuildAsync(
		SiteConfig config, Dictionary<string, MockFileData> files, bool dryRun = false)
	{
		var fs = new MockFileSystem(files);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IFileSystem>(fs);
		services.AddMokaDocsParsing();
		services.AddMokaDocsCSharp();
		services.AddMokaDocsEngine();

		await using ServiceProvider provider = services.BuildServiceProvider();
		var context = new BuildContext
		{
			Config = config,
			FileSystem = fs,
			RootDirectory = Root,
			OutputDirectory = Root + "/_site",
			DryRun = dryRun
		};

		await provider.GetRequiredService<BuildPipeline>().ExecuteAsync(context, TestContext.Current.CancellationToken);
		return (context, fs);
	}
}
