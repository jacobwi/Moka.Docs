---
title: Search
order: 5
---

# Search

MokaDocs builds a search index along with the site and runs queries against it in the browser, so search needs no server or external service. It is on by default.

## How It Works

The build writes `search-index.json` to the root of the output folder. The first time a reader opens search on a page, the theme downloads that file and matches queries against it in the browser. The file is not kept between page loads: the next page fetches it again, normally from the browser cache.

### What Gets Indexed

| Source              | Index entries |
|---------------------|---------------|
| Markdown pages      | One entry for the page, plus one for each heading, the page's `#` heading included |
| Generated API pages | One entry for each page (the API index and each type page), plus one for each section heading on a type page, such as Remarks, Constructors and Methods |

Members such as methods and properties have no entries of their own, and neither do namespaces.

A **page entry** holds:

- **Title**: the front matter `title`. The first heading is not used as a fallback, so a page without a title is indexed as "Untitled".
- **Content**: the first 300 characters of the page's text. The text comes from paragraphs and headings, inline code and link text included; code blocks are left out. For an API type page, the content is the type's XML `<summary>`.
- **Tags**: the front matter `tags`.

A **heading entry** holds the page title, the heading text and a link to the heading. It has no content and no tags, so it matches on the heading text and the page title only.

Words after the first 300 characters of a page are not in the index. Readers can still find that part of the page through its headings.

Draft pages are indexed only when you build with `--draft`. Pages with `visibility: hidden` stay out of the navigation but are still indexed.

### Search Categories

Every entry has one of two categories:

| Category      | Entries |
|---------------|---------|
| Documentation | Markdown pages and their headings |
| API Reference | Generated API pages and their headings |

Each result shows its category as a label. Results are a single list sorted by score; they are not grouped by category.

## Using Search

### Opening Search

Click **Search** in the header or press **Ctrl+K** (**Cmd+K** on macOS). The same shortcut closes the dialog.

### Results

- Results update as you type.
- **Up** and **Down** move through the results, and **Enter** opens the selected one.
- **Escape**, or a click outside the dialog, closes it.
- The list shows the 20 best matches.

A result shows the page title, the heading (for heading entries) and the category. A page entry also shows an excerpt of its content with the query words highlighted. Titles and headings are not highlighted, and tags are not shown.

### How Matching Works

Every word in the query has to appear in the entry's title, heading, content or tags. Matching is a case-insensitive substring test, so `config` matches "Configuration". There is no stemming or typo tolerance, and accents count: `cafe` does not match "Café".

Each word adds points for every field it appears in, and results are sorted by the total:

| Field   | Points |
|---------|--------|
| Title   | 10 |
| Heading | 5 |
| Tags    | 3 |
| Content | 1 |

### Tags

Front matter `tags` add keywords to a page's entry:

```yaml
---
title: Configuration
tags:
  - setup
  - yaml
  - settings
---
```

A query word found in the tags adds 3 points, so tags help a page turn up for words that are not in its title or text. Tags are only on the page entry, not on its heading entries. The default theme does not display them.

Write tags as a YAML list. A plain string such as `tags: setup, yaml` stops the whole front matter block from parsing. The build warns about the file, and the page is indexed as "Untitled" with no tags.

## Configuration

### Enabling and Disabling Search

Search is on by default. To turn it off:

```yaml
features:
  search:
    enabled: false
```

With search off, the build writes no `search-index.json`, and pages have no Search button, search dialog or keyboard shortcut.

To remove search from the pages but keep writing the index, set `showSearch` instead:

```yaml
theme:
  options:
    showSearch: false
```

There is one search implementation, the one described on this page. A `features.search.provider` value is accepted in `mokadocs.yaml` but has no effect.

## Index Structure

`search-index.json` is a JSON array with one object per entry. Field names are single letters to keep the file small:

| Field | Meaning  | Page entry | Heading entry |
|-------|----------|------------|---------------|
| `t`   | Title    | Page title | Page title |
| `s`   | Section  | Empty string | Heading text |
| `r`   | Route    | Page URL | Page URL plus `#` and the heading id |
| `c`   | Content  | First 300 characters of the page text | Empty string |
| `g`   | Category | `Documentation` or `API Reference` | Same as its page |
| `k`   | Tags     | Tags separated by spaces; left out when the page has none | Left out |

URLs in `r` include `build.basePath` when the site sets one.

### Page-Level vs Section-Level Entries

For `docs/guide/getting-started.md` with this content:

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

the index gets four entries:

1. **Page entry**: title "Getting Started", URL `/guide/getting-started`, tags `quickstart`
2. **Heading entry**: "Getting Started", URL `/guide/getting-started#getting-started`
3. **Heading entry**: "Installation", URL `/guide/getting-started#installation`
4. **Heading entry**: "Configuration", URL `/guide/getting-started#configuration`

A search for "installation" scores 5 on the Installation heading entry and 1 on the page entry (the word is in its content). The heading entry ranks higher, and it links straight to that section.

## Performance Considerations

### Index Size

Each page adds one entry with at most 300 characters of text and one short entry per heading, so a page with many headings adds more to the file than a long page with few. `mokadocs build` prints the entry count in its summary. The whole file is downloaded before the first query on a page.

### Optimizing Search Quality

1. **Put the words readers search for in titles and headings.** A match in the title scores 10 and a match in a heading scores 5.
2. **Add tags for other words readers might use.** A tag match scores 3.
3. **Put key terms near the top of the page.** Only the first 300 characters of the page text are indexed.
