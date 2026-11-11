using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks behavior of the default theme's script and stylesheet. The search snippet code runs
///     under Node when it is on PATH; the rest looks for the rules that implement a fix.
/// </summary>
public sealed class EmbeddedThemeScriptTests
{
	private static readonly Lazy<bool> _nodeAvailable = new(() =>
	{
		try
		{
			using Process? node = Process.Start(new ProcessStartInfo("node", "--version")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false
			});
			return node is not null && node.WaitForExit(10_000) && node.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			return false;
		}
	});

	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	public static TheoryData<string, string, string> SnippetCases()
	{
		string longText = new string('x', 40) + " target " + new string('y', 100);
		return new TheoryData<string, string, string>
		{
			// Highlighting the escaped text turned &lt; into &<mark>lt</mark>;.
			{ "if a < b then", "lt", "if a &lt; b then" },
			// A second term matched inside the <mark> tags the first one had added.
			{ "a mark here", "a mark", "<mark>a</mark> <mark>mark</mark> here" },
			{ "Say \"hi\" & <go>", "hi", "Say &quot;<mark>hi</mark>&quot; &amp; &lt;go&gt;" },
			{ "List<T> and list", "LIST", "<mark>List</mark>&lt;T&gt; and <mark>list</mark>" },
			{ longText, "target", "..." + new string('x', 29) + " <mark>target</mark> " + new string('y', 80) + "..." }
		};
	}

	[Theory]
	[MemberData(nameof(SnippetCases))]
	public void SearchSnippet_HighlightsTheRawTextAndThenEscapesIt(string text, string query, string expected)
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		string[] terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

		string result = RunNode($$"""
		                          {{SearchFunctions()}}
		                          process.stdout.write(JSON.stringify(searchSnippet({{JsonSerializer.Serialize(text)}}, {{JsonSerializer.Serialize(terms)}})));
		                          """);

		JsonSerializer.Deserialize<string>(result).Should().Be(expected);
	}

	[Fact]
	public void SearchResultLinks_EscapeQuotesInTheHref()
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");

		string result = RunNode($$"""
		                          {{SearchFunctions()}}
		                          process.stdout.write(JSON.stringify(esc('/a"b\'c<d>&e')));
		                          """);

		JsonSerializer.Deserialize<string>(result).Should().Be("/a&quot;b&#39;c&lt;d&gt;&amp;e");
		_js.Should().Contain("href=\"' + esc(e.r) + '\"");
	}

	[Fact]
	public void Search_MatchesWordsThatOnlyAppearInCode()
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		int start = _js.IndexOf("function doSearch(", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		string doSearch = _js[start.._js.IndexOf("function searchSnippet(", start, StringComparison.Ordinal)];

		string result = RunNode($$"""
		                          let searchData = [
		                              { t: 'Guide', s: 'Install', r: '/guide#install', c: 'Run the tool.', x: 'dotnet tool install', g: 'Documentation' },
		                              { t: 'Other', s: '', r: '/other', c: 'Nothing here.', g: 'Documentation' }
		                          ];
		                          let activeIdx = -1;
		                          const searchResults = { innerHTML: '', classList: { add() {}, remove() {}, toggle() {} } };
		                          {{doSearch}}
		                          {{SearchFunctions()}}
		                          doSearch('DOTNET');
		                          process.stdout.write(JSON.stringify(searchResults.innerHTML));
		                          """);

		JsonSerializer.Deserialize<string>(result).Should().Contain("href=\"/guide#install\"").And.NotContain("/other");
	}

	[Fact]
	public void VersionMenu_ClosesOnClicksOutsideItInTheCapturePhase()
	{
		// The other pickers stop their clicks from bubbling, so a bubble-phase listener left the
		// version menu open next to the picker that had just opened.
		Match listener = Regex.Match(_js,
			@"document\.addEventListener\('click', \(e\) => \{\s*if \(!e\.target\.closest \|\| !e\.target\.closest\('\.version-selector'\)\) \{[^}]*\}\s*\}, true\);");

		listener.Success.Should().BeTrue();
		Regex.IsMatch(_js, @"if \(!isOpen\) \{\s*document\.querySelectorAll\('\.color-theme-dropdown, \.code-theme-dropdown, \.code-style-dropdown'\)")
			.Should().BeTrue("opening the version menu closes the other pickers");
	}

	[Fact]
	public void HeaderPickers_OpeningOneClosesTheOthers()
	{
		// Each picker stops its clicks from bubbling, so opening the color picker left the code
		// theme and code style menus open under it.
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		int start = _js.IndexOf("function closeOtherPickers(", StringComparison.Ordinal);
		int end = _js.IndexOf("// Style selector", start, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		end.Should().BeGreaterThan(start);

		string result = RunNode($$"""
		                          function element() {
		                              const attributes = {}, listeners = [];
		                              return {
		                                  setAttribute(k, v) { attributes[k] = String(v); },
		                                  getAttribute(k) { return k in attributes ? attributes[k] : null; },
		                                  removeAttribute(k) { delete attributes[k]; },
		                                  hasAttribute(k) { return k in attributes; },
		                                  addEventListener(type, f) { if (type === 'click') listeners.push(f); },
		                                  querySelectorAll() { return []; },
		                                  click() { listeners.forEach(f => f({ stopPropagation() {} })); }
		                              };
		                          }
		                          const nodes = {};
		                          for (const kind of ['color-theme', 'code-theme', 'code-style']) {
		                              nodes['.' + kind + '-trigger'] = element();
		                              nodes['.' + kind + '-dropdown'] = element();
		                              nodes['.' + kind + '-dropdown'].setAttribute('hidden', '');
		                          }
		                          const document = { querySelector: s => nodes[s] || null, querySelectorAll: () => [], addEventListener() {} };
		                          const html = element();
		                          const localStorage = { getItem: () => null, setItem() {} };
		                          {{_js[start..end]}}
		                          const state = () => ['color-theme', 'code-theme', 'code-style']
		                              .map(k => (nodes['.' + k + '-dropdown'].hasAttribute('hidden') ? '-' : 'open') + ':' + nodes['.' + k + '-trigger'].getAttribute('aria-expanded'))
		                              .join(' ');
		                          const seen = [];
		                          for (const kind of ['code-theme', 'color-theme', 'code-style', 'color-theme', 'code-theme']) {
		                              nodes['.' + kind + '-trigger'].click();
		                              seen.push(state());
		                          }
		                          process.stdout.write(JSON.stringify(seen));
		                          """);

		JsonSerializer.Deserialize<string[]>(result).Should().Equal(
			"-:false open:true -:false",
			"open:true -:false -:false",
			"-:false -:false open:true",
			"open:true -:false -:false",
			"-:false open:true -:false");
	}

	public static TheoryData<string> ColorPresets() => ["ocean", "emerald", "violet", "amber", "rose", "moka-red"];

	[Theory]
	[MemberData(nameof(ColorPresets))]
	public void AccentText_MeetsAaInTheDefaultStyle(string preset)
	{
		// Links and inline code used the raw preset color: ocean was 2.8:1 on white, amber 2.2:1.
		string primary = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{\s*--color-primary: (#[0-9a-f]{{6}}) !important;").Groups[1].Value;
		string light = Regex.Match(_css, $@"html\[data-color-theme=""{preset}""\] \{{ --color-primary-text: (#[0-9a-f]{{6}}); \}}") is { Success: true } l
			? l.Groups[1].Value
			: primary;
		string dark = Regex.Match(_css, $@"html\[data-theme=""dark""\]\[data-color-theme=""{preset}""\] \{{ --color-primary-text: (#[0-9a-f]{{6}}); \}}") is { Success: true } d
			? d.Groups[1].Value
			: Regex.IsMatch(_css, $@"html\[data-theme=""dark""\]:is\([^)]*\[data-color-theme=""{preset}""\][^)]*\) \{{ --color-primary-text: var\(--color-primary\); \}}")
				? primary
				: light;

		primary.Should().NotBeEmpty();
		Regex.Match(_css, @"\n\s*\.nav-link\.current \{[^}]*\}").Value
			.Should().Contain("background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg));");
		foreach (string background in new[] { "--color-bg", "--color-bg-secondary", "--color-bg-code" })
		{
			Contrast(light, Variable(":root {", background)).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} in light mode", preset, background);
			Contrast(dark, Variable("[data-theme=\"dark\"] {", background)).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} in dark mode", preset, background);
		}

		// The current sidebar link sits on 8% of the preset over the page background.
		Contrast(light, Mix(primary, 0.08, Variable(":root {", "--color-bg"))).Should().BeGreaterThanOrEqualTo(4.5, "{0} current link, light", preset);
		Contrast(dark, Mix(primary, 0.08, Variable("[data-theme=\"dark\"] {", "--color-bg"))).Should().BeGreaterThanOrEqualTo(4.5, "{0} current link, dark", preset);
	}

	private static string Mix(string color, double amount, string background)
	{
		int Channel(string hex, int offset) => int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		return "#" + string.Concat(new[] { 1, 3, 5 }.Select(o =>
			((int)Math.Round(amount * Channel(color, o) + (1 - amount) * Channel(background, o), MidpointRounding.AwayFromZero)).ToString("x2", CultureInfo.InvariantCulture)));
	}

	[Theory]
	[InlineData(":root {")]
	[InlineData("[data-theme=\"dark\"] {")]
	public void MutedText_MeetsAaInTheDefaultStyle(string scope)
	{
		// It was 2.6:1 on white and 3.1:1 on the dark secondary background.
		string muted = Variable(scope, "--color-text-muted");

		Contrast(muted, Variable(scope, "--color-bg")).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(muted, Variable(scope, "--color-bg-secondary")).Should().BeGreaterThanOrEqualTo(4.5);
	}

	[Fact]
	public void AccentText_UsesTheAaColorWhereItIsText()
	{
		_css.Should().Contain("a { color: var(--color-primary-text);")
			.And.Contain("a:hover { color: var(--color-primary-text-hover); }");
		Regex.Match(_css, @"\n\s*\.page-content code \{[^}]*\}").Value.Should().Contain("color: var(--color-primary-text);");
		Regex.Match(_css, @"\n\s*\.nav-link\.current \{[^}]*\}").Value.Should().Contain("color: var(--color-primary-text);");
	}

	[Fact]
	public void AnimationsOff_DropTheDelaysThatKeptTheLandingHeroHidden()
	{
		// The hero title, subtitle and buttons fade in with delays up to 0.4 s and fill-mode both.
		// Shortening only the duration left them invisible for the delay.
		string reducedMotion = Block("@media (prefers-reduced-motion: reduce) {");
		string noAnimations = Block("[data-no-animations] *, [data-no-animations] *::before, [data-no-animations] *::after {");

		reducedMotion.Should().Contain("animation-delay: 0s !important;");
		noAnimations.Should().Contain("animation-delay: 0s !important;");
	}

	public static TheoryData<string> CodeThemes() =>
		["catppuccin-mocha", "catppuccin-latte", "github-dark", "github-light", "dracula", "one-dark", "nord"];

	[Theory]
	[MemberData(nameof(CodeThemes))]
	public void LineNumbers_MeetAaContrastOnTheCodeBackground(string codeTheme)
	{
		// They were the page's muted text color at 35% opacity: about 2:1 on every code theme.
		string variables = Regex.Match(_css, $@"\[data-code-theme=""{codeTheme}""\] \{{(?<body>[^}}]*)\}}").Groups["body"].Value;
		string lineNumber = Regex.Match(variables, @"--sh-line-number: (#[0-9a-f]{6});").Groups[1].Value;
		string background = Regex.Match(_css, $@"\[data-code-theme=""{codeTheme}""\] \.page-content pre \{{ background: (#[0-9a-f]{{6}});").Groups[1].Value;

		lineNumber.Should().NotBeEmpty();
		background.Should().NotBeEmpty();
		Contrast(lineNumber, background).Should().BeGreaterThanOrEqualTo(4.5);
		Contrast(lineNumber, "#0a0a0d").Should().BeGreaterThanOrEqualTo(codeTheme.Contains("light") || codeTheme.Contains("latte") ? 0 : 4.5,
			"the moka style darkens dark code themes to #0a0a0d");
	}

	[Theory]
	[InlineData(":root {")]
	[InlineData("[data-theme=\"dark\"] {")]
	public void LandingLineNumbers_MeetAaContrastInTheDefaultStyle(string scope)
	{
		// Landing code blocks use the page colors, not a code theme. The muted text color they had
		// is 2.45:1 on the light code background.
		string rule = Regex.Match(_css, @"\.landing-content pre \.line-numbers \{[^}]*\}").Value;
		string variable = Regex.Match(rule, @"color: var\((--[\w-]+)\)").Groups[1].Value;
		string variables = Block("\n" + scope);

		string Value(string name) => Regex.Match(variables, Regex.Escape(name) + @": (#[0-9a-f]{6});").Groups[1].Value;

		variable.Should().NotBeEmpty();
		Contrast(Value(variable), Value("--color-bg-secondary")).Should().BeGreaterThanOrEqualTo(4.5);
	}

	[Fact]
	public void LineNumbers_AreNotFadedWithOpacity()
	{
		MatchCollection rules = Regex.Matches(_css, @"[^{}]*\.line-numbers \{[^}]*\}");

		rules.Should().NotBeEmpty();
		rules.Select(r => r.Value).Where(r => r.Contains("opacity", StringComparison.Ordinal)).Should().BeEmpty();
	}

	private static string Block(string start)
	{
		int at = _css.IndexOf(start, StringComparison.Ordinal);
		at.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have {0}", start);
		int depth = 0;
		for (int i = at; i < _css.Length; i++)
		{
			depth += _css[i] == '{' ? 1 : _css[i] == '}' ? -1 : 0;
			if (depth == 0 && _css[i] == '}')
			{
				return _css[at..(i + 1)];
			}
		}

		return _css[at..];
	}

	private static string Variable(string scope, string name)
	{
		string value = Regex.Match(Block("\n" + scope), Regex.Escape(name) + @": (#[0-9a-f]{6});").Groups[1].Value;
		value.Should().NotBeEmpty("{0} sets {1}", scope, name);
		return value;
	}

	private static string SearchFunctions()
	{
		int start = _js.IndexOf("function searchSnippet(", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the search code builds snippets in searchSnippet");
		int end = _js.IndexOf("function updateActive()", start, StringComparison.Ordinal);
		return _js[start..end];
	}

	private static string RunNode(string script)
	{
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-theme-" + Guid.NewGuid().ToString("N") + ".js");
		File.WriteAllText(path, script);
		try
		{
			var info = new ProcessStartInfo("node")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};
			info.ArgumentList.Add(path);
			using Process node = Process.Start(info)!;
			Task<string> stdout = node.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
			Task<string> stderr = node.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
			node.WaitForExit(30_000).Should().BeTrue("node should finish");
			node.ExitCode.Should().Be(0, stderr.Result);
			return stdout.Result;
		}
		finally
		{
			File.Delete(path);
		}
	}

	private static double Contrast(string a, string b)
	{
		double la = Luminance(a);
		double lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static double Luminance(string hex)
	{
		double Channel(int offset)
		{
			double c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
			return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}

		return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
	}
}
