---
title: Blazor Component Preview
order: 3
---

# Blazor Component Preview Plugin

The Blazor Preview plugin renders Razor component source code as live HTML previews directly in your documentation. Readers can see what a component looks like alongside its source code, without needing to run a Blazor application.

**Plugin ID:** `mokadocs-blazor-preview`

## What It Does

The plugin transforms code blocks marked with the `blazor-preview` language identifier into a tabbed interface with two views:

- **Preview tab** -- A rendered HTML representation of the component, showing the initial visual output.
- **Source tab** -- The original Razor/Blazor source code with syntax highlighting.

The preview tab is shown by default, giving readers an immediate visual understanding of the component. A **BLAZOR** badge and **Refresh** button are included in the tab header.

## Markdown Syntax

Use the `blazor-preview` language identifier on fenced code blocks:

````markdown
```blazor-preview
<h3>Counter: @currentCount</h3>
<button @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;
    private void IncrementCount() => currentCount++;
}
```
````

This renders as a tabbed preview with the counter heading showing "Counter: 0" and a styled button in the Preview tab, and the full source code in the Source tab.

## How It Works

The Blazor Preview plugin integrates through both a Markdig extension and the plugin execution pipeline.

### 1. Markdown Extension (BlazorPreviewExtension)

During markdown parsing, the `BlazorPreviewExtension` detects code blocks with the `blazor-preview` language identifier and wraps them in a structured container:

```html
<div class="blazor-preview-container" data-blazor-preview="true">
    <pre><code class="language-csharp"><!-- original source --></code></pre>
</div>
```

### 2. Plugin Injection (BlazorPreviewPlugin)

During the plugin execution phase, the `BlazorPreviewPlugin` injects the tabbed UI framework into pages containing preview containers. This includes:

- **Tab bar** with Preview and Source tabs
- **BLAZOR badge** indicating the component type
- **Refresh button** to re-fetch the preview
- **CSS** for the preview frame, tabs, and badges
- **JavaScript** that auto-fetches the preview on page load

### 3. Preview Request

The injected JavaScript extracts the source code from each preview container and sends a `POST` request to `/api/blazor/preview` with the component source. On receiving the rendered HTML response, it injects the output into the Preview tab panel.

### 4. Server-Side Rendering (BlazorPreviewService)

The `BlazorPreviewService` performs a lightweight server-side rendering of the Razor component source. This is not full Blazor Server or WebAssembly rendering. Instead, it performs template-level processing:

1. **Extracts `@using` directives** -- Any `@using` statements are collected for namespace resolution context.

2. **Separates markup from `@code` block** -- The service splits the component into its HTML template portion and the `@code { }` section.

3. **Parses field initializers** -- Fields declared in the `@code` block are parsed for their initial values. For example, `private int currentCount = 0;` registers `currentCount` with value `0`.

4. **Substitutes `@variable` expressions** -- Occurrences of `@variableName` in the markup are replaced with the field's initial value. So `@currentCount` becomes `0` in the rendered output.

5. **Processes `@if` blocks** -- Conditional blocks are evaluated based on the truthiness of the field value. Non-null, non-zero, non-empty values are truthy.

6. **Processes `@foreach` blocks** -- Foreach loops over simple collections (arrays and lists initialized inline) are expanded, repeating the loop body for each element.

7. **Strips Blazor directives** -- Interactive directives like `@onclick`, `@bind`, `@ref`, `@onchange`, and others are removed from the preview HTML since they require a Blazor runtime to function.

The result is a static HTML snapshot representing the component's initial render state.

## Supported Features

### HTML Markup with Inline Styles

Standard HTML elements and inline styles render as expected:

````markdown
```blazor-preview
<div style="padding: 1rem; background: #f0f0f0; border-radius: 8px;">
    <h4 style="color: #333;">Styled Card</h4>
    <p>This card has custom styling.</p>
</div>
```
````

### Field Initializers

Variables declared in the `@code` block with initial values are substituted into the template:

````markdown
```blazor-preview
<p>Welcome, @userName!</p>
<p>You have @messageCount new messages.</p>

@code {
    private string userName = "Alice";
    private int messageCount = 5;
}
```
````

The preview renders "Welcome, Alice!" and "You have 5 new messages."

### @foreach Loops

Simple foreach loops over inline collections are expanded:

````markdown
```blazor-preview
<ul>
    @foreach (var item in items)
    {
        <li>@item</li>
    }
</ul>

@code {
    private List<string> items = new() { "Apples", "Bananas", "Cherries" };
}
```
````

The preview renders an unordered list with three items.

### @if Conditionals

Conditional rendering based on field truthiness:

````markdown
```blazor-preview
@if (showMessage)
{
    <div class="alert">This message is visible!</div>
}

@code {
    private bool showMessage = true;
}
```
````

Since `showMessage` is `true`, the alert div is included in the preview.

### Blazor Directives in Source

Directives like `@onclick`, `@bind`, and `@ref` are preserved in the Source tab for documentation purposes but are stripped from the Preview tab output since they require a Blazor runtime:

````markdown
```blazor-preview
<input @bind="searchText" placeholder="Search..." />
<button @onclick="Search">Go</button>
<p>Searching for: @searchText</p>

@code {
    private string searchText = "";
    private void Search() { /* ... */ }
}
```
````

The Preview tab shows the input, button, and paragraph without the `@bind` and `@onclick` attributes. The Source tab shows the complete Razor source.

## Configuration

The Blazor Preview plugin requires no options for basic usage:

```yaml
plugins:
  - name: mokadocs-blazor-preview
```

## Limitations

The Blazor Preview plugin performs template-level rendering, not full Blazor component rendering. This means several capabilities are not supported:

- **No runtime interactivity** -- Buttons, inputs, and other interactive elements are rendered visually but do not respond to clicks or input. Event handlers (`@onclick`, `@onchange`, etc.) are stripped from the preview.
- **No dependency injection** -- Services injected with `@inject` are not available. Components that depend on injected services will not render those values.
- **No complex expressions** -- Only simple `@variable` substitution is supported. Complex C# expressions like `@(count + 1)`, method calls like `@GetTitle()`, or ternary operators are not evaluated.
- **No component composition** -- Child components (e.g., `<MyChildComponent />`) are not rendered. Only native HTML elements are processed.
- **No lifecycle methods** -- `OnInitialized`, `OnParametersSet`, and other lifecycle methods are not executed. Only field initializers provide values.
- **No two-way binding** -- `@bind` directives are stripped. Bound values show their initial state only.
- **No CSS isolation** -- Component-scoped CSS (`::deep`, CSS isolation files) is not applied to previews.

For scenarios requiring full interactivity, consider linking to a running Blazor sample application or using the REPL plugin to demonstrate code execution.
