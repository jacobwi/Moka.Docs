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

When `url` is set, both layouts write the canonical link plus Open Graph and Twitter tags on every page. The landing layout uses the site title for `og:title` and `og:type` `website`; the default layout uses the page title and `article`. Add an [`image`](#image) for link previews with a picture.

In the ASP.NET Core host, set `MokaDocsOptions.SiteUrl` (see [Link Previews](/guide/aspnetcore#link-previews)).

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

**Collision detection**: if two of `site.logo`, `site.favicon` and
`site.image` flatten to the same publish URL (same filename from different
source directories), reading the configuration fails with an error. Rename or
move one of them.

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

### `image`

- **Type:** `string` (nullable) - filesystem path or absolute URL
- **Default:** `null`
- **Environment variable:** `MOKADOCS_SITE_IMAGE`

The picture link previews show (chat apps, social networks) for pages that don't set their own [`image`](/configuration/front-matter#image) in front matter. It becomes `og:image` and `twitter:image`, and switches `twitter:card` from `summary` to `summary_large_image`. A common size is 1200 by 630 pixels.

```yaml
site:
  url: "https://user.github.io/Repo"
  image: assets/social-card.png
```

The path rules of [`logo`](#logo) apply: a local file is copied into the site, `../` paths flatten to `/_media/`, and a full URL is used as written. The tags need absolute URLs, so a local image is written under [`url`](#url) with the base path once (`https://user.github.io/Repo/assets/social-card.png` above). Without `url` there are no preview tags at all. A protocol-relative URL gets `https:`, and a `data:` URI is ignored. A missing local file is a build warning, and pages get no image.

In the ASP.NET Core host, set `MokaDocsOptions.SocialImage`, a file under `DocsPath` or a full URL, together with `SiteUrl` (see [Link Previews](/guide/aspnetcore#link-previews)).

### `copyright`

- **Type:** `string` (nullable)
- **Default:** `null`

A notice shown in the site footer. `{year}` is replaced with the current year when the site is built.

```yaml
site:
  copyright: "Copyright {year} My Company. All rights reserved."
```

### `language`

- **Type:** `string`, a BCP 47 language tag
- **Default:** `"en"`
- **Environment variable:** `MOKADOCS_SITE_LANGUAGE`

The language of the pages, such as `de`, `ja` or `pt-BR`: `<html lang>` on every page, the theme's built-in translation (Spanish, French, German, Brazilian Portuguese, Japanese and simplified Chinese), plural forms, how dates are written and `og:locale`. A value that isn't a language tag is a build warning, and the site is built in English. See [Languages](/guide/languages).

```yaml
site:
  language: de
```

### `direction`

- **Type:** `string`, `ltr` or `rtl`
- **Default:** the language's own direction
- **Environment variable:** `MOKADOCS_SITE_DIRECTION`

`<html dir>` of every page. Arabic, Hebrew, Persian, Urdu and a few more languages are right to left without it; right-to-left pages load a stylesheet that mirrors the layout (see [Languages](/guide/languages#site.direction)). Any other value is a build warning.

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

Files with these extensions are copied to the output at the same relative path: `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.webp`, `.avif`, `.ico`, `.pdf`, `.zip`, `.mp4`, `.webm`, `.css`, `.js`, `.json`, `.xml`, `.txt`, `.webmanifest`, `.woff`, `.woff2`, `.ttf` and `.eot`. The files `CNAME`, `_redirects` and `_headers` are copied too when they sit directly in the docs folder. Nothing inside the [`build.output`](#output) folder is read, and neither is any other folder below the docs folder that holds a `.mokadocs-output` file: that is a site MokaDocs built, such as an old `./docs/_site` left behind after `build.output` moved elsewhere. The build warns once for each such folder it skips.

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

#### `options.preset`

- **Type:** `string`
- **Default:** none
- **Values:** `reference`, `terminal`, `book`, `product`, `minimal`, `accessible`, `moka`

A named bundle of options applied in one line: a [style](#options.style), [layout](#options.layout) variants and, for some presets, a [`defaultColorTheme`](#options.defaultcolortheme) and a [`codeTheme`](#options.codetheme). The preset fills in only the options your `theme.options` leaves out, so anything you set yourself wins, one key at a time: setting `layout.footer` keeps the preset's other layout parts.

```yaml
theme:
  options:
    preset: reference
    layout:
      footer: minimal    # wins over the preset's columns footer
```

| Preset | Sets |
|---|---|
| `reference` | `style: classic`, `layout.navbar: tabs`, `layout.footer: columns`, `layout.breadcrumbs: chevrons`, `layout.tables: striped` |
| `terminal` | `style: terminal`, `defaultColorTheme: moka-red`, `codeTheme: tokyo-night`, `layout.navbar: compact`, `layout.breadcrumbs: path`, `layout.footer: minimal`, `layout.pageNav: minimal`, `layout.density: compact` |
| `book` | `style: paper`, `layout.toc: inline`, `layout.pageNav: minimal`, `layout.footer: minimal`, `layout.breadcrumbs: back`, `layout.font: serif` |
| `product` | `style: soft`, `layout.navbar: floating`, `layout.footer: columns`, `layout.hero: split`, `layout.heroBackground: gradient`, `layout.admonitions: filled` |
| `minimal` | `style: minimal`, `layout.navbar: autohide`, `layout.toc: minimal`, `layout.footer: minimal`, `layout.breadcrumbs: back`, `layout.pageNav: minimal`, `layout.tables: minimal` |
| `accessible` | `style: contrast`, `layout.font: system`, `layout.density: spacious` |
| `moka` | `style: moka`, `defaultColorTheme: moka-red`, `codeTheme: catppuccin-mocha`, `layout.navbar: compact`, `layout.density: compact` |

The name is matched without regard to case. An unknown name applies nothing, and the build reports a warning (`mokadocs validate` lists it), for example `Theme preset 'refrence' is not one of reference, terminal, book, product, minimal, accessible, moka; no preset applied`. `MOKADOCS_THEME_PRESET` picks a preset from the [environment](#environment-variables). The variable for a single option, such as `MOKADOCS_STYLE`, still wins over the preset's value. The preset sets what pages start with: readers can still pick another style, color preset or code theme in the header. See [Theme Presets](/themes/customization#theme-presets).

#### `options.style`

- **Type:** `string`
- **Default:** `"default"`
- **Values:** `default`, `moka`, `minimal`, `classic`, `soft`, `terminal`, `paper`, `contrast`, `glass`, `brutal`

The whole look of the default theme: surfaces, borders, corner radii, shadows, fonts and spacing. It applies on top of the color preset, the code theme and the code frame, in light and dark mode. When [`styleSelector`](#options.styleselector) is on, readers can pick another style in the Appearance panel.

| Style | Look |
|---|---|
| `default` | The original look |
| `moka` | Dense and dark first: near-black surfaces, borders tinted with the primary color, glow rings instead of shadows, uppercase micro-labels, monospace for technical text. Starts in dark mode until the reader uses the dark mode toggle |
| `minimal` | Few borders and shadows, more whitespace, a reading column about 70 characters wide, plain sidebar links |
| `classic` | Solid header bar, bordered sidebar and panels, small radii, dense striped tables, the system font |
| `soft` | Rounded cards with soft shadows, tinted sections, pill-shaped highlights, roomier spacing |
| `terminal` | Monospace and dense: square corners, `$` and `#` before headings, the current page in reverse video, an underline on the focused control. Starts in dark mode like `moka` |
| `paper` | A book page: system serif text, a narrow column, warm paper colors, small capitals and thin rules instead of boxes |
| `contrast` | Accessibility first: 7:1 body text and links, 4.5:1 for the rest, underlined links, thick focus rings, no state shown by color alone, no transparency |
| `glass` | Translucent, blurred header, sidebar, cards and menus over a soft gradient; solid where blur is missing or the reader asks for less transparency |
| `brutal` | Thick solid lines, flat fills, square corners, heavy headings and hard offset shadows |

The name is matched without regard to case. An unknown name renders as `default`, and the build reports a warning (`mokadocs validate` lists it). See [Styles](/themes/customization#styles).

```yaml
theme:
  options:
    style: "moka"
```

#### `options.styleSelector`

- **Type:** `bool`
- **Default:** `true`

Shows the Style section of the Appearance panel, the header button where readers change the site's look (see [Appearance Panel](/themes/customization#appearance-panel)), for picking one of the 10 styles. The choice is saved in `localStorage` and applied before the page is drawn. With `false`, every page uses [`style`](#options.style), including for readers who picked another style earlier.

```yaml
theme:
  options:
    styleSelector: false
```

#### `options.textSizeSelector`

- **Type:** `bool`
- **Default:** `true`

Shows the Text size section of the Appearance panel: 90%, 100%, 112.5% or 125%. It scales the root font size, so all of the text follows. The choice is saved in `localStorage` and applied before the page is drawn. With `false`, every page uses the reader's browser font size, including for readers who picked a size earlier.

```yaml
theme:
  options:
    textSizeSelector: false
```

#### `options.widthSelector`

- **Type:** `bool`
- **Default:** `true`

Shows the Reading width section of the Appearance panel: Narrow (about 60 characters), Default (the style's own width), Wide (1040px), Wider (1280px) or Full (no limit: the column, the header and the footer fill the window). Where the column wouldn't fit between the sidebar and "On this page", the page grows with it, up to 1680px for Wide and 1920px for Wider, and a pick never makes the column narrower than [`layout.width`](#layout.width) does, except Narrow. [Reading width and layout.width](/themes/customization#reading-width-and-layout.width) has the result of each pair. The choice is saved in `localStorage` and applied before the page is drawn, and only while the section is shown.

```yaml
theme:
  options:
    widthSelector: false
```

#### `options.layoutSelector`

- **Type:** `bool`
- **Default:** `false`

Adds a Layout section to the Appearance panel, where readers switch the [layout](#options.layout) variant of each part of a docs page: `navbar`, `footer`, `toc`, `breadcrumbs`, `pageNav`, `sidebar`, `sidebarScope`, `admonitions`, `tables`, `density`, `font`, `headings` and `links`. `width` is left to the Reading width section, and `hero` and `heroBackground` to you. Your variants are the defaults, marked "(default)" in each list. A pick applies at once, is saved in `localStorage` as `mokadocs-layout-<part>` (`mokadocs-layout-page-nav` for `pageNav`) and is applied before later pages are drawn. A saved name the theme doesn't know is ignored, and so are all saved picks once the option is off again. Reset to site defaults removes them.

```yaml
theme:
  options:
    layoutSelector: true
```

Pages then carry the markup of every variant (the tabs row, the link columns footer, the inline "On this page" box and the progress bar, Top link and counter of the panel) and the stylesheet shows only what the current variants use. On the MokaDocs site that adds 4 to 9 KB to a page (1 to 2 KB compressed), most of it the footer's link columns and a second copy of the "On this page" list, and every page links `_theme/css/layouts.css`. `MOKADOCS_SHOW_LAYOUT_SELECTOR` sets it for one build.

With every section of the panel off (`showDarkModeToggle`, `styleSelector`, `colorThemes`, `codeThemeSelector`, `codeStyleSelector`, `textSizeSelector`, `widthSelector` and `layoutSelector`), the header has no Appearance button.

#### `options.layout`

- **Type:** object with the keys below
- **Default:** `default` for every part

Picks a layout variant for each part of the page: the width, the top bar, the footer, the "On this page" panel, the breadcrumbs, the previous/next links, the sidebar's look and what it lists, the landing page as a whole, its hero, the hero's backdrop and its feature cards, admonitions, tables, spacing, the typeface, section headings and links. Each part is set on its own and works with every [style](#options.style), color preset and code theme, in light and dark mode. A part you leave out keeps its `default` variant, the theme's original layout.

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
      sidebar: lines
      sidebarScope: section
      home: showcase
      hero: split
      heroBackground: gradient
      features: bento
      admonitions: github
      tables: striped
      density: compact
      font: system
      headings: accent
      links: underlined
```

Only the default theme reads these settings. Every reader gets the layout you set, unless [`layoutSelector`](#options.layoutselector) lets them switch the parts of a docs page in the Appearance panel. The landing layout (`layout: landing` in front matter) uses the `home`, `navbar`, `footer`, `hero`, `heroBackground`, `features`, `tables`, `links`, `width` and `font` settings too, and with `home: docs` the sidebar settings as well.

Names are matched without regard to case. An unknown name renders that part as `default`, and the build reports a warning (`mokadocs validate` lists it), for example `Layout navbar 'tbs' is not one of default, compact, floating, tabs, autohide, centered, transparent; using 'default'`. Each part also has an [environment variable](#environment-variables), such as `MOKADOCS_LAYOUT_NAVBAR`.

##### `layout.width`

| Value | Layout |
|---|---|
| `default` | The sidebar, a reading column of about 780px (the style can change it) and the "On this page" panel, up to 1440px wide |
| `wide` | A 1040px reading column, up to 1680px wide |
| `wider` | A 1280px reading column, up to 1920px wide |
| `full` | The page fills the window and the reading column has no cap |
| `boxed` | The page, its top bar and its footer sit in a 1280px framed column on a tinted background. Narrower windows show it without the frame |
| `narrow` | A 640px reading column, centered between the sidebar and the "On this page" panel, which keep their width |

A reader who picks a [reading width](#options.widthselector) in the Appearance panel gets that width instead.

##### `layout.navbar`

| Value | Layout |
|---|---|
| `default` | A sticky bar |
| `compact` | A shorter bar with smaller controls |
| `floating` | A rounded bar inset from the edges |
| `tabs` | Adds a second row with a tab for each top-level sidebar entry, the current one underlined. On a phone the row scrolls sideways |
| `autohide` | Slides away while scrolling down, and comes back on scrolling up or when it gets keyboard focus |
| `centered` | The logo in the middle, the search button on the left and the other buttons on the right. On a phone it's laid out like `default` |
| `transparent` | No border, and the page shows through the bar, blurred, as it scrolls underneath. The bar keeps enough of the page color for its text to stay readable over any content. Readers who ask their system for less transparency, and browsers without blur, get a solid bar |

##### `layout.footer`

| Value | Layout |
|---|---|
| `default` | One row: copyright, social links and "Built with" |
| `minimal` | One centered line |
| `columns` | The site name and description next to a column of links for each top-level sidebar section (up to four columns of six links), with the copyright and "Built with" underneath. Top-level pages with nothing under them share one more column after the sections, titled "More" (or "Docs" when there are no sections) |
| `none` | No footer |
| `centered` | Everything stacked and centered: the social links as a row of icons, then the copyright and "Built with" |
| `compact` | One small line with the parts separated by dots |

##### `layout.toc`

| Value | Layout |
|---|---|
| `default` | The right-hand column with reading progress, a section counter and a Top link |
| `minimal` | The right-hand column with the links only |
| `inline` | A collapsible "On this page" box under the page title, in place of the right-hand column |
| `floating` | A button in the corner opens the list over the page, at every screen width |
| `numbered` | The right-hand column with each entry numbered: 1, 1.1, 1.1.1. Pairs with [`headings: numbered`](#layout.headings) |
| `timeline` | The entries as dots on a vertical line, the section in view filled in |
| `bordered` | The right-hand column as a bordered card |

[`showTableOfContents: false`](#options.showtableofcontents) and `toc: false` in a page's front matter still turn it off.

##### `layout.breadcrumbs`

| Value | Layout |
|---|---|
| `default` | A slash-separated trail |
| `chevrons` | Chevron separators, with the current page in bold |
| `pills` | Each step in a rounded chip |
| `path` | A monospace path, like a terminal prompt |
| `back` | Only a "back to parent" link |
| `arrows` | Arrow separators, with the current page in bold |
| `dots` | Dot separators and lighter links |

[`showBreadcrumbs: false`](#options.showbreadcrumbs) still hides them. See [`showBreadcrumbs`](#options.showbreadcrumbs) for what the trail lists.

##### `layout.pageNav`

| Value | Layout |
|---|---|
| `default` | Two bordered cards |
| `minimal` | Plain links, each under a thin divider |
| `stacked` | Each link a full-width row, previous above next |
| `split` | One bar between two rules, previous on the left and next on the right, without cards |
| `compact` | Small links on one line, such as "Previous: Install" and "Next: Usage" |

[`showPrevNext: false`](#options.showprevnext) still hides them. Every variant sits under the article and lines up with it. On a phone the two links stack, except with `split` and `compact`, which keep them side by side.

##### `layout.sidebar`

| Value | Layout |
|---|---|
| `default` | Rows with a tinted current page and a bar on its left |
| `lines` | Guide lines along each nested level, the current page marked on the line |
| `pills` | Roomier rows, the current page a filled pill in the accent color |
| `compact` | Denser rows in smaller type, for long sidebars |
| `accent` | A bar in the accent color on the current page's left edge, and no fill |
| `cards` | Each top-level section in a card. Top-level pages without entries below them stay plain rows |
| `minimal` | Plain text without icons or fills, the current page in bold and underlined |

##### `layout.sidebarScope`

| Value | Layout |
|---|---|
| `default` | The sidebar lists every section |
| `section` | The sidebar lists only the top-level section the current page is in. A page outside every section, such as a top-level page, still gets all of them |

`section` pairs with [`navbar: tabs`](#layout.navbar): the tabs switch between sections, and the sidebar lists the current one. It works the same with [`sidebar: shared`](#options.sidebar), and readers without JavaScript see the same sidebar.

##### `layout.home`

The page with `layout: landing` as a whole. [Home page](/themes/customization#home-page) has an example of each.

| Value | Layout |
|---|---|
| `default` | The hero, the cards from the page's [`features`](/configuration/front-matter#features), then the page content |
| `portal` | A compact hero with a large search box (it opens the search dialog, like `Ctrl K`), then a card for each top-level sidebar section with its icon and first five pages, then the content. No feature cards and no buttons. Without [search](#search.enabled) there is no search box |
| `showcase` | The hero and the feature cards, then three optional bands from front matter: a code sample beside a heading and text ([`showcaseCode`](/configuration/front-matter#showcasecode), `showcaseTitle`, `showcaseText`), a row of figures ([`stats`](/configuration/front-matter#stats)) and a call to action in the accent color ([`cta`](/configuration/front-matter#cta)), then the content. Without that front matter it looks like `default` |
| `minimal` | The site title, the description, the two buttons and the content: no icon, no feature cards and no hero backdrop |
| `docs` | No hero: the page looks like a docs page, with the sidebar, the site title and description as the page header, the feature cards as link cards, then the content |

`hero` still shapes the hero of `portal`, `showcase` and `minimal` (`minimal` and `portal` leave out the `heroCode` or `heroImage` of `hero: split`), and `heroBackground` applies to `portal` and `showcase`. `features` applies to `default` and `showcase`. `docs` uses the [`sidebar`](#layout.sidebar), [`sidebarScope`](#layout.sidebarscope) and [`sidebarToggle`](#options.sidebartoggle) settings like any other page.

##### `layout.hero`

The hero is the top of a page with `layout: landing`: the site title, the page's `description` and the buttons.

| Value | Layout |
|---|---|
| `default` | Everything centered, under the site logo or a book icon |
| `split` | The text and buttons on the left, a code sample or an image on the right (stacked on narrow screens). The sample comes from the page's [`heroCode`](/configuration/front-matter) front matter, highlighted at build time in the [`heroCodeLanguage`](/configuration/front-matter); the image from `heroImage`. With neither, the hero stays centered |
| `minimal` | Left aligned, no icon, a smaller title and no backdrop |
| `banner` | A full-width band in the accent color, with the text and buttons in the color that reads on it. It has no backdrop, so `heroBackground` doesn't apply |

```markdown
---
title: Home
layout: landing
description: Docs for the Acme SDK.
heroCodeLanguage: csharp
heroCode: |
  var client = new AcmeClient(apiKey);
  var order = await client.Orders.GetAsync(42);
---
```

##### `layout.heroBackground`

| Value | Layout |
|---|---|
| `default` | The style's own backdrop: a soft tint with a faint dot grid in the default style |
| `dots` | A dot grid that fades toward the edges |
| `gradient` | Soft glows in the color preset's colors |
| `none` | The plain page background |
| `mesh` | Soft, blurred patches of the color preset's colors toward the edges |
| `lines` | Faint diagonal lines that fade toward the edges |

This setting wins over `hero: minimal`, which otherwise has no backdrop. `hero: banner` and `home: minimal` have none whatever this says, and `home: docs` has no hero at all.

##### `layout.features`

The cards from a landing page's [`features`](/configuration/front-matter#features) front matter.

| Value | Layout |
|---|---|
| `default` | Cards, three to a row |
| `list` | The icon beside the text, in two columns with a rule between rows and no card boxes. One column on phones |
| `icons` | Large round icons above centered text, without card boxes |
| `compact` | Small, dense cards, four to a row |
| `bento` | A grid of mixed sizes: the first card takes two columns and two rows, and the last one stretches to fill its row |

A card with a `link` stays a link in every variant. `home: portal`, `minimal` and `docs` don't show this grid.

##### `layout.admonitions`

| Value | Layout |
|---|---|
| `default` | A tinted box with a colored left border |
| `filled` | A tinted box under a solid title strip |
| `minimal` | A colored left bar with the title in small uppercase letters, no box and no icon |
| `github` | Like GitHub's alerts: an icon and the title in the type's color over a left border |
| `outlined` | A border in the type's color all around, and no fill |
| `callout` | A larger box: the icon in a tinted circle beside the title, the text under the title |

##### `layout.tables`

| Value | Layout |
|---|---|
| `default` | Ruled cells with a shaded header row |
| `striped` | Every other row shaded |
| `bordered` | A rounded frame with a rule around every cell, as wide as the table's content |
| `minimal` | No frame and no vertical lines: a rule under the header only |
| `compact` | Denser rows in smaller type |
| `rounded` | A rounded frame as wide as the table's content, rules between rows and a header row tinted with the accent color |

##### `layout.density`

Spacing in the page content, the sidebar and the "On this page" panel.

| Value | Layout |
|---|---|
| `default` | The theme's spacing |
| `compact` | Tighter paragraphs, headings and rows |
| `spacious` | More room between paragraphs, headings and rows, and taller lines |

##### `layout.font`

The typeface of text and headings. Code stays monospace, and no variant downloads a font: each uses fonts already on the reader's device.

| Value | Layout |
|---|---|
| `default` | The style's own font (Inter when it's installed, else the system font, in the default style) |
| `system` | The operating system's interface font |
| `serif` | A book serif such as Iowan Old Style, Charter or Georgia |
| `mono` | The style's monospace font |
| `rounded` | A rounded sans serif: SF Pro Rounded on Apple devices, else Quicksand, Comfortaa or Arial Rounded when installed, else Calibri or the system sans serif |
| `humanist` | A humanist sans serif: Seravek, Gill Sans Nova, Ubuntu, Calibri or DejaVu Sans, whichever the device has |

##### `layout.headings`

Section headings in the page content. The page title isn't changed.

| Value | Layout |
|---|---|
| `default` | The style's own headings |
| `underlined` | A full-width rule under each h2 |
| `accent` | A short bar in the accent color before each h2 and h3 |
| `numbered` | Each h2 and h3 numbered: 1, 1.1, 1.2, 2. Pairs with [`toc: numbered`](#layout.toc), which numbers the "On this page" entries the same way |

##### `layout.links`

Links in the page content. Navigation, breadcrumbs and buttons keep their own look.

| Value | Layout |
|---|---|
| `default` | The style's links: the accent color, underlined on hover in most styles |
| `underlined` | Always underlined |
| `subtle` | The color of the text around them with a dotted underline, and a solid underline in the accent color on hover and keyboard focus |

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

#### `options.sidebarToggle`

- **Type:** `bool`
- **Default:** `false`

Adds a button at the top left of the content that hides and shows the sidebar on wide screens, giving the page the whole width. The reader's choice is kept in `localStorage` and applied on later pages before they are drawn. Phones keep their menu button, and readers without JavaScript don't get the button.

```yaml
theme:
  options:
    sidebarToggle: true
```

#### `options.primaryColor`

- **Type:** `string`
- **Default:** `"#0ea5e9"`

The main brand color, used for links, the current navigation item and hover states. Any CSS color value works. MokaDocs sets the `--color-primary` CSS variable to it and derives lighter and darker shades with `color-mix()`.

Text and buttons don't use the color as written. When the site is built, MokaDocs works out a text shade for light mode and one for dark mode (`--color-primary-text`): the same hue, darkened or lightened just far enough for a 4.5:1 contrast ratio on the code background, the current sidebar link and the admonition boxes. A color that already passes is kept. This covers hex, `rgb()`, `hsl()` and named colors. Any other syntax, such as `oklch()` or `var()`, is used for text as written, so check its contrast yourself. See [Primary Color](/themes/customization#primary-color).

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

The syntax highlighting colors for code blocks. When [`codeThemeSelector`](#options.codethemeselector) is on, readers can switch themes in the Appearance panel. Switching between light and dark mode swaps a theme that has a light or dark counterpart for it; the others stay as they are.

| Theme Name | Background | Dark mode toggle swaps it with |
|---|---|---|
| `catppuccin-mocha` | Dark (default) | `catppuccin-latte` |
| `catppuccin-latte` | Light | `catppuccin-mocha` |
| `github-dark` | Dark | `github-light` |
| `github-light` | Light | `github-dark` |
| `dracula` | Dark | |
| `one-dark` | Dark | |
| `nord` | Dark | |
| `tokyo-night` | Dark | |
| `gruvbox-dark` | Dark | `gruvbox-light` |
| `gruvbox-light` | Light | `gruvbox-dark` |
| `solarized-dark` | Dark | `solarized-light` |
| `solarized-light` | Light | `solarized-dark` |
| `rose-pine` | Dark | `rose-pine-dawn` |
| `rose-pine-dawn` | Light | `rose-pine` |
| `night-owl` | Dark | |
| `monokai` | Dark | |

The name is matched without regard to case. An unknown name renders as `catppuccin-mocha`, and the build reports a warning (`mokadocs validate` lists it). A custom theme gets the name as written.

```yaml
theme:
  options:
    codeTheme: "github-dark"
```

#### `options.codeThemeSelector`

- **Type:** `bool`
- **Default:** `false`

Shows the Code theme section of the Appearance panel for picking one of the 16 code themes. The choice is saved in `localStorage`. A saved choice applies only while the section is shown: with `false`, every page uses [`codeTheme`](#options.codetheme), including for readers who picked another theme earlier.

```yaml
theme:
  options:
    codeThemeSelector: true
```

#### `options.codeStyle`

- **Type:** `string`
- **Default:** `"plain"`

The window frame drawn around code blocks. When [`codeStyleSelector`](#options.codestyleselector) is on, readers can switch frames in the Appearance panel.

| Style Name | Description |
|---|---|
| `plain` | No frame (default) |
| `macos` | Title bar with three colored dots |
| `terminal` | Terminal bar with a `$` prompt |
| `vscode` | Tab bar in the primary color |

The name is matched without regard to case. An unknown name renders as `plain`, and the build reports a warning.

```yaml
theme:
  options:
    codeStyle: "macos"
```

#### `options.codeStyleSelector`

- **Type:** `bool`
- **Default:** `false`

Shows the Code style section of the Appearance panel for picking one of the 4 code styles. The choice is saved in `localStorage` and applies only while the section is shown.

```yaml
theme:
  options:
    codeStyleSelector: true
```

#### `options.colorThemes`

- **Type:** `bool`
- **Default:** `true`

Shows the Color section of the Appearance panel, with 11 color presets: Ocean, Emerald, Violet, Amber, Rose, Moka Red, Slate, Teal, Indigo, Orange and Graphite. The reader's pick is saved in `localStorage` and applies only while the section is shown. Hiding the section doesn't stop [`defaultColorTheme`](#options.defaultcolortheme) from applying.

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

Shows "Last updated:" and the date, written out in the [site's language](#language), at the bottom of Markdown pages that use the default layout. When the docs folder is inside a git repository and `git` is on `PATH`, the date is that of the last commit that changed the file. A file with uncommitted changes, or one git doesn't track, uses its modified time on disk instead, and so does every file when git isn't available. The same date goes into the sitemap's `<lastmod>`.

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

Shows the sun/moon button in the header (on screens wider than 768px) and the Mode section of the Appearance panel (Light, Dark, System). Without them, pages follow the reader's operating system color scheme, or a choice saved during an earlier visit.

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

Shows the "On this page" sidebar on pages that use the default layout. A page can also turn it off with `toc: false` in its front matter, and a page with fewer than two entries (counting headings down to [`tocDepth`](#options.tocdepth)) has none.

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

The trail starts at Home and follows the sidebar: each section above the page, with its sidebar label, then the page title. A section without a page of its own links to its first page. A page the sidebar doesn't list gets one step per folder of its URL instead, named after the sidebar section that holds that folder, else after the folder (`getting-started` becomes "Getting started").

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

Adds line numbers to every code block with three or more lines. A block can override it with `showLineNumbers` or `noLineNumbers` after its language (see [Code Block Options](/guide/markdown#code-block-options)).

```yaml
theme:
  options:
    showLineNumbers: true
```

#### `options.imageZoom`

- **Type:** `bool`
- **Default:** `true`

Clicking an image in the page content, or pressing Enter on it, opens it full size over the page. Any click or Escape closes it again. Images inside links and images smaller than 96 by 64 pixels are left alone, and so is an image with a `data-no-zoom` attribute. Works on the default and landing layouts.

```yaml
theme:
  options:
    imageZoom: false
```

#### `options.tocDepth`

- **Type:** `int`
- **Default:** `3`
- **Range:** `2` to `6` (other values are clamped)

The deepest heading level listed in the "On this page" sidebar. With `3`, headings down to `h3` are listed; with `6`, every level.

The page's own title isn't in the list: an `h1` at the top of the page, or a first heading with the same text as the page title. The `h2` headings are the top entries, and deeper headings nest under them.

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
- **Values:** `ocean`, `emerald`, `violet`, `amber`, `rose`, `moka-red`, `slate`, `teal`, `indigo`, `orange`, `graphite`, or `""` for no preset

The color preset a page starts with until the reader picks one. There is one exception: when [`primaryColor`](#options.primarycolor) differs from its default and this stays `ocean`, pages start with no preset so `primaryColor` shows. Any other value applies that preset, which hides a custom `primaryColor`. The preset applies even when [`colorThemes`](#options.colorthemes) hides the palette button. The name is matched without regard to case; an unknown name counts as `ocean`, and the build reports a warning.

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

#### `options.strings`

- **Type:** `map` of key to text
- **Default:** empty

Your own wording for any text of the theme, by key, such as `toc.title` or `search.placeholder`. It wins over English and the built-in translation of [`site.language`](#language). A key the theme doesn't have is a build warning. [Languages](/guide/languages#text-keys) lists every key.

```yaml
theme:
  options:
    strings:
      toc.title: "Contents"
      feedback.prompt: "Did this page help?"
```

#### `options.banner`

- **Type:** object with the keys below
- **Default:** none

A one-line announcement above the header of every page, for a release, a deprecation or an event. It scrolls away with the page while the header stays at the top. Only the default theme shows it. See [Announcement Banner](/themes/customization#announcement-banner).

```yaml
theme:
  options:
    banner:
      text: "Version 2.0 is out"
      link: /blog/2-0
      linkText: "Read the release notes"
      variant: accent
```

##### `banner.text`

- **Type:** `string`
- **Required:** Yes

The announcement, as plain text. HTML in it shows as written instead of being rendered. Without text there is no banner. `MOKADOCS_BANNER_TEXT` sets it from the environment, which shows a banner even when `mokadocs.yaml` has none.

##### `banner.link`

- **Type:** `string`
- **Default:** none

Where the link after the text goes: an absolute URL, or a path from the site root such as `/blog/2-0`, which gets the [`basePath`](#basepath). Without it there is no link. `MOKADOCS_BANNER_LINK` sets it from the environment.

##### `banner.linkText`

- **Type:** `string`
- **Default:** `"Learn more"`

The link's text.

##### `banner.id`

- **Type:** `string`
- **Default:** a hash of `text` and `link`

What a reader's dismissal is remembered by. The dismiss button saves the id in the reader's `localStorage`, and pages with the same id hide the banner before they are drawn. A new id shows the banner again to readers who dismissed the old one. Without an id, changing the text or the link does the same; set one to keep a banner dismissed while you fix a typo in it.

##### `banner.dismissible`

- **Type:** `bool`
- **Default:** `true`

Shows a close button at the end of the banner. With `false` the banner stays on every page.

##### `banner.variant`

- **Type:** `string`
- **Default:** `"info"`
- **Values:** `info`, `warning`, `accent`

The look: `info` is a light tint of the primary color, `warning` an amber tint with a warning icon, and `accent` a solid bar in the primary color. Each one meets WCAG AA contrast in every style, with every color preset, in light and dark mode. The name is matched without regard to case; an unknown name renders as `info`, and the build reports a warning.

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

## `glossary` Section

- **Type:** map of term to definition
- **Default:** empty

Terms and their definitions. `:term[...]` in a page shows a term's definition in a tooltip, and a `:::glossary` block lists all of them (see [Glossary](/guide/components#glossary)).

```yaml
glossary:
  API: The types and members a library makes public.
  Front matter: The YAML block at the top of a Markdown page, between two --- lines.
```

A definition is plain text; write `` `code` `` for code. Terms match without regard to case. A term with no definition is a warning and is left out.

The same map can live in a `glossary.yml` (or `glossary.yaml`) file at the root of the docs folder, with or without the `glossary:` key around it. The build reads both, and a term in both keeps its `mokadocs.yaml` definition, with a warning. The file isn't published. In the ASP.NET Core host, set `MokaDocsOptions.Glossary`, or put the file in `DocsPath`.

---

## `build` Section

The `build` section controls how the final output is generated.

### `output`

- **Type:** `string`
- **Default:** `"./_site"`

The folder the site is written to, and the one you deploy. The `--output` option of `mokadocs build` and `mokadocs serve` overrides it, and a relative `--output` value also resolves against the configuration file's folder.

The build deletes the output folder first when [`clean`](#clean) is on, and `mokadocs clean` always deletes it. To protect your sources, `mokadocs build`, `serve` and `clean` stop with an error when the output folder is, or contains, the project folder (the one holding `mokadocs.yaml`), the docs folder, the folder of a [`content.projects`](#projects) entry, a custom [`theme.name`](#name) folder, or the source file of [`site.logo`](#logo), [`site.favicon`](#favicon) or [`site.image`](#image). `mokadocs validate` and `mokadocs doctor` report the same error. A folder inside the docs folder or inside a project, such as `./docs/_site`, is allowed, and MokaDocs doesn't read it as content.

Every build writes a one-line `.mokadocs-output` file into the output folder. Before deleting an existing output folder that is not empty, the build and `mokadocs clean` check for that file (or for the `_theme/css/main.css` older versions wrote) and stop with an error when it is missing, since MokaDocs did not create that folder. Point `output` at a new folder, or empty the folder yourself once.

Every file is written inside the output folder. A page whose route would land outside it is reported as an error and skipped, and so is a file a plugin asks to write there.

Paths in `mokadocs.yaml` (`content.docs`, `content.projects[].path`, `theme.name`, `build.output`, `site.logo`, `site.favicon`, `site.image`) can use `\` or `/`; MokaDocs reads both as `/`, so the same file works on Windows, Linux and macOS.

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
- **Environment variable:** `MOKADOCS_MINIFY_OUTPUT`

When `true`, the build minifies the HTML pages (`404.html` included): each run of whitespace becomes one character (a line break when the run had one) and comments are removed, except conditional comments such as `<!--[if IE]>`. Tags and the contents of `pre`, `code`, `textarea`, `script` and `style` elements are written as they are, so code blocks keep their layout. It also minifies the theme's CSS and JavaScript, whatever [`minifyAssets`](#minifyassets) says.

The default layouts carry no indentation of their own, so the HTML shrinks little: on this repository's documentation from 12.11 MB to 12.08 MB. Most hosts compress files anyway, so the saving on the wire is smaller than on disk.

```yaml
build:
  minify: true
```

In the ASP.NET Core host, set `MokaDocsOptions.Minify`.

### `minifyAssets`

- **Type:** `bool`
- **Default:** on for `mokadocs build`, off for `mokadocs serve`
- **Environment variable:** `MOKADOCS_MINIFY_ASSETS`

Minifies the default theme's stylesheets and scripts: `main.css`, the style stylesheets (`style-<name>.css`), `layouts.css`, `rtl.css`, `main.js` and the shared sidebar script (`sidebar.js`, see [`sidebar`](#options.sidebar)). The stylesheets lose their comments and the spaces they don't need. The scripts lose their comments and indentation, and keep their line breaks. Strings, `url(...)` values, regular expressions and template literals are written as they are. `icons.svg` is written as it is.

`mokadocs build` minifies these files unless you set `minifyAssets: false`. `mokadocs serve` keeps them readable for debugging in the browser, unless `minifyAssets: true` (or the environment variable) is set explicitly, or [`minify`](#minify) is `true`. Pages are the same either way: only the files and their `?v=` hashes change, since the hash is computed from the bytes written.

A [custom theme's](/themes/customization#choosing-a-theme) own CSS and JavaScript are copied as you wrote them, minified or not: MokaDocs doesn't change files it didn't write. Minify them in your own build step if you want them smaller.

On this repository's documentation:

| File | Readable | Minified | Readable, gzipped | Minified, gzipped |
|---|---:|---:|---:|---:|
| `main.css` | 157 KB | 121 KB | 30 KB | 21 KB |
| `main.js` | 100 KB | 63 KB | 25 KB | 17 KB |
| `layouts.css` | 72 KB | 62 KB | 12 KB | 9.6 KB |
| `style-moka.css` | 42 KB | 37 KB | 6.5 KB | 5.6 KB |
| `rtl.css` | 40 KB | 37 KB | 5.3 KB | 4.8 KB |

The other style stylesheets lose about 12% raw and 15% gzipped. A page with the default style downloads `main.css` and `main.js`: 38 KB gzipped instead of 55 KB.

```yaml
build:
  minifyAssets: false
```

In the ASP.NET Core host, set `MokaDocsOptions.MinifyAssets`. Its default minifies outside the Development environment and keeps the files readable in Development.

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

### `llms`

- **Type:** `bool`
- **Default:** `true`
- **Environment variable:** `MOKADOCS_LLMS`

When `true`, the build writes files for language models and for readers who want a page as text:

- A Markdown copy of each public page next to its HTML: `/guide/intro` gets `/guide/intro.md`, and the home page `/index.md`. The copy is the page's Markdown without its front matter, under an `# Title` heading when the page doesn't start with one. Markdown links and images that point into the site become absolute (under [`site.url`](#url) when it is set, else with the base path), and MokaDocs blocks such as `::: tip` and `:::steps` stay as written. [`xref:` links](/guide/components#api-cross-references) that resolve point at the API page, and `<xref:...>` becomes a link named after the type. C# API type pages and the API index get a Markdown version of their content. Other generated pages (OpenAPI, Python API, blog listings) get no copy.
- `llms.txt` at the site root, in the [llmstxt.org](https://llmstxt.org) format: the site title, the description, then one section per top-level sidebar entry listing its pages as `- [Title](url): description`. Top-level pages with nothing under them share a "Docs" section (first when it holds the home page), and public pages the sidebar doesn't list go under "Optional". Links point at the Markdown copies, or at the HTML page when there is none.
- `llms-full.txt`: every Markdown copy in one file, in the order `llms.txt` lists them.

Draft and hidden pages, and pages a `version` or `requires` gate leaves out, get nothing. A `llms.txt` or `llms-full.txt` in the docs folder is published as written instead of the generated one. The default theme shows a "Copy page" button and a "View as Markdown" link above each page that has a copy.

```yaml
build:
  llms: true
```

In the ASP.NET Core host, set `MokaDocsOptions.Llms`.

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
| `MOKADOCS_THEME_PRESET` | `theme.options.preset` |
| `MOKADOCS_STYLE` | `theme.options.style` |
| `MOKADOCS_SHOW_STYLE_SELECTOR` | `theme.options.styleSelector` |
| `MOKADOCS_SHOW_TEXT_SIZE_SELECTOR` | `theme.options.textSizeSelector` |
| `MOKADOCS_SHOW_WIDTH_SELECTOR` | `theme.options.widthSelector` |
| `MOKADOCS_SHOW_LAYOUT_SELECTOR` | `theme.options.layoutSelector` |
| `MOKADOCS_LAYOUT_WIDTH` | `theme.options.layout.width` |
| `MOKADOCS_LAYOUT_NAVBAR` | `theme.options.layout.navbar` |
| `MOKADOCS_LAYOUT_FOOTER` | `theme.options.layout.footer` |
| `MOKADOCS_LAYOUT_TOC` | `theme.options.layout.toc` |
| `MOKADOCS_LAYOUT_BREADCRUMBS` | `theme.options.layout.breadcrumbs` |
| `MOKADOCS_LAYOUT_PAGE_NAV` | `theme.options.layout.pageNav` |
| `MOKADOCS_LAYOUT_SIDEBAR` | `theme.options.layout.sidebar` |
| `MOKADOCS_LAYOUT_SIDEBAR_SCOPE` | `theme.options.layout.sidebarScope` |
| `MOKADOCS_LAYOUT_HOME` | `theme.options.layout.home` |
| `MOKADOCS_LAYOUT_HERO` | `theme.options.layout.hero` |
| `MOKADOCS_LAYOUT_HERO_BACKGROUND` | `theme.options.layout.heroBackground` |
| `MOKADOCS_LAYOUT_FEATURES` | `theme.options.layout.features` |
| `MOKADOCS_LAYOUT_ADMONITIONS` | `theme.options.layout.admonitions` |
| `MOKADOCS_LAYOUT_TABLES` | `theme.options.layout.tables` |
| `MOKADOCS_LAYOUT_DENSITY` | `theme.options.layout.density` |
| `MOKADOCS_LAYOUT_FONT` | `theme.options.layout.font` |
| `MOKADOCS_LAYOUT_HEADINGS` | `theme.options.layout.headings` |
| `MOKADOCS_LAYOUT_LINKS` | `theme.options.layout.links` |
| `MOKADOCS_SIDEBAR` | `theme.options.sidebar` |
| `MOKADOCS_SIDEBAR_INLINE_LIMIT` | `theme.options.sidebarInlineLimit` |
| `MOKADOCS_SIDEBAR_TOGGLE` | `theme.options.sidebarToggle` |
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
| `MOKADOCS_IMAGE_ZOOM` | `theme.options.imageZoom` |
| `MOKADOCS_SHOW_VERSION_SELECTOR` | `theme.options.showVersionSelector` |
| `MOKADOCS_BANNER_TEXT` | `theme.options.banner.text` |
| `MOKADOCS_BANNER_LINK` | `theme.options.banner.link` |
| `MOKADOCS_SEARCH_ENABLED` | `features.search.enabled` |
| `MOKADOCS_CLEAN_OUTPUT` | `build.clean` |
| `MOKADOCS_MINIFY_OUTPUT` | `build.minify` |
| `MOKADOCS_MINIFY_ASSETS` | `build.minifyAssets` |
| `MOKADOCS_GENERATE_SITEMAP` | `build.sitemap` |
| `MOKADOCS_GENERATE_ROBOTS` | `build.robots` |
| `MOKADOCS_LLMS` | `build.llms` |
| `MOKADOCS_SITE_IMAGE` | `site.image` |
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
  image: "./assets/social-card.png"
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
    preset: "reference"    # fills in only what this block leaves out
    style: "default"
    styleSelector: true
    textSizeSelector: true
    widthSelector: true
    layoutSelector: false
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
    imageZoom: true
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
    banner:
      text: "Contoso SDK 3.1 is in beta"
      link: "/blog/3-1-beta"
      linkText: "Try it"
      dismissible: true
      variant: "info"

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
  llms: true
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
