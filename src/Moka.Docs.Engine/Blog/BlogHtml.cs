using System.Globalization;
using System.Net;
using System.Text;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Engine.Blog;

/// <summary>
///     The markup of the blog's generated pages and of the header added to each post. Links are
///     root-relative; rendering adds the base path. The default theme styles every <c>blog-*</c>
///     class; a custom theme gets the same markup.
/// </summary>
internal static class BlogHtml
{
	private const string _chevronLeft =
		"<svg class=\"blog-pager-arrow\" width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><polyline points=\"15 18 9 12 15 6\"></polyline></svg>";

	private const string _chevronRight =
		"<svg class=\"blog-pager-arrow\" width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><polyline points=\"9 18 15 12 9 6\"></polyline></svg>";

	/// <summary>A date as readers see it, such as <c>March 15, 2026</c>, in the post's own offset.</summary>
	public static string DisplayDate(DateTimeOffset date) => date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

	/// <summary>
	///     The header placed under a post's title: date, reading time, authors, tags and cover image.
	/// </summary>
	public static string PostHeader(BlogPost post, bool showAuthors)
	{
		var sb = new StringBuilder();
		sb.Append("<div class=\"blog-post-header\">");
		Meta(sb, post, showAuthors);
		Tags(sb, post.Tags);
		if (post.Image is { } image)
		{
			sb.Append("<img class=\"blog-cover\" src=\"").Append(Encode(image)).Append("\" alt=\"\" />");
		}

		sb.Append("</div>");
		return sb.ToString();
	}

	/// <summary>
	///     Inserts the post header after the post's leading <c>h1</c>, or adds an <c>h1</c> with the
	///     title when the post starts without one.
	/// </summary>
	public static string WithPostHeader(string html, string title, string header)
	{
		string trimmed = html.TrimStart();
		if (trimmed.StartsWith("<h1", StringComparison.OrdinalIgnoreCase))
		{
			int end = trimmed.IndexOf("</h1>", StringComparison.OrdinalIgnoreCase);
			if (end >= 0)
			{
				int split = end + "</h1>".Length;
				return trimmed[..split] + "\n" + header + trimmed[split..];
			}
		}

		return $"<h1>{Encode(title)}</h1>\n{header}\n{html}";
	}

	/// <summary>One page of the post list: the heading, an optional introduction, the cards and the pager.</summary>
	public static string ListPage(string title, string intro, IEnumerable<BlogPost> posts, bool showAuthors,
		Pager? pager, string emptyText)
	{
		var sb = new StringBuilder("<div class=\"blog-page\">\n");
		if (!intro.TrimStart().StartsWith("<h1", StringComparison.OrdinalIgnoreCase))
		{
			sb.Append("<h1>").Append(Encode(title)).Append("</h1>\n");
		}

		sb.Append(intro);

		var list = posts.ToList();
		if (list.Count == 0)
		{
			sb.Append("<p class=\"blog-empty\">").Append(Encode(emptyText)).Append("</p>\n");
		}
		else
		{
			sb.Append("<div class=\"blog-list\">\n");
			foreach (BlogPost post in list)
			{
				Card(sb, post, showAuthors);
			}

			sb.Append("</div>\n");
			if (pager is not null)
			{
				Pagination(sb, pager);
			}
		}

		return sb.Append("</div>\n").ToString();
	}

	/// <summary>The page listing every tag with its number of posts.</summary>
	public static string TagsPage(string title, IReadOnlyList<BlogTag> tags)
	{
		var sb = new StringBuilder("<div class=\"blog-page\">\n");
		sb.Append("<h1>").Append(Encode(title)).Append("</h1>\n");
		if (tags.Count == 0)
		{
			sb.Append("<p class=\"blog-empty\">No tags yet.</p>\n");
		}
		else
		{
			sb.Append("<ul class=\"blog-tags blog-tags-all\">");
			foreach (BlogTag tag in tags)
			{
				sb.Append("<li><a class=\"blog-tag\" href=\"").Append(Encode(tag.Route)).Append("\">")
					.Append(Encode(tag.Name))
					.Append(" <span class=\"blog-tag-count\">").Append(tag.Count.ToString(CultureInfo.InvariantCulture))
					.Append("<span class=\"blog-sr-only\"> ").Append(tag.Count == 1 ? "post" : "posts")
					.Append("</span></span></a></li>");
			}

			sb.Append("</ul>\n");
		}

		return sb.Append("</div>\n").ToString();
	}

	/// <summary>The line under a tag page's title: how many posts have the tag, and a link to all tags.</summary>
	public static string TagLead(int count, string tagsRoute) =>
		$"<p class=\"blog-description\">{count.ToString(CultureInfo.InvariantCulture)} {(count == 1 ? "post" : "posts")}. " +
		$"<a href=\"{Encode(tagsRoute)}\">All tags</a></p>\n";

	/// <summary>A paragraph with the encoded text, or nothing when the text is empty.</summary>
	public static string Paragraph(string cssClass, string text) =>
		string.IsNullOrWhiteSpace(text) ? "" : $"<p class=\"{cssClass}\">{Encode(text)}</p>\n";

