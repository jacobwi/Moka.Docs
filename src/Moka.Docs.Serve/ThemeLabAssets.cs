using System.Text.Json;

namespace Moka.Docs.Serve;

/// <summary>
///     The theme lab's script and styles. The script builds the panel inside a shadow root, so the
///     site's CSS can't reach it and its CSS can't reach the site. Sizes are in px on purpose: the
///     reader's text size setting scales the site's rem, not the tool.
/// </summary>
internal static class ThemeLabAssets
{
	private const string _css = """
		:host { all: initial; }
		.fab, .panel {
			box-sizing: border-box;
			color: #f4f4f5;
			color-scheme: dark;
			font-family: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
			font-size: 13px;
			line-height: 1.45;
			text-align: left;
			-webkit-font-smoothing: antialiased;
		}
		.panel *, .panel *::before, .panel *::after { box-sizing: inherit; }
		button, select, input { font: inherit; color: inherit; margin: 0; }
		:focus-visible { outline: 2px solid #93c5fd; outline-offset: 2px; }
		.fab {
			position: fixed;
			left: 16px;
			bottom: 16px;
			z-index: 150;
			display: inline-flex;
			align-items: center;
			gap: 6px;
			padding: 6px 12px 6px 10px;
			background: #18181b;
			border: 1px solid #71717a;
			border-radius: 8px;
			font-size: 12px;
			font-weight: 600;
			cursor: pointer;
		}
		.fab:hover { background: #27272a; }
		.fab svg { display: block; width: 16px; height: 16px; }
		.fab .dot { width: 8px; height: 8px; border-radius: 50%; background: #fcd34d; }
		.fab .dot[hidden] { display: none; }
		.panel {
			position: fixed;
			left: 16px;
			bottom: 58px;
			z-index: 150;
			display: flex;
			flex-direction: column;
			width: 380px;
			max-width: calc(100vw - 32px);
			max-height: calc(100vh - 74px);
			background: #18181b;
			border: 1px solid #52525b;
			border-radius: 10px;
			box-shadow: 0 16px 40px rgba(0, 0, 0, 0.5);
		}
		.panel[hidden] { display: none; }
		.head { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-bottom: 1px solid #3f3f46; }
		h2 { margin: 0; font-size: 14px; font-weight: 600; }
		h2:focus { outline: none; }
		.file {
			flex: 1;
			min-width: 0;
			overflow: hidden;
			color: #a1a1aa;
			font: 11px/1.4 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
			text-overflow: ellipsis;
			white-space: nowrap;
		}
		.close {
			width: 28px;
			height: 28px;
			padding: 0;
			background: transparent;
			border: 1px solid transparent;
			border-radius: 6px;
			font-size: 18px;
			line-height: 1;
			cursor: pointer;
		}
		.close:hover { background: #27272a; border-color: #52525b; }
		.body { overflow-y: auto; overscroll-behavior: contain; padding: 0 12px 12px; }
		fieldset { margin: 14px 0 0; padding: 0; border: 0; min-width: 0; }
		legend {
			margin: 0 0 4px;
			padding: 0;
			color: #a1a1aa;
			font-size: 11px;
			font-weight: 700;
			letter-spacing: 0.06em;
			text-transform: uppercase;
		}
		.row { display: grid; grid-template-columns: minmax(0, 1fr) 170px; gap: 2px 10px; align-items: center; padding: 5px 0; }
		.row.text { grid-template-columns: minmax(0, 1fr); }
		.row label { display: flex; flex-direction: column; min-width: 0; cursor: pointer; }
		.row label code {
			color: #a1a1aa;
			font: 10.5px/1.4 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
			overflow-wrap: anywhere;
		}
		.row.changed .name::after { content: " *"; color: #fcd34d; }
		select, input[type="text"] {
			width: 100%;
			min-height: 28px;
			padding: 3px 6px;
			background: #27272a;
			border: 1px solid #52525b;
			border-radius: 6px;
		}
		input[type="checkbox"] { justify-self: start; width: 16px; height: 16px; accent-color: #93c5fd; }
		.row.rebuild select { border-color: #fcd34d; }
		.hint { grid-column: 1 / -1; margin: 0; color: #a1a1aa; font-size: 11.5px; }
		.hint:empty { display: none; }
		.hint span { display: block; }
		.hint .rebuild { color: #fcd34d; }
		.hint .env { color: #fca5a5; }
		.notice { margin: 12px 0 0; padding: 8px 10px; border: 1px solid #52525b; border-radius: 6px; font-size: 12px; }
		.notice.warn { color: #fcd34d; border-color: #a16207; }
		.notice.error { color: #fca5a5; border-color: #b91c1c; }
		.notice.ok { color: #86efac; border-color: #15803d; }
		.snippet { margin-top: 14px; border: 1px solid #3f3f46; border-radius: 6px; background: #09090b; }
		.snippet[hidden] { display: none; }
		.snippet-head {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 8px;
			padding: 6px 8px 6px 10px;
			border-bottom: 1px solid #3f3f46;
			color: #a1a1aa;
			font-size: 11.5px;
		}
		.snippet pre { margin: 0; padding: 8px 10px; overflow-x: auto; }
		.snippet code { color: #e4e4e7; font: 12px/1.5 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; white-space: pre; }
		.foot { padding: 10px 12px; border-top: 1px solid #3f3f46; }
		.status { min-height: 18px; margin: 0 0 8px; color: #d4d4d8; font-size: 12px; }
		.status.ok { color: #86efac; }
		.status.error { color: #fca5a5; }
		.actions { display: flex; flex-wrap: wrap; gap: 8px; }
		.actions button, .small {
			padding: 5px 12px;
			background: #27272a;
			border: 1px solid #52525b;
			border-radius: 6px;
			cursor: pointer;
		}
		.small { padding: 2px 10px; font-size: 12px; }
		.actions button:hover, .small:hover { background: #3f3f46; }
		.actions .primary { background: #93c5fd; border-color: #93c5fd; color: #18181b; font-weight: 600; }
		.actions .primary:hover { background: #bfdbfe; }
		.actions button:disabled { cursor: not-allowed; opacity: 0.55; }
		@media (max-width: 480px) {
			.fab { left: 8px; bottom: 8px; }
			.panel { left: 8px; right: 8px; bottom: 50px; width: auto; max-width: none; max-height: calc(100vh - 62px); }
			.row { grid-template-columns: minmax(0, 1fr) 140px; }
		}
		@media (forced-colors: active) {
			.fab, .panel, .actions button, .small, select, input { border: 1px solid CanvasText; }
		}
		@media print {
			.fab, .panel { display: none !important; }
		}
		""";

