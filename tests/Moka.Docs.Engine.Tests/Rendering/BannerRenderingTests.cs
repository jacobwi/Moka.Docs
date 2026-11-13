using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Engine.Tests.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Rendering;

/// <summary>
///     The announcement banner of <c>theme.options.banner</c> in the default theme's page and landing
///     layouts.
/// </summary>
public sealed class BannerRenderingTests
{
	private readonly ScribanTemplateEngine _engine = new(NullLogger<ScribanTemplateEngine>.Instance);

	private static readonly DocPage _page = new()
	{
		Route = "/guide/intro",
		FrontMatter = new FrontMatter { Title = "Intro" },
		Content = new PageContent { Html = "<p>Intro</p>" }
	};

	private static readonly DocPage _landing = new()
	{
		Route = "/",
		FrontMatter = new FrontMatter { Title = "Home", Layout = "landing" },
		Content = new PageContent { Html = "<h1>Home</h1>" }
	};

	private string Render(DocPage page, ThemeBanner? banner, string basePath = "/")
	{
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		var context = new ThemeRenderContext
		{
			Config = new SiteConfig
			{
				Site = new SiteMetadata { Title = "Test" },
				Theme = new ThemeConfig { Options = new ThemeOptions { Banner = banner } },
				Build = new BuildConfig { BasePath = basePath }
			},
			Templates = theme.Templates,
			Partials = theme.Partials,
			CssFiles = theme.CssFiles,
			JsFiles = theme.JsFiles,
			AllPages = [_page, _landing]
		};

		return _engine.RenderPage(page, context, TestContext.Current.CancellationToken);
	}

	private static string Banner(string html) =>
		Regex.Match(html, "<div class=\"site-banner\".*?</div>\\s*</div>", RegexOptions.Singleline).Value;

	[Fact]
	public void NoBanner_WritesNoMarkupOrScript()
	{
		string html = Render(_page, null);

		html.Should().NotContain("site-banner").And.NotContain("mokadocs-banner");
	}

	[Theory]
	[InlineData("default")]
	[InlineData("landing")]
	public void Banner_ComesRightAfterTheSkipLink_InBothLayouts(string layout)
	{
		string html = Render(layout == "landing" ? _landing : _page, new ThemeBanner { Text = "Version 2 is out" });

		int skip = html.IndexOf("class=\"skip-link\"", StringComparison.Ordinal);
		int banner = html.IndexOf("<div class=\"site-banner\"", StringComparison.Ordinal);
		int header = html.IndexOf("<header class=\"site-header\"", StringComparison.Ordinal);
		skip.Should().BeGreaterThan(0);
		banner.Should().BeGreaterThan(skip);
		header.Should().BeGreaterThan(banner);
		html.IndexOf("<body", StringComparison.Ordinal).Should().BeLessThan(skip);
	}

	[Fact]
	public void Banner_IsALabelledRegionWithTheTextEscaped()
	{
		string html = Render(_page, new ThemeBanner { Text = "Try <b>2.0</b> & more" });
		string banner = Banner(html);

		banner.Should().Contain("role=\"region\"").And.Contain("aria-label=\"Announcement\"");
		banner.Should().Contain("Try &lt;b&gt;2.0&lt;/b&gt; &amp; more").And.NotContain("<b>");
		banner.Should().Contain("data-variant=\"info\"");
	}

	[Fact]
	public void Link_FromTheSiteRoot_GetsTheBasePath_AndAbsoluteLinksStayAsWritten()
	{
		string rooted = Render(_page, new ThemeBanner { Text = "News", Link = "/blog/v2" }, "/Docs");
		string absolute = Render(_page, new ThemeBanner { Text = "News", Link = "https://example.com/v2", LinkText = "Read the post" }, "/Docs");
		string protocolRelative = Render(_page, new ThemeBanner { Text = "News", Link = "//cdn.example.com/v2" }, "/Docs");

		rooted.Should().Contain("<a class=\"site-banner-link\" href=\"/Docs/blog/v2\">Learn more<svg");
		absolute.Should().Contain("<a class=\"site-banner-link\" href=\"https://example.com/v2\">Read the post<svg");
		protocolRelative.Should().Contain("href=\"//cdn.example.com/v2\"");
	}