	private static void Card(StringBuilder sb, BlogPost post, bool showAuthors)
	{
		string route = Encode(post.Route);
		sb.Append("<article class=\"blog-card\">");
		if (post.Image is { } image)
		{
			sb.Append("<a class=\"blog-card-image\" href=\"").Append(route).Append("\" tabindex=\"-1\" aria-hidden=\"true\">")
				.Append("<img src=\"").Append(Encode(image)).Append("\" alt=\"\" loading=\"lazy\" /></a>");
		}

		sb.Append("<div class=\"blog-card-body\">");
		sb.Append("<h2 class=\"blog-card-title\"><a href=\"").Append(route).Append("\">").Append(Encode(post.Title))
			.Append("</a></h2>");
		Meta(sb, post, showAuthors);
		if (post.Summary.Length > 0)
		{
			sb.Append("<p class=\"blog-card-summary\">").Append(Encode(post.Summary)).Append("</p>");
		}

		Tags(sb, post.Tags);
		sb.Append("<a class=\"blog-read-more\" href=\"").Append(route).Append("\">Read more<span class=\"blog-sr-only\"> about ")
			.Append(Encode(post.Title)).Append("</span>").Append(_chevronRight).Append("</a>");
		sb.Append("</div></article>\n");
	}

	private static void Meta(StringBuilder sb, BlogPost post, bool showAuthors)
	{
		sb.Append("<div class=\"blog-meta\">");
		sb.Append("<time datetime=\"").Append(post.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\">")
			.Append(Encode(DisplayDate(post.Date))).Append("</time>");
		sb.Append("<span class=\"blog-meta-sep\" aria-hidden=\"true\">·</span>");
		sb.Append("<span class=\"blog-reading-time\">")
			.Append(post.ReadingMinutes.ToString(CultureInfo.InvariantCulture)).Append(" min read</span>");
		if (post.IsDraft)
		{
			sb.Append("<span class=\"blog-meta-sep\" aria-hidden=\"true\">·</span><span class=\"blog-draft\">Draft</span>");
		}

		sb.Append("</div>");

		// A row of its own, so a long list of names never leaves a dangling separator at the end
		// of the date line.
		if (showAuthors && post.Authors.Count > 0)
		{
			sb.Append("<div class=\"blog-authors\"><span class=\"blog-sr-only\">By</span>");
			for (int i = 0; i < post.Authors.Count; i++)
			{
				string separator = i == post.Authors.Count - 1 ? ""
					: i == post.Authors.Count - 2 ? "<span class=\"blog-author-sep\">and</span>"
					: ",";
				Author(sb, post.Authors[i], separator);
			}

			sb.Append("</div>");
		}
	}

	private static void Author(StringBuilder sb, BlogAuthor author, string separator)
	{
		sb.Append("<span class=\"blog-author\">");
		if (author.Avatar is { } avatar)
		{
			sb.Append("<img class=\"blog-author-avatar\" src=\"").Append(Encode(avatar))
				.Append("\" alt=\"\" width=\"24\" height=\"24\" loading=\"lazy\" />");
		}

		if (author.Url is { } url)
		{
			sb.Append("<a href=\"").Append(Encode(url)).Append("\" rel=\"author\">").Append(Encode(author.Name)).Append("</a>");
		}
		else
		{
			sb.Append("<span class=\"blog-author-name\">").Append(Encode(author.Name)).Append("</span>");
		}

		sb.Append(separator).Append("</span>");
	}

	private static void Tags(StringBuilder sb, IReadOnlyList<BlogTag> tags)
	{
		if (tags.Count == 0)
		{
			return;
		}

		sb.Append("<ul class=\"blog-tags\" aria-label=\"Tags\">");
		foreach (BlogTag tag in tags)
		{
			sb.Append("<li><a class=\"blog-tag\" href=\"").Append(Encode(tag.Route)).Append("\">").Append(Encode(tag.Name))
				.Append("</a></li>");
		}

		sb.Append("</ul>");
	}

	private static void Pagination(StringBuilder sb, Pager pager)
	{
		sb.Append("<nav class=\"blog-pager\" aria-label=\"Pages\">");
		if (pager.NewerRoute is { } newer)
		{
			sb.Append("<a class=\"blog-pager-link blog-pager-newer\" href=\"").Append(Encode(newer)).Append("\" rel=\"prev\">")
				.Append(_chevronLeft).Append("Newer posts</a>");
		}
		else
		{
			sb.Append("<span class=\"blog-pager-spacer\"></span>");
		}

		sb.Append("<span class=\"blog-pager-status\">Page ").Append(pager.Page.ToString(CultureInfo.InvariantCulture))
			.Append(" of ").Append(pager.PageCount.ToString(CultureInfo.InvariantCulture)).Append("</span>");

		if (pager.OlderRoute is { } older)
		{
			sb.Append("<a class=\"blog-pager-link blog-pager-older\" href=\"").Append(Encode(older)).Append("\" rel=\"next\">")
				.Append("Older posts").Append(_chevronRight).Append("</a>");
		}
		else
		{
			sb.Append("<span class=\"blog-pager-spacer\"></span>");
		}

		sb.Append("</nav>\n");
	}

	private static string Encode(string value) => WebUtility.HtmlEncode(value);

	/// <summary>Where a list page sits among its pages.</summary>
	/// <param name="Page">The page number, from 1.</param>
	/// <param name="PageCount">The number of pages.</param>
	/// <param name="NewerRoute">The previous page, with newer posts, or <c>null</c> on the first.</param>
	/// <param name="OlderRoute">The next page, with older posts, or <c>null</c> on the last.</param>
	internal sealed record Pager(int Page, int PageCount, string? NewerRoute, string? OlderRoute);
}
