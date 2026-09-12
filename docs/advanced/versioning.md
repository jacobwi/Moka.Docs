---
title: Versioning
order: 3
---

# Versioning

Versioning in MokaDocs is a version dropdown in the site header. It lists the versions you configure, and each entry links to a fixed path on the same site. A build runs once and writes one site: MokaDocs doesn't check out branches or build several versions for you. You build each version yourself and deploy it at the path its entry links to.

## Enabling the Dropdown

Add a `versioning` section under `features` in your `mokadocs.yaml`:

```yaml
features:
  versioning:
    enabled: true
    versions:
      - label: "v2.0"
        default: true
      - label: "v1.0"
      - label: "v3.0-beta"
        prerelease: true
```

The dropdown appears on pages that use the `default` layout when versioning is enabled and at least one version is listed. The `landing` layout has no dropdown. To hide it, set `theme.options.showVersionSelector: false`.

### Configuration Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `enabled` | boolean | `false` | Turns the dropdown on |
| `versions` | list | empty | The versions to list, in dropdown order |
| `strategy` | string | `directory` | Parsed but never used. Every value behaves the same |

### Version Entry Fields

| Field | Type | Description |
|-------|------|-------------|
| `label` | string | Text shown in the dropdown. The URL slug is derived from it |
| `default` | boolean | Marks the version deployed at the site root. Set it on exactly one entry |
| `prerelease` | boolean | Adds a "pre" badge to the entry |
| `branch` | string | Optional. Parsed but never used; MokaDocs has no git integration |

## How the Dropdown Works

- The button shows the default version's label on every page of the build. The default version is the first entry with `default: true`, else the first entry that isn't a prerelease, else the first entry.
- An entry with `default: true` links to the site root (`<base path>/`) and gets a "latest" badge.
- Every other entry links to `<base path>/<slug>/`.
- The links are plain same-site links to each version's home page. They don't map the current page to its counterpart in another version, and a version that isn't deployed at its path returns a 404.

If no entry sets `default: true`, the button still shows the fallback label, but no entry links to the site root or gets the "latest" badge.

The slug is the label trimmed and lowercased, with spaces turned into hyphens and every character other than letters, digits, dots and hyphens removed. `v2.0` stays `v2.0`, and `Version 1 (LTS)` becomes `version-1-lts`.

## Deploying Several Versions

1. Build the default version and deploy its output at the site root.
2. For every other version, check out that version's docs yourself, build them with `--base-path /<slug>` so their links and assets resolve under that path, and deploy the output to `/<slug>/`. If the site itself has a base path, put it first, as in `--base-path /MyRepo/v1.0`.

::: warning
The dropdown's links include the build's base path. In a build made with `--base-path /v1.0`, the default entry links to `/v1.0/` (that build's own home page) and the other entries link to `/v1.0/<slug>/`, which doesn't exist. Only the build deployed at the site root gets working dropdown links. In the other builds, turn the dropdown off with `theme.options.showVersionSelector: false`.
:::

The label on the button is whatever the config says. Nothing reads it from your package version, so update it by hand when you release.

## Not Implemented

These settings are parsed but have no effect:

- `strategy`. There is no `directory` or `dropdown-only` mode, and a build never writes per-version subfolders.
- `branch`. MokaDocs never runs git.
- `version` in page front matter, such as `version: ">=2.0"`. The page is built either way.

## Internals

`VersionManager` in `Moka.Docs.Versioning` maps `features.versioning` to a list of `DocVersion` records and picks the default version. The list is empty when versioning is disabled. `mokadocs build` and `mokadocs serve` copy the list into the build context and set the current version to the default version.

`DocVersion` lives in `Moka.Docs.Core.Content`:

```csharp
public sealed record DocVersion
{
    public required string Label { get; init; }  // Display label, e.g. "v2.0"
    public required string Slug { get; init; }   // URL path segment derived from Label
    public bool IsDefault { get; init; }         // Set by default: true
    public bool IsPrerelease { get; init; }      // Set by prerelease: true
}
```

`ScribanTemplateEngine` passes the list to templates as `versions`, where each item has `label`, `slug`, `is_default` and `is_prerelease`, and the default version's label as `current_version`. `VersionManager` also has `GetOutputPath`, `GetBranch` and `FindByLabel`, but nothing in the build calls them.
