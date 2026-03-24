---
title: Build Pipeline & Architecture
order: 2
---

# Build Pipeline & Architecture

MokaDocs uses a phased build pipeline to transform your Markdown files, C# projects, and configuration into a complete static documentation site. Understanding this architecture helps when writing plugins, debugging build issues, or contributing to MokaDocs itself.

## Pipeline Overview

The build process is orchestrated by the `BuildPipeline` class. Each phase implements the `IBuildPhase` interface, which defines three members:

```csharp
public interface IBuildPhase
{
    string Name { get; }
    int Order { get; }
    Task ExecuteAsync(BuildContext context);
}
```

Phases are sorted by their `Order` value and executed sequentially. Each phase reads from and writes to a shared `BuildContext` object.

## Build Phases

The following table lists all built-in phases in execution order:

| Order | Phase | Description |
|-------|-------|-------------|
| 200 | **DiscoveryPhase** | Scans the configured docs directory for `.md` files, locates `.csproj` project files for API analysis, and collects static assets (images, CSS, JS) |
| 300 | **CSharpAnalysisPhase** | Uses Roslyn to analyze C# projects. Extracts type information, XML documentation comments, member signatures, attributes, and package metadata from `.csproj` files |
| 400 | **MarkdownParsePhase** | Parses all discovered Markdown files using Markdig with custom extensions. Extracts YAML front matter, generates HTML content, builds per-page table of contents from headings |
| 450 | **FeatureGatePhase** | Filters pages based on the `requires` front matter field. Pages whose required feature is disabled are removed from the build before plugins or navigation see them |
| 500 | **Plugin Hook** | All registered plugins execute at this point via the `PluginHost`. Plugins can modify pages, add new pages, transform content, or extend the build context |
| 600 | **NavigationBuildPhase** | Generates the sidebar navigation tree from discovered pages and any explicit navigation configuration in `mokadocs.yaml`. Resolves ordering, nesting, icons, and active states |
| 700 | **SearchIndexPhase** | Builds the client-side search index by extracting text content from all pages and their headings. Produces a JSON index that powers the browser-based search |
| 800 | **ApiPageRenderer** | Generates HTML content for API reference pages. Transforms the `ApiType` models from the C# analysis phase into structured, navigable documentation pages |
| 900 | **RenderPhase** | Applies Scriban templates to all pages. Combines page content with layout templates, injects navigation, search data, and site-wide configuration into the final HTML |
| 1000 | **OutputPhase** | Writes all rendered HTML files to the output directory. Copies static assets (images, fonts, downloads) to the appropriate locations |
| 1100 | **ThemeAssetPhase** | Writes theme-specific CSS and JavaScript files to the output directory. Includes the search client, syntax highlighting, navigation scripts, and theme stylesheets |

## BuildContext

The `BuildContext` is the central state object that flows through every phase. Each phase can read data added by earlier phases and contribute its own data for downstream phases.

### Properties

```csharp
public class BuildContext
{
    // Configuration
    public SiteConfig Config { get; set; }

    // Content
    public List<DocPage> Pages { get; set; }
    public ApiReference ApiModel { get; set; }

    // Navigation & Search
    public NavigationTree Navigation { get; set; }
    public SearchIndex SearchIndex { get; set; }

    // Diagnostics
    public DiagnosticBag Diagnostics { get; set; }

    // File System (abstraction for testing)
    public IFileSystem FileSystem { get; set; }

    // Discovery Results
    public List<string> DiscoveredMarkdownFiles { get; set; }
    public List<string> DiscoveredProjectFiles { get; set; }
    public List<string> DiscoveredAssetFiles { get; set; }

    // Package Info (from .csproj analysis)
    public PackageMetadata PackageInfo { get; set; }

    // Versioning
    public List<DocVersion> Versions { get; set; }
}
```

### Property Details

| Property | Set By Phase | Description |
|----------|-------------|-------------|
| `Config` | Pipeline init | The parsed `SiteConfig` from `mokadocs.yaml` |
| `Pages` | MarkdownParsePhase | All documentation pages with parsed content and front matter |
| `ApiModel` | CSharpAnalysisPhase | The complete API reference model extracted from C# projects |
| `Navigation` | NavigationBuildPhase | The hierarchical navigation tree used to render the sidebar |
| `SearchIndex` | SearchIndexPhase | The search index entries used for client-side search |
| `Diagnostics` | Any phase | Collects errors, warnings, and info messages throughout the build |
| `FileSystem` | Pipeline init | An abstraction over the file system, enabling unit testing with in-memory file systems |
| `DiscoveredMarkdownFiles` | DiscoveryPhase | Absolute paths to all `.md` files found in the docs directory |
| `DiscoveredProjectFiles` | DiscoveryPhase | Absolute paths to all `.csproj` files found for API analysis |
| `DiscoveredAssetFiles` | DiscoveryPhase | Absolute paths to static assets (images, downloads, etc.) |
| `PackageInfo` | CSharpAnalysisPhase | Package name, version, authors, and description from `.csproj` metadata |
| `Versions` | Pipeline init | List of configured documentation versions |

