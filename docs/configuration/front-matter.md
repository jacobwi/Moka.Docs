---
title: Front Matter
order: 2
---

# Front Matter

Every Markdown documentation page in MokaDocs can start with a YAML front matter block, enclosed between triple-dash delimiters (`---`). It sets the page's title and controls where and how the page appears.

## Syntax

Front matter goes at the top of the file, before any other content:

```markdown
---
title: "My Page Title"
description: "A brief summary of this page."
order: 3
---

Your page content starts here.
```

The block is parsed as YAML. Quote string values that contain special characters. Keys MokaDocs doesn't know are ignored.

If the block isn't valid YAML, or a value has the wrong type (such as `order: first`, or `tags: api` where a list is expected), MokaDocs ignores the whole block and reports a warning that names the file. The page gets the defaults listed below, including the title "Untitled".

---

## Properties Reference

### `title`

- **Type:** `string`
- **Default:** `"Untitled"`

The page title. This value is used in several places:

- The `<title>` tag in the browser tab. The default layout writes `Page Title - Site Title`.
- The sidebar label, unless a `nav` item's `label` replaces it.
- Search results, breadcrumbs and the previous/next links.
- The `og:title` and `twitter:title` meta tags, which the default layout writes when `site.url` is set.

The default layout doesn't render the title as a heading. Start the content with a `#` heading if the page needs one.

```yaml
---
title: "Getting Started with MokaDocs"
---
```

A page without a `title` is called "Untitled" everywhere, and the build doesn't warn about it. `mokadocs doctor` lists pages that have no title, and `mokadocs doctor --fix` adds one based on the file name (the folder name, for `index.md`).

### `description`

- **Type:** `string`
- **Default:** `""`

A short summary of the page. It fills the `<meta name="description">` tag, and the default layout also writes it to `og:description` and `twitter:description` when `site.url` is set. A page without a description uses `site.description` in those tags instead. The `landing` layout also shows the description under the site title and appends it to the `<title>` tag.

Search results don't use it. The snippet under a result comes from the page text.

```yaml
---
title: "Installation"
description: "How to install MokaDocs via the .NET CLI, NuGet, or from source."
---
```

### `order`

- **Type:** `int`
- **Default:** `0`

Sort position of the page among its siblings in the sidebar. Lower numbers come first, and pages with the same `order` are sorted by title. It applies to auto-generated navigation and to the pages a `nav` item lists from its `path`. Items written in the `nav` section use their own `order` field.

```yaml
---
title: "Installation"
order: 1
---
```

```yaml
---
title: "Quick Start"
order: 2
---
```

```yaml
---
title: "Configuration"
order: 3
---
```

In this example, the sidebar within the section would display: Installation, Quick Start, Configuration.

**Sorting behavior in detail:**

| Scenario | Result |
|---|---|
| All pages have `order` | Sorted by `order`, ascending |
| No pages have `order` | All count as `0`, so they're sorted by title |
| Some pages have `order`, some do not | Pages without `order` count as `0`. They sort after negative values and before `order: 1` and up |
| Multiple pages share the same `order` | Sorted by title, ignoring case |
| Negative values | Allowed; they sort before `0` |

The previous/next links at the bottom of a page follow `order` too. They step through the site's public pages whose `layout` is `default`, sorted by `order` and then by route.

See [Navigation & Sidebar](/configuration/navigation) for how sections are ordered.

### `icon`

- **Type:** `string` (nullable)
- **Default:** `null`

An icon shown next to the page's link in the sidebar. Only the names bundled with the default theme render; the list is in [Navigation & Sidebar](/configuration/navigation#icon-support). An unknown name shows nothing.

```yaml
---
title: "Installation"
icon: "download"
---
```

```yaml
---
title: "API Reference"
icon: "code"
---
```

In auto-generated navigation, every page shows its icon, and a top-level section shows the icon from its folder's `index.md`. With a `nav` section, the pages an item lists from its `path` show their icons, but an item that links to the page directly uses its own `icon` instead. The default theme never draws an icon on a second-level item that has children, or on a third-level item.

### `layout`

- **Type:** `string`
- **Default:** `"default"`

The layout template to use when rendering this page. The default theme has two:

