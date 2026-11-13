using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Behavior of the default theme's client runtime: storage, the Appearance panel, search, the
///     table of contents, layering and the no-JS fallbacks. Parts of main.js run under Node (with a
///     small fake DOM where they need one) when Node is on PATH; the rest looks for the rules that
///     implement a fix.
/// </summary>
public sealed class EmbeddedThemeRuntimeTests
{
	private static readonly string _css = ThemeCss.All;
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	// Elements and text with attributes, class lists, listeners, insertion, an outerHTML and simple selectors
	// (tag, classes and [name="value"], [name$="value"] or [name*="value"], in comma lists). fire() runs document
	// capture listeners, then the target's listeners up the tree, then document bubble listeners.
	private const string _fakeDom = """
	                                const observers = [];
	                                class MutationObserver { constructor(f) { observers.push(f); } observe() {} }
	                                class ClassList {
	                                    constructor(el) { this.el = el; }
	                                    get list() { return (this.el.attrs.class || '').split(/\s+/).filter(Boolean); }
	                                    contains(c) { return this.list.includes(c); }
	                                    toggle(c, force) {
	                                        const on = force === undefined ? !this.contains(c) : !!force;
	                                        const l = this.list.filter(x => x !== c);
	                                        if (on) l.push(c);
	                                        this.el.attrs.class = l.join(' ');
	                                        return on;
	                                    }
	                                    add(c) { this.toggle(c, true); }
	                                    remove(c) { this.toggle(c, false); }
	                                }
	                                function matchOne(el, s) {
	                                    const m = /^([a-z]*)((?:\.[\w-]+)*)((?:\[[\w-]+[$*]?="[^"]*"\])*)$/.exec(s);
	                                    if (!m) throw new Error('unsupported selector ' + s);
	                                    if (!el.attrs) return false;
	                                    if (m[1] && m[1] !== el.tag) return false;
	                                    if (!m[2].split('.').filter(Boolean).every(c => el.classList.contains(c))) return false;
	                                    return [...m[3].matchAll(/\[([\w-]+)([$*]?)="([^"]*)"\]/g)].every(a => {
	                                        const v = el.getAttribute(a[1]);
	                                        return v !== null && (a[2] === '$' ? v.endsWith(a[3]) : a[2] === '*' ? v.includes(a[3]) : v === a[3]);
	                                    });
	                                }
	                                const escapeHtml = (t, attr) => t.replace(/&/g, '&amp;').replace(attr ? /"/g : /[<>]/g, c => ({ '"': '&quot;', '<': '&lt;', '>': '&gt;' })[c]);
	                                class Text {
	                                    constructor(text) { this.text = text; this.parentElement = null; }
	                                    *walk() { }
	                                    matches() { return false; }
	                                    get outerHTML() { return escapeHtml(this.text, false); }
	                                }
	                                class El {
	                                    constructor(tag, attrs, children) {
	                                        this.tag = tag; this.attrs = Object.assign({}, attrs || {}); this.children = []; this.parentElement = null;
	                                        this.listeners = {}; this.classList = new ClassList(this);
	                                        (children || []).forEach(c => { c.parentElement = this; this.children.push(c); });
	                                    }
	                                    get hidden() { return 'hidden' in this.attrs; }
	                                    set hidden(v) { if (v) this.attrs.hidden = ''; else delete this.attrs.hidden; }
	                                    getAttribute(n) { return n in this.attrs ? this.attrs[n] : null; }
	                                    setAttribute(n, v) { this.attrs[n] = String(v); if (this === html) observers.forEach(f => f()); }
	                                    removeAttribute(n) { delete this.attrs[n]; }
	                                    hasAttribute(n) { return n in this.attrs; }
	                                    addEventListener(type, f) { (this.listeners[type] = this.listeners[type] || []).push(f); }
	                                    focus() { document.activeElement = this; }
	                                    contains(o) { for (let e = o; e; e = e.parentElement) if (e === this) return true; return false; }
	                                    *walk() { for (const c of this.children) { yield c; yield* c.walk(); } }
	                                    matches(sel) { return sel.split(',').some(s => matchOne(this, s.trim())); }
	                                    querySelectorAll(sel) { return [...this.walk()].filter(e => e.matches(sel)); }
	                                    querySelector(sel) { return this.querySelectorAll(sel)[0] || null; }
	                                    closest(sel) { for (let e = this; e; e = e.parentElement) if (e.matches(sel)) return e; return null; }
	                                    appendChild(c) { return this.insertBefore(c, null); }
	                                    insertBefore(c, ref) {
	                                        c.parentElement = this;
	                                        this.children.splice(ref ? this.children.indexOf(ref) : this.children.length, 0, c);
	                                        return c;
	                                    }
	                                    after(c) { this.parentElement.insertBefore(c, this.parentElement.children[this.parentElement.children.indexOf(this) + 1] || null); }
	                                    remove() { const p = this.parentElement; if (p) p.children.splice(p.children.indexOf(this), 1); this.parentElement = null; }
	                                    get outerHTML() {
	                                        const attrs = Object.keys(this.attrs).map(n => ' ' + n + '="' + escapeHtml(this.attrs[n], true) + '"').join('');
	                                        return '<' + this.tag + attrs + '>' + this.children.map(c => c.outerHTML).join('') + '</' + this.tag + '>';
	                                    }
	                                }
	                                const h = (tag, attrs, ...children) => new El(tag, attrs, children);
	                                const capture = {}, bubble = {};
	                                const document = {
	                                    activeElement: null,
	                                    querySelector: s => body.querySelector(s),
	                                    querySelectorAll: s => body.querySelectorAll(s),
	                                    createElement: tag => new El(tag, {}, []),
	                                    createTextNode: text => new Text(text),
	                                    addEventListener(type, f, useCapture) { const l = useCapture ? capture : bubble; (l[type] = l[type] || []).push(f); }
	                                };
	                                function fire(el, type, init) {
	                                    const e = Object.assign({ target: el, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }, stopPropagation() {} }, init || {});
	                                    (capture[type] || []).forEach(f => f(e));
	                                    for (let n = el; n; n = n.parentElement) (n.listeners[type] || []).forEach(f => f(e));
	                                    (bubble[type] || []).forEach(f => f(e));
	                                    return e;
	                                }
	                                """;

	private static string Slice(string from, string to)
	{
		int start = _js.IndexOf(from, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "main.js should contain {0}", from);
		int end = _js.IndexOf(to, start + from.Length, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start, "main.js should contain {0} after {1}", to, from);
		return _js[start..end];
	}

	private static string Rule(string selector)
	{
		int start = _css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have a rule for {0}", selector);
		return _css[start..(_css.IndexOf('}', start) + 1)];
	}

