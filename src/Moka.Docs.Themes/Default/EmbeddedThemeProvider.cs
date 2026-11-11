using Moka.Docs.Rendering.Scriban;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Provides the built-in "Moka" default theme with all templates, CSS, and JS.
///     This enables zero-config builds without requiring a theme directory on disk.
/// </summary>
public static partial class EmbeddedThemeProvider
{
	#region Embedded CSS

	private const string _embeddedCss = """
	                                    /* MokaDocs Default Theme - "Moka" */
	                                    /* ================================ */

	                                    :root {
	                                        --color-primary: #0ea5e9;
	                                        --color-primary-light: #38bdf8;
	                                        --color-primary-dark: #0284c7;
	                                        --gradient-secondary: #06b6d4;
	                                        --gradient-tertiary: #3b82f6;
	                                        --color-accent: #f59e0b;
	                                        --color-bg: #ffffff;
	                                        --color-bg-secondary: #f8fafc;
	                                        --color-bg-code: #f1f5f9;
	                                        --color-text: #1e293b;
	                                        --color-text-secondary: #64748b;
	                                        --color-text-muted: #667488;
	                                        --color-border: #e2e8f0;
	                                        --color-border-light: #f1f5f9;
	                                        --sidebar-width: 280px;
	                                        --toc-width: 220px;
	                                        --header-height: 60px;
	                                        --content-max-width: 780px;
	                                        --font-body: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
	                                        --font-mono: 'JetBrains Mono', 'Fira Code', 'Cascadia Code', monospace;
	                                        --radius: 8px;
	                                        --radius-sm: 4px;
	                                        --shadow-sm: 0 1px 2px rgba(0,0,0,0.05);
	                                        --shadow-md: 0 4px 6px -1px rgba(0,0,0,0.1);
	                                        --shadow-lg: 0 10px 25px -3px rgba(0,0,0,0.15);
	                                        --transition: 150ms ease;
	                                    }

	                                    [data-theme="dark"] {
	                                        --color-bg: #0f172a;
	                                        --color-bg-secondary: #1e293b;
	                                        --color-bg-code: #1e293b;
	                                        --color-text: #e2e8f0;
	                                        --color-text-secondary: #94a3b8;
	                                        --color-text-muted: #8091a9;
	                                        --color-border: #334155;
	                                        --color-border-light: #1e293b;
	                                    }

	                                    /* Color theme presets - override primary accent colors
	                                       Uses html[] for higher specificity than inline :root overrides */
	                                    html[data-color-theme="ocean"] {
	                                        --color-primary: #0ea5e9 !important;
	                                        --color-primary-light: #38bdf8 !important;
	                                        --color-primary-dark: #0284c7 !important;
	                                        --gradient-secondary: #06b6d4;
	                                        --gradient-tertiary: #3b82f6;
	                                    }
	                                    html[data-color-theme="emerald"] {
	                                        --color-primary: #10b981 !important;
	                                        --color-primary-light: #34d399 !important;
	                                        --color-primary-dark: #059669 !important;
	                                        --gradient-secondary: #06d6a0;
	                                        --gradient-tertiary: #2dd4bf;
	                                    }
	                                    html[data-color-theme="violet"] {
	                                        --color-primary: #8b5cf6 !important;
	                                        --color-primary-light: #a78bfa !important;
	                                        --color-primary-dark: #7c3aed !important;
	                                        --gradient-secondary: #a855f7;
	                                        --gradient-tertiary: #6366f1;
	                                    }
	                                    html[data-color-theme="amber"] {
	                                        --color-primary: #f59e0b !important;
	                                        --color-primary-light: #fbbf24 !important;
	                                        --color-primary-dark: #d97706 !important;
	                                        --gradient-secondary: #f97316;
	                                        --gradient-tertiary: #ef4444;
	                                    }
	                                    html[data-color-theme="rose"] {
	                                        --color-primary: #f43f5e !important;
	                                        --color-primary-light: #fb7185 !important;
	                                        --color-primary-dark: #e11d48 !important;
	                                        --gradient-secondary: #ec4899;
	                                        --gradient-tertiary: #f97316;
	                                    }
	                                    html[data-color-theme="moka-red"] {
	                                        --color-primary: #d32f2f !important;
	                                        --color-primary-light: #ff6659 !important;
	                                        --color-primary-dark: #9a0007 !important;
	                                        --gradient-secondary: #c62828;
	                                        --gradient-tertiary: #e53935;
	                                    }

	                                    /* Text in the accent color (links, inline code, active items). Each preset's
	                                       primary only darkened (light) or lightened (dark) as far as 4.5:1 on the
	                                       code background and the current sidebar link's tint needs, keeping its hue.
	                                       A custom primaryColor has no preset and is used as it is. */
	                                    :root {
	                                        --color-primary-text: var(--color-primary);
	                                        --color-primary-text-hover: color-mix(in srgb, var(--color-primary-text) 80%, black);
	                                    }
	                                    [data-theme="dark"] { --color-primary-text-hover: color-mix(in srgb, var(--color-primary-text) 75%, white); }
	                                    html[data-color-theme="ocean"] { --color-primary-text: #0377aa; }
	                                    html[data-color-theme="emerald"] { --color-primary-text: #038058; }
	                                    html[data-color-theme="violet"] { --color-primary-text: #804fe9; }
	                                    html[data-color-theme="amber"] { --color-primary-text: #9d6302; }
	                                    html[data-color-theme="rose"] { --color-primary-text: #d81c48; }
	                                    html[data-color-theme="moka-red"] { --color-primary-text: #d12c2d; }
	                                    html[data-theme="dark"]:is([data-color-theme="ocean"], [data-color-theme="emerald"], [data-color-theme="amber"]) { --color-primary-text: var(--color-primary); }
	                                    html[data-theme="dark"][data-color-theme="violet"] { --color-primary-text: #9b77ff; }
	                                    html[data-theme="dark"][data-color-theme="rose"] { --color-primary-text: #ff4d68; }
	                                    html[data-theme="dark"][data-color-theme="moka-red"] { --color-primary-text: #f9564f; }

	                                    /* Reset */
	                                    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

	                                    html { scroll-behavior: smooth; scroll-padding-top: calc(var(--header-height) + 1rem); }

	                                    body {
	                                        font-family: var(--font-body);
	                                        font-size: 16px;
	                                        line-height: 1.7;
	                                        color: var(--color-text);
	                                        background: var(--color-bg);
	                                        -webkit-font-smoothing: antialiased;
	                                    }

	                                    a { color: var(--color-primary-text); text-decoration: none; transition: color var(--transition); }
	                                    a:hover { color: var(--color-primary-text-hover); }

	                                    /* Header */
	                                    .site-header {
	                                        position: sticky;
	                                        top: 0;
	                                        z-index: 100;
	                                        height: var(--header-height);
	                                        background: var(--color-bg);
	                                        border-bottom: 1px solid var(--color-border);
	                                        backdrop-filter: blur(8px);
	                                    }

	                                    .header-inner {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: space-between;
	                                        max-width: 1440px;
	                                        margin: 0 auto;
	                                        padding: 0 1.5rem;
	                                        height: 100%;
	                                    }

	                                    .site-logo {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.625rem;
	                                        font-weight: 700;
	                                        font-size: 1.125rem;
	                                        color: var(--color-text);
	                                        text-decoration: none;
	                                    }
	                                    .site-logo:hover { color: var(--color-primary); }
	                                    .site-logo-icon {
	                                        color: var(--color-primary);
	                                        flex-shrink: 0;
	                                    }
	                                    .site-logo-img {
	                                        height: 28px;
	                                        width: auto;
	                                        flex-shrink: 0;
	                                    }
	                                    .site-name {
	                                        font-size: 1.0625rem;
	                                        font-weight: 700;
	                                        letter-spacing: -0.01em;
	                                    }

	                                    .header-actions { display: flex; align-items: center; gap: 0.5rem; }

	                                    .search-trigger {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.5rem;
	                                        padding: 0.375rem 0.75rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text-muted);
	                                        cursor: pointer;
	                                        font-size: 0.875rem;
	                                        transition: border-color var(--transition);
	                                    }
	                                    .search-trigger:hover { border-color: var(--color-primary); }
	                                    .search-trigger kbd {
	                                        font-family: var(--font-mono);
	                                        font-size: 0.75rem;
	                                        padding: 0.125rem 0.375rem;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-sm);
	                                    }

	                                    .theme-toggle, .mobile-nav-toggle {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 36px;
	                                        height: 36px;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                    }
	                                    .theme-toggle:hover, .mobile-nav-toggle:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }

	                                    [data-theme="light"] .icon-moon { display: none; }
	                                    [data-theme="dark"] .icon-sun { display: none; }

	                                    /* Color theme selector */
	                                    .color-theme-selector { position: relative; }
	                                    .color-theme-trigger {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 36px;
	                                        height: 36px;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                    }
	                                    .color-theme-trigger:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .color-theme-trigger[aria-expanded="true"] {
	                                        border-color: var(--color-primary);
	                                        color: var(--color-primary);
	                                    }
	                                    .color-theme-dropdown {
	                                        position: absolute;
	                                        top: calc(100% + 8px);
	                                        right: 0;
	                                        display: flex;
	                                        gap: 8px;
	                                        padding: 10px 12px;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        box-shadow: var(--shadow-lg);
	                                        z-index: 200;
	                                    }
	                                    .color-theme-dropdown[hidden] { display: none; }
	                                    .color-swatch {
	                                        width: 22px;
	                                        height: 22px;
	                                        border-radius: 50%;
	                                        border: 2px solid transparent;
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                        padding: 0;
	                                        outline-offset: 2px;
	                                    }
	                                    .color-swatch:hover {
	                                        transform: scale(1.15);
	                                    }
	                                    .color-swatch.active {
	                                        border-color: var(--color-text);
	                                        box-shadow: 0 0 0 2px var(--color-bg), 0 0 0 4px currentColor;
	                                        box-shadow: 0 0 0 3px var(--color-bg), 0 0 0 5px var(--color-text-secondary);
	                                    }

	                                    /* Code Theme Selector */
	                                    .code-theme-selector { position: relative; }
	                                    .code-theme-trigger {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 36px;
	                                        height: 36px;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                    }
	                                    .code-theme-trigger:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .code-theme-trigger[aria-expanded="true"] {
	                                        border-color: var(--color-primary);
	                                        color: var(--color-primary);
	                                    }
	                                    .code-theme-dropdown {
	                                        position: absolute;
	                                        top: calc(100% + 8px);
	                                        right: 0;
	                                        display: flex;
	                                        flex-direction: column;
	                                        gap: 2px;
	                                        padding: 6px;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        box-shadow: var(--shadow-lg);
	                                        z-index: 200;
	                                        min-width: 200px;
	                                    }
	                                    .code-theme-dropdown[hidden] { display: none; }
	                                    .code-theme-option {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 10px;
	                                        padding: 7px 10px;
	                                        border: none;
	                                        border-radius: calc(var(--radius) - 2px);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                        font-size: 0.82rem;
	                                        width: 100%;
	                                        text-align: left;
	                                    }
	                                    .code-theme-option:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .code-theme-option.active {
	                                        color: var(--color-primary-text);
	                                        font-weight: 600;
	                                    }
	                                    .code-theme-preview {
	                                        width: 16px;
	                                        height: 16px;
	                                        border-radius: 3px;
	                                        flex-shrink: 0;
	                                    }
	                                    .code-theme-name { flex: 1; }
	                                    .code-theme-check {
	                                        opacity: 0;
	                                        font-size: 0.75rem;
	                                        color: var(--color-primary);
	                                        transition: opacity var(--transition);
	                                    }
	                                    .code-theme-option.active .code-theme-check { opacity: 1; }

	                                    /* Code Style Selector */
	                                    .code-style-selector { position: relative; }
	                                    .code-style-trigger {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 36px;
	                                        height: 36px;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                    }
	                                    .code-style-trigger:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .code-style-trigger[aria-expanded="true"] {
	                                        border-color: var(--color-primary);
	                                        color: var(--color-primary);
	                                    }
	                                    .code-style-dropdown {
	                                        position: absolute;
	                                        top: calc(100% + 8px);
	                                        right: 0;
	                                        display: flex;
	                                        flex-direction: column;
	                                        gap: 2px;
	                                        padding: 6px;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        box-shadow: var(--shadow-lg);
	                                        z-index: 200;
	                                        min-width: 160px;
	                                    }
	                                    .code-style-dropdown[hidden] { display: none; }
	                                    .code-style-option {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 10px;
	                                        padding: 7px 10px;
	                                        border: none;
	                                        border-radius: calc(var(--radius) - 2px);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                        font-size: 0.82rem;
	                                        width: 100%;
	                                        text-align: left;
	                                    }
	                                    .code-style-option:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .code-style-option.active {
	                                        color: var(--color-primary-text);
	                                        font-weight: 600;
	                                    }
	                                    .code-style-indicator {
	                                        width: 16px;
	                                        height: 16px;
	                                        border-radius: 50%;
	                                        border: 2px solid var(--color-border);
	                                        flex-shrink: 0;
	                                        transition: all var(--transition);
	                                    }
	                                    .code-style-option.active .code-style-indicator {
	                                        border-color: var(--color-primary);
	                                        background: var(--color-primary);
	                                        box-shadow: inset 0 0 0 3px var(--color-bg);
	                                    }
	                                    .code-style-name { flex: 1; }
	                                    .code-style-check {
	                                        opacity: 0;
	                                        font-size: 0.75rem;
	                                        color: var(--color-primary);
	                                        transition: opacity var(--transition);
	                                    }
	                                    .code-style-option.active .code-style-check { opacity: 1; }

	                                    /* Appearance group - wraps color/code/style selectors */
	                                    .appearance-group { display: flex; align-items: center; gap: 0.5rem; }

	                                    .mobile-nav-toggle { display: none; }

	                                    /* Layout */
	                                    .site-container {
	                                        display: flex;
	                                        max-width: 1440px;
	                                        margin: 0 auto;
	                                        min-height: calc(100vh - var(--header-height) - 60px);
	                                    }

	                                    /* Sidebar */
	                                    .sidebar {
	                                        position: sticky;
	                                        top: var(--header-height);
	                                        width: var(--sidebar-width);
	                                        height: calc(100vh - var(--header-height));
	                                        overflow-y: auto;
	                                        padding: 1.5rem 1rem;
	                                        border-right: 1px solid var(--color-border);
	                                        flex-shrink: 0;
	                                    }

	                                    .nav-section { margin-bottom: 0.25rem; }

	                                    .nav-link {
	                                        display: flex;
	                                        align-items: center;
	                                        padding: 0.375rem 0.75rem;
	                                        border-radius: var(--radius-sm);
	                                        color: var(--color-text-secondary);
	                                        font-size: 0.875rem;
	                                        font-weight: 500;
	                                        transition: all var(--transition);
	                                        text-decoration: none;
	                                    }
	                                    .nav-link:hover { color: var(--color-text); background: var(--color-bg-secondary); }
	                                    .nav-link.current {
	                                        color: var(--color-primary-text);
	                                        background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg));
	                                        font-weight: 600;
	                                        border-left: 2px solid var(--color-primary);
	                                        padding-left: calc(0.75rem - 2px);
	                                    }

	                                    .nav-header {
	                                        display: flex;
	                                        align-items: center;
	                                    }
	                                    .nav-header .nav-link {
	                                        flex: 1;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                    }
	                                    .nav-header .nav-link:hover { color: var(--color-primary-text); }
	                                    .nav-header .nav-link.parent-active {
	                                        color: var(--color-text);
	                                        font-weight: 700;
	                                    }
	                                    .nav-chevron-btn {
	                                        background: none;
	                                        border: none;
	                                        cursor: pointer;
	                                        padding: 0.375rem 0.5rem;
	                                        color: var(--color-text-muted);
	                                        border-radius: 4px;
	                                        display: flex;
	                                        align-items: center;
	                                    }
	                                    .nav-chevron-btn:hover { color: var(--color-text); background: var(--color-bg-secondary); }

	                                    .nav-group-label {
	                                        cursor: default;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                        flex: 1;
	                                    }
	                                    .nav-group-label.parent-active { color: var(--color-primary-text); }

	                                    .nav-sub-section { margin-bottom: 0.125rem; }
	                                    .nav-sub-section .nav-header { padding-left: 0; }
	                                    .nav-sub-section .nav-children { padding-left: 0.5rem; }

	                                    .nav-icon {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        margin-right: 0.375rem;
	                                        flex-shrink: 0;
	                                    }

	                                    .nav-chevron {
	                                        flex-shrink: 0;
	                                        transition: transform var(--transition);
	                                    }
	                                    .nav-toggle[aria-expanded="true"] .nav-chevron {
	                                        transform: rotate(180deg);
	                                    }

	                                    .nav-children {
	                                        list-style: none;
	                                        padding-left: 1rem;
	                                    }

	                                    /* Content */
	                                    .content {
	                                        flex: 1;
	                                        min-width: 0;
	                                        padding: 2rem 2.5rem;
	                                        max-width: calc(var(--content-max-width) + 5rem);
	                                    }

	                                    .page-content h1 { font-size: 2rem; font-weight: 800; margin-bottom: 1rem; letter-spacing: -0.025em; }
	                                    .page-content h2 { font-size: 1.5rem; font-weight: 700; margin: 2.5rem 0 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid var(--color-border); }
	                                    .page-content h3 { font-size: 1.25rem; font-weight: 600; margin: 2rem 0 0.75rem; }
	                                    .page-content h4 { font-size: 1.125rem; font-weight: 600; margin: 1.5rem 0 0.5rem; }

	                                    .page-content p { margin-bottom: 1rem; }
	                                    .page-content ul, .page-content ol { margin-bottom: 1rem; padding-left: 1.5rem; }
	                                    .page-content li { margin-bottom: 0.25rem; }
	                                    .page-content img { max-width: 100%; height: auto; border-radius: var(--radius); }

	                                    .page-content code {
	                                        font-family: var(--font-mono);
	                                        font-size: 0.875em;
	                                        padding: 0.125rem 0.375rem;
	                                        background: var(--color-bg-code);
	                                        border-radius: var(--radius-sm);
	                                        color: var(--color-primary-text);
	                                    }

	                                    .page-content pre {
	                                        position: relative;
	                                        margin: 1.5rem 0;
	                                        padding: 1rem 1.25rem;
	                                        background: var(--color-bg-code);
	                                        border-radius: var(--radius);
	                                        border: 1px solid var(--color-border);
	                                        overflow-x: auto;
	                                    }
	                                    .page-content pre code {
	                                        padding: 0;
	                                        background: none;
	                                        color: var(--color-text);
	                                        font-size: 0.875rem;
	                                        line-height: 1.6;
	                                    }

	                                    /* Copy button on code blocks - uses inherit to match code block theme */
	                                    .page-content pre .copy-btn {
	                                        position: absolute;
	                                        top: 0.5rem;
	                                        right: 0.5rem;
	                                        padding: 0.25rem 0.5rem;
	                                        font-size: 0.75rem;
	                                        font-family: var(--font-mono);
	                                        background: rgba(255,255,255,0.15);
	                                        border: 1px solid rgba(255,255,255,0.25);
	                                        border-radius: var(--radius-sm);
	                                        color: rgba(255,255,255,0.75);
	                                        cursor: pointer;
	                                        opacity: 0;
	                                        transition: all var(--transition);
	                                    }
	                                    .page-content pre .copy-btn:hover {
	                                        background: rgba(255,255,255,0.25);
	                                        color: #fff;
	                                        border-color: rgba(255,255,255,0.4);
	                                    }
	                                    /* The language label and the Copy button share the top-right corner, so only one shows
	                                       at a time: the label hides at once when the button appears, and comes back only after
	                                       the button has faded out. */
	                                    .page-content pre:hover .copy-btn,
	                                    .page-content pre .copy-btn:focus-visible { opacity: 1; }
	                                    .page-content pre:hover .code-lang,
	                                    .page-content pre:focus-within .code-lang { opacity: 0; transition: none; }
	                                    /* Touch screens have no hover, so the button used to stay invisible. */
	                                    @media (hover: none) {
	                                        .page-content pre .copy-btn { opacity: 1; }
	                                        html:not([data-code-style="macos"]):not([data-code-style="terminal"]):not([data-code-style="vscode"]) .page-content pre .code-lang { opacity: 0; }
	                                    }

	                                    /* Light code themes need dark button colors */
	                                    [data-code-theme="catppuccin-latte"] .page-content pre .copy-btn,
	                                    [data-code-theme="github-light"] .page-content pre .copy-btn {
	                                        background: rgba(0,0,0,0.06);
	                                        border-color: rgba(0,0,0,0.12);
	                                        color: rgba(0,0,0,0.45);
	                                    }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre .copy-btn:hover,
	                                    [data-code-theme="github-light"] .page-content pre .copy-btn:hover {
	                                        background: rgba(0,0,0,0.12);
	                                        color: rgba(0,0,0,0.7);
	                                    }

	                                    /* Line numbers on code blocks */
	                                    .page-content pre.has-line-numbers { padding-left: 3.5rem; }
	                                    .page-content pre .line-numbers {
	                                        position: absolute; left: 0; top: 1rem; bottom: 0; width: 2.5rem;
	                                        padding-right: 0.5rem; text-align: right; font-family: var(--font-mono);
	                                        font-size: 0.875rem; line-height: 1.6; user-select: none;
	                                        color: var(--sh-line-number); border-right: 1px solid var(--color-border);
	                                    }
	                                    .page-content pre .line-numbers span { display: block; }

	                                    /* Language label on code blocks */
	                                    .page-content pre .code-lang {
	                                        position: absolute;
	                                        top: 0.375rem;
	                                        left: 0.75rem;
	                                        font-size: 0.625rem;
	                                        font-family: var(--font-mono);
	                                        text-transform: uppercase;
	                                        letter-spacing: 0.05em;
	                                        color: rgba(255,255,255,0.35);
	                                        user-select: none;
	                                        transition: opacity var(--transition);
	                                    }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre .code-lang,
	                                    [data-code-theme="github-light"] .page-content pre .code-lang {
	                                        color: rgba(0,0,0,0.3);
	                                    }

	                                    /* Syntax highlighting tokens */
	                                    .token.comment, .token.prolog { color: var(--sh-comment); font-style: italic; }
	                                    .token.string, .token.char { color: var(--sh-string); }
	                                    .token.keyword { color: var(--sh-keyword); font-weight: 600; }
	                                    .token.number, .token.boolean { color: var(--sh-number); }
	                                    .token.type-name, .token.class-name { color: var(--sh-type); }
	                                    .token.function { color: var(--sh-function); }
	                                    .token.operator { color: var(--sh-operator); }
	                                    .token.punctuation { color: var(--sh-punctuation); }
	                                    .token.attr-name { color: var(--sh-attr); }
	                                    .token.attr-value, .token.tag-name { color: var(--sh-tag); }
	                                    .token.property { color: var(--sh-property); }
	                                    .token.namespace { color: var(--sh-type); opacity: 0.8; }
	                                    .token.directive { color: var(--sh-keyword); }

	                                    /* Code Theme: Catppuccin Mocha (default) */
	                                    :root, [data-code-theme="catppuccin-mocha"] {
	                                        --sh-comment: #6c7086; --sh-string: #a6e3a1; --sh-keyword: #cba6f7;
	                                        --sh-number: #fab387; --sh-type: #89dceb; --sh-function: #89b4fa;
	                                        --sh-operator: #94e2d5; --sh-punctuation: #bac2de; --sh-attr: #f9e2af;
	                                        --sh-tag: #f38ba8; --sh-property: #b4befe; --sh-line-number: #9399b2;
	                                    }
	                                    [data-code-theme="catppuccin-mocha"] .page-content pre { background: #1e1e2e; border-color: #313244; }
	                                    [data-code-theme="catppuccin-mocha"] .page-content pre code { color: #cdd6f4; }

	                                    /* Code Theme: Catppuccin Latte */
	                                    [data-code-theme="catppuccin-latte"] {
	                                        --sh-comment: #9ca0b0; --sh-string: #40a02b; --sh-keyword: #8839ef;
	                                        --sh-number: #fe640b; --sh-type: #04a5e5; --sh-function: #1e66f5;
	                                        --sh-operator: #179299; --sh-punctuation: #5c5f77; --sh-attr: #df8e1d;
	                                        --sh-tag: #d20f39; --sh-property: #7287fd; --sh-line-number: #5c5f77;
	                                    }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre { background: #eff1f5; border-color: #ccd0da; }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre code { color: #4c4f69; }

	                                    /* Code Theme: GitHub Dark */
	                                    [data-code-theme="github-dark"] {
	                                        --sh-comment: #8b949e; --sh-string: #a5d6ff; --sh-keyword: #ff7b72;
	                                        --sh-number: #79c0ff; --sh-type: #ffa657; --sh-function: #d2a8ff;
	                                        --sh-operator: #ff7b72; --sh-punctuation: #c9d1d9; --sh-attr: #79c0ff;
	                                        --sh-tag: #7ee787; --sh-property: #79c0ff; --sh-line-number: #8b949e;
	                                    }
	                                    [data-code-theme="github-dark"] .page-content pre { background: #0d1117; border-color: #30363d; }
	                                    [data-code-theme="github-dark"] .page-content pre code { color: #c9d1d9; }

	                                    /* Code Theme: Dracula */
	                                    [data-code-theme="dracula"] {
	                                        --sh-comment: #6272a4; --sh-string: #f1fa8c; --sh-keyword: #ff79c6;
	                                        --sh-number: #bd93f9; --sh-type: #8be9fd; --sh-function: #50fa7b;
	                                        --sh-operator: #ff79c6; --sh-punctuation: #f8f8f2; --sh-attr: #50fa7b;
	                                        --sh-tag: #ff79c6; --sh-property: #66d9ef; --sh-line-number: #9aa1c4;
	                                    }
	                                    [data-code-theme="dracula"] .page-content pre { background: #282a36; border-color: #44475a; }
	                                    [data-code-theme="dracula"] .page-content pre code { color: #f8f8f2; }

	                                    /* Code Theme: One Dark */
	                                    [data-code-theme="one-dark"] {
	                                        --sh-comment: #5c6370; --sh-string: #98c379; --sh-keyword: #c678dd;
	                                        --sh-number: #d19a66; --sh-type: #e5c07b; --sh-function: #61afef;
	                                        --sh-operator: #56b6c2; --sh-punctuation: #abb2bf; --sh-attr: #d19a66;
	                                        --sh-tag: #e06c75; --sh-property: #e06c75; --sh-line-number: #959ba5;
	                                    }
	                                    [data-code-theme="one-dark"] .page-content pre { background: #282c34; border-color: #3e4451; }
	                                    [data-code-theme="one-dark"] .page-content pre code { color: #abb2bf; }

	                                    /* Code Theme: Nord */
	                                    [data-code-theme="nord"] {
	                                        --sh-comment: #616e88; --sh-string: #a3be8c; --sh-keyword: #81a1c1;
	                                        --sh-number: #b48ead; --sh-type: #8fbcbb; --sh-function: #88c0d0;
	                                        --sh-operator: #81a1c1; --sh-punctuation: #d8dee9; --sh-attr: #8fbcbb;
	                                        --sh-tag: #bf616a; --sh-property: #88c0d0; --sh-line-number: #a0a9ba;
	                                    }
	                                    [data-code-theme="nord"] .page-content pre { background: #2e3440; border-color: #3b4252; }
	                                    [data-code-theme="nord"] .page-content pre code { color: #d8dee9; }

	                                    /* Code Theme: GitHub Light */
	                                    [data-code-theme="github-light"] {
	                                        --sh-comment: #6e7781; --sh-string: #0a3069; --sh-keyword: #cf222e;
	                                        --sh-number: #0550ae; --sh-type: #953800; --sh-function: #8250df;
	                                        --sh-operator: #cf222e; --sh-punctuation: #24292f; --sh-attr: #0550ae;
	                                        --sh-tag: #116329; --sh-property: #0550ae; --sh-line-number: #656d76;
	                                    }
	                                    [data-code-theme="github-light"] .page-content pre { background: #ffffff; border-color: #d0d7de; }
	                                    [data-code-theme="github-light"] .page-content pre code { color: #24292f; }

	                                    /* ── Code Block Window Styles ─────────────────────────── */

	                                    /* macOS style - three traffic-light dots in a title bar */
	                                    html[data-code-style="macos"] .page-content pre {
	                                        padding-top: 2.75rem !important;
	                                        border-radius: 10px !important;
	                                    }
	                                    html[data-code-style="macos"] .page-content pre::before {
	                                        content: '';
	                                        display: block;
	                                        position: absolute;
	                                        top: 0;
	                                        left: 0;
	                                        right: 0;
	                                        height: 32px;
	                                        background: rgba(128,128,128,0.08);
	                                        border-bottom: 1px solid rgba(128,128,128,0.1);
	                                        border-radius: 10px 10px 0 0;
	                                    }
	                                    html[data-code-style="macos"] .page-content pre::after {
	                                        content: '';
	                                        position: absolute;
	                                        top: 10px;
	                                        left: 14px;
	                                        width: 12px;
	                                        height: 12px;
	                                        border-radius: 50%;
	                                        background: #ff5f57;
	                                        box-shadow: 20px 0 0 #febc2e, 40px 0 0 #28c840;
	                                    }
	                                    html[data-code-style="macos"] .page-content pre .code-lang {
	                                        top: 0 !important;
	                                        right: auto !important;
	                                        left: 50% !important;
	                                        transform: translateX(-50%) !important;
	                                        border-radius: 0 0 var(--radius-sm) var(--radius-sm) !important;
	                                        border-top: none !important;
	                                        line-height: 32px !important;
	                                        padding: 0 0.75rem !important;
	                                        background: transparent !important;
	                                        border: none !important;
	                                    }
	                                    html[data-code-style="macos"] .page-content pre .copy-btn {
	                                        top: 4px !important;
	                                        right: 8px !important;
	                                    }
	                                    html[data-code-style="macos"] .page-content pre.has-line-numbers .line-numbers {
	                                        top: 32px !important;
	                                    }
	                                    /* Light mode adjustments for macOS dots */
	                                    [data-theme="light"] html[data-code-style="macos"] .page-content pre::before,
	                                    html[data-code-style="macos"][data-theme="light"] .page-content pre::before {
	                                        background: rgba(128,128,128,0.06);
	                                    }

	                                    /* Terminal style - dark header with prompt icon */
	                                    html[data-code-style="terminal"] .page-content pre {
	                                        padding-top: 2.75rem !important;
	                                        border-radius: 2px !important;
	                                    }
	                                    html[data-code-style="terminal"] .page-content pre::before {
	                                        content: '';
	                                        display: block;
	                                        position: absolute;
	                                        top: 0;
	                                        left: 0;
	                                        right: 0;
	                                        height: 34px;
	                                        background: #1a1a2e;
	                                        border-bottom: 1px solid rgba(0,255,100,0.15);
	                                        border-radius: 2px 2px 0 0;
	                                    }
	                                    html[data-code-style="terminal"] .page-content pre::after {
	                                        content: '$';
	                                        position: absolute;
	                                        top: 7px;
	                                        left: 14px;
	                                        font-family: var(--font-mono);
	                                        font-size: 0.85rem;
	                                        font-weight: 700;
	                                        color: #4ade80;
	                                    }
	                                    html[data-code-style="terminal"] .page-content pre .code-lang {
	                                        top: 0 !important;
	                                        right: auto !important;
	                                        left: 32px !important;
	                                        border-radius: 0 !important;
	                                        line-height: 34px !important;
	                                        padding: 0 0.5rem !important;
	                                        background: transparent !important;
	                                        border: none !important;
	                                        color: #4ade80 !important;
	                                        font-weight: 600 !important;
	                                        opacity: 0.9 !important;
	                                    }
	                                    html[data-code-style="terminal"] .page-content pre .copy-btn {
	                                        top: 5px !important;
	                                        right: 8px !important;
	                                    }
	                                    html[data-code-style="terminal"] .page-content pre.has-line-numbers .line-numbers {
	                                        top: 34px !important;
	                                    }
	                                    /* Light mode terminal */
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre::before {
	                                        background: #e8e8f0;
	                                        border-bottom-color: rgba(0,128,60,0.2);
	                                    }
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre::after {
	                                        color: #16a34a;
	                                    }
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre .code-lang {
	                                        color: #16a34a !important;
	                                    }

	                                    /* VS Code style - tab header with accent bar and activity border */
	                                    html[data-code-style="vscode"] .page-content pre {
	                                        padding-top: 2.75rem !important;
	                                        border-radius: 4px !important;
	                                        border-left: 3px solid color-mix(in srgb, var(--color-primary) 40%, transparent) !important;
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre::before {
	                                        content: '';
	                                        display: block;
	                                        position: absolute;
	                                        top: 0;
	                                        left: -3px;
	                                        right: 0;
	                                        height: 3px;
	                                        background: var(--color-primary);
	                                        border-radius: 4px 4px 0 0;
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre::after {
	                                        content: '';
	                                        position: absolute;
	                                        top: 3px;
	                                        left: -3px;
	                                        right: 0;
	                                        height: 32px;
	                                        background: rgba(128,128,128,0.06);
	                                        border-bottom: 1px solid rgba(128,128,128,0.1);
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre .code-lang {
	                                        top: 3px !important;
	                                        right: auto !important;
	                                        left: 0 !important;
	                                        border-radius: 0 !important;
	                                        line-height: 32px !important;
	                                        padding: 0 1rem !important;
	                                        background: rgba(128,128,128,0.04) !important;
	                                        border: none !important;
	                                        border-right: 1px solid rgba(128,128,128,0.1) !important;
	                                        border-bottom: 2px solid var(--color-primary) !important;
	                                        font-weight: 500 !important;
	                                        opacity: 1 !important;
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre .copy-btn {
	                                        top: 7px !important;
	                                        right: 8px !important;
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre.has-line-numbers {
	                                        padding-left: 4rem !important;
	                                    }
	                                    html[data-code-style="vscode"] .page-content pre.has-line-numbers .line-numbers {
	                                        top: 35px !important;
	                                        left: 0 !important;
	                                        background: rgba(128,128,128,0.04) !important;
	                                        border-right: 1px solid rgba(128,128,128,0.1) !important;
	                                    }

	                                    /* Tables */
	                                    .table-responsive {
	                                        overflow-x: auto;
	                                        -webkit-overflow-scrolling: touch;
	                                        margin: 1.5rem 0;
	                                    }
	                                    .table-responsive table { margin: 0; }
	                                    .page-content table {
	                                        width: 100%;
	                                        margin: 1.5rem 0;
	                                        border-collapse: collapse;
	                                        font-size: 0.875rem;
	                                        display: block;
	                                        overflow-x: auto;
	                                        -webkit-overflow-scrolling: touch;
	                                    }
	                                    .page-content thead, .page-content tbody, .page-content tr { width: 100%; }
	                                    .page-content th, .page-content td {
	                                        padding: 0.625rem 1rem;
	                                        text-align: left;
	                                        border: 1px solid var(--color-border);
	                                        white-space: nowrap;
	                                    }
	                                    .page-content td:last-child { white-space: normal; }
	                                    .page-content th { background: var(--color-bg-secondary); font-weight: 600; }
	                                    .page-content tr:hover td { background: var(--color-bg-secondary); }

	                                    /* Package install widget */
	                                    .package-install-widget {
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius, 8px);
	                                        margin-bottom: 2rem;
	                                        overflow: hidden;
	                                    }
	                                    .install-header {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: space-between;
	                                        padding: 0.5rem 1rem;
	                                        background: var(--color-bg-secondary);
	                                        border-bottom: 1px solid var(--color-border);
	                                    }
	                                    .install-package-name {
	                                        font-weight: 600;
	                                        font-size: 0.875rem;
	                                        color: var(--color-text);
	                                    }
	                                    .install-nuget-link {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        font-size: 0.75rem;
	                                        font-weight: 500;
	                                        color: var(--color-text-secondary);
	                                        text-decoration: none;
	                                        padding: 0.25rem 0.5rem;
	                                        border-radius: var(--radius-sm, 4px);
	                                        transition: all var(--transition);
	                                    }
	                                    .install-nuget-link:hover {
	                                        color: var(--color-primary);
	                                        background: color-mix(in srgb, var(--color-primary) 10%, transparent);
	                                    }
	                                    .install-tabs {
	                                        display: flex;
	                                        gap: 0;
	                                        border-bottom: 1px solid var(--color-border);
	                                        background: var(--color-bg-secondary);
	                                        padding: 0 0.5rem;
	                                    }
	                                    .install-tab {
	                                        padding: 0.5rem 1rem;
	                                        font-size: 0.8125rem;
	                                        font-weight: 500;
	                                        color: var(--color-text-secondary);
	                                        background: none;
	                                        border: none;
	                                        border-bottom: 2px solid transparent;
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                        margin-bottom: -1px;
	                                    }
	                                    .install-tab:hover { color: var(--color-text); }
	                                    .install-tab.active {
	                                        color: var(--color-primary-text);
	                                        border-bottom-color: var(--color-primary);
	                                        font-weight: 600;
	                                    }
	                                    .install-panel {
	                                        display: none;
	                                        position: relative;
	                                        padding: 0.875rem 1.25rem;
	                                        background: var(--color-bg-code, var(--color-bg-secondary));
	                                    }
	                                    .install-panel.active { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
	                                    .install-panel pre {
	                                        margin: 0;
	                                        padding: 0;
	                                        background: none !important;
	                                        border: none !important;
	                                        overflow-x: auto;
	                                        flex: 1;
	                                        min-width: 0;
	                                    }
	                                    .install-panel pre code {
	                                        font-family: var(--font-mono);
	                                        font-size: 0.8125rem;
	                                        line-height: 1.5;
	                                        color: var(--color-text);
	                                        background: none !important;
	                                        padding: 0 !important;
	                                    }
	                                    /* Hide the global copy button injected by code block JS */
	                                    .install-panel pre .copy-btn { display: none !important; }
	                                    .install-copy-btn {
	                                        flex-shrink: 0;
	                                        padding: 0.3rem 0.75rem;
	                                        font-size: 0.75rem;
	                                        font-weight: 500;
	                                        background: var(--color-bg, #fff);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-sm, 4px);
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                        white-space: nowrap;
	                                    }
	                                    .install-copy-btn:hover {
	                                        background: var(--color-primary);
	                                        color: #fff;
	                                        border-color: var(--color-primary);
	                                    }

	                                    /* API Reference styles */
	                                    .api-type-header { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1rem; }
	                                    .api-badge {
	                                        display: inline-block; padding: 0.25rem 0.625rem; border-radius: var(--radius-sm);
	                                        font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em;
	                                        min-width: 4.5rem; text-align: center;
	                                    }
	                                    .api-badge-class { background: color-mix(in srgb, #3b82f6 15%, var(--color-bg)); color: #3b82f6; }
	                                    .api-badge-struct { background: color-mix(in srgb, #22c55e 15%, var(--color-bg)); color: #22c55e; }
	                                    .api-badge-record { background: color-mix(in srgb, #8b5cf6 15%, var(--color-bg)); color: #8b5cf6; }
	                                    .api-badge-interface { background: color-mix(in srgb, #06b6d4 15%, var(--color-bg)); color: #06b6d4; }
	                                    .api-badge-enum { background: color-mix(in srgb, #f59e0b 15%, var(--color-bg)); color: #f59e0b; }
	                                    .api-badge-delegate { background: color-mix(in srgb, #ec4899 15%, var(--color-bg)); color: #ec4899; }
	                                    .api-badge-static { background: color-mix(in srgb, #64748b 15%, var(--color-bg)); color: #64748b; }
	                                    .api-badge-abstract { background: color-mix(in srgb, #a78bfa 15%, var(--color-bg)); color: #a78bfa; }
	                                    .api-badge-sealed { background: color-mix(in srgb, #f97316 15%, var(--color-bg)); color: #f97316; }
	                                    .api-badge-obsolete { background: color-mix(in srgb, #ef4444 15%, var(--color-bg)); color: #ef4444; }
	                                    .api-badge-sm { font-size: 0.625rem; padding: 0.125rem 0.375rem; border-radius: 3px; vertical-align: middle; }
	                                    .api-badge-sm.api-badge-static { background: color-mix(in srgb, #64748b 12%, var(--color-bg)); color: #64748b; }
	                                    .api-badge-sm.api-badge-virtual { background: color-mix(in srgb, #06b6d4 12%, var(--color-bg)); color: #06b6d4; }
	                                    .api-badge-sm.api-badge-abstract { background: color-mix(in srgb, #a78bfa 12%, var(--color-bg)); color: #a78bfa; }
	                                    .api-badge-sm.api-badge-obsolete { background: color-mix(in srgb, #ef4444 12%, var(--color-bg)); color: #ef4444; }
	                                    .api-signature { margin: 0.5rem 0 1.5rem; }
	                                    .api-namespace { font-size: 0.875rem; color: var(--color-text-secondary); margin-bottom: 1rem; }
	                                    .api-summary { margin-bottom: 1.5rem; font-size: 1.05rem; line-height: 1.7; }
	                                    .api-member-table { width: 100%; }
	                                    .api-member-table td:first-child { white-space: nowrap; }
	                                    .api-member-table td { vertical-align: top; padding: 0.5rem 1rem; }
	                                    .api-member-table td:last-child { color: var(--color-text-secondary); }
	                                    .api-member-table code { font-size: 0.8125rem; }
	                                    .api-member-table pre { white-space: pre-wrap; word-break: break-word; overflow-x: auto; }
	                                    .api-member-link { text-decoration: none; color: var(--color-primary-text); transition: opacity var(--transition); }
	                                    .api-member-link:hover { opacity: 0.8; }
	                                    .api-member-link code { color: inherit; background: none; padding: 0; font-weight: 500; }

	                                    /* Source viewer */
	                                    .source-viewer { margin-top: 2rem; border: 1px solid var(--color-border); border-radius: 8px; }
	                                    .source-viewer summary {
	                                        display: flex; align-items: center; gap: 0.5rem; padding: 0.75rem 1rem;
	                                        cursor: pointer; font-size: 0.875rem; font-weight: 500; color: var(--color-text-secondary);
	                                        user-select: none; list-style: none; transition: color 0.15s;
	                                    }
	                                    .source-viewer summary::-webkit-details-marker { display: none; }
	                                    .source-viewer summary::marker { display: none; content: ""; }
	                                    .source-viewer summary:hover { color: var(--color-primary-text); }
	                                    .source-viewer summary svg { flex-shrink: 0; }
	                                    .source-viewer[open] summary { border-bottom: 1px solid var(--color-border); }
	                                    .source-viewer pre { margin: 0; border: none; border-radius: 0 0 8px 8px; }

	                                    /* Blockquotes */
	                                    .page-content blockquote {
	                                        margin: 1.5rem 0;
	                                        padding: 0.75rem 1.25rem;
	                                        border-left: 4px solid var(--color-primary);
	                                        background: var(--color-bg-secondary);
	                                        border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
	                                        color: var(--color-text-secondary);
	                                    }

	                                    /* Admonitions - AdmonitionExtension renders ::: type [title] as
	                                       <div class="admonition admonition-{type}"> with a title row and content body.
	                                       Descendant selectors, not direct children, so admonitions nested in tabs
	                                       or components still pick up the styling. */
	                                    .page-content .admonition {
	                                        margin: 1.5rem 0;
	                                        padding: 1rem 1.25rem;
	                                        border-radius: var(--radius);
	                                        border-left: 4px solid;
	                                    }
	                                    .page-content .admonition-title {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.5rem;
	                                        font-weight: 600;
	                                        font-size: 0.9375rem;
	                                        margin-bottom: 0.5rem;
	                                    }
	                                    .page-content .admonition-icon { font-size: 1rem; line-height: 1; }
	                                    .page-content .admonition-content > :first-child { margin-top: 0; }
	                                    .page-content .admonition-content > :last-child { margin-bottom: 0; }
	                                    .page-content .admonition-note { border-color: #3b82f6; background: color-mix(in srgb, #3b82f6 8%, var(--color-bg)); }
	                                    .page-content .admonition-note .admonition-title { color: #3b82f6; }
	                                    .page-content .admonition-tip { border-color: #22c55e; background: color-mix(in srgb, #22c55e 8%, var(--color-bg)); }
	                                    .page-content .admonition-tip .admonition-title { color: #22c55e; }
	                                    .page-content .admonition-warning { border-color: #f59e0b; background: color-mix(in srgb, #f59e0b 8%, var(--color-bg)); }
	                                    .page-content .admonition-warning .admonition-title { color: #f59e0b; }
	                                    .page-content .admonition-caution { border-color: #f59e0b; background: color-mix(in srgb, #f59e0b 8%, var(--color-bg)); }
	                                    .page-content .admonition-caution .admonition-title { color: #f59e0b; }
	                                    .page-content .admonition-danger { border-color: #ef4444; background: color-mix(in srgb, #ef4444 8%, var(--color-bg)); }
	                                    .page-content .admonition-danger .admonition-title { color: #ef4444; }
	                                    .page-content .admonition-info { border-color: #8b5cf6; background: color-mix(in srgb, #8b5cf6 8%, var(--color-bg)); }
	                                    .page-content .admonition-info .admonition-title { color: #8b5cf6; }
	                                    .page-content .admonition-important { border-color: #8b5cf6; background: color-mix(in srgb, #8b5cf6 8%, var(--color-bg)); }
	                                    .page-content .admonition-important .admonition-title { color: #8b5cf6; }

	                                    /* Tabs */
	                                    .tabs { margin: 1.5rem 0; border: 1px solid var(--color-border); border-radius: var(--radius); }
	                                    .tab-headers {
	                                        display: flex;
	                                        border-bottom: 1px solid var(--color-border);
	                                        background: var(--color-bg-secondary);
	                                        border-radius: var(--radius) var(--radius) 0 0;
	                                        overflow-x: auto;
	                                    }
	                                    .tab-header {
	                                        padding: 0.5rem 1rem;
	                                        border: none;
	                                        background: transparent;
	                                        font-size: 0.875rem;
	                                        font-weight: 500;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        border-bottom: 2px solid transparent;
	                                        transition: all var(--transition);
	                                        white-space: nowrap;
	                                    }
	                                    .tab-header:hover { color: var(--color-text); }
	                                    .tab-header.active { color: var(--color-primary); border-bottom-color: var(--color-primary); }
	                                    .tab-content { padding: 1rem 1.25rem; }
	                                    .tab-content[hidden] { display: none; }

	                                    /* UI Components - Card */
	                                    .component-card {
	                                        margin: 1.5rem 0;
	                                        padding: 1.25rem 1.5rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: var(--color-bg);
	                                        transition: border-color var(--transition), box-shadow var(--transition);
	                                    }
	                                    .component-card:hover {
	                                        border-color: var(--color-primary);
	                                        box-shadow: var(--shadow-sm);
	                                    }
	                                    .component-card-info { border-left: 4px solid #3b82f6; }
	                                    .component-card-success { border-left: 4px solid #22c55e; }
	                                    .component-card-warning { border-left: 4px solid #f59e0b; }
	                                    .component-card-header {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.5rem;
	                                        margin-bottom: 0.625rem;
	                                    }
	                                    .component-card-icon {
	                                        display: flex;
	                                        align-items: center;
	                                        color: var(--color-primary);
	                                        flex-shrink: 0;
	                                    }
	                                    .component-card-info .component-card-icon { color: #3b82f6; }
	                                    .component-card-success .component-card-icon { color: #22c55e; }
	                                    .component-card-warning .component-card-icon { color: #f59e0b; }
	                                    .component-card-title {
	                                        font-size: 1.05rem;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                    }
	                                    .component-card-body {
	                                        color: var(--color-text-secondary);
	                                        font-size: 0.9375rem;
	                                        line-height: 1.65;
	                                    }
	                                    .component-card-body > *:first-child { margin-top: 0; }
	                                    .component-card-body > *:last-child { margin-bottom: 0; }

	                                    /* UI Components - Steps */
	                                    .component-steps {
	                                        margin: 1.5rem 0;
	                                        display: flex;
	                                        flex-direction: column;
	                                        gap: 0;
	                                    }
	                                    .component-step {
	                                        display: flex;
	                                        gap: 1rem;
	                                        position: relative;
	                                    }
	                                    .component-step-indicator {
	                                        display: flex;
	                                        flex-direction: column;
	                                        align-items: center;
	                                        flex-shrink: 0;
	                                    }
	                                    .component-step-number {
	                                        width: 32px;
	                                        height: 32px;
	                                        border-radius: 50%;
	                                        background: var(--color-primary);
	                                        color: #fff;
	                                        font-size: 0.875rem;
	                                        font-weight: 600;
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        flex-shrink: 0;
	                                        position: relative;
	                                        z-index: 1;
	                                    }
	                                    .component-step:not(:last-child) .component-step-indicator::after {
	                                        content: '';
	                                        width: 2px;
	                                        flex: 1;
	                                        background: var(--color-border);
	                                        margin-top: 0.25rem;
	                                    }
	                                    .component-step-content {
	                                        padding-bottom: 1.5rem;
	                                        min-width: 0;
	                                        flex: 1;
	                                    }
	                                    .component-step:last-child .component-step-content {
	                                        padding-bottom: 0;
	                                    }
	                                    .component-step-title {
	                                        font-size: 1rem;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                        margin: 0.25rem 0 0.5rem;
	                                        line-height: 1.4;
	                                    }
	                                    .component-step-content > *:not(.component-step-title) {
	                                        font-size: 0.9375rem;
	                                        color: var(--color-text-secondary);
	                                    }
	                                    .component-step-content > *:last-child { margin-bottom: 0; }

	                                    /* UI Components - Link Cards */
	                                    .component-link-cards {
	                                        display: grid;
	                                        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
	                                        gap: 0.75rem;
	                                        margin: 1.5rem 0;
	                                    }
	                                    .component-link-card {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: space-between;
	                                        padding: 1rem 1.25rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: var(--color-bg);
	                                        text-decoration: none;
	                                        transition: all var(--transition);
	                                        gap: 0.75rem;
	                                    }
	                                    .component-link-card:hover {
	                                        border-color: var(--color-primary);
	                                        box-shadow: var(--shadow-sm);
	                                        color: var(--color-text);
	                                    }
	                                    .component-link-card-content {
	                                        display: flex;
	                                        flex-direction: column;
	                                        gap: 0.25rem;
	                                        min-width: 0;
	                                    }
	                                    .component-link-card-title {
	                                        font-size: 0.9375rem;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                        line-height: 1.35;
	                                    }
	                                    .component-link-card-desc {
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                        line-height: 1.5;
	                                    }
	                                    .component-link-card-arrow {
	                                        color: var(--color-text-muted);
	                                        flex-shrink: 0;
	                                        transition: transform var(--transition), color var(--transition);
	                                    }
	                                    .component-link-card:hover .component-link-card-arrow {
	                                        color: var(--color-primary);
	                                        transform: translateX(2px);
	                                    }

	                                    /* UI Components - Code Group */
	                                    .component-code-group .tab-content { padding: 0; }
	                                    .component-code-group .tab-content pre {
	                                        margin: 0;
	                                        border: none;
	                                        border-radius: 0 0 var(--radius) var(--radius);
	                                    }

	                                    /* ToC Sidebar - "On this page" */
	                                    .toc-sidebar {
	                                        position: sticky;
	                                        top: var(--header-height);
	                                        width: var(--toc-width);
	                                        height: calc(100vh - var(--header-height));
	                                        overflow-y: auto;
	                                        padding: 1.5rem 0 1.5rem 0;
	                                        flex-shrink: 0;
	                                        border-left: 1px solid var(--color-border);
	                                    }
	                                    .toc-title {
	                                        font-size: 0.75rem;
	                                        font-weight: 600;
	                                        text-transform: uppercase;
	                                        letter-spacing: 0.05em;
	                                        color: var(--color-text);
	                                        margin-bottom: 0.75rem;
	                                        padding: 0 1rem;
	                                    }
	                                    .toc-list { list-style: none; border-left: 2px solid var(--color-border); margin-left: 1rem; }
	                                    .toc-list ul { list-style: none; padding-left: 0; }
	                                    .toc-item a {
	                                        display: block;
	                                        padding: 0.25rem 0.75rem;
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                        transition: all var(--transition);
	                                        line-height: 1.4;
	                                        border-left: 2px solid transparent;
	                                        margin-left: -2px;
	                                    }
	                                    .toc-item a:hover { color: var(--color-text); }
	                                    .toc-item a.active {
	                                        color: var(--color-primary-text);
	                                        font-weight: 500;
	                                        border-left-color: var(--color-primary);
	                                    }
	                                    .toc-level-3 a { padding-left: 1.5rem; }
	                                    .toc-level-4 a { padding-left: 2.25rem; }

	                                    /* ToC progress indicator */
	                                    .toc-progress {
	                                        height: 3px;
	                                        background: var(--color-border);
	                                        border-radius: 2px;
	                                        margin-bottom: 0.75rem;
	                                        overflow: hidden;
	                                    }
	                                    .toc-progress-bar {
	                                        height: 100%;
	                                        background: var(--color-primary);
	                                        border-radius: 2px;
	                                        width: 0%;
	                                        transition: width 0.15s ease;
	                                    }

	                                    /* ToC expand/collapse for nested items */
	                                    .toc-item.has-children > a::after {
	                                        content: '';
	                                        display: inline-block;
	                                        width: 0;
	                                        height: 0;
	                                        border-left: 4px solid transparent;
	                                        border-right: 4px solid transparent;
	                                        border-top: 5px solid currentColor;
	                                        margin-left: 0.4em;
	                                        opacity: 0.5;
	                                        transition: transform 0.2s;
	                                        vertical-align: middle;
	                                    }
	                                    .toc-item.has-children.collapsed > a::after {
	                                        transform: rotate(-90deg);
	                                    }
	                                    .toc-item.has-children.collapsed > ul {
	                                        display: none;
	                                    }

	                                    /* ToC section counter */
	                                    .toc-counter {
	                                        font-size: 0.7rem;
	                                        color: var(--color-text-secondary);
	                                        padding: 0.5rem 0 0;
	                                        margin-top: 0.5rem;
	                                        border-top: 1px solid var(--color-border);
	                                        text-align: center;
	                                    }

	                                    /* ToC back-to-top link */
	                                    .toc-back-to-top {
	                                        display: block;
	                                        font-size: 0.75rem;
	                                        color: var(--color-text-muted);
	                                        text-decoration: none;
	                                        padding: 0.25rem 0.75rem;
	                                        margin-bottom: 0.25rem;
	                                        transition: color var(--transition);
	                                    }
	                                    .toc-back-to-top:hover { color: var(--color-primary-text); }

	                                    /* Breadcrumbs */
	                                    .breadcrumbs {
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                        margin-bottom: 1.5rem;
	                                    }
	                                    .breadcrumbs ol {
	                                        display: flex;
	                                        align-items: center;
	                                        flex-wrap: wrap;
	                                        list-style: none;
	                                        gap: 0;
	                                    }
	                                    .breadcrumbs li { display: flex; align-items: center; }
	                                    .breadcrumbs li:not(:last-child)::after {
	                                        content: '/';
	                                        margin: 0 0.5rem;
	                                        color: var(--color-text-muted);
	                                        opacity: 0.5;
	                                    }
	                                    .breadcrumbs a { color: var(--color-text-secondary); }
	                                    .breadcrumbs a:hover { color: var(--color-primary-text); }
	                                    .breadcrumbs [aria-current="page"] { color: var(--color-text); font-weight: 500; }

	                                    /* Edit link / Last updated */
	                                    .edit-link, .last-updated {
	                                        margin-top: 3rem;
	                                        padding-top: 1rem;
	                                        border-top: 1px solid var(--color-border);
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                    }
	                                    .edit-link + .last-updated { margin-top: 0.5rem; padding-top: 0; border-top: none; }
	                                    .edit-link a { color: var(--color-text-secondary); display: inline-flex; align-items: center; gap: 0.375rem; }
	                                    .edit-link a:hover { color: var(--color-primary-text); }
	                                    .last-updated { display: flex; align-items: center; gap: 0.375rem; }

	                                    /* Page contributors (theme.options.showContributors) */
	                                    .page-contributors {
	                                        display: flex;
	                                        flex-wrap: wrap;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        margin-top: 3rem;
	                                        padding-top: 1rem;
	                                        border-top: 1px solid var(--color-border);
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                    }
	                                    :is(.edit-link, .last-updated) + .page-contributors { margin-top: 0.5rem; padding-top: 0; border-top: none; }
	                                    .page-contributors > svg { flex-shrink: 0; }
	                                    .page-content .page-contributors-list {
	                                        display: flex;
	                                        flex-wrap: wrap;
	                                        gap: 0.25rem 0.75rem;
	                                        margin: 0;
	                                        padding: 0;
	                                        list-style: none;
	                                    }
	                                    .page-content .page-contributors-list > li { display: inline-flex; align-items: center; gap: 0.375rem; margin: 0; line-height: 1.5; }
	                                    .page-contributor-avatar {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 1.5rem;
	                                        height: 1.5rem;
	                                        border-radius: 50%;
	                                        border: 1px solid var(--color-border);
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text-secondary);
	                                        font-size: 0.625rem;
	                                        font-weight: 600;
	                                        letter-spacing: 0.02em;
	                                        flex-shrink: 0;
	                                    }

	                                    /* Footer */
	                                    .site-footer {
	                                        border-top: 1px solid var(--color-border);
	                                        padding: 1.5rem;
	                                    }
	                                    .footer-inner {
	                                        max-width: 1440px;
	                                        margin: 0 auto;
	                                        display: flex;
	                                        justify-content: space-between;
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                    }
	                                    .built-with a { color: var(--color-text-secondary); }
	                                    .footer-social { display: flex; align-items: center; gap: 0.75rem; }
	                                    .footer-social a { color: var(--color-text-muted); transition: color var(--transition); display: flex; }
	                                    .footer-social a:hover { color: var(--color-primary); }

	                                    /* Heading anchor links */
	                                    .page-content h2, .page-content h3, .page-content h4 { position: relative; }
	                                    .heading-anchor {
	                                        position: absolute; left: -1.5rem; top: 50%; transform: translateY(-50%);
	                                        color: var(--color-text-muted); text-decoration: none; font-size: 0.875rem;
	                                        opacity: 0; transition: opacity 0.15s; font-weight: 400;
	                                    }
	                                    .page-content h2:hover .heading-anchor,
	                                    .page-content h3:hover .heading-anchor,
	                                    .page-content h4:hover .heading-anchor { opacity: 1; }
	                                    .heading-anchor:hover { color: var(--color-primary); }

	                                    /* Responsive */
	                                    @media (max-width: 1200px) {
	                                        .toc-sidebar {
	                                            display: none;
	                                            position: fixed;
	                                            top: var(--header-height);
	                                            right: 0;
	                                            bottom: 0;
	                                            width: 280px;
	                                            max-width: 80vw;
	                                            background: var(--color-bg);
	                                            border-left: 1px solid var(--color-border);
	                                            box-shadow: -4px 0 24px rgba(0,0,0,0.1);
	                                            z-index: 100;
	                                            padding: 1.5rem;
	                                            overflow-y: auto;
	                                            transition: transform 0.3s ease;
	                                            height: auto;
	                                        }
	                                        .toc-sidebar.open { display: block; }
	                                        .toc-toggle-btn {
	                                            display: flex;
	                                            position: fixed;
	                                            bottom: 5rem;
	                                            right: 1rem;
	                                            width: 40px;
	                                            height: 40px;
	                                            align-items: center;
	                                            justify-content: center;
	                                            background: var(--color-primary);
	                                            color: white;
	                                            border: none;
	                                            border-radius: 50%;
	                                            cursor: pointer;
	                                            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
	                                            z-index: 99;
	                                            transition: opacity 0.3s, transform 0.3s;
	                                        }
	                                        .toc-toggle-btn:hover { transform: scale(1.1); }
	                                        .toc-toggle-btn svg { width: 20px; height: 20px; }
	                                    }
	                                    @media (min-width: 1201px) {
	                                        .toc-toggle-btn { display: none; }
	                                    }

	                                    @media (max-width: 768px) {
	                                        .sidebar { display: none; position: fixed; top: var(--header-height); left: 0; bottom: 0; z-index: 50; background: var(--color-bg); }
	                                        .sidebar.open { display: block; }
	                                        .mobile-nav-toggle { display: flex; }
	                                        .content { padding: 1.5rem 1rem; }
	                                        .search-label, .search-trigger kbd { display: none; }
	                                        .search-trigger { padding: 0.375rem; }
	                                        .header-inner { padding: 0 0.75rem; gap: 0.5rem; }
	                                        .site-logo { gap: 0.375rem; min-width: 0; }
	                                        .site-name { font-size: 0.9375rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	                                        .header-actions { gap: 0.25rem; flex-shrink: 0; }
	                                        .appearance-group { display: none; }
	                                        .color-theme-selector, .code-theme-selector, .code-style-selector { display: none; }
	                                        .search-modal { padding-top: 5vh; }
	                                        .search-dialog { width: 95%; max-height: 70vh; }
	                                        .footer-inner { flex-direction: column; gap: 0.5rem; align-items: center; }
	                                    }

	                                    /* Search Modal */
	                                    .search-modal { position: fixed; inset: 0; z-index: 200; display: flex; align-items: flex-start; justify-content: center; padding-top: 15vh; }
	                                    .search-modal[hidden] { display: none; }
	                                    .search-backdrop { position: absolute; inset: 0; background: rgba(0,0,0,0.5); backdrop-filter: blur(2px); }
	                                    .search-dialog {
	                                        position: relative;
	                                        width: 90%;
	                                        max-width: 560px;
	                                        max-height: 60vh;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        box-shadow: 0 16px 48px rgba(0,0,0,0.2);
	                                        display: flex;
	                                        flex-direction: column;
	                                        overflow: hidden;
	                                    }
	                                    .search-input-wrap {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.75rem;
	                                        padding: 0.75rem 1rem;
	                                        border-bottom: 1px solid var(--color-border);
	                                        color: var(--color-text-muted);
	                                    }
	                                    .search-input {
	                                        flex: 1;
	                                        border: none;
	                                        outline: none;
	                                        background: transparent;
	                                        font-size: 1rem;
	                                        font-family: var(--font-body);
	                                        color: var(--color-text);
	                                    }
	                                    .search-input::placeholder { color: var(--color-text-muted); }
	                                    .search-esc {
	                                        font-family: var(--font-mono);
	                                        font-size: 0.7rem;
	                                        padding: 0.125rem 0.375rem;
	                                        background: var(--color-bg-secondary);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-sm);
	                                        color: var(--color-text-muted);
	                                    }
	                                    .search-results {
	                                        overflow-y: auto;
	                                        padding: 0.5rem;
	                                    }
	                                    .search-results:empty::after {
	                                        content: 'Type to search...';
	                                        display: block;
	                                        padding: 2rem;
	                                        text-align: center;
	                                        color: var(--color-text-muted);
	                                        font-size: 0.875rem;
	                                    }
	                                    .search-results.has-query:empty::after { content: 'No results found.'; }
	                                    .search-result {
	                                        display: block;
	                                        padding: 0.625rem 0.75rem;
	                                        border-radius: var(--radius-sm);
	                                        cursor: pointer;
	                                        text-decoration: none;
	                                        color: var(--color-text);
	                                        transition: background var(--transition);
	                                    }
	                                    .search-result:hover, .search-result.active { background: var(--color-bg-secondary); color: var(--color-text); }
	                                    .search-result-title { font-weight: 600; font-size: 0.875rem; }
	                                    .search-result-section { font-size: 0.8125rem; color: var(--color-primary-text); margin-left: 0.25rem; }
	                                    .search-result-category { font-size: 0.75rem; color: var(--color-text-muted); }
	                                    .search-result-snippet { font-size: 0.8125rem; color: var(--color-text-secondary); margin-top: 0.125rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
	                                    .search-result-snippet mark { background: color-mix(in srgb, var(--color-accent) 30%, transparent); color: inherit; border-radius: 2px; }

	                                    /* Prev/Next page navigation */
	                                    .page-nav {
	                                        display: grid;
	                                        grid-template-columns: 1fr 1fr;
	                                        gap: 1rem;
	                                        max-width: var(--content-max-width);
	                                        margin: 2.5rem auto 0;
	                                        padding: 1.5rem 2.5rem;
	                                    }
	                                    .page-nav-spacer { display: block; }
	                                    .page-nav-link {
	                                        display: flex;
	                                        align-items: center;
	                                        gap: 0.75rem;
	                                        padding: 1rem 1.25rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-md, 8px);
	                                        text-decoration: none;
	                                        color: var(--color-text);
	                                        transition: border-color var(--transition), background var(--transition), box-shadow var(--transition);
	                                    }
	                                    .page-nav-link:hover {
	                                        border-color: var(--color-primary);
	                                        background: color-mix(in srgb, var(--color-primary) 4%, var(--color-bg));
	                                        box-shadow: 0 2px 8px rgba(0,0,0,0.04);
	                                    }
	                                    .page-nav-next { text-align: right; justify-content: flex-end; }
	                                    .page-nav-text { display: flex; flex-direction: column; gap: 0.125rem; min-width: 0; }
	                                    .page-nav-label { font-size: 0.75rem; font-weight: 500; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.04em; }
	                                    .page-nav-title { font-size: 0.9375rem; font-weight: 600; color: var(--color-primary-text); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	                                    .page-nav-arrow { flex-shrink: 0; color: var(--color-text-muted); transition: color var(--transition), transform var(--transition); }
	                                    .page-nav-link:hover .page-nav-arrow { color: var(--color-primary); }
	                                    .page-nav-prev:hover .page-nav-arrow { transform: translateX(-2px); }
	                                    .page-nav-next:hover .page-nav-arrow { transform: translateX(2px); }

	                                    /* Feedback widget */
	                                    .feedback-widget {
	                                        display: flex;
	                                        flex-direction: column;
	                                        align-items: center;
	                                        gap: 0.75rem;
	                                        max-width: var(--content-max-width);
	                                        margin: 2.5rem auto 0;
	                                        padding: 1.5rem 2.5rem;
	                                        text-align: center;
	                                    }
	                                    .feedback-prompt {
	                                        font-size: 0.875rem;
	                                        font-weight: 500;
	                                        color: var(--color-text-secondary);
	                                    }
	                                    .feedback-buttons {
	                                        display: flex;
	                                        gap: 0.5rem;
	                                    }
	                                    .feedback-btn {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        padding: 0.4rem 0.875rem;
	                                        font-size: 0.8125rem;
	                                        font-weight: 500;
	                                        font-family: inherit;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-md, 8px);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: border-color 0.2s, background 0.2s, color 0.2s, box-shadow 0.2s;
	                                    }
	                                    .feedback-btn:hover { border-color: var(--color-text-muted); }
	                                    .feedback-btn-yes:hover { border-color: #22c55e; color: #16a34a; background: color-mix(in srgb, #22c55e 8%, transparent); }
	                                    .feedback-btn-no:hover { border-color: #ef4444; color: #dc2626; background: color-mix(in srgb, #ef4444 8%, transparent); }
	                                    .feedback-btn-yes.selected { border-color: #22c55e; color: #fff; background: #22c55e; }
	                                    .feedback-btn-no.selected { border-color: #ef4444; color: #fff; background: #ef4444; }
	                                    .feedback-btn:disabled { cursor: default; }
	                                    .feedback-btn-yes:disabled:not(.selected) { opacity: 0.4; }
	                                    .feedback-btn-no:disabled:not(.selected) { opacity: 0.4; }
	                                    .feedback-thanks {
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                        animation: feedbackFadeIn 0.3s ease;
	                                    }
	                                    @keyframes feedbackFadeIn {
	                                        from { opacity: 0; transform: translateY(-4px); }
	                                        to { opacity: 1; transform: translateY(0); }
	                                    }

	                                    /* Back to top button */
	                                    .back-to-top { position: fixed; bottom: 2rem; right: 2rem; width: 40px; height: 40px; border-radius: 50%; background: var(--color-primary); color: white; border: none; font-size: 1.25rem; cursor: pointer; opacity: 0; pointer-events: none; transition: opacity 0.2s; z-index: 50; box-shadow: var(--shadow-md); }
	                                    .back-to-top.visible { opacity: 1; pointer-events: auto; }

	                                    /* =============================== */
	                                    /* Landing Page                    */
	                                    /* =============================== */

	                                    .landing .landing-content {
	                                        max-width: 1200px;
	                                        margin: 0 auto;
	                                        padding: 0 2rem;
	                                    }

	                                    /* Subtle hero background glow */
	                                    .landing::before {
	                                        content: '';
	                                        position: absolute;
	                                        top: 0;
	                                        left: 50%;
	                                        transform: translateX(-50%);
	                                        width: 800px;
	                                        height: 500px;
	                                        background: radial-gradient(ellipse, color-mix(in srgb, var(--color-primary) 8%, transparent) 0%, transparent 70%);
	                                        pointer-events: none;
	                                        z-index: 0;
	                                    }
	                                    .landing { position: relative; }
	                                    .landing .landing-content { position: relative; z-index: 1; }

	                                    /* Hero - first h1 + first p */
	                                    .landing .landing-content > h1:first-child,
	                                    .landing .landing-content > h1:first-of-type {
	                                        font-size: clamp(2.75rem, 6vw, 4.5rem);
	                                        font-weight: 800;
	                                        text-align: center;
	                                        margin: 5rem auto 1.25rem;
	                                        max-width: 800px;
	                                        letter-spacing: -0.03em;
	                                        line-height: 1.1;
	                                        background: linear-gradient(135deg, var(--color-primary), var(--color-primary-light));
	                                        -webkit-background-clip: text;
	                                        -webkit-text-fill-color: transparent;
	                                        background-clip: text;
	                                    }

	                                    .landing .landing-content > h1:first-of-type + p {
	                                        font-size: clamp(1.125rem, 2vw, 1.375rem);
	                                        text-align: center;
	                                        color: var(--color-text-secondary);
	                                        max-width: 640px;
	                                        margin: 0 auto 2.5rem;
	                                        line-height: 1.6;
	                                    }

	                                    /* CTA buttons row - first ul after hero p, or a p with links */
	                                    .landing .landing-content > h1:first-of-type + p + p,
	                                    .landing .landing-content > h1:first-of-type + p + ul {
	                                        text-align: center;
	                                        display: flex;
	                                        gap: 1rem;
	                                        justify-content: center;
	                                        flex-wrap: wrap;
	                                        list-style: none;
	                                        padding: 0;
	                                        margin: 0 auto 4rem;
	                                    }

	                                    .landing .landing-content > h1:first-of-type + p + p a,
	                                    .landing .landing-content > h1:first-of-type + p + ul a {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.5rem;
	                                        padding: 0.75rem 1.75rem;
	                                        border-radius: var(--radius);
	                                        font-weight: 600;
	                                        font-size: 1rem;
	                                        text-decoration: none;
	                                        transition: all var(--transition);
	                                    }

	                                    .landing .landing-content > h1:first-of-type + p + p a:first-child,
	                                    .landing .landing-content > h1:first-of-type + p + ul a:first-of-type {
	                                        background: var(--color-primary);
	                                        color: white;
	                                        box-shadow: var(--shadow-md);
	                                    }
	                                    .landing .landing-content > h1:first-of-type + p + p a:first-child:hover,
	                                    .landing .landing-content > h1:first-of-type + p + ul a:first-of-type:hover {
	                                        background: var(--color-primary-dark);
	                                        transform: translateY(-1px);
	                                        box-shadow: var(--shadow-lg);
	                                    }

	                                    .landing .landing-content > h1:first-of-type + p + p a:not(:first-child),
	                                    .landing .landing-content > h1:first-of-type + p + ul a:not(:first-of-type) {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                        border: 1px solid var(--color-border);
	                                    }
	                                    .landing .landing-content > h1:first-of-type + p + p a:not(:first-child):hover,
	                                    .landing .landing-content > h1:first-of-type + p + ul a:not(:first-of-type):hover {
	                                        border-color: var(--color-primary);
	                                        color: var(--color-primary);
	                                    }

	                                    /* Feature cards - h2 sections become cards */
	                                    .landing .landing-content > h2 {
	                                        font-size: 1.75rem;
	                                        text-align: center;
	                                        margin: 4rem auto 2.5rem;
	                                        font-weight: 700;
	                                    }

	                                    .landing .landing-content > ul {
	                                        display: grid;
	                                        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
	                                        gap: 1.5rem;
	                                        list-style: none;
	                                        padding: 0;
	                                        margin: 0 auto 3rem;
	                                        max-width: 960px;
	                                    }

	                                    .landing .landing-content > ul > li {
	                                        background: var(--color-bg-secondary);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        padding: 1.75rem;
	                                        transition: border-color var(--transition), box-shadow var(--transition), transform var(--transition);
	                                    }
	                                    .landing .landing-content > ul > li:hover {
	                                        border-color: var(--color-primary);
	                                        box-shadow: 0 8px 24px color-mix(in srgb, var(--color-primary) 12%, transparent);
	                                        transform: translateY(-2px);
	                                    }

	                                    .landing .landing-content > ul > li strong {
	                                        display: block;
	                                        font-size: 1.0625rem;
	                                        margin-bottom: 0.5rem;
	                                        color: var(--color-primary);
	                                    }

	                                    .landing .landing-content > ul > li code {
	                                        font-size: 0.8125rem;
	                                    }

	                                    /* Divider line - hr */
	                                    .landing .landing-content > hr {
	                                        border: none;
	                                        border-top: 1px solid var(--color-border);
	                                        margin: 3rem auto;
	                                        max-width: 200px;
	                                    }

	                                    /* Code blocks on landing */
	                                    .landing .landing-content > pre {
	                                        max-width: 720px;
	                                        margin: 2rem auto;
	                                        border-radius: var(--radius);
	                                    }

	                                    /* General landing text */
	                                    .landing .landing-content > p {
	                                        text-align: center;
	                                        max-width: 640px;
	                                        margin-left: auto;
	                                        margin-right: auto;
	                                        color: var(--color-text-secondary);
	                                    }

	                                    /* Landing code blocks - reuse page-content pre styles */
	                                    .landing-content pre { position: relative; margin: 2rem auto; padding: 1.25rem 1.5rem; border-radius: var(--radius); overflow-x: auto; border: 1px solid var(--color-border); max-width: 720px; background: var(--color-bg-secondary); }
	                                    .landing-content pre code { font-family: var(--font-mono); font-size: 0.875rem; line-height: 1.7; background: none; padding: 0; border: none; }
	                                    .landing-content pre .copy-btn { position: absolute; top: 0.5rem; right: 0.5rem; background: rgba(255,255,255,0.15); border: 1px solid rgba(255,255,255,0.25); color: rgba(255,255,255,0.75); padding: 0.25rem 0.625rem; border-radius: var(--radius-sm); font-size: 0.75rem; font-family: var(--font-mono); cursor: pointer; opacity: 0; transition: opacity 0.15s ease, background var(--transition), color var(--transition), border-color var(--transition); }
	                                    .landing-content pre .copy-btn:hover { background: rgba(255,255,255,0.25); color: #fff; border-color: rgba(255,255,255,0.4); }
	                                    .landing-content pre:hover .copy-btn, .landing-content pre .copy-btn:focus-visible { opacity: 1; }
	                                    /* Same corner as the Copy button: hide at once, return after the button fades out. */
	                                    .landing-content pre:hover .code-lang, .landing-content pre:focus-within .code-lang { opacity: 0; transition: none; }
	                                    @media (hover: none) { .landing-content pre .copy-btn { opacity: 1; } .landing-content pre .code-lang { opacity: 0; } }
	                                    .landing-content pre .code-lang { position: absolute; top: 0.5rem; right: 0.5rem; font-size: 0.6875rem; font-family: var(--font-mono); text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-muted); user-select: none; transition: opacity 0.15s ease 0.15s; }
	                                    .landing-content pre.has-line-numbers { padding-left: 3.5rem; }
	                                    .landing-content pre .line-numbers { position: absolute; top: 1.25rem; left: 0; width: 2.75rem; text-align: right; padding-right: 0.75rem; font-family: var(--font-mono); font-size: 0.875rem; line-height: 1.7; color: var(--color-text-secondary); border-right: 1px solid var(--color-border); user-select: none; }
	                                    .landing-content pre .line-numbers span { display: block; }

	                                    @media (max-width: 768px) {
	                                        .landing .landing-content { padding: 0 1rem; }
	                                        .landing .landing-content > h1:first-of-type { margin-top: 2rem; }
	                                        .landing .landing-content > ul { grid-template-columns: 1fr; }
	                                    }

	                                    /* Version selector */
	                                    .version-selector { position: relative; }
	                                    .version-trigger {
	                                        display: flex; align-items: center; gap: 0.375rem;
	                                        padding: 0.375rem 0.75rem;
	                                        border: 1px solid var(--color-border); border-radius: var(--radius);
	                                        background: var(--color-bg); color: var(--color-text);
	                                        font-size: 0.8125rem; font-weight: 500;
	                                        cursor: pointer; transition: all 150ms ease;
	                                        white-space: nowrap;
	                                    }
	                                    .version-trigger:hover { border-color: var(--color-primary); }
	                                    .version-trigger[aria-expanded="true"] { border-color: var(--color-primary); box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 20%, transparent); }
	                                    .version-chevron { transition: transform 150ms ease; }
	                                    .version-trigger[aria-expanded="true"] .version-chevron { transform: rotate(180deg); }
	                                    .version-dropdown {
	                                        position: absolute; top: calc(100% + 0.375rem); right: 0;
	                                        min-width: 180px; max-height: 280px; overflow-y: auto;
	                                        background: var(--color-bg); border: 1px solid var(--color-border);
	                                        border-radius: var(--radius); box-shadow: 0 8px 24px rgba(0,0,0,0.12);
	                                        list-style: none; margin: 0; padding: 0.25rem;
	                                        z-index: 100;
	                                    }
	                                    .version-dropdown[hidden] { display: none; }
	                                    .version-option {
	                                        display: flex; align-items: center; gap: 0.5rem;
	                                        padding: 0.5rem 0.75rem; border-radius: calc(var(--radius) - 2px);
	                                        color: var(--color-text); text-decoration: none;
	                                        font-size: 0.8125rem; transition: background 100ms ease;
	                                    }
	                                    .version-option:hover { background: var(--color-bg-secondary); color: var(--color-text); }
	                                    .version-option.active { color: var(--color-primary-text); font-weight: 600; }
	                                    .version-badge {
	                                        font-size: 0.6875rem; font-weight: 600; text-transform: uppercase;
	                                        padding: 0.0625rem 0.375rem; border-radius: 9999px;
	                                        letter-spacing: 0.02em;
	                                    }
	                                    .version-badge-default { background: color-mix(in srgb, var(--color-primary) 15%, transparent); color: var(--color-primary); }
	                                    .version-badge-pre { background: color-mix(in srgb, #f59e0b 15%, transparent); color: #d97706; }
	                                    @media (max-width: 768px) {
	                                        .version-trigger .version-label { display: none; }
	                                        .version-dropdown { right: auto; left: 0; }
	                                    }

	                                    /* Version selector text contrast: the raw primary and amber are under 4.5:1 on white for most presets */
	                                    .version-option.active, .version-badge-default { color: color-mix(in srgb, var(--color-primary) 56%, #000); }
	                                    [data-theme="dark"] .version-option.active, [data-theme="dark"] .version-badge-default { color: color-mix(in srgb, var(--color-primary) 70%, #fff); }
	                                    .version-badge-pre { color: #92400e; }
	                                    [data-theme="dark"] .version-badge-pre { color: #fcd34d; }

	                                    /* Mermaid diagrams */
	                                    .mermaid {
	                                        display: flex;
	                                        justify-content: center;
	                                        margin: 1.5rem 0;
	                                        background: transparent;
	                                        border: none;
	                                        overflow-x: auto;
	                                    }

	                                    /* ================================ */
	                                    /* Animations & Visual Polish       */
	                                    /* ================================ */

	                                    /* --- Keyframes --- */
	                                    @keyframes heroGradientShift {
	                                        0%, 100% { background-position: 0% 50%; }
	                                        50% { background-position: 100% 50%; }
	                                    }
	                                    @keyframes heroFloat {
	                                        0%, 100% { transform: translateY(0); }
	                                        50% { transform: translateY(-8px); }
	                                    }
	                                    @keyframes fadeInUp {
	                                        from { opacity: 0; transform: translateY(20px); }
	                                        to { opacity: 1; transform: translateY(0); }
	                                    }
	                                    @keyframes fadeIn {
	                                        from { opacity: 0; }
	                                        to { opacity: 1; }
	                                    }
	                                    @keyframes slideInLeft {
	                                        from { width: 0; }
	                                        to { width: 2px; }
	                                    }
	                                    @keyframes slideInFromBottom {
	                                        from { opacity: 0; transform: translateY(12px); }
	                                        to { opacity: 1; transform: translateY(0); }
	                                    }

	                                    /* --- 1. Landing Page: Hero gradient animation --- */
	                                    .landing-hero::before {
	                                        background: linear-gradient(135deg,
	                                            color-mix(in srgb, var(--color-primary) 8%, transparent) 0%,
	                                            color-mix(in srgb, #7c3aed 6%, transparent) 25%,
	                                            color-mix(in srgb, #2563eb 5%, transparent) 50%,
	                                            color-mix(in srgb, var(--color-primary) 7%, transparent) 75%,
	                                            color-mix(in srgb, #06b6d4 5%, transparent) 100%) !important;
	                                        background-size: 300% 300% !important;
	                                        animation: heroGradientShift 12s ease infinite;
	                                    }

	                                    /* Hero dot/grid pattern overlay */
	                                    .landing-hero .hero-grid-pattern {
	                                        position: absolute;
	                                        inset: 0;
	                                        background-image: radial-gradient(circle, color-mix(in srgb, var(--color-primary) 15%, transparent) 1px, transparent 1px);
	                                        background-size: 28px 28px;
	                                        pointer-events: none;
	                                        z-index: 0;
	                                        opacity: 0.5;
	                                    }
	                                    [data-theme="dark"] .landing-hero .hero-grid-pattern {
	                                        opacity: 0.3;
	                                    }

	                                    /* Floating/pulsing hero icon */
	                                    .landing-hero-icon {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 72px;
	                                        height: 72px;
	                                        margin-bottom: 1.5rem;
	                                        border-radius: 18px;
	                                        background: color-mix(in srgb, var(--color-primary) 12%, transparent);
	                                        color: var(--color-primary);
	                                        font-size: 2rem;
	                                        animation: heroFloat 4s ease-in-out infinite;
	                                    }

	                                    /* Staggered fade-in for hero elements */
	                                    .landing-hero-title {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.1s;
	                                    }
	                                    .landing-hero-subtitle {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.25s;
	                                    }
	                                    .landing-hero-actions {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.4s;
	                                    }

	                                    /* Feature card hover lift */
	                                    .landing-feature-card {
	                                        transition: border-color 200ms ease, box-shadow 300ms ease, transform 300ms ease !important;
	                                    }
	                                    .landing-feature-card:hover {
	                                        transform: translateY(-5px) !important;
	                                        box-shadow: 0 12px 36px color-mix(in srgb, var(--color-primary) 14%, transparent) !important;
	                                    }

	                                    /* --- 2. Page transitions --- */
	                                    .page-content {
	                                        animation: fadeIn 0.4s ease both;
	                                    }
	                                    .page-content h1,
	                                    .page-content h2,
	                                    .page-content h3 {
	                                        animation: slideInFromBottom 0.5s ease both;
	                                    }

	                                    /* --- 3. Typography improvements --- */
	                                    .page-content h1 {
	                                        font-size: 2.25rem !important;
	                                        font-weight: 800 !important;
	                                        letter-spacing: -0.03em !important;
	                                        line-height: 1.15 !important;
	                                        margin-bottom: 1.25rem !important;
	                                        background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-light) 100%);
	                                        -webkit-background-clip: text;
	                                        -webkit-text-fill-color: transparent;
	                                        background-clip: text;
	                                    }
	                                    .page-content h2 {
	                                        font-size: 1.6rem !important;
	                                        letter-spacing: -0.02em;
	                                        line-height: 1.25;
	                                    }
	                                    .page-content h3 {
	                                        font-size: 1.3rem !important;
	                                        letter-spacing: -0.015em;
	                                        line-height: 1.3;
	                                    }
	                                    .page-content p {
	                                        line-height: 1.8;
	                                        margin-bottom: 1.15rem;
	                                    }
	                                    .page-content li {
	                                        line-height: 1.75;
	                                    }

	                                    /* Better inline code styling */
	                                    .page-content code {
	                                        font-size: 0.84em !important;
	                                        padding: 0.15rem 0.45rem !important;
	                                        border-radius: 5px !important;
	                                        border: 1px solid color-mix(in srgb, var(--color-primary-dark) 12%, var(--color-border));
	                                        font-weight: 500;
	                                    }
	                                    .page-content pre code {
	                                        border: none !important;
	                                        font-weight: normal !important;
	                                        font-size: 0.875rem !important;
	                                        padding: 0 !important;
	                                        border-radius: 0 !important;
	                                        -webkit-text-fill-color: unset;
	                                        background-clip: unset;
	                                        -webkit-background-clip: unset;
	                                        background: none !important;
	                                    }
	                                    /* Ensure h1 gradient doesn't break in code inside headings */
	                                    .page-content h1 code {
	                                        -webkit-text-fill-color: var(--color-primary-dark);
	                                    }

	                                    /* --- 4. Sidebar polish --- */
	                                    .nav-children {
	                                        overflow: hidden;
	                                        transition: max-height 0.3s ease, opacity 0.25s ease;
	                                    }
	                                    .nav-children:not(.expanded) {
	                                        max-height: 0 !important;
	                                        opacity: 0;
	                                        display: block !important;
	                                    }
	                                    .nav-children.expanded {
	                                        max-height: none;
	                                        opacity: 1;
	                                    }
	                                    .nav-link {
	                                        transition: all 0.2s ease !important;
	                                        border-left: 2px solid transparent;
	                                        position: relative;
	                                    }
	                                    .nav-link.current {
	                                        border-left-color: var(--color-primary) !important;
	                                        transition: all 0.25s ease, border-left-color 0.3s ease !important;
	                                    }
	                                    .nav-link:hover {
	                                        padding-left: calc(0.75rem + 2px) !important;
	                                    }
	                                    .nav-chevron-btn {
	                                        transition: all 0.2s ease !important;
	                                    }

	                                    /* --- 5. Code block improvements --- */
	                                    .page-content pre .code-lang {
	                                        position: absolute !important;
	                                        top: 0 !important;
	                                        right: 0 !important;
	                                        left: auto !important;
	                                        font-size: 0.65rem !important;
	                                        font-family: var(--font-mono) !important;
	                                        text-transform: uppercase !important;
	                                        letter-spacing: 0.06em !important;
	                                        padding: 0.25rem 0.65rem !important;
	                                        border-radius: 0 var(--radius) 0 var(--radius) !important;
	                                        background: rgba(255,255,255,0.08) !important;
	                                        color: rgba(255,255,255,0.5) !important;
	                                        border-bottom: 1px solid rgba(255,255,255,0.06);
	                                        border-left: 1px solid rgba(255,255,255,0.06);
	                                        user-select: none;
	                                        transition: opacity 0.15s ease 0.15s;
	                                        font-weight: 500;
	                                    }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre .code-lang,
	                                    [data-code-theme="github-light"] .page-content pre .code-lang {
	                                        background: rgba(0,0,0,0.04) !important;
	                                        color: rgba(0,0,0,0.4) !important;
	                                        border-bottom-color: rgba(0,0,0,0.06);
	                                        border-left-color: rgba(0,0,0,0.06);
	                                    }

	                                    /* Better line number styling */
	                                    .page-content pre .line-numbers {
	                                        border-right-color: rgba(255,255,255,0.08) !important;
	                                        font-size: 0.8rem !important;
	                                    }
	                                    [data-code-theme="catppuccin-latte"] .page-content pre .line-numbers,
	                                    [data-code-theme="github-light"] .page-content pre .line-numbers {
	                                        border-right-color: rgba(0,0,0,0.08) !important;
	                                    }

	                                    /* Code block toolbar integrated look */
	                                    .page-content pre .copy-btn {
	                                        border-radius: var(--radius-sm) !important;
	                                        font-weight: 500 !important;
	                                        font-size: 0.7rem !important;
	                                        padding: 0.2rem 0.6rem !important;
	                                        letter-spacing: 0.02em;
	                                        transition: opacity 0.15s ease, background 0.2s ease, color 0.2s ease, border-color 0.2s ease !important;
	                                    }

	                                    /* --- 6. TOC scroll spy --- */
	                                    .toc-item a {
	                                        transition: all 0.25s ease !important;
	                                        border-left: 2px solid transparent !important;
	                                        margin-left: -2px;
	                                    }
	                                    .toc-item a.active {
	                                        color: var(--color-primary-text) !important;
	                                        font-weight: 600 !important;
	                                        border-left-color: var(--color-primary) !important;
	                                        background: color-mix(in srgb, var(--color-primary) 5%, transparent);
	                                    }

	                                    /* --- 7. Back to top button polish --- */
	                                    .back-to-top {
	                                        opacity: 0 !important;
	                                        transform: translateY(16px);
	                                        pointer-events: none;
	                                        transition: opacity 0.3s ease, transform 0.3s ease, background 0.2s ease, box-shadow 0.2s ease !important;
	                                    }
	                                    .back-to-top.visible {
	                                        opacity: 1 !important;
	                                        transform: translateY(0);
	                                        pointer-events: auto;
	                                    }
	                                    .back-to-top:hover {
	                                        background: var(--color-primary-dark) !important;
	                                        box-shadow: 0 6px 20px color-mix(in srgb, var(--color-primary) 40%, transparent) !important;
	                                        transform: translateY(-2px);
	                                    }

	                                    /* --- Landing page staggered content on markdown-based landing --- */
	                                    .landing .landing-content > h1:first-of-type {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.1s;
	                                    }
	                                    .landing .landing-content > h1:first-of-type + p {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.25s;
	                                    }
	                                    .landing .landing-content > h1:first-of-type + p + p,
	                                    .landing .landing-content > h1:first-of-type + p + ul {
	                                        animation: fadeInUp 0.7s ease both;
	                                        animation-delay: 0.4s;
	                                    }
	                                    .landing .landing-content > ul > li {
	                                        transition: border-color 200ms ease, box-shadow 300ms ease, transform 300ms ease !important;
	                                    }
	                                    .landing .landing-content > ul > li:hover {
	                                        transform: translateY(-5px) !important;
	                                        box-shadow: 0 12px 36px color-mix(in srgb, var(--color-primary) 14%, transparent) !important;
	                                    }

	                                    /* ── Reduced Motion ─────────────────────────────────────── */
	                                    /* Respects OS-level "reduce motion" + config showAnimations: false */
	                                    @media (prefers-reduced-motion: reduce) {
	                                        *, *::before, *::after {
	                                            animation-duration: 0.01ms !important;
	                                            animation-delay: 0s !important;
	                                            animation-iteration-count: 1 !important;
	                                            transition-duration: 0.01ms !important;
	                                            scroll-behavior: auto !important;
	                                        }
	                                    }
	                                    [data-no-animations] *, [data-no-animations] *::before, [data-no-animations] *::after {
	                                        animation-duration: 0.01ms !important;
	                                        animation-delay: 0s !important;
	                                        animation-iteration-count: 1 !important;
	                                        transition-duration: 0.01ms !important;
	                                        scroll-behavior: auto !important;
	                                    }
	                                    /* Landing page overrides */
	                                    body.landing { overflow-x: hidden; }
	                                    .landing-hero {
	                                        position: relative;
	                                        padding: 6rem 2rem 4rem;
	                                        text-align: center;
	                                        overflow: hidden;
	                                    }
	                                    .landing-hero::before {
	                                        content: '';
	                                        position: absolute;
	                                        inset: 0;
	                                        background: linear-gradient(135deg,
	                                            color-mix(in srgb, var(--color-primary) 6%, transparent) 0%,
	                                            color-mix(in srgb, #7c3aed 5%, transparent) 50%,
	                                            color-mix(in srgb, #2563eb 4%, transparent) 100%);
	                                        pointer-events: none;
	                                        z-index: 0;
	                                    }
	                                    .landing-hero::after {
	                                        content: '';
	                                        position: absolute;
	                                        top: -40%;
	                                        left: 50%;
	                                        transform: translateX(-50%);
	                                        width: min(900px, 200vw);
	                                        height: 600px;
	                                        background: radial-gradient(ellipse, color-mix(in srgb, var(--color-primary) 12%, transparent) 0%, transparent 70%);
	                                        pointer-events: none;
	                                        z-index: 0;
	                                    }
	                                    .landing-hero > * { position: relative; z-index: 1; }
	                                    .landing-hero-title {
	                                        font-size: clamp(2.75rem, 6vw, 4.5rem);
	                                        font-weight: 800;
	                                        letter-spacing: -0.03em;
	                                        line-height: 1.1;
	                                        margin: 0 auto 1.25rem;
	                                        max-width: 800px;
	                                        background: linear-gradient(135deg, var(--color-primary) 0%, var(--gradient-secondary) 50%, var(--gradient-tertiary) 100%);
	                                        -webkit-background-clip: text;
	                                        -webkit-text-fill-color: transparent;
	                                        background-clip: text;
	                                    }
	                                    .landing-hero-subtitle {
	                                        font-size: clamp(1.125rem, 2vw, 1.375rem);
	                                        color: var(--color-text-secondary);
	                                        max-width: 640px;
	                                        margin: 0 auto 2.5rem;
	                                        line-height: 1.6;
	                                    }
	                                    .landing-hero-actions {
	                                        display: flex;
	                                        gap: 1rem;
	                                        justify-content: center;
	                                        flex-wrap: wrap;
	                                    }
	                                    .landing-btn {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        gap: 0.5rem;
	                                        padding: 0.8rem 2rem;
	                                        border-radius: var(--radius);
	                                        font-weight: 600;
	                                        font-size: 1rem;
	                                        text-decoration: none;
	                                        transition: all 150ms ease;
	                                        border: none;
	                                        cursor: pointer;
	                                        min-width: 200px;
	                                    }
	                                    .landing-btn-primary {
	                                        background: var(--color-primary);
	                                        color: #fff;
	                                        box-shadow: 0 4px 14px color-mix(in srgb, var(--color-primary) 40%, transparent);
	                                    }
	                                    .landing-btn-primary:hover {
	                                        background: var(--color-primary-dark);
	                                        color: #fff;
	                                        transform: translateY(-2px);
	                                        box-shadow: 0 6px 20px color-mix(in srgb, var(--color-primary) 50%, transparent);
	                                    }
	                                    .landing-btn-secondary {
	                                        background: transparent;
	                                        color: var(--color-text);
	                                        border: 1px solid var(--color-border);
	                                        backdrop-filter: blur(4px);
	                                    }
	                                    .landing-btn-secondary:hover {
	                                        border-color: var(--color-primary);
	                                        color: var(--color-primary);
	                                        transform: translateY(-2px);
	                                    }
	                                    .landing-btn svg { width: 18px; height: 18px; }

	                                    /* Features grid */
	                                    .landing-features {
	                                        max-width: 1080px;
	                                        margin: 0 auto;
	                                        padding: 4rem 2rem;
	                                    }
	                                    .landing-features-title {
	                                        font-size: 1.75rem;
	                                        font-weight: 700;
	                                        text-align: center;
	                                        margin-bottom: 0.75rem;
	                                    }
	                                    .landing-features-subtitle {
	                                        text-align: center;
	                                        color: var(--color-text-secondary);
	                                        margin-bottom: 3rem;
	                                        font-size: 1.0625rem;
	                                    }
	                                    .landing-features-grid {
	                                        display: grid;
	                                        grid-template-columns: repeat(3, 1fr);
	                                        gap: 1.5rem;
	                                        list-style: none;
	                                        padding: 0;
	                                        margin: 0;
	                                    }
	                                    .landing-feature-card {
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        padding: 2rem 1.75rem;
	                                        transition: border-color 150ms ease, box-shadow 200ms ease, transform 200ms ease;
	                                        position: relative;
	                                        overflow: hidden;
	                                    }
	                                    .landing-feature-card::before {
	                                        content: '';
	                                        position: absolute;
	                                        top: 0;
	                                        left: 0;
	                                        right: 0;
	                                        height: 3px;
	                                        background: linear-gradient(90deg, var(--color-primary), var(--color-primary-light));
	                                        opacity: 0;
	                                        transition: opacity 200ms ease;
	                                    }
	                                    .landing-feature-card:hover {
	                                        border-color: color-mix(in srgb, var(--color-primary) 40%, var(--color-border));
	                                        box-shadow: 0 8px 30px color-mix(in srgb, var(--color-primary) 10%, transparent);
	                                        transform: translateY(-3px);
	                                    }
	                                    .landing-feature-card:hover::before { opacity: 1; }
	                                    a.landing-feature-card, a.landing-feature-card:hover { color: inherit; }
	                                    .landing-feature-icon {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 44px;
	                                        height: 44px;
	                                        border-radius: 10px;
	                                        background: color-mix(in srgb, var(--color-primary) 10%, transparent);
	                                        color: var(--color-primary);
	                                        font-size: 1.25rem;
	                                        font-weight: 700;
	                                        margin-bottom: 1rem;
	                                        font-family: var(--font-mono);
	                                    }
	                                    .landing-feature-icon svg { width: 22px; height: 22px; }
	                                    .landing-feature-name {
	                                        font-size: 1.0625rem;
	                                        font-weight: 650;
	                                        margin-bottom: 0.5rem;
	                                        color: var(--color-text);
	                                    }
	                                    .landing-feature-desc {
	                                        font-size: 0.9rem;
	                                        color: var(--color-text-secondary);
	                                        line-height: 1.6;
	                                        margin: 0;
	                                    }

	                                    /* Markdown content area below hero */
	                                    .landing-md-content {
	                                        max-width: 1200px;
	                                        margin: 0 auto;
	                                        padding: 0 2rem 3rem;
	                                    }
	                                    .landing-md-content h2 { text-align: center; font-size: 1.75rem; margin: 2.5rem 0 1rem; }
	                                    .landing-md-content table {
	                                        width: 100%; border-collapse: collapse; font-size: 0.875rem; margin: 1rem 0;
	                                        background: var(--color-bg); border-radius: var(--radius); overflow: hidden;
	                                        border: 1px solid var(--color-border);
	                                    }
	                                    .landing-md-content th { background: var(--color-bg-secondary); font-weight: 600; text-align: left; }
	                                    .landing-md-content th, .landing-md-content td { padding: 0.625rem 1rem; border: 1px solid var(--color-border); }
	                                    .landing-md-content tr:hover td { background: var(--color-bg-secondary); }

	                                    /* Landing footer */
	                                    .landing-footer {
	                                        text-align: center;
	                                        padding: 2.5rem 1rem;
	                                        border-top: 1px solid var(--color-border);
	                                        color: var(--color-text-muted);
	                                        font-size: 0.875rem;
	                                    }
	                                    .landing-footer-heart { color: var(--color-primary); }
	                                    .landing-footer a { color: var(--color-text-secondary); }
	                                    .landing-footer a:hover { color: var(--color-primary); }

	                                    /* Divider */
	                                    .landing-divider {
	                                        border: none;
	                                        border-top: 1px solid var(--color-border);
	                                        max-width: 200px;
	                                        margin: 0 auto;
	                                    }

	                                    /* Responsive */
	                                    @media (max-width: 900px) {
	                                        .landing-features-grid { grid-template-columns: repeat(2, 1fr); }
	                                    }
	                                    @media (max-width: 600px) {
	                                        .landing-hero { padding: 3rem 1rem 2.5rem; }
	                                        .landing-features { padding: 2rem 1rem; }
	                                        .landing-features-grid { grid-template-columns: 1fr; }
	                                        .landing-md-content { padding: 0 1rem 2rem; }
	                                    }

	                                    /* Blog: index cards, post header, tags and pager. Accent text is mixed toward black (light)
	                                       or white (dark) so every color preset keeps 4.5:1; the styles use their own --st-link. */
	                                    :root { --blog-link: color-mix(in srgb, var(--color-primary) 56%, #000000); }
	                                    [data-theme="dark"] { --blog-link: color-mix(in srgb, var(--color-primary) 70%, #ffffff); }
	                                    .blog-sr-only {
	                                        position: absolute;
	                                        width: 1px;
	                                        height: 1px;
	                                        padding: 0;
	                                        margin: -1px;
	                                        overflow: hidden;
	                                        clip: rect(0, 0, 0, 0);
	                                        white-space: nowrap;
	                                        border: 0;
	                                    }
	                                    .page-content .blog-page .blog-description { font-size: 1.0625rem; color: var(--color-text-secondary); margin: -0.25rem 0 1.75rem; }
	                                    .page-content .blog-page .blog-description a { color: var(--blog-link); font-weight: 500; }
	                                    .page-content .blog-page .blog-empty { color: var(--color-text-secondary); }
	                                    .blog-list { display: flex; flex-direction: column; gap: 1.25rem; margin: 0 0 2rem; }
	                                    .blog-card {
	                                        display: flex;
	                                        gap: 1.25rem;
	                                        padding: 1.25rem 1.5rem;
	                                        background: var(--color-bg);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        transition: border-color var(--transition), box-shadow var(--transition);
	                                    }
	                                    .blog-card:hover { border-color: color-mix(in srgb, var(--color-primary) 45%, var(--color-border)); box-shadow: var(--shadow-md); }
	                                    .blog-card-image { flex: 0 0 168px; display: block; }
	                                    .page-content .blog-card-image img { display: block; width: 168px; height: 112px; object-fit: cover; border-radius: var(--radius-sm); }
	                                    .blog-card-body { flex: 1; min-width: 0; }
	                                    .page-content .blog-card .blog-card-title {
	                                        font-size: 1.25rem !important;
	                                        line-height: 1.35;
	                                        margin: 0 0 0.375rem;
	                                        padding: 0;
	                                        border: none;
	                                        animation: none;
	                                    }
	                                    .page-content .blog-card-title a { color: var(--color-text); }
	                                    .page-content .blog-card-title a:hover { color: var(--blog-link); }
	                                    .blog-meta {
	                                        display: flex;
	                                        flex-wrap: wrap;
	                                        align-items: center;
	                                        gap: 0.25rem 0.5rem;
	                                        font-size: 0.875rem;
	                                        line-height: 1.5;
	                                        color: var(--color-text-secondary);
	                                    }
	                                    .blog-meta-sep { color: var(--color-text-secondary); }
	                                    .blog-authors { display: flex; flex-wrap: wrap; align-items: center; gap: 0.25rem 0.375rem; margin-top: 0.375rem; font-size: 0.875rem; line-height: 1.5; color: var(--color-text-secondary); }
	                                    .blog-author { display: inline-flex; align-items: center; }
	                                    .blog-authors .blog-author a, .blog-author-name { color: var(--color-text); font-weight: 500; }
	                                    .blog-authors .blog-author a:hover { color: var(--blog-link); }
	                                    .blog-author-sep { margin-left: 0.375rem; }
	                                    .page-content .blog-author-avatar { width: 24px; height: 24px; margin-right: 0.375rem; border-radius: 50%; object-fit: cover; }
	                                    .blog-draft {
	                                        padding: 0 0.5rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: 999px;
	                                        font-size: 0.75rem;
	                                        font-weight: 600;
	                                        color: var(--color-text);
	                                    }
	                                    .page-content .blog-card .blog-card-summary { margin: 0.625rem 0 0; color: var(--color-text); line-height: 1.65; }
	                                    .page-content .blog-tags { display: flex; flex-wrap: wrap; gap: 0.375rem; list-style: none; padding: 0; margin: 0.75rem 0 0; }
	                                    .page-content .blog-tags li { margin: 0; line-height: 1.4; }
	                                    .blog-tag {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        padding: 0.125rem 0.625rem;
	                                        border: 1px solid color-mix(in srgb, var(--color-primary) 28%, var(--color-border));
	                                        border-radius: 999px;
	                                        background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg));
	                                        font-size: 0.8125rem;
	                                        font-weight: 500;
	                                        color: var(--blog-link);
	                                        transition: background var(--transition), border-color var(--transition);
	                                    }
	                                    .blog-tag:hover { color: var(--blog-link); background: color-mix(in srgb, var(--color-primary) 16%, var(--color-bg)); border-color: var(--blog-link); }
	                                    .blog-tag-count { font-size: 0.75rem; font-weight: 700; color: inherit; }
	                                    .page-content .blog-tags-all { gap: 0.5rem; margin-top: 0.5rem; }
	                                    .blog-read-more {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.25rem;
	                                        margin-top: 0.875rem;
	                                        font-size: 0.875rem;
	                                        font-weight: 600;
	                                        color: var(--blog-link);
	                                    }
	                                    .blog-read-more:hover { color: var(--blog-link); text-decoration: underline; text-underline-offset: 0.18em; }
	                                    .blog-pager-arrow { flex-shrink: 0; transition: transform var(--transition); }
	                                    .blog-read-more:hover .blog-pager-arrow, .blog-pager-older:hover .blog-pager-arrow { transform: translateX(2px); }
	                                    .blog-pager-newer:hover .blog-pager-arrow { transform: translateX(-2px); }
	                                    .blog-post-header { margin: 0 0 2rem; padding-bottom: 1.25rem; border-bottom: 1px solid var(--color-border); }
	                                    .page-content .blog-post-header .blog-tags { margin-top: 0.625rem; }
	                                    .page-content .blog-cover { display: block; width: 100%; max-height: 380px; object-fit: cover; margin-top: 1.25rem; }
	                                    .blog-pager {
	                                        display: grid;
	                                        grid-template-columns: 1fr auto 1fr;
	                                        align-items: center;
	                                        gap: 0.75rem 1rem;
	                                        margin: 0 0 1rem;
	                                    }
	                                    .blog-pager-newer { justify-self: start; }
	                                    .blog-pager-older { justify-self: end; }
	                                    .blog-pager-status { font-size: 0.875rem; color: var(--color-text-secondary); text-align: center; }
	                                    .blog-pager-link {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        padding: 0.5rem 1rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        font-size: 0.875rem;
	                                        font-weight: 600;
	                                        color: var(--blog-link);
	                                        transition: border-color var(--transition), background var(--transition);
	                                    }
	                                    .blog-pager-link:hover { color: var(--blog-link); border-color: var(--blog-link); background: color-mix(in srgb, var(--color-primary) 5%, var(--color-bg)); }
	                                    @media (max-width: 640px) {
	                                        .blog-card { flex-direction: column; gap: 0.875rem; padding: 1rem; }
	                                        .blog-card-image { flex-basis: auto; }
	                                        .page-content .blog-card-image img { width: 100%; height: 168px; }
	                                        .blog-pager { grid-template-columns: 1fr 1fr; }
	                                        .blog-pager-status { grid-column: 1 / -1; order: -1; }
	                                    }
	                                    """;

