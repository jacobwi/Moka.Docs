---
title: MokaDocs
description: A static documentation site generator for .NET libraries.
order: 0
layout: landing
featuresTitle: Everything you need
featuresSubtitle: Documentation sites for .NET libraries
features:
  - icon: "C#"
    title: C# API Reference
    description: Reference pages generated from the doc comments in your C# source.
  - icon: "</>"
    title: Themes
    description: A default theme with color presets, or your own Scriban layouts.
  - icon: "⚡"
    title: Instant Search
    description: Client-side search over titles, headings, tags and page text. No external service.
  - icon: "☽"
    title: Dark Mode
    description: Automatic light and dark mode with system preference detection and manual toggle support.
  - icon: "v2"
    title: Versioning
    description: A version dropdown linking to the docs you publish for each release.
  - icon: "⚙"
    title: Plugins
    description: Built-in plugins for runnable C#, Blazor previews, changelogs, OpenAPI specs and Python APIs.
---

## Simple Configuration

A minimal `mokadocs.yaml`:

```yaml
site:
  title: My Project
  description: Docs for my .NET library
content:
  docs: ./docs
  projects:
    - path: ./src/MyLib/MyLib.csproj
```

## Quick Start

:::steps

### Install MokaDocs

Install the MokaDocs CLI as a .NET global tool:

```bash
dotnet tool install -g mokadocs
```

### Initialize Your Project

In your library's folder, create `mokadocs.yaml` and a starter `docs/index.md`:

```bash
mokadocs init
```

### Start Writing

Build the site and serve it at `http://localhost:5080`. Saving a file in `docs/` rebuilds the site and reloads the browser:

```bash
mokadocs serve
```

:::

## Key Features

:::link-cards
- [API Documentation](/guide/api-docs) - API reference pages generated from C# source and its XML doc comments
- [Markdown Guides](/guide/markdown) - Admonitions, tabs, code groups and other Markdown extensions
- [UI Components](/guide/components) - Cards, steps, link-cards, and code-group components
- [Mermaid Diagrams](/guide/diagrams) - Flowcharts, sequence diagrams, class diagrams
- [Interactive REPL](/plugins/repl) - Run C# code blocks from the page while the dev server is running. A deployed static site can't run them
- [Blazor Preview](/plugins/blazor-preview) - Live preview Blazor components in docs
- [Versioning](/advanced/versioning) - A header dropdown linking to the doc versions you build and deploy
- [Themes](/themes/customization) - Theme options, or your own layouts and CSS in a theme folder
:::

## Packages

| Package | Description |
|---------|-------------|
| `mokadocs` | The CLI, installed as a .NET tool (project `Moka.Docs.Cli`) |
| `Moka.Docs.Core` | Configuration, page and API models, and the build pipeline interfaces |
| `Moka.Docs.Engine` | Build pipeline and phases, including search index generation |
| `Moka.Docs.Parsing` | Markdown and front matter parsing with the custom extensions |
| `Moka.Docs.Rendering` | Scriban template rendering |
| `Moka.Docs.CSharp` | Roslyn-based C# API analysis |
| `Moka.Docs.Themes` | The built-in theme and custom theme loading |
| `Moka.Docs.Plugins` | Plugin system and built-in plugins |
| `Moka.Docs.Serve` | Dev server with hot reload, plus the REPL and Blazor preview endpoints |
| `Moka.Docs.Versioning` | Builds the version list for the header version dropdown |
| `Moka.Docs.AspNetCore` | Builds and serves docs inside an ASP.NET Core app (`AddMokaDocs`, `MapMokaDocs`) |
| `Moka.Docs.Cloud` | Services that check the `cloud:` settings. Nothing in the CLI or the build calls them |
| `Moka.Docs.Search` | Empty placeholder. Search index generation lives in `Moka.Docs.Engine` |
