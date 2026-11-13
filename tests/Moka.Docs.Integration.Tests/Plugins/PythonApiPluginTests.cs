using System.Diagnostics;
using System.Text.RegularExpressions;
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

	private static string Article(string html)
	{
		string body = html[html.IndexOf("<article", StringComparison.Ordinal)..];
		return body[..body.IndexOf("</article>", StringComparison.Ordinal)];
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
	public async Task Pages_AreLabelledAndHighlightedAsPython()
	{
		// Declarations, signatures and View Source were labelled and highlighted as C#.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./src")));

		string calc = Article(pages.Single(p => p.Route == "/python-api/calc/calc").Content.Html);
		calc.Should().NotContain("language-csharp").And.NotContain("data-lang=\"csharp\"");
		calc.Should().Contain("<div class=\"code-block\" data-lang=\"python\">")
			.And.Contain("<span class=\"line\"><span class=\"token keyword\">class</span> Calc:</span>")
			.And.Contain("<span class=\"token keyword\">def</span> add(<span class=\"token keyword\">self</span>, a: int, b: int) -&gt; int</span>");
		Article(pages.Single(p => p.Route == "/python-api/calc").Content.Html)
			.Should().Contain("<span class=\"line\"><span class=\"token keyword\">import</span> calc</span>")
			.And.NotContain("language-csharp");
	}

	[Fact]
	public async Task SourceEndingInASeparator_IsAnalyzed()
	{
		// The arguments were one quoted string, and on Windows "C:\...\src\" escaped its closing
		// quote, so Python got the source path and every argument after it as one argument.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./src/")));

		pages.Should().Contain(p => p.Route == "/python-api/calc/calc");
	}

	[Fact]
	public async Task ClassInAPackageAndASubmoduleNamedLikeIt_GetSeparatePages()
	{
		// Class Calc in mypkg/__init__.py and the functions of mypkg/calc.py both mapped to
		// /python-api/mypkg/calc, and one page overwrote the other.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");
		WritePackage();

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./pkgsrc")));

		pages.Should().Contain(p => p.Route == "/python-api/mypkg/calc" && p.FrontMatter.Title == "calc",
			"the module keeps its own path");
		DocPage calcClass = pages.Should().ContainSingle(p => p.FrontMatter.Title == "Calc").Subject;
		calcClass.Route.Should().Be("/python-api/mypkg/calc-2");
		pages.GroupBy(p => p.Route, StringComparer.OrdinalIgnoreCase).Should().OnlyContain(g => g.Count() == 1);
		pages.Single(p => p.Route == "/python-api").Content.Html
			.Should().Contain("href=\"/python-api/mypkg/calc\"").And.Contain("href=\"/python-api/mypkg/calc-2\"");
	}

	[Fact]
	public async Task SourcePointingAtAPackage_NamesItsInitModuleAfterTheFolder()
	{
		// An __init__.py directly in source became a module named __init__.
		Assert.SkipUnless(_pythonAvailable.Value, "python3 is not on PATH");
		WritePackage();

		List<DocPage> pages = await BuildAsync(Declaration(("source", "./pkgsrc/mypkg")));

		pages.Should().Contain(p => p.Route == "/python-api/mypkg" && p.FrontMatter.Title == "mypkg");
		pages.Should().Contain(p => p.Route == "/python-api/mypkg/calc" && p.FrontMatter.Title == "Calc");
		pages.Should().NotContain(p => p.Route.Contains("__init__", StringComparison.Ordinal));
	}

	[Fact]
	public async Task CancellingTheBuild_StopsTheAnalyzerAndWhatItStarted()
	{
		// The analyzer was read with the build's token but never killed, so a cancelled build
		// left it running.
		using var tree = new HangingProcessTree();
		string fakePython = tree.WriteScript();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Python" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false },
			Plugins = [Declaration(("source", "./src"), ("pythonPath", fakePython))]
		};

		Task<DryRunOutcome> build = DryRunBuild.RunAsync(config, _root, false, false, cts.Token);
		await tree.WaitUntilStartedAsync(TimeSpan.FromSeconds(60));
		await cts.CancelAsync();
		Func<Task> act = () => build.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<OperationCanceledException>();
		(await tree.GrandchildExitedAsync(TimeSpan.FromSeconds(15))).Should().BeTrue();
	}

	private void WritePackage()
	{
		string package = Path.Combine(_root, "pkgsrc", "mypkg");
		Directory.CreateDirectory(package);
		File.WriteAllText(Path.Combine(package, "__init__.py"), string.Join("\n",
			"'''My package.'''",
			"",
			"class Calc:",
			"    '''A calculator defined in the package.'''",
			"",
			"    def add(self, a: int, b: int) -> int:",
			"        '''Adds two numbers.'''",
			"        return a + b",
			"",
			"",
			"def helper(x: int) -> int:",
			"    '''Package level helper.'''",
			"    return x",
			""));
		File.WriteAllText(Path.Combine(package, "calc.py"), string.Join("\n",
			"'''Calc module functions.'''",
			"",
			"def compute(x: int) -> int:",
			"    '''Computes something.'''",
			"    return x * 2",
			""));
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
		body.Should().Contain("<span class=\"line\">&gt;&gt;&gt; Shape().area()</span>\n<span class=\"line\"><span class=\"token number\">0</span></span></code></pre>");

		// View Source keeps the method indented under the class.
		string source = Regex.Replace(body, "<span class=\"(?:line|token [a-z-]+)\">|</span>", "");
		source.Should().Contain("class Shape:\n");
		source.Should().Contain("\n    def area(self) -&gt; int:");
	}
}