	#endregion

	#region Embedded Style CSS

	private const string _stylesCss = """

	                                  /* =============================== */
	                                  /* Styles (html[data-style])       */
	                                  /* =============================== */

	                                  /* Style selector */
	                                  .style-selector { position: relative; }
	                                  .style-trigger {
	                                      display: flex;
	                                      align-items: center;
	                                      justify-content: center;
	                                      width: 36px;
	                                      height: 36px;
	                                      border: 1px solid var(--color-border);
	                                      border-radius: var(--radius);
	                                      background: transparent;
	                                      color: var(--color-text-secondary);
	                                      cursor: pointer;
	                                      transition: all var(--transition);
	                                  }
	                                  .style-trigger:hover {
	                                      background: var(--color-bg-secondary);
	                                      color: var(--color-text);
	                                  }
	                                  .style-trigger[aria-expanded="true"] {
	                                      border-color: var(--color-primary);
	                                      color: var(--color-primary);
	                                  }
	                                  .style-dropdown {
	                                      position: absolute;
	                                      top: calc(100% + 8px);
	                                      right: 0;
	                                      display: flex;
	                                      flex-direction: column;
	                                      gap: 2px;
	                                      padding: 6px;
	                                      background: var(--color-bg);
	                                      border: 1px solid var(--color-border);
	                                      border-radius: var(--radius);
	                                      box-shadow: var(--shadow-lg);
	                                      z-index: 200;
	                                      min-width: 220px;
	                                  }
	                                  .style-dropdown[hidden] { display: none; }
	                                  .style-option {
	                                      display: flex;
	                                      align-items: center;
	                                      gap: 10px;
	                                      padding: 7px 10px;
	                                      border: none;
	                                      border-radius: calc(var(--radius) - 2px);
	                                      background: transparent;
	                                      color: var(--color-text);
	                                      cursor: pointer;
	                                      transition: all var(--transition);
	                                      font-family: inherit;
	                                      font-size: 0.82rem;
	                                      width: 100%;
	                                      text-align: left;
	                                  }
	                                  .style-option:hover, .style-option:focus-visible { background: var(--color-bg-secondary); }
	                                  .style-option.active { font-weight: 600; }
	                                  .style-text { display: flex; flex-direction: column; flex: 1; line-height: 1.3; }
	                                  .style-desc { font-size: 0.72rem; font-weight: 400; color: var(--color-text-secondary); }
	                                  .style-check {
	                                      opacity: 0;
	                                      font-size: 0.75rem;
	                                      color: var(--color-text);
	                                      transition: opacity var(--transition);
	                                  }
	                                  .style-option.active .style-check { opacity: 1; }
	                                  .style-preview {
	                                      width: 24px;
	                                      height: 18px;
	                                      flex-shrink: 0;
	                                      border-radius: 3px;
	                                      border: 1px solid #cbd5e1;
	                                      background: #ffffff;
	                                  }
	                                  .style-preview-default { background: linear-gradient(#0ea5e9 0 4px, #f8fafc 4px); }
	                                  .style-preview-moka {
	                                      background: #060608;
	                                      border-color: color-mix(in srgb, var(--color-primary) 60%, transparent);
	                                      box-shadow: inset 0 0 6px color-mix(in srgb, var(--color-primary) 40%, transparent);
	                                  }
	                                  .style-preview-minimal { background: linear-gradient(90deg, transparent 6px, #d4d4d8 6px 7px, transparent 7px), #ffffff; border-color: #e4e4e7; }
	                                  .style-preview-classic { background: linear-gradient(#1f2937 0 5px, #ffffff 5px); border-color: #9ca3af; border-radius: 1px; }
	                                  .style-preview-soft { background: #eef2ff; border-color: transparent; border-radius: 7px; box-shadow: 0 2px 5px rgba(15,23,42,0.22); }
	                                  @media (max-width: 768px) {
	                                      .style-selector { display: none; }
	                                  }

	                                  /* Styles: shared tokens. Accent text and fills are mixed toward black (light) or white
	                                     (dark) so every color preset keeps 4.5:1 against the style's surfaces. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) {
	                                      --st-ink: #000000;
	                                      --st-hue: 56%;
	                                      --st-link: color-mix(in srgb, var(--color-primary) var(--st-hue), var(--st-ink));
	                                      --st-on-fill: #ffffff;
	                                  }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"])[data-theme="dark"] {
	                                      --st-ink: #ffffff;
	                                      --st-hue: 70%;
	                                      --st-on-fill: #0b0b0f;
	                                      color-scheme: dark;
	                                  }
	                                  :where(html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"])) a,
	                                  :where(html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"])) a:hover { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content a:not([class]):hover { text-decoration: underline; text-underline-offset: 0.18em; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :focus-visible { outline: 2px solid var(--st-link); outline-offset: 2px; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content :not(pre) > code,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .nav-link.current,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .nav-header .nav-link:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .nav-group-label.parent-active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-member-link,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-nav-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .search-result-section,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .version-option.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .version-badge-default,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-theme-option.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-style-option.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .style-option.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-theme-check,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-style-check,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .style-check,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .site-logo-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .component-card-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-feature-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-hero-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-footer-heart,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing .landing-content > ul > li strong,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .blazor-preview-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .blazor-preview-error { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .breadcrumbs a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .edit-link a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .toc-back-to-top:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .heading-anchor:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .footer-social a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-footer a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .source-viewer summary:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .install-nuget-link:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .site-logo:hover { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .toc-item a.active { color: var(--st-link) !important; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .tab-header.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .install-tab.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .blazor-preview-tab.active { color: var(--st-link); border-bottom-color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .color-theme-trigger[aria-expanded="true"],
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-theme-trigger[aria-expanded="true"],
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .code-style-trigger[aria-expanded="true"],
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .style-trigger[aria-expanded="true"],
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .version-trigger[aria-expanded="true"] { color: var(--st-link); border-color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content h1,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-hero-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing .landing-content > h1:first-of-type {
	                                      background: none;
	                                      -webkit-text-fill-color: currentColor;
	                                      color: var(--color-text);
	                                  }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content h1 code { -webkit-text-fill-color: currentColor; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .component-step-number,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .toc-toggle-btn,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .back-to-top,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-btn-primary,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing-btn-primary:hover { background: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .back-to-top:hover { background: var(--st-link) !important; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .install-copy-btn:hover { background: var(--st-link); border-color: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing .landing-content > h1:first-of-type + p + p a:first-child,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .landing .landing-content > h1:first-of-type + p + ul a:first-of-type { background: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-endpoint-card h2.openapi-endpoint-header { margin: 0 0 0.75rem; padding: 0; border: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content .table-responsive table { margin: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-endpoint-card > :last-child { margin-bottom: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .breadcrumbs ol { padding-left: 0; margin-bottom: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .search-input:focus-visible { outline: none; box-shadow: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .search-input-wrap:focus-within { box-shadow: inset 0 -1px 0 var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .search-result-snippet mark { color: var(--color-text); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) ::selection { background: color-mix(in srgb, var(--color-primary) 28%, transparent); }

	                                  /* Styles: semantic colors (badges, admonitions, REST methods), mixed the same way */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge-sm { --st-c: var(--color-text-secondary); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-class, .admonition-note, .openapi-method-post, .openapi-status-info) { --st-c: #3b82f6; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-struct, .admonition-tip, .openapi-method-get, .openapi-status-success, .feedback-btn-yes, .repl-run-btn) { --st-c: #22c55e; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-record, .admonition-info, .admonition-important, .openapi-method-patch) { --st-c: #8b5cf6; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-interface, .api-badge-virtual) { --st-c: #06b6d4; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-enum, .admonition-warning, .admonition-caution, .openapi-method-put, .openapi-status-redirect, .openapi-auth-label, .version-badge-pre) { --st-c: #f59e0b; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge-delegate { --st-c: #ec4899; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-static, .openapi-method-head, .openapi-method-options, .openapi-optional) { --st-c: #64748b; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge-abstract { --st-c: #a78bfa; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge-sealed { --st-c: #f97316; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) :is(.api-badge-obsolete, .admonition-danger, .openapi-method-delete, .openapi-status-client-error, .openapi-status-server-error, .openapi-required, .feedback-btn-no) { --st-c: #ef4444; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .api-badge-sm,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .page-content .admonition-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-status,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-required,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-optional,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-auth-label,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .version-badge-pre { color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink)); }
	                                  /* The changelog plugin follows the operating system's color scheme; within a style it follows the page. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) {
	                                      --cl-bg: var(--color-bg);
	                                      --cl-bg-alt: var(--color-bg-secondary);
	                                      --cl-text: var(--color-text);
	                                      --cl-text-muted: var(--color-text-secondary);
	                                      --cl-border: var(--color-border);
	                                      --cl-line: var(--color-border);
	                                      --cl-dot-ring: var(--color-bg);
	                                      --cl-shadow: var(--shadow-sm);
	                                      --cl-shadow-hover: var(--shadow-md);
	                                      --cl-radius: var(--radius);
	                                      --cl-filter-bg: var(--color-bg-secondary);
	                                      --cl-filter-active: var(--color-text);
	                                      --cl-filter-active-text: var(--color-bg);
	                                      --cl-code-bg: var(--color-bg-code);
	                                      --cl-code-text: var(--color-text);
	                                      --cl-major: color-mix(in srgb, #ef4444 var(--st-hue), var(--st-ink));
	                                      --cl-minor: color-mix(in srgb, #3b82f6 var(--st-hue), var(--st-ink));
	                                      --cl-patch: color-mix(in srgb, #22c55e var(--st-hue), var(--st-ink));
	                                      --cl-initial: color-mix(in srgb, #8b5cf6 var(--st-hue), var(--st-ink));
	                                      --cl-added: color-mix(in srgb, #22c55e var(--st-hue), var(--st-ink));
	                                      --cl-changed: color-mix(in srgb, #3b82f6 var(--st-hue), var(--st-ink));
	                                      --cl-fixed: color-mix(in srgb, #f97316 var(--st-hue), var(--st-ink));
	                                      --cl-breaking: color-mix(in srgb, #ef4444 var(--st-hue), var(--st-ink));
	                                      --cl-deprecated: color-mix(in srgb, #f59e0b var(--st-hue), var(--st-ink));
	                                      --cl-removed: color-mix(in srgb, #ef4444 var(--st-hue), var(--st-ink));
	                                      --cl-security: color-mix(in srgb, #8b5cf6 var(--st-hue), var(--st-ink));
	                                  }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .changelog-badge-major { background: color-mix(in srgb, #ef4444 12%, var(--color-bg)); border-color: color-mix(in srgb, #ef4444 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .changelog-badge-minor { background: color-mix(in srgb, #3b82f6 12%, var(--color-bg)); border-color: color-mix(in srgb, #3b82f6 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .changelog-badge-patch { background: color-mix(in srgb, #22c55e 12%, var(--color-bg)); border-color: color-mix(in srgb, #22c55e 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .changelog-badge-initial { background: color-mix(in srgb, #8b5cf6 12%, var(--color-bg)); border-color: color-mix(in srgb, #8b5cf6 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .openapi-method,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .repl-run-btn { background: color-mix(in srgb, var(--st-c) 58%, #000000); color: #ffffff; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .repl-run-btn:hover { background: color-mix(in srgb, var(--st-c) 48%, #000000); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .feedback-btn-yes:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .feedback-btn-yes.selected,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .feedback-btn-no:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .feedback-btn-no.selected {
	                                      border-color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink));
	                                      color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink));
	                                      background: color-mix(in srgb, var(--st-c) 12%, var(--color-bg));
	                                  }

	                                  /* Style: Moka */
	                                  html[data-style="moka"] {
	                                      --color-bg: #fafafa;
	                                      --color-bg-secondary: #f3f3f5;
	                                      --color-bg-code: #f0f0f3;
	                                      --color-text: #16161a;
	                                      --color-text-secondary: #4a4a55;
	                                      --color-text-muted: #5c5c66;
	                                      --mk-surface: #ffffff;
	                                      --mk-hover: #ececf0;
	                                      --mk-surface-3: #e6e6ea;
	                                      --mk-border: color-mix(in srgb, var(--color-primary) 14%, transparent);
	                                      --mk-border-visible: color-mix(in srgb, var(--color-primary) 24%, transparent);
	                                      --mk-border-strong: color-mix(in srgb, var(--color-primary) 40%, transparent);
	                                      --mk-glow: color-mix(in srgb, var(--color-primary) 8%, transparent);
	                                      --mk-glow-md: color-mix(in srgb, var(--color-primary) 15%, transparent);
	                                      --mk-glow-strong: color-mix(in srgb, var(--color-primary) 25%, transparent);
	                                      --mk-backdrop: rgba(250, 250, 250, 0.85);
	                                      --color-border: var(--mk-border-visible);
	                                      --color-border-light: var(--mk-border);
	                                      --font-body: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
	                                      --font-mono: 'JetBrains Mono', 'Cascadia Code', 'SF Mono', Menlo, monospace;
	                                      --radius: 8px;
	                                      --radius-sm: 4px;
	                                      --radius-md: 10px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                      --transition: 150ms ease-out;
	                                      --header-height: 52px;
	                                      --sidebar-width: 256px;
	                                      --toc-width: 208px;
	                                      --content-max-width: 820px;
	                                  }
	                                  html[data-style="moka"][data-theme="dark"] {
	                                      --color-bg: #060608;
	                                      --color-bg-secondary: #0c0c10;
	                                      --color-bg-code: #101015;
	                                      --color-text: #e8e8ec;
	                                      --color-text-secondary: #a0a0aa;
	                                      --color-text-muted: #8a8a94;
	                                      --mk-surface: #0c0c10;
	                                      --mk-hover: #14141a;
	                                      --mk-surface-3: #1a1a22;
	                                      --mk-border: color-mix(in srgb, var(--color-primary) 6%, transparent);
	                                      --mk-border-visible: color-mix(in srgb, var(--color-primary) 12%, transparent);
	                                      --mk-border-strong: color-mix(in srgb, var(--color-primary) 24%, transparent);
	                                      --mk-backdrop: rgba(6, 6, 8, 0.85);
	                                  }
	                                  html[data-style="moka"] body { font-size: 14px; line-height: 1.6; }
	                                  html[data-style="moka"] :focus-visible {
	                                      outline: none;
	                                      box-shadow: 0 0 0 1px var(--st-link), 0 0 0 4px var(--mk-glow-md), inset 0 0 12px var(--mk-glow);
	                                  }
	                                  html[data-style="moka"] ::selection { background: var(--mk-glow-strong); }
	                                  html[data-style="moka"] :is(.sidebar, .toc-sidebar, .search-results, .version-dropdown, pre, .page-content table) { scrollbar-width: thin; scrollbar-color: var(--mk-surface-3) transparent; }
	                                  html[data-style="moka"] .site-header {
	                                      background: color-mix(in srgb, var(--color-bg) 88%, transparent);
	                                      border-bottom-color: var(--mk-border-visible);
	                                  }
	                                  html[data-style="moka"] .site-name { font-size: 0.9375rem; font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="moka"] .search-trigger {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      color: var(--color-text-muted);
	                                      font-family: var(--font-mono);
	                                      font-size: 11px;
	                                      padding: 0.375rem 0.625rem;
	                                      transition: border-color var(--transition), box-shadow var(--transition);
	                                  }
	                                  html[data-style="moka"] .search-trigger:hover { border-color: var(--mk-border-strong); box-shadow: inset 0 0 12px var(--mk-glow); }
	                                  html[data-style="moka"] .search-trigger kbd { background: transparent; border-color: var(--mk-border-visible); font-size: 10px; }
	                                  html[data-style="moka"] :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger) {
	                                      width: 32px;
	                                      height: 32px;
	                                      border-color: var(--mk-border-visible);
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="moka"] :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger):hover {
	                                      background: var(--mk-hover);
	                                      border-color: var(--mk-border-strong);
	                                      color: var(--color-text);
	                                  }
	                                  html[data-style="moka"] :is(.color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger, .version-trigger)[aria-expanded="true"] { box-shadow: inset 0 0 12px var(--mk-glow-md); }
	                                  html[data-style="moka"] :is(.color-theme-dropdown, .code-theme-dropdown, .code-style-dropdown, .style-dropdown, .version-dropdown) {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-strong);
	                                      border-radius: var(--radius-md);
	                                      box-shadow: none;
	                                  }
	                                  html[data-style="moka"] :is(.code-theme-option, .code-style-option, .style-option, .version-option) { border-radius: 6px; font-size: 13px; }
	                                  html[data-style="moka"] :is(.code-theme-option, .code-style-option, .style-option, .version-option):hover { background: var(--mk-hover); color: var(--color-text); }
	                                  html[data-style="moka"] :is(.code-theme-option, .code-style-option, .style-option, .version-option).active { box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .version-trigger {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      font-family: var(--font-mono);
	                                      font-size: 11px;
	                                      letter-spacing: 0.02em;
	                                  }
	                                  html[data-style="moka"] .version-trigger:hover { border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .version-option { font-family: var(--font-mono); font-size: 12px; }
	                                  html[data-style="moka"] .version-badge {
	                                      border-radius: var(--radius-sm);
	                                      font-family: var(--font-body);
	                                      font-size: 9px;
	                                      letter-spacing: 0.08em;
	                                      border: 1px solid color-mix(in srgb, currentColor 30%, transparent);
	                                  }
	                                  html[data-style="moka"] .sidebar { padding: 1.25rem 0.75rem; border-right-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .nav-section { margin-bottom: 0.375rem; }
	                                  html[data-style="moka"] .nav-link {
	                                      padding: 0.3125rem 0.625rem !important;
	                                      border: 1px solid transparent !important;
	                                      border-radius: 6px;
	                                      font-size: 13px;
	                                      color: var(--color-text-secondary);
	                                      transition: background var(--transition), border-color var(--transition), color var(--transition), box-shadow var(--transition) !important;
	                                  }
	                                  html[data-style="moka"] .nav-link:hover { background: var(--mk-hover); border-color: var(--mk-border-visible) !important; color: var(--color-text); }
	                                  html[data-style="moka"] .nav-link.current {
	                                      background: var(--mk-glow);
	                                      border-color: var(--mk-border-strong) !important;
	                                      box-shadow: inset 0 0 20px var(--mk-glow), 0 0 12px var(--mk-glow);
	                                      font-weight: 600;
	                                  }
	                                  html[data-style="moka"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) {
	                                      font-size: 10px;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                      color: var(--color-text-muted);
	                                  }
	                                  html[data-style="moka"] .nav-section > .nav-header :is(.nav-link.parent-active, .nav-group-label.parent-active) { color: var(--color-text); }
	                                  html[data-style="moka"] .nav-section > .nav-header .nav-link.current { color: var(--st-link); }
	                                  html[data-style="moka"] .nav-sub-section > .nav-header .nav-link { font-size: 13px; font-weight: 600; color: var(--color-text); }
	                                  html[data-style="moka"] .nav-chevron-btn { color: var(--color-text-muted); border-radius: var(--radius-sm); }
	                                  html[data-style="moka"] .nav-chevron-btn:hover { background: var(--mk-hover); color: var(--color-text); }
	                                  html[data-style="moka"] .nav-sub-section .nav-children { margin-left: 0.75rem; padding-left: 0.5rem; border-left: 1px solid var(--mk-border-visible); }
	                                  html[data-style="moka"] .content { padding: 1.75rem 2.25rem; }
	                                  html[data-style="moka"] .page-content h1 {
	                                      font-size: 1.75rem !important;
	                                      font-weight: 600 !important;
	                                      letter-spacing: -0.02em !important;
	                                      margin-bottom: 1rem !important;
	                                  }
	                                  html[data-style="moka"] .page-content h2 {
	                                      font-size: 1.25rem !important;
	                                      font-weight: 600;
	                                      letter-spacing: -0.01em;
	                                      margin: 2.25rem 0 0.75rem;
	                                      padding-bottom: 0.375rem;
	                                      border-bottom-color: var(--mk-border-visible);
	                                  }
	                                  html[data-style="moka"] .page-content h3 { font-size: 1.0625rem !important; font-weight: 600; margin: 1.75rem 0 0.5rem; }
	                                  html[data-style="moka"] .page-content h4 { font-size: 0.9375rem; }
	                                  html[data-style="moka"] .page-content p { line-height: 1.65; margin-bottom: 0.875rem; }
	                                  html[data-style="moka"] .page-content li { line-height: 1.6; }
	                                  html[data-style="moka"] .page-content :not(pre) > code {
	                                      background: var(--mk-glow);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-sm) !important;
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="moka"] .page-content pre { border-color: var(--mk-border-visible); border-radius: var(--radius); }
	                                  html[data-style="moka"][data-theme="dark"]:not([data-code-theme="catppuccin-latte"]):not([data-code-theme="github-light"]) :is(.page-content, .landing-content) pre { background: #0a0a0d; }
	                                  html[data-style="moka"] .page-content pre .line-numbers { border-right-color: var(--mk-border-visible) !important; }
	                                  html[data-style="moka"] .page-content pre .code-lang { letter-spacing: 0.08em !important; font-weight: 600; }
	                                  html[data-style="moka"] .page-content blockquote {
	                                      background: var(--mk-surface);
	                                      border-left: 2px solid var(--st-link);
	                                      border-radius: 0 var(--radius) var(--radius) 0;
	                                      box-shadow: inset 0 0 20px var(--mk-glow);
	                                  }
	                                  html[data-style="moka"] .page-content table { font-size: 13px; }
	                                  html[data-style="moka"] .page-content :is(th, td) { padding: 0.4375rem 0.75rem; border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .page-content th {
	                                      background: var(--mk-surface);
	                                      color: var(--color-text-muted);
	                                      font-size: 10px;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .page-content tr:hover td { background: var(--mk-hover); }
	                                  html[data-style="moka"] .api-member-table code { font-size: 12px; }
	                                  html[data-style="moka"] .api-namespace { font-family: var(--font-mono); font-size: 11px; letter-spacing: 0.02em; }
	                                  html[data-style="moka"] :is(.api-badge, .api-badge-sm) {
	                                      border: 1px solid color-mix(in srgb, currentColor 30%, transparent);
	                                      border-radius: var(--radius-sm);
	                                      font-size: 10px;
	                                      letter-spacing: 0.08em;
	                                  }
	                                  html[data-style="moka"] .api-badge-sm { font-size: 9px; }
	                                  html[data-style="moka"] .page-content .admonition {
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 24%, transparent);
	                                      border-left: 3px solid var(--st-c);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 6%, var(--color-bg));
	                                      padding: 0.875rem 1rem;
	                                  }
	                                  html[data-style="moka"] .page-content .admonition-title { font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .tabs { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .tab-headers { background: var(--mk-surface); border-bottom-color: var(--mk-border-visible); border-radius: var(--radius-md) var(--radius-md) 0 0; }
	                                  html[data-style="moka"] .tab-header { font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .tab-header:hover { background: var(--mk-hover); }
	                                  html[data-style="moka"] .tab-header.active { box-shadow: inset 0 -12px 16px -12px var(--mk-glow-strong); }
	                                  html[data-style="moka"] :is(.component-card, .component-link-card) {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-md);
	                                  }
	                                  html[data-style="moka"] .component-card { border-left-width: 1px; }
	                                  html[data-style="moka"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 3px; }
	                                  html[data-style="moka"] :is(.component-card, .component-link-card):hover { border-color: var(--mk-border-strong); box-shadow: inset 0 0 20px var(--mk-glow); background: var(--mk-hover); }
	                                  html[data-style="moka"] .component-card-title { font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-secondary); }
	                                  html[data-style="moka"] .component-step-number {
	                                      width: 28px;
	                                      height: 28px;
	                                      border-radius: var(--radius);
	                                      background: var(--mk-surface);
	                                      color: var(--st-link);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: inset 0 0 12px var(--mk-glow);
	                                      font-family: var(--font-mono);
	                                      font-size: 12px;
	                                  }
	                                  html[data-style="moka"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--mk-border-visible); }
	                                  html[data-style="moka"] .package-install-widget { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] :is(.install-header, .install-tabs) { background: var(--mk-surface); border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .install-package-name { font-family: var(--font-mono); font-size: 12px; }
	                                  html[data-style="moka"] .install-tab { font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .install-copy-btn { background: transparent; border-color: var(--mk-border-visible); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; font-weight: 600; }
	                                  html[data-style="moka"] .install-copy-btn:hover { background: var(--mk-glow); border-color: var(--mk-border-strong); color: var(--st-link); }
	                                  html[data-style="moka"] .source-viewer { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .source-viewer summary { font-family: var(--font-mono); font-size: 12px; }
	                                  html[data-style="moka"] .source-viewer[open] summary { border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-sidebar { padding: 1.25rem 0; border-left-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-title { font-size: 10px; letter-spacing: 0.08em; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .toc-list { border-left: 1px solid var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-item a {
	                                      font-size: 12px;
	                                      border-left-width: 1px !important;
	                                      margin-left: -1px;
	                                      color: var(--color-text-muted);
	                                      transition: color var(--transition), background var(--transition), box-shadow var(--transition) !important;
	                                  }
	                                  html[data-style="moka"] .toc-item a:hover { color: var(--color-text); background: var(--mk-hover); }
	                                  html[data-style="moka"] .toc-item a.active {
	                                      border-left-color: var(--st-link) !important;
	                                      background: var(--mk-glow);
	                                      box-shadow: inset 12px 0 16px -12px var(--mk-glow-strong);
	                                      font-weight: 500 !important;
	                                  }
	                                  html[data-style="moka"] .toc-progress { height: 1px; background: var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-progress-bar { background: var(--st-link); }
	                                  html[data-style="moka"] :is(.toc-counter, .toc-back-to-top) { font-family: var(--font-mono); font-size: 11px; }
	                                  html[data-style="moka"] .toc-counter { border-top-color: var(--mk-border); color: var(--color-text-muted); }
	                                  html[data-style="moka"] .breadcrumbs { font-family: var(--font-mono); font-size: 11px; letter-spacing: 0.02em; }
	                                  html[data-style="moka"] .breadcrumbs li:not(:last-child)::after { color: var(--mk-border-strong); opacity: 1; }
	                                  html[data-style="moka"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--mk-border-visible); font-size: 11px; }
	                                  html[data-style="moka"] .last-updated { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .page-nav-link {
	                                      padding: 0.75rem 1rem;
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-md);
	                                  }
	                                  html[data-style="moka"] .page-nav-link:hover { background: var(--mk-hover); border-color: var(--mk-border-strong); box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .page-nav-label { font-size: 10px; font-weight: 600; letter-spacing: 0.08em; }
	                                  html[data-style="moka"] .page-nav-title { font-size: 13px; }
	                                  html[data-style="moka"] .feedback-prompt { font-size: 10px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .feedback-btn {
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius);
	                                      font-size: 11px;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .feedback-btn:hover { background: var(--mk-hover); }
	                                  html[data-style="moka"] .feedback-btn.selected { box-shadow: inset 0 0 20px color-mix(in srgb, var(--st-c) 12%, transparent); }
	                                  html[data-style="moka"] .site-footer { border-top-color: var(--mk-border-visible); padding: 1rem 1.5rem; }
	                                  html[data-style="moka"] .footer-inner { font-size: 11px; }
	                                  html[data-style="moka"] .built-with { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .search-backdrop { background: var(--mk-backdrop); backdrop-filter: blur(8px); }
	                                  html[data-style="moka"] .search-dialog {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-strong);
	                                      border-radius: 12px;
	                                      box-shadow: 0 0 0 1px var(--mk-border);
	                                  }
	                                  html[data-style="moka"] .search-input-wrap { border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .search-input { font-size: 14px; }
	                                  html[data-style="moka"] .search-input::placeholder { font-family: var(--font-mono); font-size: 11px; }
	                                  html[data-style="moka"] :is(.search-esc, .search-result-category) { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .search-result-category { font-size: 10px; letter-spacing: 0.04em; text-transform: uppercase; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .search-result { border: 1px solid transparent; border-radius: 6px; }
	                                  html[data-style="moka"] :is(.search-result:hover, .search-result.active) { background: var(--mk-hover); border-color: var(--mk-border-visible); box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .back-to-top {
	                                      border-radius: var(--radius);
	                                      background: var(--mk-surface);
	                                      color: var(--st-link);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: none;
	                                  }
	                                  html[data-style="moka"] .back-to-top:hover { background: var(--mk-hover) !important; box-shadow: inset 0 0 12px var(--mk-glow-md) !important; transform: none; }
	                                  html[data-style="moka"] .toc-toggle-btn {
	                                      border-radius: var(--radius);
	                                      background: var(--mk-surface);
	                                      color: var(--st-link);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: none;
	                                  }
	                                  html[data-style="moka"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="moka"] .landing-hero .hero-grid-pattern {
	                                      background-image: linear-gradient(var(--mk-border-visible) 1px, transparent 1px), linear-gradient(90deg, var(--mk-border-visible) 1px, transparent 1px);
	                                      background-size: 32px 32px;
	                                      opacity: 1;
	                                      -webkit-mask-image: radial-gradient(ellipse at 50% 40%, #000 20%, transparent 70%);
	                                      mask-image: radial-gradient(ellipse at 50% 40%, #000 20%, transparent 70%);
	                                  }
	                                  html[data-style="moka"] .landing-hero-icon {
	                                      animation: none;
	                                      width: 64px;
	                                      height: 64px;
	                                      border-radius: 12px;
	                                      background: var(--mk-surface);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: inset 0 0 20px var(--mk-glow-md);
	                                  }
	                                  html[data-style="moka"] .landing-hero-title { font-weight: 600; letter-spacing: -0.035em; }
	                                  html[data-style="moka"] :is(.landing-hero-title, .landing-hero-subtitle, .landing-hero-actions) { animation-duration: 200ms; animation-delay: 0s; }
	                                  html[data-style="moka"] .landing-hero-subtitle { font-size: 1.0625rem; }
	                                  html[data-style="moka"] .landing-btn {
	                                      min-width: 0;
	                                      padding: 0.75rem 1.5rem;
	                                      border-radius: var(--radius);
	                                      font-size: 11px;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] :is(.landing-btn-primary, .landing-btn-primary:hover) {
	                                      background: transparent;
	                                      color: var(--st-link);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: none;
	                                      transform: none;
	                                  }
	                                  html[data-style="moka"] .landing-btn-primary:hover { background: var(--mk-glow); box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .landing-btn-primary:active { box-shadow: 0 0 24px var(--mk-glow-strong); }
	                                  html[data-style="moka"] .landing-btn-secondary { border-color: var(--mk-border-visible); color: var(--color-text-secondary); backdrop-filter: none; }
	                                  html[data-style="moka"] .landing-btn-secondary:hover { background: var(--mk-hover); border-color: var(--mk-border-strong); color: var(--color-text); transform: none; }
	                                  html[data-style="moka"] .landing-features-title { font-size: 1.375rem; font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="moka"] .landing-features-subtitle { font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .landing-features-grid { gap: 1rem; }
	                                  html[data-style="moka"] .landing-feature-card {
	                                      padding: 1.25rem;
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-md);
	                                  }
	                                  html[data-style="moka"] .landing-feature-card::before { height: 1px; background: var(--st-link); }
	                                  html[data-style="moka"] .landing-feature-card:hover {
	                                      transform: none !important;
	                                      background: var(--mk-hover);
	                                      border-color: var(--mk-border-strong);
	                                      box-shadow: inset 0 0 20px var(--mk-glow) !important;
	                                  }
	                                  html[data-style="moka"] .landing-feature-icon {
	                                      width: 36px;
	                                      height: 36px;
	                                      border-radius: var(--radius);
	                                      background: transparent;
	                                      border: 1px solid var(--mk-border-visible);
	                                      font-size: 0.875rem;
	                                  }
	                                  html[data-style="moka"] .landing-feature-name { font-size: 14px; font-weight: 600; }
	                                  html[data-style="moka"] .landing-feature-desc { font-size: 13px; }
	                                  html[data-style="moka"] :is(.landing-divider, .landing .landing-content > hr) { border-top-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-md-content :is(th, td) { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-md-content table { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing .landing-content > ul > li { background: var(--mk-surface); border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: inset 0 0 20px var(--mk-glow) !important; border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .landing-content pre { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-footer { border-top-color: var(--mk-border-visible); font-size: 11px; }
	                                  html[data-style="moka"] .repl-container { border-color: var(--mk-border-visible); border-radius: var(--radius-md); background: #0a0a0d; }
	                                  html[data-style="moka"] .repl-toolbar { background: #0c0c10; border-top-color: rgba(255, 255, 255, 0.08); }
	                                  html[data-style="moka"] .repl-run-btn {
	                                      background: transparent;
	                                      color: #4ade80;
	                                      border: 1px solid rgba(74, 222, 128, 0.35);
	                                      font-family: var(--font-mono);
	                                      font-size: 11px;
	                                      letter-spacing: 0.06em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .repl-run-btn:hover { background: rgba(74, 222, 128, 0.1); }
	                                  html[data-style="moka"] .repl-output { border-top-color: rgba(255, 255, 255, 0.08); background: #060608; }
	                                  html[data-style="moka"] .openapi-method {
	                                      background: color-mix(in srgb, var(--st-c) 12%, var(--color-bg));
	                                      color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink));
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 35%, transparent);
	                                      font-size: 11px;
	                                      letter-spacing: 0.06em;
	                                  }
	                                  html[data-style="moka"] .openapi-endpoint-card { border-color: var(--mk-border-visible); border-radius: var(--radius-md); background: var(--mk-surface); }
	                                  html[data-style="moka"] .openapi-endpoint-card h3 { border-bottom-color: var(--mk-border-visible); font-size: 11px !important; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-secondary); }
	                                  html[data-style="moka"] .openapi-path { font-family: var(--font-mono); font-size: 0.9375rem; }
	                                  html[data-style="moka"] :is(.openapi-required, .openapi-optional, .openapi-auth-label, .openapi-status) { border-radius: var(--radius-sm); font-family: var(--font-mono); }
	                                  html[data-style="moka"] .blazor-preview-container { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  @media (max-width: 1200px) {
	                                      html[data-style="moka"] .toc-sidebar { box-shadow: none; border-left-color: var(--mk-border-strong); }
	                                  }

	                                  /* Style: Minimal */
	                                  html[data-style="minimal"] {
	                                      --color-bg: #ffffff;
	                                      --color-bg-secondary: #f6f6f7;
	                                      --color-bg-code: #f6f6f7;
	                                      --color-text: #1c1c1f;
	                                      --color-text-secondary: #55555e;
	                                      --color-text-muted: #6b6b75;
	                                      --color-border: #e7e7ea;
	                                      --color-border-light: #f1f1f3;
	                                      --radius: 6px;
	                                      --radius-sm: 4px;
	                                      --radius-md: 6px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: 0 8px 28px rgba(0, 0, 0, 0.08);
	                                      --sidebar-width: 260px;
	                                      --toc-width: 220px;
	                                      --content-max-width: 70ch;
	                                  }
	                                  html[data-style="minimal"][data-theme="dark"] {
	                                      --color-bg: #111113;
	                                      --color-bg-secondary: #19191c;
	                                      --color-bg-code: #19191c;
	                                      --color-text: #ececef;
	                                      --color-text-secondary: #a8a8b2;
	                                      --color-text-muted: #8e8e98;
	                                      --color-border: #26262b;
	                                      --color-border-light: #1d1d21;
	                                      --shadow-lg: 0 8px 28px rgba(0, 0, 0, 0.5);
	                                  }
	                                  html[data-style="minimal"] body { line-height: 1.75; }
	                                  html[data-style="minimal"] .site-header { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="minimal"] .search-trigger { border-color: transparent; }
	                                  html[data-style="minimal"] :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger, .version-trigger) { border-color: transparent; }
	                                  html[data-style="minimal"] .version-trigger { background: transparent; }
	                                  html[data-style="minimal"] .sidebar { border-right: none; padding: 2rem 1rem 2rem 1.5rem; }
	                                  html[data-style="minimal"] .nav-section { margin-bottom: 0.5rem; }
	                                  html[data-style="minimal"] .nav-link {
	                                      padding: 0.3rem 0.75rem !important;
	                                      border-radius: 0;
	                                      background: none !important;
	                                      font-weight: 400;
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="minimal"] .nav-link:hover { color: var(--color-text); }
	                                  html[data-style="minimal"] .nav-link.current {
	                                      color: var(--color-text);
	                                      font-weight: 600;
	                                      border-left: 2px solid var(--st-link) !important;
	                                  }
	                                  html[data-style="minimal"] .nav-header .nav-link,
	                                  html[data-style="minimal"] .nav-group-label { font-weight: 600; color: var(--color-text); }
	                                  html[data-style="minimal"] .nav-header .nav-link:hover { color: var(--color-text-secondary); }
	                                  html[data-style="minimal"] .nav-group-label.parent-active { color: var(--color-text); }
	                                  html[data-style="minimal"] .nav-chevron-btn:hover { background: none; }
	                                  html[data-style="minimal"] .nav-children { padding-left: 0.75rem; }
	                                  html[data-style="minimal"] .content { padding: 3rem; }
	                                  html[data-style="minimal"] .page-content h1 { font-size: 2rem !important; font-weight: 700 !important; letter-spacing: -0.025em !important; margin-bottom: 1.5rem !important; }
	                                  html[data-style="minimal"] .page-content h2 {
	                                      font-size: 1.375rem !important;
	                                      font-weight: 600;
	                                      margin: 3rem 0 1rem;
	                                      padding-bottom: 0;
	                                      border-bottom: none;
	                                  }
	                                  html[data-style="minimal"] .page-content h3 { font-size: 1.125rem !important; margin: 2.25rem 0 0.75rem; }
	                                  html[data-style="minimal"] .page-content p { line-height: 1.8; margin-bottom: 1.25rem; }
	                                  html[data-style="minimal"] .page-content :not(pre) > code { color: var(--color-text); border-color: transparent; border-radius: var(--radius-sm) !important; }
	                                  html[data-style="minimal"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="minimal"] :is(.page-content, .landing-content) pre { border-color: transparent; }
	                                  html[data-style="minimal"] .page-content pre .line-numbers { border-right-color: transparent !important; }
	                                  html[data-style="minimal"] .page-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border); padding: 0.625rem 1rem 0.625rem 0; }
	                                  html[data-style="minimal"] .page-content th { background: none; color: var(--color-text-secondary); font-size: 0.8125rem; }
	                                  html[data-style="minimal"] .page-content tr:hover td { background: none; }
	                                  html[data-style="minimal"] .page-content blockquote { background: none; border-left: 2px solid var(--color-border); border-radius: 0; padding: 0 0 0 1.25rem; }
	                                  html[data-style="minimal"] .page-content .admonition {
	                                      background: none;
	                                      border: none;
	                                      border-left: 2px solid var(--st-c);
	                                      border-radius: 0;
	                                      padding: 0.125rem 0 0.125rem 1.25rem;
	                                  }
	                                  html[data-style="minimal"] :is(.tabs, .package-install-widget, .source-viewer) { border-color: transparent; border-radius: 0; }
	                                  html[data-style="minimal"] :is(.tab-headers, .install-header, .install-tabs) { background: none; border-radius: 0; border-bottom-color: var(--color-border); }
	                                  html[data-style="minimal"] .tab-content { padding: 1rem 0; }
	                                  html[data-style="minimal"] .component-code-group .tab-content { padding: 0; }
	                                  html[data-style="minimal"] .install-header { padding: 0.5rem 0; }
	                                  html[data-style="minimal"] .install-tabs { padding: 0; }
	                                  html[data-style="minimal"] .install-panel { border-radius: var(--radius); }
	                                  html[data-style="minimal"] .source-viewer summary { padding: 0.75rem 0; }
	                                  html[data-style="minimal"] .source-viewer pre { border-radius: var(--radius); }
	                                  html[data-style="minimal"] :is(.component-card, .component-link-card) { border-color: transparent; background: var(--color-bg-secondary); }
	                                  html[data-style="minimal"] :is(.component-card, .component-link-card):hover { border-color: transparent; box-shadow: none; }
	                                  html[data-style="minimal"] .component-link-card:hover { background: color-mix(in srgb, var(--color-text) 6%, var(--color-bg)); }
	                                  html[data-style="minimal"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 2px; border-radius: 0 var(--radius) var(--radius) 0; }
	                                  html[data-style="minimal"] .component-step-number { background: transparent; color: var(--color-text); border: 1px solid var(--color-border); font-weight: 500; }
	                                  html[data-style="minimal"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; }
	                                  html[data-style="minimal"] .toc-sidebar { border-left: none; }
	                                  html[data-style="minimal"] .toc-list { border-left: none; }
	                                  html[data-style="minimal"] .toc-item a { color: var(--color-text-muted); }
	                                  html[data-style="minimal"] .toc-item a:hover { color: var(--color-text); }
	                                  html[data-style="minimal"] .toc-item a.active { color: var(--color-text) !important; font-weight: 500 !important; background: none; border-left-color: var(--st-link) !important; }
	                                  html[data-style="minimal"] .toc-progress { height: 1px; }
	                                  html[data-style="minimal"] .toc-counter { border-top: none; }
	                                  html[data-style="minimal"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--color-border-light); }
	                                  html[data-style="minimal"] .page-nav-link { border-color: transparent; padding: 0.75rem 0; }
	                                  html[data-style="minimal"] .page-nav-link:hover { background: none; border-color: transparent; box-shadow: none; }
	                                  html[data-style="minimal"] .page-nav-link:hover .page-nav-title { text-decoration: underline; text-underline-offset: 0.18em; }
	                                  html[data-style="minimal"] .site-footer { border-top-color: var(--color-border-light); }
	                                  html[data-style="minimal"] .search-dialog { border-radius: 10px; }
	                                  html[data-style="minimal"] .search-backdrop { background: rgba(0, 0, 0, 0.3); backdrop-filter: none; }
	                                  html[data-style="minimal"] :is(.back-to-top, .toc-toggle-btn) { background: var(--color-bg); color: var(--color-text); border: 1px solid var(--color-border); box-shadow: none; }
	                                  html[data-style="minimal"] .back-to-top:hover { background: var(--color-bg-secondary) !important; box-shadow: none !important; }
	                                  html[data-style="minimal"] .landing::before,
	                                  html[data-style="minimal"] .landing-hero::before,
	                                  html[data-style="minimal"] .landing-hero::after { display: none; }
	                                  html[data-style="minimal"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="minimal"] .landing-hero-icon { background: none; animation: none; }
	                                  html[data-style="minimal"] .landing-hero-title { font-weight: 700; letter-spacing: -0.035em; }
	                                  html[data-style="minimal"] .landing-btn { border-radius: var(--radius); min-width: 0; padding: 0.75rem 1.5rem; }
	                                  html[data-style="minimal"] .landing-btn-primary { box-shadow: none; }
	                                  html[data-style="minimal"] .landing-btn-primary:hover { transform: none; box-shadow: none; background: var(--color-text); color: var(--color-bg); }
	                                  html[data-style="minimal"] .landing-btn-secondary:hover { transform: none; color: var(--color-text); border-color: var(--color-text-muted); }
	                                  html[data-style="minimal"] .landing-features-grid { gap: 2.5rem 2rem; }
	                                  html[data-style="minimal"] .landing-feature-card { border: none; background: transparent; padding: 0; }
	                                  html[data-style="minimal"] .landing-feature-card::before { display: none; }
	                                  html[data-style="minimal"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; }
	                                  html[data-style="minimal"] .landing-feature-icon { background: none; width: auto; height: auto; justify-content: flex-start; margin-bottom: 0.5rem; }
	                                  html[data-style="minimal"] .landing .landing-content > ul > li { border-color: transparent; background: var(--color-bg-secondary); }
	                                  html[data-style="minimal"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; border-color: transparent; }
	                                  html[data-style="minimal"] .landing-md-content table { border-color: transparent; }
	                                  html[data-style="minimal"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="minimal"] .landing-md-content th { background: none; }
	                                  html[data-style="minimal"] .landing-footer { border-top-color: var(--color-border-light); }
	                                  html[data-style="minimal"] .repl-container { border-color: transparent; border-radius: var(--radius); }
	                                  html[data-style="minimal"] .openapi-endpoint-card { border-color: transparent; border-top-color: var(--color-border); border-radius: 0; padding: 1.5rem 0; }
	                                  html[data-style="minimal"] .openapi-endpoint-card h3 { border-bottom: none; }
	                                  html[data-style="minimal"] .blazor-preview-container { border-color: var(--color-border-light); }
	                                  @media (max-width: 1200px) {
	                                      html[data-style="minimal"] .toc-sidebar { border-left: 1px solid var(--color-border); box-shadow: var(--shadow-lg); }
	                                  }

	                                  /* Style: Classic */
	                                  html[data-style="classic"] {
	                                      --color-bg: #ffffff;
	                                      --color-bg-secondary: #f4f5f7;
	                                      --color-bg-code: #f6f8fa;
	                                      --color-text: #1b1f24;
	                                      --color-text-secondary: #424a53;
	                                      --color-text-muted: #57606a;
	                                      --color-border: #d0d7de;
	                                      --color-border-light: #e5e8eb;
	                                      --cs-hover: #e9eaed;
	                                      --cs-zebra: #f7f7f8;
	                                      --cs-bar: var(--st-link);
	                                      --cs-on-bar: #ffffff;
	                                      --cs-on-bar-muted: #ffffff;
	                                      --cs-bar-line: rgba(0, 0, 0, 0.2);
	                                      --cs-bar-control: rgba(0, 0, 0, 0.2);
	                                      --cs-bar-control-hover: rgba(255, 255, 255, 0.2);
	                                      --cs-bar-control-border: rgba(255, 255, 255, 0.4);
	                                      --font-body: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, 'Noto Sans', sans-serif;
	                                      --font-mono: ui-monospace, SFMono-Regular, Consolas, 'Liberation Mono', Menlo, monospace;
	                                      --radius: 3px;
	                                      --radius-sm: 2px;
	                                      --radius-md: 4px;
	                                      --shadow-sm: 0 1px 2px rgba(31, 35, 40, 0.06);
	                                      --shadow-md: 0 2px 6px rgba(31, 35, 40, 0.1);
	                                      --shadow-lg: 0 4px 14px rgba(31, 35, 40, 0.14);
	                                      --transition: 100ms ease;
	                                      --header-height: 54px;
	                                      --toc-width: 230px;
	                                      --content-max-width: 860px;
	                                  }
	                                  html[data-style="classic"][data-theme="dark"] {
	                                      --color-bg: #0d1117;
	                                      --color-bg-secondary: #161b22;
	                                      --color-bg-code: #161b22;
	                                      --color-text: #e6edf3;
	                                      --color-text-secondary: #b1bac4;
	                                      --color-text-muted: #9198a1;
	                                      --color-border: #30363d;
	                                      --color-border-light: #21262d;
	                                      --cs-hover: #1f252d;
	                                      --cs-zebra: #12171e;
	                                      --cs-bar: #010409;
	                                      --cs-on-bar: var(--color-text);
	                                      --cs-on-bar-muted: var(--color-text-secondary);
	                                      --cs-bar-line: var(--color-border);
	                                      --cs-bar-control: var(--color-bg-secondary);
	                                      --cs-bar-control-hover: #1f252d;
	                                      --cs-bar-control-border: var(--color-border);
	                                      --shadow-sm: 0 1px 2px rgba(1, 4, 9, 0.6);
	                                      --shadow-md: 0 3px 8px rgba(1, 4, 9, 0.7);
	                                      --shadow-lg: 0 8px 24px rgba(1, 4, 9, 0.8);
	                                  }
	                                  html[data-style="classic"] body { font-size: 15px; line-height: 1.6; }
	                                  html[data-style="classic"] .site-header {
	                                      background: var(--cs-bar);
	                                      border-bottom: 1px solid var(--cs-bar-line);
	                                      box-shadow: inset 0 3px 0 var(--st-link);
	                                      backdrop-filter: none;
	                                  }
	                                  html[data-style="classic"]:not([data-theme="dark"]) .site-header { box-shadow: none; }
	                                  html[data-style="classic"] .site-header :is(.site-logo, .site-logo:hover, .site-logo-icon) { color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-name { font-weight: 600; }
	                                  html[data-style="classic"] .site-header :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger, .version-trigger) {
	                                      color: var(--cs-on-bar);
	                                      background: transparent;
	                                      border-color: var(--cs-bar-control-border);
	                                  }
	                                  html[data-style="classic"] .site-header :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger, .version-trigger):hover { background: var(--cs-bar-control-hover); color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-header :is(.color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger, .version-trigger)[aria-expanded="true"] {
	                                      color: var(--cs-on-bar);
	                                      border-color: var(--cs-on-bar);
	                                      background: var(--cs-bar-control-hover);
	                                      box-shadow: none;
	                                  }
	                                  html[data-style="classic"] .site-header .search-trigger {
	                                      background: var(--cs-bar-control);
	                                      border-color: var(--cs-bar-control-border);
	                                      color: var(--cs-on-bar-muted);
	                                      min-width: 200px;
	                                  }
	                                  html[data-style="classic"] .site-header .search-trigger:hover { border-color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-header .search-trigger kbd { background: transparent; border-color: var(--cs-bar-control-border); color: var(--cs-on-bar-muted); margin-left: auto; }
	                                  html[data-style="classic"] :is(.color-theme-dropdown, .code-theme-dropdown, .code-style-dropdown, .style-dropdown, .version-dropdown) { box-shadow: var(--shadow-md); }
	                                  html[data-style="classic"] :is(.code-theme-option, .code-style-option, .style-option, .version-option):hover { background: var(--cs-hover); }
	                                  html[data-style="classic"] .sidebar { background: var(--color-bg-secondary); border-right-color: var(--color-border); padding: 1rem 0; }
	                                  html[data-style="classic"] .nav-section { margin-bottom: 0.5rem; }
	                                  html[data-style="classic"] .nav-link {
	                                      padding: 0.3125rem 1.25rem !important;
	                                      border-radius: 0;
	                                      border-left: 3px solid transparent !important;
	                                      color: var(--color-text-secondary);
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="classic"] .nav-link:hover { background: var(--cs-hover); color: var(--color-text); }
	                                  html[data-style="classic"] .nav-link.current {
	                                      background: var(--color-bg);
	                                      border-left-color: var(--st-link) !important;
	                                      font-weight: 600;
	                                      box-shadow: inset 0 1px 0 var(--color-border), inset 0 -1px 0 var(--color-border);
	                                  }
	                                  html[data-style="classic"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) {
	                                      font-size: 0.75rem;
	                                      font-weight: 700;
	                                      letter-spacing: 0.04em;
	                                      text-transform: uppercase;
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="classic"] .nav-section > .nav-header .nav-link.current { color: var(--st-link); }
	                                  html[data-style="classic"] .nav-sub-section > .nav-header .nav-link { font-weight: 600; color: var(--color-text); }
	                                  html[data-style="classic"] .nav-chevron-btn { border-radius: var(--radius-sm); margin-right: 0.5rem; }
	                                  html[data-style="classic"] .nav-chevron-btn:hover { background: var(--cs-hover); }
	                                  html[data-style="classic"] .nav-children { padding-left: 0; }
	                                  html[data-style="classic"] .nav-children .nav-link { padding-left: 2rem !important; }
	                                  html[data-style="classic"] .nav-sub-section .nav-children .nav-link { padding-left: 2.75rem !important; }
	                                  html[data-style="classic"] .content { padding: 1.5rem 2.5rem 2rem; }
	                                  html[data-style="classic"] .page-content h1 {
	                                      font-size: 2rem !important;
	                                      font-weight: 600 !important;
	                                      letter-spacing: -0.01em !important;
	                                      line-height: 1.25 !important;
	                                      padding-bottom: 0.5rem;
	                                      border-bottom: 1px solid var(--color-border);
	                                  }
	                                  html[data-style="classic"] .page-content h2 { font-size: 1.5rem !important; font-weight: 600; margin: 2rem 0 0.875rem; padding-bottom: 0.3rem; letter-spacing: -0.005em; }
	                                  html[data-style="classic"] .page-content h3 { font-size: 1.1875rem !important; font-weight: 600; }
	                                  html[data-style="classic"] .page-content p { line-height: 1.65; margin-bottom: 1rem; }
	                                  html[data-style="classic"] .page-content li { line-height: 1.6; }
	                                  html[data-style="classic"] .page-content :not(pre) > code {
	                                      background: var(--color-bg-code);
	                                      border-color: var(--color-border-light);
	                                      border-radius: var(--radius) !important;
	                                      color: var(--color-text);
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="classic"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="classic"] .page-content pre { border-radius: var(--radius); }
	                                  html[data-style="classic"] .page-content table { font-size: 0.875rem; }
	                                  html[data-style="classic"] .page-content :is(th, td) { padding: 0.4rem 0.75rem; }
	                                  html[data-style="classic"] .page-content th { background: var(--color-bg-secondary); }
	                                  html[data-style="classic"] .page-content tbody tr:nth-child(even) td { background: var(--cs-zebra); }
	                                  html[data-style="classic"] .page-content tr:hover td { background: color-mix(in srgb, var(--color-primary) 7%, var(--color-bg)); }
	                                  html[data-style="classic"] .page-content blockquote { background: var(--color-bg-secondary); border-left-color: var(--color-border); border-radius: 0; color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .page-content .admonition {
	                                      border: 1px solid var(--color-border);
	                                      border-left: 4px solid var(--st-c);
	                                      border-radius: var(--radius-sm);
	                                      background: color-mix(in srgb, var(--st-c) 5%, var(--color-bg));
	                                  }
	                                  html[data-style="classic"] .page-content .admonition-title {
	                                      margin: -1rem -1.25rem 0.75rem;
	                                      padding: 0.375rem 1.25rem;
	                                      background: color-mix(in srgb, var(--st-c) 14%, var(--color-bg));
	                                      font-size: 0.875rem;
	                                  }
	                                  html[data-style="classic"] :is(.tabs, .package-install-widget, .source-viewer) { border-radius: var(--radius); }
	                                  html[data-style="classic"] .tab-headers { border-radius: var(--radius) var(--radius) 0 0; }
	                                  html[data-style="classic"] .tab-header.active { background: var(--color-bg); }
	                                  html[data-style="classic"] :is(.component-card, .component-link-card) { border-radius: var(--radius); box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] :is(.component-card, .component-link-card):hover { border-color: var(--st-link); box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] .component-step-number { border-radius: var(--radius); width: 28px; height: 28px; }
	                                  html[data-style="classic"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; }
	                                  html[data-style="classic"] .toc-sidebar { border-left-color: var(--color-border); }
	                                  html[data-style="classic"] .toc-title {
	                                      color: var(--color-text);
	                                      margin: 0 1rem 0.75rem;
	                                      padding: 0 0 0.5rem;
	                                      border-bottom: 1px solid var(--color-border);
	                                  }
	                                  html[data-style="classic"] .toc-list { border-left: none; margin-left: 0.5rem; }
	                                  html[data-style="classic"] .toc-item a { color: var(--color-text-secondary); border-left-width: 3px !important; margin-left: 0; }
	                                  html[data-style="classic"] .toc-item a:hover { background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="classic"] .toc-item a.active { background: var(--color-bg-secondary); }
	                                  html[data-style="classic"] .toc-counter { color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .breadcrumbs {
	                                      padding: 0.375rem 0.75rem;
	                                      background: var(--color-bg-secondary);
	                                      border: 1px solid var(--color-border-light);
	                                      border-radius: var(--radius);
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="classic"] .breadcrumbs li:not(:last-child)::after { content: '\203A'; opacity: 1; color: var(--color-text-muted); }
	                                  html[data-style="classic"] .page-nav-link { border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="classic"] .page-nav-link:hover { background: var(--color-bg); border-color: var(--st-link); box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] .page-nav-label { color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .feedback-btn { border-radius: var(--radius); }
	                                  html[data-style="classic"] .site-footer { background: var(--color-bg-secondary); border-top-color: var(--color-border); }
	                                  html[data-style="classic"] .footer-inner { color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .search-dialog { border-radius: var(--radius-md); }
	                                  html[data-style="classic"] .search-result { border-radius: 0; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="classic"] :is(.search-result:hover, .search-result.active) { background: var(--cs-hover); }
	                                  html[data-style="classic"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius); box-shadow: var(--shadow-md); }
	                                  html[data-style="classic"] .back-to-top:hover { box-shadow: var(--shadow-md) !important; transform: none; }
	                                  html[data-style="classic"] .landing::before,
	                                  html[data-style="classic"] .landing-hero::after { display: none; }
	                                  html[data-style="classic"] .landing-hero { background: var(--color-bg-secondary); border-bottom: 1px solid var(--color-border); padding: 4.5rem 2rem 3.5rem; }
	                                  html[data-style="classic"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="classic"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="classic"] .landing-hero-icon { animation: none; border-radius: var(--radius-md); background: var(--color-bg); border: 1px solid var(--color-border); }
	                                  html[data-style="classic"] .landing-hero-title { font-weight: 600; letter-spacing: -0.02em; }
	                                  html[data-style="classic"] .landing-btn { border-radius: var(--radius); min-width: 180px; }
	                                  html[data-style="classic"] .landing-btn-primary { box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] .landing-btn-primary:hover { transform: none; box-shadow: var(--shadow-md); background: color-mix(in srgb, var(--st-link) 85%, var(--st-ink)); }
	                                  html[data-style="classic"] .landing-btn-secondary { background: var(--color-bg); border-color: var(--color-border); }
	                                  html[data-style="classic"] .landing-btn-secondary:hover { transform: none; border-color: var(--st-link); color: var(--st-link); }
	                                  html[data-style="classic"] .landing-feature-card { border-radius: var(--radius); box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] .landing-feature-card::before { display: none; }
	                                  html[data-style="classic"] .landing-feature-card:hover { transform: none !important; box-shadow: var(--shadow-md) !important; border-color: var(--st-link); }
	                                  html[data-style="classic"] .landing-feature-icon { border-radius: var(--radius); }
	                                  html[data-style="classic"] .landing .landing-content > ul > li { border-radius: var(--radius); }
	                                  html[data-style="classic"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: var(--shadow-md) !important; }
	                                  html[data-style="classic"] .landing-md-content table { border-radius: var(--radius); }
	                                  html[data-style="classic"] .landing-md-content tbody tr:nth-child(even) td { background: var(--cs-zebra); }
	                                  html[data-style="classic"] .landing-footer { background: var(--color-bg-secondary); color: var(--color-text-secondary); }
	                                  html[data-style="classic"] :is(.repl-container, .blazor-preview-container, .openapi-endpoint-card) { border-radius: var(--radius); }
	                                  html[data-style="classic"] .openapi-endpoint-card { box-shadow: var(--shadow-sm); }
	                                  html[data-style="classic"] :is(.openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: var(--radius-sm); }

	                                  /* Style: Soft */
	                                  html[data-style="soft"] {
	                                      --color-bg: #fbfbfd;
	                                      --color-bg-secondary: color-mix(in srgb, var(--color-primary) 6%, #ffffff);
	                                      --color-bg-code: color-mix(in srgb, var(--color-primary) 4%, #f4f5f8);
	                                      --color-text: #1d2030;
	                                      --color-text-secondary: #4b5068;
	                                      --color-text-muted: #5f6478;
	                                      --color-border: color-mix(in srgb, var(--color-primary) 10%, #e6e8ef);
	                                      --color-border-light: color-mix(in srgb, var(--color-primary) 6%, #f0f1f5);
	                                      --sf-surface: #ffffff;
	                                      --sf-tint: color-mix(in srgb, var(--color-primary) 8%, #ffffff);
	                                      --sf-tint-strong: color-mix(in srgb, var(--color-primary) 14%, #ffffff);
	                                      --radius: 12px;
	                                      --radius-sm: 8px;
	                                      --radius-md: 12px;
	                                      --shadow-sm: 0 1px 2px rgba(20, 24, 40, 0.04), 0 1px 3px rgba(20, 24, 40, 0.06);
	                                      --shadow-md: 0 2px 4px rgba(20, 24, 40, 0.04), 0 6px 16px -4px rgba(20, 24, 40, 0.1);
	                                      --shadow-lg: 0 4px 8px rgba(20, 24, 40, 0.05), 0 16px 40px -8px rgba(20, 24, 40, 0.16);
	                                      --transition: 200ms ease;
	                                      --header-height: 64px;
	                                      --toc-width: 230px;
	                                      --content-max-width: 800px;
	                                  }
	                                  html[data-style="soft"][data-theme="dark"] {
	                                      --color-bg: #12131a;
	                                      --color-bg-secondary: color-mix(in srgb, var(--color-primary) 8%, #191b24);
	                                      --color-bg-code: color-mix(in srgb, var(--color-primary) 5%, #171922);
	                                      --color-text: #e9eaf2;
	                                      --color-text-secondary: #b3b6c8;
	                                      --color-text-muted: #9ca0b5;
	                                      --color-border: color-mix(in srgb, var(--color-primary) 12%, #2a2d3a);
	                                      --color-border-light: color-mix(in srgb, var(--color-primary) 8%, #20222c);
	                                      --sf-surface: #181a23;
	                                      --sf-tint: color-mix(in srgb, var(--color-primary) 12%, #181a23);
	                                      --sf-tint-strong: color-mix(in srgb, var(--color-primary) 20%, #181a23);
	                                      --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.3), 0 1px 3px rgba(0, 0, 0, 0.25);
	                                      --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.3), 0 8px 20px -4px rgba(0, 0, 0, 0.45);
	                                      --shadow-lg: 0 4px 10px rgba(0, 0, 0, 0.35), 0 20px 48px -8px rgba(0, 0, 0, 0.6);
	                                  }
	                                  html[data-style="soft"] body { line-height: 1.75; }
	                                  html[data-style="soft"] .site-header {
	                                      background: color-mix(in srgb, var(--color-bg) 82%, transparent);
	                                      border-bottom-color: transparent;
	                                      box-shadow: var(--shadow-sm);
	                                      backdrop-filter: blur(12px) saturate(1.4);
	                                  }
	                                  html[data-style="soft"] .header-actions { gap: 0.375rem; }
	                                  html[data-style="soft"] :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger) {
	                                      border-color: transparent;
	                                      border-radius: 999px;
	                                      background: var(--sf-tint);
	                                  }
	                                  html[data-style="soft"] :is(.theme-toggle, .mobile-nav-toggle, .color-theme-trigger, .code-theme-trigger, .code-style-trigger, .style-trigger):hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .search-trigger { border-color: transparent; border-radius: 999px; background: var(--sf-tint); padding: 0.4rem 0.9rem; }
	                                  html[data-style="soft"] .search-trigger:hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .search-trigger kbd { border-color: transparent; border-radius: 999px; background: var(--sf-surface); }
	                                  html[data-style="soft"] .version-trigger { border-radius: 999px; border-color: transparent; background: var(--sf-tint); }
	                                  html[data-style="soft"] :is(.color-theme-dropdown, .code-theme-dropdown, .code-style-dropdown, .style-dropdown, .version-dropdown) { border-color: var(--color-border-light); background: var(--sf-surface); padding: 8px; }
	                                  html[data-style="soft"] :is(.code-theme-option, .code-style-option, .style-option, .version-option) { border-radius: 8px; }
	                                  html[data-style="soft"] :is(.code-theme-option, .code-style-option, .style-option, .version-option):hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .version-badge-default { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .sidebar { border-right: none; padding: 1.5rem 1rem; }
	                                  html[data-style="soft"] .nav-section { margin-bottom: 0.5rem; }
	                                  html[data-style="soft"] .nav-link {
	                                      padding: 0.4rem 0.875rem !important;
	                                      border-left: 0 !important;
	                                      border-radius: 999px;
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="soft"] .nav-link:hover { background: var(--sf-tint); color: var(--color-text); }
	                                  html[data-style="soft"] .nav-link.current { background: var(--sf-tint-strong); font-weight: 600; }
	                                  html[data-style="soft"] .nav-header .nav-link,
	                                  html[data-style="soft"] .nav-group-label { font-weight: 700; color: var(--color-text); }
	                                  html[data-style="soft"] .nav-chevron-btn { border-radius: 999px; }
	                                  html[data-style="soft"] .nav-chevron-btn:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .nav-children { padding-left: 0.75rem; }
	                                  html[data-style="soft"] .content { padding: 2.5rem 3rem; }
	                                  html[data-style="soft"] .page-content h1 { margin-bottom: 1.5rem !important; }
	                                  html[data-style="soft"] .page-content h2 { border-bottom: none; padding-bottom: 0; margin: 3rem 0 1rem; }
	                                  html[data-style="soft"] .page-content h3 { margin-top: 2.25rem; }
	                                  html[data-style="soft"] .page-content p { line-height: 1.85; margin-bottom: 1.25rem; }
	                                  html[data-style="soft"] .page-content :not(pre) > code { background: var(--sf-tint); border-color: transparent; border-radius: 6px !important; }
	                                  html[data-style="soft"] :is(.page-content, .landing-content) pre { border-color: transparent; border-radius: var(--radius); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .page-content :is(.install-panel, .source-viewer, .tab-content, .repl-container, .blazor-preview-source) pre { box-shadow: none; }
	                                  html[data-style="soft"] .page-content table {
	                                      width: fit-content;
	                                      max-width: 100%;
	                                      border: 1px solid var(--color-border);
	                                      border-radius: var(--radius);
	                                      border-collapse: separate;
	                                      border-spacing: 0;
	                                      background: var(--sf-surface);
	                                  }
	                                  html[data-style="soft"] .page-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border-light); padding: 0.75rem 1rem; }
	                                  html[data-style="soft"] .page-content tr:last-child td { border-bottom: none; }
	                                  html[data-style="soft"] .page-content th { background: var(--sf-tint); }
	                                  html[data-style="soft"] .page-content tr:hover td { background: var(--sf-tint); }
	                                  html[data-style="soft"] .page-content blockquote { border-left-color: var(--sf-tint-strong); border-radius: var(--radius); background: var(--sf-tint); }
	                                  html[data-style="soft"] .page-content .admonition {
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 18%, transparent);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 9%, var(--color-bg));
	                                      box-shadow: var(--shadow-sm);
	                                      padding: 1.125rem 1.375rem;
	                                  }
	                                  html[data-style="soft"] :is(.tabs, .package-install-widget, .source-viewer) {
	                                      border-color: transparent;
	                                      border-radius: var(--radius);
	                                      background: var(--sf-surface);
	                                      box-shadow: var(--shadow-md);
	                                      overflow: hidden;
	                                  }
	                                  html[data-style="soft"] :is(.tab-headers, .install-tabs) { background: var(--sf-tint); border-bottom: none; padding: 4px; gap: 4px; }
	                                  html[data-style="soft"] :is(.tab-header, .install-tab) { border-radius: 999px; border-bottom: none; margin-bottom: 0; padding: 0.375rem 0.875rem; }
	                                  html[data-style="soft"] :is(.tab-header.active, .install-tab.active) { background: var(--sf-surface); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .install-header { background: var(--sf-surface); border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] .install-copy-btn { border-radius: 999px; }
	                                  html[data-style="soft"] .source-viewer[open] summary { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] .source-viewer pre { border-radius: 0; }
	                                  html[data-style="soft"] :is(.component-card, .component-link-card) {
	                                      border-color: transparent;
	                                      border-radius: var(--radius);
	                                      background: var(--sf-surface);
	                                      box-shadow: var(--shadow-md);
	                                  }
	                                  html[data-style="soft"] :is(.component-card, .component-link-card):hover { border-color: transparent; box-shadow: var(--shadow-lg); transform: translateY(-1px); }
	                                  html[data-style="soft"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-color: transparent; }
	                                  html[data-style="soft"] .component-card-info { background: color-mix(in srgb, #3b82f6 7%, var(--sf-surface)); }
	                                  html[data-style="soft"] .component-card-success { background: color-mix(in srgb, #22c55e 7%, var(--sf-surface)); }
	                                  html[data-style="soft"] .component-card-warning { background: color-mix(in srgb, #f59e0b 8%, var(--sf-surface)); }
	                                  html[data-style="soft"] .component-step-number { background: var(--sf-tint-strong); color: var(--st-link); }
	                                  html[data-style="soft"] .component-step:not(:last-child) .component-step-indicator::after { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .toc-sidebar { border-left: none; }
	                                  html[data-style="soft"] .toc-list { border-left: none; margin-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-item a { border-left: 0 !important; border-radius: 999px; margin-left: 0; padding: 0.3rem 0.75rem; }
	                                  html[data-style="soft"] .toc-level-3 a { margin-left: 0.75rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-level-4 a { margin-left: 1.5rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-item a:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .toc-item a.active { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .toc-progress { height: 4px; border-radius: 999px; background: var(--sf-tint); }
	                                  html[data-style="soft"] .toc-progress-bar { border-radius: 999px; background: var(--st-link); }
	                                  html[data-style="soft"] .toc-counter { border-top: none; }
	                                  html[data-style="soft"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--color-border-light); }
	                                  html[data-style="soft"] .page-nav-link { border-color: transparent; border-radius: var(--radius); background: var(--sf-surface); box-shadow: var(--shadow-sm); padding: 1.125rem 1.375rem; }
	                                  html[data-style="soft"] .page-nav-link:hover { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .feedback-btn { border-radius: 999px; padding: 0.45rem 1rem; }
	                                  html[data-style="soft"] .site-footer { background: var(--color-bg-secondary); border-top: none; }
	                                  html[data-style="soft"] .search-dialog { border-color: transparent; border-radius: 16px; background: var(--sf-surface); }
	                                  html[data-style="soft"] .search-result { border-radius: 10px; }
	                                  html[data-style="soft"] :is(.search-result:hover, .search-result.active) { background: var(--sf-tint); }
	                                  html[data-style="soft"] :is(.back-to-top, .toc-toggle-btn) { box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .back-to-top:hover { box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="soft"] .landing-hero::before { animation: none; background: radial-gradient(ellipse 70% 60% at 50% 0%, var(--sf-tint-strong), transparent 70%) !important; }
	                                  html[data-style="soft"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="soft"] .landing-hero-icon { border-radius: 20px; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-hero-title {
	                                      background: linear-gradient(135deg, var(--color-text) 35%, var(--st-link));
	                                      -webkit-background-clip: text;
	                                      background-clip: text;
	                                      -webkit-text-fill-color: transparent;
	                                  }
	                                  html[data-style="soft"] .landing-btn { border-radius: 999px; }
	                                  html[data-style="soft"] .landing-btn-primary { box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-btn-primary:hover { box-shadow: var(--shadow-lg); }
	                                  html[data-style="soft"] .landing-btn-secondary { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .landing-btn-secondary:hover { border-color: transparent; color: var(--st-link); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-feature-card { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-feature-card::before { display: none; }
	                                  html[data-style="soft"] .landing-feature-card:hover { box-shadow: var(--shadow-lg) !important; transform: translateY(-3px) !important; }
	                                  html[data-style="soft"] .landing-feature-icon { border-radius: 12px; background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .landing .landing-content > ul > li { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing .landing-content > ul > li:hover { box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="soft"] .landing-md-content table { border-color: var(--color-border); border-collapse: separate; border-spacing: 0; }
	                                  html[data-style="soft"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="soft"] .landing-md-content th { background: var(--sf-tint); }
	                                  html[data-style="soft"] .landing-footer { background: var(--color-bg-secondary); border-top: none; }
	                                  html[data-style="soft"] :is(.repl-container, .blazor-preview-container) { border-color: transparent; border-radius: var(--radius); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .openapi-endpoint-card { border-color: transparent; border-radius: var(--radius); box-shadow: var(--shadow-md); background: var(--sf-surface); }
	                                  html[data-style="soft"] .openapi-endpoint-card h3 { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] :is(.openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: 999px; }
	                                  @media (max-width: 1200px) {
	                                      html[data-style="soft"] .toc-sidebar { border-left: none; border-radius: var(--radius) 0 0 var(--radius); box-shadow: var(--shadow-lg); }
	                                  }

	                                  /* Styles: blog */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) { --blog-link: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .blog-card:hover { box-shadow: none; }
	                                  html[data-style="moka"] .blog-card { background: var(--mk-surface); border-color: var(--mk-border-visible); border-radius: var(--radius-md); padding: 1rem 1.25rem; }
	                                  html[data-style="moka"] .blog-card:hover { border-color: var(--mk-border-strong); box-shadow: inset 0 0 20px var(--mk-glow), 0 0 12px var(--mk-glow); }
	                                  html[data-style="moka"] .page-content .blog-card .blog-card-title { font-size: 1.0625rem !important; font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="moka"] .blog-meta,
	                                  html[data-style="moka"] .blog-authors { font-size: 11px; }
	                                  html[data-style="moka"] .blog-meta time,
	                                  html[data-style="moka"] .blog-reading-time,
	                                  html[data-style="moka"] .blog-pager-status,
	                                  html[data-style="moka"] .blog-tag-count { font-family: var(--font-mono); letter-spacing: 0.02em; }
	                                  html[data-style="moka"] .blog-tag { border-radius: var(--radius-sm); border-color: var(--mk-border-visible); background: var(--mk-glow); font-family: var(--font-mono); font-size: 11px; }
	                                  html[data-style="moka"] .blog-tag:hover { border-color: var(--mk-border-strong); background: var(--mk-glow-md); }
	                                  html[data-style="moka"] .blog-read-more,
	                                  html[data-style="moka"] .blog-pager-link,
	                                  html[data-style="moka"] .blog-draft { font-size: 11px; text-transform: uppercase; letter-spacing: 0.08em; }
	                                  html[data-style="moka"] .blog-draft { border-radius: var(--radius-sm); border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .blog-pager-link { border-color: var(--mk-border-visible); background: transparent; }
	                                  html[data-style="moka"] .blog-pager-link:hover { border-color: var(--mk-border-strong); background: var(--mk-glow); }
	                                  html[data-style="moka"] .blog-pager-status { font-size: 11px; }
	                                  html[data-style="moka"] .blog-post-header { border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="minimal"] .blog-list { gap: 0; }
	                                  html[data-style="minimal"] .blog-card { padding: 1.75rem 0; background: transparent; border: none; border-bottom: 1px solid var(--color-border); border-radius: 0; }
	                                  html[data-style="minimal"] .blog-card:first-child { padding-top: 0.5rem; }
	                                  html[data-style="minimal"] .page-content .blog-card .blog-card-title { font-size: 1.375rem !important; font-weight: 600; }
	                                  html[data-style="minimal"] .blog-tag { background: transparent; border-color: var(--color-border); color: var(--color-text-secondary); }
	                                  html[data-style="minimal"] .blog-tag:hover { background: transparent; border-color: var(--st-link); color: var(--st-link); }
	                                  html[data-style="minimal"] .blog-pager-link { padding-left: 0; padding-right: 0; border-color: transparent; }
	                                  html[data-style="minimal"] .blog-pager-link:hover { background: transparent; border-color: transparent; text-decoration: underline; text-underline-offset: 0.18em; }
	                                  html[data-style="minimal"] .blog-post-header { padding-bottom: 0; border-bottom: none; }
	                                  html[data-style="classic"] .blog-card:hover { border-color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .page-content .blog-card .blog-card-title { font-size: 1.25rem !important; font-weight: 600; }
	                                  html[data-style="classic"] .blog-tag { padding: 0.0625rem 0.625rem; border-color: transparent; border-radius: 2em; background: color-mix(in srgb, var(--color-primary) 10%, var(--color-bg)); font-size: 0.75rem; }
	                                  html[data-style="classic"] .blog-tag:hover { border-color: transparent; background: color-mix(in srgb, var(--color-primary) 18%, var(--color-bg)); text-decoration: underline; }
	                                  html[data-style="classic"] .blog-pager-link { background: var(--color-bg-secondary); }
	                                  html[data-style="classic"] .blog-pager-link:hover { background: var(--cs-hover); border-color: var(--color-text-secondary); }
	                                  html[data-style="soft"] .blog-list { gap: 1.5rem; }
	                                  html[data-style="soft"] .blog-card { padding: 1.5rem 1.75rem; background: var(--sf-surface); border-color: transparent; box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .blog-card:hover { border-color: transparent; box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .blog-tag,
	                                  html[data-style="soft"] .blog-tag:hover { border-color: transparent; }
	                                  html[data-style="soft"] .blog-tag { background: var(--sf-tint); }
	                                  html[data-style="soft"] .blog-tag:hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .blog-pager-link { background: var(--sf-surface); border-color: transparent; border-radius: 999px; box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .blog-pager-link:hover { background: var(--sf-surface); border-color: transparent; box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .blog-post-header { border-bottom: none; padding-bottom: 0.5rem; }
	                                  html[data-style="soft"] .page-content .blog-cover { border-radius: var(--radius); }

	                                  /* Styles: narrow screens */
	                                  @media (max-width: 768px) {
	                                      html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"]) .content { padding: 1.5rem 1rem; }
	                                      html[data-style="moka"] .sidebar { border-right: 1px solid var(--mk-border-strong); }
	                                      html[data-style="minimal"] .sidebar { border-right: 1px solid var(--color-border); padding: 1.5rem 1rem; }
	                                      html[data-style="classic"] .site-header .search-trigger { min-width: 0; }
	                                      html[data-style="soft"] .sidebar { background: var(--sf-surface); box-shadow: var(--shadow-lg); }
	                                  }

	                                  /* Styles: version selector. The open trigger's label is text, unlike the other pickers' icons, and white on the
	                                     lightened bar was under 4.5:1 for the emerald and amber presets */
	                                  html[data-style="classic"] .site-header .version-trigger[aria-expanded="true"] { background: var(--cs-bar-control); }
	                                  """;

