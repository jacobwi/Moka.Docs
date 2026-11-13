using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using Moka.Docs.Serve;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     The theme lab previews a layout variant by switching its attribute on <c>&lt;html&gt;</c>, and
///     says "needs a rebuild" for the variants that can't be shown that way. Which ones those are is
///     decided here from what the theme renders: a site is built with every variant and compared with
///     the default build, so a template change that adds markup for a variant fails this test until
///     <see cref="ThemeLabCatalog" /> lists it.
/// </summary>
public sealed partial class ThemeLabRebuildTests
{
	private static readonly Lazy<Task<Dictionary<string, string>>> _defaultPages = new(() => BuildPagesAsync("width", "default"));

	private static readonly Lazy<Task<Dictionary<string, string>>> _defaultSelectorPages =
		new(() => BuildPagesAsync("width", "default", layoutSelector: true));

	// Variants the theme script names but reads on every pass, so they switch on an open page:
	// initAutohide checks data-navbar each frame (EmbeddedThemeRuntimeTests.Autohide_FollowsTheAttributeOnAnOpenPage).
	private static readonly HashSet<string> _readOnEveryPass = new(StringComparer.Ordinal) { "data-navbar=autohide" };

	private static Dictionary<string, MockFileData> Files() => new()
	{
		{
			MockSite.Root + "/docs/index.md",
			new MockFileData("---\ntitle: Home\ndescription: The home page\nlayout: landing\nheroCodeLanguage: csharp\n" +
			                 "heroCode: |\n  var list = new List<int>();\nfeatures:\n  - title: Fast\n    icon: zap\n" +
			                 "  - title: Typed\n    link: /guide/intro\nshowcaseCode: var x = 1;\nstats:\n  - value: 2M\n" +
			                 "cta:\n  title: Start\n---\n# Home\n")
		},
		{
			MockSite.Root + "/docs/guide/intro.md",
			new MockFileData("---\ntitle: Intro\norder: 1\n---\n# Intro\n\n## Setup\n\nText.\n\n### Details\n\nMore.\n\n## Usage\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n::: tip\nA tip.\n:::\n")
		},
		{
			MockSite.Root + "/docs/guide/install.md",
			new MockFileData("---\ntitle: Install\norder: 2\n---\n# Install\n\n## Steps\n\nText.\n\n## Check\n\nText.\n")
		},
		{
			MockSite.Root + "/docs/reference/options.md",
			new MockFileData("---\ntitle: Options\n---\n# Options\n\n## Flags\n\nText.\n\n## Values\n\nText.\n")
		}
	};

	public static TheoryData<string> Parts() => new(ThemeLayouts.Parts.Select(p => p.Key));

