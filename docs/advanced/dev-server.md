---
title: Dev Server & Hot Reload
order: 4
---

# Dev Server & Hot Reload

`mokadocs serve` builds the site, serves the output folder on this machine only, and rebuilds and reloads the browser when a file in the docs folder changes.

## Starting the Dev Server

```bash
mokadocs serve
```

The server listens on port 5080 by default: open `http://localhost:5080`. It opens your browser there unless you pass `--no-open`. `http://127.0.0.1:5080` works too, and on Windows `http://[::1]:5080`. When the system refuses one of those addresses, for example through a Windows URL reservation, the server answers on the rest and says so with `--verbose`.

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
3. **Rebuild**: the whole build pipeline runs again, writing to the output folder. One rebuild runs at a time. Changes saved while it runs start one more rebuild when it ends, which picks up all of them.
4. **Reload**: after the rebuild, the server sends `reload` over a WebSocket to every connected browser. The terminal prints the warning and error counts and lists the errors, and `--verbose` lists the warnings too. A build that reports errors still reloads the browser. If the rebuild throws, the terminal shows `Build failed:` with the message and browsers aren't reloaded.
5. **Browser script**: the server adds a small script before `</body>` in every HTML response. It connects to `ws://<host>/__mokadocs-ws`, reloads the page when it receives `reload`, and reconnects every second when the connection closes.

Changes inside the output folder and `.mokadocs` are ignored, and so are files and folders below the docs folder whose names start with `.`, such as `.git`.

### Configuration Changes

When `mokadocs.yaml` changes, the rebuild starts by reading it again. Everything that comes from it is set up again before the build: the theme and its parsed templates, the navigation, the versions and the plugins, so a plugin you add to `plugins` starts running, and a plugin loaded from a `path` is built and loaded again. Saving the file without changing it doesn't reload anything.

A file that doesn't parse leaves the site as it was. The terminal prints the error and `Still serving the last good site`, and no rebuild runs, even for docs changes, until you fix the file and save it again.

Some settings are fixed while `serve` runs. When the new file changes them, the terminal prints a line such as `build.basePath changed from '/' to '/docs', which takes a restart. Still using '/'.` and the old value stays until you restart:

- `build.basePath`, which the server strips from request paths
- `build.output`, the folder the server serves
- `content.docs`, the folder it watches
- the `mokadocs-repl` entry, since the REPL endpoint and its packages are set up at startup

The port is a command-line option, so it can only change with a restart too. `--output` and `--base-path` keep overriding the file after a reload.

### Polling

Where the operating system can't watch a folder, such as on Linux past the inotify limits, `serve` logs a warning and checks the folder for changes every second instead. Set the `DOTNET_USE_POLLING_FILE_WATCHER` environment variable to `1` or `true` to poll from the start, which helps on network drives and some container mounts that don't report changes. When more changes arrive at once than the watcher can list, `serve` rebuilds anyway.

### What Isn't Picked Up

