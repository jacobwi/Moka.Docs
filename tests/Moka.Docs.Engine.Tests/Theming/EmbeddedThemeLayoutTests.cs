using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the CSS of the default theme's layout variants (<c>theme.options.layout</c>). Each
///     variant is a block of rules scoped to the attribute the layouts write on <c>&lt;html&gt;</c>.
/// </summary>
public sealed partial class EmbeddedThemeLayoutTests
{
	private static readonly string _css = ThemeCss.All;

	private static string LayoutCss()
	{
		int start = _css.IndexOf("/* Layouts:", StringComparison.Ordinal);
		start.Should().BeGreaterThan(0);
		int end = _css.IndexOf("/* Components:", start, StringComparison.Ordinal);
		return end > start ? _css[start..end] : _css[start..];
	}

	private static readonly string _js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");

	private static string AttributeOf(ThemeLayoutPart part) =>
		string.Concat(part.Key.Select(c => char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : c.ToString()));

	private static string BaseRule(string selector)
	{
		int start = _css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have a rule for {0}", selector);
		return _css[start..(_css.IndexOf('}', start) + 1)];
	}

	[GeneratedRegex("""^(?::where\()?html(?:\[data-style="[a-z]+"\])?\[data-(?<part>[a-z-]+)="(?<variant>[a-z]+)"\]""")]
	private static partial Regex ScopeRegex();

	[Fact]
	public void LayoutRules_AreScopedToANonDefaultVariant()
	{
		// An unscoped rule would change the default layout, which must stay as it was.
		var unscoped = new List<string>();
		bool inComment = false;
		foreach (string line in LayoutCss().Split('\n'))
		{
			if (inComment || line.TrimStart().StartsWith("/*", StringComparison.Ordinal))
			{
				inComment = !line.Contains("*/", StringComparison.Ordinal);
				continue;
			}

			int brace = line.IndexOf('{', StringComparison.Ordinal);
			string? selector = brace >= 0 ? line[..brace].Trim()
				: line.Length > 0 && !char.IsWhiteSpace(line[0]) && line.EndsWith(',') ? line.Trim()
				: null;
			if (string.IsNullOrEmpty(selector) || selector.StartsWith('@'))
			{
				continue;
			}

			Match scope = ScopeRegex().Match(selector);
			string variant = scope.Groups["variant"].Value;
			ThemeLayoutPart? part = ThemeLayouts.Parts.SingleOrDefault(p => AttributeOf(p) == scope.Groups["part"].Value);
			if (!scope.Success || part is null || variant == ThemeLayouts.Default || !part.Names.Contains(variant))
			{
				unscoped.Add(selector);
			}
		}

		unscoped.Should().BeEmpty();
	}

