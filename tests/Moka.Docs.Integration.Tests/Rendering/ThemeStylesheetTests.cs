using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using Moka.Docs.Engine.Minification;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Integration.Tests.Rendering;

/// <summary>
///     Which of the default theme's files a page links, in which order, and with which hash: main.css, the
///     site's style, a style the reader picked, the layout variants, the icon sprite and the search index.
/// </summary>
public sealed class ThemeStylesheetTests
{
	private static readonly string[] _pages = ["/project/_site/index.html", "/project/_site/guide/index.html", "/project/_site/404.html"];

	private static Dictionary<string, MockFileData> Files() => new()
	{
		["/project/docs/index.md"] = new("---\ntitle: Home\nlayout: landing\n---\nBody.\n"),
		["/project/docs/guide.md"] = new("---\ntitle: Guide\nicon: rocket\n---\n# Guide\n\nText.\n")
	};

	private static Task<(Core.Pipeline.BuildContext Context, MockFileSystem FileSystem)> BuildAsync(ThemeOptions options, string basePath = "/Repo",
		bool search = true, bool? minifyAssets = null) =>
		MockSite.BuildAsync(MockSite.Config() with
		{
			Build = new BuildConfig { BasePath = basePath, MinifyAssets = minifyAssets },
			Theme = new ThemeConfig { Options = options },
			Features = new FeaturesConfig { Search = new SearchFeatureConfig { Enabled = search } }
		}, Files());

	private static string Hash(MockFileSystem fs, string path) =>
		Convert.ToHexStringLower(SHA256.HashData(fs.File.ReadAllBytes(path)))[..10];

