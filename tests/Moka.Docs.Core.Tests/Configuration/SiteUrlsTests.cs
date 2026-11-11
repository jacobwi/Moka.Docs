using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

public sealed class SiteUrlsTests
{
	[Theory]
	// site.url already includes the base path (this repository's own config)
	[InlineData("https://user.github.io/Repo", "/Repo", "/guide", "https://user.github.io/Repo/guide/")]
	// site.url is the origin and the base path is separate
	[InlineData("https://user.github.io", "/Repo", "/guide", "https://user.github.io/Repo/guide/")]
	[InlineData("https://user.github.io/", "/Repo", "/", "https://user.github.io/Repo/")]
	[InlineData("https://docs.example.com", "/", "/guide/intro", "https://docs.example.com/guide/intro/")]
	[InlineData("https://user.github.io/repo", "/Repo", "/a", "https://user.github.io/repo/a/")]
	public void Absolute_NeverDoublesOrDropsTheBasePath(string siteUrl, string basePath, string route, string expected) =>
		SiteUrls.Absolute(siteUrl, basePath, route).Should().Be(expected);

	[Theory]
	// Pages are written as {route}/index.html; without the slash GitHub Pages answers with a redirect.
	[InlineData("/guide/", "https://x.dev/guide/")]
	[InlineData("", "https://x.dev/")]
	// A raw & made the whole sitemap invalid XML; spaces and non-ASCII were not encoded.
	[InlineData("/Q&A", "https://x.dev/Q%26A/")]
	[InlineData("/my page/café", "https://x.dev/my%20page/caf%C3%A9/")]
	[InlineData("/a\"b<c>", "https://x.dev/a%22b%3Cc%3E/")]
	public void Absolute_EncodesSegmentsAndEndsWithASlash(string route, string expected) =>
		SiteUrls.Absolute("https://x.dev", "/", route).Should().Be(expected);

	[Theory]
	[InlineData("https://docs.example.com/", "/", "sitemap.xml", "https://docs.example.com/sitemap.xml")]
	[InlineData("https://user.github.io", "/Repo", "/sitemap.xml", "https://user.github.io/Repo/sitemap.xml")]
	public void AbsoluteFile_HasNoTrailingSlash(string siteUrl, string basePath, string path, string expected) =>
		SiteUrls.AbsoluteFile(siteUrl, basePath, path).Should().Be(expected);

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Absolute_NoSiteUrl_IsEmpty(string siteUrl) =>
		SiteUrls.Absolute(siteUrl, "/Repo", "/guide").Should().BeEmpty();
}