- **Some configuration settings**: `build.basePath`, `build.output`, `content.docs` and the `mokadocs-repl` entry need a restart. See [Configuration Changes](#configuration-changes).
- **Theme changes**: the theme and its parsed templates are cached until `mokadocs.yaml` changes, and a custom theme folder outside `content.docs` isn't watched. Restart `serve` after editing a theme.
- **Plugin source code**: a plugin loaded from a project `path` is built when `serve` starts and when `mokadocs.yaml` changes, not when its own files change.
- **C# source files**: project folders aren't watched. After changing code, save any file in the docs folder to rebuild. A project whose `.cs` files changed is analyzed again, since its cache entry no longer matches.

### Output Folder Inside the Docs Folder

`build.output` can be a folder inside `content.docs`, as in this repository, which builds to `docs/_site`. The files a rebuild writes there don't trigger another rebuild, and the build doesn't read them as content.

## Static File Serving

The server runs one build at startup and then serves files from the output folder. File responses carry `Cache-Control: no-cache, no-store, must-revalidate`, so the browser always fetches the latest build. Files answer `GET` and `HEAD`; other methods get `405`.

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
| `.js`, `.mjs` | `application/javascript; charset=utf-8` |
| `.json` | `application/json; charset=utf-8` |
| `.xml` | `application/xml; charset=utf-8` |
| `.txt` | `text/plain; charset=utf-8` |
| `.yaml`, `.yml` | `application/yaml; charset=utf-8` |
| `.svg` | `image/svg+xml` |
| `.map` | `application/json` |
| `.wasm` | `application/wasm` |
| `.zip` | `application/zip` |
| Anything else | The type ASP.NET Core's static file middleware uses, such as `image/png`, `image/avif`, `application/pdf`, `video/mp4`, `video/webm` and `application/manifest+json` for `.webmanifest`; `application/octet-stream` for an extension it doesn't know |

## 404 Page

When no file matches, the server returns the build's `404.html` with status 404, or a plain-text `404 Not Found` when that file doesn't exist.

The build generates `404.html` with the site title, a link back to the home page, and the theme's `_theme/css/main.css` and `_theme/js/main.js`. The page starts in light mode. The theme script then applies the reader's saved choice, or dark mode when there is no saved choice and the system prefers it.

## API Endpoints

The server accepts `POST` requests on two endpoints. The same paths also work under the base path, so a site built with `--base-path /my-repo` can post to `/my-repo/api/feedback`.

### Request Rules

Every `POST` to `/api/*` needs `Content-Type: application/json`. A browser request must also come from the origin of the address it was sent to: `http://localhost:<port>`, `http://127.0.0.1:<port>` or, on Windows, `http://[::1]:<port>`, matching the request's `Host` header. A request whose `Origin` header names another origin (a page on `localhost` posting to `127.0.0.1` included), or whose `Sec-Fetch-Site` header is anything other than `same-origin` or `none`, gets `403`. Requests without those headers, such as from `curl`, are accepted.

The server sends no CORS headers, and a preflight `OPTIONS` request gets a `405`, so pages on other origins can't call these endpoints.

A body that isn't JSON gets `400`. The REPL endpoint reads at most 64 KB of body and the feedback endpoint 4 KB, the same limits as the ASP.NET Core host; a longer body gets `413` without being read.

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

Runs happen one at a time, and up to four more wait their turn. Past that, the endpoint answers `429` with `{"error": "Too many snippets are waiting to run. Try again in a few seconds."}`.

### Feedback: `POST /api/feedback`

The default theme's "Was this page helpful?" widget posts here:

```json
{ "page": "/guide/getting-started", "helpful": true }
```

The server logs the vote at Information level, which only prints with `--verbose`, and replies `{"ok": true}`. A vote without a page or a `helpful` value, or with a page over 1,024 characters or containing control characters, gets `400` with `{"ok": false}`.

## Port Selection

The server starts listening after the initial build, and only on the loopback addresses. If it can't, for example because the port is in use, `serve` prints `Error: Can't listen on port 5080` with the reason from the operating system and exits with code 1. Then either:

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
- If no change triggers a rebuild, check that the file isn't inside the output folder or in a folder whose name starts with `.`. On a network drive or a container mount, set `DOTNET_USE_POLLING_FILE_WATCHER=1` (see [Polling](#polling)).
- Try a hard refresh in the browser (`Ctrl+Shift+R`, or `Cmd+Shift+R` on macOS).

### Slow Rebuilds

- Every rebuild runs the whole pipeline. `--verbose` logs each phase as it starts, without timings, and the total time is printed after each build.
- C# analysis results are cached in `.mokadocs/cache` while `build.cache` is on (the default), so unchanged projects aren't analyzed again. With `build.cache: false`, every rebuild analyzes them.

### WebSocket Connection Issues

- The browser script reconnects every second after the connection drops, but reconnecting doesn't reload the page. Reload by hand after restarting `serve`.
- The WebSocket endpoint is `ws://localhost:<port>/__mokadocs-ws`, at the root even when a base path is set.
- If hot reload stops working, check for browser extensions that block WebSocket connections.
