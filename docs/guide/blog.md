---
title: Blog
order: 6
---

# Blog

MokaDocs includes an optional blog feature that lets you publish posts alongside your documentation. This is useful for release announcements, tutorials, changelogs, and other long-form content related to your project.

> **Note:** The blog feature is currently upcoming and may not yet be available. Check the MokaDocs release notes for current status.

## Configuration

Enable the blog in your `mokadocs.yaml`:

```yaml
features:
  blog:
    enabled: true
    postsPerPage: 10
    showAuthors: true
```

- `enabled` -- Set to `true` to activate the blog section on your site.
- `postsPerPage` -- Number of posts displayed per page on the blog index. Defaults to `10`.
- `showAuthors` -- When `true`, author names and avatars are displayed on each blog post.

## Writing Posts

Blog posts are Markdown files placed in a designated blog directory. Each post uses frontmatter to define its title, date, author, and other metadata:

```markdown
---
title: Announcing v2.0
date: 2025-06-15
author: Your Name
summary: A quick look at what's new in version 2.0.
---

Your post content here.
```

Posts are listed in reverse chronological order on the blog index page, with pagination controlled by the `postsPerPage` setting.
