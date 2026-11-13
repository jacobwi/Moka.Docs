using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the default theme's code block chrome. The behavior is CSS and script, so these
///     look for the rules that implement it.
/// </summary>
public sealed class EmbeddedThemeCodeBlockTests
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

	private static readonly string _css = ThemeCss.All;
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	[Fact]
	public void LandingCodeBlocks_ShowTheCopyButtonInPlaceOfTheLanguageLabel()
	{
		// On the landing layout the Copy button was always visible and drawn over the label.
		string css = EmbeddedThemeProvider.GetCss();

		Rule(css, ".landing-content pre .copy-btn {").Should().Contain("opacity: 0;");
		css.Should().Contain(".landing-content pre:hover .copy-btn, .landing-content pre .copy-btn:focus-visible { opacity: 1; }");
		css.Should().Contain(".landing-content pre:hover .code-lang, .landing-content pre:focus-within .code-lang { opacity: 0;");
	}

	[Fact]
	public void PageCodeBlocks_ShowTheCopyButtonOnTouchScreens()
	{
		// Without hover the button stayed at opacity 0: invisible, but still under the finger.
		string css = EmbeddedThemeProvider.GetCss();

		css.Should().Contain("@media (hover: none)");
		css.Should().Contain(".page-content pre .copy-btn:focus-visible { opacity: 1; }");
	}

	[Fact]
	public void LineNumbers_AreOnlyAddedWhereTheThemeStylesThem()
	{
		// Every <pre> got line numbers. Where no CSS positioned them they ran into the code as
		// text, as in the landing page's configuration example ("MyLib.csproj12345678").
		EmbeddedThemeProvider.GetJs().Should().Contain("block.closest('.page-content, .landing-content')");
	}

	[Fact]
	public void Script_NoLongerHighlights()
	{
		// The build highlights every block now; the regex table was 10 KB that ran on every page load.
		_js.Should().NotContain("langRules").And.NotContain("function highlight(").And.NotContain("innerHTML = highlight");
	}

	[Fact]
	public void Chrome_SitsInThePreBesideTheScrollingCode()
	{
		// The label, the numbers and the button were inside the scrolling <pre> and scrolled away
		// with a long line. The <pre> is now the frame; the <code> scrolls.
		Rule(_css, ".code-block > pre {").Should().Contain("overflow: hidden;");
		Rule(_css, ".code-block > pre > code {").Should().Contain("overflow-x: auto;").And.Contain("display: grid;");
		_js.Should().Contain("pre.insertBefore(label, code);")
			.And.Contain("pre.insertBefore(gutter, code);")
			.And.Contain("pre.insertBefore(button, code);")
			.And.NotContain("appendChild(btn)");
	}

	[Fact]
	public void Chrome_IsHiddenFromScreenReadersAndTheCodeIsReachableByKeyboard()
	{
		_js.Should().Contain("label.setAttribute('aria-hidden', 'true');")
			.And.Contain("gutter.setAttribute('aria-hidden', 'true');");
		Rule(_css, ".code-block > pre > code:focus-visible {").Should().Contain("outline: 2px solid");
		_css.Should().Contain("content: counter(line) / '';", "numbers drawn by CSS in wrapped blocks get empty alternative text");
	}

	[Fact]
	public void LineNumbers_AreExactlyOneCodeLineHigh()
	{
		// With their own font size and a relative line height the numbers drifted about 2px a line
		// away from the code they belong to.
		Rule(_css, ".code-block > pre > .line-numbers span {").Should().Contain("height: calc(var(--code-font) * var(--code-lh));");
		Rule(_css, ".code-block > pre > code {").Should().Contain("line-height: var(--code-lh);").And.Contain("font-size: var(--code-font);");
		Rule(_css, ".code-block > pre > .line-numbers {").Should().Contain("top: var(--code-top);");
	}

	[Fact]
	public void CopyButton_GuardsTheClipboardAndAnnouncesTheResult()
	{
		// Pages served over plain http have no navigator.clipboard.
		_js.Should().Contain("if (navigator.clipboard && window.isSecureContext)")
			.And.Contain("document.execCommand('copy')")
			.And.Contain("status.setAttribute('role', 'status');")
			.And.Contain("announce(ok ? i18n('code.copied') : i18n('copy.failed'));");
		_css.Should().Contain(".page-content pre .copy-btn:focus-visible,\n.landing-content pre .copy-btn:focus-visible { outline: 2px solid");
	}

	[Fact]
	public void CopyButton_CopiesTheCodeAfterTheChange()
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");

		string result = RunNode($$"""
		                          {{Function("codeText")}}
		                          const line = (classes, text) => ({ classList: { contains: c => classes.split(' ').includes(c) }, textContent: text });
		                          const lines = [line('line', 'keep'), line('line diff-remove', 'old'), line('line diff-add', 'new'),
		                              line('line diff-hunk', '@@ -1 +1 @@'), line('line highlighted focused', 'done'), line('line', '')];
		                          const marked = { textContent: 'unused', querySelectorAll: () => lines };
		                          const plain = { textContent: 'raw\ntext', querySelectorAll: () => [] };
		                          process.stdout.write(JSON.stringify([codeText(marked), codeText(plain)]));
		                          """);

		JsonSerializer.Deserialize<string[]>(result).Should().Equal("keep\nnew\ndone\n", "raw\ntext");
	}

	[Fact]
	public void CodeBlockSwitches_AreReadFromTheRootAndTheBlock()
	{
		_js.Should().Contain("html.hasAttribute('data-no-copy-button')")
			.And.Contain("html.hasAttribute('data-no-line-numbers')")
			.And.Contain("setting === 'true' || (setting !== 'false' && !noNumbers && count >= 3)");
	}

	[Theory]
	[InlineData(".code-block > pre > code > .line.highlighted {", "--sh-line-highlight")]
	[InlineData(".code-block > pre > code > .line.diff-add {", "--sh-diff-add-bg")]
	[InlineData(".code-block > pre > code > .line.diff-remove {", "--sh-diff-remove-bg")]
	[InlineData(".code-block[data-wrap] > pre > code {", "pre-wrap")]
	[InlineData(".code-block > .code-title {", "position: absolute;")]
	public void FenceFeatures_HaveTheirRules(string selector, string declaration)
	{
		Rule(_css, selector).Should().Contain(declaration);
	}

	[Fact]
	public void DiffSigns_ComeFromCssSoTheyArentCopied()
	{
		Rule(_css, ".code-block[data-diff] > pre > code > .line.diff-add::before {").Should().Contain("content: '+';");
		Rule(_css, ".code-block[data-diff] > pre > code > .line.diff-remove::before {").Should().Contain("content: '-';");
	}

	public static TheoryData<string> CodeThemes() =>
	new(global::Moka.Docs.Core.Theming.CodeThemes.Names);

	private const string _lightCodeThemes = "[data-code-theme=\"catppuccin-latte\"],\n[data-code-theme=\"github-light\"],\n[data-code-theme=\"gruvbox-light\"],\n[data-code-theme=\"solarized-light\"],\n[data-code-theme=\"rose-pine-dawn\"] {";

	private static bool IsLight(string codeTheme) => _lightCodeThemes.Contains($"\"{codeTheme}\"", StringComparison.Ordinal);

	private static string CodeBackground(string codeTheme) =>
		Regex.Match(_css, $@"\[data-code-theme=""{codeTheme}""\] :is\(\.page-content, \.landing-content\) pre \{{ background: (#[0-9a-f]{{6}});").Groups[1].Value;

	private static string CodeText(string codeTheme) =>
		Regex.Match(_css, $@"\[data-code-theme=""{codeTheme}""\] :is\(\.page-content, \.landing-content\) pre code \{{ color: (#[0-9a-f]{{6}});").Groups[1].Value;

	// The line and chrome variables a code theme gets: the dark set on :root, the light set for the
	// themes in the light list.
	private static Dictionary<string, string> LineVariables(string codeTheme)
	{
		Dictionary<string, string> lines = Variables(":root {\n    --sh-line-shade");
		if (IsLight(codeTheme))
		{
			foreach ((string name, string value) in Variables(_lightCodeThemes))
			{
				lines[name] = value;
			}
		}

		return lines;
	}

	[Theory]
	[MemberData(nameof(CodeThemes))]
	public void MarkedLines_KeepEveryTokenThatMeetsAaAtAa(string codeTheme)
	{
		// A tint alone lightened dark lines; tokens that just pass on the plain background, such as
		// nord's keywords, fell to about 3.8:1 on a highlighted line.
		bool light = IsLight(codeTheme);
		Dictionary<string, string> theme = Variables($"[data-code-theme=\"{codeTheme}\"] {{");
		Dictionary<string, string> lines = LineVariables(codeTheme);

		string background = CodeBackground(codeTheme);
		string text = CodeText(codeTheme);
		background.Should().NotBeEmpty();
		text.Should().NotBeEmpty();
		string[] presets = Regex.Matches(_css, @"html\[data-color-theme=""[a-z-]+""\] \{\s*--color-primary: (#[0-9a-f]{6}) !important;")
			.Select(m => m.Groups[1].Value).ToArray();
		presets.Should().HaveCount(11);

		var tints = new List<(string Name, double[] Color, double Alpha)>
		{
			("diff-add", Rgba(lines["--sh-diff-add-bg"]).Color, Rgba(lines["--sh-diff-add-bg"]).Alpha),
			("diff-remove", Rgba(lines["--sh-diff-remove-bg"]).Color, Rgba(lines["--sh-diff-remove-bg"]).Alpha)
		};
		double highlight = double.Parse(Regex.Match(lines["--sh-line-highlight"], @"(\d+)%").Groups[1].Value) / 100;
		tints.AddRange(presets.Select(p => ("highlight " + p, Hex(p), highlight)));

		var tokens = theme.Where(v => v.Key != "--sh-line-number" && v.Value.StartsWith('#')).Select(v => (v.Key, v.Value)).ToList();
		tokens.Add(("text", text));
		if (codeTheme == "github-light")
		{
			tokens.RemoveAll(t => t.Key == "--sh-comment");
			tokens.Add(("--sh-comment on a marked line", Regex.Match(_css, @"\[data-code-theme=""github-light""\] \.code-block \.line:is\([^)]*\) \{ --sh-comment: (#[0-9a-f]{6}); \}").Groups[1].Value));
		}

		// Moka darkens the dark code themes to #0a0a0d.
		foreach (string bg in light ? new[] { background } : new[] { background, "#0a0a0d" })
		{
			double[] shaded = lines["--sh-line-shade"] == "transparent" ? Hex(bg) : Over(Rgba(lines["--sh-line-shade"]).Color, Rgba(lines["--sh-line-shade"]).Alpha, Hex(bg));
			foreach ((string tint, double[] color, double alpha) in tints)
			{
				double[] line = Over(color, alpha, shaded);
				foreach ((string name, string value) in tokens.Where(t => Contrast(Hex(t.Value), Hex(bg)) >= 4.5))
				{
					Contrast(Hex(value), line).Should().BeGreaterThanOrEqualTo(4.5, $"{name} on a {tint} line over {bg}");
				}
			}

			Contrast(Hex(lines["--sh-diff-add"]), Over(tints[0].Color, tints[0].Alpha, shaded)).Should().BeGreaterThanOrEqualTo(4.5);
			Contrast(Hex(lines["--sh-diff-remove"]), Over(tints[1].Color, tints[1].Alpha, shaded)).Should().BeGreaterThanOrEqualTo(4.5);
		}
	}

	[Theory]
	[MemberData(nameof(CodeThemes))]
	public void CodeThemes_HaveEveryTokenAndLineNumberAtAa(string codeTheme)
	{
		// Published palettes have comments and line numbers near 2.5:1 (catppuccin-latte most tokens);
		// every theme is shifted to 4.5:1. Line numbers also sit on the VS Code style's gutter tint and
		// on moka's #0a0a0d.
		double[] background = Hex(CodeBackground(codeTheme));
		Dictionary<string, string> theme = Variables($"[data-code-theme=\"{codeTheme}\"] {{");
		theme.Should().ContainKeys("--sh-comment", "--sh-string", "--sh-keyword", "--sh-number", "--sh-type", "--sh-attr", "--sh-tag", "--sh-property", "--sh-line-number");

		foreach ((string name, string value) in theme.Where(v => v.Key != "--sh-line-number").Append(new KeyValuePair<string, string>("text", CodeText(codeTheme))))
		{
			Contrast(Hex(value), background).Should().BeGreaterThanOrEqualTo(4.5, "{0} {1} on {2}", codeTheme, name, CodeBackground(codeTheme));
		}

		// The code title bar is 8% gray over the background.
		Contrast(Hex(theme["--sh-punctuation"]), Over([128 / 255.0, 128 / 255.0, 128 / 255.0], 0.08, background)).Should().BeGreaterThanOrEqualTo(4.5);
		string[] bases = IsLight(codeTheme) ? [CodeBackground(codeTheme)] : [CodeBackground(codeTheme), "#0a0a0d"];
		foreach (string bg in bases)
		{
			Contrast(Hex(theme["--sh-line-number"]), Hex(bg)).Should().BeGreaterThanOrEqualTo(4.5, "line numbers on {0}", bg);
			Contrast(Hex(theme["--sh-line-number"]), Over([128 / 255.0, 128 / 255.0, 128 / 255.0], 0.04, Hex(bg))).Should().BeGreaterThanOrEqualTo(4.5, "line numbers on the VS Code gutter over {0}", bg);
		}
	}

	[Theory]
	[MemberData(nameof(CodeThemes))]
	public void Chrome_LabelAndCopyButtonMeetAaOnEveryCodeTheme(string codeTheme)
	{
		// The language label and the Copy button were white or black at 30 to 45% opacity: about
		// 2.8:1 on the light code themes.
		Dictionary<string, string> lines = LineVariables(codeTheme);
		double[] background = Hex(CodeBackground(codeTheme));

		double[] On(params string[] layers)
		{
			double[] under = background;
			foreach (string layer in layers)
			{
				(double[] c, double a) = Rgba(lines[layer]);
				under = Over(c, a, under);
			}

			return under;
		}

		double[] Text(string variable, double[] under) => lines[variable].StartsWith('#') ? Hex(lines[variable]) : Over(Rgba(lines[variable]).Color, Rgba(lines[variable]).Alpha, under);

		double[] label = On("--sh-chrome-bg");
		double[] copy = On("--sh-copy-bg");
		double[] copyHover = On("--sh-copy-bg-hover");
		Contrast(Text("--sh-chrome", label), label).Should().BeGreaterThanOrEqualTo(4.5, "the language label on {0}", codeTheme);
		Contrast(Text("--sh-chrome", copy), copy).Should().BeGreaterThanOrEqualTo(4.5, "the Copy button on {0}", codeTheme);
		Contrast(Text("--sh-chrome-strong", copyHover), copyHover).Should().BeGreaterThanOrEqualTo(4.5, "the hovered Copy button on {0}", codeTheme);
		Rule(_css, ".page-content pre .copy-btn {").Should().Contain("color: var(--sh-chrome);").And.Contain("background: var(--sh-copy-bg);");
		Rule(_css, ".landing-content pre .copy-btn {").Should().Contain("color: var(--sh-chrome);");
	}

	[Fact]
	public void LandingCodeBlocks_UseTheCodeTheme()
	{
		// Landing blocks kept the page's light secondary background under the code theme's token
		// colors, about 1.5:1 for mocha's strings, and a white Copy button on it.
		_css.Should().Contain("[data-code-theme=\"catppuccin-mocha\"] :is(.page-content, .landing-content) pre { background: #1e1e2e;");
		_css.Should().NotContain(".landing-content pre .line-numbers { color: var(--color-text-secondary); }");
		Rule(_css, "\n.landing-content pre .code-lang { position").Should().Contain("color: var(--sh-chrome);");
	}

	private static Dictionary<string, string> Variables(string blockStart)
	{
		int start = _css.IndexOf(blockStart, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the stylesheet should have {blockStart}");
		string body = _css[_css.IndexOf('{', start)..(_css.IndexOf('}', start) + 1)];
		return Regex.Matches(body, @"(--[\w-]+): ([^;]+);").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim());
	}

	private static (double[] Color, double Alpha) Rgba(string value)
	{
		double[] parts = Regex.Matches(value, @"[\d.]+").Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
		return ([parts[0] / 255, parts[1] / 255, parts[2] / 255], parts[3]);
	}

	private static double[] Hex(string hex) =>
		[Convert.ToInt32(hex[1..3], 16) / 255.0, Convert.ToInt32(hex[3..5], 16) / 255.0, Convert.ToInt32(hex[5..7], 16) / 255.0];

	private static double[] Over(double[] top, double alpha, double[] bottom) =>
		[top[0] * alpha + bottom[0] * (1 - alpha), top[1] * alpha + bottom[1] * (1 - alpha), top[2] * alpha + bottom[2] * (1 - alpha)];

	private static double Contrast(double[] a, double[] b)
	{
		static double Channel(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		static double Luminance(double[] c) => 0.2126 * Channel(c[0]) + 0.7152 * Channel(c[1]) + 0.0722 * Channel(c[2]);
		double la = Luminance(a), lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static string Rule(string css, string selector)
	{
		int start = css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the theme should have a rule for {selector}");
		return css[start..(css.IndexOf('}', start) + 1)];
	}

	private static string Function(string name)
	{
		Match start = Regex.Match(_js, $@"function {name}\(");
		start.Success.Should().BeTrue($"the theme script should define {name}");
		int depth = 0;
		for (int i = _js.IndexOf('{', start.Index); i < _js.Length; i++)
		{
			depth += _js[i] == '{' ? 1 : _js[i] == '}' ? -1 : 0;
			if (depth == 0)
			{
				return _js[start.Index..(i + 1)];
			}
		}

		throw new InvalidOperationException($"{name} has no closing brace");
	}

	private static string RunNode(string script)
	{
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-codeblock-" + Guid.NewGuid().ToString("N") + ".js");
		File.WriteAllText(path, script);
		try
		{
			using Process node = Process.Start(new ProcessStartInfo("node", [path])
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			})!;
			string output = node.StandardOutput.ReadToEnd();
			string errors = node.StandardError.ReadToEnd();
			node.WaitForExit(30_000).Should().BeTrue();
			node.ExitCode.Should().Be(0, errors);
			return output;
		}
		finally
		{
			File.Delete(path);
		}
	}
}
