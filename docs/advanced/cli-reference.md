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

---

## mokadocs init

Scaffolds a new MokaDocs project in the current directory. This creates a starter configuration file and docs folder so you can begin writing documentation immediately.

### What It Creates

- `mokadocs.yaml` - Default site configuration file with common settings pre-filled
- `docs/` - Documentation source directory
- `docs/index.md` - A starter homepage with example front matter and content

### Options

This command has no required or optional parameters. It operates on the current working directory.

### Usage

```bash
# Initialize a new MokaDocs project
mokadocs init
```

```bash
# Typical workflow: create a project directory, then initialize
mkdir my-library-docs
cd my-library-docs
mokadocs init
```

### Behavior

- If `mokadocs.yaml` already exists in the current directory, the command will warn you and exit without overwriting.
- The generated `docs/index.md` includes sample front matter demonstrating `title`, `order`, and `icon` properties.

---

## mokadocs build

Builds the documentation site by running the full build pipeline. This processes all Markdown files, analyzes C# projects (if configured), generates API reference pages, builds the search index, and writes the final static site to the output directory.

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--config <path>` | Path to the configuration file | `mokadocs.yaml` |
| `--output <path>` | Output directory (overrides the `build.output` value in config) | Value from config, typically `_site` |
| `--watch` | Watch for file changes and rebuild automatically | Off |
| `--verbose` | Enable verbose/debug logging for troubleshooting | Off |
| `--draft` | Include pages that have `visibility: draft` in their front matter | Off |
| `--base-path <path>` | Path prefix for subdirectory deployments (e.g. `/repo-name` for GitHub Pages) | Empty |
| `--no-cache` | Force a full rebuild, skipping any cached results from previous builds | Off |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Build completed successfully |
| 1 | Build failed due to errors (check diagnostics output) |

### Build Output Summary

On a successful build, MokaDocs prints a summary to the console:

```
Build completed successfully.
  Pages:              24
  API types:          18
  Search entries:     142
  Output:             _site/
  Duration:           1.23s
```

### Usage Examples

```bash
# Basic build with default settings
mokadocs build

# Build with a custom config file
mokadocs build --config ./config/mokadocs.prod.yaml

# Build to a custom output directory
mokadocs build --output ./dist

# Build including draft pages (useful during authoring)
mokadocs build --draft

# Watch mode: rebuilds automatically when files change
mokadocs build --watch

# Full clean rebuild with debug logging
mokadocs build --no-cache --verbose

# Build for GitHub Pages subdirectory deployment
mokadocs build --base-path /my-repo

# Combine options
mokadocs build --config mokadocs.yaml --output ./dist --draft --verbose
```

### Notes

- The `--watch` flag keeps the process running and monitors the `docs/` directory and `mokadocs.yaml` for changes. Press `Ctrl+C` to stop.
- Using `--no-cache` is recommended after upgrading MokaDocs or changing theme settings to ensure a clean build.
- The `--draft` flag is useful during development. Draft pages are excluded from production builds by default.

---

## mokadocs serve

Starts a local development server with hot reload support. This is the recommended way to preview your documentation while writing.

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--port <number>` | Port number for the development server | `5080` |
| `--config <path>` | Path to the configuration file | `mokadocs.yaml` |
| `--output <path>` | Output directory | Value from config |
| `--open` | Automatically open the site in the default browser when the server starts | On |
| `--no-open` | Disable automatic browser opening | Off |
| `--draft` | Include pages that have `visibility: draft` in their front matter | Off |
| `--base-path <path>` | Path prefix the site is served under, matching `build.basePath` | Value from config |
| `--verbose` | Enable verbose logging | Off |

### Features

