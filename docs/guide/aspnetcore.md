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
    options.DocsPath = "Docs";
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
| `DocsPath` | `string?` | `null` | Folder of Markdown pages. A relative path resolves from the application's content root. When `null`, only API reference pages are built. |
| `Assemblies` | `List<Assembly>` | `[]` | Assemblies to document. When it's empty, `AddMokaDocs()` adds the assembly that called it. |
| `IncludeXmlDocs` | `bool` | `true` | Whether to read XML documentation: the `.xml` file next to each assembly's DLL, and the files in `XmlDocFiles`. |
| `XmlDocFiles` | `List<string>` | `[]` | More XML documentation files, each matched to an assembly by the assembly name inside it. A relative path resolves from the content root (see XML Documentation). |
| `IncludeNamespaces` | `List<string>` | `[]` | Namespaces to document, each with the namespaces below it. When empty, every namespace is documented (see Filtering). |
| `ExcludeNamespaces` | `List<string>` | `[]` | Namespaces to leave out, each with the namespaces below it. Wins over `IncludeNamespaces`. |
| `SourceLinks` | `bool` | `true` | Whether type pages link to the type's source in its repository, from the SourceLink data in the assembly's PDB (see View Source). |
| `IncludeSourceCode` | `bool` | `false` | Whether type pages show the type's declaration, method bodies included, in a View Source section (see View Source). |
| `PrimaryColor` | `string` | `"#0ea5e9"` | Primary theme color as a CSS hex value. |
| `AccentColor` | `string` | `"#f59e0b"` | Sets the `--color-accent` CSS variable. |
| `Version` | `string?` | `null` | Label of the version selector in the header, such as `"v2.0"`. When `null`, the header has no version selector. |
| `EnableRepl` | `bool` | `false` | Adds a Run button to `csharp-repl` code blocks and, in Development, the endpoint that runs them (see Interactive REPL). |
| `EnableBlazorPreview` | `bool` | `false` | Loads the Blazor preview plugin. Its options go in a `Plugins` entry (see Plugins). |
| `Plugins` | `List<PluginEntry>` | `[]` | Plugins to run, each with its options (see Plugins). |
| `OpenApiDocument` | `string?` | `null` | Name of one of the app's OpenAPI documents, such as `"v1"`, to build REST API pages from (see REST API (OpenAPI)). When `null`, there are none. |
| `OpenApiRoutePrefix` | `string` | `"/rest-api"` | Route of the REST API pages, below `BasePath`. Also the default `routePrefix` of `openapi` entries in `Plugins`. |
| `OpenApiLabel` | `string` | `"REST API"` | Title of the REST API overview page and label of its sidebar entry. Also the default `label` of `openapi` entries in `Plugins`. |
| `BasePath` | `string` | `"/docs"` | URL prefix the site is served under. The site is built with it, so every link and asset URL includes it. |
| `CacheOutput` | `bool` | `true` | When `true`, the site is built once (see `BuildOnStartup`) and kept in memory. When `false`, every request rebuilds it. |
| `BuildOnStartup` | `bool` | `true` | Builds the site in the background as soon as the app has started, so the first reader doesn't wait for it. Ignored when `CacheOutput` is `false`. |
| `WatchDocs` | `bool?` | `null` | Rebuilds the site when a file under `DocsPath` changes. `null` watches in Development only. Ignored when `CacheOutput` is `false`. |
| `ShowFeedback` | `bool` | `true` | Shows the "Was this page helpful?" widget and maps the endpoint it posts to (see Feedback). |
| `OnFeedback` | `Func<DocsFeedback, HttpContext, Task>?` | `null` | Called with each vote, so you can store it (see Feedback). |
| `AllowRemoteRepl` | `bool` | `false` | Maps the REPL endpoint outside Development and lets it run code for requests from other machines. Anyone who can reach it can run code on the server (see Interactive REPL). |
| `ReplWorkerStartInfo` | `Func<ProcessStartInfo>?` | `null` | The command that starts the REPL worker process, for hosts where the default doesn't work (see Interactive REPL). |
| `Copyright` | `string?` | `null` | Footer text. `{year}` is replaced with the current year. When `null`, the footer shows `© <current year> <Title>`. |
| `Nav` | `List<NavEntry>` | `[]` | Sidebar entries. When empty, the sidebar gets the default entries described below. |

