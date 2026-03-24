---
title: ASP.NET Core Integration
description: Embed MokaDocs directly in your ASP.NET Core application
order: 7
icon: server
---

# ASP.NET Core Integration

The `Moka.Docs.AspNetCore` package lets you embed a fully generated documentation site directly inside your ASP.NET Core application. Instead of running the MokaDocs CLI as a separate build step, your app serves its own API reference and guide pages at runtime. The API surface is discovered automatically from your loaded assemblies using reflection, so there is no Roslyn compilation step and no external tooling required.

## Installation

Install the NuGet package into your ASP.NET Core project:

```bash
dotnet add package Moka.Docs.AspNetCore
```

Make sure your project produces XML documentation files so that summaries, parameter descriptions, and remarks appear in the generated API pages. Add this to your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Quick Start

Two calls are all you need -- `AddMokaDocs()` in your service registration and `MapMokaDocs()` in your endpoint configuration:

```csharp
using Moka.Docs.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register MokaDocs services
builder.Services.AddMokaDocs();

var app = builder.Build();

// Map the documentation endpoints
app.MapMokaDocs();

app.Run();
```

With this setup, navigating to `https://localhost:5001/docs` serves a complete documentation site. MokaDocs auto-discovers all public types from the calling assembly and generates API reference pages for every class, struct, interface, enum, record, and delegate it finds.

## Configuration

Pass an `Action<MokaDocsOptions>` to `AddMokaDocs()` to customize the site. Every property has a sensible default, so you only need to set what you want to change.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API";
    options.Description = "Developer documentation for the Contoso platform";
    options.Version = "v2.1";
    options.PrimaryColor = "#6d28d9";
    options.AccentColor = "#f59e0b";
    options.BasePath = "/docs";
    options.Copyright = "© 2026 Contoso Ltd.";
    options.DocsPath = "Docs";
    options.CacheOutput = true;
});
```

### Options Reference

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string` | `"API Documentation"` | Site title shown in the header and browser tab. |
| `Description` | `string` | `""` | Landing page description text. |
| `LogoUrl` | `string?` | `null` | URL to a logo image. Falls back to the default book icon. |
| `FaviconUrl` | `string?` | `null` | URL to a favicon. |
| `DocsPath` | `string?` | `null` | Path to a folder of Markdown files for guide pages. Relative paths resolve from the content root. When `null`, only API reference pages are generated. |
| `Assemblies` | `List<Assembly>` | `[]` | Assemblies to scan for public types. If left empty, the calling assembly is auto-detected. |
| `IncludeXmlDocs` | `bool` | `true` | Whether to locate and parse XML documentation files (`.xml`) next to each assembly DLL. |
| `PrimaryColor` | `string` | `"#0ea5e9"` | Primary theme color as a CSS hex value. |
| `AccentColor` | `string` | `"#f59e0b"` | Accent theme color as a CSS hex value. |
| `Version` | `string?` | `null` | Version label displayed in the header (e.g., `"v2.0"`). |
| `EnableRepl` | `bool` | `false` | Registers the interactive C# REPL plugin. |
| `EnableBlazorPreview` | `bool` | `false` | Registers the Blazor component preview plugin. |
| `BasePath` | `string` | `"/docs"` | URL path prefix where documentation is served. The site is accessible at this path and all sub-paths. |
| `CacheOutput` | `bool` | `true` | When `true`, the site is built once on the first request and served from memory. When `false`, the site rebuilds on every request. |
| `Copyright` | `string?` | `null` | Footer copyright text. Auto-generated from the title and current year if not set. |
| `Nav` | `List<NavEntry>` | `[]` | Custom sidebar navigation entries. When empty, navigation is auto-generated from the docs folder structure and API namespaces. |

### Custom Navigation

