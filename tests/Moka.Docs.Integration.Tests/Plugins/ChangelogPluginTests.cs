using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.Changelog;

namespace Moka.Docs.Integration.Tests.Plugins;

public sealed class ChangelogPluginTests
{
	private static async Task<string> EnhanceAsync(string html)
	{
		var config = new SiteConfig { Site = new SiteMetadata { Title = "T" } };
		var context = new BuildContext
		{
			Config = config,
			FileSystem = new MockFileSystem(),
			RootDirectory = "/site",
			OutputDirectory = "/site/_site"
		};
		context.Pages.Add(new DocPage
		{
			FrontMatter = new FrontMatter { Title = "Changes" },
			Content = new PageContent { Html = html },
			Route = "/changes"
		});

		var pluginContext = new PluginContext(config, new Dictionary<string, object>(),
			new ServiceCollection().BuildServiceProvider(), NullLogger.Instance);
		await new ChangelogPlugin().ExecuteAsync(pluginContext, context, TestContext.Current.CancellationToken);
		return context.Pages[0].Content.Html;
	}

	[Fact]
	public async Task FirstRelease_OpensWithoutAMeasuredHeight()
	{
		// scrollHeight is 0 while the changelog sits in an inactive tab, so the first release
		// rendered 1px tall there.
		string html = await EnhanceAsync("<div class=\"changelog\"><div class=\"changelog-entry\"></div></div>");

		html.Should().MatchRegex(
			@"(?s)if \(index > 0\) \{\s*entry\.classList\.add\('changelog-collapsed'\);\s*body\.style\.maxHeight = '0';\s*\}\s*else \{\s*body\.style\.maxHeight = 'none';\s*\}");
	}
}