### Custom Navigation

With an empty `Nav`, the sidebar has an "API Reference" entry for `/api`, listing each namespace with its types, a "REST API" entry when the site has REST API pages (see REST API (OpenAPI)), and, when `DocsPath` is set, a "Guide" entry for `/guide`. Set `Nav` to replace them:

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
| `AutoGenerate` | `bool` | On an entry whose `Path` is `/api`, lists each API namespace with its types. Other entries are unaffected. |

How entries fill in, for the defaults and for your own entries alike:

- An entry lists the pages one folder level below its `Path`. For `Path = "/guide"`, that's the pages in `Docs/guide/`. Pages in a deeper folder are listed only when a page exists at that folder's route, such as `Docs/guide/advanced/index.md`, and they appear under that page.
- Children are sorted by their front matter `order`, then by title.
- When no page exists at the entry's own path, the entry links to its first child.
- Type pages sit several route segments below `/api`, so an entry for `/api` gets no children from its pages. With `AutoGenerate = true` it lists a heading for each namespace, holding a link to each type. Without it, namespaces and types are listed on the `/api` page only.
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

`ReflectionApiModelBuilder` reads each assembly's exported (public) types with `System.Reflection`, adds the protected nested types of types a consumer can derive from, and skips compiler-generated types and members. It also skips types and members marked `[EditorBrowsable(EditorBrowsableState.Never)]`, and the namespaces the filters leave out (see Filtering). For each type it collects the members declared on the type itself:

- **Constructors**: instance constructors, with parameter modifiers (`ref`, `out`, `in`, `params`) and default values
- **Properties and indexers**, with `init` and accessor accessibility such as `{ get; private set; }`
- **Methods**, including generic and extension methods. Property and event accessors are skipped.
- **Operators**: operator overloads and conversion operators
- **Fields**, with `const` values
- **Events**
- **Delegates**: an `Invoke` member with the delegate's parameters and return type
- **Enum members**, with their numeric values
- **Type metadata**: base type, interfaces not already inherited, generic constraints and `[Obsolete]`

Public members are read on every type. Protected and protected internal members are read too, unless the type is sealed or static, since a consumer reaches them by deriving from the type. Member signatures read like declarations: `protected virtual void OnChanged()`, `public static string Describe(this Shape shape, string? prefix = null)`, `public const int MaxSides = 12`.

The pages use the same renderer as the CLI, so the rendering notes in [API Documentation](/guide/api-docs) apply here too. As with the CLI, a type page lists the members it inherits and the extension methods that apply to it, when the types that declare them are in `Assemblies`. Compared with the CLI, the View Source section is opt-in (see View Source), and the `/api` page has no NuGet install widget.

### Filtering

`IncludeNamespaces` and `ExcludeNamespaces` pick the namespaces that get pages:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.IncludeNamespaces = ["Contoso.Api"];
    options.ExcludeNamespaces = ["Contoso.Api.Internal"];
});
```

- An entry matches its namespace and every namespace below it, a segment at a time: `Contoso.Api` matches `Contoso.Api` and `Contoso.Api.Models`, not `Contoso.Apis`. Matching is case-sensitive.
- An empty `IncludeNamespaces` documents every namespace.
- An exclude entry wins over an include entry. The example documents `Contoso.Api` and the namespaces below it, except `Contoso.Api.Internal` and the ones below that.
- Types declared outside any namespace are listed under `(global)`. Use `"(global)"` or an empty string to include or exclude them. Either one matches the global namespace only.

A type the filters leave out gets no page, no sidebar entry and no search entry, and nothing links to it: a `<see cref>` that names it, or a base type in an excluded namespace, shows as plain text. It still counts for `<inheritdoc/>`, so a type you keep inherits documentation from a base type you left out.

Types and members marked `[EditorBrowsable(EditorBrowsableState.Never)]` are left out too, the way IntelliSense hides them, along with the types nested in a hidden type. They aren't read at all, so they pass no documentation on through `<inheritdoc/>`.

### XML Documentation

When `IncludeXmlDocs` is `true` (the default), MokaDocs looks for an XML file next to each assembly's DLL. For an assembly at `bin/Debug/net9.0/MyLib.dll`, it reads `bin/Debug/net9.0/MyLib.xml`.

An assembly without one gets a warning that names the assembly and the path MokaDocs looked in. Like every build warning, it's logged and listed in `InMemorySite.Diagnostics`. To fix it, set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in that assembly's project, or add its XML file to `XmlDocFiles`. To go without XML docs, and without the warning, set `IncludeXmlDocs = false`. An assembly with no file location, as in a single-file publish, gets the warning too, since there's no folder to look in.

A package's XML file stays in the NuGet cache: the build copies the package's DLL to the output folder, not its documentation. Set this in the app's project to copy the XML files of every package reference:

```xml
<PropertyGroup>
  <CopyDocumentationFilesFromPackages>true</CopyDocumentationFilesFromPackages>
