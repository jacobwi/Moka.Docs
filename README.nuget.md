# MokaDocs

**The modern documentation site generator built for .NET libraries.**

Point MokaDocs at your `.csproj` and `docs/` folder — it auto-discovers your API surface, parses XML docs, and generates a complete documentation site.

[![.NET 9/10](https://img.shields.io/badge/.NET-9.0%20%7C%2010.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com) [![MIT License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/jacobwi/Moka.Docs/blob/main/LICENSE) [![Tests](https://img.shields.io/badge/tests-384%20passed-brightgreen?style=flat-square)](https://github.com/jacobwi/Moka.Docs/actions) [![NuGet](https://img.shields.io/nuget/v/Moka.Docs.Core?style=flat-square&logo=nuget&color=orange)](https://www.nuget.org/packages/Moka.Docs.Core)

## Quick Start

```bash
dotnet tool install -g mokadocs
mokadocs init
mokadocs serve
```

## Features

- **C# API Reference** — Auto-generated from assemblies with full type info, XML comments, and `<inheritdoc/>` support
- **Markdown Guides** — Admonitions, tabs, code groups, task lists, footnotes, and more
- **Interactive REPL** — Run C# code directly in the browser (Roslyn-powered)
- **Full-Text Search** — Client-side instant search with `Ctrl+K` / `Cmd+K`
- **5 Color Themes** — Ocean, Emerald, Violet, Amber, Rose with live switcher
- **7 Code Themes** — Catppuccin, GitHub, Dracula, One Dark, Nord
- **Dark/Light Mode** — Auto-detects system preference
- **Hot Reload** — File watcher + WebSocket for instant preview
- **Versioning** — Multi-version docs with dropdown selector
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
site:
  title: "My Library"
  description: "Documentation for My Library"

content:
  docs: ./docs
  projects:
    - path: ./src/MyLibrary/MyLibrary.csproj

theme:
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

## Links

- [GitHub](https://github.com/jacobwi/Moka.Docs)
- [Documentation](https://mokadocs.dev)
- [License: MIT](https://github.com/jacobwi/Moka.Docs/blob/main/LICENSE)
