using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     Theme stylesheet and script URLs carry a hash of the file the build writes, so a browser or
///     CDN that cached the old file for a day fetches the new one after an upgrade.
/// </summary>
public sealed class ThemeAssetVersionTests
{
	[Fact]
	public async Task EmbeddedTheme_LinksItsCssAndJsWithAHashOfTheWrittenBytes()
	{
		SiteConfig config = MockSite.Config() with { Build = new BuildConfig { BasePath = "/Repo" } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\nlayout: landing\n---\nBody.\n"),
			["/project/docs/guide.md"] = new("---\ntitle: Guide\n---\nText.\n")
		});

		foreach (string page in new[] { "/project/_site/index.html", "/project/_site/guide/index.html" })
		{
			string html = fs.File.ReadAllText(page);
			html.Should().Contain($"<link rel=\"stylesheet\" href=\"/Repo/_theme/css/main.css?v={Hash(fs, "/project/_site/_theme/css/main.css")}\" />")
				.And.Contain($"<script src=\"/Repo/_theme/js/main.js?v={Hash(fs, "/project/_site/_theme/js/main.js")}\"></script>");
		}
	}

	[Fact]
	public async Task CustomTheme_LinksItsOwnFilesWithTheirHashes()
	{
		SiteConfig config = MockSite.Config() with { Theme = new ThemeConfig { Name = "./mytheme" } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\nBody.\n"),
			["/project/mytheme/layouts/default.html"] = new(
				"<html><head>{{ for css in css_files }}<link href=\"{{ css }}\">{{ end }}</head><body>{{ page.content }}{{ for js in js_files }}<script src=\"{{ js }}\"></script>{{ end }}</body></html>"),
			["/project/mytheme/css/site.css"] = new("body { color: red; }"),
			["/project/mytheme/js/site.js"] = new("console.log(1);")
		});

		string html = fs.File.ReadAllText("/project/_site/index.html");
		html.Should().Contain($"<link href=\"/_theme/css/site.css?v={Hash(fs, "/project/_site/_theme/css/site.css")}\">")
			.And.Contain($"<script src=\"/_theme/js/site.js?v={Hash(fs, "/project/_site/_theme/js/site.js")}\"></script>");
	}

	[Fact]
	public async Task ChangedThemeFile_GetsANewUrl()
	{
		async Task<string> CssUrl(string css)
		{
			(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config() with { Theme = new ThemeConfig { Name = "./t" } },
				new Dictionary<string, MockFileData>
				{
					["/project/docs/index.md"] = new("---\ntitle: Home\n---\nBody.\n"),
					["/project/t/layouts/default.html"] = new("{{ for css in css_files }}{{ css }}{{ end }}"),
					["/project/t/css/site.css"] = new(css)
				});
			return fs.File.ReadAllText("/project/_site/index.html");
		}

		string before = await CssUrl("body { color: red; }");
		string after = await CssUrl("body { color: blue; }");

		before.Should().MatchRegex("^/_theme/css/site\\.css\\?v=[0-9a-f]{10}$");
		after.Should().MatchRegex("^/_theme/css/site\\.css\\?v=[0-9a-f]{10}$").And.NotBe(before);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NotFoundPage_LinksTheThemeWithTheSameHashAsThePages(bool minify)
	{
		// The 404 page linked the bare main.css and main.js, so after an upgrade it could pair
		// with a cached stylesheet from the old release.
		SiteConfig config = MockSite.Config() with { Build = new BuildConfig { BasePath = "/Repo", Minify = minify } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\nBody.\n")
		});

		string css = $"/Repo/_theme/css/main.css?v={Hash(fs, "/project/_site/_theme/css/main.css")}";
		string js = $"/Repo/_theme/js/main.js?v={Hash(fs, "/project/_site/_theme/js/main.js")}";
		fs.File.ReadAllText("/project/_site/404.html").Should()
			.Contain($"<link rel=\"stylesheet\" href=\"{css}\" />")
			.And.Contain($"<script src=\"{js}\"></script>");
		fs.File.ReadAllText("/project/_site/index.html").Should().Contain(css).And.Contain(js);
	}

	[Fact]
	public async Task MinifiedBuild_WritesSmallerThemeFilesAndHashesThem()
	{
		Dictionary<string, MockFileData> Files() => new() { ["/project/docs/index.md"] = new("---\ntitle: Home\n---\nBody.\n") };
		(_, MockFileSystem plain) = await MockSite.BuildAsync(MockSite.Config(), Files());
		(_, MockFileSystem minified) = await MockSite.BuildAsync(
			MockSite.Config() with { Build = new BuildConfig { Minify = true } }, Files());

		foreach (string file in new[] { "/project/_site/_theme/css/main.css", "/project/_site/_theme/js/main.js" })
		{
			minified.FileInfo.New(file).Length.Should().BeLessThan(plain.FileInfo.New(file).Length * 9 / 10);
			Hash(minified, file).Should().NotBe(Hash(plain, file));
			minified.File.ReadAllText("/project/_site/index.html").Should().Contain("?v=" + Hash(minified, file));
		}

		// The style stylesheets too, and the page's style loader and main.js name them by the minified bytes.
		foreach (string style in EmbeddedThemeProvider.StyleFileNames)
		{
			string file = $"/project/_site/_theme/css/style-{style}.css";
			minified.FileInfo.New(file).Length.Should().BeLessThan(plain.FileInfo.New(file).Length * 9 / 10);
			minified.File.ReadAllText("/project/_site/index.html").Should().Contain($"'{style}':'{Hash(minified, file)}',");
			minified.File.ReadAllText("/project/_site/_theme/js/main.js").Should().Contain($"\"{style}\":\"{Hash(minified, file)}\"");
		}

		minified.FileInfo.New("/project/_site/_theme/css/layouts.css").Length.Should()
			.BeLessThan(plain.FileInfo.New("/project/_site/_theme/css/layouts.css").Length * 9 / 10);
	}

	[Fact]
	public async Task CustomTheme_NotFoundPageIsRenderedWithTheThemesLayoutAndHashedCss()
	{
		SiteConfig config = MockSite.Config() with { Theme = new ThemeConfig { Name = "./mytheme" } };

		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, new Dictionary<string, MockFileData>
		{
			["/project/docs/index.md"] = new("---\ntitle: Home\n---\nBody.\n"),
			["/project/mytheme/layouts/default.html"] = new(
				"<html><head>{{ for css in css_files }}<link href=\"{{ css }}\">{{ end }}</head><body class=\"mine\">{{ page.content }}</body></html>"),
			["/project/mytheme/css/main.css"] = new("body { color: red; }")
		});

		string notFound = fs.File.ReadAllText("/project/_site/404.html");
		notFound.Should().Contain($"href=\"/_theme/css/main.css?v={Hash(fs, "/project/_site/_theme/css/main.css")}\"")
			.And.Contain("<body class=\"mine\">").And.Contain("<h1>Page not found</h1>");
	}

	private static string Hash(MockFileSystem fs, string path) =>
		Convert.ToHexStringLower(SHA256.HashData(fs.File.ReadAllBytes(path)))[..10];
}
