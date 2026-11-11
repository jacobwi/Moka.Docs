using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.Output;

public sealed class MinifiedOutputTests
{
	private static Dictionary<string, MockFileData> Files() => new()
	{
		["/project/docs/index.md"] = new("""
		                                 ---
		                                 title: Home
		                                 ---
		                                 # Home

		                                 Some   text.

		                                 <!-- an editor's note -->

		                                 ```csharp
		                                 if (ready)
		                                 {
		                                     Run();   // aligned
		                                 }
		                                 ```
		                                 """.ReplaceLineEndings("\n"))
	};

	[Fact]
	public async Task MinifyOff_WritesPagesAsRendered()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), Files());

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().Contain("\n    <meta").And.Contain("<!-- an editor's note -->");
	}

	[Fact]
	public async Task MinifyOn_CollapsesWhitespaceAndDropsCommentsButKeepsCode()
	{
		SiteConfig config = MockSite.Config() with { Build = new BuildConfig { Minify = true } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().NotContain("\n    <meta")
			.And.NotContain("an editor's note")
			.And.Contain("<pre><code class=\"language-csharp\">if (ready)\n{\n    Run();   // aligned\n}\n</code></pre>");
		fs.File.ReadAllText("/project/_site/404.html").Should().NotContain("\n    <");
	}
}
