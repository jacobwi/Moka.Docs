# Changelog

All notable changes to MokaDocs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-04-08 — Python API Reference Plugin

### ✨ New
- **`mokadocs-python-api` plugin** — generates API reference documentation for
  Python libraries. Parses `.py` source files via a bundled Python script
  (pure stdlib `ast` module, zero pip dependencies) and produces the same page
  layout as the existing C# API docs using `ApiPageRenderer`.

  Supports:
  - Classes, dataclasses (`Record`), enums, protocols/ABCs (`Interface`)
  - Methods (instance, static, class, abstract), properties, fields
  - Type annotations (parameters, return types, class variables, `Optional[T]`)
  - Decorators → badges and attributes
  - Google-style docstrings (Args, Returns, Raises, Examples, Attributes, Note,
    Warning, See Also sections)
  - `__all__` exports filter
  - Module-level functions as static members of a synthetic module type
  - Mermaid type dependency graphs

  Configuration:
  ```yaml
  plugins:
    - name: mokadocs-python-api
      options:
        source: ../src/mylib           # path to Python source dir
        label: "Python API"             # nav label (default)
        routePrefix: /python-api        # URL prefix (default)
        docstringFormat: google          # docstring parser (default)
        pythonPath: python3              # Python executable (default)
  ```

  Requires Python 3.9+ on PATH. Falls back from `python3` → `python` on
  Windows where `python3` is a Microsoft Store alias (exit code 9009).

### 📚 Docs
- New `docs/plugins/python-api.md` — full plugin documentation with config
  reference, supported constructs table, docstring parsing examples,
  limitations, and a worked end-to-end example.
- Fixed `docs/themes/customization.md` footer section — was using a fake
  `footer: copyright:` top-level yaml key that doesn't exist in the schema.
  Now correctly documents `site: copyright:` and `theme: options: showBuiltWith:`
  to control the "Built with MokaDocs" branding.

## [1.3.8] - 2026-04-08

### ✨ New — `SiteAssetReference` for logo and favicon

`site.logo` and `site.favicon` in `mokadocs.yaml` now support the full range
of path forms users actually want to write, with automatic resolution,
asset copying, and base-path-aware URL emission. The new
`SiteAssetReference` type parses the yaml value once at config load time
and downstream code works only with resolved data (`SourcePath`,
`PublishUrl`, `IsAbsoluteUrl`).

Supported path forms:

| Yaml value | Publish URL | File copied |
|---|---|---|
| `logo.png` (bare filename) | `/logo.png` | `_site/logo.png` |
| `assets/logo.svg` (relative) | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `./assets/logo.svg` | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `/assets/logo.svg` (leading slash) | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `../branding/logo.png` (escapes yaml dir) | `/_media/logo.png` | `_site/_media/logo.png` |
| `https://cdn.example.com/logo.png` | `https://cdn.example.com/logo.png` | (no copy) |
| `//cdn.example.com/logo.png` | `//cdn.example.com/logo.png` | (no copy) |
| `data:image/svg+xml;base64,…` | *(URL verbatim)* | (no copy) |

Prior to this release, only relative paths inside the `content.docs` tree
were actually discovered by the asset glob. Paths at the yaml directory
level, parent-directory escapes, and absolute URLs were silently broken
or required unrelated assets under `content.docs` to work by accident.

### 🛡 Implementation details

- **`SiteAssetReference`** — new sealed record in `Moka.Docs.Core.Configuration`
  with `RawValue`, `SourcePath`, `PublishUrl`, and `IsAbsoluteUrl` fields.
- **`SiteConfigReader.ParseAssetReference()`** — new private helper that
  resolves each logo/favicon yaml string against the source file's
  directory, normalizes `./` prefixes, flattens `../` escapes to
  `/_media/{filename}`, and detects absolute URLs (http/https/protocol-
  relative/data URI) for pass-through.
- **Collision detection** — when logo and favicon both flatten to the
  same publish URL from different source files, `SiteConfigReader`
  throws `SiteConfigException` with a clear error message.
