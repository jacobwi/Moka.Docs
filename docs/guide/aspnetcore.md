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
| `SiteUrl` | `string?` | `null` | The URL readers reach the app at, such as `"https://contoso.com"`, like [`site.url`](/configuration/site-config#url). When set, pages get a canonical link and link preview tags, and `llms.txt` links are absolute (see [Link Previews](#link-previews)). |
| `SocialImage` | `string?` | `null` | The picture link previews show for pages without a front matter [`image`](/configuration/front-matter#image), like [`site.image`](/configuration/site-config#image): a file under `DocsPath` or an absolute URL. Needs `SiteUrl` (see [Link Previews](#link-previews)). |
| `DocsPath` | `string?` | `null` | Folder of Markdown pages. A relative path resolves from the application's content root. When `null`, only API reference pages are built. |
| `Assemblies` | `List<Assembly>` | `[]` | Assemblies to document. When it's empty, `AddMokaDocs()` adds the assembly that called it. |
| `IncludeXmlDocs` | `bool` | `true` | Whether to read XML documentation: the `.xml` file next to each assembly's DLL, and the files in `XmlDocFiles`. |
| `XmlDocFiles` | `List<string>` | `[]` | More XML documentation files, each matched to an assembly by the assembly name inside it. A relative path resolves from the content root (see XML Documentation). |
| `IncludeNamespaces` | `List<string>` | `[]` | Namespaces to document, each with the namespaces below it. When empty, every namespace is documented (see Filtering). |
| `ExcludeNamespaces` | `List<string>` | `[]` | Namespaces to leave out, each with the namespaces below it. Wins over `IncludeNamespaces`. |
| `SourceLinks` | `bool` | `true` | Whether type pages link to the type's source in its repository, from the SourceLink data in the assembly's PDB (see View Source). |
| `IncludeSourceCode` | `bool` | `false` | Whether type pages show the type's declaration, method bodies included, in a View Source section (see View Source). |
| `Language` | `string` | `"en"` | The language of the pages, a tag such as `"de"` or `"pt-BR"`, like [`site.language`](/guide/languages#site.language): `<html lang>`, the theme's built-in translation, plural forms and dates (see [Languages](#languages)). |
| `Direction` | `string?` | `null` | `"ltr"` or `"rtl"`, like [`site.direction`](/guide/languages#site.direction). `null` follows `Language`: right to left for Arabic, Hebrew, Persian, Urdu and a few more. |
| `Strings` | `Dictionary<string, string>` | `{}` | Your own wording for the theme's text, by key, like [`theme.options.strings`](/guide/languages#theme.options.strings). An unknown key logs a warning. |
| `PrimaryColor` | `string` | `"#0ea5e9"` | Primary theme color as a CSS hex value. |
| `AccentColor` | `string` | `"#f59e0b"` | Sets the `--color-accent` CSS variable. |
| `Preset` | `string?` | `null` | A theme preset: `"reference"`, `"terminal"`, `"book"`, `"product"`, `"minimal"`, `"accessible"` or `"moka"`. It fills in the theme options still at their default value (see [Theme Presets](#theme-presets)). An unknown name applies nothing and logs a warning. |
| `Style` | `string` | `"default"` | The look pages start with: `"default"`, `"moka"`, `"minimal"`, `"classic"` or `"soft"` (see Theme Styles). An unknown name renders as `"default"` and logs a warning. |
| `StyleSelector` | `bool` | `true` | Shows the Style section of the Appearance panel, where readers pick another style. When `false`, every page uses `Style`. |
| `TextSizeSelector` | `bool` | `true` | Shows the Text size section of the Appearance panel (90% to 125%), like [`theme.options.textSizeSelector`](/configuration/site-config#options.textsizeselector). |
| `WidthSelector` | `bool` | `true` | Shows the Reading width section of the Appearance panel (narrow, default, wide, wider or full), like [`theme.options.widthSelector`](/configuration/site-config#options.widthselector). |
| `LayoutSelector` | `bool` | `false` | Adds a Layout section to the Appearance panel, where readers switch the layout variants of a docs page; `Layout` stays the default. Like [`theme.options.layoutSelector`](/configuration/site-config#options.layoutselector). |
| `ImageZoom` | `bool` | `true` | Opens a content image full size when the reader clicks it, like [`theme.options.imageZoom`](/configuration/site-config#options.imagezoom). |
| `Layout` | `MokaDocsLayoutOptions` | every part `"default"` | Layout variants for the page width, top bar, footer, "On this page" panel, breadcrumbs, previous/next links, sidebar, home page, landing hero, feature cards, admonitions, tables, spacing, typeface, headings and links (see Page Layout). An unknown name renders as `"default"` and logs a warning. |
| `Sidebar` | `string` | `"auto"` | How pages get the sidebar: `"inline"` (all of it in every page), `"shared"` (written once to `{BasePath}/_theme/js/sidebar.js` and loaded by every page) or `"auto"` (inline up to 300 entries, shared above). See Sidebar Size. |
| `SidebarToggle` | `bool` | `false` | Adds a button that hides and shows the sidebar on wide screens, like [`theme.options.sidebarToggle`](/configuration/site-config#options.sidebartoggle). |
| `Version` | `string?` | `null` | Label of the version selector in the header, such as `"v2.0"`. When `null`, the header has no version selector. It isn't read from the app's assembly version, which is rarely the version of the library the docs describe. Pages with a front matter `version` range that leaves out this version aren't served. |
| `EnableRepl` | `bool` | `false` | Adds a Run button to `csharp-repl` code blocks and, in Development, the endpoint that runs them (see Interactive REPL). |
| `EnableBlazorPreview` | `bool` | `false` | Loads the Blazor preview plugin. Its options go in a `Plugins` entry (see Plugins). Outside Development the plugin never publishes a preview host itself. |
| `Plugins` | `List<PluginEntry>` | `[]` | Plugins to run, each with its options (see Plugins). |
| `OpenApiDocument` | `string?` | `null` | Name of one of the app's OpenAPI documents, such as `"v1"`, to build REST API pages from (see REST API (OpenAPI)). When `null`, there are none. |
| `OpenApiRoutePrefix` | `string` | `"/rest-api"` | Route of the REST API pages, below `BasePath`. Also the default `routePrefix` of `openapi` entries in `Plugins`. |
| `OpenApiLabel` | `string` | `"REST API"` | Title of the REST API overview page and label of its sidebar entry. Also the default `label` of `openapi` entries in `Plugins`. |
| `BasePath` | `string` | `"/docs"` | URL prefix the site is served under. The site is built with it, so every link and asset URL includes it. |
| `PathBase` | `string?` | `null` | The app's path base, such as `"/shop"` for an IIS sub-application, `UsePathBase("/shop")` or a proxy that sends `X-Forwarded-Prefix`. Links start with `PathBase` followed by `BasePath` (see [Path Base](#path-base)). |
| `CacheOutput` | `bool` | `true` | When `true`, the site is built once (see `BuildOnStartup`) and kept in memory. When `false`, every request rebuilds it and waits for its build. |
| `Minify` | `bool` | `false` | Minifies the pages, like [`build.minify`](/configuration/site-config#minify). When `true`, the theme's CSS and JS are minified too. |
| `MinifyAssets` | `bool?` | `null` | Minifies the theme's CSS and JS, like [`build.minifyAssets`](/configuration/site-config#minifyassets). `null` minifies them outside the Development environment and keeps them readable in Development. |
| `Llms` | `bool` | `true` | Publishes a Markdown copy of each page next to its HTML, plus `llms.txt` and `llms-full.txt`, like [`build.llms`](/configuration/site-config#llms). Pages with a copy get a "Copy page" button. |
| `BuildOnStartup` | `bool` | `true` | Builds the site in the background as soon as the app has started. Requests that arrive before it's done get `503` with `Retry-After`. When `false`, the first request starts the build. Ignored when `CacheOutput` is `false`. |
| `WatchDocs` | `bool?` | `null` | Rebuilds the site when a file under `DocsPath` changes. `null` watches in Development only. Ignored when `CacheOutput` is `false`. |
| `ShowFeedback` | `bool` | `true` | Shows the "Was this page helpful?" widget and maps the endpoint it posts to (see Feedback). |
| `OnFeedback` | `Func<DocsFeedback, HttpContext, Task>?` | `null` | Called with each vote, so you can store it (see Feedback). |
| `ConfigureFeedbackEndpoint` | `Action<IEndpointConventionBuilder>?` | `null` | Adds conventions, such as a rate limit, to the feedback endpoint alone (see [Endpoint Conventions](#endpoint-conventions)). |
| `ConfigureReplEndpoint` | `Action<IEndpointConventionBuilder>?` | `null` | Adds conventions, such as authorization, to the REPL endpoint alone (see [Endpoint Conventions](#endpoint-conventions)). |
| `AllowRemoteRepl` | `bool` | `false` | Maps the REPL endpoint outside Development and lets it run code for requests from other machines. Anyone who can reach it can run code on the server (see Interactive REPL). |
| `ReplWorkerStartInfo` | `Func<ProcessStartInfo>?` | `null` | The command that starts the REPL worker process, for hosts where the default doesn't work (see Interactive REPL). |
| `Copyright` | `string?` | `null` | Footer text. `{year}` is replaced with the current year. When `null`, the footer shows `© <current year> <Title>`. |
| `Banner` | `MokaDocsBannerOptions?` | `null` | An announcement above the header of every page, like [`theme.options.banner`](/configuration/site-config#options.banner) (see [Announcement Banner](#announcement-banner)). When `null`, there is none. |
| `Nav` | `List<NavEntry>` | `[]` | Sidebar entries. When empty, the sidebar gets the default entries described below. |
| `Blog` | `MokaDocsBlogOptions?` | `null` | Builds the posts in a folder under `DocsPath` (`blog` by default) into a blog with an index, tag pages and a sidebar entry, without feeds (see [Blog](/guide/blog#in-the-asp.net-core-host)). When `null`, the folder's files are ordinary pages. |
| `Glossary` | `Dictionary<string, string>` | `{}` | Glossary terms and their definitions, like [`glossary`](/configuration/site-config#glossary-section): `:term[API]` shows a definition and `:::glossary` lists them (see [Glossary](/guide/components#glossary)). A `glossary.yml` file at the root of `DocsPath` adds more. |

### Custom Navigation

With an empty `Nav`, the sidebar has an "API Reference" entry for `/api`, listing each namespace with its types, a "REST API" entry when the site has REST API pages (see REST API (OpenAPI)), when `DocsPath` is set a "Guide" entry listing the folder tree below `/guide`, and with `Blog` set an entry for the blog. The API Reference and Guide entries are `AutoGenerate` entries. Set `Nav` to replace them:

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
| `AutoGenerate` | `bool` | Lists everything below `Path` as a tree: each folder is a section, and under `/api` each namespace is a section holding its types. Built by the same code as a `mokadocs` site without a `nav:` list. |

How entries fill in, for the defaults and for your own entries alike:

- With `AutoGenerate = true`, an entry lists the folder tree below its `Path`: for `Path = "/guide"`, the pages in `Docs/guide/`, and a section for each subfolder, whether it has an `index.md` or not. For `Path = "/api"`, a collapsed heading for each namespace, holding a link to each type; the one with the current page opens. The tree is three levels deep at most, counting the entry; deeper pages are listed after their third-level folder, labeled like `Hooks / Events`.
- Without it, an entry lists the pages one folder level below its `Path`. Pages in a deeper folder are listed only when a page exists at that folder's route, such as `Docs/guide/advanced/index.md`, and they appear under that page. Type pages sit several route segments below `/api`, so an entry for `/api` gets no children; namespaces and types are listed on the `/api` page only.
- Children are sorted by their front matter `order`, then by title.
- When no page exists at the entry's own path, the entry links to its first child.
- Pages directly inside the `DocsPath` folder aren't listed under any entry. To link one, add an entry whose `Path` is that page's route.

### Sidebar Size

Every page shows the whole sidebar. With `Sidebar = "inline"` every page also holds a copy of it, which on a big API reference makes up most of each page. With `Sidebar = "shared"`, the sidebar is written once, to `{BasePath}/_theme/js/sidebar.js`, and served from the in-memory site like any other file, with an `ETag`. Each page loads it from a script tag inside the sidebar, with a hash of the file in the URL, and keeps only the top-level entries and the entries along its own path for readers without JavaScript. The default, `"auto"`, is inline up to 300 sidebar entries and shared above that. An unknown value works like `"auto"` and logs a warning.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Sidebar = "shared";
});
```

See [Large Sidebars](/configuration/navigation#large-sidebars-inline-or-shared) for how the shared sidebar works.

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

`ReflectionApiModelBuilder` reads each assembly's exported (public) types with `System.Reflection`, adds the protected nested types of types a consumer can derive from, and skips compiler-generated types and members. It also skips types and members marked `[EditorBrowsable(EditorBrowsableState.Never)]`, and the namespaces the filters leave out (see Filtering). An assembly listed twice in `Assemblies` is read once. For each type it collects the members declared on the type itself:

- **Constructors**: instance constructors, with parameter modifiers (`ref`, `out`, `in`, `params`) and default values
- **Properties and indexers**, with `init` and accessor accessibility such as `{ get; private set; }`
- **Methods**, including generic and extension methods. Property and event accessors are skipped.
- **Extension blocks** (C# 14): the members of each `extension(...)` block, read from the nested types the compiler emits for it, and its receiver, type parameters and documentation. Properties stay properties, and the static methods that implement the members aren't listed as methods of their own.
- **Operators**: operator overloads and conversion operators
- **Fields**, with `const` values (`const decimal` included), `ref` and `ref readonly` fields, and `fixed` buffers as `public fixed byte Bytes[16]`
- **Events**
- **Delegates**: an `Invoke` member with the delegate's parameters and return type
- **Enum members**, with their numeric values
- **Type metadata**: base type, interfaces not already inherited, generic constraints, records (record structs included) and `[Obsolete]`. The base type and interfaces are written with their namespaces, as the CLI writes them: `System.Exception`, `System.IEquatable<Contoso.Money>`.

Public members are read on every type. Protected and protected internal members are read too, unless the type is sealed or static, since a consumer reaches them by deriving from the type. Member signatures read like declarations: `protected virtual void OnChanged()`, `public static string Describe(this Shape shape, string? prefix = null)`, `public const int MaxSides = 12`, `public static delegate* unmanaged[Cdecl]<int> Native()`. Attribute arguments are written the way C# source writes them: `[Produces("application/json", "text/plain", Types = new Type[] { typeof(int) })]`, `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]`.

The compiler marks every ref struct, and the constructors of a type with `required` members, with an `[Obsolete]` of its own so that old compilers refuse them. Those markers aren't shown; an `[Obsolete]` you write, on a constructor too, is.

### Missing Dependencies

A type or member that needs an assembly the app doesn't deploy, such as a package referenced only at compile time, can't be read by reflection. A base type, an interface or an attribute from such an assembly leaves the type out; a parameter, property type or attribute leaves the member out. Each one gets a warning that names it and the missing assembly, and the rest of the API reference is built as usual:

```text
Type 'Contoso.Api.Handler' in assembly 'Contoso.Api': left out of the API reference. It needs assembly 'Contoso.Contracts, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null', which could not be found.
```

To document those types, deploy the assembly with the app.

The pages use the same renderer as the CLI, so the rendering notes in [API Documentation](/guide/api-docs) apply here too. As with the CLI, a type page lists the members it inherits and the extension methods and extension block members that apply to it, when the types that declare them are in `Assemblies`, and a static class's page groups its extension block members by block (see [Extension Blocks](/guide/api-docs#extension-blocks)). Compared with the CLI, the View Source section is opt-in (see View Source), and the `/api` page has no NuGet install widget.

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
- `*` is not a wildcard: an entry already covers the namespaces below it. An entry with `*`, or one that matches no namespace in `Assemblies`, gets a warning.

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

Each file in `XmlDocFiles` is matched to an assembly by the `<doc><assembly><name>` element inside it, so its file name doesn't matter, and it's read instead of the file next to that assembly's DLL. Two files for the same assembly are combined. A relative path resolves from the content root. A file that doesn't exist, or that documents an assembly not in `Assemblies`, gets a warning. So does an XML file that can't be read, here or next to a DLL, with the parser's reason. `XmlDocFiles` is ignored when `IncludeXmlDocs` is `false`.

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

A type whose source can't be found has no View Source section. Members are left out of the section by the same rules as the page, `[EditorBrowsable(EditorBrowsableState.Never)]` included, on enum members too.

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

The build reads the Markdown files from the folder and the files the site publishes as they are: images, PDFs, MP4 and WebM video, fonts, CSS, JavaScript, JSON, XML, text and `.webmanifest`, plus `CNAME`, `_redirects` and `_headers` at the top. Files and folders whose names start with a dot are skipped, except `.well-known`. A file that can't be read, such as one another process holds open, is left out with a warning, and the rest of the site builds.

Point `DocsPath` at a folder that holds only the docs. The app's settings and build output never reach the site: `appsettings*.json`, `launchSettings.json`, `secrets.json`, `*.deps.json`, `*.runtimeconfig.json`, `*.dll`, `*.pdb` and the `bin`, `obj` and `node_modules` folders are left out wherever they are. When `DocsPath` is the content root (`"."`) or holds any of these, the build logs one warning listing what it left out.

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

### Theme Styles

`Style` picks the whole look of the pages, the same ten styles as `theme.options.style` in `mokadocs.yaml` (see [Styles](/themes/customization#styles)): `default`, `moka` (dense and dark first), `minimal`, `classic`, `soft`, `terminal` (monospace and dark first), `paper`, `contrast`, `glass` and `brutal`. Readers can switch styles in the header's Appearance panel, and their pick is saved in the browser. `StyleSelector = false` removes the panel's Style section, so every reader gets `Style`.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Style = "classic";
    options.StyleSelector = false;
});
```

### Page Layout

`Layout` has a property for each part of the page that `theme.options.layout` in `mokadocs.yaml` sets, with the same variants (see [`options.layout`](/configuration/site-config#options.layout)): `Width`, `Navbar`, `Footer`, `Toc`, `Breadcrumbs`, `PageNav`, `Sidebar`, `SidebarScope`, `Home`, `Hero`, `HeroBackground`, `Features`, `Admonitions`, `Tables`, `Density`, `Font`, `Headings` and `Links`. Each starts as `"default"`. Readers get these unless `LayoutSelector` is on, which lets them switch every part of a docs page but `Width`, `Home`, `Hero`, `HeroBackground` and `Features` in the Appearance panel.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Layout.Width = "wide";
    options.Layout.Navbar = "tabs";
    options.Layout.Toc = "floating";
    options.Layout.SidebarScope = "section";
    options.Layout.Admonitions = "github";
    options.Layout.Headings = "numbered";
});
```

### Theme Presets

`Preset` applies one of the presets of `theme.options.preset` in `mokadocs.yaml` (see [Theme Presets](/themes/customization#theme-presets)): a style, layout variants and, for some, a color preset and a code theme.

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Preset = "reference";
    options.Layout.Footer = "minimal"; // wins over the preset's columns footer
});
```

The options are plain properties, so the host can't tell one you never set from one you set to its default value. The preset fills in every option that is still at its default: `Layout.Footer = "default"` takes the preset's footer, as if it weren't set. To keep a default part, leave `Preset` unset and set the parts you want instead. Options the host has no property for, such as the code theme and the starting color preset, come from the preset as well.

### Announcement Banner

`Banner` shows a one-line announcement above the header of every page, like `theme.options.banner` (see [Announcement Banner](/themes/customization#announcement-banner)):

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Banner = new MokaDocsBannerOptions
    {
        Text = "Version 2.0 is out",
        Link = "/blog/2-0",
        LinkText = "Read the release notes",
        Variant = "accent" // "info" (default), "warning" or "accent"
    };
});
```

`Text` is plain text, and an empty one shows no banner. A `Link` that starts with `/` is a path from the site root and gets `PathBase` and `BasePath`, so the example above links to `/docs/blog/2-0`. `Id` decides when a reader who closed the banner sees it again: without it, changing `Text` or `Link` shows it again. `Dismissible = false` removes the close button.

### Languages

`Language` puts the theme's text in another language and sets `<html lang>`; `Strings` changes any text; `Direction` writes the pages right to left. They work like `site.language`, `theme.options.strings` and `site.direction` in mokadocs.yaml (see [Languages](/guide/languages)):

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.Language = "de";
    options.Strings["toc.title"] = "Inhalt";
});
```

The page the app shows while the first build runs is in that language too. The API section of the default sidebar is named like the API index page (`api.title`), and the other default sidebar labels ("Guide", the blog's title) stay as they are; set `Nav` to name them yourself.

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

### Link Previews

Chat apps and social networks show a card for a shared link when the page has Open Graph and Twitter tags. Those tags need absolute URLs, and the app can't tell from a request which URL readers use, so the host writes them only when `SiteUrl` names it:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.DocsPath = "Docs";
    options.SiteUrl = "https://contoso.com";
    options.SocialImage = "images/social-card.png";
});
```

With `SiteUrl` set, every page gets a canonical link and `og:` and `twitter:` tags, and the links in `llms.txt` are absolute. `PathBase` and `BasePath` are added to it unless it already ends with them, so the page at `/docs/guide/intro` gets `https://contoso.com/docs/guide/intro/` as its canonical URL.

`SocialImage` is the picture for pages that don't set a front matter `image`. A path is a file under `DocsPath`, published at the same place in the site: `Docs/images/social-card.png` above becomes `https://contoso.com/docs/images/social-card.png`. A file outside `DocsPath` isn't published, and the build logs a warning. For an image the app serves itself, give its absolute URL.

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

A `PluginEntry` has no `path`, unlike a `plugins:` entry in `mokadocs.yaml`: this host doesn't load plugin assemblies. Reference the plugin's project or package from the application and register it as above. A plugin project made with `mokadocs new plugin` works in both.

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

  In Development, when the preview host's publish output is missing or older than its sources, the plugin runs `dotnet publish` during the build, which needs the .NET SDK on the machine serving the docs. In every other environment the host passes the plugin `publishHost: false`: it never scaffolds a preview host or runs `dotnet publish` inside the running app. It uses a preview host published beforehand, for example as a step of the app's own build, and without one it logs a warning and the pages show the components' static snapshots. To publish anyway, set `["publishHost"] = true` in the entry. The published host is read once per change to its files and served from memory under `{BasePath}/_preview-wasm/`.
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

`MapMokaDocs()` returns the group, so endpoint conventions such as `RequireAuthorization` chain onto it and cover every docs route (see [Security](#security)). For the feedback or REPL endpoint alone, see [Endpoint Conventions](#endpoint-conventions).

Search works under the base path: the page script loads `{BasePath}/search-index.json` (with a `?v=` hash of the file), and result links include the base path.

For each request, the handler looks for an exact file match first, then tries appending `/index.html`, and then `.html`. A path with no match gets status 404 and the site's `404.html` page.

Each file's content type comes from its extension: UTF-8 text types for HTML, CSS, JavaScript, JSON, text and YAML, `application/wasm` for WebAssembly, and ASP.NET Core's static file map for the rest, such as PDF, MP4, WebM, AVIF and `.webmanifest`.

### Compression

Each build compresses the files worth compressing once, with Brotli and gzip at the optimal level: HTML, CSS, JavaScript, JSON, XML, SVG, text, Markdown and WebAssembly files of 1 KB or more. A rebuild reuses the compressed forms of files that didn't change. A request gets the form its `Accept-Encoding` header ranks highest, Brotli first when two rank the same, with a `Content-Encoding` header; a coding the header gives `q=0` is never sent, and without the header the file goes out as it is. Every file that has compressed forms is sent with `Vary: Accept-Encoding`, whichever form the request gets, so a shared cache keeps them apart. Images, fonts, video and archives are compressed already and go out as they are.

The Blazor preview host's `.br` and `.gz` files serve as the Brotli and gzip forms of the file they sit next to, once the build has checked that they decompress to it. They aren't served at URLs of their own.

The files hold no per-request data, so compressing them over HTTPS doesn't open the door to BREACH, which needs a secret and attacker-controlled text in the same response. An app that calls `UseResponseCompression()` for its own endpoints doesn't compress the docs twice: the middleware skips a response that already has a `Content-Encoding`.

### Cache Headers

Every response has a strong `ETag`. A compressed form has a tag of its own, computed from its bytes, so a tag names exactly what the client holds. A request whose `If-None-Match` names the tag of the form it would get receives `304 Not Modified` with no body. An unchanged file keeps its tags across rebuilds.

`Cache-Control` depends on whether the URL names the content:

| URL | `Cache-Control` |
|---|---|
| A `?v=` that matches the file's content hash: the theme's CSS, JavaScript and icon sprite, the shared sidebar script, `search-index.json` as pages request it | `public, max-age=31536000, immutable` |
| `{BasePath}/_preview-wasm/_framework/` files with a fingerprint in the name, such as `dotnet.native.3bptkeu1pi.wasm` | `public, max-age=31536000, immutable` |
| `{BasePath}/_preview-assemblies/{hash}.bin`, named after the SHA-256 hash of its bytes | `public, max-age=31536000, immutable` |
| Everything else: pages, `search-index.json` without its `?v=`, the preview host's `index.html`, `dotnet.js` and an unfingerprinted `blazor.webassembly.js`, images, and a `?v=` that doesn't match | `no-cache` |

A browser keeps the first kind for a year without asking again: a rebuild that changes the file changes its URL too. It revalidates the rest on each use for the cost of a `304`, so a rebuilt page, or the theme of a new MokaDocs version, shows up right away. When the request is authenticated, `public` becomes `private`, so a shared cache doesn't keep docs that only signed-in readers may see.

### Response Headers

Every docs response has `X-Content-Type-Options: nosniff`. Files under `{BasePath}/_preview-wasm/`, the Blazor preview host the pages load in an iframe, also get `Content-Security-Policy: frame-ancestors 'self'`, so only pages of the same origin can frame it, and `Cross-Origin-Opener-Policy: same-origin`. The rest of the preview host's policy, with the hashes of its inline scripts, is in a meta tag in its `index.html`; a `script-src` in the header would have to allow the same hashes, so the header leaves it out.

Range requests aren't supported: a request with a `Range` header gets the whole file.

### Path Base

The site is built once and kept in memory, so its links can't follow the path base of each request. When the app isn't at the root of its host, set `PathBase` to the part of the URL in front of the app's routes, the same value that ends up in `HttpRequest.PathBase`:

- an IIS sub-application at `/shop`
- `app.UsePathBase("/shop")` in front of `UseRouting()`
- a reverse proxy that forwards `/shop/...` with `X-Forwarded-Prefix: /shop`, applied by `UseForwardedHeaders()` with `ForwardedHeaders.XForwardedPrefix`

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.PathBase = "/shop";
});

var app = builder.Build();
app.UsePathBase("/shop");
app.UseRouting();
app.MapMokaDocs();
```

Links, styles, scripts and search results then start with `/shop/docs`. A request that arrives with a different path base, or with none when `PathBase` is set, is still served, but the pages it gets load without styles or working links. The first such request for each path base logs a warning that names it.

### Endpoint Conventions

Conventions on the group `MapMokaDocs()` returns apply to every docs route. To limit the feedback or REPL endpoint alone, set `ConfigureFeedbackEndpoint` or `ConfigureReplEndpoint`. `MapMokaDocs()` calls them with each endpoint it maps:

```csharp
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddFixedWindowLimiter("votes", window =>
    {
        window.PermitLimit = 10;
        window.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddMokaDocs(options =>
{
    options.ConfigureFeedbackEndpoint = endpoint => endpoint.RequireRateLimiting("votes");
    options.ConfigureReplEndpoint = endpoint => endpoint.RequireAuthorization("Admins");
});

var app = builder.Build();
app.UseRateLimiter();
app.MapMokaDocs();
```

`ConfigureReplEndpoint` isn't called when the REPL endpoint isn't mapped, and `ConfigureFeedbackEndpoint` isn't called when `ShowFeedback` is `false`. With `UseRouting()` in the app, call `UseRateLimiter()` after it.

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

The snippet limits are the same as under `mokadocs serve`: 10,000 characters of code, 5 seconds per run and 100,000 characters of output, return value and error message (see [Security](/plugins/repl#security) on the REPL page). The endpoint reads at most 64 KB of request body. Runs happen one at a time and up to four more wait; past that, the endpoint answers `429` with the reason in `error`.

### The worker command

MokaDocs starts the worker with:

- `dotnet MyApp.dll`, when the app runs as `dotnet MyApp.dll`
- the app's own executable, when it runs from it (`MyApp.exe`, `dotnet run`, a single-file app)
- `dotnet MyApp.dll` otherwise, such as under IIS in-process hosting, with the `dotnet` named by `DOTNET_HOST_PATH` or found on the `PATH`

For any other host, set `ReplWorkerStartInfo` to a function that returns the command. The process has to call `ReplWorker.RunAsync` with its standard input and output, which is what `TryRunAsync` does when the `MOKADOCS_REPL_WORKER` environment variable is `1`. MokaDocs adds that variable and `MOKADOCS_REPL_PARENT_PID` to the start info, so a worker exits when the web app does:

```csharp
options.ReplWorkerStartInfo = () => new ProcessStartInfo("/opt/shop/Contoso.Shop");
```

With `ReplWorkerStartInfo` set, MokaDocs doesn't check for the line in `Program.cs`.

### Under dotnet watch

The worker starts from your running app, so it would inherit the environment `dotnet watch` gives the app: `DOTNET_STARTUP_HOOKS` with the hot reload and browser refresh agents, `DOTNET_MODIFIABLE_ASSEMBLIES=debug`, `DOTNET_WATCH*`, `ASPNETCORE_AUTO_RELOAD_*`, `__ASPNETCORE_BROWSER_TOOLS` and the browser refresh entry in `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES`. MokaDocs removes all of them from the worker's start info, a `ReplWorkerStartInfo` command included, so the worker doesn't load the agents and doesn't run in hot reload mode. A startup hook you set yourself is removed from the worker too; the worker runs snippets and needs none.

Hot reload changes the running app in memory and leaves the build output on disk alone. The worker is a separate process that loads the assemblies in `Assemblies` from disk, so snippets run the code of the app's last build, not edits hot reload applied since. They catch up when `dotnet watch` rebuilds and restarts the app, after an edit hot reload can't apply or when you press `Ctrl+R`; the old worker exits with the old app and the next Run starts a new one.

### Remote REPL

Outside Development, `EnableRepl` keeps the Run buttons but maps no endpoint and logs a warning; a click shows "REPL server unavailable". In Development, the endpoint runs code only for a request that comes from this machine, and anything else gets a 403, an error in the output panel and a warning in the log naming the reason. A request comes from this machine when:

- `HttpContext.Connection.RemoteIpAddress` is a loopback address. That covers an app that listens on every interface, as apps in containers often do: the origin checks stop other web pages, not other machines.
- The `Host` header is `localhost`, a name under `.localhost`, or a loopback address such as `127.0.0.1` or `[::1]`, on any port. A page on another site can point its own DNS name at `127.0.0.1` (DNS rebinding); its requests then reach the app from a loopback address with that site's name in `Host` and `Origin`, which the origin checks accept.
- It has no `Forwarded`, `X-Forwarded-For` or `X-Forwarded-Host` header, and none of the `X-Original-*` headers `UseForwardedHeaders()` leaves behind. A proxy or tunnel on the same machine, such as nginx, ngrok or a dev tunnel, makes every caller look local.

So behind a reverse proxy, even one on the same machine, the REPL needs `AllowRemoteRepl`.

To allow other machines and proxies, set `AllowRemoteRepl = true` and put the docs behind authorization that only people you would trust with a shell on the server can pass:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.EnableRepl = true;
    options.AllowRemoteRepl = true;
});

var app = builder.Build();
app.MapMokaDocs().RequireAuthorization("Admins");
```

To keep the pages open to everyone and put only the REPL behind authorization, set `options.ConfigureReplEndpoint = endpoint => endpoint.RequireAuthorization("Admins")` instead (see [Endpoint Conventions](#endpoint-conventions)).

The worker process isolates hangs and crashes. It is not a sandbox: a snippet can read and write the same files, secrets and network as the app.

## Production Considerations

### Caching

By default, `CacheOutput` is `true` and the site is built in the background as soon as the app has started (`BuildOnStartup`). The build starts from `IHostApplicationLifetime.ApplicationStarted`, after every endpoint is mapped. Every file is kept in memory as a byte array, with its compressed forms, and later requests are served from memory without touching the disk.

Docs requests never wait for a build. One that arrives before the first build is done gets `503 Service Unavailable` with `Retry-After: 2` and `Cache-Control: no-store`. A browser gets a short page that reloads itself after two seconds; other clients get a line of text. With `BuildOnStartup = false`, the first request starts the build in the background and gets the same answer. Requests that arrive during the build don't start builds of their own. An app that calls `AddMokaDocs()` but never `MapMokaDocs()`, for example one that maps the docs in Development only, builds nothing at startup.

A build that fails, at startup or later, is logged once. Requests then get the last site that built, or `503` with a `Retry-After` header when no build has succeeded yet, and none of them builds again for the next 5 seconds; after that, the next request tries again.

To rebuild while the app runs, resolve `MokaDocsService` from DI. `RebuildAsync()` builds a new site while requests keep getting the current one, then swaps it in. `InvalidateCache()` drops the site; the next request starts a build and, like the requests after it, gets the site from before until the build is done, or when it fails. `GetSiteAsync()` waits for a build when there is no site. Callers that arrive during a build share it, and one that stops waiting doesn't stop the build for the others. It throws `InvalidOperationException` when the build fails and no earlier build succeeded:

```csharp
app.MapPost("/admin/refresh-docs", async (MokaDocsService docsService) =>
{
    await docsService.RebuildAsync();
    return Results.Ok("Documentation rebuilt");
}).RequireAuthorization("Admins");
```

### Development Mode

In Development, MokaDocs watches the `DocsPath` folder and rebuilds the site in the background when a file in it changes. Requests get the previous site until the new one is ready, then the new one replaces it. Changes made in quick succession start one rebuild, and changes made during a rebuild start one more when it ends. Files and folders whose names start with a dot are ignored, and so are the `bin`, `obj` and `node_modules` folders directly under `DocsPath`. A rebuild that fails is logged, and the previous site stays.

Where the operating system can't watch the folder, such as on Linux past the inotify limits, MokaDocs logs a warning and checks it for changes every second instead. Setting `DOTNET_USE_POLLING_FILE_WATCHER` to `1` or `true` polls from the start, as it does for ASP.NET Core's own file watching.

`WatchDocs` controls this. Its default, `null`, watches in Development only; `true` watches in every environment and `false` turns it off:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.DocsPath = "Docs";
    options.WatchDocs = true; // a staging server that edits docs in place
});
```

API pages come from reflection over the assemblies loaded in the running app, so code changes show up only after a restart.

`CacheOutput = false` still rebuilds the site on every request, and each request waits for its build. Nothing is built at startup or watched then.

### Performance

Once the site is built, serving a page is a dictionary lookup followed by writing the stored bytes, or their compressed form, to the response. Nothing is compressed while a request waits. With `CacheOutput = false`, every request runs a full build, so keep caching on in production.

A Blazor preview host is hundreds of files and tens of megabytes. The build reads it once, and a rebuild reuses those files until a file in the published folder is added, removed or rewritten.

### Security

`MapMokaDocs()` returns the route group that holds every docs endpoint, so `RequireAuthorization` chains onto it. The requirement covers every docs route, including CSS and JavaScript files, the search index and the POST endpoints:

```csharp
app.MapMokaDocs().RequireAuthorization("InternalOnly");
```

The `InternalOnly` policy has to be registered with `AddAuthorization`. Other endpoint conventions go on the group the same way. A group that the docs are mapped into works too: `app.MapGroup("").RequireAuthorization("InternalOnly").MapMokaDocs()`. For conventions on the feedback or REPL endpoint alone, such as a rate limit on votes, see [Endpoint Conventions](#endpoint-conventions).

The feedback and REPL endpoints accept a POST only from the docs' own pages, or from a client that isn't a browser:

- `Content-Type` has to be `application/json`. A page on another site can't send that without a CORS preflight, and nothing answers one.
- A `Sec-Fetch-Site` header, when present, has to be `same-origin`.
- An `Origin` header, when present, has to match the scheme, host and port the request was sent to.

Anything else gets `403`, and the log gets a warning. Requests without either header, such as from `curl`, are accepted. Behind a proxy that terminates TLS, call `app.UseForwardedHeaders()` so the request's scheme and host match what the browser sees, or every vote and Run from a browser gets `403`. The REPL also needs `AllowRemoteRepl` behind any proxy (see [Remote REPL](#remote-repl)).

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
