---
title: API Documentation
order: 4
---

# API Documentation

MokaDocs can automatically generate API reference documentation from your C# source code. It uses Roslyn-based static analysis to extract type information, member signatures, and XML documentation comments directly from your `.csproj` files.

## How It Works

When you configure one or more .NET project files in your MokaDocs configuration, the build process:

1. Loads the `.csproj` file and resolves its dependencies
2. Uses the Roslyn compiler to parse and analyze all source files
3. Extracts type declarations, member signatures, and XML documentation
4. Resolves `<inheritdoc/>` references automatically
5. Generates structured HTML pages for each type in your API

The result is a fully navigable API reference that stays in sync with your source code every time you build your documentation.

## Configuration

Add your .NET projects to the MokaDocs configuration file:

```yaml
api:
  projects:
    - src/MyLibrary/MyLibrary.csproj
    - src/MyLibrary.Abstractions/MyLibrary.Abstractions.csproj
```

### Project Options

Each project entry supports additional options:

```yaml
api:
  projects:
    - path: src/MyLibrary/MyLibrary.csproj
      label: Core Library
      includeInternals: false
    - path: src/MyLibrary.Abstractions/MyLibrary.Abstractions.csproj
      label: Abstractions
      includeInternals: false
```

| Option             | Type   | Default | Description                                       |
|--------------------|--------|---------|---------------------------------------------------|
| `path`             | string | —       | Path to the `.csproj` file (required)             |
| `label`            | string | Project name | Display name in the API navigation           |
| `includeInternals` | bool   | `false` | Include `internal` types and members in the output |

## What Gets Documented

MokaDocs extracts and documents all public (and optionally internal) type declarations from your project.

### Type Declarations

The following type kinds are documented with their full signatures:

| Type Kind   | Example                                       |
|-------------|-----------------------------------------------|
| Classes     | `public class DocumentProcessor`              |
| Structs     | `public struct Point`                         |
| Records     | `public record Person(string Name, int Age)`  |
| Interfaces  | `public interface IRenderer`                  |
| Enums       | `public enum LogLevel`                        |
| Delegates   | `public delegate void EventHandler(Event e)`  |

### Member Documentation

For each type, all applicable members are documented:

| Member Kind   | Details Shown                                        |
|---------------|------------------------------------------------------|
| Constructors  | Parameters, XML docs, overloads                      |
| Methods       | Return type, parameters, generic type params, XML docs |
| Properties    | Type, getter/setter accessibility, XML docs          |
| Fields        | Type, constant values, XML docs                      |
| Events        | Delegate type, XML docs                              |
| Operators     | Operator symbol, parameter types, return type        |
| Indexers      | Parameter types, return type, accessor info          |

## XML Documentation Comments

MokaDocs fully supports C# XML documentation comments. Writing thorough XML docs in your source code is the best way to produce high-quality API reference pages.

### Supported Tags

#### `<summary>`

The primary description of a type or member. This is the most important tag and should be included on every public API.

```csharp
/// <summary>
/// Processes Markdown documents and converts them to HTML output.
/// </summary>
public class MarkdownProcessor { }
```

#### `<remarks>`

Additional details, usage notes, or background information that supplements the summary.

```csharp
/// <summary>
/// Converts a Markdown string to HTML.
/// </summary>
/// <remarks>
/// This method uses the Markdig pipeline configured during initialization.
/// The output HTML is sanitized to prevent XSS attacks.
/// </remarks>
public string Convert(string markdown) { }
```

#### `<param>`

Describes a method or constructor parameter.

```csharp
/// <summary>
/// Creates a new document from the specified file.
/// </summary>
/// <param name="filePath">The absolute path to the Markdown file.</param>
/// <param name="encoding">
/// The character encoding to use when reading the file. Defaults to UTF-8.
/// </param>
public Document(string filePath, Encoding? encoding = null) { }
```

#### `<typeparam>`

Describes a generic type parameter.

```csharp
/// <summary>
/// A thread-safe cache with configurable eviction policies.
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
public class Cache<TKey, TValue> where TKey : notnull { }
```

#### `<returns>`

Describes the return value of a method.

