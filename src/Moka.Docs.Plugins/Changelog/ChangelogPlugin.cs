using System.Text;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.Changelog;

/// <summary>
///     MokaDocs plugin that adds rich changelog/release notes styling and interactivity.
///     Pages containing <c>.changelog</c> containers (rendered by ChangelogExtension) are
///     enhanced with inline CSS for timeline visuals and JS for collapse/expand, filtering,
///     and scroll-triggered animations.
/// </summary>
public sealed class ChangelogPlugin : IMokaPlugin
{
	#region Inline CSS

	private const string InlineCss = """
	                                 <style>
	                                 /* ── Changelog Timeline ─────────────────────────────────────────── */
	                                 :root {
	                                     --cl-bg: #ffffff;
	                                     --cl-bg-alt: #f8fafc;
	                                     --cl-text: #1e293b;
	                                     --cl-text-muted: #64748b;
	                                     --cl-border: #e2e8f0;
	                                     --cl-line: #cbd5e1;
	                                     --cl-dot-ring: #ffffff;
	                                     --cl-shadow: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
	                                     --cl-shadow-hover: 0 4px 12px rgba(0,0,0,0.08), 0 2px 4px rgba(0,0,0,0.04);
	                                     --cl-radius: 12px;
	                                     --cl-major: #ef4444;
	                                     --cl-minor: #3b82f6;
	                                     --cl-patch: #22c55e;
	                                     --cl-initial: #8b5cf6;
	                                     --cl-added: #16a34a;
	                                     --cl-changed: #2563eb;
	                                     --cl-fixed: #ea580c;
	                                     --cl-breaking: #dc2626;
	                                     --cl-deprecated: #ca8a04;
	                                     --cl-removed: #dc2626;
	                                     --cl-security: #7c3aed;
	                                     --cl-filter-bg: #f1f5f9;
	                                     --cl-filter-active: #0f172a;
	                                     --cl-filter-active-text: #ffffff;
	                                     --cl-code-bg: #f1f5f9;
	                                     --cl-code-text: #0f172a;
	                                 }

	                                 @media (prefers-color-scheme: dark) {
	                                     :root {
	                                         --cl-bg: #1e1e2e;
	                                         --cl-bg-alt: #181825;
	                                         --cl-text: #cdd6f4;
	                                         --cl-text-muted: #7f849c;
	                                         --cl-border: #313244;
	                                         --cl-line: #45475a;
	                                         --cl-dot-ring: #1e1e2e;
	                                         --cl-shadow: 0 1px 3px rgba(0,0,0,0.3), 0 1px 2px rgba(0,0,0,0.2);
	                                         --cl-shadow-hover: 0 4px 12px rgba(0,0,0,0.4), 0 2px 4px rgba(0,0,0,0.3);
	                                         --cl-filter-bg: #313244;
	                                         --cl-filter-active: #cdd6f4;
	                                         --cl-filter-active-text: #1e1e2e;
	                                         --cl-code-bg: #313244;
	                                         --cl-code-text: #cdd6f4;
	                                     }
	                                 }

	                                 .changelog {
	                                     position: relative;
	                                     padding: 0;
	                                     max-width: 860px;
	                                 }

	                                 /* ── Filter Bar ───────────────────────────────────────────────── */
	                                 .changelog-filters {
	                                     display: flex;
	                                     flex-wrap: wrap;
	                                     gap: 6px;
	                                     margin-bottom: 1.75rem;
	                                     padding: 12px 16px;
	                                     background: var(--cl-bg-alt);
	                                     border: 1px solid var(--cl-border);
	                                     border-radius: var(--cl-radius);
	                                 }

	                                 .changelog-filter-btn {
	                                     display: inline-flex;
	                                     align-items: center;
	                                     gap: 5px;
	                                     padding: 5px 12px;
	                                     font-size: 0.78rem;
	                                     font-weight: 600;
	                                     font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
	                                     color: var(--cl-text-muted);
	                                     background: var(--cl-filter-bg);
	                                     border: 1px solid transparent;
	                                     border-radius: 6px;
	                                     cursor: pointer;
	                                     transition: all 0.15s ease;
	                                     user-select: none;
	                                 }

	                                 .changelog-filter-btn:hover {
	                                     color: var(--cl-text);
	                                     border-color: var(--cl-border);
	                                 }

	                                 .changelog-filter-btn.active {
	                                     background: var(--cl-filter-active);
	                                     color: var(--cl-filter-active-text);
	                                     border-color: transparent;
	                                 }

	                                 .changelog-filter-count {
	                                     display: inline-flex;
	                                     align-items: center;
	                                     justify-content: center;
	                                     min-width: 18px;
	                                     height: 18px;
	                                     padding: 0 5px;
	                                     font-size: 0.65rem;
	                                     font-weight: 700;
	                                     border-radius: 9px;
	                                     background: rgba(0,0,0,0.1);
	                                     color: inherit;
	                                 }

	                                 .changelog-filter-btn.active .changelog-filter-count {
	                                     background: rgba(255,255,255,0.2);
	                                 }

	                                 /* ── Entry ────────────────────────────────────────────────────── */
	                                 .changelog-entry {
	                                     display: flex;
	                                     gap: 0;
	                                     position: relative;
	                                     padding-bottom: 2rem;
	                                     opacity: 0;
	                                     transform: translateY(16px);
	                                     transition: opacity 0.5s ease, transform 0.5s ease;
	                                 }

	                                 .changelog-entry.changelog-visible {
	                                     opacity: 1;
	                                     transform: translateY(0);
	                                 }

	                                 .changelog-entry:last-child {
	                                     padding-bottom: 0;
	                                 }

	                                 .changelog-entry:last-child .changelog-line {
	                                     display: none;
	                                 }

	                                 /* ── Timeline Column ──────────────────────────────────────────── */
	                                 .changelog-timeline {
	                                     display: flex;
	                                     flex-direction: column;
	                                     align-items: center;
	                                     width: 40px;
	                                     min-width: 40px;
	                                     padding-top: 6px;
	                                 }

	                                 .changelog-dot {
	                                     width: 14px;
	                                     height: 14px;
	                                     border-radius: 50%;
	                                     border: 3px solid var(--cl-dot-ring);
	                                     box-shadow: 0 0 0 2px var(--cl-line);
	                                     background: var(--cl-minor);
	                                     z-index: 1;
	                                     transition: transform 0.2s ease, box-shadow 0.2s ease;
	                                     flex-shrink: 0;
	                                 }

	                                 .changelog-entry:hover .changelog-dot {
	                                     transform: scale(1.3);
	                                     box-shadow: 0 0 0 3px var(--cl-line);
	                                 }

	                                 .changelog-entry[data-type="major"] .changelog-dot { background: var(--cl-major); }
	                                 .changelog-entry[data-type="minor"] .changelog-dot { background: var(--cl-minor); }
	                                 .changelog-entry[data-type="patch"] .changelog-dot { background: var(--cl-patch); }
	                                 .changelog-entry[data-type="initial"] .changelog-dot { background: var(--cl-initial); }

	                                 .changelog-line {
	                                     width: 2px;
	                                     flex: 1;
	                                     background: var(--cl-line);
	                                     margin-top: 4px;
	                                 }

	                                 /* ── Content Column ───────────────────────────────────────────── */
	                                 .changelog-content {
	                                     flex: 1;
	                                     min-width: 0;
	                                     padding: 0 0 0 16px;
	                                 }

	                                 /* ── Header ───────────────────────────────────────────────────── */
	                                 .changelog-header {
	                                     display: flex;
	                                     align-items: center;
	                                     flex-wrap: wrap;
	                                     gap: 10px;
	                                     padding: 10px 16px;
	                                     background: var(--cl-bg-alt);
	                                     border: 1px solid var(--cl-border);
	                                     border-radius: var(--cl-radius) var(--cl-radius) 0 0;
	                                     cursor: pointer;
	                                     user-select: none;
	                                     transition: background 0.15s ease, box-shadow 0.15s ease;
	                                 }

	                                 .changelog-header:hover {
	                                     background: var(--cl-bg);
	                                     box-shadow: var(--cl-shadow);
	                                 }

	                                 .changelog-entry.changelog-collapsed .changelog-header {
	                                     border-radius: var(--cl-radius);
	                                 }

	                                 .changelog-version {
	                                     font-size: 1.15rem;
	                                     font-weight: 800;
	                                     font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                     color: var(--cl-text);
	                                     letter-spacing: -0.02em;
	                                 }

	                                 .changelog-badge {
	                                     display: inline-block;
	                                     padding: 2px 10px;
	                                     font-size: 0.7rem;
	                                     font-weight: 700;
	                                     text-transform: uppercase;
	                                     letter-spacing: 0.05em;
	                                     border-radius: 4px;
	                                     line-height: 1.4;
	                                 }

	                                 .changelog-badge-major {
	                                     background: #fef2f2;
	                                     color: var(--cl-major);
	                                     border: 1px solid #fecaca;
	                                 }
	                                 .changelog-badge-minor {
	                                     background: #eff6ff;
	                                     color: var(--cl-minor);
	                                     border: 1px solid #bfdbfe;
	                                 }
	                                 .changelog-badge-patch {
	                                     background: #f0fdf4;
	                                     color: var(--cl-patch);
	                                     border: 1px solid #bbf7d0;
	                                 }
	                                 .changelog-badge-initial {
	                                     background: #f5f3ff;
	                                     color: var(--cl-initial);
	                                     border: 1px solid #ddd6fe;
	                                 }

	                                 @media (prefers-color-scheme: dark) {
	                                     .changelog-badge-major {
	                                         background: rgba(239,68,68,0.12);
	                                         border-color: rgba(239,68,68,0.25);
	                                     }
	                                     .changelog-badge-minor {
	                                         background: rgba(59,130,246,0.12);
	                                         border-color: rgba(59,130,246,0.25);
	                                     }
	                                     .changelog-badge-patch {
	                                         background: rgba(34,197,94,0.12);
	                                         border-color: rgba(34,197,94,0.25);
	                                     }
	                                     .changelog-badge-initial {
	                                         background: rgba(139,92,246,0.12);
	                                         border-color: rgba(139,92,246,0.25);
	                                     }
	                                 }

	                                 .changelog-date {
	                                     font-size: 0.82rem;
	                                     color: var(--cl-text-muted);
	                                     margin-left: auto;
	                                 }

	                                 .changelog-toggle-icon {
	                                     display: inline-flex;
	                                     margin-left: 8px;
	                                     transition: transform 0.25s ease;
	                                     color: var(--cl-text-muted);
	                                 }

	                                 .changelog-collapsed .changelog-toggle-icon {
	                                     transform: rotate(-90deg);
	                                 }

	                                 /* ── Categories (collapsible body) ────────────────────────────── */
	                                 .changelog-body {
	                                     border: 1px solid var(--cl-border);
	                                     border-top: none;
	                                     border-radius: 0 0 var(--cl-radius) var(--cl-radius);
	                                     background: var(--cl-bg);
	                                     overflow: hidden;
	                                     transition: max-height 0.35s ease, opacity 0.25s ease;
	                                 }

	                                 .changelog-collapsed .changelog-body {
	                                     max-height: 0 !important;
	                                     opacity: 0;
	                                     border-color: transparent;
	                                 }

	                                 .changelog-category {
	                                     padding: 12px 20px 8px;
	                                 }

	                                 .changelog-category + .changelog-category {
	                                     border-top: 1px solid var(--cl-border);
	                                 }

	                                 .changelog-category-title {
	                                     display: flex;
	                                     align-items: center;
	                                     gap: 6px;
	                                     margin: 0 0 8px 0;
	                                     padding: 0;
	                                     font-size: 0.82rem;
	                                     font-weight: 700;
	                                     text-transform: uppercase;
	                                     letter-spacing: 0.05em;
	                                 }

	                                 .changelog-added { color: var(--cl-added); }
	                                 .changelog-changed { color: var(--cl-changed); }
	                                 .changelog-fixed { color: var(--cl-fixed); }
	                                 .changelog-breaking { color: var(--cl-breaking); }
	                                 .changelog-deprecated { color: var(--cl-deprecated); }
	                                 .changelog-removed { color: var(--cl-removed); }
	                                 .changelog-security { color: var(--cl-security); }

	                                 .changelog-category ul {
	                                     margin: 0;
	                                     padding: 0 0 0 4px;
	                                     list-style: none;
	                                 }

	                                 .changelog-category li {
	                                     position: relative;
	                                     padding: 5px 10px 5px 18px;
	                                     font-size: 0.9rem;
	                                     line-height: 1.55;
	                                     color: var(--cl-text);
	                                     border-radius: 6px;
	                                     transition: background 0.15s ease;
	                                 }

	                                 .changelog-category li::before {
	                                     content: "";
	                                     position: absolute;
	                                     left: 4px;
	                                     top: 13px;
	                                     width: 5px;
	                                     height: 5px;
	                                     border-radius: 50%;
	                                     background: var(--cl-line);
	                                 }

	                                 .changelog-category li:hover {
	                                     background: var(--cl-bg-alt);
	                                 }

	                                 .changelog-category li code {
	                                     font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                     font-size: 0.82em;
	                                     padding: 1px 6px;
	                                     background: var(--cl-code-bg);
	                                     color: var(--cl-code-text);
	                                     border-radius: 4px;
	                                 }

	                                 /* ── Hidden categories (filtered out) ─────────────────────────── */
	                                 .changelog-category-hidden {
	                                     display: none;
	                                 }

	                                 /* ── Responsive ───────────────────────────────────────────────── */
	                                 @media (max-width: 600px) {
	                                     .changelog-timeline {
	                                         width: 28px;
	                                         min-width: 28px;
	                                     }

	                                     .changelog-dot {
	                                         width: 10px;
	                                         height: 10px;
	                                         border-width: 2px;
	                                     }

	                                     .changelog-content {
	                                         padding-left: 10px;
	                                     }

	                                     .changelog-header {
	                                         padding: 8px 12px;
	                                         gap: 6px;
	                                     }

	                                     .changelog-version {
	                                         font-size: 1rem;
	                                     }

	                                     .changelog-date {
	                                         display: none;
	                                     }

	                                     .changelog-category {
	                                         padding: 10px 14px 6px;
	                                     }
	                                 }
	                                 </style>
	                                 """;

