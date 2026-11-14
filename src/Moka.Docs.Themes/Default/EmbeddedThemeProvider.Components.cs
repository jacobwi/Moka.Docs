namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	// Style-specific component rules. They go into each style's own stylesheet with the styles' rules, so
	// main.css stays the default style alone.
	private const string _componentStylesCss = """
	                                      /* Styles: the components take each style's shape */
	                                      html[data-style="minimal"] :is(.component-details, .component-file-tree) { border-color: transparent; background: var(--color-bg-secondary); }
	                                      html[data-style="minimal"] .component-details[open] > summary { border-bottom-color: var(--color-bg); }
	                                      html[data-style="minimal"] .component-faq > .component-details + .component-details { border-top: 1px solid var(--color-bg); }
	                                      html[data-style="paper"] .component-details { border-left: 0; border-right: 0; border-radius: 0; background: none; }
	                                      html[data-style="paper"] .component-faq > .component-details { border-radius: 0; }
	                                      html[data-style="paper"] .component-file-tree { background: none; border-radius: 0; }
	                                      html[data-style="classic"] :is(.component-details, .component-file-tree) { box-shadow: var(--shadow-sm); }
	                                      html[data-style="soft"] :is(.component-details, .component-file-tree) { border-color: transparent; box-shadow: var(--shadow-md); }
	                                      html[data-style="soft"] .component-faq > .component-details + .component-details { border-top: 1px solid var(--color-border); }
	                                      html[data-style="glass"] :is(.component-details, .component-file-tree) {
	                                          background: color-mix(in srgb, var(--color-bg) 72%, transparent);
	                                          -webkit-backdrop-filter: blur(10px);
	                                          backdrop-filter: blur(10px);
	                                      }
	                                      html[data-style="brutal"] :is(.component-details, .component-file-tree, .component-video-frame) { border-width: 2px; box-shadow: var(--shadow-sm); }
	                                      html[data-style="brutal"] .component-faq > .component-details + .component-details { border-top: 2px solid var(--color-border); }
	                                      html[data-style="brutal"] .component-faq > .component-details:not(:last-of-type) { box-shadow: none; }
	                                      html[data-style="brutal"] .component-timeline-item::before { border-radius: 0; box-shadow: 0 0 0 2px var(--color-bg), 0 0 0 4px var(--color-border); }
	                                      html[data-style="brutal"] :is(.component-badge, .component-kbd > kbd) { border: 2px solid var(--color-border); }
	                                      html[data-style="contrast"] :is(.component-details, .component-file-tree) { border-width: 2px; }
	                                      html[data-style="contrast"] .component-badge { border-color: currentColor; }
	                                      html[data-style="terminal"] .component-badge { border-radius: 0; letter-spacing: 0; }
	                                      html[data-style="terminal"] .component-badge::before { content: "["; content: "[" / ""; }
	                                      html[data-style="terminal"] .component-badge::after { content: "]"; content: "]" / ""; }
	                                      html[data-style="moka"] .component-details > summary { font-size: 0.875rem; letter-spacing: 0.01em; }
	                                      """;

	// The Markdown components (details, FAQ, columns, file tree, timeline, video, figure, badges, keys and
	// glossary terms). The package widget reuses the API index's install widget styles and script.
	private const string _componentsCss = """
	                                      /* Components: details, FAQ, columns, file tree, timeline, video, figure, badges, keys, glossary.
	                                         Accent text is the default style's text shade or a style's --st-link. Semantic colors are the API badge
	                                         shades in the default style and the base color mixed toward the style's ink elsewhere, as the styles'
	                                         own badges are. */
	                                      :root {
	                                          --cmp-accent: var(--st-link, var(--color-primary-text));
	                                          --cmp-blue: var(--badge-blue);
	                                          --cmp-green: var(--badge-green);
	                                          --cmp-amber: var(--badge-amber);
	                                          --cmp-red: var(--badge-red);
	                                          --cmp-slate: var(--badge-slate);
	                                          --cmp-violet: var(--badge-violet);
	                                      }
	                                      html:not([data-style="default"]) {
	                                          --cmp-blue: color-mix(in srgb, #3b82f6 var(--st-hue), var(--st-ink));
	                                          --cmp-green: color-mix(in srgb, #22c55e var(--st-hue), var(--st-ink));
	                                          --cmp-amber: color-mix(in srgb, #f59e0b var(--st-hue), var(--st-ink));
	                                          --cmp-red: color-mix(in srgb, #ef4444 var(--st-hue), var(--st-ink));
	                                          --cmp-slate: color-mix(in srgb, #64748b var(--st-hue), var(--st-ink));
	                                          --cmp-violet: color-mix(in srgb, #8b5cf6 var(--st-hue), var(--st-ink));
	                                      }

	                                      /* Details and FAQ */
	                                      .component-details {
	                                          margin: 1rem 0;
	                                          border: 1px solid var(--color-border);
	                                          border-radius: var(--radius);
	                                          background: var(--color-bg);
	                                      }
	                                      .component-details > summary {
	                                          display: flex;
	                                          align-items: baseline;
	                                          gap: 0.625rem;
	                                          overflow-wrap: anywhere;
	                                          padding: 0.625rem 1rem;
	                                          font-weight: 600;
	                                          line-height: 1.5;
	                                          list-style: none;
	                                          cursor: pointer;
	                                          border-radius: var(--radius);
	                                      }
	                                      .component-details > summary::-webkit-details-marker { display: none; }
	                                      .component-details > summary::before {
	                                          content: "";
	                                          flex-shrink: 0;
	                                          width: 0.5em;
	                                          height: 0.5em;
	                                          border-right: 2px solid var(--cmp-accent);
	                                          border-bottom: 2px solid var(--cmp-accent);
	                                          transform: translateY(-0.1em) rotate(-45deg);
	                                          transition: transform var(--transition);
	                                      }
	                                      .component-details[open] > summary::before { transform: translateY(-0.25em) rotate(45deg); }
	                                      .component-details > summary:hover { background: var(--color-bg-secondary); }
	                                      .component-details[open] > summary { border-bottom: 1px solid var(--color-border); border-radius: var(--radius) var(--radius) 0 0; }
	                                      .component-details-body { padding: 0.875rem 1rem; }
	                                      .component-details-body > :first-child { margin-top: 0; }
	                                      .component-details-body > :last-child { margin-bottom: 0; }
	                                      .page-content .component-details > summary > .component-faq-question {
	                                          display: inline;
	                                          margin: 0;
	                                          padding: 0;
	                                          border: 0;
	                                          font-size: 1.0625rem !important;
	                                          line-height: 1.5;
	                                          animation: none;
	                                      }
	                                      .component-faq { margin: 1.5rem 0; }
	                                      .component-faq > .component-details { margin: 0; border-radius: 0; }
	                                      .component-faq > .component-details + .component-details { border-top: none; }
	                                      .component-faq > .component-details:first-of-type,
	                                      .component-faq > .component-details:first-of-type > summary { border-top-left-radius: var(--radius); border-top-right-radius: var(--radius); }
	                                      .component-faq > .component-details:last-of-type { border-bottom-left-radius: var(--radius); border-bottom-right-radius: var(--radius); }
	                                      .component-faq > .component-details > summary { border-radius: 0; }

	                                      /* Columns: up to four, two at tablet width for three or four, one on phones */
	                                      .component-columns {
	                                          --cmp-columns: 2;
	                                          display: grid;
	                                          grid-template-columns: repeat(var(--cmp-columns), minmax(0, 1fr));
	                                          gap: 1.5rem;
	                                          margin: 1.5rem 0;
	                                      }
	                                      .component-columns[data-columns="1"] { --cmp-columns: 1; }
	                                      .component-columns[data-columns="3"] { --cmp-columns: 3; }
	                                      .component-columns[data-columns="4"] { --cmp-columns: 4; }
	                                      .component-column { min-width: 0; }
	                                      .component-column > :first-child { margin-top: 0; }
	                                      .component-column > :last-child { margin-bottom: 0; }
	                                      @media (max-width: 960px) {
	                                          .component-columns[data-columns="3"], .component-columns[data-columns="4"] { --cmp-columns: 2; }
	                                      }
	                                      @media (max-width: 640px) {
	                                          .component-columns { --cmp-columns: 1 !important; gap: 1rem; }
	                                      }

	                                      /* File tree */
	                                      .component-file-tree {
	                                          margin: 1.5rem 0;
	                                          padding: 0.75rem 1rem;
	                                          overflow-x: auto;
	                                          border: 1px solid var(--color-border);
	                                          border-radius: var(--radius);
	                                          background: var(--color-bg-secondary);
	                                          font-family: var(--font-mono);
	                                          font-size: 0.875rem;
	                                          line-height: 1.5;
	                                      }
	                                      .page-content .component-file-tree ul { list-style: none; margin: 0; padding: 0; }
	                                      .page-content .component-file-tree ul ul { margin-left: 0.8125rem; padding-left: 0.75rem; border-left: 1px solid var(--color-border); }
	                                      .page-content .component-file-tree li { margin: 0; line-height: 1.5; }
	                                      .file-tree-entry {
	                                          position: relative;
	                                          display: inline-flex;
	                                          align-items: center;
	                                          gap: 0.4375rem;
	                                          max-width: 100%;
	                                          padding: 0.0625rem 0.375rem 0.0625rem 1.125rem;
	                                          border-radius: var(--radius-sm);
	                                          white-space: nowrap;
	                                      }
	                                      summary.file-tree-entry { list-style: none; cursor: pointer; }
	                                      summary.file-tree-entry::-webkit-details-marker { display: none; }
	                                      summary.file-tree-entry::before {
	                                          content: "";
	                                          position: absolute;
	                                          left: 0.375rem;
	                                          top: 50%;
	                                          width: 0.3125rem;
	                                          height: 0.3125rem;
	                                          border-right: 1.5px solid var(--color-text-secondary);
	                                          border-bottom: 1.5px solid var(--color-text-secondary);
	                                          transform: translateY(-70%) rotate(45deg);
	                                      }
	                                      details:not([open]) > summary.file-tree-entry::before { transform: translate(-25%, -50%) rotate(-45deg); }
	                                      summary.file-tree-entry:hover { background: var(--color-bg); }
	                                      .file-tree-icon { display: inline-flex; flex-shrink: 0; color: var(--color-text-secondary); }
	                                      .file-tree-icon svg { width: 1rem; height: 1rem; }
	                                      .file-tree-icon-folder { color: var(--cmp-accent); }
	                                      .file-tree-icon-code { color: var(--cmp-blue); }
	                                      .file-tree-icon-data { color: var(--cmp-amber); }
	                                      .file-tree-icon-image { color: var(--cmp-violet); }
	                                      .file-tree-icon-media { color: var(--cmp-red); }
	                                      .file-tree-icon-archive { color: var(--cmp-green); }
	                                      .file-tree-name { color: var(--color-text); }
	                                      .file-tree-comment {
	                                          margin-left: 0.5rem;
	                                          color: var(--color-text-secondary);
	                                          font-family: var(--font-body);
	                                          font-size: 0.8125rem;
	                                          white-space: normal;
	                                      }
	                                      .page-content .file-tree-comment code { font-size: 0.75rem !important; padding: 0 0.25rem !important; }
	                                      .file-tree-highlight > .file-tree-entry,
	                                      .file-tree-highlight > details > .file-tree-entry {
	                                          background: color-mix(in srgb, var(--color-primary) 14%, transparent);
	                                          font-weight: 600;
	                                      }
	                                      .file-tree-highlight > .file-tree-entry .file-tree-comment,
	                                      .file-tree-highlight > details > .file-tree-entry .file-tree-comment { color: var(--color-text); }

	                                      /* Package widgets: the commands keep the page's text color whatever the code theme, which colors other
	                                         code blocks (a dark code theme on a light page left them light on light). */
	                                      html .page-content .install-panel pre code { color: var(--color-text); }

	                                      /* Timeline */
	                                      .page-content .component-timeline { list-style: none; margin: 1.5rem 0; padding: 0; }
	                                      .page-content .component-timeline-item { position: relative; margin: 0; padding: 0 0 1.25rem 1.75rem; }
	                                      .component-timeline-item::before {
	                                          content: "";
	                                          position: absolute;
	                                          left: 0.3125rem;
	                                          top: 0.5rem;
	                                          width: 0.625rem;
	                                          height: 0.625rem;
	                                          border-radius: 50%;
	                                          background: var(--cmp-accent);
	                                          box-shadow: 0 0 0 3px var(--color-bg);
	                                      }
	                                      .component-timeline-item::after {
	                                          content: "";
	                                          position: absolute;
	                                          left: calc(0.625rem - 1px);
	                                          top: 1.375rem;
	                                          bottom: 0.125rem;
	                                          width: 2px;
	                                          background: var(--color-border);
	                                      }
	                                      .page-content .component-timeline-item:last-child { padding-bottom: 0; }
	                                      .component-timeline-item:last-child::after { content: none; }
	                                      .page-content .component-timeline-date {
	                                          display: block;
	                                          margin: 0 0 0.25rem;
	                                          padding: 0;
	                                          border: 0;
	                                          color: var(--cmp-accent);
	                                          font-size: 0.9375rem !important;
	                                          font-weight: 700;
	                                          line-height: 1.6;
	                                          letter-spacing: 0;
	                                          animation: none;
	                                      }
	                                      .component-timeline-body > :first-child { margin-top: 0; }
	                                      .component-timeline-body > :last-child { margin-bottom: 0; }

	                                      /* Video and figure */
	                                      .component-video, .component-figure { margin: 1.5rem 0; }
	                                      .component-video-frame {
	                                          position: relative;
	                                          aspect-ratio: 16 / 9;
	                                          overflow: hidden;
	                                          border: 1px solid var(--color-border);
	                                          border-radius: var(--radius);
	                                          background: #000000;
	                                      }
	                                      .component-video-frame > :is(iframe, video) {
	                                          position: absolute;
	                                          inset: 0;
	                                          width: 100%;
	                                          height: 100%;
	                                          border: 0;
	                                          object-fit: contain;
	                                      }
	                                      .component-figure > :not(figcaption) { margin-left: auto; margin-right: auto; }
	                                      .component-figure > img { display: block; }
	                                      :is(.component-video, .component-figure) > figcaption {
	                                          margin-top: 0.625rem;
	                                          color: var(--color-text-secondary);
	                                          font-size: 0.875rem;
	                                          line-height: 1.6;
	                                          text-align: center;
	                                      }
	                                      :is(.component-video, .component-figure) > figcaption > :first-child { margin-top: 0; }
	                                      :is(.component-video, .component-figure) > figcaption > :last-child { margin-bottom: 0; }

	                                      /* Badges */
	                                      .component-badge {
	                                          --cmp-c: #3b82f6;
	                                          --cmp-text: var(--cmp-blue);
	                                          display: inline-block;
	                                          margin: 0 0.125rem;
	                                          padding: 0 0.4375rem;
	                                          border: 1px solid color-mix(in srgb, var(--cmp-c) 32%, transparent);
	                                          border-radius: var(--radius-sm);
	                                          background: color-mix(in srgb, var(--cmp-c) 14%, var(--color-bg));
	                                          color: var(--cmp-text);
	                                          -webkit-text-fill-color: currentColor;
	                                          font-family: var(--font-body);
	                                          font-size: 0.75rem;
	                                          font-weight: 600;
	                                          line-height: 1.6;
	                                          letter-spacing: 0.01em;
	                                          white-space: nowrap;
	                                          vertical-align: 0.125em;
	                                      }
	                                      .component-badge-success { --cmp-c: #22c55e; --cmp-text: var(--cmp-green); }
	                                      .component-badge-warning { --cmp-c: #f59e0b; --cmp-text: var(--cmp-amber); }
	                                      .component-badge-danger { --cmp-c: #ef4444; --cmp-text: var(--cmp-red); }
	                                      .component-badge-neutral { --cmp-c: #64748b; --cmp-text: var(--cmp-slate); }
	                                      :is(h1, h2, h3, h4, h5, h6) .component-badge { vertical-align: middle; letter-spacing: 0.01em; }

	                                      /* Keys */
	                                      .component-kbd { white-space: nowrap; }
	                                      .component-kbd > kbd {
	                                          display: inline-block;
	                                          min-width: 1.6em;
	                                          margin: 0 0.125rem;
	                                          padding: 0 0.375rem;
	                                          border: 1px solid var(--color-border);
	                                          border-bottom-width: 2px;
	                                          border-radius: var(--radius-sm);
	                                          background: var(--color-bg-secondary);
	                                          color: var(--color-text);
	                                          -webkit-text-fill-color: currentColor;
	                                          font-family: var(--font-mono);
	                                          font-size: 0.8em;
	                                          font-weight: 500;
	                                          line-height: 1.6;
	                                          text-align: center;
	                                          vertical-align: 0.1em;
	                                      }
	                                      /* On a phone a long badge wraps, and a key combination wraps between its keys. */
	                                      @media (max-width: 768px) {
	                                          .component-badge { max-width: 100%; white-space: normal; overflow-wrap: anywhere; }
	                                          .component-kbd { white-space: normal; }
	                                      }

	                                      /* Glossary terms: the definition shows on hover and keyboard focus, a click or tap keeps it open, Escape
	                                         hides it, and the pointer can move onto it. */
	                                      .glossary-term { position: relative; }
	                                      .glossary-term-button {
	                                          padding: 0;
	                                          margin: 0;
	                                          border: 0;
	                                          background: none;
	                                          color: inherit;
	                                          -webkit-text-fill-color: currentColor;
	                                          font: inherit;
	                                          letter-spacing: inherit;
	                                          text-decoration: underline dotted;
	                                          text-decoration-thickness: 1px;
	                                          text-underline-offset: 0.2em;
	                                          cursor: help;
	                                      }
	                                      .glossary-term-button:hover { text-decoration-style: solid; }
	                                      /* A hidden tip is scaled to nothing: it still has its box, and one near the right edge made the page scroll
	                                         sideways. It scales down only once it has faded out. */
	                                      .glossary-tip {
	                                          position: absolute;
	                                          left: 50%;
	                                          bottom: calc(100% + 0.5rem);
	                                          z-index: var(--z-popover);
	                                          width: max-content;
	                                          max-width: min(20rem, calc(100vw - 2rem));
	                                          padding: 0.5rem 0.75rem;
	                                          border-radius: var(--radius-sm);
	                                          background: var(--color-text);
	                                          color: var(--color-bg);
	                                          -webkit-text-fill-color: currentColor;
	                                          box-shadow: var(--shadow-md);
	                                          font-family: var(--font-body);
	                                          font-size: 0.8125rem;
	                                          font-weight: 400;
	                                          font-style: normal;
	                                          line-height: 1.5;
	                                          letter-spacing: 0;
	                                          text-align: left;
	                                          text-transform: none;
	                                          white-space: normal;
	                                          transform: translateX(calc(-50% + var(--tip-shift, 0px))) scale(var(--tip-scale, 0));
	                                          visibility: hidden;
	                                          opacity: 0;
	                                          transition: opacity var(--transition), visibility var(--transition), transform 0s var(--transition);
	                                      }
	                                      .glossary-tip::after {
	                                          content: "";
	                                          position: absolute;
	                                          left: 0;
	                                          right: 0;
	                                          top: 100%;
	                                          height: 0.5rem;
	                                      }
	                                      .glossary-term.is-below > .glossary-tip { top: calc(100% + 0.5rem); bottom: auto; }
	                                      .glossary-term.is-below > .glossary-tip::after { top: auto; bottom: 100%; }
	                                      .glossary-term:hover > .glossary-tip,
	                                      .glossary-term:focus-within > .glossary-tip,
	                                      .glossary-term.is-open > .glossary-tip { --tip-scale: 1; visibility: visible; opacity: 1; transition: opacity var(--transition), visibility var(--transition); }
	                                      .glossary-term.is-dismissed > .glossary-tip { --tip-scale: 0; visibility: hidden; opacity: 0; transition: opacity var(--transition), visibility var(--transition), transform 0s var(--transition); }
	                                      html .page-content .glossary-tip code {
	                                          padding: 0 0.25rem !important;
	                                          border: 0 !important;
	                                          background: color-mix(in srgb, var(--color-bg) 18%, transparent) !important;
	                                          color: inherit !important;
	                                          -webkit-text-fill-color: currentColor;
	                                      }
	                                      .component-glossary { margin: 1.5rem 0; }
	                                      .component-glossary > dt { margin-top: 1rem; color: var(--color-text); font-weight: 700; }
	                                      .component-glossary > dt:first-child { margin-top: 0; }
	                                      .component-glossary > dd {
	                                          margin: 0.25rem 0 0;
	                                          padding-left: 0.875rem;
	                                          border-left: 2px solid var(--color-border);
	                                          color: var(--color-text-secondary);
	                                      }
	                                      .component-glossary > dt:target { color: var(--cmp-accent); }

	                                      /* Print: closed details are opened by the script; tooltips and players don't print */
	                                      @media print {
	                                          .glossary-tip, .component-video-frame { display: none !important; }
	                                          .glossary-term-button { text-decoration: none; }
	                                          :is(.component-details, .component-file-tree, .component-timeline-item, .component-figure) { break-inside: avoid; }
	                                      }

	                                      @media (forced-colors: active) {
	                                          :is(.component-badge, .component-kbd > kbd, .glossary-tip) { border: 1px solid CanvasText; }
	                                          .component-timeline-item::before { forced-color-adjust: none; background: CanvasText; box-shadow: none; }
	                                          .file-tree-highlight > .file-tree-entry, .file-tree-highlight > details > .file-tree-entry { outline: 1px solid Highlight; }
	                                      }
	                                      """;

	private const string _componentsJs = """
	                                     /* Components: glossary term tooltips, and details that open for a linked heading or for printing */
	                                     (function() {
	                                         'use strict';

	                                         var openTerm = null;

	                                         function headerBottom() {
	                                             var header = document.querySelector('.site-header');
	                                             return header ? Math.max(0, header.getBoundingClientRect().bottom) : 0;
	                                         }

	                                         // Above the term when it fits under the header, else below, and moved sideways to stay on screen.
	                                         function placeTip(term) {
	                                             var tip = term.querySelector('.glossary-tip');
	                                             if (!tip) return;
	                                             term.classList.remove('is-below');
	                                             tip.style.removeProperty('--tip-shift');
	                                             if (tip.getBoundingClientRect().top < headerBottom() + 8) term.classList.add('is-below');
	                                             var rect = tip.getBoundingClientRect();
	                                             var width = document.documentElement.clientWidth;
	                                             var shift = rect.left < 8 ? 8 - rect.left : rect.right > width - 8 ? width - 8 - rect.right : 0;
	                                             if (shift) tip.style.setProperty('--tip-shift', Math.round(shift) + 'px');
	                                         }

	                                         function closeTerm(term, dismiss) {
	                                             term.classList.remove('is-open');
	                                             if (dismiss) term.classList.add('is-dismissed');
	                                             if (openTerm === term) openTerm = null;
	                                         }

	                                         function showTerm(term) {
	                                             term.classList.remove('is-dismissed');
	                                             placeTip(term);
	                                         }

	                                         document.querySelectorAll('.glossary-term').forEach(function(term) {
	                                             term.addEventListener('pointerenter', function() { showTerm(term); });
	                                             term.addEventListener('pointerleave', function() { term.classList.remove('is-dismissed'); });
	                                             term.addEventListener('focusin', function() { showTerm(term); });
	                                             term.addEventListener('focusout', function(e) {
	                                                 if (!term.contains(e.relatedTarget)) {
	                                                     closeTerm(term, false);
	                                                     term.classList.remove('is-dismissed');
	                                                 }
	                                             });
	                                         });

	                                         // A click or tap toggles the definition, so it can be read on a touch screen and kept open with a mouse.
	                                         document.addEventListener('click', function(e) {
	                                             var button = e.target && e.target.closest ? e.target.closest('.glossary-term-button') : null;
	                                             var term = button ? button.parentElement : null;
	                                             if (openTerm && openTerm !== term && !openTerm.contains(e.target)) closeTerm(openTerm, false);
	                                             if (!term) return;
	                                             if (term.classList.contains('is-open')) {
	                                                 closeTerm(term, true);
	                                             } else {
	                                                 showTerm(term);
	                                                 term.classList.add('is-open');
	                                                 openTerm = term;
	                                             }
	                                         });

	                                         // Escape hides a definition without moving the pointer or the focus away from it.
	                                         document.addEventListener('keydown', function(e) {
	                                             if (e.key !== 'Escape' || e.defaultPrevented) return;
	                                             var shown = Array.prototype.filter.call(document.querySelectorAll('.glossary-term'), function(term) {
	                                                 return !term.classList.contains('is-dismissed')
	                                                     && (term.classList.contains('is-open') || term.matches(':hover') || term.contains(document.activeElement));
	                                             });
	                                             shown.forEach(function(term) { closeTerm(term, true); });
	                                             if (shown.length > 0) e.preventDefault();
	                                         });

	                                         // A link to a heading inside a closed details block opens it (and the blocks around it).
	                                         function revealTarget() {
	                                             var id = window.location.hash.slice(1);
	                                             if (!id) return;
	                                             var target = null;
	                                             try { target = document.getElementById(decodeURIComponent(id)); } catch (err) { target = document.getElementById(id); }
	                                             if (!target || !target.closest) return;
	                                             var opened = false;
	                                             for (var details = target.closest('details'); details; details = details.parentElement ? details.parentElement.closest('details') : null) {
	                                                 if (!details.open && !details.classList.contains('toc-inline')) {
	                                                     details.open = true;
	                                                     opened = true;
	                                                 }
	                                             }
	                                             if (opened) target.scrollIntoView({ block: 'start' });
	                                         }
	                                         revealTarget();
	                                         window.addEventListener('hashchange', revealTarget);

	                                         // Printing shows what closed blocks hold; they close again afterwards.
	                                         var openedForPrint = [];
	                                         window.addEventListener('beforeprint', function() {
	                                             openedForPrint = Array.prototype.slice.call(document.querySelectorAll('.component-details:not([open]), .component-file-tree details:not([open])'));
	                                             openedForPrint.forEach(function(details) { details.open = true; });
	                                         });
	                                         window.addEventListener('afterprint', function() {
	                                             openedForPrint.forEach(function(details) { details.open = false; });
	                                             openedForPrint = [];
	                                         });
	                                     })();
	                                     """;
}
