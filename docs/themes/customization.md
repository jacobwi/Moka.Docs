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
- **Syntax highlighting**: done in the browser, with seven color themes.
- **Search**: a dialog opened from the header button or with `Ctrl+K` / `Cmd+K`. It matches page titles, headings, tags and the first 300 characters of each page's text. See [Search](/guide/search).
- **Table of contents**: built from the page's headings, highlighting the section in view.

Links between pages are ordinary page loads; there is no client-side navigation.

## CSS Custom Properties

The default theme's stylesheet defines these custom properties. The default theme has no setting for loading an extra stylesheet, so overriding them takes a custom theme (see [Changing Styles](#changing-styles)). `primaryColor` and `accentColor` set the two brand colors from configuration.

### Colors

| Property | Light | Dark | Used for |
|----------|-------|------|----------|
| `--color-primary` | `#0ea5e9` | unchanged | Links, active navigation, buttons |
| `--color-primary-light` | `#38bdf8` | unchanged | Lighter shade of the primary color |
| `--color-primary-dark` | `#0284c7` | unchanged | Darker shade, used for link hover |
| `--color-accent` | `#f59e0b` | unchanged | Highlighted matches in search results |
| `--color-bg` | `#ffffff` | `#0f172a` | Page background |
| `--color-bg-secondary` | `#f8fafc` | `#1e293b` | Secondary surfaces and hover backgrounds |
| `--color-bg-code` | `#f1f5f9` | `#1e293b` | Inline code background |
| `--color-text` | `#1e293b` | `#e2e8f0` | Body text |
| `--color-text-secondary` | `#64748b` | `#94a3b8` | Secondary text |
| `--color-text-muted` | `#94a3b8` | `#64748b` | Muted text |
| `--color-border` | `#e2e8f0` | `#334155` | Borders and dividers |
| `--color-border-light` | `#f1f5f9` | `#1e293b` | Defined, but not used by the default stylesheet |
| `--gradient-secondary` | `#06b6d4` | unchanged | Second color of the landing page title gradient |
| `--gradient-tertiary` | `#3b82f6` | unchanged | Third color of the landing page title gradient |

