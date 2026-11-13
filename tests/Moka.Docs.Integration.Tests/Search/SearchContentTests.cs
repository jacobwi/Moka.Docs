using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using FluentAssertions;
using Moka.Docs.Engine.Search;

namespace Moka.Docs.Integration.Tests.Search;

/// <summary>
///     Heading entries used to have no content and code blocks weren't indexed, so only a page's
///     first 300 characters and its headings could be found.
/// </summary>
public sealed class SearchContentTests
{
	[Fact]
	public void Split_GivesEachHeadingTheTextUnderIt()
	{
		const string html = """
		                    <p>Intro <em>text</em>.</p>
		                    <h2 id="install">Install</h2>
		                    <p>Run the <code>dotnet</code> tool.</p>
		                    <h3 id="windows">Windows</h3>
		                    <p>Use &amp; enjoy&nbsp;it.</p><h4>Deep</h4><p>Still windows.</p>
		                    """;

		SectionSplit split = SearchText.Split(html, new HashSet<string> { "install", "windows" });

		split.Intro.Text.Should().Be("Intro text.");
		split.Sections["install"].Text.Should().Be("Run the dotnet tool.");
		split.Sections["windows"].Text.Should().Be("Use & enjoy it. Deep Still windows.");
	}

	[Fact]
	public void Split_IndexesCodeBlocksAsWordsAndKeepsThemOutOfTheText()
	{
		const string html = "<h2 id=\"setup\">Setup</h2><p>Call it:</p>"
		                    + "<pre><code class=\"language-csharp\">services.AddMokaDocs(options =&gt; options.Title = \"Docs\");\nservices.AddMokaDocs();</code></pre>"
		                    + "<pre class=\"mermaid\">graph TD; Alpha--&gt;Beta</pre>"
		                    + "<script>var hidden = 1;</script><!-- <p>comment</p> --><p>Done.</p>";

		SectionText setup = SearchText.Split(html, new HashSet<string> { "setup" }).Sections["setup"];

		setup.Text.Should().Be("Call it: Done.");
		setup.Code.Should().Be("services.AddMokaDocs options.Title");
	}

	[Fact]
	public void CodeWords_DropRepeatsNumbersAndWordsAnotherWordContains()
	{
		SearchText.CodeWords(["Foo foo FOO 123 1.0 --- --base-path base x .config."], 300)
			.Should().Be("Foo --base-path config");
	}

	[Fact]
	public void CodeWords_StopAtTheLimit()
	{
		string code = string.Join(" ", Enumerable.Range(0, 100).Select(i => $"word{i:D3}"));

		SearchText.CodeWords([code], 50).Should().Be("word000 word001 word002 word003 word004 word005");
	}

	[Theory]
	[InlineData(250, 100, 250)]
	[InlineData(400, 0, 300)]
	[InlineData(100, 50, 151)]
	public void Trim_CutsAtTheLastSpaceNearTheLimit(int first, int second, int expectedLength)
	{
		string text = second == 0 ? new string('a', first) : new string('a', first) + " " + new string('b', second);

		SearchText.Trim(text, 300).Length.Should().Be(expectedLength);
	}

	[Fact]
	public async Task SearchIndex_HeadingEntriesCarryTheirSectionTextAndCodeWords()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), new Dictionary<string, MockFileData>
		{
			["/project/docs/guide.md"] = new("""
			                                 ---
			                                 title: Guide
			                                 ---
			                                 # Guide

			                                 Intro paragraph.

			                                 ## Install

			                                 Run the tool.

			                                 ```bash
			                                 dotnet tool install --global mokadocs
			                                 ```

			                                 ## Configure

			                                 Edit the file.
			                                 """)
		});

		using JsonDocument index = JsonDocument.Parse(fs.File.ReadAllText("/project/_site/search-index.json"));
		Dictionary<string, JsonElement> byRoute = index.RootElement.EnumerateArray()
			.ToDictionary(e => e.GetProperty("r").GetString()!);

		// The page's own h1 is the page entry, not a section of it.
		byRoute.Should().NotContainKey("/guide#guide");
		byRoute["/guide"].GetProperty("c").GetString().Should().Contain("Intro paragraph.");
		byRoute["/guide#install"].GetProperty("c").GetString().Should().Be("Run the tool.");
		byRoute["/guide#install"].GetProperty("x").GetString().Should().Be("dotnet tool install --global mokadocs");
		byRoute["/guide#configure"].GetProperty("c").GetString().Should().Be("Edit the file.");
		byRoute["/guide#configure"].TryGetProperty("x", out _).Should().BeFalse("a section without code has no code words");
		byRoute["/guide"].TryGetProperty("x", out _).Should().BeFalse();
	}

	[Fact]
	public async Task SearchIndex_CodeWordsLeaveOutTheHighlightingMarkup()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config(), new Dictionary<string, MockFileData>
		{
			["/project/docs/guide.md"] = new("""
			                                 ---
			                                 title: Guide
			                                 ---
			                                 # Guide

			                                 ## Setup

			                                 ```csharp title="Program.cs" {1} showLineNumbers
			                                 var app = WebApplication.Create(args); // [!code focus]
			                                 app.MapDocs(); // [!code ++]
			                                 ```

			                                 ```diff-json
			                                 -{ "oldKey": 1 }
			                                 +{ "newKey": 2 }
			                                 ```

			                                 <pre><code class="language-yaml">siteTitle: Docs</code></pre>
			                                 """)
		});

		using JsonDocument index = JsonDocument.Parse(fs.File.ReadAllText("/project/_site/search-index.json"));
		JsonElement setup = index.RootElement.EnumerateArray().Single(e => e.GetProperty("r").GetString() == "/guide#setup");

		// "app" and "Docs" are left out because longer words contain them (CodeWords ignores case).
		setup.GetProperty("x").GetString().Should().Be("var WebApplication.Create args app.MapDocs oldKey newKey siteTitle");
		setup.GetProperty("c").GetString().Should().Be("Program.cs");
	}
}
