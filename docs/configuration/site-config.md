---
title: Site Configuration
order: 1
---

# Site Configuration

MokaDocs is configured through a single `mokadocs.yaml` file located at the root of your project. This file controls every aspect of your documentation site, from metadata and theming to build output and plugin registration.

## Configuration File Location

MokaDocs looks for the configuration file in the following order:

1. `mokadocs.yaml` in the current working directory
2. `mokadocs.yml` (alternate extension)
3. A path specified via the `--config` CLI flag

All relative paths within the configuration file are resolved relative to the directory containing the configuration file itself.

---

## `site` Section

The `site` section defines core metadata about your documentation site.

### `title`

- **Type:** `string`
- **Required:** Yes

The title of your documentation site. This value appears in the site header, the browser tab title, and is used as the default `og:title` for pages that do not specify their own title.

```yaml
site:
  title: "My Project Documentation"
```

### `description`

- **Type:** `string`
- **Default:** `""`

A short description of your site used in the HTML `<meta name="description">` tag. This is important for SEO and is displayed in search engine result pages.

```yaml
site:
  description: "Official documentation for My Project, a .NET library for building APIs."
```

### `url`

- **Type:** `string`
- **Default:** `""`

The base URL where your site will be published. This is used to generate absolute URLs in the sitemap, canonical link tags, and Open Graph metadata. Include the protocol but omit the trailing slash.

```yaml
site:
  url: "https://docs.myproject.com"
```

### `logo`

- **Type:** `string` (nullable)
- **Default:** `null`

Path to a logo image file displayed in the site header alongside the title. The path is resolved relative to the configuration file. Supported formats include SVG, PNG, JPG, and WebP.

```yaml
site:
  logo: "./assets/logo.svg"
```

When no logo is provided, only the site title text is shown in the header.

### `favicon`

- **Type:** `string` (nullable)
- **Default:** `null`

Path to a favicon file for the browser tab icon. The path is resolved relative to the configuration file. If omitted, browsers will attempt to load `/favicon.ico` from the site root.

```yaml
site:
  favicon: "./assets/favicon.png"
```

### `copyright`

- **Type:** `string` (nullable)
- **Default:** `null`

A copyright notice displayed in the site footer. You can include the current year using the `{year}` placeholder, which is replaced at build time.

```yaml
site:
  copyright: "Copyright {year} My Company. All rights reserved."
```

### `editLink`

- **Type:** `object` (nullable)
- **Default:** `null`

Configuration for "Edit this page" links that allow readers to propose changes to documentation pages directly on your source repository. When configured, an edit link appears at the bottom of each documentation page.

#### `editLink.repo`

- **Type:** `string`
- **Required:** Yes (when `editLink` is specified)

The full URL of the source repository.

#### `editLink.branch`

- **Type:** `string`
- **Default:** `"main"`

The Git branch name to link to. Change this if your default branch is named differently.

#### `editLink.path`

- **Type:** `string`
- **Default:** `"docs/"`

The path prefix within the repository where documentation files are located. This is prepended to the relative file path of each page when constructing the edit URL.

```yaml
site:
  editLink:
    repo: "https://github.com/myorg/myproject"
    branch: "main"
    path: "docs/"
```

The resulting edit link for a page at `docs/guides/getting-started.md` would be:
`https://github.com/myorg/myproject/edit/main/docs/guides/getting-started.md`

---

## `content` Section

The `content` section defines where MokaDocs finds your documentation sources and C# projects for API reference generation.

### `docs`

- **Type:** `string`
- **Default:** `"./docs"`

The path to the directory containing your Markdown documentation files. MokaDocs recursively scans this directory for `.md` files and builds the site structure from them.

```yaml
content:
  docs: "./docs"
```

### `projects`

- **Type:** `list` of `ProjectSource`
- **Default:** `[]`

A list of C# project definitions for automatic API reference documentation. MokaDocs analyzes these projects using Roslyn to extract types, members, XML documentation comments, and generate structured API pages.

Each entry in the list is a `ProjectSource` object with the following properties:

#### `projects[].path`

- **Type:** `string`
- **Required:** Yes

The path to the `.csproj` file. Resolved relative to the configuration file.

#### `projects[].label`

- **Type:** `string` (nullable)
- **Default:** The project's `AssemblyName` or filename

