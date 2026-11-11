using System.Globalization;
using System.Text;
using System.Xml;
using Moka.Docs.Core.Blog;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Engine.Blog;

/// <summary>
///     Writes the blog's RSS 2.0 and Atom feeds. Every link is absolute, and XmlWriter escapes
///     every value, so a title with <c>&amp;</c> or <c>&lt;</c> keeps the file valid.
/// </summary>
internal static class BlogFeedWriter
{
	/// <summary>How many of the latest posts a feed lists.</summary>
	public const int MaxPosts = 20;

	private const string _atomNamespace = "http://www.w3.org/2005/Atom";
	private const string _dublinCoreNamespace = "http://purl.org/dc/elements/1.1/";

	private static readonly XmlWriterSettings _settings = new()
	{
		Encoding = new UTF8Encoding(false),
		Indent = true,
		NewLineChars = "\n"
	};

	/// <summary>The feed in one format, as UTF-8 bytes.</summary>
	/// <param name="format"><see cref="BlogSettings.RssFormat" /> or <see cref="BlogSettings.AtomFormat" />.</param>
	/// <param name="feed">What the feed is about.</param>
	/// <param name="posts">The posts, newest first; the first <see cref="MaxPosts" /> are written.</param>
	public static byte[] Write(string format, FeedInfo feed, IReadOnlyList<BlogPost> posts)
	{
		using var stream = new MemoryStream();
		using (var writer = XmlWriter.Create(stream, _settings))
		{
			List<BlogPost> latest = posts.Take(MaxPosts).ToList();
			if (format == BlogSettings.AtomFormat)
			{
				WriteAtom(writer, feed, latest);
			}
			else
			{
				WriteRss(writer, feed, latest);
			}
		}

		return stream.ToArray();
	}

	private static void WriteRss(XmlWriter w, FeedInfo feed, List<BlogPost> posts)
	{
		w.WriteStartDocument();
		w.WriteStartElement("rss");
		w.WriteAttributeString("version", "2.0");
		w.WriteAttributeString("xmlns", "atom", null, _atomNamespace);
		w.WriteAttributeString("xmlns", "dc", null, _dublinCoreNamespace);
		w.WriteStartElement("channel");
		w.WriteElementString("title", feed.Title);
		w.WriteElementString("link", feed.BlogUrl);
		w.WriteElementString("description", feed.Description);
		w.WriteElementString("generator", "MokaDocs");
		if (posts.Count > 0)
		{
			w.WriteElementString("lastBuildDate", RssDate(posts.Max(p => p.Date)));
		}

		w.WriteStartElement("link", _atomNamespace);
		w.WriteAttributeString("href", feed.SelfUrl);
		w.WriteAttributeString("rel", "self");
		w.WriteAttributeString("type", "application/rss+xml");
		w.WriteEndElement();

		foreach (BlogPost post in posts)
		{
			string url = feed.PostUrl(post.Route);
			w.WriteStartElement("item");
			w.WriteElementString("title", post.Title);
			w.WriteElementString("link", url);
			w.WriteStartElement("guid");
			w.WriteAttributeString("isPermaLink", "true");
			w.WriteString(url);
			w.WriteEndElement();
			w.WriteElementString("pubDate", RssDate(post.Date));
			if (post.Summary.Length > 0)
			{
				w.WriteElementString("description", post.Summary);
			}

			// RSS's own <author> must be an email address; dc:creator takes a name.
			foreach (BlogAuthor author in post.Authors)
			{
				w.WriteElementString("dc", "creator", _dublinCoreNamespace, author.Name);
			}

			foreach (BlogTag tag in post.Tags)
			{
				w.WriteElementString("category", tag.Name);
			}

			w.WriteEndElement();
		}

		w.WriteEndElement();
		w.WriteEndElement();
		w.WriteEndDocument();
	}

	private static void WriteAtom(XmlWriter w, FeedInfo feed, List<BlogPost> posts)
	{
		w.WriteStartDocument();
		w.WriteStartElement("feed", _atomNamespace);
		w.WriteElementString("title", _atomNamespace, feed.Title);
		if (feed.Description.Length > 0)
		{
			w.WriteElementString("subtitle", _atomNamespace, feed.Description);
		}

		WriteAtomLink(w, feed.SelfUrl, "self", "application/atom+xml");
		WriteAtomLink(w, feed.BlogUrl, "alternate", "text/html");
		w.WriteElementString("id", _atomNamespace, feed.BlogUrl);
		w.WriteElementString("updated", _atomNamespace,
			AtomDate(posts.Count > 0 ? posts.Max(p => p.Date) : DateTimeOffset.UnixEpoch));

		// Atom wants an author on the feed unless every entry has one.
		w.WriteStartElement("author", _atomNamespace);
		w.WriteElementString("name", _atomNamespace, feed.SiteTitle);
		w.WriteEndElement();
		w.WriteElementString("generator", _atomNamespace, "MokaDocs");

		foreach (BlogPost post in posts)
		{
			string url = feed.PostUrl(post.Route);
			w.WriteStartElement("entry", _atomNamespace);
			w.WriteElementString("title", _atomNamespace, post.Title);
			WriteAtomLink(w, url, "alternate", "text/html");
			w.WriteElementString("id", _atomNamespace, url);
			w.WriteElementString("published", _atomNamespace, AtomDate(post.Date));
			w.WriteElementString("updated", _atomNamespace, AtomDate(post.Date));
			if (post.Summary.Length > 0)
			{
				w.WriteStartElement("summary", _atomNamespace);
				w.WriteAttributeString("type", "text");
				w.WriteString(post.Summary);
				w.WriteEndElement();
			}

			foreach (BlogAuthor author in post.Authors)
			{
				w.WriteStartElement("author", _atomNamespace);
				w.WriteElementString("name", _atomNamespace, author.Name);
				if (author.Url is { } authorUrl && Uri.IsWellFormedUriString(authorUrl, UriKind.Absolute))
				{
					w.WriteElementString("uri", _atomNamespace, authorUrl);
				}

				w.WriteEndElement();
			}

			foreach (BlogTag tag in post.Tags)
			{
				w.WriteStartElement("category", _atomNamespace);
				w.WriteAttributeString("term", tag.Name);
				w.WriteEndElement();
			}

			w.WriteEndElement();
		}

		w.WriteEndElement();
		w.WriteEndDocument();
	}

	private static void WriteAtomLink(XmlWriter w, string href, string rel, string type)
	{
		w.WriteStartElement("link", _atomNamespace);
		w.WriteAttributeString("href", href);
		w.WriteAttributeString("rel", rel);
		w.WriteAttributeString("type", type);
		w.WriteEndElement();
	}

	// RFC 822, which RSS 2.0 requires: "Sun, 15 Mar 2026 00:00:00 GMT".
	private static string RssDate(DateTimeOffset date) =>
		date.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);

	// RFC 3339, which Atom requires: "2026-03-15T00:00:00+00:00".
	private static string AtomDate(DateTimeOffset date) =>
		date.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

	/// <summary>The feed's titles and absolute URLs.</summary>
	internal sealed record FeedInfo
	{
		public required string Title { get; init; }
		public required string SiteTitle { get; init; }
		public required string Description { get; init; }
		public required string BlogUrl { get; init; }
		public required string SelfUrl { get; init; }
		public required SiteConfig Config { get; init; }

		public string PostUrl(string route) => SiteUrls.Absolute(Config.Site.Url, Config.Build.BasePath, route);
	}
}