	#endregion

	#region Embedded JS

	private const string _embeddedJs = """
	                                   /* MokaDocs Default Theme JS */
	                                   (function() {
	                                       'use strict';

	                                       // Dark mode toggle
	                                       const toggle = document.querySelector('.theme-toggle');
	                                       const html = document.documentElement;
	                                       const stored = localStorage.getItem('mokadocs-theme');
	                                       if (stored) {
	                                           html.setAttribute('data-theme', stored);
	                                       } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
	                                           html.setAttribute('data-theme', 'dark');
	                                       }
	                                       var lightCodeThemes = ['catppuccin-latte', 'github-light'];
	                                       var darkCodePairs = { 'catppuccin-latte': 'catppuccin-mocha', 'github-light': 'github-dark' };
	                                       var lightCodePairs = { 'catppuccin-mocha': 'catppuccin-latte', 'github-dark': 'github-light' };

	                                       if (toggle) {
	                                           toggle.addEventListener('click', () => {
	                                               const current = html.getAttribute('data-theme');
	                                               const next = current === 'dark' ? 'light' : 'dark';
	                                               html.setAttribute('data-theme', next);
	                                               localStorage.setItem('mokadocs-theme', next);

	                                               // Auto-switch code theme to match dark/light mode
	                                               var codeTheme = html.getAttribute('data-code-theme');
	                                               var isLightCode = lightCodeThemes.indexOf(codeTheme) !== -1;
	                                               if (next === 'dark' && isLightCode && darkCodePairs[codeTheme]) {
	                                                   var newCode = darkCodePairs[codeTheme];
	                                                   html.setAttribute('data-code-theme', newCode);
	                                                   localStorage.setItem('mokadocs-code-theme', newCode);
	                                                   if (typeof updateCodeThemeActive === 'function') updateCodeThemeActive();
	                                               } else if (next === 'light' && !isLightCode && lightCodePairs[codeTheme]) {
	                                                   var newCode = lightCodePairs[codeTheme];
	                                                   html.setAttribute('data-code-theme', newCode);
	                                                   localStorage.setItem('mokadocs-code-theme', newCode);
	                                                   if (typeof updateCodeThemeActive === 'function') updateCodeThemeActive();
	                                               }
	                                           });
	                                       }

	                                       // Opening one of these pickers closes the other two. Their clicks stop
	                                       // propagating, so the others' outside-click handlers never see them.
	                                       function closeOtherPickers(keep) {
	                                           [['.color-theme-dropdown', '.color-theme-trigger'], ['.code-theme-dropdown', '.code-theme-trigger'], ['.code-style-dropdown', '.code-style-trigger']].forEach(function(pair) {
	                                               var dd = document.querySelector(pair[0]);
	                                               if (!dd || dd === keep) return;
	                                               dd.setAttribute('hidden', '');
	                                               var tr = document.querySelector(pair[1]);
	                                               if (tr) tr.setAttribute('aria-expanded', 'false');
	                                           });
	                                       }

	                                       // Color theme selector
	                                       (function() {
	                                           // No preset when the site sets its own primaryColor and the reader has not
	                                           // picked one: the presets use !important and would hide the configured color.
	                                           var colorTheme = localStorage.getItem('mokadocs-color-theme') || html.getAttribute('data-color-theme');
	                                           if (colorTheme) html.setAttribute('data-color-theme', colorTheme);

	                                           var trigger = document.querySelector('.color-theme-trigger');
	                                           var dropdown = document.querySelector('.color-theme-dropdown');
	                                           if (trigger && dropdown) {
	                                               // Mark active swatch
	                                               function updateActive() {
	                                                   dropdown.querySelectorAll('.color-swatch').forEach(function(s) {
	                                                       s.classList.toggle('active', s.getAttribute('data-color-theme') === html.getAttribute('data-color-theme'));
	                                                   });
	                                               }
	                                               updateActive();

	                                               trigger.addEventListener('click', function(e) {
	                                                   e.stopPropagation();
	                                                   var open = dropdown.hasAttribute('hidden');
	                                                   if (open) {
	                                                       closeOtherPickers(dropdown);
	                                                       dropdown.removeAttribute('hidden');
	                                                       trigger.setAttribute('aria-expanded', 'true');
	                                                   } else {
	                                                       dropdown.setAttribute('hidden', '');
	                                                       trigger.setAttribute('aria-expanded', 'false');
	                                                   }
	                                               });

	                                               dropdown.querySelectorAll('.color-swatch').forEach(function(swatch) {
	                                                   swatch.addEventListener('click', function(e) {
	                                                       e.stopPropagation();
	                                                       var theme = swatch.getAttribute('data-color-theme');
	                                                       html.setAttribute('data-color-theme', theme);
	                                                       localStorage.setItem('mokadocs-color-theme', theme);
	                                                       updateActive();
	                                                       dropdown.setAttribute('hidden', '');
	                                                       trigger.setAttribute('aria-expanded', 'false');
	                                                   });
	                                               });

	                                               document.addEventListener('click', function() {
	                                                   dropdown.setAttribute('hidden', '');
	                                                   trigger.setAttribute('aria-expanded', 'false');
	                                               });
	                                           }
	                                       })();

	                                       // Code theme selector
	                                       var updateCodeThemeActive;
	                                       (function() {
	                                           var storedCode = localStorage.getItem('mokadocs-code-theme');
	                                           if (storedCode) {
	                                               html.setAttribute('data-code-theme', storedCode);
	                                           }

	                                           var ctTrigger = document.querySelector('.code-theme-trigger');
	                                           var ctDropdown = document.querySelector('.code-theme-dropdown');
	                                           if (ctTrigger && ctDropdown) {
	                                               updateCodeThemeActive = function() {
	                                                   var current = html.getAttribute('data-code-theme');
	                                                   ctDropdown.querySelectorAll('.code-theme-option').forEach(function(opt) {
	                                                       opt.classList.toggle('active', opt.getAttribute('data-code-theme') === current);
	                                                   });
	                                               };
	                                               updateCodeThemeActive();

	                                               ctTrigger.addEventListener('click', function(e) {
	                                                   e.stopPropagation();
	                                                   closeOtherPickers(ctDropdown);

	                                                   var open = ctDropdown.hasAttribute('hidden');
	                                                   if (open) {
	                                                       ctDropdown.removeAttribute('hidden');
	                                                       ctTrigger.setAttribute('aria-expanded', 'true');
	                                                   } else {
	                                                       ctDropdown.setAttribute('hidden', '');
	                                                       ctTrigger.setAttribute('aria-expanded', 'false');
	                                                   }
	                                               });

	                                               ctDropdown.querySelectorAll('.code-theme-option').forEach(function(opt) {
	                                                   opt.addEventListener('click', function(e) {
	                                                       e.stopPropagation();
	                                                       var theme = opt.getAttribute('data-code-theme');
	                                                       html.setAttribute('data-code-theme', theme);
	                                                       localStorage.setItem('mokadocs-code-theme', theme);
	                                                       updateCodeThemeActive();
	                                                       ctDropdown.setAttribute('hidden', '');
	                                                       ctTrigger.setAttribute('aria-expanded', 'false');
	                                                   });
	                                               });

	                                               document.addEventListener('click', function() {
	                                                   ctDropdown.setAttribute('hidden', '');
	                                                   ctTrigger.setAttribute('aria-expanded', 'false');
	                                               });
	                                           }
	                                       })();

	                                       // Code style selector
	                                       (function() {
	                                           var storedStyle = localStorage.getItem('mokadocs-code-style');
	                                           if (storedStyle) {
	                                               html.setAttribute('data-code-style', storedStyle);
	                                           }

	                                           var csTrigger = document.querySelector('.code-style-trigger');
	                                           var csDropdown = document.querySelector('.code-style-dropdown');
	                                           if (csTrigger && csDropdown) {
	                                               function updateCodeStyleActive() {
	                                                   var current = html.getAttribute('data-code-style') || 'plain';
	                                                   csDropdown.querySelectorAll('.code-style-option').forEach(function(opt) {
	                                                       opt.classList.toggle('active', opt.getAttribute('data-code-style') === current);
	                                                   });
	                                               }
	                                               updateCodeStyleActive();

	                                               csTrigger.addEventListener('click', function(e) {
	                                                   e.stopPropagation();
	                                                   closeOtherPickers(csDropdown);

	                                                   var open = csDropdown.hasAttribute('hidden');
	                                                   if (open) {
	                                                       csDropdown.removeAttribute('hidden');
	                                                       csTrigger.setAttribute('aria-expanded', 'true');
	                                                   } else {
	                                                       csDropdown.setAttribute('hidden', '');
	                                                       csTrigger.setAttribute('aria-expanded', 'false');
	                                                   }
	                                               });

	                                               csDropdown.querySelectorAll('.code-style-option').forEach(function(opt) {
	                                                   opt.addEventListener('click', function(e) {
	                                                       e.stopPropagation();
	                                                       var style = opt.getAttribute('data-code-style');
	                                                       html.setAttribute('data-code-style', style);
	                                                       localStorage.setItem('mokadocs-code-style', style);
	                                                       updateCodeStyleActive();
	                                                       csDropdown.setAttribute('hidden', '');
	                                                       csTrigger.setAttribute('aria-expanded', 'false');
	                                                   });
	                                               });

	                                               document.addEventListener('click', function() {
	                                                   csDropdown.setAttribute('hidden', '');
	                                                   csTrigger.setAttribute('aria-expanded', 'false');
	                                               });
	                                           }
	                                       })();

	                                       // Style selector
	                                       (function() {
	                                           var stTrigger = document.querySelector('.style-trigger');
	                                           var stDropdown = document.querySelector('.style-dropdown');
	                                           if (!stTrigger || !stDropdown) return;
	                                           var stOptions = Array.prototype.slice.call(stDropdown.querySelectorAll('.style-option'));

	                                           function updateStyleActive() {
	                                               var current = html.getAttribute('data-style') || 'default';
	                                               stOptions.forEach(function(opt) {
	                                                   var active = opt.getAttribute('data-style-option') === current;
	                                                   opt.classList.toggle('active', active);
	                                                   opt.setAttribute('aria-checked', active ? 'true' : 'false');
	                                               });
	                                           }

	                                           function openStyleMenu() {
	                                               document.querySelectorAll('.color-theme-dropdown, .code-theme-dropdown, .code-style-dropdown, .version-dropdown').forEach(function(d) { d.setAttribute('hidden', ''); });
	                                               document.querySelectorAll('.color-theme-trigger, .code-theme-trigger, .code-style-trigger, .version-trigger').forEach(function(t) { t.setAttribute('aria-expanded', 'false'); });
	                                               stDropdown.removeAttribute('hidden');
	                                               stTrigger.setAttribute('aria-expanded', 'true');
	                                               (stDropdown.querySelector('.style-option.active') || stOptions[0]).focus();
	                                           }

	                                           function closeStyleMenu(returnFocus) {
	                                               if (stDropdown.hasAttribute('hidden')) return;
	                                               stDropdown.setAttribute('hidden', '');
	                                               stTrigger.setAttribute('aria-expanded', 'false');
	                                               if (returnFocus) stTrigger.focus();
	                                           }

	                                           // Moka starts dark, but only for a reader who has never used the dark mode toggle.
	                                           function applyStyle(style) {
	                                               html.setAttribute('data-style', style);
	                                               localStorage.setItem('mokadocs-style', style);
	                                               if (!localStorage.getItem('mokadocs-theme')) {
	                                                   var dark = style === 'moka' || window.matchMedia('(prefers-color-scheme: dark)').matches;
	                                                   html.setAttribute('data-theme', dark ? 'dark' : 'light');
	                                               }
	                                               updateStyleActive();
	                                           }

	                                           updateStyleActive();

	                                           stTrigger.addEventListener('click', function(e) {
	                                               e.stopPropagation();
	                                               if (stDropdown.hasAttribute('hidden')) { openStyleMenu(); } else { closeStyleMenu(false); }
	                                           });
	                                           stTrigger.addEventListener('keydown', function(e) {
	                                               if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { e.preventDefault(); openStyleMenu(); }
	                                           });

	                                           stOptions.forEach(function(opt, i) {
	                                               opt.addEventListener('click', function(e) {
	                                                   e.stopPropagation();
	                                                   applyStyle(opt.getAttribute('data-style-option'));
	                                                   closeStyleMenu(true);
	                                               });
	                                               opt.addEventListener('keydown', function(e) {
	                                                   var next = null;
	                                                   if (e.key === 'ArrowDown') next = stOptions[(i + 1) % stOptions.length];
	                                                   else if (e.key === 'ArrowUp') next = stOptions[(i - 1 + stOptions.length) % stOptions.length];
	                                                   else if (e.key === 'Home') next = stOptions[0];
	                                                   else if (e.key === 'End') next = stOptions[stOptions.length - 1];
	                                                   else if (e.key === 'Escape') { e.preventDefault(); closeStyleMenu(true); }
	                                                   else if (e.key === 'Tab') closeStyleMenu(false);
	                                                   if (next) { e.preventDefault(); next.focus(); }
	                                               });
	                                           });

	                                           // Capture phase: the other pickers stop their clicks from bubbling, and this
	                                           // menu still has to close when one of them opens.
	                                           document.addEventListener('click', function(e) {
	                                               if (!e.target.closest || !e.target.closest('.style-selector')) closeStyleMenu(false);
	                                           }, true);
	                                       })();

	                                       // Mobile nav toggle
	                                       const mobileToggle = document.querySelector('.mobile-nav-toggle');
	                                       const sidebar = document.getElementById('sidebar');
	                                       if (mobileToggle && sidebar) {
	                                           mobileToggle.addEventListener('click', () => {
	                                               // The sidebar is hidden on load here, so it could not be scrolled to the current page then.
	                                               if (sidebar.classList.toggle('open')) sidebar.querySelector('.nav-link.current')?.scrollIntoView({ block: 'center' });
	                                           });
	                                       }

	                                       // Syntax highlighting + copy buttons + language labels
	                                       const langRules = {
	                                           csharp: { keywords: /\b(using|namespace|class|struct|record|interface|enum|delegate|public|private|protected|internal|static|sealed|abstract|virtual|override|async|await|new|return|if|else|for|foreach|while|do|switch|case|break|continue|try|catch|finally|throw|var|const|readonly|ref|out|in|is|as|typeof|sizeof|nameof|this|base|null|true|false|void|int|string|bool|double|float|long|short|byte|char|decimal|object|dynamic|where|select|from|get|set|init|required|event|partial|yield|lock|params|stackalloc|when|not|and|or|with|record)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?(f|d|m|L|u|ul)?\b/g },
	                                           javascript: { keywords: /\b(const|let|var|function|return|if|else|for|while|do|switch|case|break|continue|try|catch|finally|throw|new|typeof|instanceof|in|of|class|extends|import|export|default|from|async|await|yield|this|super|null|undefined|true|false|void|delete|debugger)\b/g, strings: /(`(?:[^`\\]|\\.)*`|"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*')/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?\b/g },
	                                           typescript: null, // uses javascript rules
	                                           html: { tags: /<\/?[a-zA-Z][a-zA-Z0-9-]*(?:\s|>)/g, attrs: /\s[a-zA-Z-]+=(?=")/g, strings: /"[^"]*"/g, comments: /<!--[\s\S]*?-->/gm },
	                                           xml: null, // uses html rules
	                                           css: { keywords: /(@media|@import|@keyframes|@font-face|@supports|!important)\b/g, strings: /"[^"]*"|'[^']*'/g, comments: /\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?(px|em|rem|%|vh|vw|s|ms|deg|fr)?\b/g, properties: /[\w-]+(?=\s*:)/g },
	                                           json: { strings: /"(?:[^"\\]|\\.)*"/g, numbers: /\b\d+(\.\d+)?\b/g, keywords: /\b(true|false|null)\b/g },
	                                           yaml: { comments: /#.*/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, keywords: /\b(true|false|null|yes|no)\b/g, numbers: /\b\d+(\.\d+)?\b/g },
	                                           bash: { keywords: /\b(if|then|else|elif|fi|for|while|do|done|case|esac|function|return|in|echo|exit|export|source|cd|ls|rm|cp|mv|mkdir|grep|sed|awk|cat|sudo|apt|npm|dotnet|git|docker|pip|curl|wget)\b/g, strings: /"(?:[^"\\]|\\.)*"|'[^']*'/g, comments: /#.*/g, numbers: /\b\d+\b/g },
	                                           python: { keywords: /\b(def|class|if|elif|else|for|while|try|except|finally|with|as|import|from|return|yield|raise|pass|break|continue|and|or|not|in|is|None|True|False|self|lambda|global|nonlocal|async|await|print)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /#.*/g, numbers: /\b\d+(\.\d+)?\b/g },
	                                           sql: { keywords: /\b(SELECT|FROM|WHERE|INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|TABLE|INDEX|VIEW|JOIN|LEFT|RIGHT|INNER|OUTER|ON|AND|OR|NOT|IN|IS|NULL|AS|ORDER|BY|GROUP|HAVING|LIMIT|OFFSET|UNION|INTO|VALUES|SET|DISTINCT|COUNT|SUM|AVG|MAX|MIN|LIKE|BETWEEN|EXISTS|CASE|WHEN|THEN|ELSE|END|PRIMARY|KEY|FOREIGN|REFERENCES|CASCADE)\b/gi, strings: /'[^']*'/g, comments: /--.*$/gm, numbers: /\b\d+(\.\d+)?\b/g },
	                                           rust: { keywords: /\b(fn|let|mut|const|if|else|for|while|loop|match|return|struct|enum|impl|trait|pub|use|mod|self|super|crate|where|async|await|move|ref|true|false|Some|None|Ok|Err|Box|Vec|String|Option|Result)\b/g, strings: /"(?:[^"\\]|\\.)*"/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?(f32|f64|i32|i64|u32|u64|usize|isize)?\b/g },
	                                           go: { keywords: /\b(func|var|const|type|struct|interface|map|chan|if|else|for|range|switch|case|default|return|break|continue|go|defer|select|package|import|true|false|nil|make|new|len|cap|append|delete|panic|recover)\b/g, strings: /("(?:[^"\\]|\\.)*"|`[^`]*`)/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?\b/g },
	                                           razor: { keywords: /\b(using|namespace|class|public|private|protected|if|else|for|foreach|while|switch|case|return|var|new|async|await|true|false|null|void|int|string|bool|double|this|base|typeof|readonly|static|partial|override|virtual|abstract|sealed|record|yield|throw|try|catch|finally|get|set|init)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\/|@\*[\s\S]*?\*@/gm, tags: /<\/?[a-zA-Z][a-zA-Z0-9-]*(?:\s|>)/g, attrs: /\s[a-zA-Z@:-]+=(?=")/g, numbers: /\b\d+(\.\d+)?\b/g },
	                                           java: { keywords: /\b(abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|null|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|var|void|volatile|while|true|false|record|sealed|permits|yield)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?[lLfFdD]?\b/g },
	                                           kotlin: { keywords: /\b(abstract|actual|annotation|as|break|by|catch|class|companion|const|constructor|continue|crossinline|data|do|else|enum|expect|external|false|final|finally|for|fun|get|if|import|in|infix|init|inline|inner|interface|internal|is|it|lateinit|noinline|null|object|open|operator|out|override|package|private|protected|public|reified|return|sealed|set|super|suspend|tailrec|this|throw|true|try|typealias|typeof|val|var|vararg|when|where|while)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?[lLfF]?\b/g },
	                                           swift: { keywords: /\b(actor|associatedtype|async|await|break|case|catch|class|continue|default|defer|deinit|do|else|enum|extension|fallthrough|false|fileprivate|for|func|guard|if|import|in|init|inout|internal|is|lazy|let|nil|open|operator|override|private|protocol|public|repeat|rethrows|return|self|Self|some|static|struct|subscript|super|switch|throw|throws|true|try|typealias|var|weak|where|while)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\//gm, numbers: /\b\d+(\.\d+)?\b/g },
	                                           php: { keywords: /\b(abstract|and|array|as|break|callable|case|catch|class|clone|const|continue|declare|default|die|do|echo|else|elseif|empty|enddeclare|endfor|endforeach|endif|endswitch|endwhile|eval|exit|extends|final|finally|fn|for|foreach|function|global|goto|if|implements|include|include_once|instanceof|insteadof|interface|isset|list|match|namespace|new|null|or|print|private|protected|public|readonly|require|require_once|return|static|switch|throw|trait|true|false|try|unset|use|var|while|yield|yield from)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /\/\/.*$|\/\*[\s\S]*?\*\/|#.*/gm, numbers: /\b\d+(\.\d+)?\b/g },
	                                           ruby: { keywords: /\b(alias|and|begin|break|case|class|def|defined\?|do|else|elsif|end|ensure|false|for|if|in|module|next|nil|not|or|redo|rescue|retry|return|self|super|then|true|undef|unless|until|when|while|yield|require|include|extend|attr_reader|attr_writer|attr_accessor|puts|print|raise)\b/g, strings: /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, comments: /#.*/g, numbers: /\b\d+(\.\d+)?\b/g },
	                                           docker: { keywords: /\b(FROM|AS|RUN|CMD|LABEL|EXPOSE|ENV|ADD|COPY|ENTRYPOINT|VOLUME|USER|WORKDIR|ARG|ONBUILD|STOPSIGNAL|HEALTHCHECK|SHELL)\b/g, strings: /"(?:[^"\\]|\\.)*"|'[^']*'/g, comments: /#.*/g },
	                                           powershell: { keywords: /\b(Begin|Break|Catch|Class|Continue|Data|Define|Do|DynamicParam|Else|ElseIf|End|Enum|Exit|Filter|Finally|For|ForEach|From|Function|Hidden|If|In|InlineScript|Param|Process|Return|Static|Switch|Throw|Trap|Try|Until|Using|Var|While|Workflow|parallel|sequence)\b/gi, strings: /"(?:[^"\\]|\\.)*"|'[^']*'/g, comments: /#.*/g, numbers: /\b\d+(\.\d+)?\b/g },
	                                           toml: { strings: /"(?:[^"\\]|\\.)*"|'[^']*'/g, comments: /#.*/g, keywords: /\b(true|false)\b/g, numbers: /\b\d+(\.\d+)?\b/g, properties: /[\w.-]+(?=\s*=)/g },
	                                           fsharp: { keywords: /\b(abstract|and|as|assert|base|begin|class|default|delegate|do|done|downcast|downto|elif|else|end|exception|extern|false|finally|fixed|for|fun|function|global|if|in|inherit|inline|interface|internal|lazy|let|match|member|module|mutable|namespace|new|not|null|of|open|or|override|private|public|rec|return|select|static|struct|then|to|true|try|type|upcast|use|val|void|when|while|with|yield|async|await|task)\b/g, types: /\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b/g, strings: /"(?:[^"\\]|\\.)*"/g, comments: /\/\/.*$|\(\*[\s\S]*?\*\)/gm, numbers: /\b\d+(\.\d+)?[lLmMfF]?\b/g },
	                                           markdown: { strings: /\[.*?\]\(.*?\)/g, keywords: /^#{1,6}\s.*/gm, comments: /<!--[\s\S]*?-->/gm },
	                                           diff: { keywords: /^\+.*/gm, strings: /^-.*/gm, comments: /^@@.*/gm },
	                                           ini: { strings: /"(?:[^"\\]|\\.)*"|'[^']*'/g, comments: /[;#].*/g, properties: /[\w.-]+(?=\s*=)/g },
	                                           graphql: { keywords: /\b(type|query|mutation|subscription|fragment|on|schema|extend|input|interface|union|enum|scalar|directive|implements)\b/g, strings: /"(?:[^"\\]|\\.)*"/g, comments: /#.*/g },
	                                           proto: { keywords: /\b(syntax|package|import|option|message|enum|service|rpc|returns|repeated|optional|required|map|oneof|reserved|extend|true|false)\b/g, strings: /"(?:[^"\\]|\\.)*"/g, comments: /\/\/.*$/gm, types: /\b(int32|int64|uint32|uint64|sint32|sint64|fixed32|fixed64|sfixed32|sfixed64|float|double|bool|string|bytes)\b/g, numbers: /\b\d+\b/g },
	                                       };
	                                       langRules.typescript = langRules.javascript;
	                                       langRules.xml = langRules.html;
	                                       langRules.sh = langRules.bash;
	                                       langRules.shell = langRules.bash;
	                                       langRules.cs = langRules.csharp;
	                                       langRules.js = langRules.javascript;
	                                       langRules.ts = langRules.typescript;
	                                       langRules.py = langRules.python;
	                                       langRules.yml = langRules.yaml;
	                                       langRules.blazor = langRules.razor;
	                                       langRules.cshtml = langRules.razor;
	                                       langRules.jsx = langRules.javascript;
	                                       langRules.tsx = langRules.typescript;
	                                       langRules.kt = langRules.kotlin;
	                                       langRules.rb = langRules.ruby;
	                                       langRules.dockerfile = langRules.docker;
	                                       langRules.ps1 = langRules.powershell;
	                                       langRules.pwsh = langRules.powershell;
	                                       langRules.fs = langRules.fsharp;
	                                       langRules.md = langRules.markdown;
	                                       langRules.protobuf = langRules.proto;
	                                       langRules.cfg = langRules.ini;
	                                       langRules.conf = langRules.ini;
	                                       langRules.gql = langRules.graphql;

	                                       function highlight(code, lang) {
	                                           const rules = langRules[lang];
	                                           if (!rules) return escapeHtml(code);
	                                           // Apply rules in order: comments first (highest priority), then strings, keywords, etc.
	                                           const replacements = [];
	                                           const processRule = (regex, cls) => {
	                                               const r = new RegExp(regex.source, regex.flags);
	                                               let m;
	                                               while ((m = r.exec(code)) !== null) {
	                                                   replacements.push({ start: m.index, end: m.index + m[0].length, text: m[0], cls });
	                                               }
	                                           };
	                                           if (rules.comments) processRule(rules.comments, 'comment');
	                                           if (rules.strings) processRule(rules.strings, 'string');
	                                           if (rules.keywords) processRule(rules.keywords, 'keyword');
	                                           if (rules.numbers) processRule(rules.numbers, 'number');
	                                           if (rules.types) processRule(rules.types, 'type-name');
	                                           if (rules.tags) processRule(rules.tags, 'tag-name');
	                                           if (rules.attrs) processRule(rules.attrs, 'attr-name');
	                                           if (rules.properties) processRule(rules.properties, 'property');
	                                           // Sort by start position, longer matches first
	                                           replacements.sort((a, b) => a.start - b.start || b.end - a.end);
	                                           // Remove overlapping
	                                           const filtered = [];
	                                           let lastEnd = -1;
	                                           for (const r of replacements) {
	                                               if (r.start >= lastEnd) { filtered.push(r); lastEnd = r.end; }
	                                           }
	                                           // Escape the text between tokens as well. Only tokens used to be escaped, so
	                                           // a "<" outside one (if (a<b), <br/>) opened a real HTML tag and the rest of
	                                           // the block vanished.
	                                           let result = '';
	                                           let pos = 0;
	                                           for (const r of filtered) {
	                                               result += escapeHtml(code.substring(pos, r.start));
	                                               result += '<span class="token ' + r.cls + '">' + escapeHtml(r.text) + '</span>';
	                                               pos = r.end;
	                                           }
	                                           return result + escapeHtml(code.substring(pos));
	                                       }

	                                       function escapeHtml(s) {
	                                           return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
	                                       }

	                                       document.querySelectorAll('pre code').forEach(block => {
	                                           // Detect language from class
	                                           const cls = block.className || '';
	                                           const langMatch = cls.match(/language-(\w+)/);
	                                           const lang = langMatch ? langMatch[1] : '';

	                                           // Apply syntax highlighting
	                                           if (lang && langRules[lang]) {
	                                               block.innerHTML = highlight(block.textContent, lang);
	                                           }

	                                           // Add language label
	                                           if (lang) {
	                                               const label = document.createElement('span');
	                                               label.className = 'code-lang';
	                                               label.textContent = lang;
	                                               block.parentElement.appendChild(label);
	                                           }

	                                           // Add copy button (theme.options.showCopyButton)
	                                           if (!html.hasAttribute('data-no-copy-button')) {
	                                           const btn = document.createElement('button');
	                                           btn.className = 'copy-btn';
	                                           btn.textContent = 'Copy';
	                                           btn.addEventListener('click', () => {
	                                               const text = block.textContent;
	                                               function done(ok) { btn.textContent = ok ? 'Copied!' : 'Failed'; setTimeout(() => btn.textContent = 'Copy', 2000); }
	                                               function fallback(t) {
	                                                   const ta = document.createElement('textarea');
	                                                   ta.value = t; ta.style.position = 'fixed'; ta.style.left = '-9999px';
	                                                   document.body.appendChild(ta); ta.select();
	                                                   try { document.execCommand('copy'); done(true); } catch(e) { done(false); }
	                                                   document.body.removeChild(ta);
	                                               }
	                                               if (navigator.clipboard && window.isSecureContext) {
	                                                   navigator.clipboard.writeText(text).then(() => done(true)).catch(() => fallback(text));
	                                               } else {
	                                                   fallback(text);
	                                               }
	                                           });
	                                           block.parentElement.appendChild(btn);
	                                           }

	                                           // Add line numbers for multi-line code (theme.options.showLineNumbers).
	                                           // The trailing newline is dropped first: it counted as a line, numbering one past the end.
	                                           const lines = block.textContent.replace(/\n$/, '').split('\n');
	                                           if (lines.length >= 3 && !html.hasAttribute('data-no-line-numbers') && block.closest('.page-content, .landing-content')) {
	                                               const lineNums = document.createElement('span');
	                                               lineNums.className = 'line-numbers';
	                                               lineNums.innerHTML = lines.map((_, i) => '<span>' + (i + 1) + '</span>').join('');
	                                               block.parentElement.appendChild(lineNums);
	                                               block.parentElement.classList.add('has-line-numbers');
	                                           }
	                                       });

	                                       // Package install widget tabs and copy
	                                       (function() {
	                                           var widget = document.querySelector('.package-install-widget');
	                                           if (!widget) return;
	                                           widget.querySelectorAll('.install-tab').forEach(function(tab) {
	                                               tab.addEventListener('click', function() {
	                                                   widget.querySelectorAll('.install-tab').forEach(function(t) {
	                                                       t.classList.remove('active');
	                                                       t.setAttribute('aria-selected', 'false');
	                                                   });
	                                                   widget.querySelectorAll('.install-panel').forEach(function(p) {
	                                                       p.classList.remove('active');
	                                                   });
	                                                   tab.classList.add('active');
	                                                   tab.setAttribute('aria-selected', 'true');
	                                                   var panel = widget.querySelector('[data-panel="' + tab.getAttribute('data-tab') + '"]');
	                                                   if (panel) panel.classList.add('active');
	                                               });
	                                           });
	                                           widget.querySelectorAll('.install-copy-btn').forEach(function(btn) {
	                                               btn.addEventListener('click', function() {
	                                                   var code = btn.parentElement.querySelector('code');
	                                                   if (!code) return;
	                                                   var text = code.textContent;
	                                                   function done(ok) {
	                                                       btn.textContent = ok ? 'Copied!' : 'Copy';
	                                                       setTimeout(function() { btn.textContent = 'Copy'; }, 2000);
	                                                   }
	                                                   if (navigator.clipboard && window.isSecureContext) {
	                                                       navigator.clipboard.writeText(text).then(function() { done(true); }).catch(function() { fallbackCopy(text); });
	                                                   } else {
	                                                       fallbackCopy(text);
	                                                   }
	                                                   function fallbackCopy(t) {
	                                                       var ta = document.createElement('textarea');
	                                                       ta.value = t; ta.style.position = 'fixed'; ta.style.left = '-9999px';
	                                                       document.body.appendChild(ta); ta.select();
	                                                       try { document.execCommand('copy'); done(true); } catch(e) { done(false); }
	                                                       document.body.removeChild(ta);
	                                                   }
	                                               });
	                                           });
	                                       })();

	                                       // Heading anchor links
	                                       document.querySelectorAll('.page-content h2[id], .page-content h3[id], .page-content h4[id]').forEach(heading => {
	                                           const anchor = document.createElement('a');
	                                           anchor.className = 'heading-anchor';
	                                           anchor.href = '#' + heading.id;
	                                           anchor.textContent = '#';
	                                           anchor.title = 'Copy link';
	                                           anchor.addEventListener('click', (e) => {
	                                               e.preventDefault();
	                                               const url = window.location.origin + window.location.pathname + '#' + heading.id;
	                                               navigator.clipboard.writeText(url).then(() => {
	                                                   anchor.textContent = '\u2713';
	                                                   setTimeout(() => anchor.textContent = '#', 1500);
	                                               }).catch(() => {});
	                                               history.replaceState(null, '', '#' + heading.id);
	                                           });
	                                           heading.prepend(anchor);
	                                       });

	                                       // Tab switching
	                                       document.querySelectorAll('.tabs').forEach(tabGroup => {
	                                           const headers = tabGroup.querySelectorAll(':scope > .tab-headers > .tab-header');
	                                           const contents = tabGroup.querySelectorAll(':scope > .tab-content');
	                                           headers.forEach((header, i) => {
	                                               header.addEventListener('click', () => {
	                                                   headers.forEach(h => { h.classList.remove('active'); h.setAttribute('aria-selected', 'false'); });
	                                                   contents.forEach(c => { c.classList.remove('active'); c.hidden = true; });
	                                                   header.classList.add('active');
	                                                   header.setAttribute('aria-selected', 'true');
	                                                   if (contents[i]) { contents[i].classList.add('active'); contents[i].hidden = false; }
	                                               });
	                                           });
	                                       });

	                                       // ToC active tracking
	                                       const tocLinks = document.querySelectorAll('.toc-item a');
	                                       const tocCounter = document.getElementById('tocCounter');
	                                       if (tocLinks.length > 0) {
	                                           const headings = [];
	                                           tocLinks.forEach(link => {
	                                               const id = link.getAttribute('href')?.slice(1);
	                                               if (id) { const el = document.getElementById(id); if (el) headings.push({ el, link }); }
	                                           });
	                                           const totalSections = headings.length;
	                                           const observer = new IntersectionObserver(entries => {
	                                               entries.forEach(entry => {
	                                                   if (entry.isIntersecting) {
	                                                       tocLinks.forEach(l => l.classList.remove('active'));
	                                                       const match = headings.find(h => h.el === entry.target);
	                                                       if (match) {
	                                                           match.link.classList.add('active');
	                                                           var idx = headings.indexOf(match) + 1;
	                                                           if (tocCounter) tocCounter.textContent = 'Section ' + idx + ' of ' + totalSections;
	                                                       }
	                                                   }
	                                               });
	                                           }, { rootMargin: '-80px 0px -80% 0px' });
	                                           headings.forEach(h => observer.observe(h.el));
	                                       }

	                                       // ToC reading progress indicator
	                                       var tocProgressBar = document.getElementById('tocProgress');
	                                       if (tocProgressBar) {
	                                           window.addEventListener('scroll', function() {
	                                               var scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
	                                               var scrollHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
	                                               var progress = scrollHeight > 0 ? (scrollTop / scrollHeight) * 100 : 0;
	                                               tocProgressBar.style.width = Math.min(100, Math.max(0, progress)) + '%';
	                                           }, { passive: true });
	                                       }

	                                       // ToC expand/collapse for nested items
	                                       document.querySelectorAll('.toc-item').forEach(function(item) {
	                                           if (item.querySelector('ul')) {
	                                               item.classList.add('has-children');
	                                               item.querySelector('a').addEventListener('click', function(e) {
	                                                   if (item.classList.contains('has-children')) {
	                                                       item.classList.toggle('collapsed');
	                                                   }
	                                               });
	                                           }
	                                       });

	                                       // ToC smooth scroll on click
	                                       document.querySelectorAll('.toc-item a').forEach(function(link) {
	                                           link.addEventListener('click', function(e) {
	                                               var targetId = link.getAttribute('href');
	                                               if (targetId && targetId.startsWith('#')) {
	                                                   var target = document.getElementById(targetId.slice(1));
	                                                   if (target) {
	                                                       e.preventDefault();
	                                                       target.scrollIntoView({ behavior: 'smooth', block: 'start' });
	                                                       history.pushState(null, '', targetId);
	                                                   }
	                                               }
	                                           });
	                                       });

	                                       // ToC back-to-top link
	                                       var tocBackToTop = document.getElementById('tocBackToTop');
	                                       if (tocBackToTop) {
	                                           tocBackToTop.addEventListener('click', function(e) {
	                                               e.preventDefault();
	                                               window.scrollTo({ top: 0, behavior: 'smooth' });
	                                           });
	                                       }

	                                       // Search
	                                       const searchModal = document.getElementById('searchModal');
	                                       const searchInput = document.getElementById('searchInput');
	                                       const searchResults = document.getElementById('searchResults');
	                                       let searchData = null;
	                                       let activeIdx = -1;

	                                       function openSearch() {
	                                           if (!searchModal) return;
	                                           searchModal.hidden = false;
	                                           searchInput.value = '';
	                                           searchResults.innerHTML = '';
	                                           searchResults.classList.remove('has-query');
	                                           activeIdx = -1;
	                                           searchInput.focus();
	                                           if (!searchData) {
	                                               fetch((document.documentElement.dataset.basePath || '') + '/search-index.json').then(r => { if (!r.ok) throw new Error(r.status); return r.json(); }).then(d => { searchData = d; }).catch(() => {
	                                                   searchResults.innerHTML = '<div class="search-empty">Search unavailable</div>';
	                                               });
	                                           }
	                                       }

	                                       function closeSearch() {
	                                           if (searchModal) searchModal.hidden = true;
	                                       }

	                                       function doSearch(query) {
	                                           if (!searchData || !query) { searchResults.innerHTML = ''; searchResults.classList.toggle('has-query', !!query); activeIdx = -1; return; }
	                                           const terms = query.toLowerCase().split(/\s+/).filter(Boolean);
	                                           const scored = [];
	                                           for (const entry of searchData) {
	                                               const title = (entry.t || '').toLowerCase();
	                                               const section = (entry.s || '').toLowerCase();
	                                               const content = (entry.c || '').toLowerCase();
	                                               const code = (entry.x || '').toLowerCase();
	                                               const tags = (entry.k || '').toLowerCase();
	                                               let score = 0;
	                                               let matched = true;
	                                               for (const term of terms) {
	                                                   const inTitle = title.includes(term);
	                                                   const inSection = section.includes(term);
	                                                   const inContent = content.includes(term) || code.includes(term);
	                                                   const inTags = tags.includes(term);
	                                                   if (!inTitle && !inSection && !inContent && !inTags) { matched = false; break; }
	                                                   if (inTitle) score += 10;
	                                                   if (inSection) score += 5;
	                                                   if (inTags) score += 3;
	                                                   if (inContent) score += 1;
	                                               }
	                                               if (matched) scored.push({ entry, score });
	                                           }
	                                           scored.sort((a, b) => b.score - a.score);
	                                           const top = scored.slice(0, 20);
	                                           searchResults.classList.add('has-query');
	                                           activeIdx = top.length > 0 ? 0 : -1;
	                                           searchResults.innerHTML = top.map((item, i) => {
	                                               const e = item.entry;
	                                               const snippet = searchSnippet(e.c || '', terms);
	                                               const sectionPart = e.s ? '<span class="search-result-section">&rsaquo; ' + esc(e.s) + '</span>' : '';
	                                               return '<a class="search-result' + (i === 0 ? ' active' : '') + '" href="' + esc(e.r) + '">' +
	                                                   '<div><span class="search-result-title">' + esc(e.t) + '</span>' + sectionPart +
	                                                   ' <span class="search-result-category">' + esc(e.g) + '</span></div>' +
	                                                   (snippet ? '<div class="search-result-snippet">' + snippet + '</div>' : '') +
	                                                   '</a>';
	                                           }).join('');
	                                       }

	                                       // Cut and highlight the raw text, then escape each piece. Highlighting escaped text matched
	                                       // inside entities ("lt" in &lt;) and inside the <mark> tags an earlier term had added.
	                                       function searchSnippet(text, terms) {
	                                           const idx = text.toLowerCase().indexOf(terms[0] || '');
	                                           const prefix = idx > 30 ? '...' : '';
	                                           let raw = idx > 30 ? text.slice(idx - 30) : text;
	                                           let suffix = '';
	                                           if (prefix.length + raw.length > 120) { raw = raw.slice(0, 120 - prefix.length); suffix = '...'; }
	                                           const pattern = terms.filter(Boolean).sort((a, b) => b.length - a.length)
	                                               .map(t => t.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|');
	                                           const parts = pattern ? raw.split(new RegExp('(' + pattern + ')', 'gi')) : [raw];
	                                           return prefix + parts.map((part, i) => i % 2 ? '<mark>' + esc(part) + '</mark>' : esc(part)).join('') + suffix;
	                                       }

	                                       function esc(s) {
	                                           return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
	                                       }

	                                       function updateActive() {
	                                           const items = searchResults.querySelectorAll('.search-result');
	                                           items.forEach((el, i) => el.classList.toggle('active', i === activeIdx));
	                                           if (items[activeIdx]) items[activeIdx].scrollIntoView({ block: 'nearest' });
	                                       }

	                                       if (searchInput) {
	                                           searchInput.addEventListener('input', () => doSearch(searchInput.value.trim()));
	                                           searchInput.addEventListener('keydown', (e) => {
	                                               const items = searchResults.querySelectorAll('.search-result');
	                                               if (e.key === 'ArrowDown') { e.preventDefault(); activeIdx = Math.min(activeIdx + 1, items.length - 1); updateActive(); }
	                                               else if (e.key === 'ArrowUp') { e.preventDefault(); activeIdx = Math.max(activeIdx - 1, 0); updateActive(); }
	                                               else if (e.key === 'Enter' && items[activeIdx]) { e.preventDefault(); items[activeIdx].click(); }
	                                               else if (e.key === 'Escape') { closeSearch(); }
	                                           });
	                                       }

	                                       if (searchModal) {
	                                           searchModal.querySelector('.search-backdrop').addEventListener('click', closeSearch);
	                                       }

	                                       const searchTrigger = document.querySelector('.search-trigger');
	                                       if (searchTrigger) searchTrigger.addEventListener('click', openSearch);

	                                       // Only claim Ctrl/Cmd+K when the page has search.
	                                       if (searchModal) {
	                                           document.addEventListener('keydown', (e) => {
	                                               if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
	                                                   e.preventDefault();
	                                                   !searchModal.hidden ? closeSearch() : openSearch();
	                                               }
	                                           });
	                                       }

	                                       // Sidebar nav expand/collapse - toggle sections
	                                       document.querySelectorAll('.nav-toggle').forEach(toggle => {
	                                           // The list belongs to the nearest section. Looking only for .nav-section made a
	                                           // nested section's chevron collapse its parent's list instead of its own.
	                                           const section = toggle.closest('.nav-sub-section, .nav-section');
	                                           const children = section?.querySelector(':scope > .nav-children');
	                                           if (children) {
	                                               const chevron = toggle.querySelector('.nav-chevron');
	                                               let settle;
	                                               toggle.addEventListener('click', (e) => {
	                                                   e.stopPropagation();
	                                                   // An open list has no max-height: a fixed one cut long lists off. The
	                                                   // height is set in pixels only for the animation.
	                                                   children.style.maxHeight = children.scrollHeight + 'px';
	                                                   void children.offsetHeight;
	                                                   const isExpanded = children.classList.toggle('expanded');
	                                                   clearTimeout(settle);
	                                                   settle = setTimeout(() => { children.style.maxHeight = ''; }, 350);
	                                                   toggle.setAttribute('aria-expanded', isExpanded);
	                                                   if (chevron) chevron.style.transform = isExpanded ? 'rotate(180deg)' : '';
	                                               });
	                                               toggle.addEventListener('keydown', (e) => {
	                                                   if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle.click(); }
	                                               });
	                                           }
	                                       });

	                                       // Close mobile sidebar on navigation
	                                       document.querySelectorAll('.sidebar .nav-link').forEach(link => {
	                                           link.addEventListener('click', () => {
	                                               if (window.innerWidth <= 768 && sidebar) {
	                                                   sidebar.classList.remove('open');
	                                               }
	                                           });
	                                       });

	                                       // Version selector dropdown
	                                       const versionTrigger = document.querySelector('.version-trigger');
	                                       const versionDropdown = document.querySelector('.version-dropdown');
	                                       if (versionTrigger && versionDropdown) {
	                                           versionTrigger.addEventListener('click', (e) => {
	                                               e.stopPropagation();
	                                               const isOpen = versionTrigger.getAttribute('aria-expanded') === 'true';
	                                               if (!isOpen) {
	                                                   document.querySelectorAll('.color-theme-dropdown, .code-theme-dropdown, .code-style-dropdown').forEach(d => d.setAttribute('hidden', ''));
	                                                   document.querySelectorAll('.color-theme-trigger, .code-theme-trigger, .code-style-trigger').forEach(t => t.setAttribute('aria-expanded', 'false'));
	                                               }
	                                               versionTrigger.setAttribute('aria-expanded', String(!isOpen));
	                                               versionDropdown.hidden = isOpen;
	                                           });
	                                           // Capture phase, like the style menu: the other pickers stop their clicks from bubbling, so
	                                           // the menu stayed open when one of them opened.
	                                           document.addEventListener('click', (e) => {
	                                               if (!e.target.closest || !e.target.closest('.version-selector')) {
	                                                   versionTrigger.setAttribute('aria-expanded', 'false');
	                                                   versionDropdown.hidden = true;
	                                               }
	                                           }, true);
	                                           document.addEventListener('keydown', (e) => {
	                                               if (e.key === 'Escape' && !versionDropdown.hidden) {
	                                                   versionTrigger.setAttribute('aria-expanded', 'false');
	                                                   versionDropdown.hidden = true;
	                                                   versionTrigger.focus();
	                                               }
	                                           });
	                                       }

	                                       // Back to top button
	                                       const backToTop = document.getElementById('backToTop');
	                                       if (backToTop) {
	                                           window.addEventListener('scroll', () => {
	                                               backToTop.classList.toggle('visible', window.scrollY > 300);
	                                           }, { passive: true });
	                                           backToTop.addEventListener('click', () => {
	                                               window.scrollTo({ top: 0, behavior: 'smooth' });
	                                           });
	                                       }

	                                       // TOC toggle (mobile/tablet)
	                                       var tocToggle = document.getElementById('tocToggle');
	                                       var tocSidebar = document.getElementById('tocSidebar');
	                                       if (tocToggle && tocSidebar) {
	                                           tocToggle.addEventListener('click', function() {
	                                               tocSidebar.classList.toggle('open');
	                                           });
	                                           tocSidebar.querySelectorAll('.toc-item a').forEach(function(link) {
	                                               link.addEventListener('click', function() {
	                                                   tocSidebar.classList.remove('open');
	                                               });
	                                           });
	                                           document.addEventListener('click', function(e) {
	                                               if (!tocSidebar.contains(e.target) && e.target !== tocToggle && !tocToggle.contains(e.target)) {
	                                                   tocSidebar.classList.remove('open');
	                                               }
	                                           });
	                                       }

	                                       // Mermaid diagrams. The script is only fetched on pages that have a diagram, and
	                                       // runs on every layout; it used to be inlined in the default layout only.
	                                       (function() {
	                                           var diagrams = document.querySelectorAll('pre.mermaid');
	                                           if (diagrams.length === 0) return;
	                                           // Keep each diagram's source, since Mermaid replaces the element's content with
	                                           // SVG. textContent decodes the entities the build wrote; writing it back with
	                                           // textContent keeps a "<" in the source from turning into markup on re-render.
	                                           diagrams.forEach(function(el) { el.setAttribute('data-mermaid-src', el.textContent); });
	                                           var script = document.createElement('script');
	                                           script.src = 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js';
	                                           script.onload = function() {
	                                               function render() {
	                                                   var dark = html.getAttribute('data-theme') === 'dark';
	                                                   mermaid.initialize({ startOnLoad: false, theme: dark ? 'dark' : 'default' });
	                                                   diagrams.forEach(function(el) {
	                                                       el.removeAttribute('data-processed');
	                                                       el.textContent = el.getAttribute('data-mermaid-src');
	                                                   });
	                                                   mermaid.run({ nodes: diagrams });
	                                               }
	                                               render();
	                                               new MutationObserver(function(mutations) {
	                                                   if (mutations.some(function(m) { return m.attributeName === 'data-theme'; })) render();
	                                               }).observe(html, { attributes: true, attributeFilter: ['data-theme'] });
	                                           };
	                                           document.head.appendChild(script);
	                                       })();

	                                       // Feedback widget
	                                       const feedbackWidget = document.getElementById('feedbackWidget');
	                                       if (feedbackWidget) {
	                                           const btnYes = feedbackWidget.querySelector('.feedback-btn-yes');
	                                           const btnNo = feedbackWidget.querySelector('.feedback-btn-no');
	                                           const prompt = feedbackWidget.querySelector('.feedback-prompt');
	                                           const thanks = feedbackWidget.querySelector('.feedback-thanks');
	                                           const storageKey = 'mokadocs-feedback:' + window.location.pathname;

	                                           function showThanks(helpful) {
	                                               btnYes.classList.toggle('selected', helpful === true);
	                                               btnNo.classList.toggle('selected', helpful === false);
	                                               prompt.hidden = true;
	                                               thanks.hidden = false;
	                                           }

	                                           function sendFeedback(helpful) {
	                                               localStorage.setItem(storageKey, JSON.stringify(helpful));
	                                               showThanks(helpful);
	                                               try {
	                                                   fetch((document.documentElement.dataset.basePath || '') + '/api/feedback', {
	                                                       method: 'POST',
	                                                       headers: { 'Content-Type': 'application/json' },
	                                                       body: JSON.stringify({ page: window.location.pathname, helpful: helpful })
	                                                   }).catch(function() {});
	                                               } catch(e) {}
	                                           }

	                                           // Restore previous vote
	                                           const stored = localStorage.getItem(storageKey);
	                                           if (stored !== null) {
	                                               showThanks(JSON.parse(stored));
	                                           }

	                                           btnYes.addEventListener('click', function() { sendFeedback(true); });
	                                           btnNo.addEventListener('click', function() { sendFeedback(false); });
	                                       }
	                                   })();
	                                   """;

