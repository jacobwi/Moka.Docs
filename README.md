<p align="center">
  <a href="https://dotnet.microsoft.com"><img src="https://img.shields.io/badge/.NET-9.0%20%7C%2010.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 9/10" /></a>
  <a href="https://github.com/jacobwi/Moka.Docs/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue?style=flat-square" alt="MIT License" /></a>
  <a href="https://github.com/jacobwi/Moka.Docs/actions"><img src="https://img.shields.io/badge/tests-384%20passed-brightgreen?style=flat-square" alt="Tests" /></a>
  <a href="https://www.nuget.org/packages/Moka.Docs.Core"><img src="https://img.shields.io/nuget/v/Moka.Docs.Core?style=flat-square&logo=nuget&color=orange" alt="NuGet" /></a>
</p>

<p align="center">
  <img src="icon.png" alt="MokaDocs" width="120" />
</p>

<h1 align="center">MokaDocs</h1>

<p align="center">
  <strong>The modern documentation site generator built for .NET libraries.</strong>
</p>

<p align="center">
  Point MokaDocs at your <code>.csproj</code> and <code>docs/</code> folder.<br/>
  It auto-discovers your API surface, parses XML docs, and generates a complete site.
</p>

---

## Quick Start

```bash
# Install
dotnet tool install -g mokadocs

# Initialize in your project
mokadocs init

# Start dev server with hot reload
mokadocs serve

# Build for production
mokadocs build
```

## Features

- **C# API Reference** — Auto-generate docs from assemblies with full type info, XML comments, and `<inheritdoc/>` support
- **Markdown Guides** — Admonitions, tabs, code groups, task lists, footnotes, and more via Markdig
- **Interactive REPL** — Readers run C# code directly in the browser (Roslyn-powered)
- **Blazor Component Preview** — Live-render Razor components in your docs
- **Mermaid Diagrams** — Flowcharts, sequence diagrams, class diagrams with dark/light mode
- **UI Components** — Cards, steps, link-cards, and code-group custom blocks
- **Release Changelog** — Rich timeline UI with version badges, category filters, and collapsible entries
- **Full-Text Search** — Client-side instant search with `Ctrl+K` / `Cmd+K`
- **5 Color Themes** — Ocean, Emerald, Violet, Amber, Rose with live switcher
- **7 Code Syntax Themes** — Catppuccin, GitHub, Dracula, One Dark, Nord
- **4 Code Block Styles** — Plain, macOS (traffic lights), Terminal, VS Code
- **Dark/Light Mode** — Auto-detects system preference with manual toggle
- **Versioning** — Multi-version docs with dropdown selector
- **Hot Reload** — File watcher + WebSocket for instant preview
- **Feedback Widget** — "Was this page helpful?" on every page
- **Type Dependency Graphs** — Auto-generated Mermaid diagrams on API pages
- **ASP.NET Core Integration** — Embed docs directly in your web app

## ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMokaDocs(options =>
{
    options.Title = "My API Docs";
    options.Assemblies = [typeof(MyService).Assembly];
});

var app = builder.Build();
app.MapMokaDocs("/docs");
app.Run();
```

## Configuration

```yaml
# mokadocs.yaml
site:
  title: "My Library"
  description: "Documentation for My Library"

content:
  docs: ./docs
  projects:
    - path: ./src/MyLibrary/MyLibrary.csproj

theme:
  name: default
  options:
    primaryColor: "#0ea5e9"
    codeTheme: catppuccin-mocha
    codeStyle: macos

plugins:
  - name: mokadocs-repl
  - name: mokadocs-changelog
```

## Packages

| Package | Description |
|---------|-------------|
| `Moka.Docs.Core` | Core models, configuration, and interfaces |
| `Moka.Docs.CLI` | Command-line tool (`mokadocs`) |
| `Moka.Docs.Engine` | Build pipeline and phase orchestration |
| `Moka.Docs.Parsing` | Markdown parsing with custom extensions |
| `Moka.Docs.CSharp` | Roslyn-based C# API analysis |
| `Moka.Docs.Rendering` | Scriban template engine |
| `Moka.Docs.Themes` | Theme system and default theme |
| `Moka.Docs.Plugins` | Plugin system (REPL, Blazor, OpenAPI, Changelog) |
| `Moka.Docs.Serve` | Dev server with hot reload |
| `Moka.Docs.AspNetCore` | ASP.NET Core middleware integration |
| `Moka.Docs.Search` | Client-side search index generation |
| `Moka.Docs.Versioning` | Multi-version documentation support |

## Development

```bash
# Build
dotnet build MokaDocs.sln

# Test (384 tests across 5 projects x 2 TFMs)
dotnet test MokaDocs.sln

# Run self-documenting site
dotnet run --project src/Moka.Docs.CLI -- serve

# Run sample library site
cd samples/Moka.Docs.Samples.Library
dotnet run --project ../../src/Moka.Docs.CLI -- serve
```

## License

MIT