A human-readable display name for the project, shown in navigation headers and API reference sections.

#### `projects[].includeInternals`

- **Type:** `bool`
- **Default:** `false`

When set to `true`, internal members (those with the `internal` access modifier) are included in the generated API documentation. By default, only `public` and `protected` members are documented.

```yaml
content:
  projects:
    - path: "../src/MyLibrary/MyLibrary.csproj"
      label: "MyLibrary"
      includeInternals: false

    - path: "../src/MyLibrary.Extensions/MyLibrary.Extensions.csproj"
      label: "Extensions"
      includeInternals: true
```

---

## `theme` Section

The `theme` section controls the visual appearance of your documentation site.

### `name`

- **Type:** `string`
- **Default:** `"default"`

The name of the theme to use. MokaDocs ships with a built-in `"default"` theme. Custom themes can be loaded by name if installed as plugins.

```yaml
theme:
  name: "default"
```

### `options`

- **Type:** `ThemeOptions`

A set of options that control the default theme's appearance and behavior. Custom themes may define their own option schemas.

#### `options.primaryColor`

- **Type:** `string`
- **Default:** `"#6366f1"`

The primary brand color used throughout the site for links, active states, sidebar highlights, and interactive elements. Accepts any valid CSS color value (hex, RGB, HSL, or named colors).

```yaml
theme:
  options:
    primaryColor: "#6366f1"
```

#### `options.accentColor`

- **Type:** `string`
- **Default:** `"#f59e0b"`

The accent color used for callout highlights, badges, notification indicators, and other elements that need visual distinction from the primary color.

```yaml
theme:
  options:
    accentColor: "#f59e0b"
```

#### `options.codeTheme`

- **Type:** `string`
- **Default:** `"catppuccin-mocha"`

The syntax highlighting theme applied to fenced code blocks. A `</>` button in the header lets readers switch themes at runtime; this option sets the initial default. When toggling between site dark/light mode, paired themes swap automatically (catppuccin-mocha with catppuccin-latte, github-dark with github-light).

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
- **Default:** `true`

Controls whether the code syntax theme selector button (`</>`) is displayed in the site header. When enabled, readers can switch between all 7 built-in themes. The selection is persisted in `localStorage`.

```yaml
theme:
  options:
    codeThemeSelector: true
```

#### `options.codeStyle`

- **Type:** `string`
- **Default:** `"plain"`

The window frame style applied to fenced code blocks. A window icon button in the header lets readers switch styles at runtime; this option sets the initial default.

| Style Name | Description |
|---|---|
| `plain` | No frame decoration (default) |
| `macos` | macOS-style title bar with traffic light dots |
| `terminal` | Terminal style with a `$` prompt indicator |
| `vscode` | VS Code style with an accent-colored tab bar |

```yaml
theme:
  options:
    codeStyle: "macos"
```

#### `options.codeStyleSelector`

- **Type:** `bool`
- **Default:** `true`

Controls whether the code block window style selector button is displayed in the site header. When enabled, readers can switch between all 4 built-in window styles. The selection is persisted in `localStorage`.

```yaml
theme:
  options:
    codeStyleSelector: true
```

#### `options.colorThemes`

- **Type:** `bool`
- **Default:** `true`

Controls whether the color theme preset selector (palette icon) is displayed in the site header. When enabled, readers can switch between 5 built-in color presets (Ocean, Emerald, Violet, Amber, Rose). The selection is persisted in `localStorage`.

```yaml
theme:
  options:
    colorThemes: true
```

#### `options.showEditLink`

- **Type:** `bool`
- **Default:** `true`

Controls whether the "Edit this page" link is displayed at the bottom of documentation pages. Requires the `site.editLink` section to be configured. When set to `false`, edit links are hidden globally regardless of the `editLink` configuration.

```yaml
theme:
  options:
    showEditLink: true
```

#### `options.showLastUpdated`

- **Type:** `bool`
- **Default:** `true`

Controls whether the "Last updated" timestamp is shown at the bottom of each page. The timestamp is derived from the file's last modification time, or from Git history if the project is inside a Git repository.

```yaml
theme:
  options:
    showLastUpdated: true
```

#### `options.showContributors`

- **Type:** `bool`
- **Default:** `false`

