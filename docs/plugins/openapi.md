---
title: OpenAPI Plugin
order: 4
---

# OpenAPI Plugin

The OpenAPI plugin generates REST API reference pages from an OpenAPI specification file (JSON or YAML). It creates an overview page and one page per tag, with parameter tables, request and response schemas, and examples.

**Plugin ID:** `openapi`

## What It Does

Given an OpenAPI document, the plugin generates:

- An **index overview page** listing all endpoints grouped by tag
- **Per-tag detail pages** documenting each endpoint in that group
- **HTTP method badges** with color coding for quick visual identification
- **Parameter tables** for path, query, header, and cookie parameters
- **Request bodies** with their content type and top-level properties, and **responses** with their schema type
- **Example JSON** when the specification provides examples

The pages are added to the search index. See [Sidebar Navigation](#sidebar-navigation) for how they get into the navigation.

## Configuration

### Basic Configuration

```yaml
plugins:
  - name: openapi
    options:
      spec: ./openapi.json
      label: "REST API"
      routePrefix: /rest-api
```

### Configuration Options

| Option        | Type   | Default              | Description |
|---------------|--------|----------------------|-------------|
| `spec`        | string | Auto-discovered      | Path to the OpenAPI spec file (JSON or YAML), relative to `mokadocs.yaml`. Absolute paths also work. |
| `label`       | string | `"REST API"`         | Title of the generated index page. With an automatic sidebar, it is also the section label. |
| `routePrefix` | string | `"/api"`             | The URL prefix for all generated API pages. Include the leading `/`. |

::: warning
The default prefix `/api` is also where the C# API reference puts its index page when `content.projects` is set. Pages that share a route overwrite each other: the OpenAPI index replaces the C# one, and the build warns that `2 pages share the route '/api'`. On sites that document C# projects, use another prefix, such as `/rest-api`.
:::

### Auto-Discovery

If the `spec` option is not set, the plugin looks in the directory that contains `mokadocs.yaml`, then in the `content.docs` directory, for these file names in order:

1. `openapi.json`
2. `openapi.yaml`
3. `openapi.yml`
4. `swagger.json`
5. `swagger.yaml`

The first file found is used. If `spec` is set but the file doesn't exist, there is no fallback to auto-discovery. When no file is found, the plugin adds a build warning (`OpenAPI plugin: No OpenAPI spec file found. Skipping.`) and generates nothing. A spec that can't be read or parsed is a build error, so `mokadocs build` exits with code 1.

## Generated Pages

### Index Overview Page

The plugin creates an overview page at the configured `routePrefix`. This page includes:

- The API title and description from the `info` section of the specification
- The API version
- One table per tag, sorted by tag name, listing each endpoint's method, path and summary
- Links from each path to its tag's detail page

Deprecated endpoints are shown struck through.

### Per-Tag Detail Pages

For each tag used by an operation, the plugin generates a detail page at `{routePrefix}/{tag-slug}`. The slug is the tag name in lowercase with spaces, slashes and dots replaced by hyphens, so `Pet Store` becomes `pet-store`. An endpoint with several tags appears on each of their pages.

Endpoints without tags are grouped under **Other**, at `{routePrefix}/other`.

### Sidebar Navigation

When `mokadocs.yaml` has no `nav:` section, the generated pages appear in the sidebar automatically, under the `label`. With a `nav:` section, they only appear if you add an item for the prefix:

```yaml
nav:
  - label: "REST API"
    path: /rest-api
```

## Endpoint Documentation

Each endpoint entry on a detail page documents the following:

### HTTP Method Badges

Methods are displayed with color-coded badges for quick visual scanning:

| Method    | Color      | Hex       |
|-----------|------------|-----------|
| `GET`     | Green      | `#22c55e` |
| `POST`    | Blue       | `#3b82f6` |
| `PUT`     | Amber      | `#f59e0b` |
| `PATCH`   | Violet     | `#8b5cf6` |
| `DELETE`  | Red        | `#ef4444` |
| `HEAD`    | Slate gray | `#64748b` |
| `OPTIONS` | Slate gray | `#64748b` |

Any other method uses the `GET` color. Operations marked `deprecated` also get a **Deprecated** badge.

### Path and Summary

The endpoint path (e.g., `/users/{id}`) and its summary are displayed at the top of the entry. The description follows when it differs from the summary.

### Parameters

Parameters are documented in a table with the following columns:

- **Name** - The parameter name
- **Location** - Where the parameter appears: `path`, `query`, `header`, or `cookie`
- **Type** - The schema type, such as `string`, `integer (int64)`, `string[]` or a referenced schema name
- **Required** - A `required` or `optional` badge
- **Description** - The parameter description from the specification

Parameters defined on the path are merged with the operation's own. An operation parameter with the same name and location replaces the path-level one.

### Request Body

If the endpoint accepts a request body, the plugin documents:

- The content type (the first one listed, e.g. `application/json`)
- The body's description
- The schema name, and for a schema with properties, a table of its top-level properties with their type, a `required` or `optional` badge, and description

Nested objects are listed by their type name (or `object`) and not expanded. Whether the body itself is required is not shown.

### Response Schemas

Responses are listed in a table with three columns:

- **Status** - The status code or `default`, as a badge: green for `2xx`, amber for `3xx`, red for `4xx` and `5xx`, blue for `1xx` and `default`
- **Description** - The response description
- **Schema** - The type name of the response schema, taken from its first content type

Response content types and the properties of response schemas are not shown.

### Examples

An entry shows at most two examples, as pretty-printed JSON code blocks that the default theme highlights:

- **Example Request** - from the request body's first content type: its `example`, otherwise the first of its `examples`, otherwise the first value in its schema's `examples`
- **Example Response** - found the same way, from the first `2xx` response that has one

Parameter examples are not read.

## Usage Example

Given a project with an `openapi.json` file describing a users API, the following configuration:

```yaml
plugins:
  - name: openapi
    options:
      spec: ./openapi.json
      label: "REST API"
      routePrefix: /rest-api
```

Produces documentation pages such as:

- `/rest-api` - Overview page listing all endpoints
- `/rest-api/users` - Detail page for endpoints tagged with "Users"
- `/rest-api/authentication` - Detail page for endpoints tagged with "Authentication"

The pages are searchable through the MokaDocs search feature. Without a `nav:` section they appear in the sidebar under "REST API"; with one, add a `path: /rest-api` item as shown in [Sidebar Navigation](#sidebar-navigation).

## Specification Requirements

The plugin reads the specification with `Microsoft.OpenApi` 2.x and its YAML reader, which accept OpenAPI 2.0, 3.0 and 3.1 documents in JSON or YAML (`.yaml`/`.yml`). A file whose first non-whitespace character is `{` is read as JSON, anything else as YAML. The generated pages show only the parts of the document described on this page.

The specification should include:

- `info` section with `title` and `version`
- `paths` with endpoint definitions
- `tags` on operations for grouping endpoints (recommended but not required)
- `components/schemas` for reusable schema definitions (referenced via `$ref`)

The plugin resolves `$ref` references within the specification, including references to shared schemas in `components/schemas`. A schema that refers to itself is shown by name at the point where it repeats instead of being expanded again.
