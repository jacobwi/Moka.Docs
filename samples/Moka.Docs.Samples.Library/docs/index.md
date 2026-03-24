---
title: SampleLibrary
description: A demo library showcasing MokaDocs features
layout: default
order: 1
---

# SampleLibrary

Welcome to the **SampleLibrary** documentation! This project demonstrates all the features of MokaDocs.

## Features

- Basic arithmetic with the `Calculator` class
- Generic result types with `OperationResult<T>`
- Shape abstractions with `IShape` and `Circle`
- Observable collections with events
- Extension methods for strings

## Quick Start

```csharp
var calc = new Calculator();
var result = calc.Add(2, 3); // returns 5
```

::: tip
Check out the [Getting Started](./guide/getting-started) guide for a full walkthrough.
:::

## API Overview

| Class | Description |
|-------|-------------|
| `Calculator` | Basic arithmetic operations |
| `OperationResult<T>` | Generic result wrapper |
| `Circle` | A shape with area calculation |
| `ObservableList<T>` | List with change events |
| `StringExtensions` | String utility methods |
