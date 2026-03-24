---
title: UI Components
order: 2
---

# UI Components

MokaDocs provides a set of reusable UI components that extend standard Markdown with richer presentation options. These components use the `:::` fenced container syntax with specific keywords and attributes.

## Cards

Cards present content in a visually contained box with an optional title and icon. They are useful for highlighting features, key concepts, or important information.

### Basic Syntax

```markdown
:::card{title="Getting Started"}
Set up your first MokaDocs project in under five minutes with our
step-by-step guide.
:::
```

:::card{title="Getting Started"}
Set up your first MokaDocs project in under five minutes with our step-by-step guide.
:::

### With Icons

Add a `icon` attribute to display a [Lucide](https://lucide.dev/icons/) icon next to the card title.

```markdown
:::card{title="Blazing Fast" icon="zap"}
MokaDocs generates static sites with optimized assets for
lightning-fast page loads.
:::
```

:::card{title="Blazing Fast" icon="zap"}
MokaDocs generates static sites with optimized assets for lightning-fast page loads.
:::

### Variants

The `variant` attribute controls the visual style of the card. Three variants are available:

| Variant    | Description                                          |
|------------|------------------------------------------------------|
| `default`  | Standard card with subtle background and border      |
| `outlined` | Card with a prominent border and no background fill  |
| `filled`   | Card with a solid background color                   |

```markdown
:::card{title="Default Card" icon="box" variant="default"}
This is the default card style with a subtle background.
:::

:::card{title="Outlined Card" icon="square" variant="outlined"}
This card uses only a border with no background fill.
:::

:::card{title="Filled Card" icon="palette" variant="filled"}
This card has a solid background color for maximum emphasis.
:::
```

:::card{title="Default Card" icon="box" variant="default"}
This is the default card style with a subtle background.
:::

:::card{title="Outlined Card" icon="square" variant="outlined"}
This card uses only a border with no background fill.
:::

:::card{title="Filled Card" icon="palette" variant="filled"}
This card has a solid background color for maximum emphasis.
:::

### Rich Content in Cards

Cards support full Markdown content inside them, including lists, code blocks, and links.

```markdown
:::card{title="Quick Setup" icon="rocket"}
Install MokaDocs and create your first project:

\`\`\`bash
dotnet tool install -g mokadocs
mokadocs init my-docs
cd my-docs
mokadocs serve
\`\`\`

See the [full guide](./getting-started.md) for more options.
:::
```

### Attributes Reference

| Attribute | Required | Type   | Description                                    |
|-----------|----------|--------|------------------------------------------------|
| `title`   | Yes      | string | The title displayed at the top of the card      |
| `icon`    | No       | string | A Lucide icon name (e.g., `rocket`, `book`, `zap`) |
| `variant` | No       | string | Visual style: `default`, `outlined`, or `filled` |

## Steps

Steps display a numbered sequence of instructions with visual step indicators and connecting lines. Each step is defined by an `h3` heading inside the `:::steps` container.

### Basic Syntax

```markdown
:::steps
### Install the CLI tool

Install MokaDocs globally using the .NET CLI:

\`\`\`bash
dotnet tool install -g mokadocs
\`\`\`

### Create a new project

Scaffold a new documentation project:

\`\`\`bash
mokadocs init my-docs
\`\`\`

### Start the dev server

Launch the local development server with hot reload:

\`\`\`bash
cd my-docs
mokadocs serve
\`\`\`

### Write your docs

Open the `docs/` folder and start writing Markdown files. The dev server
will automatically reload when you save changes.
:::
```

:::steps
### Install the CLI tool

Install MokaDocs globally using the .NET CLI:

```bash
dotnet tool install -g mokadocs
```

### Create a new project

Scaffold a new documentation project:

```bash
mokadocs init my-docs
```

### Start the dev server

Launch the local development server with hot reload:

```bash
cd my-docs
mokadocs serve
```

### Write your docs

Open the `docs/` folder and start writing Markdown files. The dev server will automatically reload when you save changes.
:::

### How Steps Work

- Each `### Heading` inside the container becomes a step title
- Steps are automatically numbered starting from 1
- A vertical line connects each step visually
- Each step displays a numbered circle indicator
- Any Markdown content between headings becomes the step body

### Real-World Example

```markdown
:::steps
### Configure your project

Create a `mokadocs.yml` file in your repository root:

\`\`\`yaml
title: My Project Docs
description: Documentation for My Project
base_url: https://docs.myproject.com
\`\`\`

### Add your API project

Reference your .NET project for API documentation generation:

\`\`\`yaml
api:
  projects:
    - src/MyProject/MyProject.csproj
\`\`\`

### Build and deploy

Generate the static site and deploy it to your hosting provider:

\`\`\`bash
mokadocs build --output ./dist
\`\`\`

The `dist/` folder contains the complete static site ready for deployment.
:::
```

## Link Cards

Link cards render a grid of clickable card elements, each linking to a different page or URL. They are ideal for navigation sections, related pages, or feature overviews.

### Basic Syntax

```markdown
:::link-cards
- [Getting Started](/guide/getting-started) — Set up your first MokaDocs project
- [Configuration](/guide/configuration) — Customize your documentation site
- [Markdown Features](/guide/markdown) — Learn about supported Markdown syntax
- [Deployment](/guide/deployment) — Deploy your site to production
:::
```

:::link-cards
- [Getting Started](/getting-started/quickstart) — Set up your first MokaDocs project
- [Configuration](/configuration/site-config) — Customize your documentation site
- [Markdown Features](/guide/markdown) — Learn about supported Markdown syntax
- [Deployment](/advanced/deployment) — Deploy your site to production
:::

### How Link Cards Work

Each list item inside the container follows a specific format:

```
- [Card Title](url) — Description text
```

| Part          | Source                | Description                          |
|---------------|-----------------------|--------------------------------------|
| Title         | Link text             | Displayed as the card heading        |
| URL           | Link href             | The destination when the card is clicked |
| Description   | Text after em-dash    | Displayed below the title            |

The cards are rendered in a responsive grid layout that adapts to the available width. On wider screens, cards display in multiple columns; on narrow screens, they stack vertically.

### External Links

Link cards work with both internal and external URLs:

```markdown
:::link-cards
- [GitHub Repository](https://github.com/example/mokadocs) — View the source code and contribute
- [NuGet Package](https://nuget.org/packages/MokaDocs) — Install from NuGet
- [API Reference](/api/) — Browse the full API documentation
- [Changelog](/changelog) — See what changed in each release
:::
```

## Code Groups

Code groups display multiple code blocks as a tabbed interface, allowing users to switch between different implementations, languages, or file contents. This differs from the general tabbed content feature in that code groups are specifically optimized for code blocks.

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

The tab label for each code block comes from the `title` attribute on the code fence. If no title is specified, the language identifier is used as the tab label.

````markdown
:::code-group
```json title="package.json"
{
  "name": "my-project",
  "version": "1.0.0"
}
```
```yaml title="mokadocs.yml"
title: My Project
version: 1.0.0
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
```yaml title="mokadocs.yml"
title: My Project
version: 1.0.0
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
dotnet add package MokaDocs --version 2.0.0
```
```powershell title="Package Manager"
Install-Package MokaDocs -Version 2.0.0
```
```xml title="PackageReference"
<PackageReference Include="MokaDocs" Version="2.0.0" />
```
:::
````

:::code-group
```bash title=".NET CLI"
dotnet add package MokaDocs --version 2.0.0
```
```powershell title="Package Manager"
Install-Package MokaDocs -Version 2.0.0
```
```xml title="PackageReference"
<PackageReference Include="MokaDocs" Version="2.0.0" />
```
:::

### Features

- Each tab gets its own syntax highlighting based on the code fence language
- The copy button works independently per tab, copying only the visible code
- Tab selection is preserved when switching between tabs
- Code groups support line numbers (`has-line-numbers` class) on individual code blocks

## Combining Components

Components can be used together to build rich documentation pages. For example, you might use steps with code groups inside each step, or cards alongside link cards.

```markdown
:::steps
### Choose your package manager

:::code-group
\`\`\`bash title="npm"
npm install mokadocs
\`\`\`
\`\`\`bash title="yarn"
yarn add mokadocs
\`\`\`
:::

### Configure your project

:::card{title="Configuration File" icon="settings"}
Create a `mokadocs.yml` file in your project root. See the
[configuration reference](/configuration/site-config) for all available options.
:::
:::
```
