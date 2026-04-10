---
title: Deployment
order: 5
---

# Deployment

MokaDocs generates a standard static site in the `_site/` directory (or your configured output path). The output is plain HTML, CSS, and JavaScript with no server-side runtime required. You can deploy it to any static hosting platform.

## Build for Production

```bash
mokadocs build
```

The output directory contains everything needed for deployment. No additional processing or bundling is required.

For a clean production build:

```bash
mokadocs clean && mokadocs build --no-cache
```

## GitHub Pages

GitHub Pages is a free hosting option for public repositories. You can deploy MokaDocs output using GitHub Actions.

### GitHub Actions Workflow

Create `.github/workflows/docs.yml` in your repository:

```yaml
name: Deploy Documentation

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install MokaDocs
        run: dotnet tool install -g mokadocs

      - name: Build documentation
        run: mokadocs build

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: _site

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

### Setup Steps

1. Go to your repository Settings > Pages.
2. Under "Source", select "GitHub Actions".
3. Push the workflow file to your `main` branch.
4. The documentation will build and deploy automatically on each push to `main`.

### `.nojekyll` File

Add an empty `.nojekyll` file to the site root to prevent Jekyll from stripping directories starting with `_` (like `_theme/`). The `mokadocs-blazor-preview` plugin emits this automatically, but other sites need it too. You can create this file in your `docs/` directory so it is copied to the output during build.

### Base Path for GitHub Pages Project Sites

If your site is hosted at `https://username.github.io/repo-name/` (not at the root of a custom domain), you need to set the base path so all links and assets include the correct prefix.

**Option 1: CLI flag** (recommended for CI)

```bash
mokadocs build --base-path /repo-name
```

**Option 2: Config file**

```yaml
# mokadocs.yaml
build:
  basePath: /repo-name
```

The `--base-path` CLI flag takes precedence over the config value. This prefixes all routes, CSS/JS paths, navigation links, search index entries, and the 404 page with the specified path.

## Netlify

Netlify provides continuous deployment from Git repositories with automatic builds on push.

### netlify.toml

Create a `netlify.toml` file in your repository root:

```toml
[build]
  command = "dotnet tool install -g mokadocs && mokadocs build"
  publish = "_site"

[build.environment]
  DOTNET_VERSION = "10.0"

# Clean URL support
[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
  conditions = {Role = ["admin"]}

# Custom 404
[[redirects]]
  from = "/*"
  to = "/404.html"
  status = 404
```

### Setup Steps

1. Connect your repository to Netlify.
2. Netlify will detect the `netlify.toml` and configure the build automatically.
3. Each push to your main branch triggers a new deployment.

## Vercel

Vercel supports static site deployment with zero configuration.

### vercel.json

Create a `vercel.json` file in your repository root:

```json
{
  "buildCommand": "dotnet tool install -g mokadocs && mokadocs build",
  "outputDirectory": "_site",
  "cleanUrls": true
}
```

### Setup Steps

1. Import your repository in the Vercel dashboard.
2. Vercel reads the `vercel.json` configuration automatically.
3. Deployments happen on each push with preview URLs for pull requests.

## Azure Static Web Apps

Azure Static Web Apps provides free hosting with global distribution.

### GitHub Actions Workflow for Azure

```yaml
name: Deploy to Azure Static Web Apps

on:
  push:
    branches: [main]
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches: [main]

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install MokaDocs
        run: dotnet tool install -g mokadocs

      - name: Build documentation
        run: mokadocs build

      - name: Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "_site"
          skip_app_build: true
```

## Docker

You can serve MokaDocs output with any static file server. Here is an example using nginx.

### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet tool install -g mokadocs
ENV PATH="$PATH:/root/.dotnet/tools"
RUN mokadocs build

# Serve stage
FROM nginx:alpine
COPY --from=build /src/_site /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

### nginx.conf

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # Clean URL support
    location / {
        try_files $uri $uri/index.html $uri.html =404;
    }

    # Custom 404 page
    error_page 404 /404.html;

    # Cache static assets
    location ~* \.(css|js|png|jpg|gif|svg|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### Build and Run

```bash
docker build -t my-docs .
docker run -p 8080:80 my-docs
```

## Custom Domain Setup

Most hosting platforms support custom domains. The general process is:

1. **Add a CNAME record** pointing your domain (e.g., `docs.example.com`) to your hosting provider.
2. **Configure the domain** in your hosting provider's dashboard.
3. **Update the base URL** in your MokaDocs configuration if needed:

```yaml
build:
  basePath: /
  # No prefix needed when using a custom domain at the root
```

4. **Enable HTTPS** — most platforms provide free SSL certificates via Let's Encrypt.

### CNAME File

For GitHub Pages with a custom domain, create a `CNAME` file in your docs directory so it gets copied to the output:

```
docs.example.com
```

Place this file at `docs/CNAME` (no file extension) and it will be included in the build output.

## CI/CD Best Practices

### Caching .NET Tools

Speed up CI builds by caching the .NET tool installation:

```yaml
# GitHub Actions example with caching
- name: Cache .NET tools
  uses: actions/cache@v4
  with:
    path: ~/.dotnet/tools
    key: dotnet-tools-${{ runner.os }}-mokadocs

- name: Install MokaDocs
  run: dotnet tool install -g mokadocs || true
```

### Build Validation on Pull Requests

Run the build on pull requests to catch documentation errors before merging:

```yaml
name: Validate Documentation

on:
  pull_request:
    paths:
      - 'docs/**'
      - 'mokadocs.yaml'

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install MokaDocs
        run: dotnet tool install -g mokadocs

      - name: Build documentation
        run: mokadocs build --verbose

      - name: Check for build warnings
        run: |
          if mokadocs build 2>&1 | grep -q "WARNING"; then
            echo "::warning::Documentation build produced warnings"
          fi
```

## Clean URLs and Trailing Slashes

MokaDocs generates `index.html` files inside directories to support clean URLs:

```
/guide/getting-started  →  /guide/getting-started/index.html
/api/MyClass            →  /api/MyClass/index.html
```

Most static hosting platforms handle this automatically. If your platform requires explicit configuration for clean URLs, refer to the platform-specific sections above.

### Trailing Slash Behavior

- MokaDocs generates links without trailing slashes (e.g., `/guide` not `/guide/`).
- Most platforms normalize these automatically.
- If you encounter 404 errors on internal navigation, check your platform's trailing slash configuration.

## Base URL Configuration

When deploying to a subdirectory (e.g., `https://example.com/docs/`), set the `basePath` in your configuration:

```yaml
build:
  basePath: /docs/
```

This prefixes all internal links and asset paths with the specified base path. Without this setting, links will point to the domain root and break when served from a subdirectory.

**Common scenarios:**

| Hosting Setup | basePath |
|--------------|----------|
| Custom domain root (`docs.example.com`) | `/` |
| GitHub Pages user site (`username.github.io`) | `/` |
| GitHub Pages project site (`username.github.io/repo`) | `/repo/` |
| Subdirectory (`example.com/docs`) | `/docs/` |
