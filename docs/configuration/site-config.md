---
title: Site Configuration
order: 1
---

# Site Configuration

MokaDocs reads its settings from a `mokadocs.yaml` file at the root of your project. The file holds site metadata, content sources, theme settings, navigation, plugins and build options.

Keys are camelCase and case sensitive. A key MokaDocs doesn't recognize has no effect, and the build, `mokadocs validate` and `mokadocs doctor` warn about it with its line number, such as `unknown key 'build.basepath' (line 12); keys are case sensitive, did you mean 'basePath'?`. Keys under a plugin's `options` are not checked. A file that can't be read at all fails with the line and column of the problem and its cause.

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

Page URLs end with `/`, because each page is written as `{route}/index.html` and hosts such as GitHub Pages redirect the form without the slash. Path segments are percent-encoded, so a page from `Q&A.md` is listed as `https://docs.myproject.com/Q%26A/`.

For a site served under a [`basePath`](#basepath), `url` can include the base path or leave it out. Both of these produce `https://user.github.io/Repo/guide/` for the `/guide` page:

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

The folder that holds your Markdown files. MokaDocs reads every `.md` file in it and in its subfolders; the extension matches in any letter case (`Readme.MD` too) on every operating system. Linked folders (symbolic links, junctions) are followed, except a link that points back at a folder it sits in, which is skipped with a warning instead of being read again and again. A folder that doesn't exist is an error.

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

#### `options.style`

- **Type:** `string`
- **Default:** `"default"`
- **Values:** `default`, `moka`, `minimal`, `classic`, `soft`

The whole look of the default theme: surfaces, borders, corner radii, shadows, fonts and spacing. It applies on top of the color preset, the code theme and the code frame, in light and dark mode. When [`styleSelector`](#options.styleselector) is on, readers can pick another style from the header.

| Style | Look |
|---|---|
| `default` | The original look |
| `moka` | Dense and dark first: near-black surfaces, borders tinted with the primary color, glow rings instead of shadows, uppercase micro-labels, monospace for technical text. Starts in dark mode until the reader uses the dark mode toggle |
| `minimal` | Few borders and shadows, more whitespace, a reading column about 70 characters wide, plain sidebar links |
| `classic` | Solid header bar, bordered sidebar and panels, small radii, dense striped tables, the system font |
| `soft` | Rounded cards with soft shadows, tinted sections, pill-shaped highlights, roomier spacing |

The name is matched without regard to case. An unknown name renders as `default`, and the build reports a warning (`mokadocs validate` lists it). See [Styles](/themes/customization#styles).

```yaml
theme:
  options:
    style: "moka"
```

#### `options.styleSelector`

- **Type:** `bool`
- **Default:** `true`

Shows a layout button in the header for picking one of the 5 styles. The choice is saved in `localStorage` and applied before the page is drawn. With `false`, every page uses [`style`](#options.style), including for readers who picked another style earlier.

```yaml
theme:
  options:
    styleSelector: false
```

#### `options.layout`

- **Type:** object with the keys below
- **Default:** `default` for every part

Picks a layout variant for six parts of the page: the width, the top bar, the footer, the "On this page" panel, the breadcrumbs and the previous/next links. Each part is set on its own and works with every [style](#options.style), color preset and code theme, in light and dark mode. A part you leave out keeps its `default` variant, the theme's original layout.

```yaml
theme:
  options:
    layout:
      width: wide
      navbar: tabs
      footer: columns
      toc: floating
      breadcrumbs: pills
      pageNav: minimal
```

Only the default theme reads these settings. Unlike the style, readers can't switch layouts from the header: every reader gets the layout you set. The landing layout (`layout: landing` in front matter) uses the `navbar` and `footer` settings too.

Names are matched without regard to case. An unknown name renders that part as `default`, and the build reports a warning (`mokadocs validate` lists it), for example `Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide; using 'default'`. Each part also has an [environment variable](#environment-variables), such as `MOKADOCS_LAYOUT_NAVBAR`.

##### `layout.width`

| Value | Layout |
|---|---|
| `default` | The sidebar, a reading column of about 780px (the style can change it) and the "On this page" panel, up to 1440px wide |
| `wide` | A 1040px reading column, up to 1680px wide |
| `full` | The page fills the window and the reading column has no cap |
| `boxed` | The page sits in a 1280px framed column on a tinted background. Narrower windows show it without the frame |

##### `layout.navbar`

| Value | Layout |
|---|---|
| `default` | A sticky bar |
| `compact` | A shorter bar with smaller controls |
| `floating` | A rounded bar inset from the edges |
| `tabs` | Adds a second row with a tab for each top-level sidebar entry, the current one underlined. On a phone the row scrolls sideways |
| `autohide` | Slides away while scrolling down, and comes back on scrolling up or when it gets keyboard focus |

##### `layout.footer`

| Value | Layout |
|---|---|
| `default` | One row: copyright, social links and "Built with" |
| `minimal` | One centered line |
| `columns` | The site name and description next to a column of links for each top-level sidebar section (up to four columns of six links), with the copyright and "Built with" underneath. Top-level pages with nothing under them share a first column titled "Docs" |
| `none` | No footer |

##### `layout.toc`

| Value | Layout |
|---|---|
| `default` | The right-hand column with reading progress, a section counter and a Top link |
| `minimal` | The right-hand column with the links only |
| `inline` | A collapsible "On this page" box under the page title, in place of the right-hand column |
| `floating` | A button in the corner opens the list over the page, at every screen width |

[`showTableOfContents: false`](#options.showtableofcontents) and `toc: false` in a page's front matter still turn it off.

##### `layout.breadcrumbs`

| Value | Layout |
|---|---|
| `default` | A slash-separated trail |
| `chevrons` | Chevron separators, with the current page in bold |
| `pills` | Each step in a rounded chip |
| `path` | A monospace path, like a terminal prompt |
| `back` | Only a "back to parent" link |

[`showBreadcrumbs: false`](#options.showbreadcrumbs) still hides them.

##### `layout.pageNav`

| Value | Layout |
|---|---|
| `default` | Two bordered cards |
| `minimal` | Plain links, each under a thin divider |
| `stacked` | Each link a full-width row, previous above next |

[`showPrevNext: false`](#options.showprevnext) still hides them.

#### `options.sidebar`

- **Type:** `string`
- **Default:** `"auto"`
- **Values:** `auto`, `inline`, `shared`

How pages get the sidebar navigation. `inline` writes the whole sidebar into every page; the pages need no JavaScript for it. `shared` writes it once, to `_theme/js/sidebar.js`, and every page loads that file with a hash of its content in the URL. A page then holds only the top-level entries and the entries along its own path, which is what readers without JavaScript and crawlers see. `auto` is `inline` up to [`sidebarInlineLimit`](#options.sidebarinlinelimit) entries and `shared` above that.

On a site of 5000 pages, the inline sidebar was about 1.3 MB of every page and 6.5 GB in all. Shared, the pages are about 42 KB and the sidebar file 503 KB. The name is matched without regard to case; an unknown name works like `auto`, and the build reports a warning. Only the default theme can share its sidebar: a custom theme always gets it inline, with a warning when this is `shared`. See [Large Sidebars](/configuration/navigation#large-sidebars-inline-or-shared).

```yaml
theme:
  options:
    sidebar: shared
```

#### `options.sidebarInlineLimit`

- **Type:** `int`
- **Default:** `300`

The most sidebar entries, links and section headings together, that [`sidebar: auto`](#options.sidebar) writes into every page. A bigger sidebar is shared. A negative value counts as `0`. At 300 entries, the inline sidebar adds about 80 KB to each page.

```yaml
theme:
  options:
    sidebarInlineLimit: 500
```

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

Lists the people who committed to each Markdown page under the "Last updated" line, most commits first, each with their initials in a circle. The names come from the same `git log` that dates the pages, with the repository's `.mailmap` applied, and commits with the same email count as one person. Emails aren't shown, and nothing is fetched from the network.

Turning it on makes the build read the docs folder's whole git history, since a page's first author is in its oldest commit. Renames aren't followed, so authors from before a file was moved are missing, and a shallow clone only knows the authors of its newest commit (see [Deployment](/advanced/deployment#last-updated-dates-in-ci)). Pages without git history, generated pages and the ASP.NET Core host show no contributors.

```yaml
theme:
  options:
    showContributors: true
```

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

Shows Previous and Next links at the bottom of pages that use the default layout. They follow the sidebar from top to bottom, nested items included, and then go through the public pages the sidebar doesn't list, by `order` and route.

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

Shows the version dropdown in the page header. It appears only when [versioning](#versioning) is enabled.

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

The build writes `search-index.json` with each page's title, tags and the first 300 characters of its text, and each heading with the first 300 characters of the text under it and the words of the code blocks there. The theme searches that file in the browser. See [Search](/guide/search).

#### `search.enabled`

- **Type:** `bool`
- **Default:** `true`

When `false`, no search index is written, and pages get no search button, search dialog or Ctrl/Cmd+K shortcut.

The built-in search described above is the only one. `search.provider` was accepted by earlier versions and never did anything; it is no longer read, and a config that still sets it gets a warning.

```yaml
features:
  search:
    enabled: true
```

### `versioning`

Adds a version dropdown to the header, and when versions name a git branch or tag, has `mokadocs build` build each of them into its own folder of the site. See [Versioning](/advanced/versioning).

#### `versioning.enabled`

- **Type:** `bool`
- **Default:** `false`

Turns versioning on. With no `versions` listed, the site has one version, labelled from your package version (`v1.8` for 1.8.0), and the dropdown shows it. Hide the dropdown with `theme.options.showVersionSelector: false`.

#### `versioning.strategy`

- **Type:** `string`
- **Default:** `"directory"`
- **Values:** `directory`, `dropdown-only`

`directory` builds every version that names a `branch` or `tag` into `{output}/{slug}/`, and the default version at the site root too. `dropdown-only` builds the working tree only and lists the other versions in the dropdown, for versions you build and deploy yourself. A config whose versions name no ref builds like `dropdown-only`.

#### `versioning.versions`

- **Type:** `list` of version definitions
- **Default:** `[]`

The versions listed in the dropdown, in this order.

##### `versions[].label`

- **Type:** `string`
- **Default:** derived

The text shown in the dropdown. Leave it out and the label is `v{major}.{minor}` of the documented package's version, from the first packable project in `content.projects` (its `Version`, `VersionPrefix` or `PackageVersion`, or the nearest `Directory.Build.props` that sets one). A version built from a ref reads it from that ref. When no version is set anywhere, the label is the ref name, or `latest`.

The version's URL slug and folder name come from the label: lowercased, with spaces turned into `-` and characters other than letters, digits, `.` and `-` removed. `v2.0 LTS` becomes `v2.0-lts`.

##### `versions[].tag`

- **Type:** `string` (nullable)
- **Default:** `null`

A git tag to build this version from. Wins over `branch` when both are set.

##### `versions[].branch`

- **Type:** `string` (nullable)
- **Default:** `null`

A git branch to build this version from: the local branch, else `origin/{branch}`. The checked-out branch is built from the working tree, uncommitted changes included. A version with neither `tag` nor `branch` is built from the working tree too.

##### `versions[].default`

- **Type:** `bool`
- **Default:** `false`

The default version is also built at the site root, and the dropdown links to it there and gives it a "latest" badge. It's the first entry with `default: true`, or when there is none, the first entry that isn't a prerelease, or else the first entry.

##### `versions[].prerelease`

- **Type:** `bool`
- **Default:** `false`

Adds a "pre" badge in the dropdown, and counts when the default version is picked. A derived label whose package version has a suffix such as `-beta` is a prerelease too.

```yaml
features:
  versioning:
    enabled: true
    versions:
      - label: next
        branch: main
        prerelease: true
      - tag: v3.0.0
        default: true
      - tag: v2.0.0
```

### `blog`

Turns a folder of Markdown files into a blog: a paginated index of the posts, newest first, a page per tag, an RSS or Atom feed and one sidebar entry. See [Blog](/guide/blog) for the posts' front matter and what the build generates.

```yaml
features:
  blog:
    enabled: true
    title: "News"
    description: "Releases and notes from the team"
    postsPerPage: 10
    feed: [rss, atom]
    authors:
      jane:
        name: "Jane Doe"
        url: "https://github.com/jane"
        avatar: "img/jane.png"
```

#### `blog.enabled`

- **Type:** `bool`
- **Default:** `false`

Builds the blog. Without it, the posts folder builds like any other section.

#### `blog.path`

- **Type:** `string`
- **Default:** `"blog"`

The folder under `content.docs` that holds the posts. Every `.md` file below it, in subfolders too, is a post. Its `index.md` is not a post: its text appears on the index page above the posts. Posts keep the route of their file, so `docs/blog/hello.md` is at `/blog/hello`. A path outside the docs folder is a warning and `blog` is used.

#### `blog.route`

- **Type:** `string`
- **Default:** `/` followed by `path`

Where the index page goes. The other generated pages and the feeds go below it: `{route}/page/2`, `{route}/tags`, `{route}/tags/{tag}`, `{route}/rss.xml`. A route that can't be a folder, or `/`, is a warning and the default is used.

#### `blog.title`

- **Type:** `string`
- **Default:** `"Blog"`

The index page's heading, the label of the blog's sidebar entry when the sidebar is built from folders, and part of the feeds' title.

#### `blog.description`

- **Type:** `string`
- **Default:** `""`

Shown under the index page's heading and used as the feeds' description. When empty, the index page uses the `description` of the posts folder's `index.md`, and the feeds use `site.description`.

#### `blog.postsPerPage`

- **Type:** `int`
- **Default:** `10`

Posts on each index page and tag page. More posts go to `{route}/page/2` and on. A value below 1 is a warning and 10 is used.

#### `blog.sidebarPosts`

- **Type:** `int`
- **Default:** `5`

How many of the latest posts the blog's sidebar entry lists, newest first. `0` lists none, so the entry only links to the index page.

#### `blog.feed`

- **Type:** `string` or `list`
- **Default:** `rss`

Which feeds to write: `rss` (`{route}/rss.xml`, RSS 2.0), `atom` (`{route}/atom.xml`) or both, as `[rss, atom]` or `rss, atom`. `none` or `[]` writes no feed. An unknown name is a warning.

Feeds list the 20 latest posts with their summaries, and need absolute links: without `site.url` the build warns and writes no feed. Every page links each feed from its head with `<link rel="alternate">`, so browsers and feed readers find it.

#### `blog.authors`

- **Type:** map of author key to `name`, `url` and `avatar`
- **Default:** none

Authors defined once and named by key in a post's `author` front matter. Keys match without regard to case. `name` defaults to the key. `avatar` is an absolute URL or a path from the site root; `img/jane.png` and `/img/jane.png` both mean the file at `/img/jane.png`. A post author that is no key is shown as written.

#### `blog.showAuthors`

- **Type:** `bool`
- **Default:** `true`

Shows the authors on the index pages and posts. The feeds name them either way.

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

A plugin of your own: a `.dll`, a project that `mokadocs` builds with `dotnet build -c Release`, or a folder holding either, relative to this file. The entry takes the plugin whose id is `name` from that assembly, and a path that doesn't load is a build error. An entry with a `path` and no `name` loads nothing, and `mokadocs validate` and `mokadocs doctor` warn about it. See [Loading a Plugin from a Path](/plugins/overview#loading-a-plugin-from-a-path).

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

The `nav` section defines the sidebar by hand. When it is present, it replaces the generated sidebar. Without it, MokaDocs builds the sidebar from the docs folder as a tree: every folder is a section, titled by its `index.md`, and the API type pages are grouped by namespace. In that generated sidebar, a section's `index.md` can set `expanded: false` to start the section collapsed. See [Navigation & Sidebar](/configuration/navigation).

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

API type pages live at `/api/{namespace}/{type}`, more than one segment below `/api`, so `path: /api` gets no children and stays a single link to the API index. Add [`autoGenerate: true`](#nav.autogenerate) to list the namespaces and their types.

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

Lists everything below [`path`](#nav.path) as a tree, the way the sidebar is built without a `nav` section: every folder becomes a section (labeled by its `index.md`, or by the folder name), and under `/api` the type pages are grouped by namespace, each namespace starting collapsed. Entries are sorted by front matter `order`, then title. Ignored when the item has `children`.

```yaml
nav:
  - label: "Guides"
    path: /guides
    autoGenerate: true
  - label: "API Reference"
    path: /api
    icon: code
    autoGenerate: true
```

The sidebar renders three levels, counting the item's own. Pages below the third level are listed after their third-level folder, labeled with the folders they are in, such as `Hooks / Events`.

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

The build deletes the output folder first when [`clean`](#clean) is on, and `mokadocs clean` always deletes it. To protect your sources, `mokadocs build`, `serve` and `clean` stop with an error when the output folder is, or contains, the project folder (the one holding `mokadocs.yaml`), the docs folder, the folder of a [`content.projects`](#projects) entry, a custom [`theme.name`](#name) folder, or the source file of [`site.logo`](#logo) or [`site.favicon`](#favicon). `mokadocs validate` and `mokadocs doctor` report the same error. A folder inside the docs folder or inside a project, such as `./docs/_site`, is allowed, and MokaDocs doesn't read it as content.

Every build writes a one-line `.mokadocs-output` file into the output folder. Before deleting an existing output folder that is not empty, the build and `mokadocs clean` check for that file (or for the `_theme/css/main.css` older versions wrote) and stop with an error when it is missing, since MokaDocs did not create that folder. Point `output` at a new folder, or empty the folder yourself once.

Every file is written inside the output folder. A page whose route would land outside it is reported as an error and skipped, and so is a file a plugin asks to write there.

Paths in `mokadocs.yaml` (`content.docs`, `content.projects[].path`, `theme.name`, `build.output`, `site.logo`, `site.favicon`) can use `\` or `/`; MokaDocs reads both as `/`, so the same file works on Windows, Linux and macOS.

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
- **Default:** `false`

When `true`, the build writes smaller files. In HTML pages (`404.html` included), each run of whitespace becomes one character (a line break when the run had one) and comments are removed, except conditional comments such as `<!--[if IE]>`. Tags and the contents of `pre`, `code`, `textarea`, `script` and `style` elements are written as they are, so code blocks keep their layout. The default theme's `main.css` loses its comments and the spaces it doesn't need, and `main.js` its comments and indentation; its line breaks stay. A custom theme's files are copied as they are.

On this repository's documentation it cut the HTML from 8.96 MB to 6.05 MB (1.48 MB to 1.37 MB gzipped), `main.css` from 167 KB to 142 KB and `main.js` from 50 KB to 36 KB. Most hosts compress files anyway, so the saving on the wire is smaller than on disk. The `?v=` hash on the theme's URLs is computed from the files as written, minified or not.

```yaml
build:
  minify: true
```

In the ASP.NET Core host, set `MokaDocsOptions.Minify`.

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

### `strict`

- **Type:** `bool`
- **Default:** `false`

When `true`, warnings fail the build: `mokadocs build` exits 1 and `mokadocs validate` exits 2 when the build reports any warning, such as a broken front matter block, an unknown configuration key or a sidebar entry that matches no page. Errors always fail the build. The `--warnings-as-errors` option of `build` and `validate` does the same for one run, which suits a CI job. `mokadocs serve` ignores this setting.

```yaml
build:
  strict: true
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
| `MOKADOCS_STYLE` | `theme.options.style` |
| `MOKADOCS_SHOW_STYLE_SELECTOR` | `theme.options.styleSelector` |
| `MOKADOCS_LAYOUT_WIDTH` | `theme.options.layout.width` |
| `MOKADOCS_LAYOUT_NAVBAR` | `theme.options.layout.navbar` |
| `MOKADOCS_LAYOUT_FOOTER` | `theme.options.layout.footer` |
| `MOKADOCS_LAYOUT_TOC` | `theme.options.layout.toc` |
| `MOKADOCS_LAYOUT_BREADCRUMBS` | `theme.options.layout.breadcrumbs` |
| `MOKADOCS_LAYOUT_PAGE_NAV` | `theme.options.layout.pageNav` |
| `MOKADOCS_SIDEBAR` | `theme.options.sidebar` |
| `MOKADOCS_SIDEBAR_INLINE_LIMIT` | `theme.options.sidebarInlineLimit` |
| `MOKADOCS_PRIMARY_COLOR` | `theme.options.primaryColor` |
| `MOKADOCS_CODE_THEME` | `theme.options.codeTheme` |
| `MOKADOCS_CODE_STYLE` | `theme.options.codeStyle` |
| `MOKADOCS_SHOW_CODE_THEME_SELECTOR` | `theme.options.codeThemeSelector` |
| `MOKADOCS_SHOW_CODE_STYLE_SELECTOR` | `theme.options.codeStyleSelector` |
| `MOKADOCS_SHOW_COLOR_THEME_SELECTOR` | `theme.options.colorThemes` |
| `MOKADOCS_SHOW_EDIT_LINK` | `theme.options.showEditLink` |
| `MOKADOCS_SHOW_LAST_UPDATED` | `theme.options.showLastUpdated` |
| `MOKADOCS_SHOW_CONTRIBUTORS` | `theme.options.showContributors` |
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
| `MOKADOCS_CLEAN_OUTPUT` | `build.clean` |
| `MOKADOCS_MINIFY_OUTPUT` | `build.minify` |
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
    style: "default"
    styleSelector: true
    layout:
      width: "default"
      navbar: "default"
      footer: "default"
      toc: "default"
      breadcrumbs: "default"
      pageNav: "default"
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
