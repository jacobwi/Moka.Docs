---
title: Blazor Component Preview
order: 3
---

# Blazor Component Preview Plugin

The Blazor Preview plugin turns ` ```blazor-preview ` code blocks into live
component previews. The build compiles each block with Roslyn, and the page
shows it in a lazy-loaded iframe that runs a Blazor WebAssembly preview host.
Previews run entirely in the browser, so they also work on static hosts such
as GitHub Pages.

**Plugin ID:** `mokadocs-blazor-preview`

**Version:** 3.x (the 1.x/2.x SSR and inline-mount modes are removed; see the
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

The plugin does nothing on builds where no page has a preview block. When a
build finds one, the plugin:

1. Finds a preview-host project, or **scaffolds** `./preview-host/`: a Blazor
   WebAssembly project with PackageReferences to `Moka.Blazor.Repl.Host` and
   your library.
2. Runs `dotnet publish -c Release -f net10.0` on that project, unless its
   publish output is newer than all of its files (files under `bin/`, `obj/`
   and `publish-output/` don't count).
3. Copies the published `wwwroot` to `_site/_preview-wasm/`.
4. Compiles every ` ```blazor-preview ` block with Roslyn against the preview
   host's `bin/Release` assemblies and writes one `.dll` per block to
   `_site/_preview-assemblies/{hash}.dll`.
5. Replaces each block with an `<iframe loading="lazy">` pointing at
   `/_preview-wasm/index.html?assembly=/_preview-assemblies/{hash}.dll&entry=...`.

Once steps 1 and 2 succeed, the plugin doesn't repeat them for the rest of the
process, so `mokadocs serve` and `mokadocs build --watch` don't republish the
preview host after you edit it. Restart the command instead. Step 4 runs on
every build: there is no compile cache, so every block is recompiled each time.

Requirements:

- A .NET 10 SDK on `PATH`, because the plugin publishes with `-f net10.0`
- A preview host that targets `net10.0`

`mokadocs validate` and `mokadocs doctor` run plugins too, so they can also
scaffold and publish the preview host.

---

## How interactive previews work

```
┌──────────────────────────── mokadocs build ────────────────────────────┐
│                                                                        │
│  mokadocs.yaml: library: MyLibrary@1.2.3                               │
│                          ↓                                             │
│  preview-host/  ← scaffolded if missing                                │
│    ├── DocsPreviewHost.csproj  (MyLibrary + Moka.Blazor.Repl.Host)     │
│    ├── Program.cs              (RootComponents.Add<App>("#app"))       │
│    └── wwwroot/index.html                                              │
│                          ↓ dotnet publish -c Release -f net10.0        │
│  preview-host/publish-output/net10.0/wwwroot/                          │
│                          ↓ copied into the site                        │
│  _site/                                                                │
│    ├── _preview-wasm/          (the published wwwroot)                 │
│    │   ├── index.html                                                  │
│    │   ├── _framework/         (Blazor WASM runtime + your library)    │
│    │   └── _content/           (static web assets, scoped CSS)         │
│    └── _preview-assemblies/                                            │
│        ├── a1b2c3d4e5f6.dll    (compiled from one preview block)       │
│        └── 7890abcdef01.dll                                            │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                                    ↓
              ┌────────── reader opens the page ───────────┐
              │                                            │
              │  <iframe loading="lazy"                    │
              │    src="/_preview-wasm/index.html          │
              │      ?assembly=/_preview-assemblies/       │
              │      a1b2c3d4e5f6.dll&entry=...">          │
              │                                            │
              │  The iframe boots when it scrolls into     │
              │  view. App (from Moka.Blazor.Repl.Host)    │
              │  reads the assembly and entry query        │
              │  parameters, fetches the DLL, loads it     │
              │  and renders the entry component.          │
              │                                            │
              └────────────────────────────────────────────┘
```

**Why iframes?** Each preview gets its own document, so components that render
into `document.body` (dialogs, popovers, toasts, drawers) stay inside their
preview instead of spilling into the docs page. With `loading="lazy"`, a
preview doesn't boot the Blazor runtime until the reader scrolls to it. All
iframes on a page load the same `_framework/` files, which the browser can
serve from its cache. A script on the page resizes each iframe to the height
its preview reports.

The build also renders each compiled component to static HTML inside a
`<noscript>` element, for readers without JavaScript. Apart from logging, that
render only has stub `IJSRuntime` and `NavigationManager` services. When it throws (for
example, because the component injects another service), the build reports a
warning and the fallback shows the error message. The iframe is not affected.

---

## Markdown syntax

Use a fenced code block with the `blazor-preview` info string
(`razor-preview` also works, in any letter case):

