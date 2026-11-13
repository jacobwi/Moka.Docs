using System.Text.Json;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _runtimeJs = """
	                                   /* MokaDocs Default Theme JS */
	                                   (function() {
	                                       'use strict';

	                                       const html = document.documentElement;
	                                       html.classList.remove('no-js');

	                                       // localStorage throws on every access when the browser blocks storage (privacy settings,
	                                       // sandboxed frames). One unguarded read used to stop the whole runtime.
	                                       const store = {
	                                           get: function(key) { try { return window.localStorage.getItem(key); } catch (e) { return null; } },
	                                           set: function(key, value) { try { window.localStorage.setItem(key, value); } catch (e) { } },
	                                           remove: function(key) { try { window.localStorage.removeItem(key); } catch (e) { } }
	                                       };

	                                       // The theme's text in the site's language with the site's own wording (site.language, theme.options.strings).
	                                       // The build fills it in with the keys this script uses.
	                                       const strings = /*theme-strings*/{};

	                                       // The text of a key, each {name} in it replaced by values[name] in one pass, so a value is never filled in again.
	                                       function i18n(key, values) {
	                                           const text = Object.prototype.hasOwnProperty.call(strings, key) ? strings[key] : key;
	                                           if (!values) return text;
	                                           return text.replace(/\{(\w+)\}/g, function(match, name) {
	                                               return Object.prototype.hasOwnProperty.call(values, name) ? String(values[name]) : match;
	                                           });
	                                       }

	                                       // A counted text: the plural form the page's language uses for the count (key.one, key.other, or a form such as
	                                       // key.few that the site added), else key.other.
	                                       let pluralRules = null;
	                                       function i18nCount(key, count, values) {
	                                           if (pluralRules === null) {
	                                               try { pluralRules = new Intl.PluralRules(html.getAttribute('lang') || 'en'); } catch (e) { pluralRules = false; }
	                                           }
	                                           const form = key + '.' + (pluralRules ? pluralRules.select(count) : count === 1 ? 'one' : 'other');
	                                           return i18n(Object.prototype.hasOwnProperty.call(strings, form) ? form : key + '.other', Object.assign({ count: count }, values));
	                                       }

	                                       function closest(target, selector) {
	                                           return target && target.closest ? target.closest(selector) : null;
	                                       }

	                                       function mediaMatches(query) {
	                                           return !!(window.matchMedia && window.matchMedia(query).matches);
	                                       }

	                                       // Smooth only when the reader allows motion and the site has animations on.
	                                       function scrollBehavior() {
	                                           return html.hasAttribute('data-no-animations') || mediaMatches('(prefers-reduced-motion: reduce)') ? 'auto' : 'smooth';
	                                       }

	                                       // html has scroll-behavior: smooth, which a plain scrollBy would follow.
	                                       function jumpBy(dy) {
	                                           const previous = html.style.scrollBehavior;
	                                           html.style.scrollBehavior = 'auto';
	                                           window.scrollBy(0, dy);
	                                           html.style.scrollBehavior = previous;
	                                       }

	                                       const focusableSelector = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]';

	                                       function focusableIn(container) {
	                                           return Array.prototype.filter.call(container.querySelectorAll(focusableSelector), function(el) {
	                                               return el.getAttribute('tabindex') !== '-1' && el.getClientRects().length > 0;
	                                           });
	                                       }

	                                       function trapFocus(e, container) {
	                                           const items = focusableIn(container);
	                                           if (items.length === 0) {
	                                               e.preventDefault();
	                                               return;
	                                           }
	                                           const first = items[0];
	                                           const last = items[items.length - 1];
	                                           const active = document.activeElement;
	                                           if (e.shiftKey && (active === first || !container.contains(active))) {
	                                               e.preventDefault();
	                                               last.focus();
	                                           } else if (!e.shiftKey && (active === last || !container.contains(active))) {
	                                               e.preventDefault();
	                                               first.focus();
	                                           }
	                                       }

	                                       function restoreFocus(el) {
	                                           if (el && el.focus && document.contains(el)) el.focus();
	                                       }

	                                       // --header-offset is a calc() of the bar, the tabs row and the floating gap, and 0px while
	                                       // autohide has hidden the bar. An element sized by it resolves it to pixels.
	                                       let offsetProbe = null;
	                                       function headerOffset() {
	                                           if (!offsetProbe) {
	                                               offsetProbe = document.createElement('div');
	                                               offsetProbe.setAttribute('aria-hidden', 'true');
	                                               offsetProbe.style.cssText = 'position:absolute;top:0;left:0;width:0;height:var(--header-offset,0px);visibility:hidden;pointer-events:none';
	                                               document.body.appendChild(offsetProbe);
	                                           }
	                                           return offsetProbe.offsetHeight;
	                                       }

	                                       // One requestAnimationFrame pass for everything that follows the scroll position. Each task
	                                       // reads layout and returns its writes, which run after every task has read, so no task forces
	                                       // a layout that another one has just invalidated.
	                                       const frameTasks = [];
	                                       let frameQueued = false;

	                                       function onFrame(task) {
	                                           frameTasks.push(task);
	                                       }

	                                       function runFrame() {
	                                           frameQueued = false;
	                                           const state = { y: window.scrollY, viewport: window.innerHeight, height: html.scrollHeight };
	                                           frameTasks.map(function(task) { return task(state); }).forEach(function(write) { if (write) write(); });
	                                       }

	                                       function queueFrame() {
	                                           if (frameQueued) return;
	                                           frameQueued = true;
	                                           window.requestAnimationFrame(runFrame);
	                                       }

	                                       // The Appearance panel marks its checked options from these attributes, so a change made elsewhere (the dark
	                                       // mode button, a style that starts dark) shows there too.
	                                       const appearanceListeners = [];

	                                       function appearanceChanged() {
	                                           appearanceListeners.forEach(function(listener) { listener(); });
	                                       }

	                                       function onAppearanceChange(listener) {
	                                           appearanceListeners.push(listener);
	                                           listener();
	                                       }

	                                       new MutationObserver(appearanceChanged).observe(html, { attributes: true, attributeFilter: ['data-theme', 'data-style', 'data-color-theme', 'data-code-theme', 'data-code-style', 'data-text-size', 'data-reader-width'] });

	                                       // Dark mode toggle
	                                       const storedTheme = store.get('mokadocs-theme');
	                                       if (storedTheme) {
	                                           html.setAttribute('data-theme', storedTheme);
	                                       } else if (mediaMatches('(prefers-color-scheme: dark)')) {
	                                           html.setAttribute('data-theme', 'dark');
	                                       }

	                                       // Dark code themes and their light counterparts, and the styles that start dark. The theme
	                                       // fills both in from the same lists as the head script on every page.
	                                       const codeThemePairs = /*code-theme-pairs*/{};
	                                       const darkFirstStyles = /*dark-first-styles*/[];

	                                       // Every style but the default has a stylesheet of its own, versioned by its content. A page links the
	                                       // site's style and the head script the reader's; another one loads when the reader picks it.
	                                       const styleFiles = /*style-files*/{};
	                                       const styleLoads = {};

	                                       // Calls done(true) once the style's stylesheet applies, done(false) if it fails to load. A new one goes
	                                       // right after main.css: the styles never match together, and the layout variants' stylesheet must
	                                       // stay after every style.
	                                       function whenStyleLoaded(style, done) {
	                                           const version = Object.prototype.hasOwnProperty.call(styleFiles, style) ? styleFiles[style] : '';
	                                           if (!version) {
	                                               done(true);
	                                               return;
	                                           }
	                                           const path = '/_theme/css/style-' + style + '.css?v=' + version;
	                                           let load = styleLoads[style];
	                                           if (!load) {
	                                               load = styleLoads[style] = { done: false, ok: false, waiting: [] };
	                                               if (document.querySelector('link[rel="stylesheet"][href$="' + path + '"]')) {
	                                                   load.done = load.ok = true;
	                                               } else {
	                                                   const link = document.createElement('link');
	                                                   const finish = function(ok) {
	                                                       load.done = true;
	                                                       load.ok = ok;
	                                                       if (!ok) {
	                                                           delete styleLoads[style];
	                                                           link.remove();
	                                                       }
	                                                       load.waiting.splice(0).forEach(function(callback) { callback(ok); });
	                                                   };
	                                                   link.setAttribute('rel', 'stylesheet');
	                                                   link.addEventListener('load', function() { finish(true); });
	                                                   link.addEventListener('error', function() { finish(false); });
	                                                   link.setAttribute('href', (html.getAttribute('data-base-path') || '') + path);
	                                                   const main = document.querySelector('link[rel="stylesheet"][href*="/_theme/css/main.css"]');
	                                                   if (main) main.after(link);
	                                                   else document.head.appendChild(link);
	                                               }
	                                           }
	                                           if (load.done) done(load.ok);
	                                           else load.waiting.push(done);
	                                       }

	                                       // Whatever sets data-style, its stylesheet follows. The panel loads the stylesheet first instead, so a
	                                       // pick there never shows half styled.
	                                       function followStyle() {
	                                           onAppearanceChange(function() { whenStyleLoaded(html.getAttribute('data-style') || 'default', function() { }); });
	                                       }

	                                       // The code theme for a light or dark page: a theme with a counterpart swaps with it, the
	                                       // others stay. The head bootstrap derives it the same way on every page.
	                                       function codeThemeFor(mode, theme) {
	                                           if (mode === 'light') return codeThemePairs[theme] || theme;
	                                           for (const dark in codeThemePairs) {
	                                               if (codeThemePairs[dark] === theme) return dark;
	                                           }
	                                           return theme;
	                                       }

	                                       // Light or dark for a reader who hasn't chosen a mode: a dark-first style starts dark, the others follow the
	                                       // system. The head script decides the same way on every page.
	                                       function defaultMode() {
	                                           return darkFirstStyles.indexOf(html.getAttribute('data-style')) !== -1 || mediaMatches('(prefers-color-scheme: dark)') ? 'dark' : 'light';
	                                       }

	                                       function pickedCodeTheme() {
	                                           return document.querySelector('[data-appearance="code-theme"]') ? store.get('mokadocs-code-theme') : null;
	                                       }

	                                       // Light and dark are stored and swap the code theme for its counterpart. Only a code theme the reader picked is
	                                       // stored, and kept in step here: storing the switched one every time made it look like a pick and pinned it on
	                                       // later pages. System forgets the stored mode and goes back to the code theme the head script picks without
	                                       // one (the reader's own, else the site's), so the next page looks the same.
	                                       function setMode(mode, siteCodeTheme) {
	                                           if (mode === 'system') {
	                                               store.remove('mokadocs-theme');
	                                               html.setAttribute('data-theme', defaultMode());
	                                               const code = pickedCodeTheme() || siteCodeTheme;
	                                               if (code) html.setAttribute('data-code-theme', code);
	                                               return;
	                                           }
	                                           html.setAttribute('data-theme', mode);
	                                           store.set('mokadocs-theme', mode);
	                                           const code = codeThemeFor(mode, html.getAttribute('data-code-theme'));
	                                           html.setAttribute('data-code-theme', code);
	                                           if (pickedCodeTheme()) store.set('mokadocs-code-theme', code);
	                                       }

	                                       function initDarkToggle() {
	                                           const toggle = document.querySelector('.theme-toggle');
	                                           if (!toggle) return;
	                                           onAppearanceChange(function() {
	                                               toggle.setAttribute('aria-pressed', html.getAttribute('data-theme') === 'dark' ? 'true' : 'false');
	                                           });
	                                           toggle.addEventListener('click', function() {
	                                               setMode(html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
	                                           });
	                                       }

	                                       // A reader who hasn't chosen a mode follows the system while the page is open, too.
	                                       function followSystemMode() {
	                                           const query = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)');
	                                           if (!query || !query.addEventListener) return;
	                                           query.addEventListener('change', function() {
	                                               if (!store.get('mokadocs-theme')) html.setAttribute('data-theme', defaultMode());
	                                           });
	                                       }

	                                       // Header popups: the version list and the Appearance panel. Opening one closes the others, a click
	                                       // outside closes them (in the capture phase, so a click that something stops still counts),
	                                       // and Escape closes the open one and puts focus back on its button.
	                                       const popups = [];

	                                       function closePopups(keep) {
	                                           popups.forEach(function(p) { if (p !== keep) p.close(false); });
	                                       }

	                                       function closeOpenPopup() {
	                                           const open = popups.filter(function(p) { return p.isOpen(); })[0];
	                                           if (!open) return false;
	                                           open.close(true);
	                                           return true;
	                                       }

	                                       function popup(container, trigger, panel, itemSelector, focusOnClick) {
	                                           const self = {
	                                               container: container,
	                                               items: function() { return Array.prototype.slice.call(panel.querySelectorAll(itemSelector)); },
	                                               isOpen: function() { return !panel.hidden; },
	                                               open: function(focus) {
	                                                   closePopups(self);
	                                                   panel.hidden = false;
	                                                   trigger.setAttribute('aria-expanded', 'true');
	                                                   if (!focus) return;
	                                                   const items = self.items();
	                                                   const target = focus === 'last' ? items[items.length - 1] : panel.querySelector('[aria-checked="true"], [aria-current="page"]') || items[0];
	                                                   if (target) target.focus();
	                                               },
	                                               close: function(returnFocus) {
	                                                   if (panel.hidden) return;
	                                                   panel.hidden = true;
	                                                   trigger.setAttribute('aria-expanded', 'false');
	                                                   if (returnFocus) trigger.focus();
	                                               }
	                                           };
	                                           trigger.addEventListener('click', function() {
	                                               if (self.isOpen()) {
	                                                   self.close(false);
	                                               } else {
	                                                   self.open(focusOnClick ? 'current' : null);
	                                               }
	                                           });
	                                           trigger.addEventListener('keydown', function(e) {
	                                               if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;
	                                               e.preventDefault();
	                                               self.open(e.key === 'ArrowUp' ? 'last' : 'current');
	                                           });
	                                           panel.addEventListener('keydown', function(e) {
	                                               const items = self.items();
	                                               const i = items.indexOf(document.activeElement);
	                                               if (i === -1) return;
	                                               let next = null;
	                                               if (e.key === 'ArrowDown' || e.key === 'ArrowRight') next = items[(i + 1) % items.length];
	                                               else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') next = items[(i - 1 + items.length) % items.length];
	                                               else if (e.key === 'Home') next = items[0];
	                                               else if (e.key === 'End') next = items[items.length - 1];
	                                               else if (e.key === 'Tab') self.close(false);
	                                               if (next) {
	                                                   e.preventDefault();
	                                                   next.focus();
	                                               }
	                                           });
	                                           popups.push(self);
	                                           return self;
	                                       }

	                                       // One section of the Appearance panel: a radiogroup with a roving tabindex. The arrow keys move and choose, as
	                                       // in a native radio group, and every choice applies at once.
	                                       function radioGroup(group, current, choose) {
	                                           function radios() {
	                                               return Array.prototype.slice.call(group.querySelectorAll('[role="radio"]'));
	                                           }

	                                           function pick(value) {
	                                               choose(value);
	                                               appearanceChanged();
	                                           }

	                                           group.addEventListener('click', function(e) {
	                                               const radio = closest(e.target, '[role="radio"]');
	                                               if (radio && group.contains(radio)) pick(radio.getAttribute('data-value'));
	                                           });
	                                           group.addEventListener('keydown', function(e) {
	                                               const list = radios();
	                                               const i = list.indexOf(document.activeElement);
	                                               if (i === -1) return;
	                                               let next = -1;
	                                               if (e.key === 'ArrowDown' || e.key === 'ArrowRight') next = (i + 1) % list.length;
	                                               else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') next = (i - 1 + list.length) % list.length;
	                                               else if (e.key === 'Home') next = 0;
	                                               else if (e.key === 'End') next = list.length - 1;
	                                               if (next === -1) return;
	                                               e.preventDefault();
	                                               list[next].focus();
	                                               pick(list[next].getAttribute('data-value'));
	                                           });
	                                           onAppearanceChange(function() {
	                                               const value = current();
	                                               const list = radios();
	                                               const checked = list.filter(function(radio) { return radio.getAttribute('data-value') === value; })[0];
	                                               const tabStop = checked || list[0];
	                                               list.forEach(function(radio) {
	                                                   radio.setAttribute('aria-checked', radio === checked ? 'true' : 'false');
	                                                   radio.setAttribute('tabindex', radio === tabStop ? '0' : '-1');
	                                               });
	                                           });
	                                       }

	                                       // The panel's style, color, code theme and code style options. It builds them the first time it opens, so
	                                       // pages don't carry their markup. The theme fills this in from the lists the head script also uses.
	                                       const appearanceOptions = /*appearance-options*/{};

	                                       function element(tag, attributes, children) {
	                                           const el = document.createElement(tag);
	                                           Object.keys(attributes).forEach(function(name) { el.setAttribute(name, attributes[name]); });
	                                           (children || []).forEach(function(child) { el.appendChild(typeof child === 'string' ? document.createTextNode(child) : child); });
	                                           return el;
	                                       }

	                                       function optionRadio(className, value, children) {
	                                           return element('button', { class: className, type: 'button', role: 'radio', 'aria-checked': 'false', tabindex: '-1', 'data-value': value }, children);
	                                       }

	                                       const optionBuilders = {
	                                           'style': function(group) {
	                                               appearanceOptions['style'].forEach(function(o) {
	                                                   group.appendChild(optionRadio('appearance-option appearance-style', o.name, [
	                                                       element('span', { class: 'style-preview style-preview-' + o.name, 'aria-hidden': 'true' }),
	                                                       element('span', { class: 'appearance-style-text' }, [
	                                                           element('span', { class: 'appearance-style-name' }, [o.label]),
	                                                           element('span', { class: 'appearance-style-desc' }, [o.description])])]));
	                                               });
	                                           },
	                                           'color-theme': function(group) {
	                                               appearanceOptions['color-theme'].forEach(function(o) {
	                                                   group.appendChild(optionRadio('appearance-option appearance-swatch', o.name, [
	                                                       element('span', { class: 'appearance-swatch-dot', 'aria-hidden': 'true', style: 'background:' + o.swatch }),
	                                                       element('span', { class: 'appearance-swatch-label' }, [o.label])]));
	                                               });
	                                           },
	                                           // Dark themes go under the first group label, light ones under the second.
	                                           'code-theme': function(group) {
	                                               const light = group.querySelectorAll('.appearance-group-label')[1] || null;
	                                               appearanceOptions['code-theme'].forEach(function(o) {
	                                                   group.insertBefore(optionRadio('appearance-option appearance-code-theme', o.name, [
	                                                       element('span', { class: 'appearance-code-preview', 'aria-hidden': 'true', style: 'background:' + o.background }),
	                                                       element('span', {}, [o.label])]), o.dark ? light : null);
	                                               });
	                                           },
	                                           'code-style': function(group) {
	                                               appearanceOptions['code-style'].forEach(function(o) {
	                                                   group.appendChild(optionRadio('appearance-option', o.name, [o.label]));
	                                               });
	                                           }
	                                       };

	                                       function buildAppearanceOptions(panel) {
	                                           panel.querySelectorAll('.appearance-options').forEach(function(group) {
	                                               const build = optionBuilders[group.getAttribute('data-appearance')];
	                                               if (build && !group.querySelector('[role="radio"]')) build(group);
	                                           });
	                                           appearanceChanged();
	                                       }

	                                       // The Layout section (theme.options.layoutSelector): a labelled select per part of a docs page. A pick switches the
	                                       // part's attribute on <html> at once and is stored; the head script applies it on later pages. Picking the site's
	                                       // own variant forgets the stored one, so a later change to the site's layout reaches the reader again.
	                                       const layoutParts = appearanceOptions['layout'];

	                                       function layoutAttribute(part) {
	                                           return 'data-' + part.key;
	                                       }

	                                       function layoutStoreKey(part) {
	                                           return 'mokadocs-layout-' + part.key;
	                                       }

	                                       function setLayout(part, variant) {
	                                           if (variant === 'default') html.removeAttribute(layoutAttribute(part));
	                                           else html.setAttribute(layoutAttribute(part), variant);
	                                       }

	                                       // The site's own variants: data-default-layout lists the parts it doesn't leave at default, as part=variant.
	                                       function siteLayout(section) {
	                                           const site = {};
	                                           (section.getAttribute('data-default-layout') || '').split(' ').forEach(function(pair) {
	                                               const at = pair.indexOf('=');
	                                               if (at > 0) site[pair.slice(0, at)] = pair.slice(at + 1);
	                                           });
	                                           return function(part) { return site[part.key] || 'default'; };
	                                       }

	                                       function initLayoutSection(panel) {
	                                           const section = panel.querySelector('.appearance-layout');
	                                           if (!section) return null;
	                                           const siteVariant = siteLayout(section);
	                                           const partOf = function(select) {
	                                               return layoutParts.filter(function(part) { return part.key === select.getAttribute('data-layout-part'); })[0];
	                                           };
	                                           const sync = function() {
	                                               section.querySelectorAll('select').forEach(function(select) {
	                                                   select.value = html.getAttribute(layoutAttribute(partOf(select))) || 'default';
	                                               });
	                                           };
	                                           section.addEventListener('change', function(e) {
	                                               const select = closest(e.target, 'select');
	                                               const part = select && partOf(select);
	                                               if (!part) return;
	                                               setLayout(part, select.value);
	                                               if (select.value === siteVariant(part)) store.remove(layoutStoreKey(part));
	                                               else store.set(layoutStoreKey(part), select.value);
	                                           });
	                                           // Reset and the theme lab change the attributes too.
	                                           new MutationObserver(sync).observe(html, { attributes: true, attributeFilter: layoutParts.map(layoutAttribute) });
	                                           return {
	                                               build: function() {
	                                                   layoutParts.forEach(function(part) {
	                                                       const id = 'appearanceLayout-' + part.key;
	                                                       const options = part.variants.map(function(v) {
	                                                           return element('option', { value: v.name }, [v.name === siteVariant(part) ? i18n('appearance.siteDefault', { label: v.label }) : v.label]);
	                                                       });
	                                                       section.appendChild(element('div', { class: 'appearance-layout-field' }, [
	                                                           element('label', { for: id }, [part.label]),
	                                                           element('select', { id: id, 'data-layout-part': part.key }, options)]));
	                                                   });
	                                                   sync();
	                                               },
	                                               reset: function() {
	                                                   layoutParts.forEach(function(part) { setLayout(part, siteVariant(part)); });
	                                               }
	                                           };
	                                       }

	                                       // Every key the panel stores. Reset removes them all, the sections the site turned off included.
	                                       const appearanceKeys = ['mokadocs-theme', 'mokadocs-style', 'mokadocs-color-theme', 'mokadocs-code-theme', 'mokadocs-code-style', 'mokadocs-text-size', 'mokadocs-reader-width']
	                                           .concat(layoutParts.map(layoutStoreKey));

	                                       // The Appearance panel: a non-modal dialog under its header button, with one radiogroup per setting the site
	                                       // offers and the Layout section's selects. The head script applies a stored choice only while its section is on, so
	                                       // turning one off in the configuration also drops what readers chose with it.
	                                       function initAppearance() {
	                                           const container = document.querySelector('.appearance');
	                                           const trigger = container && container.querySelector('.appearance-trigger');
	                                           const panel = container && container.querySelector('.appearance-panel');
	                                           if (!trigger || !panel) return null;
	                                           const site = {
	                                               style: panel.getAttribute('data-default-style') || 'default',
	                                               colorTheme: panel.getAttribute('data-default-color-theme') || '',
	                                               codeTheme: panel.getAttribute('data-default-code-theme') || '',
	                                               codeStyle: panel.getAttribute('data-default-code-style') || 'plain'
	                                           };

	                                           // The default text size and reading width are no attribute at all, so the page is unchanged.
	                                           function attributeSetting(attribute, key, fallback, removeFallback) {
	                                               return {
	                                                   current: function() { return html.getAttribute(attribute) || fallback; },
	                                                   choose: function(value) {
	                                                       if (removeFallback && value === fallback) html.removeAttribute(attribute);
	                                                       else html.setAttribute(attribute, value);
	                                                       store.set(key, value);
	                                                   }
	                                               };
	                                           }

	                                           // The style shows checked at once and applies once its stylesheet has loaded, so the page never shows it
	                                           // half styled. Only the last pick applies.
	                                           let pendingStyle = null;
	                                           const settings = {
	                                               'mode': {
	                                                   current: function() { return store.get('mokadocs-theme') ? html.getAttribute('data-theme') : 'system'; },
	                                                   choose: function(mode) { setMode(mode, site.codeTheme); }
	                                               },
	                                               'style': {
	                                                   current: function() { return pendingStyle || html.getAttribute('data-style') || 'default'; },
	                                                   choose: function(style) {
	                                                       pendingStyle = style;
	                                                       whenStyleLoaded(style, function(loaded) {
	                                                           if (pendingStyle !== style) return;
	                                                           pendingStyle = null;
	                                                           if (loaded) {
	                                                               html.setAttribute('data-style', style);
	                                                               store.set('mokadocs-style', style);
	                                                               // A dark-first style starts dark, but only for a reader who hasn't chosen a mode.
	                                                               if (!store.get('mokadocs-theme')) html.setAttribute('data-theme', defaultMode());
	                                                           }
	                                                           appearanceChanged();
	                                                       });
	                                                   }
	                                               },
	                                               'color-theme': attributeSetting('data-color-theme', 'mokadocs-color-theme', ''),
	                                               'code-theme': attributeSetting('data-code-theme', 'mokadocs-code-theme', ''),
	                                               'code-style': attributeSetting('data-code-style', 'mokadocs-code-style', 'plain'),
	                                               'text-size': attributeSetting('data-text-size', 'mokadocs-text-size', '100', true),
	                                               'reader-width': attributeSetting('data-reader-width', 'mokadocs-reader-width', 'default', true)
	                                           };
	                                           panel.querySelectorAll('.appearance-options').forEach(function(group) {
	                                               const setting = settings[group.getAttribute('data-appearance')];
	                                               if (setting) radioGroup(group, setting.current, setting.choose);
	                                           });
	                                           const layout = initLayoutSection(panel);

	                                           const reset = panel.querySelector('.appearance-reset');
	                                           if (reset) {
	                                               reset.addEventListener('click', function() {
	                                                   appearanceKeys.forEach(function(key) { store.remove(key); });
	                                                   pendingStyle = null;
	                                                   html.setAttribute('data-style', site.style);
	                                                   html.setAttribute('data-theme', defaultMode());
	                                                   if (site.colorTheme) html.setAttribute('data-color-theme', site.colorTheme);
	                                                   else html.removeAttribute('data-color-theme');
	                                                   if (site.codeTheme) html.setAttribute('data-code-theme', site.codeTheme);
	                                                   html.setAttribute('data-code-style', site.codeStyle);
	                                                   html.removeAttribute('data-text-size');
	                                                   html.removeAttribute('data-reader-width');
	                                                   if (layout) layout.reset();
	                                                   appearanceChanged();
	                                               });
	                                           }

	                                           let built = false;
	                                           const self = {
	                                               container: container,
	                                               isOpen: function() { return !panel.hidden; },
	                                               open: function() {
	                                                   if (!built) {
	                                                       built = true;
	                                                       buildAppearanceOptions(panel);
	                                                       if (layout) layout.build();
	                                                   }
	                                                   closePopups(self);
	                                                   panel.hidden = false;
	                                                   trigger.setAttribute('aria-expanded', 'true');
	                                                   const first = panel.querySelector('[role="radio"][tabindex="0"]') || panel.querySelector('.appearance-close');
	                                                   if (first) first.focus();
	                                               },
	                                               close: function(returnFocus) {
	                                                   if (panel.hidden) return;
	                                                   panel.hidden = true;
	                                                   trigger.setAttribute('aria-expanded', 'false');
	                                                   if (returnFocus) trigger.focus();
	                                               }
	                                           };
	                                           trigger.addEventListener('click', function() {
	                                               if (self.isOpen()) {
	                                                   self.close(false);
	                                               } else {
	                                                   self.open();
	                                               }
	                                           });
	                                           const close = panel.querySelector('.appearance-close');
	                                           if (close) close.addEventListener('click', function() { self.close(true); });
	                                           // Focus leaving the panel and its button (Tab past the last option) closes the panel. It isn't modal.
	                                           container.addEventListener('focusout', function(e) {
	                                               if (self.isOpen() && e.relatedTarget && !container.contains(e.relatedTarget)) self.close(false);
	                                           });
	                                           popups.push(self);
	                                           return self;
	                                       }

	                                       // A list of links: opening it with the mouse leaves focus on the button, the arrow keys move
	                                       // through the versions.
	                                       function initVersionMenu() {
	                                           const container = document.querySelector('.version-selector');
	                                           const trigger = container && container.querySelector('.version-trigger');
	                                           const panel = container && container.querySelector('.version-dropdown');
	                                           if (!trigger || !panel) return null;
	                                           return popup(container, trigger, panel, '.version-option', false);
	                                       }

	                                       // Mobile nav toggle and the "On this page" drawer. Only one of them is open at a time.
	                                       const sidebar = document.getElementById('mokadocs-sidebar');
	                                       const mobileToggle = document.querySelector('.mobile-nav-toggle');
	                                       const tocSidebar = document.getElementById('tocSidebar');
	                                       const tocToggle = document.getElementById('tocToggle');
	                                       let navBackdrop = null;
	                                       let tocBackdrop = null;

	                                       // A dimmed layer under an open drawer. A click on it lands outside the drawer, which closes it,
	                                       // and focus goes back to the button that opens it. The CSS decides at which widths it shows.
	                                       function createBackdrop(name, toggle) {
	                                           const backdrop = document.createElement('div');
	                                           backdrop.className = 'drawer-backdrop ' + name;
	                                           backdrop.setAttribute('aria-hidden', 'true');
	                                           backdrop.addEventListener('click', function() { toggle.focus(); });
	                                           document.body.appendChild(backdrop);
	                                           return backdrop;
	                                       }

	                                       function setNavOpen(open, returnFocus) {
	                                           if (!sidebar || !mobileToggle || sidebar.classList.contains('open') === open) return false;
	                                           if (open) setTocOpen(false);
	                                           sidebar.classList.toggle('open', open);
	                                           mobileToggle.setAttribute('aria-expanded', open ? 'true' : 'false');
	                                           if (navBackdrop) navBackdrop.classList.toggle('open', open);
	                                           if (open) {
	                                               // The sidebar is hidden on load here, so it could not be scrolled to the current page then.
	                                               const current = sidebar.querySelector('.nav-link.current');
	                                               if (current) current.scrollIntoView({ block: 'center' });
	                                               const target = current || focusableIn(sidebar)[0];
	                                               if (target) target.focus({ preventScroll: true });
	                                           } else if (returnFocus) {
	                                               mobileToggle.focus();
	                                           }
	                                           return true;
	                                       }

	                                       function setTocOpen(open, returnFocus) {
	                                           if (!tocSidebar || !tocToggle || tocSidebar.classList.contains('open') === open) return false;
	                                           if (open) setNavOpen(false);
	                                           tocSidebar.classList.toggle('open', open);
	                                           tocToggle.setAttribute('aria-expanded', open ? 'true' : 'false');
	                                           if (tocBackdrop) tocBackdrop.classList.toggle('open', open);
	                                           if (open) {
	                                               const target = tocSidebar.querySelector('.toc-item a.active') || focusableIn(tocSidebar)[0];
	                                               if (target) target.focus({ preventScroll: true });
	                                           } else if (returnFocus) {
	                                               tocToggle.focus();
	                                           }
	                                           return true;
	                                       }

	                                       function initMobileNav() {
	                                           if (!sidebar || !mobileToggle) return;
	                                           navBackdrop = createBackdrop('nav-backdrop', mobileToggle);
	                                           mobileToggle.addEventListener('click', function() {
	                                               setNavOpen(!sidebar.classList.contains('open'));
	                                           });
	                                           sidebar.addEventListener('click', function(e) {
	                                               if (closest(e.target, 'a.nav-link') && mediaMatches('(max-width: 768px)')) setNavOpen(false);
	                                           });
	                                       }

	                                       // Hides and shows the sidebar on wide screens (theme.options.sidebarToggle). The inline script after
	                                       // the sidebar applied the stored choice before the first paint.
	                                       function initSidebarToggle() {
	                                           const button = document.getElementById('sidebarToggle');
	                                           if (!button) return;
	                                           button.addEventListener('click', function() {
	                                               const hidden = !html.classList.contains('sidebar-hidden');
	                                               html.classList.toggle('sidebar-hidden', hidden);
	                                               button.setAttribute('aria-expanded', hidden ? 'false' : 'true');
	                                               if (hidden) store.set('mokadocs-sidebar-hidden', 'true');
	                                               else store.remove('mokadocs-sidebar-hidden');
	                                           });
	                                       }

	                                       function initTocDrawer() {
	                                           if (!tocSidebar || !tocToggle) return;
	                                           tocBackdrop = createBackdrop('toc-backdrop', tocToggle);
	                                           tocToggle.addEventListener('click', function() {
	                                               setTocOpen(!tocSidebar.classList.contains('open'));
	                                           });
	                                           tocSidebar.addEventListener('click', function(e) {
	                                               if (closest(e.target, '.toc-item a')) setTocOpen(false);
	                                           });
	                                       }

	                                       // Code blocks. The build highlights them and wraps each <pre> in a .code-block; this puts the
	                                       // language label, line numbers and Copy button into the <pre> ahead of the <code>, which is the
	                                       // part that scrolls, so they stay in place. The <pre> stays the last child of the wrapper: the REPL
	                                       // plugin's toolbar goes after it.
	                                       (function() {
	                                           const blocks = document.querySelectorAll('.code-block');
	                                           if (!blocks.length) return;
	                                           const noCopy = html.hasAttribute('data-no-copy-button');
	                                           const noNumbers = html.hasAttribute('data-no-line-numbers');
	                                           let status = null;
	                                           if (!noCopy) {
	                                               status = document.createElement('div');
	                                               status.className = 'code-copy-status';
	                                               status.setAttribute('role', 'status');
	                                               document.body.appendChild(status);
	                                           }

	                                           // The code as the reader should paste it: removed diff lines and hunk headers left out, the
	                                           // +/- signs are CSS. Markers like [!code ++] were taken out at build time.
	                                           function codeText(code) {
	                                               const lines = code.querySelectorAll(':scope > .line');
	                                               if (!lines.length) return code.textContent;
	                                               const kept = [];
	                                               lines.forEach(function(line) {
	                                                   if (!line.classList.contains('diff-remove') && !line.classList.contains('diff-hunk')) kept.push(line.textContent);
	                                               });
	                                               return kept.join('\n');
	                                           }

	                                           // Plain http has no Clipboard API; execCommand still works there.
	                                           function copyText(text) {
	                                               if (navigator.clipboard && window.isSecureContext) {
	                                                   return navigator.clipboard.writeText(text).catch(function() { return legacyCopy(text); });
	                                               }
	                                               return legacyCopy(text);
	                                           }
	                                           function legacyCopy(text) {
	                                               return new Promise(function(resolve, reject) {
	                                                   const focused = document.activeElement;
	                                                   const area = document.createElement('textarea');
	                                                   area.value = text;
	                                                   area.setAttribute('readonly', '');
	                                                   area.style.position = 'fixed';
	                                                   area.style.left = '-9999px';
	                                                   document.body.appendChild(area);
	                                                   area.select();
	                                                   let ok = false;
	                                                   try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
	                                                   document.body.removeChild(area);
	                                                   if (focused && focused.focus) focused.focus();
	                                                   if (ok) resolve(); else reject();
	                                               });
	                                           }

	                                           function announce(text) {
	                                               status.textContent = '';
	                                               setTimeout(function() { status.textContent = text; }, 100);
	                                           }

	                                           blocks.forEach(function(block) {
	                                               const pre = block.querySelector(':scope > pre');
	                                               const code = pre && pre.querySelector(':scope > code');
	                                               if (!code) return;

	                                               const lang = block.getAttribute('data-lang');
	                                               if (lang) {
	                                                   const label = document.createElement('span');
	                                                   label.className = 'code-lang';
	                                                   label.setAttribute('aria-hidden', 'true');
	                                                   label.textContent = lang;
	                                                   pre.insertBefore(label, code);
	                                               }

	                                               const setting = block.getAttribute('data-line-numbers');
	                                               const count = code.querySelectorAll(':scope > .line').length || code.textContent.replace(/\n$/, '').split('\n').length;
	                                               const numbered = setting === 'true' || (setting !== 'false' && !noNumbers && count >= 3);
	                                               if (numbered && block.closest('.page-content, .landing-content')) {
	                                                   block.classList.add('has-line-numbers');
	                                                   if (!block.hasAttribute('data-wrap')) {
	                                                       const gutter = document.createElement('span');
	                                                       gutter.className = 'line-numbers';
	                                                       gutter.setAttribute('aria-hidden', 'true');
	                                                       let numbers = '';
	                                                       for (let n = 1; n <= count; n++) numbers += '<span>' + n + '</span>';
	                                                       gutter.innerHTML = numbers;
	                                                       pre.insertBefore(gutter, code);
	                                                   }
	                                               }

	                                               if (noCopy) return;
	                                               const button = document.createElement('button');
	                                               button.type = 'button';
	                                               button.className = 'copy-btn';
	                                               button.textContent = i18n('code.copy');
	                                               button.setAttribute('aria-label', i18n('code.copyLabel'));
	                                               let reset = 0;
	                                               button.addEventListener('click', function() {
	                                                   copyText(codeText(code)).then(function() { done(true); }, function() { done(false); });
	                                               });
	                                               function done(ok) {
	                                                   button.textContent = ok ? i18n('code.copied') : i18n('code.failed');
	                                                   button.classList.toggle('copied', ok);
	                                                   announce(ok ? i18n('code.copied') : i18n('copy.failed'));
	                                                   clearTimeout(reset);
	                                                   reset = setTimeout(function() {
	                                                       button.textContent = i18n('code.copy');
	                                                       button.classList.remove('copied');
	                                                   }, 2000);
	                                               }
	                                               pre.insertBefore(button, code);
	                                           });
	                                       })();

	                                       // The NuGet install widgets: the API index's and each :::package block's. Their tabs use
	                                       // the content tabs' tablist code below.
	                                       function initInstallWidget() {
	                                           document.querySelectorAll('.package-install-widget').forEach(initPackageWidget);
	                                       }

	                                       function initPackageWidget(widget, n) {
	                                           const tabs = Array.prototype.slice.call(widget.querySelectorAll('.install-tab'));
	                                           const panels = tabs.map(function(tab) { return widget.querySelector('.install-panel[data-panel="' + tab.getAttribute('data-tab') + '"]'); });
	                                           linkTabs(tabs, panels, n ? 'install-' + n : 'install');
	                                           activateTab(tabs, panels, Math.max(0, tabs.findIndex(function(tab) { return tab.classList.contains('active'); })));
	                                           widget.addEventListener('click', function(e) {
	                                               const tab = closest(e.target, '.install-tab');
	                                               if (tab) activateTab(tabs, panels, tabs.indexOf(tab));
	                                           });
	                                           widget.addEventListener('keydown', function(e) {
	                                               const tab = closest(e.target, '.install-tab');
	                                               const next = tab && tabKeyTarget(e, tabs, tab);
	                                               if (!next) return;
	                                               e.preventDefault();
	                                               next.focus();
	                                               activateTab(tabs, panels, tabs.indexOf(next));
	                                           });
	                                           widget.querySelectorAll('.install-copy-btn').forEach(function(button) {
	                                               button.addEventListener('click', function() {
	                                                   const code = button.parentElement.querySelector('code');
	                                                   if (!code) return;
	                                                   copyText(code.textContent, function(ok) {
	                                                       button.textContent = ok ? i18n('copy.copied') : i18n('copy.failed');
	                                                       setTimeout(function() { button.textContent = i18n('code.copy'); }, 2000);
	                                                   });
	                                               });
	                                           });
	                                       }

	                                       // navigator.clipboard exists only in secure contexts, so a site served over plain http copies
	                                       // through a selected textarea instead.
	                                       function copyText(text, done) {
	                                           function fallback() {
	                                               const focused = document.activeElement;
	                                               const area = document.createElement('textarea');
	                                               area.value = text;
	                                               area.setAttribute('readonly', '');
	                                               area.style.position = 'fixed';
	                                               area.style.left = '-9999px';
	                                               document.body.appendChild(area);
	                                               area.select();
	                                               let ok = false;
	                                               try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
	                                               document.body.removeChild(area);
	                                               restoreFocus(focused);
	                                               done(ok);
	                                           }
	                                           if (navigator.clipboard && window.isSecureContext) {
	                                               navigator.clipboard.writeText(text).then(function() { done(true); }, fallback);
	                                           } else {
	                                               fallback();
	                                           }
	                                       }

	                                       // Heading anchor links. The anchor follows the heading's text, so the heading's accessible
	                                       // name starts with its own words.
	                                       function initHeadingAnchors() {
	                                           document.querySelectorAll('.page-content h2[id], .page-content h3[id], .page-content h4[id]').forEach(function(heading) {
	                                               const anchor = document.createElement('a');
	                                               anchor.className = 'heading-anchor';
	                                               anchor.href = '#' + heading.id;
	                                               anchor.textContent = '#';
	                                               anchor.setAttribute('aria-label', i18n('heading.anchor', { heading: heading.textContent.trim() }));
	                                               heading.appendChild(anchor);
	                                           });
	                                           document.addEventListener('click', function(e) {
	                                               const anchor = closest(e.target, '.heading-anchor');
	                                               if (!anchor) return;
	                                               e.preventDefault();
	                                               const hash = anchor.getAttribute('href');
	                                               history.replaceState(null, '', hash);
	                                               copyText(window.location.origin + window.location.pathname + hash, function(ok) {
	                                                   if (!ok) return;
	                                                   anchor.textContent = '\u2713';
	                                                   setTimeout(function() { anchor.textContent = '#'; }, 1500);
	                                               });
	                                           });
	                                       }

	                                       // Tabs and code groups. Choosing a label chooses it in every group on the page that has it,
	                                       // and the last few labels are remembered, so "C#" or "CLI" stays chosen on later pages.
	                                       const tabHeaderSelector = '.tabs > .tab-headers > .tab-header';

	                                       function tabHeaders(group) {
	                                           return Array.prototype.slice.call(group.querySelectorAll(':scope > .tab-headers > .tab-header'));
	                                       }

	                                       function tabPanels(group) {
	                                           return Array.prototype.slice.call(group.querySelectorAll(':scope > .tab-content'));
	                                       }

	                                       function tabLabel(header) {
	                                           return header.textContent.trim();
	                                       }

	                                       // Tablist code shared by the content tabs and the install widget. Only the selected tab is in
	                                       // the Tab order; the arrow keys, Home and End move between tabs.
	                                       function linkTabs(headers, panels, key) {
	                                           headers.forEach(function(header, i) {
	                                               const panel = panels[i];
	                                               if (!header.id) header.id = 'mokadocs-tab-' + key + '-' + i;
	                                               if (!panel) return;
	                                               if (!panel.id) panel.id = 'mokadocs-tabpanel-' + key + '-' + i;
	                                               header.setAttribute('aria-controls', panel.id);
	                                               panel.setAttribute('role', 'tabpanel');
	                                               panel.setAttribute('aria-labelledby', header.id);
	                                               panel.tabIndex = 0;
	                                           });
	                                       }

	                                       function activateTab(headers, panels, index) {
	                                           headers.forEach(function(header, i) {
	                                               header.classList.toggle('active', i === index);
	                                               header.setAttribute('aria-selected', i === index ? 'true' : 'false');
	                                               header.tabIndex = i === index ? 0 : -1;
	                                           });
	                                           panels.forEach(function(panel, i) {
	                                               if (!panel) return;
	                                               panel.classList.toggle('active', i === index);
	                                               panel.hidden = i !== index;
	                                           });
	                                       }

	                                       function tabKeyTarget(e, headers, header) {
	                                           const i = headers.indexOf(header);
	                                           let next = -1;
	                                           if (e.key === 'ArrowRight') next = (i + 1) % headers.length;
	                                           else if (e.key === 'ArrowLeft') next = (i - 1 + headers.length) % headers.length;
	                                           else if (e.key === 'Home') next = 0;
	                                           else if (e.key === 'End') next = headers.length - 1;
	                                           return next === -1 ? null : headers[next];
	                                       }

	                                       function selectTab(group, index) {
	                                           activateTab(tabHeaders(group), tabPanels(group), index);
	                                       }

	                                       function rememberedTabs() {
	                                           try {
	                                               const labels = JSON.parse(store.get('mokadocs-tabs') || '[]');
	                                               return Array.isArray(labels) ? labels : [];
	                                           } catch (e) {
	                                               store.remove('mokadocs-tabs');
	                                               return [];
	                                           }
	                                       }

	                                       function chooseTab(header) {
	                                           const group = header.parentElement.parentElement;
	                                           const label = tabLabel(header);
	                                           const top = header.getBoundingClientRect().top;
	                                           document.querySelectorAll('.tabs').forEach(function(other) {
	                                               const index = other === group ? tabHeaders(group).indexOf(header) : tabHeaders(other).map(tabLabel).indexOf(label);
	                                               if (index !== -1) selectTab(other, index);
	                                           });
	                                           store.set('mokadocs-tabs', JSON.stringify([label].concat(rememberedTabs().filter(function(l) { return l !== label; })).slice(0, 10)));
	                                           // Groups above this one can change height. Keep the chosen tab where the reader clicked it.
	                                           const shift = header.getBoundingClientRect().top - top;
	                                           if (Math.abs(shift) >= 1) jumpBy(shift);
	                                       }

	                                       function initTabs() {
	                                           const remembered = rememberedTabs();
	                                           document.querySelectorAll('.tabs').forEach(function(group, g) {
	                                               const headers = tabHeaders(group);
	                                               linkTabs(headers, tabPanels(group), g);
	                                               const labels = headers.map(tabLabel);
	                                               const preferred = remembered.map(function(label) { return labels.indexOf(label); }).filter(function(i) { return i !== -1; });
	                                               const active = headers.findIndex(function(header) { return header.classList.contains('active'); });
	                                               selectTab(group, preferred.length > 0 ? preferred[0] : Math.max(0, active));
	                                           });
	                                           document.addEventListener('click', function(e) {
	                                               const header = closest(e.target, tabHeaderSelector);
	                                               if (header) chooseTab(header);
	                                           });
	                                           document.addEventListener('keydown', function(e) {
	                                               const header = closest(e.target, tabHeaderSelector);
	                                               const next = header && tabKeyTarget(e, tabHeaders(header.parentElement.parentElement), header);
	                                               if (!next) return;
	                                               e.preventDefault();
	                                               next.focus();
	                                               chooseTab(next);
	                                           });
	                                       }

	                                       // ToC: a caret button collapses an entry's subsections; the link itself only navigates. Built
	                                       // from the li > ul structure, so it works at any depth.
	                                       function initTocCarets() {
	                                           let count = 0;
	                                           document.querySelectorAll('.toc-item').forEach(function(item) {
	                                               const link = item.querySelector(':scope > a');
	                                               const list = item.querySelector(':scope > ul');
	                                               if (!link || !list || !list.querySelector('li')) return;
	                                               if (!list.id) list.id = 'mokadocs-toc-sub-' + (++count);
	                                               const caret = document.createElement('button');
	                                               caret.type = 'button';
	                                               caret.className = 'toc-caret';
	                                               caret.setAttribute('aria-expanded', 'true');
	                                               caret.setAttribute('aria-controls', list.id);
	                                               caret.setAttribute('aria-label', i18n('nav.toggleSection', { label: link.textContent.trim() }));
	                                               link.after(caret);
	                                               item.classList.add('has-children');
	                                           });
	                                           if (count === 0) return;
	                                           document.addEventListener('click', function(e) {
	                                               const caret = closest(e.target, '.toc-caret');
	                                               if (!caret) return;
	                                               const collapsed = caret.parentElement.classList.toggle('collapsed');
	                                               caret.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
	                                           });
	                                       }

	                                       function tocTarget(link) {
	                                           const id = link.getAttribute('href').slice(1);
	                                           try {
	                                               return document.getElementById(id) || document.getElementById(decodeURIComponent(id));
	                                           } catch (e) {
	                                               return null;
	                                           }
	                                       }

	                                       // ToC active tracking and reading progress. The current section is the last heading that has
	                                       // scrolled up to just below the header (--header-offset, so every navbar variant and short
	                                       // viewports work). At the bottom of the page, where the last headings can never get there, it
	                                       // is the last heading on screen.
	                                       function initTocSpy() {
	                                           // A page whose readers can switch the panel's layout has the list twice, beside the page and inline; one entry
	                                           // per heading marks the link in both.
	                                           const headings = [];
	                                           document.querySelectorAll('.toc-item > a[href^="#"]').forEach(function(link) {
	                                               const el = tocTarget(link);
	                                               if (!el) return;
	                                               const known = headings.filter(function(heading) { return heading.el === el; })[0];
	                                               if (known) known.links.push(link);
	                                               else headings.push({ el: el, links: [link] });
	                                           });
	                                           const counter = document.getElementById('tocCounter');
	                                           const progress = document.getElementById('tocProgress');
	                                           if (headings.length === 0 && !progress) return;
	                                           let current = null;
	                                           headerOffset();
	                                           onFrame(function(state) {
	                                               const line = headerOffset() + 32;
	                                               const scrollable = state.height - state.viewport > 1;
	                                               const atBottom = scrollable && state.y > 0 && state.y + state.viewport >= state.height - 2;
	                                               let active = null;
	                                               let lastOnScreen = null;
	                                               headings.forEach(function(heading) {
	                                                   const rect = heading.el.getBoundingClientRect();
	                                                   if (rect.width === 0 && rect.height === 0) return;
	                                                   if (rect.top <= line) active = heading;
	                                                   if (rect.top < state.viewport) lastOnScreen = heading;
	                                               });
	                                               if (atBottom && lastOnScreen) active = lastOnScreen;
	                                               const ratio = scrollable ? Math.min(1, Math.max(0, state.y / (state.height - state.viewport))) : 0;
	                                               return function() {
	                                                   if (progress) progress.style.transform = 'scaleX(' + ratio + ')';
	                                                   if (active === current) return;
	                                                   if (current) {
	                                                       current.links.forEach(function(link) {
	                                                           link.classList.remove('active');
	                                                           link.removeAttribute('aria-current');
	                                                       });
	                                                   }
	                                                   if (active) {
	                                                       active.links.forEach(function(link) {
	                                                           link.classList.add('active');
	                                                           link.setAttribute('aria-current', 'location');
	                                                       });
	                                                   }
	                                                   if (counter) counter.textContent = active ? i18nCount('toc.position', headings.length, { index: headings.indexOf(active) + 1 }) : '';
	                                                   current = active;
	                                               };
	                                           });
	                                       }

	                                       // Back to the top of the page. Focus goes to the skip link, the first thing on the page, so
	                                       // the next Tab starts from the top too.
	                                       function scrollToTop() {
	                                           window.scrollTo({ top: 0, behavior: scrollBehavior() });
	                                           const skip = document.querySelector('.skip-link');
	                                           if (skip) skip.focus({ preventScroll: true });
	                                       }

	                                       function initBackToTop() {
	                                           const tocBackToTop = document.getElementById('tocBackToTop');
	                                           if (tocBackToTop) {
	                                               tocBackToTop.addEventListener('click', function(e) {
	                                                   e.preventDefault();
	                                                   scrollToTop();
	                                               });
	                                           }
	                                           const button = document.getElementById('backToTop');
	                                           if (!button) return;
	                                           button.addEventListener('click', scrollToTop);
	                                           onFrame(function(state) {
	                                               const show = state.y > 300;
	                                               return function() { button.classList.toggle('visible', show); };
	                                           });
	                                       }

	                                       // Search. Entries are lowercased once when the index arrives. A term matches as a substring of
	                                       // the title (10 points), heading (5), tags (3) or text and code words (1). A term of five or
	                                       // more letters that matches nowhere may match a word one typo away (a letter added, dropped,
	                                       // changed or swapped with its neighbor) at the start of the word, so a word still being typed
	                                       // counts too. Those results rank below every result that matched exactly.
	                                       function prepareSearchIndex(data) {
	                                           const vocabulary = new Set();
	                                           const entries = data.map(function(e) {
	                                               const item = {
	                                                   entry: e,
	                                                   t: String(e.t || '').toLowerCase(),
	                                                   s: String(e.s || '').toLowerCase(),
	                                                   k: String(e.k || '').toLowerCase(),
	                                                   c: (String(e.c || '') + ' ' + String(e.x || '')).toLowerCase(),
	                                                   words: new Set()
	                                               };
	                                               (item.t + ' ' + item.s + ' ' + item.k + ' ' + item.c).split(/[^a-z0-9_\u00c0-\uffff]+/).forEach(function(word) {
	                                                   if (word.length < 4) return;
	                                                   item.words.add(word);
	                                                   vocabulary.add(word);
	                                               });
	                                               return item;
	                                           });
	                                           return { entries: entries, vocabulary: Array.from(vocabulary) };
	                                       }

	                                       function withinOneEdit(a, b) {
	                                           if (a === b) return true;
	                                           if (Math.abs(a.length - b.length) > 1) return false;
	                                           let i = 0;
	                                           while (i < a.length && i < b.length && a[i] === b[i]) i++;
	                                           if (a.length > b.length) return a.slice(i + 1) === b.slice(i);
	                                           if (a.length < b.length) return a.slice(i) === b.slice(i + 1);
	                                           return a.slice(i + 1) === b.slice(i + 1) || (a[i] === b[i + 1] && a[i + 1] === b[i] && a.slice(i + 2) === b.slice(i + 2));
	                                       }

	                                       function typoMatches(term, vocabulary) {
	                                           const found = [];
	                                           if (term.length < 5) return found;
	                                           for (let i = 0; i < vocabulary.length && found.length < 20; i++) {
	                                               const word = vocabulary[i];
	                                               if (word.length < term.length - 1 || word.indexOf(term) !== -1) continue;
	                                               if (withinOneEdit(term, word.slice(0, term.length)) || withinOneEdit(term, word.slice(0, term.length + 1)) || withinOneEdit(term, word.slice(0, term.length - 1))) {
	                                                   found.push(word);
	                                               }
	                                           }
	                                           return found;
	                                       }

	                                       // Chinese, Japanese and Korean are written without spaces between words, so a query such as 検索設定 is one term
	                                       // that a page writing 検索の設定 doesn't contain. Such a term that matches nowhere as written still matches where
	                                       // at least two thirds of its pairs of neighboring characters appear, ranked with the typo matches below every
	                                       // exact one.
	                                       const cjkPattern = /[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\uac00-\ud7af]/;

	                                       function characterPairs(term) {
	                                           const pairs = [];
	                                           for (let i = 0; i + 1 < term.length; i++) {
	                                               const pair = term.slice(i, i + 2);
	                                               if (pairs.indexOf(pair) === -1) pairs.push(pair);
	                                           }
	                                           return pairs;
	                                       }

	                                       function pairMatch(item, term, fields) {
	                                           if (term.length < 3 || !cjkPattern.test(term)) return null;
	                                           const pairs = characterPairs(term);
	                                           const found = pairs.filter(function(pair) {
	                                               return fields.some(function(field) { return item[field[0]].indexOf(pair) !== -1; });
	                                           });
	                                           if (found.length < Math.ceil(pairs.length * 2 / 3)) return null;
	                                           let score = 0;
	                                           fields.forEach(function(field) {
	                                               if (found.some(function(pair) { return item[field[0]].indexOf(pair) !== -1; })) score += field[1];
	                                           });
	                                           return { score: score, words: found };
	                                       }

	                                       function searchIndex(index, query) {
	                                           const fields = [['t', 10], ['s', 5], ['k', 3], ['c', 1]];
	                                           const terms = query.toLowerCase().split(/\s+/).filter(Boolean);
	                                           const near = terms.map(function(term) { return typoMatches(term, index.vocabulary); });
	                                           const results = [];
	                                           index.entries.forEach(function(item) {
	                                               let score = 0;
	                                               let typos = 0;
	                                               const words = [];
	                                               for (let i = 0; i < terms.length; i++) {
	                                                   let exact = 0;
	                                                   let fuzzy = 0;
	                                                   let typo = null;
	                                                   for (let f = 0; f < fields.length; f++) {
	                                                       if (item[fields[f][0]].indexOf(terms[i]) !== -1) exact += fields[f][1];
	                                                   }
	                                                   if (exact === 0) {
	                                                       const pairs = pairMatch(item, terms[i], fields);
	                                                       if (pairs) {
	                                                           score += pairs.score;
	                                                           typos++;
	                                                           words.push.apply(words, pairs.words);
	                                                           continue;
	                                                       }
	                                                       typo = near[i].filter(function(word) { return item.words.has(word); })[0] || null;
	                                                       if (!typo) return;
	                                                       for (let f = 0; f < fields.length; f++) {
	                                                           if (item[fields[f][0]].indexOf(typo) !== -1) fuzzy += fields[f][1];
	                                                       }
	                                                       typos++;
	                                                   }
	                                                   score += exact || fuzzy;
	                                                   words.push(typo || terms[i]);
	                                               }
	                                               if (terms.length > 0) results.push({ entry: item.entry, score: score, typos: typos, words: words });
	                                           });
	                                           results.sort(function(a, b) { return a.typos - b.typos || b.score - a.score; });
	                                           return results;
	                                       }

	                                       function renderSearchResult(item, i) {
	                                           const e = item.entry;
	                                           const snippet = searchSnippet(e.c || '', item.words);
	                                           const sectionPart = e.s ? '<span class="search-result-section">&rsaquo; ' + esc(e.s) + '</span>' : '';
	                                           return '<a class="search-result" id="search-result-' + i + '" role="option" aria-selected="false" tabindex="-1" href="' + esc(e.r) + '">' +
	                                               '<div><span class="search-result-title">' + esc(e.t) + '</span>' + sectionPart +
	                                               ' <span class="search-result-category">' + esc(e.g) + '</span></div>' +
	                                               (snippet ? '<div class="search-result-snippet">' + snippet + '</div>' : '') +
	                                               '</a>';
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

	                                       // The dialog keeps the last query, selected, when it opens again. The index is fetched the
	                                       // first time it opens, and a query typed before the index arrives runs when it does.
	                                       function initSearch() {
	                                           const modal = document.getElementById('searchModal');
	                                           const input = document.getElementById('searchInput');
	                                           const results = document.getElementById('searchResults');
	                                           if (!modal || !input || !results) return null;
	                                           const dialog = modal.querySelector('.search-dialog') || modal;
	                                           const message = document.getElementById('searchMessage');
	                                           const status = document.getElementById('searchStatus');
	                                           let index = null;
	                                           let loading = false;
	                                           let failed = false;
	                                           let opener = null;
	                                           let timer = 0;
	                                           let activeIdx = -1;

	                                           function show(text, announcement) {
	                                               if (message) {
	                                                   message.textContent = text;
	                                                   message.hidden = !text;
	                                               }
	                                               if (status) status.textContent = announcement;
	                                           }

	                                           function load() {
	                                               if (index || loading) return;
	                                               loading = true;
	                                               failed = false;
	                                               fetch(modal.getAttribute('data-index') || (html.dataset.basePath || '') + '/search-index.json')
	                                                   .then(function(r) { if (!r.ok) throw new Error(String(r.status)); return r.json(); })
	                                                   .then(function(data) { index = prepareSearchIndex(Array.isArray(data) ? data : []); })
	                                                   .catch(function() { failed = true; })
	                                                   .then(function() {
	                                                       loading = false;
	                                                       if (!modal.hidden) run();
	                                                   });
	                                           }

	                                           function select(i) {
	                                               const items = results.querySelectorAll('.search-result');
	                                               activeIdx = items.length === 0 ? -1 : Math.max(0, Math.min(i, items.length - 1));
	                                               items.forEach(function(item, j) {
	                                                   item.classList.toggle('active', j === activeIdx);
	                                                   item.setAttribute('aria-selected', j === activeIdx ? 'true' : 'false');
	                                               });
	                                               if (activeIdx === -1) {
	                                                   input.removeAttribute('aria-activedescendant');
	                                               } else {
	                                                   input.setAttribute('aria-activedescendant', items[activeIdx].id);
	                                                   items[activeIdx].scrollIntoView({ block: 'nearest' });
	                                               }
	                                           }

	                                           function run() {
	                                               clearTimeout(timer);
	                                               timer = 0;
	                                               const query = input.value.trim();
	                                               const found = query && index ? searchIndex(index, query).slice(0, 20) : [];
	                                               results.innerHTML = found.map(renderSearchResult).join('');
	                                               input.setAttribute('aria-expanded', found.length > 0 ? 'true' : 'false');
	                                               select(0);
	                                               if (!query) {
	                                                   show(i18n('search.prompt'), '');
	                                               } else if (index) {
	                                                   const none = i18n('search.noResults', { query: query });
	                                                   show(found.length === 0 ? none : '', found.length === 0 ? none : i18nCount('search.resultCount', found.length));
	                                               } else if (failed) {
	                                                   show(i18n('search.unavailable'), i18n('search.unavailable'));
	                                               } else {
	                                                   show(i18n('search.loading'), i18n('search.loading'));
	                                                   load();
	                                               }
	                                           }

	                                           function open() {
	                                               if (!modal.hidden) return;
	                                               closePopups(null);
	                                               setNavOpen(false);
	                                               setTocOpen(false);
	                                               opener = document.activeElement;
	                                               modal.hidden = false;
	                                               input.focus();
	                                               input.select();
	                                               load();
	                                               run();
	                                           }

	                                           function close(returnFocus) {
	                                               if (modal.hidden) return false;
	                                               if (modal.contains(document.activeElement)) document.activeElement.blur();
	                                               modal.hidden = true;
	                                               clearTimeout(timer);
	                                               timer = 0;
	                                               if (returnFocus) restoreFocus(opener);
	                                               opener = null;
	                                               return true;
	                                           }

	                                           input.addEventListener('input', function() {
	                                               clearTimeout(timer);
	                                               timer = setTimeout(run, 80);
	                                           });
	                                           input.addEventListener('keydown', function(e) {
	                                               if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
	                                                   e.preventDefault();
	                                                   if (timer) run();
	                                                   select(activeIdx + (e.key === 'ArrowDown' ? 1 : -1));
	                                               } else if (e.key === 'Enter') {
	                                                   if (timer) run();
	                                                   const item = results.querySelectorAll('.search-result')[activeIdx];
	                                                   if (item) {
	                                                       e.preventDefault();
	                                                       item.click();
	                                                   }
	                                               }
	                                           });
	                                           // Any result closes the dialog, one on the current page included.
	                                           results.addEventListener('click', function(e) {
	                                               if (closest(e.target, '.search-result')) close(false);
	                                           });
	                                           modal.addEventListener('keydown', function(e) {
	                                               if (e.key === 'Tab') trapFocus(e, dialog);
	                                           });
	                                           modal.querySelectorAll('.search-backdrop, .search-close').forEach(function(el) {
	                                               el.addEventListener('click', function() { close(true); });
	                                           });

	                                           const platform = (navigator.userAgentData && navigator.userAgentData.platform) || navigator.platform || navigator.userAgent || '';
	                                           const apple = /mac|iphone|ipad|ipod/i.test(platform);
	                                           document.querySelectorAll('.search-trigger, .home-search').forEach(function(trigger) {
	                                               trigger.setAttribute('aria-keyshortcuts', apple ? 'Meta+K' : 'Control+K');
	                                               const kbd = trigger.querySelector('kbd');
	                                               if (kbd) kbd.textContent = apple ? '\u2318K' : i18n('search.shortcut');
	                                               trigger.addEventListener('click', open);
	                                           });
	                                           // Only claim Ctrl/Cmd+K when the page has search. The key is compared case-insensitively,
	                                           // so Caps Lock doesn't break it.
	                                           document.addEventListener('keydown', function(e) {
	                                               if (!(e.ctrlKey || e.metaKey) || e.altKey || e.shiftKey || !e.key || e.key.toLowerCase() !== 'k') return;
	                                               e.preventDefault();
	                                               if (modal.hidden) {
	                                                   open();
	                                               } else {
	                                                   close(true);
	                                               }
	                                           });

	                                           return { close: close };
	                                       }

	                                       // Sidebar nav expand/collapse, and the current page scrolled into view on load. The shared
	                                       // sidebar script has done that already when the aside has data-nav-route.
	                                       function initSidebar() {
	                                           if (!sidebar) return;
	                                           if (!sidebar.hasAttribute('data-nav-route')) {
	                                               const current = sidebar.querySelector('.nav-link.current');
	                                               if (current && sidebar.clientHeight) {
	                                                   const offset = current.getBoundingClientRect().top - sidebar.getBoundingClientRect().top;
	                                                   if (offset < 0 || offset + current.offsetHeight > sidebar.clientHeight) {
	                                                       sidebar.scrollTop += offset - sidebar.clientHeight / 3;
	                                                   }
	                                               }
	                                           }
	                                           const settle = new WeakMap();
	                                           sidebar.addEventListener('click', function(e) {
	                                               const toggle = closest(e.target, '.nav-toggle');
	                                               if (!toggle) return;
	                                               // The list belongs to the nearest section. Looking only for .nav-section made a
	                                               // nested section's chevron collapse its parent's list instead of its own.
	                                               const section = toggle.closest('.nav-sub-section, .nav-section');
	                                               const children = section && section.querySelector(':scope > .nav-children');
	                                               if (!children) return;
	                                               // An open list has no max-height: a fixed one cut long lists off. The height is set in
	                                               // pixels only for the animation.
	                                               children.style.maxHeight = children.scrollHeight + 'px';
	                                               void children.offsetHeight;
	                                               const isExpanded = children.classList.toggle('expanded');
	                                               clearTimeout(settle.get(children));
	                                               settle.set(children, setTimeout(() => { children.style.maxHeight = ''; }, 350));
	                                               toggle.setAttribute('aria-expanded', isExpanded ? 'true' : 'false');
	                                           });
	                                       }

	                                       // Auto-hiding navbar (layout.navbar: autohide). It stays while a menu in it is open, while it
	                                       // has focus and while the mobile sidebar it toggles is open. The attribute is read on every pass:
	                                       // readers (the Appearance panel's Layout section) and the theme lab switch it on an open page.
	                                       function initAutohide() {
	                                           const header = document.querySelector('.site-header');
	                                           if (!header) return;
	                                           let lastY = window.scrollY;
	                                           function show() { html.classList.remove('navbar-hidden'); }
	                                           // Switched off: keep up with the position, so switching it on again doesn't count the scrolling done meanwhile.
	                                           function off(y) {
	                                               lastY = y;
	                                               return html.classList.contains('navbar-hidden') ? show : null;
	                                           }
	                                           onFrame(function(state) {
	                                               const y = state.y;
	                                               if (html.getAttribute('data-navbar') !== 'autohide') return off(y);
	                                               if (Math.abs(y - lastY) < 8) return null;
	                                               const hide = y > lastY && y > header.offsetHeight
	                                                   && !header.contains(document.activeElement)
	                                                   && !header.querySelector('[aria-expanded="true"]')
	                                                   && !(sidebar && sidebar.classList.contains('open'));
	                                               lastY = y;
	                                               return function() { html.classList.toggle('navbar-hidden', hide); };
	                                           });
	                                           header.addEventListener('focusin', show);
	                                       }

	                                       // The tabs row (layout.navbar: tabs) scrolls sideways on narrow screens; start it at the
	                                       // current tab.
	                                       function initHeaderTabs() {
	                                           const current = document.querySelector('.header-tab.current');
	                                           const row = current && current.parentElement;
	                                           if (!row || row.scrollWidth <= row.clientWidth) return;
	                                           const left = current.getBoundingClientRect().left - row.getBoundingClientRect().left + row.scrollLeft;
	                                           row.scrollLeft = Math.max(0, left - (row.clientWidth - current.offsetWidth) / 2);
	                                       }

	                                       // A layout variant switched on an open page (the Layout section, the theme lab) can move the header and
	                                       // "On this page": the scroll work runs again, and a tabs row that just appeared starts at the current tab.
	                                       function followLayoutChanges() {
	                                           new MutationObserver(function() {
	                                               queueFrame();
	                                               initHeaderTabs();
	                                           }).observe(html, { attributes: true, attributeFilter: layoutParts.map(layoutAttribute) });
	                                       }

	                                       // Image zoom (theme.options.imageZoom). A content image that isn't a link and isn't tiny opens
	                                       // full size in a dialog; any click, or Escape, closes it and focus goes back to the image.
	                                       function initImageZoom() {
	                                           const none = { close: function() { return false; } };
	                                           if (html.hasAttribute('data-no-image-zoom')) return none;
	                                           let box = null;
	                                           let boxImage = null;
	                                           let boxClose = null;
	                                           let opener = null;

	                                           // An image in a hidden tab has no layout yet; its own size decides then.
	                                           function enable(img) {
	                                               if ((img.offsetWidth || img.naturalWidth) < 96 || (img.offsetHeight || img.naturalHeight) < 64) return;
	                                               img.classList.add('zoomable');
	                                               img.tabIndex = 0;
	                                               img.setAttribute('role', 'button');
	                                               img.setAttribute('aria-haspopup', 'dialog');
	                                               img.setAttribute('aria-label', img.alt ? i18n('image.zoomNamed', { alt: img.alt }) : i18n('image.zoom'));
	                                           }

	                                           function open(img) {
	                                               if (!box) {
	                                                   box = document.createElement('div');
	                                                   box.className = 'lightbox';
	                                                   box.hidden = true;
	                                                   box.setAttribute('role', 'dialog');
	                                                   box.setAttribute('aria-modal', 'true');
	                                                   box.innerHTML = '<button type="button" class="lightbox-close">&times;</button><img class="lightbox-image" alt="">';
	                                                   boxClose = box.querySelector('.lightbox-close');
	                                                   boxClose.setAttribute('aria-label', i18n('image.close'));
	                                                   boxImage = box.querySelector('.lightbox-image');
	                                                   box.addEventListener('click', function() { close(true); });
	                                                   box.addEventListener('keydown', function(e) {
	                                                       if (e.key !== 'Tab') return;
	                                                       e.preventDefault();
	                                                       boxClose.focus();
	                                                   });
	                                                   document.body.appendChild(box);
	                                               }
	                                               opener = img;
	                                               boxImage.removeAttribute('srcset');
	                                               if (img.srcset) {
	                                                   boxImage.srcset = img.srcset;
	                                                   boxImage.sizes = '100vw';
	                                               }
	                                               boxImage.src = img.currentSrc || img.src;
	                                               boxImage.alt = img.alt || '';
	                                               // Never smaller than on the page, where a small image may have been scaled up.
	                                               boxImage.style.minWidth = Math.min(img.offsetWidth, window.innerWidth - 32) + 'px';
	                                               box.setAttribute('aria-label', img.alt ? i18n('image.dialogNamed', { alt: img.alt }) : i18n('image.dialog'));
	                                               box.hidden = false;
	                                               html.classList.add('lightbox-open');
	                                               boxClose.focus();
	                                           }

	                                           function close(returnFocus) {
	                                               if (!box || box.hidden) return false;
	                                               box.hidden = true;
	                                               html.classList.remove('lightbox-open');
	                                               if (returnFocus) restoreFocus(opener);
	                                               opener = null;
	                                               return true;
	                                           }

	                                           document.querySelectorAll('.page-content img, .landing-content img').forEach(function(img) {
	                                               if (closest(img, 'a, button, .mermaid, [data-no-zoom]')) return;
	                                               if (img.complete && img.naturalWidth) {
	                                                   enable(img);
	                                               } else {
	                                                   img.addEventListener('load', function() { enable(img); }, { once: true });
	                                               }
	                                           });
	                                           document.addEventListener('click', function(e) {
	                                               const img = closest(e.target, 'img.zoomable');
	                                               if (img) open(img);
	                                           });
	                                           document.addEventListener('keydown', function(e) {
	                                               if ((e.key !== 'Enter' && e.key !== ' ') || !closest(e.target, 'img.zoomable')) return;
	                                               e.preventDefault();
	                                               open(e.target);
	                                           });
	                                           return { close: close };
	                                       }

	                                       // Mermaid diagrams. The script is only fetched on pages that have a diagram, and
	                                       // runs on every layout; it used to be inlined in the default layout only.
	                                       function initMermaid() {
	                                           const diagrams = document.querySelectorAll('pre.mermaid');
	                                           if (diagrams.length === 0) return;
	                                           // Keep each diagram's source, since Mermaid replaces the element's content with
	                                           // SVG. textContent decodes the entities the build wrote; writing it back with
	                                           // textContent keeps a "<" in the source from turning into markup on re-render.
	                                           diagrams.forEach(function(el) { el.setAttribute('data-mermaid-src', el.textContent); });
	                                           const script = document.createElement('script');
	                                           // An exact version with Subresource Integrity: the browser refuses the file if the CDN
	                                           // ever serves different bytes. Bumping the version means a new hash.
	                                           script.src = 'https://cdn.jsdelivr.net/npm/mermaid@11.17.2/dist/mermaid.min.js';
	                                           script.integrity = 'sha384-EOXBFmc3gx5mb+vn0vPvvGqACToJD24hhacX5Yx+8NUUQrHIle/Qi5Bg9o3zKwW2';
	                                           script.crossOrigin = 'anonymous';
	                                           script.onload = function() {
	                                               function render() {
	                                                   const dark = html.getAttribute('data-theme') === 'dark';
	                                                   // Gantt charts take their width from this setting, not from the page, and came out
	                                                   // 300px wide in any column.
	                                                   const width = Math.max(600, diagrams[0].clientWidth);
	                                                   mermaid.initialize({ startOnLoad: false, theme: dark ? 'dark' : 'default', gantt: { useWidth: width } });
	                                                   diagrams.forEach(function(el) {
	                                                       el.removeAttribute('data-processed');
	                                                       el.textContent = el.getAttribute('data-mermaid-src');
	                                                   });
	                                                   mermaid.run({ nodes: diagrams }).then(keepDiagramsLegible, function() {});
	                                               }
	                                               // Mermaid scales a diagram down to fit its box. A wide one on a phone would shrink
	                                               // to unreadable text, so it stops at 60% of its own size and the box scrolls.
	                                               function keepDiagramsLegible() {
	                                                   diagrams.forEach(function(el) {
	                                                       const svg = el.querySelector('svg');
	                                                       const box = svg && svg.viewBox && svg.viewBox.baseVal;
	                                                       if (box && box.width > 0) svg.style.minWidth = Math.round(box.width * 0.6) + 'px';
	                                                   });
	                                               }
	                                               render();
	                                               new MutationObserver(function(mutations) {
	                                                   if (mutations.some(function(m) { return m.attributeName === 'data-theme'; })) render();
	                                               }).observe(html, { attributes: true, attributeFilter: ['data-theme'] });
	                                           };
	                                           document.head.appendChild(script);
	                                       }

	                                       // Feedback widget
	                                       function initFeedback() {
	                                           const feedbackWidget = document.getElementById('feedbackWidget');
	                                           if (!feedbackWidget) return;
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
	                                               store.set(storageKey, JSON.stringify(helpful));
	                                               showThanks(helpful);
	                                               try {
	                                                   fetch((html.dataset.basePath || '') + '/api/feedback', {
	                                                       method: 'POST',
	                                                       headers: { 'Content-Type': 'application/json' },
	                                                       body: JSON.stringify({ page: window.location.pathname, helpful: helpful })
	                                                   }).catch(function() {});
	                                               } catch(e) {}
	                                           }

	                                           // Restore previous vote
	                                           const stored = store.get(storageKey);
	                                           if (stored === 'true' || stored === 'false') showThanks(stored === 'true');

	                                           btnYes.addEventListener('click', function() { sendFeedback(true); });
	                                           btnNo.addEventListener('click', function() { sendFeedback(false); });
	                                       }

	                                       // Announcement banner (theme.options.banner). The inline script after it hides it before the
	                                       // first paint when the reader dismissed this id; a new id shows it again. While it is on screen
	                                       // the header sits lower, and the drawers follow --banner-offset.
	                                       function initBanner() {
	                                           const banner = document.getElementById('mokadocs-banner');
	                                           if (!banner) return;
	                                           const dismiss = banner.querySelector('.site-banner-close');
	                                           if (dismiss) {
	                                               dismiss.addEventListener('click', function() {
	                                                   store.set('mokadocs-banner-dismissed', banner.getAttribute('data-banner-id') || '');
	                                                   banner.hidden = true;
	                                                   const next = document.querySelector('.site-logo');
	                                                   if (next) next.focus({ preventScroll: true });
	                                                   queueFrame();
	                                               });
	                                           }
	                                           let offset = -1;
	                                           onFrame(function() {
	                                               const visible = banner.hidden ? 0 : Math.max(0, Math.round(banner.getBoundingClientRect().bottom));
	                                               if (visible === offset) return null;
	                                               offset = visible;
	                                               return function() { html.style.setProperty('--banner-offset', visible + 'px'); };
	                                           });
	                                       }

	                                       initDarkToggle();
	                                       followSystemMode();
	                                       followStyle();
	                                       initAppearance();
	                                       initVersionMenu();
	                                       initMobileNav();
	                                       initTocDrawer();
	                                       initSidebar();
	                                       initSidebarToggle();
	                                       initHeadingAnchors();
	                                       initTabs();
	                                       initInstallWidget();
	                                       initTocCarets();
	                                       initTocSpy();
	                                       initBackToTop();
	                                       initAutohide();
	                                       initHeaderTabs();
	                                       followLayoutChanges();
	                                       const search = initSearch();
	                                       const imageZoom = initImageZoom();
	                                       initMermaid();
	                                       initFeedback();
	                                       initBanner();

	                                       // Capture phase: a click that a handler stops from bubbling still closes what it is outside of.
	                                       document.addEventListener('click', function(e) {
	                                           const target = e.target;
	                                           popups.forEach(function(p) { if (p.isOpen() && !p.container.contains(target)) p.close(false); });
	                                           if (sidebar && !sidebar.contains(target) && !(mobileToggle && mobileToggle.contains(target))) setNavOpen(false);
	                                           if (tocSidebar && !tocSidebar.contains(target) && !(tocToggle && tocToggle.contains(target))) setTocOpen(false);
	                                       }, true);

	                                       // Escape closes the topmost thing that is open and returns focus to what opened it.
	                                       document.addEventListener('keydown', function(e) {
	                                           if (e.key !== 'Escape') return;
	                                           if (imageZoom.close(true) || (search && search.close(true)) || closeOpenPopup() || setTocOpen(false, true) || setNavOpen(false, true)) {
	                                               e.preventDefault();
	                                           }
	                                       });

	                                       window.addEventListener('scroll', queueFrame, { passive: true });
	                                       window.addEventListener('resize', queueFrame, { passive: true });
	                                       window.addEventListener('load', queueFrame);
	                                       queueFrame();
	                                   })();

	                                   /* Copy page: copies the page's Markdown copy (page.markdown_url). The not-found page's
	                                      "search the docs" link opens the search dialog. */
	                                   (function() {
	                                       'use strict';

	                                       var copyButton = document.querySelector('.page-copy-markdown');
	                                       if (copyButton && window.fetch && window.Promise) {
	                                           var markdownUrl = copyButton.getAttribute('data-markdown-url');
	                                           var label = copyButton.textContent;
	                                           var cached = null;
	                                           var pending = null;

	                                           var load = function() {
	                                               if (cached !== null) return Promise.resolve(cached);
	                                               if (!pending) {
	                                                   pending = fetch(markdownUrl, { credentials: 'same-origin' })
	                                                       .then(function(response) {
	                                                           if (!response.ok) throw new Error('HTTP ' + response.status);
	                                                           return response.text();
	                                                       })
	                                                       .then(function(text) { cached = text; return text; },
	                                                           function(error) { pending = null; throw error; });
	                                               }
	                                               return pending;
	                                           };

	                                           // execCommand needs no secure context, so it also works on a site served over plain http.
	                                           var copyWithSelection = function(text) {
	                                               var area = document.createElement('textarea');
	                                               area.value = text;
	                                               area.setAttribute('readonly', '');
	                                               area.style.position = 'fixed';
	                                               area.style.left = '-9999px';
	                                               document.body.appendChild(area);
	                                               area.select();
	                                               var copied = false;
	                                               try { copied = document.execCommand('copy'); } catch (e) { copied = false; }
	                                               document.body.removeChild(area);
	                                               return copied;
	                                           };

	                                           var copy = function(text) {
	                                               try {
	                                                   if (navigator.clipboard && window.isSecureContext) {
	                                                       return navigator.clipboard.writeText(text).then(
	                                                           function() { return true; },
	                                                           function() { return copyWithSelection(text); });
	                                                   }
	                                               } catch (e) { }
	                                               return Promise.resolve(copyWithSelection(text));
	                                           };

	                                           var showResult = function(copied) {
	                                               copyButton.textContent = copied ? copyButton.getAttribute('data-copied') || 'Copied!' : copyButton.getAttribute('data-failed') || 'Copy failed';
	                                               setTimeout(function() { copyButton.textContent = label; }, 2000);
	                                           };

	                                           // Fetched on hover or focus, so the click can copy while the browser still counts it as
	                                           // the user's action: Safari refuses a clipboard write that waits for a download.
	                                           var prefetch = function() { load().catch(function() { }); };
	                                           copyButton.addEventListener('pointerenter', prefetch);
	                                           copyButton.addEventListener('focus', prefetch);
	                                           copyButton.addEventListener('click', function() {
	                                               var copying = cached !== null ? copy(cached) : load().then(copy);
	                                               copying.then(showResult, function() { showResult(false); });
	                                           });
	                                           copyButton.hidden = false;
	                                       }

	                                       var searchLink = document.querySelector('[data-open-search]');
	                                       var searchTrigger = document.querySelector('.search-trigger');
	                                       if (searchLink && searchTrigger) {
	                                           searchLink.addEventListener('click', function(e) {
	                                               e.preventDefault();
	                                               searchTrigger.click();
	                                           });
	                                       }
	                                   })();
	                                   """;

	private static readonly string _embeddedJs = _runtimeJs
		.Replace("/*code-theme-pairs*/{}", JsonSerializer.Serialize(
			CodeThemes.All.Where(t => t.IsDark && t.Counterpart is not null).ToDictionary(t => t.Name, t => t.Counterpart)), StringComparison.Ordinal)
		.Replace("/*dark-first-styles*/[]", JsonSerializer.Serialize(
			ThemeStyles.All.Where(s => s.DarkFirst).Select(s => s.Name)), StringComparison.Ordinal);

	/// <summary>
	///     The keys of <see cref="ThemeStrings.All" /> that main.js shows: every <c>i18n('key')</c> and, for
	///     <c>i18nCount('key')</c>, the key's plural forms.
	/// </summary>
	public static IReadOnlyList<string> ScriptStringKeys { get; } =
	[
		.. ScriptKeyCall().Matches(_runtimeJs + _componentsJs)
			.SelectMany(m => m.Groups[1].Value == "Count"
				? ThemeStrings.Keys.Where(k => k.StartsWith(m.Groups[2].Value + ".", StringComparison.Ordinal))
				: [m.Groups[2].Value])
			.Distinct()
			.Order(StringComparer.Ordinal)
	];

	// The appearance options with the site's names for them, and the texts the script shows, as JSON for main.js.
	private static Dictionary<string, string> TextPlaceholders(ThemeText text)
	{
		string options = JsonSerializer.Serialize(new Dictionary<string, object>
		{
			["style"] = ThemeStyles.All.Select(s => new
			{
				name = s.Name, label = text["style." + s.Name], description = text["style." + s.Name + ".description"]
			}),
			["color-theme"] = ColorPresets.All.Select(p => new { name = p.Name, label = text["color." + p.Name], swatch = p.Swatch }),
			["code-theme"] = CodeThemes.All.Select(t => new { name = t.Name, label = t.Label, background = t.Background, dark = t.IsDark }),
			["code-style"] = CodeStyles.All.Select(s => new { name = s.Name, label = text["codeStyle." + s.Name] }),
			["layout"] = ThemeLayouts.ReaderParts.Select(p => new
			{
				key = AttributeName(p.Key),
				label = text["layout." + p.Key],
				variants = p.Names.Select(n => new { name = n, label = text[ThemeStrings.LayoutVariantKey(p, n)] })
			})
		});

		// Plural forms a site added (search.resultCount.few) come along with the key's own.
		var strings = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (string key in ScriptStringKeys)
		{
			strings[key] = text[key];
		}

		foreach ((string key, string value) in text.Strings)
		{
			int dot = key.LastIndexOf('.');
			if (dot > 0 && !strings.ContainsKey(key) && strings.ContainsKey(key[..dot] + ".other"))
			{
				strings[key] = value;
			}
		}

		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["/*appearance-options*/{}"] = options,
			["/*theme-strings*/{}"] = JsonSerializer.Serialize(strings)
		};
	}

	[GeneratedRegex(@"\bi18n(Count)?\('([\w.]+)'")]
	private static partial Regex ScriptKeyCall();

	[GeneratedRegex(@"/\*(?:style-files|appearance-options|theme-strings)\*/\{\}")]
	private static partial Regex ScriptPlaceholder();

	// pageNav -> page-nav: the part's attribute on <html> without data-.
	private static string AttributeName(string key) =>
		string.Concat(key.Select(c => char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : c.ToString()));
}
