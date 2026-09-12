---
title: Interactive REPL
order: 2
---

# Interactive REPL Plugin

The REPL plugin adds a **Run** button to C# code blocks. While you preview the site with `mokadocs serve`, a click runs the snippet through Roslyn scripting inside the dev server and shows the console output under the block. Static builds show the code but can't run it.

**Plugin ID:** `mokadocs-repl`

## What It Does

REPL blocks render as read-only, syntax-highlighted code with a toolbar underneath that holds a **Run** button and a status label. Readers can't edit the code in the browser: Run executes the snippet exactly as written, and the result appears in an output panel below it.

## Markdown Syntax

Use the `csharp-repl` language identifier on fenced code blocks to mark them as runnable. The aliases `cs-repl`, `csharp repl` and `cs repl` also work, in any letter case.

````markdown
```csharp-repl
var x = 42;
Console.WriteLine($"The answer is {x}");
```
````

Standard `csharp` code blocks are not affected. If `mokadocs.yaml` doesn't declare the plugin, REPL blocks render as plain code blocks with no Run button.

## How It Works

The REPL plugin integrates at multiple levels of the MokaDocs pipeline:

### 1. Markdown Extension (ReplExtension)

During Markdown parsing, `ReplExtension` (a Markdig extension) wraps each REPL block in a container and adds a hidden output panel after the code:

```html
<div class="repl-container" data-repl="true">
    <pre><code class="language-csharp">var x = 42;
Console.WriteLine($"The answer is {x}");</code></pre>
    <div class="repl-output" style="display:none;"></div>
</div>
```

The `data-repl="true"` attribute marks the container for the plugin's script.

### 2. Plugin Injection (ReplPlugin)

