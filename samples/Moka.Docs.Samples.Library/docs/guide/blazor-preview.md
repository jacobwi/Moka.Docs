---
title: Blazor Component Preview
description: Write Blazor components in your docs and see live previews
sidebar_position: 5
---

# Blazor Component Preview

MokaDocs supports live Blazor component previews directly in your documentation. Use the `blazor-preview` fenced code block to display both the source code and a rendered preview of your component.

## Counter Component

A classic Blazor counter component demonstrating state and event handling:

```blazor-preview
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web

<h3>Counter: @currentCount</h3>
<p>Click the button to increment the counter.</p>
<button @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;
    private void IncrementCount() => currentCount++;
}
```

## Styled Card Component

A card component with custom styling using inline styles:

```blazor-preview
<div style="border: 1px solid #e2e8f0; border-radius: 8px; padding: 1.5em; max-width: 400px; font-family: sans-serif;">
    <h4 style="margin-top: 0; color: #1e293b;">@title</h4>
    <p style="color: #64748b; line-height: 1.6;">@description</p>
    <div style="display: flex; gap: 0.5em;">
        <button @onclick="OnLearnMore" style="padding: 0.5em 1em; background: #3b82f6; color: white; border: none; border-radius: 6px; cursor: pointer;">Learn More</button>
        <button @onclick="OnDismiss" style="padding: 0.5em 1em; background: transparent; color: #64748b; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer;">Dismiss</button>
    </div>
</div>

@code {
    private string title = "Getting Started with MokaDocs";
    private string description = "MokaDocs is a modern documentation site generator for .NET libraries. It supports API docs, markdown guides, and interactive code examples.";
    private void OnLearnMore() { }
    private void OnDismiss() { }
}
```

## List Component

A component that renders a dynamic list of items:

```blazor-preview
<div style="max-width: 500px; font-family: sans-serif;">
    <h4 style="color: #1e293b; margin-bottom: 0.75em;">Features</h4>
    <ul style="list-style: none; padding: 0; margin: 0;">
        @foreach (var feature in features)
        {
            <li style="padding: 0.5em 0.75em; border-bottom: 1px solid #f1f5f9; display: flex; align-items: center; gap: 0.5em;">
                <span style="color: #22c55e;">&#10003;</span>
                <span>@feature</span>
            </li>
        }
    </ul>
</div>

@code {
    private List<string> features = new() { "XML Doc Parsing", "Markdown Guides", "Interactive REPL", "Blazor Previews", "Hot Reload" };
}
```

## How It Works

When you use `mokadocs serve`, the preview tab sends the component source to the dev server, which performs server-side rendering to produce a static HTML preview. This gives you a visual representation of the component's initial state without requiring a full Blazor WebAssembly runtime.

The preview supports:

- **HTML markup** with inline styles
- **Field initializers** (variables are substituted with their initial values)
- **`@foreach` loops** over simple collections
- **`@if` conditionals** based on field truthiness
- **Blazor directives** (`@onclick`, `@bind`, etc.) are displayed in source but stripped from the preview

::: tip
For the best experience, run `mokadocs serve` to see live previews. In static builds, the source code is always visible but previews require the dev server.
:::
