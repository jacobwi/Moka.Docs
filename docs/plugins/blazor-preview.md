---
title: Blazor Component Preview
order: 3
---

# Blazor Component Preview Plugin

The Blazor Preview plugin renders `blazor-preview` code blocks into **real static HTML** at build time using Roslyn compilation and Blazor's built-in `HtmlRenderer`. Readers see the exact same output the component would produce on first render — no JavaScript, no WASM, no iframes.

**Plugin ID:** `mokadocs-blazor-preview`

## What It Does

Each ` ```blazor-preview ` code block is compiled and rendered at build time. The result is injected directly into the page HTML as a tabbed card:

- **Preview tab** — Real server-side-rendered HTML from the component's `OnInitialized` state.
- **Source tab** — The original Razor source with syntax highlighting.

Components are rendered with full type resolution, parameter binding, dependency injection stubs, and correct CSS isolation attributes (`b-xxxxxxxx` scoped attributes) — exactly as they would appear in a real Blazor app.

## Markdown Syntax

````markdown
```blazor-preview
@code { string _name = "Aria"; }
<MokaTextField @bind-Value="_name" Label="Full name" Required />
```
````

## How It Works

### Architecture overview

```
Markdown: ```blazor-preview  →  BlazorPreviewExtension (Markdig)
                                  ↓  wraps in <div data-blazor-preview="true">
                              BlazorPreviewPlugin.ExecuteAsync
                                  ↓
                              CompileAndRenderBlocksAsync
                               ├─ Pass 1: RoslynCompilationService.CompileAsync(source)
                               │    ↓ on CS0103/CS0246 only
                               └─ Pass 2: retry with accumulated @code preamble
                                  ↓ success
                              RenderComponentAsync
                               ├─ PreviewAssemblyLoadContext.LoadFromStream(bytes)
                               ├─ assembly.GetType(entryPointTypeName)
                               └─ HtmlRenderer.RenderComponentAsync(type)
                                  ↓
                              Inject rendered HTML into page
```

### Stage 1 — Markdown Extension

`BlazorPreviewExtension` (a Markdig inline parser) detects ` ```blazor-preview ` fenced blocks and wraps each one in:

```html
<div class="blazor-preview-container" data-blazor-preview="true">
  <div class="blazor-preview-source"><pre><code>…source…</code></pre></div>
  <div class="blazor-preview-render"></div>
</div>
```

The tab bar and JavaScript are injected once per page by the plugin after rendering, not by the extension.

### Stage 2 — Roslyn Compilation

For each `blazor-preview-render` placeholder, the plugin:

1. Extracts and HTML-decodes the Razor source from the adjacent `blazor-preview-source` div.
2. Creates a `ReplProject` with a single file `Preview.razor`.
3. Adds all `usings` from `mokadocs.yaml` as global usings.
4. Calls `RoslynCompilationService.CompileAsync(project)` which:
   - Compiles the Razor file with the Razor compiler (`.razor` → C#)
   - Compiles the generated C# with Roslyn
   - Returns `CompilationResult` with either `AssemblyBytes + EntryPointTypeName` or a list of errors

All DLL references configured under `references` are passed as `MetadataReference` entries so the Razor compiler can resolve component types, base classes, and constraints.

### Stage 3 — Two-pass compilation

Previews on the same page sometimes share state — a `@code` block in one snippet may define a record or list used by the next snippet:

```razor
@* Snippet 1: defines _people *@
@code { record Person(string Name); List<Person> _people = [ new("Ada"), new("Grace") ]; }
<div>@_people.Count people</div>

@* Snippet 2: uses _people from snippet 1 *@
<MokaTable Items="_people" ...>
```

The plugin handles this with a **two-pass strategy**:

- **Pass 1** — compile the snippet as a self-contained file. If it succeeds, use the result.
- **Pass 2** — if pass 1 fails with only name-not-found errors (`CS0103`, `CS0246`, `CS0012`) _and_ previous snippets have already rendered successfully, prepend the accumulated `@code` blocks from those earlier snippets and retry.

Pass 2 is only triggered for name-not-found errors because other error types (type mismatches, wrong signatures) indicate an incorrect snippet, not a missing dependency.

### Stage 4 — Assembly Loading

The compiled assembly bytes are loaded into a **collectible `PreviewAssemblyLoadContext`** — an isolated context that can be unloaded after each render to avoid memory accumulation:

```
PreviewAssemblyLoadContext.Load(assemblyName)
  → try Default.LoadFromAssemblyName(assemblyName)   // framework + pre-loaded DLLs
  → fall back to dllLookup[assemblyName]              // project DLLs from references
```

Resolving from the **default context first** is critical. It keeps `IComponent`, `ILoggerFactory`, and all Moka.Red types at the same identity as the `HtmlRenderer`'s service provider. If project DLLs were loaded independently into the isolated context, type comparisons would fail (`MokaTextField from context A ≠ MokaTextField from context B`).

To ensure project DLLs are in the default context before the isolated context tries to find them, the plugin pre-loads all paths from `references` via `Assembly.LoadFrom()` before rendering begins.

### Stage 5 — HtmlRenderer SSR

A single `HtmlRenderer` is created once per build pass and reused for all pages. Its `ServiceProvider` is configured with:

| Service | Implementation |
|---------|---------------|
| `ILoggerFactory` | From the MokaDocs host |
| `IJSRuntime` | `NullJsRuntime` — no-op, returns `default(T)` |
| `NavigationManager` | `NullNavigationManager` — pre-initialised to `https://localhost/` |
| `IMokaToastService` | Stub registered via reflection if DLL is loaded |