By default, MokaDocs auto-generates sidebar navigation with an "API Reference" section and, if `DocsPath` is set, a "Guide" section. Override this with the `Nav` property:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "My Library";
    options.DocsPath = "Docs";

    options.Nav =
    [
        new NavEntry
        {
            Label = "Getting Started",
            Path = "/guide",
            Icon = "book-open",
            Expanded = true
        },
        new NavEntry
        {
            Label = "API Reference",
            Path = "/api",
            Icon = "code",
            AutoGenerate = true
        },
        new NavEntry
        {
            Label = "Examples",
            Path = "/examples",
            Icon = "flask-conical"
        }
    ];
});
```

Each `NavEntry` supports these properties:

| Property | Type | Description |
|---|---|---|
| `Label` | `string` | Display label in the sidebar. |
| `Path` | `string` | URL path (e.g., `"/guide"`). |
| `Icon` | `string?` | Lucide icon name (e.g., `"book-open"`, `"code"`). |
| `Expanded` | `bool` | Whether the section starts expanded. |
| `AutoGenerate` | `bool` | Whether to auto-generate child pages (used for API reference sections). |

## Auto-Discovery

When you call `AddMokaDocs()` without specifying any assemblies, MokaDocs automatically detects the calling assembly using `Assembly.GetCallingAssembly()`. This means your project's own public types are documented with zero configuration.

To document additional assemblies (for example, a shared domain model or a companion library), add them explicitly:

```csharp
using System.Reflection;
using Contoso.Domain;
using Contoso.Shared;

builder.Services.AddMokaDocs(options =>
{
    options.Assemblies.Add(typeof(Program).Assembly);
    options.Assemblies.Add(typeof(Order).Assembly);
    options.Assemblies.Add(typeof(SharedHelpers).Assembly);
});
```

### What Gets Scanned

The `ReflectionApiModelBuilder` uses `System.Reflection` to scan each assembly's exported (public) types. For every type it finds, it builds a full API model including:

- **Constructors** with parameter types, default values, and modifiers (`ref`, `out`, `in`, `params`)
- **Properties** with getter/setter detection, static and virtual modifiers
- **Methods** including generic type parameters, extension methods, and operator overloads
- **Fields** with `const` and `readonly` detection
- **Events** with handler type information
- **Enum members** with numeric values
- **Indexers** with parameter lists
- **Type metadata** -- base types, implemented interfaces, generic constraints, attributes, obsolete markers

Compiler-generated types and members (backing fields, state machines, anonymous types) are automatically filtered out.

### XML Documentation

When `IncludeXmlDocs` is `true` (the default), MokaDocs looks for an XML file alongside each assembly's DLL. For an assembly at `bin/Debug/net9.0/MyLib.dll`, it checks for `bin/Debug/net9.0/MyLib.xml`. If found, summaries, parameter descriptions, remarks, returns tags, and `<inheritdoc/>` references are resolved and attached to the corresponding API types and members.

Make sure your `.csproj` enables XML doc generation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Markdown Docs

In addition to auto-generated API reference pages, you can include hand-written Markdown guide pages. Set the `DocsPath` option to point at a folder of `.md` files:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API";
    options.DocsPath = "Docs";
});
```

Then create a `Docs/` folder in your project root with Markdown files:

```
MyApp/
  Docs/
    getting-started.md
    authentication.md
    error-handling.md
  Program.cs
  MyApp.csproj
```

Each Markdown file supports YAML front matter for metadata:

```markdown
---
title: Getting Started
description: Set up the Contoso SDK in five minutes
order: 1
---

# Getting Started

Install the package and configure your API key...
```

These pages appear alongside the auto-generated API reference in the navigation sidebar. When `DocsPath` is set and no custom `Nav` entries are provided, MokaDocs auto-generates a "Guide" section in the sidebar that lists all your Markdown pages.

Make sure the `Docs/` folder is copied to the output directory. Add this to your `.csproj`:

```xml
<ItemGroup>
  <Content Include="Docs\**\*.md" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Customization

### Theme Colors

The `PrimaryColor` and `AccentColor` options accept any CSS hex color and are applied globally across the documentation theme:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.PrimaryColor = "#059669";  // emerald green
    options.AccentColor = "#d946ef";   // fuchsia
});
```

### Branding

Supply a logo URL and favicon to match your project's visual identity:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso Platform";
    options.LogoUrl = "/images/logo.svg";
    options.FaviconUrl = "/images/favicon.ico";
    options.Copyright = "© 2026 Contoso Ltd. All rights reserved.";
});
```

The logo and favicon URLs can be absolute or relative to your application root. If you serve static files with `app.UseStaticFiles()`, place the images in your `wwwroot` folder.

### Plugins

Enable the interactive REPL or Blazor component preview plugins through the options:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.EnableRepl = true;
    options.EnableBlazorPreview = true;
});
```

