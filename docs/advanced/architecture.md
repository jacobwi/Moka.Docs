---
title: Build Pipeline & Architecture
order: 2
---

# Build Pipeline & Architecture

MokaDocs builds a site by running a fixed sequence of phases over one shared `BuildContext`. This page covers the phases, the context and models they pass along, the variables layout templates receive, and how build problems are reported.

## Pipeline Overview

`BuildPipeline` runs every registered `IBuildPhase` in ascending `Order`:

```csharp
public interface IBuildPhase
{
    string Name { get; }
    int Order { get; }
    Task ExecuteAsync(BuildContext context, CancellationToken ct = default);
}
```

Phases run one after another and read and write the same `BuildContext`. A phase that hits a problem records a [diagnostic](#diagnostics) and lets the build continue. The exception is a `build.output` that is, or contains, the project folder or the docs folder: the build stops before deleting anything.

The pipeline also has a plugin hook. It runs once, just before the first phase with an order of 500 or higher.

## Build Phases

| Order | Phase | What it does |
|-------|-------|--------------|
| 200 | `DiscoveryPhase` | Lists Markdown files and static assets under `content.docs`, skipping anything inside `build.output`. Checks which `content.projects` files exist and resolves `site.logo` and `site.favicon` |
| 300 | `CSharpAnalysisPhase` | Reads the `.cs` files of each configured project with Roslyn, or loads the result from the build cache. Adds the `/api` index page and one page per type, and reads the package name and version from the first project's `.csproj` |
| 400 | `MarkdownParsePhase` | Turns each Markdown file into a `DocPage`: front matter, HTML, table of contents and route |
| | Plugin hook | Runs the plugins declared under `plugins:` in `mokadocs.yaml` |
| 500 | `FeatureGatePhase` | Removes pages whose `requires` feature flag is disabled |
| 600 | `NavigationBuildPhase` | Builds the sidebar tree from the `nav` section, or from page routes when there is no `nav` |
| 700 | `SearchIndexPhase` | Creates a search entry for each page and for each heading in its table of contents. Skipped when `features.search.enabled` is false |
| 900 | `RenderPhase` | Renders each page through its layout template |
| 1100 | `OutputPhase` | Checks the output directory and duplicate routes, deletes the output directory when `build.clean` is on, then writes pages, section redirect pages, `404.html`, `.nojekyll`, `search-index.json`, static assets and files registered by plugins |
| 1150 | `ThemeAssetPhase` | Writes the default theme's `_theme/css/main.css` and `_theme/js/main.js`, or copies a custom theme's `css/`, `js/` and `assets/` folders into `_theme/` |
| 1200 | `PostProcessPhase` | Writes `sitemap.xml` (only when `site.url` is set) and `robots.txt` |

`--verbose` logs each phase by its `Name` as it starts: `Discovery`, `CSharpAnalysis`, `MarkdownParse`, `FeatureGate`, `NavigationBuild`, `SearchIndex`, `Render`, `Output`, `ThemeAssets` and `PostProcess`.

A few details the table leaves out:

- Plugins run before `FeatureGatePhase`, so pages a plugin adds go through feature gating, navigation, search and rendering like any other page. Only plugins declared in the configuration run. See [Plugin System](/plugins/overview).
- Pages with `visibility: draft` are skipped by `SearchIndexPhase`, `RenderPhase` and `OutputPhase` unless the build runs with `--draft`.
- In a dry run (`mokadocs validate` and `mokadocs doctor`), `OutputPhase`, `ThemeAssetPhase` and `PostProcessPhase` return before writing anything. `OutputPhase` still reports an unsafe output directory and duplicate routes first.
- `ApiPageRenderer` is not a phase. It's a static helper that `CSharpAnalysisPhase` calls to build the HTML of each type page.
- The [ASP.NET Core integration](/guide/aspnetcore) registers one more phase, `ReflectionApiPagePhase` at order 350. It generates API pages from the model the host builds by reflection over loaded assemblies, when `CSharpAnalysisPhase` produced none.

## BuildContext

`BuildContext` holds the state every phase reads and writes. The host that runs the build (the CLI or the ASP.NET Core integration) creates it and sets the `required` members; phases fill in the rest. Members grouped by purpose:

```csharp
public sealed class BuildContext
{
    // Set by the host
    public required SiteConfig Config { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required string RootDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public bool DryRun { get; init; }
    public bool UseCache { get; init; } = true;
    public bool IncludeDrafts { get; init; }

    // Content
    public List<DocPage> Pages { get; } = [];
    public ApiReference? ApiModel { get; set; }
    public PackageMetadata? PackageInfo { get; set; }

    // Navigation and search
    public NavigationTree? Navigation { get; set; }
    public SearchIndex? SearchIndex { get; set; }

    // Versioning
    public DocVersion? CurrentVersion { get; set; }
    public List<DocVersion> Versions { get; } = [];

    // Diagnostics
    public DiagnosticBag Diagnostics { get; } = new();

    // Discovery results
    public List<string> DiscoveredMarkdownFiles { get; } = [];
    public List<string> DiscoveredProjectFiles { get; } = [];
    public List<string> DiscoveredAssetFiles { get; } = [];
    public Dictionary<string, string> BrandAssetFiles { get; } = new(StringComparer.Ordinal);

    // Extra output registered by plugins
    public Dictionary<string, byte[]> DeferredOutputFiles { get; } = new();
    public List<(string SourceDir, string DestRelPath)> DeferredOutputDirectories { get; } = [];
}
```

### Property Details

| Property | Set by | Notes |
|----------|--------|-------|
| `Config` | Host | The parsed `mokadocs.yaml`, with CLI overrides such as `--output` and `--base-path` applied |
| `FileSystem` | Host | A `System.IO.Abstractions` file system. The CLI passes the real disk and the ASP.NET Core host passes an in-memory `MockFileSystem`, so phases must use it instead of `System.IO` |
| `RootDirectory` | Host | The folder containing `mokadocs.yaml`. Relative paths in the configuration resolve against it |
| `OutputDirectory` | Host | The absolute `build.output` path |
| `DryRun` | Host | True for `mokadocs validate` and `mokadocs doctor`. Phases that write output return early |
| `UseCache` | Host | False with `--no-cache` or `build.cache: false`. Controls whether cached C# analysis is reused |
| `IncludeDrafts` | Host | True with `--draft` |
| `Pages` | `CSharpAnalysisPhase`, `MarkdownParsePhase`, plugins | `FeatureGatePhase` removes pages. `RenderPhase` replaces each page's `Content.Html` with the fully rendered document |
| `ApiModel` | `CSharpAnalysisPhase` | All analyzed projects merged by namespace, with `<inheritdoc/>` resolved. The ASP.NET Core host sets it before the build starts |
| `PackageInfo` | `CSharpAnalysisPhase` | `Name` and `Version` from the first project's `.csproj`, shown by the NuGet install widget on `/api` |
| `Navigation` | `NavigationBuildPhase` | The sidebar tree |
| `SearchIndex` | `SearchIndexPhase` | Null when search is disabled |
| `CurrentVersion`, `Versions` | Host | The CLI fills them from `features.versioning` when versioning is enabled |
| `Diagnostics` | Any phase or plugin | See [Diagnostics](#diagnostics) |
| `DiscoveredMarkdownFiles` | `DiscoveryPhase` | Paths relative to `content.docs` |
| `DiscoveredProjectFiles` | `DiscoveryPhase` | Absolute paths of the `content.projects` files that exist |
| `DiscoveredAssetFiles` | `DiscoveryPhase` | Paths relative to `content.docs` |
| `BrandAssetFiles` | `DiscoveryPhase` | Logo and favicon files to copy. The key is the publish URL (such as `/assets/logo.png`), the value the absolute source path |
| `DeferredOutputFiles` | Plugins | File contents keyed by output-relative path. `OutputPhase` writes them after cleaning the output directory |
| `DeferredOutputDirectories` | Plugins | Folders that `OutputPhase` copies into the output directory |

## Key Models

### DocPage

A page, whether it came from Markdown, C# analysis or a plugin.

```csharp
public sealed record DocPage
{
    public required FrontMatter FrontMatter { get; init; }
    public required PageContent Content { get; set; }
    public TableOfContents TableOfContents { get; init; } = TableOfContents.Empty;
    public string? SourcePath { get; init; }        // relative to content.docs, null for generated pages
    public required string Route { get; init; }     // e.g. "/guide/getting-started"
    public PageOrigin Origin { get; init; } = PageOrigin.Markdown;   // Markdown or ApiGenerated
    public DateTimeOffset? LastModified { get; init; }
}

public sealed record FrontMatter
{
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public int Order { get; init; }
    public string? Icon { get; init; }
    public string Layout { get; init; } = "default";
    public List<string> Tags { get; init; } = [];
    public PageVisibility Visibility { get; init; } = PageVisibility.Public;   // Public, Hidden, Draft
    public bool Toc { get; init; } = true;
    public bool Expanded { get; init; } = true;
    public string? Route { get; init; }
    public string? Version { get; init; }
    public string? Requires { get; init; }
}
```

- `FrontMatter` has only the properties above. Front matter keys MokaDocs doesn't recognize are ignored, and there is no collection of custom fields.
- `PageContent` has `Html` and `PlainText`. After `MarkdownParsePhase`, `Html` is the page body; after `RenderPhase` it's the whole HTML document. `PlainText` feeds the search index.
- `TableOfContents.Entries` is a list of `TocEntry` records with `Level`, `Text`, `Id` and `Children`.
- `Route` comes from the file path (`guide/intro.md` becomes `/guide/intro`, and `index.md` takes its folder's route) unless front matter sets `route`. A `route` value always gets a leading slash and loses any trailing slash. `OutputPhase` writes each page to `{route}/index.html`.
- Front matter that isn't valid YAML, or has a value of the wrong type, is ignored with a warning. The page is then titled `Untitled` and the front matter block stays in its content.
- `LastModified` is the Markdown file's last write time on disk. Generated pages have none.

### NavigationNode

An item in the sidebar tree. `NavigationTree.Items` holds the top-level nodes.

```csharp
public sealed record NavigationNode
{
    public required string Label { get; init; }
    public string? Route { get; init; }       // null for a nav group with no path
    public string? Icon { get; init; }        // icon name
    public int Order { get; init; }
    public bool Expanded { get; init; } = true;
    public bool IsActive { get; init; }
    public List<NavigationNode> Children { get; init; } = [];
}
```

The tree is built once per build, not once per page, so the engine never sets `IsActive`. The template engine works out the current page while rendering and exposes it as `is_active` and `has_active_child` on each `nav` item. It also turns `Icon` into SVG markup. An icon name that isn't in MokaDocs' icon set renders nothing, and `NavigationBuildPhase` records a warning for it. See [Navigation](/configuration/navigation) for how the tree is built.

### SearchEntry

An entry in the search index.

```csharp
public sealed record SearchEntry
{
    public required string Title { get; init; }      // page title
    public string? Section { get; init; }            // heading text, on heading entries
    public required string Route { get; init; }      // "/guide/intro" or "/guide/intro#setup"
    public required string Content { get; init; }    // page plain text, empty on heading entries
    public string Category { get; init; } = "Documentation";   // "Documentation" or "API Reference"
    public List<string> Tags { get; init; } = [];    // front matter tags, on page entries
}
```

Each page gets one entry, plus one entry per heading with an anchor route. `OutputPhase` writes the index to `search-index.json` with short keys (`t`, `s`, `r`, `c`, `g`, `k`), adds the base path to each route and keeps the first 300 characters of `Content`. See [Search](/guide/search).

### ApiType

A C# type from the analysis phase. `ApiReference.Namespaces` holds `ApiNamespace` records, and each namespace holds its `Types`.

```csharp
public sealed record ApiType
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required ApiTypeKind Kind { get; init; }   // Class, Struct, Record, Interface, Enum, Delegate
    public ApiAccessibility Accessibility { get; init; } = ApiAccessibility.Public;
    public bool IsStatic { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsSealed { get; init; }
    public bool IsRecord { get; init; }
    public List<ApiTypeParameter> TypeParameters { get; init; } = [];
    public string? BaseType { get; init; }
    public List<string> ImplementedInterfaces { get; init; } = [];
    public List<ApiMember> Members { get; init; } = [];
    public XmlDocBlock? Documentation { get; init; }
    public List<ApiAttribute> Attributes { get; init; } = [];
    public string? Namespace { get; init; }
    public string? Assembly { get; init; }
    public string? SourcePath { get; init; }
    public bool IsObsolete { get; init; }
    public string? ObsoleteMessage { get; init; }
    public string? SourceCode { get; init; }
}
```

- `ApiMember` describes a constructor, method, property, field, event, operator or indexer, with its `Signature`, `Parameters` and `Documentation`.
- `XmlDocBlock` holds the parsed doc comment: `Summary`, `Remarks`, `Parameters`, `TypeParameters`, `Returns`, `Value`, `Exceptions`, `Examples` and `SeeAlso`.
- `ApiTypeParameter` has a `Name` and its `Constraints`. `ApiAttribute` has a `Name` and its `Arguments`.

## Template Engine

Layouts are [Scriban](https://github.com/scriban/scriban) templates: `{{ page.title }}` expressions, `{{ if }}` and `{{ for }}` blocks. `RenderPhase` picks the layout named by the page's `layout` front matter and `ScribanTemplateEngine` renders it with the variables below. Parsed templates are cached by layout name for the life of the process.

When the theme has no layout with that name, the page uses `default` instead, and `RenderPhase` records one warning per missing layout name. `RenderPhase` also records a warning when `theme.name` points at a folder that doesn't exist or has no `layouts/*.html`, in which case the default theme is used.

When a layout has a syntax error or throws while rendering, the page is written without the layout, and `RenderPhase` records an error: one per distinct message, with the number of pages it affected and an example route.

### Template Variables

| Variable | Contents |
|----------|----------|
| `page` | The page being rendered (fields below) |
| `site` | Values from `site:` in `mokadocs.yaml` (fields below) |
| `theme` | Values from `theme.options` (fields below) |
| `nav` | Sidebar items with `label`, `route`, `icon` (SVG markup), `expanded`, `is_active`, `has_active_child`, `has_children`, `has_page` and `children` |
| `breadcrumbs` | Trail items with `label`, `url`, `is_current` and `has_page`, starting with Home |
| `prev_page`, `next_page` | `title` and `route` of the previous and next page, when there is one |
| `base_path` | `build.basePath`, or an empty string for a site at the root |
| `search_enabled` | True when `features.search.enabled` and `theme.options.showSearch` are both true |
| `css_files`, `js_files` | Theme stylesheet and script URLs, base path included |
| `partials` | The theme's partials as raw strings, keyed by file name |
| `versions` | Configured versions with `label`, `slug`, `is_default` and `is_prerelease`. Empty without versioning |
| `current_version` | Label of the version being built, or an empty string |
| `package` | `name` and `version` from the first C# project. Only set when a project was analyzed |
| `edit_url` | The "Edit this page" URL. Only set when `site.editLink` is configured, `theme.options.showEditLink` is true and the page has a source file, so generated pages have none |
| `mokadocs_version` | The MokaDocs version, such as `1.6.0` |

`page` fields:

| Field | Contents |
|-------|----------|
| `page.title`, `page.description` | Front matter values |
| `page.meta_description` | `description`, or `site.description` when the page has none |
| `page.canonical_url` | Absolute URL built from `site.url`, the base path and the route. Empty without `site.url` |
| `page.content` | The page HTML, with root-relative links prefixed by the base path |
| `page.route` | The route, base path included |
| `page.toc` | Headings with `level`, `text`, `id` and `children` |
| `page.show_toc` | True when front matter `toc` and `theme.options.showTableOfContents` are true and the page has headings |
| `page.tags`, `page.layout` | Front matter values |
| `page.source_path` | Path relative to `content.docs` with forward slashes, empty for generated pages |
| `page.last_modified` | The date as `yyyy-MM-dd`, or an empty string |
| `page.is_api`, `page.is_api_index` | Whether the page was generated by C# analysis, and whether it is `/api` |

`site` fields: `title`, `description`, `url`, `copyright` (with `{year}` replaced), `logo` and `favicon` (the raw values from `mokadocs.yaml`), `logo_url` and `favicon_url` (resolved URLs, with the base path added for local files), and `repo_url` (`site.editLink.repo`).

`theme` fields: `primary_color`, `accent_color`, `code_theme`, `code_style`, `default_color_theme`, `initial_color_theme` (the preset applied on a first visit, empty when none is), `toc_depth`, `social_links` (each with `icon`, `url` and `icon_svg`), and one boolean per option: `color_themes`, `code_theme_selector`, `code_style_selector`, `show_edit_link`, `show_last_updated`, `show_feedback`, `show_dark_mode_toggle`, `show_animations`, `show_search`, `show_table_of_contents`, `show_prev_next`, `show_breadcrumbs`, `show_back_to_top`, `show_copy_button`, `show_line_numbers`, `show_version_selector` and `show_built_with`.

There is no variable for the search index. The default theme's script fetches `{base_path}/search-index.json` when the search dialog opens.

### Partials

Scriban's `include` needs a template loader, and MokaDocs doesn't configure one. A layout that calls `{{ include 'footer' }}` fails to render, and the build reports an error as described in [Template Engine](#template-engine).

Files in a theme's `partials/` folder are available as `partials.<name>`, where the name is the file name without `.html`. They are raw strings, so the file is inserted as written and any Scriban expressions inside it are not evaluated:

```html
<body>
    {{ partials.header }}
    <main>{{ page.content }}</main>
</body>
```

The default theme's partials are all empty.

## Diagnostics

Phases and plugins record problems in `BuildContext.Diagnostics`, a `DiagnosticBag`, instead of throwing. Each `Diagnostic` has a `Severity`, a `Message` and a `Source` (the phase name or plugin id).

- **Error**: output is missing or wrong. Recorded when a plugin throws during a build or calls `LogError`, when a layout fails to render, and by a dry run when `build.output` is, or contains, the project folder or the docs folder. A real build with that output setting stops before deleting anything. `mokadocs build` exits with code 1 when the build throws or records an error.
- **Warning**: the build carries on. Recorded by:
  - `MarkdownParsePhase`: a Markdown file that fails to parse, or front matter that can't be read
  - `CSharpAnalysisPhase`: a C# project that is missing or fails to analyze
  - `NavigationBuildPhase`: a sidebar icon name that isn't in the icon set
  - `RenderPhase`: a `theme.name` folder that is missing or has no layouts, a `layout` name the theme doesn't have, or a social link icon that isn't in the icon set
  - `OutputPhase`: pages that share a route (only the last one is written)
  - plugins that call `LogWarning`
- **Info**: available to phases and plugins. No built-in phase records one.

How the CLI reports them:

- `mokadocs build` prints the warning and error counts. It always lists errors, and lists warnings with `--verbose`. Each line reads `[Severity] Message`, in the order the diagnostics were recorded, without the source or a file position.
- `mokadocs validate` lists warnings and errors sorted by severity and then source, with the source name, and adds info diagnostics with `--verbose`. It exits with 2 on errors and 1 on warnings.
- `mokadocs serve` prints the same summary as `build` after every build and rebuild, and keeps serving when there are errors.

See [CLI Commands](/advanced/cli-reference) for the full command reference.
