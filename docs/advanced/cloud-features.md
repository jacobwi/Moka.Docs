---
title: Cloud Features
order: 6
requires: Cloud
---

# Cloud Features

MokaDocs offers a set of optional cloud-powered features that extend the core static documentation experience. All cloud features are disabled by default and require an API key to activate.

> **Note:** Cloud features are upcoming premium additions and may not yet be available. Check the MokaDocs release notes for current availability.

## Configuration

Enable cloud features in your `mokadocs.yaml`:

```yaml
features:
  cloud:
    enabled: true
    apiKey: "your-api-key"
    features:
      aiSummaries: true
      pdfExport: true
      analytics: true
      customDomain: true
```

Set `enabled: true` and provide your API key to activate the cloud integration. Then toggle individual features on or off under the `features` block.

## AI Summaries

```yaml
features:
  cloud:
    features:
      aiSummaries: true
```

When enabled, MokaDocs generates AI-powered summaries for API types and reference pages. These summaries appear at the top of each API documentation page, giving readers a quick overview of the type's purpose, key members, and common usage patterns.

## PDF Export

```yaml
features:
  cloud:
    features:
      pdfExport: true
```

Server-side PDF generation of your entire documentation site. This produces a downloadable PDF that includes all pages, formatted with proper page breaks, a table of contents, and consistent styling. Useful for offline distribution or compliance requirements.

## Analytics

```yaml
features:
  cloud:
    features:
      analytics: true
```

A usage analytics dashboard that tracks page views, search queries, and reader navigation patterns. Use this data to identify which pages are most visited, what users are searching for, and where readers drop off. The dashboard is accessible from the MokaDocs cloud portal.

## Custom Domain

```yaml
features:
  cloud:
    features:
      customDomain: true
```

Host your documentation on a custom domain with automatic SSL certificate provisioning. Point your DNS to the MokaDocs cloud infrastructure and your documentation will be served under your own domain with HTTPS enabled automatically.
