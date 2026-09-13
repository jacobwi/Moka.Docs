---
title: API Documentation
order: 4
---

# API Documentation

MokaDocs generates API reference pages from the C# source of the projects listed in `mokadocs.yaml`. It uses Roslyn to read type declarations, member signatures and XML documentation comments from the `.cs` files. To build API pages at runtime from compiled assemblies instead, see [ASP.NET Core Integration](/guide/aspnetcore).

## How It Works

For each configured project, the build:

1. Collects every `.cs` file under the folder that contains the `.csproj`, skipping `bin` and `obj` folders
2. Reads the project's settings from the `.csproj`, the `Directory.Build.props` it imports and `obj/project.assets.json`
3. Compiles the files with Roslyn against the shared framework of the .NET runtime MokaDocs runs on, plus the project's package and project references
4. Extracts type declarations, member signatures and XML documentation comments
5. Fills in missing documentation from base types and interfaces (see `<inheritdoc>` below)
6. Writes an index page at `/api` and one page per type

MSBuild doesn't run. From the project files, MokaDocs reads the target framework, `ImplicitUsings`, `<Using>` items (including `Static`, `Alias` and `Remove`), `DefineConstants` and `Nullable`. In practice:

- A project with several target frameworks is analyzed for the highest one. Its preprocessor symbols are defined the way the .NET SDK defines them (for `net9.0`: `NET`, `NET9_0`, `NETCOREAPP`, `NET5_0_OR_GREATER` through `NET9_0_OR_GREATER`, and `NETCOREAPP1_0_OR_GREATER` through `NETCOREAPP3_1_OR_GREATER`), along with `TRACE`, `RELEASE` and the `DefineConstants` values. `#if` branches follow those symbols.
- Only unconditional properties and items are read. Values built from other properties are used when those properties were read first, as in `$(DefineConstants);MY_SYMBOL`; anything else that needs MSBuild to evaluate is skipped. A `Directory.Build.props` higher up applies only when the nearest one imports it, as MSBuild does.
- Package and project references come from `obj/project.assets.json`, so the project needs a `dotnet restore`. A project reference also needs the referenced project built, because its assembly is taken from that project's `bin` folder. Types that can't be resolved show without their namespace, and an exception `cref` that points at one shows as written.
- Shared framework references, such as `Microsoft.AspNetCore.App` in a Web SDK project, resolve from the same .NET installation.
- Every `.cs` file below the project folder is included, whether or not the `.csproj` compiles it. Keep other projects out of that folder.

## Configuration

List your projects under `content.projects`. Each entry is an object with a `path`, relative to `mokadocs.yaml`:

```yaml
content:
  projects:
    - path: src/MyLibrary/MyLibrary.csproj
    - path: src/MyLibrary.Abstractions/MyLibrary.Abstractions.csproj
```

A plain string entry such as `- src/MyLibrary/MyLibrary.csproj` makes the build stop with "Failed to parse configuration". A path that doesn't exist produces a "Project file not found" warning and that project is skipped.

### Project Options

```yaml
content:
  projects:
    - path: src/MyLibrary/MyLibrary.csproj
      label: Core Library
      includeInternals: false
```

| Option             | Type   | Default                 | Description                                                                               |
|--------------------|--------|-------------------------|-------------------------------------------------------------------------------------------|
| `path`             | string | -                       | Path to the `.csproj` file (required). The `.cs` files in its folder are analyzed.        |
| `label`            | string | File name, no extension | Assembly name for the Roslyn compilation, also used in build logs. Not shown on the site. |
| `includeInternals` | bool   | `false`                 | Also document `internal` and `private protected` types and members.                       |

## What Gets Documented

### Type Declarations

Each class, struct, record, interface, enum and delegate gets a page. The route is `/api/` followed by the namespace with dots turned into slashes and the type name, all lowercase: `MyLibrary.Text.Parser` is at `/api/mylibrary/text/parser`.

Types declared outside any namespace are listed under `(global)`, so `Helper` in the global namespace is at `/api/(global)/helper`.

