---
title: Colors & Enums
description: Working with the Color enum and other enumerations
order: 2
tags: [enums, colors]
---

# Colors & Enums

## The Color Enum

SampleLibrary includes a `Color` enum with four values:

```csharp
public enum Color
{
    Red,        // 0
    Green,      // 1
    Blue,       // 2
    Yellow = 10 // Explicit value
}
```

::: info
Note that `Yellow` has an explicit value of `10`, not the default sequential value of `3`.
:::

## Usage Examples

### Basic enum usage

```csharp
Color favorite = Color.Blue;
Console.WriteLine(favorite); // "Blue"
```

### Pattern matching with enums

```csharp
string GetHexColor(Color color) => color switch
{
    Color.Red => "#FF0000",
    Color.Green => "#00FF00",
    Color.Blue => "#0000FF",
    Color.Yellow => "#FFFF00",
    _ => throw new ArgumentOutOfRangeException(nameof(color))
};
```

### Iterating all values

```csharp
foreach (Color c in Enum.GetValues<Color>())
{
    Console.WriteLine($"{c} = {(int)c}");
}
// Output:
// Red = 0
// Green = 1
// Blue = 2
// Yellow = 10
```

## Delegates

The `ValueChangedHandler<T>` delegate is used for change notifications:

```csharp
ValueChangedHandler<int> handler = (oldValue, newValue) =>
{
    Console.WriteLine($"Changed from {oldValue} to {newValue}");
};

handler(1, 2); // "Changed from 1 to 2"
```
