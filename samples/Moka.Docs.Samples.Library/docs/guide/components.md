---
title: UI Components
description: Rich reusable components for your documentation
order: 3
icon: layout
tags: [components, cards, steps, code-group]
---

# UI Components

MokaDocs provides a set of rich UI components you can use directly in your Markdown documentation.

## Cards

Cards highlight key information with optional icons and color variants.

:::card{title="Quick Start" icon="rocket"}
Get up and running in under 5 minutes with our simple setup guide.
:::

:::card{title="Secure by Default" icon="shield" variant="success"}
All API endpoints are authenticated and encrypted out of the box.
:::

:::card{title="Breaking Change" icon="zap" variant="warning"}
Version 2.0 introduces a new configuration format. See the migration guide for details.
:::

:::card{title="Performance" icon="star" variant="info"}
SampleLibrary processes over 10,000 requests per second on a single core.
:::

## Steps

Present instructions as clear, numbered steps.

:::steps
### Install the package

Run the following command to add SampleLibrary to your project:

```bash
dotnet add package SampleLibrary
```

### Configure your project

Add the configuration to your `Program.cs`:

```csharp
builder.Services.AddSampleLibrary(options =>
{
    options.EnableCaching = true;
});
```

### Build and run

Execute the following to see results:

```bash
dotnet run
```
:::

## Link Cards

Create a grid of clickable navigation cards.

:::link-cards
- [Getting Started](/guide/getting-started) — Learn the basics of SampleLibrary
- [API Reference](/api) — Browse all types and members
- [Configuration](/guide/colors) — Customize colors and themes
:::

## Code Group

Group multiple code blocks into a tabbed interface for multi-language examples.

:::code-group
```csharp title="C#"
var calculator = new Calculator();
var result = calculator.Add(2, 3);
Console.WriteLine(result); // 5
```
```fsharp title="F#"
let calculator = Calculator()
let result = calculator.Add(2, 3)
printfn "%d" result // 5
```
```python title="Python"
# Using the Python bindings
calculator = Calculator()
result = calculator.add(2, 3)
print(result)  # 5
```
:::
