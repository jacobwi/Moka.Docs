namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _defaultLayout = """
	                                      <!DOCTYPE html>
	                                      <html class="no-js" lang="{{ site.language | html.escape }}" dir="{{ site.direction | html.escape }}" data-theme="light" data-code-theme="{{ theme.code_theme | html.escape }}" data-code-style="{{ theme.code_style | html.escape }}" data-style="{{ theme.style | html.escape }}"
	                                      """ + _layoutAttributes + """
	                                      {{ if base_path != "" }} data-base-path="{{ base_path | html.escape }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}{{ if theme.show_copy_button == false }} data-no-copy-button{{ end }}{{ if theme.show_line_numbers == false }} data-no-line-numbers{{ end }}{{ if theme.image_zoom == false }} data-no-image-zoom{{ end }}>
	                                      <head>
	                                      """ + _headBootstrap + """
	                                          <meta charset="utf-8" />
	                                          <meta name="viewport" content="width=device-width, initial-scale=1" />
	                                          <title>{{ page.title | html.escape }} - {{ site.title | html.escape }}</title>
	                                          <meta name="description" content="{{ page.meta_description | html.escape }}" />
	                                          {{ if page.noindex }}<meta name="robots" content="noindex" />{{ end }}
	                                          {{ if page.canonical_url != "" }}<link rel="canonical" href="{{ page.canonical_url | html.escape }}" />{{ end }}
	                                          {{ if page.canonical_url != "" }}<meta property="og:title" content="{{ page.title | html.escape }}" />
	                                          <meta property="og:description" content="{{ page.meta_description | html.escape }}" />
	                                          <meta property="og:url" content="{{ page.canonical_url | html.escape }}" />
	                                          <meta property="og:site_name" content="{{ site.title | html.escape }}" />
	                                          <meta property="og:type" content="article" />{{ if site.og_locale != "" }}<meta property="og:locale" content="{{ site.og_locale | html.escape }}" />{{ end }}
	                                          {{ if page.image_url != "" }}<meta property="og:image" content="{{ page.image_url | html.escape }}" />
	                                          {{ end }}<meta name="twitter:card" content="{{ if page.image_url != "" }}summary_large_image{{ else }}summary{{ end }}" />
	                                          <meta name="twitter:title" content="{{ page.title | html.escape }}" />
	                                          <meta name="twitter:description" content="{{ page.meta_description | html.escape }}" />{{ if page.image_url != "" }}
	                                          <meta name="twitter:image" content="{{ page.image_url | html.escape }}" />{{ end }}{{ end }}
	                                          {{ if page.markdown_url != "" }}<link rel="alternate" type="text/markdown" title="Markdown" href="{{ page.markdown_url | html.escape }}" />{{ end }}
	                                          {{ for css in css_files }}<link rel="stylesheet" href="{{ css | html.escape }}" />
	                                          {{ if for.first }}
	                                      """ + _styleLoader + """
	                                      {{ end }}{{ end }}
	                                          {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color | html.escape }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color | html.escape }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color | html.escape }} 80%,#000)}{{ if theme.primary_text }}html:not([data-color-theme]){--color-primary-text:{{ theme.primary_text.light | html.escape }};--color-primary-text-hover:{{ theme.primary_text.light_hover | html.escape }};}html:not([data-color-theme])[data-theme="dark"]{--color-primary-text:{{ theme.primary_text.dark | html.escape }};--color-primary-text-hover:{{ theme.primary_text.dark_hover | html.escape }};}{{ end }}</style>{{ end }}
	                                          {{ if theme.accent_color != "" }}<style>:root{--color-accent:{{ theme.accent_color | html.escape }}}</style>{{ end }}
	                                          {{ if site.favicon_url != "" }}<link rel="icon" href="{{ site.favicon_url | html.escape }}" />{{ end }}
	                                          {{ for feed in blog.feeds }}<link rel="alternate" type="{{ feed.type | html.escape }}" title="{{ feed.title | html.escape }}" href="{{ feed.url | html.escape }}" />
	                                          {{ end }}
	                                      </head>
	                                      <body>
	                                          <a class="skip-link" href="#mokadocs-main">{{ t.a11y_skip_to_content | html.escape }}</a>
	                                          {{ if theme.banner }}
	                                          <div class="site-banner" id="mokadocs-banner" role="region" aria-label="{{ t.banner_label | html.escape }}" data-variant="{{ theme.banner.variant | html.escape }}" data-banner-id="{{ theme.banner.id | html.escape }}">
	                                              <div class="site-banner-inner">
	                                                  {{ if theme.banner.variant == "warning" }}<svg class="site-banner-icon" aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#warning"/></svg>{{ end }}
	                                                  <p class="site-banner-text">{{ theme.banner.text | html.escape }}{{ if theme.banner.link != "" }} <a class="site-banner-link" href="{{ theme.banner.link | html.escape }}">{{ theme.banner.link_text | html.escape }}<svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#arrow-right"/></svg></a>{{ end }}</p>
	                                                  {{ if theme.banner.dismissible }}<button class="site-banner-close js-only" type="button" aria-label="{{ t.banner_dismiss | html.escape }}"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#x"/></svg></button>{{ end }}
	                                              </div>
	                                          </div>
	                                          {{ if theme.banner.dismissible }}<script>try{const b=document.getElementById('mokadocs-banner');if(localStorage.getItem('mokadocs-banner-dismissed')===b.getAttribute('data-banner-id'))b.hidden=true}catch(e){}</script>{{ end }}
	                                          {{ end }}
	                                          <header class="site-header">
	                                              <div class="header-inner">
	                                                  <a class="site-logo" href="{{ base_path | html.escape }}/">
	                                                      {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="{{ site.title | html.escape }}" class="site-logo-img" />{{ else }}<svg class="site-logo-icon" width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#logo"/></svg>{{ end }}
	                                                      <span class="site-name">{{ site.title | html.escape }}</span>
	                                                  </a>
	                                                  <div class="header-actions">
	                                                      {{ if versions.size > 0 && theme.show_version_selector }}
	                                                      <div class="version-selector">
	                                                          <button class="version-trigger" type="button" aria-expanded="false" aria-controls="versionMenu">
	                                                              <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#pen"/></svg>
	                                                              <span class="visually-hidden">{{ t.version_label | html.escape }} </span><span class="version-label">{{ current_version | html.escape }}</span>
	                                                              <svg class="version-chevron" aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#chevron"/></svg>
	                                                          </button>
	                                                          <ul class="version-dropdown" id="versionMenu" hidden>
	                                                              {{ for v in versions }}
	                                                              <li>
	                                                                  <a class="version-option{{ if v.is_current }} active{{ end }}" href="{{ v.url | html.escape }}" data-version-slug="{{ v.slug | html.escape }}"{{ if v.is_current }} aria-current="page"{{ end }}>
	                                                                      <span class="version-option-label">{{ v.label | html.escape }}</span>
	                                                                      {{ if v.is_default }}<span class="version-badge version-badge-default">{{ t.version_latest | html.escape }}</span>{{ end }}
	                                                                      {{ if v.is_prerelease }}<span class="version-badge version-badge-pre">{{ t.version_prerelease | html.escape }}</span>{{ end }}
	                                                                  </a>
	                                                              </li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if search_enabled }}
	                                                      <button class="search-trigger" type="button" aria-label="{{ t.search_button | html.escape }}" aria-keyshortcuts="Control+K">
	                                                          <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#search"/></svg>
	                                                          <span class="search-label">{{ t.search_button | html.escape }}</span>
	                                                          <kbd aria-hidden="true">{{ t.search_shortcut | html.escape }}</kbd>
	                                                      </button>
	                                                      {{ end }}
	                                      """ + _appearanceMenu + """
	                                                      {{ if theme.show_dark_mode_toggle }}
	                                                      <button class="theme-toggle" type="button" aria-label="{{ t.appearance_dark_mode | html.escape }}" aria-pressed="false">
	                                                          <svg class="icon-sun" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#sun"/></svg>
	                                                          <svg class="icon-moon" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#moon"/></svg>
	                                                      </button>
	                                                      {{ end }}
	                                      """ + _mobileNavToggle + """
	                                                  </div>
	                                              </div>
	                                      """ + _headerTabs + """
	                                          </header>

	                                          <div class="site-container">
	                                      """ + _sidebarAside + """
	                                              <main class="content" id="mokadocs-main" tabindex="-1">
	                                                  <article class="page-content">
	                                                      {{ if theme.show_breadcrumbs && breadcrumbs.size > 1 }}
	                                                      <nav class="breadcrumbs" aria-label="{{ t.breadcrumbs_label | html.escape }}">
	                                                          <ol>
	                                                          {{ for crumb in breadcrumbs }}
	                                                              <li>
	                                                              {{ if crumb.is_current }}
	                                                                  <span aria-current="page">{{ crumb.label | html.escape }}</span>
	                                                              {{ else if crumb.url != "" }}
	                                                                  <a href="{{ crumb.url | html.escape }}">{{ crumb.label | html.escape }}</a>
	                                                              {{ else }}
	                                                                  <span>{{ crumb.label | html.escape }}</span>
	                                                              {{ end }}
	                                                              </li>
	                                                          {{ end }}
	                                                          </ol>
	                                                      </nav>
	                                                      {{ end }}
	                                                      {{ if page.markdown_url != "" }}
	                                                      <div class="page-actions">
	                                                          <button type="button" class="page-action page-copy-markdown js-only" data-markdown-url="{{ page.markdown_url | html.escape }}" data-copied="{{ t.copy_copied | html.escape }}" data-failed="{{ t.copy_failed | html.escape }}" hidden>{{ t.page_copy | html.escape }}</button>
	                                                          <a class="page-action page-view-markdown" href="{{ page.markdown_url | html.escape }}" type="text/markdown">{{ t.page_view_markdown | html.escape }}</a>
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if package && page.is_api_index }}
	                                                      <div class="package-install-widget">
	                                                          <div class="install-header"><span class="install-package-name">{{ package.name | html.escape }}</span><a href="https://www.nuget.org/packages/{{ package.name | html.escape }}" target="_blank" rel="noopener noreferrer" class="install-nuget-link" aria-label="{{ t.install_nuget_link | string.replace "{package}" package.name | html.escape }}"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#nuget-mark"/></svg><span>NuGet</span><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#external"/></svg></a></div>
	                                                          <div class="install-tabs" role="tablist" aria-label="{{ t.install_with | string.replace "{package}" package.name | html.escape }}">
	                                                              <button class="install-tab active" type="button" role="tab" aria-selected="true" data-tab="pm">{{ t.install_package_manager | html.escape }}</button>
	                                                              <button class="install-tab" type="button" role="tab" aria-selected="false" data-tab="cli">.NET CLI</button>
	                                                              <button class="install-tab" type="button" role="tab" aria-selected="false" data-tab="ref">PackageReference</button>
	                                                          </div>
	                                                          <div class="install-panel active" data-panel="pm">
	                                                              <pre><code>Install-Package {{ package.name | html.escape }} -Version {{ package.version | html.escape }}</code></pre>
	                                                              <button class="install-copy-btn" aria-label="{{ t.install_copy_label | html.escape }}">{{ t.code_copy | html.escape }}</button>
	                                                          </div>
	                                                          <div class="install-panel" data-panel="cli">
	                                                              <pre><code>dotnet add package {{ package.name | html.escape }} --version {{ package.version | html.escape }}</code></pre>
	                                                              <button class="install-copy-btn" aria-label="{{ t.install_copy_label | html.escape }}">{{ t.code_copy | html.escape }}</button>
	                                                          </div>
	                                                          <div class="install-panel" data-panel="ref">
	                                                              <pre><code>&lt;PackageReference Include="{{ package.name | html.escape }}" Version="{{ package.version | html.escape }}" /&gt;</code></pre>
	                                                              <button class="install-copy-btn" aria-label="{{ t.install_copy_label | html.escape }}">{{ t.code_copy | html.escape }}</button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if page.show_toc && (theme.layout.toc == "inline" || theme.layout_selector) }}
	                                                      <details class="toc-inline" id="tocInline" open>
	                                                          <summary>{{ t.toc_title | html.escape }}</summary>
	                                                          <nav class="toc" aria-label="{{ t.toc_title | html.escape }}">
	                                      """ + _tocList + """
	                                                          </nav>
	                                                      </details>
	                                                      {{ end }}
	                                                      {{ page.content }}
	                                                      {{ if page.show_toc && (theme.layout.toc == "inline" || theme.layout_selector) }}<script>(function(){var t=document.getElementById('tocInline'),h=t&&t.parentElement.querySelector(':scope > h1');if(h)h.after(t)})()</script>{{ end }}

	                                                      {{ if edit_url }}
	                                                      <div class="edit-link">
	                                                          <a href="{{ edit_url | html.escape }}" target="_blank" rel="noopener">
	                                                              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#edit"/></svg>
	                                                              {{ t.page_edit | html.escape }}
	                                                          </a>
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if page.last_modified != "" && theme.show_last_updated }}
	                                                      <div class="last-updated">
	                                                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#clock"/></svg>
	                                                          {{ t.page_last_updated | html.escape | string.replace "{date}" ('<time datetime="' + (page.last_modified | html.escape) + '">' + (page.last_modified_display | html.escape) + '</time>') }}
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if theme.show_contributors && page.contributors.size > 0 }}
	                                                      <div class="page-contributors">
	                                                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><use href="{{ icon_sprite | html.escape }}#users"/></svg>
	                                                          <span>{{ t.page_contributors | html.escape }}</span>
	                                                          <ul class="page-contributors-list">
	                                                              {{ for contributor in page.contributors }}
	                                                              <li><span class="page-contributor-avatar" aria-hidden="true">{{ contributor.initials | html.escape }}</span>{{ contributor.name | html.escape }}</li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                  </article>

	                                                  {{ if theme.show_feedback != false }}
	                                                  <div class="feedback-widget" id="feedbackWidget">
	                                                      <span class="feedback-prompt">{{ t.feedback_prompt | html.escape }}</span>
	                                                      <div class="feedback-buttons">
	                                                          <button class="feedback-btn feedback-btn-yes" data-helpful="true" aria-label="{{ t.feedback_yes_label | html.escape }}">
	                                                              <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#thumbs-up"/></svg>
	                                                              <span>{{ t.feedback_yes | html.escape }}</span>
	                                                          </button>
	                                                          <button class="feedback-btn feedback-btn-no" data-helpful="false" aria-label="{{ t.feedback_no_label | html.escape }}">
	                                                              <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#thumbs-down"/></svg>
	                                                              <span>{{ t.feedback_no | html.escape }}</span>
	                                                          </button>
	                                                      </div>
	                                                      <span class="feedback-thanks" hidden>{{ t.feedback_thanks | html.escape }}</span>
	                                                  </div>
	                                                  {{ end }}

	                                                  {{ if theme.show_prev_next && (prev_page || next_page) }}
	                                                  <nav class="page-nav" aria-label="{{ t.page_nav_label | html.escape }}">
	                                                    {{ if prev_page }}
	                                                    <a class="page-nav-link page-nav-prev" href="{{ prev_page.route | html.escape }}">
	                                                      <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#prev"/></svg>
	                                                      <div class="page-nav-text">
	                                                        <span class="page-nav-label">{{ if page.is_blog_post }}{{ t.page_nav_newer_post | html.escape }}{{ else }}{{ t.page_nav_previous | html.escape }}{{ end }}</span>
	                                                        <span class="page-nav-title">{{ prev_page.title | html.escape }}</span>
	                                                      </div>
	                                                    </a>
	                                                    {{ else }}
	                                                    <span class="page-nav-spacer"></span>
	                                                    {{ end }}
	                                                    {{ if next_page }}
	                                                    <a class="page-nav-link page-nav-next" href="{{ next_page.route | html.escape }}">
	                                                      <div class="page-nav-text">
	                                                        <span class="page-nav-label">{{ if page.is_blog_post }}{{ t.page_nav_older_post | html.escape }}{{ else }}{{ t.page_nav_next | html.escape }}{{ end }}</span>
	                                                        <span class="page-nav-title">{{ next_page.title | html.escape }}</span>
	                                                      </div>
	                                                      <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#next"/></svg>
	                                                    </a>
	                                                    {{ else }}
	                                                    <span class="page-nav-spacer"></span>
	                                                    {{ end }}
	                                                  </nav>
	                                                  {{ end }}
	                                              </main>

	                                              {{ if page.show_toc && (theme.layout.toc != "inline" || theme.layout_selector) }}
	                                              <aside class="toc-sidebar" id="tocSidebar">
	                                                  <nav class="toc" aria-labelledby="tocTitle">
	                                                      <p class="toc-title" id="tocTitle">{{ t.toc_title | html.escape }}</p>
	                                                      {{ if theme.layout.toc != "minimal" || theme.layout_selector }}
	                                                      <div class="toc-progress"><div class="toc-progress-bar" id="tocProgress"></div></div>
	                                                      <a href="#" class="toc-back-to-top" id="tocBackToTop">&uarr; {{ t.toc_top | html.escape }}</a>
	                                                      {{ end }}
	                                      """ + _tocList + """
	                                                      {{ if theme.layout.toc != "minimal" || theme.layout_selector }}<div class="toc-counter" id="tocCounter"></div>{{ end }}
	                                                  </nav>
	                                              </aside>
	                                              {{ end }}
	                                          </div>

	                                          {{ if theme.layout.footer == "columns" || theme.layout_selector }}
	                                      """ + _columnsFooter + """
	                                          {{ end }}
	                                          {{ if (theme.layout.footer != "columns" && theme.layout.footer != "none") || theme.layout_selector }}
	                                          <footer class="site-footer{{ if theme.layout_selector }} site-footer-row{{ end }}">
	                                              <div class="footer-inner">
	                                                  {{ if site.copyright }}<span>{{ site.copyright }}</span>{{ end }}
	                                                  {{ if theme.social_links.size > 0 }}<div class="footer-social">{{ for link in theme.social_links }}<a href="{{ link.url | html.escape }}" target="_blank" rel="noopener noreferrer" aria-label="{{ link.icon | html.escape }}">{{ link.icon_svg }}</a>{{ end }}</div>{{ end }}
	                                                  {{ if theme.show_built_with != false }}<span class="built-with">{{ t.footer_built_with | html.escape | string.replace "{mokadocs}" ('<a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v' + mokadocs_version) }}</span>{{ end }}
	                                              </div>
	                                          </footer>
	                                          {{ end }}

	                                          {{ if search_enabled }}
	                                          <div class="search-modal" id="searchModal"{{ if search_index_url != "" }} data-index="{{ search_index_url | html.escape }}"{{ end }} hidden>
	                                              <div class="search-backdrop"></div>
	                                              <div class="search-dialog" role="dialog" aria-modal="true" aria-label="{{ t.search_label | html.escape }}">
	                                                  <div class="search-input-wrap">
	                                                      <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#search"/></svg>
	                                                      <label class="visually-hidden" for="searchInput">{{ t.search_label | html.escape }}</label>
	                                                      <input type="text" class="search-input" id="searchInput" placeholder="{{ t.search_placeholder | html.escape }}" autocomplete="off" spellcheck="false" enterkeyhint="search" role="combobox" aria-expanded="false" aria-controls="searchResults" aria-autocomplete="list" />
	                                                      <button type="button" class="search-close" aria-label="{{ t.search_close | html.escape }}"><kbd class="search-esc" aria-hidden="true">Esc</kbd><svg class="search-close-icon" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><use href="{{ icon_sprite | html.escape }}#close"/></svg></button>
	                                                  </div>
	                                                  <p class="search-message" id="searchMessage">{{ t.search_prompt | html.escape }}</p>
	                                                  <div class="search-results" id="searchResults" role="listbox" aria-label="{{ t.search_results | html.escape }}"></div>
	                                                  <div class="visually-hidden" id="searchStatus" role="status" aria-live="polite"></div>
	                                              </div>
	                                          </div>
	                                          {{ end }}

	                                          {{ if page.show_toc && (theme.layout.toc != "inline" || theme.layout_selector) }}
	                                         <button class="toc-toggle-btn" id="tocToggle" type="button" aria-label="{{ t.toc_toggle | html.escape }}" aria-expanded="false" aria-controls="tocSidebar">
	                                             <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#toc"/></svg>
	                                         </button>
	                                         {{ end }}
	                                         {{ if theme.show_back_to_top }}
	                                         <button class="back-to-top" id="backToTop" type="button" aria-label="{{ t.page_back_to_top | html.escape }}">&#8593;</button>
	                                         {{ end }}

	                                          {{ for js in js_files }}<script src="{{ js | html.escape }}"></script>
	                                          {{ end }}
	                                      </body>
	                                      </html>
	                                      """;

	private const string _landingLayout = """
	                                      <!DOCTYPE html>
	                                      <html class="no-js" lang="{{ site.language | html.escape }}" dir="{{ site.direction | html.escape }}" data-theme="light" data-code-theme="{{ theme.code_theme | html.escape }}" data-code-style="{{ theme.code_style | html.escape }}" data-style="{{ theme.style | html.escape }}"
	                                      """ + _layoutAttributes + """
	                                      {{ if base_path != "" }} data-base-path="{{ base_path | html.escape }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}{{ if theme.show_copy_button == false }} data-no-copy-button{{ end }}{{ if theme.show_line_numbers == false }} data-no-line-numbers{{ end }}{{ if theme.image_zoom == false }} data-no-image-zoom{{ end }}>
	                                      <head>
	                                      """ + _headBootstrap + """
	                                          <meta charset="utf-8" />
	                                          <meta name="viewport" content="width=device-width, initial-scale=1" />
	                                          <title>{{ site.title | html.escape }}{{ if page.description != "" }} - {{ page.description | html.escape }}{{ end }}</title>
	                                          <meta name="description" content="{{ page.meta_description | html.escape }}" />
	                                          {{ if page.canonical_url != "" }}<link rel="canonical" href="{{ page.canonical_url | html.escape }}" />{{ end }}
	                                          {{ if page.canonical_url != "" }}<meta property="og:title" content="{{ site.title | html.escape }}" />
	                                          <meta property="og:description" content="{{ page.meta_description | html.escape }}" />
	                                          <meta property="og:url" content="{{ page.canonical_url | html.escape }}" />
	                                          <meta property="og:site_name" content="{{ site.title | html.escape }}" />
	                                          <meta property="og:type" content="website" />{{ if site.og_locale != "" }}<meta property="og:locale" content="{{ site.og_locale | html.escape }}" />{{ end }}
	                                          {{ if page.image_url != "" }}<meta property="og:image" content="{{ page.image_url | html.escape }}" />
	                                          {{ end }}<meta name="twitter:card" content="{{ if page.image_url != "" }}summary_large_image{{ else }}summary{{ end }}" />
	                                          <meta name="twitter:title" content="{{ site.title | html.escape }}" />
	                                          <meta name="twitter:description" content="{{ page.meta_description | html.escape }}" />{{ if page.image_url != "" }}
	                                          <meta name="twitter:image" content="{{ page.image_url | html.escape }}" />{{ end }}{{ end }}
	                                          {{ if page.markdown_url != "" }}<link rel="alternate" type="text/markdown" title="Markdown" href="{{ page.markdown_url | html.escape }}" />{{ end }}
	                                          {{ for css in css_files }}<link rel="stylesheet" href="{{ css | html.escape }}" />
	                                          {{ if for.first }}
	                                      """ + _styleLoader + """
	                                      {{ end }}{{ end }}
	                                          {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color | html.escape }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color | html.escape }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color | html.escape }} 80%,#000)}{{ if theme.primary_text }}html:not([data-color-theme]){--color-primary-text:{{ theme.primary_text.light | html.escape }};--color-primary-text-hover:{{ theme.primary_text.light_hover | html.escape }};}html:not([data-color-theme])[data-theme="dark"]{--color-primary-text:{{ theme.primary_text.dark | html.escape }};--color-primary-text-hover:{{ theme.primary_text.dark_hover | html.escape }};}{{ end }}</style>{{ end }}
	                                          {{ if theme.accent_color != "" }}<style>:root{--color-accent:{{ theme.accent_color | html.escape }}}</style>{{ end }}
	                                          {{ if site.favicon_url != "" }}<link rel="icon" href="{{ site.favicon_url | html.escape }}" />{{ end }}
	                                          {{ for feed in blog.feeds }}<link rel="alternate" type="{{ feed.type | html.escape }}" title="{{ feed.title | html.escape }}" href="{{ feed.url | html.escape }}" />
	                                          {{ end }}
	                                      </head>
	                                      <body{{ if theme.layout.home != "docs" }} class="landing"{{ end }}>
	                                          <a class="skip-link" href="#mokadocs-main">{{ t.a11y_skip_to_content | html.escape }}</a>
	                                          {{ if theme.banner }}
	                                          <div class="site-banner" id="mokadocs-banner" role="region" aria-label="{{ t.banner_label | html.escape }}" data-variant="{{ theme.banner.variant | html.escape }}" data-banner-id="{{ theme.banner.id | html.escape }}">
	                                              <div class="site-banner-inner">
	                                                  {{ if theme.banner.variant == "warning" }}<svg class="site-banner-icon" aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#warning"/></svg>{{ end }}
	                                                  <p class="site-banner-text">{{ theme.banner.text | html.escape }}{{ if theme.banner.link != "" }} <a class="site-banner-link" href="{{ theme.banner.link | html.escape }}">{{ theme.banner.link_text | html.escape }}<svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#arrow-right"/></svg></a>{{ end }}</p>
	                                                  {{ if theme.banner.dismissible }}<button class="site-banner-close js-only" type="button" aria-label="{{ t.banner_dismiss | html.escape }}"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#x"/></svg></button>{{ end }}
	                                              </div>
	                                          </div>
	                                          {{ if theme.banner.dismissible }}<script>try{const b=document.getElementById('mokadocs-banner');if(localStorage.getItem('mokadocs-banner-dismissed')===b.getAttribute('data-banner-id'))b.hidden=true}catch(e){}</script>{{ end }}
	                                          {{ end }}
	                                          <header class="site-header">
	                                              <div class="header-inner">
	                                                  <a class="site-logo" href="{{ base_path | html.escape }}/">
	                                                      {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="{{ site.title | html.escape }}" class="site-logo-img" />{{ end }}
	                                                      <span class="site-name">{{ site.title | html.escape }}</span>
	                                                  </a>
	                                                  <div class="header-actions">
	                                                      {{ if versions.size > 0 && theme.show_version_selector }}
	                                                      <div class="version-selector">
	                                                          <button class="version-trigger" type="button" aria-expanded="false" aria-controls="versionMenu">
	                                                              <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#pen"/></svg>
	                                                              <span class="visually-hidden">{{ t.version_label | html.escape }} </span><span class="version-label">{{ current_version | html.escape }}</span>
	                                                              <svg class="version-chevron" aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#chevron"/></svg>
	                                                          </button>
	                                                          <ul class="version-dropdown" id="versionMenu" hidden>
	                                                              {{ for v in versions }}
	                                                              <li>
	                                                                  <a class="version-option{{ if v.is_current }} active{{ end }}" href="{{ v.url | html.escape }}" data-version-slug="{{ v.slug | html.escape }}"{{ if v.is_current }} aria-current="page"{{ end }}>
	                                                                      <span class="version-option-label">{{ v.label | html.escape }}</span>
	                                                                      {{ if v.is_default }}<span class="version-badge version-badge-default">{{ t.version_latest | html.escape }}</span>{{ end }}
	                                                                      {{ if v.is_prerelease }}<span class="version-badge version-badge-pre">{{ t.version_prerelease | html.escape }}</span>{{ end }}
	                                                                  </a>
	                                                              </li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if search_enabled }}
	                                                      <button class="search-trigger" type="button" aria-label="{{ t.search_button | html.escape }}" aria-keyshortcuts="Control+K">
	                                                          <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#search"/></svg>
	                                                          <span class="search-label">{{ t.search_button | html.escape }}</span>
	                                                          <kbd aria-hidden="true">{{ t.search_shortcut | html.escape }}</kbd>
	                                                      </button>
	                                                      {{ end }}
	                                      """ + _appearanceMenu + """
	                                                      {{ if theme.show_dark_mode_toggle }}
	                                                      <button class="theme-toggle" type="button" aria-label="{{ t.appearance_dark_mode | html.escape }}" aria-pressed="false">
	                                                          <svg class="icon-sun" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#sun"/></svg>
	                                                          <svg class="icon-moon" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#moon"/></svg>
	                                                      </button>
	                                                      {{ end }}
	                                                      {{ if theme.layout.home == "docs" }}
	                                      """ + _mobileNavToggle + """
	                                                      {{ end }}
	                                                  </div>
	                                              </div>
	                                      """ + _headerTabs + """
	                                          </header>

	                                          {{ if theme.layout.home == "docs" }}
	                                      """ + _docsHome + """
	                                          {{ else }}
	                                          <main id="mokadocs-main" tabindex="-1">
	                                          {{ home = theme.layout.home }}
	                                          {{ hero_media = theme.layout.hero == "split" && home != "portal" && home != "minimal" && (page.hero_code != "" || page.hero_image != "") }}
	                                          <section class="landing-hero{{ if hero_media }} landing-hero-split{{ end }}">
	                                              {{ if home != "minimal" }}<div class="hero-grid-pattern"></div>{{ end }}
	                                              {{ if hero_media }}<div class="landing-hero-text">{{ end }}
	                                              {{ if theme.layout.hero != "minimal" && home != "portal" && home != "minimal" }}
	                                              <div class="landing-hero-icon">
	                                                  {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="" style="height:40px;width:auto" />{{ else }}<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#logo"/></svg>{{ end }}
	                                              </div>
	                                              {{ end }}
	                                              <h1 class="landing-hero-title">{{ site.title | html.escape }}</h1>
	                                              <p class="landing-hero-subtitle">{{ page.description | html.escape }}</p>
	                                              {{ if home == "portal" }}
	                                              {{ if search_enabled }}
	                                              <button class="home-search js-only" type="button" aria-keyshortcuts="Control+K">
	                                                  <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#search"/></svg>
	                                                  <span class="home-search-label">{{ t.landing_search | html.escape }}</span>
	                                                  <kbd aria-hidden="true">{{ t.search_shortcut | html.escape }}</kbd>
	                                              </button>
	                                              {{ end }}
	                                              {{ else }}
	                                              <div class="landing-hero-actions">
	                                                  <a class="landing-btn landing-btn-primary" href="{{ if first_nav_route != "" }}{{ first_nav_route | html.escape }}{{ else }}{{ base_path | html.escape }}/docs/{{ end }}">
	                                                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#arrow"/></svg>
	                                                      {{ t.landing_get_started | html.escape }}
	                                                  </a>
	                                                  {{ if site.repo_url != "" }}
	                                                  <a class="landing-btn landing-btn-secondary" href="{{ site.repo_url | html.escape }}" target="_blank" rel="noopener">
	                                                      <svg viewBox="0 0 24 24" fill="currentColor"><use href="{{ icon_sprite | html.escape }}#github-mark"/></svg>
	                                                      {{ t.landing_view_on_github | html.escape }}
	                                                  </a>
	                                                  {{ end }}
	                                              </div>
	                                              {{ end }}
	                                              {{ if hero_media }}</div>
	                                              <div class="landing-hero-media landing-content">
	                                                  {{ if page.hero_code != "" }}{{ page.hero_code }}{{ else }}<img src="{{ page.hero_image | html.escape }}" alt="" />{{ end }}
	                                              </div>{{ end }}
	                                          </section>

	                                          {{ if home == "portal" && home_sections.size > 0 }}
	                                          <nav class="home-sections" aria-label="{{ t.landing_sections | html.escape }}">
	                                              {{ for section in home_sections }}
	                                              <div class="home-section landing-feature-card">
	                                                  <h2 class="home-section-title"><a href="{{ section.url | html.escape }}">{{ if section.icon != "" }}<span class="home-section-icon">{{ section.icon }}</span>{{ end }}<span>{{ section.title | html.escape }}</span></a></h2>
	                                                  {{ if section.links.size > 0 }}
	                                                  <ul class="home-section-links">
	                                                      {{ for link in section.links }}<li><a href="{{ link.url | html.escape }}">{{ link.label | html.escape }}</a></li>{{ end }}
	                                                  </ul>
	                                                  {{ end }}
	                                              </div>
	                                              {{ end }}
	                                          </nav>
	                                          {{ end }}

	                                          {{ if page.features.size > 0 && home != "portal" && home != "minimal" }}
	                                          <!-- Features Grid -->
	                                          <section class="landing-features">
	                                              {{ if page.features_title != "" }}<h2 class="landing-features-title">{{ page.features_title | html.escape }}</h2>{{ end }}
	                                              {{ if page.features_subtitle != "" }}<p class="landing-features-subtitle">{{ page.features_subtitle | html.escape }}</p>{{ end }}
	                                              <div class="landing-features-grid">
	                                                  {{ for feature in page.features }}
	                                                  {{ if feature.link != "" }}<a class="landing-feature-card" href="{{ feature.link | html.escape }}">{{ else }}<div class="landing-feature-card">{{ end }}
	                                                      {{ if feature.icon_svg != "" }}<div class="landing-feature-icon">{{ feature.icon_svg }}</div>{{ else if feature.icon != "" }}<div class="landing-feature-icon">{{ feature.icon | html.escape }}</div>{{ end }}
	                                                      {{ if feature.title != "" }}<div class="landing-feature-name">{{ feature.title | html.escape }}</div>{{ end }}
	                                                      {{ if feature.description != "" }}<p class="landing-feature-desc">{{ feature.description | html.escape }}</p>{{ end }}
	                                                  {{ if feature.link != "" }}</a>{{ else }}</div>{{ end }}
	                                                  {{ end }}
	                                              </div>
	                                          </section>
	                                          {{ end }}

	                                          {{ showcase_bands = home == "showcase" && (page.showcase_code != "" || page.showcase_title != "" || page.showcase_text != "" || page.stats.size > 0 || page.cta) }}
	                                          {{ if showcase_bands }}
	                                          {{ if page.showcase_code != "" || page.showcase_title != "" || page.showcase_text != "" }}
	                                          <section class="home-showcase{{ if page.showcase_code == "" }} home-showcase-plain{{ end }}">
	                                              {{ if page.showcase_title != "" || page.showcase_text != "" }}
	                                              <div class="home-showcase-text">
	                                                  {{ if page.showcase_title != "" }}<h2 class="home-showcase-title">{{ page.showcase_title | html.escape }}</h2>{{ end }}
	                                                  {{ if page.showcase_text != "" }}<p class="home-showcase-desc">{{ page.showcase_text | html.escape }}</p>{{ end }}
	                                              </div>
	                                              {{ end }}
	                                              {{ if page.showcase_code != "" }}<div class="home-showcase-code landing-content">{{ page.showcase_code }}</div>{{ end }}
	                                          </section>
	                                          {{ end }}
	                                          {{ if page.stats.size > 0 }}
	                                          <section class="home-stats" aria-label="{{ t.landing_key_figures | html.escape }}">
	                                              <ul class="home-stats-list">
	                                                  {{ for stat in page.stats }}<li class="home-stat">{{ if stat.value != "" }}<span class="home-stat-value">{{ stat.value | html.escape }}</span>{{ end }}{{ if stat.value != "" && stat.label != "" }} {{ end }}{{ if stat.label != "" }}<span class="home-stat-label">{{ stat.label | html.escape }}</span>{{ end }}</li>{{ end }}
	                                              </ul>
	                                          </section>
	                                          {{ end }}
	                                          {{ if page.cta }}
	                                          <section class="home-cta">
	                                              {{ if page.cta.title != "" }}<h2 class="home-cta-title">{{ page.cta.title | html.escape }}</h2>{{ end }}
	                                              {{ if page.cta.text != "" }}<p class="home-cta-text">{{ page.cta.text | html.escape }}</p>{{ end }}
	                                              {{ if page.cta.link != "" }}
	                                              <a class="landing-btn landing-btn-primary home-cta-button" href="{{ page.cta.link | html.escape }}">
	                                                  {{ page.cta.link_text | html.escape }}
	                                                  <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#arrow"/></svg>
	                                              </a>
	                                              {{ end }}
	                                          </section>
	                                          {{ end }}
	                                          {{ end }}

	                                          {{ if page.features.size > 0 && page.content != "" && (home == "default" || (home == "showcase" && !showcase_bands)) }}<hr class="landing-divider" />{{ end }}

	                                          <!-- Rendered markdown content from index.md -->
	                                          {{ if page.content != "" }}
	                                          <div class="landing-md-content landing-content">
	                                              {{ page.content }}
	                                          </div>
	                                          {{ end }}
	                                          </main>
	                                          {{ end }}

	                                          <!-- Footer -->
	                                          {{ if theme.layout.footer == "columns" || theme.layout_selector }}
	                                      """ + _columnsFooter + """
	                                          {{ end }}
	                                          {{ if (theme.layout.footer != "columns" && theme.layout.footer != "none") || theme.layout_selector }}
	                                          <footer class="landing-footer">
	                                              {{ if theme.show_built_with != false }}<p>{{ t.footer_built_with_love | html.escape | string.replace "{heart}" '<span class="landing-footer-heart">&#x2764;</span>' | string.replace "{mokadocs}" ('<a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v' + mokadocs_version) }}</p>{{ end }}
	                                              {{ if site.copyright }}<p style="margin-top:0.5rem;">{{ site.copyright }}</p>{{ end }}
	                                          </footer>
	                                          {{ end }}

	                                          {{ if search_enabled }}
	                                          <div class="search-modal" id="searchModal"{{ if search_index_url != "" }} data-index="{{ search_index_url | html.escape }}"{{ end }} hidden>
	                                              <div class="search-backdrop"></div>
	                                              <div class="search-dialog" role="dialog" aria-modal="true" aria-label="{{ t.search_label | html.escape }}">
	                                                  <div class="search-input-wrap">
	                                                      <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#search"/></svg>
	                                                      <label class="visually-hidden" for="searchInput">{{ t.search_label | html.escape }}</label>
	                                                      <input type="text" class="search-input" id="searchInput" placeholder="{{ t.search_placeholder | html.escape }}" autocomplete="off" spellcheck="false" enterkeyhint="search" role="combobox" aria-expanded="false" aria-controls="searchResults" aria-autocomplete="list" />
	                                                      <button type="button" class="search-close" aria-label="{{ t.search_close | html.escape }}"><kbd class="search-esc" aria-hidden="true">Esc</kbd><svg class="search-close-icon" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><use href="{{ icon_sprite | html.escape }}#close"/></svg></button>
	                                                  </div>
	                                                  <p class="search-message" id="searchMessage">{{ t.search_prompt | html.escape }}</p>
	                                                  <div class="search-results" id="searchResults" role="listbox" aria-label="{{ t.search_results | html.escape }}"></div>
	                                                  <div class="visually-hidden" id="searchStatus" role="status" aria-live="polite"></div>
	                                              </div>
	                                          </div>
	                                          {{ end }}

	                                          {{ for js in js_files }}<script src="{{ js | html.escape }}"></script>
	                                          {{ end }}
	                                      </body>
	                                      </html>
	                                      """;

	private const string _sidebarNav = """

	                                                  <nav class="sidebar-nav" aria-label="{{ t.nav_label | html.escape }}">
	                                                      {{ for item in nav }}
	                                                      <div class="nav-section{{ if item.is_active || item.has_active_child }} active{{ end }}">
	                                                          {{ if item.has_children }}
	                                                          <div class="nav-header">
	                                                              {{ if item.has_page }}
	                                                              <a class="nav-link{{ if item.is_active }} current{{ else if item.has_active_child }} parent-active{{ end }}" href="{{ item.route | html.escape }}"{{ if item.is_active }} aria-current="page"{{ end }}>
	                                                                  {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
	                                                                  {{ item.label | html.escape }}
	                                                              </a>
	                                                              {{ else }}
	                                                              <span class="nav-link nav-group-label{{ if item.has_active_child }} parent-active{{ end }}">
	                                                                  {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
	                                                                  {{ item.label | html.escape }}
	                                                              </span>
	                                                              {{ end }}
	                                                              <button class="nav-chevron-btn nav-toggle" aria-label="{{ t.nav_toggle_section | string.replace "{label}" item.label | html.escape }}" type="button" aria-expanded="{{ if item.expanded || item.has_active_child }}true{{ else }}false{{ end }}">
	                                                                  <svg class="nav-chevron" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#chevron"/></svg>
	                                                              </button>
	                                                          </div>
	                                                          {{ else }}
	                                                          <a class="nav-link{{ if item.is_active }} current{{ end }}" href="{{ item.route | html.escape }}"{{ if item.is_active }} aria-current="page"{{ end }}>
	                                                              {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
	                                                              {{ item.label | html.escape }}
	                                                          </a>
	                                                          {{ end }}
	                                                          {{ if item.children.size > 0 }}
	                                                          <ul class="nav-children{{ if item.expanded || item.has_active_child }} expanded{{ end }}">
	                                                              {{ for child in item.children }}
	                                                              <li>
	                                                                  {{ if child.has_children }}
	                                                                  <div class="nav-sub-section{{ if child.is_active || child.has_active_child }} active{{ end }}">
	                                                                      <div class="nav-header">
	                                                                          {{ if child.has_page }}
	                                                                          <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route | html.escape }}"{{ if child.is_active }} aria-current="page"{{ end }}>{{ child.label | html.escape }}</a>
	                                                                          {{ else }}
	                                                                          <span class="nav-link nav-group-label">{{ child.label | html.escape }}</span>
	                                                                          {{ end }}
	                                                                          <button class="nav-chevron-btn nav-toggle" aria-label="{{ t.nav_toggle_section | string.replace "{label}" child.label | html.escape }}" type="button" aria-expanded="{{ if child.expanded || child.has_active_child }}true{{ else }}false{{ end }}">
	                                                                              <svg class="nav-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#chevron"/></svg>
	                                                                          </button>
	                                                                      </div>
	                                                                      <ul class="nav-children{{ if child.expanded || child.has_active_child }} expanded{{ end }}">
	                                                                          {{ for grandchild in child.children }}
	                                                                          <li>
	                                                                              <a class="nav-link{{ if grandchild.is_active }} current{{ end }}" href="{{ grandchild.route | html.escape }}"{{ if grandchild.is_active }} aria-current="page"{{ end }}>{{ grandchild.label | html.escape }}</a>
	                                                                          </li>
	                                                                          {{ end }}
	                                                                      </ul>
	                                                                  </div>
	                                                                  {{ else }}
	                                                                  <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route | html.escape }}"{{ if child.is_active }} aria-current="page"{{ end }}>
	                                                                      {{ if child.icon != "" }}<span class="nav-icon">{{ child.icon }}</span>{{ end }}
	                                                                      {{ child.label | html.escape }}
	                                                                  </a>
	                                                                  {{ end }}
	                                                              </li>
	                                                              {{ end }}
	                                                          </ul>
	                                                          {{ end }}
	                                                      </div>
	                                                      {{ end }}
	                                                  </nav>
	                                      """;

	// The sidebar and its show/hide button, shared by the default layout and the landing layout's docs
	// home page. The shared sidebar script runs from inside the aside and reads its data-nav-route.
	private const string _sidebarAside = """

	                                              <aside class="sidebar" id="mokadocs-sidebar"{{ if sidebar_script != "" }} data-nav-route="{{ page.route | html.escape }}"{{ end }}>
	                                      """ + _sidebarNav + """

	                                              {{ if sidebar_script != "" }}<script src="{{ sidebar_script | html.escape }}"></script>{{ end }}</aside>
	                                              {{ if theme.sidebar_toggle }}
	                                              <button class="sidebar-toggle js-only" id="sidebarToggle" type="button" aria-label="{{ t.nav_toggle_sidebar | html.escape }}" aria-controls="mokadocs-sidebar" aria-expanded="true">
	                                                  <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#panel-left"/></svg>
	                                              </button>
	                                              <script>try{if(localStorage.getItem('mokadocs-sidebar-hidden')==='true'){document.documentElement.classList.add('sidebar-hidden');document.getElementById('sidebarToggle').setAttribute('aria-expanded','false')}}catch(e){}</script>
	                                              {{ end }}

	                                      """;

	private const string _mobileNavToggle = """

	                                                      <button class="mobile-nav-toggle" type="button" aria-label="{{ t.nav_toggle_navigation | html.escape }}" aria-expanded="false" aria-controls="mokadocs-sidebar">
	                                                          <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><use href="{{ icon_sprite | html.escape }}#menu-lines"/></svg>
	                                                      </button>

	                                      """;

	// theme.options.layout.home: docs. The landing page as a docs page: the sidebar, the title and
	// description as the page header, the feature cards as link cards, then the content.
	private const string _docsHome = """

	                                          <div class="site-container">
	                                      """ + _sidebarAside + """
	                                              <main class="content" id="mokadocs-main" tabindex="-1">
	                                                  <article class="page-content home-docs">
	                                                      <div class="home-docs-header">
	                                                          <h1>{{ site.title | html.escape }}</h1>
	                                                          {{ if page.description != "" }}<p class="home-docs-lead">{{ page.description | html.escape }}</p>{{ end }}
	                                                      </div>
	                                                      {{ if page.features.size > 0 }}
	                                                      <section class="home-docs-features">
	                                                          {{ if page.features_title != "" }}<h2 class="home-docs-features-title">{{ page.features_title | html.escape }}</h2>{{ end }}
	                                                          {{ if page.features_subtitle != "" }}<p class="home-docs-features-subtitle">{{ page.features_subtitle | html.escape }}</p>{{ end }}
	                                                          <div class="component-link-cards home-docs-cards">
	                                                              {{ for feature in page.features }}
	                                                              {{ if feature.link != "" }}<a class="component-link-card" href="{{ feature.link | html.escape }}">{{ else }}<div class="component-link-card">{{ end }}
	                                                                  {{ if feature.icon_svg != "" }}<span class="home-docs-card-icon">{{ feature.icon_svg }}</span>{{ else if feature.icon != "" }}<span class="home-docs-card-icon">{{ feature.icon | html.escape }}</span>{{ end }}
	                                                                  <div class="component-link-card-content">
	                                                                      {{ if feature.title != "" }}<span class="component-link-card-title">{{ feature.title | html.escape }}</span>{{ end }}
	                                                                      {{ if feature.description != "" }}<span class="component-link-card-desc">{{ feature.description | html.escape }}</span>{{ end }}
	                                                                  </div>
	                                                                  {{ if feature.link != "" }}<span class="component-link-card-arrow"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#next"/></svg></span>{{ end }}
	                                                              {{ if feature.link != "" }}</a>{{ else }}</div>{{ end }}
	                                                              {{ end }}
	                                                          </div>
	                                                      </section>
	                                                      {{ end }}
	                                                      {{ page.content }}
	                                                  </article>
	                                              </main>
	                                          </div>

	                                      """;

	private const string _headPartial = "";
	private const string _navSidebarPartial = "";
	private const string _tocPartial = "";
	private const string _breadcrumbsPartial = "";
	private const string _footerPartial = "";
}
