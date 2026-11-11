using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     <c>features.blog</c> was parsed into three values that nothing read. These are the keys the
///     blog phase reads now.
/// </summary>
public sealed class BlogConfigTests
{
	[Fact]
	public void Parse_Blog_MapsEveryKey()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    features:
		                      blog:
		                        enabled: true
		                        path: news/posts
		                        route: /news
		                        title: "  Release Notes  "
		                        description: Everything that shipped
		                        postsPerPage: 4
		                        sidebarPosts: 0
		                        showAuthors: false
		                        feed: [rss, atom]
		                        authors:
		                          jane:
		                            name: Jane Doe
		                            url: https://example.com/jane
		                            avatar: img/jane.png
		                          bot: {}
		                    """;

		BlogFeatureConfig blog = SiteConfigReader.Parse(yaml).Features.Blog;

		blog.Enabled.Should().BeTrue();
		blog.Path.Should().Be("news/posts");
		blog.Route.Should().Be("/news");
		blog.Title.Should().Be("Release Notes");
		blog.Description.Should().Be("Everything that shipped");
		blog.PostsPerPage.Should().Be(4);
		blog.SidebarPosts.Should().Be(0);
		blog.ShowAuthors.Should().BeFalse();
		blog.Feeds.Should().Equal("rss", "atom");
		blog.Authors["jane"].Should().Be(new BlogAuthor
		{
			Name = "Jane Doe",
			Url = "https://example.com/jane",
			Avatar = "img/jane.png"
		});
		blog.Authors["bot"].Name.Should().Be("bot", "an author without a name is shown by its key");
	}

	[Fact]
	public void Parse_BlogWithOnlyEnabled_UsesTheDefaults()
	{
		const string yaml = "site:\n  title: Docs\nfeatures:\n  blog:\n    enabled: true\n";

		BlogFeatureConfig blog = SiteConfigReader.Parse(yaml).Features.Blog;

		blog.Path.Should().Be("blog");
		blog.Route.Should().BeNull("the route follows the path unless it is set");
		blog.Title.Should().Be("Blog");
		blog.Description.Should().BeEmpty();
		blog.PostsPerPage.Should().Be(10);
		blog.SidebarPosts.Should().Be(5);
		blog.Feeds.Should().Equal("rss");
		blog.Authors.Should().BeEmpty();
		blog.ShowAuthors.Should().BeTrue();
	}

	[Fact]
	public void Parse_NoBlogSection_IsDisabledWithTheSameDefaults()
	{
		BlogFeatureConfig blog = SiteConfigReader.Parse("site:\n  title: Docs\n").Features.Blog;

		blog.Should().BeEquivalentTo(new BlogFeatureConfig());
		blog.Enabled.Should().BeFalse();
	}

	[Theory]
	[InlineData("rss", new[] { "rss" })]
	[InlineData("atom", new[] { "atom" })]
	[InlineData("RSS, Atom", new[] { "rss", "atom" })]
	[InlineData("[atom, rss, atom]", new[] { "atom", "rss" })]
	[InlineData("none", new string[0])]
	[InlineData("false", new string[0])]
	[InlineData("[]", new string[0])]
	[InlineData("json", new[] { "json" })]
	public void Parse_Feed_TakesANameAListOrNone(string feed, string[] expected)
	{
		string yaml = $"site:\n  title: Docs\nfeatures:\n  blog:\n    enabled: true\n    feed: {feed}\n";

		SiteConfigReader.Parse(yaml).Features.Blog.Feeds.Should().Equal(expected);
	}

	[Fact]
	public void Parse_AuthorKeys_MatchWithoutRegardToCase()
	{
		const string yaml = "site:\n  title: Docs\nfeatures:\n  blog:\n    authors:\n      Jane:\n        name: Jane\n";

		SiteConfigReader.Parse(yaml).Features.Blog.Authors.Should().ContainKey("jane");
	}

	[Fact]
	public void Parse_UnknownBlogKeys_AreWarnings_IncludingInsideAnAuthor()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    features:
		                      blog:
		                        enabled: true
		                        feeds: atom
		                        postPerPage: 3
		                        authors:
		                          jane:
		                            name: Jane
		                            avatr: jane.png
		                    """;

		SiteConfigReader.Parse(yaml).Warnings.Should().Equal(
			"unknown key 'features.blog.feeds' (line 6)",
			"unknown key 'features.blog.postPerPage' (line 7)",
			"unknown key 'features.blog.authors.jane.avatr' (line 11)");
	}

	[Fact]
	public void Parse_EveryDocumentedBlogKey_IsKnown()
	{
		const string yaml = """
		                    site:
		                      title: Docs
		                    features:
		                      blog:
		                        enabled: true
		                        path: blog
		                        route: /blog
		                        title: Blog
		                        description: Posts
		                        postsPerPage: 10
		                        sidebarPosts: 5
		                        showAuthors: true
		                        feed: rss
		                        authors:
		                          jane:
		                            name: Jane
		                            url: https://example.com
		                            avatar: jane.png
		                    """;

		SiteConfigReader.Parse(yaml).Warnings.Should().BeEmpty();
	}
}
