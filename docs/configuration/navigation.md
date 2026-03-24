---
title: Navigation & Sidebar
order: 3
---

# Navigation & Sidebar

MokaDocs provides a sidebar navigation system that can be auto-generated from your directory structure, manually defined in the configuration file, or a combination of both. This page covers all aspects of how navigation is built, ordered, and displayed.

---

## Auto-Generated Navigation

By default, when no `nav` section is present in `mokadocs.yaml`, MokaDocs automatically generates the sidebar navigation from the `content.docs` directory structure.

### How It Works

1. MokaDocs scans the documentation directory recursively.
2. Each subdirectory becomes a collapsible section in the sidebar.
3. Each `.md` file becomes a navigation item (page link).
4. The hierarchy of directories maps directly to nested navigation groups.
5. Files and directories are sorted according to the `order` front matter value, then alphabetically by title.

### Example Directory Structure

Given this directory layout:

```
docs/
  index.md
  getting-started/
    index.md
    installation.md
    quick-start.md
  guides/
    index.md
    writing-content.md
    theming.md
    deployment.md
  configuration/
    index.md
    site-config.md
    front-matter.md
    navigation.md
```

MokaDocs generates this sidebar:

```
Getting Started
  Installation
  Quick Start
Guides
  Writing Content
  Theming
  Deployment
Configuration
  Site Config
  Front Matter
  Navigation
```

The top-level `index.md` is treated as the home page and does not appear in the sidebar.

---

## Manual Navigation via `nav` Config

For full control over the sidebar structure, define a `nav` section in `mokadocs.yaml`. When present, the `nav` configuration takes precedence over auto-generated navigation.

```yaml
nav:
  - label: "Getting Started"
    icon: "rocket"
    expanded: true
    children:
      - label: "Installation"
        path: "/getting-started/installation"
      - label: "Quick Start"
        path: "/getting-started/quick-start"

  - label: "Guides"
    icon: "book-open"
    expanded: true
    children:
      - label: "Writing Content"
        path: "/guides/writing-content"
      - label: "Theming"
        path: "/guides/theming"

  - label: "API Reference"
    icon: "code"
    autoGenerate: true

  - label: "Changelog"
    path: "/changelog"
    icon: "history"
```

### NavItem Properties

Each item in the `nav` list supports the following properties:

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `label` | `string` | Yes | - | Display text in the sidebar |
| `path` | `string` | No | `null` | URL path this item links to |
| `icon` | `string` | No | `null` | Lucide icon name |
| `expanded` | `bool` | No | `false` | Whether section is expanded by default |
| `autoGenerate` | `bool` | No | `false` | Auto-populate children from API analysis |
| `children` | `list` | No | `[]` | Nested child items |

Items with `children` act as section headers. Items with `path` act as page links. An item can have both a `path` (making the section header clickable) and `children`.

---

## How `order` Front Matter Affects Sorting

The `order` front matter property is the primary mechanism for controlling the display order of pages within a section in auto-generated navigation.

### Sorting Algorithm

For each directory (section), MokaDocs sorts its children using this algorithm:

1. **Pages with `order` defined** are placed first, sorted by `order` in ascending order.
2. **Pages without `order`** are placed after ordered pages, sorted alphabetically by `title`.
3. **Ties** (pages with the same `order` value) are broken alphabetically by `title`.
4. **Subdirectories** follow the same rules. A directory's sort order is determined by the `order` value in its `index.md` front matter.

### Example

Given these files and their front matter:

| File | `order` | `title` |
|---|---|---|
| `installation.md` | `1` | Installation |
| `quick-start.md` | `2` | Quick Start |
| `configuration.md` | `3` | Configuration |
| `troubleshooting.md` | (none) | Troubleshooting |
| `advanced-usage.md` | (none) | Advanced Usage |

The resulting sidebar order is:

1. Installation (order: 1)
2. Quick Start (order: 2)
3. Configuration (order: 3)
4. Advanced Usage (alphabetical, no order)
5. Troubleshooting (alphabetical, no order)

### Negative Order Values

Negative `order` values are allowed and place pages before those with `order: 0` or higher. This can be useful for pinning an "Overview" or "Introduction" page at the very top:

```yaml
---
title: "Overview"
order: -1
---
```

### Section (Directory) Ordering

To control the order of entire sections (directories), set the `order` property in the section's `index.md` file:

```
docs/
  getting-started/
    index.md         # order: 1
  guides/
    index.md         # order: 2
  configuration/
    index.md         # order: 3
  reference/
    index.md         # order: 4
```

---

## Icon Support

Both manual `nav` items and front matter support icons via the `icon` property. Icons are rendered from the Lucide icon set, an open-source icon library with consistent, clean designs.

### Commonly Used Icons

The following icons are frequently used in documentation sidebars:

