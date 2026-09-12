---
title: ASP.NET Core Integration
description: Embed MokaDocs directly in your ASP.NET Core application
order: 7
---

# ASP.NET Core Integration

The `Moka.Docs.AspNetCore` package serves a documentation site from inside your ASP.NET Core application. Instead of running the MokaDocs CLI as a separate build step, the app builds the site in memory and serves it. API reference pages come from reflection over your loaded assemblies, so there's no Roslyn source analysis and no separate tool to install.

## Installation

Install the NuGet package into your ASP.NET Core project:

```bash
dotnet add package Moka.Docs.AspNetCore
```

Turn on XML documentation output so your doc comments show up in the API pages. Add this to your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Quick Start

Call `AddMokaDocs()` when registering services and `MapMokaDocs()` when mapping endpoints:

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

Run the app and open `/docs/api`. It lists the public types of the assembly that called `AddMokaDocs()`, with a page for each class, struct, interface, enum, record and delegate.

`/docs` itself returns 404 until the site has a home page. To add one, set `DocsPath` and put an `index.md` in that folder (see Markdown Docs below).

## Configuration

Pass an `Action<MokaDocsOptions>` to `AddMokaDocs()` to customize the site. Every property has a default, so you only set what you want to change.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API";
    options.Description = "Developer documentation for the Contoso platform";
    options.PrimaryColor = "#6d28d9";
    options.BasePath = "/docs";
    options.Copyright = "© 2026 Contoso Ltd.";
    options.DocsPath = Path.Combine(builder.Environment.ContentRootPath, "Docs");
    options.CacheOutput = true;
});
```

### Options Reference

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string` | `"API Documentation"` | Site title shown in the header and browser tab. |
| `Description` | `string` | `""` | Site description. Used as the meta description of pages that don't set their own. |
| `LogoUrl` | `string?` | `null` | Logo image URL, written into the page as given. When `null`, the header shows a default book icon. |
| `FaviconUrl` | `string?` | `null` | Favicon URL, written into the page as given. |
| `DocsPath` | `string?` | `null` | Folder of Markdown pages. A relative path resolves from the process's current working directory, not the content root. When `null`, only API reference pages are built. |
| `Assemblies` | `List<Assembly>` | `[]` | Assemblies to document. When it's empty, `AddMokaDocs()` adds the assembly that called it. |
| `IncludeXmlDocs` | `bool` | `true` | Whether to read the XML documentation file (`.xml`) next to each assembly's DLL. |
| `PrimaryColor` | `string` | `"#0ea5e9"` | Primary theme color as a CSS hex value. |
| `AccentColor` | `string` | `"#f59e0b"` | Sets the `--color-accent` CSS variable. |
| `Version` | `string?` | `null` | Not used. Setting it has no effect. |
| `EnableRepl` | `bool` | `false` | Loads the REPL plugin. The REPL doesn't run in this host (see Plugins). |
| `EnableBlazorPreview` | `bool` | `false` | Loads the Blazor preview plugin. Previews don't build in this host (see Plugins). |
| `BasePath` | `string` | `"/docs"` | URL prefix the site is served under. The site is built with it, so every link and asset URL includes it. |
| `CacheOutput` | `bool` | `true` | When `true`, the site is built on the first request and kept in memory. When `false`, every request rebuilds it. |
| `Copyright` | `string?` | `null` | Footer text. `{year}` is replaced with the current year. When `null`, the footer shows `© <current year> <Title>`. |
| `Nav` | `List<NavEntry>` | `[]` | Sidebar entries. When empty, the sidebar gets the default entries described below. |

### Custom Navigation

With an empty `Nav`, the sidebar has an "API Reference" entry for `/api` and, when `DocsPath` is set, a "Guide" entry for `/guide`. Set `Nav` to replace them:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "My Library";
    options.DocsPath = Path.Combine(builder.Environment.ContentRootPath, "Docs");

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
            Icon = "code"
        },
        new NavEntry
        {
            Label = "Examples",
            Path = "/examples",
            Icon = "file-code"
        }
    ];
});
```

Each `NavEntry` supports these properties:

| Property | Type | Description |
|---|---|---|
| `Label` | `string` | Display label in the sidebar. |
| `Path` | `string` | URL path (e.g., `"/guide"`). |
| `Icon` | `string?` | Name of an icon in MokaDocs's built-in Lucide set, such as `"book-open"` or `"code"`. An unknown name shows no icon. |
| `Expanded` | `bool` | Whether the section starts expanded. |
| `AutoGenerate` | `bool` | Not used. Setting it has no effect. |

How entries fill in, for the defaults and for your own entries alike:

- An entry lists the pages one folder level below its `Path`. For `Path = "/guide"`, that's the pages in `Docs/guide/`. Pages in a deeper folder are listed only when a page exists at that folder's route, such as `Docs/guide/advanced/index.md`, and they appear under that page.
- Children are sorted by their front matter `order`, then by title.
- When no page exists at the entry's own path, the entry links to its first child.
- The API Reference entry has no children. Namespaces and types are listed on the `/api` page itself.
- Pages directly inside the `DocsPath` folder aren't listed under any entry. To link one, add an entry whose `Path` is that page's route.

## Auto-Discovery

When no assemblies are set, `AddMokaDocs()` adds the assembly that called it, using `Assembly.GetCallingAssembly()`. Your project's own public types are documented without any configuration.

To document more assemblies (for example, a shared domain model or a companion library), add them explicitly:

```csharp
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