When enabled, displays a list of contributors (from Git history) at the bottom of each documentation page. Each contributor is shown with their name and a link to their profile if available.

```yaml
theme:
  options:
    showContributors: true
```

#### `options.showFeedback`

- **Type:** `bool`
- **Default:** `true`

Controls whether the "Was this page helpful?" feedback widget is displayed at the bottom of each documentation page. When enabled, readers can submit a thumbs-up or thumbs-down vote that is stored in `localStorage`. In dev server mode, votes are also sent as a POST request to `/api/feedback`.

```yaml
theme:
  options:
    showFeedback: true
```

#### `options.showAnimations`

- **Type:** `bool`
- **Default:** `true`

Controls whether UI animations are enabled across the site. When set to `false`, all animations (page transitions, sidebar expand/collapse, landing hero entrance, hover effects) are replaced with instant state changes. MokaDocs also respects the `prefers-reduced-motion` OS setting automatically, suppressing animations for users who have enabled reduced motion regardless of this value.

```yaml
theme:
  options:
    showAnimations: true
```

#### `options.showBuiltWith`

- **Type:** `boolean`
- **Default:** `true`

Show the "Built with MokaDocs v{version}" branding in the site footer. The version is automatically read from the installed MokaDocs package. Set to `false` to hide the branding entirely.

```yaml
theme:
  options:
    showBuiltWith: true
```

#### `options.socialLinks`

- **Type:** `list` of social link objects
- **Default:** `[]`

A list of social/external links displayed in the site header. Each entry specifies an icon and a URL.

##### `socialLinks[].icon`

- **Type:** `string`
- **Required:** Yes

The icon identifier to display. Supported icon names:

| Icon | Description |
|---|---|
| `github` | GitHub |
| `discord` | Discord |
| `twitter` | Twitter / X |
| `npm` | npm |
| `nuget` | NuGet |
| `mastodon` | Mastodon |
| `linkedin` | LinkedIn |
| `youtube` | YouTube |
| `slack` | Slack |
| `facebook` | Facebook |
| `instagram` | Instagram |
| `reddit` | Reddit |
| `stackoverflow` | Stack Overflow |

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

The `features` section enables and configures optional site features.

### `search`

Configuration for the built-in search functionality.

#### `search.enabled`

- **Type:** `bool`
- **Default:** `true`

Enables or disables the search feature entirely. When disabled, the search bar is removed from the header.

#### `search.provider`

- **Type:** `string`
- **Default:** `"pagefind"`

The search provider to use. MokaDocs supports two providers:

| Provider | Description |
|---|---|
| `pagefind` | Static search index generated at build time. No server required. Fast and lightweight. Recommended for most sites. |
| `flexsearch` | In-memory JavaScript-based search. The full index is loaded into the browser. Better for small sites where instant results are preferred over index size. |

```yaml
features:
  search:
    enabled: true
    provider: "pagefind"
```

### `versioning`

Configuration for multi-version documentation support.

#### `versioning.enabled`

- **Type:** `bool`
- **Default:** `false`

Enables multi-version documentation. When enabled, a version selector appears in the site header.

#### `versioning.strategy`

- **Type:** `string`
- **Default:** `"directory"`

The strategy used for organizing versioned content:

| Strategy | Description |
|---|---|
| `"directory"` | Each version is built as a separate subdirectory (e.g., `/v1.0/`, `/v2.0/`). All versions are deployed simultaneously. |
| `"dropdown-only"` | A dropdown selector links to different deployments or branches. Only one version is built per invocation. |

#### `versioning.versions`

- **Type:** `list` of version definitions
- **Default:** `[]`

The list of available documentation versions.

##### `versions[].label`

- **Type:** `string`
- **Required:** Yes

The display label for this version, shown in the version selector dropdown (e.g., `"v2.0"`, `"v1.5 LTS"`, `"Latest"`).

##### `versions[].branch`

- **Type:** `string` (nullable)
- **Default:** `null`

The Git branch associated with this version. Used with the `"dropdown-only"` strategy to link to different branches, and during CI builds to determine which version is being built.

##### `versions[].default`

- **Type:** `bool`
- **Default:** `false`

Marks this version as the default. The default version is shown when a user visits the site root without specifying a version. Exactly one version should be marked as default.

