---
title: Build Pipeline & Architecture
order: 2
---

# Build Pipeline & Architecture

MokaDocs builds a site by running a fixed sequence of phases over one shared `BuildContext`. This page covers the phases, the context and models they pass along, the variables layout templates receive, and how build problems are reported.

## Pipeline Overview

`BuildPipeline` runs every registered `IBuildPhase` in ascending `Order`:

```csharp
public interface IBuildPhase
{
    string Name { get; }
    int Order { get; }
    Task ExecuteAsync(BuildContext context, CancellationToken ct = default);
}
```

Phases run one after another and read and write the same `BuildContext`. A phase that hits a problem records a [diagnostic](#diagnostics) and lets the build continue. The exception is a `build.output` that is, or contains, the project folder, the docs folder, a `content.projects` folder, a custom theme folder or a logo or favicon source, or a non-empty folder MokaDocs did not create: the build stops before deleting anything.

The pipeline also has a plugin hook. It runs once, just before the first phase with an order of 500 or higher.

## Build Phases

| Order | Phase | What it does |
|-------|-------|--------------|
| 200 | `DiscoveryPhase` | Lists Markdown files and static assets under `content.docs`, skipping anything inside `build.output` and any folder holding a `.mokadocs-output` file (a site MokaDocs built), with a warning for each such folder. Checks which `content.projects` files exist and resolves `site.logo`, `site.favicon` and `site.image` |
| 300 | `CSharpAnalysisPhase` | Reads the `.cs` files of each configured project with Roslyn, using the settings and references from its project files, or loads the result from the build cache. Adds the `/api` index page and one page per type, and reads the package name and version from the first project that produces a package |
| 400 | `MarkdownParsePhase` | Turns each Markdown file into a `DocPage`: front matter, HTML, table of contents and route |
| 450 | `VersionGatePhase` | Removes pages whose front matter `version` range doesn't include the version being built. Does nothing without versioning |
| | Plugin hook | Runs the plugins declared under `plugins:` in `mokadocs.yaml` |
| 500 | `FeatureGatePhase` | Removes pages whose `requires` feature flag is disabled |
| 550 | `BlogPhase` | With `features.blog.enabled`: adds a header to each post, generates the blog's index, tags and tag pages, and stages its feeds for `OutputPhase`. See [Blog](/guide/blog) |
| 600 | `NavigationBuildPhase` | Builds the sidebar tree from the `nav` section, or from page routes when there is no `nav` |
| 650 | `BlogNavigationPhase` | Lists the latest posts under the blog's sidebar entry |
| 700 | `SearchIndexPhase` | Creates a search entry for each page and for each heading in its table of contents, with the text and code words under that heading. Skipped when `features.search.enabled` is false |
| 800 | `LlmsPhase` | With `build.llms` (the default) and outside a dry run: stages a Markdown copy of each public page, `llms.txt` and `llms-full.txt` for `OutputPhase` |
| 900 | `RenderPhase` | Renders each page through its layout template, and `404.html` through the default layout. Theme file URLs and the search index URL get a `?v=` hash of the bytes the later phases write |
| 1100 | `OutputPhase` | Checks the output directory and duplicate routes, deletes the output directory when `build.clean` is on, then writes pages, section redirect pages, `.nojekyll`, `search-index.json`, static assets and the files the render phase and plugins staged (`404.html` among them). With `build.minify`, pages and `404.html` are minified |
| 1150 | `ThemeAssetPhase` | Writes the default theme's files: `_theme/css/main.css`, a `_theme/css/style-<name>.css` for each style other than `default`, `_theme/css/layouts.css`, `_theme/icons.svg` and `_theme/js/main.js` (the stylesheets and script minified unless `build.minifyAssets` is `false`; `mokadocs serve` writes them readable). With a custom theme it copies the theme's `css/`, `js/` and `assets/` folders into `_theme/` instead |
| 1200 | `PostProcessPhase` | Writes `sitemap.xml` (only when `site.url` is set) and `robots.txt` |

`--verbose` logs each phase by its `Name` as it starts and how long it took when it ends: `Discovery`, `CSharpAnalysis`, `MarkdownParse`, `FeatureGate`, `Blog`, `NavigationBuild`, `BlogNavigation`, `SearchIndex`, `Llms`, `Render`, `Output`, `ThemeAssets` and `PostProcess`. A last line lists every phase with its time, the plugin hook as `Plugins`. `BuildPipeline.LastTimings` holds the same numbers for a host that wants them.

`MarkdownParsePhase` parses files in parallel, and `OutputPhase` writes pages in parallel when the build runs on the real disk. Pages and diagnostics are still added in discovery order, and pages sharing a route still leave the last one written, so the output is the same as a sequential build.

A few details the table leaves out:

- Plugins run before `FeatureGatePhase`, so pages a plugin adds go through feature gating, navigation, search and rendering like any other page. Only plugins declared in the configuration run. See [Plugin System](/plugins/overview).
- Pages with `visibility: draft` are skipped by `SearchIndexPhase`, `RenderPhase` and `OutputPhase` unless the build runs with `--draft`.
- In a dry run (`mokadocs validate` and `mokadocs doctor`), `OutputPhase`, `ThemeAssetPhase` and `PostProcessPhase` return before writing anything. `OutputPhase` still reports an unsafe output directory and duplicate routes first.
- `ApiPageRenderer` is not a phase. It's a static helper that `CSharpAnalysisPhase` calls to build the HTML of each type page.
- The [ASP.NET Core integration](/guide/aspnetcore) registers one more phase, `ReflectionApiPagePhase` at order 350. It generates API pages from the model the host builds by reflection over loaded assemblies, when `CSharpAnalysisPhase` produced none.

## BuildContext

`BuildContext` holds the state every phase reads and writes. The host that runs the build (the CLI or the ASP.NET Core integration) creates it and sets the `required` members; phases fill in the rest. Members grouped by purpose:

```csharp
public sealed class BuildContext
{
    // Set by the host
    public required SiteConfig Config { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required string RootDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public bool DryRun { get; init; }
    public bool UseCache { get; init; } = true;
    public bool IncludeDrafts { get; init; }

    // Content
    public List<DocPage> Pages { get; } = [];
    public ApiReference? ApiModel { get; set; }
    public PackageMetadata? PackageInfo { get; set; }

    // Navigation and search
    public NavigationTree? Navigation { get; set; }
    public SearchIndex? SearchIndex { get; set; }

    // Versioning
    public DocVersion? CurrentVersion { get; set; }
    public List<DocVersion> Versions { get; } = [];

    // Diagnostics
    public DiagnosticBag Diagnostics { get; } = new();

    // Discovery results
    public List<string> DiscoveredMarkdownFiles { get; } = [];
    public List<string> DiscoveredProjectFiles { get; } = [];
    public List<string> DiscoveredAssetFiles { get; } = [];
    public Dictionary<string, string> BrandAssetFiles { get; } = new(StringComparer.Ordinal);

    // Extra output registered by plugins
    public Dictionary<string, byte[]> DeferredOutputFiles { get; } = new();
    public List<(string SourceDir, string DestRelPath)> DeferredOutputDirectories { get; } = [];
}
```

### Property Details

| Property | Set by | Notes |
|----------|--------|-------|
| `Config` | Host | The parsed `mokadocs.yaml`, with CLI overrides such as `--output` and `--base-path` applied |
| `FileSystem` | Host | A `System.IO.Abstractions` file system. The CLI passes the real disk and the ASP.NET Core host passes an in-memory `MockFileSystem`, so phases must use it instead of `System.IO` |
| `RootDirectory` | Host | The folder containing `mokadocs.yaml`. Relative paths in the configuration resolve against it |
| `OutputDirectory` | Host | The absolute `build.output` path |
| `DryRun` | Host | True for `mokadocs validate` and `mokadocs doctor`. Phases that write output return early |
| `UseCache` | Host | False with `--no-cache` or `build.cache: false`. Controls whether cached C# analysis is reused |
| `IncludeDrafts` | Host | True with `--draft` |
| `Pages` | `CSharpAnalysisPhase`, `MarkdownParsePhase`, plugins | `FeatureGatePhase` removes pages. `RenderPhase` replaces each page's `Content.Html` with the fully rendered document |
| `ApiModel` | `CSharpAnalysisPhase` | All analyzed projects merged by namespace, with `<inheritdoc/>` resolved. The ASP.NET Core host sets it before the build starts |
| `PackageInfo` | `CSharpAnalysisPhase` | `Name` and `Version` from the first packable project's `.csproj`, shown by the NuGet install widget on `/api` |
| `Navigation` | `NavigationBuildPhase` | The sidebar tree. `BlogNavigationPhase` fills in the blog's entry |
| `Blog` | `BlogPhase` | The blog's listed posts (newest first), tags, generated page routes and feeds. Null when the blog is off |
| `SearchIndex` | `SearchIndexPhase` | Null when search is disabled |
| `CurrentVersion`, `Versions` | Host | The CLI fills them from `features.versioning` when versioning is enabled (`VersionManager.ApplyTo`), or per version in a [versioned build](/advanced/versioning#building-several-versions) |
| `Diagnostics` | Any phase or plugin | See [Diagnostics](#diagnostics) |
| `DiscoveredMarkdownFiles` | `DiscoveryPhase` | Paths relative to `content.docs` |
| `DiscoveredProjectFiles` | `DiscoveryPhase` | Absolute paths of the `content.projects` files that exist |
| `DiscoveredAssetFiles` | `DiscoveryPhase` | Paths relative to `content.docs` |
| `BrandAssetFiles` | `DiscoveryPhase` | Logo and favicon files to copy. The key is the publish URL (such as `/assets/logo.png`), the value the absolute source path. A local logo or favicon whose file is missing isn't listed, gets a warning, and renders with an empty `site.logo_url` or `site.favicon_url` |
| `DeferredOutputFiles` | Plugins | File contents keyed by output-relative path. `OutputPhase` writes them after cleaning the output directory |
| `DeferredOutputDirectories` | Plugins | Folders that `OutputPhase` copies into the output directory |

## Key Models

### DocPage

A page, whether it came from Markdown, C# analysis or a plugin.

```csharp
public sealed record DocPage
{
    public required FrontMatter FrontMatter { get; init; }
    public required PageContent Content { get; set; }
    public TableOfContents TableOfContents { get; init; } = TableOfContents.Empty;
    public string? SourcePath { get; init; }        // relative to content.docs, null for generated pages
    public required string Route { get; init; }     // e.g. "/guide/getting-started"
    public PageOrigin Origin { get; init; } = PageOrigin.Markdown;   // Markdown or ApiGenerated
    public DateTimeOffset? LastModified { get; init; }
}

public sealed record FrontMatter
{
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public int Order { get; init; }
    public string? Icon { get; init; }
    public string Layout { get; init; } = "default";
    public List<string> Tags { get; init; } = [];
    public PageVisibility Visibility { get; init; } = PageVisibility.Public;   // Public, Hidden, Draft
    public bool Toc { get; init; } = true;
    public bool Expanded { get; init; } = true;
    public string? Route { get; init; }
    public string? Version { get; init; }
    public string? Requires { get; init; }
    public List<LandingFeature> Features { get; init; } = [];   // cards for the landing layout
    public string FeaturesTitle { get; init; } = "";
    public string FeaturesSubtitle { get; init; } = "";
}

public sealed record LandingFeature
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? Icon { get; init; }   // icon name, or text shown as is
    public string? Link { get; init; }
}
```

- `FrontMatter` has only the properties above. Front matter keys MokaDocs doesn't recognize are left out with a warning, and there is no collection of custom fields.
- `PageContent` has `Html` and `PlainText`. After `MarkdownParsePhase`, `Html` is the page body; after `RenderPhase` it's the whole HTML document. `PlainText` feeds the search index.
- `TableOfContents.Entries` is a list of `TocEntry` records with `Level`, `Text`, `Id` and `Children`.
- `Route` comes from the file path (`guide/intro.md` becomes `/guide/intro`, and `index.md` takes its folder's route) unless front matter sets `route`. Only the trailing `.md` is removed, in any case. A `route` value always gets a leading slash and loses any trailing slash, and one that could not be written as a folder (`..`, `?`, `#`, characters Windows forbids) is an error and is ignored. `OutputPhase` writes each page to `{route}/index.html`, and refuses any path that would land outside the output directory.
- Front matter that isn't valid YAML, or has a value of the wrong type, is ignored with a warning that names the line and column. The page is then titled `Untitled`. Unknown front matter keys are warnings too.
- A Markdown file that can't be read or parsed is an error naming the file and the cause, and the page is left out.
- `LastModified` is the date of the last git commit that changed the Markdown file, read with one `git log` for the whole docs folder. It falls back to the file's last write time on disk for uncommitted or untracked files, when git isn't available, and for builds over a non-physical file system such as the ASP.NET Core host's. Generated pages have none.
- `Contributors` lists the authors of the file's commits, most commits first, from the same `git log` (with `.mailmap` applied, one entry per email). It's read only when `theme.options.showContributors` is on, because it needs the whole history rather than each file's newest commit.

### NavigationNode

An item in the sidebar tree. `NavigationTree.Items` holds the top-level nodes.

```csharp
public sealed record NavigationNode
{
    public required string Label { get; init; }
    public string? Route { get; init; }       // null for a nav group with no path
    public string? Icon { get; init; }        // icon name
    public int Order { get; init; }
    public bool Expanded { get; init; } = true;
    public bool IsActive { get; init; }
    public List<NavigationNode> Children { get; init; } = [];
}
```

The tree is built once per build, not once per page, so the engine never sets `IsActive`. The template engine works out the current page while rendering and exposes it as `is_active` and `has_active_child` on each `nav` item. It also turns `Icon` into SVG markup. An icon name that isn't in MokaDocs' icon set renders nothing, and `NavigationBuildPhase` records a warning for it. See [Navigation](/configuration/navigation) for how the tree is built.

With a [shared sidebar](/configuration/navigation#large-sidebars-inline-or-shared), `RenderPhase` renders the sidebar once with no current page, through `ScribanTemplateEngine.RenderSidebar`, and stages it as `_theme/js/sidebar.js` in `DeferredOutputFiles`. Pages then get a `nav` with only the top-level entries and the entries along their own path, and its URL as `sidebar_script`.

### SearchEntry

An entry in the search index.

```csharp
public sealed record SearchEntry
{
    public required string Title { get; init; }      // page title
    public string? Section { get; init; }            // heading text, on heading entries
    public required string Route { get; init; }      // "/guide/intro" or "/guide/intro#setup"
    public required string Content { get; init; }    // page plain text, or the text under the heading
    public string? Code { get; init; }               // words from code blocks, null when there are none
    public string Category { get; init; } = "Documentation";   // "Documentation" or "API Reference"
    public List<string> Tags { get; init; } = [];    // front matter tags, on page entries
}
```

Each page gets one entry, plus one entry per heading with an anchor route (one per member on an API type page). `SearchIndexPhase` splits the page body at its headings to give each heading entry the text and code words of its section. `OutputPhase` writes the index to `search-index.json` with short keys (`t`, `s`, `r`, `c`, `x`, `g`, `k`), adds the base path to each route and keeps the first 300 characters of `Content`. `RenderPhase` serializes the same bytes first, to put their hash in `search_index_url`. See [Search](/guide/search).

### ApiType

A C# type from the analysis phase. `ApiReference.Namespaces` holds `ApiNamespace` records, and each namespace holds its `Types`.

```csharp
public sealed record ApiType
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required ApiTypeKind Kind { get; init; }   // Class, Struct, Record, Interface, Enum, Delegate
    public ApiAccessibility Accessibility { get; init; } = ApiAccessibility.Public;
    public bool IsStatic { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsSealed { get; init; }
    public bool IsRecord { get; init; }
    public List<ApiTypeParameter> TypeParameters { get; init; } = [];
    public string? BaseType { get; init; }
    public List<string> ImplementedInterfaces { get; init; } = [];
    public List<ApiMember> Members { get; init; } = [];
    public XmlDocBlock? Documentation { get; init; }
    public List<ApiAttribute> Attributes { get; init; } = [];
    public string? Namespace { get; init; }
    public string? Assembly { get; init; }
    public string? SourcePath { get; init; }
    public bool IsObsolete { get; init; }
    public string? ObsoleteMessage { get; init; }
    public string? SourceCode { get; init; }
}
```

- `ApiMember` describes a constructor, method, property, field, event, operator or indexer, with its `Signature`, `Parameters` and `Documentation`.
- `XmlDocBlock` holds the parsed doc comment: `Summary`, `Remarks`, `Parameters`, `TypeParameters`, `Returns`, `Value`, `Exceptions`, `Examples` and `SeeAlso`.
- `ApiTypeParameter` has a `Name` and its `Constraints`. `ApiAttribute` has a `Name` and its `Arguments`.

## Template Engine

Layouts are [Scriban](https://github.com/scriban/scriban) templates: `{{ page.title }}` expressions, `{{ if }}` and `{{ for }}` blocks. `RenderPhase` picks the layout named by the page's `layout` front matter and `ScribanTemplateEngine` renders it with the variables below. Parsed templates are cached by layout name for the life of the process.

When the theme has no layout with that name, the page uses `default` instead, and `RenderPhase` records one warning per missing layout name. `RenderPhase` also records a warning when `theme.name` points at a folder that doesn't exist or has no `layouts/*.html`, in which case the default theme is used.

When a layout has a syntax error or throws while rendering, the page is written without the layout, and `RenderPhase` records an error: one per distinct message, with the number of pages it affected and an example route.

Scriban's own loop and string limits are off, so a sidebar with thousands of entries and a page of several megabytes render in full. A layout that is still rendering one page after 30 seconds, or writes more than 64 million characters for it, is stopped with that error instead, which catches a template loop that never ends.

### Template Variables

| Variable | Contents |
|----------|----------|
| `page` | The page being rendered (fields below) |
| `site` | Values from `site:` in `mokadocs.yaml` (fields below) |
| `theme` | Values from `theme.options` (fields below) |
| `nav` | Sidebar items with `label`, `route`, `icon` (SVG markup; with the default theme an `<svg>` around a `<use>` of the icon sprite), `expanded`, `is_active`, `has_active_child`, `has_children`, `has_page` and `children`. With a shared sidebar, only the top-level items and the items along the path to the page |
| `sidebar_script` | URL of the shared sidebar script, base path and `?v=` hash included, or an empty string when the sidebar is inline |
| `breadcrumbs` | Trail items with `label`, `url`, `is_current` and `has_page`, starting with Home. The sections above the page in the sidebar, with their sidebar labels, or the page's route folders when the sidebar doesn't list it. Every item has a `url`: a section without a page of its own (`has_page` false) links to its first page |
| `prev_page`, `next_page` | `title` and `route` of the previous and next page, when there is one. The order follows `nav` depth first, then the public pages it doesn't list by `order` and route. Only pages with the default layout take part. On a blog post they are the next newer and older post, and the blog's generated pages have neither |
| `blog` | `enabled`, `title`, `route` (base path included) and `feeds`, each with `format` (`rss` or `atom`), `type` (the media type), `title` and `url` (absolute). Empty values without a blog; `feeds` is empty without `site.url` |
| `home_sections` | On a `landing` page with `theme.options.layout.home: portal`, one item per top-level sidebar entry that leads to a page: `title`, `url` (its own page, else the first page below it), `icon` (SVG markup, or empty when the entry has no bundled icon) and `links`, its first five pages, each with `label` and `url`. Base path included; a top-level entry for the landing page itself is left out. Empty on other pages and with other variants |
| `first_nav_route` | The first route in `nav` order that isn't this page, preferring one that has a page, base path included. Empty when there is none. The landing layout's Get Started button links to it |
| `base_path` | `build.basePath`, or an empty string for a site at the root |
| `search_enabled` | True when `features.search.enabled` and `theme.options.showSearch` are both true |
| `css_files`, `js_files` | Theme stylesheet and script URLs, base path included, each ending in `?v=` and a hash of the file. With the default theme, `css_files` is `main.css`, the configured style's stylesheet unless it is `default`, and `layouts.css` when a layout variant is set, in that order |
| `icon_sprite` | The default theme's SVG sprite, base path and `?v=` hash included, such as `/_theme/icons.svg?v=0123456789`. Icons are drawn as `<svg ...><use href="{icon_sprite}#name"/></svg>`. Empty with a custom theme, whose `nav` icons, `social_links` and `page.features` icons stay whole SVG markup |
| `search_index_url` | The URL of `search-index.json`, base path and `?v=` hash included, or an empty string when the build writes no index |
| `partials` | The theme's partials as raw strings, keyed by file name |
| `versions` | Configured versions with `label`, `slug`, `is_default`, `is_prerelease`, `is_current` and `url`, the link from this page: the same page in that version when it has one, else its home page. Empty without versioning |
| `current_version` | Label of the version being built, or an empty string |
| `package` | `name` and `version` from the first C# project. Only set when a project was analyzed |
| `edit_url` | The "Edit this page" URL. Only set when `site.editLink` is configured, `theme.options.showEditLink` is true and the page has a source file, so generated pages have none |
| `mokadocs_version` | The MokaDocs version, such as `1.6.0` |
| `t` | The theme's text in the site's language, one variable per key in snake case: `t.toc_title` for `toc.title` (see [Languages](/guide/languages#in-a-custom-theme)) |

Values are raw text unless a field says it holds HTML. The default layouts pass every value they write into an attribute through `html.escape`, so a route or label with `"` or `<` in it can't end the attribute early.

`page` fields:

| Field | Contents |
|-------|----------|
| `page.title`, `page.description` | Front matter values |
| `page.meta_description` | `description`, or `site.description` when the page has none |
| `page.canonical_url` | Absolute URL built from `site.url`, the base path and the route, with each segment percent-encoded and a trailing `/`. Empty without `site.url`, and on the 404 page |
| `page.noindex` | True on the 404 page, which the default layout marks with `<meta name="robots" content="noindex">` |
| `page.image_url` | The absolute URL of the page's link preview image: front matter `image`, else `site.image_url`. Empty when there is none or it can't be made absolute |
| `page.markdown_url` | The URL of the page's Markdown copy, base path included, or an empty string when `build.llms` is off or the page has none |
| `page.content` | The page HTML, with the base path added to root-relative URLs in `href`, `src` and `srcset` attributes, quoted or not |
| `page.route` | The route, base path included |
| `page.toc` | Headings with `level`, `text`, `id` and `children`, down to `theme.options.tocDepth`, without the page's own title heading |
| `page.show_toc` | True when front matter `toc` and `theme.options.showTableOfContents` are true and `page.toc` has at least two entries, counting nested ones |
| `page.tags`, `page.layout` | Front matter values |
| `page.source_path` | Path relative to `content.docs` with forward slashes, empty for generated pages |
| `page.last_modified` | The date as `yyyy-MM-dd`, or an empty string |
| `page.last_modified_display` | The same date written out in the site's language, such as `November 12, 2026`, or an empty string |
| `page.contributors` | The people who committed to the page's file, most commits first, each with `name`, `initials` (up to two letters) and `commits`. Empty unless `theme.options.showContributors` is on |
| `page.is_api`, `page.is_api_index` | Whether the page was generated by C# analysis, and whether it is `/api` |
| `page.features` | Cards from the `features` front matter, each with `title`, `description`, `icon`, `icon_svg` (SVG markup when `icon` names a bundled icon, otherwise empty) and `link` (a link starting with `/` includes the base path). Text is not escaped |
| `page.features_title`, `page.features_subtitle` | Front matter `featuresTitle` and `featuresSubtitle`, not escaped |
| `page.hero_code` | HTML: front matter `heroCode` as a highlighted code block in the `heroCodeLanguage`, or an empty string |
| `page.hero_image` | Front matter `heroImage`, resolved against the page's file, with the base path added to a path from the site root. Empty when there is none |
| `page.showcase_code` | HTML: front matter `showcaseCode` as a highlighted code block in the `showcaseCodeLanguage`, or an empty string |
| `page.showcase_title`, `page.showcase_text` | Front matter `showcaseTitle` and `showcaseText`, not escaped |
| `page.stats` | Figures from the `stats` front matter, each with `value` and `label`, not escaped |
| `page.cta` | The `cta` front matter with `title`, `text`, `link` (a link starting with `/` includes the base path; empty for no button) and `link_text` ("Get Started" when not set), not escaped. Null when the page has none |
| `page.is_blog_post` | Whether the page is a listed blog post. The default layout labels its previous and next links "Newer post" and "Older post" then |

`site` fields: `title`, `description`, `url`, `copyright` (with `{year}` replaced), `logo` and `favicon` (the raw values from `mokadocs.yaml`), `logo_url` and `favicon_url` (resolved URLs, with the base path added for local files, or empty when a local file is missing), `image_url` (the absolute URL of `site.image` under `site.url` and the base path, or empty), `repo_url` (`site.editLink.repo`), `language` (the page language, such as `pt-BR`), `direction` (`ltr` or `rtl`) and `og_locale` (such as `pt_BR`, or empty; see [Languages](/guide/languages)).

`theme` fields: `primary_color`, `accent_color`, `code_theme`, `code_style`, `style` (the configured style in lower case, `default` when it names none), `default_color_theme`, `initial_color_theme` (the preset applied on a first visit, empty when none is), `toc_depth`, `social_links` (each with `icon`, `url` and `icon_svg`), `style_files` (the default theme's style stylesheets, each with `name` and `version`, the `?v=` hash of `_theme/css/style-<name>.css`; empty with a custom theme), `layout` (the variant of each part of `theme.options.layout`, resolved to a known name or `default`, keyed in snake case: `width`, `navbar`, `footer`, `toc`, `breadcrumbs`, `page_nav`, `sidebar`, `sidebar_scope`, `home`, `hero`, `hero_background`, `features`, `admonitions`, `tables`, `density`, `font`, `headings` and `links`), and one boolean per option: `color_themes`, `code_theme_selector`, `code_style_selector`, `style_selector`, `text_size_selector`, `width_selector`, `show_edit_link`, `show_last_updated`, `show_contributors`, `show_feedback`, `show_dark_mode_toggle`, `show_animations`, `show_search`, `show_table_of_contents`, `show_prev_next`, `show_breadcrumbs`, `show_back_to_top`, `show_copy_button`, `show_line_numbers`, `image_zoom`, `sidebar_toggle`, `show_version_selector` and `show_built_with`.

`theme.layout_selector` is `theme.options.layoutSelector`. `theme.layout_parts` lists the parts readers can switch in the Appearance panel's Layout section, each with `key` (its name in `theme.layout`, such as `page_nav`), `name` (its attribute without `data-`, such as `page-nav`) and `names` (every variant, space separated). With `layout_selector` on, `layout_tabs` and `footer_columns` are filled whatever `navbar` and `footer` are set to.

The default theme's search dialog carries `search_index_url` in its `data-index` attribute, and the script fetches that URL when the dialog first opens. The hash changes when the index does, so a browser can keep the file cached between pages and visits.

### Partials

Scriban's `include` needs a template loader, and MokaDocs doesn't configure one. A layout that calls `{{ include 'footer' }}` fails to render, and the build reports an error as described in [Template Engine](#template-engine).

Files in a theme's `partials/` folder are available as `partials.<name>`, where the name is the file name without `.html`. They are raw strings, so the file is inserted as written and any Scriban expressions inside it are not evaluated:

```html
<body>
    {{ partials.header }}
    <main>{{ page.content }}</main>
</body>
```

The default theme's partials are all empty.

## Diagnostics

Phases and plugins record problems in `BuildContext.Diagnostics`, a `DiagnosticBag`, instead of throwing. Each `Diagnostic` has a `Severity`, a `Message` and a `Source` (the phase name or plugin id).

- **Error**: output is missing or wrong. Recorded when a plugin throws during a build or calls `LogError`, when a layout fails to render, and by a dry run when `build.output` is, or contains, the project folder or the docs folder. A real build with that output setting stops before deleting anything. `mokadocs build` exits with code 1 when the build throws or records an error.
- **Warning**: the build carries on. Recorded by:
  - `MarkdownParsePhase`: a Markdown file that fails to parse, or front matter that can't be read
  - `CSharpAnalysisPhase`: a C# project that is missing or fails to analyze
  - `NavigationBuildPhase`: a sidebar icon name that isn't in the icon set
  - `RenderPhase`: a `theme.name` folder that is missing or has no layouts, a `layout` name the theme doesn't have, or a social link icon that isn't in the icon set
  - `OutputPhase`: pages that share a route (only the last one is written)
  - plugins that call `LogWarning`
- **Info**: available to phases and plugins. No built-in phase records one.

How the CLI reports them:

- `mokadocs build` prints the warning and error counts. It always lists errors, and lists warnings with `--verbose`. Each line reads `[Severity] Message`, in the order the diagnostics were recorded, without the source or a file position.
- `mokadocs validate` lists warnings and errors sorted by severity and then source, with the source name, and adds info diagnostics with `--verbose`. It exits with 2 on errors and 1 on warnings.
- `mokadocs serve` prints the same summary as `build` after every build and rebuild, and keeps serving when there are errors.

See [CLI Commands](/advanced/cli-reference) for the full command reference.
