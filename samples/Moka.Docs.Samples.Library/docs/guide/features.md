---
title: Features Overview
description: A comprehensive showcase of every MokaDocs documentation feature
order: 2
icon: sparkles
tags: [features, cards, steps, admonitions, tables, diagrams]
---

# Features Overview

This page demonstrates every rich content feature available in MokaDocs. Use it as a reference when writing your own documentation.

## Cards

Cards draw attention to key information. They support `title`, `icon`, and `variant` attributes.

:::card{title="Calculator" icon="calculator"}
Perform arithmetic operations with full error handling. Supports addition, division, and generic functional transformations via `Process<T>`.
:::

:::card{title="Observable Collections" icon="list" variant="info"}
`ObservableList<T>` fires `ItemAdded` events whenever elements are inserted, making it easy to build reactive data pipelines.
:::

:::card{title="Shape Abstractions" icon="hexagon" variant="success"}
The `IShape` interface and its implementations (`Circle`, `Rectangle`) provide a polymorphic area-calculation API.
:::

:::card{title="Deprecation Notice" icon="alert-triangle" variant="warning"}
`LegacyCalculator` is obsolete and will be removed in v3.0. Migrate to `Calculator` before upgrading.
:::

---

## Steps

Steps render numbered instructions. Each `###` heading inside `:::steps` becomes a step.

:::steps
### Install SampleLibrary

Add the package to your .NET project:

```bash
dotnet add package SampleLibrary --version 2.0.0
```

### Import the namespace

Add the using directive at the top of your file:

```csharp
using SampleLibrary;
```

### Create a Calculator instance

Instantiate the class and call a method:

```csharp
var calc = new Calculator();
int sum = calc.Add(3, 7); // 10
```

### Handle results safely

Use `TryDivide` for operations that may fail:

```csharp
var result = calc.TryDivide(10, 0);
if (!result.Success)
    Console.WriteLine(result.Error);
```
:::

---

## Link Cards

Link cards create a clickable navigation grid. Each bullet becomes a card.

:::link-cards
- [Getting Started](/guide/getting-started) — Install and run your first project
- [API Reference](/api) — Full type and member documentation
- [Diagrams](/guide/diagrams) — Architecture and data-flow visualizations
- [Colors & Enums](/guide/colors) — Working with the Color enum
- [UI Components](/guide/components) — Cards, steps, code groups, and more
- [Blazor Preview](/guide/blazor-preview) — Live component rendering in docs
:::

---

## Code Groups

Code groups present the same concept in multiple languages or styles using tabs.

:::code-group
```csharp title="C#"
var calc = new Calculator();
var result = calc.Add(2, 3);
Console.WriteLine(result); // 5
```
```fsharp title="F#"
let calc = Calculator()
let result = calc.Add(2, 3)
printfn "%d" result // 5
```
```vb title="VB.NET"
Dim calc As New Calculator()
Dim result As Integer = calc.Add(2, 3)
Console.WriteLine(result) ' 5
```
:::

Here is another code group comparing error-handling approaches:

:::code-group
```csharp title="Exception-based"
try
{
    double val = calc.Divide(10, 0);
}
catch (DivideByZeroException ex)
{
    Console.WriteLine(ex.Message);
}
```
```csharp title="Result-based"
var result = calc.TryDivide(10, 0);
if (!result.Success)
    Console.WriteLine(result.Error);
```
:::

---

## Admonitions

MokaDocs supports seven admonition types. Each conveys a different level of importance.

::: tip
Use `TryDivide` instead of `Divide` when processing untrusted user input to avoid unhandled exceptions.
:::

::: info
SampleLibrary requires .NET 9.0 or later. Earlier runtimes are not supported.
:::

::: note
The `Circle` record uses `Math.PI` for area calculations, providing full double-precision accuracy.
:::

::: warning
Passing a negative divisor to `Divide` throws `ArgumentOutOfRangeException`. Validate inputs before calling.
:::

::: danger
Never use `LegacyCalculator` in production code. It is marked `[Obsolete]` and will be removed in the next major release.
:::

::: important
Always subscribe to `ObservableList<T>.ItemAdded` *before* adding items, or you will miss the initial events.
:::

::: caution
The `Truncate` extension method does not account for multi-byte Unicode characters. Use with ASCII or Latin text for predictable results.
:::

---

## Tables

A comparison table of the core types in SampleLibrary:

| Type | Kind | Generic | Key Members | Since |
|------|------|:-------:|-------------|:-----:|
| `Calculator` | Class | No | `Add`, `Divide`, `TryDivide`, `Process<T>` | v1.0 |
| `OperationResult<T>` | Record | Yes | `Success`, `Value`, `Error` | v1.0 |
| `IShape` | Interface | No | `Name`, `CalculateArea` | v1.0 |
| `Circle` | Record | No | `Radius` | v1.0 |
| `Rectangle` | Record | No | `Width`, `Height` | v2.0 |
| `ObservableList<T>` | Class | Yes | `Add`, `Count`, `ItemAdded` | v1.0 |
| `StringExtensions` | Static class | No | `Truncate` | v1.0 |
| `NumericExtensions` | Static class | No | `Clamp`, `IsInRange` | v2.0 |
| `Color` | Enum | No | `Red`, `Green`, `Blue`, `Yellow` | v1.0 |
| `LegacyCalculator` | Class | No | `Add` | v1.0 |

---

## Task Lists

Track implementation progress with GitHub-style checkboxes:

- [x] Core arithmetic (`Calculator`)
- [x] Generic result type (`OperationResult<T>`)
- [x] Shape abstractions (`IShape`, `Circle`)
- [x] Observable collections (`ObservableList<T>`)
- [x] String extensions (`Truncate`)
- [x] Rectangle shape
- [x] Numeric extensions (`Clamp`, `IsInRange`)
- [x] Safe division (`TryDivide`)
- [ ] Matrix operations (planned for v3.0)
- [ ] Complex number support (planned for v3.0)

---

## Footnotes

SampleLibrary follows semantic versioning[^1] and maintains backward compatibility within major versions. The `OperationResult<T>` pattern is inspired by the Result monad[^2] commonly used in functional programming.

All public APIs include XML documentation[^3] that is parsed by MokaDocs to generate the API Reference section automatically.

[^1]: See [semver.org](https://semver.org) for the full specification.
[^2]: Also known as the Either type in languages like Haskell and Scala.
[^3]: XML documentation comments use `///` triple-slash syntax in C#.
