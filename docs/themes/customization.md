---
title: Themes & Customization
order: 1
---

# Themes & Customization

MokaDocs ships with one built-in theme. Options under `theme.options` in `mokadocs.yaml` change its colors and turn its features on and off. For changes beyond that, point `theme.name` at a theme folder of your own.

## Default Theme Features

The built-in theme includes:

- **Responsive layout**: on screens 768px wide or less, the sidebar moves behind a menu button.
- **Dark and light mode**: follows the system setting on a first visit, with a header toggle that remembers the reader's choice.
- **Appearance panel**: one header button where readers pick light, dark or system mode, the style, the color, the code theme and frame, the text size and the reading width, on phones too, and, when you turn it on, the page layout. See [Appearance Panel](#appearance-panel).
- **Styles**: ten whole looks (default, moka, minimal, classic, soft, terminal, paper, contrast, glass and brutal) that readers can switch between in the Appearance panel. See [Styles](#styles).
- **Page layouts**: variants for the page width, top bar, footer, "On this page" panel, breadcrumbs, previous/next links, sidebar, landing hero, admonitions, tables, spacing, typeface, headings and links, set in `mokadocs.yaml`. See [Page Layout](#page-layout).
- **Syntax highlighting**: done when the site is built, with sixteen color themes.
- **Search**: a dialog opened from the header button or with `Ctrl+K` / `Cmd+K`. It matches page titles, headings, tags and the first 300 characters of each page's text, and forgives one typo in longer words. See [Search](/guide/search).
- **Table of contents**: built from the page's headings, highlighting the section in view. A caret next to an entry folds its subsections.
- **Image zoom**: clicking an image in the page opens it full size. See [Image Zoom](#image-zoom).
- **Keyboard and screen reader support**: see [Keyboard and Accessibility](#keyboard-and-accessibility).

Links between pages are ordinary page loads; there is no client-side navigation.

## CSS Custom Properties

The default theme's stylesheet defines these custom properties. The default theme has no setting for loading an extra stylesheet, so overriding them takes a custom theme (see [Changing Styles](#changing-styles)). `primaryColor` and `accentColor` set the two brand colors from configuration.

### Colors

| Property | Light | Dark | Used for |
|----------|-------|------|----------|
| `--color-primary` | `#0ea5e9` | unchanged | Borders, icons, tints and the code line highlight |
| `--color-primary-text` | the preset's text shade | the preset's text shade | Links, inline code, page titles, the current sidebar and table of contents entries. See [Color Theme Presets](#color-theme-presets) |
| `--color-primary-text-hover` | 80% of `--color-primary-text`, 20% black | 75% of `--color-primary-text`, 25% white | Link hover, the end of the page title gradient |
| `--color-primary-fill` | `--color-primary-text` | `--color-primary-text` | Filled buttons: back to top, "On this page", Get Started, step numbers |
| `--color-on-primary` | `#ffffff` | `#0f172a` | Text and icons on `--color-primary-fill` |
| `--color-primary-light` | `#38bdf8` | unchanged | Lighter shade of the primary color (the landing feature cards' top edge) |
| `--color-primary-dark` | `#0284c7` | unchanged | Darker shade of the primary color (the inline code border tint) |
| `--color-accent` | `#f59e0b` | unchanged | Highlight behind matched words in search results |
| `--color-bg` | `#ffffff` | `#0f172a` | Page background |
| `--color-bg-secondary` | `#f8fafc` | `#1e293b` | Secondary surfaces and hover backgrounds |
| `--color-bg-code` | `#f1f5f9` | `#1e293b` | Inline code background |
| `--color-text` | `#1e293b` | `#e2e8f0` | Body text |
| `--color-text-secondary` | `#64748b` | `#94a3b8` | Secondary text |
| `--color-text-muted` | `#667488` | `#8091a9` | Muted text, at least 4.5:1 on both backgrounds |
| `--color-border` | `#e2e8f0` | `#334155` | Borders and dividers |
| `--color-border-light` | `#f1f5f9` | `#1e293b` | Defined, but not used by the default stylesheet |
| `--gradient-secondary` | `#06b6d4` | unchanged | Second color of the landing page title gradient |
| `--gradient-tertiary` | `#3b82f6` | unchanged | Third color of the landing page title gradient |

Code block colors come from the selected [code theme](#code-syntax-themes), which sets `--sh-comment`, `--sh-string`, `--sh-keyword` and the other `--sh-*` properties. The language label and the Copy button use `--sh-chrome` and the `--sh-copy-*` properties, one set for the dark code themes and one for the light ones.

### Layout

| Property | Default | Description |
|----------|---------|-------------|
| `--sidebar-width` | `280px` | Width of the sidebar |
| `--toc-width` | `220px` | Width of the "On this page" column |
| `--header-height` | `60px` | Height of the header, also used to offset sticky elements |
| `--content-max-width` | `780px` | Maximum width of the page content. The reader's [reading width](#appearance-panel) overrides it |

### Typography and Shape

| Property | Default |
|----------|---------|
| `--font-body` | `'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif` |
| `--font-mono` | `'JetBrains Mono', 'Fira Code', 'Cascadia Code', monospace` |
| `--radius` | `8px` |
| `--radius-sm` | `4px` |
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` |
| `--shadow-md` | `0 4px 6px -1px rgba(0,0,0,0.1)` |
| `--shadow-lg` | `0 10px 25px -3px rgba(0,0,0,0.15)` |
| `--transition` | `150ms ease` |

The theme doesn't load web fonts, so Inter and JetBrains Mono are only used when they're installed on the reader's machine. The body font size (`1rem`, 16px at the browser's default size) and line height (1.7) are fixed values, not properties. Every font size in the theme and its styles is in `rem`, so the text follows the font size set in the reader's browser and the panel's [text size](#appearance-panel).

### Changing Styles

With the default theme, `css_files` contains `/_theme/css/main.css`, then the stylesheet of the configured [style](#styles) unless it is `default` (`/_theme/css/style-moka.css`), then `/_theme/css/layouts.css` when any part of [`layout`](#page-layout) isn't `default`. Each URL is followed by a `?v=` content hash. There is no `customCss` option. A `.css` file in the docs folder is copied to the output, but no page links to it.

That leaves two options:

- `primaryColor` and `accentColor` in `theme.options` change the brand colors. See [Config-Driven Customization](#config-driven-customization).
- A [custom theme](#choosing-a-theme) can ship its own stylesheets. It replaces the default theme entirely, layouts included. Building a site with the default theme writes its stylesheets to `_theme/css/` (`main.css`, a `style-<name>.css` for each style and `layouts.css`), its icons to `_theme/icons.svg` and its script to `_theme/js/main.js`, which you can copy into your theme as a starting point. The default layouts are defined in `EmbeddedThemeProvider.cs` in the MokaDocs source.

A stylesheet in a custom theme can override any of the properties above:

```css
/* my-theme/css/overrides.css (sorts after main.css) */
:root {
    --sidebar-width: 320px;
    --font-body: system-ui, sans-serif;
}
```

## Choosing a Theme

`theme.name` selects which theme renders the site. The default is the theme built into MokaDocs:

```yaml
theme:
  name: default
```

Any other value is a path to a theme folder, absolute or relative to `mokadocs.yaml`:

```yaml
theme:
  name: ./my-theme
```

### Theme Directory Layout

```
my-theme/
  layouts/
    default.html      # required: used by every page unless front matter names another layout
    landing.html      # optional: used by pages with `layout: landing`
  partials/
    footer.html       # optional: available to layouts as partials.footer
  css/
    main.css
  js/
    main.js
  assets/
    logo.svg
```

Layouts are [Scriban](https://github.com/scriban/scriban) templates. A minimal `default.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>{{ page.title | html.escape }} - {{ site.title | html.escape }}</title>
    {{ for css in css_files }}<link rel="stylesheet" href="{{ css | html.escape }}" />
    {{ end }}
</head>
<body>
    <main>{{ page.content }}</main>
    {{ for js in js_files }}<script src="{{ js | html.escape }}"></script>
    {{ end }}
</body>
</html>
```

Everything under `css/`, `js/` and `assets/`, subfolders included, is copied to `_theme/` in the output. `css_files` lists every `.css` file under `css/`, subfolders included, in alphabetical order of path. `js_files` lists only the `.js` files directly inside `js/`. Each URL ends in `?v=` and a hash of the file, so browsers and CDNs fetch a changed file instead of a cached copy. The built-in stylesheet and script are not included, so a custom theme owns its styling completely.

A few things to know when writing layouts:

- The variables a layout can use are listed in [Template Variables](/advanced/architecture#template-variables).
- Variables hold raw text. Write them through `html.escape`, as above, in element text and in attributes: a route or a label can contain `"`, `<` or `&`. `page.content`, `partials` and the `icon` and `icon_svg` fields are HTML and go in as they are.
- Partials are inserted as raw text: `{{ partials.footer }}` outputs the file as written, without evaluating Scriban expressions in it. Scriban's `include` doesn't work in MokaDocs layouts. See [Partials](/advanced/architecture#partials).
- A page whose front matter names a layout the theme doesn't have renders with `default.html`, and the build reports a warning for each missing layout name.
- The build still generates `404.html` from its own markup, which links `_theme/css/main.css` and `_theme/js/main.js`.
- `mokadocs serve` caches the theme for the life of the process, so restart it after editing theme files.

### Fallback Behaviour

When the theme folder doesn't exist or contains no `layouts/*.html`, MokaDocs builds the site with the default theme and reports a warning. A typo in `theme.name` produces a styled site plus a warning rather than a failed build. `mokadocs build` counts the warning in its summary and lists it with `--verbose`, and `mokadocs validate` lists it.

## Config-Driven Customization

`theme.options` in `mokadocs.yaml` customizes the default theme without writing CSS. The full list of options is in [Site Configuration](/configuration/site-config).

To try presets, styles, colors, code themes, layout variants and the banner on your own pages first, open the [theme lab](/advanced/dev-server#theme-lab) from the button at the bottom left of any page `mokadocs serve` serves. It previews them on the page and saves the ones you keep to `mokadocs.yaml`, leaving your comments and formatting alone.

### Feature Toggles

| Option | Default | Controls |
|--------|---------|----------|
| `showSearch` | `true` | Search button, search dialog and the `Ctrl+K` shortcut. Search also needs `features.search.enabled` |
| `showDarkModeToggle` | `true` | Dark mode button in the header |
| `showTableOfContents` | `true` | "On this page" column. Front matter `toc: false` hides it on one page |
| `tocDepth` | `3` | Deepest heading level in the table of contents, from 2 to 6. The page's title heading is never listed |
| `showBreadcrumbs` | `true` | Breadcrumb trail above the content |
| `showPrevNext` | `true` | Previous and next page links below the content |
| `showBackToTop` | `true` | Back-to-top button |
| `showCopyButton` | `true` | Copy button on code blocks |
| `showLineNumbers` | `true` | Line numbers on code blocks of three or more lines |
| `imageZoom` | `true` | [Image zoom](#image-zoom) on content images |
| `showVersionSelector` | `true` | Version menu in the header, when versioning is enabled |
| `showFeedback` | `true` | [Feedback widget](#feedback-widget) |
| `showLastUpdated` | `true` | [Last updated](#last-updated) date |
| `showEditLink` | `false` | [Edit link](#edit-links) |
| `showAnimations` | `false` | [Animations](#animations) |
| `showBuiltWith` | `true` | ["Built with MokaDocs"](#footer-customization) footer text |

The breadcrumbs, previous and next links, back-to-top button, feedback widget, last updated date and edit link appear in the default layout only, not on landing pages. The version menu appears in both.

### Theme Presets

A preset picks a style, layout variants and, for some, a color preset and a code theme in one line:

```yaml
theme:
  options:
    preset: book
```

The preset only fills in the options you leave out. Anything you set yourself wins, one key at a time, so you can start from a preset and change a single part:

```yaml
theme:
  options:
    preset: product
    style: glass          # the rest of product still applies
    layout:
      footer: minimal     # product's other layout parts still apply
```

| Preset | Look | Sets |
|---|---|---|
| `reference` | Classic reference docs | `style: classic`, `layout.navbar: tabs`, `layout.footer: columns`, `layout.breadcrumbs: chevrons`, `layout.tables: striped` |
| `terminal` | Monospace and dark first, compact | `style: terminal`, `defaultColorTheme: moka-red`, `codeTheme: tokyo-night`, `layout.navbar: compact`, `layout.breadcrumbs: path`, `layout.footer: minimal`, `layout.pageNav: minimal`, `layout.density: compact` |
| `book` | Serif text, the page contents inline, quiet navigation | `style: paper`, `layout.toc: inline`, `layout.pageNav: minimal`, `layout.footer: minimal`, `layout.breadcrumbs: back`, `layout.font: serif` |
| `product` | Rounded and roomy, for a product site | `style: soft`, `layout.navbar: floating`, `layout.footer: columns`, `layout.hero: split`, `layout.heroBackground: gradient`, `layout.admonitions: filled` |
| `minimal` | Out of the way while reading | `style: minimal`, `layout.navbar: autohide`, `layout.toc: minimal`, `layout.footer: minimal`, `layout.breadcrumbs: back`, `layout.pageNav: minimal`, `layout.tables: minimal` |
| `accessible` | High contrast, the system font, spacious | `style: contrast`, `layout.font: system`, `layout.density: spacious` |
| `moka` | Dense, dark first, red accent | `style: moka`, `defaultColorTheme: moka-red`, `codeTheme: catppuccin-mocha`, `layout.navbar: compact`, `layout.density: compact` |

A preset only sets what pages start with. Readers can still pick another style, color preset or code theme in the [Appearance panel](#appearance-panel), and the `show*` toggles still apply. The `MOKADOCS_THEME_PRESET` environment variable picks a preset for one build; a variable for a single option, such as `MOKADOCS_STYLE`, wins over the preset's value. An unknown name applies nothing, and the build reports a warning that `mokadocs validate` lists. [`options.style`](/configuration/site-config#options.style) and [`options.layout`](/configuration/site-config#options.layout) describe the values.

The ASP.NET Core host has the same presets in `MokaDocsOptions.Preset` (see [ASP.NET Core Integration](/guide/aspnetcore#theme-presets)).

### Appearance Panel

The sliders button in the header opens the Appearance panel, where readers change how the site looks to them. It opens under the button on wide screens. On screens 768px wide or less it fills the screen below the header and takes the place of the dark mode button. Each section has its own option and shows only while that option is on:

| Section | Choices | Option (default) | Saved as |
|---|---|---|---|
| Mode | Light, Dark, System | `showDarkModeToggle` (on) | `mokadocs-theme` |
| Style | The ten [styles](#styles), each with a preview | `styleSelector` (on) | `mokadocs-style` |
| Color | The eleven [color presets](#color-theme-presets) | `colorThemes` (on) | `mokadocs-color-theme` |
| Code theme | The sixteen [code themes](#code-syntax-themes), dark ones first | `codeThemeSelector` (off) | `mokadocs-code-theme` |
| Code style | The four [code block frames](#code-block-window-styles) | `codeStyleSelector` (off) | `mokadocs-code-style` |
| Text size | 90%, 100%, 112.5%, 125% | `textSizeSelector` (on) | `mokadocs-text-size` |
| Reading width | Narrow, Default, Wide, Wider, Full | `widthSelector` (on) | `mokadocs-reader-width` |
| Layout | A list for each [layout part](#page-layout) of a docs page | `layoutSelector` (off) | `mokadocs-layout-<part>`, such as `mokadocs-layout-page-nav` |

A choice applies at once and is saved in `localStorage`. A script at the top of every page applies the saved choices before the page is drawn, so there is no flash of the site's own look. A saved choice only applies while its section is shown: turning a section off also drops what readers chose with it. With every section off, there is no Appearance button.

- **System** forgets the saved mode. The page goes back to what a first visit gets: the system's light or dark setting, followed live while the page is open, except that `moka` and `terminal` start dark.
- **Text size** scales the root font size, so all text grows or shrinks with it. Column widths stay as they are.
- **Reading width** sets the width of the page column: Narrow is about 60 characters, Wide 1040px, Wider 1280px, and Full takes away every limit, so the column, the header and the footer fill the window. Where the column wouldn't fit between the sidebar and "On this page", the page grows with it: up to 1680px for Wide and 1920px for Wider. A pick never makes the column narrower than the site's own [`layout.width`](#page-layout), except Narrow. See [Reading width and layout.width](#reading-width-and-layout.width).
- **Layout** has one list for each part of a docs page in [Page Layout](#page-layout) except `width` (Reading width covers it) and the landing page's `hero` and `heroBackground`: top bar, footer, "On this page", breadcrumbs, previous and next links, sidebar, what the sidebar lists, admonitions, tables, spacing, font, headings and links. The site's own variant is marked "(default)" and is what the list shows until the reader picks another. Picking it again forgets the saved pick, so a later change to `mokadocs.yaml` reaches that reader too.
- **Reset to site defaults** removes every saved choice, those of hidden sections included, and goes back to the values in `mokadocs.yaml`.

To offer only some of them:

```yaml
theme:
  options:
    textSizeSelector: false
    widthSelector: false
    codeThemeSelector: true
    layoutSelector: true
```

#### Reading width and layout.width

The reader's width and the site's [`layout.width`](#page-layout) work together. Each cell gives the reading column, and the page width where the pick changes it. The column can't be wider than the space between the sidebar and "On this page", so on a 1440px page it stops at about 860px.

| `layout.width` | Narrow | Default | Wide | Wider | Full |
|---|---|---|---|---|---|
| `default` (page 1440px, column 780px) | 60 characters | 780px | 1040px, page 1680px | 1280px, page 1920px | no limit |
| `narrow` (column 640px, centered) | 60 characters | 640px | 1040px, page 1680px | 1280px, page 1920px | no limit |
| `wide` (page 1680px, column 1040px) | 60 characters | 1040px | 1040px | 1280px, page 1920px | no limit |
| `wider` (page 1920px, column 1280px) | 60 characters | 1280px | 1280px | 1280px | no limit |
| `full` (no limit) | 60 characters | no limit | no limit | no limit | no limit |
| `boxed` (a 1280px frame) | 60 characters | 780px | 1040px, in the frame (about 700px) | 1280px, page 1920px, no frame | no limit, no frame |

The panel works with the keyboard and screen readers: see [Keyboard and Accessibility](#keyboard-and-accessibility).

### Styles

A style changes the whole look of the default theme: surfaces, borders, corner radii, shadows, fonts and spacing. Each one works in light and dark mode, with every color preset, code theme and code frame, on every kind of page. Set the style pages start with:

```yaml
theme:
  options:
    style: moka
```

| Style | Look |
|---|---|
| `default` | The original look, used when `style` isn't set. |
| `moka` | Dense and dark first. Near-black surfaces, borders tinted with the primary color, glow rings instead of shadows, uppercase micro-labels for headings in the sidebar, tables and table of contents, and a monospace font for signatures, versions and other technical text. A reader who has never used the dark mode toggle starts in dark mode. Light mode keeps the same structure on a near-white page. |
| `minimal` | Quiet. Almost no borders or shadows, more whitespace, a reading column about 70 characters wide, plain sidebar links with a thin marker on the current page, and flat code blocks. |
| `classic` | Traditional reference docs. A solid header bar in the primary color (near-black in dark mode), a bordered sidebar and panels, small corner radii, dense tables with striped rows, rules under the headings and the system font. |
| `soft` | Rounded. 12px cards with soft layered shadows, tinted section backgrounds, pill-shaped highlights in the sidebar and table of contents, and roomier spacing. |
| `terminal` | A terminal. Monospace text everywhere, dense spacing, square corners, `$` and `#` marks before the headings, the current page in reverse video and an underline in the primary color on whatever has keyboard focus. Starts in dark mode, like `moka`. |
| `paper` | A printed book. Serif text from the fonts already on the reader's system, a narrow reading column, warm off-white paper (warm dark brown in dark mode), small capitals for labels and thin rules in place of boxes. |
| `contrast` | Accessibility first. Body text and links at 7:1 or more and everything else at 4.5:1 or more in both modes, underlined links, a thick focus ring, bold markers on the current page and tab so no state relies on color alone, nothing translucent or blurred, and outlines that hold up in Windows high contrast mode. |
| `glass` | Frosted glass. A translucent, blurred header, sidebar, cards and menus over a soft gradient in the primary color. Where the browser can't blur, or the reader asks the system for less transparency, the panels turn solid. |
| `brutal` | Neo-brutalist. Thick black lines (white in dark mode), flat fills in the primary color, square corners, heavy headings and hard offset shadows instead of soft ones. |

In every style except `default`, text in the primary color is darkened in light mode and lightened in dark mode, and so are the colors of badges and callouts, so they keep a contrast ratio of at least 4.5:1 with each preset. Links in these styles are a deeper or paler shade than the preset's swatch.

An unknown name renders as `default`, and the build reports a warning that `mokadocs validate` lists.

#### Showing the Style Selector

The Style section of the [Appearance panel](#appearance-panel) lists the ten styles, each with a small preview. The reader's pick is saved in `localStorage` and applies to every page before it's drawn, so there is no flash of another style. The section is shown by default, on every screen size.

Every style other than `default` is a stylesheet of its own, `_theme/css/style-<name>.css`, so a page downloads only the styles it shows. Pages link the stylesheet of the configured `style`. When a reader has picked another one, a script in the page's `<head>` loads that stylesheet before the page is drawn. A style picked in the panel shows as selected at once and applies as soon as its stylesheet has loaded.

To give every reader the same style:

```yaml
theme:
  options:
    style: classic
    styleSelector: false
```

With the selector hidden, a style a reader picked earlier no longer applies, and pages carry no script for loading another style's stylesheet.

### Page Layout

`layout` changes how the parts of the page are laid out, independent of the style and the colors. Set only the parts you want to change; the others keep their `default` variant.

```yaml
theme:
  options:
    layout:
      width: wide              # default, wide, wider, full, boxed or narrow
      navbar: tabs             # default, compact, floating, tabs, autohide, centered or transparent
      footer: columns          # default, minimal, columns, none, centered or compact
      toc: floating            # default, minimal, inline, floating, numbered, timeline or bordered
      breadcrumbs: pills       # default, chevrons, pills, path, back, arrows or dots
      pageNav: minimal         # default, minimal, stacked, split or compact
      sidebar: lines           # default, lines, pills, compact, accent, cards or minimal
      sidebarScope: section    # default or section
      home: portal             # default, portal, showcase, minimal or docs
      hero: split              # default, split, minimal or banner
      heroBackground: dots     # default, dots, gradient, none, mesh or lines
      features: bento          # default, list, icons, compact or bento
      admonitions: github      # default, filled, minimal, github, outlined or callout
      tables: striped          # default, striped, bordered, minimal, compact or rounded
      density: compact         # default, compact or spacious
      font: system             # default, system, serif, mono, rounded or humanist
      headings: accent         # default, underlined, accent or numbered
      links: underlined        # default, underlined or subtle
```

Only the default theme reads these. Landing pages use the `home`, `navbar`, `footer`, `hero`, `heroBackground`, `features`, `tables`, `links`, `width` and `font` settings too (see [Home Page](#home-page)). The `show*` options still win, so `showBreadcrumbs: false` hides the breadcrumbs whatever `breadcrumbs` is set to.

`wide` widens the reading column to 1040px on a page up to 1680px wide, and `wider` to 1280px on a page up to 1920px wide.

#### Letting Readers Try the Variants

Every reader gets the layout you set. To let readers try the other variants, turn on [`layoutSelector`](/configuration/site-config#options.layoutselector): the [Appearance panel](#appearance-panel) gets a Layout section with a list for each part of a docs page, your variants marked as the defaults. A pick applies at once, and is saved for the reader's later pages. This site has it on, so open the Appearance panel to try them here.

```yaml
theme:
  options:
    layoutSelector: true
```

Some variants need markup of their own: the tabs row of `navbar: tabs`, the link columns of `footer: columns`, the box of `toc: inline`, and the progress bar, Top link and section counter that `toc: minimal` leaves out. With `layoutSelector` on, every page carries all of it and the stylesheet shows only what the current variants use, and pages always link `_theme/css/layouts.css`. On this site that adds 4 to 9 KB to a page (1 to 2 KB compressed), most of it the footer's link columns and a second copy of the "On this page" list. Without JavaScript, pages show your own variants.

Some pairs work well together:

- `navbar: tabs` with `sidebarScope: section`: the tabs switch between the top-level sections, and the sidebar lists only the current one.
- `width: full` with `sidebar: compact` and `density: compact` for large API references.
- `hero: split` with a `heroCode` sample in the landing page's front matter, to show the library in use first thing.
- `home: portal` with `heroBackground: gradient` for a docs hub whose first thing is the search box.
- `home: showcase` with `hero: split` and `features: bento` for a product page.
- `headings: numbered` with `toc: numbered`: the sections and the "On this page" entries carry the same numbers, which suits specifications and long guides.
- `width: narrow` with `font: humanist` or `serif` and `links: underlined` for prose-heavy docs.

Every variant keeps text at WCAG AA contrast (4.5:1, 3:1 for large text and icons) with every style and color preset, in light and dark mode. `links: subtle` drops the accent color from links in the content, so it marks them with a dotted underline instead and shows a solid one on hover and keyboard focus.

An unknown name renders that part as `default`, and the build reports a warning that `mokadocs validate` lists. [`options.layout`](/configuration/site-config#options.layout) describes every variant.

[`sidebarToggle: true`](/configuration/site-config#options.sidebartoggle) adds a button that hides and shows the sidebar on wide screens. The reader's choice carries over to later pages.

### Primary Color

Set `primaryColor` to change the brand color across the site:

```yaml
theme:
  options:
    primaryColor: "#e11d48"
```

The value becomes `--color-primary` in an inline style, and `--color-primary-light` and `--color-primary-dark` are derived from it with CSS `color-mix()`: 75% of your color mixed with white, and 80% mixed with black. You only set one value.

Text in your color (links, inline code, page titles, the current page in the sidebar) and filled buttons use a text shade that MokaDocs works out when it builds the site, one for light mode and one for dark mode. It keeps the hue and moves the lightness just far enough for a 4.5:1 contrast ratio on the code background, the current sidebar link's tint and the admonition boxes, the same way the presets' shades are made. A color that already passes is used as it is. A light yellow such as `#facc15`, for example, becomes `#876c00` for text in light mode and stays `#facc15` in dark mode.

The value is inserted as written, so any CSS color works. The text shade is worked out for hex, `rgb()`, `hsl()` and named colors; other syntax, such as `oklch()` or `var()`, is used for text as written, so check its contrast yourself:

```yaml
theme:
  options:
    # Hex
    primaryColor: "#0ea5e9"

    # Named color
    primaryColor: "rebeccapurple"

    # HSL
    primaryColor: "hsl(250, 80%, 60%)"

    # RGB
    primaryColor: "rgb(99, 102, 241)"
```

A color preset overrides `primaryColor` while it's applied. See [Color Theme Presets](#color-theme-presets).

### Accent Color

```yaml
theme:
  options:
    accentColor: "#22c55e"
```

`accentColor` sets `--color-accent` (default `#f59e0b`). The default theme uses it to highlight matching words in search results.

### Code Syntax Themes

MokaDocs ships with 16 syntax highlighting themes for code blocks. Set the default with the `codeTheme` option:

```yaml
theme:
  options:
    codeTheme: "catppuccin-mocha"
```

#### Available Themes

| Theme | Style | Description |
|---|---|---|
| `catppuccin-mocha` | Dark | Warm dark theme with pastel accents (default) |
| `catppuccin-latte` | Light | Light companion to catppuccin-mocha |
| `github-dark` | Dark | GitHub's dark syntax colors |
| `github-light` | Light | GitHub's light syntax colors |
| `dracula` | Dark | The Dracula color scheme |
| `one-dark` | Dark | Based on Atom One Dark |
| `nord` | Dark | Arctic, blue-gray palette |
| `tokyo-night` | Dark | Deep blue background with soft neon accents |
| `gruvbox-dark` | Dark | Warm, retro browns and yellows |
| `gruvbox-light` | Light | Light companion to gruvbox-dark |
| `solarized-dark` | Dark | Ethan Schoonover's Solarized, dark |
| `solarized-light` | Light | Solarized, light |
| `rose-pine` | Dark | Muted purples and pinks (Rosé Pine) |
| `rose-pine-dawn` | Light | Light companion to rose-pine |
| `night-owl` | Dark | Navy background tuned for night reading |
| `monokai` | Dark | The classic Monokai colors |

Every theme follows its published palette, except that colors under a 4.5:1 contrast ratio on the theme's background, mostly comments and line numbers, are shifted in lightness until they pass. Solarized and `catppuccin-latte` change the most. Line numbers, the language label and the Copy button pass on every theme too.

#### Automatic Light/Dark Pairing

When a reader switches between light and dark mode with the header button or the Appearance panel, the code theme changes to its light or dark counterpart to match, if it has one:

- `catppuccin-mocha` (dark) swaps with `catppuccin-latte` (light)
- `github-dark` swaps with `github-light`
- `gruvbox-dark` swaps with `gruvbox-light`
- `solarized-dark` swaps with `solarized-light`
- `rose-pine` swaps with `rose-pine-dawn`

Themes without a pair (`dracula`, `one-dark`, `nord`, `tokyo-night`, `night-owl` and `monokai`) stay as they are. The switched theme isn't saved: every page works it out again from the saved mode and `codeTheme`, so changing `codeTheme` later still reaches readers who used the toggle. Only when the reader picked a code theme in the Appearance panel is that pick saved, and the toggle swaps it for its counterpart. Choosing System in the panel goes back to the reader's own pick, else `codeTheme`, without a swap.

#### Showing the Theme Selector

The Code theme section of the [Appearance panel](#appearance-panel) is hidden by default. To show it:

```yaml
theme:
  options:
    codeThemeSelector: true
```

It lists the sixteen themes with a color preview, the dark ones first, and the reader's choice is saved in `localStorage`. A saved choice only applies while the section is shown.

### Code Block Window Styles

Four frame styles change the chrome around fenced code blocks. Set the default with the `codeStyle` option:

```yaml
theme:
  options:
    codeStyle: "plain"
```

#### Available Styles

| Style | Description |
|---|---|
| `plain` | No frame (default). Code blocks render with a plain background. |
| `macos` | A title bar with red, yellow and green window buttons. |
| `terminal` | A terminal-style top bar with a green `$` prompt. |
| `vscode` | A tab bar with a primary-colored top edge and left border. |

#### Showing the Style Selector

The Code style section of the [Appearance panel](#appearance-panel) is hidden by default. To show it:

```yaml
theme:
  options:
    codeStyleSelector: true
```

The reader's choice is saved in `localStorage`. A saved choice only applies while the section is shown.

### Color Theme Presets

Eleven presets set the primary color, along with the landing page title gradient. The Color section of the [Appearance panel](#appearance-panel) lists them, and the reader's pick is saved in `localStorage`. The section is shown by default.

#### Available Presets

| Preset | ID | Hex | Text, light | Text, dark |
|---|---|---|---|---|
| Ocean | `ocean` | `#0ea5e9` | `#0076a9` | `#0ea5e9` |
| Emerald | `emerald` | `#10b981` | `#007f57` | `#10b981` |
| Violet | `violet` | `#8b5cf6` | `#804fe8` | `#9b77ff` |
| Amber | `amber` | `#f59e0b` | `#9c6300` | `#f59e0b` |
| Rose | `rose` | `#f43f5e` | `#d81c49` | `#ff4c67` |
| Moka Red | `moka-red` | `#d32f2f` | `#d12d2d` | `#f9564f` |
| Slate | `slate` | `#64748b` | `#607087` | `#8090a8` |
| Teal | `teal` | `#14b8a6` | `#007d71` | `#14b8a6` |
| Indigo | `indigo` | `#6366f1` | `#5c5de8` | `#7a82ff` |
| Orange | `orange` | `#f97316` | `#b85000` | `#f97316` |
| Graphite | `graphite` | `#52525b` | `#52525b` | `#8e8e98` |

In the `default` style, text in the primary color (links, inline code, page titles, the current page in the sidebar and table of contents) uses the "Text" shade (`--color-primary-text`): the preset's hue, darkened in light mode or lightened in dark mode only as far as a 4.5:1 contrast ratio on the code background, the current sidebar link's tint and the admonition boxes needs. Filled buttons use the same shade, with white text in light mode and the dark page color in dark mode. Borders, tints and icons keep the swatch color. A custom `primaryColor` without a preset gets its own shades, worked out the same way (see [Primary Color](#primary-color)).

#### Presets and `primaryColor`

Presets set `--color-primary` with `!important`, so while a preset is applied, `primaryColor` has no visible effect. `defaultColorTheme` (default `ocean`) names the preset applied on a reader's first visit:

- If you change `primaryColor` and leave `defaultColorTheme` unset or `ocean`, no preset is applied and your color shows.
- If you set `defaultColorTheme` to another preset, that preset is applied and `primaryColor` doesn't show.
- Once a reader picks a preset in the Appearance panel, their pick applies on every page.

```yaml
theme:
  options:
    defaultColorTheme: emerald
```

#### Hiding the Color Preset Selector

```yaml
theme:
  options:
    colorThemes: false
```

This hides the Color section. `defaultColorTheme` still applies; a preset a reader picked earlier no longer does.

### Social Links

`socialLinks` adds icon links to the footer of the default layout. Landing pages don't show them.

```yaml
theme:
  options:
    socialLinks:
      - icon: github
        url: "https://github.com/your-org/your-repo"
      - icon: discord
        url: "https://discord.gg/your-server"
      - icon: nuget
        url: "https://www.nuget.org/packages/YourPackage"
```

`icon` must be a name from MokaDocs' built-in icon set, which is a small subset of Lucide. The brand icons are `github`, `twitter`, `discord` and `nuget`; general icons such as `globe`, `mail`, `link` and `message-circle` also work. A name that isn't in the set is printed as plain text in place of the icon, and the build warns about it.

### Edit Links

An "Edit this page" link at the bottom of each page lets readers propose corrections. It needs both `site.editLink` and `showEditLink`, which is off by default:

```yaml
site:
  editLink:
    repo: "https://github.com/your-org/your-repo"
    branch: main
    path: "docs/"

theme:
  options:
    showEditLink: true
```

Markdown pages link to `{repo}/edit/{branch}/{path}/{file}`, where `{file}` is the page's path inside the docs folder. That URL layout matches GitHub. `branch` defaults to `main` and `path` to `docs/`. Generated pages, such as API pages, have no source file and get no edit link.

### Contributors

`showContributors: true` lists the people who committed to each Markdown page under the "Last updated" line, most commits first, with their initials as an avatar. It's off by default. Names come from `git log` with the repository's `.mailmap` applied, one entry per email; emails aren't shown and no avatar is downloaded.

```yaml
theme:
  options:
    showContributors: true
```

A custom theme gets the list as `page.contributors` (each with `name`, `initials` and `commits`) and the option as `theme.show_contributors`.

### Last Updated

The date a page was last modified appears at the bottom of each page, written out in the site's language (`November 12, 2026` in English, see [Languages](/guide/languages#dates)). It's on by default; `showLastUpdated: false` hides it.

The date is that of the last git commit that changed the Markdown file. Files with uncommitted changes, files git doesn't track, and sites built without git or outside a repository use the file's last write time on disk. A shallow clone gives every page the date of its one commit, so CI builds need the full history (`fetch-depth: 0` with `actions/checkout`, see [Deployment](/advanced/deployment#last-updated-dates-in-ci)).

### Logo and Favicon

Customize the site branding with logo and favicon settings:

```yaml
site:
  logo: assets/logo.svg
  favicon: assets/favicon.ico
```

The logo replaces the book icon next to the site title in the header. Any image format the browser can display works. If the logo or favicon file doesn't exist, the build warns and leaves it out, so the header keeps the book icon.

Paths are resolved **relative to the directory containing `mokadocs.yaml`** and support several forms, including parent-directory escapes via `../` and absolute URLs for CDN-hosted assets. See the [Site Configuration - Logo](/configuration/site-config#logo) page for the full path resolution rules and worked examples.

## Dark Mode

### Automatic Detection

On a reader's first visit, a script at the top of the page picks dark mode when the system prefers it (`prefers-color-scheme: dark`) and light mode otherwise. It sets `data-theme` on the `<html>` element before the page renders. With the [`moka` style](#styles) the first visit starts in dark mode whatever the system prefers.

### Manual Toggle

The sun/moon button in the header switches modes and saves the choice in `localStorage`. A saved choice takes priority over the system setting on later visits. The Mode section of the [Appearance panel](#appearance-panel) offers Light, Dark and System; System removes the saved choice. On screens 768px wide or less the button is left out and the panel is the place to switch. `showDarkModeToggle: false` hides the button and the Mode section.

The button reports its state to screen readers as pressed (dark) or not pressed (light).

### Customizing Dark Mode Colors

Dark mode colors are defined under a `[data-theme="dark"]` selector, which redefines the background, text and border properties listed in [Colors](#colors). `--color-primary` and its shades are not redefined, so the primary color is the same in both modes.

To change the dark colors, override them in a custom theme's stylesheet:

```css
[data-theme="dark"] {
    --color-bg: #0a0a0a;
    --color-bg-secondary: #171717;
    --color-bg-code: #1c1c1e;
    --color-text: #fafafa;
    --color-text-secondary: #a1a1aa;
    --color-border: #27272a;
}
```

## Landing Page

Pages with `layout: landing` in their front matter use the landing layout. It starts with a hero section: the site logo, the site title, the page's `description`, a **Get Started** button that links to the first sidebar page other than the landing page itself, and a **View on GitHub** button when `site.editLink.repo` is set. Next come the feature cards listed in the page's [`features`](/configuration/front-matter#features) front matter, under an optional `featuresTitle` and `featuresSubtitle`, then the page's own Markdown content. A page without `features` goes from the hero straight to its content.

```yaml
---
title: Widgets
description: UI widgets for .NET
layout: landing
featuresTitle: Why Widgets
features:
  - title: Fast
    icon: zap
    description: Renders in under a millisecond.
    link: /guide/performance
---
```

[`layout.hero`](/configuration/site-config#layout.hero) moves the hero to the left (`minimal`), splits it, with a code sample from [`heroCode`](/configuration/front-matter#herocode) or an image from `heroImage` on the right (`split`), or puts it on a band in the accent color (`banner`). [`layout.heroBackground`](/configuration/site-config#layout.herobackground) swaps its backdrop for dots, a gradient, blurred color patches (`mesh`), diagonal lines or nothing.

### Home Page

[`layout.home`](/configuration/site-config#layout.home) changes the landing page as a whole, and [`layout.features`](/configuration/site-config#layout.features) the feature cards. Both are site settings, so every page with `layout: landing` gets them.

```yaml
theme:
  options:
    layout:
      home: showcase
      features: bento
```

#### `default`

The hero, the feature cards and the page content, as described above.

#### `portal`

A docs hub: a compact hero whose title and description sit over a large search box, then a card for each top-level section of the sidebar, then the page content. A card shows the section's [nav icon](/configuration/navigation#icon-support) when it has one, links to the section's first page and lists its first five pages. The search box opens the same search dialog as the header button and `Ctrl K` (`Cmd K` on a Mac); it needs [search](/configuration/site-config#search.enabled) and JavaScript, and is left out without them. The page's `features` aren't shown.

```yaml
theme:
  options:
    layout:
      home: portal
      heroBackground: gradient
```

The front matter needs nothing more than a `description`, which becomes the line above the search box.

#### `showcase`

A product page: the hero and the feature cards, then up to three bands from the page's front matter, then the content.

```yaml
---
title: Home
description: Fast, typed UI widgets for .NET apps.
layout: landing
features:
  - title: Fast
    icon: zap
    description: Renders ten thousand rows without a pause.
showcaseTitle: Bind a grid in two lines
showcaseText: Point a DataGrid at any list and it pages, sorts and filters on its own.
showcaseCodeLanguage: csharp
showcaseCode: |
  var grid = new DataGrid<Order> { PageSize = 50 };
  grid.Bind(await db.Orders.ToListAsync());
stats:
  - value: 2M+
    label: NuGet downloads
  - value: 40
    label: Widgets
cta:
  title: Ready to build?
  text: Install the package and have a grid on screen in five minutes.
  link: /guide/install
  linkText: Install Widgets
---
```

- [`showcaseCode`](/configuration/front-matter#showcasecode) is a code sample, highlighted at build time in `showcaseCodeLanguage`, with `showcaseTitle` and `showcaseText` beside it on a tinted band. Without a sample, the title and text are centered on the band.
- [`stats`](/configuration/front-matter#stats) is a row of large figures, each with a label under it.
- [`cta`](/configuration/front-matter#cta) is a closing band in the accent color with a heading, a line of text and a button.

Each band appears only when its front matter is set, so `showcase` without them looks like `default`.

#### `minimal`

The site title, the description, the **Get Started** and **View on GitHub** buttons and the page content. No logo, no feature cards and no backdrop behind the title, whatever `heroBackground` says. It reads no extra front matter.

#### `docs`

No hero: the landing page looks like the other pages, with the sidebar on the left. The site title and the page's `description` form the page header, the `features` follow as link cards (a card with a `link` is clickable), then the page content. On a phone, the menu button in the header opens the sidebar as on any other page. The sidebar settings ([`sidebar`](/configuration/site-config#layout.sidebar), `sidebarScope`, `sidebarToggle` and the [shared sidebar](/configuration/site-config#options.sidebar)) apply; the hero and feature card variants don't.

#### Feature Cards

| `features` | Cards |
|---|---|
| `default` | Cards, three to a row |
| `list` | The icon beside the text, two columns separated by rules, no card boxes |
| `icons` | Large round icons over centered text, no card boxes |
| `compact` | Small dense cards, four to a row |
| `bento` | The first card takes two columns and two rows, the rest fill in around it |

They apply to `home: default` and `showcase`, and every variant keeps a card's `link`. On phones every variant is one card to a row (`compact` keeps two down to about 520 pixels).

### Gradient Customization

The hero title's gradient runs from the primary color's text shade (`--color-primary-text`) through `--gradient-secondary` to `--gradient-tertiary`. The last two are mixed with black in light mode and with white in dark mode (`--gradient-text-secondary` and `--gradient-text-tertiary`), so every color in the title has at least the 3:1 contrast ratio large text needs. Page titles run from `--color-primary-text` to `--color-primary-text-hover`. The hero background mixes `--color-primary` with fixed colors. A color preset sets all three properties. There are no `--landing-gradient-*` properties; other changes to the gradients need a custom theme.

## Announcement Banner

`banner` puts a one-line announcement above the header of every page, landing pages and the 404 page included:

```yaml
theme:
  options:
    banner:
      text: "Version 2.0 is out"
      link: /blog/2-0                  # a URL, or a path from the site root
      linkText: "Read the release notes"
      variant: accent                  # info (default), warning or accent
```

- **Text** is plain text: HTML in it shows as written. The link comes after it, with "Learn more" when `linkText` isn't set. A path from the site root gets the base path.
- **Variants:** `info` is a light tint of the primary color, `warning` an amber tint with a warning icon, and `accent` a solid bar in the primary color. Each style gives the bar its own edge and type, and every variant meets WCAG AA contrast with every color preset in light and dark mode.
- **Scrolling:** the banner isn't sticky. It scrolls away with the page and the header stays at the top as usual. While it is on screen, the mobile sidebar and the "On this page" drawer open below the header.
- **Dismissing:** a close button hides the banner and saves its `id` in the reader's `localStorage` (`mokadocs-banner-dismissed`). Every page with the same id then hides the banner before it is drawn, so it doesn't flash. Without an `id`, the id is a hash of the text and the link, so changing either shows the banner again to readers who closed it. Set `id` yourself to decide when that happens, and `dismissible: false` for a banner readers can't close. Without JavaScript the close button isn't shown.
- **Printing:** the banner is left out.
- **Environment:** `MOKADOCS_BANNER_TEXT` and `MOKADOCS_BANNER_LINK` set the text and the link for one build, with or without a `banner` block, which suits a CI job that announces a release:

```bash
MOKADOCS_BANNER_TEXT="Docs for the 2.0 preview" MOKADOCS_BANNER_LINK=/blog/preview mokadocs build
```

The banner is `role="region"` with the label "Announcement", right after the "Skip to content" link. Only the default theme shows it; a custom theme's layouts get it as `theme.banner` (`text`, `link` with the base path added, `link_text`, `id`, `dismissible`, `variant`), or nothing when there is no banner. See [`options.banner`](/configuration/site-config#options.banner) for every key.

## Feedback Widget

Pages using the default layout end with a "Was this page helpful?" widget with thumbs-up and thumbs-down buttons. Landing pages don't include it.

### Configuration

The widget is on by default. To remove it everywhere:

```yaml
theme:
  options:
    showFeedback: false
```

### How It Works

- **Vote storage:** each vote is saved in the reader's `localStorage` under the page path, so a returning reader sees their earlier vote.
- **Reporting:** the widget also sends `POST {base path}/api/feedback` with the page path and the vote. `mokadocs serve` logs the vote at Information level, which only prints with `--verbose`. A server of your own that handles that route receives the same request.
- **Static hosts:** a static host has no such endpoint. The request fails without an error shown to the reader, and the vote is still stored in `localStorage`.

## Animations

Animations are off by default. To turn them on:

```yaml
theme:
  options:
    showAnimations: true
```

### Accessibility: `prefers-reduced-motion`

When a reader's system asks for reduced motion (`prefers-reduced-motion: reduce`), animations stay off whatever `showAnimations` says. With animations off, content that would fade in is there from the first frame, the landing page hero included.

### What Is Affected

With animations on:

- **Page load**: the page content fades in and headings slide up.
- **Sidebar**: sections animate open and closed.
- **Landing page hero**: the title, subtitle and buttons fade in, the icon floats, and the background gradient shifts.
- **Hover effects**: buttons, links and cards change with transitions.

When animations are off, by configuration or by the system setting, the stylesheet cuts animation and transition durations to 0.01ms, so state changes look instant. Scrolling jumps instead of gliding too: links to a heading, the back-to-top buttons and the table of contents.

## Image Zoom

Clicking an image in the page content opens it full size over a dark backdrop. Any click, Escape or the close button closes it, and focus goes back to the image. Keyboard users reach each zoomable image with Tab and open it with Enter. Images inside links keep working as links, and images smaller than 96 by 64 pixels, such as badges and icons, are left alone. To leave out one image, give it a `data-no-zoom` attribute in raw HTML:

```html
<img src="/diagram.png" alt="Architecture" data-no-zoom>
```

To turn the feature off for the whole site:

```yaml
theme:
  options:
    imageZoom: false
```

## Keyboard and Accessibility

The default theme works with a keyboard and a screen reader:

- **Skip link**: the first Tab on a page shows a "Skip to content" link that jumps past the header and sidebar.
- **Search**: `Ctrl+K` (`Cmd+K` on Apple devices; the header shows the right one) opens the dialog, Up and Down move through the results, Enter opens one, and Escape closes the dialog and returns focus to where it was. Tab stays inside the dialog while it is open. Screen readers hear the number of results.
- **Appearance panel**: Enter or Space on its button opens it and moves focus to the first section. Each section is a radio group and one Tab stop: the arrow keys, Home and End move between its choices and apply them at once. The Layout section is a labelled list box for each part, which the arrow keys change at once too. Escape or the close button closes the panel and returns focus to its button; a click outside, or tabbing out of the panel, closes it too. The page behind stays usable, since the panel isn't modal.
- **Version menu**: opens with Enter, Space or Down, moves with the arrow keys, Home and End, and closes with Escape.
- **Tabs and code groups**: Left and Right move between tabs, Home and End jump to the first and last. Choosing a tab such as "C#" or "CLI" chooses the same label in every tab group on the page, and the theme remembers the last few labels for later pages. The NuGet install box on the API index (Package Manager, .NET CLI, PackageReference) uses the same keys.
- **Mobile menu and "On this page" drawer**: focus moves into them when they open, the page behind them is dimmed, and Escape or a click outside closes them. Only one of them is open at a time.
- **Sidebar**: a collapsed section's links are skipped by Tab and not read out, and the current page's link is marked as the current page.
- **Heading links**: the `#` link next to a heading appears on keyboard focus as well as on hover. Clicking it puts the heading in the address bar and copies the link. Over plain `http`, where browsers have no clipboard API, it falls back to the older copy command.

- **Contrast**: in the `default` style, page text, links, titles, admonition titles, badges and buttons meet WCAG AA (4.5:1, 3:1 for large text such as page titles) in light and dark mode with every color preset, and with a custom `primaryColor` in hex, `rgb()`, `hsl()` or named form. Colors inside code blocks depend on the [code theme](#code-syntax-themes).
- **High contrast mode**: with Windows High Contrast or another forced color mode, page titles drop their gradient, focus rings use the system highlight color, and the current page in the sidebar, the active "On this page" entry and the selected tab get an underline or a border in that color, since the tints that mark them are gone.

Reader choices (dark mode, style, color, code theme and style, text size, reading width, layout, tab labels, a dismissed banner) are kept in `localStorage`. When the browser blocks storage, the theme still works; it just doesn't remember them.

Without JavaScript, the theme hides the buttons that need it (search, the Appearance and version buttons, the dark mode toggle, the drawer buttons and the banner's close button), opens every sidebar section, shows every tab of a tab group under its own label, and on phones lists the sidebar after the page content.

## Printing

The default theme has a print stylesheet. A printed page shows the article alone at full width, in black on white whatever mode or style the reader had on screen:

- The announcement banner, header, sidebar, "On this page" panel, feedback widget, previous and next links, floating buttons, Copy buttons, "Copy page" and search are left out.
- Links to other pages and sites print their address in parentheses after the link text. Links within the page don't.
- Code blocks wrap long lines and use light colors, whatever the screen's code theme. Line numbers stay with their lines.
- Code blocks, tables, admonitions and figures don't break across pages when they fit on one.
- Page titles print in solid black instead of the gradient.

## Footer Customization

The footer of the default layout contains:

1. **Copyright text** from `site.copyright`
2. **Social links** from `theme.options.socialLinks`
3. **"Built with MokaDocs"** branding with the MokaDocs version

`theme.options.layout.footer` changes how these are laid out, on landing pages too, or removes the footer. See [Page Layout](#page-layout).

### Copyright Text

Set the `copyright` field under `site:` in your `mokadocs.yaml`:

```yaml
site:
  copyright: "© {year} Your Organization. All rights reserved."
```

`{year}` is replaced with the current year when the site is built. The rest of the text is inserted as written. Without `copyright`, the footer shows no copyright text.

### Disabling "Built with MokaDocs" Branding

By default, the footer displays `Built with MokaDocs vX.Y.Z` with a link to the GitHub repository. To hide it:

```yaml
theme:
  options:
    showBuiltWith: false
```

With `showBuiltWith: false`, no `copyright` and no social links, the footer is an empty bar.

### Footer on the Landing Page

The landing layout's footer reads `Built with ❤ using MokaDocs vX.Y.Z`, followed by the copyright text. The same `showBuiltWith` option controls both layouts. Social links don't appear on the landing page.

:::tip
There is no `footer:` top-level key in `mokadocs.yaml`. The copyright text goes under `site: copyright:`, and the branding toggle goes under `theme: options: showBuiltWith:`.
:::

## Full Configuration Example

A `mokadocs.yaml` with every theme option that has an effect. Apart from the `site` values, `showEditLink` and `socialLinks`, the values shown are the defaults.

```yaml
site:
  title: "My Library Docs"
  description: "Documentation for My Library"
  url: "https://my-org.github.io/my-library"   # canonical and Open Graph tags are only written when set
  copyright: "© {year} My Organization. All rights reserved."
  logo: assets/logo.svg
  favicon: assets/favicon.ico
  editLink:
    repo: "https://github.com/my-org/my-library"
    branch: main
    path: "docs/"

content:
  docs: ./docs

theme:
  name: default
  options:
    # Look
    style: default                # default, moka, minimal, classic, soft, terminal, paper, contrast, glass or brutal
    styleSelector: true           # Style section of the Appearance panel
    textSizeSelector: true        # Text size section
    widthSelector: true           # Reading width section
    layoutSelector: false         # Layout section: readers switch the layout variants

    # Layout
    layout:
      width: default              # default, wide, wider, full, boxed or narrow
      navbar: default             # default, compact, floating, tabs or autohide
      footer: default             # default, minimal, columns or none
      toc: default                # default, minimal, inline or floating
      breadcrumbs: default        # default, chevrons, pills, path or back
      pageNav: default            # default, minimal or stacked

    # Colors
    primaryColor: "#0ea5e9"
    accentColor: "#f59e0b"
    colorThemes: true             # Color section of the Appearance panel
    defaultColorTheme: ocean      # preset applied on a first visit

    # Code blocks
    codeTheme: "catppuccin-mocha"
    codeThemeSelector: false
    codeStyle: "plain"
    codeStyleSelector: false
    showCopyButton: true
    showLineNumbers: true
    imageZoom: true

    # Page features
    showSearch: true
    showDarkModeToggle: true
    showTableOfContents: true
    tocDepth: 3
    showBreadcrumbs: true
    showPrevNext: true
    showBackToTop: true
    showVersionSelector: true
    showFeedback: true
    showLastUpdated: true
    showEditLink: true            # default: false
    showAnimations: false

    # Footer
    showBuiltWith: true

    # Social links (footer of the default layout)
    socialLinks:
      - icon: github
        url: "https://github.com/my-org/my-library"
      - icon: nuget
        url: "https://www.nuget.org/packages/MyLibrary"
      - icon: discord
        url: "https://discord.gg/my-community"
```