- **Hot Reload via WebSocket**: The dev server injects a small WebSocket client into served pages. When a source file changes, the browser automatically refreshes without manual intervention.
- **File Watching**: Monitors the `docs/` directory and `mokadocs.yaml` for changes. Any modification triggers an incremental rebuild followed by a browser refresh.
- **Clean URL Support**: Serves clean URLs automatically. A request to `/guide` resolves to `/guide/index.html`, so your local preview matches production behavior.
- **Custom 404 Page**: Displays a styled 404 page (with dark/light theme support) when a route is not found.
- **Base Path Aware**: If the site is built for a subdirectory (`build.basePath` or `--base-path`), the server strips that prefix from incoming requests, so `http://localhost:5080/my-repo/` serves the same pages the deployed site will.
- **REPL Execution Endpoint**: When the REPL plugin is active, exposes `POST /api/repl/execute` for running C# code snippets interactively.
- **Blazor Preview Endpoint**: When the Blazor plugin is active, exposes `POST /api/blazor/preview` for rendering Blazor component previews.

### Auto-Open Browser

By default, `mokadocs serve` opens your site in the default browser when the server starts. This works cross-platform (macOS, Windows, and Linux). Use `--no-open` to disable this behavior.

### Usage Examples

```bash
# Start the dev server on the default port (5080)
mokadocs serve

# Start on a custom port
mokadocs serve --port 3000

# Start without opening the browser
mokadocs serve --no-open

# Start with a specific config file
mokadocs serve --config mokadocs.prod.yaml

# Start with verbose logging to debug issues
mokadocs serve --verbose
```

### Output

```
MokaDocs dev server started.
  Listening on:  http://localhost:5080
  Watching:      docs/, mokadocs.yaml
  Hot reload:    enabled (WebSocket)

Press Ctrl+C to stop.
```

---

## mokadocs new

Scaffolds new pages, plugins, or component examples for your MokaDocs project. This command has three subcommands: `page`, `plugin`, and `component`.

### mokadocs new page

Creates a new Markdown documentation page with pre-filled front matter.

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--title <text>` | `-t` | The page title used in front matter | Derived from `<name>` |
| `--path <dir>` | `-p` | Directory where the page file is created | `./docs` |
| `--layout <name>` | | Layout template to set in front matter (`default`, `wide`, `landing`, `raw`) | `default` |
| `--order <number>` | | Sort order value to set in front matter | None |

#### Usage

```bash
# Create a basic page in the docs directory
mokadocs new page getting-started

# Create a page with a custom title and order
mokadocs new page installation --title "Installation Guide" --order 1

# Create a page in a subdirectory with a specific layout
mokadocs new page overview --path ./docs/guides --layout wide
```

### mokadocs new plugin

Scaffolds a new plugin project with a `.csproj` file and a starter `IMokaPlugin` implementation stub. This gives you a working project structure that you can build on immediately.

#### Usage

```bash
# Scaffold a new plugin project
mokadocs new plugin MyCustomPlugin
```

This creates a directory named `MyCustomPlugin` containing a `.csproj` configured for MokaDocs plugin development and a C# file with an `IMokaPlugin` stub ready to implement.

### mokadocs new component

Generates an example page demonstrating a built-in MokaDocs component. Use this to quickly see how a component works and to get starter markup you can copy into your own pages.

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

# Generate a code-group example
mokadocs new component code-group
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
| Broken Links | Root-relative links and images point at something the build produces |
| Front Matter | Every page has a non-empty `title` |
| Orphan Images | Images in the docs folder that nothing references |
| Plugins | Every `plugins[].name` matches a real plugin id and initializes |
| Search | The search index builds with entries. Search turned off in config passes. |
| API Coverage | Share of public types and members with a summary. Only shown when projects are configured. |

### How Links Are Checked

A link counts as valid when it resolves to any of:

- a page, including API reference pages, plugin pages, and pages whose front matter sets `route:`
- a section directory such as `/guide`, which the build redirects to its first page
- a static asset in the docs folder, the logo or favicon, or a generated file like `/sitemap.xml`

Links are read from the parsed Markdown, not with a text search, so examples inside code blocks and inline code are never reported. Links to a draft page are reported and labelled, since drafts are left out of production builds. Only root-relative links (`/guide/intro`) are checked; relative links and external URLs are not.

### How API Coverage Is Measured

Coverage is read from the API model generated from your source code, the same one your API pages are built from. A type or member counts as documented when it has a `<summary>`, or when its comment is `<inheritdoc/>`, even if the inherited text lives in a framework type that MokaDocs cannot read. A delegate's `Invoke` shares the delegate's own documentation. Use `--verbose` to list what is missing.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--fix` | | Automatically fix problems where possible. Currently adds a missing front matter `title`, derived from the file name, or from the folder name for `index.md`. | Off |
| `--config <path>` | `-c` | Path to the configuration file. Paths inside it resolve relative to the file, not the directory you run the command from. | `mokadocs.yaml` |
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