</PropertyGroup>
```

Or list files in `XmlDocFiles`:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Assemblies.Add(typeof(Program).Assembly);
    options.Assemblies.Add(typeof(JsonConvert).Assembly);
    options.XmlDocFiles.Add("xml-docs/Newtonsoft.Json.xml");
});
```

Each file in `XmlDocFiles` is matched to an assembly by the `<doc><assembly><name>` element inside it, so its file name doesn't matter, and it's read instead of the file next to that assembly's DLL. Two files for the same assembly are combined. A relative path resolves from the content root. A file that doesn't exist, or that documents an assembly not in `Assemblies`, gets a warning. `XmlDocFiles` is ignored when `IncludeXmlDocs` is `false`.

Documentation is matched by documentation ID, the `name` attribute in the XML file. A delegate's `<param>` and `<returns>` tags describe its `Invoke` member.

Empty summaries on types and members are filled from base types and interfaces, when those types are also in `Assemblies`, even when a namespace filter leaves them out. The rules are the same as for the CLI, `<inheritdoc cref="..."/>` included; see `<inheritdoc>` in [API Documentation](/guide/api-docs).

### View Source

Type pages can link to the type's source in its repository and show its declaration, as CLI pages do. Both come from the assembly's portable PDB: the `.pdb` file next to the DLL, or the PDB embedded in the DLL with `<DebugType>embedded</DebugType>`. A PDB from a different build of the assembly is ignored. Without a usable PDB, type pages have neither, and the site builds as usual.

**Source links** are on by default (`SourceLinks`). When the PDB carries SourceLink data, the type page ends with a "View source on github.com" link. The .NET 8 SDK and later write that data for a project in a git repository whose remote is on a host SourceLink knows, such as GitHub.

- On GitHub and GitLab, self-hosted GitLab included, the link opens the file at the type, such as `https://github.com/contoso/shop/blob/<commit>/src/Orders/Order.cs#L12`.
- Other hosts, such as Azure Repos and Bitbucket, get the raw file URL that SourceLink records, with no line.
- The line is the type's first line of code, such as a method body, a field initializer or a property's `get;`, since the PDB records lines for code only. A type without code, such as an enum, an interface or an empty class, links to its file with no line.
- A `partial` type links to the first of its files that's in the repository. `EmbedUntrackedSources` puts the files git doesn't track, such as generated ones, in the PDB; those are skipped unless every file is embedded.

**Inline source** is off by default. Set `IncludeSourceCode = true` to add the CLI's collapsed View Source section to each type page: the type's declaration, with every part of a `partial` type, without the members the page leaves out. The members it keeps show with their bodies, which can reveal how your application works, so turn it on only when everyone who can read the docs may see that code.

The source text comes from the PDB when it embeds the source files, or else from the paths the PDB records, when those files exist on the machine serving the docs. They usually do while you develop and don't after publishing, since the publish folder has no source and a build with `ContinuousIntegrationBuild` set records paths such as `/_/src/Orders/Order.cs`. To carry the source with the app, embed it:

```xml
<PropertyGroup>
  <DebugType>embedded</DebugType>
  <EmbedAllSources>true</EmbedAllSources>
</PropertyGroup>
```

A type whose source can't be found has no View Source section. Members are left out of the section by the same rules as the page, `[EditorBrowsable(EditorBrowsableState.Never)]` included, except that enum members always show.

## Markdown Docs

