---
title: Dev Server & Hot Reload
order: 4
---

# Dev Server & Hot Reload

`mokadocs serve` builds the site, serves the output folder on `localhost`, and rebuilds and reloads the browser when a file in the docs folder changes.

## Starting the Dev Server

```bash
mokadocs serve
```

The server listens on port 5080 by default: open `http://localhost:5080`. It opens your browser there unless you pass `--no-open`.

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--port <number>` | `-p` | Port to listen on | `5080` |
| `--config <path>` | `-c` | Configuration file | `mokadocs.yaml` in the current folder, or `mokadocs.yml` when only that exists |
| `--output <path>` | `-o` | Folder to build into and serve | `build.output` |
| `--base-path <path>` | | Base path the site is served under | `build.basePath` |
| `--draft` | | Include pages with `visibility: draft` | Off |
| `--open` | | Open the browser after the server starts | On |
| `--no-open` | | Don't open the browser | Off |
| `--verbose` | `-v` | Print pipeline and server logging | Off |

`serve` has no `--no-cache` option; it follows `build.cache`. See [CLI Commands](/advanced/cli-reference) for the other commands.

### Examples

```bash
# Serve on port 5080
mokadocs serve

# Use another port without opening the browser
mokadocs serve --port 3000 --no-open

# Include draft pages, with pipeline logging
mokadocs serve --draft --verbose

# Preview a site built for https://user.github.io/my-repo/ at http://localhost:5080/my-repo/
mokadocs serve --base-path /my-repo

# Use a different config file
mokadocs serve --config mokadocs.dev.yaml
```

In Git Bash on Windows, run `--base-path` commands with `MSYS_NO_PATHCONV=1`; see [Base Path](/advanced/deployment#base-path).

## Hot Reload

1. **File watching**: the server watches the `content.docs` folder, including subfolders, and the configuration file.
2. **Debounce**: a rebuild starts 300 ms after the last change, so a save that fires several file system events causes one rebuild.
3. **Rebuild**: the whole build pipeline runs again, writing to the output folder.
4. **Reload**: after the rebuild, the server sends `reload` over a WebSocket to every connected browser. The terminal prints the warning and error counts and lists the errors, and `--verbose` lists the warnings too. A build that reports errors still reloads the browser. If the rebuild throws, the terminal shows `Build failed:` with the message and browsers aren't reloaded.
5. **Browser script**: the server adds a small script before `</body>` in every HTML response. It connects to `ws://<host>/__mokadocs-ws`, reloads the page when it receives `reload`, and reconnects every second when the connection closes.

Changes inside the output folder are ignored, and so are files and folders below the docs folder whose names start with `.`, such as `.git`.

### What Isn't Picked Up

- **Configuration changes**: saving `mokadocs.yaml` triggers a rebuild, but the rebuild uses the configuration loaded at startup. Restart `serve` to apply the change.
- **Theme changes**: the theme and its parsed templates are cached for the life of the process, and a custom theme folder outside `content.docs` isn't watched. Restart `serve` after editing a theme.
- **C# source files**: project folders aren't watched. After changing code, save any file in the docs folder to rebuild. A project whose `.cs` files changed is analyzed again, since its cache entry no longer matches.

### Output Folder Inside the Docs Folder

`build.output` can be a folder inside `content.docs`, as in this repository, which builds to `docs/_site`. The files a rebuild writes there don't trigger another rebuild, and the build doesn't read them as content.

## Static File Serving

The server runs one build at startup and then serves files from the output folder. File responses carry `Cache-Control: no-cache, no-store, must-revalidate`, so the browser always fetches the latest build.

### Clean URLs

A request for a folder is served from the folder's `index.html`, and every page is written as `<route>/index.html`. A path without an extension that matches no file or folder is tried again with `.html` added.

| Request | Served file |
|---------|-------------|
| `/` | `index.html` |
| `/guide` | `guide/index.html` |
| `/guide/getting-started` | `guide/getting-started/index.html` |
| `/api/mylibrary/widget` | `api/mylibrary/widget/index.html` |

Requests can't reach files outside the output folder. Paths containing `..` and paths that resolve outside the folder are not served.

### Base Path

With a base path (`build.basePath` or `--base-path`), the server removes the prefix from request paths, so `/my-repo/guide` serves `guide/index.html`. Paths without the prefix are served too.

### Content Types

| Extension | Content-Type |
|-----------|--------------|
| `.html`, `.htm` | `text/html; charset=utf-8` |
| `.css` | `text/css; charset=utf-8` |
| `.js` | `application/javascript; charset=utf-8` |
| `.json` | `application/json; charset=utf-8` |
| `.xml` | `application/xml; charset=utf-8` |
| `.txt` | `text/plain; charset=utf-8` |
| `.svg` | `image/svg+xml` |
| `.png`, `.gif`, `.webp` | `image/png`, `image/gif`, `image/webp` |
| `.jpg`, `.jpeg` | `image/jpeg` |
| `.ico` | `image/x-icon` |
| `.woff`, `.woff2`, `.ttf` | `font/woff`, `font/woff2`, `font/ttf` |
| `.eot` | `application/vnd.ms-fontobject` |
| `.map` | `application/json` |
| Anything else | `application/octet-stream` |