| Icon Name | Visual Use |
|---|---|
| `rocket` | Getting started, launch |
| `book-open` | Guides, documentation |
| `code` | API reference, code |
| `settings` | Configuration, settings |
| `download` | Installation, downloads |
| `zap` | Quick start, performance |
| `puzzle` | Plugins, extensions |
| `palette` | Theming, design |
| `search` | Search |
| `shield` | Security, authentication |
| `database` | Data, storage |
| `globe` | Deployment, web |
| `terminal` | CLI, commands |
| `file-text` | Content, files |
| `folder` | Directories, organization |
| `layers` | Architecture, layers |
| `git-branch` | Versioning, branches |
| `history` | Changelog, history |
| `help-circle` | FAQ, help |
| `alert-triangle` | Warnings, troubleshooting |
| `check-circle` | Testing, validation |
| `users` | Community, contributors |
| `package` | Packages, NuGet |
| `cpu` | Performance, system |
| `key` | Authentication, keys |
| `link` | Links, references |
| `list` | Lists, navigation |
| `map` | Roadmap, overview |
| `message-circle` | Feedback, discussion |
| `tag` | Tags, labels |
| `tool` | Utilities, tools |
| `trending-up` | Migration, upgrades |
| `eye` | Visibility, preview |

### Using Icons in Front Matter

```yaml
---
title: "Security Guide"
icon: "shield"
---
```

### Using Icons in Nav Config

```yaml
nav:
  - label: "Deployment"
    icon: "globe"
    children:
      - label: "Azure"
        path: "/deployment/azure"
        icon: "cloud"
      - label: "Docker"
        path: "/deployment/docker"
        icon: "container"
```

Icons specified in the `nav` config take precedence over icons defined in front matter for the same page.

