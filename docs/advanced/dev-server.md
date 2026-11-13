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

The server listens on port 5080 by default: open `http://localhost:5080`. It opens your browser there unless you pass `--no-open`. `http://127.0.0.1:5080` and `http://[::1]:5080` work too; on a machine without IPv6 the server answers on `127.0.0.1` only. The server runs on Kestrel and listens on those loopback addresses only, so other machines can't reach it. It ignores `ASPNETCORE_URLS` and any `appsettings.json` in the folder you run it from. A request whose `Host` header names anything other than `localhost`, `127.0.0.1` or `[::1]`, such as a DNS name pointed at `127.0.0.1`, gets `400`.

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
| `--no-lab` | | Leave the [theme lab](#theme-lab) out of served pages | Off |
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
5. **Browser script**: the server adds a small script before `</body>` in every `.html` page it serves, the 404 page included. Blazor preview frames under `/_preview-wasm/` don't get it, since the page that holds them already reloads. The script connects to `ws://<host>/__mokadocs-ws`, reloads the page when it receives `reload`, and reconnects every second when the connection closes. The [theme lab's](#theme-lab) script follows it, unless `--no-lab` is set. The socket only accepts connections from the server's own pages: a handshake whose `Origin` is anything other than `http://localhost:<port>`, `http://127.0.0.1:<port>` or `http://[::1]:<port>` gets `403`.

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

## Theme Lab

The theme lab tries the default theme's looks on your own pages and saves the ones you keep to `mokadocs.yaml`. It exists only under `serve`: `mokadocs build` never writes it into a site, and the ASP.NET Core host doesn't have it. `--no-lab` leaves it out.

Click **Theme lab** at the bottom left of any page, or Tab to it and press Enter. The panel opens above the button and lists:

| Group | Options | Saved under `theme.options` |
|---|---|---|
| Look | Preset, style, color preset, code theme, code style | `preset`, `style`, `defaultColorTheme`, `codeTheme`, `codeStyle` |
| Layout | Every part of [Page Layout](/themes/customization#page-layout), and the sidebar toggle button | `layout.*`, `sidebarToggle` |
| Banner | Text, link, variant | `banner.text`, `banner.link`, `banner.variant` |

The lists come from MokaDocs itself, so a new style, preset or layout variant shows up in the lab with the release that adds it. The panel doesn't trap focus: Tab moves on to the page, and Escape closes the panel and puts focus back on the button. It stays open across page loads in the same tab until you close it.

### Live Preview and Rebuilds

Style, color preset, code theme, code style and most layout variants switch an attribute on the page's `<html>` element, so the page shows them as soon as you pick them. The others change the HTML the build writes, and the lab marks them **Needs a rebuild: shows after Save**:

- `layout.navbar`: `tabs`, which adds a row of section links
- `layout.footer`: `columns` and `none`
- `layout.toc`: `inline` and `minimal`
- `layout.home`: every variant but `default`
- `layout.hero`: `split` and `minimal`
- `sidebarToggle` and the banner

Leaving one of these variants needs a rebuild too, since the page was built with its markup. On a site with [`layoutSelector`](/configuration/site-config#options.layoutselector) on, pages carry the markup of every docs-page variant, so there only `home`, `hero`, `sidebarToggle` and the banner need a rebuild. A preset shows through the options it sets: the ones that switch an attribute change at once and the rest are marked, each with "From the ... preset" under it. Picking a dark-first style (`moka` or `terminal`) also switches the page to dark mode, unless you picked a mode with the dark mode button, as on a first visit.

Unsaved choices survive the reload that follows a docs edit, in the same browser tab. **Reset** goes back to the values in the file.

### Saving

**Save** writes the options you changed to the configuration file `serve` reads, the one `--config` names if you passed it. `serve` sees the change, rebuilds the site and reloads the page, and the panel opens again with a note of what it saved. The terminal prints a line such as `Theme lab: saved style, layout.navbar to mokadocs.yaml`.

The lab edits only the keys you changed:

- Comments, blank lines, key order, quoting and line endings (LF or CRLF) stay as they were, and so does a UTF-8 byte order mark.
- A value already in the file is replaced where it stands, in the quotes it had.
- A missing key goes at the end of its parent mapping with the same indentation. Missing `theme:`, `options:`, `layout:` and `banner:` blocks are added, indented by the file's own step.
- Picking the value an option has without its key (the preset's value, else the default) removes the key, and a `layout:` or `banner:` block left empty goes too, unless it holds comments. With a preset, a value that differs from the preset's is written even when it is the default, so it still wins over the preset.
- Text that would read differently without quotes, such as `Version 2: out now`, gets double quotes.

Before writing, the lab loads the edited text the way a build does and checks that every option has the value you picked; if not, nothing is written and the panel says why. The text goes to a hidden temporary file next to the original, which is then moved over it, so a build never reads half a file. If the file changed on disk since the page loaded it, Save asks you to reload the page instead of writing over your edit.

Save is off, with the reason in the panel, when the file:

- doesn't load, or isn't UTF-8
- holds more than one YAML document (`---`)
- writes `theme`, `theme.options`, `layout` or `banner` as a flow mapping (`{ }`), or the whole file as one
- uses an anchor, an alias or a merge key (`&`, `*`, `<<`) on the way to an option, or a merge key at the top level
- has a tab character in the `theme:` section
- writes one of the options as a block scalar (`|` or `>`) or a list, or has the same key twice

Anchors and flow collections elsewhere, such as in `nav` or plugin options, don't get in the way. **Show YAML** always shows the lab's options as they'd look under `theme.options`, and **Copy** puts them on the clipboard, to paste by hand when Save is off.

### What Can Hide a Saved Value

- **Environment variables**: a `MOKADOCS_*` variable wins over the file, such as `MOKADOCS_STYLE` or `MOKADOCS_LAYOUT_NAVBAR`. The lab names the variable and its value under each option it overrides; the saved value shows once you unset it.
- **Your own Appearance choices**: a style, color preset, code theme or code style picked in the site's [Appearance panel](/themes/customization#appearance-panel) shows instead of the site's value in that browser. The lab says when that happens, and saving an option clears your pick for it.
- **A custom theme**: these are the default theme's options. With `theme.name` set to a theme folder, the lab says so, since your theme may not read them.

### Theme Lab Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /api/theme-lab/lab.js` | The panel's script. Every page loads it with `<script src="/api/theme-lab/lab.js" defer></script>`, added after the hot reload script (not in Blazor preview frames). The panel is built in a shadow root, so the site's CSS doesn't reach it and its CSS doesn't reach the site |
| `GET /api/theme-lab/catalog` | The options and their choices, which choices need a rebuild, the value each has in the file, the environment variables that override them, and a `version` hash of the file |
| `POST /api/theme-lab/save` | Saves changed options, as below |

```json
{
  "version": "1164192562385db7",
  "values": { "style": "paper", "layout.navbar": "tabs", "layout.toc": null, "sidebarToggle": true }
}
```

`null` removes a key, and `version` is optional. The answer is `{"saved": true, "changed": ["style", ...], "version": "..."}`, or `"saved": false` when the file already has the values. An unknown option or a value outside its choices gets `400`, a file that changed since `version` gets `409`, and a file the lab can't edit or that doesn't load gets `422`; each with `{"error": "..."}`. The endpoint follows the [request rules](#request-rules) below and reads at most 16 KB.

## Static File Serving

The server runs one build at startup and then serves files from the output folder. File responses carry `Cache-Control: no-cache, no-store, must-revalidate`, so the browser always fetches the latest build. Files answer `GET` and `HEAD`; other methods get `405`. Files other than pages also answer range requests, so a video can seek.

### Clean URLs

A request for a folder is served from the folder's `index.html` without a redirect, and every page is written as `<route>/index.html`. A path without an extension that matches no file or folder is tried again with `.html` added.

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
| `.md` | `text/markdown; charset=utf-8` |
| `.yaml`, `.yml` | `application/yaml; charset=utf-8` |
| `.svg` | `image/svg+xml` |
| `.map` | `application/json` |
| `.wasm` | `application/wasm` |
| `.zip` | `application/zip` |
| `.avif` | `image/avif` |
| `.dll`, `.pdb`, `.dat`, `.blat`, `.webcil`, `.br` | `application/octet-stream`, for the Blazor preview runtime |
| Anything else | The type ASP.NET Core's static file middleware uses, such as `image/png`, `application/pdf`, `video/mp4`, `video/webm` and `application/manifest+json` for `.webmanifest` |

A file whose extension has no content type isn't served and gets the 404 page. Every file type the build copies has one, except the host files `CNAME`, `_redirects` and `_headers`, which only a static host reads.

## 404 Page

When no file matches, the server returns the build's `404.html` with status 404, or a plain-text `404 Not Found` when that file doesn't exist.

The build renders `404.html` with the theme's default layout, so it has the site's header, sidebar, style, colors, dark mode, favicon and layout variants. It says the page wasn't found and links to the home page, and to search when search is on. It tells search engines not to index it and has no canonical link. Its links include the base path, so it works at any missing address.

## API Endpoints

The server accepts `POST` requests on the REPL and feedback endpoints below, and on the [theme lab's](#theme-lab-endpoints) save. The same paths also work under the base path, so a site built with `--base-path /my-repo` can post to `/my-repo/api/feedback`.

### Request Rules

Every request to `/api/*` other than `GET` and `HEAD` needs `Content-Type: application/json`. A browser request must also come from the origin of the address it was sent to: `http://localhost:<port>`, `http://127.0.0.1:<port>` or `http://[::1]:<port>`, matching the request's `Host` header. A request whose `Origin` header names another origin (a page on `localhost` posting to `127.0.0.1` included), or whose `Sec-Fetch-Site` header is anything other than `same-origin` or `none`, gets `403`. Requests without those headers, such as from `curl`, are accepted.

The server sends no CORS headers, and a preflight `OPTIONS` request gets a `403`, so pages on other origins can't call these endpoints.

A body that isn't JSON gets `400`. The REPL endpoint reads at most 64 KB of body and the feedback endpoint 4 KB, the same limits as the ASP.NET Core host; a longer body gets `413` without being read.

### REPL: `POST /api/repl/execute`

Available when the `mokadocs-repl` plugin is declared in `mokadocs.yaml`. Without it, the endpoint returns `503` with `{"error": "REPL service is not available."}`.

The endpoint runs the posted C# script in a worker process that `serve` starts, with the permissions of the user running `serve`. Scripts are limited to 10,000 characters and 5 seconds; a script that runs longer has its worker killed and replaced, so it can't hang the server. A run also stops that way when the request is cancelled, for example when the reader leaves the page, and when `serve` stops.

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

The server starts listening after the initial build, and only on the loopback addresses. If it can't, for example because another program uses the port on `127.0.0.1` or `[::1]`, `serve` prints `Error: Can't listen on port 5080` with the reason from the operating system and exits with code 1. Then either:

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
- The socket refuses pages from any origin but the server's own with `403`, and the server refuses a `Host` other than `localhost`, `127.0.0.1` or `[::1]` with `400`, so hot reload doesn't work through a tunnel or a proxy that changes either. Open the site on one of those three addresses.
- If hot reload stops working, check for browser extensions that block WebSocket connections.