	[Fact]
	public void Storage_BlockedByTheBrowser_ReadsAsEmptyAndWritesNothing()
	{
		// A blocked localStorage throws on every access; the first unguarded read stopped the whole
		// runtime, so no button on the page worked.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 const window = { get localStorage() { throw new Error('SecurityError'); } };
		                                 {{Slice("const store = {", "function closest(")}}
		                                 store.set('mokadocs-theme', 'dark');
		                                 store.remove('mokadocs-theme');
		                                 process.stdout.write(JSON.stringify(store.get('mokadocs-theme')));
		                                 """);

		result.Should().Be("null");
	}

	[Fact]
	public void Storage_IsOnlyTouchedThroughTheStoreHelper()
	{
		Regex.Matches(_js, @"localStorage\.").Should().HaveCount(3, "only store.get, store.set and store.remove use it");
		Slice("const store = {", "function closest(").Should().Contain("try { return window.localStorage.getItem(key); } catch (e) { return null; }");
		Regex.Matches(EmbeddedThemeProvider.CreateSidebarScript("<nav class=\"sidebar-nav\"></nav>"), "localStorage").Should().BeEmpty();
	}

	[Theory]
	[InlineData("light", "catppuccin-mocha", "catppuccin-latte")]
	[InlineData("light", "github-dark", "github-light")]
	[InlineData("light", "dracula", "dracula")]
	[InlineData("dark", "catppuccin-latte", "catppuccin-mocha")]
	[InlineData("dark", "github-light", "github-dark")]
	[InlineData("dark", "catppuccin-mocha", "catppuccin-mocha")]
	[InlineData("dark", "nord", "nord")]
	[InlineData("light", "gruvbox-dark", "gruvbox-light")]
	[InlineData("dark", "solarized-light", "solarized-dark")]
	[InlineData("light", "rose-pine", "rose-pine-dawn")]
	[InlineData("dark", "rose-pine-dawn", "rose-pine")]
	[InlineData("light", "tokyo-night", "tokyo-night")]
	public void CodeThemeFor_SwapsOnlyThemesThatHaveACounterpart(string mode, string theme, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 {{Slice("const codeThemePairs", "function initDarkToggle(")}}
		                                 process.stdout.write(codeThemeFor({{JsonSerializer.Serialize(mode)}}, {{JsonSerializer.Serialize(theme)}}));
		                                 """);

