---
title: Themes & Customization
order: 1
---

# Themes & Customization

MokaDocs ships with a default theme that is responsive, accessible, and customizable. You can adjust colors, typography, layout dimensions, and more through configuration and CSS custom properties.

## Default Theme Features

The built-in theme includes:

- **Responsive layout** -- Adapts to desktop, tablet, and mobile screen sizes with a collapsible sidebar
- **Dark and light mode** -- Automatic detection via `prefers-color-scheme` with a manual toggle in the header
- **Syntax highlighting** -- Code blocks are highlighted with configurable color themes
- **Search overlay** -- Full-text search with keyboard shortcut support (Ctrl+K / Cmd+K)
- **Smooth navigation** -- Client-side page transitions with scroll position restoration
- **Table of contents** -- Auto-generated from headings with scroll-spy highlighting

## CSS Custom Properties

The theme exposes CSS custom properties that you can override to customize the look and feel without modifying theme files. These properties control the core visual design system.

### Color Properties

| Property                  | Default (Light)   | Default (Dark)     | Description |
|---------------------------|-------------------|--------------------|-------------|
| `--color-primary`         | `#6366f1`         | `#818cf8`          | Primary brand color used for links, active states, and accents |
| `--color-primary-light`   | Derived           | Derived            | Lighter variant of primary, used for hover states and backgrounds |
| `--color-primary-dark`    | Derived           | Derived            | Darker variant of primary, used for active/pressed states |
| `--color-bg`              | `#ffffff`         | `#0f172a`          | Page background color |
| `--color-surface`         | `#f8fafc`         | `#1e293b`          | Surface color for cards, sidebar, and elevated elements |
| `--color-text`            | `#1e293b`         | `#e2e8f0`          | Primary text color |
| `--color-text-muted`      | `#64748b`         | `#94a3b8`          | Secondary/muted text color for descriptions and metadata |
| `--color-border`          | `#e2e8f0`         | `#334155`          | Border color for dividers, cards, and input fields |
| `--color-code-bg`         | `#f1f5f9`         | `#1e293b`          | Background color for inline code and code blocks |

### Layout Properties

| Property           | Default    | Description |
|--------------------|------------|-------------|
| `--sidebar-width`  | `280px`    | Width of the sidebar navigation panel |
| `--header-height`  | `60px`     | Height of the top header bar |

### Overriding Custom Properties

Create a CSS file and reference it in your project, or use the custom CSS injection point in your configuration. For example, to use a green color scheme:

```css
:root {
    --color-primary: #10b981;
    --color-primary-light: #34d399;
    --color-primary-dark: #059669;
}
```

## Config-Driven Customization

The `mokadocs.yaml` file provides several options for customizing the theme without writing CSS.

### Primary Color

Set the `primaryColor` property to change the brand color across the entire site:

```yaml
primaryColor: "#e11d48"
```

MokaDocs uses CSS `color-mix()` to automatically derive the light and dark variants from your primary color. This means you only need to specify a single color value, and the hover states, active states, and background tints are computed automatically.

Any valid CSS color value is accepted:

```yaml
# Hex
primaryColor: "#6366f1"

# Named color
primaryColor: "rebeccapurple"

# HSL
primaryColor: "hsl(250, 80%, 60%)"

# RGB
primaryColor: "rgb(99, 102, 241)"
```

### Code Syntax Themes

MokaDocs ships with 7 built-in syntax highlighting themes for code blocks. A `</>` button in the site header opens a dropdown listing all available themes with colored previews, allowing readers to switch themes on the fly. The selected theme is saved to `localStorage` and persists across pages.

Set the default code theme with the `codeTheme` option:

```yaml
theme:
  options:
    codeTheme: "catppuccin-mocha"
```

#### Available Themes

| Theme | Style | Description |
|---|---|---|
| `catppuccin-mocha` | Dark | Warm dark theme with pastel accents (default) |
| `catppuccin-latte` | Light | Warm light companion to catppuccin-mocha |
| `github-dark` | Dark | GitHub's dark syntax colors |
| `github-light` | Light | GitHub's light syntax colors |
| `dracula` | Dark | Popular Dracula color scheme |
| `one-dark` | Dark | Atom One Dark inspired |
| `nord` | Dark | Arctic, north-bluish palette |

#### Automatic Light/Dark Pairing

When a reader toggles between light and dark mode using the header switch, the code theme automatically switches to its paired counterpart if one exists:

- `catppuccin-mocha` (dark) swaps with `catppuccin-latte` (light)
- `github-dark` swaps with `github-light`

Themes without a pair (such as `dracula`, `one-dark`, and `nord`) remain unchanged when toggling site mode.

#### Hiding the Theme Selector

