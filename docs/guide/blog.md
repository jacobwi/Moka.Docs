---
title: Blog
order: 6
---

# Blog

A folder of Markdown files becomes a blog: an index of the posts, newest first, a page for every tag, RSS and Atom feeds, and one entry in the sidebar. Turn it on in `mokadocs.yaml`:

```yaml
site:
  url: "https://example.github.io/MyLib"   # feeds need absolute links

features:
  blog:
    enabled: true
```

Every `.md` file under `docs/blog/`, in subfolders too, is now a post. See [`features.blog`](/configuration/site-config#blog) for the folder, the route, the page size, the feeds and the authors.

## Writing a Post

A post is a Markdown page with a date:

```markdown
---
title: "SampleLibrary v2.0 Released"
date: 2026-03-15
author: jane
summary: "Rectangles, safe division and a fluent result builder."
tags: [release, v2.0]
image: ./v2-cover.png
---

# SampleLibrary v2.0 Released

We are excited to announce SampleLibrary v2.0...
```

| Key | What it does |
|---|---|
| `date` | Publication date, `2026-03-15` or `2026-03-15T09:30:00Z`. Required: without it the build warns and uses the file's last commit date |
| `author` | One name or a list. A key of `features.blog.authors` shows that author's name, link and avatar; any other name is shown as written |
| `summary` | Text for the index page and the feeds. Defaults to the post's first paragraph |
| `tags` | Each tag gets a page at `/blog/tags/{tag}` |
| `image` | A cover image, relative to the post or from the site root |
| `draft` | `true` leaves the post out until you build with `--draft` |

The [front matter reference](/configuration/front-matter#blog-post-keys) has the details. Every other front matter key works as it does on any page.

Define authors once and name them by key:

```yaml
features:
  blog:
    enabled: true
    authors:
      jane:
        name: "Jane Doe"
        url: "https://github.com/jane"
        avatar: "img/jane.png"
```

## What the Build Generates

| Route | Page |
|---|---|
| `/blog` | The index: the newest posts, with title, date, reading time, authors, summary and tags |
| `/blog/page/2`, `/blog/page/3`, ... | The older posts, `postsPerPage` at a time |
| `/blog/tags` | Every tag with its number of posts |
| `/blog/tags/{tag}` | The posts with that tag, paginated the same way |
| `/blog/rss.xml`, `/blog/atom.xml` | The feeds, with the 20 latest posts and their summaries |

Tag pages use a slug of the tag: lower case letters and digits joined by hyphens, with accents removed and `#` and `+` spelled out, so `C#` is `/blog/tags/csharp`. Two tags that end up with the same slug get `-2`, `-3` and so on.

Each post gets a header under its title with the date, the reading time, the authors and the tags, and an optional cover image. Its previous and next links at the bottom go to the next newer and older post instead of the next docs page. Posts keep the route of their file: `docs/blog/release-v2.md` is at `/blog/release-v2`.

A `docs/blog/index.md` is not a post. Its text appears on the index page above the posts, its `description` stands in for `features.blog.description`, and its `order` and `icon` place the blog's sidebar entry.

## Sidebar and Search

The blog takes one sidebar entry, linked to the index page, that lists the latest posts (5 by default, set by `sidebarPosts`). Older posts are reached from the index pages. The generated list pages are not in the sidebar.

- Without a `nav` section, the entry is built from the folder like any section, sorted by the `order` of `docs/blog/index.md`.
- With a `nav` section, add an item whose `path` is the blog's route. Its posts are filled in unless you give it `children` of your own:

```yaml
nav:
  - label: "Guide"
    path: /guide
  - label: "Blog"
    path: /blog
    icon: newspaper
```

Search finds posts under the category **Blog**, with their headings and tags. The index page is in search too; the later index pages and the tag pages are not, since they only repeat the posts.

## Feeds

`feed: rss` (the default) writes `/blog/rss.xml`, `feed: atom` writes `/blog/atom.xml`, and `feed: [rss, atom]` writes both. Every page links them from its head, so browsers and feed readers find them from any page.

Feeds need `site.url`, because a feed reader opens links outside the site. Without it the build warns and writes no feed. Drafts never reach a feed, even in a `--draft` build.

## Hidden and Draft Posts

- `visibility: hidden` builds the post at its URL, with its header, but leaves it out of the index, the tag pages, the feeds and the sidebar.
- `draft: true` (or `visibility: draft`) leaves the post out entirely. A build with `--draft` builds it and lists it with a **Draft** label.

## In the ASP.NET Core Host

Set `MokaDocsOptions.Blog` to build the same blog from `DocsPath`:

```csharp
builder.Services.AddMokaDocs(options =>
{
    options.DocsPath = "docs";
    options.Blog = new MokaDocsBlogOptions
    {
        Title = "News",
        Authors = { ["jane"] = new BlogAuthor { Name = "Jane Doe" } }
    };
});
```

The default sidebar gets a blog entry. The host writes no feeds: it doesn't know the URL readers reach it at.
