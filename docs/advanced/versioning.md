---
title: Versioning
order: 3
---

# Versioning

MokaDocs supports documentation versioning, allowing you to publish and maintain docs for multiple releases of your library or framework. Readers can switch between versions using a dropdown selector in the site header.

## Enabling Versioning

Add a `versioning` section under `features` in your `mokadocs.yaml`:

```yaml
features:
  versioning:
    enabled: true
    strategy: dropdown-only
    versions:
      - label: "v2.0"
        branch: main
        default: true
      - label: "v1.0"
        branch: release/1.0
      - label: "v3.0-beta"
        branch: dev
        prerelease: true
```

### Configuration Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `enabled` | boolean | Yes | Set to `true` to activate versioning |
| `strategy` | string | Yes | Either `dropdown-only` or `directory` |
| `versions` | list | Yes | List of version definitions |

### Version Entry Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `label` | string | Yes | Display label shown in the version selector (e.g., `"v2.0"`, `"v1.0-beta"`) |
| `branch` | string | Yes | Git branch that contains the documentation source for this version |
| `default` | boolean | No | Set to `true` for the version served at the root URL. Exactly one version should be marked as default |
| `prerelease` | boolean | No | Set to `true` to mark this version as a prerelease. Prerelease versions are displayed with distinct styling in the selector |

## Strategies

MokaDocs offers two versioning strategies that control how versioned content is organized and served.

### dropdown-only

```yaml
strategy: dropdown-only
```

The `dropdown-only` strategy adds a version selector dropdown to the site header without changing the output directory structure. This is the simpler approach and works well when you build and deploy each version independently.

**How it works:**
- The version selector appears in the header bar, listing all configured versions.
- Selecting a different version navigates to that version's deployed URL.
- Each version is built and deployed as a separate site (or subdomain).
- The output directory structure is flat — no version-specific subdirectories.

**Best for:**
- Projects that deploy each version to a separate subdomain (e.g., `v1.docs.example.com`, `v2.docs.example.com`).
- CI/CD pipelines that build each branch independently.

### directory

```yaml
strategy: directory
```

The `directory` strategy generates a separate output subdirectory for each version within a single build. All versions are deployed together as one site.

**How it works:**
- Each version is output to its own subdirectory: `_site/v1.0/`, `_site/v2.0/`, etc.
- The default version is also served at the root (`_site/`).
- The version selector dropdown links between version subdirectories on the same domain.
- A single build produces documentation for all versions.

**Output structure example:**
```
_site/
  index.html              # Default version (v2.0)
  guide/
    getting-started/
      index.html
  v1.0/
    index.html
    guide/
      getting-started/
        index.html
  v2.0/
    index.html
    guide/
      getting-started/
        index.html
  v3.0-beta/
    index.html
    guide/
      getting-started/
        index.html
```

**Best for:**
- Projects that want a single deployment containing all versions.
- Static hosting platforms where subdomain routing is not available.

## Version Selector UI

The version selector appears as a dropdown in the site header bar. It displays the currently active version and lists all available versions when clicked.

**Default versions** are shown normally with their label text.

**Prerelease versions** are displayed with distinct visual styling — typically a badge or italic text — to indicate they are not yet stable. This helps readers understand they are viewing documentation for an unreleased version.

When a reader selects a version, the browser navigates to the equivalent page in the selected version. If the equivalent page does not exist in the target version, the reader is directed to that version's homepage.

## Page-Level Version Constraints

You can restrict individual pages to specific versions using the `version` field in front matter:

```yaml
---
title: New Feature Guide
version: ">=2.0"
---
```

### Supported Constraints

| Constraint | Meaning |
|------------|---------|
| `">=2.0"` | Include this page only in version 2.0 and later |
| `"<2.0"` | Include this page only in versions before 2.0 |
| `"1.0"` | Include this page only in version 1.0 |
| `">=1.0 <3.0"` | Include this page in versions 1.0 through 2.x |

Pages that do not match the current version's constraint are excluded from that version's build output, navigation, and search index.

## Internal Architecture

### DocVersion Model

Each configured version is represented by a `DocVersion` object:

```csharp
public class DocVersion
{
    public string Label { get; set; }        // Display label (e.g., "v2.0")
    public string Slug { get; set; }         // URL-safe identifier (e.g., "v2.0")
    public string Branch { get; set; }       // Git branch name
    public bool IsDefault { get; set; }      // Whether this is the root version
    public bool IsPrerelease { get; set; }   // Whether to show prerelease styling
}
```

The `Slug` is automatically derived from the `Label` by converting it to a URL-safe format (lowercased, special characters removed).

### VersionManager

The `VersionManager` class handles version-related logic during the build:

- **Resolving the active version** based on the current branch or explicit selection.
- **Computing output paths** for the `directory` strategy (prepending the version slug to all routes).
- **Filtering pages** based on `version` front matter constraints.
- **Generating version selector data** for the template engine.
- **Branch mapping** to associate Git branches with version labels.

## Example: Full Versioned Configuration

```yaml
site:
  title: MyLibrary Docs
  description: Documentation for MyLibrary

features:
  versioning:
    enabled: true
    strategy: directory
    versions:
      - label: "v3.0-beta"
        branch: dev
        prerelease: true
      - label: "v2.0"
        branch: main
        default: true
      - label: "v1.0"
        branch: release/1.0

build:
  output: _site
```

With this configuration, running `mokadocs build` produces a `_site/` directory containing documentation for all three versions, with v2.0 as the default at the root URL.
