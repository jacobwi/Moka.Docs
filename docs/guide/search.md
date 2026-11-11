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
| Generated API pages | One entry for each page (the API index and each type page), plus one for each member on a type page, such as a method or property, linked to its description |

Namespaces have no entries of their own. Pages that plugins add, such as REST API pages, are indexed like Markdown pages.

A **page entry** holds:

- **Title**: the front matter `title`. The first heading is not used as a fallback, so a page without a title is indexed as "Untitled".
- **Content**: the first 300 characters of the page's text. The text comes from paragraphs and headings, inline code and link text included; code blocks are left out. For an API type page, the content is the type's XML `<summary>`.
- **Code**: the words of the code blocks above the page's first heading, if any (see below).
- **Tags**: the front matter `tags`.

A **heading entry** holds the page title, the heading text, a link to the heading and:

- **Content**: the first 300 characters of the text between this heading and the next one, at any level. The text of tables, lists, callouts and tabs counts; code blocks and diagrams don't.
- **Code**: the words of the code blocks between this heading and the next one.

A member entry holds the page title, the member's name and parameters as the heading, and its XML `<summary>` as content.

Text more than 300 characters into a section is not in the index. Put the words readers search for near the start of the section, or give that part its own heading.

### Code Blocks

Code blocks are indexed as a list of their words, so an identifier that only appears in a sample, such as `AddMokaDocs` or `--base-path`, still finds its section. A word is a run of letters, digits, `_`, `.` and `-`, so `context.FileSystem` stays one word, and a query for `FileSystem` finds it because matching looks for the query inside each word. Each word is listed once, a word that another listed word contains is left out, words without a letter (numbers, `---`) are skipped, and the list stops at 300 characters for each entry. Mermaid diagrams are not indexed.

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

A result shows the page title, the heading (for heading entries) and the category, and an excerpt of its content with the query words highlighted. Titles and headings are not highlighted, and tags and code words are not shown.

### How Matching Works

Every word in the query has to appear in the entry's title, heading, content, code words or tags. Matching is a case-insensitive substring test, so `config` matches "Configuration". There is no stemming or typo tolerance, and accents count: `cafe` does not match "Café".

Each word adds points for every field it appears in, and results are sorted by the total:

| Field   | Points |
|---------|--------|
| Title   | 10 |
| Heading | 5 |
| Tags    | 3 |
| Content or code words | 1 |

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

There is one search implementation, the one described on this page. Earlier versions accepted a `features.search.provider` value that did nothing; it is no longer read, and the build warns when `mokadocs.yaml` still sets it.

## Index Structure

`search-index.json` is a JSON array with one object per entry. Field names are single letters to keep the file small:

| Field | Meaning  | Page entry | Heading entry |
|-------|----------|------------|---------------|
| `t`   | Title    | Page title | Page title |
| `s`   | Section  | Empty string | Heading text |
| `r`   | Route    | Page URL | Page URL plus `#` and the heading id |
| `c`   | Content  | First 300 characters of the page text | First 300 characters of the section's text |
| `x`   | Code     | Code words from above the first heading; left out when there are none | Code words from the section; left out when there are none |
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
2. **Heading entry**: "Getting Started", URL `/guide/getting-started#getting-started`, content "Introduction paragraph..."
3. **Heading entry**: "Installation", URL `/guide/getting-started#installation`, content "Installation instructions..."
4. **Heading entry**: "Configuration", URL `/guide/getting-started#configuration`, content "Configuration details..."

A search for "installation" scores 6 on the Installation heading entry (5 for the heading, 1 for its content) and 1 on the page entry (the word is in its content). The heading entry ranks higher, and it links straight to that section.

## Performance Considerations

### Index Size

Each page entry and each heading entry holds at most 300 characters of text and 300 characters of code words, so the file grows with the number of headings as well as pages. On this repository's own documentation, giving heading entries their text and code words took the file from 296 KB to 477 KB (50 KB to 110 KB gzipped). `mokadocs build` prints the entry count in its summary. The whole file is downloaded before the first query on a page.

### Optimizing Search Quality

1. **Put the words readers search for in titles and headings.** A match in the title scores 10 and a match in a heading scores 5.
2. **Add tags for other words readers might use.** A tag match scores 3.
3. **Put key terms near the top of the page and of each section.** Only the first 300 characters of the page text and of each section's text are indexed.