	#endregion

	/// <summary>
	///     Creates a <see cref="ThemeRenderContext" /> with the embedded default theme.
	/// </summary>
	public static ThemeRenderContext CreateDefault()
	{
		return new ThemeRenderContext
		{
			Config = null!, // Set by render phase
			Templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["default"] = _defaultLayout,
				["landing"] = _landingLayout,
				["api-type"] = _apiTypeLayout
			},
			Partials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["head"] = _headPartial,
				["nav-sidebar"] = _navSidebarPartial,
				["toc"] = _tocPartial,
				["breadcrumbs"] = _breadcrumbsPartial,
				["footer"] = _footerPartial
			},
			CssFiles = ["/_theme/css/main.css"],
			JsFiles = ["/_theme/js/main.js"]
		};
	}

	/// <summary>
	///     Returns the embedded CSS content.
	/// </summary>
	public static string GetCss() => _embeddedCss + _stylesCss;

	/// <summary>
	///     Returns the embedded JS content.
	/// </summary>
	public static string GetJs() => _embeddedJs;

	#region Layout Templates

	private const string _defaultLayout = """
	                                      <!DOCTYPE html>
	                                      <html lang="en" data-theme="light" data-code-theme="{{ theme.code_theme | html.escape }}" data-code-style="{{ theme.code_style | html.escape }}" data-style="{{ theme.style | html.escape }}"{{ if base_path != "" }} data-base-path="{{ base_path | html.escape }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}{{ if theme.show_copy_button == false }} data-no-copy-button{{ end }}{{ if theme.show_line_numbers == false }} data-no-line-numbers{{ end }}>
	                                      <head>
	                                          <script>try{const s={{ if theme.style_selector }}localStorage.getItem('mokadocs-style')||{{ end }}'{{ theme.style }}';document.documentElement.setAttribute('data-style',s);const t=localStorage.getItem('mokadocs-theme')||(s==='moka'||matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',t);const c=localStorage.getItem('mokadocs-color-theme')||'{{ theme.initial_color_theme | html.escape | string.escape }}';if(c)document.documentElement.setAttribute('data-color-theme',c);const ct=localStorage.getItem('mokadocs-code-theme');if(ct)document.documentElement.setAttribute('data-code-theme',ct);const cs=localStorage.getItem('mokadocs-code-style')||'{{ theme.code_style | html.escape | string.escape }}';document.documentElement.setAttribute('data-code-style',cs)}catch(e){}</script>
	                                          <meta charset="utf-8" />
	                                          <meta name="viewport" content="width=device-width, initial-scale=1" />
	                                          <title>{{ page.title | html.escape }} - {{ site.title | html.escape }}</title>
	                                          <meta name="description" content="{{ page.meta_description | html.escape }}" />
	                                          {{ if page.canonical_url != "" }}<link rel="canonical" href="{{ page.canonical_url | html.escape }}" />{{ end }}
	                                          {{ if page.canonical_url != "" }}<meta property="og:title" content="{{ page.title | html.escape }}" />
	                                          <meta property="og:description" content="{{ page.meta_description | html.escape }}" />
	                                          <meta property="og:url" content="{{ page.canonical_url | html.escape }}" />
	                                          <meta property="og:site_name" content="{{ site.title | html.escape }}" />
	                                          <meta property="og:type" content="article" />
	                                          <meta name="twitter:card" content="summary" />
	                                          <meta name="twitter:title" content="{{ page.title | html.escape }}" />
	                                          <meta name="twitter:description" content="{{ page.meta_description | html.escape }}" />{{ end }}
	                                          {{ for css in css_files }}<link rel="stylesheet" href="{{ css | html.escape }}" />
	                                          {{ end }}
	                                          {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color | html.escape }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color | html.escape }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color | html.escape }} 80%,#000)}</style>{{ end }}
	                                          {{ if theme.accent_color != "" }}<style>:root{--color-accent:{{ theme.accent_color | html.escape }}}</style>{{ end }}
	                                          {{ if site.favicon_url != "" }}<link rel="icon" href="{{ site.favicon_url | html.escape }}" />{{ end }}
	                                          {{ for feed in blog.feeds }}<link rel="alternate" type="{{ feed.type | html.escape }}" title="{{ feed.title | html.escape }}" href="{{ feed.url | html.escape }}" />
	                                          {{ end }}
	                                      </head>
	                                      <body>
	                                          <header class="site-header">
	                                              <div class="header-inner">
	                                                  <a class="site-logo" href="{{ base_path | html.escape }}/">
	                                                      {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="{{ site.title | html.escape }}" class="site-logo-img" />{{ else }}<svg class="site-logo-icon" width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/><path d="M8 7h6"/><path d="M8 11h8"/></svg>{{ end }}
	                                                      <span class="site-name">{{ site.title | html.escape }}</span>
	                                                  </a>
	                                                  <div class="header-actions">
	                                                      {{ if versions.size > 0 && theme.show_version_selector }}
	                                                      <div class="version-selector">
	                                                          <button class="version-trigger" aria-label="Select version" aria-expanded="false" aria-haspopup="listbox">
	                                                              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
	                                                              <span class="version-label">{{ current_version | html.escape }}</span>
	                                                              <svg class="version-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"/></svg>
	                                                          </button>
	                                                          <ul class="version-dropdown" role="listbox" hidden>
	                                                              {{ for v in versions }}
	                                                              <li role="option"{{ if v.is_current }} aria-selected="true"{{ end }}>
	                                                                  <a class="version-option{{ if v.is_current }} active{{ end }}" href="{{ v.url | html.escape }}" data-version-slug="{{ v.slug | html.escape }}"{{ if v.is_current }} aria-current="page"{{ end }}>
	                                                                      <span class="version-option-label">{{ v.label | html.escape }}</span>
	                                                                      {{ if v.is_default }}<span class="version-badge version-badge-default">latest</span>{{ end }}
	                                                                      {{ if v.is_prerelease }}<span class="version-badge version-badge-pre">pre</span>{{ end }}
	                                                                  </a>
	                                                              </li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if search_enabled }}
	                                                      <button class="search-trigger" aria-label="Search" data-shortcut="Ctrl+K">
	                                                          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
	                                                          <span class="search-label">Search</span>
	                                                          <kbd>⌘K</kbd>
	                                                      </button>
	                                                      {{ end }}
	                                                      <div class="appearance-group">
	                                                      {{ if theme.style_selector }}
	                                                      <div class="style-selector">
	                                                          <button class="style-trigger" aria-label="Change style" aria-expanded="false" aria-haspopup="menu" aria-controls="styleMenu">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18"/><path d="M9 21V9"/></svg>
	                                                          </button>
	                                                          <div class="style-dropdown" id="styleMenu" role="menu" aria-label="Style" hidden>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="default"><span class="style-preview style-preview-default"></span><span class="style-text"><span class="style-name">Default</span><span class="style-desc">The original look</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="moka"><span class="style-preview style-preview-moka"></span><span class="style-text"><span class="style-name">Moka</span><span class="style-desc">Dense, dark first</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="minimal"><span class="style-preview style-preview-minimal"></span><span class="style-text"><span class="style-name">Minimal</span><span class="style-desc">Quiet, few lines</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="classic"><span class="style-preview style-preview-classic"></span><span class="style-text"><span class="style-name">Classic</span><span class="style-desc">Reference docs</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="soft"><span class="style-preview style-preview-soft"></span><span class="style-text"><span class="style-name">Soft</span><span class="style-desc">Rounded, roomy</span></span><span class="style-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.color_themes != false }}
	                                                      <div class="color-theme-selector">
	                                                          <button class="color-theme-trigger" aria-label="Change color theme" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="13.5" cy="6.5" r="2.5"/><circle cx="17.5" cy="10.5" r="2.5"/><circle cx="8.5" cy="7.5" r="2.5"/><circle cx="6.5" cy="12" r="2.5"/><path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10c.9 0 1.5-.7 1.5-1.5 0-.4-.1-.7-.4-1-.3-.3-.4-.6-.4-1 0-.8.7-1.5 1.5-1.5H16c3.3 0 6-2.7 6-6 0-5.5-4.5-9-10-9z"/></svg>
	                                                          </button>
	                                                          <div class="color-theme-dropdown" hidden>
	                                                              <button class="color-swatch" data-color-theme="ocean" aria-label="Ocean theme" style="background:#0ea5e9"></button>
	                                                              <button class="color-swatch" data-color-theme="emerald" aria-label="Emerald theme" style="background:#10b981"></button>
	                                                              <button class="color-swatch" data-color-theme="violet" aria-label="Violet theme" style="background:#8b5cf6"></button>
	                                                              <button class="color-swatch" data-color-theme="amber" aria-label="Amber theme" style="background:#f59e0b"></button>
	                                                              <button class="color-swatch" data-color-theme="rose" aria-label="Rose theme" style="background:#f43f5e"></button>
	                                                              <button class="color-swatch" data-color-theme="moka-red" aria-label="Moka Red theme" style="background:#d32f2f"></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.code_theme_selector != false }}
	                                                      <div class="code-theme-selector">
	                                                          <button class="code-theme-trigger" aria-label="Change code theme" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>
	                                                          </button>
	                                                          <div class="code-theme-dropdown" hidden>
	                                                              <button class="code-theme-option" data-code-theme="catppuccin-mocha"><span class="code-theme-preview" style="background:#1e1e2e"></span><span class="code-theme-name">Catppuccin Mocha</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="catppuccin-latte"><span class="code-theme-preview" style="background:#eff1f5"></span><span class="code-theme-name">Catppuccin Latte</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="github-dark"><span class="code-theme-preview" style="background:#0d1117"></span><span class="code-theme-name">GitHub Dark</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="github-light"><span class="code-theme-preview" style="background:#ffffff;border:1px solid #d0d7de"></span><span class="code-theme-name">GitHub Light</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="dracula"><span class="code-theme-preview" style="background:#282a36"></span><span class="code-theme-name">Dracula</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="one-dark"><span class="code-theme-preview" style="background:#282c34"></span><span class="code-theme-name">One Dark</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="nord"><span class="code-theme-preview" style="background:#2e3440"></span><span class="code-theme-name">Nord</span><span class="code-theme-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.code_style_selector != false }}
	                                                      <div class="code-style-selector">
	                                                          <button class="code-style-trigger" aria-label="Change code block style" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>
	                                                          </button>
	                                                          <div class="code-style-dropdown" hidden>
	                                                              <button class="code-style-option" data-code-style="plain"><span class="code-style-indicator"></span><span class="code-style-name">Plain</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="macos"><span class="code-style-indicator"></span><span class="code-style-name">macOS</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="terminal"><span class="code-style-indicator"></span><span class="code-style-name">Terminal</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="vscode"><span class="code-style-indicator"></span><span class="code-style-name">VS Code</span><span class="code-style-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      </div>
	                                                      {{ if theme.show_dark_mode_toggle }}
	                                                      <button class="theme-toggle" aria-label="Toggle dark mode">
	                                                          <svg class="icon-sun" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
	                                                          <svg class="icon-moon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
	                                                      </button>
	                                                      {{ end }}
	                                                      <button class="mobile-nav-toggle" aria-label="Toggle navigation">
	                                                          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 12h18M3 6h18M3 18h18"/></svg>
	                                                      </button>
	                                                  </div>
	                                              </div>
	                                          </header>

	                                          <div class="site-container">
	                                              <aside class="sidebar" id="sidebar"{{ if sidebar_script != "" }} data-nav-route="{{ page.route | html.escape }}"{{ end }}>
	                                      """ + _sidebarNav + """

	                                              {{ if sidebar_script != "" }}<script src="{{ sidebar_script | html.escape }}"></script>{{ end }}</aside>

	                                              <main class="content">
	                                                  <article class="page-content">
	                                                      {{ if theme.show_breadcrumbs && breadcrumbs.size > 1 }}
	                                                      <nav class="breadcrumbs" aria-label="Breadcrumb">
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

	                                                      {{ if package && page.is_api_index }}
	                                                      <div class="package-install-widget">
	                                                          <div class="install-header"><span class="install-package-name">{{ package.name | html.escape }}</span><a href="https://www.nuget.org/packages/{{ package.name | html.escape }}" target="_blank" rel="noopener noreferrer" class="install-nuget-link" aria-label="View on NuGet.org"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6.5 17.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z"/><path d="M14 14a4 4 0 1 0 0-8 4 4 0 0 0 0 8z"/><circle cx="7" cy="7" r="2"/></svg><span>NuGet</span><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg></a></div>
	                                                          <div class="install-tabs" role="tablist">
	                                                              <button class="install-tab active" role="tab" aria-selected="true" data-tab="pm">Package Manager</button>
	                                                              <button class="install-tab" role="tab" aria-selected="false" data-tab="cli">.NET CLI</button>
	                                                              <button class="install-tab" role="tab" aria-selected="false" data-tab="ref">PackageReference</button>
	                                                          </div>
	                                                          <div class="install-panel active" data-panel="pm">
	                                                              <pre><code>Install-Package {{ package.name | html.escape }} -Version {{ package.version | html.escape }}</code></pre>
	                                                              <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
	                                                          </div>
	                                                          <div class="install-panel" data-panel="cli">
	                                                              <pre><code>dotnet add package {{ package.name | html.escape }} --version {{ package.version | html.escape }}</code></pre>
	                                                              <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
	                                                          </div>
	                                                          <div class="install-panel" data-panel="ref">
	                                                              <pre><code>&lt;PackageReference Include="{{ package.name | html.escape }}" Version="{{ package.version | html.escape }}" /&gt;</code></pre>
	                                                              <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ page.content }}

	                                                      {{ if edit_url }}
	                                                      <div class="edit-link">
	                                                          <a href="{{ edit_url | html.escape }}" target="_blank" rel="noopener">
	                                                              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
	                                                              Edit this page
	                                                          </a>
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if page.last_modified != "" && theme.show_last_updated }}
	                                                      <div class="last-updated">
	                                                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
	                                                          Last updated: {{ page.last_modified }}
	                                                      </div>
	                                                      {{ end }}

	                                                      {{ if theme.show_contributors && page.contributors.size > 0 }}
	                                                      <div class="page-contributors">
	                                                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
	                                                          <span>Contributors:</span>
	                                                          <ul class="page-contributors-list">
	                                                              {{ for contributor in page.contributors }}
	                                                              <li><span class="page-contributor-avatar" aria-hidden="true">{{ contributor.initials | html.escape }}</span>{{ contributor.name | html.escape }}</li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                  </article>
	                                              </main>

	                                              {{ if page.show_toc }}
	                                              <aside class="toc-sidebar" id="tocSidebar">
	                                                  <nav class="toc" aria-label="On this page">
	                                                      <h4 class="toc-title">On this page</h4>
	                                                      <div class="toc-progress"><div class="toc-progress-bar" id="tocProgress"></div></div>
	                                                      <a href="#" class="toc-back-to-top" id="tocBackToTop">&uarr; Top</a>
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
	                                                      <div class="toc-counter" id="tocCounter"></div>
	                                                  </nav>
	                                              </aside>
	                                              {{ end }}
	                                          </div>

	                                          {{ if theme.show_feedback != false }}
	                                          <div class="feedback-widget" id="feedbackWidget">
	                                              <span class="feedback-prompt">Was this page helpful?</span>
	                                              <div class="feedback-buttons">
	                                                  <button class="feedback-btn feedback-btn-yes" data-helpful="true" aria-label="Yes, this page was helpful">
	                                                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M7 10v12"/><path d="M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2h0a3.13 3.13 0 0 1 3 3.88Z"/></svg>
	                                                      <span>Yes</span>
	                                                  </button>
	                                                  <button class="feedback-btn feedback-btn-no" data-helpful="false" aria-label="No, this page was not helpful">
	                                                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 14V2"/><path d="M9 18.12 10 14H4.17a2 2 0 0 1-1.92-2.56l2.33-8A2 2 0 0 1 6.5 2H20a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-2.76a2 2 0 0 0-1.79 1.11L14 22h0a3.13 3.13 0 0 1-3-3.88Z"/></svg>
	                                                      <span>No</span>
	                                                  </button>
	                                              </div>
	                                              <span class="feedback-thanks" hidden>Thanks for your feedback!</span>
	                                          </div>
	                                          {{ end }}

	                                          {{ if theme.show_prev_next }}
	                                          <nav class="page-nav">
	                                            {{ if prev_page }}
	                                            <a class="page-nav-link page-nav-prev" href="{{ prev_page.route | html.escape }}">
	                                              <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"></polyline></svg>
	                                              <div class="page-nav-text">
	                                                <span class="page-nav-label">{{ if page.is_blog_post }}Newer post{{ else }}Previous{{ end }}</span>
	                                                <span class="page-nav-title">{{ prev_page.title | html.escape }}</span>
	                                              </div>
	                                            </a>
	                                            {{ else }}
	                                            <span class="page-nav-spacer"></span>
	                                            {{ end }}
	                                            {{ if next_page }}
	                                            <a class="page-nav-link page-nav-next" href="{{ next_page.route | html.escape }}">
	                                              <div class="page-nav-text">
	                                                <span class="page-nav-label">{{ if page.is_blog_post }}Older post{{ else }}Next{{ end }}</span>
	                                                <span class="page-nav-title">{{ next_page.title | html.escape }}</span>
	                                              </div>
	                                              <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>
	                                            </a>
	                                            {{ else }}
	                                            <span class="page-nav-spacer"></span>
	                                            {{ end }}
	                                          </nav>
	                                          {{ end }}

	                                          <footer class="site-footer">
	                                              <div class="footer-inner">
	                                                  {{ if site.copyright }}<span>{{ site.copyright }}</span>{{ end }}
	                                                  {{ if theme.social_links.size > 0 }}<div class="footer-social">{{ for link in theme.social_links }}<a href="{{ link.url | html.escape }}" target="_blank" rel="noopener noreferrer" aria-label="{{ link.icon | html.escape }}">{{ link.icon_svg }}</a>{{ end }}</div>{{ end }}
	                                                  {{ if theme.show_built_with != false }}<span class="built-with">Built with <a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v{{ mokadocs_version }}</span>{{ end }}
	                                              </div>
	                                          </footer>

	                                          {{ if search_enabled }}
	                                          <div class="search-modal" id="searchModal" hidden>
	                                              <div class="search-backdrop"></div>
	                                              <div class="search-dialog" role="dialog" aria-label="Search documentation">
	                                                  <div class="search-input-wrap">
	                                                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
	                                                      <input type="text" class="search-input" id="searchInput" placeholder="Search documentation..." autocomplete="off" />
	                                                      <kbd class="search-esc">Esc</kbd>
	                                                  </div>
	                                                  <div class="search-results" id="searchResults"></div>
	                                              </div>
	                                          </div>
	                                          {{ end }}

	                                          {{ if page.show_toc }}
	                                         <button class="toc-toggle-btn" id="tocToggle" aria-label="Toggle table of contents">
	                                             <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
	                                                 <line x1="8" y1="6" x2="21" y2="6"></line>
	                                                 <line x1="8" y1="12" x2="21" y2="12"></line>
	                                                 <line x1="8" y1="18" x2="21" y2="18"></line>
	                                                 <line x1="3" y1="6" x2="3.01" y2="6"></line>
	                                                 <line x1="3" y1="12" x2="3.01" y2="12"></line>
	                                                 <line x1="3" y1="18" x2="3.01" y2="18"></line>
	                                             </svg>
	                                         </button>
	                                         {{ end }}
	                                         {{ if theme.show_back_to_top }}
	                                         <button class="back-to-top" id="backToTop" aria-label="Back to top">&#8593;</button>
	                                         {{ end }}

	                                          {{ for js in js_files }}<script src="{{ js | html.escape }}"></script>
	                                          {{ end }}
	                                      </body>
	                                      </html>
	                                      """;

	private const string _landingLayout = """
	                                      <!DOCTYPE html>
	                                      <html lang="en" data-theme="light" data-code-theme="{{ theme.code_theme | html.escape }}" data-code-style="{{ theme.code_style | html.escape }}" data-style="{{ theme.style | html.escape }}"{{ if base_path != "" }} data-base-path="{{ base_path | html.escape }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}{{ if theme.show_copy_button == false }} data-no-copy-button{{ end }}{{ if theme.show_line_numbers == false }} data-no-line-numbers{{ end }}>
	                                      <head>
	                                          <script>try{const s={{ if theme.style_selector }}localStorage.getItem('mokadocs-style')||{{ end }}'{{ theme.style }}';document.documentElement.setAttribute('data-style',s);const t=localStorage.getItem('mokadocs-theme')||(s==='moka'||matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',t);const c=localStorage.getItem('mokadocs-color-theme')||'{{ theme.initial_color_theme | html.escape | string.escape }}';if(c)document.documentElement.setAttribute('data-color-theme',c);const ct=localStorage.getItem('mokadocs-code-theme');if(ct)document.documentElement.setAttribute('data-code-theme',ct);const cs=localStorage.getItem('mokadocs-code-style')||'{{ theme.code_style | html.escape | string.escape }}';document.documentElement.setAttribute('data-code-style',cs)}catch(e){}</script>
	                                          <meta charset="utf-8" />
	                                          <meta name="viewport" content="width=device-width, initial-scale=1" />
	                                          <title>{{ site.title | html.escape }}{{ if page.description != "" }} - {{ page.description | html.escape }}{{ end }}</title>
	                                          <meta name="description" content="{{ page.meta_description | html.escape }}" />
	                                          {{ if page.canonical_url != "" }}<link rel="canonical" href="{{ page.canonical_url | html.escape }}" />{{ end }}
	                                          {{ for css in css_files }}<link rel="stylesheet" href="{{ css | html.escape }}" />
	                                          {{ end }}
	                                          {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color | html.escape }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color | html.escape }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color | html.escape }} 80%,#000)}</style>{{ end }}
	                                          {{ if theme.accent_color != "" }}<style>:root{--color-accent:{{ theme.accent_color | html.escape }}}</style>{{ end }}
	                                          {{ if site.favicon_url != "" }}<link rel="icon" href="{{ site.favicon_url | html.escape }}" />{{ end }}
	                                          {{ for feed in blog.feeds }}<link rel="alternate" type="{{ feed.type | html.escape }}" title="{{ feed.title | html.escape }}" href="{{ feed.url | html.escape }}" />
	                                          {{ end }}
	                                      </head>
	                                      <body class="landing">
	                                          <header class="site-header">
	                                              <div class="header-inner">
	                                                  <a class="site-logo" href="{{ base_path | html.escape }}/">
	                                                      {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="{{ site.title | html.escape }}" class="site-logo-img" />{{ end }}
	                                                      <span class="site-name">{{ site.title | html.escape }}</span>
	                                                  </a>
	                                                  <div class="header-actions">
	                                                      {{ if versions.size > 0 && theme.show_version_selector }}
	                                                      <div class="version-selector">
	                                                          <button class="version-trigger" aria-label="Select version" aria-expanded="false" aria-haspopup="listbox">
	                                                              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
	                                                              <span class="version-label">{{ current_version | html.escape }}</span>
	                                                              <svg class="version-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"/></svg>
	                                                          </button>
	                                                          <ul class="version-dropdown" role="listbox" hidden>
	                                                              {{ for v in versions }}
	                                                              <li role="option"{{ if v.is_current }} aria-selected="true"{{ end }}>
	                                                                  <a class="version-option{{ if v.is_current }} active{{ end }}" href="{{ v.url | html.escape }}" data-version-slug="{{ v.slug | html.escape }}"{{ if v.is_current }} aria-current="page"{{ end }}>
	                                                                      <span class="version-option-label">{{ v.label | html.escape }}</span>
	                                                                      {{ if v.is_default }}<span class="version-badge version-badge-default">latest</span>{{ end }}
	                                                                      {{ if v.is_prerelease }}<span class="version-badge version-badge-pre">pre</span>{{ end }}
	                                                                  </a>
	                                                              </li>
	                                                              {{ end }}
	                                                          </ul>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if search_enabled }}
	                                                      <button class="search-trigger" aria-label="Search">
	                                                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
	                                                          <span class="search-label">Search</span>
	                                                          <kbd>⌘K</kbd>
	                                                      </button>
	                                                      {{ end }}
	                                                      {{ if theme.style_selector }}
	                                                      <div class="style-selector">
	                                                          <button class="style-trigger" aria-label="Change style" aria-expanded="false" aria-haspopup="menu" aria-controls="styleMenu">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18"/><path d="M9 21V9"/></svg>
	                                                          </button>
	                                                          <div class="style-dropdown" id="styleMenu" role="menu" aria-label="Style" hidden>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="default"><span class="style-preview style-preview-default"></span><span class="style-text"><span class="style-name">Default</span><span class="style-desc">The original look</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="moka"><span class="style-preview style-preview-moka"></span><span class="style-text"><span class="style-name">Moka</span><span class="style-desc">Dense, dark first</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="minimal"><span class="style-preview style-preview-minimal"></span><span class="style-text"><span class="style-name">Minimal</span><span class="style-desc">Quiet, few lines</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="classic"><span class="style-preview style-preview-classic"></span><span class="style-text"><span class="style-name">Classic</span><span class="style-desc">Reference docs</span></span><span class="style-check">&#10003;</span></button>
	                                                              <button class="style-option" type="button" role="menuitemradio" aria-checked="false" data-style-option="soft"><span class="style-preview style-preview-soft"></span><span class="style-text"><span class="style-name">Soft</span><span class="style-desc">Rounded, roomy</span></span><span class="style-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.color_themes != false }}
	                                                      <div class="color-theme-selector">
	                                                          <button class="color-theme-trigger" aria-label="Change color theme" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="13.5" cy="6.5" r="2.5"/><circle cx="17.5" cy="10.5" r="2.5"/><circle cx="8.5" cy="7.5" r="2.5"/><circle cx="6.5" cy="12" r="2.5"/><path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10c.9 0 1.5-.7 1.5-1.5 0-.4-.1-.7-.4-1-.3-.3-.4-.6-.4-1 0-.8.7-1.5 1.5-1.5H16c3.3 0 6-2.7 6-6 0-5.5-4.5-9-10-9z"/></svg>
	                                                          </button>
	                                                          <div class="color-theme-dropdown" hidden>
	                                                              <button class="color-swatch" data-color-theme="ocean" aria-label="Ocean theme" style="background:#0ea5e9"></button>
	                                                              <button class="color-swatch" data-color-theme="emerald" aria-label="Emerald theme" style="background:#10b981"></button>
	                                                              <button class="color-swatch" data-color-theme="violet" aria-label="Violet theme" style="background:#8b5cf6"></button>
	                                                              <button class="color-swatch" data-color-theme="amber" aria-label="Amber theme" style="background:#f59e0b"></button>
	                                                              <button class="color-swatch" data-color-theme="rose" aria-label="Rose theme" style="background:#f43f5e"></button>
	                                                              <button class="color-swatch" data-color-theme="moka-red" aria-label="Moka Red theme" style="background:#d32f2f"></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.code_theme_selector != false }}
	                                                      <div class="code-theme-selector">
	                                                          <button class="code-theme-trigger" aria-label="Change code theme" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>
	                                                          </button>
	                                                          <div class="code-theme-dropdown" hidden>
	                                                              <button class="code-theme-option" data-code-theme="catppuccin-mocha"><span class="code-theme-preview" style="background:#1e1e2e"></span><span class="code-theme-name">Catppuccin Mocha</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="catppuccin-latte"><span class="code-theme-preview" style="background:#eff1f5"></span><span class="code-theme-name">Catppuccin Latte</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="github-dark"><span class="code-theme-preview" style="background:#0d1117"></span><span class="code-theme-name">GitHub Dark</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="github-light"><span class="code-theme-preview" style="background:#ffffff;border:1px solid #d0d7de"></span><span class="code-theme-name">GitHub Light</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="dracula"><span class="code-theme-preview" style="background:#282a36"></span><span class="code-theme-name">Dracula</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="one-dark"><span class="code-theme-preview" style="background:#282c34"></span><span class="code-theme-name">One Dark</span><span class="code-theme-check">&#10003;</span></button>
	                                                              <button class="code-theme-option" data-code-theme="nord"><span class="code-theme-preview" style="background:#2e3440"></span><span class="code-theme-name">Nord</span><span class="code-theme-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.code_style_selector != false }}
	                                                      <div class="code-style-selector">
	                                                          <button class="code-style-trigger" aria-label="Change code block style" aria-expanded="false">
	                                                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>
	                                                          </button>
	                                                          <div class="code-style-dropdown" hidden>
	                                                              <button class="code-style-option" data-code-style="plain"><span class="code-style-indicator"></span><span class="code-style-name">Plain</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="macos"><span class="code-style-indicator"></span><span class="code-style-name">macOS</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="terminal"><span class="code-style-indicator"></span><span class="code-style-name">Terminal</span><span class="code-style-check">&#10003;</span></button>
	                                                              <button class="code-style-option" data-code-style="vscode"><span class="code-style-indicator"></span><span class="code-style-name">VS Code</span><span class="code-style-check">&#10003;</span></button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}
	                                                      {{ if theme.show_dark_mode_toggle }}
	                                                      <button class="theme-toggle" aria-label="Toggle dark mode">
	                                                          <svg class="icon-sun" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
	                                                          <svg class="icon-moon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
	                                                      </button>
	                                                      {{ end }}
	                                                  </div>
	                                              </div>
	                                          </header>

	                                          <!-- Hero Section -->
	                                          <section class="landing-hero">
	                                              <div class="hero-grid-pattern"></div>
	                                              <div class="landing-hero-icon">
	                                                  {{ if site.logo_url != "" }}<img src="{{ site.logo_url | html.escape }}" alt="" style="height:40px;width:auto" />{{ else }}<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/><path d="M8 7h6"/><path d="M8 11h8"/></svg>{{ end }}
	                                              </div>
	                                              <h1 class="landing-hero-title">{{ site.title | html.escape }}</h1>
	                                              <p class="landing-hero-subtitle">{{ page.description | html.escape }}</p>
	                                              <div class="landing-hero-actions">
	                                                  <a class="landing-btn landing-btn-primary" href="{{ if first_nav_route != "" }}{{ first_nav_route | html.escape }}{{ else }}{{ base_path | html.escape }}/docs/{{ end }}">
	                                                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
	                                                      Get Started
	                                                  </a>
	                                                  {{ if site.repo_url != "" }}
	                                                  <a class="landing-btn landing-btn-secondary" href="{{ site.repo_url | html.escape }}" target="_blank" rel="noopener">
	                                                      <svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.2 11.39.6.11.82-.26.82-.58v-2.03c-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76-1.34-1.76-1.09-.74.08-.73.08-.73 1.2.08 1.84 1.24 1.84 1.24 1.07 1.83 2.81 1.3 3.5 1 .1-.78.42-1.3.76-1.6-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.13-.3-.54-1.52.12-3.18 0 0 1-.32 3.3 1.23a11.5 11.5 0 016.02 0c2.28-1.55 3.29-1.23 3.29-1.23.66 1.66.25 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.8 5.63-5.48 5.92.43.37.81 1.1.81 2.22v3.29c0 .32.22.7.82.58A12.01 12.01 0 0024 12c0-6.63-5.37-12-12-12z"/></svg>
	                                                      View on GitHub
	                                                  </a>
	                                                  {{ end }}
	                                              </div>
	                                          </section>

	                                          {{ if page.features.size > 0 }}
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

	                                          {{ if page.features.size > 0 && page.content != "" }}<hr class="landing-divider" />{{ end }}

	                                          <!-- Rendered markdown content from index.md -->
	                                          {{ if page.content != "" }}
	                                          <div class="landing-md-content landing-content">
	                                              {{ page.content }}
	                                          </div>
	                                          {{ end }}

	                                          <!-- Footer -->
	                                          <footer class="landing-footer">
	                                              {{ if theme.show_built_with != false }}<p>Built with <span class="landing-footer-heart">&#x2764;</span> using <a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a> v{{ mokadocs_version }}</p>{{ end }}
	                                              {{ if site.copyright }}<p style="margin-top:0.5rem;">{{ site.copyright }}</p>{{ end }}
	                                          </footer>

	                                          {{ if search_enabled }}
	                                          <div id="searchModal" class="search-modal" hidden>
	                                              <div class="search-backdrop"></div>
	                                              <div class="search-dialog">
	                                                  <div class="search-input-wrap">
	                                                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
	                                                      <input type="text" class="search-input" id="searchInput" placeholder="Search documentation..." autocomplete="off" />
	                                                      <kbd>Esc</kbd>
	                                                  </div>
	                                                  <div class="search-results" id="searchResults"></div>
	                                              </div>
	                                          </div>
	                                          {{ end }}

	                                          {{ for js in js_files }}<script src="{{ js | html.escape }}"></script>
	                                          {{ end }}
	                                      </body>
	                                      </html>
	                                      """;

	private const string _sidebarNav = """

	                                                  <nav class="sidebar-nav" aria-label="Documentation">
	                                                      {{ for item in nav }}
	                                                      <div class="nav-section{{ if item.is_active || item.has_active_child }} active{{ end }}">
	                                                          {{ if item.has_children }}
	                                                          <div class="nav-header">
	                                                              {{ if item.has_page }}
	                                                              <a class="nav-link{{ if item.is_active }} current{{ else if item.has_active_child }} parent-active{{ end }}" href="{{ item.route | html.escape }}">
	                                                                  {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
	                                                                  {{ item.label | html.escape }}
	                                                              </a>
	                                                              {{ else }}
	                                                              <span class="nav-link nav-group-label{{ if item.has_active_child }} parent-active{{ end }}">
	                                                                  {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
	                                                                  {{ item.label | html.escape }}
	                                                              </span>
	                                                              {{ end }}
	                                                              <button class="nav-chevron-btn nav-toggle" aria-label="Toggle {{ item.label | html.escape }}" type="button" aria-expanded="{{ if item.expanded || item.has_active_child }}true{{ else }}false{{ end }}">
	                                                                  <svg class="nav-chevron" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
	                                                              </button>
	                                                          </div>
	                                                          {{ else }}
	                                                          <a class="nav-link{{ if item.is_active }} current{{ end }}" href="{{ item.route | html.escape }}">
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
	                                                                          <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route | html.escape }}">{{ child.label | html.escape }}</a>
	                                                                          {{ else }}
	                                                                          <span class="nav-link nav-group-label">{{ child.label | html.escape }}</span>
	                                                                          {{ end }}
	                                                                          <button class="nav-chevron-btn nav-toggle" aria-label="Toggle {{ child.label | html.escape }}" type="button" aria-expanded="{{ if child.expanded || child.has_active_child }}true{{ else }}false{{ end }}">
	                                                                              <svg class="nav-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
	                                                                          </button>
	                                                                      </div>
	                                                                      <ul class="nav-children{{ if child.expanded || child.has_active_child }} expanded{{ end }}">
	                                                                          {{ for grandchild in child.children }}
	                                                                          <li>
	                                                                              <a class="nav-link{{ if grandchild.is_active }} current{{ end }}" href="{{ grandchild.route | html.escape }}">{{ grandchild.label | html.escape }}</a>
	                                                                          </li>
	                                                                          {{ end }}
	                                                                      </ul>
	                                                                  </div>
	                                                                  {{ else }}
	                                                                  <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route | html.escape }}">
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

	private const string _apiTypeLayout = _defaultLayout; // Reuses default for now

	private const string _headPartial = "";
	private const string _navSidebarPartial = "";
	private const string _tocPartial = "";
	private const string _breadcrumbsPartial = "";
	private const string _footerPartial = "";

	#endregion
}