````markdown
```blazor-preview
<MokaButton>Click me</MokaButton>
```
````

Each block is compiled on its own as a Razor component file named
`Preview.razor`, so it can hold markup, several components, `@code` blocks,
and `@using` or `@inject` directives. Services used with `@inject` must be
registered in the preview host's `Program.cs`. On the page, each preview gets
**Preview** and **Source** tabs.

### Multi-snippet state sharing

A block can use fields and types declared in the `@code` blocks of earlier
blocks on the same page. Snippet 1 defines a field, snippet 2 references it:

````markdown
```blazor-preview
@code { record Person(string Name, int Age); List<Person> _people = [new("Ada", 36), new("Grace", 47)]; }
<div>@_people.Count people</div>
```

```blazor-preview
<MokaDataList Items="_people" />
```
````

This works through a compile retry. If a block fails to compile and every
error is `CS0103`, `CS0246` or `CS0012` (names or types that can't be found),
the plugin prepends the `@code` blocks of all earlier blocks on the page,
whether or not those blocks compiled, and compiles again. It keeps the retry
result when it compiles or has fewer errors. Any other error skips the retry,
so type mismatches and wrong signatures are reported as written. At runtime,
each preview still runs its own copy of the code in its own iframe.

### Entry component

There is no option to choose which component a block renders. The plugin
passes the type name the compiler reports (`DocsPreview.Preview` for these
blocks) to the iframe as `entry`, and falls back to `MokaRepl.Preview` if the
compiler reports none.

---

## Configuration

### Minimal (recommended)

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      library: MyLibrary@1.2.3
```

Everything else (`previewHost`, `references`, `usings`) is optional. The
plugin scaffolds a preview-host project from a library-agnostic template and
takes its compile references from that project's `bin/Release/` output.

### Full

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      # Needed only when there is no preview-host project to find or point at
      library: MyLibrary@1.2.3

      # Optional - path to an existing preview-host project, relative to mokadocs.yaml.
      # Auto-discovered when omitted (./preview-host/, ./docs-preview-host/, or
      # any immediate subdirectory containing a Microsoft.NET.Sdk.BlazorWebAssembly csproj).
      previewHost: ./preview-host

      # Optional - namespaces added as global usings to every compiled preview.
      # Same effect as @using lines at the top of every block.
      usings:
        - MyLibrary.Components
        - MyLibrary.Components.Forms
        - System.ComponentModel.DataAnnotations

      # Optional - additional Roslyn reference directories or DLL files layered
      # ON TOP of the preview-host's bin. Same-named assemblies here OVERRIDE the
      # preview-host copies, so you can compile against a local build of your
      # in-development library while the preview-host uses a stable NuGet version.
      references:
        - ../src/MyLibrary/bin/Debug/net10.0
```

### `library` (recommended)

NuGet package ID and optional version of your component library, written as
`PackageId@Version` (e.g. `MyLibrary@1.2.3`) or just `PackageId` (latest stable
version). The scaffolded preview host's `.csproj` uses it for the
PackageReference to your library.

The plugin reads `library` only when it scaffolds, which happens when the
preview-host directory has no Blazor WebAssembly project. In that case
`library` is required, and without it the build fails with an error. Once the
project exists, change the library version in its `.csproj`.

### `previewHost`

Path to your docs preview-host Blazor WebAssembly project directory
(relative to `mokadocs.yaml`). When omitted, the plugin uses the first of:

1. `./preview-host/`
2. `./docs-preview-host/`
3. Any immediate subdirectory of the `mokadocs.yaml` folder

A directory only counts when a `.csproj` at its top level uses
`Microsoft.NET.Sdk.BlazorWebAssembly`. If the directory has no such project
and `library` is set, the plugin **scaffolds** one there (`./preview-host/`
when `previewHost` is not set). The scaffold only writes files that don't
exist yet, so mokadocs never overwrites an existing project.

The project must target `net10.0`: the plugin publishes it with
`-f net10.0` into `publish-output/net10.0/`.

### `usings`

Namespaces added as C# global usings to every compiled preview block, so the
blocks don't need `@using` lines for them.

You don't need to list the namespaces the compiler already imports: `System`,
`System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`,
`Microsoft.AspNetCore.Components`, `Microsoft.AspNetCore.Components.Forms` and
`Microsoft.AspNetCore.Components.Web`.

### `references`

Additional directories or DLL files to add on top of the preview host's bin as
Roslyn `MetadataReference` entries. Paths are relative to `mokadocs.yaml`, and
directories are scanned for `*.dll` files at their top level only. Useful when
you want the compiler to resolve your in-development library against its local
`bin/Debug/` output while the preview host still ships the stable NuGet
version.

Same-named assemblies in these directories **override** the preview-host
copies, so a `MyLibrary.dll` here wins over the one in the preview host's bin.
Assemblies that belong to the .NET shared framework the tool runs on are
skipped, since the compiler already references them.

References only change compilation and the static `<noscript>` render. The
iframe still runs the library version published with the preview host, so a
block that uses APIs missing from that version compiles but can fail in the
browser.

---

## The preview-host project

The plugin uses a Blazor WebAssembly project to host the iframe runtime. It is
a normal project that you own: mokadocs scaffolds the missing files once and
never overwrites them, so you can customize services, CSS, theme tokens, and
HTML head tags freely. The plugin does publish it, which writes `bin/`, `obj/`
and `publish-output/`.

### Scaffolded files

The first build that finds a preview block, with `library:` set and no existing
project, creates these files:

```
preview-host/
├── Directory.Build.props         ← empty shadow (isolates from parent repo)
├── Directory.Build.targets       ← empty shadow
├── Directory.Packages.props      ← empty shadow
├── DocsPreviewHost.csproj        ← net10.0, references MyLibrary + Moka.Blazor.Repl.Host 1.3.5
├── Program.cs                    ← RootComponents.Add<App>("#app")
└── wwwroot/
    └── index.html                ← loads wasmPreview.js + blazor.webassembly.js
```

**Commit all of these to your repo.** They're source code you own, not
generated artifacts. The `bin/`, `obj/`, and `publish-output/` subdirectories
are fine to gitignore.

### Customizing - Program.cs

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

### Customizing - wwwroot/index.html

Between the comment markers, add the CSS link tags for your library's
global stylesheets. The Blazor WASM SDK already bundles every referenced
library's **scoped** CSS into `DocsPreviewHost.styles.css`, but global tokens
(`--my-color-primary`, etc.) need explicit `<link>` tags pointing at
`_content/<PackageId>/`:

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
├── .nojekyll                      ← written for every site, see below
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
    └── 7890abcdef01.dll
```

Each iframe loads `/_preview-wasm/index.html?assembly=/_preview-assemblies/{hash}.dll&entry={type}`.
The hash is the first 12 hex characters of the SHA-256 of the block's source.

If the preview host also has a `publish-output/wwwroot/` folder, the plugin
copies that one instead of `publish-output/net10.0/wwwroot/`.

---

## `mokadocs serve` endpoint

While `mokadocs-blazor-preview` is declared, `mokadocs serve` also accepts
`POST /api/blazor/preview` with the JSON body `{"source": "..."}`. It compiles
the component in the dev server process and returns server-rendered HTML as
`{"html": "...", "error": "..."}`. Nothing in the generated pages calls it.

The endpoint follows the same rules as the [REPL endpoint](/plugins/repl#security):
it needs `Content-Type: application/json`, rejects browser requests from other
origins with a 403, and answers 503 when the plugin isn't declared. The
component code runs unsandboxed in the dev server process.

---

## Deploying to GitHub Pages

The output is static files, so it deploys to GitHub Pages, including
project-page subpath deploys.

### `.nojekyll`

Builds write an empty `.nojekyll` file at the site root for every site, whether
or not it uses this plugin. **Without it**, Jekyll (GitHub Pages' default
processor) strips every directory starting with `_`, including
`_preview-wasm/`, `_preview-assemblies/`, `_framework/`, and `_content/`, and
the previews silently disappear from the deployed site.

### Subpath deploys (`username.github.io/my-project/`)

When building for a project page, pass `--base-path /my-project/`:

```bash
mokadocs build --base-path /my-project/
```

Both the iframe `src` and its `?assembly=` parameter carry the base path: the
template engine prefixes root-relative `src` attributes, and the plugin adds
the base path to the `assembly` parameter itself. Root-deployed sites
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

      - name: Build library (only needed if references point at local builds)
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

The `mokadocs build` step scaffolds the preview host if it isn't in the repo,
publishes it, copies it into `_site/_preview-wasm/`, and compiles every
preview block, so the workflow needs no separate `dotnet publish` step.

---

## Troubleshooting

### Previews show up as static buttons with no interactivity

The iframes are loading but the Blazor runtime isn't mounting components.
Common causes:

1. **Missing `.nojekyll`** - if you're on GitHub Pages, confirm
   `_site/.nojekyll` exists. The build writes it for every site, but a later
   step in your pipeline could remove it.
2. **IL trimming over the preview-host** - `Moka.Blazor.Repl.Host` ships a
   `build/Moka.Blazor.Repl.Host.targets` file that sets `PublishTrimmed=false`,
   because the trimmer strips types and methods that dynamically loaded
   previews need, such as `RouteAttribute` and `CreateInferredEventCallback`.
   The symptom is `TypeLoadException` or `MissingMethodException` in the
   browser console. If you see it, check that nothing in your build turns
   trimming back on for the preview host.

### "Dynamic root components have not been enabled in this application"

This plugin does **not** use Blazor's dynamic root components. The scaffolded
`Program.cs` mounts `App` from `Moka.Blazor.Repl.Host` as a static root with
`RootComponents.Add<App>("#app")`, and `App` loads the preview assembly
itself. If you see this error, your preview host's `Program.cs` was probably
hand-written with `RegisterForJavaScript<T>("...")` from an older tutorial.
Replace it with the scaffolded `RootComponents.Add<App>("#app")` pattern.

### "NETSDK1082: no runtime pack for Microsoft.AspNetCore.App"

Your component library's NuGet package declares `<FrameworkReference
Include="Microsoft.AspNetCore.App" />` in its nuspec, which propagates
transitively to the Blazor WebAssembly preview host. There is no
`Microsoft.AspNetCore.App` runtime pack for the `browser-wasm` RID.

Fix this in the **library project**, not the preview-host: use a
`PackageReference Include="Microsoft.AspNetCore.Components.Web"` instead of
the framework reference, and strip the implicit framework reference at
pack time with an MSBuild target that runs before `ProcessFrameworkReferences`.
See Moka.Red's `Directory.Build.targets` for a working example.

### Preview compile errors

Compile errors replace the iframe on the page, one per line, in the form
`[CS0103] /Preview.razor(1,25): The name '_people' does not exist in the current context`.
The build also reports them as warnings. The Source tab still shows the
block's code. Common gotchas:

- **Generic type inference** - Razor's generated code for components with
  `@typeparam TValue where TValue : ...` may reference a constraint type
  that isn't in scope. Workaround: specify the type explicitly with
  `<MokaNumericField TValue="int" @bind-Value="_qty" />`.
- **Missing namespaces** - add the namespace to `usings` in your plugin
  options instead of putting `@using` directives in the preview block
  itself. Plugin global usings apply to every block without cluttering
  each snippet.

### Iframe keeps growing in height

The page script sets each iframe's height to the size its preview reports
through `postMessage`, plus 16px. `Moka.Blazor.Repl.Host` 1.3.5, the version the
scaffold references, avoids a growth loop: it measures the `#app` element
instead of `documentElement.scrollHeight`, ignores changes under 2px, and sends
at most 20 size updates per second. If an older preview host keeps growing,
raise its `Moka.Blazor.Repl.Host` version in the `.csproj`. The next build
republishes because the project file changed (restart `mokadocs serve` first
if it's running).

---

## Limitations

- **One Blazor WebAssembly runtime per preview** - each iframe that boots
  runs its own runtime. With `loading="lazy"`, only previews the reader
  scrolls to will boot.
- **No cross-iframe communication** - each preview is isolated. Sharing
  between previews on the same page happens only at compile time, through
  the `@code` retry described above, not at runtime.
- **Read-only code** - `blazor-preview` blocks are compiled when the site
  builds. Readers can't edit them in the browser, and REPL blocks aren't
  editable either.
- **No compile cache** - every block is recompiled on every build, so build
  time grows with the number of blocks.
- **.NET 10 only** - the preview host must target `net10.0`, and publishing it
  needs a .NET 10 SDK.
- **Preview-host edits need a restart** - `mokadocs serve` and
  `mokadocs build --watch` check whether to republish only once per process.

---

## Migration from v1.x / v2.x

The plugin was rewritten in v3.0 with a new yaml shape. If you're coming
from the old `mode: wasm | ssr` / `wasmAppPath` / `stylesheets` schema:

| Old option | New equivalent |
|---|---|
| `mode: wasm` (default) | The only mode - removed, iframes always used |
| `mode: ssr` | Removed - each preview still includes a static `<noscript>` render |
| `wasmAppPath: …` | Removed - use `previewHost: …` instead, pointing at a real Blazor WASM csproj |
| `stylesheets: […]` | Moved into the preview-host's `wwwroot/index.html` `<link>` tags |
| `references: […]` (required) | Optional and additive - the plugin derives refs from the preview-host's bin automatically |
| (new) `library: PackageId@Version` | Required when auto-scaffolding |

The simplest migration is to **delete your old options and set only
`library: …`**. The plugin then scaffolds a new preview host for you. If
you had custom logic in your old wasmAppPath or stylesheets, move it into
`preview-host/Program.cs` and `preview-host/wwwroot/index.html` after the
scaffold runs.
