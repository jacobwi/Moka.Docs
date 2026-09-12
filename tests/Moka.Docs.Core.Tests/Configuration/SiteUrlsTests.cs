using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

public sealed class SiteUrlsTests
{
	[Theory]
	// site.url already includes the base path (this repository's own config)
	[InlineData("https://user.github.io/Repo", "/Repo", "/guide", "https://user.github.io/Repo/guide")]
	// site.url is the origin and the base path is separate
	[InlineData("https://user.github.io", "/Repo", "/guide", "https://user.github.io/Repo/guide")]
	[InlineData("https://user.github.io/", "/Repo", "/", "https://user.github.io/Repo/")]
	[InlineData("https://docs.example.com", "/", "/guide/intro", "https://docs.example.com/guide/intro")]
	[InlineData("https://docs.example.com/", "/", "sitemap.xml", "https://docs.example.com/sitemap.xml")]
	[InlineData("https://user.github.io/repo", "/Repo", "/a", "https://user.github.io/repo/a")]
	public void Absolute_NeverDoublesOrDropsTheBasePath(string siteUrl, string basePath, string route, string expected) =>
		SiteUrls.Absolute(siteUrl, basePath, route).Should().Be(expected);

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Absolute_NoSiteUrl_IsEmpty(string siteUrl) =>
		SiteUrls.Absolute(siteUrl, "/Repo", "/guide").Should().BeEmpty();
}
