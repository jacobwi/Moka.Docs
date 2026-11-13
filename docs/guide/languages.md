---
title: Languages
description: Show the theme's text in your site's language, change any of it, and write pages right to left
order: 8
---

# Languages

Everything the default theme writes around your content, from "On this page" and "Search documentation..." to the names screen readers announce, comes from one list of texts. `site.language` picks a built-in translation of that list, and `theme.options.strings` changes any text you want worded differently. Your own pages stay as you wrote them.

```yaml
site:
  title: "Mein Projekt"
  language: de
```

## `site.language`

- **Type:** `string`, a BCP 47 language tag such as `en`, `de`, `pt-BR` or `zh-Hans`
- **Default:** `en`
- **Environment variable:** `MOKADOCS_SITE_LANGUAGE`

The language sets:

- `<html lang="...">` on every page MokaDocs writes: the pages, the 404 page and the redirect pages of section folders and blog page folders. Screen readers pick their voice from it, and browsers their hyphenation and fonts.
- The theme's text, from the built-in translation for the language (see below).
- `og:locale` for link previews, when the language maps cleanly: a tag with a region (`pt-BR` gives `pt_BR`, `de-AT` gives `de_AT`), and `ja`, `ko`, `zh-Hans` and `zh-Hant` without one (`ja_JP`, `ko_KR`, `zh_CN`, `zh_TW`). Other tags without a region, `en` included, write no `og:locale`. Like the other preview tags it needs [`site.url`](/configuration/site-config#url).
- How dates are written: "Last updated" under each page and the dates of blog posts.
- Plural forms of counted texts, such as the number of search results.
- The text MokaDocs writes into your pages: the titles of admonitions and `:::details` blocks that have none of their own, the install widget of `:::package`, the names screen readers announce for code blocks, videos and file tree folders, the API index's title and table headings, and the blog's listings.

The tag is written in its usual case (`PT-br` becomes `pt-BR`). A value that isn't a language tag is a build warning, and the site is built in English.

### Built-in translations

| Language | `site.language` | Notes |
|---|---|---|
| English | `en` (any `en-*`) | The default and the fallback for every text |
| Spanish | `es`, `es-*` | Neutral wording, "tú" |
| French | `fr`, `fr-*` | Non-breaking spaces before `:` `?` `!` |
| German | `de`, `de-*` | "Strg K" on the search button |
| Portuguese | `pt`, `pt-BR`, `pt-PT` | Brazilian wording |
| Japanese | `ja` | |
| Chinese (simplified) | `zh`, `zh-CN`, `zh-SG`, `zh-Hans` | Traditional Chinese (`zh-TW`, `zh-HK`, `zh-Hant`) has no translation yet and gets English |

A language looks for each text key by key, from the most specific tag to the least: `pt-BR` uses the `pt` translation, and a key a translation leaves out is English. A language with no translation at all (`ar`, `ko`, `nl`, ...) is English except for what you set in `theme.options.strings`.

The translations are starting points, written once and not yet reviewed by native speakers of every language. If a wording reads wrong, change it for your site with `theme.options.strings`, and send the fix upstream: each language is one file, `src/Moka.Docs.Core/Theming/ThemeStrings.<Language>.cs`, a list of key and text pairs that anyone can edit.

### Dates

| Language | "Last updated" for 2026-11-12 |
|---|---|
| English | November 12, 2026 |
| Spanish | 12 de noviembre de 2026 |
| French | 12 novembre 2026 |
| German | 12. November 2026 |
| Portuguese | 12 de novembro de 2026 |
| Japanese, Chinese | 2026年11月12日 |

These patterns are built in, so a site builds the same text on every machine. For any other language MokaDocs uses the build machine's long date for it, without the day of the week, and the ISO date (`2026-11-12`) when the machine has no data for the language (a container running with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`, for example). The page also carries the ISO date in a `<time datetime>` element.

## `site.direction`

- **Type:** `string`, `ltr` or `rtl`
- **Default:** the language's own direction
- **Environment variable:** `MOKADOCS_SITE_DIRECTION`

Arabic (`ar`), Hebrew (`he`), Persian (`fa`), Urdu (`ur`), Pashto (`ps`), Sindhi (`sd`), Uyghur (`ug`), Yiddish (`yi`), Dhivehi (`dv`) and Central Kurdish (`ckb`) are written right to left, and a site in one of them gets `dir="rtl"` without setting anything. Set `direction` to override that either way. Any other value is a build warning, and the language's own direction is used.

```yaml
site:
  language: ar
  # direction: rtl is the default for Arabic
```

A right-to-left page loads one more stylesheet, `_theme/css/rtl.css`, after the others. It mirrors the page in every style and layout variant: the sidebar sits on the right and "On this page" on the left, text and tables align right, borders and accent bars move to the other side, the breadcrumbs, previous and next links, link card arrows and blog page links point the other way, the Appearance panel and the version menu open toward the page, the drawers slide in from the other side, and the back-to-top and "On this page" buttons move to the bottom left. Left-to-right sites don't load it, so their pages are unchanged.

Not mirrored:

- Code blocks, inline code and keyboard keys stay left to right, as code reads.
- Mermaid diagrams keep the direction Mermaid draws them in.
- Background gradients and images, such as the `boxed` width's frame and the home page's hero backgrounds.
- Characters the theme puts before text, such as the `terminal` style's `$` and `#` prompts, stay the same characters (they still come first, now on the right).
- With the `lines` sidebar variant, the end of each link keeps the theme's standard padding rather than the style's own, a difference of a few pixels.

## `theme.options.strings`

- **Type:** `map` of key to text
- **Default:** empty

Changes any text of the theme. It wins over the built-in translation and over English, for that key only:

```yaml
site:
  language: de
theme:
  options:
    strings:
      toc.title: "Inhalt"
      search.placeholder: "Suchen..."
      footer.builtWith: "Gebaut mit {mokadocs}"
```

- Keys are case sensitive. A key the theme doesn't have is a build warning that names the closest key, and it is ignored.
- `{name}` marks a value the theme fills in where the text is used: `{label}`, `{count}`, `{query}`, `{date}` and so on (the table below lists them). Keep them, in the place your language needs them.
- Text is plain text. The theme escapes it wherever it goes, so `<`, quotes and `{{` show as written.
- An empty text (`toc.title: ""`) leaves that text out.
- Changing a text that the theme's script shows (search messages, Copy buttons, the "On this page" counter) changes `main.js`, and with it the `?v=` the pages load it with, so browsers fetch the new one.

### Plural forms

A text with a count has a `.one` and an `.other` key, such as `search.resultCount.one` (`{count} result`) and `search.resultCount.other` (`{count} results`). The theme picks one with the plural rules of the page's language: `.one` for 1 in English, German and Spanish, for 0 and 1 in French and Portuguese, never in Japanese and Chinese. For a language with more forms, add them to `strings`: `.zero`, `.two`, `.few` and `.many` keys of a counted text are accepted, and the browser's plural rules pick among them in the search dialog and the "On this page" counter. Texts written at build time (blog reading times and post counts) use `.one` and `.other`.

## In the ASP.NET Core Host

`MokaDocsOptions.Language`, `MokaDocsOptions.Direction` and `MokaDocsOptions.Strings` do the same as `site.language`, `site.direction` and `theme.options.strings` (see [ASP.NET Core Integration](/guide/aspnetcore#languages)).

## In a Custom Theme

Layouts read the text as `t`, one variable per key in snake case: `toc.title` is `t.toc_title`, `a11y.skipToContent` is `t.a11y_skip_to_content`, `search.resultCount.other` is `t.search_result_count_other`. Write it through `html.escape`, and fill in a value after escaping:

```html
<html lang="{{ site.language | html.escape }}" dir="{{ site.direction | html.escape }}">
...
<p class="toc-title">{{ t.toc_title | html.escape }}</p>
<span>{{ t.page_last_updated | html.escape | string.replace "{date}" (page.last_modified_display | html.escape) }}</span>
```

`site.language`, `site.direction` (`ltr` or `rtl`), `site.og_locale` (empty when there is none) and `page.last_modified_display` (the date written out) are there for every theme. `page.last_modified` stays the ISO date.

## Search in Other Languages

Search works the same in every language: each word of the query must appear in a result, as a case-insensitive substring. The query is split into words at spaces, which suits languages that put spaces between words.

Chinese, Japanese and Korean write words without spaces, so a query is often one long word. A page matches when it contains the query as written. When it doesn't, a query of three or more characters still matches a page that holds at least two thirds of the query's pairs of neighboring characters, so `検索設定` finds a page that writes `検索の設定`. Those results rank below exact matches, like results found through a typo. There is no other word segmentation, so put a space between words when a query finds too much or too little.

## What Stays in English

- Generated API reference pages, apart from the API index's title and table headings: member sections, signatures and badges.
- Pages and controls of plugins: REST API (OpenAPI) and Python API pages, the changelog's section labels, the REPL's Run button and the Blazor preview.
- Names of code themes (Catppuccin Mocha, GitHub Dark, ...) and of the `.NET CLI` and `PackageReference` install tabs.
- `llms.txt`, `llms-full.txt` and the Markdown copies of pages, which are written for tools.
- Build output, warnings, `mokadocs` commands and the theme lab of `mokadocs serve`.

## Text Keys

Every text of the theme, with its English wording. The style, color, code style and Layout section names follow the lists in [Site Configuration](/configuration/site-config#options.style).

| Key | English | Where it shows |
|---|---|---|
| `a11y.skipToContent` | `Skip to content` | The first link on every page, for keyboard users |
| `nav.label` | `Documentation` | Name of the sidebar for screen readers |
| `nav.toggleSection` | `Toggle {label}` | Button that opens or closes a sidebar section or a table of contents entry; \{label} is its title |
| `nav.toggleNavigation` | `Toggle navigation` | Button that opens the sidebar on phones |
| `nav.toggleSidebar` | `Toggle sidebar` | Button that hides the sidebar on wide screens (sidebarToggle) |
| `nav.sections` | `Sections` | Name of the tabs row (layout.navbar: tabs) for screen readers |
| `breadcrumbs.label` | `Breadcrumb` | Name of the breadcrumbs for screen readers |
| `breadcrumbs.home` | `Home` | The first breadcrumb |
| `banner.label` | `Announcement` | Name of the banner for screen readers |
| `banner.learnMore` | `Learn more` | Banner link when banner.linkText isn't set |
| `banner.dismiss` | `Dismiss announcement` | Button that closes the banner |
| `version.label` | `Version` | Read before the version on the version menu button |
| `version.latest` | `latest` | Badge on the default version |
| `version.prerelease` | `pre` | Badge on a prerelease version |
| `search.button` | `Search` | Search button in the header |
| `search.shortcut` | `Ctrl K` | Shortcut on the search button, except on macOS and iOS |
| `search.label` | `Search documentation` | Title of the search dialog and label of its field |
| `search.placeholder` | `Search documentation...` | Search field before anything is typed |
| `search.close` | `Close search` | Button that closes the search dialog |
| `search.results` | `Search results` | Name of the result list for screen readers |
| `search.prompt` | `Type to search.` | Search dialog before anything is typed |
| `search.loading` | `Loading the search index...` | Search dialog while the index downloads |
| `search.unavailable` | `Search is unavailable.` | Search dialog when the index can't be loaded |
| `search.noResults` | `No results for "{query}".` | Search dialog when nothing matches; \{query} is what was typed |
| `search.resultCount.one` | `{count} result` | Read by screen readers after a search; \{count} is the number of results |
| `search.resultCount.other` | `{count} results` | The same, for any other count |
| `search.categoryDocs` | `Documentation` | Category of a docs page in search results |
| `search.categoryBlog` | `Blog` | Category of a blog page in search results |
| `appearance.darkMode` | `Dark mode` | Dark mode button in the header |
| `appearance.title` | `Appearance` | Appearance button and panel title |
| `appearance.close` | `Close appearance settings` | Button that closes the Appearance panel |
| `appearance.mode` | `Mode` | Appearance panel: light, dark or system |
| `appearance.light` | `Light` | Appearance panel: light mode, and the light code themes |
| `appearance.dark` | `Dark` | Appearance panel: dark mode, and the dark code themes |
| `appearance.system` | `System` | Appearance panel: follow the system's mode |
| `appearance.style` | `Style` | Appearance panel section |
| `appearance.color` | `Color` | Appearance panel section |
| `appearance.codeTheme` | `Code theme` | Appearance panel section |
| `appearance.codeStyle` | `Code style` | Appearance panel section |
| `appearance.textSize` | `Text size` | Appearance panel section |
| `appearance.readingWidth` | `Reading width` | Appearance panel section |
| `appearance.width.narrow` | `Narrow` | Appearance panel: reading width |
| `appearance.width.default` | `Default` | Appearance panel: reading width |
| `appearance.width.wide` | `Wide` | Appearance panel: reading width |
| `appearance.width.wider` | `Wider` | Appearance panel: reading width |
| `appearance.width.full` | `Full` | Appearance panel: reading width |
| `appearance.layout` | `Layout` | Appearance panel section (layoutSelector) |
| `appearance.siteDefault` | `{label} (default)` | Layout section: the site's own choice; \{label} is its name |
| `appearance.reset` | `Reset to site defaults` | Button at the end of the Appearance panel |
| `page.copy` | `Copy page` | Button that copies the page's Markdown |
| `page.viewMarkdown` | `View as Markdown` | Link to the page's Markdown |
| `page.edit` | `Edit this page` | Edit link (editLink) |
| `page.lastUpdated` | `Last updated: {date}` | Under the page; \{date} is the date written out in the site's language |
| `page.contributors` | `Contributors:` | Before the list of contributors (showContributors) |
| `page.backToTop` | `Back to top` | Floating button |
| `toc.title` | `On this page` | Title of the table of contents |
| `toc.top` | `Top` | Link at the top of the table of contents |
| `toc.toggle` | `Toggle table of contents` | Button that opens the table of contents on narrow screens |
| `toc.position.one` | `Section {index} of {count}` | Under the table of contents; \{index} is the current section, \{count} how many there are |
| `toc.position.other` | `Section {index} of {count}` | The same, for any other count |
| `pageNav.label` | `Previous and next pages` | Name of the previous and next links for screen readers |
| `pageNav.previous` | `Previous` | Above the previous page's title |
| `pageNav.next` | `Next` | Above the next page's title |
| `pageNav.newerPost` | `Newer post` | Above the newer post's title on a blog post |
| `pageNav.olderPost` | `Older post` | Above the older post's title on a blog post |
| `feedback.prompt` | `Was this page helpful?` | Feedback widget |
| `feedback.yes` | `Yes` | Feedback widget button |
| `feedback.no` | `No` | Feedback widget button |
| `feedback.yesLabel` | `Yes, this page was helpful` | Name of the Yes button for screen readers |
| `feedback.noLabel` | `No, this page was not helpful` | Name of the No button for screen readers |
| `feedback.thanks` | `Thanks for your feedback!` | Feedback widget after a vote |
| `footer.builtWith` | `Built with {mokadocs}` | Footer; \{mokadocs} is the MokaDocs link and version |
| `footer.builtWithLove` | `Built with {heart} using {mokadocs}` | Home page footer; \{heart} is a heart |
| `footer.label` | `Footer` | Name of the footer link columns (layout.footer: columns) for screen readers |
| `footer.docs` | `Docs` | Footer column of top-level pages when the sidebar has no sections |
| `footer.more` | `More` | Footer column of top-level pages after the section columns |
| `landing.getStarted` | `Get Started` | Home page button, and a call to action without linkText |
| `landing.viewOnGithub` | `View on GitHub` | Home page button |
| `landing.search` | `Search the documentation` | Search box on the portal home page |
| `landing.sections` | `Documentation sections` | Name of the portal home page's section cards for screen readers |
| `landing.keyFigures` | `Key figures` | Name of the showcase home page's figures for screen readers |
| `code.copy` | `Copy` | Copy button on code blocks and install commands |
| `code.copyLabel` | `Copy code` | Name of a code block's Copy button for screen readers |
| `code.copied` | `Copied` | Code block Copy button after copying |
| `code.failed` | `Failed` | Code block Copy button when copying failed |
| `code.label` | `{language} code` | Name of a code block for screen readers; \{language} is its language |
| `code.labelPlain` | `Code` | Name of a code block without a language for screen readers |
| `copy.copied` | `Copied!` | Copy page and install buttons after copying |
| `copy.failed` | `Copy failed` | Copy buttons when copying failed |
| `heading.anchor` | `Link to {heading}` | Name of a heading's # link for screen readers; \{heading} is the heading |
| `image.zoom` | `Zoom image` | Name of a zoomable image without alt text for screen readers (imageZoom) |
| `image.zoomNamed` | `Zoom image: {alt}` | The same for an image with alt text |
| `image.dialog` | `Image` | Name of the zoomed image for screen readers |
| `image.dialogNamed` | `Image: {alt}` | The same for an image with alt text |
| `image.close` | `Close image` | Button that closes the zoomed image |
| `install.nugetLink` | `View {package} on NuGet.org` | NuGet link of the install widget; \{package} is the package ID |
| `install.with` | `Install {package} with` | Name of the install widget's tabs for screen readers |
| `install.packageManager` | `Package Manager` | Install widget tab |
| `install.copyLabel` | `Copy to clipboard` | Name of the install widget's Copy button for screen readers |
| `admonition.note` | `Note` | Title of a note block without its own |
| `admonition.tip` | `Tip` | Title of a tip block without its own |
| `admonition.info` | `Info` | Title of an info block without its own |
| `admonition.warning` | `Warning` | Title of a warning block without its own |
| `admonition.caution` | `Caution` | Title of a caution block without its own |
| `admonition.danger` | `Danger` | Title of a danger block without its own |
| `admonition.important` | `Important` | Title of an important block without its own |
| `details.summary` | `Details` | Title of a :::details block without its own |
| `fileTree.folder` | `, folder` | Read by screen readers after a folder's name in a :::file-tree |
| `video.title` | `Video` | Name of a :::video without a title, for screen readers |
| `video.youtube` | `YouTube video {id}` | The same for a YouTube video; \{id} is the video's ID |
| `video.vimeo` | `Vimeo video {id}` | The same for a Vimeo video |
| `video.download` | `Download the video: {title}` | Link inside a :::video for browsers that can't play it |
| `api.title` | `API Reference` | Title of the API index page, and the category of API pages in search results |
| `api.name` | `Name` | API index table header |
| `api.kind` | `Kind` | API index table header |
| `api.description` | `Description` | API index table header |
| `blog.readMore` | `Read more` | Link under a post on the blog's index |
| `blog.readMoreAbout` | `about {title}` | Read by screen readers after Read more; \{title} is the post's title |
| `blog.noPosts` | `No posts yet.` | Blog index or tag page without posts |
| `blog.readingTime.one` | `{count} min read` | Reading time of a post; \{count} is minutes |
| `blog.readingTime.other` | `{count} min read` | The same, for any other count |
| `blog.draft` | `Draft` | Marks a draft post when drafts are shown |
| `blog.by` | `By` | Read by screen readers before a post's authors |
| `blog.and` | `and` | Between a post's last two authors |
| `blog.tags` | `Tags` | Title of the tags page, and the name of a post's tags for screen readers |
| `blog.tagsDescription` | `Every tag used in {blog}` | Description of the tags page; \{blog} is the blog's title |
| `blog.noTags` | `No tags yet.` | Tags page of a blog without tags |
| `blog.allTags` | `All tags` | Link on a tag page |
| `blog.postCount.one` | `{count} post` | Number of posts with a tag |
| `blog.postCount.other` | `{count} posts` | The same, for any other count |
| `blog.taggedTitle` | `Posts tagged "{tag}"` | Title of a tag page; \{tag} is the tag |
| `blog.taggedDescription` | `Posts tagged {tag} in {blog}` | Description of a tag page |
| `blog.pages` | `Pages` | Name of the blog's page links for screen readers |
| `blog.newerPosts` | `Newer posts` | Blog page link |
| `blog.olderPosts` | `Older posts` | Blog page link |
| `blog.page` | `Page {page} of {count}` | Between the blog's page links |
| `blog.pageTitle` | `{title} (page {page})` | Title of the blog's later pages; \{title} is the blog's or the tag page's title |
| `notFound.title` | `Page not found` | Title of the 404 page |
| `notFound.text` | `There's no page at this address. It may have moved, or the link may be mistyped.` | 404 page |
| `notFound.home` | `Go to the home page` | 404 page link |
| `notFound.search` | `search the docs` | 404 page link that opens search |
| `notFound.links` | `{home}, or {search}.` | 404 page: \{home} and \{search} are the two links |
| `notFound.linksNoSearch` | `{home}.` | 404 page without search: \{home} is the home page link |
| `redirect.section` | `This section starts at {link}.` | Page a section folder redirects from; \{link} links its first page |
| `redirect.page` | `Redirecting to {link}...` | Page an address redirects from (blog page folders) |
| `building.title` | `Building the documentation` | ASP.NET Core host: title of the page shown until the first build is done |
| `building.text` | `The documentation is being built. This page reloads in a few seconds.` | ASP.NET Core host: that page |
| `style.default` | `Default` | Appearance panel: name of a style |
| `style.default.description` | `The original look` | Appearance panel: under the style's name |
| `style.moka` | `Moka` | Appearance panel: name of a style |
| `style.moka.description` | `Dense, dark first` | Appearance panel: under the style's name |
| `style.minimal` | `Minimal` | Appearance panel: name of a style |
| `style.minimal.description` | `Quiet, few lines` | Appearance panel: under the style's name |
| `style.classic` | `Classic` | Appearance panel: name of a style |
| `style.classic.description` | `Reference docs` | Appearance panel: under the style's name |
| `style.soft` | `Soft` | Appearance panel: name of a style |
| `style.soft.description` | `Rounded, roomy` | Appearance panel: under the style's name |
| `style.terminal` | `Terminal` | Appearance panel: name of a style |
| `style.terminal.description` | `Monospace, dark first` | Appearance panel: under the style's name |
| `style.paper` | `Paper` | Appearance panel: name of a style |
| `style.paper.description` | `Serif, like a book` | Appearance panel: under the style's name |
| `style.contrast` | `Contrast` | Appearance panel: name of a style |
| `style.contrast.description` | `High contrast, clear focus` | Appearance panel: under the style's name |
| `style.glass` | `Glass` | Appearance panel: name of a style |
| `style.glass.description` | `Frosted panels` | Appearance panel: under the style's name |
| `style.brutal` | `Brutal` | Appearance panel: name of a style |
| `style.brutal.description` | `Thick lines, flat fills` | Appearance panel: under the style's name |
| `color.ocean` | `Ocean` | Appearance panel: name of a color |
| `color.emerald` | `Emerald` | Appearance panel: name of a color |
| `color.violet` | `Violet` | Appearance panel: name of a color |
| `color.amber` | `Amber` | Appearance panel: name of a color |
| `color.rose` | `Rose` | Appearance panel: name of a color |
| `color.moka-red` | `Moka Red` | Appearance panel: name of a color |
| `color.slate` | `Slate` | Appearance panel: name of a color |
| `color.teal` | `Teal` | Appearance panel: name of a color |
| `color.indigo` | `Indigo` | Appearance panel: name of a color |
| `color.orange` | `Orange` | Appearance panel: name of a color |
| `color.graphite` | `Graphite` | Appearance panel: name of a color |
| `codeStyle.plain` | `Plain` | Appearance panel: name of a code style |
| `codeStyle.macos` | `macOS` | Appearance panel: name of a code style |
| `codeStyle.terminal` | `Terminal` | Appearance panel: name of a code style |
| `codeStyle.vscode` | `VS Code` | Appearance panel: name of a code style |
| `layout.navbar` | `Top bar` | Appearance panel, Layout section: name of a part of the page |
| `layout.navbar.autohide` | `Auto-hide` | Appearance panel, Layout section: a choice for this part only |
| `layout.footer` | `Footer` | Appearance panel, Layout section: name of a part of the page |
| `layout.toc` | `On this page` | Appearance panel, Layout section: name of a part of the page |
| `layout.breadcrumbs` | `Breadcrumbs` | Appearance panel, Layout section: name of a part of the page |
| `layout.pageNav` | `Previous and next` | Appearance panel, Layout section: name of a part of the page |
| `layout.sidebar` | `Sidebar` | Appearance panel, Layout section: name of a part of the page |
| `layout.sidebar.accent` | `Accent bar` | Appearance panel, Layout section: a choice for this part only |
| `layout.sidebarScope` | `Sidebar lists` | Appearance panel, Layout section: name of a part of the page |
| `layout.sidebarScope.default` | `All sections` | Appearance panel, Layout section: a choice for this part only |
| `layout.sidebarScope.section` | `Current section` | Appearance panel, Layout section: a choice for this part only |
| `layout.admonitions` | `Admonitions` | Appearance panel, Layout section: name of a part of the page |
| `layout.admonitions.github` | `GitHub` | Appearance panel, Layout section: a choice for this part only |
| `layout.tables` | `Tables` | Appearance panel, Layout section: name of a part of the page |
| `layout.density` | `Spacing` | Appearance panel, Layout section: name of a part of the page |
| `layout.font` | `Font` | Appearance panel, Layout section: name of a part of the page |
| `layout.font.system` | `System UI` | Appearance panel, Layout section: a choice for this part only |
| `layout.font.mono` | `Monospace` | Appearance panel, Layout section: a choice for this part only |
| `layout.headings` | `Headings` | Appearance panel, Layout section: name of a part of the page |
| `layout.headings.accent` | `Accent bar` | Appearance panel, Layout section: a choice for this part only |
| `layout.links` | `Links` | Appearance panel, Layout section: name of a part of the page |
| `layout.variant.arrows` | `Arrows` | Appearance panel, Layout section: a choice |
| `layout.variant.back` | `Back` | Appearance panel, Layout section: a choice |
| `layout.variant.bordered` | `Bordered` | Appearance panel, Layout section: a choice |
| `layout.variant.callout` | `Callout` | Appearance panel, Layout section: a choice |
| `layout.variant.cards` | `Cards` | Appearance panel, Layout section: a choice |
| `layout.variant.centered` | `Centered` | Appearance panel, Layout section: a choice |
| `layout.variant.chevrons` | `Chevrons` | Appearance panel, Layout section: a choice |
| `layout.variant.columns` | `Columns` | Appearance panel, Layout section: a choice |
| `layout.variant.compact` | `Compact` | Appearance panel, Layout section: a choice |
| `layout.variant.default` | `Standard` | Appearance panel, Layout section: a choice |
| `layout.variant.dots` | `Dots` | Appearance panel, Layout section: a choice |
| `layout.variant.filled` | `Filled` | Appearance panel, Layout section: a choice |
| `layout.variant.floating` | `Floating` | Appearance panel, Layout section: a choice |
| `layout.variant.humanist` | `Humanist` | Appearance panel, Layout section: a choice |
| `layout.variant.inline` | `Inline` | Appearance panel, Layout section: a choice |
| `layout.variant.lines` | `Lines` | Appearance panel, Layout section: a choice |
| `layout.variant.minimal` | `Minimal` | Appearance panel, Layout section: a choice |
| `layout.variant.none` | `None` | Appearance panel, Layout section: a choice |
| `layout.variant.numbered` | `Numbered` | Appearance panel, Layout section: a choice |
| `layout.variant.outlined` | `Outlined` | Appearance panel, Layout section: a choice |
| `layout.variant.path` | `Path` | Appearance panel, Layout section: a choice |
| `layout.variant.pills` | `Pills` | Appearance panel, Layout section: a choice |
| `layout.variant.rounded` | `Rounded` | Appearance panel, Layout section: a choice |
| `layout.variant.serif` | `Serif` | Appearance panel, Layout section: a choice |
| `layout.variant.spacious` | `Spacious` | Appearance panel, Layout section: a choice |
| `layout.variant.split` | `Split` | Appearance panel, Layout section: a choice |
| `layout.variant.stacked` | `Stacked` | Appearance panel, Layout section: a choice |
| `layout.variant.striped` | `Striped` | Appearance panel, Layout section: a choice |
| `layout.variant.subtle` | `Subtle` | Appearance panel, Layout section: a choice |
| `layout.variant.tabs` | `Tabs` | Appearance panel, Layout section: a choice |
| `layout.variant.timeline` | `Timeline` | Appearance panel, Layout section: a choice |
| `layout.variant.transparent` | `Transparent` | Appearance panel, Layout section: a choice |
| `layout.variant.underlined` | `Underlined` | Appearance panel, Layout section: a choice |
