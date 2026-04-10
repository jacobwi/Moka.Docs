---
title: Interactive REPL
order: 2
---

# Interactive REPL Plugin

The REPL plugin adds interactive C# code execution to your documentation. Readers can run code snippets directly in the browser, making it easy to explore APIs and experiment with your library.

**Plugin ID:** `mokadocs-repl`

## What It Does

The REPL plugin transforms specially marked code blocks into interactive editors with a **Run** button. When a reader clicks Run, the C# code is sent to the server, executed via Roslyn, and the console output is displayed in a panel below the code block. This provides a "try it live" experience without readers needing to set up a local development environment.

## Markdown Syntax

Use the `csharp-repl` language identifier on fenced code blocks to mark them as interactive. The following aliases are also recognized: `cs-repl`, `csharp repl`, and `cs repl`.

````markdown
```csharp-repl
var x = 42;
Console.WriteLine($"The answer is {x}");
```
````

Standard `csharp` code blocks are not affected and will render as normal syntax-highlighted code without the Run button.

## How It Works

The REPL plugin integrates at multiple levels of the MokaDocs pipeline:

### 1. Markdown Extension (ReplExtension)

During markdown parsing, the `ReplExtension` (a Markdig extension) intercepts code blocks with the `csharp-repl` language identifier. It wraps the code in a structured HTML container:

```html
<div class="repl-container" data-repl="true">
    <pre><code class="language-csharp">var x = 42;
Console.WriteLine($"The answer is {x}");</code></pre>
</div>
```

The `data-repl="true"` attribute marks the container for the client-side JavaScript to enhance.

### 2. Plugin Injection (ReplPlugin)

During the plugin execution phase (order 500), the `ReplPlugin` injects the necessary CSS and JavaScript into each page that contains REPL containers. This includes:

- A **Run** button overlaid on the code block
- An **output panel** below the code block that shows execution results
- A **loading indicator** displayed while code is executing
- **Styling** for success and error states

### 3. Client-Side Execution Request

When the reader clicks the Run button, the injected JavaScript:

1. Extracts the code from the code block
2. Sends a `POST` request to `/api/repl/execute` with the code as the request body
3. Displays a loading spinner while waiting for the response
4. Renders the output (or error message) in the output panel

### 4. Server-Side Execution (ReplExecutionService)

The `/api/repl/execute` endpoint is available in **serve mode** only. The `ReplExecutionService` handles execution:

1. Receives the C# code string
2. Creates a Roslyn `CSharpScript` instance with configured options
3. Redirects `Console.Out` to a `StringWriter` to capture output
4. Executes the script with a 5-second timeout
5. Returns the captured console output (or the exception message on failure)

### 5. Output Display

The output panel renders results in a monospace font. Successful output is shown in the default text color. Errors and exceptions are shown in red with the exception type and message.

## Features

### Execution Timeout

All REPL executions are subject to a **5-second timeout**. If the script does not complete within this window, execution is cancelled and an error message is returned. This prevents infinite loops and long-running operations from blocking the server.

### Console Output Capture

The REPL captures output from `Console.Write` and `Console.WriteLine`. Both methods are fully supported, including formatted strings and interpolated strings:

````markdown
```csharp-repl
Console.WriteLine("Hello, World!");
Console.Write("No ");
Console.Write("newline ");
Console.WriteLine("here.");
Console.WriteLine($"2 + 2 = {2 + 2}");
```
````

### Default Namespaces

The following namespaces are imported by default in every REPL execution, so you do not need to add `using` statements for them:

- `System`
- `System.Linq`
- `System.Collections.Generic`
- `System.Text`

You can use types from these namespaces directly:

````markdown
```csharp-repl
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var even = numbers.Where(n => n % 2 == 0).ToList();
Console.WriteLine(string.Join(", ", even));
```
````

### NuGet Package Loading

You can configure additional NuGet packages to be available in REPL sessions. Packages are specified in the plugin options and are resolved and loaded at initialization time.

```yaml
plugins:
  - name: mokadocs-repl
    options:
      packages:
        - Newtonsoft.Json
        - Humanizer@2.14.1
```

Packages without a version specifier resolve to the latest stable version. Use the `@version` syntax to pin a specific version.

Once configured, you can use types from those packages in your REPL blocks:

````markdown
```csharp-repl
using Newtonsoft.Json;

var obj = new { Name = "MokaDocs", Version = "1.0" };
var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
Console.WriteLine(json);
```
````

### Project Assembly Auto-Loading

When you are documenting your own .NET library, the REPL plugin can automatically load your project's compiled assembly. This allows REPL blocks to reference the types and methods from the library you are documenting, providing readers with a true "try the API" experience.

The assembly is resolved from the build output of the project referenced in your MokaDocs configuration. No additional setup is required beyond having a successful build of your library.

## Configuration

### Basic Configuration

Add the REPL plugin to your `mokadocs.yaml`:

```yaml
plugins:
  - name: mokadocs-repl
```

### With NuGet Packages

```yaml
plugins:
  - name: mokadocs-repl
    options:
      packages:
        - Newtonsoft.Json
        - Humanizer@2.14.1
```

### Full Configuration Example

```yaml
plugins:
  - name: mokadocs-repl
    options:
      packages:
        - Newtonsoft.Json
        - Humanizer@2.14.1
        - FluentValidation@11.0.0
```

### Configuration Options

| Option     | Type     | Description |
|------------|----------|-------------|
| `packages` | string[] | List of NuGet packages to make available. Use `PackageName` or `PackageName@Version` format. |

## Security

The REPL execution environment includes several safety measures:

- **Roslyn scripting sandbox** -- Scripts run via the Roslyn scripting API with captured console output, providing isolation from the host process.
- **5-second timeout** -- Execution is forcefully cancelled after 5 seconds, protecting against infinite loops, `Thread.Sleep` abuse, and other long-running operations.
- **10KB code size limit** -- Submitted code is limited to 10KB to prevent abuse and excessive memory consumption.
- **No persistent state** -- Each execution is independent. Variables and state do not carry over between Run clicks, even on the same code block.

These restrictions mean certain operations will fail in the REPL:

- File I/O operations (`File.ReadAllText`, `StreamWriter`, etc.)
- Network requests (`HttpClient`, `WebClient`, etc.)
- Thread and process creation
- Assembly loading beyond the pre-configured packages
- Code submissions exceeding 10KB in size

## Static Build Behavior

When you run `mokadocs build` to produce a static site, the REPL API endpoint is not available. In static builds, the REPL containers display a message in place of the Run button:

> **Run `mokadocs serve` to execute code**

This informs readers that they need the live dev server to use the interactive features. The code blocks still display with full syntax highlighting so readers can study the code even in the static version of the site.
