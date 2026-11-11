---
title: "How This Blog Is Built"
description: "The front matter and folder layout behind the SampleLibrary blog"
date: 2026-04-02
authors: [sam, Guest Writer]
tags: [meta, tutorial]
---

# How This Blog Is Built

This blog is a folder of Markdown files under `docs/blog`. MokaDocs lists the posts newest first, gives every tag a page and writes RSS and Atom feeds next to the index.

## Front Matter

Each post starts with a date. The rest is optional:

```yaml
---
title: "SampleLibrary v2.0 Released"
date: 2026-03-15T09:30:00Z
author: [team, sam]
summary: "Shown on the index page and in the feeds."
tags: [release, v2.0]
image: ./release-v2-cover.svg
---
```

`team` and `sam` are keys of `features.blog.authors` in `mokadocs.yaml`, which holds each author's name, link and avatar once. A name that is no key, such as the guest writer of this post, is shown as written.

Without a `summary`, the index page and the feeds use the post's first paragraph.

## What the Build Adds

- A header under the title with the date, the reading time, the authors and the tags.
- Links to the next newer and older post at the bottom of the page.
- An index page at `/blog`, more pages once there are more posts than `postsPerPage`, and a page per tag under `/blog/tags`.
- Search entries in the **Blog** category.

::: tip
Add `draft: true` to a post to leave it out until `mokadocs build --draft`.
:::