`ReflectionApiModelBuilder` reads each assembly's exported (public) types with `System.Reflection` and skips compiler-generated types. For each type it collects:

- **Constructors**: public instance constructors, with parameter modifiers (`ref`, `out`, `in`, `params`) and default values
- **Properties and indexers**: public ones declared on the type itself
- **Methods**: public ones declared on the type itself, including generic methods. Methods with the special-name flag are skipped, which removes property and event accessors along with operator overloads.
- **Fields**: public ones, with `const` values and `static` or `readonly` in the signature
- **Events**: public ones declared on the type itself
- **Enum members**, with their numeric values
- **Type metadata**: base type, interfaces not already inherited, generic constraints and `[Obsolete]`

The pages use the same renderer as the CLI, so the rendering notes in [API Documentation](/guide/api-docs) apply here too. As with the CLI, inherited members aren't listed on the derived type, and extension methods appear as ordinary static methods. Compared with the CLI:

- Only public members are read, so `protected` members don't appear.
- Operator overloads don't appear.
- Delegates get a page with no members, so their parameters aren't shown.
- Type pages have no View Source section, and the `/api` page has no NuGet install widget.

### XML Documentation

When `IncludeXmlDocs` is `true` (the default), MokaDocs looks for an XML file next to each assembly's DLL. For an assembly at `bin/Debug/net9.0/MyLib.dll`, it reads `bin/Debug/net9.0/MyLib.xml`. An assembly with no file location, as in a single-file publish, gets no XML docs.

Documentation is matched by documentation ID. Nested types use a different name form in reflection (`Outer+Inner`) than in the XML file (`Outer.Inner`), so nested types and their members show no XML docs.

Empty summaries on types and members are filled from the direct base type or interfaces, when those types are also in `Assemblies`. The rules are the same as for the CLI; see `<inheritdoc>` in [API Documentation](/guide/api-docs).

## Markdown Docs

In addition to the API reference, you can serve hand-written Markdown pages. Set `DocsPath` to a folder of `.md` files. A relative path resolves from the process's current working directory, which isn't always your project folder, so building the path from the content root is safer:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API";
    options.DocsPath = Path.Combine(builder.Environment.ContentRootPath, "Docs");
});
```

If the folder doesn't exist, the site is built without Markdown pages. The log line "Copying docs from ..." shows the path MokaDocs looked in.

Put guide pages in a `guide` subfolder, since the default Guide entry lists the pages in `Docs/guide/`. `Docs/index.md` becomes the page at `/docs`:

```
MyApp/
  Docs/
    index.md                  -> /docs
    guide/
      getting-started.md      -> /docs/guide/getting-started
      authentication.md       -> /docs/guide/authentication
      error-handling.md       -> /docs/guide/error-handling
  Program.cs
  MyApp.csproj
```

Other pages placed directly in `Docs/` are built, but the default sidebar doesn't link to them.

Each Markdown file supports YAML [front matter](/configuration/front-matter):

```markdown
---
title: Getting Started
description: Set up the Contoso SDK in five minutes
order: 1
---

# Getting Started

Install the package and configure your API key...
```

When the app runs from its publish folder, the Markdown files need to be there too. Copy them to the output in your `.csproj`:

```xml
<ItemGroup>
  <Content Include="Docs\**\*.md" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Customization

### Theme Colors

`PrimaryColor` sets the `--color-primary` CSS variable and the lighter and darker shades derived from it. When it differs from the default `#0ea5e9`, pages start without a color preset so your color shows. A reader who picks a preset from the color menu in the header overrides it in their own browser.

`AccentColor` sets `--color-accent`. The default theme uses it to highlight matches in search results.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.PrimaryColor = "#059669";  // emerald green
    options.AccentColor = "#d946ef";   // fuchsia
});
```

### Branding

`LogoUrl` and `FaviconUrl` take image URLs, and `Copyright` sets the footer text:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso Platform";
    options.LogoUrl = "/images/logo.svg";
    options.FaviconUrl = "/images/favicon.ico";
    options.Copyright = "© {year} Contoso Ltd. All rights reserved.";
});
```

`LogoUrl` and `FaviconUrl` are written into the page exactly as given, and `BasePath` isn't added to them. A root-relative URL such as `/images/logo.svg` points at your application's own static files, so place the images in `wwwroot` and call `app.UseStaticFiles()`. Absolute URLs work too.

### Plugins

