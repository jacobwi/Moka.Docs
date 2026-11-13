namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	// rtl.css: linked last, and only by pages written right to left, so left-to-right pages keep exactly the
	// base stylesheet's rules. The page's flex and grid rows already run right to left under dir="rtl"; these
	// rules move what the base stylesheet places with left and right, mirror the arrows, and keep code left to right.
	private const string _rtlCss = """

	                               /* Right to left: site.direction rtl, or a language written that way. Every rule is scoped to html[dir="rtl"] inside :where(), so it has the
	                                  specificity of the rule it turns around and wins over it by coming later. */
	                               :where(html[dir="rtl"]) :is(pre, .code-block, .install-panel, .line-numbers) { direction: ltr; text-align: left; }
	                               :where(html[dir="rtl"]) :not(pre) > code, :where(html[dir="rtl"]) kbd { direction: ltr; unicode-bidi: isolate; }
	                               :where(html[dir="rtl"]) :is(.page-nav-arrow, .component-link-card-arrow svg, .site-banner-link svg, .landing-btn-primary svg, .blog-pager-arrow) { transform: scaleX(-1); }

	                               :where(html[dir="rtl"]) .skip-link { left: auto; right: 0.5rem; }
	                               :where(html[dir="rtl"]) .sidebar { border-right: none; border-left: 1px solid var(--color-border); }
	                               :where(html[dir="rtl"]) .nav-link { border-left: none; border-right: 2px solid transparent; }
	                               :where(html[dir="rtl"]) .nav-link.current { border-left: none; border-right: 2px solid var(--color-primary); border-right-color: var(--color-primary) !important; padding-left: 0.75rem; padding-right: calc(0.75rem - 2px); }
	                               :where(html[dir="rtl"]) .nav-link:hover { padding-left: 0.75rem !important; padding-right: calc(0.75rem + 2px) !important; }
	                               :where(html[dir="rtl"]) .nav-sub-section .nav-header { padding-right: 0; }
	                               :where(html[dir="rtl"]) .nav-children { padding-left: 0; padding-right: 1rem; }
	                               :where(html[dir="rtl"]) .nav-sub-section .nav-children { padding-left: 0; padding-right: 0.5rem; }
	                               :where(html[dir="rtl"]) .nav-icon { margin-right: 0; margin-left: 0.375rem; }

	                               :where(html[dir="rtl"]) .page-content :is(ul, ol) { padding-left: 0; padding-right: 1.5rem; }
	                               :where(html[dir="rtl"]) :is(.page-content, .landing-md-content) :is(th, td) { text-align: right; }
	                               :where(html[dir="rtl"]) .page-content blockquote { border-left: none; border-right: 4px solid var(--color-primary); border-radius: var(--radius-sm) 0 0 var(--radius-sm); }
	                               :where(html[dir="rtl"]) .page-content .admonition { border-left-width: 0; border-right-width: 4px; border-right-style: solid; }
	                               /* Same weight as the base rules, so a card still takes the primary color on hover. */
	                               :where(html[dir="rtl"]) .component-card-info { border-left: 1px solid var(--color-border); border-right: 4px solid #3b82f6; }
	                               :where(html[dir="rtl"]) .component-card-success { border-left: 1px solid var(--color-border); border-right: 4px solid #22c55e; }
	                               :where(html[dir="rtl"]) .component-card-warning { border-left: 1px solid var(--color-border); border-right: 4px solid #f59e0b; }
	                               :where(html[dir="rtl"]) .component-link-card:hover .component-link-card-arrow { transform: translateX(-2px); }
	                               :where(html[dir="rtl"]) .heading-anchor { left: auto; right: -1.5rem; }
	                               @media (max-width: 768px) {
	                                   :where(html[dir="rtl"]) .heading-anchor { right: auto; margin-left: 0; margin-right: 0.375rem; }
	                               }

	                               :where(html[dir="rtl"]) .toc-sidebar { border-left: none; border-right: 1px solid var(--color-border); }
	                               :where(html[dir="rtl"]) .toc-list { border-left: none; border-right: 2px solid var(--color-border); margin-left: 0; margin-right: 1rem; }
	                               :where(html[dir="rtl"]) .toc-list ul { padding-right: 0; }
	                               :where(html[dir="rtl"]) .toc-item a { border-left: none !important; border-right: 2px solid transparent !important; margin-left: 0; margin-right: -2px; }
	                               :where(html[dir="rtl"]) .toc-item a.active { border-right-color: var(--color-primary) !important; }
	                               :where(html[dir="rtl"]) .toc-level-3 a { padding-left: 0.75rem; padding-right: 1.5rem; }
	                               :where(html[dir="rtl"]) .toc-level-4 a { padding-left: 0.75rem; padding-right: 2.25rem; }
	                               :where(html[dir="rtl"]) .toc-level-5 a { padding-left: 0.75rem; padding-right: 3rem; }
	                               :where(html[dir="rtl"]) .toc-level-6 a { padding-left: 0.75rem; padding-right: 3.75rem; }
	                               :where(html[dir="rtl"]) .toc-progress-bar { transform-origin: right center; }
	                               :where(html[dir="rtl"]) .toc-item.has-children.collapsed > .toc-caret::before { transform: rotate(90deg); }

	                               :where(html[dir="rtl"]) .page-nav-next { text-align: left; }
	                               :where(html[dir="rtl"]) .page-nav-prev:hover .page-nav-arrow { transform: translateX(2px) scaleX(-1); }
	                               :where(html[dir="rtl"]) .page-nav-next:hover .page-nav-arrow { transform: translateX(-2px) scaleX(-1); }

	                               :where(html[dir="rtl"]) .appearance-panel { right: auto; left: 0; text-align: right; }
	                               :where(html[dir="rtl"]) :is(.appearance-style, .appearance-code-theme) { text-align: right; }
	                               :where(html[dir="rtl"]) .version-dropdown { right: auto; left: 0; }
	                               :where(html[dir="rtl"]) .search-result-section { margin-left: 0; margin-right: 0.25rem; }
	                               :where(html[dir="rtl"]) .lightbox-close { right: auto; left: 0.75rem; }
	                               :where(html[dir="rtl"]) .site-banner-link { margin-left: 0; margin-right: 0.25rem; }
	                               :where(html[dir="rtl"]) .site-banner-close { right: auto; left: 0.75rem; }
	                               :where(html[dir="rtl"]) .blog-author-sep { margin-left: 0; margin-right: 0.375rem; }
	                               :where(html[dir="rtl"]) .page-content .blog-author-avatar { margin-right: 0; margin-left: 0.375rem; }
	                               :where(html[dir="rtl"]) .blog-read-more:hover .blog-pager-arrow, :where(html[dir="rtl"]) .blog-pager-older:hover .blog-pager-arrow { transform: translateX(-2px) scaleX(-1); }
	                               :where(html[dir="rtl"]) .blog-pager-newer:hover .blog-pager-arrow { transform: translateX(2px) scaleX(-1); }

	                               :where(html[dir="rtl"]) .back-to-top { right: auto; left: var(--fab-right); }
	                               @media (max-width: 1200px) {
	                                   :where(html[dir="rtl"]) .toc-sidebar { right: auto; left: 0; border-left: 1px solid var(--color-border); border-right: none; box-shadow: 4px 0 24px rgba(0,0,0,0.1); }
	                                   :where(html[dir="rtl"]) .toc-toggle-btn { right: auto; left: var(--fab-right); }
	                               }
	                               @media (max-width: 768px) {
	                                   :where(html[dir="rtl"]) .sidebar { left: auto; right: 0; }
	                                   :where(html[dir="rtl"]) .version-dropdown { left: auto; right: 0; }
	                                   :where(html[dir="rtl"]) .site-banner-inner { padding: 0.5rem 1rem 0.5rem 3rem; }
	                                   :where(html[dir="rtl"]) .site-banner-text { text-align: right; }
	                               }

	                               html:where([dir="rtl"])[data-breadcrumbs="path"] .breadcrumbs ol::before { margin-right: 0; margin-left: 0.3rem; }
	                               html:where([dir="rtl"])[data-breadcrumbs="back"] .breadcrumbs li:nth-last-child(2) > :is(a, span)::before { content: '\2192'; }
	                               html:where([dir="rtl"])[data-breadcrumbs="arrows"] .breadcrumbs li:not(:last-child)::after { content: '\2190'; content: '\2190' / ''; }
	                               """;

	// After the mirrored rules: where a mirrored copy isn't what a right-to-left page needs.
	private const string _rtlCssAfter = """

	                                    /* Corrections to the mirrored rules: the side a rule leaves alone keeps the padding these links
	                                       already had, and a glossary tip stays centered on its term the way main.js measures it. */
	                                    html:where([dir="rtl"]:not([data-sidebar="pills"]))[data-style="classic"] .nav-children .nav-link,
	                                    html:where([dir="rtl"]:not([data-sidebar="pills"]))[data-style="classic"] .nav-sub-section .nav-children .nav-link { padding-left: 1.25rem !important; }
	                                    html:where([dir="rtl"])[data-style="soft"] :is(.toc-level-3, .toc-level-4, .toc-level-5, .toc-level-6) a { padding-left: 0.75rem; }
	                                    html:where([dir="rtl"])[data-toc="timeline"] :is(.toc-item, .toc-level-3, .toc-level-4, .toc-level-5, .toc-level-6) a { padding-left: 0.75rem; }
	                                    html:where([dir="rtl"])[data-sidebar="lines"] .sidebar .nav-children .nav-link { padding-left: 0.75rem !important; }
	                                    html:where([dir="rtl"])[data-sidebar="pills"][data-style="moka"] .sidebar .nav-link { border-left: 1px solid transparent !important; }
	                                    html:where([dir="rtl"])[data-sidebar="pills"][data-style="brutal"] .sidebar .nav-link { border-left: 2px solid transparent !important; }
	                                    :where(html[dir="rtl"]) .glossary-tip { left: 50%; right: auto; transform: translateX(calc(-50% + var(--tip-shift, 0px))); }
	                                    /* A card's other side has the border the style gives every side of a card. */
	                                    html:where([dir="rtl"])[data-style="soft"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-color: transparent; }
	                                    html:where([dir="rtl"])[data-style="contrast"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 2px; }
	                                    html:where([dir="rtl"])[data-style="glass"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left: 1px solid var(--gl-edge); }
	                                    html:where([dir="rtl"])[data-style="brutal"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 3px; border-left-color: var(--bt-ink); }
	                                    """;
}
