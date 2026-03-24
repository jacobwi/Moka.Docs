---
title: Front Matter
order: 2
---

# Front Matter

Every Markdown documentation page in MokaDocs can include a YAML front matter block at the top of the file. Front matter is enclosed between triple-dash delimiters (`---`) and defines metadata that controls how the page is rendered, organized, and discovered.

## Syntax

Front matter must appear at the very beginning of the file, before any other content:

```markdown
---
title: "My Page Title"
description: "A brief summary of this page."
order: 3
---

Your page content starts here.
```

The front matter block is parsed as standard YAML. String values containing special characters should be quoted.

---

## Properties Reference

### `title`

- **Type:** `string`
- **Required:** Yes

The page title. This value is used in multiple places:

- The `<title>` tag in the browser tab (combined with the site title)
- The `<h1>` heading rendered at the top of the page (unless the page content already begins with an `# H1` heading)
- The sidebar navigation label (unless overridden by the `nav` config)
- The `og:title` and `twitter:title` meta tags

```yaml
---
title: "Getting Started with MokaDocs"
---
```

If a page does not have a `title` in its front matter and does not start with an H1 heading, the filename (converted from kebab-case or snake_case to title case) is used as a fallback.

### `description`

- **Type:** `string`
- **Default:** `""`

A brief description of the page content. This is used for the `<meta name="description">` tag (important for SEO), the `og:description` meta tag, and as the snippet text in search results.

```yaml
---
title: "Installation"
description: "How to install MokaDocs via the .NET CLI, NuGet, or from source."
---
```

### `order`

- **Type:** `int`
- **Default:** `0`

Controls the sort position of this page within its section in the sidebar navigation. Pages are sorted by `order` in ascending order, with lower numbers appearing first. Pages with the same `order` value are sorted alphabetically by title.

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
| All pages have `order` | Sorted by `order` ascending |
| No pages have `order` | Sorted alphabetically by title |
| Some pages have `order`, some do not | Pages with `order` appear first (sorted by value), then pages without `order` (sorted alphabetically) |
| Multiple pages share the same `order` | Ties are broken alphabetically by title |
| Negative values | Allowed; pages with negative order appear before pages with order `0` |

### `icon`

- **Type:** `string` (nullable)
- **Default:** `null`

A Lucide icon name to display next to the page title in the sidebar navigation. Icons provide visual cues that help users scan the navigation quickly.

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

See the [Navigation & Sidebar](./navigation) page for a list of commonly used icon names.

### `layout`

- **Type:** `string`
- **Default:** `"default"`

The layout template to use when rendering this page. MokaDocs ships with several built-in layouts:

| Layout | Description |
|---|---|
| `default` | Standard documentation page with sidebar, table of contents, and content area. |
| `wide` | Full-width content area without the table of contents sidebar. Useful for pages with wide tables or diagrams. |
| `landing` | A landing/home page layout without the sidebar. Designed for the site's index page. |
| `raw` | Minimal layout with no navigation chrome. Only the page content and basic site styles are applied. |
| `api` | Specialized layout for API reference pages. Automatically applied to generated API docs. |

```yaml
---
title: "Welcome to My Docs"
layout: "landing"
---
```

Custom themes and plugins can register additional layout names.

### `tags`

- **Type:** `list` of `string`
- **Default:** `[]`

A list of tags associated with this page. Tags are used for categorization and can improve search relevance. Some themes display tags on the page or provide tag-based filtering.

```yaml
---
title: "Working with Dependency Injection"
tags:
  - dependency-injection
  - advanced
  - patterns
---
```

Tags are normalized to lowercase and can contain letters, numbers, and hyphens.

### `visibility`

- **Type:** `string`
- **Default:** `"public"`

Controls the visibility and discoverability of the page. Three modes are available:

#### `public`

The page is fully visible. It appears in the sidebar navigation, is indexed by search, and is included in the sitemap.

```yaml
---
title: "Getting Started"
visibility: "public"
---
```

#### `hidden`

The page is built and accessible by its URL, but it does not appear in the sidebar navigation or the sitemap. It is still indexed by search unless the page also opts out of search. This is useful for pages you want to link to directly without cluttering the sidebar.

```yaml
---
title: "Legacy Migration Notes"
visibility: "hidden"
---
```

**Use cases for hidden pages:**

- Supplementary content linked from other pages
- Legacy pages that should remain accessible but not prominently listed
- Special pages (e.g., a custom 404 page)
- Content that is contextually linked but does not belong in navigation

#### `draft`

