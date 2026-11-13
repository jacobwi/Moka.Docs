using System.Security.Cryptography;
using System.Text;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     The stylesheet and script docs pages with previews load: the Preview/Source tabs, the
///     error and loading states, and the iframe resizing. They are written once per site and
///     linked with a <c>?v=</c> content hash, instead of going inline into every page.
/// </summary>
internal static class PreviewPageAssets
{
	/// <summary>Output path of the stylesheet.</summary>
	internal const string StylesheetPath = "_preview-assets/blazor-preview.css";

	/// <summary>Output path of the script.</summary>
	internal const string ScriptPath = "_preview-assets/blazor-preview.js";

	// All colors inherit from mokadocs theme tokens (--color-*) so the preview chrome
	// matches the surrounding docs theme in both light and dark mode, and across all
	// color themes (ocean, emerald, violet, amber, rose, moka-red, ...). Fallbacks are
	// only used when the page lacks a mokadocs theme (e.g. standalone render).
	//
	// The iframe min-height is 160px (matches preview-host body min-height) so small
	// previews aren't jammed into a cramped strip while the runtime boots and the
	// ResizeObserver hasn't fired yet. The height transition smooths the jump when
	// the postMessage resize event resizes the iframe to fit its actual content.
	private const string _css = """
	                            .blazor-preview-container {
	                                position: relative;
	                                border: 1px solid var(--color-border, #e2e8f0);
	                                border-radius: 8px;
	                                margin: 1.5em 0;
	                                overflow: hidden;
	                                background: var(--color-bg, #ffffff);
	                            }
	                            .blazor-preview-tabs {
	                                display: flex;
	                                align-items: center;
	                                border-bottom: 1px solid var(--color-border, #e2e8f0);
	                                background: var(--color-bg-secondary, var(--color-bg, #f7f7f9));
	                                padding: 0;
	                                margin: 0;
	                            }
	                            .blazor-preview-tab {
	                                padding: 0.55em 1.1em;
	                                font-size: 0.78rem;
	                                font-weight: 600;
	                                color: var(--color-text-muted, var(--color-text-secondary, #64748b));
	                                background: transparent;
	                                border: none;
	                                border-bottom: 2px solid transparent;
	                                cursor: pointer;
	                                transition: color 0.15s ease-out, border-color 0.15s ease-out, background 0.15s ease-out;
	                                font-family: var(--font-body, inherit);
	                                letter-spacing: 0.01em;
	                            }
	                            .blazor-preview-tab:hover {
	                                color: var(--color-text, #1a1a1a);
	                                background: var(--color-bg, #ffffff);
	                            }
	                            .blazor-preview-tab.active {
	                                color: var(--color-primary, #0ea5e9);
	                                border-bottom-color: var(--color-primary, #0ea5e9);
	                                background: var(--color-bg, #ffffff);
	                            }
	                            .blazor-preview-source { display: none; }
	                            .blazor-preview-source.active { display: block; }
	                            .blazor-preview-source pre { margin: 0; border: none; border-radius: 0; }
	                            .blazor-preview-source code {
	                                font-family: var(--font-mono, 'JetBrains Mono', 'Cascadia Code', 'Fira Code', ui-monospace, monospace);
	                            }
	                            .blazor-preview-render {
	                                display: none;
	                                padding: 0;
	                                background: var(--color-bg, #ffffff);
	                                min-height: 160px;
	                                background-image: linear-gradient(var(--color-border-light, var(--color-border, #eef0f2)) 1px, transparent 1px),
	                                                  linear-gradient(90deg, var(--color-border-light, var(--color-border, #eef0f2)) 1px, transparent 1px);
	                                background-size: 24px 24px;
	                                background-position: -1px -1px;
	                            }
	                            .blazor-preview-render.active { display: block; }
	                            .blazor-preview-error {
	                                color: var(--color-primary, #ef4444);
	                                font-size: 0.85rem;
	                                font-family: var(--font-mono, 'JetBrains Mono', 'Cascadia Code', ui-monospace, monospace);
	                                padding: 1em;
	                                background: var(--color-bg-secondary, #fef2f2);
	                                border: 1px solid var(--color-border, #fecaca);
	                                white-space: pre-wrap;
	                                border-radius: 4px;
	                            }
	                            .blazor-preview-loading {
	                                min-height: 24px;
	                                opacity: 0.5;
	                                background: repeating-linear-gradient(
	                                    -45deg,
	                                    var(--color-border, #e2e8f0),
	                                    var(--color-border, #e2e8f0) 10px,
	                                    transparent 10px,
	                                    transparent 20px
	                                );
	                                border-radius: 4px;
	                            }
	                            .blazor-preview-badge {
	                                display: inline-flex;
	                                align-items: center;
	                                font-size: 0.62rem;
	                                font-weight: 700;
	                                text-transform: uppercase;
	                                letter-spacing: 0.06em;
	                                color: var(--color-primary, #0ea5e9);
	                                background: var(--color-bg, #ffffff);
	                                border: 1px solid var(--color-border, #e2e8f0);
	                                padding: 0.2em 0.55em;
	                                border-radius: 999px;
	                                margin-left: auto;
	                                margin-right: 0.75em;
	                            }
	                            .blazor-preview-iframe {
	                                width: 100%;
	                                min-height: 160px;
	                                border: none;
	                                display: block;
	                                background: transparent;
	                                transition: height 200ms ease-out;
	                            }
	                            .blazor-preview-ssr-fallback { padding: 1.25rem 1.5rem; }

	                            """;

