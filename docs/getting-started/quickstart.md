---
title: Quick Start
description: Get your first MokaDocs site up and running in 5 minutes
order: 2
---

# Quick Start

This guide walks you through creating your first MokaDocs documentation site.

## 1. Initialize Your Project

Navigate to your .NET solution directory and run:

```bash
mokadocs init
```

This creates:
- `mokadocs.yaml` — site configuration
- `docs/` — directory for your Markdown guides
- `docs/index.md` — your landing page

## 2. Configure Your Site

Edit `mokadocs.yaml` to point to your project:

```yaml
site:
  title: "My Library Docs"
  description: "Documentation for MyLibrary"

content:
  docs: ./docs
  projects:
    - path: ./src/MyLibrary/MyLibrary.csproj
      label: "MyLibrary"

theme:
  name: default

features:
  search:
    enabled: true
```

## 3. Write Your First Guide

Create `docs/getting-started.md`:

```markdown
---
title: Getting Started
description: Learn how to use MyLibrary
order: 1
---

# Getting Started

Welcome to MyLibrary! Here's how to get started.

## Installation

Install from NuGet:

\`\`\`bash
dotnet add package MyLibrary
\`\`\`

## Basic Usage

\`\`\`csharp
using MyLibrary;

var result = MyClass.DoSomething("hello");
Console.WriteLine(result);
\`\`\`
```

## 4. Start the Dev Server

```bash
mokadocs serve
```

Open your browser to `http://localhost:5080`. You'll see:

- Your landing page
- Auto-generated API reference from your `.csproj`
- Full-text search
- Responsive sidebar navigation

::: tip
The dev server watches for changes and automatically rebuilds. Edit a `.md` file or your C# code and the browser refreshes instantly.
:::

## 5. Build for Production

When you're ready to deploy:

```bash
mokadocs build
```

The static site is generated in `_site/` (configurable). Deploy this directory to any static hosting provider — GitHub Pages, Netlify, Vercel, Azure Static Web Apps, etc.

## Project Structure

A typical MokaDocs project looks like this:

```
my-library/
├── src/
│   └── MyLibrary/
│       ├── MyLibrary.csproj
│       └── MyClass.cs
├── docs/
│   ├── index.md              # Landing page
│   ├── getting-started.md    # Guide page
│   └── guide/
│       ├── configuration.md  # Nested guide
│       └── advanced.md       # Nested guide
├── mokadocs.yaml             # Site configuration
└── _site/                    # Generated output (gitignore this)
```

## What's Next?

::: link-cards
- [Configuration](/configuration/site-config) — Learn about all configuration options
- [Markdown Guide](/guide/markdown) — Master the Markdown extensions
- [API Documentation](/guide/api-docs) — Configure API reference generation
- [Themes](/themes/customization) — Customize the look and feel
:::