In addition to the API reference, you can serve hand-written Markdown pages. Set `DocsPath` to a folder of `.md` files. A relative path resolves from the application's content root, so it finds the folder whichever directory the app starts in, including with `--contentRoot`:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Title = "Contoso API";
    options.DocsPath = "Docs";
});
```

If the folder doesn't exist, the site is built without Markdown pages, and a warning in the log names the path MokaDocs looked in.

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

## REST API (OpenAPI)

Set `OpenApiDocument` to the name of one of your app's OpenAPI documents, and the site gets REST API pages built from it: an overview at `/docs/rest-api`, a page per tag and a models page at `/docs/rest-api/models`, laid out as described in [OpenAPI Plugin](/plugins/openapi).

```csharp
builder.Services.AddOpenApi();

builder.Services.AddMokaDocs(options =>
{
    options.OpenApiDocument = "v1";
});

var app = builder.Build();

app.MapOpenApi();
app.MapMokaDocs();
```

MokaDocs gets the document from the provider that `AddOpenApi()` (Microsoft.AspNetCore.OpenApi) or `AddSwaggerGen()` (Swashbuckle.AspNetCore) registers, the one `dotnet-getdocument` uses to write the document at build time. `MapOpenApi()` isn't needed for the docs; it serves the JSON at `/openapi/v1.json` for other tools. The document is read for every build, after the app has started, so it describes every endpoint the app maps. The name is matched without regard to case.

- A schema name links to the API reference page of the C# type with the same name, when exactly one type in `Assemblies` has it, and to the schema on the models page otherwise.
- The document the pages were built from is served at `/docs/rest-api/openapi.json`, from memory like the pages, and the overview links to it. It is the JSON the provider generated when the site was built, so it can differ from what `MapOpenApi()` serves for a request: that one names the request's host as its server.
- The overview lists the document's servers and security schemes, and each endpoint shows the schemes it needs. In Development, Microsoft.AspNetCore.OpenApi names the addresses the app listens on as servers, such as `http://127.0.0.1:5000`; in other environments, and with Swashbuckle, the document has none unless a document transformer or filter adds some. Security schemes appear once a transformer or `AddSecurityDefinition` adds them.
- With an empty `Nav`, the sidebar gets a "REST API" entry that lists the tag pages, then the models page. With your own `Nav`, add one: `new NavEntry { Label = "REST API", Path = "/rest-api", Icon = "globe" }`.
- `OpenApiRoutePrefix` and `OpenApiLabel` change the route and the label.

When the document can't be read, the site is built without REST API pages, and a warning in the log and in `InMemorySite.Diagnostics` says why:

- No provider is registered: call `AddOpenApi()` or `AddSwaggerGen()`.
- No document has that name: the warning lists the names there are.
- Generating the document threw: the warning has the exception's message.

MokaDocs is tested with these providers:

| Package | Target | Writes |
|---|---|---|
| Microsoft.AspNetCore.OpenApi 9 | .NET 9 | OpenAPI 3.0 |
| Microsoft.AspNetCore.OpenApi 10 | .NET 10 | OpenAPI 3.1 |
| Swashbuckle.AspNetCore 9 | .NET 9 | OpenAPI 3.0 |
| Swashbuckle.AspNetCore 10 | .NET 10 | OpenAPI 3.0 |

Swashbuckle needs `builder.Services.AddEndpointsApiExplorer()` before `AddSwaggerGen()` to see minimal API endpoints.

MokaDocs reads the document as JSON itself and doesn't reference Microsoft.OpenApi, so the app keeps the version of that library its OpenAPI package needs: 1.x for the 9 packages, 2.x for the 10 ones.

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

