using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The theme's part for the Markdown components: its stylesheet block, the colors it gives text, and the
///     glossary tooltip script.
/// </summary>
public sealed partial class EmbeddedThemeComponentTests
{
	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	private static string ComponentCss()
	{
		int start = _css.IndexOf("/* Components:", StringComparison.Ordinal);
		start.Should().BeGreaterThan(_css.IndexOf("/* Layouts:", StringComparison.Ordinal), "the components come after the layouts");
		return _css[start..];
	}

	[Fact]
	public void ComponentCss_SizesTextInRem_AndUsesNoRawColorForText()
	{
		string css = ComponentCss();

		FontSizePx().Matches(css).Should().BeEmpty();
		// Text takes the theme's tokens; raw colors only feed a mix or a tint.
		RawTextColor().Matches(css).Select(m => m.Value).Should().BeEmpty();
	}

	[Fact]
	public void StyleTweaks_ShipInTheirStylesStylesheet()
	{
		// A style's component tweaks go into that style's own file, so main.css stays the default style
		// and a tweak can't reach the other styles.
		EmbeddedThemeProvider.GetCss().Should().NotMatchRegex(@"html\[data-style=""[a-z]+""\] [^{]*\.component-");
		EmbeddedThemeProvider.GetStyleCss("paper").Should().Contain("html[data-style=\"paper\"] .component-details { border-left: 0;");
		EmbeddedThemeProvider.GetStyleCss("terminal").Should().Contain("html[data-style=\"terminal\"] .component-badge::before");
		EmbeddedThemeProvider.GetStyleCss("moka").Should().NotContain(".component-badge::before")
			.And.NotContain("html[data-style=\"paper\"]");
	}

	// The default style's badges reuse the API badge shades on a 14% tint, in light and dark mode.
	[Theory]
	[InlineData("blue", "#3b82f6")]
	[InlineData("green", "#22c55e")]
	[InlineData("amber", "#f59e0b")]
	[InlineData("red", "#ef4444")]
	[InlineData("slate", "#64748b")]
	public void Badges_MeetAaInTheDefaultStyle(string shade, string baseColor)
	{
		MatchCollection shades = Regex.Matches(_css, $"--badge-{shade}: (#[0-9a-f]{{6}});");
		shades.Count.Should().BeGreaterThanOrEqualTo(2);
		(string text, string page)[] modes = [(shades[0].Groups[1].Value, "#ffffff"), (shades[1].Groups[1].Value, "#0f172a")];
		ComponentCss().Should().Contain("background: color-mix(in srgb, var(--cmp-c) 14%, var(--color-bg));");

		foreach ((string text, string page) in modes)
		{
			double[] background = ThemeColors.Mix(ThemeColors.Hex(baseColor), 0.14, ThemeColors.Hex(page));
			ThemeColors.Contrast(ThemeColors.Hex(text), background).Should().BeGreaterThanOrEqualTo(4.5, "{0} on its tint over {1}", text, page);
		}
	}

	[Fact]
	public void InstallWidgetScript_SetsUpEveryWidgetOnThePage()
	{
		_js.Should().Contain("document.querySelectorAll('.package-install-widget').forEach(initPackageWidget);")
			.And.Contain("linkTabs(tabs, panels, n ? 'install-' + n : 'install');");
	}

	[Fact]
	public void GlossaryTooltip_TogglesOnClick_HidesOnEscape_AndStaysOnScreen()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");
		string block = _js[_js.IndexOf("/* Components:", StringComparison.Ordinal)..];

		string result = NodeScript.Run($$"""
		                                 const docListeners = {}, winListeners = {};
		                                 function classes() {
		                                     const set = new Set();
		                                     return { add: (...c) => c.forEach(x => set.add(x)), remove: (...c) => c.forEach(x => set.delete(x)), contains: c => set.has(c), toString: () => [...set].sort().join(' ') };
		                                 }
		                                 function element(cls, parent) {
		                                     const e = {
		                                         cls, parentElement: parent || null, classList: classes(), listeners: {}, hovered: false, rect: { top: 300, left: 100, right: 300 },
		                                         style: { props: {}, setProperty(k, v) { this.props[k] = v; }, removeProperty(k) { delete this.props[k]; } },
		                                         addEventListener(t, f) { (this.listeners[t] = this.listeners[t] || []).push(f); },
		                                         fire(t, ev) { (this.listeners[t] || []).forEach(f => f(ev || {})); },
		                                         contains(o) { for (let x = o; x; x = x.parentElement) if (x === this) return true; return false; },
		                                         closest(sel) { for (let x = this; x; x = x.parentElement) if (sel === '.' + x.cls) return x; return null; },
		                                         matches(sel) { return sel === ':hover' && this.hovered; },
		                                         querySelector(sel) { return sel === '.glossary-tip' ? tip : null; },
		                                         getBoundingClientRect() { return this.rect; }
		                                     };
		                                     return e;
		                                 }
		                                 const page = element('page');
		                                 const term = element('glossary-term', page);
		                                 const button = element('glossary-term-button', term);
		                                 const tip = element('glossary-tip', term);
		                                 const header = element('site-header');
		                                 header.rect = { bottom: 60 };
		                                 const document = {
		                                     activeElement: null,
		                                     documentElement: { clientWidth: 400 },
		                                     querySelector: sel => sel === '.site-header' ? header : null,
		                                     querySelectorAll: sel => sel === '.glossary-term' ? [term] : [],
		                                     getElementById: () => null,
		                                     addEventListener(t, f) { (docListeners[t] = docListeners[t] || []).push(f); }
		                                 };
		                                 const window = { location: { hash: '' }, addEventListener(t, f) { (winListeners[t] = winListeners[t] || []).push(f); } };
		                                 function fireDoc(t, ev) { (docListeners[t] || []).forEach(f => f(ev)); }
		                                 const out = [];
		                                 {{block}}
		                                 fireDoc('click', { target: button });
		                                 out.push('click:' + term.classList);
		                                 fireDoc('click', { target: button });
		                                 out.push('again:' + term.classList);
		                                 term.fire('pointerleave');
		                                 fireDoc('click', { target: button });
		                                 fireDoc('click', { target: page });
		                                 out.push('outside:' + term.classList);
		                                 term.hovered = true;
		                                 let prevented = false;
		                                 fireDoc('keydown', { key: 'Escape', defaultPrevented: false, preventDefault() { prevented = true; } });
		                                 out.push('escape:' + term.classList + ':' + prevented);
		                                 term.classList.remove('is-dismissed');
		                                 fireDoc('keydown', { key: 'Escape', defaultPrevented: true, preventDefault() {} });
		                                 out.push('handled:' + term.classList);
		                                 tip.rect = { top: 40, left: -20, right: 180 };
		                                 term.fire('pointerenter');
		                                 out.push('placed:' + term.classList + ':' + tip.style.props['--tip-shift']);
		                                 console.log(out.join('|'));
		                                 """);

		result.Trim().Should().Be("click:is-open|again:is-dismissed|outside:|escape:is-dismissed:true|handled:|placed:is-below:28px");
	}

	[GeneratedRegex(@"font-size: \d+(\.\d+)?px")]
	private static partial Regex FontSizePx();

	[GeneratedRegex(@"(?<![-\w])color: #[0-9a-fA-F]{3,6}")]
	private static partial Regex RawTextColor();
}
