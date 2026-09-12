---
title: Release Changelog
order: 5
---

# Release Changelog Plugin

The Changelog plugin renders release notes as a timeline in your documentation. Each release shows its version, a badge for the release type, its date, and its changes grouped into categories with icons.

**Plugin ID:** `mokadocs-changelog`

## What It Does

MokaDocs always turns `:::changelog` blocks into timeline markup. The plugin adds the CSS and JavaScript on top: colored badges and timeline dots, collapsible releases, a category filter bar and scroll-in animations. Without the plugin, the block renders as unstyled HTML.

## Enabling the Plugin

Add the plugin to your `mokadocs.yaml` configuration:

```yaml
plugins:
  - name: mokadocs-changelog
```

The plugin has no options. It enhances every page that contains a `:::changelog` block.

## Markdown Syntax

Wrap your release notes in a `:::changelog` container and close it with `:::`. Inside the container, each release is a level-2 heading (`##`) and each category is a level-3 heading (`###`) followed by a bullet list.

### Basic Structure

````markdown
:::changelog

## v2.1.0 - 2025-06-15

### Added
- New dashboard analytics widget
- CSV export for reports

### Fixed
- Resolved timeout on large dataset queries
- Corrected currency formatting in invoices

## v2.0.0 - 2025-05-01

### Breaking
- Removed deprecated `v1/auth` endpoint
- Changed default pagination from 50 to 25

### Added
- OAuth 2.0 support with PKCE flow
- Multi-tenant workspace switching

### Changed
- Migrated database layer to PostgreSQL

:::
````

### Release Headings

Each release heading follows the format:

```markdown
## vX.Y.Z - YYYY-MM-DD
```

- **Version.** The leading `v` is optional, and the version is always displayed with a `v` in front. Write it as a single bare word: `## Unreleased` shows as `vUnreleased`, and `## [1.0.0]` shows as `v[1.0.0]`.
- **Date.** The date is optional. Put a space before the hyphen that starts it (an em dash also works), so prerelease versions such as `1.0.0-beta.2` keep their hyphen. A date that parses is displayed as `June 15, 2025`; anything else is shown as written.
- **Other `##` lines.** A `##` heading that doesn't fit this format, such as `## Version 1.5`, is ignored, and the categories under it are added to the release above it.

### Release Types

The release type sets the badge text and color. Add it at the end of the heading:

````markdown
## v1.0.0 - 2025-01-01 {type: initial}

## v2.0.0 - 2025-05-01 {type: major}

## v2.1.0 - 2025-06-15 {type: minor}

## v2.1.1 - 2025-06-20 {type: patch}
````

A `type:` line under the heading works too, and overrides a type given in the heading:

````markdown
## v2.1.1 - 2025-06-20
type: patch
````

| Type | Badge Color | Hex | Use Case |
|---|---|---|---|
| `initial` | Violet | `#8b5cf6` | First public release |
| `major` | Red | `#ef4444` | Breaking changes, major new version |
| `minor` | Blue | `#3b82f6` | New features, backward-compatible |
| `patch` | Green | `#22c55e` | Bug fixes and small improvements |

When a release has no type, it is inferred from the version number: `X.0.0` is `major`, `X.Y.0` is `minor`, and anything else is `patch`. Prerelease and build suffixes are ignored, so `1.0.0-beta.2` counts as `major`. A version that doesn't start with `X.Y` digits, such as `Unreleased`, counts as `patch`, and `initial` is never inferred. Other type names are accepted and shown as the badge text, without a badge color.

## Supported Categories

Each category is a level-3 heading inside a release. These categories get their own icon and color:

| Category | Icon | Description |
|---|---|---|
| `### Added` | ✚ plus | New features and capabilities |
| `### Changed` | ✎ pencil | Modifications to existing behavior |
| `### Fixed` | 🔧 wrench | Bug fixes |
| `### Breaking` | ⚠ warning sign | Backward-incompatible changes |
| `### Deprecated` | ⚡ lightning bolt | Features scheduled for removal |
| `### Removed` | ✕ cross | Features that have been removed |
| `### Security` | 🛡 shield | Security-related fixes and improvements |

Category names match in any letter case. Any other `###` heading, such as `### Performance`, is shown with a bullet (•) and gets no button in the filter bar.

Within each category, write one item per `-` or `*` bullet line:

- Items support `` `code` `` spans and `**bold**` text. Other Markdown, such as links and italics, shows up as literal text.
- Indented bullets are added to the same flat list.
- Lines that aren't bullets, such as a paragraph or a wrapped continuation line, are ignored.

## UI Features

### Timeline Layout

Releases are displayed along a vertical timeline with a colored dot for each version. The badge next to the version uses the color of the release type.

### Collapsible Entries

The first release in the block starts expanded and all other releases start collapsed, whatever their dates, so put the newest release first. Readers can click any release header to expand or collapse it.

### Category Filter Bar

A filter bar above the timeline has an **All** button and one button for each built-in category in the changelog, with its item count. Selecting a category hides the other category sections in every release. Releases that don't have the selected category stay on the timeline as empty headers. Only one category can be selected at a time.

### Scroll-In Animations

Releases fade in as the reader scrolls them into view. With the default theme, releases appear without the fade when `showAnimations` is `false` or the operating system requests reduced motion (`prefers-reduced-motion`).

### Dark Mode

The timeline's dark colors follow the operating system's color scheme (`prefers-color-scheme`), not the site's light/dark toggle.

## Full Example

A complete changelog page with multiple releases and categories:

````markdown
---
title: Changelog
order: 99
---

# Changelog

:::changelog

## v3.0.0 - 2025-07-01 {type: major}

### Breaking
- Dropped support for .NET 6; minimum is now .NET 8
- Renamed `IDocService` to `IDocumentService`

### Added
- Plugin hot-reload in dev server mode
- Built-in OpenTelemetry tracing

### Changed
- Upgraded Markdig to 0.38

## v2.2.0 - 2025-06-15 {type: minor}

### Added
- Dark mode code theme auto-pairing
- CSV export for search analytics

### Fixed
- Sidebar scroll position lost on navigation
- Broken anchor links with special characters

## v2.1.1 - 2025-06-02 {type: patch}

### Fixed
- Hot-reload crash when deleting a docs folder
- Incorrect page title on 404 page

### Security
- Updated dependency to patch CVE-2025-XXXXX

:::
````
