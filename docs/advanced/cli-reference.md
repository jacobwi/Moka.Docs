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

- `mokadocs.yaml` — Default site configuration file with common settings pre-filled
- `docs/` — Documentation source directory
- `docs/index.md` — A starter homepage with example front matter and content

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
| `--verbose` | Enable verbose logging | Off |

### Features

- **Hot Reload via WebSocket**: The dev server injects a small WebSocket client into served pages. When a source file changes, the browser automatically refreshes without manual intervention.
- **File Watching**: Monitors the `docs/` directory and `mokadocs.yaml` for changes. Any modification triggers an incremental rebuild followed by a browser refresh.
- **Clean URL Support**: Serves clean URLs automatically. A request to `/guide` resolves to `/guide/index.html`, so your local preview matches production behavior.
- **Custom 404 Page**: Displays a styled 404 page (with dark/light theme support) when a route is not found.
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

Runs a suite of diagnostic checks against your MokaDocs project and reports any problems found. This is useful for catching configuration issues, broken links, missing metadata, and other common problems before they reach production.

### Checks

The `doctor` command runs 11 built-in checks:

| Check | What It Validates |
|-------|-------------------|
| Config | `mokadocs.yaml` syntax and required fields |
| .NET SDK | Presence and version of the .NET SDK |
| Projects | `.csproj` paths in config resolve to valid files |
| XML Docs | C# projects have XML documentation generation enabled |
| Docs folder | The configured docs directory exists and contains `.md` files |
| Broken links | Internal links between pages resolve to valid targets |
| Missing front matter | Pages that lack a `title` or other recommended fields |
| Orphan images | Image files in the docs directory that are not referenced by any page |
| Plugins | Configured plugins can be resolved and loaded |
| Search | Search index can be built without errors |
| API coverage | Percentage of public API members that have XML doc comments |

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--fix` | | Automatically fix problems where possible (e.g., add missing front matter titles) | Off |
| `--config <path>` | `-c` | Path to the configuration file | `mokadocs.yaml` |
| `--verbose` | `-v` | Show detailed output for each check, including passed checks | Off |

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

# Run with a custom config and verbose output
mokadocs doctor --config mokadocs.prod.yaml --verbose
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

## mokadocs validate (Superseded)

> The `mokadocs validate` command was originally planned but has been superseded by [`mokadocs doctor`](#mokadocs-doctor), which provides a broader set of diagnostic checks along with auto-fix support. Use `mokadocs doctor` instead.

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
| Run diagnostics | `mokadocs doctor` |
| Run diagnostics with auto-fix | `mokadocs doctor --fix` |
| View project stats | `mokadocs stats` |
| View project stats as JSON | `mokadocs stats --json` |
| Clean build output | `mokadocs clean` |
| Full clean rebuild | `mokadocs clean && mokadocs build --no-cache` |
| Check environment | `mokadocs info` |
| Watch and rebuild on changes | `mokadocs build --watch` |
