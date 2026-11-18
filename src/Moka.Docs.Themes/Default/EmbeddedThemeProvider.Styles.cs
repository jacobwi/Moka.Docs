namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _stylesCss = """

	                                  /* =============================== */
	                                  /* Styles (html[data-style])       */
	                                  /* =============================== */

	                                  /* Style previews on the Appearance panel's style cards */
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
	                                  .style-preview-terminal { background: linear-gradient(90deg, #4ade80 0 3px, transparent 3px 5px, #4ade80 5px 12px, transparent 12px) 4px 6px / 18px 2px no-repeat, #0b0c0b; border-color: #3a403a; border-radius: 0; }
	                                  .style-preview-paper { background: repeating-linear-gradient(#fbf8f1 0 3px, #d8ccb4 3px 4px) 4px 4px / 16px 10px no-repeat, #fbf8f1; border-color: #d8ccb4; border-radius: 1px; }
	                                  .style-preview-contrast { background: linear-gradient(90deg, #000000 50%, #ffffff 50%); border: 2px solid #000000; }
	                                  .style-preview-glass { background: radial-gradient(circle at 25% 20%, color-mix(in srgb, var(--color-primary) 55%, transparent), transparent 70%), linear-gradient(135deg, #e0e7ff, #f5f3ff); border-color: rgba(255, 255, 255, 0.9); border-radius: 6px; box-shadow: 0 1px 4px rgba(15, 23, 42, 0.2); }
	                                  .style-preview-brutal { background: color-mix(in srgb, var(--color-primary) 30%, #ffffff); border: 2px solid #0a0a0a; border-radius: 0; box-shadow: 2px 2px 0 #0a0a0a; }
	                                  .style-preview-blueprint { background: linear-gradient(#b8cbe3 1px, transparent 1px) 0 0 / 5px 5px, linear-gradient(90deg, #b8cbe3 1px, transparent 1px) 0 0 / 5px 5px, #eef4fb; border-color: #0b2f5b; border-radius: 0; }
	                                  .style-preview-newspaper { background: linear-gradient(#1a1814 0 1px, transparent 1px 3px, #1a1814 3px 4px) 0 4px / 100% 4px no-repeat, repeating-linear-gradient(#9a8f78 0 1px, transparent 1px 3px) 3px 10px / 18px 6px no-repeat, #f6f2e8; border-color: #9a8f78; border-radius: 0; }
	                                  .style-preview-retro { background: linear-gradient(90deg, #0a246a, #3a6ea5) 0 0 / 100% 5px no-repeat, #d4d0c8; border: 2px solid; border-color: #ffffff #404040 #404040 #ffffff; border-radius: 0; }
	                                  .style-preview-neon { background: linear-gradient(90deg, #3ee6ff 0 8px, transparent 8px) 4px 4px / 16px 2px no-repeat, linear-gradient(transparent 11px, #ff4fa3 11px 12px, transparent 12px), #120b1f; border-color: #3d2a66; border-radius: 2px; }
	                                  .style-preview-tonal { background: linear-gradient(color-mix(in srgb, var(--color-primary) 26%, #ffffff), color-mix(in srgb, var(--color-primary) 26%, #ffffff)) 4px 6px / 16px 6px no-repeat, #f3edf7; border-color: transparent; border-radius: 9px; }
	                                  .style-preview-editor { background: linear-gradient(transparent 14px, color-mix(in srgb, var(--color-primary) 56%, #000000) 14px), linear-gradient(90deg, #f3f3f3 0 7px, #e5e5e5 7px 8px, #ffffff 8px); border-color: #cbd5e1; border-radius: 0; }

	                                  /* Styles: shared tokens. Accent text and fills are mixed toward black (light) or white
	                                     (dark) so every color preset keeps 4.5:1 against the style's surfaces. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) {
	                                      --st-ink: #000000;
	                                      --st-hue: 56%;
	                                      --st-link: color-mix(in srgb, var(--color-primary) var(--st-hue), var(--st-ink));
	                                      --st-on-fill: #ffffff;
	                                  }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])[data-theme="dark"] {
	                                      --st-ink: #ffffff;
	                                      --st-hue: 70%;
	                                      --st-on-fill: #0b0b0f;
	                                      color-scheme: dark;
	                                  }
	                                  /* Vivid accent strength (Appearance panel): the accent gets back the chroma its mix with ink took, at the same OKLCH
	                                     lightness, so text keeps 4.5:1. A color past the edge of sRGB is clipped; the accessibility tests check clipped colors. */
	                                  @supports (color: oklch(from red l c h)) {
	                                      html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])[data-accent-strength="vivid"] { --st-link: oklch(from color-mix(in srgb, var(--color-primary) var(--st-hue), var(--st-ink)) l calc(c * 1.35) h); }
	                                  }
	                                  :where(html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])) a,
	                                  :where(html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])) a:hover { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content a:not([class]):hover { text-decoration: underline; text-underline-offset: 0.18em; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :focus-visible { outline: 2px solid var(--st-link); outline-offset: 2px; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content :not(pre) > code,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .nav-link.current,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .nav-header .nav-link:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .nav-group-label.parent-active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-member-link,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-nav-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .search-result-section,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .version-option.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .version-badge-default,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-logo-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .component-card-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-feature-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-hero-icon,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-footer-heart,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing .landing-content > ul > li strong,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .blazor-preview-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .blazor-preview-error { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .breadcrumbs a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .edit-link a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .toc-back-to-top:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .heading-anchor:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .footer-social a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-footer a:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .source-viewer summary:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .install-nuget-link:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-logo:hover { color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .toc-item a.active { color: var(--st-link) !important; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .tab-header.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .install-tab.active,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .blazor-preview-tab.active { color: var(--st-link); border-bottom-color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .appearance-trigger[aria-expanded="true"],
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .version-trigger[aria-expanded="true"] { color: var(--st-link); border-color: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .appearance { --appearance-accent: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content h1,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-hero-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing .landing-content > h1:first-of-type {
	                                      background: none;
	                                      -webkit-text-fill-color: currentColor;
	                                      color: var(--color-text);
	                                  }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content h1 code { -webkit-text-fill-color: currentColor; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .component-step-number,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .toc-toggle-btn,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .back-to-top,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-btn-primary,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing-btn-primary:hover { background: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .back-to-top:hover { background: var(--st-link) !important; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .install-copy-btn:hover { background: var(--st-link); border-color: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing .landing-content > h1:first-of-type + p + p a:first-child,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .landing .landing-content > h1:first-of-type + p + ul a:first-of-type { background: var(--st-link); color: var(--st-on-fill); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-endpoint-card h2.openapi-endpoint-header { margin: 0 0 0.75rem; padding: 0; border: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content .table-responsive table { margin: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-endpoint-card > :last-child { margin-bottom: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .breadcrumbs ol { padding-left: 0; margin-bottom: 0; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .search-input:focus-visible { outline: none; box-shadow: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .search-input-wrap:focus-within { box-shadow: inset 0 -1px 0 var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .search-result-snippet mark { color: var(--color-text); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) ::selection { background: color-mix(in srgb, var(--color-primary) 28%, transparent); }

	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .skip-link { border-color: var(--st-link); }
	                                  /* A code block keeps its theme's background in both modes, so its focus ring takes the code's own text color. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .code-block > pre > code:focus-visible { outline: 2px solid currentColor; outline-offset: -2px; box-shadow: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content pre .copy-btn:focus-visible { outline: 2px solid currentColor; outline-offset: 2px; box-shadow: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .lightbox-close:focus-visible { outline: 2px solid #ffffff; outline-offset: 2px; box-shadow: none; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])[data-navbar="tabs"] .header-tab:focus-visible { outline-color: var(--st-link); }
	                                  /* Graphite is so dark that the usual 70% of it falls under 4.5:1 on the tinted dark surfaces. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"])[data-theme="dark"][data-color-theme="graphite"] { --st-hue: 52%; }

	                                  /* Styles: semantic colors (badges, admonitions, REST methods), mixed the same way */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge-sm { --st-c: var(--color-text-secondary); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-class, .admonition-note, .openapi-method-post, .openapi-status-info) { --st-c: #3b82f6; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-struct, .admonition-tip, .openapi-method-get, .openapi-status-success, .feedback-btn-yes, .repl-run-btn) { --st-c: #22c55e; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-record, .admonition-info, .admonition-important, .openapi-method-patch) { --st-c: #8b5cf6; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-interface, .api-badge-virtual) { --st-c: #06b6d4; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-enum, .admonition-warning, .admonition-caution, .openapi-method-put, .openapi-status-redirect, .openapi-auth-label, .version-badge-pre) { --st-c: #f59e0b; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge-delegate { --st-c: #ec4899; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-static, .openapi-method-head, .openapi-method-options, .openapi-optional) { --st-c: #64748b; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge-abstract { --st-c: #a78bfa; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge-sealed { --st-c: #f97316; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :is(.api-badge-obsolete, .admonition-danger, .openapi-method-delete, .openapi-status-client-error, .openapi-status-server-error, .openapi-required, .feedback-btn-no) { --st-c: #ef4444; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .api-badge-sm,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .page-content .admonition-title,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-status,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-required,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-optional,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-auth-label,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .version-badge-pre { color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink)); }
	                                  /* The changelog plugin follows the operating system's color scheme; within a style it follows the page. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) {
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
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .changelog-badge-major { background: color-mix(in srgb, #ef4444 12%, var(--color-bg)); border-color: color-mix(in srgb, #ef4444 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .changelog-badge-minor { background: color-mix(in srgb, #3b82f6 12%, var(--color-bg)); border-color: color-mix(in srgb, #3b82f6 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .changelog-badge-patch { background: color-mix(in srgb, #22c55e 12%, var(--color-bg)); border-color: color-mix(in srgb, #22c55e 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .changelog-badge-initial { background: color-mix(in srgb, #8b5cf6 12%, var(--color-bg)); border-color: color-mix(in srgb, #8b5cf6 30%, transparent); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .openapi-method,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .repl-run-btn { background: color-mix(in srgb, var(--st-c) 58%, #000000); color: #ffffff; }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .repl-run-btn:hover { background: color-mix(in srgb, var(--st-c) 48%, #000000); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .feedback-btn-yes:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .feedback-btn-yes.selected,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .feedback-btn-no:hover,
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .feedback-btn-no.selected {
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
	                                      --dark-bg: #060608;
	                                      --dark-bg-secondary: #0c0c10;
	                                      --dark-bg-code: #101015;
	                                      --color-text: #e8e8ec;
	                                      --color-text-secondary: #a0a0aa;
	                                      --color-text-muted: #8a8a94;
	                                      --mk-surface: var(--color-bg-secondary);
	                                      --mk-hover: color-mix(in oklab, var(--color-text) 5%, var(--color-bg-secondary));
	                                      --mk-surface-3: color-mix(in oklab, var(--color-text) 9%, var(--color-bg-secondary));
	                                      --mk-border: color-mix(in srgb, var(--color-primary) 6%, transparent);
	                                      --mk-border-visible: color-mix(in srgb, var(--color-primary) 12%, transparent);
	                                      --mk-border-strong: color-mix(in srgb, var(--color-primary) 24%, transparent);
	                                      --mk-backdrop: color-mix(in srgb, var(--color-bg) 85%, transparent);
	                                  }
	                                  html[data-style="moka"] body { font-size: 0.875rem; line-height: 1.6; }
	                                  html[data-style="moka"] :focus-visible {
	                                      outline: none;
	                                      box-shadow: 0 0 0 1px var(--st-link), 0 0 0 4px var(--mk-glow-md), inset 0 0 12px var(--mk-glow);
	                                  }
	                                  html[data-style="moka"] ::selection { background: var(--mk-glow-strong); }
	                                  html[data-style="moka"] :is(.sidebar, .toc-sidebar, .search-results, .version-dropdown, pre, pre > code, .page-content table) { scrollbar-width: thin; scrollbar-color: var(--mk-surface-3) transparent; }
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
	                                      font-size: 0.6875rem;
	                                      padding: 0.375rem 0.625rem;
	                                      transition: border-color var(--transition), box-shadow var(--transition);
	                                  }
	                                  html[data-style="moka"] .search-trigger:hover { border-color: var(--mk-border-strong); box-shadow: inset 0 0 12px var(--mk-glow); }
	                                  html[data-style="moka"] .search-trigger kbd { background: transparent; border-color: var(--mk-border-visible); font-size: 0.625rem; }
	                                  html[data-style="moka"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger) {
	                                      width: 32px;
	                                      height: 32px;
	                                      border-color: var(--mk-border-visible);
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="moka"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger):hover {
	                                      background: var(--mk-hover);
	                                      border-color: var(--mk-border-strong);
	                                      color: var(--color-text);
	                                  }
	                                  html[data-style="moka"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] { box-shadow: inset 0 0 12px var(--mk-glow-md); }
	                                  html[data-style="moka"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"]:focus-visible { box-shadow: 0 0 0 1px var(--st-link), 0 0 0 4px var(--mk-glow-md), inset 0 0 12px var(--mk-glow-md); }
	                                  html[data-style="moka"] :is(.appearance-panel, .version-dropdown) {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-strong);
	                                      border-radius: var(--radius-md);
	                                      box-shadow: none;
	                                  }
	                                  html[data-style="moka"] :is(.appearance-option, .version-option) { border-radius: calc(6px * var(--radius-scale)); font-size: 0.8125rem; }
	                                  html[data-style="moka"] :is(.appearance-option, .version-option):hover { background: var(--mk-hover); color: var(--color-text); }
	                                  html[data-style="moka"] :is(.appearance-option[aria-checked="true"], .version-option.active) { box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .appearance-panel { font-size: 0.8125rem; }
	                                  html[data-style="moka"] .appearance-title { font-size: 0.875rem; font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="moka"] :is(.appearance-label, .appearance-group-label) { font-size: 0.625rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .appearance-section { border-top-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] :is(.appearance-option, .appearance-reset) { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] :is(.appearance-segments .appearance-option, .appearance-reset) { font-size: 0.6875rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] [data-appearance="text-size"] .appearance-option { font-family: var(--font-mono); letter-spacing: 0.02em; }
	                                  html[data-style="moka"] :is(.appearance-option, .appearance-reset):hover { border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .appearance-option[aria-checked="true"] { border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .appearance-option[aria-checked="true"]:focus-visible { box-shadow: 0 0 0 1px var(--st-link), 0 0 0 4px var(--mk-glow-md), inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .appearance-reset:hover { background: var(--mk-glow); color: var(--st-link); }
	                                  html[data-style="moka"] .appearance-close:hover { background: var(--mk-hover); }
	                                  html[data-style="moka"] .version-trigger {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      font-family: var(--font-mono);
	                                      font-size: 0.6875rem;
	                                      letter-spacing: 0.02em;
	                                  }
	                                  html[data-style="moka"] .version-trigger:hover { border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .version-option { font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="moka"] .version-badge {
	                                      border-radius: var(--radius-sm);
	                                      font-family: var(--font-body);
	                                      font-size: 0.5625rem;
	                                      letter-spacing: 0.08em;
	                                      border: 1px solid color-mix(in srgb, currentColor 30%, transparent);
	                                  }
	                                  html[data-style="moka"] .sidebar { padding: 1.25rem 0.75rem; border-right-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .nav-section { margin-bottom: 0.375rem; }
	                                  html[data-style="moka"] .nav-link {
	                                      padding: 0.3125rem 0.625rem !important;
	                                      border: 1px solid transparent !important;
	                                      border-radius: calc(6px * var(--radius-scale));
	                                      font-size: 0.8125rem;
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
	                                      font-size: 0.625rem;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                      color: var(--color-text-muted);
	                                  }
	                                  html[data-style="moka"] .nav-section > .nav-header :is(.nav-link.parent-active, .nav-group-label.parent-active) { color: var(--color-text); }
	                                  html[data-style="moka"] .nav-section > .nav-header .nav-link.current { color: var(--st-link); }
	                                  html[data-style="moka"] .nav-sub-section > .nav-header .nav-link { font-size: 0.8125rem; font-weight: 600; color: var(--color-text); }
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
	                                  html[data-style="moka"] .page-content p { line-height: calc(1.65 * var(--line-spacing)); margin-bottom: calc(0.875rem * var(--paragraph-spacing)); }
	                                  html[data-style="moka"] .page-content li { line-height: calc(1.6 * var(--line-spacing)); }
	                                  html[data-style="moka"] .page-content :not(pre) > code {
	                                      background: var(--mk-glow);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-sm) !important;
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="moka"] .page-content pre { border-color: var(--mk-border-visible); border-radius: var(--radius); }
	                                  html[data-style="moka"][data-theme="dark"]:not([data-code-theme="catppuccin-latte"], [data-code-theme="github-light"], [data-code-theme="gruvbox-light"], [data-code-theme="solarized-light"], [data-code-theme="rose-pine-dawn"]) :is(.page-content, .landing-content) pre { background: #0a0a0d; }
	                                  html[data-style="moka"] .page-content pre .line-numbers { border-right-color: var(--mk-border-visible) !important; }
	                                  html[data-style="moka"] .page-content pre .code-lang { letter-spacing: 0.08em !important; font-weight: 600; }
	                                  html[data-style="moka"] .page-content blockquote {
	                                      background: var(--mk-surface);
	                                      border-left: 2px solid var(--st-link);
	                                      border-radius: 0 var(--radius) var(--radius) 0;
	                                      box-shadow: inset 0 0 20px var(--mk-glow);
	                                  }
	                                  html[data-style="moka"] .page-content table { font-size: 0.8125rem; }
	                                  html[data-style="moka"] .page-content :is(th, td) { padding: 0.4375rem 0.75rem; border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .page-content th {
	                                      background: var(--mk-surface);
	                                      color: var(--color-text-muted);
	                                      font-size: 0.625rem;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .page-content tr:hover td { background: var(--mk-hover); }
	                                  html[data-style="moka"] .api-member-table code { font-size: 0.75rem; }
	                                  html[data-style="moka"] .api-namespace { font-family: var(--font-mono); font-size: 0.6875rem; letter-spacing: 0.02em; }
	                                  html[data-style="moka"] :is(.api-badge, .api-badge-sm) {
	                                      border: 1px solid color-mix(in srgb, currentColor 30%, transparent);
	                                      border-radius: var(--radius-sm);
	                                      font-size: 0.625rem;
	                                      letter-spacing: 0.08em;
	                                  }
	                                  html[data-style="moka"] .api-badge-sm { font-size: 0.5625rem; }
	                                  html[data-style="moka"] .page-content .admonition {
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 24%, transparent);
	                                      border-left: 3px solid var(--st-c);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 6%, var(--color-bg));
	                                      padding: 0.875rem 1rem;
	                                  }
	                                  html[data-style="moka"] .page-content .admonition-title { font-size: 0.6875rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .tabs { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .tab-headers { background: var(--mk-surface); border-bottom-color: var(--mk-border-visible); border-radius: var(--radius-md) var(--radius-md) 0 0; }
	                                  html[data-style="moka"] .tab-header { font-size: 0.6875rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; }
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
	                                  html[data-style="moka"] .component-card-title { font-size: 0.6875rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-secondary); }
	                                  html[data-style="moka"] .component-step-number {
	                                      width: 28px;
	                                      height: 28px;
	                                      border-radius: var(--radius);
	                                      background: var(--mk-surface);
	                                      color: var(--st-link);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: inset 0 0 12px var(--mk-glow);
	                                      font-family: var(--font-mono);
	                                      font-size: 0.75rem;
	                                  }
	                                  html[data-style="moka"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--mk-border-visible); }
	                                  html[data-style="moka"] .package-install-widget { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] :is(.install-header, .install-tabs) { background: var(--mk-surface); border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .install-package-name { font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="moka"] .install-tab { font-size: 0.6875rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="moka"] .install-copy-btn { background: transparent; border-color: var(--mk-border-visible); font-size: 0.6875rem; letter-spacing: 0.08em; text-transform: uppercase; font-weight: 600; }
	                                  html[data-style="moka"] .install-copy-btn:hover { background: var(--mk-glow); border-color: var(--mk-border-strong); color: var(--st-link); }
	                                  html[data-style="moka"] .source-viewer { border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .source-viewer summary { font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="moka"] .source-viewer[open] summary { border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-sidebar { padding: 1.25rem 0; border-left-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-title { font-size: 0.625rem; letter-spacing: 0.08em; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .toc-list { border-left: 1px solid var(--mk-border-visible); }
	                                  html[data-style="moka"] .toc-item a {
	                                      font-size: 0.75rem;
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
	                                  html[data-style="moka"] :is(.toc-counter, .toc-back-to-top) { font-family: var(--font-mono); font-size: 0.6875rem; }
	                                  html[data-style="moka"] .toc-counter { border-top-color: var(--mk-border); color: var(--color-text-muted); }
	                                  html[data-style="moka"] .breadcrumbs { font-family: var(--font-mono); font-size: 0.6875rem; letter-spacing: 0.02em; }
	                                  html[data-style="moka"] .breadcrumbs li:not(:last-child)::after { color: var(--mk-border-strong); opacity: 1; }
	                                  html[data-style="moka"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--mk-border-visible); font-size: 0.6875rem; }
	                                  html[data-style="moka"] .last-updated { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .page-nav-link {
	                                      padding: 0.75rem 1rem;
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius-md);
	                                  }
	                                  html[data-style="moka"] .page-nav-link:hover { background: var(--mk-hover); border-color: var(--mk-border-strong); box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .page-nav-label { font-size: 0.625rem; font-weight: 600; letter-spacing: 0.08em; }
	                                  html[data-style="moka"] .page-nav-title { font-size: 0.8125rem; }
	                                  html[data-style="moka"] .feedback-prompt { font-size: 0.625rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .feedback-btn {
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius);
	                                      font-size: 0.6875rem;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .feedback-btn:hover { background: var(--mk-hover); }
	                                  html[data-style="moka"] .feedback-btn.selected { box-shadow: inset 0 0 20px color-mix(in srgb, var(--st-c) 12%, transparent); }
	                                  html[data-style="moka"] .site-footer { border-top-color: var(--mk-border-visible); padding: 1rem 1.5rem; }
	                                  html[data-style="moka"] .footer-inner { font-size: 0.6875rem; }
	                                  html[data-style="moka"] .built-with { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .search-backdrop { background: var(--mk-backdrop); backdrop-filter: blur(8px); }
	                                  html[data-style="moka"] .search-dialog {
	                                      background: var(--mk-surface);
	                                      border-color: var(--mk-border-strong);
	                                      border-radius: calc(12px * var(--radius-scale));
	                                      box-shadow: 0 0 0 1px var(--mk-border);
	                                  }
	                                  html[data-style="moka"] .search-input-wrap { border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .search-input { font-size: 0.875rem; }
	                                  html[data-style="moka"] .search-input::placeholder { font-family: var(--font-mono); font-size: 0.6875rem; }
	                                  html[data-style="moka"] :is(.search-esc, .search-result-category) { font-family: var(--font-mono); }
	                                  html[data-style="moka"] .search-result-category { font-size: 0.625rem; letter-spacing: 0.04em; text-transform: uppercase; color: var(--color-text-muted); }
	                                  html[data-style="moka"] .search-result { border: 1px solid transparent; border-radius: calc(6px * var(--radius-scale)); }
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
	                                      border-radius: calc(12px * var(--radius-scale));
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
	                                      font-size: 0.6875rem;
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
	                                  html[data-style="moka"] .landing-features-subtitle { font-size: 0.6875rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-muted); }
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
	                                  html[data-style="moka"] .landing-feature-name { font-size: 0.875rem; font-weight: 600; }
	                                  html[data-style="moka"] .landing-feature-desc { font-size: 0.8125rem; }
	                                  html[data-style="moka"] :is(.landing-divider, .landing .landing-content > hr) { border-top-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-md-content :is(th, td) { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-md-content table { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing .landing-content > ul > li { background: var(--mk-surface); border-color: var(--mk-border-visible); border-radius: var(--radius-md); }
	                                  html[data-style="moka"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: inset 0 0 20px var(--mk-glow) !important; border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .landing-content pre { border-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .landing-footer { border-top-color: var(--mk-border-visible); font-size: 0.6875rem; }
	                                  html[data-style="moka"] .repl-container { border-color: var(--mk-border-visible); border-radius: var(--radius-md); background: #0a0a0d; }
	                                  html[data-style="moka"] .repl-toolbar { background: #0c0c10; border-top-color: rgba(255, 255, 255, 0.08); }
	                                  html[data-style="moka"] .repl-run-btn {
	                                      background: transparent;
	                                      color: #4ade80;
	                                      border: 1px solid rgba(74, 222, 128, 0.35);
	                                      font-family: var(--font-mono);
	                                      font-size: 0.6875rem;
	                                      letter-spacing: 0.06em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .repl-run-btn:hover { background: rgba(74, 222, 128, 0.1); }
	                                  html[data-style="moka"] .repl-output { border-top-color: rgba(255, 255, 255, 0.08); background: #060608; }
	                                  html[data-style="moka"] .openapi-method {
	                                      background: color-mix(in srgb, var(--st-c) 12%, var(--color-bg));
	                                      color: color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink));
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 35%, transparent);
	                                      font-size: 0.6875rem;
	                                      letter-spacing: 0.06em;
	                                  }
	                                  html[data-style="moka"] .openapi-endpoint-card { border-color: var(--mk-border-visible); border-radius: var(--radius-md); background: var(--mk-surface); }
	                                  html[data-style="moka"] .openapi-endpoint-card h3 { border-bottom-color: var(--mk-border-visible); font-size: 0.6875rem !important; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-secondary); }
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
	                                      --dark-bg: #111113;
	                                      --dark-bg-secondary: #19191c;
	                                      --dark-bg-code: #19191c;
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
	                                  html[data-style="minimal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: transparent; }
	                                  html[data-style="minimal"] .appearance-section { border-top-color: var(--color-border-light); }
	                                  html[data-style="minimal"] :is(.appearance-option, .appearance-reset) { border-color: transparent; }
	                                  html[data-style="minimal"] .appearance-option[aria-checked="true"] { border-color: var(--appearance-accent); }
	                                  html[data-style="minimal"] .appearance-reset:hover { background: none; color: var(--color-text); text-decoration: underline; text-underline-offset: 0.18em; }
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
	                                  html[data-style="minimal"] .page-content p { line-height: calc(1.8 * var(--line-spacing)); margin-bottom: calc(1.25rem * var(--paragraph-spacing)); }
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
	                                  html[data-style="minimal"] .search-dialog { border-radius: calc(10px * var(--radius-scale)); }
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
	                                      --dark-bg: #0d1117;
	                                      --dark-bg-secondary: #161b22;
	                                      --dark-bg-code: #161b22;
	                                      --color-text: #e6edf3;
	                                      --color-text-secondary: #b1bac4;
	                                      --color-text-muted: #9198a1;
	                                      --color-border: #30363d;
	                                      --color-border-light: #21262d;
	                                      --cs-hover: color-mix(in oklab, var(--color-text) 6%, var(--color-bg-secondary));
	                                      --cs-zebra: color-mix(in oklab, var(--color-bg-secondary) 61%, var(--color-bg));
	                                      --cs-bar: color-mix(in oklab, #000000 36%, var(--color-bg));
	                                      --cs-on-bar: var(--color-text);
	                                      --cs-on-bar-muted: var(--color-text-secondary);
	                                      --cs-bar-line: var(--color-border);
	                                      --cs-bar-control: var(--color-bg-secondary);
	                                      --cs-bar-control-hover: var(--cs-hover);
	                                      --cs-bar-control-border: var(--color-border);
	                                      --shadow-sm: 0 1px 2px rgba(1, 4, 9, 0.6);
	                                      --shadow-md: 0 3px 8px rgba(1, 4, 9, 0.7);
	                                      --shadow-lg: 0 8px 24px rgba(1, 4, 9, 0.8);
	                                  }
	                                  html[data-style="classic"] body { font-size: 0.9375rem; line-height: 1.6; }
	                                  html[data-style="classic"] .site-header {
	                                      background: var(--cs-bar);
	                                      border-bottom: 1px solid var(--cs-bar-line);
	                                      box-shadow: inset 0 3px 0 var(--st-link);
	                                      backdrop-filter: none;
	                                  }
	                                  html[data-style="classic"]:not([data-theme="dark"]) .site-header { box-shadow: none; }
	                                  html[data-style="classic"] .site-header :is(.site-logo, .site-logo:hover, .site-logo-icon) { color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-name { font-weight: 600; }
	                                  html[data-style="classic"] .site-header :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) {
	                                      color: var(--cs-on-bar);
	                                      background: transparent;
	                                      border-color: var(--cs-bar-control-border);
	                                  }
	                                  html[data-style="classic"] .site-header :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):hover { background: var(--cs-bar-control-hover); color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-header :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] {
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
	                                  html[data-style="classic"] :is(.appearance-panel, .version-dropdown) { box-shadow: var(--shadow-md); }
	                                  html[data-style="classic"] :is(.appearance-option, .version-option):hover { background: var(--cs-hover); }
	                                  html[data-style="classic"] :is(.appearance-reset, .appearance-close):hover { background: var(--cs-hover); }
	                                  html[data-style="classic"] .appearance-label { letter-spacing: 0.02em; }
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
	                                  html[data-style="classic"] .page-content p { line-height: calc(1.65 * var(--line-spacing)); margin-bottom: calc(1rem * var(--paragraph-spacing)); }
	                                  html[data-style="classic"] .page-content li { line-height: calc(1.6 * var(--line-spacing)); }
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
	                                  html[data-style="classic"] .breadcrumbs li:not(:last-child)::after { content: '\203A'; content: '\203A' / ''; opacity: 1; color: var(--color-text-muted); }
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
	                                      --dark-bg: #12131a;
	                                      --dark-bg-secondary: color-mix(in srgb, var(--color-primary) 8%, #191b24);
	                                      --dark-bg-code: color-mix(in srgb, var(--color-primary) 5%, #171922);
	                                      --color-text: #e9eaf2;
	                                      --color-text-secondary: #b3b6c8;
	                                      --color-text-muted: #9ca0b5;
	                                      --color-border: color-mix(in srgb, var(--color-primary) 12%, #2a2d3a);
	                                      --color-border-light: color-mix(in srgb, var(--color-primary) 8%, #20222c);
	                                      --sf-surface: color-mix(in oklab, var(--color-text) 4%, var(--color-bg));
	                                      --sf-tint: color-mix(in srgb, var(--color-primary) 12%, var(--color-bg));
	                                      --sf-tint-strong: color-mix(in srgb, var(--color-primary) 20%, var(--color-bg));
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
	                                  html[data-style="soft"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger) {
	                                      border-color: transparent;
	                                      border-radius: calc(999px * var(--radius-scale));
	                                      background: var(--sf-tint);
	                                  }
	                                  html[data-style="soft"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger):hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .search-trigger { border-color: transparent; border-radius: calc(999px * var(--radius-scale)); background: var(--sf-tint); padding: 0.4rem 0.9rem; }
	                                  html[data-style="soft"] .search-trigger:hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .search-trigger kbd { border-color: transparent; border-radius: calc(999px * var(--radius-scale)); background: var(--sf-surface); }
	                                  html[data-style="soft"] .version-trigger { border-radius: calc(999px * var(--radius-scale)); border-color: transparent; background: var(--sf-tint); }
	                                  html[data-style="soft"] :is(.appearance-panel, .version-dropdown) { border-color: var(--color-border-light); background: var(--sf-surface); padding: 8px; }
	                                  html[data-style="soft"] :is(.appearance-option, .version-option) { border-radius: calc(8px * var(--radius-scale)); }
	                                  html[data-style="soft"] :is(.appearance-option, .version-option):hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .appearance-panel { padding: 0.875rem 1rem 1rem; box-shadow: var(--shadow-lg); }
	                                  html[data-style="soft"] .appearance-section { border-top-color: var(--color-border-light); }
	                                  html[data-style="soft"] :is(.appearance-option, .appearance-reset) { border-color: transparent; background: var(--sf-tint); }
	                                  html[data-style="soft"] :is(.appearance-segments .appearance-option, .appearance-reset, .appearance-close) { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="soft"] :is(.appearance-option, .appearance-reset):hover { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .appearance-option[aria-checked="true"] { border-color: var(--appearance-accent); background: var(--sf-surface); }
	                                  html[data-style="soft"] .appearance-close:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .version-badge-default { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .sidebar { border-right: none; padding: 1.5rem 1rem; }
	                                  html[data-style="soft"] .nav-section { margin-bottom: 0.5rem; }
	                                  html[data-style="soft"] .nav-link {
	                                      padding: 0.4rem 0.875rem !important;
	                                      border-left: 0 !important;
	                                      border-radius: calc(999px * var(--radius-scale));
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="soft"] .nav-link:hover { background: var(--sf-tint); color: var(--color-text); }
	                                  html[data-style="soft"] .nav-link.current { background: var(--sf-tint-strong); font-weight: 600; }
	                                  html[data-style="soft"] .nav-header .nav-link,
	                                  html[data-style="soft"] .nav-group-label { font-weight: 700; color: var(--color-text); }
	                                  html[data-style="soft"] .nav-chevron-btn { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="soft"] .nav-chevron-btn:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .nav-children { padding-left: 0.75rem; }
	                                  html[data-style="soft"] .content { padding: 2.5rem 3rem; }
	                                  html[data-style="soft"] .page-content h1 { margin-bottom: 1.5rem !important; }
	                                  html[data-style="soft"] .page-content h2 { border-bottom: none; padding-bottom: 0; margin: 3rem 0 1rem; }
	                                  html[data-style="soft"] .page-content h3 { margin-top: 2.25rem; }
	                                  html[data-style="soft"] .page-content p { line-height: calc(1.85 * var(--line-spacing)); margin-bottom: calc(1.25rem * var(--paragraph-spacing)); }
	                                  html[data-style="soft"] .page-content :not(pre) > code { background: var(--sf-tint); border-color: transparent; border-radius: calc(6px * var(--radius-scale)) !important; }
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
	                                  html[data-style="soft"] :is(.tab-header, .install-tab) { border-radius: calc(999px * var(--radius-scale)); border-bottom: none; margin-bottom: 0; padding: 0.375rem 0.875rem; }
	                                  html[data-style="soft"] :is(.tab-header.active, .install-tab.active) { background: var(--sf-surface); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .install-header { background: var(--sf-surface); border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] .install-copy-btn { border-radius: calc(999px * var(--radius-scale)); }
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
	                                  html[data-style="soft"] .toc-item a { border-left: 0 !important; border-radius: calc(999px * var(--radius-scale)); margin-left: 0; padding: 0.3rem 0.75rem; }
	                                  html[data-style="soft"] .toc-level-3 a { margin-left: 0.75rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-level-4 a { margin-left: 1.5rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-item a:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .toc-item a.active { background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .toc-progress { height: 4px; border-radius: calc(999px * var(--radius-scale)); background: var(--sf-tint); }
	                                  html[data-style="soft"] .toc-progress-bar { border-radius: calc(999px * var(--radius-scale)); background: var(--st-link); }
	                                  html[data-style="soft"] .toc-counter { border-top: none; }
	                                  html[data-style="soft"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--color-border-light); }
	                                  html[data-style="soft"] .page-nav-link { border-color: transparent; border-radius: var(--radius); background: var(--sf-surface); box-shadow: var(--shadow-sm); padding: 1.125rem 1.375rem; }
	                                  html[data-style="soft"] .page-nav-link:hover { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .feedback-btn { border-radius: calc(999px * var(--radius-scale)); padding: 0.45rem 1rem; }
	                                  html[data-style="soft"] .site-footer { background: var(--color-bg-secondary); border-top: none; }
	                                  html[data-style="soft"] .search-dialog { border-color: transparent; border-radius: calc(16px * var(--radius-scale)); background: var(--sf-surface); }
	                                  html[data-style="soft"] .search-result { border-radius: calc(10px * var(--radius-scale)); }
	                                  html[data-style="soft"] :is(.search-result:hover, .search-result.active) { background: var(--sf-tint); }
	                                  html[data-style="soft"] :is(.back-to-top, .toc-toggle-btn) { box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .back-to-top:hover { box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="soft"] .landing-hero::before { animation: none; background: radial-gradient(ellipse 70% 60% at 50% 0%, var(--sf-tint-strong), transparent 70%) !important; }
	                                  html[data-style="soft"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="soft"] .landing-hero-icon { border-radius: calc(20px * var(--radius-scale)); background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-hero-title {
	                                      background: linear-gradient(135deg, var(--color-text) 35%, var(--st-link));
	                                      -webkit-background-clip: text;
	                                      background-clip: text;
	                                      -webkit-text-fill-color: transparent;
	                                  }
	                                  html[data-style="soft"] .landing-btn { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="soft"] .landing-btn-primary { box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-btn-primary:hover { box-shadow: var(--shadow-lg); }
	                                  html[data-style="soft"] .landing-btn-secondary { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .landing-btn-secondary:hover { border-color: transparent; color: var(--st-link); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-feature-card { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing-feature-card::before { display: none; }
	                                  html[data-style="soft"] .landing-feature-card:hover { box-shadow: var(--shadow-lg) !important; transform: translateY(-3px) !important; }
	                                  html[data-style="soft"] .landing-feature-icon { border-radius: calc(12px * var(--radius-scale)); background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .landing .landing-content > ul > li { border-color: transparent; background: var(--sf-surface); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .landing .landing-content > ul > li:hover { box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="soft"] .landing-md-content table { border-color: var(--color-border); border-collapse: separate; border-spacing: 0; }
	                                  html[data-style="soft"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="soft"] .landing-md-content th { background: var(--sf-tint); }
	                                  html[data-style="soft"] .landing-footer { background: var(--color-bg-secondary); border-top: none; }
	                                  html[data-style="soft"] :is(.repl-container, .blazor-preview-container) { border-color: transparent; border-radius: var(--radius); box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .openapi-endpoint-card { border-color: transparent; border-radius: var(--radius); box-shadow: var(--shadow-md); background: var(--sf-surface); }
	                                  html[data-style="soft"] .openapi-endpoint-card h3 { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] :is(.openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: calc(999px * var(--radius-scale)); }
	                                  @media (max-width: 1200px) {
	                                      html[data-style="soft"] .toc-sidebar { border-left: none; border-radius: var(--radius) 0 0 var(--radius); box-shadow: var(--shadow-lg); }
	                                  }

	                                  /* Styles: skip link, page actions, search dialog, table of contents levels and carets, code block chrome */
	                                  html[data-style="moka"] .skip-link {
	                                      background: var(--mk-surface);
	                                      border: 1px solid var(--mk-border-strong);
	                                      box-shadow: inset 0 0 12px var(--mk-glow);
	                                      color: var(--st-link);
	                                      font-size: 0.6875rem;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .page-actions .page-action {
	                                      border-color: var(--mk-border-visible);
	                                      border-radius: var(--radius);
	                                      font-size: 0.6875rem;
	                                      font-weight: 600;
	                                      letter-spacing: 0.08em;
	                                      text-transform: uppercase;
	                                  }
	                                  html[data-style="moka"] .page-actions .page-action:hover { border-color: var(--mk-border-strong); background: var(--mk-glow); color: var(--st-link); }
	                                  html[data-style="moka"] .toc-caret:hover { background: var(--mk-hover); }
	                                  html[data-style="moka"] :is(.search-message, .search-close) { font-family: var(--font-mono); font-size: 0.6875rem; }
	                                  html[data-style="moka"] .search-close:hover { color: var(--st-link); }
	                                  html[data-style="moka"] .code-block > .code-title { letter-spacing: 0.02em; }
	                                  html[data-style="minimal"] .page-actions .page-action { border-color: transparent; padding-left: 0.375rem; padding-right: 0.375rem; color: var(--color-text-muted); }
	                                  html[data-style="minimal"] .page-actions .page-action:hover { border-color: transparent; color: var(--color-text); text-decoration: underline; text-underline-offset: 0.18em; }
	                                  html[data-style="minimal"] .toc-caret:hover { background: none; color: var(--color-text); }
	                                  html[data-style="minimal"] .skip-link { border-width: 1px; }
	                                  html[data-style="minimal"] .search-input-wrap { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="classic"] .page-actions .page-action { border-radius: var(--radius); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="classic"] .page-actions .page-action:hover { background: var(--cs-hover); border-color: var(--color-text-secondary); }
	                                  html[data-style="classic"] .skip-link { border-radius: var(--radius); box-shadow: var(--shadow-md); }
	                                  html[data-style="classic"] .toc-caret:hover { background: var(--cs-hover); }
	                                  html[data-style="classic"] .search-input-wrap { background: var(--color-bg-secondary); }
	                                  /* The light header bar is the accent itself, so a focus ring in the accent vanished on it. */
	                                  html[data-style="classic"] .site-header :focus-visible,
	                                  html[data-style="classic"][data-navbar="tabs"] .header-tab:focus-visible { outline-color: var(--cs-on-bar); }
	                                  html[data-style="classic"] .site-header .appearance-panel :focus-visible { outline-color: var(--st-link); }
	                                  html[data-style="soft"] .page-actions .page-action { border-color: transparent; border-radius: calc(999px * var(--radius-scale)); background: var(--sf-tint); }
	                                  html[data-style="soft"] .page-actions .page-action:hover { border-color: transparent; background: var(--sf-tint-strong); }
	                                  html[data-style="soft"] .skip-link { border: none; border-radius: calc(999px * var(--radius-scale)); box-shadow: 0 0 0 2px var(--st-link), var(--shadow-lg); }
	                                  html[data-style="soft"] .toc-level-5 a { margin-left: 2.25rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-level-6 a { margin-left: 3rem; padding-left: 0.75rem; }
	                                  html[data-style="soft"] .toc-caret { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="soft"] .toc-caret:hover { background: var(--sf-tint); }
	                                  html[data-style="soft"] .search-input-wrap { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="soft"] .search-close { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="soft"] .search-close:hover { background: var(--sf-tint); }

	                                  /* Style: Terminal */
	                                  html[data-style="terminal"] {
	                                      --color-bg: #fbfbf8;
	                                      --color-bg-secondary: #f1f1ec;
	                                      --color-bg-code: #efefe9;
	                                      --color-text: #1b1d1b;
	                                      --color-text-secondary: #3e433e;
	                                      --color-text-muted: #5a605a;
	                                      --color-border: #d3d6cf;
	                                      --color-border-light: #e5e7e1;
	                                      --tm-hover: #e8e9e3;
	                                      --tm-line: #9aa098;
	                                      --font-mono: 'JetBrains Mono', 'Cascadia Mono', 'Cascadia Code', 'SF Mono', Menlo, Consolas, 'Liberation Mono', monospace;
	                                      --font-body: var(--font-mono);
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: 0 0 0 1px var(--color-border);
	                                      --transition: 100ms linear;
	                                      --header-height: 48px;
	                                      --sidebar-width: 264px;
	                                      --toc-width: 224px;
	                                      --content-max-width: 820px;
	                                  }
	                                  html[data-style="terminal"][data-theme="dark"] {
	                                      --dark-bg: #0b0c0b;
	                                      --dark-bg-secondary: #121412;
	                                      --dark-bg-code: #0f110f;
	                                      --color-text: #d8ddd6;
	                                      --color-text-secondary: #aab2a8;
	                                      --color-text-muted: #8b948a;
	                                      --color-border: #272c27;
	                                      --color-border-light: #1a1d1a;
	                                      --tm-hover: color-mix(in oklab, var(--color-text) 5%, var(--color-bg-secondary));
	                                      --tm-line: #4a524a;
	                                  }
	                                  html[data-style="terminal"] body { font-size: 0.875rem; line-height: 1.65; }
	                                  html[data-style="terminal"] :focus-visible { outline: none; box-shadow: inset 0 -2px 0 var(--st-link); }
	                                  html[data-style="terminal"] img.zoomable:focus-visible { outline: 2px solid var(--st-link); outline-offset: 2px; box-shadow: none; }
	                                  html[data-style="terminal"] .site-header { background: var(--color-bg); border-bottom-color: var(--color-border); backdrop-filter: none; }
	                                  html[data-style="terminal"] .site-name { font-size: 0.875rem; font-weight: 700; letter-spacing: 0; }
	                                  html[data-style="terminal"] .site-name::before { content: '~/'; content: '~/' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .search-trigger { background: transparent; border-color: var(--color-border); font-size: 0.75rem; }
	                                  html[data-style="terminal"] .search-trigger kbd { background: transparent; border-color: var(--color-border); }
	                                  html[data-style="terminal"] { --header-button: 32px; }
	                                  html[data-style="terminal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { background: var(--tm-hover); border-color: var(--tm-line); color: var(--color-text); }
	                                  html[data-style="terminal"] :is(.appearance-panel, .version-dropdown) { border-color: var(--tm-line); }
	                                  html[data-style="terminal"] :is(.appearance-option, .version-option):hover { background: var(--tm-hover); }
	                                  html[data-style="terminal"] :is(.appearance-option[aria-checked="true"], .version-option.active) { background: var(--tm-hover); box-shadow: inset 2px 0 0 var(--st-link); }
	                                  html[data-style="terminal"] .appearance-label { letter-spacing: 0; text-transform: none; }
	                                  html[data-style="terminal"] .appearance-label::before { content: '# '; content: '# ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .appearance-option[aria-checked="true"]:focus-visible { box-shadow: inset 2px 0 0 var(--st-link), inset 0 -2px 0 var(--st-link); }
	                                  html[data-style="terminal"] :is(.appearance-reset, .appearance-close):hover { background: var(--tm-hover); color: var(--color-text); }
	                                  html[data-style="terminal"] .version-trigger { background: transparent; font-size: 0.75rem; }
	                                  html[data-style="terminal"] .sidebar { padding: 1rem 0.75rem; }
	                                  html[data-style="terminal"] .nav-section { margin-bottom: 0.25rem; }
	                                  html[data-style="terminal"] .nav-link {
	                                      padding: 0.1875rem 0.5rem !important;
	                                      border-left: 0 !important;
	                                      font-size: 0.8125rem;
	                                      font-weight: 400;
	                                      transition: background var(--transition), color var(--transition) !important;
	                                  }
	                                  html[data-style="terminal"] .nav-link:hover { background: var(--tm-hover); color: var(--color-text); }
	                                  html[data-style="terminal"] .nav-header .nav-link,
	                                  html[data-style="terminal"] .nav-group-label { font-weight: 700; color: var(--color-text); }
	                                  html[data-style="terminal"] .sidebar .nav-link.current { background: var(--st-link); color: var(--st-on-fill); font-weight: 700; }
	                                  html[data-style="terminal"] .sidebar .nav-link.current:focus-visible { box-shadow: inset 0 -2px 0 var(--st-on-fill); }
	                                  html[data-style="terminal"] .nav-chevron-btn { border-radius: var(--radius-sm); }
	                                  html[data-style="terminal"] .nav-chevron-btn:hover { background: var(--tm-hover); }
	                                  html[data-style="terminal"] .nav-children { margin-left: 0.5rem; padding-left: 0.5rem; border-left: 1px dashed var(--tm-line); }
	                                  html[data-style="terminal"] .content { padding: 1.5rem 2rem; }
	                                  html[data-style="terminal"] .page-content h1 {
	                                      font-size: 1.5rem !important;
	                                      font-weight: 700 !important;
	                                      letter-spacing: 0 !important;
	                                      line-height: 1.35 !important;
	                                      margin-bottom: 1rem !important;
	                                  }
	                                  html[data-style="terminal"] .page-content h2 {
	                                      font-size: 1.1875rem !important;
	                                      font-weight: 700;
	                                      letter-spacing: 0;
	                                      margin: 2.25rem 0 0.75rem;
	                                      padding-bottom: 0.25rem;
	                                      border-bottom: 1px dashed var(--tm-line);
	                                  }
	                                  html[data-style="terminal"] .page-content h3 { font-size: 1rem !important; font-weight: 700; letter-spacing: 0; }
	                                  html[data-style="terminal"] .page-content h4 { font-size: 0.9375rem; font-weight: 700; }
	                                  html[data-style="terminal"] .page-content h1::before { content: '$ '; content: '$ ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .page-content h2::before { content: '# '; content: '# ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .page-content h3::before { content: '## '; content: '## ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .page-content h4::before { content: '### '; content: '### ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .page-content p { line-height: calc(1.7 * var(--line-spacing)); margin-bottom: calc(1rem * var(--paragraph-spacing)); }
	                                  html[data-style="terminal"] .page-content li { line-height: calc(1.65 * var(--line-spacing)); }
	                                  html[data-style="terminal"] .page-content a:not([class]) { text-decoration: underline; text-decoration-thickness: 1px; text-underline-offset: 0.2em; }
	                                  html[data-style="terminal"] .page-content :not(pre) > code {
	                                      background: var(--color-bg-code);
	                                      border-color: var(--color-border);
	                                      border-radius: var(--radius-sm) !important;
	                                      font-size: 0.9em !important;
	                                      font-weight: 700;
	                                  }
	                                  html[data-style="terminal"] .page-content pre { border-color: var(--color-border); border-radius: var(--radius); }
	                                  html[data-style="terminal"] .page-content pre :is(.code-lang, .copy-btn) { border-radius: 0 !important; }
	                                  html[data-style="terminal"] .page-content blockquote { background: none; border-left: 2px solid var(--tm-line); border-radius: 0; }
	                                  html[data-style="terminal"] .page-content table { font-size: 0.8125rem; }
	                                  html[data-style="terminal"] .page-content :is(th, td) { padding: 0.375rem 0.75rem; }
	                                  html[data-style="terminal"] .page-content th { font-size: 0.6875rem; letter-spacing: 0.06em; text-transform: uppercase; color: var(--color-text-secondary); }
	                                  html[data-style="terminal"] .page-content .admonition {
	                                      border: 1px solid var(--color-border);
	                                      border-left: 3px solid var(--st-c);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 6%, var(--color-bg));
	                                  }
	                                  html[data-style="terminal"] .page-content .admonition-title { font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="terminal"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border-radius: var(--radius); }
	                                  html[data-style="terminal"] :is(.tab-headers, .install-tabs) { border-radius: 0; }
	                                  html[data-style="terminal"] :is(.tab-header, .install-tab) { font-size: 0.75rem; }
	                                  html[data-style="terminal"] :is(.tab-header.active, .install-tab.active) { background: var(--color-bg); font-weight: 700; }
	                                  html[data-style="terminal"] :is(.component-card, .component-link-card) { border-radius: var(--radius); }
	                                  html[data-style="terminal"] :is(.component-card, .component-link-card):hover { border-color: var(--st-link); box-shadow: none; }
	                                  html[data-style="terminal"] .component-step-number { border-radius: var(--radius-sm); background: transparent; color: var(--st-link); border: 1px solid currentColor; font-weight: 700; }
	                                  html[data-style="terminal"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label, .page-contributor-avatar) { border-radius: var(--radius-sm); }
	                                  html[data-style="terminal"] .toc-sidebar { padding: 1rem 0; }
	                                  html[data-style="terminal"] .toc-title { font-size: 0.6875rem; letter-spacing: 0.08em; color: var(--color-text-muted); }
	                                  html[data-style="terminal"] .toc-list { border-left: 1px dashed var(--tm-line); }
	                                  html[data-style="terminal"] .toc-item a { font-size: 0.75rem; border-left-width: 1px !important; margin-left: -1px; }
	                                  html[data-style="terminal"] .toc-item a.active { background: none; font-weight: 700 !important; }
	                                  html[data-style="terminal"] .toc-item a.active::before { content: '> '; content: '> ' / ''; }
	                                  html[data-style="terminal"] .toc-progress { height: 1px; border-radius: 0; }
	                                  html[data-style="terminal"] .toc-progress-bar { background: var(--st-link); border-radius: 0; }
	                                  html[data-style="terminal"] .toc-caret { border-radius: var(--radius-sm); }
	                                  html[data-style="terminal"] .toc-caret:hover { background: var(--tm-hover); }
	                                  html[data-style="terminal"] :is(.toc-counter, .toc-back-to-top) { font-size: 0.6875rem; }
	                                  html[data-style="terminal"] .toc-counter { border-top-style: dashed; }
	                                  html[data-style="terminal"] .breadcrumbs { font-size: 0.75rem; }
	                                  html[data-style="terminal"] .breadcrumbs li:not(:last-child)::after { opacity: 1; }
	                                  html[data-style="terminal"] :is(.edit-link, .last-updated, .page-contributors) { border-top-style: dashed; font-size: 0.75rem; }
	                                  html[data-style="terminal"] .page-nav-link { border-radius: var(--radius); }
	                                  html[data-style="terminal"] .page-nav-link:hover { background: var(--tm-hover); border-color: var(--st-link); box-shadow: none; }
	                                  html[data-style="terminal"] .page-nav-label { font-size: 0.6875rem; letter-spacing: 0.08em; }
	                                  html[data-style="terminal"] .page-nav-title { font-size: 0.8125rem; }
	                                  html[data-style="terminal"] :is(.feedback-btn, .install-copy-btn, .install-nuget-link) { border-radius: var(--radius-sm); }
	                                  html[data-style="terminal"] .page-actions .page-action { border-color: transparent; border-radius: var(--radius-sm); padding-left: 0.25rem; padding-right: 0.25rem; }
	                                  html[data-style="terminal"] .page-actions .page-action:hover { border-color: transparent; background: var(--tm-hover); color: var(--st-link); }
	                                  html[data-style="terminal"] .page-actions .page-action::before { content: '['; content: '[' / ''; color: var(--color-text-muted); }
	                                  html[data-style="terminal"] .page-actions .page-action::after { content: ']'; content: ']' / ''; color: var(--color-text-muted); }
	                                  html[data-style="terminal"] .footer-inner { font-size: 0.75rem; }
	                                  html[data-style="terminal"] .skip-link { border-radius: var(--radius-sm); border-width: 1px; }
	                                  html[data-style="terminal"] .search-backdrop { background: rgba(0, 0, 0, 0.6); backdrop-filter: none; }
	                                  html[data-style="terminal"] .search-dialog { border-color: var(--tm-line); box-shadow: none; }
	                                  html[data-style="terminal"] .search-input { font-size: 0.875rem; }
	                                  html[data-style="terminal"] .search-input-wrap > svg { display: none; }
	                                  html[data-style="terminal"] .search-input-wrap::before { content: '>'; content: '>' / ''; color: var(--st-link); font-weight: 700; }
	                                  html[data-style="terminal"] :is(.search-esc, .search-close) { border-radius: var(--radius-sm); }
	                                  html[data-style="terminal"] .search-result { border-radius: var(--radius); box-shadow: inset 2px 0 0 transparent; }
	                                  html[data-style="terminal"] :is(.search-result:hover, .search-result.active) { background: var(--tm-hover); box-shadow: inset 2px 0 0 var(--st-link); }
	                                  html[data-style="terminal"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius-sm); background: var(--color-bg); color: var(--st-link); border: 1px solid currentColor; box-shadow: none; }
	                                  html[data-style="terminal"] .back-to-top:hover { background: var(--tm-hover) !important; box-shadow: none !important; transform: none; }
	                                  html[data-style="terminal"] .landing::before,
	                                  html[data-style="terminal"] .landing-hero::after { display: none; }
	                                  html[data-style="terminal"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="terminal"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="terminal"] .landing-hero-icon { animation: none; border-radius: var(--radius-sm); background: none; border: 1px solid var(--color-border); }
	                                  html[data-style="terminal"] .landing-hero-title { font-weight: 700; letter-spacing: -0.02em; }
	                                  html[data-style="terminal"] .landing-hero-title::before { content: '$ '; content: '$ ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"] .landing-btn { border-radius: var(--radius-sm); min-width: 0; font-size: 0.8125rem; }
	                                  html[data-style="terminal"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="terminal"] .landing-btn-secondary:hover { border-color: var(--st-link); color: var(--st-link); }
	                                  html[data-style="terminal"] :is(.landing-feature-card, .landing-feature-icon) { border-radius: var(--radius); }
	                                  html[data-style="terminal"] .landing-feature-card::before { display: none; }
	                                  html[data-style="terminal"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-color: var(--st-link); }
	                                  html[data-style="terminal"] .landing .landing-content > ul > li { border-radius: var(--radius); }
	                                  html[data-style="terminal"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; border-color: var(--st-link); }
	                                  html[data-style="terminal"][data-navbar="floating"] .site-header { border-radius: var(--radius); box-shadow: none; background: var(--color-bg); backdrop-filter: none; }
	                                  html[data-style="terminal"][data-navbar="tabs"] .header-tab.current { background: var(--tm-hover); }
	                                  html[data-style="terminal"]:is([data-footer="columns"], [data-footer="mega"]) .footer-col-title::before { content: '# '; content: '# ' / ''; color: var(--st-link); }
	                                  html[data-style="terminal"][data-toc="inline"] .toc-inline { border-style: dashed; border-color: var(--tm-line); border-radius: var(--radius); background: none; }
	                                  html[data-style="terminal"][data-toc="floating"] .toc-sidebar { border-color: var(--tm-line); border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="terminal"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border-radius: var(--radius-sm); background: none; }
	                                  html[data-style="terminal"][data-page-nav="minimal"] .page-nav-link:hover { background: none; }

	                                  /* Style: Paper */
	                                  html[data-style="paper"] {
	                                      --color-bg: #fbf8f1;
	                                      --color-bg-secondary: #f3eee2;
	                                      --color-bg-code: #f1ebde;
	                                      --color-text: #2a241d;
	                                      --color-text-secondary: #4d4438;
	                                      --color-text-muted: #6a5f50;
	                                      --color-border: #ddd3c0;
	                                      --color-border-light: #eae3d4;
	                                      --pp-rule: #cbbfa8;
	                                      --pp-hover: #efe8da;
	                                      --font-body: 'Iowan Old Style', 'Sitka Text', Charter, 'Bitstream Charter', 'Palatino Linotype', Palatino, 'Book Antiqua', Georgia, Cambria, serif;
	                                      --font-mono: ui-monospace, 'SF Mono', 'Cascadia Mono', Consolas, 'Liberation Mono', Menlo, monospace;
	                                      --radius: 3px;
	                                      --radius-sm: 2px;
	                                      --radius-md: 3px;
	                                      --shadow-sm: none;
	                                      --shadow-md: 0 1px 3px rgba(60, 45, 20, 0.12);
	                                      --shadow-lg: 0 6px 24px rgba(60, 45, 20, 0.16);
	                                      --header-height: 58px;
	                                      --sidebar-width: 264px;
	                                      --toc-width: 224px;
	                                      --content-max-width: 680px;
	                                  }
	                                  html[data-style="paper"][data-theme="dark"] {
	                                      --dark-bg: #1b1814;
	                                      --dark-bg-secondary: #23201a;
	                                      --dark-bg-code: #221e18;
	                                      --color-text: #ebe3d3;
	                                      --color-text-secondary: #c9bda9;
	                                      --color-text-muted: #a99c87;
	                                      --color-border: #3b342a;
	                                      --color-border-light: #2b2620;
	                                      --pp-rule: #514838;
	                                      --pp-hover: color-mix(in oklab, var(--color-text) 3%, var(--color-bg-secondary));
	                                      --shadow-md: 0 1px 3px rgba(0, 0, 0, 0.4);
	                                      --shadow-lg: 0 8px 28px rgba(0, 0, 0, 0.55);
	                                  }
	                                  html[data-style="paper"] body { font-size: 1.0625rem; line-height: 1.7; font-kerning: normal; }
	                                  html[data-style="paper"] .site-header { background: var(--color-bg); border-bottom-color: var(--pp-rule); backdrop-filter: none; }
	                                  html[data-style="paper"] .site-name { font-size: 1.1875rem; font-weight: 600; font-variant-caps: small-caps; letter-spacing: 0.04em; }
	                                  html[data-style="paper"] .search-trigger { background: transparent; }
	                                  html[data-style="paper"] .search-label { font-style: italic; }
	                                  html[data-style="paper"] .search-trigger kbd { background: transparent; }
	                                  html[data-style="paper"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: transparent; background: transparent; }
	                                  html[data-style="paper"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):hover { background: var(--pp-hover); }
	                                  html[data-style="paper"] :is(.appearance-panel, .version-dropdown) { border-color: var(--pp-rule); }
	                                  html[data-style="paper"] :is(.appearance-option, .version-option):hover { background: var(--pp-hover); }
	                                  html[data-style="paper"] .appearance-title { font-weight: 600; font-variant-caps: small-caps; letter-spacing: 0.04em; }
	                                  html[data-style="paper"] .appearance-label { font-size: 0.875rem; font-variant-caps: all-small-caps; letter-spacing: 0.06em; text-transform: none; }
	                                  html[data-style="paper"] .appearance-section { border-top-color: var(--pp-rule); }
	                                  html[data-style="paper"] :is(.appearance-option, .appearance-reset) { border-color: var(--pp-rule); }
	                                  html[data-style="paper"] :is(.appearance-reset, .appearance-close):hover { background: var(--pp-hover); }
	                                  html[data-style="paper"] .sidebar { border-right-color: var(--pp-rule); padding: 2rem 1.25rem; }
	                                  html[data-style="paper"] .nav-section { margin-bottom: 0.75rem; }
	                                  html[data-style="paper"] .nav-link {
	                                      padding: 0.25rem 0.75rem !important;
	                                      border-radius: 0;
	                                      border-left: 1px solid transparent !important;
	                                      font-size: 0.9375rem;
	                                      font-weight: 400;
	                                      color: var(--color-text-secondary);
	                                  }
	                                  html[data-style="paper"] .nav-link:hover { background: none; color: var(--color-text); }
	                                  html[data-style="paper"] .nav-link.current { background: none; border-left-color: currentColor !important; font-style: italic; font-weight: 600; }
	                                  html[data-style="paper"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) {
	                                      font-size: 1rem;
	                                      font-weight: 600;
	                                      font-variant-caps: all-small-caps;
	                                      letter-spacing: 0.06em;
	                                      color: var(--color-text);
	                                  }
	                                  html[data-style="paper"] .nav-section > .nav-header .nav-link.current { color: var(--st-link); }
	                                  html[data-style="paper"] .nav-chevron-btn:hover { background: var(--pp-hover); }
	                                  html[data-style="paper"] .nav-children { padding-left: 0.75rem; }
	                                  html[data-style="paper"] .content { padding: 3rem 3rem 2.5rem; }
	                                  html[data-style="paper"] .page-content h1 {
	                                      font-size: 2.375rem !important;
	                                      font-weight: 600 !important;
	                                      letter-spacing: -0.01em !important;
	                                      line-height: 1.2 !important;
	                                      margin-bottom: 1.5rem !important;
	                                  }
	                                  html[data-style="paper"] .page-content h2 {
	                                      font-size: 1.5rem !important;
	                                      font-weight: 600;
	                                      letter-spacing: 0;
	                                      margin: 2.75rem 0 1rem;
	                                      padding: 1.25rem 0 0;
	                                      border-bottom: none;
	                                      border-top: 1px solid var(--pp-rule);
	                                  }
	                                  html[data-style="paper"] .page-content h3 { font-size: 1.1875rem !important; font-weight: 600; font-style: italic; letter-spacing: 0; }
	                                  html[data-style="paper"] .page-content h4 { font-size: 1.0625rem; font-variant-caps: all-small-caps; letter-spacing: 0.06em; }
	                                  html[data-style="paper"] .page-content p { line-height: calc(1.75 * var(--line-spacing)); margin-bottom: calc(1.125rem * var(--paragraph-spacing)); }
	                                  html[data-style="paper"] .page-content li { line-height: calc(1.7 * var(--line-spacing)); }
	                                  html[data-style="paper"] .page-content a:not([class]) {
	                                      text-decoration: underline;
	                                      text-decoration-thickness: 1px;
	                                      text-underline-offset: 0.2em;
	                                      text-decoration-color: color-mix(in srgb, currentColor 45%, transparent);
	                                  }
	                                  html[data-style="paper"] .page-content a:not([class]):hover { text-decoration-color: currentColor; }
	                                  html[data-style="paper"] .page-content :not(pre) > code { background: var(--color-bg-code); border-color: transparent; color: var(--color-text); font-weight: 400; }
	                                  html[data-style="paper"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="paper"] .page-content blockquote {
	                                      padding: 0.25rem 0 0.25rem 1.5rem;
	                                      background: none;
	                                      border-left: 2px solid var(--pp-rule);
	                                      border-radius: 0;
	                                      font-style: italic;
	                                  }
	                                  html[data-style="paper"] .page-content table { font-size: 0.9375rem; font-variant-numeric: lining-nums tabular-nums; }
	                                  html[data-style="paper"] .page-content :is(th, td) { padding: 0.5rem 1.25rem 0.5rem 0; border: none; border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="paper"] .page-content thead th { border-top: 2px solid var(--color-text); border-bottom-color: var(--color-text); }
	                                  html[data-style="paper"] .page-content tbody tr:last-child td { border-bottom: 2px solid var(--color-text); }
	                                  html[data-style="paper"] .page-content th { background: none; font-size: 1rem; font-variant-caps: all-small-caps; letter-spacing: 0.05em; }
	                                  html[data-style="paper"] .page-content tr:hover td { background: none; }
	                                  html[data-style="paper"] .page-content .admonition {
	                                      padding: 0.875rem 0;
	                                      background: none;
	                                      border: none;
	                                      border-top: 1px solid var(--pp-rule);
	                                      border-bottom: 1px solid var(--pp-rule);
	                                      border-radius: 0;
	                                  }
	                                  html[data-style="paper"] .page-content .admonition-title { font-size: 1.0625rem; font-variant-caps: all-small-caps; letter-spacing: 0.06em; }
	                                  html[data-style="paper"] :is(.tabs, .package-install-widget, .source-viewer) { border-color: var(--pp-rule); border-left: none; border-right: none; border-radius: 0; }
	                                  html[data-style="paper"] :is(.tab-headers, .install-header, .install-tabs) { background: none; border-radius: 0; border-bottom-color: var(--pp-rule); }
	                                  html[data-style="paper"] .install-header { padding-left: 0; padding-right: 0; }
	                                  html[data-style="paper"] .install-tabs { padding: 0; }
	                                  html[data-style="paper"] :is(.tab-header.active, .install-tab.active) { font-style: italic; }
	                                  html[data-style="paper"] .tab-content { padding: 1rem 0; }
	                                  html[data-style="paper"] .component-code-group .tab-content { padding: 0; }
	                                  html[data-style="paper"] .source-viewer summary { padding-left: 0; padding-right: 0; font-style: italic; }
	                                  html[data-style="paper"] .source-viewer pre { border-radius: 0; }
	                                  html[data-style="paper"] :is(.component-card, .component-link-card) { background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; box-shadow: none; }
	                                  html[data-style="paper"] :is(.component-card, .component-link-card):hover { background: none; box-shadow: none; border-top-color: var(--st-link); }
	                                  html[data-style="paper"] .component-card-info { border-top: 2px solid #3b82f6; }
	                                  html[data-style="paper"] .component-card-success { border-top: 2px solid #22c55e; }
	                                  html[data-style="paper"] .component-card-warning { border-top: 2px solid #f59e0b; }
	                                  html[data-style="paper"] .component-card-title { font-variant-caps: all-small-caps; letter-spacing: 0.06em; }
	                                  html[data-style="paper"] .component-step-number { background: none; color: var(--st-link); border: 1px solid var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--pp-rule); }
	                                  html[data-style="paper"] .toc-sidebar { border-left-color: var(--pp-rule); }
	                                  html[data-style="paper"] .toc-title { text-transform: none; font-size: 0.9375rem; font-variant-caps: all-small-caps; letter-spacing: 0.08em; color: var(--color-text-secondary); }
	                                  html[data-style="paper"] .toc-list { border-left: 1px solid var(--pp-rule); }
	                                  html[data-style="paper"] .toc-item a { font-size: 0.875rem; border-left-width: 1px !important; margin-left: -1px; color: var(--color-text-secondary); }
	                                  html[data-style="paper"] .toc-item a.active { background: none; font-style: italic; }
	                                  html[data-style="paper"] .toc-progress { height: 1px; border-radius: 0; background: var(--pp-rule); }
	                                  html[data-style="paper"] .toc-progress-bar { background: var(--st-link); border-radius: 0; }
	                                  html[data-style="paper"] .toc-caret:hover { background: var(--pp-hover); }
	                                  html[data-style="paper"] .toc-counter { border-top-color: var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"] .breadcrumbs { font-size: 0.9375rem; font-variant-caps: all-small-caps; letter-spacing: 0.05em; }
	                                  html[data-style="paper"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"] .page-nav-link { padding: 1rem 0 0; background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; }
	                                  html[data-style="paper"] .page-nav-link:hover { background: none; box-shadow: none; border-top-color: var(--st-link); }
	                                  html[data-style="paper"] .page-nav-label { text-transform: none; font-size: 0.9375rem; font-variant-caps: all-small-caps; letter-spacing: 0.08em; }
	                                  html[data-style="paper"] .page-nav-title { font-size: 1.0625rem; font-style: italic; }
	                                  html[data-style="paper"] .feedback-prompt { font-style: italic; }
	                                  html[data-style="paper"] .page-actions .page-action { border-color: transparent; padding-left: 0.375rem; padding-right: 0.375rem; font-style: italic; }
	                                  html[data-style="paper"] .page-actions .page-action:hover { border-color: transparent; text-decoration: underline; text-underline-offset: 0.2em; }
	                                  html[data-style="paper"] .site-footer { border-top-color: var(--pp-rule); }
	                                  html[data-style="paper"] .footer-inner { font-style: italic; }
	                                  html[data-style="paper"] .skip-link { border-width: 1px; font-style: italic; }
	                                  html[data-style="paper"] .search-backdrop { background: rgba(40, 30, 15, 0.35); backdrop-filter: none; }
	                                  html[data-style="paper"] .search-dialog { border-color: var(--pp-rule); box-shadow: var(--shadow-lg); }
	                                  html[data-style="paper"] .search-input { font-size: 1.0625rem; }
	                                  html[data-style="paper"] .search-input::placeholder,
	                                  html[data-style="paper"] .search-message { font-style: italic; }
	                                  html[data-style="paper"] .search-result { border-radius: 0; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="paper"] :is(.search-result:hover, .search-result.active) { background: var(--pp-hover); }
	                                  html[data-style="paper"] .search-result-title { font-size: 1rem; }
	                                  html[data-style="paper"] .search-result-category { font-style: italic; }
	                                  html[data-style="paper"] :is(.back-to-top, .toc-toggle-btn) { box-shadow: var(--shadow-md); }
	                                  html[data-style="paper"] .landing::before,
	                                  html[data-style="paper"] .landing-hero::after { display: none; }
	                                  html[data-style="paper"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="paper"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="paper"] .landing-hero { border-bottom: 1px solid var(--pp-rule); }
	                                  html[data-style="paper"] .landing-hero-icon { animation: none; background: none; }
	                                  html[data-style="paper"] .landing-hero-title { font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="paper"] .landing-hero-subtitle { font-style: italic; }
	                                  html[data-style="paper"] .landing-btn { min-width: 0; }
	                                  html[data-style="paper"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="paper"] .landing-btn-secondary:hover { border-color: var(--st-link); color: var(--st-link); }
	                                  html[data-style="paper"] .landing-feature-card { padding: 1.25rem 0 0; background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; }
	                                  html[data-style="paper"] .landing-feature-card::before { display: none; }
	                                  html[data-style="paper"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-top-color: var(--st-link); }
	                                  html[data-style="paper"] .landing-feature-icon { background: none; width: auto; height: auto; justify-content: flex-start; }
	                                  html[data-style="paper"] .landing-feature-name { font-style: italic; }
	                                  html[data-style="paper"] .landing .landing-content > ul > li { background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; }
	                                  html[data-style="paper"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; }
	                                  html[data-style="paper"] .landing-md-content table { border-color: transparent; }
	                                  html[data-style="paper"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="paper"] .landing-md-content th { background: none; }
	                                  html[data-style="paper"] .landing-footer { border-top-color: var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"] .openapi-endpoint-card { padding: 1.5rem 0; background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; }
	                                  html[data-style="paper"] .api-namespace { font-style: italic; }
	                                  html[data-style="paper"][data-navbar="floating"] .site-header { background: var(--color-bg); border-color: var(--pp-rule); border-radius: var(--radius); backdrop-filter: none; }
	                                  html[data-style="paper"][data-navbar="tabs"] .header-tab { font-size: 1rem; font-variant-caps: all-small-caps; letter-spacing: 0.06em; }
	                                  html[data-style="paper"]:is([data-footer="columns"], [data-footer="mega"]) .footer-col-title { text-transform: none; font-size: 0.9375rem; font-variant-caps: all-small-caps; letter-spacing: 0.08em; }
	                                  html[data-style="paper"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top-color: var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"][data-toc="inline"] .toc-inline { background: none; border-color: var(--pp-rule); border-left: none; border-right: none; border-radius: 0; }
	                                  html[data-style="paper"][data-toc="inline"] .toc-inline > summary { padding-left: 0; padding-right: 0; text-transform: none; font-size: 0.9375rem; font-variant-caps: all-small-caps; letter-spacing: 0.08em; }
	                                  html[data-style="paper"][data-toc="inline"] .toc-inline .toc { padding-left: 0; padding-right: 0; }
	                                  html[data-style="paper"][data-toc="floating"] .toc-sidebar { border-color: var(--pp-rule); }
	                                  html[data-style="paper"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { background: none; border-radius: var(--radius); }
	                                  html[data-style="paper"][data-breadcrumbs="path"] .breadcrumbs { font-variant-caps: normal; letter-spacing: 0.02em; }

	                                  /* Style: Contrast. Body text is 7:1 or more, everything else at least 4.5:1; links are underlined; state never
	                                     rests on color alone; nothing is translucent or blurred. */
	                                  html[data-style="contrast"] {
	                                      --color-bg: #ffffff;
	                                      --color-bg-secondary: #f0f0f0;
	                                      --color-bg-code: #f0f0f0;
	                                      --color-text: #000000;
	                                      --color-text-secondary: #222222;
	                                      --color-text-muted: #3d3d3d;
	                                      --color-border: #6b6b6b;
	                                      --color-border-light: #858585;
	                                      --hc-hover: #e2e2e2;
	                                      --st-hue: 46%;
	                                      --radius: 4px;
	                                      --radius-sm: 3px;
	                                      --radius-md: 4px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                  }
	                                  html[data-style="contrast"][data-theme="dark"] {
	                                      --dark-bg: #000000;
	                                      --dark-bg-secondary: #161616;
	                                      --dark-bg-code: #121212;
	                                      --color-text: #ffffff;
	                                      --color-text-secondary: #e8e8e8;
	                                      --color-text-muted: #cfcfcf;
	                                      --color-border: #9c9c9c;
	                                      --color-border-light: #808080;
	                                      --hc-hover: color-mix(in oklab, var(--color-text) 11%, var(--color-bg-secondary));
	                                      --st-hue: 55%;
	                                      --st-on-fill: #000000;
	                                  }
	                                  html[data-style="contrast"] body { font-size: 1.0625rem; line-height: 1.7; }
	                                  html[data-style="contrast"] :focus-visible { outline: 3px solid var(--color-text); outline-offset: 2px; box-shadow: 0 0 0 5px var(--color-bg); }
	                                  /* These scroll or clip, which cut off a ring drawn outside the element. */
	                                  html[data-style="contrast"] :is(.sidebar, .toc-sidebar, .tab-headers, .install-tabs, .header-tabs, .search-results, .version-dropdown) :focus-visible { outline-offset: -3px; box-shadow: none; }
	                                  html[data-style="contrast"] .code-block > pre > code:focus-visible { outline-width: 3px; outline-offset: -3px; }
	                                  html[data-style="contrast"] :is(.page-content, .landing-content) a:not([class]),
	                                  html[data-style="contrast"] .api-member-link,
	                                  html[data-style="contrast"] .breadcrumbs a,
	                                  html[data-style="contrast"] .edit-link a,
	                                  html[data-style="contrast"] .site-footer a,
	                                  html[data-style="contrast"] .landing-footer a {
	                                      text-decoration: underline;
	                                      text-decoration-thickness: max(1px, 0.07em);
	                                      text-underline-offset: 0.18em;
	                                  }
	                                  html[data-style="contrast"] :is(.page-content, .landing-content) a:not([class]):hover,
	                                  html[data-style="contrast"] .breadcrumbs a:hover,
	                                  html[data-style="contrast"] .site-footer a:hover { text-decoration-thickness: 2px; }
	                                  html[data-style="contrast"] .site-header { background: var(--color-bg); border-bottom: 2px solid var(--color-text); backdrop-filter: none; }
	                                  html[data-style="contrast"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger) {
	                                      border: 2px solid var(--color-border);
	                                      background: var(--color-bg);
	                                      color: var(--color-text);
	                                  }
	                                  html[data-style="contrast"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { background: var(--hc-hover); border-color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] { background: var(--hc-hover); border-color: var(--color-text); color: var(--color-text); }
	                                  html[data-style="contrast"] .search-trigger kbd { background: var(--color-bg); border-color: var(--color-border); color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.appearance-panel, .version-dropdown) { border: 2px solid var(--color-text); }
	                                  html[data-style="contrast"] :is(.appearance-option, .version-option) { color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.appearance-option, .version-option):hover { background: var(--hc-hover); }
	                                  html[data-style="contrast"] :is(.appearance-option[aria-checked="true"], .version-option.active) { font-weight: 700; }
	                                  html[data-style="contrast"] :is(.appearance-label, .appearance-group-label, .appearance-style-desc) { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .appearance-section { border-top: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] :is(.appearance-option, .appearance-reset) { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] :is(.appearance-reset, .appearance-close) { color: var(--color-text); }
	                                  html[data-style="contrast"] .appearance-option[aria-checked="true"] { border-color: var(--color-text); box-shadow: inset 0 0 0 2px var(--color-text); }
	                                  html[data-style="contrast"] :is(.appearance-reset, .appearance-close):hover { background: var(--hc-hover); }
	                                  html[data-style="contrast"] .sidebar { border-right: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .nav-link {
	                                      padding: 0.375rem 0.75rem 0.375rem calc(0.75rem - 4px) !important;
	                                      border-left: 4px solid transparent !important;
	                                      color: var(--color-text-secondary);
	                                      transition: none !important;
	                                  }
	                                  html[data-style="contrast"] .nav-link:hover { background: var(--hc-hover); color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] .nav-header .nav-link,
	                                  html[data-style="contrast"] .nav-group-label { color: var(--color-text); }
	                                  html[data-style="contrast"] .sidebar .nav-link.current { background: var(--color-bg-secondary); border-left-color: currentColor !important; font-weight: 700; }
	                                  html[data-style="contrast"] .nav-chevron-btn { color: var(--color-text); }
	                                  html[data-style="contrast"] .nav-chevron-btn:hover { background: var(--hc-hover); }
	                                  html[data-style="contrast"] .page-content h1 { font-weight: 700 !important; letter-spacing: -0.02em !important; }
	                                  html[data-style="contrast"] .page-content h2 { border-bottom: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .page-content :not(pre) > code { background: var(--color-bg-secondary); border-color: var(--color-border-light); color: var(--color-text); }
	                                  html[data-style="contrast"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="contrast"] .page-content pre { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .page-content pre .code-lang { color: var(--sh-punctuation) !important; background: transparent !important; border: none; opacity: 1; }
	                                  html[data-style="contrast"] .page-content pre .copy-btn { color: var(--sh-punctuation); border-color: currentColor; background: transparent; }
	                                  html[data-style="contrast"] .page-content pre .copy-btn:hover { color: var(--sh-punctuation); background: transparent; text-decoration: underline; }
	                                  html[data-style="contrast"] .code-block[data-focus] > pre > code > .line:not(.focused) { opacity: 1; filter: none; }
	                                  html[data-style="contrast"] .code-block[data-focus] > pre > code > .line.focused { box-shadow: inset 3px 0 0 var(--sh-line-highlight-border); }
	                                  html[data-style="contrast"] .page-content blockquote { background: var(--color-bg); border-left: 4px solid var(--color-text); color: var(--color-text); }
	                                  html[data-style="contrast"] .page-content :is(th, td) { border-color: var(--color-border); }
	                                  html[data-style="contrast"] .page-content th { color: var(--color-text); }
	                                  html[data-style="contrast"] .page-content .admonition {
	                                      background: var(--color-bg);
	                                      border: 2px solid color-mix(in srgb, var(--st-c) var(--st-hue), var(--st-ink));
	                                      border-left-width: 6px;
	                                  }
	                                  html[data-style="contrast"] :is(.tabs, .package-install-widget, .source-viewer) { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] :is(.tab-header, .install-tab) { color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.tab-header.active, .install-tab.active) { border-bottom-width: 4px; font-weight: 700; }
	                                  html[data-style="contrast"] .install-nuget-link { color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] .install-copy-btn { border: 2px solid var(--color-border); color: var(--color-text); }
	                                  html[data-style="contrast"] .install-panel pre code { color: var(--color-text); }
	                                  html[data-style="contrast"] .source-viewer summary { color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.component-card, .component-link-card) { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 6px; }
	                                  html[data-style="contrast"] :is(.component-card, .component-link-card):hover { border-color: var(--color-text); box-shadow: none; transform: none; }
	                                  html[data-style="contrast"] :is(.component-link-card-desc, .component-card-title) { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .toc-sidebar { border-left: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .toc-title { color: var(--color-text); }
	                                  html[data-style="contrast"] .toc-list { border-left: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .toc-item a { color: var(--color-text-secondary); border-left-width: 4px !important; margin-left: -2px; }
	                                  html[data-style="contrast"] .toc-item a:hover { color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] .toc-item a.active { background: var(--color-bg-secondary); border-left-color: currentColor !important; font-weight: 700 !important; }
	                                  html[data-style="contrast"] .toc-caret { color: var(--color-text); }
	                                  html[data-style="contrast"] .toc-caret:hover { background: var(--hc-hover); }
	                                  html[data-style="contrast"] .toc-progress { background: var(--color-border-light); }
	                                  html[data-style="contrast"] .toc-progress-bar { background: var(--color-text); transition: none; }
	                                  html[data-style="contrast"] .breadcrumbs li:not(:last-child)::after { opacity: 1; color: var(--color-text-muted); }
	                                  html[data-style="contrast"] :is(.edit-link, .last-updated, .page-contributors) { border-top: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .page-contributor-avatar { border: 2px solid var(--color-border); color: var(--color-text); }
	                                  html[data-style="contrast"] .page-nav-link { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .page-nav-link:hover { border-color: var(--color-text); background: var(--hc-hover); box-shadow: none; }
	                                  html[data-style="contrast"] .page-nav-link:hover .page-nav-title { text-decoration: underline; }
	                                  html[data-style="contrast"] .page-nav-arrow { color: var(--color-text); }
	                                  html[data-style="contrast"] .feedback-btn { border: 2px solid var(--color-border); color: var(--color-text); }
	                                  html[data-style="contrast"] .feedback-btn:disabled:not(.selected) { opacity: 1; text-decoration: line-through; }
	                                  html[data-style="contrast"] .feedback-btn.selected { border-width: 3px; font-weight: 700; }
	                                  html[data-style="contrast"] .page-actions .page-action { border: 2px solid var(--color-border); color: var(--color-text); }
	                                  html[data-style="contrast"] .page-actions .page-action:hover { border-color: var(--color-text); background: var(--hc-hover); text-decoration: underline; }
	                                  html[data-style="contrast"] .site-footer { border-top: 2px solid var(--color-text); }
	                                  html[data-style="contrast"] .footer-inner { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .skip-link { border: 3px solid var(--color-text); color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] .search-backdrop { background: rgba(0, 0, 0, 0.85); backdrop-filter: none; }
	                                  html[data-style="contrast"] .search-dialog { border: 2px solid var(--color-text); box-shadow: none; }
	                                  html[data-style="contrast"] .search-input-wrap { border-bottom: 2px solid var(--color-text); color: var(--color-text); }
	                                  html[data-style="contrast"] .search-input-wrap:focus-within { box-shadow: inset 0 -4px 0 var(--color-text); }
	                                  html[data-style="contrast"] .search-input::placeholder { color: var(--color-text-muted); opacity: 1; }
	                                  html[data-style="contrast"] :is(.search-close, .search-esc, .search-message) { color: var(--color-text); }
	                                  html[data-style="contrast"] .search-esc { background: var(--color-bg); border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .search-result { border: 2px solid transparent; }
	                                  html[data-style="contrast"] :is(.search-result:hover, .search-result.active) { background: var(--hc-hover); border-color: var(--color-text); }
	                                  html[data-style="contrast"] .search-result-snippet { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .search-result-snippet mark { background: #ffe14d; color: #000000; text-decoration: underline; }
	                                  html[data-style="contrast"] :is(.back-to-top, .toc-toggle-btn) { border: 2px solid var(--color-bg); box-shadow: 0 0 0 2px var(--st-link); }
	                                  html[data-style="contrast"] .back-to-top:hover { box-shadow: 0 0 0 2px var(--color-text) !important; transform: none; }
	                                  html[data-style="contrast"] .lightbox { background: #000000; }
	                                  html[data-style="contrast"] .lightbox-close { background: #000000; border: 2px solid #ffffff; }
	                                  html[data-style="contrast"] .nav-backdrop.open { background: rgba(0, 0, 0, 0.7); }
	                                  html[data-style="contrast"] .landing::before,
	                                  html[data-style="contrast"] .landing-hero::after { display: none; }
	                                  html[data-style="contrast"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="contrast"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="contrast"] .landing-hero { border-bottom: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .landing-hero-icon { animation: none; background: none; }
	                                  html[data-style="contrast"] .landing-hero-subtitle { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] :is(.landing-hero-title, .landing-hero-subtitle, .landing-hero-actions) { animation: none; }
	                                  html[data-style="contrast"] .landing-btn { border: 2px solid var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="contrast"] .landing-btn-secondary { background: var(--color-bg); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="contrast"] .landing-btn-secondary:hover { background: var(--hc-hover); color: var(--color-text); }
	                                  html[data-style="contrast"] .landing-feature-card { border: 2px solid var(--color-border); background: var(--color-bg); }
	                                  html[data-style="contrast"] .landing-feature-card::before { display: none; }
	                                  html[data-style="contrast"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-color: var(--color-text); }
	                                  html[data-style="contrast"] .landing-feature-desc { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .landing .landing-content > ul > li { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; border-color: var(--color-text); }
	                                  html[data-style="contrast"] .landing-footer { border-top: 2px solid var(--color-text); color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] :is(.repl-container, .blazor-preview-container, .openapi-endpoint-card) { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] :is(.api-badge, .api-badge-sm, .openapi-required, .openapi-optional, .openapi-auth-label, .openapi-status) { border: 1px solid currentColor; }
	                                  html[data-style="contrast"] .api-namespace { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"] .version-badge { border: 1px solid currentColor; }
	                                  html[data-style="contrast"][data-navbar="floating"] .site-header { border: 2px solid var(--color-text); background: var(--color-bg); box-shadow: none; backdrop-filter: none; }
	                                  html[data-style="contrast"][data-navbar="tabs"] .header-tabs { border-top: 2px solid var(--color-border); }
	                                  html[data-style="contrast"][data-navbar="tabs"] .header-tab { color: var(--color-text); }
	                                  html[data-style="contrast"][data-navbar="tabs"] .header-tab.current { border-bottom-width: 4px; font-weight: 700; }
	                                  html[data-style="contrast"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top: 2px solid var(--color-border); }
	                                  html[data-style="contrast"]:is([data-footer="columns"], [data-footer="mega"]) :is(.footer-brand-desc, .footer-bottom, .footer-col a) { color: var(--color-text-secondary); }
	                                  html[data-style="contrast"][data-toc="inline"] .toc-inline { border: 2px solid var(--color-border); background: var(--color-bg); }
	                                  html[data-style="contrast"][data-toc="floating"] .toc-sidebar { border: 2px solid var(--color-text); box-shadow: none; }
	                                  html[data-style="contrast"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border: 2px solid var(--color-border); background: var(--color-bg); }
	                                  html[data-style="contrast"][data-breadcrumbs="pills"] .breadcrumbs [aria-current="page"] { border-color: var(--color-text); font-weight: 700; }
	                                  html[data-style="contrast"][data-breadcrumbs] .breadcrumbs li:not(:last-child)::after { opacity: 1; }
	                                  html[data-style="contrast"][data-page-nav="minimal"] .page-nav-link { border: none; border-top: 2px solid var(--color-border); }
	                                  html[data-style="contrast"][data-page-nav="minimal"] .page-nav-link:hover { border-top-color: var(--color-text); background: none; }
	                                  @media (forced-colors: active) {
	                                      html[data-style="contrast"] .sidebar .nav-link.current,
	                                      html[data-style="contrast"] .toc-item a.active { border-left-color: Highlight !important; }
	                                      html[data-style="contrast"] :is(.tab-header.active, .install-tab.active, .header-tab.current) { border-bottom-color: Highlight; }
	                                      html[data-style="contrast"] :is(.search-result:hover, .search-result.active) { border-color: Highlight; }
	                                      html[data-style="contrast"] .search-input-wrap:focus-within { outline: 3px solid Highlight; outline-offset: -3px; }
	                                      html[data-style="contrast"] :is(.back-to-top, .toc-toggle-btn) { border-color: ButtonText; }
	                                  }

	                                  /* Style: Glass. --gl-panel is the translucent fill of the header, sidebar, cards and menus; the fallbacks at the
	                                     end make it solid where blur is missing or the reader asks for less transparency. */
	                                  html[data-style="glass"] {
	                                      --color-bg: #f3f5fa;
	                                      --color-bg-secondary: #e8ecf4;
	                                      --color-bg-code: #e7ebf3;
	                                      --color-text: #111625;
	                                      --color-text-secondary: #363e56;
	                                      --color-text-muted: #4b5470;
	                                      --color-border: rgba(17, 22, 37, 0.13);
	                                      --color-border-light: rgba(17, 22, 37, 0.08);
	                                      --gl-solid: #ffffff;
	                                      --gl-panel: rgba(255, 255, 255, 0.6);
	                                      --gl-panel-strong: rgba(255, 255, 255, 0.82);
	                                      --gl-edge: rgba(255, 255, 255, 0.75);
	                                      --gl-hover: rgba(17, 22, 37, 0.06);
	                                      --gl-blur: blur(18px) saturate(1.6);
	                                      --gl-glow-a: color-mix(in srgb, var(--color-primary) 22%, transparent);
	                                      --gl-glow-b: color-mix(in srgb, var(--gradient-tertiary, var(--color-primary)) 16%, transparent);
	                                      --st-hue: 50%;
	                                      --radius: 12px;
	                                      --radius-sm: 8px;
	                                      --radius-md: 14px;
	                                      --shadow-sm: 0 0 0 1px rgba(17, 22, 37, 0.05), 0 1px 3px rgba(17, 22, 37, 0.08);
	                                      --shadow-md: 0 0 0 1px rgba(17, 22, 37, 0.05), 0 10px 28px -10px rgba(17, 22, 37, 0.22);
	                                      --shadow-lg: 0 0 0 1px rgba(17, 22, 37, 0.06), 0 22px 56px -14px rgba(17, 22, 37, 0.32);
	                                      --transition: 180ms ease;
	                                      --header-height: 62px;
	                                  }
	                                  html[data-style="glass"][data-theme="dark"] {
	                                      --dark-bg: #0b0e16;
	                                      --dark-bg-secondary: #151a26;
	                                      --dark-bg-code: #121724;
	                                      --color-text: #eef1f8;
	                                      --color-text-secondary: #c3c9d8;
	                                      --color-text-muted: #a1a9bd;
	                                      --color-border: rgba(255, 255, 255, 0.13);
	                                      --color-border-light: rgba(255, 255, 255, 0.08);
	                                      --gl-solid: var(--color-bg-secondary);
	                                      --gl-panel: color-mix(in srgb, color-mix(in oklab, var(--color-text) 4%, var(--color-bg-secondary)) 55%, transparent);
	                                      --gl-panel-strong: color-mix(in srgb, color-mix(in oklab, var(--color-text) 4%, var(--color-bg-secondary)) 86%, transparent);
	                                      --gl-edge: rgba(255, 255, 255, 0.1);
	                                      --gl-hover: rgba(255, 255, 255, 0.07);
	                                      --gl-glow-a: color-mix(in srgb, var(--color-primary) 20%, transparent);
	                                      --gl-glow-b: color-mix(in srgb, var(--gradient-tertiary, var(--color-primary)) 14%, transparent);
	                                      --st-hue: 60%;
	                                      --shadow-sm: 0 0 0 1px rgba(255, 255, 255, 0.04), 0 1px 3px rgba(0, 0, 0, 0.4);
	                                      --shadow-md: 0 0 0 1px rgba(255, 255, 255, 0.04), 0 12px 32px -12px rgba(0, 0, 0, 0.7);
	                                      --shadow-lg: 0 0 0 1px rgba(255, 255, 255, 0.05), 0 24px 60px -14px rgba(0, 0, 0, 0.8);
	                                  }
	                                  html[data-style="glass"] body {
	                                      background:
	                                          radial-gradient(56rem 38rem at 6% -10%, var(--gl-glow-a), transparent 65%),
	                                          radial-gradient(48rem 36rem at 100% 12%, var(--gl-glow-b), transparent 65%),
	                                          radial-gradient(44rem 32rem at 45% 112%, color-mix(in srgb, var(--color-primary) 10%, transparent), transparent 65%),
	                                          var(--color-bg);
	                                      background-attachment: fixed;
	                                  }
	                                  html[data-style="glass"] .sidebar,
	                                  html[data-style="glass"] :is(.component-card, .component-link-card, .page-nav-link, .tabs, .package-install-widget, .source-viewer, .openapi-endpoint-card, .landing-feature-card, .toc-inline) {
	                                      background: var(--gl-panel);
	                                      -webkit-backdrop-filter: var(--gl-blur);
	                                      backdrop-filter: var(--gl-blur);
	                                  }
	                                  /* Page content scrolls under the header, so it is the stronger fill: muted text stays 4.5:1 over a dark code block. */
	                                  html[data-style="glass"] .site-header {
	                                      background: var(--gl-panel-strong);
	                                      -webkit-backdrop-filter: var(--gl-blur);
	                                      backdrop-filter: var(--gl-blur);
	                                      border-bottom: 1px solid var(--gl-edge);
	                                      box-shadow: var(--shadow-sm);
	                                  }
	                                  html[data-style="glass"] .sidebar { border-right: 1px solid var(--gl-edge); }
	                                  html[data-style="glass"] :is(.component-card, .component-link-card, .page-nav-link, .tabs, .package-install-widget, .source-viewer, .openapi-endpoint-card, .landing-feature-card) {
	                                      border: 1px solid var(--gl-edge);
	                                      border-radius: var(--radius-md);
	                                      box-shadow: var(--shadow-md);
	                                  }
	                                  html[data-style="glass"] .component-card-info { border-left: 4px solid #3b82f6; }
	                                  html[data-style="glass"] .component-card-success { border-left: 4px solid #22c55e; }
	                                  html[data-style="glass"] .component-card-warning { border-left: 4px solid #f59e0b; }
	                                  html[data-style="glass"] :is(.component-card, .component-link-card, .page-nav-link, .landing-feature-card):hover { background: var(--gl-panel-strong); box-shadow: var(--shadow-lg); }
	                                  html[data-style="glass"] :is(.appearance-panel, .version-dropdown, .search-dialog) {
	                                      background: var(--gl-panel-strong);
	                                      -webkit-backdrop-filter: var(--gl-blur);
	                                      backdrop-filter: var(--gl-blur);
	                                      border: 1px solid var(--gl-edge);
	                                      box-shadow: var(--shadow-lg);
	                                  }
	                                  html[data-style="glass"] :is(.appearance-option, .version-option, .search-result):hover,
	                                  html[data-style="glass"] .search-result.active { background: var(--gl-hover); }
	                                  html[data-style="glass"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger) {
	                                      border-color: var(--gl-edge);
	                                      background: var(--gl-panel);
	                                      border-radius: calc(10px * var(--radius-scale));
	                                  }
	                                  html[data-style="glass"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { background: var(--gl-panel-strong); color: var(--color-text); }
	                                  /* The panel opens inside the header, whose backdrop-filter keeps a nested blur to the header's own box, so the page
	                                     would show through it unblurred: it gets a solid fill. */
	                                  html[data-style="glass"] .appearance-panel { background: var(--gl-solid); -webkit-backdrop-filter: none; backdrop-filter: none; }
	                                  html[data-style="glass"] .appearance-section { border-top-color: var(--color-border-light); }
	                                  html[data-style="glass"] :is(.appearance-option, .appearance-reset, .appearance-close) { border-radius: calc(10px * var(--radius-scale)); }
	                                  html[data-style="glass"] .appearance-option[aria-checked="true"] { background: var(--gl-hover); }
	                                  html[data-style="glass"] :is(.appearance-reset, .appearance-close):hover { background: var(--gl-hover); }
	                                  html[data-style="glass"] .search-trigger kbd { background: transparent; border-color: var(--color-border); }
	                                  html[data-style="glass"] .nav-link { padding: 0.375rem 0.75rem !important; border-left: 0 !important; border-radius: calc(10px * var(--radius-scale)); }
	                                  html[data-style="glass"] .nav-link:hover { background: var(--gl-hover); }
	                                  html[data-style="glass"] .sidebar .nav-link.current { background: var(--gl-panel-strong); box-shadow: var(--shadow-sm); }
	                                  html[data-style="glass"] .nav-chevron-btn { border-radius: calc(8px * var(--radius-scale)); }
	                                  html[data-style="glass"] .nav-chevron-btn:hover { background: var(--gl-hover); }
	                                  html[data-style="glass"] .content { padding: 2.25rem 2.75rem; }
	                                  html[data-style="glass"] .page-content h1 { letter-spacing: -0.03em !important; }
	                                  html[data-style="glass"] .page-content h2 { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="glass"] .page-content :not(pre) > code { background: var(--gl-panel-strong); border-color: var(--color-border-light); border-radius: calc(6px * var(--radius-scale)) !important; }
	                                  html[data-style="glass"] :is(.page-content, .landing-content) pre { border-color: var(--gl-edge); box-shadow: var(--shadow-md); }
	                                  html[data-style="glass"] .page-content :is(.install-panel, .source-viewer, .tab-content, .repl-container, .blazor-preview-source) pre { box-shadow: none; }
	                                  html[data-style="glass"] .page-content table { border-collapse: separate; border-spacing: 0; border: 1px solid var(--color-border); border-radius: var(--radius); background: var(--gl-panel); }
	                                  html[data-style="glass"] .page-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="glass"] .page-content tr:last-child td { border-bottom: none; }
	                                  html[data-style="glass"] .page-content th { background: var(--gl-hover); }
	                                  html[data-style="glass"] .page-content tr:hover td { background: var(--gl-hover); }
	                                  html[data-style="glass"] .page-content blockquote { background: var(--gl-panel); border-radius: 0 var(--radius) var(--radius) 0; border-left-color: var(--st-link); }
	                                  html[data-style="glass"] .page-content .admonition {
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 30%, transparent);
	                                      border-left: 4px solid var(--st-c);
	                                      background: color-mix(in srgb, var(--st-c) 9%, var(--gl-panel-strong));
	                                  }
	                                  html[data-style="glass"] :is(.tab-headers, .install-tabs, .install-header) { background: var(--gl-hover); border-bottom-color: var(--color-border-light); }
	                                  html[data-style="glass"] .tab-headers { border-radius: var(--radius-md) var(--radius-md) 0 0; }
	                                  html[data-style="glass"] .install-copy-btn { border-radius: calc(8px * var(--radius-scale)); background: var(--gl-panel-strong); }
	                                  html[data-style="glass"] .component-step-number { box-shadow: var(--shadow-sm); }
	                                  html[data-style="glass"] .toc-sidebar { border-left: none; }
	                                  html[data-style="glass"] .toc-list { border-left-color: var(--color-border); }
	                                  html[data-style="glass"] .toc-item a { border-radius: 0 calc(8px * var(--radius-scale)) calc(8px * var(--radius-scale)) 0; }
	                                  html[data-style="glass"] .toc-item a:hover { background: var(--gl-hover); }
	                                  html[data-style="glass"] .toc-item a.active { background: var(--gl-hover); }
	                                  html[data-style="glass"] .toc-progress { background: var(--color-border); }
	                                  html[data-style="glass"] .toc-caret:hover { background: var(--gl-hover); }
	                                  html[data-style="glass"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--color-border-light); }
	                                  html[data-style="glass"] .page-contributor-avatar { background: var(--gl-panel-strong); }
	                                  html[data-style="glass"] .page-nav-link:hover { border-color: var(--gl-edge); }
	                                  html[data-style="glass"] .feedback-btn { border-radius: calc(10px * var(--radius-scale)); background: var(--gl-panel); }
	                                  html[data-style="glass"] .page-actions .page-action { border-color: var(--gl-edge); border-radius: calc(10px * var(--radius-scale)); background: var(--gl-panel); }
	                                  html[data-style="glass"] .page-actions .page-action:hover { border-color: var(--gl-edge); background: var(--gl-panel-strong); }
	                                  html[data-style="glass"] .site-footer { background: var(--gl-panel); border-top-color: var(--gl-edge); }
	                                  html[data-style="glass"] .skip-link { border-radius: calc(10px * var(--radius-scale)); box-shadow: var(--shadow-lg); }
	                                  html[data-style="glass"] .search-backdrop { background: color-mix(in srgb, var(--color-bg) 45%, transparent); -webkit-backdrop-filter: blur(6px); backdrop-filter: blur(6px); }
	                                  html[data-style="glass"] .search-dialog { border-radius: calc(18px * var(--radius-scale)); }
	                                  html[data-style="glass"] .search-input-wrap { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="glass"] .search-result { border-radius: calc(10px * var(--radius-scale)); }
	                                  html[data-style="glass"] .search-close { border-radius: calc(8px * var(--radius-scale)); }
	                                  html[data-style="glass"] :is(.back-to-top, .toc-toggle-btn) { box-shadow: var(--shadow-md); }
	                                  html[data-style="glass"] .lightbox { background: rgba(8, 10, 16, 0.72); -webkit-backdrop-filter: blur(10px); backdrop-filter: blur(10px); }
	                                  html[data-style="glass"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="glass"] .landing-hero-icon { background: var(--gl-panel); box-shadow: var(--shadow-md); -webkit-backdrop-filter: var(--gl-blur); backdrop-filter: var(--gl-blur); }
	                                  html[data-style="glass"] .landing-btn { border-radius: calc(12px * var(--radius-scale)); }
	                                  html[data-style="glass"] .landing-btn-secondary { background: var(--gl-panel); border-color: var(--gl-edge); }
	                                  html[data-style="glass"] .landing-feature-card::before { display: none; }
	                                  html[data-style="glass"] .landing-feature-card:hover { transform: translateY(-2px) !important; }
	                                  html[data-style="glass"] .landing-feature-icon { border-radius: calc(12px * var(--radius-scale)); background: var(--gl-panel-strong); }
	                                  html[data-style="glass"] .landing .landing-content > ul > li { background: var(--gl-panel); border-color: var(--gl-edge); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
	                                  html[data-style="glass"] .landing-footer { background: var(--gl-panel); border-top-color: var(--gl-edge); }
	                                  html[data-style="glass"] :is(.repl-container, .blazor-preview-container) { border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
	                                  html[data-style="glass"] :is(.openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="glass"][data-navbar="floating"] .site-header { border: 1px solid var(--gl-edge); border-radius: calc(18px * var(--radius-scale)); background: var(--gl-panel); box-shadow: var(--shadow-lg); }
	                                  html[data-style="glass"][data-navbar="tabs"] .header-tab.current { background: var(--gl-hover); }
	                                  html[data-style="glass"][data-toc="inline"] .toc-inline { border-color: var(--gl-edge); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
	                                  html[data-style="glass"][data-toc="floating"] .toc-sidebar { background: var(--gl-panel-strong); -webkit-backdrop-filter: var(--gl-blur); backdrop-filter: var(--gl-blur); border-color: var(--gl-edge); border-radius: calc(18px * var(--radius-scale)); }
	                                  html[data-style="glass"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { background: var(--gl-panel); border-color: var(--gl-edge); }
	                                  html[data-style="glass"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top-color: var(--color-border-light); }
	                                  html[data-style="glass"][data-page-nav="minimal"] .page-nav-link { background: none; -webkit-backdrop-filter: none; backdrop-filter: none; }
	                                  @supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
	                                      html[data-style="glass"] { --gl-panel: var(--gl-solid); --gl-panel-strong: var(--gl-solid); }
	                                  }
	                                  @media (prefers-reduced-transparency: reduce) {
	                                      html[data-style="glass"] { --gl-panel: var(--gl-solid); --gl-panel-strong: var(--gl-solid); --gl-blur: none; }
	                                      html[data-style="glass"] body { background: var(--color-bg); }
	                                      html[data-style="glass"] :is(.search-backdrop, .lightbox) { -webkit-backdrop-filter: none; backdrop-filter: none; }
	                                  }

	                                  /* Style: Brutal. --bt-ink draws the lines, --bt-shadow the hard offset shadows (the accent in dark mode), --bt-fill the
	                                     flat accent fill behind current and hovered items. */
	                                  html[data-style="brutal"] {
	                                      --color-bg: #fffdf7;
	                                      --color-bg-secondary: #f3efe2;
	                                      --color-bg-code: #f3efe2;
	                                      --color-text: #0a0a0a;
	                                      --color-text-secondary: #1f1f1f;
	                                      --color-text-muted: #3b3b3b;
	                                      --color-border: #0a0a0a;
	                                      --color-border-light: #0a0a0a;
	                                      --bt-ink: #0a0a0a;
	                                      --bt-shadow: #0a0a0a;
	                                      --bt-fill: color-mix(in srgb, var(--color-primary) 22%, #ffffff);
	                                      --bt-hover: #ebe5d3;
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: 2px 2px 0 var(--bt-shadow);
	                                      --shadow-md: 4px 4px 0 var(--bt-shadow);
	                                      --shadow-lg: 6px 6px 0 var(--bt-shadow);
	                                      --transition: 80ms linear;
	                                  }
	                                  html[data-style="brutal"][data-theme="dark"] {
	                                      --dark-bg: #111111;
	                                      --dark-bg-secondary: #1c1c1c;
	                                      --dark-bg-code: #1a1a1a;
	                                      --color-text: #f7f7f2;
	                                      --color-text-secondary: #e4e4de;
	                                      --color-text-muted: #c4c4bd;
	                                      --color-border: #f7f7f2;
	                                      --color-border-light: #f7f7f2;
	                                      --bt-ink: #f7f7f2;
	                                      --bt-shadow: var(--st-link);
	                                      --bt-fill: color-mix(in srgb, var(--color-primary) 24%, var(--color-bg));
	                                      --bt-hover: color-mix(in oklab, var(--color-text) 6%, var(--color-bg-secondary));
	                                  }
	                                  html[data-style="brutal"] :focus-visible { outline: 3px solid var(--bt-ink); outline-offset: 2px; box-shadow: none; }
	                                  html[data-style="brutal"] :is(.sidebar, .toc-sidebar, .tab-headers, .install-tabs, .header-tabs, .search-results, .version-dropdown) :focus-visible { outline-offset: -3px; }
	                                  html[data-style="brutal"] .site-header { background: var(--color-bg); border-bottom: 3px solid var(--bt-ink); backdrop-filter: none; }
	                                  html[data-style="brutal"] .site-name { font-weight: 900; letter-spacing: -0.01em; text-transform: uppercase; }
	                                  html[data-style="brutal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger) {
	                                      border: 2px solid var(--bt-ink);
	                                      background: var(--color-bg);
	                                      color: var(--color-text);
	                                      box-shadow: 2px 2px 0 var(--bt-shadow);
	                                  }
	                                  html[data-style="brutal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { background: var(--bt-fill); color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] {
	                                      background: var(--bt-fill);
	                                      border-color: var(--bt-ink);
	                                      color: var(--color-text);
	                                      box-shadow: none;
	                                      transform: translate(2px, 2px);
	                                  }
	                                  html[data-style="brutal"] .search-trigger { font-weight: 700; color: var(--color-text-secondary); }
	                                  html[data-style="brutal"] .search-trigger kbd { border: 2px solid var(--bt-ink); background: var(--bt-fill); color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.appearance-panel, .version-dropdown) { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-lg); }
	                                  html[data-style="brutal"] :is(.appearance-option, .version-option) { color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.appearance-option, .version-option):hover { background: var(--bt-hover); }
	                                  html[data-style="brutal"] :is(.appearance-option[aria-checked="true"], .version-option.active) { background: var(--bt-fill); font-weight: 800; }
	                                  html[data-style="brutal"] :is(.appearance-title, .appearance-label) { font-weight: 900; text-transform: uppercase; }
	                                  html[data-style="brutal"] .appearance-label { color: var(--color-text); }
	                                  html[data-style="brutal"] .appearance-section { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] :is(.appearance-option, .appearance-reset) { border: 2px solid var(--bt-ink); color: var(--color-text); font-weight: 700; }
	                                  html[data-style="brutal"] .appearance-option[aria-checked="true"] { box-shadow: 2px 2px 0 var(--bt-shadow); }
	                                  html[data-style="brutal"] .appearance-style[aria-checked="true"] .appearance-style-name { color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.appearance-reset, .appearance-close):hover { background: var(--bt-hover); }
	                                  html[data-style="brutal"] .sidebar { border-right: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .nav-link {
	                                      padding: 0.3125rem 0.625rem !important;
	                                      border: 2px solid transparent !important;
	                                      color: var(--color-text-secondary);
	                                      font-weight: 600;
	                                      transition: none !important;
	                                  }
	                                  html[data-style="brutal"] .nav-link:hover { background: var(--bt-hover); border-color: var(--bt-ink) !important; color: var(--color-text); }
	                                  html[data-style="brutal"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--color-text); font-size: 0.75rem; font-weight: 900; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="brutal"] .sidebar .nav-link.current { background: var(--bt-fill); border-color: var(--bt-ink) !important; color: var(--color-text); font-weight: 800; }
	                                  html[data-style="brutal"] .nav-chevron-btn { border-radius: var(--radius-sm); color: var(--color-text); }
	                                  html[data-style="brutal"] .nav-chevron-btn:hover { background: var(--bt-hover); }
	                                  html[data-style="brutal"] .nav-children { margin-left: 0.625rem; padding-left: 0.5rem; border-left: 2px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .page-content h1 {
	                                      font-size: 2.5rem !important;
	                                      font-weight: 900 !important;
	                                      letter-spacing: -0.03em !important;
	                                      line-height: 1.05 !important;
	                                  }
	                                  html[data-style="brutal"] .page-content h2 { font-size: 1.625rem !important; font-weight: 900; letter-spacing: -0.02em; border-bottom: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .page-content :is(h3, h4) { font-weight: 800; }
	                                  html[data-style="brutal"] .page-content a:not([class]) { font-weight: 600; text-decoration: underline; text-decoration-thickness: 2px; text-underline-offset: 0.15em; }
	                                  html[data-style="brutal"] .page-content a:not([class]):hover { background: var(--bt-fill); color: var(--color-text); }
	                                  html[data-style="brutal"] .page-content :not(pre) > code { background: var(--bt-fill); border-color: var(--bt-ink); color: var(--color-text); font-weight: 600; }
	                                  html[data-style="brutal"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.page-content, .landing-content) pre { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .page-content :is(.install-panel, .source-viewer, .tab-content, .repl-container, .blazor-preview-source) pre { box-shadow: none; }
	                                  html[data-style="brutal"] .page-content :is(.tab-content, .source-viewer) pre { border-width: 0; }
	                                  html[data-style="brutal"] .page-content :is(th, td) { border: 2px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .page-content th { background: var(--bt-ink); color: var(--color-bg); font-size: 0.8125rem; font-weight: 800; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="brutal"] .page-content tr:hover td { background: var(--bt-hover); }
	                                  html[data-style="brutal"] .page-content blockquote { background: var(--bt-fill); border-left: 6px solid var(--bt-ink); color: var(--color-text); }
	                                  html[data-style="brutal"] .page-content .admonition {
	                                      border: 3px solid var(--bt-ink);
	                                      border-left: 12px solid var(--st-c);
	                                      background: color-mix(in srgb, var(--st-c) 12%, var(--color-bg));
	                                      box-shadow: var(--shadow-md);
	                                  }
	                                  html[data-style="brutal"] .page-content .admonition-title { color: var(--color-text); font-weight: 900; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="brutal"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] :is(.tab-headers, .install-tabs, .install-header) { border-bottom: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] :is(.tab-header, .install-tab) { border-right: 2px solid var(--bt-ink); color: var(--color-text); font-weight: 700; }
	                                  html[data-style="brutal"] :is(.tab-header, .install-tab):hover { background: var(--bt-hover); }
	                                  html[data-style="brutal"] :is(.tab-header.active, .install-tab.active) { background: var(--bt-fill); color: var(--color-text); border-bottom-color: var(--bt-ink); font-weight: 900; }
	                                  html[data-style="brutal"] .source-viewer summary { color: var(--color-text); font-weight: 700; }
	                                  html[data-style="brutal"] .source-viewer[open] summary { border-bottom: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] :is(.component-card, .component-link-card, .landing-feature-card, .page-nav-link) { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-width: 12px; }
	                                  html[data-style="brutal"] .component-card-info { border-left-color: #3b82f6; }
	                                  html[data-style="brutal"] .component-card-success { border-left-color: #22c55e; }
	                                  html[data-style="brutal"] .component-card-warning { border-left-color: #f59e0b; }
	                                  html[data-style="brutal"] :is(.component-link-card, .page-nav-link):hover { background: var(--bt-fill); border-color: var(--bt-ink); box-shadow: var(--shadow-lg); transform: translate(-2px, -2px); }
	                                  html[data-style="brutal"] :is(.component-link-card, .page-nav-link):active { box-shadow: none; transform: translate(4px, 4px); }
	                                  html[data-style="brutal"] .component-card:hover { box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .component-card-title { font-weight: 900; text-transform: uppercase; letter-spacing: 0.04em; }
	                                  html[data-style="brutal"] .component-step-number { border: 3px solid var(--bt-ink); font-weight: 900; }
	                                  html[data-style="brutal"] .component-step:not(:last-child) .component-step-indicator::after { width: 3px; background: var(--bt-ink); }
	                                  html[data-style="brutal"] .toc-sidebar { border-left: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .toc-title { font-weight: 900; color: var(--color-text); }
	                                  html[data-style="brutal"] .toc-list { border-left: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .toc-item a { color: var(--color-text-secondary); border-left-width: 3px !important; margin-left: -3px; font-weight: 600; }
	                                  html[data-style="brutal"] .toc-item a:hover { background: var(--bt-hover); color: var(--color-text); }
	                                  html[data-style="brutal"] .toc-item a.active { background: var(--bt-fill); color: var(--color-text) !important; border-left-color: var(--bt-ink) !important; font-weight: 900 !important; }
	                                  html[data-style="brutal"] .toc-progress { height: 6px; border: 2px solid var(--bt-ink); border-radius: 0; background: var(--color-bg); }
	                                  html[data-style="brutal"] .toc-progress-bar { background: var(--bt-ink); border-radius: 0; }
	                                  html[data-style="brutal"] .toc-caret { color: var(--color-text); }
	                                  html[data-style="brutal"] .toc-caret:hover { background: var(--bt-hover); }
	                                  html[data-style="brutal"] .toc-counter { border-top: 3px solid var(--bt-ink); font-weight: 700; }
	                                  html[data-style="brutal"] .breadcrumbs { font-weight: 700; }
	                                  html[data-style="brutal"] .breadcrumbs li:not(:last-child)::after { opacity: 1; color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.edit-link, .last-updated, .page-contributors) { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .page-contributor-avatar { border: 2px solid var(--bt-ink); border-radius: var(--radius-sm); color: var(--color-text); }
	                                  html[data-style="brutal"] .page-nav-label { color: var(--color-text-secondary); font-weight: 800; }
	                                  html[data-style="brutal"] .page-nav-title { font-weight: 900; }
	                                  html[data-style="brutal"] .page-nav-arrow { color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action) {
	                                      border: 2px solid var(--bt-ink);
	                                      background: var(--color-bg);
	                                      color: var(--color-text);
	                                      box-shadow: 3px 3px 0 var(--bt-shadow);
	                                      font-weight: 700;
	                                  }
	                                  html[data-style="brutal"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action):hover { background: var(--bt-fill); border-color: var(--bt-ink); color: var(--color-text); }
	                                  html[data-style="brutal"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action):active { box-shadow: none; transform: translate(3px, 3px); }
	                                  html[data-style="brutal"] .feedback-btn.selected { background: var(--bt-fill); box-shadow: none; transform: translate(3px, 3px); }
	                                  html[data-style="brutal"] .feedback-btn:disabled:not(.selected) { opacity: 1; text-decoration: line-through; box-shadow: none; }
	                                  html[data-style="brutal"] .site-footer { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .footer-inner { color: var(--color-text-secondary); font-weight: 600; }
	                                  html[data-style="brutal"] .skip-link { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); font-weight: 900; }
	                                  html[data-style="brutal"] .search-backdrop { background: rgba(0, 0, 0, 0.55); backdrop-filter: none; }
	                                  html[data-style="brutal"] .search-dialog { border: 3px solid var(--bt-ink); box-shadow: 8px 8px 0 var(--bt-shadow); }
	                                  html[data-style="brutal"] .search-input-wrap { border-bottom: 3px solid var(--bt-ink); color: var(--color-text); }
	                                  html[data-style="brutal"] .search-input { font-weight: 600; }
	                                  html[data-style="brutal"] .search-esc { border: 2px solid var(--bt-ink); color: var(--color-text); }
	                                  html[data-style="brutal"] .search-result { border: 2px solid transparent; }
	                                  html[data-style="brutal"] :is(.search-result:hover, .search-result.active) { background: var(--bt-fill); border-color: var(--bt-ink); }
	                                  html[data-style="brutal"] .search-result-title { font-weight: 800; }
	                                  html[data-style="brutal"] .search-result-snippet mark { background: var(--bt-fill); color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="brutal"] :is(.back-to-top, .toc-toggle-btn) { border: 3px solid var(--bt-ink); border-radius: var(--radius-sm); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .back-to-top:hover { box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="brutal"] .lightbox-close { border-radius: var(--radius-sm); border-width: 2px; }
	                                  html[data-style="brutal"] .landing::before,
	                                  html[data-style="brutal"] .landing-hero::after { display: none; }
	                                  html[data-style="brutal"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="brutal"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="brutal"] .landing-hero { border-bottom: 3px solid var(--bt-ink); background: var(--bt-fill); }
	                                  html[data-style="brutal"] .landing-hero-icon { animation: none; border: 3px solid var(--bt-ink); background: var(--color-bg); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .landing-hero-title { font-weight: 900; letter-spacing: -0.04em; }
	                                  html[data-style="brutal"] .landing-hero-subtitle { color: var(--color-text); font-weight: 600; }
	                                  html[data-style="brutal"] .landing-btn { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); font-weight: 900; text-transform: uppercase; letter-spacing: 0.04em; min-width: 0; }
	                                  html[data-style="brutal"] :is(.landing-btn-primary, .landing-btn-secondary):hover { transform: translate(-2px, -2px); box-shadow: var(--shadow-lg); }
	                                  html[data-style="brutal"] :is(.landing-btn-primary, .landing-btn-secondary):active { transform: translate(4px, 4px); box-shadow: none; }
	                                  html[data-style="brutal"] .landing-btn-secondary { background: var(--color-bg); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="brutal"] .landing-btn-secondary:hover { background: var(--color-bg); border-color: var(--bt-ink); color: var(--color-text); }
	                                  html[data-style="brutal"] .landing-feature-card::before { display: none; }
	                                  html[data-style="brutal"] .landing-feature-card:hover { transform: translate(-2px, -2px) !important; box-shadow: var(--shadow-lg) !important; border-color: var(--bt-ink); }
	                                  html[data-style="brutal"] .landing-feature-icon { border: 2px solid var(--bt-ink); background: var(--bt-fill); }
	                                  html[data-style="brutal"] .landing-feature-name { font-weight: 900; }
	                                  html[data-style="brutal"] .landing .landing-content > ul > li { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .landing .landing-content > ul > li:hover { transform: translate(-2px, -2px) !important; box-shadow: var(--shadow-lg) !important; }
	                                  html[data-style="brutal"] .landing-md-content :is(th, td) { border: 2px solid var(--bt-ink); }
	                                  html[data-style="brutal"] .landing-footer { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border: 2px solid currentColor; font-weight: 800; }
	                                  html[data-style="brutal"][data-navbar="floating"] .site-header { border: 3px solid var(--bt-ink); border-radius: var(--radius); background: var(--color-bg); box-shadow: var(--shadow-lg); backdrop-filter: none; }
	                                  html[data-style="brutal"][data-navbar="tabs"] .header-tabs { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"][data-navbar="tabs"] .header-tab { color: var(--color-text); font-weight: 700; }
	                                  html[data-style="brutal"][data-navbar="tabs"] .header-tab.current { background: var(--bt-fill); color: var(--color-text); border-bottom-width: 4px; font-weight: 900; }
	                                  html[data-style="brutal"]:is([data-footer="columns"], [data-footer="mega"]) .footer-col-title { font-weight: 900; }
	                                  html[data-style="brutal"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top: 3px solid var(--bt-ink); }
	                                  html[data-style="brutal"][data-toc="inline"] .toc-inline { border: 3px solid var(--bt-ink); background: var(--color-bg); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"][data-toc="floating"] .toc-sidebar { border: 3px solid var(--bt-ink); border-radius: var(--radius); box-shadow: var(--shadow-lg); }
	                                  html[data-style="brutal"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border: 2px solid var(--bt-ink); border-radius: var(--radius-sm); background: var(--color-bg); }
	                                  html[data-style="brutal"][data-breadcrumbs="pills"] .breadcrumbs [aria-current="page"] { background: var(--bt-fill); }
	                                  html[data-style="brutal"][data-page-nav="minimal"] .page-nav-link { border: none; border-top: 3px solid var(--bt-ink); box-shadow: none; }
	                                  html[data-style="brutal"][data-page-nav="minimal"] .page-nav-link:hover { background: var(--bt-fill); transform: none; }

	                                  /* Style: Blueprint. A technical drawing: ink-blue text and lines on pale blue paper (deep blueprint blue in dark mode), a
	                                     12px grid on the page, dashed rules, monospace headings and labels, square corners. --bp-ink draws the solid lines,
	                                     --bp-grid the grid lines and --bp-hover the hovered and current rows. */
	                                  html[data-style="blueprint"] {
	                                      --color-bg: #eef4fb;
	                                      --color-bg-secondary: #e2ebf6;
	                                      --color-bg-code: #dfe8f4;
	                                      --color-text: #0b2f5b;
	                                      --color-text-secondary: #1d426e;
	                                      --color-text-muted: #375882;
	                                      --color-border: #7f9cc2;
	                                      --color-border-light: #b8cbe3;
	                                      --bp-ink: #0b2f5b;
	                                      --bp-grid: #dce6f2;
	                                      --bp-hover: #d8e3f1;
	                                      --font-body: 'IBM Plex Sans', 'Segoe UI', system-ui, -apple-system, Roboto, 'Helvetica Neue', Arial, sans-serif;
	                                      --font-mono: 'IBM Plex Mono', 'JetBrains Mono', 'Cascadia Mono', 'SF Mono', Consolas, 'Liberation Mono', Menlo, monospace;
	                                      --st-hue: 54%;
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                      --transition: 120ms linear;
	                                  }
	                                  html[data-style="blueprint"][data-theme="dark"] {
	                                      --dark-bg: #0b2342;
	                                      --dark-bg-secondary: #0e294c;
	                                      --dark-bg-code: #112e54;
	                                      --color-text: #e4eefa;
	                                      --color-text-secondary: #c0d3ea;
	                                      --color-text-muted: #a3bcdb;
	                                      --color-border: #4b74a6;
	                                      --color-border-light: #1f3f68;
	                                      --bp-ink: #8ec5ff;
	                                      --bp-grid: color-mix(in oklab, var(--color-border-light) 25%, var(--color-bg));
	                                      --bp-hover: color-mix(in oklab, var(--color-border-light) 43%, var(--color-bg-secondary));
	                                      --st-hue: 62%;
	                                  }
	                                  /* Two 1px lines a tile: the page paints the grid once, and every panel with a background of its own covers it. */
	                                  html[data-style="blueprint"] body {
	                                      background-color: var(--color-bg);
	                                      background-image: linear-gradient(var(--bp-grid) 1px, transparent 1px), linear-gradient(90deg, var(--bp-grid) 1px, transparent 1px);
	                                      background-size: 12px 12px;
	                                  }
	                                  html[data-style="blueprint"] :is(.sidebar, .toc-sidebar, .tab-headers, .install-tabs, .header-tabs, .search-results, .version-dropdown) :focus-visible { outline-offset: -2px; }
	                                  html[data-style="blueprint"] .site-header { background: var(--color-bg); border-bottom: 2px solid var(--bp-ink); backdrop-filter: none; }
	                                  html[data-style="blueprint"] .site-name { font-family: var(--font-mono); font-size: 0.9375rem; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger) { border: 1px dashed var(--color-border); background: var(--color-bg); color: var(--color-text); }
	                                  html[data-style="blueprint"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { border-style: solid; border-color: var(--bp-ink); background: var(--bp-hover); color: var(--color-text); }
	                                  html[data-style="blueprint"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] { border-style: solid; background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .search-trigger { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.06em; text-transform: uppercase; color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] .search-trigger kbd { background: transparent; border-color: var(--color-border); color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] .version-trigger { font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="blueprint"] :is(.appearance-panel, .version-dropdown) { border: 1px solid var(--bp-ink); box-shadow: none; }
	                                  html[data-style="blueprint"] :is(.appearance-title, .appearance-label, .appearance-group-label) { font-family: var(--font-mono); letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .appearance-title { font-size: 0.875rem; }
	                                  html[data-style="blueprint"] .appearance-label { font-size: 0.6875rem; color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] .appearance-section { border-top: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] :is(.appearance-option, .appearance-reset) { border: 1px dashed var(--color-border); color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] :is(.appearance-option, .version-option, .appearance-reset, .appearance-close):hover { background: var(--bp-hover); color: var(--color-text); }
	                                  html[data-style="blueprint"] .appearance-option[aria-checked="true"] { border-style: solid; border-color: var(--st-link); box-shadow: inset 0 0 0 1px var(--st-link); background: var(--bp-hover); color: var(--st-link); }
	                                  html[data-style="blueprint"] .appearance-reset { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .appearance-layout-field select { border-style: dashed; }
	                                  html[data-style="blueprint"] .version-option.active { background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .sidebar { border-right: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .nav-link {
	                                      padding: 0.3125rem 0.75rem !important;
	                                      border-left: 2px solid transparent !important;
	                                      color: var(--color-text-secondary);
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="blueprint"] .nav-link:hover { background: var(--bp-hover); color: var(--color-text); }
	                                  html[data-style="blueprint"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--color-text); font-family: var(--font-mono); font-size: 0.75rem; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .nav-section > .nav-header .nav-link.current { color: var(--st-link); }
	                                  html[data-style="blueprint"] .sidebar .nav-link.current { background: var(--bp-hover); border-left-color: var(--st-link) !important; font-weight: 600; }
	                                  html[data-style="blueprint"] .nav-chevron-btn { border-radius: var(--radius-sm); }
	                                  html[data-style="blueprint"] .nav-chevron-btn:hover { background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .nav-children { margin-left: 0.75rem; padding-left: 0.375rem; border-left: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .page-content :is(h1, h2, h3, h4) { font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .page-content h1 {
	                                      font-size: 1.875rem !important;
	                                      font-weight: 700 !important;
	                                      letter-spacing: -0.01em !important;
	                                      line-height: 1.25 !important;
	                                      padding-bottom: 0.625rem;
	                                      border-bottom: 2px solid var(--bp-ink);
	                                  }
	                                  html[data-style="blueprint"] .page-content h2 { font-size: 1.375rem !important; font-weight: 700; letter-spacing: 0; padding-bottom: 0.375rem; border-bottom: 1px dashed var(--color-border); }
	                                  /* A datum mark: a hollow square in front of each section title, unless the titles are numbered. */
	                                  html[data-style="blueprint"]:not([data-headings="numbered"]) .page-content h2::before {
	                                      content: '';
	                                      display: inline-block;
	                                      width: 0.5em;
	                                      height: 0.5em;
	                                      margin-right: 0.5em;
	                                      border: 2px solid var(--bp-ink);
	                                      vertical-align: 0.1em;
	                                  }
	                                  html[data-style="blueprint"] .page-content h3 { font-size: 1.125rem !important; font-weight: 700; letter-spacing: 0; }
	                                  html[data-style="blueprint"] .page-content h4 { font-size: 0.9375rem; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .page-content a:not([class]) { text-decoration: underline; text-decoration-style: dashed; text-decoration-thickness: 1px; text-underline-offset: 0.2em; }
	                                  html[data-style="blueprint"] .page-content a:not([class]):hover { text-decoration-style: solid; }
	                                  html[data-style="blueprint"] .page-content :not(pre) > code { background: var(--color-bg-code); border: 1px dashed var(--color-border); border-radius: var(--radius-sm) !important; color: var(--color-text); font-weight: 500; }
	                                  html[data-style="blueprint"] .page-content h1 code { color: var(--color-text); }
	                                  /* The dark code themes take the drawing's deep blue in both modes. A light code theme keeps its own background: its tokens are
	                                     dark. A new light code theme goes in this list too. */
	                                  html[data-style="blueprint"]:not([data-code-theme="catppuccin-latte"], [data-code-theme="github-light"], [data-code-theme="gruvbox-light"], [data-code-theme="solarized-light"], [data-code-theme="rose-pine-dawn"]) :is(.page-content, .landing-content) pre { background: #04142a; }
	                                  html[data-style="blueprint"] :is(.page-content, .landing-content) pre { border: 1px solid var(--bp-ink); border-radius: var(--radius); }
	                                  html[data-style="blueprint"] .page-content :is(.tab-content, .source-viewer) pre { border-width: 0; }
	                                  html[data-style="blueprint"] .page-content :is(th, td) { border: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .page-content th { background: var(--bp-hover); color: var(--color-text); font-family: var(--font-mono); font-size: 0.75rem; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .page-content tr:hover td { background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .page-content blockquote { background: var(--color-bg); border: 1px dashed var(--color-border); border-left: 3px solid var(--bp-ink); border-radius: var(--radius); color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] .page-content .admonition {
	                                      border: 1px dashed var(--color-border);
	                                      border-left: 3px solid var(--st-c);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 7%, var(--color-bg));
	                                  }
	                                  html[data-style="blueprint"] .page-content .admonition-title { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border: 1px solid var(--bp-ink); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"] :is(.tab-headers, .install-tabs, .install-header) { background: var(--bp-hover); border-bottom: 1px dashed var(--color-border); border-radius: 0; }
	                                  html[data-style="blueprint"] :is(.tab-header, .install-tab) { color: var(--color-text-secondary); font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.tab-header, .install-tab):hover { color: var(--color-text); }
	                                  html[data-style="blueprint"] :is(.tab-header.active, .install-tab.active) { background: var(--color-bg); font-weight: 700; }
	                                  html[data-style="blueprint"] .source-viewer summary { font-family: var(--font-mono); font-size: 0.8125rem; }
	                                  html[data-style="blueprint"] .source-viewer[open] summary { border-bottom: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] :is(.component-card, .component-link-card, .page-nav-link) { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); box-shadow: none; }
	                                  html[data-style="blueprint"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-left-style: solid; border-left-width: 3px; }
	                                  html[data-style="blueprint"] .component-card-info { border-left-color: #3b82f6; }
	                                  html[data-style="blueprint"] .component-card-success { border-left-color: #22c55e; }
	                                  html[data-style="blueprint"] .component-card-warning { border-left-color: #f59e0b; }
	                                  html[data-style="blueprint"] :is(.component-link-card, .page-nav-link):hover { border-style: solid; border-color: var(--bp-ink); background: var(--bp-hover); box-shadow: none; transform: none; }
	                                  html[data-style="blueprint"] :is(.component-card-title, .page-nav-label) { font-family: var(--font-mono); letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .component-card-title { font-size: 0.8125rem; }
	                                  html[data-style="blueprint"] .component-step-number { border-radius: var(--radius); font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--color-border); }
	                                  html[data-style="blueprint"] .toc-sidebar { border-left: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .toc-title { color: var(--color-text-secondary); font-family: var(--font-mono); font-size: 0.6875rem; letter-spacing: 0.1em; }
	                                  html[data-style="blueprint"] .toc-list { border-left: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .toc-item a { color: var(--color-text-secondary); border-left-width: 1px !important; margin-left: -1px; }
	                                  html[data-style="blueprint"] .toc-item a:hover { background: var(--bp-hover); color: var(--color-text); }
	                                  html[data-style="blueprint"] .toc-item a.active { background: var(--bp-hover); border-left-color: var(--st-link) !important; font-weight: 600 !important; }
	                                  html[data-style="blueprint"] .toc-progress { height: 2px; border-radius: var(--radius); background: var(--color-border-light); }
	                                  html[data-style="blueprint"] .toc-progress-bar { background: var(--st-link); border-radius: var(--radius); }
	                                  html[data-style="blueprint"] .toc-caret:hover { background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .toc-counter { border-top: 1px dashed var(--color-border); font-family: var(--font-mono); font-size: 0.6875rem; }
	                                  html[data-style="blueprint"] .breadcrumbs { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .breadcrumbs li:not(:last-child)::after { opacity: 1; }
	                                  html[data-style="blueprint"] :is(.edit-link, .last-updated, .page-contributors) { border-top: 1px dashed var(--color-border); font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="blueprint"] .page-contributor-avatar { border: 1px solid var(--bp-ink); border-radius: var(--radius); }
	                                  html[data-style="blueprint"] .page-nav-label { font-size: 0.6875rem; }
	                                  html[data-style="blueprint"] .page-nav-title { font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action) { border-style: dashed; border-radius: var(--radius); font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action):hover,
	                                  html[data-style="blueprint"] .feedback-btn.selected { border-style: solid; }
	                                  html[data-style="blueprint"] .site-footer { border-top: 2px solid var(--bp-ink); background: var(--color-bg); }
	                                  html[data-style="blueprint"] .footer-inner { color: var(--color-text-secondary); font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .skip-link { border: 1px solid var(--bp-ink); border-radius: var(--radius); font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .search-backdrop { background: rgba(4, 20, 42, 0.55); backdrop-filter: none; }
	                                  html[data-style="blueprint"] .search-dialog { border: 1px solid var(--bp-ink); border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="blueprint"] .search-input-wrap { border-bottom: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] :is(.search-esc, .search-close) { border-radius: var(--radius); font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .search-result { border: 1px dashed transparent; border-radius: var(--radius); }
	                                  html[data-style="blueprint"] :is(.search-result:hover, .search-result.active) { background: var(--bp-hover); border-color: var(--color-border); }
	                                  html[data-style="blueprint"] .search-result-title { font-family: var(--font-mono); font-size: 0.875rem; }
	                                  html[data-style="blueprint"] :is(.back-to-top, .toc-toggle-btn) { border: 1px solid var(--bp-ink); border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="blueprint"] .back-to-top:hover { box-shadow: none !important; transform: none; }
	                                  html[data-style="blueprint"] .lightbox-close { border-radius: var(--radius); }
	                                  html[data-style="blueprint"] .landing::before,
	                                  html[data-style="blueprint"] .landing-hero::after { display: none; }
	                                  html[data-style="blueprint"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="blueprint"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="blueprint"] .landing-hero { border-bottom: 2px solid var(--bp-ink); }
	                                  html[data-style="blueprint"] .landing-hero-icon { animation: none; border: 1px solid var(--bp-ink); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"] .landing-hero-title { font-family: var(--font-mono); font-weight: 700; letter-spacing: -0.02em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .landing-hero-subtitle { color: var(--color-text-secondary); }
	                                  html[data-style="blueprint"] :is(.landing-features-title, .landing-md-content h2, .component-step-title) { font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .landing-btn { border-radius: var(--radius); min-width: 0; font-family: var(--font-mono); font-size: 0.875rem; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="blueprint"] .landing-btn-secondary { background: var(--color-bg); border: 1px dashed var(--bp-ink); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="blueprint"] .landing-btn-secondary:hover { border-style: solid; border-color: var(--bp-ink); background: var(--bp-hover); color: var(--color-text); }
	                                  html[data-style="blueprint"] .landing-feature-card { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"] .landing-feature-card::before { display: none; }
	                                  html[data-style="blueprint"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-style: solid; border-color: var(--bp-ink); }
	                                  html[data-style="blueprint"] .landing-feature-icon { border: 1px solid var(--bp-ink); border-radius: var(--radius); background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .landing-feature-name { font-family: var(--font-mono); letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .landing .landing-content > ul > li { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; border-style: solid; border-color: var(--bp-ink); }
	                                  html[data-style="blueprint"] .landing-md-content :is(th, td) { border: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"] .landing-md-content th { background: var(--bp-hover); font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] .landing-footer { border-top: 2px solid var(--bp-ink); background: var(--color-bg); font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: var(--radius); font-family: var(--font-mono); }
	                                  html[data-style="blueprint"] :is(.api-badge, .api-badge-sm) { border: 1px solid currentColor; }
	                                  html[data-style="blueprint"][data-navbar="floating"] .site-header { border: 1px solid var(--bp-ink); border-radius: var(--radius); background: var(--color-bg); box-shadow: none; backdrop-filter: none; }
	                                  html[data-style="blueprint"][data-navbar="tabs"] .header-tabs { border-top: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"][data-navbar="tabs"] .header-tab { font-family: var(--font-mono); font-size: 0.75rem; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="blueprint"]:is([data-footer="columns"], [data-footer="mega"]) .footer-col-title { font-family: var(--font-mono); letter-spacing: 0.08em; }
	                                  html[data-style="blueprint"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top: 1px dashed var(--color-border); }
	                                  html[data-style="blueprint"][data-toc="inline"] .toc-inline { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"][data-toc="floating"] .toc-sidebar { border: 1px solid var(--bp-ink); border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="blueprint"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"][data-page-nav="minimal"] .page-nav-link { border: none; border-top: 1px dashed var(--color-border); background: none; }

	                                  /* Style: Newspaper. Editorial print: newsprint and ink, a serif body under a high-contrast headline face, a double rule
	                                     under the masthead, thin column rules, a drop cap that opens the article, italic callouts on a left rule and square
	                                     corners. --np-accent is the paper's own red for the drop cap and the section kickers; links follow the color preset. */
	                                  html[data-style="newspaper"] {
	                                      --color-bg: #f6f2e8;
	                                      --color-bg-secondary: #ede7d9;
	                                      --color-bg-code: #ebe4d4;
	                                      --color-text: #1a1814;
	                                      --color-text-secondary: #3a352c;
	                                      --color-text-muted: #595244;
	                                      --color-border: #cbc1ab;
	                                      --color-border-light: #ddd5c4;
	                                      --np-ink: #1a1814;
	                                      --np-rule: #9a8f78;
	                                      --np-accent: #9b1c1c;
	                                      --np-hover: #ece5d5;
	                                      --np-head: 'Bodoni 72', 'Bodoni MT', Didot, 'Didot LT STD', 'Playfair Display', 'Libre Bodoni', 'Times New Roman', Times, serif;
	                                      --np-sans: 'Franklin Gothic Medium', 'Libre Franklin', 'Arial Narrow', 'Helvetica Neue', Arial, sans-serif;
	                                      --font-body: Georgia, Cambria, 'Times New Roman', Times, serif;
	                                      --font-mono: ui-monospace, 'SF Mono', 'Cascadia Mono', Consolas, 'Liberation Mono', Menlo, monospace;
	                                      --st-hue: 54%;
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: 0 8px 24px rgba(26, 24, 20, 0.16);
	                                      --transition: 120ms ease;
	                                      --header-height: 64px;
	                                      --content-max-width: 720px;
	                                  }
	                                  html[data-style="newspaper"][data-theme="dark"] {
	                                      --dark-bg: #161513;
	                                      --dark-bg-secondary: #1f1d1a;
	                                      --dark-bg-code: #24221e;
	                                      --color-text: #ede5d3;
	                                      --color-text-secondary: #cfc6b3;
	                                      --color-text-muted: #aca38f;
	                                      --color-border: #45403a;
	                                      --color-border-light: #2c2925;
	                                      --np-ink: #ede5d3;
	                                      --np-rule: #6f675a;
	                                      --np-accent: #e8907f;
	                                      --np-hover: color-mix(in oklab, var(--color-text) 2%, var(--color-bg-secondary));
	                                      --shadow-lg: 0 8px 28px rgba(0, 0, 0, 0.6);
	                                  }
	                                  html[data-style="newspaper"] body { font-size: 1.0625rem; line-height: 1.65; font-kerning: normal; }
	                                  html[data-style="newspaper"] .site-header { background: var(--color-bg); border-bottom: 4px double var(--np-ink); backdrop-filter: none; }
	                                  html[data-style="newspaper"] .site-name { font-family: var(--np-head); font-size: 1.5rem; font-weight: 700; letter-spacing: -0.01em; }
	                                  html[data-style="newspaper"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: transparent; background: transparent; color: var(--color-text); }
	                                  html[data-style="newspaper"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { border-color: var(--np-ink); background: var(--np-hover); color: var(--color-text); }
	                                  html[data-style="newspaper"] .search-trigger { background: transparent; border-color: var(--np-rule); color: var(--color-text-secondary); }
	                                  html[data-style="newspaper"] .search-label { font-style: italic; }
	                                  html[data-style="newspaper"] .search-trigger kbd { background: transparent; border-color: var(--np-rule); }
	                                  html[data-style="newspaper"] :is(.appearance-panel, .version-dropdown) { border: 1px solid var(--np-ink); }
	                                  html[data-style="newspaper"] .appearance-head { padding-bottom: 0.375rem; border-bottom: 4px double var(--np-ink); }
	                                  html[data-style="newspaper"] .appearance-title { font-family: var(--np-head); font-size: 1.375rem; }
	                                  html[data-style="newspaper"] :is(.appearance-label, .appearance-group-label) { color: var(--np-accent); font-family: var(--np-sans); font-weight: 700; letter-spacing: 0.12em; }
	                                  html[data-style="newspaper"] .appearance-section { border-top-color: var(--color-border); }
	                                  html[data-style="newspaper"] :is(.appearance-option, .appearance-reset) { border-color: var(--color-border); }
	                                  html[data-style="newspaper"] .appearance-option[aria-checked="true"] { border-color: var(--np-ink); box-shadow: inset 0 0 0 1px var(--np-ink); color: var(--color-text); }
	                                  html[data-style="newspaper"] :is(.appearance-option, .version-option, .appearance-reset, .appearance-close):hover { background: var(--np-hover); color: var(--color-text); }
	                                  html[data-style="newspaper"] .appearance-style-desc { font-style: italic; }
	                                  html[data-style="newspaper"] .appearance-reset { font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .sidebar { border-right: 1px solid var(--np-rule); padding: 1.75rem 1.25rem; }
	                                  html[data-style="newspaper"] .nav-section { margin-bottom: 0.875rem; }
	                                  html[data-style="newspaper"] .nav-link {
	                                      padding: 0.25rem 0.5rem !important;
	                                      border-left: 0 !important;
	                                      color: var(--color-text-secondary);
	                                      font-size: 0.9375rem;
	                                      font-weight: 400;
	                                  }
	                                  html[data-style="newspaper"] .nav-link:hover { background: none; color: var(--color-text); text-decoration: underline; text-underline-offset: 0.2em; }
	                                  html[data-style="newspaper"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--np-accent); font-family: var(--np-sans); font-size: 0.75rem; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .sidebar .nav-link.current { background: none; font-weight: 700; }
	                                  html[data-style="newspaper"] .nav-section > .nav-header .nav-link.current { text-decoration: underline; text-decoration-thickness: 2px; text-underline-offset: 0.3em; }
	                                  html[data-style="newspaper"] .nav-chevron-btn { border-radius: var(--radius-sm); }
	                                  html[data-style="newspaper"] .nav-chevron-btn:hover { background: var(--np-hover); }
	                                  html[data-style="newspaper"] .nav-children { padding-left: 0.5rem; }
	                                  html[data-style="newspaper"] .content { padding: 2.5rem 3rem; }
	                                  html[data-style="newspaper"] .page-content :is(h1, h2, h3) { font-family: var(--np-head); }
	                                  html[data-style="newspaper"] .page-content h1 {
	                                      font-size: 2.75rem !important;
	                                      font-weight: 700 !important;
	                                      letter-spacing: -0.015em !important;
	                                      line-height: 1.08 !important;
	                                      margin-bottom: 1.25rem !important;
	                                      padding-bottom: 1rem;
	                                      border-bottom: 1px solid var(--np-ink);
	                                  }
	                                  html[data-style="newspaper"] .page-content h2 {
	                                      font-size: 1.75rem !important;
	                                      font-weight: 700;
	                                      letter-spacing: -0.01em;
	                                      line-height: 1.15;
	                                      margin: 2.5rem 0 0.875rem;
	                                      padding: 0.75rem 0 0;
	                                      border-bottom: none;
	                                      border-top: 2px solid var(--np-ink);
	                                  }
	                                  html[data-style="newspaper"] .page-content h3 { font-size: 1.3125rem !important; font-weight: 700; font-style: italic; letter-spacing: 0; }
	                                  html[data-style="newspaper"] .page-content h4 { color: var(--np-accent); font-family: var(--np-sans); font-size: 0.8125rem; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .page-content p { line-height: calc(1.7 * var(--line-spacing)); margin-bottom: calc(1rem * var(--paragraph-spacing)); }
	                                  html[data-style="newspaper"] .page-content li { line-height: calc(1.65 * var(--line-spacing)); }
	                                  /* The drop cap opens the article: the first paragraph straight under the title, never one in a list, a callout or on the
	                                     home page. flow-root keeps a one-line paragraph as tall as its drop cap. API pages start with a classed paragraph. */
	                                  html[data-style="newspaper"] .page-content:not(.home-docs) > h1 ~ p:not([class]):first-of-type { display: flow-root; }
	                                  html[data-style="newspaper"] .page-content:not(.home-docs) > h1 ~ p:not([class]):first-of-type::first-letter {
	                                      float: left;
	                                      margin: 0.05em 0.08em 0 0;
	                                      color: var(--np-accent);
	                                      font-family: var(--np-head);
	                                      font-size: 3.6em;
	                                      font-weight: 700;
	                                      line-height: 0.85;
	                                  }
	                                  html[data-style="newspaper"] .page-content a:not([class]) { text-decoration: underline; text-decoration-thickness: 1px; text-underline-offset: 0.18em; }
	                                  html[data-style="newspaper"] .page-content :not(pre) > code { background: var(--color-bg-code); border-color: transparent; border-radius: var(--radius-sm) !important; color: var(--color-text); font-weight: 400; }
	                                  html[data-style="newspaper"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="newspaper"] :is(.page-content, .landing-content) pre { border-radius: var(--radius); }
	                                  html[data-style="newspaper"] .page-content blockquote {
	                                      margin: 2rem 0;
	                                      padding: 0.25rem 0 0.25rem 1.25rem;
	                                      background: none;
	                                      border-left: 3px solid var(--np-accent);
	                                      border-radius: 0;
	                                      color: var(--color-text);
	                                      font-family: var(--np-head);
	                                      font-size: 1.25rem;
	                                      font-style: italic;
	                                      line-height: 1.45;
	                                  }
	                                  html[data-style="newspaper"] .page-content .admonition {
	                                      padding: 0.25rem 0 0.25rem 1.25rem;
	                                      background: none;
	                                      border: none;
	                                      border-left: 3px solid var(--st-c);
	                                      border-radius: 0;
	                                      font-style: italic;
	                                  }
	                                  html[data-style="newspaper"] .page-content .admonition-title { font-family: var(--np-sans); font-size: 0.75rem; font-style: normal; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .page-content .admonition :is(code, pre, table, .code-block) { font-style: normal; }
	                                  html[data-style="newspaper"] .page-content table { font-size: 0.9375rem; font-variant-numeric: lining-nums tabular-nums; }
	                                  html[data-style="newspaper"] .page-content :is(th, td) { padding: 0.5rem 1.25rem 0.5rem 0; border: none; border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="newspaper"] .page-content thead th { border-top: 2px solid var(--np-ink); border-bottom-color: var(--np-ink); }
	                                  html[data-style="newspaper"] .page-content th { background: none; font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .page-content tr:hover td { background: var(--np-hover); }
	                                  html[data-style="newspaper"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border: 1px solid var(--np-ink); border-radius: var(--radius); }
	                                  html[data-style="newspaper"] :is(.tab-headers, .install-tabs, .install-header) { background: none; border-bottom: 1px solid var(--np-ink); border-radius: 0; }
	                                  html[data-style="newspaper"] :is(.tab-header, .install-tab) { color: var(--color-text-secondary); font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] :is(.tab-header.active, .install-tab.active) { font-weight: 700; }
	                                  html[data-style="newspaper"] .source-viewer summary { font-style: italic; }
	                                  html[data-style="newspaper"] :is(.component-card, .component-link-card, .page-nav-link) { border: 1px solid var(--np-ink); border-radius: var(--radius); background: var(--color-bg); box-shadow: none; }
	                                  html[data-style="newspaper"] :is(.component-card-info, .component-card-success, .component-card-warning) { border-top-width: 4px; }
	                                  html[data-style="newspaper"] .component-card-info { border-top-color: #3b82f6; }
	                                  html[data-style="newspaper"] .component-card-success { border-top-color: #22c55e; }
	                                  html[data-style="newspaper"] .component-card-warning { border-top-color: #f59e0b; }
	                                  html[data-style="newspaper"] :is(.component-link-card, .page-nav-link):hover { background: var(--np-hover); border-color: var(--np-ink); box-shadow: none; transform: none; }
	                                  html[data-style="newspaper"] .component-card-title { font-family: var(--np-sans); font-size: 0.8125rem; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .component-step-number { border: 1px solid var(--np-ink); border-radius: var(--radius); background: var(--color-bg); color: var(--color-text); font-family: var(--np-head); font-weight: 700; }
	                                  html[data-style="newspaper"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--np-rule); }
	                                  html[data-style="newspaper"] .toc-sidebar { border-left: 1px solid var(--np-rule); }
	                                  html[data-style="newspaper"] .toc-title { color: var(--np-accent); font-family: var(--np-sans); font-size: 0.6875rem; letter-spacing: 0.14em; }
	                                  html[data-style="newspaper"] .toc-list { border-left: 1px solid var(--color-border); }
	                                  html[data-style="newspaper"] .toc-item a { color: var(--color-text-secondary); font-size: 0.875rem; border-left-width: 1px !important; margin-left: -1px; }
	                                  html[data-style="newspaper"] .toc-item a:hover { color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="newspaper"] .toc-item a.active { background: none; border-left-color: var(--np-ink) !important; font-weight: 700 !important; }
	                                  html[data-style="newspaper"] .toc-progress { height: 1px; border-radius: 0; background: var(--color-border); }
	                                  html[data-style="newspaper"] .toc-progress-bar { background: var(--np-ink); border-radius: 0; }
	                                  html[data-style="newspaper"] .toc-caret:hover { background: var(--np-hover); }
	                                  html[data-style="newspaper"] .toc-counter { border-top-color: var(--color-border); font-style: italic; }
	                                  html[data-style="newspaper"] .breadcrumbs { font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--np-rule); font-style: italic; }
	                                  html[data-style="newspaper"] .page-nav-label { color: var(--np-accent); font-family: var(--np-sans); font-size: 0.6875rem; letter-spacing: 0.12em; }
	                                  html[data-style="newspaper"] .page-nav-title { font-family: var(--np-head); font-size: 1.125rem; }
	                                  html[data-style="newspaper"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action) { border-radius: var(--radius); font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .feedback-prompt { font-style: italic; }
	                                  html[data-style="newspaper"] .site-footer { border-top: 4px double var(--np-ink); }
	                                  html[data-style="newspaper"] .footer-inner { color: var(--color-text-secondary); font-style: italic; }
	                                  html[data-style="newspaper"] .skip-link { border-width: 1px; border-radius: var(--radius); font-family: var(--np-sans); }
	                                  html[data-style="newspaper"] .search-backdrop { background: rgba(26, 24, 20, 0.45); backdrop-filter: none; }
	                                  html[data-style="newspaper"] .search-dialog { border: 1px solid var(--np-ink); border-radius: var(--radius); }
	                                  html[data-style="newspaper"] .search-input-wrap { border-bottom: 4px double var(--np-ink); }
	                                  html[data-style="newspaper"] .search-input { font-family: var(--np-head); font-size: 1.25rem; }
	                                  html[data-style="newspaper"] .search-input::placeholder,
	                                  html[data-style="newspaper"] .search-message { font-style: italic; }
	                                  html[data-style="newspaper"] :is(.search-esc, .search-close) { border-radius: var(--radius); }
	                                  html[data-style="newspaper"] .search-result { border-radius: 0; border-bottom: 1px solid var(--color-border-light); }
	                                  html[data-style="newspaper"] :is(.search-result:hover, .search-result.active) { background: var(--np-hover); }
	                                  html[data-style="newspaper"] .search-result-title { font-family: var(--np-head); font-size: 1.0625rem; }
	                                  html[data-style="newspaper"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="newspaper"] .back-to-top:hover { box-shadow: none !important; transform: none; }
	                                  html[data-style="newspaper"] .lightbox-close { border-radius: var(--radius); }
	                                  html[data-style="newspaper"] .landing::before,
	                                  html[data-style="newspaper"] .landing-hero::after { display: none; }
	                                  html[data-style="newspaper"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="newspaper"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="newspaper"] .landing-hero { border-bottom: 4px double var(--np-ink); }
	                                  html[data-style="newspaper"] .landing-hero-icon { animation: none; background: none; }
	                                  html[data-style="newspaper"] .landing-hero-title { font-family: var(--np-head); font-weight: 700; letter-spacing: -0.02em; line-height: 1.02; }
	                                  html[data-style="newspaper"] .landing-hero-subtitle { color: var(--color-text-secondary); font-style: italic; }
	                                  html[data-style="newspaper"] :is(.landing-features-title, .landing-md-content h2) { font-family: var(--np-head); }
	                                  html[data-style="newspaper"] .landing-btn { border-radius: var(--radius); min-width: 0; font-family: var(--np-sans); font-size: 0.8125rem; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="newspaper"] .landing-btn-secondary { background: none; border: 1px solid var(--np-ink); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="newspaper"] .landing-btn-secondary:hover { background: var(--np-hover); border-color: var(--np-ink); color: var(--color-text); }
	                                  html[data-style="newspaper"] .landing-feature-card { padding: 0.25rem 0 0.5rem 1.25rem; background: none; border: none; border-left: 1px solid var(--np-rule); border-radius: 0; }
	                                  html[data-style="newspaper"] .landing-feature-card::before { display: none; }
	                                  html[data-style="newspaper"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-left-color: var(--np-ink); }
	                                  html[data-style="newspaper"] .landing-feature-icon { width: auto; height: auto; justify-content: flex-start; background: none; color: var(--np-accent); }
	                                  html[data-style="newspaper"] .landing-feature-name { font-family: var(--np-head); font-size: 1.25rem; }
	                                  html[data-style="newspaper"] .landing .landing-content > ul > li { background: none; border: none; border-left: 1px solid var(--np-rule); border-radius: 0; }
	                                  html[data-style="newspaper"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; border-left-color: var(--np-ink); }
	                                  html[data-style="newspaper"] .landing-md-content table { border-color: transparent; }
	                                  html[data-style="newspaper"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="newspaper"] .landing-md-content th { background: none; font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .landing-footer { border-top: 4px double var(--np-ink); font-style: italic; }
	                                  html[data-style="newspaper"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: var(--radius); font-family: var(--np-sans); letter-spacing: 0.06em; }
	                                  html[data-style="newspaper"] .api-namespace { font-style: italic; }
	                                  html[data-style="newspaper"][data-navbar="floating"] .site-header { border: 1px solid var(--np-ink); border-bottom: 4px double var(--np-ink); border-radius: var(--radius); background: var(--color-bg); backdrop-filter: none; }
	                                  html[data-style="newspaper"][data-navbar="tabs"] .header-tabs { border-top: 1px solid var(--np-ink); }
	                                  html[data-style="newspaper"][data-navbar="tabs"] .header-tab { font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"]:is([data-footer="columns"], [data-footer="mega"]) .footer-col-title { color: var(--np-accent); font-family: var(--np-sans); letter-spacing: 0.12em; }
	                                  html[data-style="newspaper"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top-color: var(--np-rule); font-style: italic; }
	                                  html[data-style="newspaper"][data-toc="inline"] .toc-inline { background: none; border: none; border-top: 2px solid var(--np-ink); border-bottom: 1px solid var(--np-ink); border-radius: 0; }
	                                  html[data-style="newspaper"][data-toc="floating"] .toc-sidebar { border: 1px solid var(--np-ink); border-radius: var(--radius); }
	                                  html[data-style="newspaper"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border: 1px solid var(--color-border); border-radius: var(--radius); background: none; }
	                                  html[data-style="newspaper"][data-page-nav="minimal"] .page-nav-link { border: none; border-top: 2px solid var(--np-ink); background: none; }

	                                  /* Style: Retro. A classic desktop: a gray desktop, white windows in 2px bevels (light top and left, dark bottom and right when
	                                     raised, the other way round when sunk in), a navy title bar, beveled buttons that press in, Tahoma and square corners.
	                                     The bevels are border colors (--rt-raised, --rt-sunken), so pressing a button repaints no shadow. */
	                                  html[data-style="retro"] {
	                                      --color-bg: #ffffff;
	                                      --color-bg-secondary: #d4d0c8;
	                                      --color-bg-code: #f1efea;
	                                      --color-text: #000000;
	                                      --color-text-secondary: #1e1e1e;
	                                      --color-text-muted: #45433f;
	                                      --color-border: #808080;
	                                      --color-border-light: #c4c0b8;
	                                      --rt-hi: #ffffff;
	                                      --rt-shadow: #808080;
	                                      --rt-dark: #404040;
	                                      --rt-hover: #e6e3dd;
	                                      --rt-tip: #ffffe1;
	                                      --rt-title: #0a246a;
	                                      --rt-title-end: #3a6ea5;
	                                      --rt-on-title: #ffffff;
	                                      --rt-raised: var(--rt-hi) var(--rt-dark) var(--rt-dark) var(--rt-hi);
	                                      --rt-sunken: var(--rt-shadow) var(--rt-hi) var(--rt-hi) var(--rt-shadow);
	                                      --font-body: Tahoma, Verdana, 'Segoe UI', Geneva, sans-serif;
	                                      --font-mono: 'Lucida Console', Consolas, 'Courier New', monospace;
	                                      --st-hue: 50%;
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                      --transition: 0s;
	                                      --header-height: 46px;
	                                  }
	                                  html[data-style="retro"][data-theme="dark"] {
	                                      --dark-bg: #1c1c20;
	                                      --dark-bg-secondary: #2f2f35;
	                                      --dark-bg-code: #26262b;
	                                      --color-text: #f2f2f2;
	                                      --color-text-secondary: #d9d9dc;
	                                      --color-text-muted: #bdbdc3;
	                                      --color-border: #6a6a72;
	                                      --color-border-light: #3a3a41;
	                                      --rt-hi: #5d5d66;
	                                      --rt-shadow: #17171a;
	                                      --rt-dark: #000000;
	                                      --rt-hover: color-mix(in oklab, var(--color-text) 6%, var(--color-bg-secondary));
	                                      --rt-tip: color-mix(in srgb, #c8b400 14%, var(--color-bg));
	                                      --rt-title: #2a1d6b;
	                                      --rt-title-end: #5446b4;
	                                      --st-hue: 55%;
	                                  }
	                                  html[data-style="retro"] body { background: var(--color-bg-secondary); font-size: 0.9375rem; line-height: 1.6; }
	                                  html[data-style="retro"] :focus-visible { outline: 2px dotted var(--color-text); outline-offset: 2px; }
	                                  html[data-style="retro"] :is(.sidebar, .toc-sidebar, .tab-headers, .install-tabs, .header-tabs, .search-results, .version-dropdown) :focus-visible { outline-offset: -3px; }
	                                  html[data-style="retro"] .site-header { background: linear-gradient(90deg, var(--rt-title), var(--rt-title-end)); border-bottom: 1px solid var(--rt-dark); backdrop-filter: none; }
	                                  html[data-style="retro"] .site-header :focus-visible { outline-color: var(--rt-on-title); }
	                                  html[data-style="retro"] .site-header :is(.appearance-panel, .version-dropdown) :focus-visible { outline-color: var(--color-text); }
	                                  html[data-style="retro"] .site-header :is(.site-logo, .site-logo:hover, .site-logo-icon) { color: var(--rt-on-title); }
	                                  html[data-style="retro"] .site-name { font-size: 0.9375rem; font-weight: 700; letter-spacing: 0; }
	                                  html[data-style="retro"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border: 2px solid; border-color: var(--rt-raised); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):active,
	                                  html[data-style="retro"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] { border-color: var(--rt-sunken); background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] .search-trigger { border: 2px solid; border-color: var(--rt-sunken); background: var(--color-bg); color: var(--color-text-muted); }
	                                  html[data-style="retro"] .search-trigger:hover { border-color: var(--rt-sunken); color: var(--color-text); }
	                                  html[data-style="retro"] .search-trigger kbd { border-color: var(--rt-shadow); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.appearance-panel, .version-dropdown) { border: 2px solid; border-color: var(--rt-raised); background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="retro"] .appearance-head { margin: -0.75rem -1rem 0.75rem; padding: 0.1875rem 0.1875rem 0.1875rem 0.5rem; background: linear-gradient(90deg, var(--rt-title), var(--rt-title-end)); }
	                                  html[data-style="retro"] .appearance-title { color: var(--rt-on-title); font-size: 0.875rem; }
	                                  html[data-style="retro"] .appearance-close { width: 1.5rem; height: 1.5rem; border: 2px solid; border-color: var(--rt-raised); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] .appearance-close:hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] .appearance-close:active { border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .appearance-head .appearance-close:focus-visible { outline-color: var(--rt-on-title); }
	                                  html[data-style="retro"] :is(.appearance-label, .appearance-group-label) { color: var(--color-text); font-weight: 700; letter-spacing: 0; text-transform: none; }
	                                  html[data-style="retro"] .appearance-label { font-size: 0.8125rem; }
	                                  html[data-style="retro"] .appearance-section { border-top: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] :is(.appearance-option, .appearance-reset) { border: 2px solid; border-color: var(--rt-raised); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.appearance-option, .appearance-reset):hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.appearance-option, .appearance-reset):active,
	                                  html[data-style="retro"] .appearance-option[aria-checked="true"] { border-color: var(--rt-sunken); background: var(--rt-hover); box-shadow: none; color: var(--color-text); font-weight: 700; }
	                                  html[data-style="retro"] .appearance-style-desc { color: var(--color-text-secondary); }
	                                  html[data-style="retro"] .appearance-layout-field select { border: 2px solid; border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .version-option.active { color: var(--color-text); font-weight: 700; }
	                                  html[data-style="retro"] .version-option:hover { background: var(--st-link); color: var(--st-on-fill); }
	                                  html[data-style="retro"] .sidebar { border-right: 2px solid var(--rt-shadow); padding: 1rem 0.75rem; }
	                                  html[data-style="retro"] .nav-link {
	                                      padding: 0.1875rem 0.5rem !important;
	                                      border-left: 0 !important;
	                                      color: var(--color-text);
	                                      font-size: 0.8125rem;
	                                      font-weight: 400;
	                                      transition: none !important;
	                                  }
	                                  html[data-style="retro"] .nav-link:hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--color-text); font-size: 0.8125rem; font-weight: 700; }
	                                  html[data-style="retro"] .sidebar .nav-link.current { background: var(--st-link); color: var(--st-on-fill); }
	                                  html[data-style="retro"] .nav-section > .nav-header .nav-link.current { color: var(--st-on-fill); }
	                                  html[data-style="retro"] .sidebar .nav-link.current:focus-visible { outline-color: var(--st-on-fill); }
	                                  html[data-style="retro"] .nav-chevron-btn { border-radius: var(--radius-sm); color: var(--color-text); }
	                                  html[data-style="retro"] .nav-chevron-btn:hover { background: var(--rt-hover); }
	                                  html[data-style="retro"] .nav-children { margin-left: 0.75rem; padding-left: 0.375rem; border-left: 1px dotted var(--rt-shadow); }
	                                  html[data-style="retro"] .content { background: var(--color-bg); border: 2px solid; border-color: var(--rt-raised); }
	                                  html[data-style="retro"] .page-content h1 {
	                                      font-size: 1.75rem !important;
	                                      font-weight: 700 !important;
	                                      letter-spacing: 0 !important;
	                                      line-height: 1.25 !important;
	                                  }
	                                  html[data-style="retro"] .page-content h2 { font-size: 1.25rem !important; font-weight: 700; letter-spacing: 0; padding-bottom: 0.25rem; border-bottom: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] .page-content h3 { font-size: 1.0625rem !important; font-weight: 700; letter-spacing: 0; }
	                                  html[data-style="retro"] .page-content h4 { font-size: 0.9375rem; font-weight: 700; }
	                                  html[data-style="retro"] .page-content a:not([class]) { text-decoration: underline; text-underline-offset: 0.15em; }
	                                  html[data-style="retro"] .page-content :not(pre) > code { background: var(--color-bg-code); border-color: var(--color-border-light); border-radius: var(--radius-sm) !important; color: var(--color-text); font-weight: 400; }
	                                  html[data-style="retro"] .page-content h1 code { color: var(--color-text); }
	                                  html[data-style="retro"] :is(.page-content, .landing-content) pre { border: 2px solid; border-color: var(--rt-sunken); border-radius: var(--radius); }
	                                  html[data-style="retro"] .page-content :is(.tab-content, .source-viewer) pre { border-width: 0; }
	                                  html[data-style="retro"] .page-content table { font-size: 0.8125rem; }
	                                  html[data-style="retro"] .page-content :is(th, td) { padding: 0.3125rem 0.625rem; border: 1px solid var(--color-border-light); }
	                                  html[data-style="retro"] .page-content th { background: var(--color-bg-secondary); border: 2px solid; border-color: var(--rt-raised); color: var(--color-text); font-weight: 400; }
	                                  html[data-style="retro"] .page-content tr:hover td { background: var(--rt-hover); }
	                                  html[data-style="retro"] .page-content blockquote { background: var(--color-bg-secondary); border: 2px solid; border-color: var(--rt-sunken); border-radius: var(--radius); color: var(--color-text); }
	                                  html[data-style="retro"] .page-content .admonition { background: var(--color-bg-secondary); border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); }
	                                  html[data-style="retro"] .page-content .admonition-title { font-size: 0.875rem; }
	                                  /* A tip is a yellow tooltip. */
	                                  html[data-style="retro"] .page-content .admonition-tip { background: var(--rt-tip); border: 1px solid var(--color-text); }
	                                  html[data-style="retro"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="retro"] :is(.tab-headers, .install-tabs) { gap: 2px; padding: 0.25rem 0.25rem 0; background: var(--color-bg-secondary); border-bottom: 1px solid var(--rt-shadow); border-radius: 0; }
	                                  html[data-style="retro"] .install-header { background: var(--color-bg-secondary); border-bottom: 1px solid var(--rt-shadow); border-radius: 0; }
	                                  html[data-style="retro"] :is(.tab-header, .install-tab) { border: 2px solid; border-color: var(--rt-hi) var(--rt-dark) transparent var(--rt-hi); border-bottom-width: 0; background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.tab-header, .install-tab):hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.tab-header.active, .install-tab.active) { background: var(--color-bg); color: var(--color-text); font-weight: 700; }
	                                  html[data-style="retro"] :is(.tab-content, .install-panel) { background: var(--color-bg); }
	                                  html[data-style="retro"] .source-viewer summary { color: var(--color-text); font-weight: 700; }
	                                  html[data-style="retro"] .source-viewer[open] summary { border-bottom: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] :is(.component-card, .component-link-card, .page-nav-link) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="retro"] :is(.component-link-card, .page-nav-link):hover { background: var(--rt-hover); border-color: var(--rt-raised); box-shadow: none; transform: none; }
	                                  html[data-style="retro"] :is(.component-link-card, .page-nav-link):active { border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .component-step-number { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); }
	                                  html[data-style="retro"] .component-step:not(:last-child) .component-step-indicator::after { width: 1px; background: var(--rt-shadow); }
	                                  html[data-style="retro"] .toc-sidebar { border-left: none; }
	                                  html[data-style="retro"] .toc-title { color: var(--color-text); font-size: 0.8125rem; font-weight: 700; letter-spacing: 0; text-transform: none; }
	                                  html[data-style="retro"] .toc-list { border-left: 1px dotted var(--rt-shadow); }
	                                  html[data-style="retro"] .toc-item a { color: var(--color-text); font-size: 0.8125rem; border-left-width: 0 !important; margin-left: 0; }
	                                  html[data-style="retro"] .toc-item a:hover { background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] .toc-item a.active { background: var(--st-link); color: var(--st-on-fill) !important; font-weight: 400 !important; }
	                                  html[data-style="retro"] .toc-item a.active:focus-visible { outline-color: var(--st-on-fill); }
	                                  html[data-style="retro"] .toc-progress { height: 0.75rem; border: 2px solid; border-color: var(--rt-sunken); border-radius: 0; background: var(--color-bg); }
	                                  html[data-style="retro"] .toc-progress-bar { background: var(--st-link); border-radius: 0; }
	                                  html[data-style="retro"] .toc-caret:hover { background: var(--rt-hover); }
	                                  html[data-style="retro"] .toc-counter { border-top: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] .breadcrumbs { font-size: 0.8125rem; }
	                                  html[data-style="retro"] .breadcrumbs li:not(:last-child)::after { content: '\005C'; content: '\005C' / ''; opacity: 1; }
	                                  html[data-style="retro"] :is(.edit-link, .last-updated, .page-contributors) { border-top: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] .page-contributor-avatar { border: 2px solid; border-color: var(--rt-sunken); border-radius: var(--radius); }
	                                  html[data-style="retro"] .page-nav-label { color: var(--color-text-secondary); }
	                                  html[data-style="retro"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action, .search-esc, .search-close) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action, .search-esc, .search-close):hover { background: var(--rt-hover); border-color: var(--rt-raised); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.feedback-btn, .install-copy-btn, .page-actions .page-action, .search-esc, .search-close):active,
	                                  html[data-style="retro"] .feedback-btn.selected { border-color: var(--rt-sunken); background: var(--rt-hover); color: var(--color-text); }
	                                  html[data-style="retro"] .feedback-btn.selected { font-weight: 700; }
	                                  html[data-style="retro"] .site-footer { padding: 0.375rem 0.75rem; background: var(--color-bg-secondary); border-top: 2px solid var(--rt-hi); }
	                                  html[data-style="retro"] .footer-inner { padding: 0.125rem 0.5rem; border: 1px solid; border-color: var(--rt-sunken); color: var(--color-text); font-size: 0.75rem; }
	                                  html[data-style="retro"] .skip-link { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] .search-backdrop { background: rgba(0, 0, 0, 0.35); backdrop-filter: none; }
	                                  html[data-style="retro"] .search-dialog { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="retro"] .search-input-wrap { margin: 0.5rem; border: 2px solid; border-color: var(--rt-sunken); background: var(--color-bg); }
	                                  html[data-style="retro"] .search-result { border: 1px dotted transparent; border-radius: var(--radius); }
	                                  html[data-style="retro"] :is(.search-result:hover, .search-result.active) { background: var(--rt-hover); border-color: var(--color-text); }
	                                  html[data-style="retro"] :is(.back-to-top, .toc-toggle-btn) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="retro"] .back-to-top:hover { box-shadow: none !important; transform: none; }
	                                  html[data-style="retro"] :is(.back-to-top, .toc-toggle-btn):active { border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .lightbox-close { border-radius: var(--radius); }
	                                  html[data-style="retro"] .landing::before,
	                                  html[data-style="retro"] .landing-hero::after { display: none; }
	                                  html[data-style="retro"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="retro"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="retro"] .landing-hero {
	                                      max-width: calc(100% - 2rem);
	                                      margin: 1.5rem auto 0;
	                                      background: linear-gradient(90deg, var(--rt-title), var(--rt-title-end)) top / 100% 1.5rem no-repeat, var(--color-bg);
	                                      border: 2px solid;
	                                      border-color: var(--rt-raised);
	                                  }
	                                  html[data-style="retro"] .landing-hero-icon { animation: none; border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="retro"] .landing-hero-title { font-weight: 700; letter-spacing: -0.01em; }
	                                  html[data-style="retro"] .landing-hero-subtitle { color: var(--color-text-secondary); }
	                                  html[data-style="retro"] .landing-btn { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); min-width: 0; font-weight: 700; }
	                                  html[data-style="retro"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="retro"] .landing-btn-secondary { background: var(--color-bg-secondary); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="retro"] .landing-btn-secondary:hover { background: var(--rt-hover); border-color: var(--rt-raised); color: var(--color-text); }
	                                  html[data-style="retro"] .landing-btn:active { border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .landing-feature-card { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="retro"] .landing-feature-card::before { display: none; }
	                                  html[data-style="retro"] .landing-feature-card:hover { transform: none !important; box-shadow: none !important; border-color: var(--rt-raised); }
	                                  html[data-style="retro"] .landing-feature-icon { border: 2px solid; border-color: var(--rt-sunken); border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="retro"] .landing .landing-content > ul > li { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="retro"] .landing .landing-content > ul > li:hover { transform: none !important; box-shadow: none !important; }
	                                  html[data-style="retro"] .landing-md-content :is(th, td) { border: 1px solid var(--color-border-light); }
	                                  html[data-style="retro"] .landing-md-content th { background: var(--color-bg-secondary); }
	                                  html[data-style="retro"] .landing-footer { background: var(--color-bg-secondary); border-top: 2px solid var(--rt-hi); }
	                                  html[data-style="retro"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: var(--radius); }
	                                  html[data-style="retro"][data-navbar="floating"] .site-header { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: linear-gradient(90deg, var(--rt-title), var(--rt-title-end)); box-shadow: none; backdrop-filter: none; }
	                                  /* The title bar stays solid: a translucent one would put its white text on the page. */
	                                  html[data-style="retro"][data-navbar="transparent"] .site-header { background: linear-gradient(90deg, var(--rt-title), var(--rt-title-end)); -webkit-backdrop-filter: none; backdrop-filter: none; }
	                                  /* The tabs row is the window's menu bar. */
	                                  html[data-style="retro"][data-navbar="tabs"] .header-tabs { background: var(--color-bg-secondary); border-top: 1px solid var(--rt-hi); border-bottom: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"][data-navbar="tabs"] .header-tab { color: var(--color-text); font-size: 0.8125rem; }
	                                  html[data-style="retro"][data-navbar="tabs"] .header-tab:hover { background: var(--st-link); color: var(--st-on-fill); }
	                                  html[data-style="retro"][data-navbar="tabs"] .header-tab.current { font-weight: 700; }
	                                  html[data-style="retro"][data-navbar="tabs"] .header-tab:focus-visible { outline-color: var(--color-text); }
	                                  html[data-style="retro"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { border-top: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"][data-toc="inline"] .toc-inline { border: 2px solid; border-color: var(--rt-sunken); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="retro"][data-toc="floating"] .toc-sidebar { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="retro"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="retro"][data-page-nav="minimal"] .page-nav-link { border: none; border-top: 1px solid var(--rt-shadow); background: none; }

	                                  /* Style: Neon. Flat synthwave on a deep purple page. --ne-pink and --ne-cyan are the style's own two accents; links, focus rings
	                                     and filled buttons still follow the color preset through --st-link. No glows, no blur and no text shadows: the color does it.
	                                     The light shades are darker than the dark ones so they keep 4.5:1 on the lilac tints. */
	                                  html[data-style="neon"] {
	                                      --color-bg: #fbf6ff;
	                                      --color-bg-secondary: #f4eafd;
	                                      --color-bg-code: #f2e7fc;
	                                      --color-text: #21103a;
	                                      --color-text-secondary: #46305f;
	                                      --color-text-muted: #5b4677;
	                                      --color-border: #dcc6f1;
	                                      --color-border-light: #ebdcf8;
	                                      --ne-pink: #b3166f;
	                                      --ne-cyan: #08697b;
	                                      --ne-line: color-mix(in srgb, var(--ne-pink) 40%, var(--color-border));
	                                      --ne-hover: #efe1fb;
	                                      --ne-pink-tint: color-mix(in srgb, var(--ne-pink) 10%, var(--color-bg));
	                                      --radius: 4px;
	                                      --radius-sm: 2px;
	                                      --radius-md: 6px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                      --transition: 120ms ease;
	                                  }
	                                  html[data-style="neon"][data-theme="dark"] {
	                                      --dark-bg: #120b1f;
	                                      --dark-bg-secondary: #1a1030;
	                                      --dark-bg-code: #1c1233;
	                                      --color-text: #efe8ff;
	                                      --color-text-secondary: #cdc0ea;
	                                      --color-text-muted: #ab9dcc;
	                                      --color-border: #3d2a66;
	                                      --color-border-light: #2a1d47;
	                                      --ne-pink: #ff4fa3;
	                                      --ne-cyan: #3ee6ff;
	                                      --ne-hover: color-mix(in oklab, var(--color-border) 30%, var(--color-bg-secondary));
	                                  }
	                                  html[data-style="neon"] ::selection { background: color-mix(in srgb, var(--ne-pink) 30%, transparent); }
	                                  html[data-style="neon"] .site-header { background: var(--color-bg); border-bottom-color: var(--ne-line); backdrop-filter: none; }
	                                  html[data-style="neon"] .site-name { font-weight: 800; letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="neon"] .site-logo-icon { color: var(--ne-pink); }
	                                  html[data-style="neon"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: var(--color-border); background: transparent; }
	                                  html[data-style="neon"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger, .search-trigger):hover { border-color: var(--ne-pink); background: var(--ne-hover); color: var(--color-text); }
	                                  html[data-style="neon"] .search-trigger { border-color: var(--color-border); background: var(--color-bg-secondary); color: var(--color-text-muted); }
	                                  html[data-style="neon"] .search-trigger kbd { border-color: currentColor; background: transparent; color: var(--ne-cyan); }
	                                  html[data-style="neon"] :is(.appearance-panel, .version-dropdown) { border-color: var(--ne-line); box-shadow: none; }
	                                  html[data-style="neon"] .appearance-label { color: var(--ne-cyan); letter-spacing: 0.1em; }
	                                  html[data-style="neon"] .appearance-section { border-top-color: var(--color-border); }
	                                  html[data-style="neon"] :is(.appearance-option, .version-option, .appearance-reset, .appearance-close):hover { background: var(--ne-hover); color: var(--color-text); }
	                                  html[data-style="neon"] .appearance-option[aria-checked="true"] { background: var(--color-bg-secondary); }
	                                  html[data-style="neon"] .sidebar { border-right-color: var(--color-border); }
	                                  html[data-style="neon"] .nav-link { border-radius: var(--radius-sm); }
	                                  html[data-style="neon"] .nav-link:hover { background: var(--ne-hover); color: var(--color-text); }
	                                  html[data-style="neon"] .sidebar .nav-link.current {
	                                      border-inline-start: 2px solid var(--ne-pink) !important;
	                                      border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
	                                      background: var(--ne-pink-tint);
	                                      color: var(--ne-pink);
	                                      font-weight: 600;
	                                  }
	                                  html[data-style="neon"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { font-weight: 700; color: var(--color-text); }
	                                  html[data-style="neon"] .nav-group-label.parent-active { color: var(--ne-pink); }
	                                  html[data-style="neon"] .nav-chevron-btn:hover { background: var(--ne-hover); }
	                                  html[data-style="neon"] .nav-children { margin-inline-start: 0.75rem; padding-inline-start: 0.5rem; border-inline-start: 1px solid var(--color-border); }
	                                  html[data-style="neon"] .page-content h1 { font-weight: 800 !important; letter-spacing: -0.02em !important; }
	                                  html[data-style="neon"] .page-content h1::after { content: ''; display: block; width: 4rem; height: 2px; margin-top: 0.875rem; background: var(--ne-pink); }
	                                  html[data-style="neon"] .page-content h2 { border-bottom-color: var(--color-border); }
	                                  html[data-style="neon"] .page-content a:not([class]) { text-decoration: underline; text-decoration-color: color-mix(in srgb, var(--ne-pink) 55%, transparent); text-decoration-thickness: 1px; text-underline-offset: 0.2em; }
	                                  html[data-style="neon"] .page-content a:not([class]):hover { text-decoration-color: var(--ne-pink); }
	                                  html[data-style="neon"] .page-content :not(pre) > code { border: 1px solid color-mix(in srgb, var(--ne-cyan) 30%, transparent); border-radius: var(--radius-sm) !important; background: var(--color-bg-code); color: var(--ne-cyan); }
	                                  html[data-style="neon"] .page-content h1 code { color: var(--ne-cyan); -webkit-text-fill-color: currentColor; }
	                                  html[data-style="neon"] :is(.page-content, .landing-content) pre { border-color: var(--color-border); }
	                                  html[data-style="neon"] .page-content th { border-bottom: 1px solid var(--ne-pink); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="neon"] .page-content tr:hover td { background: var(--ne-hover); }
	                                  html[data-style="neon"] .page-content blockquote { border-left: 2px solid var(--ne-pink); background: var(--color-bg-secondary); }
	                                  html[data-style="neon"] .page-content .admonition {
	                                      border: 1px solid color-mix(in srgb, var(--st-c) 50%, transparent);
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 8%, var(--color-bg));
	                                  }
	                                  html[data-style="neon"] .page-content .admonition-tip { --st-c: var(--ne-cyan); }
	                                  html[data-style="neon"] .page-content .admonition-tip .admonition-title { color: var(--ne-cyan); }
	                                  html[data-style="neon"] .page-content .admonition-title { letter-spacing: 0.06em; text-transform: uppercase; font-size: 0.8125rem; }
	                                  html[data-style="neon"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border-color: var(--color-border); }
	                                  html[data-style="neon"] :is(.tab-headers, .install-tabs) { background: var(--color-bg-secondary); }
	                                  html[data-style="neon"] :is(.tab-header.active, .install-tab.active) { color: var(--color-text); border-bottom-color: var(--ne-pink); }
	                                  html[data-style="neon"] :is(.component-card, .component-link-card, .page-nav-link) { border-color: var(--color-border); box-shadow: none; }
	                                  html[data-style="neon"] :is(.component-card, .component-link-card, .page-nav-link):hover { border-color: var(--ne-pink); background: var(--color-bg); box-shadow: none; }
	                                  html[data-style="neon"] .page-nav-link:hover .page-nav-arrow { color: var(--ne-pink); }
	                                  html[data-style="neon"] .toc-sidebar { border-left-color: var(--color-border); }
	                                  html[data-style="neon"] .toc-title { color: var(--ne-cyan); letter-spacing: 0.1em; }
	                                  html[data-style="neon"] .toc-list { border-left-color: var(--color-border); }
	                                  html[data-style="neon"] .toc-item a.active { color: var(--ne-cyan) !important; border-left-color: var(--ne-cyan) !important; background: none; }
	                                  html[data-style="neon"] .toc-progress { background: var(--color-border); }
	                                  html[data-style="neon"] .toc-progress-bar { background: linear-gradient(90deg, var(--ne-pink), var(--ne-cyan)); }
	                                  html[data-style="neon"] .toc-caret:hover { background: var(--ne-hover); }
	                                  html[data-style="neon"] .breadcrumbs li:not(:last-child)::after { color: var(--ne-pink); opacity: 1; }
	                                  html[data-style="neon"] :is(.feedback-btn, .page-actions .page-action) { border-color: var(--color-border); border-radius: var(--radius-sm); }
	                                  html[data-style="neon"] .page-actions .page-action:hover { border-color: var(--ne-pink); color: var(--color-text); }
	                                  html[data-style="neon"] .site-footer { border-top-color: var(--ne-line); }
	                                  html[data-style="neon"] .skip-link { border-color: var(--ne-pink); }
	                                  html[data-style="neon"] .search-backdrop { background: color-mix(in srgb, #0a0414 72%, transparent); backdrop-filter: none; }
	                                  html[data-style="neon"] .search-dialog { border-color: var(--ne-line); box-shadow: none; }
	                                  html[data-style="neon"] .search-input-wrap { border-bottom-color: var(--color-border); }
	                                  html[data-style="neon"] .search-esc { border-color: currentColor; background: transparent; color: var(--ne-cyan); }
	                                  html[data-style="neon"] .search-result { box-shadow: inset 2px 0 0 transparent; }
	                                  html[data-style="neon"] :is(.search-result:hover, .search-result.active) { background: var(--ne-hover); box-shadow: inset 2px 0 0 var(--ne-pink); }
	                                  html[data-style="neon"] .search-result-snippet mark { background: color-mix(in srgb, var(--ne-pink) 22%, transparent); }
	                                  html[data-style="neon"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius-md); box-shadow: none; }
	                                  html[data-style="neon"] .landing::before,
	                                  html[data-style="neon"] .landing-hero::after { display: none; }
	                                  html[data-style="neon"] .landing-hero::before { background: linear-gradient(transparent 55%, var(--ne-pink-tint)) !important; animation: none; }
	                                  html[data-style="neon"] .landing-hero { border-bottom: 1px solid var(--ne-line); }
	                                  html[data-style="neon"] .landing-hero .hero-grid-pattern {
	                                      background-image: linear-gradient(color-mix(in srgb, var(--ne-pink) 22%, transparent) 1px, transparent 1px), linear-gradient(90deg, color-mix(in srgb, var(--ne-pink) 22%, transparent) 1px, transparent 1px);
	                                      background-size: 48px 48px;
	                                      background-position: center bottom;
	                                      opacity: 1;
	                                      -webkit-mask-image: linear-gradient(transparent 45%, #000000);
	                                      mask-image: linear-gradient(transparent 45%, #000000);
	                                  }
	                                  html[data-style="neon"] .landing-hero-icon { animation: none; border: 1px solid var(--ne-pink); background: var(--color-bg); color: var(--ne-pink); }
	                                  html[data-style="neon"] .landing-hero-title { font-weight: 800; letter-spacing: -0.03em; }
	                                  html[data-style="neon"] .landing-btn { border-radius: var(--radius); box-shadow: none; }
	                                  html[data-style="neon"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="neon"] .landing-btn-secondary { border-color: var(--ne-cyan); background: var(--color-bg); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="neon"] .landing-btn-secondary:hover { border-color: var(--ne-pink); color: var(--color-text); }
	                                  html[data-style="neon"] .landing-feature-card { border-color: var(--color-border); background: var(--color-bg); }
	                                  html[data-style="neon"] .landing-feature-card::before { background: linear-gradient(90deg, var(--ne-pink), var(--ne-cyan)); }
	                                  html[data-style="neon"] .landing-feature-card:hover { border-color: var(--ne-pink); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="neon"] .landing-feature-icon { border: 1px solid currentColor; background: transparent; color: var(--ne-pink); }
	                                  html[data-style="neon"] .landing .landing-content > ul > li { border-color: var(--color-border); box-shadow: none; }
	                                  html[data-style="neon"] .landing .landing-content > ul > li:hover { border-color: var(--ne-pink); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="neon"] .landing-md-content th { border-bottom: 1px solid var(--ne-pink); }
	                                  html[data-style="neon"] .landing-footer { border-top-color: var(--ne-line); }
	                                  html[data-style="neon"] .landing-footer-heart { color: var(--ne-pink); }
	                                  html[data-style="neon"] :is(.api-badge, .api-badge-sm, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border: 1px solid color-mix(in srgb, currentColor 45%, transparent); background: transparent; }
	                                  html[data-style="neon"][data-navbar="floating"] .site-header { border: 1px solid var(--ne-line); background: var(--color-bg); box-shadow: none; backdrop-filter: none; }
	                                  html[data-style="neon"][data-navbar="tabs"] .header-tab.current { color: var(--color-text); border-bottom-color: var(--ne-pink); }
	                                  html[data-style="neon"][data-toc="inline"] .toc-inline { border-color: var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="neon"][data-toc="floating"] .toc-sidebar { border-color: var(--ne-line); box-shadow: none; }
	                                  html[data-style="neon"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border-color: var(--color-border); background: transparent; }
	                                  html[data-style="neon"][data-breadcrumbs="pills"] .breadcrumbs [aria-current="page"] { border-color: var(--ne-pink); }

	                                  /* Style: Tonal. Surfaces step up in tone instead of casting shadows or drawing lines, and every tint comes from the color
	                                     preset. --tn-container is the tinted pill (the current sidebar entry, the search field, tips) under --tn-on-container text,
	                                     --tn-raised the tone of anything that floats or is hovered. Pills (--tn-round) and the large shapes are multiples of --radius,
	                                     so square corners make them square too. */
	                                  html[data-style="tonal"] {
	                                      --color-bg: color-mix(in srgb, var(--color-primary) 2%, #fefbff);
	                                      --color-bg-secondary: color-mix(in srgb, var(--color-primary) 4%, #f6f2f8);
	                                      --color-bg-code: color-mix(in srgb, var(--color-primary) 4%, #f6f2f8);
	                                      --color-text: #1d1b20;
	                                      --color-text-secondary: #49454f;
	                                      --color-text-muted: #5c5866;
	                                      --color-border: color-mix(in srgb, var(--color-primary) 8%, #cac4d0);
	                                      --color-border-light: color-mix(in srgb, var(--color-primary) 6%, #e7e0ec);
	                                      --tn-container: color-mix(in srgb, var(--color-primary) 17%, #ffffff);
	                                      --tn-on-container: color-mix(in srgb, var(--color-primary) 14%, #1d1b20);
	                                      --tn-raised: color-mix(in srgb, var(--color-primary) 6%, #ede8f1);
	                                      --tn-round: calc(var(--radius) * 999);
	                                      --radius: 14px;
	                                      --radius-sm: 8px;
	                                      --radius-md: 14px;
	                                      --shadow-sm: none;
	                                      --shadow-md: none;
	                                      --shadow-lg: none;
	                                      --transition: 200ms cubic-bezier(0.2, 0, 0, 1);
	                                      --header-height: 64px;
	                                  }
	                                  html[data-style="tonal"][data-theme="dark"] {
	                                      --dark-bg: color-mix(in srgb, var(--color-primary) 2%, #131117);
	                                      --dark-bg-secondary: color-mix(in srgb, var(--color-primary) 4%, #1f1d24);
	                                      --dark-bg-code: color-mix(in srgb, var(--color-primary) 4%, #1f1d24);
	                                      --color-text: #e6e0e9;
	                                      --color-text-secondary: #cac4d0;
	                                      --color-text-muted: #aca7b3;
	                                      --color-border: color-mix(in srgb, var(--color-primary) 8%, #49454f);
	                                      --color-border-light: color-mix(in srgb, var(--color-primary) 6%, #36343b);
	                                      --tn-container: color-mix(in srgb, var(--color-primary) 16%, color-mix(in oklab, var(--color-text) 11%, var(--color-bg-secondary)));
	                                      --tn-on-container: color-mix(in srgb, var(--color-primary) 10%, #ece6f0);
	                                      --tn-raised: color-mix(in srgb, var(--color-primary) 5%, color-mix(in oklab, var(--color-text) 7%, var(--color-bg-secondary)));
	                                      --st-hue: 55%;
	                                      --st-on-fill: color-mix(in srgb, var(--color-primary) 30%, #0b0b0f);
	                                  }
	                                  html[data-style="tonal"] body { line-height: 1.7; }
	                                  html[data-style="tonal"] .site-header { background: var(--color-bg); border-bottom-color: transparent; backdrop-filter: none; }
	                                  html[data-style="tonal"] .site-name { font-weight: 500; letter-spacing: 0; }
	                                  html[data-style="tonal"] .header-actions { gap: 0.25rem; }
	                                  html[data-style="tonal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: transparent; border-radius: var(--tn-round); background: transparent; }
	                                  html[data-style="tonal"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):hover { background: var(--tn-raised); color: var(--color-text); }
	                                  html[data-style="tonal"] :is(.appearance-trigger, .version-trigger)[aria-expanded="true"] { border-color: transparent; background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .search-trigger { padding: 0.5rem 0.625rem 0.5rem 1rem; border-color: transparent; border-radius: var(--tn-round); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .search-trigger:hover { border-color: transparent; background: color-mix(in srgb, var(--tn-on-container) 8%, var(--tn-container)); }
	                                  html[data-style="tonal"] .search-trigger kbd { border-color: transparent; border-radius: var(--tn-round); background: var(--color-bg); color: var(--color-text); }
	                                  html[data-style="tonal"] :is(.appearance-panel, .version-dropdown) { border-color: transparent; border-radius: calc(var(--radius) * 2); background: var(--tn-raised); }
	                                  html[data-style="tonal"] .version-dropdown { border-radius: var(--radius); padding: 0.5rem; }
	                                  html[data-style="tonal"] .appearance-panel { padding: 1rem 1.25rem 1.25rem; }
	                                  html[data-style="tonal"] .appearance-section { border-top-color: var(--color-border-light); }
	                                  html[data-style="tonal"] .appearance-label { color: var(--color-text-secondary); letter-spacing: 0.02em; text-transform: none; font-size: 0.8125rem; }
	                                  html[data-style="tonal"] :is(.appearance-option, .appearance-reset) { border-color: transparent; background: var(--color-bg); }
	                                  html[data-style="tonal"] :is(.appearance-segments .appearance-option, .appearance-reset, .appearance-close) { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] :is(.appearance-option, .appearance-reset, .version-option):hover { background: color-mix(in srgb, var(--color-text) 8%, var(--color-bg)); color: var(--color-text); }
	                                  html[data-style="tonal"] .version-option { border-radius: var(--radius-sm); }
	                                  html[data-style="tonal"] .appearance-close:hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); }
	                                  html[data-style="tonal"] :is(.appearance-option[aria-checked="true"], .version-option.active) { border-color: transparent; background: var(--tn-container); box-shadow: none; color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .appearance-style[aria-checked="true"] .appearance-style-name { color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .version-badge-default { background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .sidebar { border-right: none; padding: 1rem 0.75rem; }
	                                  html[data-style="tonal"] .nav-section { margin-bottom: 0.375rem; }
	                                  html[data-style="tonal"] .nav-link {
	                                      min-height: 2.5rem;
	                                      padding: 0.375rem 1rem !important;
	                                      border-left: 0 !important;
	                                      border-radius: var(--tn-round);
	                                      color: var(--color-text-secondary);
	                                      transition: background var(--transition), color var(--transition) !important;
	                                  }
	                                  html[data-style="tonal"] .nav-link:hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); color: var(--color-text); }
	                                  html[data-style="tonal"] .sidebar .nav-link.current { background: var(--tn-container); color: var(--tn-on-container); font-weight: 600; }
	                                  html[data-style="tonal"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--color-text); font-weight: 600; }
	                                  html[data-style="tonal"] .nav-group-label.parent-active { color: var(--color-text); }
	                                  html[data-style="tonal"] .nav-chevron-btn { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .nav-chevron-btn:hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); }
	                                  html[data-style="tonal"] .nav-children { padding-inline-start: 0.75rem; }
	                                  html[data-style="tonal"] .content { padding: 2.5rem 3rem; }
	                                  html[data-style="tonal"] .page-content h1 { font-size: 2.5rem !important; font-weight: 500 !important; letter-spacing: -0.01em !important; line-height: 1.2 !important; }
	                                  html[data-style="tonal"] .page-content h2 { font-size: 1.625rem !important; font-weight: 500; letter-spacing: 0; border-bottom: none; padding-bottom: 0; margin: 2.75rem 0 1rem; }
	                                  html[data-style="tonal"] .page-content :is(h3, h4) { font-weight: 500; letter-spacing: 0; }
	                                  html[data-style="tonal"] .page-content :not(pre) > code { border-color: transparent; border-radius: var(--radius-sm) !important; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] :is(.page-content, .landing-content) pre { border-color: transparent; border-radius: var(--radius); }
	                                  html[data-style="tonal"] .page-content :is(.install-panel, .source-viewer, .tab-content, .repl-container, .blazor-preview-source) pre { border-radius: 0; }
	                                  html[data-style="tonal"] .page-content table {
	                                      width: fit-content;
	                                      max-width: 100%;
	                                      border: none;
	                                      border-radius: var(--radius);
	                                      border-collapse: separate;
	                                      border-spacing: 0;
	                                      background: var(--color-bg-secondary);
	                                  }
	                                  html[data-style="tonal"] .page-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-bg); padding: 0.75rem 1rem; }
	                                  html[data-style="tonal"] .page-content tr:last-child td { border-bottom: none; }
	                                  html[data-style="tonal"] .page-content th { background: var(--tn-raised); font-weight: 600; }
	                                  html[data-style="tonal"] .page-content tr:hover td { background: var(--tn-raised); }
	                                  html[data-style="tonal"] .page-content blockquote { border: none; border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .page-content .admonition {
	                                      border: none;
	                                      border-radius: var(--radius);
	                                      background: color-mix(in srgb, var(--st-c) 12%, var(--color-bg));
	                                      padding: 1.125rem 1.375rem;
	                                  }
	                                  html[data-style="tonal"] .page-content .admonition-tip { background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .page-content .admonition-tip .admonition-title { color: var(--tn-on-container); }
	                                  html[data-style="tonal"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) {
	                                      border-color: transparent;
	                                      border-radius: var(--radius);
	                                      background: var(--color-bg-secondary);
	                                      overflow: hidden;
	                                  }
	                                  html[data-style="tonal"] :is(.tab-headers, .install-tabs) { gap: 0.25rem; padding: 0.375rem; border-bottom: none; background: transparent; }
	                                  html[data-style="tonal"] :is(.tab-header, .install-tab) { margin-bottom: 0; padding: 0.375rem 1rem; border-bottom: none; border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] :is(.tab-header, .install-tab):hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); }
	                                  html[data-style="tonal"] :is(.tab-header.active, .install-tab.active) { background: var(--tn-container); color: var(--tn-on-container); font-weight: 600; }
	                                  html[data-style="tonal"] .install-header { border-bottom-color: var(--color-bg); }
	                                  html[data-style="tonal"] :is(.install-copy-btn, .install-nuget-link) { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .source-viewer[open] summary { border-bottom-color: var(--color-bg); }
	                                  html[data-style="tonal"] :is(.component-card, .component-link-card, .page-nav-link) { border-color: transparent; border-radius: var(--radius); background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="tonal"] :is(.component-card, .component-link-card, .page-nav-link):hover { border-color: transparent; background: var(--tn-raised); box-shadow: none; }
	                                  html[data-style="tonal"] :is(.component-card-info, .component-card-success, .component-card-warning) { border: 1px solid transparent; }
	                                  html[data-style="tonal"] .component-card-info { background: color-mix(in srgb, #3b82f6 12%, var(--color-bg)); }
	                                  html[data-style="tonal"] .component-card-success { background: color-mix(in srgb, #22c55e 12%, var(--color-bg)); }
	                                  html[data-style="tonal"] .component-card-warning { background: color-mix(in srgb, #f59e0b 14%, var(--color-bg)); }
	                                  html[data-style="tonal"] .component-step-number { background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .component-step:not(:last-child) .component-step-indicator::after { background: var(--tn-container); }
	                                  html[data-style="tonal"] .toc-sidebar { border-left: none; }
	                                  html[data-style="tonal"] .toc-title { color: var(--color-text); letter-spacing: 0.02em; text-transform: none; font-size: 0.875rem; }
	                                  html[data-style="tonal"] .toc-list { margin-left: 0.5rem; border-left: none; }
	                                  html[data-style="tonal"] .toc-item a { margin-left: 0; padding: 0.3125rem 0.875rem; border-left: 0 !important; border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .toc-item a:hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); }
	                                  html[data-style="tonal"] .toc-item a.active { background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .toc-level-3 a { margin-left: 0.75rem; }
	                                  html[data-style="tonal"] .toc-level-4 a { margin-left: 1.5rem; }
	                                  html[data-style="tonal"] .toc-level-5 a { margin-left: 2.25rem; }
	                                  html[data-style="tonal"] .toc-level-6 a { margin-left: 3rem; }
	                                  html[data-style="tonal"] .toc-progress { height: 4px; border-radius: var(--tn-round); background: var(--tn-container); }
	                                  html[data-style="tonal"] .toc-progress-bar { border-radius: var(--tn-round); background: var(--st-link); }
	                                  html[data-style="tonal"] .toc-caret { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .toc-caret:hover { background: color-mix(in srgb, var(--color-text) 8%, transparent); }
	                                  html[data-style="tonal"] .toc-counter { border-top: none; }
	                                  html[data-style="tonal"] :is(.edit-link, .last-updated, .page-contributors) { border-top-color: var(--color-border-light); }
	                                  html[data-style="tonal"] .page-contributor-avatar { border-color: transparent; background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .page-nav-link { padding: 1.125rem 1.375rem; }
	                                  html[data-style="tonal"] :is(.feedback-btn, .page-actions .page-action) { border-color: var(--color-border); border-radius: var(--tn-round); padding-left: 1rem; padding-right: 1rem; }
	                                  html[data-style="tonal"] .page-actions .page-action:hover { border-color: var(--color-border); background: color-mix(in srgb, var(--color-text) 8%, transparent); color: var(--color-text); }
	                                  html[data-style="tonal"] .site-footer { border-top: none; border-radius: calc(var(--radius) * 2) calc(var(--radius) * 2) 0 0; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .skip-link { border: none; border-radius: var(--tn-round); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .search-backdrop { background: rgba(0, 0, 0, 0.4); backdrop-filter: none; }
	                                  html[data-style="tonal"] .search-dialog { border-color: transparent; border-radius: calc(1.75rem * var(--radius-scale)); background: var(--tn-raised); box-shadow: none; }
	                                  html[data-style="tonal"] .search-input-wrap { padding: 1rem 1.25rem; border-bottom-color: var(--color-border); }
	                                  html[data-style="tonal"] .search-esc { border-color: transparent; border-radius: var(--tn-round); background: var(--color-bg); }
	                                  html[data-style="tonal"] .search-close { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .search-result { border-radius: var(--radius); }
	                                  html[data-style="tonal"] :is(.search-result:hover, .search-result.active) { background: var(--color-bg); }
	                                  html[data-style="tonal"] .search-result-snippet mark { background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius); background: var(--tn-container); color: var(--tn-on-container); box-shadow: none; }
	                                  html[data-style="tonal"] .back-to-top:hover { background: color-mix(in srgb, var(--tn-on-container) 8%, var(--tn-container)) !important; box-shadow: none; }
	                                  html[data-style="tonal"] .lightbox-close { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"] .landing::before,
	                                  html[data-style="tonal"] .landing-hero::after { display: none; }
	                                  html[data-style="tonal"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="tonal"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="tonal"] .landing-hero { margin: 0.75rem 1rem 0; border-radius: calc(var(--radius) * 2); background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .landing-hero-icon { animation: none; border-radius: var(--radius); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .landing-hero-title { font-weight: 500; letter-spacing: -0.02em; }
	                                  html[data-style="tonal"] .landing-btn { border-radius: var(--tn-round); box-shadow: none; }
	                                  html[data-style="tonal"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="tonal"] .landing-btn-secondary { border: none; background: var(--tn-container); color: var(--tn-on-container); backdrop-filter: none; }
	                                  html[data-style="tonal"] .landing-btn-secondary:hover { background: color-mix(in srgb, var(--tn-on-container) 8%, var(--tn-container)); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .landing-feature-card { border-color: transparent; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .landing-feature-card::before { display: none; }
	                                  html[data-style="tonal"] .landing-feature-card:hover { border-color: transparent; background: var(--tn-raised); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="tonal"] .landing-feature-icon { border-radius: var(--radius-sm); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .landing-features-title { font-weight: 500; }
	                                  html[data-style="tonal"] .landing-feature-name { font-weight: 600; }
	                                  html[data-style="tonal"] .landing .landing-content > ul > li { border-color: transparent; background: var(--color-bg-secondary); box-shadow: none; }
	                                  html[data-style="tonal"] .landing .landing-content > ul > li:hover { background: var(--tn-raised); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="tonal"] .landing-md-content table { border: none; border-collapse: separate; border-spacing: 0; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .landing-md-content :is(th, td) { border: none; border-bottom: 1px solid var(--color-bg); }
	                                  html[data-style="tonal"] .landing-md-content th { background: var(--tn-raised); }
	                                  html[data-style="tonal"] .landing-footer { border-top: none; border-radius: calc(var(--radius) * 2) calc(var(--radius) * 2) 0 0; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label) { border-radius: var(--tn-round); }
	                                  html[data-style="tonal"][data-navbar="floating"] .site-header { border-color: transparent; border-radius: var(--tn-round); background: var(--tn-raised); box-shadow: none; backdrop-filter: none; }
	                                  html[data-style="tonal"][data-navbar="tabs"] .header-tabs { border-top-color: transparent; }
	                                  html[data-style="tonal"][data-navbar="tabs"] .header-tab.current { border-radius: var(--tn-round); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"][data-toc="inline"] .toc-inline { border-color: transparent; border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"][data-toc="floating"] .toc-sidebar { border-color: transparent; border-radius: var(--radius); background: var(--tn-raised); box-shadow: none; }
	                                  html[data-style="tonal"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border-color: transparent; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"][data-breadcrumbs="pills"] .breadcrumbs [aria-current="page"] { background: var(--tn-container); color: var(--tn-on-container); }
	                                  @media (max-width: 1200px) {
	                                      html[data-style="tonal"] .toc-sidebar { border-radius: var(--radius) 0 0 var(--radius); background: var(--tn-raised); }
	                                  }

	                                  /* Style: Editor. A code editor's window: the sidebar and "On this page" are side panels (--color-bg-secondary), the
	                                     breadcrumbs are the open file's tab, content tabs are editor tabs, --ed-select is a selected row and the footer is the status
	                                     bar. --ed-bar fills the status bar with the accent mixed dark enough for --ed-on-bar (white) in both modes. */
	                                  html[data-style="editor"] {
	                                      --color-bg: #ffffff;
	                                      --color-bg-secondary: #f3f3f3;
	                                      --color-bg-code: #f5f5f5;
	                                      --color-text: #1f1f1f;
	                                      --color-text-secondary: #3b3b3b;
	                                      --color-text-muted: #616161;
	                                      --color-border: #e5e5e5;
	                                      --color-border-light: #ededed;
	                                      --ed-hover: #e8e8e8;
	                                      --ed-select: color-mix(in srgb, var(--color-primary) 16%, var(--color-bg-secondary));
	                                      --ed-select-line: color-mix(in srgb, var(--st-link) 55%, transparent);
	                                      --ed-bar: color-mix(in srgb, var(--color-primary) 56%, #000000);
	                                      --ed-on-bar: #ffffff;
	                                      --font-body: 'Segoe UI', system-ui, -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif;
	                                      --font-mono: 'Cascadia Code', 'JetBrains Mono', Consolas, 'SF Mono', Menlo, 'Liberation Mono', monospace;
	                                      --radius: 0px;
	                                      --radius-sm: 0px;
	                                      --radius-md: 0px;
	                                      --shadow-sm: none;
	                                      --shadow-md: 0 2px 8px rgba(0, 0, 0, 0.14);
	                                      --shadow-lg: 0 4px 16px rgba(0, 0, 0, 0.18);
	                                      --transition: 80ms linear;
	                                      --header-height: 48px;
	                                      --header-button: 32px;
	                                      --sidebar-width: 264px;
	                                  }
	                                  html[data-style="editor"][data-theme="dark"] {
	                                      --dark-bg: #1e1e1e;
	                                      --dark-bg-secondary: #252526;
	                                      --dark-bg-code: #252526;
	                                      --color-text: #d4d4d4;
	                                      --color-text-secondary: #cccccc;
	                                      --color-text-muted: #9d9d9d;
	                                      --color-border: #333333;
	                                      --color-border-light: #2b2b2b;
	                                      --ed-hover: color-mix(in oklab, var(--color-text) 5%, var(--color-bg-secondary));
	                                      --ed-select: color-mix(in srgb, var(--color-primary) 28%, var(--color-bg-secondary));
	                                      --shadow-md: 0 2px 8px rgba(0, 0, 0, 0.5);
	                                      --shadow-lg: 0 4px 16px rgba(0, 0, 0, 0.6);
	                                  }
	                                  html[data-style="editor"] :is(.sidebar, .toc-sidebar, .tab-headers, .install-tabs, .breadcrumbs, .search-results) :focus-visible { outline-offset: -2px; }
	                                  html[data-style="editor"] .site-header { background: var(--color-bg-secondary); border-bottom-color: var(--color-border); backdrop-filter: none; }
	                                  html[data-style="editor"] .site-name { font-size: 0.9375rem; font-weight: 600; letter-spacing: 0; }
	                                  html[data-style="editor"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger) { border-color: transparent; background: transparent; }
	                                  html[data-style="editor"] :is(.theme-toggle, .mobile-nav-toggle, .appearance-trigger, .version-trigger):hover { border-color: transparent; background: var(--ed-hover); color: var(--color-text); }
	                                  html[data-style="editor"] .search-trigger { min-width: 16rem; padding: 0.3125rem 0.625rem; border-color: var(--color-border); background: var(--color-bg); font-size: 0.8125rem; }
	                                  html[data-style="editor"] .search-trigger:hover { border-color: var(--color-text-muted); }
	                                  html[data-style="editor"] .search-trigger kbd { margin-inline-start: auto; border-color: var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] :is(.appearance-panel, .version-dropdown) { border-color: var(--color-border); box-shadow: var(--shadow-lg); }
	                                  html[data-style="editor"] .appearance-label { font-size: 0.6875rem; font-weight: 700; color: var(--color-text-secondary); }
	                                  html[data-style="editor"] .appearance-section { border-top-color: var(--color-border); }
	                                  html[data-style="editor"] :is(.appearance-option, .version-option, .appearance-reset, .appearance-close):hover { background: var(--ed-hover); color: var(--color-text); }
	                                  html[data-style="editor"] :is(.appearance-option[aria-checked="true"], .version-option.active) { background: var(--ed-select); box-shadow: inset 0 0 0 1px var(--ed-select-line); color: var(--color-text); }
	                                  html[data-style="editor"] .appearance-option[aria-checked="true"] { border-color: transparent; }
	                                  html[data-style="editor"] .appearance-style[aria-checked="true"] .appearance-style-name { color: var(--color-text); }
	                                  html[data-style="editor"] .sidebar { padding: 0.5rem 0 1rem; border-right-color: var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .nav-section { margin-bottom: 0.25rem; }
	                                  html[data-style="editor"] .nav-link {
	                                      padding-block: 0.1875rem !important;
	                                      padding-inline: 1rem 0.75rem !important;
	                                      border-left: 0 !important;
	                                      color: var(--color-text-secondary);
	                                      font-size: 0.8125rem;
	                                      font-weight: 400;
	                                      line-height: 1.6;
	                                      transition: none !important;
	                                  }
	                                  html[data-style="editor"] .nav-link:hover { background: var(--ed-hover); color: var(--color-text); }
	                                  html[data-style="editor"] .sidebar .nav-children { padding-inline-start: 0; }
	                                  html[data-style="editor"] .nav-children .nav-link { padding-inline-start: 1.75rem !important; }
	                                  html[data-style="editor"] .nav-children :where(.nav-children) .nav-link { padding-inline-start: 2.5rem !important; }
	                                  html[data-style="editor"] .sidebar .nav-link.current { background: var(--ed-select); box-shadow: inset 0 0 0 1px var(--ed-select-line); color: var(--color-text); font-weight: 400; }
	                                  html[data-style="editor"] .nav-section > .nav-header :is(.nav-link, .nav-group-label) { color: var(--color-text); font-size: 0.6875rem; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="editor"] .nav-group-label.parent-active { color: var(--color-text); }
	                                  html[data-style="editor"] .nav-chevron-btn { border-radius: var(--radius-sm); color: var(--color-text-secondary); }
	                                  html[data-style="editor"] .nav-chevron-btn:hover { background: var(--ed-hover); }
	                                  html[data-style="editor"] .page-content h1 { font-size: 2rem !important; font-weight: 600 !important; letter-spacing: -0.01em !important; }
	                                  html[data-style="editor"] .page-content h2 { font-size: 1.4375rem !important; font-weight: 600; letter-spacing: 0; }
	                                  html[data-style="editor"] .page-content :is(h3, h4) { font-weight: 600; letter-spacing: 0; }
	                                  html[data-style="editor"] .page-content :not(pre) > code { border-color: var(--color-border); border-radius: var(--radius-sm) !important; background: var(--color-bg-code); }
	                                  html[data-style="editor"] :is(.page-content, .landing-content) pre { border-color: var(--color-border); }
	                                  html[data-style="editor"] .page-content pre :is(.code-lang, .copy-btn) { border-radius: var(--radius-sm) !important; }
	                                  html[data-style="editor"] .breadcrumbs { display: flex; margin-bottom: 1.75rem; border-bottom: 1px solid var(--color-border); background: var(--color-bg-secondary); font-size: 0.8125rem; }
	                                  html[data-style="editor"] .breadcrumbs ol {
	                                      min-width: 0;
	                                      margin-bottom: -1px;
	                                      padding: 0.4375rem 1rem 0.5rem;
	                                      border-top: 2px solid var(--st-link);
	                                      border-right: 1px solid var(--color-border);
	                                      background: var(--color-bg);
	                                  }
	                                  html[data-style="editor"] .breadcrumbs [aria-current="page"] { font-weight: 600; }
	                                  html[data-style="editor"] .page-content th { background: var(--color-bg-secondary); font-size: 0.8125rem; }
	                                  html[data-style="editor"] .page-content tr:hover td { background: var(--ed-hover); }
	                                  html[data-style="editor"] .page-content blockquote { border-left: 3px solid color-mix(in srgb, var(--st-link) 50%, transparent); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .page-content .admonition {
	                                      padding: 0;
	                                      border: 1px solid var(--color-border);
	                                      border-top: 2px solid var(--st-c);
	                                      background: var(--color-bg);
	                                  }
	                                  html[data-style="editor"] .page-content .admonition-title {
	                                      margin: 0;
	                                      padding: 0.375rem 0.875rem;
	                                      border-bottom: 1px solid var(--color-border);
	                                      background: color-mix(in srgb, var(--st-c) 7%, var(--color-bg-secondary));
	                                      font-size: 0.8125rem;
	                                  }
	                                  html[data-style="editor"] .page-content .admonition-content { padding: 0.75rem 0.875rem; }
	                                  html[data-style="editor"] :is(.tabs, .package-install-widget, .source-viewer, .repl-container, .blazor-preview-container, .openapi-endpoint-card) { border-color: var(--color-border); border-radius: var(--radius); }
	                                  html[data-style="editor"] :is(.tab-headers, .install-tabs) { border-bottom: none; background: var(--color-bg-secondary); box-shadow: inset 0 -1px 0 var(--color-border); }
	                                  html[data-style="editor"] :is(.tab-header, .install-tab) { padding: 0.4375rem 1rem; border-top: 2px solid transparent; border-right: 1px solid var(--color-border); border-bottom: none; font-size: 0.8125rem; }
	                                  html[data-style="editor"] :is(.tab-header, .install-tab):hover { color: var(--color-text); }
	                                  html[data-style="editor"] :is(.tab-header.active, .install-tab.active) { border-top-color: var(--st-link); background: var(--color-bg); color: var(--color-text); }
	                                  html[data-style="editor"] .install-header { border-bottom-color: var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] :is(.component-card, .component-link-card, .page-nav-link) { border-color: var(--color-border); box-shadow: none; }
	                                  html[data-style="editor"] :is(.component-card, .component-link-card, .page-nav-link):hover { border-color: var(--st-link); background: var(--color-bg); box-shadow: none; }
	                                  html[data-style="editor"] .component-step-number { border-radius: var(--radius-sm); }
	                                  html[data-style="editor"] .toc-sidebar { padding: 0.5rem 0 1rem; border-left-color: var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .toc-title { margin-bottom: 0.25rem; padding: 0.375rem 1rem; font-size: 0.6875rem; font-weight: 700; letter-spacing: 0.04em; color: var(--color-text); }
	                                  html[data-style="editor"] .toc-list { margin-left: 0; border-left: none; }
	                                  html[data-style="editor"] .toc-item a { margin-left: 0; padding: 0.1875rem 1rem; border-left: 0 !important; color: var(--color-text-secondary); font-size: 0.8125rem; transition: none !important; }
	                                  html[data-style="editor"] .toc-level-3 a { padding-inline-start: 1.75rem; }
	                                  html[data-style="editor"] .toc-level-4 a { padding-inline-start: 2.5rem; }
	                                  html[data-style="editor"] .toc-level-5 a { padding-inline-start: 3.25rem; }
	                                  html[data-style="editor"] .toc-level-6 a { padding-inline-start: 4rem; }
	                                  html[data-style="editor"] .toc-item a:hover { background: var(--ed-hover); color: var(--color-text); }
	                                  html[data-style="editor"] .toc-item a.active { background: var(--ed-select); color: var(--color-text) !important; }
	                                  html[data-style="editor"] .toc-progress { height: 2px; border-radius: var(--radius-sm); background: var(--color-border); }
	                                  html[data-style="editor"] .toc-progress-bar { border-radius: var(--radius-sm); background: var(--st-link); }
	                                  html[data-style="editor"] .toc-caret { border-radius: var(--radius-sm); }
	                                  html[data-style="editor"] .toc-caret:hover { background: var(--ed-hover); }
	                                  html[data-style="editor"] :is(.feedback-btn, .install-copy-btn, .install-nuget-link, .page-actions .page-action) { border-radius: var(--radius-sm); }
	                                  html[data-style="editor"] .page-actions .page-action { background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .page-actions .page-action:hover { background: var(--ed-hover); }
	                                  html[data-style="editor"] .site-footer { margin-bottom: var(--fab-clear); padding: 0.3125rem 1rem; border-top: none; background: var(--ed-bar); }
	                                  html[data-style="editor"] .site-footer::after { height: 0; }
	                                  html[data-style="editor"] :is(.footer-inner, .footer-bottom) { align-items: center; color: var(--ed-on-bar); font-size: 0.75rem; }
	                                  html[data-style="editor"] :is(.footer-inner, .footer-bottom) a,
	                                  html[data-style="editor"] :is(.footer-inner, .footer-bottom) a:hover { color: var(--ed-on-bar); }
	                                  html[data-style="editor"] :is(.footer-inner, .footer-bottom) a:hover { text-decoration: underline; }
	                                  html[data-style="editor"] :is(.footer-inner, .footer-bottom) :focus-visible { outline-color: var(--ed-on-bar); }
	                                  html[data-style="editor"] .skip-link { border-width: 1px; }
	                                  html[data-style="editor"] .search-backdrop { background: rgba(0, 0, 0, 0.3); backdrop-filter: none; }
	                                  html[data-style="editor"] .search-modal { padding-top: calc(var(--header-offset) + 0.5rem); }
	                                  html[data-style="editor"] .search-dialog { max-width: 600px; border-color: var(--color-border); box-shadow: var(--shadow-lg); }
	                                  html[data-style="editor"] .search-input-wrap { margin: 0.375rem; padding: 0.375rem 0.625rem; border: 1px solid var(--st-link); }
	                                  html[data-style="editor"] .search-input { font-size: 0.875rem; }
	                                  html[data-style="editor"] .search-results { padding: 0 0 0.375rem; }
	                                  html[data-style="editor"] html[data-style="editor"] :is(.search-result:hover, .search-result.active) { background: var(--ed-select); }
	                                  html[data-style="editor"] :is(.search-result:hover, .search-result.active) :is(.search-result-category, .search-result-snippet) { color: var(--color-text-secondary); }
	                                  html[data-style="editor"] .search-result-snippet mark { background: none; color: var(--color-text); font-weight: 700; text-decoration: underline; text-underline-offset: 0.15em; }
	                                  html[data-style="editor"] :is(.back-to-top, .toc-toggle-btn) { border-radius: var(--radius-sm); box-shadow: var(--shadow-md); }
	                                  html[data-style="editor"] .lightbox-close { border-radius: var(--radius-sm); }
	                                  html[data-style="editor"] .landing::before,
	                                  html[data-style="editor"] .landing-hero::after { display: none; }
	                                  html[data-style="editor"] .landing-hero::before { background: none !important; animation: none; }
	                                  html[data-style="editor"] .landing-hero .hero-grid-pattern { display: none; }
	                                  html[data-style="editor"] .landing-hero { border-bottom: 1px solid var(--color-border); }
	                                  html[data-style="editor"] .landing-hero-icon { animation: none; border: 1px solid var(--color-border); border-radius: var(--radius); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .landing-hero-title { font-weight: 600; letter-spacing: -0.02em; }
	                                  html[data-style="editor"] .landing-btn { min-width: 0; font-size: 0.9375rem; box-shadow: none; }
	                                  html[data-style="editor"] :is(.landing-btn-primary, .landing-btn-primary:hover, .landing-btn-secondary:hover) { transform: none; box-shadow: none; }
	                                  html[data-style="editor"] .landing-btn-secondary { border-color: var(--color-border); background: var(--color-bg-secondary); color: var(--color-text); backdrop-filter: none; }
	                                  html[data-style="editor"] .landing-btn-secondary:hover { border-color: var(--st-link); background: var(--ed-hover); color: var(--color-text); }
	                                  html[data-style="editor"] .landing-feature-card { border-color: var(--color-border); }
	                                  html[data-style="editor"] .landing-feature-card::before { height: 2px; background: var(--st-link); }
	                                  html[data-style="editor"] .landing-feature-card:hover { border-color: var(--color-border); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="editor"] .landing-feature-icon { border-radius: var(--radius-sm); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .landing .landing-content > ul > li { box-shadow: none; }
	                                  html[data-style="editor"] .landing .landing-content > ul > li:hover { border-color: var(--st-link); box-shadow: none !important; transform: none !important; }
	                                  html[data-style="editor"] .landing-footer { display: flex; flex-wrap: wrap; justify-content: center; gap: 0 1.5rem; padding: 0.375rem 1rem; border-top: none; background: var(--ed-bar); color: var(--ed-on-bar); font-size: 0.75rem; }
	                                  html[data-style="editor"] .landing-footer :is(a, a:hover, .landing-footer-heart) { color: var(--ed-on-bar); }
	                                  html[data-style="editor"] .landing-footer :focus-visible { outline-color: var(--ed-on-bar); }
	                                  html[data-style="editor"] .landing-footer p + p { margin-top: 0 !important; }
	                                  html[data-style="editor"] :is(.api-badge, .api-badge-sm, .version-badge, .openapi-method, .openapi-status, .openapi-required, .openapi-optional, .openapi-auth-label, .page-contributor-avatar) { border-radius: var(--radius-sm); }
	                                  html[data-style="editor"][data-navbar="floating"] .site-header { border-color: var(--color-border); border-radius: var(--radius); background: var(--color-bg-secondary); box-shadow: var(--shadow-md); backdrop-filter: none; }
	                                  html[data-style="editor"][data-navbar="tabs"] .header-tabs { border-top-color: var(--color-border); }
	                                  html[data-style="editor"][data-navbar="tabs"] .header-tab { border-top: 2px solid transparent; }
	                                  html[data-style="editor"][data-navbar="tabs"] .header-tab.current { border-top-color: var(--st-link); border-bottom-color: transparent; background: var(--color-bg); color: var(--color-text); }
	                                  html[data-style="editor"][data-footer="minimal"] .site-footer,
	                                  html[data-style="editor"][data-footer="compact"] .site-footer { padding-top: 0.3125rem; padding-bottom: 0.3125rem; }
	                                  html[data-style="editor"][data-footer="centered"] .site-footer { padding-top: 0.625rem; padding-bottom: 0.625rem; }
	                                  html[data-style="editor"][data-footer="centered"] .footer-inner { gap: 0.375rem; }
	                                  html[data-style="editor"][data-footer="centered"] .landing-footer { flex-direction: column; align-items: center; padding-top: 0.625rem; padding-bottom: 0.625rem; }
	                                  html[data-style="editor"]:is([data-footer="columns"], [data-footer="mega"]) .site-footer { padding-bottom: 0; border-top: 1px solid var(--color-border); background: var(--color-bg-secondary); }
	                                  html[data-style="editor"]:is([data-footer="columns"], [data-footer="mega"]) .footer-bottom { max-width: none; margin: 2.5rem -1.5rem 0; padding: 0.3125rem 1.5rem; border-top: none; background: var(--ed-bar); }
	                                  html[data-style="editor"][data-toc="inline"] .toc-inline { background: var(--color-bg-secondary); }
	                                  html[data-style="editor"][data-toc="floating"] .toc-sidebar { border-color: var(--color-border); border-radius: var(--radius); box-shadow: var(--shadow-lg); }
	                                  html[data-style="editor"][data-breadcrumbs="pills"] .breadcrumbs li > :is(a, span) { border-radius: var(--radius-sm); }
	                                  @media (max-width: 768px) {
	                                      html[data-style="editor"] .search-trigger { min-width: 0; }
	                                  }

	                                  /* Styles: announcement banner. The link and the accent fill use the style's accent shade, the warning icon is mixed
	                                     like the other semantic colors, and each style gives the bar its own edge and type. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-banner[data-variant="info"] { --banner-link: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-banner[data-variant="warning"] { --banner-icon: color-mix(in srgb, #f59e0b var(--st-hue), var(--st-ink)); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-banner[data-variant="accent"] { --banner-bg: var(--st-link); --banner-text: var(--st-on-fill); --banner-link: var(--st-on-fill); --banner-border: var(--st-link); }
	                                  /* The styles' focus rings are drawn in the accent shade, which the accent bar is filled with. */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .site-banner :focus-visible { outline: 2px solid currentColor; outline-offset: 2px; box-shadow: none; }
	                                  html[data-style="moka"] .site-banner { font-size: 0.75rem; border-bottom-color: var(--mk-border-visible); }
	                                  html[data-style="moka"] .site-banner[data-variant="info"] { --banner-bg: var(--mk-surface); box-shadow: inset 0 0 20px var(--mk-glow); }
	                                  html[data-style="moka"] .site-banner-link { font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.08em; }
	                                  html[data-style="minimal"] .site-banner { border-bottom-color: var(--color-border-light); }
	                                  html[data-style="minimal"] .site-banner[data-variant="info"] { --banner-bg: color-mix(in srgb, var(--color-primary) 5%, var(--color-bg)); }
	                                  html[data-style="minimal"] .site-banner-link { font-weight: 500; }
	                                  html[data-style="classic"] .site-banner { font-size: 0.8125rem; border-bottom-color: var(--color-border); }
	                                  html[data-style="soft"] .site-banner { margin: 0.5rem 0.75rem 0; border: none; border-radius: var(--radius); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .site-banner[data-variant="info"] { --banner-bg: var(--sf-tint); }
	                                  html[data-style="soft"] .site-banner-close { border-radius: calc(999px * var(--radius-scale)); }
	                                  html[data-style="terminal"] .site-banner { font-family: var(--font-mono); font-size: 0.8125rem; border-bottom-style: dashed; border-bottom-color: var(--color-border); }
	                                  html[data-style="terminal"] .site-banner-close { border-radius: var(--radius-sm); }
	                                  html[data-style="paper"] .site-banner { border-bottom-color: var(--pp-rule); font-style: italic; }
	                                  html[data-style="paper"] .site-banner-link { font-style: normal; font-variant: small-caps; letter-spacing: 0.04em; }
	                                  html[data-style="contrast"] .site-banner { border-bottom: 2px solid var(--color-text); }
	                                  html[data-style="contrast"] .site-banner[data-variant="info"] { --banner-bg: var(--color-bg); }
	                                  html[data-style="contrast"] .site-banner-link { text-decoration-thickness: 2px; }
	                                  html[data-style="contrast"] .site-banner-close { border: 2px solid currentColor; }
	                                  html[data-style="glass"] .site-banner { border-bottom-color: var(--gl-edge); }
	                                  html[data-style="brutal"] .site-banner { border-bottom: 3px solid var(--bt-ink); font-weight: 600; }
	                                  html[data-style="brutal"] .site-banner[data-variant="info"] { --banner-bg: var(--bt-fill); }
	                                  html[data-style="brutal"] .site-banner-link { text-transform: uppercase; letter-spacing: 0.04em; }
	                                  html[data-style="brutal"] .site-banner-close { border: 2px solid currentColor; border-radius: var(--radius-sm); }
	                                  html[data-style="blueprint"] .site-banner { border-bottom: 1px dashed var(--color-border); font-family: var(--font-mono); font-size: 0.8125rem; }
	                                  html[data-style="blueprint"] .site-banner[data-variant="info"] { --banner-bg: var(--bp-hover); }
	                                  html[data-style="blueprint"] .site-banner-link { letter-spacing: 0.04em; text-transform: uppercase; }
	                                  html[data-style="blueprint"] .site-banner-close { border-radius: var(--radius); }
	                                  html[data-style="newspaper"] .site-banner { border-bottom: 1px solid var(--np-ink); font-style: italic; }
	                                  html[data-style="newspaper"] .site-banner-link { font-family: var(--np-sans); font-size: 0.75rem; font-style: normal; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .site-banner-close { border-radius: var(--radius); }
	                                  html[data-style="retro"] .site-banner { border-bottom: 1px solid var(--rt-shadow); }
	                                  html[data-style="retro"] .site-banner[data-variant="info"] { --banner-bg: var(--rt-tip); }
	                                  html[data-style="retro"] .site-banner-close { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] .site-banner-close:active { border-color: var(--rt-sunken); }
	                                  html[data-style="neon"] .site-banner { border-bottom-color: var(--ne-line); }
	                                  html[data-style="neon"] .site-banner[data-variant="info"] { --banner-bg: var(--color-bg-secondary); }
	                                  html[data-style="neon"] .site-banner-link { letter-spacing: 0.06em; text-transform: uppercase; }
	                                  html[data-style="neon"] .site-banner-close { border-radius: var(--radius-sm); }
	                                  html[data-style="tonal"] .site-banner { margin: 0.5rem 0.75rem 0; border: none; border-radius: var(--radius); }
	                                  html[data-style="tonal"] .site-banner[data-variant="info"] { --banner-bg: var(--tn-container); --banner-text: var(--tn-on-container); }
	                                  html[data-style="tonal"] .site-banner-close { border-radius: var(--tn-round); }
	                                  html[data-style="editor"] .site-banner { border-bottom-color: var(--color-border); font-size: 0.8125rem; }
	                                  html[data-style="editor"] .site-banner[data-variant="info"] { --banner-bg: var(--color-bg-secondary); }
	                                  html[data-style="editor"] .site-banner-close { border-radius: var(--radius-sm); }

	                                  /* Styles: blog */
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) { --blog-link: var(--st-link); }
	                                  html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .blog-card:hover { box-shadow: none; }
	                                  html[data-style="moka"] .blog-card { background: var(--mk-surface); border-color: var(--mk-border-visible); border-radius: var(--radius-md); padding: 1rem 1.25rem; }
	                                  html[data-style="moka"] .blog-card:hover { border-color: var(--mk-border-strong); box-shadow: inset 0 0 20px var(--mk-glow), 0 0 12px var(--mk-glow); }
	                                  html[data-style="moka"] .page-content .blog-card .blog-card-title { font-size: 1.0625rem !important; font-weight: 600; letter-spacing: -0.01em; }
	                                  html[data-style="moka"] .blog-meta,
	                                  html[data-style="moka"] .blog-authors { font-size: 0.6875rem; }
	                                  html[data-style="moka"] .blog-meta time,
	                                  html[data-style="moka"] .blog-reading-time,
	                                  html[data-style="moka"] .blog-pager-status,
	                                  html[data-style="moka"] .blog-tag-count { font-family: var(--font-mono); letter-spacing: 0.02em; }
	                                  html[data-style="moka"] .blog-tag { border-radius: var(--radius-sm); border-color: var(--mk-border-visible); background: var(--mk-glow); font-family: var(--font-mono); font-size: 0.6875rem; }
	                                  html[data-style="moka"] .blog-tag:hover { border-color: var(--mk-border-strong); background: var(--mk-glow-md); }
	                                  html[data-style="moka"] .blog-read-more,
	                                  html[data-style="moka"] .blog-pager-link,
	                                  html[data-style="moka"] .blog-draft { font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.08em; }
	                                  html[data-style="moka"] .blog-draft { border-radius: var(--radius-sm); border-color: var(--mk-border-strong); }
	                                  html[data-style="moka"] .blog-pager-link { border-color: var(--mk-border-visible); background: transparent; }
	                                  html[data-style="moka"] .blog-pager-link:hover { border-color: var(--mk-border-strong); background: var(--mk-glow); }
	                                  html[data-style="moka"] .blog-pager-status { font-size: 0.6875rem; }
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
	                                  html[data-style="classic"] .blog-tag { padding: 0.0625rem 0.625rem; border-color: transparent; border-radius: calc(2em * var(--radius-scale)); background: color-mix(in srgb, var(--color-primary) 10%, var(--color-bg)); font-size: 0.75rem; }
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
	                                  html[data-style="soft"] .blog-pager-link { background: var(--sf-surface); border-color: transparent; border-radius: calc(999px * var(--radius-scale)); box-shadow: var(--shadow-sm); }
	                                  html[data-style="soft"] .blog-pager-link:hover { background: var(--sf-surface); border-color: transparent; box-shadow: var(--shadow-md); }
	                                  html[data-style="soft"] .blog-post-header { border-bottom: none; padding-bottom: 0.5rem; }
	                                  html[data-style="soft"] .page-content .blog-cover { border-radius: var(--radius); }
	                                  html[data-style="terminal"] .blog-card { border-radius: var(--radius); }
	                                  html[data-style="terminal"] .blog-card:hover { border-color: var(--st-link); }
	                                  html[data-style="terminal"] .page-content .blog-card .blog-card-title { font-size: 1.0625rem !important; }
	                                  html[data-style="terminal"] :is(.blog-tag, .blog-pager-link, .blog-draft) { border-radius: var(--radius-sm); font-size: 0.75rem; }
	                                  html[data-style="terminal"] .blog-tag::before { content: '#'; content: '#' / ''; }
	                                  html[data-style="paper"] .blog-list { gap: 0; }
	                                  html[data-style="paper"] .blog-card { padding: 1.75rem 0; background: none; border: none; border-top: 1px solid var(--pp-rule); border-radius: 0; }
	                                  html[data-style="paper"] .page-content .blog-card .blog-card-title { font-size: 1.5rem !important; font-weight: 600; font-style: italic; }
	                                  html[data-style="paper"] :is(.blog-meta, .blog-authors) { font-style: italic; }
	                                  html[data-style="paper"] .blog-tag { background: none; border-color: transparent; font-variant-caps: all-small-caps; letter-spacing: 0.05em; font-size: 0.9375rem; }
	                                  html[data-style="paper"] .blog-tag:hover { background: none; border-color: transparent; text-decoration: underline; }
	                                  html[data-style="paper"] .blog-pager-link { background: none; border-color: transparent; font-style: italic; }
	                                  html[data-style="paper"] .blog-pager-link:hover { background: none; border-color: transparent; text-decoration: underline; }
	                                  html[data-style="paper"] .blog-post-header { border-bottom-color: var(--pp-rule); }
	                                  html[data-style="contrast"] .blog-card { border: 2px solid var(--color-border); }
	                                  html[data-style="contrast"] .blog-card:hover { border-color: var(--color-text); }
	                                  html[data-style="contrast"] :is(.blog-tag, .blog-pager-link) { border: 2px solid var(--color-border); color: var(--color-text); text-decoration: underline; }
	                                  html[data-style="contrast"] :is(.blog-tag, .blog-pager-link):hover { border-color: var(--color-text); background: var(--hc-hover); }
	                                  html[data-style="contrast"] :is(.blog-meta, .blog-authors, .blog-reading-time) { color: var(--color-text-secondary); }
	                                  html[data-style="glass"] .blog-card { background: var(--gl-panel); border-color: var(--gl-edge); border-radius: var(--radius-md); box-shadow: var(--shadow-md); -webkit-backdrop-filter: var(--gl-blur); backdrop-filter: var(--gl-blur); }
	                                  html[data-style="glass"] .blog-card:hover { background: var(--gl-panel-strong); box-shadow: var(--shadow-lg); }
	                                  html[data-style="glass"] :is(.blog-tag, .blog-pager-link) { background: var(--gl-panel); border-color: var(--gl-edge); }
	                                  html[data-style="glass"] :is(.blog-tag, .blog-pager-link):hover { background: var(--gl-panel-strong); }
	                                  html[data-style="brutal"] .blog-card { border: 3px solid var(--bt-ink); box-shadow: var(--shadow-md); }
	                                  html[data-style="brutal"] .blog-card:hover { border-color: var(--bt-ink); box-shadow: var(--shadow-lg); transform: translate(-2px, -2px); }
	                                  html[data-style="brutal"] .page-content .blog-card .blog-card-title { font-weight: 900 !important; }
	                                  html[data-style="brutal"] :is(.blog-tag, .blog-pager-link, .blog-draft) { border: 2px solid var(--bt-ink); color: var(--color-text); font-weight: 700; }
	                                  html[data-style="brutal"] :is(.blog-tag, .blog-pager-link):hover { background: var(--bt-fill); border-color: var(--bt-ink); }
	                                  html[data-style="brutal"] .blog-post-header { border-bottom: 3px solid var(--bt-ink); }
	                                  html[data-style="blueprint"] .blog-card { border: 1px dashed var(--color-border); border-radius: var(--radius); background: var(--color-bg); }
	                                  html[data-style="blueprint"] .blog-card:hover { border-style: solid; border-color: var(--bp-ink); }
	                                  html[data-style="blueprint"] .page-content .blog-card .blog-card-title { font-family: var(--font-mono); font-size: 1.125rem !important; }
	                                  html[data-style="blueprint"] :is(.blog-meta, .blog-authors, .blog-reading-time, .blog-pager-status) { font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="blueprint"] :is(.blog-tag, .blog-pager-link, .blog-draft) { border: 1px dashed var(--color-border); border-radius: var(--radius); font-family: var(--font-mono); font-size: 0.75rem; }
	                                  html[data-style="blueprint"] :is(.blog-tag, .blog-pager-link):hover { border-style: solid; border-color: var(--bp-ink); background: var(--bp-hover); }
	                                  html[data-style="blueprint"] .blog-post-header { border-bottom: 2px solid var(--bp-ink); }
	                                  html[data-style="newspaper"] .blog-list { gap: 0; }
	                                  html[data-style="newspaper"] .blog-card { padding: 1.5rem 0; background: none; border: none; border-top: 1px solid var(--np-ink); border-radius: 0; }
	                                  html[data-style="newspaper"] .blog-card:first-child { border-top-width: 2px; }
	                                  html[data-style="newspaper"] .page-content .blog-card .blog-card-title { font-family: var(--np-head); font-size: 1.625rem !important; font-weight: 700; line-height: 1.15; }
	                                  html[data-style="newspaper"] :is(.blog-meta, .blog-authors) { font-style: italic; }
	                                  html[data-style="newspaper"] .blog-tag { background: none; border-color: transparent; color: var(--np-accent); font-family: var(--np-sans); font-size: 0.6875rem; letter-spacing: 0.1em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .blog-tag:hover { background: none; border-color: transparent; text-decoration: underline; }
	                                  html[data-style="newspaper"] .blog-pager-link { background: none; border-color: var(--np-ink); border-radius: var(--radius); font-family: var(--np-sans); font-size: 0.75rem; letter-spacing: 0.08em; text-transform: uppercase; }
	                                  html[data-style="newspaper"] .blog-pager-link:hover { background: var(--np-hover); }
	                                  html[data-style="newspaper"] .blog-post-header { border-bottom: 4px double var(--np-ink); }
	                                  html[data-style="retro"] .blog-card { background: var(--color-bg-secondary); border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); }
	                                  html[data-style="retro"] .blog-card:hover { background: var(--rt-hover); border-color: var(--rt-raised); }
	                                  html[data-style="retro"] :is(.blog-tag, .blog-pager-link, .blog-draft) { border: 2px solid; border-color: var(--rt-raised); border-radius: var(--radius); background: var(--color-bg-secondary); color: var(--color-text); }
	                                  html[data-style="retro"] :is(.blog-tag, .blog-pager-link):hover { background: var(--rt-hover); border-color: var(--rt-raised); }
	                                  html[data-style="retro"] :is(.blog-tag, .blog-pager-link):active { border-color: var(--rt-sunken); }
	                                  html[data-style="retro"] .blog-post-header { border-bottom: 1px solid var(--rt-shadow); }
	                                  html[data-style="neon"] .blog-card { border-color: var(--color-border); }
	                                  html[data-style="neon"] .blog-card:hover { border-color: var(--ne-pink); }
	                                  html[data-style="neon"] .page-content .blog-card .blog-card-title { font-weight: 800; }
	                                  html[data-style="neon"] :is(.blog-tag, .blog-pager-link) { border-color: var(--color-border); background: transparent; }
	                                  html[data-style="neon"] .blog-tag { color: var(--ne-cyan); }
	                                  html[data-style="neon"] :is(.blog-tag, .blog-pager-link):hover { border-color: var(--ne-pink); background: var(--ne-hover); }
	                                  html[data-style="neon"] .blog-post-header { border-bottom-color: var(--ne-line); }
	                                  html[data-style="tonal"] .blog-list { gap: 1rem; }
	                                  html[data-style="tonal"] .blog-card { padding: 1.5rem 1.75rem; border-color: transparent; background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .blog-card:hover { border-color: transparent; background: var(--tn-raised); }
	                                  html[data-style="tonal"] .page-content .blog-card .blog-card-title { font-weight: 500; }
	                                  html[data-style="tonal"] .blog-tag { border-color: transparent; border-radius: var(--tn-round); background: var(--tn-container); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .blog-tag:hover { border-color: transparent; background: color-mix(in srgb, var(--tn-on-container) 8%, var(--tn-container)); color: var(--tn-on-container); }
	                                  html[data-style="tonal"] .blog-pager-link { border-color: transparent; border-radius: var(--tn-round); background: var(--color-bg-secondary); }
	                                  html[data-style="tonal"] .blog-pager-link:hover { border-color: transparent; background: var(--tn-raised); }
	                                  html[data-style="tonal"] .blog-post-header { border-bottom: none; }
	                                  html[data-style="editor"] .blog-card:hover { border-color: var(--st-link); }
	                                  html[data-style="editor"] .page-content .blog-card .blog-card-title { font-weight: 600; }
	                                  html[data-style="editor"] :is(.blog-tag, .blog-pager-link) { background: var(--color-bg-secondary); }
	                                  html[data-style="editor"] :is(.blog-tag, .blog-pager-link):hover { background: var(--ed-hover); }

	                                  /* Styles: narrow screens */
	                                  @media (max-width: 768px) {
	                                      html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .content { padding: 1.5rem 1rem; }
	                                      html[data-style="moka"] .sidebar { border-right: 1px solid var(--mk-border-strong); }
	                                      html[data-style="minimal"] .sidebar { border-right: 1px solid var(--color-border); padding: 1.5rem 1rem; }
	                                      html[data-style="classic"] .site-header .search-trigger { min-width: 0; }
	                                      html[data-style="soft"] .sidebar { background: var(--sf-surface); box-shadow: var(--shadow-lg); }
	                                      html[data-style="terminal"] .sidebar { border-right: 1px solid var(--tm-line); }
	                                      html[data-style="paper"] .content { padding: 2rem 1.25rem; }
	                                      html[data-style="contrast"] .sidebar { border-right: 2px solid var(--color-text); }
	                                      html[data-style="glass"] .sidebar { background: var(--gl-panel-strong); box-shadow: var(--shadow-lg); }
	                                      html[data-style="brutal"] .sidebar { box-shadow: var(--shadow-lg); }
	                                      html[data-style="brutal"] .page-content h1 { font-size: 2rem !important; }
	                                      html[data-style="blueprint"] .sidebar { border-right: 1px solid var(--bp-ink); }
	                                      html[data-style="newspaper"] .content { padding: 2rem 1.25rem; }
	                                      html[data-style="newspaper"] .page-content h1 { font-size: 2.125rem !important; }
	                                      html[data-style="retro"] .sidebar { background: var(--color-bg-secondary); border-right: 2px solid var(--rt-dark); }
	                                      html[data-style="neon"] .sidebar { border-right: 1px solid var(--ne-line); }
	                                      html[data-style="tonal"] .sidebar { border-radius: 0 calc(var(--radius) * 2) calc(var(--radius) * 2) 0; background: var(--tn-raised); }
	                                      html[data-style="tonal"] .landing-hero { margin: 0.5rem 0.5rem 0; }
	                                      html[data-style="editor"] .sidebar { box-shadow: var(--shadow-lg); }
	                                  }
	                                  @media (max-width: 600px) {
	                                      html[data-style="classic"] .landing-hero { padding: 3rem 1rem 2.5rem; }
	                                  }

	                                  /* Styles: version selector. The open trigger's label is text, unlike the other pickers' icons, and white on the
	                                     lightened bar was under 4.5:1 for the emerald and amber presets */
	                                  html[data-style="classic"] .site-header .version-trigger[aria-expanded="true"] { background: var(--cs-bar-control); }

	                                  /* Styles: forced colors drop box-shadow, which moka's and terminal's focus rings and the search box's focus line are
	                                     made of. */
	                                  @media (forced-colors: active) {
	                                      html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) :focus-visible { outline-style: solid; outline-color: Highlight; }
	                                      html:is([data-style="moka"], [data-style="minimal"], [data-style="classic"], [data-style="soft"], [data-style="terminal"], [data-style="paper"], [data-style="contrast"], [data-style="glass"], [data-style="brutal"], [data-style="blueprint"], [data-style="newspaper"], [data-style="retro"], [data-style="neon"], [data-style="tonal"], [data-style="editor"]) .search-input-wrap:focus-within { outline: 2px solid Highlight; outline-offset: -2px; }
	                                  }
	                                  """;
}
