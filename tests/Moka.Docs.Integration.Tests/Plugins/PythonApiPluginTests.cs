using System.Diagnostics;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     Runs the real analyzer script, so these tests need Python 3 on PATH and skip without it.
/// </summary>
public sealed class PythonApiPluginTests : IDisposable
{
	// Same candidates the plugin tries: python3, then python. On Windows python3 is often a
	// Store alias that exits 9009.
	private static readonly Lazy<bool> _pythonAvailable = new(() => new[] { "python3", "python" }.Any(python =>
	{
		try
		{
			using Process? process = Process.Start(new ProcessStartInfo(python, "--version")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			return process is not null && process.WaitForExit(10_000) && process.ExitCode == 0;
		}
		catch (Exception)
		{
			return false;
		}
	}));

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-python-" + Guid.NewGuid().ToString("N"));

	public PythonApiPluginTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		Directory.CreateDirectory(Path.Combine(_root, "src"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\nHome\n");
		string[] calc =
		[
			"'''Calculator helpers.'''",
			"",
			"class Calc:",
			"    '''Adds numbers.'''",
			"",
			"    def add(self, a: int, b: int) -> int:",
			"        '''Adds two numbers.'''",
			"        return a + b",
			"",
			"",
			"def double(value: int) -> int:",
			"    '''Doubles a number.'''",
			"    return value * 2",
			""
		];
		File.WriteAllText(Path.Combine(_root, "src", "calc.py"), string.Join("\n", calc));
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

	private async Task<List<DocPage>> BuildAsync(params PluginDeclaration[] plugins)
	{
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Python" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false },
			Plugins = [.. plugins]
		};

		DryRunOutcome outcome = await DryRunBuild.RunAsync(config, _root, false, false,
			TestContext.Current.CancellationToken);
		outcome.Failure.Should().BeNull();
		outcome.Context.Diagnostics.All.Should().NotContain(d => d.Source == "mokadocs-python-api");
		return outcome.Context.Pages;
	}

	private static PluginDeclaration Declaration(params (string Key, object Value)[] options) => new()
	{
		Name = "mokadocs-python-api",
		Options = options.ToDictionary(o => o.Key, o => o.Value)
	};

	[Fact]
	public async Task ModuleFunctionsAndAClassNamedLikeTheModule_GetSeparatePages()
	{
		// calc.py defines class Calc. The module's functions page used the same route as the
		// class page, and one of the two was overwritten.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./src")));

		pages.Should().Contain(p => p.Route == "/python-api/calc" && p.FrontMatter.Title == "calc");
		pages.Should().Contain(p => p.Route == "/python-api/calc/calc" && p.FrontMatter.Title == "Calc");
		pages.GroupBy(p => p.Route, StringComparer.OrdinalIgnoreCase).Should().OnlyContain(g => g.Count() == 1);
	}

	[Fact]
	public async Task SecondDeclaration_DoesNotInheritTheFirstOnesOptions()
	{
		// Both entries run on one plugin instance, and option fields were only overwritten
		// when present, so the second entry used /first as its route prefix.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");

		List<DocPage> pages = await BuildAsync(
			Declaration(("source", "./src"), ("routePrefix", "/first")),
			Declaration(("source", "./src")));

		pages.Should().Contain(p => p.Route == "/first/calc/calc");
		pages.Should().Contain(p => p.Route == "/python-api/calc/calc");
	}

	[Fact]
	public async Task Docstrings_AreEscapedAndExamplesKeepTheirLines()
	{
		// Docstrings went into the page as raw HTML, and examples collapsed into one line.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");
		string[] shapes =
		[
			"class Shape:",
			"    '''Compares when a < b <b>really</b>.",
			"",
			"    Example:",
			"        >>> Shape().area()",
			"        0",
			"    '''",
			"",
			"    def area(self) -> int:",
			"        '''Area.'''",
			"        return 0",
			""
		];
		File.WriteAllText(Path.Combine(_root, "src", "shapes.py"), string.Join("\n", shapes));

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./src")));

		string html = pages.Single(p => p.Route == "/python-api/shapes/shape").Content.Html;
		string body = html[html.IndexOf("<article", StringComparison.Ordinal)..];
		body = body[..body.IndexOf("</article>", StringComparison.Ordinal)];
		body.Should().Contain("a &lt; b &lt;b&gt;really&lt;/b&gt;").And.NotContain("<b>really</b>");
		body.Should().Contain("<pre><code class=\"language-python\">&gt;&gt;&gt; Shape().area()\n0</code></pre>");

		// View Source keeps the method indented under the class.
		body.Should().Contain("class Shape:\n");
		body.Should().Contain("\n    def area(self) -&gt; int:");
	}
}
