---
title: Search
order: 5
---

# Search

MokaDocs includes a built-in client-side search system that is enabled by default. Search allows users to quickly find content across all documentation pages and API reference entries without requiring a server-side search backend.

## How It Works

During the build process, MokaDocs generates a `search-index.json` file that contains all searchable content from your documentation site. This index is loaded in the browser and searched entirely on the client side, providing instant results with no network latency.

### What Gets Indexed

The search index is built from two sources:

| Source              | Index Entries Created                              |
|---------------------|----------------------------------------------------|
| Documentation pages | One entry per page, plus one entry per heading      |
| API reference pages | One entry per documented type and member            |

For each documentation page, the indexer extracts:

- **Page title** from the YAML front matter or first heading
- **Section headings** (each heading becomes a separately searchable entry with a deep link)
- **Plain text content** with all Markdown formatting, HTML tags, and code block syntax stripped

This means users can search for content within a specific section of a page and jump directly to that section from the search results.

### Search Categories

Search results are organized into two categories:

| Category          | Content                                           |
|-------------------|----------------------------------------------------|
| Documentation     | All Markdown documentation pages and their sections |
| API Reference     | Generated API types, members, and namespaces       |

Categories are displayed as grouped sections in the search results panel, making it easy to distinguish between narrative documentation and API reference entries.

## Using Search

### Keyboard Shortcut

Press **Cmd+K** (macOS) or **Ctrl+K** (Windows/Linux) from any page to open the search dialog. This shortcut works globally, regardless of where focus is on the page.

### Search UI Features

The search dialog provides several features for a fast and fluid experience:

- **Instant results** — Results appear as you type with no submit button required
- **Keyboard navigation** — Use arrow keys to move through results and Enter to select
- **Deep linking** — Results that match a heading link directly to that section of the page
- **Category grouping** — Results are grouped by Documentation and API Reference
- **Result highlighting** — Matched terms are highlighted in the result titles and excerpts
- **Escape to close** — Press Escape or click outside the dialog to dismiss it

### Tags

Pages can include `tags` in their YAML front matter to improve searchability:

```yaml
---
title: Configuration
tags:
  - setup
  - yaml
  - settings
---
```

Tags provide additional keywords that help the search engine match pages even when the search query does not appear in the page title or body text.

## Configuration

### Enabling and Disabling Search

Search is enabled by default. To disable it entirely:

```yaml
features:
  search:
    enabled: false
```

When disabled, the search button and keyboard shortcut are removed from the site.

### Search Provider

MokaDocs supports multiple search provider implementations. Configure the provider in your `mokadocs.yaml`:

```yaml
features:
  search:
    provider: flexsearch
```

Available providers:

| Provider     | Description                                                  |
|--------------|--------------------------------------------------------------|
| `flexsearch` | Default provider. Lightweight, fast, and works entirely client-side. Good for most documentation sites. |
| `pagefind`   | A more advanced search engine that builds a compressed index at build time. Better for very large sites with thousands of pages. |

### FlexSearch Provider

FlexSearch is the default provider and requires no additional setup. It loads the full search index into memory on the client and provides near-instant results.

Best for:
- Small to medium documentation sites (up to several hundred pages)
- Sites where simplicity is preferred
- Offline-capable documentation

### Pagefind Provider

Pagefind generates a highly optimized, compressed search index during the build step. Only relevant index fragments are loaded on demand, making it efficient for large sites.

Best for:
- Large documentation sites with many pages
- Sites where initial page load size is a concern
- Projects that need more advanced search features like content weighting

```yaml
features:
  search:
    provider: pagefind
```

::: note
When using the Pagefind provider, the search index is generated as part of the build process. The index files are placed in the output directory alongside the rest of the static site.
:::

## Index Structure

The generated `search-index.json` file contains an array of search entries. Each entry includes:

| Field      | Description                                              |
|------------|----------------------------------------------------------|
| `title`    | The page title or section heading                        |
| `url`      | The URL path to the page, with anchor for section entries |
| `content`  | Plain text excerpt of the page or section content        |
| `category` | Either "Documentation" or "API Reference"                |
| `tags`     | Array of tags from the page front matter (if any)        |

::: note Compact Field Names
The actual `search-index.json` file uses abbreviated field names for a smaller payload: `t` (title), `s` (section/url), `r` (route), `c` (content), and `g` (tags/category). The table above shows the logical field names for clarity.
:::

### Page-Level vs Section-Level Entries

For a documentation page with the following structure:

```markdown
---
title: Getting Started
tags:
  - quickstart
---

# Getting Started

Introduction paragraph...

## Installation

Installation instructions...

## Configuration

Configuration details...
```

The search index will contain three entries:

1. **Page entry** — title: "Getting Started", url: `/guide/getting-started`
2. **Section entry** — title: "Installation", url: `/guide/getting-started#installation`
3. **Section entry** — title: "Configuration", url: `/guide/getting-started#configuration`

This granularity ensures that users searching for "installation" are taken directly to the relevant section rather than just the top of the page.

## Performance Considerations

### Index Size

The search index size depends on the amount of content in your documentation. For typical documentation sites:

| Site Size         | Approximate Index Size |
|-------------------|----------------------|
| Small (< 50 pages) | Under 100 KB         |
| Medium (50-200 pages) | 100-500 KB        |
| Large (200+ pages) | 500 KB+              |

The index is loaded once when the user first opens the search dialog and cached for the duration of the session.

### Optimizing Search Quality

To get the best search results:

1. **Use descriptive page titles** — The title field carries the most weight in search ranking
2. **Write clear section headings** — Each heading becomes a searchable entry
3. **Add relevant tags** — Tags help surface pages for queries that use different terminology
4. **Keep content focused** — Pages that cover a single topic rank better than pages that cover many unrelated topics
5. **Use consistent terminology** — Consistent naming across your documentation helps users find related content
