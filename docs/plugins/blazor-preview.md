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
```

That's enough for blocks that use plain Razor and the built-in Blazor
components. To preview components from your own library, name its package:

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      library: MyLibrary@1.2.3
```

The plugin does nothing on builds where no page has a preview block. When a
build finds one, the plugin:

1. Finds a preview-host project, or **scaffolds** one: a Blazor WebAssembly
   project with a PackageReference to `Moka.Blazor.Repl.Host`. With `library`
   set, the project goes in `./preview-host/` and also references your library.
   Without it, the project goes in `./.mokadocs/preview-host/`.
2. Runs `dotnet publish -c Release -f net10.0` on that project, unless it was
   published after the last change to its files (files under `bin/`, `obj/`
   and `publish-output/` don't count) and to the source files of the projects
   it references with `ProjectReference`. Before publishing it empties
   `publish-output/net10.0/`, so files from an earlier publish don't ship. A
   successful publish writes `publish-output/net10.0/.mokadocs-published`,
   which records when it started. A publish that runs past `publishTimeout`
   (10 minutes by default) is stopped along with every process it started,
   and the build reports an error.
3. Copies the published `wwwroot` to `_site/_preview-wasm/`, adding a
   Content-Security-Policy and a message guard to its pages (see
   [Security](#security)).
4. Compiles every ` ```blazor-preview ` block with Roslyn against the preview
   host's `bin/Release` assemblies and writes one assembly per block to
   `_site/_preview-assemblies/{hash}.bin`.
5. Replaces each block with an `<iframe loading="lazy">` pointing at
   `/_preview-wasm/index.html?assembly=/_preview-assemblies/{hash}.bin&entry=...`.

Once steps 1 and 2 succeed, the plugin doesn't repeat them for the rest of the
process, so `mokadocs serve` and `mokadocs build --watch` don't republish the
preview host after you edit it. Restart the command instead. Step 4 runs on
every build: there is no compile cache, so every block is recompiled each time.

Requirements:

- A .NET 10 SDK on `PATH`, because the plugin publishes with `-f net10.0`
- A preview host that targets `net10.0`

`mokadocs validate` and `mokadocs doctor` run plugins too, so they can also
scaffold and publish the preview host. Set [`publishHost`](#publishhost) to
`false` to keep the plugin from scaffolding or publishing anything.

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
│    │   ├── index.html          (+ CSP and message guard)               │
│    │   ├── _framework/         (Blazor WASM runtime + your library)    │
│    │   └── _content/           (static web assets, scoped CSS)         │
│    └── _preview-assemblies/                                            │
│        ├── 3f9a…c41e.bin       (compiled from one preview block)       │
│        └── 81d0…77ab.bin                                               │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                                    ↓
              ┌────────── reader opens the page ───────────┐
              │                                            │
              │  <iframe loading="lazy"                    │
              │    src="/_preview-wasm/index.html          │
              │      ?assembly=/_preview-assemblies/       │
              │      3f9a…c41e.bin&entry=...">             │
              │                                            │
              │  The iframe boots when it scrolls into     │
              │  view. App (from Moka.Blazor.Repl.Host)    │
              │  reads the assembly and entry query        │
              │  parameters, fetches the assembly, loads   │
              │  it and renders the entry component.       │
              │                                            │
              └────────────────────────────────────────────┘
```

**Why iframes?** Each preview gets its own document, so components that render
into `document.body` (dialogs, popovers, toasts, drawers) stay inside their
preview instead of spilling into the docs page. With `loading="lazy"`, a
preview doesn't boot the Blazor runtime until the reader scrolls to it. A
script on the page resizes each iframe to the height its preview reports.

All iframes on a page load the same `_framework/` files. Whether the browser
takes the second and later copies from its cache depends on the server:
`mokadocs serve` sends `no-store`, so under serve every preview downloads the
runtime again, while the ASP.NET Core host lets browsers keep the
fingerprinted `_framework/` files, the `?v=` versioned files and the
`_preview-assemblies/` files for good. Static hosts use their own cache
rules (GitHub Pages caches for 10 minutes).

The build also renders each compiled component to static HTML inside a
`<noscript>` element, for readers without JavaScript. Apart from logging, that
render only has stub `IJSRuntime` and `NavigationManager` services. When it throws (for
example, because the component injects another service), the build reports a
warning and the fallback shows the error message. The iframe is not affected.

The render waits up to `renderTimeout` (5 seconds by default) for the
component's async work to finish. A component that is still rendering after
that, such as a timer that calls `StateHasChanged` forever, keeps the HTML it
had at that point, and the build reports a warning that names the page. One
that blocks a lifecycle method gets an empty fallback and the same warning.
Either way the build carries on.

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

### Minimal

```yaml
plugins:
  - name: mokadocs-blazor-preview
```

Every option is optional. The plugin scaffolds a preview-host project from a
library-agnostic template and takes its compile references from that
project's `bin/Release/` output. Add `library` when the blocks use components
from your own package.

### Full

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      # Read only when the plugin scaffolds a preview-host project
      library: MyLibrary@1.2.3

      # Optional - path to an existing preview-host project, relative to mokadocs.yaml.
      # Auto-discovered when omitted (./preview-host/, ./docs-preview-host/, or
      # any immediate subdirectory containing a Microsoft.NET.Sdk.BlazorWebAssembly csproj).
      previewHost: ./preview-host

      # Optional - false: never scaffold or run dotnet publish; use what previewHost
      # already holds, or show only static renders (default true)
      publishHost: true

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

      # Optional - seconds dotnet publish of the preview host may take (default 600)
      publishTimeout: 600

      # Optional - seconds the static <noscript> render of one preview may take (default 5)
      renderTimeout: 5
```

### `library`

NuGet package ID and optional version of your component library, written as
`PackageId@Version` (e.g. `MyLibrary@1.2.3`) or just `PackageId` (latest stable
version). The scaffolded preview host's `.csproj` uses it for the
PackageReference to your library.

The plugin reads `library` only when it scaffolds, which happens when the
preview-host directory has no Blazor WebAssembly project. Without `library`,
the scaffolded project references only `Moka.Blazor.Repl.Host`, which is
enough for plain Razor, and it goes in `./.mokadocs/preview-host/` unless
`previewHost` names another folder. Once the project exists, change the
library version in its `.csproj`.

The value must be a NuGet package id (letters, digits, and `.`, `-` or `_`
between them) with an optional version, floating version (`1.*`) or version
range (`[1.0,2.0)`) after the `@`. Anything else is ignored with a warning, so
it never reaches the generated project file.

### `previewHost`

Path to your docs preview-host Blazor WebAssembly project directory
(relative to `mokadocs.yaml`). When omitted, the plugin uses the first of:

1. `./preview-host/`
2. `./docs-preview-host/`
3. Any immediate subdirectory of the `mokadocs.yaml` folder

A directory only counts when a `.csproj` at its top level uses
`Microsoft.NET.Sdk.BlazorWebAssembly`. When none of them has such a project,
the plugin picks `./preview-host/` if `library` is set and
`./.mokadocs/preview-host/` if it isn't. If the chosen directory, or the
`previewHost` directory, has no project yet, the plugin **scaffolds** one
there. The scaffold only writes files that don't exist yet, so mokadocs never
overwrites an existing project.

The project must target `net10.0`: the plugin publishes it with
`-f net10.0` into `publish-output/net10.0/` and copies that folder's
`wwwroot`.

### `publishHost`

Whether the plugin may scaffold and publish the preview host. Default: `true`.

With `false`, the plugin never writes a project and never runs
`dotnet publish`. It looks for a published app under `previewHost`: the
newest of `publish-output/wwwroot/`, `publish-output/{tfm}/wwwroot/`, the
`previewHost` folder itself and its `wwwroot/`, whichever has
`_framework/blazor.webassembly.js` (or its fingerprinted
`blazor.webassembly.{fingerprint}.js`). So `previewHost` can name the project
you publish yourself, or a copy of its published output. Compile references
still come from the project's `bin/Release/`, plus `references`.

Without `previewHost`, or when it holds no published app, the build reports a
warning once and each block shows its static render (the same HTML the
`<noscript>` fallback holds) instead of an iframe. The ASP.NET Core host turns
`publishHost` off outside the Development environment, so an app never runs
`dotnet publish` in production.

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      publishHost: false
      previewHost: ./preview-host   # published in CI with dotnet publish -o publish-output/net10.0
```

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
skipped, since the compiler already references them. The plugin asks the
runtime where it would load each name from, so this works wherever .NET is
installed (Program Files, `~/.dotnet`, snap, Homebrew or a custom
`DOTNET_ROOT`).

References only change compilation and the static `<noscript>` render. The
iframe still runs the library version published with the preview host, so a
block that uses APIs missing from that version compiles but can fail in the
browser. For the same reason, rebuilding a `references` folder doesn't
republish the preview host; a library the host itself references with
`ProjectReference` does.

### `publishTimeout`

Seconds the `dotnet publish` of the preview host may run. Default: `600`. The
first publish restores the WebAssembly packages, so raise it on a slow
connection. Past the limit the plugin stops `dotnet publish` and every process
it started, and reports an error. Cancelling the build (Ctrl+C) stops it the
same way.

### `renderTimeout`

Seconds the static `<noscript>` render of one preview may take. Default: `5`.
See [How interactive previews work](#how-interactive-previews-work) for what
happens to a component that takes longer.

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
├── DocsPreviewHost.csproj        ← net10.0, references MyLibrary + Moka.Blazor.Repl.Host 1.3.6
├── Program.cs                    ← RootComponents.Add<App>("#app")
└── wwwroot/
    └── index.html                ← loads wasmPreview.js + blazor.webassembly.js
```

The project sets `OverrideHtmlAssetPlaceholders`, and `index.html` has the
placeholders .NET 10 fills in when it publishes: `<link rel="preload"
id="webassembly" />` and `<script type="importmap"></script>` in the head, and
`_framework/blazor.webassembly#[.{fingerprint}].js` as the Blazor script. The
published framework files then carry a content hash in their names and are
preloaded, so a browser or CDN can keep them until they change. See
[Fingerprint client-side static assets](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/static-files#fingerprint-client-side-static-assets-in-standalone-blazor-webassembly-apps)
in the Blazor docs.

Hosts scaffolded by earlier versions don't have this, and mokadocs never
rewrites them. `mokadocs validate` and `mokadocs doctor` warn about a published
host without fingerprinting; add the property and the three placeholders to
upgrade it.

**Commit all of these to your repo.** They're source code you own, not
generated artifacts. The `bin/`, `obj/`, and `publish-output/` subdirectories
are fine to gitignore.

Without `library:`, the same files go in `.mokadocs/preview-host/`, and the
`.csproj` has no library reference. That folder sits with the build cache,
which [belongs in `.gitignore`](/getting-started/project-structure#output-directory),
and `mokadocs clean` deletes it, so the next build scaffolds and publishes the
host again. To customize a host created this way, move the folder to
`./preview-host/` and commit it.

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

The SDK only writes `DocsPreviewHost.styles.css` when the host or a library it references has scoped (`.razor.css`) styles. When the publish has none, mokadocs leaves the link out of the copy in the site, so previews don't request a file that isn't there.

You can also add custom fonts, data attributes for theme toggles, or
inline `<style>` blocks that control how the preview iframe body renders
its content. The page gets a Content-Security-Policy when it's copied into
the site, so keep to what it allows (see [Security](#security)): scripts and
stylesheets from the docs origin or from origins the page links to directly,
inline `<script>` blocks, no inline event handlers such as `onclick="..."`,
and requests (`fetch`, `HttpClient`) to the docs origin only.

---

## Output layout

After a build with `library: MyLibrary@1.2.3`:

```
_site/
├── .nojekyll                      ← written for every site, see below
├── .gitattributes                 ← *.js binary, see below
├── _preview-wasm/                 ← copy of preview-host/publish-output/net10.0/wwwroot
│   ├── index.html                 ← with the CSP and message guard added
│   ├── index.html.br / .gz        ← compressed again from the new index.html
│   ├── DocsPreviewHost.styles.css ← only when there are scoped styles
│   ├── _framework/
│   │   ├── blazor.webassembly.{fingerprint}.js
│   │   ├── dotnet.{fingerprint}.js + dotnet.native.{fingerprint}.wasm
│   │   └── *.{fingerprint}.wasm   ← compiled assemblies (MyLibrary + deps)
│   └── _content/
│       ├── MyLibrary/             ← scoped CSS bundles + static web assets
│       └── Moka.Blazor.Repl.Host/
│           └── wasmPreview.js     ← resize + postMessage bridge
├── _preview-assemblies/
│   ├── 3f9a…c41e.bin              ← one assembly per blazor-preview block
│   └── 81d0…77ab.bin
└── _preview-assets/
    ├── blazor-preview.css         ← tabs, iframe and error styles
    └── blazor-preview.js          ← tabs and iframe resizing
```

Each iframe loads `/_preview-wasm/index.html?assembly=/_preview-assemblies/{hash}.bin&entry={type}`.
The hash is the SHA-256 of the compiled assembly (64 hex characters), so a
file name always stands for the same bytes. Pages with previews link
`_preview-assets/blazor-preview.css` and `.js` with a `?v=` content hash
instead of carrying their own copy of the styles and script.

---

## Security

The preview page (`_preview-wasm/index.html`) runs .NET code it is given: an
assembly named by its `?assembly=` parameter, or one posted to it by the page
around it. As `Moka.Blazor.Repl.Host` ships it, the page takes both from
anywhere, so any site could open your docs' preview page and run code in your
docs' origin (in an app that serves its docs with `MapMokaDocs`, the app's
origin). mokadocs closes that when it copies the page into the site, for the
host it generates and for your own `previewHost` alike. Your project's files
are not changed.

Right after `<meta charset>`, before anything that loads, every page at the top
of the published `wwwroot` gets:

- **A guard script.** It drops every `message` event that doesn't come from the
  page's parent frame or the page itself on the same origin, before the preview
  bridge sees it. It stops the page from loading when `?assembly=` names
  anything other than a `_preview-assemblies/{sha256}.bin` file next to the
  preview host: another origin, an absolute URL, another folder.
- **A Content-Security-Policy `<meta>` tag**, adapted from the client-side
  example in the Blazor docs'
  [Content Security Policy](https://learn.microsoft.com/aspnet/core/blazor/security/content-security-policy)
  article:

  ```
  base-uri 'self'; default-src 'self'; img-src 'self' data: https:;
  font-src 'self' data: https:; object-src 'none';
  script-src 'self' 'wasm-unsafe-eval' <origins> <hashes>;
  style-src 'self' 'unsafe-inline' <origins>; connect-src 'self'
  ```

  `<hashes>` are the SHA-256 hashes of the page's inline scripts: the guard,
  the import map .NET 10 writes, and any `<script>` block of your own.
  `<origins>` are the origins of `<script src>` and stylesheet `<link href>`
  URLs the page names directly, so a host that loads a library from a CDN keeps
  working. `connect-src 'self'` means previews can only make requests to the
  docs origin.

Building again replaces what an earlier build added. The page's `.br` and `.gz`
copies are compressed again from the new page, so a server that negotiates
encodings never serves the old one. If your `index.html` has a
Content-Security-Policy of its own, the build warns: the browser enforces both,
and yours has to allow the added inline scripts.

The preview iframes have no `sandbox` attribute. On a frame from the docs' own
origin, `allow-scripts allow-same-origin` let the framed page remove its own
sandbox, so it protected nothing, and without `allow-forms` it blocked
`EditForm` submits inside previews.

A `<meta>` policy can't set `frame-ancestors`. The ASP.NET Core host sends
`frame-ancestors 'self'` as a header for `_preview-wasm/`; static hosts such as
GitHub Pages can't send headers, but the guard doesn't depend on it.

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

### `.gitattributes`

When the site has a preview host, the build also writes a `.gitattributes`
file holding `*.js binary` at the site root. If you deploy by committing the
site to a branch (`gh-pages`), Git would otherwise convert line endings in the
framework scripts, their hashes would no longer match the integrity values the
runtime checks, and previews would fail to load. Deploys that upload the
folder as an artifact (the workflow below) don't need it.

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

### Previews show a static render instead of an iframe

The build found no published preview host to use: `publishHost` is `false`
and `previewHost` isn't set or holds no published app, or the published
`wwwroot` has no `index.html`. The build's warning says which. Publish the
host into `previewHost`, or turn `publishHost` back on.

### "Refused to load" or "violates the following Content Security Policy directive"

The preview page's policy (see [Security](#security)) blocked something your
preview host or a preview does. The browser console names the URL or the
inline code. Common causes:

- A script or stylesheet loaded by a library at runtime from another origin.
  Link it from `wwwroot/index.html` instead, which adds its origin, or serve
  a copy from the host's `wwwroot`.
- An inline event handler (`onclick="..."`) in `index.html`. Move it into a
  script file or an inline `<script>` block.
- A preview that calls an API on another origin with `HttpClient`. Previews
  can only reach the docs origin.

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
through `postMessage`, plus 16px. `Moka.Blazor.Repl.Host` 1.3.5 and later (the
scaffold references 1.3.6) avoid a growth loop: it measures the `#app` element
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
| (new) `library: PackageId@Version` | Adds your library to the auto-scaffolded preview host |

The simplest migration is to **delete your old options and set only
`library: …`**. The plugin then scaffolds a new preview host for you. If
you had custom logic in your old wasmAppPath or stylesheets, move it into
`preview-host/Program.cs` and `preview-host/wwwroot/index.html` after the
scaffold runs.
