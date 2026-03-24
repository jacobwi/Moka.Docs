using Moka.Docs.Rendering.Scriban;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Provides the built-in "Moka" default theme with all templates, CSS, and JS.
///     This enables zero-config builds without requiring a theme directory on disk.
/// </summary>
public static class EmbeddedThemeProvider
{
    #region Embedded CSS

    private const string EmbeddedCss = """
                                       /* MokaDocs Default Theme — "Moka" */
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
                                           --color-text-muted: #94a3b8;
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
                                           --color-text-muted: #64748b;
                                           --color-border: #334155;
                                           --color-border-light: #1e293b;
                                       }

                                       /* Color theme presets — override primary accent colors
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

                                       a { color: var(--color-primary); text-decoration: none; transition: color var(--transition); }
                                       a:hover { color: var(--color-primary-dark); }

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
                                           color: var(--color-primary);
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
                                           color: var(--color-primary);
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

                                       /* Appearance group — wraps color/code/style selectors */
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
                                           color: var(--color-primary);
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
                                       .nav-header .nav-link:hover { color: var(--color-primary); }
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
                                       .nav-group-label.parent-active { color: var(--color-primary); }

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
                                           color: var(--color-primary-dark);
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

                                       /* Copy button on code blocks — uses inherit to match code block theme */
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
                                       .page-content pre:hover .copy-btn { opacity: 1; }
                                       .page-content pre:hover .code-lang { opacity: 0; }

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
                                           color: var(--color-text-muted); opacity: 0.4; border-right: 1px solid var(--color-border);
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
                                           --sh-tag: #f38ba8; --sh-property: #b4befe;
                                       }
                                       [data-code-theme="catppuccin-mocha"] .page-content pre { background: #1e1e2e; border-color: #313244; }
                                       [data-code-theme="catppuccin-mocha"] .page-content pre code { color: #cdd6f4; }

                                       /* Code Theme: Catppuccin Latte */
                                       [data-code-theme="catppuccin-latte"] {
                                           --sh-comment: #9ca0b0; --sh-string: #40a02b; --sh-keyword: #8839ef;
                                           --sh-number: #fe640b; --sh-type: #04a5e5; --sh-function: #1e66f5;
                                           --sh-operator: #179299; --sh-punctuation: #5c5f77; --sh-attr: #df8e1d;
                                           --sh-tag: #d20f39; --sh-property: #7287fd;
                                       }
                                       [data-code-theme="catppuccin-latte"] .page-content pre { background: #eff1f5; border-color: #ccd0da; }
                                       [data-code-theme="catppuccin-latte"] .page-content pre code { color: #4c4f69; }

                                       /* Code Theme: GitHub Dark */
                                       [data-code-theme="github-dark"] {
                                           --sh-comment: #8b949e; --sh-string: #a5d6ff; --sh-keyword: #ff7b72;
                                           --sh-number: #79c0ff; --sh-type: #ffa657; --sh-function: #d2a8ff;
                                           --sh-operator: #ff7b72; --sh-punctuation: #c9d1d9; --sh-attr: #79c0ff;
                                           --sh-tag: #7ee787; --sh-property: #79c0ff;
                                       }
                                       [data-code-theme="github-dark"] .page-content pre { background: #0d1117; border-color: #30363d; }
                                       [data-code-theme="github-dark"] .page-content pre code { color: #c9d1d9; }

                                       /* Code Theme: Dracula */
                                       [data-code-theme="dracula"] {
                                           --sh-comment: #6272a4; --sh-string: #f1fa8c; --sh-keyword: #ff79c6;
                                           --sh-number: #bd93f9; --sh-type: #8be9fd; --sh-function: #50fa7b;
                                           --sh-operator: #ff79c6; --sh-punctuation: #f8f8f2; --sh-attr: #50fa7b;
                                           --sh-tag: #ff79c6; --sh-property: #66d9ef;
                                       }
                                       [data-code-theme="dracula"] .page-content pre { background: #282a36; border-color: #44475a; }
                                       [data-code-theme="dracula"] .page-content pre code { color: #f8f8f2; }

                                       /* Code Theme: One Dark */
                                       [data-code-theme="one-dark"] {
                                           --sh-comment: #5c6370; --sh-string: #98c379; --sh-keyword: #c678dd;
                                           --sh-number: #d19a66; --sh-type: #e5c07b; --sh-function: #61afef;
                                           --sh-operator: #56b6c2; --sh-punctuation: #abb2bf; --sh-attr: #d19a66;
                                           --sh-tag: #e06c75; --sh-property: #e06c75;
                                       }
                                       [data-code-theme="one-dark"] .page-content pre { background: #282c34; border-color: #3e4451; }
                                       [data-code-theme="one-dark"] .page-content pre code { color: #abb2bf; }

                                       /* Code Theme: Nord */
                                       [data-code-theme="nord"] {
                                           --sh-comment: #616e88; --sh-string: #a3be8c; --sh-keyword: #81a1c1;
                                           --sh-number: #b48ead; --sh-type: #8fbcbb; --sh-function: #88c0d0;
                                           --sh-operator: #81a1c1; --sh-punctuation: #d8dee9; --sh-attr: #8fbcbb;
                                           --sh-tag: #bf616a; --sh-property: #88c0d0;
                                       }
                                       [data-code-theme="nord"] .page-content pre { background: #2e3440; border-color: #3b4252; }
                                       [data-code-theme="nord"] .page-content pre code { color: #d8dee9; }

                                       /* Code Theme: GitHub Light */
                                       [data-code-theme="github-light"] {
                                           --sh-comment: #6e7781; --sh-string: #0a3069; --sh-keyword: #cf222e;
                                           --sh-number: #0550ae; --sh-type: #953800; --sh-function: #8250df;
                                           --sh-operator: #cf222e; --sh-punctuation: #24292f; --sh-attr: #0550ae;
                                           --sh-tag: #116329; --sh-property: #0550ae;
                                       }
                                       [data-code-theme="github-light"] .page-content pre { background: #ffffff; border-color: #d0d7de; }
                                       [data-code-theme="github-light"] .page-content pre code { color: #24292f; }

                                       /* ── Code Block Window Styles ─────────────────────────── */

                                       /* macOS style — three traffic-light dots in a title bar */
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

                                       /* Terminal style — dark header with prompt icon */
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

                                       /* VS Code style — tab header with accent bar and activity border */
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
                                       }
                                       .page-content th, .page-content td {
                                           padding: 0.625rem 1rem;
                                           text-align: left;
                                           border: 1px solid var(--color-border);
                                       }
                                       .page-content th { background: var(--color-bg-secondary); font-weight: 600; }
                                       .page-content tr:hover td { background: var(--color-bg-secondary); }

                                       /* Package install widget */
                                       .package-install-widget {
                                           border: 1px solid var(--color-border);
                                           border-radius: var(--radius, 8px);
                                           margin-bottom: 2rem;
                                           overflow: hidden;
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
                                           color: var(--color-primary);
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
                                       .api-member-link { text-decoration: none; color: var(--color-primary); transition: opacity var(--transition); }
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
                                       .source-viewer summary:hover { color: var(--color-primary); }
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

                                       /* Admonitions — Markdig renders ::: type as <div class="type"> */
                                       .page-content > .note,
                                       .page-content > .tip,
                                       .page-content > .warning,
                                       .page-content > .caution,
                                       .page-content > .danger,
                                       .page-content > .info,
                                       .page-content > .important {
                                           margin: 1.5rem 0;
                                           padding: 1rem 1.25rem;
                                           border-radius: var(--radius);
                                           border-left: 4px solid;
                                       }
                                       .page-content > .note { border-color: #3b82f6; background: color-mix(in srgb, #3b82f6 8%, var(--color-bg)); }
                                       .page-content > .tip { border-color: #22c55e; background: color-mix(in srgb, #22c55e 8%, var(--color-bg)); }
                                       .page-content > .warning, .page-content > .caution { border-color: #f59e0b; background: color-mix(in srgb, #f59e0b 8%, var(--color-bg)); }
                                       .page-content > .danger { border-color: #ef4444; background: color-mix(in srgb, #ef4444 8%, var(--color-bg)); }
                                       .page-content > .info, .page-content > .important { border-color: #8b5cf6; background: color-mix(in srgb, #8b5cf6 8%, var(--color-bg)); }

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

                                       /* UI Components — Card */
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

                                       /* UI Components — Steps */
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

                                       /* UI Components — Link Cards */
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

                                       /* UI Components — Code Group */
                                       .component-code-group .tab-content { padding: 0; }
                                       .component-code-group .tab-content pre {
                                           margin: 0;
                                           border: none;
                                           border-radius: 0 0 var(--radius) var(--radius);
                                       }

                                       /* ToC Sidebar — "On this page" */
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
                                           color: var(--color-primary);
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
                                       .toc-back-to-top:hover { color: var(--color-primary); }

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
                                       .breadcrumbs a:hover { color: var(--color-primary); }
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
                                       .edit-link a:hover { color: var(--color-primary); }
                                       .last-updated { display: flex; align-items: center; gap: 0.375rem; }

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
                                       .search-result-section { font-size: 0.8125rem; color: var(--color-primary); margin-left: 0.25rem; }
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
                                       .page-nav-title { font-size: 0.9375rem; font-weight: 600; color: var(--color-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
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

                                       /* Hero — first h1 + first p */
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

                                       /* CTA buttons row — first ul after hero p, or a p with links */
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

                                       /* Feature cards — h2 sections become cards */
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

                                       /* Divider line — hr */
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

                                       /* Landing code blocks — reuse page-content pre styles */
                                       .landing-content pre { position: relative; margin: 2rem auto; padding: 1.25rem 1.5rem; border-radius: var(--radius); overflow-x: auto; border: 1px solid var(--color-border); max-width: 720px; background: var(--color-bg-secondary); }
                                       .landing-content pre code { font-family: var(--font-mono); font-size: 0.875rem; line-height: 1.7; background: none; padding: 0; border: none; }
                                       .landing-content pre .copy-btn { position: absolute; top: 0.5rem; right: 0.5rem; background: rgba(255,255,255,0.15); border: 1px solid rgba(255,255,255,0.25); color: rgba(255,255,255,0.75); padding: 0.25rem 0.625rem; border-radius: var(--radius-sm); font-size: 0.75rem; font-family: var(--font-mono); cursor: pointer; transition: all var(--transition); }
                                       .landing-content pre .copy-btn:hover { background: rgba(255,255,255,0.25); color: #fff; border-color: rgba(255,255,255,0.4); }
                                       .landing-content pre:hover .code-lang { opacity: 0; }
                                       .landing-content pre .code-lang { position: absolute; top: 0.5rem; right: 0.5rem; font-size: 0.6875rem; font-family: var(--font-mono); text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-muted); user-select: none; transition: opacity var(--transition); }
                                       .landing-content pre.has-line-numbers { padding-left: 3.5rem; }
                                       .landing-content pre .line-numbers { position: absolute; top: 1.25rem; left: 0; width: 2.75rem; text-align: right; padding-right: 0.75rem; font-family: var(--font-mono); font-size: 0.875rem; line-height: 1.7; color: var(--color-text-muted); border-right: 1px solid var(--color-border); user-select: none; }
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
                                       .version-option.active { color: var(--color-primary); font-weight: 600; }
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
                                           max-height: 2000px;
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
                                           transition: opacity var(--transition);
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
                                           opacity: 0.35 !important;
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
                                           transition: all 0.2s ease !important;
                                       }

                                       /* --- 6. TOC scroll spy --- */
                                       .toc-item a {
                                           transition: all 0.25s ease !important;
                                           border-left: 2px solid transparent !important;
                                           margin-left: -2px;
                                       }
                                       .toc-item a.active {
                                           color: var(--color-primary) !important;
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
                                               animation-iteration-count: 1 !important;
                                               transition-duration: 0.01ms !important;
                                               scroll-behavior: auto !important;
                                           }
                                       }
                                       [data-no-animations] *, [data-no-animations] *::before, [data-no-animations] *::after {
                                           animation-duration: 0.01ms !important;
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
                                           gap: 0.5rem;
                                           padding: 0.8rem 2rem;
                                           border-radius: var(--radius);
                                           font-weight: 600;
                                           font-size: 1rem;
                                           text-decoration: none;
                                           transition: all 150ms ease;
                                           border: none;
                                           cursor: pointer;
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

                                       /* Code preview section */
                                       .landing-code-section {
                                           max-width: 720px;
                                           margin: 0 auto;
                                           padding: 2rem 2rem 4rem;
                                           text-align: center;
                                       }
                                       .landing-code-title {
                                           font-size: 1.75rem;
                                           font-weight: 700;
                                           margin-bottom: 0.75rem;
                                       }
                                       .landing-code-subtitle {
                                           color: var(--color-text-secondary);
                                           margin-bottom: 2rem;
                                           font-size: 1.0625rem;
                                       }
                                       .landing-code-block {
                                           position: relative;
                                           text-align: left;
                                           background: #1e293b;
                                           border-radius: var(--radius);
                                           padding: 1.5rem;
                                           overflow-x: auto;
                                           border: 1px solid var(--color-border);
                                           box-shadow: 0 8px 30px rgba(0,0,0,0.12);
                                       }
                                       [data-theme="dark"] .landing-code-block {
                                           background: #0c1222;
                                           border-color: #334155;
                                           box-shadow: 0 8px 30px rgba(0,0,0,0.3);
                                       }
                                       .landing-code-block pre {
                                           margin: 0;
                                           padding: 0;
                                           background: none;
                                           border: none;
                                           overflow: visible;
                                       }
                                       .landing-code-block code {
                                           font-family: var(--font-mono);
                                           font-size: 0.875rem;
                                           line-height: 1.8;
                                           color: #e2e8f0;
                                           background: none;
                                           padding: 0;
                                           border: none;
                                       }
                                       .landing-code-block .code-comment { color: #64748b; }
                                       .landing-code-block .code-key { color: #7dd3fc; }
                                       .landing-code-block .code-string { color: #86efac; }
                                       .landing-code-block .code-lang-badge {
                                           position: absolute;
                                           top: 0.625rem;
                                           right: 0.75rem;
                                           font-family: var(--font-mono);
                                           font-size: 0.6875rem;
                                           text-transform: uppercase;
                                           letter-spacing: 0.05em;
                                           color: #64748b;
                                           user-select: none;
                                       }

                                       /* Landing copy button — high contrast on dark code blocks */
                                       .landing-code-block .copy-btn {
                                           position: absolute;
                                           top: 0.5rem;
                                           right: 0.5rem;
                                           background: rgba(255,255,255,0.15);
                                           border: 1px solid rgba(255,255,255,0.2);
                                           color: rgba(255,255,255,0.7);
                                           padding: 0.3rem 0.7rem;
                                           border-radius: var(--radius-sm);
                                           font-size: 0.75rem;
                                           font-family: var(--font-mono);
                                           cursor: pointer;
                                           transition: all 150ms ease;
                                           opacity: 0;
                                       }
                                       .landing-code-block:hover .copy-btn { opacity: 1; }
                                       .landing-code-block:hover .code-lang-badge { opacity: 0; }
                                       .landing-code-block .copy-btn:hover {
                                           background: rgba(255,255,255,0.25);
                                           color: #fff;
                                           border-color: rgba(255,255,255,0.35);
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
                                           .landing-code-section { padding: 1rem 1rem 2rem; }
                                           .landing-md-content { padding: 0 1rem 2rem; }
                                       }
                                       """;
    #endregion

