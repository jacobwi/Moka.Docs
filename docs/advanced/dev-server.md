---
title: Dev Server & Hot Reload
order: 4
---

# Dev Server & Hot Reload

MokaDocs includes a built-in development server that serves your documentation locally with automatic hot reload. This gives you a fast feedback loop while writing docs — save a file, and the browser updates immediately.

## Starting the Dev Server

```bash
mokadocs serve
```

By default, the server starts on port 5080. Open `http://localhost:5080` in your browser to view your documentation.

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--port <number>` | Port number for the server | `5080` |
| `--config <path>` | Path to the configuration file | `mokadocs.yaml` |
| `--output <path>` | Output directory for built files | Value from config |
| `--verbose` | Enable verbose logging for debugging | Off |

### Examples

```bash
# Default: serve on port 5080
mokadocs serve

# Use a custom port
mokadocs serve --port 3000

# Serve with verbose logging
mokadocs serve --verbose

# Use a different config file
mokadocs serve --config mokadocs.dev.yaml
```

## Hot Reload Mechanism

The dev server provides automatic browser refresh when source files change. Here is how it works:

1. **File Watching**: A `FileSystemWatcher` monitors two locations:
   - The `docs/` directory (and all subdirectories) for Markdown, image, and asset changes
   - The `mokadocs.yaml` configuration file for setting changes

2. **Rebuild Trigger**: When a watched file is created, modified, or deleted, the server triggers a rebuild of the documentation site. The rebuild runs the full build pipeline (discovery, parsing, rendering, output).

3. **WebSocket Notification**: After the rebuild completes, the server sends a reload notification through a WebSocket connection to all connected browsers.

4. **Browser Refresh**: A small JavaScript client injected into every served page listens on the WebSocket connection. When it receives a reload notification, it automatically refreshes the page.

The entire cycle — from saving a file to seeing the updated page — typically completes in under a second for most documentation sites.

### Debouncing

File system events are debounced to prevent multiple rapid rebuilds when a single save operation triggers multiple file system notifications. The server waits for a brief quiet period (typically 100-200ms) after the last file system event before starting a rebuild.

## Static File Serving

The dev server serves files from the build output directory. On startup, it runs an initial build and then serves the resulting files.

### Clean URLs

The server supports clean URLs to match production behavior. When a request comes in for a path like `/guide`, the server looks for `/guide/index.html` and serves it. This means your local preview matches exactly how the site will behave when deployed.

| Request | Resolved File |
|---------|---------------|
| `/` | `/index.html` |
| `/guide` | `/guide/index.html` |
| `/guide/getting-started` | `/guide/getting-started/index.html` |
| `/api/MyClass` | `/api/MyClass/index.html` |

### Content Types

The server sets appropriate `Content-Type` headers based on file extensions:

| Extension | Content-Type |
|-----------|-------------|
| `.html` | `text/html` |
| `.css` | `text/css` |
| `.js` | `application/javascript` |
| `.json` | `application/json` |
| `.png` | `image/png` |
| `.svg` | `image/svg+xml` |
| `.woff2` | `font/woff2` |

## Custom 404 Page

When a requested route does not match any file, the server returns a styled 404 page. The 404 page is auto-generated and supports both dark and light themes (matching the site's configured theme mode). It includes a link back to the documentation homepage.

## Plugin API Endpoints

When certain plugins are active, the dev server exposes additional API endpoints.

### REPL Endpoint

**Available when**: The REPL plugin is enabled in your configuration.

```
POST /api/repl/execute
Content-Type: application/json

{
  "code": "Console.WriteLine(\"Hello, world!\");",
  "references": ["System.Linq"]
}
```

This endpoint accepts C# code, compiles and executes it using Roslyn, and returns the output. It powers the interactive code examples on your documentation pages.

**Response:**
```json
{
  "success": true,
  "output": "Hello, world!\n",
  "error": null,
  "executionTimeMs": 42
}
```

### Blazor Preview Endpoint

**Available when**: The Blazor plugin is enabled in your configuration.

```
POST /api/blazor/preview
Content-Type: application/json

{
  "component": "MyComponent",
  "parameters": {
    "Title": "Hello"
  }
}
```

This endpoint renders a Blazor component with the given parameters and returns the HTML output. It powers live component previews in your documentation.

**Response:**
```json
{
  "success": true,
  "html": "<div class=\"my-component\"><h1>Hello</h1></div>",
  "error": null
}
```

## CORS Headers

The dev server includes permissive CORS headers to allow local development with tools like browser extensions or separate frontend dev servers:

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

These headers are only set in the dev server and are not present in production builds.

## Port Selection

If the requested port is already in use, the server will report an error and exit. To resolve this:

- Specify a different port with `--port`
- Stop the other process using the port
- On macOS/Linux, find the process using the port: `lsof -i :5080`

```bash
# If port 5080 is busy, try another port
mokadocs serve --port 5081
```

## Troubleshooting

### Changes Not Appearing

- Verify the file is inside the watched `docs/` directory.
- Check the terminal output for build errors after saving.
- Try a hard refresh in the browser (`Ctrl+Shift+R` or `Cmd+Shift+R`).
- Restart the server if the WebSocket connection was lost.

### Slow Rebuilds

- Large documentation sites with many API types may take longer to rebuild.
- Use `--verbose` to see which build phases are taking the most time.
- Consider splitting large C# projects if API analysis is the bottleneck.

### WebSocket Connection Issues

- The WebSocket client reconnects automatically if the connection drops.
- If hot reload stops working, check for browser extensions that might block WebSocket connections.
- The WebSocket endpoint is at `ws://localhost:<port>/_ws`.
