---
title: Deployment
order: 5
---

# Deployment

`mokadocs build` writes a static site to `build.output` (`./_site` by default): the HTML pages, `404.html`, the theme's CSS and JavaScript under `_theme/`, `search-index.json` and `robots.txt`, plus `sitemap.xml` when `site.url` is set. Any static host can serve it.

The dev server's `/api/*` endpoints don't exist on a static host. REPL blocks can't run there, and feedback votes are only stored in the reader's browser.

## Build for Production

```bash
mokadocs build
```

The build exits with code 1 when it fails or reports an error, so a CI job stops before deploying a broken site. See [CLI Commands](/advanced/cli-reference) for the options.

For a build from scratch:

```bash
mokadocs clean && mokadocs build
```

`mokadocs clean` deletes the output directory and the `.mokadocs` cache folder, so the next build analyzes C# projects again.

## GitHub Pages

GitHub Pages hosts public repositories for free. You can deploy MokaDocs output with GitHub Actions.

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

      # A project site is served from https://<user>.github.io/<repo>/, so links need the
      # repository name as a base path. Drop --base-path for a user site or a custom domain.
      - name: Build documentation
        run: mokadocs build --base-path /my-repo

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

Replace `/my-repo` with your repository name. See [Base Path](#base-path).

### Setup Steps

1. Open your repository's **Settings > Pages**.
2. Under **Source**, select **GitHub Actions**.
3. Push the workflow file to `main`. Each push to `main` then builds and deploys the site.

### `.nojekyll` File

MokaDocs writes an empty `.nojekyll` file to the site root on every build. When GitHub Pages publishes from a branch, it runs Jekyll first, and Jekyll drops folders whose names start with `_`. That would remove `_theme/` and all of the site's CSS and JavaScript.

You don't need to create the file yourself. A workflow that uploads the site with `actions/upload-pages-artifact` and publishes it with `actions/deploy-pages`, like the one above, doesn't run Jekyll, so the file has no effect there.

## Base Path

A site served from a subfolder, such as a GitHub Pages project site at `https://<user>.github.io/<repo>/`, needs a base path so links and asset URLs include the prefix. A site at the root of its domain doesn't.

Set it on the command line (convenient in CI):

```bash
mokadocs build --base-path /repo-name
```

Or in the configuration:

```yaml
# mokadocs.yaml
build:
  basePath: /repo-name
```

`--base-path` overrides `build.basePath`. The value gets a leading slash and loses any trailing slash, so `repo-name/` and `/repo-name` mean the same thing. The prefix is added to page and navigation links, theme CSS and JavaScript URLs, root-relative links in page content, search results, the links in `404.html`, and the canonical and sitemap URLs.

| Hosting setup | `basePath` |
|---------------|------------|
| Custom domain root (`docs.example.com`) | `/` (the default) |
| GitHub Pages user site (`username.github.io`) | `/` |
| GitHub Pages project site (`username.github.io/repo`) | `/repo` |
| Subfolder (`example.com/docs`) | `/docs` |

Set `site.url` to the address the site is served from. Canonical links, Open Graph and Twitter tags and `sitemap.xml` are only written when it is set. It can include the base path or leave it out: with `basePath: /repo`, `url: https://username.github.io/repo` and `url: https://username.github.io` produce the same URLs.

::: note Git Bash on Windows
Git Bash converts arguments that start with `/` into Windows paths, so `--base-path /repo-name` reaches MokaDocs as something like `C:/Program Files/Git/repo-name`. Run the command as `MSYS_NO_PATHCONV=1 mokadocs build --base-path /repo-name`, or set `build.basePath` in `mokadocs.yaml` instead.
:::

## Netlify

Netlify doesn't list the .NET SDK among the software in its build image, so the build has to install it first. One way is a script in your repository that installs the SDK with Microsoft's `dotnet-install.sh` and then runs MokaDocs:

```bash
#!/usr/bin/env bash
# build-docs.sh
set -euo pipefail

curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$HOME/.dotnet"

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

dotnet tool install -g mokadocs
mokadocs build
```

`dotnet-install.sh` runs in its own process, so any `PATH` change it makes doesn't reach the rest of the script. That's why the script sets `DOTNET_ROOT` and `PATH` itself; the `mokadocs` command needs both.

### netlify.toml

Create `netlify.toml` in your repository root:

```toml
[build]
  command = "bash build-docs.sh"
  publish = "_site"
```

No redirect rules are needed. Pages are written as `<route>/index.html`, which Netlify serves at `/<route>`, and Netlify shows the generated `404.html` for missing pages. For your own redirects or headers, add `_redirects` or `_headers` files to the docs folder (see [Host Configuration Files](#host-configuration-files)).

### Setup Steps

1. Connect your repository to Netlify.
2. Netlify reads `netlify.toml` from the repository root.
3. Each push to your production branch starts a deploy.

## Vercel

Vercel's build image is based on Amazon Linux 2023 and doesn't list .NET among its runtimes either. Use the `build-docs.sh` script from the [Netlify section](#netlify).

### vercel.json

Create `vercel.json` in your repository root:

```json
{
  "buildCommand": "bash build-docs.sh",
  "outputDirectory": "_site"
}
```

If .NET fails to start in the build because the ICU library is missing, add `dnf install -y libicu` to the start of the script, or set the `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` environment variable.

### Setup Steps

1. Import your repository in the Vercel dashboard.
2. Vercel reads `vercel.json` from the repository root.
3. Each push deploys the site, and pull requests get preview URLs.

## Azure Static Web Apps

The site is served from the root of its domain, so no base path is needed.

### GitHub Actions Workflow for Azure

```yaml
name: Deploy to Azure Static Web Apps

on:
  push:
    branches: [main]
  workflow_dispatch:

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

Any static file server can serve the output. This example builds the site in the .NET SDK image and serves it with nginx.

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

    # /guide/intro is written as /guide/intro/index.html
    location / {
        try_files $uri $uri/index.html =404;
    }

    error_page 404 /404.html;
}
```

The theme's files keep the same names on every build (`/_theme/css/main.css`, `/_theme/js/main.js`), so don't serve them with long-lived `immutable` cache headers. Readers would keep the old CSS and JavaScript after a deploy.

### Build and Run

```bash
docker build -t my-docs .
docker run -p 8080:80 my-docs
```

## Custom Domains

The general steps:

1. Add a DNS record for your domain (for example a `CNAME` record for `docs.example.com`) as your host's documentation describes.
2. Add the domain in your host's settings.
3. Build without a base path. Remove `build.basePath` or `--base-path` if you set them for a project site, and update `site.url`.
4. Turn on HTTPS in your host's settings.

### GitHub Pages Custom Domains

With a GitHub Actions workflow like the one above, set the custom domain in **Settings > Pages**. GitHub ignores any `CNAME` file in a site published from a custom workflow.

If you publish from a branch instead, for example by pushing the build output to a `gh-pages` branch, GitHub keeps the domain in a `CNAME` file at the root of the published branch. Save the file as `CNAME` (no extension) in the root of your docs folder, so every build copies it to the output:

```
docs.example.com
```

### Host Configuration Files

The build copies these files from the root of `content.docs` to the root of the output. Files with the same names in subfolders are not copied.

| File | Used by |
|------|---------|
| `CNAME` | GitHub Pages custom domain, when publishing from a branch |
| `_redirects` | Redirect rules on Netlify and Cloudflare Pages |
| `_headers` | Response headers on Netlify and Cloudflare Pages |

## CI/CD Best Practices

### Pinning and Caching MokaDocs

A local tool manifest pins the MokaDocs version in your repository (see [Installation](/getting-started/installation#install-as-a-local-tool)). CI then restores that exact version, and the NuGet folder it restores into can be cached between runs:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'

- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/dotnet-tools.json') }}

- name: Restore MokaDocs
  run: dotnet tool restore

- name: Build documentation
  run: dotnet mokadocs build
```

`dotnet new tool-manifest` creates `.config/dotnet-tools.json` with the .NET 9 SDK and `dotnet-tools.json` in the current folder with the .NET 10 SDK. The `**/dotnet-tools.json` pattern matches either.

### Build Validation on Pull Requests

Run `mokadocs validate` and `mokadocs doctor` on pull requests to catch documentation problems before merging. Both exit non-zero when they find something, so the job fails without any output parsing:

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

      # Runs the full build without writing output. Exits 1 on warnings, 2 on errors.
      - name: Validate the build
        run: mokadocs validate

      # Broken links, missing titles, unknown plugins, unused images.
      - name: Check the docs
        run: mokadocs doctor
```

To fail only on errors and let warnings through, accept exit code 1:

```yaml
      - name: Validate the build
        run: mokadocs validate || [ $? -eq 1 ]
```

## Clean URLs and Trailing Slashes

Every page is written to `<route>/index.html`:

```
/guide/getting-started    ->  guide/getting-started/index.html
/api/mylibrary/widget     ->  api/mylibrary/widget/index.html
```

API routes are lowercase: the namespace becomes folders, followed by the type name. A folder that has pages under it but no page of its own gets an `index.html` that redirects to its first subfolder with a page, in alphabetical order.

Hosts that serve a folder's `index.html` for the folder URL need no extra configuration. The nginx example above shows the equivalent rule for a server you configure yourself.

### Trailing Slashes

- Sidebar, previous/next and search result links have no trailing slash (`/guide/intro`).
- Some hosts, GitHub Pages among them, redirect `/guide/intro` to `/guide/intro/`. Relative Markdown links, including links to `.md` files, are resolved to root-relative links at build time, so they reach the same page either way.
- Relative links in raw HTML, such as `<a href="../intro">`, are left as written, and the browser resolves them differently with and without the trailing slash. Use root-relative paths in raw HTML.