Declare plugins in `Plugins`. A `PluginEntry` names a plugin by its `Id` and carries the options its `plugins:` entry would take in `mokadocs.yaml`:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Plugins =
    [
        new PluginEntry
        {
            Id = "openapi",
            Options = { ["spec"] = "openapi.json" }
        }
    ];

    options.Nav =
    [
        new NavEntry { Label = "REST API", Path = "/rest-api", Icon = "globe" },
        new NavEntry { Label = "API Reference", Path = "/api", Icon = "code", AutoGenerate = true }
    ];
});
```

Relative paths in plugin options, such as the OpenAPI `spec`, resolve from the content root, the way the CLI resolves them from the folder of `mokadocs.yaml`. A list option takes any collection of strings, such as a `string[]`.

A declared plugin runs only if the service collection has it. `AddMokaDocs()` registers the built-in plugins you declare: `openapi`, `mokadocs-blazor-preview`, `mokadocs-changelog` and `mokadocs-python-api`. Register a plugin of your own as an `IMokaPlugin`:

```csharp
builder.Services.AddSingleton<IMokaPlugin, ReleaseNotesPlugin>();
builder.Services.AddMokaDocs(options =>
{
    options.Plugins = [new PluginEntry { Id = "release-notes" }];
});
```

An id that matches no registered plugin is skipped with a warning in the log. The build's warnings and errors, plugin failures included, are logged and listed in `InMemorySite.Diagnostics`.

Notes on the built-in plugins in this host:

- **OpenAPI.** An `openapi` entry without a `routePrefix` or `label` gets `OpenApiRoutePrefix` (`/rest-api`) and `OpenApiLabel`. The plugin's own default prefix, `/api`, is where this host puts the C# API reference. For the app's own document, set `OpenApiDocument` (see REST API (OpenAPI)). To document a second one, add an entry whose `appDocument` option names it, with a `routePrefix` of its own: `new PluginEntry { Id = "openapi", Options = { ["appDocument"] = "internal", ["routePrefix"] = "/internal-api" } }`.
- **Blazor preview.** `EnableBlazorPreview = true` loads the plugin. Its options go in an entry with the id `mokadocs-blazor-preview`, which loads the plugin on its own too:

  ```csharp
  options.Plugins =
  [
      new PluginEntry
      {
          Id = "mokadocs-blazor-preview",
          Options = { ["previewHost"] = "../PreviewHost", ["usings"] = new[] { "Contoso.Components" } }
      }
  ];
  ```

  When the preview host's publish output is missing or older than its sources, the plugin runs `dotnet publish` during the build, which needs the .NET SDK on the machine serving the docs. The published host is served from memory under `{BasePath}/_preview-wasm/`.
- **REPL.** `EnableRepl` loads the plugin, which adds a Run button to `csharp-repl` code blocks. Running the code takes one more line in `Program.cs`; see [Interactive REPL](#interactive-repl) below.

## Endpoint Mapping

`MapMokaDocs()` creates a route group at the base path and maps the docs inside it:

| Route | Methods | Mapped when |
|---|---|---|
| `{BasePath}` and `{BasePath}/{**path}` | GET, HEAD | Always |
| `{BasePath}/api/feedback` | POST | `ShowFeedback` is `true`, the default (see [Feedback](#feedback)) |
| `{BasePath}/api/repl/execute` | POST | `EnableRepl` is `true` and the app runs in Development, or `AllowRemoteRepl` is `true` (see [Interactive REPL](#interactive-repl)) |

Passing a path overrides `BasePath` for both the routes and the site build, so links and assets follow the new path:

```csharp
// Uses options.BasePath (default "/docs")
app.MapMokaDocs();

// Serves the docs at /api-docs instead
app.MapMokaDocs("/api-docs");

// Serves the docs at the root of the app
app.MapMokaDocs("/");
```

At the root, the catch-all route answers every GET and HEAD request that no other endpoint matches, with the docs' 404 page when the site has no such file. `UseStaticFiles()` skips a request once routing has picked an endpoint for it, so call `app.UseStaticFiles()` before `app.UseRouting()` if the app also serves files from `wwwroot`.

`MapMokaDocs()` returns the group, so endpoint conventions such as `RequireAuthorization` chain onto it and cover every docs route (see [Security](#security)).

Search works under the base path: the page script loads `{BasePath}/search-index.json`, and result links include the base path.

For each request, the handler looks for an exact file match first, then tries appending `/index.html`, and then `.html`. A path with no match gets status 404 and the site's `404.html` page.

Every file is served with a strong `ETag` computed from its content. A request whose `If-None-Match` header names it gets `304 Not Modified` with no body, and an unchanged file keeps its `ETag` across rebuilds. CSS, JavaScript, font and image files are served with `Cache-Control: public, max-age=86400` (24 hours). Pages, the search index and every other file get `Cache-Control: no-cache`, so browsers check the `ETag` on each visit and see a rebuilt page right away.

## Feedback

Each page ends with a "Was this page helpful?" widget. A vote posts `{"page": "/docs/guide/setup", "helpful": true}` to `{BasePath}/api/feedback`. The app logs it at Information level and, when you set `OnFeedback`, passes it to your code so you can store it:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.OnFeedback = async (feedback, context) =>
    {
        var votes = context.RequestServices.GetRequiredService<IFeedbackStore>();
        await votes.SaveAsync(feedback.Page, feedback.Helpful, context.RequestAborted);
    };
});
```

