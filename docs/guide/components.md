---
title: UI Components
order: 2
---

# UI Components

MokaDocs adds four block components to Markdown: cards, steps, link cards and code groups. Each one is a `:::` block named after the component, and they can be nested inside each other (see [Combining Components](#combining-components)).

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

`:::code-group` shows fenced code blocks as tabs, one tab per block. Use it when every tab holds only code. For tabs that mix prose and code, use [tabbed content](/guide/markdown#tabbed-content).

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