	[Fact]
	public void EveryVariant_HasRules()
	{
		string css = LayoutCss();
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			string attribute = AttributeOf(part);
			foreach (string variant in part.Names.Where(n => n != ThemeLayouts.Default))
			{
				// footer: none leaves the footer out of the page, so it has nothing to style.
				if (part.Key == "footer" && variant == "none")
				{
					continue;
				}

				css.Should().Contain($"[data-{attribute}=\"{variant}\"]", $"{part.Key}: {variant} needs its rules");
			}
		}
	}

	[Fact]
	public void SidebarScopeSection_HidesTheOtherSectionsFromTheActiveMarkingAlone()
	{
		// Pure CSS on .nav-section.active, which the inline sidebar and the shared sidebar script both
		// write, so the two behave the same. A page outside every section keeps all of them.
		LayoutCss().Should().Contain(
			"html[data-sidebar-scope=\"section\"] .sidebar-nav:has(> .nav-section.active > .nav-children) > .nav-section:not(.active) { display: none; }");
	}

	[Fact]
	public void GithubAdmonitions_HaveAnIconForEveryType()
	{
		string css = LayoutCss();
		foreach (string type in new[] { "note", "tip", "warning", "caution", "danger", "info", "important" })
		{
			css.Should().MatchRegex(
				$@"html\[data-admonitions=""github""\] (:is\([^)]*)?\.admonition-{type}\b[^{{]*\{{ --ad-icon: url\(""data:image/svg\+xml,");
		}

		css.Should().NotContain("data:image/svg+xml,<", "the icons are percent-encoded");
	}

	[Fact]
	public void AdmonitionTypes_DefineTheirColorAndTitleShade()
	{
		BaseRule("\n.admonition-note {").Should().Contain("--ad-c: #3b82f6; --ad-text: var(--admonition-note-text);");
		BaseRule("\n.admonition-danger {").Should().Contain("--ad-c: #ef4444; --ad-text: var(--admonition-danger-text);");
		BaseRule(".admonition { --ad-ink:").Should().Contain("var(--st-c, var(--ad-text");
	}

	[Fact]
	public void FloatingButtons_ShareOneCornerAndStackWithoutOverlap()
	{
		// Back to top and the "On this page" button used different right offsets (2rem and 1rem), and
		// the button sat over the footer's "Built with" line at the end of the page.
		BaseRule(".back-to-top { position: fixed;").Should().Contain("bottom: var(--fab-bottom); right: var(--fab-right);");
		_css.Should().Contain("bottom: calc(var(--fab-bottom) + var(--fab-size) + var(--fab-gap));\n        right: var(--fab-right);");
		LayoutCss().Should().Contain("right: var(--fab-right);\n    bottom: calc(var(--fab-bottom) + 2 * var(--fab-size) + 2 * var(--fab-gap));");
		BaseRule(".site-footer::after").Should().Contain("height: var(--fab-clear);");
		_css.Should().NotContain("bottom: 5rem").And.NotContain("right: 2rem");
	}

	[Fact]
	public void PageNavAndFeedback_FollowTheArticleColumn()
	{
		BaseRule(".page-nav {").Should().NotContain("max-width").And.NotContain("auto");
		BaseRule(".feedback-widget {").Should().NotContain("max-width").And.NotContain("auto");
		_css.Should().Contain("@media (max-width: 640px) {\n    .page-nav { grid-template-columns: minmax(0, 1fr);");
	}

	[Fact]
	public void BoxedWidth_FramesTheHeaderAndFooterToo()
	{
		string css = LayoutCss();
		css.Should().Contain("html[data-width=\"boxed\"]:not([data-navbar=\"floating\"]) .site-header,");
		css.Should().Contain("html[data-width=\"boxed\"] :is(.site-footer, .landing-footer) { max-width: var(--layout-max-width);");
	}

	[Fact]
	public void SidebarToggle_HidesTheSidebarOnWideScreensOnly()
	{
		string css = _css[_css.IndexOf(".sidebar-toggle { display: none; }", StringComparison.Ordinal)..];
		css.Should().StartWith(".sidebar-toggle { display: none; }\n@media (min-width: 769px) {");
		css[..css.IndexOf("\n}\n", StringComparison.Ordinal)].Should().Contain("html.sidebar-hidden .sidebar { display: none; }");
	}

	[Fact]
	public void SidebarToggle_ClickHidesAndShowsAndRemembersTheChoice()
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");

		int start = _js.IndexOf("function initSidebarToggle(", StringComparison.Ordinal);
		int end = _js.IndexOf("function initTocDrawer(", start, StringComparison.Ordinal);
		string result = NodeScript.Run($$"""
		                                 const saved = {};
		                                 const store = { set: (k, v) => { saved[k] = v; }, remove: k => { delete saved[k]; } };
		                                 const classes = new Set();
		                                 const html = { classList: { contains: c => classes.has(c), toggle: (c, on) => on ? classes.add(c) : classes.delete(c) } };
		                                 const attrs = {};
		                                 let click = null;
		                                 const button = { setAttribute: (k, v) => { attrs[k] = v; }, addEventListener: (t, f) => { click = f; } };
		                                 const document = { getElementById: id => id === 'sidebarToggle' ? button : null };
		                                 {{_js[start..end]}}
		                                 initSidebarToggle();
		                                 const states = [];
		                                 click(); states.push([classes.has('sidebar-hidden'), attrs['aria-expanded'], saved['mokadocs-sidebar-hidden'] || null]);
		                                 click(); states.push([classes.has('sidebar-hidden'), attrs['aria-expanded'], saved['mokadocs-sidebar-hidden'] || null]);
		                                 process.stdout.write(JSON.stringify(states));
		                                 """);

		result.Should().Be("[[true,\"false\",\"true\"],[false,\"true\",null]]");
	}

	private static string Block(string css, string start)
	{
		int at = css.IndexOf(start, StringComparison.Ordinal);
		at.Should().BeGreaterThanOrEqualTo(0, "the stylesheet should have {0}", start);
		int depth = 0;
		for (int i = css.IndexOf('{', at); i < css.Length; i++)
		{
			depth += css[i] == '{' ? 1 : css[i] == '}' ? -1 : 0;
			if (depth == 0)
			{
				return css[at..(i + 1)];
			}
		}

		return css[at..];
	}

	[Fact]
	public void TransparentNavbar_IsSolidWithoutBlurOrWhenTheReaderAsksForLessTransparency()
	{
		string css = LayoutCss();
		Block(css, "@supports not ((backdrop-filter: blur(1px))")
			.Should().Contain("html[data-navbar=\"transparent\"] .site-header { background: var(--color-bg); }");
		Block(css, "@media (prefers-reduced-transparency: reduce) {\n    html[data-navbar=\"transparent\"]")
			.Should().Contain("background: var(--color-bg); -webkit-backdrop-filter: none; backdrop-filter: none;");
	}

	[Fact]
	public void SubtleLinks_KeepAnUnderlineAtRestAndASolidOneOnFocus()
	{
		// The links take the text color, so color alone can't mark them (WCAG 1.4.1).
		string css = LayoutCss();
		const string link = "html[data-links=\"subtle\"] :is(.page-content, .landing-md-content) a:not([class], :is(.breadcrumbs, .toc-inline) *)";
		Block(css, link + " {").Should().Contain("color: inherit;").And.Contain("text-decoration: underline dotted;");
		Block(css, link + ":is(:hover, :focus-visible) {").Should().Contain("text-decoration: underline solid;")
			.And.Contain("text-decoration-color: var(--st-link, var(--color-primary-text));");
	}

	[Fact]
	public void NumberedHeadingsAndToc_CountWithCssCountersAndLeaveThePageTitleAlone()
	{
		string css = LayoutCss();
		css.Should().Contain("html[data-headings=\"numbered\"] .page-content > h2 { counter-increment: heading-2; counter-reset: heading-3; }");
		css.Should().Contain("html[data-headings=\"numbered\"] .page-content > h3 { counter-increment: heading-3; }");
		css.Should().NotContain("data-headings=\"numbered\"] .page-content > h1");
		css.Should().Contain("html[data-toc=\"numbered\"] :is(.toc-list, .toc-list ul) { counter-reset: toc-entry; }");
		css.Should().Contain("content: counters(toc-entry, '.') / '';");
	}

	[Fact]
	public void HeroBanner_UsesTheAccentFillAndItsTextColor()
	{
		Block(LayoutCss(), "html[data-hero=\"banner\"] .landing-hero {")
			.Should().Contain("background: var(--st-link, var(--color-primary-fill));")
			.And.Contain("color: var(--st-on-fill, var(--color-on-primary));");
	}

	[Fact]
	public void ShowcaseCallToAction_UsesTheAccentFillAndItsTextColor()
	{
		Block(LayoutCss(), "html[data-home=\"showcase\"] .home-cta {")
			.Should().Contain("background: var(--st-link, var(--color-primary-fill));")
			.And.Contain("color: var(--st-on-fill, var(--color-on-primary));");
	}

	[Fact]
	public void PortalSearchBox_OpensTheSearchDialogLikeTheHeaderButton()
	{
		// The same loop sets the shortcut on it (Meta+K on Apple devices) and opens the dialog on click.
		_js.Should().Contain("document.querySelectorAll('.search-trigger, .home-search').forEach(function(trigger) {");
		_css.Should().Contain("html.no-js :is(.search-trigger,", "the box has the js-only class, hidden without a script");
	}

	[Fact]
	public void FeatureVariants_LeaveThePortalSectionCardsAlone()
	{
		// The portal's section cards carry .landing-feature-card for each style's card look, so a features
		// variant only reaches the cards inside the feature section.
		string[] rules = [.. LayoutCss().Split('\n').Where(l => l.Contains("[data-features=", StringComparison.Ordinal) && l.Contains(".landing-feature-", StringComparison.Ordinal))];

		rules.Should().NotBeEmpty();
		rules.Should().OnlyContain(r => r.Contains(".landing-features ", StringComparison.Ordinal));
	}

	[Fact]
	public void StickyParts_FollowTheHeaderOffset()
	{
		// The tabs, floating and autohide navbars move where the sticky sidebars start through
		// --header-offset. A rule using --header-height would sit under the tabs row.
		_css.Should().NotContain("top: var(--header-height)").And.Contain("top: var(--header-offset)");
		_css.Should().NotContain("calc(100vh - var(--header-height))");
	}
}
