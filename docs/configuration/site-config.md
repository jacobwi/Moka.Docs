---
title: Site Configuration
order: 1
---

# Site Configuration

MokaDocs reads its settings from a `mokadocs.yaml` file at the root of your project. The file holds site metadata, content sources, theme settings, navigation, plugins and build options.

Keys are camelCase. MokaDocs ignores keys it doesn't recognize and gives no warning, so a misspelled key has no effect.

## Configuration File Location

Every command that loads a project picks the configuration file in this order:

1. The path passed with `--config` (or `-c`)
2. `mokadocs.yaml` in the current working directory
3. `mokadocs.yml` in the current working directory

Relative paths inside the file resolve against the folder that contains the configuration file, not the working directory.

---

## `site` Section

The `site` section holds metadata about your documentation site. It is required.

### `title`

- **Type:** `string`
- **Required:** Yes

The site name. It appears in the header, in the browser tab title (`Page Title - Site Title` on regular pages) and as `og:site_name`. `og:title` is always the page's own title, or `Untitled` when the page has none.

```yaml
site:
  title: "My Project Documentation"
```

### `description`

- **Type:** `string`
- **Default:** `""`

The description used for pages whose front matter has no `description`. MokaDocs writes it to `<meta name="description">`, `og:description` and `twitter:description`.

```yaml
site:
  description: "Official documentation for My Project, a .NET library for building APIs."
```

### `url`

- **Type:** `string`
- **Default:** `""`

The public address of the site, including the scheme. MokaDocs builds absolute URLs from it for the canonical link, `og:url`, `sitemap.xml` and the `Sitemap:` line in `robots.txt`. Without `url` there is no canonical link, no Open Graph or Twitter tags, and no sitemap. A trailing slash is optional.

```yaml
site:
  url: "https://docs.myproject.com"
```