Displays statistics about your documentation project, including page counts, word counts, API coverage, and more.

### Output

The command prints a formatted table with the following information:

| Metric | Description |
|--------|-------------|
| Pages | Total number of documentation pages |
| Word count | Total word count across all pages |
| API types | Number of C# types extracted from configured projects |
| API members | Total number of members across all API types |
| XML doc coverage | Percentage of public API members with XML documentation comments |
| Namespaces | Number of distinct namespaces in API projects |
| Plugins | Number of loaded plugins |
| Docs size | Total size of the documentation source files on disk |

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--json` | | Output stats as JSON instead of a table (useful for CI pipelines) | Off |

### Usage

```bash
# Show project statistics
mokadocs stats

# Output as JSON for CI integration
mokadocs stats --json

# Use a specific config file
mokadocs stats --config mokadocs.prod.yaml
```

---

## mokadocs clean

Deletes the output directory to remove all previously built files. This is useful before a fresh build or when switching between configurations.

### Behavior

- Reads the `build.output` path from your configuration file (defaults to `_site`).
- Recursively deletes the entire output directory and its contents.
- If the output directory does not exist, the command exits silently without error.

### Usage Examples

```bash
# Clean the output directory
mokadocs clean

# Clean and then rebuild
mokadocs clean && mokadocs build

# Clean with a custom config (uses that config's output path)
mokadocs clean --config mokadocs.prod.yaml
```

---

## mokadocs info

Displays environment and configuration information. This is helpful for debugging issues or when filing bug reports.

### Output

```
MokaDocs Environment Info
  MokaDocs version:  1.2.0
  .NET version:      8.0.100
  OS:                macOS 15.2 (Darwin 24.2.0)
  Config path:       /Users/dev/project/mokadocs.yaml
  Output path:       /Users/dev/project/_site
  Docs path:         /Users/dev/project/docs
```

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
- **`mokadocs validate`** shows what the build itself reports: a page that failed to parse or render, a project that could not be analyzed, a plugin that crashed or never loaded. These only show up by actually running the build.

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

Each problem is prefixed with the build phase that reported it.

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
| Scaffold a new page | `mokadocs new page my-page --title "My Page"` |
| Scaffold a new plugin | `mokadocs new plugin MyPlugin` |
| Scaffold a component example | `mokadocs new component card` |
| Build for production | `mokadocs build` |
| Build with drafts visible | `mokadocs build --draft` |
| Start dev server | `mokadocs serve` |
| Start dev server on port 3000 | `mokadocs serve --port 3000` |
| Start dev server without opening browser | `mokadocs serve --no-open` |
| Check the build without writing output | `mokadocs validate` |
| Run diagnostics | `mokadocs doctor` |
| Run diagnostics with auto-fix | `mokadocs doctor --fix` |
| View project stats | `mokadocs stats` |
| View project stats as JSON | `mokadocs stats --json` |
| Clean build output | `mokadocs clean` |
| Full clean rebuild | `mokadocs clean && mokadocs build --no-cache` |
| Check environment | `mokadocs info` |
| Watch and rebuild on changes | `mokadocs build --watch` |
