using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The announcement banner's stylesheet rules and its part of main.js. The markup and the inline
///     script that hides a dismissed banner are in <c>BannerRenderingTests</c>.
/// </summary>
public sealed class EmbeddedThemeBannerTests
{
	private static readonly string _css = ThemeCss.All;
	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	public static TheoryData<string> Styles()
	{
		var data = new TheoryData<string>();
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			data.Add(style);
		}

		return data;
	}

	private static string InitBanner()
	{
		int start = _js.IndexOf("function initBanner()", StringComparison.Ordinal);
		start.Should().BeGreaterThan(0, "main.js should have initBanner");
		int end = _js.IndexOf("\n    }\n", start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return _js[start..(end + 6)];
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

	[Fact]
	public void HiddenBanner_IsNotDisplayed_PrintLeavesItOut_AndForcedColorsKeepItsEdge()
	{
		// The banner's display would otherwise beat the hidden attribute the inline script sets.
		_css.Should().Contain(".site-banner[hidden] { display: none; }");
		Block("@media print {").Should().Contain(".site-banner { display: none !important; }");
		Block("@media (forced-colors: active) {").Should().Contain(".site-banner { border-bottom-color: CanvasText; }");
	}

	[Fact]
	public void Variants_SetTheirColorsThroughTheBannerVariables()
	{
		_css.Should().Contain(".site-banner[data-variant=\"warning\"] {").And.Contain(".site-banner[data-variant=\"accent\"] {");
		Regex.Match(_css, @"\.site-banner\[data-variant=""accent""\] \{[^}]*\}").Value
			.Should().Contain("--banner-bg: var(--color-primary-fill);").And.Contain("--banner-text: var(--color-on-primary);");
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_GivesTheBannerItsOwnLook(string style)
	{
		_css.Should().Contain($"html[data-style=\"{style}\"] .site-banner");
	}

	[Fact]
	public void Styles_UseTheirAccentShadeForTheLinkTheFillAndAFocusRingThatShowsOnTheFill()
	{
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			string scope = $"html[data-style=\"{style}\"]";
			string css = ThemeCss.Style(style);
			css.Should().Contain(scope + " .site-banner[data-variant=\"info\"] { --banner-link: var(--st-link); }");
			css.Should().Contain(scope + " .site-banner[data-variant=\"accent\"] { --banner-bg: var(--st-link); --banner-text: var(--st-on-fill);");
			css.Should().Contain(scope + " .site-banner :focus-visible { outline: 2px solid currentColor;");
		}
	}

	[Fact]
	public void Drawers_MoveDownByTheVisibleBanner_BelowTheLayoutVariants()
	{
		_css.Should().Contain(":where(.site-banner:not([hidden]) ~ .site-container) .sidebar { margin-top: var(--banner-offset, 0px); }");
		_css.Should().Contain(":where(.site-banner:not([hidden]) ~ .site-container) .toc-sidebar { margin-top: var(--banner-offset, 0px); }");
		_css.Should().Contain(":where(.site-banner:not([hidden]) ~ .drawer-backdrop) { margin-top: var(--banner-offset, 0px); }");
	}

	[Fact]
	public void Runtime_StartsTheBanner()
	{
		_js.Should().Contain("initBanner();");
	}

	[Fact]
	public void Dismiss_StoresTheIdHidesTheBannerAndMovesFocusToTheHeader()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		string result = NodeScript.Run($$"""
		                                 const stored = {};
		                                 const store = { set: (k, v) => { stored[k] = v; } };
		                                 const props = {};
		                                 const html = { style: { setProperty: (k, v) => { props[k] = v; } } };
		                                 const tasks = [];
		                                 const onFrame = t => tasks.push(t);
		                                 let queued = 0;
		                                 const queueFrame = () => { queued++; };
		                                 let bottom = 40;
		                                 const listeners = {};
		                                 const close = { addEventListener: (type, f) => { listeners[type] = f; } };
		                                 const banner = {
		                                     hidden: false,
		                                     getAttribute: n => n === 'data-banner-id' ? 'launch-2' : null,
		                                     querySelector: s => s === '.site-banner-close' ? close : null,
		                                     getBoundingClientRect: () => ({ bottom })
		                                 };
		                                 let focused = null;
		                                 const logo = { focus: () => { focused = 'logo'; } };
		                                 const document = {
		                                     getElementById: id => id === 'mokadocs-banner' ? banner : null,
		                                     querySelector: s => s === '.site-logo' ? logo : null
		                                 };
		                                 {{InitBanner()}}
		                                 initBanner();
		                                 const frame = () => { const w = tasks[0](); if (w) w(); return w ? 'write' : 'none'; };
		                                 const out = [frame(), props['--banner-offset'], frame()];
		                                 bottom = -12.4;
		                                 out.push(frame(), props['--banner-offset']);
		                                 bottom = 40;
		                                 out.push(frame(), props['--banner-offset']);
		                                 listeners.click();
		                                 out.push(stored['mokadocs-banner-dismissed'], banner.hidden, focused, queued, frame(), props['--banner-offset']);
		                                 process.stdout.write(JSON.stringify(out));
		                                 """);

		result.Should().Be("""["write","40px","none","write","0px","write","40px","launch-2",true,"logo",1,"write","0px"]""");
	}
}
