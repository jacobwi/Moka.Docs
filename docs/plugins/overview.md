---
title: Plugin System
order: 1
---

# Plugin System

MokaDocs includes an extensible plugin system that allows you to add custom functionality to your documentation site. Plugins can modify page content, inject scripts and styles, generate new pages, and hook into the build pipeline.

## Architecture Overview

The plugin system is built on two core interfaces: `IMokaPlugin` defines the plugin contract, and `IPluginContext` provides access to configuration, services, and logging. Plugins are discovered through .NET dependency injection, initialized once at startup, and executed during each build cycle.

Plugins participate in the build pipeline at a specific ordering point, giving them access to parsed content while still allowing downstream steps (like navigation generation) to incorporate any changes the plugin makes.

### Build Pipeline Order

| Order | Stage                  |
|-------|------------------------|
| 100   | Content discovery      |
| 200   | Front matter parsing   |
| 300   | Asset processing       |
| 400   | Markdown parsing       |
| **500** | **Plugin execution** |
| 600   | Navigation generation  |
| 700   | Template rendering     |
| 800   | Output writing         |

Because plugins run at order 500, they receive fully parsed HTML from the markdown stage and can modify it before navigation and template rendering occur. This means plugins can add new pages that will appear in the sidebar, or alter page HTML that will be wrapped in the site layout.

## Built-in Plugins

MokaDocs ships with several built-in plugins that are ready to use. Add them to your `mokadocs.yaml` to enable them.

| Plugin ID | Name | Description |
|---|---|---|
| `mokadocs-repl` | [Interactive REPL](/plugins/repl) | Adds runnable C# code blocks with live output |
| `mokadocs-blazor-preview` | [Blazor Component Preview](/plugins/blazor-preview) | Renders Razor components as live HTML previews |
| `openapi` | [OpenAPI Plugin](/plugins/openapi) | Generates API reference pages from OpenAPI 3.0 specs |
| `mokadocs-changelog` | [Release Changelog](/plugins/changelog) | Rich timeline UI for release notes using `:::changelog` containers |

## IMokaPlugin Interface

Every plugin must implement the `IMokaPlugin` interface:

```csharp
public interface IMokaPlugin {
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(IPluginContext context, CancellationToken ct);
    Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct);
}
```

### Properties

- **Id** -- A unique identifier string used to match the plugin to its configuration entry. This must match the `name` value in `mokadocs.yaml`. For example, a plugin with `Id = "my-footer-plugin"` is activated by `name: my-footer-plugin` in the config.
- **Name** -- A human-readable display name for the plugin, shown in build logs and diagnostics.
- **Version** -- A semantic version string (e.g., `"1.0.0"`) for tracking plugin compatibility.

### Methods

- **InitializeAsync** -- Called once when the plugin is first loaded. Use this to validate configuration, set up resources, or register services. The `IPluginContext` provides access to site configuration and plugin-specific options.
- **ExecuteAsync** -- Called during each build cycle at order 500 in the pipeline. The `BuildContext` provides access to all pages, assets, and output paths. This is where the plugin performs its main work.

## IPluginContext Interface

The plugin context is the primary interface for plugins to interact with the MokaDocs environment:

```csharp
public interface IPluginContext {
    SiteConfig SiteConfig { get; }
    IReadOnlyDictionary<string, object> Options { get; }
    T? GetService<T>() where T : class;
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}
```

### Members

- **SiteConfig** -- The fully resolved site configuration from `mokadocs.yaml`. Contains the site title, base URL, theme settings, and all other configuration values.
- **Options** -- A read-only dictionary of plugin-specific options from the `options` block in `mokadocs.yaml`. Values are deserialized as `object` and can be cast to their expected types (strings, lists, numbers, etc.).
- **GetService\<T\>()** -- Resolves a service from the dependency injection container. Use this to access built-in MokaDocs services or services registered by other plugins. Returns `null` if the service is not registered.
- **LogInfo / LogWarning / LogError** -- Structured logging methods that write to the MokaDocs build output. Messages are prefixed with the plugin name for easy identification.

## Plugin Lifecycle

The plugin system follows a three-phase lifecycle:

### 1. Discovery

During application startup, MokaDocs scans the DI container for all registered `IMokaPlugin` implementations. Each discovered plugin is matched against the `plugins` section in `mokadocs.yaml` by comparing the plugin's `Id` property to the `name` field in the configuration.

Only plugins that have a matching configuration entry are activated. Registered plugins without a config entry are ignored, and config entries without a matching registered plugin produce a warning in the build log.

### 2. Initialization

For each matched plugin, `InitializeAsync` is called once. Plugins receive their `IPluginContext` with the resolved `Options` dictionary. This phase is for one-time setup tasks:

- Validating required options are present
- Creating output directories
- Loading external resources or data files
- Registering additional services in the container

If a plugin throws an exception during initialization, it is disabled for the remainder of the build and an error is logged.

### 3. Execution

During each build (or rebuild in serve/watch mode), `ExecuteAsync` is called for every initialized plugin. The `BuildContext` parameter provides:

- Access to all discovered pages and their parsed HTML
- The output directory path
- The current build mode (static build vs. serve mode)
- Methods to add new pages or modify existing ones

