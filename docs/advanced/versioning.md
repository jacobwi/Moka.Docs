---
title: Versioning
order: 3
---

# Versioning

MokaDocs labels your docs with the version of the package they document, and can build every release of them into one site from git: each version in its own folder, with a header dropdown that switches between them and keeps you on the same page.

## The Version Dropdown

Turn versioning on in `mokadocs.yaml`:

```yaml
features:
  versioning:
    enabled: true
```

With no `versions` listed, the site has one version, and its label comes from your package version: `v{major}.{minor}` of the first packable project in `content.projects`. MokaDocs reads `PackageVersion`, `Version` or `VersionPrefix` (with `VersionSuffix`) from the project file, or from the nearest `Directory.Build.props` above it that sets one, like the NuGet install widget on the API page does. A project at 1.8.0 shows `v1.8`. Bump the version in your project and the label follows; there's nothing to edit in `mokadocs.yaml` at release time.

A version that can't be read (no project, or a value built from other MSBuild properties such as `$(Major).1`) gives the label `latest`. A version with a suffix such as `2.0.0-beta` makes the version a prerelease, with a "pre" badge.

The dropdown appears in the header of every page, landing pages included. To hide it, set `theme.options.showVersionSelector: false`.

## Building Several Versions

List the versions and the git branch or tag each one is built from:

```yaml
features:
  versioning:
    enabled: true
    versions:
      - label: next
        branch: main
        prerelease: true
      - tag: v2.0.0
        default: true
      - tag: v1.0.0
```

`mokadocs build` then builds every version into its own folder of the output, and the default version at the site root as well:

```
_site/
├── index.html        the default version (v2.0), at the root
├── guide/...
├── v2.0/             the default version again, at a fixed URL
├── v1.0/
└── next/
```

- Versions without a `label` take it from the package version in their own ref, so the entries above are `next`, `v2.0` and `v1.0`.
- A version from a ref is checked out into a temporary git worktree in the system temp folder, built, and the worktree is removed afterwards. Your working tree, index and branches are never touched.
- The branch you have checked out is built from the working tree, uncommitted changes included, and so is a version with neither `branch` nor `tag`.
- A branch is looked up as a local branch first, then as `origin/{branch}`. A `tag` wins over `branch` when an entry sets both.
- Each version is built with the `mokadocs.yaml` at the same path in its ref, so an old release keeps its own navigation, theme options and plugins. A ref without one is built with the current config. Where the site is published always comes from the current config: `site.url`, the whole `build` section and the version list.
- The default version is the first entry with `default: true`, else the first that isn't a prerelease, else the first. Building it at the root keeps the URLs of a site that had one version working, and its folder gives each release a URL that doesn't move when a newer version becomes the default.
- Each version has its own search index, sitemap and theme files, so search only finds pages of the version you are reading.

### The Dropdown in a Versioned Site

Every page lists all the versions and marks the one you are reading. Each entry links to the same page in that version when it has one, and to that version's home page when it doesn't. The links are worked out during the build, so they work without JavaScript and on any static host. The default version's entry links to its copy at the root.

### Rebuilding One Version

```bash
mokadocs build --version v1.0
```

builds that version only, by slug or label. Its folder is cleaned and rebuilt, the other folders stay as an earlier build left them, and the dropdown keeps linking to their pages. Naming the default version rebuilds its folder and the site root, without cleaning the root, since the root holds the other versions' folders. A full build cleans and rebuilds everything.

`--version` also works when no version names a ref: it builds the working tree as that version, into its folder, which is how you build an old release from a checkout of your own.

`mokadocs build --watch` rebuilds the versions that come from the working tree on each change.

### Problems

A ref that doesn't exist, a checkout that fails or a version whose build fails is an error that names the version, and `mokadocs build` exits 1. The other versions are still built, and a version whose ref is missing is left out of the dropdown. Two versions whose labels give the same folder name are an error too.

`mokadocs validate` and `mokadocs doctor` check that every ref exists without checking anything out or building it, and build the working tree's version.

### Serving

`mokadocs serve` builds the working tree only, as the version it is: the one without a ref or with the checked-out branch, else the default. The dropdown lists every version, but the dev server only has this one.

### In CI

A checkout in CI usually has the current commit and nothing else, so fetch the tags and branches your versions name. With GitHub Actions:

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

Branches that only exist as `origin/{branch}` after the fetch are found.

## Pages for Some Versions

A page can say which versions it belongs to with the [`version`](/configuration/front-matter#version) front matter key. When versioning is on, a page whose range doesn't include the version being built is left out of that version's site, sidebar and search:

```yaml
---
title: Streaming API
version: ">=2.0"
---
```

The version being built is the number in its label (`v2.0` is 2.0.0), or the package version for a label without one, such as `next`. With versioning off the key is ignored.

## Deploying Versions Yourself

With `strategy: dropdown-only`, or when no version names a ref, a build makes one site from the working tree, and the dropdown lists the other versions without building them. The default version links to the site root and every other version to `{base path}/{slug}/`, so deploy each one there yourself. `mokadocs build --version v1.0` builds one into its folder with the right base path and links: from its tag or branch when the entry names one, else from the working tree.

## Configuration Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `enabled` | boolean | `false` | Turns versioning on |
| `strategy` | string | `directory` | `directory` builds the versions that name a ref; `dropdown-only` only lists them |
| `versions` | list | empty | The versions, in dropdown order. Empty means one version labelled from the package |

| Version field | Type | Description |
|-------|------|-------------|
| `label` | string | Text in the dropdown, and the source of the URL slug and folder name. Derived from the package version when left out |
| `tag` | string | Git tag to build the version from |
| `branch` | string | Git branch to build the version from |
| `default` | boolean | Also built at the site root, with a "latest" badge |
| `prerelease` | boolean | Adds a "pre" badge |

The slug is the label trimmed and lowercased, with spaces turned into hyphens and every character other than letters, digits, dots and hyphens removed. `v2.0` stays `v2.0`, and `Version 1 (LTS)` becomes `version-1-lts`.

## ASP.NET Core Host

The host serves one version. `MokaDocsOptions.Version` sets its label, and without it there's no dropdown. It isn't derived from the app's assembly version: the app's version is rarely the version of the library it documents, and a dropdown appearing in every app after an upgrade would be a surprise. Page `version` ranges are matched against this label.

## Internals

`VersionManager.ApplyTo(context)` in `Moka.Docs.Versioning` fills `BuildContext.Versions` and `CurrentVersion` for a build of the working tree; `build`, `serve`, `validate` and `doctor` call it. `VersionedBuildOrchestrator` runs the versioned build: one pipeline per version, each paused before rendering until every version's pages are known, then rendered and written one at a time, the site root first. `VersionGatePhase` (order 450) applies the front matter ranges.

`DocVersion` in `Moka.Docs.Core.Content` has `Label`, `Slug`, `IsDefault` and `IsPrerelease`, plus `Number` (the version ranges are matched against), `UrlPath` (where the version is published) and `Routes` (its pages, for the dropdown links).

Templates get `versions`, where each item has `label`, `slug`, `is_default`, `is_prerelease`, `is_current` and `url` (the link from the page being rendered), and `current_version`, the label of the version being built.