| Layout | Description |
|---|---|
| `default` | Documentation page with the sidebar, content area and table of contents. |
| `landing` | Home page layout with no sidebar or table of contents. It shows the site title and the page `description` at the top, then the cards from [`features`](#features) when the page has any, then the page content. |

`api-type` is also accepted and renders the same template as `default`. Any other name, such as `wide`, falls back to `default`, and the build warns once for each unknown name.

```yaml
---
title: "Welcome to My Docs"
layout: "landing"
---
```

A custom theme replaces these with the files in its `layouts/` folder: `layouts/docs.html` is the layout `docs`. Unknown names fall back to the theme's `default.html`. Plugins have no way to add layouts.

### `tags`

- **Type:** `list` of `string`
- **Default:** `[]`

Keywords for the page. They go into the search index, and the search box matches them: a tag match ranks below a match in a title or heading, and above a match in the page text. Tags are stored as written, and the search box ignores case when matching.

The default theme doesn't display tags. A custom theme's templates can read them as `page.tags`.

```yaml
---
title: "Working with Dependency Injection"
tags:
  - dependency-injection
  - advanced
  - patterns
---
```

`tags` must be a YAML list, either as above or as `tags: [advanced, patterns]`. A single string such as `tags: advanced` invalidates the whole front matter block.

### `visibility`

- **Type:** `string`
- **Default:** `"public"`

One of `public`, `hidden` or `draft`, in any letter case. Any other value counts as `public`.

#### `public`

The page appears in the sidebar and in search, and it's listed in the sitemap. MokaDocs writes `sitemap.xml` only when `site.url` is set and `build.sitemap` is on (the default).

```yaml
---
title: "Getting Started"
visibility: "public"
---
```

#### `hidden`

The page is built and reachable by its URL, and search still finds it. It's left out of the sidebar and the sitemap. A `nav` item can still link to a hidden page through its `path`.

```yaml
---
title: "Legacy Migration Notes"
visibility: "hidden"
---
```

**Use cases for hidden pages:**

- Supplementary content linked from other pages
- Legacy pages that should remain accessible but not prominently listed
- Content that is contextually linked but does not belong in navigation

#### `draft`

Draft pages are skipped unless the command runs with `--draft`. With the flag, they're written and indexed by search, but they still don't appear in the sidebar or the sitemap.

```yaml
---
title: "Upcoming v4.0 Features"
visibility: "draft"
---
```

To include drafts in the build:

```bash
mokadocs build --draft
mokadocs serve --draft
```

**Visibility comparison:**

| Behavior | `public` | `hidden` | `draft` |
|---|---|---|---|
| Appears in sidebar | Yes | No | No |
| Written to the output | Yes | Yes | Only with `--draft` |
| Included in search index | Yes | Yes | Only with `--draft` |
| Included in sitemap | Yes | No | No |

### `toc`

- **Type:** `bool`
- **Default:** `true`

Controls whether the "On this page" table of contents is displayed. It lists the page's headings, H1 included, down to the level set by `theme.options.tocDepth` (default `3`). The default theme nests entries three levels deep at most, so on a page that starts with an H1, headings below H3 don't appear even with a higher `tocDepth`. The table is also hidden when the page has no headings or `theme.options.showTableOfContents` is `false`.

```yaml
---
title: "Changelog"
toc: false
---
```

Set it to `false` for pages with few headings, or where a table of contents would get in the way (changelog pages, for example).

### `expanded`

- **Type:** `bool`
- **Default:** `true`

Whether a sidebar section starts expanded. It's read in two places:

- The `index.md` of a top-level folder in auto-generated navigation. `expanded: false` starts that section collapsed.
- A page that a `nav` item lists from its `path`, when that page has pages below it.

```yaml
---
title: "Advanced Topics"
expanded: false
---
```

Items written in the `nav` section ignore it and use their own `expanded` field, which defaults to `false`. A section that contains the current page is always expanded when the page loads.

### `route`

- **Type:** `string` (nullable)
- **Default:** `null` (generated from the file path)

Overrides the URL path for this page. By default, routes are generated from the file path relative to the docs directory (see [Navigation & Sidebar](/configuration/navigation) for the rules).

```yaml
---
title: "Frequently Asked Questions"
route: "/faq"
---
```

**Examples of route overrides:**

| File Path | Default Route | Custom Route |
|---|---|---|
| `docs/guides/faq.md` | `/guides/faq` | `/faq` |
| `docs/reference/api-v2.md` | `/reference/api-v2` | `/api-v2` |
| `docs/about/team.md` | `/about/team` | `/team` |

**Notes:**

- A missing leading slash is added and a trailing slash is removed, so `help/faq/` becomes `/help/faq`.
- A `nav` item that links to the page needs the new route as its `path`.
- Relative Markdown links to the file, such as `[FAQ](../guides/faq.md)`, resolve to the file's location (`/guides/faq`), not to the override. Link to `/faq` instead.
- If another page has the same route (ignoring case), the build warns that the pages share the route and writes only the last one.

### `version`

- **Type:** `string` (nullable)
- **Default:** `null`

MokaDocs reads this key but doesn't act on it. A page with `version` is built and listed like any other page, whether or not versioning is enabled.

### `requires`

- **Type:** `string` (nullable)
- **Default:** `null`

The name of a feature flag the page depends on. When the flag is off, the page is removed before navigation, search and the sitemap are built, and it isn't written to the output.

```yaml
---
title: "Beta Features"
requires: ShowBetaDocs
---
```

Flag values come from built-in defaults and from environment variables named `MOKADOCS_FeatureManagement__<Flag>`. They can't be set in `mokadocs.yaml`. To turn a flag on for a build:

```bash
MOKADOCS_FeatureManagement__ShowBetaDocs=true mokadocs build
```

```powershell
$env:MOKADOCS_FeatureManagement__ShowBetaDocs = "true"
mokadocs build
```

Flag names ignore case. A name with no built-in default and no environment variable counts as off, so the page is left out. You can gate pages on a name of your own this way: `requires: PartnerDocs` hides the page until `MOKADOCS_FeatureManagement__PartnerDocs=true` is set.

Flags only decide which pages are built. Turning one on doesn't switch on the feature it's named after, and the built-in defaults don't follow `mokadocs.yaml`: `requires: SearchBar` keeps the page even when `theme.options.showSearch` is `false`.

**Built-in flags:**

| Default | Flags |
|---|---|
| On | `BackToTop`, `Breadcrumbs`, `CodeLanguageBadge`, `ColorThemeSelector`, `CopyButton`, `DarkModeToggle`, `FeedbackWidget`, `InheritDocResolution`, `InstallWidget`, `LastUpdated`, `LineNumbers`, `MinifyOutput`, `PrevNextNavigation`, `RobotsTxt`, `SearchBar`, `SearchIndex`, `Sitemap`, `TableOfContents`, `TypeDependencyGraph`, `VersionSelector`, `ViewSource` |
| Off | `ShowBetaDocs`, `ShowCloudDocs`, `ShowInternalDocs`, `ShowPremiumDocs`, `AiSearch`, `Analytics`, `ApiAccess`, `AuditLog`, `BlazorPreview`, `ChangelogPlugin`, `Cloud`, `CodeStyleSelector`, `CodeThemeSelector`, `Contributors`, `CustomBranding`, `CustomDomain`, `EditLink`, `OpenApiPlugin`, `PageAnimations`, `PdfExport`, `PrivateRepo`, `ReplPlugin`, `SSOAuth`, `TeamCollaboration`, `WhiteLabel` |

### `features`

- **Type:** `list` of objects
- **Default:** `[]`

Feature cards for the `landing` layout. They appear in a grid between the hero section and the page content, three to a row on wide screens. A landing page without `features` has no card section. The `default` layout ignores this key.

Each card takes these keys, all optional:

| Key | Contents |
|---|---|
| `title` | The card heading |
| `description` | The text under the heading |
| `icon` | A name from the [icon list](/configuration/navigation#icon-support), drawn as that icon. Any other value, such as `C#` or an emoji, is shown as text |
| `link` | A URL that makes the whole card a link. A value starting with `/` gets the base path |

```yaml
---
title: "Widgets"
description: "UI widgets for .NET"
layout: landing
featuresTitle: "Why Widgets"
featuresSubtitle: "Everything ships in one package"
features:
  - title: "Fast"
    icon: "zap"
    description: "Renders in under a millisecond."
    link: "/guide/performance"
  - title: "Typed"
    icon: "C#"
    description: "Every option is a C# property."
---
```

Titles, descriptions and text icons are HTML-escaped, so `<b>` shows up as written rather than making text bold. Links are used as written apart from the base path: point them at a page's route, such as `/guide/performance`, not at its `.md` file.

`features` must be a YAML list. A single value such as `features: Fast` invalidates the whole front matter block.

A custom theme's templates can read the cards as `page.features`, see [Template Variables](/advanced/architecture#template-variables).

### `featuresTitle`

- **Type:** `string`
- **Default:** `""`

The heading above the landing layout's feature cards. The cards have no heading without it, and it has no effect on a page without `features`.

### `featuresSubtitle`

- **Type:** `string`
- **Default:** `""`

A line of text under `featuresTitle`. Like the title, it only shows on a page with `features`.

---

## Complete Example

A page using most of the properties. `requires` is left out because it hides the page unless its flag is on, and `version` has no effect.

```yaml
---
title: "Dependency Injection Guide"
description: "Learn how to configure and use dependency injection with the Contoso SDK."
order: 5
icon: "puzzle"
layout: "default"
tags:
  - dependency-injection
  - configuration
  - advanced
visibility: "public"
toc: true
expanded: true
route: "/guides/di"
---

# Dependency Injection Guide

This guide covers how to configure dependency injection...
```

## Front Matter Defaults

When front matter properties are omitted, the following defaults apply:

| Property | Default Value |
|---|---|
| `title` | `"Untitled"` |
| `description` | `""` (meta tags use `site.description`) |
| `order` | `0` |
| `icon` | `null` (no icon) |
| `layout` | `"default"` |
| `tags` | `[]` (no tags) |
| `visibility` | `"public"` |
| `toc` | `true` |
| `expanded` | `true` |
| `route` | Generated from the file path |
| `version` | `null` (not used) |
| `requires` | `null` (no feature gate) |
| `features` | `[]` (no feature cards) |
| `featuresTitle` | `""` |
| `featuresSubtitle` | `""` |

## Tips

- Always set `title`. Without one, the page is called "Untitled" in the sidebar, the browser tab and search results.
- Set `description` on every page. Otherwise the page's meta description and link previews fall back to `site.description`.
- Give every page in a section an `order`, or none of them. Pages without `order` count as `0` and sort before pages with `order: 1`.
- Prefer `visibility: "hidden"` over deleting pages when you want to remove something from the sidebar but keep the URL working (to avoid broken links).
- Use `visibility: "draft"` for work in progress rather than keeping draft files outside the docs directory.