| Type Kind   | Example                                       |
|-------------|-----------------------------------------------|
| Classes     | `public class DocumentProcessor`              |
| Structs     | `public struct Point`                         |
| Records     | `public record Person(string Name, int Age)`  |
| Interfaces  | `public interface IRenderer`                  |
| Enums       | `public enum LogLevel`                        |
| Delegates   | `public delegate void EventHandler(Event e)`  |

A page opens with a declaration line such as `public sealed class Parser : MyLibrary.Text.ParserBase, System.IDisposable`. It shows type parameter names without their constraints. A delegate's line shows only its name. Its parameters are listed under its `Invoke` method, whose signature is the full delegate declaration.

A `partial` type declared in several files gets one page, which lists the members of every declaration. Its View Source section shows each declaration, headed by the name of its file.

### Accessibility

By default, MokaDocs documents `public`, `protected` and `protected internal` types and members. It leaves out `internal` and `private protected` ones, and it leaves out a public type nested inside an internal type. With `includeInternals: true`, the `internal` and `private protected` ones are documented too. Private members never are.

### Member Documentation

A type's members are grouped into sections:

| Section      | Contains                  | Signature example                                         |
|--------------|---------------------------|-----------------------------------------------------------|
| Constructors | Constructors              | `public Point(int x, int y = 5)`                          |
| Properties   | Properties and indexers   | `public int X { get; private set; }`                      |
| Methods      | Methods                   | `public static string ToPlainText(this string markdown)`  |
| Events       | Events                    | `public event EventHandler? Changed`                      |
| Fields       | Fields and enum members   | `public const int MaxDepth = 10`, `Info = 1`              |
| Operators    | Operators and conversions | `public static Point operator +(Point a, Point b)`        |

Each section starts with a table of member names and summaries. Below it, members get a detail block with their signature and documentation. What to expect:

