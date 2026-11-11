using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Runs the shared sidebar script under Node, when it is on PATH, against a small DOM made from
///     the rendered pages: for every page, the sidebar the script builds must equal the one the
///     layout writes into the page in the inline mode.
/// </summary>
public sealed class SharedSidebarScriptTests
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

	// Just enough DOM for the script: elements parsed from the layout's markup, class lists,
	// attribute access, simple class selectors and replaceChild. Unchanged tags print as they were
	// read, so the result compares as text.
	private const string _dom = """
	                            class Text { constructor(text) { this.text = text; this.parentElement = null; } }
	                            class ClassList {
	                                constructor(el) { this.el = el; }
	                                list() { const v = this.el.getAttribute('class'); return v ? v.split(/\s+/).filter(Boolean) : []; }
	                                contains(name) { return this.list().includes(name); }
	                                add(name) { const l = this.list(); if (!l.includes(name)) { l.push(name); this.el.setAttribute('class', l.join(' ')); } }
	                            }
	                            class El {
	                                constructor(tag, attrs, rawTag, selfClosing) {
	                                    this.tag = tag; this.attrs = attrs; this.rawTag = rawTag; this.selfClosing = selfClosing;
	                                    this.childNodes = []; this.parentElement = null; this.classList = new ClassList(this);
	                                }
	                                get children() { return this.childNodes.filter(n => n instanceof El); }
	                                get parentNode() { return this.parentElement; }
	                                get clientHeight() { return 0; }
	                                getAttribute(name) { const a = this.attrs.find(a => a.name === name); return a ? a.value : null; }
	                                setAttribute(name, value) {
	                                    this.rawTag = null;
	                                    const a = this.attrs.find(a => a.name === name);
	                                    if (a) a.value = String(value); else this.attrs.push({ name, value: String(value) });
	                                }
	                                appendChild(child) { child.parentElement = this; this.childNodes.push(child); }
	                                replaceChild(added, removed) {
	                                    this.childNodes[this.childNodes.indexOf(removed)] = added;
	                                    added.parentElement = this; removed.parentElement = null;
	                                }
	                                contains(other) { for (let e = other; e; e = e.parentElement) if (e === this) return true; return false; }
	                                *walk() { for (const c of this.children) { yield c; yield* c.walk(); } }
	                                matches(selector) {
	                                    const m = /^([a-z]*)((?:\.[\w-]+)*)$/.exec(selector);
	                                    if (!m) throw new Error('unsupported selector ' + selector);
	                                    if (m[1] && m[1] !== this.tag) return false;
	                                    return m[2].split('.').filter(Boolean).every(c => this.classList.contains(c));
	                                }
	                                querySelectorAll(selector) { return [...this.walk()].filter(e => e.matches(selector)); }
	                                querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
	                            }
	                            const decode = s => s.replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
	                            function parse(html) {
	                                const root = new El('#root', [], '', false);
	                                const stack = [root];
	                                const re = /<(\/?)([a-zA-Z][\w-]*)((?:\s+[^\s=>\/]+(?:="[^"]*"|='[^']*')?)*)\s*(\/?)>|([^<]+)/g;
	                                let m;
	                                while ((m = re.exec(html)) !== null) {
	                                    const top = stack[stack.length - 1];
	                                    if (m[5] !== undefined) { const t = new Text(m[5]); t.parentElement = top; top.childNodes.push(t); continue; }
	                                    const tag = m[2].toLowerCase();
	                                    if (m[1]) { while (stack.length > 1 && stack.pop().tag !== tag) { } continue; }
	                                    const attrs = [];
	                                    const ar = /([^\s=>\/]+)(?:="([^"]*)"|='([^']*)')?/g;
	                                    let a;
	                                    while ((a = ar.exec(m[3])) !== null) attrs.push({ name: a[1], value: decode(a[2] || a[3] || '') });
	                                    const el = new El(tag, attrs, m[0], !!m[4]);
	                                    top.appendChild(el);
	                                    if (!m[4]) stack.push(el);
	                                }
	                                return root;
	                            }
	                            function print(node) {
	                                if (node instanceof Text) return node.text;
	                                const open = node.rawTag !== null ? node.rawTag
	                                    : '<' + node.tag + node.attrs.map(a => ' ' + a.name + '="' + a.value.replace(/"/g, '&quot;') + '"').join('') + '>';
	                                return node.selfClosing ? open : open + node.childNodes.map(print).join('') + '</' + node.tag + '>';
	                            }
	                            function runPage(asideHtml) {
	                                const aside = parse(asideHtml).children[0];
	                                const document = {
	                                    currentScript: aside.querySelector('script'),
	                                    createElement() { return { set innerHTML(html) { this.content = parse(html); } }; }
	                                };
	                                new Function('document', SCRIPT)(document);
	                                return print(aside.querySelector('.sidebar-nav'));
	                            }
	                            """;

	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	private static DocPage Page(string route, string title) => new()
	{
		Route = route,
		FrontMatter = new FrontMatter { Title = title },
		Content = new PageContent { Html = "<p>Body</p>" }
	};

	private static NavigationNode Node(string label, string? route, params NavigationNode[] children) =>
		new() { Label = label, Route = route, Children = [.. children] };

	public static TheoryData<string> BasePaths() => ["/", "/Repo"];

	[Theory]
	[MemberData(nameof(BasePaths))]
	public void Script_BuildsTheSidebarTheLayoutWritesForEveryPage(string basePath)
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");

		DocPage[] pages =
		[
			Page("/", "Home"), Page("/guide", "Guide"), Page("/guide/intro", "Intro"), Page("/guide/advanced", "Advanced"),
			Page("/guide/advanced/caching", "Caching"), Page("/guide/advanced/tuning", "Tuning"),
			Page("/reference/cli", "CLI"), Page("/reference/config", "Config"), Page("/start/one", "One"),
			Page("/start/two", "Two"), Page("/api", "API"), Page("/api/contoso/canvas", "Canvas"),
			Page("/api/contoso/brush", "Brush"), Page("/notes/x", "Unlisted")
		];
		var nav = new NavigationTree
		{
			Items =
			[
				Node("Guide", "/guide",
					Node("Intro", "/guide/intro"),
					Node("Advanced", "/guide/advanced", Node("Caching", "/guide/advanced/caching"),
						Node("Tuning", "/guide/advanced/tuning"))) with { Icon = "book-open" },
				// A folder without an index page: a heading, not a link.
				Node("Reference", "/reference", Node("CLI", "/reference/cli"), Node("Config", "/reference/config")) with
				{
					Expanded = false
				},
				// A nav item whose path has no page takes its first child's route.
				Node("Start", "/start/one", Node("One", "/start/one"), Node("Two", "/start/two")),
				Node("API", "/api", Node("Contoso", null, Node("Brush", "/api/contoso/brush"), Node("Canvas", "/api/contoso/canvas")) with
				{
					Expanded = false
				}),
				Node("Home", "/")
			]
		};
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Docs" },
			Build = new BuildConfig { BasePath = basePath }
		};
		ThemeRenderContext Context(string? script) => new()
		{
			Config = config,
			Navigation = nav,
			Templates = EmbeddedThemeProvider.CreateDefault().Templates,
			Partials = [],
			AllPages = pages,
			SidebarScriptUrl = script
		};

		string script = EmbeddedThemeProvider.CreateSidebarScript(
			_engine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(), Context(null), TestContext.Current.CancellationToken));
		ThemeRenderContext inline = Context(null);
		ThemeRenderContext shared = Context("/_theme/js/sidebar.js?v=0123456789");

		var expected = new List<string>();
		var asides = new List<string>();
		foreach (DocPage page in pages)
		{
			string inlinePage = _engine.RenderPage(page, inline, TestContext.Current.CancellationToken);
			string sharedPage = _engine.RenderPage(page, shared, TestContext.Current.CancellationToken);
			expected.Add(Compact(Regex.Match(inlinePage, "<nav class=\"sidebar-nav\".*?</nav>", RegexOptions.Singleline).Value));
			asides.Add(Regex.Match(sharedPage, "<aside class=\"sidebar\".*?</aside>", RegexOptions.Singleline).Value);
		}

		string output = RunNode($$"""
		                          const SCRIPT = {{JsonSerializer.Serialize(script)}};
		                          {{_dom}}
		                          const asides = {{JsonSerializer.Serialize(asides)}};
		                          process.stdout.write(JSON.stringify(asides.map(runPage)));
		                          """);

		List<string> actual = JsonSerializer.Deserialize<List<string>>(output)!;
		for (int i = 0; i < pages.Length; i++)
		{
			actual[i].Should().Be(expected[i], "the page {0} should get the same sidebar either way", pages[i].Route);
		}

		expected.Should().Contain(s => s.Contains("parent-active"), "the tree has pages below the top level");
		expected.Should().Contain(s => s.Contains("<span class=\"nav-icon\">"), "an entry has an icon");
	}

	private static string Compact(string html) => Regex.Replace(html.Trim(), @"\s*\n\s*", "\n");

	private static string RunNode(string script)
	{
		string path = Path.Combine(Path.GetTempPath(), "mokadocs-sidebar-" + Guid.NewGuid().ToString("N") + ".js");
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
}