	[Fact]
	public void NoLink_WritesNoAnchor()
	{
		Banner(Render(_page, new ThemeBanner { Text = "Maintenance tonight" })).Should().NotContain("<a ");
	}

	[Fact]
	public void Dismissible_HasAScriptOnlyButtonAndHidesADismissedIdBeforePaint()
	{
		var banner = new ThemeBanner { Text = "Version 2 is out", Id = "v2-launch" };
		string html = Render(_page, banner);

		Banner(html).Should().Contain("<button class=\"site-banner-close js-only\" type=\"button\" aria-label=\"Dismiss announcement\">");
		html.Should().Contain("data-banner-id=\"v2-launch\"");
		string script = Regex.Match(html, "</div>\\s*</div>\\s*(<script>.*?</script>)", RegexOptions.Singleline).Groups[1].Value;
		script.Should().Contain("localStorage.getItem('mokadocs-banner-dismissed')")
			.And.Contain("getAttribute('data-banner-id')")
			.And.Contain("b.hidden=true")
			.And.Contain("try{");
	}

	[Theory]
	[InlineData("'v2-launch'", true)]
	[InlineData("'v1-launch'", false)]
	[InlineData("null", false)]
	[InlineData("blocked", false)]
	public void InlineScript_HidesTheBannerOnlyWhenThisIdWasDismissed(string stored, bool hidden)
	{
		Assert.SkipUnless(NodeScript.Available, "node is not on PATH");
		string html = Render(_page, new ThemeBanner { Text = "Version 2 is out", Id = "v2-launch" });
		string script = Regex.Match(html, "</div>\\s*</div>\\s*<script>(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
		string storage = stored == "blocked"
			? "{ getItem() { throw new Error('SecurityError'); } }"
			: $"{{ getItem: k => k === 'mokadocs-banner-dismissed' ? {stored} : null }}";

		string result = NodeScript.Run($$"""
		                                 const localStorage = {{storage}};
		                                 const banner = { hidden: false, getAttribute: n => n === 'data-banner-id' ? 'v2-launch' : null };
		                                 const document = { getElementById: id => id === 'mokadocs-banner' ? banner : null };
		                                 {{script}}
		                                 process.stdout.write(JSON.stringify(banner.hidden));
		                                 """);

		result.Should().Be(hidden ? "true" : "false");
	}

	[Fact]
	public void NotDismissible_HasNoButtonAndNoScript()
	{
		string html = Render(_page, new ThemeBanner { Text = "Read only", Dismissible = false });

		html.Should().Contain("<div class=\"site-banner\"");
		html.Should().NotContain("site-banner-close").And.NotContain("mokadocs-banner-dismissed");
	}

	[Theory]
	[InlineData("warning", "warning", true)]
	[InlineData("ACCENT", "accent", false)]
	[InlineData("loud", "info", false)]
	[InlineData("", "info", false)]
	public void Variant_IsResolved_AndOnlyWarningHasAnIcon(string configured, string expected, bool icon)
	{
		string banner = Banner(Render(_page, new ThemeBanner { Text = "News", Variant = configured }));

		banner.Should().Contain($"data-variant=\"{expected}\"");
		banner.Contains("site-banner-icon", StringComparison.Ordinal).Should().Be(icon);
	}

	[Fact]
	public void Id_DefaultsToAHashOfTextAndLink()
	{
		var banner = new ThemeBanner { Text = "Version 2 is out", Link = "/blog/v2" };
		string html = Render(_page, banner);

		banner.EffectiveId.Should().MatchRegex("^[0-9a-f]{12}$");
		html.Should().Contain($"data-banner-id=\"{banner.EffectiveId}\"");
	}
}