The render call:

```csharp
await htmlRenderer.Dispatcher.InvokeAsync(async () =>
{
    var output = await htmlRenderer.RenderComponentAsync(componentType);
    return output.ToHtmlString();
});
```

This executes the component's synchronous lifecycle (`SetParametersAsync`, `OnInitialized`, `BuildRenderTree`) and serialises the render tree to HTML. `OnAfterRenderAsync` and any JS interop are never invoked by `HtmlRenderer`.

## Configuration

### Full example

```yaml
plugins:
  - name: mokadocs-blazor-preview
    options:
      references:
        - ../src/MyLibrary/bin/Debug/net9.0
      stylesheets:
        - ../src/MyLibrary/obj/Debug/net9.0/scopedcss/projectbundle/MyLibrary.bundle.scp.css
        - ../src/MyLibrary/wwwroot/tokens.css
      usings:
        - MyLibrary.Components
        - MyLibrary.Components.Forms
        - System.ComponentModel.DataAnnotations
```

### `references`

Paths to directories or individual DLL files containing the components you want to preview. Both absolute and relative (to `mokadocs.yaml`) paths are accepted. Directories are scanned recursively for `*.dll` files.

Every DLL in a directory is:
1. Added as a `MetadataReference` so the Roslyn compiler can resolve types.
2. Registered in `_knownDllPaths` so `PreviewAssemblyLoadContext` can locate them at runtime.
3. Pre-loaded via `Assembly.LoadFrom()` so stub service registration works.

Tip: point at your project's `bin/Debug/net9.0` directory so all transitive DLLs are picked up automatically.

### `stylesheets`

CSS files concatenated into a single bundle served at `/_preview-css/moka-preview.css`. List them in the order they should cascade:

1. CSS reset / base tokens
2. Component-level scoped CSS bundles (`*.bundle.scp.css` from the build output)
3. Any supplemental CSS (icon fonts, extra utilities)

The scoped CSS bundles contain the `b-xxxxxxxx` attribute selectors generated by Blazor's CSS isolation. Without them, previews render without component styles.

### `usings`

Namespace strings added as C# global usings to every compiled preview. These correspond to the `@using` statements you would put in `_Imports.razor`.

You need one entry per namespace containing components you reference in previews. For example, if `<MokaButton>` is in `MyLibrary.Primitives.Button`, add `MyLibrary.Primitives.Button`.

Common system namespaces (`System`, `System.Collections.Generic`, `System.Threading.Tasks`, `Microsoft.AspNetCore.Components`) are added by .NET's implicit global usings and do not need to be listed.

## CSS and theming

The plugin injects a `<style>` block once per page with:

1. **Design tokens** — CSS custom properties (`--moka-color-*`, `--moka-font-*`, etc.) scoped to `.blazor-preview-render`, covering both light and dark modes.
2. **Preview container chrome** — tab bar, source/render pane layout, error styling.

If you need the previews to inherit the page's theme, structure your design tokens so that `[data-theme="dark"] .blazor-preview-render` overrides light-mode values. The plugin follows this convention for Moka.Red:

```css
.blazor-preview-render {
    --moka-color-primary: #d32f2f;
    /* … other light tokens … */
}
[data-theme="dark"] .blazor-preview-render {
    --moka-color-primary: #ef5350;
    /* … dark overrides … */
}
```

## Generic components and type inference

Razor's type-inference code generator emits a constraint helper method for generic components (those with `@typeparam TValue where TValue : ...`). If the constraint references a type that is not in scope, compilation fails.

To avoid this, either:

- **Specify the type parameter explicitly** — `<MokaNumericField TValue="int" @bind-Value="_qty" />` — which bypasses the type inference path entirely.
- **Add the constraint's namespace** to `usings` — but only if doing so does not conflict with other generated code.

## Limitations

- **No runtime interactivity** — `@onclick`, `@bind`, and other event handlers are compiled and stripped by `HtmlRenderer`. Buttons and inputs are rendered visually but do not respond to user interaction.
- **No JS interop** — Calls to `IJSRuntime` return `default(T)`. Components that require JS for initial rendering (canvas-based, signature pads, rich editors) will render their host element but not their JS-initialised content.
- **No `OnAfterRenderAsync`** — `HtmlRenderer` does not invoke `OnAfterRenderAsync`. Only `SetParametersAsync`, `OnInitialized[Async]`, `OnParametersSet[Async]`, and `BuildRenderTree` are executed.
- **No injected services beyond stubs** — Services other than those explicitly registered in the `HtmlRenderer`'s `ServiceProvider` are unavailable. Components that `@inject` an unregistered service will throw at render time and fall back to an error display.
- **Synchronous initial render only** — If a component awaits a network call inside `OnInitializedAsync`, the preview will show the loading/empty state, not the loaded state.
- **CSS isolation requires bundle** — Scoped styles only apply when the `*.bundle.scp.css` from your build output is included in `stylesheets`.