The page is excluded from the build entirely unless MokaDocs is run with the `--draft` flag. Draft pages do not appear in navigation, search, or the sitemap during normal builds. This is ideal for work-in-progress content.

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
| Accessible by URL | Yes | Yes | Only with `--draft` |
| Included in search index | Yes | Yes | No |
| Included in sitemap | Yes | No | No |
| Built by default | Yes | Yes | No |

### `toc`

- **Type:** `bool`
- **Default:** `true`

Controls whether the table of contents sidebar is displayed on this page. The table of contents is automatically generated from the H2 and H3 headings in the page content.

```yaml
---
title: "Changelog"
toc: false
---
```

Set to `false` for pages that have few or no headings, or for pages where the right-side table of contents would be distracting (e.g., changelog pages, landing pages).

### `expanded`

- **Type:** `bool`
- **Default:** `true`

Controls whether this section is expanded or collapsed by default in the sidebar navigation. This only applies to section index pages (`index.md` files within a directory) that serve as parents for other pages.

```yaml
---
title: "Advanced Topics"
expanded: false
---
```

When set to `false`, the section appears collapsed in the sidebar and the user must click to expand it. This is useful for sections that contain many pages and would otherwise make the sidebar too long.

### `route`

- **Type:** `string` (nullable)
- **Default:** `null` (auto-generated from file path)

Overrides the URL path for this page. By default, routes are generated from the file path relative to the docs directory (see the [Navigation & Sidebar](./navigation) page for route generation rules). Setting `route` allows you to define a custom URL.

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
| `docs/reference/api-v2.md` | `/reference/api-v2` | `/api` |
| `docs/about/team.md` | `/about/team` | `/team` |

Custom routes must:
- Start with a forward slash (`/`)
- Contain only lowercase letters, numbers, hyphens, and forward slashes
- Not conflict with another page's route (conflicts produce a build error)

### `version`

- **Type:** `string` (nullable)
- **Default:** `null`

A version range constraint that limits which documentation versions this page appears in. This is only relevant when versioning is enabled in the site configuration. The value uses semver-style range syntax.

```yaml
---
title: "New Authentication API"
version: ">=2.0"
---
```

**Supported range expressions:**

| Expression | Meaning |
|---|---|
| `">=2.0"` | Included in version 2.0 and above |
| `"<3.0"` | Included in versions before 3.0 |
| `">=1.5 <2.0"` | Included in versions 1.5 through 1.x |
| `"2.0"` | Included only in version 2.0 |

Pages without a `version` constraint appear in all versions.

### `requires`

- **Type:** `string` (nullable)
- **Default:** `null`

Use `requires` to conditionally include a page based on a named feature. If the feature is disabled, the page is excluded from the build entirely -- it will not appear in navigation, search, or the sitemap.

```yaml
---
title: "Cloud Deployment Guide"
requires: Cloud
---
```

```yaml
---
title: "Interactive REPL Tutorial"
requires: Repl
---
```

If the `requires` field is omitted or set to `null`, the page is always included (subject to other visibility rules).

**Behavior summary:**

| Feature State | Page Included? |
|---|---|
| Enabled | Yes |
| Disabled | No -- page is excluded from the build |
| Feature not recognized | Page is excluded (treated as disabled) |

---

## Complete Example

A page using all available front matter properties:

```yaml
---
title: "Dependency Injection Guide"
description: "Learn how to configure and use dependency injection with the Contoso SDK."
order: 5
icon: "syringe"
layout: "default"
tags:
  - dependency-injection
  - configuration
  - advanced
visibility: "public"
toc: true
expanded: true
route: "/guides/di"
version: ">=2.0"
requires: Cloud
---

# Dependency Injection Guide

This guide covers how to configure dependency injection...
```

## Front Matter Defaults

When front matter properties are omitted, the following defaults apply:

| Property | Default Value |
|---|---|
| `title` | Derived from filename or first H1 heading |
| `description` | `""` |
| `order` | `0` |
| `icon` | `null` (no icon) |
| `layout` | `"default"` |
| `tags` | `[]` (no tags) |
| `visibility` | `"public"` |
| `toc` | `true` |
| `expanded` | `true` |
| `route` | Auto-generated from file path |
| `version` | `null` (all versions) |
| `requires` | `null` (no feature gate) |

## Tips

- Always provide a `title` explicitly. Relying on filename-derived titles can produce unexpected results with abbreviations or unconventional filenames.
- Use `description` on every page. Search results and social sharing previews look much better with a well-written description.
- Use `order` consistently within a section. If you order some pages but not others, the unordered pages may appear in unexpected positions.
- Prefer `visibility: "hidden"` over deleting pages when you want to remove something from the sidebar but keep the URL working (to avoid broken links).
- Use `visibility: "draft"` for work in progress rather than keeping draft files outside the docs directory.
