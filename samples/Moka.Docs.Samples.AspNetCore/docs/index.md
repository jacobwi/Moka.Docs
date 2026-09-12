---
title: Product API
layout: landing
description: REST API for managing products, categories, and inventory
order: 1
---

# Product API

Documentation served straight out of the running application by
`Moka.Docs.AspNetCore`. No static site, no build step, no separate host.

[Get Started](/docs/guide/getting-started) [View API Reference](/docs/api)

## How this page gets here

- **Two calls** - `AddMokaDocs(...)` in the service registration and
  `MapMokaDocs()` on the app. That is the whole integration.
- **Markdown from disk** - everything under `DocsPath` is parsed at startup,
  including this page.
- **API reference by reflection** - types in the assemblies listed in
  `options.Assemblies` are documented from their XML comments.
- **Built in memory** - the same build pipeline the CLI uses runs over a virtual
  file system, and the result is held in memory rather than written to `_site`.
- **Cached in production** - `CacheOutput` is off in Development so edits show up
  on refresh, and on everywhere else so the site is built once.

---

## Running it

```bash
dotnet run --project samples/Moka.Docs.Samples.AspNetCore
```

Then open `/docs`. The API itself lives under `/api/products`.
