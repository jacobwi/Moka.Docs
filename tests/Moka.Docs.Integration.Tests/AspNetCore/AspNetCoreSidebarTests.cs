using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     <c>MokaDocsOptions.Sidebar</c>: a shared sidebar is one more file of the in-memory site, served
///     under the base path with an ETag.
/// </summary>
public sealed class AspNetCoreSidebarTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-sidebar-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreSidebarTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs", "guide", "deep"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nThe home page.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "guide", "index.md"), "---\ntitle: Guide\n---\n\nGuide.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "guide", "intro.md"), "---\ntitle: Intro\n---\n\nIntro.\n");
		File.WriteAllText(Path.Combine(_root, "docs", "guide", "deep", "page.md"), "---\ntitle: Deep Page\n---\n\nDeep.\n");
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
	public async Task SharedSidebar_IsServedUnderTheBasePathWithAnETag()
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.Sidebar = "shared";
		});

		string page = await host.GetTextAsync("/docs/guide/intro");
		Match script = Regex.Match(page, "<script src=\"(/docs/_theme/js/sidebar\\.js)\\?v=([0-9a-f]{10})\"></script></aside>");
		script.Success.Should().BeTrue("the page should load the shared sidebar");
		page.Should().Contain("data-nav-route=\"/docs/guide/intro\"");

		HttpResponseMessage response = await host.GetAsync(script.Groups[1].Value + "?v=" + script.Groups[2].Value);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType!.MediaType.Should().Be("application/javascript");
		byte[] body = await response.Content.ReadAsByteArrayAsync(Ct);
		Convert.ToHexStringLower(SHA256.HashData(body))[..10].Should().Be(script.Groups[2].Value);
		string text = System.Text.Encoding.UTF8.GetString(body);
		text.Should().Contain("href=\\\"/docs/guide/deep/page\\\"").And.Contain("href=\\\"/docs/api/");

		string etag = response.Headers.ETag!.Tag;
		HttpResponseMessage again = await host.GetAsync(script.Groups[1].Value, etag);
		again.StatusCode.Should().Be(HttpStatusCode.NotModified);
	}

	[Fact]
	public async Task DefaultSidebar_SmallSite_IsInlineAndTheGuideIsATree()
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options => options.DocsPath = "docs");

		string page = await host.GetTextAsync("/docs/guide/intro");
		page.Should().NotContain("sidebar.js");
		(await host.GetAsync("/docs/_theme/js/sidebar.js")).StatusCode.Should().Be(HttpStatusCode.NotFound);

		// The default Guide entry lists the folder tree below /guide, as the CLI does.
		string sidebar = Regex.Match(page, "<aside class=\"sidebar\".*?</aside>", RegexOptions.Singleline).Value;
		sidebar.Should().Contain("<span class=\"nav-link nav-group-label\">Deep</span>")
			.And.Contain("href=\"/docs/guide/deep/page\"");
	}
}
