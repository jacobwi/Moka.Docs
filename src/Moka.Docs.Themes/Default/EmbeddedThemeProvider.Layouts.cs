namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	// The layout variants on <html>, written only for a variant other than default, so a site that sets
	// nothing gets no attributes. Both layouts write all of them.
	private const string _layoutAttributes = """
	                                      {{ if theme.layout.width != "default" }} data-width="{{ theme.layout.width | html.escape }}"{{ end }}{{ if theme.layout.navbar != "default" }} data-navbar="{{ theme.layout.navbar | html.escape }}"{{ end }}{{ if theme.layout.footer != "default" }} data-footer="{{ theme.layout.footer | html.escape }}"{{ end }}{{ if theme.layout.toc != "default" }} data-toc="{{ theme.layout.toc | html.escape }}"{{ end }}{{ if theme.layout.breadcrumbs != "default" }} data-breadcrumbs="{{ theme.layout.breadcrumbs | html.escape }}"{{ end }}{{ if theme.layout.page_nav != "default" }} data-page-nav="{{ theme.layout.page_nav | html.escape }}"{{ end }}{{ if theme.layout.sidebar != "default" }} data-sidebar="{{ theme.layout.sidebar | html.escape }}"{{ end }}{{ if theme.layout.sidebar_scope != "default" }} data-sidebar-scope="{{ theme.layout.sidebar_scope | html.escape }}"{{ end }}{{ if theme.layout.home != "default" }} data-home="{{ theme.layout.home | html.escape }}"{{ end }}{{ if theme.layout.hero != "default" }} data-hero="{{ theme.layout.hero | html.escape }}"{{ end }}{{ if theme.layout.hero_background != "default" }} data-hero-background="{{ theme.layout.hero_background | html.escape }}"{{ end }}{{ if theme.layout.features != "default" }} data-features="{{ theme.layout.features | html.escape }}"{{ end }}{{ if theme.layout.admonitions != "default" }} data-admonitions="{{ theme.layout.admonitions | html.escape }}"{{ end }}{{ if theme.layout.tables != "default" }} data-tables="{{ theme.layout.tables | html.escape }}"{{ end }}{{ if theme.layout.density != "default" }} data-density="{{ theme.layout.density | html.escape }}"{{ end }}{{ if theme.layout.font != "default" }} data-font="{{ theme.layout.font | html.escape }}"{{ end }}
	                                      {{- if theme.layout.headings != "default" }} data-headings="{{ theme.layout.headings | html.escape }}"{{ end }}{{ if theme.layout.links != "default" }} data-links="{{ theme.layout.links | html.escape }}"{{ end }}
	                                      """;

	private const string _headerTabs = """

	                                          {{ if (theme.layout.navbar == "tabs" || theme.layout_selector) && layout_tabs.size > 0 }}
	                                          <nav class="header-tabs" aria-label="{{ t.nav_sections | html.escape }}">
	                                              <div class="header-tabs-inner">
	                                                  {{ for tab in layout_tabs }}<a class="header-tab{{ if tab.is_current }} current{{ end }}" href="{{ tab.url | html.escape }}"{{ if tab.is_current }} aria-current="true"{{ end }}>{{ tab.label | html.escape }}</a>{{ end }}
	                                              </div>
	                                          </nav>
	                                          {{ end }}

	                                      """;

	private const string _tocList = """

	                                                      {{ func toc_entries(entries) }}{{ for entry in entries }}{{ if entry.level <= theme.toc_depth }}
	                                                          <li class="toc-item toc-level-{{ entry.level }}">
	                                                              <a href="#{{ entry.id | html.escape }}">{{ entry.text | html.escape }}</a>
	                                                              {{ if entry.children.size > 0 }}<ul>{{ toc_entries(entry.children) }}</ul>{{ end }}
	                                                          </li>
	                                                      {{ end }}{{ end }}{{ end }}
	                                                      <ul class="toc-list">{{ toc_entries(page.toc) }}</ul>

	                                      """;

	private const string _columnsFooter = """

	                                          <footer class="site-footer{{ if theme.layout_selector }} site-footer-columns{{ end }}">
	                                              <div class="footer-columns">
	                                                  <div class="footer-brand">
	                                                      <a class="footer-brand-name" href="{{ base_path | html.escape }}/">{{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="" />{{ end }}<span>{{ site.title | html.escape }}</span></a>
	                                                      {{ if site.description != "" }}<p class="footer-brand-desc">{{ site.description | html.escape }}</p>{{ end }}
	                                                      {{ if theme.social_links.size > 0 }}<div class="footer-social">{{ for link in theme.social_links }}<a href="{{ link.url | html.escape }}" target="_blank" rel="noopener noreferrer" aria-label="{{ link.icon | html.escape }}">{{ link.icon_svg }}</a>{{ end }}</div>{{ end }}
	                                                  </div>
	                                                  {{ if footer_columns.size > 0 }}
	                                                  <nav class="footer-links" aria-label="{{ t.footer_label | html.escape }}">
	                                                      {{ for column in footer_columns }}
	                                                      <div class="footer-col">
	                                                          <p class="footer-col-title">{{ column.title | html.escape }}</p>
	                                                          <ul>
	                                                              {{ for link in column.links }}<li><a href="{{ link.url | html.escape }}">{{ link.label | html.escape }}</a></li>{{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                  </nav>
	                                                  {{ end }}
	                                              </div>
	                                              {{ if site.copyright || theme.show_built_with != false }}
	                                              <div class="footer-bottom">
	                                                  {{ if site.copyright }}<span>{{ site.copyright }}</span>{{ end }}
	                                                  {{ if theme.show_built_with != false }}<span class="built-with">{{ t.footer_built_with | html.escape | string.replace "{mokadocs}" ('<a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v' + mokadocs_version) }}</span>{{ end }}
	                                              </div>
	                                              {{ end }}
	                                          </footer>

	                                      """;

	private const string _layoutsCss = """

	/* Layouts: theme.options.layout. Every rule is scoped to its variant's attribute on <html>, which the
	   layouts only write for a variant other than default. Colors sit in :where() so a style's own
	   colors still win; the rest comes after the styles on purpose, so a variant's shape wins. */
	html[data-width="wide"] { --layout-max-width: 1680px; --content-max-width: 1040px; }
	html[data-width="wider"] { --layout-max-width: 1920px; --content-max-width: 1280px; }
	html[data-width="full"] { --layout-max-width: none; }
	html[data-width="full"] .content { max-width: none; }
	/* A reader who picks a reading width in the Appearance panel still gets it on a full-width site. */
	html[data-width="full"][data-reader-width] .content { max-width: calc(var(--content-max-width) + 5rem); }
	html[data-width="boxed"] {
	    --layout-max-width: 1280px;
	    --boxed-edge: max(0px, (100% - 1280px) / 2);
	}
	html[data-width="boxed"] body {
	    background: linear-gradient(to right,
	        var(--color-bg-secondary) var(--boxed-edge),
	        var(--color-border) var(--boxed-edge) calc(var(--boxed-edge) + 1px),
	        var(--color-bg) calc(var(--boxed-edge) + 1px) calc(100% - var(--boxed-edge) - 1px),
	        var(--color-border) calc(100% - var(--boxed-edge) - 1px) calc(100% - var(--boxed-edge)),
	        var(--color-bg-secondary) calc(100% - var(--boxed-edge)));
	}
	@media (min-width: 1297px) {
	    html[data-width="boxed"]:not([data-navbar="floating"]) .site-header,
	    html[data-width="boxed"] :is(.site-footer, .landing-footer) { max-width: var(--layout-max-width); margin-left: auto; margin-right: auto; }
	    html[data-width="boxed"]:not([data-navbar="floating"]) .site-header { border-left: 1px solid var(--color-border); border-right: 1px solid var(--color-border); }
	}
	@media (max-width: 1296px) {
	    html[data-width="boxed"] body { background: var(--color-bg); }
	}

	html[data-navbar="compact"] { --header-height: 48px; --header-button: 32px; }
	html[data-navbar="compact"] .site-logo { font-size: 1rem; gap: 0.5rem; }
	html[data-navbar="compact"] .site-logo-icon { width: 22px; height: 22px; }
	html[data-navbar="compact"] .site-logo-img { height: 22px; }
	html[data-navbar="compact"] .header-actions { gap: 0.375rem; }
	html[data-navbar="compact"] .site-header .search-trigger { padding-top: 0.25rem; padding-bottom: 0.25rem; font-size: 0.8125rem; }
	html[data-navbar="compact"] .site-header .version-trigger { padding-top: 0.25rem; padding-bottom: 0.25rem; }

	html[data-navbar="floating"] { --header-offset: calc(var(--header-height) + 1.5rem); }
	html[data-navbar="floating"] .site-header {
	    top: 0.75rem;
	    width: calc(100% - 2rem);
	    max-width: calc(var(--layout-max-width) - 2rem);
	    margin: 0.75rem auto 0;
	    border: 1px solid var(--color-border);
	    border-radius: 14px;
	}
	:where(html[data-navbar="floating"]) .site-header {
	    background: color-mix(in srgb, var(--color-bg) 86%, transparent);
	    box-shadow: var(--shadow-md);
	    backdrop-filter: blur(12px);
	}
	html[data-navbar="floating"] .header-inner { padding: 0 1rem; }
	html[data-navbar="floating"] .site-container { margin-top: 0.75rem; }
	@media (max-width: 768px) {
	    html[data-navbar="floating"] { --header-offset: calc(var(--header-height) + 1rem); }
	    html[data-navbar="floating"] .site-header { top: 0.5rem; width: calc(100% - 1rem); margin-top: 0.5rem; }
	    html[data-navbar="floating"] .header-inner { padding: 0 0.625rem; }
	    html[data-navbar="floating"] .site-container { margin-top: 0.5rem; }
	}

	html[data-navbar="tabs"] { --tabs-height: 2.75rem; --header-offset: calc(var(--header-height) + var(--tabs-height)); }
	html[data-navbar="tabs"] .site-header { height: auto; }
	html[data-navbar="tabs"] .header-inner { height: var(--header-height); }
	html[data-navbar="tabs"] .header-tabs { border-top: 1px solid var(--color-border-light, var(--color-border)); }
	html[data-navbar="tabs"] .header-tabs-inner {
	    display: flex;
	    align-items: stretch;
	    gap: 0.25rem;
	    max-width: var(--layout-max-width);
	    height: var(--tabs-height);
	    margin: 0 auto;
	    padding: 0 0.75rem;
	    overflow-x: auto;
	    scrollbar-width: none;
	}
	html[data-navbar="tabs"] .header-tab {
	    display: flex;
	    align-items: center;
	    padding: 0 0.75rem;
	    border-bottom: 2px solid transparent;
	    font-size: 0.875rem;
	    font-weight: 500;
	    white-space: nowrap;
	    text-decoration: none;
	    transition: color var(--transition), border-color var(--transition);
	}
	:where(html[data-navbar="tabs"]) .header-tab { color: var(--color-text-secondary); }
	:where(html[data-navbar="tabs"]) .header-tab:hover { color: var(--color-text); }
	:where(html[data-navbar="tabs"]) .header-tab.current { color: var(--st-link, var(--color-primary-text)); }
	html[data-navbar="tabs"] .header-tab.current { border-bottom-color: currentColor; }
	html[data-navbar="tabs"] .header-tab:focus-visible { outline: 2px solid var(--color-primary); outline-offset: -2px; }
	html[data-style="classic"][data-navbar="tabs"] .header-tabs { border-top-color: var(--cs-bar-line); }
	html[data-style="classic"][data-navbar="tabs"] .header-tab { color: var(--cs-on-bar-muted); }
	html[data-style="classic"][data-navbar="tabs"] :is(.header-tab:hover, .header-tab.current) { color: var(--cs-on-bar); }
	@media (max-width: 768px) {
	    html[data-navbar="tabs"] .header-tabs-inner { padding: 0 0.25rem; }
	}

	html[data-navbar="autohide"] .site-header { transition: transform 0.2s ease; }
	html[data-navbar="autohide"] :is(.sidebar, .toc-sidebar) { transition: top 0.2s ease, height 0.2s ease; }
	html[data-navbar="autohide"].navbar-hidden { --header-offset: 0px; }
	html[data-navbar="autohide"].navbar-hidden .site-header { transform: translateY(-100%); }
	@media (prefers-reduced-motion: reduce) {
	    html[data-navbar="autohide"] :is(.site-header, .sidebar, .toc-sidebar) { transition: none; }
	}

	html[data-footer="minimal"] .site-footer { padding: 1.25rem 1.5rem; }
	html[data-footer="minimal"] .footer-inner {
	    flex-wrap: wrap;
	    align-items: center;
	    justify-content: center;
	    gap: 0.375rem 1.5rem;
	    font-size: 0.75rem;
	}

	html[data-footer="columns"] .site-footer { padding: 3rem 1.5rem 1.5rem; }
	html[data-footer="columns"] .footer-columns {
	    display: flex;
	    flex-wrap: wrap;
	    gap: 2.5rem 3rem;
	    max-width: var(--layout-max-width);
	    margin: 0 auto;
	}
	html[data-footer="columns"] .footer-brand { display: flex; flex: 1 1 220px; flex-direction: column; gap: 0.75rem; max-width: 340px; }
	html[data-footer="columns"] .footer-brand-name { display: flex; align-items: center; gap: 0.5rem; font-size: 1rem; font-weight: 700; text-decoration: none; }
	html[data-footer="columns"] .footer-brand-name img { height: 24px; width: auto; }
	html[data-footer="columns"] .footer-brand-desc { font-size: 0.8125rem; line-height: 1.6; }
	html[data-footer="columns"] .footer-links {
	    display: grid;
	    flex: 3 1 480px;
	    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
	    gap: 2rem;
	}
	html[data-footer="columns"] .footer-col-title {
	    margin-bottom: 0.75rem;
	    font-size: 0.75rem;
	    font-weight: 600;
	    letter-spacing: 0.05em;
	    text-transform: uppercase;
	}
	html[data-footer="columns"] .footer-col ul { display: flex; flex-direction: column; gap: 0.5rem; margin: 0; padding: 0; list-style: none; }
	html[data-footer="columns"] .footer-col a { font-size: 0.8125rem; text-decoration: none; }
	html[data-footer="columns"] .footer-bottom {
	    display: flex;
	    flex-wrap: wrap;
	    justify-content: space-between;
	    gap: 0.5rem 1.5rem;
	    max-width: var(--layout-max-width);
	    margin: 2.5rem auto 0;
	    padding-top: 1.25rem;
	    border-top: 1px solid var(--color-border);
	    font-size: 0.75rem;
	}
	:where(html[data-footer="columns"]) :is(.footer-brand-name, .footer-col-title) { color: var(--color-text); }
	:where(html[data-footer="columns"]) :is(.footer-brand-desc, .footer-bottom) { color: var(--color-text-muted); }
	:where(html[data-footer="columns"]) .footer-col a { color: var(--color-text-secondary); }
	:where(html[data-footer="columns"]) :is(.footer-col a, .footer-brand-name):hover { color: var(--st-link, var(--color-primary-text)); }
	html[data-style="classic"][data-footer="columns"] :is(.footer-brand-desc, .footer-bottom) { color: var(--color-text-secondary); }
	@media (max-width: 768px) {
	    html[data-footer="columns"] .footer-columns { gap: 2rem; }
	    html[data-footer="columns"] .footer-links { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	    html[data-footer="columns"] .footer-bottom { flex-direction: column; align-items: center; text-align: center; }
	}

	html[data-toc="minimal"] .toc-sidebar { border-left-color: transparent; }
	html[data-toc="minimal"] .toc-list { margin-left: 0.25rem; border-left: none; }
	html[data-toc="minimal"] .toc-item a { margin-left: 0; border-left: none; }
	html[data-toc="minimal"] .toc-item a.active { font-weight: 600; }

	html[data-toc="inline"] .toc-inline { margin: 0 0 2rem; border: 1px solid var(--color-border); border-radius: var(--radius); }
	:where(html[data-toc="inline"]) .toc-inline { background: var(--color-bg-secondary); }
	html[data-toc="inline"] .toc-inline > summary {
	    display: flex;
	    align-items: center;
	    justify-content: space-between;
	    padding: 0.75rem 1rem;
	    list-style: none;
	    cursor: pointer;
	    font-size: 0.75rem;
	    font-weight: 600;
	    letter-spacing: 0.05em;
	    text-transform: uppercase;
	    color: var(--color-text);
	}
	html[data-toc="inline"] .toc-inline > summary::-webkit-details-marker { display: none; }
	html[data-toc="inline"] .toc-inline > summary::after {
	    content: '';
	    width: 0.45rem;
	    height: 0.45rem;
	    margin-right: 0.25rem;
	    border-right: 2px solid currentColor;
	    border-bottom: 2px solid currentColor;
	    opacity: 0.6;
	    transform: rotate(45deg);
	    transition: transform var(--transition);
	}
	html[data-toc="inline"] .toc-inline[open] > summary::after { transform: rotate(-135deg); }
	html[data-toc="inline"] .toc-inline .toc { padding: 0 1rem 0.875rem; }
	html[data-toc="inline"] .page-content .toc-inline ul { margin: 0; padding-left: 0; list-style: none; }
	html[data-toc="inline"] .page-content .toc-inline li { margin: 0; }
	html[data-toc="inline"] .page-content .toc-inline .toc-list { margin-left: 0; }

	html[data-toc="floating"]:not(.no-js):has(.toc-toggle-btn) { --fab-stack: calc(2 * var(--fab-size) + var(--fab-gap)); }
	html[data-toc="floating"] .toc-sidebar {
	    display: none;
	    position: fixed;
	    top: auto;
	    right: var(--fab-right);
	    bottom: calc(var(--fab-bottom) + 2 * var(--fab-size) + 2 * var(--fab-gap));
	    left: auto;
	    width: 300px;
	    max-width: calc(100vw - 2rem);
	    height: auto;
	    max-height: min(70vh, 34rem);
	    padding: 1rem 0.5rem;
	    overflow-y: auto;
	    border: 1px solid var(--color-border);
	    border-radius: 12px;
	    z-index: var(--z-drawer);
	}
	:where(html[data-toc="floating"]) .toc-sidebar { background: var(--color-bg); box-shadow: var(--shadow-lg); }
	html[data-toc="floating"] .toc-sidebar.open { display: block; }
	html[data-toc="floating"] .toc-toggle-btn {
	    display: flex;
	    position: fixed;
	    right: var(--fab-right);
	    bottom: calc(var(--fab-bottom) + var(--fab-size) + var(--fab-gap));
	    align-items: center;
	    justify-content: center;
	    width: var(--fab-size);
	    height: var(--fab-size);
	    border: none;
	    cursor: pointer;
	    z-index: var(--z-fab);
	}
	:where(html[data-toc="floating"]) .toc-toggle-btn { border-radius: 50%; background: var(--color-primary-fill); color: var(--color-on-primary); box-shadow: var(--shadow-md); }
	html[data-toc="floating"] .toc-toggle-btn svg { width: 20px; height: 20px; }

	html[data-breadcrumbs="chevrons"] .breadcrumbs li:not(:last-child)::after {
	    content: '';
	    width: 0.4em;
	    height: 0.4em;
	    margin: 0 0.75rem 0 0.5rem;
	    border-top: 1.5px solid currentColor;
	    border-right: 1.5px solid currentColor;
	    opacity: 0.6;
	    transform: rotate(45deg);
	}
	html[data-breadcrumbs="chevrons"] .breadcrumbs [aria-current="page"] { font-weight: 600; }

	html[data-breadcrumbs="pills"] .breadcrumbs li:not(:last-child)::after { content: '\203A'; margin: 0 0.375rem; opacity: 0.6; }
	html[data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) {
	    display: inline-flex;
	    align-items: center;
	    padding: 0.2rem 0.75rem;
	    border: 1px solid var(--color-border);
	    line-height: 1.4;
	}
	:where(html[data-breadcrumbs="pills"]) .breadcrumbs li > :is(a, span) { border-radius: 999px; background: var(--color-bg-secondary); }
	html[data-breadcrumbs="pills"] .breadcrumbs li > a:hover { border-color: var(--color-primary); text-decoration: none; }
	html[data-breadcrumbs="pills"] .breadcrumbs [aria-current="page"] { border-color: color-mix(in srgb, var(--color-primary) 45%, var(--color-border)); }

	html[data-breadcrumbs="path"] .breadcrumbs { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.02em; }
	html[data-breadcrumbs="path"] .breadcrumbs li:not(:last-child)::after { content: '/'; margin: 0 0.3rem; opacity: 0.6; }
	html[data-breadcrumbs="path"] .breadcrumbs ol::before { content: '~'; margin-right: 0.3rem; opacity: 0.7; }
	html[data-breadcrumbs="path"] .breadcrumbs [aria-current="page"] { font-weight: 600; }

	html[data-breadcrumbs="back"] .breadcrumbs li { display: none; }
	html[data-breadcrumbs="back"] .breadcrumbs li:nth-last-child(2) { display: flex; }
	html[data-breadcrumbs="back"] .breadcrumbs li:nth-last-child(2)::after { content: none; }
	html[data-breadcrumbs="back"] .breadcrumbs li:nth-last-child(2) > :is(a, span)::before { content: '\2190'; margin-right: 0.375rem; }

	html[data-page-nav="minimal"] .page-nav { gap: 2rem; padding-top: 0; }
	html[data-page-nav="minimal"] .page-nav-link {
	    padding: 1.25rem 0 0;
	    border: none;
	    border-top: 1px solid var(--color-border);
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	}
	html[data-page-nav="minimal"] .page-nav-link:hover { border-top-color: var(--color-primary); background: none; box-shadow: none; }
	html[data-page-nav="minimal"] .page-nav-link:hover .page-nav-title { text-decoration: underline; text-underline-offset: 3px; }

	html[data-page-nav="stacked"] .page-nav { grid-template-columns: minmax(0, 1fr); gap: 0.75rem; }
	html[data-page-nav="stacked"] .page-nav-spacer { display: none; }

	html[data-sidebar="lines"] .sidebar .nav-children { margin-left: 0.875rem; padding-left: 0; border-left: 1px solid var(--color-border); }
	html[data-sidebar="lines"] .sidebar .nav-children .nav-link {
	    margin-left: -1px;
	    padding-left: 0.875rem !important;
	    border-left: 2px solid transparent;
	    border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
	}
	:where(html[data-sidebar="lines"] .sidebar .nav-children) .nav-link:hover { border-left-color: var(--color-text-muted); background: none; }
	:where(html[data-sidebar="lines"] .sidebar .nav-children) .nav-link.current { background: none; }

	html[data-sidebar="pills"] .sidebar { padding-left: 0.75rem; padding-right: 0.75rem; }
	html[data-sidebar="pills"] .nav-section { margin-bottom: 0.5rem; }
	html[data-sidebar="pills"] .sidebar .nav-link { padding: 0.4375rem 0.875rem !important; border-left: 0 !important; border-radius: 999px; }
	html[data-sidebar="pills"] .sidebar .nav-children { padding-left: 0.625rem; }
	html[data-sidebar="pills"] .sidebar .nav-children > li + li { margin-top: 0.125rem; }
	html[data-sidebar="pills"] .sidebar .nav-link.current,
	html[data-sidebar="pills"] .sidebar .nav-link.current:hover {
	    background: var(--st-link, var(--color-primary-fill)) !important;
	    color: var(--st-on-fill, var(--color-on-primary)) !important;
	    font-weight: 600;
	    box-shadow: none;
	}
	html[data-sidebar="pills"] .sidebar .nav-link.current:focus-visible { outline: 2px solid var(--st-link, var(--color-primary-text)); outline-offset: 2px; }

	html[data-sidebar="compact"] .sidebar { padding-top: 1rem; padding-bottom: 1rem; }
	html[data-sidebar="compact"] .nav-section { margin-bottom: 0.125rem; }
	html[data-sidebar="compact"] .sidebar .nav-link { padding-top: 0.1875rem !important; padding-bottom: 0.1875rem !important; font-size: 0.8125rem; line-height: 1.45; }
	html[data-sidebar="compact"] .sidebar .nav-chevron-btn { padding-top: 0.125rem; padding-bottom: 0.125rem; }
	html[data-sidebar="compact"] .sidebar .nav-icon svg { width: 14px; height: 14px; }

	/* Only the section with the current page; a page outside every section keeps them all. */
	html[data-sidebar-scope="section"] .sidebar-nav:has(> .nav-section.active > .nav-children) > .nav-section:not(.active) { display: none; }

	html[data-hero="split"] .landing-hero-split {
	    display: grid;
	    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
	    align-items: center;
	    gap: 3rem;
	    padding: 5rem max(2rem, calc((100% - 1200px) / 2)) 4rem;
	    text-align: left;
	}
	html[data-hero="split"] .landing-hero-split :is(.landing-hero-title, .landing-hero-subtitle) { max-width: none; margin-left: 0; margin-right: 0; }
	html[data-hero="split"] .landing-hero-split .landing-hero-actions { justify-content: flex-start; }
	html[data-hero="split"] .landing-hero-split .landing-btn { min-width: 0; }
	html[data-hero="split"] .landing-hero-media { min-width: 0; max-width: none; margin: 0; padding: 0; text-align: left; }
	html[data-hero="split"] .landing-hero-media .code-block { max-width: none; margin: 0; }
	html[data-hero="split"] .landing-hero-media .code-block > pre { box-shadow: var(--shadow-lg); }
	html[data-hero="split"] .landing-hero-media img { display: block; width: 100%; height: auto; border: 1px solid var(--color-border); border-radius: var(--radius); box-shadow: var(--shadow-lg); }
	@media (max-width: 900px) {
	    html[data-hero="split"] .landing-hero-split { grid-template-columns: minmax(0, 1fr); gap: 2.5rem; padding: 3.5rem 1.5rem 3rem; }
	}
	@media (max-width: 600px) {
	    html[data-hero="split"] .landing-hero-split { gap: 2rem; padding: 3rem 1rem 2.5rem; }
	}

	html[data-hero="minimal"] .landing-hero { padding: 4.5rem max(2rem, calc((100% - 1200px) / 2)) 3rem; text-align: left; background: none; }
	html[data-hero="minimal"] .landing-hero::before,
	html[data-hero="minimal"] .landing-hero::after,
	html[data-hero="minimal"] .landing-hero .hero-grid-pattern,
	html[data-hero="minimal"] .landing::before { display: none; }
	html[data-hero="minimal"] .landing-hero-title { max-width: 44rem; margin: 0 0 1rem; font-size: clamp(2rem, 4vw, 2.75rem); }
	html[data-hero="minimal"] .landing-hero-subtitle { max-width: 40rem; margin: 0 0 2rem; font-size: 1.125rem; }
	html[data-hero="minimal"] .landing-hero-actions { justify-content: flex-start; }
	html[data-hero="minimal"] .landing-btn { min-width: 0; padding: 0.625rem 1.375rem; font-size: 0.9375rem; }
	@media (max-width: 600px) {
	    html[data-hero="minimal"] .landing-hero { padding: 2.5rem 1rem 2rem; }
	}

	/* The hero backdrop is the pattern element and the hero's two pseudo-elements, which the styles restyle. */
	html[data-hero-background="dots"] .landing-hero::before,
	html[data-hero-background="dots"] .landing-hero::after,
	html[data-hero-background="dots"] .landing::before { display: none; }
	html[data-hero-background="dots"] .landing-hero .hero-grid-pattern {
	    display: block;
	    opacity: 1;
	    background-image: radial-gradient(circle, color-mix(in srgb, var(--color-text) 24%, transparent) 1.25px, transparent 1.75px);
	    background-size: 22px 22px;
	    -webkit-mask-image: radial-gradient(ellipse 80% 75% at 50% 45%, #000 25%, transparent 100%);
	    mask-image: radial-gradient(ellipse 80% 75% at 50% 45%, #000 25%, transparent 100%);
	}

	html[data-hero-background="gradient"] .landing-hero::after,
	html[data-hero-background="gradient"] .landing-hero .hero-grid-pattern,
	html[data-hero-background="gradient"] .landing::before { display: none; }
	html[data-hero-background="gradient"] .landing-hero::before {
	    content: '';
	    display: block;
	    background:
	        radial-gradient(55% 75% at 12% 15%, color-mix(in srgb, var(--color-primary) 16%, transparent), transparent 70%),
	        radial-gradient(50% 70% at 88% 25%, color-mix(in srgb, var(--gradient-secondary) 14%, transparent), transparent 70%),
	        radial-gradient(60% 65% at 50% 110%, color-mix(in srgb, var(--gradient-tertiary) 12%, transparent), transparent 70%) !important;
	    background-size: auto !important;
	    animation: none;
	}
	html[data-hero-background="gradient"] .landing-hero-subtitle { color: color-mix(in srgb, var(--color-text-secondary) 60%, var(--color-text)); }

	html[data-hero-background="none"] .landing-hero { background: none; }
	html[data-hero-background="none"] .landing-hero::before,
	html[data-hero-background="none"] .landing-hero::after,
	html[data-hero-background="none"] .landing-hero .hero-grid-pattern,
	html[data-hero-background="none"] .landing::before { display: none; }

	html[data-admonitions="filled"] .page-content .admonition {
	    padding: 0;
	    overflow: hidden;
	    border: 1px solid color-mix(in srgb, var(--ad-c, var(--color-border)) 35%, var(--color-border));
	    border-radius: var(--radius);
	    background: color-mix(in srgb, var(--ad-c, var(--color-border)) 7%, var(--color-bg));
	    box-shadow: none;
	}
	html[data-admonitions="filled"] .page-content .admonition-title {
	    margin: 0;
	    padding: 0.5rem 1rem;
	    border: none;
	    background: var(--ad-ink, var(--color-text));
	    color: var(--color-bg);
	    font-size: 0.875rem;
	    font-weight: 700;
	}
	html[data-admonitions="filled"] .page-content .admonition-content { padding: 0.875rem 1rem; }

	html[data-admonitions="minimal"] .page-content .admonition {
	    padding: 0.125rem 0 0.125rem 1rem;
	    border: none;
	    border-left: 3px solid var(--ad-c, var(--color-border));
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	}
	html[data-admonitions="minimal"] .page-content .admonition-icon { display: none; }
	html[data-admonitions="minimal"] .page-content .admonition-title {
	    margin: 0 0 0.25rem;
	    padding: 0;
	    border: none;
	    background: none;
	    font-size: 0.8125rem;
	    letter-spacing: 0.05em;
	    text-transform: uppercase;
	}

	html[data-admonitions="github"] .page-content .admonition {
	    padding: 0.5rem 1rem;
	    border: none;
	    border-left: 0.25em solid var(--ad-c, var(--color-border));
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	}
	html[data-admonitions="github"] .page-content .admonition-icon { display: none; }
	html[data-admonitions="github"] .page-content .admonition-title {
	    margin: 0 0 0.375rem;
	    padding: 0;
	    border: none;
	    background: none;
	    color: var(--ad-ink, var(--color-text));
	    font-size: 1rem;
	    font-weight: 600;
	    font-variant-caps: normal;
	    letter-spacing: normal;
	    line-height: 1.5;
	    text-transform: none;
	}
	html[data-admonitions="github"] .page-content .admonition-title::before {
	    content: '';
	    flex-shrink: 0;
	    width: 1em;
	    height: 1em;
	    background: currentColor;
	    -webkit-mask: var(--ad-icon, none) center / contain no-repeat;
	    mask: var(--ad-icon, none) center / contain no-repeat;
	}
	html[data-admonitions="github"] :is(.admonition-note, .admonition-info) { --ad-icon: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Ccircle cx='12' cy='12' r='10'/%3E%3Cpath d='M12 16v-4M12 8h.01'/%3E%3C/svg%3E"); }
	html[data-admonitions="github"] .admonition-tip { --ad-icon: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5M9 18h6M10 22h4'/%3E%3C/svg%3E"); }
	html[data-admonitions="github"] .admonition-important { --ad-icon: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2zM12 7v2M12 13h.01'/%3E%3C/svg%3E"); }
	html[data-admonitions="github"] :is(.admonition-warning, .admonition-caution) { --ad-icon: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3M12 9v4M12 17h.01'/%3E%3C/svg%3E"); }
	html[data-admonitions="github"] .admonition-danger { --ad-icon: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='M8.69 2h6.62a2 2 0 0 1 1.42.59l4.68 4.68A2 2 0 0 1 22 8.69v6.62a2 2 0 0 1-.59 1.42l-4.68 4.68a2 2 0 0 1-1.42.59H8.69a2 2 0 0 1-1.42-.59l-4.68-4.68A2 2 0 0 1 2 15.31V8.69a2 2 0 0 1 .59-1.42l4.68-4.68A2 2 0 0 1 8.69 2zM12 8v4M12 16h.01'/%3E%3C/svg%3E"); }
	@media (forced-colors: active) {
	    html[data-admonitions="github"] .page-content .admonition-title::before { forced-color-adjust: none; background: CanvasText; }
	}

	html[data-tables="striped"] :is(.page-content, .landing-md-content) tbody tr:nth-child(even) td { background: color-mix(in srgb, var(--color-text) 4%, transparent); }
	html[data-tables="striped"] :is(.page-content, .landing-md-content) tbody tr:hover td { background: color-mix(in srgb, var(--color-text) 8%, transparent); }

	html[data-tables="bordered"] :is(.page-content, .landing-md-content) table {
	    width: max-content;
	    max-width: 100%;
	    border: 1px solid var(--color-border);
	    border-collapse: separate;
	    border-spacing: 0;
	    border-radius: var(--radius);
	}
	html[data-tables="bordered"] :is(.page-content, .landing-md-content) :is(th, td) { border: none; border-right: 1px solid var(--color-border); border-bottom: 1px solid var(--color-border); }
	html[data-tables="bordered"] :is(.page-content, .landing-md-content) :is(th, td):last-child { border-right: none; }
	html[data-tables="bordered"] :is(.page-content, .landing-md-content) tr:last-child > td { border-bottom: none; }

	html[data-tables="minimal"] :is(.page-content, .landing-md-content) table { border: none; border-radius: 0; background: none; }
	html[data-tables="minimal"] :is(.page-content, .landing-md-content) :is(th, td) { padding-left: 0; padding-right: 1.5rem; border: none; }
	html[data-tables="minimal"] :is(.page-content, .landing-md-content) th { border-bottom: 2px solid var(--color-border); background: none; color: var(--color-text); }
	html[data-tables="minimal"] :is(.page-content, .landing-md-content) tr:hover td { background: none; }

	html[data-density="compact"] .content { padding-top: 1.5rem; padding-bottom: 1.5rem; }
	html[data-density="compact"] .page-content { line-height: 1.6; }
	html[data-density="compact"] .page-content :is(p, ul, ol) { margin-bottom: 0.75rem; }
	html[data-density="compact"] .page-content li { margin-bottom: 0.125rem; }
	html[data-density="compact"] .page-content h2 { margin: 2rem 0 0.75rem; }
	html[data-density="compact"] .page-content h3 { margin: 1.5rem 0 0.5rem; }
	html[data-density="compact"] .page-content h4 { margin: 1.25rem 0 0.375rem; }
	html[data-density="compact"] .page-content :is(pre, .code-block, .admonition, blockquote, table, .tabs) { margin-top: 1rem; margin-bottom: 1rem; }
	html[data-density="compact"] .sidebar { padding-top: 1rem; padding-bottom: 1rem; }
	html[data-density="compact"] .nav-section { margin-bottom: 0.125rem; }
	html[data-density="compact"] .nav-link { padding-top: 0.25rem !important; padding-bottom: 0.25rem !important; }
	html[data-density="compact"] .toc-sidebar { padding-top: 1rem; padding-bottom: 1rem; }
	html[data-density="compact"] .toc-item a { padding-top: 0.1875rem; padding-bottom: 0.1875rem; }

	html[data-density="spacious"] .content { padding-top: 2.75rem; padding-bottom: 2.75rem; }
	html[data-density="spacious"] .page-content { line-height: 1.8; }
	html[data-density="spacious"] .page-content :is(p, ul, ol) { margin-bottom: 1.375rem; }
	html[data-density="spacious"] .page-content li { margin-bottom: 0.375rem; }
	html[data-density="spacious"] .page-content h2 { margin: 3.25rem 0 1.25rem; }
	html[data-density="spacious"] .page-content h3 { margin: 2.5rem 0 1rem; }
	html[data-density="spacious"] .page-content h4 { margin: 2rem 0 0.75rem; }
	html[data-density="spacious"] .page-content :is(pre, .code-block, .admonition, blockquote, table, .tabs) { margin-top: 2rem; margin-bottom: 2rem; }
	html[data-density="spacious"] .sidebar { padding-top: 2rem; padding-bottom: 2rem; }
	html[data-density="spacious"] .nav-section { margin-bottom: 0.5rem; }
	html[data-density="spacious"] .nav-link { padding-top: 0.5rem !important; padding-bottom: 0.5rem !important; }
	html[data-density="spacious"] .toc-sidebar { padding-top: 2rem; padding-bottom: 2rem; }
	html[data-density="spacious"] .toc-item a { padding-top: 0.375rem; padding-bottom: 0.375rem; }

	html[data-font="system"] { --font-body: system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', 'Noto Sans', Arial, sans-serif; }
	html[data-font="serif"] { --font-body: 'Iowan Old Style', 'Sitka Text', Charter, 'Bitstream Charter', 'Palatino Linotype', Palatino, Georgia, Cambria, serif; }
	html[data-font="mono"] { --font-body: var(--font-mono); }

	html[data-width="narrow"] { --content-max-width: 640px; }
	html[data-width="narrow"] .content { margin-left: auto; margin-right: auto; }

	/* The logo sits in the middle column of a grid, so it stays centered whatever the actions measure. */
	@media (min-width: 769px) {
	    html[data-navbar="centered"] .header-inner {
	        display: grid;
	        position: relative;
	        grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
	        gap: 1rem;
	    }
	    html[data-navbar="centered"] .site-logo { grid-column: 2; grid-row: 1; min-width: 0; max-width: min(32rem, 44vw); }
	    html[data-navbar="centered"] .site-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	    html[data-navbar="centered"] .header-actions { grid-column: 3; grid-row: 1; justify-self: end; }
	    html[data-navbar="centered"] .header-actions > .search-trigger { position: absolute; top: 50%; left: 1.5rem; transform: translateY(-50%); }
	}

	/* Over the page, not on it: what scrolls under the bar shows through, blurred, behind a tint strong
	   enough to keep the bar's text at 4.5:1 and its icons at 3:1 over any content. */
	html[data-navbar="transparent"] .site-header {
	    border-bottom-color: transparent;
	    background: color-mix(in srgb, var(--color-bg) 80%, transparent);
	    box-shadow: none;
	    -webkit-backdrop-filter: blur(16px) saturate(1.6);
	    backdrop-filter: blur(16px) saturate(1.6);
	}
	html[data-style="classic"][data-navbar="transparent"] .site-header { background: color-mix(in srgb, var(--cs-bar) 88%, transparent); }
	@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
	    html[data-navbar="transparent"] .site-header { background: var(--color-bg); }
	    html[data-style="classic"][data-navbar="transparent"] .site-header { background: var(--cs-bar); }
	}
	@media (prefers-reduced-transparency: reduce) {
	    html[data-navbar="transparent"] .site-header { border-bottom-color: var(--color-border); background: var(--color-bg); -webkit-backdrop-filter: none; backdrop-filter: none; }
	    html[data-style="classic"][data-navbar="transparent"] .site-header { background: var(--cs-bar); }
	}

	html[data-footer="centered"] .site-footer { padding: 2.5rem 1.5rem; }
	html[data-footer="centered"] .footer-inner { flex-direction: column; align-items: center; gap: 1rem; text-align: center; }
	html[data-footer="centered"] .footer-social { order: -1; gap: 1.25rem; }
	html[data-footer="centered"] .footer-social svg { width: 20px; height: 20px; }
	html[data-footer="centered"] .landing-footer { padding: 2.5rem 1.5rem; }

	html[data-footer="compact"] .site-footer { padding: 0.625rem 1.5rem; }
	html[data-footer="compact"] .footer-inner { flex-direction: row; flex-wrap: wrap; align-items: center; justify-content: flex-start; gap: 0.125rem 0; font-size: 0.75rem; }
	html[data-footer="compact"] .footer-inner > * + *::before,
	html[data-footer="compact"] .landing-footer p + p::before { content: '\00B7'; content: '\00B7' / ''; margin: 0 0.625rem; font-weight: 700; }
	html[data-footer="compact"] .footer-social { gap: 0.5rem; }
	html[data-footer="compact"] .footer-social svg { width: 14px; height: 14px; }
	html[data-footer="compact"] .landing-footer { padding: 0.75rem 1rem; font-size: 0.75rem; }
	html[data-footer="compact"] .landing-footer p { display: inline; margin: 0 !important; }
	@media (max-width: 768px) {
	    html[data-footer="compact"] .footer-inner { justify-content: center; }
	}

	html[data-toc="numbered"] :is(.toc-list, .toc-list ul) { counter-reset: toc-entry; }
	html[data-toc="numbered"] .toc-item { counter-increment: toc-entry; }
	html[data-toc="numbered"] .toc-item > a { display: flex; gap: 0.5em; }
	html[data-toc="numbered"] .toc-item > a::before {
	    content: counters(toc-entry, '.');
	    content: counters(toc-entry, '.') / '';
	    flex-shrink: 0;
	    font-variant-numeric: tabular-nums;
	}
	html[data-style="terminal"][data-toc="numbered"] .toc-item > a.active::before { content: counters(toc-entry, '.'); content: counters(toc-entry, '.') / ''; }

	/* The list's left border is the line; each link puts its dot on it. */
	html[data-toc="timeline"] .toc-list { margin-left: 1.25rem; border-left: 2px solid var(--color-border); }
	html[data-toc="timeline"] .toc-item a {
	    position: relative;
	    margin-left: -2px;
	    padding-left: 1rem;
	    border-left: 2px solid transparent !important;
	}
	html[data-toc="timeline"] .toc-level-3 a { padding-left: 1.75rem; }
	html[data-toc="timeline"] .toc-level-4 a { padding-left: 2.5rem; }
	html[data-toc="timeline"] :is(.toc-level-5, .toc-level-6) a { padding-left: 3.25rem; }
	html[data-toc="timeline"] .toc-item a::after {
	    content: '';
	    position: absolute;
	    top: calc(0.25rem + 0.7em - 5px);
	    left: -6px;
	    width: 10px;
	    height: 10px;
	    box-sizing: border-box;
	    border: 2px solid var(--color-text-muted);
	    border-radius: 50%;
	    background: var(--color-bg);
	}
	html[data-toc="timeline"] .toc-item:not(.toc-level-2) > a::after { top: calc(0.25rem + 0.7em - 4px); left: -5px; width: 8px; height: 8px; }
	html[data-toc="timeline"] .toc-item a.active { background: none; }
	html[data-toc="timeline"] .toc-item a.active::after { border-color: var(--st-link, var(--color-primary-text)); background: var(--st-link, var(--color-primary-text)); }

	@media (min-width: 1201px) {
	    html[data-toc="bordered"] { --toc-width: 248px; }
	    html[data-toc="bordered"] .toc-sidebar { padding: 1.5rem 1.25rem 1.5rem 0.5rem; border-left-color: transparent; }
	    html[data-toc="bordered"] .toc-sidebar > .toc { padding: 1rem 0.25rem 0.75rem 0; border: 1px solid var(--color-border); border-radius: var(--radius-md, var(--radius)); }
	    :where(html[data-toc="bordered"]) .toc-sidebar > .toc { background: var(--color-bg); box-shadow: var(--shadow-sm); }
	}

	html[data-breadcrumbs="arrows"] .breadcrumbs li:not(:last-child)::after { content: '\2192'; content: '\2192' / ''; margin: 0 0.5rem; opacity: 0.8; }
	html[data-breadcrumbs="arrows"] .breadcrumbs [aria-current="page"] { font-weight: 600; }

	html[data-breadcrumbs="dots"] .breadcrumbs li:not(:last-child)::after { content: '\00B7'; content: '\00B7' / ''; margin: 0 0.5rem; font-weight: 700; opacity: 0.7; }
	:where(html[data-breadcrumbs="dots"]) .breadcrumbs a { color: var(--color-text-muted); }
	html[data-breadcrumbs="dots"] .breadcrumbs [aria-current="page"] { font-weight: 400; }

	html[data-page-nav="split"] .page-nav {
	    grid-template-columns: repeat(2, minmax(0, 1fr));
	    gap: 1rem;
	    padding: 0.875rem 0;
	    border-top: 1px solid var(--color-border);
	    border-bottom: 1px solid var(--color-border);
	}
	html[data-page-nav="split"] .page-nav-spacer { display: block; }
	html[data-page-nav="split"] .page-nav-link {
	    padding: 0.25rem 0;
	    border: none;
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	    -webkit-backdrop-filter: none;
	    backdrop-filter: none;
	}
	html[data-page-nav="split"] .page-nav-link:hover { background: none; box-shadow: none; }
	html[data-page-nav="split"] .page-nav-link:hover .page-nav-title { text-decoration: underline; text-underline-offset: 3px; }

	html[data-page-nav="compact"] .page-nav { display: flex; justify-content: space-between; gap: 1rem; margin-top: 2rem; }
	html[data-page-nav="compact"] .page-nav-spacer { display: block; }
	html[data-page-nav="compact"] .page-nav-link {
	    gap: 0.375rem;
	    min-width: 0;
	    padding: 0;
	    border: none;
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	    -webkit-backdrop-filter: none;
	    backdrop-filter: none;
	}
	html[data-page-nav="compact"] .page-nav-link:hover { background: none; box-shadow: none; }
	html[data-page-nav="compact"] .page-nav-text { flex-direction: row; align-items: baseline; gap: 0.375rem; }
	html[data-page-nav="compact"] .page-nav-label { font-size: 0.75rem; letter-spacing: normal; text-transform: none; }
	html[data-page-nav="compact"] .page-nav-label::after { content: ':'; }
	html[data-page-nav="compact"] .page-nav-title { font-size: 0.875rem; font-style: normal; }
	html[data-page-nav="compact"] .page-nav-link:hover .page-nav-title { text-decoration: underline; text-underline-offset: 3px; }
	html[data-page-nav="compact"] .page-nav-arrow { width: 14px; height: 14px; }

	html[data-sidebar="accent"] .sidebar .nav-link.current,
	html[data-sidebar="accent"] .sidebar .nav-link.current:hover {
	    border-color: transparent !important;
	    border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
	    background: none !important;
	    box-shadow: inset 3px 0 0 var(--st-link, var(--color-primary-text)) !important;
	    color: var(--st-link, var(--color-primary-text)) !important;
	}

	/* Only the top-level entries with entries below them are sections; a lone page stays a plain row. */
	html[data-sidebar="cards"] .sidebar-nav > .nav-section:has(> .nav-header) {
	    margin-bottom: 0.625rem;
	    padding: 0.25rem;
	    border: 1px solid var(--color-border);
	    border-radius: var(--radius-md, var(--radius));
	}
	:where(html[data-sidebar="cards"]) .sidebar-nav > .nav-section:has(> .nav-header) { background: var(--color-bg); box-shadow: var(--shadow-sm); }
	html[data-sidebar="cards"] .sidebar-nav > .nav-section:not(:has(> .nav-header)) { margin-bottom: 0.625rem; }

	html[data-sidebar="minimal"] .sidebar .nav-icon { display: none; }
	html[data-sidebar="minimal"] .sidebar .nav-link,
	html[data-sidebar="minimal"] .sidebar .nav-link:hover {
	    border-color: transparent !important;
	    background: none !important;
	    box-shadow: none !important;
	}
	html[data-sidebar="minimal"] .sidebar .nav-link:hover { text-decoration: underline; text-underline-offset: 0.25em; }
	html[data-sidebar="minimal"] .sidebar .nav-link.current {
	    border-color: transparent !important;
	    color: var(--color-text) !important;
	    font-weight: 700;
	    text-decoration: underline;
	    text-decoration-color: var(--st-link, var(--color-primary-text));
	    text-decoration-thickness: 2px;
	    text-underline-offset: 0.3em;
	}

	/* A flat band in the accent fill, whatever heroBackground says: a backdrop would take the text on it
	   under 4.5:1. */
	html[data-hero="banner"] .landing-hero {
	    padding: 4.5rem max(1.5rem, calc((100% - 1100px) / 2)) 4rem;
	    border: none;
	    background: var(--st-link, var(--color-primary-fill));
	    box-shadow: none;
	    color: var(--st-on-fill, var(--color-on-primary));
	}
	html[data-hero="banner"] .landing-hero::before,
	html[data-hero="banner"] .landing-hero::after,
	html[data-hero="banner"] .landing::before,
	html[data-hero="banner"][data-hero-background] .landing-hero::before,
	html[data-hero="banner"] .landing-hero .hero-grid-pattern,
	html[data-hero="banner"][data-hero-background] .landing-hero .hero-grid-pattern { display: none; }
	html[data-hero="banner"] .landing-hero .landing-hero-title {
	    background: none;
	    color: inherit;
	    -webkit-text-fill-color: currentColor;
	    text-shadow: none;
	}
	html[data-hero="banner"] .landing-hero .landing-hero-subtitle { color: inherit; }
	html[data-hero="banner"] .landing-hero .landing-hero-icon { border-color: transparent; background: color-mix(in srgb, currentColor 16%, transparent); box-shadow: none; color: inherit; }
	html[data-hero="banner"] .landing-hero :is(.landing-btn-primary, .landing-btn-primary:hover) {
	    border-color: transparent;
	    background: var(--st-on-fill, var(--color-on-primary));
	    box-shadow: none;
	    color: var(--st-link, var(--color-primary-fill));
	}
	html[data-hero="banner"] .landing-hero :is(.landing-btn-secondary, .landing-btn-secondary:hover) {
	    border: 1px solid color-mix(in srgb, currentColor 60%, transparent);
	    background: transparent;
	    box-shadow: none;
	    color: var(--st-on-fill, var(--color-on-primary));
	}
	html[data-hero="banner"] .landing-hero .landing-btn:focus-visible { outline: 2px solid var(--st-on-fill, var(--color-on-primary)); outline-offset: 3px; }
	@media (max-width: 600px) {
	    html[data-hero="banner"] .landing-hero { padding: 3rem 1rem 2.5rem; }
	}

	/* Blurred blobs toward the edges, where the hero has no text, faint enough to keep it at AA. */
	html[data-hero-background="mesh"] .landing-hero::after,
	html[data-hero-background="mesh"] .landing-hero .hero-grid-pattern,
	html[data-hero-background="mesh"] .landing::before { display: none; }
	html[data-hero-background="mesh"] .landing-hero::before {
	    content: '';
	    display: block;
	    inset: -10%;
	    background:
	        radial-gradient(30% 40% at 10% 20%, color-mix(in srgb, var(--color-primary) 18%, transparent), transparent 70%),
	        radial-gradient(28% 38% at 90% 15%, color-mix(in srgb, var(--gradient-secondary) 17%, transparent), transparent 70%),
	        radial-gradient(34% 40% at 75% 95%, color-mix(in srgb, var(--gradient-tertiary) 15%, transparent), transparent 70%),
	        radial-gradient(26% 34% at 20% 95%, color-mix(in srgb, var(--color-accent) 10%, transparent), transparent 70%) !important;
	    background-size: auto !important;
	    filter: blur(48px);
	    animation: none;
	}
	html[data-hero-background="mesh"] .landing-hero-subtitle { color: color-mix(in srgb, var(--color-text-secondary) 40%, var(--color-text)); }

	html[data-hero-background="lines"] .landing-hero::before,
	html[data-hero-background="lines"] .landing-hero::after,
	html[data-hero-background="lines"] .landing::before { display: none; }
	html[data-hero-background="lines"] .landing-hero .hero-grid-pattern {
	    display: block;
	    opacity: 1;
	    background-image: repeating-linear-gradient(135deg, color-mix(in srgb, var(--color-text) 8%, transparent) 0 1px, transparent 1px 14px);
	    background-size: auto;
	    -webkit-mask-image: radial-gradient(ellipse 85% 80% at 50% 40%, #000 20%, transparent 100%);
	    mask-image: radial-gradient(ellipse 85% 80% at 50% 40%, #000 20%, transparent 100%);
	}
	html[data-hero-background="lines"] .landing-hero-subtitle { color: color-mix(in srgb, var(--color-text-secondary) 55%, var(--color-text)); }

	/* Home page variants come after the hero's, so their spacing wins over a hero variant's while the
	   hero variant keeps its alignment and colors. */
	html[data-home="portal"] .landing-hero { padding: 3.5rem max(1.5rem, calc((100% - 1100px) / 2)) 3rem; }
	html[data-home="portal"] .landing-hero-title { margin-bottom: 0.75rem; font-size: clamp(2rem, 4.5vw, 3rem); }
	html[data-home="portal"] .landing-hero-subtitle { margin-bottom: 1.75rem; font-size: 1.0625rem; }
	html[data-home="portal"] .home-search {
	    display: flex;
	    align-items: center;
	    gap: 0.75rem;
	    width: 100%;
	    max-width: 40rem;
	    margin: 0 auto;
	    padding: 0.875rem 1rem 0.875rem 1.125rem;
	    border: 1px solid var(--color-border);
	    border-radius: var(--radius);
	    background: var(--color-bg);
	    box-shadow: var(--shadow-md);
	    color: var(--color-text-secondary);
	    font: inherit;
	    font-size: 1.0625rem;
	    text-align: left;
	    cursor: text;
	    transition: border-color var(--transition), box-shadow var(--transition);
	}
	html[data-home="portal"] .home-search:hover { border-color: var(--st-link, var(--color-primary)); }
	html[data-home="portal"] .home-search:focus-visible { outline: 2px solid var(--st-link, var(--color-primary-text)); outline-offset: 2px; }
	html[data-home="portal"] .home-search svg { flex-shrink: 0; color: var(--color-text-secondary); }
	html[data-home="portal"] .home-search-label { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	html[data-home="portal"] .home-search kbd {
	    flex-shrink: 0;
	    padding: 0.125rem 0.5rem;
	    border: 1px solid var(--color-border);
	    border-radius: var(--radius-sm);
	    background: transparent;
	    color: var(--color-text-secondary);
	    font-family: var(--font-mono);
	    font-size: 0.75rem;
	}
	html[data-home="portal"][data-hero="minimal"] .home-search { margin-left: 0; }
	html[data-style="brutal"][data-home="portal"] .home-search { border: 3px solid var(--bt-ink); }
	html[data-style="terminal"][data-home="portal"] .home-search { border-radius: 0; box-shadow: none; }
	html[data-home="portal"][data-hero="banner"] .home-search { border-color: transparent; }
	html[data-home="portal"][data-hero="banner"] .home-search:focus-visible { outline-color: var(--st-on-fill, var(--color-on-primary)); }
	html[data-home="portal"] .home-sections {
	    display: grid;
	    grid-template-columns: repeat(auto-fill, minmax(min(100%, 16rem), 1fr));
	    gap: 1.25rem;
	    max-width: 1100px;
	    margin: 0 auto;
	    padding: 3rem 2rem;
	}
	html[data-home="portal"] .home-section { padding: 1.5rem; overflow-wrap: anywhere; }
	/* The card holds several links, so it doesn't lift as if it were one. */
	html[data-home="portal"] .home-section:hover { transform: none !important; }
	html[data-home="portal"] .home-section-title { margin: 0 0 0.875rem; padding: 0; border: none; font-size: 1.0625rem; font-weight: 650; line-height: 1.35; }
	html[data-home="portal"] .home-section-title a { display: inline-flex; align-items: center; gap: 0.75rem; color: var(--color-text); text-decoration: none; }
	html[data-home="portal"] .home-section-title a:hover { color: var(--st-link, var(--color-primary-text)); text-decoration: underline; text-underline-offset: 0.2em; }
	html[data-home="portal"] .home-section-icon {
	    display: inline-flex;
	    flex-shrink: 0;
	    align-items: center;
	    justify-content: center;
	    width: 2.25rem;
	    height: 2.25rem;
	    border-radius: var(--radius);
	    background: color-mix(in srgb, var(--color-primary) 10%, transparent);
	    color: var(--st-link, var(--color-primary-text));
	}
	html[data-home="portal"] .home-section-icon svg { width: 18px; height: 18px; }
	html[data-home="portal"] .home-section-links { display: grid; gap: 0.125rem; margin: 0; padding: 0; list-style: none; }
	html[data-home="portal"] .home-section-links li { margin: 0; }
	html[data-home="portal"] .home-section-links a { display: block; padding: 0.25rem 0; color: var(--color-text-secondary); font-size: 0.9375rem; text-decoration: none; }
	html[data-home="portal"] .home-section-links a:hover { color: var(--st-link, var(--color-primary-text)); text-decoration: underline; text-underline-offset: 0.2em; }
	@media (max-width: 600px) {
	    html[data-home="portal"] .landing-hero { padding: 2.5rem 1rem 2rem; }
	    html[data-home="portal"] .home-search { padding: 0.75rem 0.875rem; font-size: 1rem; }
	    html[data-home="portal"] .home-search kbd { display: none; }
	    html[data-home="portal"] .home-sections { padding: 2rem 1rem; gap: 1rem; }
	}

	html[data-home="showcase"] .home-showcase {
	    display: grid;
	    grid-template-columns: minmax(0, 2fr) minmax(0, 3fr);
	    align-items: center;
	    gap: 3rem;
	    padding: 4rem max(2rem, calc((100% - 1100px) / 2));
	    border-top: 1px solid var(--color-border);
	    border-bottom: 1px solid var(--color-border);
	    background: var(--color-bg-secondary);
	}
	html[data-home="showcase"] .home-showcase:not(:has(.home-showcase-text)) { grid-template-columns: minmax(0, 1fr); }
	html[data-home="showcase"] .home-showcase:not(:has(.home-showcase-text)) .home-showcase-code { width: 100%; max-width: 720px; margin: 0 auto; }
	html[data-home="showcase"] .home-showcase-plain { grid-template-columns: minmax(0, 1fr); text-align: center; }
	html[data-home="showcase"] .home-showcase-plain .home-showcase-text { max-width: 44rem; margin: 0 auto; }
	html[data-home="showcase"] .home-showcase-title { margin: 0 0 0.75rem; padding: 0; border: none; font-size: 1.75rem; font-weight: 700; letter-spacing: -0.02em; line-height: 1.25; }
	html[data-home="showcase"] .home-showcase-desc { margin: 0; color: color-mix(in srgb, var(--color-text-secondary) 70%, var(--color-text)); font-size: 1.0625rem; line-height: 1.7; }
	html[data-home="showcase"] .home-showcase-code { min-width: 0; max-width: none; margin: 0; padding: 0; }
	html[data-home="showcase"] .home-showcase-code .code-block { max-width: none; margin: 0; }
	html[data-home="showcase"] .home-showcase-code .code-block > pre { box-shadow: var(--shadow-lg); }
	html[data-home="showcase"] .home-stats { max-width: 1100px; margin: 0 auto; padding: 3.5rem 2rem; }
	html[data-home="showcase"] .home-stats-list {
	    display: grid;
	    grid-template-columns: repeat(auto-fit, minmax(min(100%, 8rem), 1fr));
	    gap: 2rem 1.5rem;
	    margin: 0;
	    padding: 0;
	    list-style: none;
	    text-align: center;
	}
	html[data-home="showcase"] .home-stat { display: flex; flex-direction: column; gap: 0.375rem; margin: 0; }
	html[data-home="showcase"] .home-stat-value {
	    color: var(--st-link, var(--color-primary-text));
	    font-size: clamp(2rem, 4vw, 2.75rem);
	    font-weight: 800;
	    font-variant-numeric: tabular-nums;
	    letter-spacing: -0.02em;
	    line-height: 1.1;
	}
	html[data-home="showcase"] .home-stat-label { color: var(--color-text-secondary); font-size: 0.9375rem; line-height: 1.4; }
	/* The accent fill and the text color made for it, like hero: banner, so it reads with every preset. */
	html[data-home="showcase"] .home-cta {
	    width: calc(100% - 4rem);
	    max-width: 1100px;
	    margin: 1rem auto 4rem;
	    padding: 3rem 2rem;
	    border-radius: var(--radius-md, 12px);
	    background: var(--st-link, var(--color-primary-fill));
	    color: var(--st-on-fill, var(--color-on-primary));
	    text-align: center;
	}
	html[data-home="showcase"] .home-cta-title { margin: 0 0 0.75rem; padding: 0; border: none; color: inherit; font-size: 1.75rem; font-weight: 700; letter-spacing: -0.02em; line-height: 1.25; }
	html[data-home="showcase"] .home-cta-text { max-width: 40rem; margin: 0 auto; color: inherit; font-size: 1.0625rem; line-height: 1.6; }
	html[data-home="showcase"] .home-cta :is(.home-cta-title, .home-cta-text) + .home-cta-button { margin-top: 1.75rem; }
	html[data-home="showcase"] .home-cta :is(.home-cta-button, .home-cta-button:hover) {
	    border: none;
	    background: var(--st-on-fill, var(--color-on-primary));
	    box-shadow: none;
	    color: var(--st-link, var(--color-primary-fill));
	    text-decoration: none;
	}
	html[data-home="showcase"] .home-cta .home-cta-button:focus-visible { outline: 2px solid var(--st-on-fill, var(--color-on-primary)); outline-offset: 3px; }
	html[data-style="brutal"][data-home="showcase"] .home-cta { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	@media (max-width: 900px) {
	    html[data-home="showcase"] .home-showcase { grid-template-columns: minmax(0, 1fr); gap: 2rem; padding: 3rem 1.5rem; }
	}
	@media (max-width: 600px) {
	    html[data-home="showcase"] .home-showcase { padding: 2.5rem 1rem; }
	    html[data-home="showcase"] .home-stats { padding: 2.5rem 1rem; }
	    html[data-home="showcase"] .home-cta { width: calc(100% - 2rem); margin-bottom: 3rem; padding: 2.25rem 1.25rem; }
	    html[data-home="showcase"] :is(.home-showcase-title, .home-cta-title) { font-size: 1.5rem; }
	}

	/* No backdrop, whatever heroBackground says. A banner hero keeps its band, which its text colors need. */
	html[data-home="minimal"]:not([data-hero="banner"]) .landing-hero { background: none; }
	html[data-home="minimal"] .landing-hero { padding-top: 5rem; padding-bottom: 3rem; }
	html[data-home="minimal"] .landing-hero::before,
	html[data-home="minimal"] .landing-hero::after,
	html[data-home="minimal"] .landing::before,
	html[data-home="minimal"][data-hero-background] .landing-hero::before,
	html[data-home="minimal"][data-hero-background] .landing-hero::after { display: none; }
	html[data-home="minimal"] .landing-hero-subtitle { color: var(--color-text-secondary); }
	@media (max-width: 600px) {
	    html[data-home="minimal"] .landing-hero { padding-top: 3rem; padding-bottom: 2rem; }
	}

	html[data-home="docs"] .home-docs-header { margin-bottom: 2rem; padding-bottom: 1.5rem; border-bottom: 1px solid var(--color-border); }
	html[data-home="docs"] .home-docs-header h1 { margin-bottom: 0.5rem !important; }
	html[data-home="docs"] .home-docs-lead { margin: 0; color: var(--color-text-secondary); font-size: 1.1875rem; line-height: 1.6; }
	html[data-home="docs"] .home-docs-features { margin-bottom: 2.5rem; }
	html[data-home="docs"] .home-docs-features-title { margin-top: 0; }
	html[data-home="docs"] .home-docs-features-subtitle { margin: -0.25rem 0 1rem; color: var(--color-text-secondary); }
	html[data-home="docs"] .home-docs-cards { margin: 0; }
	html[data-home="docs"] .home-docs-cards .component-link-card { justify-content: flex-start; align-items: flex-start; }
	html[data-home="docs"] .home-docs-cards .component-link-card-content { flex: 1; }
	html[data-home="docs"] .home-docs-cards .component-link-card-arrow { align-self: center; }
	html[data-home="docs"] .home-docs-card-icon {
	    display: inline-flex;
	    flex-shrink: 0;
	    align-items: center;
	    justify-content: center;
	    width: 2.25rem;
	    height: 2.25rem;
	    border-radius: var(--radius);
	    background: color-mix(in srgb, var(--color-primary) 8%, transparent);
	    color: var(--st-link, var(--color-primary-text));
	    font-family: var(--font-mono);
	    font-size: 0.875rem;
	    font-weight: 700;
	}
	html[data-home="docs"] .home-docs-card-icon svg { width: 18px; height: 18px; }
	html[data-home="docs"] div.component-link-card:hover { border-color: var(--color-border); box-shadow: none; }

	/* Feature card variants, scoped to the feature section: the portal's section cards share the card class. */
	html[data-features="list"] .landing-features-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 2.5rem; }
	html[data-features="list"] .landing-features .landing-feature-card {
	    display: grid;
	    grid-template-columns: auto minmax(0, 1fr);
	    align-content: start;
	    column-gap: 1rem;
	    padding: 1.25rem 0.75rem;
	    overflow: visible;
	    border: none;
	    border-top: 1px solid var(--color-border);
	    border-radius: 0;
	    background: none;
	    box-shadow: none;
	}
	html[data-features="list"] .landing-features .landing-feature-card::before { display: none; }
	html[data-features="list"] .landing-features .landing-feature-card:hover { transform: none !important; box-shadow: none !important; }
	html[data-features="list"] .landing-features a.landing-feature-card:hover { background: color-mix(in srgb, var(--color-text) 4%, transparent); }
	html[data-features="list"] .landing-features .landing-feature-card:not(:has(.landing-feature-icon)) { grid-template-columns: minmax(0, 1fr); }
	html[data-features="list"] .landing-features .landing-feature-icon { grid-row: 1 / span 2; margin-bottom: 0; }
	html[data-features="list"] .landing-features .landing-feature-name { margin-bottom: 0.25rem; }
	html[data-features="list"] .landing-features :is(.landing-feature-name, .landing-feature-desc) { grid-column: -2; }
	html[data-style="brutal"][data-features="list"] .landing-features .landing-feature-card { border-top: 3px solid var(--bt-ink); }
	@media (max-width: 700px) {
	    html[data-features="list"] .landing-features-grid { grid-template-columns: minmax(0, 1fr); }
	}

	html[data-features="icons"] .landing-features-grid { gap: 2.5rem 2rem; }
	html[data-features="icons"] .landing-features .landing-feature-card {
	    padding: 0.5rem 1rem;
	    overflow: visible;
	    border: none;
	    background: none;
	    box-shadow: none;
	    text-align: center;
	}
	html[data-features="icons"] .landing-features .landing-feature-card::before { display: none; }
	html[data-features="icons"] .landing-features .landing-feature-card:hover { transform: none !important; box-shadow: none !important; }
	html[data-features="icons"] .landing-features a.landing-feature-card:hover .landing-feature-name { text-decoration: underline; text-underline-offset: 0.2em; }
	html[data-features="icons"] .landing-features .landing-feature-icon {
	    width: 4.5rem;
	    height: 4.5rem;
	    margin: 0 auto 1.25rem;
	    border: none;
	    border-radius: 50%;
	    background: color-mix(in srgb, var(--color-primary) 12%, transparent);
	    font-size: 1.75rem;
	}
	html[data-features="icons"] .landing-features .landing-feature-icon svg { width: 34px; height: 34px; }
	html[data-features="icons"] .landing-features .landing-feature-name { font-size: 1.125rem; }
	html[data-style="brutal"][data-features="icons"] .landing-features .landing-feature-icon { border: 3px solid var(--bt-ink); }
	html[data-style="terminal"][data-features="icons"] .landing-features .landing-feature-icon { border: 1px solid var(--color-border); border-radius: 0; background: none; }

	html[data-features="compact"] .landing-features { max-width: 1200px; padding-top: 3rem; padding-bottom: 3rem; }
	html[data-features="compact"] .landing-features-grid { grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0.75rem; }
	html[data-features="compact"] .landing-features .landing-feature-card { padding: 1rem 1.125rem; }
	html[data-features="compact"] .landing-features .landing-feature-card:hover { transform: translateY(-2px) !important; }
	html[data-features="compact"] .landing-features .landing-feature-icon { width: 2rem; height: 2rem; margin-bottom: 0.625rem; border-radius: var(--radius-sm); font-size: 0.875rem; }
	html[data-features="compact"] .landing-features .landing-feature-icon svg { width: 16px; height: 16px; }
	/* The smaller text icon (C#) isn't large text any more, so it needs a lighter tint behind it for 4.5:1. */
	:where(html[data-features="compact"]) .landing-features .landing-feature-icon { background: color-mix(in srgb, var(--color-primary) 6%, transparent); }
	html[data-features="compact"] .landing-features .landing-feature-name { margin-bottom: 0.25rem; font-size: 0.9375rem; }
	html[data-features="compact"] .landing-features .landing-feature-desc { font-size: 0.8125rem; line-height: 1.5; }
	@media (max-width: 1000px) {
	    html[data-features="compact"] .landing-features-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
	}
	@media (max-width: 760px) {
	    html[data-features="compact"] .landing-features-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	}
	@media (max-width: 520px) {
	    html[data-features="compact"] .landing-features-grid { grid-template-columns: minmax(0, 1fr); }
	}

	/* Three columns: the first card takes two of them and two rows, the cards after it fill the rest, and the
	   last one stretches over the cells its row has left. */
	html[data-features="bento"] .landing-features-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
	html[data-features="bento"] .landing-features .landing-feature-card:first-child {
	    grid-column: span 2;
	    grid-row: span 2;
	    display: flex;
	    flex-direction: column;
	    justify-content: flex-end;
	    padding: 2.5rem 2.25rem;
	    background-image: linear-gradient(135deg, color-mix(in srgb, var(--color-primary) 10%, transparent), transparent 70%);
	}
	html[data-features="bento"] .landing-features .landing-feature-card:first-child .landing-feature-icon { width: 3.5rem; height: 3.5rem; margin-bottom: auto; font-size: 1.5rem; }
	html[data-features="bento"] .landing-features .landing-feature-card:first-child .landing-feature-icon svg { width: 28px; height: 28px; }
	html[data-features="bento"] .landing-features .landing-feature-card:first-child .landing-feature-name { margin-top: 2rem; font-size: 1.5rem; }
	html[data-features="bento"] .landing-features .landing-feature-card:first-child .landing-feature-desc { max-width: 32rem; color: color-mix(in srgb, var(--color-text-secondary) 70%, var(--color-text)); font-size: 1.0625rem; }
	html[data-features="bento"] .landing-features .landing-feature-card:only-child { grid-column: 1 / -1; grid-row: auto; }
	html[data-features="bento"] .landing-features .landing-feature-card:nth-child(2):last-child { grid-row: span 2; }
	html[data-features="bento"] .landing-features .landing-feature-card:nth-child(3n + 4):last-child { grid-column: span 3; }
	html[data-features="bento"] .landing-features .landing-feature-card:nth-child(3n + 5):last-child { grid-column: span 2; }
	@media (max-width: 900px) {
	    html[data-features="bento"] .landing-features-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	    html[data-features="bento"] .landing-features .landing-feature-card:first-child { grid-column: 1 / -1; grid-row: auto; }
	    html[data-features="bento"] .landing-features .landing-feature-card:nth-child(2):last-child { grid-row: auto; }
	    html[data-features="bento"] .landing-features .landing-feature-card:nth-child(3n + 4):last-child,
	    html[data-features="bento"] .landing-features .landing-feature-card:nth-child(3n + 5):last-child { grid-column: auto; }
	    html[data-features="bento"] .landing-features .landing-feature-card:nth-child(even):last-child { grid-column: 1 / -1; }
	}
	@media (max-width: 600px) {
	    html[data-features="bento"] .landing-features-grid { grid-template-columns: minmax(0, 1fr); }
	    html[data-features="bento"] .landing-features .landing-feature-card:first-child { padding: 2rem 1.5rem; }
	    html[data-features="bento"] .landing-features .landing-feature-card:first-child .landing-feature-name { margin-top: 1.25rem; font-size: 1.25rem; }
	}

	html[data-admonitions="outlined"] .page-content .admonition {
	    padding: 1rem 1.25rem;
	    border: 1.5px solid var(--ad-c, var(--color-border));
	    border-radius: var(--radius);
	    background: none;
	    box-shadow: none;
	}
	html[data-admonitions="outlined"] .page-content .admonition-title { margin: 0 0 0.5rem; padding: 0; border: none; background: none; }

	html[data-admonitions="callout"] .page-content .admonition {
	    padding: 1.25rem 1.5rem;
	    border: 1px solid color-mix(in srgb, var(--ad-c, var(--color-border)) 35%, var(--color-border));
	    border-radius: var(--radius-md, var(--radius));
	    background: color-mix(in srgb, var(--ad-c, var(--color-border)) 6%, var(--color-bg));
	    box-shadow: none;
	}
	html[data-admonitions="callout"] .page-content .admonition-title {
	    gap: 0.75rem;
	    margin: 0 0 0.625rem;
	    padding: 0;
	    border: none;
	    background: none;
	    font-size: 1.0625rem;
	    font-weight: 700;
	    font-variant-caps: normal;
	    letter-spacing: normal;
	    line-height: 1.4;
	    text-transform: none;
	}
	html[data-admonitions="callout"] .page-content .admonition-icon {
	    display: inline-flex;
	    flex-shrink: 0;
	    align-items: center;
	    justify-content: center;
	    width: 2.25rem;
	    height: 2.25rem;
	    border-radius: 50%;
	    background: color-mix(in srgb, var(--ad-c, var(--color-border)) 18%, var(--color-bg));
	    font-size: 1.125rem;
	}
	html[data-admonitions="callout"] .page-content .admonition-content { padding-left: 3rem; font-size: 1rem; }
	@media (max-width: 640px) {
	    html[data-admonitions="callout"] .page-content .admonition { padding: 1rem; }
	    html[data-admonitions="callout"] .page-content .admonition-content { padding-left: 0; }
	}

	html[data-tables="compact"] :is(.page-content, .landing-md-content) table { font-size: 0.8125rem; line-height: 1.45; }
	html[data-tables="compact"] :is(.page-content, .landing-md-content) :is(th, td) { padding-top: 0.3125rem; padding-bottom: 0.3125rem; }

	html[data-tables="rounded"] :is(.page-content, .landing-md-content) table {
	    width: max-content;
	    max-width: 100%;
	    border: 1px solid var(--color-border);
	    border-collapse: separate;
	    border-spacing: 0;
	    border-radius: var(--radius-md, var(--radius));
	}
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) :is(th, td) { padding-left: 0.875rem; padding-right: 0.875rem; border: none; border-bottom: 1px solid var(--color-border); }
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) thead th { border-top: none; border-bottom-color: var(--color-border); }
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) tr:last-child > td { border-bottom: none; }
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) th { background: color-mix(in srgb, var(--color-primary) 9%, var(--color-bg)); color: var(--color-text); }
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) th:first-child { border-top-left-radius: calc(var(--radius-md, var(--radius)) - 1px); }
	html[data-tables="rounded"] :is(.page-content, .landing-md-content) th:last-child { border-top-right-radius: calc(var(--radius-md, var(--radius)) - 1px); }
	html[data-style="brutal"][data-tables="rounded"] :is(.page-content, .landing-md-content) table { border: 3px solid var(--bt-ink); }
	html[data-style="brutal"][data-tables="rounded"] :is(.page-content, .landing-md-content) :is(th, td) { border-bottom: 2px solid var(--bt-ink); }

	html[data-font="rounded"] { --font-body: ui-rounded, 'SF Pro Rounded', 'Hiragino Maru Gothic ProN', Quicksand, Comfortaa, Manjari, 'Arial Rounded MT', 'Arial Rounded MT Bold', Calibri, source-sans-pro, sans-serif; }
	html[data-font="humanist"] { --font-body: Seravek, 'Gill Sans Nova', Ubuntu, Calibri, 'DejaVu Sans', source-sans-pro, sans-serif; }

	html[data-headings="underlined"] .page-content > h2 {
	    padding-top: 0;
	    padding-bottom: 0.5rem;
	    border-top: none;
	    border-bottom: 2px solid color-mix(in srgb, var(--color-text) 22%, transparent);
	}

	/* A background drawn from the content box, not a border or a pseudo-element: the bar stays one line
	   tall at the first line of text whatever padding a style gives the heading, and leaves the h2's rule
	   and the terminal style's prefixes alone. */
	html[data-headings="accent"] .page-content > :is(h2, h3) {
	    padding-left: 1rem;
	    background: linear-gradient(var(--st-link, var(--color-primary-text)), var(--st-link, var(--color-primary-text))) -1rem 0.2em / 0.25rem 1.05em no-repeat;
	    background-origin: content-box;
	}

	/* Direct children of the article only, so the page title (h1) and headings inside components keep
	   their own look. */
	html[data-headings="numbered"] .page-content { counter-reset: heading-2; }
	html[data-headings="numbered"] .page-content > h2 { counter-increment: heading-2; counter-reset: heading-3; }
	html[data-headings="numbered"] .page-content > h3 { counter-increment: heading-3; }
	html[data-headings="numbered"] .page-content > h2::before { content: counter(heading-2) '.'; content: counter(heading-2) '.' / ''; margin-right: 0.5em; }
	html[data-headings="numbered"] .page-content > h3::before { content: counter(heading-2) '.' counter(heading-3); content: counter(heading-2) '.' counter(heading-3) / ''; margin-right: 0.5em; }
	html[data-headings="numbered"] .page-content > :is(h2, h3)::before { font-variant-numeric: tabular-nums; }
	html[data-style="terminal"][data-headings="numbered"] .page-content > h2::before { content: '# ' counter(heading-2) '.'; content: '# ' counter(heading-2) '.' / ''; }
	html[data-style="terminal"][data-headings="numbered"] .page-content > h3::before { content: '## ' counter(heading-2) '.' counter(heading-3); content: '## ' counter(heading-2) '.' counter(heading-3) / ''; }

	html[data-links="underlined"] :is(.page-content, .landing-md-content) a:not([class], :is(.breadcrumbs, .toc-inline) *) {
	    text-decoration: underline;
	    text-decoration-thickness: 1px;
	    text-underline-offset: 0.2em;
	}
	html[data-links="underlined"] :is(.page-content, .landing-md-content) a:not([class], :is(.breadcrumbs, .toc-inline) *):hover { text-decoration-thickness: 2px; }

	/* Body text color, so a link needs another cue than color (WCAG 1.4.1): a dotted underline at rest,
	   a solid one in the accent color on hover and focus. */
	html[data-links="subtle"] :is(.page-content, .landing-md-content) a:not([class], :is(.breadcrumbs, .toc-inline) *) {
	    color: inherit;
	    text-decoration: underline dotted;
	    text-decoration-color: var(--color-text-muted);
	    text-decoration-thickness: 1px;
	    text-underline-offset: 0.22em;
	}
	html[data-links="subtle"] :is(.page-content, .landing-md-content) a:not([class], :is(.breadcrumbs, .toc-inline) *):is(:hover, :focus-visible) {
	    color: inherit;
	    text-decoration: underline solid;
	    text-decoration-color: var(--st-link, var(--color-primary-text));
	    text-decoration-thickness: 2px;
	}

	/* Forced colors drop box shadows and gradients, which draw the accent bars and the current dot above. */
	@media (forced-colors: active) {
	    html[data-sidebar="accent"] .sidebar .nav-link.current { border-left: 3px solid Highlight !important; }
	    html[data-toc="timeline"] .toc-item a.active::after { forced-color-adjust: none; border-color: Highlight; background: Highlight; }
	    html[data-headings="accent"] .page-content > :is(h2, h3) { padding-left: 0.75rem; border-left: 0.25rem solid CanvasText; }
	}

	/* Readers who can switch layouts (theme.options.layoutSelector) get pages with the markup of every variant:
	   main.css hides what only some variants show, these rules show it while its variant is on and hide what the
	   variant replaces. */
	html[data-navbar="tabs"] .header-tabs { display: block; }
	html[data-footer="columns"] .site-footer-columns { display: block; }
	html[data-footer="columns"] :is(.site-footer-row, .landing-footer) { display: none; }
	html[data-footer="none"] :is(.site-footer, .landing-footer) { display: none; }
	html[data-toc="inline"] .toc-inline { display: block; }
	html[data-toc="inline"] :is(.toc-sidebar, .toc-toggle-btn) { display: none; }
	html[data-toc="minimal"] :is(.toc-progress, .toc-back-to-top, .toc-counter) { display: none; }
	@media (max-width: 1200px) {
	    html[data-toc="inline"]:not(.no-js):has(.toc-toggle-btn) { --fab-stack: 0px; }
	    html[data-toc="inline"]:not(.no-js):has(.toc-toggle-btn):has(.back-to-top) { --fab-stack: var(--fab-size); }
	}

	/* A reader's Wider or Full width outgrows the box, so the frame goes. */
	html[data-width="boxed"]:is([data-reader-width="wider"], [data-reader-width="full"]) body { background: var(--color-bg); }
	html[data-width="boxed"]:is([data-reader-width="wider"], [data-reader-width="full"]):not([data-navbar="floating"]) .site-header,
	html[data-width="boxed"]:is([data-reader-width="wider"], [data-reader-width="full"]) :is(.site-footer, .landing-footer) { max-width: none; border-left: none; border-right: none; }

	""";
}
