---
title: OpenAPI Plugin
order: 4
---

# OpenAPI Plugin

The OpenAPI plugin generates REST API reference pages from an OpenAPI 2.0, 3.0 or 3.1 document (JSON or YAML). It creates an overview page, one page per tag and a page of models, with parameter tables, request and response schemas, and examples. The document comes from a file, or in the [ASP.NET Core host](/guide/aspnetcore#rest-api-openapi), from the app itself.

**Plugin ID:** `openapi`

## What It Does

Given an OpenAPI document, the plugin generates:

- An **index overview page** listing all endpoints grouped by tag, with the API's servers, how it authenticates, and a link to download the document
- **Per-tag detail pages** documenting each endpoint in that group, each endpoint under a heading you can link to
- A **models page** listing the document's schemas
- **HTTP method badges** with color coding for quick visual identification
- **Parameter tables** for path, query, header, and cookie parameters
- **Request bodies** with their content type and top-level properties, and **responses** with their schema type
- **Types with nullability**, such as `string | null`, and the allowed values of enums
- **Descriptions rendered as Markdown**, as OpenAPI defines them (see [Descriptions](#descriptions))
- **Links from schema names to the C# API reference**, when the site documents C# types too, and to the models page otherwise (see [Links to C# Types](#links-to-c-types))
- **Example JSON** when the specification provides examples

The pages are added to the search index, with an entry for each endpoint and each model. See [Sidebar Navigation](#sidebar-navigation) for how they get into the navigation.

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
| `document`    | string | None                 | The document itself, as JSON or YAML text. When it is set, `spec` and auto-discovery are not used, and an empty value generates no pages. The ASP.NET Core host sets it to the app's own document. |
| `label`       | string | `"REST API"`         | Title of the generated index page. With an automatic sidebar, it is also the section label. |
| `routePrefix` | string | `"/api"`             | The URL prefix for all generated API pages. A missing leading `/` is added. The ASP.NET Core host defaults it to `/rest-api`. |

::: warning
The default prefix `/api` is also where the C# API reference puts its index page when `content.projects` is set. Pages that share a route overwrite each other: the OpenAPI index replaces the C# one, and the build warns that `2 pages share the route '/api'`. On sites that document C# projects, use another prefix, such as `/rest-api`.
:::

### Several Documents

Each `openapi` entry in `plugins:` reads its own document with its own options, so one site can document several APIs. Give each entry its own `routePrefix`:

```yaml
plugins:
  - name: openapi
    options:
      spec: ./public-api.json
      routePrefix: /public-api
  - name: openapi
    options:
      spec: ./admin-api.json
      label: "Admin API"
      routePrefix: /admin-api
```

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
- A link to download the document (see [The Source Document](#the-source-document)), and one to the [models page](#models-page) when the document has schemas
- A **Servers** section (see [Servers](#servers)) and an **Authentication** section (see [Authentication](#authentication)), when the document has them
- One table per tag, sorted by tag name, listing each endpoint's method, path and summary
- Links from each path to the endpoint on its tag's detail page, such as `/rest-api/pets#list-pets`

Deprecated endpoints are shown struck through. The page's table of contents lists the Servers and Authentication sections and the tags.

### Per-Tag Detail Pages

For each tag used by an operation, the plugin generates a detail page at `{routePrefix}/{tag-slug}`. The slug is the tag name in lowercase with spaces, slashes and dots replaced by hyphens, so `Pet Store` becomes `pet-store`. An endpoint with several tags appears on each of their pages.

Endpoints without tags are grouped under **Other**, at `{routePrefix}/other`.

The tag's description, from the document's top-level `tags` list, opens the page. Each endpoint sits under a heading with an id, so `{routePrefix}/{tag-slug}#{anchor}` links straight to it, and the page's table of contents lists every endpoint as `GET /pets/{id}`. Search has an entry for each endpoint too. The anchor comes from the `operationId` when there is one, otherwise from the method and path:

| Endpoint | Anchor |
|---|---|
| `operationId: listPets` | `list-pets` |
| `operationId: Pets_GetById` | `pets-get-by-id` |
| `DELETE /pets/{id}`, no `operationId` | `delete-pets-id` |

A word in camelCase becomes separate words, and everything is lowercase. An anchor that is already taken on the page gets `-2`, `-3` and so on, so two endpoints with the same `operationId` on one page still get anchors of their own. Anchors depend only on the document, so they stay the same from build to build.

### Models Page

When the document has schemas (`components/schemas` in 3.x, `definitions` in 2.0), the plugin generates a page at `{routePrefix}/models` that lists them by name. Each schema has:

- A heading with an anchor, such as `{routePrefix}/models#pet`, made the same way as the endpoint anchors
- A link to the C# type's page, when the site documents a type with that name (see [Links to C# Types](#links-to-c-types))
- Its description
- Its type, unless it is an object with properties, and its enum values
- The table of its properties, as for a [request body](#request-body)

The table of contents lists every schema, and search has an entry for each. If a tag's page already has the route `{routePrefix}/models`, the models page moves to `{routePrefix}/models-2`.

### Sidebar Navigation

When `mokadocs.yaml` has no `nav:` section, the generated pages appear in the sidebar automatically, under the `label`. With a `nav:` section, they only appear if you add an item for the prefix:

```yaml
nav:
  - label: "REST API"
    path: /rest-api
```

The tag pages are listed by tag name, and the models page comes after them.

### The Source Document

The document is published next to the pages, at `{routePrefix}/openapi.json`, or `{routePrefix}/openapi.yaml` when it is YAML, and the index page links to it. The file has the document's text exactly as the plugin read it, so readers can load it into other tools. Each `openapi` entry publishes its own document under its own `routePrefix`. Like every other output file, it isn't written by `mokadocs validate` or `mokadocs doctor`.

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

The endpoint's heading shows its method and path (e.g., `/users/{id}`), followed by the schemes it needs (see [Authentication](#authentication)) and its summary. The description follows when it differs from the summary.

### Parameters

Parameters are documented in a table with the following columns:

- **Name** - The parameter name
- **Location** - Where the parameter appears: `path`, `query`, `header`, or `cookie`
- **Type** - The schema type, written as described in [Types](#types)
- **Required** - A `required` or `optional` badge
- **Description** - The parameter description from the specification, followed by the allowed values of an enum

Parameters defined on the path are merged with the operation's own. An operation parameter with the same name and location replaces the path-level one.

### Types

Types are written the same way in every table:

| Schema | Shown as |
|---|---|
| `type: string` | `string` |
| `type: integer`, `format: int32` | `integer (int32)` |
| A `$ref` to `#/components/schemas/Product` | `Product` |
| `type: array` of `Product` | `Product[]` |
| An object with only `additionalProperties: {type: string}` | `map<string, string>` |
| `type: [integer, string]` with a numeric `format` or a `pattern` | `integer (int32)` |
| A schema with no type, reference or properties | `any` |

Values that can be `null`, and alternatives, are joined with a bar:

- `type: [string, "null"]` (3.1), `nullable: true` (3.0), or `oneOf`/`anyOf` with a `{type: "null"}` alternative: `string | null`, `Product | null`
- `oneOf` or `anyOf` with several alternatives: `Cat | Dog`

ASP.NET Core 10 writes every number that may also arrive as a string as `type: [integer, string]` with a digits-only `pattern`. That is shown as the number. Without a numeric `format` or a `pattern`, such a list is a real union and is shown as `string | integer`.

`enum` values, and a 3.1 `const`, are listed after the description as **Values**. An enum without a `type` gets one from its values.

### Links to C# Types

When the site also has an API reference (`content.projects` in the CLI, `Assemblies` in the ASP.NET Core host), a schema name links to the page of the C# type with the same name. ASP.NET Core's generators name schemas after their types, so `Product` in the document is usually the `Product` class. A name links to a C# type only when exactly one documented type has it: with two `Product` types in different namespaces, there is no telling which one the schema describes. Generic types are named differently in the document (`PagedResultOfProduct`) and don't match.

A schema name without a C# type links to its entry on the [models page](#models-page) instead. A name that matches neither, such as a reference to another file, is shown as text.

### Descriptions

OpenAPI defines `description` fields as CommonMark, and the plugin renders them as Markdown: the document's, the tags', and those of operations, parameters, request bodies, responses, schemas, properties, servers and security schemes. Pipe tables, strikethrough and bare URLs work too. In a table cell, a description of one paragraph is shown without the paragraph around it.

A document can come from anywhere, so raw HTML in a description is shown as text, and a link whose scheme isn't `http`, `https` or `mailto` (such as `javascript:`) keeps its text and loses the link. Headings in a description get no ids.

Summaries are plain text, as the specification defines them, so `List *all* pets` is shown with its asterisks.

### Servers

The index page's **Servers** section lists each server's URL and description:

- **OpenAPI 3.x** - the top-level `servers` list. Each `{variable}` in a URL is replaced by its `default`, and a variable with an `enum` lists its values after the description. Servers on paths and operations are not shown.
- **OpenAPI 2.0** - a URL for each of the `schemes`, made from `host` and `basePath`, such as `https://api.example.com/v1`. Without `schemes`, the URL starts with `//`, the scheme the document is served with. Without a `host`, the URL is the `basePath` alone.

When the document names no server, there is no Servers section. Documents that Microsoft.AspNetCore.OpenApi and Swashbuckle write at build time name none.

### Authentication

The index page's **Authentication** section describes each security scheme in `components/securitySchemes` (`securityDefinitions` in 2.0), under a heading with the anchor `auth-{name}`, with its description and:

| Type | Shown |
|---|---|
| `apiKey` | The header, query parameter or cookie name, and where it goes |
| `http` | The scheme, such as `bearer` or `basic`, and the bearer format |
| `oauth2` | Each flow with its authorization, token and refresh URLs, and its scopes |
| `openIdConnect` | The discovery URL |

A 2.0 `basic` scheme is shown as `http` with the `basic` scheme. When the document sets a top-level `security`, the section says what endpoints need unless they say otherwise.

Each endpoint shows the schemes it needs under its heading, each linked to its entry in the Authentication section:

- An operation's own `security` replaces the document's. Without one, the document's applies.
- The entries of a `security` list are alternatives, joined with **or**. The schemes within one entry are all needed, joined with **+**. OAuth 2.0 scopes follow the scheme in parentheses: `oauth (read:pets) + key or bearer`.
- An empty entry (`{}`) makes authentication optional, shown as **Auth (optional)**.
- An empty list (`security: []`) means the endpoint needs no authentication. When the document's default needs some, the endpoint says **No authentication**.

### Request Body

If the endpoint accepts a request body, the plugin documents:

- The content type (the first one listed, e.g. `application/json`)
- The body's description
- The schema name, and for a schema with properties, a table of its top-level properties with their type, a `required` or `optional` badge, and description
- For a polymorphic schema (a named schema made of `oneOf` or `anyOf` alternatives), the alternatives

Nested objects are listed by their type name (or `object`) and not expanded. The properties of the parts of an `allOf` are listed together, as for a type that inherits from another. Whether the body itself is required is not shown.

### Response Schemas

Responses are listed in a table with three columns:

- **Status** - The status code or `default`, as a badge: green for `2xx`, amber for `3xx`, red for `4xx` and `5xx`, blue for `1xx` and `default`
- **Description** - The response description
- **Schema** - The type of the response schema, taken from its first content type and written as described in [Types](#types)

Response content types and the properties of response schemas are not shown.

### Examples

An entry shows at most two examples, as pretty-printed JSON code blocks that the default theme highlights:

- **Example Request** - from the request body's first content type: its `example`, otherwise the first of its `examples`, otherwise the first value in its schema's `examples` (3.1), otherwise its schema's `example` (3.0)
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
- `/rest-api/models` - The schemas
- `/rest-api/openapi.json` - The document itself

The pages are searchable through the MokaDocs search feature. Without a `nav:` section they appear in the sidebar under "REST API"; with one, add a `path: /rest-api` item as shown in [Sidebar Navigation](#sidebar-navigation).

## Specification Requirements

The plugin reads OpenAPI 2.0 (Swagger), 3.0 and 3.1 documents in JSON or YAML (`.yaml`/`.yml`). A file whose first non-whitespace character is `{` is read as JSON, anything else as YAML. The document needs an `openapi` or `swagger` version field; one that can't be read is a build error. The generated pages show only the parts of the document described on this page.

MokaDocs reads the document itself, JSON with System.Text.Json and YAML with YamlDotNet, and doesn't use an OpenAPI library. In YAML, unquoted `true`, `false`, `null` and numbers are read as those values, and anything quoted as text. Anchors and `<<` merge keys work.

The specification should include:

- `info` section with `title` and `version`
- `paths` with endpoint definitions
- `tags` on operations for grouping endpoints (recommended but not required), and a top-level `tags` list for their descriptions
- `components/schemas` for reusable schema definitions (referenced via `$ref`)
- `servers` and `components/securitySchemes` with `security`, when you want them documented

The plugin resolves `$ref` references within the document: to schemas, and to shared parameters, request bodies, responses and examples (`components/...` in 3.x, `definitions`, `parameters` and `responses` in 2.0). A reference to another file isn't followed; its schema is shown by name. A schema that refers to itself is shown by name at the point where it repeats instead of being expanded again.

OpenAPI 3.1 documents, such as those Microsoft.AspNetCore.OpenApi 10 writes by default, are read with their 3.1 keywords: type lists, `const`, schema `examples` arrays, and a `$ref` with keywords beside it (its `description` is the property's description). In a 3.0 document, `nullable: true` counts on any schema, including one that only wraps a `$ref` in `allOf`. In a 2.0 document, the `body` parameter is the request body, `formData` parameters are listed as the properties of a form body, and `x-nullable: true` makes a value nullable.

MokaDocs is tested against the documents that Microsoft.AspNetCore.OpenApi 9 and 10 and Swashbuckle 9 and 10 write for minimal APIs.