##### `versions[].prerelease`

- **Type:** `bool`
- **Default:** `false`

Marks this version as a prerelease. Prerelease versions are shown with a visual indicator in the version selector and can optionally be excluded from search indexing.

```yaml
features:
  versioning:
    enabled: true
    strategy: "directory"
    versions:
      - label: "v3.0"
        branch: "main"
        default: true
        prerelease: false
      - label: "v3.1-beta"
        branch: "release/3.1"
        prerelease: true
      - label: "v2.0"
        branch: "release/2.0"
      - label: "v1.0"
        branch: "release/1.0"
```

---

## `plugins` Section

The `plugins` section declares a list of plugins to load. Each entry is a `PluginDeclaration`.

### Plugin Resolution

Plugins can be loaded in two ways:

1. **By name** - Resolved as a NuGet package ID. MokaDocs will look for the package in configured NuGet sources.
2. **By path** - A direct file path to a plugin DLL. Resolved relative to the configuration file.

You must specify either `name` or `path`, but not both.

### `plugins[].name`

- **Type:** `string` (nullable)
- **Default:** `null`

The NuGet package name of the plugin.

### `plugins[].path`

- **Type:** `string` (nullable)
- **Default:** `null`

A file path to a local plugin DLL. Resolved relative to the configuration file.

### `plugins[].options`

- **Type:** `dict` (string keys, arbitrary values)
- **Default:** `{}`

A dictionary of plugin-specific configuration options. Each plugin defines its own supported options. Refer to the plugin's documentation for available keys and value types.

```yaml
plugins:
  - name: "MokaDocs.Plugin.Mermaid"
    options:
      theme: "dark"

  - name: "MokaDocs.Plugin.Analytics"
    options:
      trackingId: "G-XXXXXXXXXX"
      anonymizeIp: true

  - path: "./plugins/MyCustomPlugin.dll"
    options:
      customSetting: "value"
```

---

## `nav` Section

The `nav` section lets you manually define the navigation sidebar structure instead of (or in addition to) relying on automatic generation from the directory layout. Each entry is a `NavItem`.

When the `nav` section is present, it takes precedence over auto-generated navigation. If omitted, navigation is generated automatically from the `content.docs` directory structure.

### `nav[].label`

- **Type:** `string`
- **Required:** Yes

The display text for this navigation item in the sidebar.

### `nav[].path`

- **Type:** `string` (nullable)
- **Default:** `null`

The URL path this item links to. For pages, this is the route (e.g., `"/guides/getting-started"`). Section headers that serve as group labels typically omit this field.

### `nav[].icon`

- **Type:** `string` (nullable)
- **Default:** `null`

A Lucide icon name displayed next to the label in the sidebar. See the Navigation & Sidebar page for a list of commonly used icons.

### `nav[].expanded`

- **Type:** `bool`
- **Default:** `false`

Whether this section is expanded by default when the page loads. Only applies to items that have `children`.

### `nav[].autoGenerate`

- **Type:** `bool`
- **Default:** `false`

When set to `true`, MokaDocs automatically populates the `children` of this nav item from API analysis or directory scanning. This is commonly used for API reference sections where you want the sidebar to reflect the project's namespace structure.

### `nav[].children`

- **Type:** `list` of `NavItem`
- **Default:** `[]`

Nested child navigation items. Supports arbitrary depth.

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

  - label: "Guides"
    icon: "book-open"
    expanded: true
    children:
      - label: "Writing Content"
        path: "/guides/writing-content"
      - label: "Custom Themes"
        path: "/guides/custom-themes"

  - label: "API Reference"
    icon: "code"
    autoGenerate: true

  - label: "Changelog"
    path: "/changelog"
    icon: "history"
```

---

## `build` Section

The `build` section controls how the final output is generated.

### `output`

- **Type:** `string`
- **Default:** `"./_site"`

The directory where the built site is written. Resolved relative to the configuration file. This is the directory you deploy to your hosting provider.

```yaml
build:
  output: "./_site"
```

### `basePath`

- **Type:** `string`
- **Default:** `""` (empty — site is served from root)

A path prefix added to all generated routes, asset links, and navigation URLs. Use this when deploying to a subdirectory, such as GitHub Pages project sites (`/repo-name`) or IIS virtual directories.

```yaml
build:
  basePath: /my-project
