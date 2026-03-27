using System.Text;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     MokaDocs plugin that adds Blazor component preview functionality to documentation pages.
///     Code blocks written as <c>```blazor-preview</c> are enhanced with a tabbed Source/Preview UI.
///     In <c>mokadocs serve</c> mode, the component markup is rendered server-side via the
///     Blazor preview endpoint. In static builds, a message directs users to use the dev server.
/// </summary>
public sealed class BlazorPreviewPlugin : IMokaPlugin
{
	#region Inline CSS

	private const string InlineCss = """
	                                 <style>
	                                 .blazor-preview-container {
	                                     position: relative;
	                                     border: 1px solid var(--border-color, #e2e8f0);
	                                     border-radius: 8px;
	                                     margin: 1.5em 0;
	                                     overflow: hidden;
	                                     background: var(--card-bg, #ffffff);
	                                 }
	                                 .blazor-preview-tabs {
	                                     display: flex;
	                                     border-bottom: 1px solid var(--border-color, #e2e8f0);
	                                     background: var(--code-toolbar-bg, #181825);
	                                     padding: 0;
	                                     margin: 0;
	                                 }
	                                 .blazor-preview-tab {
	                                     padding: 0.5em 1.2em;
	                                     font-size: 0.8rem;
	                                     font-weight: 600;
	                                     color: #94a3b8;
	                                     background: transparent;
	                                     border: none;
	                                     border-bottom: 2px solid transparent;
	                                     cursor: pointer;
	                                     transition: color 0.15s, border-color 0.15s;
	                                     font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
	                                 }
	                                 .blazor-preview-tab:hover {
	                                     color: #cbd5e1;
	                                 }
	                                 .blazor-preview-tab.active {
	                                     color: #60a5fa;
	                                     border-bottom-color: #60a5fa;
	                                 }
	                                 .blazor-preview-source {
	                                     display: none;
	                                 }
	                                 .blazor-preview-source.active {
	                                     display: block;
	                                 }
	                                 .blazor-preview-source pre {
	                                     margin: 0;
	                                     border: none;
	                                     border-radius: 0;
	                                 }
	                                 .blazor-preview-source code {
	                                     font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                 }
	                                 .blazor-preview-render {
	                                     display: none;
	                                     padding: 1.5em;
	                                     background: #ffffff;
	                                     color: #1e293b;
	                                     min-height: 60px;
	                                 }
	                                 .blazor-preview-render.active {
	                                     display: block;
	                                 }
	                                 .blazor-preview-render-content {
	                                     font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
	                                     line-height: 1.6;
	                                 }
	                                 .blazor-preview-render-content h1,
	                                 .blazor-preview-render-content h2,
	                                 .blazor-preview-render-content h3,
	                                 .blazor-preview-render-content h4 {
	                                     margin-top: 0;
	                                     color: #1e293b;
	                                 }
	                                 .blazor-preview-render-content button {
	                                     padding: 0.4em 1em;
	                                     border: 1px solid #d1d5db;
	                                     border-radius: 6px;
	                                     background: #f9fafb;
	                                     color: #374151;
	                                     font-size: 0.9rem;
	                                     cursor: pointer;
	                                     transition: background 0.15s;
	                                 }
	                                 .blazor-preview-render-content button:hover {
	                                     background: #f3f4f6;
	                                 }
	                                 .blazor-preview-render-content input {
	                                     padding: 0.4em 0.75em;
	                                     border: 1px solid #d1d5db;
	                                     border-radius: 6px;
	                                     font-size: 0.9rem;
	                                 }
	                                 .blazor-preview-loading {
	                                     display: flex;
	                                     align-items: center;
	                                     gap: 0.5em;
	                                     color: #94a3b8;
	                                     font-size: 0.85rem;
	                                     font-family: monospace;
	                                 }
	                                 .blazor-preview-error {
	                                     color: #ef4444;
	                                     font-size: 0.85rem;
	                                     font-family: monospace;
	                                     padding: 0.5em;
	                                     background: #fef2f2;
	                                     border-radius: 4px;
	                                 }
	                                 .blazor-preview-unavailable {
	                                     color: #f59e0b;
	                                     font-size: 0.85rem;
	                                     font-family: monospace;
	                                 }
	                                 .blazor-preview-toolbar {
	                                     display: flex;
	                                     align-items: center;
	                                     gap: 0.5em;
	                                     padding: 0.4em 0.75em;
	                                     background: #f8fafc;
	                                     border-top: 1px solid var(--border-color, #e2e8f0);
	                                 }
	                                 .blazor-preview-refresh-btn {
	                                     display: inline-flex;
	                                     align-items: center;
	                                     gap: 0.35em;
	                                     padding: 0.3em 0.75em;
	                                     font-size: 0.75rem;
	                                     font-weight: 600;
	                                     color: #475569;
	                                     background: #e2e8f0;
	                                     border: none;
	                                     border-radius: 4px;
	                                     cursor: pointer;
	                                     transition: background 0.15s;
	                                 }
	                                 .blazor-preview-refresh-btn:hover {
	                                     background: #cbd5e1;
	                                 }
	                                 .blazor-preview-refresh-btn:disabled {
	                                     opacity: 0.5;
	                                     cursor: not-allowed;
	                                 }
	                                 .blazor-preview-refresh-btn svg {
	                                     width: 12px;
	                                     height: 12px;
	                                     fill: currentColor;
	                                 }
	                                 .blazor-preview-badge {
	                                     display: inline-block;
	                                     font-size: 0.65rem;
	                                     font-weight: 700;
	                                     text-transform: uppercase;
	                                     letter-spacing: 0.05em;
	                                     color: #7c3aed;
	                                     background: #ede9fe;
	                                     padding: 0.15em 0.5em;
	                                     border-radius: 3px;
	                                     margin-left: auto;
	                                 }
	                                 </style>
	                                 """;

