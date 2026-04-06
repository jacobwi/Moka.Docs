# Changelog

All notable changes to MokaDocs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-04-06

### ✨ New
- **WASM Blazor preview mode** — interactive component previews on static sites (GitHub Pages, etc.)
  - Components compiled to DLLs at build time, loaded in-browser via Blazor WebAssembly iframe
  - SSR fallback in `<noscript>` for users without JavaScript
  - Configurable via `mode: wasm` (default) or `mode: ssr` in plugin options
- `Moka.Blazor.Repl.Wasm` auto-downloaded as dependency — no manual install needed
- `WasmAppAssetResolver` auto-discovers WASM app from NuGet cache
- Updated Blazor preview docs with WASM mode documentation

### 🔄 Changed
- Default Blazor preview mode changed from SSR to WASM
- Plugin falls back to SSR with warning if WASM app not found

## [1.1.2] - 2026-04-06

### ✨ New
- `showBuiltWith` theme option — shows "Built with MokaDocs v{version}" in footer (default `true`, set `false` to hide)
- MokaDocs version automatically read from assembly and displayed in footer

### 🔄 Changed
- Footer branding now includes version number on both default and landing layouts

## [1.1.1] - 2026-04-06

### 🐛 Fixed
- Use NuGet package for `Moka.Blazor.Repl.Compiler` instead of local ProjectReference
- Fix GitHub Packages auth for NuGet publish workflow

## [1.1.0] - 2026-04-06

### ✨ New
- **Blazor SSR preview** — replaced regex renderer with real Roslyn + HtmlRenderer server-side rendering
- `Moka.Blazor.Repl.Compiler` package integration for live Blazor component previews

### 🐛 Fixed
- Restored inline CSS/JS for Blazor preview chrome

### 🔄 Changed
- Updated Scriban to 7.0.6, Spectre.Console to 0.55.0, Verify.XunitV3 to 31.15.0

## [1.0.7] - 2026-03-27

### ✨ New
- Auto-create GitHub Release from CHANGELOG.md when version tags are pushed
- Auto-categorized release notes via `.github/release.yml`

## [1.0.6] - 2026-03-27

### ✨ New
- Show version in CLI startup messages (`MokaDocs v1.0.6 — Building...`)

### 🐛 Fixed
- NuGet social link icon now uses official NuGet logo SVG from [NuGet/Media](https://github.com/NuGet/Media)

## [1.0.5] - 2026-03-27

### ✨ New
- NuGet install widget on API reference pages with tabbed install commands and NuGet.org link
- NuGet and Discord social link icons for footer
- Social links showcase and NuGet widget docs in sample library

### 🔄 Changed
- Updated Markdig to 1.1.2, Scriban to 7.0.5, Verify.XunitV3 to 31.13.5
- NuGet badge in README now dynamically pulls latest version from nuget.org
- CI workflow triggers NuGet publish on version tags

## [1.0.4] - 2026-03-27

### 🐛 Fixed
- All markdown and API tables are now horizontally scrollable on mobile
- Added `table-responsive` wrapper to API member, parameter, exception, and OpenAPI tables
- Constructor summary tables use compact `TodoItem(…)` format instead of full parameter list

## [1.0.3] - 2026-03-27

### 🐛 Fixed
- Logo path now uses `base_path` prefix for subdirectory deployments
- Hide duplicate site name when logo image is configured

## [1.0.2] - 2026-03-27

### 🐛 Fixed
- Logo path uses `base_path` for correct rendering on GitHub Pages

## [1.0.1] - 2026-03-27

### 🐛 Fixed
- Stop double-encoding XML doc summaries in API index tables

## [1.0.0] - 2026-03-24

### ✨ New
- **13 projects**: Core, CLI, Engine, Parsing, CSharp, Rendering, Themes, Search, Plugins, Serve, Versioning, Cloud, AspNetCore
- **10-phase build pipeline** with Roslyn C# analysis and Markdig markdown parsing
- **4 built-in plugins**: Interactive REPL, Blazor preview, Changelog timeline, OpenAPI docs
- **Embedded default theme** — 5 color themes, 7 code syntax themes, 4 code block styles, dark/light mode
- **Full-text client-side search** with `Ctrl+K` / `Cmd+K` shortcut
- **Dev server** with WebSocket hot reload and file watcher
- **ASP.NET Core integration** via `AddMokaDocs()` / `MapMokaDocs()`
- **Versioning** with multi-version dropdown selector
- **`basePath` support** for subdirectory deployments (GitHub Pages, IIS subfolders)
- **Social links** in footer — GitHub, NuGet, Discord, Twitter, 70+ Lucide icons
- **Type dependency graphs** — auto-generated Mermaid class diagrams on API pages
- **Custom markdown extensions**: admonitions, tabs, cards, steps, link-cards, code groups, Mermaid, changelogs
- **`<inheritdoc/>` resolution** — walks base types and interfaces for missing docs
- **Favicon and logo** via `site.favicon` / `site.logo` config
- **Feature gating** — hide pages behind feature flags via `requires` front matter
- **Sitemap and robots.txt** generation
- **384 tests** across 5 test projects (net9.0 + net10.0)
- **9 CLI commands**: `init`, `build`, `serve`, `clean`, `info`, `validate`, `doctor`, `stats`, `new`

[1.2.0]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.2...v1.2.0
[1.1.2]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.7...v1.1.0
[1.0.7]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.6...v1.0.7
[1.0.6]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/jacobwi/Moka.Docs/releases/tag/v1.0.0
