---
title: Release Changelog
order: 5
---

# Release Changelog Plugin

The Changelog plugin renders release notes as a rich, interactive timeline directly in your documentation. Each release is displayed with version badges, categorized entries, and visual indicators that make it easy for readers to scan your project's history.

**Plugin ID:** `mokadocs-changelog`

## What It Does

The plugin transforms `:::changelog` custom container blocks in your Markdown into a styled timeline UI. Each release entry gets a colored version badge, a collapsible body, and categorized sections with icons. Readers can filter entries by category and expand or collapse individual releases.

## Enabling the Plugin

Add the plugin to your `mokadocs.yaml` configuration:

```yaml
plugins:
  - name: mokadocs-changelog
```

No additional options are required. The plugin activates on any page that contains a `:::changelog` container.

## Markdown Syntax

Wrap your release notes in a `:::changelog` fenced container. Inside the container, each release is a level-2 heading (`##`) and each category is a level-3 heading (`###`).

### Basic Structure

````markdown
:::changelog

## v2.1.0 — 2025-06-15

### Added
- New dashboard analytics widget
- CSV export for reports

### Fixed
- Resolved timeout on large dataset queries
- Corrected currency formatting in invoices

## v2.0.0 — 2025-05-01

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
## vX.Y.Z — YYYY-MM-DD
```

The version number and date are extracted and displayed in the timeline UI. The date portion after the em dash is optional but recommended.

### Release Types

You can annotate a release heading with a `type` to control the badge color:

````markdown
## v1.0.0 — 2025-01-01 {type: initial}

## v2.0.0 — 2025-05-01 {type: major}

## v2.1.0 — 2025-06-15 {type: minor}

## v2.1.1 — 2025-06-20 {type: patch}
````

| Type | Badge Color | Use Case |
|---|---|---|
| `initial` | Blue | First public release |
| `major` | Red | Breaking changes, major new version |
| `minor` | Green | New features, backward-compatible |
| `patch` | Gray | Bug fixes and small improvements |

When no type is specified, the plugin infers it from the version number (major version bump, minor bump, or patch bump).

## Supported Categories

Each category is a level-3 heading inside a release. The plugin recognizes the following categories and renders each with a distinct icon:

| Category | Icon | Description |
|---|---|---|
| `### Added` | Plus circle | New features and capabilities |
| `### Changed` | Edit | Modifications to existing behavior |
| `### Fixed` | Check circle | Bug fixes |
| `### Breaking` | Alert triangle | Backward-incompatible changes |
| `### Deprecated` | Clock | Features scheduled for removal |
| `### Removed` | Trash | Features that have been removed |
| `### Security` | Shield | Security-related fixes and improvements |

Within each category, list individual items as standard Markdown bullet points.

## UI Features

### Timeline Layout

Releases are displayed along a vertical timeline with colored dots marking each version. Version badges appear next to each dot, using the color corresponding to the release type.

### Collapsible Entries

By default, the most recent release is expanded and all older releases are collapsed. Readers can click on any release heading to expand or collapse it. This keeps the page scannable when you have many releases.

### Category Filter Bar

A filter bar is displayed above the timeline, showing toggles for each category that appears in the changelog. Readers can show or hide releases by category (for example, showing only "Breaking" and "Security" entries) to find relevant information quickly.

### Scroll-In Animations

Timeline entries animate into view as the reader scrolls down the page. This provides a polished, progressive reveal effect. The animations respect the site-wide `showAnimations` setting and the operating system's `prefers-reduced-motion` preference.

## Full Example

A complete changelog page with multiple releases and categories:

````markdown
---
title: Changelog
order: 99
---

# Changelog

:::changelog

## v3.0.0 — 2025-07-01 {type: major}

### Breaking
- Dropped support for .NET 6; minimum is now .NET 8
- Renamed `IDocService` to `IDocumentService`

### Added
- Plugin hot-reload in dev server mode
- Built-in OpenTelemetry tracing

### Changed
- Upgraded Markdig to 0.38

## v2.2.0 — 2025-06-15 {type: minor}

### Added
- Dark mode code theme auto-pairing
- CSV export for search analytics

### Fixed
- Sidebar scroll position lost on navigation
- Broken anchor links with special characters

## v2.1.1 — 2025-06-02 {type: patch}

### Fixed
- Hot-reload crash when deleting a docs folder
- Incorrect page title on 404 page

### Security
- Updated dependency to patch CVE-2025-XXXXX

:::
````