```csharp
/// <summary>
/// Searches the index for documents matching the query.
/// </summary>
/// <param name="query">The search query string.</param>
/// <returns>
/// A collection of search results ranked by relevance score,
/// or an empty collection if no matches are found.
/// </returns>
public IReadOnlyList<SearchResult> Search(string query) { }
```

#### `<exception>`

Documents exceptions that a method may throw.

```csharp
/// <summary>
/// Reads and parses a configuration file.
/// </summary>
/// <param name="path">Path to the configuration file.</param>
/// <exception cref="FileNotFoundException">
/// Thrown when the specified configuration file does not exist.
/// </exception>
/// <exception cref="InvalidConfigException">
/// Thrown when the configuration file contains invalid YAML syntax.
/// </exception>
public Config LoadConfig(string path) { }
```

#### `<example>`

Provides a code example showing how to use the API.

```csharp
/// <summary>
/// Registers MokaDocs services in the dependency injection container.
/// </summary>
/// <example>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Services.AddMokaDocs(options =>
/// {
///     options.Title = "My Documentation";
///     options.BaseUrl = "https://docs.example.com";
/// });
/// </code>
/// </example>
public static IServiceCollection AddMokaDocs(
    this IServiceCollection services,
    Action<MokaDocsOptions> configure) { }
```

#### `<seealso>`

Creates cross-references to related types or members.

```csharp
/// <summary>
/// Renders a document to HTML.
/// </summary>
/// <seealso cref="MarkdownProcessor"/>
/// <seealso cref="ITemplateEngine"/>
/// <seealso href="https://docs.example.com/rendering">Rendering Guide</seealso>
public string Render(Document doc) { }
```

#### `<inheritdoc>`

Inherits documentation from a base class or interface. MokaDocs resolves `<inheritdoc/>` references automatically during the build process.

```csharp
public interface IProcessor
{
    /// <summary>
    /// Processes the input document and returns the result.
    /// </summary>
    /// <param name="input">The document to process.</param>
    /// <returns>The processing result.</returns>
    Result Process(Document input);
}

public class MarkdownProcessor : IProcessor
{
    /// <inheritdoc/>
    public Result Process(Document input)
    {
        // The XML docs from IProcessor.Process are used automatically
    }
}
```

You can also inherit from a specific member:

```csharp
/// <inheritdoc cref="IProcessor.Process(Document)"/>
public Result ProcessMarkdown(Document input) { }
```

## Attributes Display

MokaDocs detects and displays relevant attributes on types and members. For example, the `[Obsolete]` attribute is rendered with a visual warning indicator:

```csharp
/// <summary>
/// Converts documents using the legacy pipeline.
/// </summary>
[Obsolete("Use MarkdownProcessor instead. This class will be removed in v3.0.")]
public class LegacyConverter { }
```

This renders with a deprecation notice on the generated API page, ensuring consumers of your library are aware of deprecated APIs.

## Source Code Viewing

Each documented type includes a collapsible "View Source" / "Expand Source" section at the bottom of its API page. This lets readers inspect the actual implementation without leaving the documentation.

### How It Works

During the build, the `AssemblyAnalyzer` uses Roslyn's `SyntaxNode.NormalizeWhitespace().ToFullString()` to extract clean, consistently formatted source code for every type it processes. The extracted source is stored in the `ApiType.SourceCode` property and rendered by `ApiPageRenderer` as an HTML `<details>`/`<summary>` element, so the code block is collapsed by default and can be expanded on demand.

### When It Appears

The "View Source" section only appears on an API page when source code is available. This requires the project to have syntax trees accessible to Roslyn at analysis time. If you are referencing a project through a compiled assembly or a NuGet package without source, the section will not be rendered.

### Including Internal Members

Each project entry in your configuration supports an `includeInternals` option. When set to `true`, types and members declared as `internal` are included in the generated API documentation alongside public APIs. This is useful for internal developer documentation or when your library uses `InternalsVisibleTo`.

```yaml
content:
  projects:
    - path: ./src/MyLib/MyLib.csproj
      label: "MyLib"
      includeInternals: true
```

When `includeInternals` is `false` (the default), only `public` types and members are documented.

## Type Relationships

MokaDocs automatically extracts and displays type relationships:

### Base Types and Interfaces

The inheritance chain and implemented interfaces are displayed for each type:

