# Changelog

All notable changes to MokaDocs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.5] - 2026-03-27

### Changed
- Updated Markdig to 1.1.2, Scriban to 7.0.5, Verify.XunitV3 to 31.13.5
- NuGet badge in README now dynamically pulls latest version from nuget.org

## [1.0.4] - 2026-03-27

### Fixed
- All markdown and API tables are now horizontally scrollable on mobile
- Added `table-responsive` wrapper to API member, parameter, exception, and OpenAPI tables

## [1.0.3] - 2026-03-27

### Fixed
- Logo path now uses `base_path` prefix for subdirectory deployments
- Hide duplicate site name when logo image is configured

## [1.0.2] - 2026-03-27

### Fixed
- Logo path uses `base_path` for correct rendering on GitHub Pages

## [1.0.1] - 2026-03-27

### Fixed
- Stop double-encoding XML doc summaries in API index tables

## [1.0.0] - 2026-03-24

### Added
- **13 projects**: Core, CLI, Engine, Parsing, CSharp, Rendering, Themes, Search, Plugins, Serve, Versioning, Cloud, AspNetCore
- **10-phase build pipeline** with Roslyn C# analysis and Markdig markdown parsing
- **4 built-in plugins**: Interactive REPL, Blazor component preview, Changelog timeline, OpenAPI documentation
- **Embedded default theme** with 5 color themes (ocean, emerald, violet, amber, rose), 7 code syntax themes, 4 code block styles
- **Dark/light mode** with system preference detection and manual toggle
- **Full-text client-side search** with Ctrl+K / Cmd+K keyboard shortcut
- **Dev server** with WebSocket hot reload and file watcher
- **ASP.NET Core integration** via `AddMokaDocs()` / `MapMokaDocs()` for embedded docs
- **Versioning support** with multi-version dropdown selector
- **`basePath` support** for subdirectory deployments (GitHub Pages, IIS subfolders)
- **Social links** in footer (GitHub, NuGet, Discord, Twitter, and 70+ Lucide icons)
- **NuGet install widget** on API reference pages with tabbed install commands
- **Type dependency graphs** — auto-generated Mermaid class diagrams on API pages
- **Custom markdown extensions**: admonitions, tabs, cards, steps, link-cards, code groups, Mermaid diagrams, changelog blocks
- **`<inheritdoc/>` resolution** — walks base types and interfaces for missing documentation
- **Favicon and logo support** via `site.favicon` and `site.logo` config
- **Feature gating** — hide pages behind feature flags via `requires` front matter
- **Sitemap and robots.txt** generation
- **384 tests** across 5 test projects (net9.0 + net10.0)
- **9 CLI commands**: init, build, serve, clean, info, validate, doctor, stats, new

[1.0.5]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/jacobwi/Moka.Docs/releases/tag/v1.0.0
