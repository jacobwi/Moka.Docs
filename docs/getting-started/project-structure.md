---
title: Project Structure
description: Understanding the MokaDocs project layout and file conventions
order: 3
---

# Project Structure

This page covers where MokaDocs looks for files and how they turn into URLs.

## Directory Layout

```
my-project/
├── mokadocs.yaml          # Required - site configuration
├── docs/                  # Markdown documentation
│   ├── index.md           # Site home page
│   ├── getting-started.md # Top-level guide page
│   └── guide/             # Nested section
│       ├── index.md       # Section index (optional)
│       ├── basics.md      # Guide page
│       └── advanced.md    # Guide page
├── src/
│   └── MyLib/
│       └── MyLib.csproj   # .NET project for API docs
├── .mokadocs/             # API analysis cache
└── _site/                 # Generated output
```

## Configuration File

Every command that reads the config accepts `-c` or `--config`. Without it, MokaDocs uses `mokadocs.yaml` in the current directory, or `mokadocs.yml` when only that file exists:

```bash
mokadocs build --config ./path/to/mokadocs.yaml
```

Relative paths in the config resolve against the folder that contains the config file, not the current directory. So does a relative `-o`/`--output` value.

## Documentation Directory

The `docs/` directory (set with `content.docs`) holds your Markdown files. A file's path, without the `.md` extension, becomes its URL, and `index.md` takes the URL of its folder:

| File Path | URL Route |
|-----------|-----------|
| `docs/index.md` | `/` |
| `docs/getting-started.md` | `/getting-started` |
| `docs/guide/basics.md` | `/guide/basics` |
| `docs/guide/index.md` | `/guide` |
| `docs/guide/setup/windows.md` | `/guide/setup/windows` |

Each page is written to `<route>/index.html` in the output folder.

## Front Matter

Every Markdown file should start with YAML front matter:

```yaml
---
title: Page Title        # Sidebar label and <title>
description: Summary     # Meta description; falls back to site.description
order: 1                 # Sidebar sort order, lowest first, then by title
icon: rocket             # Lucide icon name shown in the sidebar
layout: default          # default or landing in the built-in theme
tags: [guide, basics]    # Matched by search
visibility: public       # public, hidden, or draft
toc: true                # Show the table of contents (default: true)
expanded: true           # Section index only: start the section expanded (default: true)
route: /custom-url       # Replaces the file-based URL
---
```

How the fields behave:

- A page without a `title` is titled "Untitled".
- `tags` feed search. The default theme doesn't display them.
- `hidden` pages are built and show up in search, but not in the generated sidebar. `draft` pages are skipped unless you pass `--draft` to `build` or `serve`, and the generated sidebar leaves them out even then.
- In the generated sidebar, a section shows the `icon` from its `index.md`, and pages nested in a section show their own.
- `expanded` is read from a section's `index.md`. A section that contains the current page always opens. When `mokadocs.yaml` has a `nav:` list, the `expanded` value on each nav item applies instead.
- `route` gets a leading slash when it has none and loses a trailing one. When two pages end up with the same route, the build warns and only the last one is written.
- `version` is parsed but has no effect. See [Versioning](/advanced/versioning).

See [Front Matter](/configuration/front-matter) for the full reference.

## Section Indexes

An `index.md` in a folder becomes the page at that folder's URL. For a folder without one, the build writes a redirect page that sends visitors to the alphabetically first subfolder that has an index page. Every page is written as `<name>/index.html`, so this is the page whose URL segment sorts first. `order` plays no part.

For example, if `docs/guide/` contains `zeta.md` with `order: 1` and `alpha.md` with `order: 2` and no `index.md`, `/guide/` redirects to `/guide/alpha/`.

## Static Assets

The build copies files from the docs folder to the output, at the same relative path, when their extension is on this list:

| Kind | Extensions |
|------|------------|
| Images | `.png` `.jpg` `.jpeg` `.gif` `.svg` `.webp` `.avif` `.ico` |
| Documents | `.pdf` `.zip` `.txt` |
| Video | `.mp4` `.webm` |
| Web files | `.css` `.js` `.json` `.xml` `.webmanifest` |
| Fonts | `.woff` `.woff2` `.ttf` `.eot` |

`CNAME`, `_redirects` and `_headers` are copied too when they sit directly in the docs folder. Other files are skipped without a warning, and nothing inside the output folder is copied.

Reference assets with paths relative to the Markdown file:

```markdown
![Architecture Diagram](./images/architecture.png)
```

The build resolves relative links and images against the source file, which gives the same target an editor or GitHub shows. From `docs/guide/basics.md`, the image above resolves to `/guide/images/architecture.png`. Links to other Markdown files work the same way: `[Setup](../getting-started.md)` becomes `/getting-started`. Links inside raw HTML tags aren't rewritten.

## Output Directory

The `_site/` directory (set with `build.output`, or `-o` for a single run) contains the generated site. With `build.clean: true`, the default, the build deletes this folder before writing. The build refuses an output folder that contains the project folder or the docs folder.

Add the generated folders to `.gitignore`:

```
# .gitignore
_site/
.mokadocs/
```

`.mokadocs/` holds the cached API analysis. `mokadocs clean` deletes both folders.

## API Projects

For each project listed under `content.projects`, the CLI parses every `.cs` file in the project's folder and its subfolders (skipping `bin` and `obj`) with Roslyn and reads the `///` doc comments from source. It doesn't run MSBuild or need a compiled assembly. The result is cached in `.mokadocs/` until a `.cs` file changes; `mokadocs build --no-cache` skips the cache.

API pages always go under `/api`, and the prefix can't be changed. `/api` lists every type, and each type gets a lowercase route such as `/api/mylib/widget` for `MyLib.Widget`. A Markdown page with the same route as an API page triggers the duplicate route warning.
