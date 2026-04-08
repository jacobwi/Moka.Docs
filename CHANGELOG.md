# Changelog

All notable changes to MokaDocs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.1] - 2026-04-08

### ✨ New
- **`mokadocs-blazor-preview` plugin** now auto-discovers, auto-scaffolds, and
  auto-publishes the consumer's docs preview-host project. Yaml shrinks from
  ~120 lines of plugin config to a single `library: <PackageId>@<Version>` line.
- **Auto-discovery**: when `previewHost:` is omitted, the plugin scans
  `./preview-host/`, `./docs-preview-host/`, then any direct subdirectory of
  the docs root containing a `Microsoft.NET.Sdk.BlazorWebAssembly` csproj.
- **Auto-scaffold**: if no preview-host is found at the resolved location, the
  plugin writes a fresh, library-agnostic project from a generic template
  (csproj + Program.cs + wwwroot/index.html + empty Directory.Build.props /
  Directory.Build.targets / Directory.Packages.props shadow files so the
  scaffolded project doesn't inherit anything from the consumer repo's parent
  build configuration). The csproj substitutes `{LIBRARY_ID}` and
  `{LIBRARY_VERSION}` from the new `library:` yaml option, plus a pinned
  `Moka.Blazor.Repl.Host` `PackageReference` for the runtime.
- **Auto-publish**: the plugin shells out to `dotnet publish -c Release -f
  net10.0 -o publish-output/net10.0` on the preview-host. Incremental — skipped
  when the publish-output marker (`_framework/blazor.webassembly.js`) is newer
  than every input file under the preview-host directory (excluding `bin/`,
  `obj/`, `publish-output/`).
- **Owner contract**: scaffolded files are written ONLY when missing. mokadocs
  never overwrites user edits. The user owns the files thereafter and can
  freely customize Program.cs (services, theme), wwwroot/index.html (CSS, fonts),
  and the csproj (extra PackageReferences).

### 🔄 Changed
- New yaml option `library: <PackageId>@<Version>` (or just `<PackageId>` →
  resolves to `*` latest). Used by the auto-scaffold template's csproj
  PackageReference. Required when no preview-host exists yet.
- `previewHost:` yaml option is now **optional**. When omitted, auto-discovery
  resolves it to a conventional location.
- `references:` and `usings:` yaml options remain supported but are usually
  unnecessary now — the auto-scaffolded preview-host's bin already contains the
  consumer's library DLLs (resolved from nuget.org via the PackageReference),
  which the plugin reads for Roslyn references automatically.

## [1.3.0] - 2026-04-08

### ⚠️ Breaking
- **`mokadocs-blazor-preview` plugin** rewritten. The yaml schema for the plugin's
  options changed:
  - **Removed** `mode` (was `wasm` | `ssr`) — the plugin now always emits one Blazor
    WebAssembly iframe per preview block, with an SSR snapshot in `<noscript>` for
    crawlers and JS-disabled visitors.
  - **Removed** `wasmAppPath` and the implicit NuGet-cache discovery via
    `WasmAppAssetResolver`. The plugin no longer scans `~/.nuget/packages`.
  - **Removed** `stylesheets` — the consumer's preview-host project now ships its own
    CSS in its `index.html` and via Blazor's static web asset bundle.
  - **Added** required `previewHost` — path (relative to docs root) to the consumer's
    Blazor WebAssembly preview-host project. Conventional layout:
    `{previewHost}/bin/Release/{tfm}/` (Roslyn references) and
    `{previewHost}/publish-output/{tfm}/wwwroot/` or `publish-output/wwwroot/`
    (the static WASM runtime, copied verbatim to `_site/_preview-wasm/`).
  - `references` and `usings` are kept and now act as additive overrides on top of
    the preview-host bin. Same-named assemblies in `references` win, so a local
    source build can override a NuGet copy in the host bin.

### ✨ New
- Mokadocs is now **library-agnostic**: zero hardcoded references to Moka.Red. The
  previously hardcoded `Moka.Red.Feedback.Toast.IMokaToastService` SSR stub was removed.
- Framework-assembly filtering uses `Assembly.Load()` against the host runtime to
  detect which DLLs the .NET shared framework supplies — works on both .NET 9 and
  .NET 10 hosts with no hardcoded prefix lists.
- Iframes are emitted with `loading="lazy"` so off-screen previews don't boot a
  Blazor runtime until scrolled into view.

### 🔄 Changed
- `BlazorPreviewPlugin.Version` bumped to `3.0.0` (in-process plugin version, separate
  from the mokadocs CLI version).
- Iframe `<noscript>` SSR fallback HTML is rendered using the same Roslyn-compiled
  assembly bytes that ship to the browser, so crawlers see the same DOM as JS users.

### Removed
- `BlazorPreviewMode` enum.
- `WasmAppAssetResolver` (NuGet-cache scanning).

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
