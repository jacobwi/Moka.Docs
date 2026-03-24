---
title: Diagrams
description: Visual architecture and data-flow diagrams for SampleLibrary
order: 4
icon: diagram
tags: [mermaid, diagrams, visualization, architecture]
---

# Diagrams

MokaDocs renders [Mermaid](https://mermaid.js.org/) diagrams as SVG automatically. This page provides practical diagrams that map directly to the SampleLibrary architecture.

## Data Flow

How a calculation request moves through SampleLibrary:

```mermaid
graph LR
    A[Caller] -->|input values| B[Calculator]
    B -->|Add / Divide| C{Validation}
    C -->|valid| D[Compute Result]
    C -->|invalid| E[Throw Exception]
    D --> F[Return Value]
    E --> G[OperationResult.Error]
    B -->|TryDivide| H{Safe Validation}
    H -->|valid| I[OperationResult.Success]
    H -->|invalid| G
```

## Class Diagram

The full type hierarchy of SampleLibrary:

```mermaid
classDiagram
    class IShape {
        <<interface>>
        +string Name
        +CalculateArea() double
    }

    class Circle {
        +double Radius
        +string Name
        +CalculateArea() double
    }

    class Rectangle {
        +double Width
        +double Height
        +string Name
        +CalculateArea() double
    }

    class Calculator {
        +Add(int a, int b) int
        +Divide(double dividend, double divisor) double
        +TryDivide(double dividend, double divisor) OperationResult~double~
        +Process~T~(T value, Func transform) T
    }

    class OperationResult~T~ {
        +bool Success
        +T? Value
        +string? Error
        +static Ok(T value) OperationResult~T~
        +static Fail(string error) OperationResult~T~
    }

    class ObservableList~T~ {
        -List~T~ _items
        +event ItemAdded
        +int Count
        +Add(T item) void
        +this[int index] T
    }

    class StringExtensions {
        <<static>>
        +Truncate(string, int, string) string
    }

    class NumericExtensions {
        <<static>>
        +Clamp(int, int, int) int
        +IsInRange(int, int, int) bool
    }

    class Color {
        <<enumeration>>
        Red
        Green
        Blue
        Yellow
    }

    IShape <|.. Circle
    IShape <|.. Rectangle
    Calculator ..> OperationResult~T~ : creates
```

## Sequence Diagram — Calculator Usage

A typical interaction between application code and the Calculator:

```mermaid
sequenceDiagram
    participant App as Application
    participant Calc as Calculator
    participant Result as OperationResult

    App->>Calc: Add(10, 20)
    Calc-->>App: 30

    App->>Calc: Divide(100, 3)
    Calc-->>App: 33.333...

    App->>Calc: TryDivide(10, 0)
    Calc->>Result: Fail("Cannot divide by zero.")
    Result-->>App: { Success: false, Error: "..." }

    App->>Calc: TryDivide(10, 2)
    Calc->>Result: Ok(5.0)
    Result-->>App: { Success: true, Value: 5.0 }
```

## Sequence Diagram — Observable List Events

How events fire when items are added to an ObservableList:

```mermaid
sequenceDiagram
    participant Consumer
    participant List as ObservableList~string~
    participant Handler as EventHandler

    Consumer->>List: subscribe ItemAdded
    Consumer->>List: Add("Alice")
    List->>Handler: ItemAdded("Alice")
    Handler-->>Consumer: callback fires

    Consumer->>List: Add("Bob")
    List->>Handler: ItemAdded("Bob")
    Handler-->>Consumer: callback fires

    Consumer->>List: Count
    List-->>Consumer: 2
```

## State Diagram — OperationResult Lifecycle

The possible states of an `OperationResult<T>`:

```mermaid
stateDiagram-v2
    [*] --> Pending : Operation starts
    Pending --> Success : Computation succeeds
    Pending --> Failure : Validation fails
    Pending --> Failure : Exception caught
    Success --> [*] : Value consumed
    Failure --> [*] : Error handled
    Failure --> Pending : Retry
```

## Entity Relationship Diagram

How the library types relate to each other:

```mermaid
erDiagram
    Calculator ||--o{ OperationResult : produces
    IShape ||--|{ Circle : "implemented by"
    IShape ||--|{ Rectangle : "implemented by"
    ObservableList ||--o{ EventHandler : notifies
    StringExtensions }|--|| String : extends
    NumericExtensions }|--|| Int32 : extends
```
