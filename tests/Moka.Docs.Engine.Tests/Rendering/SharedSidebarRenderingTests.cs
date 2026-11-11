using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Rendering;

/// <summary>
///     What a page holds of a shared sidebar, and the sidebar rendered once for all pages.
/// </summary>
public sealed class SharedSidebarRenderingTests
{
	private const string _payload = "x\" data-broke=\"1\"><b data-injected>'";
	private const string _script = "/_theme/js/sidebar.js?v=0123456789";

	private static readonly DocPage[] _pages =
	[
		Page("/", "Home"), Page("/guide", "Guide"), Page("/guide/intro", "Intro"), Page("/guide/advanced", "Advanced"),
		Page("/guide/advanced/caching", "Caching"), Page("/reference/cli", "CLI"), Page("/reference/config", "Config"),
		Page("/api/contoso/canvas", "Canvas")
	];

	private static readonly NavigationTree _nav = new()
	{
		Items =
		[
			Node("Guide", "/guide",
				Node("Intro", "/guide/intro"),
				Node("Advanced", "/guide/advanced", Node("Caching", "/guide/advanced/caching"))),
			Node("Reference", "/reference", Node("CLI", "/reference/cli"), Node("Config", "/reference/config")) with
			{
				Expanded = false
			},
			Node("API", null, Node("Contoso", null, Node("Canvas", "/api/contoso/canvas"))),
			Node("Empty group", null, Node("Nothing", null)),
			Node("Home", "/")
		]
	};

	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	private static DocPage Page(string route, string title) => new()
	{
		Route = route,
		FrontMatter = new FrontMatter { Title = title },
		Content = new PageContent { Html = "<p>Body</p>" }
	};

	private static NavigationNode Node(string label, string? route, params NavigationNode[] children) =>
		new() { Label = label, Route = route, Children = [.. children] };

	private static ThemeRenderContext Context(string? sidebarScript, string basePath = "/",
		NavigationTree? nav = null, IReadOnlyList<DocPage>? pages = null) => new()
	{
		Config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Docs" },
			Build = new BuildConfig { BasePath = basePath }
		},
		Navigation = nav ?? _nav,
		Templates = EmbeddedThemeProvider.CreateDefault().Templates,
		Partials = [],
		AllPages = pages ?? _pages,
		SidebarScriptUrl = sidebarScript
	};

	private static string Sidebar(string html) =>
		Regex.Match(html, "<aside class=\"sidebar\".*?</aside>", RegexOptions.Singleline).Value;

	private static List<string> Links(string html) =>
		Regex.Matches(html, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();

	private string Render(string route, ThemeRenderContext context) =>
		_engine.RenderPage(_pages.Single(p => p.Route == route), context, TestContext.Current.CancellationToken);

	[Fact]
	public void SharedSidebar_PageHoldsTheTopLevelAndItsOwnPath()
	{
		string sidebar = Sidebar(Render("/guide/advanced/caching", Context(_script)));

		// The API namespace group has no route, so it links to its first type; the empty group
		// leads nowhere and is left out.
		Links(sidebar).Should().Equal(
			"/guide", "/guide/intro", "/guide/advanced", "/guide/advanced/caching", "/reference", "/api/contoso/canvas", "/");
		sidebar.Should().Contain("<a class=\"nav-link current\" href=\"/guide/advanced/caching\">Caching</a>");
		sidebar.Should().NotContain("Empty group");
		sidebar.Should().Contain("data-nav-route=\"/guide/advanced/caching\"");
		sidebar.Should().Contain($"<script src=\"{_script}\"></script></aside>");
	}

	[Fact]
	public void SharedSidebar_SectionPage_KeepsItsChildren()
	{
		string sidebar = Sidebar(Render("/guide", Context(_script)));

		Links(sidebar).Should().Equal("/guide", "/guide/intro", "/guide/advanced", "/reference", "/api/contoso/canvas", "/");
	}

	[Fact]
	public void InlineSidebar_HoldsEveryEntryAndNoScript()
	{
		string sidebar = Sidebar(Render("/guide/intro", Context(null)));

		// /reference has no page, so it is a heading; the empty group's entry has no route at all.
		Links(sidebar).Should().Equal("/guide", "/guide/intro", "/guide/advanced", "/guide/advanced/caching",
			"/reference/cli", "/reference/config", "/api/contoso/canvas", "", "/");
		sidebar.Should().NotContain("<script").And.NotContain("data-nav-route");
		sidebar.Should().StartWith("<aside class=\"sidebar\" id=\"sidebar\">");
	}

	[Fact]
	public void SharedSidebar_WithABasePath_PrefixesTheScriptAndTheRoute()
	{
		string sidebar = Sidebar(Render("/guide/intro", Context(_script, "/Repo")));

		sidebar.Should().Contain("data-nav-route=\"/Repo/guide/intro\"");
		sidebar.Should().Contain("<script src=\"/Repo/_theme/js/sidebar.js?v=0123456789\"></script>");
		Links(sidebar).Should().AllSatisfy(link => link.Should().StartWith("/Repo/"));
	}

	[Fact]
	public void SharedSidebar_EscapesTheRouteAndTheScriptUrl()
	{
		DocPage page = Page("/guide/" + _payload, "Odd");
		var nav = new NavigationTree { Items = [Node("Guide " + _payload, "/guide", Node("Odd " + _payload, page.Route))] };

		string html = _engine.RenderPage(page, Context("/_theme/js/sidebar.js?v=" + _payload, nav: nav, pages: [page]),
			TestContext.Current.CancellationToken);

		html.Should().NotContain("data-broke=\"1\"").And.NotContain("<b data-injected");
		html.Should().Contain("data-broke=&quot;1&quot;");
	}

	[Fact]
	public void RenderSidebar_ListsEveryEntryWithNothingCurrent()
	{
		string html = _engine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(), Context(null, "/Repo"),
			TestContext.Current.CancellationToken);

		html.Trim().Should().StartWith("<nav class=\"sidebar-nav\"").And.EndWith("</nav>");
		Links(html).Should().Equal("/Repo/guide", "/Repo/guide/intro", "/Repo/guide/advanced", "/Repo/guide/advanced/caching",
			"/Repo/reference/cli", "/Repo/reference/config", "/Repo/api/contoso/canvas", "", "/Repo/");
		html.Should().NotContain(" current\"").And.NotContain("parent-active").And.NotContain(" active\"");
		Regex.Matches(html, "class=\"nav-children\"").Should().ContainSingle("only Reference starts collapsed");
		Regex.Matches(html, "class=\"nav-children expanded\"").Should().HaveCount(5);
	}

	[Fact]
	public void CreateSidebarScript_HoldsTheMarkupAsOneCompactString()
	{
		string html = _engine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(),
			Context(null, nav: new NavigationTree { Items = [Node("Line" + (char)0x2028 + "break </script> & \"quotes\"", "/a")] }),
			TestContext.Current.CancellationToken);

		string script = EmbeddedThemeProvider.CreateSidebarScript(html);

		string literal = Regex.Match(script, "template\\.innerHTML = (\".*\");").Groups[1].Value;
		literal.Should().StartWith("\"<nav class=\\\"sidebar-nav\\\"");
		literal.Should().NotContain("\\n ", "the layout's indentation is dropped");
		literal.Should().Contain("Line\\u2028break &lt;/script&gt; &amp; &quot;quotes&quot;");
		literal.Should().NotContain("\\u003C", "HTML characters stay as they are");
		script.Should().NotContain("__SIDEBAR_HTML__");
	}
}
