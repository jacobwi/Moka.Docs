---
title: CLI Commands
order: 1
---

# CLI Commands

MokaDocs provides a command-line interface for scaffolding, building, serving, and managing your documentation site. All commands are invoked via the `mokadocs` executable.

## Global Usage

```bash
mokadocs <command> [options]
```

Run `mokadocs --help` to see all available commands, or `mokadocs <command> --help` for details on a specific command.

Commands that read a project look for the configuration file in this order: the `--config` path when given, otherwise `mokadocs.yaml` in the current directory, otherwise `mokadocs.yml`. Paths inside the file resolve relative to the file itself, not the directory you run the command from.

---

## mokadocs init

Scaffolds a new MokaDocs project in the current directory: a starter configuration file and a docs folder.

### What It Creates

- `mokadocs.yaml` - site configuration with common settings pre-filled
- `docs/index.md` - a starter home page

### Options

`init` takes no arguments or options. It always works in the current directory.

### Usage

```bash
mkdir my-library-docs
cd my-library-docs
mokadocs init
```

### Behavior

- If `mokadocs.yaml` already exists in the current directory, `init` prints a message and changes nothing.
- The generated `docs/index.md` has `title`, `description`, `order` and `layout` in its front matter.

---

## mokadocs build

Runs the full build pipeline: parses Markdown, analyzes the configured C# projects, generates API pages, builds the search index, and writes the static site to the output directory.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--output <path>` | `-o` | Output directory, overriding `build.output` | Value from config |
| `--watch` | | Rebuild when files in the docs folder or the configuration file change | Off |
| `--verbose` | `-v` | Print pipeline logging and every warning | Off |
| `--draft` | | Include pages that have `visibility: draft` in their front matter | Off |
| `--base-path <path>` | | Path prefix for subdirectory deployments (e.g. `/repo-name` for GitHub Pages) | `build.basePath` |
| `--no-cache` | | Analyze C# projects again instead of using `.mokadocs/cache` | Off |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The build completed without errors |
| 1 | The build failed, or it reported one or more errors |

Errors include problems reported by plugins, such as a Blazor preview block with no preview host. They are always printed; warnings are counted, and listed with `--verbose`.

### Build Output Summary

```
MokaDocs v1.6.0 - Building documentation site...

✓ Config: mokadocs.yaml - "My Library"

✅ MokaDocs build complete in 3.42s
📄 Pages:        182 (26 markdown, 156 generated)
🔧 API Types:    155 across 10 assemblies
🔍 Search Index: 1145 entries
📦 Output:       ./_site
```

When the build reports problems, a `Diagnostics` line follows with the warning and error counts.

### Usage Examples

```bash
# Basic build with default settings
mokadocs build

# Build with a custom config file
mokadocs build -c ./config/mokadocs.prod.yaml

# Build to a custom output directory
mokadocs build -o ./dist

# Build including draft pages
mokadocs build --draft

# Watch mode: rebuilds automatically when files change
mokadocs build --watch

# Re-analyze all C# projects, with pipeline logging
mokadocs build --no-cache -v

# Build for GitHub Pages subdirectory deployment
mokadocs build --base-path /my-repo
```

### Notes

- `--watch` monitors the docs folder and the configuration file. A change to the configuration file triggers a rebuild with the settings loaded at startup, so restart the command to apply configuration changes. Press `Ctrl+C` to stop.
- The C# analysis cache is keyed by the project's source files and by the MokaDocs build that wrote it, so editing code or upgrading MokaDocs invalidates it on its own. `--no-cache` is only needed to rule the cache out while troubleshooting.
- The output directory must not be the project folder, the docs folder, or a folder that contains either. The build stops with an error instead of deleting your sources.
- Draft pages are left out of builds unless `--draft` is passed.

---

## mokadocs serve

Builds the site and serves it locally, rebuilding and reloading the browser when files change.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--port <number>` | `-p` | Port for the development server | `5080` |
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--output <path>` | `-o` | Output directory | Value from config |
| `--open` | | Open the site in the default browser when the server starts | On |
| `--no-open` | | Do not open the browser | Off |
| `--draft` | | Include pages that have `visibility: draft` in their front matter | Off |
| `--base-path <path>` | | Path prefix the site is served under, matching `build.basePath` | Value from config |
| `--verbose` | `-v` | Enable pipeline logging | Off |

### Features

