using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Engine.Phases;
using Moka.Docs.Themes.Default;

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
	[InlineData(false, null, "Production", true)]
	[InlineData(false, null, "Development", false)]
	[InlineData(false, false, "Production", false)]
	[InlineData(false, true, "Development", true)]
	[InlineData(true, null, "Development", true)]
	[InlineData(true, false, "Production", true)]
	public async Task Pages_LinkThemeFilesWithTheHashOfWhatTheHostServes(bool minify, bool? minifyAssets, string environment,
		bool minifiedAssets)
	{
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.Minify = minify;
			options.MinifyAssets = minifyAssets;
		}, environment: environment);

		string page = await host.GetTextAsync("/docs/");
		page.Contains("\n\n", StringComparison.Ordinal).Should().Be(!minify, "minify drops the blank lines a condition leaves");
		Match css = Regex.Match(page, "<link rel=\"stylesheet\" href=\"(/docs/_theme/css/main\\.css)\\?v=([0-9a-f]{10})\"");
		Match js = Regex.Match(page, "<script src=\"(/docs/_theme/js/main\\.js)\\?v=([0-9a-f]{10})\"");
		Match sprite = Regex.Match(page, "<use href=\"(/docs/_theme/icons\\.svg)\\?v=([0-9a-f]{10})#search\"/>");
		Match index = Regex.Match(page, "data-index=\"(/docs/search-index\\.json)\\?v=([0-9a-f]{10})\"");

		css.Success.Should().BeTrue("the page should link the stylesheet with a version");
		js.Success.Should().BeTrue("the page should link the script with a version");
		sprite.Success.Should().BeTrue("the icons should use the sprite with a version");
		index.Success.Should().BeTrue("the search dialog should fetch the index with a version");
		foreach (Match link in new[] { css, js, sprite, index })
		{
			HttpResponseMessage response = await host.GetAsync(link.Groups[1].Value + "?v=" + link.Groups[2].Value);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			response.Headers.NonValidated["Cache-Control"].ToString().Should().Be("public, max-age=31536000, immutable");
			byte[] body = await response.Content.ReadAsByteArrayAsync(Ct);
			Convert.ToHexStringLower(SHA256.HashData(body))[..10].Should().Be(link.Groups[2].Value);
			if (link != index)
			{
				body.Should().Equal(ThemeAssetPhase.EmbeddedAssets(minifiedAssets, ThemeText.English)[link.Groups[1].Value["/docs".Length..]]);
			}
		}

		// The page's style loader names every style's stylesheet by the hash of the bytes served.
		MatchCollection styles = Regex.Matches(page, "'(\\w+)':'([0-9a-f]{10})',");
		styles.Should().HaveCount(EmbeddedThemeProvider.StyleFileNames.Count);
		foreach (Match style in styles)
		{
			byte[] body = await (await host.GetAsync($"/docs/_theme/css/style-{style.Groups[1].Value}.css")).Content.ReadAsByteArrayAsync(Ct);
			Convert.ToHexStringLower(SHA256.HashData(body))[..10].Should().Be(style.Groups[2].Value);
		}
	}
}