## Key Models

### DocPage

Represents a single documentation page, whether sourced from Markdown or generated from API analysis.

```csharp
public class DocPage
{
    public FrontMatter FrontMatter { get; set; }      // Title, order, icon, visibility, etc.
    public PageContent Content { get; set; }           // Html and PlainText representations
    public TableOfContents TableOfContents { get; set; } // Headings extracted from content
    public string Route { get; set; }                  // URL path (e.g., "/guide/getting-started")
    public PageOrigin Origin { get; set; }             // Markdown or ApiGenerated
    public DateTime? LastModified { get; set; }        // File last-modified timestamp
}
```

- **FrontMatter**: Contains all YAML front matter fields — `title`, `order`, `icon`, `visibility`, `description`, `tags`, `layout`, and any custom fields.
- **PageContent**: Holds both the rendered HTML (`Html`) and a plain text extraction (`PlainText`) used for search indexing.
- **TableOfContents**: A list of heading entries with `Level`, `Text`, `Id`, and nesting structure.
- **Route**: The clean URL path for the page, derived from the file's location in the docs directory.
- **Origin**: An enum indicating whether the page was parsed from a Markdown file (`Markdown`) or generated from C# API analysis (`ApiGenerated`).

### NavigationNode

Represents a single item in the sidebar navigation tree.

```csharp
public class NavigationNode
{
    public string Label { get; set; }                  // Display text
    public string Route { get; set; }                  // Link target
    public string Icon { get; set; }                   // Icon identifier
    public int Order { get; set; }                     // Sort order
    public bool Expanded { get; set; }                 // Whether children are visible
    public bool IsActive { get; set; }                 // Whether this is the current page
    public List<NavigationNode> Children { get; set; } // Nested items
}
```

Navigation nodes form a tree structure. Top-level nodes correspond to sections (directories), and leaf nodes correspond to individual pages. The `Order` field controls sorting within each level.

### SearchEntry

Represents a single entry in the search index.

```csharp
public class SearchEntry
{
    public string Title { get; set; }       // Page or section title
    public string Section { get; set; }     // Parent section name
    public string Route { get; set; }       // URL path with optional anchor
    public string Content { get; set; }     // Plain text content for matching
    public string Category { get; set; }    // "guide", "api", "reference", etc.
    public List<string> Tags { get; set; }  // Tags from front matter
}
```

Each page generates one or more search entries. Headings within a page create additional entries with anchor links, allowing search results to deep-link into specific sections.

### ApiType

Represents a C# type extracted during the analysis phase.

```csharp
public class ApiType
{
    public string Name { get; set; }                    // Type name
    public string FullName { get; set; }                // Fully qualified name
    public string Namespace { get; set; }               // Containing namespace
    public TypeKind Kind { get; set; }                  // Class, Interface, Enum, Struct, Record, Delegate
    public List<ApiMember> Members { get; set; }        // Methods, properties, fields, events
    public XmlDocumentation Documentation { get; set; } // Parsed XML doc comments
    public List<AttributeInfo> Attributes { get; set; } // Applied attributes
    public string SourceCode { get; set; }              // Original source (if enabled)
    public string BaseType { get; set; }                // Base class
    public List<string> Interfaces { get; set; }        // Implemented interfaces
    public List<string> TypeParameters { get; set; }    // Generic type parameters
    public AccessModifier Access { get; set; }          // public, internal, etc.
}
```

## Template Engine

MokaDocs uses **Scriban** as its template engine. Scriban uses a Liquid-like syntax with `{{ }}` delimiters and supports filters, conditionals, loops, and partials.

### ThemeRenderContext

The `ThemeRenderContext` is the data object passed to every template during rendering. It provides access to all site data:

```
{{ page.title }}           — Current page title
{{ page.content }}         — Rendered HTML content
{{ page.toc }}             — Table of contents entries
{{ site.title }}           — Site name from config
{{ site.description }}     — Site description
{{ navigation }}           — The full navigation tree
{{ search_index }}         — Search index JSON
{{ package.name }}         — Package name from .csproj
{{ package.version }}      — Package version
{{ version.current }}      — Current documentation version
{{ version.all }}          — All available versions
```

### Partials

Templates can include reusable partials:

```
{{ include 'partials/header' }}
{{ include 'partials/sidebar' }}
{{ include 'partials/footer' }}
```

Partials are resolved from the theme's `partials/` directory and can accept parameters for customization.

## Diagnostics

The `DiagnosticBag` collects messages throughout the build process. Messages are categorized by severity:

- **Error**: Build-breaking issues (missing required files, invalid configuration). Cause a non-zero exit code.
- **Warning**: Non-fatal issues (broken internal links, missing front matter fields). Build continues but issues are reported.
- **Info**: Informational messages (page count, timing, skipped files). Shown in verbose mode.

Diagnostics are printed to the console at the end of the build, grouped by severity, with file paths and line numbers where applicable.