Two plugins can be switched on through the options:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.EnableRepl = true;
    options.EnableBlazorPreview = true;
});
```

Neither one works in this host:

- **REPL.** `EnableRepl` adds a Run button to `csharp-repl` code blocks (see [Interactive REPL](/plugins/repl)). The button posts the code to `/api/repl/execute`, which only the `mokadocs serve` dev server handles. `MapMokaDocs()` doesn't map that endpoint, so readers get a "REPL server unavailable" message.
- **Blazor preview.** The plugin needs its `previewHost` or `library` option to find a preview host project, and `MokaDocsOptions` has no way to pass plugin options. Pages with `blazor-preview` blocks are built without live previews.

Other `IMokaPlugin` implementations, including ones you register in the service collection yourself, are never loaded.

## Endpoint Mapping

`MapMokaDocs()` maps two routes: the base path itself and a catch-all below it. Passing a path overrides `BasePath` for both the routes and the site build, so links and assets follow the new path:

```csharp
// Uses options.BasePath (default "/docs")
app.MapMokaDocs();

// Serves the docs at /api-docs instead
app.MapMokaDocs("/api-docs");
```

Search works under the base path: the page script loads `{BasePath}/search-index.json`, and result links include the base path.

For each request, the handler looks for an exact file match first, then tries appending `/index.html`, and then `.html`. CSS, JavaScript, font and image files are served with `Cache-Control: public, max-age=86400` (24 hours). A path with no match gets status 404 and the site's `404.html` page.

## Production Considerations

### Caching

By default, `CacheOutput` is `true`. The first request builds the whole site and keeps every file in memory as a byte array. Later requests are served from memory without touching the disk. Requests that arrive while the first build is running wait for it instead of starting builds of their own.

To rebuild while the app runs, for example after changing files under `DocsPath`, resolve `MokaDocsService` from DI and call `InvalidateCache()`:

```csharp
app.MapPost("/admin/refresh-docs", (MokaDocsService docsService) =>
{
    docsService.InvalidateCache();
    return Results.Ok("Documentation cache cleared");
});
```

### Development Mode

During development, set `CacheOutput = false` so every request rebuilds the site and Markdown edits show up on the next request. API pages come from reflection over the assemblies loaded in the running app, so code changes show up only after a restart.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.CacheOutput = !builder.Environment.IsDevelopment();
    options.DocsPath = Path.Combine(builder.Environment.ContentRootPath, "Docs");
});
```

### Performance

Once the site is built, serving a page is a dictionary lookup followed by writing the stored bytes to the response. With `CacheOutput = false`, every request runs a full build, so keep caching on in production.

### Security

`MapMokaDocs()` returns the `IEndpointRouteBuilder` it was called on, not an endpoint convention builder, so `RequireAuthorization` can't be chained onto it. Map the docs inside a route group and put the requirement on the group:

```csharp
app.MapGroup("")
    .RequireAuthorization("InternalOnly")
    .MapMokaDocs();
```

The requirement covers every docs route, including CSS and JavaScript files. The `InternalOnly` policy has to be registered with `AddAuthorization`. Other endpoint conventions go on the group the same way.

To serve the docs only during development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapMokaDocs();
}
```

## Comparison: ASP.NET Core Integration vs CLI

| | ASP.NET Core Integration | CLI (`mokadocs build`) |
|---|---|---|
| **Setup** | NuGet package and two method calls | `mokadocs.yaml` config file and CLI install |
| **API Discovery** | Reflection over loaded assemblies, public members only | Roslyn analysis of the `.cs` files in each project folder |
| **Output** | In memory, served by your app | Static files written to disk |
| **Hosting** | Your app serves everything | Deploy static files to any host |
| **Build trigger** | First HTTP request, or every request with `CacheOutput = false` | Explicit `mokadocs build` command |
| **Best for** | Internal tools and dev portals | Public documentation sites and CI/CD pipelines |

Use the ASP.NET Core integration when you want your application to carry its own documentation with no separate build step. Use the CLI when you need a static site you can deploy on its own, for example to GitHub Pages.

## Full Example

Here is a complete `Program.cs` with the options that affect the site:

```csharp
using Moka.Docs.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API Documentation";
    options.Description = "Reference for the Contoso platform SDK";

    options.PrimaryColor = "#6d28d9";
    options.LogoUrl = "/images/contoso-logo.svg";
    options.FaviconUrl = "/images/favicon.png";
    options.Copyright = "© {year} Contoso Ltd.";

    options.DocsPath = Path.Combine(builder.Environment.ContentRootPath, "Docs");
    options.BasePath = "/docs";
    options.CacheOutput = !builder.Environment.IsDevelopment();

    options.Assemblies.Add(typeof(Program).Assembly);

    options.Nav =
    [
        new NavEntry { Label = "Guide", Path = "/guide", Icon = "book-open", Expanded = true },
        new NavEntry { Label = "API Reference", Path = "/api", Icon = "code" }
    ];
});

var app = builder.Build();

app.UseStaticFiles();
app.MapMokaDocs();

app.Run();
```
