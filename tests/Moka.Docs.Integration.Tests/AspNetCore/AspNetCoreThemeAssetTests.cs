using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The host caches theme files for a day, so pages link them with a hash of their content and
///     an upgraded theme reaches readers on the next page load.
/// </summary>
public sealed class AspNetCoreThemeAssetTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-theme-assets-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreThemeAssetTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nThe home page.\n");
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

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Pages_LinkThemeFilesWithTheHashOfWhatTheHostServes(bool minify)
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.Minify = minify;
		});

		string page = await host.GetTextAsync("/docs/");
		page.Contains("\n    <meta", StringComparison.Ordinal).Should().Be(!minify, "minify drops the layout's indentation");
		Match css = Regex.Match(page, "<link rel=\"stylesheet\" href=\"(/docs/_theme/css/main\\.css)\\?v=([0-9a-f]{10})\"");
		Match js = Regex.Match(page, "<script src=\"(/docs/_theme/js/main\\.js)\\?v=([0-9a-f]{10})\"");

		css.Success.Should().BeTrue("the page should link the stylesheet with a version");
		js.Success.Should().BeTrue("the page should link the script with a version");
		foreach (Match link in new[] { css, js })
		{
			HttpResponseMessage response = await host.GetAsync(link.Groups[1].Value + "?v=" + link.Groups[2].Value);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			byte[] body = await response.Content.ReadAsByteArrayAsync(Ct);
			Convert.ToHexStringLower(SHA256.HashData(body))[..10].Should().Be(link.Groups[2].Value);
			body.Should().Equal(ThemeAssetPhase.EmbeddedAssets(minify)[link.Groups[1].Value["/docs".Length..]]);
		}
	}
}