	// The preview host posts { type: 'resize', height } up to the parent frame when its
	// content height changes, so each iframe grows to fit its component.
	private const string _js = """
	                           (function() {
	                               document.querySelectorAll('.blazor-preview-container[data-blazor-preview="true"]').forEach(function(container) {
	                                   var sourceDiv = container.querySelector('.blazor-preview-source');
	                                   var renderDiv = container.querySelector('.blazor-preview-render');
	                                   if (!sourceDiv || !renderDiv) return;

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
	                                   container.insertBefore(tabBar, container.firstChild);

	                                   renderDiv.classList.add('active');

	                                   previewTab.addEventListener('click', function() {
	                                       previewTab.classList.add('active');
	                                       sourceTab.classList.remove('active');
	                                       renderDiv.classList.add('active');
	                                       sourceDiv.classList.remove('active');
	                                   });
	                                   sourceTab.addEventListener('click', function() {
	                                       sourceTab.classList.add('active');
	                                       previewTab.classList.remove('active');
	                                       sourceDiv.classList.add('active');
	                                       renderDiv.classList.remove('active');
	                                   });
	                               });

	                               window.addEventListener('message', function(e) {
	                                   if (e.origin !== location.origin || !e.data || e.data.type !== 'resize') return;
	                                   var height = Number(e.data.height);
	                                   if (!isFinite(height) || height < 0) return;
	                                   document.querySelectorAll('.blazor-preview-iframe').forEach(function(iframe) {
	                                       if (iframe.contentWindow === e.source) {
	                                           iframe.style.height = (height + 16) + 'px';
	                                       }
	                                   });
	                               });
	                           })();

	                           """;

	/// <summary>The stylesheet's bytes.</summary>
	internal static byte[] Stylesheet { get; } = new UTF8Encoding(false).GetBytes(_css.ReplaceLineEndings("\n"));

	/// <summary>The script's bytes.</summary>
	internal static byte[] Script { get; } = new UTF8Encoding(false).GetBytes(_js.ReplaceLineEndings("\n"));

	/// <summary>Root-relative stylesheet URL with its <c>?v=</c> hash; the render phase adds the base path.</summary>
	internal static string StylesheetUrl { get; } = $"/{StylesheetPath}?v={ContentHash(Stylesheet)}";

	/// <summary>Root-relative script URL with its <c>?v=</c> hash; the render phase adds the base path.</summary>
	internal static string ScriptUrl { get; } = $"/{ScriptPath}?v={ContentHash(Script)}";

	/// <summary>Links the stylesheet before a page's content and the script after it.</summary>
	internal static string Link(string html) =>
		$"<link rel=\"stylesheet\" href=\"{StylesheetUrl}\">{html}<script src=\"{ScriptUrl}\" defer></script>";

	/// <summary>The same 10-character hash the theme's <c>?v=</c> URLs use.</summary>
	internal static string ContentHash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes))[..10];
}
