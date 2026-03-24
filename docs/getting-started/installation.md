---
title: Installation
description: How to install and set up MokaDocs
order: 1
---

# Installation

MokaDocs requires .NET 9 SDK or later.

## Install as a Global Tool

The recommended way to install MokaDocs is as a .NET global tool:

```bash
dotnet tool install -g mokadocs
```

Verify the installation:

```bash
mokadocs --version
```

## Update

To update to the latest version:

```bash
dotnet tool update -g mokadocs
```

## Install as a Local Tool

For project-specific installations, use a local tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install mokadocs
```

Then run with `dotnet mokadocs` instead of `mokadocs`.

## Prerequisites

- **.NET 9 SDK** or later — [Download](https://dotnet.microsoft.com/download)
- A .NET class library project with XML documentation enabled
- Markdown files for guide content (optional)

::: tip
Enable XML documentation generation in your `.csproj` to get the most out of MokaDocs:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```
:::

## System Requirements

MokaDocs runs on any platform supported by .NET 9:

- **Windows** 10/11 (x64, ARM64)
- **macOS** 12+ (x64, Apple Silicon)
- **Linux** (x64, ARM64) — Ubuntu, Fedora, Alpine, etc.