The code theme selector button is visible by default. To hide it:

```yaml
theme:
  options:
    codeThemeSelector: false
```

### Code Block Window Styles

MokaDocs provides 4 window frame styles that change the visual chrome around fenced code blocks. A window icon button in the site header opens a selector dropdown. The selected style is saved to `localStorage` and persists across pages.

Set the default window style with the `codeStyle` option:

```yaml
theme:
  options:
    codeStyle: "plain"
```

#### Available Styles

| Style | Description |
|---|---|
| `plain` | No frame decoration (default). Code blocks render with a simple background. |
| `macos` | macOS-style title bar with red, yellow, and green traffic light dots. |
| `terminal` | Terminal style with a `$` prompt indicator in the top bar. |
| `vscode` | VS Code style with an accent-colored tab bar along the top edge. |

#### Hiding the Style Selector

The window style selector button is visible by default. To hide it:

```yaml
theme:
  options:
    codeStyleSelector: false
```

### Color Theme Presets

MokaDocs includes 5 built-in color theme presets that change the primary color across the entire site with one click. A palette icon button in the header opens a dropdown showing all available presets. The selected preset is saved to `localStorage`.

The presets provide a quick way for readers to personalize the documentation appearance without the site author needing to set up multiple themes.

#### Available Presets

| Preset | Color | Hex |
|---|---|---|
| Ocean | Blue | `#0ea5e9` |
| Emerald | Green | `#10b981` |
| Violet | Purple | `#8b5cf6` |
| Amber | Orange | `#f59e0b` |
| Rose | Pink | `#f43f5e` |

The default primary color configured via `primaryColor` is always available alongside these presets.

#### Hiding the Color Preset Selector

The color preset selector is visible by default. To hide it:

```yaml
theme:
  options:
    colorThemes: false
```

### Social Links

Add social media and external links to the site header using the `socialLinks` configuration. Each entry is rendered as an icon button in the header navigation.

```yaml
site:
  socialLinks:
    - icon: github
      url: "https://github.com/your-org/your-repo"
    - icon: discord
      url: "https://discord.gg/your-server"
    - icon: nuget
      url: "https://www.nuget.org/packages/YourPackage"
```

Common icon names: `github`, `twitter`, `discord`, `nuget`, `youtube`, `linkedin`, `mastodon`. Icons are rendered as SVGs in the header navigation.

### Edit Links

Add an "Edit this page" link to the bottom of every documentation page. This is useful for open-source projects where you want readers to contribute corrections directly.

```yaml
site:
  editLink:
    repo: "https://github.com/your-org/your-repo"
    branch: main
    path: "docs/"
```

The `repo` value is the base URL of your repository. The `branch` specifies which branch the edit link should point to, and `path` is the directory prefix where your documentation source files live relative to the repository root.

To disable the edit link, set the following theme option:

```yaml
theme:
  options:
    showEditLink: false
```

### Contributors

Display contributor avatars on documentation pages. When enabled, MokaDocs shows the avatars of contributors who have modified each page (pulled from Git history).

```yaml
theme:
  options:
    showContributors: true
```

### Last Updated

Show the last modification date at the bottom of each documentation page. The date is derived from the file's last modified timestamp.

```yaml
theme:
  options:
    showLastUpdated: true
```

### Logo and Favicon

Customize the site branding with logo and favicon settings:

```yaml
logo: ./assets/logo.svg
favicon: ./assets/favicon.ico
```

The logo is displayed in the header next to the site title. Both SVG and raster image formats (PNG, JPG, ICO) are supported. Paths are relative to the project root.

### Typography and Spacing

The default theme uses a system font stack for optimal performance and native appearance. To customize typography, override the relevant CSS custom properties:

```css
:root {
    --font-family: 'Inter', system-ui, -apple-system, sans-serif;
    --font-family-mono: 'JetBrains Mono', 'Fira Code', monospace;
    --font-size-base: 16px;
    --line-height-base: 1.7;
    --content-max-width: 48rem;
}
```

For spacing adjustments, the theme uses a consistent spacing scale based on `rem` units. Override specific spacing values in your custom CSS as needed.

### Custom CSS Injection

You can inject additional CSS to extend or override the default theme. Place your custom CSS file in the project and reference it in the configuration:

```yaml
customCss: ./assets/custom.css
```

The custom CSS file is loaded after the theme styles, so your rules take precedence. This is the recommended approach for advanced styling changes that go beyond what the CSS custom properties offer.

## Dark Mode

### Automatic Detection

The default theme automatically detects the user's system preference using the `prefers-color-scheme` CSS media query. If the operating system or browser is set to dark mode, the documentation site renders in dark mode on first visit.

### Manual Toggle

