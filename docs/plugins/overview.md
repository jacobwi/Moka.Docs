---
title: Plugin System
order: 1
---

# Plugin System

MokaDocs includes a plugin system for adding functionality to a documentation site. Plugins can modify page content, inject scripts and styles, generate new pages, and queue extra files for the output directory.

The `mokadocs` CLI only loads its built-in plugins. A plugin you write yourself runs from your own host program; see [Creating a Custom Plugin](#creating-a-custom-plugin).

## Architecture Overview

The plugin system is built on two interfaces: `IMokaPlugin` defines the plugin contract, and `IPluginContext` gives a plugin its options, services and logging. A host registers plugins in its .NET dependency injection container. `PluginHost` then activates the ones declared in the site configuration and runs them during every build.

Plugins run at a fixed point in the build pipeline: after Markdown parsing and before feature gating. They receive the parsed page content, and the later phases work with whatever the plugins changed or added.

### Build Pipeline Order

| Order | Phase | Description |
|-------|-------|-------------|
| 200 | Discovery | Scans for Markdown files, C# projects, and static assets |
| 300 | C# Analysis | Roslyn analysis of the projects in `content.projects`, generating API pages |
| 400 | Markdown Parse | Parses Markdown with custom extensions |
| | **Plugin hook** | **Loaded plugins run here, immediately before the first phase with order 500 or higher** |
| 500 | Feature Gate | Removes pages whose `requires` feature flag is disabled |
| 600 | Navigation Build | Builds the sidebar navigation tree |
| 700 | Search Index | Builds client-side search entries |
| 900 | Render | Applies Scriban templates |
| 1100 | Output | Writes pages and assets to the output directory (`_site/` by default) |
| 1150 | Theme Assets | Writes CSS/JS to `_theme/` |
| 1200 | Post-Process | Generates `sitemap.xml` and `robots.txt` |

Because plugins run before feature gating, the pages they add still go through feature flags, the search index and template rendering, and the HTML they change is the page body before the site layout wraps it. The ASP.NET Core host adds one more phase at order 350, which generates API pages through reflection.

## Built-in Plugins

MokaDocs ships with these built-in plugins. Add them to your `mokadocs.yaml` to enable them.

| Plugin ID | Name | Description |
|---|---|---|
| `mokadocs-repl` | [Interactive REPL](/plugins/repl) | Adds a Run button to C# code blocks; `mokadocs serve` executes them |
| `mokadocs-blazor-preview` | [Blazor Component Preview](/plugins/blazor-preview) | Compiles Razor blocks and shows them as live previews in iframes |
| `openapi` | [OpenAPI Plugin](/plugins/openapi) | Generates REST API reference pages from an OpenAPI document |
| `mokadocs-changelog` | [Release Changelog](/plugins/changelog) | Timeline UI for release notes using `:::changelog` containers |
| `mokadocs-python-api` | [Python API Reference](/plugins/python-api) | Generates API reference pages from Python source files |

In the [ASP.NET Core host](/guide/aspnetcore), `MokaDocsOptions.Plugins` declares plugins by id with their options, and `EnableRepl` and `EnableBlazorPreview` turn those two on. The REPL doesn't run code there.

## IMokaPlugin Interface

Every plugin implements `IMokaPlugin` from the `Moka.Docs.Plugins` namespace. `BuildContext` comes from `Moka.Docs.Core.Pipeline`.

```csharp
public interface IMokaPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default);
}
```

### Properties

- **Id** - A unique identifier matched against the `name` values under `plugins` in `mokadocs.yaml`, ignoring case. For example, a plugin with `Id = "mokadocs-custom-footer"` is activated by `name: mokadocs-custom-footer`.
- **Name** - A human-readable display name, used in the host's log messages.
- **Version** - A version string (e.g., `"1.0.0"`), also used in log messages.

### Methods

- **InitializeAsync** - Called once for each matching `plugins` entry when the host loads plugins, before the first build. Read and validate options here. The DI container is already built at this point, so a plugin can resolve services but can't register new ones.
- **ExecuteAsync** - Called during every build at the plugin hook. The `BuildContext` gives access to the configuration, all pages, the API model, diagnostics, and output paths. This is where the plugin does its main work.

## IPluginContext Interface

The plugin context is how a plugin reads its configuration, resolves services, and logs:

```csharp
public interface IPluginContext
{
    SiteConfig SiteConfig { get; }
    IReadOnlyDictionary<string, object> Options { get; }
    T? GetService<T>() where T : class;
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}
```

### Members

- **SiteConfig** - The site configuration the host loaded, with command-line overrides such as `--base-path` applied.
- **Options** - The `options` map of this plugin's `plugins` entry. See [Options Passing](#options-passing) for the value types.
- **GetService\<T\>()** - Resolves a service from the host's DI container, for example `ILoggerFactory`. Returns `null` if the service is not registered.
- **LogInfo / LogWarning / LogError** - Write to the host's logger. Messages are prefixed with `[Plugin]` and logged under the `PluginHost` category, not the plugin's name. See [Logging and Diagnostics](#logging-and-diagnostics).

## Plugin Lifecycle

The plugin system follows a three-phase lifecycle:

### 1. Discovery

When the host loads plugins, `PluginHost` resolves every `IMokaPlugin` registered in the DI container and matches each entry in the `plugins` section to the plugin whose `Id` equals the entry's `name`, ignoring case.

Only plugins with a matching entry are activated. Registered plugins without an entry are ignored, an entry that matches no registered plugin is logged as a warning, and an entry without a `name` is skipped. `mokadocs validate` and `mokadocs doctor` report entries whose name matches no plugin.

The same id can be declared more than once. Each entry gets its own options and context, but all entries share one plugin instance, so reset any per-entry state at the start of `ExecuteAsync`.

### 2. Initialization

For each matched entry, `InitializeAsync` is called once with that entry's options. This phase is for one-time setup:

- Validating required options
- Loading external resources or data files
- Resolving services with `GetService<T>()`

If a plugin throws during initialization, it is left out of every build for the rest of the run and the error is logged. `mokadocs validate` and `mokadocs doctor` report plugins that failed to initialize. `LogWarning` and `LogError` calls made during initialization only go to the log; they are not recorded as build diagnostics.

### 3. Execution

During each build, including every rebuild in `mokadocs serve` and `mokadocs build --watch`, `ExecuteAsync` is called for every initialized plugin. Plugins run one at a time, in the order they appear in the `plugins` list. The `BuildContext` parameter provides:

- `Pages` - every page in the build. Change a page by assigning a new `Content`, or add pages with `Pages.Add`.
- `ApiModel` - the C# API model, when the site has C# projects
- `Config`, `RootDirectory` and `OutputDirectory`
- `Diagnostics` - the build's warnings and errors
- `DeferredOutputFiles` and `DeferredOutputDirectories` - extra files and directories that the Output phase writes after it cleans the output directory
- `DryRun` - `true` under `mokadocs validate` and `mokadocs doctor`, which must not change the output directory. Deferred files are not written in a dry run.

Nothing in `BuildContext` tells a static build apart from `mokadocs serve`.

If `ExecuteAsync` throws, the build records the error `Plugin '<id>' failed: <message>` and moves on to the next plugin.

### Adding Pages

Pages a plugin adds to `buildContext.Pages` are rendered like Markdown pages. They appear in the sidebar automatically only when `mokadocs.yaml` has no `nav:` section. With a `nav:` section, the site needs an item whose `path` is the route the pages live under, such as a plugin's route prefix. See [Navigation & Sidebar](/configuration/navigation).

Pages that share a route overwrite each other, and the build reports a warning when that happens.

## Logging and Diagnostics

- **LogInfo** writes an informational message to the log. The CLI only prints log output with `--verbose`.
- **LogWarning** and **LogError** also write to the log. When called during `ExecuteAsync`, they are recorded as build diagnostics too, with the plugin's id as the source.

Build diagnostics appear in these places:

- `mokadocs build` counts warnings and errors in its summary and lists the errors (warnings too with `--verbose`). The build exits with code 1 when there are errors.
- `mokadocs validate` lists every warning and error with its source.
- `mokadocs doctor` reports the counts and points you to `validate`.
- `mokadocs serve` prints the same summary as `build` after every build and rebuild.

## Configuration

Plugins are configured in the `mokadocs.yaml` file under the `plugins` key:

```yaml
plugins:
  - name: my-plugin-id
    options:
      key1: value1
      key2: value2
      items:
        - item1
        - item2

  - name: another-plugin
    options:
      enabled: true
      outputPath: ./custom
```

### Configuration Fields

| Field     | Type   | Description |
|-----------|--------|-------------|
| `name`    | string | **Required.** Matched against the `Id` property of a registered `IMokaPlugin`, ignoring case. |
| `options` | object | Optional. A key-value map passed to the plugin via `context.Options`. |
| `path`    | string | Not supported. No host loads plugin assemblies from a path; `validate` and `doctor` warn when it is set. |

### Plugin Matching

The `name` field in the configuration is compared against the `Id` property of each registered `IMokaPlugin` implementation, ignoring case. For example:

```csharp
// In your plugin class
public string Id => "mokadocs-repl";
```

```yaml
# In mokadocs.yaml (MokaDocs-Repl would match too)
plugins:
  - name: mokadocs-repl
```

### Options Passing

The `options` map is deserialized with YamlDotNet's default rules and made available through `context.Options`:

- Scalar values arrive as strings: `enabled: true` gives `"true"` and `count: 3` gives `"3"`. Convert them with `bool.TryParse`, `int.TryParse` and similar methods.
- Nested maps arrive as `Dictionary<object, object>`.
- Lists arrive as `List<object>`.
- A key without a value arrives as `null`.
- Keys are case-sensitive.

Plugins should validate and convert options during `InitializeAsync`:

```csharp
public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
{
    if (context.Options.TryGetValue("outputPath", out object? path) && path is string pathValue)
    {
        _outputPath = pathValue;
    }

    if (context.Options.TryGetValue("enabled", out object? enabled)
        && bool.TryParse(enabled as string, out bool enabledValue))
    {
        _enabled = enabledValue;
    }

    if (context.Options.TryGetValue("items", out object? items) && items is List<object> itemList)
    {
        _items = itemList.Select(item => item.ToString() ?? "").ToList();
    }

    return Task.CompletedTask;
}
```

## DI Registration

A host registers plugins in its .NET dependency injection container as `IMokaPlugin`:

```csharp
services.AddSingleton<IMokaPlugin, MyCustomPlugin>();
```

Registration alone doesn't activate a plugin: its id also has to be declared under `plugins` in the site configuration. Where that is possible:

- **`mokadocs` CLI** - registers only the built-in plugins. You can't add your own.
- **ASP.NET Core host** (`AddMokaDocs`) - register your plugin in the application's services and add a `PluginEntry` with its id to `MokaDocsOptions.Plugins`. See [Plugins](/guide/aspnetcore#plugins).
- **Your own host** - a program that builds the site with the MokaDocs libraries, registers your plugin, and reads a `mokadocs.yaml` that declares it. [Step 3](#step-3-run-it-from-your-own-host) below shows a complete one.

## Creating a Custom Plugin

This step-by-step guide walks through creating a plugin that adds a custom footer to every documentation page, then running it from a small host program.

### Scaffolding with the CLI

Start a new plugin project with the `mokadocs new plugin` command. The name is given in kebab-case:

```bash
mokadocs new plugin custom-footer
```

This creates a `Moka.Docs.Plugins.CustomFooter/` directory in the current directory (`--path` picks another parent directory) containing:

- `Moka.Docs.Plugins.CustomFooter.csproj` - a `net9.0` class library with a PackageReference to `Moka.Docs.Plugins` at the version of your `mokadocs` tool
- `CustomFooterPlugin.cs` - a starter `IMokaPlugin` class with the id `mokadocs-custom-footer`

The command also prints a reminder that the CLI loads only built-in plugins.

### Step 1: Write the Plugin Class

Replace the generated class with the plugin logic:

```csharp
using System.Net;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;

namespace Moka.Docs.Plugins.CustomFooter;

public sealed class CustomFooterPlugin : IMokaPlugin
{
    private string _footerText = "Built with MokaDocs";
    private string _cssClass = "custom-footer";

    public string Id => "mokadocs-custom-footer";
    public string Name => "Custom Footer";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        // Option values arrive as strings.
        if (context.Options.TryGetValue("text", out object? text) && text is string textValue)
        {
            _footerText = textValue;
        }

        if (context.Options.TryGetValue("cssClass", out object? css) && css is string cssValue)
        {
            _cssClass = cssValue;
        }

        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
    {
        string footerHtml =
            $"<div class=\"{WebUtility.HtmlEncode(_cssClass)}\"><p>{WebUtility.HtmlEncode(_footerText)}</p></div>";

        foreach (DocPage page in buildContext.Pages)
        {
            // PageContent is an immutable record, so replace it with a changed copy.
            page.Content = page.Content with { Html = page.Content.Html + footerHtml };
        }

        context.LogInfo($"Added a footer to {buildContext.Pages.Count} pages");
        return Task.CompletedTask;
    }
}
```

The plugin runs before the Render phase, so the footer ends up at the bottom of each page's content, inside the site layout.

### Step 2: Configure the Plugin

Add the plugin configuration to your `mokadocs.yaml`:

```yaml
plugins:
  - name: mokadocs-custom-footer
    options:
      text: "Copyright 2026 My Company. Built with MokaDocs."
      cssClass: docs-page-footer
```

### Step 3: Run It from Your Own Host

The `mokadocs` CLI can't load this plugin, so build the site from a console app (`dotnet new console`) with a project reference to the plugin project. `Moka.Docs.Plugins` brings in the build engine, and this `Program.cs` runs a full build:

```csharp
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.CSharp;
using Moka.Docs.Engine;
using Moka.Docs.Parsing;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.CustomFooter;

string configPath = Path.GetFullPath(args.Length > 0 ? args[0] : "mokadocs.yaml");
string rootDir = Path.GetDirectoryName(configPath)!;
SiteConfig config = new SiteConfigReader(new FileSystem()).Read(configPath);

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IFileSystem>(new FileSystem());
services.AddSingleton(config);
services.AddMokaDocsParsing();
services.AddMokaDocsCSharp();
services.AddMokaDocsEngine();
services.AddMokaDocsPlugins();
services.AddSingleton<IMokaPlugin, CustomFooterPlugin>();

await using ServiceProvider provider = services.BuildServiceProvider();

PluginHost pluginHost = provider.GetRequiredService<PluginHost>();
await pluginHost.DiscoverAndInitializeAsync();

BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
pipeline.PluginHook = (context, ct) => pluginHost.ExecuteAllAsync(context, ct);

var buildContext = new BuildContext
{
    Config = config,
    FileSystem = new FileSystem(),
    RootDirectory = rootDir,
    OutputDirectory = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output))
};

await pipeline.ExecuteAsync(buildContext);

foreach (Diagnostic diagnostic in buildContext.Diagnostics.All)
{
    Console.WriteLine($"{diagnostic.Severity} {diagnostic.Source}: {diagnostic.Message}");
}

return buildContext.Diagnostics.HasErrors ? 1 : 0;
```

Run it with the path to your `mokadocs.yaml`:

```bash
dotnet run -- ../docs-site/mokadocs.yaml
```

Every page in the built site now ends with the custom footer, and the footer text and CSS class come from `mokadocs.yaml`.

This host covers a plain build. It has no dev server, watch mode or versioning, and `AddMokaDocsPlugins` registers only the Python API plugin among the built-ins. Register others the same way, for example `services.AddSingleton<IMokaPlugin, ChangelogPlugin>()` from `Moka.Docs.Plugins.Changelog`.

### Plugin Best Practices

- **Keep plugins focused.** Each plugin should do one job, which keeps its options and failures easy to understand.
- **Validate options early.** Check required options in `InitializeAsync`. Problems you report there only reach the log, so also report them with `LogWarning` or `LogError` from `ExecuteAsync` if they should show up in the build output.
- **Use cancellation tokens.** Pass the `CancellationToken` to any async operations.
- **Log meaningfully.** Use `LogInfo` for progress, `LogWarning` for problems the build can survive, and `LogError` for problems that break the output. Errors make `mokadocs build` exit with code 1.
- **Handle missing services gracefully.** When calling `GetService<T>()`, always check for `null` returns.
- **Reset per-entry state.** If your plugin can be declared more than once, reset its fields at the start of `ExecuteAsync`.
- **Respect dry runs.** When `buildContext.DryRun` is set, don't write to the output directory yourself. Files queued in `DeferredOutputFiles` are skipped for you.
