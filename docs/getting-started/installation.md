---
title: Installation
description: How to install and set up MokaDocs
order: 1
---

# Installation

MokaDocs is a .NET tool. The package contains builds for .NET 9 and .NET 10, so you need the .NET 9 or .NET 10 SDK.

## Install as a Global Tool

```bash
dotnet tool install -g mokadocs
```

Check the installation:

```bash
mokadocs --version
```

## Update

```bash
dotnet tool update -g mokadocs
```

## Install as a Local Tool

To pin the version for one repository, use a local tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install mokadocs
```

Then run `dotnet mokadocs` instead of `mokadocs`.

## Prerequisites

- **.NET 9 or .NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- A folder of Markdown files for your guides. `mokadocs init` creates a starter one.
- Optional: C# projects to generate API reference pages from, listed under `content.projects` in `mokadocs.yaml`

::: tip
The CLI reads `///` doc comments straight from your `.cs` files with Roslyn, so `mokadocs build` and `mokadocs serve` don't need `<GenerateDocumentationFile>`. The [ASP.NET Core integration](/guide/aspnetcore) works differently: it builds API pages by reflection over loaded assemblies and reads the `.xml` file next to each assembly. Enable `GenerateDocumentationFile` in projects you document that way:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```
:::

## Supported Platforms

The tool package has no platform-specific build. It needs only a .NET 9 or .NET 10 runtime, which Microsoft ships for Windows, macOS and Linux.
