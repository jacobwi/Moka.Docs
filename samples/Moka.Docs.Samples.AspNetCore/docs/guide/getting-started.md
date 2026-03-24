---
title: Getting Started
order: 1
---

# Getting Started

This sample demonstrates embedding MokaDocs directly in an ASP.NET Core application using `Moka.Docs.AspNetCore`.

## Setup

Add MokaDocs to your `Program.cs`:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Product API";
    options.Assemblies = [typeof(Product).Assembly];
    options.DocsPath = "./docs";
});

app.MapMokaDocs(); // serves at /docs by default
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create a product |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product |

## Key Features

:::card{title="Auto-Discovery" icon="search"}
MokaDocs automatically discovers all public types in your assemblies and generates API reference pages with full XML doc comments.
:::

:::card{title="Live Docs" icon="zap"}
Documentation is served directly from your running application — no separate build step needed. Changes to your code are reflected immediately when `CacheOutput = false`.
:::

:::card{title="Markdown Guides" icon="book-open"}
Combine auto-generated API docs with hand-written Markdown guides. Just point `DocsPath` at your docs folder.
:::
