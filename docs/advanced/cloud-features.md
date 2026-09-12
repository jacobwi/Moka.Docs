---
title: Cloud Features
order: 6
requires: Cloud
---

# Cloud Features

MokaDocs has no working cloud features. The configuration has switches for AI summaries, PDF export, analytics and custom domains, but nothing reads them, and turning them on doesn't change the generated site.

## Configuration

The settings go under a top-level `cloud:` key:

```yaml
cloud:
  enabled: false
  apiKey: "your-api-key"
  features:
    aiSummaries: false
    pdfExport: false
    analytics: false
    customDomain: false
```

Every switch defaults to `false`. A `cloud:` block nested under `features:` isn't recognized, and the config reader ignores unknown keys without a warning.

## What Reads These Settings

Nothing in the `mokadocs` CLI, the build pipeline or the ASP.NET Core integration. The `Moka.Docs.Cloud` package contains `CloudFeatureService`, which reports which switches are on, and `CredentialStore`, which returns `apiKey` (or the `MOKADOCS_API_KEY` environment variable) when `enabled` is `true`. No MokaDocs code registers or calls either one.

## Custom Domains

A custom domain on GitHub Pages doesn't need any of this. Put a `CNAME` file directly in your docs folder and the build copies it to the root of the output. See [Deployment](/advanced/deployment).