When `EnableRepl` is `true`, Markdown code blocks tagged with `csharp-repl` become interactive -- readers can run the code directly in the browser. See the [Interactive REPL](/plugins/repl) documentation for syntax details.

## Endpoint Mapping

`MapMokaDocs()` registers a catch-all route at the configured base path. You can override the path at mapping time if needed:

```csharp
// Uses options.BasePath (default "/docs")
app.MapMokaDocs();

// Override to serve at /api-docs instead
app.MapMokaDocs("/api-docs");
```

The middleware handles file resolution with fallback logic: it checks for exact path matches first, then tries appending `/index.html`, and finally tries `.html`. Static assets (CSS, JS, fonts, images) are served with a 24-hour `Cache-Control` header. If a requested path does not match any file, a 404 page from the built site is returned when available.

## Production Considerations

### Caching

By default, `CacheOutput` is `true`. The entire documentation site is built once on the first incoming request and held in memory as a dictionary of byte arrays. Subsequent requests are served directly from this in-memory cache with no disk I/O. The build is thread-safe: if multiple requests arrive before the first build completes, they all wait for the single build to finish rather than triggering duplicate builds.

To force a rebuild at runtime (for example, after a configuration change or deployment), resolve `MokaDocsService` from DI and call `InvalidateCache()`:

```csharp
app.MapPost("/admin/refresh-docs", (MokaDocsService docsService) =>
{
    docsService.InvalidateCache();
    return Results.Ok("Documentation cache cleared");
});
```

### Development Mode

During development, set `CacheOutput = false` so the site rebuilds on every request. This way changes to your Markdown files or code are reflected immediately without restarting the app:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.CacheOutput = !builder.Environment.IsDevelopment();
    options.DocsPath = "Docs";
});
```

### Performance

The in-memory site typically builds in under a second for small-to-medium projects. Once built, serving pages is a dictionary lookup followed by writing bytes to the response stream -- comparable to serving static files. Static assets are served with cache headers, so browsers cache CSS, JavaScript, fonts, and images for 24 hours.

### Security

The documentation endpoints are standard ASP.NET Core routes. Apply authorization, rate limiting, or any other middleware the same way you would for your application's own endpoints:

```csharp
app.MapMokaDocs()
    .RequireAuthorization("InternalOnly");
```

If the docs contain internal API surface that should not be publicly accessible, restrict the base path with authorization policies or serve docs only in non-production environments:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapMokaDocs();
}
```

## Comparison: ASP.NET Core Integration vs CLI

| | ASP.NET Core Integration | CLI (`mokadocs build`) |
|---|---|---|
| **Setup** | NuGet package + two lines of code | `mokadocs.yaml` config file + CLI install |
| **API Discovery** | Reflection on loaded assemblies | Roslyn source analysis from `.csproj` files |
| **Output** | In-memory, served by your app | Static files written to disk |
| **Hosting** | Your app serves everything | Deploy static files to any host |
| **Build trigger** | First HTTP request (or every request) | Explicit `mokadocs build` command |
| **Best for** | Internal tools, dev portals, self-documenting APIs | Public documentation sites, CI/CD pipelines |

Use the ASP.NET Core integration when you want your application to carry its own documentation with no external build steps. Use the CLI when you need a static site that can be deployed independently to GitHub Pages, Netlify, or any static file host.

## Full Example

Here is a complete `Program.cs` showing all major options:

```csharp
using System.Reflection;
using Moka.Docs.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API Documentation";
    options.Description = "Complete reference for the Contoso platform SDK";
    options.Version = "v3.0";

    options.PrimaryColor = "#6d28d9";
    options.AccentColor = "#f59e0b";
    options.LogoUrl = "/images/contoso-logo.svg";
    options.FaviconUrl = "/images/favicon.png";
    options.Copyright = "© 2026 Contoso Ltd.";

    options.DocsPath = "Docs";
    options.BasePath = "/docs";
    options.CacheOutput = !builder.Environment.IsDevelopment();

    options.Assemblies.Add(typeof(Program).Assembly);

    options.EnableRepl = builder.Environment.IsDevelopment();

    options.Nav =
    [
        new NavEntry { Label = "Guide", Path = "/guide", Icon = "book-open", Expanded = true },
        new NavEntry { Label = "API Reference", Path = "/api", Icon = "code", AutoGenerate = true }
    ];
});

var app = builder.Build();

app.UseStaticFiles();
app.MapMokaDocs();

app.Run();
```
