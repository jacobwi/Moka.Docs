---
title: Navigation & Sidebar
order: 3
---

# Navigation & Sidebar

MokaDocs generates the sidebar from your page routes, unless `mokadocs.yaml` has a `nav` section. Then the sidebar follows that section instead.

---

## Auto-Generated Navigation

### How It Works

1. The sidebar is a tree of the page routes. Every folder is a section and every page is an entry in its folder's section, so `/guides/theming` sits in `Guides` and `/guides/advanced/plugins` in `Advanced` inside `Guides`.
2. When a page exists at a section's own route (a folder's `index.md`), the section header links to it and takes its `title`, `icon`, `order` and `expanded` values. A folder without one is labeled from its name.
3. Generated API type pages are grouped by namespace under the `/api` section instead of by route, one collapsed section per namespace. See [API Reference Items](#api-reference-items).
4. The root `index.md` (route `/`) is a top-level item like any other.
5. The entries of every section, and the top-level items, are sorted by `order` and then by title.
6. The tree is at most three levels deep, the depth the default theme renders. The pages below a third-level folder are listed after it, prefixed with the folders they are in: `Advanced / Caching`.

The tree follows routes, not files, so a page with a `route` override goes where its new route puts it. Hidden and draft pages are left out; see `visibility` in [Front Matter](/configuration/front-matter).

### Example

Given this directory layout:

```
docs/
  index.md              # title: Home, order: -1
  changelog.md          # title: Changelog, order: 3
  getting-started/
    index.md            # title: Getting Started, order: 1
    installation.md     # title: Installation, order: 1
    quick-start.md      # title: Quick Start, order: 2
  guides/
    index.md            # title: Guides, order: 2
    writing-content.md  # title: Writing Content
    theming.md          # title: Theming
    advanced/
      index.md          # title: Advanced
      plugins.md        # title: Plugins
  reference/
    cli.md              # title: CLI
```

MokaDocs generates this sidebar:

```
Home
Reference
  CLI
Getting Started
  Installation
  Quick Start
Guides
  Advanced
    Plugins
  Theming
  Writing Content
Changelog
```

- `Home` comes first because the root `index.md` has `order: -1`.
- `reference/` has no `index.md`, so its label comes from the folder name and its header doesn't link anywhere. With no `order` it counts as `0`, which puts it between `Home` and `Getting Started`.
- `advanced/` is a section inside `Guides`, titled and linked by its `index.md`, and sorted by title with the other entries of `Guides`.

---

## Manual Navigation via `nav` Config

A `nav` section in `mokadocs.yaml` replaces auto-generated navigation. The sidebar then contains only the items you list and the pages they pull in through `path`.

```yaml
nav:
  - label: "Home"
    path: /
    icon: home
    order: -1

  - label: "Getting Started"
    path: /getting-started
    icon: rocket
    expanded: true

  - label: "Guides"
    icon: book-open
    children:
      - label: "Writing Content"
        path: /guides/writing-content
      - label: "Theming"
        path: /guides/theming

  - label: "Reference"
    path: /reference

  - label: "Changelog"
    path: /changelog
    icon: scroll-text
    order: 10
```

With the directory layout above, this gives:

```
Home
Getting Started
  Installation
  Quick Start
Guides
  Writing Content
  Theming
Reference
  CLI
Changelog
```

`Getting Started` gets its children from its `path`. `Guides` shows exactly the two children listed. There is no page at `/reference`, so the `Reference` header links to its first child, `/reference/cli`.

### NavItem Properties

Each item in the `nav` list supports the following properties:

| Property | Type | Default | Description |
|---|---|---|---|
| `label` | `string` | `""` | Text shown in the sidebar. It's used even when `path` points at a page with a different title. |
| `path` | `string` | none | Route the item links to. A missing leading `/` is added. |
| `icon` | `string` | none | Icon name from the list in [Icon Support](#icon-support). |
| `order` | `int` | `0` | Position among sibling items. Lower values come first. Items with the same value keep their order from `mokadocs.yaml`. |
| `expanded` | `bool` | `false` | Whether the item's children start expanded. |
| `autoGenerate` | `bool` | `false` | List everything below `path` as a tree, the way the sidebar is built without a `nav` section. Ignored when the item has `children`. |
| `children` | `list` | `[]` | Child items. |

### How Items Are Built

- **With `children`**: the listed children are used and nothing is generated.
- **With `path` and `autoGenerate: true`**: the children are the part of the [auto-generated tree](#how-it-works) below `path`. Every folder is a section, with or without an `index.md`, and under `/api` the type pages are grouped by namespace. The tree still stops at the third level of the sidebar, counting the item's own level.
- **With `path` and no `children`**: public pages exactly one route segment below `path` become the children, sorted by front matter `order` and then title. Each of those pages gets its own children the same way, so a subfolder with an `index.md` becomes a nested section. Pages in a subfolder without an `index.md` don't appear, because there is no page at the subfolder's route to list them under.
- **Link target**: the item links to `path`. When no page exists there and the item has children, it links to its first child instead. When there is no page at `path`, no children, and no page further below it, the link is dead and the build warns about the item by label.
- **Without `path`**: the header is a plain label. An item with neither `path` nor `children` renders as a link with an empty `href`.

A few details about `path`:

- `guides` and `/guides` are the same.
- A trailing slash is removed, so `path: /guides/` works the same as `path: /guides`.
- Matching a page ignores case, but the link uses `path` exactly as written, and the current-page highlight compares case. Write it in the same case as the route.

### API Reference Items

Generated API pages have routes like `/api/contoso/sdk/contosoclient`: the namespace with its dots turned into slashes, then the type name, all lowercase. None of them sit one segment below `/api`, so a `path: /api` item gets no children. It links to the API index page at `/api`, which lists every namespace and type.

```yaml
nav:
  - label: "API Reference"
    path: /api
    icon: code
```

With `autoGenerate: true`, the item lists one section per namespace, each holding its types sorted by name. The namespace sections start collapsed; the one that holds the current page opens. A namespace has no page of its own, so its header is a plain label.

```yaml
nav:
  - label: "API Reference"
    path: /api
    icon: code
    autoGenerate: true
```

```
API Reference
  Contoso.Sdk
    ContosoClient
    ContosoOptions
  Contoso.Sdk.Models
    Order
```

Without a `nav` section, the `API Reference` item has the same namespace sections. The ASP.NET Core host builds its `AutoGenerate` entries with the same code, so both give the same sidebar.

---

## How `order` Affects Sorting

Front matter `order` sorts pages in auto-generated navigation and among the children a nav item generates from `path`. Items you write in the `nav` section use their own `order` field instead.

### Sorting Rules

1. Pages are sorted by `order`, lowest first.
2. A page without `order` counts as `0`, so it sorts before pages with `order: 1` or higher.
3. Pages with the same `order` are sorted by title, ignoring case.
4. In auto-generated navigation, a section's position among its siblings comes from the `order` in its folder's `index.md`. A folder without an `index.md` counts as `0`. Sections and pages are sorted together.

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

1. Advanced Usage (no order, counts as 0)
2. Troubleshooting (no order, counts as 0)
3. Installation (order: 1)
4. Quick Start (order: 2)
5. Configuration (order: 3)

To move Advanced Usage and Troubleshooting to the end, give them an `order` higher than `3`.

### Negative Order Values

Negative `order` values are allowed and sort before `0`. Use one to pin an "Overview" or "Introduction" page to the top:

```yaml
---
title: "Overview"
order: -1
---
```

### Section (Directory) Ordering

To order the sections of auto-generated navigation, set `order` in each section's `index.md`:

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

Top-level pages such as `docs/changelog.md` and the root `index.md` are sorted in the same list, so give them an `order` too.

---

## Icon Support

Nav items and front matter both take an `icon` name. The default theme bundles a fixed set of Lucide icons, and only these names render:

| Group | Names |
|---|---|
| Navigation | `home`, `menu`, `search`, `settings`, `arrow-left`, `arrow-right`, `chevron-right`, `chevron-down`, `external-link` |
| Content | `book`, `book-open`, `file`, `file-text`, `file-code`, `folder`, `newspaper`, `scroll-text` |
| Development | `code`, `code-2`, `terminal`, `braces`, `cpu`, `database`, `git-branch`, `package`, `puzzle` |
| Actions and status | `rocket`, `zap`, `check`, `x`, `alert-triangle`, `info`, `lightbulb`, `star`, `heart`, `download`, `upload` |
| Communication | `mail`, `message-circle`, `globe`, `users`, `link`, `github`, `twitter`, `nuget`, `discord` |
| Objects | `shield`, `key`, `lock`, `clock`, `calendar`, `image`, `play`, `list`, `layers`, `tag`, `wrench`, `box`, `compass`, `map` |

Names ignore case. Any other name renders nothing, and the build warns once for each unknown name.

### Where Icons Appear

- **`nav` items** show their own `icon`. They don't fall back to the front matter `icon` of the page at `path`.
- **Pages a nav item pulls in through `path`** show their front matter `icon`.
- **Auto-generated navigation** shows the front matter `icon` of every page in the sidebar. A section takes its icon from its folder's `index.md`, so a folder without one has no icon.
- **Depth:** the default theme draws icons on top-level items and on second-level items without children. Second-level section headers and third-level items don't get one.

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
    icon: "upload"
    children:
      - label: "Azure"
        path: "/deployment/azure"
        icon: "globe"
      - label: "Docker"
        path: "/deployment/docker"
        icon: "box"
```

---

## Nested Sections and Expansion

### Creating Nested Sections

Auto-generated navigation nests a section for every subfolder. In a `nav` section, nest items with `children`, point `path` at a folder whose subfolders have an `index.md`, or add `autoGenerate: true` to list a folder's whole tree.

The default theme renders three levels at most: top-level items, their children and their grandchildren. Auto-generated navigation, with or without `nav`, lists the pages below the third level right after their third-level folder, labeled with the folders they are in: under `Guides` > `Advanced`, a page at `/guides/advanced/plugins/hooks/events` shows as `Plugins / Hooks / Events`, after the `Plugins` page. In items you write in `nav`, anything deeper than three levels is not shown.

Given this layout, where each page's `title` matches its name and no page sets `order`:

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

Auto-generated navigation, and a nav item with `path: /guides`, both nest the subfolders:

```yaml
nav:
  - label: "Guides"
    path: /guides
```

```
Guides
  Advanced
    Custom Components
    Plugins
  Basics
    Front Matter
    Markdown Syntax
```

### Controlling Expansion

| Section | Starts | Change it with |
|---|---|---|
| Auto-generated section | Expanded | `expanded: false` in the folder's `index.md` |
| Auto-generated API namespace | Collapsed | Can't be changed |
| `nav` item | Collapsed | `expanded: true` on the item |
| Page pulled in by a nav `path` that has pages below it | Expanded | `expanded: false` in that page's front matter |

A section that contains the current page is always expanded when the page loads.

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
    expanded: true
    children:
      - label: "Custom Components"
        path: "/advanced/custom-components"
      - label: "Plugin Development"
        path: "/advanced/plugin-development"
```

**Expansion behavior:**

- Only the chevron button next to a section header expands or collapses it. The header text is a link when a page exists at the section's route.
- Expansion isn't remembered. Each page load starts from the defaults above.

---

## Large Sidebars: Inline or Shared

Every page shows the whole sidebar, and by default every page also holds a copy of it. That is fine for a few hundred entries, and the pages work without JavaScript. On a big site the copies are most of the output: 5000 pages with an entry each wrote 6.5 GB of HTML, about 1.3 MB per page, and took 65 seconds to build.

`theme.options.sidebar` picks how pages get the sidebar:

| Mode | What pages hold |
|---|---|
| `auto` (default) | `inline` up to `theme.options.sidebarInlineLimit` entries (default `300`), `shared` above that |
| `inline` | The whole sidebar, in every page |
| `shared` | The top-level entries and the entries along the path to the page. The whole sidebar is written once, to `_theme/js/sidebar.js` |

```yaml
theme:
  options:
    sidebar: shared          # auto | inline | shared
    sidebarInlineLimit: 300  # used by auto
```

The limit counts every entry of the sidebar, links and section headings alike, not the pages of the site. `MOKADOCS_SIDEBAR` and `MOKADOCS_SIDEBAR_INLINE_LIMIT` override both settings. An unknown mode works like `auto`, and the build warns about it.

How a shared sidebar works:

- Each page loads `_theme/js/sidebar.js?v=<hash>` from a `<script>` tag inside the sidebar. The `v` value is a hash of the file, so a rebuild that changes the sidebar changes the URL, and browsers can keep the file cached between pages.
- The script runs before the rest of the page is shown. It puts the whole sidebar in place of the page's short one, marks the current page and opens the sections around it the same way the inline sidebar does, and scrolls the sidebar so the current page is in view. The reader never sees the tree collapsed or the short sidebar swapped out.
- Without JavaScript, and for crawlers, the page keeps its short sidebar: every top-level entry, and the entries of each section on the path to the page. A section off that path links to its own route (or to its first page when it has none), so every part of the site stays reachable. The previous and next links at the bottom of each page follow the whole sidebar either way.
- The default theme only. A custom theme always gets the whole sidebar in every page; asking it for `shared` is a warning.

On the 5000-page site, the shared sidebar file is 503 KB (34 KB with gzip), a page is about 42 KB, and the build wrote 258 MB in 10 seconds.

---

## Active State Highlighting

### Current Page

The sidebar link for the current page gets the `current` CSS class. The default theme draws it in the primary color with a tinted background, bold text and a left border. With a [shared sidebar](#large-sidebars-inline-or-shared), the script adds this class and the ones below, so the markup ends up the same.

### Parent Active State

A top-level item that contains the current page gets `parent-active` on its header. The default theme shows that header in bolder text, with no background tint. Second-level section headers don't get the class.

The wrapper element also gets an `active` class when it is or contains the current page: `nav-section` for top-level items, `nav-sub-section` for second-level sections. The default theme doesn't style it.

**Example:** viewing `/guides/basics/markdown-syntax` with the `path: /guides` nav item from the previous section:

| Element | Class added |
|---|---|
| `Guides` wrapper (`nav-section`) | `active` |
| `Guides` header link | `parent-active` |
| `Basics` wrapper (`nav-sub-section`) | `active` |
| `Basics` header link | (none) |
| `Markdown Syntax` link | `current` |
| `Advanced` and its links | (none) |

---

## Section Index Pages

### `index.md` Behavior

In auto-generated navigation, a folder's `index.md` is the page for its section, at any depth:

- Its `title` is the section label. Without a `title`, the label is "Untitled".
- Its `icon` is shown next to the label (on top-level sections; see [Where Icons Appear](#where-icons-appear)).
- Its `order` sets the section's position among its siblings.
- Its `expanded` sets whether the section starts expanded.
- The section header links to it.

An `index.md` with only front matter still produces a page, so the header links to it.

### How Section Headers Behave

| Scenario | Section Header Behavior |
|---|---|
| Folder has an `index.md` | Links to the index page. The label comes from its `title`. |
| Folder has no `index.md` | Plain label from the folder name, with the first letter uppercased and hyphens turned into spaces (`getting-started` becomes "Getting started"). The section counts as `order: 0`. |

When a folder has no index page, the build writes a redirect at the folder's URL to the alphabetically first page inside it.

### Section Labels in Nav Config

In a `nav` section, an item's `label` is always the text shown, whatever the page's `title` says. Children generated from `path` use their page's `title`.

---

## Route Generation from File Paths

Every `.md` file under `content.docs` becomes a page, except files inside the build output folder. The route is the file's path relative to `content.docs`.

### Route Generation Rules

| Rule | Example Path | Generated Route |
|---|---|---|
| Extension is removed | `installation.md` | `/installation` |
| Directory separators become path segments | `guides/theming.md` | `/guides/theming` |
| `index.md` maps to its directory | `guides/index.md` | `/guides` |
| Root `index.md` maps to `/` | `index.md` | `/` |

Nothing else changes. Case and underscores are kept, so `QuickStart_Guide.md` becomes `/QuickStart_Guide`.

### Route Override

Any page can replace its generated route with the `route` front matter property:

```yaml
---
title: "Frequently Asked Questions"
route: "/faq"
---
```

The file `docs/support/frequently-asked-questions.md` would normally produce the route `/support/frequently-asked-questions`, but the override changes it to `/faq`.

- A missing leading slash is added and a trailing slash is removed.
- A nav item that links to the page needs `path: /faq`.
- Relative Markdown links to the file, such as `[FAQ](../support/frequently-asked-questions.md)`, still resolve to `/support/frequently-asked-questions`, where there is no page. Link to `/faq` instead.

### Route Conflicts

When two pages end up with the same route, the build writes only the last one and warns:

```
2 pages share the route '/faq' (faq.md, help.md); only the last one is written
```

Routes that differ only by case count as the same route. This happens when a `route` override matches another page's route, or when a Markdown page sits at the route of a generated page such as `/api`. `mokadocs validate` lists the warning; `mokadocs build` counts it in its summary and lists it with `--verbose`.

---

## Putting It All Together

### Recommended Approach

1. Start with auto-generated navigation. Give each folder an `index.md` with a `title` and an `order`, and order the pages inside with front matter.
2. Add a `nav` section when you need labels that differ from page titles, or a sidebar that doesn't follow the folders.
3. Inside `nav`, use `path` with `autoGenerate: true` to list a folder's whole tree without naming each page, or `path` alone for the pages one level down. Use `children` when you want to choose and order them by hand.

### Example: Mixed Navigation

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

  # Children come from the public pages under /guides
  - label: "Developer Guides"
    path: "/guides"
    icon: "book-open"
    expanded: true

  - label: "API Reference"
    path: "/api"
    icon: "code"

  - label: "Resources"
    icon: "link"
    children:
      - label: "FAQ"
        path: "/faq"
        icon: "info"
      - label: "Changelog"
        path: "/changelog"
        icon: "scroll-text"
      - label: "Contributing"
        path: "/contributing"
        icon: "users"
```

`Getting Started` and `Resources` show exactly the pages listed. `Developer Guides` picks up new pages under `/guides` without a config change. `API Reference` links to the API index page.