	[Theory]
	[MemberData(nameof(Parts))]
	public async Task RebuildVariants_AreTheOnesWhosePagesOrScriptDiffer(string key)
	{
		ThemeLayoutPart part = ThemeLayouts.Parts.Single(p => p.Key == key);
		ThemeLabOption option = ThemeLabCatalog.CreateOptions().Single(o => o.Key == "layout." + key);
		Dictionary<string, string> defaults = await _defaultPages.Value;
		string[] variants = [.. part.Names.Where(n => n != ThemeLayouts.Default)];
		Dictionary<string, string>[] builds = await Task.WhenAll(variants.Select(v => BuildPagesAsync(key, v)));

		for (int i = 0; i < variants.Length; i++)
		{
			string variant = variants[i];
			Dictionary<string, string> pages = builds[i];
			pages.Keys.Should().BeEquivalentTo(defaults.Keys);
			pages.Values.Should().AllSatisfy(page => HtmlTag(page).Should().Contain($"{option.Attribute}=\"{variant}\""));
			string[] differing = [.. pages.Where(p => WithoutHtmlTag(p.Value) != WithoutHtmlTag(defaults[p.Key])).Select(p => p.Key)];
			bool scriptReadsIt = ScriptReads(option.Attribute!, variant) && !_readOnEveryPass.Contains($"{option.Attribute}={variant}");

			ThemeLabCatalog.NeedsRebuild(part, variant).Should().Be(differing.Length > 0 || scriptReadsIt,
				$"{key}: {variant} changes {(differing.Length > 0 ? string.Join(", ", differing) : "no markup")} " +
				$"and {(scriptReadsIt ? "is" : "isn't")} read by the theme script");
		}
	}

	public static TheoryData<string> ReaderParts() => new(ThemeLayouts.ReaderParts.Select(p => p.Key));

	[Theory]
	[MemberData(nameof(ReaderParts))]
	public async Task LayoutSelector_PagesCarryTheMarkupOfEveryVariant(string key)
	{
		// With theme.options.layoutSelector a reader switches a part on an open page, so every variant's
		// markup is already there: a build with any variant differs from the default build only in the
		// <html> attributes and the panel's record of the site's variants.
		ThemeLayoutPart part = ThemeLayouts.ReaderParts.Single(p => p.Key == key);
		Dictionary<string, string> defaults = await _defaultSelectorPages.Value;
		string[] variants = [.. part.Names.Where(n => n != ThemeLayouts.Default)];
		Dictionary<string, string>[] builds = await Task.WhenAll(variants.Select(v => BuildPagesAsync(key, v, layoutSelector: true)));

		for (int i = 0; i < variants.Length; i++)
		{
			Dictionary<string, string> pages = builds[i];
			pages.Keys.Should().BeEquivalentTo(defaults.Keys);
			string[] differing = [.. pages.Where(p => WithoutSiteVariants(p.Value) != WithoutSiteVariants(defaults[p.Key])).Select(p => p.Key)];
			differing.Should().BeEmpty($"{key}: {variants[i]} should render no markup of its own when readers can switch it");
			pages.Values.Should().AllSatisfy(page => page.Should().Contain($"data-default-layout=\"{AttributeName(key)}={variants[i]} \""));
		}
	}

	[Fact]
	public async Task BuiltSite_NeverHasTheLab()
	{
		Dictionary<string, string> pages = await _defaultPages.Value;

		pages.Values.Should().NotContain(page => page.Contains("theme-lab", StringComparison.Ordinal));
		EmbeddedThemeProvider.GetJs().Should().NotContain("theme-lab");
	}

	// The theme script tells a variant apart when it names it right after the attribute, as in
	// html.getAttribute('data-navbar') !== 'autohide'.
	private static bool ScriptReads(string attribute, string variant)
	{
		string script = EmbeddedThemeProvider.GetJs();
		for (int at = script.IndexOf(attribute, StringComparison.Ordinal); at >= 0; at = script.IndexOf(attribute, at + 1, StringComparison.Ordinal))
		{
			string after = script.Substring(at, Math.Min(120, script.Length - at));
			if (after.Contains($"'{variant}'", StringComparison.Ordinal) || after.Contains($"\"{variant}\"", StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static string HtmlTag(string page) => HtmlTagPattern().Match(page).Value;

	// A variant links layouts.css, which the lab loads itself for a live preview; that link is no markup change.
	private static string WithoutHtmlTag(string page) =>
		LayoutsCssLinkPattern().Replace(HtmlTagPattern().Replace(page, "<html>", 1), "");

	private static string WithoutSiteVariants(string page) =>
		SiteVariantsPattern().Replace(HtmlTagPattern().Replace(page, "<html>", 1), "data-default-layout=\"\"");

	private static string AttributeName(string key) =>
		string.Concat(key.Select(c => char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : c.ToString()));

	private static async Task<Dictionary<string, string>> BuildPagesAsync(string part, string variant, bool layoutSelector = false)
	{
		SiteConfig config = SiteConfigReader.Parse(
			$"site:\n  title: Test Site\n  url: https://test.example.com\n  copyright: Copyright Test\ncontent:\n  docs: ./docs\n" +
			$"theme:\n  options:\n    layoutSelector: {(layoutSelector ? "true" : "false")}\n    layout:\n      {part}: {variant}\n");
		(_, MockFileSystem fs) = await MockSite.BuildAsync(config, Files());
		// Section redirects are plain pages without the theme.
		return fs.AllFiles
			.Where(f => f.EndsWith(".html", StringComparison.Ordinal))
			.ToDictionary(f => f, f => fs.File.ReadAllText(f))
			.Where(p => p.Value.Contains("/_theme/css/main.css", StringComparison.Ordinal))
			.ToDictionary();
	}

	[GeneratedRegex("<html\\b[^>]*>")]
	private static partial Regex HtmlTagPattern();

	[GeneratedRegex(@"<link[^>]*/_theme/css/layouts\.css[^>]*>\r?\n?")]
	private static partial Regex LayoutsCssLinkPattern();

	[GeneratedRegex("data-default-layout=\"[^\"]*\"")]
	private static partial Regex SiteVariantsPattern();
}
