---
title: OpenAPI Plugin
order: 4
---

# OpenAPI Plugin

The OpenAPI plugin generates API reference documentation pages from OpenAPI 3.0 JSON specification files. It creates structured, navigable pages for your REST API endpoints, grouped by tags, with full documentation of parameters, request bodies, response schemas, and examples.

**Plugin ID:** `openapi`

## What It Does

Given an OpenAPI 3.0 JSON specification, the plugin automatically generates:

- An **index overview page** listing all available endpoints grouped by tag
- **Per-tag detail pages** with full documentation for each endpoint in that group
- **HTTP method badges** with color coding for quick visual identification
- **Parameter documentation** including path, query, header, and cookie parameters
- **Request and response schema** rendering with type information and descriptions
- **Example values** when provided in the specification

These pages are integrated into the MokaDocs navigation and are searchable alongside your regular documentation.

## Configuration

### Basic Configuration

```yaml
plugins:
  - name: openapi
    options:
      spec: ./openapi.json
      label: "REST API"
      routePrefix: /api
```

### Configuration Options

| Option        | Type   | Default              | Description |
|---------------|--------|----------------------|-------------|
| `spec`        | string | Auto-discovered      | Path to the OpenAPI 3.0 JSON file, relative to the project root. |
| `label`       | string | `"API Reference"`    | The display label used in navigation for the generated API section. |
| `routePrefix` | string | `"/api"`             | The URL prefix for all generated API pages. |

### Auto-Discovery

If the `spec` option is not specified, the plugin searches for specification files in the project root in the following order:

1. `openapi.json`
2. `swagger.json`

The first file found is used. If neither file exists and no `spec` path is configured, the plugin logs a warning and skips generation.

## Generated Pages

### Index Overview Page

The plugin creates an overview page at the configured `routePrefix` (default: `/api`). This page includes:

- The API title and description from the `info` section of the specification
- The API version
- A summary table of all endpoints grouped by tag
- Links to the per-tag detail pages

### Per-Tag Detail Pages

For each tag defined in the specification, the plugin generates a detail page at `{routePrefix}/{tag-slug}`. Each detail page includes full documentation for every endpoint associated with that tag.

Endpoints without tags are grouped under a "Default" section.

## Endpoint Documentation

Each endpoint entry on a detail page documents the following:

### HTTP Method Badges

Methods are displayed with color-coded badges for quick visual scanning:

| Method   | Color  |
|----------|--------|
| `GET`    | Green  |
| `POST`   | Blue   |
| `PUT`    | Orange |
| `PATCH`  | Yellow |
| `DELETE` | Red    |
| `HEAD`   | Purple |
| `OPTIONS`| Gray   |

### Path and Summary

The endpoint path (e.g., `/users/{id}`) and its summary or description from the specification are displayed prominently.

### Parameters

Parameters are documented in a table with the following columns:

- **Name** -- The parameter name
- **In** -- Where the parameter appears: `path`, `query`, `header`, or `cookie`
- **Type** -- The data type from the schema (e.g., `string`, `integer`, `array`)
- **Required** -- Whether the parameter is mandatory
- **Description** -- The parameter description from the specification

### Request Body

If the endpoint accepts a request body, the plugin documents:

- The content type (e.g., `application/json`)
- Whether the body is required
- The schema structure with property names, types, and descriptions
- Example values if provided in the specification

### Response Schemas

Each documented response status code includes:

- The HTTP status code and its description
- The response content type
- The response schema with property details
- Example responses if provided

Schemas are rendered recursively, so nested objects and arrays display their full structure with proper indentation.

### Examples

When the specification includes `example` or `examples` fields on parameters, request bodies, or responses, they are rendered in formatted code blocks. JSON examples are syntax-highlighted for readability.

## Usage Example

Given a project with an `openapi.json` file describing a users API, the following configuration:

```yaml
plugins:
  - name: openapi
    options:
      spec: ./openapi.json
      label: "REST API"
      routePrefix: /api
```

Produces documentation pages such as:

- `/api` -- Overview page listing all endpoints
- `/api/users` -- Detail page for endpoints tagged with "Users"
- `/api/authentication` -- Detail page for endpoints tagged with "Authentication"

These pages appear in the site navigation under the "REST API" label and are fully searchable through the MokaDocs search feature.

## Specification Requirements

The plugin supports **OpenAPI 3.0** format in JSON. YAML specification files are not currently supported. If your specification is in YAML format, convert it to JSON before using it with this plugin.

The specification should include:

- `info` section with `title` and `version`
- `paths` with endpoint definitions
- `tags` for grouping endpoints (recommended but not required)
- `components/schemas` for reusable schema definitions (referenced via `$ref`)

The plugin resolves `$ref` references within the specification, including references to shared schemas in `components/schemas`.
