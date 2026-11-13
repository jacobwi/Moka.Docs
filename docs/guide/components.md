---
title: UI Components
order: 2
---

# UI Components

MokaDocs adds components to Markdown. Most are blocks: a `:::` line named after the component opens one and a `:::` line closes it, and they can be nested inside each other (see [Combining Components](#combining-components)).

| Component | Syntax |
|-----------|--------|
| [Cards](#cards) | `:::card{title="..." icon="..." variant="..."}` |
| [Steps](#steps) | `:::steps` with `###` headings |
| [Link cards](#link-cards) | `:::link-cards` with a list of links |
| [Code groups](#code-groups) | `:::code-group` with code blocks |
| [Collapsible details](#collapsible-details) | `:::details Summary`, `:::details{open} Summary` |
| [FAQ](#faq) | `:::faq` with `###` questions |
| [Columns](#columns) | `::::columns` with `:::column` blocks |
| [File trees](#file-trees) | `:::file-tree` with a nested list |
| [Timelines](#timelines) | `:::timeline` with `**date**` list items or `###` dates |
| [Videos](#videos) | `:::video URL` |
| [Figures](#figures) | `:::figure Caption` around an image |
| [Package install](#package-install) | `:::package Name [version]` |
| [Glossary](#glossary) | `:term[text]` and `:::glossary` |
| [API cross-references](#api-cross-references) | `[text](xref:Namespace.Type)`, `<xref:Namespace.Type.Member>` |
| [Badges](#badges) | `:badge[text]{variant}` |
| [Keyboard keys](#keyboard-keys) | `:kbd[Ctrl+K]` |

Attributes go in braces right after the name, as `name="value"` pairs (single quotes or no quotes work too) or single words such as `open`. Text you write in a component, titles and attributes included, is escaped, so `List<T>` shows as written. The [Markdown copies](/configuration/site-config#llms) of pages keep components as written.

## Cards

A card is a bordered box with an optional header that holds a title and an icon.

### Basic Syntax

```markdown
:::card{title="Getting Started"}
Install the CLI and run `mokadocs init` to create a project with a
starter page.
:::
```

:::card{title="Getting Started"}
Install the CLI and run `mokadocs init` to create a project with a starter page.
:::

Attributes go in braces after `card`, as `name="value"` pairs (single quotes work too). Leave out `title` and the card has no header.

### With Icons

`icon` adds an icon before the title:

```markdown
:::card{title="Static Output" icon="zap"}
`mokadocs build` writes a static site that any static file host
can serve.
:::
```

:::card{title="Static Output" icon="zap"}
`mokadocs build` writes a static site that any static file host can serve.
:::

The icon sits in the header, so a card without a `title` shows no icon. Icons are drawings from [Lucide](https://lucide.dev/icons/), but only these names are available:

`alert-triangle`, `arrow-left`, `arrow-right`, `book`, `book-open`, `box`, `braces`, `calendar`, `check`, `chevron-down`, `chevron-right`, `clock`, `code`, `code-2`, `compass`, `cpu`, `database`, `discord`, `download`, `external-link`, `file`, `file-code`, `file-text`, `folder`, `git-branch`, `github`, `globe`, `heart`, `home`, `image`, `info`, `key`, `layers`, `lightbulb`, `link`, `list`, `lock`, `mail`, `map`, `menu`, `message-circle`, `newspaper`, `nuget`, `package`, `play`, `puzzle`, `rocket`, `scroll-text`, `search`, `settings`, `shield`, `star`, `tag`, `terminal`, `twitter`, `upload`, `users`, `wrench`, `x`, `zap`

Ten of them (`rocket`, `book`, `code`, `settings`, `zap`, `shield`, `package`, `star`, `check` and `globe`) use drawings built into the card component. The rest come from the theme's shared icon set. A name that is not in the list shows no icon.

### Variants

`variant` sets the card's style. There are four:

| Variant   | Look |
|-----------|------|
| `default` | Border on the page background. This is what you get without `variant`. |
| `info`    | Blue left border and icon |
| `success` | Green left border and icon |
| `warning` | Amber left border and icon |

Any other value adds a `component-card-<value>` CSS class that has no styles, so the card looks like `default`.

```markdown
:::card{title="Default Card" icon="box" variant="default"}
This is the default card style.
:::

:::card{title="Info Card" icon="info" variant="info"}
This card uses a blue accent.
:::

:::card{title="Success Card" icon="check" variant="success"}
This card uses a green accent.
:::
```

:::card{title="Default Card" icon="box" variant="default"}
This is the default card style.
:::

:::card{title="Info Card" icon="info" variant="info"}
This card uses a blue accent.
:::

:::card{title="Success Card" icon="check" variant="success"}
This card uses a green accent.
:::

### Rich Content in Cards

A card can hold any Markdown, such as a code block and a link:

````markdown
:::card{title="Quick Setup" icon="rocket"}
Install MokaDocs and create your first project:

```bash
dotnet tool install -g mokadocs
mkdir my-docs
cd my-docs
mokadocs init
mokadocs serve
```

See the [quickstart](/getting-started/quickstart) for more options.
:::
````

### Attributes Reference

| Attribute | Required | Type   | Description |
|-----------|----------|--------|-------------|
| `title`   | No       | string | Header text. Without it the card has no header and no icon. |
| `icon`    | No       | string | An icon name from the list above |
| `variant` | No       | string | `default`, `info`, `success` or `warning` |

## Steps

`:::steps` turns `###` headings into a numbered sequence. Each step shows its number in a circle, with a line down to the next step.

### Basic Syntax

````markdown
:::steps
### Install the CLI tool

Install MokaDocs globally using the .NET CLI:

```bash
dotnet tool install -g mokadocs
```

### Create a new project

Run `init` in an empty folder. It writes `mokadocs.yaml` and `docs/index.md`:

```bash
mkdir my-docs
cd my-docs
mokadocs init
```

### Start the dev server

Build the site and serve it locally:

```bash
mokadocs serve
```

### Write your docs

Add Markdown files to the `docs/` folder. The dev server rebuilds the site and
reloads the browser when you save a file.
:::
````

:::steps
### Install the CLI tool

Install MokaDocs globally using the .NET CLI:

```bash
dotnet tool install -g mokadocs
```

### Create a new project

Run `init` in an empty folder. It writes `mokadocs.yaml` and `docs/index.md`:

```bash
mkdir my-docs
cd my-docs
mokadocs init
```

### Start the dev server

Build the site and serve it locally:

```bash
mokadocs serve
```

### Write your docs

Add Markdown files to the `docs/` folder. The dev server rebuilds the site and reloads the browser when you save a file.
:::

### How Steps Work

- Each `###` heading starts a step and becomes its title. Steps are numbered from 1.
- The Markdown after a heading, up to the next `###`, is the step's body. Headings of other levels stay inside the body.
- Content before the first `###` becomes a step without a title.
- Step headings keep their ids, so they show up in the page's table of contents and in search, and you can link to them.

To put a card or a code group inside a step, see [Combining Components](#combining-components).

### Real-World Example

````markdown
:::steps
### Configure your project

Create a `mokadocs.yaml` file in your repository root:

```yaml
site:
  title: My Project Docs
  description: Documentation for My Project
  url: https://docs.myproject.com
```

### Add your API project

Point MokaDocs at the .NET project to generate API documentation for:

```yaml
content:
  projects:
    - path: src/MyProject/MyProject.csproj
```

### Build

Generate the static site:

```bash
mokadocs build --output ./dist
```

The `dist/` folder now holds the static site, ready to deploy.
:::
````

## Link Cards

`:::link-cards` turns a Markdown list of links into a grid of clickable cards.

### Basic Syntax

```markdown
:::link-cards
- [Getting Started](/getting-started/quickstart) - Set up your first MokaDocs project
- [Configuration](/configuration/site-config) - Customize your documentation site
- [Markdown Features](/guide/markdown) - Learn about supported Markdown syntax
- [Deployment](/advanced/deployment) - Deploy your site to production
:::
```

:::link-cards
- [Getting Started](/getting-started/quickstart) - Set up your first MokaDocs project
- [Configuration](/configuration/site-config) - Customize your documentation site
- [Markdown Features](/guide/markdown) - Learn about supported Markdown syntax
- [Deployment](/advanced/deployment) - Deploy your site to production
:::

### How Link Cards Work

Each list item becomes one card:

```
- [Card Title](url) - Description text
```

| Part        | Source                                          | Shown as |
|-------------|-------------------------------------------------|----------|
| Title       | Link text                                       | The card heading |
| URL         | Link target                                     | Where the card goes |
| Description | Everything after the link, minus the separator  | A line under the title (optional) |

The first link in an item sets the card's title and URL. The title uses the link text without its formatting. Everything after that link is the description, with a leading hyphen or em dash separator removed. Inline code in the description stays code; other formatting, including a second link, keeps only its text. Text before the first link is ignored, and list items without a link are skipped.

Relative links in link cards are resolved like any other link (see [Linking to Headings](/guide/markdown#linking-to-headings)). The grid fits as many columns as it can at a minimum card width of 250px, so on a narrow screen the cards stack in one column.

### External Links

A card can link to any URL. Links open in the same tab.

```markdown
:::link-cards
- [GitHub Repository](https://github.com/your-org/your-repo) - View the source code and contribute
- [NuGet Package](https://www.nuget.org/packages/MyLibrary) - Install from NuGet
- [API Reference](/api) - Browse the generated API documentation
:::
```

## Code Groups

`:::code-group` shows fenced code blocks as tabs, one tab per block. Use it when every tab holds only code. For tabs that mix prose and code, use [tabbed content](/guide/markdown#tabbed-content). Code groups and tabbed content share one choice: picking "C#" in one group picks "C#" in every group on the page, and later pages start on it.

Put only fenced code blocks in a code group. When a group has any, everything else inside it, such as a paragraph of explanation, is not rendered.

### Basic Syntax

````markdown
:::code-group
```csharp title="C#"
public record Person(string Name, int Age);
```
```fsharp title="F#"
type Person = { Name: string; Age: int }
```
```python title="Python"
@dataclass
class Person:
    name: str
    age: int
```
:::
````

:::code-group
```csharp title="C#"
public record Person(string Name, int Age);
```
```fsharp title="F#"
type Person = { Name: string; Age: int }
```
```python title="Python"
@dataclass
class Person:
    name: str
    age: int
```
:::

### Tab Titles

A tab's label comes from `title="..."` after the language. Without it, the label is a display name for the language:

| Language            | Label |
|---------------------|-------|
| `csharp`, `cs`      | C# |
| `fsharp`, `fs`      | F# |
| `javascript`, `js`  | JavaScript |
| `typescript`, `ts`  | TypeScript |
| `python`, `py`      | Python |
| `bash`, `sh`, `shell` | Shell |
| `powershell`, `ps1` | PowerShell |
| `json`              | JSON |
| `yaml`, `yml`       | YAML |
| `xml`               | XML |
| `html`              | HTML |
| `css`               | CSS |
| `sql`               | SQL |
| `dockerfile`        | Dockerfile |
| `markdown`, `md`    | Markdown |
| Any other language  | The identifier as written |
| No language         | Code |

File names make good titles:

````markdown
:::code-group
```json title="package.json"
{
  "name": "my-project",
  "version": "1.0.0"
}
```
```yaml title="mokadocs.yaml"
site:
  title: My Project
```
```xml title="Directory.Build.props"
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```
:::
````

:::code-group
```json title="package.json"
{
  "name": "my-project",
  "version": "1.0.0"
}
```
```yaml title="mokadocs.yaml"
site:
  title: My Project
```
```xml title="Directory.Build.props"
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```
:::

### Real-World Example: Installation Instructions

````markdown
:::code-group
```bash title=".NET CLI"
dotnet add package MyLibrary --version 1.0.0
```
```powershell title="Package Manager"
Install-Package MyLibrary -Version 1.0.0
```
```xml title="PackageReference"
<PackageReference Include="MyLibrary" Version="1.0.0" />
```
:::
````

:::code-group
```bash title=".NET CLI"
dotnet add package MyLibrary --version 1.0.0
```
```powershell title="Package Manager"
Install-Package MyLibrary -Version 1.0.0
```
```xml title="PackageReference"
<PackageReference Include="MyLibrary" Version="1.0.0" />
```
:::

### Behavior

- Each tab is an ordinary code block, so it gets highlighting, line numbers and a copy button under the same rules as any other (see [Syntax Highlighting](/guide/markdown#syntax-highlighting)).
- The first tab is selected when the page loads. The selection is not remembered across page loads, and code groups do not switch together.

## Collapsible Details

`:::details` hides its content behind a summary line that readers click to open. The text after `details` is the summary, and it can hold inline Markdown such as code and emphasis.

```markdown
:::details Why does the build warn about an unknown key?
Keys in `mokadocs.yaml` are case sensitive. `basepath` is not `basePath`,
so the build reports it and uses the default.
:::
```

:::details Why does the build warn about an unknown key?
Keys in `mokadocs.yaml` are case sensitive. `basepath` is not `basePath`, so the build reports it and uses the default.
:::

Add `{open}` after `details` to start the block open:

```markdown
:::details{open} What the dev server watches
Markdown files, `mokadocs.yaml` and the C# projects listed under `content.projects`.
:::
```

:::details{open} What the dev server watches
Markdown files, `mokadocs.yaml` and the C# projects listed under `content.projects`.
:::

Without a summary the block reads "Details". It is a native `<details>` element, so it works without JavaScript, the browser's find-in-page searches inside closed blocks, and a link to a heading inside one opens it. Printing opens every block.

## FAQ

`:::faq` turns `###` headings into a list of questions. Each question opens to show the content up to the next `###`:

```markdown
:::faq
### Does MokaDocs need Node.js?
No. It is a .NET tool, and the generated site is static HTML, CSS and JavaScript.

### Can I host the site on GitHub Pages?
Yes. Build with `--base-path /your-repo` and publish the output folder.
:::
```

:::faq
### Does MokaDocs need Node.js?
No. It is a .NET tool, and the generated site is static HTML, CSS and JavaScript.

### Can I host the site on GitHub Pages?
Yes. Build with `--base-path /your-repo` and publish the output folder.
:::

The questions are headings, so they appear in the table of contents and in search, and you can link to them. A `:::details` block inside the FAQ is a question too (give the FAQ the longer fence, `::::faq`). Content before the first question stays above the list.

The page also gets [FAQPage](https://schema.org/FAQPage) structured data for search engines: one block for all the questions on the page, with plain-text answers.

## Columns

`:::columns` puts its `:::column` blocks side by side. Use two to four columns; they stack into one on phones, and three or four become two on tablets.

```markdown
::::columns
:::column
**CLI**

Build a static site with `mokadocs build` and host it anywhere.
:::
:::column
**ASP.NET Core**

Serve the same docs from your app with `AddMokaDocs()` and `MapMokaDocs()`.
:::
::::
```

::::columns
:::column
**CLI**

Build a static site with `mokadocs build` and host it anywhere.
:::
:::column
**ASP.NET Core**

Serve the same docs from your app with `AddMokaDocs()` and `MapMokaDocs()`.
:::
::::

The columns block needs the longer fence because the columns nest inside it. With more than four columns the build warns and the rest wrap onto another row. Anything written straight into `:::columns`, outside a `:::column`, is a column of its own.

## File Trees

`:::file-tree` turns a nested list into a folder tree with an icon for each entry:

```markdown
:::file-tree
- docs/
  - guide/
    - **components.md** # this page
    - markdown.md
  - glossary.yml # terms for :term[...]
  - index.md
- src/
  - Program.cs
- mokadocs.yaml
- ...
:::
```

:::file-tree
- docs/
  - guide/
    - **components.md** # this page
    - markdown.md
  - glossary.yml # terms for :term[...]
  - index.md
- src/
  - Program.cs
- mokadocs.yaml
- ...
:::

- An entry with nested entries, or a name ending in `/`, is a folder. Folders with entries can be collapsed; they start open.
- Text after ` # ` (a space, `#` and a space) is a comment. Comments can hold `` `code` ``.
- `**name**` highlights the entry.
- The icon follows the extension: code, data and configuration, text, images, media, archives, and other files. `...` shows as is, without an icon.

Entries are read as plain text, so names such as `__init__.py` keep their underscores. Readers get the tree as nested lists, with each folder announced as a folder.

## Timelines

`:::timeline` shows dated entries along a vertical line. Start each list item with the date in bold:

```markdown
:::timeline
- **2026-01-12** Plan: pick the pages the first release needs.
- **2026-02-02** Write: guides, then the API comments.
- **2026-03-16** Release: publish the site with the package.
:::
```

:::timeline
- **2026-01-12** Plan: pick the pages the first release needs.
- **2026-02-02** Write: guides, then the API comments.
- **2026-03-16** Release: publish the site with the package.
:::

A colon or hyphen after the date is dropped. For longer entries, use `###` headings as the dates; the content up to the next `###` is the entry, and the dates appear in the table of contents:

```markdown
:::timeline
### 2026-03-16
The first release. This entry can hold lists, code and other blocks.

### 2026-05-04
The second release.
:::
```

A date written as `2026`, `2026-11` or `2026-11-12` is marked up as a `<time>` element. Other text, such as "Q3 2026", shows as written.

## Videos

`:::video` embeds a video from its URL:

```markdown
:::video https://www.youtube.com/watch?v=aqz-KE-bpKQ
Big Buck Bunny, a short film by the Blender Foundation
:::
```

:::video https://www.youtube.com/watch?v=aqz-KE-bpKQ
Big Buck Bunny, a short film by the Blender Foundation
:::

- **YouTube** (`youtube.com/watch?v=`, `youtu.be/`, `/embed/`, `/shorts/` and `/live/` addresses) plays in an iframe from `youtube-nocookie.com`. A `t=` or `start=` time in the address (`90`, `1m30s`) starts it there.
- **Vimeo** (`vimeo.com/123456789`, with the hash of an unlisted video after the number) plays in an iframe from `player.vimeo.com` with `dnt=1`, which turns off tracking.
- **A video file** (`.mp4`, `.webm`, `.ogv` or `.mov`), on the web or next to the page (`./media/intro.mp4`), plays in a `<video>` element with the browser's controls.

The content of the block is the video's title and its caption. Without content, write the title as an attribute, `:::video{title="Install walkthrough"} URL`, or the title is made from the URL ("YouTube video aqz-KE-bpKQ"), which is less useful to screen reader users. A video file can have a still image to show before it plays: `:::video{poster="./intro.png"} ./intro.mp4`.

The player keeps a 16:9 frame at any width. Nothing loads until the reader scrolls near an iframe, and a video file loads only when played. No third-party script is added to the page. Any other web address is a warning and shows as a link.

## Figures

`:::figure` gives an image (or a diagram, table or code block) a caption. The text after `figure` is the caption:

```markdown
:::figure The MokaDocs logo
![MokaDocs logo](../icon.png)
:::
```

:::figure The MokaDocs logo
![MokaDocs logo](../icon.png)
:::

The caption can also follow the image on the next line, or come as paragraphs after it:

```markdown
:::figure
![MokaDocs logo](../icon.png)
The logo, at its full size.
:::
```

The first block in the figure is the figure itself; everything after it is the caption. Image zoom works on figures as it does on any image.

## Package Install

`:::package` shows the NuGet install widget from the API reference for any package, with Package Manager, .NET CLI and PackageReference commands and copy buttons:

```markdown
:::package Moka.Docs.Core
:::
```

:::package Moka.Docs.Core
:::

A version after the ID (`:::package Moka.Docs.Core 1.9.0`, or `:::package{version="1.9.0"} Moka.Docs.Core`) goes into every command and the NuGet link. Without one, the commands install the latest version and the PackageReference uses `Version="*"`, except for the package of the first project under `content.projects`, which gets that project's version. Use the block as often as you like on a page. An ID that isn't a valid NuGet ID is a warning and shows nothing.

## Glossary

Define terms once and show their definitions where you use them. Put the terms under `glossary:` in `mokadocs.yaml` (see [`glossary`](/configuration/site-config#glossary-section)), or in a `glossary.yml` file at the root of the docs folder:

```yaml
API: The types and members a library makes public, and what the API reference documents.
Front matter: The YAML block at the top of a Markdown page, between two --- lines.
```

### Terms in Text

`:term[...]` marks a term. Its definition shows when the reader hovers over it, moves the keyboard focus to it, or taps it:

```markdown
Every page starts with :term[front matter]. The :term[API] reference
is generated from your projects.
```

Every page starts with :term[front matter]. The :term[API] reference is generated from your projects.

Terms match without regard to case, and a plural finds its singular (`:term[APIs]` finds "API"). To show other text, name the term: `:term[public surface]{term="API"}`. Escape hides the definition; it stays while the pointer moves onto it. A term that isn't in the glossary is a warning with its line, and shows as plain text.

### The Glossary List

`:::glossary` lists every term with its definition, sorted by term. Each term gets an anchor, such as `#glossary-api`:

```markdown
:::glossary
:::
```

:::glossary
:::

Content inside the block shows above the list. Definitions are plain text, with `` `code` `` for code.

## API Cross-References

Link to a type or member of the generated API reference by its name, with `xref:` instead of a URL:

```markdown
The configuration is a [`SiteConfig`](xref:Moka.Docs.Core.Configuration.SiteConfig) record.
Its build options are in <xref:Moka.Docs.Core.Configuration.SiteConfig.Build>.
```

The configuration is a [`SiteConfig`](xref:Moka.Docs.Core.Configuration.SiteConfig) record. Its build options are in <xref:Moka.Docs.Core.Configuration.SiteConfig.Build>.

A link with text keeps it. `<xref:...>` shows the type's name, or the type and member for a member. The link goes to the type's page, and to the member's section for a member, at the same addresses the API pages use.

| You write | Links to |
|-----------|----------|
| `xref:MyLib.Text.Parser` | The type |
| ``xref:MyLib.Result`1``, `xref:MyLib.Result{T}`, `xref:MyLib.Result<T>` | The generic type |
| `xref:MyLib.Text.Parser.Parse` | The member; with overloads, the first one |
| `xref:MyLib.Text.Parser.Parse(System.String)` | That overload |
| `xref:MyLib.Text.Parser.#ctor` | The constructor |
| `xref:M:MyLib.Text.Parser.Parse(System.String)` | A documentation ID, as in `<see cref>`, works as written |
| `xref:Parser`, `xref:Parser.Parse` | The one type (or member) with that name, when the name is unique |
| `xref:System.String` | The page on learn.microsoft.com, for `System.*` and `Microsoft.*` names |

A reference that matches nothing is a build warning with the page and line, and shows as text: the link's own text, or the reference as code. Cross-references work with the API pages of the CLI and of the ASP.NET Core host. The Markdown copies of pages (`build.llms`) get the resolved links too.

## Inline Components

Three components go inside a line of text, including headings. They share one syntax: a colon, the name, the text in brackets, and options in braces.

### Badges

`:badge[...]` adds a small label:

```markdown
#### Glossary terms :badge[New]{variant=success}

A :badge[Beta]{warning} feature, a :badge[Deprecated]{danger} one,
one that arrived :badge[Since 2.0]{neutral} and a plain :badge[Info] badge.
```

#### Glossary terms :badge[New]{variant=success}

A :badge[Beta]{warning} feature, a :badge[Deprecated]{danger} one, one that arrived :badge[Since 2.0]{neutral} and a plain :badge[Info] badge.

| Variant | Use |
|---------|-----|
| `info` | The default: notes, versions |
| `success` | New or recommended |
| `warning` | Beta, experimental, changing |
| `danger` | Deprecated, removed, breaking |
| `neutral` | Plain labels such as "Since 1.9" |

Write the variant as `{variant=success}` or just `{success}`. Another name is a warning, and the badge uses `info`. In a heading, the badge isn't part of the heading's anchor or its table of contents entry, so adding or removing one doesn't break links.

### Keyboard Keys

`:kbd[...]` shows keys, one key cap per key, with `+` between them:

```markdown
Open search with :kbd[Ctrl+K]. Zoom in with :kbd[Ctrl++].
```

Open search with :kbd[Ctrl+K]. Zoom in with :kbd[Ctrl++].

Keys show as written; write the names your readers know. A `+` next to another `+` is the plus key itself.

### Escaping

Text in brackets is plain text. Write `\]` for a closing bracket inside it. A name right after a letter or digit (`word:badge[x]`) is left alone.

## Combining Components

Components nest. When a `:::` block sits inside another `:::` block, the outer one needs a longer fence: a line of colons closes a block only if it has at least as many colons as that block's opening line. Here the steps use four colons, so the `:::` lines of the code group and the card leave the steps open:

````markdown
::::steps
### Create the project

:::code-group
```bash title="Bash"
mkdir my-docs && cd my-docs && mokadocs init
```
```powershell title="PowerShell"
mkdir my-docs; cd my-docs; mokadocs init
```
:::

### Configure your project

:::card{title="Configuration File" icon="settings"}
`mokadocs init` writes `mokadocs.yaml` in the project root. See the
[configuration reference](/configuration/site-config) for all available options.
:::
::::
````

::::steps
### Create the project

:::code-group
```bash title="Bash"
mkdir my-docs && cd my-docs && mokadocs init
```
```powershell title="PowerShell"
mkdir my-docs; cd my-docs; mokadocs init
```
:::

### Configure your project

:::card{title="Configuration File" icon="settings"}
`mokadocs init` writes `mokadocs.yaml` in the project root. See the [configuration reference](/configuration/site-config) for all available options.
:::
::::

Add a colon for each level of nesting, such as `:::::steps` around `::::card` around `:::code-group`. With equal fences the first inner `:::` closes the steps as well, so the remaining steps render as plain headings after them, and the leftover `:::` wraps the content that follows in a plain `<div>`.

Blocks with different fence characters need no extra colons: a code group inside a tab (`===`) works as written. Admonitions and tab groups follow the same nesting rule; see [Nesting Blocks](/guide/markdown#nesting-blocks).