## 404 Page

When no file matches, the server returns the build's `404.html` with status 404, or a plain-text `404 Not Found` when that file doesn't exist.

The build generates `404.html` with the site title, a link back to the home page, and the theme's `_theme/css/main.css` and `_theme/js/main.js`. The page starts in light mode. The theme script then applies the reader's saved choice, or dark mode when there is no saved choice and the system prefers it.

## API Endpoints

The server accepts `POST` requests on three endpoints. The same paths also work under the base path, so a site built with `--base-path /my-repo` can post to `/my-repo/api/feedback`.

### Request Rules

Every `POST` to `/api/*` needs `Content-Type: application/json`. A browser request must also come from the server's own origin, `http://localhost:<port>`. A request whose `Origin` header names another origin (`http://127.0.0.1:5080` included), or whose `Sec-Fetch-Site` header is anything other than `same-origin` or `none`, gets `403`. Requests without those headers, such as from `curl`, are accepted.

The server sends no CORS headers, and a preflight `OPTIONS` request gets a 404, so pages on other origins can't call these endpoints.

### REPL: `POST /api/repl/execute`

Available when the `mokadocs-repl` plugin is declared in `mokadocs.yaml`. Without it, the endpoint returns `503` with `{"error": "REPL service is not available."}`.

The endpoint runs the posted C# script in a worker process that `serve` starts, with the permissions of the user running `serve`. Scripts are limited to 10,000 characters and 5 seconds; a script that runs longer has its worker killed and replaced, so it can't hang the server.

```
POST /api/repl/execute
Content-Type: application/json

{ "code": "1 + 2" }
```

```json
{ "output": "3", "error": "" }
```

`output` holds what the script wrote to the console, or its return value when it wrote nothing. Compilation and runtime errors come back in `error` with status 200. See [REPL plugin](/plugins/repl).

### Blazor Preview: `POST /api/blazor/preview`

Available when the `mokadocs-blazor-preview` plugin is declared. Without it, the endpoint returns `503` with `{"error": "Blazor preview service is not available."}`.

The body carries Razor source, up to 50,000 characters. The server compiles it and renders the first component it finds to HTML.

```
POST /api/blazor/preview
Content-Type: application/json

{ "source": "<h1>Hello</h1>" }
```

```json
{ "html": "<rendered HTML>", "error": "" }
```

Compilation and rendering errors come back in `error`. See [Blazor Component Preview](/plugins/blazor-preview).

### Feedback: `POST /api/feedback`

The default theme's "Was this page helpful?" widget posts here:

```json
{ "page": "/guide/getting-started", "helpful": true }
```

The server logs the vote at Information level, which only prints with `--verbose`, and replies `{"ok": true}`.

## Port Selection

The server starts listening after the initial build, and only on `localhost`. If it can't, for example because the port is in use, `serve` prints `Error: Can't listen on port 5080` with the reason from the operating system and exits with code 1. Then either:

- Pick another port with `--port`.
- Stop the other process. On macOS and Linux, `lsof -i :5080` shows it; on Windows, `netstat -ano | findstr :5080` shows its process ID.

```bash
# If port 5080 is busy, try another port
mokadocs serve --port 5081
```

## Troubleshooting

### Changes Not Appearing

- Check that the file is inside the `content.docs` folder. See [What Isn't Picked Up](#what-isnt-picked-up) for changes that need a restart.
- Check the terminal after the rebuild. Errors are always listed; run with `--verbose` to list warnings as well.
- If no change triggers a rebuild, check that the file isn't inside the output folder or in a folder whose name starts with `.`.
- Try a hard refresh in the browser (`Ctrl+Shift+R`, or `Cmd+Shift+R` on macOS).

### Slow Rebuilds

- Every rebuild runs the whole pipeline. `--verbose` logs each phase as it starts, without timings, and the total time is printed after each build.
- C# analysis results are cached in `.mokadocs/cache` while `build.cache` is on (the default), so unchanged projects aren't analyzed again. With `build.cache: false`, every rebuild analyzes them.

### WebSocket Connection Issues

- The browser script reconnects every second after the connection drops, but reconnecting doesn't reload the page. Reload by hand after restarting `serve`.
- The WebSocket endpoint is `ws://localhost:<port>/__mokadocs-ws`, at the root even when a base path is set.
- If hot reload stops working, check for browser extensions that block WebSocket connections.