- A member gets a detail block when it has any documentation tag, is obsolete, or has an attribute that the page shows (see [Attributes Display](#attributes-display)). Otherwise it only appears in the table, and its name there isn't a link.
- The table shows the member name and a short parameter list. Operators and conversions appear as C# writes them, such as `operator +(Point a, Point b)` and `implicit operator Point(int value)`. Indexers appear as `this[int index]` and constructors under the type name. A list of more than two parameters is shortened to `(…)`, and hovering over it shows the whole list.
- A detail block's anchor is the member's metadata name in lowercase, such as `#parse`, `#op_addition` or `#this[]`, and the type name for a constructor. Overloads after the first get `-2`, `-3` and so on, in the order the table lists them.
- Signatures read like the declaration: accessibility, modifiers such as `static`, `abstract`, `sealed override`, `new`, `readonly`, `required`, `const` and `volatile`, `this` on an extension method's first parameter, default values, type parameter constraints, accessor accessibility such as `{ get; private set; }` or `{ get; init; }`, and constant values. `async` isn't part of a signature. Interface members show only the modifiers their declarations spell out, such as `static abstract`.
- Types in signatures and in the Parameters table are written without their namespace.
- Static, virtual, abstract, override and obsolete members also get a badge in the table.
- The Parameters table, which lists each parameter's type, only appears when the member has `<param>` docs.

## XML Documentation Comments

MokaDocs reads the standard XML documentation tags. Some render only on type pages, as noted for each tag below.

In the text of tags such as `<summary>` and `<remarks>`, `<see href="...">` becomes a link. `<c>`, `<paramref>`, `<typeparamref>` and `<see langword="null"/>` become inline code, `<code>` a code block, `<para>` a paragraph and `<list>` a bulleted or numbered list. Escaped text such as `<c>List&lt;T&gt;</c>` shows as `List<T>`.

`<see cref="..."/>` links to the first of these that exists:

- The page of a documented type, or the detail block of a documented member. An overload is picked by its parameter types.
- The documented type, or a member of it, named as written, when the compiler couldn't resolve the reference and exactly one documented type has that name. Projects are compiled separately, so this is what links a reference to a type in another configured project.
- The page on learn.microsoft.com for a `System.*` or `Microsoft.*` type or member.

A reference that matches none of these shows as inline code. The link text is the type name, or `Type.Member` for a member, unless the tag has text of its own. The `/api` index page shows the names without links.

Search results and the page's meta description use the type's summary as plain text, without the markup.

### Supported Tags

#### `<summary>`

The main description. It appears under the type name, in the `/api` index, in member tables and in member detail blocks.

```csharp
/// <summary>
/// Processes Markdown documents and converts them to HTML output.
/// </summary>
public class MarkdownProcessor { }
```

#### `<remarks>`

Shown in a Remarks section on type pages and in a member's detail block.

```csharp
/// <summary>
/// Converts Markdown to HTML.
/// </summary>
/// <remarks>
/// Uses the Markdig pipeline configured at construction.
/// Instances are not thread-safe.
/// </remarks>
public class MarkdownConverter { }
```

#### `<param>`

Fills the description column of the Parameters table in a member's detail block.

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

Fills the description column of the Type Parameters table on a generic type's page. The table also lists each parameter's constraints. A generic method's detail block has the same table.

```csharp
/// <summary>
/// A thread-safe cache with configurable eviction policies.
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
public class Cache<TKey, TValue> where TKey : notnull { }
```

#### `<returns>`

Shown as a Returns line in the member's detail block.

```csharp
/// <summary>
/// Searches the index for documents matching the query.
/// </summary>
/// <param name="query">The search query string.</param>
/// <returns>
/// A collection of search results ranked by relevance score,
/// or an empty collection if no matches are found.
/// </returns>
public IReadOnlyList<SearchResult> Search(string query) => [];
```

#### `<value>`

Shown as a Value line in a property's detail block.

```csharp
/// <summary>
/// Gets the number of indexed documents.
/// </summary>
/// <value>Zero until the first build finishes.</value>
public int Count { get; }
```

#### `<exception>`

Listed in an Exceptions table in the member's detail block, under the exception's full type name: `System.IO.FileNotFoundException` for the example below. The name links the same way as `<see cref>`. A `cref` the compiler can't resolve is shown as written.

```csharp
/// <summary>
/// Reads a configuration file.
/// </summary>
/// <param name="path">Path to the configuration file.</param>
/// <returns>The file contents.</returns>
/// <exception cref="FileNotFoundException">
/// Thrown when the specified configuration file does not exist.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="path"/> is empty.
/// </exception>
public string LoadConfig(string path) => File.ReadAllText(path);
```

#### `<example>`

Shown in an Examples section on type pages and in a member's detail block.

```csharp
/// <summary>
/// Converts Markdown to HTML.
/// </summary>
/// <example>
/// <code>
/// var converter = new MarkdownConverter();
/// string html = converter.Convert("# Title");
/// </code>
/// </example>
public class MarkdownConverter
{
    /// <summary>Converts a Markdown string to HTML.</summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <returns>The HTML output.</returns>
    public string Convert(string markdown) => markdown;
}
```

#### `<seealso>`

Shown in a See Also list on type pages and in a member's detail block. A `cref` entry links the same way as `<see cref>`. An `href` entry links to its URL, with the element's text, or the URL itself, as the link text.

```csharp
/// <summary>
/// Renders documents to HTML.
/// </summary>
/// <seealso cref="MarkdownConverter"/>
/// <seealso href="https://docs.example.com/rendering">Rendering Guide</seealso>
public class HtmlRenderer { }
```

#### `<inheritdoc>`

After analysis, a type or member takes documentation from elsewhere when its summary is empty or its comment has an `<inheritdoc>` tag. Tags it documents itself are kept, and the rest come from the source:

- With `<inheritdoc cref="..."/>`, the source is the type or member the `cref` names.
- Otherwise MokaDocs searches the base types, nearest first, and then the interfaces: those the type declares, then those of each base type, then the interfaces those extend. For a member it looks for one with the same name, kind and parameter count, preferring identical parameter types. The first match with a summary is the source, and a match that inherits its own documentation is resolved first.
- The source has to be one of the documented types. It's found by full name, generic interfaces such as `IRepository<User>` included, or by simple name when exactly one documented type has that name. This lets `<inheritdoc/>` work across separate configured projects.

```csharp
public interface IProcessor
{
    /// <summary>
    /// Processes the input document and returns the result.
    /// </summary>
    /// <param name="input">The document to process.</param>
    /// <returns>The processing result.</returns>
    string Process(string input);
}

public class MarkdownProcessor : IProcessor
{
    /// <inheritdoc/>
    public string Process(string input) => input;
}
```

`MarkdownProcessor.Process` shows the summary, parameter and return docs from `IProcessor.Process`.

`ProcessMarkdown` below has no base member of that name, so it names its source with `cref` and shows the docs of `IProcessor.Process`:

```csharp
/// <inheritdoc cref="IProcessor.Process(string)"/>
public string ProcessMarkdown(string input) => input;
```

A member with no doc comment at all is filled the same way as one with `<inheritdoc/>`. A `cref` that names nothing in the documented types falls back to the search.

## Attributes Display

Attributes appear above the declaration line of a type and in a member's detail block, the way C# writes them: `[Flags]` above `public enum Options`. Attributes that the compiler generates or that only tools read are left out, such as `[CompilerGenerated]`, `[Nullable]`, `[AsyncStateMachine]`, `[DebuggerDisplay]` and `[MethodImpl]`. Names and arguments appear as written: `[Experimental("MOKA001")]`, `[EditorBrowsable(EditorBrowsableState.Never)]`. An attribute the compiler can't resolve, such as one from a package that hasn't been restored, appears without its arguments.

An obsolete type gets an Obsolete badge and a warning box with the attribute's message instead of an attribute line. An obsolete member gets an `obsolete` badge in its section's table and the warning box in its detail block. The attribute is only recognized when the compiler resolves it, so in a file without `using System;` it shows as a plain `[Obsolete]` line with no badge.

```csharp
/// <summary>
/// Converts documents using the legacy pipeline.
/// </summary>
[Obsolete("Use MarkdownProcessor instead. This class will be removed in v3.0.")]
public class LegacyConverter { }
```

## Source Code Viewing

Type pages built by the CLI end with a collapsed "View Source" section that holds the type's declaration.

### How It Works

During analysis, `AssemblyAnalyzer` takes each of the type's declarations, removes the members the page doesn't document and stores the result of `SyntaxNode.NormalizeWhitespace().ToFullString()` in `ApiType.SourceCode`. `ApiPageRenderer` renders it inside an HTML `<details>` element, so the code stays collapsed until the reader opens it.

### What It Contains

The section shows the declaration with normalized whitespace, doc comments included. It keeps the members the page documents, with their bodies, and leaves out the rest: `private` members and private nested types always, and `internal` and `private protected` ones unless `includeInternals` is `true`. A `#region` or `#if` directive on a removed member stays when its closing directive is on a member that stays. For a `partial` type it shows every declaration.

Pages served by the [ASP.NET Core integration](/guide/aspnetcore) have no View Source section, because that host reads compiled assemblies.

## Type Relationships

### Base Types and Interfaces

An Inheritance section lists the direct base type and the interfaces the type declares itself. Types further up the chain aren't listed. Structs show `System.ValueType` as their base type.

```csharp
namespace MyLibrary;

public interface IProcessor { }

public abstract class DocumentProcessor { }

public class MarkdownProcessor : DocumentProcessor, IProcessor, IDisposable
{
    public void Dispose() { }
}
```

The `MarkdownProcessor` page lists `MyLibrary.DocumentProcessor` as its base type and `MyLibrary.IProcessor` and `System.IDisposable` as interfaces.

### Generic Type Parameters

A generic type's page has a Type Parameters table with each parameter's constraints and `<typeparam>` description. The declaration line shows only the parameter names.

```csharp
namespace MyLibrary;

public interface IEntity<TKey> { }

public class Repository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
}
```

### Type Dependency Graph

Type pages can also include a collapsible "Type Relationships" section with a Mermaid class diagram. It shows direct relationships only:

- the type's base type
- the interfaces the type declares
- types whose base type is this type
- for an interface, types that declare it

The diagram doesn't follow the hierarchy any further in either direction. No configuration is needed.

- The current type is highlighted.
- The graph stops adding nodes at 20.
- A type with no base type other than `object`, no interfaces and no derived types gets no section.
- The diagram is drawn in the browser by the same Mermaid script as [Markdown diagrams](/guide/diagrams).

## Extension Methods

Extension methods are listed as static methods of the class that declares them. The `this` modifier in the signature is the only mark that the method extends a type.

```csharp
public static class StringExtensions
{
    /// <summary>
    /// Converts a Markdown string to plain text by stripping all formatting.
    /// </summary>
    /// <param name="markdown">The Markdown-formatted string.</param>
    /// <returns>The plain text content without Markdown formatting.</returns>
    public static string ToPlainText(this string markdown) => markdown;
}
```

This method's signature shows as `public static string ToPlainText(this string markdown)`.

## Analysis Cache

MokaDocs caches each project's analysis result in `.mokadocs/cache/` next to `mokadocs.yaml`. A cached result is reused only when:

- the project's `.cs` files have the same paths, sizes and modification times as before
- so do the `.csproj`, any `Directory.Build.props` above it (adding one counts as a change), `obj/project.assets.json` and the reference assemblies it points to
- MokaDocs runs on the same .NET runtime version
- `includeInternals` hasn't changed
- the running MokaDocs build is the one that wrote the entry

To skip the cache for one build, run `mokadocs build --no-cache`. To turn it off, set `build.cache: false` in `mokadocs.yaml`. `mokadocs clean` deletes the `.mokadocs` folder along with the output directory. See the [CLI reference](/advanced/cli-reference).

## Package Metadata and NuGet Widget

The `/api` index page starts with an install widget for the first project in `content.projects` that produces a package. A project is skipped when its `.csproj`, or the nearest `Directory.Build.props` above it that sets the property, has `<IsTestProject>true</IsTestProject>` or `<IsPackable>false</IsPackable>`, and when it uses the Web, Worker or Blazor WebAssembly SDK without setting `IsPackable`. If every project is skipped, the widget describes the first one. Its tabs hold these install commands:

```
Install-Package MyLibrary -Version 2.1.0
```

```
dotnet add package MyLibrary --version 2.1.0
```

```xml
<PackageReference Include="MyLibrary" Version="2.1.0" />
```

The package id and version are read from that project's `.csproj` file:

| Value      | Taken from                                                         |
|------------|--------------------------------------------------------------------|
| Package id | `PackageId`, else `AssemblyName`, else the `.csproj` file name     |
| Version    | `PackageVersion`, else `Version`, else `VersionPrefix` (with `-VersionSuffix` when set), else `1.0.0` |

The version is looked up in the `.csproj` first, then in each `Directory.Build.props` above it, nearest first. `1.0.0` is what `dotnet pack` uses when nothing sets a version. Nothing evaluates MSBuild, so a few cases are missed:

- Values built from other properties, such as `$(MajorVersion).0`, are skipped.
- Properties with a `Condition`, or inside a `PropertyGroup` with one, are skipped.
- A version passed on the command line (`-p:Version=...`) isn't seen.

The widget appears on every build that produces API pages, including builds with `--base-path`. The default theme has no setting to hide it.

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
public void Build(int maxDepth = 3) { }
```

The Parameters table only appears when a member has `<param>` docs.

### Add Examples for Non-Obvious Usage

For a type or member whose usage isn't obvious from its signature, add an `<example>`. It shows on the type page or in the member's detail block.

### Put Behavioral Details in Remarks

Implementation notes, thread safety, performance characteristics and edge cases belong in `<remarks>` rather than the `<summary>`, which also appears in member tables and search results.

```csharp
/// <summary>
/// Searches the full-text index for matching documents.
/// </summary>
/// <remarks>
/// Instances are thread-safe. Results are ranked using BM25 scoring.
/// The search supports prefix matching and phrase queries in double quotes.
/// </remarks>
public class SearchIndex { }
```

### Document Exceptions Consistently

List the exceptions callers should expect and handle, with the conditions under which each one is thrown.

### Use `<inheritdoc/>` to Avoid Duplication

When implementing an interface or overriding a base member, use `<inheritdoc/>` instead of copying the documentation. The member then picks up the base docs when they change. This works for any documented base type or interface in the chain, as described in the `<inheritdoc>` section above.
