---
title: Project Structure
description: Understanding the MokaDocs project layout and file conventions
order: 3
---

# Project Structure

MokaDocs follows conventions to keep your documentation organized. Understanding these conventions helps you structure your docs effectively.

## Directory Layout

```
my-project/
├── mokadocs.yaml          # Required — site configuration
├── docs/                  # Markdown documentation
│   ├── index.md           # Site landing page
│   ├── getting-started.md # Top-level guide page
│   └── guide/             # Nested section
│       ├── index.md       # Section index (optional)
│       ├── basics.md      # Guide page
│       └── advanced.md    # Guide page
├── src/
│   └── MyLib/
│       └── MyLib.csproj   # .NET project for API docs
└── _site/                 # Generated output
```

## Configuration File

The `mokadocs.yaml` file is the entry point. MokaDocs looks for it in the current working directory by default, or you can specify a path with `--config`:

```bash
mokadocs build --config ./path/to/mokadocs.yaml
```

All paths in the config are relative to the config file's location.

## Documentation Directory

The `docs/` directory (configurable via `content.docs`) contains your Markdown files. The directory structure maps directly to URL routes:

| File Path | URL Route |
|-----------|-----------|
| `docs/index.md` | `/` |
| `docs/getting-started.md` | `/getting-started` |
| `docs/guide/basics.md` | `/guide/basics` |
| `docs/guide/index.md` | `/guide` |
| `docs/api/overview.md` | `/api/overview` |

## Front Matter

Every Markdown file should include YAML front matter:

```yaml
---
title: Page Title        # Required — used in nav and <title>
description: Summary     # Optional — meta description
order: 1                 # Optional — sort order in sidebar
icon: rocket             # Optional — sidebar icon
layout: default          # Optional — template to use
tags: [guide, basics]    # Optional — for search
visibility: public       # Optional — public, hidden, or draft
toc: true                # Optional — show table of contents
expanded: true           # Optional — expand section in sidebar
route: /custom-url       # Optional — override URL
---
```

## Section Indexes

Create an `index.md` in a directory to define that section's landing page. Without an `index.md`, MokaDocs auto-generates a redirect to the first child page.

## Static Assets

Images and other static files placed in the `docs/` directory are copied to the output as-is. Reference them with relative paths in your Markdown:

```markdown
![Architecture Diagram](./images/architecture.png)
```

## Output Directory

The `_site/` directory (configurable via `build.output`) contains the generated static site. This directory should be added to `.gitignore`:

```
# .gitignore
_site/
```

## API Projects

C# projects listed under `content.projects` are analyzed using Roslyn to extract type information and XML documentation. The generated API reference pages appear under the `/api` route by default.