```

This can also be set via the `--base-path` CLI flag, which takes precedence over the config value.

### `clean`

- **Type:** `bool`
- **Default:** `true`

When `true`, the output directory is completely emptied before each build. This prevents stale files from previous builds from remaining in the output. Set to `false` if you have other processes that write to the output directory and you want to preserve those files.

```yaml
build:
  clean: true
```

### `minify`

- **Type:** `bool`
- **Default:** `true`

When `true`, the HTML, CSS, and JavaScript output is minified to reduce file sizes and improve load times. Disable during development if you need to inspect the raw output.

```yaml
build:
  minify: true
```

### `sitemap`

- **Type:** `bool`
- **Default:** `true`

When `true`, a `sitemap.xml` file is generated in the output directory containing URLs for all public pages. The sitemap uses the `site.url` value as the base URL. If `site.url` is empty, the sitemap is not generated regardless of this setting.

```yaml
build:
  sitemap: true
```

### `robots`

- **Type:** `bool`
- **Default:** `true`

When `true`, a `robots.txt` file is generated in the output directory. The generated file allows all crawlers and references the sitemap URL (if sitemap generation is also enabled).

```yaml
build:
  robots: true
```

### `cache`

- **Type:** `bool`
- **Default:** `true`

When `true`, MokaDocs caches intermediate build results (such as parsed Markdown ASTs and Roslyn analysis output) to speed up incremental rebuilds. The cache is stored in a `.mokadocs/cache` directory. Disable this if you experience stale content issues during development.

```yaml
build:
  cache: true
```

---

## Complete Example

The following example demonstrates every available configuration option:

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
      label: "Contoso SDK"
      includeInternals: false
    - path: "../src/Contoso.Sdk.Extensions/Contoso.Sdk.Extensions.csproj"
      label: "Extensions"

theme:
  name: "default"
  options:
    primaryColor: "#0ea5e9"
    accentColor: "#f59e0b"
    codeTheme: "catppuccin-mocha"
    codeThemeSelector: true
    codeStyle: "plain"
    codeStyleSelector: true
    colorThemes: true
    showEditLink: true
    showLastUpdated: true
    showContributors: true
    showFeedback: true
    showAnimations: true
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
    provider: "pagefind"
  versioning:
    enabled: true
    strategy: "directory"
    versions:
      - label: "v3.0"
        branch: "main"
        default: true
      - label: "v3.1-beta"
        branch: "release/3.1"
        prerelease: true
      - label: "v2.0"
        branch: "release/2.0"
      - label: "v1.0"
        branch: "release/1.0"

plugins:
  - name: "MokaDocs.Plugin.Mermaid"
    options:
      theme: "dark"
  - name: "MokaDocs.Plugin.Analytics"
    options:
      trackingId: "G-XXXXXXXXXX"
      anonymizeIp: true
  - path: "./plugins/MyCustomPlugin.dll"
    options:
      enableFeatureX: true

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
    icon: "book-open"
    children:
      - label: "Writing Content"
        path: "/guides/writing-content"
      - label: "Theming"
        path: "/guides/theming"
      - label: "Deployment"
        path: "/guides/deployment"
  - label: "API Reference"
    icon: "code"
    autoGenerate: true
  - label: "Changelog"
    path: "/changelog"
    icon: "history"

build:
  output: "./_site"
  basePath: ""
  clean: true
  minify: true
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

With this minimal configuration, MokaDocs uses all default values: it looks for Markdown files in `./docs`, uses the default theme with default colors, enables search with Pagefind, and outputs to `./_site`.

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

- All relative paths in `mokadocs.yaml` are resolved relative to the directory containing the configuration file.
- The `content.docs` path points to the root of your Markdown documentation tree.
- The `content.projects[].path` values point to `.csproj` files and can traverse up the directory tree using `../`.
- The `build.output` path is where the generated site is written.
- The `plugins[].path` values point to plugin DLL files.
- The `site.logo` and `site.favicon` paths point to asset files that are copied to the output during build.

If you run MokaDocs from a different directory than where the config file lives, use the `--config` flag to specify the config file path. All relative paths within the config are still resolved relative to the config file location, not the current working directory.