```csharp
public class MarkdownProcessor : DocumentProcessor, IProcessor, IDisposable
{
    // Base type: DocumentProcessor
    // Implements: IProcessor, IDisposable
}
```

### Generic Type Parameters

Generic type parameters and their constraints are fully rendered:

```csharp
public class Repository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
    // Generic parameters and constraints are displayed
}
```

### Type Dependency Graph

In addition to the textual display of base types and interfaces, each API type page includes a collapsible "Type Relationships" section that renders a visual dependency graph as a Mermaid class diagram.

The diagram shows inheritance chains, implemented interfaces, and derived types for the current type. The current type is highlighted in pink so it stands out within the graph. The diagram is auto-generated during the build -- no configuration is needed.

**Behavior details:**

- The graph is capped at 20 nodes to keep rendering fast and the diagram readable.
- It only appears when there are meaningful relationships to display. Orphan types (those with no base class other than `object`, no implemented interfaces, and no known derived types) do not show the section at all.
- The Mermaid diagram is rendered client-side using the same Mermaid integration available for Markdown content.

## Extension Methods

Extension methods are documented with their target type clearly indicated:

```csharp
public static class StringExtensions
{
    /// <summary>
    /// Converts a Markdown string to plain text by stripping all formatting.
    /// </summary>
    /// <param name="markdown">The Markdown-formatted string.</param>
    /// <returns>The plain text content without Markdown formatting.</returns>
    public static string ToPlainText(this string markdown) { }
}
```

The generated documentation shows that `ToPlainText` extends `string`, making it easy for users to discover available extension methods.

## Package Metadata and NuGet Widget

MokaDocs extracts package metadata from your `.csproj` file properties:

| Property        | Usage                                                |
|-----------------|------------------------------------------------------|
| `PackageId`     | Used as the package name in install instructions     |
| `Version`       | Displayed version and used in install commands       |

When package metadata is available, MokaDocs automatically generates a NuGet installation widget at the top of the API section. This widget renders a tabbed UI with three installation methods — .NET CLI, Package Manager Console, and PackageReference — so readers can copy the command that matches their workflow:

```
dotnet add package MyLibrary --version 2.1.0
```

```
Install-Package MyLibrary -Version 2.1.0
```

```xml
<PackageReference Include="MyLibrary" Version="2.1.0" />
```

The version number is extracted directly from the project file, ensuring the install instructions always reference the correct version.

## Tips for Writing Good XML Docs

### Be Specific in Summaries

Write summaries that describe *what* the member does, not just *what* it is.

```csharp
// Bad: States the obvious
/// <summary>
/// The name property.
/// </summary>
public string Name { get; set; }

// Good: Explains the purpose
/// <summary>
/// Gets or sets the display name shown in the site navigation sidebar.
/// </summary>
public string Name { get; set; }
```

### Document Parameters Thoroughly

Each parameter should explain what valid values look like and what happens with edge cases.

```csharp
/// <param name="maxDepth">
/// The maximum depth to traverse when building the navigation tree.
/// Must be between 1 and 10 inclusive. A value of 1 shows only
/// top-level pages. Defaults to 3.
/// </param>
public NavigationTree Build(int maxDepth = 3) { }
```

### Include Examples for Complex APIs

For APIs with non-obvious usage patterns, always include an `<example>` tag.

### Use `<remarks>` for Behavioral Details

Put implementation notes, thread safety information, performance characteristics, and edge case behavior in `<remarks>` rather than cluttering the `<summary>`.

```csharp
/// <summary>
/// Searches the full-text index for matching documents.
/// </summary>
/// <remarks>
/// This method is thread-safe and can be called concurrently.
/// Results are ranked using BM25 scoring. The search supports
/// prefix matching, phrase queries (using double quotes), and
/// boolean operators (AND, OR, NOT).
/// </remarks>
public IReadOnlyList<SearchResult> Search(string query) { }
```

### Document Exceptions Consistently

List all exceptions that callers should expect and handle. Include the conditions under which each exception is thrown.

### Use `<inheritdoc/>` to Avoid Duplication

When implementing interfaces or overriding base class members, use `<inheritdoc/>` rather than copying the documentation. This keeps your docs DRY and ensures consistency when the base documentation changes.
