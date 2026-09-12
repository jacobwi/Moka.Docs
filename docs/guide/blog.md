---
title: Blog
order: 6
---

# Blog

MokaDocs has no blog feature. The config reader accepts a `features.blog` section with `enabled`, `postsPerPage` and `showAuthors`, but nothing reads those values, so setting them changes nothing.

## Posts in a Blog Folder

A `docs/blog/` folder builds like any other section:

- Each file becomes an ordinary page at `/blog/<file-name>`.
- Front matter such as `date`, `author` and `summary` is ignored. Only the standard [front matter](/configuration/front-matter) fields apply.
- The sidebar sorts the pages by `order`, then by title. There is no date sorting, post index or pagination.
- Without a `docs/blog/index.md`, `/blog/` redirects to the page whose file name sorts first alphabetically.

To show the newest posts first, give newer posts a lower `order`, or write your own list of posts in `docs/blog/index.md`.
