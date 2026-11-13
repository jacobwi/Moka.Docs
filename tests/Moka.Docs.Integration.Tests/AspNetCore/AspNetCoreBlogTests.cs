using System.Net;
using FluentAssertions;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The embedded host builds the same blog as the CLI when <see cref="MokaDocsOptions.Blog" /> is
///     set, under its base path and without feeds, since it has no public URL.
/// </summary>
public sealed class AspNetCoreBlogTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-host-blog-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreBlogTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs", "blog"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n# Home\n");
		File.WriteAllText(Path.Combine(_root, "docs", "blog", "first.md"),
			"---\ntitle: First Post\ndate: 2026-01-10\nauthor: jane\ntags: [news]\n---\n# First Post\n\nThe first post.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "blog", "second.md"),
			"---\ntitle: Second Post\ndate: 2026-02-20\n---\n# Second Post\n\nThe second post.\n");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task Host_BuildsTheBlog_UnderItsBasePath()
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.Blog = new MokaDocsBlogOptions
			{
				Title = "News",
				PostsPerPage = 1,
				Authors = { ["jane"] = new BlogAuthor { Name = "Jane Doe" } }
			};
		});

		string index = await host.GetTextAsync("/docs/blog/");
		index.Should().Contain("<h1>News</h1>")
			.And.Contain("<a href=\"/docs/blog/second\">Second Post</a>")
			.And.Contain("href=\"/docs/blog/page/2\" rel=\"next\"")
			.And.NotContain("rel=\"alternate\"", "the host has no public URL for a feed");
		index.Should().Contain("<a class=\"nav-link current\" href=\"/docs/blog\" aria-current=\"page\">", "the default sidebar gets a blog entry");

		(await host.GetTextAsync("/docs/blog/page/2/")).Should().Contain("First Post");
		(await host.GetTextAsync("/docs/blog/tags/news/")).Should().Contain("First Post");
		(await host.GetTextAsync("/docs/blog/first/")).Should().Contain("<span class=\"blog-author-name\">Jane Doe</span>")
			.And.Contain("<span class=\"page-nav-label\">Newer post</span>");
		(await host.GetAsync("/docs/blog/rss.xml")).StatusCode.Should().Be(HttpStatusCode.NotFound);

		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		site.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Warning && d.Source == "Blog");
	}

	[Fact]
	public async Task Host_WithoutBlog_BuildsTheFolderAsOrdinaryPages()
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options => options.DocsPath = "docs");

		(await host.GetTextAsync("/docs/blog/first/")).Should().NotContain("blog-post-header");
		(await host.GetAsync("/docs/blog/tags/news/")).StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
