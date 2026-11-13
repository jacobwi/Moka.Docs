using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Rendering;

/// <summary>
///     The version dropdown linked every entry to the version's home page. It now keeps the reader on
///     the same page when the other version has it.
/// </summary>
public sealed class VersionDropdownTests
{
	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	private static readonly DocVersion _v2 = new()
	{
		Label = "v2.0", Slug = "v2.0", IsDefault = true, UrlPath = "/docs",
		Routes = new HashSet<string> { "/", "/guide/intro", "/guide/new" }
	};

	private static readonly DocVersion _v1 = new()
	{
		Label = "v1.0", Slug = "v1.0", UrlPath = "/docs/v1.0",
		Routes = new HashSet<string> { "/", "/guide/intro", "/guide/legacy" }
	};

	private static readonly DocVersion _next = new()
	{
		Label = "next", Slug = "next", IsPrerelease = true, UrlPath = "/docs/next"
	};

	[Theory]
	[InlineData("default")]
	[InlineData("landing")]
	public void Links_KeepThePageInVersionsThatHaveIt(string layout)
	{
		Dictionary<string, string> links = Links(Render("/guide/intro", _v1, layout));

		links.Should().Equal(new Dictionary<string, string>
		{
			["v2.0"] = "/docs/guide/intro",
			["v1.0"] = "/docs/v1.0/guide/intro",
			["next"] = "/docs/next/"
		});
	}

	[Fact]
	public void Links_FallBackToTheHomePageOfAVersionWithoutThePage()
	{
		Dictionary<string, string> links = Links(Render("/guide/legacy", _v1));

		links["v2.0"].Should().Be("/docs/");
		links["next"].Should().Be("/docs/next/");
	}

	[Fact]
	public void CurrentVersion_IsMarkedAndLinksToThePageItself()
	{
		string html = Render("/guide/new", _v2);

		html.Should().Contain("<span class=\"version-label\">v2.0</span>");
		Regex.Matches(html, "class=\"version-option active\"").Should().ContainSingle();
		html.Should().Contain("class=\"version-option active\" href=\"/docs/v2.0/guide/new\" data-version-slug=\"v2.0\" aria-current=\"page\"");
		Links(html)["v1.0"].Should().Be("/docs/v1.0/");
	}

	[Fact]
	public void VersionsWithoutAPlace_LinkTheDefaultToTheRootAndTheOthersToTheirSlug()
	{
		// A single build that only lists versions, as before multi-version builds.
		DocVersion stable = new() { Label = "v2.0", Slug = "v2.0", IsDefault = true };
		DocVersion old = new() { Label = "v1.0", Slug = "v1.0" };

		Dictionary<string, string> links = Links(Render("/guide/intro", stable, versions: [stable, old], basePath: "/docs"));

		links["v2.0"].Should().Be("/docs/guide/intro");
		links["v1.0"].Should().Be("/docs/v1.0/");
	}

	[Theory]
	[InlineData("default")]
	[InlineData("landing")]
	public void Menu_IsAListOfLinksNotAListboxOfOptions(string layout)
	{
		// Options wrapping links read as neither: screen readers announced a listbox whose items
		// couldn't be selected.
		string menu = Regex.Match(Render("/guide/intro", _v2, layout), "<div class=\"version-selector\">.*?</ul>", RegexOptions.Singleline).Value;

		Regex.Match(menu, "<button class=\"version-trigger\"[^>]*>").Value.Should()
			.Contain("aria-expanded=\"false\"").And.Contain("aria-controls=\"versionMenu\"").And.NotContain("aria-haspopup");
		menu.Should().Contain("<ul class=\"version-dropdown\" id=\"versionMenu\" hidden>");
		menu.Should().NotContain("role=").And.NotContain("aria-selected");
		menu.Should().Contain("<span class=\"visually-hidden\">Version </span><span class=\"version-label\">v2.0</span>");
	}

	private string Render(string route, DocVersion current, string layout = "default",
		IReadOnlyList<DocVersion>? versions = null, string? basePath = null)
	{
		var page = new DocPage
		{
			Route = route,
			FrontMatter = new FrontMatter { Title = "Page", Layout = layout },
			Content = new PageContent { Html = "<p>Page</p>" }
		};

		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		var context = new ThemeRenderContext
		{
			Config = new SiteConfig
			{
				Site = new SiteMetadata { Title = "Test" },
				Build = new BuildConfig { BasePath = basePath ?? (current.UrlPath is null ? "/" : BasePathOf(current)) }
			},
			Templates = theme.Templates,
			Partials = theme.Partials,
			AllPages = [page],
			Versions = versions ?? [_v2, _v1, _next],
			CurrentVersion = current
		};

		return _engine.RenderPage(page, context, TestContext.Current.CancellationToken);
	}

	// The default version's own pages are built twice; the copy rendered here is the one in its folder.
	private static string BasePathOf(DocVersion version) => "/docs/" + version.Slug;

	private static Dictionary<string, string> Links(string html) =>
		Regex.Matches(html, "<a class=\"version-option[^\"]*\" href=\"([^\"]*)\" data-version-slug=\"([^\"]*)\"")
			.ToDictionary(m => WebUtility.HtmlDecode(m.Groups[2].Value), m => WebUtility.HtmlDecode(m.Groups[1].Value));
}