- **Hot reload**: the server injects a small WebSocket client into served pages and tells the browser to reload after each rebuild.
- **File watching**: watches the docs folder and the configuration file, ignoring the output folder. Configuration changes are rebuilt with the settings loaded at startup; restart `serve` to apply them.
- **Diagnostics**: after every build the terminal shows the warning and error counts and lists the errors, like `build`. `--verbose` lists the warnings too.
- **Clean URLs**: a request for `/guide` is served from `/guide/index.html`.
- **404 page**: unknown paths get the site's generated `404.html`.
- **Base path aware**: with `build.basePath` or `--base-path`, the server strips that prefix from requests, so `http://localhost:5080/my-repo/` serves the same pages the deployed site will.
- **REPL endpoint**: `POST /api/repl/execute` exists only when the `mokadocs-repl` plugin is declared. It runs the posted C# code inside the server process with your permissions.
- **Blazor preview endpoint**: `POST /api/blazor/preview` exists only when the `mokadocs-blazor-preview` plugin is declared.

The API endpoints accept only JSON requests, and a browser request must come from the dev server's own origin (`http://localhost:<port>`). Requests from other sites get `403`, so a web page open in the same browser cannot run code through the REPL endpoint.

### Usage Examples

```bash
# Start the dev server on the default port (5080)
mokadocs serve

# Start on a custom port without opening the browser
mokadocs serve -p 3000 --no-open

# Start with a specific config file and pipeline logging
mokadocs serve -c mokadocs.prod.yaml -v
```

### Output

```
MokaDocs v1.6.0 - Dev server starting...

Config: mokadocs.yaml - "My Library"
Build complete in 1.21s

Dev server running: http://localhost:5080/
Press Ctrl+C to stop
```

---

## mokadocs new

Scaffolds new pages, plugins, or component examples. This command has three subcommands: `page`, `plugin`, and `component`.

### mokadocs new page

Creates a new Markdown page with front matter.

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--title <text>` | `-t` | The page title used in front matter | Derived from `<name>` |
| `--path <dir>` | `-p` | Directory where the page file is created, relative to the current directory | `./docs` |
| `--layout <name>` | | Layout to set in front matter: `default` or `landing` | `default` |
| `--order <number>` | | Sort order value to set in front matter | None |

The name can include folders (`guides/overview` creates `guides/overview.md`); the default title comes from the last part. Titles that YAML would misread, such as `Api: v2`, are written quoted. An existing file is never overwritten.

#### Usage

```bash
# Create a basic page in the docs directory
mokadocs new page getting-started

# Create a page with a custom title and order
mokadocs new page installation -t "Installation Guide" --order 1

# Create a landing page in a subdirectory
mokadocs new page overview -p ./docs/guides --layout landing
```

### mokadocs new plugin

Scaffolds a plugin project: a `.csproj` that references the `Moka.Docs.Plugins` package at the version of the tool you are running, and a class that implements `IMokaPlugin`.

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--path <dir>` | `-p` | Directory the project folder is created in | `.` |

#### Usage

```bash
mokadocs new plugin MyCustomPlugin
```

This creates `Moka.Docs.Plugins.MyCustomPlugin/` containing `Moka.Docs.Plugins.MyCustomPlugin.csproj` and `MyCustomPluginPlugin.cs`. The plugin id is `mokadocs-my-custom-plugin`; `MyCustomPlugin` and `my-custom-plugin` produce the same id.

The `mokadocs` CLI loads only its built-in plugins, so a plugin built from this template runs only in a host that registers it as an `IMokaPlugin` and declares its id under `plugins`. See [Plugin System](/plugins/overview).

### mokadocs new component

Generates an example page for a built-in component, named `<component>-example.md`.

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--path <dir>` | `-p` | Directory where the page is created | `./docs` |

#### Available Components

| Component | Description |
|-----------|-------------|
| `card` | Card layout component |
| `steps` | Step-by-step instruction component |
| `link-cards` | Grid of linked cards |
| `code-group` | Tabbed code blocks for multiple languages |
| `changelog` | Release notes timeline |

#### Usage

```bash
# Generate a card component example page
mokadocs new component card