		result.Should().Be(expected);
	}

	[Fact]
	public void DarkToggle_StoresTheCodeThemeOnlyWhenTheReaderPickedOne()
	{
		// The toggle used to store the switched code theme on every click, so it looked like the
		// reader's own pick from then on.
		string mode = Slice("function setMode(", "function initDarkToggle(");

		mode.Should().Contain("store.set('mokadocs-theme', mode);");
		Regex.Matches(mode, @"store\.set\('mokadocs-code-theme'").Should().ContainSingle();
		mode.Should().Contain("if (pickedCodeTheme()) store.set('mokadocs-code-theme', code);");
		Slice("function pickedCodeTheme(", "function setMode(").Should()
			.Contain("return document.querySelector('[data-appearance=\"code-theme\"]') ? store.get('mokadocs-code-theme') : null;");
		Slice("function initDarkToggle(", "function followSystemMode(").Should()
			.Contain("setMode(html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');")
			.And.Contain("toggle.setAttribute('aria-pressed',");
	}

	[Fact]
	public void StoredPicks_AreLeftToTheHeadScript()
	{
		// The head script applies a stored pick only while its section is on the page; main.js
		// reading one again would bring back a pick the site turned off.
		foreach (string key in new[] { "mokadocs-style", "mokadocs-color-theme", "mokadocs-code-style", "mokadocs-text-size", "mokadocs-reader-width" })
		{
			_js.Should().NotContain($"store.get('{key}')");
		}

		Regex.Matches(_js, @"store\.get\('mokadocs-code-theme'\)").Should().ContainSingle("only pickedCodeTheme reads it, after checking for the section");
	}

	[Fact]
	public void SystemMode_FollowsTheOperatingSystemUntilTheReaderChoosesAMode()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  let osDark = false;
		                  const changes = [];
		                  const window = { matchMedia: q => ({ get matches() { return osDark && q.includes('dark'); }, addEventListener: (type, f) => changes.push(f) }) };
		                  const html = h('html', { 'data-theme': 'light', 'data-style': 'default' });
		                  const body = h('body', {});
		                  const saved = {};
		                  const store = { get: k => saved[k] || null, set: (k, v) => { saved[k] = v; }, remove: k => { delete saved[k]; } };
		                  {{Slice("function closest(", "function scrollBehavior(")}}
		                  {{Slice("const appearanceListeners = [];", "// Header popups: the version list")}}
		                  followSystemMode();
		                  const seen = [];
		                  const change = dark => { osDark = dark; changes.forEach(f => f()); seen.push(html.getAttribute('data-theme')); };
		                  change(true);
		                  change(false);
		                  setMode('dark');
		                  change(false);
		                  setMode('system', 'nord');
		                  seen.push(html.getAttribute('data-theme') + ' ' + html.getAttribute('data-code-theme') + ' ' + JSON.stringify(saved));
		                  html.setAttribute('data-style', 'moka');
		                  change(false);
		                  process.stdout.write(JSON.stringify(seen));
		                  """;

		JsonSerializer.Deserialize<string[]>(NodeScript.Run(script)).Should().Equal(
			"dark", "light", "dark", "light nord {}", "dark");
	}

	[Fact]
	public void AppearancePanel_IsADialogOfRadioGroupsThatApplyAtOnce()
	{
		// Arrows move and choose with a roving tabindex; Escape closes and returns focus; a click
		// outside, the version menu or focus leaving the panel closes it; Reset removes every stored
		// key and goes back to the site's own values.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  const radio = value => h('button', { role: 'radio', 'aria-checked': 'false', tabindex: '-1', 'data-value': value });
		                  const group = (name, values) => h('div', { class: 'appearance-options', role: 'radiogroup', 'data-appearance': name }, ...values.map(radio));
		                  const html = h('html', { 'data-theme': 'light', 'data-style': 'default', 'data-color-theme': 'ocean', 'data-code-theme': 'catppuccin-latte', 'data-code-style': 'plain' });
		                  const toggle = h('button', { class: 'theme-toggle', 'aria-pressed': 'false' });
		                  const outside = h('p', {});
		                  const body = h('body', {},
		                      h('div', { class: 'version-selector' },
		                          h('button', { class: 'version-trigger', 'aria-expanded': 'false' }),
		                          h('ul', { class: 'version-dropdown', hidden: '' }, h('li', {}, h('a', { class: 'version-option', href: '/v2/' })))),
		                      h('div', { class: 'appearance' },
		                          h('button', { class: 'appearance-trigger', 'aria-expanded': 'false' }),
		                          h('div', { class: 'appearance-panel', hidden: '', 'data-default-style': 'paper', 'data-default-color-theme': 'teal', 'data-default-code-theme': 'nord', 'data-default-code-style': 'macos' },
		                              h('button', { class: 'appearance-close' }),
		                              group('mode', ['light', 'dark', 'system']),
		                              group('style', ['default', 'moka', 'paper']),
		                              group('color-theme', ['ocean', 'teal']),
		                              group('code-theme', ['catppuccin-mocha', 'catppuccin-latte', 'nord']),
		                              group('text-size', ['90', '100', '112.5', '125']),
		                              group('reader-width', ['narrow', 'default', 'wide']),
		                              h('button', { class: 'appearance-reset' }))),
		                      toggle,
		                      outside);
		                  const saved = {};
		                  const store = { get: k => saved[k] || null, set: (k, v) => { saved[k] = v; }, remove: k => { delete saved[k]; } };
		                  const window = { matchMedia: () => ({ matches: false }) };
		                  {{Slice("function closest(", "function scrollBehavior(")}}
		                  {{Slice("const appearanceListeners = [];", "// Mobile nav toggle and the")}}
		                  // The page links the stylesheets of the styles picked here, so they apply at once.
		                  ['moka', 'paper'].forEach(s => body.appendChild(h('link', { rel: 'stylesheet', href: '/_theme/css/style-' + s + '.css?v=' + styleFiles[s] })));
		                  const sidebar = null, mobileToggle = null, tocSidebar = null, tocToggle = null, search = null;
		                  const imageZoom = { close: () => false };
		                  function setNavOpen() { return false; }
		                  function setTocOpen() { return false; }
		                  initDarkToggle(); followSystemMode(); initAppearance(); initVersionMenu();
		                  {{Slice("// Capture phase: a click that", "window.addEventListener('scroll'")}}
		                  const q = s => body.querySelector(s);
		                  const option = (name, value) => q('[data-appearance="' + name + '"]').querySelector('[data-value="' + value + '"]');
		                  const checked = name => {
		                      const radios = q('[data-appearance="' + name + '"]').querySelectorAll('[role="radio"]');
		                      const pick = attr => radios.filter(r => attr(r)).map(r => r.getAttribute('data-value')).join('');
		                      return pick(r => r.getAttribute('aria-checked') === 'true') + '/' + pick(r => r.getAttribute('tabindex') === '0');
		                  };
		                  const state = () => ['mode', 'style', 'color-theme', 'code-theme', 'text-size', 'reader-width'].map(checked).join(' ');
		                  const attrs = () => ['data-theme', 'data-style', 'data-color-theme', 'data-code-theme', 'data-code-style', 'data-text-size', 'data-reader-width'].map(a => html.getAttribute(a) || '-').join(' ');
		                  const keys = () => Object.keys(saved).map(k => k.replace('mokadocs-', '') + '=' + saved[k]).join(',');
		                  const focused = () => document.activeElement ? (document.activeElement.getAttribute('data-value') || document.activeElement.attrs.class) : 'none';
		                  const open = () => (q('.appearance-panel').hidden ? 'closed' : 'open') + ' ' + q('.appearance-trigger').getAttribute('aria-expanded');
		                  const click = s => { const el = typeof s === 'string' ? q(s) : s; el.focus(); fire(el, 'click'); };
		                  const key = k => fire(document.activeElement, 'keydown', { key: k });
		                  const seen = [state()];

		                  click('.appearance-trigger');
		                  seen.push(open() + ' ' + focused());
		                  key('ArrowRight');
		                  seen.push(attrs() + ' | ' + focused() + ' | ' + keys() + ' | ' + checked('mode'));
		                  key('ArrowLeft');
		                  seen.push(attrs() + ' | ' + focused() + ' | ' + keys() + ' | ' + checked('mode'));
		                  q('[data-appearance="style"]').querySelector('[tabindex="0"]').focus();
		                  key('End');
		                  seen.push(attrs() + ' | ' + focused() + ' | ' + keys());
		                  key('Home');
		                  key('ArrowDown');
		                  seen.push(attrs() + ' | ' + focused() + ' | ' + keys());
		                  click(option('code-theme', 'catppuccin-mocha'));
		                  seen.push(attrs() + ' | ' + keys() + ' | ' + checked('code-theme'));
		                  click(toggle);
		                  seen.push(attrs() + ' | ' + toggle.getAttribute('aria-pressed') + ' | ' + open() + ' | ' + checked('mode') + ' | ' + keys());
		                  click(option('text-size', '125'));
		                  seen.push(attrs());
		                  click(option('text-size', '100'));
		                  click(option('reader-width', 'wide'));
		                  seen.push(attrs() + ' | ' + keys() + ' | ' + checked('text-size') + ' ' + checked('reader-width'));
		                  click('.appearance-trigger');
		                  fire(document.activeElement, 'keydown', { key: 'Escape' });
		                  seen.push(open() + ' ' + focused());
		                  click('.appearance-trigger');
		                  click('.version-trigger');
		                  seen.push(open() + ' ' + q('.version-trigger').getAttribute('aria-expanded'));
		                  click(outside);
		                  click('.appearance-trigger');
		                  fire(document.activeElement, 'focusout', { relatedTarget: q('.appearance-trigger') });
		                  seen.push(open());
		                  fire(document.activeElement, 'focusout', { relatedTarget: toggle });
		                  seen.push(open());
		                  click('.appearance-trigger');
		                  click('.appearance-reset');
		                  seen.push(attrs() + ' | ' + keys() + ' | ' + state() + ' | ' + open());
		                  click('.appearance-close');
		                  seen.push(open() + ' ' + focused());
		                  process.stdout.write(JSON.stringify(seen));
		                  """;

		JsonSerializer.Deserialize<string[]>(NodeScript.Run(script)).Should().Equal(
			"system/system default/default ocean/ocean catppuccin-latte/catppuccin-latte 100/100 default/default",
			"open true system",
			"light default ocean catppuccin-latte plain - - | light | theme=light | light/light",
			"light default ocean nord plain - - | system |  | system/system",
			"light paper ocean nord plain - - | paper | style=paper",
			"dark moka ocean nord plain - - | moka | style=moka",
			"dark moka ocean catppuccin-mocha plain - - | style=moka,code-theme=catppuccin-mocha | catppuccin-mocha/catppuccin-mocha",
			"light moka ocean catppuccin-latte plain - - | false | closed false | light/light | style=moka,code-theme=catppuccin-latte,theme=light",
			"light moka ocean catppuccin-latte plain 125 -",
			"light moka ocean catppuccin-latte plain - wide | style=moka,code-theme=catppuccin-latte,theme=light,text-size=100,reader-width=wide | 100/100 wide/wide",
			"closed false appearance-trigger",
			"closed false true",
			"open true",
			"closed false",
			"light paper teal nord macos - - |  | system/system paper/paper teal/teal nord/nord 100/100 default/default | open true",
			"closed false appearance-trigger");
	}

	// A panel with every section the runtime fills in, and main.css on the page.
	private const string _emptyPanel = """
	                                   const group = (name, ...children) => h('div', { class: 'appearance-options', role: 'radiogroup', 'data-appearance': name }, ...children);
	                                   const label = text => h('span', { class: 'appearance-group-label' }, new Text(text));
	                                   const html = h('html', { 'data-theme': 'light', 'data-style': 'default', 'data-base-path': '/Repo', 'data-color-theme': 'ocean', 'data-code-theme': 'nord', 'data-code-style': 'plain' });
	                                   const mainCss = h('link', { rel: 'stylesheet', href: '/Repo/_theme/css/main.css?v=0123456789' });
	                                   const layoutsCss = h('link', { rel: 'stylesheet', href: '/Repo/_theme/css/layouts.css?v=0123456789' });
	                                   const body = h('body', {},
	                                       mainCss,
	                                       layoutsCss,
	                                       h('div', { class: 'appearance' },
	                                           h('button', { class: 'appearance-trigger', 'aria-expanded': 'false' }),
	                                           h('div', { class: 'appearance-panel', hidden: '', 'data-default-style': 'default', 'data-default-color-theme': 'ocean', 'data-default-code-theme': 'nord', 'data-default-code-style': 'plain' },
	                                               h('button', { class: 'appearance-close' }),
	                                               group('style'),
	                                               group('color-theme'),
	                                               group('code-theme', label('Dark'), label('Light')),
	                                               group('code-style'),
	                                               h('button', { class: 'appearance-reset' }))));
	                                   const saved = {};
	                                   const store = { get: k => saved[k] || null, set: (k, v) => { saved[k] = v; }, remove: k => { delete saved[k]; } };
	                                   const window = { matchMedia: () => ({ matches: false }) };
	                                   """;

	[Fact]
	public void AppearancePanel_BuildsTheOptionListsWhenItFirstOpens()
	{
		// Pages used to carry the markup of every style, color, code theme and code style. The panel builds the
		// same buttons from the lists in main.js the first time it opens.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  {{_emptyPanel}}
		                  {{Slice("function closest(", "function scrollBehavior(")}}
		                  {{Slice("const appearanceListeners = [];", "// Mobile nav toggle and the")}}
		                  initAppearance();
		                  const markup = name => body.querySelector('[data-appearance="' + name + '"]').children.map(c => c.outerHTML).join('');
		                  const before = ['style', 'color-theme', 'code-theme', 'code-style'].map(markup);
		                  fire(body.querySelector('.appearance-trigger'), 'click');
		                  const after = ['style', 'color-theme', 'code-theme', 'code-style'].map(markup);
		                  fire(body.querySelector('.appearance-trigger'), 'click');
		                  fire(body.querySelector('.appearance-trigger'), 'click');
		                  const again = ['style', 'color-theme', 'code-theme', 'code-style'].map(markup);
		                  process.stdout.write(JSON.stringify({ before, after, again, focused: document.activeElement.getAttribute('data-value') }));
		                  """;

		using JsonDocument result = JsonDocument.Parse(NodeScript.Run(script));
		string[] before = result.RootElement.GetProperty("before").EnumerateArray().Select(e => e.GetString()!).ToArray();
		string[] after = result.RootElement.GetProperty("after").EnumerateArray().Select(e => e.GetString()!).ToArray();
		string[] again = result.RootElement.GetProperty("again").EnumerateArray().Select(e => e.GetString()!).ToArray();

		before.Should().Equal("", "", "<span class=\"appearance-group-label\">Dark</span><span class=\"appearance-group-label\">Light</span>", "");
		again.Should().Equal(after, "the lists are built once");
		result.RootElement.GetProperty("focused").GetString().Should().Be("default", "focus goes to the checked style");

		static string Radio(string className, string value, string content) =>
			$"<button class=\"{className}\" type=\"button\" role=\"radio\" aria-checked=\"false\" tabindex=\"-1\" data-value=\"{value}\">{content}</button>";

		static string Unchecked(string markup) =>
			markup.Replace("aria-checked=\"true\" tabindex=\"0\"", "aria-checked=\"false\" tabindex=\"-1\"", StringComparison.Ordinal);

		static string Text(string text) => text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

		string CodeTheme(CodeThemeInfo t) => Radio("appearance-option appearance-code-theme", t.Name,
			$"<span class=\"appearance-code-preview\" aria-hidden=\"true\" style=\"background:{t.Background}\"></span><span>{Text(t.Label)}</span>");

		Unchecked(after[0]).Should().Be(string.Concat(ThemeStyles.All.Select(s => Radio("appearance-option appearance-style", s.Name,
			$"<span class=\"style-preview style-preview-{s.Name}\" aria-hidden=\"true\"></span><span class=\"appearance-style-text\"><span class=\"appearance-style-name\">{Text(s.Label)}</span><span class=\"appearance-style-desc\">{Text(s.Description)}</span></span>"))));
		Unchecked(after[1]).Should().Be(string.Concat(ColorPresets.All.Select(p => Radio("appearance-option appearance-swatch", p.Name,
			$"<span class=\"appearance-swatch-dot\" aria-hidden=\"true\" style=\"background:{p.Swatch}\"></span><span class=\"appearance-swatch-label\">{Text(p.Label)}</span>"))));
		Unchecked(after[2]).Should().Be("<span class=\"appearance-group-label\">Dark</span>"
		                                + string.Concat(CodeThemes.All.Where(t => t.IsDark).Select(CodeTheme))
		                                + "<span class=\"appearance-group-label\">Light</span>"
		                                + string.Concat(CodeThemes.All.Where(t => !t.IsDark).Select(CodeTheme)));
		Unchecked(after[3]).Should().Be(string.Concat(CodeStyles.All.Select(s => Radio("appearance-option", s.Name, Text(s.Label)))));
		Regex.Matches(after[0] + after[1] + after[2] + after[3], "aria-checked=\"true\" tabindex=\"0\"").Should().HaveCount(4, "each list checks the current value");
	}

	[Fact]
	public void AppearancePanel_SwitchesToAStyleOnlyOnceItsStylesheetHasLoaded()
	{
		// A style's rules are a file of their own. Switching data-style before it loaded showed the page without them.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  {{_emptyPanel}}
		                  {{Slice("function closest(", "function scrollBehavior(")}}
		                  {{Slice("const appearanceListeners = [];", "// Mobile nav toggle and the")}}
		                  initAppearance();
		                  fire(body.querySelector('.appearance-trigger'), 'click');
		                  const radio = v => body.querySelector('[data-appearance="style"]').querySelector('[data-value="' + v + '"]');
		                  const links = () => body.children.filter(c => c.tag === 'link').map(l => l.getAttribute('href').replace(/\?v=\w+/, '?v=hash')).join(' ');
		                  const state = () => html.getAttribute('data-style') + ' ' + html.getAttribute('data-theme') + ' ' + radio('moka').getAttribute('aria-checked') + ' ' + radio('soft').getAttribute('aria-checked') + ' ' + JSON.stringify(saved);
		                  const seen = [];
		                  fire(radio('moka'), 'click');
		                  seen.push(state(), links());
		                  const moka = body.children[1];
		                  fire(moka, 'load');
		                  seen.push(state());
		                  fire(radio('soft'), 'click');
		                  const soft = body.children[1];
		                  fire(radio('default'), 'click');
		                  seen.push(state());
		                  fire(soft, 'load');
		                  seen.push(state());
		                  fire(radio('moka'), 'click');
		                  seen.push(state(), links());
		                  fire(radio('brutal'), 'click');
		                  fire(body.children[1], 'error');
		                  seen.push(state(), links());
		                  process.stdout.write(JSON.stringify(seen));
		                  """;

		JsonSerializer.Deserialize<string[]>(NodeScript.Run(script)).Should().Equal(
			"default light true false {}",
			"/Repo/_theme/css/main.css?v=hash /Repo/_theme/css/style-moka.css?v=hash /Repo/_theme/css/layouts.css?v=hash",
			"moka dark true false {\"mokadocs-style\":\"moka\"}",
			"default light false false {\"mokadocs-style\":\"default\"}",
			"default light false false {\"mokadocs-style\":\"default\"}",
			"moka dark true false {\"mokadocs-style\":\"moka\"}",
			"/Repo/_theme/css/main.css?v=hash /Repo/_theme/css/style-soft.css?v=hash /Repo/_theme/css/style-moka.css?v=hash /Repo/_theme/css/layouts.css?v=hash",
			"moka dark true false {\"mokadocs-style\":\"moka\"}",
			"/Repo/_theme/css/main.css?v=hash /Repo/_theme/css/style-soft.css?v=hash /Repo/_theme/css/style-moka.css?v=hash /Repo/_theme/css/layouts.css?v=hash");
	}

	private static string SearchCore() => Slice("function prepareSearchIndex(", "function initSearch(");

	private static string[] Search(string query, string entries)
	{
		string result = NodeScript.Run($$"""
		                                 {{SearchCore()}}
		                                 const index = prepareSearchIndex({{entries}});
		                                 process.stdout.write(JSON.stringify(searchIndex(index, {{JsonSerializer.Serialize(query)}}).map(r => r.entry.r + ':' + r.score + ':' + r.typos)));
		                                 """);
		return JsonSerializer.Deserialize<string[]>(result)!;
	}

	private const string _entries = """
	                                [
	                                    { t: 'Configuration', s: '', r: '/config', c: 'Every option of mokadocs.yaml.', g: 'Documentation' },
	                                    { t: 'Guide', s: 'Install', r: '/guide#install', c: 'Run the tool.', x: 'dotnet tool install', g: 'Documentation' },
	                                    { t: 'Themes', s: 'Colors', r: '/themes#colors', c: 'Pick a color preset.', k: 'palette', g: 'Documentation' },
	                                    { t: 'Other', s: '', r: '/other', c: 'Nothing here.', g: 'Documentation' }
	                                ]
	                                """;

	[Fact]
	public void Search_MatchesWordsThatOnlyAppearInCode()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Search("DOTNET", _entries).Should().Equal("/guide#install:1:0");
	}

	[Fact]
	public void Search_KeepsTheFieldWeightsForExactMatches()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Search("colors", _entries).Should().Equal("/themes#colors:5:0");
		Search("palette", _entries).Should().Equal("/themes#colors:3:0");
		Search("config", _entries).Should().Equal("/config:10:0");
	}

	[Theory]
	[InlineData("confgiuration", "/config:10:1")]
	[InlineData("configuraton", "/config:10:1")]
	[InlineData("confuguration", "/config:10:1")]
	[InlineData("confi", "/config:10:0")]
	[InlineData("confgi", "/config:10:1")]
	[InlineData("palete", "/themes#colors:3:1")]
	[InlineData("insatll", "/guide#install:6:1")]
	public void Search_ToleratesOneTypoInATermOfFiveLettersOrMore(string query, string expected)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Search(query, _entries).Should().Equal(expected);
	}

	[Theory]
	[InlineData("tolo")]
	[InlineData("isntlal")]
	public void Search_ShortTermsAndTwoTyposDoNotMatchFuzzily(string query)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		Search(query, _entries).Should().BeEmpty();
		Search("tool", _entries).Should().Equal("/guide#install:1:0");
	}

	[Fact]
	public void Search_RanksEveryExactMatchAboveTypoMatches()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		// "colour" is exact in the content of /b (1 point) and one typo from the title of /a (10).
		string[] results = Search("colour", """
		                                    [
		                                        { t: 'Colors', s: '', r: '/a', c: '', g: 'Documentation' },
		                                        { t: 'Other', s: '', r: '/b', c: 'British colour names.', g: 'Documentation' }
		                                    ]
		                                    """);

		results.Should().Equal("/b:1:0", "/a:10:1");
	}

	[Fact]
	public void Search_HighlightsTheWordATypoMatched()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 {{SearchCore()}}
		                                 const index = prepareSearchIndex([{ t: 'Themes', s: '', r: '/t', c: 'Pick a palette here.', g: 'Documentation' }]);
		                                 process.stdout.write(JSON.stringify(searchIndex(index, 'palete').map(renderSearchResult)[0]));
		                                 """);

		JsonSerializer.Deserialize<string>(result).Should()
			.Contain("<mark>palette</mark>")
			.And.Contain("id=\"search-result-0\" role=\"option\" aria-selected=\"false\" tabindex=\"-1\" href=\"/t\"");
	}

	[Fact]
	public void SearchDialog_ShowsLoadingAndRunsTheQueryWhenTheIndexArrives()
	{
		// It showed "No results found." while the index loaded and never ran the query again.
		string search = Slice("function initSearch(", "// Sidebar nav expand/collapse");

		search.Should().Contain("show(i18n('search.loading'), i18n('search.loading'));");
		search.Should().MatchRegex(@"loading = false;\s*if \(!modal\.hidden\) run\(\);");
		search.Should().Contain("timer = setTimeout(run, 80);", "input is debounced");
		search.Should().Contain("if (closest(e.target, '.search-result')) close(false);", "any result closes the dialog");
		search.Should().Contain("input.select();", "the last query is kept and selected");
		search.Should().NotContain("input.value = ''");
		search.Should().Contain("if (e.key === 'Tab') trapFocus(e, dialog);");
		search.Should().Contain("input.setAttribute('aria-activedescendant', items[activeIdx].id);");
		search.Should().Contain("e.key.toLowerCase() !== 'k'");
		search.Should().Contain("kbd.textContent = apple ? '\\u2318K' : i18n('search.shortcut');");
		Regex.Matches(Slice("function searchIndex(", "function renderSearchResult("), "toLowerCase").Should()
			.ContainSingle("the index is lowercased once when it arrives; a search lowercases only the query");
	}

	[Fact]
	public void Escape_ClosesTheTopmostLayerAndReturnsFocus()
	{
		Slice("// Escape closes the topmost thing", "window.addEventListener('scroll'").Should().Contain(
			"if (imageZoom.close(true) || (search && search.close(true)) || closeOpenPopup() || setTocOpen(false, true) || setNavOpen(false, true)) {");
	}

	[Fact]
	public void OutsideClicks_CloseDrawersAndMenusInTheCapturePhase()
	{
		// The pickers stopped their clicks from bubbling, so the drawer's bubble-phase listener
		// never saw them and their menus opened under the open drawer.
		string listener = Slice("// Capture phase: a click that", "// Escape closes the topmost thing");

		listener.Should().Contain("}, true);");
		listener.Should().Contain("if (sidebar && !sidebar.contains(target) && !(mobileToggle && mobileToggle.contains(target))) setNavOpen(false);");
		listener.Should().Contain("if (tocSidebar && !tocSidebar.contains(target) && !(tocToggle && tocToggle.contains(target))) setTocOpen(false);");
	}

	[Fact]
	public void MobileSidebarAndTocDrawer_CloseEachOtherAndManageFocus()
	{
		string drawers = Slice("function setNavOpen(", "function initMobileNav(");

		drawers.Should().Contain("if (open) setTocOpen(false);").And.Contain("if (open) setNavOpen(false);");
		drawers.Should().Contain("mobileToggle.setAttribute('aria-expanded', open ? 'true' : 'false');");
		drawers.Should().Contain("tocToggle.setAttribute('aria-expanded', open ? 'true' : 'false');");
		drawers.Should().Contain("mobileToggle.focus();").And.Contain("tocToggle.focus();");
		Slice("function initMobileNav(", "function initTocDrawer(").Should().Contain("navBackdrop = createBackdrop('nav-backdrop', mobileToggle);");
	}

	public static TheoryData<string, string> ZIndexRules() => new()
	{
		{ ".site-header {", "--z-header" },
		{ ".appearance-panel {", "--z-popover" },
		{ ".version-dropdown {", "--z-popover" },
		{ ".search-modal { position: fixed;", "--z-modal" },
		{ ".lightbox {", "--z-modal" },
		{ ".back-to-top {", "--z-fab" }
	};

	[Theory]
	[MemberData(nameof(ZIndexRules))]
	public void Layers_UseTheZIndexScale(string selector, string variable)
	{
		Rule(selector).Should().Contain("z-index: var(" + variable + ")");
	}

	[Fact]
	public void Layers_PutTheHeaderAboveTheDrawersAndTheModalsAboveAll()
	{
		// The "On this page" drawer had the header's z-index and came later in the page, so header
		// menus opened under it between 769 and 1200 pixels.
		string root = Regex.Match(_css, @":root \{[^}]*--z-fab[^}]*\}").Value;
		int Z(string name) => int.Parse(Regex.Match(root, "--z-" + name + @": (\d+);").Groups[1].Value, CultureInfo.InvariantCulture);

		Z("fab").Should().BeLessThan(Z("backdrop"));
		Z("backdrop").Should().BeLessThan(Z("drawer"));
		Z("drawer").Should().BeLessThan(Z("header"));
		Z("header").Should().BeLessThan(Z("popover"));
		Z("popover").Should().BeLessThan(Z("modal"));
		Regex.Match(_css, @"@media \(max-width: 1200px\) \{\s*\.toc-sidebar \{[^}]*\}").Value.Should().Contain("z-index: var(--z-drawer);");
		Regex.Match(_css, @"@media \(max-width: 768px\) \{\s*\.sidebar \{[^}]*\}").Value.Should().Contain("z-index: var(--z-drawer);");
	}

	[Fact]
	public void TocSpy_UsesTheHeaderOffsetAndTheBottomOfThePage()
	{
		// The band was a fixed -80px from the top (wrong under every taller navbar variant) and the
		// bottom 80% of the viewport, which left nothing under 400 pixels tall; the last headings
		// of a page never became current.
		string spy = Slice("function initTocSpy(", "// Back to the top of the page.");

		_js.Should().NotContain("rootMargin");
		spy.Should().Contain("const line = headerOffset() + 32;");
		spy.Should().Contain("if (atBottom && lastOnScreen) active = lastOnScreen;");
		Slice("function headerOffset(", "// One requestAnimationFrame pass").Should().Contain("height:var(--header-offset,0px)");
	}

	[Fact]
	public void ScrollWork_RunsInOneThrottledFrameAndAnimatesTheProgressBarWithATransform()
	{
		Regex.Matches(_js, @"addEventListener\('scroll'").Should().ContainSingle();
		_js.Should().Contain("window.addEventListener('scroll', queueFrame, { passive: true });");
		_js.Should().Contain("progress.style.transform = 'scaleX(' + ratio + ')';").And.NotContain("style.width =");
		Rule(".toc-progress-bar {").Should().Contain("transform: scaleX(0);").And.Contain("transition: transform").And.NotContain("transition: width");
		// Autohide reads the header's height in the read pass, before anything writes.
		Slice("function initAutohide(", "// The tabs row").Should().MatchRegex(@"onFrame\(function\(state\) \{[^}]*header\.offsetHeight");
	}

	[Fact]
	public void SmoothScrolling_RespectsReducedMotionAndAnimationsOff()
	{
		Regex.Matches(_js, "'smooth'").Should().ContainSingle("only scrollBehavior() asks for it");
		Slice("function scrollBehavior(", "// html has scroll-behavior").Should()
			.Contain("html.hasAttribute('data-no-animations') || mediaMatches('(prefers-reduced-motion: reduce)') ? 'auto' : 'smooth'");
		_css.Should().Contain("html[data-no-animations] { scroll-behavior: auto; }");
	}

	[Fact]
	public void TocCarets_CollapseSubsectionsFromTheirOwnButton()
	{
		// Clicking the link of an entry with subsections used to collapse them as well as navigate.
		string carets = Slice("function initTocCarets(", "function tocTarget(");

		carets.Should().Contain("const list = item.querySelector(':scope > ul');");
		carets.Should().Contain("caret.setAttribute('aria-expanded', 'true');").And.Contain("caret.setAttribute('aria-controls', list.id);");
		carets.Should().Contain("const collapsed = caret.parentElement.classList.toggle('collapsed');");
		_css.Should().NotContain(".toc-item.has-children > a::after");
		Rule(".toc-item.has-children.collapsed > ul {").Should().Contain("display: none;");
	}

	[Fact]
	public void Tabs_HaveTabRolesKeysAndSyncedRememberedLabels()
	{
		string tabs = Slice("const tabHeaderSelector", "// ToC: a caret button");

		tabs.Should().Contain("header.setAttribute('aria-controls', panel.id);").And.Contain("panel.setAttribute('aria-labelledby', header.id);");
		tabs.Should().Contain("header.tabIndex = i === index ? 0 : -1;");
		tabs.Should().Contain("e.key === 'ArrowRight'").And.Contain("e.key === 'ArrowLeft'").And.Contain("e.key === 'Home'").And.Contain("e.key === 'End'");
		tabs.Should().Contain("tabHeaders(other).map(tabLabel).indexOf(label)");
		tabs.Should().Contain("store.set('mokadocs-tabs',");
	}

	[Fact]
	public void HeadingAnchors_FollowTheTextAndCopyOnlyWhereTheClipboardExists()
	{
		string anchors = Slice("function copyText(", "// Tabs and code groups.");

		anchors.Should().Contain("anchor.setAttribute('aria-label', i18n('heading.anchor', { heading: heading.textContent.trim() }));");
		anchors.Should().Contain("heading.appendChild(anchor);").And.NotContain("heading.prepend(");
		anchors.Should().Contain("if (navigator.clipboard && window.isSecureContext) {");
		anchors.IndexOf("history.replaceState(null, '', hash);", StringComparison.Ordinal).Should()
			.BeLessThan(anchors.IndexOf("copyText(window.location.origin", StringComparison.Ordinal), "the hash changes first");
		_css.Should().MatchRegex(@"\.heading-anchor:focus-visible \{ outline: 2px solid");
		_css.Should().Contain(".page-content h4:hover .heading-anchor,\n.heading-anchor:focus-visible { opacity: 1; }");
	}

	[Fact]
	public void Mermaid_IsPinnedWithSubresourceIntegrity()
	{
		string mermaid = Slice("function initMermaid(", "// Feedback widget");

		mermaid.Should().MatchRegex(@"script\.src = 'https://cdn\.jsdelivr\.net/npm/mermaid@\d+\.\d+\.\d+/dist/mermaid\.min\.js';");
		mermaid.Should().MatchRegex(@"script\.integrity = 'sha384-[A-Za-z0-9+/]{64}';");
		mermaid.Should().Contain("script.crossOrigin = 'anonymous';");
	}

	[Fact]
	public void Mermaid_MeasuresWithoutTransitions()
	{
		// The reduced-motion rules give every element a 0.01ms transition. Mermaid measured its nodes
		// mid-transition and laid flowcharts out up to 16,000px wide, then shrank them to fit.
		int noAnimations = _css.IndexOf("[data-no-animations] *, [data-no-animations] *::before", StringComparison.Ordinal);
		int reducedMotion = _css.IndexOf("@media (prefers-reduced-motion: reduce) {\n    *, *::before, *::after {", StringComparison.Ordinal);
		int mermaidRule = _css.IndexOf(".mermaid, .mermaid *, .mermaid *::before, .mermaid *::after { transition-duration: 0s !important; }", StringComparison.Ordinal);

		noAnimations.Should().BeGreaterThan(0);
		reducedMotion.Should().BeGreaterThan(0);
		mermaidRule.Should().BeGreaterThan(noAnimations, "the later rule wins at equal specificity");
		mermaidRule.Should().BeGreaterThan(reducedMotion);
	}

	[Fact]
	public void Mermaid_DiagramsStayLegibleOnNarrowScreens()
	{
		string mermaid = Slice("function initMermaid(", "// Feedback widget");

		mermaid.Should().Contain("gantt: { useWidth: width }", "gantt charts are 300px wide without it");
		mermaid.Should().Contain("mermaid.run({ nodes: diagrams }).then(keepDiagramsLegible");
		mermaid.Should().Contain("svg.style.minWidth = Math.round(box.width * 0.6) + 'px';");
		_css.Should().Contain(".mermaid > svg { flex: none; margin-inline: auto; }");
		_css.Should().NotMatchRegex(@"\.mermaid \{[^}]*justify-content: center", "centering with justify-content cuts off the left of a wide diagram");
	}

	[Fact]
	public void CollapsedSidebarSections_AreHiddenAfterTheirTransition()
	{
		string closed = Rule(".nav-children:not(.expanded) {");

		closed.Should().Contain("visibility: hidden;").And.Contain("visibility 0s linear 0.3s");
		Regex.Matches(_css, @"\.nav-children\.expanded \{[^}]*\}").Select(m => m.Value).Should().AllSatisfy(r => r.Should().NotContain("visibility"));
	}

	[Fact]
	public void BackToTop_IsNoTabStopWhileHidden()
	{
		Regex.Match(_css, @"\n\s*\.back-to-top \{\s*opacity: 0 !important;[^}]*\}").Value.Should().Contain("visibility: hidden;");
		Rule(".back-to-top.visible {\n").Should().Contain("visibility: visible;");
		Slice("function scrollToTop(", "function initBackToTop(").Should()
			.Contain("window.scrollTo({ top: 0, behavior: scrollBehavior() });").And.Contain("skip.focus({ preventScroll: true });");
	}

	[Fact]
	public void NoJs_HidesScriptControlsAndShowsWhatScriptsReveal()
	{
		string hidden = Regex.Match(_css, @"html\.no-js :is\(([^)]*)\) \{ display: none !important; \}").Groups[1].Value;

		foreach (string control in new[]
		         {
			         ".search-trigger", ".theme-toggle", ".toc-toggle-btn", ".mobile-nav-toggle", ".js-only"
		         })
		{
			hidden.Should().Contain(control);
		}

		hidden.Should().NotContain("theme-selector").And.NotContain("style-selector", "the Appearance panel is js-only; the old pickers are gone");
		_css.Should().Contain("html.no-js .nav-children:not(.expanded) { max-height: none !important; opacity: 1; visibility: visible; }");
		_css.Should().Contain("html.no-js .tabs > .tab-content[hidden] { display: block; }");
		_css.Should().Contain("html.no-js .tabs > .tab-headers { display: contents; }");
		_js.Should().Contain("html.classList.remove('no-js');");
	}

	[Fact]
	public void ImageZoom_SkipsLinkedAndTinyImagesAndCanBeTurnedOff()
	{
		string zoom = Slice("function initImageZoom(", "// Mermaid diagrams.");

		zoom.Should().Contain("if (html.hasAttribute('data-no-image-zoom')) return none;");
		zoom.Should().Contain("if (closest(img, 'a, button, .mermaid, [data-no-zoom]')) return;");
		zoom.Should().Contain("< 96 ||").And.Contain("< 64) return;");
		zoom.Should().Contain("box.setAttribute('aria-modal', 'true');").And.Contain("restoreFocus(opener);");
		Rule(".lightbox {").Should().Contain("cursor: zoom-out;");
		_css.Should().Contain("@media (prefers-reduced-motion: no-preference) {\n    html:not([data-no-animations]) .lightbox:not([hidden])");
	}

	[Fact]
	public void TocDrawer_GetsTheSidebarsBackdrop()
	{
		// Only the mobile sidebar dimmed the page behind it; the "On this page" drawer and the
		// floating popover on phones had nothing to click away on.
		Slice("function createBackdrop(", "function setNavOpen(").Should()
			.Contain("backdrop.className = 'drawer-backdrop ' + name;")
			.And.Contain("backdrop.addEventListener('click', function() { toggle.focus(); });");
		Slice("function initTocDrawer(", "// Code blocks.").Should().Contain("tocBackdrop = createBackdrop('toc-backdrop', tocToggle);");
		Slice("function setTocOpen(", "function initMobileNav(").Should().Contain("if (tocBackdrop) tocBackdrop.classList.toggle('open', open);");
		Rule(".drawer-backdrop {").Should().Contain("z-index: var(--z-backdrop);").And.Contain("display: none;");
		_css.Should().Contain("@media (max-width: 1200px) {\n    .toc-backdrop.open { display: block; }\n}");
		_css.Should().Contain("@media (max-width: 768px) {\n    .nav-backdrop.open { display: block; }");
		_css.Should().Contain("@media (min-width: 769px) {\n    html[data-toc=\"floating\"] .toc-backdrop.open { display: none; }\n}");
	}

	[Fact]
	public void InstallWidgetTabs_AreATablistWithArrowKeys()
	{
		// The Package Manager / .NET CLI / PackageReference tabs took clicks only: no roving
		// tabindex, no tabpanel links, no arrow keys.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  const tab = (key, active) => h('button', { class: 'install-tab' + (active ? ' active' : ''), role: 'tab', 'aria-selected': active ? 'true' : 'false', 'data-tab': key });
		                  const panel = (key, active) => h('div', { class: 'install-panel' + (active ? ' active' : ''), 'data-panel': key }, h('pre', {}, h('code', {})), h('button', { class: 'install-copy-btn' }));
		                  const html = h('html', {});
		                  const body = h('body', {}, h('div', { class: 'package-install-widget' },
		                      h('div', { class: 'install-tabs', role: 'tablist' }, tab('pm', true), tab('cli'), tab('ref')),
		                      panel('pm', true), panel('cli'), panel('ref')));
		                  function copyText() {}
		                  {{Slice("function closest(", "function mediaMatches(")}}
		                  {{Slice("function initInstallWidget(", "// navigator.clipboard exists only")}}
		                  {{Slice("function linkTabs(", "function selectTab(")}}
		                  initInstallWidget();
		                  const tabs = body.querySelectorAll('.install-tab');
		                  const panels = body.querySelectorAll('.install-panel');
		                  const state = () => tabs.map(t => t.getAttribute('aria-selected') + t.tabIndex).join(' ') + ' | '
		                      + panels.map(p => p.hidden ? '-' : p.getAttribute('data-panel')).join('') + ' | '
		                      + (document.activeElement ? document.activeElement.getAttribute('data-tab') : 'none');
		                  const seen = [state(), tabs[0].getAttribute('aria-controls') + ' ' + panels[0].getAttribute('role') + ' ' + panels[0].getAttribute('aria-labelledby')];
		                  const key = k => fire(document.activeElement, 'keydown', { key: k });
		                  tabs[0].focus();
		                  key('ArrowRight'); seen.push(state());
		                  key('End'); seen.push(state());
		                  key('ArrowRight'); seen.push(state());
		                  key('ArrowLeft'); seen.push(state());
		                  key('Home'); seen.push(state());
		                  fire(tabs[1], 'click'); seen.push(state());
		                  process.stdout.write(JSON.stringify(seen));
		                  """;

		JsonSerializer.Deserialize<string[]>(NodeScript.Run(script)).Should().Equal(
			"true0 false-1 false-1 | pm-- | none",
			"mokadocs-tabpanel-install-0 tabpanel mokadocs-tab-install-0",
			"false-1 true0 false-1 | -cli- | cli",
			"false-1 false-1 true0 | --ref | ref",
			"true0 false-1 false-1 | pm-- | pm",
			"false-1 false-1 true0 | --ref | ref",
			"true0 false-1 false-1 | pm-- | pm",
			"false-1 true0 false-1 | -cli- | pm");
	}

	[Fact]
	public void LayoutSection_SwitchesStoresAndResetsTheLayoutParts()
	{
		// One labelled select per part, built when the panel first opens. A pick sets the part's attribute on
		// <html> at once and is stored; picking the site's own variant forgets the stored one; a change made
		// elsewhere shows in the selects; Reset goes back to the site's variants and removes every key.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string script = $$"""
		                  {{_fakeDom}}
		                  Object.defineProperty(El.prototype, 'value', {
		                      get() {
		                          if (this.tag === 'option') return this.attrs.value;
		                          const options = this.children.filter(c => c.tag === 'option');
		                          const picked = options.find(o => 'selected' in o.attrs) || options[0];
		                          return picked ? picked.attrs.value : '';
		                      },
		                      set(v) { this.children.filter(c => c.tag === 'option').forEach(o => { if (o.attrs.value === v) o.attrs.selected = ''; else delete o.attrs.selected; }); }
		                  });
		                  const removeAttribute = El.prototype.removeAttribute;
		                  El.prototype.removeAttribute = function(n) { removeAttribute.call(this, n); if (this === html) observers.forEach(f => f()); };
		                  const html = h('html', { 'data-theme': 'light', 'data-style': 'default', 'data-navbar': 'tabs', 'data-toc': 'inline', 'data-footer': 'none' });
		                  const body = h('body', {},
		                      h('div', { class: 'appearance' },
		                          h('button', { class: 'appearance-trigger', 'aria-expanded': 'false' }),
		                          h('div', { class: 'appearance-panel', hidden: '', 'data-default-style': 'default' },
		                              h('button', { class: 'appearance-close' }),
		                              h('div', { class: 'appearance-layout', role: 'group', 'data-default-layout': 'navbar=tabs toc=inline ' }),
		                              h('button', { class: 'appearance-reset' }))));
		                  const saved = { 'mokadocs-layout-density': 'spacious' };
		                  const store = { get: k => saved[k] || null, set: (k, v) => { saved[k] = v; }, remove: k => { delete saved[k]; } };
		                  const window = { matchMedia: () => ({ matches: false }) };
		                  {{Slice("const strings =", "function scrollBehavior(")}}
		                  {{Slice("const appearanceListeners = [];", "// Mobile nav toggle and the")}}
		                  initAppearance();
		                  const select = key => body.querySelector('[data-layout-part="' + key + '"]');
		                  const pick = (key, value) => { select(key).value = value; fire(select(key), 'change'); };
		                  const attrs = () => ['data-navbar', 'data-footer', 'data-toc', 'data-density'].map(a => html.getAttribute(a) || '-').join(' ');
		                  const keys = () => Object.keys(saved).sort().map(k => k.replace('mokadocs-layout-', '') + '=' + saved[k]).join(',');
		                  const values = () => body.querySelectorAll('select').map(s => s.value).join(' ');
		                  const seen = [String(body.querySelectorAll('select').length)];
		                  fire(body.querySelector('.appearance-trigger'), 'click');
		                  const fields = body.querySelector('.appearance-layout').children;
		                  seen.push(fields.length + ' ' + fields.every(f => f.children[0].tag === 'label' && f.children[0].getAttribute('for') === f.children[1].getAttribute('id')));
		                  seen.push(fields.map(f => f.children[0].outerHTML.replace(/<[^>]+>/g, '')).join('|'));
		                  seen.push(select('navbar').children.map(o => o.outerHTML.replace(/<[^>]+>/g, '')).join('|'));
		                  seen.push(select('sidebar-scope').children.map(o => o.outerHTML.replace(/<[^>]+>/g, '')).join('|'));
		                  seen.push(values());
		                  pick('navbar', 'compact');
		                  seen.push(attrs() + ' | ' + keys());
		                  pick('navbar', 'tabs');
		                  pick('footer', 'columns');
		                  pick('toc', 'default');
		                  seen.push(attrs() + ' | ' + keys());
		                  html.setAttribute('data-density', 'compact');
		                  seen.push(values());
		                  fire(body.querySelector('.appearance-reset'), 'click');
		                  seen.push(attrs() + ' | ' + keys() + ' | ' + values());
		                  process.stdout.write(JSON.stringify(seen));
		                  """;

		JsonSerializer.Deserialize<string[]>(NodeScript.Run(script)).Should().Equal(
			"0",
			"13 true",
			"Top bar|Footer|On this page|Breadcrumbs|Previous and next|Sidebar|Sidebar lists|Admonitions|Tables|Spacing|Font|Headings|Links",
			"Standard|Compact|Floating|Tabs (default)|Auto-hide|Centered|Transparent",
			"All sections (default)|Current section",
			"tabs none inline default default default default default default default default default default",
			"compact none inline - | density=spacious,navbar=compact",
			"tabs columns - - | density=spacious,footer=columns,toc=default",
			"tabs columns default default default default default default default compact default default default",
			"tabs - inline - |  | tabs default inline default default default default default default default default default default");
	}

	[Fact]
	public void Autohide_FollowsTheAttributeOnAnOpenPage()
	{
		// The script used to set autohide up only when the page loaded with it, so neither the Layout section nor
		// the theme lab could switch to it, and switching away left the bar hidden.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 const classes = new Set();
		                                 const attrs = {};
		                                 const html = { getAttribute: n => n in attrs ? attrs[n] : null, classList: { contains: c => classes.has(c), toggle: (c, on) => on ? classes.add(c) : classes.delete(c), remove: c => classes.delete(c) } };
		                                 const header = { offsetHeight: 60, contains: () => false, querySelector: () => null, addEventListener: () => {} };
		                                 const document = { activeElement: null, querySelector: s => s === '.site-header' ? header : null };
		                                 const window = { scrollY: 0 };
		                                 const sidebar = null;
		                                 let task = null;
		                                 function onFrame(t) { task = t; }
		                                 {{Slice("function initAutohide(", "// The tabs row")}}
		                                 initAutohide();
		                                 const run = y => { const write = task({ y }); if (write) write(); return classes.has('navbar-hidden'); };
		                                 const seen = [run(500)];
		                                 attrs['data-navbar'] = 'autohide';
		                                 seen.push(run(1000), run(990), run(1500));
		                                 attrs['data-navbar'] = 'compact';
		                                 seen.push(run(1500));
		                                 attrs['data-navbar'] = 'autohide';
		                                 seen.push(run(1500), run(2000));
		                                 process.stdout.write(JSON.stringify(seen));
		                                 """);

		result.Should().Be("[false,true,false,true,false,false,true]");
	}

	[Fact]
	public void TocSpy_MarksTheCurrentHeadingInBothCopiesOfTheList()
	{
		// With the Layout section on, a page carries "On this page" beside the content and inline. Each heading
		// counts once, and its link in both lists is marked.
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 const tops = { a: -400, b: 40, c: 600 };
		                                 const heading = id => ({ getBoundingClientRect: () => ({ top: tops[id], width: 100, height: 20 }) });
		                                 const headings = { a: heading('a'), b: heading('b'), c: heading('c') };
		                                 const link = (id, list) => {
		                                     const classes = new Set(), attrs = { href: '#' + id };
		                                     return { list, id, classes, attrs, classList: { add: c => classes.add(c), remove: c => classes.delete(c) },
		                                         getAttribute: n => n in attrs ? attrs[n] : null, setAttribute: (n, v) => { attrs[n] = v; }, removeAttribute: n => { delete attrs[n]; } };
		                                 };
		                                 const links = ['a', 'b', 'c'].map(id => link(id, 'inline')).concat(['a', 'b', 'c'].map(id => link(id, 'aside')));
		                                 const counter = { textContent: '' };
		                                 const document = { querySelectorAll: () => links, getElementById: id => id === 'tocCounter' ? counter : id === 'tocProgress' ? null : headings[id] || null };
		                                 function headerOffset() { return 60; }
		                                 let task = null;
		                                 function onFrame(t) { task = t; }
		                                 const html = { getAttribute: () => 'en' };
		                                 {{Slice("const strings =", "function closest(")}}
		                                 {{Slice("function tocTarget(", "// Back to the top of the page.")}}
		                                 initTocSpy();
		                                 const marked = () => links.filter(l => l.classes.has('active')).map(l => l.list + ':' + l.id + ':' + l.attrs['aria-current']).join(' ') + ' | ' + counter.textContent;
		                                 task({ y: 400, viewport: 800, height: 5000 })();
		                                 const seen = [marked()];
		                                 tops.c = 20;
		                                 task({ y: 900, viewport: 800, height: 5000 })();
		                                 seen.push(marked());
		                                 process.stdout.write(JSON.stringify(seen));
		                                 """);

		JsonSerializer.Deserialize<string[]>(result).Should().Equal(
			"inline:b:location aside:b:location | Section 2 of 3",
			"inline:c:location aside:c:location | Section 3 of 3");
	}
}