The full list of available icons can be found at [lucide.dev/icons](https://lucide.dev/icons).

---

## Nested Sections and Expansion

### Creating Nested Sections

Nested sections are created through subdirectories in auto-generated navigation, or through the `children` property in manual navigation. There is no hard limit on nesting depth, but more than three levels deep is generally discouraged for usability.

```
docs/
  guides/
    index.md
    basics/
      index.md
      markdown-syntax.md
      front-matter.md
    advanced/
      index.md
      custom-components.md
      plugins.md
```

This creates:

```
Guides
  Basics
    Markdown Syntax
    Front Matter
  Advanced
    Custom Components
    Plugins
```

### Controlling Expansion

By default, sections are expanded (`expanded: true`). You can collapse a section by default using the `expanded` property in either front matter or nav config.

**In front matter** (for the section's `index.md`):

```yaml
---
title: "Advanced Topics"
expanded: false
---
```

**In nav config:**

```yaml
nav:
  - label: "Advanced Topics"
    expanded: false
    children:
      - label: "Custom Components"
        path: "/advanced/custom-components"
      - label: "Plugin Development"
        path: "/advanced/plugin-development"
```

**Expansion behavior:**

- Collapsed sections show only their section header with a chevron indicator.
- Clicking the section header toggles expansion.
- When a user navigates to a page inside a collapsed section, that section is automatically expanded to show the active page.
- Expansion state is preserved during the session using the browser's session storage.

---

## `autoGenerate` for API Reference Sections

The `autoGenerate` property on a nav item tells MokaDocs to automatically populate that section's children from the API analysis of configured C# projects.

```yaml
nav:
  - label: "API Reference"
    icon: "code"
    autoGenerate: true
```

When `autoGenerate` is `true`:

1. MokaDocs analyzes all projects listed in `content.projects`.
2. Namespaces become nested sections.
3. Types (classes, interfaces, structs, enums, delegates) become page links within their namespace section.
4. The structure mirrors the namespace hierarchy of the analyzed projects.

### Example Generated Structure

For a project with these namespaces and types:

```
Contoso.Sdk
  ContosoClient
  ContosoOptions
Contoso.Sdk.Auth
  AuthProvider
  TokenManager
Contoso.Sdk.Http
  HttpClientFactory
```

The generated sidebar section looks like:

```
API Reference
  Contoso.Sdk
    ContosoClient
    ContosoOptions
  Contoso.Sdk.Auth
    AuthProvider
    TokenManager
  Contoso.Sdk.Http
    HttpClientFactory
```

### Combining Manual and Auto-Generated Items

You can mix manual children with `autoGenerate`:

```yaml
nav:
  - label: "API Reference"
    icon: "code"
    children:
      - label: "Overview"
        path: "/api/overview"
      - label: "Generated API"
        autoGenerate: true
```

---

## Active State Highlighting

MokaDocs applies visual highlighting to indicate the user's current position in the navigation.

### Current Page

The navigation item corresponding to the current page receives an `active` CSS class. This is styled with a distinct background color and bold text in the default theme, using the configured `primaryColor`.

### Parent Active State

All ancestor sections of the current page receive a `parent-active` CSS class. This provides a subtle visual indication of the current section hierarchy without the full emphasis of the active page highlight.

**Example:** When viewing the page at `/guides/advanced/plugins`:

| Nav Item | State |
|---|---|
| Guides | `parent-active` |
| Advanced | `parent-active` |
| Plugins | `active` |
| Basics | (no state) |

### Styling

In the default theme:
- `active` items have a highlighted background using the primary color at reduced opacity, bold font weight, and a left border accent.
- `parent-active` items have a subtle background tint to indicate they are on the path to the active page.
- All other items have no special styling.

---

## Section Index Pages

### `index.md` Behavior

An `index.md` file inside a directory serves a dual purpose:

1. **Section metadata** - Its front matter (`title`, `order`, `icon`, `expanded`) defines how the section appears in the sidebar.
2. **Section landing page** - Its content is rendered when the user clicks the section header (if the section header is clickable).

### How Section Headers Behave

| Scenario | Section Header Behavior |
|---|---|
| Directory has `index.md` with content | Section header is clickable and navigates to the index page |
| Directory has `index.md` with only front matter (no content) | Section header toggles expansion only (not clickable as a link) |
| Directory has no `index.md` | Section header toggles expansion only; section title is derived from the directory name |

### Example

```
docs/
  guides/
    index.md       # Has content -> "Guides" header links to /guides/
    writing.md
    theming.md
  reference/
    index.md       # Front matter only -> "Reference" header just toggles
    classes.md
    interfaces.md
```

### Section Title Resolution

The section title (displayed in the sidebar) is determined by this priority:

1. The `title` from the section's `index.md` front matter
2. The `label` from the `nav` config (if manually defined)
3. The directory name converted to title case (e.g., `getting-started` becomes "Getting Started")

---

## Route Generation from File Paths

MokaDocs generates URL routes from the file system path of each Markdown file, relative to the `content.docs` directory.

### Route Generation Rules

| Rule | Example Path | Generated Route |
|---|---|---|
| Extension is removed | `installation.md` | `/installation` |
| `index.md` maps to parent directory | `guides/index.md` | `/guides` |
| Directory separators become path segments | `guides/theming.md` | `/guides/theming` |
| Root `index.md` maps to `/` | `index.md` | `/` |
| Filenames are lowercased | `QuickStart.md` | `/quickstart` |
| Spaces and underscores become hyphens | `quick_start.md` | `/quick-start` |
| Multiple hyphens are collapsed | `my--page.md` | `/my-page` |
| Leading/trailing hyphens are trimmed | `-about-.md` | `/about` |

### Route Override

Any page can override its generated route using the `route` front matter property:

```yaml
---
title: "Frequently Asked Questions"
route: "/faq"
---
```

The file `docs/support/frequently-asked-questions.md` would normally produce the route `/support/frequently-asked-questions`, but the override changes it to `/faq`.

### Route Conflicts

If two pages resolve to the same route (whether through auto-generation, override, or a combination), MokaDocs produces a build error identifying both files. Resolve the conflict by renaming one file or using a `route` override.

---

## Putting It All Together

### Recommended Approach

For most projects, a hybrid approach works best:

1. Let auto-generation handle the majority of your documentation pages using directory structure and `order` front matter.
2. Use the `nav` config only when you need precise control over grouping, ordering, or labeling that cannot be achieved through file organization alone.
3. Use `autoGenerate: true` for API reference sections to keep them in sync with your codebase.

### Example: Hybrid Navigation

```yaml
# mokadocs.yaml
nav:
  - label: "Getting Started"
    icon: "rocket"
    expanded: true
    children:
      - label: "Installation"
        path: "/getting-started/installation"
      - label: "Quick Start"
        path: "/getting-started/quick-start"
      - label: "Configuration"
        path: "/getting-started/configuration"

  # This section auto-generates from the /guides directory
  # but with a custom icon and label
  - label: "Developer Guides"
    icon: "book-open"
    expanded: true
    children:
      - label: "Writing Content"
        path: "/guides/writing-content"
      - label: "Themes & Styling"
        path: "/guides/theming"
      - label: "Deploying Your Site"
        path: "/guides/deployment"

  - label: "API Reference"
    icon: "code"
    autoGenerate: true

  - label: "Resources"
    icon: "link"
    children:
      - label: "FAQ"
        path: "/faq"
        icon: "help-circle"
      - label: "Changelog"
        path: "/changelog"
        icon: "history"
      - label: "Contributing"
        path: "/contributing"
        icon: "users"
```

This gives you full control over the top-level structure and ordering, while API reference pages are automatically kept in sync with your source code.
