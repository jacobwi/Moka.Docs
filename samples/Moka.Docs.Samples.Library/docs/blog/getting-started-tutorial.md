---
title: "Tutorial: Building a Shape Calculator"
description: "A step-by-step tutorial for building a shape area calculator with SampleLibrary"
date: 2026-03-10
author: MokaDocs Team
tags: [tutorial, beginner, shapes]
icon: graduation-cap
---

# Tutorial: Building a Shape Calculator

In this tutorial you will build a small console application that uses SampleLibrary to calculate the areas of different geometric shapes and handle errors gracefully.

::: info Prerequisites
This tutorial assumes you have .NET 9.0 SDK installed and basic familiarity with C#.
:::

## What You Will Build

A command-line tool that:

1. Creates several shapes
2. Calculates their areas
3. Demonstrates safe error handling with `OperationResult<T>`
4. Uses the observable list to log events

## Step-by-Step

:::steps
### Create a new project

Open your terminal and scaffold a console application:

```bash
dotnet new console -n ShapeCalculator
cd ShapeCalculator
dotnet add package SampleLibrary --version 2.0.0
```

### Define the shapes

Open `Program.cs` and replace its contents:

```csharp
using SampleLibrary;

// Create a collection of shapes
IShape[] shapes =
[
    new Circle(5.0),
    new Circle(2.5),
    new Rectangle(4.0, 6.0),
    new Rectangle(10.0, 10.0),
];
```

### Calculate and display areas

Add a loop that prints each shape and its area:

```csharp
Console.WriteLine("Shape Areas");
Console.WriteLine(new string('-', 30));

foreach (var shape in shapes)
{
    string name = shape.Name.Truncate(15);
    double area = shape.CalculateArea();
    Console.WriteLine($"{name,-15} {area,10:F2} sq units");
}
```

::: tip
The `Truncate` extension method comes from `StringExtensions` in SampleLibrary. It is available on all strings once you add `using SampleLibrary;`.
:::

### Add safe division

Use the `Calculator` to compute ratios between shapes:

```csharp
var calc = new Calculator();

Console.WriteLine();
Console.WriteLine("Area Ratios (relative to first shape)");
Console.WriteLine(new string('-', 40));

double baseArea = shapes[0].CalculateArea();

foreach (var shape in shapes)
{
    var result = calc.TryDivide(shape.CalculateArea(), baseArea);
    if (result.Success)
        Console.WriteLine($"{shape.Name}: {result.Value:F3}x");
    else
        Console.WriteLine($"{shape.Name}: {result.Error}");
}
```

### Track additions with ObservableList

Use an `ObservableList` to log when shapes are registered:

```csharp
Console.WriteLine();
Console.WriteLine("Registering shapes...");

var registry = new ObservableList<IShape>();
registry.ItemAdded += (_, shape) =>
    Console.WriteLine($"  Registered: {shape.Name} (area={shape.CalculateArea():F2})");

foreach (var shape in shapes)
    registry.Add(shape);

Console.WriteLine($"Total shapes registered: {registry.Count}");
```

### Run the application

```bash
dotnet run
```
:::

## Expected Output

```
Shape Areas
------------------------------
Circle            78.54 sq units
Circle            19.63 sq units
Rectangle         24.00 sq units
Rectangle        100.00 sq units

Area Ratios (relative to first shape)
----------------------------------------
Circle: 1.000x
Circle: 0.250x
Rectangle: 0.306x
Rectangle: 1.274x

Registering shapes...
  Registered: Circle (area=78.54)
  Registered: Circle (area=19.63)
  Registered: Rectangle (area=24.00)
  Registered: Rectangle (area=100.00)
Total shapes registered: 4
```

## Key Takeaways

:::card{title="Result Types Over Exceptions" icon="shield" variant="success"}
`TryDivide` returns an `OperationResult<double>` instead of throwing. This is safer in loops and user-facing code.
:::

:::card{title="Observable Collections" icon="bell" variant="info"}
`ObservableList<T>` fires events automatically. No manual notification code required.
:::

:::card{title="Extension Methods" icon="puzzle" variant="success"}
Import `SampleLibrary` once and get `Truncate`, `Clamp`, and `IsInRange` on built-in types.
:::

## Next Steps

:::link-cards
- [Features Overview](/guide/features) — Explore every MokaDocs documentation feature
- [API Reference](/api) — Full type and member documentation
- [v2.0 Release Notes](/blog/release-v2) — See everything new in v2.0
:::
