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

		// The layouts have no indentation of their own any more; a blank line is where a condition wrote nothing.
		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().Contain("Some   text.").And.Contain("<!-- an editor's note -->").And.Contain("\n\n<link rel=\"canonical\"");
	}

	[Fact]
	public async Task MinifyOn_CollapsesWhitespaceAndDropsCommentsButKeepsCode()
	{
		SiteConfig config = MockSite.Config() with { Build = new BuildConfig { Minify = true } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().NotContain("\n\n<link rel=\"canonical\"")
			.And.Contain("<p>Some text.</p>")
			.And.NotContain("an editor's note")
			.And.Contain("<span class=\"line\"><span class=\"token keyword\">if</span> (ready)</span>\n<span class=\"line\">{</span>\n"
			             + "<span class=\"line\">    <span class=\"token type-name\">Run</span>();   <span class=\"token comment\">// aligned</span></span>\n"
			             + "<span class=\"line\">}</span></code></pre>");
		fs.File.ReadAllText("/project/_site/404.html").Should().NotContain("\n\n");
	}
}