A sun/moon toggle button is displayed in the header, allowing readers to manually switch between light and dark modes. The selected preference is persisted in `localStorage` so it is remembered across visits.

### Customizing Dark Mode Colors

Dark mode colors are defined under a `[data-theme="dark"]` selector. To customize dark mode specifically:

```css
[data-theme="dark"] {
    --color-bg: #0a0a0a;
    --color-surface: #171717;
    --color-text: #fafafa;
    --color-text-muted: #a1a1aa;
    --color-border: #27272a;
    --color-code-bg: #1c1c1e;
}
```

Both light and dark variants of `--color-primary` are computed from your `primaryColor` configuration, so the accent color adapts to both modes automatically.

## Landing Page

The landing page (your site's root `/` page) supports a gradient hero section. The gradient is derived from the primary color by default, creating a visually distinct entry point to your documentation.

### Gradient Customization

To customize the landing page gradient, override the gradient CSS properties:

```css
.landing-hero {
    --landing-gradient-start: #6366f1;
    --landing-gradient-end: #a855f7;
    --landing-gradient-angle: 135deg;
}
```

The landing page hero section includes the site title, description, and call-to-action buttons. The gradient serves as the background for this section.

## Feedback Widget

Every documentation page displays a "Was this page helpful?" widget at the bottom, allowing readers to provide quick feedback with a thumbs-up or thumbs-down vote.

### Configuration

The feedback widget is enabled by default. To disable it globally, set the `showFeedback` theme option to `false`:

```yaml
theme:
  options:
    showFeedback: false
```

### How It Works

- **Vote storage:** Each vote is persisted in the reader's `localStorage` so that returning visitors see their previous selection and are not prompted again for the same page.
- **Dev mode reporting:** When running the MokaDocs dev server, the widget sends a `POST` request to `/api/feedback` with the page path and vote value. This lets you collect feedback data during local development or when running behind a backend.
- **Static builds:** In static/exported builds where no backend is available, the POST request silently fails. Votes are still recorded in `localStorage` so the UI remains consistent for the reader.

## Animations

MokaDocs includes subtle animations throughout the interface -- page transitions, sidebar expand/collapse, the landing page hero entrance, and hover effects on interactive elements. These are all controlled by a single theme option.

### Configuration

Animations are enabled by default. To disable all animations globally:

```yaml
theme:
  options:
    showAnimations: false
```

### Accessibility: `prefers-reduced-motion`

MokaDocs automatically respects the operating system's reduced-motion accessibility setting. When a reader has enabled "Reduce motion" in their OS preferences, all animations are suppressed regardless of the `showAnimations` value. This ensures your documentation is accessible without requiring any configuration on your part.

### What Is Affected

The following animations are controlled by `showAnimations` and `prefers-reduced-motion`:

- **Page transitions** -- Fade/slide effects when navigating between pages
- **Sidebar expand/collapse** -- Smooth open/close animation for sidebar sections
- **Landing page hero** -- Entrance animation on the hero section of the landing page
- **Hover effects** -- Scale and color transition effects on buttons, links, and cards

When animations are disabled (either by configuration or by the OS setting), all transitions are replaced with instant state changes.

## Footer Customization

The default footer displays a copyright notice. Customize the footer text through configuration:

```yaml
footer:
  copyright: "Copyright 2025 Your Organization. All rights reserved."
```

The footer appears on every page below the main content area. For more advanced footer customization (adding links, logos, or additional sections), use the Custom Footer plugin described in the [Plugin System](/plugins/overview) documentation or inject custom CSS and HTML through the custom CSS injection point.

## Full Configuration Example

A complete `mokadocs.yaml` with all theme-related options:

```yaml
title: My Library Docs
description: Documentation for My Library
primaryColor: "#6366f1"
logo: ./assets/logo.svg
favicon: ./assets/favicon.ico
customCss: ./assets/custom.css

site:
  socialLinks:
    - icon: github
      url: "https://github.com/my-org/my-library"
    - icon: nuget
      url: "https://www.nuget.org/packages/MyLibrary"
    - icon: discord
      url: "https://discord.gg/my-community"
  editLink:
    repo: "https://github.com/my-org/my-library"
    branch: main
    path: "docs/"

theme:
  options:
    showEditLink: true
    showContributors: true
    showLastUpdated: true
    showFeedback: true
    showAnimations: true
    codeTheme: "catppuccin-mocha"
    codeThemeSelector: true
    codeStyle: "plain"
    codeStyleSelector: true
    colorThemes: true

footer:
  copyright: "Copyright 2025 My Organization."

plugins:
  - name: mokadocs-repl
  - name: mokadocs-blazor-preview
  - name: mokadocs-changelog
```
