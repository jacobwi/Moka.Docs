namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _headerTabs = """

	                                          {{ if theme.layout.navbar == "tabs" && layout_tabs.size > 0 }}
	                                          <nav class="header-tabs" aria-label="Sections">
	                                              <div class="header-tabs-inner">
	                                                  {{ for tab in layout_tabs }}<a class="header-tab{{ if tab.is_current }} current{{ end }}" href="{{ tab.url | html.escape }}"{{ if tab.is_current }} aria-current="true"{{ end }}>{{ tab.label | html.escape }}</a>{{ end }}
	                                              </div>
	                                          </nav>
	                                          {{ end }}

	                                      """;

	private const string _tocList = """

	                                                      <ul class="toc-list">
	                                                          {{ for entry in page.toc }}
	                                                          {{ if entry.level <= theme.toc_depth }}
	                                                          <li class="toc-item toc-level-{{ entry.level }}">
	                                                              <a href="#{{ entry.id | html.escape }}">{{ entry.text | html.escape }}</a>
	                                                              {{ if entry.children.size > 0 }}
	                                                              <ul>
	                                                                  {{ for child in entry.children }}
	                                                                  {{ if child.level <= theme.toc_depth }}
	                                                                  <li class="toc-item toc-level-{{ child.level }}">
	                                                                      <a href="#{{ child.id | html.escape }}">{{ child.text | html.escape }}</a>
	                                                                      {{ if child.children.size > 0 }}
	                                                                      <ul>
	                                                                          {{ for grandchild in child.children }}
	                                                                          {{ if grandchild.level <= theme.toc_depth }}
	                                                                          <li class="toc-item toc-level-{{ grandchild.level }}">
	                                                                              <a href="#{{ grandchild.id | html.escape }}">{{ grandchild.text | html.escape }}</a>
	                                                                          </li>
	                                                                          {{ end }}
	                                                                          {{ end }}
	                                                                      </ul>
	                                                                      {{ end }}
	                                                                  </li>
	                                                                  {{ end }}
	                                                                  {{ end }}
	                                                              </ul>
	                                                              {{ end }}
	                                                          </li>
	                                                          {{ end }}
	                                                          {{ end }}
	                                                      </ul>

	                                      """;

	private const string _columnsFooter = """

	                                          <footer class="site-footer">
	                                              <div class="footer-columns">
	                                                  <div class="footer-brand">
	                                                      <a class="footer-brand-name" href="{{ base_path | html.escape }}/">{{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="" />{{ end }}<span>{{ site.title | html.escape }}</span></a>
	                                                      {{ if site.description != "" }}<p class="footer-brand-desc">{{ site.description | html.escape }}</p>{{ end }}
	                                                      {{ if theme.social_links.size > 0 }}<div class="footer-social">{{ for link in theme.social_links }}<a href="{{ link.url | html.escape }}" target="_blank" rel="noopener noreferrer" aria-label="{{ link.icon | html.escape }}">{{ link.icon_svg }}</a>{{ end }}</div>{{ end }}
	                                                  </div>
	                                                  {{ if footer_columns.size > 0 }}
	                                                  <nav class="footer-links" aria-label="Footer">
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
	                                                  {{ if theme.show_built_with != false }}<span class="built-with">Built with <a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v{{ mokadocs_version }}</span>{{ end }}
	                                              </div>
	                                              {{ end }}
	                                          </footer>

	                                      """;

	private const string _layoutsCss = """

	/* Layouts: theme.options.layout. Every rule is scoped to its variant's attribute on <html>, which the
	   layouts only write for a variant other than default. Colors sit in :where() so a style's own
	   colors still win; the rest comes after the styles on purpose, so a variant's shape wins. */
	html[data-width="wide"] { --layout-max-width: 1680px; --content-max-width: 1040px; }
	html[data-width="full"] { --layout-max-width: none; }
	html[data-width="full"] .content { max-width: none; }
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
	@media (max-width: 1296px) {
	    html[data-width="boxed"] body { background: var(--color-bg); }
	}

	html[data-navbar="compact"] { --header-height: 48px; }
	html[data-navbar="compact"] .site-logo { font-size: 1rem; gap: 0.5rem; }
	html[data-navbar="compact"] .site-logo-icon { width: 22px; height: 22px; }
	html[data-navbar="compact"] .site-logo-img { height: 22px; }
	html[data-navbar="compact"] .header-actions { gap: 0.375rem; }
	html[data-navbar="compact"] .site-header .search-trigger { padding-top: 0.25rem; padding-bottom: 0.25rem; font-size: 0.8125rem; }
	html[data-navbar="compact"] .site-header :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger) { width: 32px; height: 32px; }
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

	html[data-toc="floating"] .toc-sidebar {
	    display: none;
	    position: fixed;
	    top: auto;
	    right: 2rem;
	    bottom: 8rem;
	    left: auto;
	    width: 300px;
	    max-width: calc(100vw - 2rem);
	    height: auto;
	    max-height: min(70vh, 34rem);
	    padding: 1rem 0.5rem;
	    overflow-y: auto;
	    border: 1px solid var(--color-border);
	    border-radius: 12px;
	    z-index: 100;
	}
	:where(html[data-toc="floating"]) .toc-sidebar { background: var(--color-bg); box-shadow: var(--shadow-lg); }
	html[data-toc="floating"] .toc-sidebar.open { display: block; }
	html[data-toc="floating"] .toc-toggle-btn {
	    display: flex;
	    position: fixed;
	    right: 2rem;
	    bottom: 5rem;
	    align-items: center;
	    justify-content: center;
	    width: 40px;
	    height: 40px;
	    border: none;
	    cursor: pointer;
	    z-index: 99;
	}
	:where(html[data-toc="floating"]) .toc-toggle-btn { border-radius: 50%; background: var(--color-primary); color: #ffffff; box-shadow: var(--shadow-md); }
	html[data-toc="floating"] .toc-toggle-btn svg { width: 20px; height: 20px; }
	@media (max-width: 768px) {
	    html[data-toc="floating"] .toc-sidebar { right: 1rem; }
	    html[data-toc="floating"] .toc-toggle-btn { right: 1rem; }
	}

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

	""";
}