	#endregion

	#region Inline JS

	private const string InlineJs = """
	                                <script>
	                                (function() {
	                                    var chevronSvg = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>';

	                                    document.querySelectorAll('.changelog').forEach(function(changelog) {
	                                        var entries = changelog.querySelectorAll('.changelog-entry');
	                                        if (entries.length === 0) return;

	                                        // ── Build filter bar ────────────────────────────────────
	                                        var categoryMap = {};
	                                        entries.forEach(function(entry) {
	                                            entry.querySelectorAll('.changelog-category').forEach(function(cat) {
	                                                var name = cat.getAttribute('data-category');
	                                                if (name) {
	                                                    categoryMap[name] = (categoryMap[name] || 0) + cat.querySelectorAll('li').length;
	                                                }
	                                            });
	                                        });

	                                        var categoryOrder = ['added','changed','fixed','breaking','deprecated','removed','security'];
	                                        var filterBar = document.createElement('div');
	                                        filterBar.className = 'changelog-filters';

	                                        var allBtn = document.createElement('button');
	                                        allBtn.className = 'changelog-filter-btn active';
	                                        allBtn.type = 'button';
	                                        allBtn.setAttribute('data-filter', 'all');
	                                        allBtn.textContent = 'All';
	                                        filterBar.appendChild(allBtn);

	                                        var filterButtons = [allBtn];

	                                        categoryOrder.forEach(function(cat) {
	                                            if (!categoryMap[cat]) return;
	                                            var btn = document.createElement('button');
	                                            btn.className = 'changelog-filter-btn';
	                                            btn.type = 'button';
	                                            btn.setAttribute('data-filter', cat);
	                                            var label = cat.charAt(0).toUpperCase() + cat.slice(1);
	                                            btn.innerHTML = label + ' <span class="changelog-filter-count">' + categoryMap[cat] + '</span>';
	                                            filterBar.appendChild(btn);
	                                            filterButtons.push(btn);
	                                        });

	                                        changelog.insertBefore(filterBar, changelog.firstChild);

	                                        // Filter logic
	                                        var activeFilter = 'all';
	                                        filterBar.addEventListener('click', function(e) {
	                                            var btn = e.target.closest('.changelog-filter-btn');
	                                            if (!btn) return;
	                                            var filter = btn.getAttribute('data-filter');
	                                            activeFilter = filter;

	                                            filterButtons.forEach(function(b) { b.classList.remove('active'); });
	                                            btn.classList.add('active');

	                                            entries.forEach(function(entry) {
	                                                entry.querySelectorAll('.changelog-category').forEach(function(cat) {
	                                                    var catName = cat.getAttribute('data-category');
	                                                    if (filter === 'all' || catName === filter) {
	                                                        cat.classList.remove('changelog-category-hidden');
	                                                    } else {
	                                                        cat.classList.add('changelog-category-hidden');
	                                                    }
	                                                });
	                                            });
	                                        });

	                                        // ── Collapse/expand each entry ──────────────────────────
	                                        entries.forEach(function(entry, index) {
	                                            var header = entry.querySelector('.changelog-header');
	                                            if (!header) return;

	                                            // Wrap categories in a body div for collapsing
	                                            var categories = entry.querySelectorAll('.changelog-category');
	                                            var body = document.createElement('div');
	                                            body.className = 'changelog-body';
	                                            var content = entry.querySelector('.changelog-content');
	                                            categories.forEach(function(cat) {
	                                                body.appendChild(cat);
	                                            });
	                                            content.appendChild(body);

	                                            // Add toggle chevron
	                                            var toggleIcon = document.createElement('span');
	                                            toggleIcon.className = 'changelog-toggle-icon';
	                                            toggleIcon.innerHTML = chevronSvg;
	                                            header.appendChild(toggleIcon);

	                                            // Collapse all except the first entry
	                                            if (index > 0) {
	                                                entry.classList.add('changelog-collapsed');
	                                                body.style.maxHeight = '0';
	                                            } else {
	                                                body.style.maxHeight = body.scrollHeight + 'px';
	                                            }

	                                            header.addEventListener('click', function() {
	                                                var isCollapsed = entry.classList.contains('changelog-collapsed');
	                                                if (isCollapsed) {
	                                                    entry.classList.remove('changelog-collapsed');
	                                                    body.style.maxHeight = body.scrollHeight + 'px';
	                                                    body.style.opacity = '1';
	                                                } else {
	                                                    entry.classList.add('changelog-collapsed');
	                                                    body.style.maxHeight = '0';
	                                                    body.style.opacity = '0';
	                                                }
	                                            });
	                                        });

	                                        // ── Scroll-in animation via IntersectionObserver ────────
	                                        if ('IntersectionObserver' in window) {
	                                            var observer = new IntersectionObserver(function(ioEntries) {
	                                                ioEntries.forEach(function(ioEntry) {
	                                                    if (ioEntry.isIntersecting) {
	                                                        ioEntry.target.classList.add('changelog-visible');
	                                                        observer.unobserve(ioEntry.target);
	                                                    }
	                                                });
	                                            }, {
	                                                threshold: 0.1,
	                                                rootMargin: '0px 0px -40px 0px'
	                                            });

	                                            entries.forEach(function(entry) {
	                                                observer.observe(entry);
	                                            });
	                                        } else {
	                                            // Fallback: show all immediately
	                                            entries.forEach(function(entry) {
	                                                entry.classList.add('changelog-visible');
	                                            });
	                                        }
	                                    });
	                                })();
	                                </script>
	                                """;

	#endregion

	/// <inheritdoc />
	public string Id => "mokadocs-changelog";

	/// <inheritdoc />
	public string Name => "Release Changelog";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("Changelog plugin initialized — release timeline UI enabled");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		int pagesWithChangelog = 0;

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html))
			{
				continue;
			}

			if (!html.Contains("class=\"changelog\"", StringComparison.Ordinal))
			{
				continue;
			}

			page.Content = page.Content with
			{
				Html = InjectChangelogAssets(html)
			};
			pagesWithChangelog++;
		}

		if (pagesWithChangelog > 0)
		{
			context.LogInfo($"Changelog plugin: Enhanced {pagesWithChangelog} page(s) with release timeline UI");
		}

		return Task.CompletedTask;
	}

	private static string InjectChangelogAssets(string html)
	{
		var sb = new StringBuilder(html.Length + InlineCss.Length + InlineJs.Length + 64);
		sb.Append(InlineCss);
		sb.Append(html);
		sb.Append(InlineJs);
		return sb.ToString();
	}
}
