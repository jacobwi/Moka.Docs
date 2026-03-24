---
title: "SampleLibrary v2.0 Released"
description: "Rectangle shapes, numeric extensions, safe division, and the new ResultBuilder API"
date: 2026-03-15
author: MokaDocs Team
tags: [release, v2.0, announcement]
icon: package
---

# SampleLibrary v2.0 Released

We are excited to announce **SampleLibrary v2.0** — the biggest update since the initial release. This version introduces new shape types, numeric utilities, safe division, and a fluent result builder.

::: tip
Upgrade today with `dotnet add package SampleLibrary --version 2.0.0`
:::

## What's New

### Rectangle Shape

The `IShape` family gains a new member. `Rectangle` joins `Circle` as a first-class shape:

```csharp
var rect = new Rectangle(4.0, 6.0);
Console.WriteLine(rect.Name);            // "Rectangle"
Console.WriteLine(rect.CalculateArea());  // 24.0
```

### Safe Division with TryDivide

No more wrapping `Divide` in try/catch. The new `TryDivide` method returns an `OperationResult<double>`:

:::code-group
```csharp title="Before (v1.0)"
try
{
    double val = calc.Divide(10, 0);
}
catch (DivideByZeroException ex)
{
    Console.WriteLine(ex.Message);
}
```
```csharp title="After (v2.0)"
var result = calc.TryDivide(10, 0);
if (!result.Success)
    Console.WriteLine(result.Error);
```
:::

### ResultBuilder for Fluent Chains

Chain multiple operations with short-circuit semantics:

```csharp
var result = new ResultBuilder<double>(10.0)
    .Then(x => x > 0
        ? OperationResult<double>.Ok(x)
        : OperationResult<double>.Fail("Must be positive"))
    .Then(x => OperationResult<double>.Ok(x * 2))
    .Build();

Console.WriteLine(result.Value); // 20.0
```

### Numeric Extensions

Two new extension methods on `int`:

```csharp
int clamped = 150.Clamp(0, 100);     // 100
bool valid  = 42.IsInRange(0, 100);  // true
```

### Factory Methods on OperationResult

Creating results is now cleaner with `Ok` and `Fail` static methods:

```csharp
var success = OperationResult<int>.Ok(42);
var failure = OperationResult<int>.Fail("Something went wrong");
```

## Changelog

- **Added**: `Rectangle` record implementing `IShape`
- **Added**: `Calculator.TryDivide` for exception-free division
- **Added**: `OperationResult<T>.Ok` and `OperationResult<T>.Fail` factory methods
- **Added**: `ResultBuilder<T>` for fluent operation chaining
- **Added**: `NumericExtensions.Clamp` and `NumericExtensions.IsInRange`
- **Improved**: XML documentation across all public types
- **Deprecated**: `LegacyCalculator` now warns about v3.0 removal

::: warning Breaking Changes
There are no breaking changes in v2.0. All existing v1.0 APIs remain fully compatible.
:::

## Migration Guide

:::steps
### Update the package reference

```bash
dotnet add package SampleLibrary --version 2.0.0
```

### Replace try/catch with TryDivide

Find any `Divide` calls wrapped in try/catch and migrate to `TryDivide`.

### Adopt factory methods

Replace manual `OperationResult` initialization with `Ok` and `Fail`:

```csharp
// Old
var r = new OperationResult<int> { Success = true, Value = 42 };
// New
var r = OperationResult<int>.Ok(42);
```
:::

## Explore More

:::link-cards
- [Getting Started](/guide/getting-started) — Updated guide with v2.0 examples
- [Features Overview](/guide/features) — See every feature in action
- [API Reference](/api) — Full documentation for all new types
- [Diagrams](/guide/diagrams) — Updated architecture diagrams
:::