	private const string _js = """
		(function () {
			'use strict';
			const script = document.currentScript;
			if (window.mokadocsThemeLab || !script || !document.body || !Element.prototype.attachShadow) return;
			window.mokadocsThemeLab = true;

			const css = __LAB_CSS__;
			const api = script.src.replace(/[^/]*$/, '');
			const html = document.documentElement;
			const openKey = 'mokadocs-theme-lab-open';
			const pendingKey = 'mokadocs-theme-lab-pending';
			const noteKey = 'mokadocs-theme-lab-note';

			// Blocked storage throws on every access; the lab works without it.
			function storage(name) {
				return {
					get: function (key) { try { return window[name].getItem(key); } catch (e) { return null; } },
					set: function (key, value) { try { window[name].setItem(key, value); } catch (e) { } },
					remove: function (key) { try { window[name].removeItem(key); } catch (e) { } }
				};
			}
			const session = storage('sessionStorage');
			const local = storage('localStorage');

			function el(tag, props, children) {
				const node = document.createElement(tag);
				Object.keys(props || {}).forEach(function (name) {
					const value = props[name];
					if (value === null || value === undefined || value === false) return;
					if (name === 'text') node.textContent = value;
					else if (name === 'className') node.className = value;
					else node.setAttribute(name, value === true ? '' : String(value));
				});
				(children || []).forEach(function (child) {
					if (child) node.appendChild(typeof child === 'string' ? document.createTextNode(child) : child);
				});
				return node;
			}

			const icon = el('span', { 'aria-hidden': 'true' });
			icon.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="13.5" cy="6.5" r=".5" fill="currentColor"/><circle cx="17.5" cy="10.5" r=".5" fill="currentColor"/><circle cx="8.5" cy="7.5" r=".5" fill="currentColor"/><circle cx="6.5" cy="12.5" r=".5" fill="currentColor"/><path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10c.93 0 1.65-.75 1.65-1.69 0-.44-.18-.84-.44-1.13-.29-.29-.44-.65-.44-1.13a1.64 1.64 0 0 1 1.67-1.67h2c3.05 0 5.55-2.5 5.55-5.55C21.97 6.01 17.46 2 12 2z"/></svg>';
			const dot = el('span', { className: 'dot', hidden: true });
			const fab = el('button', { type: 'button', className: 'fab', 'aria-expanded': 'false', 'aria-controls': 'lab-panel' },
				[icon, el('span', { text: 'Theme lab' }), dot]);
			const title = el('h2', { id: 'lab-title', tabindex: '-1', text: 'Theme lab' });
			const fileName = el('span', { className: 'file' });
			const closeButton = el('button', { type: 'button', className: 'close', 'aria-label': 'Close the theme lab', text: String.fromCharCode(215) });
			const notices = el('div', { className: 'notices' });
			const form = el('div', { className: 'form' });
			const snippetCode = el('code');
			const copyButton = el('button', { type: 'button', className: 'small', text: 'Copy' });
			const snippet = el('div', { className: 'snippet', id: 'lab-snippet', hidden: true }, [
				el('div', { className: 'snippet-head' }, [el('span', { text: 'What Save writes under theme.options' }), copyButton]),
				el('pre', null, [snippetCode])
			]);
			const status = el('p', { className: 'status', role: 'status' });
			const saveButton = el('button', { type: 'button', className: 'primary', text: 'Save', disabled: true });
			const resetButton = el('button', { type: 'button', text: 'Reset', disabled: true });
			const yamlButton = el('button', { type: 'button', 'aria-expanded': 'false', 'aria-controls': 'lab-snippet', text: 'Show YAML' });
			const panel = el('section', { className: 'panel', id: 'lab-panel', role: 'dialog', 'aria-labelledby': 'lab-title', hidden: true }, [
				el('header', { className: 'head' }, [title, fileName, closeButton]),
				el('div', { className: 'body' }, [notices, form, snippet]),
				el('footer', { className: 'foot' }, [status, el('div', { className: 'actions' }, [saveButton, resetButton, yamlButton])])
			]);

			const host = document.createElement('mokadocs-theme-lab');
			const root = host.attachShadow({ mode: 'open' });
			root.appendChild(el('style', { text: css }));
			root.appendChild(fab);
			root.appendChild(panel);
			document.body.appendChild(host);

			let catalog = null;
			let options = [];
			let loaded = {};
			let state = {};
			let loading = null;
			let busy = false;
			let saved = false;
			const initial = {};
			const touched = {};
			const controls = {};

			function same(a, b) {
				return String(a === null || a === undefined ? '' : a).toLowerCase() === String(b === null || b === undefined ? '' : b).toLowerCase();
			}

			function isSet(value) {
				return value !== null && value !== undefined;
			}

			// Text compares exactly; names and switches the way the configuration reader does, without case.
			function sameValue(option, a, b) {
				if (!isSet(a) || !isSet(b)) return !isSet(a) && !isSet(b);
				return option.kind === 'text' ? a === b : same(a, b);
			}

			function optionFor(key) {
				return options.filter(function (option) { return option.key === key; })[0] || null;
			}

			function find(option, value) {
				return option.choices.filter(function (choice) { return same(choice.value, value); })[0] || null;
			}

			function presetOf(values) {
				const option = optionFor('preset');
				return option && find(option, values.preset) || null;
			}

			// What a key reads as while the file leaves it out: the preset's value, else the default.
			function baseline(option, values) {
				const preset = option.key === 'preset' ? null : presetOf(values);
				return preset && preset.sets && Object.prototype.hasOwnProperty.call(preset.sets, option.key) ? preset.sets[option.key] : option.default;
			}

			function effective(option, values) {
				return isSet(values[option.key]) ? values[option.key] : baseline(option, values);
			}

			function changedOptions() {
				return options.filter(function (option) { return !sameValue(option, state[option.key], loaded[option.key]); });
			}

			// 'same' when the page already shows it, 'live' when switching an attribute on <html> shows it, else
			// 'rebuild'. A layout variant is live only when it and the variant the page was built with are both
			// plain CSS: the others render other markup. A page with the Appearance panel's Layout section
			// (theme.options.layoutSelector) carries the markup of every variant of the parts readers switch.
			function statusOf(option) {
				const now = effective(option, state);
				// A preset shows through the options it sets, each with its own status.
				if (option.key === 'preset' || sameValue(option, now, effective(option, loaded))) return 'same';
				if (!option.attribute) return 'rebuild';
				const everyVariant = option.liveWithLayoutSelector && !!document.querySelector('.appearance-layout');
				const choice = find(option, now);
				if (!choice || (choice.rebuild && !everyVariant)) return 'rebuild';
				if (option.removeAttributeAtDefault) {
					const built = find(option, isSet(initial[option.attribute]) ? initial[option.attribute] : option.default);
					if (!built || (built.rebuild && !everyVariant)) return 'rebuild';
				}
				return 'live';
			}

			// The theme links layouts.css only on sites that set a layout variant, so a live preview on any
			// other site loads it, after the theme's own stylesheets so the variant rules still win.
			function loadLayoutsCss() {
				if (document.querySelector('link[href*="/_theme/css/layouts.css"]')) return;
				const sheets = document.querySelectorAll('link[rel="stylesheet"][href*="/_theme/css/"]');
				const link = document.createElement('link');
				link.rel = 'stylesheet';
				link.href = (html.getAttribute('data-base-path') || '') + '/_theme/css/layouts.css';
				const last = sheets[sheets.length - 1];
				if (last) last.after(link); else document.head.appendChild(link);
			}

			function setAttribute(name, value) {
				touched[name] = true;
				if (value === null) html.removeAttribute(name);
				else if (html.getAttribute(name) !== value) html.setAttribute(name, value);
			}

			// Only attributes the lab changed go back, so a pick made in the site's own Appearance panel stays.
			function restore(name) {
				if (!touched[name]) return;
				touched[name] = false;
				const value = initial[name];
				if (!isSet(value)) html.removeAttribute(name);
				else if (html.getAttribute(name) !== value) html.setAttribute(name, value);
			}

			function applyPreview() {
				if (saved) return;
				options.forEach(function (option) {
					if (!option.attribute) return;
					if (statusOf(option) !== 'live') {
						restore(option.attribute);
						return;
					}
					const value = find(option, effective(option, state)).value;
					if (option.key.indexOf('layout.') === 0 && value !== option.default) loadLayoutsCss();
					setAttribute(option.attribute, option.removeAttributeAtDefault && value === option.default ? null : value);
				});
				// A dark-first style starts dark for a reader who hasn't picked a mode, as the theme does.
				const style = optionFor('style');
				if (style && statusOf(style) === 'live' && !local.get('mokadocs-theme')) {
					const choice = find(style, effective(style, state));
					const dark = choice.darkFirst || (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
					setAttribute('data-theme', dark ? 'dark' : 'light');
				} else {
					restore('data-theme');
				}
			}

			function choiceText(option, choice) {
				return choice.label + (choice.rebuild ? ' (rebuild)' : '');
			}

			function row(option) {
				const id = 'lab-' + option.key.replace(/\./g, '-');
				let input;
				if (option.kind === 'choice') {
					input = el('select', { id: id });
					option.choices.forEach(function (choice) {
						input.appendChild(el('option', { value: choice.value, text: choiceText(option, choice) }));
					});
					input.addEventListener('change', function () { pick(option, input.value); });
				} else if (option.kind === 'toggle') {
					input = el('input', { id: id, type: 'checkbox' });
					input.addEventListener('change', function () { pick(option, input.checked ? 'true' : 'false'); });
				} else {
					input = el('input', { id: id, type: 'text', maxlength: option.maxLength, autocomplete: 'off', spellcheck: 'false' });
					input.addEventListener('input', function () { pick(option, input.value.trim()); });
				}
				const hint = el('p', { className: 'hint', id: id + '-hint' });
				input.setAttribute('aria-describedby', hint.id);
				const label = el('label', { for: id }, [el('span', { className: 'name', text: option.label }), el('code', { text: option.key })]);
				const box = el('div', { className: 'row ' + option.kind }, [label, input, hint]);
				controls[option.key] = { input: input, hint: hint, row: box };
				return box;
			}

			function buildForm() {
				const groups = {};
				form.replaceChildren();
				options.forEach(function (option) {
					if (!groups[option.group]) {
						groups[option.group] = el('fieldset', null, [el('legend', { text: option.group })]);
						form.appendChild(groups[option.group]);
					}
					groups[option.group].appendChild(row(option));
				});
			}

			// The file's value comes back as written; a value equal to what the key reads as without it is left out.
			function pick(option, value) {
				if (isSet(loaded[option.key]) && sameValue(option, value, loaded[option.key])) state[option.key] = loaded[option.key];
				else if (value === '' || sameValue(option, value, baseline(option, state))) state[option.key] = null;
				else state[option.key] = value;
				update();
			}

			function renderControl(option) {
				const control = controls[option.key];
				const value = effective(option, state);
				if (option.kind === 'choice') {
					const choice = find(option, value);
					let unknown = control.input.querySelector('option[data-unknown]');
					if (choice) {
						if (unknown) unknown.remove();
						control.input.value = choice.value;
					} else {
						if (!unknown) {
							unknown = el('option', { 'data-unknown': true });
							control.input.insertBefore(unknown, control.input.firstChild);
						}
						unknown.value = value;
						unknown.textContent = (value === '' ? '(empty)' : value) + ' (unknown name)';
						control.input.value = value;
					}
				} else if (option.kind === 'toggle') {
					control.input.checked = same(value, 'true');
				} else if (root.activeElement !== control.input) {
					control.input.value = value;
				}

				const status = statusOf(option);
				const lines = [];
				const preset = presetOf(state);
				if (option.key === 'preset') {
					const choice = find(option, value);
					if (choice && choice.description) lines.push(['', choice.description]);
				} else if (!isSet(state[option.key]) && preset && preset.sets && Object.prototype.hasOwnProperty.call(preset.sets, option.key)) {
					lines.push(['', 'From the ' + preset.label + ' preset.']);
				}
				if (option.key === 'defaultColorTheme' && catalog.customPrimaryColor && same(value, option.default)) {
					lines.push(['', 'With ' + value + ', pages show your primaryColor.']);
				}
				if (status === 'rebuild') lines.push(['rebuild', 'Needs a rebuild: shows after Save.']);
				if (option.environment) {
					lines.push(['env', option.environment.name + ' is set to "' + option.environment.value + '" and wins over ' +
						catalog.configFile + ', so a saved value won\'t show while it is set.']);
				}
				control.hint.replaceChildren.apply(control.hint, lines.map(function (line) {
					return el('span', { className: line[0] || null, text: line[1] });
				}));
				control.row.classList.toggle('rebuild', status === 'rebuild');
				control.row.classList.toggle('changed', !sameValue(option, state[option.key], loaded[option.key]));
			}

			function renderStatus(message, tone) {
				const changed = changedOptions();
				saveButton.disabled = busy || !changed.length || !!catalog.readOnly;
				resetButton.disabled = busy || !changed.length;
				dot.hidden = !changed.length;
				fab.setAttribute('aria-label', changed.length ? 'Theme lab, unsaved changes' : 'Theme lab');
				status.className = 'status' + (tone ? ' ' + tone : '');
				if (message) {
					status.textContent = message;
					return;
				}
				const rebuild = options.filter(function (option) { return statusOf(option) === 'rebuild'; }).length;
				status.textContent = !changed.length
					? 'No unsaved changes.'
					: changed.length + (changed.length === 1 ? ' unsaved change' : ' unsaved changes') +
						(rebuild
							? '. ' + rebuild + (rebuild === 1 ? ' option needs' : ' options need') + ' a rebuild and ' + (rebuild === 1 ? 'shows' : 'show') + ' after Save.'
							: ', previewed on this page.');
			}

			// Mirrors the server's quoting: plain when that reads back as the same string, else JSON, which is
			// valid double-quoted YAML.
			function scalar(value) {
				const plain = /^[^\s\-?:,\[\]{}#&*!|>'"%@`]/.test(value) && !/[\u0000-\u001f\u007f]/.test(value) &&
					!/: |\s#|:$|\s$/.test(value) && !/^(null|~|true|false|yes|no|on|off|y|n)$/i.test(value) &&
					!/^[-+]?(\.?[0-9][0-9_]*(\.[0-9_]*)?([eE][-+]?[0-9]+)?|0x[0-9a-fA-F_]+|0o[0-7_]+|0b[01_]+|\.(inf|nan))$/i.test(value);
				return plain ? value : JSON.stringify(value);
			}

			function renderSnippet() {
				const tree = {};
				options.forEach(function (option) {
					const value = state[option.key];
					if (!isSet(value)) return;
					const path = option.key.split('.');
					let node = tree;
					for (let i = 0; i < path.length - 1; i++) node = node[path[i]] = node[path[i]] || {};
					node[path[path.length - 1]] = option.kind === 'toggle' ? (same(value, 'true') ? 'true' : 'false') : scalar(value);
				});
				const lines = [];
				(function write(node, indent) {
					Object.keys(node).forEach(function (key) {
						if (typeof node[key] === 'object') {
							lines.push(indent + key + ':');
							write(node[key], indent + '  ');
						} else {
							lines.push(indent + key + ': ' + node[key]);
						}
					});
				})(tree, '    ');
				snippetCode.textContent = lines.length
					? 'theme:\n  options:\n' + lines.join('\n') + '\n'
					: '# Every option is at its default: nothing to write under theme.options.\n';
			}

			function persistPending() {
				if (changedOptions().length) session.set(pendingKey, JSON.stringify({ version: catalog.version, state: state }));
				else session.remove(pendingKey);
			}

			function update() {
				options.forEach(renderControl);
				applyPreview();
				renderStatus();
				renderSnippet();
				persistPending();
			}

			function notice(tone, text) {
				return el('p', { className: 'notice ' + tone, text: text });
			}

			function showNotices() {
				const list = [];
				const note = session.get(noteKey);
				if (note) {
					session.remove(noteKey);
					list.push(notice('ok', note));
				}
				if (catalog.readOnly) list.push(notice('warn', catalog.readOnly + ' Show YAML has the values to copy instead.'));
				if (catalog.customTheme) {
					list.push(notice('warn', 'theme.name is "' + catalog.themeName + '": these are the default theme\'s options, and a custom theme may not read them.'));
				}
				// A stored pick only counts while the page shows it: the theme drops picks whose section is off.
				const picks = options.filter(function (option) {
					const stored = option.readerStorageKeys.length === 1 ? local.get(option.readerStorageKeys[0]) : null;
					return stored && same(stored, initial[option.attribute]) && !same(stored, effective(option, loaded));
				}).map(function (option) { return option.label.toLowerCase(); });
				if (picks.length) {
					list.push(notice('info', 'This browser has its own Appearance panel picks (' + picks.join(', ') +
						'), which pages show instead of the site\'s values. Saving an option clears its pick.'));
				}
				notices.replaceChildren.apply(notices, list);
			}

			function setup(data) {
				catalog = data;
				options = data.options;
				fileName.textContent = data.configFile;
				fileName.title = data.configFile;
				options.forEach(function (option) {
					loaded[option.key] = option.value;
					if (option.attribute) initial[option.attribute] = html.getAttribute(option.attribute);
				});
				initial['data-theme'] = html.getAttribute('data-theme');
				state = Object.assign({}, loaded);
				let pending = null;
				try { pending = JSON.parse(session.get(pendingKey) || 'null'); } catch (e) { }
				if (pending && pending.version === data.version && pending.state) {
					options.forEach(function (option) {
						if (Object.prototype.hasOwnProperty.call(pending.state, option.key)) state[option.key] = pending.state[option.key];
					});
				} else {
					session.remove(pendingKey);
				}
				buildForm();
				showNotices();
				update();
			}

			function load() {
				if (!loading) {
					loading = fetch(api + 'catalog', { cache: 'no-store', headers: { Accept: 'application/json' } })
						.then(function (response) {
							return response.json().then(function (body) {
								if (!response.ok) throw new Error(body && body.error || 'HTTP ' + response.status);
								return body;
							});
						})
						.then(setup)
						.catch(function (error) {
							loading = null;
							notices.replaceChildren(notice('error', 'The theme lab couldn\'t load its options: ' + error.message));
						});
				}
				return loading;
			}

			function open(focus) {
				panel.hidden = false;
				fab.setAttribute('aria-expanded', 'true');
				session.set(openKey, '1');
				load().then(function () {
					if (focus && !panel.hidden) (form.querySelector('select, input') || title).focus();
				});
			}

			function close(focus) {
				panel.hidden = true;
				fab.setAttribute('aria-expanded', 'false');
				session.remove(openKey);
				if (focus) fab.focus();
			}

			function copyText(text) {
				const area = el('textarea', { readonly: true, style: 'position:fixed;top:0;left:0;opacity:0' });
				area.value = text;
				document.body.appendChild(area);
				area.select();
				let copied = false;
				try { copied = document.execCommand('copy'); } catch (e) { }
				area.remove();
				copyButton.focus();
				return copied;
			}

			fab.addEventListener('click', function () {
				if (panel.hidden) open(true);
				else close(false);
			});
			closeButton.addEventListener('click', function () { close(true); });
			root.addEventListener('keydown', function (event) {
				if (event.key === 'Escape' && !panel.hidden) {
					event.preventDefault();
					event.stopPropagation();
					close(true);
				}
			});

			yamlButton.addEventListener('click', function () {
				const show = snippet.hidden;
				snippet.hidden = !show;
				yamlButton.setAttribute('aria-expanded', show ? 'true' : 'false');
				yamlButton.textContent = show ? 'Hide YAML' : 'Show YAML';
			});

			copyButton.addEventListener('click', function () {
				const text = snippetCode.textContent;
				const done = function () { renderStatus('Copied the YAML.', 'ok'); };
				const fallback = function () {
					if (copyText(text)) done();
					else renderStatus('Copying failed: select the YAML and copy it.', 'error');
				};
				if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(text).then(done, fallback);
				else fallback();
			});

			resetButton.addEventListener('click', function () {
				state = Object.assign({}, loaded);
				options.forEach(function (option) {
					if (option.kind === 'text') controls[option.key].input.value = effective(option, state);
				});
				update();
				renderStatus('Back to the values in ' + catalog.configFile + '.');
			});

			saveButton.addEventListener('click', function () {
				const changed = changedOptions();
				if (busy || !catalog || !changed.length) return;
				const values = {};
				changed.forEach(function (option) {
					const value = state[option.key];
					values[option.key] = !isSet(value) ? null : option.kind === 'toggle' ? same(value, 'true') : value;
				});
				busy = true;
				renderStatus('Saving...');
				fetch(api + 'save', {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({ version: catalog.version, values: values })
				})
					.then(function (response) {
						return response.json().then(
							function (body) { return { ok: response.ok, body: body }; },
							function () { return { ok: false, body: { error: 'The server answered ' + response.status + '.' } }; });
					})
					.then(function (result) {
						busy = false;
						if (!result.ok) {
							renderStatus(result.body.error || 'Saving failed.', 'error');
							return;
						}
						// A reader's own pick would hide the saved value after the reload.
						changed.forEach(function (option) {
							option.readerStorageKeys.forEach(function (key) { local.remove(key); });
						});
						loaded = Object.assign({}, state);
						catalog.version = result.body.version;
						session.remove(pendingKey);
						if (!result.body.saved) {
							update();
							renderStatus(result.body.message, 'ok');
							return;
						}
						// The page keeps the preview until mokadocs serve has rebuilt the site and reloads it.
						saved = true;
						session.set(noteKey, 'Saved ' + result.body.changed.join(', ') + ' to ' + catalog.configFile + '.');
						options.forEach(renderControl);
						renderStatus('Saved to ' + catalog.configFile + '. Rebuilding the site...', 'ok');
					})
					.catch(function () {
						busy = false;
						renderStatus('Couldn\'t reach mokadocs serve. Is it still running?', 'error');
					});
			});

			if (session.get(openKey)) open(false);
			else if (session.get(pendingKey)) load();
		})();
		""";

	/// <summary>The script served at <c>/api/theme-lab/lab.js</c>, with the styles inlined.</summary>
	internal static string Script() => _js.Replace("__LAB_CSS__", JsonSerializer.Serialize(_css), StringComparison.Ordinal);
}
