using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _sidebarHtmlPlaceholder = "__SIDEBAR_HTML__";

	// Runs where the page's <script> tag is, inside the sidebar and before the page content, so the
	// whole tree is in place, opened at the current page, before anything is painted. Pages keep
	// their short sidebar for readers without JavaScript and for crawlers.
	private const string _sidebarJs = """
	                                  (function () {
	                                      var script = document.currentScript;
	                                      var aside = script && script.parentElement;
	                                      var fallback = aside && aside.querySelector('.sidebar-nav');
	                                      if (!fallback) return;
	                                      var template = document.createElement('template');
	                                      template.innerHTML = __SIDEBAR_HTML__;
	                                      var nav = template.content.querySelector('.sidebar-nav');
	                                      if (!nav) return;
	                                      markCurrent(nav, aside.getAttribute('data-nav-route'));
	                                      fallback.parentNode.replaceChild(nav, fallback);
	                                      showCurrent(aside, nav);

	                                      function hasClass(el, name) { return el.classList.contains(name); }

	                                      function childWithClass(el, name) {
	                                          for (var i = 0; i < el.children.length; i++) {
	                                              if (hasClass(el.children[i], name)) return el.children[i];
	                                          }
	                                          return null;
	                                      }

	                                      // The section a link heads, or the top-level entry it is; null for a plain list item.
	                                      function ownSection(link) {
	                                          var parent = link.parentElement;
	                                          if (hasClass(parent, 'nav-header')) return parent.parentElement;
	                                          return hasClass(parent, 'nav-section') ? parent : null;
	                                      }

	                                      // As in the layout, a section that shares its route with one of its pages
	                                      // isn't current; the page is.
	                                      function markCurrent(nav, route) {
	                                          if (!route) return;
	                                          var links = nav.querySelectorAll('a.nav-link');
	                                          var hits = [];
	                                          for (var i = 0; i < links.length; i++) {
	                                              if (links[i].getAttribute('href') === route) hits.push(links[i]);
	                                          }
	                                          hits.forEach(function (link) {
	                                              var own = ownSection(link);
	                                              var list = own && childWithClass(own, 'nav-children');
	                                              if (list && hits.some(function (other) { return list.contains(other); })) return;
	                                              link.classList.add('current');
	                                              link.setAttribute('aria-current', 'page');
	                                              if (own) own.classList.add('active');
	                                              openAncestors(link, nav);
	                                          });
	                                      }

	                                      function openAncestors(link, nav) {
	                                          for (var el = link.parentElement; el && el !== nav; el = el.parentElement) {
	                                              if (!hasClass(el, 'nav-children')) continue;
	                                              el.classList.add('expanded');
	                                              var section = el.parentElement;
	                                              section.classList.add('active');
	                                              var header = childWithClass(section, 'nav-header');
	                                              if (!header) continue;
	                                              var toggle = childWithClass(header, 'nav-toggle');
	                                              if (toggle) toggle.setAttribute('aria-expanded', 'true');
	                                              var label = childWithClass(header, 'nav-link');
	                                              if (label && hasClass(section, 'nav-section')) label.classList.add('parent-active');
	                                          }
	                                      }

	                                      // Scrolled before the first paint, so a page deep in a long tree doesn't open
	                                      // with its entry out of sight.
	                                      function showCurrent(aside, nav) {
	                                          var current = nav.querySelector('.nav-link.current');
	                                          if (!current || !aside.clientHeight) return;
	                                          var offset = current.getBoundingClientRect().top - aside.getBoundingClientRect().top;
	                                          if (offset + current.offsetHeight > aside.clientHeight) {
	                                              aside.scrollTop = offset - aside.clientHeight / 3;
	                                          }
	                                      }
	                                  })();

	                                  """;

	/// <summary>
	///     The Scriban markup of the sidebar navigation, the <c>nav.sidebar-nav</c> element that the
	///     default layout writes into every page. A build with a shared sidebar renders it once more,
	///     with no current page, for <see cref="CreateSidebarScript" />.
	/// </summary>
	public static string GetSidebarTemplate() => _sidebarTemplate;

	/// <summary>
	///     The script that pages with a shared sidebar load. It replaces the page's short sidebar with
	///     <paramref name="sidebarHtml" />, after marking the page's entry current and opening the
	///     sections that hold it, so the reader never sees the tree collapsed.
	/// </summary>
	/// <param name="sidebarHtml">The rendered <see cref="GetSidebarTemplate" />.</param>
	/// <returns>The script's source.</returns>
	public static string CreateSidebarScript(string sidebarHtml)
	{
		ArgumentNullException.ThrowIfNull(sidebarHtml);

		// The markup has no <pre>, so a run of whitespace holding a line break renders like a single
		// line break. The layout's indentation was most of the file.
		string compact = Regex.Replace(sidebarHtml.Trim(), @"\s*\n\s*", "\n");

		// The script is a file of its own, never inline in a page, so HTML characters need no
		// escaping; the default encoder writes every < and > as six characters. This one still
		// escapes the line and paragraph separators that older browsers reject inside a string.
		string literal = "\"" + JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(compact) + "\"";

		return _sidebarJs.Replace(_sidebarHtmlPlaceholder, literal, StringComparison.Ordinal);
	}
}