`DocsFeedback.Page` is the path the reader was on, base path included. The endpoint reads at most 4 KB of body (`413` beyond that) and answers `400` to a vote without a page or a `helpful` value, or with a page over 1,024 characters. When `OnFeedback` throws, the error is logged and the reader's browser gets a `500`. Anyone who can load the docs can vote, as often as they like, so treat the counts as a hint rather than a poll.

`ShowFeedback = false` removes the widget from every page and leaves the endpoint unmapped.

## Interactive REPL

`EnableRepl = true` adds a Run button to `csharp-repl` code blocks (see [Interactive REPL](/plugins/repl)) and maps `POST {BasePath}/api/repl/execute`, which runs the snippet and returns its output.

:::warning
The endpoint compiles and runs any C# code it receives, with the permissions of the account your app runs as. **Anyone who can reach it can run code on your server.** By default it is mapped in the Development environment only and runs code only for requests from the machine the app runs on, like `mokadocs serve`, which listens on localhost. See [Remote REPL](#remote-repl) to change that.
:::

Snippets run in a separate process, so a snippet that hangs is killed after 5 seconds without taking the app with it. That process is your app's own executable, started again. Make this the first line of `Program.cs`:

```csharp
if (await MokaDocsReplWorker.TryRunAsync()) return;
```

In the web app, it returns `false` at once. In the copy started for snippets, it runs them until the web app closes it and then returns `true`, so the copy never builds the web host. Without this line, the endpoint starts nothing: starting your executable again would start a second copy of the web app. It answers with an error naming the missing line, and the log gets a warning at startup.

The worker starts on the first Run and stops with the app. It references every assembly in `Assemblies`, so snippets can use the documented types. Their top-level namespaces are imported (`Contoso` and `Contoso.Shop` for `Contoso.Shop.Orders.Order`); deeper ones need a `using`:

```csharp
using Contoso.Shop.Orders;

var order = new Order { Id = 42 };
Console.WriteLine(order.Total);
```

The snippet limits are the same as under `mokadocs serve`: 10,000 characters of code, 5 seconds per run and 100,000 characters of output (see [Security](/plugins/repl#security) on the REPL page). The endpoint reads at most 64 KB of request body.

### The worker command

MokaDocs starts the worker with:

- `dotnet MyApp.dll`, when the app runs as `dotnet MyApp.dll`
- the app's own executable, when it runs from it (`MyApp.exe`, `dotnet run`)
- `dotnet MyApp.dll` otherwise, such as under IIS in-process hosting, with the `dotnet` named by `DOTNET_HOST_PATH` or found on the `PATH`

For any other host, set `ReplWorkerStartInfo` to a function that returns the command. The process has to call `ReplWorker.RunAsync` with its standard input and output, which is what `TryRunAsync` does when the `MOKADOCS_REPL_WORKER` environment variable is `1`. MokaDocs adds that variable and `MOKADOCS_REPL_PARENT_PID` to the start info, so a worker exits when the web app does:

```csharp
options.ReplWorkerStartInfo = () => new ProcessStartInfo("/opt/shop/Contoso.Shop");
```

With `ReplWorkerStartInfo` set, MokaDocs doesn't check for the line in `Program.cs`.

### Remote REPL

Outside Development, `EnableRepl` keeps the Run buttons but maps no endpoint and logs a warning; a click shows "REPL server unavailable". In Development, a request from another machine gets a 403 and an error in the output panel. That covers an app that listens on every interface, as apps in containers often do: the origin checks stop other web pages, not other machines. The check uses `HttpContext.Connection.RemoteIpAddress`, so behind a proxy on another machine, call `UseForwardedHeaders()` first.

To allow both, set `AllowRemoteRepl = true` and put the docs behind authorization that only people you would trust with a shell on the server can pass:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.EnableRepl = true;
    options.AllowRemoteRepl = true;
});

