---
title: Blazor Component Preview
order: 3
---

# Blazor Component Preview Plugin

The Blazor Preview plugin renders ` ```blazor-preview ` code blocks as **live,
interactive component previews** in your documentation site. Each block is compiled
at build time with Roslyn and hosted inside a lazy-loaded iframe that runs a
Blazor WebAssembly preview-host — so clicks, state updates, form inputs,
popovers, and dialogs all work, even on static hosts like GitHub Pages.

**Plugin ID:** `mokadocs-blazor-preview`

**Version:** 3.x (the 1.x/2.x SSR and inline-mount modes are removed — see the
migration note at the end of this page if you're coming from an older release).

---

## Quick start

All you need in `mokadocs.yaml`:

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      library: MyLibrary@1.2.3
```

That's it. On first build:

1. Plugin auto-discovers or **scaffolds** a `./preview-host/` Blazor WebAssembly
   project with a PackageReference to `Moka.Blazor.Repl.Host` and your library.
2. Plugin runs `dotnet publish` on that project (cached — skipped on subsequent
   builds when nothing has changed).
3. Plugin compiles every ` ```blazor-preview ` block with Roslyn against the
   preview-host's built bin, writing one `.dll` per block to
   `_site/_preview-assemblies/{sha}.dll`.
4. Plugin emits one `<iframe loading="lazy">` per preview block pointing at
   `/_preview-wasm/index.html?assembly=/_preview-assemblies/{sha}.dll`.

Subsequent builds only recompile changed preview blocks, and the `dotnet
publish` step is skipped entirely when no inputs changed.

---

## How interactive previews work

```
┌──────────────────────── mokadocs build ──────────────────────────┐
│                                                                   │
│  mokadocs.yaml: library: MyLibrary@1.2.3                         │
│                        ↓                                          │
│  preview-host/  ← auto-scaffolded if missing                     │
│    ├── DocsPreviewHost.csproj  (PackageReference MyLibrary +     │
│    │                            Moka.Blazor.Repl.Host)           │
│    ├── Program.cs              (RootComponents.Add<App>("#app")) │
│    └── wwwroot/index.html                                        │
│                        ↓ dotnet publish (cached, incremental)    │
│  _site/                                                           │
│    ├── _preview-wasm/          (copy of preview-host/wwwroot)    │
│    │   ├── index.html                                            │
│    │   ├── _framework/         (Blazor WASM runtime + your lib)  │
│    │   └── _content/           (your lib's scoped CSS bundles)   │
│    └── _preview-assemblies/                                       │
│        ├── a1b2c3d4e5f6.dll    (compiled from one preview block) │
│        └── 7890abcdef01.dll                                       │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
                            ↓
                ┌───── user visits page ─────┐
                │                              │
                │  _site/components/foo/      │
                │   index.html contains:      │
                │                              │
                │  <iframe loading="lazy"     │
                │   src="/_preview-wasm/      │
                │        index.html?assembly=│
                │        /_preview-assemblies│
                │        /a1b2c3d4.dll">     │
                │                              │
                │  iframe boots lazily when   │
                │  scrolled into view →       │
                │  Moka.Blazor.Repl.Host      │
                │  App.razor fetches the DLL, │
                │  Assembly.Load's it, and    │
                │  mounts the component via   │
                │  [JSInvokable] LoadAssembly │
                └──────────────────────────────┘
```

**Why iframes?** Each preview needs its own document root so portal-style
components (Dialog, Popover, Toast, Drawer) can attach to `document.body`
without conflicting with the doc page's own body. Lazy loading means only
previews the user actually scrolls to boot a Blazor runtime — one
`_framework/` download is shared across all iframes on a page via the HTTP
cache.

---

## Markdown syntax

Use a fenced code block with the `blazor-preview` info string:

````markdown
```blazor-preview
<MokaButton>Click me</MokaButton>
```
````

Any valid Razor is fine — `@code` blocks, `@using` directives, field
initializers, multiple components, dependency injection. The plugin compiles
each block as a standalone Razor file named `Preview.razor` with an entry
component type of `MokaRepl.Preview`.

### Multi-snippet state sharing

Previews on the same page can share `@code`-declared state. Snippet 1 defines
a field, snippet 2 references it:

````markdown
```blazor-preview
@code { record Person(string Name, int Age); List<Person> _people = [new("Ada", 36), new("Grace", 47)]; }
<div>@_people.Count people</div>
```

```blazor-preview
<MokaDataList Items="_people" />
```
````

The plugin uses a **two-pass compile strategy** for this: if the second
snippet fails with name-not-found errors only (`CS0103`, `CS0246`, `CS0012`)
and earlier snippets on the same page succeeded, the plugin prepends their
accumulated `@code` blocks and retries. Non-name-related errors (type
mismatches, wrong signatures) skip the retry to avoid masking real bugs.

### Entry component

By default each block renders `MokaRepl.Preview` (the type the Razor compiler
generates for `Preview.razor`). You can set `@code` fields on it but you
cannot currently override the entry type name — the plugin finds it
automatically from the compiled assembly metadata.

---

## Configuration

### Minimal (recommended)

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      library: MyLibrary@1.2.3
```

Everything else — `previewHost`, `references`, `usings` — is optional. The
plugin auto-scaffolds a preview-host project from a library-agnostic template
and derives its references list from the scaffolded project's `bin/Release/`.

### Full

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      # Required unless previewHost exists already
      library: MyLibrary@1.2.3

      # Optional — path to an existing preview-host project.
      # Auto-discovered when omitted (./preview-host/, ./docs-preview-host/, or
      # any subdirectory containing a Microsoft.NET.Sdk.BlazorWebAssembly csproj).
      previewHost: ./preview-host

      # Optional — namespaces added as global usings to every compiled preview.
      # Equivalent to putting @using statements in a _Imports.razor file.
      usings:
        - MyLibrary.Components
        - MyLibrary.Components.Forms
        - System.ComponentModel.DataAnnotations

      # Optional — additional Roslyn reference directories layered ON TOP of
      # the preview-host's bin. Same-named assemblies here OVERRIDE the
      # preview-host copies, so you can point at a local source build for
      # your in-development library while the preview-host uses a stable
      # NuGet version for everything else.
      references:
        - ../src/MyLibrary/bin/Debug/net10.0
```

### `library` (recommended)

Pinned NuGet package ID + optional version for your component library, in
the format `PackageId@Version` (e.g. `MyLibrary@1.2.3`) or just `PackageId`
(resolves to latest). The scaffolded preview-host's `.csproj` uses this to
build its own PackageReference to your library.

Required when no `./preview-host/` exists yet. You can also omit it if you
point `previewHost` at a pre-existing project.

### `previewHost`

Path to your docs preview-host Blazor WebAssembly project directory
(relative to `mokadocs.yaml`). When omitted the plugin auto-discovers one
of:

1. `./preview-host/`
2. `./docs-preview-host/`
3. Any immediate subdirectory containing a csproj using
   `Microsoft.NET.Sdk.BlazorWebAssembly`

If none exists and `library` is set, one is **scaffolded** at
`./preview-host/` from a generic template. The scaffold is a one-time
operation — mokadocs never overwrites an existing project.

### `usings`

Namespace strings added as C# global usings to every compiled preview
block. These are the `@using` directives your previews would otherwise need
at the top.

**Common system namespaces** (`System`, `System.Collections.Generic`,
`System.Threading.Tasks`, `Microsoft.AspNetCore.Components`) are added
by .NET's implicit global usings and don't need to be listed.

### `references`

Additional directories or DLL files to layer on top of the preview-host's
bin as Roslyn `MetadataReference` entries. Useful when you want the compiler
to resolve your in-development library against its local `bin/Debug/` output
while the preview-host still ships the stable NuGet version.

Same-named assemblies in these directories **override** the preview-host
copies — so a `MyLibrary.dll` here wins over the one in the preview-host's
bin. Framework assemblies (`System.*`, `Microsoft.AspNetCore.*`, etc.) are
filtered out automatically to avoid duplicate-reference conflicts.

---

## The preview-host project

The plugin owns a Blazor WebAssembly project that hosts the iframe runtime.
This is a **normal user-editable project** — mokadocs scaffolds it once and
never touches it again, so you can customize services, CSS, theme tokens,
and HTML head tags freely.

### Scaffolded files

The first `mokadocs build` after you add `library:` creates these files:

```
preview-host/
├── Directory.Build.props         ← empty shadow (isolates from parent repo)
├── Directory.Build.targets       ← empty shadow
├── Directory.Packages.props      ← empty shadow
├── DocsPreviewHost.csproj        ← references MyLibrary + Moka.Blazor.Repl.Host
├── Program.cs                    ← RootComponents.Add<App>("#app")
└── wwwroot/
    └── index.html                ← loads wasmPreview.js + blazor.webassembly.js
```

**Commit all of these to your repo.** They're source code you own, not
generated artifacts. The `bin/`, `obj/`, and `publish-output/` subdirectories
are fine to gitignore.

### Customizing — Program.cs

Between the comment markers, add your library's DI services so preview
snippets can `@inject` them:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moka.Blazor.Repl.Host;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ── Customize: register your library's services here ─────────────
builder.Services.AddMyLibrary();
builder.Services.AddSingleton<IMyAppService, MyAppService>();
// ──────────────────────────────────────────────────────────────────

await builder.Build().RunAsync();
```

### Customizing — wwwroot/index.html

Between the comment markers, add the CSS link tags for your library's
global stylesheets. The Blazor WASM SDK already auto-bundles every
referenced library's **scoped** CSS into `DocsPreviewHost.styles.css`, but
global tokens (`--my-color-primary`, etc.) need explicit `<link>` tags
pointing at `_content/<PackageId>/`:

```html
<!-- ── Customize: link your library's CSS here ──────────────── -->
<link rel="stylesheet" href="_content/MyLibrary/reset.css" />
<link rel="stylesheet" href="_content/MyLibrary/tokens.css" />
<link rel="stylesheet" href="_content/MyLibrary/my-library.css" />
<!-- ────────────────────────────────────────────────────────────── -->

<link rel="stylesheet" href="DocsPreviewHost.styles.css" />
```

You can also add custom fonts, data attributes for theme toggles, or
inline `<style>` blocks that control how the preview iframe body renders
its content.

---

## Output layout

After a build with `library: MyLibrary@1.2.3`:

```
_site/
├── .nojekyll                      ← emitted by the plugin so GitHub Pages
│                                     doesn't strip underscore-prefixed dirs
├── _preview-wasm/                 ← copy of preview-host/publish-output/net10.0/wwwroot
│   ├── index.html
│   ├── DocsPreviewHost.styles.css
│   ├── _framework/
│   │   ├── blazor.webassembly.js
│   │   ├── dotnet.js + dotnet.native.wasm
│   │   └── *.wasm                 ← compiled assemblies (MyLibrary + deps)
│   └── _content/
│       ├── MyLibrary/             ← scoped CSS bundles + static web assets
│       └── Moka.Blazor.Repl.Host/
│           └── wasmPreview.js     ← resize + postMessage bridge
└── _preview-assemblies/
    ├── a1b2c3d4e5f6.dll           ← one DLL per blazor-preview block
    └── 7890abcdef0123.dll
```

Each iframe loads from `/_preview-wasm/index.html?assembly=/_preview-assemblies/{hash}.dll`
and the `_framework/` files are HTTP-cached across iframes on the same page.

---

## Deploying to GitHub Pages

The plugin is designed to produce output that ships cleanly to GitHub Pages,
including project-page subpath deploys.

### `.nojekyll`

The plugin automatically writes an empty `.nojekyll` file at the site root.
**Without this**, Jekyll (GitHub Pages' default processor) strips every
directory starting with `_` — including `_preview-wasm/`,
`_preview-assemblies/`, `_framework/`, and `_content/`. The preview system
would silently disappear from the deployed site.

### Subpath deploys (`username.github.io/my-project/`)

When building for a project page, pass `--base-path /my-project/`:

```bash
mokadocs build --base-path /my-project/
```

The plugin prefixes iframe `src="/…"` attributes and `?assembly=/…` query
params with the base path so both the iframe location and its dynamic
assembly load resolve correctly on the subpath. Root-deployed sites
(`username.github.io`) work with a plain `mokadocs build`.

### GitHub Actions workflow

```yaml
name: Deploy Docs

on:
  push:
    branches: [master]

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - name: Build library (Roslyn references)
        run: dotnet build MyLibrary.slnx -c Release

      - name: Install mokadocs
        run: dotnet tool install -g mokadocs

      - name: Build docs (scaffolds + publishes preview-host automatically)
        run: mokadocs build --base-path /my-project/
        working-directory: docs

      - uses: actions/upload-pages-artifact@v3
        with: { path: docs/_site }

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment: { name: github-pages, url: "${{ steps.d.outputs.page_url }}" }
    steps:
      - id: d
        uses: actions/deploy-pages@v4
```

The `mokadocs build` step transparently runs `dotnet publish` on the
scaffolded preview-host, populates `_site/_preview-wasm/`, and compiles
every preview block — no extra CI steps required.

---

## Troubleshooting

### Previews show up as static buttons with no interactivity

The iframes are loading but the Blazor runtime isn't mounting components.
Most common causes:

1. **Missing `.nojekyll`** — if you're on GitHub Pages, confirm
   `_site/.nojekyll` exists. The plugin emits it automatically but a
   post-build step could be stripping it.
2. **IL trimming over the preview-host** — `Moka.Blazor.Repl.Host` ships a
   `build/Moka.Blazor.Repl.Host.targets` file that sets
   `PublishTrimmed=false` unconditionally. If your preview-host overrides
   this by setting `PublishTrimmed=true` in its own csproj, the trimmer
   will strip `RouteAttribute` and `CreateInferredEventCallback`, and
   previews will throw `TypeLoadException` / `MissingMethodException` in
   the browser console. Remove the override.

### "Dynamic root components have not been enabled in this application"

The preview-host is trying to use `Blazor.rootComponents.add()` but the
shared host library isn't set up for it. This plugin does **not** use
dynamic root components — each iframe uses a static `RootComponents.Add<App>("#app")`
call with a `[JSInvokable] LoadAssembly` method. If you see this error,
your preview-host's `Program.cs` was hand-authored with
`RegisterForJavaScript<T>("…")` from an older tutorial. Replace it with
the scaffolded template's `RootComponents.Add<App>("#app")` pattern.

### "NETSDK1082: no runtime pack for Microsoft.AspNetCore.App"

Your component library's NuGet package declares `<FrameworkReference
Include="Microsoft.AspNetCore.App" />` in its nuspec, which transitively
propagates to the Blazor WebAssembly preview-host — but there is no
`Microsoft.AspNetCore.App` runtime pack for the `browser-wasm` RID.

Fix this in the **library project**, not the preview-host: use a
`PackageReference Include="Microsoft.AspNetCore.Components.Web"` instead of
the framework reference, and strip the implicit framework reference at
pack time via a `BeforeTargets="ProcessFrameworkReferences"` MSBuild
target. See Moka.Red's `Directory.Build.targets` for a working example.

### Preview compile errors

Preview compile errors are shown inline in place of the iframe. The source
code tab still works so you can see what the user entered. Common gotchas:

- **Generic type inference** — Razor's generated code for components with
  `@typeparam TValue where TValue : …` may reference a constraint type
  that isn't in scope. Workaround: specify the type explicitly with
  `<MokaNumericField TValue="int" @bind-Value="_qty" />`.
- **Missing namespaces** — add the namespace to `usings` in your plugin
  options instead of putting `@using` directives in the preview block
  itself. Plugin global usings apply to every block without cluttering
  each snippet.

### Iframe keeps growing in height

The `wasmPreview.js` bridge reports content size to the parent via
postMessage, and `Moka.Blazor.Repl.Host` v1.3.5+ guards against monotonic
growth by measuring the `#app` element instead of `documentElement.scrollHeight`,
using a 2px change threshold, and rate-limiting to 20 posts/sec. If you're
hitting the grow loop on an older version, upgrade `Moka.Blazor.Repl.Host`
and run `dotnet publish` on the preview-host again.

---

## Limitations

- **One Blazor WebAssembly runtime per page** — each visible iframe boots
  its own runtime. With `loading="lazy"`, only previews the user actually
  scrolls to will boot, and all iframes on a page share the same
  `_framework/` files via the HTTP cache.
- **No cross-iframe communication** — each preview is isolated. State
  sharing between previews on the same page happens only at compile time
  via the two-pass `@code` inheritance described above, not at runtime.
- **Compile-time source only** — `blazor-preview` blocks are compiled
  during `mokadocs build`, not at runtime. Users can't edit the code in
  the browser (that's what the REPL plugin is for).

---

## Migration from v1.x / v2.x

The plugin was rewritten in v3.0 with a new yaml shape. If you're coming
from the old `mode: wasm | ssr` / `wasmAppPath` / `stylesheets` schema:

| Old option | New equivalent |
|---|---|
| `mode: wasm` (default) | The only mode — removed, iframes always used |
| `mode: ssr` | Removed — use a proper static site generator if you need non-interactive HTML |
| `wasmAppPath: …` | Removed — use `previewHost: …` instead, pointing at a real Blazor WASM csproj |
| `stylesheets: […]` | Moved into the preview-host's `wwwroot/index.html` `<link>` tags |
| `references: […]` (required) | Optional and additive — the plugin derives refs from the preview-host's bin automatically |
| (new) `library: PackageId@Version` | Required when auto-scaffolding |

The simplest migration is to **delete your old options and set just
`library: …`** — the plugin will scaffold a new preview-host for you. If
you had custom logic in your old wasmAppPath or stylesheets, move it into
`preview-host/Program.cs` and `preview-host/wwwroot/index.html` after the
scaffold runs.