- **`BrandAssetResolver`** — new service in `Moka.Docs.Engine.Discovery`
  that runs in the Discovery phase (after the normal glob) and populates
  `BuildContext.BrandAssetFiles` with resolved logo + favicon entries.
  Logs warnings for missing source files but doesn't fail the build.
- **`OutputPhase.CopyAssets`** — now copies brand assets to their
  resolved publish URLs in addition to the normal `content.docs` glob
  output. Skips files already written by the main glob path to avoid
  overwrite conflicts.
- **`ScribanTemplateEngine`** — exposes two new template variables per
  brand asset:
  - `site.logo_url` / `site.favicon_url` — the final URL the theme
    should emit, with base-path prefix applied for relative paths and
    pass-through for absolute URLs.
  - `site.logo` / `site.favicon` — the raw yaml value, kept for
    backward compatibility with any custom user template that read the
    old string directly.
- **`EmbeddedThemeProvider`** — five template sites updated to use
  `site.logo_url` / `site.favicon_url` instead of the old
  `{{ base_path }}/{{ site.logo }}` concatenation, so the new
  absolute-URL pass-through and out-of-tree `_media/` flattening work
  without any conditional logic in the markup.

### 🩺 `doctor` command

Added a new check that validates `site.logo` and `site.favicon`:

- ✓ Pass with the resolved `rawValue → publishUrl` mapping, flagging
  when an escaped source is flattened to `/_media/`.
- ✓ Pass for absolute URLs without touching the filesystem.
- ⚠ Warn when the asset reference has no resolvable source path
  (e.g. yaml parsed without a yamlDir).
- ✗ Error when the source file doesn't exist on disk.

Unset brand assets are silently skipped — a site without a logo is
valid and shouldn't clutter the doctor output.

### 📚 Docs

- **`docs/plugins/blazor-preview.md`** — rewritten for the v3.x plugin
  API. The previous doc described the legacy `mode: wasm | ssr` /
  `wasmAppPath` / `stylesheets` schema which was removed in v1.3.0.
  The new doc covers auto-scaffold + auto-publish, the `library:` yaml
  option, the preview-host project structure, GitHub Pages deployment,
  a full troubleshooting section, and migration notes for users coming
  from the old schema.
- **`docs/configuration/site-config.md`** — expanded the `logo` and
  `favicon` sections with a full table of supported path forms plus
  worked examples for each resolution rule.

### 🧪 Tests

- **`SiteConfigReaderTests`** — 13 new tests covering every path form
  (bare filename, nested relative, `./` normalization, single-level
  `../` escape, multi-level `../` escape, absolute URLs for all four
  scheme types, leading-slash treatment, round-trip raw value
  preservation, collision detection, missing asset returns null).
