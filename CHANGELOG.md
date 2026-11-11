# Changelog

All notable changes to MokaDocs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.9.0] - 2026-11-11

### ✨ New

- **Page layout variants.** `theme.options.layout` picks a layout for six parts of the
  default theme's pages, each on its own and with every style, preset and mode:
  - `width`: `default`, `wide`, `full` or `boxed`
  - `navbar`: `default`, `compact`, `floating`, `tabs` (a row of top-level sections) or
    `autohide`
  - `footer`: `default`, `minimal`, `columns` (links built from the sidebar) or `none`
  - `toc` ("On this page"): `default`, `minimal`, `inline` (a box under the title) or
    `floating` (a button that opens it over the page)
  - `breadcrumbs`: `default`, `chevrons`, `pills`, `path` or `back`
  - `pageNav`: `default`, `minimal` or `stacked`

  Landing pages use the navbar and footer settings. Each part has a `MOKADOCS_LAYOUT_*`
  environment variable, and the ASP.NET Core host has `MokaDocsOptions.Layout`. An
  unknown name warns and renders as `default`. A site that sets nothing looks the same as
  before.

## [1.8.0] - 2026-11-11

### 🔒 Security

- **Restore failed on two new .NET advisories.** NuGet.Packaging 7.9.0 brings
  `System.Security.Cryptography.Pkcs` 8.0.1 onto the net9.0 leg, which carries
  GHSA-2fmv-rv2c-59x7 (moderate) and GHSA-846f-f764-76mx (high), both published on
  2026-10-13. It is pinned to the patched 10.0.15. The 1.7.0 packages ship 8.0.1 on
  net9.0.

- **XML doc comments could inject script into API pages.** A `paramref` or
  `typeparamref` name went into the page unescaped, and `<see href="javascript:...">`
  became a live link (the `seealso` check could be skipped with a tab inside the
  scheme). Names are escaped, and doc links only go to http, https, mailto or relative
  URLs, checked after control characters are removed. This matters most in the ASP.NET
  Core host, which reads third-party XML files.
- **A page could be written outside the output folder.** A front matter `route:` with
  `..`, or an OpenAPI tag holding a Windows path under `routePrefix: "/"`, wrote
  `index.html` outside `_site`. Bad routes are now errors, reported before the old site
  is deleted, and every write is checked to stay under the output folder.
- **`DocsPath = "."` published the app's settings.** The ASP.NET Core host copied every
  file under DocsPath, so `/docs/appsettings.json`, deps and runtimeconfig files, dlls
  and pdbs were served. It now copies only Markdown and the asset types a site publishes,
  never app files or bin, obj and node_modules, and warns about what it left out.
- **Values in page attributes were written unescaped.** A route, label or OpenAPI tag
  containing a quote could break out of `href` and other attributes. Every value the
  default layouts write into an attribute is escaped now.

### 🔄 Changed

- **NuGet dependencies updated to the latest of the .NET 10 line.** Microsoft.Extensions.*
  and Microsoft.AspNetCore.OpenApi 10.0.12 to 10.0.15 (9.0.23 on the net9.0 legs),
  Markdig 1.3.2 to 1.6.0, Scriban 7.4.0 to 7.6.1, System.IO.Abstractions 22.2.0 to
  23.0.0, System.CommandLine 2.0.12 to 3.0.0, Microsoft.FeatureManagement 4.7.0 to
  4.8.0 and Moka.Blazor.Repl.* 1.3.5 to 1.3.6, and on the test side xunit.v3 4.0.3,
  Microsoft.Testing.Platform 2.5.3, Microsoft.NET.Test.Sdk 18.12.0, FluentAssertions 8.11.0
  and Swashbuckle 10.3.1. The .NET 11 releases of the framework packages (Microsoft.Extensions
  11, Microsoft.AspNetCore.OpenApi 11) are not used. The repository builds from
  `MokaDocs.slnx`; `MokaDocs.sln` is gone.
  Verify.XunitV3 stays on 32.x: 33 adds a license check that fails the build.
- **New Blazor preview hosts reference Moka.Blazor.Repl.Host 1.3.6** and
  Microsoft.AspNetCore.Components.WebAssembly 10.0.15. An existing scaffolded host
  keeps its versions; delete `.mokadocs/preview-host` to regenerate it.
- **The `NuGet.Packaging`/`NuGet.Protocol` pins are gone.** Moka.Blazor.Repl.Compiler
  1.3.6 depends on NuGet 7.9.0 itself, so the pins that cleared GHSA-g4vj-cjjj-v7hg
  no longer change anything.

- **Faster builds on big sites.** Rendering no longer slows down quadratically (5000
  pages render in about 2 s instead of 7 s), Markdown parsing and page writing run in
  parallel (a 4000-page build went from 11 to 13 s down to 5 to 7 s), and C# analysis
  runs in parallel without reformatting source (a 2000-type project analyzes in about
  1 s instead of 2.5 s). `-v` shows how long each build phase took.
- CI's GitHub actions run on Node 24 (checkout 7, setup-dotnet 6, upload-artifact 7,
  upload-pages-artifact 5, deploy-pages 5, action-gh-release 3). The unused test-only
  Verify.XunitV3 reference is gone.

### ⚠️ Behavior changes

- **`MapMokaDocs()` returns a `RouteGroupBuilder`**, so `.RequireAuthorization()` and
  other conventions chain onto it and cover every docs endpoint. It still implements
  `IEndpointRouteBuilder`, so source compiles as before; code compiled against 1.7.0
  needs a rebuild. Pages and assets answer GET and HEAD only, other methods get 405.
- **The ASP.NET Core site is built at startup.** The build runs in the background once
  the app has started (`BuildOnStartup`), so the first reader no longer waits for it.
  Startup work only happens when `MapMokaDocs` was called.
- **Docs responses carry a strong ETag** and answer a matching `If-None-Match` with 304.
  Everything except static assets gets `Cache-Control: no-cache`.
- **OpenAPI pages in the ASP.NET Core host default to `/rest-api`.** An `openapi` entry
  without a `routePrefix` used `/api`, the API reference's own route. The CLI default
  is unchanged.

- **Nested types and types that differ only in type parameters get their own API
  pages.** `Outer.Options` is at `/api/ns/outer.options`, and `Result<T>` next to
  `Result` is at `/api/ns/result-1`; both used to share one route and one page was lost.
  Titles, the `/api` index, the sidebar and cref links show names as C# writes them
  (`Outer.Options`, `Result<T>`), so generic types now show their type parameters.
- **More problems fail the build.** A missing docs folder, a page that can't be read or
  parsed, an invalid front matter `route:`, and a configured project that is missing or
  fails to analyze are errors, so `mokadocs build` exits 1 instead of shipping a site
  without them. `build.strict: true` or `--warnings-as-errors` also fails on warnings.
- **Unknown keys warn.** A key in `mokadocs.yaml` or in front matter that MokaDocs
  doesn't read is a warning with its line, and suggests the right spelling when only the
  letter case differs. YAML errors name the line, column and cause.
- **The output folder guard covers more.** Build and `mokadocs clean` refuse an output
  folder that holds a configured project, a custom theme or the logo or favicon source,
  and refuse to empty a folder MokaDocs didn't create. Each build writes a
  `.mokadocs-output` marker; sites built by older versions are recognized by their
  theme files.
- **Sitemap, canonical and `og:url` addresses** are percent-encoded and end with `/`,
  matching how pages are written; the sitemap is always valid XML.
- **View Source shows the code as written** instead of a reformatted copy, still without
  the members the page leaves out. `[EditorBrowsable(Never)]` members are left out by the
  CLI too, as the host already did.
- **Previous and Next links follow the sidebar order.**
- **API search has one entry per member** with its summary, instead of empty Methods and
  Properties entries per type.
- **OpenAPI tags with characters a route can't hold** (quotes, colons, `&`, `#`, paths)
  get a safe slug, and tags that slug alike get `-2`, `-3` in document order. Tags whose
  routes were already safe keep them.
- **Python API:** a class whose route matches its module's page moves to `-2`, and a
  root `__init__.py` is named after its folder.
- **Moka.Docs.Serve:** the unused `/api/blazor/preview` dev server endpoint is removed,
  with `BlazorPreviewService` and `BlazorPreviewResult`; `DevServer`'s constructor lost
  that parameter. `ReplExecutionService.ExecuteAsync` can throw `ReplQueueFullException`
  when four runs are already waiting (the endpoints answer 429).

- **The generated sidebar is a tree.** Without `nav:`, every folder is a section titled by
  its index page, and API types are grouped by namespace, collapsed except the one holding the
  current page. Pages more than three levels down are listed under their third-level folder
  instead of being dropped. `nav[].autoGenerate: true` now lists the tree below the item's
  path; it used to be ignored. The ASP.NET Core host's default Guide entry is a tree too.
- **Sites with more than 300 sidebar entries share one sidebar file.** With the default
  `theme.options.sidebar: auto`, the full sidebar is written once to `_theme/js/sidebar.js`
  and each page keeps its top-level entries and its own path, so readers without JavaScript
  get that shorter sidebar. `sidebar: inline` keeps the old behavior.
- **`draft: true` in front matter drafts the page**, like `visibility: draft`. It used to be
  ignored and the page was published.
- **The navbar version label comes from the package version.** With versioning on, a
  version without a label shows `v{major}.{minor}` of the documented package; versioning on
  with no `versions` list now shows that one version. Pages with a `version:` range are left
  out of versions the range doesn't cover.
- **Default style colors meet WCAG AA.** Muted text and text in the accent color (links,
  inline code, the current sidebar and contents entries) are darker in light mode and
  lighter in dark mode where a preset fell short: ocean links were 2.8:1 on white, amber
  2.2:1. Each preset keeps its hue. A custom `primaryColor` is used as is.