	private static List<string> Stylesheets(string html) =>
		Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"']*)\" />").Select(m => m.Groups[1].Value).ToList();

	[Fact]
	public async Task ConfiguredStyle_IsLinkedAfterMainCss_AndTheLayoutVariantsLast()
	{
		// The layout variants' rules win over a style's at equal specificity, so their stylesheet comes last.
		ThemeOptions options = new() { Style = "Paper", Layout = new ThemeLayoutOptions { Navbar = "tabs" } };
		(_, MockFileSystem fs) = await BuildAsync(options);

		foreach (string page in _pages)
		{
			string html = fs.File.ReadAllText(page);
			Stylesheets(html).Should().Equal(
				$"/Repo/_theme/css/main.css?v={Hash(fs, "/project/_site/_theme/css/main.css")}",
				$"/Repo/_theme/css/style-paper.css?v={Hash(fs, "/project/_site/_theme/css/style-paper.css")}",
				$"/Repo/_theme/css/layouts.css?v={Hash(fs, "/project/_site/_theme/css/layouts.css")}");

			// The loader for a style the reader picked sits between main.css and the rest.
			int loader = html.IndexOf("<script>try{const v={", StringComparison.Ordinal);
			loader.Should().BeGreaterThan(html.IndexOf("/_theme/css/main.css", StringComparison.Ordinal), page)
				.And.BeLessThan(html.IndexOf("/_theme/css/style-paper.css", StringComparison.Ordinal), page);
			html.Should().Contain("if(s!=='paper'&&v.hasOwnProperty(s))document.write('<link rel=\"stylesheet\" href=\"/Repo/_theme/css/style-'+s+'.css?v='+v[s]+'\" />')");
		}
	}

	[Fact]
	public async Task StyleSelectorOff_LinksOnlyTheFilesThePageNeeds()
	{
		(_, MockFileSystem fs) = await BuildAsync(new ThemeOptions { StyleSelector = false });

		foreach (string page in _pages)
		{
			string html = fs.File.ReadAllText(page);
			Stylesheets(html).Should().Equal($"/Repo/_theme/css/main.css?v={Hash(fs, "/project/_site/_theme/css/main.css")}");
			html.Should().NotContain("_theme/css/style-").And.NotContain("layouts.css").And.NotContain("document.write");
		}

		(_, MockFileSystem paper) = await BuildAsync(new ThemeOptions { StyleSelector = false, Style = "paper" });
		Stylesheets(paper.File.ReadAllText("/project/_site/guide/index.html")).Should().HaveCount(2)
			.And.EndWith($"/Repo/_theme/css/style-paper.css?v={Hash(paper, "/project/_site/_theme/css/style-paper.css")}");
	}

	[Theory]
	[InlineData(null)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task EveryStyleStylesheet_IsWrittenWithTheVersionThePagesName(bool? minifyAssets)
	{
		(_, MockFileSystem fs) = await BuildAsync(new ThemeOptions(), minifyAssets: minifyAssets);
		Func<string, string> written = minifyAssets == false ? css => css : CssMinifier.Minify;

		string html = fs.File.ReadAllText("/project/_site/guide/index.html");
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			string path = $"/project/_site/_theme/css/style-{style}.css";
			fs.File.ReadAllText(path).Should().Be(written(EmbeddedThemeProvider.GetStyleCss(style)));
			html.Should().Contain($"'{style}':'{Hash(fs, path)}',");
		}

		fs.File.ReadAllText("/project/_site/_theme/css/layouts.css").Should().Be(written(EmbeddedThemeProvider.GetLayoutsCss()));
		fs.File.ReadAllText("/project/_site/_theme/css/rtl.css").Should().Be(written(EmbeddedThemeProvider.GetRtlCss()));
		fs.File.ReadAllText("/project/_site/_theme/css/main.css").Should().Be(written(EmbeddedThemeProvider.GetCss()));
		fs.File.ReadAllText("/project/_site/_theme/icons.svg").Should().Be(EmbeddedThemeProvider.GetIconSprite(), "the sprite is written as it is");
	}

	[Fact]
	public async Task Icons_UseTheSprite_WithAHashOfItsBytes()
	{
		ThemeOptions options = new() { SocialLinks = [new SocialLink { Icon = "github", Url = "https://example.com/repo" }] };
		(_, MockFileSystem fs) = await BuildAsync(options);

		string sprite = fs.File.ReadAllText("/project/_site/_theme/icons.svg");
		string url = $"/Repo/_theme/icons.svg?v={Hash(fs, "/project/_site/_theme/icons.svg")}";
		foreach (string page in _pages)
		{
			string html = fs.File.ReadAllText(page);
			List<string> uses = Regex.Matches(html, "<use href=\"([^\"#]*)#([^\"]+)\"/>").Select(m => m.Groups[1].Value + " " + m.Groups[2].Value).ToList();
			uses.Should().NotBeEmpty().And.OnlyContain(u => u.StartsWith(url + " ", StringComparison.Ordinal), page);
			foreach (string id in uses.Select(u => u[(url.Length + 1)..]).Distinct())
			{
				sprite.Should().Contain($"<symbol id=\"{id}\" ", "{0} uses it", page);
			}
		}

		string guide = fs.File.ReadAllText("/project/_site/guide/index.html");
		guide.Should().Contain($"<span class=\"nav-icon\"><svg class=\"icon\" xmlns=\"http://www.w3.org/2000/svg\" width=\"18\" height=\"18\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><use href=\"{url}#rocket\"/></svg></span>");
		guide.Should().Contain($"aria-label=\"github\"><svg class=\"icon\"").And.Contain($"<use href=\"{url}#github\"/>");
		Regex.Matches(guide, "<svg[^>]*>(?!<use )").Should().BeEmpty("every icon on the page comes from the sprite");
	}

	[Fact]
	public async Task CustomTheme_KeepsItsIconsInline()
	{
		(_, MockFileSystem fs) = await MockSite.BuildAsync(MockSite.Config() with { Theme = new ThemeConfig { Name = "./mytheme" } },
			new Dictionary<string, MockFileData>
			{
				["/project/docs/guide.md"] = new("---\ntitle: Guide\nicon: rocket\n---\nText.\n"),
				["/project/mytheme/layouts/default.html"] = new("<nav>{{ for item in nav }}{{ item.icon }}{{ end }}</nav>{{ icon_sprite }}|{{ search_index_url }}")
			});

		string html = fs.File.ReadAllText("/project/_site/guide/index.html");
		html.Should().Contain("<path d='M4.5 16.5c").And.NotContain("<use").And.MatchRegex(@"</nav>\|/search-index\.json\?v=[0-9a-f]{10}$");
		fs.File.Exists("/project/_site/_theme/icons.svg").Should().BeFalse();
	}

	[Fact]
	public async Task SearchIndex_IsFetchedWithAHashOfItsBytes()
	{
		(_, MockFileSystem fs) = await BuildAsync(new ThemeOptions());

		string url = $"/Repo/search-index.json?v={Hash(fs, "/project/_site/search-index.json")}";
		foreach (string page in _pages)
		{
			fs.File.ReadAllText(page).Should().Contain($"<div class=\"search-modal\" id=\"searchModal\" data-index=\"{url}\" hidden>");
		}

		(_, MockFileSystem off) = await BuildAsync(new ThemeOptions(), search: false);
		off.File.ReadAllText("/project/_site/guide/index.html").Should().NotContain("search-index").And.NotContain("searchModal");
	}
}