	#endregion

	#region Inline JS

	private const string InlineJs = """
	                                <script>
	                                (function() {
	                                    var refreshIcon = '<svg viewBox="0 0 24 24"><path d="M17.65 6.35A7.958 7.958 0 0 0 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08A5.99 5.99 0 0 1 12 18c-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/></svg>';

	                                    document.querySelectorAll('.blazor-preview-container[data-blazor-preview="true"]').forEach(function(container) {
	                                        var sourceDiv = container.querySelector('.blazor-preview-source');
	                                        var renderDiv = container.querySelector('.blazor-preview-render');
	                                        var codeEl = container.querySelector('code');
	                                        if (!sourceDiv || !renderDiv || !codeEl) return;

	                                        // Create tab bar
	                                        var tabBar = document.createElement('div');
	                                        tabBar.className = 'blazor-preview-tabs';

	                                        var previewTab = document.createElement('button');
	                                        previewTab.className = 'blazor-preview-tab active';
	                                        previewTab.textContent = 'Preview';
	                                        previewTab.type = 'button';

	                                        var sourceTab = document.createElement('button');
	                                        sourceTab.className = 'blazor-preview-tab';
	                                        sourceTab.textContent = 'Source';
	                                        sourceTab.type = 'button';

	                                        var badge = document.createElement('span');
	                                        badge.className = 'blazor-preview-badge';
	                                        badge.textContent = 'Blazor';

	                                        tabBar.appendChild(previewTab);
	                                        tabBar.appendChild(sourceTab);
	                                        tabBar.appendChild(badge);

	                                        // Insert tab bar at the top of the container
	                                        container.insertBefore(tabBar, container.firstChild);

	                                        // Create content wrapper inside render div
	                                        var contentWrapper = document.createElement('div');
	                                        contentWrapper.className = 'blazor-preview-render-content';
	                                        renderDiv.appendChild(contentWrapper);

	                                        // Create toolbar with refresh button
	                                        var toolbar = document.createElement('div');
	                                        toolbar.className = 'blazor-preview-toolbar';

	                                        var refreshBtn = document.createElement('button');
	                                        refreshBtn.className = 'blazor-preview-refresh-btn';
	                                        refreshBtn.innerHTML = refreshIcon + ' Refresh';
	                                        refreshBtn.type = 'button';

	                                        toolbar.appendChild(refreshBtn);
	                                        renderDiv.after(toolbar);

	                                        // Show preview tab by default
	                                        renderDiv.classList.add('active');
	                                        sourceDiv.classList.remove('active');

	                                        // Tab switching
	                                        previewTab.addEventListener('click', function() {
	                                            previewTab.classList.add('active');
	                                            sourceTab.classList.remove('active');
	                                            renderDiv.classList.add('active');
	                                            sourceDiv.classList.remove('active');
	                                            toolbar.style.display = '';
	                                        });

	                                        sourceTab.addEventListener('click', function() {
	                                            sourceTab.classList.add('active');
	                                            previewTab.classList.remove('active');
	                                            sourceDiv.classList.add('active');
	                                            renderDiv.classList.remove('active');
	                                            toolbar.style.display = 'none';
	                                        });

	                                        // Fetch preview function
	                                        function fetchPreview() {
	                                            var source = codeEl.textContent;
	                                            refreshBtn.disabled = true;
	                                            contentWrapper.innerHTML = '<div class="blazor-preview-loading"><svg viewBox="0 0 24 24" width="16" height="16"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" stroke-dasharray="31.4 31.4"><animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="0.8s" repeatCount="indefinite"/></circle></svg> Rendering preview\u2026</div>';

	                                            fetch('/api/blazor/preview', {
	                                                method: 'POST',
	                                                headers: { 'Content-Type': 'application/json' },
	                                                body: JSON.stringify({ source: source })
	                                            })
	                                            .then(function(res) { return res.json(); })
	                                            .then(function(data) {
	                                                if (data.error) {
	                                                    contentWrapper.innerHTML = '<div class="blazor-preview-error">' + escapeHtml(data.error) + '</div>';
	                                                } else if (data.html) {
	                                                    contentWrapper.innerHTML = data.html;
	                                                } else {
	                                                    contentWrapper.innerHTML = '<div class="blazor-preview-loading">(empty preview)</div>';
	                                                }
	                                            })
	                                            .catch(function(err) {
	                                                contentWrapper.innerHTML = '<div class="blazor-preview-unavailable">Preview server unavailable. Run "mokadocs serve" to enable Blazor component previews.</div>';
	                                            })
	                                            .finally(function() {
	                                                refreshBtn.disabled = false;
	                                            });
	                                        }

	                                        // Refresh button
	                                        refreshBtn.addEventListener('click', fetchPreview);

	                                        // Auto-fetch preview on load
	                                        fetchPreview();
	                                    });

	                                    function escapeHtml(str) {
	                                        var div = document.createElement('div');
	                                        div.appendChild(document.createTextNode(str));
	                                        return div.innerHTML;
	                                    }
	                                })();
	                                </script>
	                                """;

	#endregion

	/// <inheritdoc />
	public string Id => "mokadocs-blazor-preview";

	/// <inheritdoc />
	public string Name => "Blazor Component Preview";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("Blazor preview plugin initialized — component preview blocks enabled");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		int pagesWithPreview = 0;

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html))
			{
				continue;
			}

			if (!html.Contains("data-blazor-preview=\"true\"", StringComparison.Ordinal))
			{
				continue;
			}

			// Inject CSS and JS into pages that have Blazor preview blocks
			page.Content = page.Content with
			{
				Html = InjectBlazorPreviewAssets(html)
			};
			pagesWithPreview++;
		}

		if (pagesWithPreview > 0)
		{
			context.LogInfo($"Blazor preview plugin: Enhanced {pagesWithPreview} page(s) with component previews");
		}

		return Task.CompletedTask;
	}

	/// <summary>
	///     Wraps the page HTML with inline Blazor preview CSS at the top and JS at the bottom.
	/// </summary>
	private static string InjectBlazorPreviewAssets(string html)
	{
		var sb = new StringBuilder(html.Length + InlineCss.Length + InlineJs.Length + 64);
		sb.Append(InlineCss);
		sb.Append(html);
		sb.Append(InlineJs);
		return sb.ToString();
	}
}