# Generate a code-group example in another folder
mokadocs new component code-group -p ./docs/examples
```

---

## mokadocs doctor

Runs diagnostic checks against your MokaDocs project and reports problems before they reach production: configuration mistakes, broken links, missing titles, unused images, misspelled plugins and undocumented API.

Checks that depend on what the build produces run against a dry-run build (see [`mokadocs validate`](#mokadocs-validate)), so they see the same routes, plugins and API model a real build would. Nothing is written to your output directory.

### Checks

| Check | What It Validates |
|-------|-------------------|
| Configuration | `mokadocs.yaml` exists and parses |
| .NET SDK | The .NET SDK is on `PATH` |
| Projects | Every `content.projects` path points at a real `.csproj`. A site with no projects is valid and passes. |
| Docs Folder | The `content.docs` directory exists |
| Logo, Favicon | `site.logo` and `site.favicon` resolve to real files. Only shown when set. |
| Build | A dry-run build completes. Warnings and errors it reports are counted here; run `mokadocs validate` to see them. |
| Broken Links | Links and images point at something the build produces |
| Front Matter | Every page has a non-empty `title` |
| Orphan Images | Images in the docs folder that nothing references |
| Plugins | Every `plugins[].name` matches a built-in plugin id and initializes. Entries that set `path` or have no name are reported, since nothing loads them. |
| Search | The search index builds with entries. Search turned off in config passes. |
| API Coverage | Share of public types and members with a summary. Only shown when projects are configured. |

### How Links Are Checked

A link counts as valid when it resolves to any of:

- a page, including API reference pages, plugin pages, and pages whose front matter sets `route:`
- a section directory such as `/guide`, which the build redirects to its first page
- a static asset in the docs folder, the logo or favicon, or a generated file like `/sitemap.xml`

Root-relative links (`/guide/intro`) are checked as written. Relative links (`./intro`, `../guide/intro.md`) are resolved against the file that contains them, the same way the build rewrites them. Links are read from the parsed Markdown, not with a text search, so examples inside code blocks and inline code are never reported. Links to a draft page are reported and labelled, since drafts are left out of production builds. External URLs are not checked.

### How API Coverage Is Measured

Coverage is read from the API model generated from your source code, the same one your API pages are built from. A type or member counts as documented when it has a `<summary>`, or when its comment is `<inheritdoc/>`, even if the inherited text lives in a framework type that MokaDocs cannot read. A delegate's `Invoke` shares the delegate's own documentation. Use `--verbose` to list what is missing.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--fix` | | Automatically fix problems where possible. Currently adds a missing front matter `title`, derived from the file name, or from the folder name for `index.md`. | Off |
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--verbose` | `-v` | Show more detail, such as which pages lack titles and which symbols lack summaries | Off |

`--fix` preserves line endings and any UTF-8 byte order mark, and inserts the title without touching the rest of your front matter.

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All checks passed |
| 1 | One or more warnings were found |
| 2 | One or more errors were found |

### Usage

```bash
# Run all diagnostics
mokadocs doctor

# Run diagnostics with automatic fixes
mokadocs doctor --fix

# Diagnose another project and list details
mokadocs doctor -c samples/my-lib/mokadocs.yaml -v
```

Output is colored using Spectre.Console, with green for passed checks, yellow for warnings, and red for errors.

---

## mokadocs stats

Displays statistics about your documentation project. The numbers come from a dry-run build, so they match what `mokadocs build` produces. Nothing is written to the output directory.

### Output

```
mokadocs stats - My Library

╭─────────────────────┬───────────────────────╮
│ Metric              │                 Value │
├─────────────────────┼───────────────────────┤
│ Markdown Pages      │                    10 │
│ Generated Pages     │                    13 │
│ Total Pages         │                    23 │
│ Word Count          │                 4,044 │
│ ─────────────────── │               ─────── │
│ API Types           │                    12 │
│ API Members         │                    36 │
│ XML Doc Coverage    │                  100% │
│ Namespaces          │                     1 │
│ ─────────────────── │               ─────── │
│ Plugins             │                     3 │
│ Search              │ Enabled (155 entries) │
│ Docs Files          │                    10 │
│ Docs Size           │               34.0 KB │
╰─────────────────────┴───────────────────────╯
```

| Metric | Description |
|--------|-------------|
| Markdown Pages | Pages built from Markdown files, drafts included |
| Generated Pages | Pages the build creates: API reference, plugin and index pages |
| Word Count | Words in the Markdown files the build read, front matter excluded |
| API Types, API Members, Namespaces | Counts from the API model. Only shown when the build produced one. |
| XML Doc Coverage | Share of types and members with a summary, measured as in `mokadocs doctor` |
| Plugins | Declared plugins that initialized. Shows "N of M loaded" when some did not. |
| Search | Whether search is enabled, and how many index entries the build produced |
| Docs Files, Docs Size | Markdown and asset files the build read from the docs folder. A built site inside the docs folder is not counted. |

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--json` | | Print the statistics as JSON instead of a table | Off |

The JSON keys are `Markdown Pages`, `Word Count`, `Generated Pages`, `Total Pages`, `API Types`, `API Members`, `Namespaces`, `XML Doc Coverage` (the four API keys only when there is an API model), `Plugins`, `Search Enabled`, `Search Entries`, `Docs File Count` and `Docs Size`.

### Usage

```bash
# Show project statistics
mokadocs stats

# Output as JSON for CI integration
mokadocs stats --json

# Use a specific config file
mokadocs stats -c mokadocs.prod.yaml
```

The command exits `1` when the configuration cannot be read or the build fails.