Code block colors come from the selected [code theme](#code-syntax-themes), which sets `--sh-comment`, `--sh-string`, `--sh-keyword` and the other `--sh-*` properties.

### Layout

| Property | Default | Description |
|----------|---------|-------------|
| `--sidebar-width` | `280px` | Width of the sidebar |
| `--toc-width` | `220px` | Width of the "On this page" column |
| `--header-height` | `60px` | Height of the header, also used to offset sticky elements |
| `--content-max-width` | `780px` | Maximum width of the page content |

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

The theme doesn't load web fonts, so Inter and JetBrains Mono are only used when they're installed on the reader's machine. The body font size (16px) and line height (1.7) are fixed values, not properties.

### Changing Styles

With the default theme, `css_files` contains only `/_theme/css/main.css`, and there is no `customCss` option. A `.css` file in the docs folder is copied to the output, but no page links to it.

That leaves two options:

- `primaryColor` and `accentColor` in `theme.options` change the brand colors. See [Config-Driven Customization](#config-driven-customization).
- A [custom theme](#choosing-a-theme) can ship its own stylesheets. It replaces the default theme entirely, layouts included. Building a site with the default theme writes its stylesheet and script to `_theme/css/main.css` and `_theme/js/main.js`, which you can copy into your theme as a starting point. The default layouts are defined in `EmbeddedThemeProvider.cs` in the MokaDocs source.

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
    <title>{{ page.title }} - {{ site.title }}</title>
    {{ for css in css_files }}<link rel="stylesheet" href="{{ css }}" />
    {{ end }}
</head>
<body>
    <main>{{ page.content }}</main>
    {{ for js in js_files }}<script src="{{ js }}"></script>
    {{ end }}
</body>
</html>
```

Everything under `css/`, `js/` and `assets/`, subfolders included, is copied to `_theme/` in the output. `css_files` lists every `.css` file under `css/`, subfolders included, in alphabetical order of path. `js_files` lists only the `.js` files directly inside `js/`. The built-in stylesheet and script are not included, so a custom theme owns its styling completely.

A few things to know when writing layouts:

- The variables a layout can use are listed in [Template Variables](/advanced/architecture#template-variables).
- Partials are inserted as raw text: `{{ partials.footer }}` outputs the file as written, without evaluating Scriban expressions in it. Scriban's `include` doesn't work in MokaDocs layouts. See [Partials](/advanced/architecture#partials).
- A page whose front matter names a layout the theme doesn't have renders with `default.html`, and the build reports a warning for each missing layout name.
- The build still generates `404.html` from its own markup, which links `_theme/css/main.css` and `_theme/js/main.js`.
- `mokadocs serve` caches the theme for the life of the process, so restart it after editing theme files.

### Fallback Behaviour

When the theme folder doesn't exist or contains no `layouts/*.html`, MokaDocs builds the site with the default theme and reports a warning. A typo in `theme.name` produces a styled site plus a warning rather than a failed build. `mokadocs build` counts the warning in its summary and lists it with `--verbose`, and `mokadocs validate` lists it.

## Config-Driven Customization

`theme.options` in `mokadocs.yaml` customizes the default theme without writing CSS. The full list of options is in [Site Configuration](/configuration/site-config).

### Feature Toggles

| Option | Default | Controls |
|--------|---------|----------|
| `showSearch` | `true` | Search button, search dialog and the `Ctrl+K` shortcut. Search also needs `features.search.enabled` |
| `showDarkModeToggle` | `true` | Dark mode button in the header |
| `showTableOfContents` | `true` | "On this page" column. Front matter `toc: false` hides it on one page |
| `tocDepth` | `3` | Deepest heading level in the table of contents, from 2 to 6 |
| `showBreadcrumbs` | `true` | Breadcrumb trail above the content |
| `showPrevNext` | `true` | Previous and next page links below the content |
| `showBackToTop` | `true` | Back-to-top button |
| `showCopyButton` | `true` | Copy button on code blocks |
| `showLineNumbers` | `true` | Line numbers on code blocks of three or more lines |
| `showVersionSelector` | `true` | Version menu in the header, when versions are configured |
| `showFeedback` | `true` | [Feedback widget](#feedback-widget) |
| `showLastUpdated` | `true` | [Last updated](#last-updated) date |
| `showEditLink` | `false` | [Edit link](#edit-links) |
| `showAnimations` | `false` | [Animations](#animations) |
| `showBuiltWith` | `true` | ["Built with MokaDocs"](#footer-customization) footer text |

The breadcrumbs, previous and next links, back-to-top button, version menu, feedback widget, last updated date and edit link appear in the default layout only, not on landing pages.

### Primary Color

Set `primaryColor` to change the brand color across the site:

```yaml
theme:
  options:
    primaryColor: "#e11d48"
```

The value becomes `--color-primary` in an inline style, and `--color-primary-light` and `--color-primary-dark` are derived from it with CSS `color-mix()`: 75% of your color mixed with white, and 80% mixed with black. You only set one value.

The value is inserted as written, so any CSS color works:

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

MokaDocs ships with 7 syntax highlighting themes for code blocks. Set the default with the `codeTheme` option:

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

#### Automatic Light/Dark Pairing

When a reader switches between light and dark mode with the header button, the code theme changes to its light or dark counterpart to match, if it has one:

- `catppuccin-mocha` (dark) swaps with `catppuccin-latte` (light)
- `github-dark` swaps with `github-light`

Themes without a pair (`dracula`, `one-dark` and `nord`) stay as they are. The switched theme is saved in `localStorage` like a choice the reader made.

#### Showing the Theme Selector

The code theme selector, a `</>` button in the header, is hidden by default. To show it:

```yaml
theme:
  options:
    codeThemeSelector: true
```

It lists the seven themes with a color preview, and the reader's choice is saved in `localStorage`.

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

The window style selector, a window icon button in the header, is hidden by default. To show it:

```yaml
theme:
  options:
    codeStyleSelector: true
```

The reader's choice is saved in `localStorage`.

### Color Theme Presets

Six presets set the primary color, along with the landing page title gradient. A palette button in the header lists them, and the reader's pick is saved in `localStorage`. The palette is shown by default.

#### Available Presets

| Preset | ID | Hex |
|---|---|---|
| Ocean | `ocean` | `#0ea5e9` |
| Emerald | `emerald` | `#10b981` |
| Violet | `violet` | `#8b5cf6` |
| Amber | `amber` | `#f59e0b` |
| Rose | `rose` | `#f43f5e` |
| Moka Red | `moka-red` | `#d32f2f` |

#### Presets and `primaryColor`

Presets set `--color-primary` with `!important`, so while a preset is applied, `primaryColor` has no visible effect. `defaultColorTheme` (default `ocean`) names the preset applied on a reader's first visit:

- If you change `primaryColor` and leave `defaultColorTheme` unset or `ocean`, no preset is applied and your color shows.
- If you set `defaultColorTheme` to another preset, that preset is applied and `primaryColor` doesn't show.
- Once a reader picks a preset from the palette, their pick applies on every page.

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

This hides the palette button only. `defaultColorTheme` still applies, and so does a preset a reader picked earlier.

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

`showContributors` is accepted in `mokadocs.yaml` but has no effect. The default theme doesn't show contributors, and MokaDocs doesn't read git history.

### Last Updated

The date a page was last modified appears at the bottom of each page, formatted `yyyy-MM-dd`. It's on by default; `showLastUpdated: false` hides it.

The date is the Markdown file's last write time on disk. A fresh git clone gives every file the time of the checkout, so a site built in CI shows the build date on every page.

### Logo and Favicon

Customize the site branding with logo and favicon settings:

```yaml
site:
  logo: assets/logo.svg
  favicon: assets/favicon.ico
```

The logo replaces the book icon next to the site title in the header. Any image format the browser can display works.

Paths are resolved **relative to the directory containing `mokadocs.yaml`** and support several forms, including parent-directory escapes via `../` and absolute URLs for CDN-hosted assets. See the [Site Configuration - Logo](/configuration/site-config#logo) page for the full path resolution rules and worked examples.

## Dark Mode

### Automatic Detection

On a reader's first visit, a script at the top of the page picks dark mode when the system prefers it (`prefers-color-scheme: dark`) and light mode otherwise. It sets `data-theme` on the `<html>` element before the page renders.

### Manual Toggle

The sun/moon button in the header switches modes and saves the choice in `localStorage`. A saved choice takes priority over the system setting on later visits. `showDarkModeToggle: false` hides the button.

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

Pages with `layout: landing` in their front matter use the landing layout. It starts with a hero section: the site logo, the site title, the page's `description`, a **Get Started** button that links to the first sidebar item, and a **View on GitHub** button when `site.editLink.repo` is set. A fixed feature grid and a sample `mokadocs.yaml` block follow, then the page's own Markdown content. The feature grid and sample block can't be changed from configuration.

### Gradient Customization

The hero title's gradient runs from `--color-primary` through `--gradient-secondary` to `--gradient-tertiary`, and the hero background mixes `--color-primary` with fixed colors. A color preset sets all three properties. There are no `--landing-gradient-*` properties; other changes to the gradients need a custom theme.

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

When a reader's system asks for reduced motion (`prefers-reduced-motion: reduce`), animations stay off whatever `showAnimations` says.

### What Is Affected

With animations on:

- **Page load**: the page content fades in and headings slide up.
- **Sidebar**: sections animate open and closed.
- **Landing page hero**: the title, subtitle and buttons fade in, the icon floats, and the background gradient shifts.
- **Hover effects**: buttons, links and cards change with transitions.

When animations are off, by configuration or by the system setting, the stylesheet cuts animation and transition durations to 0.01ms, so state changes look instant.

## Footer Customization

The footer of the default layout contains:

1. **Copyright text** from `site.copyright`
2. **Social links** from `theme.options.socialLinks`
3. **"Built with MokaDocs"** branding with the MokaDocs version

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
    # Colors
    primaryColor: "#0ea5e9"
    accentColor: "#f59e0b"
    colorThemes: true             # palette button in the header
    defaultColorTheme: ocean      # preset applied on a first visit

    # Code blocks
    codeTheme: "catppuccin-mocha"
    codeThemeSelector: false
    codeStyle: "plain"
    codeStyleSelector: false
    showCopyButton: true
    showLineNumbers: true

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
