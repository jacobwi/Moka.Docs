namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
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
	                                        --header-offset: var(--header-height);
	                                        --header-button: 36px;
	                                        --layout-max-width: 1440px;
	                                        --content-max-width: 780px;
	                                        --font-body: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
	                                        --font-mono: 'JetBrains Mono', 'Fira Code', 'Cascadia Code', monospace;
	                                        --radius: 8px;
	                                        --radius-sm: 4px;
	                                        --shadow-sm: 0 1px 2px rgba(0,0,0,0.05);
	                                        --shadow-md: 0 4px 6px -1px rgba(0,0,0,0.1);
	                                        --shadow-lg: 0 10px 25px -3px rgba(0,0,0,0.15);
	                                        --transition: 150ms ease;
	                                        /* Stacking order. The header (and the menus that open from it) sits above the
	                                           drawers below it, so a menu is never covered by the open "On this page" drawer. */
	                                        --z-fab: 50;
	                                        --z-backdrop: 80;
	                                        --z-drawer: 90;
	                                        --z-header: 100;
	                                        --z-popover: 110;
	                                        --z-modal: 200;
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
	                                    html[data-color-theme="slate"] {
	                                        --color-primary: #64748b !important;
	                                        --color-primary-light: #94a3b8 !important;
	                                        --color-primary-dark: #475569 !important;
	                                        --gradient-secondary: #94a3b8;
	                                        --gradient-tertiary: #475569;
	                                    }
	                                    html[data-color-theme="teal"] {
	                                        --color-primary: #14b8a6 !important;
	                                        --color-primary-light: #2dd4bf !important;
	                                        --color-primary-dark: #0d9488 !important;
	                                        --gradient-secondary: #06b6d4;
	                                        --gradient-tertiary: #10b981;
	                                    }
	                                    html[data-color-theme="indigo"] {
	                                        --color-primary: #6366f1 !important;
	                                        --color-primary-light: #818cf8 !important;
	                                        --color-primary-dark: #4f46e5 !important;
	                                        --gradient-secondary: #8b5cf6;
	                                        --gradient-tertiary: #3b82f6;
	                                    }
	                                    html[data-color-theme="orange"] {
	                                        --color-primary: #f97316 !important;
	                                        --color-primary-light: #fb923c !important;
	                                        --color-primary-dark: #ea580c !important;
	                                        --gradient-secondary: #f59e0b;
	                                        --gradient-tertiary: #ef4444;
	                                    }
	                                    html[data-color-theme="graphite"] {
	                                        --color-primary: #52525b !important;
	                                        --color-primary-light: #a1a1aa !important;
	                                        --color-primary-dark: #3f3f46 !important;
	                                        --gradient-secondary: #a1a1aa;
	                                        --gradient-tertiary: #71717a;
	                                    }

	                                    /* Text in the accent color (links, inline code, active items). Each preset's shade keeps
	                                       the hue and moves the OKLCH lightness (darker in light mode, lighter in dark mode) just
	                                       far enough for 4.5:1 on the code background, the current sidebar link's tint and the
	                                       admonition tints. PrimaryColorContrast in Moka.Docs.Rendering computes these, and the
	                                       same shades for a custom primaryColor. A preset whose primary already passes has no rule. */
	                                    :root {
	                                        --color-primary-text: var(--color-primary);
	                                        --color-primary-text-hover: color-mix(in srgb, var(--color-primary-text) 80%, black);
	                                    }
	                                    [data-theme="dark"] { --color-primary-text-hover: color-mix(in srgb, var(--color-primary-text) 75%, white); }
	                                    html[data-color-theme="ocean"] { --color-primary-text: #0076a9; }
	                                    html[data-color-theme="emerald"] { --color-primary-text: #007f57; }
	                                    html[data-color-theme="violet"] { --color-primary-text: #804fe8; }
	                                    html[data-color-theme="amber"] { --color-primary-text: #9c6300; }
	                                    html[data-color-theme="rose"] { --color-primary-text: #d81c49; }
	                                    html[data-color-theme="moka-red"] { --color-primary-text: #d12d2d; }
	                                    html[data-color-theme="slate"] { --color-primary-text: #607087; }
	                                    html[data-color-theme="teal"] { --color-primary-text: #007d71; }
	                                    html[data-color-theme="indigo"] { --color-primary-text: #5c5de8; }
	                                    html[data-color-theme="orange"] { --color-primary-text: #b85000; }
	                                    html[data-theme="dark"]:is([data-color-theme="ocean"], [data-color-theme="emerald"], [data-color-theme="amber"], [data-color-theme="teal"], [data-color-theme="orange"]) { --color-primary-text: var(--color-primary); }
	                                    html[data-theme="dark"][data-color-theme="violet"] { --color-primary-text: #9b77ff; }
	                                    html[data-theme="dark"][data-color-theme="rose"] { --color-primary-text: #ff4c67; }
	                                    html[data-theme="dark"][data-color-theme="moka-red"] { --color-primary-text: #f9564f; }
	                                    html[data-theme="dark"][data-color-theme="slate"] { --color-primary-text: #8090a8; }
	                                    html[data-theme="dark"][data-color-theme="indigo"] { --color-primary-text: #7a82ff; }
	                                    html[data-theme="dark"][data-color-theme="graphite"] { --color-primary-text: #8e8e98; }

	                                    /* Fills in the accent color (buttons, step numbers) use the text shade too: white on it
	                                       passes in light mode, and the dark page color on it in dark mode. The raw primary under
	                                       white text was 2.2:1 for amber. */
	                                    :root {
	                                        --color-primary-fill: var(--color-primary-text);
	                                        --color-primary-fill-hover: var(--color-primary-text-hover);
	                                        --color-on-primary: #ffffff;
	                                    }
	                                    [data-theme="dark"] { --color-on-primary: #0f172a; }

	                                    /* Gradient titles. Every stop passes 3:1 (large text) on the page and the landing hero's
	                                       tint with every preset: the first is the text shade, the others are the preset's
	                                       gradient colors mixed toward black (light) or white (dark). */
	                                    :root {
	                                        --title-ink: #000000;
	                                        --title-hue: 72%;
	                                        --gradient-text-secondary: color-mix(in oklab, var(--gradient-secondary) var(--title-hue), var(--title-ink));
	                                        --gradient-text-tertiary: color-mix(in oklab, var(--gradient-tertiary) var(--title-hue), var(--title-ink));
	                                    }
	                                    [data-theme="dark"] { --title-ink: #ffffff; --title-hue: 76%; }

	                                    /* Admonition title colors: the box color darkened (light) or lightened (dark) to 4.5:1 on
	                                       the box's own tint. The raw colors were 2.0 to 3.8:1 on white. */
	                                    :root {
	                                        --admonition-note-text: #1d4ed8;
	                                        --admonition-tip-text: #15803d;
	                                        --admonition-warning-text: #b45309;
	                                        --admonition-danger-text: #b91c1c;
	                                        --admonition-info-text: #6d28d9;
	                                    }
	                                    [data-theme="dark"] {
	                                        --admonition-note-text: #60a5fa;
	                                        --admonition-tip-text: #4ade80;
	                                        --admonition-warning-text: #fbbf24;
	                                        --admonition-danger-text: #f87171;
	                                        --admonition-info-text: #a78bfa;
	                                    }

	                                    /* Reset */
	                                    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

	                                    html { scroll-behavior: smooth; scroll-padding-top: calc(var(--header-offset) + 1rem); }

	                                    body {
	                                        font-family: var(--font-body);
	                                        font-size: 1rem;
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
	                                        z-index: var(--z-header);
	                                        height: var(--header-height);
	                                        background: var(--color-bg);
	                                        border-bottom: 1px solid var(--color-border);
	                                        backdrop-filter: blur(8px);
	                                    }

	                                    .header-inner {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: space-between;
	                                        max-width: var(--layout-max-width);
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
	                                    .site-logo:hover { color: var(--color-primary-text); }
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
	                                        width: var(--header-button);
	                                        height: var(--header-button);
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

	                                    /* Appearance panel: one header button opening a non-modal dialog, a radiogroup per setting. It hangs under the
	                                       button on wide screens. On phones it spans the sticky header (its containing block there) and runs down to the
	                                       bottom of the screen; a fixed panel would land inside the header, which backdrop-filter makes the containing
	                                       block of fixed descendants. Inside the header, --z-popover keeps it above the drawers and below the modals. */
	                                    .appearance { position: relative; --appearance-accent: var(--color-primary-text); }
	                                    .appearance-trigger {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: var(--header-button);
	                                        height: var(--header-button);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                        transition: all var(--transition);
	                                    }
	                                    .appearance-trigger:hover {
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                    }
	                                    .appearance-trigger[aria-expanded="true"] {
	                                        border-color: var(--appearance-accent);
	                                        color: var(--appearance-accent);
	                                    }
	                                    .appearance-trigger:focus-visible { outline: 2px solid var(--appearance-accent); outline-offset: 2px; }
	                                    .appearance-panel {
	                                        position: absolute;
	                                        top: calc(100% + 8px);
	                                        right: 0;
	                                        z-index: var(--z-popover);
	                                        width: 24rem;
	                                        max-width: calc(100vw - 2rem);
	                                        max-height: calc(100vh - var(--header-offset) - 1.5rem);
	                                        max-height: calc(100dvh - var(--header-offset) - 1.5rem);
	                                        overflow-y: auto;
	                                        overscroll-behavior: contain;
	                                        padding: 0.75rem 1rem 1rem;
	                                        background: var(--color-bg);
	                                        color: var(--color-text);
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius-md, var(--radius));
	                                        box-shadow: var(--shadow-lg);
	                                        font-size: 0.875rem;
	                                        line-height: 1.4;
	                                        text-align: left;
	                                    }
	                                    .appearance-panel[hidden] { display: none; }
	                                    .appearance-head { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; }
	                                    .appearance-title { margin: 0; font-size: 1rem; font-weight: 700; line-height: 1.3; color: var(--color-text); }
	                                    .appearance-close {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 32px;
	                                        height: 32px;
	                                        border: none;
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        cursor: pointer;
	                                    }
	                                    .appearance-close:hover { background: var(--color-bg-secondary); color: var(--color-text); }
	                                    .appearance-section { margin-top: 0.75rem; padding-top: 0.75rem; border-top: 1px solid var(--color-border); }
	                                    .appearance-label {
	                                        margin: 0 0 0.5rem;
	                                        font-size: 0.75rem;
	                                        font-weight: 600;
	                                        letter-spacing: 0.04em;
	                                        text-transform: uppercase;
	                                        color: var(--color-text-muted);
	                                    }
	                                    .appearance-options { display: grid; gap: 0.375rem; }
	                                    .appearance-segments { grid-template-columns: repeat(auto-fit, minmax(4.25rem, 1fr)); }
	                                    .appearance-styles, .appearance-code-themes { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	                                    .appearance-swatches { grid-template-columns: repeat(auto-fill, minmax(4.25rem, 1fr)); }
	                                    .appearance-option {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        gap: 0.375rem;
	                                        min-width: 0;
	                                        min-height: 2.25rem;
	                                        padding: 0.375rem 0.5rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        font: inherit;
	                                        font-size: 0.8125rem;
	                                        line-height: 1.25;
	                                        text-align: center;
	                                        cursor: pointer;
	                                        transition: background var(--transition), border-color var(--transition), color var(--transition);
	                                    }
	                                    .appearance-option:hover { background: var(--color-bg-secondary); color: var(--color-text); }
	                                    .appearance-option[aria-checked="true"] {
	                                        border-color: var(--appearance-accent);
	                                        box-shadow: inset 0 0 0 1px var(--appearance-accent);
	                                        color: var(--appearance-accent);
	                                        font-weight: 600;
	                                    }
	                                    .appearance-option svg { flex-shrink: 0; }
	                                    .appearance-style, .appearance-code-theme { justify-content: flex-start; text-align: left; }
	                                    .appearance-style { gap: 0.625rem; padding: 0.5rem; }
	                                    .appearance-style-text { display: flex; flex-direction: column; min-width: 0; }
	                                    .appearance-style-name { font-weight: 600; color: var(--color-text); }
	                                    .appearance-style-desc { font-size: 0.6875rem; font-weight: 400; color: var(--color-text-secondary); }
	                                    .appearance-style[aria-checked="true"] .appearance-style-name { color: var(--appearance-accent); }
	                                    .appearance-swatch { flex-direction: column; gap: 0.3125rem; padding: 0.5rem 0.25rem; }
	                                    .appearance-swatch-dot { width: 1.375rem; height: 1.375rem; border-radius: 50%; box-shadow: inset 0 0 0 1px rgba(0, 0, 0, 0.15); }
	                                    .appearance-swatch-label { font-size: 0.6875rem; }
	                                    .appearance-code-preview { width: 1rem; height: 1rem; flex-shrink: 0; border-radius: 3px; box-shadow: inset 0 0 0 1px rgba(127, 127, 127, 0.45); }
	                                    .appearance-group-label { grid-column: 1 / -1; margin-top: 0.25rem; font-size: 0.6875rem; font-weight: 600; color: var(--color-text-muted); }
	                                    .appearance-group-label:first-child { margin-top: 0; }
	                                    .appearance-reset {
	                                        width: 100%;
	                                        margin-top: 1rem;
	                                        padding: 0.5rem 0.75rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background: transparent;
	                                        color: var(--color-text-secondary);
	                                        font: inherit;
	                                        font-size: 0.8125rem;
	                                        cursor: pointer;
	                                        transition: background var(--transition), color var(--transition);
	                                    }
	                                    .appearance-reset:hover { background: var(--color-bg-secondary); color: var(--color-text); }
	                                    /* Five reading widths in one row: less side padding, and a wrap only when large text needs it. */
	                                    [data-appearance="reader-width"] { grid-template-columns: repeat(auto-fit, minmax(3.75rem, 1fr)); }
	                                    [data-appearance="reader-width"] .appearance-option { padding-left: 0.25rem; padding-right: 0.25rem; }
	                                    /* The Layout section (theme.options.layoutSelector): a labelled select per part of the page, which main.js
	                                       builds when the panel first opens. color-scheme gives the open list the page's mode. */
	                                    .appearance-layout { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.625rem 0.75rem; }
	                                    .appearance-layout-field { display: flex; flex-direction: column; gap: 0.25rem; min-width: 0; }
	                                    .appearance-layout-field label { font-size: 0.75rem; font-weight: 500; line-height: 1.3; color: var(--color-text-secondary); }
	                                    .appearance-layout-field select {
	                                        width: 100%;
	                                        min-width: 0;
	                                        min-height: 2.25rem;
	                                        padding: 0.375rem 0.5rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius);
	                                        background-color: var(--color-bg);
	                                        color: var(--color-text);
	                                        color-scheme: light;
	                                        font: inherit;
	                                        font-size: 0.8125rem;
	                                        cursor: pointer;
	                                    }
	                                    [data-theme="dark"] .appearance-layout-field select { color-scheme: dark; }
	                                    .appearance-layout-field option { background-color: var(--color-bg); color: var(--color-text); }
	                                    .appearance-layout-field select:hover { border-color: var(--color-text-muted); }
	                                    @media (max-width: 359px) {
	                                        .appearance-layout { grid-template-columns: minmax(0, 1fr); }
	                                    }
	                                    .appearance-panel :focus-visible { outline: 2px solid var(--appearance-accent); outline-offset: 2px; }
	                                    @media (max-width: 768px) {
	                                        .appearance { position: static; }
	                                        .appearance-panel {
	                                            top: 100%;
	                                            left: 0;
	                                            right: 0;
	                                            width: auto;
	                                            max-width: none;
	                                            max-height: calc(100vh - var(--header-offset));
	                                            max-height: calc(100dvh - var(--header-offset));
	                                            padding: 0.75rem 1rem 1.25rem;
	                                            border-width: 1px 0;
	                                            border-radius: 0;
	                                        }
	                                        .appearance ~ .theme-toggle { display: none; }
	                                    }

	                                    /* Reader text size and reading width (Appearance panel). Font sizes are in rem, so the root size scales all of the
	                                       text. A width moves --content-max-width, and the page (--layout-max-width) grows with it where the column wouldn't
	                                       fit between the sidebar and "On this page"; it never comes out narrower than the site's own layout.width, except
	                                       narrow. :root lifts these above the styles' and layouts' own values. */
	                                    html[data-text-size="90"] { font-size: 90%; }
	                                    html[data-text-size="112.5"] { font-size: 112.5%; }
	                                    html[data-text-size="125"] { font-size: 125%; }
	                                    html:root[data-reader-width="narrow"] { --content-max-width: 60ch; }
	                                    html:root[data-reader-width="wide"] { --content-max-width: 1040px; }
	                                    html:root[data-reader-width="wide"]:is(:not([data-width]), [data-width="narrow"]) { --layout-max-width: 1680px; }
	                                    html:root[data-reader-width="wide"][data-width="wider"] { --content-max-width: 1280px; }
	                                    html:root[data-reader-width="wide"][data-width="full"] { --content-max-width: none; }
	                                    html:root[data-reader-width="wider"] { --layout-max-width: 1920px; --content-max-width: 1280px; }
	                                    html:root[data-reader-width="wider"][data-width="full"] { --layout-max-width: none; --content-max-width: none; }
	                                    html:root[data-reader-width="full"] { --layout-max-width: none; --content-max-width: none; }

	                                    /* Markup that only some layout variants show. Pages that let readers switch layouts (theme.options.layoutSelector)
	                                       carry it for every variant, and layouts.css shows it while its variant is on. */
	                                    .header-tabs, .toc-inline, .site-footer-columns { display: none; }

	                                    .mobile-nav-toggle { display: none; }

	                                    /* Layout */
	                                    .site-container {
	                                        display: flex;
	                                        max-width: var(--layout-max-width);
	                                        margin: 0 auto;
	                                        min-height: calc(100vh - var(--header-height) - 60px);
	                                    }

	                                    /* Sidebar */
	                                    .sidebar {
	                                        position: sticky;
	                                        top: var(--header-offset);
	                                        width: var(--sidebar-width);
	                                        height: calc(100vh - var(--header-offset));
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

	                                    /* Sidebar toggle (theme.options.sidebarToggle): a handle at the top of the content column's
	                                       gutter that takes no room of its own. Phones use the menu button instead. */
	                                    .sidebar-toggle { display: none; }
	                                    @media (min-width: 769px) {
	                                        .sidebar-toggle {
	                                            display: flex;
	                                            position: sticky;
	                                            top: calc(var(--header-offset) + 0.75rem);
	                                            z-index: 1;
	                                            flex-shrink: 0;
	                                            align-self: flex-start;
	                                            align-items: center;
	                                            justify-content: center;
	                                            width: 28px;
	                                            height: 28px;
	                                            margin: 0.75rem calc(-28px - 0.375rem) 0 0.375rem;
	                                            padding: 0;
	                                            border: 1px solid var(--color-border);
	                                            border-radius: var(--radius-sm);
	                                            background: var(--color-bg);
	                                            color: var(--color-text-secondary);
	                                            cursor: pointer;
	                                            transition: color var(--transition), border-color var(--transition);
	                                        }
	                                        .sidebar-toggle:hover { color: var(--color-text); border-color: var(--color-text-muted); }
	                                        .sidebar-toggle:focus-visible { outline: 2px solid var(--color-primary-text); outline-offset: 2px; }
	                                        .sidebar-toggle svg { width: 16px; height: 16px; }
	                                        html.sidebar-hidden .sidebar { display: none; }
	                                        html.sidebar-hidden .content { margin-left: auto; margin-right: auto; }
	                                    }

	                                    /* Content */
	                                    .content {
	                                        flex: 1;
	                                        min-width: 0;
	                                        padding: 2rem 2.5rem;
	                                        max-width: calc(var(--content-max-width) + 5rem);
	                                    }
	                                    /* A word longer than its line (a URL, a dotted type name in code) breaks instead of pushing the page
	                                       sideways. break-word only breaks what would overflow, so it changes nothing that fits. Flex rows
	                                       of author text (card and admonition titles, summaries, buttons, the footers) take anywhere: a flex
	                                       item is never narrower than its longest word under break-word. Tables keep break-word, since
	                                       anywhere would change how their columns share the width. */
	                                    #mokadocs-main { overflow-wrap: break-word; }
	                                    :is(.site-footer, .landing-footer) { overflow-wrap: anywhere; }

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

	                                    /* Code blocks: .code-block wraps the <pre>, which draws the frame and doesn't scroll. The <code>
	                                       inside it scrolls. The script puts the language label, line numbers and Copy button into the
	                                       <pre> ahead of the <code>, so they stay put while long lines scroll. --code-top is the <pre>'s
	                                       top padding, which the line numbers have to start at. */
	                                    .code-block {
	                                        position: relative;
	                                        --code-top: 1rem;
	                                        --code-pad-x: 1.25rem;
	                                        --code-gutter: 2.75rem;
	                                        --code-font: 0.875rem;
	                                        --code-lh: 1.6;
	                                    }
	                                    .code-block > pre {
	                                        overflow: hidden;
	                                        padding: var(--code-top) 0 0;
	                                    }
	                                    .code-block > pre > code {
	                                        display: grid;
	                                        overflow-x: auto;
	                                        padding: 0 0 1rem;
	                                        font-size: var(--code-font);
	                                        line-height: var(--code-lh);
	                                    }
	                                    .page-content .code-block > pre > code { padding: 0 0 1rem !important; }
	                                    .code-block > pre > code > .line {
	                                        min-height: calc(var(--code-font) * var(--code-lh));
	                                        padding: 0 var(--code-pad-x);
	                                    }
	                                    .code-block > pre > code:focus-visible {
	                                        outline: 2px solid var(--color-primary-text);
	                                        outline-offset: -2px;
	                                        border-radius: var(--radius-sm);
	                                    }
	                                    .code-block[data-wrap] > pre > code { white-space: pre-wrap; overflow-wrap: anywhere; }

	                                    /* File name from title="..." in a bar across the top of the frame */
	                                    .code-block > .code-title {
	                                        position: absolute;
	                                        z-index: 1;
	                                        top: 1px;
	                                        left: calc(1px + var(--code-pad-x));
	                                        max-width: calc(100% - 10rem);
	                                        height: 2.25rem;
	                                        line-height: 2.25rem;
	                                        overflow: hidden;
	                                        white-space: nowrap;
	                                        text-overflow: ellipsis;
	                                        font-family: var(--font-mono);
	                                        font-size: 0.75rem;
	                                        font-weight: 500;
	                                        color: var(--sh-punctuation);
	                                    }
	                                    html:not([data-code-style="macos"]):not([data-code-style="terminal"]):not([data-code-style="vscode"]) .code-block[data-title] { --code-top: 3rem; }
	                                    html:not([data-code-style="macos"]):not([data-code-style="terminal"]):not([data-code-style="vscode"]) .code-block[data-title] > pre::before {
	                                        content: '';
	                                        position: absolute;
	                                        top: 0;
	                                        left: 0;
	                                        right: 0;
	                                        height: 2.25rem;
	                                        background: rgba(128,128,128,0.08);
	                                        border-bottom: 1px solid rgba(128,128,128,0.15);
	                                        pointer-events: none;
	                                    }

	                                    /* Highlighted lines ({1,3-5} or [!code highlight]), diff lines and focus. The tint
	                                       sits on --sh-line-shade, which darkens dark themes first: a tint alone lightened the
	                                       line, and token colors that just pass AA on the plain background failed on it. */
	                                    .code-block > pre > code > .line.highlighted {
	                                        background: linear-gradient(var(--sh-line-highlight), var(--sh-line-highlight)), var(--sh-line-shade);
	                                        box-shadow: inset 3px 0 0 var(--sh-line-highlight-border);
	                                    }
	                                    .code-block > pre > code > .line.diff-add {
	                                        background: linear-gradient(var(--sh-diff-add-bg), var(--sh-diff-add-bg)), var(--sh-line-shade);
	                                        box-shadow: inset 3px 0 0 var(--sh-diff-add);
	                                    }
	                                    .code-block > pre > code > .line.diff-remove {
	                                        background: linear-gradient(var(--sh-diff-remove-bg), var(--sh-diff-remove-bg)), var(--sh-line-shade);
	                                        box-shadow: inset 3px 0 0 var(--sh-diff-remove);
	                                    }
	                                    .code-block[data-diff] > pre > code > .line::before {
	                                        content: '';
	                                        display: inline-block;
	                                        width: 2ch;
	                                        user-select: none;
	                                    }
	                                    .code-block[data-diff] > pre > code > .line.diff-add::before { content: '+'; color: var(--sh-diff-add); }
	                                    .code-block[data-diff] > pre > code > .line.diff-remove::before { content: '-'; color: var(--sh-diff-remove); }
	                                    .code-block[data-focus] > pre > code > .line:not(.focused) {
	                                        opacity: 0.45;
	                                        filter: blur(1px);
	                                        transition: opacity 0.2s ease, filter 0.2s ease;
	                                    }
	                                    .code-block[data-focus]:is(:hover, :focus-within) > pre > code > .line { opacity: 1; filter: none; }

	                                    /* Copy button on code blocks, in the code theme's chrome colors (--sh-chrome*, light or dark) */
	                                    .page-content pre .copy-btn {
	                                        position: absolute;
	                                        top: 0.5rem;
	                                        right: 0.5rem;
	                                        padding: 0.25rem 0.5rem;
	                                        font-size: 0.75rem;
	                                        font-family: var(--font-mono);
	                                        background: var(--sh-copy-bg);
	                                        border: 1px solid var(--sh-copy-border);
	                                        border-radius: var(--radius-sm);
	                                        color: var(--sh-chrome);
	                                        cursor: pointer;
	                                        opacity: 0;
	                                        transition: all var(--transition);
	                                    }
	                                    .page-content pre .copy-btn:hover {
	                                        background: var(--sh-copy-bg-hover);
	                                        color: var(--sh-chrome-strong);
	                                        border-color: var(--sh-copy-border-hover);
	                                    }
	                                    /* The language label and the Copy button share the top-right corner, so only one shows
	                                       at a time: the label hides at once when the button appears, and comes back only after
	                                       the button has faded out. */
	                                    .page-content pre:hover .copy-btn,
	                                    .page-content pre .copy-btn:focus-visible { opacity: 1; }
	                                    .page-content pre .copy-btn:focus-visible,
	                                    .landing-content pre .copy-btn:focus-visible { outline: 2px solid var(--color-primary-text); outline-offset: 2px; }
	                                    .code-copy-status {
	                                        position: absolute;
	                                        width: 1px;
	                                        height: 1px;
	                                        margin: -1px;
	                                        padding: 0;
	                                        overflow: hidden;
	                                        clip: rect(0 0 0 0);
	                                        white-space: nowrap;
	                                        border: 0;
	                                    }
	                                    .page-content pre:hover .code-lang,
	                                    .page-content pre:focus-within .code-lang { opacity: 0; transition: none; }
	                                    /* Touch screens have no hover, so the button used to stay invisible. */
	                                    @media (hover: none) {
	                                        .page-content pre .copy-btn { opacity: 1; }
	                                        html:not([data-code-style="macos"]):not([data-code-style="terminal"]):not([data-code-style="vscode"]) .page-content pre .code-lang { opacity: 0; }
	                                    }



	                                    /* Line numbers on code blocks: a column beside the scrolling <code>, or with wrap a counter on
	                                       each line (wrapped lines have no fixed height for a column to follow). Each number is exactly
	                                       one code line high; with their own font size and a relative line height they drifted. */
	                                    .code-block.has-line-numbers:not([data-wrap]) > pre { padding-left: var(--code-gutter); }
	                                    .code-block.has-line-numbers > pre > code > .line { padding-left: 0.75rem; }
	                                    .code-block > pre > .line-numbers {
	                                        position: absolute; left: 0; top: var(--code-top); bottom: 0; width: var(--code-gutter);
	                                        padding-right: 0.75rem; text-align: right; font-family: var(--font-mono);
	                                        user-select: none; pointer-events: none;
	                                        color: var(--sh-line-number); border-right: 1px solid var(--color-border);
	                                    }
	                                    .code-block > pre > .line-numbers span {
	                                        display: block;
	                                        height: calc(var(--code-font) * var(--code-lh));
	                                        font-size: 0.8rem;
	                                        line-height: calc(var(--code-font) * var(--code-lh));
	                                    }
	                                    .code-block[data-wrap].has-line-numbers > pre > code { counter-reset: line; }
	                                    .code-block[data-wrap].has-line-numbers > pre > code > .line {
	                                        position: relative;
	                                        counter-increment: line;
	                                        padding-left: calc(var(--code-gutter) + 0.75rem);
	                                    }
	                                    .code-block[data-wrap].has-line-numbers > pre > code > .line::after {
	                                        content: counter(line);
	                                        content: counter(line) / '';
	                                        position: absolute; top: 0; bottom: 0; left: 0; width: var(--code-gutter);
	                                        padding-right: 0.75rem; text-align: right; font-family: var(--font-mono);
	                                        font-size: 0.8rem; line-height: calc(var(--code-font) * var(--code-lh));
	                                        color: var(--sh-line-number); border-right: 1px solid var(--color-border);
	                                        user-select: none;
	                                    }

	                                    /* Language label on code blocks */
	                                    .page-content pre .code-lang {
	                                        position: absolute;
	                                        top: 0.375rem;
	                                        left: 0.75rem;
	                                        font-size: 0.625rem;
	                                        font-family: var(--font-mono);
	                                        text-transform: uppercase;
	                                        letter-spacing: 0.05em;
	                                        color: var(--sh-chrome);
	                                        user-select: none;
	                                        transition: opacity var(--transition);
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
	                                        --sh-comment: #81859c; --sh-string: #a6e3a1; --sh-keyword: #cba6f7;
	                                        --sh-number: #fab387; --sh-type: #89dceb; --sh-function: #89b4fa;
	                                        --sh-operator: #94e2d5; --sh-punctuation: #bac2de; --sh-attr: #f9e2af;
	                                        --sh-tag: #f38ba8; --sh-property: #b4befe; --sh-line-number: #9399b2;
	                                    }
	                                    [data-code-theme="catppuccin-mocha"] :is(.page-content, .landing-content) pre { background: #1e1e2e; border-color: #313244; }
	                                    [data-code-theme="catppuccin-mocha"] :is(.page-content, .landing-content) pre code { color: #cdd6f4; }

	                                    /* Code Theme: Catppuccin Latte */
	                                    [data-code-theme="catppuccin-latte"] {
	                                        --sh-comment: #666978; --sh-string: #1f7a00; --sh-keyword: #8839ef;
	                                        --sh-number: #b74500; --sh-type: #00719f; --sh-function: #165eed;
	                                        --sh-operator: #00767c; --sh-punctuation: #5c5f77; --sh-attr: #975c00;
	                                        --sh-tag: #d20f39; --sh-property: #4f5ed0; --sh-line-number: #5c5f77;
	                                    }
	                                    [data-code-theme="catppuccin-latte"] :is(.page-content, .landing-content) pre { background: #eff1f5; border-color: #ccd0da; }
	                                    [data-code-theme="catppuccin-latte"] :is(.page-content, .landing-content) pre code { color: #4c4f69; }

	                                    /* Code Theme: GitHub Dark */
	                                    [data-code-theme="github-dark"] {
	                                        --sh-comment: #8b949e; --sh-string: #a5d6ff; --sh-keyword: #ff7b72;
	                                        --sh-number: #79c0ff; --sh-type: #ffa657; --sh-function: #d2a8ff;
	                                        --sh-operator: #ff7b72; --sh-punctuation: #c9d1d9; --sh-attr: #79c0ff;
	                                        --sh-tag: #7ee787; --sh-property: #79c0ff; --sh-line-number: #8b949e;
	                                    }
	                                    [data-code-theme="github-dark"] :is(.page-content, .landing-content) pre { background: #0d1117; border-color: #30363d; }
	                                    [data-code-theme="github-dark"] :is(.page-content, .landing-content) pre code { color: #c9d1d9; }

	                                    /* Code Theme: Dracula */
	                                    [data-code-theme="dracula"] {
	                                        --sh-comment: #7f90c4; --sh-string: #f1fa8c; --sh-keyword: #ff79c6;
	                                        --sh-number: #bd93f9; --sh-type: #8be9fd; --sh-function: #50fa7b;
	                                        --sh-operator: #ff79c6; --sh-punctuation: #f8f8f2; --sh-attr: #50fa7b;
	                                        --sh-tag: #ff79c6; --sh-property: #66d9ef; --sh-line-number: #9aa1c4;
	                                    }
	                                    [data-code-theme="dracula"] :is(.page-content, .landing-content) pre { background: #282a36; border-color: #44475a; }
	                                    [data-code-theme="dracula"] :is(.page-content, .landing-content) pre code { color: #f8f8f2; }

	                                    /* Code Theme: One Dark */
	                                    [data-code-theme="one-dark"] {
	                                        --sh-comment: #8b93a1; --sh-string: #98c379; --sh-keyword: #c678dd;
	                                        --sh-number: #d19a66; --sh-type: #e5c07b; --sh-function: #61afef;
	                                        --sh-operator: #56b6c2; --sh-punctuation: #abb2bf; --sh-attr: #d19a66;
	                                        --sh-tag: #e36e77; --sh-property: #e36e77; --sh-line-number: #959ba5;
	                                    }
	                                    [data-code-theme="one-dark"] :is(.page-content, .landing-content) pre { background: #282c34; border-color: #3e4451; }
	                                    [data-code-theme="one-dark"] :is(.page-content, .landing-content) pre code { color: #abb2bf; }

	                                    /* Code Theme: Nord */
	                                    [data-code-theme="nord"] {
	                                        --sh-comment: #8e9cb7; --sh-string: #a3be8c; --sh-keyword: #81a1c1;
	                                        --sh-number: #b690af; --sh-type: #8fbcbb; --sh-function: #88c0d0;
	                                        --sh-operator: #81a1c1; --sh-punctuation: #d8dee9; --sh-attr: #8fbcbb;
	                                        --sh-tag: #e17f87; --sh-property: #88c0d0; --sh-line-number: #a0a9ba;
	                                    }
	                                    [data-code-theme="nord"] :is(.page-content, .landing-content) pre { background: #2e3440; border-color: #3b4252; }
	                                    [data-code-theme="nord"] :is(.page-content, .landing-content) pre code { color: #d8dee9; }

	                                    /* Code Theme: GitHub Light */
	                                    [data-code-theme="github-light"] {
	                                        --sh-comment: #6e7781; --sh-string: #0a3069; --sh-keyword: #cf222e;
	                                        --sh-number: #0550ae; --sh-type: #953800; --sh-function: #8250df;
	                                        --sh-operator: #cf222e; --sh-punctuation: #24292f; --sh-attr: #0550ae;
	                                        --sh-tag: #116329; --sh-property: #0550ae; --sh-line-number: #656d76;
	                                    }
	                                    [data-code-theme="github-light"] :is(.page-content, .landing-content) pre { background: #ffffff; border-color: #d0d7de; }
	                                    [data-code-theme="github-light"] :is(.page-content, .landing-content) pre code { color: #24292f; }

	                                    /* Code Theme: Tokyo Night */
	                                    [data-code-theme="tokyo-night"] {
	                                        --sh-comment: #7882ae; --sh-string: #9ece6a; --sh-keyword: #bb9af7;
	                                        --sh-number: #ff9e64; --sh-type: #2ac3de; --sh-function: #7aa2f7;
	                                        --sh-operator: #89ddff; --sh-punctuation: #a9b1d6; --sh-attr: #e0af68;
	                                        --sh-tag: #f7768e; --sh-property: #73daca; --sh-line-number: #7d86a8;
	                                    }
	                                    [data-code-theme="tokyo-night"] :is(.page-content, .landing-content) pre { background: #1a1b26; border-color: #292e42; }
	                                    [data-code-theme="tokyo-night"] :is(.page-content, .landing-content) pre code { color: #c0caf5; }

	                                    /* Code Theme: Gruvbox Dark */
	                                    [data-code-theme="gruvbox-dark"] {
	                                        --sh-comment: #9b8c7d; --sh-string: #b8bb26; --sh-keyword: #ff4f3a;
	                                        --sh-number: #d3869b; --sh-type: #fabd2f; --sh-function: #8ec07c;
	                                        --sh-operator: #fe8019; --sh-punctuation: #a89984; --sh-attr: #fabd2f;
	                                        --sh-tag: #83a598; --sh-property: #83a598; --sh-line-number: #9d9084;
	                                    }
	                                    [data-code-theme="gruvbox-dark"] :is(.page-content, .landing-content) pre { background: #282828; border-color: #3c3836; }
	                                    [data-code-theme="gruvbox-dark"] :is(.page-content, .landing-content) pre code { color: #ebdbb2; }

	                                    /* Code Theme: Gruvbox Light */
	                                    [data-code-theme="gruvbox-light"] {
	                                        --sh-comment: #766759; --sh-string: #716c00; --sh-keyword: #9d0006;
	                                        --sh-number: #8f3f71; --sh-type: #945e00; --sh-function: #3c7553;
	                                        --sh-operator: #af3a03; --sh-punctuation: #73665b; --sh-attr: #945e00;
	                                        --sh-tag: #076678; --sh-property: #076678; --sh-line-number: #776955;
	                                    }
	                                    [data-code-theme="gruvbox-light"] :is(.page-content, .landing-content) pre { background: #fbf1c7; border-color: #ebdbb2; }
	                                    [data-code-theme="gruvbox-light"] :is(.page-content, .landing-content) pre code { color: #3c3836; }

	                                    /* Code Theme: Solarized Dark */
	                                    [data-code-theme="solarized-dark"] {
	                                        --sh-comment: #7a9198; --sh-string: #2aa198; --sh-keyword: #859900;
	                                        --sh-number: #ef5299; --sh-type: #b58900; --sh-function: #3093da;
	                                        --sh-operator: #859900; --sh-punctuation: #93a1a1; --sh-attr: #b58900;
	                                        --sh-tag: #3093da; --sh-property: #3093da; --sh-line-number: #7e959c;
	                                    }
	                                    [data-code-theme="solarized-dark"] :is(.page-content, .landing-content) pre { background: #002b36; border-color: #073642; }
	                                    [data-code-theme="solarized-dark"] :is(.page-content, .landing-content) pre code { color: #839496; }

	                                    /* Code Theme: Solarized Light */
	                                    [data-code-theme="solarized-light"] {
	                                        --sh-comment: #626f6f; --sh-string: #007a73; --sh-keyword: #657400;
	                                        --sh-number: #c82a79; --sh-type: #8a6800; --sh-function: #0072b4;
	                                        --sh-operator: #657400; --sh-punctuation: #586e75; --sh-attr: #8a6800;
	                                        --sh-tag: #0072b4; --sh-property: #0072b4; --sh-line-number: #637171;
	                                    }
	                                    [data-code-theme="solarized-light"] :is(.page-content, .landing-content) pre { background: #fdf6e3; border-color: #eee8d5; }
	                                    [data-code-theme="solarized-light"] :is(.page-content, .landing-content) pre code { color: #5b7078; }

	                                    /* Code Theme: Rose Pine */
	                                    [data-code-theme="rose-pine"] {
	                                        --sh-comment: #837f9c; --sh-string: #f6c177; --sh-keyword: #488aa5;
	                                        --sh-number: #ebbcba; --sh-type: #9ccfd8; --sh-function: #ebbcba;
	                                        --sh-operator: #908caa; --sh-punctuation: #908caa; --sh-attr: #c4a7e7;
	                                        --sh-tag: #9ccfd8; --sh-property: #9ccfd8; --sh-line-number: #85819d;
	                                    }
	                                    [data-code-theme="rose-pine"] :is(.page-content, .landing-content) pre { background: #191724; border-color: #26233a; }
	                                    [data-code-theme="rose-pine"] :is(.page-content, .landing-content) pre code { color: #e0def4; }

	                                    /* Code Theme: Rose Pine Dawn */
	                                    [data-code-theme="rose-pine-dawn"] {
	                                        --sh-comment: #6e6a7b; --sh-string: #986000; --sh-keyword: #286983;
	                                        --sh-number: #a45552; --sh-type: #377580; --sh-function: #a45552;
	                                        --sh-operator: #6d6986; --sh-punctuation: #6b6784; --sh-attr: #796391;
	                                        --sh-tag: #377580; --sh-property: #377580; --sh-line-number: #706b7c;
	                                    }
	                                    [data-code-theme="rose-pine-dawn"] :is(.page-content, .landing-content) pre { background: #faf4ed; border-color: #f2e9e1; }
	                                    [data-code-theme="rose-pine-dawn"] :is(.page-content, .landing-content) pre code { color: #575279; }

	                                    /* Code Theme: Night Owl */
	                                    [data-code-theme="night-owl"] {
	                                        --sh-comment: #708585; --sh-string: #ecc48d; --sh-keyword: #c792ea;
	                                        --sh-number: #f78c6c; --sh-type: #ffcb8b; --sh-function: #82aaff;
	                                        --sh-operator: #c792ea; --sh-punctuation: #7fdbca; --sh-attr: #addb67;
	                                        --sh-tag: #7fdbca; --sh-property: #80cbc4; --sh-line-number: #6a8499;
	                                    }
	                                    [data-code-theme="night-owl"] :is(.page-content, .landing-content) pre { background: #011627; border-color: #1d3b53; }
	                                    [data-code-theme="night-owl"] :is(.page-content, .landing-content) pre code { color: #d6deeb; }

	                                    /* Code Theme: Monokai */
	                                    [data-code-theme="monokai"] {
	                                        --sh-comment: #928e7a; --sh-string: #e6db74; --sh-keyword: #ff447e;
	                                        --sh-number: #ae81ff; --sh-type: #66d9ef; --sh-function: #a6e22e;
	                                        --sh-operator: #ff447e; --sh-punctuation: #f8f8f2; --sh-attr: #a6e22e;
	                                        --sh-tag: #ff447e; --sh-property: #66d9ef; --sh-line-number: #92928c;
	                                    }
	                                    [data-code-theme="monokai"] :is(.page-content, .landing-content) pre { background: #272822; border-color: #3e3d32; }
	                                    [data-code-theme="monokai"] :is(.page-content, .landing-content) pre code { color: #f8f8f2; }

	                                    /* Line highlights, diff colors and the chrome (language label, Copy button). The first set suits
	                                       the dark code themes. Checked against every token color that meets AA on its theme, all eleven
	                                       presets, and moka's #0a0a0d: none drops below 4.5:1 on a marked line. The chrome text is 4.5:1
	                                       on its own tint in every theme. */
	                                    :root {
	                                        --sh-line-shade: rgba(0, 0, 0, 0.4);
	                                        --sh-line-highlight: color-mix(in srgb, var(--color-primary) 7%, transparent);
	                                        --sh-line-highlight-border: var(--color-primary);
	                                        --sh-diff-add-bg: rgba(46, 160, 67, 0.1);
	                                        --sh-diff-remove-bg: rgba(248, 81, 73, 0.1);
	                                        --sh-diff-add: #56d364;
	                                        --sh-diff-remove: #ff8e86;
	                                        --sh-chrome: rgba(255, 255, 255, 0.72);
	                                        --sh-chrome-strong: #ffffff;
	                                        --sh-chrome-bg: rgba(255, 255, 255, 0.08);
	                                        --sh-chrome-line: rgba(255, 255, 255, 0.08);
	                                        --sh-copy-bg: rgba(255, 255, 255, 0.15);
	                                        --sh-copy-border: rgba(255, 255, 255, 0.25);
	                                        --sh-copy-bg-hover: rgba(255, 255, 255, 0.25);
	                                        --sh-copy-border-hover: rgba(255, 255, 255, 0.4);
	                                    }
	                                    /* The light code themes. A new light theme goes in this list. */
	                                    [data-code-theme="catppuccin-latte"],
	                                    [data-code-theme="github-light"],
	                                    [data-code-theme="gruvbox-light"],
	                                    [data-code-theme="solarized-light"],
	                                    [data-code-theme="rose-pine-dawn"] {
	                                        --sh-line-shade: transparent;
	                                        --sh-line-highlight: color-mix(in srgb, var(--color-primary) 4%, transparent);
	                                        --sh-diff-add-bg: rgba(26, 127, 55, 0.04);
	                                        --sh-diff-remove-bg: rgba(207, 34, 46, 0.04);
	                                        --sh-diff-add: #116329;
	                                        --sh-diff-remove: #a40e26;
	                                        --sh-chrome: rgba(0, 0, 0, 0.66);
	                                        --sh-chrome-strong: rgba(0, 0, 0, 0.87);
	                                        --sh-chrome-bg: rgba(0, 0, 0, 0.04);
	                                        --sh-chrome-line: rgba(0, 0, 0, 0.08);
	                                        --sh-copy-bg: rgba(0, 0, 0, 0.06);
	                                        --sh-copy-border: rgba(0, 0, 0, 0.12);
	                                        --sh-copy-bg-hover: rgba(0, 0, 0, 0.12);
	                                        --sh-copy-border-hover: rgba(0, 0, 0, 0.2);
	                                    }
	                                    /* github-light's comments are 4.55:1 on white, so any tint takes them under AA. */
	                                    [data-code-theme="github-light"] .code-block .line:is(.highlighted, .diff-add, .diff-remove) { --sh-comment: #656d76; }

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
	                                    html[data-code-style="macos"] .page-content .code-block { --code-top: 2.75rem; }
	                                    html[data-code-style="macos"] .page-content .code-block > .code-title {
	                                        top: 1px;
	                                        left: 50%;
	                                        transform: translateX(-50%);
	                                        max-width: calc(100% - 14rem);
	                                        height: 32px;
	                                        line-height: 32px;
	                                    }
	                                    html[data-code-style="macos"] .page-content .code-block[data-title] > pre > .code-lang { display: none !important; }
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
	                                    html[data-code-style="terminal"] .page-content .code-block { --code-top: 2.75rem; }
	                                    html[data-code-style="terminal"] .page-content .code-block > .code-title {
	                                        top: 1px;
	                                        left: calc(1px + 32px + 0.5rem);
	                                        max-width: calc(100% - 9rem);
	                                        height: 34px;
	                                        line-height: 34px;
	                                        color: #4ade80;
	                                        font-weight: 600;
	                                    }
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content .code-block > .code-title { color: #166534; }
	                                    html[data-code-style="terminal"] .page-content .code-block[data-title] > pre > .code-lang { display: none !important; }
	                                    /* Light mode terminal */
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre::before {
	                                        background: #e8e8f0;
	                                        border-bottom-color: rgba(0,128,60,0.2);
	                                    }
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre::after {
	                                        color: #16a34a;
	                                    }
	                                    html[data-code-style="terminal"][data-theme="light"] .page-content pre .code-lang {
	                                        color: #166534 !important;
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
	                                    html[data-code-style="vscode"] .page-content .code-block { --code-top: 2.75rem; --code-gutter: 3.25rem; }
	                                    /* The gutter starts under the tab bar (3px accent + 32px), its numbers at the first code line. */
	                                    html[data-code-style="vscode"] .page-content .code-block > pre > .line-numbers {
	                                        top: 35px;
	                                        padding-top: calc(var(--code-top) - 35px);
	                                        background: rgba(128,128,128,0.04);
	                                        border-right: 1px solid rgba(128,128,128,0.1) !important;
	                                    }
	                                    html[data-code-style="vscode"] .page-content .code-block > .code-title {
	                                        top: 4px;
	                                        left: 3px;
	                                        max-width: calc(100% - 8rem);
	                                        height: 32px;
	                                        line-height: 32px;
	                                        padding: 0 1rem;
	                                        background: rgba(128,128,128,0.04);
	                                        border-right: 1px solid rgba(128,128,128,0.1);
	                                        border-bottom: 2px solid var(--color-primary);
	                                    }
	                                    html[data-code-style="vscode"] .page-content .code-block[data-title] > pre > .code-lang { display: none !important; }

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
	                                        gap: 0.75rem;
	                                        overflow-wrap: anywhere;
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
	                                        flex-shrink: 0;
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
	                                        color: var(--color-primary-text);
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
	                                    @media (max-width: 480px) {
	                                        .install-tabs { overflow-x: auto; scrollbar-width: none; border-bottom: none; box-shadow: inset 0 -1px 0 var(--color-border); }
	                                        .install-tab { flex-shrink: 0; margin-bottom: 0; }
	                                    }
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
	                                        background: var(--color-primary-fill);
	                                        color: var(--color-on-primary);
	                                        border-color: var(--color-primary-fill);
	                                    }

	                                    /* API Reference styles */
	                                    .api-type-header { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1rem; }
	                                    .api-badge {
	                                        display: inline-block; padding: 0.25rem 0.625rem; border-radius: var(--radius-sm);
	                                        font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em;
	                                        min-width: 4.5rem; text-align: center;
	                                    }
	                                    /* Badge text: the badge color darkened (light) or lightened (dark) to 4.5:1 on its own tint. The raw
	                                       colors were 1.9 to 3.9:1 on the light tints. */
	                                    :root {
	                                        --badge-blue: #1d4ed8; --badge-green: #166534; --badge-violet: #6d28d9; --badge-cyan: #0e7490; --badge-amber: #92400e;
	                                        --badge-pink: #be185d; --badge-slate: #475569; --badge-lavender: #6d28d9; --badge-orange: #9a3412; --badge-red: #b91c1c;
	                                    }
	                                    [data-theme="dark"] {
	                                        --badge-blue: #60a5fa; --badge-green: #4ade80; --badge-violet: #a78bfa; --badge-cyan: #22d3ee; --badge-amber: #fbbf24;
	                                        --badge-pink: #f472b6; --badge-slate: #94a3b8; --badge-lavender: #c4b5fd; --badge-orange: #fb923c; --badge-red: #f87171;
	                                    }
	                                    .api-badge-class { background: color-mix(in srgb, #3b82f6 15%, var(--color-bg)); color: var(--badge-blue); }
	                                    .api-badge-struct { background: color-mix(in srgb, #22c55e 15%, var(--color-bg)); color: var(--badge-green); }
	                                    .api-badge-record { background: color-mix(in srgb, #8b5cf6 15%, var(--color-bg)); color: var(--badge-violet); }
	                                    .api-badge-interface { background: color-mix(in srgb, #06b6d4 15%, var(--color-bg)); color: var(--badge-cyan); }
	                                    .api-badge-enum { background: color-mix(in srgb, #f59e0b 15%, var(--color-bg)); color: var(--badge-amber); }
	                                    .api-badge-delegate { background: color-mix(in srgb, #ec4899 15%, var(--color-bg)); color: var(--badge-pink); }
	                                    .api-badge-static { background: color-mix(in srgb, #64748b 15%, var(--color-bg)); color: var(--badge-slate); }
	                                    .api-badge-abstract { background: color-mix(in srgb, #a78bfa 15%, var(--color-bg)); color: var(--badge-lavender); }
	                                    .api-badge-sealed { background: color-mix(in srgb, #f97316 15%, var(--color-bg)); color: var(--badge-orange); }
	                                    .api-badge-obsolete { background: color-mix(in srgb, #ef4444 15%, var(--color-bg)); color: var(--badge-red); }
	                                    .api-badge-sm { font-size: 0.625rem; padding: 0.125rem 0.375rem; border-radius: 3px; vertical-align: middle; }
	                                    .api-badge-sm.api-badge-static { background: color-mix(in srgb, #64748b 12%, var(--color-bg)); color: var(--badge-slate); }
	                                    .api-badge-sm.api-badge-virtual { background: color-mix(in srgb, #06b6d4 12%, var(--color-bg)); color: var(--badge-cyan); }
	                                    .api-badge-sm.api-badge-abstract { background: color-mix(in srgb, #a78bfa 12%, var(--color-bg)); color: var(--badge-lavender); }
	                                    .api-badge-sm.api-badge-obsolete { background: color-mix(in srgb, #ef4444 12%, var(--color-bg)); color: var(--badge-red); }
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
	                                        overflow-wrap: anywhere;
	                                        font-weight: 600;
	                                        font-size: 0.9375rem;
	                                        margin-bottom: 0.5rem;
	                                    }
	                                    .page-content .admonition-icon { font-size: 1rem; line-height: 1; }
	                                    .page-content .admonition-content > :first-child { margin-top: 0; }
	                                    .page-content .admonition-content > :last-child { margin-bottom: 0; }
	                                    .page-content .admonition-note { border-color: #3b82f6; background: color-mix(in srgb, #3b82f6 8%, var(--color-bg)); }
	                                    .page-content .admonition-note .admonition-title { color: var(--admonition-note-text); }
	                                    .page-content .admonition-tip { border-color: #22c55e; background: color-mix(in srgb, #22c55e 8%, var(--color-bg)); }
	                                    .page-content .admonition-tip .admonition-title { color: var(--admonition-tip-text); }
	                                    .page-content .admonition-warning { border-color: #f59e0b; background: color-mix(in srgb, #f59e0b 8%, var(--color-bg)); }
	                                    .page-content .admonition-warning .admonition-title { color: var(--admonition-warning-text); }
	                                    .page-content .admonition-caution { border-color: #f59e0b; background: color-mix(in srgb, #f59e0b 8%, var(--color-bg)); }
	                                    .page-content .admonition-caution .admonition-title { color: var(--admonition-warning-text); }
	                                    .page-content .admonition-danger { border-color: #ef4444; background: color-mix(in srgb, #ef4444 8%, var(--color-bg)); }
	                                    .page-content .admonition-danger .admonition-title { color: var(--admonition-danger-text); }
	                                    .page-content .admonition-info { border-color: #8b5cf6; background: color-mix(in srgb, #8b5cf6 8%, var(--color-bg)); }
	                                    .page-content .admonition-info .admonition-title { color: var(--admonition-info-text); }
	                                    .page-content .admonition-important { border-color: #8b5cf6; background: color-mix(in srgb, #8b5cf6 8%, var(--color-bg)); }
	                                    .page-content .admonition-important .admonition-title { color: var(--admonition-info-text); }
	                                    /* Each type's color and its text shade, for theme.options.layout.admonitions. --ad-ink is the title
	                                       color: the text shade in the default style, a style's own mix of the color in the others. */
	                                    .admonition-note { --ad-c: #3b82f6; --ad-text: var(--admonition-note-text); }
	                                    .admonition-tip { --ad-c: #22c55e; --ad-text: var(--admonition-tip-text); }
	                                    .admonition-warning, .admonition-caution { --ad-c: #f59e0b; --ad-text: var(--admonition-warning-text); }
	                                    .admonition-danger { --ad-c: #ef4444; --ad-text: var(--admonition-danger-text); }
	                                    .admonition-info, .admonition-important { --ad-c: #8b5cf6; --ad-text: var(--admonition-info-text); }
	                                    .admonition { --ad-ink: color-mix(in srgb, var(--st-c, var(--ad-text, var(--color-text))) var(--st-hue, 100%), var(--st-ink, #000000)); }

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
	                                    .tab-header.active { color: var(--color-primary-text); border-bottom-color: var(--color-primary); }
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
	                                        overflow-wrap: anywhere;
	                                        margin-bottom: 0.625rem;
	                                    }
	                                    .component-card-icon {
	                                        display: flex;
	                                        align-items: center;
	                                        color: var(--color-primary-text);
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
	                                        background: var(--color-primary-fill);
	                                        color: var(--color-on-primary);
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
	                                        grid-template-columns: repeat(auto-fill, minmax(min(100%, 250px), 1fr));
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
	                                        top: var(--header-offset);
	                                        width: var(--toc-width);
	                                        height: calc(100vh - var(--header-offset));
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
	                                    .toc-level-5 a { padding-left: 3rem; }
	                                    .toc-level-6 a { padding-left: 3.75rem; }

	                                    /* ToC progress indicator */
	                                    .toc-progress {
	                                        height: 3px;
	                                        background: var(--color-border);
	                                        border-radius: 2px;
	                                        margin-bottom: 0.75rem;
	                                        overflow: hidden;
	                                    }
	                                    /* Scaled, not resized: a transform doesn't lay the page out again on every scroll frame. */
	                                    .toc-progress-bar {
	                                        height: 100%;
	                                        background: var(--color-primary);
	                                        border-radius: 2px;
	                                        width: 100%;
	                                        transform: scaleX(0);
	                                        transform-origin: left center;
	                                        transition: transform 0.15s ease;
	                                    }

	                                    /* ToC expand/collapse for nested items: a caret button next to the link (main.js adds it) */
	                                    .toc-item.has-children {
	                                        display: grid;
	                                        grid-template-columns: minmax(0, 1fr) auto;
	                                        align-items: start;
	                                    }
	                                    .toc-item.has-children > ul { grid-column: 1 / -1; }
	                                    .toc-caret {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 1.5rem;
	                                        height: 1.5rem;
	                                        margin-top: 0.125rem;
	                                        padding: 0;
	                                        border: none;
	                                        border-radius: var(--radius-sm);
	                                        background: none;
	                                        color: var(--color-text-muted);
	                                        cursor: pointer;
	                                    }
	                                    .toc-caret:hover { color: var(--color-text); background: var(--color-bg-secondary); }
	                                    .toc-caret::before {
	                                        content: '';
	                                        border-left: 4px solid transparent;
	                                        border-right: 4px solid transparent;
	                                        border-top: 5px solid currentColor;
	                                        transition: transform 0.2s;
	                                    }
	                                    .toc-item.has-children.collapsed > .toc-caret::before {
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

	                                    /* Page actions (page.markdown_url): Copy page and View as Markdown above the content. Copy page is
	                                       js-only, so the no-JS rules hide it. */
	                                    .page-actions {
	                                        display: flex;
	                                        flex-wrap: wrap;
	                                        justify-content: flex-end;
	                                        gap: 0.5rem;
	                                        margin: 0 0 0.75rem;
	                                        font-size: 0.8125rem;
	                                    }
	                                    .page-actions .page-action {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.375rem;
	                                        padding: 0.25rem 0.625rem;
	                                        border: 1px solid var(--color-border);
	                                        border-radius: var(--radius, 6px);
	                                        background: none;
	                                        color: var(--color-text-secondary);
	                                        font: inherit;
	                                        line-height: 1.4;
	                                        text-decoration: none;
	                                        cursor: pointer;
	                                    }
	                                    .page-actions .page-action:hover { border-color: var(--color-text-secondary); color: var(--color-text); text-decoration: none; }
	                                    .page-actions .page-action:focus-visible { outline: 2px solid currentColor; outline-offset: 2px; }
	                                    .page-actions .page-action[hidden] { display: none; }

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
	                                    .breadcrumbs li { display: flex; align-items: center; min-width: 0; overflow-wrap: anywhere; }
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
	                                        max-width: var(--layout-max-width);
	                                        margin: 0 auto;
	                                        display: flex;
	                                        justify-content: space-between;
	                                        font-size: 0.8125rem;
	                                        color: var(--color-text-muted);
	                                    }
	                                    .built-with a { color: var(--color-text-secondary); }
	                                    .footer-social { display: flex; align-items: center; gap: 0.75rem; }
	                                    .footer-social a { color: var(--color-text-muted); transition: color var(--transition); display: flex; }
	                                    .footer-social a:hover { color: var(--color-primary-text); }

	                                    /* Heading anchor links */
	                                    .page-content h2, .page-content h3, .page-content h4 { position: relative; }
	                                    .heading-anchor {
	                                        position: absolute; left: -1.5rem; top: 50%; transform: translateY(-50%);
	                                        color: var(--color-text-muted); text-decoration: none; font-size: 0.875rem;
	                                        opacity: 0; transition: opacity 0.15s; font-weight: 400;
	                                    }
	                                    .page-content h2:hover .heading-anchor,
	                                    .page-content h3:hover .heading-anchor,
	                                    .page-content h4:hover .heading-anchor,
	                                    .heading-anchor:focus-visible { opacity: 1; }
	                                    .heading-anchor:hover { color: var(--color-primary-text); }
	                                    .heading-anchor:focus-visible { outline: 2px solid var(--color-primary-text); outline-offset: 2px; border-radius: 2px; }
	                                    /* No gutter left of the heading on a phone: the anchor follows the text there. */
	                                    @media (max-width: 768px) {
	                                        .heading-anchor { position: static; margin-left: 0.375rem; transform: none; }
	                                    }

	                                    /* Responsive */
	                                    @media (max-width: 1200px) {
	                                        .toc-sidebar {
	                                            display: none;
	                                            position: fixed;
	                                            top: var(--header-offset);
	                                            right: 0;
	                                            bottom: 0;
	                                            width: 280px;
	                                            max-width: 80vw;
	                                            background: var(--color-bg);
	                                            border-left: 1px solid var(--color-border);
	                                            box-shadow: -4px 0 24px rgba(0,0,0,0.1);
	                                            z-index: var(--z-drawer);
	                                            padding: 1.5rem;
	                                            overflow-y: auto;
	                                            transition: transform 0.3s ease;
	                                            height: auto;
	                                        }
	                                        .toc-sidebar.open { display: block; }
	                                        .toc-toggle-btn {
	                                            display: flex;
	                                            position: fixed;
	                                            bottom: calc(var(--fab-bottom) + var(--fab-size) + var(--fab-gap));
	                                            right: var(--fab-right);
	                                            width: var(--fab-size);
	                                            height: var(--fab-size);
	                                            align-items: center;
	                                            justify-content: center;
	                                            background: var(--color-primary-fill);
	                                            color: var(--color-on-primary);
	                                            border: none;
	                                            border-radius: 50%;
	                                            cursor: pointer;
	                                            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
	                                            z-index: var(--z-fab);
	                                            transition: opacity 0.3s, transform 0.3s;
	                                        }
	                                        .toc-toggle-btn:hover { transform: scale(1.1); }
	                                        .toc-toggle-btn svg { width: 20px; height: 20px; }
	                                    }
	                                    @media (min-width: 1201px) {
	                                        .toc-toggle-btn { display: none; }
	                                    }

	                                    @media (max-width: 768px) {
	                                        .sidebar { display: none; position: fixed; top: var(--header-offset); left: 0; bottom: 0; z-index: var(--z-drawer); background: var(--color-bg); }
	                                        .sidebar.open { display: block; }
	                                        .mobile-nav-toggle { display: flex; }
	                                        .content { padding: 1.5rem 1rem; }
	                                        .search-label, .search-trigger kbd { display: none; }
	                                        /* Bigger touch targets, and search as an icon button like the ones beside it. A style or navbar variant
	                                           that sets its own --header-button keeps it. */
	                                        :root { --header-button: 40px; }
	                                        .site-header .header-actions .search-trigger { justify-content: center; width: var(--header-button); height: var(--header-button); padding: 0; }
	                                        .header-inner { padding: 0 0.75rem; gap: 0.5rem; }
	                                        .site-logo { gap: 0.375rem; min-width: 0; }
	                                        .site-name { font-size: 0.9375rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	                                        .header-actions { gap: 0.25rem; flex-shrink: 0; }
	                                        .search-modal { padding-top: 5vh; }
	                                        .search-dialog { width: 95%; max-height: 70vh; }
	                                        .footer-inner { flex-direction: column; gap: 0.5rem; align-items: center; }
	                                    }
	                                    /* The site name keeps its room beside four buttons on the smallest phones. */
	                                    @media (max-width: 359px) {
	                                        :root { --header-button: 36px; }
	                                    }

	                                    /* Search Modal */
	                                    .search-modal { position: fixed; inset: 0; z-index: var(--z-modal); display: flex; align-items: flex-start; justify-content: center; padding-top: 15vh; }
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
	                                    .search-results:empty { display: none; }
	                                    .search-message {
	                                        padding: 2rem;
	                                        text-align: center;
	                                        color: var(--color-text-muted);
	                                        font-size: 0.875rem;
	                                    }
	                                    .search-message[hidden] { display: none; }
	                                    .search-close {
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        min-width: 2rem;
	                                        min-height: 2rem;
	                                        padding: 0;
	                                        border: none;
	                                        background: none;
	                                        color: var(--color-text-muted);
	                                        cursor: pointer;
	                                    }
	                                    .search-close:hover { color: var(--color-text); }
	                                    .search-close-icon { display: none; }
	                                    /* Touch screens get an X: there is no Escape key to press. */
	                                    @media (hover: none), (max-width: 768px) {
	                                        .search-close .search-esc { display: none; }
	                                        .search-close-icon { display: block; }
	                                    }
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
	                                    .search-result-snippet mark { background: color-mix(in srgb, var(--color-accent) 30%, transparent); color: var(--color-text); border-radius: 2px; }

	                                    /* Prev/Next page navigation */
	                                    /* Inside the content column, under the article, so both line up with it at every width. */
	                                    .page-nav {
	                                        display: grid;
	                                        grid-template-columns: repeat(2, minmax(0, 1fr));
	                                        gap: 1rem;
	                                        margin: 2.5rem 0 0;
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
	                                    @media (max-width: 640px) {
	                                        .page-nav { grid-template-columns: minmax(0, 1fr); gap: 0.75rem; }
	                                        .page-nav-spacer { display: none; }
	                                    }

	                                    /* Feedback widget */
	                                    .feedback-widget {
	                                        display: flex;
	                                        flex-direction: column;
	                                        align-items: center;
	                                        gap: 0.75rem;
	                                        margin: 2.5rem 0 0;
	                                        padding: 1.5rem 0 0;
	                                        text-align: center;
	                                    }
	                                    .feedback-prompt {
	                                        min-width: 0;
	                                        overflow-wrap: anywhere;
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
	                                    /* The chosen answer is a solid fill: dark green or red under white text in light mode, light
	                                       ones under the page color in dark mode. The raw #22c55e under white was 2.3:1. */
	                                    :root { --feedback-yes: #15803d; --feedback-no: #b91c1c; --feedback-on: #ffffff; }
	                                    [data-theme="dark"] { --feedback-yes: #4ade80; --feedback-no: #f87171; --feedback-on: #0f172a; }
	                                    .feedback-btn-yes:hover { border-color: #22c55e; color: var(--feedback-yes); background: color-mix(in srgb, #22c55e 8%, transparent); }
	                                    .feedback-btn-no:hover { border-color: #ef4444; color: var(--feedback-no); background: color-mix(in srgb, #ef4444 8%, transparent); }
	                                    .feedback-btn-yes.selected { border-color: var(--feedback-yes); color: var(--feedback-on); background: var(--feedback-yes); }
	                                    .feedback-btn-no.selected { border-color: var(--feedback-no); color: var(--feedback-on); background: var(--feedback-no); }
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

	                                    /* Floating buttons: back to top, and the "On this page" button and panel above it, in one corner.
	                                       --fab-stack is the height of the buttons on the page; the footer grows by --fab-clear so its last
	                                       line stays clear of them at the end of the page. min() makes it 0 without buttons. */
	                                    :root {
	                                        --fab-right: 1.5rem;
	                                        --fab-bottom: 1.5rem;
	                                        --fab-size: 40px;
	                                        --fab-gap: 0.75rem;
	                                        --fab-stack: 0px;
	                                        --fab-clear: min(calc(var(--fab-stack) * 100), calc(var(--fab-stack) + var(--fab-bottom) - 0.5rem));
	                                    }
	                                    html:not(.no-js):has(.back-to-top) { --fab-stack: var(--fab-size); }
	                                    @media (max-width: 1200px) {
	                                        html:not(.no-js):has(.toc-toggle-btn) { --fab-stack: calc(2 * var(--fab-size) + var(--fab-gap)); }
	                                    }
	                                    @media (max-width: 768px) {
	                                        :root { --fab-right: 1rem; --fab-bottom: 1rem; }
	                                    }
	                                    .site-footer::after { content: ''; display: block; height: var(--fab-clear); }
	                                    .back-to-top { position: fixed; bottom: var(--fab-bottom); right: var(--fab-right); width: var(--fab-size); height: var(--fab-size); border-radius: 50%; background: var(--color-primary-fill); color: var(--color-on-primary); border: none; font-size: 1.25rem; cursor: pointer; opacity: 0; pointer-events: none; transition: opacity 0.2s; z-index: var(--z-fab); box-shadow: var(--shadow-md); }
	                                    .back-to-top.visible { opacity: 1; pointer-events: auto; }

	                                    /* =============================== */
	                                    /* Landing Page                    */
	                                    /* =============================== */

	                                    .landing .landing-content {
	                                        max-width: 1200px;
	                                        margin: 0 auto;
	                                        padding: 0 2rem;
	                                    }

	                                    /* Subtle hero background glow, an 800x500 ellipse at the top. The box spans the page and the gradient
	                                       has the size an 800px box gave it, so it paints as before: the 800px box stuck out past both edges
	                                       of a narrower screen, which then scrolled sideways. */
	                                    .landing::before {
	                                        content: '';
	                                        position: absolute;
	                                        top: 0;
	                                        left: 0;
	                                        right: 0;
	                                        height: 500px;
	                                        background: radial-gradient(565.69px 353.55px at 50% 50%, color-mix(in srgb, var(--color-primary) 8%, transparent) 0%, transparent 70%);
	                                        pointer-events: none;
	                                        z-index: 0;
	                                    }
	                                    .landing { position: relative; }
	                                    .landing .landing-content { position: relative; z-index: 1; }

	                                    /* Hero - first h1 + first p */
	                                    .landing .landing-content > h1:first-child,
	                                    .landing .landing-content > h1:first-of-type {
	                                        font-size: clamp(2.25rem, 6vw, 4.5rem);
	                                        font-weight: 800;
	                                        text-align: center;
	                                        margin: 5rem auto 1.25rem;
	                                        max-width: 800px;
	                                        letter-spacing: -0.03em;
	                                        line-height: 1.1;
	                                        background: linear-gradient(135deg, var(--color-primary-text) 0%, var(--gradient-text-secondary) 50%, var(--gradient-text-tertiary) 100%);
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
	                                        background: var(--color-primary-fill);
	                                        color: var(--color-on-primary);
	                                        box-shadow: var(--shadow-md);
	                                    }
	                                    .landing .landing-content > h1:first-of-type + p + p a:first-child:hover,
	                                    .landing .landing-content > h1:first-of-type + p + ul a:first-of-type:hover {
	                                        background: var(--color-primary-fill-hover);
	                                        color: var(--color-on-primary);
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
	                                        color: var(--color-primary-text);
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
	                                        grid-template-columns: repeat(auto-fit, minmax(min(100%, 280px), 1fr));
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
	                                        color: var(--color-primary-text);
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
	                                    .landing-content pre .copy-btn { position: absolute; top: 0.5rem; right: 0.5rem; background: var(--sh-copy-bg); border: 1px solid var(--sh-copy-border); color: var(--sh-chrome); padding: 0.25rem 0.625rem; border-radius: var(--radius-sm); font-size: 0.75rem; font-family: var(--font-mono); cursor: pointer; opacity: 0; transition: opacity 0.15s ease, background var(--transition), color var(--transition), border-color var(--transition); }
	                                    .landing-content pre .copy-btn:hover { background: var(--sh-copy-bg-hover); color: var(--sh-chrome-strong); border-color: var(--sh-copy-border-hover); }
	                                    .landing-content pre:hover .copy-btn, .landing-content pre .copy-btn:focus-visible { opacity: 1; }
	                                    /* Same corner as the Copy button: hide at once, return after the button fades out. */
	                                    .landing-content pre:hover .code-lang, .landing-content pre:focus-within .code-lang { opacity: 0; transition: none; }
	                                    @media (hover: none) { .landing-content pre .copy-btn { opacity: 1; } .landing-content pre .code-lang { opacity: 0; } }
	                                    .landing-content pre .code-lang { position: absolute; top: 0.5rem; right: 0.5rem; font-size: 0.6875rem; font-family: var(--font-mono); text-transform: uppercase; letter-spacing: 0.05em; color: var(--sh-chrome); user-select: none; transition: opacity 0.15s ease 0.15s; }
	                                    .landing-content .code-block { --code-top: 1.25rem; --code-pad-x: 1.5rem; --code-lh: 1.7; max-width: 720px; margin: 2rem auto; }
	                                    .landing-content .code-block > pre { margin: 0; max-width: none; padding: var(--code-top) 0 0; overflow: hidden; }
	                                    .landing-content .code-block > pre > code { padding-bottom: 1.25rem; }

	                                    @media (max-width: 768px) {
	                                        .landing .landing-content { padding: 0 1rem; }
	                                        .landing .landing-content > h1:first-of-type { margin-top: 2rem; }
	                                        .landing .landing-content > ul { grid-template-columns: minmax(0, 1fr); }
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
	                                        z-index: var(--z-popover);
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
	                                        .version-trigger { min-height: var(--header-button); padding-left: 0.5rem; padding-right: 0.5rem; }
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
	                                        margin: 1.5rem 0;
	                                        background: transparent;
	                                        border: none;
	                                        overflow-x: auto;
	                                    }
	                                    /* Auto margins center the diagram but, unlike justify-content: center, give way when
	                                       it is wider than the box, so its left edge can still be scrolled to. */
	                                    .mermaid > svg { flex: none; margin-inline: auto; }

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
	                                        color: var(--color-primary-text);
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
	                                        background: linear-gradient(135deg, var(--color-primary-text) 0%, var(--color-primary-text-hover) 100%);
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
	                                        -webkit-text-fill-color: var(--color-primary-text);
	                                    }

	                                    /* --- 4. Sidebar polish --- */
	                                    .nav-children {
	                                        overflow: hidden;
	                                        transition: max-height 0.3s ease, opacity 0.25s ease, visibility 0s linear 0s;
	                                    }
	                                    /* Hidden once the collapse has played, so a closed section's links are neither
	                                       focusable nor read out. An open list doesn't set visibility: a nested open list
	                                       inside a closed one has to stay hidden with it. */
	                                    .nav-children:not(.expanded) {
	                                        max-height: 0 !important;
	                                        opacity: 0;
	                                        visibility: hidden;
	                                        transition: max-height 0.3s ease, opacity 0.25s ease, visibility 0s linear 0.3s;
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
	                                        background: var(--sh-chrome-bg) !important;
	                                        color: var(--sh-chrome) !important;
	                                        border-bottom: 1px solid var(--sh-chrome-line);
	                                        border-left: 1px solid var(--sh-chrome-line);
	                                        user-select: none;
	                                        transition: opacity 0.15s ease 0.15s;
	                                        font-weight: 500;
	                                    }

	                                    /* Better line number styling */
	                                    .page-content pre .line-numbers {
	                                        border-right-color: var(--sh-chrome-line) !important;
	                                        font-size: 0.8rem !important;
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
	                                    /* Hidden, not just transparent, once it has faded out: no tab stop while it is invisible. */
	                                    .back-to-top {
	                                        opacity: 0 !important;
	                                        visibility: hidden;
	                                        transform: translateY(16px);
	                                        pointer-events: none;
	                                        transition: opacity 0.3s ease, transform 0.3s ease, background 0.2s ease, box-shadow 0.2s ease, visibility 0s linear 0.3s !important;
	                                    }
	                                    .back-to-top.visible {
	                                        opacity: 1 !important;
	                                        visibility: visible;
	                                        transform: translateY(0);
	                                        pointer-events: auto;
	                                        transition: opacity 0.3s ease, transform 0.3s ease, background 0.2s ease, box-shadow 0.2s ease, visibility 0s !important;
	                                    }
	                                    .back-to-top:hover {
	                                        background: var(--color-primary-fill-hover) !important;
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
	                                    /* Mermaid measures each node right after creating it. Under the two rules above every
	                                       new node started a 0.01ms transition, the measurement read its start values and
	                                       diagrams were laid out thousands of pixels wide, then shrunk to fit. */
	                                    .mermaid, .mermaid *, .mermaid *::before, .mermaid *::after { transition-duration: 0s !important; }
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
	                                        font-size: clamp(2.25rem, 6vw, 4.5rem);
	                                        font-weight: 800;
	                                        letter-spacing: -0.03em;
	                                        line-height: 1.1;
	                                        margin: 0 auto 1.25rem;
	                                        max-width: 800px;
	                                        background: linear-gradient(135deg, var(--color-primary-text) 0%, var(--gradient-text-secondary) 50%, var(--gradient-text-tertiary) 100%);
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
	                                        overflow-wrap: anywhere;
	                                    }
	                                    .landing-btn-primary {
	                                        background: var(--color-primary-fill);
	                                        color: var(--color-on-primary);
	                                        box-shadow: 0 4px 14px color-mix(in srgb, var(--color-primary) 40%, transparent);
	                                    }
	                                    .landing-btn-primary:hover {
	                                        background: var(--color-primary-fill-hover);
	                                        color: var(--color-on-primary);
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
	                                        color: var(--color-primary-text);
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
	                                        grid-template-columns: repeat(3, minmax(0, 1fr));
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
	                                        color: var(--color-primary-text);
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
	                                    @media (max-width: 768px) {
	                                        .landing-md-content table { display: block; overflow-x: auto; -webkit-overflow-scrolling: touch; }
	                                    }

	                                    /* Landing footer */
	                                    .landing-footer {
	                                        text-align: center;
	                                        padding: 2.5rem 1rem;
	                                        border-top: 1px solid var(--color-border);
	                                        color: var(--color-text-muted);
	                                        font-size: 0.875rem;
	                                    }
	                                    .landing-footer-heart { color: var(--color-primary-text); }
	                                    .landing-footer a { color: var(--color-text-secondary); }
	                                    .landing-footer a:hover { color: var(--color-primary-text); }

	                                    /* Divider */
	                                    .landing-divider {
	                                        border: none;
	                                        border-top: 1px solid var(--color-border);
	                                        max-width: 200px;
	                                        margin: 0 auto;
	                                    }

	                                    /* Responsive */
	                                    @media (max-width: 900px) {
	                                        .landing-features-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	                                    }
	                                    @media (max-width: 600px) {
	                                        .landing-hero { padding: 3rem 1rem 2.5rem; }
	                                        .landing-features { padding: 2rem 1rem; }
	                                        .landing-features-grid { grid-template-columns: minmax(0, 1fr); }
	                                        .landing-feature-card { padding: 1.5rem 1.25rem; }
	                                        .landing-md-content { padding: 0 1rem 2rem; }
	                                    }
	                                    /* The hero and call to action buttons stack at full width, labels wrapping inside them. */
	                                    @media (max-width: 480px) {
	                                        .landing-hero-actions { flex-direction: column; align-items: stretch; }
	                                        .landing-btn { width: 100%; min-width: 0; text-align: center; }
	                                        .landing .landing-content > h1:first-of-type + p + p,
	                                        .landing .landing-content > h1:first-of-type + p + ul { flex-direction: column; align-items: stretch; }
	                                        .landing .landing-content > h1:first-of-type + p + p a,
	                                        .landing .landing-content > h1:first-of-type + p + ul a { justify-content: center; }
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
	                                        grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
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
	                                        .blog-pager { grid-template-columns: repeat(2, minmax(0, 1fr)); }
	                                        .blog-pager-status { grid-column: 1 / -1; order: -1; }
	                                    }

	                                    /* Client runtime: skip link, mobile sidebar backdrop, image zoom, no-JS fallbacks */
	                                    .visually-hidden {
	                                        position: absolute !important;
	                                        width: 1px;
	                                        height: 1px;
	                                        padding: 0;
	                                        margin: -1px;
	                                        overflow: hidden;
	                                        clip: rect(0, 0, 0, 0);
	                                        white-space: nowrap;
	                                        border: 0;
	                                    }
	                                    .skip-link {
	                                        position: absolute;
	                                        top: 0.5rem;
	                                        left: 0.5rem;
	                                        z-index: calc(var(--z-modal) + 1);
	                                        padding: 0.5rem 1rem;
	                                        border: 2px solid var(--color-primary-text);
	                                        border-radius: var(--radius);
	                                        background: var(--color-bg);
	                                        color: var(--color-text);
	                                        font-size: 0.875rem;
	                                        font-weight: 600;
	                                        transform: translateY(-200%);
	                                    }
	                                    .skip-link:focus { transform: none; }
	                                    /* Back to top focuses the link; a mouse click there shouldn't reveal it. */
	                                    .skip-link:focus:not(:focus-visible) { transform: translateY(-200%); }
	                                    #mokadocs-main:focus { outline: none; }
	                                    /* The reduced-motion rules reach every element below <html>, not <html> itself. */
	                                    html[data-no-animations] { scroll-behavior: auto; }

	                                    /* The script adds one under each drawer: the mobile sidebar (phones) and the "On this page" drawer
	                                       (up to 1200px, or the toc: floating popover on phones). */
	                                    .drawer-backdrop {
	                                        display: none;
	                                        position: fixed;
	                                        inset: var(--header-offset) 0 0;
	                                        z-index: var(--z-backdrop);
	                                        background: rgba(0, 0, 0, 0.4);
	                                    }
	                                    @media (max-width: 1200px) {
	                                        .toc-backdrop.open { display: block; }
	                                    }
	                                    @media (max-width: 768px) {
	                                        .nav-backdrop.open { display: block; }
	                                        .sidebar.open { box-shadow: var(--shadow-lg); }
	                                    }
	                                    @media (min-width: 769px) {
	                                        html[data-toc="floating"] .toc-backdrop.open { display: none; }
	                                    }

	                                    .page-content img.zoomable, .landing-content img.zoomable { cursor: zoom-in; }
	                                    img.zoomable:focus-visible { outline: 2px solid var(--color-primary-text); outline-offset: 2px; }
	                                    .lightbox {
	                                        position: fixed;
	                                        inset: 0;
	                                        z-index: var(--z-modal);
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        padding: 3.5rem 1rem 1.5rem;
	                                        background: rgba(0, 0, 0, 0.85);
	                                        cursor: zoom-out;
	                                    }
	                                    .lightbox[hidden] { display: none; }
	                                    .lightbox-image {
	                                        max-width: 100%;
	                                        max-height: 100%;
	                                        width: auto;
	                                        height: auto;
	                                        object-fit: contain;
	                                        border-radius: var(--radius);
	                                        background: var(--color-bg);
	                                    }
	                                    .lightbox-close {
	                                        position: absolute;
	                                        top: 0.75rem;
	                                        right: 0.75rem;
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 2.5rem;
	                                        height: 2.5rem;
	                                        padding: 0;
	                                        border: 1px solid rgba(255, 255, 255, 0.4);
	                                        border-radius: 50%;
	                                        background: rgba(0, 0, 0, 0.6);
	                                        color: #ffffff;
	                                        font-size: 1.5rem;
	                                        line-height: 1;
	                                        cursor: pointer;
	                                    }
	                                    .lightbox-close:focus-visible { outline: 2px solid #ffffff; outline-offset: 2px; }
	                                    html.lightbox-open { overflow: hidden; }
	                                    @media (prefers-reduced-motion: no-preference) {
	                                        html:not([data-no-animations]) .lightbox:not([hidden]) { animation: lightboxFade 0.15s ease-out; }
	                                        html:not([data-no-animations]) .lightbox:not([hidden]) .lightbox-image { animation: lightboxZoom 0.2s ease-out; }
	                                    }
	                                    @keyframes lightboxFade { from { opacity: 0; } to { opacity: 1; } }
	                                    @keyframes lightboxZoom { from { transform: scale(0.94); } to { transform: none; } }

	                                    /* Announcement banner (theme.options.banner). It sits above the header and isn't sticky, so it
	                                       scrolls away and the header sticks as before. The script right after it hides a dismissed one
	                                       before the first paint. Text is --color-text on a light tint, the link the accent text shade,
	                                       which is made for 4.5:1 on an 8% tint of the primary (6% leaves a margin); accent is a fill
	                                       like the buttons. */
	                                    .site-banner {
	                                        --banner-bg: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg));
	                                        --banner-text: var(--color-text);
	                                        --banner-link: var(--color-primary-text);
	                                        --banner-border: color-mix(in srgb, var(--color-primary) 22%, var(--color-bg));
	                                        --banner-icon: currentColor;
	                                        background: var(--banner-bg);
	                                        color: var(--banner-text);
	                                        border-bottom: 1px solid var(--banner-border);
	                                        font-size: 0.875rem;
	                                        line-height: 1.5;
	                                    }
	                                    .site-banner[hidden] { display: none; }
	                                    .site-banner[data-variant="warning"] {
	                                        --banner-bg: color-mix(in srgb, #f59e0b 14%, var(--color-bg));
	                                        --banner-link: var(--color-text);
	                                        --banner-border: color-mix(in srgb, #f59e0b 45%, var(--color-bg));
	                                        --banner-icon: var(--admonition-warning-text);
	                                    }
	                                    .site-banner[data-variant="accent"] {
	                                        --banner-bg: var(--color-primary-fill);
	                                        --banner-text: var(--color-on-primary);
	                                        --banner-link: var(--color-on-primary);
	                                        --banner-border: var(--color-primary-fill);
	                                    }
	                                    .site-banner-inner {
	                                        position: relative;
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        gap: 0.5rem;
	                                        max-width: var(--layout-max-width);
	                                        min-height: 2.5rem;
	                                        margin: 0 auto;
	                                        padding: 0.5rem 3rem;
	                                    }
	                                    .site-banner-icon { flex-shrink: 0; color: var(--banner-icon); }
	                                    .site-banner-text { margin: 0; text-align: center; overflow-wrap: anywhere; }
	                                    .site-banner-link,
	                                    .site-banner-link:hover {
	                                        display: inline-flex;
	                                        align-items: center;
	                                        gap: 0.25rem;
	                                        margin-left: 0.25rem;
	                                        color: var(--banner-link);
	                                        font-weight: 600;
	                                        text-decoration: underline;
	                                        text-underline-offset: 0.2em;
	                                    }
	                                    .site-banner-link:hover { text-decoration-thickness: 2px; }
	                                    .site-banner-link svg { flex-shrink: 0; }
	                                    .site-banner-link:focus-visible, .site-banner-close:focus-visible { outline: 2px solid currentColor; outline-offset: 2px; box-shadow: none; }
	                                    .site-banner-close {
	                                        position: absolute;
	                                        top: 50%;
	                                        right: 0.75rem;
	                                        display: flex;
	                                        align-items: center;
	                                        justify-content: center;
	                                        width: 2rem;
	                                        height: 2rem;
	                                        margin-top: -1rem;
	                                        padding: 0;
	                                        border: none;
	                                        border-radius: var(--radius-sm);
	                                        background: transparent;
	                                        color: var(--banner-text);
	                                        cursor: pointer;
	                                    }
	                                    .site-banner-close:hover { background: color-mix(in srgb, currentColor 14%, transparent); }
	                                    @media (max-width: 768px) {
	                                        .site-banner-inner { justify-content: flex-start; padding: 0.5rem 3rem 0.5rem 1rem; }
	                                        .site-banner-text { text-align: left; }
	                                    }
	                                    /* While the banner is on screen the header sits below it: the drawers that open under the header move
	                                       down by what is left of it, and the sticky sidebars get shorter by as much so their ends stay in view.
	                                       The script keeps --banner-offset at the banner's visible height. The :where() keeps these below the
	                                       layout variants, such as the floating "On this page" panel. */
	                                    :where(.site-banner:not([hidden]) ~ .site-container) :is(.sidebar, .toc-sidebar) {
	                                        max-height: calc(100vh - var(--header-offset) - var(--banner-offset, 0px));
	                                    }
	                                    @media (max-width: 768px) {
	                                        :where(.site-banner:not([hidden]) ~ .site-container) .sidebar { margin-top: var(--banner-offset, 0px); }
	                                    }
	                                    @media (max-width: 1200px) {
	                                        :where(.site-banner:not([hidden]) ~ .site-container) .toc-sidebar { margin-top: var(--banner-offset, 0px); }
	                                    }
	                                    :where(.site-banner:not([hidden]) ~ .drawer-backdrop) { margin-top: var(--banner-offset, 0px); }

	                                    /* Without JavaScript. The head bootstrap removes .no-js before the first paint, so
	                                       these only apply when scripts don't run: controls that need a script are hidden, and
	                                       what a script would reveal is shown. Give another script-only control class js-only. */
	                                    html.no-js :is(.search-trigger, .version-selector, .theme-toggle, .mobile-nav-toggle, .nav-toggle, .toc-toggle-btn, .toc-progress, .back-to-top, .feedback-widget, .install-copy-btn, .js-only) { display: none !important; }
	                                    html.no-js .nav-children:not(.expanded) { max-height: none !important; opacity: 1; visibility: visible; }
	                                    @media (max-width: 768px) {
	                                        html.no-js .site-container { flex-direction: column; }
	                                        html.no-js .sidebar {
	                                            display: block;
	                                            position: static;
	                                            order: 2;
	                                            width: auto;
	                                            height: auto;
	                                            border-right: none;
	                                            border-top: 1px solid var(--color-border);
	                                        }
	                                    }
	                                    /* Every tab panel shows, each under its own label: the tab row dissolves into the
	                                       group and order interleaves labels and panels. */
	                                    html.no-js .tabs { display: flex; flex-direction: column; }
	                                    html.no-js .tabs > .tab-headers { display: contents; }
	                                    html.no-js .tabs > .tab-headers > .tab-header {
	                                        order: 99;
	                                        padding: 0.5rem 1.25rem;
	                                        border-bottom: 1px solid var(--color-border);
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                        font-weight: 600;
	                                        text-align: left;
	                                        cursor: default;
	                                    }
	                                    html.no-js .tabs > .tab-content { order: 99; }
	                                    html.no-js .tabs > .tab-content[hidden] { display: block; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(1) { order: 1; }
	                                    html.no-js .tabs > .tab-content:nth-child(2) { order: 2; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(2) { order: 3; }
	                                    html.no-js .tabs > .tab-content:nth-child(3) { order: 4; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(3) { order: 5; }
	                                    html.no-js .tabs > .tab-content:nth-child(4) { order: 6; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(4) { order: 7; }
	                                    html.no-js .tabs > .tab-content:nth-child(5) { order: 8; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(5) { order: 9; }
	                                    html.no-js .tabs > .tab-content:nth-child(6) { order: 10; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(6) { order: 11; }
	                                    html.no-js .tabs > .tab-content:nth-child(7) { order: 12; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(7) { order: 13; }
	                                    html.no-js .tabs > .tab-content:nth-child(8) { order: 14; }
	                                    html.no-js .tabs > .tab-headers > .tab-header:nth-child(8) { order: 15; }
	                                    html.no-js .tabs > .tab-content:nth-child(9) { order: 16; }
	                                    html.no-js .package-install-widget { display: flex; flex-direction: column; }
	                                    html.no-js .install-tabs { display: contents; }
	                                    html.no-js .install-tab {
	                                        padding: 0.5rem 1rem;
	                                        border-bottom: 1px solid var(--color-border);
	                                        background: var(--color-bg-secondary);
	                                        color: var(--color-text);
	                                        text-align: left;
	                                        cursor: default;
	                                    }
	                                    html.no-js .install-panel { display: flex; }
	                                    html.no-js .install-tab[data-tab="pm"] { order: 1; }
	                                    html.no-js .install-panel[data-panel="pm"] { order: 2; }
	                                    html.no-js .install-tab[data-tab="cli"] { order: 3; }
	                                    html.no-js .install-panel[data-panel="cli"] { order: 4; }
	                                    html.no-js .install-tab[data-tab="ref"] { order: 5; }
	                                    html.no-js .install-panel[data-panel="ref"] { order: 6; }

	                                    /* Print: the article alone, black on white, at full width. Link targets follow the link text,
	                                       code wraps and takes a light code theme whatever the screen uses, and blocks, tables, admonitions
	                                       and figures stay on one page where they fit. The rules use !important because the styles' colors
	                                       and the code block styles' frames are more specific. */
	                                    @media print {
	                                        html, html[data-theme="dark"] { color-scheme: light; }
	                                        html, body { background: #ffffff !important; }
	                                        body *:not(pre):not(pre *) {
	                                            color: #000000 !important;
	                                            -webkit-text-fill-color: currentColor !important;
	                                            background: transparent !important;
	                                            box-shadow: none !important;
	                                            text-shadow: none !important;
	                                        }
	                                        .skip-link, .site-header, .sidebar, .sidebar-toggle, .toc-sidebar, .toc-inline, .toc-toggle-btn, .back-to-top, .drawer-backdrop,
	                                        .feedback-widget, .page-nav, .page-actions, .edit-link, .search-modal, .lightbox, .heading-anchor,
	                                        .copy-btn, .install-copy-btn, .install-tabs, .code-lang, .landing-hero-actions, .landing-hero-icon, .hero-grid-pattern {
	                                            display: none !important;
	                                        }
	                                        .site-banner { display: none !important; }
	                                        .site-container { display: block; max-width: none; min-height: 0; }
	                                        .content { max-width: none; padding: 0; }
	                                        .page-content, .page-content h1, .page-content h2, .page-content h3 { animation: none !important; }
	                                        .site-footer { padding: 1rem 0 0; }
	                                        .site-footer::after { content: none; }

	                                        .page-content a[href]:not([href^="#"]):not(.api-member-link):not(:has(img))::after,
	                                        .landing-content a[href]:not([href^="#"]):not(:has(img))::after {
	                                            content: " (" attr(href) ")";
	                                            font-size: 0.85em;
	                                            font-weight: 400;
	                                            overflow-wrap: anywhere;
	                                        }
	                                        .breadcrumbs a[href]::after { content: none !important; }

	                                        :is(h1, h2, h3, h4, h5, h6) { break-after: avoid; }
	                                        :is(pre, .code-block, table, .admonition, figure, img, .mermaid, blockquote, .component-card, .install-panel) { break-inside: avoid; }
	                                        .page-content table, .landing-content table { display: table; overflow: visible; }
	                                        .table-responsive { overflow: visible; }
	                                        .page-content :is(th, td) { white-space: normal; }

	                                        html[data-code-theme] {
	                                            --sh-comment: #6e7781 !important; --sh-string: #0a3069 !important; --sh-keyword: #cf222e !important;
	                                            --sh-number: #0550ae !important; --sh-type: #953800 !important; --sh-function: #8250df !important;
	                                            --sh-operator: #cf222e !important; --sh-punctuation: #24292f !important; --sh-attr: #0550ae !important;
	                                            --sh-tag: #116329 !important; --sh-property: #0550ae !important; --sh-line-number: #656d76 !important;
	                                            --sh-line-shade: transparent !important;
	                                            --sh-line-highlight: rgba(0, 0, 0, 0.05) !important;
	                                            --sh-line-highlight-border: #57606a !important;
	                                            --sh-diff-add-bg: rgba(26, 127, 55, 0.06) !important;
	                                            --sh-diff-remove-bg: rgba(207, 34, 46, 0.06) !important;
	                                            --sh-diff-add: #116329 !important;
	                                            --sh-diff-remove: #a40e26 !important;
	                                        }
	                                        html[data-code-theme] :is(.page-content, .landing-content) pre {
	                                            padding-top: 0.75rem !important;
	                                            border: 1px solid #d0d7de !important;
	                                            border-radius: 6px !important;
	                                            background: #ffffff !important;
	                                            overflow: visible !important;
	                                        }
	                                        html[data-code-theme] :is(.page-content, .landing-content) pre::before,
	                                        html[data-code-theme] :is(.page-content, .landing-content) pre::after { content: none !important; }
	                                        html[data-code-theme] :is(.page-content, .landing-content) pre code {
	                                            color: #24292f !important;
	                                            white-space: pre-wrap !important;
	                                            overflow-wrap: anywhere;
	                                            overflow: visible !important;
	                                        }
	                                        .code-block { --code-top: 0.75rem !important; }
	                                        .code-block > pre > code > .line.focused, .code-block[data-focus] > pre > code > .line { opacity: 1 !important; filter: none !important; }
	                                        .code-block > .code-title {
	                                            position: static !important;
	                                            display: block;
	                                            max-width: none !important;
	                                            height: auto !important;
	                                            margin: 0 0 0.25rem;
	                                            padding: 0 !important;
	                                            border: none !important;
	                                            line-height: 1.4 !important;
	                                            transform: none !important;
	                                        }
	                                        /* Wrapped lines have no fixed height for the line number column, so every block numbers its
	                                           lines with a counter, as wrapped blocks do on screen. */
	                                        .code-block > pre > .line-numbers { display: none !important; }
	                                        .code-block.has-line-numbers > pre { padding-left: 0 !important; }
	                                        .code-block.has-line-numbers > pre > code { counter-reset: line; }
	                                        .code-block.has-line-numbers > pre > code > .line {
	                                            position: relative;
	                                            counter-increment: line;
	                                            padding-left: calc(var(--code-gutter) + 0.75rem) !important;
	                                        }
	                                        .code-block.has-line-numbers > pre > code > .line::after {
	                                            content: counter(line);
	                                            content: counter(line) / '';
	                                            position: absolute; top: 0; left: 0; width: var(--code-gutter);
	                                            padding-right: 0.75rem; text-align: right; font-family: var(--font-mono);
	                                            font-size: 0.8rem; color: var(--sh-line-number);
	                                        }
	                                    }

	                                    /* Windows High Contrast and other forced color modes. Backgrounds, gradients and shadows are
	                                       dropped there and every border takes the text color, transparent ones too. So gradient titles get
	                                       a plain system color, borders that only mark a state are Canvas until the state applies, and
	                                       every state that showed as a tint or a fill gets a border or underline in a system color. */
	                                    @media (forced-colors: active) {
	                                        .page-content h1, .landing-hero-title, .landing .landing-content > h1:first-of-type {
	                                            background: none !important;
	                                            color: CanvasText !important;
	                                            -webkit-text-fill-color: CanvasText !important;
	                                        }
	                                        :focus-visible { outline: 2px solid Highlight !important; outline-offset: 2px; }
	                                        .search-input:focus-visible { outline-offset: -2px; }
	                                        .nav-link { border-left-color: Canvas !important; }
	                                        .nav-link.current { border-left-color: Highlight !important; text-decoration: underline; text-underline-offset: 0.2em; }
	                                        .toc-item a { border-left-color: CanvasText !important; }
	                                        .toc-item a.active { border-left: 3px solid Highlight !important; text-decoration: underline; text-underline-offset: 0.2em; }
	                                        .toc-caret::before { border-left-color: Canvas; border-right-color: Canvas; }
	                                        :is(.tab-header, .install-tab, .header-tab) { border-bottom-color: Canvas !important; }
	                                        :is(.tab-header, .install-tab).active, .header-tab.current { border-bottom: 3px solid Highlight !important; }
	                                        .appearance-swatch-dot, .appearance-code-preview, .style-preview { forced-color-adjust: none; }
	                                        [role="radio"][aria-checked="true"], .version-option[aria-current="page"], .search-result.active {
	                                            outline: 2px solid Highlight;
	                                            outline-offset: -2px;
	                                        }
	                                        :is(.back-to-top, .toc-toggle-btn, .landing-btn, .component-step-number, .install-copy-btn, .copy-btn) { border: 1px solid ButtonText; }
	                                        .feedback-btn.selected { border: 3px solid Highlight; }
	                                        .search-result-snippet mark { background: Mark; color: MarkText; }
	                                        .code-block > pre > code > .line.highlighted { border-left: 3px solid Highlight; }
	                                        :is(.api-badge, .version-badge, .blog-tag) { border: 1px solid CanvasText; }
	                                        .site-banner { border-bottom-color: CanvasText; }
	                                        .site-banner-link { color: LinkText; }
	                                    }
	                                    """;
}