var app = builder.Build();
app.MapMokaDocs().RequireAuthorization("Admins");
```

The worker process isolates hangs and crashes. It is not a sandbox: a snippet can read and write the same files, secrets and network as the app.

## Production Considerations

### Caching

By default, `CacheOutput` is `true` and the site is built in the background as soon as the app has started (`BuildOnStartup`). The build starts from `IHostApplicationLifetime.ApplicationStarted`, after every endpoint is mapped. Requests that arrive before it finishes wait for it instead of starting builds of their own. Every file is kept in memory as a byte array, and later requests are served from memory without touching the disk.

A startup build that fails is logged, and the first request tries again. With `BuildOnStartup = false`, the first request builds the site. An app that calls `AddMokaDocs()` but never `MapMokaDocs()`, for example one that maps the docs in Development only, builds nothing at startup.

To rebuild while the app runs, resolve `MokaDocsService` from DI. `RebuildAsync()` builds a new site while requests keep getting the current one, then swaps it in. `InvalidateCache()` drops the site, and the next request waits for a build:

```csharp
app.MapPost("/admin/refresh-docs", async (MokaDocsService docsService) =>
{
    await docsService.RebuildAsync();
    return Results.Ok("Documentation rebuilt");
}).RequireAuthorization("Admins");
```

### Development Mode

In Development, MokaDocs watches the `DocsPath` folder and rebuilds the site in the background when a file in it changes. Requests get the previous site until the new one is ready, then the new one replaces it. Changes made in quick succession start one rebuild, and files and folders whose names start with a dot are ignored. A rebuild that fails is logged, and the previous site stays.

`WatchDocs` controls this. Its default, `null`, watches in Development only; `true` watches in every environment and `false` turns it off:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.DocsPath = "Docs";
    options.WatchDocs = true; // a staging server that edits docs in place
});
```

API pages come from reflection over the assemblies loaded in the running app, so code changes show up only after a restart.

`CacheOutput = false` still rebuilds the site on every request. Nothing is built at startup or watched then.

### Performance

Once the site is built, serving a page is a dictionary lookup followed by writing the stored bytes to the response. With `CacheOutput = false`, every request runs a full build, so keep caching on in production.

### Security

`MapMokaDocs()` returns the route group that holds every docs endpoint, so `RequireAuthorization` chains onto it. The requirement covers every docs route, including CSS and JavaScript files, the search index and the POST endpoints:

```csharp
app.MapMokaDocs().RequireAuthorization("InternalOnly");
```

The `InternalOnly` policy has to be registered with `AddAuthorization`. Other endpoint conventions go on the group the same way. A group that the docs are mapped into works too: `app.MapGroup("").RequireAuthorization("InternalOnly").MapMokaDocs()`.

The feedback and REPL endpoints accept a POST only from the docs' own pages, or from a client that isn't a browser:

- `Content-Type` has to be `application/json`. A page on another site can't send that without a CORS preflight, and nothing answers one.
- A `Sec-Fetch-Site` header, when present, has to be `same-origin`.
- An `Origin` header, when present, has to match the scheme, host and port the request was sent to.

Anything else gets `403`, and the log gets a warning. Requests without either header, such as from `curl`, are accepted. Behind a proxy that terminates TLS, call `app.UseForwardedHeaders()` so the request's scheme and host match what the browser sees, or every vote and Run from a browser gets `403`.

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
| **API Discovery** | Reflection over loaded assemblies, public and protected members | Roslyn analysis of the `.cs` files in each project folder |
| **Output** | In memory, served by your app | Static files written to disk |
| **Hosting** | Your app serves everything | Deploy static files to any host |
| **Build trigger** | App startup, a change under `DocsPath` in Development, or every request with `CacheOutput = false` | Explicit `mokadocs build` command |
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
    options.Version = "v2.0";

    options.PrimaryColor = "#6d28d9";
    options.LogoUrl = "/images/contoso-logo.svg";
    options.FaviconUrl = "/images/favicon.png";
    options.Copyright = "© {year} Contoso Ltd.";

    options.DocsPath = "Docs";
    options.BasePath = "/docs";

    options.Assemblies.Add(typeof(Program).Assembly);

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