- **Search entries carry their section text** and code words, so the index is larger (this
  repository's grew from 296 KB to 478 KB, 50 KB to 110 KB gzipped).
- **Plugin registration:** `AddMokaDocsPlugins()` now registers every built-in plugin, and the
  ASP.NET Core host registers `openapi` only when it's declared.
- **Moka.Docs.Search is no longer published.** It was an empty package (`AddMokaDocsSearch()`
  registered nothing) and 1.8.0 is its last release; search lives in Moka.Docs.Engine.
  Moka.Docs.Serve no longer depends on Moka.Blazor.Repl.Compiler.
- **Obsolete, removed in 2.0:** `DocSite`, `ThemeManifest`, `SearchFeatureConfig.Provider` and
  `MokaDefaults.SearchProvider`, none of which MokaDocs used. `features.search.provider` is no
  longer read, and a config that sets it gets a warning.

### ✨ New

- **REST API pages from the app's own OpenAPI document.** `options.OpenApiDocument = "v1"`
  reads the document through the `IDocumentProvider` contract that `dotnet-getdocument`
  uses, so it works with `AddOpenApi()` (Microsoft.AspNetCore.OpenApi 9 and 10) and
  `AddSwaggerGen()` (Swashbuckle 9 and 10) without a package dependency.
  `OpenApiRoutePrefix` and `OpenApiLabel` set where and under what name the pages
  appear, and the default sidebar gets a REST API entry. A document that can't be read
  is a build warning.
- **The REPL runs in the ASP.NET Core host.** Add
  `if (await MokaDocsReplWorker.TryRunAsync()) return;` as the first line of
  Program.cs and `EnableRepl` snippets run in a copy of the app's own executable, with
  the documented assemblies referenced. Without the line nothing is spawned and Run
  answers with the fix. The endpoint is mapped in Development only, runs code only for a
  caller on the same machine whose Host is localhost or a loopback address and whose
  request didn't come through a proxy (which stops DNS rebinding and local tunnels), and
  accepts JSON from the app's own origin only. `AllowRemoteRepl` lifts the first two;
  put the docs behind authorization first. At most four runs wait behind the one running.
  `ReplWorkerStartInfo` overrides the worker command.
- **Feedback in the ASP.NET Core host.** `POST {BasePath}/api/feedback` logs each vote
  and passes it to the new `OnFeedback` callback. `ShowFeedback = false` hides the
  widget and maps nothing.
- **`WatchDocs` rebuilds the site when a file under `DocsPath` changes**, in
  Development by default. Requests get the previous site until the new one is ready.
  `MokaDocsService.RebuildAsync()` does the same on demand.
- **View source links in the ASP.NET Core host.** Type pages link to the declaration in
  its repository, read from the SourceLink data in the assembly's portable PDB (next to
  the DLL or embedded). GitHub and GitLab links open the file at the type's first line.
  `SourceLinks = false` turns them off. `IncludeSourceCode` (off by default) adds the
  CLI's View Source panel from the PDB's embedded source or the files it names.
- **Namespace filters.** `IncludeNamespaces` and `ExcludeNamespaces` pick the
  namespaces the host documents, a segment at a time; exclude wins. Types and members
  marked `[EditorBrowsable(EditorBrowsableState.Never)]` are left out.
- **XML docs kept elsewhere.** `XmlDocFiles` adds XML documentation files, matched to
  assemblies by the name inside them. An assembly without one now gets a build warning
  naming the path that was checked.
- **Inherited Members and Extension Methods on API type pages**, in the CLI and the
  host. A type lists the members it inherits from documented base types and
  interfaces, grouped by declaring type and linked to their entries, leaving out
  constructors, operators and overridden or hidden members. It also lists the
  documented extension methods whose receiver is the type, a base type or an
  implemented interface, including constrained generic receivers.
- **OpenAPI plugin:** reads OpenAPI 3.1 (type lists, nullable references through
  `oneOf`/`anyOf`, `$ref` with sibling keywords, `const`, schema `examples`). Types
  show nullability (`string | null`), maps, unions and enum values, and `allOf` parts
  are merged. Schema names link to the C# type page when exactly one documented type
  has that name. A new `document` option takes the document's text instead of a file.
- **OpenAPI pages, more of the document.** Descriptions render as Markdown (CommonMark,
  as the spec defines them; raw HTML stays text and only http, https and mailto links
  are kept). Every endpoint has an anchor from its operationId, or its method and path,
  that the overview links to, and each endpoint gets its own table of contents and
  search entry. The overview lists the servers and an Authentication section for each
  security scheme (API key, HTTP, OAuth 2.0 flows with scopes, OpenID Connect), and each
  endpoint shows the schemes it needs. A new Models page lists the document's schemas,
  and schema names without a C# type page link there. The document itself is published
  at `{routePrefix}/openapi.json` (or `.yaml`) and linked from the overview, in the CLI
  and the ASP.NET Core host.
- A root base path (`MapMokaDocs("/")` or `BasePath = "/"`) serves the docs at the app
  root.

- **C# 14 extension blocks.** A static class's page has a section for each
  `extension(...)` block with its declaration, receiver and type parameter docs, then its
  members. Extension properties, static extension members and extension operators are
  listed on the pages of the types they apply to, next to extension methods (the section
  is headed Extension Members when it has any). The ASP.NET Core host reads them from
  compiled assemblies, and crefs and `<inheritdoc cref>` resolve to them.
- **Appearance styles.** The default theme has five looks: `default`, `moka`, `minimal`,
  `classic` and `soft`, each in light and dark with every accent preset and WCAG AA
  contrast. Pick one with `theme.options.style` (or `MOKADOCS_STYLE`,
  `MokaDocsOptions.Style`); readers can switch from a Style button in the header, which
  `theme.options.styleSelector: false` hides. An unknown name renders as default with a
  warning. `moka` starts in dark mode for readers who haven't chosen.
- **`build.strict` and `--warnings-as-errors`** on `build` and `validate` fail on
  warnings, for CI.
- **`doctor` checks `#anchor` targets** against the target page's ids, and compares link
  paths with their letter case (a case-only match 404s on Linux hosts).
- **`csharp repl`, `cs repl` and `c# repl` fences** become REPL blocks, as the REPL page
  documented.
- **OpenAPI:** OpenAPI 3.1 `webhooks` are listed on the overview, parameters described
  with `content` show their schema, and references that resolve to nothing are listed in
  a warning.
- **Blazor previews:** `renderTimeout` (5 s) stops a component that never settles from
  hanging the build, and `publishTimeout` (600 s) bounds the preview host publish, which
  now honours cancellation and kills its process tree.

- **Sidebar modes.** `theme.options.sidebar` is `auto` (default), `inline` or `shared`, with
  `theme.options.sidebarInlineLimit` (300 entries), `MOKADOCS_SIDEBAR` and
  `MokaDocsOptions.Sidebar`. A shared sidebar marks the current page and expands its sections
  before the first paint. A 5012-page site went from 65 s and 8.4 GB of output to 10 s and
  258 MB.
- **Blog.** Posts in `features.blog.path` get a paginated index, tag pages, a header with
  date, authors, tags and reading time, links to the next newer and older post, and RSS and
  Atom feeds linked from every page's head (feeds need `site.url`). New settings `path`,
  `route`, `title`, `description`, `sidebarPosts`, `feed` and `authors`; `postsPerPage` and
  `showAuthors` take effect. Post front matter `date`, `author`/`authors`, `summary` and
  `image`. The blog takes one sidebar entry listing its latest posts. `MokaDocsOptions.Blog`
  builds it in the ASP.NET Core host.
- **Multi-version builds.** With versions that name a git `branch` or the new `tag`,
  `mokadocs build` builds each into its own folder and the default version at the site root
  too, from temporary git worktrees it removes afterwards; your working tree and index are
  not touched. The version dropdown links to the same page in the other version when it has
  one, and appears on landing pages. `mokadocs build --version <name>` rebuilds one version.
  `validate` and `doctor` report refs that don't exist. Front matter `version:` takes npm or
  NuGet ranges, or a list of them.
- **Plugins from a path.** `plugins[].path` loads your own plugin in the CLI: a .dll, a
  project that mokadocs builds with `dotnet build -c Release`, or a folder holding either.
  A path that doesn't load is a build error naming the plugin, the path and the reason.
  `mokadocs new plugin` makes a project the CLI loads as it is.
- **`build --watch` and `serve` reload mokadocs.yaml**, with new plugins, theme and
  templates. A broken file keeps the last good site and prints the error. `build.output`,
  `content.docs` and, under serve, `build.basePath` still need a restart, and the terminal
  says so.
- **`build.minify`** collapses whitespace and drops comments in pages (pre, code, textarea,
  script and style are kept as written) and minifies the theme's CSS and JS. Off by default;
  `MOKADOCS_MINIFY_OUTPUT` and `MokaDocsOptions.Minify`.
- **`theme.options.showContributors`** lists the people who committed to each page under its
  "Last updated" line, from the same git log as the dates, with `.mailmap` applied. Emails
  aren't shown and nothing is downloaded.
- **OpenAPI:** endpoints show their path or operation servers when they differ from the
  document's, and `externalDocs` on documents, tags, operations and schemas show as links.

### 🐛 Fixed

- **MokaDocs broke a .NET 9 app's own OpenAPI document.** Moka.Docs.Plugins referenced
  Microsoft.OpenApi 2.x, which every app using Moka.Docs.AspNetCore then got.
  Microsoft.AspNetCore.OpenApi 9 and Swashbuckle 9 need 1.x and threw
  `TypeLoadException`, so `/openapi/v1.json` failed in a default .NET 9 web API. The
  OpenAPI parser now uses System.Text.Json and YamlDotNet, and MokaDocs no longer
  references Microsoft.OpenApi.
- The feedback widget in the ASP.NET Core host posted to an endpoint that didn't exist
  and got a 404 for every vote.
- REPL Run buttons posted to `/api/repl/execute` without the site's base path, and in
  the ASP.NET Core host to an endpoint that didn't exist. They now post under the base
  path, show the server's error when it refuses a run, and no longer tell readers to
  run `mokadocs serve`.
- A cancelled REPL run left the snippet running, and its answer reached the next run,
  or that run failed on the read still pending. The worker is now replaced on cancel,
  under `mokadocs serve` and in the host, so a reader who leaves stops the snippet.