---

## mokadocs clean

Deletes the build output and the C# analysis cache.

### Behavior

- Reads `build.output` from the configuration file and deletes that directory.
- Deletes the `.mokadocs` cache folder next to the configuration file.
- Prints a message and succeeds when there is nothing to delete.
- Refuses to delete an output directory that is the project folder, the docs folder, or contains either.
- Fails without deleting anything when the configuration file is missing or cannot be parsed.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |

### Usage Examples

```bash
# Clean the output directory and the cache
mokadocs clean

# Clean and then rebuild from scratch
mokadocs clean && mokadocs build

# Clean the project described by another config file
mokadocs clean -c mokadocs.prod.yaml
```

---

## mokadocs info

Displays environment and project paths. This is helpful for debugging or when filing bug reports.

### Output

```
                         MokaDocs Info
╭───────────────────┬──────────────────────────────────────╮
│ Property          │ Value                                │
├───────────────────┼──────────────────────────────────────┤
│ Version           │ 1.6.0                                │
│ Runtime           │ .NET 10.0.12                         │
│ OS                │ Microsoft Windows 10.0.26200         │
│ Working Directory │ C:\src\my-library                    │
│ Config File       │ C:\src\my-library\mokadocs.yaml      │
│ Docs Directory    │ C:\src\my-library\docs               │
│ Output Directory  │ C:\src\my-library\_site              │
╰───────────────────┴──────────────────────────────────────╯
```

The docs and output rows come from the configuration file and are marked `(not found)` or `(not built yet)` when the directory does not exist. Without a configuration file, only the environment rows and the missing config path are shown.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |

### Usage

```bash
mokadocs info
```

---

## mokadocs validate

Runs the complete build as a dry run and reports every warning and error it produces. Markdown is parsed, C# projects are analyzed, plugins run and every page is rendered through the theme, but nothing is written to the output directory, and an existing site there is left untouched.

### `validate` or `doctor`?

They answer different questions and work well together.

- **`mokadocs doctor`** checks your project files: links, titles, images, plugin names, API coverage. It can fix some problems.
- **`mokadocs validate`** shows what the build itself reports: a page that failed to parse, a project that could not be analyzed, a plugin that reported an error or never loaded, pages that share a route, an output directory that would delete your sources. These only show up by running the build.

When `doctor`'s Build row reports warnings or errors, `validate` lists them.

### Output

```
mokadocs validate - dry run, nothing is written

  ✓ Config        mokadocs.yaml - "My Library"
  ✓ Build         dry run completed in 0.94s
                  23 pages (10 markdown, 13 generated)
                  12 API types
                  155 search index entries
                  plugins: mokadocs-repl, mokadocs-changelog

  ⚠ CSharpAnalysis: Project file not found: ./src/Old/Old.csproj
  ⚠ Plugins: Plugin 'repl' is declared but no plugin has that id

  Result: 0 error(s), 2 warning(s)
```

Each problem is prefixed with the build phase or plugin that reported it.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--draft` | | Include pages with `visibility: draft` | Off |
| `--verbose` | `-v` | Show pipeline logging and informational diagnostics | Off |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The build reported no warnings or errors |
| 1 | The build reported warnings |
| 2 | The build reported errors, failed outright, or the config could not be read |

### Usage

```bash
# Check that the site builds cleanly
mokadocs validate

# Include drafts and see the full pipeline log
mokadocs validate --draft -v

# Fail a CI job only on errors
mokadocs validate || [ $? -eq 1 ]
```

The C# analysis cache in `.mokadocs/cache/` is used and updated as in a normal build.

---

## Command Cheat Sheet

| Task | Command |
|------|---------|
| Start a new project | `mokadocs init` |
| Scaffold a new page | `mokadocs new page my-page -t "My Page"` |
| Scaffold a new plugin | `mokadocs new plugin MyPlugin` |
| Scaffold a component example | `mokadocs new component card` |
| Build for production | `mokadocs build` |
| Build with drafts visible | `mokadocs build --draft` |
| Watch and rebuild on changes | `mokadocs build --watch` |
| Start dev server | `mokadocs serve` |
| Start dev server on port 3000 | `mokadocs serve -p 3000` |
| Start dev server without opening browser | `mokadocs serve --no-open` |
| Check the build without writing output | `mokadocs validate` |
| Run diagnostics | `mokadocs doctor` |
| Run diagnostics with auto-fix | `mokadocs doctor --fix` |
| View project stats | `mokadocs stats` |
| View project stats as JSON | `mokadocs stats --json` |
| Delete the output and the cache | `mokadocs clean` |
| Full clean rebuild | `mokadocs clean && mokadocs build` |
| Check environment | `mokadocs info` |
