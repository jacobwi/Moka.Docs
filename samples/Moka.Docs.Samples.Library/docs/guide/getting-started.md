---
title: Getting Started
description: Install SampleLibrary and build your first project in minutes
order: 1
icon: rocket
tags: [setup, quickstart, installation]
---

# Getting Started

SampleLibrary is a lightweight .NET library that provides arithmetic operations, generic result types, observable collections, and shape abstractions. This guide walks you through installation, basic usage, and interactive experimentation.

::: tip
SampleLibrary targets .NET 9.0 and supports `nullable` reference types out of the box.
:::

## Installation

Add SampleLibrary to your project using your preferred method:

:::code-group
```bash title=".NET CLI"
dotnet add package SampleLibrary --version 2.0.0
```
```powershell title="Package Manager"
Install-Package SampleLibrary -Version 2.0.0
```
```xml title="PackageReference"
<PackageReference Include="SampleLibrary" Version="2.0.0" />
```
:::

## Quick Example

Here is a complete console application that exercises the core types:

```csharp
using SampleLibrary;

// Arithmetic
var calc = new Calculator();
Console.WriteLine(calc.Add(10, 20));       // 30
Console.WriteLine(calc.Divide(100, 3));    // 33.333...

// Result handling
var result = calc.TryDivide(10, 0);
if (!result.Success)
    Console.WriteLine(result.Error);        // "Cannot divide by zero."

// Shapes
IShape circle = new Circle(5.0);
IShape rect   = new Rectangle(4.0, 6.0);
Console.WriteLine($"{circle.Name}: {circle.CalculateArea():F2}");  // Circle: 78.54
Console.WriteLine($"{rect.Name}: {rect.CalculateArea():F2}");      // Rectangle: 24.00

// Observable collections
var list = new ObservableList<string>();
list.ItemAdded += (_, item) => Console.WriteLine($"Added: {item}");
list.Add("Hello");  // Added: Hello

// Extension methods
Console.WriteLine("A very long title".Truncate(10));  // "A very..."
Console.WriteLine(42.Clamp(0, 100));                   // 42
```

::: info
All examples on this page assume you have added `using SampleLibrary;` at the top of your file.
:::

## Interactive REPL

Try SampleLibrary directly in your browser. These blocks execute on the server when using `mokadocs serve`.

```csharp-repl
// Create a calculator and perform operations
var calc = new Calculator();
Console.WriteLine($"5 + 3 = {calc.Add(5, 3)}");
Console.WriteLine($"20 / 4 = {calc.Divide(20, 4)}");

// Use the generic Process method
var doubled = calc.Process(7, x => x * 2);
Console.WriteLine($"7 doubled = {doubled}");
```

```csharp-repl
// Work with shapes
var shapes = new IShape[] { new Circle(3.0), new Rectangle(4.0, 5.0) };
foreach (var shape in shapes)
{
    Console.WriteLine($"{shape.Name}: area = {shape.CalculateArea():F2}");
}
```

```csharp-repl
// Observable list with events
var names = new ObservableList<string>();
names.ItemAdded += (_, name) => Console.WriteLine($"  Welcome, {name}!");
Console.WriteLine("Adding members:");
names.Add("Alice");
names.Add("Bob");
Console.WriteLine($"Total members: {names.Count}");
```

## Error Handling

The `Calculator` class uses exceptions for invalid inputs. Use `TryDivide` for safe division that returns an `OperationResult<double>` instead of throwing:

```csharp
var calc = new Calculator();

// Exception-based
try
{
    calc.Divide(10, 0);
}
catch (DivideByZeroException ex)
{
    Console.WriteLine(ex.Message); // "Cannot divide by zero."
}

// Result-based (no exceptions)
var result = calc.TryDivide(10, 0);
if (!result.Success)
    Console.WriteLine(result.Error); // "Cannot divide by zero."
```

::: warning
The `Divide` method throws `DivideByZeroException` when the divisor is zero and `ArgumentOutOfRangeException` when the divisor is negative. Prefer `TryDivide` when dealing with user input.
:::

## String Extensions

The `Truncate` extension method shortens strings with a configurable suffix:

```csharp
string text = "This is a very long string that needs truncating";
string short1 = text.Truncate(20);         // "This is a very lo..."
string short2 = text.Truncate(20, " [...]"); // "This is a very[...]"
```

::: tip Best practice
Use `Truncate` for display purposes only. The original string is never modified.
:::

## Next Steps

:::link-cards
- [Features Overview](/guide/features) — Explore everything SampleLibrary offers
- [API Reference](/api) — Browse all types and members
- [Diagrams](/guide/diagrams) — Visual architecture of the library
- [Colors & Enums](/guide/colors) — Working with the Color enum
:::

::: danger
The `LegacyCalculator` class is deprecated and will be removed in v3.0. Migrate to `Calculator` as soon as possible.
:::