For a site served under a [`basePath`](#basepath), `url` can include the base path or leave it out. Both of these produce `https://user.github.io/Repo/guide` for the `/guide` page:

```yaml
site:
  url: "https://user.github.io/Repo"
build:
  basePath: /Repo
```

```yaml
site:
  url: "https://user.github.io"
build:
  basePath: /Repo
```

When `url` is set, the default layout writes the canonical link plus Open Graph and Twitter tags on every page. The landing layout writes the canonical link but no Open Graph tags.

### `logo`

- **Type:** `string` (nullable) - filesystem path or absolute URL
- **Default:** `null`

Path to a logo image file displayed in the site header alongside the title.
Supported formats include SVG, PNG, JPG, and WebP.

**All paths are resolved relative to the directory containing `mokadocs.yaml`**,
not the `content.docs` directory. The build pipeline finds the file, copies it
into the output site, and emits its URL in the theme templates, with the base
path in front when `build.basePath` or `--base-path` is set.

#### Supported path forms

| Yaml value | Resolved to | Publish URL |
|---|---|---|
| `logo.png` | `{yamlDir}/logo.png` | `/logo.png` |
| `assets/logo.svg` | `{yamlDir}/assets/logo.svg` | `/assets/logo.svg` |
| `./assets/logo.svg` | `{yamlDir}/assets/logo.svg` | `/assets/logo.svg` |
| `/assets/logo.svg` | `{yamlDir}/assets/logo.svg` | `/assets/logo.svg` |
| `../branding/logo.png` | `{yamlDir}/../branding/logo.png` | `/_media/logo.png` (flattened) |
| `https://cdn.example.com/logo.png` | *(no file copy)* | `https://cdn.example.com/logo.png` |
| `//cdn.example.com/logo.png` | *(no file copy)* | `//cdn.example.com/logo.png` |
| `data:image/svg+xml;base64,...` | *(no file copy)* | *(URL verbatim)* |

#### Relative paths inside the yaml directory

The simplest case. The file lives at or below `mokadocs.yaml`:

```yaml
# mokadocs.yaml at docs/mokadocs.yaml
site:
  logo: assets/logo.svg    # resolves to docs/assets/logo.svg
```

The publish URL mirrors the source layout, so `docs/assets/logo.svg` becomes
`/assets/logo.svg` on the deployed site.

#### Relative paths escaping the yaml directory (flattened)

When you reference a file **above** the yaml directory with `..`, the build
copies it into `_site/_media/` and emits the URL `/_media/{filename}`. The
flattening avoids URL normalization issues (`/../foo.png` doesn't work in
browsers) and keeps out-of-tree assets from colliding with your content:

```yaml
# mokadocs.yaml at docs/mokadocs.yaml
site:
  logo: ../branding/logo.png     # source: branding/logo.png
                                  # output: _site/_media/logo.png
                                  # URL:    /_media/logo.png
```

**Collision detection**: if `site.logo` and `site.favicon` both flatten to
the same publish URL (same filename from different source directories),
reading the configuration fails with an error. Rename or move one of them.

#### Absolute URLs (CDN-hosted)

Full URLs, protocol-relative URLs, and `data:` URIs are passed through
verbatim. No file is copied, and no base path is added (the host is already
in the URL):

```yaml
site:
  logo: https://cdn.example.com/brand/logo.svg
```

Use this when your brand assets are hosted on a CDN and you want the docs
site to reference them directly without duplicating the file.

#### Default behavior when omitted

With no logo, the default layout shows a generic book icon next to the site
title instead of an `<img>` tag.

#### Missing files

When a local logo file doesn't exist, the build reports a warning that names
the path it looked for, and the site renders as if no logo were set: no
`<img>` tag, and the book icon in the header. `mokadocs validate` lists the
warning, and so does `mokadocs build --verbose`. Absolute URLs aren't checked.

### `favicon`

- **Type:** `string` (nullable) - filesystem path or absolute URL
- **Default:** `null`

Path to a favicon file for the browser tab icon. The resolution rules
described for [`logo`](#logo) above apply here too: relative paths are
resolved from the yaml directory, `../` escapes flatten to `/_media/`,
absolute URLs pass through verbatim, and a publish URL collision with the
logo is an error.

```yaml
site:
  favicon: assets/favicon.ico
```

If omitted, browsers fall back to requesting `/favicon.ico` from the root of
the domain. A local favicon file that doesn't exist gets the same treatment as
a [missing logo](#missing-files): a build warning, and no
`<link rel="icon">` tag on the pages.

### `copyright`

- **Type:** `string` (nullable)
- **Default:** `null`

A notice shown in the site footer. `{year}` is replaced with the current year when the site is built.

```yaml
site:
  copyright: "Copyright {year} My Company. All rights reserved."
```

### `editLink`

- **Type:** `object` (nullable)
- **Default:** `null`

Settings for the "Edit this page" link at the bottom of pages that use the default layout. The link only appears when [`theme.options.showEditLink`](#options.showeditlink) is `true`, and that option defaults to `false`.

The link has the form `{repo}/edit/{branch}/{path}/{file}`, where `{file}` is the page's path inside the docs folder. This is the edit URL format GitHub uses.

#### `editLink.repo`

- **Type:** `string`
- **Default:** none

The repository URL. MokaDocs doesn't check that it is set: without it, the link starts with `/edit/` and points at a page on your own site that doesn't exist. The landing layout also uses `repo` for its "View on GitHub" button, whatever `showEditLink` is set to.

#### `editLink.branch`

- **Type:** `string`
- **Default:** `"main"`

The branch the link edits.

#### `editLink.path`

- **Type:** `string`
- **Default:** `"docs/"`

The docs folder's path inside the repository.

```yaml
site:
  editLink:
    repo: "https://github.com/myorg/myproject"
    branch: "main"
    path: "docs/"

theme:
  options:
    showEditLink: true
```

The resulting edit link for a page at `docs/guides/getting-started.md` would be:
`https://github.com/myorg/myproject/edit/main/docs/guides/getting-started.md`

---

## `content` Section

The `content` section tells MokaDocs where your Markdown files and C# projects are.

### `docs`

- **Type:** `string`
- **Default:** `"./docs"`

The folder that holds your Markdown files. MokaDocs reads every `.md` file in it and in its subfolders.

Files with these extensions are copied to the output at the same relative path: `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.webp`, `.avif`, `.ico`, `.pdf`, `.zip`, `.mp4`, `.webm`, `.css`, `.js`, `.json`, `.xml`, `.txt`, `.webmanifest`, `.woff`, `.woff2`, `.ttf` and `.eot`. The files `CNAME`, `_redirects` and `_headers` are copied too when they sit directly in the docs folder. Nothing inside the [`build.output`](#output) folder is read.

```yaml
content:
  docs: "./docs"
```

### `projects`

- **Type:** `list` of `ProjectSource`
- **Default:** `[]`

C# projects to generate API reference pages from. MokaDocs doesn't run MSBuild. It parses every `.cs` file under the project's folder with Roslyn, skipping `bin` and `obj`, with the target framework, usings and references read from the project files, and reads the XML doc comments from the source. Each type gets a page at `/api/{namespace}/{type}` (lowercase, with the namespace's dots turned into slashes), and `/api` lists them all. See [API Documentation](/guide/api-docs).

Each entry in the list is a `ProjectSource` object with the following properties:

#### `projects[].path`

- **Type:** `string`
- **Required:** Yes

The path to the `.csproj` file. A file that doesn't exist produces a build warning. The first project that produces a package (test projects and projects with `IsPackable` set to `false` are skipped) also supplies the package name and version for the install widget on the `/api` page.

#### `projects[].label`

- **Type:** `string` (nullable)
- **Default:** The `.csproj` file name without its extension

The name MokaDocs gives the Roslyn compilation, which also appears in `--verbose` build logs. It isn't shown on pages or in the navigation.

#### `projects[].includeInternals`

- **Type:** `bool`
- **Default:** `false`

By default the reference covers `public`, `protected` and `protected internal` types and members. Set `true` to add `internal` and `private protected` ones. Private members are never included, and a nested type is left out whenever a type that contains it is left out.

```yaml
content:
  projects:
    - path: "../src/MyLibrary/MyLibrary.csproj"
      label: "MyLibrary"
      includeInternals: false

    - path: "../src/MyLibrary.Extensions/MyLibrary.Extensions.csproj"
      label: "MyLibrary.Extensions"
      includeInternals: true
```

---

## `theme` Section

The `theme` section controls the visual appearance of your documentation site.

### `name`

- **Type:** `string`
- **Default:** `"default"`

`default` selects the built-in theme. Any other value is the path to a theme folder, relative to the configuration file or absolute. The folder needs Scriban layouts in `layouts/*.html` and can also contain `partials/`, `css/`, `js/` and `assets/` folders. When the folder doesn't exist or has no layouts, MokaDocs uses the default theme and reports a warning (`mokadocs validate` lists it). See [Themes & Customization](/themes/customization).

```yaml
theme:
  name: "default"          # built-in theme
  # name: "./my-theme"     # theme folder next to mokadocs.yaml
```

### `options`

- **Type:** `ThemeOptions`

Settings for the theme's look and behavior. The options below are the complete set. A custom theme's templates receive the same values.

#### `options.primaryColor`

- **Type:** `string`
- **Default:** `"#0ea5e9"`

The main brand color, used for links, the current navigation item and hover states. Any CSS color value works. MokaDocs sets the `--color-primary` CSS variable to it and derives lighter and darker shades with `color-mix()`.

The color presets in the header set the same variable and take priority over `primaryColor`. When you change `primaryColor` and leave [`defaultColorTheme`](#options.defaultcolortheme) at `ocean`, pages start with no preset, so your color shows. A reader who picks a preset in the header gets that preset instead, and the choice is saved in the browser's `localStorage`. If you set `defaultColorTheme` to another preset, that preset applies and hides `primaryColor`.

```yaml
theme:
  options:
    primaryColor: "#2563eb"
```

#### `options.accentColor`

- **Type:** `string`
- **Default:** `"#f59e0b"`

Sets the `--color-accent` CSS variable. The default theme uses it to highlight matched words in search results.

```yaml
theme:
  options:
    accentColor: "#f59e0b"
```

#### `options.codeTheme`

- **Type:** `string`
- **Default:** `"catppuccin-mocha"`

The syntax highlighting colors for code blocks. When [`codeThemeSelector`](#options.codethemeselector) is on, readers can switch themes from the header. Clicking the dark mode toggle swaps paired themes: `catppuccin-mocha` with `catppuccin-latte`, and `github-dark` with `github-light`.

| Theme Name | Description |
|---|---|
| `catppuccin-mocha` | Warm dark theme (default) |
| `catppuccin-latte` | Warm light theme |
| `github-dark` | GitHub's dark syntax theme |
| `github-light` | GitHub's light syntax theme |
| `dracula` | Dracula color scheme |
| `one-dark` | Atom One Dark inspired |
| `nord` | Arctic-inspired dark theme |

```yaml
theme:
  options:
    codeTheme: "github-dark"
```

#### `options.codeThemeSelector`

- **Type:** `bool`
- **Default:** `false`

Shows a `</>` button in the header for picking one of the 7 code themes. The choice is saved in `localStorage`.

```yaml
theme:
  options:
    codeThemeSelector: true
```

#### `options.codeStyle`

- **Type:** `string`
- **Default:** `"plain"`

The window frame drawn around code blocks. When [`codeStyleSelector`](#options.codestyleselector) is on, readers can switch styles from the header.

| Style Name | Description |
|---|---|
| `plain` | No frame (default) |
| `macos` | Title bar with three colored dots |
| `terminal` | Terminal bar with a `$` prompt |
| `vscode` | Tab bar in the primary color |

```yaml
theme:
  options:
    codeStyle: "macos"
```

#### `options.codeStyleSelector`

- **Type:** `bool`
- **Default:** `false`

Shows a button in the header for picking one of the 4 code styles. The choice is saved in `localStorage`.

```yaml
theme:
  options:
    codeStyleSelector: true
```

#### `options.colorThemes`

- **Type:** `bool`
- **Default:** `true`

Shows the palette button in the header, with 6 color presets: Ocean, Emerald, Violet, Amber, Rose and Moka Red. The reader's pick is saved in `localStorage`. Hiding the button doesn't stop [`defaultColorTheme`](#options.defaultcolortheme) from applying.

```yaml
theme:
  options:
    colorThemes: true
```

#### `options.showEditLink`

- **Type:** `bool`
- **Default:** `false`

Shows the "Edit this page" link at the bottom of pages that use the default layout. It also needs [`site.editLink`](#editlink).

```yaml
theme:
  options:
    showEditLink: true
```

#### `options.showLastUpdated`

- **Type:** `bool`
- **Default:** `true`

Shows "Last updated: YYYY-MM-DD" at the bottom of Markdown pages that use the default layout. When the docs folder is inside a git repository and `git` is on `PATH`, the date is that of the last commit that changed the file. A file with uncommitted changes, or one git doesn't track, uses its modified time on disk instead, and so does every file when git isn't available. The same date goes into the sitemap's `<lastmod>`.

A shallow clone only has the newest commit, so every page shows that commit's date. `actions/checkout` makes a shallow clone unless you set `fetch-depth: 0`; see [Deployment](/advanced/deployment#last-updated-dates-in-ci).

```yaml
theme:
  options:
    showLastUpdated: true
```

#### `options.showContributors`

- **Type:** `bool`
- **Default:** `false`

Not implemented yet. Setting it has no effect.

#### `options.showFeedback`

- **Type:** `bool`
- **Default:** `true`

Shows a "Was this page helpful?" widget below pages that use the default layout. A vote is stored in the reader's `localStorage` and also sent as a POST request to `{basePath}/api/feedback`. The `mokadocs serve` dev server is the only thing that handles that request, and all it does is log the vote. On a static host the request fails with no visible effect.

```yaml
theme:
  options:
    showFeedback: true
```

#### `options.showAnimations`

- **Type:** `bool`
- **Default:** `false`

When `false`, CSS animations and transitions finish instantly. Readers whose operating system asks for reduced motion get the same result whatever this is set to.

```yaml
theme:
  options:
    showAnimations: false
```

#### `options.showBuiltWith`

- **Type:** `bool`
- **Default:** `true`

Shows "Built with MokaDocs v{version}" in the footer, with the version of MokaDocs that built the site.

```yaml
theme:
  options:
    showBuiltWith: true
```

#### `options.showDarkModeToggle`

- **Type:** `bool`
- **Default:** `true`

Shows the sun/moon button in the header. Without it, pages follow the reader's operating system color scheme, or a choice saved during an earlier visit.

```yaml
theme:
  options:
    showDarkModeToggle: true
```

#### `options.showSearch`

- **Type:** `bool`
- **Default:** `true`

Shows the search button in the header and turns on the search dialog and its Ctrl/Cmd+K shortcut. They appear only when [`features.search.enabled`](#search.enabled) is also `true`. With `showSearch: false` the build still writes `search-index.json`.

```yaml
theme:
  options:
    showSearch: true
```

#### `options.showTableOfContents`

- **Type:** `bool`
- **Default:** `true`

Shows the "On this page" sidebar on pages that use the default layout. A page can also turn it off with `toc: false` in its front matter, and a page without headings has none.

```yaml
theme:
  options:
    showTableOfContents: true
```

#### `options.showPrevNext`

- **Type:** `bool`
- **Default:** `true`

Shows Previous and Next links at the bottom of pages that use the default layout.

```yaml
theme:
  options:
    showPrevNext: true
```

#### `options.showBreadcrumbs`

- **Type:** `bool`
- **Default:** `true`

Shows the breadcrumb trail above the content on pages that use the default layout. The home page has no breadcrumbs.

```yaml
theme:
  options:
    showBreadcrumbs: true
```

#### `options.showBackToTop`

- **Type:** `bool`
- **Default:** `true`

Shows a floating back-to-top button once the reader scrolls down 300 pixels, on pages that use the default layout.

```yaml
theme:
  options:
    showBackToTop: true
```

#### `options.showCopyButton`

- **Type:** `bool`
- **Default:** `true`

Adds a Copy button to every code block.

```yaml
theme:
  options:
    showCopyButton: true
```

#### `options.showLineNumbers`

- **Type:** `bool`
- **Default:** `true`

Adds line numbers to every code block with three or more lines. The theme script applies this to all blocks; there is no per-block switch.

```yaml
theme:
  options:
    showLineNumbers: true
```

#### `options.tocDepth`

- **Type:** `int`
- **Default:** `3`
- **Range:** `2` to `6` (other values are clamped)

The deepest heading level listed in the "On this page" sidebar. With `3`, headings down to `h3` are listed.

The list also includes `h1` headings and shows at most three levels of nesting. On a page that starts with an `h1`, that means `h4` and deeper headings never appear, whatever `tocDepth` is set to.

```yaml
theme:
  options:
    tocDepth: 3
```

#### `options.showVersionSelector`

- **Type:** `bool`
- **Default:** `true`

Shows the version dropdown in the header of pages that use the default layout. It appears only when [versioning](#versioning) is enabled with at least one version.

```yaml
theme:
  options:
    showVersionSelector: true
```

#### `options.defaultColorTheme`

- **Type:** `string`
- **Default:** `"ocean"`
- **Values:** `ocean`, `emerald`, `violet`, `amber`, `rose`, `moka-red`

The color preset a page starts with until the reader picks one. There is one exception: when [`primaryColor`](#options.primarycolor) differs from its default and this stays `ocean`, pages start with no preset so `primaryColor` shows. Any other value applies that preset, which hides a custom `primaryColor`. The preset applies even when [`colorThemes`](#options.colorthemes) hides the palette button.

```yaml
theme:
  options:
    defaultColorTheme: "ocean"
```

#### `options.socialLinks`

- **Type:** `list` of social link objects
- **Default:** `[]`

Icon links shown in the footer of pages that use the default layout. The landing layout doesn't show them.

##### `socialLinks[].icon`

- **Type:** `string`
- **Required:** Yes

An icon name from the built-in set, matched without regard to case. The brand icons in the set are `github`, `twitter`, `discord` and `nuget`, and general icons such as `mail`, `globe` and `link` work too. The full list is under [`nav[].icon`](#nav.icon). An unknown name is printed as text where the icon would be, and the build warns about it.

##### `socialLinks[].url`

- **Type:** `string`
- **Required:** Yes

The full URL to link to.

```yaml
theme:
  options:
    socialLinks:
      - icon: "github"
        url: "https://github.com/myorg/myproject"
      - icon: "discord"
        url: "https://discord.gg/myserver"
      - icon: "nuget"
        url: "https://www.nuget.org/packages/MyLibrary"
```

---

## `features` Section

The `features` section turns optional site features on and configures them.

### `search`

The build writes `search-index.json` with each page's title, headings, tags and the first 300 characters of its text, and the theme searches that file in the browser. See [Search](/guide/search).

#### `search.enabled`

- **Type:** `bool`
- **Default:** `true`

When `false`, no search index is written, and pages get no search button, search dialog or Ctrl/Cmd+K shortcut.

#### `search.provider`

- **Type:** `string`
- **Default:** `"flexsearch"`

Reserved; has no effect. The built-in search described above is the only one.

```yaml
features:
  search:
    enabled: true
```

### `versioning`

Adds a version dropdown to the header. One build produces one site: MokaDocs doesn't check out branches or build other versions, so you build and deploy each version yourself. See [Versioning](/advanced/versioning).

#### `versioning.enabled`

- **Type:** `bool`
- **Default:** `false`

Turns on the version dropdown. It appears on pages that use the default layout, as long as `versions` has at least one entry and `theme.options.showVersionSelector` is on.

#### `versioning.strategy`

- **Type:** `string`
- **Default:** `"directory"`

Reserved; has no effect.

#### `versioning.versions`

- **Type:** `list` of version definitions
- **Default:** `[]`

The versions listed in the dropdown, in this order.

##### `versions[].label`

- **Type:** `string`

The text shown in the dropdown. The version's URL slug comes from it: lowercased, with spaces turned into `-` and characters other than letters, digits, `.` and `-` removed. `v2.0 LTS` becomes `v2.0-lts`.

##### `versions[].branch`

- **Type:** `string` (nullable)
- **Default:** `null`

Reserved; has no effect.

##### `versions[].default`

- **Type:** `bool`
- **Default:** `false`

The dropdown links a version with `default: true` to the site root (`{basePath}/`) and gives it a "latest" badge. Every other version links to `{basePath}/{slug}/`.

The dropdown's button shows the current version: the first entry with `default: true`, or when there is none, the first entry that isn't a prerelease, or else the first entry.

##### `versions[].prerelease`

- **Type:** `bool`
- **Default:** `false`

Adds a "pre" badge in the dropdown. It also counts when the current version is picked, as described under `default`. It has no other effect.

```yaml
features:
  versioning:
    enabled: true
    versions:
      - label: "v3.0"
        default: true
      - label: "v3.1-beta"
        prerelease: true
      - label: "v2.0"
      - label: "v1.0"
```

### `blog`

Reserved; has no effect. See [Blog](/guide/blog).

---

## `plugins` Section

The `plugins` section turns on built-in plugins. Each entry is a `PluginDeclaration`. See [Plugin System](/plugins/overview).

### `plugins[].name`

- **Type:** `string`
- **Required:** Yes

The plugin id. Matching ignores case. These are the plugins MokaDocs ships:

| Id | Plugin |
|---|---|
| `mokadocs-repl` | [Interactive REPL](/plugins/repl) |
| `mokadocs-blazor-preview` | [Blazor Component Preview](/plugins/blazor-preview) |
| `mokadocs-changelog` | [Release Changelog](/plugins/changelog) |
| `mokadocs-python-api` | [Python API Reference](/plugins/python-api) |
| `openapi` | [OpenAPI Plugin](/plugins/openapi) |

A name that matches none of them is skipped. `mokadocs build` only mentions it with `--verbose`, while `mokadocs validate` and `mokadocs doctor` warn about it. Mermaid diagrams don't need a plugin; see [Mermaid Diagrams](/guide/diagrams).

### `plugins[].path`

- **Type:** `string` (nullable)
- **Default:** `null`

Not supported. No MokaDocs host loads plugin assemblies, so `path` is ignored and an entry with only a `path` loads nothing. `mokadocs validate` and `mokadocs doctor` warn when it is set.

### `plugins[].options`

- **Type:** `dict` (string keys, arbitrary values)
- **Default:** `{}`

Settings passed to the plugin. Each plugin's page lists the keys it reads.

```yaml
plugins:
  - name: "mokadocs-repl"

  - name: "openapi"
    options:
      spec: "./openapi.json"
      label: "REST API"
      routePrefix: "/rest-api"
```

Warnings and errors a plugin reports during the build, and a plugin that throws, show up in the build summary. An error makes `mokadocs build` exit with code 1.

---

## `nav` Section

The `nav` section defines the sidebar by hand. When it is present, it replaces the generated sidebar. Without it, MokaDocs builds the sidebar from the docs folder, with one entry for each top-level folder or page. In that generated sidebar, a section's `index.md` can set `expanded: false` to start the section collapsed. See [Navigation & Sidebar](/configuration/navigation).

Each entry is a `NavItem`.

### `nav[].label`

- **Type:** `string`
- **Required:** Yes

The display text for this navigation item in the sidebar.

### `nav[].path`

- **Type:** `string` (nullable)
- **Default:** `null`

The route this item links to, such as `"/guides/getting-started"`. A leading `/` is added if you leave it out. An item without a `path` acts as a label for its children. If no page exists at `path` and the item has children, the item links to its first child.

When an item has a `path` and no `children`, MokaDocs fills in its children from your pages: every public page exactly one route segment below `path`, sorted by front matter `order` and then by title. `path: /guides` picks up `/guides/intro` but not `/guides/advanced/tuning`. Each child gets its own children the same way, so `/guides/advanced/tuning` shows up under `/guides/advanced` when that page exists (from `guides/advanced/index.md`, for example).

API type pages live at `/api/{namespace}/{type}`, more than one segment below `/api`, so `path: /api` gets no children and stays a single link to the API index.

### `nav[].icon`

- **Type:** `string` (nullable)
- **Default:** `null`

An icon shown next to the label. The sidebar shows icons on top-level items and on second-level items that have no children. Names are matched without regard to case, and a name outside the built-in set shows no icon. The built-in set:

`alert-triangle`, `arrow-left`, `arrow-right`, `book`, `book-open`, `box`, `braces`, `calendar`, `check`, `chevron-down`, `chevron-right`, `clock`, `code`, `code-2`, `compass`, `cpu`, `database`, `discord`, `download`, `external-link`, `file`, `file-code`, `file-text`, `folder`, `git-branch`, `github`, `globe`, `heart`, `home`, `image`, `info`, `key`, `layers`, `lightbulb`, `link`, `list`, `lock`, `mail`, `map`, `menu`, `message-circle`, `newspaper`, `nuget`, `package`, `play`, `puzzle`, `rocket`, `scroll-text`, `search`, `settings`, `shield`, `star`, `tag`, `terminal`, `twitter`, `upload`, `users`, `wrench`, `x`, `zap`

### `nav[].order`

- **Type:** `int`
- **Default:** `0`

Items are sorted by `order`, lowest first. Items with the same `order` keep their order from `mokadocs.yaml`, so a `nav` without any `order` values appears exactly as written.

Use `order` when you want to control the sidebar from each item's declaration rather than its position in the file:

```yaml
nav:
  - label: "API Reference"
    path: /api
    order: 10           # appears last even though listed first in yaml
  - label: "Getting Started"
    path: /getting-started
    order: 1            # appears first
  - label: "Guides"
    path: /guides
    order: 5            # appears in the middle
```

:::tip
`children` are sorted the same way at every level. Children generated from a `path` are sorted by their pages' front matter `order`, then by title.
:::

### `nav[].expanded`

- **Type:** `bool`
- **Default:** `false`

Makes an item with children start expanded. A section that contains the current page is always expanded.

### `nav[].autoGenerate`

- **Type:** `bool`
- **Default:** `false`

Not implemented yet. To list pages under an item, give it a [`path`](#nav.path) and no `children`.

### `nav[].children`

- **Type:** `list` of `NavItem`
- **Default:** `[]`

Nested child navigation items. The sidebar renders three levels: top-level items, their children and their grandchildren. Items nested deeper are not shown.

```yaml
nav:
  - label: "Getting Started"
    icon: "rocket"
    expanded: true
    children:
      - label: "Installation"
        path: "/getting-started/installation"
      - label: "Quick Start"
        path: "/getting-started/quick-start"
      - label: "Configuration"
        path: "/getting-started/configuration"

  - label: "Guides"            # children come from the pages under /guides
    path: "/guides"
    icon: "book-open"

  - label: "API Reference"
    path: "/api"
    icon: "code"

  - label: "Changelog"
    path: "/changelog"
    icon: "scroll-text"
```

---

## `build` Section

The `build` section controls how the final output is generated.

### `output`

- **Type:** `string`
- **Default:** `"./_site"`

The folder the site is written to, and the one you deploy. The `--output` option of `mokadocs build` and `mokadocs serve` overrides it, and a relative `--output` value also resolves against the configuration file's folder.

The build deletes the output folder first when [`clean`](#clean) is on, and `mokadocs clean` always deletes it. To protect your sources, `mokadocs build`, `serve` and `clean` stop with an error when the output folder is the project folder (the one holding `mokadocs.yaml`), the docs folder, or a folder that contains either. `mokadocs validate` reports the same error. A folder inside the docs folder, such as `./docs/_site`, is allowed, and MokaDocs doesn't read it as content.

```yaml
build:
  output: "./_site"
```

### `basePath`

- **Type:** `string`
- **Default:** `"/"` (site root)

A path prefix for a site served from a subfolder, such as a GitHub Pages project site (`/repo-name`) or an IIS virtual directory. MokaDocs adds it to navigation links, theme CSS and JS paths, logo and favicon URLs, routes in the search index and root-relative links in your Markdown (`/guide/intro` becomes `/repo-name/guide/intro`). A missing leading slash is added and a trailing slash is removed. An empty value means the site root.

```yaml
build:
  basePath: /my-project
```

The `--base-path` option of `mokadocs build` and `mokadocs serve` overrides this value. For absolute URLs, see [`site.url`](#url).

### `clean`

- **Type:** `bool`
- **Default:** `true`

When `true`, the output folder is deleted before each build, so no files from earlier builds remain. Set it to `false` to keep files that other tools write into the output folder. `mokadocs clean` deletes the folder regardless of this setting.

```yaml
build:
  clean: true
```

### `minify`

- **Type:** `bool`
- **Default:** `true`

Not implemented yet. MokaDocs doesn't minify its output, whatever this is set to.

### `sitemap`

- **Type:** `bool`
- **Default:** `true`

When `true`, the build writes `sitemap.xml` with the absolute URL of every public page, plus a `<lastmod>` date for Markdown pages. The sitemap is only written when [`site.url`](#url) is set.

```yaml
build:
  sitemap: true
```

### `robots`

- **Type:** `bool`
- **Default:** `true`

When `true`, the build writes a `robots.txt` that allows all crawlers. It includes a `Sitemap:` line only when `build.sitemap` is `true` and `site.url` is set.

```yaml
build:
  robots: true
```

### `cache`

- **Type:** `bool`
- **Default:** `true`

When `true`, MokaDocs caches the Roslyn analysis of [`content.projects`](#projects) in `.mokadocs/cache` next to `mokadocs.yaml`. Nothing else is cached; Markdown is parsed on every build. A cached result is reused while the project's `.cs` files (path, size and modified time), its `includeInternals` value and the MokaDocs binaries that produced it stay the same. `mokadocs build --no-cache` skips the cache for one build, and `mokadocs clean` deletes it along with the output folder.

```yaml
build:
  cache: true
```

---

## `cloud` Section

Reserved; has no effect.

---

## Environment Variables

Many options can also be set with environment variables, which is useful when a CI build needs different settings. The order of precedence is:

1. A `MOKADOCS_*` environment variable
2. The value in `mokadocs.yaml`
3. The built-in default

A variable applies even when the matching section is missing from `mokadocs.yaml`, and every command that reads the configuration uses it. An empty variable counts as unset.

Boolean variables accept `true`, `1` or `yes`, and `false`, `0` or `no`, in any letter case. Any other value is ignored and the built-in default applies, even when `mokadocs.yaml` sets the option.

```bash
MOKADOCS_PRIMARY_COLOR="#7c3aed" MOKADOCS_SHOW_FEEDBACK=false mokadocs build
```

| Variable | Config key |
|---|---|
| `MOKADOCS_THEME_NAME` | `theme.name` |
| `MOKADOCS_PRIMARY_COLOR` | `theme.options.primaryColor` |
| `MOKADOCS_CODE_THEME` | `theme.options.codeTheme` |
| `MOKADOCS_CODE_STYLE` | `theme.options.codeStyle` |
| `MOKADOCS_SHOW_CODE_THEME_SELECTOR` | `theme.options.codeThemeSelector` |
| `MOKADOCS_SHOW_CODE_STYLE_SELECTOR` | `theme.options.codeStyleSelector` |
| `MOKADOCS_SHOW_COLOR_THEME_SELECTOR` | `theme.options.colorThemes` |
| `MOKADOCS_SHOW_EDIT_LINK` | `theme.options.showEditLink` |
| `MOKADOCS_SHOW_LAST_UPDATED` | `theme.options.showLastUpdated` |
| `MOKADOCS_SHOW_FEEDBACK` | `theme.options.showFeedback` |
| `MOKADOCS_SHOW_ANIMATIONS` | `theme.options.showAnimations` |
| `MOKADOCS_SHOW_BUILT_WITH` | `theme.options.showBuiltWith` |
| `MOKADOCS_SHOW_DARK_MODE_TOGGLE` | `theme.options.showDarkModeToggle` |
| `MOKADOCS_SHOW_SEARCH` | `theme.options.showSearch` |
| `MOKADOCS_SHOW_TABLE_OF_CONTENTS` | `theme.options.showTableOfContents` |
| `MOKADOCS_SHOW_PREV_NEXT` | `theme.options.showPrevNext` |
| `MOKADOCS_SHOW_BREADCRUMBS` | `theme.options.showBreadcrumbs` |
| `MOKADOCS_SHOW_BACK_TO_TOP` | `theme.options.showBackToTop` |
| `MOKADOCS_SHOW_COPY_BUTTON` | `theme.options.showCopyButton` |
| `MOKADOCS_SHOW_LINE_NUMBERS` | `theme.options.showLineNumbers` |
| `MOKADOCS_SHOW_VERSION_SELECTOR` | `theme.options.showVersionSelector` |
| `MOKADOCS_SEARCH_ENABLED` | `features.search.enabled` |
| `MOKADOCS_SEARCH_PROVIDER` | `features.search.provider` (reserved) |
| `MOKADOCS_CLEAN_OUTPUT` | `build.clean` |
| `MOKADOCS_GENERATE_SITEMAP` | `build.sitemap` |
| `MOKADOCS_GENERATE_ROBOTS` | `build.robots` |
| `MOKADOCS_ENABLE_CLOUD_FEATURES` | `cloud.enabled` (reserved) |
| `MOKADOCS_ENABLE_AI_SEARCH` | `cloud.features.aiSummaries` (reserved) |
| `MOKADOCS_ENABLE_PDF_EXPORT` | `cloud.features.pdfExport` (reserved) |
| `MOKADOCS_ENABLE_ANALYTICS` | `cloud.features.analytics` (reserved) |
| `MOKADOCS_ENABLE_CUSTOM_DOMAIN` | `cloud.features.customDomain` (reserved) |

Options that are not in the table have no environment variable.

### Feature Flags

A page whose front matter sets `requires: <flag>` is left out of the build unless that flag is on (see [Front Matter](/configuration/front-matter)). Turn a flag on with `MOKADOCS_FeatureManagement__<flag>=true`:

```bash
MOKADOCS_FeatureManagement__ShowBetaDocs=true mokadocs build
```

A flag name that MokaDocs doesn't define counts as off. Flags only decide which pages are built; every other setting on this page is still controlled by `mokadocs.yaml` and the variables above.

---

## Complete Example

The following example sets every option that has an effect. Options marked above as reserved or not implemented are left out.

```yaml
# mokadocs.yaml - Complete configuration reference

site:
  title: "Contoso SDK Documentation"
  description: "Official documentation for the Contoso SDK for .NET"
  url: "https://docs.contoso.dev"
  logo: "./assets/logo.svg"
  favicon: "./assets/favicon.png"
  copyright: "Copyright {year} Contoso Ltd. All rights reserved."
  editLink:
    repo: "https://github.com/contoso/sdk-dotnet"
    branch: "main"
    path: "docs/"

content:
  docs: "./docs"
  projects:
    - path: "../src/Contoso.Sdk/Contoso.Sdk.csproj"
      label: "Contoso.Sdk"
      includeInternals: false
    - path: "../src/Contoso.Sdk.Extensions/Contoso.Sdk.Extensions.csproj"

theme:
  name: "default"
  options:
    primaryColor: "#0ea5e9"
    accentColor: "#f59e0b"
    codeTheme: "catppuccin-mocha"
    codeThemeSelector: false
    codeStyle: "plain"
    codeStyleSelector: false
    colorThemes: true
    defaultColorTheme: "ocean"
    showEditLink: true
    showLastUpdated: true
    showFeedback: true
    showAnimations: false
    showBuiltWith: true
    showDarkModeToggle: true
    showSearch: true
    showTableOfContents: true
    showPrevNext: true
    showBreadcrumbs: true
    showBackToTop: true
    showCopyButton: true
    showLineNumbers: true
    showVersionSelector: true
    tocDepth: 3
    socialLinks:
      - icon: "github"
        url: "https://github.com/contoso/sdk-dotnet"
      - icon: "discord"
        url: "https://discord.gg/contoso"
      - icon: "nuget"
        url: "https://www.nuget.org/packages/Contoso.Sdk"
      - icon: "twitter"
        url: "https://twitter.com/contoso"

features:
  search:
    enabled: true
  versioning:
    enabled: true
    versions:
      - label: "v3.0"
        default: true
      - label: "v3.1-beta"
        prerelease: true
      - label: "v2.0"
      - label: "v1.0"

plugins:
  - name: "mokadocs-repl"
  - name: "openapi"
    options:
      spec: "./openapi.json"
      label: "REST API"
      routePrefix: "/rest-api"

nav:
  - label: "Getting Started"
    icon: "rocket"
    expanded: true
    children:
      - label: "Installation"
        path: "/getting-started/installation"
      - label: "Quick Start"
        path: "/getting-started/quick-start"
  - label: "Guides"
    path: "/guides"
    icon: "book-open"
  - label: "REST API"
    path: "/rest-api"
    icon: "globe"
  - label: "API Reference"
    path: "/api"
    icon: "code"
  - label: "Changelog"
    path: "/changelog"
    icon: "scroll-text"
    order: 10

build:
  output: "./_site"
  basePath: "/"
  clean: true
  sitemap: true
  robots: true
  cache: true
```

## Minimal Example

A minimal configuration requires only the site title:

```yaml
site:
  title: "My Docs"
```

With this minimal configuration, MokaDocs uses the default for everything else: it reads Markdown from `./docs`, uses the default theme and colors, turns on the built-in search, and writes the site to `./_site`.

A more practical minimal configuration might look like this:

```yaml
site:
  title: "My Library"
  description: "Documentation for My Library"

content:
  docs: "./docs"
  projects:
    - path: "../src/MyLibrary/MyLibrary.csproj"

theme:
  options:
    primaryColor: "#2563eb"
    socialLinks:
      - icon: "github"
        url: "https://github.com/me/my-library"
```

## Path Resolution Notes

- Relative paths in `mokadocs.yaml` resolve against the folder that contains the file, not the working directory.
- `content.docs` points to the root of your Markdown tree.
- `content.projects[].path` values point to `.csproj` files and can climb the directory tree with `../`.
- `build.output` is where the site is written. A relative `--output` value resolves against the same folder.
- `theme.name`, when it isn't `default`, points to a theme folder.
- `site.logo` and `site.favicon` point to asset files that are copied to the output during the build, and may sit outside the configuration folder (see [`logo`](#logo)).

If you run MokaDocs from a different directory than the one holding the configuration file, pass the file with `--config`. Relative paths still resolve against the configuration file's location.