Plugins execute sequentially in the order they appear in the `plugins` configuration list.

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
| `name`    | string | **Required.** Must match the `Id` property of a registered `IMokaPlugin`. |
| `options` | object | Optional. A key-value map passed to the plugin via `context.Options`. |

### Plugin Matching

The `name` field in the configuration is compared against the `Id` property of each registered `IMokaPlugin` implementation. Matching is case-sensitive and must be an exact match. For example:

```csharp
// In your plugin class
public string Id => "mokadocs-repl";
```

```yaml
# In mokadocs.yaml -- must match exactly
plugins:
  - name: mokadocs-repl
```

### Options Passing

The `options` dictionary from the YAML configuration is deserialized and made available through `context.Options`. Nested objects become `Dictionary<string, object>`, and arrays become `List<object>`. Plugins should validate and cast options during `InitializeAsync`.

```csharp
public Task InitializeAsync(IPluginContext context, CancellationToken ct)
{
    if (context.Options.TryGetValue("outputPath", out var value))
    {
        _outputPath = value?.ToString() ?? "./default";
    }
    return Task.CompletedTask;
}
```

## DI Registration

Plugins are registered in the .NET dependency injection container as `IMokaPlugin`. This is typically done in your project's startup or service configuration:

```csharp
services.AddSingleton<IMokaPlugin, MyCustomPlugin>();
```

For plugins distributed as NuGet packages, an extension method pattern is conventional:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyPlugin(this IServiceCollection services)
    {
        services.AddSingleton<IMokaPlugin, MyCustomPlugin>();
        // Register any additional services the plugin needs
        services.AddSingleton<MyPluginService>();
        return services;
    }
}
```

Then in the host application:

```csharp
builder.Services.AddMyPlugin();
```

## Creating a Custom Plugin

This step-by-step guide walks through creating a plugin that adds a custom footer to every documentation page.

### Scaffolding with the CLI

The fastest way to start a new plugin is with the `mokadocs new plugin` command, which generates a `.csproj` file and a starter `IMokaPlugin` implementation:

```bash
mokadocs new plugin MyCustomPlugin
```

This creates a ready-to-build project structure. You can then modify the generated class to implement your plugin logic.

### Step 1: Create the Plugin Class

Create a new class that implements `IMokaPlugin`:

```csharp
using MokaDocs.Plugins;

public class CustomFooterPlugin : IMokaPlugin
{
    public string Id => "custom-footer";
    public string Name => "Custom Footer Plugin";
    public string Version => "1.0.0";

    private string _footerText = "Built with MokaDocs";
    private string _cssClass = "custom-footer";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        // Read options from mokadocs.yaml
        if (context.Options.TryGetValue("text", out var text))
        {
            _footerText = text?.ToString() ?? _footerText;
        }

        if (context.Options.TryGetValue("cssClass", out var cssClass))
        {
            _cssClass = cssClass?.ToString() ?? _cssClass;
        }

        context.LogInfo($"Initialized with footer text: {_footerText}");
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(
        IPluginContext context,
        BuildContext buildContext,
        CancellationToken ct)
    {
        var footerHtml = $@"
            <style>
                .{_cssClass} {{
                    margin-top: 3rem;
                    padding-top: 1.5rem;
                    border-top: 1px solid var(--color-border);
                    text-align: center;
                    font-size: 0.875rem;
                    color: var(--color-text-muted);
                }}
            </style>
            <div class=""{_cssClass}"">
                <p>{_footerText}</p>
            </div>";

        foreach (var page in buildContext.Pages)
        {
            // Append footer HTML to each page's content
            page.ContentHtml += footerHtml;
        }

        context.LogInfo($"Added footer to {buildContext.Pages.Count} pages");
        return Task.CompletedTask;
    }
}
```

### Step 2: Register the Plugin

Add the plugin to your DI container in the application startup:

```csharp
builder.Services.AddSingleton<IMokaPlugin, CustomFooterPlugin>();
```

### Step 3: Configure the Plugin

Add the plugin configuration to your `mokadocs.yaml`:

```yaml
plugins:
  - name: custom-footer
    options:
      text: "Copyright 2025 My Company. Built with MokaDocs."
      cssClass: site-footer
```

### Step 4: Build and Verify

Run a build or start the dev server to see the footer applied:

```bash
mokadocs build
# or
mokadocs serve
```

Every page in your documentation site will now display the custom footer below the page content. The footer text and CSS class are configurable through `mokadocs.yaml`, so you can change them without modifying the plugin code.

### Plugin Best Practices

- **Keep plugins focused.** Each plugin should do one thing well. Combine multiple small plugins rather than building one large one.
- **Validate options early.** Check for required options in `InitializeAsync` and log clear error messages if they are missing.
- **Use cancellation tokens.** Pass the `CancellationToken` to any async operations so builds can be cancelled cleanly.
- **Log meaningfully.** Use `LogInfo` for progress, `LogWarning` for non-fatal issues, and `LogError` for problems that affect output quality.
- **Handle missing services gracefully.** When calling `GetService<T>()`, always check for `null` returns.
- **Respect build mode.** Use `buildContext` to check whether you are in a static build or serve mode, and adjust behavior accordingly (e.g., skip runtime-only features during static builds).