    #region Embedded JS

    private const string EmbeddedJs = """
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

                                          // Color theme selector
                                          (function() {
                                              var colorTheme = localStorage.getItem('mokadocs-color-theme') || 'ocean';
                                              html.setAttribute('data-color-theme', colorTheme);

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
                                                      // Close color theme dropdown if open
                                                      var colorDd = document.querySelector('.color-theme-dropdown');
                                                      if (colorDd) { colorDd.setAttribute('hidden', ''); }
                                                      var colorTr = document.querySelector('.color-theme-trigger');
                                                      if (colorTr) { colorTr.setAttribute('aria-expanded', 'false'); }

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
                                                      // Close other dropdowns
                                                      var colorDd = document.querySelector('.color-theme-dropdown');
                                                      if (colorDd) { colorDd.setAttribute('hidden', ''); }
                                                      var colorTr = document.querySelector('.color-theme-trigger');
                                                      if (colorTr) { colorTr.setAttribute('aria-expanded', 'false'); }
                                                      var codeDd = document.querySelector('.code-theme-dropdown');
                                                      if (codeDd) { codeDd.setAttribute('hidden', ''); }
                                                      var codeTr = document.querySelector('.code-theme-trigger');
                                                      if (codeTr) { codeTr.setAttribute('aria-expanded', 'false'); }

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

                                          // Mobile nav toggle
                                          const mobileToggle = document.querySelector('.mobile-nav-toggle');
                                          const sidebar = document.getElementById('sidebar');
                                          if (mobileToggle && sidebar) {
                                              mobileToggle.addEventListener('click', () => sidebar.classList.toggle('open'));
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
                                              let html = escapeHtml(code);
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
                                              // Build result from back to front
                                              let result = code;
                                              for (let i = filtered.length - 1; i >= 0; i--) {
                                                  const r = filtered[i];
                                                  const before = result.substring(0, r.start);
                                                  const token = escapeHtml(r.text);
                                                  const after = result.substring(r.end);
                                                  result = before + '<span class="token ' + r.cls + '">' + token + '</span>' + after;
                                              }
                                              return result;
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

                                              // Add copy button
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

                                              // Add line numbers for multi-line code
                                              const lines = block.textContent.split('\n');
                                              if (lines.length > 3) {
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
                                              const headers = tabGroup.querySelectorAll('.tab-header');
                                              const contents = tabGroup.querySelectorAll('.tab-content');
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
                                                  let score = 0;
                                                  let matched = true;
                                                  for (const term of terms) {
                                                      const inTitle = title.includes(term);
                                                      const inSection = section.includes(term);
                                                      const inContent = content.includes(term);
                                                      if (!inTitle && !inSection && !inContent) { matched = false; break; }
                                                      if (inTitle) score += 10;
                                                      if (inSection) score += 5;
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
                                                  let snippet = esc(e.c || '');
                                                  const firstTerm = terms[0] || '';
                                                  const idx = snippet.toLowerCase().indexOf(firstTerm);
                                                  if (idx > 30) snippet = '...' + snippet.slice(idx - 30);
                                                  if (snippet.length > 120) snippet = snippet.slice(0, 120) + '...';
                                                  for (const term of terms) {
                                                      snippet = snippet.replace(new RegExp('(' + term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi'), '<mark>$1</mark>');
                                                  }
                                                  const sectionPart = e.s ? '<span class="search-result-section">&rsaquo; ' + esc(e.s) + '</span>' : '';
                                                  return '<a class="search-result' + (i === 0 ? ' active' : '') + '" href="' + esc(e.r) + '">' +
                                                      '<div><span class="search-result-title">' + esc(e.t) + '</span>' + sectionPart +
                                                      ' <span class="search-result-category">' + esc(e.g) + '</span></div>' +
                                                      (snippet ? '<div class="search-result-snippet">' + snippet + '</div>' : '') +
                                                      '</a>';
                                              }).join('');
                                          }

                                          function esc(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

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

                                          document.addEventListener('keydown', (e) => {
                                              if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                                                  e.preventDefault();
                                                  searchModal && !searchModal.hidden ? closeSearch() : openSearch();
                                              }
                                          });

                                          // Sidebar nav expand/collapse — toggle sections
                                          document.querySelectorAll('.nav-toggle').forEach(toggle => {
                                              // Find the nav-children list — it's a sibling of the toggle or its parent (.nav-header)
                                              const section = toggle.closest('.nav-section');
                                              const children = section?.querySelector('.nav-children');
                                              if (children) {
                                                  const chevron = toggle.querySelector('.nav-chevron');
                                                  toggle.addEventListener('click', (e) => {
                                                      e.stopPropagation();
                                                      const isExpanded = children.classList.toggle('expanded');
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
                                                  versionTrigger.setAttribute('aria-expanded', String(!isOpen));
                                                  versionDropdown.hidden = isOpen;
                                              });
                                              document.addEventListener('click', (e) => {
                                                  if (!e.target.closest('.version-selector')) {
                                                      versionTrigger.setAttribute('aria-expanded', 'false');
                                                      versionDropdown.hidden = true;
                                                  }
                                              });
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
                ["default"] = DefaultLayout,
                ["landing"] = LandingLayout,
                ["api-type"] = ApiTypeLayout
            },
            Partials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["head"] = HeadPartial,
                ["nav-sidebar"] = NavSidebarPartial,
                ["toc"] = TocPartial,
                ["breadcrumbs"] = BreadcrumbsPartial,
                ["footer"] = FooterPartial
            },
            CssFiles = ["/_theme/css/main.css"],
            JsFiles = ["/_theme/js/main.js"]
        };
    }

    /// <summary>
    ///     Returns the embedded CSS content.
    /// </summary>
    public static string GetCss()
    {
        return EmbeddedCss;
    }

    /// <summary>
    ///     Returns the embedded JS content.
    /// </summary>
    public static string GetJs()
    {
        return EmbeddedJs;
    }

    #region Layout Templates

    private const string DefaultLayout = """
                                         <!DOCTYPE html>
                                         <html lang="en" data-theme="light" data-code-theme="{{ theme.code_theme }}" data-code-style="{{ theme.code_style }}"{{ if base_path != "" }} data-base-path="{{ base_path }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}>
                                         <head>
                                             <script>try{const t=localStorage.getItem('mokadocs-theme')||(matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',t);const c=localStorage.getItem('mokadocs-color-theme')||'ocean';document.documentElement.setAttribute('data-color-theme',c);const ct=localStorage.getItem('mokadocs-code-theme');if(ct)document.documentElement.setAttribute('data-code-theme',ct);const cs=localStorage.getItem('mokadocs-code-style')||'{{ theme.code_style }}';document.documentElement.setAttribute('data-code-style',cs)}catch(e){}</script>
                                             <meta charset="utf-8" />
                                             <meta name="viewport" content="width=device-width, initial-scale=1" />
                                             <title>{{ page.title }} — {{ site.title }}</title>
                                             <meta name="description" content="{{ page.description }}" />
                                             {{ if site.url }}<link rel="canonical" href="{{ site.url }}{{ page.route }}" />{{ end }}
                                             {{ if site.url }}<meta property="og:title" content="{{ page.title }}" />
                                             <meta property="og:description" content="{{ page.description }}" />
                                             <meta property="og:url" content="{{ site.url }}{{ page.route }}" />
                                             <meta property="og:site_name" content="{{ site.title }}" />
                                             <meta property="og:type" content="article" />
                                             <meta name="twitter:card" content="summary" />
                                             <meta name="twitter:title" content="{{ page.title }}" />
                                             <meta name="twitter:description" content="{{ page.description }}" />{{ end }}
                                             <style>{{ css_inline }}</style>
                                             {{ for css in css_files }}<link rel="stylesheet" href="{{ css }}" />
                                             {{ end }}
                                             {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color }} 80%,#000)}</style>{{ end }}
                                             {{ if site.favicon != "" }}<link rel="icon" href="{{ base_path }}/{{ site.favicon }}" />{{ end }}
                                         </head>
                                         <body>
                                             <header class="site-header">
                                                 <div class="header-inner">
                                                     <a class="site-logo" href="{{ base_path }}/">
                                                         {{ if site.logo != "" }}<img src="{{ site.logo }}" alt="{{ site.title }}" class="site-logo-img" />{{ else }}<svg class="site-logo-icon" width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/><path d="M8 7h6"/><path d="M8 11h8"/></svg>{{ end }}
                                                         <span class="site-name">{{ site.title }}</span>
                                                     </a>
                                                     <div class="header-actions">
                                                         {{ if versions.size > 0 }}
                                                         <div class="version-selector">
                                                             <button class="version-trigger" aria-label="Select version" aria-expanded="false" aria-haspopup="listbox">
                                                                 <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
                                                                 <span class="version-label">{{ current_version }}</span>
                                                                 <svg class="version-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"/></svg>
                                                             </button>
                                                             <ul class="version-dropdown" role="listbox" hidden>
                                                                 {{ for v in versions }}
                                                                 <li role="option"{{ if v.label == current_version }} aria-selected="true"{{ end }}>
                                                                     <a class="version-option{{ if v.label == current_version }} active{{ end }}" href="{{ if v.is_default }}{{ base_path }}/{{ else }}{{ base_path }}/{{ v.slug }}/{{ end }}" data-version-slug="{{ v.slug }}">
                                                                         <span class="version-option-label">{{ v.label }}</span>
                                                                         {{ if v.is_default }}<span class="version-badge version-badge-default">latest</span>{{ end }}
                                                                         {{ if v.is_prerelease }}<span class="version-badge version-badge-pre">pre</span>{{ end }}
                                                                     </a>
                                                                 </li>
                                                                 {{ end }}
                                                             </ul>
                                                         </div>
                                                         {{ end }}
                                                         <button class="search-trigger" aria-label="Search" data-shortcut="Ctrl+K">
                                                             <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
                                                             <span class="search-label">Search</span>
                                                             <kbd>⌘K</kbd>
                                                         </button>
                                                         <div class="appearance-group">
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
                                                         <button class="theme-toggle" aria-label="Toggle dark mode">
                                                             <svg class="icon-sun" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
                                                             <svg class="icon-moon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
                                                         </button>
                                                         <button class="mobile-nav-toggle" aria-label="Toggle navigation">
                                                             <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 12h18M3 6h18M3 18h18"/></svg>
                                                         </button>
                                                     </div>
                                                 </div>
                                             </header>

                                             <div class="site-container">
                                                 <aside class="sidebar" id="sidebar">
                                                     <nav class="sidebar-nav" aria-label="Documentation">
                                                         {{ for item in nav }}
                                                         <div class="nav-section{{ if item.is_active || item.has_active_child }} active{{ end }}">
                                                             {{ if item.has_children }}
                                                             <div class="nav-header">
                                                                 {{ if item.has_page }}
                                                                 <a class="nav-link{{ if item.is_active }} current{{ else if item.has_active_child }} parent-active{{ end }}" href="{{ item.route }}">
                                                                     {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
                                                                     {{ item.label }}
                                                                 </a>
                                                                 {{ else }}
                                                                 <span class="nav-link nav-group-label{{ if item.has_active_child }} parent-active{{ end }}">
                                                                     {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
                                                                     {{ item.label }}
                                                                 </span>
                                                                 {{ end }}
                                                                 <button class="nav-chevron-btn nav-toggle" aria-label="Toggle {{ item.label }}" type="button" aria-expanded="{{ if item.expanded || item.has_active_child }}true{{ else }}false{{ end }}">
                                                                     <svg class="nav-chevron" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
                                                                 </button>
                                                             </div>
                                                             {{ else }}
                                                             <a class="nav-link{{ if item.is_active }} current{{ end }}" href="{{ item.route }}">
                                                                 {{ if item.icon != "" }}<span class="nav-icon">{{ item.icon }}</span>{{ end }}
                                                                 {{ item.label }}
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
                                                                             <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route }}">{{ child.label }}</a>
                                                                             {{ else }}
                                                                             <span class="nav-link nav-group-label">{{ child.label }}</span>
                                                                             {{ end }}
                                                                             <button class="nav-chevron-btn nav-toggle" aria-label="Toggle {{ child.label }}" type="button" aria-expanded="{{ if child.expanded || child.has_active_child }}true{{ else }}false{{ end }}">
                                                                                 <svg class="nav-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
                                                                             </button>
                                                                         </div>
                                                                         <ul class="nav-children{{ if child.expanded || child.has_active_child }} expanded{{ end }}">
                                                                             {{ for grandchild in child.children }}
                                                                             <li>
                                                                                 <a class="nav-link{{ if grandchild.is_active }} current{{ end }}" href="{{ grandchild.route }}">{{ grandchild.label }}</a>
                                                                             </li>
                                                                             {{ end }}
                                                                         </ul>
                                                                     </div>
                                                                     {{ else }}
                                                                     <a class="nav-link{{ if child.is_active }} current{{ end }}" href="{{ child.route }}">
                                                                         {{ if child.icon != "" }}<span class="nav-icon">{{ child.icon }}</span>{{ end }}
                                                                         {{ child.label }}
                                                                     </a>
                                                                     {{ end }}
                                                                 </li>
                                                                 {{ end }}
                                                             </ul>
                                                             {{ end }}
                                                         </div>
                                                         {{ end }}
                                                     </nav>
                                                 </aside>

                                                 <main class="content">
                                                     <article class="page-content">
                                                         {{ if breadcrumbs.size > 1 }}
                                                         <nav class="breadcrumbs" aria-label="Breadcrumb">
                                                             <ol>
                                                             {{ for crumb in breadcrumbs }}
                                                                 <li>
                                                                 {{ if crumb.is_current }}
                                                                     <span aria-current="page">{{ crumb.label }}</span>
                                                                 {{ else if crumb.url != "" }}
                                                                     <a href="{{ crumb.url }}">{{ crumb.label }}</a>
                                                                 {{ else }}
                                                                     <span>{{ crumb.label }}</span>
                                                                 {{ end }}
                                                                 </li>
                                                             {{ end }}
                                                             </ol>
                                                         </nav>
                                                         {{ end }}

                                                         {{ if package && page.route == "/api" }}
                                                         <div class="package-install-widget">
                                                             <div class="install-tabs" role="tablist">
                                                                 <button class="install-tab active" role="tab" aria-selected="true" data-tab="pm">Package Manager</button>
                                                                 <button class="install-tab" role="tab" aria-selected="false" data-tab="cli">.NET CLI</button>
                                                                 <button class="install-tab" role="tab" aria-selected="false" data-tab="ref">PackageReference</button>
                                                             </div>
                                                             <div class="install-panel active" data-panel="pm">
                                                                 <pre><code>Install-Package {{ package.name }} -Version {{ package.version }}</code></pre>
                                                                 <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
                                                             </div>
                                                             <div class="install-panel" data-panel="cli">
                                                                 <pre><code>dotnet add package {{ package.name }} --version {{ package.version }}</code></pre>
                                                                 <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
                                                             </div>
                                                             <div class="install-panel" data-panel="ref">
                                                                 <pre><code>&lt;PackageReference Include="{{ package.name }}" Version="{{ package.version }}" /&gt;</code></pre>
                                                                 <button class="install-copy-btn" aria-label="Copy to clipboard">Copy</button>
                                                             </div>
                                                         </div>
                                                         {{ end }}

                                                         {{ page.content }}

                                                         {{ if edit_url }}
                                                         <div class="edit-link">
                                                             <a href="{{ edit_url }}" target="_blank" rel="noopener">
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
                                                                 <a href="#{{ entry.id }}">{{ entry.text }}</a>
                                                                 {{ if entry.children.size > 0 }}
                                                                 <ul>
                                                                     {{ for child in entry.children }}
                                                                     {{ if child.level <= theme.toc_depth }}
                                                                     <li class="toc-item toc-level-{{ child.level }}">
                                                                         <a href="#{{ child.id }}">{{ child.text }}</a>
                                                                         {{ if child.children.size > 0 }}
                                                                         <ul>
                                                                             {{ for grandchild in child.children }}
                                                                             {{ if grandchild.level <= theme.toc_depth }}
                                                                             <li class="toc-item toc-level-{{ grandchild.level }}">
                                                                                 <a href="#{{ grandchild.id }}">{{ grandchild.text }}</a>
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

                                             <nav class="page-nav">
                                               {{ if prev_page }}
                                               <a class="page-nav-link page-nav-prev" href="{{ prev_page.route }}">
                                                 <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"></polyline></svg>
                                                 <div class="page-nav-text">
                                                   <span class="page-nav-label">Previous</span>
                                                   <span class="page-nav-title">{{ prev_page.title }}</span>
                                                 </div>
                                               </a>
                                               {{ else }}
                                               <span class="page-nav-spacer"></span>
                                               {{ end }}
                                               {{ if next_page }}
                                               <a class="page-nav-link page-nav-next" href="{{ next_page.route }}">
                                                 <div class="page-nav-text">
                                                   <span class="page-nav-label">Next</span>
                                                   <span class="page-nav-title">{{ next_page.title }}</span>
                                                 </div>
                                                 <svg class="page-nav-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>
                                               </a>
                                               {{ else }}
                                               <span class="page-nav-spacer"></span>
                                               {{ end }}
                                             </nav>

                                             <footer class="site-footer">
                                                 <div class="footer-inner">
                                                     {{ if site.copyright }}<span>{{ site.copyright }}</span>{{ end }}
                                                     {{ if theme.social_links.size > 0 }}<div class="footer-social">{{ for link in theme.social_links }}<a href="{{ link.url }}" target="_blank" rel="noopener noreferrer" aria-label="{{ link.icon }}">{{ link.icon_svg }}</a>{{ end }}</div>{{ end }}
                                                     <span class="built-with">Built with <a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a></span>
                                                 </div>
                                             </footer>

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
                                            <button class="back-to-top" id="backToTop" aria-label="Back to top">&#8593;</button>

                                             {{ for js in js_files }}<script src="{{ js }}"></script>
                                             {{ end }}
                                             <script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
                                             <script>
                                             (function() {
                                                 if (!document.querySelector('.mermaid')) return;
                                                 var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
                                                 mermaid.initialize({ startOnLoad: true, theme: isDark ? 'dark' : 'default' });

                                                 // Re-render diagrams when theme toggles
                                                 var observer = new MutationObserver(function(mutations) {
                                                     mutations.forEach(function(m) {
                                                         if (m.attributeName === 'data-theme') {
                                                             var nowDark = document.documentElement.getAttribute('data-theme') === 'dark';
                                                             mermaid.initialize({ startOnLoad: false, theme: nowDark ? 'dark' : 'default' });
                                                             document.querySelectorAll('.mermaid').forEach(function(el) {
                                                                 var orig = el.getAttribute('data-mermaid-src');
                                                                 if (orig) {
                                                                     el.removeAttribute('data-processed');
                                                                     el.innerHTML = orig;
                                                                 }
                                                             });
                                                             mermaid.run();
                                                         }
                                                     });
                                                 });
                                                 // Store original source before Mermaid replaces it with SVG
                                                 document.querySelectorAll('.mermaid').forEach(function(el) {
                                                     el.setAttribute('data-mermaid-src', el.textContent);
                                                 });
                                                 observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
                                             })();
                                             </script>
                                         </body>
                                         </html>
                                         """;

    private const string LandingLayout = """
                                         <!DOCTYPE html>
                                         <html lang="en" data-theme="light" data-code-theme="{{ theme.code_theme }}" data-code-style="{{ theme.code_style }}"{{ if base_path != "" }} data-base-path="{{ base_path }}"{{ end }}{{ if theme.show_animations == false }} data-no-animations{{ end }}>
                                         <head>
                                             <script>try{const t=localStorage.getItem('mokadocs-theme')||(matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',t);const c=localStorage.getItem('mokadocs-color-theme')||'ocean';document.documentElement.setAttribute('data-color-theme',c);const ct=localStorage.getItem('mokadocs-code-theme');if(ct)document.documentElement.setAttribute('data-code-theme',ct);const cs=localStorage.getItem('mokadocs-code-style')||'{{ theme.code_style }}';document.documentElement.setAttribute('data-code-style',cs)}catch(e){}</script>
                                             <meta charset="utf-8" />
                                             <meta name="viewport" content="width=device-width, initial-scale=1" />
                                             <title>{{ site.title }}{{ if page.description != "" }} — {{ page.description }}{{ end }}</title>
                                             <meta name="description" content="{{ page.description }}" />
                                             {{ if site.url != "" }}<link rel="canonical" href="{{ site.url }}{{ page.route }}" />{{ end }}
                                             {{ for css in css_files }}<link rel="stylesheet" href="{{ css }}" />
                                             {{ end }}
                                             <style>{{ css_inline }}</style>
                                             {{ if theme.primary_color != "" }}<style>:root{--color-primary:{{ theme.primary_color }};--color-primary-light:color-mix(in srgb,{{ theme.primary_color }} 75%,#fff);--color-primary-dark:color-mix(in srgb,{{ theme.primary_color }} 80%,#000)}</style>{{ end }}
                                             {{ if site.favicon != "" }}<link rel="icon" href="{{ base_path }}/{{ site.favicon }}" />{{ end }}
                                         </head>
                                         <body class="landing">
                                             <header class="site-header">
                                                 <div class="header-inner">
                                                     <a class="site-logo" href="{{ base_path }}/">
                                                         {{ if site.logo != "" }}<img src="{{ site.logo }}" alt="{{ site.title }}" class="site-logo-img" />{{ end }}
                                                         <span class="site-name">{{ site.title }}</span>
                                                     </a>
                                                     <div class="header-actions">
                                                         <button class="search-trigger" aria-label="Search">
                                                             <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
                                                             <span class="search-label">Search</span>
                                                             <kbd>⌘K</kbd>
                                                         </button>
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
                                                         <button class="theme-toggle" aria-label="Toggle dark mode">
                                                             <svg class="icon-sun" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
                                                             <svg class="icon-moon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
                                                         </button>
                                                     </div>
                                                 </div>
                                             </header>

                                             <!-- Hero Section -->
                                             <section class="landing-hero">
                                                 <div class="hero-grid-pattern"></div>
                                                 <div class="landing-hero-icon">
                                                     {{ if site.logo != "" }}<img src="{{ site.logo }}" alt="" style="height:40px;width:auto" />{{ else }}<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/><path d="M8 7h6"/><path d="M8 11h8"/></svg>{{ end }}
                                                 </div>
                                                 <h1 class="landing-hero-title">{{ site.title }}</h1>
                                                 <p class="landing-hero-subtitle">{{ page.description }}</p>
                                                 <div class="landing-hero-actions">
                                                     <a class="landing-btn landing-btn-primary" href="{{ nav[0].route ?? (base_path + '/docs/') }}">
                                                         <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
                                                         Get Started
                                                     </a>
                                                     {{ if site.repo_url != "" }}
                                                     <a class="landing-btn landing-btn-secondary" href="{{ site.repo_url }}" target="_blank" rel="noopener">
                                                         <svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.2 11.39.6.11.82-.26.82-.58v-2.03c-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76-1.34-1.76-1.09-.74.08-.73.08-.73 1.2.08 1.84 1.24 1.84 1.24 1.07 1.83 2.81 1.3 3.5 1 .1-.78.42-1.3.76-1.6-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.13-.3-.54-1.52.12-3.18 0 0 1-.32 3.3 1.23a11.5 11.5 0 016.02 0c2.28-1.55 3.29-1.23 3.29-1.23.66 1.66.25 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.8 5.63-5.48 5.92.43.37.81 1.1.81 2.22v3.29c0 .32.22.7.82.58A12.01 12.01 0 0024 12c0-6.63-5.37-12-12-12z"/></svg>
                                                         View on GitHub
                                                     </a>
                                                     {{ end }}
                                                 </div>
                                             </section>

                                             <!-- Features Grid -->
                                             <section class="landing-features">
                                                 <h2 class="landing-features-title">Everything you need</h2>
                                                 <p class="landing-features-subtitle">A modern documentation toolkit built for .NET developers</p>
                                                 <div class="landing-features-grid">
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">C#</div>
                                                         <div class="landing-feature-name">C# API Reference</div>
                                                         <p class="landing-feature-desc">Auto-generate API docs from your .NET assemblies with full type information and XML doc comments.</p>
                                                     </div>
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">&lt;/&gt;</div>
                                                         <div class="landing-feature-name">Beautiful Themes</div>
                                                         <p class="landing-feature-desc">Ship with a polished default theme or build your own with Scriban templates and CSS variables.</p>
                                                     </div>
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">&#x26A1;</div>
                                                         <div class="landing-feature-name">Instant Search</div>
                                                         <p class="landing-feature-desc">Client-side full-text search with zero external dependencies. Works offline and loads instantly.</p>
                                                     </div>
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">&#x263D;</div>
                                                         <div class="landing-feature-name">Dark Mode</div>
                                                         <p class="landing-feature-desc">Automatic light and dark mode with system preference detection and manual toggle support.</p>
                                                     </div>
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">v2</div>
                                                         <div class="landing-feature-name">Versioning</div>
                                                         <p class="landing-feature-desc">Maintain docs for multiple versions side by side. Readers can switch between releases seamlessly.</p>
                                                     </div>
                                                     <div class="landing-feature-card">
                                                         <div class="landing-feature-icon">&#x2699;</div>
                                                         <div class="landing-feature-name">Plugin System</div>
                                                         <p class="landing-feature-desc">Extend the build pipeline with custom plugins for content transforms, assets, and more.</p>
                                                     </div>
                                                 </div>
                                             </section>

                                             <!-- Code Preview -->
                                             <section class="landing-code-section">
                                                 <h2 class="landing-code-title">Simple configuration</h2>
                                                 <p class="landing-code-subtitle">Get a docs site running in under a minute</p>
                                                 <div class="landing-code-block">
                                                     <span class="code-lang-badge">yaml</span>
                                                     <pre style="margin:0;padding:0;background:none;border:none"><code><span class="code-comment"># mokadocs.yaml</span>&#10;<span class="code-key">title:</span> <span class="code-string">My Project</span>&#10;<span class="code-key">description:</span> <span class="code-string">Docs for my .NET library</span>&#10;<span class="code-key">theme:</span> <span class="code-string">default</span>&#10;<span class="code-key">nav:</span>&#10;  - <span class="code-key">label:</span> <span class="code-string">Getting Started</span>&#10;    <span class="code-key">path:</span> <span class="code-string">docs/getting-started.md</span>&#10;  - <span class="code-key">label:</span> <span class="code-string">API Reference</span>&#10;    <span class="code-key">path:</span> <span class="code-string">api/</span></code></pre>
                                                 </div>
                                             </section>

                                             <hr class="landing-divider" />

                                             <!-- Rendered markdown content from index.md -->
                                             {{ if page.content != "" }}
                                             <div class="landing-md-content landing-content">
                                                 {{ page.content }}
                                             </div>
                                             {{ end }}

                                             <!-- Footer -->
                                             <footer class="landing-footer">
                                                 <p>Built with <span class="landing-footer-heart">&#x2764;</span> using <a href="https://github.com/jacobwi/Moka.Docs">MokaDocs</a></p>
                                                 {{ if site.copyright }}<p style="margin-top:0.5rem;">{{ site.copyright }}</p>{{ end }}
                                             </footer>

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

                                             {{ for js in js_files }}<script src="{{ js }}"></script>
                                             {{ end }}
                                         </body>
                                         </html>
                                         """;

    private const string ApiTypeLayout = DefaultLayout; // Reuses default for now

    private const string HeadPartial = "";
    private const string NavSidebarPartial = "";
    private const string TocPartial = "";
    private const string BreadcrumbsPartial = "";
    private const string FooterPartial = "";

    #endregion
}