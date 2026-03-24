---
title: MokaDocs
description: The modern documentation site generator built for .NET libraries.
order: 0
layout: landing
---

## Quick Start

:::steps

### Install MokaDocs

Install the MokaDocs CLI as a .NET global tool:

```bash
dotnet tool install -g mokadocs
```

### Initialize Your Project

Create a new documentation project in your library's directory:

```bash
mokadocs init
```

### Start Writing

Launch the dev server with hot reload:

```bash
mokadocs serve
```

:::

## Key Features

:::link-cards
- [API Documentation](/guide/api-docs) — Auto-generate API reference pages from C# code and XML comments
- [Markdown Guides](/guide/markdown) — Admonitions, tabs, code groups, and more
- [UI Components](/guide/components) — Cards, steps, link-cards, and code-group components
- [Mermaid Diagrams](/guide/diagrams) — Flowcharts, sequence diagrams, class diagrams
- [Interactive REPL](/plugins/repl) — Let readers run C# code in the browser
- [Blazor Preview](/plugins/blazor-preview) — Live preview Blazor components in docs
- [Versioning](/advanced/versioning) — Multi-version documentation support
- [Themes](/themes/customization) — Customize colors, fonts, and layout
:::

## Packages

| Package | Description |
|---------|-------------|
| `Moka.Docs.Core` | Core models, configuration, and interfaces |
| `Moka.Docs.CLI` | Command-line interface (`mokadocs` tool) |
| `Moka.Docs.Engine` | Build pipeline and phase orchestration |
| `Moka.Docs.Parsing` | Markdown parsing with custom extensions |
| `Moka.Docs.Rendering` | Scriban template engine integration |
| `Moka.Docs.CSharp` | Roslyn-based C# API analysis |
| `Moka.Docs.Themes` | Theme management and default theme |
| `Moka.Docs.Plugins` | Plugin system and built-in plugins |
| `Moka.Docs.Search` | Client-side search index generation |
| `Moka.Docs.Serve` | Dev server with hot reload and REPL |
| `Moka.Docs.Versioning` | Multi-version documentation support |
| `Moka.Docs.AspNetCore` | ASP.NET Core integration middleware |