- **`BrandAssetResolverTests`** — 6 new tests with `MockFileSystem`
  covering inside-yaml-dir resolution, escape-to-media flattening,
  missing source file handling (warns, doesn't throw), absolute URL
  pass-through (not added to copy list), and multi-asset resolution.

**Total:** 211 tests passing (was 198).

### 🔄 Changed

- `SiteMetadata.Logo` and `SiteMetadata.Favicon` changed type from
  `string?` to `SiteAssetReference?`. This is a public API break for
  anyone consuming mokadocs as a library (not a CLI), but the yaml
  schema is 100% backward compatible — all existing `mokadocs.yaml`
  files that set logo/favicon to a relative path inside `content.docs`
  continue to work unchanged. The `SiteMetadataDto` (yaml-facing)
  still uses `string?` on both fields; conversion happens in the reader.
- `Moka.Docs.AspNetCore.SiteConfigFactory` updated to wrap
  `MokaDocsOptions.LogoUrl` / `FaviconUrl` in a `SiteAssetReference`
  with `IsAbsoluteUrl=true` (treats them as consumer-hosted URLs that
  need no copy or base-path prefix).

## [1.3.7] - 2026-04-08

### 🎨 Changed — preview box fonts inherit from mokadocs theme

Follow-up to v1.3.6 theme token inheritance. The Blazor preview box's
Preview / Source tabs and source code block were still rendering with
hardcoded font stacks (`'JetBrains Mono', 'Cascadia Code', ...`) even
though the mokadocs theme exposes `--font-body` and `--font-mono`. Now:

- `.blazor-preview-tab` uses `var(--font-body, inherit)` so tab text
  matches the surrounding docs body font
- `.blazor-preview-source code` uses `var(--font-mono, ...)` so the
  source code tab matches the rest of the docs' `<code>` styling
- `.blazor-preview-error` uses `var(--font-mono, ...)` likewise

Fallback font stacks remain for standalone consumers without a mokadocs
theme.

## [1.3.6] - 2026-04-08

### 🎨 Changed — themed Blazor preview box

All colors in the `mokadocs-blazor-preview` container chrome (the outer box
with the Preview / Source tabs that wraps each preview iframe) now inherit
from mokadocs theme tokens (`--color-primary`, `--color-border`,
`--color-bg-secondary`, `--color-text`, `--color-text-muted`,
`--color-border-light`). Previously the tabs and badge used hardcoded
slate/violet/blue hex values (`#94a3b8`, `#60a5fa`, `#7c3aed`, `#ede9fe`)
that clashed with consumer themes — e.g. Moka.Red docs with its red accent
had blue active tabs and purple "Blazor" badges. Now:

- Active tab underline + text inherit `--color-primary` → matches the
  consumer's accent (red for Moka.Red, blue for ocean, emerald, etc.)
- Hover state uses `--color-text` on `--color-bg` for a subtle theme-aware
  highlight
- Inactive tabs use `--color-text-muted`
- Container border, tab separator, and grid background use
  `--color-border` / `--color-border-light`
- "Blazor" badge became a theme-colored outlined pill chip instead of a
  hardcoded purple block
- Error state uses `--color-primary` + `--color-bg-secondary` instead of
  hardcoded red/pink
- `.blazor-preview-render` default `min-height` bumped from 48px to 160px
  so small previews don't render in a cramped strip before the iframe's
  ResizeObserver reports actual content height
- `.blazor-preview-iframe` gets a `transition: height 200ms ease-out` so
  the post-boot resize is smooth instead of a jarring jump
- `.blazor-preview-render` ships with a subtle 24px grid background so the
  preview area is visually distinct from the surrounding page even while
  the WASM runtime is booting

All fallback values remain in place so standalone consumers without a
mokadocs theme still get reasonable defaults.

## [1.3.5] - 2026-04-08

### ✨ New — GitHub Pages deployment support

The `mokadocs-blazor-preview` plugin now produces output that deploys cleanly to
GitHub Pages, including project-page subpath deployments (e.g.
`username.github.io/Moka.Red/`).

- **Emits `.nojekyll`** at the `_site/` root. GitHub Pages runs Jekyll by default,
  which strips directories starting with `_` — which would wipe `_preview-wasm/`,
  `_preview-assemblies/`, `_framework/`, and `_content/` from the deployed site,
  leaving all preview iframes broken. The `.nojekyll` marker bypasses Jekyll
  entirely so every file ships verbatim.
- **Respects `--base-path` / `Build.BasePath` for the iframe `?assembly=...`
  query parameter.** The `ScribanTemplateEngine.RewriteContentLinks` regex
  already rewrites `src="/..."` attribute openings in page HTML automatically,
  but its pattern only matches the leading `/` after `src="` — it doesn't touch
  `/` characters inside query strings. The plugin now prefixes the assembly path
  (which lives inside the iframe `src` attribute value as `?assembly=/...`) with
  the base path itself, so GitHub Pages subpath deploys resolve the preview DLL
  correctly.

### 🐛 Fixed

- **`BuildCommand` CLI `--base-path` normalization**. When `--base-path /foo/`
  was passed with a trailing slash, the value was stored as-is in
  `BuildContext.Config.Build.BasePath`, causing `ScribanTemplateEngine.RewriteContentLinks`
  to produce `/foo//_preview-wasm/...` (double slash) when concatenating with
  absolute paths in page HTML. `BuildCommand` now trims/normalizes the same way
  `SiteConfigReader.NormalizBasePath` does for YAML-provided values: leading
  slash, no trailing slash (except when the value is literally `/`).

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