When plugins run (see the [build pipeline order](/plugins/overview#build-pipeline-order)), `ReplPlugin` adds inline CSS and JavaScript to every page that contains a REPL container. The script:

- Adds a toolbar with a **Run** button and a status label under each code block
- Shows a spinner on the button while a run is in progress
- Writes the result into the output panel, styled for success, errors and an unreachable server

### 3. Client-Side Execution Request

When the reader clicks Run, the script:

1. Reads the text of the code block
2. Sends a `POST` request to `/api/repl/execute` with the JSON body `{"code": "..."}`
3. Shows the spinner until the response arrives
4. Renders the output or error in the output panel and the elapsed time in the status label

### 4. Server-Side Execution (ReplExecutionService)

Only `mokadocs serve` runs code. Its `/api/repl/execute` endpoint hands the code to `ReplExecutionService`, which:

1. Rejects empty code and code longer than 10,000 characters
2. Redirects `Console.Out` and `Console.Error` to string writers
3. Compiles the code as a Roslyn `CSharpScript` with the default imports, plus any loaded packages and project assemblies
4. Runs the script
5. Returns JSON with `output` and `error` fields

### 5. Output Display

The output panel uses a monospace font and scrolls once its content passes 300px. It shows:

- The console output, with anything written to `Console.Error` appended after it
- The value of the last expression when the snippet wrote nothing to the console, so a snippet ending in `numbers.Sum()` (no semicolon) shows the sum
- `(no output)` when there is neither
- Compiler error messages in red, one per line, without error codes or line numbers
- `Runtime error: <message>` in red when the code throws

## Features

### Execution Timeout

Each run gets a 5-second timeout, implemented as a cancellation token. It can stop a slow compile, but it doesn't interrupt code that is already running: a snippet with an infinite loop never returns, and its thread stays busy until you stop the server. See [Security](#security).

### Console Output Capture

The REPL captures output from `Console.Write` and `Console.WriteLine`, including formatted and interpolated strings:

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

These are imported in every run, so snippets don't need `using` lines for them:

- `System`
- `System.Linq`
- `System.Collections.Generic`
- `System.Text`
- `System.Text.RegularExpressions`
- `System.Math`, as a static import, so `Sqrt(2)` and `Max(a, b)` work without the `Math.` prefix

You can use types from these namespaces directly:

````markdown
```csharp-repl
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var even = numbers.Where(n => n % 2 == 0).ToList();
Console.WriteLine(string.Join(", ", even));
```
````

### NuGet Package Loading

List NuGet packages in the plugin's `packages` option to use them in REPL blocks:

```yaml
plugins:
  - name: mokadocs-repl
    options:
      packages:
        - Newtonsoft.Json
        - Humanizer@2.14.1
```

`mokadocs serve` loads the packages once, at startup, before the server starts listening. It writes a temporary project that references them and runs `dotnet publish` on it, so it needs the .NET SDK and access to your NuGet feed. `mokadocs build` never loads packages. Restart `serve` after you change the list.

- A bare `PackageName` resolves to the latest stable version. `PackageName@Version` pins a version.
- Assemblies that ship with the .NET runtime, such as `System.Text.Json`, are skipped, because the runtime's own copy is already loaded. Packages that only share the prefix, such as `System.Reactive`, load normally.
- The top-level namespaces of each loaded assembly (for example `Newtonsoft` and `Newtonsoft.Json`) are imported automatically.
- `serve` prints `REPL: Loaded N package assemblies` when the restore works. When it fails, `serve` prints `REPL: Packages not loaded:` with the `dotnet publish` output and starts without the packages.

Once loaded, the package types are available in REPL blocks:

````markdown
```csharp-repl
using Newtonsoft.Json;

var obj = new { Name = "MokaDocs", Version = "1.0" };
var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
Console.WriteLine(json);
```
````

### Project Assembly Auto-Loading

When `mokadocs serve` starts, the REPL also loads the compiled assembly of each project listed in `content.projects`, so REPL blocks can call the library you are documenting. For each project it looks for `<ProjectName>.dll` under the project's `bin/Release/<tfm>/` and `bin/Debug/<tfm>/` folders and picks:

- The highest target framework that the running .NET runtime can load
- The Release build over the Debug build for the same target framework

`serve` doesn't build your projects, so build them first. The DLL name must match the `.csproj` file name. Only that one assembly is referenced, and its top-level namespaces are imported automatically. If snippets use types from its dependencies, add those packages to `packages`.

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
| `packages` | string[] | NuGet packages to load when `mokadocs serve` starts. Use `PackageName` or `PackageName@Version` format. |

## Security

REPL code runs inside the `mokadocs serve` process with the permissions of the user who started it. There is no sandbox: a snippet can do anything the dev server process can, such as reading and writing files. Only enable the plugin for documentation whose code you trust.

These limits do apply:

- **Size limit.** Code longer than 10,000 characters is rejected before it is compiled.
- **Timeout.** The 5-second cancellation token can stop a slow compile, but not code that is already running. A tight loop never returns and keeps a thread busy until you stop the server.
- **Fresh script per run.** Variables don't carry over between runs, even on the same code block. Static state in loaded package or project assemblies lasts until the server stops.
- **Local, same-origin requests.** The dev server listens on `localhost` only. The endpoint accepts a `POST` only with `Content-Type: application/json`, and a browser request only from the dev server's own origin (`http://localhost:<port>`). Requests from other origins get a 403, and the server sends no `Access-Control-Allow-Origin` header. Requests without an `Origin` header, such as from `curl`, are accepted, so any program on your machine can send code to it.
- **Only when declared.** If `mokadocs.yaml` doesn't declare `mokadocs-repl`, the endpoint answers 503 and runs nothing.

## Static Build Behavior

The Run button is added to every page with a REPL block, including sites built with `mokadocs build`. A static host has no execution endpoint, so a click shows this message in the output panel:

> REPL server unavailable. Run "mokadocs serve" to enable interactive code execution.

The ASP.NET Core host (`EnableRepl`) doesn't add an execution endpoint either, so Run doesn't work there. In every case the code stays readable with syntax highlighting.