- Several `openapi` plugin entries rendered with the last entry's options. An OpenAPI
  3.0 schema `example` was ignored, a 3.0 `nullable: true` beside a `$ref` was dropped,
  and a `routePrefix` without a leading slash produced broken routes.
- A root base path mapped `//{**path}`, which matched nothing.
- Sidebar labels, breadcrumbs, previous and next page titles, the site title and the
  table of contents were written into pages unescaped, so a title such as `Box<T>`
  became an element. They are HTML-escaped now.
- The AspNetCore sample built for net9.0 as well: `Directory.Build.props` sets
  `TargetFrameworks`, which the sample's singular `TargetFramework` didn't override.

- A sidebar with about 1000 entries or more made every page render without its layout,
  and pages over 1 MB were cut off with "..." (Scriban's default limits).
- `build --watch` rebuilt forever when the output folder was inside the docs folder (this
  repository's own layout), and a change during a running build in `serve` or
  `build --watch` started a second build that deleted the first one's output. Rebuilds
  now run one at a time with one queued, and watching falls back to polling when the
  system refuses a watcher (`DOTNET_USE_POLLING_FILE_WATCHER` is honoured).
- `mokadocs init` overwrote an existing `docs/index.md`; it now skips existing files and
  names them.
- `mokadocs clean` could delete a project's sources when `build.output` pointed at them.
- C# analysis followed the default glob only: `<Compile Remove>` files got pages, linked
  `<Compile Include>` files were missing, and dot folders such as `.mokadocs` were
  analyzed. `<include>` doc comments are expanded, and a doc comment that isn't
  well-formed XML is a warning naming the member.
- API pages: record struct and readonly record struct declarations read like the source,
  delegates show their signature, default values print as written, attribute named
  arguments show, code blocks lose the comment indentation, lists keep their terms and
  table lists render as tables, inherited parameter docs survive a renamed parameter, the
  type graph handles generic types, and the NuGet widget fills in `$(MSBuildProjectName)`.
- Admonition, card and code-group titles and link card URLs were written raw. Table of
  contents and search text lost link text, emphasis and entities. A body starting with a
  `---` rule lost content, and front matter delimiters inside block scalars closed the
  front matter. Tab and code-group ids changed on every watch rebuild.
- `.md` files were found case-sensitively on Linux, a folder link pointing at an ancestor
  looped, `\` in config paths broke on Linux, `.md` was removed anywhere in a route, and
  with `clean: false` a changed logo or favicon outside the docs folder was never copied.
- Changelog plugin: long numeric version parts threw, partial dates like `2025-06` showed
  a day, ambiguous dates were read US-style, and the first release in a hidden tab
  rendered 1px tall.
- Theme: code block line numbers failed contrast (about 2:1) in every style, the landing
  hero stayed hidden for 0.4 s with animations off, the version menu stayed open when
  another picker opened, search snippets could break entities, and root-relative links
  in single-quoted, unquoted and `srcset` attributes missed the base path. Theme CSS and
  JS URLs carry a content hash, so an upgrade reaches readers at once.
- Dev server: HEAD answered 500 on Windows, POST bodies had no size limit, PDF, video,
  AVIF, webmanifest, zip and wasm files had generic content types, and it now also
  answers on 127.0.0.1 (and [::1] on Windows). REPL return values and errors are capped at
  100,000 characters like output, an oversized line from the worker stops it, and a
  worker that exited between runs is replaced.
- ASP.NET Core host: a failed build no longer reruns on every request (requests get the
  last site that built, or 503 with Retry-After), a locked file in DocsPath is skipped
  with a warning, a reader who leaves no longer cancels the build others wait for, and
  every response uses `no-cache` with its ETag.
- ASP.NET Core host reflection: a type or member needing an assembly the app doesn't
  deploy is left out with a warning instead of failing the whole API reference. Ref
  structs no longer show the compiler's Obsolete marker, a real Obsolete on a constructor
  shows, attribute arguments read like source, function pointers, ref fields, const
  decimals, fixed buffers and record structs render correctly, an assembly listed twice
  is documented once, and one bad SourceLink entry no longer drops the others.
- OpenAPI: deeply nested, self-expanding or heavily cross-referenced documents parsed for
  minutes or crashed the process; they now parse quickly with bounded memory. YAML over
  about 1 MB parses again, documents with a BOM, a leading comment or YAML flow style are
  read, a key written twice is a clear error, a tag listed twice no longer duplicates the
  endpoint, tag pages sort the same on every machine, and a description Markdig refuses
  shows as text instead of failing every page.
- Blazor previews: the preview host was republished in every new process and not when a
  referenced project changed, and shared framework detection missed non-default install
  locations. A `library` or REPL package value that isn't a NuGet id is reported instead
  of being written into a project file.
- Python API: `source: ./src/` failed on Windows, and the analyzer had no timeout.
- The single-file app case of the host REPL worker failed to start.
- A REPL snippet that called `Environment.Exit` under `serve` on .NET 9 was reported with exit
  code 143 instead of its own code, after System.CommandLine 3's process termination
  handling; the worker runs without it.

- Open sidebar sections were cut off at 2000px, and opening the sidebar on a phone didn't
  scroll to the current page.
- The dev server kept closed hot reload connections for its whole life, and on Linux `serve`
  could end with "Address already in use" when it shut down.
- REPL workers inherited `DOTNET_STARTUP_HOOKS` and the `dotnet watch` hot reload variables.
- The color picker didn't close the code theme and code style menus, and the code theme
  picker didn't close the code style menu.
- Relative URLs in raw HTML in Markdown (`<img src="./x.png">`, srcset, href, poster) weren't
  resolved against the file like Markdown links.
- ASP.NET Core host reflection: base types and interfaces were unqualified, nested types of
  generic types didn't resolve for inherited members and inheritdoc, and in an interface
  diamond one same-named member hid the other. View Source showed enum members marked
  `[EditorBrowsable(Never)]`, in the CLI and the host.

### 🧪 Tests

- 3682 tests on Windows (1841 per framework), up from 1246; Linux runs 3674. New coverage
  includes the ASP.NET Core host's endpoints, REPL and rebuilds on real Kestrel, OpenAPI with
  documents generated by Microsoft.AspNetCore.OpenApi and Swashbuckle on both frameworks and
  hostile documents, PDB and SourceLink reading, C# 14 extension blocks in both API paths,
  the appearance styles and contrast, the sidebar script under Node, multi-version builds in
  real git repositories, plugin loading from dlls and real project builds, and config reload
  in `build --watch` and `serve`. Every fix was confirmed by reverting it and watching its
  tests fail. The suite also runs on Linux before release, as CI does.

### 📚 Docs

- ASP.NET Core guide: REST API (OpenAPI), Filtering, View Source, Feedback,
  Interactive REPL and Remote REPL sections, a Development Mode section on watching,
  and the new options in the reference table. API documentation guide: Inherited
  Members and Extension Methods. OpenAPI and REPL plugin pages updated. The sample app
  shows the REPL, the REST API pages and the watcher.
- New or rewritten: the blog guide, the versioning page, large sidebars on the navigation
  page, plugin authoring and loading from a path, configuration changes under the dev
  server, theme styles, and the strict and unknown-key behavior in the configuration
  reference. The Library sample shows the blog and builds without warnings.

## [1.7.0] - 2026-09-12

### 🔒 Security

- **`mokadocs serve` ran code sent by any website open in the same browser.** The
  REPL and Blazor preview endpoints accepted a plain-text POST, which browsers
  send cross-origin without a CORS preflight, and answered every origin with
  `Access-Control-Allow-Origin: *`. A page could run C# on the developer's
  machine while the dev server was up. The `/api/` endpoints now require JSON and,
  from a browser, the dev server's own origin (other origins get 403), send no
  CORS headers, and only exist when their plugin is declared. Static file
  requests can no longer read outside the site: a drive-letter path such as
  `/C:/Windows/win.ini` escaped the output folder.

### ⚠️ Behavior changes

- **`mokadocs build` exits 1 when the build reports errors.** It exited 0 unless
  the pipeline threw, so CI published broken sites. Errors are always printed.
- **Plugin warnings and errors reach the build.** `LogWarning` and `LogError`
  calls made while a plugin runs are recorded as diagnostics with the plugin id
  as the source. They used to reach only a logger that `build` and `serve` leave
  silent, so a missing OpenAPI spec or a failed preview-host publish ended in a
  clean summary. Combined with the exit code change, those failures now fail the
  build.
- **Internal members are no longer part of the public API reference.** Only
  private members were filtered, so internal and `private protected` members of
  public types were listed, along with public types nested inside internal ones.
  `includeInternals: true` still includes them.
- **Nav items with the same `order` keep their mokadocs.yaml order.** Ties were
  sorted by label, so a nav config without `order` values came out alphabetical.
  This site's own sidebar started with Advanced instead of Getting Started.
- **An output directory that contains the project or the docs folder stops the
  build.** The output phase deletes the output directory first, so a typo such as
  `output: ./docs` deleted the Markdown sources. `validate` reports it and
  `clean` refuses it.
- **A layout that fails to render is a build error.** The template engine caught
  the error and wrote each page's bare content with an HTML comment, so a custom
  layout using `{{ include }}` produced a site without its layout and a build that
  passed. The error is reported once per distinct message, with a page count.
- **The ASP.NET Core host initializes plugins once.** It initialized them again
  before every in-memory build, so with `CacheOutput = false` each rebuild ran
  them one more time and injected the REPL assets again.
- **The landing layout no longer shows MokaDocs' own feature cards.** Every site
  using `layout: landing` showed "C# API Reference", "Instant Search" and a sample
  `mokadocs.yaml`. Cards now come from the page's `features` front matter (see New),
  and a page without it has no card section.
- **"Last updated" dates come from git.** Each page shows the date of the last
  commit that touched it, and so does its sitemap `<lastmod>`. They used to be file
  times, so every page of a site built in CI showed the build date. Files with
  uncommitted changes, and builds outside a git work tree, still use the file time.
  A shallow clone gives every page the latest commit's date: check out with
  `fetch-depth: 0`.

### ✨ New

- **`mokadocs validate` runs a dry-run build.** It used to print "not yet
  implemented". It now runs the whole pipeline (Markdown, Roslyn analysis,
  plugins, theme rendering) without writing to the output directory, and lists
  every warning and error the build reports, prefixed with the phase that raised
  it. Exits 0, 1 or 2 like `doctor`. Supports `--config`/`-c`, `--draft` and
  `--verbose`/`-v`. Useful in CI, where it replaces parsing build output.
- **Containers nest.** A closing fence now closes a container only when it is at
  least as long as the one that opened it, so `::::steps` can hold `:::card` and
  `:::code-group`, and `==== "Tab"` groups can hold `=== "Tab"` groups. Every
  container used to close on the first bare `:::`, spilling the rest of the outer
  block onto the page. The tab script only switches a group's own tabs.
- **Relative links work on every host.** Links such as `./intro` or
  `../guide/setup.md#install` are resolved against the file that contains them at
  build time. They were emitted as written: `.md` links always 404ed, and on
  GitHub Pages, which adds a trailing slash, relative links pointed one folder too
  deep. `doctor` now checks relative links too.
- **A warning when pages share a route.** Two `route:` overrides, routes that
  differ only by case, or a plugin page on `/api` next to the C# API index were
  written to the same file, and the first silently disappeared.
- **Warnings for other silent failures:** front matter that fails to parse (the
  page became "Untitled" and showed its YAML as content), a `layout` the theme does
  not have, a sidebar icon that does not exist, and a custom theme folder that is
  missing or has no layouts. `route:` overrides and nav `path` values now get a
  leading slash when missing and lose a trailing one; `route: faq` produced relative
  sidebar links and `path: /guide/` matched nothing.
- **Search matches front matter tags.** Tags were collected and never written to
  the index.
- **Changelog release types in the heading.** `## v2.0.0 - 2025-05-01 {type: major}`
  sets the badge; without a type it is inferred from the version (X.0.0 major,
  X.Y.0 minor, otherwise patch). The suffix used to land in the date, and every
  release showed as a patch. Prerelease versions such as `1.0.0-beta.2` no longer
  split into a version and a date.
- `mokadocs stats` reads a dry-run build: generated pages, API types, members,
  namespaces, coverage, loaded plugins and search entries now match what `build`
  produces. It counted compiled `.xml` doc files under `bin/`, which the CLI never
  uses, so most projects showed no API and zero generated pages, and it counted a
  built site inside the docs folder as source. JSON keys are unchanged, plus
  `Search Entries`.
- `mokadocs clean` also deletes the `.mokadocs` cache, takes `--config`/`-c`, and
  no longer deletes `./_site` when the configuration fails to parse.
- `mokadocs info` takes `--config`/`-c` and shows the docs and output paths from
  the configuration. It looked for `./docs` and `./_site`, so this site reported
  "Not built yet".
- Short aliases: `-c`, `-o` and `-v` on `build` and `serve`, `-p` for the `serve`
  port, `-t` and `-p` on `new page`, `-p` on `new plugin` and `new component`.
- `mokadocs.yml` is used when `mokadocs.yaml` does not exist.
- `mokadocs serve` prints the warning and error counts after every build and lists
  the errors (warnings with `--verbose`). It printed nothing about diagnostics.
- The NuGet install widget reads the version from `Directory.Build.props` when the
  project file has none, and understands `VersionPrefix` with `VersionSuffix`. This
  site's API page showed `Moka.Docs.Core 1.0.0`.
- `<see langword="null"/>` renders as code. It rendered nothing, leaving
  "Returns  when missing."
- A warning for social link icons that are not in the icon set. The footer prints
  the name as text.
- **Landing page feature cards from front matter.** `features` takes a list of
  cards with `title`, `icon`, `description` and an optional `link`; `featuresTitle`
  and `featuresSubtitle` set the section heading.
- **Blazor preview blocks work without `previewHost` or `library`.** For plain Razor
  previews the plugin generates a host in `.mokadocs/preview-host/` instead of failing
  the build. The Library sample builds again.
- **API reference links.** `<see cref>` and exception types link to type pages,
  member anchors, or learn.microsoft.com for `System.*` and `Microsoft.*` types. A
  reference that doesn't resolve shows as inline code instead of a link-colored name
  that went nowhere. See Also entries link the same way and keep `href` URLs.
- **API member details.** Member blocks show remarks, examples, see also, type
  parameters and `<value>`, and appear for any documented member; they used to need a
  summary, parameter or return value. Type and member signatures show their
  attributes, and Python pages their decorators.
- **ASP.NET Core host: plugins with options.** `MokaDocsOptions.Plugins` declares
  plugins by id, each with the options its `plugins:` entry would take in
  `mokadocs.yaml`. A plugin registered in the service collection runs; before, only
  the REPL and Blazor preview flags declared anything, so a custom plugin never ran
  and the registered OpenAPI plugin couldn't be turned on.
- `CNAME`, `_redirects` and `_headers` in the docs folder root are copied, as are
  `.txt`, `.webmanifest` and `.avif` files. A GitHub Pages custom domain file was
  dropped because it has no extension.

### 🐛 Fixed

- **`mokadocs doctor` reported problems that did not exist.** On this repository
  it exited 2 with one error and three warnings, none of them real.
  - Broken links were found with a text search, so every link example inside a
    code block was reported. Links are now read from the parsed Markdown, and
    checked against the routes a dry-run build produces, so API pages, plugin
    pages, `route:` overrides and section directories like `/guide` all count.
  - The plugin check held its own list of ids, which were wrong: it approved
    `repl`, which never loads, and rejected `mokadocs-repl`, which does. It now
    uses the plugins the CLI registers.
  - `--config` resolved paths against the working directory rather than the yaml
    file, so pointing it at another project diagnosed the current one.
  - The orphan-image check scanned the build output, and ignored images used as
    the logo or favicon.
  - XML Docs and API Coverage read compiled `.xml` files, which the CLI never uses
    because it analyzes source. The XML Docs check is gone; API Coverage is now
    measured on the model the API pages are built from.
  - Search only read the `enabled` flag. It now confirms the index built.
  - A site with no C# projects, or with search turned off, is no longer a warning.
  - The documented `-c` and `-v` aliases did not exist.
- **`doctor --fix` could corrupt front matter.** Existing front matter was passed
  through `Regex.Replace` as a replacement string, where `$0` and `$1` are
  substitutions, so `description: Save $1 today` came back as a garbled copy of
  the whole block. The title is now inserted by concatenation, preserving line
  endings and any UTF-8 byte order mark.
- **`<inheritdoc/>` across projects produced blank API descriptions.** Each
  project is compiled on its own, so an interface from another project is
  unresolved; Roslyn reports it unqualified and as a base class. The resolver
  looked it up by full name and missed, which left `Name`, `Order` and
  `ExecuteAsync` undescribed on every build phase page of this site. It now falls
  back to a unique simple-name match for unqualified references.
- **The build cache ignored MokaDocs upgrades.** Entries were keyed only by the
  user's source files, so a newer analyzer kept serving models built by the old
  one until a file changed. Each entry now records the identity of the assemblies
  that produced it, and is rebuilt when they differ.
- A delegate's generated `Invoke` member had no documentation, so its row
  rendered blank. It now shares the delegate's comment.
- A plugin that threw during the build was only logged. It is now recorded as a
  build error, so the build summary and `validate` show it.
- CI's test-results artifact contained one project's results: all five test
  projects wrote to the same file name. Each now gets its own.
- `scripts/RunSamples.bat` still built this repo's site into the root `_site`.
- **Theme options that did nothing.** `showSearch`, `showDarkModeToggle`,
  `showPrevNext`, `showBreadcrumbs`, `showBackToTop`, `showVersionSelector`,
  `showCopyButton`, `showLineNumbers` and `accentColor` reached the templates but
  nothing read them. Turning search off also left the search button, dialog and
  shortcut in place.
- **`primaryColor` had no visible effect.** Every page started on a color preset,
  and presets override `--color-primary` with `!important`. A site that changes
  `primaryColor` now starts with no preset until the reader picks one.
- **Canonical links doubled the base path** (`/Repo/Repo/page`) when `site.url`
  already ended in it, while the sitemap left it out. Both now accept `site.url`
  with or without the base path. `robots.txt` pointed at a sitemap even with
  `build.sitemap: false`.
- **The code highlighter swallowed code.** Text between highlighted tokens was
  inserted as HTML, so `if (a<b)` lost everything after `<` and `<br/>` became a
  real line break.
- **Mermaid diagrams with generics or `<<interface>>` broke**, and broke again
  after a light/dark switch, because the source was written unencoded. Diagrams
  did not render at all on the landing layout, and the Mermaid script loaded on
  every page.
- `{year}` in `site.copyright` was printed literally; pages without a description
  had an empty meta description instead of `site.description`.
- A nested sidebar section's chevron collapsed its parent's list.
- `:::steps` headings had no ids, so table of contents and search links to them
  went nowhere. Card icons only knew ten names.
- The NuGet install widget never appeared on sites built with a base path.
- Exception `cref`s lost a leading `T` from names such as `TimeoutError`, and
  unresolved ones kept a `!:` prefix.
- `MOKADOCS_*` environment variables were ignored unless the yaml already had the
  matching section.
- `expanded: false` in a section's `index.md` was ignored by the generated nav.
- `serve` ignored `build.cache: false`, and never loaded a `net10.0` project
  assembly for REPL blocks.
- `new page` wrote titles unquoted, so `--title "Api: v2"` produced a page titled
  "Untitled". `new plugin` referenced MokaDocs projects by paths that only exist
  inside this repository, and used the typed name as the plugin id.
- `plugins[].path` and entries without a name were skipped without a word;
  `validate` and `doctor` now warn.
- The Blazor preview plugin failed every build of a site that declared it without
  any preview blocks, and counted documentation that mentions the
  `data-blazor-preview` attribute as a block. Scaffolded preview hosts referenced
  Moka.Blazor.Repl.Host 1.3.0, which was never published.
- Python API reference: a module's functions page overwrote a class with the same
  name (`calculator.py` defining `Calculator`), an entry inherited options from the
  entry before it, and the analyzer failed on Python 3.9. The analyzer script was
  extracted to one shared temp file and deleted after each run, so two builds at
  the same time could delete it from under each other.
- Link card descriptions kept only their last plain-text run, so a description with
  inline code was cut short. Sections in the generated sidebar ignored the icon set
  in their `index.md`.
- Open Graph and Twitter tags were written with an empty `og:url` when `site.url`
  was not set. Edit links built on Windows contained backslashes.
- The landing layout's feature cards and the `mokadocs init` starter page claimed
  multi-version docs, offline search and custom plugins. They now describe what
  exists.
- ASP.NET Core host: search fetched its index from the host root and linked
  results without the base path, `MapMokaDocs("/path")` changed the route but not
  the links, a root-relative `LogoUrl` was rewritten into a 404, canonical links
  doubled the base path, and section folders had no redirect on Windows. The site
  is now built with the base path instead of patching the HTML afterwards.
- **`AddMokaDocs()` replaced the application's configuration.** The engine registered
  its feature flag settings as `IConfiguration`, so after `AddMokaDocs()` anything in
  the app that injected `IConfiguration`, `BindConfiguration` included, read MokaDocs'
  feature flags instead of `appsettings.json`.
- ASP.NET Core host:
  - Blazor previews never built: the plugin couldn't receive `previewHost` or
    `library`, looked for the host in a folder that wasn't on disk, and copying the
    published host through the in-memory file system threw. Previews build, and the
    published host is served from memory (`.wasm` as `application/wasm`).
  - A relative `DocsPath` resolved from the working directory, so an app started from
    another folder found no docs. It and the plugin path options resolve from the
    content root.
  - The API reference left out protected members, operators, conversions and delegate
    parameters, and nested types had no XML docs. Signatures read like declarations,
    with accessibility, modifiers, constant values and accessor accessibility.
  - `Version` did nothing; it shows in the header's version selector. `NavEntry.AutoGenerate`
    did nothing; on an `/api` entry it lists each namespace with its types.
- The dev server's feedback endpoint returned 404 under a base path.
- `AddMokaDocsThemes()` and `AddMokaDocsRendering()` were empty. The CLI
  registered feature management twice.
- 50 public symbols had no XML documentation, including every `MokaFeatureFlags`
  constant. `doctor` reports 100% API coverage for this repository.
- **API reference text lost everything in angle brackets.** XML doc text and
  `<c>`/`<code>` contents were written to the page unescaped after the XML reader
  had decoded them, so `<c>List&lt;T&gt;</c>` rendered as "List". Python
  docstrings had the same problem. Meta descriptions and search snippets of API
  pages now get the summary as plain text instead of HTML.
- **Types in the global namespace failed the build on Windows.** Their route
  contained `<global namespace>`; they are now listed under `(global)`.
- **Code indentation inside `<pre>` blocks.** Scriban's auto-indent shifted every
  line of the page content, and a pass that stripped the common indent afterwards
  also stripped real indentation: Python "View Source" panels lost a level, and
  Mermaid sources kept the layout's indentation. Auto-indent is now off and the
  stripping pass is gone.
- Section redirects (a folder without an `index.md`) used a relative URL, so a host
  that serves `/docs/guide` without a trailing slash, such as the ASP.NET Core
  integration, sent readers to `/docs/getting-started/`. They are now root-relative
  and include the base path.
- A title or description containing `"` ended the `<meta>` attribute early. Head
  tags are now escaped.
- Code blocks showed one line number more than they had lines.
- **The Copy button covered the language label** on the landing layout, where both
  were always visible in the same corner. It now takes the label's place on hover, as
  on other pages, and the label comes back only after the button fades out. On touch
  screens the button shows without hover; it used to be invisible there. Line numbers
  were added to every code block, and where the theme doesn't position them they ran
  into the code as text (`MyLib.csproj12345678` on the landing page).
- Generated pages (API, OpenAPI, Python) got an "Edit this page" link to the docs
  folder itself. They now have none.
- `new component card` used variants and icons that do not exist.
- Member tables and headings showed metadata names such as `op_Addition` and
  `this[]`. They show `operator +(Money a, Money b)`, `implicit operator Money(int
  amount)` and `this[int index]`, parameter types keep their generic arguments, and
  each overload gets its own anchor instead of sharing one.
- **C# analysis compiled against five runtime assemblies**, with no global usings and
  no `#if` symbols, so types from packages, other projects and most of the framework
  were unresolved and `#if NET8_0_OR_GREATER` branches were skipped. Projects now
  compile against the running runtime's shared frameworks and the package and
  project references in `obj/project.assets.json`, with the project's implicit and
  explicit usings, `Nullable`, `DefineConstants` and the SDK's framework symbols for
  its highest target framework. The analysis cache is invalidated when the project
  file, a `Directory.Build.props`, the assets file or a reference changes.
- **Member signatures** left out accessibility, modifiers, `this`, accessor
  accessibility and constant values. They read like the declaration, for example
  `public static string Shout(this string value)` and `public const int Max = 10`.
  Struct pages lost a Sealed badge their declarations never had.
- A `partial` type got one API page per declaration, all on the same route. It gets
  one page, and View Source shows every declaration.
- `<inheritdoc cref>` was ignored and only the direct base type and the type's own
  interfaces were searched. The cref is followed, then the whole base chain and all
  interfaces, nearest first. The ASP.NET Core host follows crefs too.
- View Source showed private members, and internal ones whatever `includeInternals`
  said. It shows only the members the page documents.
- The install widget described the first project even when it was a test or sample
  app. It uses the first project that produces a package.
- Attributes: `AttributeUsage` showed as `Usage`, strings lost their quotes and enum
  values showed as numbers. Names and arguments appear as written.
- Summaries on the `/api` index page kept `<see cref>` references as link-colored
  text with no link. They link like on the type pages.
- `mokadocs new plugin MyCustomPlugin` created the class `MyCustomPluginPlugin`.
- A `site.logo` or `site.favicon` file that doesn't exist gave a broken image and a
  favicon link that 404s. The build warns and the theme uses its default logo.
- `serve`:
  - A busy port crashed with an `ObjectDisposedException` that hid the port error.
    It now prints the reason and exits 1.
  - The file watcher ignored any path containing `_site` or a folder starting with
    `.`, so a project stored under a folder like `~/.projects` never rebuilt, and
    output inside the docs folder under any other name rebuilt in a loop. It now
    ignores the output folder and dot-names below the docs folder.
  - REPL packages whose assembly names start with `System.`, such as
    `System.Reactive`, were skipped, and "Packages loaded" printed even when the
    restore failed.
  - **REPL snippets couldn't be stopped.** They ran inside `serve`, where the
    5 second timeout was only a cancellation token: `while (true) { }` kept running
    until `serve` exited, `Environment.Exit` stopped the dev server, and
    `Console.ReadLine()` read from the terminal. Snippets now run in a
    `mokadocs repl-worker` process that is killed and replaced on a timeout or
    crash, exits with `serve`, gets no console input, and returns at most 100,000
    characters of output.
- The Blazor preview plugin sorted `bin/Release` framework folders by name, picking
  `net9.0` over `net10.0`.
- Python API examples collapsed onto one line and kept their docstring indent after
  the first line. They are now code blocks.

### 🧪 Tests

- 1246 tests, up from 466. New: `DoctorChecksTests`, `DryRunBuildTests`,
  `BuildCacheTests`, `DevServerTests`, `OutputDirectoryGuardTests`,
  `NavigationBuildPhaseTests`, `ProjectStatsTests`, `CleanCommandTests`,
  `ConfigPathTests`, `NewCommandTests`, `ServeCommandTests`,
  `PluginDiagnosticsTests`, `PythonApiPluginTests` (skipped without Python),
  `NestedContainerTests`, `RelativeLinksTests`, `ChangelogExtensionTests`,
  `SiteUrlsTests`, `SiteConfigEnvironmentTests`, `ApiGenerationTests`,
  `FileWatcherTests`, `NuGetPackageResolverTests`, `ApiDocTextTests`,
  `TargetFrameworksTests`, `ReplExecutionServiceTests` (real worker processes),
  `EmbeddedThemeCodeBlockTests`, `ApiPageRendererTests`, `MemberSignatureTests`,
  `PartialTypeTests`, `ProjectCompilationTests`, `ViewSourceTests`,
  `InheritDocChainTests`, `CSharpAnalysisPhaseTests`, `AspNetCoreHostTests`,
  `ReflectionApiModelBuilderTests`, `LandingLayoutTests`,
  `LandingFeaturesFrontMatterTests`, `BrandAssetRenderingTests`,
  `GitLastUpdatedTests` (skipped without git), `BlazorPreviewScaffoldTests`,
  rendering checks in `BuildPipelineIntegrationTests`, and analyzer and XML doc
  cases for accessibility, `<inheritdoc/>` tags, exception crefs, delegate docs,
  escaping and the global namespace. Each fix was confirmed by reverting it and
  watching its tests fail.

### 📚 Docs

- Every page was checked against the code. Pages described features that do not
  exist (the `wide` and `raw` layouts, `plugins[].path`, `autoGenerate`, version
  ranges, multi-version builds, the blog, minification, contributors, search
  providers), wrong option names and defaults, and examples that did not compile
  or parse. Those pages now describe what the code does, and say plainly what is
  not implemented.
- `cli-reference.md` documents `validate`, and describes what each `doctor` check
  actually does. The deployment guide's pull request example used
  `grep "WARNING"`, a string MokaDocs never prints; it now uses `validate` and
  `doctor`.

## [1.6.0] - 2026-09-11

### 🐛 Fixed

- **Tabbed content (`=== "Title"`) never worked and hung the build.** The
  `TabGroupParser` pushed two blocks from `TryOpen` and parented one to the
  other, corrupting the block tree and sending Markdig into an infinite loop.
  Because the extension was never registered in the pipeline, no build ever hit
  it, and it had no test file at all. The parser is rewritten to push a single
  container and record tab boundaries as child indices, which the renderer slices
  apart, the same approach `::: steps` and `::: code-group` already used.
  `docs/guide/markdown.md` documented the syntax and rendered it as literal
  `=== "npm"` paragraphs.

- **Admonition titles were silently dropped.** `::: tip Hot Tip` rendered as a
  bare `<div class="tip">` with no title, despite being documented. The
  `AdmonitionExtension`, which implements titles and icons, existed and was
  tested but was never registered, so Markdig's built-in container parser
  handled `:::` instead. It is now registered ahead of the built-in parser, and
  the theme CSS moves from `.note` / `.tip` to `.admonition-{type}` with title
  and content rows. Selectors are descendant rather than direct-child, so
  admonitions nested inside tabs are styled too.

- **Custom themes did not work.** `theme.name` was read from mokadocs.yaml and
  round-tripped but never used, and `ThemeLoader` was registered in DI and never
  resolved, so every build used the embedded theme. A new `ThemeResolver` picks
  between the embedded theme and a theme directory, and `ThemeAssetPhase` now
  ships a custom theme's own css/js/assets instead of the compiled-in stylesheet.
  Falls back to the embedded theme, with a warning, when the directory is missing
  or has no layouts.

- **No logging provider was registered, so every `ILogger` call was discarded.**
  `--verbose` set the minimum level but nothing was listening, which made it
  affect only the diagnostics summary. It now registers a console provider;
  quiet builds are unchanged.

- **`mokadocs serve` ignored `build.basePath`.** A site built for a subdirectory
  emits links like `/Sub/_theme/css/main.css`, but the dev server resolved paths
  from the output root, so every asset 404'd locally. `DevServer` now strips the
  configured base path, and `serve` gained a `--base-path` option to match
  `build`.

- **`build --draft` was parsed and ignored.** Draft pages were excluded
  unconditionally. The flag now flows through `BuildContext.IncludeDrafts` to the
  render, output and search-index phases. `serve` gained the same option.

- `MarkdownParserOptions.EnableAdmonitions` and `EnableTabs` were never read.
  They now gate their extensions.

- Removed a dead `css_inline` template variable that emitted an empty `<style>`
  element on every page.

- Fixed a typo in `SiteConfigReader.NormalizeBasePath` and made it public, so the
  CLI stops hand-duplicating base-path normalization.

- Corrected the `BuildPipeline` comment describing when the plugin hook fires.

- **No `.nojekyll` marker was written unless the Blazor preview plugin ran.**
  Every site emits `_theme/`, and GitHub Pages runs Jekyll on branch-based
  deployments, which strips directories starting with an underscore. The marker
  was emitted by the Blazor preview plugin, and only once it had resolved a
  published preview host, so most sites deployed without CSS or JS.
  `OutputPhase` now writes it on every build.

- **Discovery ingested its own build output.** Pointing `build.output` at a path
  inside `content.docs` (`./docs/_site`) made every build pick up the previous
  build's assets and nest a copy of the site inside itself, growing on each run.
  `FileDiscoveryService` now excludes anything under the resolved output
  directory. This repo's own site is built to `docs/_site` as a result.

### ✨ New

- **Build cache.** Roslyn C# analysis is roughly three quarters of a build, and
  it is now cached in `.mokadocs/cache/` next to mokadocs.yaml, keyed per project
  by a fingerprint of every source file the analyzer reads. A warm build of this
  repo drops from 2.6s to 0.87s. Adding, editing or deleting any source file
  invalidates only the affected project. `--no-cache` and `build.cache: false`
  bypass it; a corrupt or unwritable cache degrades to a normal build rather than
  failing it.

- **`build --watch`.** Rebuilds on changes to the docs directory or mokadocs.yaml
  without starting a server. The option existed but did nothing.

### 🧪 Tests

- Added `Microsoft.Testing.Extensions.TrxReport` to the test projects. Only
  `TrxReport.Abstractions` arrived transitively, so the trx report switch CI
  passes discovered zero tests and exited 5, failing the build.

- `TabbedContentExtensionTests` (9 cases, including a hang guard and a check that
  a bare `===` is still a setext heading) and `ThemeResolverTests` (8 cases).
  Total is now 458, up from 424.

### 🔄 Changed

- Replaced em dashes with hyphens in project descriptions and the Python sample
  fixture, finishing the earlier sweep.

## [1.5.0] - 2026-09-11

### 🔄 Changed

- **All NuGet dependencies updated to their latest compatible versions.** 31
  package bumps in `Directory.Packages.props`, including Scriban 7.0.6 to 7.4.0,
  Markdig 1.1.2 to 1.3.2, YamlDotNet 16.3.0 to 18.1.0, Roslyn 5.3.0 to 5.9.0,
  Spectre.Console 0.55.0 to 0.57.2, System.CommandLine 2.0.5 to 2.0.12,
  Microsoft.Extensions.* 10.0.5 to 10.0.12, Moka.Blazor.Repl.* to 1.3.5, and on
  the test side xunit.v3 3.2.2 to 4.0.0, NSubstitute 5.3.0 to 6.2.0 and
  Verify.XunitV3 31.15.0 to 32.0.0.

  This clears the NuGet audit advisories that were failing `dotnet build` under
  `TreatWarningsAsErrors`. No source changes were needed for any bump except
  OpenAPI (below).

- **Central package management now pins transitive dependencies.**
  `CentralPackageTransitivePinningEnabled` is on, with `NuGet.Packaging` and
  `NuGet.Protocol` pinned to 7.9.0. Those arrive transitively through
  `Moka.Blazor.Repl.Compiler`, which still depends on 7.3.0 (advisory
  GHSA-g4vj-cjjj-v7hg). The pins can come out once that package moves up.

  The two sample web projects joined central package management as part of this;
  `Moka.Docs.Samples.Api` keeps its per-framework OpenAPI split via
  `VersionOverride`.

### ⚠️ Breaking

- **`Moka.Docs.Plugins` now depends on `Microsoft.OpenApi` 2.12.2 instead of
  `Microsoft.OpenApi.Readers` 1.6.x.** `OpenApiParser` was rewritten against the
  2.x API: `OpenApiDocument.Parse` in place of `OpenApiStringReader`, `JsonNode`
  in place of the removed `IOpenApiAny` hierarchy, `JsonSchemaType` flags in
  place of the plain type string, and `OpenApiSchemaReference` for `$ref`
  detection. YAML support moved to the separate `Microsoft.OpenApi.YamlReader`
  package, which is now referenced and registered.

  The `OpenApiParser.Parse(string)` / `Parse(Stream)` signatures are unchanged
  and generated output is byte-identical apart from one improvement: datetime
  examples now render exactly as written in the spec instead of being round
  tripped through `OpenApiDateTime` (`"2026-03-19T10:30:00Z"` rather than
  `"2026-03-19T10:30:00.0000000+00:00"`).

  Held at 2.x rather than 3.x because `Microsoft.AspNetCore.OpenApi` 10.x
  constrains `Microsoft.OpenApi` to `>= 2.12.0 && < 3.0.0`.

### 🧪 Tests

- **Test runs now use Microsoft.Testing.Platform instead of VSTest.** xunit.v3
  4.0.0 dropped VSTest support on the .NET 10 SDK, so a root `global.json` opts
  in with `"test": { "runner": "Microsoft.Testing.Platform" }`. It sets no `sdk`
  section, so no SDK version is pinned.

  This changes the test CLI: use `--report-trx --report-trx-filename x.trx`
  instead of `--logger "trx;LogFileName=x.trx"`. CI was updated to match.

- Full suite is 422 tests (211 per target framework), all passing.

### 🔧 CI

- Both workflows now install the .NET 9 **and** .NET 10 SDKs. The solution
  multi-targets `net9.0;net10.0`, which the .NET 9 SDK alone cannot build.

## [1.4.1] - 2026-04-08

### 🐛 Fixed
- **Sidebar navigation links are now always root-relative.** Nav paths from
  `mokadocs.yaml` like `path: mpc-fopdt` (no leading `/`) were rendered as
  relative `href` attributes. When viewing a deeply-nested page like
  `/algo-api/base/basecontroller`, clicking such a link navigated to
  `/algo-api/base/mpc-fopdt` instead of `/mpc-fopdt`. The
  `NavigationBuildPhase` now normalizes all nav paths to start with `/`
  before matching pages and before storing on the `NavigationNode.Route`,
  so sidebar links always resolve from the site root regardless of the
  current page depth.

## [1.4.0] - 2026-04-08

### ✨ New
- **`mokadocs-python-api` plugin** - generates API reference documentation for
  Python libraries. Parses `.py` source files via a bundled Python script
  (pure stdlib `ast` module, zero pip dependencies) and produces the same page
  layout as the existing C# API docs using `ApiPageRenderer`.

  Supports:
  - Classes, dataclasses (`Record`), enums, protocols/ABCs (`Interface`)
  - Methods (instance, static, class, abstract), properties, fields
  - Type annotations (parameters, return types, class variables, `Optional[T]`)
  - Decorators → badges and attributes
  - Google-style docstrings (Args, Returns, Raises, Examples, Attributes, Note,
    Warning, See Also sections)
  - `__all__` exports filter
  - Module-level functions as static members of a synthetic module type
  - Mermaid type dependency graphs

  Configuration:
  ```yaml
  plugins:
    - name: mokadocs-python-api
      options:
        source: ../src/mylib           # path to Python source dir
        label: "Python API"             # nav label (default)
        routePrefix: /python-api        # URL prefix (default)
        docstringFormat: google          # docstring parser (default)
        pythonPath: python3              # Python executable (default)
  ```

  Requires Python 3.9+ on PATH. Falls back from `python3` → `python` on
  Windows where `python3` is a Microsoft Store alias (exit code 9009).

### 📚 Docs
- New `docs/plugins/python-api.md` - full plugin documentation with config
  reference, supported constructs table, docstring parsing examples,
  limitations, and a worked end-to-end example.
- Fixed `docs/themes/customization.md` footer section - was using a fake
  `footer: copyright:` top-level yaml key that doesn't exist in the schema.
  Now correctly documents `site: copyright:` and `theme: options: showBuiltWith:`
  to control the "Built with MokaDocs" branding.

## [1.3.8] - 2026-04-08

### ✨ New - `SiteAssetReference` for logo and favicon

`site.logo` and `site.favicon` in `mokadocs.yaml` now support the full range
of path forms users actually want to write, with automatic resolution,
asset copying, and base-path-aware URL emission. The new
`SiteAssetReference` type parses the yaml value once at config load time
and downstream code works only with resolved data (`SourcePath`,
`PublishUrl`, `IsAbsoluteUrl`).

Supported path forms:

| Yaml value | Publish URL | File copied |
|---|---|---|
| `logo.png` (bare filename) | `/logo.png` | `_site/logo.png` |
| `assets/logo.svg` (relative) | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `./assets/logo.svg` | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `/assets/logo.svg` (leading slash) | `/assets/logo.svg` | `_site/assets/logo.svg` |
| `../branding/logo.png` (escapes yaml dir) | `/_media/logo.png` | `_site/_media/logo.png` |
| `https://cdn.example.com/logo.png` | `https://cdn.example.com/logo.png` | (no copy) |
| `//cdn.example.com/logo.png` | `//cdn.example.com/logo.png` | (no copy) |
| `data:image/svg+xml;base64,…` | *(URL verbatim)* | (no copy) |

Prior to this release, only relative paths inside the `content.docs` tree
were actually discovered by the asset glob. Paths at the yaml directory
level, parent-directory escapes, and absolute URLs were silently broken
or required unrelated assets under `content.docs` to work by accident.

### 🛡 Implementation details

- **`SiteAssetReference`** - new sealed record in `Moka.Docs.Core.Configuration`
  with `RawValue`, `SourcePath`, `PublishUrl`, and `IsAbsoluteUrl` fields.
- **`SiteConfigReader.ParseAssetReference()`** - new private helper that
  resolves each logo/favicon yaml string against the source file's
  directory, normalizes `./` prefixes, flattens `../` escapes to
  `/_media/{filename}`, and detects absolute URLs (http/https/protocol-
  relative/data URI) for pass-through.
- **Collision detection** - when logo and favicon both flatten to the
  same publish URL from different source files, `SiteConfigReader`
  throws `SiteConfigException` with a clear error message.
- **`BrandAssetResolver`** - new service in `Moka.Docs.Engine.Discovery`
  that runs in the Discovery phase (after the normal glob) and populates
  `BuildContext.BrandAssetFiles` with resolved logo + favicon entries.
  Logs warnings for missing source files but doesn't fail the build.
- **`OutputPhase.CopyAssets`** - now copies brand assets to their
  resolved publish URLs in addition to the normal `content.docs` glob
  output. Skips files already written by the main glob path to avoid
  overwrite conflicts.
- **`ScribanTemplateEngine`** - exposes two new template variables per
  brand asset:
  - `site.logo_url` / `site.favicon_url` - the final URL the theme
    should emit, with base-path prefix applied for relative paths and
    pass-through for absolute URLs.
  - `site.logo` / `site.favicon` - the raw yaml value, kept for
    backward compatibility with any custom user template that read the
    old string directly.
- **`EmbeddedThemeProvider`** - five template sites updated to use
  `site.logo_url` / `site.favicon_url` instead of the old
  `{{ base_path }}/{{ site.logo }}` concatenation, so the new
  absolute-URL pass-through and out-of-tree `_media/` flattening work
  without any conditional logic in the markup.

### 🩺 `doctor` command

Added a new check that validates `site.logo` and `site.favicon`:

- ✓ Pass with the resolved `rawValue → publishUrl` mapping, flagging
  when an escaped source is flattened to `/_media/`.
- ✓ Pass for absolute URLs without touching the filesystem.
- ⚠ Warn when the asset reference has no resolvable source path
  (e.g. yaml parsed without a yamlDir).
- ✗ Error when the source file doesn't exist on disk.

Unset brand assets are silently skipped - a site without a logo is
valid and shouldn't clutter the doctor output.

### 📚 Docs

- **`docs/plugins/blazor-preview.md`** - rewritten for the v3.x plugin
  API. The previous doc described the legacy `mode: wasm | ssr` /
  `wasmAppPath` / `stylesheets` schema which was removed in v1.3.0.
  The new doc covers auto-scaffold + auto-publish, the `library:` yaml
  option, the preview-host project structure, GitHub Pages deployment,
  a full troubleshooting section, and migration notes for users coming
  from the old schema.
- **`docs/configuration/site-config.md`** - expanded the `logo` and
  `favicon` sections with a full table of supported path forms plus
  worked examples for each resolution rule.

### 🧪 Tests

- **`SiteConfigReaderTests`** - 13 new tests covering every path form
  (bare filename, nested relative, `./` normalization, single-level
  `../` escape, multi-level `../` escape, absolute URLs for all four
  scheme types, leading-slash treatment, round-trip raw value
  preservation, collision detection, missing asset returns null).
- **`BrandAssetResolverTests`** - 6 new tests with `MockFileSystem`
  covering inside-yaml-dir resolution, escape-to-media flattening,
  missing source file handling (warns, doesn't throw), absolute URL
  pass-through (not added to copy list), and multi-asset resolution.

**Total:** 211 tests passing (was 198).

### 🔄 Changed

- `SiteMetadata.Logo` and `SiteMetadata.Favicon` changed type from
  `string?` to `SiteAssetReference?`. This is a public API break for
  anyone consuming mokadocs as a library (not a CLI), but the yaml
  schema is 100% backward compatible - all existing `mokadocs.yaml`
  files that set logo/favicon to a relative path inside `content.docs`
  continue to work unchanged. The `SiteMetadataDto` (yaml-facing)
  still uses `string?` on both fields; conversion happens in the reader.
- `Moka.Docs.AspNetCore.SiteConfigFactory` updated to wrap
  `MokaDocsOptions.LogoUrl` / `FaviconUrl` in a `SiteAssetReference`
  with `IsAbsoluteUrl=true` (treats them as consumer-hosted URLs that
  need no copy or base-path prefix).

## [1.3.7] - 2026-04-08

### 🎨 Changed - preview box fonts inherit from mokadocs theme

Follow-up to v1.3.6 theme token inheritance. The Blazor preview box's
Preview / Source tabs and source code block were still rendering with
hardcoded font stacks (`'JetBrains Mono', 'Cascadia Code', ...`) even
though the mokadocs theme exposes `--font-body` and `--font-mono`. Now:

- `.blazor-preview-tab` uses `var(--font-body, inherit)` so tab text
  matches the surrounding docs body font
- `.blazor-preview-source code` uses `var(--font-mono, ...)` so the
  source code tab matches the rest of the docs' `<code>` styling
- `.blazor-preview-error` uses `var(--font-mono, ...)` likewise

Fallback font stacks remain for standalone consumers without a mokadocs
theme.

## [1.3.6] - 2026-04-08

### 🎨 Changed - themed Blazor preview box

All colors in the `mokadocs-blazor-preview` container chrome (the outer box
with the Preview / Source tabs that wraps each preview iframe) now inherit
from mokadocs theme tokens (`--color-primary`, `--color-border`,
`--color-bg-secondary`, `--color-text`, `--color-text-muted`,
`--color-border-light`). Previously the tabs and badge used hardcoded
slate/violet/blue hex values (`#94a3b8`, `#60a5fa`, `#7c3aed`, `#ede9fe`)
that clashed with consumer themes - e.g. Moka.Red docs with its red accent
had blue active tabs and purple "Blazor" badges. Now:

- Active tab underline + text inherit `--color-primary` → matches the
  consumer's accent (red for Moka.Red, blue for ocean, emerald, etc.)
- Hover state uses `--color-text` on `--color-bg` for a subtle theme-aware
  highlight
- Inactive tabs use `--color-text-muted`
- Container border, tab separator, and grid background use
  `--color-border` / `--color-border-light`
- "Blazor" badge became a theme-colored outlined pill chip instead of a
  hardcoded purple block
- Error state uses `--color-primary` + `--color-bg-secondary` instead of
  hardcoded red/pink
- `.blazor-preview-render` default `min-height` bumped from 48px to 160px
  so small previews don't render in a cramped strip before the iframe's
  ResizeObserver reports actual content height
- `.blazor-preview-iframe` gets a `transition: height 200ms ease-out` so
  the post-boot resize is smooth instead of a jarring jump
- `.blazor-preview-render` ships with a subtle 24px grid background so the
  preview area is visually distinct from the surrounding page even while
  the WASM runtime is booting

All fallback values remain in place so standalone consumers without a
mokadocs theme still get reasonable defaults.

## [1.3.5] - 2026-04-08

### ✨ New - GitHub Pages deployment support

The `mokadocs-blazor-preview` plugin now produces output that deploys cleanly to
GitHub Pages, including project-page subpath deployments (e.g.
`username.github.io/Moka.Red/`).

- **Emits `.nojekyll`** at the `_site/` root. GitHub Pages runs Jekyll by default,
  which strips directories starting with `_` - which would wipe `_preview-wasm/`,
  `_preview-assemblies/`, `_framework/`, and `_content/` from the deployed site,
  leaving all preview iframes broken. The `.nojekyll` marker bypasses Jekyll
  entirely so every file ships verbatim.
- **Respects `--base-path` / `Build.BasePath` for the iframe `?assembly=...`
  query parameter.** The `ScribanTemplateEngine.RewriteContentLinks` regex
  already rewrites `src="/..."` attribute openings in page HTML automatically,
  but its pattern only matches the leading `/` after `src="` - it doesn't touch
  `/` characters inside query strings. The plugin now prefixes the assembly path
  (which lives inside the iframe `src` attribute value as `?assembly=/...`) with
  the base path itself, so GitHub Pages subpath deploys resolve the preview DLL
  correctly.

### 🐛 Fixed

- **`BuildCommand` CLI `--base-path` normalization**. When `--base-path /foo/`
  was passed with a trailing slash, the value was stored as-is in
  `BuildContext.Config.Build.BasePath`, causing `ScribanTemplateEngine.RewriteContentLinks`
  to produce `/foo//_preview-wasm/...` (double slash) when concatenating with
  absolute paths in page HTML. `BuildCommand` now trims/normalizes the same way
  `SiteConfigReader.NormalizBasePath` does for YAML-provided values: leading
  slash, no trailing slash (except when the value is literally `/`).

## [1.3.1] - 2026-04-08

### ✨ New
- **`mokadocs-blazor-preview` plugin** now auto-discovers, auto-scaffolds, and
  auto-publishes the consumer's docs preview-host project. Yaml shrinks from
  ~120 lines of plugin config to a single `library: <PackageId>@<Version>` line.
- **Auto-discovery**: when `previewHost:` is omitted, the plugin scans
  `./preview-host/`, `./docs-preview-host/`, then any direct subdirectory of
  the docs root containing a `Microsoft.NET.Sdk.BlazorWebAssembly` csproj.
- **Auto-scaffold**: if no preview-host is found at the resolved location, the
  plugin writes a fresh, library-agnostic project from a generic template
  (csproj + Program.cs + wwwroot/index.html + empty Directory.Build.props /
  Directory.Build.targets / Directory.Packages.props shadow files so the
  scaffolded project doesn't inherit anything from the consumer repo's parent
  build configuration). The csproj substitutes `{LIBRARY_ID}` and
  `{LIBRARY_VERSION}` from the new `library:` yaml option, plus a pinned
  `Moka.Blazor.Repl.Host` `PackageReference` for the runtime.
- **Auto-publish**: the plugin shells out to `dotnet publish -c Release -f
  net10.0 -o publish-output/net10.0` on the preview-host. Incremental - skipped
  when the publish-output marker (`_framework/blazor.webassembly.js`) is newer
  than every input file under the preview-host directory (excluding `bin/`,
  `obj/`, `publish-output/`).
- **Owner contract**: scaffolded files are written ONLY when missing. mokadocs
  never overwrites user edits. The user owns the files thereafter and can
  freely customize Program.cs (services, theme), wwwroot/index.html (CSS, fonts),
  and the csproj (extra PackageReferences).

### 🔄 Changed
- New yaml option `library: <PackageId>@<Version>` (or just `<PackageId>` →
  resolves to `*` latest). Used by the auto-scaffold template's csproj
  PackageReference. Required when no preview-host exists yet.
- `previewHost:` yaml option is now **optional**. When omitted, auto-discovery
  resolves it to a conventional location.
- `references:` and `usings:` yaml options remain supported but are usually
  unnecessary now - the auto-scaffolded preview-host's bin already contains the
  consumer's library DLLs (resolved from nuget.org via the PackageReference),
  which the plugin reads for Roslyn references automatically.

## [1.3.0] - 2026-04-08

### ⚠️ Breaking
- **`mokadocs-blazor-preview` plugin** rewritten. The yaml schema for the plugin's
  options changed:
  - **Removed** `mode` (was `wasm` | `ssr`) - the plugin now always emits one Blazor
    WebAssembly iframe per preview block, with an SSR snapshot in `<noscript>` for
    crawlers and JS-disabled visitors.
  - **Removed** `wasmAppPath` and the implicit NuGet-cache discovery via
    `WasmAppAssetResolver`. The plugin no longer scans `~/.nuget/packages`.
  - **Removed** `stylesheets` - the consumer's preview-host project now ships its own
    CSS in its `index.html` and via Blazor's static web asset bundle.
  - **Added** required `previewHost` - path (relative to docs root) to the consumer's
    Blazor WebAssembly preview-host project. Conventional layout:
    `{previewHost}/bin/Release/{tfm}/` (Roslyn references) and
    `{previewHost}/publish-output/{tfm}/wwwroot/` or `publish-output/wwwroot/`
    (the static WASM runtime, copied verbatim to `_site/_preview-wasm/`).
  - `references` and `usings` are kept and now act as additive overrides on top of
    the preview-host bin. Same-named assemblies in `references` win, so a local
    source build can override a NuGet copy in the host bin.

### ✨ New
- Mokadocs is now **library-agnostic**: zero hardcoded references to Moka.Red. The
  previously hardcoded `Moka.Red.Feedback.Toast.IMokaToastService` SSR stub was removed.
- Framework-assembly filtering uses `Assembly.Load()` against the host runtime to
  detect which DLLs the .NET shared framework supplies - works on both .NET 9 and
  .NET 10 hosts with no hardcoded prefix lists.
- Iframes are emitted with `loading="lazy"` so off-screen previews don't boot a
  Blazor runtime until scrolled into view.

### 🔄 Changed
- `BlazorPreviewPlugin.Version` bumped to `3.0.0` (in-process plugin version, separate
  from the mokadocs CLI version).
- Iframe `<noscript>` SSR fallback HTML is rendered using the same Roslyn-compiled
  assembly bytes that ship to the browser, so crawlers see the same DOM as JS users.

### Removed
- `BlazorPreviewMode` enum.
- `WasmAppAssetResolver` (NuGet-cache scanning).

## [1.2.0] - 2026-04-06

### ✨ New
- **WASM Blazor preview mode** - interactive component previews on static sites (GitHub Pages, etc.)
  - Components compiled to DLLs at build time, loaded in-browser via Blazor WebAssembly iframe
  - SSR fallback in `<noscript>` for users without JavaScript
  - Configurable via `mode: wasm` (default) or `mode: ssr` in plugin options
- `Moka.Blazor.Repl.Wasm` auto-downloaded as dependency - no manual install needed
- `WasmAppAssetResolver` auto-discovers WASM app from NuGet cache
- Updated Blazor preview docs with WASM mode documentation

### 🔄 Changed
- Default Blazor preview mode changed from SSR to WASM
- Plugin falls back to SSR with warning if WASM app not found

## [1.1.2] - 2026-04-06

### ✨ New
- `showBuiltWith` theme option - shows "Built with MokaDocs v{version}" in footer (default `true`, set `false` to hide)
- MokaDocs version automatically read from assembly and displayed in footer

### 🔄 Changed
- Footer branding now includes version number on both default and landing layouts

## [1.1.1] - 2026-04-06

### 🐛 Fixed
- Use NuGet package for `Moka.Blazor.Repl.Compiler` instead of local ProjectReference
- Fix GitHub Packages auth for NuGet publish workflow

## [1.1.0] - 2026-04-06

### ✨ New
- **Blazor SSR preview** - replaced regex renderer with real Roslyn + HtmlRenderer server-side rendering
- `Moka.Blazor.Repl.Compiler` package integration for live Blazor component previews

### 🐛 Fixed
- Restored inline CSS/JS for Blazor preview chrome

### 🔄 Changed
- Updated Scriban to 7.0.6, Spectre.Console to 0.55.0, Verify.XunitV3 to 31.15.0

## [1.0.7] - 2026-03-27

### ✨ New
- Auto-create GitHub Release from CHANGELOG.md when version tags are pushed
- Auto-categorized release notes via `.github/release.yml`

## [1.0.6] - 2026-03-27

### ✨ New
- Show version in CLI startup messages (`MokaDocs v1.0.6 - Building...`)

### 🐛 Fixed
- NuGet social link icon now uses official NuGet logo SVG from [NuGet/Media](https://github.com/NuGet/Media)

## [1.0.5] - 2026-03-27

### ✨ New
- NuGet install widget on API reference pages with tabbed install commands and NuGet.org link
- NuGet and Discord social link icons for footer
- Social links showcase and NuGet widget docs in sample library

### 🔄 Changed
- Updated Markdig to 1.1.2, Scriban to 7.0.5, Verify.XunitV3 to 31.13.5
- NuGet badge in README now dynamically pulls latest version from nuget.org
- CI workflow triggers NuGet publish on version tags

## [1.0.4] - 2026-03-27

### 🐛 Fixed
- All markdown and API tables are now horizontally scrollable on mobile
- Added `table-responsive` wrapper to API member, parameter, exception, and OpenAPI tables
- Constructor summary tables use compact `TodoItem(…)` format instead of full parameter list

## [1.0.3] - 2026-03-27

### 🐛 Fixed
- Logo path now uses `base_path` prefix for subdirectory deployments
- Hide duplicate site name when logo image is configured

## [1.0.2] - 2026-03-27

### 🐛 Fixed
- Logo path uses `base_path` for correct rendering on GitHub Pages

## [1.0.1] - 2026-03-27

### 🐛 Fixed
- Stop double-encoding XML doc summaries in API index tables

## [1.0.0] - 2026-03-24

### ✨ New
- **13 projects**: Core, CLI, Engine, Parsing, CSharp, Rendering, Themes, Search, Plugins, Serve, Versioning, Cloud, AspNetCore
- **10-phase build pipeline** with Roslyn C# analysis and Markdig markdown parsing
- **4 built-in plugins**: Interactive REPL, Blazor preview, Changelog timeline, OpenAPI docs
- **Embedded default theme** - 5 color themes, 7 code syntax themes, 4 code block styles, dark/light mode
- **Full-text client-side search** with `Ctrl+K` / `Cmd+K` shortcut
- **Dev server** with WebSocket hot reload and file watcher
- **ASP.NET Core integration** via `AddMokaDocs()` / `MapMokaDocs()`
- **Versioning** with multi-version dropdown selector
- **`basePath` support** for subdirectory deployments (GitHub Pages, IIS subfolders)
- **Social links** in footer - GitHub, NuGet, Discord, Twitter, 70+ Lucide icons
- **Type dependency graphs** - auto-generated Mermaid class diagrams on API pages
- **Custom markdown extensions**: admonitions, tabs, cards, steps, link-cards, code groups, Mermaid, changelogs
- **`<inheritdoc/>` resolution** - walks base types and interfaces for missing docs
- **Favicon and logo** via `site.favicon` / `site.logo` config
- **Feature gating** - hide pages behind feature flags via `requires` front matter
- **Sitemap and robots.txt** generation
- **384 tests** across 5 test projects (net9.0 + net10.0)
- **9 CLI commands**: `init`, `build`, `serve`, `clean`, `info`, `validate`, `doctor`, `stats`, `new`

[1.2.0]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.2...v1.2.0
[1.1.2]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/jacobwi/Moka.Docs/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.7...v1.1.0
[1.0.7]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.6...v1.0.7
[1.0.6]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/jacobwi/Moka.Docs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/jacobwi/Moka.Docs/releases/tag/v1.0.0
