---
title: Quick Start
description: Set up a MokaDocs site and preview it locally
order: 2
---

# Quick Start

This guide sets up a docs site for a .NET library, previews it with the dev server, then builds the static output.

## 1. Initialize Your Project

In the folder that should hold the site config (usually your solution folder), run:

```bash
mokadocs init
```

`init` takes no arguments and writes two files to the current directory:

- `mokadocs.yaml` - site configuration
- `docs/index.md` - a starter home page

If `mokadocs.yaml` already exists, `init` does nothing.

## 2. Configure Your Site

Edit `mokadocs.yaml` and list the projects you want API reference pages for:

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

build:
  output: ./_site
```

Relative paths resolve against the folder that contains `mokadocs.yaml`.

## 3. Write Your First Guide

Create `docs/getting-started.md`:

````markdown
---
title: Getting Started
description: Learn how to use MyLibrary
order: 1
---

# Getting Started

Welcome to MyLibrary! Here's how to get started.

## Installation

Install from NuGet:

```bash
dotnet add package MyLibrary
```

## Basic Usage

```csharp
using MyLibrary;

var result = MyClass.DoSomething("hello");
Console.WriteLine(result);
```
````

## 4. Start the Dev Server

```bash
mokadocs serve
```

This builds the site, serves it at `http://localhost:5080` and opens your browser. Use `-p` to pick another port and `--no-open` to skip the browser. You'll see:

- Your home page from `docs/index.md`
- API reference pages under `/api`, generated from the `.cs` files in your project's folder
- Search (Ctrl+K or Cmd+K), which matches page titles, headings, tags and the first 300 characters of each page's text. Code blocks aren't indexed.
- A sidebar generated from the `docs/` folder

::: tip
The dev server watches the docs folder (`content.docs`) and `mokadocs.yaml`. Saving a file there triggers a rebuild, then the browser reloads. Two limits:

- C# source files aren't watched. Your changes to them show up on the next rebuild or after you restart `serve`.
- Editing `mokadocs.yaml` triggers a rebuild, but `serve` keeps using the config it loaded at startup. Restart `serve` to apply config changes.
:::

See [Dev Server & Hot Reload](/advanced/dev-server) for the other options.

## 5. Build for Production

When you're ready to deploy:

```bash
mokadocs build
```

The site is written to `build.output` (`./_site` here), or to the folder you pass with `-o`. With `build.clean` left at its default of `true`, the build deletes that folder before writing. `mokadocs build` exits with code 1 when the build reports errors, so a CI job fails instead of publishing a broken site.

The output is plain static files. See [Deployment](/advanced/deployment) for GitHub Pages, base paths and other hosts.

## Project Structure

A typical MokaDocs project looks like this:

```
my-library/
├── src/
│   └── MyLibrary/
│       ├── MyLibrary.csproj
│       └── MyClass.cs
├── docs/
│   ├── index.md              # Home page
│   ├── getting-started.md    # Guide page
│   └── guide/
│       ├── configuration.md  # Nested guide
│       └── advanced.md       # Nested guide
├── mokadocs.yaml             # Site configuration
├── .mokadocs/                # API analysis cache (gitignore this)
└── _site/                    # Generated output (gitignore this)
```

See [Project Structure](/getting-started/project-structure) for how files map to URLs.

## What's Next?

::: link-cards
- [Configuration](/configuration/site-config) - The mokadocs.yaml reference
- [Markdown Guide](/guide/markdown) - The Markdown extensions MokaDocs adds
- [API Documentation](/guide/api-docs) - Configure API reference generation
- [Themes](/themes/customization) - Theme options and custom themes
:::
